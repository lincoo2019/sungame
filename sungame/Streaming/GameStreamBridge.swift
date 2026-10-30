//
//  GameStreamBridge.swift
//  QuickRecorder
//
//  把「无窗口运行的 Godot 游戏」产出的 H.264 接进 Moonlight 推流管线，并复用码流转换逻辑。
//
//  为什么需要这一层：
//    · MLGameStreamSession 输出的是 VideoToolbox 原生 AVCC（4 字节大端长度前缀，参数集在
//      CMFormatDescription 里），而 Moonlight 通道要的是 Annex-B（00 00 00 01 起始码，且每个 IDR
//      前必须内嵌 SPS/PPS）。转换逻辑与屏幕采集管线完全同构，因此抽成共享的 MLAnnexBConverter，
//      两边共用，避免两套实现各自演化出偏差。
//    · 转换后的帧喂给 MLVideoStream.sendFrame，结构上与现有 .ps5 直通模式一致（不建采集管线）。
//

import Foundation
import CoreGraphics
import CoreMedia
import ImageIO
import VideoToolbox

/// AVCC ↔ Annex-B 的共享转换工具。
///
/// 屏幕采集管线（MLCapturePipeline）和游戏串流管线（MLGameVideoBridge）都从这里取参数集、
/// 做长度前缀 → 起始码的替换，保证两条链路的组包方式一字不差。
enum MLAnnexBConverter {

    /// Annex-B 起始码
    static let startCode: [UInt8] = [0, 0, 0, 1]

    /// AVCC（4 字节大端长度前缀）→ Annex-B（每个 NAL 前置起始码）。
    ///
    /// 直接吃裸指针重载：屏幕采集管线拿到的就是 CMBlockBuffer 里的裸指针，避免先拷成 Array
    /// 再多一次分配（低延迟链路上每帧都走这里）。
    static func toAnnexB(_ bytes: UnsafePointer<UInt8>, count: Int) -> [UInt8] {
        var out = [UInt8]()
        var offset = 0
        while offset + 4 <= count {
            let naluLength = Int(UInt32(bytes[offset]) << 24 | UInt32(bytes[offset + 1]) << 16
                | UInt32(bytes[offset + 2]) << 8 | UInt32(bytes[offset + 3]))
            offset += 4
            guard naluLength > 0, offset + naluLength <= count else { break }
            out.append(contentsOf: startCode)
            out.append(contentsOf: UnsafeBufferPointer(start: bytes + offset, count: naluLength))
            offset += naluLength
        }
        return out
    }

    /// 便利重载：输入已经是 [UInt8]（游戏桥接用）
    static func toAnnexB(_ bytes: [UInt8]) -> [UInt8] {
        guard !bytes.isEmpty else { return [] }
        return bytes.withUnsafeBufferPointer { buffer in
            toAnnexB(buffer.baseAddress!, count: buffer.count)
        }
    }

    /// 从 CMFormatDescription 里取出 SPS(index 0) / PPS(index 1)。
    /// 参数集不满足（少于 2 个）时返回空数组，调用方据此跳过，不清空上一次缓存。
    static func parameterSets(from formatDescription: CMFormatDescription) -> (sps: [UInt8], pps: [UInt8]) {
        var count = 0
        CMVideoFormatDescriptionGetH264ParameterSetAtIndex(formatDescription, parameterSetIndex: 0,
                                                          parameterSetPointerOut: nil,
                                                          parameterSetSizeOut: nil,
                                                          parameterSetCountOut: &count,
                                                          nalUnitHeaderLengthOut: nil)
        guard count >= 2 else { return ([], []) }

        var sps: [UInt8] = []
        var pps: [UInt8] = []
        var pointer: UnsafePointer<UInt8>?
        var size = 0
        if CMVideoFormatDescriptionGetH264ParameterSetAtIndex(formatDescription, parameterSetIndex: 0,
                                                             parameterSetPointerOut: &pointer,
                                                             parameterSetSizeOut: &size,
                                                             parameterSetCountOut: nil,
                                                             nalUnitHeaderLengthOut: nil) == noErr,
           let pointer, size > 0 {
            sps = Array(UnsafeBufferPointer(start: pointer, count: size))
        }
        if CMVideoFormatDescriptionGetH264ParameterSetAtIndex(formatDescription, parameterSetIndex: 1,
                                                             parameterSetPointerOut: &pointer,
                                                             parameterSetSizeOut: &size,
                                                             parameterSetCountOut: nil,
                                                             nalUnitHeaderLengthOut: nil) == noErr,
           let pointer, size > 0 {
            pps = Array(UnsafeBufferPointer(start: pointer, count: size))
        }
        return (sps, pps)
    }

    /// 生成 IDR 前内嵌的参数集前缀：起始码 + SPS + 起始码 + PPS。
    /// 任一为空时返回 nil（游戏会话在首个关键帧前拿不到参数集，此时只能等下一个关键帧）。
    static func parameterSetPrefix(sps: [UInt8], pps: [UInt8]) -> [UInt8]? {
        guard !sps.isEmpty, !pps.isEmpty else { return nil }
        var out = startCode
        out.append(contentsOf: sps)
        out.append(contentsOf: startCode)
        out.append(contentsOf: pps)
        return out
    }
}

/// 游戏视频桥：把 MLGameStreamSession 的 AVCC 帧转成 Annex-B 后按 90kHz 时间戳送出。
///
/// 参数集来源说明：游戏会话不把 CMFormatDescription 交给外部，但它在每个关键帧上把 SPS/PPS
/// 提取到了自己的 sps/pps（private(set)，同模块可见）。桥接在收到 IDR 时读取这两个缓存，
/// 在码流前内嵌，语义与屏幕采集管线一致。
final class MLGameVideoBridge {

    /// 送出的视频帧：(Annex-B 数据, 是否 IDR, 90kHz 时间戳)
    var onFrame: (([UInt8], Bool, UInt32) -> Void)?

    private let session: MLGameStreamSession

    init(session: MLGameStreamSession) {
        self.session = session
        // 占用会话的编码回调；会话内部已保证在回调前完成参数集提取（IDR 时）
        session.onEncodedFrame = { [weak self] data, isKeyframe, pts in
            self?.handleEncoded(data, isKeyframe: isKeyframe, pts: pts)
        }
    }

    private func handleEncoded(_ avcc: Data, isKeyframe: Bool, pts: CMTime) {
        let annexB = MLAnnexBConverter.toAnnexB([UInt8](avcc))
        guard !annexB.isEmpty else { return }

        var output = annexB
        if isKeyframe,
           let prefix = MLAnnexBConverter.parameterSetPrefix(sps: session.sps, pps: session.pps) {
            // 每个 IDR 帧都要前置 SPS/PPS，否则客户端会一直等关键帧
            output = prefix + annexB
        }

        // VideoToolbox 给的是 CMTime 呈现时间戳；Moonlight RTP 用 90kHz
        let ts90k = UInt32(truncatingIfNeeded: Int64(CMTimeGetSeconds(pts) * 90000))
        onFrame?(output, isKeyframe, ts90k)
    }
}

// MARK: - 集成自测（QR_GAMESTREAM_BRIDGE_SELFTEST=1）
//
//  目的：用真实游戏验证「AVCC→Annex-B→(下一位)Moonlight 帧」与「Moonlight 输入包→24 字节包→游戏」
//  两条链路。输入这一段刻意走 MLInputInjector 的真实解析入口（与 MoonlightControl 一致：
//  去掉 8 字节 NV 头后把包体交给 handle(magic:body:)），而不是直接调 sendInput，
//  这样触摸协议解析本身也被覆盖到。
//
//  运行方式见文件末尾注释。

enum MLGameStreamIntegration {

    private final class Stats {
        let lock = NSLock()
        var frames = 0
        var keyframes = 0
        var totalBytes = 0
        var firstFrameIsKey = false
        var haveFirstFrame = false
        /// 前若干帧的 NAL 类型序列（验证起始码与 SPS/PPS 内嵌）
        var nalSequences: [[UInt8]] = []
        /// 前若干帧的字节数（与 nalSequences 一一对应）
        var nalSizes: [Int] = []
        var gameLines: [String] = []
        var evScreenTouch = 0
        var evScreenDrag = 0
        var evMouseMotion = 0
        var evMouseButton = 0
        /// 合成鼠标按下 / 抬起次数，以及「最后一条鼠标按键事件是否按下」（判断是否留下卡住的按键）
        var evMouseDown = 0
        var evMouseUp = 0
        var lastMouseButtonPressed: Bool?
        /// 原生触摸按下 / 抬起次数（合成鼠标验证「恰好一套」用）
        var evTouchDown = 0
        var evTouchUp = 0
        /// 引擎侧输入诊断行（QRInput: ...），触屏链路的关键证据
        var engineInputLines: [String] = []
        /// 按 index 统计游戏实际收到的 ScreenTouch 按下/抬起次数（判断状态是否错乱的关键）
        var touchDownByIndex: [Int: Int] = [:]
        var touchUpByIndex: [Int: Int] = [:]
        var dragByIndex: [Int: Int] = [:]

        func recordFrame(_ data: [UInt8], isIDR: Bool) {
            lock.lock(); defer { lock.unlock() }
            if !haveFirstFrame { haveFirstFrame = true; firstFrameIsKey = isIDR }
            frames += 1
            if isIDR { keyframes += 1 }
            totalBytes += data.count
            if nalSequences.count < 10 {
                nalSequences.append(Self.nalTypes(data))
                nalSizes.append(data.count)
            }
        }

        func recordOutput(_ line: String) {
            lock.lock(); defer { lock.unlock() }
            gameLines.append(line)
            if line.contains("QRInput:") { engineInputLines.append(line) }
            if line.contains("EV ") {
                if line.contains("InputEventScreenTouch") { evScreenTouch += 1 }
                if line.contains("InputEventScreenDrag") { evScreenDrag += 1 }
                if line.contains("InputEventMouseMotion") { evMouseMotion += 1 }
                if line.contains("InputEventMouseButton") { evMouseButton += 1 }
                if line.contains("InputEventScreenTouch") {
                    if line.contains("pressed=true") { evTouchDown += 1 }
                    if line.contains("pressed=false") { evTouchUp += 1 }
                }
                if line.contains("InputEventMouseButton") {
                    if line.contains("pressed=true") { evMouseDown += 1 }
                    if line.contains("pressed=false") { evMouseUp += 1 }
                    lastMouseButtonPressed = line.contains("pressed=true")
                }
                if let index = Self.parseInt(line, key: "index=") {
                    if line.contains("InputEventScreenTouch") {
                        if line.contains("pressed=true") { touchDownByIndex[index, default: 0] += 1 }
                        if line.contains("pressed=false") { touchUpByIndex[index, default: 0] += 1 }
                    } else if line.contains("InputEventScreenDrag") {
                        dragByIndex[index, default: 0] += 1
                    }
                }
            }
        }

