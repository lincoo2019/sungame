//
//  MoonlightMedia.swift
//  QuickRecorder
//
//  媒体传输层：视频 RTP（UDP 47998）与音频 RTP（UDP 48000）。
//
//  视频包布局（16 字节 RTP 头 + 16 字节 NV 头 + 负载）：
//    0   : RTP header = 0x90（版本 2 + 扩展位）
//    1   : packetType（视频侧被客户端忽略，填 0）
//    2   : sequenceNumber  BE16（同一 FEC 块内连续；blockBase + shardIndex）
//    4   : timestamp       BE32（90kHz，同帧所有分片相同）
//    8   : ssrc            BE32
//    12  : reserved        4 字节
//    16  : streamPacketIndex LE32 = seq << 8
//    20  : frameIndex        LE32（从 1 起单调递增）
//    24  : flags             SOF/EOF/CONTAINS_PIC_DATA
//    25  : extraFlags
//    26  : multiFecFlags     0x10
//    27  : multiFecBlocks    (blockIndex<<4) | ((blockCount-1)<<6)
//    28  : fecInfo           LE32 = (shardIndex<<12) | (dataShards<<22) | (percentage<<4)
//    32  : 负载（首个分片开头为 8 字节短帧头）
//
//  音频包布局（12 字节 RTP 头 + 负载）：
//    0: 0x80  1: 97  2: seq BE16  4: timestamp BE32(毫秒)  8: ssrc
//  负载为 Opus；若客户端请求了音频加密（Moonlight iOS 默认 ENCFLG_ALL），
//  负载需先做 AES-128-CBC(PKCS#7) 加密（密钥 = rikey，IV = BE32(riKeyId+seq) 后 12 字节 0），
//  否则客户端会把明文当密文解密 → opus 解码失败 → 完全静音。
//
//  音频 FEC 包布局（12 字节 RTP 头 + 12 字节 AUDIO_FEC_HEADER + 校验分片）：
//    RTP  : 0x80  1: 127  2: seq BE16（= 块尾数据 seq + fecShardIndex + 1）  4: ts=0  8: ssrc=0
//    FEC  : 0: fecShardIndex  1: 97  2: baseSequenceNumber BE16  4: baseTimestamp BE32  8: ssrc=0
//  每 4 个音频数据包（seq 连续、baseSeq 4 对齐）为一个 FEC 块，块尾追加 2 个 RS 校验分片
//  （Sunshine/moonlight-common-c 同款 4+2，OpenFEC 校验矩阵），客户端丢包时用校验分片恢复，
//  只能救回块内 2 个包。块内所有分片长度必须一致（Opus 已强制 CBR），否则客户端整体禁用音频 FEC。
//

import Foundation

/// 媒体流基类：绑定 UDP 端口，通过客户端的 SS_PING 学习目标端点
class MLMediaStream {
    let socket: MLUDPSocket
    private let port: Int
    private var peer: MLSocketAddress?
    private var thread: Thread?
    private var running = false
    fileprivate let logHandler: (String) -> Void
    /// 期望的 ping 负载（16 字节 ASCII），为空表示接受任意来源
    let expectedPingPayload: [UInt8]
    /// 收到的客户端 ping 数。客户端音频 socket 若还活着就会持续发 ping，
    /// 因此这个计数可以区分「客户端没收到/没解码」和「客户端音频线程已经退出」。
    private(set) var pingsReceived = 0
    private let lock = NSLock()

    /// 本流的链路优先级类别（音频/视频）。子类在 init 里指定。
    var qosType: MLUDPSocket.QoSDataType = .none
    /// 是否附带 DSCP 标记。由 ANNOUNCE 的 `x-nv-aqos.qosTrafficType` / `x-nv-vqos[0].qosTrafficType`
    /// 决定：局域网串流时客户端发 4/5（要求标记），远程串流发 0（不标记，避免被中间设备丢弃）。
    var qosDscpTagging = false

    init(port: Int, pingPayload: [UInt8], sndBufSize: Int = 1 << 20, log: @escaping (String) -> Void) throws {
        self.port = port
        self.expectedPingPayload = pingPayload
        self.logHandler = log
        self.socket = try MLUDPSocket(port: port, sndBufSize: sndBufSize)
    }

    deinit { stop() }

    var hasPeer: Bool {
        lock.lock(); defer { lock.unlock() }
        return peer != nil
    }

    func start() {
        guard !running else { return }
        running = true
        let t = Thread { [weak self] in self?.pingLoop() }
        t.name = "MLMediaStream-\(port)"
        t.stackSize = 256 * 1024
        thread = t
        t.start()
    }

    func stop() {
        running = false
        socket.close()
        thread = nil
    }

    /// 等待客户端 ping（最长 timeout 秒）
    func waitForPeer(timeout: TimeInterval) -> Bool {
        let deadline = Date().addingTimeInterval(timeout)
        while Date() < deadline {
            if hasPeer { return true }
            Thread.sleep(forTimeInterval: 0.02)
        }
        return hasPeer
    }

    private func pingLoop() {
        while running {
            guard let packet = socket.receive(maxLength: 2048) else { continue }
            let data = packet.data

            // 新协议：SS_PING { payload[16], sequenceNumber(BE32) }
            if data.count >= 20 {
                let payload = Array(data[0..<16])
                if !expectedPingPayload.isEmpty, payload != expectedPingPayload {
                    // 负载不匹配则忽略（可能是其它会话）
                    continue
                }
                setPeer(packet.from)
                continue
            }
            // 旧协议：4 字节 "PING"
            if data.count == 4, data[0] == 0x50, data[1] == 0x49, data[2] == 0x4E, data[3] == 0x47 {
                setPeer(packet.from)
            }
        }
    }

    private func setPeer(_ address: MLSocketAddress) {
        lock.lock()
        let isNew = peer != address
        peer = address
        pingsReceived += 1
        lock.unlock()
        // 学到对端后立刻设置链路优先级：音频包必须在空口上压过视频的关键帧突发，
        // 否则会在客户端侧被持续挤出乱序窗口 → PLC → 「持续不断的爆裂声」。
        if isNew {
            applyQoS()
            logHandler("端口 \(port) 已学习客户端地址 \(address)")
        }
    }

