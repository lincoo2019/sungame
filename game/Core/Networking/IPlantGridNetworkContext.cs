using Godot;
using Godot.Collections;

public interface IPlantGridNetworkContext
{
	bool IsHost { get; }

	Array GetPlants();

	Array GetGravestones();

	void SendPlantGridSnapshot(string snapshotJson);

	TowerDefenseCharacter PlantAt(string plantName, Vector2I gridPos, int syncId);

	TowerDefenseCharacter PlantAt(EconomyAccountId economyOwnerAccountId, string plantName, Vector2I gridPos, int syncId, string overrideData = "");

	void DestroyRemoteCharacter(TowerDefenseCharacter character);
}
