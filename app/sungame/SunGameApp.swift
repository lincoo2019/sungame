//
//  SunGameApp.swift
//  sunGame
//
//  sunGame：把 QuickRecorder 的 Moonlight 串流能力剥离为独立应用，
//  只提供「植物大战僵尸杂交版」与「PS5」两种画面来源。
//

import SwiftUI

@main
struct SunGameApp: App {
    var body: some Scene {
        WindowGroup("sunGame") {
            StreamingSettingsView()
        }
        .windowResizability(.contentSize)
    }
}
