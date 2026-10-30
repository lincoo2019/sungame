//
//  MoonlightHTTP.swift
//  QuickRecorder
//
//  GameStream 主机接口：
//    HTTP  47989：/serverinfo、/pair（配对全部阶段）
//    HTTPS 47984：/serverinfo、/applist、/launch、/resume、/cancel、/appasset
//
//  配对协议要点（对齐 Sunshine/src/nvhttp.cpp）：
//    * 所有二进制参数均为「标准 hex」（byte0 在前）
//    * PIN 派生密钥 = SHA256(salt16 || PIN_ASCII)[0:16]，AES-128-ECB 无填充
//    * getservercert → clientchallenge → serverchallengeresp → clientpairingsecret
//      → pairchallenge（无条件成功）
//

import Foundation
import Network
import Security

// MARK: - HTTP 报文

struct MLHTTPRequest {
    var method = "GET"
    var target = "/"
    var query: [String: String] = [:]
    var headers: [String: String] = [:] // 小写键
    var body: Data = Data()

    func header(_ name: String) -> String? { headers[name.lowercased()] }
}

struct MLHTTPResponse {
    var status = 200
    var reason = "OK"
    var contentType = "text/xml; charset=utf-8"
    var body = Data()
    var extraHeaders: [(String, String)] = []
}

// MARK: - 服务端

final class MLHTTPServer {
    /// 处理器通过 completion 回传响应，以支持"稍后响应"（配对阶段 1 需要等待用户输入 PIN）
    typealias Handler = (MLHTTPRequest, @escaping (MLHTTPResponse) -> Void) -> Void

    private var listener: NWListener?
    private let queue = DispatchQueue(label: "MLHTTPServer")
    private var routes: [(pattern: NSRegularExpression, handler: Handler)] = []
    private let identity: MLIdentity?
    private let port: Int
    private let logHandler: (String) -> Void

    init(port: Int, identity: MLIdentity?, requireClientCertificate: Bool,
         log: @escaping (String) -> Void) throws {
        self.port = port
        self.identity = identity
        self.logHandler = log

        let parameters: NWParameters
        if requireClientCertificate, let identity = identity {
            let tls = NWProtocolTLS.Options()
            let secOptions = tls.securityProtocolOptions
            if let secIdentity = sec_identity_create(identity.secIdentity) {
                sec_protocol_options_set_local_identity(secOptions, secIdentity)
            }
            sec_protocol_options_set_peer_authentication_required(secOptions, true)
            // 证书校验交给我们自己（与 Sunshine 一致：先握手，再在应用层比对已配对证书）
            sec_protocol_options_set_verify_block(secOptions, { _, _, complete in
                complete(true)
            }, DispatchQueue(label: "MLHTTPServer.tls"))
            parameters = NWParameters(tls: tls, tcp: NWProtocolTCP.Options())
        } else if let identity = identity {
            // 仅需提供证书，不要求客户端证书
            let tls = NWProtocolTLS.Options()
            let secOptions = tls.securityProtocolOptions
            if let secIdentity = sec_identity_create(identity.secIdentity) {
                sec_protocol_options_set_local_identity(secOptions, secIdentity)
            }
            sec_protocol_options_set_peer_authentication_required(secOptions, false)
            parameters = NWParameters(tls: tls, tcp: NWProtocolTCP.Options())
        } else {
            parameters = NWParameters.tcp
        }
        parameters.allowLocalEndpointReuse = true
        parameters.includePeerToPeer = false

        self.parameters = parameters
    }

    private let parameters: NWParameters

    func route(_ pattern: String, handler: @escaping Handler) {
        guard let regex = try? NSRegularExpression(pattern: pattern) else { return }
        routes.append((regex, handler))
    }

    func start() throws {
        let params = parameters
        let listener = try NWListener(using: params, on: NWEndpoint.Port(rawValue: UInt16(port))!)
        listener.newConnectionHandler = { [weak self] connection in
            self?.handle(connection)
        }
        listener.stateUpdateHandler = { [weak self] state in
            if case .failed(let error) = state {
                self?.logHandler("端口 \(self?.port ?? 0) 监听失败：\(error)")
            }
        }
        listener.start(queue: queue)
        self.listener = listener
        logHandler("已监听端口 \(port)")
    }

    func stop() {
        listener?.cancel()
        listener = nil
    }

    // MARK: - 连接处理

    private func handle(_ connection: NWConnection) {
        connection.start(queue: queue)
        receive(connection, buffer: Data())
    }

