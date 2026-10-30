using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
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
using PVZHE.ModEditor.SceneEditor;
using PVZHE.ModEditor.ScriptEditor;
using PVZHE.ModEditor.Tools;

public partial class ModEditorTestModCharacterSceneExamplesProbe : Node
{
	private sealed class ExampleSpec
	{
		public string TemplateId { get; init; } = "";

		public string TechnicalName { get; init; } = "";

		public string DisplayName { get; init; } = "";

		public string Folder { get; init; } = "";

		public string ExpectedBaseType { get; init; } = "";

		public string ExpectedConfigType { get; init; } = "";

		public string Category { get; init; } = "";
	}

	private static readonly ExampleSpec[] Examples = new ExampleSpec[7]
	{
		new ExampleSpec
		{
			TemplateId = "character-scene-zombie",
			TechnicalName = "ExampleZombie",
			DisplayName = "示例僵尸角色",
			Folder = "Resources/Characters/Zombies",
			ExpectedBaseType = "TowerDefenseZombie",
			ExpectedConfigType = "TowerDefenseZombieConfig",
			Category = "Zombie"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-prop",
			TechnicalName = "ExampleProp",
			DisplayName = "示例场景道具",
			Folder = "Resources/Characters/Props",
			ExpectedBaseType = "TowerDefenseItem",
			ExpectedConfigType = "TowerDefenseItemConfig",
			Category = "Prop"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-vase",
			TechnicalName = "ExampleVase",
			DisplayName = "示例花瓶角色",
			Folder = "Resources/Characters/Vases",
			ExpectedBaseType = "TowerDefenseVase",
			ExpectedConfigType = "TowerDefenseVaseConfig",
			Category = "Vase"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-mower",
			TechnicalName = "ExampleMower",
			DisplayName = "示例小推车角色",
			Folder = "Resources/Characters/Mowers",
			ExpectedBaseType = "TowerDefenseMower",
			ExpectedConfigType = "TowerDefenseMowerConfig",
			Category = "Mower"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-item",
			TechnicalName = "ExampleItem",
			DisplayName = "示例物品角色",
			Folder = "Resources/Characters/Items",
			ExpectedBaseType = "TowerDefenseItem",
			ExpectedConfigType = "TowerDefenseItemConfig",
			Category = "Item"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-grave",
			TechnicalName = "ExampleGrave",
			DisplayName = "示例墓碑角色",
			Folder = "Resources/Characters/Graves",
			ExpectedBaseType = "TowerDefenseGravestone",
			ExpectedConfigType = "TowerDefenseGravestoneConfig",
			Category = "Grave"
		},
		new ExampleSpec
		{
			TemplateId = "character-scene-crater",
			TechnicalName = "ExampleCrater",
			DisplayName = "示例弹坑角色",
			Folder = "Resources/Characters/Craters",
			ExpectedBaseType = "TowerDefenseCrater",
			ExpectedConfigType = "TowerDefenseCraterConfig",
			Category = "Crater"
		}
	};

