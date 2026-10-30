using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantMedicPot : TowerDefensePlant
{
	private CharacterTimerComponent _timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 1.0);
		}
	}

	public void Timeout(string timerName)
	{
		if (!(timerName == "Spawn"))
		{
			return;
		}
		if (!base.sprite.pause && !instance.sleep && GodotObject.IsInstanceValid(cell))
		{
			Dictionary dictionary = new Dictionary();
			foreach (TowerDefenseCharacter character in cell.GetCharacterList())
			{
				if (!character.die && !character.nearDie && !(character is TowerDefenseCrater) && !(character is TowerDefenseItem) && !(character is TowerDefenseGravestone) && !(character is TowerDefensePlantBowlingBase) && !CheckDifferentCamp(character.camp) && character.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS && !(character.instance.hitpoints >= character.instance.hitpointsSave))
				{
					character.Health(0.01 * character.instance.hitpointsSave);
					if (character.instance.hitpoints >= character.instance.hitpointsSave)
					{
						character.instance.hitpoints = character.instance.hitpointsSave;
					}
					dictionary[character] = true;
				}
			}
			foreach (TowerDefenseCharacter character2 in cell.GetCharacterList())
			{
				if (dictionary.ContainsKey(character2) || character2.instance.hitpoints >= character2.instance.hitpointsSave || !(character2.config is TowerDefensePlantConfig towerDefensePlantConfig) || towerDefensePlantConfig.extendGrid.Count == 0)
				{
					continue;
				}
				foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
				{
					if (TowerDefenseManager.GetMapCell(character2.gridPos + item) == cell)
					{
						character2.Health(0.01 * character2.instance.hitpointsSave);
						if (character2.instance.hitpoints >= character2.instance.hitpointsSave)
						{
							character2.instance.hitpoints = character2.instance.hitpointsSave;
						}
						dictionary[character2] = true;
						break;
					}
				}
			}
		}
		_timerComponent.Run("Spawn", 1.0);
	}
}
