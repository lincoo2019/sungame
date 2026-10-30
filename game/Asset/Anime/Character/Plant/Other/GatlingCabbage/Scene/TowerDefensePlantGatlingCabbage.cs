using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantGatlingCabbage : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "Cabbage";

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
				_fireComponent.onlyEmitSignal = _fireInterval < 0.5;
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			if (IsNodeReady() && _fireComponent != null)
			{
				_fireComponent.fireNum = _fireNum;
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
		if (fireInterval < 0.5)
		{
			int num = Mathf.Max(1, (int)((double)Mathf.Floor((float)((0.4 - fireInterval) / 0.1)) * 2.0));
			for (int i = 0; i < num; i++)
			{
				AudioManager.Instance.AudioPlay("ProjectileThrow");
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = base.gridPos.Y,
					flipXOverride = (base.Scale.X < 0f)
				};
				_fireComponent.CreateProjectileByData(0, new Vector2(randomNumberGenerator.RandfRange(300f, 800f), 0f), _fireComponent.fireCheckList[0].projectile.GetProjetile(), -1, base.camp, new Vector2(0f, randomNumberGenerator.RandfRange(-30f, 20f)), overrides);
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
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "Cabbage");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
