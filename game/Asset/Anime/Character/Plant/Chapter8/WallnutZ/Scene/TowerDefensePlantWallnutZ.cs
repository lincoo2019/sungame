using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantWallnutZ : TowerDefensePlant
{
	private const string WALL_NUT_SKIN11 = "uid://ccqw6dynty45q";

	private const string WALL_NUT_SKIN12 = "uid://bpkbxlpelgb0c";

	private CharacterTimerComponent _timerComponent;

	public bool over;

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

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (!(damangePointName == "Damage0"))
		{
			if (damangePointName == "Damage1" && base.currentCustom.Contains("Custom0"))
			{
				base.sprite.SetAtlasReplace("WallnutZ_skin1_1.png", "uid://bpkbxlpelgb0c");
			}
		}
		else if (base.currentCustom.Contains("Custom0"))
		{
			base.sprite.SetAtlasReplace("WallnutZ_skin1_1.png", "uid://ccqw6dynty45q");
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
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter("ZombieNormalWallnut", logicalGlobalPosition, base.gridPos, base.groundHeight);
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
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieNormalWallnut", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, !instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: false, base.groundHeight);
			}
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 25.0);
		}
	}

	public override void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuar");
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter zombie = packetConfig.Create(logicalGlobalPosition, base.gridPos, base.groundHeight);
		TowerDefenseGroundItemBase.characterNode.AddChild(zombie, forceReadableName: false, InternalMode.Disabled);
		if (!instance.hypnoses)
		{
			zombie.Hypnoses();
		}
		zombie.SetMainStateMachineDispatchEnabled(enabled: false);
		Tween tween = zombie.CreateTween();
		tween.SetEase(Tween.EaseType.Out);
		tween.SetTrans(Tween.TransitionType.Back);
		tween.TweenProperty(zombie.transformPoint, "scale", Vector2.One, 0.5).From(Vector2.One * 0.5f);
		tween.Finished += delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				((TowerDefenseZombie)zombie).Walk();
				zombie.SetMainStateMachineDispatchEnabled(enabled: true);
			}
		};
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuar", base.gridPos.X, base.gridPos.Y, nextSyncId, 1.0, 1.0, !instance.hypnoses, 0.0, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y, walkAfterSpawn: true, base.groundHeight);
			}
		}
		Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["over"] = over };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		over = data.ContainsKey("over") && data["over"].AsBool();
	}
}
