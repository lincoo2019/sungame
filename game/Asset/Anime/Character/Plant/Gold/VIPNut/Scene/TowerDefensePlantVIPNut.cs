using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantVIPNut : TowerDefensePlant
{
	private const string VIP_NUT_SKIN_2_1 = "uid://3lrskq4eia8b";

	private const string VIP_NUT_SKIN_2_2 = "uid://c6lmbuq33tm3";

	private const string VIP_NUT_SKIN_2_3 = "uid://frbgmrgbd7s1";

	private const string VIP_NUT_SKIN_4_1 = "uid://bisp36tuaclyt";

	private const string VIP_NUT_SKIN_4_2 = "uid://jh80dvtmfn58";

	private const string VIP_NUT_SKIN_4_3 = "uid://iywdryeb7h4g";

	private ProduceComponent _produceComponent;

	private GrowUpComponent _growUpComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private double _growUpTime = 60.0;

	[Export(PropertyHint.None, "")]
	public int growUpSunNum = 50;

	private string _produceType = "Sun";

	[Export(PropertyHint.None, "")]
	public int dieCreateSunNum = 200;

	public bool over;

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
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceInterval = (float)_produceInterval;
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
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = _sunNum;
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
			if (IsNodeReady() && _growUpComponent != null)
			{
				GrowUpComponent growUpComponent = _growUpComponent;
				if (growUpComponent != null && !growUpComponent.IsReleased && _growUpComponent.growUpTime.Count > 0)
				{
					_growUpComponent.growUpTime[0] = (float)_growUpTime;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public string produceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceType = _produceType;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			_growUpComponent = componentManager.GetRuntime<GrowUpComponent>();
			if (_growUpComponent != null)
			{
				_growUpComponent.OnGrow += GowUp;
			}
			_produceComponent.produceInterval = (float)produceInterval;
			_produceComponent.num = sunNum;
			if (_growUpComponent.growUpTime.Count > 0)
			{
				_growUpComponent.growUpTime[0] = (float)growUpTime;
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		GrowUpComponent growUpComponent = _growUpComponent;
		if (growUpComponent != null && !growUpComponent.IsReleased)
		{
			_growUpComponent.OnGrow -= GowUp;
		}
	}

	public void GowUp(int reach)
	{
		if (reach == 0)
		{
			_produceComponent.num = growUpSunNum;
			dieCreateSunNum = 400;
			instance.height = TowerDefenseEnum.CHARACTER_HEIGHT.TALL;
			if (_growUpComponent.ShouldApplyAuthoritativeGrowthEffects)
			{
				Health(4000.0);
				instance.hitpointsSave += 4000.0;
			}
		}
	}

	public override void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		for (int i = 0; (double)i < Mathf.Floor((double)dieCreateSunNum / 50.0); i++)
		{
			if (TowerDefenseManager.Instance.IsIZMMode() || TowerDefenseManager.Instance.IsIZM2Mode() || instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
			else
			{
				SunCreate(logicalGlobalPosition, 50L, TowerDefenseEnum.SUN_MOVING_METHOD.GRAVITY, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		switch (damagePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("VIPNut_skin2_1.png", "uid://3lrskq4eia8b");
				base.sprite.SetAtlasReplace("VIPNut_skin4_1.png", "uid://bisp36tuaclyt");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("VIPNut_skin2_1.png", "uid://c6lmbuq33tm3");
				base.sprite.SetAtlasReplace("VIPNut_skin4_1.png", "uid://jh80dvtmfn58");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("VIPNut_skin2_1.png", "uid://frbgmrgbd7s1");
				base.sprite.SetAtlasReplace("VIPNut_skin4_1.png", "uid://iywdryeb7h4g");
			}
			break;
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "produceInterval", produceInterval },
			{ "sunNum", sunNum },
			{ "growUpTime", growUpTime },
			{ "growUpSunNum", growUpSunNum },
			{ "dieCreateSunNum", dieCreateSunNum },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (double)data.GetValueOrDefault("produceInterval", 25.0);
		sunNum = (int)data.GetValueOrDefault("sunNum", 25);
		growUpTime = (double)data.GetValueOrDefault("growUpTime", 60.0);
		growUpSunNum = (int)data.GetValueOrDefault("growUpSunNum", 50);
		dieCreateSunNum = (int)data.GetValueOrDefault("dieCreateSunNum", 200);
		over = (bool)data.GetValueOrDefault("over", false);
	}
}
