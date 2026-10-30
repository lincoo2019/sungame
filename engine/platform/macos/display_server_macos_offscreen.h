/**************************************************************************/
/*  display_server_macos_offscreen.h                                      */
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

#pragma once

#include "core/os/thread.h"

#include "display_server_macos_base.h"

@class CALayer;
class InputEvent;

/// A headless macOS display server that renders into a host-provided IOSurface.
///
/// No NSWindow / NSView / CAContext is created. The framebuffer is a Metal
/// texture backed by the IOSurface whose ID is passed via the
/// `QR_IOSURFACE_ID` environment variable. Window size is taken from
/// `QR_OFFSCREEN_W` / `QR_OFFSCREEN_H` (or the IOSurface dimensions).
class DisplayServerMacOSOffscreen : public DisplayServerMacOSBase {
	GDSOFTCLASS(DisplayServerMacOSOffscreen, DisplayServerMacOSBase)

	HashMap<DisplayServerEnums::WindowID, ObjectID> window_attached_instance_id;

	HashMap<DisplayServerEnums::WindowID, Callable> window_event_callbacks;
	HashMap<DisplayServerEnums::WindowID, Callable> window_resize_callbacks;
	HashMap<DisplayServerEnums::WindowID, Callable> input_event_callbacks;
	HashMap<DisplayServerEnums::WindowID, Callable> input_text_callbacks;

	DisplayServerEnums::WindowID window_id_counter = DisplayServerEnums::MAIN_WINDOW_ID;

	bool transparent = false;
	Size2i window_size = Size2i(1920, 1080);

	CALayer *layer = nullptr;

	String rendering_driver;

	HDROutput hdr_output;

	Point2i ime_last_position;

	// External input injection over a Unix domain stream socket.
	//
	// The host process listens on the socket whose path is given by the
	// `QR_INPUT_SOCKET` environment variable; this background thread connects
	// (and reconnects on disconnect) and turns the fixed-size packets into
	// InputEvents. When the variable is empty, no thread is started at all.
	Thread input_thread;
	SafeFlag input_thread_should_stop;
	bool input_thread_started = false;

	// 游戏音频回传（Engine → Host）。
	//
	// 引擎用 Godot 自己的 AudioEffectCapture 抓 Master 总线混音（而不是改音频驱动内部），
	// 由本后台线程按 5ms 粒度读出 PCM，经反向通道（见 drivers/metal/qr_reverse_channel.h）回传；
	// 宿主侧再编 Opus 送进 Moonlight 音频通道。本地静音由引擎侧把 Master 总线静音实现
	// （捕获跑在总线音量整形之前，因此不被静音影响），既不需要系统音频录制授权，
	// 也不需要会把音频拖慢的 Dummy 驱动。
	//
	// 仅当 `QR_GAME_AUDIO=1` 且 `QR_INPUT_SOCKET` 非空时启动；音频不可用时只打日志，绝不影响视频/输入。
	Thread audio_thread;
	SafeFlag audio_thread_should_stop;
	bool audio_thread_started = false;

	static void _audio_thread_func(void *p_userdata);
	void _audio_thread_loop();

