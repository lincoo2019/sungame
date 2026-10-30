//
//  ChiakiPS5.swift
//  QuickRecorder
//
//  PS5 串流接入（基于 chiaki-ng 的 libchiaki）。
//
//  本层只负责「拿到 PS5 的码流与事件」：
//    · 设置持久化（ChiakiPS5Settings）
//    · 会话生命周期（chiaki_session_init/start/stop/join/fini）
//    · 事件回调（连接、PIN 请求、退出、注册信息）
//    · 视频样本回调（Annex-B 码流，交给上层用 VideoToolbox 解码）
//    · 手柄状态下发（chiaki_session_set_controller_state）
//
//  与虚拟桌面/合成画布的对接在 StreamingHost 侧完成。
//

import Foundation

// MARK: - 设置

/// PS5 连接参数，持久化在 UserDefaults
struct ChiakiPS5Settings: Codable {
    /// 是否在虚拟桌面里启用 PS5 入口
    var enabled = false
    /// 主机地址（局域网 IP 或主机名）
    var host = ""
    /// true = PS5，false = PS4
    var ps5 = true
    /// 16 字节 regist key 的 base64（注册后由主机下发）
    var registKey = ""
    /// 16 字节 morning 的 base64（注册后由主机下发）
    var morning = ""
    /// PSN Account-ID：8 字节原始值的 base64（PS5 / PS4 >= 7.0 注册必填）
    var psnAccountId = ""
    /// 首次注册用的 Remote Play PIN（主机上显示的 8 位数字）
    var pin = ""
    /// 可选的 Console PIN（4 位）
    var consolePin = ""
    /// 分辨率预设：ChiakiVideoResolutionPreset（1=360p 2=540p 3=720p 4=1080p）
    var resolutionPreset = 4
    /// 帧率（30 或 60）
    var fps = 60
    /// 码率（Kbps）。画面是直通转发，这个码率会原样占用「PS5→本机」和「本机→客户端」两段链路，
    /// 局域网 Wi-Fi 下 20Mbps 是画质与占用较均衡的起点（链路过载时会出现花屏，可适当下调）
    var bitrateKbps = 20000

    static let storageKey = "streaming.ps5.settings"

    static func load() -> ChiakiPS5Settings {
        guard let data = UserDefaults.standard.data(forKey: storageKey),
              let value = try? JSONDecoder().decode(ChiakiPS5Settings.self, from: data)
        else { return ChiakiPS5Settings() }
        return value
    }

    func save() {
        guard let data = try? JSONEncoder().encode(self) else { return }
        UserDefaults.standard.set(data, forKey: Self.storageKey)
    }

    /// 是否已经具备连接条件（启用 + 有地址 + 有注册信息）
    var isReady: Bool {
        enabled && !host.isEmpty && !registKey.isEmpty && !morning.isEmpty
    }
}

// MARK: - 事件与按键

/// 与 C 侧 ChiakiEventType 的枚举顺序一一对应（见 chiaki/session.h）
enum ChiakiPS5EventKind: Int32 {
    case connected = 0
    case loginPinRequest = 1
    case holepunch = 2
    case regist = 3
    case nicknameReceived = 4
    case keyboardOpen = 5
    case keyboardTextChange = 6
    case keyboardRemoteClose = 7
    case rumble = 8
    case quit = 9
    case triggerEffects = 10
    case motionReset = 11
    case ledColor = 12
    case playerIndex = 13
    case hapticIntensity = 14
    case triggerIntensity = 15
    case videoFecFailure = 16
}

