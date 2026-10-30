//
//  MoonlightInput.swift
//  QuickRecorder
//
//  把 Moonlight 客户端发来的输入事件注入到本机（或路由到合成画面）。
//
//  线格式（moonlight-common-c Input.h，pack(1)）：
//    NV_INPUT_HEADER { uint32 size(BE)  uint32 magic(LE) }
//    body 从偏移 8 开始；各字段字节序见下方逐条注释。
//
//  注入使用 CGEvent，需要「辅助功能」授权（系统设置 → 隐私与安全性 → 辅助功能）。
//

import Foundation
import CoreGraphics
import ApplicationServices

/// 远程输入的映射方式
enum MLInputLayout {
    /// 整幅画面直接映射到主机上的某个窗口矩形
    case window(outputSize: CGSize, targetRect: CGRect)
    /// 合成画面：顶部操作栏 + 窗口内容区。操作栏内的点击触发回调，其余映射进窗口
    case composedWindow(outputSize: CGSize, toolbarHeight: CGFloat, contentRect: CGRect, targetRect: CGRect)
    /// 虚拟桌面：坐标只用于合成画面内，不注入系统
    case virtualDesktop(canvasSize: CGSize)
    /// 外部来源（PS5）：画面不来自主机的屏幕/窗口，因此指针与键盘都不注入系统，
    /// 只保留顶部操作栏的点击（返回/重连），其余交给 PS5 会话处理
    case externalSource(outputSize: CGSize, toolbarHeight: CGFloat)
    /// 游戏模式：画面来自无窗口运行的 Godot 游戏。输入不注入系统，而是转成固定 24 字节包
    /// 发给游戏会话（见 GameStream.swift 的 sendInput）。
    /// - outputSize：客户端协商的串流画布尺寸（归一化坐标的参照系）
    /// - contentRect：游戏画面在画布中的矩形（与窗口模式的语义一致，用于坐标换算）
    /// - gameSize：游戏画面自身的像素尺寸（换算目标）
    case game(outputSize: CGSize, contentRect: CGRect, gameSize: CGSize)
}

/// 画面顶部操作栏按钮（窗口共享模式的历史类型，sunGame 不使用，保留以兼容输入映射代码）
enum MLToolbarButton {
    case back
    case activate
}

/// Moonlight 触摸事件类型（moonlight-common-c Limelight.h LI_TOUCH_EVENT_*）
enum MLTouchEvent {
    static let hover: UInt8 = 0x00
    static let down: UInt8 = 0x01
    static let up: UInt8 = 0x02
    static let move: UInt8 = 0x03
    static let cancel: UInt8 = 0x04
    static let buttonOnly: UInt8 = 0x05
    static let hoverLeave: UInt8 = 0x06
    static let cancelAll: UInt8 = 0x07
}

/// 手柄状态（对应 NV_MULTI_CONTROLLER_PACKET 里我们关心的字段）
struct MLGamepadState {
    /// Moonlight 按键位掩码（LI_CTRL_FLAG_*）
    var buttons: UInt16
    /// 左/右模拟扳机（0…255）
    var leftTrigger: UInt8
    var rightTrigger: UInt8
    /// 左右摇杆（-32768…32767，中心 0）
    var leftStickX: Int16
    var leftStickY: Int16
    var rightStickX: Int16
    var rightStickY: Int16
}

/// Moonlight 手柄按键位（LI_CTRL_FLAG_*）
enum MLGamepadButton {
    static let dpadUp: UInt16 = 0x0001
    static let dpadDown: UInt16 = 0x0002
    static let dpadLeft: UInt16 = 0x0004
    static let dpadRight: UInt16 = 0x0008
    static let start: UInt16 = 0x0010
    static let back: UInt16 = 0x0020
    static let leftStick: UInt16 = 0x0040
    static let rightStick: UInt16 = 0x0080
    static let leftBumper: UInt16 = 0x0100
    static let rightBumper: UInt16 = 0x0200
    static let special: UInt16 = 0x0400
    static let misc: UInt16 = 0x0800
    static let a: UInt16 = 0x1000
    static let b: UInt16 = 0x2000
    static let x: UInt16 = 0x4000
    static let y: UInt16 = 0x8000
}

final class MLInputInjector {
    static let shared = MLInputInjector()

    /// 输入映射布局
    private var layout: MLInputLayout = .virtualDesktop(canvasSize: CGSize(width: 1920, height: 1080))
    /// 最近一次远程指针位置（归一化 0…1）
    private var normalizedCursor = CGPoint(x: 0.5, y: 0.5)

    /// 垂直滚轮累积（Moonlight 下发高精度增量，需按 120 归整）
    private var accumulatedVScroll: Int32 = 0
    private var accumulatedHScroll: Int32 = 0

