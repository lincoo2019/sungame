using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventSetGameMode : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.GAMEMODE gameMode;

	public override string _GetName()
	{
		return "LEVLE_EVENT_SET_GAME_MODE";
	}

	public override void Execute()
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return;
		}
		switch (gameMode)
		{
		case TowerDefenseEnum.GAMEMODE.HAMMER:
			if (!currentControl.featureDictionary.ContainsKey("Hammer"))
			{
				currentControl.AddFeature("Hammer", new Dictionary());
			}
			break;
		case TowerDefenseEnum.GAMEMODE.TOWERDEFENSE:
			if (currentControl.featureDictionary.ContainsKey("Hammer"))
			{
				currentControl.RemoveFeature("Hammer");
			}
			break;
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		string value = valueDictionary.GetValueOrDefault("GameMode", "TOWERDEFENSE").AsString().ToUpper();
		gameMode = Enum.Parse<TowerDefenseEnum.GAMEMODE>(value);
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "SetGameMode",
			["Value"] = new Dictionary { ["GameMode"] = Enum.GetName(gameMode) }
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["游戏模式"] = new Dictionary { ["模式"] = new Dictionary
		{
			["Object"] = this,
			["Type"] = "Enum",
			["Property"] = "gameMode",
			["Hint"] = 0,
			["Rest"] = 0
		} };
		return property;
	}
}
