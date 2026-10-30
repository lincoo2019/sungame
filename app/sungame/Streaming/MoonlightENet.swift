//
//  MoonlightENet.swift
//  QuickRecorder
//
//  ENet 协议服务端实现（Moonlight 控制通道 / UDP 47999）。
//
//  客户端使用 cgutman/enet（moonlight 分支），建链参数：
//    enet_host_create(family, NULL, 1, CTRL_CHANNEL_COUNT=48, 0, 0)
//    enet_host_connect(host, addr, 48, ControlConnectData)
//  本实现按 ENet 1.3.x 线格式复刻，仅实现服务端必需子集：
//    CONNECT / VERIFY_CONNECT / ACKNOWLEDGE / PING / SEND_RELIABLE /
//    SEND_UNRELIABLE / SEND_UNSEQUENCED / SEND_FRAGMENT / DISCONNECT /
//    BANDWIDTH_LIMIT / THROTTLE_CONFIGURE
//  未启用 CRC 与压缩（与 moonlight 客户端一致）。
//

import Foundation

// MARK: - 协议常量

enum MLENetConst {
    static let minimumMTU = 576
    static let maximumMTU = 4096
    static let hostDefaultMTU = 900
    static let minimumWindowSize = 4096
    static let maximumWindowSize = 65536
    static let maximumPeerID: UInt16 = 0x0FFF

    static let headerFlagSentTime: UInt16 = 0x8000
    static let headerFlagCompressed: UInt16 = 0x4000
    static let headerSessionMask: UInt16 = 0x3000
    static let headerSessionShift: UInt16 = 12

    static let commandMask: UInt8 = 0x0F
    static let commandFlagAcknowledge: UInt8 = 0x80
    static let commandFlagUnsequenced: UInt8 = 0x40

    static let cmdAcknowledge: UInt8 = 1
    static let cmdConnect: UInt8 = 2
    static let cmdVerifyConnect: UInt8 = 3
    static let cmdDisconnect: UInt8 = 4
    static let cmdPing: UInt8 = 5
    static let cmdSendReliable: UInt8 = 6
    static let cmdSendUnreliable: UInt8 = 7
    static let cmdSendFragment: UInt8 = 8
    static let cmdSendUnsequenced: UInt8 = 9
    static let cmdBandwidthLimit: UInt8 = 10
    static let cmdThrottleConfigure: UInt8 = 11
    static let cmdSendUnreliableFragment: UInt8 = 12

    /// 各命令占用的字节数（索引为命令号）
    /// 必须与 enet 的 sizeof(ENetProtocol*) 一致（结构体为 ENET_PACKED）：
    ///   CONNECT = 4(头) + 2 + 1 + 1 + 10×4 = 48
    ///   VERIFY_CONNECT = 4 + 2 + 1 + 1 + 9×4 = 44
    static let commandSizes: [Int] = [0, 8, 48, 44, 8, 4, 6, 8, 24, 8, 12, 16, 24]

    static let pingInterval: TimeInterval = 0.5
    static let retransmitInterval: TimeInterval = 0.25
    static let peerTimeout: TimeInterval = 10.0

    /// 默认节流参数（客户端会原样校验 VERIFY_CONNECT 中回显值）
    static let throttleInterval: UInt32 = 5000
    static let throttleAcceleration: UInt32 = 2
    static let throttleDeceleration: UInt32 = 2
}

// MARK: - ENet 服务端

protocol MLENetServerDelegate: AnyObject {
    /// 新客户端完成握手（connectData 为 CONNECT 命令携带的 ControlConnectData）
    func enetServer(_ server: MLENetServer, didConnect peer: MLENetPeer)
    func enetServer(_ server: MLENetServer, didDisconnect peer: MLENetPeer)
    /// 收到可靠控制消息
    func enetServer(_ server: MLENetServer, peer: MLENetPeer, channel: UInt8, didReceiveReliable data: [UInt8])
    /// 收到不可靠控制消息
    func enetServer(_ server: MLENetServer, peer: MLENetPeer, channel: UInt8, didReceiveUnreliable data: [UInt8])
}

