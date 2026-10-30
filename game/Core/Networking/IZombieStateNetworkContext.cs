using System.Collections.Generic;
using Godot;
using Godot.Collections;

public interface IZombieStateNetworkContext
{
	IDictionary<int, TowerDefenseCharacter> SyncCharacters { get; }

	Dictionary ZombieLastSyncState { get; }

	IDictionary<int, int> ZombieSyncMissCount { get; }

	IDictionary<int, Vector2> ZombieTargetPositions { get; }

	IDictionary<int, Vector2> ZombieSyncVelocities { get; }

	IDictionary<int, double> ZombieLastSyncTime { get; }

	double GameTime { get; }

	void SendZombieState(string zombiesJson);

	void RemoveZombieSyncState(int syncId);

	void RegisterCharacter(int syncId, TowerDefenseCharacter character);

	void CreateMissingZombieFromRoster(int syncId, Dictionary info);

	void DestroyRemoteZombie(int syncId, TowerDefenseCharacter zombie);
}
