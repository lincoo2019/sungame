using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCornRotater : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 3.0;

	public int fireNum = 4;

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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireInterval = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			_fireComponent.OnFireVolley += FireVolley;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = _fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			_fireComponent.OnFireVolley -= FireVolley;
		}
	}

	public void FireVolley(ulong randomSeed)
	{
		using RandomNumberGenerator randomNumberGenerator = new RandomNumberGenerator
		{
			Seed = randomSeed
		};
		TowerDefenseProjectileCreateData projectile = _fireComponent.fireCheckList[0].projectile.GetProjectile(randomNumberGenerator);
		for (int i = 0; i < fireNum; i++)
		{
			AudioManager.Instance.AudioPlay("ProjectileThrow");
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				gridYOverride = base.gridPos.Y,
				flipXOverride = (base.Scale.X < 0f)
			};
			_fireComponent.CreateProjectileByData(0, new Vector2(randomNumberGenerator.RandfRange(300f, 400f), 0f), projectile, -1, base.camp, new Vector2(0f, randomNumberGenerator.RandfRange(-50f, 50f)), overrides);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (data.ContainsKey("fireNum") ? data["fireNum"].AsInt32() : 4);
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
