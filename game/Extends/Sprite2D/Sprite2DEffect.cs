using Godot;

[Tool]
[GlobalClass]
public partial class Sprite2DEffect : Sprite2D
{
	private double _shakeTime;

	private double _modulateRandomTime;

	private double _modulateBlinkTime;

	[Export(PropertyHint.None, "")]
	public bool shake { get; set; }

	[Export(PropertyHint.None, "")]
	public double shakeInterval { get; set; } = 0.05;

	[Export(PropertyHint.None, "")]
	public Vector2 shakeSpread { get; set; } = new Vector2(2f, 2f);

	[Export(PropertyHint.None, "")]
	public bool modulateRandom { get; set; }

	[Export(PropertyHint.None, "")]
	public double modulateRamdomInterval { get; set; } = 5.0;

	[Export(PropertyHint.None, "")]
	public Gradient modulateRamdomGradient { get; set; }

	[Export(PropertyHint.None, "")]
	public bool modulateBlink { get; set; }

	[Export(PropertyHint.None, "")]
	public double modulateBlinkSpeed { get; set; } = 1.0;

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			SetPhysicsProcess(shake || modulateRandom || modulateBlink);
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (shake)
		{
			_shakeTime += delta;
			if (_shakeTime > shakeInterval)
			{
				base.Offset = new Vector2((float)GD.RandRange(0f - shakeSpread.X, shakeSpread.X), (float)GD.RandRange(0f - shakeSpread.Y, shakeSpread.Y));
				_shakeTime -= shakeInterval;
			}
		}
		if (modulateRandom)
		{
			_modulateRandomTime += delta;
			if (_modulateRandomTime > modulateRamdomInterval)
			{
				base.Modulate = modulateRamdomGradient.Sample((float)GD.RandRange(0.0, 1.0));
				_modulateRandomTime -= modulateRamdomInterval;
			}
		}
		if (modulateBlink)
		{
			_modulateBlinkTime += delta * modulateBlinkSpeed;
			base.Modulate = new Color(base.Modulate.R, base.Modulate.G, base.Modulate.B, Mathf.Abs(Mathf.Sin((float)_modulateBlinkTime)));
		}
	}
}