    /// 套用链路优先级（SO_NET_SERVICE_TYPE，必要时附带 DSCP）。
    ///
    /// 需要能重复调用：客户端 ping 通常在 RTSP ANNOUNCE **之前**到达（实测同秒内先学地址、
    /// 后打印协商结果），而「要不要打 DSCP」这个决定来自 ANNOUNCE 里的 qosTrafficType。
    /// 因此协商完成后必须再调一次，否则服务类型生效、DSCP 却永远没设上。
    func applyQoS() {
        lock.lock()
        let address = peer
        let type = qosType
        let tagging = qosDscpTagging
        lock.unlock()
        guard let address else { return }
        let applied = socket.enableQoS(for: address, type: type, dscpTagging: tagging)
        // 打印内核读回值：这是判断「优先级是否真的生效」的唯一可靠依据。
        // VO(4)/VI(3) 为期望值；0 表示被拒（则需改用其他手段），-1 表示读取失败。
        let names: [Int32: String] = [0: "BE", 1: "BK", 2: "RD", 3: "VI", 4: "VO", 5: "CTL", 6: "SV", 7: "RV"]
        let appliedName = names[applied] ?? "?"
        let role = (type == .audio) ? "音频" : "视频"
        logHandler("端口 \(port) 【\(role)】链路优先级 服务类型=\(appliedName)(\(applied))"
            + (applied > 0 ? " ✓已生效" : " ✗未生效")
            + "，DSCP=\(tagging && address.ipv4Bytes == nil ? "已设置" : "不适用(macOS 无法给 v4-mapped 打标记)"))")
    }

    /// 发送一个数据报。返回 false 表示**没有对端**或内核当场拒绝了这次发送
    /// （UDP 在链路拥塞/接口队列满时会返回 ENOBUFS/EAGAIN，包被直接丢弃）。
    /// 调用方应据此决定是否重试——过去这里忽略返回值，丢包完全不可见。
    @discardableResult
    func send(_ data: [UInt8]) -> Bool {
        sendReportingOutcome(data) == .ok
    }

    /// 同 send，但把内核的失败原因回传给调用方（音频据此区分「拥塞可重试」与「对端已关」）。
    /// 音频必须走**非阻塞**路径：阻塞的 sendto 会在拥塞时把发送线程按在内核里几十毫秒。
    func sendReportingOutcome(_ data: [UInt8]) -> MLUDPSocket.SendOutcome {
        lock.lock()
        let target = peer
        lock.unlock()
        guard let target else { return .unreachable }
        return socket.sendNonBlocking(data, to: target)
    }
}

// MARK: - 视频

final class MLVideoStream: MLMediaStream {
    private var sequenceNumber: UInt16 = 0
    private var frameIndex: UInt32 = 0
    /// 全流递增的包序号（每分片 +1）。注意：写入线序时必须左移 8 位。
    /// 客户端拿到该字段会先 `>>= 8` 再与上一包做 +1 比对来判断丢包
    /// （VideoDepacketizer.c：streamPacketIndex >>= 8; 要求 == lastPacketInStream + 1），
    /// 与 Sunshine 的 `(lowseq + x) << 8` 一致。直接写裸序号会被判定为「每包都丢」，
    /// 客户端会逐包丢弃整帧并疯狂索取关键帧，表现为连不上/黑屏。
    private var streamPacketIndex: UInt32 = 0
    /// 客户端请求 IDR
    private var needsIDR = false

    // MARK: 诊断：视频「突发」统计
    //
    // 一帧的所有分片是在紧循环里连发的（见 sendFrame），没有做节拍平滑。大帧——尤其 IDR——
    // 会在链路上形成瞬时突发：包一古脑交给驱动/Wi-Fi，同一时刻的音频包被挤到后面。
    // 宿主侧的发包间隔探针只看得到「交出去的时刻」（音频自己那条线），看不到这层链路排队，
    // 所以在客户端表现为「画面卡 + 咔哒」，而宿主日志却一片正常。
    // 这里把每 5 秒窗口的「单帧最大字节、IDR 最大字节 / 条数、单帧发送耗时」打出来，用来判断突发有多猛。
    // 「IDR 条数」是关键指标：它 = 周期关键帧（VT MaxKeyFrameInterval）+ 客户端按需请求。
    // 每条 IDR 都是几十~上百 KB 的瞬时突发，每秒一条就足以把同一时刻的音频包挤到客户端缓冲见底（爆音）。
    private var burstWindowStart = CFAbsoluteTimeGetCurrent()
    private var burstMaxFrameBytes = 0
    private var burstMaxIDRBytes = 0
    private var burstMaxSendMicros: UInt64 = 0
    private var burstFrames = 0
    private var burstPackets = 0
    private var burstIDRCount = 0
    private var burstIDRFrames = 0

    /// 由 ANNOUNCE 协商：客户端包大小（含 NV 头，不含 RTP 头）
    private(set) var packetSize = 1024

    init(pingPayload: [UInt8], log: @escaping (String) -> Void) throws {
        try super.init(port: MLPort.video, pingPayload: pingPayload, log: log)
        qosType = .video
    }

    func configure(packetSize: Int) {
        // 客户端会按 16 的倍数对齐；限制在合理范围
        self.packetSize = min(max(packetSize, 256), 4096)
    }

    func requestIDR() { needsIDR = true }

    func consumeIDRRequest() -> Bool {
        defer { needsIDR = false }
        return needsIDR
    }