    /// 远程按下「返回虚拟桌面」组合键（Ctrl+Alt+D）时的回调；回调后该按键不再注入被控窗口
    var onToggleDesktop: (() -> Void)?
    /// 远程指针移动（归一化坐标）；虚拟光标据此绘制
    var onPointerMove: ((CGPoint) -> Void)?
    /// 指针变化时请求宿主尽快出一帧（让光标运动与输入对齐，宿主编排的节流在采集管线里）
    var onFrameRequest: (() -> Void)?
    /// 虚拟桌面里的点击（归一化坐标）
    var onDesktopClick: ((CGPoint) -> Void)?
    /// 命中的操作栏按钮（由宿主按当前操作栏布局判定）
    var toolbarButtonAt: ((CGPoint) -> MLToolbarButton?)?
    /// 点击了操作栏按钮
    var onToolbarAction: ((MLToolbarButton) -> Void)?
    /// 手柄状态（NV_MULTI_CONTROLLER_PACKET）；PS5 外部来源下用于驱动远端主机
    var onGamepadState: ((MLGamepadState) -> Void)?
    /// 游戏模式：把事件转成固定 24 字节包交给游戏会话。
    /// 参数依次为 (kind, pressed, code, x, y, dx, dy, touchEvent)，x/y 已经是「游戏画面像素」坐标。
    /// kind：0=鼠标移动 1=鼠标按键 2=滚轮 3=触摸
    /// touchEvent：仅 kind==3 有意义，为 Moonlight 的 LI_TOUCH_EVENT_* 原始类型；
    ///             其余事件传 0xFF（无）。引擎靠它区分 HOVER/UP/CANCEL/CANCEL_ALL 等语义。
    /// - Returns: 是否已入队（供触摸明细记录「是否真的写进了输入包」）。
    var onGameInput: ((UInt8, Bool, UInt16, Float, Float, Float, Float, UInt8) -> Bool)?
    /// 诊断日志（触摸逐事件明细等）；由宿主写入串流日志。
    var onLog: ((String) -> Void)?

    /// 返回虚拟桌面的远程组合键（Win32 虚拟键码 VK_D）
    private static let toggleDesktopVK: UInt8 = 0x44

    /// 诊断：按类别统计收到的输入事件
    private var moveAbsCount = 0
    private var moveRelCount = 0
    private var buttonCount = 0
    private var keyCount = 0
    /// 诊断：指针位移步长与事件间隔（用于判断客户端事件是否连续）
    private var pointerStepSum: Double = 0
    private var pointerStepMax: Double = 0
    private var pointerStepCount = 0
    private var lastPointerEventTime: CFTimeInterval = 0
    private var maxPointerGapMs: Double = 0

    /// 诊断：从客户端收到的触摸事件，按 LI_TOUCH_EVENT_* 分类计数（本窗口内）
    private var touchCounts: [UInt8: Int] = [:]
    /// 诊断：本窗口内 MOVE 总数与按 pointerId 的分布（MOVE 不逐条打日志，只做汇总）
    private var touchMoveCount = 0
    private var touchMoveByPointer: [UInt32: Int] = [:]

    /// 诊断：本窗口内鼠标移动的聚合样本（只记首个/末个，避免移动风暴淹没日志）。
    /// 鼠标路径没有逐事件落盘时，点击错位根本无法定位发生在「归一化」还是「换算成像素」哪一步——
    /// 这里补齐原始 x/y/refW/refH、归一化值、换算后的像素值与是否写出。
    private struct MouseMoveSample {
        /// 事件来源的原始描述（绝对：原始 x/y/refW/refH；相对：原始 dx/dy）
        var raw: String
        var normalized: CGPoint
        var pixel: CGPoint
        var dispatched: Bool
    }
    private var mouseMoveFirst: MouseMoveSample?
    private var mouseMoveLast: MouseMoveSample?
    private var mouseMoveCount = 0

    /// 取出并重置「本窗口」的鼠标移动聚合描述（供 GameStream 的 5 秒日志打印）
    func takeMouseStatsDescription() -> String {
        lock.lock(); defer { lock.unlock() }
        guard mouseMoveCount > 0, let first = mouseMoveFirst, let last = mouseMoveLast else {
            return "游戏鼠标(本窗口)：移动 无"
        }
        let text = "游戏鼠标(本窗口)：移动共=\(mouseMoveCount) " +
            "首=[\(Self.describeMouseMove(first))] 末=[\(Self.describeMouseMove(last))]"
        mouseMoveFirst = nil
        mouseMoveLast = nil
        mouseMoveCount = 0
        return text
    }

    private static func describeMouseMove(_ s: MouseMoveSample) -> String {
        String(format: "原始=%@ 归一化=(%.3f,%.3f) 像素=(%d,%d) 下发=%@",
               s.raw, s.normalized.x, s.normalized.y,
               Int(s.pixel.x.rounded()), Int(s.pixel.y.rounded()), s.dispatched ? "是" : "否")
    }

    private func recordMouseMove(rawText: String,
                                 normalized: CGPoint, pixel: CGPoint, dispatched: Bool) {
        let sample = MouseMoveSample(raw: rawText, normalized: normalized, pixel: pixel, dispatched: dispatched)
        lock.lock()
        if mouseMoveFirst == nil { mouseMoveFirst = sample }
        mouseMoveLast = sample
        mouseMoveCount += 1
        lock.unlock()
    }

