using System;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModLevelValidation
{
	public static bool Validate(TowerDefenseLevelBaseConfig level, out string error, Func<string, bool> bankExists = null, Func<string, bool> zombieExists = null)
	{
		error = "";
		if (bankExists == null)
		{
			bankExists = (string key) => ResourceManager.Instance != null && ResourceManager.Instance.TOWERDEFENSE_PACKETBANKS.TryGetValue(key, out var value4) && GodotObject.IsInstanceValid(value4);
		}
		if (zombieExists == null)
		{
			zombieExists = (string key) => ResourceManager.Instance != null && ResourceManager.Instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value4) && value4 is TowerDefensePacketConfig towerDefensePacketConfig && towerDefensePacketConfig.characterConfig is TowerDefenseZombieConfig;
		}
		try
		{
			string text = "NOONE";
			Dictionary<StringName, Dictionary> featureData;
			if (level is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig)
			{
				featureData = towerDefenseLevelNewConfig.featureData;
			}
			else
			{
				if (!(level is TowerDefenseLevelConfig towerDefenseLevelConfig))
				{
					return true;
				}
				featureData = towerDefenseLevelConfig.featureData;
				text = towerDefenseLevelConfig.packetBankMethod.ToString();
			}
			if (featureData.TryGetValue("SeedBank", out var value))
			{
				text = value.GetValueOrDefault("Method", text).AsString();
			}
			if (text.Equals("CHOOSE", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = ((level is TowerDefenseLevelConfig towerDefenseLevelConfig2) ? towerDefenseLevelConfig2.packetBank : "GeneralPlant");
				Dictionary value2;
				string text3 = (featureData.TryGetValue("PacketBank", out value2) ? value2.GetValueOrDefault("PacketBankName", text2).AsString() : text2);
				if (string.IsNullOrWhiteSpace(text3))
				{
					text3 = "GeneralPlant";
				}
				if (!bankExists(text3))
				{
					throw new InvalidOperationException("选卡库不可用：" + text3);
				}
			}
			if (featureData.TryGetValue("Wave", out var value3))
			{
				CheckWaveData(value3, zombieExists);
			}
			else if (level is TowerDefenseLevelConfig towerDefenseLevelConfig3 && GodotObject.IsInstanceValid(towerDefenseLevelConfig3.waveManager))
			{
				CheckWaveData(towerDefenseLevelConfig3.waveManager.Export(), zombieExists);
			}
			else if (level is TowerDefenseLevelNewConfig towerDefenseLevelNewConfig2 && towerDefenseLevelNewConfig2.processName == (StringName)"Wave")
			{
				CheckWaveData(towerDefenseLevelNewConfig2.processData, zombieExists);
			}
			return true;
		}
		catch (Exception ex)
		{
			error = "关卡 " + level?.name + " 配置不可用：" + ex.Message;
			return false;
		}
	}

	private static void CheckWaveData(Dictionary data, Func<string, bool> exists)
	{
		foreach (Variant item in data.GetValueOrDefault("Dynamic", new Godot.Collections.Array()).AsGodotArray())
		{
			CheckPool(item.AsGodotDictionary(), "全局动态池", exists);
		}
		int num = 0;
		foreach (Variant item2 in data.GetValueOrDefault("Wave", new Godot.Collections.Array()).AsGodotArray())
		{
			Dictionary dictionary = item2.AsGodotDictionary();
			string text = "第 " + ++num + " 波";
			CheckPool(dictionary.GetValueOrDefault("Dynamic", new Dictionary()).AsGodotDictionary(), text, exists);
			foreach (Variant item3 in dictionary.GetValueOrDefault("Spawn", new Godot.Collections.Array()).AsGodotArray())
			{
				string text2 = item3.AsGodotDictionary().GetValueOrDefault("Zombie", "").AsString();
				if (!exists(text2))
				{
					throw new InvalidOperationException(text + "僵尸卡不可用：" + text2);
				}
			}
		}
	}

	private static void CheckPool(Dictionary data, string location, Func<string, bool> exists)
	{
		foreach (Variant item in data.GetValueOrDefault("ZombiePool", new Godot.Collections.Array()).AsGodotArray())
		{
			string text = item.AsString();
			if (!exists(text))
			{
				throw new InvalidOperationException(location + "僵尸卡不可用：" + text);
			}
		}
	}
}
