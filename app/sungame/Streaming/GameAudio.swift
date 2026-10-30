//
//  GameAudio.swift
//  QuickRecorder
//
//  「游戏模式」的音频通路：把无窗口 Godot 游戏的音频输出抓进 Moonlight 音频通道。
//
//  背景（本任务改造）：游戏用 --display-driver offscreen 无窗口运行，画面进 IOSurface 由宿主取帧编码。
//  音频以前是在宿主侧用 macOS 14.2+ 的进程级音频捕获（Core Audio Tap）抓游戏进程输出，需要
//  TCC「音频录制」授权、且系统版本受限。现在改为**由引擎侧传出**：
//
//    · 引擎在 offscreen 显示驱动启动时往 Master 总线加一个 AudioEffectCapture 抓混音，
//      把 PCM 经「反向通道」（复用输入 socket 的反方向）回传（见 engines 侧
//      platform/macos/display_server_macos_offscreen.mm 与 drivers/metal/qr_reverse_channel.h）；
//    · 引擎把 Master 总线静音，让游戏声音不在 Mac 本地外放：
//      这样既静音本地，又彻底不需要音频录制授权、不依赖 macOS 14.2。
//
//  数据通路：
//    引擎 AudioEffectCapture（48000Hz / 2ch / Float32）→ 引擎转 Int16 小端 → 反向通道回传 →
//    本类按 5ms（240 样本）重分块 → libopus CBR 编码 → MLAudioStream.sendOpusFrame(packet, durationMs: 5)
//
//  本类只做「重分块 + 编码 + 发送」，与 MLAudioStream 解耦（只依赖一个 sendPacket 闭包），便于自测。
//

import Foundation

/// 引擎回传的 PCM → Opus CBR 编码 → 发送。
///
/// 生命周期：start() 分配累积缓冲与编码器，失败返回 false 并已打日志；stop() 幂等释放。
/// appendPCMPayload(_:) 由反向通道读取线程调用，内部加锁；**只做 PCM 累积**，
/// 攒满的帧进待编码队列（并记下到达时刻），由独立排水线程在「到达时刻 + 抖动垫」时编码发送，
/// UDP 发送再丢到独立串行队列。
///
/// 为什么编码不在读取线程就地做：视频编码（20Mbps 级）CPU 吃满时，反向通道读取线程
/// 会被挤住几十毫秒。就地编码会把这种停顿放大成「音频积压后突发补发」——发包间隔抖动 →
/// 客户端浅缓冲欠载 → 「咔哒」；同时读取线程变慢还会把视频帧通知批量合并（画面卡）。
/// 拆出后读取线程只剩「解析 + 拷贝」，排水线程只做「编码 + 投递」，各自轻量、节拍稳定。
final class MLGameAudioEncoder {

    // MARK: 配置

    /// 协商帧长（毫秒）。客户端按 48 × durationMs 固定分配解码缓冲，实测 = 5
    private let frameDurationMs: Int
    private let sampleRate = 48000
    private let channels = 2
    private let bitrateBps: Int
    private let frameSamples: Int

    private let sendPacket: ([UInt8], UInt32) -> Void
    private let onLog: (String) -> Void

    // MARK: 状态

    private var encoder: OpaquePointer?
    /// 累积缓冲：frameSamples * channels 个 Int16。攒满一帧就编码发送。
    /// 引擎回传的块大小不固定（随 AudioEffectCapture 读出量变化），必须自行重组，
    /// 既不丢样也不重复，不能假设恰好对齐 5ms。
    private var accum: UnsafeMutablePointer<Int16>?
    /// 已累积的「每声道样本数」（0 ..< frameSamples）
    private var accumCount = 0
    private let lock = NSLock()
    private var running = false

    // MARK: 待编码队列 + 排水线程

    /// 攒满的 PCM 帧（每帧 frameSamples * channels 个 Int16），由排水线程按节拍取走编码。
    /// appendPCMPayload 只入队（持锁拷贝，微秒级），编码不再占用反向通道读取线程。
    private var pendingFrames: [[Int16]] = []
    /// 待编码队列上限（= 1 秒音频）。宿主停摆到这个深度说明已严重跟不上，
    /// 丢最旧的帧保延迟有界（丢帧 = 缺口，记 statsDropped）。
    private let maxPendingFrames = 200

    /// 独立排水队列：固定节拍（frameDurationMs）编码 + 投递。
    /// 只有它会碰 encoder / dumpHandle，start/stop 通过 drainQueue.sync 与其串行化。
    private let drainQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.gamestream.audio.drain", qos: .userInteractive)
    /// 排水实时线程（取代 GCD 定时器 + sendQueue）。见 startDrainThread。
    private var drainThread: Thread?
    private var drainThreadStop = false
    private let drainThreadDone = DispatchSemaphore(value: 0)
    /// 抖动垫帧数（= 客户端播放相对采集的固定滞后）。
    ///
    /// 目标水位 = 抖动垫（帧）。PLL 会持续把积压钉在这个值附近。
    ///
    /// 取 8 帧（40ms）：实测引擎到达间隔的离群值达 13.6/16.3/18.0ms
    /// （串流日志「发送间隔最大」），20ms 垫子（旧值 4 帧）会被击穿，40ms 留足余量。
    /// 代价是音频端到端延迟多 40ms——对游戏音频无感，远小于爆音的代价。
    private let prefillFrames = 8
    /// 积压硬上限（帧）。超过就丢最旧，保证延迟有绝对上界。
    /// 正常由 PLL 把水位压在 prefillFrames 附近，这里取 +32（=200ms）纯粹兜底：
    /// 只有 PLL 失控或引擎长时间突发才会触发。
    private let highWaterExtra = 32
    private var drainStarted = false
    /// 下一个「应发时刻」（墙钟，5ms 节拍）。每次发出帧才推进一次，推进量由水位 PLL 微调；
    /// 断供时**重锚**到「现在」而不是把欠账挂账（见 drainTick）。
    ///
    /// 为什么重锚而不是挂账：挂账会把每次断供的欠账累积成一个越来越大的突发
    /// （恢复瞬间连发一串包），而**突发正是客户端「啪」的来源**。
    ///   早期「不推进网格、等帧补发」方案 vs 现在的重锚（30 分钟、抖动 ±3ms + 8% 迟到）：
    ///   缺帧 40 vs 16、最大发包间隔 15.3ms vs 13.4ms，且挂账方案会偶发 <2ms 连发突发。
    /// 重锚丢掉的速率由 PLL 自动补偿（水位偏低 → 步长变大 → 加速追赶），
    /// 所以不会造成早期担心的「发送率低于到达率 → 积压无界增长」。
    private var nextSlotAt: CFAbsoluteTime = 0
    /// 缓冲水位 PLL 的比例增益。作用在**每次发送的步长**上（与唤醒频率无关）：
    ///   skip = 5ms × (1 − kp × err)，err = 发送前水位 − 目标水位（截到 ±pllErrorClamp 帧）。
    /// 水位偏高 → adjust>0 → skip 变小 → 发得更勤 → 水位回落；
    /// 水位偏低 → adjust<0 → skip 变大 → 发得慢些 → 垫子养回。
    /// 取 0.20：水位每偏 1 帧，步长变 20%（限幅内），几十帧内收敛且不振荡。
    private let pllGain = 0.20
    /// PLL 单次步长调节限幅（相对 5ms 的比例）。0.20 → 间隔 4~6ms，
    /// 即包率 167~250 包/秒。这个范围足够跟上引擎 ±5% 的速率偏差
    /// （游戏音频场景采样率不会差更多），又不会造成可感的音调/唇音漂移。
    private let pllMaxRateAdjust = 0.20
    /// PLL 误差截断（帧）。防止刚启动或大突发时把步长顶到限幅而变成 bang-bang 振荡
    /// （模拟实测：不截断时 kp≥0.2 会振荡，水位掉到 6.9 而非 8）。
    private let pllErrorClamp = 8.0

