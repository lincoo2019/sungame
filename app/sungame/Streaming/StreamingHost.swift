//
//  StreamingHost.swift
//  sunGame
//
//  串流主机编排：启动 HTTP/HTTPS/RTSP 服务，处理 /launch 到 RTSP PLAY 的完整会话生命周期，
//  驱动游戏 / PS5 两种画面来源的推流、控制通道与输入。
//
//  本工程从 QuickRecorder 中剥离而来，只保留「植物大战僵尸（游戏）」与「PS5」两种串流目标，
//  不包含屏幕录制、虚拟桌面与窗口共享。
//

import Foundation
import Combine

/// 画面来源：无窗口运行的 Godot 游戏，或 PS5 直通画面
enum MLShareMode: Equatable {
    /// PS5：画面来自 libchiaki 会话，不采集主机屏幕
    case ps5
    /// 游戏：画面来自无窗口运行的 Godot 游戏（MLGameStreamSession 编出的 H.264）
    case game(projectPath: String)
}

/// 会话状态
enum MLStreamingState: Equatable {
    case stopped
    case waitingForClient
    case paired(name: String)
    case streaming(width: Int, height: Int)
    case failed(String)

    var description: String {
        switch self {
        case .stopped: return "未启动"
        case .waitingForClient: return "等待 Moonlight 连接"
        case .paired(let name): return "已配对 / 已连接：\(name)"
        case .streaming(let w, let h): return "正在串流 \(w)×\(h)"
        case .failed(let message): return "错误：\(message)"
        }
    }
}

@MainActor
final class MLStreamingHost: ObservableObject {
    static let shared = MLStreamingHost()

    // MARK: 对外状态（UI 绑定）

    @Published private(set) var isRunning = false
    @Published private(set) var state: MLStreamingState = .stopped
    /// 非 nil 时表示正在等待用户输入 Moonlight 显示的 PIN（值为发起配对的设备名）
    @Published private(set) var pairingPINNeeded: String?
    @Published private(set) var localIP: String = ""
    @Published private(set) var pairedClients: [MLPairedClient] = []
    @Published private(set) var logLines: [String] = []

    // MARK: PS5
    /// 注册进度提示
    @Published private(set) var ps5Status: String?
    @Published private(set) var ps5Registering = false
    /// 串流过程中主机要求输入 PIN
    @Published private(set) var ps5PINRequested = false

    private var hostState: MLHostState?
    private var httpServer: MLHTTPServer?
    private var httpsServer: MLHTTPServer?
    private var rtspServer: MLRTSPServer?

    private var controlStream: MLControlStream?
    private var videoStream: MLVideoStream?
    private var audioStream: MLAudioStream?

    private var currentLaunch: MLLaunchSession?
    /// 周期性打印控制通道状态，便于排查连接问题
    private var statusTimer: Timer?

    /// 当前画面来源（连接建立后按客户端选择的应用确定）
    private(set) var shareMode: MLShareMode?
    /// 客户端 ANNOUNCE 协商结果
    private var mediaNegotiation: MLMediaNegotiation?
    /// PS5 会话（libchiaki 包装）与视频直通转发器
    private let ps5Session = ChiakiPS5Session()
    private let ps5Forwarder = PS5VideoForwarder()
    /// 游戏串流会话（无窗口 Godot 游戏）与视频桥接（AVCC→Annex-B）
    private var gameSession: MLGameStreamSession?
    private var gameBridge: MLGameVideoBridge?
    /// PS5 会话建立失败后的自动重试次数
    private var ps5SessionRetryCount = 0
    private let ps5SessionMaxRetries = 3
    /// 上次向 PS5 请求关键帧的时间（限流，避免 IDR 风暴）
    private var lastPS5KeyFrameRequestAt: Date?
    /// 用于丢弃过期的切换请求
    private var switchGeneration = 0

    private init() {}

    // MARK: - 日志

    /// 日志文件路径（供排查连接问题；位于 App 支持目录下）
    private(set) var logFileURL: URL?

    private let logFileQueue = DispatchQueue(label: "MLStreamingHost.logFile")

