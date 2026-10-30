using System;
using Godot;

public partial class TowerDefenseCellClearEmptyRuntimeProbe : Node
{
	public override void _Ready()
	{
		TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance();
		TowerDefenseCharacter towerDefenseCharacter = new TowerDefenseCharacter();
		TowerDefenseCharacter towerDefenseCharacter2 = new TowerDefenseCharacter();
		TowerDefenseCharacter towerDefenseCharacter3 = new TowerDefenseCharacter();
		TowerDefenseCharacter towerDefenseCharacter4 = new TowerDefenseCharacter();
		towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter] = towerDefenseCharacter2;
		towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter3] = towerDefenseCharacter2;
		towerDefenseCellInstance.characterSlotDictionary[towerDefenseCharacter4] = towerDefenseCharacter2;
		towerDefenseCharacter3.Free();
		towerDefenseCharacter4.Free();
		towerDefenseCellInstance.dirty = true;
		bool flag = true;
		try
		{
			towerDefenseCellInstance.ClearEmpty();
		}
		catch (Exception value)
		{
			flag = false;
			GD.PushError($"TowerDefenseCellInstance.ClearEmpty threw: {value}");
		}
		bool flag2 = towerDefenseCellInstance.characterSlotDictionary.Count == 1;
		TowerDefenseCharacter value2;
		bool flag3 = towerDefenseCellInstance.characterSlotDictionary.TryGetValue(towerDefenseCharacter, out value2) && value2 == towerDefenseCharacter2;
		bool flag4 = !towerDefenseCellInstance.dirty;
		bool flag5 = false;
		try
		{
			towerDefenseCellInstance.ClearEmpty();
			flag5 = towerDefenseCellInstance.characterSlotDictionary.Count == 1;
		}
		catch (Exception value3)
		{
			GD.PushError($"TowerDefenseCellInstance.ClearEmpty idempotence failed: {value3}");
		}
		bool flag6 = flag && flag2 && flag3 && flag4 && flag5;
		GD.Print($"[TOWER_DEFENSE_CELL_CLEAR_EMPTY_PROBE] noException={flag} removedInvalidKeys={flag2} preservedValidEntry={flag3} dirtyCleared={flag4} idempotent={flag5} passed={flag6}");
		towerDefenseCharacter.Free();
		towerDefenseCharacter2.Free();
		towerDefenseCellInstance.Dispose();
		GetTree().Quit((!flag6) ? 1 : 0);
	}
}