	/// 同时按下的触点数上限。
	///
	/// 关键：这里的 64 只是「我们自己跟踪按下状态」所用槽位表的容量，**与协议里的 pointerId 空间无关**。
	/// 客户端分配的 pointerId 可能是任意 uint16（实测会一路涨到 96、111、112…），绝不能拿它直接当数组下标。
	/// Godot 事件里的 index 直接使用 pointerId 原值（Godot 内部用 HashMap 记录触点，不限制 index 大小）。
	static constexpr int kMaxTouchPoints = 64;
	/// 触摸看门狗超时（毫秒）：某 index 持续按下但这么久没有任何事件，才强制补发抬起。
	///
	/// 为什么从 3 秒放宽到 10 秒（第一版曾用 3000）：
	///   Moonlight 客户端只在手指**真正移动**时才发 MOVE。玩家「按住植物不放」时如果手指不动，
	///   引擎侧就收不到任何事件——此时 3 秒阈值会把**正常的长按**误判成「UP 丢失卡住」并强行抬起，
	///   反而打断操作。10 秒已远大于正常拖动的连续 MOVE 间隔（几十毫秒级），也大于玩家一次
	///   停顿思考的合理时长；丢 UP 这种真异常则最多 10 秒后自愈，是可控的代价。
	/// 另外，收到 CANCEL_ALL / 输入连接断开时会立刻清理（见 _handle_input_packet / _release_all_touches），
	/// 不依赖这个超时。
	///
	/// 复核结论（本次修复之后）：**保留 10000ms，不缩短**。
	///   「主修复」（新 DOWN 到来时清理残留触点，见 kStaleReleaseIdleMs）已经能在下一次点击时
	///   立刻清掉丢 UP 留下的残留，看门狗退化为最后兜底，10s 只在「丢 UP 后玩家再也不点任何东西」
	///   时才会走到；把它缩到 3s 会重新踩回上面的误伤陷阱——因为静止长按（手指按住不动）
	///   在客户端侧**根本不产生任何 MOVE**，3s 无事件正是正常长按的常态，会被误判成卡住而抬起。
	static constexpr uint64_t kTouchWatchdogMs = 10000;
	/// 「新触摸按下时清理残留触点」所用的空闲阈值（毫秒）。
	///
	/// 判据（为什么）：
	///   该客户端每次点击都分配一个**新的** pointerId，且只在手指真正移动时才发 MOVE，
	///   手指按住不动时没有任何事件。若一次点击的 UP 丢失，该 pointerId 会一直保持按下，
	///   直到 10s 看门狗才被抬走——期间所有新触摸都会被 Godot 当成同一次拖动的延续，
	///   表现为「游戏对触摸没反应」。所以在收到「另一个 pointerId 的新 DOWN」时，
	///   把已经空闲超过本阈值的其他按下触点**先释放再派发新 DOWN**。
	///
	/// 取值依据（为什么是 500ms）：
	///   - 正常拖动/连点的相邻事件间隔是几十毫秒（MOVE 基本每帧一次），真人两次点击的间隔
	///     通常也 ≥300~500ms；而「丢 UP 的残留」在下一次点击到来时往往已空闲数百毫秒以上。
	///   - 阈值太小（如 100ms）会在正常操作的抖动间隙里把仍在用的触点误释放；
	///     太大（如 2s）则要等更久才自愈。500ms 取两者之间，且远小于看门狗 10s。
	///   - 真正的多点触控另有开关保护（见 touch_multi_touch_seen），不依赖这个阈值区分。
	static constexpr uint64_t kStaleReleaseIdleMs = 500;
	/// 单批最多消费的输入包数（配合读缓冲大小，防止一波洪峰长时间占住输入线程）
	static constexpr int kMaxPacketsPerBatch = 64;

	static void _input_thread_func(void *p_userdata);
	void _input_thread_loop();
	void _handle_input_packet(const uint8_t *p_packet);

	/// 在槽位表里查找已绑定该 pointerId 的槽位；未绑定返回 -1。
	int _find_touch_slot(uint16_t p_pointer_id) const;
	/// 为一个新 pointerId 分配槽位：优先空槽，满则复用最久未活动者（并打日志）。
	int _acquire_touch_slot(uint16_t p_pointer_id);

	/// 记录一次「无法处理而丢弃」的输入包：打日志 + 计数，绝不静默丢弃。
	void _note_dropped(const String &p_reason);

	/// 收到「另一个 pointerId 的新 DOWN」时，把已空闲超过 kStaleReleaseIdleMs 的其他按下触点
	/// 强制释放（补发抬起 + 清槽位 + 日志 + 计数），再让调用方派发这个新 DOWN。
	/// 目的：自愈「客户端漏发 UP」导致的残留按下，避免新触摸被当成旧触点拖动的延续。
	void _release_stale_touches_on_new_down(uint16_t p_new_pointer_id, uint64_t p_now_ms);
	/// 判定是否真·多点触控：除 p_active_slot 外还有另一个按下槽位在阈值内收到过事件。
	/// 一旦判定为真多点，就永久关闭上面的激进释放（避免误伤并发的第二根手指）。
	void _note_concurrent_multi_touch(int p_active_slot, uint64_t p_now_ms);

	/// 派发一个 ScreenTouch / ScreenDrag（都会累加「派发事件数」计数）
	void _emit_screen_touch(int p_index, bool p_pressed, float p_x, float p_y);
	void _emit_screen_drag(int p_index, float p_x, float p_y, float p_rel_x, float p_rel_y);