    /// 发送一帧（data 为 Annex-B 码流；isIDR 时首部需已包含 SPS/PPS）
    func sendFrame(_ data: [UInt8], isIDR: Bool, timestamp90k: UInt32) {
        frameIndex &+= 1

        // 短帧头 8 字节
        var payload = [UInt8]()
        payload.append(MLVideo.shortFrameHeaderType)
        payload.append(0); payload.append(0) // frame_processing_latency
        payload.append(isIDR ? MLVideo.frameTypeIDR : MLVideo.frameTypePFrame)
        payload.append(0); payload.append(0) // lastPayloadLen（H.264/HEVC 客户端不读取）
        payload.append(0); payload.append(0) // reserved
        payload.append(contentsOf: data)

        // 每个分片的负载上限：packetSize 已含 16 字节 NV 头
        let shardPayloadSize = max(packetSize - 16, 16)
        let totalShards = max(1, (payload.count + shardPayloadSize - 1) / shardPayloadSize)
        // FEC 分片 = 整个 UDP 数据报（16B RTP + 16B NV + 负载），客户端按 packetSize + 16 补零后做 RS 解码
        //（RtpVideoQueue.c：receiveSize = StreamConfig.packetSize + MAX_RTP_HEADER_SIZE）。
        let blockSizeBytes = shardPayloadSize + 32

        // 视频 FEC（对照 Sunshine stream.cpp videoBroadcastThread / fec::encode）。
        //
        // 以前 fecInfo 里的百分比恒为 0：客户端 RtpVideoQueue 遇到 fecPercentage==0 就**不做任何恢复**，
        // Wi-Fi 上一帧几十个包里只要丢/迟到一个，整帧作废 → 画面卡顿 + 客户端索要 IDR；
        // 补发的 IDR 又是上百 KB 的突发，把同一时刻的 5ms 音频包挤出客户端乱序窗口 → 爆音，
        // 两个症状互相放大。Sunshine 默认 fec_percentage=20、minRequiredFecPackets=2，这里对齐。
        var fecPercentage = MLVideo.fecPercentage
        // 每块数据分片上限：D + P ≤ 255（nanors/GF(2^8) 限制），P = D × F → D = 25500 / (100 + F)
        var blockCount: Int
        if fecPercentage > 0 {
            let maxDataPerBlock = (255 * 100) / (100 + fecPercentage)
            blockCount = (totalShards + maxDataPerBlock - 1) / maxDataPerBlock
            if blockCount > MLVideo.maxFecBlocks {
                fecPercentage = 0 // 超大帧：与 Sunshine 一致，本帧关闭 FEC
            }
        } else {
            blockCount = 1
        }
        if fecPercentage == 0 {
            blockCount = (totalShards + MLVideo.maxDataShardsPerBlock - 1) / MLVideo.maxDataShardsPerBlock
            if blockCount > MLVideo.maxFecBlocks { return } // 帧过大（超出 4 块容量），丢弃
        }
        // 均分到各块（同 Sunshine 的 aligned_size 均分）
        let perBlock = (totalShards + blockCount - 1) / blockCount
        var blocks: [(offset: Int, count: Int)] = []
        var offset = 0
        while offset < totalShards {
            let count = min(perBlock, totalShards - offset)
            blocks.append((offset, count))
            offset += count
        }

        var seq = sequenceNumber
        let burstStart = CFAbsoluteTimeGetCurrent()
        var sentCount = 0
        for (blockIndex, block) in blocks.enumerated() {
            let blockShards = block.count
            // 本块实际的校验分片数 / 百分比（与客户端 bufferParityPackets = (D*F+99)/100 严格一致）
            var blockPct = fecPercentage
            var parityCount = 0
            if blockPct > 0 {
                parityCount = (blockShards * blockPct + 99) / 100
                if parityCount < MLVideo.minRequiredFecPackets {
                    parityCount = MLVideo.minRequiredFecPackets
                    blockPct = min(255, (100 * parityCount) / blockShards)
                    parityCount = (blockShards * blockPct + 99) / 100
                }
                if blockShards + parityCount > 255 { blockPct = 0; parityCount = 0 }
            }
            let blockLowSeq = seq
            var dataPackets: [[UInt8]] = []
            if parityCount > 0 { dataPackets.reserveCapacity(blockShards) }
            for x in 0..<blockShards {
                let shardStart = (block.offset + x) * shardPayloadSize
                let shardEnd = min(shardStart + shardPayloadSize, payload.count)
                guard shardStart < shardEnd else { continue }
                var shard = Array(payload[shardStart..<shardEnd])

                let isLastShardOfFrame = (blockIndex == blocks.count - 1) && (x == blockShards - 1)
                // 客户端要求：非 EOF 分片长度必须恰好为 packetSize
                if !isLastShardOfFrame, shard.count < shardPayloadSize {
                    shard.append(contentsOf: [UInt8](repeating: 0, count: shardPayloadSize - shard.count))
                }

                var flags: UInt8 = MLVideo.flagContainsPicData
                if x == 0 { flags |= MLVideo.flagSOF }
                if x == blockShards - 1 { flags |= MLVideo.flagEOF }

                var out = [UInt8]()
                out.reserveCapacity(32 + shard.count)
                appendVideoHeaders(&out, seq: seq, timestamp90k: timestamp90k,
                                   blockIndex: blockIndex, lastBlockIndex: blocks.count - 1,
                                   fecIndex: x, dataShards: blockShards, fecPercentage: blockPct)
                out.replaceSubrange(16..<20, with: le32(streamPacketIndex << 8))
                out[24] = flags
                out.append(contentsOf: shard)

                send(out)
                if parityCount > 0 { dataPackets.append(out) }
                seq &+= 1
                streamPacketIndex &+= 1
                sentCount += 1
                throttleBurst(sentCount)
            }

            // 校验分片：按 nanors 默认矩阵（客户端 reed_solomon_new(D,P) 不覆盖矩阵，见 RtpVideoQueue.c）
            // 对整包逐字节列做 RS，再像 Sunshine 一样把 RTP 头 / frameIndex / multiFecBlocks / fecInfo
            // 覆盖成本分片自己的值（RS 按字节列独立，覆盖只影响这些列，客户端恢复后会重写它们）。
            if parityCount > 0, dataPackets.count == blockShards {
                let parity = MLVideoFEC.encode(dataPackets, parityCount: parityCount, blockSize: blockSizeBytes)
                for (j, var pkt) in parity.enumerated() {
                    var hdr = [UInt8]()
                    appendVideoHeaders(&hdr, seq: blockLowSeq &+ UInt16(blockShards + j), timestamp90k: timestamp90k,
                                       blockIndex: blockIndex, lastBlockIndex: blocks.count - 1,
                                       fecIndex: blockShards + j, dataShards: blockShards, fecPercentage: blockPct)
                    pkt.replaceSubrange(0..<16, with: hdr[0..<16])          // RTP 头
                    pkt.replaceSubrange(20..<24, with: hdr[20..<24])        // frameIndex
                    pkt[27] = hdr[27]                                       // multiFecBlocks
                    pkt.replaceSubrange(28..<32, with: hdr[28..<32])        // fecInfo
                    send(pkt)
                    sentCount += 1
                    throttleBurst(sentCount)
                }
                seq = blockLowSeq &+ UInt16(blockShards + parityCount)
                statsVideoFecPackets += parityCount
            }
        }
        sequenceNumber = seq
        recordBurst(bytes: payload.count, isIDR: isIDR, packets: sentCount,
                    micros: UInt64(max(0, CFAbsoluteTimeGetCurrent() - burstStart) * 1_000_000))
    }

