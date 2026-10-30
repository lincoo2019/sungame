using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantNutFlower : TowerDefensePlant
{
	private const string NUT_FLOWER_HEAD0 = "uid://be82j51vjke1j";

	private const string NUT_FLOWER_HEAD1 = "uid://bxyhuq515ddlw";

	private const string NUT_FLOWER_HEAD2 = "uid://bouacd0clkgti";

	private const string NUT_FLOWER_PETALS0 = "uid://cso7gcei1860x";

	private const string NUT_FLOWER_PETALS1 = "uid://dlwrnuy6lpacf";

	private const string NUT_FLOWER_PETALS2 = "uid://c1syccnl5bb86";

	private ProduceComponent _produceComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private string _produceType = "packet";

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
			if (IsNodeReady())
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
			if (IsNodeReady())
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = value;
				}
			}
		}
	}

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,packet,QXSun")]
	public string produceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady())
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
			_produceComponent.produceInterval = (float)produceInterval;
			_produceComponent.num = sunNum;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("NutFlower_head0.png", "uid://be82j51vjke1j");
			base.sprite.SetAtlasReplace("NutFlower_petals0.png", "uid://cso7gcei1860x");
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("NutFlower_head0.png", "uid://bxyhuq515ddlw");
			base.sprite.SetAtlasReplace("NutFlower_petals0.png", "uid://dlwrnuy6lpacf");
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("NutFlower_head0.png", "uid://bouacd0clkgti");
			base.sprite.SetAtlasReplace("NutFlower_petals0.png", "uid://c1syccnl5bb86");
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 25);
	}
}
