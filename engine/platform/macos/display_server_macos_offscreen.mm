/**************************************************************************/
/*  display_server_macos_offscreen.mm                                     */
/**************************************************************************/
/*                         This file is part of:                          */
/*                             GODOT ENGINE                               */
/*                        https://godotengine.org                         */
/**************************************************************************/
/* Copyright (c) 2014-present Godot Engine contributors (see AUTHORS.md). */
/* Copyright (c) 2007-2014 Juan Linietsky, Ariel Manzur.                  */
/*                                                                        */
/* Permission is hereby granted, free of charge, to any person obtaining  */
/* a copy of this software and associated documentation files (the        */
/* "Software"), to deal in the Software without restriction, including    */
/* without limitation the rights to use, copy, modify, merge, publish,    */
/* distribute, sublicense, and/or sell copies of the Software, and to     */
/* permit persons to whom the Software is furnished to do so, subject to  */
/* the following conditions:                                              */
/*                                                                        */
/* The above copyright notice and this permission notice shall be         */
/* included in all copies or substantial portions of the Software.        */
/*                                                                        */
/* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,        */
/* EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF     */
/* MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. */
/* IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY   */
/* CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,   */
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE      */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/

#import "display_server_macos_offscreen.h"

#import "core/config/project_settings.h"
#import "core/input/input.h"
#import "core/input/input_event.h"
#import "core/os/main_loop.h"
#import "core/os/os.h"
#import "servers/audio/audio_server.h"
#import "servers/audio/effects/audio_effect_capture.h"

#import <IOSurface/IOSurface.h>

#if defined(RD_ENABLED)
#import "servers/rendering/renderer_rd/renderer_compositor_rd.h"
#import "servers/rendering/rendering_device.h"

#if defined(METAL_ENABLED)
#import "drivers/metal/rendering_context_driver_metal.h"
#endif
#endif // RD_ENABLED

#import "drivers/metal/qr_reverse_channel.h"

#import <QuartzCore/QuartzCore.h>

#include <errno.h>
#include <poll.h>
#include <pthread/qos.h>
#include <string.h>
#include <sys/socket.h>
#include <sys/un.h>
#include <unistd.h>

namespace {

constexpr size_t kInputPacketSize = 24;

// 24 字节输入包里的触摸元数据字节（见 GameStream.sendInput 的注释）：
//   [20] 携带 Moonlight 的 LI_TOUCH_EVENT_* 原始类型
//   [21] == 1 表示该字段有效（新宿主机才填，老宿主不填则回退到 pressed 位语义）
constexpr size_t kInputTouchEventOffset = 20;
constexpr size_t kInputTouchFlagOffset = 21;

inline uint16_t _rd_u16(const uint8_t *p) {
	return (uint16_t)p[0] | ((uint16_t)p[1] << 8);
}

inline uint32_t _rd_u32(const uint8_t *p) {
	return (uint32_t)p[0] | ((uint32_t)p[1] << 8) | ((uint32_t)p[2] << 16) | ((uint32_t)p[3] << 24);
}

inline float _rd_f32(const uint8_t *p) {
	uint32_t u = _rd_u32(p);
	float f;
	memcpy(&f, &u, sizeof(f));
	return f;
}

} // namespace

DisplayServerMacOSOffscreen::DisplayServerMacOSOffscreen(const String &p_rendering_driver, DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::VSyncMode p_vsync_mode, uint32_t p_flags, const Vector2i *p_position, const Vector2i &p_resolution, int p_screen, DisplayServerEnums::Context p_context, Error &r_error) {
	r_error = OK; // default to OK

	Input::get_singleton()->set_event_dispatch_function(_dispatch_input_events);

	rendering_driver = p_rendering_driver;

	// Determine the render resolution.
	window_size = p_resolution;
	if (window_size.x <= 0 || window_size.y <= 0) {
		window_size = Size2i(1920, 1080);
	}
	if (String w = OS::get_singleton()->get_environment("QR_OFFSCREEN_W"); !w.is_empty()) {
		window_size.x = w.to_int();
	}
	if (String h = OS::get_singleton()->get_environment("QR_OFFSCREEN_H"); !h.is_empty()) {
		window_size.y = h.to_int();
	}
	// The IOSurface dimensions are authoritative: the render target texture must
	// match them exactly.
	if (String surface_id = OS::get_singleton()->get_environment("QR_IOSURFACE_ID"); !surface_id.is_empty()) {
		IOSurfaceRef surface = IOSurfaceLookup((IOSurfaceID)surface_id.to_int());
		if (surface) {
			window_size.x = (int)IOSurfaceGetWidth(surface);
			window_size.y = (int)IOSurfaceGetHeight(surface);
			CFRelease(surface);
		} else {
			ERR_PRINT(vformat("Offscreen display server: could not look up IOSurface %s.", surface_id));
		}
	}

	// 触摸→鼠标：交回 Godot 内建的 emulate_mouse_from_touch，而不是我们自己合成。
	//
	// 为什么不再自己合成（这是上一轮的回归）：我们自造的鼠标事件会被游戏消费掉，把
	// ScrollContainer 等**触摸专有**的拖动手势（走 InputEventScreenDrag）抢走 → 拖动不再滚动。
	// 正确做法是让原生触摸照常送达（拖动可用），同时由 Godot 自己合成鼠标（只读鼠标的桌面游戏仍可玩）。
	//
	// 为什么写 ProjectSettings 而不是只调 Input::set_emulate_mouse_from_touch：
	//   main.cpp 在 DisplayServer 创建**之后**才执行
	//   `id->set_emulate_mouse_from_touch(GLOBAL_DEF_BASIC("input_devices/pointing/emulate_mouse_from_touch", true))`，
	//   会用它读到的值覆盖直接设置。而 GLOBAL_DEF 只在「该键尚未存在」时才写入默认值（见 project_settings.cpp
	//   的 _GLOBAL_DEF），所以这里先把该键置为 true，随后那次读取就会得到 true，进而不会被游戏工程的设置
	//   （例如显式 false）覆盖。再顺手直接调一次 Input 的 setter 作为兜底（万一将来执行顺序变化）。
	ProjectSettings::get_singleton()->set_setting("input_devices/pointing/emulate_mouse_from_touch", true);
	Input::get_singleton()->set_emulate_mouse_from_touch(true);

	// 任务C诊断：off-screen 显示驱动启动时，把「渲染目标尺寸」与「游戏自身的 viewport / 拉伸设置」打出来。
	// 这几行是判断「游戏内部是否真的按协商尺寸渲染」的引擎侧直接证据（对任何游戏都适用，无需改游戏工程）。
	{
		ProjectSettings *ps = ProjectSettings::get_singleton();
		auto get_int = [&](const char *p_key, int p_default) -> int {
			return ps->has_setting(p_key) ? (int)ps->get_setting(p_key) : p_default;
		};
		auto get_str = [&](const char *p_key) -> String {
			return ps->has_setting(p_key) ? String(ps->get_setting(p_key)) : String("(unset)");
		};
		print_line(vformat("QRSize: offscreen render_target=%dx%d game_viewport=%dx%d stretch_mode=%s stretch_aspect=%s window_override=%dx%d",
				window_size.x, window_size.y,
				get_int("display/window/size/viewport_width", -1), get_int("display/window/size/viewport_height", -1),
				get_str("display/window/stretch/mode"), get_str("display/window/stretch/aspect"),
				get_int("display/window/size/window_width_override", -1), get_int("display/window/size/window_height_override", -1)));
		fflush(stdout);
	}

#if defined(RD_ENABLED)
	if (rendering_driver == "metal") {
		rendering_context = memnew(RenderingContextDriverMetal);
	}

	if (rendering_context) {
		if (rendering_context->initialize() != OK) {
			memdelete(rendering_context);
			rendering_context = nullptr;
			r_error = ERR_CANT_CREATE;
			ERR_FAIL_MSG("Could not initialize " + rendering_driver);
		}
	} else {
		r_error = ERR_UNAVAILABLE;
		ERR_FAIL_MSG("The offscreen display server only supports the Metal rendering driver.");
	}

	if (rendering_context) {
		layer = [CAMetalLayer new];
		layer.anchorPoint = CGPointMake(0, 1);

		RenderingContextDriverMetal::WindowPlatformData wpd;
		wpd.layer = (__bridge CA::MetalLayer *)layer;

		Error err = rendering_context->window_create(window_id_counter, &wpd);
		ERR_FAIL_COND_MSG(err != OK, vformat("Can't create a %s context", rendering_driver));

		rendering_context->window_set_size(window_id_counter, window_size.width, window_size.height);
		rendering_context->window_set_vsync_mode(window_id_counter, p_vsync_mode);

		layer.contentsScale = 1.0;
		layer.opaque = true;
		layer.actions = @{ @"contents" : [NSNull null] }; // Disable implicit animations.
		layer.bounds = CGRectMake(0, 0, window_size.width, window_size.height);
	}

	if (rendering_context) {
		rendering_device = memnew(RenderingDevice);
		rendering_device->initialize(rendering_context, DisplayServerEnums::MAIN_WINDOW_ID);
		rendering_device->screen_create(DisplayServerEnums::MAIN_WINDOW_ID);

		RendererCompositorRD::make_current();
	}
#endif // RD_ENABLED

	// 游戏音频：把混音率强制为 48000（Moonlight/Opus 要求）。
	//
	// 为什么在显示驱动构造里写 ProjectSettings：与上面 emulate_mouse_from_touch 同理——
	// AudioDriverManager::initialize()（内部 GLOBAL_DEF_RST("audio/driver/mix_rate", ...)）
	// 在 DisplayServer 创建**之后**才执行，而 GLOBAL_DEF 只在键不存在时才写默认值；
	// 这里先 set_setting 就能让随后 AudioDriver::_get_configured_mix_rate() 读到 48000，
	// 不会被工程设置或默认值覆盖。只在需要抓音频时强制，避免关闭音频时改变游戏自身音高。
	const bool want_game_audio = OS::get_singleton()->get_environment("QR_GAME_AUDIO") == "1";
	if (want_game_audio) {
		ProjectSettings::get_singleton()->set_setting("audio/driver/mix_rate", 48000);
	}

	// Only start injecting external input when a socket path was provided. With
	// an empty QR_INPUT_SOCKET the offscreen driver behaves exactly as before.
	if (!OS::get_singleton()->get_environment("QR_INPUT_SOCKET").is_empty()) {
		input_thread_should_stop.clear();
		input_thread.start(_input_thread_func, this);
		input_thread_started = true;
	}

	// 音频回传依赖反向通道（即输入 socket 的反方向），因此仅在两者都具备时启动。
	if (want_game_audio && !OS::get_singleton()->get_environment("QR_INPUT_SOCKET").is_empty()) {
		audio_thread_should_stop.clear();
		audio_thread.start(_audio_thread_func, this);
		audio_thread_started = true;
	}
}