	private static readonly HashSet<string> PostHistoricalTemplateIds = new HashSet<string>(StringComparer.Ordinal) { "state-property-guard-resource", "expression-guard-resource", "state-active-guard-resource", "all-of-guard-resource", "any-of-guard-resource", "not-guard-resource", "debug-entry-csharp", "animation-atlas-profile-resource" };

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool f3 = false;
		bool projectLoaded = false;
		bool created = true;
		bool templateRecreated = false;
		bool chinese = true;
		bool packagesComplete = true;
		bool manifestCounts = false;
		bool backgroundCompile = false;
		bool compiledBaseTypes = false;
		bool scenesOpened = true;
		bool configsOpened = true;
		bool gameVisuals = true;
		bool directProperties = true;
		bool inspectorUntouched = true;
		bool saveReload = true;
		bool diskReload = false;
		bool directEditUndoRedo = false;
		bool cacheIgnoreReload = false;
		bool sceneSaveReopen = false;
		bool pmodExported = false;
		bool modLoaded = false;
		bool modApplied = false;
		bool modInstantiated = false;
		bool modUnloaded = false;
		bool unloadResponsive = false;
		bool rollbackOnBindingFailure = false;
		bool compileWithinDeadline = false;
		bool compileTaskDrained = false;
		bool companionRuntimeExecuted = false;
		bool battleFactory = false;
		bool completionMatrix = false;
		int createdCount = 0;
		int sceneOpenedCount = 0;
		int configOpenedCount = 0;
		int compiledTypeCount = 0;
		int manifestScripts = -1;
		int manifestResources = -1;
		int manifestBlueprints = -1;
		int manifestTranslations = -1;
		int strongCount = 77;
		int missingCount = 15;
		try
		{
			string projectRoot = Normalize(System.Environment.GetEnvironmentVariable("PVZHE_TEST_MOD_ROOT"));
			Require(!string.IsNullOrWhiteSpace(projectRoot) && Directory.Exists(projectRoot), "没有通过 PVZHE_TEST_MOD_ROOT 指定可用的外部测试 Mod 工程。");
			string projectFile = Directory.GetFiles(projectRoot, "*.pvzmodeproject", SearchOption.TopDirectoryOnly).SingleOrDefault();
			Require(!string.IsNullOrWhiteSpace(projectFile), "外部测试 Mod 工程文件不存在或数量不唯一。");
			ExampleSpec recreatedSpec = Examples[0];
			string text = Normalize(Path.Combine(projectRoot, recreatedSpec.Folder, recreatedSpec.TechnicalName));
			Require(IsPathInsideRoot(text, projectRoot), "代表性角色包越出了临时测试 Mod 根目录。");
			if (IsPathInsideRoot(text, projectRoot) && Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			Require(!Directory.Exists(text), "代表性角色包没有从临时测试 Mod 副本中删除。");
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
			ModProject project = ModProject.Load(projectFile);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			projectLoaded = project != null && GodotObject.IsInstanceValid(modEditorPanel) && (bool)(method?.Invoke(modEditorPanel, new object[1] { project }) ?? ((object)false));
			Require(projectLoaded, "无法通过真实 Mod 编辑器入口载入外部测试 Mod。");
			await WaitFrames(8);
			Dictionary<string, string[]> packagePaths = Examples.ToDictionary<ExampleSpec, string, string[]>((ExampleSpec exampleSpec2) => exampleSpec2.TemplateId, (ExampleSpec spec2) => GetCharacterPackagePaths(projectRoot, spec2), StringComparer.Ordinal);
			diskReload = Examples.All((ExampleSpec exampleSpec2) => PackageFilesExist(packagePaths[exampleSpec2.TemplateId]));
			Node inspectorSentinel = new Node
			{
				Name = "TestModCharacterSceneExamplesInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWTemplateLibrary library = new XWTemplateLibrary();
			List<string> list = new List<string>();
			Dictionary<string, Resource> loadedConfigs = new Dictionary<string, Resource>(StringComparer.Ordinal);
			ExampleSpec[] examples = Examples;
			foreach (ExampleSpec exampleSpec in examples)
			{
				XWTemplateLibrary.TemplateInfo templateInfo = library.FindTemplate(exampleSpec.TemplateId);
				bool flag = templateInfo != null && XWTemplatePresentation.HasChineseText(exampleSpec.DisplayName) && XWTemplatePresentation.HasChineseText(XWTemplatePresentation.Resolve(templateInfo).DisplayNameLabel);
				chinese = chinese && flag;
				Require(flag, "角色场景模板没有中文展示：" + exampleSpec.TemplateId);
				string[] array = packagePaths[exampleSpec.TemplateId];
				bool num2 = PackageFilesExist(array);
				XWTemplateLibrary.TemplateCreateResult result = null;
				if (!num2)
				{
					result = library.CreateFromTemplate(exampleSpec.TemplateId, projectRoot, exampleSpec.TechnicalName, exampleSpec.DisplayName);
					if (string.Equals(exampleSpec.TemplateId, recreatedSpec.TemplateId, StringComparison.Ordinal))
					{
						templateRecreated = (result?.Success ?? false) && result.CreatedPaths.Count == 8 && array.All((string path) => result.CreatedPaths.Any((string createdPath) => SamePath(createdPath, path)));
					}
				}
				bool flag2 = num2 || ((result?.Success ?? false) && result.CreatedPaths.Count == 8 && SamePath(result.CreatedPath, array[0]) && array.All((string path) => result.CreatedPaths.Any((string createdPath) => SamePath(createdPath, path))));
				flag2 &= PackageFilesExist(array);
				created = created && flag2;
				if (flag2)
				{
					createdCount++;
				}
				Require(flag2, "创建或读取角色包失败：" + exampleSpec.TemplateId + " -> " + (result?.Error ?? "文件不完整"));
				list.AddRange(array);
				Resource resource = LoadResource(array[1], ResourceLoader.CacheMode.Reuse);
				bool flag3 = GodotObject.IsInstanceValid(resource) && (resource.GetType().Name == exampleSpec.ExpectedConfigType || resource.IsClass(exampleSpec.ExpectedConfigType)) && resource.ResourceName == exampleSpec.DisplayName;
				Error error = (flag3 ? ResourceSaver.Save(resource, array[1], ResourceSaver.SaverFlags.None) : Error.Failed);
				bool flag4 = error == Error.Ok && XWModProjectLayout.MakeSavedTextResourceReferencesPortable(array[1], projectRoot);
				bool flag5 = flag2 && flag3 && error == Error.Ok && flag4 && ValidatePackageFiles(projectRoot, exampleSpec, array);
				packagesComplete = packagesComplete && flag5;
				chinese = chinese && flag3;
				saveReload = saveReload && flag5;
				Require(flag5, "角色包真实保存后的拓扑、稳定技术名或可移植磁盘引用不完整：" + exampleSpec.TemplateId);
				Require(flag3, "角色配置类型或中文资源名错误：" + exampleSpec.TemplateId);
				if (flag3)
				{
					loadedConfigs[exampleSpec.TemplateId] = resource;
				}
			}
			Require(templateRecreated, "代表性角色包没有在本次唯一临时副本中通过模板重新生成。");
			diskReload = Examples.All((ExampleSpec exampleSpec2) => PackageFilesExist(packagePaths[exampleSpec2.TemplateId]));
			bool condition = InjectRuntimeReadyCounter(packagePaths[recreatedSpec.TemplateId][4]);
			Require(condition, "无法向临时副本的代表性角色脚本注入运行时执行计数器。");
			bool flag6 = XWModManifestSyncService.RegisterPaths(projectRoot, list);
			bool flag7 = XWModManifestSyncService.SyncProject(projectRoot);
			XWModManifest manifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			manifestScripts = manifest?.Scripts?.Count ?? (-1);
			manifestResources = manifest?.Resources?.Count ?? (-1);
			manifestBlueprints = manifest?.Blueprints?.Count ?? (-1);
			manifestTranslations = manifest?.Translations?.Count ?? (-1);
			manifestCounts = (flag6 || flag7 || File.Exists(Path.Combine(projectRoot, "mod.json"))) && manifestScripts == 19 && manifestResources == 132 && manifestBlueprints == 1 && manifestTranslations == 1 && ManifestPathsAreHealthy(projectRoot, manifest) && list.All((string path) => ManifestContains(projectRoot, manifest, path));
			Require(manifestCounts, "完整测试 Mod 清单不是 scripts=19/resources=132/blueprints=1/translations=1，或角色包路径缺失。");
			Task<XWScriptCompiler.CompileResult> compileTask = XWScriptCompiler.CompileModProjectAsync(projectRoot);
			Stopwatch compileWatch = Stopwatch.StartNew();
			int responsiveFrames = 0;
			while (!compileTask.IsCompleted && compileWatch.Elapsed.TotalSeconds <= 120.0)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				responsiveFrames++;
			}
			compileWithinDeadline = compileTask.IsCompleted;
			XWScriptCompiler.CompileResult compileResult = ((!compileWithinDeadline) ? null : (await compileTask));
			XWScriptCompiler.CompileResult compile = compileResult;
			compileWatch.Stop();
			compileTaskDrained = compileWithinDeadline && !XWScriptCompiler.IsModBuildBusy(projectRoot) && XWScriptCompiler.GetModBuildMetrics(projectRoot).ActiveFlights == 0;
			backgroundCompile = compileWithinDeadline && compileTaskDrained && responsiveFrames > 1 && (compile?.Success ?? false) && File.Exists(compile.OutputAssemblyPath);
			Require(backgroundCompile, "最后 7 类角色脚本没有在保持主线程响应时完成后台编译。");
			Require(compileWithinDeadline, $"角色脚本后台编译超过真实时间上限：{compileWatch.Elapsed.TotalSeconds:0.###}s > {120.0:0}s。");
			Require(compileTaskDrained, "角色脚本后台编译结束后仍有遗留 single-flight 任务。");
			if (backgroundCompile)
			{
				compiledBaseTypes = ValidateCompiledBaseTypes(compile.OutputAssemblyPath, Examples, out compiledTypeCount);
			}
			Require(compiledBaseTypes, "7 个生成角色类型没有全部继承预期游戏基类。");
			XWFileSystemPanel fileSystemPanel = XWFileSystemPanel.Instance;
			XWFileSystemList fileList = fileSystemPanel?.GetNodeOrNull<XWFileSystemList>("%FileList");
			XW2DSceneEditor sceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			Require(GodotObject.IsInstanceValid(fileSystemPanel) && GodotObject.IsInstanceValid(fileList) && GodotObject.IsInstanceValid(sceneEditor), "外部测试 Mod 的文件列表或 2D 编辑器不可用。");
			string projectDir = Path.Combine(projectRoot, "Resources", "Characters");
			fileSystemPanel.NavigateToProject(projectDir);
			XWFileSystem.GetSingleton().ScanChanges();
			await WaitFrames(8);
			ExampleSpec[] examples2 = Examples;
			foreach (ExampleSpec spec in examples2)
			{
				string[] array2 = packagePaths[spec.TemplateId];
				string scenePath = array2[0];
				string configPath = array2[1];
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				bool flag8 = await ActivateFileListPath(fileSystemPanel, fileList, scenePath);
				if (flag8)
				{
					flag8 = await WaitForScene(sceneEditor, scenePath, 360);
				}
				bool flag9 = flag8;
				Node sceneRoot = sceneEditor?.CurrentSceneInstance;
				AdobeAnimateSpriteBase adobeAnimateSpriteBase = FindNode<AdobeAnimateSpriteBase>(sceneRoot);
				bool flag10 = flag9 && GodotObject.IsInstanceValid(sceneRoot) && GodotObject.IsInstanceValid(adobeAnimateSpriteBase) && GodotObject.IsInstanceValid(adobeAnimateSpriteBase.flashAnimeData);
				scenesOpened = scenesOpened && flag9;
				gameVisuals = gameVisuals && flag10;
				if (flag9)
				{
					sceneOpenedCount++;
				}
				Require(flag9, "真实文件树没有在 2D 编辑器打开角色场景：" + spec.TemplateId);
				Require(flag10, "角色场景没有可见 AdobeAnimateSpriteBase + flashAnimeData：" + spec.TemplateId);
				sceneEditor?.ShowDirectNodeProperties(sceneRoot, focusTab: false);
				await WaitFrames(3);
				XWDirectPropertySurface xWDirectPropertySurface = sceneEditor?.FindChild("NodeDirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
				bool flag11 = GodotObject.IsInstanceValid(xWDirectPropertySurface) && xWDirectPropertySurface.EditablePropertyCount > 0 && xWDirectPropertySurface.MountedPropertyCount == xWDirectPropertySurface.EditablePropertyCount && xWDirectPropertySurface.MissingPropertyCount == 0;
				directProperties = directProperties && flag11;
				Require(flag11, "2D 主面板角色节点属性拼图不完整：" + spec.TemplateId);
				bool flag12 = IsInspectorSentinel(inspectorSentinel);
				inspectorUntouched = inspectorUntouched && flag12;
				Require(flag12, "2D 角色场景错误占用了 Inspector：" + spec.TemplateId);
				if (string.Equals(spec.TemplateId, recreatedSpec.TemplateId, StringComparison.Ordinal) && sceneRoot is Node2D { Position: var from } node2D)
				{
					Vector2 editedPosition = from + new Vector2(13f, 7f);
					XWUndoRedoManager undoRedoManager = XWEditorInterface.Instance.GetUndoRedoManager();
					int currentSceneHistoryId = sceneEditor.GetCurrentSceneHistoryId();
					undoRedoManager.SetCurrentHistoryType(currentSceneHistoryId);
					undoRedoManager.CreateAction("验收 2D 场景保存重开", mergeMode: false, currentSceneHistoryId);
					undoRedoManager.AddDoProperty(node2D, "position", Variant.From(in editedPosition));
					undoRedoManager.AddUndoProperty(node2D, "position", Variant.From(in from));
					undoRedoManager.CommitAction();
					bool savedScene = node2D.Position.IsEqualApprox(editedPosition) && sceneEditor.SaveCurrentScene();
					await WaitFrames(3);
					FieldInfo? field = typeof(XW2DSceneEditor).GetField("_currentTabKey", BindingFlags.Instance | BindingFlags.NonPublic);
					MethodInfo method2 = typeof(XW2DSceneEditor).GetMethod("CloseSceneTabImmediately", BindingFlags.Instance | BindingFlags.NonPublic);
					string text2 = (field?.GetValue(sceneEditor) as string) ?? "";
					method2?.Invoke(sceneEditor, new object[1] { text2 });
					await WaitFrames(3);
					sceneEditor.LoadPackedSceneFromPath(scenePath);
					bool flag13 = await WaitForScene(sceneEditor, scenePath, 360);
					Node2D node2D2 = sceneEditor.CurrentSceneInstance as Node2D;
					sceneSaveReopen = savedScene && flag13 && GodotObject.IsInstanceValid(node2D2) && node2D2.Position.IsEqualApprox(editedPosition);
					Require(sceneSaveReopen, "代表性 2D 角色场景没有完成修改、保存、关闭并重新打开。");
				}
				Resource resource2 = loadedConfigs.GetValueOrDefault(spec.TemplateId) ?? LoadResource(configPath, ResourceLoader.CacheMode.Reuse);
				XWVisualEditorDescriptor descriptor;
				bool routeOne = GodotObject.IsInstanceValid(resource2) && XWResourceEditorRegistry.TryGetEditor(resource2, configPath, out descriptor) && descriptor.Category == "Character" && descriptor.DockKey == "character_editor";
				Require(routeOne, "角色配置没有注册 character_editor 路由：" + spec.TemplateId);
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await WaitFrames(1);
				bool configEntry = await ActivateFileListPath(fileSystemPanel, fileList, configPath);
				XWCharacterVisualResourceEditor characterEditor = await WaitForResourceEditor(configPath, 480);
				bool configOpenedOne = configEntry && routeOne && GodotObject.IsInstanceValid(characterEditor) && SamePath(characterEditor.ActiveResourcePath, configPath) && string.Equals(XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey, "character_editor", StringComparison.Ordinal);
				configsOpened = configsOpened && configOpenedOne;
				if (configOpenedOne)
				{
					configOpenedCount++;
				}
				Require(configOpenedOne, "真实文件树没有在角色编辑器打开配置：" + spec.TemplateId);
				await WaitFrames(4);
				AdobeAnimateSpriteBase adobeAnimateSpriteBase2 = FindNode<AdobeAnimateSpriteBase>(characterEditor);
				bool flag14 = configOpenedOne && GodotObject.IsInstanceValid(adobeAnimateSpriteBase2) && GodotObject.IsInstanceValid(adobeAnimateSpriteBase2.flashAnimeData);
				gameVisuals = gameVisuals && flag14;
				Require(flag14, "角色配置编辑器没有显示游戏角色动画：" + spec.TemplateId);
				bool flag15 = configOpenedOne && characterEditor.DirectEditablePropertyCount > 0 && characterEditor.DirectMountedPropertyCount == characterEditor.DirectEditablePropertyCount && characterEditor.DirectMissingPropertyCount == 0;
				directProperties = directProperties && flag15;
				Require(flag15, "角色配置主面板直接属性不完整：" + spec.TemplateId);
				PanelContainer panelContainer = characterEditor?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
				bool flag16 = IsInspectorSentinel(inspectorSentinel) && GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && characterEditor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
				inspectorUntouched = inspectorUntouched && flag16;
				Require(flag16, "角色配置错误占用了 Inspector：" + spec.TemplateId);
				if (string.Equals(spec.TemplateId, recreatedSpec.TemplateId, StringComparison.Ordinal))
				{
					TowerDefenseCharacterConfig editingConfig = characterEditor.ActiveResource as TowerDefenseCharacterConfig;
					SpinBox spinBox = characterEditor.FindChild("HitpointsSpinBox", recursive: true, owned: false) as SpinBox;
					XWUndoRedoManager history = XWEditorInterface.Instance.GetUndoRedoManager();
					history.SetCurrentHistoryType(0);
					double originalHitpoints = editingConfig?.hitpoints ?? (-1.0);
					double editedHitpoints = originalHitpoints + 37.0;
					if (GodotObject.IsInstanceValid(spinBox))
					{
						spinBox.Value = editedHitpoints;
					}
					await WaitFrames(4);
					bool savedScene = GodotObject.IsInstanceValid(editingConfig) && Math.Abs(editingConfig.hitpoints - editedHitpoints) < 0.001;
					bool undone = history.Undo();
					await WaitFrames(4);
					bool undoApplied = GodotObject.IsInstanceValid(editingConfig) && Math.Abs(editingConfig.hitpoints - originalHitpoints) < 0.001;
					bool redone = history.Redo();
					await WaitFrames(4);
					bool flag17 = GodotObject.IsInstanceValid(editingConfig) && Math.Abs(editingConfig.hitpoints - editedHitpoints) < 0.001;
					directEditUndoRedo = savedScene && undone && undoApplied && redone && flag17;
					Require(directEditUndoRedo, "角色主画面生命值控件没有完成真实编辑、撤销和重做。");
					typeof(XWCharacterVisualResourceEditor).GetMethod("SavePendingCharacterResource", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(characterEditor, null);
					cacheIgnoreReload = await WaitForPersistedHitpoints(configPath, editingConfig, editedHitpoints, TimeSpan.FromSeconds(5L));
					Require(cacheIgnoreReload, "角色直接属性没有通过 CacheMode.Ignore 从磁盘重载为重做后的值。");
				}
			}
			manifest = XWModManifest.Load(Path.Combine(projectRoot, "mod.json"));
			XWModManifest xWModManifest = manifest;
			if (xWModManifest.Provides == null)
			{
				xWModManifest.Provides = new Dictionary<string, List<string>>();
			}
			xWModManifest = manifest;
			if (xWModManifest.Overrides == null)
			{
				xWModManifest.Overrides = new Dictionary<string, List<string>>();
			}
			manifest.Provides["Character"] = Examples.Select((ExampleSpec exampleSpec2) => exampleSpec2.TechnicalName).ToList();
			manifest.Save(Path.Combine(projectRoot, "mod.json"));
			string exportDirectory = Path.Combine(projectRoot, ".acceptance-export");
			Directory.CreateDirectory(exportDirectory);
			string pmodPath = (backgroundCompile ? ModExporter.ExportFromDirectory(project.Name, new ModExporter.ModInfo
			{
				Name = project.Name,
				Version = project.Version,
				Author = project.Author,
				Description = project.Description
			}, projectRoot, project.CollectResourceFiles(), exportDirectory, compile.OutputAssemblyPath) : "");
			pmodExported = File.Exists(pmodPath);
			Require(pmodExported, "临时测试 Mod 没有导出携带声明程序集的 .pmod。");
			ModLoader.LoadedMod loadedMod = (pmodExported ? ModLoader.LoadMod(pmodPath) : null);
			modLoaded = loadedMod != null && loadedMod.Manifest != null && string.Equals(loadedMod.Manifest.RuntimeAssembly, "Runtime/ModAssembly.dll", StringComparison.Ordinal) && File.Exists(loadedMod.RuntimeAssemblyPath);
			Require(modLoaded, "ModLoader 没有加载显式声明运行时程序集的临时 .pmod。");
			string ownerId = loadedMod?.Manifest?.Id ?? "";
			modApplied = modLoaded && ModLoader.ApplyMod(loadedMod) && loadedMod.AppliedResourceCount == Examples.Length && loadedMod.CompanionBoundResourceCount == Examples.Length && XWModRuntimeRegistry.TryGetEffectiveRegistration("Character", recreatedSpec.TechnicalName, out var registration) && string.Equals(registration.OwnerMod, ownerId, StringComparison.OrdinalIgnoreCase);
			Require(modApplied, "临时 .pmod 没有按显式 Character 清单完整应用 7 个角色及其伴随脚本。");
			Node runtimeCharacter = null;
			if (modApplied)
			{
				Vector2I vector2I = new Vector2I(2, 3);
				TowerDefenseManager battleManager = TowerDefenseManager.Instance;
				runtimeCharacter = (GodotObject.IsInstanceValid(battleManager) ? battleManager.CreateCharacter(recreatedSpec.TechnicalName, vector2I) : null);
				bool configOpenedOne = GodotObject.IsInstanceValid(runtimeCharacter);
				bool configEntry = runtimeCharacter is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter.gridPos == vector2I;
				if (configOpenedOne && GodotObject.IsInstanceValid(runtimeCharacter))
				{
					AddChild(runtimeCharacter, forceReadableName: false, InternalMode.Disabled);
					await WaitFrames(3);
				}
				int num4 = ((GodotObject.IsInstanceValid(runtimeCharacter) && runtimeCharacter.HasMeta("mod_probe_runtime_ready_count")) ? runtimeCharacter.GetMeta("mod_probe_runtime_ready_count").AsInt32() : 0);
				bool flag18 = GodotObject.IsInstanceValid(runtimeCharacter) && runtimeCharacter.IsInsideTree();
				bool flag19 = GodotObject.IsInstanceValid(runtimeCharacter) && string.Equals(runtimeCharacter.GetType().Name, recreatedSpec.TechnicalName, StringComparison.Ordinal);
				bool flag20 = runtimeCharacter is TowerDefenseCharacter;
				bool flag21 = num4 == 1;
				bool flag22 = GodotObject.IsInstanceValid(runtimeCharacter) && runtimeCharacter.Get("config").AsGodotObject() is TowerDefenseCharacterConfig;
				AdobeAnimateSpriteBase adobeAnimateSpriteBase3 = FindNode<AdobeAnimateSpriteBase>(runtimeCharacter);
				bool flag23 = GodotObject.IsInstanceValid(adobeAnimateSpriteBase3) && GodotObject.IsInstanceValid(adobeAnimateSpriteBase3.flashAnimeData);
				companionRuntimeExecuted = configOpenedOne && flag18 && flag19 && flag20 && configEntry && flag21 && flag22 && flag23;
				battleFactory = companionRuntimeExecuted;
				modInstantiated = companionRuntimeExecuted;
				Require(companionRuntimeExecuted, "TowerDefenseManager.CreateCharacter 没有通过当前有效 Mod 注册创建并执行真实 C# 角色实例。" + $" managerValid={GodotObject.IsInstanceValid(battleManager)}" + $" created={configOpenedOne}" + $" insideTree={flag18}" + $" typeMatches={flag19}" + $" strongType={flag20}" + $" gridAssigned={configEntry}" + $" readyCount={num4}" + $" configValid={flag22}" + $" animationValid={flag23}");
			}
			if (GodotObject.IsInstanceValid(runtimeCharacter))
			{
				runtimeCharacter.QueueFree();
				await WaitFrames(3);
			}
			Stopwatch unloadWatch = Stopwatch.StartNew();
			bool unloadCalled = !string.IsNullOrWhiteSpace(ownerId) && ModLoader.UnloadMod(ownerId);
			unloadWatch.Stop();
			await WaitFrames(2);
			unloadResponsive = unloadCalled && unloadWatch.Elapsed < TimeSpan.FromMilliseconds(250L, 0L);
			modUnloaded = unloadCalled && !ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod item) => string.Equals(item.Manifest?.Id, ownerId, StringComparison.OrdinalIgnoreCase)) && !XWModRuntimeRegistry.TryGetEffectiveRegistration("Character", recreatedSpec.TechnicalName, out var _);
			Require(modUnloaded, "临时 Mod 的角色注册或可回收运行时程序集没有完成卸载。");
			Require(unloadResponsive, $"Mod 卸载在主线程耗时过长：{unloadWatch.Elapsed.TotalMilliseconds:0.###}ms。");
			string text3 = Path.Combine(exportDirectory, Path.GetFileNameWithoutExtension(pmodPath) + "_broken.pmod");
			ModLoader.LoadedMod loadedMod2 = (CreateBrokenCompanionPackage(pmodPath, text3, $"Resources/Characters/Zombies/{recreatedSpec.TechnicalName}/Scene/{recreatedSpec.TechnicalName}.tscn", "../Script/" + recreatedSpec.TechnicalName + ".cs", "../Script/MissingCompanion.cs") ? ModLoader.LoadMod(text3) : null);
			bool flag24 = loadedMod2 != null && ModLoader.ApplyMod(loadedMod2);
			rollbackOnBindingFailure = loadedMod2 != null && !flag24 && loadedMod2.AppliedResourceCount == 0 && loadedMod2.RuntimeResourceCache.Count == 0 && loadedMod2.CharacterCompanionRuntime == null && !ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod item) => string.Equals(item.Manifest?.Id, ownerId, StringComparison.OrdinalIgnoreCase)) && Examples.All((ExampleSpec exampleSpec2) => !XWModRuntimeRegistry.TryGetEffectiveRegistration("Character", exampleSpec2.TechnicalName, out var _));
			Require(rollbackOnBindingFailure, "伴随类型绑定失败后，ModLoader 没有完整回滚角色注册、缓存和可回收程序集。");
			bool flag25 = created && templateRecreated && chinese && packagesComplete && manifestCounts && backgroundCompile && compiledBaseTypes && scenesOpened && configsOpened && gameVisuals && directProperties && inspectorUntouched && saveReload && directEditUndoRedo && cacheIgnoreReload && sceneSaveReopen && pmodExported && modLoaded && modApplied && modInstantiated && modUnloaded && unloadResponsive && rollbackOnBindingFailure && compileWithinDeadline && compileTaskDrained && companionRuntimeExecuted && battleFactory && createdCount == 7 && sceneOpenedCount == 7 && configOpenedCount == 7 && compiledTypeCount == 7;
			HashSet<string> hashSet = library.Templates.Select((XWTemplateLibrary.TemplateInfo template) => template.Id).ToHashSet<string>(StringComparer.Ordinal);
			bool flag26 = library.Templates.Count == 92 && hashSet.Count == 92 && PostHistoricalTemplateIds.Count == 8 && PostHistoricalTemplateIds.All(hashSet.Contains);
			strongCount = (flag25 ? 84 : 77);
			missingCount = 92 - strongCount;
			completionMatrix = flag25 && flag26 && strongCount == 84 && missingCount == 8;
			Require(completionMatrix, "历史角色场景批次矩阵不是 strong=84/92、missing=8/92。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_TEST_MOD_CHARACTER_SCENE_EXAMPLES_PROBE] f3={f3} projectLoaded={projectLoaded} created={created} templateRecreated={templateRecreated} chinese={chinese} packagesComplete={packagesComplete} manifestCounts={manifestCounts} backgroundCompile={backgroundCompile} compileWithinDeadline={compileWithinDeadline} compileTaskDrained={compileTaskDrained} compiledBaseTypes={compiledBaseTypes} scenesOpened={scenesOpened} configsOpened={configsOpened} gameVisuals={gameVisuals} directProperties={directProperties} inspectorUntouched={inspectorUntouched} saveReload={saveReload} directEditUndoRedo={directEditUndoRedo} cacheIgnoreReload={cacheIgnoreReload} sceneSaveReopen={sceneSaveReopen} pmodExported={pmodExported} modLoaded={modLoaded} modApplied={modApplied} modInstantiated={modInstantiated} modUnloaded={modUnloaded} unloadResponsive={unloadResponsive} rollbackOnBindingFailure={rollbackOnBindingFailure} companionRuntimeExecuted={companionRuntimeExecuted} battleFactory={battleFactory} diskReload={diskReload} completionMatrix={completionMatrix} createdCount={createdCount}/7 sceneOpenedCount={sceneOpenedCount}/7 configOpenedCount={configOpenedCount}/7 compiledTypeCount={compiledTypeCount}/7 manifestScripts={manifestScripts} manifestResources={manifestResources} manifestBlueprints={manifestBlueprints} manifestTranslations={manifestTranslations} strong={strongCount}/92 missing={missingCount}/92 failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_TEST_MOD_CHARACTER_SCENE_EXAMPLES_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private static string[] GetCharacterPackagePaths(string projectRoot, ExampleSpec spec)
	{
		string path = Normalize(Path.Combine(projectRoot, spec.Folder, spec.TechnicalName));
		return new string[8]
		{
			Normalize(Path.Combine(path, "Scene", spec.TechnicalName + ".tscn")),
			Normalize(Path.Combine(path, "Config", spec.TechnicalName + "Config.tres")),
			Normalize(Path.Combine(path, "Sprite", spec.TechnicalName + ".tscn")),
			Normalize(Path.Combine(path, spec.TechnicalName + ".tres")),
			Normalize(Path.Combine(path, "Script", spec.TechnicalName + ".cs")),
			Normalize(Path.Combine(path, "DamagePoint", "DamagePointData.tres")),
			Normalize(Path.Combine(path, "Custom", "CustomData.tres")),
			Normalize(Path.Combine(path, "Armor", "ArmorData.tres"))
		};
	}

	private static bool PackageFilesExist(IEnumerable<string> paths)
	{
		return paths?.All(File.Exists) ?? false;
	}

	private static bool ValidatePackageFiles(string projectRoot, ExampleSpec spec, IReadOnlyList<string> paths)
	{
		if (paths == null || paths.Count != 8 || !paths.All(File.Exists))
		{
			return false;
		}
		if (paths.Any((string path) => XWTemplatePresentation.HasChineseText(Path.GetRelativePath(projectRoot, path).Replace('\\', '/'))))
		{
			return false;
		}
		string text = File.ReadAllText(paths[0]);
		string text2 = File.ReadAllText(paths[1]);
		string text3 = File.ReadAllText(paths[2]);
		string text4 = File.ReadAllText(paths[3]);
		string text5 = File.ReadAllText(paths[4]);
		if (new string[5] { text, text2, text3, text4, text5 }.Any((string text6) => text6.Contains("${", StringComparison.Ordinal)))
		{
			return false;
		}
		if (text.Contains("../Config/" + spec.TechnicalName + "Config.tres", StringComparison.Ordinal) && text.Contains("../Script/" + spec.TechnicalName + ".cs", StringComparison.Ordinal) && text.Contains("../Sprite/" + spec.TechnicalName + ".tscn", StringComparison.Ordinal) && text.Contains("metadata/mod_character_category = \"" + spec.Category + "\"", StringComparison.Ordinal) && text.Contains("metadata/mod_character_script_binding = \"CompanionOnly\"", StringComparison.Ordinal) && !text.Contains("[ext_resource type=\"Script\" path=\"../Script/", StringComparison.Ordinal) && !text.Contains("script = ExtResource(\"2_script\")", StringComparison.Ordinal) && text2.Contains("script_class=\"" + spec.ExpectedConfigType + "\"", StringComparison.Ordinal) && text2.Contains("resource_name = \"" + spec.DisplayName + "\"", StringComparison.Ordinal) && text2.Contains("name = \"" + spec.TechnicalName + "\"", StringComparison.Ordinal) && text2.Contains("../DamagePoint/DamagePointData.tres", StringComparison.Ordinal) && text2.Contains("../Armor/ArmorData.tres", StringComparison.Ordinal) && text2.Contains("../Custom/CustomData.tres", StringComparison.Ordinal) && text3.Contains("type=\"PackedScene\"", StringComparison.Ordinal) && text3.Contains("instance=ExtResource(\"1_game_sprite\")", StringComparison.Ordinal) && text3.Contains("metadata/mod_preview_source = \"内置游戏角色视觉\"", StringComparison.Ordinal) && text4.Contains("script_class=\"AdobeAnimateData\"", StringComparison.Ordinal))
		{
			return text5.Contains("public partial class " + spec.TechnicalName + " : " + spec.ExpectedBaseType, StringComparison.Ordinal);
		}
		return false;
	}

	private static Resource LoadResource(string path, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
	{
		if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<Resource>(ProjectSettings.LocalizePath(path).Replace('\\', '/'), "", cacheMode);
	}

	private async Task<bool> ActivateFileListPath(XWFileSystemPanel panel, XWFileSystemList fileList, string path)
	{
		if (!GodotObject.IsInstanceValid(panel) || !GodotObject.IsInstanceValid(fileList))
		{
			return false;
		}
		panel.NavigateToPath(path);
		await WaitFrames(3);
		int[] selectedItems = fileList.GetSelectedItems();
		if (selectedItems.Length != 1 || selectedItems[0] < 0 || selectedItems[0] >= fileList.ItemCount)
		{
			return false;
		}
		fileList.EmitSignal(ItemList.SignalName.ItemActivated, (long)selectedItems[0]);
		return true;
	}

	private async Task<bool> WaitForScene(XW2DSceneEditor editor, string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(editor.CurrentPackedScene) && SamePath(editor.CurrentPackedScene.ResourcePath, expectedPath) && GodotObject.IsInstanceValid(editor.CurrentSceneInstance) && string.Equals(XWEditorInterface.Instance.GetLayoutManager()?.ActiveMainPanelKey, "2d_editor", StringComparison.Ordinal))
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private async Task<XWCharacterVisualResourceEditor> WaitForResourceEditor(string expectedPath, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWCharacterVisualResourceEditor xWCharacterVisualResourceEditor = XWEditorInterface.Instance?.GetResourceEditor("character_editor") as XWCharacterVisualResourceEditor;
			if (GodotObject.IsInstanceValid(xWCharacterVisualResourceEditor) && SamePath(xWCharacterVisualResourceEditor.ActiveResourcePath, expectedPath))
			{
				return xWCharacterVisualResourceEditor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return null;
	}

	private static T FindNode<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is T result)
		{
			return result;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNode<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static bool ValidateCompiledBaseTypes(string assemblyPath, IEnumerable<ExampleSpec> specs, out int matchedCount)
	{
		matchedCount = 0;
		foreach (ExampleSpec spec in specs)
		{
			if (HasCompiledBaseType(assemblyPath, spec.TechnicalName, spec.ExpectedBaseType))
			{
				matchedCount++;
			}
		}
		return matchedCount == Examples.Length;
	}

	private static bool InjectRuntimeReadyCounter(string scriptPath)
	{
		if (string.IsNullOrWhiteSpace(scriptPath) || !File.Exists(scriptPath))
		{
			return false;
		}
		string text = File.ReadAllText(scriptPath);
		if (text.Contains("RuntimeReadyCount", StringComparison.Ordinal))
		{
			return true;
		}
		string text2 = text.Replace("\r\n", "\n");
		int num = text2.IndexOf("    public override void _Ready()\n    {\n", StringComparison.Ordinal);
		if (num < 0)
		{
			return false;
		}
		text2 = text2.Insert(num, "    public static int RuntimeReadyCount;\n\n");
		int num2 = text2.IndexOf("    public override void _Ready()\n    {\n", num + "    public static int RuntimeReadyCount;\n\n".Length, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return false;
		}
		num2 += "    public override void _Ready()\n    {\n".Length;
		text2 = text2.Insert(num2, "        RuntimeReadyCount++;\n        SetMeta(\"mod_probe_runtime_ready_count\", RuntimeReadyCount);\n");
		File.WriteAllText(scriptPath, text2);
		return true;
	}

	private static bool CreateBrokenCompanionPackage(string sourcePackage, string outputPackage, string sceneEntryPath, string expectedScriptPath, string missingScriptPath)
	{
		if (!File.Exists(sourcePackage) || string.IsNullOrWhiteSpace(outputPackage) || string.IsNullOrWhiteSpace(sceneEntryPath))
		{
			return false;
		}
		bool flag = false;
		using FileStream stream = File.OpenRead(sourcePackage);
		using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Read);
		using FileStream stream2 = new FileStream(outputPackage, FileMode.Create, System.IO.FileAccess.Write, FileShare.None);
		using ZipArchive zipArchive2 = new ZipArchive(stream2, ZipArchiveMode.Create);
		foreach (ZipArchiveEntry entry in zipArchive.Entries)
		{
			ZipArchiveEntry zipArchiveEntry = zipArchive2.CreateEntry(entry.FullName);
			using Stream stream3 = entry.Open();
			using Stream stream4 = zipArchiveEntry.Open();
			if (entry.FullName.Equals(sceneEntryPath, StringComparison.Ordinal))
			{
				using StreamReader streamReader = new StreamReader(stream3);
				string text = streamReader.ReadToEnd();
				string text2 = text.Replace(expectedScriptPath, missingScriptPath, StringComparison.Ordinal);
				flag = !string.Equals(text, text2, StringComparison.Ordinal);
				using StreamWriter streamWriter = new StreamWriter(stream4);
				streamWriter.Write(text2);
			}
			else
			{
				stream3.CopyTo(stream4);
			}
		}
		return flag && File.Exists(outputPackage);
	}

	private async Task<bool> WaitForPersistedHitpoints(string configPath, TowerDefenseCharacterConfig liveConfig, double expectedHitpoints, TimeSpan timeout)
	{
		Stopwatch watch = Stopwatch.StartNew();
		while (watch.Elapsed < timeout)
		{
			Resource resource = LoadResource(configPath, ResourceLoader.CacheMode.Ignore);
			bool num = resource is TowerDefenseCharacterConfig towerDefenseCharacterConfig && towerDefenseCharacterConfig != liveConfig && Math.Abs(towerDefenseCharacterConfig.hitpoints - expectedHitpoints) < 0.001;
			if (GodotObject.IsInstanceValid(resource))
			{
				resource.Dispose();
			}
			if (num)
			{
				return true;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
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

	private static bool ManifestContains(string projectRoot, XWModManifest manifest, string absolutePath)
	{
		string relative = XWModManifestSyncService.ToProjectRelativeManifestPath(absolutePath, projectRoot);
		if (!absolutePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
		{
			if (manifest == null)
			{
				return false;
			}
			return manifest.Resources?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) == true;
		}
		if (manifest == null)
		{
			return false;
		}
		return manifest.Scripts?.Any((string entry) => string.Equals(entry, relative, StringComparison.OrdinalIgnoreCase)) == true;
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
			return array.All((string relative) => !Path.IsPathRooted(relative) && !relative.Contains("..", StringComparison.Ordinal) && File.Exists(Path.Combine(projectRoot, relative.Replace('/', Path.DirectorySeparatorChar))));
		}
		return false;
	}

	private static bool IsInspectorSentinel(Node inspectorSentinel)
	{
		XWInspector xWInspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
		if (GodotObject.IsInstanceValid(xWInspector))
		{
			return xWInspector.CurrentObject == inspectorSentinel;
		}
		return false;
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

	private static bool IsPathInsideRoot(string candidatePath, string rootPath)
	{
		if (string.IsNullOrWhiteSpace(candidatePath) || string.IsNullOrWhiteSpace(rootPath))
		{
			return false;
		}
		string text = Normalize(candidatePath);
		string value = Normalize(rootPath).TrimEnd('/') + "/";
		return text.StartsWith(value, StringComparison.OrdinalIgnoreCase);
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
