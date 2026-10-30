//
//  GameStream.swift
//  QuickRecorder
//
//  游戏串流核心（「无窗口运行的 Godot 游戏」→ H.264）。
//
//  数据通路（全程不把像素拷进拷出）：
//    · 宿主创建两块 BGRA 的 IOSurface，把它们的 ID 通过环境变量 QR_IOSURFACE_ID / QR_IOSURFACE_ID_2
//      交给游戏子进程；游戏按这两块交替（ping-pong）渲染；
//    · 游戏用 offscreen 显示驱动（--display-driver offscreen）把画面用 Metal 直接渲染进 IOSurface
//      —— 不创建任何窗口，也没有 CAMetalLayer/nextDrawable 参与；
//    · 引擎在 GPU 完成一帧后，向输入 socket 的反方向写一条 8 字节完成通知（(seq, slot)）；
//      宿主只对「刚被写完、本帧 GPU 不再写」的那块 IOSurface 合成光标，再用
//      CVPixelBufferCreateWithIOSurface 包成 CVPixelBuffer（零拷贝）喂给 VideoToolbox 编 H.264；
//    · 输入走一条 Unix domain stream socket：宿主监听、游戏连接，宿主按固定 24 字节小端包下发
//      鼠标/键盘/触摸事件。
//
//  本文件只实现「取帧 + 编码 + 输入通道」三件事，刻意不接入现有的 Moonlight 推流管线
//  （MLCapturePipeline / MLVideoStream），那是下一步的工作；这样本模块可以独立验证。
//
//  跨进程共享 IOSurface 的取舍（重要）：
//    游戏子进程是用 IOSurfaceLookup(id) 按 ID 反查这块表面的，而 IOSurfaceLookup 只能看到
//    「全局（global）」表面。因此宿主创建时设置了 kIOSurfaceIsGlobal。该键自 macOS 10.11 起
//    被标记 deprecated（理由：全局表面可被系统内任意任务按 ID 查到，存在信息泄露风险）。
//    当前阶段先沿用它，等接入正式推流后再迁移到 IOSurfaceCreateMachPort + 正规 Mach/XPC 通道
//    （把 mach port 通过已有会话通道传给游戏，游戏用 IOSurfaceLookupFromMachPort 还原），
//    那时就不再需要全局表面。
//

import Foundation
import Darwin
import CoreGraphics
import CoreMedia
import CoreVideo
import ImageIO
import IOSurface
import VideoToolbox

/// 游戏串流的用户设置。读写方式与 Streaming 目录既有设置一致（UserDefaults）。
enum MLGameStreamSettings {
    /// 是否合成虚拟光标。
    ///
    /// 默认开启：双缓冲改造后，光标只被画进「GPU 刚写完、本帧不会再写」的那个槽位，
    /// 与 GPU 写入的竞态从根上消除（实测连续 40 帧导出零丢光标），因此默认开启是合理的。
    /// 纯触摸会话本就不会显示光标（见 cursorHasMouseInput），不受该开关影响。
    static let showCursorKey = "streaming.game.showCursor"

    static var showCursor: Bool {
        get { (UserDefaults.standard.object(forKey: showCursorKey) as? Bool) ?? true }
        set { UserDefaults.standard.set(newValue, forKey: showCursorKey) }
    }

    /// 游戏模式是否把游戏音频送进串流（默认开）。
    /// 环境变量 QR_GAMESTREAM_AUDIO 一旦设置即为最高优先级（"0"=关闭），方便自测对比。
    ///
    /// 打开时：宿主启动游戏会加 --audio-driver Dummy（本地静音、无需音频录制授权），
    /// 并让引擎把混音经反向通道回传，由宿主编 Opus；关闭时：不做任何音频处理，游戏声音正常本地外放。
    static let audioEnabledKey = "streaming.game.audioEnabled"

    static var audioEnabled: Bool {
        get {
            if let v = ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO"] { return v != "0" }
            return (UserDefaults.standard.object(forKey: audioEnabledKey) as? Bool) ?? true
        }
        set { UserDefaults.standard.set(newValue, forKey: audioEnabledKey) }
    }
}

/// 无窗口 Godot 游戏的采集 / 编码 / 输入会话。
///
/// 一次会话 = 两块 IOSurface（双缓冲）+ 一个游戏子进程 + 一条输入 socket + 一个 VTCompressionSession。
/// 生命周期由 start()/stop() 控制，deinit 兜底；不持有任何 UI 状态，可脱离主线程使用。
final class MLGameStreamSession {

    // MARK: 对外回调

    /// 诊断日志（给上层写进串流日志，便于排查「黑屏 / 无帧」）
    var onLog: ((String) -> Void)?
    /// 游戏进程退出：上报退出码，便于上层决定是否重试
    var onExit: ((Int32) -> Void)?
    /// 编码后的视频帧：(数据, 是否关键帧, 呈现时间戳)。
    /// 数据格式见 handleEncodedFrame 的注释（AVCC，长度前缀，无 Annex-B 起始码）。
    var onEncodedFrame: ((Data, Bool, CMTime) -> Void)?
    /// 游戏进程 stdout/stderr 的逐行输出（调试 / 输入通道验证用）
    var onOutput: ((String) -> Void)?

    // MARK: 配置（构造后不可变）

    private let width: Int
    private let height: Int
    private let fps: Int
    private let bitrateBps: Int
    /// 游戏可执行文件（Godot .app 内部的 Contents/MacOS/Godot）
    private let executablePath: String
    /// 游戏工程目录（--path 的参数）
    private let projectPath: String
    /// 宿主监听的输入 socket 路径
    private let socketPath: String

    /// 诊断：本次 RTSP 协商的分辨率（由宿主在 start() 前设置）。仅用于启动日志里核对尺寸一致性；
    /// 未设置时按 width/height 兜底。
    var negotiatedSize: CGSize?
    /// 诊断：输入坐标映射使用的尺寸（正常应与 width/height 相同）。由宿主在 start() 前设置。
    var mappingSize: CGSize?

    // MARK: 游戏音频（引擎 AudioEffectCapture 回传 PCM → Opus → Moonlight 音频通道）
    //
    //  由宿主在 start() 前注入（见 StreamingHost 的 .game 分支）：
    //    · audioStream 为空、或协商不是 2 声道、或开关关闭 → 完全不做音频（不凭空造帧）；
    //    · 环境变量 QR_GAMESTREAM_AUDIO=0 或 UserDefaults streaming.game.audioEnabled=false → 关闭。
    var audioStream: MLAudioStream?
    /// 协商得到的音频声道数（实测 = 2）
    var audioChannels: Int = 2
    /// 协商得到的音频帧长（毫秒，实测 = 5）。客户端按 48×duration 固定分配解码缓冲。
    var audioPacketDurationMs: Int = 5
    /// Opus 码率（bps）。对齐 Sunshine stream_configs 高音质档（立体声 512Kbps）。
    /// 5ms 帧 CELT-only 对码率敏感：64Kbps 金属味、96Kbps 仍有「杂音」，用高音质档彻底消除。
    var audioBitrateBps: Int = 512_000
    /// 音频编码子模块（start() 后创建，stop() 释放）
    private var gameAudio: MLGameAudioEncoder?
    /// 本次会话是否启用游戏音频（决定是否给子进程加 --audio-driver Dummy / QR_GAME_AUDIO）。
    private var gameAudioActive = false
    /// 保护 gameAudio 引用：反向通道读取线程会访问它，而 start()/stop() 在主线程替换它。
    private let audioLock = NSLock()

    // MARK: IOSurface（双缓冲）
    //
    //  为什么是两块：宿主若把光标合成进**同一块**游戏正在渲染的 IOSurface（旧实现），
    //  而 IOSurfaceLock 只约束 CPU、不约束 GPU，游戏在宿主编码的同时重画该区域，该帧就会丢光标
    //  或被覆盖（用户看到「轻闪、残影」）。改成两块后：游戏 ping-pong 渲染，引擎在 GPU 完成一帧时
    //  通过反向通道告诉宿主「刚写完哪个槽位」，宿主只对那个槽位合成 + 编码 → 竞态从根上消除。
    private var surfaces: [IOSurface?] = [nil, nil]
    /// 两块表面的全局 ID，交给游戏子进程用 IOSurfaceLookup 反查。
    private(set) var surfaceIDs: [IOSurfaceID] = [0, 0]
    /// 是否请求双缓冲。自测用 QR_GAMESTREAM_FORCE_SINGLE=1 可强制单缓冲（复现改造前的丢光标现象）。
    private let dualBufferRequested = ProcessInfo.processInfo.environment["QR_GAMESTREAM_FORCE_SINGLE"] != "1"
    /// 实际是否启用双缓冲（第二块创建成功才算）。
    private var isDualBuffer: Bool { surfaces[1] != nil }

    // MARK: 编码

    private var compressionSession: VTCompressionSession?
    /// 就绪探测定时器：轮询表面是否已有非黑内容，用来判断「画面起来了」
    private var readyTimer: DispatchSourceTimer?
    private let encodeQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.gamestream.encode", qos: .userInitiated)
    private var frameIndex: Int64 = 0

    // MARK: 编码驱动（事件驱动，而非固定节拍）
    //
    //  为什么不再是「按 fps 固定节拍出帧」：游戏往往只渲染 30fps，而协商帧率可能是 60fps。
    //  固定 60fps 会把同一帧游戏画面重复编两遍（浪费带宽），且光标更新被绑死在这条节拍上。
    //  改成「只在有变化时编一帧」：
    //    · 收到一次新的帧完成通知（游戏出了新帧）→ 编一帧（带当前光标）；
    //    · 光标位置变化但游戏没有新帧 → 也编一帧（复用上一完成槽位的游戏内容 + 新光标位置）；
    //    · 两者都没有 → 不编码（该窗口内 0 帧）。
    //  编码速率用 minInterval 限制在协商帧率内（最高 fps），避免游戏高帧率时把编码速率放大。
    private let encodeTrigger = NSCondition()
    /// 有新游戏帧可用（由反向通道读取线程置位）
    private var pendingGameFrame = false
    /// 光标位置发生了变化（由输入路径置位）
    private var pendingCursorMove = false
    /// 编码出帧的驱动定时器（取代原来的 NSCondition + usleep 循环）。
    ///
    /// 为什么改用 DispatchSourceTimer：实测在系统调度压力下，编码线程里的 usleep(33ms) 会被拉长到
    /// 上百毫秒（同机的音频排水线程用的是 DispatchSourceTimer，5ms 一直很准），导致游戏 30fps 出帧时
    /// 编码只有 8~9fps（丢掉 2/3 的帧 → 画面卡）。定时器由 libdispatch 驱动，节拍稳定，
    /// 不受线程「睡眠被拉长」的影响。
    private var encodeTickTimer: DispatchSourceTimer?
    private let encodeTickQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.gamestream.encode", qos: .userInteractive)
    /// 编码节拍周期（秒）。用于给限速留半个节拍的容差，避免离散节拍与限速点拍频。
    private var encodeTickPeriod: TimeInterval = 1.0 / 240.0
    /// 上一次编码的时刻（用于把编码速率限制在协商帧率内）
    private var lastEncodeAt = Date.distantPast
    /// 编码时间基准（第一帧编码的墙钟），用于给变速率出帧打真实的呈现时间戳。
    private var encodeWallClockStart: Date?
    /// 诊断：按「游戏新帧 / 光标移动」分别统计触发次数、实际编码帧数与字节数
    private var triggerByGameFrame = 0
    private var triggerByCursorMove = 0
    private var encodedFrameCount = 0
    private var encodedBytesTotal = 0
    /// 诊断：encodeOneFrame 自身占用的微秒数与次数，用于区分「编码本身慢」还是「编码线程被调度饿死」：
    /// 若「单帧占用」≈ 循环周期 → 是编码本身（IOSurface 锁 / 光标合成 / VT 提交）慢；
    /// 若「单帧占用」远小于周期 → 线程没被调度上 CPU（CPU 饱和或 QoS 太低）。
    private var encodeWorkMicros: UInt64 = 0
    private var encodeWorkCount = 0
    private var diagBaseEncodeWorkMicros: UInt64 = 0
    private var diagBaseEncodeWorkCount = 0
    /// 反向通道不可用（超时未收到完成通知，疑似旧版引擎）→ 回退单缓冲路径，只打一次日志
    private var reverseFallbackActive = false
    /// 首次允许「无通知」的宽限期截止时刻：期间宁可只出帧不合成光标，避免把光标画进正被 GPU 写的缓冲。
    private var reverseGraceDeadline = Date.distantPast
    /// 当前帧的 SPS/PPS（AVCC 下参数集不在码流里，而在 format description 的扩展里，
    /// 这里提取出来只为统计/上报「参数集是否出现」，不参与组包）
    private(set) var sps: [UInt8] = []
    private(set) var pps: [UInt8] = []

    /// 会话期间持有的「实时活动」断言：抑制 macOS App Nap 与定时器合并。
    ///
    /// 宿主是 LSUIElement（无 Dock 图标）应用，串流时窗口在后台 → 系统按 App Nap 节流它的定时器。
    /// 实测（音频自测，2000 包）：不抑制时会出现每几秒一次的 ~300ms「卡顿簇」，簇内音频包间隔
    /// 忽高忽低（20 次 >7ms，最大 15ms）→ 客户端连串欠载就是用户听到的「咔哒」；抑制后降到 2~4 次。
    /// .latencyCritical 把整段会话钉在实时档，彻底关掉这层节流。
    private var activityToken: NSObjectProtocol?

