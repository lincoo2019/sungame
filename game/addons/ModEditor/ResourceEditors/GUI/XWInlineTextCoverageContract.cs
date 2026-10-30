using System;
using System.Collections.Generic;
using System.Text;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public static class XWInlineTextCoverageContract
{
	private static readonly System.Collections.Generic.Dictionary<string, XWInlineTextRole> DeclaredOverrides = new System.Collections.Generic.Dictionary<string, XWInlineTextRole>(StringComparer.Ordinal)
	{
		["TowerDefensePacketConfig.name"] = XWInlineTextRole.Title,
		["TowerDefensePacketConfig.describe"] = XWInlineTextRole.Description,
		["TowerDefensePacketConfig.handbookDescribe"] = XWInlineTextRole.Description,
		["TowerDefenseLevelConfig.levelName"] = XWInlineTextRole.Title,
		["TowerDefenseLevelConfig.description"] = XWInlineTextRole.Description,
		["TowerDefenseLevelNewConfig.levelName"] = XWInlineTextRole.Title,
		["TowerDefenseLevelNewConfig.description"] = XWInlineTextRole.Description,
		["NpcTalkBaseConfig.text"] = XWInlineTextRole.Dialogue,
		["ShopItemStageConfig.describe"] = XWInlineTextRole.Description,
		["CharacterCustomConfig.customName"] = XWInlineTextRole.Title,
		["CharacterCustomConfig.customHandbookName"] = XWInlineTextRole.Title,
		["CharacterDamagePointConfig.damagePointName"] = XWInlineTextRole.Title,
		["TowerDefenseMapConfig.translate"] = XWInlineTextRole.Title,
		["TowerDefenseBackgroundMusicConfig.translate"] = XWInlineTextRole.Title,
		["ArmorSlotConfig.armorName"] = XWInlineTextRole.TechnicalBinding,
		["ArmorSlotConfig.closeFliter"] = XWInlineTextRole.TechnicalBinding,
		["ArmorSlotConfig.destroyFliter"] = XWInlineTextRole.TechnicalBinding,
		["ArmorSlotConfig.openFliter"] = XWInlineTextRole.TechnicalBinding,
		["CannonComponentDefinition.mode"] = XWInlineTextRole.TechnicalKey,
		["CatapultComponentDefinition.speedDamagePointName"] = XWInlineTextRole.TechnicalBinding,
		["ChangeProjectileComponentDefinition.changeName"] = XWInlineTextRole.TechnicalBinding,
		["DestroyComponentDefinition.ashShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["GarlicComponentDefinition.discardShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["GravebusterComponentDefinition.discardShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["HitFlashComponentDefinition.brightShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["HitFlashComponentDefinition.whiteShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["MousePressComponentDefinition.brightnessShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["NpcTalkBaseConfig.npc"] = XWInlineTextRole.TechnicalBinding,
		["NpcTalkBuyConfig.npc"] = XWInlineTextRole.TechnicalBinding,
		["NpcTalkHandConfig.npc"] = XWInlineTextRole.TechnicalBinding,
		["NpcTalkTutorialConfig.npc"] = XWInlineTextRole.TechnicalBinding,
		["PotatoComponentDefinition.readyTimerName"] = XWInlineTextRole.TechnicalBinding,
		["PuzzleShaderComponentDefinition.shaderParameter"] = XWInlineTextRole.TechnicalKey,
		["RiseComponentDefinition.discardShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["ShopItemStageConfig.npcTalk"] = XWInlineTextRole.TechnicalBinding,
		["ShovelEventCreateProjectileConfig.projecileName"] = XWInlineTextRole.TechnicalBinding,
		["SleepComponentDefinition.daySleepValue"] = XWInlineTextRole.TechnicalKey,
		["SleepComponentDefinition.neverSleepValue"] = XWInlineTextRole.TechnicalKey,
		["SleepComponentDefinition.nightSleepValue"] = XWInlineTextRole.TechnicalKey,
		["SleepComponentDefinition.sleepBuffName"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseArmorTypeData.armorName"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseBackgroundMusicConfig.drums"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseBackgroundMusicConfig.entry"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseBackgroundMusicConfig.flag1"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseBackgroundMusicConfig.win"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseCharacterBuffHypnoses.torchwoodChangeName"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseCharacterConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseCraterConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseGravestoneConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseItemConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseMowerConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefensePlantConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseVaseConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseZombieConfig.sleepTime"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseCharacterEventBowlingHurt.dir"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseLevelConfig.nextLevel"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelNewConfig.nextLevel"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelEventCreateProtal.protalShape"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelEventCurrentMapFunctionExecute.functionName"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelIZMManagerConfig.failureIgnoredZombieName"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelSpawnConfig.zombie"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseLevelWaveManagerConfig.flagZombie"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseMapConfig.dayNightSwitching"] = XWInlineTextRole.TechnicalKey,
		["TowerDefensePacketConfig.plantfood"] = XWInlineTextRole.TechnicalKey,
		["TowerDefenseProjectileConfig.skinName"] = XWInlineTextRole.TechnicalBinding,
		["TowerDefenseProjectileCreateData.skinName"] = XWInlineTextRole.TechnicalBinding,
		["WaterEnvironmentComponentDefinition.discardShaderParameter"] = XWInlineTextRole.TechnicalKey,
		["WaterInteractionComponentDefinition.discardShaderParameter"] = XWInlineTextRole.TechnicalKey
	};

	private static readonly System.Collections.Generic.Dictionary<string, XWInlineTextSpecializedCoverage> SpecializedCoverage = new System.Collections.Generic.Dictionary<string, XWInlineTextSpecializedCoverage>(StringComparer.Ordinal) { ["AdobeAnimateData.animeFile"] = new XWInlineTextSpecializedCoverage("animation-timeline", "DatFileButton", "DatFileDialog", "MOD_EDITOR_ANIMATION_FRAME_TIMELINE_PROBE") };

	private static readonly System.Collections.Generic.Dictionary<string, string> DeclaredBindingBaseTypes = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal)
	{
		["ArmorSlotConfig.armorName"] = "TowerDefenseArmorTypeData",
		["NpcTalkBaseConfig.npc"] = "TowerDefenseCharacterConfig",
		["NpcTalkBuyConfig.npc"] = "TowerDefenseCharacterConfig",
		["NpcTalkHandConfig.npc"] = "TowerDefenseCharacterConfig",
		["NpcTalkTutorialConfig.npc"] = "TowerDefenseCharacterConfig",
		["ShopItemStageConfig.npcTalk"] = "NpcTalkBaseConfig",
		["ShovelEventCreateProjectileConfig.projecileName"] = "TowerDefenseProjectileConfig",
		["SleepComponentDefinition.sleepBuffName"] = "TowerDefenseCharacterBuffConfig",
		["TowerDefenseBackgroundMusicConfig.drums"] = "AudioStream",
		["TowerDefenseBackgroundMusicConfig.entry"] = "AudioStream",
		["TowerDefenseBackgroundMusicConfig.flag1"] = "AudioStream",
		["TowerDefenseBackgroundMusicConfig.win"] = "AudioStream",
		["TowerDefenseLevelConfig.nextLevel"] = "TowerDefenseLevelConfig",
		["TowerDefenseLevelNewConfig.nextLevel"] = "TowerDefenseLevelConfig",
		["TowerDefenseLevelIZMManagerConfig.failureIgnoredZombieName"] = "TowerDefenseCharacterConfig",
		["TowerDefenseLevelSpawnConfig.zombie"] = "TowerDefenseCharacterConfig",
		["TowerDefenseLevelWaveManagerConfig.flagZombie"] = "TowerDefenseCharacterConfig"
	};

	private static readonly string[] DialogueTokens = new string[6] { "dialog", "dialogue", "talktext", "speech", "conversation", "subtitle" };

	private static readonly string[] DescriptionTokens = new string[7] { "description", "describe", "handbook", "detailtext", "bodytext", "explanation", "contenttext" };

	private static readonly string[] HudTokens = new string[11]
	{
		"message", "broadcast", "tip", "hinttext", "notice", "prompt", "toast", "progresstext", "survivaltext", "difficultytext",
		"texttemplate"
	};

	private static readonly string[] ButtonTokens = new string[7] { "buttontext", "buttonlabel", "labeltext", "caption", "optiontext", "tabtext", "menutext" };

	private static readonly string[] TitleTokens = new string[8] { "title", "displayname", "levelname", "chaptername", "itemname", "shopname", "npcname", "categoryname" };

	private static readonly string[] TechnicalKeyTokens = new string[11]
	{
		"key", "id", "uid", "version", "registry", "type", "category", "tag", "group", "slot",
		"layer"
	};

	private static readonly string[] TechnicalPathTokens = new string[6] { "path", "file", "directory", "folder", "uri", "url" };

	private static readonly string[] TechnicalBindingTokens = new string[29]
	{
		"scene", "script", "resource", "texture", "sprite", "media", "audio", "sound", "music", "bgm",
		"animation", "anime", "clip", "method", "signal", "event", "state", "process", "feature", "property",
		"node", "map", "packet", "projectile", "character", "config", "class", "action", "component"
	};

	private static readonly string[] TechnicalNamePrefixes = new string[21]
	{
		"node", "media", "method", "event", "state", "process", "feature", "animation", "anime", "clip",
		"resource", "property", "class", "script", "scene", "audio", "sound", "texture", "sprite", "slot",
		"layer"
	};

	public static bool TryGetSpecializedCoverage(XWInlineTextProperty property, out XWInlineTextSpecializedCoverage coverage)
	{
		coverage = null;
		if (property != null)
		{
			return SpecializedCoverage.TryGetValue(property.CoverageKey, out coverage);
		}
		return false;
	}

	public static bool TryResolveBindingBaseType(XWInlineTextProperty property, out string baseType)
	{
		baseType = "";
		if (property == null || property.IsTextEnum || property.IsNodePath)
		{
			return false;
		}
		if (DeclaredBindingBaseTypes.TryGetValue(property.CoverageKey, out baseType))
		{
			return true;
		}
		if (TryResolveHintBaseType(property.Hint, property.HintString, out baseType))
		{
			return true;
		}
		if (property.Role != XWInlineTextRole.TechnicalBinding)
		{
			return false;
		}
		string text = Normalize(property.Property.ToString());
		string text2 = Normalize(property.OwnerType);
		if (text.Contains("npctalk", StringComparison.Ordinal))
		{
			baseType = "NpcTalkBaseConfig";
		}
		else if (text.Contains("zombie", StringComparison.Ordinal) || text.Contains("character", StringComparison.Ordinal) || text == "npc")
		{
			baseType = "TowerDefenseCharacterConfig";
		}
		else if (text.Contains("packet", StringComparison.Ordinal))
		{
			baseType = "TowerDefensePacketConfig";
		}
		else if (text.Contains("projectile", StringComparison.Ordinal) || text.Contains("projecile", StringComparison.Ordinal))
		{
			baseType = "TowerDefenseProjectileConfig";
		}
		else if (text.Contains("nextlevel", StringComparison.Ordinal))
		{
			baseType = "TowerDefenseLevelConfig";
		}
		else if (text.Contains("map", StringComparison.Ordinal))
		{
			baseType = "TowerDefenseMapConfig";
		}
		else if (text.Contains("texture", StringComparison.Ordinal) || text.Contains("sprite", StringComparison.Ordinal) || text.Contains("media", StringComparison.Ordinal))
		{
			baseType = "Texture2D";
		}
		else if (text.Contains("audio", StringComparison.Ordinal) || text.Contains("sound", StringComparison.Ordinal) || text.Contains("music", StringComparison.Ordinal) || text.Contains("bgm", StringComparison.Ordinal) || text2.Contains("backgroundmusic", StringComparison.Ordinal))
		{
			baseType = "AudioStream";
		}
		else if (text.Contains("animation", StringComparison.Ordinal) || text.Contains("anime", StringComparison.Ordinal))
		{
			baseType = "AdobeAnimateData";
		}
		else if (text.Contains("scene", StringComparison.Ordinal))
		{
			baseType = "PackedScene";
		}
		else if (text.Contains("script", StringComparison.Ordinal))
		{
			baseType = "Script";
		}
		else if (text.Contains("state", StringComparison.Ordinal))
		{
			baseType = "StateMachineDefinition";
		}
		else if (text.Contains("component", StringComparison.Ordinal))
		{
			baseType = "CharacterComponentDefinition";
		}
		else if (text.Contains("buff", StringComparison.Ordinal))
		{
			baseType = "TowerDefenseCharacterBuffConfig";
		}
		return !string.IsNullOrWhiteSpace(baseType);
	}

	public static bool ShouldStorePickedResourcePath(XWInlineTextProperty property)
	{
		bool flag = property != null;
		if (flag)
		{
			PropertyHint hint = property.Hint;
			bool flag2 = (((ulong)(hint - 13) <= 4uL) ? true : false);
			flag = flag2;
		}
		return flag;
	}

	public static IReadOnlyList<XWInlineTextProperty> Inspect(Resource resource, string category)
	{
		List<XWInlineTextProperty> list = new List<XWInlineTextProperty>();
		if (!GodotObject.IsInstanceValid(resource))
		{
			return list;
		}
		foreach (Dictionary property in resource.GetPropertyList())
		{
			if (property.ContainsKey("name") && property.ContainsKey("type") && property.ContainsKey("usage"))
			{
				string text = property["name"].AsString();
				Variant.Type type = (Variant.Type)property["type"].AsInt32();
				PropertyUsageFlags usage = (PropertyUsageFlags)property["usage"].AsInt64();
				if (IsAuthorTextProperty(text, type, usage))
				{
					PropertyHint hint = (PropertyHint)(property.ContainsKey("hint") ? property["hint"].AsInt32() : 0);
					string hintString = (property.ContainsKey("hint_string") ? property["hint_string"].AsString() : "");
					string reason;
					XWInlineTextRole role = Classify(resource.GetType().Name, text, type, hint, hintString, out reason);
					list.Add(new XWInlineTextProperty
					{
						Owner = resource,
						Property = new StringName(text),
						VariantType = type,
						Hint = hint,
						HintString = hintString,
						Category = (string.IsNullOrWhiteSpace(category) ? "General" : category),
						OwnerType = resource.GetType().Name,
						Label = Humanize(text),
						Role = role,
						ClassificationReason = reason
					});
				}
			}
		}
		return list;
	}

	public static XWInlineTextRole Classify(string ownerType, string propertyName, Variant.Type type, PropertyHint hint, string hintString, out string reason)
	{
		string key = ownerType + "." + propertyName;
		if (DeclaredOverrides.TryGetValue(key, out var value))
		{
			reason = "declared:" + value;
			return value;
		}
		string text = Normalize(propertyName);
		if (type == Variant.Type.NodePath || ContainsAny(text, TechnicalPathTokens))
		{
			reason = "technical:path";
			return XWInlineTextRole.TechnicalPath;
		}
		if (LooksLikeResourceHint(hint, hintString))
		{
			reason = "technical:resource-hint";
			return XWInlineTextRole.TechnicalBinding;
		}
		if (ContainsAny(text, DialogueTokens) || (text == "text" && ownerType.Contains("Talk", StringComparison.OrdinalIgnoreCase)))
		{
			reason = "semantic:dialogue";
			return XWInlineTextRole.Dialogue;
		}
		bool flag = ContainsAny(text, DescriptionTokens);
		if (!flag)
		{
			bool flag2 = ((text == "text" || text == "content") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			reason = "semantic:description";
			return XWInlineTextRole.Description;
		}
		if (ContainsAny(text, HudTokens))
		{
			reason = "semantic:hud";
			return XWInlineTextRole.HudMessage;
		}
		if (ContainsAny(text, ButtonTokens))
		{
			reason = "semantic:button";
			return XWInlineTextRole.ButtonLabel;
		}
		if (ContainsAny(text, TitleTokens) || IsVisibleName(propertyName))
		{
			reason = "semantic:title";
			return XWInlineTextRole.Title;
		}
		if (ContainsAny(text, TechnicalBindingTokens))
		{
			reason = "technical:binding";
			return XWInlineTextRole.TechnicalBinding;
		}
		if (ContainsAny(text, TechnicalKeyTokens))
		{
			reason = "technical:key";
			return XWInlineTextRole.TechnicalKey;
		}
		reason = "technical:unclassified-scalar";
		return XWInlineTextRole.TechnicalKey;
	}

	private static bool IsAuthorTextProperty(string propertyName, Variant.Type type, PropertyUsageFlags usage)
	{
		bool flag = ((type == Variant.Type.String || (ulong)(type - 21) <= 1uL) ? true : false);
		bool flag2 = flag && (usage & PropertyUsageFlags.Editor) != PropertyUsageFlags.None && (usage & PropertyUsageFlags.ReadOnly) == 0;
		if (flag2)
		{
			bool flag3;
			switch (propertyName)
			{
			case "script":
			case "resource_name":
			case "resource_path":
			case "resource_local_to_scene":
				flag3 = true;
				break;
			default:
				flag3 = false;
				break;
			}
			flag2 = !flag3;
		}
		if (flag2 && !propertyName.StartsWith("_", StringComparison.Ordinal))
		{
			return !propertyName.StartsWith("metadata/_", StringComparison.Ordinal);
		}
		return false;
	}

	private static bool IsVisibleName(string propertyName)
	{
		if (!propertyName.Equals("name", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		return !ContainsAny(Normalize(propertyName), TechnicalNamePrefixes);
	}

	private static bool LooksLikeResourceHint(PropertyHint hint, string hintString)
	{
		if (((ulong)(hint - 13) <= 3uL) ? true : false)
		{
			return true;
		}
		string text = Normalize(hintString);
		if (!text.Contains("resource", StringComparison.Ordinal) && !text.Contains("packedscene", StringComparison.Ordinal) && !text.Contains("texture", StringComparison.Ordinal))
		{
			return text.Contains("audio", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool TryResolveHintBaseType(PropertyHint hint, string hintString, out string baseType)
	{
		baseType = "";
		string text = Normalize(hintString);
		if (hint == PropertyHint.ResourceType && !string.IsNullOrWhiteSpace(hintString))
		{
			baseType = hintString.Split(',')[0].Trim();
			return !string.IsNullOrWhiteSpace(baseType);
		}
		bool flag = text.Contains("packedscene", StringComparison.Ordinal) || text.Contains("tscn", StringComparison.Ordinal) || text.Contains("scn", StringComparison.Ordinal);
		bool flag2 = text.Contains("tres", StringComparison.Ordinal) || (hintString ?? "").Contains(".res", StringComparison.OrdinalIgnoreCase) || text.Contains("resource", StringComparison.Ordinal);
		if (flag && flag2)
		{
			baseType = "Resource";
		}
		else if (flag)
		{
			baseType = "PackedScene";
		}
		else if (text.Contains("texture", StringComparison.Ordinal) || text.Contains("png", StringComparison.Ordinal) || text.Contains("jpg", StringComparison.Ordinal) || text.Contains("webp", StringComparison.Ordinal) || text.Contains("svg", StringComparison.Ordinal))
		{
			baseType = "Texture2D";
		}
		else if (text.Contains("audio", StringComparison.Ordinal) || text.Contains("wav", StringComparison.Ordinal) || text.Contains("ogg", StringComparison.Ordinal) || text.Contains("mp3", StringComparison.Ordinal))
		{
			baseType = "AudioStream";
		}
		else if (text.Contains("script", StringComparison.Ordinal) || text.Contains("cs", StringComparison.Ordinal))
		{
			baseType = "Script";
		}
		else if (flag2)
		{
			baseType = "Resource";
		}
		return !string.IsNullOrWhiteSpace(baseType);
	}

	private static bool ContainsAny(string value, IEnumerable<string> tokens)
	{
		foreach (string token in tokens)
		{
			if (value.Contains(Normalize(token), StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static string Normalize(string value)
	{
		return (value ?? "").Replace("_", "", StringComparison.Ordinal).Replace("-", "", StringComparison.Ordinal).Replace("/", "", StringComparison.Ordinal)
			.ToLowerInvariant();
	}

	private static string Humanize(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "文字";
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length + 8);
		for (int i = 0; i < value.Length; i++)
		{
			char c = value[i];
			if ((c == '-' || c == '/' || c == '_') ? true : false)
			{
				stringBuilder.Append(' ');
				continue;
			}
			if (i > 0 && char.IsUpper(c) && char.IsLower(value[i - 1]))
			{
				stringBuilder.Append(' ');
			}
			stringBuilder.Append(c);
		}
		return stringBuilder.ToString().Trim();
	}
}
