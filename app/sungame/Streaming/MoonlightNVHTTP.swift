//
//  MoonlightNVHTTP.swift
//  QuickRecorder
//
//  /launch、/resume、/cancel 接口实现（HTTPS 47984）。
//
//  请求参数（对齐 Sunshine/src/nvhttp.cpp）：
//    /launch  必填 rikey（标准 hex，byte0 在前）、rikeyid、localAudioPlayMode、appid
//             可选 mode=WxHxFPS、uniqueid、surroundAudioInfo、hdrMode、corever
//    /resume  必填 rikey、rikeyid
//  响应：
//    /launch  <sessionUrl0>rtsp://ip:48010</sessionUrl0><gamesession>1</gamesession>
//    /resume  <sessionUrl0>…</sessionUrl0><resume>1</resume>
//

import Foundation

extension MLHostState {

    func launchResponse(_ request: MLHTTPRequest, localIP: String,
                        onSessionStarted: @escaping (MLLaunchSession) -> Void) -> MLHTTPResponse {
        let args = request.query
        for required in ["rikey", "rikeyid", "localAudioPlayMode", "appid"] {
            guard args[required] != nil else {
                return Self.errorXML(status: 400, reason: "BAD REQUEST",
                                     message: "Missing a required launch parameter",
                                     extra: "<gamesession>0</gamesession>")
            }
        }
        guard let key = args["rikey"]?.mlHexToBytes(), key.count == 16 else {
            return Self.errorXML(status: 400, reason: "BAD REQUEST",
                                 message: "Invalid rikey",
                                 extra: "<gamesession>0</gamesession>")
        }

        var session = MLLaunchSession()
        session.appID = args["appid"] ?? "1"
        session.gcmKey = key
        session.riKeyID = Self.parseRiKeyID(args["rikeyid"])
        session.clientName = args["uniqueid"] ?? "Moonlight"
        if let mode = args["mode"] {
            let parts = mode.components(separatedBy: "x")
            if parts.count == 3 {
                if let w = Int(parts[0]), w > 0 { session.width = w }
                if let h = Int(parts[1]), h > 0 { session.height = h }
                if let f = Int(parts[2]), f > 0 { session.fps = f }
            }
        }
        if let surround = args["surroundAudioInfo"], let v = Int(surround) { session.surroundAudioInfo = v }
        session.rtspSessionURL = "rtsp://\(localIP):\(MLPort.rtsp)"

        setLaunchSession(session)
        onSessionStarted(session)

        let xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <root status_code="200">
        <sessionUrl0>\(session.rtspSessionURL)</sessionUrl0>
        <gamesession>1</gamesession>
        </root>
        """
        return MLHTTPResponse(body: Data(xml.utf8))
    }

    func resumeResponse(_ request: MLHTTPRequest, localIP: String,
                        onSessionResumed: @escaping (MLLaunchSession) -> Void) -> MLHTTPResponse {
        let args = request.query
        guard let existing = currentLaunchSession else {
            return Self.errorXML(status: 503, reason: "SERVICE UNAVAILABLE",
                                 message: "No running app to resume",
                                 extra: "<resume>0</resume>")
        }
        for required in ["rikey", "rikeyid"] {
            guard args[required] != nil else {
                return Self.errorXML(status: 400, reason: "BAD REQUEST",
                                     message: "Missing a required resume parameter",
                                     extra: "<resume>0</resume>")
            }
        }
        guard let key = args["rikey"]?.mlHexToBytes(), key.count == 16 else {
            return Self.errorXML(status: 400, reason: "BAD REQUEST",
                                 message: "Invalid rikey",
                                 extra: "<resume>0</resume>")
        }

        var session = existing
        session.gcmKey = key
        session.riKeyID = Self.parseRiKeyID(args["rikeyid"])
        session.avPingPayloadVideo = MLHostState.makePingPayload()
        session.avPingPayloadAudio = MLHostState.makePingPayload()
        session.controlConnectData = MLRandom.uint32()
        session.rtspSessionURL = "rtsp://\(localIP):\(MLPort.rtsp)"
        setLaunchSession(session)
        onSessionResumed(session)

        let xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <root status_code="200">
        <sessionUrl0>\(session.rtspSessionURL)</sessionUrl0>
        <resume>1</resume>
        </root>
        """
        return MLHTTPResponse(body: Data(xml.utf8))
    }

    func cancelResponse(_ request: MLHTTPRequest) -> MLHTTPResponse {
        setLaunchSession(nil)
        let xml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<root status_code=\"200\"><cancel>1</cancel></root>"
        return MLHTTPResponse(body: Data(xml.utf8))
    }

    /// 解析 /launch、/resume 的 rikeyid。
    ///
    /// 客户端（Moonlight iOS StreamManager）把 `arc4random()` 的 uint32 存进 `int` 属性，
    /// 再用 `%d` 拼进 URL，因此 rikeyid 约有一半概率是**负数**（例如 `-1234567890`）。
    /// 该值最终作为音频加密 IV 的高 4 字节（`BE32(riKeyId + seq)`，见 AudioStream.c:188），
    /// 若按无符号解析会失败并退化成 0 → IV 与客户端不一致 → AES-CBC 解出乱码、完全静音。
    ///
    /// 因此这里与 Sunshine 保持一致：按**有符号 int** 解析后取位模式
    /// （Sunshine `nvhttp.cpp`：`(uint32_t) big<uint32_t>((int) from_view(args["rikeyid"]))`）。
    static func parseRiKeyID(_ value: String?) -> UInt32 {
        guard let value = value?.trimmingCharacters(in: .whitespaces), !value.isEmpty else { return 0 }
        // 负数/正数统一按 Int32 位模式处理（Int32 覆盖 int 属性的全部取值）
        if let signed = Int32(value) { return UInt32(bitPattern: signed) }
        // 兜底：超出 Int32 范围或非十进制时，尽量按原始位模式解释
        if let signed64 = Int64(value) { return UInt32(truncatingIfNeeded: signed64) }
        if value.hasPrefix("0x"), let hex = UInt32(value.dropFirst(2), radix: 16) { return hex }
        return UInt32(value) ?? 0
    }

    static func errorXML(status: Int, reason: String, message: String, extra: String = "") -> MLHTTPResponse {
        let xml = """
        <?xml version="1.0" encoding="utf-8"?>
        <root status_code="\(status)" status_message="\(message)">
        \(extra)
        </root>
        """
        return MLHTTPResponse(status: status, reason: reason, body: Data(xml.utf8))
    }
}