    // MARK: 虚拟光标（离屏运行没有系统光标，游戏也不画 → 由宿主自绘并合成进画面）
    //
    //  为什么必须「画进这块 IOSurface（CVPixelBuffer）再编码」而不是另开一路叠加：
    //    · 客户端只解一路 H.264；另开叠加需要第二条编码/合成链路，复杂且延迟更高。
    //    · 光标位置由宿主跟踪（相对移动只能靠累加），合成后与游戏内容用同一条编码路径，
    //      天然像素级对齐，不会出现「画面与光标错位」。
    //
    //  何时显示（任务要求）：
    //    · 只有本会话「收到过鼠标输入」才显示；纯触摸会话不显示（否则触摸设备上会多一个箭头）。
    //    · 首次收到鼠标输入时若还没有位置，初始化到画面中心。
    //    · 触摸是绝对坐标，绝不用它移动鼠标光标（避免两套坐标互相踩）。
    //
    //  性能（任务要求）：只重绘光标所在的小矩形（脏区），指针没动且该区域未被游戏重绘时直接跳过。
    private let cursorLock = NSLock()
    /// 本会话是否收到过鼠标输入（鼠标移动 kind=0 或鼠标按键 kind=1）
    private var cursorHasMouseInput = false
    /// 虚拟指针是否已有有效位置（游戏像素，左上原点）
    private var cursorValid = false
    private var cursorPoint = CGPoint.zero
    /// 上次真正画进 buffer 的位置及其所在脏区快照（用于「没变就不重绘」判定）。
    /// 按槽位分开保存：双缓冲下光标会被画进不同的槽位，各自的内容/快照互不相同。
    ///   · cursorSnapshot      = 画完光标**之后**该脏区的内容；
    ///   · cursorClean / cleanRect = 画光标**之前**该脏区的干净游戏内容，用于「复用同一槽位出新光标位置时，
    ///     先把旧光标擦掉」——否则光标会在画面上拖出残影。
    private var cursorDrawnPoint: [CGPoint?] = [nil, nil]
    private var cursorSnapshot: [[UInt8]?] = [nil, nil]
    private var cursorSnapshotRect: [CGRect] = [.zero, .zero]
    private var cursorClean: [[UInt8]?] = [nil, nil]
    private var cursorCleanRect: [CGRect] = [.zero, .zero]
    /// 诊断计数：实际合成次数 / 位置未变且脏区未变而跳过 / 无鼠标输入而跳过 / 尚未收到完成通知而跳过
    private var cursorBlitCount = 0
    private var cursorSkipUnchangedCount = 0
    private var cursorSkipNoMouseCount = 0
    private var cursorSkipNoNotifyCount = 0
    /// 诊断：上一次合成后回读到的光标尖端像素（BGRA），用于确认 CPU 写入确实落进了这块显存。
    private var cursorLastBlitPixel = "-"

    /// 光标合成总开关：
    ///   · 自测环境变量 QR_GAMESTREAM_CURSOR 一旦设置即为最高优先级（"0"=关闭，其余=开启），
    ///     方便对比「开/关」两种情形的编码帧率；
    ///   · 否则取用户设置 MLGameStreamSettings.showCursor。
    /// 每帧读一次 UserDefaults（一次 plist 查询，开销可忽略），因此设置在会话进行中即时生效。
    private var cursorCompositingEnabled: Bool {
        if let v = ProcessInfo.processInfo.environment["QR_GAMESTREAM_CURSOR"] { return v != "0" }
        return MLGameStreamSettings.showCursor
    }
    /// 自测用：在第 N 帧（frameIndex 计）把当前 IOSurface 导出成 PNG 到该路径。
    private var dumpPNGPath: String? = ProcessInfo.processInfo.environment["QR_GAMESTREAM_DUMP_PNG"]
    private var dumpAfterFrame = Int(ProcessInfo.processInfo.environment["QR_GAMESTREAM_DUMP_AFTER"] ?? "") ?? -1
    /// 自测用：连续导出序列时，在「合成光标」之后、写 PNG 之前额外等待的毫秒数。
    ///
    /// 为什么需要：VideoToolbox 是**异步**读取这块 IOSurface 的，编码器真正读到的时刻晚于
    /// 我们合成光标、也晚于同一把锁内立刻写出的 PNG。若在合成后立刻导出 PNG，就看不到
    /// 「GPU 在这段延迟里把光标覆盖掉」这一竞态，单/双缓冲都会显示 0 丢失——无法区分。
    /// 因此自测可设置一个约等于「游戏一帧时长」的延迟来模拟编码器的异步读取时刻：
    ///   · 单缓冲：同一块缓冲每帧都被 GPU 重写 → 延迟超过一帧后光标必被覆盖 → 丢失；
    ///   · 双缓冲：刚完成的槽位要等下一轮 ping-pong（≈两帧）才会被重写 → 延迟一帧仍完好。
    private let dumpSequenceDelayUs = UInt32(
        (Double(ProcessInfo.processInfo.environment["QR_GAMESTREAM_DUMP_DELAY_MS"] ?? "") ?? 0) * 1000)
    /// 自测用：把连续的 N 帧各导出一个 PNG 到该目录（统计「光标丢失帧数」）。
    private var dumpSequenceDir: String?
    private var dumpSequenceRemaining = 0
    private var dumpSequenceTotal = 0

    // MARK: 按需关键帧（客户端请求 IDR）
    //
    // 为什么需要：客户端解码吃到坏帧（本链路 IOSurface 取帧有撕裂风险，可能读到半帧）后会发送
    // 关键帧请求。此前游戏模式完全没有响应这条请求，客户端只能等 MaxKeyFrameInterval 自动出 IDR，
    // 花屏要拖很久才恢复（那时这个间隔是 fps，即每秒一个——虽然恢复快，代价是每秒一次大突发，
    // 见 createCompressionSession 里对「卡顿爆音」的说明；现在间隔拉长到 10 秒，按需请求就更关键了）。
    //
    // 线程安全：requestKeyFrame() 可能从主线程调用，而标志位的消费发生在编码队列上，这里用一把
    // NSLock 保护「标志位 + 上次请求时间 + 计数」这组状态。不用原子变量的原因：需要同时读写时间戳
    // 并统计拒绝次数，NSLock 更直观，且每帧最多加一次锁，开销可忽略。
    private let keyFrameLock = NSLock()
    /// 待消费的强制关键帧标志：编码每一帧时检查并消费（只影响一帧，避免每一帧都强制）
    private var keyFrameRequested = false
    /// 上次接受关键帧请求的时间（限流用，避免 IDR 风暴）
    private var lastKeyFrameRequestAt: Date?
    /// 诊断：实际强制出来的 IDR 数 / 被限流拒绝的请求数
    private var forcedKeyFrameCount = 0
    private var rejectedKeyFrameRequestCount = 0

    // MARK: 子进程

    private var process: Process?
    private(set) var processIdentifier: pid_t = 0
    /// 子进程 stdout/stderr 的管道（同时用于抓取游戏打印，便于验证输入通道）
    private var outputPipe: Pipe?
    private let outputLock = NSLock()
    /// 子进程 stdout 的整行缓存（readabilityHandler 给出的片段不保证按行对齐）
    private var outputPending = ""

    // MARK: 输入 socket

    private var listenFD: Int32 = -1
    private var clientFD: Int32 = -1
    private var socketThread: Thread?
    private let clientLock = NSLock()

    // MARK: 反向帧完成通道（游戏 → 宿主）
    //
    //  引擎在 Metal 命令缓冲完成回调里，向输入 socket 的**反方向**写一条 8 字节通知（小端）：
    //    [0..3] frame_seq    —— 引擎已完成的帧序号（从 0 起单调递增，uint32）
    //    [4..7] buffer_index —— 该帧写入的 IOSurface 槽位（0 或 1，uint32）
    //  （确切格式以引擎的 drivers/metal/qr_reverse_channel.h 为准。）
    //
    //  宿主据此知道「哪个槽位刚被 GPU 写完、本帧不会再被写」，只对该槽位做光标合成 + 编码，
    //  从而消除「光标被游戏重画覆盖 / 丢弃」的竞态。这条通知绝不阻塞发送侧（引擎用 poll 探可写性，
    //  不可写即丢弃；音频消息则会最多等 30ms 重试，丢一条音频就是一声「啪」），因此宿主读得慢
    //  最多是丢几条通知，不会反过来拖住游戏。
    private var reverseThread: Thread?
    private let reverseLock = NSLock()
    /// 最近一次完成通知的槽位；-1 表示尚未收到任何通知（会话刚开始）。
    private var reverseSlot = -1
    /// 最近一次完成通知的帧序号与累计条数（诊断用）。
    private var reverseSeq: UInt32 = 0
    private var reverseNotifications = 0
    /// 诊断：读取线程的 read() 次数、解析出的消息总数、单次读到的最大消息数。
    ///
    /// 用途：帧通知按「每次 read 只 signalEncode 一次」合并。若「读次数/秒」远低于通知数/秒，
    /// 说明一次 read 拿到了多条通知（线程被调度饿死，或引擎成批推送）→ 帧被合并丢弃。
    private var readerReads = 0
    private var readerMessages = 0
    private var readerFrameBatches = 0
    private var readerMaxBatch = 0
    private var diagBaseReaderReads = 0
    private var diagBaseReaderMessages = 0
    private var diagBaseReaderFrameBatches = 0
    /// 是否已经收到过反向完成通知。建立后编码循环改为「事件驱动」；断连或从未收到则用固定节拍兜底。
    private var sawReverseNotification = false
    /// 自测开关：即使收到通知也忽略它，用来模拟「旧版引擎不提供反向通道」→ 验证回退单缓冲路径。
    private let reverseDisabledForTest = ProcessInfo.processInfo.environment["QR_GAMESTREAM_NO_REVERSE"] == "1"
    /// 自测开关：强制用「改造前的固定节拍」出帧（忽略事件驱动），用于给出改造前的帧率/带宽对照。
    private let forceTimerDrive = ProcessInfo.processInfo.environment["QR_GAMESTREAM_FORCE_TIMER"] == "1"

    // MARK: 输入发送队列（关键：绝不阻塞调用线程）
    //
    // 为什么要有这一层：输入包是在控制通道的接收线程（MLENetServer 的 ENet 线程，见
    // MoonlightControl.handleInputPacket → MLInputInjector.handle → onGameInput）上产生的。
    // 如果在那条线程上直接 write()，一旦游戏侧输入线程读得慢、socket 发送缓冲写满，
    // write() 就会阻塞——把整条控制通道的收发一起堵死（表现为客户端输入计数冻结、心跳卡顿）。
    //
    // 因此这里把「产生输入」与「写 socket」解耦：调用方只入队（非阻塞），由独立发送线程
    // 负责落盘；队列有上限，满了就丢弃（优先丢最旧的可合并事件），任何情况下调用方都不等待。
    // 拖动/鼠标移动这类高频事件允许被「合并」（只保留最新一条），既压住队列又保证位置最新。
    private struct OutboundInput {
        var kind: UInt8
        var code: UInt16
        /// 是否可被后续同类事件合并覆盖（鼠标移动 / 触摸 MOVE）
        var coalescable: Bool
        var packet: [UInt8]
    }

    private let sendLock = NSCondition()
    private var sendQueue: [OutboundInput] = []
    private var sendThread: Thread?
    private var sendThreadShouldStop = false
    /// 队列上限。超过后不再阻塞产生方，而是丢弃最旧的可合并事件
    private let maxSendQueue = 256

    /// 诊断：已成功写出的 24 字节包数 / 丢弃数 / 被合并掉的移动事件数 / 接受连接次数
    private var inputPacketsSent = 0
    private var inputPacketsDropped = 0
    private var inputMovesCoalesced = 0
    private var inputAcceptCount = 0
    /// 5 秒周期诊断日志
    private var diagTimer: DispatchSourceTimer?
    /// 诊断窗口基准（用于算「本窗口编码帧率 / 平均每帧字节数」）
    private var diagBaseEncodedFrames = 0
    private var diagBaseEncodedBytes = 0
    private var diagBaseAt = Date()

    // MARK: 生命周期

    private let stateLock = NSLock()
    private var isStopped = true

    /// - Parameters:
    ///   - width/height: IOSurface 与编码分辨率，默认 1920×1080
    ///   - fps: 出帧帧率（30 或 60）
    ///   - bitrateBps: 目标码率
    ///   - executablePath: Godot .app 内的真实可执行文件路径
    ///   - projectPath: 游戏工程目录
    ///   - socketPath: 输入 socket 路径；默认放 /tmp，长度远小于 sun_path 上限
    init(width: Int = 1920,
         height: Int = 1080,
         fps: Int = 30,
         bitrateBps: Int = 12_000_000,
         executablePath: String,
         projectPath: String,
         socketPath: String? = nil) {
        self.width = width
        self.height = height
        self.fps = max(1, fps)
        self.bitrateBps = max(100_000, bitrateBps)
        self.executablePath = executablePath
        self.projectPath = projectPath
        self.socketPath = socketPath ?? "/tmp/quickrecorder-game-\(getpid()).sock"
    }

    deinit {
        stop()
    }

    // MARK: - 启动 / 停止

    /// 启动会话：建表面 → 起输入监听 → 起子进程 → （画面就绪后）开始出帧。
    func start() throws {
        stateLock.lock()
        guard isStopped else { stateLock.unlock(); return }
        isStopped = false
        stateLock.unlock()

        // 整段会话期间抑制 App Nap / 定时器合并（宿主无 Dock 图标，后台时会被系统节流）。
        if activityToken == nil {
            activityToken = ProcessInfo.processInfo.beginActivity(
                options: [.userInitiated, .latencyCritical],
                reason: "游戏串流：音频/视频需要实时节拍")
        }

        // 本次会话是否启用游戏音频：开关开、协商到立体声、且拿到了音频通道。
        // 计算一次后固定，launchGameProcess 与 startGameAudio 共用，避免两处判断不一致。
        gameAudioActive = MLGameStreamSettings.audioEnabled
            && audioStream != nil
            && audioChannels >= 2
            && ProcessInfo.processInfo.environment["QR_GAMESTREAM_AUDIO_FORCE_FAIL"] != "1"

        try createSurface()
        try createCompressionSession()
        startInputListener()
        startSendThread()
        // 反向完成通道的宽限期：引擎要先连上输入 socket 才能注册回发 fd，给 2 秒。
        // 期间收不到通知时只出帧、不合成光标；超过宽限期仍无通知即判定为旧版引擎并回退单缓冲。
        reverseGraceDeadline = Date().addingTimeInterval(2.0)
        startReverseReader()
        // 音频编码器要在反向通道读取线程可能处理音频消息之前就绪，故先于 launchGameProcess 建立。
        startGameAudio()
        startDiagTimer()
        try launchGameProcess()
        startReadyProbe()

        // 启动日志把「协商分辨率 / IOSurface / 编码 / 输入映射」四处尺寸全打出来（即使相同也打），
        // 便于以后一眼看出坐标换算链路是否有一端用了别的尺寸（点击错位的根因就是这种不一致）。
        let surfaceW = surfaces[0].map { Int(IOSurfaceGetWidth($0)) } ?? -1
        let surfaceH = surfaces[0].map { Int(IOSurfaceGetHeight($0)) } ?? -1
        let negotiated = negotiatedSize ?? CGSize(width: width, height: height)
        let mapping = mappingSize ?? CGSize(width: width, height: height)
        onLog?("游戏串流会话已启动：surface=\(surfaceIDs[0])/\(surfaceIDs[1]) 双缓冲=\(isDualBuffer ? "是" : "否") \(width)x\(height) @\(fps)fps \(bitrateBps/1000)Kbps")
        onLog?(String(format: "游戏尺寸链路：协商=%dx%d IOSurface=%dx%d 编码=%dx%d 输入映射=%dx%d",
                      Int(negotiated.width), Int(negotiated.height),
                      surfaceW, surfaceH, width, height, Int(mapping.width), Int(mapping.height)))
    }

