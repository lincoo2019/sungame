using System.Collections.Generic;
using System.Reflection;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public static class ModApplier
{
	public static bool ApplyPacketConfig(string packetName, Resource newConfig)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance == null)
		{
			GD.PrintErr("[ModApplier] ResourceManager.Instance 为空，无法应用配置");
			return false;
		}
		if (!instance.TOWERDEFENSE_PACKETS.ContainsKey(packetName))
		{
			GD.PrintErr("[ModApplier] 包 '" + packetName + "' 不存在于 ResourceManager.TOWERDEFENSE_PACKETS");
			return false;
		}
		instance.TOWERDEFENSE_PACKETS[packetName] = newConfig;
		ClearPacketConfigCache(packetName);
		GD.Print("[ModApplier] 已应用包配置: " + packetName);
		return true;
	}

	public static int ApplyPacketConfigs(Dictionary<string, Resource> configs)
	{
		int num = 0;
		foreach (KeyValuePair<string, Resource> config in configs)
		{
			if (ApplyPacketConfig(config.Key, config.Value))
			{
				num++;
			}
		}
		GD.Print($"[ModApplier] 批量应用完成: {num}/{configs.Count} 个配置");
		return num;
	}

	public static bool ReloadJsonConfig(string jsonPath)
	{
		if (!ResourceLoader.Exists(jsonPath))
		{
			GD.PrintErr("[ModApplier] JSON 配置不存在: " + jsonPath);
			return false;
		}
		if (ResourceLoader.Load<Json>(jsonPath, null, ResourceLoader.CacheMode.Reuse) == null)
		{
			GD.PrintErr("[ModApplier] 加载 JSON 失败: " + jsonPath);
			return false;
		}
		GD.Print("[ModApplier] 已重载 JSON 配置: " + jsonPath);
		return true;
	}

	private static void ClearPacketConfigCache(string packetName)
	{
		if (typeof(TowerDefenseManager).GetField("_packetConfigRefCache", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) is Dictionary<string, TowerDefensePacketConfig> dictionary)
		{
			dictionary.Remove(packetName);
			GD.Print("[ModApplier] 已清除包配置缓存: " + packetName);
		}
	}
}