/// ENet 通道状态
struct MLENetChannel {
    var outgoingReliableSeq: UInt16 = 0
    var outgoingUnreliableSeq: UInt16 = 0
    var incomingReliableSeq: UInt16 = 0
    var incomingUnreliableSeq: UInt16 = 0
}

enum MLENetPeerState {
    case disconnected
    case connected
}

/// 已发送但未确认的可靠命令
private struct MLENetUnacked {
    var channel: UInt8
    var seq: UInt16
    var datagram: [UInt8]
    var sentAt: TimeInterval
    var attempts: Int
}

final class MLENetPeer {
    fileprivate let slot: UInt16
    fileprivate var state: MLENetPeerState = .disconnected
    fileprivate var address: MLSocketAddress?
    fileprivate var channelCount = 0
    fileprivate var mtu = MLENetConst.hostDefaultMTU
    fileprivate var incomingSessionID: UInt8 = 0xFF
    fileprivate var outgoingSessionID: UInt8 = 0xFF
    /// 放进出站报文头的 peerID（= 客户端 CONNECT 携带的 outgoingPeerID）
    fileprivate var clientPeerID: UInt16 = MLENetConst.maximumPeerID
    /// 客户端在我们这里的 ID（= 本 peer 槽位；出站报文头使用 clientPeerID）
    fileprivate var channels: [MLENetChannel] = []
    fileprivate var unacked: [MLENetUnacked] = []
    /// channelID == 0xFF 的控制命令使用 peer 级可靠序号
    fileprivate var peerLevelSeq: UInt16 = 0
    fileprivate var lastReceive: TimeInterval = 0
    fileprivate var lastSend: TimeInterval = 0
    /// CONNECT 携带的用户数据（ControlConnectData）
    fileprivate(set) var connectData: UInt32 = 0
    fileprivate var sendQueue: [[UInt8]] = []

    var isConnected: Bool { state == .connected }
    var remoteDescription: String { address?.description ?? "unknown" }
    var remoteAddress: MLSocketAddress? { address }

    fileprivate init(slot: UInt16) { self.slot = slot }
}

final class MLENetServer {
    weak var delegate: MLENetServerDelegate?

    private let socket: MLUDPSocket
    private var peers: [MLENetPeer]
    private var thread: Thread?
    private var running = false
    private let startTime = Date()

    private let lock = NSLock()
    /// 跨线程待发送队列（由 ENet 线程统一排出，避免状态竞争）
    private var pendingOutbound: [(peer: MLENetPeer, channel: UInt8, data: [UInt8], reliable: Bool)] = []
    private var pendingDisconnect: [MLENetPeer] = []
    private var publishedPeers: [MLENetPeer] = []

    // MARK: 诊断计数（仅用于日志展示，读写不加锁）
    private(set) var datagramsReceived = 0
    private(set) var datagramsSent = 0
    private(set) var lastSenderDescription = ""

    private(set) var port: Int

    init(port: Int, maximumPeers: Int = 4) throws {
        self.port = port
        self.socket = try MLUDPSocket(port: port)
        self.peers = (0..<maximumPeers).map { MLENetPeer(slot: UInt16($0)) }
    }

    deinit { stop() }

    func start() {
        guard !running else { return }
        running = true
        let t = Thread { [weak self] in self?.runLoop() }
        t.name = "MLENetServer"
        t.stackSize = 512 * 1024
        thread = t
        t.start()
    }

    func stop() {
        running = false
        socket.close()
        thread = nil
    }

    // MARK: - 循环

    private func runLoop() {
        while running {
            if let packet = socket.receive() {
                handleDatagram(packet.data, from: packet.from)
            }
            drainOutbound()
            tick()
            // 发布连接快照供外部线程读取
            let snapshot = peers.filter { $0.state == .connected }
            lock.lock()
            publishedPeers = snapshot
            lock.unlock()
        }
    }

    private func drainOutbound() {
        lock.lock()
        let outbound = pendingOutbound
        let disconnects = pendingDisconnect
        pendingOutbound.removeAll()
        pendingDisconnect.removeAll()
        lock.unlock()

        for item in outbound {
            guard item.peer.state == .connected else { continue }
            if item.reliable {
                sendReliableNow(to: item.peer, channel: item.channel, data: item.data)
            } else {
                sendUnreliableNow(to: item.peer, channel: item.channel, data: item.data)
            }
        }
        for peer in disconnects where peer.state == .connected {
            disconnectPeerNow(peer)
        }
    }