    /// 停止会话并释放全部资源。可重复调用。
    func stop() {
        stateLock.lock()
        if isStopped { stateLock.unlock(); return }
        isStopped = true
        stateLock.unlock()

        // 结束会话：释放「实时活动」断言，恢复系统正常的 App Nap 行为（幂等）。
        if let token = activityToken {
            ProcessInfo.processInfo.endActivity(token)
            activityToken = nil
        }

        readyTimer?.cancel()
        readyTimer = nil
        // 停掉编码出帧定时器（事件触发的 pending 标志随之失效）。
        encodeTickTimer?.cancel()
        encodeTickTimer = nil
        diagTimer?.cancel()
        diagTimer = nil

        // 音频编码发送：停掉编码器（幂等）。反向通道读取线程可能并发访问它，用 audioLock 解引用后再释放。
        audioLock.lock()
        let audioToStop = gameAudio
        gameAudio = nil
        audioLock.unlock()
        audioToStop?.stop()

        // 先停发送线程，再关 fd：发送线程只走 MSG_DONTWAIT + poll，不会硬阻塞，
        // 收到 stop 后能立刻退出，因此这里用短暂的等待即可。
        sendLock.lock()
        sendThreadShouldStop = true
        sendQueue.removeAll()
        sendLock.broadcast()
        sendLock.unlock()
        sendThread = nil

        if let session = compressionSession {
            // 等已入队的帧编完再 invalidate，避免丢尾帧 / 回调打在已释放的 self 上
            VTCompressionSessionCompleteFrames(session, untilPresentationTimeStamp: .invalid)
            VTCompressionSessionInvalidate(session)
        }
        compressionSession = nil

        closeClient()
        if listenFD >= 0 {
            close(listenFD)
            listenFD = -1
        }
        socketThread = nil
        // 反向通道读取线程按 isStopped + poll 超时（≤100ms）自行退出，无需 join。
        reverseThread = nil
        // socket 文件是内核对象，不 unlink 会一直留在 /tmp
        unlink(socketPath)

        if let process = process, process.isRunning {
            process.terminate()   // 先 SIGTERM
            // 宽限 2s，仍未退出就强杀；否则端口/表面会一直被占着
            let deadline = Date().addingTimeInterval(2)
            while process.isRunning, Date() < deadline {
                usleep(50_000)
            }
            if process.isRunning { kill(process.processIdentifier, SIGKILL) }
        }
        process = nil
        processIdentifier = 0

        if let pipe = outputPipe {
            pipe.fileHandleForReading.readabilityHandler = nil
        }
        outputPipe = nil

        surfaces = [nil, nil]
        surfaceIDs = [0, 0]
        onLog?("游戏串流会话已停止")
    }

    // MARK: - IOSurface

    /// 创建供跨进程共享的 BGRA 表面（双缓冲：slot 0 / slot 1）。
    ///
    /// kIOSurfaceIsGlobal 是必须的：游戏子进程用 IOSurfaceLookup(id) 反查，而 IOSurfaceLookup
    /// 只认全局表面。该键已被 deprecated（见文件头注释的取舍说明），后续会换成 mach port 传递。
    /// 第二块创建失败时退化为单缓冲（只把 slot 0 交给游戏），会话仍可用，只是无法消除光标合成竞态。
    private func createSurface() throws {
        guard let primary = Self.makeSurface(width: width, height: height) else {
            throw MLGameStreamError.surfaceCreationFailed
        }
        surfaces[0] = primary
        surfaceIDs[0] = IOSurfaceGetID(primary)

        if dualBufferRequested, let secondary = Self.makeSurface(width: width, height: height) {
            surfaces[1] = secondary
            surfaceIDs[1] = IOSurfaceGetID(secondary)
        } else if dualBufferRequested {
            onLog?("第二块 IOSurface 创建失败，退化为单缓冲（无法消除光标合成与 GPU 写入的竞态）")
        }
    }

    /// 创建一块清零的全局 BGRA 表面。
    ///
    /// 创建后清零：新分配的 IOSurface 内容不保证是 0，如果不清零，「非黑像素占比」就分不清
    /// 是「游戏已经渲染」还是「内存里的垃圾数据」，就绪探测会被骗到、也会编出一堆垃圾帧。
    private static func makeSurface(width: Int, height: Int) -> IOSurface? {
        let props: [CFString: Any] = [
            kIOSurfaceWidth: width,
            kIOSurfaceHeight: height,
            kIOSurfaceBytesPerElement: 4,
            kIOSurfacePixelFormat: UInt32(kCVPixelFormatType_32BGRA),
            kIOSurfaceIsGlobal: true,
        ]
        guard let created = IOSurfaceCreate(props as CFDictionary) else { return nil }
        if IOSurfaceLock(created, [], nil) == kIOReturnSuccess {
            memset(IOSurfaceGetBaseAddress(created), 0,
                   Int(IOSurfaceGetBytesPerRow(created)) * Int(IOSurfaceGetHeight(created)))
            IOSurfaceUnlock(created, [], nil)
        }
        return created
    }

    /// 把 IOSurface 零拷贝包成 CVPixelBuffer。
    ///
    /// 注意：这里**没有** new 一块内存再拷像素，CVPixelBufferCreateWithIOSurface 只是给既有的
    /// IOSurface 套一个 CoreVideo 壳，像素自始至终只有一份（GPU 渲染的那份）。
    private func wrapSurfaceAsPixelBuffer(_ surface: IOSurface) -> CVPixelBuffer? {
        // CVPixelBufferCreateWithIOSurface 按 CoreFoundation 的 Create 规则返回 +1 的引用，
        // SDK 里映射成 Unmanaged，需要 takeRetainedValue 把所有权交给 ARC。
        var pixelBufferOut: Unmanaged<CVPixelBuffer>?
        let status = CVPixelBufferCreateWithIOSurface(kCFAllocatorDefault, surface, nil, &pixelBufferOut)
        guard status == kCVReturnSuccess, let pixelBuffer = pixelBufferOut?.takeRetainedValue() else {
            onLog?("CVPixelBufferCreateWithIOSurface 失败（\(status)）")
            return nil
        }
        return pixelBuffer
    }

    /// 统计表面里非黑像素占比。
    ///
    /// 用途：证明「拿到的是游戏真实画面」而不是一块从未被写过的空 buffer（全黑）。
    /// 这里走 CPU 直接读 baseAddress，所以必须 IOSurfaceLock(readOnly)：它约束的是 CPU 访问，
    /// 保证我们读的这一遍内部一致（不会被自己的读/写交叉）。
    /// 返回 <0 表示读不到（未创建 / 加锁失败）。
    func currentFrameNonBlackRatio() -> Double {
        guard let surface = surfaces[0] else { return -1 }
        guard IOSurfaceLock(surface, .readOnly, nil) == kIOReturnSuccess else { return -1 }
        defer { IOSurfaceUnlock(surface, .readOnly, nil) }

        let w = Int(IOSurfaceGetWidth(surface))
        let h = Int(IOSurfaceGetHeight(surface))
        let bytesPerRow = Int(IOSurfaceGetBytesPerRow(surface))
        guard w > 0, h > 0 else { return -1 }
        // IOSurfaceGetBaseAddress 返回非可选指针（表面创建失败时才会失败，这里已保证有效）
        let base = IOSurfaceGetBaseAddress(surface)

        let pixels = base.assumingMemoryBound(to: UInt8.self)
        var nonBlack = 0
        for y in 0..<h {
            let row = pixels + y * bytesPerRow
            var x = 0
            while x < w {
                let b = row[x * 4]
                let g = row[x * 4 + 1]
                let r = row[x * 4 + 2]
                // 阈值 8：编码/色彩空间转换带来的 ±几级误差不算「非黑」
                if b > 8 || g > 8 || r > 8 { nonBlack += 1 }
                x += 1
            }
        }
        return Double(nonBlack) / Double(w * h)
    }

    // MARK: - 子进程

    /// 启动游戏子进程，注入全部 QR_* 环境变量。
    ///
    /// 注意必须直接执行 .app 内部的二进制（而不是 `open -a`）：只有直接执行才能自定义环境变量、
    /// 并且把 stdout 收进管道用于调试。裸二进制跑不了（缺 GodotSharp/ 会卡在 .NET 初始化），
    /// 所以 executablePath 必须是「官方 Godot_mono.app 的副本」里那个被替换过的 Godot 可执行文件。
    private func launchGameProcess() throws {
        let task = Process()
        task.executableURL = URL(fileURLWithPath: executablePath)
        var args = [
            "--display-driver", "offscreen",
            "--rendering-driver", "metal",
            "--path", projectPath,
        ]
        // 音频本地静音由引擎侧完成（引擎把 Master 总线静音），宿主不传 --audio-driver：
        // 若传 Dummy，Dummy 驱动用 sleep 计时，在本机被系统定时器合并/降 QoS 时明显偏慢
        // （实测只有约 40k 帧/秒而非 48000），会把音频拖慢；用默认（CoreAudio）驱动由硬件时钟驱动才准。
        // 该方案同样不需要任何系统音频录制授权、也不依赖 macOS 14.2。
        // 自测用：允许用 QR_GAMESTREAM_MAX_FPS 覆盖游戏侧帧率（CLI 优先级高于工程设置），
        // 复现「游戏渲染远快于编码」时的高帧率竞态场景。
        if let maxFps = ProcessInfo.processInfo.environment["QR_GAMESTREAM_MAX_FPS"], !maxFps.isEmpty {
            args.append(contentsOf: ["--max-fps", maxFps])
        }
        task.arguments = args

        var env = ProcessInfo.processInfo.environment
        env["QR_IOSURFACE_ID"] = "\(surfaceIDs[0])"
        // 第二块表面：引擎据此开启 ping-pong 双缓冲并按帧回发完成通知。
        if surfaces[1] != nil {
            env["QR_IOSURFACE_ID_2"] = "\(surfaceIDs[1])"
        }
        env["QR_OFFSCREEN_W"] = "\(width)"
        env["QR_OFFSCREEN_H"] = "\(height)"
        env["QR_INPUT_SOCKET"] = socketPath
        // 音频开关：引擎据此决定是否抓 Master 混音并经反向通道回传（同时把 mix_rate 强制为 48000）。
        // 关闭/未启用时绝不设置，引擎行为与改造前完全一致。
        if gameAudioActive {
            env["QR_GAME_AUDIO"] = "1"
        } else {
            env.removeValue(forKey: "QR_GAME_AUDIO")
        }
        // mono 版 Godot 启动时要解析 .NET 运行时；不设 DOTNET_ROOT 会永久卡在初始化。
        // 用持久化目录（Application Support 下的 dotnet），避免 /tmp 被清后运行时消失。
        if env["DOTNET_ROOT"] == nil { env["DOTNET_ROOT"] = MLStreamingHost.dotnetRootPath() }
        task.environment = env

        let pipe = Pipe()
        outputPipe = pipe
        pipe.fileHandleForReading.readabilityHandler = { [weak self] handle in
            let data = handle.availableData
            guard !data.isEmpty else { return }
            self?.ingestOutput(data)
        }
        task.standardOutput = pipe
        task.standardError = pipe

        task.terminationHandler = { [weak self] proc in
            guard let self else { return }
            self.onLog?("游戏进程已退出，状态码 \(proc.terminationStatus)")
            self.onExit?(proc.terminationStatus)
        }

        try task.run()
        process = task
        processIdentifier = task.processIdentifier
        onLog?("游戏进程已启动 pid=\(processIdentifier)")
    }

    /// 准备游戏音频发送：把引擎经反向通道回传的 PCM 编成 Opus 送进 MLAudioStream。
    ///
    /// 音频来源完全在引擎侧（AudioEffectCapture 抓 Master 混音），宿主只做重分块 + 编码 + 发送；
    /// 本地静音由子进程的 --audio-driver Dummy 负责（见 launchGameProcess），因此不需要任何系统音频授权。
    /// 刻意做成「失败只记日志、绝不抛出」：音频不可用不能拖垮视频/输入。
    private func startGameAudio() {
        guard gameAudioActive, let audioStream else {
            if !MLGameStreamSettings.audioEnabled {
                onLog?("音频：已通过开关关闭（QR_GAMESTREAM_AUDIO=0 或设置项），本次不发送游戏音频")
            } else if audioStream == nil {
                onLog?("音频：未提供 Moonlight 音频通道（可能协商声道不足），本次不发送游戏音频")
            } else if audioChannels < 2 {
                onLog?("音频：协商声道数 \(audioChannels) < 2，本次不发送游戏音频")
            } else {
                onLog?("音频：已强制禁用（QR_GAMESTREAM_AUDIO_FORCE_FAIL=1），仅用于降级验证（视频/输入不受影响）")
            }
            return
        }

        let encoder = MLGameAudioEncoder(
            frameDurationMs: audioPacketDurationMs,
            bitrateBps: audioBitrateBps,
            sendPacket: { packet, ms in audioStream.sendOpusFrame(packet, durationMs: ms) },
            onLog: { [weak self] message in self?.onLog?(message) })
        audioLock.lock()
        gameAudio = encoder
        audioLock.unlock()
        encoder.start()
        onLog?("音频：由引擎侧回传混音 PCM（引擎已静音 Master 总线，本地不外放；无需系统音频录制授权）")
    }

    /// 把 stdout 的字节流按行切开后回调（片段不保证对齐，需自行拼行）
    private func ingestOutput(_ data: Data) {
        guard let text = String(data: data, encoding: .utf8) else { return }
        outputLock.lock()
        outputPending += text
        var lines: [String] = []
        while let newline = outputPending.firstIndex(of: "\n") {
            lines.append(String(outputPending[outputPending.startIndex..<newline]))
            outputPending = String(outputPending[outputPending.index(after: newline)...])
        }
        outputLock.unlock()
        for line in lines where !line.isEmpty {
            onOutput?(line)
        }
    }

    // MARK: - 输入 socket（宿主监听，游戏连接）

    /// 起一个后台线程做 AF_UNIX SOCK_STREAM 的 bind/listen/accept。
    private func startInputListener() {
        unlink(socketPath)   // 清掉上次异常退出残留的 socket 文件，否则 bind 会失败
        let thread = Thread { [weak self] in self?.inputListenerMain() }
        thread.name = "QRGameInputAccept"
        socketThread = thread
        thread.start()
    }

