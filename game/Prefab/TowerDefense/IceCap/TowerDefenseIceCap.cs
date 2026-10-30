using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseIceCap : TowerDefenseGroundItemBase
{
	private Sprite2D iceSprite;

	public Sprite2D iceCapSprite;

	private double _length;

	[Export(PropertyHint.None, "")]
	public double clearTime = 90.0;

	public double clearTimer;

	[Export(PropertyHint.None, "")]
	public double length
	{
		get
		{
			return _length;
		}
		set
		{
			_length = value;
			clearTimer = clearTime;
		}
	}

	public override void _Ready()
	{
		iceSprite = GetNode<Sprite2D>("%IceSprite");
		iceCapSprite = GetNode<Sprite2D>("%IceCapSprite");
		if (!Engine.IsEditorHint())
		{
			iceSprite.Material = (Material)iceSprite.Material.Duplicate();
		}
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!Engine.IsEditorHint())
		{
			iceSprite.Scale = new Vector2(1f + (float)length / 150f, iceSprite.Scale.Y);
			iceCapSprite.Position = new Vector2(0f - (float)length, iceCapSprite.Position.Y);
			(iceSprite.Material as ShaderMaterial).SetShaderParameter("iceLength", 150.0 + length);
			if (clearTime > 0.0)
			{
				clearTime -= delta;
			}
			else
			{
				QueueFree();
			}
		}
	}
}