/// 手柄按键位掩码，取值与 C 侧 ChiakiControllerButton 一致
struct ChiakiPS5Button: OptionSet {
    let rawValue: UInt32
    static let cross = ChiakiPS5Button(rawValue: 1 << 0)
    static let moon = ChiakiPS5Button(rawValue: 1 << 1)
    static let box = ChiakiPS5Button(rawValue: 1 << 2)
    static let pyramid = ChiakiPS5Button(rawValue: 1 << 3)
    static let dpadLeft = ChiakiPS5Button(rawValue: 1 << 4)
    static let dpadRight = ChiakiPS5Button(rawValue: 1 << 5)
    static let dpadUp = ChiakiPS5Button(rawValue: 1 << 6)
    static let dpadDown = ChiakiPS5Button(rawValue: 1 << 7)
    static let l1 = ChiakiPS5Button(rawValue: 1 << 8)
    static let r1 = ChiakiPS5Button(rawValue: 1 << 9)
    static let l3 = ChiakiPS5Button(rawValue: 1 << 10)
    static let r3 = ChiakiPS5Button(rawValue: 1 << 11)
    static let options = ChiakiPS5Button(rawValue: 1 << 12)
    static let share = ChiakiPS5Button(rawValue: 1 << 13)
    static let touchpad = ChiakiPS5Button(rawValue: 1 << 14)
    static let ps = ChiakiPS5Button(rawValue: 1 << 15)
    /// 模拟扳机（与上面的位不重叠）
    static let l2 = ChiakiPS5Button(rawValue: 1 << 16)
    static let r2 = ChiakiPS5Button(rawValue: 1 << 17)
}

// MARK: - Moonlight 手柄 → chiaki 手柄状态

extension ChiakiControllerState {
    /// 把 Moonlight 客户端的手柄状态转换为 chiaki 需要的控制器状态。
    /// 按键按物理位置对应：A=×、B=○、X=□、Y=△。
    static func fromMoonlight(_ pad: MLGamepadState) -> ChiakiControllerState {
        var state = ChiakiControllerState()
        chiaki_controller_state_set_idle(&state)

        var buttons: UInt32 = 0
        if pad.buttons & MLGamepadButton.a != 0 { buttons |= 1 << 0 }              // CROSS
        if pad.buttons & MLGamepadButton.b != 0 { buttons |= 1 << 1 }              // MOON
        if pad.buttons & MLGamepadButton.x != 0 { buttons |= 1 << 2 }              // BOX
        if pad.buttons & MLGamepadButton.y != 0 { buttons |= 1 << 3 }              // PYRAMID
        if pad.buttons & MLGamepadButton.dpadLeft != 0 { buttons |= 1 << 4 }       // DPAD_LEFT
        if pad.buttons & MLGamepadButton.dpadRight != 0 { buttons |= 1 << 5 }      // DPAD_RIGHT
        if pad.buttons & MLGamepadButton.dpadUp != 0 { buttons |= 1 << 6 }         // DPAD_UP
        if pad.buttons & MLGamepadButton.dpadDown != 0 { buttons |= 1 << 7 }       // DPAD_DOWN
        if pad.buttons & MLGamepadButton.leftBumper != 0 { buttons |= 1 << 8 }     // L1
        if pad.buttons & MLGamepadButton.rightBumper != 0 { buttons |= 1 << 9 }    // R1
        if pad.buttons & MLGamepadButton.leftStick != 0 { buttons |= 1 << 10 }     // L3
        if pad.buttons & MLGamepadButton.rightStick != 0 { buttons |= 1 << 11 }    // R3
        if pad.buttons & MLGamepadButton.start != 0 { buttons |= 1 << 12 }         // OPTIONS
        if pad.buttons & MLGamepadButton.back != 0 { buttons |= 1 << 13 }          // SHARE
        if pad.buttons & MLGamepadButton.misc != 0 { buttons |= 1 << 14 }          // TOUCHPAD
        if pad.buttons & MLGamepadButton.special != 0 { buttons |= 1 << 15 }       // PS

        state.buttons = buttons
        state.l2_state = pad.leftTrigger
        state.r2_state = pad.rightTrigger
        state.left_x = pad.leftStickX
        // Moonlight 协议的 Y 轴以「向上为正」，chiaki/SDL 以「向上为负」，需要取反
        state.left_y = flipped(pad.leftStickY)
        state.right_x = pad.rightStickX
        state.right_y = flipped(pad.rightStickY)
        return state
    }

    /// Int16 取反（避开 Int16.min 取反溢出）
    private static func flipped(_ value: Int16) -> Int16 {
        value == Int16.min ? Int16.max : -value
    }
}

// MARK: - 运行时初始化

