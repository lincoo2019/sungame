using Godot;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

internal sealed partial class XWAttackContactTimeline : Control
{
	private AttackComponentDefinition _definition;

	public void Bind(AttackComponentDefinition definition)
	{
		_definition = definition;
		QueueRedraw();
	}

	public override void _Draw()
	{
		if (GodotObject.IsInstanceValid(_definition))
		{
			Rect2 rect = new Rect2(new Vector2(18f, 36f), new Vector2(Mathf.Max(100f, base.Size.X - 36f), 58f));
			DrawRect(rect, new Color("081018"));
			DrawRect(rect, new Color("566779"), filled: false, 1.5f);
			float[] array = new float[4] { 0.05f, 0.3f, 0.66f, 0.94f };
			Color[] array2 = new Color[4]
			{
				new Color("81b6d9"),
				new Color("d6a85e"),
				new Color("ff6b56"),
				new Color("79d487")
			};
			string[] array3 = new string[4] { "检测", "进入攻击", "命中事件", "动画完成" };
			Font fallbackFont = ThemeDB.FallbackFont;
			for (int i = 0; i < array.Length; i++)
			{
				float num = rect.Position.X + rect.Size.X * array[i];
				DrawLine(new Vector2(num, rect.Position.Y + 6f), new Vector2(num, rect.End.Y - 6f), array2[i], (i == 2) ? 4 : 2);
				DrawCircle(new Vector2(num, rect.GetCenter().Y), (i == 2) ? 7 : 5, array2[i]);
				DrawString(fallbackFont, new Vector2(num - 28f, 116f), array3[i], HorizontalAlignment.Center, 56f, 12, array2[i], TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
			string text = (string.IsNullOrWhiteSpace(_definition.attackEventName) ? "无命中事件" : ("“" + _definition.attackEventName + "” → OnAttack / eventList"));
			DrawString(fallbackFont, new Vector2(18f, 24f), text, HorizontalAlignment.Left, -1f, 14, new Color("ffd0a3"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}
}
