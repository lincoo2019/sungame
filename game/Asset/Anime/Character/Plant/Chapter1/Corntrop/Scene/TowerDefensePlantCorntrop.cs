using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCorntrop : TowerDefensePlant
{
	private AttackComponent attackComponent;

	[Export(PropertyHint.None, "")]
	public double attack = 20.0;

	private double _fireInterval = 1.5;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			if (IsNodeReady() && attackComponent != null)
			{
				AttackComponent obj = attackComponent;
				if (obj != null && !obj.IsReleased)
				{
					attackComponent.attackInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			attackComponent.OnAttackSeeded += Attack;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.OnAttackSeeded -= Attack;
		}
	}

	public void Attack(ulong randomSeed)
	{
		using RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator
		{
			Seed = randomSeed
		};
		attackComponent.AttackAllFlag(attack, 2);
		AudioManager.Instance.AudioPlay("ProjectileThrow");
		string text = "Kernal";
		double baseDamage = 20.0;
		int damageFlags = 3;
		if (randomNumberGenerator.Randf() < 0.15f)
		{
			text = "Butter";
			baseDamage = 40.0;
			damageFlags = 2;
		}
		using TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(text);
		towerDefenseProjectileCreateData.baseDamage = baseDamage;
		towerDefenseProjectileCreateData.damageFlags = damageFlags;
		bool flag = Global.IsMultiplayerMode && !MultiPlayerManager.IsHost;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		for (int i = 0; i < 4; i++)
		{
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				useGravity = true,
				gridYOverride = base.gridPos.Y,
				ySpeedOverride = -200.0,
				suppressGameplay = flag
			};
			FireComponent.CreateProjectilePositionByData(this, null, 0.0, logicalGlobalPosition + new Vector2(randomNumberGenerator.RandfRange(-30f, 30f), 30f), new Vector2(randomNumberGenerator.RandfRange(-20f, 20f), 0f), towerDefenseProjectileCreateData, (!flag) ? (-1) : 0, base.camp, Vector2.Zero, overrides);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "attack", attack },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		attack = data.GetValueOrDefault("attack", 20.0).AsDouble();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
