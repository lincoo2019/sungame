using System.Text;

public static class NetProtocol
{
	public const int Version = 4;

	public const string EnvelopeOpCode = "__net_v2";

	public const string PayloadFormatJson = "json";

	public const string PayloadFormatBinaryBase64 = "binary/base64";

	public const int MaxPayloadUtf8Bytes = 3145728;

	public const int MaxEnvelopeUtf8Bytes = 4194304;

	public static bool IsWithinUtf8Budget(string value, int maxBytes)
	{
		if (value != null && value.Length <= maxBytes)
		{
			return Encoding.UTF8.GetByteCount(value) <= maxBytes;
		}
		return false;
	}

	public static NetMessageType FromLegacyOpCode(string opCode)
	{
		return opCode switch
		{
			"start_game" => NetMessageType.StartGame, 
			"place_plant" => NetMessageType.PlacePlantRequest, 
			"command_rejected" => NetMessageType.CommandRejected, 
			"remove_plant" => NetMessageType.RemovePlantRequest, 
			"use_shovel" => NetMessageType.UseShovel, 
			"move_character" => NetMessageType.MoveCharacter, 
			"gem_match_command" => NetMessageType.GemMatchCommand, 
			"game_state_sync" => NetMessageType.GameStateSync, 
			"game_result" => NetMessageType.GameResult, 
			"spawn_zombie" => NetMessageType.SpawnZombie, 
			"spawn_grid" => NetMessageType.SpawnGrid, 
			"character_destroy" => NetMessageType.CharacterDestroy, 
			"character_init" => NetMessageType.CharacterInit, 
			"character_state_sync" => NetMessageType.CharacterStateDelta, 
			"character_position_sync" => NetMessageType.CharacterPositionSync, 
			"pause" => NetMessageType.Pause, 
			"resume" => NetMessageType.Resume, 
			"cursor_sync" => NetMessageType.CursorState, 
			"cursor_pick_sync" => NetMessageType.CursorPickState, 
			"select_level" => NetMessageType.SelectLevel, 
			"level_config" => NetMessageType.LevelConfig, 
			"choose_ready" => NetMessageType.ChooseReady, 
			"choose_over" => NetMessageType.ChooseOver, 
			"vase_break_request" => NetMessageType.VaseBreakRequest, 
			"vase_break" => NetMessageType.VaseBreak, 
			"packet_spawn" => NetMessageType.PacketSpawn, 
			"packet_pick" => NetMessageType.PacketPickRequest, 
			"spawn_coin" => NetMessageType.SpawnCoin, 
			"spawn_falling_object" => NetMessageType.SpawnFallingObject, 
			"zombie_full_sync" => NetMessageType.ZombieStateDelta, 
			"spawn_character_at" => NetMessageType.SpawnCharacterAt, 
			"conveyor_spawn" => NetMessageType.ConveyorSpawn, 
			"client_ready" => NetMessageType.ClientReady, 
			"game_entry" => NetMessageType.GameEntry, 
			"game_entry_ack" => NetMessageType.GameEntryAck, 
			"tips_play" => NetMessageType.TipsPlay, 
			"level_config_ack" => NetMessageType.LevelConfigAck, 
			"damage_part" => NetMessageType.DamagePart, 
			"damage_point_reach" => NetMessageType.DamagePointReach, 
			"armor_damage_point_reach" => NetMessageType.ArmorDamagePointReach, 
			"armor_hitpoints_empty" => NetMessageType.ArmorHitpointsEmpty, 
			"crater_create" => NetMessageType.CraterCreate, 
			"plant_full_sync" => NetMessageType.PlantGridSnapshot, 
			"event_execute" => NetMessageType.EventExecute, 
			"wave_event_execute" => NetMessageType.WaveEventExecute, 
			"battle_snapshot_request" => NetMessageType.BattleSnapshotRequest, 
			"battle_character_roster" => NetMessageType.BattleCharacterRoster, 
			"battle_packet_snapshot" => NetMessageType.BattlePacketSnapshot, 
			"battle_snapshot_entities_ready" => NetMessageType.BattleSnapshotEntitiesReady, 
			"battle_snapshot_complete" => NetMessageType.BattleSnapshotComplete, 
			"projectile_effect_spawn" => NetMessageType.ProjectileEffectSpawn, 
			"character_component_operation" => NetMessageType.CharacterComponentOperation, 
			_ => NetMessageType.Unknown, 
		};
	}

