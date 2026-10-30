using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieCaptain : TowerDefenseZombie
{
	private static PackedScene _IMITATER_CLOUD;

	[Export(PropertyHint.None, "")]
	public double spawnTime = 15.0;

	public double spawnTimer;

	public bool over;

	private StateHandle _spawnStateHandle;

	private bool _roleStateSignalsConnected;

	private static PackedScene IMITATER_CLOUD => _IMITATER_CLOUD ?? (_IMITATER_CLOUD = GD.Load<PackedScene>("uid://djvfnrjg7vtqn"));

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_spawnStateHandle = base.StateMachine?.GetStateById("zombie.captain.spawn");
			StateHandle spawnStateHandle = _spawnStateHandle;
			if (spawnStateHandle != null && spawnStateHandle.IsValid)
			{
				_spawnStateHandle.Entered += SpawnEntered;
				_spawnStateHandle.Exited += SpawnExited;
				_spawnStateHandle.PhysicsProcessing += SpawnProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_spawnStateHandle != null)
			{
				_spawnStateHandle.Entered -= SpawnEntered;
				_spawnStateHandle.Exited -= SpawnExited;
				_spawnStateHandle.PhysicsProcessing -= SpawnProcessing;
			}
			_spawnStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConnectRoleStateSignals();
		}
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !die && !nearDie && base.IsInsideComponentBattlefield && !base.sprite.pause)
		{
			if (spawnTimer < spawnTime)
			{
				spawnTimer += delta * timeScale;
				return;
			}
			SendStateEvent("ToSpawn");
			spawnTimer = 0.0;
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 4.0;
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if ((double)GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X > groundRight)
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale;
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void SpawnEntered()
	{
		SpawnCrew();
		base.sprite.SetAnimation("Spawn", loop: true, 0.2);
	}

	public virtual void SpawnProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void SpawnExited()
	{
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Spawn")
		{
			Walk();
		}
	}

	public override void HitpointsNearDie()
	{
		base.HitpointsNearDie();
		DestroySet();
	}

	public void SpawnCrew()
	{
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		List<int> list = new List<int>();
		for (int i = base.gridPos.Y - 1; i <= base.gridPos.Y + 1; i++)
		{
			if (i >= 1 && i <= TowerDefenseManager.Instance.GetMapGridNum().Y)
			{
				list.Add(i);
			}
		}
		if (list.Count > 0)
		{
			int x = TowerDefenseManager.Instance.GetMapGridNum().X;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			for (int j = 0; j < 4; j++)
			{
				double num = (double)logicalGlobalPosition.X + GD.RandRange(-1.5, 1.5) * (double)TowerDefenseManager.Instance.GetMapGridSize().X;
				int x2 = Mathf.Clamp(TowerDefenseManager.Instance.GetMapGridPos(new Vector2((float)num, 0f)).X, 1, x);
				TowerDefenseCharacterOverride towerDefenseCharacterOverride = new TowerDefenseCharacterOverride();
				towerDefenseCharacterOverride.hitpointScale = instance.hitpointScale;
				towerDefenseCharacterOverride.scale = transformPoint.Scale.X;
				TowerDefenseManager.Instance.BungiSpawn("ZombieCrew", new Vector2I(x2, list[GD.RandRange(0, list.Count - 1)]), towerDefenseCharacterOverride, instance.hypnoses);
			}
		}
	}

	public override void DestroySet()
	{
		bool flag = GodotObject.IsInstanceValid(instance) && instance.hypnoses;
		base.DestroySet();
		if (instance.hitpoints > 0.0 || over)
		{
			return;
		}
		over = true;
		if (TowerDefenseInGameLevelControl.instance.awardCreate || GetTree().GetNodeCountInGroup("ZombieCrew") <= 0)
		{
			return;
		}
		Array<Node> nodesInGroup = GetTree().GetNodesInGroup("ZombieCrew");
		TowerDefenseCharacter towerDefenseCharacter = null;
		Vector2 vector = default(Vector2);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		float num = 1f / 0f;
		foreach (Node item in nodesInGroup)
		{
			if (!(item is TowerDefenseCharacter { isDestroy: false } towerDefenseCharacter2) || towerDefenseCharacter2.GetParent() is TowerDefenseZombieBungiSpawn || flag != towerDefenseCharacter2.instance.hypnoses)
			{
				continue;
			}
			Vector2 logicalGlobalPosition2 = towerDefenseCharacter2.GetLogicalGlobalPosition();
			if (!((double)logicalGlobalPosition2.X > groundRight))
			{
				float num2 = logicalGlobalPosition.DistanceSquaredTo(logicalGlobalPosition2);
				if (!(num2 >= num))
				{
					towerDefenseCharacter = towerDefenseCharacter2;
					vector = logicalGlobalPosition2;
					num = num2;
				}
			}
		}
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		if (towerDefenseCharacter.isDestroy)
		{
			return;
		}
		if (Global.Instance.isMultiplayerMode && !MultiPlayerManager.Instance.isHost)
		{
			towerDefenseCharacter.Destroy();
			return;
		}
		double _hitpointScale = instance.hitpointScale;
		Vector2 _scale = transformPoint.Scale;
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(IMITATER_CLOUD, towerDefenseCharacter.gridPos);
		towerDefenseEffectParticlesOnce.GlobalPosition = vector;
		TowerDefenseGroundItemBase.characterNode.CallDeferred("add_child", towerDefenseEffectParticlesOnce);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieCaptain");
		TowerDefenseCharacter zombie = packetConfig.Create(vector, towerDefenseCharacter.gridPos);
		TowerDefenseGroundItemBase.characterNode.CallDeferred("add_child", zombie);
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
		if (flag)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(zombie))
				{
					zombie.Hypnoses();
				}
			}).CallDeferred();
		}
		zombie.CallDeferred("WalkReady");
		if (Global.Instance.isMultiplayerMode && MultiPlayerManager.Instance.isHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.Instance.currentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, zombie);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieCaptain", towerDefenseCharacter.gridPos.X, towerDefenseCharacter.gridPos.Y, nextSyncId, _hitpointScale, _scale.X, flag, 0.0, useCreate: true, vector.X, vector.Y, walkAfterSpawn: true);
			}
		}
		towerDefenseCharacter.Destroy();
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "spawnTimer", spawnTimer },
			{ "over", over }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		spawnTimer = data.GetValueOrDefault("spawnTimer", 0.0).AsDouble();
		over = data.GetValueOrDefault("over", false).AsBool();
	}
}