DisplayServerMacOSOffscreen::~DisplayServerMacOSOffscreen() {
	if (audio_thread_started) {
		audio_thread_should_stop.set();
		audio_thread.wait_to_finish();
		audio_thread_started = false;
	}

	if (input_thread_started) {
		input_thread_should_stop.set();
		input_thread.wait_to_finish();
		input_thread_started = false;
	}

#if defined(RD_ENABLED)
	if (rendering_device) {
		memdelete(rendering_device);
		rendering_device = nullptr;
	}

	if (rendering_context) {
		memdelete(rendering_context);
		rendering_context = nullptr;
	}
#endif
}

// MARK: - External input (QR_INPUT_SOCKET)

void DisplayServerMacOSOffscreen::_input_thread_func(void *p_userdata) {
	DisplayServerMacOSOffscreen *self = (DisplayServerMacOSOffscreen *)p_userdata;
	self->_input_thread_loop();
}

void DisplayServerMacOSOffscreen::_input_thread_loop() {
	Thread::set_name("QRInput");

	const String socket_path = OS::get_singleton()->get_environment("QR_INPUT_SOCKET");
	if (socket_path.is_empty()) {
		return;
	}
	const CharString path_utf8 = socket_path.utf8();
	sockaddr_un addr;
	memset(&addr, 0, sizeof(addr));
	if (path_utf8.length() >= (int)sizeof(addr.sun_path)) {
		ERR_PRINT(vformat("Offscreen input: QR_INPUT_SOCKET path is too long: %s", socket_path));
		return;
	}
	addr.sun_family = AF_UNIX;
	memcpy(addr.sun_path, path_utf8.get_data(), path_utf8.length());

	uint8_t buf[kInputPacketSize * kMaxPacketsPerBatch];
	size_t buf_len = 0;

	while (!input_thread_should_stop.is_set()) {
		// --- Connect (with retry until the host is listening). ---
		int fd = -1;
		while (!input_thread_should_stop.is_set()) {
			fd = ::socket(AF_UNIX, SOCK_STREAM, 0);
			if (fd < 0) {
				break; // Cannot even create a socket; give up entirely.
			}
			if (::connect(fd, (struct sockaddr *)&addr, sizeof(addr)) == 0) {
				break; // Connected.
			}
			::close(fd);
			fd = -1;
			usleep(100 * 1000); // 100 ms before retrying.
		}

		if (fd < 0) {
			break; // Stopping or fatal socket error.
		}

		// Register this connection as the reverse frame-notification channel: the
		// render thread writes the completed IOSurface slot index back over it so
		// the host only ever reads a fully rendered frame.
		QRReverseChannel::set_fd(fd);

		input_connect_count++;
		buf_len = 0;

		// --- Read loop until disconnect, then reconnect. ---
		while (!input_thread_should_stop.is_set()) {
			struct pollfd pfd;
			pfd.fd = fd;
			pfd.events = POLLIN;
			pfd.revents = 0;
			int pr = ::poll(&pfd, 1, 100);
			if (pr < 0) {
				if (errno == EINTR) {
					continue;
				}
				break;
			}

			if (pr > 0) {
				ssize_t n = ::read(fd, buf + buf_len, sizeof(buf) - buf_len);
				if (n <= 0) {
					break; // Peer closed or error: reconnect.
				}
				buf_len += (size_t)n;

				// Consume complete 24-byte packets, keeping any partial tail.
				// 单批上限：一次最多派发 kMaxPacketsPerBatch 个包，避免一波洪峰
				// 长时间独占用输入线程（剩余数据下一轮 poll 立即继续处理）。
				size_t off = 0;
				int processed = 0;
				while (buf_len - off >= kInputPacketSize && processed < kMaxPacketsPerBatch) {
					_handle_input_packet(buf + off);
					off += kInputPacketSize;
					processed++;
				}
				if (off > 0) {
					memmove(buf, buf + off, buf_len - off);
					buf_len -= off;
				}
			}

			// 每一轮（无论有没有新数据）都跑看门狗与周期日志：
			// 「up 丢失」时对端已不再发数据，只有靠这里的轮询才能把卡住的 index 释放掉。
			// 注意时间戳必须在这一轮派发「之后」再取：派发时会给 touch_last_ms 写入新时间，
			// 若沿用派发前取的旧时间戳，就会得到 last > now，无符号相减溢出成巨大值 →
			// 看门狗会在拖动过程中被误触发（这正是第一版实现踩到的坑）。
			const uint64_t now_ms = OS::get_singleton()->get_ticks_msec();
			_release_stuck_touches(now_ms);
			_log_input_stats_if_due(now_ms);
		}

		// 断开连接：把仍按下的触点全部抬起，避免状态残留到下一次连接。
		_release_all_touches();
		QRReverseChannel::clear_fd();
		::close(fd);
	}
}

