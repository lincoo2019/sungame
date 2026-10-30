using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWallnutABC_C : TowerDefensePlant
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && base.config.customData != null)
		{
			Dictionary towerDefensePacketValue = GameSaveManager.Instance.GetTowerDefensePacketValue("PlantWallnutABC_A");
			if ((towerDefensePacketValue.GetValueOrDefault("Key", new Dictionary()).AsGodotDictionary() ?? new Dictionary()).GetValueOrDefault("Custom", "").AsString() != "")
			{
				base.currentCustom = new Array<string> { towerDefensePacketValue["Key"].AsGodotDictionary()["Custom"].AsString() };
			}
		}
	}
}
