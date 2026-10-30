using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantPumpkinSun : TowerDefensePlant
{
	private const string PUMPKIN_SUN_SKIN_1_1 = "uid://cvjt7q0lfkx1l";

	private const string PUMPKIN_SUN_SKIN_1_2 = "uid://cn3bp63vbxep3";

	private const string PUMPKIN_SUN_SKIN_1_3 = "uid://b24vuwof58261";

	public ProduceComponent produceComponent;

	public FireComponent fireComponent;

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			produceComponent = componentManager.GetRuntime<ProduceComponent>();
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			fireComponent.OnFireReady += FireReady;
			if (!inGame && !editorMapPreviewMode)
			{
				((PumpkinSunSprite)base.sprite).back.ZIndex = 0;
			}
			if (base.currentCustom.Contains("Custom0"))
			{
				((PumpkinSunSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
				((PumpkinSunSprite)base.sprite).back.SetFliter("skin2", open: true);
			}
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.OnFireReady -= FireReady;
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			((PumpkinSunSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
			((PumpkinSunSprite)base.sprite).back.SetFliter("skin2", open: true);
		}
		else
		{
			((PumpkinSunSprite)base.sprite).back.SetFliter("Pumpkin_back", open: true);
			((PumpkinSunSprite)base.sprite).back.SetFliter("skin2", open: false);
		}
	}

	public void FireReady()
	{
		if (fireComponent.runningCheckId == 0)
		{
			fireComponent.fireAnimeClips = "Fire2";
			fireComponent.fireProjectileList[0].checkProjectileId = 0;
		}
		else
		{
			fireComponent.fireAnimeClips = "Fire";
			fireComponent.fireProjectileList[0].checkProjectileId = 1;
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("PumpkinA_skin1_1.png", "uid://cvjt7q0lfkx1l");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("PumpkinA_skin1_1.png", "uid://cn3bp63vbxep3");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("PumpkinA_skin1_1.png", "uid://b24vuwof58261");
			}
			break;
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
			{ "fireNum", fireNum },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		produceInterval = data.GetValueOrDefault("produceInterval", 25.0).AsDouble();
		sunNum = data.GetValueOrDefault("sunNum", 25).AsInt32();
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