    private func inputListenerMain() {
        let fd = socket(AF_UNIX, SOCK_STREAM, 0)
        guard fd >= 0 else {
            onLog?("创建输入 socket 失败（errno \(errno)）")
            return
        }

        var addr = sockaddr_un()
        addr.sun_family = sa_family_t(AF_UNIX)
        let pathBytes = Array(socketPath.utf8)
        let capacity = MemoryLayout.size(ofValue: addr.sun_path)
        guard pathBytes.count < capacity else {
            onLog?("输入 socket 路径过长（\(pathBytes.count) ≥ \(capacity)）")
            close(fd)
            return
        }
        withUnsafeMutablePointer(to: &addr.sun_path) { pointer in
            pointer.withMemoryRebound(to: CChar.self, capacity: capacity) { dst in
                for (i, byte) in pathBytes.enumerated() { dst[i] = CChar(bitPattern: byte) }
                dst[pathBytes.count] = 0
            }
        }

        let bindResult = withUnsafePointer(to: &addr) { pointer in
            pointer.withMemoryRebound(to: sockaddr.self, capacity: 1) { sa in
                bind(fd, sa, socklen_t(MemoryLayout<sockaddr_un>.size))
            }
        }
        guard bindResult == 0, listen(fd, 1) == 0 else {
            onLog?("输入 socket bind/listen 失败（errno \(errno)）")
            close(fd)
            return
        }
        listenFD = fd
        onLog?("输入通道已监听：\(socketPath)")

        // poll 带超时，便于 stop() 后线程能自行退出
        while !self.isStoppedInternal {
            var pfd = pollfd(fd: fd, events: Int16(POLLIN), revents: 0)
            let ready = poll(&pfd, 1, 200)
            if ready <= 0 { continue }
            let client = accept(fd, nil, nil)
            if client < 0 { continue }
            // 关键：对端异常关闭时 write/send 会触发 SIGPIPE，默认动作是直接杀掉宿主进程。
            // SO_NOSIGPIPE 让写失败只返回 EPIPE 错误，由发送线程计入丢弃数。
            var noSigPipe: Int32 = 1
            setsockopt(client, SOL_SOCKET, SO_NOSIGPIPE, &noSigPipe, socklen_t(MemoryLayout<Int32>.size))
            // 接收缓冲放大到 1MB：反向通道上行是 200 条/秒的音频 PCM（约 384KB/s）+ 帧完成通知，
            // 默认接收缓冲偏小会在宿主线程被调度延迟时形成反压 → 引擎侧 poll 不可写 → 丢音频（「啪」）。
            var rcvBuf: Int32 = 1 << 20
            setsockopt(client, SOL_SOCKET, SO_RCVBUF, &rcvBuf, socklen_t(MemoryLayout<Int32>.size))
            clientLock.lock()
            if clientFD >= 0 { close(clientFD) }   // 只服务最新一条连接
            clientFD = client
            inputAcceptCount += 1
            clientLock.unlock()
            onLog?("游戏已连接输入通道（第 \(inputAcceptCount) 次）")
        }
    }

    private var isStoppedInternal: Bool {
        stateLock.lock(); defer { stateLock.unlock() }
        return isStopped
    }

    private func closeClient() {
        clientLock.lock()
        if clientFD >= 0 { close(clientFD); clientFD = -1 }
        clientLock.unlock()
    }

    /// 是否已有游戏侧的输入连接。
    var isInputConnected: Bool {
        clientLock.lock(); defer { clientLock.unlock() }
        return clientFD >= 0
    }

    // MARK: - 反向通道：读取（帧完成通知 + 引擎回传音频 PCM）
    //
    //  引擎在输入 socket 的反方向按统一分帧格式回写（全部小端）：
    //    [0] type   [1] reserved   [2..3] payload_len   [4..] payload
    //    · type=0 帧完成：payload 8 字节 = frame_seq(u32) + buffer_index(u32)
    //    · type=1 音频：payload = 交织 Int16 LE 立体声
    //  （确切格式以引擎的 drivers/metal/qr_reverse_channel.h 为准。）
    //
    //  帧完成让宿主知道「哪个槽位刚被 GPU 写完、本帧不会再被写」，只对该槽位做光标合成 + 编码，
    //  消除「光标被游戏重画覆盖 / 丢弃」的竞态。音频则交给 MLGameAudioEncoder 重分块 + 编码 + 发送。
    //  引擎侧帧完成通知绝不阻塞（poll 探可写、不可写即丢），音频则最多等 30ms 重试后才丢，
    //  因此宿主读得慢最多是丢几条通知，不会拖住游戏。

    /// 在独立线程里读「输入 socket 反方向」的分帧消息，更新「最近完成的槽位」并处理音频。
    private func startReverseReader() {
        let thread = Thread { [weak self] in self?.reverseReaderLoop() }
        thread.name = "QRGameReverse"
        // 这条线程既要及时排空 socket（否则引擎侧 poll 判定不可写会丢消息），
        // 又要就地做 Opus 编码——音频的时效性优先于视频帧的吞吐。
        // 提 QoS 后视频编码（本机实测 20Mbps 输出、CPU 吃满）不会把它挤到几十毫秒不上调度；
        // 引擎侧音频线程早已用同样的手段解决过同类问题。
        thread.qualityOfService = .userInteractive
        reverseThread = thread
        thread.start()
    }

    private func reverseReaderLoop() {
        // 读缓冲要能容纳引擎可能一次性送来的多条消息（单条音频消息最大 4 + 960*4 = 3844 字节）。
        let readWidth = 16384
        var buf = [UInt8](repeating: 0, count: readWidth)
        var used = 0

        while !isStoppedInternal {
            clientLock.lock(); let fd = clientFD; clientLock.unlock()
            if fd < 0 {
                used = 0
                // 没有连接：反向通道暂不可用，退回固定节拍兜底（连接建立后通知会把它重新置为 true）。
                sawReverseNotification = false
                usleep(50_000)
                continue
            }
            var pfd = pollfd(fd: fd, events: Int16(POLLIN), revents: 0)
            let ready = poll(&pfd, 1, 100)
            if ready <= 0 { continue }

            let n = buf.withUnsafeMutableBytes { raw -> Int in
                guard let base = raw.baseAddress else { return -1 }
                return read(fd, base.advanced(by: used), readWidth - used)
            }
            if n <= 0 {
                // 对端关闭或出错：丢弃残留半条，等下一次连接（accept 会把 clientFD 换成新的）。
                used = 0
                sawReverseNotification = false
                continue
            }
            used += n

            var off = 0
            var gotGameFrame = false
            var msgCount = 0
            while used - off >= 4 {
                let type = buf[off]
                let payloadLen = Int(buf[off + 2]) | (Int(buf[off + 3]) << 8)
                // 半条消息（负载还没收全）：保留待下次读取，绝不能丢，否则流会错位。
                if used - off - 4 < payloadLen { break }
                let payloadStart = off + 4
                msgCount += 1

                if reverseDisabledForTest {
                    // 自测：只把数据读走（排空 socket），但不更新任何状态 → 等价于「旧引擎不发通知」。
                    off = payloadStart + payloadLen
                    continue
                }

                switch type {
                case 0: // 帧完成通知
                    if payloadLen >= 8 {
                        let seq = UInt32(buf[payloadStart]) | (UInt32(buf[payloadStart + 1]) << 8)
                            | (UInt32(buf[payloadStart + 2]) << 16) | (UInt32(buf[payloadStart + 3]) << 24)
                        let slot = UInt32(buf[payloadStart + 4]) | (UInt32(buf[payloadStart + 5]) << 8)
                            | (UInt32(buf[payloadStart + 6]) << 16) | (UInt32(buf[payloadStart + 7]) << 24)
                        reverseLock.lock()
                        reverseSeq = seq
                        // 只接受合法槽位；异常值不更新（宁可沿用上一个已知完成槽位，也不乱指一块缓冲）。
                        if slot < 2 { reverseSlot = Int(slot) }
                        reverseNotifications += 1
                        reverseLock.unlock()
                        // 该槽位刚被 GPU 重写过：上面若残留着我们之前画的光标，此刻已被覆盖，快照失效。
                        // 必须作废该槽位的光标状态，否则下一次复用它时会拿过期快照去「擦除」，把游戏画面擦坏。
                        if slot < 2 { invalidateCursorState(slot: Int(slot)) }
                        gotGameFrame = true
                        // 反向通道被证实可用：编码循环切换为事件驱动（不再固定节拍兜底）。
                        if !sawReverseNotification { sawReverseNotification = true }
                    }
                case 1: // 引擎回传的音频 PCM
                    audioLock.lock(); let audio = gameAudio; audioLock.unlock()
                    if let audio, payloadLen > 0 {
                        audio.appendPCMPayload(Array(buf[payloadStart..<(payloadStart + payloadLen)]))
                    }
                default:
                    break
                }
                off = payloadStart + payloadLen
            }
            if off > 0 {
                let remaining = used - off
                for i in 0..<remaining { buf[i] = buf[off + i] }
                used = remaining
            }
            // 诊断：记下本次 read 的批量（消息数 / 是否含帧通知）。
            reverseLock.lock()
            readerReads += 1
            readerMessages += msgCount
            if gotGameFrame { readerFrameBatches += 1 }
            if msgCount > readerMaxBatch { readerMaxBatch = msgCount }
            reverseLock.unlock()
            // 有新游戏帧 → 唤醒编码循环编一帧（带上当前光标）。
            if gotGameFrame { signalEncode(gameFrame: true, cursorMove: false) }
        }
    }

    /// 最近一次完成通知所指的槽位；-1 表示尚未收到任何通知（会话刚开始的一小段）。
    private func completedSlotForEncoding() -> Int {
        reverseLock.lock(); defer { reverseLock.unlock() }
        return reverseSlot
    }

    /// 诊断快照：(是否收到过通知, 最近帧序号, 累计通知数, 最近槽位)。
    func reverseChannelStats() -> (received: Int, lastSeq: UInt32, lastSlot: Int) {
        reverseLock.lock(); defer { reverseLock.unlock() }
        return (reverseNotifications, reverseSeq, reverseSlot)
    }

    /// 唤醒编码循环（合并同类触发：只置位标志，不排队）。
    private func signalEncode(gameFrame: Bool, cursorMove: Bool) {
        encodeTrigger.lock()
        if gameFrame { pendingGameFrame = true }
        if cursorMove { pendingCursorMove = true }
        encodeTrigger.signal()
        encodeTrigger.unlock()
    }

    /// 作废某槽位的光标状态。当该槽位被 GPU 重写（收到新的完成通知）时调用：
    /// 上面残留的光标已被覆盖，之前保存的「干净/带光标」快照全部失效，必须清掉，
    /// 否则下次复用它去擦除会把游戏画面擦坏、或把旧光标当成有效而漏画新光标。
    private func invalidateCursorState(slot: Int) {
        guard slot >= 0, slot < 2 else { return }
        cursorLock.lock()
        cursorDrawnPoint[slot] = nil
        cursorClean[slot] = nil
        cursorCleanRect[slot] = .zero
        cursorLock.unlock()
    }

    /// 下发一个输入事件（非阻塞：只入队，由独立发送线程写 socket）。
    ///
    /// 包格式（固定 24 字节，全部小端；与 Godot offscreen 驱动的解析代码一一对应）：
    ///   [0]     kind     0=鼠标移动 1=鼠标按键 2=滚轮 3=触摸 4=键盘
    ///   [1]     pressed  0/1
    ///   [2..3]  code     u16（鼠标键位 / 触摸 index / 键码）
    ///   [4..7]  x        f32
    ///   [8..11] y        f32
    ///   [12..15] dx      f32
    ///   [16..19] dy      f32
    ///   [20]    touchEvent  kind==3 时携带 Moonlight 的 LI_TOUCH_EVENT_* 原始类型
    ///   [21]    touchFlag   kind==3 且携带 touchEvent 时为 1（引擎据此启用严格语义）
    ///   [22..23] 保留，填 0
    ///
    /// 为什么要把 eventType 原样带过去：24 字节包里只有 pressed 一位，「HOVER / HOVER_LEAVE /
    /// BUTTON_ONLY / CANCEL_ALL」这些语义在 pressed 上全都被压平成 0（看似「抬起」），
    /// 引擎无法区分，正是触摸状态错乱的根源。把 eventType 带上后引擎可以严格按语义处理。
    ///
    /// - Returns: 是否已接受入队（未连接 / 队列满被丢弃时返回 false）。**不会阻塞调用线程**。
    @discardableResult
    func sendInput(kind: UInt8, pressed: Bool, code: UInt16, x: Float, y: Float,
                   dx: Float = 0, dy: Float = 0, touchEvent: UInt8 = 0xFF) -> Bool {
        // 鼠标输入（移动 kind=0 / 按键 kind=1）驱动宿主虚拟光标；位置就是宿主已换算好的
        // 游戏像素坐标，这里直接用、不二次换算（触摸是绝对坐标，绝不用它移动鼠标）。
        // 注意：无论「光标显示」开关是否打开都跟踪位置——这样会话进行中打开开关时，
        // 光标能立刻出现在当前位置，而不用等下一次鼠标事件。
        if kind == 0 || kind == 1 {
            noteMouseCursor(x: CGFloat(x), y: CGFloat(y))
        }
        var packet = [UInt8](repeating: 0, count: 24)
        packet[0] = kind
        packet[1] = pressed ? 1 : 0
        packet[2] = UInt8(truncatingIfNeeded: code)
        packet[3] = UInt8(truncatingIfNeeded: code >> 8)
        Self.writeFloat(x, into: &packet, at: 4)
        Self.writeFloat(y, into: &packet, at: 8)
        Self.writeFloat(dx, into: &packet, at: 12)
        Self.writeFloat(dy, into: &packet, at: 16)
        if kind == 3, touchEvent != 0xFF {
            packet[20] = touchEvent
            packet[21] = 1   // 触摸元数据有效标志
        }

        // 只有「鼠标移动(kind 0)」和「触摸 MOVE(eventType 3)」允许被后续同类事件合并覆盖；
        // DOWN/UP/CANCEL 这些状态位绝不能合并，否则会丢事件、又制造出「卡住」的状态。
        let coalescable = (kind == 0) || (kind == 3 && touchEvent == MLTouchEvent.move)
        return enqueue(OutboundInput(kind: kind, code: code, coalescable: coalescable, packet: packet))
    }

