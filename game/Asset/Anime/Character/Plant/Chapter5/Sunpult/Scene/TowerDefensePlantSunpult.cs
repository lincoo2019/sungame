using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSunpult : TowerDefensePlant
{
	public FireComponent fireComponent;

	public ProduceComponent produceComponent;

	public GrowUpComponent growUpComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 15;

	private double _growUpTime = 60.0;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Sunshroom";

	private string _skinName = "Default";

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

	public string skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			_skinName = value;
			if (IsNodeReady() && this.fireComponent != null)
			{
				FireComponent fireComponent = this.fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && this.fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)this.fireComponent.fireCheckList[0].projectile).projectileData.skinName = value;
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
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Santa";
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

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Santa";
		}
		else
		{
			skinName = "Default";
		}
	}

	public virtual void GowUp(int reach)
	{
		if (reach == 0)
		{
			sunNum = 25;
			produceComponent.num = sunNum;
			FireComponentProjectileSingle obj = (FireComponentProjectileSingle)fireComponent.fireCheckList[0].projectile;
			obj.projectileData = (TowerDefenseProjectileCreateData)obj.projectileData.Duplicate(deep: true);
			obj.projectileData.baseDamage = 80.0;
			obj.projectileData.scale = new Vector2(1.5f, 1.5f);
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
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum },
			{ "growUpTime", growUpTime },
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "skinName", skinName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 15).AsInt32();
		growUpTime = data.GetValueOrDefault("growUpTime", 60.0).AsDouble();
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Sunshroom").AsString();
		skinName = data.GetValueOrDefault("skinName", "Default").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 3.0).AsDouble();
	}
}
