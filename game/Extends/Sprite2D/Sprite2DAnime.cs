using Godot;

[GlobalClass]
[Tool]
public partial class Sprite2DAnime : Sprite2D
{
	private int _frameNum;

	private double _timer;

	[Export(PropertyHint.None, "")]
	public bool isPlaying { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public double fps { get; set; } = 24.0;

	public override void _Ready()
	{
		_frameNum = base.Hframes * base.Vframes;
	}

	public override void _Process(double delta)
	{
		if (isPlaying)
		{
			if (Engine.IsEditorHint())
			{
				_frameNum = base.Hframes * base.Vframes;
			}
			if (_timer < 1.0 / fps)
			{
				_timer += delta;
				return;
			}
			_timer = 0.0;
			base.Frame = (base.Frame + 1) % _frameNum;
		}
	}
}
