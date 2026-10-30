using Godot;

[Tool]
public partial class TowerDefensePlantTorchwoodZ : TowerDefensePlant
{
	private PointLight2D _light;

	private CharacterTimerComponent _timerComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_light = GetNode<PointLight2D>("%Light");
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

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			if (!base.sprite.pause && Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				_timerComponent.Run("Spawn", 25.0);
				return;
			}
			SpawnZombie();
			_timerComponent.Run("Spawn", 25.0);
		}
	}

	public override void SpawnZombie()
	{
		if (characterDisabled || base.sprite.pause)
		{
			return;
		}
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = TowerDefenseCharacter.CreateCharacter("ZombieDancerFire", logicalGlobalPosition, base.gridPos, 0.0);
		zombie.Rise(2.5, 0.0, createDirt: true, changeState: true, 150.0, emitRiseOver: false);
		if (!instance.hypnoses)
		{
			Callable.From(delegate
			{
				zombie.Hypnoses();
			}).CallDeferred();
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieDancerFire", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame)
		{
			if (!_timerComponent.IsRunning("Spawn"))
			{
				_timerComponent.Run("Spawn", 25.0);
			}
			_light.Visible = TowerDefenseManager.GetMapIsNight() && GameSaveManager.Instance.GetConfigValue("MapEffect").AsBool();
		}
	}
}
