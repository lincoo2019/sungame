//
//  ChiakiShim.c
//  QuickRecorder
//
//  见 ChiakiShim.h。
//

#include "ChiakiShim.h"

#include <string.h>

#include <chiaki/base64.h>

static void copy_cstr(char *dst, size_t dst_len, const char *src)
{
	if(!dst || dst_len == 0)
		return;
	dst[0] = '\0';
	if(!src)
		return;
	size_t n = strlen(src);
	if(n >= dst_len)
		n = dst_len - 1;
	memcpy(dst, src, n);
	dst[n] = '\0';
}

void QRChiakiDescribeEvent(const ChiakiEvent *event, QRChiakiEventInfo *out)
{
	if(!event || !out)
		return;
	memset(out, 0, sizeof(*out));
	out->type = (int32_t)event->type;

	switch(event->type)
	{
		case CHIAKI_EVENT_QUIT:
			out->is_error = chiaki_quit_reason_is_error(event->quit.reason);
			copy_cstr(out->reason, sizeof(out->reason), event->quit.reason_str);
			if(out->reason[0] == '\0')
				copy_cstr(out->reason, sizeof(out->reason), chiaki_quit_reason_string(event->quit.reason));
			break;
		case CHIAKI_EVENT_LOGIN_PIN_REQUEST:
			out->pin_incorrect = event->login_pin_request.pin_incorrect;
			break;
		case CHIAKI_EVENT_NICKNAME_RECEIVED:
			// server_nickname 不保证以 \0 结尾
			{
				size_t n = strnlen(event->server_nickname, sizeof(event->server_nickname));
				if(n >= sizeof(out->nickname))
					n = sizeof(out->nickname) - 1;
				memcpy(out->nickname, event->server_nickname, n);
				out->nickname[n] = '\0';
			}
			break;
		case CHIAKI_EVENT_REGIST:
			{
				const ChiakiRegisteredHost *host = &event->host;
				out->regist_filled = true;
				chiaki_base64_encode((const uint8_t *)host->rp_regist_key, sizeof(host->rp_regist_key),
						out->regist_key_b64, sizeof(out->regist_key_b64));
				chiaki_base64_encode(host->rp_key, sizeof(host->rp_key),
						out->regist_morning_b64, sizeof(out->regist_morning_b64));
			}
			break;
		default:
			break;
	}
}

void QRChiakiFillVideoProfile(ChiakiConnectVideoProfile *profile, int resolution_preset, int fps_preset, int bitrate)
{
	if(!profile)
		return;
	chiaki_connect_video_profile_preset(profile,
			(ChiakiVideoResolutionPreset)resolution_preset,
			(ChiakiVideoFPSPreset)fps_preset);
	if(bitrate > 0)
		profile->bitrate = (unsigned int)bitrate;
}

void QRChiakiSetRegistKey(ChiakiConnectInfo *info, const uint8_t *src)
{
	if(!info || !src)
		return;
	memcpy(info->regist_key, src, sizeof(info->regist_key));
}

void QRChiakiSetMorning(ChiakiConnectInfo *info, const uint8_t *src)
{
	if(!info || !src)
		return;
	memcpy(info->morning, src, sizeof(info->morning));
}

bool QRChiakiSetRegistKeyB64(ChiakiConnectInfo *info, const char *b64)
{
	if(!info || !b64)
		return false;
	size_t out_size = sizeof(info->regist_key);
	if(chiaki_base64_decode(b64, strlen(b64), (uint8_t *)info->regist_key, &out_size) != CHIAKI_ERR_SUCCESS)
		return false;
	return out_size == sizeof(info->regist_key);
}

bool QRChiakiSetMorningB64(ChiakiConnectInfo *info, const char *b64)
{
	if(!info || !b64)
		return false;
	size_t out_size = sizeof(info->morning);
	if(chiaki_base64_decode(b64, strlen(b64), info->morning, &out_size) != CHIAKI_ERR_SUCCESS)
		return false;
	return out_size == sizeof(info->morning);
}

uint32_t QRChiakiControllerButtons(const ChiakiControllerState *state)
{
	return state ? state->buttons : 0;
}

void QRChiakiSetAudioSink(ChiakiSession *session, ChiakiAudioSink *sink)
{
	if(!session || !sink)
		return;
	chiaki_session_set_audio_sink(session, sink);
}

void QRChiakiDescribeAudioHeader(const ChiakiAudioHeader *header, int32_t *channels, int32_t *rate, int32_t *frame_size)
{
	if(!header)
		return;
	if(channels)
		*channels = (int32_t)header->channels;
	if(rate)
		*rate = (int32_t)header->rate;
	if(frame_size)
		*frame_size = (int32_t)header->frame_size;
}

ChiakiErrorCode QRChiakiRegistStart(ChiakiRegist *regist, ChiakiLog *log, const ChiakiRegistInfo *info,
									ChiakiRegistCb cb, void *cb_user)
{
	if(!regist || !info)
		return CHIAKI_ERR_INVALID_DATA;
	return chiaki_regist_start(regist, log, info, cb, cb_user);
}

bool QRChiakiFillRegistInfo(ChiakiRegistInfo *info, const char *host, bool ps5, const char *account_id_b64,
							uint32_t pin, uint32_t console_pin)
{
	if(!info || !host)
		return false;
	memset(info, 0, sizeof(*info));
	info->target = ps5 ? CHIAKI_TARGET_PS5_1 : CHIAKI_TARGET_PS4_10;
	info->host = host;
	info->broadcast = false;
	info->pin = pin;
	info->console_pin = console_pin;
	// PS5 与 PS4 >= 7.0 都必须用 PSN Account-ID（8 字节原始值），而不是 PSN Online-ID
	info->psn_online_id = NULL;
	info->holepunch_info = NULL;

	if(!account_id_b64 || account_id_b64[0] == '\0')
		return false;
	size_t out_size = sizeof(info->psn_account_id);
	if(chiaki_base64_decode(account_id_b64, strlen(account_id_b64), info->psn_account_id, &out_size) != CHIAKI_ERR_SUCCESS)
		return false;
	return out_size == sizeof(info->psn_account_id);
}

bool QRChiakiRegistDescribe(const ChiakiRegistEvent *event, int32_t *type,
							char *nickname, size_t nickname_len,
							char *regist_key_b64, size_t regist_key_len,
							char *morning_b64, size_t morning_len)
{
	if(!event)
		return false;
	if(type)
		*type = (int32_t)event->type;
	if(event->type != CHIAKI_REGIST_EVENT_TYPE_FINISHED_SUCCESS || !event->registered_host)
		return false;

	const ChiakiRegisteredHost *host = event->registered_host;
	size_t n = strnlen(host->server_nickname, sizeof(host->server_nickname));
	if(n >= nickname_len)
		n = nickname_len ? nickname_len - 1 : 0;
	if(nickname && nickname_len)
	{
		memcpy(nickname, host->server_nickname, n);
		nickname[n] = '\0';
	}
	if(regist_key_b64 && regist_key_len)
		chiaki_base64_encode((const uint8_t *)host->rp_regist_key, sizeof(host->rp_regist_key),
				regist_key_b64, regist_key_len);
	if(morning_b64 && morning_len)
		chiaki_base64_encode(host->rp_key, sizeof(host->rp_key), morning_b64, morning_len);
	return true;
}