/// libchiaki 的全局初始化。chiaki 的所有前端（GUI/Android/switch）都会在启动时调用
/// `chiaki_lib_init()`，它负责随机数种子、Reed-Solomon（FEC）的伽罗华域以及 AIA 初始化，
/// 不调用会导致视频 FEC 解码不可用。这里用 lazy static 保证只执行一次。
enum ChiakiRuntime {
    private static let initialized: Bool = {
        let result = chiaki_lib_init()
        if result != CHIAKI_ERR_SUCCESS {
            NSLog("[QuickRecorder/PS5] chiaki_lib_init 失败（\(result)）")
        }
        return result == CHIAKI_ERR_SUCCESS
    }()

    @discardableResult
    static func ensureInitialized() -> Bool { initialized }
}

// MARK: - 会话

/// PS5 串流会话。所有 chiaki 调用都在内部串行队列上执行。
final class ChiakiPS5Session {

    enum State: Equatable {
        case idle
        case connecting
        case streaming
        case failed(String)

        var text: String {
            switch self {
            case .idle: return "未连接"
            case .connecting: return "连接中"
            case .streaming: return "已连接"
            case .failed(let reason): return "失败：\(reason)"
            }
        }
    }

    /// 解码前的码流样本（Annex-B）：(数据, 丢失帧数, 是否由 FEC 恢复)
    var onVideoSample: ((Data, Int32, Bool) -> Void)?
    /// PS5 音频（Opus 编码）：(数据, 时长 ms)
    var onAudioFrame: ((Data, UInt32) -> Void)?
    /// 文本事件（连接、退出、PIN 请求等），用于日志与界面提示
    var onEvent: ((String) -> Void)?
    /// 注册成功后拿到注册信息（base64），由调用方保存进设置
    var onRegistKey: ((String, String) -> Void)?
    /// 需要用户输入 PIN
    var onPINRequest: ((Bool) -> Void)?
    var onStateChange: ((State) -> Void)?
    /// 注册流程结束：(是否成功, 提示)
    var onRegistrationFinished: ((Bool, String) -> Void)?

    private(set) var state: State = .idle {
        didSet { if state != oldValue { onStateChange?(state) } }
    }

    private var sessionPtr: UnsafeMutablePointer<ChiakiSession>?
    private var hostCString: UnsafeMutablePointer<CChar>?
    /// ChiakiLog 必须比会话存活更久，因此用属性持有（局部变量会在函数返回后失效）
    private var sessionLog = ChiakiLog()
    private let queue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.ps5.session")
    /// 日志文件写入串行队列与复用的文件句柄
    private let logQueue = DispatchQueue(label: "com.lihaoyun6.QuickRecorder.ps5.log")
    private var logHandle: FileHandle?

    // 注册
    private var registLog = ChiakiLog()
    private var registPtr: UnsafeMutablePointer<ChiakiRegist>?

    // 音频流参数（由 chiaki 的音频头回调填充）
    private var audioChannels: Int32 = 2
    private var audioSampleRate: Int32 = 48000
    private var audioFrameSize: Int32 = 480
    /// 已收到的音频帧数（用于首批帧的日志，排查「没有声音」）
    private var audioFrameCount = 0

    // MARK: 生命周期

    deinit {
        stop()
    }

    func start(settings: ChiakiPS5Settings) {
        queue.async { [weak self] in self?.startLocked(settings: settings) }
    }

    func stop() {
        queue.async { [weak self] in self?.stopLocked() }
    }

