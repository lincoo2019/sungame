using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.Tools;

namespace PVZHE.ModEditor.FileSystem;

public static class XWResourceCreateRoute
{
	public sealed class CreateAction
	{
		public string Id { get; set; } = "";

		public string DisplayName { get; set; } = "";

		public string IconName { get; set; } = "Object";

		public string TemplateId { get; set; } = "";

		public string DefaultName { get; set; } = "NewResource";

		public string TargetSubFolder { get; set; } = "";
	}

	private readonly record struct CharacterConfigInfo(string ScriptClass, string ScriptUid, string ScriptPath);

	private static readonly System.Collections.Generic.Dictionary<string, CreateAction[]> DirectoryActions = new System.Collections.Generic.Dictionary<string, CreateAction[]>(StringComparer.OrdinalIgnoreCase)
	{
		["Resources/LevelCatalogs"] = new CreateAction[1] { Action("new-level-catalog", "新建关卡选择目录", "ResourceLevel", "level-catalog-resource", "Adventure") },
		["Resources/PacketSpawnEntries/Level"] = new CreateAction[1] { Action("new-level-packet-entry", "新建选卡条目", "ResourceCard", "level-packet-entry-resource", "NewLevelPacket") },
		["Resources/PacketSpawnEntries/Conveyor"] = new CreateAction[1] { Action("new-conveyor-packet-entry", "新建传送带条目", "ResourceCard", "conveyor-packet-entry-resource", "NewConveyorPacket") },
		["Resources/PacketSpawnEntries/Rain"] = new CreateAction[1] { Action("new-rain-packet-entry", "新建天降卡牌条目", "ResourceCard", "rain-packet-entry-resource", "NewRainPacket") },
		["Resources/ConveyorEvents"] = new CreateAction[1] { Action("new-conveyor-add-packet-event", "新建传送带加卡事件", "GraphEdit", "conveyor-add-packet-event-resource", "NewAddPacketEvent") },
		["Resources/Levels"] = new CreateAction[2]
		{
			Action("new-playable-level", "创建可玩关卡（含选择目录）", "ResourceLevel", "playable-level", "NewLevel"),
			Action("new-level", "新建关卡", "ResourceLevel", "level-resource", "NewLevel")
		},
		["Resources/Maps"] = new CreateAction[1] { Action("new-map", "新建地图", "ResourceMap", "map-resource", "NewMap") },
		["Resources/MapCells"] = new CreateAction[1] { Action("new-map-cell", "新建地图格子画笔", "ResourceMap", "map-cell-resource", "NewMapCell") },
		["Resources/GameplayLogic/Waves"] = new CreateAction[4]
		{
			Action("new-wave-manager", "新建完整波次管理器", "ResourceLevel", "level-wave-manager-resource", "NewWaveManager"),
			Action("new-wave", "新建波次模板", "GraphEdit", "level-wave-resource", "NewWave"),
			Action("new-wave-spawn", "新建线路生成项", "ResourceCharacter", "level-wave-spawn-resource", "NewSpawn"),
			Action("new-grid-wave-spawn", "新建格子生成项", "ResourceMap", "level-grid-wave-spawn-resource", "NewGridSpawn")
		},
		["Resources/StateMachines"] = new CreateAction[1] { Action("new-state-machine", "新建状态机", "GraphEdit", "state-machine-resource", "NewStateMachine") },
		["Resources/StateMachines/Conditions"] = new CreateAction[6]
		{
			Action("new-state-property-guard", "新建属性比较条件", "GraphEdit", "state-property-guard-resource", "NewPropertyGuard"),
			Action("new-expression-guard", "新建表达式条件", "GraphEdit", "expression-guard-resource", "NewExpressionGuard"),
			Action("new-state-active-guard", "新建状态激活条件", "GraphEdit", "state-active-guard-resource", "NewStateActiveGuard"),
			Action("new-all-of-guard", "新建全部满足组合", "GraphEdit", "all-of-guard-resource", "NewAllOfGuard"),
			Action("new-any-of-guard", "新建任一满足组合", "GraphEdit", "any-of-guard-resource", "NewAnyOfGuard"),
			Action("new-not-guard", "新建结果取反组合", "GraphEdit", "not-guard-resource", "NewNotGuard")
		},
		["Resources/CharacterComponents"] = new CreateAction[2]
		{
			Action("new-character-component-set", "新建角色组件拼图", "GraphEdit", "character-component-resource", "NewComponentSet"),
			Action("new-character-component-definition", "新建可编程角色组件", "ScriptCreate", "character-component", "NewComponent")
		},
		["Resources/CharacterCombat/Attacks"] = new CreateAction[1] { Action("new-character-attack", "新建攻击配置", "ResourceProjectile", "character-attack-resource", "NewAttack") },
		["Resources/CharacterCombat/Buffs"] = new CreateAction[4]
		{
			Action("new-character-buff-frozen", "新建冻结状态", "ResourceCharacter", "character-buff-frozen-resource", "NewFrozenBuff"),
			Action("new-character-buff-burn", "新建燃烧状态", "ResourceCharacter", "character-buff-burn-resource", "NewBurnBuff"),
			Action("new-character-buff-poison", "新建中毒状态", "ResourceCharacter", "character-buff-poison-resource", "NewPoisonBuff"),
			Action("new-character-buff-hypnoses", "新建魅惑状态", "ResourceCharacter", "character-buff-hypnoses-resource", "NewHypnosesBuff")
		},
		["Resources/CharacterCombat/Events"] = new CreateAction[5]
		{
			Action("new-character-event-hurt", "新建直接伤害事件", "ResourceProjectile", "character-event-hurt-resource", "NewHurtEvent"),
			Action("new-character-event-config-hurt", "新建配置伤害事件", "ResourceProjectile", "character-event-config-hurt-resource", "NewConfigHurtEvent"),
			Action("new-character-event-add-buff", "新建添加状态事件", "ResourceCharacter", "character-event-add-buff-resource", "NewAddBuffEvent"),
			Action("new-character-event-random", "新建随机条件事件", "GraphEdit", "character-event-random-resource", "NewRandomEvent"),
			Action("new-character-event-lucky-draw", "新建权重事件池", "GraphEdit", "character-event-lucky-draw-resource", "NewLuckyDrawEvent")
		},
		["Resources/CharacterData/Armor"] = new CreateAction[3]
		{
			Action("new-character-armor-data", "新建角色护甲集合", "ResourceCharacter", "character-armor-data-resource", "NewArmorData"),
			Action("new-character-armor-slot", "新建护甲槽", "ResourceCharacter", "character-armor-slot-resource", "NewArmorSlot"),
			Action("new-character-armor-type", "新建护甲类型", "ResourceCharacter", "character-armor-type-resource", "NewArmorType")
		},
		["Resources/CharacterData/Custom"] = new CreateAction[2]
		{
			Action("new-character-custom-data", "新建角色外观集合", "ResourceCharacter", "character-custom-data-resource", "NewCustomData"),
			Action("new-character-custom-config", "新建角色外观项", "ResourceCharacter", "character-custom-config-resource", "NewCustomConfig")
		},
		["Resources/CharacterData/DamagePoints"] = new CreateAction[2]
		{
			Action("new-character-damage-point-data", "新建受伤点集合", "ResourceCharacter", "character-damage-point-data-resource", "NewDamagePointData"),
			Action("new-character-damage-point-config", "新建受伤点", "ResourceCharacter", "character-damage-point-config-resource", "NewDamagePoint")
		},
		["Resources/BuffVisuals"] = new CreateAction[1] { Action("new-buff-visual", "新建状态外观", "ResourceCharacter", "buff-visual-resource", "NewBuffVisual") },
		["Resources/CollisionGeometry"] = new CreateAction[3]
		{
			Action("new-character-hitbox", "新建角色碰撞框", "CollisionShape2D", "character-hitbox-resource", "NewHitBox"),
			Action("new-aabb-shape", "新建区域碰撞形状", "CollisionShape2D", "aabb-shape-resource", "NewAreaShape"),
			Action("new-aabb-ray", "新建检测射线", "RayCast2D", "aabb-ray-resource", "NewCheckRay")
		},
		["Resources/AwardSettlements"] = new CreateAction[1] { Action("new-award-settlement", "新建奖励结算画面", "ResourceCollectable", "award-settlement-resource", "NewAwardSettlement") },
		["Resources/UnlockConditions"] = new CreateAction[1] { Action("new-unlock-condition", "新建关卡完成解锁条件", "Lock", "unlock-condition-resource", "NewUnlockCondition") },
		["Resources/PacketEvents"] = new CreateAction[1] { Action("new-packet-event", "新建卡牌费用事件", "ResourceCard", "packet-event-resource", "NewPacketEvent") },
		["Resources/ToolEvents"] = new CreateAction[1] { Action("new-tool-event", "新建小推车阳光事件", "ResourceShovel", "tool-event-resource", "NewToolEvent") },
		["Resources/Cards"] = new CreateAction[1] { Action("new-card", "新建卡片", "ResourceCard", "card-resource", "NewCard") },
		["Resources/CardCostRules"] = new CreateAction[1] { Action("new-packet-cost-rule", "新建卡牌费用规则", "ResourceCard", "packet-cost-rule-resource", "NewCostRule") },
		["Resources/CardOverrides"] = new CreateAction[1] { Action("new-packet-override", "新建卡牌覆盖规则", "ResourceCard", "packet-override-resource", "NewOverride") },
		["Resources/PacketBank"] = new CreateAction[1] { Action("new-packet-bank", "新建卡牌库", "ResourcePacketBank", "packet-bank-resource", "NewPacketBank") },
		["Resources/Projectiles"] = new CreateAction[1] { Action("new-projectile", "新建子弹", "ResourceProjectile", "projectile", "NewProjectile") },
		["Resources/ProjectileChanges"] = new CreateAction[1] { Action("new-projectile-change", "新建子弹变化", "ResourceProjectileChange", "projectile-change-resource", "NewProjectileChange") },
		["Resources/Characters/Plants"] = new CreateAction[1] { Action("new-character-plant", "新建植物角色", "ResourceCharacter", "character-scene-plant", "NewPlant") },
		["Resources/Characters/Zombies"] = new CreateAction[1] { Action("new-character-zombie", "新建僵尸角色", "ResourceCharacter", "character-scene-zombie", "NewZombie") },
		["Resources/Characters/Props"] = new CreateAction[1] { Action("new-character-prop", "新建道具角色", "ResourceCharacter", "character-scene-prop", "NewProp") },
		["Resources/Characters/Vases"] = new CreateAction[1] { Action("new-character-vase", "新建花瓶角色", "ResourceCharacter", "character-scene-vase", "NewVase") },
		["Resources/Characters/Mowers"] = new CreateAction[1] { Action("new-character-mower", "新建小推车角色", "ResourceCharacter", "character-scene-mower", "NewMowerCharacter") },
		["Resources/Characters/Items"] = new CreateAction[1] { Action("new-character-item", "新建物品角色", "ResourceCharacter", "character-scene-item", "NewItemCharacter") },
		["Resources/Characters/Graves"] = new CreateAction[1] { Action("new-character-grave", "新建墓碑角色", "ResourceCharacter", "character-scene-grave", "NewGrave") },
		["Resources/Characters/Craters"] = new CreateAction[1] { Action("new-character-crater", "新建弹坑角色", "ResourceCharacter", "character-scene-crater", "NewCrater") },
		["Resources/Shops"] = new CreateAction[1] { Action("new-shop", "新建商店", "ResourceShop", "shop-resource", "NewShop") },
		["Resources/Dialogs"] = new CreateAction[3]
		{
			Action("new-dialog", "新建空白界面", "ResourceGUI", "dialog-scene", "NewDialog"),
			Action("new-menu-dialog", "新建菜单对话框（MenuDialogBase）", "ResourceGUI", "menu-dialog-scene", "NewMenuDialog"),
			Action("new-dialog-popup", "新建弹窗对话框（DialogPopup）", "ResourceGUI", "dialog-popup-scene", "NewDialogPopup")
		},
		["Resources/Collectables"] = new CreateAction[1] { Action("new-collectable", "新建收集物", "ResourceCollectable", "collectable-resource", "NewCollectable") },
		["Resources/DropItems"] = new CreateAction[1] { Action("new-drop-item", "新建战场掉落物", "ResourceCollectable", "drop-item-resource", "NewDropItem") },
		["Resources/Mowers"] = new CreateAction[1] { Action("new-mower", "新建小推车配置", "ResourceMower", "mower-resource", "NewMower") },
		["Resources/Shovels"] = new CreateAction[1] { Action("new-shovel", "新建铲子", "ResourceShovel", "shovel-resource", "NewShovel") },
		["Resources/FallingObjects"] = new CreateAction[1] { Action("new-falling-object", "新建掉落物", "ResourceFallingObject", "falling-object-resource", "NewFallingObject") },
		["Resources/Animations"] = new CreateAction[1] { Action("new-animation", "导入动画源文件（Adobe Animate）", "ResourceAnimation", "adobe-animate-xfl", "NewAnimation") },
		["Resources/AnimationAtlasProfiles"] = new CreateAction[1] { Action("new-animation-atlas-profile", "新建动画图集配置", "ResourceAnimation", "animation-atlas-profile-resource", "NewAtlasProfile") },
		["Resources/BGMConfigs"] = new CreateAction[1] { Action("new-bgm-config", "新建背景音乐配置", "ResourceBGM", "bgm-resource", "NewBGM") },
		["Resources/Survivals"] = new CreateAction[1] { Action("new-survival", "新建生存模式", "ResourceLevel", "survival-resource", "NewSurvival") },
		["Resources/Tutorials"] = new CreateAction[1] { Action("new-tutorial", "新建教程", "GraphEdit", "tutorial-resource", "NewTutorial") },
		["Resources/Tutorials/Conditions"] = new CreateAction[2]
		{
			Action("new-tutorial-condition", "新建角色数量条件", "ResourceCharacter", "tutorial-condition-resource", "NewCharacterCondition"),
			Action("new-tutorial-sun-condition", "新建阳光收集条件", "Add", "tutorial-sun-condition-resource", "NewSunCondition")
		},
		["Resources/Tutorials/Steps"] = new CreateAction[1] { Action("new-tutorial-step", "新建教程步骤", "GraphEdit", "tutorial-step-resource", "NewStep") },
		["Resources/NpcTalks"] = new CreateAction[1] { Action("new-npc-talk", "新建非玩家角色对话（NPC）", "ResourceGUI", "npc-talk-resource", "NewTalk") },
		["Localization"] = new CreateAction[1] { Action("new-localization-csv", "新建多语言表格（CSV）", "Translation", "localization-csv", "NewLocalization") }
	};