    /// 仅用于日志文件写入（失败时静默忽略，不影响功能）
    private func appendToLogFile(_ line: String) {
        guard let url = logFileURL, let data = (line + "\n").data(using: .utf8) else { return }
        logFileQueue.async {
            if let handle = try? FileHandle(forWritingTo: url) {
                defer { try? handle.close() }
                _ = try? handle.seekToEnd()
                try? handle.write(contentsOf: data)
            } else {
                try? data.write(to: url)
            }
        }
    }

    private func log(_ message: String) {
        let stamp = DateFormatter.localizedString(from: Date(), dateStyle: .none, timeStyle: .medium)
        let line = "[\(stamp)] \(message)"
        appendToLogFile(line)
        Task { @MainActor in
            self.logLines.append(line)
            if self.logLines.count > 400 {
                self.logLines.removeFirst(self.logLines.count - 400)
            }
        }
        NSLog("[sunGame/Streaming] \(message)")
    }

    // MARK: - 启动 / 停止

    func start() {
        guard !isRunning else { return }
        do {
            let directory = try Self.supportDirectory()
            // 保留上一会话日志（streaming.log → streaming.log.1）再开新文件
            let logURL = directory.appendingPathComponent("streaming.log")
            logFileURL = logURL
            let previousLogURL = directory.appendingPathComponent("streaming.log.1")
            try? FileManager.default.removeItem(at: previousLogURL)
            if FileManager.default.fileExists(atPath: logURL.path) {
                try? FileManager.default.moveItem(at: logURL, to: previousLogURL)
            }

            let state = try MLHostState(directory: directory) { [weak self] message in
                self?.log(message)
            }
            // 配对需要用户填入 Moonlight 显示的 PIN，这里把该请求转达给界面
            state.onPairingPINNeeded = { [weak self] deviceName in
                Task { @MainActor in
                    self?.pairingPINNeeded = deviceName
                    self?.log("Moonlight 正在配对，请把它显示的 4 位 PIN 填入本界面")
                }
            }
            hostState = state
            pairedClients = state.pairedClients
            localIP = Self.primaryIPv4Address()

            try startHTTPServer(state: state)
            try startHTTPSServer(state: state)
            try startRTSPServer(state: state)

            isRunning = true
            state_(.waitingForClient)
            log("串流主机已启动：HTTP \(MLPort.http) / HTTPS \(MLPort.https) / RTSP \(MLPort.rtsp)")
            log("请在 Moonlight 中添加主机 \(localIP)；配对时把 Moonlight 显示的 4 位 PIN 填到本界面")
            startStatusLogging()
        } catch {
            state_(.failed(error.localizedDescription))
            log("启动失败：\(error.localizedDescription)")
        }
    }

    func stop() {
        endSession(reason: "主机停止")
        statusTimer?.invalidate()
        statusTimer = nil
        rtspServer?.stop()
        httpServer?.stop()
        httpsServer?.stop()
        rtspServer = nil
        httpServer = nil
        httpsServer = nil
        isRunning = false
        state_(.stopped)
        log("串流主机已停止")
    }

    /// 提交 Moonlight 显示的 4 位 PIN
    func submitPIN(_ pin: String) {
        hostState?.submitPIN(pin)
        pairingPINNeeded = nil
    }

    /// 周期性记录控制通道状态（仅在会话存在时输出）
    private func startStatusLogging() {
        statusTimer?.invalidate()
        statusTimer = Timer.scheduledTimer(withTimeInterval: 5, repeats: true) { [weak self] _ in
            Task { @MainActor in
                guard let self, let control = self.controlStream else { return }
                self.log(control.statusDescription)
            }
        }
    }

    func unpairAll() {
        hostState?.removeAllClients()
        pairedClients = hostState?.pairedClients ?? []
        log("已解除全部客户端配对")
    }

    // MARK: - 服务

