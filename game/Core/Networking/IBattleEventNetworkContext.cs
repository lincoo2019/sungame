using System.Collections.Generic;
using Godot;
using Godot.Collections;

public interface IBattleEventNetworkContext
{
	IDictionary<int, TowerDefenseCharacter> SyncCharacters { get; }

	Array<Node> GetNodesInGroup(string groupName);

	TowerDefenseBattleFeature GetFeature(StringName featureName);

	void EnsureFeature(StringName featureName);

	void RegisterCharacter(int syncId, TowerDefenseCharacter character);
}
