//
//  OpusTranscode.c
//  QuickRecorder
//
//  实现见 OpusTranscode.h 顶部的说明。
//

#include "OpusTranscode.h"
#include <opus/opus.h>
#include <stdlib.h>
#include <string.h>

#define QR_OPUS_MAX_CHANNELS 2
/// 120ms @48kHz —— Opus 单帧上限
#define QR_OPUS_MAX_SAMPLES 5760
/// 单个输出包的上限（5ms@64kbps 只有几十字节，留足余量）
#define QR_OPUS_OUT_BUF_SIZE 1500
#define QR_OPUS_MAX_OUT 8

struct QROpusTranscoder {
	OpusDecoder *decoder;
	OpusEncoder *encoder;
	int channels;
	int in_samples;
	int out_samples;
	int max_out;
	unsigned char *out_bufs[QR_OPUS_MAX_OUT];
	int out_lengths[QR_OPUS_MAX_OUT];
	int16_t pcm[QR_OPUS_MAX_SAMPLES * QR_OPUS_MAX_CHANNELS];
};

QROpusTranscoder *QROpusTranscoderCreate(int sample_rate, int channels,
										 int in_samples, int out_samples)
{
	if(channels < 1 || channels > QR_OPUS_MAX_CHANNELS)
		return NULL;
	if(in_samples <= 0 || out_samples <= 0)
		return NULL;
	if(in_samples > QR_OPUS_MAX_SAMPLES || out_samples > QR_OPUS_MAX_SAMPLES)
		return NULL;

	int max_out = (in_samples + out_samples - 1) / out_samples;
	if(max_out < 1 || max_out > QR_OPUS_MAX_OUT)
		return NULL;

	QROpusTranscoder *t = calloc(1, sizeof(QROpusTranscoder));
	if(!t)
		return NULL;

	t->channels = channels;
	t->in_samples = in_samples;
	t->out_samples = out_samples;
	t->max_out = max_out;

	int err = OPUS_OK;
	t->decoder = opus_decoder_create(sample_rate, channels, &err);
	if(!t->decoder || err != OPUS_OK)
	{
		QROpusTranscoderDestroy(t);
		return NULL;
	}

	t->encoder = opus_encoder_create(sample_rate, channels, OPUS_APPLICATION_AUDIO, &err);
	if(!t->encoder || err != OPUS_OK)
	{
		QROpusTranscoderDestroy(t);
		return NULL;
	}

	// 与 PS5 音频的码率相近即可；复杂度取中档，100 帧/秒下 CPU 开销可忽略。
	// 必须关掉 VBR：客户端（RtpAudioQueue）要求同一个 FEC 块内所有包长度一致，
	// 否则会打印 "Audio FEC has been disabled due to an incompatibility with your
	// host's old software!" 并切到降级路径。Sunshine 同样是 OPUS_SET_VBR(0)。
	opus_encoder_ctl(t->encoder, OPUS_SET_BITRATE(64000));
	opus_encoder_ctl(t->encoder, OPUS_SET_VBR(0));
	opus_encoder_ctl(t->encoder, OPUS_SET_COMPLEXITY(5));

	for(int i = 0; i < max_out; i++)
	{
		t->out_bufs[i] = malloc(QR_OPUS_OUT_BUF_SIZE);
		if(!t->out_bufs[i])
		{
			QROpusTranscoderDestroy(t);
			return NULL;
		}
	}

	return t;
}

void QROpusTranscoderDestroy(QROpusTranscoder *t)
{
	if(!t)
		return;
	if(t->decoder)
		opus_decoder_destroy(t->decoder);
	if(t->encoder)
		opus_encoder_destroy(t->encoder);
	for(int i = 0; i < QR_OPUS_MAX_OUT; i++)
		free(t->out_bufs[i]);
	free(t);
}

int QROpusTranscoderMaxOutputCount(const QROpusTranscoder *t)
{
	return t ? t->max_out : 0;
}

int QROpusTranscoderProcess(QROpusTranscoder *t,
							const unsigned char *in, int in_len,
							unsigned char **out, int *out_len, int max_out)
{
	if(!t || !in || in_len <= 0 || !out || !out_len)
		return 0;

	// 允许一次解码到 120ms（5760 样本），足够容纳任何 PS5 帧长
	int decoded = opus_decode(t->decoder, in, in_len, t->pcm, QR_OPUS_MAX_SAMPLES, 0);
	if(decoded <= 0)
		return 0;

	if(max_out > t->max_out)
		max_out = t->max_out;

	int count = 0;
	int offset = 0;
	while(offset < decoded && count < max_out)
	{
		int chunk = decoded - offset;
		if(chunk > t->out_samples)
			chunk = t->out_samples;

		int len = opus_encode(t->encoder,
							  t->pcm + (size_t)offset * (size_t)t->channels,
							  chunk,
							  t->out_bufs[count], QR_OPUS_OUT_BUF_SIZE);
		if(len <= 0)
			break;

		out[count] = t->out_bufs[count];
		out_len[count] = len;
		count++;
		offset += chunk;
	}

	return count;
}

//==================================================================================================
// PCM(Int16) → Opus 编码器
//==================================================================================================

struct QROpusPCMEncoder {
	OpusEncoder *encoder;
	int channels;
	int frame_samples;
	unsigned char *out_buf;
};

