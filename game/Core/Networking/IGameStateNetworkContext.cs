using System.Collections.Generic;
using Godot;
using Godot.Collections;

public interface IGameStateNetworkContext
{
	IDictionary<StringName, TowerDefenseBattleFeature> Features { get; }

	TowerDefenseBattleProcess Process { get; }

	Dictionary GameStateLastSync { get; }

	void SendGameState(string stateJson);

	void EnsureFeature(StringName featureName);
}
