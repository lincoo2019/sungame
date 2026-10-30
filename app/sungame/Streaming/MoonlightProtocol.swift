//
//  MoonlightProtocol.swift
//  QuickRecorder
//
//  Sunshine / Moonlight（GameStream）协议常量与字节工具。
//  规格来源：moonlight-common-c（src/Video.h、src/Input.h、src/Limelight.h、
//  src/Limelight-internal.h、src/ControlStream.c）与 Sunshine 服务端
//  （src/stream.cpp、src/rtsp.cpp、src/nvhttp.cpp）。
//

import Foundation

// MARK: - 端口

enum MLPort {
    /// Sunshine 基准端口（所有端口 = 基准 + offset）
    static let base: Int = 47989

    /// GameStream HTTP（配对、serverinfo）
    static let http = base + 0
    /// GameStream HTTPS（serverinfo、applist、launch、resume、cancel）
    static let https = base - 5
    /// 视频 RTP（UDP）
    static let video = base + 9
    /// 控制通道（UDP，ENet）
    static let control = base + 10
    /// 音频 RTP（UDP）
    static let audio = base + 11
    /// RTSP（TCP）
    static let rtsp = base + 21
}

// MARK: - 应用 ID

/// /applist 里暴露给 Moonlight 的应用条目，也是 /launch 的 appid
enum MLAppID {
    /// PS5（直接进入 PS5 画面）
    static let ps5 = "2"
    /// 游戏（直接进入无窗口运行的 Godot 游戏画面）
    static let game = "3"
}

/// 应用列表里显示的标题
enum MLAppTitle {
    static let ps5 = "PS5"
    static let game = "植物大战僵尸杂交版"
}

// MARK: - 版本标识

enum MLVersion {
    /// 必须让第 4 段为负数，客户端据此判定对端为 Sunshine（启用 x-ss-* 扩展与多通道）
    static let appVersion = "7.1.431.-1"
    static let gfeVersion = "3.23.0.74"
    /// 客户端据此决定是否发送 X-GS-ClientVersion
    static let rtspClientVersion = 14
}

// MARK: - 能力位

/// ServerCodecModeSupport
enum MLCodecMode {
    static let h264: UInt32 = 0x0000_0001
    static let hevc: UInt32 = 0x0000_0100
    static let hevcMain10: UInt32 = 0x0000_0200
    static let av1Main8: UInt32 = 0x0001_0000
    static let h264High8_444: UInt32 = 0x0004_0000
    static let hevcRext8_444: UInt32 = 0x0008_0000
    static let hevcRext10_444: UInt32 = 0x0010_0000
}

/// x-ss-general.encryptionSupported / Requested / Enabled
enum MLEncryption {
    static let controlV2: UInt32 = 0x01
    static let video: UInt32 = 0x02
    static let audio: UInt32 = 0x04
}

/// x-ml-general.featureFlags（客户端 → 主机）
enum MLFeatureFlags {
    static let fecStatus: UInt32 = 0x01
    /// 客户端支持 X-SS-Ping-Payload / X-SS-Connect-Data
    static let sessionIDv1: UInt32 = 0x02
}

/// x-nv-general.featureFlags（客户端 → 主机，旧式能力协商）
enum MLNVFeatureFlags {
    /// 客户端请求音频加密（NVFF_AUDIO_ENCRYPTION）。Moonlight iOS 的 encryptionFlags=ENCFLG_ALL
    /// 会无条件置此位，因此主机必须对音频负载做 AES-128-CBC 加密，否则客户端解密失败、完全静音。
    static let audioEncryption: UInt32 = 0x20
}

/// x-ss-general.featureFlags（主机 → 客户端），必须与 moonlight 的 LI_FF_* 对齐
enum MLHostFeatureFlags {
    static let penTouchEvents: UInt32 = 0x01
    static let controllerTouchEvents: UInt32 = 0x02
}

// MARK: - 控制通道类型（packetTypesGen7Enc，加密控制流）

enum MLControlType {
    static let requestIDRFrame: UInt16 = 0x0302
    static let startB: UInt16 = 0x0307
    static let invalidateRefFrames: UInt16 = 0x0301
    static let lossStats: UInt16 = 0x0201
    static let frameStats: UInt16 = 0x0204
    static let inputData: UInt16 = 0x0206
    static let rumbleData: UInt16 = 0x010b
    static let termination: UInt16 = 0x0109
    static let hdrInfo: UInt16 = 0x010e
    /// 加密信封的外层类型
    static let encrypted: UInt16 = 0x0001
}

/// 客户端起始的兼容类型（Sunshine 会同时接受）
enum MLControlTypeCompat {
    static let startA: UInt16 = 0x0305
    static let periodicPing: UInt16 = 0x0200
}

// MARK: - ENet 通道

enum MLChannel {
    static let generic: UInt8 = 0x00
    static let urgent: UInt8 = 0x01
    static let keyboard: UInt8 = 0x02
    static let mouse: UInt8 = 0x03
    static let pen: UInt8 = 0x04
    static let touch: UInt8 = 0x05
    static let utf8: UInt8 = 0x06
    static let gamepadBase: UInt8 = 0x10
    static let sensorBase: UInt8 = 0x20
    /// 客户端以 48 个通道建链
    static let count: Int = 0x30
}