    private func startLocked(settings: ChiakiPS5Settings) {
        stopLocked()
        ChiakiRuntime.ensureInitialized()
        resetLogFile()

        guard !settings.host.isEmpty else {
            fail("未填写 PS5 地址")
            return
        }
        guard let keyData = Data(base64Encoded: settings.registKey), keyData.count == 16 else {
            fail("注册信息无效：regist key 需要 16 字节的 base64")
            return
        }
        guard let morningData = Data(base64Encoded: settings.morning), morningData.count == 16 else {
            fail("注册信息无效：morning 需要 16 字节的 base64")
            return
        }

        // 只开 ERROR / WARNING / INFO。VERBOSE(1<<3) 与 DEBUG(1<<4) 会输出大量十六进制 dump，
        // 串流期间会产生上百 MB 日志并拖慢 I/O
        let levelMask: UInt32 = (1 << 0) | (1 << 1) | (1 << 2)
        chiaki_log_init(&sessionLog, levelMask, { level, msg, user in
            guard let user, let msg else { return }
            let session = Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue()
            session.appendLog("chiaki[\(chiaki_log_level_char(level))] \(String(cString: msg))")
        }, Unmanaged.passUnretained(self).toOpaque())

        let ptr = UnsafeMutablePointer<ChiakiSession>.allocate(capacity: 1)
        ptr.initialize(to: ChiakiSession())
        sessionPtr = ptr
        audioFrameCount = 0

        var info = ChiakiConnectInfo()
        info.ps5 = settings.ps5
        // chiaki 在会话期间可能一直持有该指针，因此单独分配并保持到会话结束
        let hostPtr = strdup(settings.host)
        hostCString = hostPtr
        info.host = UnsafePointer(hostPtr)
        info.auto_regist = false
        info.enable_keyboard = false
        info.enable_dualsense = false
        info.video_profile_auto_downgrade = true
        info.packet_loss_max = 0.1
        info.enable_idr_on_fec_failure = true

        let keyOK = settings.registKey.withCString { QRChiakiSetRegistKeyB64(&info, $0) }
        let morningOK = settings.morning.withCString { QRChiakiSetMorningB64(&info, $0) }
        guard keyOK, morningOK else {
            fail("注册信息解析失败")
            return
        }
        // 注意 chiaki 的 video_profile.bitrate 单位是 kbps（launchspec 里是 bwKbpsSent），
        // 传 bps 会让主机收到一个大 1000 倍的带宽值，实测会被降成极低码率
        QRChiakiFillVideoProfile(&info.video_profile,
                                 Int32(settings.resolutionPreset),
                                 Int32(settings.fps == 30 ? 30 : 60),
                                 Int32(settings.bitrateKbps))

        // 必须在 chiaki_session_init 之后才能挂回调：
        // init 内部会 memset 整个 ChiakiSession（见 lib/src/session.c），
        // 提前设置事件/视频/音频回调会被清掉（表现为收不到事件、永远没有画面）。
        let initResult = chiaki_session_init(ptr, &info, &sessionLog)
        guard initResult == CHIAKI_ERR_SUCCESS else {
            fail("chiaki_session_init 失败（\(initResult)）")
            return
        }

        // 回调里不捕获任何变量，才能转换成 C 函数指针
        ptr.pointee.event_cb = { event, user in
            guard let event, let user else { return }
            let session = Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue()
            session.handleEvent(event)
        }
        ptr.pointee.event_cb_user = Unmanaged.passUnretained(self).toOpaque()
        ptr.pointee.video_sample_cb = { buf, size, framesLost, recovered, user in
            guard let buf, let user, size > 0 else { return false }
            let session = Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue()
            return session.handleVideoSample(buf: buf, size: size, framesLost: framesLost, recovered: recovered)
        }
        ptr.pointee.video_sample_cb_user = Unmanaged.passUnretained(self).toOpaque()

        // 音频 sink：chiaki 回调给出的是 Opus 编码数据，直接转发给 Moonlight 音频通道
        var sink = ChiakiAudioSink()
        sink.user = Unmanaged.passUnretained(self).toOpaque()
        sink.header_cb = { header, user in
            guard let header, let user else { return }
            Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue().handleAudioHeader(header)
        }
        sink.frame_cb = { buf, size, user in
            guard let buf, let user, size > 0 else { return }
            Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue().handleAudioFrame(buf: buf, size: size)
        }
        QRChiakiSetAudioSink(ptr, &sink)

        let startResult = chiaki_session_start(ptr)
        guard startResult == CHIAKI_ERR_SUCCESS else {
            fail("chiaki_session_start 失败（\(startResult)）")
            return
        }

        state = .connecting
        appendLog("开始连接 PS5 \(settings.host)（\(settings.ps5 ? "PS5" : "PS4")，\(settings.fps)fps，\(settings.bitrateKbps)Kbps）")
    }

