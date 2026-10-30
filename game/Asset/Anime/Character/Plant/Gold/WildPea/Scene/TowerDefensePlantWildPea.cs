using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWildPea : TowerDefensePlant
{
	private FireComponent _fireComponent;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "Pea";

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
				_fireComponent.fireInterval = (float)_fireInterval;
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
				_fireComponent.fireNum = _fireNum;
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
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileName = _projectileName;
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
				((FireComponentProjectileSingle)_fireComponent.fireCheckList[0].projectile).projectileData.skinName = _skinName;
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
				skinName = "Cyber";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "Cyber";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "fireNum", fireNum },
			{ "projectileName", projectileName },
			{ "skinName", skinName },
			{ "fireInterval", fireInterval }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		fireNum = (int)data.GetValueOrDefault("fireNum", 1);
		projectileName = (string)data.GetValueOrDefault("projectileName", "Pea");
		skinName = (string)data.GetValueOrDefault("skinName", "Default");
		fireInterval = (double)data.GetValueOrDefault("fireInterval", 2.0);
	}
}