    /// 取出并重置「本窗口」的触摸事件分类计数（供 GameStream 的 5 秒日志打印）
    func takeTouchStatsDescription() -> String {
        lock.lock(); defer { lock.unlock() }
        let names: [UInt8: String] = [MLTouchEvent.hover: "hover", MLTouchEvent.down: "down",
                                      MLTouchEvent.up: "up", MLTouchEvent.move: "move",
                                      MLTouchEvent.cancel: "cancel", MLTouchEvent.buttonOnly: "btnOnly",
                                      MLTouchEvent.hoverLeave: "hoverLeave", MLTouchEvent.cancelAll: "cancelAll"]
        let ordered: [UInt8] = [MLTouchEvent.hover, MLTouchEvent.down, MLTouchEvent.up, MLTouchEvent.move,
                                MLTouchEvent.cancel, MLTouchEvent.buttonOnly, MLTouchEvent.hoverLeave,
                                MLTouchEvent.cancelAll]
        var parts = ordered.compactMap { type -> String? in
            guard let count = touchCounts[type], count > 0 else { return nil }
            return "\(names[type] ?? String(format: "0x%02X", type))×\(count)"
        }
        // 包含表外的未知类型，便于发现协议变化
        for (type, count) in touchCounts where !ordered.contains(type) && count > 0 {
            parts.append(String(format: "0x%02X×%d", type, count))
        }
        touchCounts.removeAll()

        // MOVE 的 pointerId 分布：判断「拖动到底有没有产生 move、来自哪个触点」
        var movePart = ""
        if touchMoveCount > 0 {
            let dist = touchMoveByPointer.sorted { $0.key < $1.key }
                .map { "\($0.key)×\($0.value)" }.joined(separator: " ")
            movePart = " move分布[\(dist)]"
        }
        touchMoveCount = 0
        touchMoveByPointer.removeAll()

        return "触摸(本窗口)：\(parts.isEmpty ? "无" : parts.joined(separator: " "))\(movePart)"
    }

    /// 取出并重置输入事件统计（用于排查指针不流畅问题）
    func takeStatsDescription() -> String {
        lock.lock(); defer { lock.unlock() }
        let avgStep = pointerStepCount > 0 ? pointerStepSum / Double(pointerStepCount) : 0
        let text = String(format: "输入事件：绝对移动=%d 相对移动=%d 按键=%d 键盘=%d；平均步长=%.0fpx 最大步长=%.0fpx 最大事件间隔=%.0fms",
                          moveAbsCount, moveRelCount, buttonCount, keyCount,
                          avgStep * 1920, pointerStepMax * 1920, maxPointerGapMs)
        moveAbsCount = 0
        moveRelCount = 0
        buttonCount = 0
        keyCount = 0
        pointerStepSum = 0
        pointerStepMax = 0
        pointerStepCount = 0
        maxPointerGapMs = 0
        return text
    }

    private let lock = NSLock()

    private init() {}

    // MARK: - 配置

    func configure(layout: MLInputLayout) {
        lock.lock()
        self.layout = layout
        lock.unlock()
    }

    private var currentLayout: MLInputLayout {
        lock.lock(); defer { lock.unlock() }
        return layout
    }

    /// 画面不来自主机的屏幕/窗口（虚拟桌面、PS5、游戏等外部来源）：
    /// 这类布局下指针/键盘/滚轮都不注入系统，只用于画面上层的交互（或转发给外部会话）
    private var isNonInjectingSource: Bool {
        switch currentLayout {
        case .virtualDesktop, .externalSource, .game: return true
        case .window, .composedWindow: return false
        }
    }

    private func currentCursor() -> CGPoint {
        lock.lock(); defer { lock.unlock() }
        return normalizedCursor
    }

    /// 是否已获得辅助功能授权
    static var hasAccessibilityPermission: Bool {
        AXIsProcessTrusted()
    }

    /// 请求辅助功能授权（弹出系统提示）
    static func requestAccessibilityPermission() {
        let options = [kAXTrustedCheckOptionPrompt.takeUnretainedValue() as String: true] as CFDictionary
        _ = AXIsProcessTrustedWithOptions(options)
    }

    // MARK: - 分发

