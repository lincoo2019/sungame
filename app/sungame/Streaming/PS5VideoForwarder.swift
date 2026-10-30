//
//  PS5VideoForwarder.swift
//  QuickRecorder
//
//  PS5 视频直通：chiaki 回调给出的是 Annex-B 的 H.264 裸流。
//  这里只做三件事——按 NAL 分类、缓存 SPS/PPS、在 IDR 前补齐参数集——
//  然后原样交给 Moonlight 视频通道。
//
//  既不解码也不重编码，因此：
//    · 客户端解的就是 PS5 的原始码流，没有代际损失、没有上采样；
//    · 省掉一路 1080p/4K 编码，也省掉它占用的 Wi-Fi 空口（运动画面明显更干净）。
//

import Foundation
import CoreMedia

final class PS5VideoForwarder {

    /// 送出的视频帧：(Annex-B 数据, 是否 IDR, 90kHz 时间戳)。
    /// 回调发生在独立的发送线程上（见 submitFrame），不会阻塞 chiaki 的收包线程。
    var onFrame: (([UInt8], Bool, UInt32) -> Void)?
    /// 参考链断了（丢帧且 FEC 未恢复）时回调，用于向主机请求关键帧
    var onNeedKeyFrame: (() -> Void)?
    /// 诊断日志（写入 PS5 日志，便于排查「无画面」）
    var onLog: ((String) -> Void)?

    private var sps: [UInt8] = []
    private var pps: [UInt8] = []
    /// 首个 IDR 之前不下发：客户端缺参数集/参考帧，解不出来
    private var waitingForIDR = true
    private var sampleCount = 0
    private var droppedCount = 0
    private var lastTimestamp90k: UInt32 = 0
    /// 进入「等关键帧」的时刻与上次重请求时刻
    private var gateStartedAt: CFAbsoluteTime = 0
    private var lastKeyFrameAskAt: CFAbsoluteTime = 0
    /// 等关键帧的最长时间：超过就恢复转发（宁可短暂花屏，也不要长时间黑屏/卡住）
    private let maxGateSeconds: CFAbsoluteTime = 1.5
    private let keyFrameAskInterval: CFAbsoluteTime = 0.5

    /// 发包在独立队列上进行：chiaki 是把码流回调打在它的收包线程上的，
    /// 若在这里同步做打包 + sendto，一旦 UDP 发送缓冲被写满就会阻塞收包线程，
    /// 导致内核接收缓冲溢出、直接丢包（表现为 FEC 失败、参考帧断裂）。
    private let sendQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.ps5.videoSend", qos: .userInitiated)
    private let sendLock = NSLock()
    private var pendingSends = 0
    /// 积压上限：超过就丢帧并重新要关键帧（说明链路已经跟不上，硬排队只会徒增延迟）
    private let maxPendingSends = 12

    // 周期性统计（直通模式没有编码统计，这里补上，便于判断瓶颈在 PS5→本机还是本机→客户端）
    private var statsWindowStart = CFAbsoluteTimeGetCurrent()
    private var statsFrames = 0
    private var statsBytes = 0
    private var statsDropped = 0

    /// 每次进入 PS5 模式时重置
    func reset() {
        sps = []
        pps = []
        waitingForIDR = true
        sampleCount = 0
        droppedCount = 0
        lastTimestamp90k = 0
        gateStartedAt = 0
        lastKeyFrameAskAt = 0
        sendLock.lock()
        pendingSends = 0
        sendLock.unlock()
        statsWindowStart = CFAbsoluteTimeGetCurrent()
        statsFrames = 0
        statsBytes = 0
        statsDropped = 0
    }

