using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class PacketReplicator : NetworkReplicatorBase
{
	private readonly IPacketNetworkContext _packetContext;

	public PacketReplicator()
	{
	}

	public PacketReplicator(IPacketNetworkContext packetContext)
	{
		_packetContext = packetContext;
	}

	public Array BuildSnapshot()
	{
		Array array = new Array();
		if (_packetContext == null)
		{
			return array;
		}
		foreach (KeyValuePair<int, TowerDefenseInGamePacketShow> syncPacket in _packetContext.SyncPackets)
		{
			TowerDefenseInGamePacketShow value = syncPacket.Value;
			if (GodotObject.IsInstanceValid(value) && GodotObject.IsInstanceValid(value.config))
			{
				Vector2 vector = (GodotObject.IsInstanceValid(value.moveComponent) ? value.moveComponent.velocity : Vector2.Zero);
				Dictionary dictionary = new Dictionary
				{
					["sync_id"] = syncPacket.Key,
					["packet_name"] = value.config.saveKey,
					["pos_x"] = value.GlobalPosition.X,
					["pos_y"] = value.GlobalPosition.Y,
					["alive_time"] = ((value.aliveTime < 0.0) ? (-1.0) : Mathf.Max(0.0, value.aliveTime - value.aliveTimer)),
					["is_fall"] = false,
					["use_cost"] = value.useCost,
					["velocity_x"] = vector.X,
					["velocity_y"] = vector.Y,
					["z_index"] = value.ZIndex,
					["fall_height"] = value.GlobalPosition.Y,
					["alive"] = value.alive,
					["sun_account"] = value.SunAccountId.ToString()
				};
				TowerDefensePacketRuntimeState.Write(value.config, dictionary, "packet_override", "can_change_cost", "change_cost_list");
				array.Add(dictionary);
			}
		}
		return array;
	}

	public void ApplySnapshot(Array snapshot)
	{
		if (_packetContext == null || snapshot == null)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (Variant item in snapshot)
		{
			if (item.VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary = item.AsGodotDictionary();
			int num = dictionary.GetValueOrDefault("sync_id", -1).AsInt32();
			if (num < 0)
			{
				continue;
			}
			hashSet.Add(num);
			if (!_packetContext.TryGetPacket(num, out var packet))
			{
				ApplySpawn(dictionary);
				_packetContext.TryGetPacket(num, out packet);
			}
			if (GodotObject.IsInstanceValid(packet))
			{
				packet.GlobalPosition = new Vector2((float)dictionary.GetValueOrDefault("pos_x", packet.GlobalPosition.X).AsDouble(), (float)dictionary.GetValueOrDefault("pos_y", packet.GlobalPosition.Y).AsDouble());
				packet.aliveTime = dictionary.GetValueOrDefault("alive_time", packet.aliveTime).AsDouble();
				packet.aliveTimer = 0.0;
				if (GodotObject.IsInstanceValid(packet.moveComponent))
				{
					packet.moveComponent.velocity = new Vector2((float)dictionary.GetValueOrDefault("velocity_x", packet.moveComponent.velocity.X).AsDouble(), (float)dictionary.GetValueOrDefault("velocity_y", packet.moveComponent.velocity.Y).AsDouble());
				}
				if (dictionary.GetValueOrDefault("alive", true).AsBool())
				{
					_packetContext.UnlockPacket(packet);
				}
				else
				{
					_packetContext.LockPacket(packet);
				}
			}
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, TowerDefenseInGamePacketShow> syncPacket in _packetContext.SyncPackets)
		{
			if (!hashSet.Contains(syncPacket.Key))
			{
				list.Add(syncPacket.Key);
			}
		}
		foreach (int item2 in list)
		{
			if (_packetContext.TryGetPacket(item2, out var packet2))
			{
				_packetContext.RemovePacket(item2, packet2);
			}
			else
			{
				_packetContext.UnregisterPacket(item2);
			}
		}
	}

	public void ApplySpawn(Dictionary data)
	{
		if (_packetContext == null || data == null)
		{
			return;
		}
		EconomyAccountId.TryParse(data.GetValueOrDefault("sun_account", "").AsString(), out var accountId);
		PacketSpawnState state = new PacketSpawnState(data.GetValueOrDefault("sync_id", -1).AsInt32(), data.GetValueOrDefault("packet_name", "").AsString(), new Vector2((float)data.GetValueOrDefault("pos_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("pos_y", 0.0).AsDouble()), data.GetValueOrDefault("alive_time", 15.0).AsDouble(), data.GetValueOrDefault("is_fall", false).AsBool(), data.GetValueOrDefault("use_cost", false).AsBool(), new Vector2((float)data.GetValueOrDefault("velocity_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("velocity_y", -300.0).AsDouble()), data.GetValueOrDefault("z_index", 0).AsInt32(), data.GetValueOrDefault("fall_height", 0.0).AsDouble(), 1.0, TowerDefensePacketRuntimeState.Normalize(data, "packet_override", "can_change_cost", "change_cost_list"), accountId);
		if (state.SyncId >= 0 && !(state.PacketName == "") && !_packetContext.TryGetPacket(state.SyncId, out var _))
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = _packetContext.CreatePacket(state);
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				_packetContext.RegisterPacket(state.SyncId, towerDefenseInGamePacketShow);
			}
		}
	}

	public bool ApplyPick(int syncId, string pickType = "remove")
	{
		if (_packetContext == null || syncId < 0)
		{
			return false;
		}
		if (!_packetContext.TryGetPacket(syncId, out var packet))
		{
			_packetContext.UnregisterPacket(syncId);
			return false;
		}
		if (pickType == "lock")
		{
			_packetContext.LockPacket(packet);
			return true;
		}
		if (pickType == "unlock")
		{
			_packetContext.UnlockPacket(packet);
			return true;
		}
		_packetContext.RemovePacket(syncId, packet);
		return true;
	}

	public bool ApplyPickForPeer(int syncId, string pickType, string authenticatedPeerId)
	{
		if (_packetContext == null || syncId < 0 || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !TowerDefenseManager.Instance.TryResolveTrustedPeerSunAccount(authenticatedPeerId, out var accountId))
		{
			return false;
		}
		if (!_packetContext.TryGetPacket(syncId, out var packet) || !GodotObject.IsInstanceValid(packet))
		{
			_packetContext.UnregisterPacket(syncId);
			return false;
		}
		if (packet.SunAccountId.IsValid && packet.SunAccountId != accountId)
		{
			return false;
		}
		return ApplyPick(syncId, pickType);
	}
}
