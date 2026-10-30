using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModCSharpExamplesProbe : Node
{
	private sealed class ExampleSpec
	{
		public string TemplateId { get; init; } = "";

		public string TechnicalName { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string[] ExpectedTypes { get; init; } = Array.Empty<string>();

		public string[] ExpectedBaseTypes { get; init; } = Array.Empty<string>();

		public bool IsComponent { get; init; }
	}

	private static readonly ExampleSpec[] Examples = new ExampleSpec[9]
	{
		new ExampleSpec
		{
			TemplateId = "feature-csharp",
			TechnicalName = "ExampleBattleFeature",
			DisplayName = "示例关卡功能脚本",
			ExpectedTypes = new string[1] { "ExampleBattleFeature" },
			ExpectedBaseTypes = new string[1] { "TowerDefenseBattleFeature" }
		},
		new ExampleSpec
		{
			TemplateId = "process-csharp",
			TechnicalName = "ExampleBattleProcess",
			DisplayName = "示例关卡流程脚本",
			ExpectedTypes = new string[1] { "ExampleBattleProcess" },
			ExpectedBaseTypes = new string[1] { "TowerDefenseBattleProcess" }
		},
		new ExampleSpec
		{
			TemplateId = "character-component",
			TechnicalName = "ExampleCharacterComponent",
			DisplayName = "示例角色组件",
			ExpectedTypes = new string[2] { "ExampleCharacterComponentDefinition", "ExampleCharacterComponent" },
			ExpectedBaseTypes = new string[2] { "CharacterComponentDefinition", "CharacterComponentRuntime" },
			IsComponent = true
		},
		new ExampleSpec
		{
			TemplateId = "character-csharp",
			TechnicalName = "ExampleCharacter",
			DisplayName = "示例通用角色脚本",
			ExpectedTypes = new string[1] { "ExampleCharacter" },
			ExpectedBaseTypes = new string[1] { "TowerDefenseCharacter" }
		},
		new ExampleSpec
		{
			TemplateId = "projectile-csharp",
			TechnicalName = "ExampleProjectile",
			DisplayName = "示例子弹脚本",
			ExpectedTypes = new string[1] { "ExampleProjectile" },
			ExpectedBaseTypes = new string[1] { "TowerDefenseProjectile" }
		},
		new ExampleSpec
		{
			TemplateId = "shared-csharp",
			TechnicalName = "ExampleSharedNode",
			DisplayName = "示例共享节点脚本",
			ExpectedTypes = new string[1] { "ExampleSharedNode" },
			ExpectedBaseTypes = new string[1] { "Node" }
		},
		new ExampleSpec
		{
			TemplateId = "character-event-csharp",
			TechnicalName = "ExampleCharacterEvent",
			DisplayName = "示例角色事件脚本",
			ExpectedTypes = new string[1] { "ExampleCharacterEvent" },
			ExpectedBaseTypes = new string[1] { "TowerDefenseCharacterEventBase" }
		},
		new ExampleSpec
		{
			TemplateId = "shovel-event-csharp",
			TechnicalName = "ExampleShovelEvent",
			DisplayName = "示例铲子事件脚本",
			ExpectedTypes = new string[1] { "ExampleShovelEvent" },
			ExpectedBaseTypes = new string[1] { "ShovelEventConfig" }
		},
		new ExampleSpec
		{
			TemplateId = "card-event-csharp",
			TechnicalName = "ExampleCardEvent",
			DisplayName = "示例卡牌事件脚本",
			ExpectedTypes = new string[1] { "ExampleCardEvent" },
			ExpectedBaseTypes = new string[1] { "CardActionBehaviorDefinition" }
		}
	};

	private static readonly IReadOnlyDictionary<string, string> PriorStrongExamplePaths = new Dictionary<string, string>(StringComparer.Ordinal)
	{
		["aabb-ray-resource"] = "Resources/CollisionGeometry/ExampleCollisionRay.tres",
		["aabb-shape-resource"] = "Resources/CollisionGeometry/ExampleCollisionArea.tres",
		["adobe-animate-xfl"] = "Resources/Animations/Snowwood.tres",
		["award-settlement-resource"] = "Resources/AwardSettlements/NewAwardSettlement.tres",
		["bgm-resource"] = "Resources/BGMConfigs/NewBGM.tres",
		["blueprint"] = "Scripts/new_blueprint.tres",
		["buff-visual-resource"] = "Resources/BuffVisuals/ExampleBuffVisual.tres",
		["card-resource"] = "Resources/Cards/NewCard.tres",
		["character-armor-data-resource"] = "Resources/CharacterData/Armor/ExampleArmorData.tres",
		["character-armor-slot-resource"] = "Resources/CharacterData/Armor/ExampleArmorSlot.tres",
		["character-armor-type-resource"] = "Resources/CharacterData/Armor/ExampleArmorType.tres",
		["character-attack-resource"] = "Resources/CharacterCombat/Attacks/ExampleAttack.tres",
		["character-buff-burn-resource"] = "Resources/CharacterCombat/Buffs/ExampleBurnBuff.tres",
		["character-buff-frozen-resource"] = "Resources/CharacterCombat/Buffs/ExampleFrozenBuff.tres",
		["character-buff-hypnoses-resource"] = "Resources/CharacterCombat/Buffs/ExampleHypnosesBuff.tres",
		["character-buff-poison-resource"] = "Resources/CharacterCombat/Buffs/ExamplePoisonBuff.tres",
		["character-component-resource"] = "Resources/CharacterComponents/ExampleComponentSet.tres",
		["character-custom-config-resource"] = "Resources/CharacterData/Custom/ExampleCustomConfig.tres",
		["character-custom-data-resource"] = "Resources/CharacterData/Custom/ExampleCustomData.tres",
		["character-damage-point-config-resource"] = "Resources/CharacterData/DamagePoints/ExampleDamagePoint.tres",
		["character-damage-point-data-resource"] = "Resources/CharacterData/DamagePoints/ExampleDamagePointData.tres",
		["character-definition"] = "Resources/Characters/Plants/ExamplePlantConfig.tres",
		["character-hitbox-resource"] = "Resources/CollisionGeometry/NewHitBox.tres",
		["character-scene-plant"] = "Resources/Characters/Plants/NewPlant/Scene/NewPlant.tscn",
		["collectable-resource"] = "Resources/Collectables/NewCollectable.tres",
		["conveyor-add-packet-event-resource"] = "Resources/ConveyorEvents/ExampleAddPacketEvent.tres",
		["conveyor-packet-entry-resource"] = "Resources/PacketSpawnEntries/Conveyor/ExampleConveyorPacket.tres",
		["dialog-popup-scene"] = "Resources/Dialogs/NewDialogPopup.tscn",
		["dialog-scene"] = "Resources/Dialogs/NewDialog.tscn",
		["drop-item-resource"] = "Resources/DropItems/NewDropItem.tres",
		["falling-object-resource"] = "Resources/FallingObjects/NewFallingObject.tres",
		["level-catalog-resource"] = "Resources/LevelCatalogs/ExampleLevelCatalog.tres",
		["level-grid-wave-spawn-resource"] = "Resources/GameplayLogic/Waves/ExampleGridSpawn.tres",
		["level-packet-entry-resource"] = "Resources/PacketSpawnEntries/Level/ExampleLevelPacket.tres",
		["level-resource"] = "Resources/Levels/NewLevel.tres",
		["level-wave-manager-resource"] = "Resources/GameplayLogic/Waves/ExampleWaveManager.tres",
		["level-wave-resource"] = "Resources/GameplayLogic/Waves/ExampleWave.tres",
		["level-wave-spawn-resource"] = "Resources/GameplayLogic/Waves/ExampleLaneSpawn.tres",
		["localization-csv"] = "Localization/NewLocalization.csv",
		["map-cell-resource"] = "Resources/MapCells/ExampleMapCell.tres",
		["map-resource"] = "Resources/Maps/NewMap.tres",
		["menu-dialog-scene"] = "Resources/Dialogs/NewMenuDialog.tscn",
		["mower-resource"] = "Resources/Mowers/NewMower.tres",
		["npc-talk-resource"] = "Resources/NpcTalks/NewTalk.tres",
		["packet-bank-resource"] = "Resources/PacketBank/NewPacketBank.tres",
		["packet-cost-rule-resource"] = "Resources/CardCostRules/NewCostRule.tres",
		["packet-event-resource"] = "Resources/PacketEvents/ExamplePacketCostEvent.tres",
		["packet-override-resource"] = "Resources/CardOverrides/ExamplePacketOverride.tres",
		["projectile"] = "Resources/Projectiles/NewProjectile.tres",
		["projectile-change-resource"] = "Resources/ProjectileChanges/NewProjectileChange.tres",
		["rain-packet-entry-resource"] = "Resources/PacketSpawnEntries/Rain/ExampleRainPacket.tres",
		["shop-resource"] = "Resources/Shops/NewShop.tres",
		["shovel-resource"] = "Resources/Shovels/NewShovel.tres",
		["state-machine-resource"] = "Resources/StateMachines/ExampleStateMachine.tres",
		["survival-resource"] = "Resources/Survivals/NewSurvival.tres"
	};

	private static readonly HashSet<string> RemainingTemplateIds = new HashSet<string>(StringComparer.Ordinal)
	{
		"character-event-add-buff-resource", "character-event-config-hurt-resource", "character-event-hurt-resource", "character-event-lucky-draw-resource", "character-event-random-resource", "character-scene-crater", "character-scene-grave", "character-scene-item", "character-scene-mower", "character-scene-prop",
		"character-scene-vase", "character-scene-zombie", "character-zombie", "simple-resource", "tool-event-resource", "tutorial-condition-resource", "tutorial-resource", "tutorial-step-resource", "tutorial-sun-condition-resource", "unlock-condition-resource",
		"state-property-guard-resource", "expression-guard-resource", "state-active-guard-resource", "all-of-guard-resource", "any-of-guard-resource", "not-guard-resource", "debug-entry-csharp", "animation-atlas-profile-resource"
	};

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool projectLoaded = false;
		bool created = true;
		bool stableNames = true;
		bool chineseNames = true;
		bool eventDisplayNames = false;
		bool componentPackage = false;
		bool manifestScripts = false;
		bool manifestResources = false;
		bool manifestNotBlueprints = false;
		bool backgroundCompile = false;
		bool peBaseTypes = false;
		bool scriptsOpened = true;
		bool componentOpened = false;
		bool inspectorUntouched = true;
		bool completionMatrix = false;
		int createdCount = 0;
		int createdTypeCount = 0;
		int componentFileCount = 0;
		int scriptOpenedCount = 0;
		int expectedScriptCount = 0;
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
			Node inspectorSentinel = new Node
			{
				Name = "TestModCSharpExamplesInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
			List<(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result)> createdResults = new List<(ExampleSpec, XWTemplateLibrary.TemplateCreateResult)>();
			List<string> list = new List<string>();
			ExampleSpec[] examples = Examples;
			foreach (ExampleSpec exampleSpec in examples)
			{
				XWTemplateLibrary.TemplateInfo templateInfo = xWTemplateLibrary.FindTemplate(exampleSpec.TemplateId);
				if (templateInfo == null)
				{
					created = false;
					Require(condition: false, "找不到 C# 示例模板：" + exampleSpec.TemplateId);
					continue;
				}
				XWTemplatePresentation xWTemplatePresentation = XWTemplatePresentation.Resolve(templateInfo);
				bool flag = XWTemplatePresentation.HasChineseText(exampleSpec.DisplayName) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.DisplayNameLabel) && XWTemplatePresentation.HasChineseText(xWTemplatePresentation.CategoryLabel);
				chineseNames = chineseNames && flag;
				Require(flag, "模板没有中文可视显示名：" + exampleSpec.TemplateId);
				XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplate(exampleSpec.TemplateId, projectRoot, exampleSpec.TechnicalName, exampleSpec.DisplayName, overwrite: true);
				bool flag2 = templateCreateResult.Success && templateCreateResult.CreatedPaths.Count > 0 && templateCreateResult.CreatedPaths.All(File.Exists);
				created = created && flag2;
				if (flag2)
				{
					createdCount++;
				}
				Require(flag2, "创建 C# 示例失败：" + exampleSpec.TemplateId + " -> " + templateCreateResult.Error);
				if (flag2)
				{
					createdResults.Add((exampleSpec, templateCreateResult));
					list.AddRange(templateCreateResult.CreatedPaths);
					bool flag3 = ValidateStableTechnicalNames(projectRoot, exampleSpec, templateCreateResult);
					stableNames = stableNames && flag3;
					Require(flag3, "中文显示名污染了英文技术名或模板仍有占位符：" + exampleSpec.TemplateId);
					bool flag4 = ValidateChineseOutput(exampleSpec, templateCreateResult);
					chineseNames = chineseNames && flag4;
					Require(flag4, "生成示例没有在脚本注释或组件资源名中保留中文显示名：" + exampleSpec.TemplateId);
				}
			}
			eventDisplayNames = ValidateEventDisplayNames(createdResults);
			Require(eventDisplayNames, "角色事件或卡牌事件没有把中文显示名写入 eventName。");
			componentPackage = ValidateComponentPackage(projectRoot, createdResults, out componentFileCount);
			Require(componentPackage, "角色组件没有生成稳定命名的 Definition、Runtime、Definition.tres 三文件包。");
			bool flag5 = XWModManifestSyncService.RegisterPaths(projectRoot, list);
			bool flag6 = XWModManifestSyncService.SyncProject(projectRoot);
			XWModManifest manifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			(bool, bool, bool) tuple = ValidateManifest(projectRoot, list, manifest);
			manifestScripts = tuple.Item1;
			manifestResources = tuple.Item2;
			manifestNotBlueprints = tuple.Item3;
			manifestScripts &= flag5 || flag6 || File.Exists(Path.Combine(projectRoot, "mod.json"));
			Require(manifestScripts, "10 个 C# 文件没有全部同步到 mod.json scripts。");
			Require(manifestResources, "组件 Definition.tres 没有同步到 mod.json resources。");
			Require(manifestNotBlueprints, "第三批 C# 示例被错误归入 mod.json blueprints。");
			(completionMatrix, strongCount, missingCount) = AuditCompletionMatrix(projectRoot, xWTemplateLibrary, manifest, createdResults);
			Require(completionMatrix, "外部测试 Mod 的实际文件、清单与模板完成矩阵不是 64/92，缺失项不是 28/92。");
			Task<XWScriptCompiler.CompileResult> compileTask = XWScriptCompiler.CompileModProjectAsync(projectRoot);
			int responsiveFrames = 0;
			while (!compileTask.IsCompleted && responsiveFrames < 3600)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				responsiveFrames++;
			}
			backgroundCompile = responsiveFrames > 1;
			Require(backgroundCompile, "CompileModProjectAsync 没有保持 Godot 主线程持续响应。");
			XWScriptCompiler.CompileResult compileResult = ((!compileTask.IsCompleted) ? null : (await compileTask));
			XWScriptCompiler.CompileResult compileResult2 = compileResult;
			backgroundCompile &= compileResult2 != null && compileResult2.Success && File.Exists(compileResult2.OutputAssemblyPath);
			if (!backgroundCompile)
			{
				string text = ((compileResult2 != null && compileResult2.Diagnostics?.Count > 0) ? compileResult2.Diagnostics[0].Message : (compileResult2?.Output ?? "后台编译未在限定帧数内完成"));
				Require(condition: false, "9 类 C# 示例后台编译失败：" + text);
			}
			if (backgroundCompile)
			{
				try
				{
					peBaseTypes = ValidateCompiledBaseTypes(compileResult2.OutputAssemblyPath, Examples, out createdTypeCount);
				}
				catch (Exception ex)
				{
					Require(condition: false, "程序集 PE 元数据校验失败：" + ex.GetBaseException().Message);
				}
			}
			Require(peBaseTypes, "程序集中的 10 个生成类型没有全部继承预期游戏基类。");
			XWFileSystemPanel fileSystemPanel = XWFileSystemPanel.Instance;
			XWFileSystemList fileList = fileSystemPanel?.GetNodeOrNull<XWFileSystemList>("%FileList");
			XWScriptEditor scriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			Require(GodotObject.IsInstanceValid(fileSystemPanel) && GodotObject.IsInstanceValid(fileList) && GodotObject.IsInstanceValid(scriptEditor), "外部测试 Mod 的文件列表或 C# 脚本编辑器不可用。");
			XWFileSystem.GetSingleton().SetProjectFolderPath(projectRoot);
			fileSystemPanel.NavigateToProject(projectRoot);
			await WaitFrames(8);
			List<(ExampleSpec, string)> list2 = (from item in createdResults.SelectMany(((ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) item) => item.Result.CreatedPaths.Select((string item2) => (Spec: item.Spec, Path: item2)))
				where item.Path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
				select item).ToList();
			expectedScriptCount = list2.Count;
			foreach (var (spec, path) in list2)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				fileSystemPanel.NavigateToPath(path);
				await WaitFrames(3);
				int[] selectedItems = fileList.GetSelectedItems();
				bool flag7 = selectedItems.Length == 1 && selectedItems[0] >= 0 && selectedItems[0] < fileList.ItemCount;
				scriptsOpened = scriptsOpened && flag7;
				Require(flag7, "C# 示例没有出现在真实文件列表中：" + spec.TemplateId);
				if (flag7)
				{
					fileList.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedItems[0]);
					bool flag8 = await WaitForScript(scriptEditor, path, 240);
					bool flag9 = flag8 && XWScriptEditor.IsSupportedScriptPath(scriptEditor.GetCurrentFilePath()) && scriptEditor.GetCurrentFilePath().EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && string.Equals(XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey, "script_editor", StringComparison.Ordinal);
					scriptsOpened = scriptsOpened && flag8 && flag9;
					if (flag8)
					{
						scriptOpenedCount++;
					}
					Require(flag8, "真实文件树没有打开 C# 示例：" + spec.TemplateId);
					Require(flag9, "C# 示例没有进入仅支持 C# 的 script_editor：" + spec.TemplateId);
					XWEditorInterface.Instance.FocusPanel("script_editor");
					inspectorUntouched &= IsInspectorSentinel(inspectorSentinel);
					Require(IsInspectorSentinel(inspectorSentinel), "C# 示例错误地占用了 Inspector：" + spec.TemplateId);
				}
			}
			scriptsOpened = scriptsOpened && expectedScriptCount == 10 && scriptOpenedCount == expectedScriptCount;
			Require(scriptsOpened, "10 个生成 C# 文件没有全部通过真实文件树进入 script_editor。");
			(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) component = createdResults.FirstOrDefault(((ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) item) => item.Spec.IsComponent);
			if (component.Spec != null)
			{
				string path = component.Result.CreatedPath;
				Resource resource = ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(path).Replace('\\', '/'), "", ResourceLoader.CacheMode.Reuse);
				XWVisualEditorDescriptor descriptor;
				bool componentRoute = GodotObject.IsInstanceValid(resource) && XWResourceEditorRegistry.TryGetEditor(resource, path, out descriptor) && descriptor.Category == "CharacterComponent";
				Require(componentRoute, "组件 Definition 没有注册 CharacterComponent 可视化路由。");
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				fileSystemPanel.CompleteTemplateCreationFromTool(component.Result);
				bool flag10 = componentRoute;
				if (flag10)
				{
					flag10 = await WaitForComponentEditor(path, 3600);
				}
				componentOpened = flag10;
				Require(componentOpened, "组件 Definition 没有通过 CompleteTemplateCreationFromTool 后台编译并进入 character_component_editor。");
				inspectorUntouched &= IsInspectorSentinel(inspectorSentinel);
				Require(IsInspectorSentinel(inspectorSentinel), "组件可视化编辑器错误地占用了 Inspector。");
			}
			else
			{
				Require(condition: false, "没有可用于完成回调验收的角色组件生成结果。");
			}
		}
		catch (Exception ex2)
		{
			_failures.Add(ex2.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_CSHARP_EXAMPLES_PROBE] f3={f3} projectLoaded={projectLoaded} created={created} stableNames={stableNames} chineseNames={chineseNames} eventDisplayNames={eventDisplayNames} componentPackage={componentPackage} manifestScripts={manifestScripts} manifestResources={manifestResources} manifestNotBlueprints={manifestNotBlueprints} backgroundCompile={backgroundCompile} peBaseTypes={peBaseTypes} scriptsOpened={scriptsOpened} componentOpened={componentOpened} inspectorUntouched={inspectorUntouched} completionMatrix={completionMatrix} createdCount={createdCount}/{Examples.Length} componentFileCount={componentFileCount}/3 scriptOpenedCount={scriptOpenedCount}/{expectedScriptCount} createdTypes={createdTypeCount}/10 strong={strongCount}/92 missing={missingCount}/92 failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_CSHARP_EXAMPLES_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static (bool Complete, int StrongCount, int MissingCount) AuditCompletionMatrix(string projectRoot, XWTemplateLibrary library, XWModManifest manifest, IEnumerable<(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result)> createdResults)
	{
		HashSet<string> hashSet = library.Templates.Select((XWTemplateLibrary.TemplateInfo template) => template.Id).ToHashSet<string>(StringComparer.Ordinal);
		HashSet<string> strongIds = new HashSet<string>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> priorStrongExamplePath in PriorStrongExamplePaths)
		{
			priorStrongExamplePath.Deconstruct(out var key, out var value);
			string item = key;
			string text = value;
			string path = Normalize(Path.Combine(projectRoot, text));
			if (hashSet.Contains(item) && File.Exists(path) && ManifestContains(manifest, text))
			{
				strongIds.Add(item);
			}
		}
		foreach (var (exampleSpec, templateCreateResult) in createdResults)
		{
			if (templateCreateResult != null && templateCreateResult.Success && templateCreateResult.CreatedPaths.Count > 0 && templateCreateResult.CreatedPaths.All((string path2) => File.Exists(path2) && ManifestContains(manifest, XWModManifestSyncService.ToProjectRelativeManifestPath(path2, projectRoot))))
			{
				strongIds.Add(exampleSpec.TemplateId);
			}
		}
		HashSet<string> hashSet2 = hashSet.Where((string templateId) => !strongIds.Contains(templateId)).ToHashSet<string>(StringComparer.Ordinal);
		int count = strongIds.Count;
		int count2 = hashSet2.Count;
		return (Complete: library.Templates.Count == 92 && hashSet.Count == 92 && PriorStrongExamplePaths.Count == 55 && Examples.Length == 9 && RemainingTemplateIds.Count == 28 && count == 64 && count2 == 28 && hashSet2.SetEquals(RemainingTemplateIds), StrongCount: count, MissingCount: count2);
	}

	private static bool ManifestContains(XWModManifest manifest, string relativePath)
	{
		if (manifest == null || string.IsNullOrWhiteSpace(relativePath))
		{
			return false;
		}
		relativePath = relativePath.Replace('\\', '/');
		string text = Path.GetExtension(relativePath).ToLowerInvariant();
		IEnumerable<string> source;
		if (!(text == ".cs"))
		{
			if (text == ".csv")
			{
				IEnumerable<string> translations = manifest.Translations;
				source = translations ?? Enumerable.Empty<string>();
			}
			else if (relativePath.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase))
			{
				IEnumerable<string> translations = manifest.Blueprints;
				source = translations ?? Enumerable.Empty<string>();
			}
			else
			{
				IEnumerable<string> translations = manifest.Resources;
				source = translations ?? Enumerable.Empty<string>();
			}
		}
		else
		{
			IEnumerable<string> translations = manifest.Scripts;
			source = translations ?? Enumerable.Empty<string>();
		}
		return source.Any((string entry) => string.Equals(entry, relativePath, StringComparison.OrdinalIgnoreCase));
	}

	private static bool ValidateEventDisplayNames(IEnumerable<(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result)> createdResults)
	{
		foreach (var createdResult in createdResults)
		{
			ExampleSpec item = createdResult.Spec;
			XWTemplateLibrary.TemplateCreateResult item2 = createdResult.Result;
			string templateId = item.TemplateId;
			bool flag = ((templateId == "character-event-csharp" || templateId == "card-event-csharp") ? true : false);
			if (flag && !File.ReadAllText(item2.CreatedPath).Contains("eventName = \"" + item.DisplayName + "\";", StringComparison.Ordinal))
			{
				return false;
			}
		}
		return createdResults.Count(delegate((ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) tuple)
		{
			string templateId2 = tuple.Spec.TemplateId;
			return (templateId2 == "character-event-csharp" || templateId2 == "card-event-csharp") ? true : false;
		}) == 2;
	}

	private static bool ValidateComponentPackage(string projectRoot, IEnumerable<(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result)> createdResults, out int componentFileCount)
	{
		string path = Normalize(Path.Combine(projectRoot, "Resources", "CharacterComponents", "ExampleCharacterComponent"));
		string[] array = new string[3]
		{
			Normalize(Path.Combine(path, "ExampleCharacterComponentDefinition.cs")),
			Normalize(Path.Combine(path, "ExampleCharacterComponent.cs")),
			Normalize(Path.Combine(path, "ExampleCharacterComponentDefinition.tres"))
		};
		(ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) component = createdResults.FirstOrDefault(((ExampleSpec Spec, XWTemplateLibrary.TemplateCreateResult Result) item) => item.Spec.IsComponent);
		componentFileCount = array.Count(delegate(string text)
		{
			if (File.Exists(text))
			{
				XWTemplateLibrary.TemplateCreateResult item = component.Result;
				if (item == null)
				{
					return false;
				}
				return item.CreatedPaths?.Any((string created) => SamePath(created, text)) == true;
			}
			return false;
		});
		if (component.Spec != null && component.Result.CreatedPaths.Count == 3)
		{
			return componentFileCount == array.Length;
		}
		return false;
	}

	private static bool ValidateStableTechnicalNames(string projectRoot, ExampleSpec spec, XWTemplateLibrary.TemplateCreateResult result)
	{
		if (XWTemplatePresentation.HasChineseText(spec.TechnicalName) || result.CreatedPaths.Any((string path) => XWTemplatePresentation.HasChineseText(Path.GetRelativePath(projectRoot, path).Replace('\\', '/'))))
		{
			return false;
		}
		foreach (string item in result.CreatedPaths.Where((string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)))
		{
			if (File.ReadAllText(item).Contains("${", StringComparison.Ordinal))
			{
				return false;
			}
		}
		string[] expectedTypes = spec.ExpectedTypes;
		foreach (string typeName in expectedTypes)
		{
			if (!result.CreatedPaths.Where((string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)).Any((string path) => File.ReadAllText(path).Contains("class " + typeName, StringComparison.Ordinal)))
			{
				return false;
			}
		}
		if (spec.IsComponent)
		{
			string text = File.ReadAllText(result.CreatedPath);
			if (text.Contains("resource_name = \"" + spec.DisplayName + "\"", StringComparison.Ordinal) && text.Contains("DefinitionTypeName = \"" + spec.ExpectedTypes[0] + "\"", StringComparison.Ordinal))
			{
				return text.Contains("RuntimeTypeName = \"" + spec.ExpectedTypes[1] + "\"", StringComparison.Ordinal);
			}
			return false;
		}
		return Path.GetFileNameWithoutExtension(result.CreatedPath) == spec.TechnicalName;
	}

	private static bool ValidateChineseOutput(ExampleSpec spec, XWTemplateLibrary.TemplateCreateResult result)
	{
		return (spec.IsComponent ? result.CreatedPaths.Where((string path) => path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase)) : result.CreatedPaths.Where((string path) => path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))).Any((string path) => File.ReadAllText(path).Contains(spec.DisplayName, StringComparison.Ordinal));
	}

	private static (bool Scripts, bool Resources, bool NotBlueprints) ValidateManifest(string projectRoot, IEnumerable<string> generatedPaths, XWModManifest manifest)
	{
		if (manifest == null)
		{
			return (Scripts: false, Resources: false, NotBlueprints: false);
		}
		bool flag = true;
		bool flag2 = true;
		bool flag3 = true;
		foreach (string generatedPath in generatedPaths)
		{
			string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(generatedPath, projectRoot);
			bool num = generatedPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase);
			bool flag4 = manifest.Scripts?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) ?? false;
			bool flag5 = manifest.Resources?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) ?? false;
			bool flag6 = manifest.Blueprints?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) ?? false;
			if (num)
			{
				flag = flag && flag4;
				flag2 = flag2 && !flag5;
				flag3 = flag3 && !flag6;
			}
			else
			{
				flag2 = flag2 && flag5;
				flag = flag && !flag4;
				flag3 = flag3 && !flag6;
			}
		}
		return (Scripts: flag, Resources: flag2, NotBlueprints: flag3);
	}

	private static bool ValidateCompiledBaseTypes(string assemblyPath, IEnumerable<ExampleSpec> specs, out int matchedCount)
	{
		matchedCount = 0;
		Dictionary<string, string> dictionary = specs.SelectMany((ExampleSpec spec) => spec.ExpectedTypes.Zip(spec.ExpectedBaseTypes, (string item, string item2) => (TypeName: item, BaseTypeName: item2))).ToDictionary<(string, string), string, string>(((string TypeName, string BaseTypeName) pair) => pair.TypeName, ((string TypeName, string BaseTypeName) pair) => pair.BaseTypeName, StringComparer.Ordinal);
		foreach (var (typeName, baseTypeName) in dictionary)
		{
			if (HasCompiledBaseType(assemblyPath, typeName, baseTypeName))
			{
				matchedCount++;
			}
		}
		return matchedCount == dictionary.Count;
	}

	private static bool HasCompiledBaseType(string assemblyPath, string typeName, string baseTypeName)
	{
		using FileStream peStream = new FileStream(assemblyPath, FileMode.Open, System.IO.FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
		using PEReader peReader = new PEReader(peStream);
		MetadataReader metadataReader = peReader.GetMetadataReader();
		foreach (TypeDefinitionHandle typeDefinition2 in metadataReader.TypeDefinitions)
		{
			TypeDefinition typeDefinition = metadataReader.GetTypeDefinition(typeDefinition2);
			if (string.Equals(metadataReader.GetString(typeDefinition.Name), typeName, StringComparison.Ordinal))
			{
				return string.Equals(typeDefinition.BaseType.Kind switch
				{
					HandleKind.TypeReference => metadataReader.GetString(metadataReader.GetTypeReference((TypeReferenceHandle)typeDefinition.BaseType).Name), 
					HandleKind.TypeDefinition => metadataReader.GetString(metadataReader.GetTypeDefinition((TypeDefinitionHandle)typeDefinition.BaseType).Name), 
					_ => "", 
				}, baseTypeName, StringComparison.Ordinal);
			}
		}
		return false;
	}

	private async Task<bool> WaitForScript(XWScriptEditor scriptEditor, string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(scriptEditor) && SamePath(scriptEditor.GetCurrentFilePath(), expectedPath))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<bool> WaitForComponentEditor(string path, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor = XWEditorInterface.Instance?.TryGetLoadedResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
			if (GodotObject.IsInstanceValid(xWCharacterComponentVisualResourceEditor) && SamePath(xWCharacterComponentVisualResourceEditor.ActiveResourcePath, path) && string.Equals(XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey, "character_component_editor", StringComparison.Ordinal))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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
