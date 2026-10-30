# 第三方组件与许可声明

本项目（`app/` 下的自有代码）以 **GNU Affero General Public License v3.0**（见 `LICENSE`）发布。
以下组件随源码或二进制分发，各自遵循其原始许可；本文件汇总它们的来源与许可条款。

## 运行依赖（打包进 SunGame.app）

| 组件 | 位置 | 许可 |
|---|---|---|
| chiaki-ng / libchiaki | `app/ThirdParty/chiaki/`（`libchiaki.a`） | AGPL-3.0-only（SPDX：`LicenseRef-AGPL-3.0-only-OpenSSL`） |
| libopus | `app/ThirdParty/opus/`（`libopus.a`） | BSD-3-Clause |
| libevent | `app/ThirdParty/chiaki/lib/`（`libevent_*.dylib`） | BSD-3-Clause |
| OpenSSL（libcrypto / libssl） | `app/ThirdParty/chiaki/lib/` | Apache-2.0 |
| json-c | `app/ThirdParty/chiaki/lib/` | MIT |
| miniupnpc | `app/ThirdParty/chiaki/lib/` | BSD-3-Clause |
| Jerasure | `app/ThirdParty/chiaki/lib/` | BSD-3-Clause |
| GF-Complete | `app/ThirdParty/chiaki/lib/` | BSD-3-Clause |
| nanopb | `app/ThirdParty/chiaki/lib/` | Zlib |
| libcurl | 系统 `/usr/lib/libcurl` | curl License |

libchiaki 以 AGPL-3.0 许可，且被静态链接进应用，因此本项目整体按 AGPL-3.0 发布，
完整对应源码即本仓库（<https://github.com/lincoo2019/sungame>）。

## 引擎

- **Godot Engine**（`engine/`）：MIT License。
  版权声明与许可全文见 `engine/COPYRIGHT.txt`。本仓库对其做了离屏渲染与反向通道修改，
  修改后的源码同样以 MIT 发布。

## 游戏内容

- `game/` 为「植物大战僵尸杂交版」Godot 工程，游戏与美术、音频素材的版权归
  EA / PopCap Games 及杂交版原作者所有，**不适用**本仓库的 AGPL 许可。
- 该目录仅用于个人学习与研究，请勿用于商业用途或再分发素材。

## 预编译产物

- `GodotPatched.zip`（Release）：上述魔改 Godot 引擎的构建产物，遵循 MIT。
- `dotnet-sdk.zip`（Release）：Microsoft .NET SDK，遵循 MIT，版权归 .NET Foundation。

## 上游链接

- chiaki-ng: <https://github.com/streetpea/chiaki-ng>
- libopus: <https://opus-codec.org/>
- libevent: <https://libevent.org/>
- OpenSSL: <https://www.openssl.org/>
- json-c: <https://github.com/json-c/json-c>
- miniupnpc: <https://miniupnp.tuxfamily.org/>
- Jerasure / GF-Complete: <https://github.com/ceph/jerasure>
- nanopb: <https://jpa.kapsi.fi/nanopb/>
- Godot: <https://godotengine.org/>