        /// 从 "… index=3 …" 里取出整数（失败返回 nil）
        static func parseInt(_ line: String, key: String) -> Int? {
            guard let r = line.range(of: key) else { return nil }
            let tail = line[r.upperBound...]
            let digits = tail.prefix { $0.isNumber }
            return digits.isEmpty ? nil : Int(digits)
        }

        /// 从 Annex-B 码流里取出每个 NAL 的 nal_unit_type（低 5 位）
        static func nalTypes(_ annexB: [UInt8]) -> [UInt8] {
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
    }

    static func runSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_BRIDGE_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let frames = Int(env["QR_GAMESTREAM_FRAMES"] ?? "") ?? 30
        DispatchQueue.global(qos: .userInitiated).async {
            run(executablePath: executable, projectPath: project, targetFrames: frames)
        }
    }

    private static func run(executablePath: String, projectPath: String, targetFrames: Int) {
        func emit(_ line: String) {
            print("[QRGAMEBRIDGE] \(line)")
            fflush(stdout)
        }

        let gameW = 1920, gameH = 1080
        emit("=== 桥接自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")

        let session = MLGameStreamSession(width: gameW, height: gameH, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let stats = Stats()

        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in stats.recordOutput(line) }
        bridge.onFrame = { data, isIDR, _ in stats.recordFrame(data, isIDR: isIDR) }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // 1) 等够帧数（同时收集 NAL 类型序列）
        let deadline = Date().addingTimeInterval(45)
        while Date() < deadline {
            stats.lock.lock(); let count = stats.frames; stats.lock.unlock()
            if count >= targetFrames { break }
            usleep(200_000)
        }

        // 2) 接入真实输入解析路径：配置 .game 布局，把 onGameInput 转发到会话
        MLInputInjector.shared.configure(layout: .game(outputSize: CGSize(width: gameW, height: gameH),
                                                       contentRect: CGRect(x: 0, y: 0, width: gameW, height: gameH),
                                                       gameSize: CGSize(width: gameW, height: gameH)))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }

        emit("输入 socket 已连接 = \(session.isInputConnected)")
        usleep(500_000)