// MARK: - Game audio capture (Engine → Host)

namespace {
// 每次从 AudioEffectCapture 读出的最大帧数（20ms），足够大以减少消息条数、又不至于让负载超限。
constexpr int kAudioMaxFramesPerMsg = 960;
// 攒到这么多帧就发一次（5ms）。不足则短暂轮询等待。
constexpr int kAudioMinFramesPerChunk = 240;

// Float32 [-1,1] → Int16，越界钳位，避免溢出翻转成爆音。
inline int16_t _f32_to_i16(float p_v) {
	if (p_v > 1.0f) {
		p_v = 1.0f;
	} else if (p_v < -1.0f) {
		p_v = -1.0f;
	}
	return (int16_t)(p_v * 32767.0f);
}
} // namespace

void DisplayServerMacOSOffscreen::_audio_thread_func(void *p_userdata) {
	DisplayServerMacOSOffscreen *self = (DisplayServerMacOSOffscreen *)p_userdata;
	self->_audio_thread_loop();
}

void DisplayServerMacOSOffscreen::_audio_thread_loop() {
	Thread::set_name("QRAudio");
	// 提高本线程 QoS：低优先级/后台进程会被系统做定时器合并，导致读取间隔被拉长到十几 ms；
	// 提到 USER_INTERACTIVE 后读取更及时（不影响音频驱动线程自身的节奏）。
	pthread_set_qos_class_self_np(QOS_CLASS_USER_INTERACTIVE, 0);

	// 自测开关：模拟「AudioEffectCapture 添加失败 / 音频未启动」，验证音频不可用时视频/输入完全不受影响。
	if (OS::get_singleton()->get_environment("QR_GAME_AUDIO_FORCE_FAIL") == "1") {
		print_line("QRAudio: capture forcibly disabled (QR_GAME_AUDIO_FORCE_FAIL=1); video/input unaffected");
		fflush(stdout);
		return;
	}

	// 等待 AudioServer 就绪：驱动已 init（mix_rate 生效）且 Master 总线存在。
	// AudioDriverManager::initialize() 在本显示驱动构造之后才跑，所以这里必须等。
	AudioServer *audio_server = nullptr;
	for (int i = 0; i < 200 && !audio_thread_should_stop.is_set(); i++) {
		audio_server = AudioServer::get_singleton();
		if (audio_server != nullptr && audio_server->get_bus_count() > 0 && audio_server->get_mix_rate() > 0) {
			break;
		}
		OS::get_singleton()->delay_usec(50 * 1000);
	}
	if (audio_server == nullptr || audio_thread_should_stop.is_set()) {
		print_line("QRAudio: AudioServer not ready, capture not started; video/input unaffected");
		fflush(stdout);
		return;
	}

	const int mix_rate = (int)audio_server->get_mix_rate();
	if (mix_rate != 48000) {
		// 采样率必须是 48000（Moonlight/Opus 客户端按 48×duration 固定分配解码缓冲）。
		// CoreAudio 驱动会采用输出设备的实际采样率（覆盖我们写的 ProjectSettings），
		// 设备不是 48000 时这里直接放弃音频（只关音频，视频/输入不受影响），而不是发出音高错误的音频。
		print_line(vformat("QRAudio: unsupported mix_rate=%d (expected 48000); capture disabled, video/input unaffected", mix_rate));
		fflush(stdout);
		return;
	}

	// 本地静音：把 Master 总线静音。AudioEffectCapture 挂在总线上、其 process() 跑在
	// 「应用总线音量之前」（见 AudioServer::_mix_step：效果链处理完后才用 bus 音量/静音做输出整形），
	// 所以静音只影响本地外放，捕获到的仍是完整音量的混音。
	// 这样无需 Dummy 驱动（Dummy 的线程用 sleep 计时，在本机被系统定时器合并/降 QoS 时明显偏慢，
	// 实测只有约 40k 帧/秒而非 48000，会把音频拖慢），也不需要任何系统音频录制授权。
	audio_server->set_bus_mute(0, true);

	// 用 Godot 自己的公开能力抓混音：往 Master 总线（bus 0）加一个 AudioEffectCapture。
	Ref<AudioEffectCapture> capture;
	capture.instantiate();
	// 加大缓冲（0.5s）吸收调度抖动，避免读取不及导致丢帧。
	capture->set_buffer_length(0.5f);
	audio_server->add_bus_effect(0, capture);

	// 确认确实加进去了（取回最后一条效果应与我们加的同一个对象）。
	const int effect_count = audio_server->get_bus_effect_count(0);
	if (effect_count <= 0 || audio_server->get_bus_effect(0, effect_count - 1).ptr() != capture.ptr()) {
		print_line("QRAudio: failed to add AudioEffectCapture to Master bus; capture disabled, video/input unaffected");
		fflush(stdout);
		return;
	}

	const AudioDriver *driver = AudioDriver::get_singleton();
	print_line(vformat("QRAudio: capture started bus=Master driver=%s mix_rate=%d master_muted=%s effect_buffer_frames=%d",
			driver != nullptr ? driver->get_name() : "?", mix_rate,
			audio_server->is_bus_mute(0) ? "yes" : "no", capture->get_buffer_length_frames()));
	fflush(stdout);

	uint64_t sent_msgs = 0;
	uint64_t sent_frames = 0;
	uint64_t dropped_msgs = 0;
	uint64_t nonzero_samples = 0;
	uint64_t clipped_samples = 0;
	uint64_t last_pushed = 0;
	uint64_t last_retry_count = 0;
	uint64_t log_last_ms = OS::get_singleton()->get_ticks_msec();

	// Engine 侧就转成 Int16，回传体积比 Float 小一半；宿主拿到即可直接编码。
	int16_t pcm[kAudioMaxFramesPerMsg * 2];

	// 5ms 节拍放出音频。
	//
	// 为什么必须是「按时间放」而不是「有多少读多少」：CoreAudio 的缓冲是 1024 帧（约 21ms），
	// 原来的写法一次最多读走 960 帧，宿主于是**每 21ms 收到一次 960 帧的突发**，转发出去就是
	// 4 个包挤在一起（实测 回传≈59 消息/秒、每条 813 帧）。客户端 AudioQueue 只有约 3 个包
	// （~15ms）的深度，突发间隔一超过它就会欠载——听感就是「啪」一下。Sunshine 靠 OS 回调
	// 天然只有 ~10ms 粒度，从不需要这层匀化，所以它不会出这个问题。
	//
	// 这里改成每 5ms 只放出 240 帧（= 一帧 Opus），宿主便可一个包一个包地匀速发出。
	// AudioEffectCapture 的环形缓冲有 0.5s，足以吸收驱动侧 21ms 的批次；只有积压超过
	// 60ms（刚启动、或系统卡顿之后）才多读一点追赶，否则积压迟早会撑爆环形缓冲丢帧。
	const int paced_frames = mix_rate / 200; // 5ms（48000 → 240）
	// 追赶阈值必须**明显大于驱动一次推入的帧数**：CoreAudio 的 1024 帧缓冲是整批 push 进环形缓冲的，
	// 阈值若取 960（<1024），每个新批次一到就会走追赶分支、一次读走 960 帧，节拍等于失效
	// （实测 回传 仍是 ~66 消息/秒、每批 ~720 帧）。取 60ms 留足余量后，正常每拍只放 240 帧。
	const int backlog_limit = paced_frames * 12; // 60ms
	const uint64_t paced_period_us = 5000;
	uint64_t next_emit_us = OS::get_singleton()->get_ticks_usec();
	uint64_t catchup_reads = 0;
	uint64_t max_read_frames = 0;

	while (!audio_thread_should_stop.is_set()) {
		const int avail = capture->get_frames_available();
		const uint64_t now_us = OS::get_singleton()->get_ticks_usec();
		if (avail < kAudioMinFramesPerChunk) {
			OS::get_singleton()->delay_usec(1000);
		} else if (now_us < next_emit_us) {
			// 还没到下一个节拍：数据留在环形缓冲里（它够深），先睡一小会再来看。
			OS::get_singleton()->delay_usec(1000);
		} else {
			const bool catchup = avail > backlog_limit;
			const int to_read = catchup ? MIN(avail, kAudioMaxFramesPerMsg) : paced_frames;
			if (catchup) {
				catchup_reads++;
			}
			const PackedVector2Array frames = capture->get_buffer(to_read);
			const int got = frames.size();
			if (got > 0) {
				if ((uint64_t)got > max_read_frames) {
					max_read_frames = (uint64_t)got;
				}
				const Vector2 *src = frames.ptr();
				for (int i = 0; i < got; i++) {
					// 统计削波样本（混音总线的效果链输出可能超 ±1，转 Int16 时被钳位）。
					// 削波就是「失真」最常见的来源，计数出来才能判断是不是它。
					if (src[i].x > 1.0f || src[i].x < -1.0f || src[i].y > 1.0f || src[i].y < -1.0f) {
						clipped_samples++;
					}
					const int16_t l = _f32_to_i16(src[i].x);
					const int16_t r = _f32_to_i16(src[i].y);
					pcm[i * 2] = l;
					pcm[i * 2 + 1] = r;
					if (l != 0 || r != 0) {
						nonzero_samples++;
					}
				}
				// 音频**不允许**「不可写就丢」：丢一条就是客户端时间线上少 5ms（一声「啪」）。
				// 这里最多等 30ms 重试——音频线程是自驱节拍线程、环形缓冲有 0.5s 余量，
				// 晚发几十毫秒只是延迟抖动，客户端抖动缓冲吸收得了（帧完成通知仍保持绝不阻塞）。
				if (QRReverseChannel::send_audio(pcm, got, 30)) {
					sent_msgs++;
					sent_frames += (uint64_t)got;
				} else {
					dropped_msgs++;
				}
			}
			// 绝对时刻推进，避免累积漂移；落后太多就直接对齐到当前，
			// 不为「补回」而连续突发（那等于把问题又搬回去）。
			next_emit_us += paced_period_us;
			if (next_emit_us <= now_us) {
				next_emit_us = now_us + paced_period_us;
			}
		}

		// 每 5 秒一行引擎侧音频诊断（宿主会把它透传进串流日志）。
		// 注意：Godot 的 vformat 不支持 %llu/%lld，全部转 int/double 再格式化。
		const uint64_t now_ms = OS::get_singleton()->get_ticks_msec();
		if (now_ms - log_last_ms >= 5000) {
			// 每个诊断周期重申一次本地静音：游戏可能在启动后应用自己的音频设置，
			// 这里保证「本地不外放」这一前提始终成立（捕获在音量整形之前，不受影响）。
			audio_server->set_bus_mute(0, true);
			const int pushed = (int)capture->get_pushed_frames();
			const int discarded = (int)capture->get_discarded_frames();
			const double seconds = (double)(now_ms - log_last_ms) / 1000.0;
			const double avg_frames = sent_msgs > 0 ? (double)sent_frames / (double)sent_msgs : 0.0;
			const uint64_t retries_now = QRReverseChannel::retry_count;
			const uint64_t retries_window = retries_now - last_retry_count;
			last_retry_count = retries_now;
			print_line(vformat("QRAudio: mix_rate=%d driver=%s 回传=%.1f 消息/秒 每批帧数=%.0f 最大单批=%d 追赶=%d 累计帧=%d 非零样本=%d 削波样本=%d 丢弃消息=%d 发送等待=%d 总线新增帧/秒=%d 总线丢弃帧=%d 环形积压帧=%d",
					mix_rate, driver != nullptr ? driver->get_name() : "?",
					(double)sent_msgs / seconds, avg_frames, (int)max_read_frames, (int)catchup_reads,
					(int)sent_frames, (int)nonzero_samples, (int)clipped_samples,
					(int)dropped_msgs, (int)retries_window, (int)((double)(pushed - (int)last_pushed) / seconds), discarded, avail));
			fflush(stdout);
			last_pushed = (uint64_t)pushed;
			sent_msgs = 0;
			catchup_reads = 0;
			max_read_frames = 0;
			clipped_samples = 0;
			log_last_ms = now_ms;
		}
	}

	print_line(vformat("QRAudio: capture stopped 累计回传帧=%d 非零样本=%d 丢弃消息=%d",
			(int)sent_frames, (int)nonzero_samples, (int)dropped_msgs));
	fflush(stdout);
}

