using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class NetworkEntityRegistry
{
	private int _syncIdCounter;

	private int _packetSyncIdCounter;

	private readonly System.Collections.Generic.Dictionary<int, TowerDefenseCharacter> _characters = new System.Collections.Generic.Dictionary<int, TowerDefenseCharacter>();

	private readonly System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow> _packets = new System.Collections.Generic.Dictionary<int, TowerDefenseInGamePacketShow>();

	private readonly Dictionary _pendingDestroySyncIds = new Dictionary();

	public IReadOnlyDictionary<int, TowerDefenseCharacter> Characters => _characters;

	public IReadOnlyDictionary<int, TowerDefenseInGamePacketShow> Packets => _packets;

	public Dictionary PendingDestroySyncIds => _pendingDestroySyncIds;

	public int NextCharacterSyncId()
	{
		_syncIdCounter++;
		return _syncIdCounter;
	}

	public int NextPacketSyncId()
	{
		_packetSyncIdCounter++;
		return _packetSyncIdCounter;
	}

	public void RegisterCharacter(int syncId, TowerDefenseCharacter character)
	{
		if (syncId >= 0 && GodotObject.IsInstanceValid(character))
		{
			character.syncId = syncId;
			_characters[syncId] = character;
		}
	}

	public bool TryGetCharacter(int syncId, out TowerDefenseCharacter character)
	{
		if (_characters.TryGetValue(syncId, out character))
		{
			return GodotObject.IsInstanceValid(character);
		}
		return false;
	}

	public void UnregisterCharacter(int syncId)
	{
		_characters.Remove(syncId);
	}

	public void RegisterPacket(int syncId, TowerDefenseInGamePacketShow packet)
	{
		if (syncId >= 0 && GodotObject.IsInstanceValid(packet))
		{
			_packets[syncId] = packet;
		}
	}

	public bool TryGetPacket(int syncId, out TowerDefenseInGamePacketShow packet)
	{
		if (_packets.TryGetValue(syncId, out packet))
		{
			return GodotObject.IsInstanceValid(packet);
		}
		return false;
	}

	public void UnregisterPacket(int syncId)
	{
		_packets.Remove(syncId);
	}

	public void Clear()
	{
		_characters.Clear();
		_packets.Clear();
		_pendingDestroySyncIds.Clear();
		_syncIdCounter = 0;
		_packetSyncIdCounter = 0;
	}
}
