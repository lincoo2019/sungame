# sunGame

把 QuickRecorder 里的 Moonlight（Sunshine 协议）串流能力剥离出来，配合魔改版 Godot 引擎，
把「植物大战僵尸杂交版」以无窗口方式串流给 Moonlight 客户端，同时支持 PS5 直通。

仓库按三个部分组织：

```
app/      macOS 串流主机应用（Swift / SwiftUI）
engine/   魔改版 Godot 引擎源码（无窗口离屏渲染 + Metal 反向通道）
game/     「植物大战僵尸杂交版」Godot 工程
```

## app/

Moonlight 主机端（HTTP 47989 / HTTPS 47984 / RTSP 48010 / 视频 UDP 47998 / 控制 UDP 47999 / 音频 UDP 48000）。

- 画面来源只有两种：**游戏**（无窗口运行的 Godot 工程）与 **PS5**（libchiaki 直通）。
- 不包含屏幕录制、虚拟桌面、窗口共享。
- 依赖（libchiaki、libopus、libevent 等）已放在 `app/ThirdParty/`，静态链接。
- 构建：用 Xcode 打开 `app/sungame.xcodeproj`（macOS 14.2+，arm64），产物为 `SunGame.app`。
- 首次运行：授予「辅助功能」权限（仅用于远程输入的本地注入），点击「启动」后在 Moonlight 中
  添加本机 IP，配对时把 Moonlight 显示的 4 位 PIN 填回界面。
- PS5 需填写主机地址、PSN Account-ID 与 8 位 Remote Play PIN，先「注册到主机」。

游戏串流的引擎与 .NET 运行时默认复用 QuickRecorder 的部署目录：

```
~/Library/Application Support/QuickRecorder/Engine/GodotPatched.app
~/Library/Application Support/QuickRecorder/Engine/dotnet
```

可把 `engine/` 编译产物与 dotnet 部署到该位置，或自行调整 `StreamingHost` 中的
`gameEngineDirectory()`。

## engine/

魔改的 Godot 4 源码，去掉 `bin/` 构建产物与 `.git`。主要改动：

- `drivers/metal/qr_reverse_channel.h` — Metal 渲染进程与游戏进程之间的反向通道（输入/音频）。
- `platform/macos/display_server_macos_offscreen.mm` — 离屏（无窗口）显示服务器：
  引擎侧抓取音频混音、触摸/鼠标输入合成、硬件时钟驱动音频回传。

提交历史摘要（原始仓库）：

```
8d25c2e offscreen: 修正音频诊断格式，改用硬件时钟驱动+总线静音保证 48000 实时
dc46c65 offscreen: 游戏音频改由引擎侧 AudioEffectCapture 抓混音经反向通道回传
3ac7bb8 offscreen: 触摸→鼠标改回 Godot 内建合成，恢复原生触摸拖动
10436ee offscreen: 引擎接管触摸→鼠标合成，强制关闭 emulate_mouse_from_touch
```

构建（需 scons 与 mono 工具链）：

```bash
cd engine
scons platform=macos arch=arm64 target=editor
```

预编译的 `GodotPatched.app` 见仓库 **Releases**。

## game/

「植物大战僵尸杂交版」Godot 工程（`project.godot`）。用上面的魔改引擎打开即可；
串流时由 `app/` 以无窗口模式启动并接管输入与音频。

## 版权说明

- `app/`：本项目自有代码。
- `engine/`：Godot Engine，遵循 MIT 许可证（见 `engine/COPYRIGHT.txt`）。
- `game/`：游戏工程与素材版权归原作者所有，仅用于个人学习与研究，请勿用于商业用途。