// MARK: - 输入包 magic

enum MLInputMagic {
    static let keyDown: UInt32 = 0x0000_0003
    static let keyUp: UInt32 = 0x0000_0004
    static let mouseMoveAbs: UInt32 = 0x0000_0005
    static let mouseMoveRel: UInt32 = 0x0000_0007
    static let mouseButtonDown: UInt32 = 0x0000_0008
    static let mouseButtonUp: UInt32 = 0x0000_0009
    static let scroll: UInt32 = 0x0000_000A
    static let multiController: UInt32 = 0x0000_000C
    static let hapticsEnable: UInt32 = 0x0000_000D
    static let utf8Text: UInt32 = 0x0000_0017
    static let hscroll: UInt32 = 0x5500_0001
    static let touch: UInt32 = 0x5500_0002
    static let pen: UInt32 = 0x5500_0003
    static let controllerArrival: UInt32 = 0x5500_0004
    static let controllerTouch: UInt32 = 0x5500_0005
    static let controllerMotion: UInt32 = 0x5500_0006
    static let controllerBattery: UInt32 = 0x5500_0007
}

/// 鼠标按键（NV_MOUSE_BUTTON_PACKET.button）
enum MLMouseButton: UInt8 {
    case left = 0x01
    case middle = 0x02
    case right = 0x03
    case x1 = 0x04
    case x2 = 0x05
}

/// 键盘修饰键位（NV_KEYBOARD_PACKET.modifiers）
enum MLModifier {
    static let shift: UInt8 = 0x01
    static let ctrl: UInt8 = 0x02
    static let alt: UInt8 = 0x04
    static let meta: UInt8 = 0x08
}

// MARK: - 视频 RTP

enum MLVideo {
    /// RTP 首字节：版本 2 + 扩展位（客户端据此把 RTP 头视为 16 字节）
    static let rtpHeaderByte: UInt8 = 0x80 | 0x10
    static let fixedRTPHeaderSize = 12
    static let maxRTPHeaderSize = 16
    /// FEC 块内数据分片数的位宽上限（fecInfo 中占 10 位）
    static let maxDataShardsPerBlock = 1023
    /// multiFecBlocks 中块号占 2 位，故最多 4 块
    static let maxFecBlocks = 4
    /// 视频 FEC 百分比（Sunshine 默认 20）。QR_GAMESTREAM_VIDEO_FEC=0 可关闭做对照。
    static let fecPercentage: Int = {
        let v = Int(ProcessInfo.processInfo.environment["QR_GAMESTREAM_VIDEO_FEC"] ?? "") ?? 20
        return min(max(v, 0), 255)
    }()
    /// 每块最少校验分片数（Moonlight 默认请求 minRequiredFecPackets=2）
    static let minRequiredFecPackets = 2

    static let flagContainsPicData: UInt8 = 0x01
    static let flagEOF: UInt8 = 0x02
    static let flagSOF: UInt8 = 0x04

    static let frameTypePFrame: UInt8 = 1
    static let frameTypeIDR: UInt8 = 2
    static let frameTypeIntraRefresh: UInt8 = 4
    static let frameTypePFrameAfterRFI: UInt8 = 5

    /// 8 字节短帧头
    static let shortFrameHeaderSize = 8
    static let shortFrameHeaderType: UInt8 = 0x01
}

// MARK: - 音频 RTP

enum MLAudio {
    static let payloadTypeAudio: UInt8 = 97
    static let payloadTypeFEC: UInt8 = 127
    static let rtpHeaderByte: UInt8 = 0x80
    static let sampleRate = 48000
    static let channels = 2
    /// Opus 立体声的 surround-params。
    ///
    /// 线格式（对齐 Sunshine rtsp.cpp）：
    ///   `<channelCount><streams><coupledStreams><mapping 各位>`  —— **每个数字一位字符，无分隔符**
    /// 立体声 = channels 2 / streams 1 / coupled 1 / mapping {0,1} → `"21101"`。
    /// 注意：不能写成 `"21201,2"`——那会表达 coupledStreams=2 > streams=1（非法组合，
    /// 客户端 `opus_multistream_decoder_create` 会直接返回 NULL 导致完全无声）。
    static let stereoSurroundParams = "21101"

