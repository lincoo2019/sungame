using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModProjectLayout
{
	private sealed class DropRule
	{
		public static readonly DropRule Blocked = new DropRule(Array.Empty<string>());

		public string[] Extensions { get; }

		public string[] ResourceMarkers { get; }

		public bool RequireKnownResourceKind { get; }

		public string[] PackageExtensions { get; }

		public DropRule(string[] extensions, string[] resourceMarkers = null, bool requireKnownResourceKind = false, string[] packageExtensions = null)
		{
			Extensions = extensions ?? Array.Empty<string>();
			ResourceMarkers = resourceMarkers ?? Array.Empty<string>();
			RequireKnownResourceKind = requireKnownResourceKind;
			PackageExtensions = packageExtensions ?? Array.Empty<string>();
		}
	}

	private static readonly string[] StandardDirectories = new string[72]
	{
		"Scenes", "Scripts", "Battle", "Battle/Features", "Battle/Processes", "Battle/Components", "Resources", "Resources/Levels", "Resources/LevelCatalogs", "Resources/Maps",
		"Resources/MapCells", "Resources/GameplayLogic", "Resources/GameplayLogic/Waves", "Resources/StateMachines", "Resources/StateMachines/Conditions", "Resources/CharacterComponents", "Resources/CharacterCombat", "Resources/CharacterCombat/Attacks", "Resources/CharacterCombat/Buffs", "Resources/CharacterCombat/Events",
		"Resources/CharacterData", "Resources/CharacterData/Armor", "Resources/CharacterData/Custom", "Resources/CharacterData/DamagePoints", "Resources/BuffVisuals", "Resources/CollisionGeometry", "Resources/AwardSettlements", "Resources/UnlockConditions", "Resources/PacketEvents", "Resources/PacketSpawnEntries",
		"Resources/PacketSpawnEntries/Level", "Resources/PacketSpawnEntries/Conveyor", "Resources/PacketSpawnEntries/Rain", "Resources/ConveyorEvents", "Resources/ToolEvents", "Resources/Cards", "Resources/CardCostRules", "Resources/CardOverrides", "Resources/PacketBank", "Resources/Projectiles",
		"Resources/ProjectileChanges", "Resources/Characters", "Resources/Characters/Plants", "Resources/Characters/Zombies", "Resources/Characters/Props", "Resources/Characters/Vases", "Resources/Characters/Mowers", "Resources/Characters/Items", "Resources/Characters/Graves", "Resources/Characters/Craters",
		"Resources/Collectables", "Resources/DropItems", "Resources/Mowers", "Resources/Shovels", "Resources/FallingObjects", "Resources/Animations", "Resources/AnimationAtlasProfiles", "Resources/BGMConfigs", "Resources/Survivals", "Resources/Tutorials",
		"Resources/Tutorials/Conditions", "Resources/Tutorials/Steps", "Resources/NpcTalks", "Resources/Shops", "Resources/Dialogs", "Assets", "Assets/Images", "Assets/Audio", "Assets/Audio/Sfx", "Assets/Audio/BGM",
		"Assets/Fonts", "Localization"
	};

	private static readonly HashSet<string> ProtectedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, string> DisplayNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

	private static readonly Dictionary<string, DropRule> DropRules = new Dictionary<string, DropRule>(StringComparer.OrdinalIgnoreCase)
	{
		["Scenes"] = new DropRule(new string[2] { "tscn", "scn" }),
		["Scripts"] = new DropRule(new string[3] { "cs", "tres", "res" }, new string[1] { "XWBPScript" }),
		["Battle"] = DropRule.Blocked,
		["Battle/Features"] = new DropRule(new string[3] { "cs", "tres", "res" }, new string[1] { "XWBPScript" }),
		["Battle/Processes"] = new DropRule(new string[3] { "cs", "tres", "res" }, new string[1] { "XWBPScript" }),
		["Battle/Components"] = new DropRule(new string[3] { "cs", "tres", "res" }, new string[1] { "XWBPScript" }),
		["Resources"] = DropRule.Blocked,
		["Resources/Levels"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseLevelNewConfig" }, requireKnownResourceKind: true),
		["Resources/LevelCatalogs"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "LevelCatalogConfig" }),
		["Resources/Maps"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseMapConfig" }),
		["Resources/MapCells"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseCellConfig" }),
		["Resources/GameplayLogic"] = DropRule.Blocked,
		["Resources/GameplayLogic/Waves"] = new DropRule(new string[2] { "tres", "res" }, new string[7] { "TowerDefenseLevelWaveManagerConfig", "TowerDefenseLevelWaveConfig", "TowerDefenseLevelSpawnConfig", "TowerDefenseLevelGridSpawnConfig", "TowerDefenseLevelPreSpawnConfig", "TowerDefenseLevelSpawnDynamicConfig", "TowerDefenseLevelDynamicConfig" }, requireKnownResourceKind: true),
		["Resources/StateMachines"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "StateMachineDefinition" }),
		["Resources/StateMachines/Conditions"] = new DropRule(new string[2] { "tres", "res" }, new string[6] { "StateMachineGuardDefinition", "ExpressionGuard", "StateIsActiveGuard", "AllOfGuard", "AnyOfGuard", "NotGuard" }),
		["Resources/CharacterComponents"] = new DropRule(new string[2] { "tres", "res" }, new string[2] { "CharacterComponentSet", "CharacterComponentDefinition" }, requireKnownResourceKind: false, new string[3] { "cs", "tres", "res" }),
		["Resources/CharacterCombat"] = DropRule.Blocked,
		["Resources/CharacterCombat/Attacks"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "AttackConfig" }),
		["Resources/CharacterCombat/Buffs"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseCharacterBuff" }),
		["Resources/CharacterCombat/Events"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseCharacterEvent" }),
		["Resources/CharacterData"] = DropRule.Blocked,
		["Resources/CharacterData/Armor"] = new DropRule(new string[2] { "tres", "res" }, new string[3] { "CharacterArmorData", "ArmorSlotConfig", "TowerDefenseArmorTypeData" }),
		["Resources/CharacterData/Custom"] = new DropRule(new string[2] { "tres", "res" }, new string[2] { "CharacterCustomData", "CharacterCustomConfig" }),
		["Resources/CharacterData/DamagePoints"] = new DropRule(new string[2] { "tres", "res" }, new string[2] { "CharacterDamagePointData", "CharacterDamagePointConfig" }),
		["Resources/BuffVisuals"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "BuffVisualDefinition" }),
		["Resources/CollisionGeometry"] = new DropRule(new string[2] { "tres", "res" }, new string[3] { "CharacterHitBoxDefinition", "AabbShape2DResource", "AabbRay2DResource" }),
		["Resources/AwardSettlements"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "AwardSettlementConfig" }),
		["Resources/UnlockConditions"] = new DropRule(new string[2] { "tres", "res" }, new string[5] { "UnlockConditionBaseConfig", "UnlockConditionLevelFinishConfig", "UnlockConditionLevelSurvivalRoundConfig", "UnlockConditionPacketUnlockConfig", "UnlockConditionPacketBankCategoryPacketUnlockNumConfig" }),
		["Resources/PacketEvents"] = new DropRule(new string[2] { "tres", "res" }, new string[5] { "CardActionBehaviorDefinition", "CardActionBehaviorChangeCost", "CardActionBehaviorSetCooldown", "CardActionBehaviorChangePacket", "CardActionBehaviorDelete" }),
		["Resources/PacketSpawnEntries"] = DropRule.Blocked,
		["Resources/PacketSpawnEntries/Level"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseLevelPacketConfig" }),
		["Resources/PacketSpawnEntries/Conveyor"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseConveyorPacketConfig" }),
		["Resources/PacketSpawnEntries/Rain"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseRainModePacketConfig" }),
		["Resources/ConveyorEvents"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseConveyorEventAddPacket" }),
		["Resources/ToolEvents"] = new DropRule(new string[2] { "tres", "res" }, new string[11]
		{
			"MowerEventConfig", "MowerEventCreateSunConfig", "ShovelEventConfig", "ShovelEventCaptainShovelConfig", "ShovelEventCreateProjectileConfig", "ShovelEventDestroyConfig", "ShovelEventJalapenoConfig", "ShovelEventPacketSpawnConfig", "ShovelEventPumpkinShovelConfig", "ShovelEventRecycleConfig",
			"ShovelEventSkeletonShovelConfig"
		}),
		["Resources/Cards"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefensePacketConfig" }),
		["Resources/CardCostRules"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefensePacketChangeCost" }),
		["Resources/CardOverrides"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefensePacketOverride" }),
		["Resources/PacketBank"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefensePacketBankData" }),
		["Resources/Projectiles"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseProjectileData" }),
		["Resources/ProjectileChanges"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "ProjectileChange" }),
		["Resources/Characters"] = DropRule.Blocked,
		["Resources/Characters/Plants"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefensePlantConfig" }),
		["Resources/Characters/Zombies"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseZombieConfig" }),
		["Resources/Characters/Props"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseItemConfig" }),
		["Resources/Characters/Vases"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseVaseConfig" }),
		["Resources/Characters/Mowers"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseMowerConfig" }),
		["Resources/Characters/Items"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseItemConfig" }),
		["Resources/Characters/Graves"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseGravestoneConfig" }),
		["Resources/Characters/Craters"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseCraterConfig" }),
		["Resources/Collectables"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "Collectable" }),
		["Resources/DropItems"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "DropItemConfig" }),
		["Resources/Mowers"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "MowerConfig" }),
		["Resources/Shovels"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "ShovelConfig" }),
		["Resources/FallingObjects"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "FallingObject" }),
		["Resources/Animations"] = new DropRule(new string[3] { "tres", "res", "dat" }, new string[1] { "AdobeAnimateData" }),
		["Resources/AnimationAtlasProfiles"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "AdobeAnimateAtlasProfile" }),
		["Resources/BGMConfigs"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseBackgroundMusicConfig" }),
		["Resources/Survivals"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TowerDefenseLevelSurvivalConfig" }),
		["Resources/Tutorials"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TutorialConfig" }),
		["Resources/Tutorials/Conditions"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TutorialConditionConfig" }),
		["Resources/Tutorials/Steps"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "TutorialStepConfig" }),
		["Resources/NpcTalks"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "NpcTalkConfig" }),
		["Resources/Shops"] = new DropRule(new string[2] { "tres", "res" }, new string[1] { "ShopConfig" }),
		["Resources/Dialogs"] = new DropRule(new string[4] { "tres", "res", "tscn", "scn" }, new string[3] { "DialogBoxBase", "Dialog", "Control" }),
		["Assets"] = new DropRule(new string[1] { "*" }),
		["Assets/Images"] = new DropRule(new string[7] { "png", "jpg", "jpeg", "webp", "svg", "bmp", "tga" }),
		["Assets/Audio"] = DropRule.Blocked,
		["Assets/Audio/Sfx"] = new DropRule(new string[4] { "wav", "ogg", "mp3", "flac" }),
		["Assets/Audio/BGM"] = new DropRule(new string[4] { "wav", "ogg", "mp3", "flac" }),
		["Assets/Fonts"] = new DropRule(new string[4] { "ttf", "otf", "woff", "woff2" }),
		["Localization"] = new DropRule(new string[1] { "csv" })
	};

	static XWModProjectLayout()
	{
		string[] standardDirectories = StandardDirectories;
		foreach (string path in standardDirectories)
		{
			ProtectedDirectories.Add(NormalizeRelativePath(path));
		}
		AddDisplay("Resources/LevelCatalogs", "关卡选择目录");
		AddDisplay("Resources/PacketSpawnEntries", "卡牌生成条目");
		AddDisplay("Resources/PacketSpawnEntries/Level", "选卡条目");
		AddDisplay("Resources/PacketSpawnEntries/Conveyor", "传送带条目");
		AddDisplay("Resources/PacketSpawnEntries/Rain", "天降卡牌条目");
		AddDisplay("Resources/ConveyorEvents", "传送带波次事件");
		AddDisplay("Resources/UnlockConditions", "解锁条件");
		AddDisplay("Resources/PacketEvents", "卡牌事件");
		AddDisplay("Resources/ToolEvents", "工具事件");
		AddDisplay("Scenes", "场景");
		AddDisplay("Scripts", "脚本");
		AddDisplay("Battle", "战斗");
		AddDisplay("Battle/Features", "关卡功能");
		AddDisplay("Battle/Processes", "关卡流程");
		AddDisplay("Battle/Components", "旧版组件脚本（兼容）");
		AddDisplay("Resources", "资源");
		AddDisplay("Resources/Levels", "关卡");
		AddDisplay("Resources/Maps", "地图");
		AddDisplay("Resources/MapCells", "地图格子画笔");
		AddDisplay("Resources/GameplayLogic", "游戏逻辑");
		AddDisplay("Resources/GameplayLogic/Waves", "波次与生成");
		AddDisplay("Resources/StateMachines", "状态机");
		AddDisplay("Resources/StateMachines/Conditions", "状态切换条件");
		AddDisplay("Resources/CharacterComponents", "角色组件");
		AddDisplay("Resources/CharacterCombat", "角色战斗");
		AddDisplay("Resources/CharacterCombat/Attacks", "攻击配置");
		AddDisplay("Resources/CharacterCombat/Buffs", "角色状态");
		AddDisplay("Resources/CharacterCombat/Events", "角色事件");
		AddDisplay("Resources/CharacterData", "角色数据");
		AddDisplay("Resources/CharacterData/Armor", "护甲数据");
		AddDisplay("Resources/CharacterData/Custom", "角色外观");
		AddDisplay("Resources/CharacterData/DamagePoints", "受伤点");
		AddDisplay("Resources/BuffVisuals", "状态外观");
		AddDisplay("Resources/CollisionGeometry", "碰撞几何");
		AddDisplay("Resources/AwardSettlements", "奖励结算画面");
		AddDisplay("Resources/Cards", "卡片");
		AddDisplay("Resources/CardCostRules", "卡牌费用规则");
		AddDisplay("Resources/CardOverrides", "卡牌覆盖规则");
		AddDisplay("Resources/PacketBank", "卡牌库");
		AddDisplay("Resources/Projectiles", "子弹");
		AddDisplay("Resources/ProjectileChanges", "子弹变化");
		AddDisplay("Resources/Characters", "角色");
		AddDisplay("Resources/Characters/Plants", "植物");
		AddDisplay("Resources/Characters/Zombies", "僵尸");
		AddDisplay("Resources/Characters/Props", "道具");
		AddDisplay("Resources/Characters/Vases", "花瓶");
		AddDisplay("Resources/Characters/Mowers", "小推车");
		AddDisplay("Resources/Characters/Items", "物品");
		AddDisplay("Resources/Characters/Graves", "墓碑");
		AddDisplay("Resources/Characters/Craters", "弹坑");
		AddDisplay("Resources/Collectables", "收集物");
		AddDisplay("Resources/DropItems", "战场掉落物");
		AddDisplay("Resources/Mowers", "小推车配置");
		AddDisplay("Resources/Shovels", "铲子");
		AddDisplay("Resources/FallingObjects", "掉落物");
		AddDisplay("Resources/Animations", "动画配置");
		AddDisplay("Resources/AnimationAtlasProfiles", "动画图集配置");
		AddDisplay("Resources/BGMConfigs", "背景音乐配置");
		AddDisplay("Resources/Survivals", "生存模式");
		AddDisplay("Resources/Tutorials", "教程");
		AddDisplay("Resources/Tutorials/Conditions", "教程条件");
		AddDisplay("Resources/Tutorials/Steps", "教程步骤");
		AddDisplay("Resources/NpcTalks", "非玩家角色对话");
		AddDisplay("Resources/Shops", "商店");
		AddDisplay("Resources/Dialogs", "游戏界面");
		AddDisplay("Assets", "素材");
		AddDisplay("Assets/Images", "图片");
		AddDisplay("Assets/Audio", "音频");
		AddDisplay("Assets/Audio/Sfx", "音效");
		AddDisplay("Assets/Audio/BGM", "背景音乐音频");
		AddDisplay("Assets/Fonts", "字体");
		AddDisplay("Localization", "多语言");
	}

	public static IReadOnlyList<string> GetStandardDirectories()
	{
		return StandardDirectories;
	}

	public static void EnsureProjectLayout(string projectPath)
	{
		if (!string.IsNullOrWhiteSpace(projectPath))
		{
			Directory.CreateDirectory(projectPath);
			string[] standardDirectories = StandardDirectories;
			foreach (string text in standardDirectories)
			{
				Directory.CreateDirectory(Path.Combine(projectPath, text.Replace('/', Path.DirectorySeparatorChar)));
			}
		}
	}

	public static bool IsProtectedDirectory(string path, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(projectPath))
		{
			return false;
		}
		bool flag = Directory.Exists(path);
		bool flag2 = File.Exists(path);
		if (!flag && !flag2)
		{
			return false;
		}
		string text = ToProjectRelativePath(path, projectPath);
		if (text == ".")
		{
			return true;
		}
		if (!ProtectedDirectories.Contains(text) && !IsProtectedCharacterPackageSubdirectory(text, flag))
		{
			return IsProtectedCharacterPackageBaseFile(text, flag2);
		}
		return true;
	}

	private static bool IsProtectedCharacterPackageSubdirectory(string relative, bool pathIsDirectory)
	{
		if (!pathIsDirectory || string.IsNullOrWhiteSpace(relative) || !relative.StartsWith("Resources/Characters/", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = NormalizeRelativePath(relative).Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length < 5)
		{
			return false;
		}
		if (!string.Equals(array[0], "Resources", StringComparison.OrdinalIgnoreCase) || !string.Equals(array[1], "Characters", StringComparison.OrdinalIgnoreCase) || !IsCharacterCategoryFolder(array[2]) || string.IsNullOrWhiteSpace(array[3]))
		{
			return false;
		}
		return true;
	}

	private static bool IsProtectedCharacterPackageBaseFile(string relative, bool pathIsFile)
	{
		if (!pathIsFile || string.IsNullOrWhiteSpace(relative) || !relative.StartsWith("Resources/Characters/", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string[] array = NormalizeRelativePath(relative).Split('/', StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 6)
		{
			return false;
		}
		if (!string.Equals(array[0], "Resources", StringComparison.OrdinalIgnoreCase) || !string.Equals(array[1], "Characters", StringComparison.OrdinalIgnoreCase) || !IsCharacterCategoryFolder(array[2]) || string.IsNullOrWhiteSpace(array[3]))
		{
			return false;
		}
		string text = array[3];
		string text2 = array[4];
		string text3 = array[5];
		if (text2.Equals("Scene", StringComparison.OrdinalIgnoreCase))
		{
			return text3.Equals(text + ".tscn", StringComparison.OrdinalIgnoreCase);
		}
		if (text2.Equals("Config", StringComparison.OrdinalIgnoreCase))
		{
			return text3.Equals(text + "Config.tres", StringComparison.OrdinalIgnoreCase);
		}
		if (text2.Equals("Script", StringComparison.OrdinalIgnoreCase))
		{
			if (!text3.Equals(text + ".cs", StringComparison.OrdinalIgnoreCase))
			{
				return text3.Equals(text + ".cs.uid", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		if (text2.Equals("Sprite", StringComparison.OrdinalIgnoreCase))
		{
			if (!text3.Equals(text + ".tscn", StringComparison.OrdinalIgnoreCase) && !text3.Equals(text + ".tres", StringComparison.OrdinalIgnoreCase))
			{
				return text3.Equals(text + ".dat", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		if (text2.Equals("DamagePoint", StringComparison.OrdinalIgnoreCase))
		{
			return text3.Equals("DamagePointData.tres", StringComparison.OrdinalIgnoreCase);
		}
		if (text2.Equals("Custom", StringComparison.OrdinalIgnoreCase))
		{
			return text3.Equals("CustomData.tres", StringComparison.OrdinalIgnoreCase);
		}
		if (text2.Equals("Armor", StringComparison.OrdinalIgnoreCase))
		{
			return text3.Equals("ArmorData.tres", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsCharacterCategoryFolder(string segment)
	{
		if (segment != null)
		{
			if (!segment.Equals("Plants", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Zombies", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Props", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Vases", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Mowers", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Items", StringComparison.OrdinalIgnoreCase) && !segment.Equals("Graves", StringComparison.OrdinalIgnoreCase))
			{
				return segment.Equals("Craters", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
		return false;
	}

	public static string GetResourceFolder(string category, string subcategory = "")
	{
		string text = NormalizeKey(category);
		string text2 = NormalizeKey(subcategory);
		if ((text == "character" || text == "characters") ? true : false)
		{
			switch (text2)
			{
			case "zombie":
			case "zombies":
				return "Resources/Characters/Zombies";
			case "prop":
			case "props":
				return "Resources/Characters/Props";
			case "vase":
			case "vases":
				return "Resources/Characters/Vases";
			case "mowers":
			case "mower":
				return "Resources/Characters/Mowers";
			case "item":
			case "items":
				return "Resources/Characters/Items";
			case "graves":
			case "grave":
				return "Resources/Characters/Graves";
			case "crater":
			case "craters":
				return "Resources/Characters/Craters";
			default:
				return "Resources/Characters/Plants";
			}
		}
		switch (text)
		{
		case "level":
		case "levels":
			return "Resources/Levels";
		case "levelcatalog":
		case "level_catalog":
		case "levelcatalogs":
		case "level_catalogs":
			return "Resources/LevelCatalogs";
		case "map":
		case "maps":
			return "Resources/Maps";
		case "mapcell":
		case "map_cell":
		case "mapcells":
		case "map_cells":
			return "Resources/MapCells";
		case "waves":
		case "spawn":
		case "spawns":
		case "wave_manager":
		case "gameplaylogic":
		case "gameplay_logic":
		case "wave":
		case "wavemanager":
			return "Resources/GameplayLogic/Waves";
		case "statemachine":
		case "state_machine":
		case "statemachines":
		case "state_machines":
			return "Resources/StateMachines";
		case "state_guards":
		case "state_guard":
		case "stateguards":
		case "stateguard":
		case "statemachinecondition":
		case "state_machine_condition":
			return "Resources/StateMachines/Conditions";
		case "charactercomponent":
		case "character_component":
		case "charactercomponents":
		case "character_components":
			return "Resources/CharacterComponents";
		case "charactercombat":
		case "character_combat":
			return "Resources/CharacterCombat/Attacks";
		case "attack":
		case "attackconfig":
		case "attack_config":
		case "attacks":
			return "Resources/CharacterCombat/Attacks";
		case "characterbuff":
		case "character_buff":
		case "characterbuffs":
		case "character_buffs":
			return "Resources/CharacterCombat/Buffs";
		case "characterevent":
		case "character_event":
		case "characterevents":
		case "character_events":
			return "Resources/CharacterCombat/Events";
		case "characterdata":
		case "character_data":
			return "Resources/CharacterData/Armor";
		case "armordata":
		case "armorslot":
		case "armor_data":
		case "armor_slot":
			return "Resources/CharacterData/Armor";
		case "custom_data":
		case "customdata":
		case "charactercustom":
		case "character_custom":
			return "Resources/CharacterData/Custom";
		case "damagepoints":
		case "damage_point":
		case "damage_points":
		case "damagepoint":
			return "Resources/CharacterData/DamagePoints";
		case "buff_visuals":
		case "buff_visual":
		case "buffvisuals":
		case "buffvisual":
			return "Resources/BuffVisuals";
		case "collision_geometry":
		case "collisiongeometries":
		case "collision_geometries":
		case "collisiongeometry":
			return "Resources/CollisionGeometry";
		case "awardsettlement":
		case "award_settlement":
		case "awardsettlements":
		case "award_settlements":
			return "Resources/AwardSettlements";
		case "unlockcondition":
		case "unlock_condition":
		case "unlockconditions":
		case "unlock_conditions":
			return "Resources/UnlockConditions";
		case "packet_event":
		case "packetevents":
		case "packet_events":
		case "packetevent":
			return "Resources/PacketEvents";
		case "packetspawnentries":
		case "packet_spawn_entry":
		case "packet_spawn_entries":
		case "packetspawnentry":
			return "Resources/PacketSpawnEntries/Level";
		case "conveyorevent":
		case "conveyor_event":
		case "conveyorevents":
		case "conveyor_events":
			return "Resources/ConveyorEvents";
		case "toolevent":
		case "tool_events":
		case "tool_event":
		case "toolevents":
			return "Resources/ToolEvents";
		case "cards":
		case "card":
			return "Resources/Cards";
		case "cardcostrule":
		case "card_cost_rule":
		case "packetcostrule":
		case "packet_cost_rule":
			return "Resources/CardCostRules";
		case "cardoverride":
		case "card_override":
		case "packetoverride":
		case "packet_override":
			return "Resources/CardOverrides";
		case "packet_bank":
		case "packetbank":
			return "Resources/PacketBank";
		case "feature":
		case "features":
			return "Battle/Features";
		case "process":
		case "processes":
			return "Battle/Processes";
		case "component":
		case "components":
			return "Battle/Components";
		case "projectiles":
		case "projectile":
			return "Resources/Projectiles";
		case "projectilechange":
		case "projectilechanges":
		case "projectile_change":
			return "Resources/ProjectileChanges";
		case "collectables":
		case "collectable":
			return "Resources/Collectables";
		case "dropitem":
		case "dropitems":
		case "drop_item":
			return "Resources/DropItems";
		case "mower":
		case "mowers":
			return "Resources/Mowers";
		case "shovel":
		case "shovels":
			return "Resources/Shovels";
		case "fallingobject":
		case "falling_object":
		case "fallingobjects":
			return "Resources/FallingObjects";
		case "animation":
		case "animations":
			return "Resources/Animations";
		case "bgmconfig":
		case "bgmconfigs":
			return "Resources/BGMConfigs";
		case "survival":
		case "survivals":
			return "Resources/Survivals";
		case "tutorial":
		case "tutorials":
			return "Resources/Tutorials";
		case "npctalk":
		case "npctalks":
		case "npc_talk":
			return "Resources/NpcTalks";
		case "shops":
		case "shop":
			return "Resources/Shops";
		case "dialog":
		case "gui":
		case "dialogs":
		case "ui":
			return "Resources/Dialogs";
		case "image":
		case "images":
		case "texture":
		case "textures":
			return "Assets/Images";
		case "scene":
		case "scenes":
			return "Scenes";
		case "fonts":
		case "font":
			return "Assets/Fonts";
		case "shader":
		case "shaders":
			return "Assets";
		default:
			return "Resources";
		}
	}

	public static string GetAssetFolderForExtension(string extension, string preferredKind = "")
	{
		string text = NormalizeKey(preferredKind);
		if ((text == "bgm" || text == "music") ? true : false)
		{
			return "Assets/Audio/BGM";
		}
		if ((text == "sfx" || text == "audio") ? true : false)
		{
			return "Assets/Audio/Sfx";
		}
		bool flag;
		switch (text)
		{
		case "image":
		case "images":
		case "texture":
		case "textures":
			flag = true;
			break;
		default:
			flag = false;
			break;
		}
		if (flag)
		{
			return "Assets/Images";
		}
		if ((text == "font" || text == "fonts") ? true : false)
		{
			return "Assets/Fonts";
		}
		switch ((extension ?? "").Trim().TrimStart('.').ToLowerInvariant())
		{
		case "png":
		case "jpg":
		case "svg":
		case "bmp":
		case "tga":
		case "jpeg":
		case "webp":
			return "Assets/Images";
		case "ttf":
		case "otf":
		case "woff":
		case "woff2":
			return "Assets/Fonts";
		case "ogg":
		case "wav":
		case "mp3":
		case "flac":
			return "Assets/Audio/Sfx";
		default:
			return "Assets";
		}
	}

	public static bool IsDropAllowedInDirectory(string sourcePath, string targetDirectory, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || string.IsNullOrWhiteSpace(targetDirectory) || string.IsNullOrWhiteSpace(projectPath))
		{
			return false;
		}
		string text = ToProjectRelativePath(targetDirectory, projectPath);
		string dropTargetRuleRoot = GetDropTargetRuleRoot(text);
		if (string.IsNullOrEmpty(dropTargetRuleRoot))
		{
			return true;
		}
		if (!DropRules.TryGetValue(dropTargetRuleRoot, out var value) || value.Extensions.Length == 0)
		{
			return false;
		}
		string[] allowedExtensions = value.Extensions;
		if (!string.Equals(text, dropTargetRuleRoot, StringComparison.OrdinalIgnoreCase))
		{
			if (!IsDirectPackageDirectory(text, dropTargetRuleRoot) || value.PackageExtensions.Length == 0)
			{
				return false;
			}
			allowedExtensions = value.PackageExtensions;
		}
		if (Directory.Exists(sourcePath))
		{
			return false;
		}
		string text2 = Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant();
		if (!IsDropExtensionAllowed(allowedExtensions, text2))
		{
			return false;
		}
		if ((text2 == "tres" || text2 == "res") && value.ResourceMarkers.Length != 0)
		{
			string text3 = DetectResourceDropKind(sourcePath);
			if (string.IsNullOrEmpty(text3) && value.RequireKnownResourceKind)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(text3) && !IsResourceKindAllowed(text3, value.ResourceMarkers))
			{
				return false;
			}
		}
		return true;
	}

	public static string GetDropTargetRuleRoot(string relativePath)
	{
		string text = NormalizeRelativePath(relativePath);
		string text2 = "";
		foreach (string key in DropRules.Keys)
		{
			if ((string.Equals(text, key, StringComparison.OrdinalIgnoreCase) || text.StartsWith(key + "/", StringComparison.OrdinalIgnoreCase)) && key.Length > text2.Length)
			{
				text2 = key;
			}
		}
		return text2;
	}

	private static bool IsDirectPackageDirectory(string relativeTarget, string ruleRoot)
	{
		string text = ruleRoot.TrimEnd('/') + "/";
		if (!relativeTarget.StartsWith(text, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text2 = relativeTarget.Substring(text.Length);
		if (text2.Length > 0)
		{
			return !text2.Contains('/');
		}
		return false;
	}

	private static bool IsDropExtensionAllowed(string[] allowedExtensions, string extension)
	{
		foreach (string text in allowedExtensions)
		{
			if (text == "*" || string.Equals(text, extension, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsResourceKindAllowed(string resourceKind, string[] allowedMarkers)
	{
		foreach (string value in allowedMarkers)
		{
			if (resourceKind.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static string DetectResourceDropKind(string sourcePath)
	{
		try
		{
			if (!File.Exists(sourcePath))
			{
				return "";
			}
			if (Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant() != "tres")
			{
				return "";
			}
			using FileStream fileStream = File.OpenRead(sourcePath);
			int num = (int)Math.Min(fileStream.Length, 65536L);
			byte[] array = new byte[num];
			int count = fileStream.Read(array, 0, num);
			return Encoding.UTF8.GetString(array, 0, count);
		}
		catch
		{
			return "";
		}
	}

	public static string GetDisplayName(string relativePath)
	{
		string text = NormalizeRelativePath(relativePath);
		if (!DisplayNames.TryGetValue(text, out var value))
		{
			return Path.GetFileName(text);
		}
		return value;
	}

	public static string ToProjectRelativePath(string path, string projectPath)
	{
		string text = NormalizeAbsolutePath(path);
		string text2 = NormalizeAbsolutePath(projectPath);
		if (string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return ".";
		}
		try
		{
			return NormalizeRelativePath(Path.GetRelativePath(text2, text));
		}
		catch
		{
			return NormalizeRelativePath(path);
		}
	}

	public static bool IsInsideDirectory(string path, string projectPath, string relativeFolder)
	{
		string text = ToProjectRelativePath(path, projectPath);
		string text2 = NormalizeRelativePath(relativeFolder);
		if (!string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return text.StartsWith(text2 + "/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	public static bool MakeSavedTextResourceReferencesPortable(string savedPath, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(savedPath) || string.IsNullOrWhiteSpace(projectPath) || !File.Exists(savedPath))
		{
			return false;
		}
		string extension = Path.GetExtension(savedPath);
		if (!extension.Equals(".tres", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".tscn", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string path;
		string text;
		try
		{
			path = NormalizeAbsolutePath(savedPath);
			text = NormalizeAbsolutePath(projectPath);
		}
		catch
		{
			return false;
		}
		if (!IsAbsolutePathInsideProject(path, text))
		{
			return false;
		}
		string text2 = File.ReadAllText(path, Encoding.UTF8);
		string separator = (text2.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n");
		string[] array = text2.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		string relativeTo = Path.GetDirectoryName(path) ?? text;
		bool flag = false;
		for (int i = 0; i < array.Length; i++)
		{
			string text3 = array[i];
			if (!text3.TrimStart().StartsWith("[ext_resource", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			int num = text3.IndexOf("path=\"", StringComparison.OrdinalIgnoreCase);
			if (num < 0)
			{
				continue;
			}
			num += "path=\"".Length;
			int num2 = text3.IndexOf('"', num);
			if (num2 <= num)
			{
				continue;
			}
			string path2 = text3.Substring(num, num2 - num);
			if (Path.IsPathRooted(path2))
			{
				string path3;
				try
				{
					path3 = NormalizeAbsolutePath(path2);
				}
				catch
				{
					continue;
				}
				if (IsAbsolutePathInsideProject(path3, text))
				{
					string text4 = Path.GetRelativePath(relativeTo, path3).Replace('\\', '/');
					array[i] = text3.Substring(0, num) + text4 + text3.Substring(num2);
					flag = true;
				}
			}
		}
		if (flag)
		{
			File.WriteAllText(path, string.Join(separator, array), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
		}
		return flag;
	}

	private static bool IsAbsolutePathInsideProject(string path, string projectPath)
	{
		if (string.Equals(path, projectPath, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		string value = projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		return path.StartsWith(value, StringComparison.OrdinalIgnoreCase);
	}

	private static void AddDisplay(string path, string displayName)
	{
		DisplayNames[NormalizeRelativePath(path)] = displayName;
	}

	private static string NormalizeKey(string value)
	{
		return (value ?? "").Trim().Replace("-", "").Replace("_", "")
			.Replace(" ", "")
			.ToLowerInvariant();
	}

	private static string NormalizeAbsolutePath(string path)
	{
		return Path.GetFullPath(path.Replace('\\', Path.DirectorySeparatorChar).TrimEnd('/', '\\')).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private static string NormalizeRelativePath(string path)
	{
		string text = (path ?? "").Replace('\\', '/').Trim('/');
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return ".";
	}
}
