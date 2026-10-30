using System.Collections.Generic;

public interface IPacketNetworkContext
{
	IDictionary<int, TowerDefenseInGamePacketShow> SyncPackets { get; }

	TowerDefenseInGamePacketShow CreatePacket(PacketSpawnState state);

	void RegisterPacket(int syncId, TowerDefenseInGamePacketShow packet);

	bool TryGetPacket(int syncId, out TowerDefenseInGamePacketShow packet);

	void UnregisterPacket(int syncId);

	void LockPacket(TowerDefenseInGamePacketShow packet);

	void UnlockPacket(TowerDefenseInGamePacketShow packet);

	void RemovePacket(int syncId, TowerDefenseInGamePacketShow packet);
}
