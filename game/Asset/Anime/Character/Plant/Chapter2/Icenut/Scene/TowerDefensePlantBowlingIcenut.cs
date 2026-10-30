using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingIcenut : TowerDefensePlantBowlingBase
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.config.customData != null)
		{
			string text = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantMelonnut").GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary()
				.GetValueOrDefault("Custom", "")
				.AsString();
			if (text != "")
			{
				base.currentCustom = new Array<string> { text };
			}
		}
	}
}
