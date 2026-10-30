using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantIceCobCannon : TowerDefensePlant
{
	private CannonComponent _cannonComponent;

	private double _restTime = 30.0;

	private string _skinName = "Default";

	[Export(PropertyHint.None, "")]
	public double restTime
	{
		get
		{
			return _restTime;
		}
		set
		{
			_restTime = value;
			if (IsNodeReady() && _cannonComponent != null)
			{
				CannonComponent cannonComponent = _cannonComponent;
				if (cannonComponent != null && !cannonComponent.IsReleased)
				{
					_cannonComponent.restTime = (float)value;
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
			CannonComponent cannonComponent = _cannonComponent;
			if (cannonComponent != null && !cannonComponent.IsReleased && _cannonComponent.projectileData != null)
			{
				_cannonComponent.projectileData.skinName = value;
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_cannonComponent = componentManager.GetRuntime<CannonComponent>();
			if (base.currentCustom.Contains("Custom0"))
			{
				skinName = "IceCream";
			}
		}
	}

	public override void OnCustomSwitched(string customKey)
	{
		if (customKey == "Custom0")
		{
			skinName = "IceCream";
		}
		else
		{
			skinName = "Default";
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "fire")
		{
			AudioManager.Instance?.AudioPlay("CobLaunch");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["restTime"] = restTime,
			["skinName"] = skinName
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		restTime = (data.ContainsKey("restTime") ? data["restTime"].AsDouble() : 30.0);
		skinName = (data.ContainsKey("skinName") ? data["skinName"].AsString() : "Default");
	}
}