    // MARK: 发送队列

    /// 把 UDP 发送挪到独立串行队列：反向通道读取线程负责解析与编码，
    /// 不应被 socket 系统调用（可能阻塞）拖住。
    private let sendQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.gamestream.audio.send", qos: .userInitiated)

    /// 统计日志单独一个队列。以前它和 UDP 发送共用 sendQueue：写日志（落盘 + 刷新 UI）
    /// 会排在音频包后面，让本该准点发出的 5ms 包晚几十毫秒 → 客户端缓冲见底 → 「啪」。
    private let statsQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.gamestream.audio.stats", qos: .utility)

    // MARK: 统计

    private var statsTimer: DispatchSourceTimer?
    private var statsAt = CFAbsoluteTimeGetCurrent()
    private var statsPackets = 0
    private var statsBytes = 0
    private var statsPCMFrames = 0
    private var statsDropped = 0
    /// 诊断：相邻样本跳变 >|20000|（约 61% 满幅）的次数 = 源 PCM 里的疑似「爆点」。
    /// 若客户端听到微爆而这里一直为 0，问题在网络/客户端；>0 说明游戏/引擎混音本身就有断点。
    private var statsJumps = 0
    private var lastSample: (Int32, Int32) = (0, 0)
    /// QR_GAMESTREAM_PCM_DUMP=/path.raw 时把原始 PCM（48k/2ch/s16le）落盘，可用
    /// `ffplay -f s16le -ar 48000 -ac 2 /path.raw` 直接听，判断爆音是否已在宿主侧存在。
    private var pcmDumpHandle: FileHandle?
    private var opusDumpHandle: FileHandle?
    private var localDecoder: OpaquePointer?
    private let dumpWriteQueue = DispatchQueue(label: "QR.gamestream.audio.dump", qos: .utility)

    /// PCM 落盘开关，任一满足即开启（不依赖启动方式）：
    ///   · 环境变量 QR_GAMESTREAM_PCM_DUMP=路径；
    ///   · 文件 /tmp/qr_pcm_dump.enable 存在（→ /tmp/qr_pcm.raw）；
    ///   · `defaults write com.lihaoyun6.QuickRecorder QRGameStreamPCMDump -bool YES`（→ /tmp/qr_pcm.raw）。
    static func pcmDumpPath() -> String? {
        if let p = ProcessInfo.processInfo.environment["QR_GAMESTREAM_PCM_DUMP"], !p.isEmpty { return p }
        if FileManager.default.fileExists(atPath: "/tmp/qr_pcm_dump.enable") { return "/tmp/qr_pcm.raw" }
        if UserDefaults.standard.bool(forKey: "QRGameStreamPCMDump") { return "/tmp/qr_pcm.raw" }
        return nil
    }
    /// 相邻两个音频包「投递」时刻的最大间隔（毫秒）。客户端按 5ms 一包解码，
    /// 只要这个值明显大于 5ms，就说明宿主给客户端留了空洞（客户端会欠载 → 一声「咔哒」）。
    /// 用它来定位「咔哒」是否出在宿主发包侧。
    private var statsMaxGapMs: Double = 0
    /// 本窗口「定时器回调比计划槽位晚多少」的最大值（毫秒）。
    /// 用它区分两种空洞来源：**缺帧=0 而本值很大** → 不是没帧可发，而是**调度把回调推迟了**，
    /// 这段时间在客户端时间线上就是空洞（moonlight-common-c 没有音频抖动缓冲 → 一声「咔哒」）。
    private var statsMaxTimerLateMs: Double = 0
    private var lastDispatchAt: CFAbsoluteTime?
    /// 诊断：本窗口「到点却无帧可发」的**断供次数**（缺帧）。
    ///
    /// 口径：一次**连续**断供只计 1 次，用 starvedSinceLastSend 标记，成功发出帧后清零。
    /// 这样计的是「客户端时间线上出现了一个空洞」的事件数（≈ 能听出来的欠载次数），
    /// 而不是空洞里含多少个 5ms 槽、更不是 1ms 唤醒了多少次。
    /// 旧口径按每一拍计数，恒饱和在 400 拍/秒 × 5 秒 = 2000，把真故障完全淹没。
    /// 健康值 ≈ 0；持续 >0 说明垫子不足以吸收引擎到达抖动，应加大 prefillFrames。
    private var statsStarvedTicks = 0
    /// 当前是否正处于一段连续断供中（用于把一段断供只计 1 次）。
    private var starvedSinceLastSend = false

