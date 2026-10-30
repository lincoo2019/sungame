using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ModSystem;

internal static class XWModContentValidation
{
	public static bool Validate(XWModManifest manifest, out string error)
	{
		error = "";
		try
		{
			foreach (KeyValuePair<string, System.Collections.Generic.Dictionary<string, XWModRuntimeRegistry.Registration>> registration in XWModRuntimeRegistry.GetRegistrations())
			{
				foreach (XWModRuntimeRegistry.Registration value in registration.Value.Values)
				{
					if (!string.Equals(value.OwnerMod, manifest.Id, StringComparison.OrdinalIgnoreCase) || value.OverridesBuiltIn)
					{
						continue;
					}
					if (registration.Key.Equals("Packet", StringComparison.OrdinalIgnoreCase))
					{
						if (!(value.ModValue.AsGodotObject() is TowerDefensePacketConfig towerDefensePacketConfig) || !GodotObject.IsInstanceValid(towerDefensePacketConfig.characterConfig))
						{
							throw new InvalidOperationException("Packet/" + value.Key + " 缺少角色配置");
						}
						if (towerDefensePacketConfig.saveKey != value.Key)
						{
							throw new InvalidOperationException("Packet/" + value.Key + " 的 saveKey 与注册键不一致");
						}
						RequireReference(manifest, "Character", towerDefensePacketConfig.characterConfig.name);
						string key = (ResourceManager.Instance.CHARCTAER_SPRITE.ContainsKey(towerDefensePacketConfig.saveKey) ? towerDefensePacketConfig.saveKey : towerDefensePacketConfig.characterConfig.name);
						RequireReference(manifest, "CharacterSprite", key);
						if (towerDefensePacketConfig.unlockCheckList != null && towerDefensePacketConfig.unlockCheckList.Any((UnlockConditionBaseConfig condition) => !(condition is XWModProgressUnlockCondition)))
						{
							throw new InvalidOperationException("Packet/" + value.Key + " 必须使用 Mod 专属解锁条件");
						}
					}
					else
					{
						if (!registration.Key.Equals("Level", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						Dictionary dictionary = value.ModValue.AsGodotDictionary();
						HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
						foreach (Dictionary item in XWModContentCatalog.Levels(dictionary))
						{
							string text = item.GetValueOrDefault("SaveKey", "").AsString();
							if (string.IsNullOrWhiteSpace(text) || !hashSet.Add(text))
							{
								throw new InvalidOperationException("Level/" + value.Key + " 存在空或重复 SaveKey：" + text);
							}
							Dictionary dictionary2 = item["Level"].AsGodotDictionary();
							if (dictionary2.GetValueOrDefault("Normal", "").AsString().Length == 0)
							{
								throw new InvalidOperationException($"Level/{value.Key}/{text} 缺少普通难度");
							}
							string[] array = new string[3] { "Normal", "Difficult", "Ultimate" };
							foreach (string text2 in array)
							{
								string text3 = dictionary2.GetValueOrDefault(text2, "").AsString();
								if (text3.Length != 0)
								{
									TowerDefenseLevelBaseConfig towerDefenseLevelBaseConfig = XWModContentCatalog.LoadLevel(dictionary, text3);
									if (!GodotObject.IsInstanceValid(towerDefenseLevelBaseConfig) || towerDefenseLevelBaseConfig.name != text)
									{
										throw new InvalidOperationException($"Level/{value.Key}/{text}/{text2} 配置类型或 name 不匹配");
									}
								}
							}
						}
						foreach (Dictionary item2 in XWModContentCatalog.Levels(dictionary))
						{
							RequireOpenKey(item2.GetValueOrDefault("OpenKey", "").AsString(), hashSet, value.Key);
						}
						foreach (Variant item3 in dictionary["Chapter"].AsGodotArray())
						{
							RequireOpenKey(item3.AsGodotDictionary().GetValueOrDefault("OpenKey", "").AsString(), hashSet, value.Key);
						}
					}
				}
			}
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static void RequireOpenKey(string key, HashSet<string> keys, string catalog)
	{
		if (key.Length > 0 && key != "Lock" && !keys.Contains(key))
		{
			throw new InvalidOperationException("Level/" + catalog + " 的 OpenKey 不属于当前目录：" + key);
		}
	}

	private static void RequireReference(XWModManifest manifest, string category, string key)
	{
		if (!((category == "Character") ? ResourceManager.Instance.TOWERDEFENSE_CHARCATERS : ResourceManager.Instance.CHARCTAER_SPRITE).ContainsKey(key))
		{
			throw new InvalidOperationException("缺少 " + category + "/" + key);
		}
		if (!XWModRuntimeRegistry.TryGetEffectiveRegistration(category, key, out var entry) || string.Equals(entry.OwnerMod, manifest.Id, StringComparison.OrdinalIgnoreCase) || manifest.Dependencies.Any((XWModDependency dependency) => string.Equals(dependency.Id, entry.OwnerMod, StringComparison.OrdinalIgnoreCase)))
		{
			return;
		}
		throw new InvalidOperationException($"{category}/{key} 引用了未声明依赖的 Mod：{entry.OwnerMod}");
	}
}