QROpusPCMEncoder *QROpusPCMEncoderCreate(int sample_rate, int channels,
										 int frame_samples, int bitrate_bps)
{
	if(sample_rate <= 0 || frame_samples <= 0)
		return NULL;
	if(channels < 1 || channels > QR_OPUS_MAX_CHANNELS)
		return NULL;
	if(frame_samples > QR_OPUS_MAX_SAMPLES)
		return NULL;
	if(bitrate_bps <= 0)
		bitrate_bps = 64000;

	QROpusPCMEncoder *e = calloc(1, sizeof(QROpusPCMEncoder));
	if(!e)
		return NULL;

	e->channels = channels;
	e->frame_samples = frame_samples;

	int err = OPUS_OK;
	// 与 Sunshine 完全一致（Sunshine/src/audio.cpp）：OPUS_APPLICATION_RESTRICTED_LOWDELAY。
	// 之前用 OPUS_APPLICATION_AUDIO + 64Kbps 立体声：5ms 一帧只有 40 字节，音乐类内容会有
	// 明显的金属味/水下感（用户报的「失真」）。Sunshine 立体声默认 96Kbps（高音质 512Kbps）。
	e->encoder = opus_encoder_create(sample_rate, channels, OPUS_APPLICATION_RESTRICTED_LOWDELAY, &err);
	if(!e->encoder || err != OPUS_OK)
	{
		QROpusPCMEncoderDestroy(e);
		return NULL;
	}

	// 与 PS5 音频同样的理由：客户端 RtpAudioQueue 要求同一个 FEC 块内所有包长度一致，
	// 因此必须 CBR（OPUS_SET_VBR(0)），否则客户端切到降级路径。
	// 复杂度取 libopus 默认值 10（Sunshine 不显式设置，即默认 10），CPU 开销可忽略。
	opus_encoder_ctl(e->encoder, OPUS_SET_BITRATE(bitrate_bps));
	opus_encoder_ctl(e->encoder, OPUS_SET_VBR(0));
	opus_encoder_ctl(e->encoder, OPUS_SET_COMPLEXITY(10));

	e->out_buf = malloc(QR_OPUS_OUT_BUF_SIZE);
	if(!e->out_buf)
	{
		QROpusPCMEncoderDestroy(e);
		return NULL;
	}
	return e;
}

void QROpusPCMEncoderDestroy(QROpusPCMEncoder *e)
{
	if(!e)
		return;
	if(e->encoder)
		opus_encoder_destroy(e->encoder);
	free(e->out_buf);
	free(e);
}

int QROpusPCMEncoderEncode(QROpusPCMEncoder *e, const int16_t *pcm, unsigned char **out)
{
	if(!e || !pcm || !out)
		return 0;
	int len = opus_encode(e->encoder, pcm, e->frame_samples, e->out_buf, QR_OPUS_OUT_BUF_SIZE);
	if(len <= 0)
		return 0;
	*out = e->out_buf;
	return len;
}

// MARK: - 视频 FEC（见头文件说明）

static uint8_t qr_gf_exp[512], qr_gf_log[256], qr_gf_inv[256];
static uint8_t qr_gf_mul[256][256];
static int qr_gf_ready = 0;

static void qr_gf_init(void) {
    if (qr_gf_ready) return;
    int x = 1;
    for (int i = 0; i < 255; i++) {
        qr_gf_exp[i] = qr_gf_exp[i + 255] = (uint8_t)x;
        qr_gf_log[x] = (uint8_t)i;
        x <<= 1;
        if (x & 0x100) x ^= 0x11D;
    }
    for (int v = 1; v < 256; v++) qr_gf_inv[v] = qr_gf_exp[(255 - qr_gf_log[v]) % 255];
    for (int a = 1; a < 256; a++)
        for (int b = 1; b < 256; b++)
            qr_gf_mul[a][b] = qr_gf_exp[qr_gf_log[a] + qr_gf_log[b]];
    qr_gf_ready = 1;
}

void QRVideoFECEncode(const uint8_t *const *data, const int *lens, int ds,
                      uint8_t *const *parity, int ps, int bs) {
    qr_gf_init();
    for (int j = 0; j < ps; j++) {
        uint8_t *dst = parity[j];
        memset(dst, 0, (size_t)bs);
        for (int i = 0; i < ds; i++) {
            uint8_t coef = qr_gf_inv[((ps + i) ^ j) & 0xFF];
            if (coef == 0) continue;
            const uint8_t *row = qr_gf_mul[coef];
            const uint8_t *src = data[i];
            int n = lens[i] < bs ? lens[i] : bs;
            for (int k = 0; k < n; k++) dst[k] ^= row[src[k]];
        }
    }
}

// MARK: - 诊断用 Opus 解码器

struct OpaqueQROpusDecoder { OpusDecoder *dec; };

QROpusDecoder *QROpusDecoderCreate(int sample_rate, int channels) {
    int err = 0;
    OpusDecoder *d = opus_decoder_create(sample_rate, channels, &err);
    if (!d || err != OPUS_OK) return NULL;
    QROpusDecoder *w = (QROpusDecoder *)calloc(1, sizeof(*w));
    if (!w) { opus_decoder_destroy(d); return NULL; }
    w->dec = d;
    return w;
}

int QROpusDecoderDecode(QROpusDecoder *decoder, const uint8_t *packet, int length, int16_t *pcm, int max_frames) {
    if (!decoder) return -1;
    return opus_decode(decoder->dec, packet, length, pcm, max_frames, 0);
}

void QROpusDecoderDestroy(QROpusDecoder *decoder) {
    if (!decoder) return;
    opus_decoder_destroy(decoder->dec);
    free(decoder);
}