    /// 入队一个待发送输入包。全程持锁但不做任何 I/O，因此不会阻塞。
    private func enqueue(_ item: OutboundInput) -> Bool {
        sendLock.lock()
        defer { sendLock.unlock() }
        guard !sendThreadShouldStop else { return false }

        // 合并：紧挨着的最新同类可合并事件直接覆盖，避免拖动风暴把队列撑爆
        if item.coalescable, let last = sendQueue.last,
           last.coalescable, last.kind == item.kind, last.code == item.code {
            sendQueue[sendQueue.count - 1] = item
            inputMovesCoalesced += 1
            sendLock.signal()
            return true
        }

        if sendQueue.count >= maxSendQueue {
            // 队列满：优先丢最旧的可合并事件（拖动/移动），否则丢最旧的一条。
            // 无论如何都丢「旧」而不是丢「新」，保证最新状态（尤其是 DOWN/UP）能到达。
            if let idx = sendQueue.firstIndex(where: { $0.coalescable }) {
                sendQueue.remove(at: idx)
            } else {
                sendQueue.removeFirst()
            }
            inputPacketsDropped += 1
        }
        sendQueue.append(item)
        sendLock.signal()
        return true
    }

    /// 独立发送线程：串行取出队列并写 socket。所有写都带 MSG_DONTWAIT，
    /// 即使对端长时间不读也只会超时丢弃，绝不会永久卡住日志/控制路径。
    private func startSendThread() {
        sendLock.lock()
        sendThreadShouldStop = false
        sendLock.unlock()
        let thread = Thread { [weak self] in self?.sendLoop() }
        thread.name = "QRGameInputSend"
        sendThread = thread
        thread.start()
    }

    private func sendLoop() {
        while true {
            sendLock.lock()
            while sendQueue.isEmpty && !sendThreadShouldStop {
                sendLock.wait(until: Date().addingTimeInterval(0.2))
            }
            if sendThreadShouldStop { sendLock.unlock(); return }
            let item = sendQueue.removeFirst()
            sendLock.unlock()

            clientLock.lock()
            let fd = clientFD
            clientLock.unlock()
            guard fd >= 0 else { continue }

            if writeAll(fd: fd, bytes: item.packet) {
                sendLock.lock(); inputPacketsSent += 1; sendLock.unlock()
            } else {
                sendLock.lock(); inputPacketsDropped += 1; sendLock.unlock()
            }
        }
    }

    /// 把一整个 24 字节包写进 socket。
    /// 用 MSG_DONTWAIT + 短超时 poll 重试，单包最多等 2 秒：宁可丢一个包，也不让发送线程永久卡住。
    private func writeAll(fd: Int32, bytes: [UInt8]) -> Bool {
        var sent = 0
        let deadline = Date().addingTimeInterval(2.0)
        while sent < bytes.count {
            let n = bytes.withUnsafeBytes { raw -> Int in
                send(fd, raw.baseAddress!.advanced(by: sent), bytes.count - sent, MSG_DONTWAIT)
            }
            if n > 0 { sent += n; continue }
            if n < 0 && (errno == EAGAIN || errno == EWOULDBLOCK) {
                if Date() > deadline { return false }
                var pfd = pollfd(fd: fd, events: Int16(POLLOUT), revents: 0)
                _ = poll(&pfd, 1, 50)
                continue
            }
            return false   // EPIPE / EBADF 等：连接已不可用
        }
        return true
    }

    /// 每 5 秒输出一行输入通道诊断：连接状态、接受次数、队列长度、写出/丢弃/合并计数，
    /// 以及从客户端收到的触摸事件按 eventType 的分类计数。
    private func startDiagTimer() {
        let timer = DispatchSource.makeTimerSource(queue: encodeQueue)
        timer.schedule(deadline: .now() + 5, repeating: 5)
        timer.setEventHandler { [weak self] in
            guard let self, !self.isStoppedInternal else { return }
            self.sendLock.lock()
            let queued = self.sendQueue.count
            let sent = self.inputPacketsSent
            let dropped = self.inputPacketsDropped
            let coalesced = self.inputMovesCoalesced
            self.sendLock.unlock()
            let touch = MLInputInjector.shared.takeTouchStatsDescription()
            let mouse = MLInputInjector.shared.takeMouseStatsDescription()
            let cursor = self.cursorStatsSummary()
            let rev = self.reverseChannelStats()
            // 本窗口编码统计：帧率 / 平均每帧字节 / 触发来源（游戏新帧 vs 光标移动）
            self.encodeTrigger.lock()
            let encFrames = self.encodedFrameCount
            let encBytes = self.encodedBytesTotal
            let byGame = self.triggerByGameFrame
            let byCursor = self.triggerByCursorMove
            let workMicros = self.encodeWorkMicros
            let workCount = self.encodeWorkCount
            self.encodeTrigger.unlock()
            let dt = Date().timeIntervalSince(self.diagBaseAt)
            let dFrames = encFrames - self.diagBaseEncodedFrames
            let dBytes = encBytes - self.diagBaseEncodedBytes
            let dWorkMicros = workMicros &- self.diagBaseEncodeWorkMicros
            let dWorkCount = workCount - self.diagBaseEncodeWorkCount
            let windowFps = dt > 0 ? Double(dFrames) / dt : 0
            let avgBytes = dFrames > 0 ? Double(dBytes) / Double(dFrames) : 0
            // 循环周期（ms）与单帧实际占用 CPU 的时长（ms）：两者接近 = 编码本身是瓶颈；
            // 周期远大于占用 = 编码线程没被及时调度（CPU 饱和 / QoS 过低）。
            let periodMs = windowFps > 0 ? 1000.0 / windowFps : 0
            let workMs = dWorkCount > 0 ? Double(dWorkMicros) / Double(dWorkCount) / 1000.0 : 0
            self.diagBaseEncodedFrames = encFrames
            self.diagBaseEncodedBytes = encBytes
            self.diagBaseEncodeWorkMicros = workMicros
            self.diagBaseEncodeWorkCount = workCount
            self.diagBaseAt = Date()
            // 读取线程诊断：读次数/秒、每读平均消息数、含帧通知的读次数/秒。
            // 「含帧读次数/秒」就是 signalEncode 的触发率上限；它远低于通知数/秒即帧被合并丢弃。
            self.reverseLock.lock()
            let rReads = self.readerReads
            let rMsgs = self.readerMessages
            let rFrameBatches = self.readerFrameBatches
            let rMaxBatch = self.readerMaxBatch
            self.reverseLock.unlock()
            let dReads = rReads - self.diagBaseReaderReads
            let dMsgs = rMsgs - self.diagBaseReaderMessages
            let dFrameBatches = rFrameBatches - self.diagBaseReaderFrameBatches
            self.diagBaseReaderReads = rReads
            self.diagBaseReaderMessages = rMsgs
            self.diagBaseReaderFrameBatches = rFrameBatches
            let readsPerSec = dt > 0 ? Double(dReads) / dt : 0
            let msgsPerRead = dReads > 0 ? Double(dMsgs) / Double(dReads) : 0
            let frameBatchesPerSec = dt > 0 ? Double(dFrameBatches) / dt : 0
            let encode = String(format: "编码：本窗口 %.1f fps（周期 %.1f ms，单帧占用 %.1f ms）平均每帧 %.0f 字节 累计 %d 帧；触发[游戏新帧=%d 光标移动=%d]；读取线程[读/秒 %.0f 消息/读 %.1f 含帧读/秒 %.1f 单读最大 %d]；%s",
                                windowFps, periodMs, workMs, avgBytes, encFrames, byGame, byCursor,
                                readsPerSec, msgsPerRead, frameBatchesPerSec, rMaxBatch,
                                self.reverseFallbackActive ? "反向通道=回退单缓冲" : (self.isDualBuffer ? "反向通道=双缓冲" : "反向通道=单缓冲"))
            self.onLog?("输入通道：连接=\(self.isInputConnected ? "是" : "否") 接受次数=\(self.inputAcceptCount) 队列=\(queued) 已写出=\(sent) 丢弃=\(dropped) 合并=\(coalesced)；反向通道=\(self.isDualBuffer ? "双缓冲" : "单缓冲") 通知=\(rev.received) 最近seq=\(rev.lastSeq) 槽位=\(rev.lastSlot)；\(encode)；\(touch)；\(mouse)；\(cursor)")
        }
        diagTimer = timer
        timer.resume()
    }

    private static func writeFloat(_ value: Float, into buffer: inout [UInt8], at offset: Int) {
        var bits = value.bitPattern.littleEndian
        withUnsafeBytes(of: &bits) { raw in
            for i in 0..<4 { buffer[offset + i] = raw[i] }
        }
    }

    /// 便捷封装：鼠标移动到 (x, y)
    @discardableResult
    func sendMouseMove(x: Float, y: Float, dx: Float = 0, dy: Float = 0) -> Bool {
        sendInput(kind: 0, pressed: false, code: 0, x: x, y: y, dx: dx, dy: dy)
    }

    /// 便捷封装：鼠标按键（Godot MouseButton：1=左 2=右 3=中）
    @discardableResult
    func sendMouseButton(_ button: UInt16, pressed: Bool, x: Float, y: Float) -> Bool {
        sendInput(kind: 1, pressed: pressed, code: button, x: x, y: y, dx: 0, dy: 0)
    }

    // MARK: - 虚拟光标位置跟踪与合成

    /// 记录一次鼠标输入带来的虚拟指针位置（游戏像素，左上原点）。
    ///
    /// 位置由宿主（MLInputInjector）换算：绝对鼠标直接换算，相对移动累加后换算；这里只接收最终像素。
    /// 首次鼠标输入时，若还没有位置，先初始化到画面中心，保证光标有一个确定的起点。
    private func noteMouseCursor(x: CGFloat, y: CGFloat) {
        cursorLock.lock()
        let moved = !cursorValid || cursorPoint.x != x || cursorPoint.y != y
        if !cursorHasMouseInput {
            cursorHasMouseInput = true
            if !cursorValid {
                cursorPoint = CGPoint(x: CGFloat(width) / 2, y: CGFloat(height) / 2)
                cursorValid = true
            }
        }
        cursorPoint = CGPoint(x: x, y: y)
        cursorValid = true
        cursorLock.unlock()
        // 位置真的变了才唤醒编码：光标更新由此获得与客户端帧率同级的刷新能力，
        // 即使游戏没有出新帧，也能靠「复用上一完成槽位 + 新光标位置」编出光标移动帧。
        if moved { signalEncode(gameFrame: false, cursorMove: true) }
    }

    /// 光标箭头的包围盒（左上原点坐标）。形状与 MLCompose.drawCursor 一致：
    /// 尖端在 point，向右下展开约 (0.68, 1.12) × (20×scale)，四周留出描边/抗锯齿余量。
    private func cursorBounds(around point: CGPoint, scale: CGFloat, canvas: CGSize) -> CGRect {
        let s = 20 * scale
        let pad = 2.0
        let raw = CGRect(x: point.x - pad, y: point.y - pad, width: 0.68 * s + pad * 2, height: 1.12 * s + pad * 2)
        return raw.intersection(CGRect(origin: .zero, size: canvas))
    }

    /// 逐字节比较「当前表面在 rect 内的内容」与上次画完光标后保存的快照。
    /// 相等说明这块区域自上次绘制后没被游戏重绘（光标还在），可以跳过重绘。
    private func cursorRegionMatches(base: UnsafeMutableRawPointer, bytesPerRow: Int,
                                     rect: CGRect, snapshot: [UInt8]) -> Bool {
        let x0 = Int(rect.minX), y0 = Int(rect.minY)
        let w = Int(rect.width), h = Int(rect.height)
        guard w > 0, h > 0, snapshot.count == w * h * 4 else { return false }
        let base8 = base.assumingMemoryBound(to: UInt8.self)
        var index = 0
        for row in 0..<h {
            let p = base8 + (y0 + row) * bytesPerRow + x0 * 4
            for i in 0..<(w * 4) {
                if p[i] != snapshot[index] { return false }
                index += 1
            }
        }
        return true
    }

    /// 拷贝「当前表面在 rect 内的内容」作为快照。
    private func cursorRegionSnapshot(base: UnsafeMutableRawPointer, bytesPerRow: Int, rect: CGRect) -> [UInt8] {
        let x0 = Int(rect.minX), y0 = Int(rect.minY)
        let w = Int(rect.width), h = Int(rect.height)
        guard w > 0, h > 0 else { return [] }
        let base8 = base.assumingMemoryBound(to: UInt8.self)
        var out = [UInt8](repeating: 0, count: w * h * 4)
        var index = 0
        for row in 0..<h {
            let p = base8 + (y0 + row) * bytesPerRow + x0 * 4
            for i in 0..<(w * 4) { out[index] = p[i]; index += 1 }
        }
        return out
    }