    private func receive(_ connection: NWConnection, buffer: Data) {
        connection.receive(minimumIncompleteLength: 1, maximumLength: 64 * 1024) { [weak self] data, _, isComplete, error in
            guard let self else { return }
            var accumulated = buffer
            if let data = data { accumulated.append(data) }

            if error != nil {
                connection.cancel()
                return
            }

            if let request = MLHTTPServer.parse(accumulated) {
                self.dispatch(request) { [weak self] response in
                    guard let self else { return }
                    self.queue.async { self.send(connection, response: response) }
                }
                return
            }

            if isComplete {
                connection.cancel()
                return
            }
            if accumulated.count > 4 * 1024 * 1024 {
                connection.cancel()
                return
            }
            self.receive(connection, buffer: accumulated)
        }
    }

    private func send(_ connection: NWConnection, response: MLHTTPResponse) {
        var head = "HTTP/1.1 \(response.status) \(response.reason)\r\n"
        head += "Content-Type: \(response.contentType)\r\n"
        head += "Content-Length: \(response.body.count)\r\n"
        head += "Connection: close\r\n"
        for (k, v) in response.extraHeaders {
            head += "\(k): \(v)\r\n"
        }
        head += "\r\n"

        var out = Data(head.utf8)
        out.append(response.body)
        connection.send(content: out, completion: .contentProcessed { _ in
            connection.cancel()
        })
    }

    private func dispatch(_ request: MLHTTPRequest, completion: @escaping (MLHTTPResponse) -> Void) {
        for (regex, handler) in routes {
            let range = NSRange(request.target.startIndex..., in: request.target)
            if regex.firstMatch(in: request.target, range: range) != nil {
                handler(request, completion)
                return
            }
        }
        completion(MLHTTPResponse(status: 404, reason: "NOT FOUND",
                                  body: Data("<root status_code=\"404\"/>".utf8)))
    }

    // MARK: - 解析

    static func parse(_ data: Data) -> MLHTTPRequest? {
        guard let headerEnd = data.range(of: Data("\r\n\r\n".utf8)) else { return nil }
        guard let headString = String(data: data[..<headerEnd.lowerBound], encoding: .utf8) else { return nil }
        var lines = headString.components(separatedBy: "\r\n")
        guard !lines.isEmpty else { return nil }

        let requestLine = lines.removeFirst().components(separatedBy: " ")
        guard requestLine.count >= 2 else { return nil }

        var request = MLHTTPRequest()
        request.method = requestLine[0]
        let fullTarget = requestLine[1]
        if let qIndex = fullTarget.firstIndex(of: "?") {
            request.target = String(fullTarget[..<qIndex])
            let queryString = String(fullTarget[fullTarget.index(after: qIndex)...])
            request.query = parseQuery(queryString)
        } else {
            request.target = fullTarget
        }

        for line in lines {
            guard let colon = line.firstIndex(of: ":") else { continue }
            let name = String(line[..<colon]).trimmingCharacters(in: .whitespaces).lowercased()
            let value = String(line[line.index(after: colon)...]).trimmingCharacters(in: .whitespaces)
            request.headers[name] = value
        }

        let bodyStart = headerEnd.upperBound
        let contentLength = Int(request.header("content-length") ?? "0") ?? 0
        if contentLength > 0 {
            guard data.count >= bodyStart + contentLength else { return nil }
            request.body = data.subdata(in: bodyStart..<(bodyStart + contentLength))
        }
        return request
    }

    static func parseQuery(_ string: String) -> [String: String] {
        var result: [String: String] = [:]
        for pair in string.split(separator: "&") {
            let kv = pair.split(separator: "=", maxSplits: 1, omittingEmptySubsequences: false)
            guard let key = kv.first else { continue }
            let rawValue = kv.count > 1 ? String(kv[1]) : ""
            let decodedKey = String(key).removingPercentEncoding ?? String(key)
            result[decodedKey] = rawValue.removingPercentEncoding ?? rawValue
        }
        return result
    }
}

// MARK: - 主机状态与配对

/// 一个已配对客户端
struct MLPairedClient: Codable {
    var name: String
    var cert: String
    /// 客户端的 uniqueid（后续请求用于识别已配对设备）
    var uniqueID: String
    var uuid: String
    var enabled: Bool
}