    func handle(magic: UInt32, body: [UInt8]) {
        countEvent(magic)
        switch magic {
        case MLInputMagic.mouseMoveRel:
            // deltaX(int16 BE) @0, deltaY(int16 BE) @2
            guard body.count >= 4 else { return }
            handleRelativeMove(dx: Int16(bitPattern: body.mlBE16(0)), dy: Int16(bitPattern: body.mlBE16(2)))

        case MLInputMagic.mouseMoveAbs:
            // x(int16 BE) @0, y @2, unused @4, width @6, height @8
            guard body.count >= 10 else { return }
            let x = Int16(bitPattern: body.mlBE16(0))
            let y = Int16(bitPattern: body.mlBE16(2))
            let refW = Int16(bitPattern: body.mlBE16(6))
            let refH = Int16(bitPattern: body.mlBE16(8))
            guard refW > 0, refH > 0 else { return }
            // 把原始 x/y/refW/refH 一并带下去：游戏模式下用于明细日志，便于直接判定点击错位在哪一步
            handlePointerMove(CGPoint(x: CGFloat(x) / CGFloat(refW), y: CGFloat(y) / CGFloat(refH)),
                              rawText: "(\(x),\(y),\(refW),\(refH))")

        case MLInputMagic.mouseButtonDown:
            guard let b = body.first else { return }
            handleButton(b, down: true)

        case MLInputMagic.mouseButtonUp:
            guard let b = body.first else { return }
            handleButton(b, down: false)

        case MLInputMagic.scroll:
            guard body.count >= 2 else { return }
            let amount = Int16(bitPattern: body.mlBE16(0))
            if case .game(let outputSize, let contentRect, let gameSize) = currentLayout {
                // 游戏模式：滚轮量转成 24 字节包（kind=2，dy 为滚轮量）
                let point = gamePoint(currentCursor(), outputSize: outputSize,
                                      contentRect: contentRect, gameSize: gameSize)
                _ = onGameInput?(2, true, 0, Float(point.x), Float(point.y), 0, Float(amount), 0xFF)
            } else {
                guard !isNonInjectingSource else { return }
                scrollVertical(amount)
            }

        case MLInputMagic.hscroll:
            guard body.count >= 2 else { return }
            let amount = Int16(bitPattern: body.mlBE16(0))
            if case .game(let outputSize, let contentRect, let gameSize) = currentLayout {
                let point = gamePoint(currentCursor(), outputSize: outputSize,
                                      contentRect: contentRect, gameSize: gameSize)
                _ = onGameInput?(2, true, 0, Float(point.x), Float(point.y), Float(amount), 0, 0xFF)
            } else {
                guard !isNonInjectingSource else { return }
                scrollHorizontal(amount)
            }

        case MLInputMagic.keyDown:
            handleKeyboard(body, down: true)

        case MLInputMagic.keyUp:
            handleKeyboard(body, down: false)

        case MLInputMagic.utf8Text:
            guard !isNonInjectingSource else { return }
            guard let text = String(bytes: body, encoding: .utf8) else { return }
            typeText(text)

        case MLInputMagic.multiController:
            handleMultiController(body)

        case MLInputMagic.touch:
            handleTouch(body)

        default:
            // 笔 / 传感器事件在「共享窗口」场景不注入
            break
        }
    }

    /// 触摸事件包（SS_TOUCH_PACKET，去掉 8 字节 NV 头后的包体）。
    ///
    /// 布局依据 moonlight-common-c `src/Input.h`（`#pragma pack(1)`）：
    ///   typedef struct _SS_TOUCH_PACKET {
    ///       NV_INPUT_HEADER header;   // 8 字节：size(uint32 BE) + magic(uint32 LE)
    ///       uint8_t  eventType;       // body@0，LI_TOUCH_EVENT_*
    ///       uint8_t  zero[1];         // body@1，保留/对齐
    ///       uint16_t rotation;        // body@2，小端
    ///       uint32_t pointerId;       // body@4，小端
    ///       netfloat x;               // body@8， 小端 float，归一化 0..1（左上原点）
    ///       netfloat y;               // body@12，小端 float
    ///       netfloat pressureOrDistance;  // body@16
    ///       netfloat contactAreaMajor;    // body@20
    ///       netfloat contactAreaMinor;    // body@24
    ///   } SS_TOUCH_PACKET;            // 包体 28 字节，整包 36 字节
    /// 坐标是归一化 0..1（Sunshine src/input.cpp:1374-1378 把它乘 65535 当 16 位坐标处理即证）。
    /// 游戏模式下只转成 kind=3 的 24 字节包；Godot 的 emulate_mouse_from_touch 会自动合成鼠标事件。
    ///
    /// eventType 语义处理（关键，修「触摸卡死」）：
    ///   · 只有 DOWN / MOVE 是「接触中」（pressed=1）；
    ///   · UP / CANCEL 是「抬起」；
    ///   · CANCEL_ALL 是「清空所有触点」，必须原样送到引擎；
    ///   · HOVER / HOVER_LEAVE / BUTTON_ONLY 不是接触事件——过去它们被 pressed=0 压成「抬起」，
    ///     会在拖动过程中凭空插入一次抬起，把引擎的按下状态清掉，从而出现「拖一会儿就卡死」。
    ///     这类事件现在直接不下发（也不影响指针位置，游戏模式用不到悬停）。
    private func handleTouch(_ body: [UInt8]) {
        guard case .game(let outputSize, let contentRect, let gameSize) = currentLayout,
              body.count >= 16 else { return }
        let eventType = body[0]
        let pointerId = body.mlLE32(4)
        // 归一化坐标（0..1，相对整个串流画布）→ 游戏画面像素坐标
        let normalized = CGPoint(x: CGFloat(body.mlLEFloat(8)), y: CGFloat(body.mlLEFloat(12)))
        let point = gamePoint(normalized, outputSize: outputSize,
                              contentRect: contentRect, gameSize: gameSize)

        // MOVE 数量极大（一次拖动可达上千条），逐条打日志会淹没真正有用的信息：
        // 只累计总数与 pointerId 分布，由 5 秒周期的 takeTouchStatsDescription() 汇总输出。
        if eventType == MLTouchEvent.move {
            lock.lock()
            touchCounts[eventType, default: 0] += 1
            touchMoveCount += 1
            touchMoveByPointer[pointerId, default: 0] += 1
            lock.unlock()
            _ = onGameInput?(3, true, UInt16(truncatingIfNeeded: pointerId),
                             Float(point.x), Float(point.y), 0, 0, eventType)
            return
        }

        lock.lock(); touchCounts[eventType, default: 0] += 1; lock.unlock()

        // 状态跃迁类事件逐条打明细：eventType 原始值 / pointerId / 归一化坐标 / 游戏像素坐标 / 是否下发
        let detail = "evt=\(String(format: "0x%02X", eventType)) id=\(pointerId) " +
            "归一化=(\(String(format: "%.3f", normalized.x)),\(String(format: "%.3f", normalized.y))) " +
            "像素=(\(Int(point.x.rounded())),\(Int(point.y.rounded())))"

        switch eventType {
        case MLTouchEvent.hover, MLTouchEvent.hoverLeave, MLTouchEvent.buttonOnly:
            // 非接触事件：不下发，绝不能当成 down/up（过去被压平为 pressed=0，会在拖动中凭空插入抬起）
            onLog?("游戏触摸：\(Self.touchEventName(eventType))(忽略不下发) \(detail)")
            return
        default:
            break
        }

        let pressed = (eventType == MLTouchEvent.down)
        let dispatched = onGameInput?(3, pressed, UInt16(truncatingIfNeeded: pointerId),
                                     Float(point.x), Float(point.y), 0, 0, eventType) ?? false
        onLog?("游戏触摸：\(Self.touchEventName(eventType)) \(detail) 下发=\(dispatched ? "是" : "否")")
    }