    /// 把虚拟光标合成进指定槽位的 IOSurface（BGRA）。**调用前必须已对该 surface 加写锁**。
    ///
    /// 只做小矩形（脏区）绘制：用 CGContext 包住整块显存、但 clip 到光标包围盒，
    /// 因此只有光标那几个像素被写，不做整帧遍历。
    ///
    /// 只在「该槽位刚被 GPU 写完、本帧不再被写」时调用（由 encodeOneFrame 保证）。
    ///
    /// - Returns: 本帧是否真的进行了合成（false = 跳过或未显示）。
    @discardableResult
    private func compositeCursorLocked(slot: Int) -> Bool {
        guard cursorCompositingEnabled else { return false }
        guard slot >= 0, slot < surfaces.count, let surface = surfaces[slot] else { return false }

        cursorLock.lock()
        let hasInput = cursorHasMouseInput
        let visible = cursorValid
        var point = cursorPoint
        let drawnPoint = cursorDrawnPoint[slot]
        let pendingClean = cursorClean[slot]
        let pendingCleanRect = cursorCleanRect[slot]
        cursorLock.unlock()

        guard hasInput, visible else {
            cursorLock.lock(); cursorSkipNoMouseCount += 1; cursorLock.unlock()
            return false
        }

        let canvas = CGSize(width: width, height: height)
        let scale = MLCompose.uiScale(for: canvas)
        // clamp 到画面内（相对移动累加可能越界）
        point.x = min(max(point.x, 0), canvas.width - 1)
        point.y = min(max(point.y, 0), canvas.height - 1)
        let rect = cursorBounds(around: point, scale: scale, canvas: canvas)
        guard rect.width >= 1, rect.height >= 1 else { return false }

        let base = IOSurfaceGetBaseAddress(surface)
        let bytesPerRow = Int(IOSurfaceGetBytesPerRow(surface))

        // 「指针位置没变且该槽位脏区未被重绘」→ 直接跳过（快照逐字节相等即证明光标还在）。
        // 双缓冲下各槽位内容不同，快照按槽位分别保存，比较的也是该槽位自身。
        if let drawnPoint, drawnPoint == point, cursorSnapshotRect[slot] == rect, let snapshot = cursorSnapshot[slot],
           cursorRegionMatches(base: base, bytesPerRow: bytesPerRow, rect: rect, snapshot: snapshot) {
            cursorLock.lock(); cursorSkipUnchangedCount += 1; cursorLock.unlock()
            return false
        }

        // 关键（消除残影）：若本槽位上还留着上一次画的光标（且其干净快照仍有效，即 GPU 尚未重写该槽位），
        // 先把那块区域恢复成「画光标之前」的干净游戏内容，再在新位置画。否则「光标移动但游戏没出新帧」
        // 时会在旧位置留下一个残影。收到新的完成通知时 invalidateCursorState 会清掉这个快照，
        // 因此这里只会擦「确实还在、且未被 GPU 覆盖」的旧光标。
        if let clean = pendingClean {
            cursorRestoreRegion(base: base, bytesPerRow: bytesPerRow, rect: pendingCleanRect, bytes: clean)
            cursorLock.lock(); cursorClean[slot] = nil; cursorLock.unlock()
        }

        // 画之前先记录该脏区的干净内容（供下一次擦除用）。
        let cleanSnap = cursorRegionSnapshot(base: base, bytesPerRow: bytesPerRow, rect: rect)

        // CGContext 直接包住 IOSurface 显存（BGRA，左上原点，白色光标由 MLCompose 负责翻转到 CG 坐标）。
        let bitmapInfo = CGImageAlphaInfo.premultipliedFirst.rawValue | CGBitmapInfo.byteOrder32Little.rawValue
        guard let ctx = CGContext(data: base, width: width, height: height, bitsPerComponent: 8,
                                  bytesPerRow: bytesPerRow, space: CGColorSpaceCreateDeviceRGB(),
                                  bitmapInfo: bitmapInfo) else { return false }
        ctx.saveGState()
        ctx.clip(to: MLCompose.flip(rect, canvas))          // 脏区裁剪：只写这一小块
        MLCompose.drawCursor(ctx, at: point, size: canvas, scale: scale)
        ctx.restoreGState()

        cursorLock.lock()
        cursorDrawnPoint[slot] = point
        cursorSnapshot[slot] = cursorRegionSnapshot(base: base, bytesPerRow: bytesPerRow, rect: rect)
        cursorSnapshotRect[slot] = rect
        cursorClean[slot] = cleanSnap
        cursorCleanRect[slot] = rect
        cursorBlitCount += 1
        // 回读光标尖端附近的像素（BGRA），确认写入落地。
        let sx = min(max(Int(point.x) + 3, 0), width - 1)
        let sy = min(max(Int(point.y) + 3, 0), height - 1)
        let sp = base.assumingMemoryBound(to: UInt8.self) + sy * bytesPerRow + sx * 4
        cursorLastBlitPixel = "B\(sp[0])G\(sp[1])R\(sp[2])A\(sp[3])"
        cursorLock.unlock()
        return true
    }

    /// 把之前保存的干净内容写回 rect 区域（擦除画在那里的光标）。
    private func cursorRestoreRegion(base: UnsafeMutableRawPointer, bytesPerRow: Int,
                                     rect: CGRect, bytes: [UInt8]) {
        let x0 = Int(rect.minX), y0 = Int(rect.minY)
        let w = Int(rect.width), h = Int(rect.height)
        guard w > 0, h > 0, bytes.count == w * h * 4 else { return }
        let base8 = base.assumingMemoryBound(to: UInt8.self)
        var index = 0
        for row in 0..<h {
            let p = base8 + (y0 + row) * bytesPerRow + x0 * 4
            for i in 0..<(w * 4) { p[i] = bytes[index]; index += 1 }
        }
    }

    /// 光标诊断快照（含用户开关状态、合成/各类跳过计数、当前位置）。
    func cursorStatsSummary() -> String {
        cursorLock.lock(); defer { cursorLock.unlock() }
        let mode = cursorHasMouseInput ? "鼠标" : "触摸"
        let shown = (cursorHasMouseInput && cursorValid) ? "显示" : "隐藏"
        let onOff = cursorCompositingEnabled ? "开" : "关"
        let pos = cursorValid ? String(format: "(%.0f,%.0f)", cursorPoint.x, cursorPoint.y) : "无"
        return "光标：开关=\(onOff) 模式=\(mode) \(shown) 位置=\(pos) 合成=\(cursorBlitCount) 未变跳过=\(cursorSkipUnchangedCount) 无鼠标跳过=\(cursorSkipNoMouseCount) 无通知跳过=\(cursorSkipNoNotifyCount) 回读=\(cursorLastBlitPixel)"
    }

    /// 自测：从当前帧起再过 n 帧，把画面导出到 path。
    func scheduleFrameDump(afterFramesFromNow n: Int, to path: String) {
        dumpPNGPath = path
        dumpAfterFrame = Int(frameIndex) + n
    }

    /// 自测：把连续的 count 帧各导出一个 PNG 到 directory（文件名 frame-000.png …），
    /// 用于统计「光标丢失帧数」。仅自测用。
    func scheduleFrameDumpSequence(count: Int, directory: String) {
        dumpSequenceDir = directory
        dumpSequenceTotal = count
        dumpSequenceRemaining = count
    }

    /// 自测：对同一块槽位连续合成两次光标，验证第二次会因「位置没变 + 脏区未变」被跳过。
    /// 固定用 slot 0（单块模式下就是唯一那块；双缓冲下也能验证「未变跳过」优化仍生效）。
    /// - Returns: (第一次是否绘制, 第二次是否绘制, 第二次是否因「未变」被跳过(1/0), 脏区宽, 脏区高, 未变跳过累计, 合成累计)
    func cursorCompositeProbe() -> (firstBlit: Bool, secondBlit: Bool, secondSkipped: Int,
                                    dirtyW: Int, dirtyH: Int, skipUnchanged: Int, blit: Int) {
        guard let surface = surfaces[0], IOSurfaceLock(surface, [], nil) == kIOReturnSuccess else {
            return (false, false, 0, 0, 0, 0, 0)
        }
        let first = compositeCursorLocked(slot: 0)
        cursorLock.lock(); let skipAfterFirst = cursorSkipUnchangedCount; cursorLock.unlock()
        let second = compositeCursorLocked(slot: 0);   // 中间没有任何写操作，快照应逐字节相等 → 跳过
        cursorLock.lock()
        let secondSkipped = cursorSkipUnchangedCount - skipAfterFirst
        let rect = cursorSnapshotRect[0]
        let skip = cursorSkipUnchangedCount
        let blit = cursorBlitCount
        cursorLock.unlock()
        IOSurfaceUnlock(surface, [], nil)
        return (first, second, secondSkipped, Int(rect.width), Int(rect.height), skip, blit)
    }

    // MARK: - 编码

    private func createCompressionSession() throws {
        var session: VTCompressionSession?
        let refcon = Unmanaged.passUnretained(self).toOpaque()
        // 明确要求「优先硬件编码器」：4K 下若 VideoToolbox 静默回退到软件编码，CPU 会被吃满、
        // 出帧率塌方。这里用 Enable（而非 Require）——个别环境无硬件编码器时仍可降级建会话，
        // 但是否真的走了硬件会在建会话后读 UsingHardwareAcceleratedVideoEncoder 打日志确认。
        let encoderSpec: CFDictionary = [
            kVTVideoEncoderSpecification_EnableHardwareAcceleratedVideoEncoder: kCFBooleanTrue as Any
        ] as CFDictionary
        let status = VTCompressionSessionCreate(
            allocator: kCFAllocatorDefault,
            width: Int32(width), height: Int32(height),
            codecType: kCMVideoCodecType_H264,
            encoderSpecification: encoderSpec,
            imageBufferAttributes: nil,
            compressedDataAllocator: nil,
            outputCallback: { refcon, _, _, _, sampleBuffer in
                guard let refcon, let sampleBuffer else { return }
                let session = Unmanaged<MLGameStreamSession>.fromOpaque(refcon).takeUnretainedValue()
                session.handleEncodedFrame(sampleBuffer)
            },
            refcon: refcon,
            compressionSessionOut: &session)
        guard status == noErr, let session else {
            throw MLGameStreamError.compressionSessionFailed(status)
        }

        // 硬解/硬编确认：读取实际生效的编码器类型打日志。
        // 「软件编码！」出现在 4K 时基本就是出帧率塌方的元凶（CPU 编码 4K 追不上 30fps）。
        var usingHW: CFTypeRef?
        if VTSessionCopyProperty(session, key: kVTCompressionPropertyKey_UsingHardwareAcceleratedVideoEncoder,
                                 allocator: kCFAllocatorDefault, valueOut: &usingHW) == noErr,
           let num = usingHW as? NSNumber {
            onLog?("编码器：\(width)x\(height)@\(fps) 码率 \(bitrateBps / 1000)Kbps，硬件加速=\(num.boolValue ? "是" : "否（软件编码！）")")
        } else {
            onLog?("编码器：\(width)x\(height)@\(fps) 码率 \(bitrateBps / 1000)Kbps，无法读取硬件加速标志")
        }

        // 参数选择理由：
        //  · RealTime + 关掉帧重排序 = 零 B 帧、编码延迟最小，交互式串流必须；
        //  · H264_High_AutoLevel = 1080p 常规选择，兼容绝大多数客户端解码器；
        //  · DataRateLimits（VBV）= 把关键帧体积压在不超过窗口预算内，避免 IDR 突刺打爆链路；
        //  · MaxKeyFrameInterval = fps × 10（10 秒）——**不能取 fps（= 1 秒）**，那是「卡顿爆音」的元凶：
        //      本机 1080p 的 IDR 约 100~240KB；1 秒一次关键帧等于每秒把 70~170 个 UDP 包
        //      （实测单帧连发 4~8ms）在几毫秒内一次性灌进链路。而客户端音频是「每 5ms 一包、
        //      到包即解码播放」，抖动缓冲只有约 2~3 个包（~10~15ms）：这波突发会把音频包整体
        //      推迟十几到几十毫秒 → 客户端音频缓冲见底 → 每秒一声「啪」，听感就是持续爆音；
        //      视频包同样被挤迟到/挤丢 → 卡顿、客户端反复请求 IDR。
        //      宿主侧日志此时完全正常（音频在自己队列上按时投递，只看得见「交给内核」的时刻），
        //      故障只发生在链路与客户端——正是「宿主指标干净、客户端却卡顿爆音」的成因。
        //      10 秒 ≈ NVENC 默认 gopLength（250 帧 @30fps）；Sunshine 干脆用 NVENC_INFINITE_GOPLENGTH，
        //      关键帧完全按需（客户端 0x0302 请求 / RFI）发送。这里留一个 10 秒兜底即可。
        //
        //      复现/回归开关：QR_GAMESTREAM_KEYFRAME_INTERVAL=1 可把间隔还原成 1 秒（旧行为），
        //      用于对照「视频：本窗口 …（IDR N 条 M 包）」里每秒多出来的那 4~5 条大突发。
        let keyFrameIntervalSecs = Double(ProcessInfo.processInfo.environment["QR_GAMESTREAM_KEYFRAME_INTERVAL"] ?? "") ?? 10.0
        let keyFrameIntervalFrames = max(1, Int(Double(fps) * max(0.1, keyFrameIntervalSecs)))
        let properties: [CFString: Any] = [
            kVTCompressionPropertyKey_RealTime: kCFBooleanTrue as Any,
            kVTCompressionPropertyKey_AllowFrameReordering: kCFBooleanFalse as Any,
            kVTCompressionPropertyKey_ProfileLevel: kVTProfileLevel_H264_High_AutoLevel,
            kVTCompressionPropertyKey_AverageBitRate: NSNumber(value: bitrateBps),
            kVTCompressionPropertyKey_DataRateLimits: [NSNumber(value: bitrateBps / 80), NSNumber(value: 0.1)] as CFArray,
            kVTCompressionPropertyKey_MaxKeyFrameInterval: NSNumber(value: keyFrameIntervalFrames),
            kVTCompressionPropertyKey_ExpectedFrameRate: NSNumber(value: fps),
            kVTCompressionPropertyKey_AllowTemporalCompression: kCFBooleanTrue as Any,
        ]
        for (key, value) in properties {
            VTSessionSetProperty(session, key: key, value: value as CFTypeRef)
        }
        compressionSession = session
    }

    /// 请求把下一帧编成 IDR（关键帧）。
    ///
    /// 客户端丢包后会高频请求关键帧（PS5 路径实测可达每秒上百次），这里与 requestPS5KeyFrame 一样
    /// 做 0.5 秒间隔限制，避免 IDR 风暴（每个 IDR 都是几十 KB，越要越堵）。
    /// 若 VTCompressionSession 尚未就绪（还没启动 / 已停止），安全忽略而不是崩溃。
    ///
    /// - Returns: 是否接受了本次请求（未就绪或被限流时返回 false）
    @discardableResult
    func requestKeyFrame() -> Bool {
        keyFrameLock.lock()
        defer { keyFrameLock.unlock() }
        // 会话还没建好编码器（或已停止）：忽略。强解包会崩，这里只做存在性检查。
        guard compressionSession != nil else { return false }
        let now = Date()
        if let last = lastKeyFrameRequestAt, now.timeIntervalSince(last) < 0.5 {
            rejectedKeyFrameRequestCount += 1
            return false
        }
        lastKeyFrameRequestAt = now
        // 只置标志位、不直接编：由编码队列在下一帧消费，保证与取帧/送编同一时序，不引入并发编码
        keyFrameRequested = true
        return true
    }

    /// 诊断快照：(实际强制出来的 IDR 数, 被限流拒绝的请求数)
    func keyFrameRequestStats() -> (forced: Int, rejected: Int) {
        keyFrameLock.lock(); defer { keyFrameLock.unlock() }
        return (forcedKeyFrameCount, rejectedKeyFrameRequestCount)
    }

    /// 诊断快照：(累计编码帧数, 累计编码字节, 游戏新帧触发次数, 光标移动触发次数)。
    /// 供自测统计「编码帧率 / 平均每帧字节 / 光标更新速率（≈光标触发次数）」。
    func encodeStats() -> (frames: Int, bytes: Int, byGame: Int, byCursor: Int) {
        encodeTrigger.lock(); defer { encodeTrigger.unlock() }
        return (encodedFrameCount, encodedBytesTotal, triggerByGameFrame, triggerByCursorMove)
    }

