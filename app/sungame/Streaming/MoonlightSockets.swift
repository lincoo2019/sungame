//
//  MoonlightSockets.swift
//  QuickRecorder
//
//  串流所需的 UDP 套接字封装（视频 / 音频 / 控制通道 / ping 唤醒）。
//  使用双栈 IPv6 套接字（IPV6_V6ONLY=0）以同时接受 IPv4 与 IPv6 客户端。
//

import Foundation

/// 与 sockaddr_storage 对应的地址值类型，支持比较与展示
struct MLSocketAddress: Hashable, CustomStringConvertible {
    var storage: sockaddr_storage

    init(storage: sockaddr_storage) {
        self.storage = storage
    }

    static func == (lhs: MLSocketAddress, rhs: MLSocketAddress) -> Bool {
        var l = lhs.storage
        var r = rhs.storage
        return withUnsafeBytes(of: &l) { lb in
            withUnsafeBytes(of: &r) { rb in
                guard let la = lb.baseAddress, let ra = rb.baseAddress else { return false }
                // 仅比较 family/addr/port 关键字段
                return memcmp(la, ra, MemoryLayout<sockaddr_storage>.size) == 0
            }
        }
    }

    func hash(into hasher: inout Hasher) {
        var s = storage
        withUnsafeBytes(of: &s) { raw in
            if let base = raw.baseAddress {
                hasher.combine(bytes: UnsafeRawBufferPointer(start: base, count: MemoryLayout<sockaddr_storage>.size))
            }
        }
    }

    var description: String {
        var host = [CChar](repeating: 0, count: Int(NI_MAXHOST))
        var serv = [CChar](repeating: 0, count: Int(NI_MAXSERV))
        var s = storage
        let len = socklen_t(s.ss_len)
        let rc = withUnsafePointer(to: &s) { ptr in
            ptr.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                getnameinfo(sa, len, &host, socklen_t(host.count), &serv, socklen_t(serv.count),
                            NI_NUMERICHOST | NI_NUMERICSERV)
            }
        }
        guard rc == 0 else { return "unknown" }
        return "\(String(cString: host)):\(String(cString: serv))"
    }

    var host: String {
        let parts = description.split(separator: ":")
        return parts.isEmpty ? "" : String(parts[0])
    }

    var port: UInt16 {
        let parts = description.split(separator: ":")
        return parts.count < 2 ? 0 : UInt16(parts[1]) ?? 0
    }

    /// 转为 IPv4 数值（用于 X-SS-Ping-Payload 匹配等），仅 IPv4/IPv4 映射地址有效
    var ipv4Bytes: [UInt8]? {
        var s = storage
        if s.ss_family == sa_family_t(AF_INET) {
            return withUnsafeBytes(of: &s) { raw in
                let sa = raw.baseAddress!.assumingMemoryBound(to: sockaddr_in.self)
                let a = UInt32(bigEndian: sa.pointee.sin_addr.s_addr)
                return [UInt8((a >> 24) & 0xFF), UInt8((a >> 16) & 0xFF),
                        UInt8((a >> 8) & 0xFF), UInt8(a & 0xFF)]
            }
        }
        if s.ss_family == sa_family_t(AF_INET6) {
            return withUnsafeBytes(of: &s) { raw in
                let sa = raw.baseAddress!.assumingMemoryBound(to: sockaddr_in6.self)
                let b = withUnsafeBytes(of: sa.pointee.sin6_addr) { Array($0) }
                // v4-mapped: ::ffff:a.b.c.d
                if b.count == 16, b[0...9].allSatisfy({ $0 == 0 }), b[10] == 0xFF, b[11] == 0xFF {
                    return Array(b[12...15])
                }
                return nil
            }
        }
        return nil
    }
}

/// 带读超时的 UDP 套接字（阻塞式，配合专用线程使用）
final class MLUDPSocket {
    private(set) var fd: Int32 = -1
    let port: Int