	public static string ToLegacyOpCode(NetMessageType messageType)
	{
		return messageType switch
		{
			NetMessageType.StartGame => "start_game", 
			NetMessageType.PlacePlantRequest => "place_plant", 
			NetMessageType.PlantPlaced => "place_plant", 
			NetMessageType.CommandRejected => "command_rejected", 
			NetMessageType.RemovePlantRequest => "remove_plant", 
			NetMessageType.PlantRemoved => "remove_plant", 
			NetMessageType.UseShovel => "use_shovel", 
			NetMessageType.MoveCharacter => "move_character", 
			NetMessageType.GemMatchCommand => "gem_match_command", 
			NetMessageType.GameStateSync => "game_state_sync", 
			NetMessageType.GameResult => "game_result", 
			NetMessageType.SpawnZombie => "spawn_zombie", 
			NetMessageType.SpawnGrid => "spawn_grid", 
			NetMessageType.CharacterDestroy => "character_destroy", 
			NetMessageType.CharacterInit => "character_init", 
			NetMessageType.CharacterStateDelta => "character_state_sync", 
			NetMessageType.CharacterPositionSync => "character_position_sync", 
			NetMessageType.Pause => "pause", 
			NetMessageType.Resume => "resume", 
			NetMessageType.CursorState => "cursor_sync", 
			NetMessageType.CursorPickState => "cursor_pick_sync", 
			NetMessageType.SelectLevel => "select_level", 
			NetMessageType.LevelConfig => "level_config", 
			NetMessageType.ChooseReady => "choose_ready", 
			NetMessageType.ChooseOver => "choose_over", 
			NetMessageType.VaseBreakRequest => "vase_break_request", 
			NetMessageType.VaseBreak => "vase_break", 
			NetMessageType.PacketSpawn => "packet_spawn", 
			NetMessageType.PacketPickRequest => "packet_pick", 
			NetMessageType.PacketPick => "packet_pick", 
			NetMessageType.SpawnCoin => "spawn_coin", 
			NetMessageType.SpawnFallingObject => "spawn_falling_object", 
			NetMessageType.ZombieStateDelta => "zombie_full_sync", 
			NetMessageType.SpawnCharacterAt => "spawn_character_at", 
			NetMessageType.ConveyorSpawn => "conveyor_spawn", 
			NetMessageType.ClientReady => "client_ready", 
			NetMessageType.GameEntry => "game_entry", 
			NetMessageType.GameEntryAck => "game_entry_ack", 
			NetMessageType.TipsPlay => "tips_play", 
			NetMessageType.LevelConfigAck => "level_config_ack", 
			NetMessageType.DamagePart => "damage_part", 
			NetMessageType.DamagePointReach => "damage_point_reach", 
			NetMessageType.ArmorDamagePointReach => "armor_damage_point_reach", 
			NetMessageType.ArmorHitpointsEmpty => "armor_hitpoints_empty", 
			NetMessageType.CraterCreate => "crater_create", 
			NetMessageType.PlantGridSnapshot => "plant_full_sync", 
			NetMessageType.EventExecute => "event_execute", 
			NetMessageType.WaveEventExecute => "wave_event_execute", 
			NetMessageType.BattleSnapshotRequest => "battle_snapshot_request", 
			NetMessageType.BattleCharacterRoster => "battle_character_roster", 
			NetMessageType.BattlePacketSnapshot => "battle_packet_snapshot", 
			NetMessageType.BattleSnapshotEntitiesReady => "battle_snapshot_entities_ready", 
			NetMessageType.BattleSnapshotComplete => "battle_snapshot_complete", 
			NetMessageType.ProjectileEffectSpawn => "projectile_effect_spawn", 
			NetMessageType.CharacterComponentOperation => "character_component_operation", 
			_ => "", 
		};
	}

	public static bool IsStateMessage(NetMessageType messageType)
	{
		if (messageType != NetMessageType.ZombieStateDelta && messageType != NetMessageType.CharacterStateDelta && messageType != NetMessageType.CharacterPositionSync)
		{
			return messageType == NetMessageType.CursorState;
		}
		return true;
	}
}