    /// 诊断：把交给 sendPacket 的 Opus 包按 [UInt32 LE 长度][负载] 追加落盘，供离线解码验证。
    /// 仅当设置 QR_GAMESTREAM_AUDIO_DUMP 时启用。
    private var dumpHandle: FileHandle?
    private var dumpURL: URL?

    // MARK: 初始化

    init(frameDurationMs: Int, bitrateBps: Int = 96000,
         sendPacket: @escaping ([UInt8], UInt32) -> Void,
         onLog: @escaping (String) -> Void) {
        self.frameDurationMs = max(1, frameDurationMs)
        self.frameSamples = 48000 * max(1, frameDurationMs) / 1000
        self.bitrateBps = bitrateBps > 0 ? bitrateBps : 96000
        self.sendPacket = sendPacket
        self.onLog = onLog
    }

    deinit { stop() }

    // MARK: 启动 / 停止

    /// 准备编码链路。返回 false 表示不可用（已打清晰日志），调用方无需处理，串流照常。
    @discardableResult
    func start() -> Bool {
        lock.lock()
        defer { lock.unlock() }
        if running { return true }

        let accumBuffer = UnsafeMutablePointer<Int16>.allocate(capacity: frameSamples * channels)
        accumBuffer.initialize(repeating: 0, count: frameSamples * channels)
        accum = accumBuffer
        accumCount = 0

        encoder = QROpusPCMEncoderCreate(Int32(sampleRate), Int32(channels), Int32(frameSamples), Int32(bitrateBps))
        guard encoder != nil else {
            accum?.deallocate(); accum = nil
            onLog("音频：libopus PCM 编码器创建失败，本次不发送游戏音频（视频/输入不受影响）")
            return false
        }

        running = true
        pendingFrames.removeAll(keepingCapacity: true)
        drainStarted = false
        openDumpIfRequested()
        if let path = MLGameAudioEncoder.pcmDumpPath() {
            FileManager.default.createFile(atPath: path, contents: nil)
            pcmDumpHandle = FileHandle(forWritingAtPath: path)
            // 同时把「本地 Opus 编码→解码」后的 PCM 落盘（与客户端解码同一条路），
            // 用来判断 Opus 编码本身是否引入爆音：原始干净而这份有爆音 → 编码器问题。
            let opusPath = (path as NSString).deletingPathExtension + "_opus.raw"
            FileManager.default.createFile(atPath: opusPath, contents: nil)
            opusDumpHandle = FileHandle(forWritingAtPath: opusPath)
            localDecoder = QROpusDecoderCreate(Int32(sampleRate), Int32(channels))
            onLog("音频诊断：原始 PCM → \(path)，Opus 解码 PCM → \(opusPath)（48k/2ch/s16le）")
        }
        startDrainThread()
        startStatsTimer()
        onLog("音频：引擎侧混音捕获已接管（AudioEffectCapture 回传 PCM）；编码=Opus CBR \(bitrateBps / 1000)Kbps 帧长=\(frameDurationMs)ms(每声道 \(frameSamples) 样本) 采样率=\(sampleRate)")
        return true
    }

    /// 释放全部资源。可重复调用。
    func stop() {
        lock.lock()
        let wasRunning = running
        running = false
        lock.unlock()
        statsTimer?.cancel(); statsTimer = nil
        stopDrainThread()
        drainQueue.sync {}
        guard wasRunning else { return }
        lock.lock()
        pendingFrames.removeAll()
        if let encoder { QROpusPCMEncoderDestroy(encoder) }
        encoder = nil
        if let accum { accum.deallocate() }
        accum = nil
        accumCount = 0
        lock.unlock()
        if let handle = dumpHandle { try? handle.close() }
        lock.lock(); let pcmHandle = pcmDumpHandle; pcmDumpHandle = nil; lock.unlock()
        try? pcmHandle?.close()
        dumpWriteQueue.sync {}
        try? opusDumpHandle?.close(); opusDumpHandle = nil
        if let d = localDecoder { QROpusDecoderDestroy(d) }; localDecoder = nil
        dumpHandle = nil
        if let url = dumpURL { onLog("音频包已落盘：\(url.path)") }
        onLog("音频：编码发送已停止")
    }

    // MARK: 反向通道读取线程入口

    /// 追加一段交织 Int16 小端立体声 PCM（由反向通道读取线程调用）。
    /// 只做累积与入队：攒满 frameSamples 就把整帧拷进待编码队列，并**立刻唤醒排水线程**
    /// （采集时钟驱动：引擎一批恰好是一个 5ms 帧，来了就发，发包节奏跟着采集时钟走）；
    /// 跨调用保留不足一帧的尾巴，保证不丢样、不重复。
    /// 这里绝不做 Opus 编码——它必须留在轻量路径上，避免把读取线程的停顿放大成发包突发。
    func appendPCMPayload(_ bytes: [UInt8]) {
        guard !bytes.isEmpty else { return }
        lock.lock()
        defer { lock.unlock() }
        guard running, let accum, encoder != nil else { return }

        let sampleStride = channels * 2   // 每帧 2 声道 × 2 字节
        let frames = bytes.count / sampleStride
        var bi = 0
        for _ in 0..<frames {
            for c in 0..<channels {
                let lo = UInt16(bytes[bi])
                let hi = UInt16(bytes[bi + 1])
                let v = Int16(bitPattern: lo | (hi << 8))
                accum[accumCount * channels + c] = v
                let prev = c == 0 ? lastSample.0 : lastSample.1
                if abs(Int32(v) - prev) > 20000 { statsJumps += 1 }
                if c == 0 { lastSample.0 = Int32(v) } else { lastSample.1 = Int32(v) }
                bi += 2
            }
            accumCount += 1
            if accumCount == frameSamples {
                accumCount = 0
                enqueueFrameLocked(accum)
            }
        }
        statsPCMFrames += frames
        if let h = pcmDumpHandle { try? h.write(contentsOf: Data(bytes[0..<(frames * sampleStride)])) }
        if frames > 0 {
            // 唤醒排水线程。帧不会因此被立刻发出——发送时刻由 5ms 网格决定（见 drainTick）。
            // 这次唤醒的价值在**垫子被击穿后的恢复**：若调度中断导致帧积压，
            // 帧一到就唤醒能让排水立刻补上欠槽，而不必干等下一次定时器（最多 2ms）。
            //
            // 为什么不「到达即发」：那等于把引擎的到达抖动原样透传到线上。
            // moonlight-common-c **没有抖动缓冲**（AudioStream.c 收到包直接
            // decodeAndPlaySample，FEC 重组只等 RTPQ_OOS_WAIT_TIME_MS=10ms），
            // 所以任何到达抖动都会变成客户端欠载（PLC「微爆」）。
            // 服务器必须自己把这个抖动垫掉——这正是 Sunshine 靠 OS 音频回调天然获得、
            // 而我们不能依赖的东西。
            // 实时排水线程每 ≤1ms 自行轮询，这里不再向 GCD 投递（投递本身会被调度推迟）。
        }
    }