// 在槽位表里查找已绑定该 pointerId 的槽位；未绑定返回 -1。
//
// 为什么用「定长槽位表 + 扫描」而不是以 pointerId 直接作下标：
// 协议里的 pointerId 由客户端分配，可能是任意 uint16（实测 96、111、112…一路递增），
// 而我们的按下状态数组只有 kMaxTouchPoints 个槽位。把 pointerId 当标签、与槽位解耦后，
// 再大的 pointerId 也不会被丢弃。事件量很小（一次拖动也就上千条），O(n) 扫描完全够用。
int DisplayServerMacOSOffscreen::_find_touch_slot(uint16_t p_pointer_id) const {
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (touch_down[i] && touch_slot_pointer[i] == (int)p_pointer_id) {
			return i;
		}
	}
	return -1;
}

// 为一个新 pointerId 分配槽位：优先空槽，全部占用时复用最久未活动者（并打日志）。
int DisplayServerMacOSOffscreen::_acquire_touch_slot(uint16_t p_pointer_id) {
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (!touch_down[i]) {
			touch_down[i] = true;
			touch_slot_pointer[i] = (int)p_pointer_id;
			return i;
		}
	}

	// 同时按下的触点数达到上限。正常手指数量根本到不了这里，但协议允许，
	// 必须保证「不丢新事件、不崩」，所以按「复用最久未活动槽位」的策略腾位置。
	int victim = 0;
	for (int i = 1; i < kMaxTouchPoints; i++) {
		if (touch_last_ms[i] < touch_last_ms[victim]) {
			victim = i;
		}
	}
	const int evicted_id = touch_slot_pointer[victim];
	// 给被顶掉的触点补发一次抬起：Godot 侧的触摸 index 就是 pointerId，
	// 若不抬起而直接改绑，那个 pointerId 会在游戏里永远保持按下（表现为粘住手指）。
	_emit_screen_touch(evicted_id, false, touch_last_x[victim], touch_last_y[victim]);
	input_slot_recycled++;
	print_line(vformat("QRInput: touch slot table full (%d active), recycling least-recently-active slot=%d (evict pointerId=%d, admit pointerId=%d)",
			kMaxTouchPoints, victim, evicted_id, (int)p_pointer_id));
	fflush(stdout);

	touch_down[victim] = true;
	touch_slot_pointer[victim] = (int)p_pointer_id;
	return victim;
}

// 记录一次「无法处理而丢弃」的输入包。绝不静默丢弃：以后看 dropped 计数就能一眼发现异常。
void DisplayServerMacOSOffscreen::_note_dropped(const String &p_reason) {
	input_packets_dropped++;
	print_line("QRInput: dropped input packet (" + p_reason + ")");
	fflush(stdout);
}

void DisplayServerMacOSOffscreen::_emit_screen_touch(int p_index, bool p_pressed, float p_x, float p_y) {
	Ref<InputEventScreenTouch> ev;
	ev.instantiate();
	ev->set_window_id(DisplayServerEnums::MAIN_WINDOW_ID);
	ev->set_index(p_index);
	ev->set_position(Vector2(p_x, p_y));
	ev->set_pressed(p_pressed);
	Input::get_singleton()->parse_input_event(ev);
	input_events_dispatched++;
}

