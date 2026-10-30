using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunShroomCharge : TowerDefensePlant
{
	private ProduceComponent _produceComponent;

	private const int MaxLevel = 6;

	private const double HpPerLevel = 300.0;

	private const int SunPerLevel = 15;

	private const float ScalePerLevel = 0.2f;

	[Export(PropertyHint.None, "")]
	public int level = 1;

	private double _produceInterval = 25.0;

	private int _sunNum = 15;

	private string _produceType = "Sun";

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
					_produceComponent.produceInterval = (float)value;
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
					_produceComponent.num = value;
				}
			}
		}
	}

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,QXSun")]
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
					_produceComponent.produceType = value;
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
			if (level > 1)
			{
				ApplyLevelStats(level);
			}
		}
	}

	public override void Cover(TowerDefenseCharacter character)
	{
		if (base.config != null && character.config != null && character.config.name == base.config.name && character is TowerDefensePlantSunShroomCharge towerDefensePlantSunShroomCharge)
		{
			if (towerDefensePlantSunShroomCharge.level >= 6)
			{
				level = 6;
				ApplyLevelStats(level);
			}
			else
			{
				level = Mathf.Clamp(towerDefensePlantSunShroomCharge.level + 1, 1, 6);
				ApplyLevelStats(level);
			}
			if (character.instance.wakeUp)
			{
				instance.wakeUp = true;
			}
		}
	}

	private void ApplyLevelStats(int lv)
	{
		lv = Mathf.Clamp(lv, 1, 6);
		int num = lv - 1;
		double num2 = ((base.config != null) ? base.config.hitpoints : 300.0);
		if (instance != null)
		{
			instance.hitpointsSave = num2 + 300.0 * (double)num;
			instance.hitpoints = instance.hitpointsSave;
			instance.height = ((lv < 3) ? TowerDefenseEnum.CHARACTER_HEIGHT.LOW : TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL);
		}
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			_produceComponent.num = 15 + 15 * num;
		}
		float num3 = 1f + 0.2f * (float)num;
		if (base.sprite != null)
		{
			base.sprite.Scale = new Vector2(num3, num3);
			base.sprite.clip = ((lv > 1) ? "Shooting" : "Idle");
		}
		if (shadowSprite != null)
		{
			shadowSprite.Scale = new Vector2(0.4f * num3, 0.4f * num3);
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		ProduceComponent produceComponent = _produceComponent;
		if (produceComponent != null && !produceComponent.IsReleased)
		{
			_produceComponent.produceType = (instance.hypnoses ? "BrainSun" : "Sun");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["level"] = level,
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		level = ((!data.ContainsKey("level")) ? 1 : data["level"].AsInt32());
		ApplyLevelStats(level);
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 15);
	}
}
