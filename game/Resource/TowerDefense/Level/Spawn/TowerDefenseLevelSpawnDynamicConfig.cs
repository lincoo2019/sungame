using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelSpawnDynamicConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public int points;

	[Export(PropertyHint.None, "")]
	public Array<string> zombiePool = new Array<string>();

	public void Init(Dictionary dynamicDictionary)
	{
		points = dynamicDictionary.GetValueOrDefault("Point", 0).AsInt32();
		Array obj = (dynamicDictionary.ContainsKey("ZombiePool") ? ((Array)dynamicDictionary["ZombiePool"]) : new Array());
		zombiePool = new Array<string>();
		foreach (Variant item in obj)
		{
			zombiePool.Add(item.AsString());
		}
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Point"] = points,
			["ZombiePool"] = zombiePool
		};
	}
}