    /// 连发节流：一帧几十~上百个包若一古脑交给内核，会在几毫秒内独占发送路径与一个 CPU 核，
    /// 把同一时刻的音频排水定时器回调推迟十几毫秒——客户端（moonlight-common-c）**没有音频抖动缓冲**，
    /// 这十几毫秒的空洞会直接变成欠载，听感就是「咔哒」。每 8 个包让出 100µs：
    /// 峰值速率从数百 MB/s 压到 ~110MB/s，单帧最多多花 ~2ms（一帧 33ms 的预算内可忽略），
    /// 但不再长时间霸占调度，音频排水定时器能准点回调。
    private func throttleBurst(_ sent: Int) {
        if sent % 8 == 0 { usleep(100) }
    }

    /// 统计一帧的突发规模，每 5 秒打一行（见字段注释）。
    private var statsVideoFecPackets = 0

    private func le32(_ v: UInt32) -> [UInt8] {
        [UInt8(v & 0xFF), UInt8((v >> 8) & 0xFF), UInt8((v >> 16) & 0xFF), UInt8((v >> 24) & 0xFF)]
    }

    /// 写 16B RTP 头 + 16B NV 头（streamPacketIndex/flags 先置 0，由调用方填）。
    private func appendVideoHeaders(_ out: inout [UInt8], seq: UInt16, timestamp90k: UInt32,
                                    blockIndex: Int, lastBlockIndex: Int,
                                    fecIndex: Int, dataShards: Int, fecPercentage: Int) {
        out.append(MLVideo.rtpHeaderByte)
        out.append(0)
        out.append(UInt8(seq >> 8)); out.append(UInt8(seq & 0xFF))
        out.append(UInt8((timestamp90k >> 24) & 0xFF)); out.append(UInt8((timestamp90k >> 16) & 0xFF))
        out.append(UInt8((timestamp90k >> 8) & 0xFF)); out.append(UInt8(timestamp90k & 0xFF))
        out.append(0); out.append(0); out.append(0); out.append(0) // ssrc
        out.append(0); out.append(0); out.append(0); out.append(0) // reserved
        out.append(contentsOf: [0, 0, 0, 0])                        // streamPacketIndex
        out.mlAppendLE32(frameIndex)
        out.append(0)                                               // flags
        out.append(0)                                               // extraFlags
        out.append(0x10)                                            // multiFecFlags
        out.append(UInt8((blockIndex << 4) | (lastBlockIndex << 6)))
        let fecInfo = (UInt32(fecIndex) << 12) | (UInt32(dataShards) << 22) | (UInt32(fecPercentage & 0xFF) << 4)
        out.mlAppendLE32(fecInfo)
    }

    private func recordBurst(bytes: Int, isIDR: Bool, packets: Int, micros: UInt64) {
        burstFrames += 1
        burstPackets += packets
        if bytes > burstMaxFrameBytes { burstMaxFrameBytes = bytes }
        if isIDR {
            burstIDRCount += 1
            burstIDRFrames += packets
            if bytes > burstMaxIDRBytes { burstMaxIDRBytes = bytes }
        }
        if micros > burstMaxSendMicros { burstMaxSendMicros = micros }
        let now = CFAbsoluteTimeGetCurrent()
        let elapsed = now - burstWindowStart
        guard elapsed >= 5 else { return }
        logHandler(String(format: "视频：本窗口 %d 帧 %d 包（IDR %d 条 %d 包，FEC 校验 %d 包）；单帧最大 %.1f KB（IDR 最大 %.1f KB）；单帧连发耗时最大 %.2f ms",
                          burstFrames, burstPackets, burstIDRCount, burstIDRFrames, statsVideoFecPackets,
                          Double(burstMaxFrameBytes) / 1024, Double(burstMaxIDRBytes) / 1024,
                          Double(burstMaxSendMicros) / 1000))
        burstWindowStart = now
        burstMaxFrameBytes = 0
        burstMaxIDRBytes = 0
        burstMaxSendMicros = 0
        burstFrames = 0
        burstPackets = 0
        burstIDRCount = 0
        burstIDRFrames = 0
        statsVideoFecPackets = 0
    }
}

// MARK: - 音频

final class MLAudioStream: MLMediaStream {
    private var sequenceNumber: UInt16 = 0
    private var timestampMs: UInt32 = 0

    /// 音频加密密钥（/launch 的 rikey）。非 nil 表示客户端请求了音频加密，
    /// 每个 Opus 负载都要 AES-128-CBC 加密后再发（见文件头说明）。
    private var cipherKey: [UInt8]?
    private var riKeyID: UInt32 = 0
    /// 加密启用后失败只提示一次，避免刷屏
    private var loggedCipherFailure = false