    /// 调用方已持锁。把攒满的一帧 PCM 复制进待编码队列；超上限丢最旧（记 statsDropped）。
    private func enqueueFrameLocked(_ src: UnsafeMutablePointer<Int16>) {
        let sampleCount = frameSamples * channels
        var frame = [Int16](repeating: 0, count: sampleCount)
        frame.withUnsafeMutableBufferPointer { dst in
            dst.baseAddress?.update(from: src, count: sampleCount)
        }
        if pendingFrames.count >= maxPendingFrames {
            pendingFrames.removeFirst()
            statsDropped += 1
        }
        pendingFrames.append(frame)
    }

    // MARK: 排水线程：水位 PLL 控制的编码 + 投递

    /// 排水定时器：只驱动「到点就发」，不做任何「欠账补发」。
    ///
    /// 发包时刻由 nextSlotAt（5ms 节拍 + 水位 PLL 微调）决定，与帧的到达抖动无关。
    /// 定时器因此不是节拍源，只是「检查是否到点」的唤醒源。
    /// 取 1ms 而不是 2ms：到点时刻会落在定时器的任意相位上，唤醒粒度直接等于
    /// 发送时刻的量化误差（2ms 粒度 → 发包间隔 6/4/6/4 交替；1ms 粒度基本贴合 5ms）。
    /// 客户端没有抖动缓冲，这 1ms 值得省；每秒 1000 次空转开销可忽略。
    /// 实时排水线程：Mach time-constraint 策略（与 CoreAudio IO 线程同类），
    /// 用 mach_wait_until 精确睡到「下一个应发时刻」或 1ms 后，不受 GCD 线程池/QoS 合并影响。
    /// 日志实测 GCD 定时器在负载下「回调迟到最大 14.6ms」，这是剩余微爆的另一来源。
    private func startDrainThread() {
        drainThreadStop = false
        let thread = Thread { [weak self] in
            MLGameAudioEncoder.promoteToRealtime(periodMs: 1.0)
            var tb = mach_timebase_info_data_t()
            mach_timebase_info(&tb)
            let nsToAbs = Double(tb.denom) / Double(tb.numer)
            while true {
                guard let self else { break }
                self.lock.lock(); let stop = self.drainThreadStop; let slot = self.nextSlotAt; let started = self.drainStarted; self.lock.unlock()
                if stop { break }
                self.drainQueueSyncTick()
                // 睡到下一个槽位（最多 1ms），留 150µs 余量
                let now = CFAbsoluteTimeGetCurrent()
                var waitS = 0.001
                if started { waitS = max(0.0001, min(0.001, slot - now - 0.00015)) }
                mach_wait_until(mach_absolute_time() + UInt64(waitS * 1e9 * nsToAbs))
            }
            self?.drainThreadDone.signal()
        }
        thread.name = "QR.gamestream.audio.drain.rt"
        thread.qualityOfService = .userInteractive
        drainThread = thread
        thread.start()
    }

    private func stopDrainThread() {
        guard drainThread != nil else { return }
        lock.lock(); drainThreadStop = true; lock.unlock()
        // deinit 恰好发生在排水线程上时不能等自己
        if Thread.current !== drainThread { drainThreadDone.wait() }
        drainThread = nil
    }

    /// 在实时线程上跑一拍；仍经 drainQueue.sync 与 start/stop 串行化（无竞争时开销极小）。
    private func drainQueueSyncTick() {
        drainQueue.sync { self.drainTick() }
    }

    static func promoteToRealtime(periodMs: Double) {
        var tb = mach_timebase_info_data_t()
        mach_timebase_info(&tb)
        let msToAbs = 1_000_000.0 * Double(tb.denom) / Double(tb.numer)
        var policy = thread_time_constraint_policy_data_t(
            period: UInt32(periodMs * msToAbs),
            computation: UInt32(0.3 * msToAbs),
            constraint: UInt32(0.8 * msToAbs),
            preemptible: 1)
        let count = mach_msg_type_number_t(MemoryLayout<thread_time_constraint_policy_data_t>.size / MemoryLayout<integer_t>.size)
        _ = withUnsafeMutablePointer(to: &policy) { ptr in
            ptr.withMemoryRebound(to: integer_t.self, capacity: Int(count)) {
                thread_policy_set(pthread_mach_thread_np(pthread_self()), thread_policy_flavor_t(THREAD_TIME_CONSTRAINT_POLICY), $0, count)
            }
        }
    }

