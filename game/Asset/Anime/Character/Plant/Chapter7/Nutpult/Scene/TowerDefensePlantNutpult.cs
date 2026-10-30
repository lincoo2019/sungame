using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantNutpult : TowerDefensePlant
{
	private const string NUT_PULT_SKIN11 = "uid://ctkrk4pg3v5g8";

	private const string NUT_PULT_SKIN12 = "uid://btg4rkfp3v5c0";

	private const string NUT_PULT_SKIN13 = "uid://bnsn0dnoeyfh0";

	private FireComponent _fireComponent;

	private double _fireInterval = 3.0;

	private int _fireNum = 1;

	private string _projectileName = "Nut";

	private string _skinName = "Default";

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
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = value;
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
			if (IsNodeReady() && _fireComponent != null)
			{
				FireComponent fireComponent = _fireComponent;
				if (fireComponent != null && !fireComponent.IsReleased && _fireComponent.fireCheckList.Count > 0)
				{
					((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.skinName = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "Egg";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Egg";
		}
		else
		{
			skinName = "Default";
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
				base.sprite.SetAtlasReplace("NutPult_skin1_1.png", "uid://ctkrk4pg3v5g8");
			}
			break;
		case "Damage1":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("NutPult_skin1_1.png", "uid://btg4rkfp3v5c0");
			}
			break;
		case "Damage2":
			if (base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("NutPult_skin1_1.png", "uid://bnsn0dnoeyfh0");
			}
			break;
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["skinName"] = skinName,
			["fireInterval"] = fireInterval
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = ((!data.ContainsKey("fireNum")) ? 1 : data["fireNum"].AsInt32());
		projectileName = (data.ContainsKey("projectileName") ? data["projectileName"].AsString() : "Nut");
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
		fireInterval = (data.ContainsKey("fireInterval") ? data["fireInterval"].AsDouble() : 3.0);
	}
}
