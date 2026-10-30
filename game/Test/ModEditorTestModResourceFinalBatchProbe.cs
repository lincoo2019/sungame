using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModResourceFinalBatchProbe : Node
{
	private sealed class ExampleSpec
	{
		public string TemplateId { get; init; } = "";

		public string TechnicalName { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string ExpectedClass { get; init; } = "";

		public string ExpectedCategory { get; init; } = "";

		public string ExpectedDockKey { get; init; } = "";
	}

	private static readonly ExampleSpec[] Examples = new ExampleSpec[13]
	{
		new ExampleSpec
		{
			TemplateId = "character-event-hurt-resource",
			TechnicalName = "ExampleDirectHurtEvent",
			DisplayName = "示例直接伤害事件",
			ExpectedClass = "TowerDefenseCharacterEventHurt",
			ExpectedCategory = "CharacterCombat",
			ExpectedDockKey = "character_combat_editor"
		},
		new ExampleSpec
		{
			TemplateId = "character-event-config-hurt-resource",
			TechnicalName = "ExampleConfiguredHurtEvent",
			DisplayName = "示例参数伤害事件",
			ExpectedClass = "TowerDefenseCharacterEventHurtWithConfig",
			ExpectedCategory = "CharacterCombat",
			ExpectedDockKey = "character_combat_editor"
		},
		new ExampleSpec
		{
			TemplateId = "character-event-add-buff-resource",
			TechnicalName = "ExampleAddBuffEvent",
			DisplayName = "示例附加状态事件",
			ExpectedClass = "TowerDefenseCharacterEventAddBuff",
			ExpectedCategory = "CharacterCombat",
			ExpectedDockKey = "character_combat_editor"
		},
		new ExampleSpec
		{
			TemplateId = "character-event-random-resource",
			TechnicalName = "ExampleRandomConditionEvent",
			DisplayName = "示例概率事件",
			ExpectedClass = "TowerDefenseCharacterEventConditionRandom",
			ExpectedCategory = "CharacterCombat",
			ExpectedDockKey = "character_combat_editor"
		},
		new ExampleSpec
		{
			TemplateId = "character-event-lucky-draw-resource",
			TechnicalName = "ExampleLuckyDrawEvent",
			DisplayName = "示例权重抽选事件",
			ExpectedClass = "TowerDefenseCharacterEventLuckyDraw",
			ExpectedCategory = "CharacterCombat",
			ExpectedDockKey = "character_combat_editor"
		},
		new ExampleSpec
		{
			TemplateId = "character-zombie",
			TechnicalName = "ExampleZombieDefinition",
			DisplayName = "示例僵尸配置",
			ExpectedClass = "TowerDefenseZombieConfig",
			ExpectedCategory = "Character",
			ExpectedDockKey = "character_editor"
		},
		new ExampleSpec
		{
			TemplateId = "simple-resource",
			TechnicalName = "ExampleSimpleResource",
			DisplayName = "示例通用资源",
			ExpectedClass = "Resource",
			ExpectedCategory = "General",
			ExpectedDockKey = "resource_editor"
		},
		new ExampleSpec
		{
			TemplateId = "tool-event-resource",
			TechnicalName = "ExampleMowerSunEvent",
			DisplayName = "示例小推车产阳光事件",
			ExpectedClass = "MowerEventCreateSunConfig",
			ExpectedCategory = "ToolEvent",
			ExpectedDockKey = "tool_event_editor"
		},
		new ExampleSpec
		{
			TemplateId = "tutorial-condition-resource",
			TechnicalName = "ExampleCharacterCountCondition",
			DisplayName = "示例角色数量条件",
			ExpectedClass = "TutorialConditionCheckCharaterNum",
			ExpectedCategory = "Tutorial",
			ExpectedDockKey = "tutorial_editor"
		},
		new ExampleSpec
		{
			TemplateId = "tutorial-sun-condition-resource",
			TechnicalName = "ExampleSunCollectCondition",
			DisplayName = "示例阳光收集条件",
			ExpectedClass = "TutorialConditionCheckSunCollect",
			ExpectedCategory = "Tutorial",
			ExpectedDockKey = "tutorial_editor"
		},
		new ExampleSpec
		{
			TemplateId = "tutorial-step-resource",
			TechnicalName = "ExampleTutorialStep",
			DisplayName = "示例教程步骤",
			ExpectedClass = "TutorialStepConfig",
			ExpectedCategory = "Tutorial",
			ExpectedDockKey = "tutorial_editor"
		},
		new ExampleSpec
		{
			TemplateId = "tutorial-resource",
			TechnicalName = "ExampleTutorial",
			DisplayName = "示例基础教程",
			ExpectedClass = "TutorialConfig",
			ExpectedCategory = "Tutorial",
			ExpectedDockKey = "tutorial_editor"
		},
		new ExampleSpec
		{
			TemplateId = "unlock-condition-resource",
			TechnicalName = "ExampleLevelUnlockCondition",
			DisplayName = "示例关卡完成解锁条件",
			ExpectedClass = "UnlockConditionLevelFinishConfig",
			ExpectedCategory = "UnlockCondition",
			ExpectedDockKey = "unlock_condition_editor"
		}
	};

	private static readonly HashSet<string> RemainingSceneTemplateIds = new HashSet<string>(StringComparer.Ordinal) { "character-scene-crater", "character-scene-grave", "character-scene-item", "character-scene-mower", "character-scene-prop", "character-scene-vase", "character-scene-zombie" };

	private static readonly HashSet<string> PostHistoricalTemplateIds = new HashSet<string>(StringComparer.Ordinal) { "state-property-guard-resource", "expression-guard-resource", "state-active-guard-resource", "all-of-guard-resource", "any-of-guard-resource", "not-guard-resource", "debug-entry-csharp", "animation-atlas-profile-resource" };

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool projectLoaded = false;
		bool created = true;
		bool stableNames = true;
		bool chineseNames = true;
		bool typedResources = true;
		bool eventTopology = false;
		bool tutorialTopology = false;
		bool zombieReferences = false;
		bool simpleResource = false;
		bool manifestCounts = false;
		bool resourcesOpened = true;
		bool routesCorrect = true;
		bool simpleDirectProperty = false;
		bool inspectorUntouched = true;
		bool saveReload = false;
		bool diskReload = false;
		bool completionMatrix = false;
		int createdCount = 0;
		int openedCount = 0;
		int routedCount = 0;
		int manifestScripts = -1;
		int manifestResources = -1;
		int manifestBlueprints = -1;
		int manifestTranslations = -1;
		int strongCount = 0;
		int missingCount = 0;
		try
		{
			string projectRoot = Normalize(System.Environment.GetEnvironmentVariable("PVZHE_TEST_MOD_ROOT"));
			Require(!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot), "没有通过 PVZHE_TEST_MOD_ROOT 指定可用的外部测试 Mod 工程。");
			string projectFile = Directory.GetFiles(projectRoot, "*.pvzmodeproject", SearchOption.TopDirectoryOnly).SingleOrDefault();
			Require(!string.IsNullOrWhiteSpace(projectFile), "外部测试 Mod 工程文件不存在或数量不唯一。");
			ModEditorManager modEditorManager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(modEditorManager))
				{
					AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(modEditorManager), "无法实例化 ModEditorManager。");
			await WaitFrames(2);
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = true
			});
			Input.ParseInputEvent(new InputEventKey
			{
				Keycode = Key.F3,
				PhysicalKeycode = Key.F3,
				Pressed = false
			});
			f3 = await WaitForEditor(900);
			Require(f3, "F3 未能初始化 Mod 编辑器。");
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			(modEditorPanel?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			ModProject modProject = ModProject.Load(projectFile);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			projectLoaded = modProject != null && GodotObject.IsInstanceValid(modEditorPanel) && (bool)(method?.Invoke(modEditorPanel, new object[1] { modProject }) ?? ((object)false));
			Require(projectLoaded, "无法通过真实 Mod 编辑器入口载入外部测试 Mod。");
			await WaitFrames(8);
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			int num = xWModManifest?.Scripts?.Count ?? (-1);
			int num2 = xWModManifest?.Resources?.Count ?? (-1);
			int num3 = xWModManifest?.Blueprints?.Count ?? (-1);
			int num4 = xWModManifest?.Translations?.Count ?? (-1);
			bool baselineHealthy = num >= 11 && num2 >= 62 && num3 >= 1 && num4 >= 1 && ManifestPathsAreHealthy(projectRoot, xWModManifest);
			Require(baselineHealthy, "测试 Mod 清单低于第四批所需基线，或存在缺失/重复路径。");
			Node inspectorSentinel = new Node
			{
				Name = "TestModResourceFinalBatchInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWTemplateLibrary library = new XWTemplateLibrary();
			bool flag = Examples.All(delegate(ExampleSpec exampleSpec2)
			{
				XWTemplateLibrary.TemplateInfo templateInfo2 = library.FindTemplate(exampleSpec2.TemplateId);
				return templateInfo2 != null && File.Exists(Normalize(Path.Combine(projectRoot, templateInfo2.DefaultFolder, exampleSpec2.TechnicalName + templateInfo2.FileExtension)));
			});
			System.Collections.Generic.Dictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> createdResources = new System.Collections.Generic.Dictionary<string, (ExampleSpec, string, Resource)>(StringComparer.Ordinal);
			List<string> list = new List<string>();
			ExampleSpec[] examples = Examples;
			foreach (ExampleSpec exampleSpec in examples)
			{
				XWTemplateLibrary.TemplateInfo templateInfo = library.FindTemplate(exampleSpec.TemplateId);
				if (templateInfo == null)
				{
					created = false;
					Require(condition: false, "找不到资源示例模板：" + exampleSpec.TemplateId);
					continue;
				}
				string text = Normalize(Path.Combine(projectRoot, templateInfo.DefaultFolder, exampleSpec.TechnicalName + templateInfo.FileExtension));
				XWTemplateLibrary.TemplateCreateResult templateCreateResult = (flag ? null : library.CreateFromTemplate(exampleSpec.TemplateId, projectRoot, exampleSpec.TechnicalName, exampleSpec.DisplayName, overwrite: true));
				bool flag2 = (flag ? File.Exists(text) : (templateCreateResult.Success && SamePath(templateCreateResult.CreatedPath, text) && templateCreateResult.CreatedPaths.Count == 1 && File.Exists(text)));
				created = created && flag2;
				if (flag2)
				{
					createdCount++;
				}
				Require(flag2, "创建或读取资源示例失败：" + exampleSpec.TemplateId + " -> " + (templateCreateResult?.Error ?? "磁盘资源不存在"));
				if (flag2)
				{
					string text2 = File.ReadAllText(text);
					bool flag3 = Path.GetFileNameWithoutExtension(text) == exampleSpec.TechnicalName && !XWTemplatePresentation.HasChineseText(Path.GetRelativePath(projectRoot, text).Replace('\\', '/')) && !text2.Contains("${", StringComparison.Ordinal);
					stableNames = stableNames && flag3;
					Require(flag3, "资源示例的稳定技术名被中文展示名污染：" + exampleSpec.TemplateId);
					Resource resource = LoadResource(text, ResourceLoader.CacheMode.Reuse);
					bool flag4 = GodotObject.IsInstanceValid(resource) && (resource.GetType().Name == exampleSpec.ExpectedClass || resource.IsClass(exampleSpec.ExpectedClass));
					typedResources = typedResources && flag4;
					Require(flag4, $"资源示例类型错误：{exampleSpec.TemplateId}，期望 {exampleSpec.ExpectedClass}，实际 {resource?.GetType().Name ?? "null"}");
					if (flag4)
					{
						bool flag5 = resource.ResourceName == exampleSpec.DisplayName && XWTemplatePresentation.HasChineseText(exampleSpec.DisplayName);
						chineseNames = chineseNames && flag5;
						Require(flag5, "资源示例没有保存中文 resource_name：" + exampleSpec.TemplateId);
						createdResources[exampleSpec.TemplateId] = (exampleSpec, text, resource);
						list.Add(text);
					}
				}
			}
			if (flag)
			{
				System.Collections.Generic.Dictionary<string, Resource> resources = createdResources.ToDictionary<KeyValuePair<string, (ExampleSpec, string, Resource)>, string, Resource>((KeyValuePair<string, (ExampleSpec Spec, string Path, Resource Resource)> entry) => entry.Key, (KeyValuePair<string, (ExampleSpec Spec, string Path, Resource Resource)> entry) => entry.Value.Resource, StringComparer.Ordinal);
				eventTopology = ValidateCharacterEventTopology(resources);
				tutorialTopology = ValidateTutorialTopology(resources);
				zombieReferences = ValidateZombieReferences(resources);
				simpleResource = ValidateSimpleResource(createdResources);
				saveReload = ValidatePersistedTopologyFiles(createdResources) && ValidateStandaloneValues(resources);
				diskReload = eventTopology && tutorialTopology && zombieReferences && simpleResource && saveReload;
			}
			else
			{
				eventTopology = LinkCharacterEvents(projectRoot, createdResources);
				tutorialTopology = LinkTutorialResources(createdResources);
				zombieReferences = LinkZombieData(projectRoot, createdResources);
				simpleResource = ConfigureSimpleResource(createdResources);
				ConfigureStandaloneValues(createdResources);
				System.Collections.Generic.Dictionary<string, Resource> resources2 = createdResources.ToDictionary<KeyValuePair<string, (ExampleSpec, string, Resource)>, string, Resource>((KeyValuePair<string, (ExampleSpec Spec, string Path, Resource Resource)> entry) => entry.Key, (KeyValuePair<string, (ExampleSpec Spec, string Path, Resource Resource)> entry) => entry.Value.Resource, StringComparer.Ordinal);
				saveReload = ValidatePersistedTopologyFiles(createdResources) && ValidateReloadedTopologies(resources2) && ValidateStandaloneValues(resources2);
			}
			Require(eventTopology, "角色事件没有形成攻击、状态、概率和权重引用拓扑。");
			Require(tutorialTopology, "教程资源没有形成条件、步骤和根教程引用拓扑。");
			Require(zombieReferences, "独立僵尸配置没有连接护甲、外观和受伤点数据。");
			Require(simpleResource, "通用资源不是无 script_class 的中文元数据资源。");
			Require(saveReload, "资源示例保存后的磁盘引用拓扑不完整。");
			bool flag6 = XWModManifestSyncService.RegisterPaths(projectRoot, list);
			bool flag7 = XWModManifestSyncService.SyncProject(projectRoot);
			XWModManifest manifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			manifestScripts = manifest?.Scripts?.Count ?? (-1);
			manifestResources = manifest?.Resources?.Count ?? (-1);
			manifestBlueprints = manifest?.Blueprints?.Count ?? (-1);
			manifestTranslations = manifest?.Translations?.Count ?? (-1);
			manifestCounts = (flag6 || flag7 || File.Exists(Path.Combine(projectRoot, "mod.json"))) && ManifestMeetsFloor(manifest, Math.Max(11, num), Math.Max(75, num2), Math.Max(1, num3), Math.Max(1, num4)) && ManifestPathsAreHealthy(projectRoot, manifest) && list.All((string path2) => ManifestContainsResource(projectRoot, manifest, path2));
			Require(manifestCounts, "第四批清单数量倒退、生成资源未登记，或存在缺失/重复路径。");
			XWFileSystemPanel fileSystemPanel = XWFileSystemPanel.Instance;
			XWFileSystemList fileList = fileSystemPanel?.GetNodeOrNull<XWFileSystemList>("%FileList");
			Require(GodotObject.IsInstanceValid(fileSystemPanel) && GodotObject.IsInstanceValid(fileList), "外部测试 Mod 的真实文件列表不可用。");
			XWFileSystem.GetSingleton().SetProjectFolderPath(projectRoot);
			XWFileSystem.GetSingleton().ScanChanges();
			fileSystemPanel.NavigateToProject(projectRoot);
			await WaitFrames(8);
			foreach (var (spec, path, resource2) in createdResources.Values)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				XWVisualEditorDescriptor descriptor;
				bool flag8 = XWResourceEditorRegistry.TryGetEditor(resource2, path, out descriptor) && descriptor.Category == spec.ExpectedCategory && descriptor.DockKey == spec.ExpectedDockKey && (spec.TemplateId == "simple-resource" || descriptor.Category != "General");
				routesCorrect = routesCorrect && flag8;
				if (flag8)
				{
					routedCount++;
				}
				Require(flag8, $"资源没有进入预期可视编辑器：{spec.TemplateId} -> {descriptor?.Category ?? "无"}/{descriptor?.DockKey ?? "无"}");
				if (!flag8)
				{
					continue;
				}
				fileSystemPanel.NavigateToPath(path);
				await WaitFrames(3);
				int[] selectedItems = fileList.GetSelectedItems();
				bool flag9 = selectedItems.Length == 1 && selectedItems[0] >= 0 && selectedItems[0] < fileList.ItemCount;
				resourcesOpened = resourcesOpened && flag9;
				Require(flag9, "资源没有出现在真实文件列表：" + spec.TemplateId);
				if (flag9)
				{
					fileList.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedItems[0]);
					XWGenericVisualResourceEditor visualEditor = await WaitForResourceEditor(spec.ExpectedDockKey, path, 360);
					await WaitFrames(2);
					bool flag10 = GodotObject.IsInstanceValid(visualEditor) && string.Equals(XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey, spec.ExpectedDockKey, StringComparison.Ordinal);
					resourcesOpened = resourcesOpened && flag10;
					if (flag10)
					{
						openedCount++;
					}
					Require(flag10, "资源未在主面板打开：" + spec.TemplateId);
					if (spec.TemplateId == "simple-resource" && flag10)
					{
						simpleDirectProperty = visualEditor.DirectEditablePropertyCount > 0 && visualEditor.DirectMountedPropertyCount == visualEditor.DirectEditablePropertyCount && visualEditor.DirectMissingPropertyCount == 0 && visualEditor.FindChild("Direct_resource_name", recursive: true, owned: false) != null;
						Require(simpleDirectProperty, "通用资源没有在主面板显示完整直接属性卡。");
					}
					bool flag11 = IsInspectorSentinel(inspectorSentinel) && visualEditor?.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
					inspectorUntouched = inspectorUntouched && flag11;
					Require(flag11, "资源错误占用了 Inspector：" + spec.TemplateId);
				}
			}
			HashSet<string> hashSet = library.Templates.Select((XWTemplateLibrary.TemplateInfo template) => template.Id).ToHashSet<string>(StringComparer.Ordinal);
			HashSet<string> hashSet2 = Examples.Select((ExampleSpec exampleSpec2) => exampleSpec2.TemplateId).ToHashSet<string>(StringComparer.Ordinal);
			bool flag12 = RemainingSceneTemplateIds.Count == 7 && RemainingSceneTemplateIds.All(hashSet.Contains) && PostHistoricalTemplateIds.Count == 8 && PostHistoricalTemplateIds.All(hashSet.Contains) && hashSet2.Count == 13 && hashSet2.All(hashSet.Contains) && !hashSet2.Overlaps(RemainingSceneTemplateIds) && !hashSet2.Overlaps(PostHistoricalTemplateIds) && !RemainingSceneTemplateIds.Overlaps(PostHistoricalTemplateIds);
			strongCount = hashSet.Count - RemainingSceneTemplateIds.Count - PostHistoricalTemplateIds.Count;
			missingCount = RemainingSceneTemplateIds.Count + PostHistoricalTemplateIds.Count;
			completionMatrix = baselineHealthy && library.Templates.Count == 92 && hashSet.Count == 92 && flag12 && createdCount == Examples.Length && typedResources && eventTopology && tutorialTopology && zombieReferences && simpleResource && saveReload && manifestCounts && strongCount == 77 && missingCount == 15;
			Require(completionMatrix, "实际测试 Mod 完成矩阵不是 strong=77/92、missing=15/92。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_RESOURCE_FINAL_BATCH_PROBE] f3={f3} projectLoaded={projectLoaded} created={created} stableNames={stableNames} chineseNames={chineseNames} typedResources={typedResources} eventTopology={eventTopology} tutorialTopology={tutorialTopology} zombieReferences={zombieReferences} simpleResource={simpleResource} manifestCounts={manifestCounts} resourcesOpened={resourcesOpened} routesCorrect={routesCorrect} simpleDirectProperty={simpleDirectProperty} inspectorUntouched={inspectorUntouched} saveReload={saveReload} diskReload={diskReload} completionMatrix={completionMatrix} createdCount={createdCount}/{Examples.Length} openedCount={openedCount}/{Examples.Length} routedCount={routedCount}/{Examples.Length} manifestScripts={manifestScripts} manifestResources={manifestResources} manifestBlueprints={manifestBlueprints} manifestTranslations={manifestTranslations} strong={strongCount}/92 missing={missingCount}/92 failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_RESOURCE_FINAL_BATCH_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static bool LinkCharacterEvents(string projectRoot, IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		TowerDefenseCharacterEventHurt towerDefenseCharacterEventHurt = Get<TowerDefenseCharacterEventHurt>(resources, "character-event-hurt-resource");
		TowerDefenseCharacterEventHurtWithConfig towerDefenseCharacterEventHurtWithConfig = Get<TowerDefenseCharacterEventHurtWithConfig>(resources, "character-event-config-hurt-resource");
		TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = Get<TowerDefenseCharacterEventAddBuff>(resources, "character-event-add-buff-resource");
		TowerDefenseCharacterEventConditionRandom towerDefenseCharacterEventConditionRandom = Get<TowerDefenseCharacterEventConditionRandom>(resources, "character-event-random-resource");
		TowerDefenseCharacterEventLuckyDraw towerDefenseCharacterEventLuckyDraw = Get<TowerDefenseCharacterEventLuckyDraw>(resources, "character-event-lucky-draw-resource");
		AttackConfig attackConfig = LoadResource(Path.Combine(projectRoot, "Resources/CharacterCombat/Attacks/ExampleAttack.tres"), ResourceLoader.CacheMode.Reuse) as AttackConfig;
		TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig = LoadResource(Path.Combine(projectRoot, "Resources/CharacterCombat/Buffs/ExampleFrozenBuff.tres"), ResourceLoader.CacheMode.Reuse) as TowerDefenseCharacterBuffConfig;
		TowerDefenseCharacterBuffConfig towerDefenseCharacterBuffConfig2 = LoadResource(Path.Combine(projectRoot, "Resources/CharacterCombat/Buffs/ExampleBurnBuff.tres"), ResourceLoader.CacheMode.Reuse) as TowerDefenseCharacterBuffConfig;
		if (!AllValid(towerDefenseCharacterEventHurt, towerDefenseCharacterEventHurtWithConfig, towerDefenseCharacterEventAddBuff, towerDefenseCharacterEventConditionRandom, towerDefenseCharacterEventLuckyDraw, attackConfig, towerDefenseCharacterBuffConfig, towerDefenseCharacterBuffConfig2))
		{
			return false;
		}
		towerDefenseCharacterEventHurt.num = 35.0;
		towerDefenseCharacterEventHurt.playSplatAudio = true;
		towerDefenseCharacterEventHurtWithConfig.AttackConfig = attackConfig;
		towerDefenseCharacterEventHurtWithConfig.playSplatAudio = true;
		towerDefenseCharacterEventAddBuff.buffList = new Array<TowerDefenseCharacterBuffConfig> { towerDefenseCharacterBuffConfig, towerDefenseCharacterBuffConfig2 };
		towerDefenseCharacterEventConditionRandom.percentage = 0.5;
		towerDefenseCharacterEventConditionRandom.eventList = new Array<TowerDefenseCharacterEventBase> { towerDefenseCharacterEventHurt, towerDefenseCharacterEventAddBuff };
		towerDefenseCharacterEventLuckyDraw.eventList = new Array<TowerDefenseCharacterEventLuckyDrawItem>
		{
			new TowerDefenseCharacterEventLuckyDrawItem
			{
				_event = towerDefenseCharacterEventHurt,
				weight = 70.0
			},
			new TowerDefenseCharacterEventLuckyDrawItem
			{
				_event = towerDefenseCharacterEventConditionRandom,
				weight = 30.0
			}
		};
		if (Save(towerDefenseCharacterEventHurt) && Save(towerDefenseCharacterEventHurtWithConfig) && Save(towerDefenseCharacterEventAddBuff) && Save(towerDefenseCharacterEventConditionRandom) && Save(towerDefenseCharacterEventLuckyDraw) && SameResource(towerDefenseCharacterEventHurtWithConfig.AttackConfig, attackConfig) && towerDefenseCharacterEventAddBuff.buffList.Count == 2 && towerDefenseCharacterEventConditionRandom.eventList.Count == 2 && towerDefenseCharacterEventLuckyDraw.eventList.Count == 2)
		{
			return towerDefenseCharacterEventLuckyDraw.eventList.Sum((TowerDefenseCharacterEventLuckyDrawItem item) => item.weight) == 100.0;
		}
		return false;
	}

	private static bool LinkTutorialResources(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		TutorialConditionCheckCharaterNum tutorialConditionCheckCharaterNum = Get<TutorialConditionCheckCharaterNum>(resources, "tutorial-condition-resource");
		TutorialConditionCheckSunCollect tutorialConditionCheckSunCollect = Get<TutorialConditionCheckSunCollect>(resources, "tutorial-sun-condition-resource");
		TutorialStepConfig tutorialStepConfig = Get<TutorialStepConfig>(resources, "tutorial-step-resource");
		TutorialConfig tutorialConfig = Get<TutorialConfig>(resources, "tutorial-resource");
		if (!AllValid(tutorialConditionCheckCharaterNum, tutorialConditionCheckSunCollect, tutorialStepConfig, tutorialConfig))
		{
			return false;
		}
		tutorialConditionCheckCharaterNum.characterName = "NewPlant";
		tutorialConditionCheckCharaterNum.method = ">=";
		tutorialConditionCheckCharaterNum.num = 1;
		tutorialConditionCheckSunCollect.num = 25;
		tutorialStepConfig.broadCastUse = true;
		tutorialStepConfig.broadCastConfig = new BroadCastConfig
		{
			broadCastString = "种植一株示例植物，并收集 25 点阳光。",
			broadCastTime = 5.0
		};
		tutorialStepConfig.conditionList = new Array<TutorialConditionConfig> { tutorialConditionCheckCharaterNum, tutorialConditionCheckSunCollect };
		tutorialConfig.saveKey = "ExampleTutorial";
		tutorialConfig.step = new Array<TutorialStepConfig> { tutorialStepConfig };
		if (Save(tutorialConditionCheckCharaterNum) && Save(tutorialConditionCheckSunCollect) && Save(tutorialStepConfig) && Save(tutorialConfig) && tutorialConfig.step.Count == 1 && tutorialStepConfig.conditionList.Count == 2)
		{
			return tutorialStepConfig.broadCastConfig.broadCastString.Contains("示例植物", StringComparison.Ordinal);
		}
		return false;
	}

	private static bool LinkZombieData(string projectRoot, IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		TowerDefenseZombieConfig towerDefenseZombieConfig = Get<TowerDefenseZombieConfig>(resources, "character-zombie");
		CharacterArmorData characterArmorData = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/Armor/ExampleArmorData.tres"), ResourceLoader.CacheMode.Reuse) as CharacterArmorData;
		CharacterCustomData characterCustomData = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/Custom/ExampleCustomData.tres"), ResourceLoader.CacheMode.Reuse) as CharacterCustomData;
		CharacterDamagePointData characterDamagePointData = LoadResource(Path.Combine(projectRoot, "Resources/CharacterData/DamagePoints/ExampleDamagePointData.tres"), ResourceLoader.CacheMode.Reuse) as CharacterDamagePointData;
		if (!AllValid(towerDefenseZombieConfig, characterArmorData, characterCustomData, characterDamagePointData))
		{
			return false;
		}
		towerDefenseZombieConfig.name = "ExampleZombieDefinition";
		towerDefenseZombieConfig.hitpoints = 270.0;
		towerDefenseZombieConfig.homeWorld = GeneralEnum.HOMEWORLD.MORDEN;
		towerDefenseZombieConfig.attack = 100.0;
		towerDefenseZombieConfig.preview = true;
		towerDefenseZombieConfig.weight = 1000;
		towerDefenseZombieConfig.wavePointCost = 100;
		towerDefenseZombieConfig.damagePointData = characterDamagePointData;
		towerDefenseZombieConfig.armorData = characterArmorData;
		towerDefenseZombieConfig.customData = characterCustomData;
		if (Save(towerDefenseZombieConfig) && SameResource(towerDefenseZombieConfig.damagePointData, characterDamagePointData) && SameResource(towerDefenseZombieConfig.armorData, characterArmorData))
		{
			return SameResource(towerDefenseZombieConfig.customData, characterCustomData);
		}
		return false;
	}

	private static bool ConfigureSimpleResource(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		Resource resource = Get<Resource>(resources, "simple-resource");
		if (!GodotObject.IsInstanceValid(resource) || resource.GetType() != typeof(Resource))
		{
			return false;
		}
		resource.SetMeta("mod_display_name", "示例通用资源");
		resource.SetMeta("mod_description", "用于演示通用元数据和资源引用。");
		resource.SetMeta("mod_resource_kind", "General");
		if (Save(resource) && !File.ReadAllText(resource.ResourcePath).Contains("script_class=", StringComparison.Ordinal))
		{
			return resource.ResourceName == "示例通用资源";
		}
		return false;
	}

	private static void ConfigureStandaloneValues(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		MowerEventCreateSunConfig mowerEventCreateSunConfig = Get<MowerEventCreateSunConfig>(resources, "tool-event-resource");
		UnlockConditionLevelFinishConfig unlockConditionLevelFinishConfig = Get<UnlockConditionLevelFinishConfig>(resources, "unlock-condition-resource");
		if (GodotObject.IsInstanceValid(mowerEventCreateSunConfig))
		{
			mowerEventCreateSunConfig.num = 25;
			Save(mowerEventCreateSunConfig);
		}
		if (GodotObject.IsInstanceValid(unlockConditionLevelFinishConfig))
		{
			unlockConditionLevelFinishConfig.levelSaveKey = "NewLevel";
			Save(unlockConditionLevelFinishConfig);
		}
	}

	private static bool ValidateReloadedTopologies(IReadOnlyDictionary<string, Resource> resources)
	{
		if (ValidateCharacterEventTopology(resources) && ValidateTutorialTopology(resources))
		{
			return ValidateZombieReferences(resources);
		}
		return false;
	}

	private static bool ValidateCharacterEventTopology(IReadOnlyDictionary<string, Resource> resources)
	{
		TowerDefenseCharacterEventHurt towerDefenseCharacterEventHurt = Get<TowerDefenseCharacterEventHurt>(resources, "character-event-hurt-resource");
		TowerDefenseCharacterEventHurtWithConfig towerDefenseCharacterEventHurtWithConfig = Get<TowerDefenseCharacterEventHurtWithConfig>(resources, "character-event-config-hurt-resource");
		TowerDefenseCharacterEventAddBuff towerDefenseCharacterEventAddBuff = Get<TowerDefenseCharacterEventAddBuff>(resources, "character-event-add-buff-resource");
		TowerDefenseCharacterEventConditionRandom towerDefenseCharacterEventConditionRandom = Get<TowerDefenseCharacterEventConditionRandom>(resources, "character-event-random-resource");
		TowerDefenseCharacterEventLuckyDraw towerDefenseCharacterEventLuckyDraw = Get<TowerDefenseCharacterEventLuckyDraw>(resources, "character-event-lucky-draw-resource");
		if (AllValid(towerDefenseCharacterEventHurt, towerDefenseCharacterEventHurtWithConfig, towerDefenseCharacterEventAddBuff, towerDefenseCharacterEventConditionRandom, towerDefenseCharacterEventLuckyDraw))
		{
			AttackConfig attackConfig = towerDefenseCharacterEventHurtWithConfig.AttackConfig;
			if (attackConfig != null && attackConfig.ResourcePath.EndsWith("Resources/CharacterCombat/Attacks/ExampleAttack.tres", StringComparison.OrdinalIgnoreCase) && towerDefenseCharacterEventAddBuff.buffList.Count >= 1 && towerDefenseCharacterEventAddBuff.buffList[0].ResourcePath.EndsWith("ExampleFrozenBuff.tres", StringComparison.OrdinalIgnoreCase) && towerDefenseCharacterEventConditionRandom.eventList.Count == 2 && SameResource(towerDefenseCharacterEventConditionRandom.eventList[0], towerDefenseCharacterEventHurt) && SameResource(towerDefenseCharacterEventConditionRandom.eventList[1], towerDefenseCharacterEventAddBuff) && towerDefenseCharacterEventLuckyDraw.eventList.Count == 2 && SameResource(towerDefenseCharacterEventLuckyDraw.eventList[0]._event, towerDefenseCharacterEventHurt))
			{
				return SameResource(towerDefenseCharacterEventLuckyDraw.eventList[1]._event, towerDefenseCharacterEventConditionRandom);
			}
		}
		return false;
	}

	private static bool ValidateTutorialTopology(IReadOnlyDictionary<string, Resource> resources)
	{
		TutorialConditionCheckCharaterNum tutorialConditionCheckCharaterNum = Get<TutorialConditionCheckCharaterNum>(resources, "tutorial-condition-resource");
		TutorialConditionCheckSunCollect tutorialConditionCheckSunCollect = Get<TutorialConditionCheckSunCollect>(resources, "tutorial-sun-condition-resource");
		TutorialStepConfig tutorialStepConfig = Get<TutorialStepConfig>(resources, "tutorial-step-resource");
		TutorialConfig tutorialConfig = Get<TutorialConfig>(resources, "tutorial-resource");
		if (AllValid(tutorialConditionCheckCharaterNum, tutorialConditionCheckSunCollect, tutorialStepConfig, tutorialConfig) && tutorialConfig.step.Count == 1 && SameResource(tutorialConfig.step[0], tutorialStepConfig) && tutorialStepConfig.conditionList.Count == 2 && SameResource(tutorialStepConfig.conditionList[0], tutorialConditionCheckCharaterNum) && SameResource(tutorialStepConfig.conditionList[1], tutorialConditionCheckSunCollect))
		{
			return tutorialStepConfig.broadCastConfig?.broadCastString.Contains("示例植物", StringComparison.Ordinal) ?? false;
		}
		return false;
	}

	private static bool ValidateZombieReferences(IReadOnlyDictionary<string, Resource> resources)
	{
		TowerDefenseZombieConfig towerDefenseZombieConfig = Get<TowerDefenseZombieConfig>(resources, "character-zombie");
		if (AllValid(towerDefenseZombieConfig))
		{
			CharacterArmorData armorData = towerDefenseZombieConfig.armorData;
			if (armorData != null && armorData.ResourcePath.EndsWith("ExampleArmorData.tres", StringComparison.OrdinalIgnoreCase))
			{
				CharacterCustomData customData = towerDefenseZombieConfig.customData;
				if (customData != null && customData.ResourcePath.EndsWith("ExampleCustomData.tres", StringComparison.OrdinalIgnoreCase))
				{
					return towerDefenseZombieConfig.damagePointData?.ResourcePath.EndsWith("ExampleDamagePointData.tres", StringComparison.OrdinalIgnoreCase) ?? false;
				}
			}
		}
		return false;
	}

	private static bool ValidateSimpleResource(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		Resource resource = Get<Resource>(resources, "simple-resource");
		if (GodotObject.IsInstanceValid(resource) && resource.GetType() == typeof(Resource) && resource.ResourceName == "示例通用资源" && resource.HasMeta("mod_display_name") && resource.GetMeta("mod_display_name").AsString() == "示例通用资源")
		{
			return !File.ReadAllText(resource.ResourcePath).Contains("script_class=", StringComparison.Ordinal);
		}
		return false;
	}

	private static bool ValidateStandaloneValues(IReadOnlyDictionary<string, Resource> resources)
	{
		MowerEventCreateSunConfig mowerEventCreateSunConfig = Get<MowerEventCreateSunConfig>(resources, "tool-event-resource");
		UnlockConditionLevelFinishConfig unlockConditionLevelFinishConfig = Get<UnlockConditionLevelFinishConfig>(resources, "unlock-condition-resource");
		if (AllValid(mowerEventCreateSunConfig, unlockConditionLevelFinishConfig) && mowerEventCreateSunConfig.num == 25)
		{
			return unlockConditionLevelFinishConfig.levelSaveKey == "NewLevel";
		}
		return false;
	}

	private static bool ValidatePersistedTopologyFiles(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		if (Contains("character-event-config-hurt-resource", new string[1] { "ExampleAttack.tres" }) && Contains("character-event-add-buff-resource", new string[1] { "ExampleFrozenBuff.tres" }) && Contains("character-event-random-resource", new string[2] { "ExampleDirectHurtEvent.tres", "ExampleAddBuffEvent.tres" }) && Contains("character-event-lucky-draw-resource", new string[4] { "ExampleDirectHurtEvent.tres", "ExampleRandomConditionEvent.tres", "weight = 70", "weight = 30" }) && Contains("tutorial-step-resource", new string[3] { "ExampleCharacterCountCondition.tres", "ExampleSunCollectCondition.tres", "种植一株示例植物" }) && Contains("tutorial-resource", new string[1] { "ExampleTutorialStep.tres" }) && Contains("character-zombie", new string[3] { "ExampleArmorData.tres", "ExampleCustomData.tres", "ExampleDamagePointData.tres" }) && Contains("simple-resource", new string[2] { "resource_name = \"示例通用资源\"", "metadata/mod_display_name = \"示例通用资源\"" }))
		{
			return Contains("unlock-condition-resource", new string[1] { "levelSaveKey = \"NewLevel\"" });
		}
		return false;
		bool Contains(string templateId, string[] expected)
		{
			if (!resources.TryGetValue(templateId, out (ExampleSpec, string, Resource) value) || !File.Exists(value.Item2))
			{
				return false;
			}
			string source = File.ReadAllText(value.Item2);
			return expected.All((string token) => source.Contains(token, StringComparison.Ordinal));
		}
	}

	private async Task<XWGenericVisualResourceEditor> WaitForResourceEditor(string dockKey, string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWGenericVisualResourceEditor xWGenericVisualResourceEditor = XWEditorInterface.Instance?.GetResourceEditor(dockKey) as XWGenericVisualResourceEditor;
			if (GodotObject.IsInstanceValid(xWGenericVisualResourceEditor) && SamePath(xWGenericVisualResourceEditor.ActiveResourcePath, expectedPath))
			{
				return xWGenericVisualResourceEditor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return null;
	}

	private static bool ManifestMeetsFloor(XWModManifest manifest, int scripts, int resources, int blueprints, int translations)
	{
		if (manifest != null)
		{
			List<string> scripts2 = manifest.Scripts;
			if (scripts2 != null && scripts2.Count >= scripts)
			{
				List<string> resources2 = manifest.Resources;
				if (resources2 != null && resources2.Count >= resources)
				{
					List<string> blueprints2 = manifest.Blueprints;
					if (blueprints2 != null && blueprints2.Count >= blueprints)
					{
						List<string> translations2 = manifest.Translations;
						if (translations2 == null)
						{
							return false;
						}
						return translations2.Count >= translations;
					}
				}
			}
		}
		return false;
	}

	private static bool ManifestPathsAreHealthy(string projectRoot, XWModManifest manifest)
	{
		if (manifest == null)
		{
			return false;
		}
		string[] array = (manifest.Scripts ?? new List<string>()).Concat(manifest.Resources ?? new List<string>()).Concat(manifest.Blueprints ?? new List<string>()).Concat(manifest.Translations ?? new List<string>())
			.ToArray();
		if (array.Length == array.Distinct<string>(StringComparer.OrdinalIgnoreCase).Count())
		{
			return array.All((string entry) => File.Exists(Path.Combine(projectRoot, entry)));
		}
		return false;
	}

	private static bool ManifestContainsResource(string projectRoot, XWModManifest manifest, string path)
	{
		string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(path, projectRoot);
		if (manifest == null)
		{
			return false;
		}
		return manifest.Resources?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) == true;
	}

	private static T Get<T>(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources, string templateId) where T : Resource
	{
		if (!resources.TryGetValue(templateId, out (ExampleSpec, string, Resource) value))
		{
			return null;
		}
		return value.Item3 as T;
	}

	private static T Get<T>(IReadOnlyDictionary<string, Resource> resources, string templateId) where T : Resource
	{
		if (!resources.TryGetValue(templateId, out var value))
		{
			return null;
		}
		return value as T;
	}

	private static bool AllValid(params GodotObject[] objects)
	{
		return objects.All(GodotObject.IsInstanceValid);
	}

	private static bool Save(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			return ResourceSaver.Save(resource, resource.ResourcePath, ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		return false;
	}

	private static Resource LoadResource(string path, ResourceLoader.CacheMode cacheMode)
	{
		return ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(Normalize(path)).Replace('\\', '/'), "", cacheMode);
	}

	private static bool SameResource(Resource left, Resource right)
	{
		if (GodotObject.IsInstanceValid(left) && GodotObject.IsInstanceValid(right))
		{
			return SamePath(left.ResourcePath, right.ResourcePath);
		}
		return false;
	}

	private static bool IsInspectorSentinel(Node inspectorSentinel)
	{
		if (XWEditorInterface.Instance?.GetInspector() is XWInspector xWInspector)
		{
			return xWInspector.CurrentObject == inspectorSentinel;
		}
		return true;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance != null && XWEditorInterface.Instance.GetEditorPanel() is ModEditorPanel)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private static bool SamePath(string left, string right)
	{
		return string.Equals(Normalize(left).TrimEnd('/'), Normalize(right).TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
	}

	private static string Normalize(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
	}
}