    /// 音频 FEC 的 4+2 Reed-Solomon 校验矩阵（行优先，2 行 × 4 列）。
    ///
    /// **不能改回 nanors 的公式式 `INV[(ps+i)^j]`**（那会得到 {0x8e,0xf4,0x47,0xa7,...}）。
    /// 客户端 moonlight-common-c 在 `RtpAudioQueue.c:54-60` 建好 RS 上下文后会把这 8 个字节
    /// **覆盖**进去（注释：nanors 算出的矩阵和 NVIDIA 用的不一致，OpenFEC 那个才是对的），
    /// 参考宿主 Sunshine `stream.cpp:1863-1864` 做的是同一件事。因此这 8 字节才是链路上真正
    /// 通用的矩阵；用公式矩阵发 FEC，客户端解出来的分片全是乱码 → 持续爆裂声。
    /// 已用 moonlight-common-c 自带的真实 nanors 库实测：公式矩阵的校验块解不回来，
    /// 这 8 字节的校验块能精确恢复。
    static let openFecParityMatrix: [UInt8] = [0x77, 0x40, 0x38, 0x0e, 0xc7, 0xa7, 0x0d, 0x6c]
}

// MARK: - 字节工具

@inline(__always)
func mlBE16(_ v: UInt16) -> UInt16 { v.byteSwapped }
@inline(__always)
func mlBE32(_ v: UInt32) -> UInt32 { v.byteSwapped }

extension Array where Element == UInt8 {
    @inline(__always)
    func mlU8(_ offset: Int) -> UInt8 { self[offset] }

    @inline(__always)
    func mlBE16(_ offset: Int) -> UInt16 {
        (UInt16(self[offset]) << 8) | UInt16(self[offset + 1])
    }

    @inline(__always)
    func mlLE16(_ offset: Int) -> UInt16 {
        UInt16(self[offset]) | (UInt16(self[offset + 1]) << 8)
    }

    @inline(__always)
    func mlBE32(_ offset: Int) -> UInt32 {
        (UInt32(self[offset]) << 24) | (UInt32(self[offset + 1]) << 16)
            | (UInt32(self[offset + 2]) << 8) | UInt32(self[offset + 3])
    }

    @inline(__always)
    func mlLE32(_ offset: Int) -> UInt32 {
        UInt32(self[offset]) | (UInt32(self[offset + 1]) << 8)
            | (UInt32(self[offset + 2]) << 16) | (UInt32(self[offset + 3]) << 24)
    }

    /// 读取小端 float（netfloat）
    @inline(__always)
    func mlLEFloat(_ offset: Int) -> Float {
        Float(bitPattern: mlLE32(offset))
    }

    mutating func mlAppendBE16(_ v: UInt16) {
        append(UInt8(v >> 8)); append(UInt8(v & 0xFF))
    }

    mutating func mlAppendLE16(_ v: UInt16) {
        append(UInt8(v & 0xFF)); append(UInt8(v >> 8))
    }

    mutating func mlAppendBE32(_ v: UInt32) {
        append(UInt8((v >> 24) & 0xFF)); append(UInt8((v >> 16) & 0xFF))
        append(UInt8((v >> 8) & 0xFF)); append(UInt8(v & 0xFF))
    }

    mutating func mlAppendLE32(_ v: UInt32) {
        append(UInt8(v & 0xFF)); append(UInt8((v >> 8) & 0xFF))
        append(UInt8((v >> 16) & 0xFF)); append(UInt8((v >> 24) & 0xFF))
    }
}

extension Data {
    mutating func mlAppendBE16(_ v: UInt16) {
        append(UInt8(v >> 8)); append(UInt8(v & 0xFF))
    }
    mutating func mlAppendLE16(_ v: UInt16) {
        append(UInt8(v & 0xFF)); append(UInt8(v >> 8))
    }
    mutating func mlAppendBE32(_ v: UInt32) {
        append(UInt8((v >> 24) & 0xFF)); append(UInt8((v >> 16) & 0xFF))
        append(UInt8((v >> 8) & 0xFF)); append(UInt8(v & 0xFF))
    }
    mutating func mlAppendLE32(_ v: UInt32) {
        append(UInt8(v & 0xFF)); append(UInt8((v >> 8) & 0xFF))
        append(UInt8((v >> 16) & 0xFF)); append(UInt8((v >> 24) & 0xFF))
    }
    /// 追加固定长度字符串（不足补 0）
    mutating func mlAppendFixedString(_ s: String, length: Int) {
        var bytes = Array(s.utf8.prefix(length))
        append(contentsOf: bytes)
        if bytes.count < length { append(contentsOf: [UInt8](repeating: 0, count: length - bytes.count)) }
        bytes.removeAll()
    }
}

extension String {
    /// hex 字符串 → 字节（标准顺序，byte0 在前）
    func mlHexToBytes() -> [UInt8]? {
        let chars = Array(utf8)
        guard chars.count % 2 == 0 else { return nil }
        var out = [UInt8]()
        out.reserveCapacity(chars.count / 2)
        var i = 0
        while i < chars.count {
            guard let hi = String(bytes: [chars[i]], encoding: .utf8).flatMap({ UInt8($0, radix: 16) }),
                  let lo = String(bytes: [chars[i + 1]], encoding: .utf8).flatMap({ UInt8($0, radix: 16) })
            else { return nil }
            out.append(hi << 4 | lo)
            i += 2
        }
        return out
    }
}

extension Array where Element == UInt8 {
    func mlHexString(uppercase: Bool = true) -> String {
        map { String(format: uppercase ? "%02X" : "%02x", $0) }.joined()
    }
}
