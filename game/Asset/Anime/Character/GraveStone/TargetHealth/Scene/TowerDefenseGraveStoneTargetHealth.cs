using Godot;

[Tool]
public partial class TowerDefenseGraveStoneTargetHealth : TowerDefenseGravestone
{
	private CharacterAabbAreaComponent checkArea;

	private CharacterTimerComponent timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			checkArea = componentManager.GetRuntime<CharacterAabbAreaComponent>("character.aabb_area.0");
			timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			timerComponent.OnTimeout += Timeout;
			checkArea.SetRuntimeRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * 2.75f);
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent characterTimerComponent = timerComponent;
		if (characterTimerComponent != null && !characterTimerComponent.IsReleased)
		{
			timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !timerComponent.IsRunning("Spawn"))
		{
			timerComponent.Run("Spawn", 3.0);
		}
	}

	public void Timeout(string timerName)
	{
		if (!(timerName == "Spawn"))
		{
			return;
		}
		timerComponent.Run("Spawn", 3.0);
		if ((Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || base.sprite.pause)
		{
			return;
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCampFriendlyFromArea(base.camp, checkArea))
		{
			TowerDefenseCharacter towerDefenseCharacter = item.As<TowerDefenseCharacter>();
			if (!(towerDefenseCharacter is TowerDefensePlant) && towerDefenseCharacter.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				towerDefenseCharacter.Health(0.05 * towerDefenseCharacter.instance.hitpointsSave);
				if (towerDefenseCharacter.instance.hitpoints >= towerDefenseCharacter.instance.hitpointsSave)
				{
					towerDefenseCharacter.instance.hitpoints = towerDefenseCharacter.instance.hitpointsSave;
				}
			}
		}
	}
}