    /// 轮询表面内容，等「画面起来」再开始出帧。
    ///
    /// 为什么不固定 sleep N 秒：.NET 运行时 + 场景加载的耗时随工程而变（真实游戏比测试工程慢很多），
    /// 固定延时要么太短（编出一堆全黑帧）要么太长（白等）。这里以「表面出现非黑像素」为准，
    /// 同时给一个上限兜底（超时就照常开始，宁可先编黑帧也不无限等）。
    private func startReadyProbe() {
        let started = Date()
        let timer = DispatchSource.makeTimerSource(queue: encodeQueue)
        timer.schedule(deadline: .now() + 0.5, repeating: 0.5)
        timer.setEventHandler { [weak self] in
            guard let self else { return }
            if self.isStoppedInternal { return }
            let ratio = self.currentFrameNonBlackRatio()
            let elapsed = Date().timeIntervalSince(started)
            if ratio > 0.02 || elapsed > 20 {
                self.readyTimer?.cancel()
                self.readyTimer = nil
                if ratio > 0.02 {
                    self.onLog?(String(format: "画面已就绪（非黑占比 %.2f%%，用时 %.1fs），开始出帧", ratio * 100, elapsed))
                } else {
                    self.onLog?(String(format: "等待画面就绪超时（%.0fs），仍开始出帧", elapsed))
                }
                self.startEncodeLoop()
            }
        }
        readyTimer = timer
        timer.resume()
    }

    /// 启动编码出帧：用 DispatchSourceTimer 按「协商帧率的 2 倍」节拍驱动（保证不漏帧，
    /// 真正的出帧速率由 encodeTick 里的 minInterval 限速卡在协商帧率内）。
    private func startEncodeLoop() {
        guard !isStoppedInternal else { return }
        // 节拍取协商帧率的 4 倍：定时器节拍是离散的，若只取 1~2 倍，限速点（1/fps）
        // 与节拍会形成「拍频」——每次都晚一拍才够限速，把 30fps 拖到 ~20fps。
        encodeTickPeriod = max(0.001, 1.0 / Double(max(1, fps)) / 4.0)
        let tickMs = max(1, Int(encodeTickPeriod * 1000))
        let timer = DispatchSource.makeTimerSource(queue: encodeTickQueue)
        timer.schedule(deadline: .now(), repeating: .milliseconds(tickMs), leeway: .microseconds(200))
        timer.setEventHandler { [weak self] in self?.encodeTick() }
        encodeTickTimer = timer
        timer.resume()
    }

    /// 编码一拍：有新游戏帧 / 光标移动才编一帧；都没有就跳过（不编码）。
    ///
    /// 限速在协商帧率内（minInterval = 1/fps），因此：
    ///   · 游戏 60fps + 光标 60fps → 最多 60fps；
    ///   · 游戏 30fps + 光标 60fps → 游戏帧与光标帧交错，约 60fps（无重复编码同一游戏帧两次）；
    ///   · 游戏 30fps + 光标不动   → 约 30fps（只跟随游戏帧，不再无脑 60fps 重复编）；
    ///   · 游戏不出新帧 + 光标不动 → 完全不编码。
    private func encodeTick() {
        guard !isStoppedInternal, compressionSession != nil else { return }

        // 限速：距上一帧不足一帧间隔就跳过本拍（保留 pending，下一拍再编），
        // 既保证不把编码速率放大到协商帧率之上，又不会丢帧。
        // 容差 = 半个节拍：离散节拍不可能正好落在限速点上，不给容差会让每次都晚一整拍。
        let minInterval = 1.0 / Double(fps)
        if Date().timeIntervalSince(lastEncodeAt) < minInterval - encodeTickPeriod / 2 { return }

        var byGame = false
        var byCursor = false
        encodeTrigger.lock()
        if pendingGameFrame || pendingCursorMove {
            byGame = pendingGameFrame
            byCursor = pendingCursorMove
            pendingGameFrame = false
            pendingCursorMove = false
        } else if !sawReverseNotification || forceTimerDrive {
            byGame = true   // 无反向通道 / 强制旧行为：固定节拍出帧
        } else {
            encodeTrigger.unlock()
            return
        }
        if byGame { triggerByGameFrame += 1 }
        if byCursor { triggerByCursorMove += 1 }
        encodeTrigger.unlock()

        lastEncodeAt = Date()

        let workStart = CFAbsoluteTimeGetCurrent()
        encodeOneFrame()
        let workMicros = UInt64(max(0, CFAbsoluteTimeGetCurrent() - workStart) * 1_000_000)
        encodeTrigger.lock()
        encodeWorkMicros &+= workMicros
        encodeWorkCount += 1
        encodeTrigger.unlock()
    }

    private func encodeOneFrame() {
        guard !isStoppedInternal, let session = compressionSession else { return }

        // 槽位选择与光标合成策略（消除「光标闪烁 / 残影」的核心）：
        //   · 收到过完成通知：只处理「刚被 GPU 写完」的槽位 —— 它要再被写必须等下一轮 ping-pong
        //     （至少一整帧之后），因此在此槽位上合成光标 + 编码期间不会被 GPU 改写，竞态从根上消除；
        //   · 宽限期内还没收到通知：只出帧、不合成光标，绝不猜一个可能正被 GPU 写的槽位去画光标；
        //   · 超过宽限期仍无通知（旧版引擎，无反向通道）：回退单缓冲语义（slot 0 且照常合成光标），
        //     行为与改造前一致，只打一次日志说明，绝不卡死。
        let notifiedSlot = completedSlotForEncoding()
        let slot: Int
        let compositable: Bool
        if notifiedSlot >= 0 {
            slot = notifiedSlot
            compositable = true
        } else if Date() < reverseGraceDeadline {
            slot = 0
            compositable = false
        } else {
            if !reverseFallbackActive {
                reverseFallbackActive = true
                onLog?("反向完成通道超时未收到通知（疑似旧版引擎），回退单缓冲路径：只用 slot 0 且照常合成光标")
            }
            slot = 0
            compositable = true
        }
        guard let surface = surfaces[slot] else { return }
        guard let pixelBuffer = wrapSurfaceAsPixelBuffer(surface) else { return }

        // 时间戳按「真实经过时间」打（而不是 frameIndex/fps）：事件驱动是变速率出帧，
        // 用固定帧索引当时间轴会让客户端以为每帧都是 1/fps，导致播放节奏与真实时长对不上。
        if encodeWallClockStart == nil { encodeWallClockStart = Date() }
        let elapsed = Date().timeIntervalSince(encodeWallClockStart!)
        let pts = CMTime(seconds: elapsed, preferredTimescale: 600)
        let duration = CMTime(value: 1, timescale: Int32(fps))
        frameIndex += 1

        // 消费「客户端请求关键帧」标志：置位时给本帧加 ForceKeyFrame，编出一个 IDR。
        // 标志只作用于本帧，消费后立即清零——否则会每一帧都强制成 IDR，反而制造 IDR 风暴。
        keyFrameLock.lock()
        let forceKeyFrame = keyFrameRequested
        if forceKeyFrame {
            keyFrameRequested = false
            forcedKeyFrameCount += 1
        }
        keyFrameLock.unlock()
        let frameProperties: CFDictionary? = forceKeyFrame
            ? [kVTEncodeFrameOptionKey_ForceKeyFrame: kCFBooleanTrue] as CFDictionary
            : nil

        // 关于同步：IOSurfaceLock 只约束「CPU 访问」的一致性，不约束 GPU 写入。这里加锁是为了
        // 保护本进程内「编码器读 / 我们合成光标写」的交叉。跨进程的 GPU 写入竞态由上面的
        // 「只处理刚完成的槽位」解决（双缓冲 + 反向通道握手），不再依赖这把锁。
        let locked = IOSurfaceLock(surface, [], nil) == kIOReturnSuccess
        if locked {
            if compositable {
                // 取帧之后、交给编码器之前，把虚拟光标合成进这块（GPU 已不再写 / 或回退单缓冲）的 BGRA buffer。
                compositeCursorLocked(slot: slot)
            } else {
                cursorLock.lock(); cursorSkipNoNotifyCount += 1; cursorLock.unlock()
            }
            // 自测：在指定帧把「已合成光标」的画面导出 PNG（用于断言光标出现在期望坐标）。
            // 这里直接用当前已持有的写锁与基址，避免同一线程再嵌套一次 IOSurfaceLock。
            let base = IOSurfaceGetBaseAddress(surface)
            let bpr = Int(IOSurfaceGetBytesPerRow(surface))
            if let dumpPNGPath, dumpAfterFrame >= 0, frameIndex - 1 == Int64(dumpAfterFrame) {
                writeSurfacePNG(base: base, bytesPerRow: bpr, w: width, h: height, to: dumpPNGPath)
            }
            writeDumpSequenceIfNeeded(base: base, bytesPerRow: bpr)
        }
        VTCompressionSessionEncodeFrame(session,
                                        imageBuffer: pixelBuffer,
                                        presentationTimeStamp: pts,
                                        duration: duration,
                                        frameProperties: frameProperties,
                                        sourceFrameRefcon: nil,
                                        infoFlagsOut: nil)
        if locked { IOSurfaceUnlock(surface, [], nil) }
    }

    /// 自测：连续导出若干帧 PNG。调用方必须已持有该 surface 的写锁。
    private func writeDumpSequenceIfNeeded(base: UnsafeMutableRawPointer, bytesPerRow: Int) {
        guard let dir = dumpSequenceDir, dumpSequenceRemaining > 0 else { return }
        // 模拟编码器异步读取的时刻（见 dumpSequenceDelayUs 注释）：延迟后再导出。
        if dumpSequenceDelayUs > 0 { usleep(dumpSequenceDelayUs) }
        let index = dumpSequenceTotal - dumpSequenceRemaining
        let path = "\(dir)/frame-\(String(format: "%03d", index)).png"
        writeSurfacePNG(base: base, bytesPerRow: bytesPerRow, w: width, h: height, to: path, logCorners: false)
        dumpSequenceRemaining -= 1
        if dumpSequenceRemaining == 0 {
            onLog?("已连续导出 \(dumpSequenceTotal) 帧 PNG 到 \(dir)")
        }
    }

    // MARK: - 自测：把当前 IOSurface 导出为 PNG

    /// 导出当前帧为 PNG。调用方必须**已经**持有该 surface 的锁（编码路径里就是这种情况），
    /// 所以这里不再加锁，避免同一线程递归 IOSurfaceLock 失败。
    private func writeSurfacePNG(base: UnsafeMutableRawPointer, bytesPerRow: Int,
                                 w: Int, h: Int, to path: String, logCorners: Bool = true) {
        let cs = CGColorSpaceCreateDeviceRGB()
        let bitmapInfo = CGImageAlphaInfo.premultipliedFirst.rawValue | CGBitmapInfo.byteOrder32Little.rawValue
        // 复制一份像素：PNG 编码在 provider 生命周期内需要数据有效，避免与随后的解锁/重绘竞争。
        let data = Data(bytes: base, count: bytesPerRow * h)
        guard let provider = CGDataProvider(data: data as CFData),
              let image = CGImage(width: w, height: h, bitsPerComponent: 8, bitsPerPixel: 32,
                                  bytesPerRow: bytesPerRow, space: cs,
                                  bitmapInfo: CGBitmapInfo(rawValue: bitmapInfo),
                                  provider: provider, decode: nil, shouldInterpolate: false, intent: .defaultIntent),
              let dest = CGImageDestinationCreateWithURL(URL(fileURLWithPath: path) as CFURL,
                                                         "public.png" as CFString, 1, nil) else {
            onLog?("PNG 导出失败：无法创建图像（\(path)）")
            return
        }
        CGImageDestinationAddImage(dest, image, nil)
        let ok = CGImageDestinationFinalize(dest)
        guard logCorners else { return }
        // 采样四角与中心：留黑边/拉伸的最直接证据。
        func rgb(_ x: Int, _ y: Int) -> String {
            let p = base.assumingMemoryBound(to: UInt8.self) + y * bytesPerRow + x * 4
            return "(\(p[2]),\(p[1]),\(p[0]))"  // BGRA → (R,G,B)
        }
        let cx = w / 2, cy = h / 2
        onLog?(String(format: "PNG 导出 %@ %dx%d 角点: 左上=%@ 右上=%@ 左下=%@ 右下=%@ 中心=%@",
                      ok ? "成功" : "失败", w, h,
                      rgb(0, 0), rgb(w - 1, 0), rgb(0, h - 1), rgb(w - 1, h - 1), rgb(cx, cy)))
    }

    /// VideoToolbox 编码输出回调。
    ///
    /// 输出格式说明：VideoToolbox 的 H.264 输出是 **AVCC**（每个 NAL 前是 4 字节大端长度前缀），
    /// **不带 Annex-B 起始码**；并且 **SPS/PPS 不内嵌在码流里**，而是随 CMFormatDescription 给出。
    /// 这里直接把 AVCC 数据交给上层；参数集单独提取出来只为「参数集是否出现」的验证统计。
    /// （下一步接入 Moonlight 时，需要像 MLCapturePipeline 那样转成 Annex-B 并在 IDR 前内嵌 SPS/PPS。）
    private func handleEncodedFrame(_ sampleBuffer: CMSampleBuffer) {
        guard let formatDescription = CMSampleBufferGetFormatDescription(sampleBuffer),
              let blockBuffer = CMSampleBufferGetDataBuffer(sampleBuffer) else { return }

        let attachments = CMSampleBufferGetSampleAttachmentsArray(sampleBuffer, createIfNecessary: false) as? [[CFString: Any]]
        let notSync = attachments?.first?[kCMSampleAttachmentKey_NotSync] as? Bool ?? false
        let isKeyframe = !notSync
        if isKeyframe { extractParameterSets(formatDescription) }

        var length = 0
        var pointer: UnsafeMutablePointer<Int8>?
        let status = CMBlockBufferGetDataPointer(blockBuffer, atOffset: 0, lengthAtOffsetOut: nil,
                                                 totalLengthOut: &length, dataPointerOut: &pointer)
        guard status == kCMBlockBufferNoErr, let pointer, length > 0 else { return }

        let data = Data(bytes: pointer, count: length)
        let pts = CMSampleBufferGetPresentationTimeStamp(sampleBuffer)
        // 诊断统计（编码输出线程上调用，与编码循环共用 encodeTrigger 锁）
        encodeTrigger.lock()
        encodedFrameCount += 1
        encodedBytesTotal += data.count
        encodeTrigger.unlock()
        onEncodedFrame?(data, isKeyframe, pts)
    }

