using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorBlueprintStateMachineRuntimeProbe : Node
{
	private const string OwnerId = "blueprint-state-machine-probe";

	private readonly List<string> _failures = new List<string>();

	private string _probeParent = string.Empty;

	private StateMachineController _controller;

	private StateChart _productionChart;

	private XWScriptEditor _scriptEditor;

	public override async void _Ready()
	{
		bool resourceRoundTrip = false;
		bool generated = false;
		bool backgroundCompile = false;
		bool compiled = false;
		bool catalog = false;
		bool productionHost = false;
		bool runtime = false;
		bool guardFalse = false;
		bool hotPath = false;
		bool f3 = false;
		bool directRoute = false;
		bool inspectorUntouched = false;
		bool leaseBlocked = false;
		bool unloaded = false;
		bool productionLeaseBlocked = false;
		bool productionRuntimeRetained = false;
		bool productionHotReloadBlocked = false;
		bool productionUnloaded = false;
		bool productionHotReloaded = false;
		try
		{
			_probeParent = Path.Combine(ProjectSettings.GlobalizePath("user://BlueprintStateMachineRuntimeProbe/"), Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_probeParent);
			ModProject project = ModProject.Create(_probeParent, "中文蓝图状态机验证", "1.0.0", "自动验证", "验证蓝图动作在游戏状态机中的真实运行闭环");
			Require(project != null, "无法创建隔离的中文蓝图 Mod 工程。");
			if (project == null)
			{
				Finish(resourceRoundTrip, generated, backgroundCompile, compiled, catalog, productionHost, runtime, guardFalse, hotPath, f3, directRoute, inspectorUntouched, leaseBlocked, unloaded, productionLeaseBlocked, productionRuntimeRetained, productionHotReloadBlocked, productionUnloaded, productionHotReloaded);
				return;
			}
			string blueprintPath = Path.Combine(project.ProjectPath, "Scripts", "中文状态机动作.tres");
			string text = Path.Combine(project.ProjectPath, "Scripts", "中文状态机动作.generated.cs");
			string path = Path.Combine(project.ProjectPath, "Resources", "StateMachines", "中文蓝图状态机.tres");
			Directory.CreateDirectory(Path.GetDirectoryName(path) ?? project.ProjectPath);
			Error error = ResourceSaver.Save(CreateBlueprint(), blueprintPath, ResourceSaver.SaverFlags.None);
			XWBPScriptData xWBPScriptData = ResourceLoader.Load<XWBPScript>(blueprintPath, "", ResourceLoader.CacheMode.Ignore)?.Deserialize();
			resourceRoundTrip = error == Error.Ok && xWBPScriptData != null && xWBPScriptData.Functions.Count == 5 && xWBPScriptData.Functions.Values.All((XWBPFunctionData function) => function.StateMachineCallbackEnabled && !string.IsNullOrWhiteSpace(function.StateMachineCallbackLocalKey));
			Require(resourceRoundTrip, "五种蓝图动作绑定没有通过资源保存与重新加载。");
			string className = XWBPCodeGenerator.BuildGeneratedClassName(blueprintPath, project.ProjectPath);
			string text2 = string.Empty;
			try
			{
				text2 = new XWBPCodeGenerator(xWBPScriptData)
				{
					ClassName = className,
					BlueprintSourcePath = "Scripts/中文状态机动作.tres"
				}.Generate();
				text2 = XWBlueprintGeneratedCSharpPolicy.AttachMetadata(text2, blueprintPath, text);
				File.WriteAllText(text, text2);
			}
			catch (Exception ex)
			{
				Require(condition: false, "蓝图 C# 生成失败：" + ex.Message);
			}
			generated = CountOccurrences(text2, "[StateMachineCallback(") == 5 && text2.Contains("StateMachineCallbackSourceKind.Blueprint", StringComparison.Ordinal) && text2.Contains("Scripts/中文状态机动作.tres", StringComparison.Ordinal) && text2.Contains("in StateMachineGuardContext context", StringComparison.Ordinal);
			Require(generated, "生成代码没有包含五种蓝图状态机包装方法及来源信息。");
			StateMachineDefinition definition = CreateStateMachineDefinition();
			Error error2 = ResourceSaver.Save(definition, path, ResourceSaver.SaverFlags.None);
			Require(error2 == Error.Ok, "中文蓝图状态机资源保存失败：" + error2);
			XWModManifestSyncService.SyncProject(project.ProjectPath);
			string path2 = Path.Combine(project.ProjectPath, "Assets", "Textures", "lease_probe.svg");
			Directory.CreateDirectory(Path.GetDirectoryName(path2) ?? project.ProjectPath);
			File.WriteAllText(path2, "<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"2\" height=\"2\" viewBox=\"0 0 2 2\"><rect width=\"2\" height=\"2\" fill=\"#45d07f\"/></svg>");
			XWModManifest xWModManifest = XWModManifest.Load(Path.Combine(project.ProjectPath, "mod.json"));
			Require(xWModManifest != null, "无法加载蓝图状态机运行时回归的 Mod 清单。");
			if (xWModManifest != null)
			{
				xWModManifest.Id = "blueprint-state-machine-probe";
				XWModManifest xWModManifest2 = xWModManifest;
				if (xWModManifest2.Provides == null)
				{
					xWModManifest2.Provides = new Dictionary<string, List<string>>();
				}
				xWModManifest.Provides["Texture"] = new List<string> { "lease_probe" };
				xWModManifest.Save(Path.Combine(project.ProjectPath, "mod.json"));
			}
			ModEditorPanel editorPanel = await OpenModEditorAsync();
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			int num;
			if (GodotObject.IsInstanceValid(editorPanel))
			{
				object obj = method?.Invoke(editorPanel, new object[1] { project });
				num = ((obj is bool && (bool)obj) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool enteredProject = (byte)num != 0;
			await WaitFrames(12);
			_scriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			Task<XWScriptCompiler.CompileResult> compileTask = ((!enteredProject) ? null : _scriptEditor?.CompileScriptsAsync());
			Require(compileTask != null, "真实 F3 C# 脚本编辑器没有启动 Mod 编译。");
			if (compileTask == null)
			{
				throw new InvalidOperationException("真实 F3 C# 脚本编辑器不可用。");
			}
			int liveFrames = await WaitForTask(compileTask, 3600);
			XWScriptCompiler.CompileResult compileResult = await compileTask;
			backgroundCompile = liveFrames > 0;
			compiled = (compileResult?.Success ?? false) && File.Exists(compileResult.OutputAssemblyPath);
			Require(backgroundCompile, "外部 Mod 编译期间 Godot 主线程没有继续处理帧。");
			Require(compiled, "生成的蓝图状态机包装器没有通过真实外部 Mod 编译：" + (compileResult?.Output ?? "无编译结果"));
			if (!compiled)
			{
				throw new InvalidOperationException("蓝图状态机外部 Mod 编译失败，停止后续加载验证。");
			}
			StateMachineCallbackCatalogEntry[] entries = StateMachineCallbackRegistry.Shared.GetCatalogSnapshot().Entries;
			catalog = _scriptEditor?.CallbackPreviewOwnerId == "blueprint-state-machine-probe" && entries.Count((StateMachineCallbackCatalogEntry entry) => entry.OwnerId == "blueprint-state-machine-probe" && entry.SourceKind == StateMachineCallbackSourceKind.Blueprint && entry.SourcePath == "Scripts/中文状态机动作.tres") == 5;
			Require(catalog, "真实 F3 编译没有用 mod.json 的固定 Mod ID 刷新五个蓝图来源动作。");
			_productionChart = new StateChart
			{
				Name = "BlueprintProductionStateChart",
				RuntimeMode = StateMachineRuntimeMode.Resource,
				Definition = definition
			};
			AddChild(_productionChart, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(10);
			productionHost = _productionChart.ResourceInitializationSucceeded && string.IsNullOrWhiteSpace(_productionChart.InitializationError) && (_productionChart.GetStateById("待机")?.IsActive ?? false);
			Require(productionHost, "真实 StateChart 生产入口没有用普通宿主运行蓝图动作：" + _productionChart.InitializationError);
			_productionChart.SendEvent("尝试激活");
			int num2;
			if (_productionChart.GetStateById("待机")?.IsActive ?? false)
			{
				StateHandle stateById = _productionChart.GetStateById("激活");
				num2 = ((stateById != null && !stateById.IsActive) ? 1 : 0);
			}
			else
			{
				num2 = 0;
			}
			guardFalse = (byte)num2 != 0;
			_productionChart.SendEvent("强制激活");
			int num3;
			if (productionHost && guardFalse)
			{
				StateHandle stateById2 = _productionChart.GetStateById("待机");
				if (stateById2 != null && !stateById2.IsActive)
				{
					num3 = ((_productionChart.GetStateById("激活")?.IsActive ?? false) ? 1 : 0);
					goto IL_09df;
				}
			}
			num3 = 0;
			goto IL_09df;
			IL_09df:
			runtime = (byte)num3 != 0;
			Require(guardFalse, "蓝图 Guard 的 false 返回值没有阻止真实 StateChart 切换。");
			Require(runtime, "蓝图动作没有通过真实 StateChart 宿主完成进入、退出和切换。");
			_controller = new StateMachineController();
			bool condition = _controller.Initialize(definition, new object(), "blueprint-state-machine-probe") && _controller.EnterInitialState();
			_controller.TickProcess(0.016);
			_controller.TickPhysics(0.02);
			Require(condition, "普通对象宿主没有建立蓝图状态机热路径。");
			long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
			Stopwatch stopwatch = Stopwatch.StartNew();
			for (int num4 = 0; num4 < 10000; num4++)
			{
				_controller.TickProcess(0.016);
			}
			stopwatch.Stop();
			long num5 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
			hotPath = num5 <= 1024 && stopwatch.ElapsedMilliseconds < 1000;
			Require(hotPath, $"蓝图 Process 热路径发生额外分配或明显卡顿：allocated={num5}, elapsedMs={stopwatch.ElapsedMilliseconds}。");
			leaseBlocked = _scriptEditor != null && !_scriptEditor.TrySwitchProjectRoot("");
			Require(leaseBlocked, "活动状态机没有阻止 F3 动作目录提前卸载。");
			(f3, directRoute, inspectorUntouched) = await ProbeRealEditorRouteAsync(editorPanel, definition, blueprintPath);
			Require(f3, "F3 没有打开真实 Mod 编辑器。");
			Require(directRoute, "状态机的蓝图动作卡没有跳转到对应蓝图函数。");
			Require(inspectorUntouched, "从状态机打开蓝图动作时污染了原始 Inspector。");
			_controller.Dispose();
			_controller = null;
			_productionChart.QueueFree();
			_productionChart = null;
			await WaitFrames(8);
			unloaded = (_scriptEditor?.TrySwitchProjectRoot("") ?? false) && string.IsNullOrWhiteSpace(_scriptEditor.CallbackPreviewOwnerId);
			Require(unloaded, "状态机释放后 F3 动作目录仍无法卸载。");
			ModEditorManager instance = ModEditorManager.Instance;
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.QueueFree();
				await WaitFrames(10);
			}
			string text3 = Path.Combine(project.ProjectPath, ".lease-export");
			Directory.CreateDirectory(text3);
			string text4 = ModExporter.ExportFromDirectory(project.Name, new ModExporter.ModInfo
			{
				Name = project.Name,
				Version = project.Version,
				Author = project.Author,
				Description = project.Description
			}, project.ProjectPath, project.CollectResourceFiles(), text3, compileResult.OutputAssemblyPath);
			ModLoader.LoadedMod loadedMod = (File.Exists(text4) ? ModLoader.LoadMod(text4) : null);
			bool flag = loadedMod != null && ModLoader.ApplyMod(loadedMod) && (loadedMod.CharacterCompanionRuntime?.IsLoaded ?? false);
			Require(flag, "真实 ModLoader/CompanionRuntime 没有应用带回调程序集的包。");
			using (StateMachineController stateMachineController = new StateMachineController())
			{
				bool flag2 = flag && stateMachineController.Initialize(definition, new object(), "blueprint-state-machine-probe");
				Require(flag2, "真实 ModLoader 程序集没有建立状态机 binding 租约。");
				StateMachineUnloadBlockers blockers;
				bool flag3 = ModLoader.UnloadMod("blueprint-state-machine-probe", out blockers);
				productionLeaseBlocked = flag2 && !flag3 && blockers != null && blockers.LeaseCount > 0;
				productionRuntimeRetained = productionLeaseBlocked && ModLoader.GetLoadedMods().Contains(loadedMod) && (loadedMod.CharacterCompanionRuntime?.IsLoaded ?? false) && string.Equals(loadedMod.CharacterCompanionRuntime.OwnerId, "blueprint-state-machine-probe", StringComparison.Ordinal);
				Require(productionLeaseBlocked, "活动 binding 没有让真实 ModLoader 返回卸载 blocker。");
				Require(productionRuntimeRetained, "卸载被阻止后真实 LoadedMod 或 CompanionRuntime 句柄丢失。");
				ModLoader.LoadedMod loadedMod2 = ModLoader.LoadMod(text4);
				productionHotReloadBlocked = loadedMod2 != null && !ModLoader.ApplyMod(loadedMod2) && ModLoader.GetLoadedMods().Contains(loadedMod) && (loadedMod.CharacterCompanionRuntime?.IsLoaded ?? false);
				Require(productionHotReloadBlocked, "活动 binding 期间热重载没有保留原 LoadedMod。");
			}
			productionUnloaded = ModLoader.UnloadMod("blueprint-state-machine-probe", out var blockers2) && blockers2 != null && blockers2.LeaseCount == 0 && !ModLoader.GetLoadedMods().Any((ModLoader.LoadedMod item) => string.Equals(item.Manifest?.Id, "blueprint-state-machine-probe", StringComparison.OrdinalIgnoreCase));
			Require(productionUnloaded, "释放 binding 后真实 ModLoader 仍不能完成卸载。");
			ModLoader.LoadedMod loadedMod3 = ModLoader.LoadMod(text4);
			productionHotReloaded = loadedMod3 != null && ModLoader.ApplyMod(loadedMod3) && (loadedMod3.CharacterCompanionRuntime?.IsLoaded ?? false) && ModLoader.UnloadMod("blueprint-state-machine-probe");
			Require(productionHotReloaded, "释放 binding 后同 Owner Mod 没有完成真实热重载与再次卸载。");
		}
		catch (Exception ex2)
		{
			Require(condition: false, ex2.ToString());
		}
		finally
		{
			_controller?.Dispose();
			_controller = null;
			if (GodotObject.IsInstanceValid(_productionChart))
			{
				_productionChart.QueueFree();
				_productionChart = null;
			}
			_scriptEditor?.TrySwitchProjectRoot("");
			ModLoader.UnloadMod("blueprint-state-machine-probe");
			ModEditorManager instance2 = ModEditorManager.Instance;
			if (GodotObject.IsInstanceValid(instance2))
			{
				instance2.QueueFree();
			}
			try
			{
				if (!string.IsNullOrWhiteSpace(_probeParent) && Directory.Exists(_probeParent))
				{
					Directory.Delete(_probeParent, recursive: true);
				}
			}
			catch (Exception ex3)
			{
				Require(condition: false, "清理临时 Mod 失败：" + ex3.Message);
			}
		}
		Finish(resourceRoundTrip, generated, backgroundCompile, compiled, catalog, productionHost, runtime, guardFalse, hotPath, f3, directRoute, inspectorUntouched, leaseBlocked, unloaded, productionLeaseBlocked, productionRuntimeRetained, productionHotReloadBlocked, productionUnloaded, productionHotReloaded);
	}

	private static XWBPScript CreateBlueprint()
	{
		XWBPScript xWBPScript = XWBPScript.Create();
		XWBPScriptData xWBPScriptData = xWBPScript.Deserialize();
		xWBPScriptData.ExtendsClass = "RefCounted";
		AddCallbackFunction(xWBPScriptData, "进入动作", "进入", StateMachineCallbackPhase.Enter);
		AddCallbackFunction(xWBPScriptData, "退出动作", "退出", StateMachineCallbackPhase.Exit);
		AddCallbackFunction(xWBPScriptData, "每帧动作", "每帧", StateMachineCallbackPhase.Process);
		AddCallbackFunction(xWBPScriptData, "物理帧动作", "物理帧", StateMachineCallbackPhase.PhysicsProcess);
		AddCallbackFunction(xWBPScriptData, "切换条件", "允许切换", StateMachineCallbackPhase.Guard);
		xWBPScript.Serialize(xWBPScriptData);
		return xWBPScript;
	}

	private static void AddCallbackFunction(XWBPScriptData owner, string name, string localKey, StateMachineCallbackPhase phase)
	{
		XWBPFunctionData xWBPFunctionData = XWBPFunctionData.Create();
		xWBPFunctionData.Owner = owner;
		xWBPFunctionData.Name = name;
		xWBPFunctionData.StateMachineCallbackEnabled = true;
		xWBPFunctionData.StateMachineCallbackLocalKey = localKey;
		xWBPFunctionData.StateMachineCallbackPhase = phase;
		if ((uint)(phase - 2) <= 1u)
		{
			xWBPFunctionData.Inputs.Add(new XWBPNodePortData("delta", XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Float, null, Variant.From<double>(0.0)));
			xWBPFunctionData.InputsSet();
		}
		if (phase == StateMachineCallbackPhase.Guard)
		{
			xWBPFunctionData.Outputs.Add(new XWBPNodePortData("result", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Bool, null, Variant.From<bool>(false)));
			xWBPFunctionData.OutputsSet();
		}
		owner.AddFunction(xWBPFunctionData);
	}

	private static StateMachineDefinition CreateStateMachineDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "测试.中文蓝图状态机",
			RootStateId = "根状态",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "根状态",
					DisplayName = "根状态",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "待机"
				},
				new StateMachineStateDefinition
				{
					StableId = "待机",
					DisplayName = "待机",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "根状态",
					ProcessFlags = (StateMachineProcessFlags.Process | StateMachineProcessFlags.PhysicsProcess),
					EnterCallbackKey = "mod/blueprint-state-machine-probe/进入",
					ExitCallbackKey = "mod/blueprint-state-machine-probe/退出",
					ProcessCallbackKey = "mod/blueprint-state-machine-probe/每帧",
					PhysicsProcessCallbackKey = "mod/blueprint-state-machine-probe/物理帧"
				},
				new StateMachineStateDefinition
				{
					StableId = "激活",
					DisplayName = "激活",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "根状态",
					EnterCallbackKey = "mod/blueprint-state-machine-probe/进入",
					ExitCallbackKey = "mod/blueprint-state-machine-probe/退出"
				}
			},
			Transitions = 
			{
				new StateMachineTransitionDefinition
				{
					StableId = "条件激活",
					SourceStateId = "待机",
					TargetStateId = "激活",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "尝试激活",
					Priority = 100,
					GuardDefinition = new StateMachineGuardDefinition
					{
						Kind = StateMachineGuardKind.Callback,
						CallbackKey = "mod/blueprint-state-machine-probe/允许切换"
					}
				},
				new StateMachineTransitionDefinition
				{
					StableId = "强制激活",
					SourceStateId = "待机",
					TargetStateId = "激活",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "强制激活"
				},
				new StateMachineTransitionDefinition
				{
					StableId = "返回待机",
					SourceStateId = "激活",
					TargetStateId = "待机",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "返回待机"
				}
			}
		};
	}

	private async Task<(bool f3, bool route, bool inspector)> ProbeRealEditorRouteAsync(ModEditorPanel panel, StateMachineDefinition definition, string blueprintPath)
	{
		XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
		Node sentinel = new Node
		{
			Name = "BlueprintStateMachineInspectorSentinel"
		};
		AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
		XWEditorInterface.Instance?.InspectObject(sentinel);
		bool routed = GodotObject.IsInstanceValid(panel) && XWResourceEditorRegistry.TryOpen(definition, definition.ResourcePath);
		XWEditorInterface.Instance?.FocusPanel("state_machine_editor");
		await WaitFrames(8);
		XWStateMachineVisualResourceEditor xWStateMachineVisualResourceEditor = XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor") as XWStateMachineVisualResourceEditor;
		if (xWStateMachineVisualResourceEditor?.WorkbenchTabs != null)
		{
			xWStateMachineVisualResourceEditor.WorkbenchTabs.CurrentTab = 0;
		}
		StateMachineGraphEditorSurface surface = xWStateMachineVisualResourceEditor?.GraphSurface;
		surface?.NavigateToStableId("待机");
		await WaitFrames(6);
		surface?.DetailsPanel?.Show();
		Button openBlueprint = surface?.DetailsPanel?.FindChildren("ProcessOpenBlueprint*", "Button", recursive: true, owned: false).OfType<Button>().FirstOrDefault();
		openBlueprint?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(8);
		XWBPEditor xWBPEditor = XWEditorInterface.Instance?.GetBlueprintEditor() as XWBPEditor;
		bool item = routed && GodotObject.IsInstanceValid(openBlueprint) && GodotObject.IsInstanceValid(xWBPEditor) && string.Equals(Path.GetFullPath(xWBPEditor.BpScript?.ResourcePath ?? string.Empty), Path.GetFullPath(blueprintPath), StringComparison.OrdinalIgnoreCase) && xWBPEditor.GetGraphEditor()?.GraphData is XWBPFunctionData { StateMachineCallbackPhase: StateMachineCallbackPhase.Process } xWBPFunctionData && xWBPFunctionData.StateMachineCallbackLocalKey == "每帧";
		bool item2 = inspector == null || inspector.CurrentObject == sentinel;
		return (f3: GodotObject.IsInstanceValid(panel), route: item, inspector: item2);
	}

	private async Task<ModEditorPanel> OpenModEditorAsync()
	{
		ModEditorManager modEditorManager = ModEditorManager.Instance;
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			modEditorManager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(modEditorManager))
			{
				AddChild(modEditorManager, forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return null;
		}
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
		for (int frame = 0; frame < 900; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			Node instance = modEditorPanel?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(modEditorPanel) && !GodotObject.IsInstanceValid(instance))
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<int> WaitForTask(Task task, int maximumFrames)
	{
		int frames = 0;
		while (!task.IsCompleted && frames < maximumFrames)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			frames++;
		}
		if (!task.IsCompleted)
		{
			throw new TimeoutException("等待蓝图 Mod 后台编译超时。");
		}
		return frames;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static int CountOccurrences(string source, string value)
	{
		int num = 0;
		int startIndex = 0;
		while (!string.IsNullOrEmpty(source) && !string.IsNullOrEmpty(value) && (startIndex = source.IndexOf(value, startIndex, StringComparison.Ordinal)) >= 0)
		{
			num++;
			startIndex += value.Length;
		}
		return num;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_STATE_MACHINE_FAILURE] " + message);
		}
	}

	private void Finish(bool resourceRoundTrip, bool generated, bool backgroundCompile, bool compiled, bool catalog, bool productionHost, bool runtime, bool guardFalse, bool hotPath, bool f3, bool directRoute, bool inspectorUntouched, bool leaseBlocked, bool unloaded, bool productionLeaseBlocked, bool productionRuntimeRetained, bool productionHotReloadBlocked, bool productionUnloaded, bool productionHotReloaded)
	{
		GD.Print("[MOD_EDITOR_BLUEPRINT_STATE_MACHINE] " + $"resourceRoundTrip={resourceRoundTrip} " + $"generated={generated} " + $"backgroundCompile={backgroundCompile} " + $"compiled={compiled} catalog={catalog} " + $"productionHost={productionHost} " + $"runtime={runtime} guardFalse={guardFalse} " + $"hotPath={hotPath} f3={f3} " + $"directRoute={directRoute} " + $"inspectorUntouched={inspectorUntouched} " + $"leaseBlocked={leaseBlocked} " + $"unloaded={unloaded} " + $"productionLeaseBlocked={productionLeaseBlocked} " + $"productionRuntimeRetained={productionRuntimeRetained} " + $"productionHotReloadBlocked={productionHotReloadBlocked} " + $"productionUnloaded={productionUnloaded} " + $"productionHotReloaded={productionHotReloaded} " + $"failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_STATE_MACHINE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
