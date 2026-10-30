//
//  MoonlightRTSP.swift
//  QuickRecorder
//
//  RTSP 服务端（TCP 48010）。
//
//  客户端行为（moonlight-common-c RtspConnection.c）：
//    * 每条命令使用「独立的 TCP 连接」，收到响应后由客户端关闭；服务端必须在响应后关闭连接
//    * 头部必须为「名称: 值」（冒号后一个空格），大小写敏感（Transport/Session/X-SS-*）
//    * 严格校验：状态行首 token 为 RTSP、协议为 RTSP/1.0、DESCRIBE 必须有 body、
//      audio SETUP 响应必须含非空 Session
//    * 序列：OPTIONS → DESCRIBE → SETUP(audio) → SETUP(video) → SETUP(control) → ANNOUNCE → PLAY /
//

import Foundation
import Network

/// 客户端通过 ANNOUNCE 协商出的媒体参数
struct MLMediaNegotiation {
    var width = 1920
    var height = 1080
    var fps = 60
    var packetSize = 1024
    /// 0 = H.264, 1 = HEVC, 2 = AV1
    var videoFormat = 0
    var bitrateKbps = 20000
    var audioChannels = 2
    var audioPacketDuration = 5
    var audioQuality = 0
    /// 客户端要求的音频/视频 QoS 标记类型（`x-nv-aqos.qosTrafficType` / `x-nv-vqos[0].qosTrafficType`）。
    /// 局域网串流时 Moonlight 会发 4（音频）/5（视频）= 要求打 DSCP；远程串流发 0 = 不标记。
    /// 宿主据此决定是否给音视频套接字加 DSCP 标记（否则链路层优先级永远不生效）。
    var audioQosType = 0
    var videoQosType = 0
    var encryptionEnabled: UInt32 = 0
    var mlFeatureFlags: UInt32 = 0
    /// 客户端旧式能力位 x-nv-general.featureFlags（含 NVFF_AUDIO_ENCRYPTION=0x20）
    var nvFeatureFlags: UInt32 = 0
    var dynamicRange = 0

    /// 客户端是否请求音频加密。两条来源任一惊动即需加密：
    ///   · x-ss-general.encryptionEnabled 的 SS_ENC_AUDIO 位（新版协商）
    ///   · x-nv-general.featureFlags 的 NVFF_AUDIO_ENCRYPTION 位（旧式协商；Moonlight iOS
    ///     encryptionFlags=ENCFLG_ALL 会无条件置位）
    var audioEncryptionEnabled: Bool {
        (encryptionEnabled & MLEncryption.audio) != 0
            || (nvFeatureFlags & MLNVFeatureFlags.audioEncryption) != 0
    }
}

protocol MLRTSPServerDelegate: AnyObject {
    /// ANNOUNCE 完成，可以开始采集与推流
    func rtspServer(_ server: MLRTSPServer, didNegotiate media: MLMediaNegotiation)
    /// PLAY 完成
    func rtspServerDidPlay(_ server: MLRTSPServer)
    /// TEARDOWN / 连接异常
    func rtspServerDidTearDown(_ server: MLRTSPServer)
}

final class MLRTSPServer {
    weak var delegate: MLRTSPServerDelegate?

    private var listener: NWListener?
    private let queue = DispatchQueue(label: "MLRTSPServer")
    private let port: Int
    private let logHandler: (String) -> Void
    private weak var hostState: MLHostState?

    /// 当前 RTSP 会话（一次只能有一个启动会话，与 Sunshine 一致）
    private var session: MLMediaNegotiation?
    private var announced = false

    init(port: Int, hostState: MLHostState, log: @escaping (String) -> Void) {
        self.port = port
        self.hostState = hostState
        self.logHandler = log
    }

    func start() throws {
        let parameters = NWParameters.tcp
        parameters.allowLocalEndpointReuse = true
        let listener = try NWListener(using: parameters, on: NWEndpoint.Port(rawValue: UInt16(port))!)
        listener.newConnectionHandler = { [weak self] connection in
            self?.handle(connection)
        }
        listener.stateUpdateHandler = { [weak self] state in
            if case .failed(let error) = state {
                self?.logHandler("RTSP 端口 \(self?.port ?? 0) 监听失败：\(error)")
            }
        }
        listener.start(queue: queue)
        self.listener = listener
        logHandler("RTSP 已监听端口 \(port)")
    }

    func stop() {
        listener?.cancel()
        listener = nil
    }

    /// 由 /launch 或 /resume 触发，准备接受 RTSP 握手
    func prepareSession() {
        queue.async { [weak self] in
            self?.session = MLMediaNegotiation()
            self?.announced = false
        }
    }

    // MARK: - 连接

    private func handle(_ connection: NWConnection) {
        connection.start(queue: queue)
        receive(connection, buffer: Data())
    }