void DisplayServerMacOSOffscreen::_emit_screen_drag(int p_index, float p_x, float p_y, float p_rel_x, float p_rel_y) {
	Ref<InputEventScreenDrag> ev;
	ev.instantiate();
	ev->set_window_id(DisplayServerEnums::MAIN_WINDOW_ID);
	ev->set_index(p_index);
	ev->set_position(Vector2(p_x, p_y));
	ev->set_relative(Vector2(p_rel_x, p_rel_y));
	Input::get_singleton()->parse_input_event(ev);
	input_events_dispatched++;
}

// MARK: - 触摸（原生）

void DisplayServerMacOSOffscreen::_release_stuck_touches(uint64_t p_now_ms) {
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (!touch_down[i]) {
			continue;
		}
		// 防御性判断：时间戳理论上不会超前于 now，但一旦出现（可重入/时钟回拨）必须跳过，
		// 否则无符号相减会溢出成一个极大值，把正常拖动误判成「卡住」。
		if (touch_last_ms[i] >= p_now_ms) {
			continue;
		}
		const uint64_t idle_ms = p_now_ms - touch_last_ms[i];
		if (idle_ms <= kTouchWatchdogMs) {
			continue;
		}
		// 该 index 持续按下、且已超过 kTouchWatchdogMs 没有任何事件（典型原因：UP 包丢失）。
		// 说明：正常拖动会持续产生 MOVE（不断刷新 touch_last_ms），正常长按也会在客户端发 MOVE
		// 时刷新；只有真·长时间无事件（阈值见 kTouchWatchdogMs=10s）才走到这里，避免误伤长按。
		// 强制补发一次「抬起」，把状态清干净，否则后续所有新触摸都会被当成同一次拖动的延续。
		const int pointer_id = touch_slot_pointer[i];
		touch_down[i] = false;
		_emit_screen_touch(pointer_id, false, touch_last_x[i], touch_last_y[i]);
		touch_watchdog_fired++;
		// 记录触发原因与当时的间隔毫秒数，便于事后判断是「真丢 UP」还是「阈值过短误触发」。
		print_line(vformat("QRInput: touch watchdog fired (reason=no_event_while_pressed, idle_ms=%d, threshold_ms=%d) forced release pointerId=%d (slot=%d)",
				(int)idle_ms, (int)kTouchWatchdogMs, pointer_id, i));
		fflush(stdout); // stdout 是管道时默认全缓冲，不主动 flush 会被宿主一直看不到
	}
}

void DisplayServerMacOSOffscreen::_release_all_touches() {
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (!touch_down[i]) {
			continue;
		}
		const int pointer_id = touch_slot_pointer[i];
		touch_down[i] = false;
		_emit_screen_touch(pointer_id, false, touch_last_x[i], touch_last_y[i]);
		print_line(vformat("QRInput: input disconnected, released residual touch pointerId=%d (slot=%d)", pointer_id, i));
	}
}

void DisplayServerMacOSOffscreen::_note_concurrent_multi_touch(int p_active_slot, uint64_t p_now_ms) {
	if (touch_multi_touch_seen) {
		return;
	}
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (i == p_active_slot || !touch_down[i]) {
			continue;
		}
		// 防御：时间戳超前 now 时跳过（无符号相减会溢出）。
		if (touch_last_ms[i] >= p_now_ms) {
			continue;
		}
		if (p_now_ms - touch_last_ms[i] <= kStaleReleaseIdleMs) {
			// 另一个按下触点在阈值内也收到过事件 → 两个 id 正在并发产生事件，判定为真·多点触控。
			// 记录一次即可，之后永久关闭激进释放。
			touch_multi_touch_seen = true;
			print_line(vformat("QRInput: concurrent multi-touch observed (slot=%d and slot=%d both active), disabling stale-touch aggressive release",
					p_active_slot, i));
			fflush(stdout);
			return;
		}
	}
}

void DisplayServerMacOSOffscreen::_release_stale_touches_on_new_down(uint16_t p_new_pointer_id, uint64_t p_now_ms) {
	if (touch_multi_touch_seen) {
		// 已确认是真·多点触控客户端：不做激进释放，避免误伤并发的第二根手指，交回看门狗兜底。
		return;
	}
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (!touch_down[i]) {
			continue;
		}
		// 防御：时间戳超前 now 时跳过（无符号相减会溢出，会把正常触点误判成残留）。
		if (touch_last_ms[i] >= p_now_ms) {
			continue;
		}
		const uint64_t idle_ms = p_now_ms - touch_last_ms[i];
		if (idle_ms <= kStaleReleaseIdleMs) {
			// 阈值内仍有活动：不是残留，保留原状（可能是正常操作中的另一根手指）。
			continue;
		}
		// 该触点已空闲超过阈值，而此刻又来了一个「另一个 pointerId 的新 DOWN」——
		// 判定为「客户端漏发 UP」留下的残留按下，先释放它再派发新 DOWN，
		// 否则 Godot 会把新触点当成同一次拖动的延续，游戏对触摸失去反应。
		const int pointer_id = touch_slot_pointer[i];
		touch_down[i] = false;
		_emit_screen_touch(pointer_id, false, touch_last_x[i], touch_last_y[i]);
		input_stale_released++;
		// 日志写清原因、被释放的 pointerId、空闲毫秒数、以及触发它的新 pointerId，便于事后核对。
		print_line(vformat("QRInput: stale touch released on new DOWN (reason=another_pointer_went_down, idle_ms=%d, threshold_ms=%d) released_pointerId=%d (slot=%d) admitting_pointerId=%d",
				(int)idle_ms, (int)kStaleReleaseIdleMs, pointer_id, i, (int)p_new_pointer_id));
		fflush(stdout);
	}
}

void DisplayServerMacOSOffscreen::_log_input_stats_if_due(uint64_t p_now_ms) {
	if (input_log_last_ms == 0) {
		input_log_last_ms = p_now_ms;
		return;
	}
	if (p_now_ms - input_log_last_ms < 5000) {
		return;
	}
	input_log_last_ms = p_now_ms;

	String down_set;
	for (int i = 0; i < kMaxTouchPoints; i++) {
		if (!touch_down[i]) {
			continue;
		}
		if (!down_set.is_empty()) {
			down_set += ",";
		}
		// 打印 pointerId（而非槽位下标）：诊断要一眼看出客户端用的大 id 有没有被正确处理。
		down_set += itos(touch_slot_pointer[i]);
	}
	if (down_set.is_empty()) {
		down_set = "empty";
	}
	// 日志刻意用 ASCII：Godot 从管道输出时的编码不一定稳定，中文容易出现乱码，
	// 而引擎里既有的日志（如 "Offscreen window can't be resized."）本来就是英文。
	print_line("QRInput: recv=" + itos((int64_t)input_packets_received) +
			" kind[mouse=" + itos((int64_t)input_by_kind[0]) +
			" btn=" + itos((int64_t)input_by_kind[1]) +
			" wheel=" + itos((int64_t)input_by_kind[2]) +
			" touch=" + itos((int64_t)input_by_kind[3]) +
			" key=" + itos((int64_t)input_by_kind[4]) + "]" +
			" touchType[hover=" + itos((int64_t)input_touch_by_type[0]) +
			" down=" + itos((int64_t)input_touch_by_type[1]) +
			" up=" + itos((int64_t)input_touch_by_type[2]) +
			" move=" + itos((int64_t)input_touch_by_type[3]) +
			" cancel=" + itos((int64_t)input_touch_by_type[4]) +
			" btnOnly=" + itos((int64_t)input_touch_by_type[5]) +
			" leave=" + itos((int64_t)input_touch_by_type[6]) +
			" cancelAll=" + itos((int64_t)input_touch_by_type[7]) + "]" +
			" dispatched=" + itos((int64_t)input_events_dispatched) +
			" dropped=" + itos((int64_t)input_packets_dropped) +
			" recycled=" + itos((int64_t)input_slot_recycled) +
			" stale_released=" + itos((int64_t)input_stale_released) +
			" multiTouch=" + String(touch_multi_touch_seen ? "yes" : "no") +
			" downIndices={" + down_set + "}" +
			" watchdog=" + itos((int64_t)touch_watchdog_fired) +
			// 触摸→鼠标改由 Godot 内建合成，这里报告该设置的实际值（我们强制为 true）。
			" emulateMouseFromTouch=" + String(Input::get_singleton()->is_emulating_mouse_from_touch() ? "true" : "false") +
			" connects=" + itos((int64_t)input_connect_count) +
			" flushTicks=" + itos((int64_t)process_events_calls));
	fflush(stdout);
}

