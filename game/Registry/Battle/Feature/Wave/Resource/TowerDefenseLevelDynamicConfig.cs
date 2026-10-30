using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelDynamicConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public int pointIncrementPerWave;

	[Export(PropertyHint.None, "")]
	public int startingPoints;

	[Export(PropertyHint.None, "")]
	public int startingWave;

	[Export(PropertyHint.None, "")]
	public Array<string> zombiePool = new Array<string>();

	public void Init(Dictionary dynamicDictionary)
	{
		pointIncrementPerWave = dynamicDictionary.GetValueOrDefault("PointIncrementPerWave", 0).AsInt32();
		startingPoints = dynamicDictionary.GetValueOrDefault("StartingPoints", 0).AsInt32();
		startingWave = dynamicDictionary.GetValueOrDefault("StartingWave", 0).AsInt32();
		Array array = dynamicDictionary.GetValueOrDefault("ZombiePool", new Array()).AsGodotArray();
		zombiePool = new Array<string>();
		foreach (Variant item in array)
		{
			zombiePool.Add(item.AsString());
		}
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["PointIncrementPerWave"] = pointIncrementPerWave,
			["StartingPoints"] = startingPoints,
			["StartingWave"] = startingWave,
			["ZombiePool"] = zombiePool
		};
	}
}
