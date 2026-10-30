using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHWC : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 1.5;

	private string _projectileName = "Spike";

	public int fireNum = 12;

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
				_fireComponent.fireInterval = (float)_fireInterval;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
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
		for (int i = 0; i <= 2; i++)
		{
			for (int j = 0; j < fireNum; j++)
			{
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				_fireComponent.CreateProjectileByData(i, new Vector2(randomNumberGenerator.RandfRange(300f, 400f), 0f), _fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, new Vector2(0f, randomNumberGenerator.RandfRange(-50f, 50f)), overrides);
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (int)data.GetValueOrDefault("fireNum", 12);
		projectileName = (string)data.GetValueOrDefault("projectileName", "Spike");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 1.5);
	}
}
