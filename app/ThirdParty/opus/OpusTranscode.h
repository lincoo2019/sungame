//
//  OpusTranscode.h
//  QuickRecorder
//
//  把 PS5 直通过来的 Opus 帧重打包成 Moonlight 客户端协商的帧长。
//
//  背景：客户端按协商时长（立体声默认 5ms）固定分配解码缓冲，并固定向
//  opus 索要 48 * duration 个样本（moonlight-ios/Connection.m 的
//  ArDecodeAndPlaySample）。而 PS5 固定送 10ms（480 样本）帧，直接转发会让
//  opus_multistream_decode 返回 OPUS_BUFFER_TOO_SMALL，客户端 `decodeLen > 0`
//  不成立 → 一帧都不播（表现为完全静音）。这里用 libopus 解码成 PCM 再按目标
//  帧长重新编码，只作用于 ~64kbps 的音频，与视频直通互不影响。
//

#ifndef OpusTranscode_h
#define OpusTranscode_h

#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

typedef struct QROpusTranscoder QROpusTranscoder;

/// 创建转码器。in_samples 为输入帧每声道样本数，out_samples 为目标帧每声道样本数。
/// 失败返回 NULL。
QROpusTranscoder *QROpusTranscoderCreate(int sample_rate, int channels,
                                         int in_samples, int out_samples);

void QROpusTranscoderDestroy(QROpusTranscoder *transcoder);

/// 送入一个 Opus 包，产出若干目标帧长的 Opus 包。
/// out[] / out_len[] 指向转码器内部缓冲，内容在下次调用前有效。
/// 返回实际产出的包数（0 表示失败或没有产出）。
int QROpusTranscoderProcess(QROpusTranscoder *transcoder,
                            const unsigned char *in, int in_len,
                            unsigned char **out, int *out_len, int max_out);

/// 一个输入帧最多会产出多少个输出帧（用于上层分配数组）
int QROpusTranscoderMaxOutputCount(const QROpusTranscoder *transcoder);

//==================================================================================================
// PCM(Int16 interleaved) → Opus 编码器
//
//  用途：游戏模式下宿主自己从进程音频捕获得到 PCM，需要编码成 Opus 再交给 Moonlight 音频通道。
//  与上面的 QROpusTranscoder 是两条独立路径（那条做 Opus→Opus 重打包），互不影响。
//==================================================================================================

typedef struct QROpusPCMEncoder QROpusPCMEncoder;

/// 创建编码器。frame_samples 为每声道每帧样本数（如 5ms@48k = 240）；
/// bitrate_bps <= 0 时取 64000。失败返回 NULL。
QROpusPCMEncoder *QROpusPCMEncoderCreate(int sample_rate, int channels,
                                         int frame_samples, int bitrate_bps);

void QROpusPCMEncoderDestroy(QROpusPCMEncoder *encoder);

/// 编码一帧。pcm 必须含恰好 frame_samples * channels 个 Int16（interleaved）。
/// 成功返回 >0：Opus 包字节数，*out 指向编码器内部缓冲（下次调用前有效）；失败返回 <=0。
int QROpusPCMEncoderEncode(QROpusPCMEncoder *encoder, const int16_t *pcm,
                           unsigned char **out);

/// 视频 RS 校验（与 nanors reed_solomon_encode 等价，GF(2^8) 多项式 0x11D，
/// 矩阵 P[j][i] = INV[(ps+i)^j]）。data[i] 长度 lens[i]（不足 bs 视为补零），parity 为 ps 个 bs 字节输出缓冲。
/// 放在 C 里是因为 Swift Debug(-Onone) 下 238KB 的 IDR 要算 600+ms，把视频发送线程卡住。
/// 诊断用：本地 Opus 解码器（与客户端一样解成 Int16 交织 PCM），返回每声道样本数，<0 为错误。
typedef struct OpaqueQROpusDecoder QROpusDecoder;
QROpusDecoder *QROpusDecoderCreate(int sample_rate, int channels);
int QROpusDecoderDecode(QROpusDecoder *decoder, const uint8_t *packet, int length, int16_t *pcm, int max_frames);
void QROpusDecoderDestroy(QROpusDecoder *decoder);

void QRVideoFECEncode(const uint8_t *const *data, const int *lens, int ds,
                      uint8_t *const *parity, int ps, int bs);

#ifdef __cplusplus
}
#endif

#endif /* OpusTranscode_h */
