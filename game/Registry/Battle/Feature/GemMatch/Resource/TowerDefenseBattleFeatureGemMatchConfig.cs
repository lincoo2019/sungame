using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureGemMatchConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public int boardRows = 5;

	[Export(PropertyHint.None, "")]
	public int boardCols = 9;

	[Export(PropertyHint.None, "")]
	public Array<StringName> plantList = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public int sunPerMatch = 25;

	[Export(PropertyHint.None, "")]
	public int matchValuePerMatch = 25;

	[Export(PropertyHint.None, "")]
	public int clearMatchValue;

	[Export(PropertyHint.None, "")]
	public int clearMatchCount;

	[Export(PropertyHint.None, "")]
	public double fallSpeed = 300.0;

	[Export(PropertyHint.None, "")]
	public double swapDuration = 0.15;

	[Export(PropertyHint.None, "")]
	public double matchResolveDelay = 0.05;

	[Export(PropertyHint.None, "")]
	public float fallSpawnHeight = 100f;

	[Export(PropertyHint.None, "")]
	public double fallRowDelay = 0.075;

	[Export(PropertyHint.None, "")]
	public double minimumFallDuration = 0.15;

	[Export(PropertyHint.None, "")]
	public float dragThreshold = 10f;

	[Export(PropertyHint.None, "")]
	public float inputRadius = 60f;

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> plantUpgradeList = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public int fillHoleCost = 200;

	[Export(PropertyHint.None, "")]
	public string refreshBoardPacketKey = "PlantBlover";

	[Export(PropertyHint.None, "")]
	public int refreshBoardCost = 500;

	private Dictionary _upgradeMap = new Dictionary();

	private Dictionary _upgradeSourceMap = new Dictionary();

	public void Init(Dictionary data)
	{
		plantList.Clear();
		plantUpgradeList.Clear();
		_upgradeMap.Clear();
		_upgradeSourceMap.Clear();
		boardRows = Math.Max(1, data.GetValueOrDefault("boardRows", 5).AsInt32());
		boardCols = Math.Max(1, data.GetValueOrDefault("boardCols", 9).AsInt32());
		foreach (Variant item2 in data.ContainsKey("plantList") ? ((Godot.Collections.Array)data["plantList"]) : new Godot.Collections.Array())
		{
			StringName stringName = new StringName((string)item2);
			if (stringName != (StringName)"" && !plantList.Contains(stringName))
			{
				plantList.Add(stringName);
			}
		}
		sunPerMatch = Math.Max(0, data.GetValueOrDefault("sunPerMatch", 25).AsInt32());
		matchValuePerMatch = Math.Max(0, data.GetValueOrDefault("matchValuePerMatch", sunPerMatch).AsInt32());
		clearMatchValue = Math.Max(0, data.GetValueOrDefault("clearMatchValue", 0).AsInt32());
		clearMatchCount = Math.Max(0, data.GetValueOrDefault("clearMatchCount", clearMatchValue).AsInt32());
		fallSpeed = Math.Max(1.0, data.GetValueOrDefault("fallSpeed", 300.0).AsDouble());
		swapDuration = Math.Max(0.01, data.GetValueOrDefault("swapDuration", 0.15).AsDouble());
		matchResolveDelay = Math.Max(0.0, data.GetValueOrDefault("matchResolveDelay", 0.05).AsDouble());
		fallSpawnHeight = Math.Max(1f, (float)data.GetValueOrDefault("fallSpawnHeight", 100.0).AsDouble());
		fallRowDelay = Math.Max(0.0, data.GetValueOrDefault("fallRowDelay", 0.075).AsDouble());
		minimumFallDuration = Math.Max(0.01, data.GetValueOrDefault("minimumFallDuration", 0.15).AsDouble());
		dragThreshold = Math.Max(0f, (float)data.GetValueOrDefault("dragThreshold", 10.0).AsDouble());
		inputRadius = Math.Max(1f, (float)data.GetValueOrDefault("inputRadius", 60.0).AsDouble());
		fillHoleCost = Math.Max(0, data.GetValueOrDefault("fillHoleCost", 200).AsInt32());
		refreshBoardPacketKey = data.GetValueOrDefault("refreshBoardPacketKey", "PlantBlover").AsString();
		refreshBoardCost = Math.Max(0, data.GetValueOrDefault("refreshBoardCost", 500).AsInt32());
		foreach (Variant item3 in data.ContainsKey("plantUpgradeList") ? ((Godot.Collections.Array)data["plantUpgradeList"]) : new Godot.Collections.Array())
		{
			Dictionary dictionary = (Dictionary)item3;
			StringName stringName2 = new StringName(dictionary.ContainsKey("from") ? ((string)dictionary["from"]) : "");
			StringName stringName3 = new StringName(dictionary.ContainsKey("to") ? ((string)dictionary["to"]) : "");
			int num = Math.Max(0, dictionary.ContainsKey("cost") ? dictionary["cost"].AsInt32() : 0);
			if (stringName2 != (StringName)"" && stringName3 != (StringName)"")
			{
				Dictionary item = new Dictionary
				{
					["from"] = stringName2,
					["to"] = stringName3,
					["cost"] = num
				};
				plantUpgradeList.Add(item);
				Dictionary dictionary2 = new Dictionary();
				dictionary2["to"] = stringName3;
				dictionary2["cost"] = num;
				_upgradeMap[stringName2] = dictionary2;
				_upgradeSourceMap[stringName3] = stringName2;
			}
		}
	}

	public StringName GetUpgradeTarget(StringName characterKey)
	{
		if (_upgradeMap.ContainsKey(characterKey))
		{
			Dictionary dictionary = (Dictionary)_upgradeMap[characterKey];
			if (!dictionary.ContainsKey("to"))
			{
				return "";
			}
			return new StringName((string)dictionary["to"]);
		}
		return "";
	}

	public int GetUpgradeCost(StringName characterKey)
	{
		if (_upgradeMap.ContainsKey(characterKey))
		{
			Dictionary dictionary = (Dictionary)_upgradeMap[characterKey];
			if (!dictionary.ContainsKey("cost"))
			{
				return 0;
			}
			return dictionary["cost"].AsInt32();
		}
		return 0;
	}

	public StringName GetUpgradeSource(StringName targetKey)
	{
		if (!_upgradeSourceMap.TryGetValue(targetKey, out var value))
		{
			return new StringName();
		}
		return value.AsStringName();
	}
}
