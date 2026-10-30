using Godot;

[GlobalClass]
public partial class TowerDefenseProjectileEffectMagicPeaBlow : TowerDefenseProjectileEffectBase
{
	[Export(PropertyHint.None, "")]
	public float duration = 0.5f;

	[Export(PropertyHint.None, "")]
	public float startScale = 0.25f;

	[Export(PropertyHint.None, "")]
	public float endScale = 1.25f;

	[Export(PropertyHint.None, "")]
	public string spriteNodeName = "Blow";

	private Sprite2D _blowSprite;

	private bool _started;

	public override void _Ready()
	{
		_blowSprite = GetNodeOrNull<Sprite2D>(spriteNodeName);
		if (!GodotObject.IsInstanceValid(_blowSprite))
		{
			_blowSprite = GetNodeOrNull<Sprite2D>("Blow");
		}
		if (!GodotObject.IsInstanceValid(_blowSprite))
		{
			QueueFree();
		}
		else
		{
			PlaySpread();
		}
	}

	private void PlaySpread()
	{
		if (!_started)
		{
			_started = true;
			Vector2 vector = Vector2.One * Mathf.Max(0.001f, startScale);
			Vector2 vector2 = Vector2.One * Mathf.Max(0.001f, endScale);
			float num = Mathf.Max(0.01f, duration);
			_blowSprite.Scale = vector;
			_blowSprite.Modulate = new Color(1f, 1f, 1f);
			Tween tween = CreateTween();
			tween.SetParallel();
			tween.TweenProperty(_blowSprite, "scale", vector2, num).From(vector);
			tween.TweenProperty(_blowSprite, "modulate:a", 0f, num).From(1f);
			tween.Chain().TweenCallback(Callable.From(FinishSpread));
		}
	}

	private void FinishSpread()
	{
		QueueFree();
	}
}