    /// 从 format description 里取出 SPS(index 0) / PPS(index 1)
    private func extractParameterSets(_ formatDescription: CMFormatDescription) {
        // 复用共享提取逻辑（与屏幕采集管线、串流桥接一致）
        let params = MLAnnexBConverter.parameterSets(from: formatDescription)
        if !params.sps.isEmpty { sps = params.sps }
        if !params.pps.isEmpty { pps = params.pps }
    }
}

// MARK: - 独立验证入口
//
//  为什么做成「静态方法 + 环境变量开关」而不是新建 target：
//    工程只有一个 target，新建命令行 target 需要额外维护一套构建设置，成本远大于收益。
//    这里用 QR_GAMESTREAM_SELFTEST=1 触发：应用启动后走一段自测流程，把结果打到 stdout 再退出。
//    运行方式见文件末尾注释。
//
extension MLGameStreamSession {

    /// 自测统计（编码回调在 VideoToolbox 的线程上、stdout 回调在管道线程上，必须加锁）
    private final class SelfTestStats {
        let lock = NSLock()
        var encodedFrames = 0
        var keyFrames = 0
        var totalBytes = 0
        var firstFrameSeen = false
        var firstFrameIsKey = false
        var gameLines: [String] = []
        var evMotion = 0
        var evButton = 0
    }

    /// 环境变量开关：QR_GAMESTREAM_SELFTEST=1 时启动自测
    static func runSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let frames = Int(env["QR_GAMESTREAM_FRAMES"] ?? "") ?? 60
        DispatchQueue.global(qos: .userInitiated).async {
            runSelfTest(executablePath: executable, projectPath: project, targetFrames: frames)
        }
    }

    /// 完整自测：起会话 → 等够 N 帧 → 发一次输入 → 统计非黑占比 / 窗口数 → 打印报告 → 退出
    static func runSelfTest(executablePath: String, projectPath: String, targetFrames: Int) {
        func emit(_ line: String) {
            print("[QRGAMESTREAM] \(line)")
            fflush(stdout)
        }

        emit("=== 自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("target     = \(targetFrames) 帧")

        let session = MLGameStreamSession(width: 1920, height: 1080, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let stats = SelfTestStats()

        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            stats.lock.lock()
            stats.gameLines.append(line)
            if line.contains("EV ") {
                if line.contains("InputEventMouseMotion") { stats.evMotion += 1 }
                if line.contains("InputEventMouseButton") { stats.evButton += 1 }
            }
            stats.lock.unlock()
        }
        session.onEncodedFrame = { data, isKey, _ in
            stats.lock.lock()
            if !stats.firstFrameSeen {
                stats.firstFrameSeen = true
                stats.firstFrameIsKey = isKey
            }
            stats.encodedFrames += 1
            if isKey { stats.keyFrames += 1 }
            stats.totalBytes += data.count
            stats.lock.unlock()
        }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // 1) 等够帧数
        let deadline = Date().addingTimeInterval(45)
        while Date() < deadline {
            stats.lock.lock(); let count = stats.encodedFrames; stats.lock.unlock()
            if count >= targetFrames { break }
            usleep(200_000)
        }

        // 2) 输入通道验证：必须等场景就绪之后再发（这里帧数已达标，说明画面早已起来）
        emit("输入 socket 已连接 = \(session.isInputConnected)")
        usleep(300_000)
        let moveOK = session.sendMouseMove(x: 500, y: 300, dx: 5, dy: 0)
        usleep(300_000)
        let downOK = session.sendMouseButton(1, pressed: true, x: 500, y: 300)
        usleep(200_000)
        let upOK = session.sendMouseButton(1, pressed: false, x: 500, y: 300)
        emit("sendInput 写出：move=\(moveOK) buttonDown=\(downOK) buttonUp=\(upOK)")
        usleep(1_200_000)   // 等游戏把事件打印到 stdout

        // 3) 画面真实性：非黑像素占比
        let ratio = session.currentFrameNonBlackRatio()

        // 4) 确认屏幕上没有 Godot 窗口
        let windows = onScreenWindowInfo(pid: session.processIdentifier)

        // 5) 汇总
        stats.lock.lock()
        let encoded = stats.encodedFrames
        let keys = stats.keyFrames
        let bytes = stats.totalBytes
        let firstKey = stats.firstFrameIsKey
        let evMotion = stats.evMotion
        let evButton = stats.evButton
        let evSamples = stats.gameLines.filter { $0.contains("EV ") }.prefix(4).map { $0 }
        stats.lock.unlock()

        emit("---- 编码统计 ----")
        emit("编码成功帧数 = \(encoded)")
        emit("其中关键帧数 = \(keys)")
        emit("输出总字节数 = \(bytes)")
        emit(String(format: "平均每帧字节 = %.1f", encoded > 0 ? Double(bytes) / Double(encoded) : 0))
        emit("首帧是关键帧 = \(firstKey)")
        emit("SPS 字节数 = \(session.sps.count)，PPS 字节数 = \(session.pps.count)")
        emit(String(format: "IOSurface 非黑像素占比 = %.4f%%", ratio * 100))
        emit("屏幕上 Godot 窗口数（onscreen）= \(windows.count)")
        for line in windows.descriptions { emit("  window: \(line)") }
        emit("---- 输入通道 ----")
        emit("游戏收到 InputEventMouseMotion 次数 = \(evMotion)")
        emit("游戏收到 InputEventMouseButton 次数 = \(evButton)")
        for line in evSamples { emit("  game: \(line)") }

        session.stop()
        emit("=== 自测结束 ===")
        exit(0)
    }

    // MARK: - 按需关键帧自测（QR_GAMESTREAM_IDR_SELFTEST=1）
    //
    //  目的：用真实游戏验证三件事：
    //    A) 未请求时仍按 MaxKeyFrameInterval（= fps）周期出 IDR（行为未被破坏）；
    //    B) 中途触发一次 requestKeyFrame()，紧接着的下一帧就是 IDR，且前置了 SPS/PPS；
    //    C) 高频请求被 0.5s 限流，实际只强制出少数 IDR，避免 IDR 风暴。
    //  运行方式见文件末尾注释。

    static func runKeyFrameSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_IDR_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let spamCount = Int(env["QR_GAMESTREAM_IDR_SPAM"] ?? "") ?? 300
        DispatchQueue.global(qos: .userInitiated).async {
            runKeyFrameSelfTest(executablePath: executable, projectPath: project, spamCount: spamCount)
        }
    }

    /// 逐帧记录（编码回调在 VideoToolbox 线程上追加，验证线程读取，必须加锁）
    private final class KeyFrameSelfTestLog {
        let lock = NSLock()
        var isKey: [Bool] = []
        var nal: [[UInt8]] = []
        var bytes: [Int] = []
        func append(_ key: Bool, _ types: [UInt8], _ size: Int) {
            lock.lock(); isKey.append(key); nal.append(types); bytes.append(size); lock.unlock()
        }
        var count: Int { lock.lock(); defer { lock.unlock() }; return isKey.count }
        func snapshot() -> (isKey: [Bool], nal: [[UInt8]], bytes: [Int]) {
            lock.lock(); defer { lock.unlock() }; return (isKey, nal, bytes)
        }
    }

    private static func runKeyFrameSelfTest(executablePath: String, projectPath: String, spamCount: Int) {
        func emit(_ line: String) {
            print("[QRIDR] \(line)")
            fflush(stdout)
        }

        emit("=== 按需关键帧自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")

        let session = MLGameStreamSession(width: 1920, height: 1080, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let log = KeyFrameSelfTestLog()

        session.onLog = { emit("log: \($0)") }
        // 观察桥接后的 Annex-B（含 IDR 前置的 SPS/PPS），即真正送进视频通道的那份字节
        bridge.onFrame = { data, isIDR, _ in
            log.append(isIDR, nalTypes(data), data.count)
        }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // ---- 阶段 A：未请求时的周期 IDR ----
        let phaseAEnd = Date().addingTimeInterval(20)
        while Date() < phaseAEnd, log.count < 70 { usleep(100_000) }
        let a = log.snapshot()
        let periodicKeys = a.isKey.enumerated().filter { $0.element }.map { $0.offset }
        var gaps: [Int] = []
        for i in 1..<periodicKeys.count { gaps.append(periodicKeys[i] - periodicKeys[i - 1]) }
        let statsA = session.keyFrameRequestStats()
        emit("---- 阶段A：未请求时的周期 IDR ----")
        emit("已编码帧数 = \(a.isKey.count)，其中 IDR 帧序号 = \(periodicKeys)")
        emit("相邻 IDR 间隔（帧） = \(gaps)")
        if !a.nal.isEmpty { emit("首帧 NAL 类型序列 = \(a.nal[0])（应含 SPS(7)/PPS(8)/IDR(5)）") }
        emit("此时强制 IDR 计数 = \(statsA.forced)（应为 0），被限流拒绝计数 = \(statsA.rejected)（应为 0）")

        // ---- 阶段 B：中途触发一次 requestKeyFrame，确认紧接着的下一帧就是 IDR ----
        let n = log.count
        let acceptedB = session.requestKeyFrame()
        emit("---- 阶段B：中途触发一次 requestKeyFrame ----")
        emit("请求时已编码帧数 = \(n)，requestKeyFrame() 返回 = \(acceptedB)")
        let bDeadline = Date().addingTimeInterval(5)
        while Date() < bDeadline, log.count < n + 6 { usleep(20_000) }
        let b = log.snapshot()
        // 「请求后第一个 IDR」= 序号 >= n 的第一个关键帧；周期 IDR 要 ~30 帧后才来，
        // 若这里只隔了极少几帧就出现 IDR，即证明是本次强制出来的。
        if let k = (n..<b.isKey.count).first(where: { b.isKey[$0] }) {
            emit("请求后第一个编码帧：帧序号 = \(n)，是否 IDR = \(b.isKey[n])")
            emit("请求后第一个 IDR 帧序号 = \(k)，距请求 \(k - n) 帧")
            emit("该帧 NAL 类型序列 = \(b.nal[k])（应含 SPS(7)/PPS(8)/IDR(5)）")
            emit("该帧字节数 = \(b.bytes[k])")
        } else {
            emit("未观察到请求后的关键帧（异常）")
        }
        let statsB = session.keyFrameRequestStats()
        emit("阶段B 结束时强制 IDR 计数 = \(statsB.forced)（应为 \(statsA.forced + 1)），被限流拒绝计数 = \(statsB.rejected)")

        // ---- 阶段 C：限流（高频请求只强制出少数 IDR）----
        let beforeC = session.keyFrameRequestStats()
        var accepted = 0
        var rejected = 0
        let cStart = Date()
        for _ in 0..<spamCount {
            if session.requestKeyFrame() { accepted += 1 } else { rejected += 1 }
            usleep(10_000)
        }
        let span = Date().timeIntervalSince(cStart)
        let afterC = session.keyFrameRequestStats()
        emit("---- 阶段C：限流（连发 \(spamCount) 次 requestKeyFrame，间隔 10ms）----")
        emit(String(format: "总发出 = %d，接受 = %d，被限流拒绝 = %d，耗时 %.2fs", spamCount, accepted, rejected, span))
        emit("强制 IDR 计数增量 = \(afterC.forced - beforeC.forced)（阶段C 实际强制出的 IDR）")
        emit("被限流拒绝计数增量 = \(afterC.rejected - beforeC.rejected)")
        emit(String(format: "理论上限 ≈ 跨度/0.5s ≈ %.1f 次", span / 0.5))

        session.stop()
        emit("=== 按需关键帧自测结束 ===")
        exit(0)
    }

    /// 从 Annex-B 码流里取出每个 NAL 的 nal_unit_type（低 5 位）
    private static func nalTypes(_ annexB: [UInt8]) -> [UInt8] {
        var types: [UInt8] = []
        var i = 0
        while i + 3 < annexB.count {
            if annexB[i] == 0, annexB[i + 1] == 0, annexB[i + 2] == 0, annexB[i + 3] == 1 {
                let header = i + 4
                if header < annexB.count { types.append(annexB[header] & 0x1F) }
                i = header + 1
            } else {
                i += 1
            }
        }
        return types
    }

    /// 用 CGWindowListCopyWindowInfo 统计属于该 pid 且 kCGWindowIsOnscreen 为真的窗口
    private static func onScreenWindowInfo(pid: pid_t) -> (count: Int, descriptions: [String]) {
        guard let list = CGWindowListCopyWindowInfo([.optionOnScreenOnly, .excludeDesktopElements], kCGNullWindowID)
                as? [[String: Any]] else {
            return (0, [])
        }
        var descriptions: [String] = []
        for window in list {
            guard let owner = window[kCGWindowOwnerPID as String] as? Int, owner == Int(pid) else { continue }
            let onscreen = (window[kCGWindowIsOnscreen as String] as? Bool) ?? false
            guard onscreen else { continue }
            let name = (window[kCGWindowName as String] as? String) ?? "(null)"
            let layer = (window[kCGWindowLayer as String] as? Int) ?? -1
            descriptions.append("name=\(name) layer=\(layer) pid=\(pid)")
        }
        return (descriptions.count, descriptions)
    }
}

/// 会话相关错误
enum MLGameStreamError: LocalizedError {
    case surfaceCreationFailed
    case compressionSessionFailed(OSStatus)

    var errorDescription: String? {
        switch self {
        case .surfaceCreationFailed:
            return "IOSurfaceCreate 失败（检查 kIOSurfaceIsGlobal 与尺寸/像素格式）"
        case .compressionSessionFailed(let status):
            return "VTCompressionSessionCreate 失败（\(status)）"
        }
    }
}

// 自测运行方式（构建并部署好 App 之后）：
//
//   QR_GAMESTREAM_SELFTEST=1 \
//   QR_GAMESTREAM_EXEC=/tmp/qr-probe/GodotPatched.app/Contents/MacOS/Godot \
//   QR_GAMESTREAM_PROJECT=/tmp/gdtest \
//   QR_GAMESTREAM_FRAMES=60 \
//   /Volumes/soft/backup/prototype/remote/QuickRecorder/build/QuickRecorder.app/Contents/MacOS/QuickRecorder
//
// 说明：用测试工程 /tmp/gdtest（纯 GDScript，迭代快，且打印 EV 输入事件便于验证输入通道）
// 还是用真实游戏（/Volumes/soft/backup/prototype/remote/植物大战僵尸杂交版）由 QR_GAMESTREAM_PROJECT 决定。
