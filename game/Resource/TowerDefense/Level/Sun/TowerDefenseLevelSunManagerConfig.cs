using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseLevelSunManagerConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public bool open = true;

	[Export(PropertyHint.None, "")]
	public string type = "Normal";

	[Export(PropertyHint.None, "")]
	public long begin = 300L;

	[Export(PropertyHint.None, "")]
	public double spawnInterval = 12.0;

	[Export(PropertyHint.None, "")]
	public long spawnNum = 50L;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.SUN_MOVING_METHOD movingMethod;

	public void Init(Dictionary sunManagerData)
	{
		open = sunManagerData.GetValueOrDefault("Open", true).AsBool();
		type = sunManagerData.GetValueOrDefault("Type", "Normal").AsString();
		begin = sunManagerData.GetValueOrDefault("Begin", 300).AsInt64();
		spawnInterval = sunManagerData.GetValueOrDefault("SpawnInterval", 12.0).AsDouble();
		spawnNum = sunManagerData.GetValueOrDefault("SpawnNum", 50).AsInt64();
		movingMethod = (TowerDefenseEnum.SUN_MOVING_METHOD)Enum.Parse(typeof(TowerDefenseEnum.SUN_MOVING_METHOD), sunManagerData.GetValueOrDefault("MovingMethod", "LAND").AsString().ToUpper());
	}

	public Dictionary Export()
	{
		return new Dictionary
		{
			["Open"] = open,
			["Type"] = type,
			["Begin"] = begin,
			["SpawnInterval"] = spawnInterval,
			["SpawnNum"] = spawnNum,
			["MovingMethod"] = Enum.GetName(typeof(TowerDefenseEnum.SUN_MOVING_METHOD), movingMethod)
		};
	}
}