	/// 看门狗：把长时间无事件的按下触点强制抬起
	void _release_stuck_touches(uint64_t p_now_ms);
	/// 连接断开时把仍按下的触点全部抬起，避免状态跨连接残留
	void _release_all_touches();
	/// 每 5 秒打印一行输入诊断（包分类计数 / 触摸类型计数 / 按下 pointerId 集合 / 丢弃计数 / 看门狗次数）
	void _log_input_stats_if_due(uint64_t p_now_ms);

	// 触摸按下状态。只由输入线程访问，无需加锁。
	//
	// 这是一张「pointerId → 槽位」的定长映射表：touch_down[i] 为真时，touch_slot_pointer[i]
	// 存该槽位当前绑定的大 pointerId。旧实现直接以 pointerId 作下标，导致 pointerId ≥ 64
	// 的触摸被整批静默丢弃；现在 pointerId 只是标签，与槽位下标解耦。
	bool touch_down[kMaxTouchPoints] = { false };
	int touch_slot_pointer[kMaxTouchPoints] = { 0 };
	uint64_t touch_last_ms[kMaxTouchPoints] = { 0 };
	float touch_last_x[kMaxTouchPoints] = { 0 };
	float touch_last_y[kMaxTouchPoints] = { 0 };

	// 诊断计数。只由输入线程访问，无需加锁。
	uint64_t input_packets_received = 0;
	uint64_t input_events_dispatched = 0;
	/// 诊断：因无法处理而丢弃的输入包数（未知 kind、未按下的 UP/CANCEL、未知 eventType 等）。
	uint64_t input_packets_dropped = 0;
	/// 诊断：因同时按下触点数达到上限而回收最久未活动槽位的次数。
	uint64_t input_slot_recycled = 0;
	/// 诊断：因「新 DOWN 到来时清理残留按下触点」而强制释放的次数。
	uint64_t input_stale_released = 0;
	/// 本会话是否观察到「两个不同 pointerId 同时按下且都在产生事件」——即真·多点触控。
	///
	/// 用途：作为「新 DOWN 时激进释放其他按下触点」的开关。该客户端是单点交互
	/// （每次点击一个新 pointerId，任何时刻只有一个 id 在收事件），一旦观察到真并发多点，
	/// 说明面对的是另一种客户端，激进释放可能误伤，于是永久关闭它、交回 10s 看门狗兜底。
	/// 判据只用「两个 id 都在收到事件」（某个 id 的 MOVE / 重复 DOWN 时，另一个 id 仍在阈值内活动），
	/// 刻意不用「新 DOWN 到来时另一个 id 仍按下」——否则单点客户端的快速连点（前一次 UP 丢失）
	/// 会被误判成多点，反而把主修复关掉。
	bool touch_multi_touch_seen = false;
	uint64_t input_by_kind[8] = { 0 };
	uint64_t input_touch_by_type[8] = { 0 };
	uint64_t touch_watchdog_fired = 0;
	uint64_t input_connect_count = 0;
	uint64_t input_log_last_ms = 0;
	/// 诊断：主循环调用 process_events()（即 flush_buffered_events）的次数
	uint64_t process_events_calls = 0;

	HDROutput &_get_hdr_output(DisplayServerEnums::WindowID p_window) override;
	const HDROutput &_get_hdr_output(DisplayServerEnums::WindowID p_window) const override;

	void _mouse_apply_mode(DisplayServerEnums::MouseMode p_prev_mode, DisplayServerEnums::MouseMode p_new_mode) override;

public:
	static void register_offscreen_driver();
	static DisplayServer *create_func(const String &p_rendering_driver, DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::VSyncMode p_vsync_mode, uint32_t p_flags, const Vector2i *p_position, const Vector2i &p_resolution, int p_screen, DisplayServerEnums::Context p_context, int64_t p_parent_window, Error &r_error);
	static Vector<String> get_rendering_drivers_func();

	// MARK: - Events

	virtual void process_events() override;