    /// 送入一段 chiaki 给出的 Annex-B 数据。
    /// `framesLost` 是这一帧之前丢掉的帧数，`recovered` 表示 FEC 是否已恢复：
    /// 若丢帧且未恢复，参考链是断的，转发出去客户端只会解成花屏，而且会引发
    /// 「花屏 → 客户端要 IDR → IDR 是大帧 → 更堵」的恶性循环，所以先压住不发，
    /// 等主机给关键帧；但最多只压 maxGateSeconds，避免主机迟迟不给 IDR 时画面长时间卡住。
    func handle(_ data: Data, framesLost: Int32, recovered: Bool) {
        let now = CFAbsoluteTimeGetCurrent()
        if framesLost > 0, !recovered, !waitingForIDR {
            waitingForIDR = true
            gateStartedAt = now
            lastKeyFrameAskAt = now
            onNeedKeyFrame?()
        }
        // 等关键帧期间持续向主机重请求（chiaki 自己也在要，但主机响应偏慢）；
        // 超过 maxGateSeconds 仍未拿到 IDR 就恢复转发，宁可短暂花屏也不要长时间卡住
        if waitingForIDR {
            if now - gateStartedAt > maxGateSeconds {
                waitingForIDR = false
                onLog?("等待关键帧超时（\(String(format: "%.1f", maxGateSeconds))s），恢复转发")
            } else if now - lastKeyFrameAskAt >= keyFrameAskInterval {
                lastKeyFrameAskAt = now
                onNeedKeyFrame?()
            }
        }
        guard !data.isEmpty else { return }

        let nals = Self.splitNALUnits(data)
        sampleCount += 1
        if sampleCount <= 3 {
            if nals.isEmpty {
                onLog?("样本 #\(sampleCount)：\(data.count) 字节，未找到起始码（首字节 0x\(String(format: "%02X", data.first ?? 0))）")
            } else {
                let types = nals.compactMap { $0.first.map { String($0 & 0x1F) } }
                onLog?("样本 #\(sampleCount)：\(data.count) 字节，NAL 类型 [\(types.joined(separator: ","))]")
            }
        }
        guard !nals.isEmpty else { return }

        var frameNALs: [[UInt8]] = []
        var hasIDR = false
        for nal in nals {
            guard let first = nal.first else { continue }
            switch first & 0x1F {
            case 7: // SPS
                if sps.isEmpty { onLog?("缓存 SPS（\(nal.count) 字节）") }
                sps = nal
            case 8: // PPS
                if pps.isEmpty { onLog?("缓存 PPS（\(nal.count) 字节）") }
                pps = nal
            case 5: // IDR
                hasIDR = true
                frameNALs.append(nal)
            default:
                frameNALs.append(nal)
            }
        }
        guard !frameNALs.isEmpty else { return }

        if waitingForIDR, !hasIDR {
            droppedCount += 1
            statsDropped += 1
            if droppedCount == 1 || droppedCount % 120 == 0 {
                onLog?("等关键帧中，已丢弃 \(droppedCount) 帧（累计样本 \(sampleCount)）")
            }
            reportStatsIfNeeded()
            return
        }
        if hasIDR { waitingForIDR = false }

        var output: [UInt8] = []
        output.reserveCapacity(data.count + 64)
        if hasIDR, !sps.isEmpty, !pps.isEmpty {
            // 每个 IDR 都要内嵌参数集，否则客户端会一直等 IDR
            output.append(contentsOf: [0, 0, 0, 1])
            output.append(contentsOf: sps)
            output.append(contentsOf: [0, 0, 0, 1])
            output.append(contentsOf: pps)
        }
        for nal in frameNALs {
            output.append(contentsOf: [0, 0, 0, 1])
            output.append(contentsOf: nal)
        }

        // 时间戳沿用绝对单调时钟（与窗口/桌面模式同一基准），切换目标时不会跳变
        let seconds = CMTimeGetSeconds(CMClockGetTime(CMClockGetHostTimeClock()))
        var stamp = UInt32(truncatingIfNeeded: Int64(seconds * 90000))
        if stamp <= lastTimestamp90k { stamp = lastTimestamp90k &+ 1 }
        lastTimestamp90k = stamp

        submitFrame(output, isIDR: hasIDR, timestamp90k: stamp)

        statsFrames += 1
        statsBytes += output.count
        reportStatsIfNeeded()
    }

    /// 把发送挪到独立队列，避免阻塞 chiaki 的收包线程
    private func submitFrame(_ frame: [UInt8], isIDR: Bool, timestamp90k: UInt32) {
        // 先把发送闭包读出来：避免发送线程与主线程同时读写该属性
        guard let sender = onFrame else { return }

        sendLock.lock()
        let accepted = pendingSends < maxPendingSends
        if accepted { pendingSends += 1 }
        sendLock.unlock()

        guard accepted else {
            statsDropped += 1
            onNeedKeyFrame?()
            return
        }
        sendQueue.async { [weak self] in
            sender(frame, isIDR, timestamp90k)
            self?.sendLock.lock()
            self?.pendingSends -= 1
            self?.sendLock.unlock()
        }
    }

    /// 每 5 秒汇总一次：帧率与码率反映「PS5→本机」这一段的实际情况
    private func reportStatsIfNeeded() {
        let now = CFAbsoluteTimeGetCurrent()
        let elapsed = now - statsWindowStart
        guard elapsed >= 5 else { return }
        let fps = Double(statsFrames) / elapsed
        let kbps = Double(statsBytes) * 8 / elapsed / 1000
        onLog?(String(format: "直通统计：%.1f fps，%.0f Kbps，丢弃 %d 帧", fps, kbps, statsDropped))
        statsWindowStart = now
        statsFrames = 0
        statsBytes = 0
        statsDropped = 0
    }

    /// 按起始码（00 00 01 / 00 00 00 01）切分 NAL
    private static func splitNALUnits(_ data: Data) -> [[UInt8]] {
        let bytes = [UInt8](data)
        var result: [[UInt8]] = []
        var starts: [Int] = []
        var i = 0
        while i + 2 < bytes.count {
            if bytes[i] == 0, bytes[i + 1] == 0, bytes[i + 2] == 1 {
                starts.append(i)
                i += 3
            } else if i + 3 < bytes.count, bytes[i] == 0, bytes[i + 1] == 0, bytes[i + 2] == 0, bytes[i + 3] == 1 {
                starts.append(i)
                i += 4
            } else {
                i += 1
            }
        }
        guard !starts.isEmpty else { return [] }
        for (index, start) in starts.enumerated() {
            var begin = start
            while begin < bytes.count, bytes[begin] == 0 { begin += 1 }
            guard begin < bytes.count, bytes[begin] == 1 else { continue }
            begin += 1
            let end = index + 1 < starts.count ? starts[index + 1] : bytes.count
            if end > begin {
                result.append(Array(bytes[begin..<end]))
            }
        }
        return result
    }
}