    private func startHTTPServer(state: MLHostState) throws {
        let server = try MLHTTPServer(port: MLPort.http, identity: nil,
                                      requireClientCertificate: false,
                                      log: { [weak self] in self?.log($0) })
        server.route("^/serverinfo$") { [weak self, weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            completion(state.serverInfoResponse(https: false, clientUniqueID: request.query["uniqueid"],
                                                localIP: self?.localIP ?? "127.0.0.1"))
        }
        server.route("^/pair$") { [weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            state.pairResponse(request, completion: completion)
        }
        try server.start()
        httpServer = server
    }

    private func startHTTPSServer(state: MLHostState) throws {
        let server = try MLHTTPServer(port: MLPort.https, identity: state.identity,
                                      requireClientCertificate: true,
                                      log: { [weak self] in self?.log($0) })
        server.route("^/serverinfo$") { [weak self, weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            completion(state.serverInfoResponse(https: true, clientUniqueID: request.query["uniqueid"],
                                                localIP: self?.localIP ?? "127.0.0.1"))
        }
        server.route("^/pair$") { [weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            state.pairResponse(request, completion: completion)
        }
        server.route("^/applist$") { [weak state] _, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            completion(state.appListResponse())
        }
        server.route("^/launch$") { [weak self, weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            let response = state.launchResponse(request, localIP: self?.localIP ?? "127.0.0.1") { session in
                Task { @MainActor in self?.prepareSession(session) }
            }
            completion(response)
        }
        server.route("^/resume$") { [weak self, weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            let response = state.resumeResponse(request, localIP: self?.localIP ?? "127.0.0.1") { session in
                Task { @MainActor in self?.prepareSession(session) }
            }
            completion(response)
        }
        server.route("^/cancel$") { [weak self, weak state] request, completion in
            guard let state = state else {
                completion(MLHTTPResponse(status: 500, reason: "INTERNAL ERROR")); return
            }
            let response = state.cancelResponse(request)
            Task { @MainActor in self?.endSession(reason: "客户端取消会话") }
            completion(response)
        }
        server.route("^/appasset$") { _, completion in
            // 不提供应用图标
            completion(MLHTTPResponse(status: 404, reason: "NOT FOUND",
                                      body: Data("<root status_code=\"404\"/>".utf8)))
        }
        try server.start()
        httpsServer = server
    }

    private func startRTSPServer(state: MLHostState) throws {
        let server = MLRTSPServer(port: MLPort.rtsp, hostState: state) { [weak self] message in
            self?.log(message)
        }
        server.delegate = self
        try server.start()
        rtspServer = server
    }

    // MARK: - 会话生命周期

    private func prepareSession(_ session: MLLaunchSession) {
        currentLaunch = session
        hostState?.setLaunchSession(session)
        rtspServer?.prepareSession()
        state_(.paired(name: session.clientName))
        log("会话已启动：\(session.appID)，分辨率 \(session.width)×\(session.height)@\(session.fps)")

        // 释放上一次会话可能残留的 socket。
        // UDP 允许重复绑定同一端口（SO_REUSEADDR），若旧 socket 未关闭，
        // 客户端发来的包可能被旧实例收走并按其过期状态处理。
        controlStream?.stop()
        controlStream = nil
        videoStream?.stop()
        videoStream = nil
        audioStream?.stop()
        audioStream = nil

        // 控制通道与音视频 socket 必须在 RTSP SETUP 返回端口前就绪
        do {
            let control = try MLControlStream(port: MLPort.control) { [weak self] message in
                self?.log(message)
            }
            control.delegate = self
            control.gcmKey = session.gcmKey
            control.encryptionEnabled = true
            control.expectedConnectData = session.controlConnectData
            controlStream = control
            control.start()

            let video = try MLVideoStream(pingPayload: session.avPingPayloadVideo) { [weak self] message in
                self?.log(message)
            }
            videoStream = video
            video.start()

            let audio = try MLAudioStream(pingPayload: session.avPingPayloadAudio) { [weak self] message in
                self?.log(message)
            }
            audioStream = audio
            audio.start()
        } catch {
            state_(.failed(error.localizedDescription))
            log("媒体端口启动失败：\(error.localizedDescription)")
        }
    }

    private func startCapture(media: MLMediaNegotiation) {
        guard let video = videoStream else { return }
        video.configure(packetSize: media.packetSize)
        mediaNegotiation = media
        // 音频加密：客户端（Moonlight iOS 默认 encryptionFlags=ENCFLG_ALL）会请求音频加密，
        // 必须用 /launch 的 rikey + rikeyid 对每个音频负载做 AES-128-CBC(PKCS#7)，
        // IV = BE32(riKeyId+seq) 后 12 字节 0；否则客户端把明文当密文解密 → opus 解码失败 → 完全静音。
        if let launch = currentLaunch {
            audioStream?.configureAudioEncryption(key: launch.gcmKey, riKeyID: launch.riKeyID,
                                                  enabled: media.audioEncryptionEnabled)
        }
        log("音频加密=\(media.audioEncryptionEnabled ? "开(AES-128-CBC)" : "关") riKeyID=\(currentLaunch?.riKeyID ?? 0)")
        // 链路优先级（QoS）：协商完成后按 qosTrafficType 套用一次。
        audioStream?.qosDscpTagging = media.audioQosType != 0
        videoStream?.qosDscpTagging = media.videoQosType != 0
        audioStream?.applyQoS()
        videoStream?.applyQoS()
        log("客户端 QoS 请求：音频 qosTrafficType=\(media.audioQosType)，视频 qosTrafficType=\(media.videoQosType)")

        // 应用列表只有 PS5 与游戏两项
        let initialMode: MLShareMode
        if currentLaunch?.appID == MLAppID.ps5 {
            initialMode = .ps5
        } else {
            initialMode = .game(projectPath: Self.gameProjectPath())
        }
        shareMode = initialMode

        // PS5：chiaki 送来的 Annex-B 码流交给直通转发器
        let forwarder = ps5Forwarder
        ps5Session.onVideoSample = { data, framesLost, recovered in
            forwarder.handle(data, framesLost: framesLost, recovered: recovered)
        }
        ps5Session.onEvent = { [weak self] message in
            Task { @MainActor in self?.log("PS5：\(message)") }
        }
        ps5Session.onStateChange = { [weak self] ps5State in
            Task { @MainActor in self?.handlePS5State(ps5State) }
        }
        ps5Session.onPINRequest = { [weak self] incorrect in
            Task { @MainActor in
                self?.ps5PINRequested = true
                self?.log(incorrect ? "PS5 要求重新输入 PIN（上次错误）" : "PS5 要求输入 PIN")
            }
        }

        switch initialMode {
        case .ps5: log("已收到协商，准备进入 PS5")
        case .game: log("已收到协商，准备进入游戏")
        }

        Task { [weak self] in
            guard let self else { return }
            // 等待客户端在视频端口发送 ping（用于学习目标端点，与 Sunshine 行为一致）
            let videoStreamReference = self.videoStream
            let hasPeer = await Task.detached { videoStreamReference?.waitForPeer(timeout: 10) ?? false }.value
            guard hasPeer else {
                await MainActor.run {
                    self.log("等待客户端视频 ping 超时，未开始推流")
                    self.state_(.failed("客户端未建立视频通道"))
                }
                return
            }
            await self.beginCapture(mode: initialMode)
        }
    }

    /// 启动画面来源。两种模式都不采集主机屏幕：PS5 走 chiaki 直通，游戏走无窗口 Godot 编码。
    private func beginCapture(mode: MLShareMode) async {
        guard let media = mediaNegotiation, videoStream != nil else { return }

        switchGeneration += 1
        let generation = switchGeneration

        // 重置旧会话：停掉游戏子进程与输入转发
        stopGameSession()

        // 断开 chiaki 会话并清掉直通回调
        MLInputInjector.shared.onGamepadState = nil
        ps5Session.onAudioFrame = nil
        ps5Session.stop()
        ps5Forwarder.onFrame = nil
        ps5Forwarder.onLog = nil
        ps5Forwarder.onNeedKeyFrame = nil
        ps5Forwarder.reset()

        guard generation == switchGeneration else { return }

        let bitrate = max(media.bitrateKbps, 1000) * 1000

        switch mode {
        case .ps5:
            let settings = ChiakiPS5Settings.load()
            guard settings.isReady else {
                log("PS5 尚未配置（需启用并填写主机地址、完成注册）")
                state_(.failed("PS5 未配置"))
                return
            }
            // PS5 走视频直通：不建编码管线，chiaki 给出的 H.264 裸流原样转发给客户端。
            // 画面分辨率由 PS5 决定（1080p），这里只按协商尺寸配置输入映射。
            let output = cappedCanvas(CGSize(width: media.width, height: media.height),
                                      maxWidth: 1920, maxHeight: 1080)
            MLInputInjector.shared.configure(layout: .externalSource(outputSize: output, toolbarHeight: 0))
            shareMode = .ps5

            let sessionRef = ps5Session
            let streamRef = videoStream
            ps5Forwarder.reset()
            ps5Forwarder.onLog = { message in sessionRef.report("视频 \(message)") }
            ps5Forwarder.onNeedKeyFrame = { [weak self] in
                Task { @MainActor in self?.requestPS5KeyFrame() }
            }
            ps5Forwarder.onFrame = { data, isIDR, timestamp in
                streamRef?.sendFrame(data, isIDR: isIDR, timestamp90k: timestamp)
            }
            ps5Session.start(settings: settings)
            // PS5 音频（chiaki 直接给出 Opus）转发到 Moonlight 音频通道。
            // 客户端按协商时长固定分配解码缓冲，而 PS5 固定送 10ms 帧，两端不一致时必须重打包。
            if media.audioChannels >= 2, let audioStream {
                let resampler = PS5AudioResampler(targetMs: UInt32(max(1, media.audioPacketDuration)))
                ps5Session.onAudioFrame = { data, durationMs in
                    for (packet, ms) in resampler.packets(from: data, durationMs: durationMs) {
                        audioStream.sendOpusFrame(packet, durationMs: ms)
                    }
                }
            } else {
                ps5Session.onAudioFrame = nil
            }
            // 远程手柄 → chiaki 控制器状态
            MLInputInjector.shared.onGamepadState = { pad in
                sessionRef.sendControllerState(ChiakiControllerState.fromMoonlight(pad))
            }
            log("画面来源：PS5（直通 \(settings.host)，\(settings.fps)fps）")
            state_(.streaming(width: Int(output.width), height: Int(output.height)))

        case .game(let projectPath):
            // 游戏走视频直通：MLGameStreamSession 编出的 H.264 经桥接转成 Annex-B 后交给视频通道。
            // 尺寸统一取本次协商分辨率（仅做 4K 上限保护），保证输入坐标参照系一致。
            let canvas = cappedCanvas(CGSize(width: media.width, height: media.height))
            MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                           contentRect: CGRect(origin: .zero, size: canvas),
                                                           gameSize: canvas))
            shareMode = .game(projectPath: projectPath)

            let session = MLGameStreamSession(width: Int(canvas.width), height: Int(canvas.height),
                                              fps: min(max(media.fps, 30), 60), bitrateBps: bitrate,
                                              executablePath: Self.gameExecutablePath(),
                                              projectPath: projectPath)
            session.negotiatedSize = CGSize(width: media.width, height: media.height)
            session.mappingSize = canvas
            session.audioStream = media.audioChannels >= 2 ? audioStream : nil
            session.audioChannels = media.audioChannels
            session.audioPacketDurationMs = media.audioPacketDuration
            // Opus 码率对齐 Sunshine 高音质档（立体声 512Kbps）
            session.audioBitrateBps = 512_000
            let bridge = MLGameVideoBridge(session: session)
            let streamRef = videoStream
            bridge.onFrame = { data, isIDR, timestamp in
                streamRef?.sendFrame(data, isIDR: isIDR, timestamp90k: timestamp)
            }
            session.onLog = { [weak self] message in
                Task { @MainActor in self?.log("游戏：\(message)") }
            }
            session.onOutput = { [weak self] line in
                guard MLStreamingHost.isLogWorthyGameOutput(line) else { return }
                Task { @MainActor in self?.log("游戏输出：\(line)") }
            }
            session.onExit = { [weak self] status in
                Task { @MainActor in self?.log("游戏进程已退出（状态码 \(status)）") }
            }
            MLInputInjector.shared.onLog = { [weak self] message in
                Task { @MainActor in self?.log(message) }
            }
            // Moonlight 输入（触摸/鼠标）→ 固定 24 字节包 → 游戏。
            MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
                session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                                  dx: dx, dy: dy, touchEvent: touchEvent)
            }
            gameSession = session
            gameBridge = bridge
            do {
                try session.start()
                log("画面来源：游戏（\(projectPath)）")
                state_(.streaming(width: Int(canvas.width), height: Int(canvas.height)))
            } catch {
                log("游戏启动失败：\(error.localizedDescription)")
                state_(.failed(error.localizedDescription))
            }
        }
    }

    /// 把编码画布限制在合理范围内（支持到 4K）。
    private func cappedCanvas(_ size: CGSize, maxWidth: CGFloat = 3840, maxHeight: CGFloat = 2160) -> CGSize {
        let scale = min(1, min(maxWidth / max(size.width, 1), maxHeight / max(size.height, 1)))
        return CGSize(width: max(2, (size.width * scale).rounded()),
                      height: max(2, (size.height * scale).rounded()))
    }

    /// PS5 会话状态变化
    private func handlePS5State(_ ps5State: ChiakiPS5Session.State) {
        guard shareMode == .ps5 else { return }
        switch ps5State {
        case .streaming:
            log("PS5 已连接，开始接收画面")
            // 连接建立时主机送来的是 GOP 中间的 P 帧，必须先要一个关键帧
            ps5Session.requestKeyFrame()
        case .failed(let reason):
            log("PS5 连接失败：\(reason)")
            state_(.failed("PS5：\(reason)"))
            schedulePS5Retry()
        case .idle, .connecting:
            break
        }
    }

    /// PS5 会话建立失败后自动重试。主机正忙、网络抖动等情况很常见，重试通常就能成功。
    private func schedulePS5Retry() {
        guard shareMode == .ps5 else { return }
        guard ps5SessionRetryCount < ps5SessionMaxRetries else {
            log("PS5 多次重试仍未成功，停止本次串流")
            state_(.failed("PS5 连接失败"))
            return
        }
        ps5SessionRetryCount += 1
        let attempt = ps5SessionRetryCount
        log("PS5 连接失败，3 秒后自动重试（第 \(attempt)/\(ps5SessionMaxRetries) 次）")
        Task { [weak self] in
            try? await Task.sleep(nanoseconds: 3_000_000_000)
            guard let self, self.shareMode == .ps5 else { return }
            await self.beginCapture(mode: .ps5)
        }
    }

    // MARK: - PS5 注册

    /// 发起 PS5 注册（首次与主机配对）：主机上会显示 PIN，在主机端确认后完成。
    /// 成功后把注册信息写回设置。
    func registerPS5(host address: String, ps5: Bool, psnAccountId: String, pin: String, consolePin: String) {
        guard !ps5Registering else { return }
        ps5Registering = true
        ps5Status = "正在注册…请在主机上确认 PIN"

        ps5Session.onRegistKey = { [weak self] key, morning in
            Task { @MainActor in
                var settings = ChiakiPS5Settings.load()
                settings.host = address
                settings.ps5 = ps5
                settings.psnAccountId = psnAccountId
                settings.registKey = key
                settings.morning = morning
                settings.enabled = true
                settings.save()
                self?.log("PS5 注册成功，注册信息已保存")
            }
        }
        ps5Session.onRegistrationFinished = { [weak self] success, message in
            Task { @MainActor in
                self?.ps5Registering = false
                self?.ps5Status = message
                self?.log("PS5 注册\(success ? "成功" : "结束")：\(message)")
            }
        }
        ps5Session.startRegistration(host: address, ps5: ps5, psnAccountId: psnAccountId,
                                     pin: pin, consolePin: consolePin)
    }

    /// 取消正在进行的注册
    func cancelPS5Registration() {
        ps5Session.cancelRegistration()
        ps5Registering = false
        ps5Status = "已取消注册"
    }

    /// 串流过程中主机要求 PIN 时提交
    func submitPS5PIN(_ pin: String) {
        ps5PINRequested = false
        ps5Session.sendPIN(pin)
    }

    /// 停止游戏串流会话：先摘掉桥接回调，再停会话（会 kill 子进程、关闭输入 socket）。可重复调用。
    private func stopGameSession() {
        guard gameSession != nil || gameBridge != nil else { return }
        gameBridge?.onFrame = nil
        gameBridge = nil
        gameSession?.onOutput = nil
        gameSession?.onLog = nil
        gameSession?.onExit = nil
        gameSession?.stop()
        gameSession = nil
        MLInputInjector.shared.onGameInput = nil
        MLInputInjector.shared.onLog = nil
    }

    private func endSession(reason: String) {
        log("结束会话：\(reason)")
        switchGeneration += 1
        controlStream?.sendTermination()
        controlStream?.stop()
        controlStream = nil
        videoStream?.stop()
        videoStream = nil
        audioStream?.stop()
        audioStream = nil
        MLInputInjector.shared.onGamepadState = nil
        MLInputInjector.shared.onGameInput = nil
        MLInputInjector.shared.onLog = nil
        stopGameSession()
        ps5Session.stop()
        ps5Forwarder.onFrame = nil
        ps5Forwarder.onLog = nil
        ps5Forwarder.onNeedKeyFrame = nil
        ps5Forwarder.reset()
        ps5SessionRetryCount = 0
        mediaNegotiation = nil
        shareMode = nil
        hostState?.setLaunchSession(nil)
        currentLaunch = nil
        if isRunning { state_(.waitingForClient) }
    }

    private func state_(_ value: MLStreamingState) {
        state = value
    }

    // MARK: - 工具

    /// 判断游戏进程的一行 stdout/stderr 是否值得写入串流日志。
    /// 只保留：引擎输入诊断（`QRInput:` 等）与异常输出。
    /// nonisolated：由子进程管道的后台线程调用。
    nonisolated static func isLogWorthyGameOutput(_ line: String) -> Bool {
        if line.hasPrefix("QRInput:") { return true }
        if line.hasPrefix("QRSize:") { return true }
        if line.hasPrefix("QRAudio:") { return true }
        if line.contains("ERROR:") || line.contains("SCRIPT ERROR") || line.contains("WARNING:") {
            return true
        }
        return false
    }

    /// 游戏工程目录来源：优先 UserDefaults 的 QRGameProjectPath，否则用固定默认值。
    /// nonisolated：/applist 在非主线程的 HTTP 处理里构造应用条目，需要在此处直接读取。
    nonisolated static func gameProjectPath() -> String {
        if let path = UserDefaults.standard.string(forKey: "QRGameProjectPath"), !path.isEmpty {
            return path
        }
        return "/Volumes/soft/backup/prototype/remote/植物大战僵尸杂交版"
    }

    /// 引擎与 .NET 运行时所在的持久化目录（Application Support 下）。
    ///
    /// 沿用 QuickRecorder 的目录，避免独立应用首次启动时找不到已部署的引擎与 dotnet。
    nonisolated static func gameEngineDirectory() -> String {
        let base = (try? FileManager.default.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                                 appropriateFor: nil, create: false).path)
            ?? (NSHomeDirectory() + "/Library/Application Support")
        return base + "/QuickRecorder"
    }

    /// 游戏可执行文件路径（官方 Godot_mono.app 副本里被替换过的二进制）。
    /// 允许 QR_GAMESTREAM_EXEC 覆盖，便于自测与不同部署环境；否则按「持久化位置 → 旧 /tmp 位置」依次探测。
    nonisolated static func gameExecutablePath() -> String {
        if let path = ProcessInfo.processInfo.environment["QR_GAMESTREAM_EXEC"], !path.isEmpty {
            return path
        }
        let candidates = [
            gameEngineDirectory() + "/Engine/GodotPatched.app/Contents/MacOS/Godot",
            "/tmp/qr-probe/GodotPatched.app/Contents/MacOS/Godot",
        ]
        if let hit = candidates.first(where: { FileManager.default.isExecutableFile(atPath: $0) }) {
            return hit
        }
        return candidates[0]
    }

    /// 供 mono 版 Godot 解析 .NET 运行时用的 DOTNET_ROOT（含 .NET 9 SDK 的持久化目录）。
    nonisolated static func dotnetRootPath() -> String {
        if let path = ProcessInfo.processInfo.environment["DOTNET_ROOT"], !path.isEmpty {
            return path
        }
        let candidates = [
            gameEngineDirectory() + "/dotnet",
            NSHomeDirectory() + "/.dotnet",
            "/usr/local/share/dotnet",
        ]
        if let hit = candidates.first(where: { FileManager.default.fileExists(atPath: $0 + "/dotnet") }) {
            return hit
        }
        return candidates[0]
    }

    static func supportDirectory() throws -> URL {
        let base = try FileManager.default.url(for: .applicationSupportDirectory, in: .userDomainMask,
                                               appropriateFor: nil, create: true)
        let directory = base.appendingPathComponent("sunGame/Streaming", isDirectory: true)
        try FileManager.default.createDirectory(at: directory, withIntermediateDirectories: true)
        return directory
    }

    /// 取得本机用于展示给 Moonlight 的 IPv4 地址
    static func primaryIPv4Address() -> String {
        var address = "127.0.0.1"
        var ifaddr: UnsafeMutablePointer<ifaddrs>?
        guard getifaddrs(&ifaddr) == 0, let first = ifaddr else { return address }
        defer { freeifaddrs(ifaddr) }

        var pointer: UnsafeMutablePointer<ifaddrs>? = first
        while let current = pointer {
            let interface = current.pointee
            if interface.ifa_addr.pointee.sa_family == UInt8(AF_INET) {
                let name = String(cString: interface.ifa_name)
                if name.hasPrefix("en") || name.hasPrefix("bridge") {
                    var host = [CChar](repeating: 0, count: Int(NI_MAXHOST))
                    getnameinfo(interface.ifa_addr, socklen_t(interface.ifa_addr.pointee.sa_len),
                                &host, socklen_t(host.count), nil, 0, NI_NUMERICHOST)
                    let candidate = String(cString: host)
                    if !candidate.hasPrefix("169.254.") {
                        address = candidate
                        if name == "en0" { break }
                    }
                }
            }
            pointer = interface.ifa_next
        }
        return address
    }
}

// MARK: - RTSP 委托

extension MLStreamingHost: MLRTSPServerDelegate {
    nonisolated func rtspServer(_ server: MLRTSPServer, didNegotiate media: MLMediaNegotiation) {
        Task { @MainActor in
            self.log("收到 ANNOUNCE：\(media.width)×\(media.height)@\(media.fps)，包大小 \(media.packetSize)")
            self.startCapture(media: media)
        }
    }

