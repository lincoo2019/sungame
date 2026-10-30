using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseMedicPlantern : TowerDefensePlant
{
	private CharacterAabbAreaComponent _checkArea;

	private CharacterTimerComponent _timerComponent;

	private Vector2 _healRange;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_checkArea = componentManager.GetRuntime<CharacterAabbAreaComponent>("character.aabb_area.0");
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			AudioManager.Instance.AudioPlay("Plantern");
			_checkArea.SetRuntimeRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
			_healRange = TowerDefenseManager.Instance.GetMapGridSize() * 2.75f * 0.5f;
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
		if (!base.sprite.pause && !instance.sleep)
		{
			Array campFriendly = TowerDefenseManager.Instance.GetCampFriendly(base.camp);
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			foreach (Variant item in campFriendly)
			{
				TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
				if (towerDefenseCharacter is TowerDefensePlantBowlingBase || towerDefenseCharacter.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS || towerDefenseCharacter.instance.hitpoints >= towerDefenseCharacter.instance.hitpointsSave)
				{
					continue;
				}
				Vector2 vector = (towerDefenseCharacter.GetLogicalGlobalPosition() - logicalGlobalPosition).Abs();
				if (!(vector.X > _healRange.X) && !(vector.Y > _healRange.Y))
				{
					towerDefenseCharacter.Health(0.01 * towerDefenseCharacter.instance.hitpointsSave);
					if (towerDefenseCharacter.instance.hitpoints >= towerDefenseCharacter.instance.hitpointsSave)
					{
						towerDefenseCharacter.instance.hitpoints = towerDefenseCharacter.instance.hitpointsSave;
					}
				}
			}
		}
		_timerComponent.Run("Spawn", 1.0);
	}
}
