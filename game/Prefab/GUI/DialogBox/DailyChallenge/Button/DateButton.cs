using Godot;

public partial class DateButton : Button
{
	public TextureRect finishTexture;

	public override void _Ready()
	{
		finishTexture = GetNode<TextureRect>("%FinishTexture");
	}
}
