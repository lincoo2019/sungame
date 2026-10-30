using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class PortalZombieRenderTransformNetworkContextStub : IZombieStateNetworkContext
{
	public IDictionary<int, TowerDefenseCharacter> SyncCharacters { get; } = new System.Collections.Generic.Dictionary<int, TowerDefenseCharacter>();

	public Dictionary ZombieLastSyncState { get; } = new Dictionary();

	public IDictionary<int, int> ZombieSyncMissCount { get; } = new System.Collections.Generic.Dictionary<int, int>();

	public IDictionary<int, Vector2> ZombieTargetPositions { get; } = new System.Collections.Generic.Dictionary<int, Vector2>();

	public IDictionary<int, Vector2> ZombieSyncVelocities { get; } = new System.Collections.Generic.Dictionary<int, Vector2>();

	public IDictionary<int, double> ZombieLastSyncTime { get; } = new System.Collections.Generic.Dictionary<int, double>();

	public double GameTime => 1.0;

	public void SendZombieState(string zombiesJson)
	{
	}

	public void RemoveZombieSyncState(int syncId)
	{
		SyncCharacters.Remove(syncId);
		ZombieTargetPositions.Remove(syncId);
		ZombieSyncVelocities.Remove(syncId);
		ZombieLastSyncTime.Remove(syncId);
	}

	public void RegisterCharacter(int syncId, TowerDefenseCharacter character)
	{
		SyncCharacters[syncId] = character;
	}

	public void CreateMissingZombieFromRoster(int syncId, Dictionary info)
	{
	}

	public void DestroyRemoteZombie(int syncId, TowerDefenseCharacter zombie)
	{
	}
}