    /// 每 1ms 一拍（外加「帧到达即唤醒」）：按 5ms 节拍发送，并用**积压水位 PLL** 把
    /// 抖动垫钉在 prefillFrames 帧。
    ///
    /// 为什么必须闭环（这是「持续微爆」的根因与修复）：
    /// 客户端 moonlight-common-c 没有抖动缓冲，宿主发包间隔的空洞会直接变成
    /// 客户端欠载（PLC「啪」）。所以宿主必须自己把引擎的到达抖动垫掉。
    /// 但如果只做「固定 5ms 网格 + 预填 8 帧」，水位
    /// `L = 垫子 + 到达数 - 发送数` 在有抖动时是**随机游走**：即使发包率与到达率严格相等
    /// （漂移 = 0），它也迟早漫游到 0。一旦 L=0，之后每个到达离群值都直接变成线上空洞。
    /// 实测证据：committed 基线「到达即发」的最大发包间隔 16.9ms，与引擎离群值
    /// 13.6/16.3/18.0ms 一致——抖动被原样透传，垫子形同虚设。
    ///
    /// 修复：用积压水位做反馈，把**发送步长**当被控量。
    ///   L > 目标 → 步长变小 → 发得更勤 → L 回落；
    ///   L < 目标 → 步长变大 → 发得慢些 → 垫子养回。
    /// 限幅 ±20%（包率 167~250），足以跟上引擎 ±5% 速率偏差，又不致可感音调漂移。
    /// 模拟（10~30 分钟，抖动 ±3ms + 8% 迟到至 18ms，引擎 −5%~+1%）：
    ///   缺帧 2~16 次、丢弃 0、水位首/末 1/4 均 ≈8.4（零漂移）、
    ///   p50 发包间隔 5.00ms、最大发包间隔 13.4~15.7ms
    ///   （committed 基线「到达即发」恒为 16.9ms，即把引擎离群值原样透传）。
    private func drainTick() {
        var frames: [[Int16]] = []
        var justStarted = false
        lock.lock()
        guard running else { lock.unlock(); return }
        let now = CFAbsoluteTimeGetCurrent()
        let period = Double(frameDurationMs) / 1000.0
        if !drainStarted {
            guard pendingFrames.count >= prefillFrames else { lock.unlock(); return }
            drainStarted = true
            justStarted = true
            // 垫子已就位，网格从「现在」起算（不补历史欠账，避免一上来就炸一串包）。
            nextSlotAt = now
        }
        // 到点就发。**每拍最多 1 帧**：绝不在一拍内连发，
        // 因为突发本身就是客户端「啪」的来源（客户端没有抖动缓冲）。
        var take = 0
        if now >= nextSlotAt {
            if pendingFrames.isEmpty {
                // 到点却无帧 = 真断供（引擎这一拍没供上）。
                // 一段连续断供只计 1 次：计的是「客户端听到一次空洞」的事件数，
                // 而不是 1ms 唤醒了多少次（旧口径因此恒饱和在 400×5=2000）。
                if !starvedSinceLastSend {
                    starvedSinceLastSend = true
                    statsStarvedTicks += 1
                }
                // 重锚：把这拍欠账丢掉，网格从「现在」重新起算。
                // 丢掉的速率由 PLL 补偿（水位偏低 → 步长变大 → 加速追赶），
                // 因此不会像早期「不重锚」方案那样把欠账累积成恢复瞬间的大突发。
                nextSlotAt = now
            } else {
                take = 1
                // 定时器迟到量：回调比计划槽位晚了多久。它是「回调被系统推迟」的直接度量——
                // 迟到量明显 >0 就在客户端时间线上留洞（客户端无音频抖动缓冲，洞=「咔哒」）。
                let lateMs = (now - nextSlotAt) * 1000
                if lateMs > statsMaxTimerLateMs { statsMaxTimerLateMs = lateMs }
                // MARK: 缓冲水位 PLL——这是抗微爆的核心，不能删。
                //
                // 为什么必须有它：单纯「固定 5ms 网格 + 预填垫子」无法长期守住垫子。水位
                // L = 垫子 + 到达数 - 发送数 在有抖动时是**随机游走**；即使发送率与到达率
                // 严格相等（漂移 = 0），它也迟早漫游到 0（赌徒破产）。一旦 L=0，之后每个
                // 到达离群值都直接变成线上空洞 → 客户端 PLC「啪」。
                // 实测证据：committed 基线「到达即发」的最大发包间隔 16.9ms，与引擎离群值
                // 13.6/16.3/18.0ms 一致——抖动被原样透传，垫子形同虚设。
                //
                // 解法：把**发送步长**当被控量，用发送前的水位做反馈，把 L 钉在 prefillFrames。
                // 步长只在「真的发出帧」时推进一次，所以增益与定时器唤醒频率完全无关
                // （定时器改成 2ms 或 0.5ms 都不会改变控制特性）——这是它可靠的关键。
                //
                // 注意：这里**不能**再加「nextSlotAt 不得超过 now+period」的钳位。
                // adjust<0（需要减速、把垫子养回来）时步长必须能 >period，
                // 那个钳位会把整个减速半边吃掉，使控制器只能加速不能减速，
                // 水位被单调压到 0（模拟实测：水位停在 6.9 而非目标 8.0，缺帧 561 次）。
                var error = Double(pendingFrames.count - prefillFrames)
                error = max(-pllErrorClamp, min(pllErrorClamp, error))
                let adjust = max(-pllMaxRateAdjust, min(pllMaxRateAdjust, pllGain * error))
                nextSlotAt += period * (1 - adjust)
            }
        }
        if take > 0 {
            frames = Array(pendingFrames.prefix(take))
            pendingFrames.removeFirst(take)
            starvedSinceLastSend = false
        }
        // 延迟兜底：积压超过硬上限才丢最旧（正常 PLL 已把水位压住，几乎不触发）。
        let hardCap = prefillFrames + highWaterExtra
        if pendingFrames.count > hardCap {
            let excess = pendingFrames.count - hardCap
            pendingFrames.removeFirst(excess)
            statsDropped += excess
        }
        lock.unlock()
        if justStarted {
            onLog("音频排水：预填 \(prefillFrames) 帧（\(prefillFrames * frameDurationMs)ms 抖动垫）就位，按 \(frameDurationMs)ms 网格 + 水位 PLL（目标 \(prefillFrames) 帧）发送，包率 \(1000 / frameDurationMs) 包/秒 ±\(Int(pllMaxRateAdjust * 100))%")
        }
        for frame in frames {
            encodeAndSend(frame)
        }
    }

