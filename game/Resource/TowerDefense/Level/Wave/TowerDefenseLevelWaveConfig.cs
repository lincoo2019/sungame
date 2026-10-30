using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelWaveConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Array<int> dynamicPlantfood = new Array<int>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelSpawnConfig> spawn = new Array<TowerDefenseLevelSpawnConfig>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelGridSpawnConfig> gridSpawn = new Array<TowerDefenseLevelGridSpawnConfig>();

	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelSpawnDynamicConfig dynamic = new TowerDefenseLevelSpawnDynamicConfig();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseLevelEventBase> eventList = new Array<TowerDefenseLevelEventBase>();

	public void Init(Dictionary waveDictionary)
	{
		if (waveDictionary == null)
		{
			waveDictionary = new Dictionary();
		}
		if (dynamicPlantfood == null)
		{
			dynamicPlantfood = new Array<int>();
		}
		if (spawn == null)
		{
			spawn = new Array<TowerDefenseLevelSpawnConfig>();
		}
		if (gridSpawn == null)
		{
			gridSpawn = new Array<TowerDefenseLevelGridSpawnConfig>();
		}
		if (eventList == null)
		{
			eventList = new Array<TowerDefenseLevelEventBase>();
		}
		dynamicPlantfood.Clear();
		spawn.Clear();
		gridSpawn.Clear();
		eventList.Clear();
		Godot.Collections.Array array = waveDictionary.GetValueOrDefault("DynamicPlantfood", new Godot.Collections.Array()).AsGodotArray();
		if (array != null)
		{
			foreach (Variant item in array)
			{
				dynamicPlantfood.Add(item.AsInt32());
			}
		}
		foreach (Variant item2 in waveDictionary.GetValueOrDefault("Spawn", new Godot.Collections.Array()).AsGodotArray())
		{
			if (item2.VariantType == Variant.Type.Dictionary)
			{
				TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig = new TowerDefenseLevelSpawnConfig();
				towerDefenseLevelSpawnConfig.Init(item2.AsGodotDictionary());
				spawn.Add(towerDefenseLevelSpawnConfig);
			}
		}
		foreach (Variant item3 in waveDictionary.GetValueOrDefault("GridSpawn", new Godot.Collections.Array()).AsGodotArray())
		{
			if (item3.VariantType == Variant.Type.Dictionary)
			{
				TowerDefenseLevelGridSpawnConfig towerDefenseLevelGridSpawnConfig = new TowerDefenseLevelGridSpawnConfig();
				towerDefenseLevelGridSpawnConfig.Init(item3.AsGodotDictionary());
				gridSpawn.Add(towerDefenseLevelGridSpawnConfig);
			}
		}
		Dictionary dynamicDictionary = waveDictionary.GetValueOrDefault("Dynamic", new Dictionary()).AsGodotDictionary();
		dynamic = new TowerDefenseLevelSpawnDynamicConfig();
		dynamic.Init(dynamicDictionary);
		Godot.Collections.Array array2 = waveDictionary.GetValueOrDefault("Event", new Godot.Collections.Array()).AsGodotArray();
		for (int i = 0; i < array2.Count; i++)
		{
			try
			{
				if (array2[i].VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary = array2[i].AsGodotDictionary();
					string text = dictionary.GetValueOrDefault("EventName", "").AsString();
					TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(text);
					if (!GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
					{
						GD.PushError($"[WaveConfig] Unknown Event[{i}] '{text}'.");
					}
					else
					{
						Dictionary valueDictionary = dictionary.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary();
						towerDefenseLevelEventBase.Init(valueDictionary);
						eventList.Add(towerDefenseLevelEventBase);
					}
				}
			}
			catch (Exception value)
			{
				GD.PushError($"[WaveConfig] Failed to load Event[{i}]: {value}");
			}
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["DynamicPlantfood"] = dynamicPlantfood,
			["Spawn"] = new Godot.Collections.Array(),
			["GridSpawn"] = new Godot.Collections.Array(),
			["Dynamic"] = (GodotObject.IsInstanceValid(dynamic) ? dynamic.Export() : new Dictionary()),
			["Event"] = new Godot.Collections.Array()
		};
		foreach (TowerDefenseLevelSpawnConfig item in spawn)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				((Godot.Collections.Array)dictionary["Spawn"]).Add(item.Export());
			}
		}
		foreach (TowerDefenseLevelGridSpawnConfig item2 in gridSpawn)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				((Godot.Collections.Array)dictionary["GridSpawn"]).Add(item2.Export());
			}
		}
		for (int i = 0; i < eventList.Count; i++)
		{
			try
			{
				if (GodotObject.IsInstanceValid(eventList[i]))
				{
					((Godot.Collections.Array)dictionary["Event"]).Add(eventList[i].Export());
				}
			}
			catch (Exception value)
			{
				GD.PushError($"[WaveConfig] Failed to export Event[{i}]: {value}");
			}
		}
		return dictionary;
	}
}
