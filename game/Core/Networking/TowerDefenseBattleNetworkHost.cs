using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class TowerDefenseBattleNetworkHost : IDisposable
{
	private readonly BattleNetworkSession _session;

	private readonly IBattleSnapshotNetworkContext _snapshotContext;

	private readonly HashSet<string> _snapshotEntityPhasePeers = new HashSet<string>();

	private readonly System.Collections.Generic.Dictionary<string, ulong> _lastEntitySnapshotRequestMs = new System.Collections.Generic.Dictionary<string, ulong>();

	public NetworkEntityRegistry Entities { get; }

	public GameStateReplicator GameState { get; }

	public CharacterStateReplicator CharacterState { get; }

	public PlantGridReplicator PlantGrid { get; }

	public ZombieStateReplicator ZombieState { get; }

	public PacketReplicator Packets { get; }

	public BattleEventReplicator Events { get; }

	public BattleCommandReplicator Commands { get; }

	public BattleSessionReplicator SessionEvents { get; }

	public CursorReplicator Cursor { get; }

	private TowerDefenseBattleNetworkHost(IBattleNetworkContext context)
	{
		_snapshotContext = context as IBattleSnapshotNetworkContext;
		Entities = new NetworkEntityRegistry();
		_session = new BattleNetworkSession(context, new NetworkMessageRouter(), Entities);
		GameState = ((context is IGameStateNetworkContext gameContext) ? new GameStateReplicator(gameContext) : new GameStateReplicator());
		_session.AddReplicator(GameState);
		CharacterState = ((context is ICharacterStateNetworkContext characterContext) ? new CharacterStateReplicator(characterContext) : new CharacterStateReplicator());
		_session.AddReplicator(CharacterState);
		PlantGrid = ((context is IPlantGridNetworkContext plantContext) ? new PlantGridReplicator(plantContext) : new PlantGridReplicator());
		_session.AddReplicator(PlantGrid);
		ZombieState = ((context is IZombieStateNetworkContext zombieContext) ? new ZombieStateReplicator(zombieContext) : new ZombieStateReplicator());
		_session.AddReplicator(ZombieState);
		Packets = ((context is IPacketNetworkContext packetContext) ? new PacketReplicator(packetContext) : new PacketReplicator());
		_session.AddReplicator(Packets);
		Events = ((context is IBattleEventNetworkContext eventContext) ? new BattleEventReplicator(eventContext, context as IPacketNetworkContext) : new BattleEventReplicator());
		_session.AddReplicator(Events);
		Commands = ((context is IBattleCommandNetworkContext commandContext) ? new BattleCommandReplicator(commandContext) : new BattleCommandReplicator());
		_session.AddReplicator(Commands);
		SessionEvents = ((context is IBattleSessionNetworkContext sessionContext) ? new BattleSessionReplicator(sessionContext) : new BattleSessionReplicator());
		_session.AddReplicator(SessionEvents);
		Cursor = ((context is ICursorNetworkContext cursorContext) ? new CursorReplicator(cursorContext) : new CursorReplicator());
		_session.AddReplicator(Cursor);
	}

	public static TowerDefenseBattleNetworkHost TryCreate(IBattleNetworkContext context)
	{
		if (!Global.IsMultiplayerMode || context == null)
		{
			return null;
		}
		return new TowerDefenseBattleNetworkHost(context);
	}

	public void Process(double delta)
	{
		_session?.Process(delta);
	}

	public void ApplyPause(bool paused)
	{
		if (paused)
		{
			_session?.ApplyLocalPause();
		}
		else
		{
			_session?.ApplyLocalResume();
		}
	}

	public void RequestFullSnapshot()
	{
		if (_session != null && !_session.IsHost && _snapshotContext != null)
		{
			_snapshotContext.RequestBattleSnapshot("entities");
		}
	}

	private void SendEntitySnapshot(string targetPeerId)
	{
		if (_session != null && _session.IsHost && _snapshotContext != null && !(targetPeerId == ""))
		{
			ulong ticksMsec = Time.GetTicksMsec();
			if (!_lastEntitySnapshotRequestMs.TryGetValue(targetPeerId, out var value) || ticksMsec - value >= 1000)
			{
				_lastEntitySnapshotRequestMs[targetPeerId] = ticksMsec;
				_snapshotEntityPhasePeers.Add(targetPeerId);
				_snapshotContext.SendBattleSnapshotMessage("battle_character_roster", Json.Stringify(Events.BuildCharacterRoster()), targetPeerId);
				_snapshotContext.SendBattleSnapshotMessage("plant_full_sync", Json.Stringify(PlantGrid.BuildSnapshot()), targetPeerId);
				_snapshotContext.SendBattleSnapshotMessage("zombie_full_sync", Json.Stringify(ZombieState.BuildFullState()), targetPeerId);
				_snapshotContext.SendBattleSnapshotMessage("battle_packet_snapshot", Json.Stringify(Packets.BuildSnapshot()), targetPeerId);
				_snapshotContext.SendBattleSnapshotMessage("battle_snapshot_entities_ready", "{}", targetPeerId);
			}
		}
	}

	private void SendStateSnapshot(string targetPeerId)
	{
		if (_session != null && _session.IsHost && _snapshotContext != null && !(targetPeerId == ""))
		{
			_snapshotContext.SendBattleSnapshotMessage("game_state_sync", Json.Stringify(GameState.BuildFullState()), targetPeerId);
			_snapshotContext.SendBattleSnapshotMessage("zombie_full_sync", Json.Stringify(ZombieState.BuildFullState()), targetPeerId);
			_snapshotContext.SendBattleSnapshotMessage("character_state_sync", Json.Stringify(CharacterState.BuildFullState()), targetPeerId);
			_snapshotContext.SendBattleSnapshotMessage("battle_packet_snapshot", Json.Stringify(Packets.BuildSnapshot()), targetPeerId);
			_snapshotContext.SendBattleSnapshotMessage(_snapshotContext.IsNetworkPaused ? "pause" : "resume", "{}", targetPeerId);
			_snapshotContext.SendBattleSnapshotMessage("battle_snapshot_complete", "{}", targetPeerId);
		}
	}

	public void HandleLocalVictory()
	{
		SendGameResult(victory: true);
	}

	public void HandleLocalFailed()
	{
		SendGameResult(victory: false);
	}

	public void HandlePeerLeft(string username, string peerId)
	{
		Cursor?.RemoveRemoteCursor(peerId);
		_snapshotEntityPhasePeers.Remove(peerId);
		_lastEntitySnapshotRequestMs.Remove(peerId);
	}

	private void SendGameResult(bool victory)
	{
		if (_session != null && _session.IsHost)
		{
			MultiPlayerManager.Instance?.SendGameResult(victory);
		}
	}

	public void HandleLegacyMessage(string opCode, string data, string senderId)
	{
		if (MultiPlayerManager.Instance == null || !MultiPlayerManager.Instance.IsBattleParticipant(MultiPlayerManager.Instance.peerId) || (MultiPlayerManager.Instance.isHost && senderId != MultiPlayerManager.Instance.peerId && !IsAuthenticatedClient(senderId)))
		{
			return;
		}
		switch (opCode)
		{
		case "battle_snapshot_request":
		{
			if (_session == null || !_session.IsHost || senderId == "" || senderId == MultiPlayerManager.Instance?.peerId)
			{
				break;
			}
			Variant variant2 = Json.ParseString(data);
			if (((variant2.VariantType == Variant.Type.Dictionary) ? variant2.AsGodotDictionary().GetValueOrDefault("phase", "entities").AsString() : "entities") == "state")
			{
				if (_snapshotEntityPhasePeers.Remove(senderId))
				{
					SendStateSnapshot(senderId);
				}
			}
			else
			{
				SendEntitySnapshot(senderId);
			}
			break;
		}
		case "battle_snapshot_entities_ready":
			if (IsAuthenticatedHostMessage(senderId))
			{
				_snapshotContext?.RequestStateSnapshotAfterEntitiesReady();
			}
			break;
		case "battle_snapshot_complete":
			break;
		case "battle_character_roster":
			if (IsAuthenticatedHostMessage(senderId))
			{
				Variant variant7 = Json.ParseString(data);
				if (variant7.VariantType == Variant.Type.Array)
				{
					Events?.ApplyCharacterRoster(variant7.AsGodotArray());
				}
			}
			break;
		case "battle_packet_snapshot":
			if (IsAuthenticatedHostMessage(senderId))
			{
				Variant variant5 = Json.ParseString(data);
				if (variant5.VariantType == Variant.Type.Array)
				{
					Packets?.ApplySnapshot(variant5.AsGodotArray());
				}
			}
			break;
		case "cursor_sync":
		{
			Variant variant8 = Json.ParseString(data);
			if (variant8.VariantType != Variant.Type.Nil)
			{
				Cursor?.ApplyCursorState(variant8.AsGodotDictionary());
			}
			break;
		}
		case "cursor_pick_sync":
		{
			Variant variant4 = Json.ParseString(data);
			if (variant4.VariantType != Variant.Type.Nil)
			{
				Cursor?.ApplyCursorPickState(variant4.AsGodotDictionary());
			}
			break;
		}
		case "choose_ready":
		{
			Variant variant9 = Json.ParseString(data);
			if (variant9.VariantType != Variant.Type.Nil)
			{
				SessionEvents?.ApplyChooseReady(variant9.AsGodotDictionary());
			}
			break;
		}
		case "choose_over":
			SessionEvents?.ApplyChooseOver();
			break;
		case "pause":
			SessionEvents?.ApplyPause(paused: true);
			break;
		case "resume":
			SessionEvents?.ApplyPause(paused: false);
			break;
		case "client_ready":
			if (IsAuthenticatedClient(senderId))
			{
				UserIdDto userIdDto2 = MatchStateSerializer.Deserialize<UserIdDto>(data) ?? new UserIdDto();
				userIdDto2.user_id = senderId;
				SessionEvents?.ApplyClientReady(userIdDto2);
			}
			break;
		case "game_entry":
			SessionEvents?.ApplyGameEntry(MatchStateSerializer.Deserialize<GameEntryDto>(data));
			break;
		case "game_entry_ack":
			if (IsAuthenticatedClient(senderId))
			{
				UserIdDto userIdDto = MatchStateSerializer.Deserialize<UserIdDto>(data) ?? new UserIdDto();
				userIdDto.user_id = senderId;
				SessionEvents?.ApplyGameEntryAck(userIdDto);
			}
			break;
		case "tips_play":
			SessionEvents?.ApplyTips(MatchStateSerializer.Deserialize<TipsPlayDto>(data));
			break;
		case "damage_part":
			Events?.ApplyDamagePart(MatchStateSerializer.Deserialize<DamagePartDto>(data));
			break;
		case "damage_point_reach":
			Events?.ApplyDamagePointReach(MatchStateSerializer.Deserialize<DamagePointReachDto>(data));
			break;
		case "armor_damage_point_reach":
			Events?.ApplyArmorDamagePointReach(MatchStateSerializer.Deserialize<ArmorDamagePointReachDto>(data));
			break;
		case "armor_hitpoints_empty":
			Events?.ApplyArmorHitpointsEmpty(MatchStateSerializer.Deserialize<ArmorHitpointsEmptyDto>(data));
			break;
		case "crater_create":
			Events?.ApplyCraterCreate(MatchStateSerializer.Deserialize<CraterCreateDto>(data));
			break;
		case "projectile_effect_spawn":
			if (IsAuthenticatedHostMessage(senderId))
			{
				Events?.ApplyProjectileEffectSpawn(MatchStateSerializer.Deserialize<ProjectileEffectSpawnDto>(data));
			}
			break;
		case "character_component_operation":
			if (IsAuthenticatedHostMessage(senderId))
			{
				Variant variant6 = Json.ParseString(data);
				if (variant6.VariantType == Variant.Type.Dictionary)
				{
					Events?.ApplyCharacterComponentOperation(variant6.AsGodotDictionary());
				}
			}
			break;
		case "plant_full_sync":
			if (IsAuthenticatedHostMessage(senderId))
			{
				Variant plantsDataVariant = Json.ParseString(data);
				if (plantsDataVariant.VariantType != Variant.Type.Nil)
				{
					PlantGrid?.ApplySnapshot(plantsDataVariant);
				}
			}
			break;
		case "event_execute":
		{
			Variant variant3 = Json.ParseString(data);
			if (variant3.VariantType == Variant.Type.Dictionary)
			{
				Events?.ApplyEventExecute(variant3.AsGodotDictionary());
			}
			break;
		}
		case "wave_event_execute":
		{
			Variant variant = Json.ParseString(data);
			if (variant.VariantType == Variant.Type.Dictionary)
			{
				Events?.ApplyWaveEventExecute(variant.AsGodotDictionary());
			}
			break;
		}
		default:
			if (MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost)
			{
				HandleLegacyClientAction(opCode, data, senderId);
			}
			else
			{
				HandleLegacyHostSync(opCode, data, senderId);
			}
			break;
		}
	}

	private static bool IsAuthenticatedHostMessage(string senderId)
	{
		if (MultiPlayerManager.Instance != null && !MultiPlayerManager.Instance.isHost)
		{
			return senderId == "1";
		}
		return false;
	}

	private static bool IsAuthenticatedClient(string senderId)
	{
		if (MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost && senderId != "" && senderId != MultiPlayerManager.Instance.peerId && MultiPlayerManager.Instance.matchMembers.Contains(senderId))
		{
			return MultiPlayerManager.Instance.IsBattleParticipant(senderId);
		}
		return false;
	}

	private void HandleLegacyClientAction(string opCode, string data, string senderId)
	{
		Variant variant = Json.ParseString(data);
		if (variant.VariantType == Variant.Type.Nil)
		{
			return;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		switch (opCode)
		{
		case "place_plant":
		case "remove_plant":
		case "use_shovel":
		case "move_character":
		case "gem_match_command":
		case "vase_break_request":
			Commands?.HandleClientAction(opCode, dictionary, senderId);
			break;
		case "packet_spawn":
			Packets?.ApplySpawn(dictionary);
			break;
		case "packet_pick":
		{
			int syncId = dictionary.GetValueOrDefault("sync_id", -1).AsInt32();
			string pickType = dictionary.GetValueOrDefault("pick_type", "remove").AsString();
			PacketReplicator packets = Packets;
			if (packets != null && packets.ApplyPickForPeer(syncId, pickType, senderId))
			{
				MultiPlayerManager.Instance?.SendPacketPick(syncId, pickType);
			}
			break;
		}
		case "spawn_coin":
			Events?.ApplySpawnCoin(dictionary);
			break;
		case "spawn_falling_object":
			Events?.ApplySpawnFallingObject(dictionary);
			break;
		}
	}

	private void HandleLegacyHostSync(string opCode, string data, string authenticatedSenderId)
	{
		if (authenticatedSenderId != "1")
		{
			GD.PushWarning($"Rejected host-sync opcode {opCode} from non-host peer {authenticatedSenderId}.");
			return;
		}
		Variant variant = Json.ParseString(data);
		if (variant.VariantType == Variant.Type.Nil)
		{
			return;
		}
		if (opCode == "character_state_sync")
		{
			CharacterState?.ApplyState(variant);
			return;
		}
		if (opCode == "zombie_full_sync")
		{
			ZombieState?.ApplyState(variant);
			return;
		}
		Dictionary dictionary = variant.AsGodotDictionary();
		string text = dictionary.GetValueOrDefault("user_id", "").AsString();
		if (MultiPlayerManager.Instance == null || !(text == MultiPlayerManager.Instance.peerId))
		{
			switch (opCode)
			{
			case "place_plant":
			case "remove_plant":
			case "use_shovel":
			case "move_character":
			case "spawn_zombie":
			case "spawn_grid":
				Commands?.ApplyHostSync(opCode, dictionary);
				break;
			case "spawn_character_at":
				Events?.ApplySpawnCharacterAt(dictionary);
				break;
			case "conveyor_spawn":
				Events?.ApplyConveyorSpawn(dictionary);
				break;
			case "character_destroy":
			{
				CharacterDestroyDto characterDestroyDto = MatchStateSerializer.Deserialize<CharacterDestroyDto>(data);
				CharacterState?.ApplyDestroy(characterDestroyDto?.sync_id ?? (-1), characterDestroyDto?.is_explode ?? false, characterDestroyDto?.is_smash ?? false);
				break;
			}
			case "character_init":
				CharacterState?.ApplyInit(dictionary);
				break;
			case "character_position_sync":
				CharacterState?.ApplyPosition(dictionary);
				break;
			case "game_state_sync":
				GameState?.ApplyState(dictionary);
				break;
			case "game_result":
				SessionEvents?.ApplyGameResult(MatchStateSerializer.Deserialize<GameResultDto>(data));
				break;
			case "vase_break":
				Events?.ApplyVaseBreak(dictionary);
				break;
			case "packet_spawn":
				Packets?.ApplySpawn(dictionary);
				break;
			case "packet_pick":
				Packets?.ApplyPick(dictionary.GetValueOrDefault("sync_id", -1).AsInt32(), dictionary.GetValueOrDefault("pick_type", "remove").AsString());
				break;
			case "spawn_coin":
				Events?.ApplySpawnCoin(dictionary);
				break;
			case "spawn_falling_object":
				Events?.ApplySpawnFallingObject(dictionary);
				break;
			}
		}
	}

	public void Dispose()
	{
		_snapshotEntityPhasePeers.Clear();
		_lastEntitySnapshotRequestMs.Clear();
		_session?.Dispose();
		Entities?.Clear();
	}
}
