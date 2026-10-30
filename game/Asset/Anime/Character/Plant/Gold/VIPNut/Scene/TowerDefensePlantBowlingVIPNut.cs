using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingVIPNut : TowerDefensePlantBowlingBase
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.config.customData != null)
		{
			Dictionary dictionary = (Dictionary)GameSaveManager.Instance.GetTowerDefensePacketValue("PlantVIPNut").GetValueOrDefault("Key", new Dictionary());
			if ((string)dictionary.GetValueOrDefault("Custom", "") != "")
			{
				base.currentCustom = new Array<string> { (string)dictionary["Custom"] };
			}
		}
	}
}
