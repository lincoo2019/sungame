//
//  ChiakiShim.h
//  QuickRecorder
//
//  C 垫片：chiaki 的 ChiakiEvent 使用匿名 union、若干接口使用 C 枚举，
//  这两类在 Swift 侧访问都容易踩坑。这里统一收口成「普通结构体 + int」的形式。
//

#ifndef CHIAKI_SHIM_H
#define CHIAKI_SHIM_H

#include <stdbool.h>
#include <stddef.h>

#include <chiaki/common.h>
#include <chiaki/session.h>

#ifdef __cplusplus
extern "C" {
#endif

/// 事件摘要：与 ChiakiEventType 的枚举顺序一一对应（见 ChiakiPS5.swift 里的映射）
typedef struct qr_chiaki_event_info_t
{
	int32_t type;						///< ChiakiEventType
	bool is_error;						///< QUIT 且属于错误
	bool pin_incorrect;					///< LOGIN_PIN_REQUEST 且上次 PIN 错误
	char reason[256];					///< QUIT 的原因字符串
	char nickname[64];					///< NICKNAME_RECEIVED 的服务端昵称
	bool regist_filled;					///< REGIST 且拿到了注册信息
	char regist_host[128];				///< REGIST：主机地址
	char regist_key_b64[64];			///< REGIST：regist key（base64）
	char regist_morning_b64[64];		///< REGIST：morning（base64）
} QRChiakiEventInfo;

/// 提取事件摘要
void QRChiakiDescribeEvent(const ChiakiEvent *event, QRChiakiEventInfo *out);

/// 填充视频协商参数（内部调用 chiaki_connect_video_profile_preset，
/// resolution_preset/fps_preset 直接传 ChiakiVideoResolutionPreset / ChiakiVideoFPSPreset 的整数值）
/// bitrate 单位是 kbps（与 ChiakiConnectVideoProfile.bitrate / launchspec 的 bwKbpsSent 一致）
void QRChiakiFillVideoProfile(ChiakiConnectVideoProfile *profile, int resolution_preset, int fps_preset, int bitrate);

/// 写入 16 字节的 regist key / morning（src 需至少 16 字节）
void QRChiakiSetRegistKey(ChiakiConnectInfo *info, const uint8_t *src);
void QRChiakiSetMorning(ChiakiConnectInfo *info, const uint8_t *src);

/// 把 base64 文本解码进 info 的 regist_key / morning 字段；成功返回 true
bool QRChiakiSetRegistKeyB64(ChiakiConnectInfo *info, const char *b64);
bool QRChiakiSetMorningB64(ChiakiConnectInfo *info, const char *b64);

/// 读取 ChiakiControllerState 的按钮位掩码（等价于 state->buttons）
uint32_t QRChiakiControllerButtons(const ChiakiControllerState *state);

/// 安装音频 sink（chiaki_session_set_audio_sink 是 static inline，Swift 无法直接调用）
void QRChiakiSetAudioSink(ChiakiSession *session, ChiakiAudioSink *sink);

/// 提取音频头（采样率/声道/每帧样本数）
void QRChiakiDescribeAudioHeader(const ChiakiAudioHeader *header, int32_t *channels, int32_t *rate, int32_t *frame_size);

/// 发起主机注册（PS5 首次配对）
ChiakiErrorCode QRChiakiRegistStart(ChiakiRegist *regist, ChiakiLog *log, const ChiakiRegistInfo *info,
                                     ChiakiRegistCb cb, void *cb_user);

/// 填充注册信息。
/// PS5（以及 PS4 >= 7.0）必须提供 PSN Account-ID：`account_id_b64` 为 8 字节原始值的 base64。
/// `pin` 是主机上显示的 8 位 Remote Play PIN（参与注册加密），`console_pin` 为可选的 4 位。
/// 返回 false 表示参数不合法（通常是 account id 不是 8 字节的 base64）。
bool QRChiakiFillRegistInfo(ChiakiRegistInfo *info, const char *host, bool ps5, const char *account_id_b64,
                             uint32_t pin, uint32_t console_pin);

/// 提取注册事件：type 写回事件类型，成功时把主机信息以 base64 写入各缓冲区；
/// 返回 true 表示注册成功且主机信息可用
bool QRChiakiRegistDescribe(const ChiakiRegistEvent *event, int32_t *type,
                             char *nickname, size_t nickname_len,
                             char *regist_key_b64, size_t regist_key_len,
                             char *morning_b64, size_t morning_len);

#ifdef __cplusplus
}
#endif

#endif /* CHIAKI_SHIM_H */
