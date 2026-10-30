//
//  StreamingSettingsView.swift
//  sunGame
//
//  sunGame 设置界面：作为 Moonlight 主机串流「植物大战僵尸」游戏或 PS5 画面。
//

import SwiftUI

struct StreamingSettingsView: View {
    @StateObject private var host = MLStreamingHost.shared

    @State private var pinInput = ""
    @State private var accessibilityGranted = MLInputInjector.hasAccessibilityPermission
    /// PS5 连接设置
    @State private var ps5 = ChiakiPS5Settings.load()
    @State private var showPS5PINPrompt = false
    /// 游戏串流设置
    @State private var gameShowCursor = MLGameStreamSettings.showCursor
    @State private var gameAudioEnabled = MLGameStreamSettings.audioEnabled
    @State private var gameProjectPath = MLStreamingHost.gameProjectPath()

    var body: some View {
        ScrollView {
            VStack(alignment: .leading, spacing: 16) {
                statusSection
                gameSection
                ps5Section
                pairingSection
                clientsSection
                permissionSection
                logSection
            }
            .padding(20)
        }
        .frame(minWidth: 520, minHeight: 560)
        .onAppear {
            accessibilityGranted = MLInputInjector.hasAccessibilityPermission
            gameProjectPath = MLStreamingHost.gameProjectPath()
            ps5 = ChiakiPS5Settings.load()
        }
        .onChange(of: host.ps5PINRequested) { requested in
            showPS5PINPrompt = requested
        }
        .alert("PS5 需要输入 PIN", isPresented: $showPS5PINPrompt) {
            TextField("PIN", text: $pinInput)
            Button("提交") {
                host.submitPS5PIN(pinInput)
                pinInput = ""
            }
            Button("取消", role: .cancel) {
                host.submitPS5PIN("")
            }
        } message: {
            Text("请在 PS5 主机上查看显示的数字并输入。")
        }
    }

    // MARK: - 状态

