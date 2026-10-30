using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class TowerDefensePacketRuntimeState
{
	public static Array ExportChangeCosts(TowerDefensePacketConfig packetConfig)
	{
		Array array = new Array();
		if (!GodotObject.IsInstanceValid(packetConfig) || packetConfig.changeCostList == null)
		{
			return array;
		}
		foreach (TowerDefensePacketChangeCost changeCost in packetConfig.changeCostList)
		{
			if (GodotObject.IsInstanceValid(changeCost))
			{
				array.Add(changeCost.ExportSave());
			}
		}
		return array;
	}

	public static void Write(TowerDefensePacketConfig packetConfig, Dictionary target, string overrideKey, string canChangeCostKey, string changeCostListKey)
	{
		target[overrideKey] = (GodotObject.IsInstanceValid(packetConfig?._override) ? packetConfig._override.Export() : new Dictionary());
		target[canChangeCostKey] = !GodotObject.IsInstanceValid(packetConfig) || packetConfig.canChangeCost;
		target[changeCostListKey] = ExportChangeCosts(packetConfig);
	}

	public static bool TryCreate(string saveKey, Dictionary state, string overrideKey, string canChangeCostKey, string changeCostListKey, out TowerDefensePacketConfig packetConfig)
	{
		packetConfig = null;
		if (string.IsNullOrEmpty(saveKey))
		{
			return true;
		}
		packetConfig = TowerDefenseManager.GetPacketConfig(saveKey);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			packetConfig = null;
			return false;
		}
		packetConfig.coldDownDecreaseDictionary = new Dictionary();
		if (state != null && state.ContainsKey(overrideKey))
		{
			Dictionary dictionary = state[overrideKey].AsGodotDictionary();
			if (dictionary.Count > 0)
			{
				TowerDefensePacketOverride towerDefensePacketOverride = new TowerDefensePacketOverride();
				towerDefensePacketOverride.Init(dictionary);
				packetConfig._override = towerDefensePacketOverride;
			}
			else
			{
				packetConfig._override = null;
			}
		}
		if (state != null && state.ContainsKey(canChangeCostKey))
		{
			packetConfig.canChangeCost = state[canChangeCostKey].AsBool();
		}
		if (state != null && state.ContainsKey(changeCostListKey))
		{
			packetConfig.changeCostList = new List<TowerDefensePacketChangeCost>();
			foreach (Variant item in state[changeCostListKey].AsGodotArray())
			{
				TowerDefensePacketChangeCost towerDefensePacketChangeCost = TowerDefensePacketChangeCost.ImportSave(item.AsGodotDictionary());
				if (GodotObject.IsInstanceValid(towerDefensePacketChangeCost))
				{
					packetConfig.changeCostList.Add(towerDefensePacketChangeCost);
				}
			}
		}
		return true;
	}

	public static Dictionary Normalize(Dictionary source, string overrideKey, string canChangeCostKey, string changeCostListKey)
	{
		Dictionary dictionary = new Dictionary();
		if (source == null)
		{
			return dictionary;
		}
		if (source.ContainsKey(overrideKey))
		{
			dictionary["override"] = source[overrideKey];
		}
		if (source.ContainsKey(canChangeCostKey))
		{
			dictionary["can_change_cost"] = source[canChangeCostKey];
		}
		if (source.ContainsKey(changeCostListKey))
		{
			dictionary["change_cost_list"] = source[changeCostListKey];
		}
		return dictionary;
	}
}
