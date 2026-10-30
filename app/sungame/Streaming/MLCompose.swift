//
//  MLCompose.swift
//  sunGame
//
//  合成画面的绘制工具（坐标一律使用左上原点，绘制前翻转到 CG 坐标）。
//  从 QuickRecorder 的 VirtualDesktop.swift 中抽取，仅保留游戏串流用到的部分。
//

import AppKit

enum MLCompose {
    /// 左上原点坐标 → CG 左下原点坐标
    static func flip(_ rect: CGRect, _ size: CGSize) -> CGRect {
        CGRect(x: rect.minX, y: size.height - rect.maxY, width: rect.width, height: rect.height)
    }

    /// 自绘虚拟光标（箭头）
    static func drawCursor(_ context: CGContext, at point: CGPoint, size: CGSize, scale: CGFloat) {
        let s = 20 * scale
        let points = [CGPoint(x: 0, y: 0), CGPoint(x: 0, y: 1.0), CGPoint(x: 0.25, y: 0.75),
                      CGPoint(x: 0.42, y: 1.12), CGPoint(x: 0.58, y: 1.05), CGPoint(x: 0.4, y: 0.7),
                      CGPoint(x: 0.68, y: 0.68)]
        let path = CGMutablePath()
        for (index, p) in points.enumerated() {
            let converted = CGPoint(x: point.x + p.x * s, y: point.y + p.y * s)
            if index == 0 { path.move(to: converted) } else { path.addLine(to: converted) }
        }
        path.closeSubpath()

        var transform = CGAffineTransform(translationX: 0, y: size.height).scaledBy(x: 1, y: -1)
        guard let flipped = path.copy(using: &transform) else { return }
        context.saveGState()
        context.addPath(flipped)
        context.setFillColor(NSColor.white.cgColor)
        context.setStrokeColor(NSColor.black.withAlphaComponent(0.75).cgColor)
        context.setLineWidth(1.2 * scale)
        context.drawPath(using: .fillStroke)
        context.restoreGState()
    }

    /// 画布缩放系数（以 1920×1080 为基准，便于不同分辨率下保持观感一致）
    static func uiScale(for size: CGSize) -> CGFloat {
        max(min(size.width / 1920, size.height / 1080), 0.4)
    }
}