    /// - Parameter sndBufSize: 发送缓冲字节数。音频要**故意设小**：1MB 的缓冲会把迟到的音频包
    ///   在内核里排队慢慢发，宿主侧观测到的只是「已交给内核」，真实空口延迟完全不可见，而客户端
    ///   那 30ms 的抖动窗口早就过期了 → 听感是「持续微爆」。小缓冲让拥塞**当场暴露**成 EAGAIN，
    ///   由上层决定重试还是计数，而不是静默积压几百毫秒。视频相反，需要大缓冲吸收关键帧突发。
    init(port: Int, sndBufSize: Int = 1 << 20) throws {
        self.port = port
        fd = socket(AF_INET6, SOCK_DGRAM, 0)
        guard fd >= 0 else { throw MLCryptoError.cryptoFailure("socket() 失败: \(errno)") }

        var reuse: Int32 = 1
        setsockopt(fd, SOL_SOCKET, SO_REUSEADDR, &reuse, socklen_t(MemoryLayout<Int32>.size))
        // 关闭 IPV6_V6ONLY，使该套接字同时接受 IPv4（以 v4-mapped 形式）与 IPv6 客户端。
        // 注意：必须显式设为 0；若设为 1 则只收 IPv6，IPv4 客户端的包会全部收不到。
        var v6Only: Int32 = 0
        setsockopt(fd, IPPROTO_IPV6, IPV6_V6ONLY, &v6Only, socklen_t(MemoryLayout<Int32>.size))
        var rcvBuf: Int32 = 1 << 20
        setsockopt(fd, SOL_SOCKET, SO_RCVBUF, &rcvBuf, socklen_t(MemoryLayout<Int32>.size))
        var sndBuf = Int32(sndBufSize)
        setsockopt(fd, SOL_SOCKET, SO_SNDBUF, &sndBuf, socklen_t(MemoryLayout<Int32>.size))

        var addr = sockaddr_in6()
        addr.sin6_family = sa_family_t(AF_INET6)
        addr.sin6_port = in_port_t(UInt16(port).bigEndian)
        addr.sin6_addr = in6addr_any

        let rc = withUnsafePointer(to: &addr) { ptr in
            ptr.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                bind(fd, sa, socklen_t(MemoryLayout<sockaddr_in6>.size))
            }
        }
        guard rc == 0 else {
            Darwin.close(fd); fd = -1
            throw MLCryptoError.cryptoFailure("bind(\(port)) 失败: \(errno)")
        }
        try setReadTimeout(ms: 20)
    }

    deinit {
        if fd >= 0 { Darwin.close(fd) }
    }

    func setReadTimeout(ms: Int) throws {
        var tv = timeval(tv_sec: ms / 1000, tv_usec: suseconds_t((ms % 1000) * 1000))
        let rc = setsockopt(fd, SOL_SOCKET, SO_RCVTIMEO, &tv, socklen_t(MemoryLayout<timeval>.size))
        guard rc == 0 else { throw MLCryptoError.cryptoFailure("SO_RCVTIMEO 失败: \(errno)") }
    }

    /// 接收一个数据报；超时返回 nil
    func receive(maxLength: Int = 65536) -> (data: [UInt8], from: MLSocketAddress)? {
        var buffer = [UInt8](repeating: 0, count: maxLength)
        var from = sockaddr_storage()
        var fromLen = socklen_t(MemoryLayout<sockaddr_storage>.size)
        let n: Int = withUnsafeMutablePointer(to: &from) { fromPtr in
            fromPtr.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                buffer.withUnsafeMutableBytes { raw in
                    recvfrom(fd, raw.baseAddress, maxLength, 0, sa, &fromLen)
                }
            }
        }
        if n < 0 {
            return nil
        }
        return (Array(buffer[0..<n]), MLSocketAddress(storage: from))
    }

    /// 一次发送的结果。区分「暂时发不出去」与「永久失败」，上层据此决定重试还是放弃。
    enum SendOutcome: Equatable {
        case ok
        /// 内核发送队列满（EAGAIN/EWOULDBLOCK/ENOBUFS）。包**没有被发出**，重试有意义。
        case wouldBlock
        /// 对端不可达（ECONNREFUSED 等）：重试无意义，且通常说明对端已经关闭。
        case unreachable
        /// 其它错误
        case failed(Int32)
    }

    @discardableResult
    func send(_ data: [UInt8], to address: MLSocketAddress) -> Bool {
        sendDetailed(data, to: address, dontWait: false) == .ok
    }

    /// 非阻塞发送：绝不在发送线程里硬阻塞（音频专用，见 sendDetailed 说明）。
    @discardableResult
    func sendNonBlocking(_ data: [UInt8], to address: MLSocketAddress) -> SendOutcome {
        sendDetailed(data, to: address, dontWait: true)
    }

    /// 带错误分类的发送。`dontWait` 决定是否带 MSG_DONTWAIT。
    ///
    /// 为什么音频必须非阻塞：音频每 5ms 一包，客户端乱序等待窗口只有 ~30ms。如果 sendto 在
    /// 拥塞时阻塞几十毫秒，发送线程会被按住在内核里，后续音频包全部迟到，客户端只能走 PLC
    /// → 正是「持续微爆」。非阻塞后拥塞表现为 wouldBlock，可由上层立刻重试（微秒级退避），
    /// 既不阻塞线程，也不让包静默积压。
    /// 视频/控制通道保持阻塞语义（关键帧突发需要内核排队吸收，且 ENet 依赖其原有时序）。
    func sendDetailed(_ data: [UInt8], to address: MLSocketAddress, dontWait: Bool = false) -> SendOutcome {
        var target = address.storage
        let len = socklen_t(target.ss_len)
        let flags = dontWait ? MSG_DONTWAIT : 0
        let n: Int = withUnsafePointer(to: &target) { ptr in
            ptr.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                data.withUnsafeBytes { raw in
                    sendto(fd, raw.baseAddress, data.count, flags, sa, len)
                }
            }
        }
        if n == data.count { return .ok }
        return MLUDPSocket.classifySendError(errno)
    }

    /// 把内核 errno 映射成发送结果。抽成纯函数以便穷举测试（见 harness）：
    /// 这套分类决定了音频是「立刻重试」还是「计数放弃」，错分会导致白等或丢包不可见。
    static func classifySendError(_ e: Int32) -> SendOutcome {
        switch e {
        case EAGAIN, EWOULDBLOCK, ENOBUFS:
            // 内核发送队列满：包没发出去，重试有意义。
            return .wouldBlock
        case ECONNREFUSED, EHOSTUNREACH, ENETUNREACH, ECONNRESET:
            // 对端不可达：重试无意义，通常说明客户端已关闭。
            return .unreachable
        default:
            return .failed(e)
        }
    }

    func close() {
        if fd >= 0 { Darwin.close(fd); fd = -1 }
    }

    // MARK: - 链路层优先级（QoS）

    /// 数据流在链路上的优先级类别。取值语义与 Sunshine `qos_data_type_e` 一致。
    enum QoSDataType {
        case none
        /// 音频：Interactive Voice
        case audio
        /// 视频：Interactive Video
        case video

        /// macOS 原生服务类型（SO_NET_SERVICE_TYPE）。这是**决定 Wi-Fi WMM 接入类别**的关键：
        /// 不设时音频与视频同处 best-effort 队列，会被几十 KB 的关键帧突发整段挡住。
        var serviceType: Int32? {
            switch self {
            case .audio: return Int32(NET_SERVICE_TYPE_VO)
            case .video: return Int32(NET_SERVICE_TYPE_VI)
            case .none:  return nil
            }
        }

        /// DSCP 值（尚未左移 2 位）。与 Sunshine/Windows 一致：音频 CS6=48、视频 AF41=40。
        var dscp: Int32? {
            switch self {
            case .audio: return 48
            case .video: return 40
            case .none:  return nil
            }
        }
    }

    /// 给套接字设置链路优先级。必须在**学到对端地址之后**调用。
    ///
    /// 为什么必须做（真实症状与对照实现）：宿主经 Wi-Fi 串流时，音频若不做任何标记，就会和
    /// 视频（30fps、周期性 200KB+ 关键帧、单帧连发数毫秒）排在同一条 best-effort 队列里，
    /// 于是音频包**持续**迟到；客户端音频队列只有约 3 个包（~15ms）深度、乱序等待窗口仅 10ms，
    /// 一旦被顶出窗口就按丢包走 PLC —— 听感正是「持续不断的爆裂声」。
    /// 而宿主侧完全看不到：它只测量「交给内核」的时刻（实测发包间隔最大 8ms、缺帧 0 拍），
    /// 丢/迟都发生在网卡与空口之后。Sunshine 对此的处理是
    /// `platf::enable_socket_qos(..., qos_data_type_e::audio, ...)`（src/stream.cpp:2174），
    /// 由客户端 ANNOUNCE 的 `x-nv-aqos.qosTrafficType` 决定是否附带 DSCP 标记。
    /// - Returns: 内核读回的实际服务类型；-1 表示无法读取（设置未生效或被拒）。
    @discardableResult
    func enableQoS(for address: MLSocketAddress, type: QoSDataType, dscpTagging: Bool) -> Int32 {
        guard fd >= 0, let serviceType = type.serviceType else { return -1 }

        // ① 服务类型 —— 这是 macOS 上**真正决定 Wi-Fi WMM 接入类别**的开关，也是本修复的核心。
        //    Sunshine 的注释即为此意：link-layer prioritization *without* DSCP tagging
        //    （src/platform/macos/misc.mm:554）。实测本机：setsockopt 后读回 = 4(VO)/3(VI)，
        //    空口调度随即按语音/视频队列处理。
        var st = serviceType
        setsockopt(fd, SOL_SOCKET, SO_NET_SERVICE_TYPE, &st, socklen_t(MemoryLayout<Int32>.size))

        // 读回确认。低权限/沙盒进程有可能被内核静默拒绝，只看 setsockopt 返回值会把
        // 「没生效」误判成「已生效」，因此这里必须读回，并把结果写进串流日志供核对。
        var applied: Int32 = -1
        var appliedLen = socklen_t(MemoryLayout<Int32>.size)
        getsockopt(fd, SOL_SOCKET, SO_NET_SERVICE_TYPE, &applied, &appliedLen)

        // ② DSCP 标记：仅当客户端要求（qosTrafficType != 0）且对端是**真 IPv6** 时设置。
        //
        // 实测结论（本机以广播包经真实 Wi-Fi 出口 + IP_RECVTOS 观测报文头）：
        //   · IP_TOS 在 AF_INET6 套接字上直接返回 EINVAL，永远不生效；
        //   · IPV6_TCLASS 能设、能读回 192，但对 v4-mapped 对端（::ffff:a.b.c.d）发出的是
        //     IPv4 报文，实测报文 TOS 恒为 0 —— 它并不会把 DSCP 写进 IPv4 头。
        // 客户端就在同一子网（192.168.31.0/24），中间没有按 DSCP 调度的路由器，
        // 所以 DSCP 对本场景几乎无收益；Wi-Fi 上的优先级完全由上面的服务类型提供。
        // 保留这段是为了对真 IPv6 对端与 Sunshine 行为一致；v4-mapped 路径下**不做无效设置**，
        // 以免日志/代码给人「已经打了 DSCP」的错误印象。
        if dscpTagging, let dscp = type.dscp, address.ipv4Bytes == nil {
            var tos = dscp << 2
            setsockopt(fd, IPPROTO_IPV6, IPV6_TCLASS, &tos, socklen_t(MemoryLayout<Int32>.size))
        }

        return applied
    }
}
