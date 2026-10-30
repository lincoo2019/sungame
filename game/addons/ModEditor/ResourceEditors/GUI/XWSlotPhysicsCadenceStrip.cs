using System;
using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWSlotPhysicsCadenceStrip : Control
{
	private int _interval = 3;

	public void Bind(int interval)
	{
		_interval = Math.Clamp(interval, 1, 30);
		QueueRedraw();
	}

	public override void _Draw()
	{
		int num = Math.Min(12, _interval);
		float num2 = 5f;
		float num3 = Math.Max(14f, (base.Size.X - num2 * (float)(num - 1)) / (float)num);
		Font fallbackFont = ThemeDB.FallbackFont;
		for (int i = 0; i < num; i++)
		{
			Rect2 rect = new Rect2(new Vector2((float)i * (num3 + num2), 8f), new Vector2(num3, 25f));
			bool flag = i == num - 1;
			DrawRect(rect, flag ? new Color("55c7ed") : new Color("294351"));
			DrawString(fallbackFont, rect.Position + new Vector2(5f, 18f), flag ? "扫描" : $"{i + 1}", HorizontalAlignment.Left, -1f, 12, flag ? new Color("08232f") : new Color("a6bdc9"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
		if (_interval > num)
		{
			DrawString(fallbackFont, new Vector2(0f, 54f), $"… 共 {_interval} 个物理帧后重新查询占用者", HorizontalAlignment.Left, -1f, 12, new Color("88a9ba"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
