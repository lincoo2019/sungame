using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class TowerDefenseBattleNetworkContext : IBattleNetworkContext, IPlantGridNetworkContext, ICursorNetworkContext, ICharacterStateNetworkContext, IGameStateNetworkContext, IZombieStateNetworkContext, IPacketNetworkContext, IBattleEventNetworkContext, IBattleCommandNetworkContext, IBattleSessionNetworkContext, IBattleSnapshotNetworkContext
{
	private readonly TowerDefenseControlNew _control;

	private readonly TowerDefenseBattleCommandExecutor _commandExecutor;

	public bool IsMultiplayerActive
	{
		get
		{
			if (Global.IsMultiplayerMode && MultiPlayerManager.Instance != null)
			{
				return MultiPlayerManager.Instance.IsConnect();
			}
			return false;
		}
	}

	public bool IsHost
	{
		get
		{
			if (IsMultiplayerActive)
			{
				return MultiPlayerManager.Instance.isHost;
			}
			return false;
		}
	}

	public double GameTime
	{
		get
		{
			if (TowerDefenseManager.Instance == null)
			{
				return 0.0;
			}
			return TowerDefenseManager.Instance.runGameTime;
		}
	}

	public string LocalPeerId => MultiPlayerManager.Instance?.peerId ?? "";

	public IDictionary<StringName, TowerDefenseBattleFeature> Features
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<StringName, TowerDefenseBattleFeature>();
			}
			return _control.featureDictionary;
		}
	}

	public TowerDefenseBattleProcess Process
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return null;
			}
			return _control.process;
		}
	}

	public Dictionary GameStateLastSync
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new Dictionary();
			}
			return _control.GameStateLastSync;
		}
	}

	public IDictionary<int, TowerDefenseCharacter> SyncCharacters
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, TowerDefenseCharacter>();
			}
			return _control._syncCharacters;
		}
	}

	public Dictionary PendingDestroySyncIds
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new Dictionary();
			}
			return _control.PendingDestroySyncIds;
		}
	}

	public Dictionary CharacterLastSyncState
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new Dictionary();
			}
			return _control.CharacterLastSyncState;
		}
	}

	public Dictionary ComponentLastSyncState
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new Dictionary();
			}
			return _control.ComponentLastSyncState;
		}
	}

	public Dictionary ZombieLastSyncState
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new Dictionary();
			}
			return _control.ZombieLastSyncState;
		}
	}

	public IDictionary<int, int> ZombieSyncMissCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, int>();
			}
			return _control.ZombieSyncMissCount;
		}
	}

	public IDictionary<int, Vector2> ZombieTargetPositions
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, Vector2>();
			}
			return _control.ZombieTargetPositions;
		}
	}

	public IDictionary<int, Vector2> ZombieSyncVelocities
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, Vector2>();
			}
			return _control.ZombieSyncVelocities;
		}
	}

	public IDictionary<int, double> ZombieLastSyncTime
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, double>();
			}
			return _control.ZombieLastSyncTime;
		}
	}

	public IDictionary<int, TowerDefenseInGamePacketShow> SyncPackets
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_control))
			{
				return new System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow>();
			}
			return _control.SyncPackets;
		}
	}

	public bool IsNetworkPaused
	{
		get
		{
			if (GodotObject.IsInstanceValid(_control))
			{
				return _control.IsNetworkPaused;
			}
			return false;
		}
	}

	public TowerDefenseBattleNetworkContext(TowerDefenseControlNew control)
	{
		_control = control;
		_commandExecutor = new TowerDefenseBattleCommandExecutor(control);
	}

	public void SendBattleSnapshotMessage(string opCode, string data, string targetPeerId)
	{
		MultiPlayerManager.Instance?.SendMatchStateToPeer(opCode, data, targetPeerId);
	}

	public void RequestBattleSnapshot(string phase)
	{
		if (MultiPlayerManager.Instance != null && !MultiPlayerManager.Instance.isHost)
		{
			MultiPlayerManager.Instance.SendMatchState("battle_snapshot_request", Json.Stringify(new Dictionary { ["phase"] = phase }));
		}
	}

	public async void RequestStateSnapshotAfterEntitiesReady()
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			await _control.ToSignal(_control.GetTree(), SceneTree.SignalName.ProcessFrame);
			await _control.ToSignal(_control.GetTree(), SceneTree.SignalName.ProcessFrame);
			if (GodotObject.IsInstanceValid(_control))
			{
				RequestBattleSnapshot("state");
			}
		}
	}

	public void ApplyPause(bool paused)
	{
		_control?.ApplyNetworkPauseFromSession(paused);
	}

	public void ApplyChooseReady(Dictionary data)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.OnChooseReady(data);
		}
	}

	public void ApplyChooseOver()
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.OnChooseOver();
		}
	}

	public void ApplyGameEntry(int roundNum)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			if (_control.GetFeature("Wave") is TowerDefenseBattleFeatureWave { isSurvival: not false } towerDefenseBattleFeatureWave && GodotObject.IsInstanceValid(towerDefenseBattleFeatureWave.survivalRunner))
			{
				towerDefenseBattleFeatureWave.survivalRunner.roundNum = roundNum;
			}
			_control.GameEntry();
		}
	}

	public void ApplyTips(string text, double duration)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.levelControl?.TipsPlay(text, duration);
		}
	}

	public void ApplyGameResult(bool victory, bool leave)
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return;
		}
		if (leave)
		{
			SendClientReady();
			SceneManager.Instance.ChangeScene("MainMenu");
			return;
		}
		if (victory)
		{
			_control.levelControl?.AwardCreate(_control.levelControl.GlobalPosition);
			return;
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter)
			{
				towerDefenseCharacter.ProcessMode = Node.ProcessModeEnum.Disabled;
			}
		}
		_control.isGameFail = true;
		_control.isGameRunning = false;
		_control.ZombieWonLevelFail(playAnime: false);
	}

	public void SendGameEntryAck()
	{
		MultiPlayerManager.Instance?.SendGameEntryAck();
	}

	public void SendClientReady()
	{
		MultiPlayerManager.Instance?.SendClientReady();
	}

	public Array GetPlants()
	{
		if (TowerDefenseManager.Instance == null)
		{
			return new Array();
		}
		return TowerDefenseManager.Instance.GetPlant();
	}

	public Array GetGravestones()
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return new Array();
		}
		Array array = new Array();
		foreach (Node item in _control.GetTree().GetNodesInGroup("Gravestone"))
		{
			array.Add(item);
		}
		return array;
	}

	public void SendPlantGridSnapshot(string snapshotJson)
	{
		MultiPlayerManager.Instance?.SendPlantFullSync(snapshotJson);
	}

	public void SendCharacterState(string charactersJson)
	{
		MultiPlayerManager.Instance?.SendCharacterStateSync(charactersJson);
	}

	public void SendCharacterDestroy(int syncId, bool isExplode, bool isSmash)
	{
		MultiPlayerManager.Instance?.SendCharacterDestroy(syncId, isExplode, isSmash);
	}

	public void SendCharacterInit(int syncId, double x, double y, double hp, bool die, string clipName, bool loop, double blendTime, int frame, double timeScale, double walkSpeedScale)
	{
		MultiPlayerManager.Instance?.SendCharacterInit(syncId, x, y, hp, die, clipName, loop, blendTime, frame, timeScale, walkSpeedScale);
	}

	public void SendCharacterPosition(int syncId, double x, double y)
	{
		MultiPlayerManager.Instance?.SendCharacterPositionSync(syncId, x, y);
	}

	public void SendGameState(string stateJson)
	{
		MultiPlayerManager.Instance?.SendMatchState("game_state_sync", stateJson);
	}

	public void SendZombieState(string zombiesJson)
	{
		MultiPlayerManager.Instance?.SendZombieFullSync(zombiesJson);
	}

	public void EnsureFeature(StringName featureName)
	{
		if (GodotObject.IsInstanceValid(_control) && !_control.featureDictionary.ContainsKey(featureName))
		{
			_control.AddFeature(featureName, _control.GetConfiguredFeatureData(featureName));
		}
	}

	public void RemoveCharacterSyncState(int syncId)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.RemoveCharacterSyncState(syncId);
		}
	}

	public void CleanupCharacterCell(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.CleanupCharacterCell(character);
		}
	}

	public void RemoveZombieSyncState(int syncId)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.RemoveZombieSyncState(syncId);
		}
	}

	public void RegisterCharacter(int syncId, TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.RegisterSyncCharacter(syncId, character);
		}
	}

	public void CreateMissingZombieFromRoster(int syncId, Dictionary info)
	{
		if (!GodotObject.IsInstanceValid(_control) || info == null)
		{
			return;
		}
		string text = info.GetValueOrDefault("n", "").AsString();
		if (text == "")
		{
			return;
		}
		int y = info.GetValueOrDefault("g", 1).AsInt32();
		float x = (float)info.GetValueOrDefault("x", 0.0).AsDouble();
		float y2 = (float)info.GetValueOrDefault("y", 0.0).AsDouble();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(new Vector2(x, y2), new Vector2I(0, y));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				_control.characterNode.CallDeferred("add_child", towerDefenseCharacter);
				_control.RegisterSyncCharacter(syncId, towerDefenseCharacter);
			}
		}
	}

	public void DestroyRemoteZombie(int syncId, TowerDefenseCharacter zombie)
	{
		if (GodotObject.IsInstanceValid(_control) && GodotObject.IsInstanceValid(zombie))
		{
			_control.RemoveZombieSyncState(syncId);
			DestroyComponent destroyComponent = zombie.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				zombie.destroyComponent.isRemoteDestroy = true;
			}
			zombie.Destroy();
		}
	}

	public TowerDefenseInGamePacketShow CreatePacket(PacketSpawnState state)
	{
		if (!GodotObject.IsInstanceValid(_control) || state.PacketName == "")
		{
			return null;
		}
		if (!TowerDefensePacketRuntimeState.TryCreate(state.PacketName, state.RuntimeState, "override", "can_change_cost", "change_cost_list", out var packetConfig) || !GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
		EconomyAccountId accountId = (state.SunAccountId.IsValid ? state.SunAccountId : EconomyAccountId.Local);
		if (!towerDefenseInGamePacketShow.TryBindSunAccount(accountId))
		{
			towerDefenseInGamePacketShow.QueueFree();
			return null;
		}
		towerDefenseInGamePacketShow.ZIndex = state.ZIndex;
		towerDefenseInGamePacketShow.GlobalPosition = state.Position;
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, Node.InternalMode.Disabled);
		towerDefenseInGamePacketShow.Init(packetConfig);
		towerDefenseInGamePacketShow.onlyDraw = false;
		towerDefenseInGamePacketShow.showCost = state.UseCost;
		towerDefenseInGamePacketShow.useCost = state.UseCost;
		towerDefenseInGamePacketShow.plantOnce = true;
		towerDefenseInGamePacketShow.canPressPutBack = false;
		towerDefenseInGamePacketShow.StartInit();
		towerDefenseInGamePacketShow.alive = true;
		towerDefenseInGamePacketShow.aliveTime = state.AliveTime;
		if (state.UseCost)
		{
			towerDefenseInGamePacketShow.start = true;
		}
		if (state.IsFall)
		{
			towerDefenseInGamePacketShow.CreateTween().TweenProperty(towerDefenseInGamePacketShow, "global_position:y", state.FallHeight, (state.FallHeight - (double)state.Position.Y) / 25.0);
		}
		else
		{
			towerDefenseInGamePacketShow.height = state.Height;
			towerDefenseInGamePacketShow.moveComponent.gravity = state.Gravity;
			towerDefenseInGamePacketShow.moveComponent.velocity = state.Velocity;
		}
		PacketPickControl packetPickControl = TowerDefenseManager.Instance.GetPacketPickControl();
		if (GodotObject.IsInstanceValid(packetPickControl))
		{
			towerDefenseInGamePacketShow.OnPressed += packetPickControl.PickPacket;
		}
		if (state.SyncId >= 0)
		{
			towerDefenseInGamePacketShow.SetMeta("packet_sync_id", state.SyncId);
		}
		return towerDefenseInGamePacketShow;
	}

	public void RegisterPacket(int syncId, TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.RegisterSyncPacket(syncId, packet);
		}
	}

	public bool TryGetPacket(int syncId, out TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			return _control.TryGetSyncPacket(syncId, out packet);
		}
		packet = null;
		return false;
	}

	public void UnregisterPacket(int syncId)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.UnregisterSyncPacket(syncId);
		}
	}

	public void LockPacket(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.alive = false;
			if (GodotObject.IsInstanceValid(packet.button))
			{
				packet.button.MouseFilter = Control.MouseFilterEnum.Ignore;
			}
		}
	}

	public void UnlockPacket(TowerDefenseInGamePacketShow packet)
	{
		if (GodotObject.IsInstanceValid(packet))
		{
			packet.alive = true;
			if (GodotObject.IsInstanceValid(packet.button))
			{
				packet.button.MouseFilter = Control.MouseFilterEnum.Pass;
			}
		}
	}

	public void RemovePacket(int syncId, TowerDefenseInGamePacketShow packet)
	{
		if (!GodotObject.IsInstanceValid(packet))
		{
			UnregisterPacket(syncId);
			return;
		}
		PacketPickControl packetPickControl = TowerDefenseManager.Instance.GetPacketPickControl();
		if (GodotObject.IsInstanceValid(packetPickControl) && packetPickControl.packetPick != null && GodotObject.IsInstanceValid(packetPickControl.packetPick) && packetPickControl.packetPick == packet)
		{
			packetPickControl.PacketPickRelease();
		}
		UnregisterPacket(syncId);
		packet.QueueFree();
	}

	public TowerDefenseCharacter PlantAt(string plantName, Vector2I gridPos, int syncId)
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _commandExecutor.PlantAt(plantName, gridPos, syncId);
	}

	public TowerDefenseCharacter PlantAt(string plantName, Vector2I gridPos, int syncId, string overrideData = "")
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _commandExecutor.PlantAt(plantName, gridPos, syncId, overrideData);
	}

	public TowerDefenseCharacter PlantAt(EconomyAccountId economyOwnerAccountId, string plantName, Vector2I gridPos, int syncId, string overrideData = "")
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _commandExecutor.PlantAt(economyOwnerAccountId, plantName, gridPos, syncId, overrideData);
	}

	public TowerDefenseCharacter PlantOnCharacter(string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId, string overrideData = "")
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _commandExecutor.PlantOnCharacter(plantName, targetSyncId, placementKind, hypnoses, syncId, overrideData);
	}

	public TowerDefenseCharacter PlantOnCharacter(EconomyAccountId economyOwnerAccountId, string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId, string overrideData = "")
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _commandExecutor.PlantOnCharacter(economyOwnerAccountId, plantName, targetSyncId, placementKind, hypnoses, syncId, overrideData);
	}

	public int NextCharacterSyncId()
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return -1;
		}
		return _control.GetNextSyncId();
	}

	public bool RemovePlantAt(Vector2I gridPos)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			return _commandExecutor.RemovePlantAt(gridPos);
		}
		return false;
	}

	public bool ShovelPlantAt(Vector2I gridPos)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			return _commandExecutor.ShovelPlantAt(gridPos);
		}
		return false;
	}

	public bool MoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			return _commandExecutor.MoveCharacter(syncId, fromGridPos, toGridPos, moveKind, duration);
		}
		return false;
	}

	public void SpawnZombie(string zombieName, int line, float offsetX, int syncId, string spawnOverride, string spawnConfigOverride)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_commandExecutor.SpawnZombie(zombieName, line, offsetX, syncId, spawnOverride, spawnConfigOverride);
		}
	}

	public void BreakVaseRequest(Vector2I gridPos)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_commandExecutor.BreakVaseRequest(gridPos);
		}
	}

	public void SendPlacePlant(string plantName, int gridX, int gridY, int syncId, string overrideData, string requestId = "", string ownerPeerId = "", string placementKind = "grid", int targetSyncId = -1, bool hypnoses = false)
	{
		MultiPlayerManager.Instance?.SendPlacePlant(plantName, gridX, gridY, syncId, overrideData, requestId, ownerPeerId, placementKind, targetSyncId, hypnoses);
	}

	public void SendCommandRejected(string targetPeerId, string rejectedOpCode, string requestId, string reason)
	{
		MultiPlayerManager.Instance?.SendCommandRejected(targetPeerId, rejectedOpCode, requestId, reason);
	}

	public void SendRemovePlant(int gridX, int gridY)
	{
		MultiPlayerManager.Instance?.SendRemovePlant(gridX, gridY);
	}

	public void SendUseShovel(int gridX, int gridY)
	{
		MultiPlayerManager.Instance?.SendUseShovel(gridX, gridY);
	}

	public void SendMoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5)
	{
		MultiPlayerManager.Instance?.SendMoveCharacter(syncId, fromGridPos, toGridPos, moveKind, duration);
	}

	public Array<Node> GetNodesInGroup(string groupName)
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return new Array<Node>();
		}
		return _control.GetTree().GetNodesInGroup(groupName);
	}

	public TowerDefenseBattleFeature GetFeature(StringName featureName)
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return null;
		}
		return _control.GetFeature(featureName);
	}

	public bool ExecuteGemMatchCommand(Dictionary command, EconomyAccountId accountId)
	{
		TowerDefenseBattleFeatureGemMatch towerDefenseBattleFeatureGemMatch = GetFeature("GemMatch") as TowerDefenseBattleFeatureGemMatch;
		if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureGemMatch))
		{
			return towerDefenseBattleFeatureGemMatch.ExecuteNetworkCommand(command, accountId);
		}
		return false;
	}

	public void DestroyRemoteCharacter(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(_control) && GodotObject.IsInstanceValid(character) && !character.isDestroy)
		{
			_control.CleanupCharacterCell(character);
			DestroyComponent destroyComponent = character.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				character.destroyComponent.isRemoteDestroy = true;
			}
			character.Destroy();
		}
	}

	public Vector2 GetCursorPosition()
	{
		if (!GodotObject.IsInstanceValid(_control))
		{
			return Vector2.Zero;
		}
		return _control.GetViewport().GetMousePosition();
	}

	public CursorPickState GetCursorPickState()
	{
		string pickType = "";
		string pickName = "";
		PacketPickControl packetPickControl = TowerDefenseManager.Instance?.GetPacketPickControl();
		if (GodotObject.IsInstanceValid(packetPickControl))
		{
			if (packetPickControl.packetPick != null && GodotObject.IsInstanceValid(packetPickControl.packetPick) && packetPickControl.packetPick.select)
			{
				pickType = "plant";
				pickName = packetPickControl.packetPick.config.saveKey;
			}
			else if (packetPickControl.tools != null && packetPickControl.tools.Count > 0)
			{
				foreach (PacketPickTool tool in packetPickControl.tools)
				{
					if (tool is ShovelPickTool shovelPickTool && shovelPickTool.IsPicking())
					{
						pickType = "shovel";
						string text = GameSaveManager.Instance.GetKeyValue("CurrentShovel").AsString();
						pickName = ((text != "") ? text : "ShovelDefault");
						break;
					}
					if (tool is GlovePickTool glovePickTool && glovePickTool.IsPicking())
					{
						pickType = "glove";
						pickName = "Glove";
						break;
					}
				}
			}
		}
		return new CursorPickState(pickType, pickName);
	}

	public void SendCursorPosition(Vector2 position)
	{
		MultiPlayerManager.Instance?.SendCursorSync(position.X, position.Y);
	}

	public void SendCursorPick(CursorPickState pickState)
	{
		MultiPlayerManager.Instance?.SendCursorPickSync(pickState.PickType, pickState.PickName);
	}

	public void ApplyRemoteCursorPosition(string userId, Vector2 position)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.ApplyRemoteCursorPosition(userId, position);
		}
	}

	public void ApplyRemoteCursorPick(string userId, CursorPickState pickState)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.ApplyRemoteCursorPick(userId, pickState.PickType, pickState.PickName);
		}
	}

	public void RemoveRemoteCursor(string userId)
	{
		if (GodotObject.IsInstanceValid(_control))
		{
			_control.RemoveRemoteCursor(userId);
		}
	}
}