        // 3) 模拟 Moonlight 触摸客户端：SS_TOUCH_PACKET 包体（去掉 8 字节 NV 头）
        //    归一化坐标 0.25,0.5 → 期望游戏像素 (480,540)；0.5,0.5 → (960,540)
        MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                      body: touchBody(eventType: MLTouchEvent.down, pointerId: 7, x: 0.25, y: 0.5))
        usleep(150_000)
        MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                      body: touchBody(eventType: MLTouchEvent.move, pointerId: 7, x: 0.5, y: 0.5))
        usleep(150_000)
        MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                      body: touchBody(eventType: MLTouchEvent.up, pointerId: 7, x: 0.5, y: 0.5))
        usleep(200_000)

        // 4) 模拟桌面版 Moonlight 客户端：绝对鼠标移动 + 左键按下/抬起
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseMoveAbs,
                                      body: mouseAbsBody(x: 960, y: 540, width: 1920, height: 1080))
        usleep(150_000)
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonDown, body: [MLMouseButton.left.rawValue])
        usleep(150_000)
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonUp, body: [MLMouseButton.left.rawValue])
        usleep(1_200_000)   // 等游戏把事件打印到 stdout

        // 5) 汇总
        stats.lock.lock()
        let frames = stats.frames
        let keys = stats.keyframes
        let bytes = stats.totalBytes
        let firstKey = stats.firstFrameIsKey
        let sequences = stats.nalSequences
        let sizes = stats.nalSizes
        let evTouch = stats.evScreenTouch
        let evDrag = stats.evScreenDrag
        let evMotion = stats.evMouseMotion
        let evButton = stats.evMouseButton
        let evSamples = stats.gameLines.filter { $0.contains("EV ") }.prefix(8).map { $0 }
        stats.lock.unlock()

        emit("---- 码流格式（送进 MLVideoStream.sendFrame 的字节）----")
        emit("帧数 = \(frames)，关键帧数 = \(keys)，总字节 = \(bytes)，首帧是关键帧 = \(firstKey)")
        for (index, types) in sequences.enumerated() {
            let size = index < sizes.count ? sizes[index] : 0
            emit("frame#\(index + 1) NAL types = \(types) bytes = \(size)")
        }
        let firstSeq = sequences.first ?? []
        emit("首帧起始码检查 = \(firstSeq.isEmpty ? "无 NAL" : "以 00 00 00 01 开头（已确认）")")
        emit("首帧是否含 SPS(7)/PPS(8) = \(firstSeq.contains(7)) / \(firstSeq.contains(8))")

        emit("---- 触摸 / 鼠标链路（游戏 stdout）----")
        emit("InputEventScreenTouch 次数 = \(evTouch)")
        emit("InputEventScreenDrag 次数 = \(evDrag)")
        emit("InputEventMouseMotion 次数 = \(evMotion)")
        emit("InputEventMouseButton 次数 = \(evButton)")
        for line in evSamples { emit("  game: \(line)") }

        session.stop()
        emit("=== 桥接自测结束 ===")
        exit(0)
    }

    // MARK: - 触摸压力自测（QR_GAMESTREAM_TOUCH_SELFTEST=1）
    //
    //  目的：用真实引擎 + 真实宿主路径复现并验证触摸链路场景：
    //    1) 长拖动（≥1000 个 MOVE）不卡死、持续收到事件、引擎按下集合正确；
    //    1b) 长按 12 秒（DOWN + 每 100ms 一个 MOVE）看门狗不得误触发，downIndices 只含该 index；
    //    2) 丢一次 UP 后，看门狗在阈值后把状态清回空集合并补发抬起（记录触发原因与间隔毫秒）；
    //    3) CANCEL_ALL 清空所有 index；
    //    4) HOVER / HOVER_LEAVE 不得被当成 down/up（场景 1 的历史根因）。
    //  另外：本自测把「值得落盘」的引擎输出按宿主相同的过滤规则写入真实 streaming.log，
    //  便于用真实文件验证 QRInput: 诊断确实落盘。

    static func runTouchStressFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_TOUCH_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let moves = Int(env["QR_GAMESTREAM_MOVES"] ?? "") ?? 1200
        DispatchQueue.global(qos: .userInitiated).async {
            runTouchStress(executablePath: executable, projectPath: project, moveCount: moves)
        }
    }

    private static func sleepMs(_ ms: Int) {
        usleep(useconds_t(ms) * 1000)
    }

    private static func runTouchStress(executablePath: String, projectPath: String, moveCount: Int) {
        let t0 = Date()
        func emit(_ line: String) {
            let ts = String(format: "%.2f", Date().timeIntervalSince(t0))
            print("[QRTOUCH +\(ts)s] \(line)")
            fflush(stdout)
        }

        let gameW = 1920, gameH = 1080
        emit("=== 触摸压力自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("moveCount  = \(moveCount)")

        // 清空并复现宿主的落盘行为：把引擎输出按同一过滤规则写进真实 streaming.log
        prepareStreamingLogForSelfTest()

        let session = MLGameStreamSession(width: gameW, height: gameH, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let stats = Stats()

        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            stats.recordOutput(line)
            appendLogWorthyOutput(line)   // 与宿主 onOutput 相同的过滤 → 落到真实 streaming.log
        }
        bridge.onFrame = { data, isIDR, _ in stats.recordFrame(data, isIDR: isIDR) }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // 等「画面就绪 + 输入连接建立」
        let deadline = Date().addingTimeInterval(45)
        while Date() < deadline {
            stats.lock.lock(); let frames = stats.frames; stats.lock.unlock()
            if frames >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")

        // 走真实输入解析路径（与 MoonlightControl 一致）
        MLInputInjector.shared.configure(layout: .game(outputSize: CGSize(width: gameW, height: gameH),
                                                       contentRect: CGRect(x: 0, y: 0, width: gameW, height: gameH),
                                                       gameSize: CGSize(width: gameW, height: gameH)))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }
        // 触摸逐事件明细（宿主里由 MLInputInjector.onLog 推给串流日志，这里直接打印）
        MLInputInjector.shared.onLog = { message in emit(message) }

        // 模拟 Moonlight 客户端下发 SS_TOUCH_PACKET（包体 28 字节）
        func touch(_ type: UInt8, id: UInt32, nx: Float, ny: Float) {
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: type, pointerId: id, x: nx, y: ny))
        }

        // ---- 场景 1：长拖动（>= moveCount 个 MOVE），跨度约 6 秒，跨过一次引擎 5 秒诊断 ----
        emit("---- 场景1：长拖动（DOWN + \(moveCount) MOVE + UP，跨度约 6s）----")
        touch(MLTouchEvent.down, id: 0, nx: 0.10, ny: 0.50)
        let perMoveUs = useconds_t(max(1, 6_000_000 / max(moveCount, 1)))
        for i in 0..<moveCount {
            let t = Float(i) / Float(max(moveCount - 1, 1))
            touch(MLTouchEvent.move, id: 0, nx: 0.10 + 0.80 * t, ny: 0.50)
            usleep(perMoveUs)
        }
        touch(MLTouchEvent.up, id: 0, nx: 0.90, ny: 0.50)
        emit("场景1 结束（已发 DOWN + \(moveCount) MOVE + UP）")
        sleepMs(600)

        // ---- 场景1b：长按 12 秒（DOWN + 每 100ms 一个 MOVE 表示手指未抬起）----
        //  验证看门狗不会把「正常的长按/拖动」误判为卡住：downIndices 应始终只含 index=3，
        //  且日志里不应出现 watchdog fired。
        emit("---- 场景1b：长按 12s（DOWN + 每 100ms 一个 MOVE，验证看门狗不误触发）----")
        touch(MLTouchEvent.down, id: 3, nx: 0.40, ny: 0.60)
        for i in 0..<120 {   // 120 × 100ms = 12s（超过任务要求的 10s）
            touch(MLTouchEvent.move, id: 3, nx: 0.40 + Float(i) * 0.0001, ny: 0.60)
            sleepMs(100)
        }
        touch(MLTouchEvent.up, id: 3, nx: 0.41, ny: 0.60)
        emit("场景1b 结束（长按 12s，未见 watchdog fired 即正常）")
        sleepMs(600)

        // ---- 场景 2：丢一次 UP（有 DOWN + 大量 MOVE，但没有 UP），验证看门狗自愈 ----
        emit("---- 场景2：丢 UP（DOWN + 200 MOVE，不发 UP），等看门狗自愈 ----")
        touch(MLTouchEvent.down, id: 1, nx: 0.30, ny: 0.30)
        for i in 0..<200 {
            touch(MLTouchEvent.move, id: 1, nx: 0.30 + Float(i) * 0.001, ny: 0.30)
            sleepMs(5)
        }
        emit("场景2：MOVE 已停发，等待 10s 看门狗阈值 + 5s 诊断周期…")
        sleepMs(13_000)
        emit("场景2 结束")

        // ---- 场景 3：CANCEL_ALL ----
        emit("---- 场景3：CANCEL_ALL（先 DOWN index0/1，再 cancelAll）----")
        touch(MLTouchEvent.down, id: 0, nx: 0.20, ny: 0.20)
        touch(MLTouchEvent.down, id: 1, nx: 0.40, ny: 0.40)
        sleepMs(200)
        touch(MLTouchEvent.cancelAll, id: 0, nx: 0.0, ny: 0.0)
        sleepMs(6_000)
        emit("场景3 结束")

        // ---- 场景 4：HOVER / HOVER_LEAVE / 虚假 UP 不得破坏按下状态 ----
        //  注意：宿主已经会丢弃 HOVER 类事件；这里额外用底层 sendInput 直接构造
        //  eventType=HOVER 的 24 字节包，专门验证「引擎」侧的严格语义（防回归）。
        //  期望：index2 恰好 1 次按下 + 1 次抬起；index9 的虚假 UP 被完全忽略（0 次事件）。
        emit("---- 场景4：引擎侧 HOVER/虚假UP 语义（DOWN idx2 → HOVER → MOVE → 虚假UP idx9 → UP idx2）----")
        touch(MLTouchEvent.down, id: 2, nx: 0.50, ny: 0.50)
        sleepMs(100)
        // 直接经底层通道发 HOVER / HOVER_LEAVE（绕过宿主的过滤），模拟异常宿主
        _ = session.sendInput(kind: 3, pressed: false, code: 2, x: 960, y: 540,
                              touchEvent: MLTouchEvent.hover)
        _ = session.sendInput(kind: 3, pressed: false, code: 2, x: 960, y: 540,
                              touchEvent: MLTouchEvent.hoverLeave)
        _ = session.sendInput(kind: 3, pressed: true, code: 2, x: 1100, y: 540,
                              touchEvent: MLTouchEvent.move)
        sleepMs(100)
        // 对一个从未按下的 index 发 UP：引擎应忽略（不得回发虚假抬起）
        _ = session.sendInput(kind: 3, pressed: false, code: 9, x: 100, y: 100,
                              touchEvent: MLTouchEvent.up)
        touch(MLTouchEvent.up, id: 2, nx: 0.60, ny: 0.50)
        sleepMs(1_000)
        emit("场景4 结束")

        // ---- 汇总 ----
        stats.lock.lock()
        let frames = stats.frames
        let evTouch = stats.evScreenTouch
        let evDrag = stats.evScreenDrag
        let engineLines = stats.engineInputLines
        let downByIndex = stats.touchDownByIndex
        let upByIndex = stats.touchUpByIndex
        let dragByIndex = stats.dragByIndex
        let evSamples = stats.gameLines.filter { $0.contains("EV ") }
        stats.lock.unlock()

        emit("---- 汇总 ----")
        emit("编码帧数 = \(frames)")
        emit("游戏 stdout：InputEventScreenTouch 行数 = \(evTouch)，InputEventScreenDrag 行数 = \(evDrag)")
        emit("游戏 stdout：EV 行总数 = \(evSamples.count)")
        emit("---- 每个 index 的实际触摸事件计数（游戏侧收到的）----")
        for index in Set(downByIndex.keys).union(upByIndex.keys).union(dragByIndex.keys).sorted() {
            emit("  index=\(index): ScreenTouch按下=\(downByIndex[index] ?? 0) 抬起=\(upByIndex[index] ?? 0) ScreenDrag=\(dragByIndex[index] ?? 0)")
        }
        emit("  期望：idx0 按下2/抬起2，idx1 按下2/抬起2，idx2 按下1/抬起1，idx3 按下1/抬起1（长按12s）；idx9（虚假UP）不出现")
        emit("---- 游戏 stdout 事件行（前后各取样）----")
        for line in evSamples.prefix(6) { emit("  game: \(line)") }
        if evSamples.count > 12 { emit("  game: ...（省略 \(evSamples.count - 12) 行）...") }
        for line in evSamples.suffix(6) { emit("  game: \(line)") }
        emit("---- 引擎侧输入诊断（QRInput: … 原文）----")
        for line in engineLines { emit("  engine: \(line)") }

        session.stop()
        emit("=== 触摸压力自测结束 ===")
        exit(0)
    }

    /// 构造 SS_TOUCH_PACKET 包体（去掉 8 字节 NV 头），字段布局见 MoonlightInput.handleTouch 注释
    private static func touchBody(eventType: UInt8, pointerId: UInt32, x: Float, y: Float) -> [UInt8] {
        var body = [UInt8](repeating: 0, count: 28)
        body[0] = eventType
        body[1] = 0
        body[2] = 0xFF; body[3] = 0xFF          // rotation = LI_ROT_UNKNOWN
        body[4] = UInt8(pointerId & 0xFF)
        body[5] = UInt8((pointerId >> 8) & 0xFF)
        body[6] = UInt8((pointerId >> 16) & 0xFF)
        body[7] = UInt8((pointerId >> 24) & 0xFF)
        writeLEFloat(x, into: &body, at: 8)
        writeLEFloat(y, into: &body, at: 12)
        writeLEFloat(1.0, into: &body, at: 16)  // pressureOrDistance
        return body
    }

    /// 构造 NV_ABS_MOUSE_MOVE_PACKET 包体（去掉 8 字节 NV 头）：x/y/unused/width/height 均为 int16 大端
    private static func mouseAbsBody(x: Int16, y: Int16, width: Int16, height: Int16) -> [UInt8] {
        var body = [UInt8](repeating: 0, count: 10)
        func put(_ v: Int16, at offset: Int) {
            let raw = UInt16(bitPattern: v)
            body[offset] = UInt8(raw >> 8); body[offset + 1] = UInt8(raw & 0xFF)
        }
        put(x, at: 0); put(y, at: 2); put(0, at: 4); put(width, at: 6); put(height, at: 8)
        return body
    }

    private static func writeLEFloat(_ value: Float, into buffer: inout [UInt8], at offset: Int) {
        var bits = value.bitPattern.littleEndian
        withUnsafeBytes(of: &bits) { raw in
            for i in 0..<4 { buffer[offset + i] = raw[i] }
        }
    }

    // MARK: - 串流日志落盘（自测用）
    //
    //  真实宿主的 onOutput 走 MLStreamingHost.log() 写入 streaming.log。自测进程里没有宿主，
    //  这里用**同一个过滤函数** MLStreamingHost.isLogWorthyGameOutput 复现落盘，
    //  目标文件与生产一致（App 支持目录 /QuickRecorder/Streaming/streaming.log），
    //  以便用真实文件验证「引擎 QRInput: 诊断确实落进串流日志」。

    /// 自测开始前把 streaming.log 清空，保证 grep 的是本次运行的内容
    private static func prepareStreamingLogForSelfTest() {
        guard let url = streamingLogURL() else { return }
        try? FileManager.default.removeItem(at: url)
    }

    /// 把一行游戏输出按宿主过滤规则追加到 streaming.log
    private static func appendLogWorthyOutput(_ line: String) {
        guard MLStreamingHost.isLogWorthyGameOutput(line) else { return }
        guard let url = streamingLogURL(),
              let data = ("[自测触摸] 游戏输出：" + line + "\n").data(using: .utf8) else { return }
        if let handle = try? FileHandle(forWritingTo: url) {
            defer { try? handle.close() }
            _ = try? handle.seekToEnd()
            try? handle.write(contentsOf: data)
        } else {
            try? data.write(to: url)
        }
    }

    /// 生产宿主使用的 streaming.log 路径
    private static func streamingLogURL() -> URL? {
        guard let support = FileManager.default.urls(for: .applicationSupportDirectory,
                                                     in: .userDomainMask).first else { return nil }
        let dir = support.appendingPathComponent("sunGame/Streaming", isDirectory: true)
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        return dir.appendingPathComponent("streaming.log")
    }

    // MARK: - 坐标映射定量自测（QR_GAMESTREAM_COORD_SELFTEST=1）
    //
    //  目的：把「注入已知归一化坐标 → 游戏实际收到的像素坐标」做成可复现的定量验证。
    //  两种协商分辨率各跑一遍（由 QR_COORD_W/H 指定），鼠标与触摸两条路径都覆盖，
    //  判定标准：游戏实收像素 ≈ 归一化 × 该次分辨率（允许 ±2px 取整误差）。
    //  事件坐标由游戏侧无缓冲落盘到 /tmp/gdtest_events.log（gdtest 的 m.gd 逐行 flush）。
    //
    //  运行方式见文件末尾注释。

    static func runCoordSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_COORD_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let width = Int(env["QR_COORD_W"] ?? "") ?? 1280
        let height = Int(env["QR_COORD_H"] ?? "") ?? 720
        DispatchQueue.global(qos: .userInitiated).async {
            runCoordSelfTest(executablePath: executable, projectPath: project, width: width, height: height)
        }
    }

    private static func runCoordSelfTest(executablePath: String, projectPath: String,
                                         width: Int, height: Int) {
        func emit(_ line: String) {
            print("[QRCOORD] \(line)")
            fflush(stdout)
        }

        // 归一化测试点：四角 + 中心
        let points: [(Double, Double)] = [(0.1, 0.1), (0.9, 0.1), (0.5, 0.5), (0.1, 0.9), (0.9, 0.9)]
        let eventsFile = "/tmp/gdtest_events.log"
        try? FileManager.default.removeItem(atPath: eventsFile)

        emit("=== 坐标映射自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("协商分辨率 = \(width)x\(height)")

        let session = MLGameStreamSession(width: width, height: height, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        session.negotiatedSize = CGSize(width: width, height: height)
        session.mappingSize = CGSize(width: width, height: height)
        let bridge = MLGameVideoBridge(session: session)
        let stats = Stats()
        session.onLog = { emit("log: \($0)") }
        bridge.onFrame = { data, isIDR, _ in stats.recordFrame(data, isIDR: isIDR) }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        // 等「画面就绪 + 输入连接建立」
        let deadline = Date().addingTimeInterval(45)
        while Date() < deadline {
            stats.lock.lock(); let f = stats.frames; stats.lock.unlock()
            if f >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")

        // 配置 .game 布局：canvas = 协商分辨率（与修复后的宿主一致）
        let canvas = CGSize(width: width, height: height)
        MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                       contentRect: CGRect(origin: .zero, size: canvas),
                                                       gameSize: canvas))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }
        MLInputInjector.shared.onLog = { message in emit("input: \(message)") }

        usleep(500_000)

        // ---- 鼠标路径：NV_ABS_MOUSE_MOVE_PACKET（参照尺寸取协商分辨率）+ 左键按下/抬起 ----
        emit("---- 注入鼠标点击（mouseMoveAbs + 左键 down/up）----")
        for (nx, ny) in points {
            let x = Int16((nx * Double(width)).rounded())
            let y = Int16((ny * Double(height)).rounded())
            MLInputInjector.shared.handle(magic: MLInputMagic.mouseMoveAbs,
                                          body: mouseAbsBody(x: x, y: y, width: Int16(width), height: Int16(height)))
            usleep(80_000)
            MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonDown, body: [MLMouseButton.left.rawValue])
            usleep(60_000)
            MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonUp, body: [MLMouseButton.left.rawValue])
            usleep(120_000)
        }

        // ---- 触摸路径：SS_TOUCH_PACKET（归一化坐标直接是 0..1）+ DOWN/UP ----
        // 注意 pointerId 必须落在引擎的触点上限（kMaxTouchPoints=64）之内，否则引擎会因 index 越界丢弃。
        emit("---- 注入触摸点击（touch down/up）----")
        for (i, point) in points.enumerated() {
            let (nx, ny) = point
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: MLTouchEvent.down, pointerId: UInt32(i),
                                                          x: Float(nx), y: Float(ny)))
            usleep(60_000)
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: MLTouchEvent.up, pointerId: UInt32(i),
                                                          x: Float(nx), y: Float(ny)))
            usleep(120_000)
        }

        usleep(1_500_000)   // 等游戏把事件 flush 到文件

        // ---- 读取游戏实际收到的事件坐标（只取「按下」事件，一一对应上面的注入顺序）----
        let mouseReceived = parseGameEvents(path: eventsFile, filter: "InputEventMouseButton")
        let touchReceived = parseGameEvents(path: eventsFile, filter: "InputEventScreenTouch")

        emit("---- 坐标验证表（期望 = 归一化 × \(width)x\(height)；允许 ±2px）----")
        emit("路径  归一化        期望像素        游戏实收      误差(dx,dy)   判定")
        var allPass = true
        for (i, point) in points.enumerated() {
            let (nx, ny) = point
            let ex = (nx * Double(width)).rounded()
            let ey = (ny * Double(height)).rounded()
            let mouseOK = emitCoordRow(emit: emit, path: "鼠标", nx: nx, ny: ny, ex: ex, ey: ey,
                                       received: i < mouseReceived.count ? mouseReceived[i] : nil)
            let touchOK = emitCoordRow(emit: emit, path: "触摸", nx: nx, ny: ny, ex: ex, ey: ey,
                                       received: i < touchReceived.count ? touchReceived[i] : nil)
            allPass = allPass && mouseOK && touchOK
        }
        emit("总体判定 = \(allPass ? "PASS" : "FAIL")（鼠标实收 \(mouseReceived.count) 个 / 触摸实收 \(touchReceived.count) 个）")

        session.stop()
        emit("=== 坐标映射自测结束 ===")
        exit(0)
    }

    /// 打印一行坐标验证结果，返回该行是否通过
    private static func emitCoordRow(emit: (String) -> Void, path: String,
                                     nx: Double, ny: Double, ex: Double, ey: Double,
                                     received: (x: Int, y: Int)?) -> Bool {
        guard let received else {
            emit(String(format: "%@  (%.2f,%.2f)  (%.0f,%.0f)     (缺)          -            FAIL",
                        path, nx, ny, ex, ey))
            return false
        }
        let dx = received.x - Int(ex)
        let dy = received.y - Int(ey)
        let ok = abs(dx) <= 2 && abs(dy) <= 2
        emit(String(format: "%@  (%.2f,%.2f)  (%.0f,%.0f)     (%d,%d)     (%+d,%+d)   %@",
                    path, nx, ny, ex, ey, received.x, received.y, dx, dy, ok ? "PASS" : "FAIL"))
        return ok
    }

    /// 从游戏落盘的事件文件里抽取指定类「按下」事件的坐标（按出现顺序）。
    /// 行格式形如 `EV InputEventMouseButton pos=(123, 45) pressed=true`。
    private static func parseGameEvents(path: String, filter: String) -> [(x: Int, y: Int)] {
        guard let text = try? String(contentsOfFile: path, encoding: .utf8) else { return [] }
        var out: [(x: Int, y: Int)] = []
        for line in text.split(separator: "\n") {
            let s = String(line)
            guard s.contains("EV "), s.contains(filter), s.contains("pressed=true") else { continue }
            guard let range = s.range(of: "pos=(") else { continue }
            let coords = s[range.upperBound...].prefix { $0 != ")" }.split(separator: ",")
            guard coords.count >= 2,
                  let x = Double(coords[0].trimmingCharacters(in: .whitespaces)),
                  let y = Double(coords[1].trimmingCharacters(in: .whitespaces)) else { continue }
            out.append((Int(x.rounded()), Int(y.rounded())))
        }
        return out
    }

    // MARK: - 虚拟光标合成自测（QR_GAMESTREAM_CURSOR_SELFTEST=1）
    //
    //  目的：用真实引擎复现任务 A 的两条核心断言：
    //    1) 注入一组「相对移动」，断言光标真的出现在「中心 + 增量累加」的期望坐标上，
    //       且画面其他位置没有同类（纯白）像素；
    //    2) 位置没变时第二次合成被跳过（脏区快照未变），并给出脏区尺寸与编码帧率。
    //
    //  依赖：gdtest 的 _draw 只画黑底 + 红矩形（没有白色），因此「白像素」就是光标本身。

    static func runCursorSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_CURSOR_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let width = Int(env["QR_CURSOR_W"] ?? "") ?? 1920
        let height = Int(env["QR_CURSOR_H"] ?? "") ?? 1080
        let touchOnly = env["QR_CURSOR_TOUCH_ONLY"] == "1"
        let dump = env["QR_CURSOR_DUMP"] ?? "/tmp/qr-cursor.png"
        DispatchQueue.global(qos: .userInitiated).async {
            runCursorSelfTest(executablePath: executable, projectPath: project,
                              width: width, height: height, touchOnly: touchOnly, dumpPath: dump)
        }
    }

    /// 编码帧计数（VideoToolbox 回调线程写入，验证线程读取）
    private final class FrameCounter {
        private let lock = NSLock()
        private var n = 0
        func inc() { lock.lock(); n += 1; lock.unlock() }
        var value: Int { lock.lock(); defer { lock.unlock() }; return n }
    }

    /// 构造 NV_REL_MOUSE_MOVE_PACKET 的包体（去掉 8 字节 NV 头）：deltaX/deltaY 均为 int16 大端
    private static func mouseRelBody(dx: Int16, dy: Int16) -> [UInt8] {
        var body = [UInt8](repeating: 0, count: 4)
        let ux = UInt16(bitPattern: dx), uy = UInt16(bitPattern: dy)
        body[0] = UInt8(ux >> 8); body[1] = UInt8(ux & 0xFF)
        body[2] = UInt8(uy >> 8); body[3] = UInt8(uy & 0xFF)
        return body
    }

    private static func runCursorSelfTest(executablePath: String, projectPath: String,
                                          width: Int, height: Int, touchOnly: Bool, dumpPath: String) {
        func emit(_ line: String) {
            print("[QRCURSOR] \(line)")
            fflush(stdout)
        }

        let canvas = CGSize(width: width, height: height)
        let cursorEnabled = ProcessInfo.processInfo.environment["QR_GAMESTREAM_CURSOR"] != "0"
        emit("=== 光标合成自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("分辨率     = \(width)x\(height)  触摸专用=\(touchOnly)  光标合成开关=\(cursorEnabled)")

        let session = MLGameStreamSession(width: width, height: height, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        session.negotiatedSize = canvas
        session.mappingSize = canvas
        let bridge = MLGameVideoBridge(session: session)
        let counter = FrameCounter()
        bridge.onFrame = { _, _, _ in counter.inc() }
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            if line.contains("QRInput:") || line.contains("QRSize:") { emit("engine: \(line)") }
        }
        MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                       contentRect: CGRect(origin: .zero, size: canvas),
                                                       gameSize: canvas))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }
        MLInputInjector.shared.onLog = { message in emit("input: \(message)") }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        let ready = Date().addingTimeInterval(45)
        while Date() < ready {
            if counter.value >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")

        // ---- 帧率测量（稳态 4s，无输入）----
        let f0 = counter.value
        let t0 = Date()
        while Date().timeIntervalSince(t0) < 4 { usleep(100_000) }
        let f1 = counter.value
        let dt = Date().timeIntervalSince(t0)
        emit(String(format: "编码帧率（稳态无输入）：%d 帧 / %.1fs = %.1f fps", f1 - f0, dt, Double(f1 - f0) / dt))

        if touchOnly {
            emit("---- 纯触摸会话：不得显示光标 ----")
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: MLTouchEvent.down, pointerId: 1, x: 0.5, y: 0.5))
            usleep(150_000)
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: MLTouchEvent.move, pointerId: 1, x: 0.6, y: 0.5))
            usleep(150_000)
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: MLTouchEvent.up, pointerId: 1, x: 0.6, y: 0.5))
            usleep(400_000)
            emit("触摸会话统计：\(session.cursorStatsSummary())")
            let probe = session.cursorCompositeProbe()
            emit("触摸会话主动探测：firstBlit=\(probe.firstBlit)（应为 false：无鼠标输入，不显示光标）")
            session.stop()
            emit("=== 光标合成自测结束 ===")
            exit(0)
        }

        // ---- 相对移动序列（起点为归一化中心 (0.5,0.5)）----
        let seq: [(Int16, Int16)] = [(200, -100), (100, 50), (-40, 20)]
        let sumX = seq.reduce(0) { $0 + Int($1.0) }
        let sumY = seq.reduce(0) { $0 + Int($1.1) }
        emit("---- 注入相对移动序列 \(seq)（合计 dx=\(sumX) dy=\(sumY)）----")
        for (dx, dy) in seq {
            MLInputInjector.shared.handle(magic: MLInputMagic.mouseMoveRel, body: mouseRelBody(dx: dx, dy: dy))
            usleep(60_000)
        }
        let expected = CGPoint(x: canvas.width / 2 + CGFloat(sumX), y: canvas.height / 2 + CGFloat(sumY))
        emit(String(format: "期望光标位置 = 中心 + (%+d,%+d) = (%.0f,%.0f)", sumX, sumY, expected.x, expected.y))
        usleep(500_000)
        emit("鼠标会话统计：\(session.cursorStatsSummary())")

        // ---- 定向探测：同一块（未被游戏重绘的）surface 连续合成两次 ----
        let probe = session.cursorCompositeProbe()
        emit("定向探测(同位置连续两次合成)：firstBlit=\(probe.firstBlit) secondBlit=\(probe.secondBlit) " +
             "第二次是否因未变被跳过=\(probe.secondSkipped) 脏区=\(probe.dirtyW)x\(probe.dirtyH)px " +
             "未变跳过累计=\(probe.skipUnchanged) 合成累计=\(probe.blit)")
        let dirtyFraction = Double(probe.dirtyW * probe.dirtyH) / Double(width * height)
        emit(String(format: "脏区占整帧 = %.4f%%（只写这一小块，不做整帧遍历）", dirtyFraction * 100))

        // ---- 导出 PNG 并断言光标位置 ----
        try? FileManager.default.removeItem(atPath: dumpPath)
        session.scheduleFrameDump(afterFramesFromNow: 10, to: dumpPath)
        let dumpDeadline = Date().addingTimeInterval(6)
        while Date() < dumpDeadline, !FileManager.default.fileExists(atPath: dumpPath) { usleep(100_000) }
        guard let png = loadPNG(dumpPath) else {
            emit("PNG 读取失败：\(dumpPath)")
            session.stop(); exit(1)
        }
        emit("PNG = \(dumpPath) 尺寸 = \(png.w)x\(png.h)")
        let a = analyzeWhiteCluster(png.px, w: png.w, h: png.h, expected: expected)
        emit(String(format: "白色(光标)像素数 = %d，包围盒 = [%d,%d]-[%d,%d]，距期望>100px 的白色像素 = %d",
                    a.count, a.minX, a.minY, a.maxX, a.maxY, a.outside))
        let nearTip = abs(Double(a.minX) - Double(expected.x)) <= 8 && abs(Double(a.minY) - Double(expected.y)) <= 8
        let unique = a.outside == 0
        // 第二次连续合成必须被跳过（位置没变 + 脏区未被重绘）。
        let skipOK = !probe.secondBlit && probe.secondSkipped == 1
        emit("判定：光标出现在期望坐标 = \(nearTip ? "PASS" : "FAIL")；" +
             "画面其他位置无同类像素 = \(unique ? "PASS" : "FAIL")；" +
             "位置未变时第二次不重绘 = \(skipOK ? "PASS" : "FAIL")")

        session.stop()
        emit("=== 光标合成自测结束 ===")
        exit(0)
    }

    // MARK: - 分辨率跟随自测（QR_GAMESTREAM_SIZE_SELFTEST=1）
    //
    //  目的（任务 C）：用真实游戏在指定协商分辨率下跑一遍，导出 IOSurface 一帧 PNG，
    //  用「PNG 尺寸 + 四边黑边宽度 + 四角/中心采样」判断游戏内部是否真的按该尺寸渲染、
    //  有没有被拉伸或留黑边。引擎侧会打印 QRSize: 行说明窗口尺寸与游戏自身的 viewport/stretch 设置。

    static func runSizeSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_SIZE_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let width = Int(env["QR_SIZE_W"] ?? "") ?? 1280
        let height = Int(env["QR_SIZE_H"] ?? "") ?? 720
        let dump = env["QR_SIZE_DUMP"] ?? "/tmp/qr-size-\(width)x\(height).png"
        DispatchQueue.global(qos: .userInitiated).async {
            runSizeSelfTest(executablePath: executable, projectPath: project,
                            width: width, height: height, dumpPath: dump)
        }
    }

    private static func runSizeSelfTest(executablePath: String, projectPath: String,
                                        width: Int, height: Int, dumpPath: String) {
        func emit(_ line: String) {
            print("[QRSIZE] \(line)")
            fflush(stdout)
        }

        emit("=== 分辨率跟随自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("协商分辨率 = \(width)x\(height)")

        let session = MLGameStreamSession(width: width, height: height, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        session.negotiatedSize = CGSize(width: width, height: height)
        session.mappingSize = CGSize(width: width, height: height)
        let bridge = MLGameVideoBridge(session: session)
        let counter = FrameCounter()
        bridge.onFrame = { _, _, _ in counter.inc() }
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            if line.contains("QRSize:") || line.contains("QRInput:") { emit("engine: \(line)") }
        }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }

        let ready = Date().addingTimeInterval(60)
        while Date() < ready {
            if counter.value >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("已编码帧数 = \(counter.value)")

        try? FileManager.default.removeItem(atPath: dumpPath)
        // 等游戏真正进入画面（真实游戏启动较慢，30 帧可能还在加载画面）再导出。
        session.scheduleFrameDump(afterFramesFromNow: 300, to: dumpPath)
        let dumpDeadline = Date().addingTimeInterval(25)
        while Date() < dumpDeadline, !FileManager.default.fileExists(atPath: dumpPath) { usleep(100_000) }
        guard let png = loadPNG(dumpPath) else {
            emit("PNG 读取失败：\(dumpPath)")
            session.stop(); exit(1)
        }
        emit("PNG = \(dumpPath) 尺寸 = \(png.w)x\(png.h)")
        let bars = analyzeBars(png.px, w: png.w, h: png.h)
        emit(String(format: "黑边宽度：上=%d 下=%d 左=%d 右=%d（像素）；非黑像素占比 = %.2f%%",
                    bars.top, bars.bottom, bars.left, bars.right, bars.nonBlack * 100))
        emit(String(format: "宽高比：PNG = %.4f，协商 = %.4f",
                    Double(png.w) / Double(png.h), Double(width) / Double(height)))

        session.stop()
        emit("=== 分辨率跟随自测结束 ===")
        exit(0)
    }

    // MARK: - 触摸→鼠标合成自测（QR_GAMESTREAM_TOUCHMOUSE_SELFTEST=1）
    //
    //  目的：用真实引擎 + 真实宿主输入路径，断言「触摸输入只产生一套鼠标事件」：
    //    · 游戏收到原生 InputEventScreenTouch / ScreenDrag；
    //    · 同时收到恰好一套**由 Godot 内建合成**的 InputEventMouseMotion / MouseButton（不重复）；
    //    · 残留触点被强制释放时，合成鼠标的按下态也被一并释放（最后一帧鼠标不是按下态）。
    //  本轮改为「引擎强制 emulate_mouse_from_touch=true、不再自己合成」，因此把游戏工程的该项
    //  显式设为 false 再跑一遍，结果应仍旧「恰好一套」（证明我们强制覆盖、不依赖游戏工程设置）。

    static func runTouchMouseSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_TOUCHMOUSE_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        DispatchQueue.global(qos: .userInitiated).async {
            runTouchMouseSelfTest(executablePath: executable, projectPath: project)
        }
    }

    private static func runTouchMouseSelfTest(executablePath: String, projectPath: String) {
        func emit(_ line: String) {
            print("[QRTOUCHMOUSE] \(line)")
            fflush(stdout)
        }
        let canvas = CGSize(width: 1920, height: 1080)
        emit("=== 触摸→鼠标合成自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")

        let session = MLGameStreamSession(width: 1920, height: 1080, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let stats = Stats()
        // 打码流与否无关紧要，占用回调即可
        bridge.onFrame = { _, _, _ in }
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            stats.recordOutput(line)
            if line.contains("QRInput:") { emit("engine: \(line)") }
        }
        MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                       contentRect: CGRect(origin: .zero, size: canvas),
                                                       gameSize: canvas))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }
        func touch(_ type: UInt8, id: UInt32, nx: Float, ny: Float) {
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: type, pointerId: id, x: nx, y: ny))
        }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }
        let ready = Date().addingTimeInterval(45)
        while Date() < ready {
            stats.lock.lock(); let f = stats.evScreenDrag; stats.lock.unlock()
            if session.isInputConnected && f >= 0 { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")
        sleepMs(300)

        // ---- 阶段1：常规触摸 DOWN/MOVE/UP ----
        emit("---- 阶段1：触摸 DOWN/MOVE/UP（期望：原生触摸一套 + 合成鼠标一套）----")
        touch(MLTouchEvent.down, id: 7, nx: 0.25, ny: 0.50)
        sleepMs(200)
        touch(MLTouchEvent.move, id: 7, nx: 0.50, ny: 0.50)
        sleepMs(200)
        touch(MLTouchEvent.up, id: 7, nx: 0.50, ny: 0.50)
        sleepMs(500)

        // ---- 阶段2：残留触点被强制释放（DOWN id=8 → 空闲 >500ms → 新 DOWN id=9 触发释放）----
        emit("---- 阶段2：残留触点强制释放（期望：被释放的 id=8 合成鼠标也抬起）----")
        touch(MLTouchEvent.down, id: 8, nx: 0.30, ny: 0.40)
        sleepMs(900)                       // 空闲超过 kStaleReleaseIdleMs=500ms
        touch(MLTouchEvent.down, id: 9, nx: 0.70, ny: 0.60)   // 触发 stale release(8) + 新主触点 9
        sleepMs(200)
        touch(MLTouchEvent.up, id: 9, nx: 0.70, ny: 0.60)
        sleepMs(600)

        // 等引擎 5 秒诊断落一行（含 synthMouse 计数）
        sleepMs(5_500)

        stats.lock.lock()
        let touchDown = stats.evTouchDown
        let touchUp = stats.evTouchUp
        let drag = stats.evScreenDrag
        let mouseDown = stats.evMouseDown
        let mouseUp = stats.evMouseUp
        let mouseMotion = stats.evMouseMotion
        let lastPressed = stats.lastMouseButtonPressed
        let engineLines = stats.engineInputLines
        let samples = stats.gameLines.filter { $0.contains("EV ") }
        stats.lock.unlock()

        emit("---- 游戏实际收到的事件计数 ----")
        emit("原生触摸：ScreenTouch 按下=\(touchDown) 抬起=\(touchUp)，ScreenDrag=\(drag)")
        emit("合成鼠标：MouseButton 按下=\(mouseDown) 抬起=\(mouseUp)，MouseMotion=\(mouseMotion)")
        emit("最后一条鼠标按键是否按下 = \(lastPressed.map { $0 ? "是(pressed)" : "否(released)" } ?? "无")")
        emit("---- 游戏事件行（样本）----")
        for line in samples.prefix(14) { emit("  game: \(line)") }
        if let lastEngine = engineLines.last { emit("  engine(最近一行诊断): \(lastEngine)") }

        // ---- 判定 ----
        let exactlyOneSet = (mouseDown == touchDown) && (mouseUp == touchUp) && touchDown == 3 && touchUp == 3
        let notStuck = (lastPressed == false) && (mouseDown == mouseUp)
        emit("判定：恰好一套事件（mouse下上 == touch下上，且各为 3，无重复）= \(exactlyOneSet ? "PASS" : "FAIL")；" +
             "残留释放后无卡住的鼠标键 = \(notStuck ? "PASS" : "FAIL")")

        session.stop()
        emit("=== 触摸→鼠标合成自测结束 ===")
        exit(0)
    }

    // MARK: - 原生触摸拖动自测（QR_GAMESTREAM_DRAG_SELFTEST=1）
    //
    //  目的（任务1）：验证「去掉引擎自合成鼠标 + 强制 emulate_mouse_from_touch=true」之后：
    //    · 「必须靠拖动才能滚动」的控件确实被拖动滚动（由原生 InputEventScreenDrag 驱动）；
    //    · 原生触摸事件照常送达（ScreenTouch / ScreenDrag 计数正确）；
    //    · Godot 自己合成的鼠标事件仍能被只读鼠标的游戏（_input 里判 InputEventMouseButton）收到。
    //  把游戏工程的 emulate_mouse_from_touch 显式设为 false 再跑一遍，结果应不变（证明我们强制覆盖）。
    //
    //  对应 gdtest：m.tscn 里挂了一个全屏 Control（drag.gd），只在收到 ScreenDrag 时改变滚动值，
    //  单击（只有 ScreenTouch 没有 Drag）不会滚动；滚动值逐行无缓冲写入 /tmp/gdtest_drag.log。

    static func runDragSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_DRAG_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        DispatchQueue.global(qos: .userInitiated).async {
            runDragSelfTest(executablePath: executable, projectPath: project)
        }
    }

    private static func runDragSelfTest(executablePath: String, projectPath: String) {
        func emit(_ line: String) {
            print("[QRDRAG] \(line)")
            fflush(stdout)
        }
        let canvas = CGSize(width: 1920, height: 1080)
        let dragLog = "/tmp/gdtest_drag.log"
        let eventLog = "/tmp/gdtest_events.log"
        try? FileManager.default.removeItem(atPath: dragLog)
        try? FileManager.default.removeItem(atPath: eventLog)

        emit("=== 原生触摸拖动自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")

        let session = MLGameStreamSession(width: 1920, height: 1080, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let counter = FrameCounter()
        bridge.onFrame = { _, _, _ in counter.inc() }
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            if line.contains("QRInput:") || line.contains("QRSize:") { emit("engine: \(line)") }
        }
        MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                       contentRect: CGRect(origin: .zero, size: canvas),
                                                       gameSize: canvas))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }
        MLInputInjector.shared.onLog = { message in emit("input: \(message)") }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }
        let ready = Date().addingTimeInterval(45)
        while Date() < ready {
            if counter.value >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")
        sleepMs(300)

        func touch(_ type: UInt8, id: UInt32, nx: Float, ny: Float) {
            MLInputInjector.shared.handle(magic: MLInputMagic.touch,
                                          body: touchBody(eventType: type, pointerId: id, x: nx, y: ny))
        }

        // ---- 阶段1：触摸拖动（向上拖，共 40 个 MOVE）----
        emit("---- 阶段1：触摸 DOWN + 40 MOVE（向上拖动） + UP ----")
        touch(MLTouchEvent.down, id: 3, nx: 0.729, ny: 0.85)
        sleepMs(60)
        let steps = 40
        let yStart: Float = 0.85
        let yEnd: Float = 0.25
        for i in 1...steps {
            let t = Float(i) / Float(steps)
            touch(MLTouchEvent.move, id: 3, nx: 0.729, ny: yStart + (yEnd - yStart) * t)
            usleep(20_000)
        }
        touch(MLTouchEvent.up, id: 3, nx: 0.729, ny: yEnd)
        sleepMs(700)

        // ---- 阶段2：只读鼠标的桌面游戏路径（注入鼠标移动 + 左键按下/抬起）----
        emit("---- 阶段2：注入鼠标（mouseMoveAbs + 左键 down/up），验证 Godot 合成鼠标仍可达 ----")
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseMoveAbs,
                                      body: mouseAbsBody(x: 500, y: 400, width: 1920, height: 1080))
        sleepMs(150)
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonDown, body: [MLMouseButton.left.rawValue])
        sleepMs(120)
        MLInputInjector.shared.handle(magic: MLInputMagic.mouseButtonUp, body: [MLMouseButton.left.rawValue])
        sleepMs(1_000)

        // ---- 读取 gdtest 落盘 ----
        let dragLines = readLines(dragLog)
        let eventLines = readLines(eventLog)
        let dragCount = dragLines.filter { $0.hasPrefix("DRAG ") }.count
        let touchDown = dragLines.filter { $0.hasPrefix("TOUCH DOWN") }.count
        let touchUp = dragLines.filter { $0.hasPrefix("TOUCH UP") }.count
        var finalScroll = 0.0
        if let last = dragLines.last(where: { $0.contains("scroll=") }),
           let r = last.range(of: "scroll=") {
            let tail = last[r.upperBound...]
            finalScroll = Double(tail.prefix { $0.isNumber || $0 == "." || $0 == "-" }) ?? 0
        }
        let nativeTouch = eventLines.filter { $0.contains("InputEventScreenTouch") }.count
        let nativeDrag = eventLines.filter { $0.contains("InputEventScreenDrag") }.count
        let mouseDown = eventLines.filter { $0.contains("InputEventMouseButton") && $0.contains("pressed=true") }.count
        let mouseUp = eventLines.filter { $0.contains("InputEventMouseButton") && $0.contains("pressed=false") }.count

        emit("---- gdtest 落盘证据（/tmp/gdtest_drag.log 摘录）----")
        for line in dragLines.prefix(3) { emit("  drag: \(line)") }
        if dragLines.count > 5 { emit("  drag: ...（省略 \(dragLines.count - 5) 行）...") }
        for line in dragLines.suffix(2) { emit("  drag: \(line)") }
        emit("---- 断言 ----")
        emit("滚动值：最终 scroll=\(String(format: "%.1f", finalScroll))（初始 0，拖动后应显著变化）")
        emit("原生触摸（drag.gd）：ScreenDrag=\(dragCount)，ScreenTouch 按下/抬起=\(touchDown)/\(touchUp)")
        emit("游戏 stdout：InputEventScreenDrag=\(nativeDrag)，InputEventScreenTouch=\(nativeTouch)")
        emit("只读鼠标路径：InputEventMouseButton 按下/抬起=\(mouseDown)/\(mouseUp)")

        let scrollOK = abs(finalScroll) > 50
        let dragOK = dragCount >= steps / 2
        let touchOK = touchDown == 1 && touchUp == 1 && nativeTouch >= 2
        let mouseOK = mouseDown >= 1 && mouseUp >= 1
        emit("判定：拖动导致滚动=\(scrollOK ? "PASS" : "FAIL")；原生触摸拖动送达=\(dragOK ? "PASS" : "FAIL")；" +
             "原生触摸计数正确=\(touchOK ? "PASS" : "FAIL")；合成鼠标仍可达=\(mouseOK ? "PASS" : "FAIL")")

        session.stop()
        emit("=== 原生触摸拖动自测结束 ===")
        exit(0)
    }

    // MARK: - 双缓冲光标自测（QR_GAMESTREAM_CURSORDBL_SELFTEST=1）
    //
    //  目的（任务2）：用高帧率（QR_GAMESTREAM_MAX_FPS）复现「宿主把光标合成进游戏正在渲染的
    //  同一块 IOSurface → 导出帧里 0 个光标像素」，再证明改造成双缓冲后不再复现。
    //  同一份宿主代码用 QR_GAMESTREAM_FORCE_SINGLE=1 跑一遍即「改造前」对照（单缓冲、合成进同一块）。

    static func runCursorDualSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_CURSORDBL_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let width = Int(env["QR_CURSOR_W"] ?? "") ?? 1920
        let height = Int(env["QR_CURSOR_H"] ?? "") ?? 1080
        let count = Int(env["QR_CURSORDBL_FRAMES"] ?? "") ?? 40
        let dir = env["QR_CURSORDBL_DIR"] ?? "/tmp/qr-cursordbl"
        DispatchQueue.global(qos: .userInitiated).async {
            runCursorDualSelfTest(executablePath: executable, projectPath: project,
                                  width: width, height: height, count: count, directory: dir)
        }
    }

    private static func runCursorDualSelfTest(executablePath: String, projectPath: String,
                                              width: Int, height: Int, count: Int, directory: String) {
        func emit(_ line: String) {
            print("[QRCURSORDBL] \(line)")
            fflush(stdout)
        }
        let canvas = CGSize(width: width, height: height)
        let forceSingle = ProcessInfo.processInfo.environment["QR_GAMESTREAM_FORCE_SINGLE"] == "1"
        emit("=== 双缓冲光标自测开始 ===")
        emit("executable = \(executablePath)")
        emit("project    = \(projectPath)")
        emit("模式       = \(forceSingle ? "单缓冲（改造前对照）" : "双缓冲（改造后）")，导出帧数=\(count)")

        let session = MLGameStreamSession(width: width, height: height, fps: 30, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        session.negotiatedSize = canvas
        session.mappingSize = canvas
        let bridge = MLGameVideoBridge(session: session)
        let counter = FrameCounter()
        bridge.onFrame = { _, _, _ in counter.inc() }
        session.onLog = { emit("log: \($0)") }
        session.onOutput = { line in
            if line.contains("QRInput:") || line.contains("QRSize:") { emit("engine: \(line)") }
        }
        MLInputInjector.shared.configure(layout: .game(outputSize: canvas,
                                                       contentRect: CGRect(origin: .zero, size: canvas),
                                                       gameSize: canvas))
        MLInputInjector.shared.onGameInput = { kind, pressed, code, x, y, dx, dy, touchEvent in
            session.sendInput(kind: kind, pressed: pressed, code: code, x: x, y: y,
                              dx: dx, dy: dy, touchEvent: touchEvent)
        }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }
        let ready = Date().addingTimeInterval(45)
        while Date() < ready {
            if counter.value >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")

        // ---- 帧率测量（稳态 3s，无输入）----
        let f0 = counter.value
        let t0 = Date()
        while Date().timeIntervalSince(t0) < 3 { usleep(100_000) }
        let f1 = counter.value
        let dt = Date().timeIntervalSince(t0)
        emit(String(format: "编码帧率（稳态无输入）：%d 帧 / %.1fs = %.1f fps", f1 - f0, dt, Double(f1 - f0) / dt))

        // ---- 注入相对移动到确定位置 ----
        let seq: [(Int16, Int16)] = [(200, -100), (100, 50), (-40, 20)]
        for (dx, dy) in seq {
            MLInputInjector.shared.handle(magic: MLInputMagic.mouseMoveRel, body: mouseRelBody(dx: dx, dy: dy))
            sleepMs(60)
        }
        let sumX = seq.reduce(0) { $0 + Int($1.0) }
        let sumY = seq.reduce(0) { $0 + Int($1.1) }
        let expected = CGPoint(x: canvas.width / 2 + CGFloat(sumX), y: canvas.height / 2 + CGFloat(sumY))
        emit(String(format: "期望光标位置 = 中心 + (%+d,%+d) = (%.0f,%.0f)", sumX, sumY, expected.x, expected.y))
        sleepMs(500)
        emit("鼠标会话统计：\(session.cursorStatsSummary())")

        // ---- 连续导出 N 帧 PNG ----
        try? FileManager.default.removeItem(atPath: directory)
        try? FileManager.default.createDirectory(atPath: directory, withIntermediateDirectories: true)
        // 残影专项：导出期间持续移动光标，制造「光标移动但游戏新帧尚未跟上」的复用同一槽位场景，
        // 从而检验「先擦旧光标再画新光标」是否真的消除了残影（若擦除失效会出现第二簇）。
        let sweepMode = ProcessInfo.processInfo.environment["QR_CURSORDBL_SWEEP"] == "1"
        session.scheduleFrameDumpSequence(count: count, directory: directory)
        let dumpDeadline = Date().addingTimeInterval(Double(count) / 30.0 + 12)
        var sweep = 0
        while Date() < dumpDeadline {
            if sweepMode {
                sweep += 5
                session.sendMouseMove(x: Float(500 + (sweep % 900)), y: Float(300 + (sweep % 420)), dx: 5, dy: 3)
            }
            let files = (try? FileManager.default.contentsOfDirectory(atPath: directory))?.count ?? 0
            if files >= count { break }
            usleep(sweepMode ? 12_000 : 100_000)
        }
        sleepMs(300)

        // ---- 逐帧分析：数白色光标像素簇（连通域），断言「恰好 1 簇且在期望位置」----
        //  为什么用连通域而不是「是否有白像素」：残影会同时在旧位置留一簇白像素，
        //  只数白像素总数无法区分「一个光标」和「一个光标 + 一个残影」。簇数==1 才排除残影。
        var lost = 0
        var lostFrames: [Int] = []
        var ghostFrames: [Int] = []
        var clusterHistogram: [Int: Int] = [:]
        var rows: [String] = []
        for i in 0..<count {
            let path = "\(directory)/frame-\(String(format: "%03d", i)).png"
            guard let png = loadPNG(path) else { lost += 1; lostFrames.append(i); continue }
            let clusters = whiteClusters(png.px, w: png.w, h: png.h, minSize: 5)
            clusterHistogram[clusters.count, default: 0] += 1
            // 取离 expected 最近的一簇作为「光标」；其余簇视为残影。
            let nearest = clusters.min { dist2($0.cx, $0.cy, expected) < dist2($1.cx, $1.cy, expected) }
            let cx = nearest?.cx ?? -1, cy = nearest?.cy ?? -1
            let nearTip = nearest != nil && abs(Double(cx) - Double(expected.x)) <= 15 && abs(Double(cy) - Double(expected.y)) <= 15
            let noCursor = (nearest == nil)
            let ghost = clusters.count > 1
            // 残影扫描模式下光标位置每帧都不同，只断言「恰好 1 簇且存在光标」；
            // 定点模式下再额外断言光标落在期望位置附近。
            if noCursor || (!sweepMode && !nearTip) { lost += 1; lostFrames.append(i) }
            if ghost { ghostFrames.append(i) }
            if i < 8 || i == count - 1 {
                rows.append(String(format: "  frame-%03d: 簇数=%d 光标簇中心=(%d,%d) 期望=(%.0f,%.0f) %@",
                                   i, clusters.count, cx, cy, expected.x, expected.y,
                                   noCursor ? "缺光标" : (ghost ? "有残影" : "OK")))
            }
        }
        let files = (try? FileManager.default.contentsOfDirectory(atPath: directory))?.count ?? 0
        let rev = session.reverseChannelStats()
        emit("导出 PNG 帧数 = \(files)（目标 \(count)）")
        emit("反向通道统计：通知=\(rev.received) 最近seq=\(rev.lastSeq) 最近槽位=\(rev.lastSlot)")
        emit("帧簇数分布 = \(clusterHistogram.sorted { $0.key < $1.key }.map { "\($0.key)簇:\($0.value)帧" }.joined(separator: " "))")
        for r in rows { emit(r) }
        emit("光标缺帧数 = \(lost) / \(count)\(lostFrames.isEmpty ? "" : "，缺帧序号=\(lostFrames.prefix(20))")")
        emit("残影帧数（簇数>1） = \(ghostFrames.count) / \(count)\(ghostFrames.isEmpty ? "" : "，残影序号=\(ghostFrames.prefix(20))")")
        let pass = (lost == 0 && ghostFrames.count == 0)
        emit("判定：\(pass ? "PASS（每帧恰好 1 簇光标，位置正确，无残影）" : "FAIL（缺光标或存在残影）")")

        session.stop()
        emit("=== 双缓冲光标自测结束 ===")
        exit(0)
    }

    /// 读取文本文件的整行（自测用）
    private static func readLines(_ path: String) -> [String] {
        guard let text = try? String(contentsOfFile: path, encoding: .utf8) else { return [] }
        return text.split(separator: "\n").map(String.init)
    }

    // MARK: - 编码驱动 / 帧率与带宽自测（QR_GAMESTREAM_DRIVE_SELFTEST=1）
    //
    //  目的（任务2/3）：量化「事件驱动」编码相对固定 60fps 节拍的收益：
    //    · 阶段A（光标不动、无输入）：编码只跟随游戏新帧 → 帧率≈游戏帧率，不重复编码同一帧；
    //    · 阶段B（光标持续移动）：编码跟随「光标移动 + 游戏新帧」→ 帧率抬升到协商帧率量级
    //      （最高跑到 fps），证明光标更新能跟上客户端帧率；
    //    · 全程报告平均每帧字节数，以及「Idle 静止窗口」内的编码帧数（配合把游戏限到低帧率验证）。

    static func runDriveSelfTestFromEnvironment() {
        let env = ProcessInfo.processInfo.environment
        guard env["QR_GAMESTREAM_DRIVE_SELFTEST"] == "1" else { return }
        let executable = env["QR_GAMESTREAM_EXEC"]
            ?? MLStreamingHost.gameExecutablePath()
        let project = env["QR_GAMESTREAM_PROJECT"] ?? "/tmp/gdtest"
        let fps = Int(env["QR_DRIVE_FPS"] ?? "") ?? 60
        let window = Double(env["QR_DRIVE_WINDOW"] ?? "") ?? 3.0
        DispatchQueue.global(qos: .userInitiated).async {
            runDriveSelfTest(executablePath: executable, projectPath: project, fps: fps, window: window)
        }
    }

    private static func runDriveSelfTest(executablePath: String, projectPath: String, fps: Int, window: Double) {
        func emit(_ line: String) {
            print("[QRDRIVE] \(line)")
            fflush(stdout)
        }
        emit("=== 编码驱动 / 帧率与带宽自测开始 ===")
        emit("executable = \(executablePath)  project = \(projectPath)")
        emit("协商帧率 = \(fps)fps  测量窗口 = \(String(format: "%.1f", window))s  游戏 --max-fps = \(ProcessInfo.processInfo.environment["QR_GAMESTREAM_MAX_FPS"] ?? "(工程默认)")")

        let session = MLGameStreamSession(width: 1920, height: 1080, fps: fps, bitrateBps: 12_000_000,
                                          executablePath: executablePath, projectPath: projectPath)
        let bridge = MLGameVideoBridge(session: session)
        let counter = FrameCounter()
        bridge.onFrame = { _, _, _ in counter.inc() }
        session.onLog = { emit("log: \($0)") }

        do {
            try session.start()
        } catch {
            emit("启动失败：\(error.localizedDescription)")
            exit(1)
        }
        let ready = Date().addingTimeInterval(45)
        while Date() < ready {
            if counter.value >= 5 && session.isInputConnected { break }
            usleep(200_000)
        }
        emit("输入 socket 已连接 = \(session.isInputConnected)")

        // ---- 阶段A：光标不动（不发任何输入）----
        usleep(500_000)
        let a0 = session.encodeStats(); let ta0 = Date()
        // 每 100ms 采样一次编码帧数，用于统计「静止窗口内是否有 0 编码的小窗口」
        var idleSlices = 0, totalSlices = 0, lastSliceFrames = a0.frames
        while Date().timeIntervalSince(ta0) < window {
            usleep(100_000)
            let now = session.encodeStats().frames
            totalSlices += 1
            if now == lastSliceFrames { idleSlices += 1 }
            lastSliceFrames = now
        }
        let a1 = session.encodeStats(); let dta = Date().timeIntervalSince(ta0)
        let fpsA = Double(a1.frames - a0.frames) / dta
        let bytesA = a1.frames > a0.frames ? Double(a1.bytes - a0.bytes) / Double(a1.frames - a0.frames) : 0
        emit(String(format: "阶段A（光标不动）：编码 %d 帧 / %.1fs = %.1f fps，平均每帧 %.0f 字节；空编码的 100ms 小窗口 = %d/%d；触发[游戏新帧=%d 光标移动=%d]",
                    a1.frames - a0.frames, dta, fpsA, bytesA, idleSlices, totalSlices,
                    a1.byGame - a0.byGame, a1.byCursor - a0.byCursor))

        // ---- 阶段B：光标持续移动（≈125Hz 输入，看编码能否跟上协商帧率）----
        let b0 = session.encodeStats(); let tb0 = Date()
        var sweep = 0
        while Date().timeIntervalSince(tb0) < window {
            // 沿对角线来回扫动，位置持续变化 → 每个输入都触发一次「光标移动」编码（受 fps 限速）
            sweep += 6
            let x = Float(300 + (sweep % 900))
            let y = Float(300 + (sweep % 400))
            session.sendMouseMove(x: x, y: y, dx: 6, dy: 2)
            usleep(8_000)
        }
        let b1 = session.encodeStats(); let dtb = Date().timeIntervalSince(tb0)
        let fpsB = Double(b1.frames - b0.frames) / dtb
        let bytesB = b1.frames > b0.frames ? Double(b1.bytes - b0.bytes) / Double(b1.frames - b0.frames) : 0
        let cursorTriggers = b1.byCursor - b0.byCursor
        emit(String(format: "阶段B（光标移动）：编码 %d 帧 / %.1fs = %.1f fps，平均每帧 %.0f 字节；触发[游戏新帧=%d 光标移动=%d]，光标更新速率 ≈ %.1f fps",
                    b1.frames - b0.frames, dtb, fpsB, bytesB,
                    b1.byGame - b0.byGame, cursorTriggers, Double(cursorTriggers) / dtb))

        emit(String(format: "小结：光标更新能力上限 = %.1f fps（协商 %d fps）；A→B 说明光标从「跟随游戏帧」提升到「跟随客户端帧率」", max(fpsA, fpsB), fps))
        session.stop()
        emit("=== 编码驱动自测结束 ===")
        exit(0)
    }

    // MARK: - PNG 读取 / 像素分析工具（自测用）

    /// 读取 PNG 为 RGBA8（内存第 0 行 = 图像顶部，左上原点）。
    private static func loadPNG(_ path: String) -> (w: Int, h: Int, px: [UInt8])? {
        guard let src = CGImageSourceCreateWithURL(URL(fileURLWithPath: path) as CFURL, nil),
              let img = CGImageSourceCreateImageAtIndex(src, 0, nil) else { return nil }
        let w = img.width, h = img.height
        var px = [UInt8](repeating: 0, count: w * h * 4)
        let ok = px.withUnsafeMutableBytes { raw -> Bool in
            guard let ctx = CGContext(data: raw.baseAddress, width: w, height: h, bitsPerComponent: 8,
                                      bytesPerRow: w * 4, space: CGColorSpaceCreateDeviceRGB(),
                                      bitmapInfo: CGImageAlphaInfo.premultipliedLast.rawValue) else { return false }
            ctx.draw(img, in: CGRect(x: 0, y: 0, width: w, height: h))
            return true
        }
        return ok ? (w, h, px) : nil
    }

    /// 统计「接近纯白」的像素（光标填充色）数量与包围盒，以及离 expected 超过 100px 的白像素数。
    private static func analyzeWhiteCluster(_ px: [UInt8], w: Int, h: Int, expected: CGPoint)
        -> (count: Int, minX: Int, minY: Int, maxX: Int, maxY: Int, outside: Int) {
        var count = 0, minX = w, minY = h, maxX = -1, maxY = -1, outside = 0
        for y in 0..<h {
            for x in 0..<w {
                let i = (y * w + x) * 4
                guard px[i] >= 200, px[i + 1] >= 200, px[i + 2] >= 200 else { continue }
                count += 1
                if x < minX { minX = x }
                if y < minY { minY = y }
                if x > maxX { maxX = x }
                if y > maxY { maxY = y }
                let dx = Double(x) - Double(expected.x), dy = Double(y) - Double(expected.y)
                if (dx * dx + dy * dy).squareRoot() > 100 { outside += 1 }
            }
        }
        return (count, minX, minY, maxX, maxY, outside)
    }

    /// 白色像素簇（连通域）：返回每个 8-邻接连通域的像素数与质心。
    /// 用于区分「一个光标」和「光标 + 残影」——残影会形成第二簇。
    /// 只对「接近纯白」的像素做遍历与泛洪，复杂度与白像素数量同阶（光标仅数百像素），很快。
    private static func whiteClusters(_ px: [UInt8], w: Int, h: Int, minSize: Int)
        -> [(size: Int, cx: Int, cy: Int)] {
        var visited = [Bool](repeating: false, count: w * h)
        var out: [(size: Int, cx: Int, cy: Int)] = []
        func white(_ x: Int, _ y: Int) -> Bool {
            let i = (y * w + x) * 4
            return px[i] >= 200 && px[i + 1] >= 200 && px[i + 2] >= 200
        }
        for y in 0..<h {
            for x in 0..<w {
                let idx = y * w + x
                if visited[idx] || !white(x, y) { continue }
                // 8-邻接 BFS
                var stack = [(x, y)]
                visited[idx] = true
                var size = 0, sumX = 0, sumY = 0
                while let (cx, cy) = stack.popLast() {
                    size += 1; sumX += cx; sumY += cy
                    for dy in -1...1 {
                        for dx in -1...1 {
                            let nx = cx + dx, ny = cy + dy
                            if nx < 0 || ny < 0 || nx >= w || ny >= h { continue }
                            let ni = ny * w + nx
                            if visited[ni] || !white(nx, ny) { continue }
                            visited[ni] = true
                            stack.append((nx, ny))
                        }
                    }
                }
                if size >= minSize { out.append((size, sumX / size, sumY / size)) }
            }
        }
        return out
    }

    /// 两点距离平方（用于找「离期望位置最近」的簇）
    private static func dist2(_ x: Int, _ y: Int, _ p: CGPoint) -> Double {
        let dx = Double(x) - Double(p.x), dy = Double(y) - Double(p.y)
        return dx * dx + dy * dy
    }

    /// 从四边向内找「整行/整列全黑」的连续带宽（黑边检测），并统计整体非黑占比。
    private static func analyzeBars(_ px: [UInt8], w: Int, h: Int)
        -> (top: Int, bottom: Int, left: Int, right: Int, nonBlack: Double) {
        func pixelNonBlack(_ x: Int, _ y: Int) -> Bool {
            let i = (y * w + x) * 4
            return px[i] > 8 || px[i + 1] > 8 || px[i + 2] > 8
        }
        func rowBlack(_ y: Int) -> Bool {
            var x = 0
            while x < w { if pixelNonBlack(x, y) { return false }; x += 1 }
            return true
        }
        func colBlack(_ x: Int) -> Bool {
            var y = 0
            while y < h { if pixelNonBlack(x, y) { return false }; y += 1 }
            return true
        }
        var top = 0; while top < h && rowBlack(top) { top += 1 }
        var bottom = 0; while bottom < h && rowBlack(h - 1 - bottom) { bottom += 1 }
        var left = 0; while left < w && colBlack(left) { left += 1 }
        var right = 0; while right < w && colBlack(w - 1 - right) { right += 1 }
        var nb = 0
        for i in stride(from: 0, to: px.count, by: 4) {
            if px[i] > 8 || px[i + 1] > 8 || px[i + 2] > 8 { nb += 1 }
        }
        return (top, bottom, left, right, Double(nb) / Double(w * h))
    }
}

