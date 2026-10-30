/**************************************************************************/
/*  qr_reverse_channel.h                                                  */
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
/* TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE     */
/* SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.                 */
/**************************************************************************/

#pragma once

// 宿主 ↔ 无窗口游戏 的「反向通道」（游戏 → 宿主）。
//
// 这条通道复用宿主下发输入的那条 Unix socket 的反方向（全双工）。它承载两类消息：
//
//   1) 帧完成通知：游戏渲染线程在「GPU 真正写完一块 IOSurface」后回写 (seq, slot)，
//      宿主据此只读取已完成的那块缓冲，避免读到 GPU 正在写的半帧（撕裂）——撕裂正是
//      客户端失去同步、反复请求关键帧却始终无法恢复的根因之一。
//   2) 游戏音频 PCM：引擎用 AudioEffectCapture 抓 Master 总线混音，按 5ms 量级读出
//      交织 Int16 立体声回传；宿主侧再编 Opus 送进 Moonlight 音频通道。
//
// 统一分帧格式（每条消息，全部小端）：
//   [0]     type        消息类型（见 MsgType）
//   [1]     reserved    保留，恒为 0
//   [2..3]  payload_len 负载长度（uint16 LE）
//   [4..]   payload     负载
//
//   帧完成（type=0）负载恒为 8 字节： [0..3] frame_seq、[4..7] buffer_index。
//   音频（type=1）负载为交织 Int16 LE 立体声：每帧 2 声道 * 2 字节。
//
// 为什么引入分帧头（而不是像以前那样裸发 8 字节）：同一个方向现在要传两种数据，
// 宿主必须能明确区分；帧长也随音频块大小变化，必须带长度。宿主按同一格式解析。
//
// fd 由 offscreen 显示服务器的输入线程在连接建立后注册、断开前注销；渲染线程与音频线程按规定格式发送。
//
// 发送必须真正「绝不阻塞」，原因（实测过的严重缺陷）：帧完成通知由 Metal 的 command buffer
// completed handler 在 GPU 完成回调线程上发出。宿主目前还没有消费这条通道，接收缓冲会一直被写满；
// 一旦这里的发送被阻塞，完成回调线程就卡住 → Metal 的 command buffer 无法回收 → 队列耗尽后
// 游戏主循环会在 `command_begin_render_pass → command_buffer()` 里永久阻塞（画面定格、输入再也
// 无法被主线程 flush，表现为「画面正常但触摸卡死」）。
//
// 关键细节：Darwin 的 stream socket 上 `MSG_DONTWAIT` 并不可靠，实测仅靠它仍会阻塞，
// 因此每次发送前先用 `poll(POLLOUT, timeout=0)` 探可写性，不可写就直接丢弃；同时给 fd 设了
// 1ms 的 SO_SNDTIMEO 作为竞态兜底。为了不把「阻塞」引入完成回调线程，这里**不加互斥锁**：
// 每条消息都用一次 send() 整体写出，内核在写期间持有 socket 锁，因此两条线程的消息不会互相交错；
// 再把 SO_SNDBUF 调大，使「poll 判定可写 → 一次 send 必然整条写完」成立，避免被短写截断。

#include <atomic>
#include <cstdint>
#include <cstring>

#include <poll.h>
#include <sys/socket.h>
#include <sys/time.h>
#include <sys/types.h>

