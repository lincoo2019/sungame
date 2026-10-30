using Godot;
using Godot.Collections;

public sealed class BattleCommandReplicator : NetworkReplicatorBase
{
	private readonly IBattleCommandNetworkContext _commandContext;

	public BattleCommandReplicator()
	{
	}

	public BattleCommandReplicator(IBattleCommandNetworkContext commandContext)
	{
		_commandContext = commandContext;
	}

	public void HandleClientAction(string opCode, Dictionary data, string senderId)
	{
		if (_commandContext == null || data == null || MultiPlayerManager.Instance == null || senderId == MultiPlayerManager.Instance.peerId)
		{
			return;
		}
		switch (opCode)
		{
		case "place_plant":
		{
			string text = data.GetValueOrDefault("plant_name", "").AsString();
			string requestId = data.GetValueOrDefault("request_id", "").AsString();
			if (text == "")
			{
				_commandContext.SendCommandRejected(senderId, "place_plant", requestId, "missing_plant_name");
				break;
			}
			int num = data.GetValueOrDefault("grid_x", 0).AsInt32();
			int num2 = data.GetValueOrDefault("grid_y", 0).AsInt32();
			string text2 = data.GetValueOrDefault("placement_kind", "grid").AsString();
			int targetSyncId = data.GetValueOrDefault("target_sync_id", -1).AsInt32();
			bool hypnoses = data.GetValueOrDefault("hypnoses", false).AsBool();
			string overrideData = data.GetValueOrDefault("override_data", "").AsString();
			TowerDefenseManager instance2 = TowerDefenseManager.Instance;
			if (!GodotObject.IsInstanceValid(instance2) || !instance2.TryResolveTrustedPeerSunAccount(senderId, out var accountId2))
			{
				_commandContext.SendCommandRejected(senderId, "place_plant", requestId, "invalid_authenticated_sender");
				break;
			}
			int syncId = _commandContext.NextCharacterSyncId();
			TowerDefenseCharacter towerDefenseCharacter = ((text2 == "grid") ? _commandContext.PlantAt(accountId2, text, new Vector2I(num, num2), syncId, overrideData) : _commandContext.PlantOnCharacter(accountId2, text, targetSyncId, text2, hypnoses, syncId, overrideData));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				if (text2 == "jala_vase")
				{
					syncId = towerDefenseCharacter.syncId;
				}
				_commandContext.SendPlacePlant(text, num, num2, syncId, overrideData, requestId, senderId, text2, targetSyncId, hypnoses);
			}
			else
			{
				_commandContext.SendCommandRejected(senderId, "place_plant", requestId, "host_validation_failed");
			}
			break;
		}
		case "remove_plant":
		{
			Vector2I gridPos = ReadGridPosition(data);
			if (_commandContext.RemovePlantAt(gridPos))
			{
				_commandContext.SendRemovePlant(gridPos.X, gridPos.Y);
			}
			break;
		}
		case "use_shovel":
		{
			Vector2I gridPos2 = ReadGridPosition(data);
			if (_commandContext.ShovelPlantAt(gridPos2))
			{
				_commandContext.SendUseShovel(gridPos2.X, gridPos2.Y);
			}
			break;
		}
		case "vase_break_request":
			_commandContext.BreakVaseRequest(ReadGridPosition(data));
			break;
		case "move_character":
		{
			int syncId2 = data.GetValueOrDefault("sync_id", -1).AsInt32();
			Vector2I fromGridPos = ReadGridPosition(data, "from_grid_x", "from_grid_y");
			Vector2I toGridPos = ReadGridPosition(data, "to_grid_x", "to_grid_y");
			string text3 = data.GetValueOrDefault("move_kind", "drag").AsString();
			double moveDuration = GetMoveDuration(text3);
			if (text3 == "magnet" && senderId != "1")
			{
				_commandContext.SendCommandRejected(senderId, "move_character", "", "host_only_move_kind");
			}
			else if (_commandContext.MoveCharacter(syncId2, fromGridPos, toGridPos, text3, moveDuration))
			{
				_commandContext.SendMoveCharacter(syncId2, fromGridPos, toGridPos, text3, moveDuration);
			}
			break;
		}
		case "gem_match_command":
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (!GodotObject.IsInstanceValid(instance) || !instance.TryResolveTrustedPeerSunAccount(senderId, out var accountId))
			{
				_commandContext.SendCommandRejected(senderId, "gem_match_command", "", "invalid_authenticated_sender");
			}
			else if (!_commandContext.ExecuteGemMatchCommand(data, accountId))
			{
				_commandContext.SendCommandRejected(senderId, "gem_match_command", "", "host_validation_failed");
			}
			break;
		}
		}
	}

	public void ApplyHostSync(string opCode, Dictionary data)
	{
		if (_commandContext == null || data == null)
		{
			return;
		}
		switch (opCode)
		{
		case "place_plant":
		{
			string text3 = data.GetValueOrDefault("plant_name", "").AsString();
			if (!(text3 != ""))
			{
				break;
			}
			string text4 = data.GetValueOrDefault("owner_peer_id", "").AsString();
			string text5 = data.GetValueOrDefault("placement_kind", "grid").AsString();
			int targetSyncId = data.GetValueOrDefault("target_sync_id", -1).AsInt32();
			bool hypnoses = data.GetValueOrDefault("hypnoses", false).AsBool();
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			EconomyAccountId accountId;
			if (text5 == "jala_vase")
			{
				if (data.TryGetValue("target_state", out var value) && value.VariantType == Variant.Type.Dictionary && _commandContext.PlantOnCharacter(text3, targetSyncId, text5, hypnoses, -1) is TowerDefensePlantJalaVase towerDefensePlantJalaVase)
				{
					towerDefensePlantJalaVase.ImportVariantSave(value.AsGodotDictionary());
				}
			}
			else if (text4 != "" && GodotObject.IsInstanceValid(instance) && instance.TryResolveTrustedPeerSunAccount(text4, out accountId))
			{
				int syncId = data.GetValueOrDefault("sync_id", -1).AsInt32();
				string overrideData = data.GetValueOrDefault("override_data", "").AsString();
				if (text5 == "grid")
				{
					_commandContext.PlantAt(accountId, text3, ReadGridPosition(data), syncId, overrideData);
				}
				else
				{
					_commandContext.PlantOnCharacter(accountId, text3, targetSyncId, text5, hypnoses, syncId, overrideData);
				}
			}
			else if (text4 == "")
			{
				int syncId2 = data.GetValueOrDefault("sync_id", -1).AsInt32();
				string overrideData2 = data.GetValueOrDefault("override_data", "").AsString();
				if (text5 == "grid")
				{
					_commandContext.PlantAt(text3, ReadGridPosition(data), syncId2, overrideData2);
				}
				else
				{
					_commandContext.PlantOnCharacter(text3, targetSyncId, text5, hypnoses, syncId2, overrideData2);
				}
			}
			else
			{
				GD.PushWarning("Rejected PLACE_PLANT with invalid owner peer " + text4 + ".");
			}
			break;
		}
		case "remove_plant":
			_commandContext.RemovePlantAt(ReadGridPosition(data));
			break;
		case "use_shovel":
			_commandContext.ShovelPlantAt(ReadGridPosition(data));
			break;
		case "move_character":
			_commandContext.MoveCharacter(data.GetValueOrDefault("sync_id", -1).AsInt32(), ReadGridPosition(data, "from_grid_x", "from_grid_y"), ReadGridPosition(data, "to_grid_x", "to_grid_y"), data.GetValueOrDefault("move_kind", "drag").AsString(), GetMoveDuration(data.GetValueOrDefault("move_kind", "drag").AsString()));
			break;
		case "spawn_zombie":
		{
			string text2 = data.GetValueOrDefault("zombie_name", "").AsString();
			if (text2 != "")
			{
				_commandContext.SpawnZombie(text2, data.GetValueOrDefault("line", 1).AsInt32(), (float)data.GetValueOrDefault("offset_x", 0.0).AsDouble(), data.GetValueOrDefault("sync_id", -1).AsInt32(), data.GetValueOrDefault("spawn_override", "").AsString(), data.GetValueOrDefault("spawn_config_override", "").AsString());
			}
			break;
		}
		case "spawn_grid":
		{
			string text = data.GetValueOrDefault("packet_name", "").AsString();
			if (text != "")
			{
				_commandContext.PlantAt(text, ReadGridPosition(data), data.GetValueOrDefault("sync_id", -1).AsInt32());
			}
			break;
		}
		}
	}

	private static double GetMoveDuration(string moveKind)
	{
		if (!(moveKind == "magnet"))
		{
			return 0.5;
		}
		return 2.0;
	}

	private static Vector2I ReadGridPosition(Dictionary data, string xKey = "grid_x", string yKey = "grid_y")
	{
		return new Vector2I(data.GetValueOrDefault(xKey, 0).AsInt32(), data.GetValueOrDefault(yKey, 0).AsInt32());
	}
}