    private func now() -> TimeInterval {
        Date().timeIntervalSince(startTime)
    }

    /// 重传未确认命令、发送空闲 PING、清理超时 peer（仅在 ENet 线程调用）
    private func tick() {
        let t = now()
        for peer in peers where peer.state == .connected {
            if !peer.unacked.isEmpty {
                for i in peer.unacked.indices {
                    if t - peer.unacked[i].sentAt >= MLENetConst.retransmitInterval {
                        peer.unacked[i].sentAt = t
                        peer.unacked[i].attempts += 1
                        if let address = peer.address {
                            sendDatagram(peer.unacked[i].datagram, to: address)
                        }
                    }
                }
                peer.unacked.removeAll { $0.attempts > 40 }
            }
            // 空闲 500ms 主动发 PING（保持 RTT 与对端存活判定）
            if t - peer.lastSend >= MLENetConst.pingInterval, peer.address != nil {
                sendPing(peer)
            }
            if t - peer.lastReceive > MLENetConst.peerTimeout {
                disconnect(peer, notify: true)
            }
        }
    }

    // MARK: - 收包

    private func handleDatagram(_ data: [UInt8], from: MLSocketAddress) {
        datagramsReceived += 1
        lastSenderDescription = from.description
        guard data.count >= 4 else { return }
        let rawPeerID = data.mlBE16(0)
        let sessionID = UInt8((rawPeerID & MLENetConst.headerSessionMask) >> MLENetConst.headerSessionShift)
        let flags = rawPeerID & 0xC000
        let peerID = rawPeerID & MLENetConst.maximumPeerID

        // 未启用压缩与 CRC
        guard flags & MLENetConst.headerFlagCompressed == 0 else { return }
        let headerSize = (flags & MLENetConst.headerFlagSentTime) != 0 ? 4 : 2
        guard data.count >= headerSize else { return }
        let sentTime: UInt16 = headerSize == 4 ? data.mlBE16(2) : 0

        var peer: MLENetPeer?
        if peerID != MLENetConst.maximumPeerID {
            guard Int(peerID) < peers.count else { return }
            let candidate = peers[Int(peerID)]
            if candidate.state != .disconnected {
                // 会话号必须匹配
                if candidate.clientPeerID != MLENetConst.maximumPeerID && sessionID != candidate.incomingSessionID {
                    return
                }
                peer = candidate
            }
        }

        if let p = peer {
            p.lastReceive = now()
            p.address = from
        }

        var offset = headerSize
        // 注意：SEND_RELIABLE / SEND_UNRELIABLE / SEND_UNSEQUENCED / 分片 命令后面还跟着
        // 负载，必须按「命令头 + 负载」推进，否则负载会被当作命令解析（ENet 自身
        // 的解析器在 handle_* 里推进 payload 后再累加 commandSize）。
        commandLoop: while offset + 4 <= data.count {
            let commandByte = data[offset]
            let command = commandByte & MLENetConst.commandMask
            let channelID = data[offset + 1]
            let reliableSeq = data.mlBE16(offset + 2)
            let size = Int(command) < MLENetConst.commandSizes.count ? MLENetConst.commandSizes[Int(command)] : 0
            guard size > 0, offset + size <= data.count else { break }

            var advance = size

            switch command {
            case MLENetConst.cmdConnect:
                if let newPeer = handleConnect(data, offset: offset, sentTime: sentTime, from: from) {
                    peer = newPeer
                }

            case MLENetConst.cmdAcknowledge:
                if let p = peer {
                    let ackedSeq = data.mlBE16(offset + 4)
                    p.unacked.removeAll { $0.channel == channelID && $0.seq == ackedSeq }
                }

            case MLENetConst.cmdPing:
                if let p = peer {
                    sendAcknowledge(p, channelID: channelID, reliableSeq: reliableSeq, sentTime: sentTime)
                }

            case MLENetConst.cmdSendReliable:
                let length = Int(data.mlBE16(offset + 4))
                let bodyStart = offset + 6
                guard bodyStart + length <= data.count else { break commandLoop }
                advance = 6 + length
                if let p = peer, p.state == .connected {
                    sendAcknowledge(p, channelID: channelID, reliableSeq: reliableSeq, sentTime: sentTime)
                    if Int(channelID) < p.channelCount {
                        p.channels[Int(channelID)].incomingReliableSeq = reliableSeq
                    }
                    delegate?.enetServer(self, peer: p, channel: channelID,
                                         didReceiveReliable: Array(data[bodyStart..<(bodyStart + length)]))
                }

            case MLENetConst.cmdSendUnreliable, MLENetConst.cmdSendUnsequenced:
                let length = Int(data.mlBE16(offset + 6))
                let bodyStart = offset + 8
                guard bodyStart + length <= data.count else { break commandLoop }
                advance = 8 + length
                if let p = peer, p.state == .connected {
                    delegate?.enetServer(self, peer: p, channel: channelID,
                                         didReceiveUnreliable: Array(data[bodyStart..<(bodyStart + length)]))
                }

            case MLENetConst.cmdSendFragment, MLENetConst.cmdSendUnreliableFragment:
                // 控制与输入包远小于 MTU(900)，正常不会出现分片；此处仅应答以维持序号状态
                let length = Int(data.mlBE16(offset + 6))
                guard offset + size + length <= data.count else { break commandLoop }
                advance = 24 + length
                if let p = peer, commandByte & MLENetConst.commandFlagAcknowledge != 0 {
                    sendAcknowledge(p, channelID: channelID, reliableSeq: reliableSeq, sentTime: sentTime)
                }

            case MLENetConst.cmdDisconnect:
                if let p = peer {
                    if commandByte & MLENetConst.commandFlagAcknowledge != 0 {
                        sendAcknowledge(p, channelID: channelID, reliableSeq: reliableSeq, sentTime: sentTime)
                    }
                    disconnect(p, notify: false)
                    peer = nil
                }

            case MLENetConst.cmdBandwidthLimit, MLENetConst.cmdThrottleConfigure:
                break

            default:
                break
            }

            offset += advance
        }
    }

