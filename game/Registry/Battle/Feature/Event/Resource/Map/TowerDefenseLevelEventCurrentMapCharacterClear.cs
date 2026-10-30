using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCurrentMapCharacterClear : TowerDefenseLevelEventBase
{
	public override string _GetName()
	{
		return "LEVLE_EVENT_CURRENTMAP_CHARACTER_CLEAR";
	}

	public override void Execute()
	{
		TowerDefenseMap currentMap = TowerDefenseManager.Instance.GetCurrentMap();
		if (GodotObject.IsInstanceValid(currentMap))
		{
			currentMap.CharacterClear();
			return;
		}
		TowerDefenseManager.GetMapFeature()?.TryEnqueuePendingMapAction(delegate
		{
			TowerDefenseMap currentMap2 = TowerDefenseManager.Instance.GetCurrentMap();
			if (GodotObject.IsInstanceValid(currentMap2))
			{
				currentMap2.CharacterClear();
			}
		});
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CurrentMapCharacterClear",
			["Value"] = new Dictionary()
		};
	}
}