    /// 只在排水队列上调用（encoder 生命周期由 stop() 的 drainQueue.sync 保证）。
    private func encodeAndSend(_ frame: [Int16]) {
        guard let encoder else { return }
        var packet: [UInt8] = []
        frame.withUnsafeBufferPointer { buf in
            var out: UnsafeMutablePointer<UInt8>?
            let length = QROpusPCMEncoderEncode(encoder, buf.baseAddress, &out)
            guard length > 0, let out else { return }
            packet = Array(UnsafeBufferPointer(start: out, count: Int(length)))
        }
        guard !packet.isEmpty else {
            lock.lock(); statsDropped += 1; lock.unlock()
            return
        }
        let now = CFAbsoluteTimeGetCurrent()
        lock.lock()
        if let last = lastDispatchAt {
            let gapMs = (now - last) * 1000
            if gapMs > statsMaxGapMs { statsMaxGapMs = gapMs }
        }
        lastDispatchAt = now
        statsPackets += 1
        statsBytes += packet.count
        lock.unlock()
        writeDump(packet)
        if let dec = localDecoder, let h = opusDumpHandle {
            var pcm = [Int16](repeating: 0, count: frameSamples * channels)
            let n = packet.withUnsafeBufferPointer { pb in
                pcm.withUnsafeMutableBufferPointer { QROpusDecoderDecode(dec, pb.baseAddress, Int32(packet.count), $0.baseAddress, Int32(frameSamples)) }
            }
            if n > 0 {
                let data = pcm.withUnsafeBytes { Data($0.prefix(Int(n) * channels * 2)) }
                dumpWriteQueue.async { try? h.write(contentsOf: data) }
            }
        }
        let duration = UInt32(frameDurationMs)
        // 直接在实时排水线程里发：sendto 是 MSG_DONTWAIT 非阻塞（最坏 ~150µs）。
        // 以前丢到 qos=.userInitiated 的 sendQueue，GCD 在视频编码/连发吃满 CPU 时会把它推迟
        // 十几~二十几毫秒——日志实测「音频 已转发 … 发送间隔最大=24.3ms/29.0ms」，而同一窗口
        // 编码侧「发送间隔最大 5.4ms」：洞出在 sendQueue，正好超出客户端乱序窗口 → 微爆。
        sendPacket(packet, duration)
    }

    // MARK: 统计日志（每 5 秒一行）

    private func startStatsTimer() {
        let timer = DispatchSource.makeTimerSource(queue: statsQueue)
        timer.schedule(deadline: .now() + 5, repeating: 5)
        timer.setEventHandler { [weak self] in self?.reportStats() }
        statsTimer = timer
        statsAt = CFAbsoluteTimeGetCurrent()
        timer.resume()
    }

    private func reportStats() {
        lock.lock()
        let elapsed = CFAbsoluteTimeGetCurrent() - statsAt
        let packets = statsPackets
        let bytes = statsBytes
        let pcmFrames = statsPCMFrames
        let dropped = statsDropped
        let backlog = pendingFrames.count
        let maxGapMs = statsMaxGapMs
        let starved = statsStarvedTicks
        let jumps = statsJumps
        statsJumps = 0
        let maxLateMs = statsMaxTimerLateMs
        statsAt = CFAbsoluteTimeGetCurrent()
        statsPackets = 0
        statsBytes = 0
        statsPCMFrames = 0
        statsDropped = 0
        statsMaxGapMs = 0
        statsStarvedTicks = 0
        statsMaxTimerLateMs = 0
        lock.unlock()

        let fps = elapsed > 0 ? Double(packets) / elapsed : 0
        let pcmFps = elapsed > 0 ? Double(pcmFrames) / elapsed : 0
        let avgBytes = packets > 0 ? Double(bytes) / Double(packets) : 0
        let kbps = elapsed > 0 ? Double(bytes) * 8 / elapsed / 1000 : 0
        // 排水积压 = 当前抖动垫水位，正常应稳定在 prefillFrames 附近（PLL 在控）。
        // 缺帧 = 「到点却无帧可发」的**连续断供次数**（一段连续断供只计 1 次）
        //        = 客户端时间线上出现空洞的次数 ≈ 能听出来的欠载（PLC「啪」）。
        //        按事件数统计（不再按 1ms 唤醒次数，旧口径恒饱和在 400×5=2000）。
        // 健康标准：缺帧 0、排水积压 ≈ prefillFrames、发送间隔最大 < 8ms。
        onLog("音频源 PCM 疑似爆点(相邻样本跳变>20000)=\(jumps)")
        onLog(String(format: "音频：引擎回传 PCM %.0f 帧/秒；编码发送 %.1f 帧/秒 平均包长 %.0f 字节 %.0f Kbps；排水积压=%d 发送间隔最大 %.1f ms（回调迟到最大 %.1f ms）缺帧=%d次 累计丢弃=%d",
                     pcmFps, fps, avgBytes, kbps, backlog, maxGapMs, maxLateMs, starved, dropped))
    }

    // MARK: 自测：Opus 包落盘

    private func openDumpIfRequested() {
        guard let path = ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_DUMP"], !path.isEmpty else { return }
        let url = URL(fileURLWithPath: path)
        FileManager.default.createFile(atPath: url.path, contents: nil)
        guard let handle = try? FileHandle(forWritingTo: url) else { return }
        dumpHandle = handle
        dumpURL = url
    }

    private func writeDump(_ packet: [UInt8]) {
        guard let handle = dumpHandle else { return }
        var length = UInt32(packet.count).littleEndian
        var record = Data(bytes: &length, count: 4)
        record.append(contentsOf: packet)
        try? handle.write(contentsOf: record)
    }
}

// MARK: - 端到端音频自测（QR_GAMESTREAM_AUDIO_SELFTEST=1）
//
//  目的：在真实宿主进程里跑一遍完整链路——
//    启动游戏（/tmp/gdtest 里在持续播放 440Hz 测试音）→ 引擎 AudioEffectCapture 抓混音 →
//    反向通道回传 PCM → 本类重分块 → Opus 编码 → MLAudioStream.sendOpusFrame（真实 RTP，UDP 48000）。
//  本自测充当「Moonlight 客户端」：先向 48000 端口发 SS_PING 让 MLAudioStream 学到对端，
//  再收 RTP，去掉 12 字节 RTP 头得到 Opus 负载，断言：
//    · 包长恒定（CBR）；· 时间戳每包 +5ms；· 帧率 ≈ 200；
//  并把负载按 [UInt32 LE 长度][负载] 写入 /tmp/qr_audio_packets.bin，
//  交给外部工具（/tmp/opa_decode）用「客户端一模一样的 multistream 解码」解回 PCM 做频率估计。
//

enum MLGameAudioSelfTest {

    private static func emit(_ line: String) {
        print("[QRGAMEAUDIO] \(line)")
        fflush(stdout)
        // 自测可能以 `open` 方式启动，此时 stdout 不进终端，所以同时追加写文件，保证报告可被外部读取。
        let path = ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_SELFTEST_LOG"] ?? "/tmp/qr_audio_selftest.log"
        if let handle = FileHandle(forWritingAtPath: path) {
            handle.seekToEndOfFile()
            handle.write(Data(("[QRGAMEAUDIO] " + line + "\n").utf8))
            try? handle.close()
        } else {
            FileManager.default.createFile(atPath: path, contents: Data(("[QRGAMEAUDIO] " + line + "\n").utf8))
        }
    }

