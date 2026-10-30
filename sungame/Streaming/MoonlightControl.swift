//
//  MoonlightControl.swift
//  QuickRecorder
//
//  控制通道应用层：SS_ENC_CONTROL_V2（AES-128-GCM）信封的收发与消息分发。
//
//  加密信封（与 moonlight-common-c ControlStream.c 完全一致）：
//    [LE16 0x0001][LE16 length][LE32 seq][tag(16)][ciphertext]
//    length = 4(seq) + 16(tag) + 4(V2 头) + payloadLen
//    V2 明文 = [LE16 type][LE16 payloadLength][payload]
//    IV(12)  = LE32(seq) || 0*6 || 'C','C'（客户端→主机）
//              LE32(seq) || 0*6 || 'H','C'（主机→客户端）
//

import Foundation

protocol MLControlStreamDelegate: AnyObject {
    /// 客户端请求 IDR 帧
    func controlStreamDidRequestIDR(_ stream: MLControlStream)
    /// 客户端上报参考帧失效区间
    func controlStream(_ stream: MLControlStream, invalidateRefFramesFrom first: Int64, to last: Int64)
    /// 控制连接断开
    func controlStreamDidDisconnect(_ stream: MLControlStream)
}

final class MLControlStream: MLENetServerDelegate {
    weak var delegate: MLControlStreamDelegate?

    /// 来自 /launch 的 rikey（AES-128 密钥）
    var gcmKey: [UInt8]?
    /// 是否启用 SS_ENC_CONTROL_V2（客户端 appversion ≥ 7.1.431 时恒为真）
    var encryptionEnabled = true

    private let server: MLENetServer
    private var hostSeq: UInt32 = 0
    private var peer: MLENetPeer?

    /// 期望的 ControlConnectData（来自 RTSP SETUP 响应头）
    var expectedConnectData: UInt32 = 0
    /// 是否已完成会话匹配
    private(set) var sessionMatched = false

    private let logHandler: (String) -> Void

    init(port: Int, log: @escaping (String) -> Void) throws {
        self.logHandler = log
        self.server = try MLENetServer(port: port)
        server.delegate = self
    }

    func start() {
        server.start()
    }

    func stop() {
        server.stop()
    }

    var isConnected: Bool { peer?.isConnected ?? false }

    // MARK: 诊断

    /// 已收到的控制消息类型统计（用于日志排查）
    private(set) var receivedMessageTypes: [UInt16: Int] = [:]
    private var loggedTypeCount = 0

    var statusDescription: String {
        let types = receivedMessageTypes.sorted { $0.key < $1.key }
            .map { String(format: "0x%04X×%d", $0.key, $0.value) }
            .joined(separator: " ")
        return "控制通道状态：已连接=\(isConnected) 收数据报=\(server.datagramsReceived) 发数据报=\(server.datagramsSent) 收到消息=\(types.isEmpty ? "无" : types)"
    }

    func disconnect() {
        if let p = peer {
            server.disconnectPeer(p)
        }
        peer = nil
    }

    // MARK: - 编码 / 发送

    /// 加密并发送一条控制消息（主机 → 客户端）
    func send(type: UInt16, payload: [UInt8]) {
        guard let target = peer, target.isConnected else { return }

        var plaintext = [UInt8]()
        plaintext.mlAppendLE16(type)
        plaintext.mlAppendLE16(UInt16(clamping: payload.count))
        plaintext.append(contentsOf: payload)

        let packet: [UInt8]
        if encryptionEnabled, let key = gcmKey {
            hostSeq &+= 1
            var iv = [UInt8](repeating: 0, count: 12)
            iv[0] = UInt8(hostSeq & 0xFF)
            iv[1] = UInt8((hostSeq >> 8) & 0xFF)
            iv[2] = UInt8((hostSeq >> 16) & 0xFF)
            iv[3] = UInt8((hostSeq >> 24) & 0xFF)
            iv[10] = 0x48 // 'H' Host originated
            iv[11] = 0x43 // 'C' Control stream
            guard let tagged = try? MLAESGCM.seal(key: key, iv: iv, plaintext: plaintext) else {
                logHandler("控制流加密失败")
                return
            }
            var out = [UInt8]()
            out.mlAppendLE16(MLControlType.encrypted)
            out.mlAppendLE16(UInt16(clamping: 4 + 16 + plaintext.count))
            out.mlAppendLE32(hostSeq)
            out.append(contentsOf: tagged)
            packet = out
        } else {
            packet = plaintext
        }

        server.sendReliable(to: target, channel: MLChannel.generic, data: packet)
    }

    /// 通知客户端流已结束
    func sendTermination(reason: UInt32 = 0x8003_0023) {
        var payload = [UInt8]()
        payload.mlAppendBE32(reason)
        send(type: MLControlType.termination, payload: payload)
    }

    /// 发送 HDR 状态（SDR 时 enable=0）
    func sendHDRInfo(enabled: Bool) {
        send(type: MLControlType.hdrInfo, payload: [enabled ? 1 : 0])
    }

    // MARK: - MLENetServerDelegate