    private func stopLocked() {
        guard let ptr = sessionPtr else { return }
        sessionPtr = nil
        appendLog("断开 PS5 会话")
        chiaki_session_stop(ptr)
        chiaki_session_join(ptr)
        chiaki_session_fini(ptr)
        ptr.deinitialize(count: 1)
        ptr.deallocate()
        if let hostCString {
            free(hostCString)
            self.hostCString = nil
        }
        state = .idle
    }

    private func fail(_ reason: String) {
        appendLog("PS5：\(reason)")
        state = .failed(reason)
        stopLocked()
    }

    // MARK: 回调用实现

    private func handleVideoSample(buf: UnsafeMutablePointer<UInt8>,
                                   size: Int,
                                   framesLost: Int32,
                                   recovered: Bool) -> Bool {
        let data = Data(bytes: buf, count: size)
        onVideoSample?(data, framesLost, recovered)
        return true
    }

    // MARK: 音频

    private func handleAudioHeader(_ header: UnsafePointer<ChiakiAudioHeader>) {
        var channels: Int32 = 0
        var rate: Int32 = 0
        var frameSize: Int32 = 0
        QRChiakiDescribeAudioHeader(header, &channels, &rate, &frameSize)
        if channels > 0 { audioChannels = channels }
        if rate > 0 { audioSampleRate = rate }
        if frameSize > 0 { audioFrameSize = frameSize }
        appendLog("PS5 音频：\(audioChannels) 声道 / \(audioSampleRate)Hz / 每帧 \(audioFrameSize) 样本")
    }

    private func handleAudioFrame(buf: UnsafeMutablePointer<UInt8>, size: Int) {
        let data = Data(bytes: buf, count: size)
        // Opus 帧时长 = 每帧样本数 / 采样率
        let durationMs = audioSampleRate > 0
            ? UInt32(max(1, (Double(audioFrameSize) / Double(audioSampleRate) * 1000).rounded()))
            : 10
        audioFrameCount += 1
        if audioFrameCount <= 3 {
            let head = data.prefix(8).map { String(format: "%02X", $0) }.joined(separator: " ")
            appendLog("音频 样本 #\(audioFrameCount)：\(size) 字节，时长 \(durationMs)ms，首字节 \(head)")
        }
        onAudioFrame?(data, durationMs)
    }

    private func handleEvent(_ event: UnsafePointer<ChiakiEvent>) {
        var info = QRChiakiEventInfo()
        QRChiakiDescribeEvent(event, &info)
        guard let kind = ChiakiPS5EventKind(rawValue: info.type) else { return }

        switch kind {
        case .connected:
            state = .streaming
            appendLog("PS5 已连接，开始接收画面")
        case .loginPinRequest:
            appendLog(info.pin_incorrect ? "PS5 要求重新输入 PIN（上次错误）" : "PS5 要求输入 PIN")
            onPINRequest?(info.pin_incorrect)
        case .nicknameReceived:
            appendLog("PS5 主机名称：\(chiakiString(info.nickname))")
        case .regist:
            let key = chiakiString(info.regist_key_b64)
            let morning = chiakiString(info.regist_morning_b64)
            if info.regist_filled, !key.isEmpty, !morning.isEmpty {
                appendLog("PS5 注册成功，已取得注册信息")
                onRegistKey?(key, morning)
            }
        case .quit:
            // reason 是 C 侧固定长度数组，已截断补 \0
            let reason = chiakiString(info.reason)
            let text = reason.isEmpty ? "未知原因" : reason
            appendLog("PS5 会话结束：\(text)")
            if info.is_error {
                state = .failed(text)
            } else {
                state = .idle
            }
        case .videoFecFailure:
            appendLog("PS5 视频丢包（已请求关键帧）")
        default:
            break
        }
    }

    // MARK: 手柄输入

    /// 下发一次完整的手柄状态
    func sendControllerState(_ state: ChiakiControllerState) {
        queue.async { [weak self] in
            guard let self, let ptr = self.sessionPtr else { return }
            var value = state
            chiaki_session_set_controller_state(ptr, &value)
        }
    }

    /// 输入 PIN（用于首次与主机建立信任）
    func sendPIN(_ pin: String) {
        queue.async { [weak self] in
            guard let self, let ptr = self.sessionPtr else { return }
            let bytes = Array(pin.utf8)
            bytes.withUnsafeBufferPointer { buffer in
                chiaki_session_set_login_pin(ptr, buffer.baseAddress, buffer.count)
            }
        }
    }