    /// 环境变量开关
    static func runIfRequested() {
        guard ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_SELFTEST"] == "1" else { return }
        let env = ProcessInfo.processInfo.environment
        let executable = env["QR_GAMESTREAM_EXEC"] ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let targetPackets = Int(env["QR_GAMESTREAM_AUDIO_PACKETS"] ?? "") ?? 800
        let dumpPath = env["QR_GAMESTREAM_AUDIO_DUMP"] ?? "/tmp/qr_audio_packets.bin"
        // 等待上限（秒）。自测断言「关闭/降级时 0 个音频包」时可用更短的等待上限，避免空等 40s。
        let deadlineSecs = Double(env["QR_GAMESTREAM_AUDIO_DEADLINE"] ?? "") ?? 40.0
        DispatchQueue.global(qos: .userInitiated).async {
            run(executablePath: executable, projectPath: project, targetPackets: targetPackets,
                dumpPath: dumpPath, deadlineSecs: deadlineSecs)
        }
    }

    private static func run(executablePath: String, projectPath: String, targetPackets: Int,
                            dumpPath: String, deadlineSecs: Double) {
        emit("=== 音频端到端自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")

        // 自测用 ping 负载（16 字节），与 MLAudioStream 约定一致
        let pingPayload = Array("QRGAMEAUDIOTEST!".utf8)
        guard pingPayload.count == 16 else { emit("内部错误：ping 负载长度不对"); exit(1) }

        let audio: MLAudioStream
        do {
            audio = try MLAudioStream(pingPayload: pingPayload) { emit("stream: \($0)") }
        } catch {
            emit("创建 MLAudioStream 失败（端口 48000 被占用？）：\(error.localizedDescription)")
            exit(1)
        }
        audio.start()

        // 可选：模拟真实会话中的音频加密（客户端 encryptionFlags=ENCFLG_ALL 会请求音频加密）。
        // 提供 QR_GAMESTREAM_AUDIO_ENC_KEY(hex，16 字节) 与 QR_GAMESTREAM_AUDIO_ENC_ID 时启用，
        // 供外部工具按客户端算法（AES-128-CBC，IV=BE32(riKeyId+seq)）解密后做解码验证。
        let encKey = ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_ENC_KEY"].flatMap { $0.mlHexToBytes() }
        let encID = UInt32(ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_ENC_ID"] ?? "") ?? 0
        if let encKey, encKey.count == 16 {
            audio.configureAudioEncryption(key: encKey, riKeyID: encID, enabled: true)
            emit("音频加密已启用：AES-128-CBC riKeyID=\(encID)")
        }

        // 冒充 Moonlight 客户端：一个 UDP socket，周期性发 ping 学习对端，然后收 RTP。
        let clientFD = socket(AF_INET, SOCK_DGRAM, 0)
        guard clientFD >= 0 else { emit("创建客户端 socket 失败"); exit(1) }
        var serverAddr = sockaddr_in()
        serverAddr.sin_len = UInt8(MemoryLayout<sockaddr_in>.size)
        serverAddr.sin_family = sa_family_t(AF_INET)
        serverAddr.sin_port = in_port_t(UInt16(MLPort.audio)).bigEndian
        serverAddr.sin_addr.s_addr = inet_addr("127.0.0.1")
        var tv = timeval(tv_sec: 0, tv_usec: 300_000)
        setsockopt(clientFD, SOL_SOCKET, SO_RCVTIMEO, &tv, socklen_t(MemoryLayout<timeval>.size))

        let pingStop = NSLock()
        var pinging = true
        let pingThread = Thread {
            var seq: UInt32 = 0
            while true {
                pingStop.lock(); let go = pinging; pingStop.unlock()
                if !go { break }
                var packet = pingPayload
                packet.append(UInt8((seq >> 24) & 0xFF)); packet.append(UInt8((seq >> 16) & 0xFF))
                packet.append(UInt8((seq >> 8) & 0xFF)); packet.append(UInt8(seq & 0xFF))
                _ = packet.withUnsafeBytes { raw -> Int in
                    withUnsafePointer(to: &serverAddr) { addrPtr in
                        addrPtr.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                            sendto(clientFD, raw.baseAddress, packet.count, 0, sa, socklen_t(MemoryLayout<sockaddr_in>.size))
                        }
                    }
                }
                seq &+= 1
                Thread.sleep(forTimeInterval: 0.2)
            }
        }
        pingThread.start()

        // 起游戏会话，把音频通道接进去
        let session = MLGameStreamSession(width: 1280, height: 720, fps: 30, bitrateBps: 8_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        session.audioStream = audio
        session.audioChannels = 2
        session.audioPacketDurationMs = 5
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { emit("out: \($0)") }
        do {
            try session.start()
        } catch {
            emit("会话启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // 收 RTP。线上现在混有两类数据报（见 MLAudioStream.sendOpusFrame / emitFecBlock）：
        //   · 音频数据包（PT 97）——每 5ms 一包；
        //   · FEC 包（PT 127）——4+2 块的 2 个校验分片，块尾两连发。
        // 断言只看「按 seq 去重后的数据包」：FEC 单独计数，不混入 CBR/时间戳断言。
        var packets: [[UInt8]] = []       // 去重后数据包负载（去掉 12 字节 RTP 头），供外部解码
        var lengths: [Int: Int] = [:]
        var timestamps: [UInt32] = []
        var headerOK = true
        var dataSeqsSeen = Set<UInt16>()
        // 每个「新数据包」（按 seq 去重后）的到达时刻。相邻两包到达间隔即客户端看到的发包节拍：
        // 正常应恒为 ~5ms；若出现 ~10ms 尖峰，说明宿主漏发了某一拍（客户端会欠载 → 一声「咔哒」）。
        var arrivalTimes: [Double] = []
        var duplicateDatagrams = 0
        var fecDatagrams = 0
        var otherDatagrams = 0
        var firstSeq: UInt16? = nil
        // 前若干个完整的 UDP 数据报（含 12 字节 RTP 头），用于与 Sunshine 布局逐字节对照
        var rawPackets: [Data] = []
        // 完整 RTP 数据报的落盘上限（QR_GAMESTREAM_AUDIO_RTP_LIMIT，默认 16）。
        let rawPacketLimit = Int(ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_RTP_LIMIT"] ?? "") ?? 16
        let deadline = Date().addingTimeInterval(deadlineSecs)
        var buffer = [UInt8](repeating: 0, count: 4096)
        while Date() < deadline && packets.count < targetPackets {
            let n = buffer.withUnsafeMutableBytes { raw -> Int in
                recvfrom(clientFD, raw.baseAddress, raw.count, 0, nil, nil)
            }
            if n <= 0 { continue }
            if n < 12 { continue }
            if buffer[0] != MLAudio.rtpHeaderByte { headerOK = false }
            let seq = UInt16((UInt16(buffer[2]) << 8) | UInt16(buffer[3]))
            let ts = (UInt32(buffer[4]) << 24) | (UInt32(buffer[5]) << 16) | (UInt32(buffer[6]) << 8) | UInt32(buffer[7])
            // 上限可调：离线用真实 RtpAudioQueue 复现丢包/FEC 恢复时需要完整的多块数据流，
            // 16 个包（4 个数据包 = 1 个 FEC 块）不够。默认仍为 16，避免平时白白占内存。
            if rawPackets.count < rawPacketLimit { rawPackets.append(Data(buffer[0..<n])) }
            switch buffer[1] {
            case MLAudio.payloadTypeAudio:
                if dataSeqsSeen.contains(seq) { duplicateDatagrams += 1; continue }
                dataSeqsSeen.insert(seq)
                if firstSeq == nil { firstSeq = seq }
                let payload = Array(buffer[12..<n])
                packets.append(payload)
                arrivalTimes.append(CFAbsoluteTimeGetCurrent())
                lengths[payload.count, default: 0] += 1
                timestamps.append(ts)
            case MLAudio.payloadTypeFEC:
                fecDatagrams += 1
            default:
                otherDatagrams += 1
            }
        }

        pingStop.lock(); pinging = false; pingStop.unlock()
        session.stop()
        audio.stop()
        close(clientFD)

        // 写落盘文件（供外部解码）
        var dump = Data()
        for packet in packets {
            var length = UInt32(packet.count).littleEndian
            dump.append(Data(bytes: &length, count: 4))
            dump.append(contentsOf: packet)
        }
        try? dump.write(to: URL(fileURLWithPath: dumpPath))

        // 额外落盘前若干个「完整数据报」（含 RTP 头），供逐字节对照（相当于抓包）
        if let rtpDumpPath = ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_RTP_DUMP"], !rtpDumpPath.isEmpty {
            var rtpDump = Data()
            for raw in rawPackets {
                var length = UInt32(raw.count).littleEndian
                rtpDump.append(Data(bytes: &length, count: 4))
                rtpDump.append(raw)
            }
            try? rtpDump.write(to: URL(fileURLWithPath: rtpDumpPath))
            emit("前 \(rawPackets.count) 个完整 RTP 数据报已写入 \(rtpDumpPath)")
        }

        // 汇总（CBR / 时间戳 / 帧率断言只看按 seq 去重后的数据包）
        let distinct = lengths.keys.sorted()
        let cbr = distinct.count == 1
        var tsDelta5 = true
        for i in 1..<max(1, timestamps.count) {
            let delta = timestamps[i] &- timestamps[i - 1]
            if delta != 5 { tsDelta5 = false; break }
        }
        emit("收到 UDP 数据报：数据 \(dataSeqsSeen.count)（冗余副本 \(duplicateDatagrams)）+ FEC \(fecDatagrams) + 其他 \(otherDatagrams)")
        emit("RTP 头 magic 正确 = \(headerOK)（0x80）")
        emit("首个数据包序号 = \(firstSeq.map { Int($0) } ?? -1)")
        emit("数据包长分布 = \(distinct.map { "\($0)字节×\(lengths[$0]!)" }.joined(separator: ", "))")
        emit("数据包是否全部等长（CBR） = \(cbr)")
        emit("数据包时间戳每包 +5ms = \(tsDelta5)")
        if timestamps.count >= 2 {
            let first = timestamps.first!, last = timestamps.last!
            let spanMs = Double(last &- first)
            emit(String(format: "数据包时间戳跨度 %.0fms / %d 包 → 实测 %.1f 帧/秒", spanMs, timestamps.count,
                        spanMs > 0 ? Double(timestamps.count - 1) * 1000.0 / spanMs : 0))
        }
        // 到达节拍（客户端真正体验到的发包间隔）——这是判断「咔哒」是否源自宿主发包空洞的直接证据。
        if arrivalTimes.count >= 2 {
            var maxGapMs = 0.0, sumGapMs = 0.0
            var over7 = 0, over15 = 0
            var holes: [(Int, Double)] = []
            for i in 1..<arrivalTimes.count {
                let gapMs = (arrivalTimes[i] - arrivalTimes[i - 1]) * 1000
                sumGapMs += gapMs
                if gapMs > maxGapMs { maxGapMs = gapMs }
                if gapMs > 7 { over7 += 1 }
                if gapMs > 15 { over15 += 1 }
                if gapMs > 6 { holes.append((i, gapMs)) }
            }
            emit(String(format: "到达间隔：平均 %.2fms，最大 %.2fms；>7ms 的 %d 次，>15ms 的 %d 次（%d 包）",
                        sumGapMs / Double(arrivalTimes.count - 1), maxGapMs, over7, over15, arrivalTimes.count))
            // 空洞出现的位置很关键：若集中在开头（引擎缓冲/游戏音频刚起）→ 属启动瞬态；
            // 若均匀散布 → 是稳态问题。列出全部 >6ms 的空洞（包序号 + 间隔）。
            let holeText = holes.prefix(40).map { String(format: "#%d=%.1f", $0.0, $0.1) }.joined(separator: " ")
            emit("空洞(>6ms)位置：\(holes.isEmpty ? "无" : holeText)\(holes.count > 40 ? " …共\(holes.count)个" : "")")
        }
        emit("Opus 负载已写入 \(dumpPath)（共 \(packets.count) 包）")
        emit("=== 音频端到端自测结束 ===")
        exit(0)
    }
}