    private var statusSection: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 10) {
                HStack {
                    Circle()
                        .fill(host.isRunning ? Color.green : Color.secondary)
                        .frame(width: 8, height: 8)
                    Text(host.state.description).font(.system(size: 13, weight: .medium))
                    Spacer()
                    Button(host.isRunning ? "停止" : "启动") {
                        if host.isRunning { host.stop() } else { host.start() }
                    }
                    .controlSize(.small)
                }

                if host.isRunning {
                    HStack(spacing: 6) {
                        Image(systemName: "info.circle")
                        Text("在 Moonlight 中添加主机 \(host.localIP)，在应用列表中选择「PS5」或「植物大战僵尸杂交版」")
                            .font(.system(size: 11))
                            .foregroundStyle(.secondary)
                    }
                }
            }
            .padding(6)
        } label: {
            Label("串流主机", systemImage: "dot.radiowaves.left.and.right")
        }
    }

    // MARK: - 游戏串流

    private var gameSection: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 8) {
                HStack(spacing: 8) {
                    Text("工程目录").frame(width: 64, alignment: .leading)
                    TextField("游戏工程目录", text: $gameProjectPath)
                        .textFieldStyle(.roundedBorder)
                        .onSubmit { saveGameProjectPath() }
                }

                Toggle("合成虚拟光标", isOn: Binding(
                    get: { gameShowCursor },
                    set: { gameShowCursor = $0; MLGameStreamSettings.showCursor = $0 }
                ))

                Toggle("把游戏音频送进串流", isOn: Binding(
                    get: { gameAudioEnabled },
                    set: { gameAudioEnabled = $0; MLGameStreamSettings.audioEnabled = $0 }
                ))

                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: "cursorarrow").foregroundStyle(.secondary)
                    Text("游戏在无窗口模式下运行，系统光标不会显示；开启「合成虚拟光标」后由本机把光标画进画面再编码。")
                        .font(.system(size: 11))
                }
            }
            .padding(6)
        } label: {
            Label("游戏串流", systemImage: "gamecontroller.fill")
        }
    }

    private func saveGameProjectPath() {
        let trimmed = gameProjectPath.trimmingCharacters(in: .whitespacesAndNewlines)
        UserDefaults.standard.set(trimmed, forKey: "QRGameProjectPath")
    }

    // MARK: - PS5

    private var ps5Section: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 10) {
                Toggle("启用 PS5 串流", isOn: Binding(
                    get: { ps5.enabled },
                    set: { ps5.enabled = $0; ps5.save() }
                ))

                HStack(spacing: 8) {
                    Text("主机地址").frame(width: 64, alignment: .leading)
                    TextField("192.168.1.100", text: $ps5.host)
                        .textFieldStyle(.roundedBorder)
                        .onSubmit { ps5.save() }
                }

                HStack(spacing: 12) {
                    Picker("分辨率", selection: Binding(
                        get: { ps5.resolutionPreset },
                        set: { ps5.resolutionPreset = $0; ps5.save() }
                    )) {
                        Text("720p").tag(3)
                        Text("1080p").tag(4)
                    }
                    .frame(width: 160)

                    Picker("帧率", selection: Binding(
                        get: { ps5.fps },
                        set: { ps5.fps = $0; ps5.save() }
                    )) {
                        Text("30").tag(30)
                        Text("60").tag(60)
                    }
                    .frame(width: 120)
                }

                HStack(spacing: 8) {
                    Text("码率").frame(width: 64, alignment: .leading)
                    TextField("20000", value: Binding(
                        get: { ps5.bitrateKbps },
                        set: { ps5.bitrateKbps = $0; ps5.save() }
                    ), format: .number)
                    .textFieldStyle(.roundedBorder)
                    .frame(width: 90)
                    Text("Kbps").font(.system(size: 11)).foregroundStyle(.secondary)
                }

                Divider()

                HStack(spacing: 8) {
                    Text("Account-ID").frame(width: 64, alignment: .leading)
                    TextField("PSN Account-ID（base64，8 字节）", text: $ps5.psnAccountId)
                        .textFieldStyle(.roundedBorder)
                    Text("注册必填")
                        .font(.system(size: 11))
                        .foregroundStyle(.secondary)
                }

                HStack(spacing: 8) {
                    Text("Remote PIN").frame(width: 64, alignment: .leading)
                    TextField("主机上显示的 8 位 PIN", text: $ps5.pin)
                        .textFieldStyle(.roundedBorder)
                    TextField("Console PIN（可选 4 位）", text: $ps5.consolePin)
                        .textFieldStyle(.roundedBorder)
                        .frame(width: 150)
                }

                HStack(spacing: 8) {
                    Text(ps5.morning.isEmpty ? "状态：未注册" : "状态：已注册")
                        .font(.system(size: 11))
                        .foregroundStyle(ps5.morning.isEmpty ? Color.secondary : Color.green)
                    Spacer()
                    if host.ps5Registering {
                        Button("取消注册") { host.cancelPS5Registration() }
                            .controlSize(.small)
                    } else {
                        Button("注册到主机") {
                            ps5.save()
                            host.registerPS5(host: ps5.host, ps5: ps5.ps5,
                                             psnAccountId: ps5.psnAccountId,
                                             pin: ps5.pin, consolePin: ps5.consolePin)
                        }
                        .controlSize(.small)
                        .disabled(ps5.host.isEmpty || ps5.psnAccountId.isEmpty || ps5.pin.isEmpty)
                    }
                }

                if let status = host.ps5Status {
                    Text(status).font(.system(size: 11)).foregroundStyle(.secondary)
                }

                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: "info.circle").foregroundStyle(.secondary)
                    Text("PS5 注册需要 PSN Account-ID（8 字节原始值的 base64）与主机显示的 8 位 Remote Play PIN；注册成功后主机信息会被保存，之后直接连接即可。")
                        .font(.system(size: 11))
                }
            }
            .padding(6)
        } label: {
            Label("PS5（PlayStation）", systemImage: "gamecontroller")
        }
    }

    // MARK: - 配对

    private var pairingSection: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 10) {
                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: "1.circle").foregroundStyle(.secondary)
                    Text("在 Moonlight 中点击你添加的主机开始配对，Moonlight 会显示一个 4 位 PIN。")
                        .font(.system(size: 11))
                }
                HStack(alignment: .top, spacing: 8) {
                    Image(systemName: "2.circle").foregroundStyle(.secondary)
                    Text("把那个 PIN 填到下面并提交，配对会自动完成。")
                        .font(.system(size: 11))
                }

                HStack(spacing: 10) {
                    TextField("4 位 PIN", text: $pinInput)
                        .textFieldStyle(.roundedBorder)
                        .font(.system(size: 18, weight: .semibold, design: .monospaced))
                        .frame(width: 120)
                        .onSubmit { submitPIN() }
                    Button("提交") { submitPIN() }
                        .controlSize(.small)
                        .disabled(pinInput.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty)
                    Spacer()
                }

                if let device = host.pairingPINNeeded {
                    HStack(spacing: 6) {
                        ProgressView().controlSize(.small)
                        Text("「\(device)」正在配对，等待你填入 PIN…")
                            .font(.system(size: 11, weight: .medium))
                            .foregroundStyle(.orange)
                    }
                } else if host.isRunning {
                    Text("当前没有待处理的配对请求").font(.system(size: 10)).foregroundStyle(.secondary)
                }

                Divider()

                VStack(alignment: .leading, spacing: 4) {
                    Text("端口").font(.system(size: 11, weight: .medium))
                    Text("HTTPS \(MLPort.https) · HTTP \(MLPort.http) · RTSP \(MLPort.rtsp) · 视频 \(MLPort.video) · 控制 \(MLPort.control) · 音频 \(MLPort.audio)")
                        .font(.system(size: 10, design: .monospaced))
                        .foregroundStyle(.secondary)
                }
            }
            .padding(6)
        } label: {
            Label("配对", systemImage: "lock.shield")
        }
    }

    private func submitPIN() {
        let value = pinInput.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !value.isEmpty else { return }
        host.submitPIN(value)
        pinInput = ""
    }

    // MARK: - 已配对客户端

    private var clientsSection: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 8) {
                if host.pairedClients.isEmpty {
                    Text("暂无已配对客户端").font(.system(size: 11)).foregroundStyle(.secondary)
                } else {
                    ForEach(host.pairedClients, id: \.uuid) { client in
                        HStack {
                            Image(systemName: "laptopcomputer")
                            Text(client.name).font(.system(size: 11))
                            Spacer()
                            Text(client.enabled ? "已启用" : "已禁用")
                                .font(.system(size: 10))
                                .foregroundStyle(.secondary)
                        }
                    }
                    Button("解除全部配对") { host.unpairAll() }
                        .controlSize(.small)
                }
            }
            .padding(6)
        } label: {
            Label("已配对设备", systemImage: "list.bullet.rectangle")
        }
    }

    // MARK: - 权限

    private var permissionSection: some View {
        GroupBox {
            VStack(alignment: .leading, spacing: 8) {
                HStack {
                    Image(systemName: accessibilityGranted ? "checkmark.seal.fill" : "exclamationmark.triangle.fill")
                        .foregroundStyle(accessibilityGranted ? .green : .orange)
                    Text(accessibilityGranted ? "已获得辅助功能授权" : "未获得辅助功能授权")
                        .font(.system(size: 11))
                    Spacer()
                    if !accessibilityGranted {
                        Button("授权") {
                            MLInputInjector.requestAccessibilityPermission()
                        }
                        .controlSize(.small)
                    }
                }
                Text("游戏与 PS5 串流不需要「屏幕录制」权限；辅助功能授权仅用于远程输入的本地注入。")
                    .font(.system(size: 10))
                    .foregroundStyle(.secondary)
            }
            .padding(6)
        } label: {
            Label("系统权限", systemImage: "hand.raised")
        }
    }

    // MARK: - 日志

    private var logSection: some View {
        GroupBox {
            ScrollView {
                VStack(alignment: .leading, spacing: 2) {
                    if host.logLines.isEmpty {
                        Text("暂无日志").font(.system(size: 11)).foregroundStyle(.secondary)
                    } else {
                        ForEach(Array(host.logLines.enumerated()), id: \.offset) { item in
                            Text(item.element)
                                .font(.system(size: 10, design: .monospaced))
                                .textSelection(.enabled)
                                .frame(maxWidth: .infinity, alignment: .leading)
                        }
                    }
                }
                .padding(4)
            }
            .frame(height: 160)
        } label: {
            Label("日志", systemImage: "text.alignleft")
        }
    }
}
