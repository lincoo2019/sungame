using Godot;

[Tool]
public partial class TowerDefenseZombieGargantuarWallnutZ : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_DUCKXING = "uid://6dy81rx4gaue";

	private const string ZOMBIE_GARGANTUAR_ZOMBIE = "uid://dtrl03qm2d0u7";

	private static PackedScene _IMITATER_CLOUD;

	private CharacterTimerComponent _timerComponent;

	public bool over;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			double num = GD.Randf();
			if (num < 0.3)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://6dy81rx4gaue");
			}
			else if (num < 0.6)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://dtrl03qm2d0u7");
			}
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 10.0);
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

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == impFireEvent)
		{
			((ZombieGargantuarWallnutZSprite)base.sprite).peashooterZImpHead.Visible = false;
		}
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}

	public override void HitpointsEmpty()
	{
		base.HitpointsEmpty();
		Destroy();
	}

	public override async void DestroySet()
	{
		if (over)
		{
			return;
		}
		over = true;
		HitBoxDestroy();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, base.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = logicalGlobalPosition;
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieGargantuar");
		TowerDefenseCharacter zombie = packetConfig.Create(logicalGlobalPosition, base.gridPos);
		TowerDefenseManager.GetCharacterNode().CallDeferred("add_child", zombie);
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		bool hypnoses = instance.hypnoses;
		Vector2 vector = logicalGlobalPosition;
		Vector2I vector2I = base.gridPos;
		bool flag = base.invisible;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				if (GodotObject.IsInstanceValid(zombie.instance))
				{
					zombie.instance.hitpointScale = _hitpointScale;
				}
				if (GodotObject.IsInstanceValid(zombie.transformPoint))
				{
					zombie.transformPoint.Scale = _scale;
				}
			}
		}).CallDeferred();
		zombie.invisible = flag;
		if (hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Hypnoses();
				}
			}).CallDeferred();
		}
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(zombie))
			{
				packetConfig.IsZombieWalk(zombie);
			}
		}).CallDeferred();
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieGargantuar", vector2I.X, vector2I.Y, nextSyncId, _hitpointScale, _scale.X, hypnoses, 0.0, useCreate: true, vector.X, vector.Y, walkAfterSpawn: true);
			}
		}
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			if (!base.sprite.pause && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
			{
				_timerComponent.Run("Spawn", 10.0);
				return;
			}
			SpawnZombie();
			_timerComponent.Run("Spawn", 10.0);
		}
	}

	public override void SpawnZombie()
	{
		base.SpawnZombie();
		if (base.sprite.pause || (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		TowerDefenseCharacter towerDefenseCharacter = TowerDefenseCharacter.CreateCharacter("ZombieNormalWallnut", logicalGlobalPosition, base.gridPos, 0.0);
		towerDefenseCharacter.Rise(2.5);
		if (instance.hypnoses)
		{
			towerDefenseCharacter.Hypnoses();
		}
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieNormalWallnut", base.gridPos.X, base.gridPos.Y, nextSyncId, instance.hitpointScale, transformPoint.Scale.X, instance.hypnoses, 2.5, useCreate: true, logicalGlobalPosition.X, logicalGlobalPosition.Y);
			}
		}
	}

	public override void Purify()
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			Destroy();
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantWallnutZ");
		if (cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(base.gridPos);
			towerDefenseCharacter.WakeUp();
			if (instance.hypnoses)
			{
				towerDefenseCharacter.Hypnoses();
			}
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("PlantWallnutZ", base.gridPos.X, base.gridPos.Y, nextSyncId);
				}
			}
		}
		Destroy();
	}
}