void DisplayServerMacOSOffscreen::_handle_input_packet(const uint8_t *p_packet) {
	const uint8_t kind = p_packet[0];
	const uint8_t pressed = p_packet[1];
	const uint16_t code = _rd_u16(p_packet + 2);
	const float x = _rd_f32(p_packet + 4);
	const float y = _rd_f32(p_packet + 8);
	const float dx = _rd_f32(p_packet + 12);
	const float dy = _rd_f32(p_packet + 16);

	input_packets_received++;
	if (kind < 8) {
		input_by_kind[kind]++;
	}
	const uint64_t now_ms = OS::get_singleton()->get_ticks_msec();

	Input *input = Input::get_singleton();
	const DisplayServerEnums::WindowID wid = DisplayServerEnums::MAIN_WINDOW_ID;

	switch (kind) {
		case 0: { // Mouse motion.
			Ref<InputEventMouseMotion> ev;
			ev.instantiate();
			ev->set_window_id(wid);
			ev->set_position(Vector2(x, y));
			ev->set_global_position(Vector2(x, y));
			ev->set_relative(Vector2(dx, dy));
			input->parse_input_event(ev);
		} break;

		case 1: { // Mouse button.
			Ref<InputEventMouseButton> ev;
			ev.instantiate();
			ev->set_window_id(wid);
			ev->set_position(Vector2(x, y));
			ev->set_global_position(Vector2(x, y));
			ev->set_button_index((MouseButton)code);
			ev->set_pressed(pressed != 0);
			input->parse_input_event(ev);
		} break;

		case 2: { // Wheel: emits a pressed + released pair, as Godot expects.
			MouseButton button = MouseButton::NONE;
			float factor = 0.0f;
			if (dy > 0.0f) {
				button = MouseButton::WHEEL_UP;
				factor = dy;
			} else if (dy < 0.0f) {
				button = MouseButton::WHEEL_DOWN;
				factor = -dy;
			} else if (dx > 0.0f) {
				button = MouseButton::WHEEL_RIGHT;
				factor = dx;
			} else if (dx < 0.0f) {
				button = MouseButton::WHEEL_LEFT;
				factor = -dx;
			}
			if (button != MouseButton::NONE) {
				Ref<InputEventMouseButton> down;
				down.instantiate();
				down->set_window_id(wid);
				down->set_position(Vector2(x, y));
				down->set_global_position(Vector2(x, y));
				down->set_button_index(button);
				down->set_factor(factor);
				down->set_pressed(true);
				input->parse_input_event(down);

				Ref<InputEventMouseButton> up;
				up.instantiate();
				up->set_window_id(wid);
				up->set_position(Vector2(x, y));
				up->set_global_position(Vector2(x, y));
				up->set_button_index(button);
				up->set_factor(factor);
				up->set_pressed(false);
				input->parse_input_event(up);
			}
		} break;

		case 3: { // Touch.
			// Godot 侧的触摸 index 直接使用协议里的 pointerId 原值：Godot 内部用
			// HashMap<int, VelocityTrack> 记录触点，不限制 index 大小，所以 96/111/1000 这类大 id
			// 与 0/1/2 一样正常。我们自己只额外维护一张定长「按下状态」表（见 _acquire_touch_slot），
			// 用来做重复 DOWN 判定、UP 匹配与看门狗——绝不因为 pointerId 数值大而丢弃事件。
			const uint16_t pointer_id = code;
			const int index = (int)pointer_id;
			const uint8_t event_type = p_packet[kInputTouchEventOffset];
			const bool has_event_type = p_packet[kInputTouchFlagOffset] == 1;
			if (has_event_type && event_type < 8) {
				input_touch_by_type[event_type]++;
			}

			int slot = _find_touch_slot(pointer_id);

			if (!has_event_type) {
				// 兼容不带 eventType 的旧宿主：沿用原来的 pressed 位语义。
				if (pressed != 0) {
					if (slot >= 0) {
						_emit_screen_drag(index, x, y, dx, dy);
					} else {
						slot = _acquire_touch_slot(pointer_id);
						_emit_screen_touch(index, true, x, y);
					}
				} else {
					if (slot >= 0) {
						touch_down[slot] = false;
						_emit_screen_touch(index, false, x, y);
					} else {
						_note_dropped(vformat("touch up for pointerId=%d which is not pressed (legacy host without eventType)", index));
					}
				}
				if (slot >= 0) {
					touch_last_ms[slot] = now_ms;
					touch_last_x[slot] = x;
					touch_last_y[slot] = y;
				}
				break;
			}

			// 严格按 Moonlight 的 LI_TOUCH_EVENT_* 语义处理。
			// 关键：只有 UP / CANCEL 清除对应 index 的按下状态，CANCEL_ALL 清空全部；
			// HOVER / HOVER_LEAVE / BUTTON_ONLY 不是接触事件，绝不能当成 down/up。
			switch (event_type) {
				case 0x00: // HOVER
				case 0x05: // BUTTON_ONLY
				case 0x06: // HOVER_LEAVE
					break;

				case 0x01: { // DOWN
					if (slot >= 0) {
						// 同一 pointerId 重复 DOWN：视为该触点的位置更新（拖动），既不新建槽位也不重复按下。
						// 依据：Godot 此时已处于 pressed=true，再发一次 ScreenTouch(pressed=true) 会被游戏
						// 当成第二次点击；而 ScreenDrag 既能同步最新位置，又不会重复触发点击。
						_note_concurrent_multi_touch(slot, now_ms);
						_emit_screen_drag(index, x, y, dx, dy);
					} else {
						// 新 pointerId 按下：先清理「其他长时间空闲的残留按下触点」，再派发这个新 DOWN。
						// 顺序很关键：必须先释放旧 id 的 pressed 状态，Godot 才会把新触点当成一次新的点击，
						// 而不是旧触点拖动的延续（这正是「触摸经常卡在按下态、游戏偶发无响应」的根因）。
						_release_stale_touches_on_new_down(pointer_id, now_ms);
						slot = _acquire_touch_slot(pointer_id);
						_emit_screen_touch(index, true, x, y);
					}
					touch_last_ms[slot] = now_ms;
					touch_last_x[slot] = x;
					touch_last_y[slot] = y;
				} break;

				case 0x03: { // MOVE
					if (slot < 0) {
						// MOVE 抢在 DOWN 之前到达（DOWN 包丢失/被丢弃）：自愈——先补一个按下，
						// 否则这个 pointerId 的拖动会被 Godot 整段丢弃。
						slot = _acquire_touch_slot(pointer_id);
						_emit_screen_touch(index, true, x, y);
					}
					_note_concurrent_multi_touch(slot, now_ms);
					_emit_screen_drag(index, x, y, dx, dy);
					touch_last_ms[slot] = now_ms;
					touch_last_x[slot] = x;
					touch_last_y[slot] = y;
				} break;

				case 0x02: // UP
				case 0x04: { // CANCEL
					if (slot >= 0) {
						touch_down[slot] = false;
						_emit_screen_touch(index, false, x, y);
						touch_last_ms[slot] = now_ms;
					} else {
						// 该 pointerId 并未处于按下：不回发虚假抬起（Godot 侧本就没有按下状态），
						// 但仍要记日志 + 计数，否则「客户端重复发 UP」会被误判成引擎在丢事件。
						_note_dropped(vformat("touch up/cancel for pointerId=%d which is not pressed", index));
					}
				} break;

				case 0x07: { // CANCEL_ALL：清空全部按下 index
					for (int i = 0; i < kMaxTouchPoints; i++) {
						if (!touch_down[i]) {
							continue;
						}
						const int pid = touch_slot_pointer[i];
						touch_down[i] = false;
						_emit_screen_touch(pid, false, touch_last_x[i], touch_last_y[i]);
					}
				} break;

				default:
					_note_dropped(vformat("unknown touch eventType=0x%02X pointerId=%d", event_type, index));
					break;
			}
		} break;

		case 4: { // Keyboard.
			Ref<InputEventKey> ev;
			ev.instantiate();
			ev->set_window_id(wid);
			ev->set_keycode((Key)code);
			ev->set_physical_keycode((Key)code);
			ev->set_pressed(pressed != 0);
			input->parse_input_event(ev);
		} break;

		default:
			// 非法 kind：不静默丢弃，记日志 + 计数，便于发现协议/宿主异常。
			_note_dropped(vformat("unknown input kind=%d (not in 0..4)", (int)kind));
			break;
	}
}