    /// 周期性统计：音频没有画面那么显眼，出问题时必须能区分
    /// 「上游（chiaki）根本没给音频」和「给了但我们没发出去」。
    private var statsStart = CFAbsoluteTimeGetCurrent()
    private var statsFrames = 0
    private var statsBytes = 0
    /// 发送失败 / 重试统计。链路拥塞时 UDP 的 sendto 会返回 ENOBUFS/EAGAIN（内核当场丢弃），
    /// 丢一个包客户端就少 5ms 音频（解码出现断点 → 「啪」）。以前这里完全不可观测。
    private var statsSendFailures = 0
    private var statsSendRetries = 0
    /// 内核回「暂时发不出」的次数（EAGAIN/ENOBUFS）。这是**唯一**能在宿主侧看到的空口拥塞信号：
    /// >0 说明内核队列已满、包正被丢弃，客户端那 30ms 抖动窗口必然不够。
    private var statsSendWouldBlock = 0
    /// 对端不可达（ECONNREFUSED）次数。非 0 说明客户端音频端口已关，不是网络抖动。
    private var statsSendUnreachable = 0
    /// 相邻两次**成功** sendto 的最大间隔（ms）。这是宿主能观测到的最接近空口的指标：
    /// 它包含加密/组包/内核入队，但不含网卡之后的空口排队。若它远小于客户端 30ms 窗口，
    /// 而客户端仍丢包，则瓶颈确定在空口或客户端。
    private var statsWireMaxGapMs = 0.0
    private var lastSendAt: CFAbsoluteTime = 0

    // MARK: 音频 FEC（Sunshine audioBroadcastThread / moonlight-common-c RtpAudioQueue 同款 4+2）
    /// 块内数据分片数 / RS 校验分片数。客户端 `RTPA_DATA_SHARDS=4`、`RTPA_FEC_SHARDS=2` 写死，
    /// 多于 2 个包丢失则无法恢复（那 2 个校验分片最多救 2 个）。
    private let fecDataShards = 4
    private let fecParityShards = 2
    /// 当前 FEC 块的 4 个数据分片（= **加密后**的负载，块内长度必须恒定）
    private var fecShards = [[UInt8]](repeating: [], count: 4)
    /// 块基准：块首数据包的 seq/timestamp（客户端要求 baseSeq 必须 4 对齐）
    private var fecBaseSeq: UInt16 = 0
    private var fecBaseTs: UInt32 = 0
    private var statsFecPackets = 0
    /// 块内长度不一致而跳过的块数（正常应恒为 0；>0 说明 Opus CBR 被破坏）
    private var statsFecSkipped = 0

    /// 音频冗余策略：**不做逐包重发**，只发 4+2 的 FEC 校验分片（见 emitFecBlock）。
    ///
    /// 取舍依据（对照 moonlight-common-c/src/RtpAudioQueue.c）：
    ///   · 客户端的音频队列在 FEC 模式下对重复包是**显式拒绝**的（marks[pos] 已置位 → "duplicate
    ///     packet - reject it"），所以副本只有在「原包真丢了」时才有用；
    ///   · 块内 4 个数据包 + 2 个校验分片，本身就能恢复**任意 2 个**丢失（等于 50% 丢包率），
    ///     副本带来的边际收益很小；而副本把音频数据报速率翻倍（200 → 400/秒），
    ///     在 Wi-Fi 上与视频突发抢空口/排队，反而更容易把音频包挤迟到。
    ///   · 结论：省掉副本，把链路预算留给真正需要的包（音频 200/秒 + FEC 100/秒）。

    init(pingPayload: [UInt8], log: @escaping (String) -> Void) throws {
        // 音频发送缓冲故意设小（32768B ≈ 100 个音频包）：让空口拥塞当场以 EAGAIN 暴露，
        // 而不是在 1MB 内核队列里静默积压——积压的包到达时早已超出客户端 30ms 抖动窗口。
        try super.init(port: MLPort.audio, pingPayload: pingPayload, sndBufSize: 32768, log: log)
        qosType = .audio
    }

    /// 由 ANNOUNCE 协商结果配置音频加密。客户端（如 Moonlight iOS 默认 encryptionFlags=ENCFLG_ALL）
    /// 会请求音频加密；此时必须加密，否则客户端解密明文得到乱码、opus 解码失败、完全静音。
    func configureAudioEncryption(key: [UInt8]?, riKeyID: UInt32, enabled: Bool) {
        cipherKey = (enabled && key?.count == 16) ? key : nil
        self.riKeyID = riKeyID
    }

    /// 发送一个 Opus 帧
    func sendOpusFrame(_ opus: [UInt8], durationMs: UInt32) {
        let seq = sequenceNumber
        let ts = timestampMs
        // 客户端请求加密时，负载 = AES-128-CBC(PKCS#7(opus))，IV 由 riKeyId + 本包序列号确定
        var payload = opus
        if let key = cipherKey {
            let ivSeq = riKeyID &+ UInt32(seq)
            var iv = [UInt8](repeating: 0, count: 16)
            iv[0] = UInt8((ivSeq >> 24) & 0xFF); iv[1] = UInt8((ivSeq >> 16) & 0xFF)
            iv[2] = UInt8((ivSeq >> 8) & 0xFF); iv[3] = UInt8(ivSeq & 0xFF)
            if let encrypted = try? MLAESCBC.encrypt(key: key, iv: iv, plaintext: opus) {
                payload = encrypted
            } else if !loggedCipherFailure {
                loggedCipherFailure = true
                logHandler("音频加密失败，负载按明文发送（客户端将无法解码）")
            }
        }

        var out = [UInt8]()
        out.reserveCapacity(12 + payload.count)
        out.append(MLAudio.rtpHeaderByte)
        out.append(MLAudio.payloadTypeAudio)
        out.append(UInt8(seq >> 8)); out.append(UInt8(seq & 0xFF))
        out.append(UInt8((ts >> 24) & 0xFF)); out.append(UInt8((ts >> 16) & 0xFF))
        out.append(UInt8((ts >> 8) & 0xFF)); out.append(UInt8(ts & 0xFF))
        out.append(0); out.append(0); out.append(0); out.append(0)
        out.append(contentsOf: payload)
        transmitAudioDatagram(out)

        // 记入当前 FEC 块（数据分片 = 加密后负载）。分片槽位直接取 seq%4，
        // 保证 baseSeq = seq&~3 与客户端「baseSeq = (seq/4)*4」的反推严格一致。
        let shardIndex = Int(seq % UInt16(fecDataShards))
        fecShards[shardIndex] = payload
        if shardIndex == 0 {
            fecBaseSeq = seq
            fecBaseTs = ts
        } else if shardIndex == fecDataShards - 1 {
            emitFecBlock(lastSeq: seq)
        }

        sequenceNumber &+= 1
        timestampMs &+= durationMs

        statsFrames += 1
        statsBytes += payload.count
        reportStatsIfNeeded()
    }

