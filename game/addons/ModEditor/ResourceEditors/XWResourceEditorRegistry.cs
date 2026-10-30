using System.Collections.Generic;
using System.Text;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

namespace PVZHE.ModEditor.ResourceEditors;

public static class XWResourceEditorRegistry
{
	private static readonly List<XWVisualEditorDescriptor> Editors = new List<XWVisualEditorDescriptor>();

	private static bool _defaultsRegistered;

	public static void RegisterDefaultEditors()
	{
		if (!_defaultsRegistered)
		{
			_defaultsRegistered = true;
			Register(new XWVisualEditorDescriptor
			{
				Category = "Level",
				DisplayName = "关卡编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Grid,
				ResourceClassNames = { "TowerDefenseLevelConfig", "TowerDefenseLevelNewConfig" },
				PathMarkers = { "Asset/Config/Level", "/Level/", "Resources/Levels" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Map",
				DisplayName = "地图编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				ResourceClassNames = { "TowerDefenseMapConfig", "TowerDefenseMapRuleConfig", "TowerDefenseMapPacketRuleConfig", "TowerDefenseMapCharacterRuleConfig" },
				PathMarkers = { "Asset/Config/Map", "/Map/", "Resources/Maps" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "MapCell",
				DisplayName = "地图格子画笔",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				FamilyPriority = 125,
				ResourceClassNames = { "TowerDefenseCellConfig" },
				PathMarkers = { "/Map/Cell/", "/MapCells/", "Resources/MapCells" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Character",
				DisplayName = "角色编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "TowerDefenseCharacterConfig", "TowerDefenseCharacterOverride", "TowerDefenseCharacterPropertyChangeConfig" },
				PathMarkers = { "Asset/Config/Character", "/Character/", "Resources/Characters" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "CharacterCombat",
				DisplayName = "角色战斗资源编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "AttackConfig", "TowerDefenseCharacterBuffConfig", "TowerDefenseCharacterEventBase", "TowerDefenseCharacterEventLuckyDrawItem", "TowerDefenseCharacterEventHurt", "TowerDefenseCharacterEventHurtWithConfig", "TowerDefenseCharacterEventExplodeHurt", "TowerDefenseCharacterEventSmashHurt", "TowerDefenseCharacterEventBowlingHurt" },
				PathMarkers = { "Resource/TowerDefense/Attack", "/Character/Event/General/", "Resources/CharacterCombat/Attacks", "Resources/CharacterCombat/Buffs", "Resources/CharacterCombat/Events" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "CharacterData",
				DisplayName = "角色数据编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "CharacterArmorData", "CharacterCustomData", "CharacterDamagePointData", "ArmorSlotConfig", "CharacterCustomConfig", "CharacterDamagePointConfig", "TowerDefenseArmorTypeData" },
				PathMarkers = { "/Armor/ArmorData.tres", "/Custom/CustomData.tres", "/DamagePoint/DamagePointData.tres", "Resource/General/Character/Armor", "Resource/General/Character/Costom", "Resource/General/Character/DamagePoint", "Resources/CharacterData/Armor", "Resources/CharacterData/Custom", "Resources/CharacterData/DamagePoints" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "CharacterComponent",
				DisplayName = "角色组件拼图编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				DockKey = "character_component_editor",
				PathMarkers = { "Resources/CharacterComponents", "/CharacterComponents/" },
				ResourceClassNames = { "CharacterComponentSet", "CharacterComponentDefinition" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "BuffVisual",
				DisplayName = "BUFF 外观编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				FamilyPriority = 130,
				ResourceClassNames = { "BuffVisualDefinition" },
				PathMarkers = { "Resources/BuffVisuals", "/BuffVisuals/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "CollisionGeometry",
				DisplayName = "碰撞几何编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				FamilyPriority = 120,
				ResourceClassNames = { "CharacterHitBoxDefinition", "AabbShape2DResource", "AabbRay2DResource" },
				PathMarkers = { "Resource/TowerDefense/Collision", "/Collision/", "/CharacterHitBoxes/" }
			});
			Register("Card", "卡片编辑器", XWVisualEditorDescriptor.SurfaceKind.Preview, "TowerDefensePacketConfig", "Asset/Config/Packet", "/Packet/", "Resources/Cards");
			Register("PacketBank", "卡牌库编辑器", XWVisualEditorDescriptor.SurfaceKind.Grid, "TowerDefensePacketBankData", "Asset/Config/PacketBank", "/PacketBank/", "Resources/PacketBank");
			Register(new XWVisualEditorDescriptor
			{
				Category = "Projectile",
				DisplayName = "子弹编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				ResourceClassNames = { "TowerDefenseProjectileConfig", "TowerDefenseProjectileData", "TowerDefenseProjectileCreateData", "ProjectileBehaviorDefinition", "FireComponentCheckConfig", "FireComponentFireProjectileConfig", "FireComponentProjectileResource", "FireComponentProjectileSingle", "FireComponentProjectileWeight", "FireComponentProjectileWeightItem" },
				PathMarkers = { "Asset/Config/Projectile", "/Projectile/", "Resources/Projectiles" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "ProjectileChange",
				DisplayName = "子弹变化编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				FamilyPriority = 110,
				ResourceClassNames = { "ChangeProjectileConfig", "ChangeProjectileSingleConfig" },
				PathMarkers = { "Asset/Config/ProjectileChange", "/ProjectileChange/", "Resources/ProjectileChanges" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Collectable",
				DisplayName = "收集物编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "CollectableConfig", "AwardSettlementConfig" },
				PathMarkers = { "Asset/Config/Collectable", "/Collectable/", "Resources/Collectables" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "DropItem",
				DisplayName = "战场掉落物编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "DropItemConfig", "DropItemHandler" },
				PathMarkers = { "Registry/DropItem", "/DropItems/", "Resources/DropItems" }
			});
			Register("Mower", "小推车编辑器", XWVisualEditorDescriptor.SurfaceKind.Preview, "MowerConfig", "Asset/Config/Mower", "/Mower/", "Resources/Mowers");
			Register("Shovel", "铲子编辑器", XWVisualEditorDescriptor.SurfaceKind.Preview, "ShovelConfig", "Asset/Config/Shovel", "/Shovel/", "Resources/Shovels");
			Register(new XWVisualEditorDescriptor
			{
				Category = "FallingObject",
				DisplayName = "掉落物编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Canvas,
				ResourceClassNames = { "FallingObjectConfig", "FallingObjectWeightItemConfig" },
				PathMarkers = { "Asset/Config/FallingObject", "/FallingObject/", "Resources/FallingObjects" }
			});
			Register("Animation", "动画编辑器", XWVisualEditorDescriptor.SurfaceKind.Timeline, "AdobeAnimateData", "Asset/Anime", "/Anime/", "Resources/Animations");
			Register(new XWVisualEditorDescriptor
			{
				Category = "AnimationAtlas",
				DisplayName = "动画图集配置",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				FamilyPriority = 115,
				ResourceClassNames = { "AdobeAnimateAtlasProfile" },
				PathMarkers = { "Resources/AnimationAtlasProfiles", "/AnimationAtlasProfiles/" }
			});
			Register("Audio", "音频编辑器", XWVisualEditorDescriptor.SurfaceKind.Waveform, "AudioStream", "Asset/Config/Audio", "/Audio/", "Assets/Audio");
			Register("BGM", "BGM 编辑器", XWVisualEditorDescriptor.SurfaceKind.Timeline, "TowerDefenseBackgroundMusicConfig", "Asset/Config/BGM", "/BGM/", "Resources/BGMConfigs");
			Register(new XWVisualEditorDescriptor
			{
				Category = "Survival",
				DisplayName = "生存模式编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Timeline,
				ResourceClassNames = { "TowerDefenseLevelSurvivalConfig", "TowerDefenseLevelSurvivalZombiePoolRoundAddConfig" },
				PathMarkers = { "Asset/Config/Survival", "/Survival/", "Resources/Survivals" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Tutorial",
				DisplayName = "教程流程编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				ResourceClassNames = { "TutorialConfig", "TutorialStepConfig", "TutorialConditionConfig" },
				PathMarkers = { "Asset/Config/Tutorial", "/Tutorial/", "Resources/Tutorials" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "NpcTalk",
				DisplayName = "NPC 对话编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				ResourceClassNames = { "NpcTalkConfig", "NpcTalkBaseConfig" },
				PathMarkers = { "Asset/Config/Npc", "/Npc/", "Resources/NpcTalks" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Shop",
				DisplayName = "商店编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Grid,
				ResourceClassNames = { "ShopConfig", "ShopPageConfig", "ShopItemConfig", "ShopItemStageConfig" },
				PathMarkers = { "Asset/Config/Shop", "/Shop/", "Resources/Shops" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "Dialog",
				DisplayName = "GUI 编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Wysiwyg,
				AllowPathMatchForLoadedResources = true,
				PathMarkers = { "Prefab/GUI/DialogBox", "/DialogBox/", "Resources/Dialogs" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "GameplayLogic",
				DisplayName = "战斗逻辑",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Wysiwyg,
				ScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn",
				FamilyPriority = 100,
				ResourceClassNames = 
				{
					"TowerDefenseLevelEventBase", "TowerDefenseLevelWaveManagerConfig", "TowerDefenseLevelWaveConfig", "TowerDefenseLevelSpawnConfig", "TowerDefenseLevelGridSpawnConfig", "TowerDefenseLevelPreSpawnConfig", "TowerDefenseLevelSpawnDynamicConfig", "TowerDefenseLevelDynamicConfig", "TowerDefenseLevelSunManagerConfig", "TowerDefenseLevelFogManagerConfig",
					"TowerDefenseLevelLookStarManagerConfig", "TowerDefenseLevelLookStarCheckConfig", "TowerDefenseBattleFeatureProgressConfig", "TowerDefenseLevelSeedBankConfig", "TowerDefenseLevelPacketBankConfig", "TowerDefenseConveyorConfig", "TowerDefenseBattleFeatureMowerConfig", "TowerDefenseBattleFeatureBrainConfig", "TowerDefenseRainModeConfig", "TowerDefenseBattleFeaturePreSpawnConfig",
					"TowerDefenseBattleFeatureGemMatchConfig", "TowerDefenseBattleFeaturePacketPickConfig", "TowerDefenseLevelVaseManagerConfig", "TowerDefenseLevelVaseConfig", "TowerDefenseLevelVaseFillConfig", "TowerDefenseLevelIZMManagerConfig", "TowerDefenseBattleProcessWaveEntryConfig", "TowerDefenseBattleProcessWaveConfig", "TowerDefenseBattleProcessIZM2Config", "TowerDefenseBattleProcessQuizConfig",
					"TowerDefenseBattleFeature", "TowerDefenseBattleProcess", "BroadCastConfig"
				}
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "PacketCostRule",
				DisplayName = "卡牌费用规则编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "TowerDefensePacketChangeCost" },
				PathMarkers = { "Registry/Battle/Feature/PacketBank/Resource/Packet/ChangeCost", "/CardCostRules/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "PacketOverride",
				DisplayName = "卡牌覆盖规则编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "TowerDefensePacketOverride" },
				PathMarkers = { "Registry/Battle/Feature/PacketBank/Resource/Packet/Override", "/CardOverrides/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "PacketSpawnEntry",
				DisplayName = "关卡卡牌条目编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				FamilyPriority = 110,
				ResourceClassNames = { "TowerDefenseLevelPacketConfig", "TowerDefenseConveyorPacketConfig", "TowerDefenseRainModePacketConfig" },
				PathMarkers = { "/Packet/", "/Conveyor/", "/RainMode/", "Resources/PacketSpawnEntries" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "ConveyorEvent",
				DisplayName = "传送带波次事件编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				ScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWConveyorEventEditorPanel.tscn",
				FamilyPriority = 115,
				ResourceClassNames = { "TowerDefenseConveyorEventAddPacket" },
				PathMarkers = { "Registry/Battle/Feature/ConveyorBelt/Resource/Event", "Resources/ConveyorEvents" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "LevelCatalog",
				DisplayName = "关卡选择目录编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Wysiwyg,
				ScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWLevelCatalogEditorPanel.tscn",
				FamilyPriority = 120,
				ResourceClassNames = { "LevelCatalogConfig", "LevelChapterConfig", "LevelChooseConfig" },
				PathMarkers = { "Resource/Level", "Resources/LevelCatalogs" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "PacketEvent",
				DisplayName = "卡牌事件编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "CardActionBehaviorDefinition" },
				PathMarkers = { "Registry/Battle/Feature/PacketBank/Resource/Packet/Event", "/PacketEvents/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "ToolEvent",
				DisplayName = "工具事件编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "MowerEventConfig", "ShovelEventConfig" },
				PathMarkers = { "Registry/Battle/Feature/Mower/Resource/Event", "Registry/Battle/Feature/Shovel/Resource/Event", "/ToolEvents/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "UnlockCondition",
				DisplayName = "解锁条件编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ResourceClassNames = { "UnlockConditionBaseConfig", "UnlockConditionLevelFinishConfig", "UnlockConditionLevelSurvivalRoundConfig", "UnlockConditionPacketUnlockConfig", "UnlockConditionPacketBankCategoryPacketUnlockNumConfig" },
				PathMarkers = { "Resource/UnlockCondition", "/UnlockCondition/", "Resources/UnlockConditions" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "StateGuard",
				DisplayName = "状态切换条件拼图",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				FamilyPriority = 118,
				ResourceClassNames = { "StateMachineGuardDefinition", "ExpressionGuard", "StateIsActiveGuard", "AllOfGuard", "AnyOfGuard", "NotGuard" },
				PathMarkers = { "Resources/StateMachines/Conditions", "/StateMachine/Conditions/", "/StateGuards/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "StateMachine",
				DisplayName = "状态机图编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Graph,
				DockKey = "state_machine_editor",
				ResourceClassNames = { "StateMachineDefinition" },
				PathMarkers = { "Resources/StateMachines", "/StateMachines/", "/StateMachine/" }
			});
			Register(new XWVisualEditorDescriptor
			{
				Category = "General",
				DisplayName = "通用资源编辑器",
				Kind = XWVisualEditorDescriptor.SurfaceKind.Preview,
				ScenePath = "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn",
				IconPath = "res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg"
			});
		}
	}

	public static void Register(XWVisualEditorDescriptor descriptor)
	{
		if (descriptor != null && !string.IsNullOrWhiteSpace(descriptor.Category))
		{
			if (string.IsNullOrWhiteSpace(descriptor.DockKey))
			{
				descriptor.DockKey = ToDockKey(descriptor.Category);
			}
			if (string.IsNullOrWhiteSpace(descriptor.ScenePath) || descriptor.ScenePath == "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn")
			{
				descriptor.ScenePath = ToScenePath(descriptor.Category);
			}
			if (string.IsNullOrWhiteSpace(descriptor.IconPath))
			{
				descriptor.IconPath = ToIconPath(descriptor.Category);
			}
			Editors.RemoveAll((XWVisualEditorDescriptor e) => e.Category == descriptor.Category);
			int num = Editors.FindIndex((XWVisualEditorDescriptor existing) => existing.FamilyPriority < descriptor.FamilyPriority);
			if (num < 0)
			{
				Editors.Add(descriptor);
			}
			else
			{
				Editors.Insert(num, descriptor);
			}
		}
	}

	public static IReadOnlyList<XWVisualEditorDescriptor> GetAllEditors()
	{
		RegisterDefaultEditors();
		return new List<XWVisualEditorDescriptor>(Editors);
	}

	public static bool TryGetEditor(Resource resource, string path, out XWVisualEditorDescriptor descriptor)
	{
		RegisterDefaultEditors();
		bool flag = GodotObject.IsInstanceValid(resource);
		string className = resource?.GetType().Name ?? "";
		string className2 = (flag ? resource.GetClass() : "");
		foreach (XWVisualEditorDescriptor editor in Editors)
		{
			if (!(editor.Category == "General"))
			{
				bool num = (flag ? editor.MatchesResource(resource) : (editor.MatchesClass(className) || editor.MatchesClass(className2)));
				bool flag2 = editor.MatchesPath(path) && (!flag || editor.AllowPathMatchForLoadedResources);
				if (num || flag2)
				{
					descriptor = editor;
					return true;
				}
			}
		}
		if (GodotObject.IsInstanceValid(resource))
		{
			descriptor = Editors.Find((XWVisualEditorDescriptor editor) => editor.Category == "General");
			return descriptor != null;
		}
		descriptor = null;
		return false;
	}

	public static bool TryOpen(Resource resource, string path)
	{
		return TryOpen(resource, path, null);
	}

	public static bool TryOpen(Resource resource, string path, XWResourceEditContext context)
	{
		if (!TryGetEditor(resource, path, out var descriptor))
		{
			return false;
		}
		if (!(XWEditorInterface.Instance?.EnsureResourceEditor(descriptor.DockKey) is XWGenericVisualResourceEditor xWGenericVisualResourceEditor))
		{
			return false;
		}
		if (context == null)
		{
			context = XWResourceEditContext.ForRoot(resource, path, descriptor.DockKey);
		}
		xWGenericVisualResourceEditor.LoadResource(resource, path, descriptor, context);
		XWEditorInterface.Instance?.FocusPanel(descriptor.DockKey);
		return true;
	}

	public static bool TryOpenPath(string path)
	{
		Resource resource = null;
		if (ResourceLoader.Exists(path))
		{
			resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Reuse);
		}
		return TryOpen(resource, path);
	}

	private static void Register(string category, string displayName, XWVisualEditorDescriptor.SurfaceKind kind, string className, params string[] pathMarkers)
	{
		XWVisualEditorDescriptor xWVisualEditorDescriptor = new XWVisualEditorDescriptor
		{
			Category = category,
			DisplayName = displayName,
			Kind = kind
		};
		xWVisualEditorDescriptor.ResourceClassNames.Add(className);
		foreach (string item in pathMarkers)
		{
			xWVisualEditorDescriptor.PathMarkers.Add(item);
		}
		Register(xWVisualEditorDescriptor);
	}

	public static string ToDockKey(string category)
	{
		if (string.IsNullOrWhiteSpace(category))
		{
			return "resource_editor";
		}
		string text = category switch
		{
			"Level" => "level_editor", 
			"Map" => "map_editor", 
			"MapCell" => "map_cell_editor", 
			"Character" => "character_editor", 
			"CharacterCombat" => "character_combat_editor", 
			"CharacterData" => "character_data_editor", 
			"CharacterComponent" => "character_component_editor", 
			"BuffVisual" => "buff_visual_editor", 
			"Card" => "card_editor", 
			"PacketBank" => "packet_bank_editor", 
			"Projectile" => "projectile_editor", 
			"ProjectileChange" => "projectile_change_editor", 
			"Collectable" => "collectable_editor", 
			"DropItem" => "drop_item_editor", 
			"Mower" => "mower_editor", 
			"Shovel" => "shovel_editor", 
			"FallingObject" => "falling_object_editor", 
			"Animation" => "animation_editor", 
			"AnimationAtlas" => "animation_atlas_editor", 
			"Audio" => "audio_editor", 
			"BGM" => "bgm_editor", 
			"Survival" => "survival_editor", 
			"Tutorial" => "tutorial_editor", 
			"NpcTalk" => "npc_talk_editor", 
			"Shop" => "shop_editor", 
			"Dialog" => "dialog_editor", 
			"PacketCostRule" => "packet_cost_rule_editor", 
			"PacketOverride" => "packet_override_editor", 
			"PacketSpawnEntry" => "packet_spawn_entry_editor", 
			"PacketEvent" => "packet_event_editor", 
			"ToolEvent" => "tool_event_editor", 
			"UnlockCondition" => "unlock_condition_editor", 
			"StateGuard" => "state_guard_editor", 
			"StateMachine" => "state_machine_editor", 
			"General" => "resource_editor", 
			_ => "", 
		};
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < category.Length; i++)
		{
			char c = category[i];
			if (char.IsUpper(c) && i > 0)
			{
				stringBuilder.Append('_');
			}
			stringBuilder.Append(char.ToLowerInvariant(c));
		}
		return $"{stringBuilder}_editor";
	}

	public static string ToIconPath(string category)
	{
		return category switch
		{
			"Level" => "res://addons/ModEditor/Icons/ResourceLevel.svg", 
			"Map" => "res://addons/ModEditor/Icons/ResourceMap.svg", 
			"MapCell" => "res://addons/ModEditor/Icons/ResourceMap.svg", 
			"Character" => "res://addons/ModEditor/Icons/ResourceCharacter.svg", 
			"CharacterCombat" => "res://addons/ModEditor/Icons/ResourceCharacter.svg", 
			"CharacterData" => "res://addons/ModEditor/Icons/ResourceCharacter.svg", 
			"CharacterComponent" => "res://addons/ModEditor/Icons/GraphEdit.svg", 
			"BuffVisual" => "res://addons/ModEditor/Icons/ResourceCharacter.svg", 
			"CollisionGeometry" => "res://addons/ModEditor/Icons/ClassIcon/CollisionShape2D.svg", 
			"Card" => "res://addons/ModEditor/Icons/ResourceCard.svg", 
			"PacketBank" => "res://addons/ModEditor/Icons/ResourcePacketBank.svg", 
			"Projectile" => "res://addons/ModEditor/Icons/ResourceProjectile.svg", 
			"ProjectileChange" => "res://addons/ModEditor/Icons/ResourceProjectileChange.svg", 
			"Collectable" => "res://addons/ModEditor/Icons/ResourceCollectable.svg", 
			"DropItem" => "res://addons/ModEditor/Icons/ResourceCollectable.svg", 
			"Mower" => "res://addons/ModEditor/Icons/ResourceMower.svg", 
			"Shovel" => "res://addons/ModEditor/Icons/ResourceShovel.svg", 
			"FallingObject" => "res://addons/ModEditor/Icons/ResourceFallingObject.svg", 
			"Animation" => "res://addons/ModEditor/Icons/ResourceAnimation.svg", 
			"AnimationAtlas" => "res://addons/ModEditor/Icons/ResourceAnimation.svg", 
			"Audio" => "res://addons/ModEditor/Icons/ResourceAudio.svg", 
			"BGM" => "res://addons/ModEditor/Icons/ResourceBGM.svg", 
			"Survival" => "res://addons/ModEditor/Icons/ResourceLevel.svg", 
			"Tutorial" => "res://addons/ModEditor/Icons/GraphEdit.svg", 
			"NpcTalk" => "res://addons/ModEditor/Icons/ResourceGUI.svg", 
			"Shop" => "res://addons/ModEditor/Icons/ResourceShop.svg", 
			"Dialog" => "res://addons/ModEditor/Icons/ResourceGUI.svg", 
			"PacketCostRule" => "res://addons/ModEditor/Icons/ResourceCard.svg", 
			"PacketOverride" => "res://addons/ModEditor/Icons/ResourceCard.svg", 
			"PacketSpawnEntry" => "res://addons/ModEditor/Icons/ResourceCard.svg", 
			"PacketEvent" => "res://addons/ModEditor/Icons/ResourceCard.svg", 
			"ToolEvent" => "res://addons/ModEditor/Icons/ResourceShovel.svg", 
			"UnlockCondition" => "res://addons/ModEditor/Icons/Lock.svg", 
			"StateGuard" => "res://addons/godot_state_charts/guard.svg", 
			"StateMachine" => "res://addons/ModEditor/Icons/GraphEdit.svg", 
			"General" => "res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg", 
			_ => "res://addons/ModEditor/Icons/ClassIcon/ResourcePreloader.svg", 
		};
	}

	public static string ToScenePath(string category)
	{
		string text = category switch
		{
			"Level" => "XWLevelEditorPanel.tscn", 
			"Map" => "XWMapEditorPanel.tscn", 
			"MapCell" => "XWMapCellEditorPanel.tscn", 
			"Character" => "XWCharacterEditorPanel.tscn", 
			"CharacterCombat" => "XWCharacterCombatEditorPanel.tscn", 
			"CharacterData" => "XWCharacterDataEditorPanel.tscn", 
			"CharacterComponent" => "XWCharacterComponentEditorPanel.tscn", 
			"BuffVisual" => "XWBuffVisualEditorPanel.tscn", 
			"CollisionGeometry" => "XWCollisionGeometryEditorPanel.tscn", 
			"Card" => "XWCardEditorPanel.tscn", 
			"PacketBank" => "XWPacketBankEditorPanel.tscn", 
			"Projectile" => "XWProjectileEditorPanel.tscn", 
			"ProjectileChange" => "XWProjectileChangeEditorPanel.tscn", 
			"Collectable" => "XWCollectableEditorPanel.tscn", 
			"DropItem" => "XWDropItemEditorPanel.tscn", 
			"Mower" => "XWMowerEditorPanel.tscn", 
			"Shovel" => "XWShovelEditorPanel.tscn", 
			"FallingObject" => "XWFallingObjectEditorPanel.tscn", 
			"Animation" => "XWAnimationEditorPanel.tscn", 
			"AnimationAtlas" => "XWAnimationAtlasEditorPanel.tscn", 
			"Audio" => "XWAudioEditorPanel.tscn", 
			"BGM" => "XWBgmEditorPanel.tscn", 
			"Survival" => "XWSurvivalEditorPanel.tscn", 
			"Tutorial" => "XWTutorialEditorPanel.tscn", 
			"NpcTalk" => "XWNpcTalkEditorPanel.tscn", 
			"Shop" => "XWShopEditorPanel.tscn", 
			"Dialog" => "XWDialogEditorPanel.tscn", 
			"PacketCostRule" => "XWPacketCostRuleEditorPanel.tscn", 
			"PacketOverride" => "XWPacketOverrideEditorPanel.tscn", 
			"PacketSpawnEntry" => "XWPacketSpawnEntryEditorPanel.tscn", 
			"PacketEvent" => "XWPacketEventEditorPanel.tscn", 
			"ToolEvent" => "XWToolEventEditorPanel.tscn", 
			"UnlockCondition" => "XWUnlockConditionEditorPanel.tscn", 
			"StateGuard" => "XWStateGuardEditorPanel.tscn", 
			"StateMachine" => "XWStateMachineEditorPanel.tscn", 
			"GameplayLogic" => "XWGameplayLogicEditorPanel.tscn", 
			_ => "", 
		};
		if (string.IsNullOrEmpty(text))
		{
			return "res://addons/ModEditor/ResourceEditors/GUI/XWGenericVisualResourceEditor.tscn";
		}
		string result;
		bool flag;
		switch (category)
		{
		case "GameplayLogic":
			result = "res://addons/ModEditor/ResourceEditors/GUI/Panels/GameplayLogic/" + text;
			break;
		case "StateMachine":
		case "StateGuard":
			flag = true;
			goto IL_06b7;
		default:
			{
				flag = false;
				goto IL_06b7;
			}
			IL_06b7:
			result = (flag ? ("res://addons/ModEditor/ResourceEditors/GUI/Panels/StateMachine/" + text) : ("res://addons/ModEditor/ResourceEditors/GUI/Panels/" + text));
			break;
		}
		return result;
	}
}
