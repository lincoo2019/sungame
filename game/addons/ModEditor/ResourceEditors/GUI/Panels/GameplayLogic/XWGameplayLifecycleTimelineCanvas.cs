using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class XWGameplayLifecycleTimelineCanvas : Control
{
	private static readonly Color ReadyColor = new Color("4e83a8");

	private static readonly Color BattleColor = new Color("4f9b62");

	private static readonly Color SettleColor = new Color("b87948");

	private XWGameplayLifecyclePlan.Snapshot _snapshot;

	private double _previewTime;

	public void SetPlan(XWGameplayLifecyclePlan.Snapshot snapshot, double previewTime)
	{
		_snapshot = snapshot;
		_previewTime = previewTime;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (_snapshot == null)
		{
			return;
		}
		float num = Math.Max(1f, base.Size.X);
		float num2 = Math.Max(1f, base.Size.Y);
		float num3 = 8f;
		float num4 = Math.Max(20f, num2 - 24f);
		double num5 = Math.Max(0.01, _snapshot.TotalDuration);
		float num6 = num * (float)(_snapshot.ReadyDuration / num5);
		float num7 = num * (float)(_snapshot.BattleDuration / num5);
		float num8 = Math.Max(1f, num - num6 - num7);
		DrawRect(new Rect2(0f, num3, num6, num4), ReadyColor);
		DrawRect(new Rect2(num6, num3, num7, num4), BattleColor);
		DrawRect(new Rect2(num6 + num7, num3, num8, num4), SettleColor);
		DrawString(GetThemeDefaultFont(), new Vector2(4f, num3 + 16f), "READY", HorizontalAlignment.Left, Math.Max(1f, num6 - 8f), 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		DrawString(GetThemeDefaultFont(), new Vector2(num6 + 4f, num3 + 16f), "BATTLE", HorizontalAlignment.Left, Math.Max(1f, num7 - 8f), 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		DrawString(GetThemeDefaultFont(), new Vector2(num6 + num7 + 4f, num3 + 16f), "SETTLE", HorizontalAlignment.Left, Math.Max(1f, num8 - 8f), 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		foreach (XWGameplayLifecyclePlan.Entry entry in _snapshot.Entries)
		{
			float x = num * (float)(Math.Clamp(entry.Time, 0.0, num5) / num5);
			DrawLine(new Vector2(x, num3 + num4 - 8f), new Vector2(x, num3 + num4 + 2f), new Color(1f, 1f, 1f, 0.72f), 2f);
		}
		float x2 = num * (float)(Math.Clamp(_previewTime, 0.0, num5) / num5);
		DrawLine(new Vector2(x2, 2f), new Vector2(x2, num2), new Color("ffe27a"), 3f);
		DrawCircle(new Vector2(x2, 4f), 4f, new Color("ffe27a"));
	}
}