    // MARK: - CONNECT

    private func handleConnect(_ data: [UInt8], offset: Int, sentTime: UInt16,
                              from: MLSocketAddress) -> MLENetPeer? {
        let channelCount = Int(data.mlBE32(offset + 16))
        guard channelCount >= 1, channelCount <= 255 else { return nil }

        // 找出空闲槽位（与 ENet 一致：首个 DISCONNECTED 槽位）
        guard let peer = peers.first(where: { $0.state == .disconnected }) else { return nil }

        let clientOutgoingPeerID = data.mlBE16(offset + 4)
        let connectIncomingSessionID = data[offset + 6]
        let connectOutgoingSessionID = data[offset + 7]
        let connectMTU = data.mlBE32(offset + 8)
        let connectIDBytes = Array(data[(offset + 40)..<(offset + 44)])

        peer.channels = Array(repeating: MLENetChannel(), count: channelCount)
        peer.channelCount = channelCount
        peer.state = .connected
        peer.address = from
        let clampedMTU = Swift.max(MLENetConst.minimumMTU, Swift.min(Int(connectMTU), MLENetConst.maximumMTU))
        peer.mtu = Swift.min(MLENetConst.hostDefaultMTU, clampedMTU)
        peer.clientPeerID = clientOutgoingPeerID
        peer.connectData = data.mlBE32(offset + 44)
        peer.unacked.removeAll()
        peer.peerLevelSeq = 0
        peer.lastReceive = now()
        peer.lastSend = 0

        // 会话号推导（与 enet_protocol_handle_connect 一致）
        var incomingSessionID = connectIncomingSessionID == 0xFF ? peer.outgoingSessionID : connectIncomingSessionID
        incomingSessionID = (incomingSessionID &+ 1) & 0x03
        if incomingSessionID == peer.outgoingSessionID { incomingSessionID = (incomingSessionID &+ 1) & 0x03 }
        peer.outgoingSessionID = incomingSessionID

        var outgoingSessionID = connectOutgoingSessionID == 0xFF ? peer.incomingSessionID : connectOutgoingSessionID
        outgoingSessionID = (outgoingSessionID &+ 1) & 0x03
        if outgoingSessionID == peer.incomingSessionID { outgoingSessionID = (outgoingSessionID &+ 1) & 0x03 }
        peer.incomingSessionID = outgoingSessionID

        // 组装 VERIFY_CONNECT（0x83，channelID=0xFF）
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdVerifyConnect | MLENetConst.commandFlagAcknowledge)
        cmd.append(0xFF)
        cmd.append(0) // reliableSequenceNumber 占位，稍后填充
        cmd.append(0)
        cmd.append(UInt8(peer.slot >> 8)); cmd.append(UInt8(peer.slot & 0xFF))
        cmd.append(incomingSessionID)
        cmd.append(outgoingSessionID)
        cmd.mlAppendBE32(UInt32(peer.mtu))
        cmd.mlAppendBE32(UInt32(MLENetConst.maximumWindowSize))
        cmd.mlAppendBE32(UInt32(channelCount))
        cmd.mlAppendBE32(0) // incomingBandwidth
        cmd.mlAppendBE32(0) // outgoingBandwidth
        cmd.mlAppendBE32(MLENetConst.throttleInterval)
        cmd.mlAppendBE32(MLENetConst.throttleAcceleration)
        cmd.mlAppendBE32(MLENetConst.throttleDeceleration)
        cmd.append(contentsOf: connectIDBytes) // connectID 原样回显，不做字节序转换