    /// 触摸事件类型的可读名（LI_TOUCH_EVENT_*）
    private static func touchEventName(_ type: UInt8) -> String {
        switch type {
        case MLTouchEvent.hover: return "HOVER"
        case MLTouchEvent.down: return "DOWN"
        case MLTouchEvent.up: return "UP"
        case MLTouchEvent.move: return "MOVE"
        case MLTouchEvent.cancel: return "CANCEL"
        case MLTouchEvent.buttonOnly: return "BUTTON_ONLY"
        case MLTouchEvent.hoverLeave: return "HOVER_LEAVE"
        case MLTouchEvent.cancelAll: return "CANCEL_ALL"
        default: return String(format: "0x%02X", type)
        }
    }

    /// 手柄状态包（NV_MULTI_CONTROLLER_PACKET）。
    /// 控制器族的短整数字段为小端（与鼠标/键盘包的大端不同，见 moonlight-common-c InputStream.c
    /// 中 `LE16(packet.multiController.controllerNumber)`）。
    private func handleMultiController(_ body: [UInt8]) {
        // 偏移（相对包体，已去掉 8 字节头）：
        //   0 headerB, 2 controllerNumber, 4 activeGamepadMask, 6 midB,
        //   8 buttonFlags, 10 leftTrigger, 11 rightTrigger,
        //   12 leftStickX, 14 leftStickY, 16 rightStickX, 18 rightStickY
        guard body.count >= 20 else { return }
        let state = MLGamepadState(buttons: body.mlLE16(8),
                                   leftTrigger: body[10],
                                   rightTrigger: body[11],
                                   leftStickX: Int16(bitPattern: body.mlLE16(12)),
                                   leftStickY: Int16(bitPattern: body.mlLE16(14)),
                                   rightStickX: Int16(bitPattern: body.mlLE16(16)),
                                   rightStickY: Int16(bitPattern: body.mlLE16(18)))
        onGamepadState?(state)
    }

    /// 分类统计收到的输入事件（诊断用）
    private func countEvent(_ magic: UInt32) {
        lock.lock(); defer { lock.unlock() }
        switch magic {
        case MLInputMagic.mouseMoveAbs: moveAbsCount += 1
        case MLInputMagic.mouseMoveRel: moveRelCount += 1
        case MLInputMagic.mouseButtonDown, MLInputMagic.mouseButtonUp: buttonCount += 1
        case MLInputMagic.keyDown, MLInputMagic.keyUp: keyCount += 1
        default: break
        }
    }

    // MARK: - 指针

    private func handlePointerMove(_ normalized: CGPoint, rawText: String? = nil) {
        // clamp 到 [0,1]：相对移动是累加的，不夹紧会漂出画面外（表现为「指针跑到屏幕外再也回不来」）。
        let clamped = CGPoint(x: min(max(normalized.x, 0), 1), y: min(max(normalized.y, 0), 1))
        lock.lock()
        let previous = normalizedCursor
        normalizedCursor = clamped

        // 诊断：记录步长与事件间隔，判断客户端事件是均匀还是突发
        let now = CFAbsoluteTimeGetCurrent()
        if lastPointerEventTime > 0 {
            let gap = (now - lastPointerEventTime) * 1000
            if gap > maxPointerGapMs { maxPointerGapMs = gap }
        }
        lastPointerEventTime = now
        let step = Double(max(abs(clamped.x - previous.x), abs(clamped.y - previous.y)))
        pointerStepSum += step
        pointerStepCount += 1
        if step > pointerStepMax { pointerStepMax = step }
        lock.unlock()

        onPointerMove?(clamped)
        // 让画面尽快带上新的光标位置（管线内部会按帧间隔节流）
        onFrameRequest?()

        switch currentLayout {
        case .virtualDesktop:
            // 虚拟桌面里的指针不注入系统
            break
        case .externalSource:
            // PS5 等外部来源：指针只驱动画面上的虚拟光标
            break
        case .game(let outputSize, let contentRect, let gameSize):
            // 游戏模式：把绝对指针位置换算成游戏像素后转发（kind=0）。
            // 移动事件量大，这里不逐条落盘，只记入本窗口的首/末样本供 5 秒诊断汇总。
            let point = gamePoint(clamped, outputSize: outputSize,
                                  contentRect: contentRect, gameSize: gameSize)
            let dispatched = onGameInput?(0, false, 0, Float(point.x), Float(point.y), 0, 0, 0xFF) ?? false
            recordMouseMove(rawText: rawText ?? "(未知来源)", normalized: clamped, pixel: point, dispatched: dispatched)
        case .window(let outputSize, let targetRect):
            postMouseMoved(toHost: hostPoint(clamped, outputSize: outputSize,
                                            frameRect: CGRect(origin: .zero, size: outputSize),
                                            targetRect: targetRect))
        case .composedWindow(let outputSize, let toolbarHeight, let contentRect, let targetRect):
            // 指针在操作栏上时不移动被控窗口，避免误触窗口内容
            guard clamped.y * outputSize.height >= toolbarHeight else { return }
            postMouseMoved(toHost: hostPoint(clamped, outputSize: outputSize,
                                            frameRect: contentRect, targetRect: targetRect))
        }
    }