    /// 块尾：按 OpenFEC 校验矩阵算 2 个 RS 校验分片，立即发 2 个 FEC 包（PT 127）。
    ///
    /// 线格式逐字节对齐 Sunshine `audioBroadcastThread`：
    /// FEC 包的 RTP seq = 块尾数据 seq + fecShardIndex + 1（客户端不校验该序号，
    /// 块归属只看 FEC 头里的 baseSequenceNumber），RTP timestamp/ssrc 置 0。
    private func emitFecBlock(lastSeq: UInt16) {
        let blockSize = fecShards[0].count
        guard blockSize > 0 else { return }
        // 块内分片长度必须一致：客户端遇到 "Audio block size mismatch" 会**整体禁用**音频 FEC。
        // Opus 已强制 CBR 正常恒为 60 字节（加密后 64）；真不一致就放弃本块 FEC，只发数据。
        guard fecShards.allSatisfy({ $0.count == blockSize }) else {
            statsFecSkipped += 1
            return
        }

        // RS 校验：parity[j][k] = XOR_i gf_mul(P[j*4+i], data[i][k])，与 nanors reed_solomon_encode 一致。
        //
        // 矩阵**必须是 OpenFEC 的那 8 个字节，不能用 nanors 的默认公式矩阵**。
        // 这一点反直觉，但两端都有原话为证，且已用真实 nanors 库实测：
        //   · 客户端 moonlight-common-c/src/RtpAudioQueue.c:54-60 —— `reed_solomon_new(4,2)` 之后
        //     立刻 `memcpy(queue->rs->p, {0x77,0x40,0x38,0x0e,0xc7,0xa7,0x0d,0x6c}, 8)`，
        //     注释原文：「RS implementation doesn't match the one Nvidia uses for audio data
        //     ... we can simply replace it with the matrix generated by OpenFEC which works correctly」。
        //   · 参考宿主 Sunshine/src/stream.cpp:1863-1864 —— 同样覆盖成这 8 个字节，注释一模一样。
        // 即：nanors 的公式式 `INV[(ps+i)^j]`（= {0x8e,0xf4,0x47,0xa7,...}）**不是**音频 FEC 实际
        // 使用的矩阵，它正是客户端要覆盖掉的那个「错误」矩阵。曾经按公式现场生成 → 客户端用覆盖后的
        // 矩阵去解 → 每个从 FEC 恢复出来的分片都是乱码 → 客户端照常当 Opus 解码 → 持续爆裂声。
        // （现成的 `MLGF256.fecMatrix` 仅用于自检/文档，不参与发包。）
        let P = MLAudio.openFecParityMatrix
        var parity = [[UInt8]](repeating: [UInt8](repeating: 0, count: blockSize), count: fecParityShards)
        for j in 0..<fecParityShards {
            for i in 0..<fecDataShards {
                let c = P[j * fecDataShards + i]
                let shard = fecShards[i]
                if c == 1 {
                    for k in 0..<blockSize { parity[j][k] ^= shard[k] }
                } else {
                    for k in 0..<blockSize { parity[j][k] ^= MLGF256.mul(c, shard[k]) }
                }
            }
        }

        for x in 0..<fecParityShards {
            let fecSeq = lastSeq &+ UInt16(x + 1)
            var out = [UInt8]()
            out.reserveCapacity(24 + blockSize)
            // RTP 头
            out.append(MLAudio.rtpHeaderByte)
            out.append(MLAudio.payloadTypeFEC)
            out.append(UInt8(fecSeq >> 8)); out.append(UInt8(fecSeq & 0xFF))
            out.append(0); out.append(0); out.append(0); out.append(0) // timestamp = 0
            out.append(0); out.append(0); out.append(0); out.append(0) // ssrc = 0
            // AUDIO_FEC_HEADER（注意 fecShardIndex 是首字段）
            out.append(UInt8(x))
            out.append(MLAudio.payloadTypeAudio)
            out.append(UInt8(fecBaseSeq >> 8)); out.append(UInt8(fecBaseSeq & 0xFF))
            out.append(UInt8((fecBaseTs >> 24) & 0xFF)); out.append(UInt8((fecBaseTs >> 16) & 0xFF))
            out.append(UInt8((fecBaseTs >> 8) & 0xFF)); out.append(UInt8(fecBaseTs & 0xFF))
            out.append(0); out.append(0); out.append(0); out.append(0) // ssrc = 0
            out.append(contentsOf: parity[x])
            transmitAudioDatagram(out)
            statsFecPackets += 1
        }
        // 刻意**不**补发 FEC 副本（与 Sunshine audioBroadcastThread 一致：每个校验分片只发一次）。
        //
        // 早先这里会在每块后再补发一遍 2 个 FEC 包，把音频侧数据报速率从 200/秒抬到 400/秒。
        // 实测抓包证实：块 base=0 的 shard 0/1 各出现两次。代价有两面，都是负面的：
        //   · 客户端的 FEC 队列对**重复分片**是显式拒绝的（RtpAudioQueue.c：marks[] 已清 →
        //     "duplicate packet - reject it"），所以副本只在「原包真丢了」时才有边际价值，
        //     而 4+2 本身已能恢复块内任意 2 个丢包；
        //   · 多出来的 200 包/秒全都要在 Wi-Fi 空口上与视频关键帧争抢，正是把音频包挤出
        //     客户端 10ms 乱序窗口（RTPQ_OOS_WAIT_TIME_MS）的主因之一 —— 与上面 QoS 修复配套，
        //     这里必须把带宽还给真正需要的包。
    }