    /// 请求一个关键帧（画面异常时使用）
    func requestKeyFrame() {
        queue.async { [weak self] in
            guard let self, let ptr = self.sessionPtr else { return }
            chiaki_session_request_idr(ptr)
        }
    }

    // MARK: 注册（首次与主机配对）

    /// 开始注册：主机上会显示 PIN，在主机上确认后完成配对。
    /// PS5 需要 PSN Account-ID（8 字节原始值的 base64，可从 PSN 账号查询网站获取）。
    /// 成功后通过 `onRegistKey` 回调返回注册信息（base64），由调用方持久化。
    func startRegistration(host: String, ps5: Bool, psnAccountId: String, pin: String, consolePin: String) {
        queue.async { [weak self] in
            self?.startRegistrationLocked(host: host, ps5: ps5, psnAccountId: psnAccountId,
                                          pin: pin, consolePin: consolePin)
        }
    }

    func cancelRegistration() {
        queue.async { [weak self] in self?.stopRegistrationLocked() }
    }

    private func startRegistrationLocked(host: String, ps5: Bool, psnAccountId: String,
                                         pin: String, consolePin: String) {
        stopRegistrationLocked()
        ChiakiRuntime.ensureInitialized()

        let hostValue = host.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !hostValue.isEmpty else {
            onRegistrationFinished?(false, "未填写主机地址")
            return
        }
        let accountValue = psnAccountId.trimmingCharacters(in: .whitespacesAndNewlines)
        guard let accountData = Data(base64Encoded: accountValue), accountData.count == 8 else {
            let message = "PSN Account-ID 无效：必须是 8 字节原始值的 base64"
            appendLog(message)
            onRegistrationFinished?(false, message)
            return
        }
        let pinValue = UInt32(pin.trimmingCharacters(in: .whitespaces)) ?? 0
        guard pinValue > 0 else {
            let message = "未填写 Remote Play PIN（主机上显示的 8 位数字）"
            appendLog(message)
            onRegistrationFinished?(false, message)
            return
        }

        // 只开 ERROR / WARNING / INFO（同会话日志，避免海量 hexdump）
        let levelMask: UInt32 = (1 << 0) | (1 << 1) | (1 << 2)
        chiaki_log_init(&registLog, levelMask, { level, msg, user in
            guard let user, let msg else { return }
            let session = Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue()
            session.appendLog("chiaki[\(chiaki_log_level_char(level))] \(String(cString: msg))")
        }, Unmanaged.passUnretained(self).toOpaque())

        // chiaki 会在 regist_start 里持有 info 内的指针，因此这里的信息在调用返回前保持有效
        let hostPtr = strdup(hostValue)
        defer { free(hostPtr) }
        let accountPtr = strdup(accountValue)
        defer { free(accountPtr) }

        var info = ChiakiRegistInfo()
        let consolePinValue = UInt32(consolePin.trimmingCharacters(in: .whitespaces)) ?? 0
        guard QRChiakiFillRegistInfo(&info, hostPtr, ps5, accountPtr, pinValue, consolePinValue) else {
            let message = "注册信息不完整：PSN Account-ID 需要 8 字节的 base64"
            appendLog(message)
            onRegistrationFinished?(false, message)
            return
        }

        let ptr = UnsafeMutablePointer<ChiakiRegist>.allocate(capacity: 1)
        ptr.initialize(to: ChiakiRegist())
        registPtr = ptr

        let err = QRChiakiRegistStart(ptr, &registLog, &info, { event, user in
            guard let event, let user else { return }
            Unmanaged<ChiakiPS5Session>.fromOpaque(user).takeUnretainedValue().handleRegistEvent(event)
        }, Unmanaged.passUnretained(self).toOpaque())

        if err == CHIAKI_ERR_SUCCESS {
            appendLog("开始注册 \(hostValue)，请在主机上确认 PIN")
        } else {
            appendLog("注册启动失败（\(err)）")
            onRegistrationFinished?(false, "注册启动失败（\(err)）")
            stopRegistrationLocked()
        }
    }