    private func receive(_ connection: NWConnection, buffer: Data) {
        connection.receive(minimumIncompleteLength: 1, maximumLength: 64 * 1024) { [weak self] data, _, isComplete, error in
            guard let self else { return }
            var accumulated = buffer
            if let data = data { accumulated.append(data) }

            if let parsed = MLRTSPServer.parseRequest(accumulated) {
                let response = self.dispatch(method: parsed.method, target: parsed.target,
                                            headers: parsed.headers, body: parsed.body)
                self.send(connection, response: response)
                return
            }

            if isComplete || error != nil {
                connection.cancel()
                return
            }
            self.receive(connection, buffer: accumulated)
        }
    }

    private func send(_ connection: NWConnection, response: (head: String, body: Data)) {
        var out = Data(response.head.utf8)
        out.append(response.body)
        connection.send(content: out, completion: .contentProcessed { _ in
            // 关键：客户端以连接关闭作为响应结束标志
            connection.cancel()
        })
    }

    // MARK: - 解析

    struct ParsedRTSP {
        var method: String
        var target: String
        var headers: [String: String]
        var body: Data
    }

    static func parseRequest(_ data: Data) -> ParsedRTSP? {
        guard let headerEnd = data.range(of: Data("\r\n\r\n".utf8)) else { return nil }
        guard let headString = String(data: data[..<headerEnd.lowerBound], encoding: .utf8) else { return nil }
        var lines = headString.components(separatedBy: "\r\n")
        guard !lines.isEmpty else { return nil }
        let requestLine = lines.removeFirst().components(separatedBy: " ")
        guard requestLine.count >= 3 else { return nil }

        var headers: [String: String] = [:]
        for line in lines {
            guard let colon = line.firstIndex(of: ":") else { continue }
            let name = String(line[..<colon]).trimmingCharacters(in: .whitespaces)
            let value = String(line[line.index(after: colon)...]).trimmingCharacters(in: .whitespaces)
            headers[name] = value
        }

        let bodyStart = headerEnd.upperBound
        let contentLength = Int(headers["Content-length"] ?? headers["Content-Length"] ?? "0") ?? 0
        var body = Data()
        if contentLength > 0 {
            guard data.count >= bodyStart + contentLength else { return nil }
            body = data.subdata(in: bodyStart..<(bodyStart + contentLength))
        }
        return ParsedRTSP(method: requestLine[0], target: requestLine[1], headers: headers, body: body)
    }

    // MARK: - 分发

    private func dispatch(method: String, target: String, headers: [String: String], body: Data) -> (head: String, body: Data) {
        let cseq = headers["CSeq"] ?? headers["cseq"] ?? "0"

        let respond: (String, [(String, String)], Data) -> (String, Data) = { status, extraHeaders, responseBody in
            var head = "RTSP/1.0 \(status)\r\n"
            head += "CSeq: \(cseq)\r\n"
            for (k, v) in extraHeaders {
                head += "\(k): \(v)\r\n"
            }
            head += "Content-Length: \(responseBody.count)\r\n"
            head += "\r\n"
            return (head, responseBody)
        }

        switch method {
        case "OPTIONS":
            return respond("200 OK", [
                ("Public", "OPTIONS, DESCRIBE, SETUP, ANNOUNCE, PLAY"),
                ("Server", "sunGame/1.0"),
            ], Data())

        case "DESCRIBE":
            let sdp = describeBody()
            return respond("200 OK", [("Content-Type", "application/sdp")], Data(sdp.utf8))

        case "SETUP":
            return setupResponse(target: target, respond: respond)

        case "ANNOUNCE":
            handleAnnounce(body: body, headers: headers)
            return respond("200 OK", [], Data())

        case "PLAY":
            queue.async { [weak self] in
                guard let self else { return }
                self.delegate?.rtspServerDidPlay(self)
            }
            return respond("200 OK", [], Data())

        default:
            return respond("404 NOT FOUND", [], Data())
        }
    }

    /// DESCRIBE 响应体：只需非空。注意不能出现 sprop-parameter-sets=AAAAAU（会被判定为 HEVC）
    private func describeBody() -> String {
        var lines: [String] = []
        lines.append("a=x-ss-general.featureFlags:\(MLHostFeatureFlags.penTouchEvents | MLHostFeatureFlags.controllerTouchEvents)")
        // 与 Sunshine 一致：广告「控制流 + 音频」加密能力，但仅请求控制流加密。
        // 客户端若 opt-in（Moonlight iOS 默认 encryptionFlags=ENCFLG_ALL 会 opt-in），
        // 会通过 x-ss-general.encryptionEnabled 的音频位 / x-nv-general.featureFlags 的
        // 0x20 位告知主机；主机据此加密音频，否则客户端解密明文失败、完全静音。
        lines.append("a=x-ss-general.encryptionSupported:\(MLEncryption.controlV2 | MLEncryption.audio)")
        lines.append("a=x-ss-general.encryptionRequested:\(MLEncryption.controlV2)")
        lines.append("a=x-nv-video[0].refPicInvalidation:1")
        lines.append("a=rtpmap:96 H264/90000")
        lines.append("a=fmtp:97 surround-params=\(MLAudio.stereoSurroundParams)")
        return lines.joined(separator: "\r\n") + "\r\n"
    }