    /// 实际发出一个 UDP 数据报，失败时短暂退避重试。
    ///
    /// 客户端按「一个 RTP 包 = 一个 5ms Opus 帧」解码：丢一个包就在解码码流里留下一个断点，
    /// 听感就是「啪」一下。而 UDP 在链路拥塞时 sendto 会当场返回 ENOBUFS/EAGAIN（包被内核丢弃，
    /// 不会排队重传）。这里重试几次（最多 ~0.75ms）能把这类**瞬时**发送失败救回来；
    /// 仍然失败才计数，便于日志里区分「网络丢的」和「上游没给」。
    private func transmitAudioDatagram(_ data: [UInt8]) {
        guard hasPeer else { return }
        var attempt = 0
        while true {
            let outcome = sendReportingOutcome(data)
            switch outcome {
            case .ok:
                // 记录成功发送的间隔（含本次入队耗时的近似值）。
                let now = CFAbsoluteTimeGetCurrent()
                if lastSendAt > 0 {
                    let gap = (now - lastSendAt) * 1000
                    if gap > statsWireMaxGapMs { statsWireMaxGapMs = gap }
                }
                lastSendAt = now
                return
            case .unreachable:
                // 对端已关/不可达，重试无意义：立刻计数返回，避免白等 0.75ms。
                statsSendUnreachable += 1
                statsSendFailures += 1
                return
            case .wouldBlock:
                statsSendWouldBlock += 1
            case .failed:
                statsSendFailures += 1
            }
            attempt += 1
            if attempt >= 4 { break }
            statsSendRetries += 1
            // 非阻塞下 EAGAIN 是「内核队列瞬时满」，微秒级让出后立刻重试即可，
            // 不需要也不应该等待毫秒级（那本身就是引入延迟）。
            usleep(200)
        }
        statsSendFailures += 1
    }

    private func reportStatsIfNeeded() {
        let now = CFAbsoluteTimeGetCurrent()
        let elapsed = now - statsStart
        guard elapsed >= 5 else { return }
        let fps = Double(statsFrames) / elapsed
        let kbps = Double(statsBytes) * 8 / elapsed / 1000
        let retries = statsSendRetries
        let failures = statsSendFailures
        let wouldBlock = statsSendWouldBlock
        let unreachable = statsSendUnreachable
        let wireGap = statsWireMaxGapMs
        let fecPackets = statsFecPackets
        let fecSkipped = statsFecSkipped
        logHandler(String(format: "音频 已转发：%.0f 帧/秒，%.0f Kbps，FEC=%d 跳过=%d，对端%@，收到 ping %d，"
            + "发送重试=%d 发送失败=%d 内核拥塞=%d 不可达=%d 发送间隔最大=%.1fms",
                          fps, kbps, fecPackets, fecSkipped, hasPeer ? "已知" : "未知", pingsReceived,
                          retries, failures, wouldBlock, unreachable, wireGap))
        statsStart = now
        statsFrames = 0
        statsBytes = 0
        statsSendRetries = 0
        statsSendFailures = 0
        statsSendWouldBlock = 0
        statsSendUnreachable = 0
        statsWireMaxGapMs = 0
        statsFecPackets = 0
        statsFecSkipped = 0
    }
}

/// GF(2^8) 乘法（本原多项式 0x11D，生成元 2）。
/// 与客户端解码所用 nanors（moonlight-common-c 子模块，commit b1e3c22）的域完全一致
/// （nanors `deps/obl/gf2_8_tables.h` 头两行注明 "generated with polynomial: 285" = 0x11D）。
///
/// 注意：这里只提供**域运算**。音频 FEC 实际使用的校验矩阵**不是** nanors 的公式矩阵
/// （`INV[(ps+i)^j]`），而是 OpenFEC 的那 8 个字节，见 `MLAudio.openFecParityMatrix`。
/// `fecMatrix` 保留下来仅用于自检/文档对照，**不参与发包**。
/// 视频 RS 校验（与 nanors reed_solomon_encode 等价：parity[j] = Σ_i P[j][i]·data[i]，P[j][i] = INV[(ps+i)^j]）。
/// 数据分片不足 blockSize 的部分按 0 补齐（客户端同样补零）。
enum MLVideoFEC {
    /// mulTable[c*256 + b] = c·b
    static let mulTable: [UInt8] = {
        var t = [UInt8](repeating: 0, count: 65536)
        for c in 1..<256 { for b in 1..<256 { t[c * 256 + b] = MLGF256.mul(UInt8(c), UInt8(b)) } }
        return t
    }()

    static func encode(_ data: [[UInt8]], parityCount: Int, blockSize: Int) -> [[UInt8]] {
        // 计算放在 C（QRVideoFECEncode）：Swift -Onone 下 IDR 帧要算 600+ms（日志「单帧连发耗时最大 637ms」）。
        var parity = [[UInt8]](repeating: [UInt8](repeating: 0, count: blockSize), count: parityCount)
        var lens = data.map { Int32($0.count) }
        let dataPtrs = data.map { d -> UnsafeMutablePointer<UInt8> in
            let p = UnsafeMutablePointer<UInt8>.allocate(capacity: max(1, d.count))
            d.withUnsafeBufferPointer { if let b = $0.baseAddress { p.update(from: b, count: d.count) } }
            return p
        }
        let outPtrs = (0..<parityCount).map { _ in UnsafeMutablePointer<UInt8>.allocate(capacity: blockSize) }
        defer { dataPtrs.forEach { $0.deallocate() }; outPtrs.forEach { $0.deallocate() } }
        var inArr: [UnsafePointer<UInt8>?] = dataPtrs.map { UnsafePointer($0) }
        var outArr: [UnsafeMutablePointer<UInt8>?] = outPtrs
        QRVideoFECEncode(&inArr, &lens, Int32(data.count), &outArr, Int32(parityCount), Int32(blockSize))
        for j in 0..<parityCount {
            parity[j].withUnsafeMutableBufferPointer { $0.baseAddress!.update(from: outPtrs[j], count: blockSize) }
        }
        return parity
    }
}