DisplayServer *DisplayServerMacOSOffscreen::create_func(const String &p_rendering_driver, DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::VSyncMode p_vsync_mode, uint32_t p_flags, const Vector2i *p_position, const Vector2i &p_resolution, int p_screen, DisplayServerEnums::Context p_context, int64_t /* p_parent_window */, Error &r_error) {
	return memnew(DisplayServerMacOSOffscreen(p_rendering_driver, p_mode, p_vsync_mode, p_flags, p_position, p_resolution, p_screen, p_context, r_error));
}

Vector<String> DisplayServerMacOSOffscreen::get_rendering_drivers_func() {
	Vector<String> drivers;

#if defined(METAL_ENABLED)
	drivers.push_back("metal");
#endif

	return drivers;
}

void DisplayServerMacOSOffscreen::register_offscreen_driver() {
	register_create_function("offscreen", create_func, get_rendering_drivers_func);
}

// MARK: - Mouse

void DisplayServerMacOSOffscreen::_mouse_apply_mode(DisplayServerEnums::MouseMode p_prev_mode, DisplayServerEnums::MouseMode p_new_mode) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::warp_mouse(const Point2i &p_position) {
	_THREAD_SAFE_METHOD_
	Input::get_singleton()->set_mouse_position(p_position);
}

Point2i DisplayServerMacOSOffscreen::mouse_get_position() const {
	_THREAD_SAFE_METHOD_
	return Point2i();
}

BitField<MouseButtonMask> DisplayServerMacOSOffscreen::mouse_get_button_state() const {
	return MouseButtonMask::NONE;
}

// MARK: - Events

void DisplayServerMacOSOffscreen::window_set_rect_changed_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window) {
	window_resize_callbacks[p_window] = p_callable;
}

void DisplayServerMacOSOffscreen::window_set_window_event_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window) {
	window_event_callbacks[p_window] = p_callable;
}

void DisplayServerMacOSOffscreen::window_set_input_event_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window) {
	input_event_callbacks[p_window] = p_callable;
}

void DisplayServerMacOSOffscreen::window_set_input_text_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window) {
	input_text_callbacks[p_window] = p_callable;
}

void DisplayServerMacOSOffscreen::window_set_drop_files_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::process_events() {
	process_events_calls++;
	Input *input = Input::get_singleton();
	input->flush_buffered_events();
}

void DisplayServerMacOSOffscreen::_dispatch_input_events(const Ref<InputEvent> &p_event) {
	Ref<InputEventFromWindow> event_from_window = p_event;
	DisplayServerEnums::WindowID window_id = DisplayServerEnums::INVALID_WINDOW_ID;
	if (event_from_window.is_valid()) {
		window_id = event_from_window->get_window_id();
	}
	DisplayServerMacOSOffscreen *ds = (DisplayServerMacOSOffscreen *)DisplayServer::get_singleton();
	ds->send_input_event(p_event, window_id);
}

void DisplayServerMacOSOffscreen::send_input_event(const Ref<InputEvent> &p_event, DisplayServerEnums::WindowID p_id) const {
	if (p_id != DisplayServerEnums::INVALID_WINDOW_ID) {
		const Callable *cb = input_event_callbacks.getptr(p_id);
		if (cb) {
			_window_callback(*cb, p_event);
		}
	} else {
		for (const KeyValue<DisplayServerEnums::WindowID, Callable> &E : input_event_callbacks) {
			_window_callback(E.value, p_event);
		}
	}
}

void DisplayServerMacOSOffscreen::send_input_text(const String &p_text, DisplayServerEnums::WindowID p_id) const {
	const Callable *cb = input_text_callbacks.getptr(p_id);
	if (cb) {
		_window_callback(*cb, p_text);
	}
}

void DisplayServerMacOSOffscreen::send_window_event_by_id(DisplayServerEnums::WindowEvent p_event, DisplayServerEnums::WindowID p_id) const {
	const Callable *cb = window_event_callbacks.getptr(p_id);
	if (cb) {
		_window_callback(*cb, int(p_event));
	}
}

void DisplayServerMacOSOffscreen::_window_callback(const Callable &p_callable, const Variant &p_arg) const {
	if (p_callable.is_valid()) {
		p_callable.call(p_arg);
	}
}

// MARK: -

bool DisplayServerMacOSOffscreen::has_feature(DisplayServerEnums::Feature p_feature) const {
	switch (p_feature) {
		case DisplayServerEnums::FEATURE_CLIPBOARD:
		case DisplayServerEnums::FEATURE_TEXT_TO_SPEECH:
			return true;
		default:
			return false;
	}
}

String DisplayServerMacOSOffscreen::get_name() const {
	return "offscreen";
}

int DisplayServerMacOSOffscreen::get_screen_count() const {
	return 1;
}

Point2i DisplayServerMacOSOffscreen::screen_get_position(int p_screen) const {
	_THREAD_SAFE_METHOD_

	p_screen = _get_screen_index(p_screen);
	int screen_count = get_screen_count();
	ERR_FAIL_INDEX_V(p_screen, screen_count, Point2i());

	return Point2i(0, 0);
}

Size2i DisplayServerMacOSOffscreen::screen_get_size(int p_screen) const {
	_THREAD_SAFE_METHOD_

	p_screen = _get_screen_index(p_screen);
	int screen_count = get_screen_count();
	ERR_FAIL_INDEX_V(p_screen, screen_count, Size2i());

	return window_get_size(DisplayServerEnums::MAIN_WINDOW_ID);
}

Rect2i DisplayServerMacOSOffscreen::screen_get_usable_rect(int p_screen) const {
	_THREAD_SAFE_METHOD_

	p_screen = _get_screen_index(p_screen);
	int screen_count = get_screen_count();
	ERR_FAIL_INDEX_V(p_screen, screen_count, Rect2i());

	return Rect2i(screen_get_position(p_screen), screen_get_size(p_screen));
}

