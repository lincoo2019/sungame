using Godot;

[GlobalClass]
public partial class DialogPopup : DialogBoxBase
{
	protected RichTextLabel textLabel;

	private NinePatchRect ninePatchRect;

	private TextureRect headTextureRect;

	public override void _Ready()
	{
		base._Ready();
		textLabel = GetNode<RichTextLabel>("%TextLabel");
		ninePatchRect = GetNode<NinePatchRect>("%NinePatchRect");
		headTextureRect = GetNode<TextureRect>("%HeadTextureRect");
		ninePatchRect.Resized += RefreshHeadPosition;
		RefreshHeadPosition();
	}

	private void RefreshHeadPosition()
	{
		if (headTextureRect != null && ninePatchRect != null)
		{
			headTextureRect.Position = new Vector2(ninePatchRect.Size.X / 2f - 100f, -42f);
		}
	}
}