/// 启动 / 恢复会话时由 /launch、/resume 提供的参数
struct MLLaunchSession {
    var appID: String = ""
    var gcmKey: [UInt8] = []
    var riKeyID: UInt32 = 0
    var width: Int = 1920
    var height: Int = 1080
    var fps: Int = 60
    var clientName: String = ""
    var rtspSessionURL: String = ""
    var surroundAudioInfo: Int = 196610
    /// 会话标识（供 RTSP / 控制通道匹配）
    /// 注意：X-SS-Ping-Payload 必须是恰好 16 个可打印字符，客户端会把它们原样放进 SS_PING
    var avPingPayloadVideo: [UInt8] = MLHostState.makePingPayload()
    var avPingPayloadAudio: [UInt8] = MLHostState.makePingPayload()
    var controlConnectData: UInt32 = MLRandom.uint32()
    var launchID: UInt32 = MLRandom.uint32()
}

private enum MLPairPhase {
    case none
    case getServerCert
    case clientChallenge
    case serverChallengeResp
}

private final class MLPairSession {
    var phase: MLPairPhase = .none
    /// 客户端 uniqueid（阶段 4 记录已配对设备时需要）
    var uniqueID = ""
    var clientCertPEM = ""
    var salt16: [UInt8] = []
    var cipherKey: [UInt8] = []
    var serverSecret: [UInt8] = []
    var serverChallenge: [UInt8] = []
    var clientHash: [UInt8] = []
    var deviceName = ""
    var createdAt = Date()
}

/// 主机端状态：证书、已配对客户端、配对会话、启动会话
final class MLHostState {
    let identity: MLIdentity
    private(set) var uniqueID: String
    private(set) var pairedClients: [MLPairedClient] = []
    /// 配对时由用户在界面填入 Moonlight 显示的 4 位 PIN
    private var submittedPIN: String?
    private let pinSignal = DispatchSemaphore(value: 0)
    /// 需要用户输入 PIN 时回调（参数为发起配对的设备名）
    var onPairingPINNeeded: ((String) -> Void)?
    private(set) var launchSession: MLLaunchSession?
    /// 正在运行的会话（供 RTSP 握手使用）
    var pendingLaunch: MLLaunchSession?

    private var pairSessions: [String: MLPairSession] = [:]
    /// 最近一次使用的配对会话键（后续阶段未带 uniqueid 时回退使用）
    private var lastPairSessionKey: String?
    private let stateURL: URL
    private let logHandler: (String) -> Void
    private let lock = NSLock()

    init(directory: URL, log: @escaping (String) -> Void) throws {
        self.identity = try MLIdentity(directory: directory)
        self.stateURL = directory.appendingPathComponent("state.json")
        self.logHandler = log
        self.uniqueID = UUID().uuidString.replacingOccurrences(of: "-", with: "").lowercased()
        loadState()
    }

    /// 生成 8 随机字节的 16 位大写 hex（用作 X-SS-Ping-Payload，客户端要求恰好 16 字节）
    static func makePingPayload() -> [UInt8] {
        Array(MLRandom.bytes(8).mlHexString(uppercase: true).utf8)
    }

    // MARK: 持久化

    private struct PersistedState: Codable {
        var uniqueID: String
        var clients: [MLPairedClient]
    }

    func loadState() {
        guard let data = try? Data(contentsOf: stateURL),
              let state = try? JSONDecoder().decode(PersistedState.self, from: data) else { return }
        uniqueID = state.uniqueID
        pairedClients = state.clients
    }

    func saveState() {
        let state = PersistedState(uniqueID: uniqueID, clients: pairedClients)
        if let data = try? JSONEncoder().encode(state) {
            try? data.write(to: stateURL)
        }
    }

    func addClient(name: String, certPEM: String, uniqueID: String) {
        lock.lock()
        defer { lock.unlock() }
        let canonical = MLX509.canonicalPEM(certPEM) ?? certPEM
        if let index = pairedClients.firstIndex(where: { $0.uniqueID == uniqueID && !uniqueID.isEmpty }) {
            pairedClients[index].name = name
            pairedClients[index].cert = canonical
            pairedClients[index].enabled = true
        } else {
            pairedClients.append(MLPairedClient(name: name, cert: canonical, uniqueID: uniqueID,
                                                uuid: UUID().uuidString.lowercased(), enabled: true))
        }
        saveState()
        logHandler("已配对客户端：\(name)")
    }

    func removeClient(uuid: String) {
        lock.lock()
        pairedClients.removeAll { $0.uuid == uuid }
        lock.unlock()
        saveState()
    }