	private static readonly CreateAction[] CharacterSubResourceActions = new CreateAction[3]
	{
		SubAction("new-character-config-child", "新建角色配置", "Resource", "character-definition", "NewConfig", "Config"),
		SubAction("new-character-script-child", "新建角色脚本", "Script", "character-csharp", "NewCharacterScript", "Script"),
		SubAction("new-character-blueprint-child", "新建蓝图", "GraphEdit", "blueprint", "new_blueprint", "Script")
	};

	public static IReadOnlyList<CreateAction> GetActionsForDirectory(string directoryPath, string projectPath)
	{
		string key = XWModProjectLayout.ToProjectRelativePath(directoryPath, projectPath);
		if (!DirectoryActions.TryGetValue(key, out var value))
		{
			return new List<CreateAction>();
		}
		return new List<CreateAction>(value);
	}

	public static bool TryGetTemplateActionPresentation(string templateId, out string displayName, out string defaultName)
	{
		displayName = "";
		defaultName = "";
		if (string.IsNullOrWhiteSpace(templateId))
		{
			return false;
		}
		foreach (CreateAction[] value in DirectoryActions.Values)
		{
			foreach (CreateAction createAction in value)
			{
				if (string.Equals(createAction.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
				{
					displayName = createAction.DisplayName;
					defaultName = createAction.DefaultName;
					return true;
				}
			}
		}
		CreateAction[] current = CharacterSubResourceActions;
		foreach (CreateAction createAction2 in current)
		{
			if (string.Equals(createAction2.TemplateId, templateId, StringComparison.OrdinalIgnoreCase))
			{
				displayName = createAction2.DisplayName;
				defaultName = createAction2.DefaultName;
				return true;
			}
		}
		return false;
	}

	public static XWTemplateLibrary.TemplateCreateResult CreateFromAction(string actionId, string directoryPath, string resourceName)
	{
		CreateAction createAction = FindAction(actionId);
		if (createAction == null)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = actionId,
				Success = false,
				Error = "Create action not found."
			};
		}
		if (createAction.Id == "new-level")
		{
			return CreateLevelResource(createAction, directoryPath, resourceName);
		}
		if (createAction.Id == "new-playable-level")
		{
			return CreatePlayableLevel(createAction, directoryPath, resourceName);
		}
		XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
		XWTemplateLibrary.TemplateInfo templateInfo = null;
		foreach (XWTemplateLibrary.TemplateInfo template in xWTemplateLibrary.Templates)
		{
			if (template.Id == createAction.TemplateId)
			{
				templateInfo = template;
				break;
			}
		}
		if (templateInfo == null)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = createAction.TemplateId,
				Success = false,
				Error = "Template not found."
			};
		}
		if (IsCharacterSceneTemplate(createAction.TemplateId))
		{
			string resourceName2 = (string.IsNullOrWhiteSpace(resourceName) ? createAction.DefaultName : resourceName);
			return CreateCharacterScenePackageFromTemplate(createAction.TemplateId, directoryPath, resourceName2, ResolveActionDisplayName(createAction, resourceName));
		}
		if (createAction.TemplateId == "character-component")
		{
			return xWTemplateLibrary.CreateFromTemplateInDirectory(createAction.TemplateId, directoryPath, resourceName, ResolveActionDisplayName(createAction, resourceName));
		}
		string text = XWTemplateLibrary.SanitizeName(string.IsNullOrWhiteSpace(resourceName) ? createAction.DefaultName : resourceName);
		Directory.CreateDirectory(directoryPath);
		string text2 = NormalizePath(Path.Combine(directoryPath, text + templateInfo.FileExtension));
		if (File.Exists(text2))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateInfo.Id,
				CreatedPath = text2,
				Success = false,
				Error = "Target file already exists."
			};
		}
		string contents = templateInfo.Content.Replace("${Name}", text).Replace("${DisplayName}", ResolveActionDisplayName(createAction, resourceName));
		File.WriteAllText(text2, contents, Encoding.UTF8);
		return new XWTemplateLibrary.TemplateCreateResult
		{
			TemplateId = templateInfo.Id,
			CreatedPath = text2,
			CreatedPaths = new List<string> { text2 },
			Success = true
		};
	}

	private static XWTemplateLibrary.TemplateCreateResult CreatePlayableLevel(CreateAction action, string directoryPath, string resourceName)
	{
		string text = FindProjectRoot(directoryPath);
		if (text == null)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				Error = "请先打开 Mod 工程。"
			};
		}
		string text2 = XWTemplateLibrary.SanitizeName(string.IsNullOrWhiteSpace(resourceName) ? action.DefaultName : resourceName);
		string text3 = NormalizePath(Path.Combine(directoryPath, text2 + ".tres"));
		string text4 = NormalizePath(Path.Combine(text, "Resources", "LevelCatalogs", text2 + "Catalog.tres"));
		if (File.Exists(text3) || File.Exists(text4))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				Error = "同名关卡或目录已存在，请使用新名称。"
			};
		}
		bool flag = false;
		bool flag2 = false;
		try
		{
			Directory.CreateDirectory(directoryPath);
			Directory.CreateDirectory(Path.GetDirectoryName(text4));
			TowerDefenseLevelNewConfig towerDefenseLevelNewConfig = XWNewLevelResourceDefaults.Create(text2, resourceName);
			if (ResourceSaver.Save(towerDefenseLevelNewConfig, text3, ResourceSaver.SaverFlags.None) != Error.Ok)
			{
				throw new IOException("关卡保存失败。");
			}
			flag = true;
			towerDefenseLevelNewConfig.TakeOverPath(text3);
			LevelChooseConfig item = new LevelChooseConfig
			{
				saveKey = text2,
				normalLevel = towerDefenseLevelNewConfig,
				unlockImage = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Adventure_LEVEL1-1.png")
			};
			LevelChapterConfig item2 = new LevelChapterConfig
			{
				chapterName = "新章节",
				levelList = new Array<LevelChooseConfig> { item },
				unlockImage = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1.png"),
				background = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1Background.jpg"),
				building = GD.Load<Texture2D>("res://Asset/Texture/GUI/TowerDefense/Level/Chapter1/Chapter1Building.png")
			};
			if (ResourceSaver.Save(new LevelCatalogConfig
			{
				catalogKey = text2 + "Catalog",
				chapterList = new Array<LevelChapterConfig> { item2 }
			}, text4, ResourceSaver.SaverFlags.None) != Error.Ok)
			{
				throw new IOException("关卡目录保存失败。");
			}
			flag2 = true;
			XWModProjectLayout.MakeSavedTextResourceReferencesPortable(text3, text);
			XWModProjectLayout.MakeSavedTextResourceReferencesPortable(text4, text);
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = action.TemplateId,
				Success = true,
				CreatedPath = text4,
				CreatedPaths = new List<string> { text3, text4 }
			};
		}
		catch (Exception ex)
		{
			if (flag2)
			{
				File.Delete(text4);
			}
			if (flag)
			{
				File.Delete(text3);
			}
			return new XWTemplateLibrary.TemplateCreateResult
			{
				Error = "创建可玩关卡失败：" + ex.Message
			};
		}
	}

	private static string FindProjectRoot(string directory)
	{
		for (DirectoryInfo directoryInfo = new DirectoryInfo(directory); directoryInfo != null; directoryInfo = directoryInfo.Parent)
		{
			if (File.Exists(Path.Combine(directoryInfo.FullName, "mod.json")))
			{
				return directoryInfo.FullName;
			}
		}
		return null;
	}

	private static XWTemplateLibrary.TemplateCreateResult CreateLevelResource(CreateAction action, string directoryPath, string resourceName)
	{
		string displayName = ResolveActionDisplayName(action, resourceName);
		string text = XWTemplateLibrary.SanitizeName(string.IsNullOrWhiteSpace(resourceName) ? action.DefaultName : resourceName);
		Directory.CreateDirectory(directoryPath);
		string text2 = NormalizePath(Path.Combine(directoryPath, text + ".tres"));
		if (File.Exists(text2))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = action.TemplateId,
				CreatedPath = text2,
				Success = false,
				Error = "Target file already exists."
			};
		}
		Error error = ResourceSaver.Save(XWNewLevelResourceDefaults.Create(text, displayName), text2, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = action.TemplateId,
				CreatedPath = text2,
				Success = false,
				Error = $"Failed to save level resource: {error}"
			};
		}
		return new XWTemplateLibrary.TemplateCreateResult
		{
			TemplateId = action.TemplateId,
			CreatedPath = text2,
			CreatedPaths = new List<string> { text2 },
			Success = true
		};
	}

	public static IReadOnlyList<CreateAction> GetSubResourceActionsForOwner(string ownerPath, string projectPath)
	{
		if (!IsCharacterSceneOwner(ownerPath, projectPath))
		{
			return new List<CreateAction>();
		}
		return new List<CreateAction>(CharacterSubResourceActions);
	}

	public static XWTemplateLibrary.TemplateCreateResult CreateSubResourceFromAction(string actionId, string ownerPath, string resourceName)
	{
		CreateAction createAction = FindSubResourceAction(actionId);
		if (createAction == null)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = actionId,
				Success = false,
				Error = "Subresource create action not found."
			};
		}
		string characterPackageDirectory = GetCharacterPackageDirectory(ownerPath);
		if (string.IsNullOrWhiteSpace(characterPackageDirectory))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = createAction.TemplateId,
				Success = false,
				Error = "Parent resource does not have a character package."
			};
		}
		string text = (string.IsNullOrWhiteSpace(createAction.TargetSubFolder) ? characterPackageDirectory : NormalizePath(Path.Combine(characterPackageDirectory, createAction.TargetSubFolder)));
		string text2 = ResolveCharacterSceneTemplateId(ownerPath);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = createAction.TemplateId,
				Success = false,
				Error = "无法识别角色包类型，请把角色放在标准的角色分类目录中。"
			};
		}
		string resourceName2 = (string.IsNullOrWhiteSpace(resourceName) ? createAction.DefaultName : resourceName);
		string displayName = ResolveActionDisplayName(createAction, resourceName);
		if (createAction.TemplateId == "character-definition")
		{
			return CreateCharacterConfigSubResource(createAction.TemplateId, text2, text, resourceName2, displayName);
		}
		if (createAction.TemplateId == "character-csharp")
		{
			return CreateCharacterScriptSubResource(createAction.TemplateId, text2, text, resourceName2);
		}
		if (createAction.TemplateId == "blueprint")
		{
			return CreateCharacterBlueprintSubResource(createAction.TemplateId, text2, text, resourceName2, displayName);
		}
		return new XWTemplateLibrary().CreateFromTemplateInDirectory(createAction.TemplateId, text, resourceName2, displayName);
	}

	public static string GetCharacterPackageDirectory(string ownerPath)
	{
		if (string.IsNullOrWhiteSpace(ownerPath))
		{
			return "";
		}
		string text = NormalizePath(ownerPath);
		string text2 = Path.GetExtension(text).TrimStart('.').ToLowerInvariant();
		if ((!(text2 == "tscn") && !(text2 == "scn")) || 1 == 0)
		{
			return "";
		}
		if (!text.Contains("/Resources/Characters/", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("Resources/Characters/", StringComparison.OrdinalIgnoreCase))
		{
			return "";
		}
		string text3 = Path.GetDirectoryName(text)?.Replace('\\', '/') ?? "";
		if (string.Equals(Path.GetFileName(text3), "Scene", StringComparison.OrdinalIgnoreCase))
		{
			return NormalizePath(Path.GetDirectoryName(text3)?.Replace('\\', '/') ?? "");
		}
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(text);
		return NormalizePath(Path.Combine(text3, fileNameWithoutExtension));
	}

	public static XWTemplateLibrary.TemplateCreateResult CreateCharacterScenePackageFromTemplate(string templateId, string directoryPath, string resourceName, string displayName, bool overwrite = false)
	{
		XWTemplateLibrary.TemplateInfo templateInfo = new XWTemplateLibrary().FindTemplate(templateId);
		if (templateInfo == null || !IsCharacterSceneTemplate(templateId))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateId,
				Success = false,
				Error = "找不到指定的角色场景模板。"
			};
		}
		string text = XWTemplateLibrary.SanitizeName(resourceName);
		string displayName2 = (string.IsNullOrWhiteSpace(displayName) ? text : displayName.Trim());
		Directory.CreateDirectory(directoryPath);
		string text2 = NormalizePath(Path.Combine(directoryPath, text));
		string text3 = NormalizePath(Path.Combine(text2, "Scene"));
		string text4 = NormalizePath(Path.Combine(text2, "Config"));
		string text5 = NormalizePath(Path.Combine(text2, "Sprite"));
		string text6 = NormalizePath(Path.Combine(text2, "DamagePoint"));
		string text7 = NormalizePath(Path.Combine(text2, "Custom"));
		string text8 = NormalizePath(Path.Combine(text2, "Armor"));
		string text9 = NormalizePath(Path.Combine(text2, "Script"));
		string text10 = NormalizePath(Path.Combine(text3, text + templateInfo.FileExtension));
		string text11 = NormalizePath(Path.Combine(text4, text + "Config.tres"));
		string text12 = NormalizePath(Path.Combine(text5, text + ".tscn"));
		string text13 = NormalizePath(Path.Combine(text2, text + ".tres"));
		string text14 = NormalizePath(Path.Combine(text9, text + ".cs"));
		string text15 = NormalizePath(Path.Combine(text6, "DamagePointData.tres"));
		string text16 = NormalizePath(Path.Combine(text7, "CustomData.tres"));
		string text17 = NormalizePath(Path.Combine(text8, "ArmorData.tres"));
		if (Directory.Exists(text2))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateInfo.Id,
				CreatedPath = text10,
				Success = false,
				Error = (overwrite ? "角色包包含多个相互引用的文件，不能直接覆盖；请换一个技术名称，或先在资源面板中明确删除旧角色包。" : "目标角色场景或角色包已经存在。")
			};
		}
		Directory.CreateDirectory(text2);
		Directory.CreateDirectory(text3);
		Directory.CreateDirectory(text4);
		Directory.CreateDirectory(text5);
		Directory.CreateDirectory(text6);
		Directory.CreateDirectory(text7);
		Directory.CreateDirectory(text8);
		Directory.CreateDirectory(text9);
		File.WriteAllText(text10, BuildCharacterRuntimeSceneContent(templateInfo.Id, text, displayName2), Encoding.UTF8);
		File.WriteAllText(text14, BuildCharacterScriptContent(templateInfo.Id, text), Encoding.UTF8);
		File.WriteAllText(text13, BuildCharacterAnimationDataContent(text, displayName2), Encoding.UTF8);
		File.WriteAllText(text12, BuildCharacterSpriteSceneContent(templateInfo.Id, text), Encoding.UTF8);
		File.WriteAllText(text15, BuildCharacterDataResourceContent("CharacterDamagePointData", "uid://glu4fym1t3ns", "Resource/General/Character/DamagePoint/CharacterDamagePointData.cs", "角色受伤点集合"), Encoding.UTF8);
		File.WriteAllText(text16, BuildCharacterDataResourceContent("CharacterCustomData", "uid://dthsm21ucumm4", "Resource/General/Character/Costom/CharacterCustomData.cs", "角色外观集合"), Encoding.UTF8);
		File.WriteAllText(text17, BuildCharacterDataResourceContent("CharacterArmorData", "uid://d4b0h447ngm28", "Resource/General/Character/Armor/CharacterArmorData.cs", "角色护甲集合"), Encoding.UTF8);
		File.WriteAllText(text11, BuildCharacterConfigContent(templateInfo.Id, text, displayName2), Encoding.UTF8);
		List<string> createdPaths = new List<string> { text10, text14, text13, text12, text15, text16, text17, text11 };
		return new XWTemplateLibrary.TemplateCreateResult
		{
			TemplateId = templateInfo.Id,
			CreatedPath = text10,
			CreatedPaths = createdPaths,
			Success = true
		};
	}

	private static bool IsCharacterSceneTemplate(string templateId)
	{
		if (!string.IsNullOrWhiteSpace(templateId))
		{
			return templateId.StartsWith("character-scene-", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool IsCharacterSceneOwner(string ownerPath, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(ownerPath))
		{
			return false;
		}
		string text = XWModProjectLayout.ToProjectRelativePath(ownerPath, projectPath);
		string text2 = Path.GetExtension(text).TrimStart('.').ToLowerInvariant();
		if ((text2 == "tscn" || text2 == "scn") ? true : false)
		{
			return text.StartsWith("Resources/Characters/", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static CreateAction Action(string id, string displayName, string iconName, string templateId, string defaultName)
	{
		return new CreateAction
		{
			Id = id,
			DisplayName = displayName,
			IconName = iconName,
			TemplateId = templateId,
			DefaultName = defaultName
		};
	}

	private static CreateAction SubAction(string id, string displayName, string iconName, string templateId, string defaultName, string targetSubFolder)
	{
		CreateAction createAction = Action(id, displayName, iconName, templateId, defaultName);
		createAction.TargetSubFolder = targetSubFolder;
		return createAction;
	}

	private static CreateAction FindAction(string actionId)
	{
		foreach (CreateAction[] value in DirectoryActions.Values)
		{
			foreach (CreateAction createAction in value)
			{
				if (createAction.Id == actionId)
				{
					return createAction;
				}
			}
		}
		return null;
	}

	private static CreateAction FindSubResourceAction(string actionId)
	{
		CreateAction[] characterSubResourceActions = CharacterSubResourceActions;
		foreach (CreateAction createAction in characterSubResourceActions)
		{
			if (createAction.Id == actionId)
			{
				return createAction;
			}
		}
		return null;
	}

	private static string ResolveActionDisplayName(CreateAction action, string resourceName)
	{
		if (action == null)
		{
			return resourceName ?? "";
		}
		if (string.IsNullOrWhiteSpace(resourceName) || string.Equals(resourceName.Trim(), action.DefaultName, StringComparison.Ordinal))
		{
			return action.DisplayName;
		}
		return resourceName.Trim();
	}

	private static XWTemplateLibrary.TemplateCreateResult CreateCharacterConfigSubResource(string templateId, string sceneTemplateId, string targetDir, string resourceName, string displayName)
	{
		string text = XWTemplateLibrary.SanitizeName(resourceName);
		string targetPath = NormalizePath(Path.Combine(targetDir, text + ".tres"));
		string content = BuildCharacterConfigContent(sceneTemplateId, text, displayName);
		return WriteCharacterSubResource(templateId, targetDir, targetPath, content);
	}

	private static XWTemplateLibrary.TemplateCreateResult CreateCharacterScriptSubResource(string templateId, string sceneTemplateId, string targetDir, string resourceName)
	{
		string text = XWTemplateLibrary.SanitizeName(resourceName);
		string targetPath = NormalizePath(Path.Combine(targetDir, text + ".cs"));
		string content = BuildCharacterScriptContent(sceneTemplateId, text);
		return WriteCharacterSubResource(templateId, targetDir, targetPath, content);
	}

	private static XWTemplateLibrary.TemplateCreateResult CreateCharacterBlueprintSubResource(string templateId, string sceneTemplateId, string targetDir, string resourceName, string displayName)
	{
		string text = XWTemplateLibrary.SanitizeName(resourceName);
		string path = NormalizePath(Path.Combine(targetDir, text + ".tres"));
		string characterBaseClassName = GetCharacterBaseClassName(sceneTemplateId);
		XWBlueprintCreationService.Result result = XWBlueprintCreationService.Create(path, characterBaseClassName, displayName);
		return new XWTemplateLibrary.TemplateCreateResult
		{
			TemplateId = templateId,
			CreatedPath = result.CreatedPath,
			CreatedPaths = (result.Success ? new List<string> { result.CreatedPath } : new List<string>()),
			Success = result.Success,
			Error = result.Error
		};
	}

	private static XWTemplateLibrary.TemplateCreateResult WriteCharacterSubResource(string templateId, string targetDir, string targetPath, string content)
	{
		if (File.Exists(targetPath))
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateId,
				CreatedPath = targetPath,
				Success = false,
				Error = "目标子资源已经存在。"
			};
		}
		try
		{
			Directory.CreateDirectory(targetDir);
			File.WriteAllText(targetPath, content, Encoding.UTF8);
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateId,
				CreatedPath = targetPath,
				CreatedPaths = new List<string> { targetPath },
				Success = true
			};
		}
		catch (Exception ex)
		{
			return new XWTemplateLibrary.TemplateCreateResult
			{
				TemplateId = templateId,
				CreatedPath = targetPath,
				Success = false,
				Error = "无法创建角色子资源：" + ex.GetBaseException().Message
			};
		}
	}

	private static string ResolveCharacterSceneTemplateId(string ownerPath)
	{
		string text = NormalizePath(ownerPath);
		(string, string)[] array = new(string, string)[8]
		{
			("Plants", "character-scene-plant"),
			("Zombies", "character-scene-zombie"),
			("Props", "character-scene-prop"),
			("Vases", "character-scene-vase"),
			("Mowers", "character-scene-mower"),
			("Items", "character-scene-item"),
			("Graves", "character-scene-grave"),
			("Craters", "character-scene-crater")
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string text2 = "Resources/Characters/" + item + "/";
			if (text.Contains("/" + text2, StringComparison.OrdinalIgnoreCase) || text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
			{
				return item2;
			}
		}
		return "";
	}

	private static string BuildTemplateContent(XWTemplateLibrary.TemplateInfo template, string safeName, string displayName)
	{
		return template.Content.Replace("${ClassName}", safeName).Replace("${Name}", safeName).Replace("${DisplayName}", displayName);
	}

	private static string BuildCharacterRuntimeSceneContent(string templateId, string safeName, string displayName)
	{
		string characterBaseScenePath = GetCharacterBaseScenePath(templateId);
		string characterCategory = GetCharacterCategory(templateId);
		string text = BuildCharacterRuntimeDefaults(templateId);
		string text2 = EscapeGodotString(displayName);
		string text3 = EscapeGodotString(safeName);
		global::_003C_003Ey__InlineArray5<object> buffer = default(global::_003C_003Ey__InlineArray5<object>);
		buffer[0] = text3;
		buffer[1] = characterBaseScenePath;
		buffer[2] = text2;
		buffer[3] = characterCategory;
		buffer[4] = text;
		return string.Format("[gd_scene load_steps=4 format=3]\n\n[ext_resource type=\"PackedScene\" path=\"{1}\" id=\"1_base\"]\n[ext_resource type=\"Resource\" path=\"../Config/{0}Config.tres\" id=\"3_config\"]\n[ext_resource type=\"PackedScene\" path=\"../Sprite/{0}.tscn\" id=\"4_sprite\"]\n\n[node name=\"{0}\" node_paths=PackedStringArray(\"sprite\") instance=ExtResource(\"1_base\")]\nconfig = ExtResource(\"3_config\")\nsprite = NodePath(\"SpriteGroup/TransformPoint/{0}Sprite\")\n{4}metadata/mod_resource_kind = \"Character\"\nmetadata/mod_display_name = \"{2}\"\nmetadata/mod_character_category = \"{3}\"\nmetadata/mod_character_config_path = \"../Config/{0}Config.tres\"\nmetadata/mod_character_script_path = \"../Script/{0}.cs\"\nmetadata/mod_character_script_binding = \"CompanionOnly\"\nmetadata/mod_character_sprite_scene = \"../Sprite/{0}.tscn\"\n\n[node name=\"{0}Sprite\" parent=\"SpriteGroup/TransformPoint\" instance=ExtResource(\"4_sprite\")]\nposition = Vector2(0, -30)\n\n[editable path=\"SpriteGroup/TransformPoint/{0}Sprite\"]\n", (ReadOnlySpan<object?>)buffer);
	}

	private static string BuildCharacterRuntimeDefaults(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => "walkAnimeClip = \"Walk1&Walk2\"\nswimAnimeClip = \"Swim\"\ndieAnimeClip = \"Death1&Death2\"\ndieWaterAnimeClip = \"Waterdeath\"\nidleAnimeClip = \"Idle1&Idle2\"\nsleepAnimeClip = \"Idle1&Idle2\"\n", 
			"character-scene-mower" => "runAnimeClips = \"Normal\"\nrunWaterAnimeClips = \"Normal\"\nidleAnimeClip = \"Normal\"\nsleepAnimeClip = \"Normal\"\n", 
			"character-scene-grave" => "rise = true\nidleAnimeClip = \"Idle1&Idle10&Idle11&Idle12&Idle13&Idle14&Idle15&Idle16&Idle17&Idle18&Idle19&Idle2&Idle20&Idle3&Idle4&Idle5&Idle6&Idle7&Idle8&Idle9\"\n", 
			"character-scene-crater" => "idleAnimeClip = \"Day\"\nsleepAnimeClip = \"Day\"\n", 
			"character-scene-prop" => "canCheck = true\ncanCheckTarget = true\n", 
			"character-scene-vase" => "packetBank = \"VaseNormal\"\nidleAnimeClip = \"Idle\"\n", 
			_ => "idleAnimeClip = \"Idle\"\n", 
		};
	}

	private static string BuildCharacterScriptContent(string templateId, string safeName)
	{
		string value = SanitizeCSharpIdentifier(safeName);
		string characterBaseClassName = GetCharacterBaseClassName(templateId);
		return "using Godot;\n\n[Tool]\n[GlobalClass]\n" + $"public partial class {value} : {characterBaseClassName}\n" + "{\n    public override void _Ready()\n    {\n        base._Ready();\n    }\n}\n";
	}

	private static string BuildCharacterAnimationDataContent(string safeName, string displayName)
	{
		return "[gd_resource type=\"Resource\" script_class=\"AdobeAnimateData\" format=3]\n\n[ext_resource type=\"Script\" uid=\"uid://b7srgu1j1lcoj\" path=\"res://addons/AdobeAnimateEditor/Resource/AdobeAnimateData.cs\" id=\"1_adobe_data\"]\n\n[resource]\nscript = ExtResource(\"1_adobe_data\")\nframeRate = 30.0\nframeScale = 1\nframeMax = 0\nmetadata/mod_resource_kind = \"CharacterSpriteAnimation\"\nmetadata/mod_display_name = \"" + EscapeGodotString(displayName) + "\"\nmetadata/mod_character_key = \"" + EscapeGodotString(safeName) + "\"\n";
	}

	private static string BuildCharacterSpriteSceneContent(string templateId, string safeName)
	{
		string text = EscapeGodotString(safeName + "Sprite");
		string characterExampleSpriteScenePath = GetCharacterExampleSpriteScenePath(templateId);
		return "[gd_scene load_steps=2 format=3]\n\n[ext_resource type=\"PackedScene\" path=\"res://" + characterExampleSpriteScenePath + "\" id=\"1_game_sprite\"]\n\n[node name=\"" + text + "\" instance=ExtResource(\"1_game_sprite\")]\nmetadata/mod_resource_kind = \"CharacterSprite\"\nmetadata/mod_preview_source = \"内置游戏角色视觉\"\nmetadata/mod_animation_data_path = \"../" + EscapeGodotString(safeName) + ".tres\"\n";
	}

	private static string GetCharacterExampleSpriteScenePath(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => "Asset/Anime/Character/Zombie/Chapter1/Normal/Sprite/Normal/ZombieNormal.tscn", 
			"character-scene-vase" => "Asset/Anime/Character/Vase/Normal/VaseNormal.tscn", 
			"character-scene-mower" => "Asset/Anime/Character/Mower/Default/LawnMower.tscn", 
			"character-scene-item" => "Asset/Anime/Character/Item/Brain/Brain.tscn", 
			"character-scene-prop" => "Asset/Anime/Character/Item/Ladder/Ladder.tscn", 
			"character-scene-grave" => "Asset/Anime/Character/GraveStone/Default/GraveStoneDefault.tscn", 
			"character-scene-crater" => "Asset/Anime/Character/Crater/CraterDayGround/CraterDayGround.tscn", 
			_ => "Asset/Anime/Character/Plant/Chapter0/PeaShooter/PeaShooter.tscn", 
		};
	}

	private static string BuildCharacterDataResourceContent(string scriptClass, string scriptUid, string scriptPath, string resourceName)
	{
		return $"[gd_resource type=\"Resource\" script_class=\"{scriptClass}\" format=3]\n\n[ext_resource type=\"Script\" uid=\"{scriptUid}\" path=\"res://{scriptPath}\" id=\"1_script\"]\n\n" + "[resource]\nscript = ExtResource(\"1_script\")\nresource_name = \"" + EscapeGodotString(resourceName) + "\"\nmetadata/mod_display_name = \"" + EscapeGodotString(resourceName) + "\"\n";
	}

	private static string BuildCharacterConfigContent(string templateId, string safeName, string displayName)
	{
		CharacterConfigInfo characterConfigInfo = GetCharacterConfigInfo(templateId);
		string text = ((templateId == "character-scene-mower") ? "[ext_resource type=\"Resource\" path=\"res://Asset/Config/Mower/Config/MowerDefault.tres\" id=\"5_mower\"]\n" : "");
		string text2 = characterConfigInfo.ScriptClass switch
		{
			"TowerDefenseZombieConfig" => "hitpoints = 270.0\nhomeWorld = 1\nweight = 1000\nwavePointCost = 100\nattack = 100.0\npreview = true\n", 
			"TowerDefenseMowerConfig" => "mowerConfig = ExtResource(\"5_mower\")\nhitpoints = 300.0\nhomeWorld = 1\ncost = 0\npacketCooldown = 80.0\nstartingCooldown = 30.0\n", 
			"TowerDefenseGravestoneConfig" => "hitpoints = 900.0\nhomeWorld = 1\ncost = 25\npacketCooldown = 30.0\nstartingCooldown = 15.0\nisChests = true\n", 
			"TowerDefenseCraterConfig" => "hitpoints = 300.0\nhomeWorld = 1\ncost = 0\npacketCooldown = 0.0\ndieDownTime = 30.0\ndieDownFliters = Array[String]([\"CraterWhole\", \"CraterHalf\"])\n", 
			"TowerDefenseVaseConfig" => "hitpoints = 300.0\nhomeWorld = 1\ncost = 50\npacketCooldown = 30.0\nstartingCooldown = 15.0\ntype = 0\n", 
			"TowerDefenseItemConfig" => (templateId == "character-scene-prop") ? "hitpoints = 300.0\nhomeWorld = 1\ncost = 0\npacketCooldown = 0.0\nisLadder = true\nisShield = false\n" : "hitpoints = 300.0\nhomeWorld = 1\ncost = 0\npacketCooldown = 0.0\nisLadder = false\nisShield = false\n", 
			_ => "hitpoints = 300.0\nhomeWorld = 1\ncost = 100\npacketCooldown = 5.0\ncanImitate = true\ncanCopy = true\ncanUsePlantfood = true\n", 
		};
		return $"[gd_resource type=\"Resource\" script_class=\"{characterConfigInfo.ScriptClass}\" format=3]\n\n[ext_resource type=\"Script\" uid=\"{characterConfigInfo.ScriptUid}\" path=\"res://{characterConfigInfo.ScriptPath}\" id=\"1_config_script\"]\n" + "[ext_resource type=\"Resource\" path=\"../DamagePoint/DamagePointData.tres\" id=\"2_damage_point\"]\n[ext_resource type=\"Resource\" path=\"../Armor/ArmorData.tres\" id=\"3_armor\"]\n[ext_resource type=\"Resource\" path=\"../Custom/CustomData.tres\" id=\"4_custom\"]\n" + text + "\n[resource]\nscript = ExtResource(\"1_config_script\")\nresource_name = \"" + EscapeGodotString(displayName) + "\"\nname = \"" + EscapeGodotString(safeName) + "\"\ndamagePointData = ExtResource(\"2_damage_point\")\narmorData = ExtResource(\"3_armor\")\ncustomData = ExtResource(\"4_custom\")\n" + text2 + "metadata/mod_display_name = \"" + EscapeGodotString(displayName) + "\"\n";
	}

	private static string GetCharacterBaseScenePath(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => "res://Prefab/TowerDefense/Character/TowerDefenseZombie.tscn", 
			"character-scene-vase" => "res://Prefab/TowerDefense/Character/TowerDefenseVase.tscn", 
			"character-scene-mower" => "res://Prefab/TowerDefense/Character/TowerDefenseMower.tscn", 
			"character-scene-item" => "res://Prefab/TowerDefense/Character/TowerDefenseItem.tscn", 
			"character-scene-grave" => "res://Prefab/TowerDefense/Character/TowerDefenseGravestone.tscn", 
			"character-scene-crater" => "res://Prefab/TowerDefense/Character/TowerDefenseCrater.tscn", 
			"character-scene-prop" => "res://Prefab/TowerDefense/Character/TowerDefenseItem.tscn", 
			_ => "res://Prefab/TowerDefense/Character/TowerDefensePlant.tscn", 
		};
	}

	private static string GetCharacterBaseClassName(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => "TowerDefenseZombie", 
			"character-scene-vase" => "TowerDefenseVase", 
			"character-scene-mower" => "TowerDefenseMower", 
			"character-scene-item" => "TowerDefenseItem", 
			"character-scene-grave" => "TowerDefenseGravestone", 
			"character-scene-crater" => "TowerDefenseCrater", 
			"character-scene-prop" => "TowerDefenseItem", 
			_ => "TowerDefensePlant", 
		};
	}

	private static string GetCharacterCategory(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => "Zombie", 
			"character-scene-vase" => "Vase", 
			"character-scene-mower" => "Mower", 
			"character-scene-item" => "Item", 
			"character-scene-grave" => "Grave", 
			"character-scene-crater" => "Crater", 
			"character-scene-prop" => "Prop", 
			_ => "Plant", 
		};
	}

	private static CharacterConfigInfo GetCharacterConfigInfo(string templateId)
	{
		return templateId switch
		{
			"character-scene-zombie" => new CharacterConfigInfo("TowerDefenseZombieConfig", "uid://dvtewieu2qn5h", "Resource/TowerDefense/Character/Config/TowerDefenseZombieConfig.cs"), 
			"character-scene-vase" => new CharacterConfigInfo("TowerDefenseVaseConfig", "uid://c8namxvtago26", "Resource/TowerDefense/Character/Config/TowerDefenseVaseConfig.cs"), 
			"character-scene-mower" => new CharacterConfigInfo("TowerDefenseMowerConfig", "uid://cavoywiynfw7j", "Resource/TowerDefense/Character/Config/TowerDefenseMowerConfig.cs"), 
			"character-scene-item" => new CharacterConfigInfo("TowerDefenseItemConfig", "uid://dl0ldh01lcm7x", "Resource/TowerDefense/Character/Config/TowerDefenseItemConfig.cs"), 
			"character-scene-grave" => new CharacterConfigInfo("TowerDefenseGravestoneConfig", "uid://capir5nl3xev4", "Resource/TowerDefense/Character/Config/TowerDefenseGravestoneConfig.cs"), 
			"character-scene-crater" => new CharacterConfigInfo("TowerDefenseCraterConfig", "uid://dbqj8io730yol", "Resource/TowerDefense/Character/Config/TowerDefenseCraterConfig.cs"), 
			"character-scene-prop" => new CharacterConfigInfo("TowerDefenseItemConfig", "uid://dl0ldh01lcm7x", "Resource/TowerDefense/Character/Config/TowerDefenseItemConfig.cs"), 
			_ => new CharacterConfigInfo("TowerDefensePlantConfig", "uid://deeto5x21j3q", "Resource/TowerDefense/Character/Config/TowerDefensePlantConfig.cs"), 
		};
	}

	private static string SanitizeCSharpIdentifier(string name)
	{
		string text = XWTemplateLibrary.SanitizeName(name);
		if (!char.IsLetter(text[0]) && text[0] != '_')
		{
			return "C" + text;
		}
		return text;
	}

	private static string EscapeGodotString(string value)
	{
		return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
	}

	private static string NormalizePath(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return path.Replace('\\', '/');
		}
		return "";
	}
}