enum MLGF256 {
    static let exp: [UInt8] = {
        var e = [UInt8](repeating: 0, count: 512)
        var x: Int = 1
        for i in 0..<255 {
            e[i] = UInt8(x)
            e[i + 255] = UInt8(x)
            x <<= 1
            if x & 0x100 != 0 { x ^= 0x11D }
        }
        return e
    }()
    static let log: [UInt8] = {
        var l = [UInt8](repeating: 0, count: 256)
        for i in 0..<255 { l[Int(exp[i])] = UInt8(i) }
        return l
    }()
    /// GF(2^8) 乘法逆元表（0 无逆元，置 0）。
    static let inv: [UInt8] = {
        var table = [UInt8](repeating: 0, count: 256)
        for v in 1..<256 { table[v] = exp[(255 - Int(log[v])) % 255] }
        return table
    }()

    /// 生成 nanors `reed_solomon_new_static` 的**默认**校验矩阵（行优先展平）。
    ///
    /// 对照 moonlight-common-c 子模块 nanors 的 rs.c（commit b1e3c22）：
    ///     for (int j = 0; j < rs->ps; j++)
    ///         for (int i = 0; i < rs->ds; i++)
    ///             row[i] = GF2_8_INV[(rs->ps + i) ^ j];
    /// 即 P[j][i] = INV[(parityShards + i) ^ j]。4+2 时为
    /// {0x8e,0xf4,0x47,0xa7, 0xf4,0x8e,0xa7,0x47}。
    ///
    /// ⚠️ **不能用它发包**。客户端建好 RS 上下文后立刻把这 8 个字节覆盖成 OpenFEC 矩阵
    /// （`RtpAudioQueue.c:54-60`），所以链路上真正通用的是 `MLAudio.openFecParityMatrix`。
    /// 曾经按本函数现场生成 FEC → 客户端解出的分片全是乱码 → 持续爆裂声。
    static func fecMatrix(dataShards: Int, parityShards: Int) -> [UInt8] {
        var matrix = [UInt8](repeating: 0, count: dataShards * parityShards)
        for j in 0..<parityShards {
            for i in 0..<dataShards {
                matrix[j * dataShards + i] = inv[(parityShards + i) ^ j]
            }
        }
        return matrix
    }

    @inline(__always)
    static func mul(_ a: UInt8, _ b: UInt8) -> UInt8 {
        if a == 0 || b == 0 { return 0 }
        return exp[Int(log[Int(a)]) + Int(log[Int(b)])]
    }
}

// MARK: - 音频重打包

/// libopus 转码器（OpusTranscode.c）的 Swift 包装：
/// 把一个 Opus 包解码成 PCM 后按目标帧长重新编码，产出一个或多个 Opus 包。
final class PS5AudioTranscoder {
    private let handle: OpaquePointer
    private let maxOutput: Int32

    /// inputFrameMs / outputFrameMs 为每声道帧长（毫秒）
    init?(inputFrameMs: UInt32, outputFrameMs: UInt32,
          sampleRate: Int = 48000, channels: Int = 2) {
        guard inputFrameMs > 0, outputFrameMs > 0 else { return nil }
        let inSamples = Int32(sampleRate) * Int32(inputFrameMs) / 1000
        let outSamples = Int32(sampleRate) * Int32(outputFrameMs) / 1000
        guard let created = QROpusTranscoderCreate(Int32(sampleRate), Int32(channels),
                                                  inSamples, outSamples) else { return nil }
        handle = created
        maxOutput = QROpusTranscoderMaxOutputCount(created)
    }

    deinit { QROpusTranscoderDestroy(handle) }

    /// 返回若干个目标帧长的 Opus 包；失败返回空数组
    func transcode(_ opus: [UInt8]) -> [[UInt8]] {
        guard !opus.isEmpty, maxOutput > 0 else { return [] }
        var outs = [UnsafeMutablePointer<UInt8>?](repeating: nil, count: Int(maxOutput))
        var lengths = [Int32](repeating: 0, count: Int(maxOutput))
        let count = opus.withUnsafeBufferPointer { buffer in
            QROpusTranscoderProcess(handle, buffer.baseAddress, Int32(buffer.count),
                                    &outs, &lengths, maxOutput)
        }
        guard count > 0 else { return [] }
        return (0..<Int(count)).compactMap { index in
            guard let pointer = outs[index] else { return nil }
            return Array(UnsafeBufferPointer(start: pointer, count: Int(lengths[index])))
        }
    }
}

/// 把 chiaki 给出的音频帧长对齐到客户端协商的帧长。
///
/// 客户端按协商时长**固定**分配解码缓冲并向 opus 索要 `48 * duration` 个样本
/// （见 moonliight-ios 的 ArDecodeAndPlaySample），所以 10ms 的帧喂给按 5ms
/// 初始化的解码器会直接解码失败、完全静音。PS5 固定送 10ms，因此这里必须重打包。
/// 仅在两端帧长不一致时才转码；一致时原样透传，不做任何多余处理。
final class PS5AudioResampler {
    private let targetMs: UInt32
    private let sampleRate: Int
    private let channels: Int
    private var transcoder: PS5AudioTranscoder?
    private var transcoderInputMs: UInt32 = 0

    init(targetMs: UInt32, sampleRate: Int = 48000, channels: Int = 2) {
        self.targetMs = max(1, targetMs)
        self.sampleRate = sampleRate
        self.channels = channels
    }

    /// 返回需要发送的 (Opus 包, 时长 ms) 列表
    func packets(from data: Data, durationMs: UInt32) -> [([UInt8], UInt32)] {
        let bytes = [UInt8](data)
        guard !bytes.isEmpty else { return [] }
        if durationMs == targetMs { return [(bytes, targetMs)] }

        if transcoder == nil || transcoderInputMs != durationMs {
            transcoder = PS5AudioTranscoder(inputFrameMs: durationMs, outputFrameMs: targetMs,
                                           sampleRate: sampleRate, channels: channels)
            transcoderInputMs = durationMs
        }
        guard let transcoder else { return [(bytes, durationMs)] }

        let packets = transcoder.transcode(bytes)
        guard !packets.isEmpty else { return [] }
        return packets.map { ($0, targetMs) }
    }
}
