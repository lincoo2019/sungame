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
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModRemainingWorkspaceExamplesProbe : Node
{
	private sealed record ExampleSpec(string TemplateId, string TechnicalName, string DisplayName, string ExpectedClass, string ExpectedCategory, string ExpectedDockKey);

	private static readonly ExampleSpec[] Examples = new ExampleSpec[7]
	{
		new ExampleSpec("state-property-guard-resource", "ExampleStatePropertyGuard", "示例资源状态机：状态为就绪", "StateMachineGuardDefinition", "StateGuard", "state_guard_editor"),
		new ExampleSpec("expression-guard-resource", "ExampleExpressionGuard", "示例场景状态图：生命值归零", "ExpressionGuard", "StateGuard", "state_guard_editor"),
		new ExampleSpec("state-active-guard-resource", "ExampleStateActiveGuard", "示例场景状态图：播放状态激活", "StateIsActiveGuard", "StateGuard", "state_guard_editor"),
		new ExampleSpec("all-of-guard-resource", "ExampleAllOfGuard", "示例场景状态图：全部满足", "AllOfGuard", "StateGuard", "state_guard_editor"),
		new ExampleSpec("any-of-guard-resource", "ExampleAnyOfGuard", "示例场景状态图：任一满足", "AnyOfGuard", "StateGuard", "state_guard_editor"),
		new ExampleSpec("not-guard-resource", "ExampleNotGuard", "示例场景状态图：播放状态未激活", "NotGuard", "StateGuard", "state_guard_editor"),
		new ExampleSpec("animation-atlas-profile-resource", "ExampleAnimationAtlasProfile", "示例启动动画图集配置", "AdobeAnimateAtlasProfile", "AnimationAtlas", "animation_atlas_editor")
	};

	private const string DebugTemplateId = "debug-entry-csharp";

	private const string DebugTechnicalName = "ExampleModDebugEntry";

	private const string DebugDisplayName = "示例 Mod 调试入口";

	private const string AudioRelativePath = "Assets/Audio/Sfx/示例短音效.ogg";

	private const string AudioSourcePath = "res://Asset/Audio/Sfx/Effect/Floop/Floop.ogg";

	private const string BgmRelativePath = "Resources/BGMConfigs/NewBGM.tres";

	private const string BootstrapManifestPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres";

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool projectLoaded = false;
		bool templatesCreated = true;
		bool chinese = true;
		bool linkedGuards = false;
		bool audioCopied = false;
		bool bgmConfigured = false;
		bool audioKeyPortable = false;
		bool typed = true;
		bool routes = true;
		bool opened = true;
		bool directProperties = true;
		bool inspectorUntouched = true;
		bool debugScriptOpened = false;
		bool saveReload = false;
		bool manifest = false;
		bool completion = false;
		int createdCount = 0;
		int routedCount = 0;
		int openedCount = 0;
		int manifestScripts = -1;
		int manifestResources = -1;
		int manifestBlueprints = -1;
		int manifestTranslations = -1;
		try
		{
			string projectRoot = Normalize(System.Environment.GetEnvironmentVariable("PVZHE_TEST_MOD_ROOT"));
			Require(Directory.Exists(projectRoot), "没有通过 PVZHE_TEST_MOD_ROOT 指定可用的测试 Mod 工程。");
			string projectFile = Directory.GetFiles(projectRoot, "*.pvzmodeproject", SearchOption.TopDirectoryOnly).SingleOrDefault();
			Require(!string.IsNullOrWhiteSpace(projectFile), "测试 Mod 工程必须且只能包含一个工程文件。");
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
			Require(f3, "F3 未能初始化真实 Mod 编辑器。");
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			(modEditorPanel?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
			ModProject modProject = ModProject.Load(projectFile);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			projectLoaded = modProject != null && GodotObject.IsInstanceValid(modEditorPanel) && (bool)(method?.Invoke(modEditorPanel, new object[1] { modProject }) ?? ((object)false));
			Require(projectLoaded, "无法从真实 Mod 编辑器入口载入测试 Mod。");
			await WaitFrames(10);
			Node inspectorSentinel = new Node
			{
				Name = "RemainingWorkspaceExamplesInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWTemplateLibrary xWTemplateLibrary = new XWTemplateLibrary();
			Require(xWTemplateLibrary.Templates.Count == 92, $"模板库总数不是 92：{xWTemplateLibrary.Templates.Count}");
			Require(xWTemplateLibrary.Templates.Select((XWTemplateLibrary.TemplateInfo item) => item.Id).Distinct<string>(StringComparer.Ordinal).Count() == 92, "模板库存在重复 ID。");
			List<string> list = new List<string>();
			System.Collections.Generic.Dictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources = new System.Collections.Generic.Dictionary<string, (ExampleSpec, string, Resource)>(StringComparer.Ordinal);
			ExampleSpec[] examples = Examples;
			foreach (ExampleSpec exampleSpec in examples)
			{
				XWTemplateLibrary.TemplateInfo templateInfo = xWTemplateLibrary.FindTemplate(exampleSpec.TemplateId);
				Require(templateInfo != null, "缺少模板：" + exampleSpec.TemplateId);
				if (templateInfo == null)
				{
					templatesCreated = false;
					continue;
				}
				chinese &= XWTemplatePresentation.HasChineseText(templateInfo.DisplayName) && XWTemplatePresentation.HasChineseText(templateInfo.PreviewText) && XWTemplatePresentation.HasChineseText(exampleSpec.DisplayName);
				XWTemplateLibrary.TemplateCreateResult templateCreateResult = xWTemplateLibrary.CreateFromTemplate(exampleSpec.TemplateId, projectRoot, exampleSpec.TechnicalName, exampleSpec.DisplayName, overwrite: true);
				string text = Normalize(Path.Combine(projectRoot, templateInfo.DefaultFolder, exampleSpec.TechnicalName + templateInfo.FileExtension));
				bool flag = templateCreateResult.Success && SamePath(templateCreateResult.CreatedPath, text) && File.Exists(text);
				templatesCreated = templatesCreated && flag;
				if (!flag)
				{
					Require(condition: false, "创建模板示例失败：" + exampleSpec.TemplateId + " -> " + templateCreateResult.Error);
					continue;
				}
				createdCount++;
				list.Add(text);
				Resource resource = LoadResource(text, ResourceLoader.CacheMode.Replace);
				bool flag2 = GodotObject.IsInstanceValid(resource) && (resource.GetType().Name == exampleSpec.ExpectedClass || resource.IsClass(exampleSpec.ExpectedClass));
				typed = typed && flag2;
				Require(flag2, "模板示例类型错误：" + exampleSpec.TemplateId + " -> " + (resource?.GetType().Name ?? "null"));
				if (flag2)
				{
					resources[exampleSpec.TemplateId] = (exampleSpec, text, resource);
				}
			}
			XWTemplateLibrary.TemplateInfo templateInfo2 = xWTemplateLibrary.FindTemplate("debug-entry-csharp");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = xWTemplateLibrary.CreateFromTemplate("debug-entry-csharp", projectRoot, "ExampleModDebugEntry", "示例 Mod 调试入口", overwrite: true);
			string debugPath = Normalize(Path.Combine(projectRoot, templateInfo2?.DefaultFolder ?? "Scripts", "ExampleModDebugEntry.cs"));
			bool flag3 = templateInfo2 != null && templateCreateResult2.Success && SamePath(templateCreateResult2.CreatedPath, debugPath) && File.Exists(debugPath) && File.ReadAllText(debugPath).Contains("ModEditorDebugEntry", StringComparison.Ordinal) && File.ReadAllText(debugPath).Contains("示例生命值", StringComparison.Ordinal);
			templatesCreated = templatesCreated && flag3;
			chinese &= templateInfo2 != null && XWTemplatePresentation.HasChineseText(templateInfo2.DisplayName) && XWTemplatePresentation.HasChineseText(templateInfo2.PreviewText);
			if (flag3)
			{
				createdCount++;
				list.Add(debugPath);
			}
			Require(flag3, "创建 C# 调试入口示例失败：" + templateCreateResult2.Error);
			linkedGuards = ConfigureGuardGraph(resources, projectRoot);
			Require(linkedGuards, "状态条件示例没有形成类型安全、无环、可移植的组合关系。");
			AdobeAnimateAtlasProfile adobeAnimateAtlasProfile = Get<AdobeAnimateAtlasProfile>(resources, "animation-atlas-profile-resource");
			if (GodotObject.IsInstanceValid(adobeAnimateAtlasProfile))
			{
				adobeAnimateAtlasProfile.ProfileId = "示例启动图集";
				adobeAnimateAtlasProfile.ManifestPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres";
				adobeAnimateAtlasProfile.StartupOnly = true;
				adobeAnimateAtlasProfile.ResourceName = "示例启动动画图集配置";
				saveReload = SavePortable(adobeAnimateAtlasProfile, projectRoot) && GodotObject.IsInstanceValid(adobeAnimateAtlasProfile.LoadManifest());
			}
			Require(saveReload, "动画图集配置没有保存、重载并解析内置启动图集清单。");
			string audioPath = Normalize(Path.Combine(projectRoot, "Assets/Audio/Sfx/示例短音效.ogg"));
			Directory.CreateDirectory(Path.GetDirectoryName(audioPath));
			string text2 = ProjectSettings.GlobalizePath("res://Asset/Audio/Sfx/Effect/Floop/Floop.ogg");
			File.Copy(text2, audioPath, overwrite: true);
			audioCopied = File.Exists(audioPath) && new FileInfo(audioPath).Length == new FileInfo(text2).Length && GodotObject.IsInstanceValid(AudioStreamOggVorbis.LoadFromFile(audioPath));
			if (audioCopied)
			{
				list.Add(audioPath);
			}
			Require(audioCopied, "没有从原游戏复制并加载示例短音效。");
			XWGameplayResourceChoice choice = new XWGameplayResourceChoice(audioPath, "示例短音效", audioPath, null, XWGameplayResourceKind.Audio, IsModResource: true);
			XWGameplayResourceChoice choice2 = new XWGameplayResourceChoice("Win", "游戏内胜利音乐", "res://Asset/Audio/BGM/Win.ogg", null, XWGameplayResourceKind.Audio, IsModResource: false);
			audioKeyPortable = XWGameplayResourcePickerWindow.GetPersistedAudioKey(choice) == "示例短音效" && XWGameplayResourcePickerWindow.GetPersistedAudioKey(choice2) == "Win";
			Require(audioKeyPortable, "Mod 音频选择没有保存成运行时可注册的稳定 key。");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult3 = xWTemplateLibrary.CreateFromTemplate("bgm-resource", projectRoot, "NewBGM", "示例背景音乐配置", overwrite: true);
			string bgmPath = Normalize(Path.Combine(projectRoot, "Resources/BGMConfigs/NewBGM.tres"));
			TowerDefenseBackgroundMusicConfig bgm = LoadResource(bgmPath, ResourceLoader.CacheMode.Replace) as TowerDefenseBackgroundMusicConfig;
			if (templateCreateResult3.Success && GodotObject.IsInstanceValid(bgm))
			{
				bgm.ResourceName = "示例背景音乐配置";
				bgm.translate = "示例背景音乐";
				bgm.entry = "ChooseYourSeeds";
				bgm.flag1 = "Grasswalk";
				bgm.drums = "GrasswalkDrums";
				bgm.win = "Win";
				bgm.drumsZombieThreshold = 10;
				bgm.drumsFadeSpeed = 1f;
				bgm.drumsCheckInterval = 0.25f;
				bgmConfigured = SavePortable(bgm, projectRoot);
			}
			Require(bgmConfigured, "BGM 示例没有使用当前脚本路径和可预听的四段内置音轨。");
			XWModManifestSyncService.RegisterPaths(projectRoot, list);
			XWModManifestSyncService.SyncProject(projectRoot);
			XWModManifest modManifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			manifestScripts = modManifest?.Scripts?.Count ?? (-1);
			manifestResources = modManifest?.Resources?.Count ?? (-1);
			manifestBlueprints = modManifest?.Blueprints?.Count ?? (-1);
			manifestTranslations = modManifest?.Translations?.Count ?? (-1);
			manifest = manifestScripts == 19 && manifestResources == 132 && manifestBlueprints == 1 && manifestTranslations == 1 && ManifestPathsAreHealthy(projectRoot, modManifest) && list.All((string path2) => ManifestContains(projectRoot, modManifest, path2));
			Require(manifest, "最终清单不是 scripts=19/resources=132/blueprints=1/translations=1，或路径不完整。");
			XWFileSystemPanel fileSystemPanel = XWFileSystemPanel.Instance;
			XWFileSystemList fileList = fileSystemPanel?.GetNodeOrNull<XWFileSystemList>("%FileList");
			Require(GodotObject.IsInstanceValid(fileSystemPanel) && GodotObject.IsInstanceValid(fileList), "真实文件树不可用。");
			XWFileSystem.GetSingleton().SetProjectFolderPath(projectRoot);
			XWFileSystem.GetSingleton().ScanChanges();
			fileSystemPanel.NavigateToProject(projectRoot);
			await WaitFrames(12);
			foreach (var (spec, path, resource2) in resources.Values)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				XWVisualEditorDescriptor descriptor;
				bool flag4 = XWResourceEditorRegistry.TryGetEditor(resource2, path, out descriptor) && descriptor.Category == spec.ExpectedCategory && descriptor.DockKey == spec.ExpectedDockKey;
				routes = routes && flag4;
				if (flag4)
				{
					routedCount++;
				}
				Require(flag4, "资源路由错误：" + spec.TemplateId);
				if (flag4)
				{
					bool flag5 = await OpenFromFileTree(fileSystemPanel, fileList, path, spec.ExpectedDockKey);
					opened = opened && flag5;
					if (flag5)
					{
						openedCount++;
					}
					Require(flag5, "无法从真实文件树打开：" + spec.TemplateId);
					XWGenericVisualResourceEditor xWGenericVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor(spec.ExpectedDockKey) as XWGenericVisualResourceEditor;
					bool flag6 = HasCompleteDirectAuthoringSurface(xWGenericVisualResourceEditor, spec.TemplateId, resource2);
					directProperties = directProperties && flag6;
					Require(flag6, "主面板没有完整直接属性：" + spec.TemplateId);
					bool flag7 = IsInspectorSentinel(inspectorSentinel) && xWGenericVisualResourceEditor?.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
					inspectorUntouched = inspectorUntouched && flag7;
					Require(flag7, "资源占用了全局或嵌入式 Inspector：" + spec.TemplateId);
				}
			}
			XWVisualEditorDescriptor descriptor2;
			bool flag8 = XWResourceEditorRegistry.TryGetEditor(AudioStreamOggVorbis.LoadFromFile(audioPath), audioPath, out descriptor2) && descriptor2.Category == "Audio" && descriptor2.DockKey == "audio_editor";
			routes = routes && flag8;
			if (flag8)
			{
				routedCount++;
			}
			Require(flag8, "示例音效没有进入 Audio 工作台。");
			if (flag8)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				bool flag9 = await OpenFromFileTree(fileSystemPanel, fileList, audioPath, "audio_editor");
				XWGenericVisualResourceEditor xWGenericVisualResourceEditor2 = XWEditorInterface.Instance.GetResourceEditor("audio_editor") as XWGenericVisualResourceEditor;
				GD.Print($"[MOD_EDITOR_TEST_MOD_REMAINING_AUDIO_DIAGNOSTIC] selected={fileList.GetSelectedItems().Length} activeDock={XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey ?? "null"} activePath={xWGenericVisualResourceEditor2?.ActiveResourcePath ?? "null"} tree={DescribeFileSystemDirectory(audioPath)} expected={audioPath}");
				opened = opened && flag9;
				if (flag9)
				{
					openedCount++;
				}
				Require(flag9, "无法从真实文件树打开示例音效。");
				XWGenericVisualResourceEditor xWGenericVisualResourceEditor3 = XWEditorInterface.Instance.GetResourceEditor("audio_editor") as XWGenericVisualResourceEditor;
				bool flag10 = GodotObject.IsInstanceValid(xWGenericVisualResourceEditor3) && xWGenericVisualResourceEditor3.DirectMissingPropertyCount == 0 && xWGenericVisualResourceEditor3.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
				directProperties = directProperties && flag10;
				inspectorUntouched &= IsInspectorSentinel(inspectorSentinel);
				Require(flag10, "Audio 主面板仍依赖 Inspector 或存在缺失属性。");
			}
			XWVisualEditorDescriptor descriptor3;
			bool flag11 = XWResourceEditorRegistry.TryGetEditor(bgm, bgmPath, out descriptor3) && descriptor3.Category == "BGM" && descriptor3.DockKey == "bgm_editor";
			routes = routes && flag11;
			if (flag11)
			{
				routedCount++;
			}
			Require(flag11, "示例 BGM 没有进入 BGM 工作台。");
			if (flag11)
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				bool flag12 = await OpenFromFileTree(fileSystemPanel, fileList, bgmPath, "bgm_editor");
				opened = opened && flag12;
				if (flag12)
				{
					openedCount++;
				}
				Require(flag12, "无法从真实文件树打开示例 BGM。");
				XWGenericVisualResourceEditor xWGenericVisualResourceEditor4 = XWEditorInterface.Instance.GetResourceEditor("bgm_editor") as XWGenericVisualResourceEditor;
				bool flag13 = HasCompleteDirectAuthoringSurface(xWGenericVisualResourceEditor4, "bgm-resource", bgm);
				directProperties = directProperties && flag13;
				inspectorUntouched &= IsInspectorSentinel(inspectorSentinel) && xWGenericVisualResourceEditor4?.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
				Require(flag13, "BGM 主面板没有完整直接属性或仍依赖 Inspector。");
			}
			fileSystemPanel.NavigateToPath(debugPath);
			await WaitFrames(5);
			int[] selectedDebug = fileList.GetSelectedItems();
			bool debugEntryVisible = selectedDebug.Length == 1 && selectedDebug[0] >= 0 && selectedDebug[0] < fileList.ItemCount;
			if (debugEntryVisible)
			{
				fileList.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedDebug[0]);
			}
			await WaitFrames(8);
			XWScriptEditor scriptEditor = XWEditorInterface.Instance.GetScriptEditor();
			debugScriptOpened = debugEntryVisible && GodotObject.IsInstanceValid(scriptEditor) && SamePath(scriptEditor.GetCurrentFilePath(), debugPath) && XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey == "script_editor";
			GD.Print($"[MOD_EDITOR_TEST_MOD_REMAINING_SCRIPT_DIAGNOSTIC] visible={debugEntryVisible} selected={selectedDebug.Length} current={scriptEditor?.GetCurrentFilePath() ?? "null"} activeDock={XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey ?? "null"} tree={DescribeFileSystemDirectory(debugPath)} expected={debugPath}");
			Require(debugScriptOpened, "C# 调试入口没有从真实文件树进入 C# 脚本编辑器。");
			bool flag14 = ValidateReloadedGuardGraph(resources);
			AdobeAnimateAtlasProfile adobeAnimateAtlasProfile2 = LoadResource(resources["animation-atlas-profile-resource"].Path, ResourceLoader.CacheMode.Ignore) as AdobeAnimateAtlasProfile;
			TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig = LoadResource(bgmPath, ResourceLoader.CacheMode.Ignore) as TowerDefenseBackgroundMusicConfig;
			saveReload &= flag14 && GodotObject.IsInstanceValid(adobeAnimateAtlasProfile2) && adobeAnimateAtlasProfile2.ProfileId == "示例启动图集" && adobeAnimateAtlasProfile2.StartupOnly && GodotObject.IsInstanceValid(adobeAnimateAtlasProfile2.LoadManifest()) && GodotObject.IsInstanceValid(towerDefenseBackgroundMusicConfig) && towerDefenseBackgroundMusicConfig.ResourceName == "示例背景音乐配置" && towerDefenseBackgroundMusicConfig.entry == "ChooseYourSeeds" && towerDefenseBackgroundMusicConfig.win == "Win";
			Require(saveReload, "最终示例没有通过忽略缓存的磁盘重载。");
			int num2 = openedCount + (debugScriptOpened ? 1 : 0);
			completion = templatesCreated && chinese && linkedGuards && audioCopied && audioKeyPortable && bgmConfigured && typed && routes && opened && directProperties && inspectorUntouched && debugScriptOpened && saveReload && manifest && createdCount == 8 && routedCount == 9 && openedCount == 9 && num2 == 10;
			Require(completion, "最终批次不是 templates=92/92、workspaces=10/10。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_REMAINING_WORKSPACE_EXAMPLES_PROBE] f3={f3} projectLoaded={projectLoaded} templatesCreated={templatesCreated} chinese={chinese} linkedGuards={linkedGuards} audioCopied={audioCopied} audioKeyPortable={audioKeyPortable} bgmConfigured={bgmConfigured} typed={typed} routes={routes} opened={opened} directProperties={directProperties} inspectorUntouched={inspectorUntouched} debugScriptOpened={debugScriptOpened} saveReload={saveReload} manifest={manifest} completion={completion} createdCount={createdCount}/8 routedCount={routedCount}/9 openedCount={openedCount}/9 verifiedWorkspaces={openedCount + (debugScriptOpened ? 1 : 0)}/10 manifestScripts={manifestScripts} manifestResources={manifestResources} manifestBlueprints={manifestBlueprints} manifestTranslations={manifestTranslations} templates=92/92 workspaces=10/10 failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_REMAINING_WORKSPACE_EXAMPLES_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static bool ConfigureGuardGraph(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources, string projectRoot)
	{
		StateMachineGuardDefinition stateMachineGuardDefinition = Get<StateMachineGuardDefinition>(resources, "state-property-guard-resource");
		ExpressionGuard expressionGuard = Get<ExpressionGuard>(resources, "expression-guard-resource");
		StateIsActiveGuard stateIsActiveGuard = Get<StateIsActiveGuard>(resources, "state-active-guard-resource");
		AllOfGuard allOfGuard = Get<AllOfGuard>(resources, "all-of-guard-resource");
		AnyOfGuard anyOfGuard = Get<AnyOfGuard>(resources, "any-of-guard-resource");
		NotGuard notGuard = Get<NotGuard>(resources, "not-guard-resource");
		if (!AllValid(stateMachineGuardDefinition, expressionGuard, stateIsActiveGuard, allOfGuard, anyOfGuard, notGuard))
		{
			return false;
		}
		stateMachineGuardDefinition.ResourceName = "示例资源状态机：状态为就绪";
		stateMachineGuardDefinition.Kind = StateMachineGuardKind.ExpressionProperty;
		stateMachineGuardDefinition.ComparedProperty = "state";
		stateMachineGuardDefinition.Operator = StateMachineComparisonOperator.Equal;
		stateMachineGuardDefinition.ExpectedValue = Variant.From<string>("ready");
		stateMachineGuardDefinition.Negate = false;
		expressionGuard.ResourceName = "示例场景状态图：生命值归零";
		expressionGuard.expression = "health <= 0";
		stateIsActiveGuard.ResourceName = "示例场景状态图：播放状态激活";
		stateIsActiveGuard.state = new NodePath("../Playing");
		notGuard.ResourceName = "示例场景状态图：播放状态未激活";
		notGuard.guard = stateIsActiveGuard;
		allOfGuard.ResourceName = "示例场景状态图：生命值归零且播放状态激活";
		allOfGuard.guards = new Array<Guard> { expressionGuard, stateIsActiveGuard };
		anyOfGuard.ResourceName = "示例场景状态图：生命值归零或播放状态未激活";
		anyOfGuard.guards = new Array<Guard> { expressionGuard, notGuard };
		Resource[] obj = new Resource[6] { stateMachineGuardDefinition, expressionGuard, stateIsActiveGuard, notGuard, allOfGuard, anyOfGuard };
		bool flag = true;
		Resource[] array = obj;
		foreach (Resource resource in array)
		{
			flag &= SavePortable(resource, projectRoot);
		}
		if (flag && allOfGuard.guards.Count == 2 && anyOfGuard.guards.Count == 2 && notGuard.guard == stateIsActiveGuard)
		{
			return allOfGuard.guards.All((Guard guard) => (guard is ExpressionGuard || guard is StateIsActiveGuard) ? true : false);
		}
		return false;
	}

	private static bool HasCompleteDirectAuthoringSurface(XWGenericVisualResourceEditor editor, string templateId, Resource resource)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return false;
		}
		if (editor is XWStateGuardVisualResourceEditor xWStateGuardVisualResourceEditor)
		{
			if (xWStateGuardVisualResourceEditor.EditingGuard != resource || !GodotObject.IsInstanceValid(xWStateGuardVisualResourceEditor.GuardPreviewCanvas) || !GodotObject.IsInstanceValid(xWStateGuardVisualResourceEditor.SaveGuardButton) || !GodotObject.IsInstanceValid(editor.FindChild("ResourceNameEdit", recursive: true, owned: false)) || !GodotObject.IsInstanceValid(editor.FindChild("LocalToSceneCheck", recursive: true, owned: false)))
			{
				return false;
			}
			return templateId switch
			{
				"state-property-guard-resource" => GodotObject.IsInstanceValid(editor.FindChild("ComparedPropertyEdit", recursive: true, owned: false)) && GodotObject.IsInstanceValid(editor.FindChild("OperatorSegments", recursive: true, owned: false)) && GodotObject.IsInstanceValid(editor.FindChild("ExpectedText", recursive: true, owned: false)), 
				"expression-guard-resource" => GodotObject.IsInstanceValid(editor.FindChild("ExpressionEdit", recursive: true, owned: false)), 
				"state-active-guard-resource" => GodotObject.IsInstanceValid(editor.FindChild("StatePathEdit", recursive: true, owned: false)), 
				"all-of-guard-resource" => xWStateGuardVisualResourceEditor.CompositionItemCount == 2, 
				"any-of-guard-resource" => xWStateGuardVisualResourceEditor.CompositionItemCount == 2, 
				"not-guard-resource" => xWStateGuardVisualResourceEditor.CompositionItemCount == 1, 
				_ => false, 
			};
		}
		if (editor.DirectEditablePropertyCount > 0 && editor.DirectMountedPropertyCount == editor.DirectEditablePropertyCount)
		{
			return editor.DirectMissingPropertyCount == 0;
		}
		return false;
	}

	private static bool ValidateReloadedGuardGraph(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources)
	{
		ExpressionGuard expressionGuard = Reload<ExpressionGuard>(resources, "expression-guard-resource");
		StateIsActiveGuard stateIsActiveGuard = Reload<StateIsActiveGuard>(resources, "state-active-guard-resource");
		AllOfGuard allOfGuard = Reload<AllOfGuard>(resources, "all-of-guard-resource");
		AnyOfGuard anyOfGuard = Reload<AnyOfGuard>(resources, "any-of-guard-resource");
		NotGuard notGuard = Reload<NotGuard>(resources, "not-guard-resource");
		StateMachineGuardDefinition stateMachineGuardDefinition = Reload<StateMachineGuardDefinition>(resources, "state-property-guard-resource");
		if (AllValid(stateMachineGuardDefinition, expressionGuard, stateIsActiveGuard, allOfGuard, anyOfGuard, notGuard) && stateMachineGuardDefinition.ExpectedValue.AsString() == "ready" && expressionGuard.expression == "health <= 0" && stateIsActiveGuard.state.ToString() == "../Playing" && allOfGuard.guards.Count == 2 && anyOfGuard.guards.Count == 2)
		{
			return GodotObject.IsInstanceValid(notGuard.guard);
		}
		return false;
	}

	private async Task<bool> OpenFromFileTree(XWFileSystemPanel panel, XWFileSystemList list, string path, string dockKey)
	{
		panel.NavigateToPath(path);
		await WaitFrames(4);
		int[] selectedItems = list.GetSelectedItems();
		if (selectedItems.Length != 1 || selectedItems[0] < 0 || selectedItems[0] >= list.ItemCount)
		{
			return false;
		}
		list.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedItems[0]);
		for (int frame = 0; frame < 480; frame++)
		{
			XWGenericVisualResourceEditor xWGenericVisualResourceEditor = XWEditorInterface.Instance?.GetResourceEditor(dockKey) as XWGenericVisualResourceEditor;
			if (GodotObject.IsInstanceValid(xWGenericVisualResourceEditor) && SamePath(xWGenericVisualResourceEditor.ActiveResourcePath, path) && XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey == dockKey)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private static bool SavePortable(Resource resource, string projectRoot)
	{
		if (!GodotObject.IsInstanceValid(resource) || string.IsNullOrWhiteSpace(resource.ResourcePath) || ResourceSaver.Save(resource, resource.ResourcePath, ResourceSaver.SaverFlags.None) != Error.Ok)
		{
			return false;
		}
		string text = Normalize(resource.ResourcePath);
		XWModProjectLayout.MakeSavedTextResourceReferencesPortable(text, projectRoot);
		if (!File.Exists(text))
		{
			return false;
		}
		string text2 = File.ReadAllText(text);
		string text3 = Normalize(projectRoot);
		if (!text2.Contains(text3, StringComparison.OrdinalIgnoreCase))
		{
			return !text2.Contains(text3.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static T Get<T>(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources, string templateId) where T : Resource
	{
		if (!resources.TryGetValue(templateId, out (ExampleSpec, string, Resource) value))
		{
			return null;
		}
		return value.Item3 as T;
	}

	private static T Reload<T>(IReadOnlyDictionary<string, (ExampleSpec Spec, string Path, Resource Resource)> resources, string templateId) where T : Resource
	{
		if (!resources.TryGetValue(templateId, out (ExampleSpec, string, Resource) value))
		{
			return null;
		}
		return LoadResource(value.Item2, ResourceLoader.CacheMode.Ignore) as T;
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

	private static bool ManifestContains(string projectRoot, XWModManifest manifest, string path)
	{
		string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(path, projectRoot);
		return ((IEnumerable<string>)(XWModManifestSyncService.GetManifestSection(relative) switch
		{
			"Scripts" => manifest?.Scripts, 
			"Blueprints" => manifest?.Blueprints, 
			"Translations" => manifest?.Translations, 
			_ => manifest?.Resources, 
		}))?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) ?? false;
	}

	private static Resource LoadResource(string path, ResourceLoader.CacheMode cacheMode)
	{
		return ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(Normalize(path)).Replace('\\', '/'), "", cacheMode);
	}

	private static string DescribeFileSystemDirectory(string filePath)
	{
		string text = Normalize(Path.GetDirectoryName(filePath) ?? "").TrimEnd('/') + "/";
		XWFileSystemDirectory filesystemPath = XWFileSystem.GetSingleton().GetFilesystemPath(text);
		if (filesystemPath != null)
		{
			return string.Join(",", filesystemPath.Files.Select((XWFileInfo file) => file.Name));
		}
		return "missing:" + text;
	}

	private static bool AllValid(params GodotObject[] objects)
	{
		return objects.All(GodotObject.IsInstanceValid);
	}

	private static bool IsInspectorSentinel(Node sentinel)
	{
		if (XWEditorInterface.Instance?.GetInspector() is XWInspector xWInspector)
		{
			return xWInspector.CurrentObject == sentinel;
		}
		return true;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel)
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
