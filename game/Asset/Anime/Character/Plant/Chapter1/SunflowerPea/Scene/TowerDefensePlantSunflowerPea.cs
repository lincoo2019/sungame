using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunflowerPea : TowerDefensePlant
{
	private FireComponent fireComponent;

	private ProduceComponent produceComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireInterval = (float)value;
				}
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					this.fireComponent.fireNum = value;
				}
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
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileName = value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && produceComponent != null)
			{
				ProduceComponent obj = produceComponent;
				if (obj != null && !obj.IsReleased)
				{
					produceComponent.num = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			produceComponent = componentManager.GetRuntime<ProduceComponent>();
			timeScaleInit = 1.25;
		}
	}

	public override void UnlimitedFireInit()
	{
		fireInterval /= 2.0;
		produceComponent.OnProduct += UnlimitedFireProduct;
		produceComponent.onlyEmit = true;
		produceInterval /= 3.0;
	}

	public async void UnlimitedFireProduct(int _pos = -1, int _num = 25)
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		if (fireComponent.CanFireCheckOnceByName(projectileName, instance.collisionFlags + 2))
		{
			Vector2 up = Vector2.Up;
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				spawnTweenOffset = up * 50f,
				spawnTweenDuration = 0.5f,
				spawnTweenEase = Tween.EaseType.Out,
				spawnTweenTrans = Tween.TransitionType.Quart
			};
			fireComponent.CreateProjectileByData(1, new Vector2(600f, 0f), new TowerDefenseProjectileCreateData("Sun"), instance.collisionFlags + 2, base.camp, Vector2.Zero, overrides);
		}
		else
		{
			produceComponent.Create(GetLogicalGlobalPosition(produceComponent.marker[0]), _num);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Pea").AsString();
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 25).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
