using Godot;

[GlobalClass]
public partial class ScreenEffectStorm : Control
{
	private ColorRect white;

	private ColorRect black;

	private ColorRect blink;

	public double timer;

	public double nextTime = 5.0;

	public override void _Ready()
	{
		white = GetNodeOrNull<ColorRect>("%White");
		black = GetNodeOrNull<ColorRect>("%Black");
		blink = GetNodeOrNull<ColorRect>("%Blink");
		Color color = black.Color;
		color.A = 1f;
		black.Color = color;
		Color color2 = white.Color;
		color2.A = 0f;
		white.Color = color2;
		Color color3 = blink.Color;
		color3.A = 0f;
		blink.Color = color3;
		timer = 0.0;
		nextTime = GD.RandRange(4.0, 6.0);
	}

	public override void _PhysicsProcess(double delta)
	{
		if (timer < nextTime)
		{
			timer += delta;
		}
		else
		{
			Storm();
		}
	}

	public async void Storm()
	{
		if (!Engine.IsEditorHint())
		{
			AudioManager.Instance.AudioPlay("Thunder");
		}
		timer = 0.0;
		nextTime = GD.RandRange(4.0, 6.0);
		Color color = black.Color;
		color.A = 0.2f;
		black.Color = color;
		Tween tween = CreateTween();
		tween.TweenProperty(white, "color:a", 0.0, 0.5).From(1.0);
		await ToSignal(tween, Tween.SignalName.Finished);
		tween = CreateTween();
		tween.TweenProperty(black, "color:a", 1.0, 3.0).From(0.2);
		for (int i = 0; i < 5; i++)
		{
			Color color2 = blink.Color;
			color2.A = (float)GD.RandRange(0.2, 0.4);
			blink.Color = color2;
			SceneTreeTimer source = GetTree().CreateTimer(GD.RandRange(0.1, 0.15), processAlways: false);
			await ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		}
		for (int i = 0; i < 5; i++)
		{
			Color color3 = blink.Color;
			color3.A = (float)GD.RandRange(0.5, 0.8);
			blink.Color = color3;
			SceneTreeTimer source2 = GetTree().CreateTimer(GD.RandRange(0.1, 0.15), processAlways: false);
			await ToSignal(source2, SceneTreeTimer.SignalName.Timeout);
		}
	}
}