    private func handleRelativeMove(dx: Int16, dy: Int16) {
        let layout = currentLayout
        let canvas: CGSize
        switch layout {
        case .virtualDesktop(let size): canvas = size
        case .window(let size, _): canvas = size
        case .composedWindow(let size, _, _, _): canvas = size
        case .externalSource(let size, _): canvas = size
        case .game(let size, _, _): canvas = size
        }
        // 相对移动：把像素增量按画布尺寸归一化后累加到当前指针位置。
        // 游戏模式下 outputSize == gameSize（contentRect 为整幅），因此 N 像素增量 → 指针移动 N 游戏像素（1:1）。
        // 原始 dx/dy 一并带进日志，便于核对积分是否 1:1（相对移动没有绝对参照系，必须靠累加）。
        let current = currentCursor()
        handlePointerMove(CGPoint(x: current.x + CGFloat(dx) / max(canvas.width, 1),
                                  y: current.y + CGFloat(dy) / max(canvas.height, 1)),
                          rawText: "(相对 dx=\(dx),dy=\(dy))")
    }

    /// 归一化坐标 → 主机全局坐标
    private func hostPoint(_ normalized: CGPoint, outputSize: CGSize,
                           frameRect: CGRect, targetRect: CGRect) -> CGPoint {
        let framePoint = CGPoint(x: normalized.x * outputSize.width, y: normalized.y * outputSize.height)
        let localX = min(max((framePoint.x - frameRect.minX) / max(frameRect.width, 1), 0), 1)
        let localY = min(max((framePoint.y - frameRect.minY) / max(frameRect.height, 1), 0), 1)
        return CGPoint(x: targetRect.minX + localX * targetRect.width,
                       y: targetRect.minY + localY * targetRect.height)
    }

    /// 归一化坐标（0..1，相对整个串流画布）→ 游戏画面像素坐标。
    ///
    /// 语义与 hostPoint 完全一致：先缩放到画布像素，再按「画面矩形」做一次归一化，最后映射到
    /// 目标坐标系——这里的目标坐标系就是游戏画面自身的像素（gameSize）。这样即便以后画布与游戏
    /// 分辨率不同（例如客户端请求 720p），坐标依旧落在正确的游戏像素上。
    private func gamePoint(_ normalized: CGPoint, outputSize: CGSize,
                           contentRect: CGRect, gameSize: CGSize) -> CGPoint {
        let framePoint = CGPoint(x: normalized.x * outputSize.width, y: normalized.y * outputSize.height)
        let localX = min(max((framePoint.x - contentRect.minX) / max(contentRect.width, 1), 0), 1)
        let localY = min(max((framePoint.y - contentRect.minY) / max(contentRect.height, 1), 0), 1)
        return CGPoint(x: localX * gameSize.width, y: localY * gameSize.height)
    }

    /// Moonlight 鼠标按钮 → Godot MouseButton 编号。
    /// 两者编号顺序不同：Moonlight left=1/middle=2/right=3；Godot LEFT=1/RIGHT=2/MIDDLE=3。
    private func godotMouseButton(_ button: UInt8) -> UInt16? {
        switch button {
        case MLMouseButton.left.rawValue: return 1
        case MLMouseButton.right.rawValue: return 2
        case MLMouseButton.middle.rawValue: return 3
        case MLMouseButton.x1.rawValue: return 8
        case MLMouseButton.x2.rawValue: return 9
        default: return nil
        }
    }

    /// 鼠标按钮的可读名（Moonlight NV 编码）
    private static func mouseButtonName(_ button: UInt8) -> String {
        switch button {
        case MLMouseButton.left.rawValue: return "左键"
        case MLMouseButton.middle.rawValue: return "中键"
        case MLMouseButton.right.rawValue: return "右键"
        case MLMouseButton.x1.rawValue: return "侧键1"
        case MLMouseButton.x2.rawValue: return "侧键2"
        default: return String(format: "按钮0x%02X", button)
        }
    }

    // MARK: - 按键

