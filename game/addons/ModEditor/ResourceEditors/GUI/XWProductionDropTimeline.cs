using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWProductionDropTimeline : Control
{
	private ProduceComponentDefinition _definition;

	public void Bind(ProduceComponentDefinition definition)
	{
		_definition = definition;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			Font fallbackFont = ThemeDB.FallbackFont;
			float num = 18f;
			float num2 = Mathf.Max(120f, base.Size.X - 36f);
			float num3 = 58f;
			DrawLine(new Vector2(num, num3), new Vector2(num + num2, num3), new Color("748676"), 3f);
			Vector2 initialDelayRange = _definition.initialDelayRange;
			float num4 = Mathf.Max(0.001f, _definition.produceInterval);
			float num5 = Mathf.Max(Mathf.Max(initialDelayRange.X, initialDelayRange.Y), num4);
			float num6 = num2 / Mathf.Max(1f, num5 + num4 * 0.25f);
			float num7 = num + Mathf.Max(0f, Mathf.Min(initialDelayRange.X, initialDelayRange.Y)) * num6;
			float num8 = num + Mathf.Max(0f, Mathf.Max(initialDelayRange.X, initialDelayRange.Y)) * num6;
			float num9 = num + num5 * num6;
			float num10 = Mathf.Max(num, num9 - Mathf.Max(0f, _definition.glowLeadTime) * num6);
			DrawRect(new Rect2(new Vector2(num7, num3 - 14f), new Vector2(Mathf.Max(2f, num8 - num7), 28f)), new Color("365b77"));
			DrawCircle(new Vector2(num10, num3), 8f, new Color("fff07a"));
			DrawCircle(new Vector2(num9, num3), 10f, new Color("77d48d"));
			DrawString(fallbackFont, new Vector2(num, 22f), "初始延迟范围", HorizontalAlignment.Left, -1f, 13, new Color("8fb6d2"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(fallbackFont, new Vector2(Mathf.Max(num, num10 - 28f), 102f), "发光", HorizontalAlignment.Left, -1f, 13, new Color("fff07a"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(fallbackFont, new Vector2(Mathf.Max(num, num9 - 32f), 125f), "生产", HorizontalAlignment.Left, -1f, 13, new Color("77d48d"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(fallbackFont, new Vector2(num, 145f), $"追赶上限 {Math.Max(1, _definition.maxCatchUpProductions)} 次/帧 · 亮起 {_definition.glowFadeInTime:0.##}s · 熄灭 {_definition.glowFadeOutTime:0.##}s", HorizontalAlignment.Left, -1f, 12, new Color("9bad9b"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
