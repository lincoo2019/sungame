using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public sealed partial class XWExplosionStageTimeline : Control
{
	private ExplodeComponentDefinition _definition;

	public void Bind(ExplodeComponentDefinition definition)
	{
		_definition = definition;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			float num = Mathf.Max(0.25f, Mathf.Max(Mathf.Max(0, _definition.cameraShakeTime), Mathf.Max(0f, _definition.screenColorBlinkDuration)));
			float num2 = 104f;
			float num3 = Mathf.Max(num2 + 160f, base.Size.X - 18f);
			float num4 = num3 - num2;
			float num5 = 28f;
			Font fallbackFont = ThemeDB.FallbackFont;
			DrawString(fallbackFont, new Vector2(12f, 18f), "触发时刻 0s", HorizontalAlignment.Left, -1f, 13, new Color("ffd19d"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawLine(new Vector2(num2, 20f), new Vector2(num2, 164f), new Color("ff8a58"), 2f);
			DrawString(fallbackFont, new Vector2(num2 - 8f, 18f), "0", HorizontalAlignment.Center, 20f, 11, new Color("ffb58b"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawString(fallbackFont, new Vector2(num3 - 24f, 18f), $"{num:0.##}s", HorizontalAlignment.Right, 48f, 11, new Color("93a7bc"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			DrawTrack(fallbackFont, "动画", num5, new Color("cb8cff"), num4 * 0.28f, $"速度 ×{_definition.explodeAnimeTimeScale:0.##}");
			num5 += 27f;
			DrawTrack(fallbackFont, "音效 / 特效", num5, new Color("ffd06d"), 10f, string.IsNullOrWhiteSpace(_definition.explodeAudio) ? "无音效" : _definition.explodeAudio);
			num5 += 27f;
			DrawTrack(fallbackFont, "镜头", num5, new Color("79b7ff"), _definition.cameraShakeUse ? (num4 * Mathf.Clamp((float)_definition.cameraShakeTime / num, 0f, 1f)) : 0f, _definition.cameraShakeUse ? $"{_definition.cameraShakeTime:0.##}s" : "关闭");
			num5 += 27f;
			DrawTrack(fallbackFont, "屏闪", num5, _definition.screenColorBlinkColor, _definition.screenColorBlinkUse ? (num4 * Mathf.Clamp(_definition.screenColorBlinkDuration / num, 0f, 1f)) : 0f, _definition.screenColorBlinkUse ? $"{_definition.screenColorBlinkDuration:0.##}s" : "关闭");
			num5 += 27f;
			DrawTrack(fallbackFont, "弹坑", num5, new Color("a86d45"), _definition.craterCreateUse ? 10 : 0, _definition.craterCreateUse ? "当前格" : "关闭");
		}
	}

	private void DrawTrack(Font font, string label, float y, Color color, float durationWidth, string detail)
	{
		DrawString(font, new Vector2(12f, y + 14f), label, HorizontalAlignment.Left, 84f, 12, new Color("b8c2cf"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		Rect2 rect = new Rect2(new Vector2(104f, y), new Vector2(Mathf.Max(160f, base.Size.X - 122f), 18f));
		DrawRect(rect, new Color("080d14"));
		DrawRect(rect, new Color("405064"), filled: false, 1f);
		if (durationWidth > 0f)
		{
			DrawRect(new Rect2(rect.Position, new Vector2(Mathf.Clamp(durationWidth, 4f, rect.Size.X), rect.Size.Y)), new Color(color, 0.78f));
		}
		DrawString(font, new Vector2(rect.Position.X + 8f, y + 14f), detail, HorizontalAlignment.Left, rect.Size.X - 12f, 11, Colors.White, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}
}