    private func handleButton(_ button: UInt8, down: Bool) {
        let cursor = currentCursor()
        switch currentLayout {
        case .virtualDesktop:
            // 虚拟桌面：左键按下即选中窗口
            if down, button == MLMouseButton.left.rawValue { onDesktopClick?(cursor) }

        case .externalSource(let outputSize, let toolbarHeight):
            // 外部来源（PS5）：只有操作栏区域可点击，其余交给 PS5 会话（后续接手柄/触摸）
            if cursor.y * outputSize.height < toolbarHeight {
                if down, button == MLMouseButton.left.rawValue,
                   let hit = toolbarButtonAt?(cursor) {
                    onToolbarAction?(hit)
                }
            }

        case .game(let outputSize, let contentRect, let gameSize):
            // 游戏模式：按键转成 24 字节包（kind=1，code=Godot MouseButton）。
            // 按键是状态跃迁事件（数量少但关键），逐条落盘：按钮号 + 像素坐标 + 是否写出。
            guard let godotButton = godotMouseButton(button) else { return }
            let point = gamePoint(cursor, outputSize: outputSize,
                                  contentRect: contentRect, gameSize: gameSize)
            let dispatched = onGameInput?(1, down, godotButton, Float(point.x), Float(point.y), 0, 0, 0xFF) ?? false
            onLog?("游戏鼠标：\(Self.mouseButtonName(button))\(down ? "按下" : "抬起") " +
                   "godotBtn=\(godotButton) 像素=(\(Int(point.x.rounded())),\(Int(point.y.rounded()))) " +
                   "下发=\(dispatched ? "是" : "否")")

        case .window(let outputSize, let targetRect):
            let point = hostPoint(cursor, outputSize: outputSize,
                                  frameRect: CGRect(origin: .zero, size: outputSize), targetRect: targetRect)
            postMouseButton(button, down: down, at: point)

        case .composedWindow(let outputSize, let toolbarHeight, let contentRect, let targetRect):
            if cursor.y * outputSize.height < toolbarHeight {
                // 操作栏区域：左键按下命中按钮则触发，不注入窗口
                if down, button == MLMouseButton.left.rawValue,
                   let hit = toolbarButtonAt?(cursor) {
                    onToolbarAction?(hit)
                }
                return
            }
            let point = hostPoint(cursor, outputSize: outputSize, frameRect: contentRect, targetRect: targetRect)
            postMouseButton(button, down: down, at: point)
        }
    }

    private func handleKeyboard(_ body: [UInt8], down: Bool) {
        // flags(uint8) @0, keyCode(int16 LE，非对齐) @1, modifiers(uint8) @3, zero2(int16 LE) @4
        guard body.count >= 3 else { return }
        let keyCode = UInt16(body[1]) | (UInt16(body[2]) << 8)
        // Sunshine 只取低 8 位
        let vk = UInt8(keyCode & 0x00FF)
        let modifiers = body.count > 3 ? body[3] : 0

        // Ctrl+Alt+D：远程返回虚拟桌面，不作为普通按键注入被控窗口（两种模式下都生效）
        if vk == Self.toggleDesktopVK,
           modifiers & MLModifier.ctrl != 0,
           modifiers & MLModifier.alt != 0 {
            if down { onToggleDesktop?() }
            return
        }
        // 虚拟桌面里没有输入焦点，键盘事件不注入
        guard !isNonInjectingSource else { return }
        key(vk: vk, down: down, modifiers: modifiers)
    }

    // MARK: - 鼠标注入

    private func postMouseMoved(toHost point: CGPoint) {
        post(event: CGEvent(mouseEventSource: source(), mouseType: .mouseMoved,
                            mouseCursorPosition: point, mouseButton: .left))
    }

    private func postMouseButton(_ button: UInt8, down: Bool, at point: CGPoint) {
        let (type, cgButton): (CGEventType, CGMouseButton)
        switch button {
        case MLMouseButton.left.rawValue: (type, cgButton) = (down ? .leftMouseDown : .leftMouseUp, .left)
        case MLMouseButton.right.rawValue: (type, cgButton) = (down ? .rightMouseDown : .rightMouseUp, .right)
        case MLMouseButton.middle.rawValue: (type, cgButton) = (down ? .otherMouseDown : .otherMouseUp, .center)
        case MLMouseButton.x1.rawValue: (type, cgButton) = (down ? .otherMouseDown : .otherMouseUp, .center)
        case MLMouseButton.x2.rawValue: (type, cgButton) = (down ? .otherMouseDown : .otherMouseUp, .center)
        default: return
        }
        post(event: CGEvent(mouseEventSource: source(), mouseType: type,
                            mouseCursorPosition: point, mouseButton: cgButton))
    }

    private func scrollVertical(_ amount: Int16) {
        // 客户端可能下发小于 120 的高精度增量，累积到整刻度后注入
        accumulatedVScroll += Int32(amount)
        let ticks = accumulatedVScroll / 120
        if ticks == 0 { return }
        accumulatedVScroll -= ticks * 120
        post(event: CGEvent(scrollWheelEvent2Source: source(), units: .line,
                            wheelCount: 1, wheel1: ticks, wheel2: 0, wheel3: 0))
    }

    private func scrollHorizontal(_ amount: Int16) {
        accumulatedHScroll += Int32(amount)
        let ticks = accumulatedHScroll / 120
        if ticks == 0 { return }
        accumulatedHScroll -= ticks * 120
        post(event: CGEvent(scrollWheelEvent2Source: source(), units: .line,
                            wheelCount: 2, wheel1: 0, wheel2: ticks, wheel3: 0))
    }

    // MARK: - 键盘注入