    func enetServer(_ server: MLENetServer, didConnect peer: MLENetPeer) {
        // Connect-Data 仅用于在多个并发会话之间区分客户端。
        // 部分 Moonlight 版本不发送该值（恒为 0），若据此拒绝连接，客户端会收到 ENet
        // DISCONNECT 并报 "连接已终止 -1"。单客户端主机无需据此拒绝，故这里只提示。
        if expectedConnectData != 0, peer.connectData != expectedConnectData {
            logHandler("控制通道 Connect-Data 与服务端不一致（收到 \(peer.connectData)，服务端为 \(expectedConnectData)），仍按同一会话接受")
        }
        self.peer = peer
        sessionMatched = true
        logHandler("控制通道已连接：\(peer.remoteDescription)")
    }

    func enetServer(_ server: MLENetServer, didDisconnect peer: MLENetPeer) {
        if self.peer === peer {
            self.peer = nil
            sessionMatched = false
            logHandler("控制通道已断开")
            delegate?.controlStreamDidDisconnect(self)
        }
    }

    func enetServer(_ server: MLENetServer, peer: MLENetPeer, channel: UInt8, didReceiveReliable data: [UInt8]) {
        handlePacket(data, peer: peer)
    }

    func enetServer(_ server: MLENetServer, peer: MLENetPeer, channel: UInt8, didReceiveUnreliable data: [UInt8]) {
        handlePacket(data, peer: peer)
    }

    // MARK: - 解码

    private func handlePacket(_ data: [UInt8], peer: MLENetPeer) {
        guard data.count >= 2 else { return }
        let outerType = data.mlLE16(0)

        var plaintext: [UInt8]
        /// 载荷起始偏移：V2 加密包为 4（type + payloadLength），V1 明文包为 2（仅 type）
        var payloadOffset = 2
        if outerType == MLControlType.encrypted {
            // 加密信封
            guard data.count >= 24 else { return }
            let length = Int(data.mlLE16(2))
            let seq = data.mlLE32(4)
            let cipherLen = length - 20
            guard cipherLen > 0, 8 + 16 + cipherLen <= data.count else { return }
            let tagged = Array(data[8..<(8 + 16 + cipherLen)])

            guard encryptionEnabled, let key = gcmKey else { return }
            var iv = [UInt8](repeating: 0, count: 12)
            iv[0] = UInt8(seq & 0xFF)
            iv[1] = UInt8((seq >> 8) & 0xFF)
            iv[2] = UInt8((seq >> 16) & 0xFF)
            iv[3] = UInt8((seq >> 24) & 0xFF)
            iv[10] = 0x43 // 'C' Client originated
            iv[11] = 0x43 // 'C' Control stream
            guard let opened = try? MLAESGCM.open(key: key, iv: iv, taggedCipher: tagged) else {
                logHandler("控制包解密失败（seq \(seq)）")
                return
            }
            plaintext = opened
            payloadOffset = 4
        } else {
            // 客户端在 ≥7.1.431 一定使用加密信封；明文仅用于兼容
            plaintext = data
        }

        guard plaintext.count >= payloadOffset else { return }
        let type = plaintext.mlLE16(0)
        let payload = plaintext.count > payloadOffset ? Array(plaintext[payloadOffset...]) : []

        dispatch(type: type, payload: payload)
    }

    private func dispatch(type: UInt16, payload: [UInt8]) {
        receivedMessageTypes[type, default: 0] += 1
        if loggedTypeCount < 5 {
            loggedTypeCount += 1
            logHandler("收到控制消息 #\(loggedTypeCount)：type=0x\(String(format: "%04X", type))，载荷 \(payload.count) 字节")
        }
        switch type {
        case MLControlType.inputData:
            // V2 明文里 payload 就是单个输入包（NV_INPUT_HEADER + 包体）
            handleInputPacket(payload)

        case MLControlTypeCompat.periodicPing, MLControlTypeCompat.startA, MLControlType.startB:
            break // 保活/启动握手，无需回应

        case MLControlType.requestIDRFrame:
            delegate?.controlStreamDidRequestIDR(self)

        case MLControlType.invalidateRefFrames:
            guard payload.count >= 16 else { return }
            let first = Int64(bitPattern: UInt64(payload.mlLE32(0)) | (UInt64(payload.mlLE32(4)) << 32))
            let last = Int64(bitPattern: UInt64(payload.mlLE32(8)) | (UInt64(payload.mlLE32(12)) << 32))
            delegate?.controlStream(self, invalidateRefFramesFrom: first, to: last)

        case MLControlType.lossStats:
            break // 仅统计

        default:
            break
        }
    }

    /// 一个输入数据消息里可能打包多个输入包（客户端会把同一批次的事件合并发送），
    /// 必须逐个解出来，否则除了第一个之外的鼠标移动等事件都会被丢掉（表现为指针跳变）。
    private func handleInputPacket(_ payload: [UInt8]) {
        var offset = 0
        while offset + 8 <= payload.count {
            // 每个包：size(BE32，= 结构体总长 - 4) + magic(LE32) + 包体
            let declaredSize = Int(payload.mlBE32(offset))
            let magic = payload.mlLE32(offset + 4)
            let packetLength = declaredSize + 4
            guard packetLength >= 8, offset + packetLength <= payload.count else { break }
            let body = Array(payload[(offset + 8)..<(offset + packetLength)])
            MLInputInjector.shared.handle(magic: magic, body: body)
            offset += packetLength
        }
    }
}
