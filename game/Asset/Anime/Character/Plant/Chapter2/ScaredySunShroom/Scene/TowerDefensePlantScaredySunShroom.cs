using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantScaredySunShroom : TowerDefensePlant
{
	private FireComponent fireComponent;

	private ProduceComponent produceComponent;

	private GrowUpComponent growUpComponent;

	private ScaredComponent scaredComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 1;

	private string _projectileName = "Puff";

	private double _produceInterval = 25.0;

	private int _sunNum = 15;

	private double _growUpTime = 60.0;

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

	[Export(PropertyHint.None, "")]
	public double growUpTime
	{
		get
		{
			return _growUpTime;
		}
		set
		{
			_growUpTime = value;
			if (IsNodeReady() && growUpComponent != null)
			{
				GrowUpComponent obj = growUpComponent;
				if (obj != null && !obj.IsReleased)
				{
					growUpComponent.growUpTime[0] = (float)value;
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
			growUpComponent = componentManager.GetRuntime<GrowUpComponent>();
			if (growUpComponent != null)
			{
				growUpComponent.OnGrow += GowUp;
			}
			scaredComponent = componentManager.GetRuntime<ScaredComponent>();
			if (scaredComponent == null)
			{
				GD.PushError("ScaredySunShroom is missing its Scared resource runtime.");
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GrowUpComponent obj = growUpComponent;
		if (obj != null && !obj.IsReleased)
		{
			growUpComponent.OnGrow -= GowUp;
		}
	}

	public void GowUp(int reach)
	{
		if (reach == 0)
		{
			sunNum = 25;
			produceComponent.num = sunNum;
			TowerDefenseProjectileCreateData projectileData = ((FireComponentProjectileSingle)fireComponent.fireCheckList[0].projectile).projectileData;
			projectileData.baseDamage = 40.0;
			projectileData.scale = Vector2.One * 1.5f;
			projectileData.InvalidateConfigCache();
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
			{ "growUpTime", growUpTime },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Puff").AsString();
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 15).AsInt32();
		growUpTime = data.GetValueOrDefault("growUpTime", 60.0).AsDouble();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
