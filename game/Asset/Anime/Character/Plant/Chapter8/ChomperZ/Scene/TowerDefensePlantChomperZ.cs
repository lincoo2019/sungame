using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantChomperZ : TowerDefensePlant
{
	private ChomperComponent _chomperComponent;

	private AttackComponent _attackComponent;

	private CharacterTimerComponent _timerComponent;

	private double _chewTime = 30.0;

	[Export(PropertyHint.None, "")]
	public double chewTime
	{
		get
		{
			return _chewTime;
		}
		set
		{
			_chewTime = value;
			if (IsNodeReady() && _chomperComponent != null && !_chomperComponent.IsReleased)
			{
				_chomperComponent.chewTime = (float)value;
			}
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
		if (characterDisabled || base.sprite.pause || (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter("ZombieNormalChomper", logicalGlobalPosition, base.gridPos, 0.0);
		towerDefenseCharacter.Rise(2.5);
		if (!instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieNormalChomper", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			_attackComponent.SetCheckAreaSegmentLengthX(0, TowerDefenseManager.Instance.GetMapGridSize().X * 1.75f);
			if (!_timerComponent.IsRunning("Spawn"))
			{
				_timerComponent.Run("Spawn", 25.0);
			}
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

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["chewTime"] = chewTime };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		chewTime = (data.ContainsKey("chewTime") ? data["chewTime"].AsDouble() : 30.0);
	}
}