    func removeAllClients() {
        lock.lock()
        pairedClients.removeAll()
        lock.unlock()
        saveState()
    }

    /// 该 uniqueid 是否为已配对设备
    func isClientPaired(uniqueID: String?) -> Bool {
        guard let uniqueID, !uniqueID.isEmpty else { return false }
        lock.lock()
        defer { lock.unlock() }
        return pairedClients.contains { $0.enabled && $0.uniqueID == uniqueID }
    }

    // MARK: 配对会话

    /// 用户在界面上提交 Moonlight 显示的 PIN
    func submitPIN(_ value: String) {
        let trimmed = value.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmed.isEmpty else { return }
        lock.lock()
        submittedPIN = trimmed
        lock.unlock()
        pinSignal.signal()
        logHandler("配对：已提交 PIN（\(trimmed)）")
    }

    /// 当前是否已有可用 PIN
    var hasPIN: Bool {
        lock.lock(); defer { lock.unlock() }
        return submittedPIN != nil
    }

    /// 等待用户提交 PIN。
    /// Moonlight 对 `phrase=getservercert` 不设超时（timeoutMs = 0），因此可安全等待用户输入。
    fileprivate func waitForPIN(deviceName: String, timeout: TimeInterval) -> String? {
        lock.lock()
        let existing = submittedPIN
        lock.unlock()
        if let existing { return existing }

        onPairingPINNeeded?(deviceName)

        let deadline = Date().addingTimeInterval(timeout)
        while Date() < deadline {
            if pinSignal.wait(timeout: .now() + 0.5) == .success {
                lock.lock()
                let value = submittedPIN
                lock.unlock()
                if let value { return value }
            }
        }
        return nil
    }

    /// 配对流程结束后清除 PIN
    fileprivate func clearPIN() {
        lock.lock()
        submittedPIN = nil
        lock.unlock()
    }

    fileprivate func pairSession(for uniqueID: String) -> MLPairSession {
        lock.lock()
        defer { lock.unlock() }
        if let existing = pairSessions[uniqueID] {
            lastPairSessionKey = uniqueID
            return existing
        }
        let session = MLPairSession()
        session.uniqueID = uniqueID
        pairSessions[uniqueID] = session
        lastPairSessionKey = uniqueID
        return session
    }

    /// 最近一次创建/使用的配对会话（当后续阶段未携带 uniqueid 时使用）
    fileprivate func currentPairSession() -> MLPairSession? {
        lock.lock()
        defer { lock.unlock() }
        guard let key = lastPairSessionKey else { return pairSessions.values.first }
        return pairSessions[key]
    }

    fileprivate func removePairSession(for uniqueID: String) {
        lock.lock()
        pairSessions.removeValue(forKey: uniqueID)
        if lastPairSessionKey == uniqueID { lastPairSessionKey = nil }
        lock.unlock()
    }

    func setLaunchSession(_ session: MLLaunchSession?) {
        lock.lock()
        launchSession = session
        lock.unlock()
    }

    var currentLaunchSession: MLLaunchSession? {
        lock.lock(); defer { lock.unlock() }
        return launchSession
    }
}

// MARK: - 配对与主机接口路由

