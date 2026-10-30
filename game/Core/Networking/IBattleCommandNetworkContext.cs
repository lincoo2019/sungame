using Godot;
using Godot.Collections;

public interface IBattleCommandNetworkContext
{
	int NextCharacterSyncId();

	TowerDefenseCharacter PlantAt(string plantName, Vector2I gridPos, int syncId, string overrideData = "");

	TowerDefenseCharacter PlantAt(EconomyAccountId economyOwnerAccountId, string plantName, Vector2I gridPos, int syncId, string overrideData = "");

	TowerDefenseCharacter PlantOnCharacter(string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId, string overrideData = "");

	TowerDefenseCharacter PlantOnCharacter(EconomyAccountId economyOwnerAccountId, string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId, string overrideData = "");

	bool RemovePlantAt(Vector2I gridPos);

	bool ShovelPlantAt(Vector2I gridPos);

	bool MoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5);

	bool ExecuteGemMatchCommand(Dictionary command, EconomyAccountId accountId);

	void SpawnZombie(string zombieName, int line, float offsetX, int syncId, string spawnOverride, string spawnConfigOverride);

	void BreakVaseRequest(Vector2I gridPos);

	void SendPlacePlant(string plantName, int gridX, int gridY, int syncId, string overrideData, string requestId = "", string ownerPeerId = "", string placementKind = "grid", int targetSyncId = -1, bool hypnoses = false);

	void SendCommandRejected(string targetPeerId, string rejectedOpCode, string requestId, string reason);

	void SendRemovePlant(int gridX, int gridY);

	void SendUseShovel(int gridX, int gridY);

	void SendMoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5);
}
