using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantHamburger : TowerDefensePlant
{
	private const string HAMBURGER00048 = "uid://cdyxcmno3lbul";

	private const string HAMBURGER00048_CRACKED2 = "uid://c88v00eh32drf";

	private const string HAMBURGER00075 = "uid://y475jqw773qy";

	private const string HAMBURGER00075_CRACKED2 = "uid://cxr5yeh7tvshg";

	private const string HAMBURGER00093 = "uid://bjb1lpeapkdcc";

	private const string HAMBURGER00093_CRACKED1 = "uid://cqi6388l2ywvi";

	private const string HAMBURGER00093_CRACKED2 = "uid://bpmrgqnlh2spp";

	private const string HAMBURGER00111 = "uid://dpipyjlhirdmw";

	private const string HAMBURGER00111_CRACKED1 = "uid://d08qy1cn78ee1";

	private const string HAMBURGER00111_CRACKED2 = "uid://b1bnqu2orejfu";

	private const string HAMBURGER_SKIN41 = "uid://jbyw6u6588t7";

	private const string HAMBURGER_SKIN43 = "uid://b7w3ndq734j6u";

	private const string HAMBURGER_SKIN51 = "uid://cbsvs0g4yvx4k";

	private const string HAMBURGER_SKIN52 = "uid://braukofb8mhes";

	private const string HAMBURGER_SKIN53 = "uid://vujm3aqinku7";

	private ProduceComponent _produceComponent;

	private FireComponent _fireComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 50;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

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
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased)
				{
					_fireComponent.fireNum = value;
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
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			base.sprite.SetAtlasReplace("hamburger_0009_3.png", "uid://bjb1lpeapkdcc");
			base.sprite.SetAtlasReplace("hamburger_0011_1.png", "uid://dpipyjlhirdmw");
			base.sprite.SetAtlasReplace("hamburger_0004_8.png", "uid://cdyxcmno3lbul");
			base.sprite.SetAtlasReplace("hamburger_0007_5.png", "uid://y475jqw773qy");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("hamburger_skin5_1.png", "uid://cbsvs0g4yvx4k");
				base.sprite.SetAtlasReplace("hamburger_skin4_1.png", "uid://jbyw6u6588t7");
			}
			break;
		case "Damage1":
			base.sprite.SetAtlasReplace("hamburger_0009_3.png", "uid://cqi6388l2ywvi");
			base.sprite.SetAtlasReplace("hamburger_0011_1.png", "uid://d08qy1cn78ee1");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("hamburger_skin5_1.png", "uid://braukofb8mhes");
			}
			break;
		case "Damage2":
			base.sprite.SetAtlasReplace("hamburger_0009_3.png", "uid://bpmrgqnlh2spp");
			base.sprite.SetAtlasReplace("hamburger_0011_1.png", "uid://b1bnqu2orejfu");
			base.sprite.SetAtlasReplace("hamburger_0004_8.png", "uid://c88v00eh32drf");
			base.sprite.SetAtlasReplace("hamburger_0007_5.png", "uid://cxr5yeh7tvshg");
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("hamburger_skin5_1.png", "uid://vujm3aqinku7");
				base.sprite.SetAtlasReplace("hamburger_skin4_1.png", "uid://b7w3ndq734j6u");
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
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum,
			["fireNum"] = fireNum,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 50);
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 2.0);
	}
}
