using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWFireVolleyTimeline : Control
{
	private FireComponentDefinition _definition;

	public void Bind(FireComponentDefinition definition)
	{
		_definition = definition;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			Rect2 rect = new Rect2(new Vector2(18f, 34f), new Vector2(Mathf.Max(80f, base.Size.X - 36f), 54f));
			DrawRect(rect, new Color("091018"));
			DrawRect(rect, new Color("526477"), filled: false, 1.5f);
			int num = Math.Max(1, _definition.fireNum);
			float num2 = Mathf.Max(1f, rect.Size.X - 30f);
			for (int i = 0; i < num; i++)
			{
				float num3 = ((_definition.fireNumAtOnce || num == 1) ? 0.08f : (0.08f + 0.84f * (float)i / ((float)num - 1f)));
				Vector2 position = new Vector2(rect.Position.X + num2 * num3, rect.GetCenter().Y);
				DrawLine(new Vector2(position.X, rect.Position.Y + 8f), new Vector2(position.X, rect.End.Y - 8f), new Color("ffb55a"), 3f);
				DrawCircle(position, (i == 0) ? 7 : 5, new Color("ffe1a8"));
			}
			Font fallbackFont = ThemeDB.FallbackFont;
			DrawString(fallbackFont, new Vector2(18f, 22f), _definition.fireNumAtOnce ? "一个动画事件 · 同时齐射" : "多个动画事件 · 逐次连射", HorizontalAlignment.Left, -1f, 14, new Color("f6ce8c"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			float num4 = Mathf.Abs(_definition.fireIntervalOffset);
			float value = Mathf.Max(0f, _definition.fireInterval - num4 * 2f);
			float value2 = Mathf.Max(0f, _definition.fireInterval - num4);
			DrawString(fallbackFont, new Vector2(18f, 113f), $"基础动画间隔 {_definition.fireIntervalBase:0.##} 秒 · 检测间隔 {value:0.##}～{value2:0.##} 秒", HorizontalAlignment.Left, -1f, 13, new Color("91a6bc"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
