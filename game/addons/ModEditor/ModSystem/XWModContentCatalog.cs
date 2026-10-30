using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModContentCatalog
{
	public sealed record Catalog(string OwnerModId, string EffectiveOwnerModId, string Key, string Title, Dictionary Data);

	public sealed record Packet(string OwnerModId, string Key, TowerDefensePacketConfig Config);

	public const string PlantCategory = "ModPlants";

	internal static Dictionary FromResource(LevelCatalogConfig catalog)
	{
		Dictionary dictionary = catalog.ToRuntimeDictionary();
		Dictionary dictionary2 = new Dictionary();
		foreach (LevelChapterConfig chapter in catalog.chapterList)
		{
			if (!GodotObject.IsInstanceValid(chapter))
			{
				continue;
			}
			foreach (LevelChooseConfig level in chapter.levelList)
			{
				if (!GodotObject.IsInstanceValid(level))
				{
					continue;
				}
				TowerDefenseLevelBaseConfig[] array = new TowerDefenseLevelBaseConfig[3] { level.normalLevel, level.difficultLevel, level.ultimateLevel };
				foreach (TowerDefenseLevelBaseConfig towerDefenseLevelBaseConfig in array)
				{
					if (GodotObject.IsInstanceValid(towerDefenseLevelBaseConfig))
					{
						dictionary2[towerDefenseLevelBaseConfig.ResourcePath] = towerDefenseLevelBaseConfig;
					}
				}
			}
		}
		dictionary["__mod_level_resources"] = dictionary2;
		return dictionary;
	}

	internal static TowerDefenseLevelBaseConfig LoadLevel(Dictionary catalog, string path)
	{
		if (catalog.TryGetValue("__mod_level_resources", out var value) && value.AsGodotDictionary().TryGetValue(path, out var value2))
		{
			return value2.AsGodotObject() as TowerDefenseLevelBaseConfig;
		}
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<TowerDefenseLevelBaseConfig>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	public static string GetContentOwner(string category, string key)
	{
		IReadOnlyList<XWModRuntimeRegistry.Registration> registrationStack = XWModRuntimeRegistry.GetRegistrationStack(category, key);
		if (registrationStack.Count != 0 && !registrationStack[0].OverridesBuiltIn)
		{
			return registrationStack[0].OwnerMod.ToLowerInvariant();
		}
		return "";
	}

	private static IEnumerable<XWModRuntimeRegistry.Registration> Effective(string category)
	{
		if (!XWModRuntimeRegistry.GetRegistrations().TryGetValue(category, out var value))
		{
			return System.Array.Empty<XWModRuntimeRegistry.Registration>();
		}
		return (from entry in value.Values
			where entry.IsEffective && !entry.OverridesBuiltIn && ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod mod) => mod.Applied && string.Equals(mod.Manifest?.Id, entry.OwnerMod, StringComparison.OrdinalIgnoreCase))
			orderby entry.LoadOrder
			select entry).ThenBy<XWModRuntimeRegistry.Registration, string>((XWModRuntimeRegistry.Registration entry) => entry.Key, StringComparer.Ordinal).ToArray();
	}

	public static IReadOnlyList<Catalog> GetCatalogs()
	{
		return (from entry in Effective("Level")
			where entry.ModValue.VariantType == Variant.Type.Dictionary
			select new Catalog(GetContentOwner("Level", entry.Key), entry.OwnerMod, entry.Key, entry.Key, entry.ModValue.AsGodotDictionary())).ToArray();
	}

	public static IReadOnlyList<Packet> GetPackets(bool plants)
	{
		List<Packet> list = new List<Packet>();
		ResourceManager instance = ResourceManager.Instance;
		if (instance == null)
		{
			return list;
		}
		foreach (XWModRuntimeRegistry.Registration item in Effective("Packet"))
		{
			if (item.ModValue.VariantType == Variant.Type.Object && item.ModValue.AsGodotObject() is TowerDefensePacketConfig towerDefensePacketConfig && GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig) && !(plants ? (!(towerDefensePacketConfig.characterConfig is TowerDefensePlantConfig)) : (!(towerDefensePacketConfig.characterConfig is TowerDefenseZombieConfig))) && instance.TOWERDEFENSE_CHARCATERS.ContainsKey(towerDefensePacketConfig.characterConfig.name) && (instance.CHARCTAER_SPRITE.ContainsKey(towerDefensePacketConfig.saveKey) || instance.CHARCTAER_SPRITE.ContainsKey(towerDefensePacketConfig.characterConfig.name)))
			{
				list.Add(new Packet(GetContentOwner("Packet", item.Key), item.Key, towerDefensePacketConfig));
			}
		}
		return list;
	}

	public static TowerDefensePacketBankData WithPlants(TowerDefensePacketBankData original)
	{
		TowerDefensePacketBankData towerDefensePacketBankData = new TowerDefensePacketBankData
		{
			category = original.category.Duplicate(deep: true)
		};
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (Packet packet in GetPackets(plants: true))
		{
			array.Add(packet.Key);
		}
		if (array.Count > 0)
		{
			towerDefensePacketBankData.category["ModPlants"] = array;
		}
		return towerDefensePacketBankData;
	}

	public static bool TryResolve(XWModLevelIdentity identity, out TowerDefenseLevelBaseConfig config, out string error)
	{
		config = null;
		error = "Mod 关卡身份或资源不可用";
		if (identity == null)
		{
			return false;
		}
		Catalog catalog = GetCatalogs().FirstOrDefault((Catalog item) => string.Equals(item.OwnerModId, identity.OwnerModId, StringComparison.OrdinalIgnoreCase) && item.Key == identity.CatalogKey);
		if (catalog == null)
		{
			return false;
		}
		string text = "";
		int num = 0;
		foreach (Dictionary item in Levels(catalog.Data))
		{
			if (!(item.GetValueOrDefault("SaveKey", "").AsString() != identity.LevelSaveKey))
			{
				num++;
				text = item.GetValueOrDefault("Level", new Dictionary()).AsGodotDictionary().GetValueOrDefault(identity.Difficulty, "")
					.AsString();
			}
		}
		if (num != 1 || string.IsNullOrEmpty(text))
		{
			return false;
		}
		try
		{
			config = LoadLevel(catalog.Data, text);
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
		if (!GodotObject.IsInstanceValid(config) || config.name != identity.LevelSaveKey)
		{
			error = "关卡 name 必须与目录 SaveKey 一致";
			config = null;
			return false;
		}
		if (!XWModLevelValidation.Validate(config, out error))
		{
			return false;
		}
		error = "";
		return true;
	}

	public static IEnumerable<Dictionary> Levels(Dictionary catalog)
	{
		foreach (Variant item in catalog.GetValueOrDefault("Chapter", new Godot.Collections.Array()).AsGodotArray())
		{
			foreach (Variant item2 in item.AsGodotDictionary().GetValueOrDefault("Level", new Godot.Collections.Array()).AsGodotArray())
			{
				yield return item2.AsGodotDictionary();
			}
		}
	}
}
