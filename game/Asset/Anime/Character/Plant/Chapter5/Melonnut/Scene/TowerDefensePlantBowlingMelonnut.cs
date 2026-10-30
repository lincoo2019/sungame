using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantBowlingMelonnut : TowerDefensePlantBowlingBase
{
	public FireComponent fireComponent;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
		if (base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantMelonnut");
			if ((towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary() ?? new Dictionary()).GetValueOrDefault("Custom", "").AsString() != "")
			{
				base.currentCustom = new Array<string> { towerDefensePacketValue["Key"].AsGodotDictionary()["Custom"].AsString() };
			}
		}
	}
}