    private func setupResponse(target: String, respond: (String, [(String, String)], Data) -> (String, Data)) -> (String, Data) {
        // target 形如 streamid=video/0/0
        var streamType = ""
        if let range = target.range(of: "streamid=") {
            let rest = target[range.upperBound...]
            streamType = String(rest.split(separator: "/").first ?? "")
        }

        var extra: [(String, String)] = []
        let sessionID = "DEADBEEFCAFE;timeout=60"

        switch streamType {
        case "audio":
            extra.append(("Session", sessionID))
            extra.append(("Transport", "unicast;server_port=\(MLPort.audio);source=0.0.0.0"))
            if let launch = hostState?.currentLaunchSession {
                extra.append(("X-SS-Ping-Payload", String(bytes: launch.avPingPayloadAudio, encoding: .utf8) ?? ""))
            }
        case "video":
            extra.append(("Session", sessionID))
            extra.append(("Transport", "unicast;server_port=\(MLPort.video);source=0.0.0.0"))
            if let launch = hostState?.currentLaunchSession {
                extra.append(("X-SS-Ping-Payload", String(bytes: launch.avPingPayloadVideo, encoding: .utf8) ?? ""))
            }
        case "control":
            extra.append(("Session", sessionID))
            extra.append(("Transport", "unicast;server_port=\(MLPort.control);source=0.0.0.0"))
            if let launch = hostState?.currentLaunchSession {
                extra.append(("X-SS-Connect-Data", "\(launch.controlConnectData)"))
            }
        default:
            return respond("404 NOT FOUND", [], Data())
        }

        return respond("200 OK", extra, Data())
    }

    private func handleAnnounce(body: Data, headers: [String: String]) {
        guard let text = String(data: body, encoding: .utf8) else { return }
        var attributes: [String: String] = [:]
        for rawLine in text.components(separatedBy: "\r\n") {
            let line = rawLine.trimmingCharacters(in: .whitespaces)
            guard line.hasPrefix("a="), let colon = line.firstIndex(of: ":") else { continue }
            let name = String(line[line.index(line.startIndex, offsetBy: 2)..<colon])
            let value = String(line[line.index(after: colon)...]).trimmingCharacters(in: .whitespaces)
            attributes[name] = value
        }

        queue.async { [weak self] in
            guard let self else { return }
            var media = MLMediaNegotiation()
            if let v = attributes["x-nv-video[0].clientViewportWd"], let n = Int(v), n > 0 { media.width = n }
            if let v = attributes["x-nv-video[0].clientViewportHt"], let n = Int(v), n > 0 { media.height = n }
            if let v = attributes["x-nv-video[0].maxFPS"], let n = Int(v), n > 0 { media.fps = n }
            if let v = attributes["x-nv-video[0].packetSize"], let n = Int(v), n > 0 { media.packetSize = n }
            if let v = attributes["x-nv-vqos[0].bitStreamFormat"], let n = Int(v) { media.videoFormat = n }
            if let v = attributes["x-nv-vqos[0].bw.maximumBitrateKbps"], let n = Int(v) { media.bitrateKbps = n }
            if let v = attributes["x-nv-audio.surround.numChannels"], let n = Int(v) { media.audioChannels = n }
            if let v = attributes["x-nv-aqos.packetDuration"], let n = Int(v) { media.audioPacketDuration = n }
            if let v = attributes["x-nv-audio.surround.AudioQuality"], let n = Int(v) { media.audioQuality = n }
            if let v = attributes["x-nv-aqos.qosTrafficType"], let n = Int(v) { media.audioQosType = n }
            if let v = attributes["x-nv-vqos[0].qosTrafficType"], let n = Int(v) { media.videoQosType = n }
            if let v = attributes["x-nv-video[0].dynamicRangeMode"], let n = Int(v) { media.dynamicRange = n }
            if let v = attributes["x-ss-general.encryptionEnabled"], let n = UInt32(v) { media.encryptionEnabled = n }
            if let v = attributes["x-ml-general.featureFlags"], let n = UInt32(v) { media.mlFeatureFlags = n }
            if let v = attributes["x-nv-general.featureFlags"], let n = UInt32(v) { media.nvFeatureFlags = n }

            self.session = media
            self.announced = true
            self.logHandler("RTSP 协商完成：\(media.width)x\(media.height)@\(media.fps) 码率 \(media.bitrateKbps)Kbps 包大小 \(media.packetSize) 音频 \(media.audioChannels)ch/\(media.audioPacketDuration)ms 加密=\(media.encryptionEnabled)(video位=\(MLEncryption.video)，audio位=\(MLEncryption.audio)) 音频加密=\(media.audioEncryptionEnabled ? "开" : "关") nv特性=\(media.nvFeatureFlags) 音质=\(media.audioQuality) 特性=\(media.mlFeatureFlags) QoS(音/视)=\(media.audioQosType)/\(media.videoQosType)")
            self.delegate?.rtspServer(self, didNegotiate: media)
        }
    }
}