        sendReliableRaw(peer, channelID: 0xFF, command: cmd)

        // 对 CONNECT 本身单独回 ACK（客户端 CONNECT 带 ACK 标志）
        sendAcknowledge(peer, channelID: 0xFF, reliableSeq: data.mlBE16(offset + 2), sentTime: sentTime)

        let connected = peer
        DispatchQueue.main.async { [weak self] in
            guard let self else { return }
            self.delegate?.enetServer(self, didConnect: connected)
        }
        return peer
    }

    private func disconnect(_ peer: MLENetPeer, notify: Bool) {
        peer.state = .disconnected
        peer.unacked.removeAll()
        peer.channels.removeAll()
        peer.channelCount = 0
        peer.address = nil
        peer.clientPeerID = MLENetConst.maximumPeerID
        peer.incomingSessionID = 0xFF
        peer.outgoingSessionID = 0xFF

        if notify {
            DispatchQueue.main.async { [weak self] in
                guard let self else { return }
                self.delegate?.enetServer(self, didDisconnect: peer)
            }
        }
    }

    // MARK: - 发送

    /// 组装数据报头：peerID | SENT_TIME | (sessionID << 12)
    private func wrap(peer: MLENetPeer, payload: [UInt8]) -> [UInt8] {
        var header: UInt16 = peer.clientPeerID & MLENetConst.maximumPeerID
        header |= MLENetConst.headerFlagSentTime
        header |= UInt16(peer.outgoingSessionID & 0x03) << MLENetConst.headerSessionShift

        var out = [UInt8]()
        out.append(UInt8(header >> 8))
        out.append(UInt8(header & 0xFF))
        let sentTime = UInt16(truncatingIfNeeded: Int(now() * 1000))
        out.append(UInt8(sentTime >> 8))
        out.append(UInt8(sentTime & 0xFF))
        out.append(contentsOf: payload)
        return out
    }

    /// 发送带 ACK 标志的可靠命令（channelID == 0xFF 使用 peer 级序号）
    private func sendReliableRaw(_ peer: MLENetPeer, channelID: UInt8, command: [UInt8]) {
        var body = command
        let seq: UInt16
        if channelID == 0xFF {
            peer.peerLevelSeq = peer.peerLevelSeq &+ 1
            seq = peer.peerLevelSeq
        } else {
            let idx = Int(channelID)
            guard idx < peer.channels.count else { return }
            peer.channels[idx].outgoingReliableSeq = peer.channels[idx].outgoingReliableSeq &+ 1
            seq = peer.channels[idx].outgoingReliableSeq
        }
        body[2] = UInt8(seq >> 8)
        body[3] = UInt8(seq & 0xFF)

        guard let address = peer.address else { return }
        let datagram = wrap(peer: peer, payload: body)
        peer.unacked.append(MLENetUnacked(channel: channelID, seq: seq, datagram: datagram,
                                          sentAt: now(), attempts: 0))
        sendDatagram(datagram, to: address)
        peer.lastSend = now()
    }

    private func sendAcknowledge(_ peer: MLENetPeer, channelID: UInt8, reliableSeq: UInt16, sentTime: UInt16) {
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdAcknowledge)
        cmd.append(channelID)
        cmd.append(0); cmd.append(0) // ACK 命令自身不参与可靠序号
        cmd.mlAppendBE16(reliableSeq)
        cmd.mlAppendBE16(sentTime)
        guard let address = peer.address else { return }
        sendDatagram(wrap(peer: peer, payload: cmd), to: address)
        peer.lastSend = now()
    }

    private func sendPing(_ peer: MLENetPeer) {
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdPing | MLENetConst.commandFlagAcknowledge)
        cmd.append(0xFF)
        cmd.append(0); cmd.append(0)
        sendReliableRaw(peer, channelID: 0xFF, command: cmd)
    }

    /// 上层发送可靠控制消息（线程安全，入队后由 ENet 线程排出）
    func sendReliable(to peer: MLENetPeer, channel: UInt8, data: [UInt8]) {
        lock.lock()
        pendingOutbound.append((peer: peer, channel: channel, data: data, reliable: true))
        lock.unlock()
    }

    /// 上层发送不可靠控制消息（线程安全）
    func sendUnreliable(to peer: MLENetPeer, channel: UInt8, data: [UInt8]) {
        lock.lock()
        pendingOutbound.append((peer: peer, channel: channel, data: data, reliable: false))
        lock.unlock()
    }

    private func sendReliableNow(to peer: MLENetPeer, channel: UInt8, data: [UInt8]) {
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdSendReliable | MLENetConst.commandFlagAcknowledge)
        cmd.append(channel)
        cmd.append(0); cmd.append(0)
        cmd.mlAppendBE16(UInt16(clamping: data.count))
        cmd.append(contentsOf: data)
        sendReliableRaw(peer, channelID: channel, command: cmd)
    }

    private func sendUnreliableNow(to peer: MLENetPeer, channel: UInt8, data: [UInt8]) {
        let idx = Int(channel)
        guard idx < peer.channels.count else { return }
        peer.channels[idx].outgoingUnreliableSeq = peer.channels[idx].outgoingUnreliableSeq &+ 1
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdSendUnreliable)
        cmd.append(channel)
        cmd.append(0); cmd.append(0)
        cmd.mlAppendBE16(peer.channels[idx].outgoingUnreliableSeq)
        cmd.mlAppendBE16(UInt16(clamping: data.count))
        cmd.append(contentsOf: data)
        guard let address = peer.address else { return }
        sendDatagram(wrap(peer: peer, payload: cmd), to: address)
        peer.lastSend = now()
    }

    /// 主动断开（通知对端）；线程安全
    func disconnectPeer(_ peer: MLENetPeer) {
        lock.lock()
        pendingDisconnect.append(peer)
        lock.unlock()
    }

    private func disconnectPeerNow(_ peer: MLENetPeer) {
        var cmd = [UInt8]()
        cmd.append(MLENetConst.cmdDisconnect | MLENetConst.commandFlagAcknowledge)
        cmd.append(0xFF)
        cmd.append(0); cmd.append(0)
        cmd.mlAppendBE32(0)
        sendReliableRaw(peer, channelID: 0xFF, command: cmd)
        disconnect(peer, notify: true)
    }

    /// 发送数据报并计数（诊断用）
    @discardableResult
    private func sendDatagram(_ bytes: [UInt8], to address: MLSocketAddress) -> Bool {
        datagramsSent += 1
        return socket.send(bytes, to: address)
    }

    var connectedPeers: [MLENetPeer] {
        lock.lock(); defer { lock.unlock() }
        return publishedPeers
    }
}
