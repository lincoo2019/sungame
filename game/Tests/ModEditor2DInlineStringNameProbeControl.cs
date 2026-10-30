using Godot;

public partial class ModEditor2DInlineStringNameProbeControl : Control
{
	[Export(PropertyHint.None, "")]
	public StringName Caption { get; set; } = new StringName("StringName 原文");

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("19382f"));
		DrawRect(new Rect2(Vector2.Zero, base.Size), new Color("d6b85d"), filled: false, 2f);
		DrawString(ThemeDB.FallbackFont, new Vector2(10f, 31f), Caption.ToString(), HorizontalAlignment.Left, Mathf.Max(1f, base.Size.X - 20f), 18, new Color("fff2b7"), TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
	}
}