extension MLHostState {
    /// 生成 /serverinfo 响应
    /// - Parameter clientUniqueID: 请求 query 中的 uniqueid，用于判断该客户端是否已配对
    func serverInfoResponse(https: Bool, clientUniqueID: String?, localIP: String) -> MLHTTPResponse {
        let paired = https && isClientPaired(uniqueID: clientUniqueID)
        let xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <root status_code="200">
        <hostname>\(Host.current().localizedName ?? "sunGame")</hostname>
        <appversion>\(MLVersion.appVersion)</appversion>
        <GfeVersion>\(MLVersion.gfeVersion)</GfeVersion>
        <uniqueid>\(uniqueID)</uniqueid>
        <HttpsPort>\(MLPort.https)</HttpsPort>
        <ExternalPort>\(MLPort.http)</ExternalPort>
        <MaxLumaPixelsHEVC>0</MaxLumaPixelsHEVC>
        <mac>00:00:00:00:00:00</mac>
        <LocalIP>\(localIP)</LocalIP>
        <ServerCodecModeSupport>\(MLCodecMode.h264)</ServerCodecModeSupport>
        <PairStatus>\(paired ? 1 : 0)</PairStatus>
        <currentgame>0</currentgame>
        <state>SUNSHINE_SERVER_FREE</state>
        </root>
        """
        return MLHTTPResponse(body: Data(xml.utf8))
    }

    /// 生成 /applist 响应：（已启用时）PS5 + 游戏
    func appListResponse() -> MLHTTPResponse {
        var body = ""
        // PS5 未启用时不显示条目，避免点了必然失败
        if ChiakiPS5Settings.load().enabled {
            body += """
            <App>
            <IsHdrSupported>0</IsHdrSupported>
            <AppTitle>\(MLAppTitle.ps5)</AppTitle>
            <ID>\(MLAppID.ps5)</ID>
            </App>
            """
        }
        // 游戏：工程目录存在时才显示条目，避免点了必然失败
        if FileManager.default.fileExists(atPath: MLStreamingHost.gameProjectPath()) {
            body += """
            <App>
            <IsHdrSupported>0</IsHdrSupported>
            <AppTitle>\(MLAppTitle.game)</AppTitle>
            <ID>\(MLAppID.game)</ID>
            </App>
            """
        }
        let xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <root status_code="200">
        \(body)
        </root>
        """
        return MLHTTPResponse(body: Data(xml.utf8))
    }