namespace QRReverseChannel {

enum MsgType : uint8_t {
	MSG_FRAME_COMPLETE = 0,
	MSG_AUDIO_PCM = 1,
};

constexpr size_t kHeaderSize = 4;
// 单条消息负载上限：音频一包最多 960 帧 * 2 声道 * 2 字节 = 3840，取 4096 留余量。
constexpr size_t kMaxPayload = 4096;

// C++17 的 inline 变量在整个进程内只有一份实例，头文件被多个 TU 包含也不会重复定义。
// fd 由输入线程写入、渲染/音频线程读取，必须原子化，否则重连时可能出现「读到已关闭/被复用的 fd」。
inline std::atomic<int> fd{ -1 };
inline std::atomic<uint64_t> sent_count{ 0 };
inline std::atomic<uint64_t> dropped_count{ 0 };
inline std::atomic<uint64_t> retry_count{ 0 };

inline void set_fd(int p_fd) {
	// 兜底：给写侧一个极短的发送超时。即便下面 poll(POLLOUT) 与 send 之间存在竞态，
	// 处于 GPU 完成回调线程上的这次写也最多等 1ms 就返回 EWOULDBLOCK，不会永久阻塞。
	struct timeval tv;
	tv.tv_sec = 0;
	tv.tv_usec = 1000;
	::setsockopt(p_fd, SOL_SOCKET, SO_SNDTIMEO, &tv, sizeof(tv));
	// 加大发送缓冲：消息很小（≤ 4100 字节）而缓冲区足够大，能让「poll 可写 → 一次 send
	// 整条写出」成立；否则极满时可能只写出一部分，把流截断、让宿主无法解析。
	int sndbuf = 1 << 20;
	::setsockopt(p_fd, SOL_SOCKET, SO_SNDBUF, &sndbuf, sizeof(sndbuf));
	fd.store(p_fd, std::memory_order_release);
}

inline void clear_fd() {
	fd.store(-1, std::memory_order_release);
}

inline int get_fd() {
	return fd.load(std::memory_order_acquire);
}

// 发送一条分帧消息。p_wait_ms = 0：绝不阻塞，先 poll 探可写，不可写直接丢弃并计数
// （GPU 完成回调线程只能用这一档）。p_wait_ms > 0：不可写时以 1ms 为步长最多等这么久再重试，
// 仍失败才丢弃——**只允许音频线程用**（它是自驱节拍线程，环形缓冲有 0.5s 余量，等几十毫秒无害）。
// 返回是否整条写出（false 表示被丢弃）。
inline bool send_message(uint8_t p_type, const uint8_t *p_payload, uint16_t p_len, int p_wait_ms = 0) {
	const int f = fd.load(std::memory_order_acquire);
	if (f < 0) {
		return false;
	}
	if (p_len > kMaxPayload) {
		dropped_count.fetch_add(1, std::memory_order_relaxed);
		return false;
	}
	uint8_t buf[kHeaderSize + kMaxPayload];
	buf[0] = p_type;
	buf[1] = 0;
	buf[2] = (uint8_t)(p_len & 0xFF);
	buf[3] = (uint8_t)((p_len >> 8) & 0xFF);
	if (p_len != 0) {
		memcpy(buf + kHeaderSize, p_payload, p_len);
	}
	const size_t total = kHeaderSize + (size_t)p_len;

	// 关键：先 poll 确认可写，不可写就直接丢弃本条消息。
	// 这段代码可能运行在 Metal 的命令缓冲完成回调线程上，一旦这里阻塞，命令缓冲无法回收，
	// 队列耗尽后游戏主循环就会永久卡死（真实故障的根因）。MSG_DONTWAIT 在 Darwin 的
	// stream socket 上并不可靠，所以必须用 poll 先探可写性。
	for (int attempt = 0;; attempt++) {
		struct pollfd pfd;
		pfd.fd = f;
		pfd.events = POLLOUT;
		pfd.revents = 0;
		if (::poll(&pfd, 1, attempt == 0 ? 0 : 1) > 0 && (pfd.revents & POLLOUT) != 0) {
			ssize_t n = ::send(f, buf, total, MSG_DONTWAIT);
			if (n == (ssize_t)total) {
				sent_count.fetch_add(1, std::memory_order_relaxed);
				return true;
			}
		}
		if (attempt >= p_wait_ms) {
			break;
		}
		if (fd.load(std::memory_order_acquire) != f) {
			return false; // 连接已断开/被换掉，别对着旧 fd 空转
		}
		retry_count.fetch_add(1, std::memory_order_relaxed);
	}
	dropped_count.fetch_add(1, std::memory_order_relaxed);
	return false;
}

inline void send_frame(uint32_t p_seq, uint32_t p_buffer_index) {
	uint8_t payload[8];
	payload[0] = (uint8_t)(p_seq & 0xFF);
	payload[1] = (uint8_t)((p_seq >> 8) & 0xFF);
	payload[2] = (uint8_t)((p_seq >> 16) & 0xFF);
	payload[3] = (uint8_t)((p_seq >> 24) & 0xFF);
	payload[4] = (uint8_t)(p_buffer_index & 0xFF);
	payload[5] = (uint8_t)((p_buffer_index >> 8) & 0xFF);
	payload[6] = (uint8_t)((p_buffer_index >> 16) & 0xFF);
	payload[7] = (uint8_t)((p_buffer_index >> 24) & 0xFF);
	send_message(MSG_FRAME_COMPLETE, payload, 8);
}

// 发送一包交织 Int16 立体声 PCM（Engine 侧已从 Float 转好，减小回传体积；本机为小端，直接按字节发）。
// p_wait_ms > 0 时最多等这么久再重试（只给音频线程用）：丢一条音频 = 客户端解码时间线上少 5ms，
// 听感就是「啪」一下，实测「丢弃消息」的累积正是偶发「啪」的来源；而音频晚发几十毫秒只表现为
// 整体延迟抖动，客户端抖动缓冲吸收得了。
inline bool send_audio(const int16_t *p_pcm, int p_frames, int p_wait_ms = 0) {
	if (p_pcm == nullptr || p_frames <= 0) {
		return false;
	}
	size_t bytes = (size_t)p_frames * 2 * sizeof(int16_t);
	if (bytes > kMaxPayload) {
		p_frames = (int)(kMaxPayload / (2 * sizeof(int16_t)));
		bytes = (size_t)p_frames * 2 * sizeof(int16_t);
	}
	return send_message(MSG_AUDIO_PCM, (const uint8_t *)p_pcm, (uint16_t)bytes, p_wait_ms);
}

} // namespace QRReverseChannel
