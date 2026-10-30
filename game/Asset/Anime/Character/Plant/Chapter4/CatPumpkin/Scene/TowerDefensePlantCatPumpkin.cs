using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantCatPumpkin : TowerDefensePlant
{
	private const string CAT_PUMPKIN_SKIN_1_1 = "uid://b808y0h42q37k";

	private const string CAT_PUMPKIN_SKIN_1_2 = "uid://dly80yge8yx84";

	private const string CAT_PUMPKIN_SKIN_1_3 = "uid://bgnypakc5u5pl";

	public FireComponent fireComponent;

	private double _fireInterval = 1.5;

	private int _fireNum = 2;

	private string _projectileName = "Spike";

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

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (!inGame && !editorMapPreviewMode)
			{
				((CatPumpkinSprite)base.sprite).back.ZIndex = 0;
			}
			if (base.currentCustom.Contains("Custom0"))
			{
				((CatPumpkinSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
				((CatPumpkinSprite)base.sprite).back.SetFliter("skin2", open: true);
			}
			RefreshBackLayers();
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			((CatPumpkinSprite)base.sprite).back.SetFliter("Pumpkin_back", open: false);
			((CatPumpkinSprite)base.sprite).back.SetFliter("skin2", open: true);
		}
		else
		{
			((CatPumpkinSprite)base.sprite).back.SetFliter("Pumpkin_back", open: true);
			((CatPumpkinSprite)base.sprite).back.SetFliter("skin2", open: false);
		}
		RefreshBackLayers();
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		switch (damangePointName)
		{
		case "Damage0":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("CatPumpkin_skin1_1.png", "uid://b808y0h42q37k");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("CatPumpkin_skin1_1.png", "uid://dly80yge8yx84");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("CatPumpkin_skin1_1.png", "uid://bgnypakc5u5pl");
			}
			break;
		}
		RefreshBackLayers();
	}

	private void RefreshBackLayers()
	{
		AdobeAnimateSpriteBase back = ((CatPumpkinSprite)base.sprite).back;
		if (!GodotObject.IsInstanceValid(back) || back.flashAnimeData == null)
		{
			return;
		}
		foreach (Variant key in back.flashAnimeData.layerDictionary.Keys)
		{
			StringName stringName = (StringName)key;
			if (stringName != (StringName)"Pumpkin_back" && stringName != (StringName)"skin2")
			{
				back.SetFliter(stringName, open: false);
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
		fireNum = data.GetValueOrDefault("fireNum", 2).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "Spike").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 1.5).AsDouble();
	}
}