	virtual void window_set_rect_changed_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_window_event_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_input_event_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_input_text_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_drop_files_callback(const Callable &p_callable, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;

	static void _dispatch_input_events(const Ref<InputEvent> &p_event);
	void send_input_event(const Ref<InputEvent> &p_event, DisplayServerEnums::WindowID p_id = DisplayServerEnums::MAIN_WINDOW_ID) const;
	void send_input_text(const String &p_text, DisplayServerEnums::WindowID p_id = DisplayServerEnums::MAIN_WINDOW_ID) const;
	virtual void send_window_event_by_id(DisplayServerEnums::WindowEvent p_event, DisplayServerEnums::WindowID p_id = DisplayServerEnums::MAIN_WINDOW_ID) const override;
	void _window_callback(const Callable &p_callable, const Variant &p_arg) const;

	// MARK: - Mouse

	virtual void warp_mouse(const Point2i &p_position) override;
	virtual Point2i mouse_get_position() const override;
	virtual BitField<MouseButtonMask> mouse_get_button_state() const override;

	// MARK: - Window

	virtual bool has_feature(DisplayServerEnums::Feature p_feature) const override;
	virtual String get_name() const override;

	virtual int get_screen_count() const override;
	virtual Point2i screen_get_position(int p_screen = DisplayServerEnums::SCREEN_OF_MAIN_WINDOW) const override;
	virtual Size2i screen_get_size(int p_screen = DisplayServerEnums::SCREEN_OF_MAIN_WINDOW) const override;
	virtual Rect2i screen_get_usable_rect(int p_screen = DisplayServerEnums::SCREEN_OF_MAIN_WINDOW) const override;
	virtual int screen_get_dpi(int p_screen = DisplayServerEnums::SCREEN_OF_MAIN_WINDOW) const override;
	virtual float screen_get_scale(int p_screen = DisplayServerEnums::SCREEN_OF_MAIN_WINDOW) const override;
	virtual Vector<DisplayServerEnums::WindowID> get_window_list() const override;

	virtual DisplayServerEnums::WindowID get_window_at_screen_position(const Point2i &p_position) const override;

	virtual void window_attach_instance_id(ObjectID p_instance, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual ObjectID window_get_attached_instance_id(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_set_title(const String &p_title, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;

	virtual int window_get_current_screen(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;
	virtual void window_set_current_screen(int p_screen, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;

	virtual Point2i window_get_position(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;
	virtual Point2i window_get_position_with_decorations(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;
	virtual void window_set_position(const Point2i &p_position, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;

	virtual void window_set_transient(DisplayServerEnums::WindowID p_window, DisplayServerEnums::WindowID p_parent) override;

	virtual void window_set_max_size(const Size2i p_size, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual Size2i window_get_max_size(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_set_min_size(const Size2i p_size, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual Size2i window_get_min_size(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_set_size(const Size2i p_size, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual Size2i window_get_size(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;
	virtual Size2i window_get_size_with_decorations(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_set_mode(DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual DisplayServerEnums::WindowMode window_get_mode(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual bool window_is_maximize_allowed(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_set_flag(DisplayServerEnums::WindowFlags p_flag, bool p_enabled, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual bool window_get_flag(DisplayServerEnums::WindowFlags p_flag, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual void window_request_attention(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_taskbar_progress_value(float p_value, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_taskbar_progress_state(DisplayServerEnums::ProgressState p_state, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_move_to_foreground(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual bool window_is_focused(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual float screen_get_max_scale() const override;

	virtual bool window_can_draw(DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) const override;

	virtual bool can_any_window_draw() const override;

	virtual void window_set_ime_active(const bool p_active, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual void window_set_ime_position(const Point2i &p_pos, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;

	virtual void window_set_vsync_mode(DisplayServerEnums::VSyncMode p_vsync_mode, DisplayServerEnums::WindowID p_window = DisplayServerEnums::MAIN_WINDOW_ID) override;
	virtual DisplayServerEnums::VSyncMode window_get_vsync_mode(DisplayServerEnums::WindowID p_vsync_mode) const override;

	virtual void cursor_set_shape(DisplayServerEnums::CursorShape p_shape) override;
	virtual void cursor_set_custom_image(const Ref<Resource> &p_cursor, DisplayServerEnums::CursorShape p_shape = DisplayServerEnums::CURSOR_ARROW, const Vector2 &p_hotspot = Vector2()) override;

	void window_get_edr_values(DisplayServerEnums::WindowID p_window, CGFloat *r_max_potential_edr_value, CGFloat *r_max_edr_value) const override;

	virtual void swap_buffers() override;

	DisplayServerMacOSOffscreen(const String &p_rendering_driver, DisplayServerEnums::WindowMode p_mode, DisplayServerEnums::VSyncMode p_vsync_mode, uint32_t p_flags, const Vector2i *p_position, const Vector2i &p_resolution, int p_screen, DisplayServerEnums::Context p_context, Error &r_error);
	~DisplayServerMacOSOffscreen();
};