    /// 处理 /pair 的全部阶段。
    /// 阶段 1 需要等待用户输入 Moonlight 显示的 PIN，故使用 completion 形式延迟响应。
    func pairResponse(_ request: MLHTTPRequest, completion: @escaping (MLHTTPResponse) -> Void) {
        let args = request.query
        func fail(_ message: String) -> MLHTTPResponse {
            logHandler("配对失败：\(message)")
            let xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root status_code=\"400\" paired=\"0\" status_message=\"\(message)\"/>"
            return MLHTTPResponse(status: 400, reason: "BAD REQUEST", body: Data(xml.utf8))
        }

        // 收尾握手：无条件成功
        if args["phrase"] == "pairchallenge" {
            logHandler("配对：pairchallenge（收尾）完成")
            completion(pairOK())
            return
        }

        // 后续阶段若未携带 uniqueid，则沿用最近的配对会话（同时只服务一个客户端）
        let session: MLPairSession
        if let uniqueID = args["uniqueid"], !uniqueID.isEmpty {
            session = pairSession(for: uniqueID)
        } else if let existing = currentPairSession() {
            session = existing
        } else {
            completion(fail("Missing uniqueid"))
            return
        }

        if let clientCertHex = args["clientcert"], let saltHex = args["salt"] {
            // 阶段 1：getservercert
            guard let certBytes = clientCertHex.mlHexToBytes(),
                  let certPEM = String(bytes: certBytes, encoding: .utf8) else {
                completion(fail("Invalid client certificate"))
                return
            }
            guard saltHex.count >= 32,
                  let salt = String(saltHex.prefix(32)).mlHexToBytes() else {
                completion(fail("Invalid salt"))
                return
            }

            session.clientCertPEM = certPEM
            session.salt16 = salt
            session.deviceName = args["devicename"] ?? "Moonlight"

            let deviceName = session.deviceName
            let certHex = Array(identity.certificatePEM.utf8).mlHexString()
            logHandler("配对：收到 getservercert（设备「\(deviceName)」），等待输入 Moonlight 显示的 PIN")

            // Moonlight 会显示一个 4 位 PIN 让用户填到主机端；该请求（timeoutMs=0）不会超时，
            // 因此这里可以安全等待用户输入。
            DispatchQueue.global(qos: .userInitiated).async { [weak self] in
                guard let self else { return }
                guard let pin = self.waitForPIN(deviceName: deviceName, timeout: 600) else {
                    completion(fail("等待输入 PIN 超时"))
                    return
                }
                // AES-128 密钥 = SHA256(salt16 || PIN)[0:16]
                session.cipherKey = Array(MLHash.sha256(salt + Array(pin.utf8)).prefix(16))
                session.phase = .getServerCert
                self.logHandler("配对：已用 PIN 派生密钥，阶段 1 响应已返回")

                let xml = """
                <?xml version="1.0" encoding="utf-8"?>
                <root status_code="200">
                <paired>1</paired>
                <plaincert>\(certHex)</plaincert>
                </root>
                """
                completion(MLHTTPResponse(body: Data(xml.utf8)))
            }
            return
        }

        if let challengeHex = args["clientchallenge"] {
            // 阶段 2：clientchallenge
            guard session.phase == .getServerCert else {
                completion(fail("Out of order call to clientchallenge（当前阶段 \(session.phase)）"))
                return
            }
            guard let cipherBytes = challengeHex.mlHexToBytes(),
                  let decrypted = try? MLAESECB.decrypt(key: session.cipherKey, ciphertext: cipherBytes) else {
                completion(fail("Failed to decrypt client challenge"))
                return
            }
            let serverSecret = MLRandom.bytes(16)
            var data = decrypted
            data.append(contentsOf: identity.certificateSignature)
            data.append(contentsOf: serverSecret)
            let hash = MLHash.sha256(data)
            let serverChallenge = MLRandom.bytes(16)
            var plaintext = hash
            plaintext.append(contentsOf: serverChallenge)
            guard let encrypted = try? MLAESECB.encrypt(key: session.cipherKey, plaintext: plaintext) else {
                completion(fail("Failed to encrypt challenge response"))
                return
            }
            session.serverSecret = serverSecret
            session.serverChallenge = serverChallenge
            session.phase = .clientChallenge
            logHandler("配对：clientchallenge 解密成功，已返回 challengeresponse")

            let xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <root status_code="200">
            <paired>1</paired>
            <challengeresponse>\(encrypted.mlHexString())</challengeresponse>
            </root>
            """
            completion(MLHTTPResponse(body: Data(xml.utf8)))
            return
        }

        if let responseHex = args["serverchallengeresp"] {
            // 阶段 3：serverchallengeresp
            guard session.phase == .clientChallenge else {
                completion(fail("Out of order call to serverchallengeresp"))
                return
            }
            guard let cipherBytes = responseHex.mlHexToBytes(),
                  let decrypted = try? MLAESECB.decrypt(key: session.cipherKey, ciphertext: cipherBytes) else {
                completion(fail("Failed to decrypt client hash"))
                return
            }
            session.clientHash = decrypted

            guard let signature = try? identity.sign(sha256: session.serverSecret) else {
                completion(fail("Failed to sign server secret"))
                return
            }
            var pairingSecret = session.serverSecret
            pairingSecret.append(contentsOf: signature)
            session.phase = .serverChallengeResp
            logHandler("配对：已返回 pairingsecret（服务端 RSA 签名 \(signature.count) 字节）")

            let xml = """
            <?xml version="1.0" encoding="utf-8"?>
            <root status_code="200">
            <pairingsecret>\(pairingSecret.mlHexString())</pairingsecret>
            <paired>1</paired>
            </root>
            """
            completion(MLHTTPResponse(body: Data(xml.utf8)))
            return
        }

        if let secretHex = args["clientpairingsecret"] {
            // 阶段 4：最终判定
            guard let raw = secretHex.mlHexToBytes(), raw.count > 16 else {
                completion(fail("Client pairing secret too short"))
                return
            }
            let secret = Array(raw[0..<16])
            let signature = Array(raw[16...])

            guard let clientDER = MLX509.der(fromPEM: session.clientCertPEM) else {
                completion(fail("Invalid client certificate"))
                return
            }
            guard let clientSignature = try? MLX509.rawSignature(der: clientDER) else {
                completion(fail("Invalid client certificate signature"))
                return
            }

            var data = session.serverChallenge
            data.append(contentsOf: clientSignature)
            data.append(contentsOf: secret)
            let hash = MLHash.sha256(data)

            let sameHash = hash == session.clientHash
            let verified = MLIdentity.verify(certificatePEM: session.clientCertPEM,
                                             data: secret, signature: signature)

            if sameHash && verified {
                addClient(name: session.deviceName, certPEM: session.clientCertPEM,
                          uniqueID: session.uniqueID)
                removePairSession(for: session.uniqueID)
                clearPIN()
                logHandler("配对成功：\(session.deviceName)（哈希与签名均校验通过）")
                completion(pairOK())
                return
            }
            logHandler("配对校验失败：hash=\(sameHash) 签名=\(verified)")
            let xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root status_code=\"200\"><paired>0</paired></root>"
            completion(MLHTTPResponse(body: Data(xml.utf8)))
            return
        }

        completion(fail("Missing pairing parameter（收到参数：\(args.keys.sorted().joined(separator: ","))）"))
    }

    private func pairOK() -> MLHTTPResponse {
        let xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root status_code=\"200\"><paired>1</paired></root>"
        return MLHTTPResponse(body: Data(xml.utf8))
    }
}