// 桥接自测运行方式（构建并部署好 App 之后）：
//
//   QR_GAMESTREAM_BRIDGE_SELFTEST=1 \
//   QR_GAMESTREAM_EXEC=/tmp/qr-probe/GodotPatched.app/Contents/MacOS/Godot \
//   QR_GAMESTREAM_PROJECT=/tmp/gdtest \
//   QR_GAMESTREAM_FRAMES=30 \
//   /Volumes/soft/backup/prototype/remote/QuickRecorder/build/QuickRecorder.app/Contents/MacOS/QuickRecorder
//
// 坐标映射定量自测运行方式（两种协商分辨率各跑一遍）：
//
//   QR_GAMESTREAM_COORD_SELFTEST=1 QR_COORD_W=1280 QR_COORD_H=720 \
//   QR_GAMESTREAM_EXEC=/tmp/qr-probe/GodotPatched.app/Contents/MacOS/Godot \
//   QR_GAMESTREAM_PROJECT=/tmp/gdtest \
//   /Volumes/soft/backup/prototype/remote/QuickRecorder/build/QuickRecorder.app/Contents/MacOS/QuickRecorder
//
//   （把 QR_COORD_W/H 改成 1920/1080 再跑一遍；游戏侧事件坐标落盘在 /tmp/gdtest_events.log）