int DisplayServerMacOSOffscreen::screen_get_dpi(int p_screen) const {
	_THREAD_SAFE_METHOD_

	p_screen = _get_screen_index(p_screen);
	int screen_count = get_screen_count();
	ERR_FAIL_INDEX_V(p_screen, screen_count, 72);

	return 96;
}

float DisplayServerMacOSOffscreen::screen_get_scale(int p_screen) const {
	_THREAD_SAFE_METHOD_
	return 1.0f;
}

Vector<DisplayServerEnums::WindowID> DisplayServerMacOSOffscreen::get_window_list() const {
	Vector<DisplayServerEnums::WindowID> list;
	list.push_back(DisplayServerEnums::MAIN_WINDOW_ID);
	return list;
}

DisplayServerEnums::WindowID DisplayServerMacOSOffscreen::get_window_at_screen_position(const Point2i &p_position) const {
	return DisplayServerEnums::MAIN_WINDOW_ID;
}

void DisplayServerMacOSOffscreen::window_attach_instance_id(ObjectID p_instance, DisplayServerEnums::WindowID p_window) {
	window_attached_instance_id[p_window] = p_instance;
}

ObjectID DisplayServerMacOSOffscreen::window_get_attached_instance_id(DisplayServerEnums::WindowID p_window) const {
	return window_attached_instance_id[p_window];
}

void DisplayServerMacOSOffscreen::window_set_title(const String &p_title, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

int DisplayServerMacOSOffscreen::window_get_current_screen(DisplayServerEnums::WindowID p_window) const {
	_THREAD_SAFE_METHOD_
	ERR_FAIL_COND_V(p_window != DisplayServerEnums::MAIN_WINDOW_ID, DisplayServerEnums::INVALID_SCREEN);

	return 0;
}

void DisplayServerMacOSOffscreen::window_set_current_screen(int p_screen, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

Point2i DisplayServerMacOSOffscreen::window_get_position(DisplayServerEnums::WindowID p_window) const {
	return Point2i();
}

Point2i DisplayServerMacOSOffscreen::window_get_position_with_decorations(DisplayServerEnums::WindowID p_window) const {
	return Point2i();
}

void DisplayServerMacOSOffscreen::window_set_position(const Point2i &p_position, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_set_transient(DisplayServerEnums::WindowID p_window, DisplayServerEnums::WindowID p_parent) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_set_max_size(const Size2i p_size, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

Size2i DisplayServerMacOSOffscreen::window_get_max_size(DisplayServerEnums::WindowID p_window) const {
	return Size2i();
}

void DisplayServerMacOSOffscreen::window_set_min_size(const Size2i p_size, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

Size2i DisplayServerMacOSOffscreen::window_get_min_size(DisplayServerEnums::WindowID p_window) const {
	return Size2i();
}

void DisplayServerMacOSOffscreen::window_set_size(const Size2i p_size, DisplayServerEnums::WindowID p_window) {
	print_line("Offscreen window can't be resized.");
}

Size2i DisplayServerMacOSOffscreen::window_get_size(DisplayServerEnums::WindowID p_window) const {
#if defined(RD_ENABLED)
	if (rendering_context) {
		RenderingContextDriver::SurfaceID surface = rendering_context->surface_get_from_window(p_window);
		if (surface != 0) {
			uint32_t width = rendering_context->surface_get_width(surface);
			uint32_t height = rendering_context->surface_get_height(surface);
			if (width > 0 && height > 0) {
				return Size2i(width, height);
			}
		}
	}
#endif
	return window_size;
}

Size2i DisplayServerMacOSOffscreen::window_get_size_with_decorations(DisplayServerEnums::WindowID p_window) const {
	return window_get_size(p_window);
}

void DisplayServerMacOSOffscreen::window_set_mode(DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

DisplayServerEnums::WindowMode DisplayServerMacOSOffscreen::window_get_mode(DisplayServerEnums::WindowID p_window) const {
	return DisplayServerEnums::WindowMode::WINDOW_MODE_WINDOWED;
}

bool DisplayServerMacOSOffscreen::window_is_maximize_allowed(DisplayServerEnums::WindowID p_window) const {
	return false;
}

void DisplayServerMacOSOffscreen::window_set_flag(DisplayServerEnums::WindowFlags p_flag, bool p_enabled, DisplayServerEnums::WindowID p_window) {
	if (p_flag == DisplayServerEnums::WINDOW_FLAG_TRANSPARENT && p_window == DisplayServerEnums::MAIN_WINDOW_ID) {
		transparent = p_enabled;
	}
}

bool DisplayServerMacOSOffscreen::window_get_flag(DisplayServerEnums::WindowFlags p_flag, DisplayServerEnums::WindowID p_window) const {
	if (p_flag == DisplayServerEnums::WINDOW_FLAG_TRANSPARENT && p_window == DisplayServerEnums::MAIN_WINDOW_ID) {
		return transparent;
	}
	return false;
}

void DisplayServerMacOSOffscreen::window_request_attention(DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_set_taskbar_progress_value(float p_value, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_set_taskbar_progress_state(DisplayServerEnums::ProgressState p_state, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_move_to_foreground(DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

bool DisplayServerMacOSOffscreen::window_is_focused(DisplayServerEnums::WindowID p_window) const {
	return true;
}

float DisplayServerMacOSOffscreen::screen_get_max_scale() const {
	return 1.0f;
}

bool DisplayServerMacOSOffscreen::window_can_draw(DisplayServerEnums::WindowID p_window) const {
	return true;
}

bool DisplayServerMacOSOffscreen::can_any_window_draw() const {
	return true;
}

void DisplayServerMacOSOffscreen::window_set_ime_active(const bool p_active, DisplayServerEnums::WindowID p_window) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::window_set_ime_position(const Point2i &p_pos, DisplayServerEnums::WindowID p_window) {
	ime_last_position = p_pos;
}

DisplayServerMacOSBase::HDROutput &DisplayServerMacOSOffscreen::_get_hdr_output(DisplayServerEnums::WindowID p_window) {
	return hdr_output;
}

const DisplayServerMacOSBase::HDROutput &DisplayServerMacOSOffscreen::_get_hdr_output(DisplayServerEnums::WindowID p_window) const {
	return hdr_output;
}

void DisplayServerMacOSOffscreen::window_get_edr_values(DisplayServerEnums::WindowID p_window, CGFloat *r_max_potential_edr_value, CGFloat *r_max_edr_value) const {
	_THREAD_SAFE_METHOD_

#define SET_VAL(v, val) \
	if (v) { \
		*v = val; \
	}

	SET_VAL(r_max_potential_edr_value, 1.0);
	SET_VAL(r_max_edr_value, 1.0);

#undef SET_VAL
}

void DisplayServerMacOSOffscreen::window_set_vsync_mode(DisplayServerEnums::VSyncMode p_vsync_mode, DisplayServerEnums::WindowID p_window) {
#if defined(RD_ENABLED)
	if (rendering_context) {
		rendering_context->window_set_vsync_mode(p_window, p_vsync_mode);
	}
#endif
}

DisplayServerEnums::VSyncMode DisplayServerMacOSOffscreen::window_get_vsync_mode(DisplayServerEnums::WindowID p_window) const {
	_THREAD_SAFE_METHOD_
#if defined(RD_ENABLED)
	if (rendering_context) {
		return rendering_context->window_get_vsync_mode(p_window);
	}
#endif
	return DisplayServerEnums::VSYNC_DISABLED;
}

void DisplayServerMacOSOffscreen::cursor_set_shape(DisplayServerEnums::CursorShape p_shape) {
	cursor_shape = p_shape;
}

void DisplayServerMacOSOffscreen::cursor_set_custom_image(const Ref<Resource> &p_cursor, DisplayServerEnums::CursorShape p_shape, const Vector2 &p_hotspot) {
	// Not supported.
}

void DisplayServerMacOSOffscreen::swap_buffers() {
}
