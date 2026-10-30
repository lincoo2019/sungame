using Godot;

[GlobalClass]
public partial class BroadCastFloat : Control
{
	public Label label;

	public void Init(string text, Color color = default(Color))
	{
		if (color == default(Color))
		{
			color = Colors.White;
		}
		label.Text = text;
		label.Modulate = color;
	}

	public override void _Ready()
	{
		label = GetNode<Label>("%Label111");
		Tween tween = CreateTween().SetParallel();
		tween.TweenProperty(label, "global_position:y", 68, 2.0);
		tween.TweenProperty(label, "modulate:a", 0.0, 2.0);
		tween.Finished += delegate
		{
			QueueFree();
		};
	}
}
