using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

namespace PVZHE.ModEditor.ModSystem.Validation;

internal static class XWModProjectContentValidation
{
	public static void Validate(XWModExportSnapshot snapshot, List<XWValidationIssue> issues)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		HashSet<string> authoredBanks = new HashSet<string>(StringComparer.Ordinal);
		HashSet<string> authoredZombies = new HashSet<string>(StringComparer.Ordinal);
		foreach (string resource in snapshot.Manifest.Resources)
		{
			string path = snapshot.ResolveFile(resource);
			string text = Path.GetExtension(path).ToLowerInvariant();
			if ((text == ".tres" || text == ".res") ? true : false)
			{
				if (resource.StartsWith("Resources/PacketBank/", StringComparison.OrdinalIgnoreCase) && GodotObject.IsInstanceValid(ResourceLoader.Load<TowerDefensePacketBankData>(path, null, ResourceLoader.CacheMode.IgnoreDeep)))
				{
					authoredBanks.Add(Path.GetFileNameWithoutExtension(path));
				}
				if (resource.StartsWith("Resources/Cards/", StringComparison.OrdinalIgnoreCase) && ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.IgnoreDeep)?.characterConfig is TowerDefenseZombieConfig)
				{
					authoredZombies.Add(Path.GetFileNameWithoutExtension(path));
				}
			}
		}
		int num;
		if (snapshot.Manifest.Scripts.Count <= 0 && snapshot.Manifest.Dependencies.Count <= 0)
		{
			ResourceManager instance = ResourceManager.Instance;
			num = ((instance == null || !instance.AreFullGameplayResourcesReady) ? 1 : 0);
		}
		else
		{
			num = 1;
		}
		bool flag = (byte)num != 0;
		string path2;
		HashSet<string> keys;
		foreach (string item in snapshot.Manifest.Resources.Concat(snapshot.Manifest.Blueprints))
		{
			path2 = snapshot.ResolveFile(item);
			bool flag2;
			switch (Path.GetExtension(path2).ToLowerInvariant())
			{
			case ".tres":
			case ".res":
			case ".tscn":
			case ".scn":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			if (!flag2)
			{
				continue;
			}
			string text = Path.GetExtension(path2).ToLowerInvariant();
			if ((text == ".tres" || text == ".tscn") ? true : false)
			{
				foreach (Match item2 in Regex.Matches(File.ReadAllText(path2), "(?m)^\\s*\\[ext_resource[^\\r\\n]*\\bpath=\"([^\"]+)\""))
				{
					if (Path.IsPathRooted(item2.Groups[1].Value))
					{
						Add(issues, path2, "资源引用依赖作者电脑绝对路径，请重新保存为工程相对引用：" + item2.Groups[1].Value);
					}
				}
			}
			string[] dependencies = ResourceLoader.GetDependencies(path2);
			for (int i = 0; i < dependencies.Length; i++)
			{
				string[] array = dependencies[i].Split("::");
				string text2 = ((array.Length >= 3 && array[^1].Length > 0) ? array[^1] : array[0]);
				if (text2.StartsWith("uid://", StringComparison.Ordinal))
				{
					if (!ResourceLoader.Exists(text2))
					{
						Add(issues, path2, "资源 UID 需要运行时确认，请保留可解析的资源路径：" + text2, warning: true);
					}
					continue;
				}
				if (text2.StartsWith("res://", StringComparison.Ordinal))
				{
					if (!ResourceLoader.Exists(text2))
					{
						Add(issues, path2, "找不到内置资源：" + text2);
					}
					continue;
				}
				string fullPath = Path.GetFullPath(Path.IsPathRooted(text2) ? text2 : Path.Combine(Path.GetDirectoryName(path2), text2));
				string value = Path.GetRelativePath(snapshot.Root, fullPath).Replace('\\', '/');
				if (!snapshot.Files.Contains<string>(value, StringComparer.OrdinalIgnoreCase))
				{
					Add(issues, path2, "安装包缺少引用资源：" + text2);
				}
			}
			if (!item.StartsWith("Resources/LevelCatalogs/", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			LevelCatalogConfig levelCatalogConfig = ResourceLoader.Load<LevelCatalogConfig>(path2, null, ResourceLoader.CacheMode.IgnoreDeep);
			if (!GodotObject.IsInstanceValid(levelCatalogConfig))
			{
				Add(issues, path2, "无法读取关卡选择目录。");
				continue;
			}
			keys = new HashSet<string>(StringComparer.Ordinal);
			foreach (LevelChapterConfig chapter in levelCatalogConfig.chapterList)
			{
				if (!GodotObject.IsInstanceValid(chapter))
				{
					Add(issues, path2, "目录包含空章节。");
					continue;
				}
				foreach (LevelChooseConfig level in chapter.levelList)
				{
					if (!GodotObject.IsInstanceValid(level))
					{
						Add(issues, path2, "目录包含空关卡。");
						continue;
					}
					if (string.IsNullOrWhiteSpace(level.saveKey) || !keys.Add(level.saveKey))
					{
						Add(issues, path2, "关卡 SaveKey 为空或重复：" + level.saveKey);
					}
					if (!GodotObject.IsInstanceValid(level.normalLevel))
					{
						Add(issues, path2, level.saveKey + "：请绑定普通难度关卡。");
					}
					TowerDefenseLevelBaseConfig[] array2 = new TowerDefenseLevelBaseConfig[3] { level.normalLevel, level.difficultLevel, level.ultimateLevel };
					foreach (TowerDefenseLevelBaseConfig towerDefenseLevelBaseConfig in array2)
					{
						if (GodotObject.IsInstanceValid(towerDefenseLevelBaseConfig))
						{
							hashSet.Add(towerDefenseLevelBaseConfig.ResourcePath.Replace('\\', '/'));
							if (towerDefenseLevelBaseConfig.name != level.saveKey)
							{
								Add(issues, path2, level.saveKey + "：关卡 name 必须与 SaveKey 一致。");
							}
							if (!XWModLevelValidation.Validate(towerDefenseLevelBaseConfig, out var error, (string key) => authoredBanks.Contains(key) || ResolveExternalResource(snapshot.Manifest, "PacketBank", key) is TowerDefensePacketBankData, (string key) => authoredZombies.Contains(key) || (ResolveExternalResource(snapshot.Manifest, "Packet", key) is TowerDefensePacketConfig towerDefensePacketConfig && towerDefensePacketConfig.characterConfig is TowerDefenseZombieConfig)))
							{
								Add(issues, path2, error + (flag ? "（依赖或运行环境就绪后仍需验证）" : ""), flag);
							}
						}
					}
				}
			}
			foreach (LevelChapterConfig item3 in levelCatalogConfig.chapterList.Where(GodotObject.IsInstanceValid))
			{
				CheckKey(item3.openKey);
				foreach (LevelChooseConfig item4 in item3.levelList.Where(GodotObject.IsInstanceValid))
				{
					CheckKey(item4.openKey);
				}
			}
		}
		foreach (string item5 in snapshot.Manifest.Resources.Where(delegate(string file)
		{
			bool flag3 = file.StartsWith("Resources/Levels/", StringComparison.OrdinalIgnoreCase);
			if (flag3)
			{
				string text4 = Path.GetExtension(file).ToLowerInvariant();
				bool flag4 = ((text4 == ".tres" || text4 == ".res") ? true : false);
				flag3 = flag4;
			}
			return flag3;
		}))
		{
			string text3 = snapshot.ResolveFile(item5).Replace('\\', '/');
			if (!hashSet.Contains(text3))
			{
				Add(issues, text3, "此关卡尚未加入关卡选择目录，玩家无法从目录找到它。", warning: true);
			}
		}
		void CheckKey(string key)
		{
			if (!string.IsNullOrEmpty(key) && key != "Lock" && !keys.Contains(key))
			{
				Add(issues, path2, "解锁条件引用了目录外关卡：" + key);
			}
		}
	}

	internal static Resource ResolveExternalResource(XWModManifest manifest, string category, string key)
	{
		IReadOnlyList<XWModRuntimeRegistry.Registration> registrationStack = XWModRuntimeRegistry.GetRegistrationStack(category, key);
		foreach (XWModRuntimeRegistry.Registration layer in registrationStack.Reverse())
		{
			if (!string.Equals(layer.OwnerMod, manifest.Id, StringComparison.OrdinalIgnoreCase))
			{
				XWModDependency dependency = manifest.Dependencies.FirstOrDefault((XWModDependency item) => string.Equals(item.Id, layer.OwnerMod, StringComparison.OrdinalIgnoreCase));
				if (dependency != null && ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => mod.Applied && string.Equals(mod.LoadedId, dependency.Id, StringComparison.OrdinalIgnoreCase) && (string.IsNullOrEmpty(dependency.Version) || string.Equals(mod.LoadedVersion, dependency.Version, StringComparison.OrdinalIgnoreCase))))
				{
					return layer.ModValue.AsGodotObject() as Resource;
				}
			}
		}
		if (registrationStack.Count > 0)
		{
			if (!registrationStack[0].OverridesBuiltIn)
			{
				return null;
			}
			return registrationStack[0].OriginalValue.AsGodotObject() as Resource;
		}
		ResourceManager instance = ResourceManager.Instance;
		if (instance == null)
		{
			return null;
		}
		if (category == "PacketBank")
		{
			if (!instance.TOWERDEFENSE_PACKETBANKS.TryGetValue(key, out var value))
			{
				return null;
			}
			return value;
		}
		if (!instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value2))
		{
			return null;
		}
		return value2;
	}

	private static void Add(List<XWValidationIssue> issues, string path, string message, bool warning = false)
	{
		issues.Add(new XWValidationIssue
		{
			Code = XWValidationIssue.IssueCode.MissingResource,
			Level = (warning ? XWValidationIssue.Severity.Warning : XWValidationIssue.Severity.Error),
			Message = message,
			FilePath = path,
			JumpTarget = path
		});
	}
}