    private func handleRegistEvent(_ event: UnsafeMutablePointer<ChiakiRegistEvent>) {
        var type: Int32 = -1
        var nickname = [CChar](repeating: 0, count: 64)
        var key = [CChar](repeating: 0, count: 128)
        var morning = [CChar](repeating: 0, count: 64)
        let success = nickname.withUnsafeMutableBufferPointer { np in
            key.withUnsafeMutableBufferPointer { kp in
                morning.withUnsafeMutableBufferPointer { mp in
                    QRChiakiRegistDescribe(event, &type, np.baseAddress, np.count,
                                           kp.baseAddress, kp.count, mp.baseAddress, mp.count)
                }
            }
        }

        if success {
            let nicknameStr = String(cString: nickname)
            let keyStr = String(cString: key)
            let morningStr = String(cString: morning)
            appendLog("注册成功：\(nicknameStr)")
            onRegistKey?(keyStr, morningStr)
            onRegistrationFinished?(true, "注册成功：\(nicknameStr)")
        } else if type == 1 { // CHIAKI_REGIST_EVENT_TYPE_FINISHED_FAILED
            appendLog("注册失败")
            onRegistrationFinished?(false, "注册失败，请确认主机地址与 PIN")
        } else { // FINISHED_CANCELED
            appendLog("注册已取消")
            onRegistrationFinished?(false, "注册已取消")
        }
        // 回调运行在注册线程上，直接 fini 会 join 当前线程而死锁，交给内部队列处理
        queue.async { [weak self] in self?.stopRegistrationLocked() }
    }

    private func stopRegistrationLocked() {
        guard let ptr = registPtr else { return }
        registPtr = nil
        chiaki_regist_stop(ptr)
        chiaki_regist_fini(ptr)
        ptr.deinitialize(count: 1)
        ptr.deallocate()
    }

    // MARK: 日志

    /// 供上层（视频管线）把诊断写入 PS5 日志，并同步到主机日志/界面
    func report(_ message: String) {
        onEvent?(message)
        writeLogFile(message)
    }

    /// chiaki 自己的日志：只落盘，不回灌主机日志/界面。
    /// 这些行量很大（每秒几十行），回灌会不断触发主线程 UI 刷新并拖慢 chiaki 的收发线程，
    /// 反而造成丢包。
    private func appendLog(_ message: String) {
        writeLogFile(message)
    }

    private func writeLogFile(_ message: String) {
        let stamp = DateFormatter.localizedString(from: Date(), dateStyle: .none, timeStyle: .medium)
        guard let data = "[\(stamp)] \(message)\n".data(using: .utf8) else { return }
        // 日志来自多个线程（chiaki 回调、会话队列），统一串行写入；
        // 句柄复用，避免每行都 open/close 一次文件
        logQueue.async { [weak self] in
            guard let self else { return }
            if self.logHandle == nil {
                guard let url = Self.logFileURL() else { return }
                FileManager.default.createFile(atPath: url.path, contents: nil)
                self.logHandle = try? FileHandle(forWritingTo: url)
                _ = try? self.logHandle?.seekToEnd()
            }
            try? self.logHandle?.write(contentsOf: data)
        }
    }

    /// 每次新会话/新注册时重建日志文件，避免无限增长
    private func resetLogFile() {
        logQueue.async { [weak self] in
            guard let self else { return }
            try? self.logHandle?.close()
            self.logHandle = nil
            if let url = Self.logFileURL() {
                try? FileManager.default.removeItem(at: url)
            }
        }
    }

    private static func logFileURL() -> URL? {
        guard let base = try? FileManager.default.url(for: .applicationSupportDirectory,
                                                      in: .userDomainMask,
                                                      appropriateFor: nil,
                                                      create: true) else { return nil }
        let dir = base.appendingPathComponent("sunGame/Streaming", isDirectory: true)
        try? FileManager.default.createDirectory(at: dir, withIntermediateDirectories: true)
        return dir.appendingPathComponent("ps5.log")
    }

    private func chiakiString<T>(_ value: T) -> String {
        withUnsafeBytes(of: value) { raw -> String in
            let bytes = raw.prefix { $0 != 0 }
            return String(decoding: bytes, as: UTF8.self)
        }
    }
}