    private func key(vk: UInt8, down: Bool, modifiers: UInt8) {
        guard let macKey = MLKeyMap.macKeyCode(forVK: vk) else { return }
        guard let event = CGEvent(keyboardEventSource: source(), virtualKey: macKey, keyDown: down) else { return }
        var flags = CGEventFlags()
        if modifiers & MLModifier.shift != 0 { flags.insert(.maskShift) }
        if modifiers & MLModifier.ctrl != 0 { flags.insert(.maskControl) }
        if modifiers & MLModifier.alt != 0 { flags.insert(.maskAlternate) }
        if modifiers & MLModifier.meta != 0 { flags.insert(.maskCommand) }
        if !flags.isEmpty { event.flags = flags }
        post(event: event)
    }

    private func typeText(_ text: String) {
        guard !text.isEmpty else { return }
        let utf16 = Array(text.utf16)
        guard let event = CGEvent(keyboardEventSource: source(), virtualKey: 0, keyDown: true) else { return }
        event.keyboardSetUnicodeString(stringLength: utf16.count, unicodeString: utf16)
        post(event: event)
        guard let up = CGEvent(keyboardEventSource: source(), virtualKey: 0, keyDown: false) else { return }
        up.keyboardSetUnicodeString(stringLength: utf16.count, unicodeString: utf16)
        post(event: up)
    }

    // MARK: - 工具

    private func source() -> CGEventSource? {
        CGEventSource(stateID: .hidSystemState)
    }

    private func post(event: CGEvent?) {
        event?.post(tap: .cghidEventTap)
    }
}

// MARK: - Win32 Virtual-Key → macOS 键码

enum MLKeyMap {
    /// Windows 虚拟键码（VK_*）到 macOS CGKeyCode 的映射。
    /// 键码按照 US 英文布局解释（与 Moonlight 约定一致）。
    static func macKeyCode(forVK vk: UInt8) -> CGKeyCode? {
        table[vk]
    }

    private static let table: [UInt8: CGKeyCode] = [
        // 字母
        0x41: 0x00, 0x53: 0x01, 0x44: 0x02, 0x46: 0x03, 0x48: 0x04, 0x47: 0x05,
        0x5A: 0x06, 0x58: 0x07, 0x43: 0x08, 0x56: 0x09, 0x42: 0x0B, 0x51: 0x0C,
        0x57: 0x0D, 0x45: 0x0E, 0x52: 0x0F, 0x59: 0x10, 0x54: 0x11, 0x4F: 0x1F,
        0x55: 0x20, 0x49: 0x22, 0x50: 0x23, 0x4C: 0x25, 0x4A: 0x26, 0x4B: 0x28,
        0x4E: 0x2D, 0x4D: 0x2E,
        // 数字行
        0x31: 0x12, 0x32: 0x13, 0x33: 0x14, 0x34: 0x15, 0x35: 0x17, 0x36: 0x16,
        0x37: 0x1A, 0x38: 0x1C, 0x39: 0x19, 0x30: 0x1D,
        // 符号
        0xBD: 0x1B, 0xBB: 0x18, 0xDB: 0x21, 0xDD: 0x1E, 0xDC: 0x2A, 0xBA: 0x29,
        0xDE: 0x27, 0xC0: 0x32, 0xBC: 0x2B, 0xBE: 0x2F, 0xBF: 0x2C,
        // 控制键
        0x0D: 0x24, 0x1B: 0x35, 0x08: 0x33, 0x09: 0x30, 0x20: 0x31, 0x14: 0x39,
        0x2E: 0x75, 0x2D: 0x72, 0x24: 0x73, 0x23: 0x77, 0x21: 0x74, 0x22: 0x79,
        0x25: 0x7B, 0x26: 0x7E, 0x27: 0x7C, 0x28: 0x7D,
        // 修饰键（左右区分）
        0x10: 0x38, 0xA0: 0x38, 0xA1: 0x3C,
        0x11: 0x3B, 0xA2: 0x3B, 0xA3: 0x3E,
        0x12: 0x3A, 0xA4: 0x3A, 0xA5: 0x3D,
        0x5B: 0x37, 0x5C: 0x36, 0x5D: 0x6E, // 右键菜单键映射为 macOS Contextual Menu
        // 功能键
        0x70: 0x7A, 0x71: 0x78, 0x72: 0x63, 0x73: 0x76, 0x74: 0x60, 0x75: 0x61,
        0x76: 0x62, 0x77: 0x64, 0x78: 0x65, 0x79: 0x6D, 0x7A: 0x67, 0x7B: 0x6F,
        0x7C: 0x69, 0x7D: 0x6B, 0x7E: 0x6A, 0x7F: 0x40, 0x80: 0x4F, 0x81: 0x50,
        // 小键盘
        0x60: 0x52, 0x61: 0x53, 0x62: 0x54, 0x63: 0x55, 0x64: 0x56, 0x65: 0x57,
        0x66: 0x58, 0x67: 0x59, 0x68: 0x5B, 0x69: 0x5C,
        0x6A: 0x43, 0x6B: 0x45, 0x6D: 0x4E, 0x6E: 0x41, 0x6F: 0x4B,
        0x0C: 0x47, 0x90: 0x47,
        // 其它
        0x2C: 0x47, // Print Screen → Clear
        0x91: 0x47, // Scroll Lock
        0x13: 0x47, // Pause
    ]
}
