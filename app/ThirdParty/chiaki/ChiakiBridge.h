//
//  ChiakiBridge.h
//  QuickRecorder
//
//  libchiaki（chiaki-ng）的 Swift 桥接头。
//  只在此处暴露 C 接口，Swift 侧通过 ChiakiPS5Session 使用。
//
//  头文件与静态库位于 ThirdParty/chiaki/：
//    include/chiaki/*.h  —— libchiaki 公开头
//    lib/*.a             —— chiaki / openssl / json-c / miniupnpc / jerasure / gf-complete / nanopb
//

#ifndef CHIAKI_BRIDGE_H
#define CHIAKI_BRIDGE_H

#include <chiaki/common.h>
#include <chiaki/log.h>
#include <chiaki/session.h>
#include <chiaki/regist.h>
#include <chiaki/discovery.h>
#include <chiaki/controller.h>
#include <chiaki/audio.h>
#include <chiaki/videoreceiver.h>
#include <chiaki/remote/holepunch.h>

#include "ChiakiShim.h"
// 音频重打包（PS5 的 10ms Opus 帧 → 客户端协商的帧长），头文件位于 ThirdParty/opus/
#include "OpusTranscode.h"

#endif /* CHIAKI_BRIDGE_H */