    nonisolated func rtspServerDidPlay(_ server: MLRTSPServer) {
        Task { @MainActor in
            self.log("客户端已开始播放（PLAY）")
        }
    }

    nonisolated func rtspServerDidTearDown(_ server: MLRTSPServer) {
        Task { @MainActor in
            self.endSession(reason: "RTSP 会话结束")
        }
    }
}

// MARK: - 控制通道委托

extension MLStreamingHost: MLControlStreamDelegate {
    nonisolated func controlStreamDidRequestIDR(_ stream: MLControlStream) {
        Task { @MainActor in
            // 两种模式都没有采集管线，画面由 PS5 / 游戏会话产出；请求必须转给对应会话，
            // 否则客户端要 IDR 时是静默 no-op（花屏后无法恢复）。
            switch self.shareMode {
            case .ps5:
                self.requestPS5KeyFrame()
            case .game:
                self.gameSession?.requestKeyFrame()
            case nil:
                break
            }
        }
    }

    nonisolated func controlStream(_ stream: MLControlStream, invalidateRefFramesFrom first: Int64, to last: Int64) {
        Task { @MainActor in
            switch self.shareMode {
            case .ps5:
                self.requestPS5KeyFrame()
            case .game:
                self.gameSession?.requestKeyFrame()
            case nil:
                break
            }
        }
    }

    /// 向 PS5 请求关键帧。客户端丢包后会频繁请求（实测可达每秒上百次），
    /// 这里做间隔限制，避免 IDR 风暴。
    private func requestPS5KeyFrame() {
        let now = Date()
        if let last = lastPS5KeyFrameRequestAt, now.timeIntervalSince(last) < 0.5 { return }
        lastPS5KeyFrameRequestAt = now
        ps5Session.requestKeyFrame()
    }

    nonisolated func controlStreamDidDisconnect(_ stream: MLControlStream) {
        Task { @MainActor in
            self.endSession(reason: "控制通道断开")
        }
    }
}
