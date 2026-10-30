using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWProductionHealthRuler : Control
{
	private int _segments = 6;

	private int _catchUp = 1;

	public void Bind(int segments, int catchUp)
	{
		_segments = Math.Clamp(segments, 1, 1000);
		_catchUp = Math.Clamp(catchUp, 1, 120);
		QueueRedraw();
	}

	public override void _Draw()
	{
		Font fallbackFont = ThemeDB.FallbackFont;
		Rect2 rect = new Rect2(new Vector2(18f, 28f), new Vector2(Mathf.Max(100f, base.Size.X - 36f), 24f));
		DrawRect(rect, new Color("2c5637"));
		DrawRect(rect, new Color("7bd78c"), filled: false, 2f);
		int num = Math.Min(_segments, 40);
		for (int i = 1; i < num; i++)
		{
			float x = rect.Position.X + rect.Size.X * (float)i / (float)num;
			DrawLine(new Vector2(x, rect.Position.Y), new Vector2(x, rect.End.Y), new Color("d7f0d8"), 1f);
		}
		DrawString(fallbackFont, new Vector2(18f, 78f), $"100% → 0% 共 {_segments} 个阈值；单帧最多补产 {_catchUp} 次", HorizontalAlignment.Left, -1f, 13, new Color("b8d9ba"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}
}
