using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieSnorkle : TowerDefenseZombie
{
	private float GlobalPositionX
	{
		get
		{
			return GetLogicalGlobalPosition().X;
		}
		set
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = value;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			base.sprite.OnAnimeStarted += AnimeStarted;
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		if (base.inWater)
		{
			instance.maskFlags = 9;
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		if (base.inWater)
		{
			instance.maskFlags = 32;
		}
	}

	public override void InWater()
	{
		base.InWater();
		instance.maskFlags = 32;
	}

	public override void OutWater()
	{
		base.OutWater();
		CreateTween().TweenProperty(base.sprite, "offset", new Vector2(-50f, -80f), 0.25);
		GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 30f;
		instance.maskFlags = 9;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-50f, -80f);
	}

	public void AnimeStarted(string clip)
	{
		if (clip == "Swim")
		{
			base.sprite.offset = new Vector2(-10f, -100f);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		if (clip == "Jump")
		{
			instance.maskFlags = 32;
			base.sprite.offset = new Vector2(-10f, -100f);
			GlobalPositionX -= base.Scale.X * transformPoint.Scale.X * 40f;
		}
		base.AnimeCompleted(clip);
	}
}
