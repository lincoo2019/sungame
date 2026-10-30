using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePreSpawnConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelPreSpawnConfig> preSpawnList = new Array<TowerDefenseLevelPreSpawnConfig>();

	[Export(PropertyHint.None, "")]
	public int maxRetryPasses = 8;

	public void Init(Dictionary data)
	{
		Clear();
		maxRetryPasses = Math.Max(1, data.GetValueOrDefault("MaxRetryPasses", 8).AsInt32());
		foreach (Variant item in data.GetValueOrDefault("Packet", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary spawnDictionary = item.AsGodotDictionary();
			TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig();
			towerDefenseLevelPreSpawnConfig.Init(spawnDictionary);
			preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
		}
	}

	public void Clear()
	{
		preSpawnList.Clear();
	}
}
