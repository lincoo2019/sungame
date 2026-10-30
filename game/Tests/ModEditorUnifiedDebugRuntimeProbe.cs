using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Debugging;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ScriptEditor;

public partial class ModEditorUnifiedDebugRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private string _sandboxRoot = string.Empty;

	public override async void _Ready()
	{
		bool realF3 = false;
		bool csharpPausedWithoutFreeze = false;
		bool csharpWorkbench = false;
		bool csharpStep = false;
		bool csharpContinue = false;
		bool csharpStoppedUnloaded = false;
		bool blueprintBreakpoint = false;
		bool blueprintFocused = false;
		bool blueprintWorkbench = false;
		bool blueprintStep = false;
		bool blueprintContinue = false;
		bool inspectorUntouched = false;
		bool hiddenSuspended = false;
		bool closeClean = false;
		XWScriptEditor scriptEditor = null;
		XWBPEditor blueprintEditor = null;
		XWModDebugController scriptController = null;
		XWModDebugController blueprintController = null;
		try
		{
			ModEditorManager manager = ModEditorManager.Instance;
			if (!GodotObject.IsInstanceValid(manager))
			{
				manager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(manager))
				{
					AddChild(manager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(manager), "ModEditorManager could not be instantiated.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				return;
			}
			await WaitFrames(2);
			PressF3();
			ModEditorPanel panel = await WaitForPanel(900);
			(XWScriptEditor, XWBPEditor) tuple = await WaitForEditors(900);
			scriptEditor = tuple.Item1;
			blueprintEditor = tuple.Item2;
			realF3 = GodotObject.IsInstanceValid(panel) && GodotObject.IsInstanceValid(scriptEditor) && GodotObject.IsInstanceValid(blueprintEditor) && FindAncestorWindow(scriptEditor) != null && FindAncestorWindow(scriptEditor) == FindAncestorWindow(blueprintEditor);
			Require(realF3, "F3 did not mount the real Script and Blueprint editors in one ModEditor window.");
			if (!realF3)
			{
				return;
			}
			string suffix = Guid.NewGuid().ToString("N");
			_sandboxRoot = ProjectSettings.GlobalizePath("user://UnifiedDebugProbe_" + suffix);
			Directory.CreateDirectory(_sandboxRoot);
			ModProject project = ModProject.Create(_sandboxRoot, "UnifiedDebugMod" + suffix, "1.0.0", "probe", "unified debug probe");
			Require(project != null, "Could not create the temporary Mod project.");
			if (project == null)
			{
				return;
			}
			string text = Path.Combine(project.ProjectPath, "Scripts");
			Directory.CreateDirectory(text);
			string sourcePath = Path.Combine(text, "UnifiedDebugEntry.cs");
			string markerPath = Path.Combine(project.ProjectPath, "unified-debug-marker.txt");
			File.WriteAllText(sourcePath, BuildDebugSource(markerPath));
			XWModManifestSyncService.SyncProject(project.ProjectPath);
			MethodInfo method = typeof(ModEditorPanel).GetMethod("EnterProject", BindingFlags.Instance | BindingFlags.NonPublic);
			ModEditorUnifiedDebugRuntimeProbe modEditorUnifiedDebugRuntimeProbe = this;
			object obj = method?.Invoke(panel, new object[1] { project });
			modEditorUnifiedDebugRuntimeProbe.Require(obj is bool && (bool)obj, "The real ModEditor project-entry workflow rejected the temporary Mod.");
			XWEditorInterface.Instance?.FocusPanel("script_editor");
			await WaitFrames(12);
			XWEditorInterface.Instance?.GetInspector();
			Node inspectorSentinel = new Node
			{
				Name = "UnifiedDebugInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			scriptEditor.OpenFile(sourcePath);
			scriptController = scriptEditor.GetDebugController();
			XWScriptDebugSession.StartResult startResult = await scriptEditor.StartDebugSessionAsync();
			Require(startResult != null && startResult.Success && startResult.EntryPointInvoked, "The real Script editor did not compile, load, and invoke ModEditorDebugEntry.");
			XWModDebugSnapshot firstScriptPause = await WaitForCSharpPause(scriptController, "first", 900);
			ulong pausedAtFrame = Engine.GetProcessFrames();
			await WaitFrames(8);
			csharpPausedWithoutFreeze = firstScriptPause != null && firstScriptPause.PauseReason == XWModDebugPauseReason.ForcedBreakpoint && Engine.GetProcessFrames() >= pausedAtFrame + 8;
			Require(csharpPausedWithoutFreeze, "C# BreakAsync froze SceneTree frames or did not produce a forced asynchronous pause.");
			XWModDebugWorkbench scriptWorkbench = scriptEditor.GetNodeOrNull<XWModDebugWorkbench>("%DebugWorkbench");
			XWCodeEdit scriptCodeEdit = scriptEditor.GetNodeOrNull<XWCodeEdit>("%XWCodeEdit");
			bool flag = await WaitForWorkbenchSnapshot(scriptWorkbench, firstScriptPause.Sequence, 180);
			csharpWorkbench = scriptEditor.IsDebugWorkbenchBound && scriptEditor.IsDebugWorkbenchVisible && flag && scriptEditor.DebugWorkbenchCallStackCount > 0 && scriptEditor.DebugWorkbenchVariableCount > 0 && scriptEditor.GetCurrentFilePath().Equals(sourcePath, StringComparison.OrdinalIgnoreCase) && scriptCodeEdit != null && scriptCodeEdit.GetCaretLine() == firstScriptPause.CurrentCheckpoint.Line - 1;
			GD.Print($"[MOD_EDITOR_UNIFIED_DEBUG_SCRIPT_WORKBENCH] applied={flag} bound={scriptEditor.IsDebugWorkbenchBound} visible={scriptEditor.IsDebugWorkbenchVisible} visibleInTree={scriptWorkbench?.IsVisibleInTree()} snapshotState={scriptWorkbench?.Snapshot?.State} snapshotVariables={scriptWorkbench?.Snapshot?.Variables.Count} stack={scriptEditor.DebugWorkbenchCallStackCount} variables={scriptEditor.DebugWorkbenchVariableCount} file={scriptEditor.GetCurrentFilePath()} caret={scriptCodeEdit?.GetCaretLine()} expectedLine={firstScriptPause.CurrentCheckpoint.Line - 1}");
			Require(csharpWorkbench, "The Script debug workbench did not show variables/call stack or auto-focus the paused source.");
			(scriptWorkbench?.GetNodeOrNull<Button>("%StepIntoButton"))?.EmitSignal(BaseButton.SignalName.Pressed);
			XWModDebugSnapshot secondScriptPause = await WaitForCSharpPause(scriptController, "second", 900);
			bool flag2 = secondScriptPause != null;
			if (flag2)
			{
				flag2 = await WaitForWorkbenchSnapshot(scriptWorkbench, secondScriptPause.Sequence, 180);
			}
			bool flag3 = flag2;
			csharpStep = secondScriptPause != null && secondScriptPause.PauseReason == XWModDebugPauseReason.Step && flag3 && scriptEditor.DebugWorkbenchCallStackCount > 0 && scriptEditor.DebugWorkbenchVariableCount > 0;
			Require(csharpStep, "Script workbench StepInto did not pause at the next C# CheckpointAsync.");
			(scriptWorkbench?.GetNodeOrNull<Button>("%PauseContinueButton"))?.EmitSignal(BaseButton.SignalName.Pressed);
			csharpContinue = await WaitForFileText(markerPath, "completed", 300) && scriptEditor.DebugEntryTaskCompleted;
			Require(csharpContinue, "Script workbench Continue did not let ModEditorDebugEntry complete.");
			bool scriptStopped = await scriptEditor.StopDebugSessionAsync();
			for (int collect = 0; collect < 6; collect++)
			{
				if (!scriptEditor.DebugLoadContextAlive)
				{
					break;
				}
				GC.Collect();
				GC.WaitForPendingFinalizers();
				await WaitFrames(1);
			}
			csharpStoppedUnloaded = scriptStopped && !scriptEditor.HasLiveDebugSession && !scriptEditor.IsDebugSessionRunning && scriptEditor.DebugLoadedAssemblyCount == 0 && !scriptEditor.DebugLoadContextAlive && scriptController.State == XWModDebugState.Stopped;
			Require(csharpStoppedUnloaded, "Stopping the real Script session did not unload the Mod assembly and release the checkpoint waiter.");
			scriptWorkbench?.Hide();
			await WaitFrames(3);
			bool scriptHidden = scriptEditor.IsDebugWorkbenchHiddenProcessSuspended;
			XWEditorInterface.Instance?.FocusPanel("bp_editor");
			XWBPScript xWBPScript = XWBPScript.Create();
			xWBPScript.ResourceName = "UnifiedDebugBlueprint";
			string text2 = Path.Combine(project.ProjectPath, "Resources");
			Directory.CreateDirectory(text2);
			string path = Path.Combine(text2, "UnifiedDebugBlueprint_" + suffix + ".tres");
			Require(ResourceSaver.Save(xWBPScript, path, ResourceSaver.SaverFlags.None) == Error.Ok, "Could not assign a real resource path to the Blueprint debug graph.");
			xWBPScript.TakeOverPath(path);
			GD.Print($"[MOD_EDITOR_UNIFIED_DEBUG_BLUEPRINT_INIT] beforeGraphs={xWBPScript.Graphs.Count} path={xWBPScript.ResourcePath} activeRoot={blueprintEditor.ActiveModProjectRoot}");
			blueprintEditor.Init(xWBPScript);
			await WaitFrames(4);
			XWBPGraphData xWBPGraphData = FirstGraph(blueprintEditor.BpScriptData);
			GD.Print($"[MOD_EDITOR_UNIFIED_DEBUG_BLUEPRINT_INIT] afterDataValid={GodotObject.IsInstanceValid(blueprintEditor.BpScriptData)} afterGraphs={blueprintEditor.BpScriptData?.Graphs.Count ?? (-1)}");
			Require(xWBPGraphData != null, "The real Blueprint editor did not create its default graph.");
			if (xWBPGraphData == null)
			{
				return;
			}
			BuildBlueprintGraph(xWBPGraphData);
			XWBPGraphEdit graphEdit = blueprintEditor.GetGraphEditor();
			graphEdit.Init(null);
			graphEdit.Init(xWBPGraphData);
			await WaitFrames(4);
			XWInspector blueprintInspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			inspectorUntouched = blueprintInspector == null || blueprintInspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "Preparing the real Blueprint debug graph replaced the Inspector sentinel.");
			blueprintController = blueprintEditor.GetDebugController();
			Task<XWBlueprintRuntimeResult> blueprintRun = blueprintEditor.RunSafePreviewAsync();
			XWModDebugSnapshot breakpointPause = await WaitForBlueprintPause(blueprintController, "2", 900);
			await WaitFrames(5);
			blueprintBreakpoint = breakpointPause != null && breakpointPause.PauseReason == XWModDebugPauseReason.ForcedBreakpoint && blueprintEditor.SafePreviewRunning;
			XWBPGraphNode graphNode = graphEdit.GetGraphNode(2);
			TabContainer nodeOrNull = blueprintEditor.GetNodeOrNull<TabContainer>("%BlueprintSidebar");
			blueprintFocused = GodotObject.IsInstanceValid(graphNode) && graphNode.Selected && graphNode.Modulate != Colors.White && nodeOrNull != null && nodeOrNull.CurrentTab == 2;
			blueprintWorkbench = blueprintEditor.IsDebugWorkbenchBound && blueprintEditor.DebugWorkbenchCallStackCount > 0 && blueprintEditor.DebugWorkbenchVariableCount > 0;
			Require(blueprintBreakpoint, "The real XWBPEditor did not pause at its Breakpoint flow node.");
			Require(blueprintFocused, "The Blueprint debugger did not auto-focus/highlight the paused graph node.");
			Require(blueprintWorkbench, "The Blueprint debug workbench did not render variables and call stack.");
			XWModDebugWorkbench blueprintWorkbenchControl = blueprintEditor.GetNodeOrNull<XWModDebugWorkbench>("%DebugWorkbench");
			(blueprintWorkbenchControl?.GetNodeOrNull<Button>("%StepIntoButton"))?.EmitSignal(BaseButton.SignalName.Pressed);
			XWModDebugSnapshot xWModDebugSnapshot = await WaitForBlueprintPause(blueprintController, "3", 900);
			blueprintStep = xWModDebugSnapshot != null && xWModDebugSnapshot.PauseReason == XWModDebugPauseReason.Step;
			Require(blueprintStep, "Blueprint workbench StepInto did not pause on the Print node.");
			(blueprintWorkbenchControl?.GetNodeOrNull<Button>("%PauseContinueButton"))?.EmitSignal(BaseButton.SignalName.Pressed);
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult = await blueprintRun;
			blueprintContinue = xWBlueprintRuntimeResult != null && xWBlueprintRuntimeResult.Success && xWBlueprintRuntimeResult.OutputMessages.SequenceEqual(new string[1] { "unified-debug-output" }) && !blueprintEditor.SafePreviewRunning && blueprintController.State == XWModDebugState.Stopped;
			Require(blueprintContinue, "Blueprint workbench Continue did not finish OnStart -> Breakpoint -> Print.");
			blueprintWorkbenchControl?.Hide();
			await WaitFrames(3);
			bool isDebugWorkbenchHiddenProcessSuspended = blueprintEditor.IsDebugWorkbenchHiddenProcessSuspended;
			hiddenSuspended = scriptHidden && isDebugWorkbenchHiddenProcessSuspended;
			Require(hiddenSuspended, "A hidden Script or Blueprint debug workbench kept processing.");
			inspectorUntouched = inspectorUntouched && (blueprintInspector == null || blueprintInspector.CurrentObject == inspectorSentinel);
			Require(inspectorUntouched, "Debugging changed the raw Inspector object.");
			long scriptSequence = scriptController.Snapshot.Sequence;
			long blueprintSequence = blueprintController.Snapshot.Sequence;
			manager.CloseEditor();
			manager.QueueFree();
			await WaitFrames(8);
			closeClean = !GodotObject.IsInstanceValid(scriptEditor) && !GodotObject.IsInstanceValid(blueprintEditor) && scriptController.State == XWModDebugState.Stopped && blueprintController.State == XWModDebugState.Stopped && scriptController.Snapshot.Sequence == scriptSequence && blueprintController.Snapshot.Sequence == blueprintSequence;
			Require(closeClean, "Closing the real ModEditor left a debug waiter, callback, or active editor behind.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PrintErr("[MOD_EDITOR_UNIFIED_DEBUG_RUNTIME_PROBE_FAILURE] " + ex);
			scriptController?.Stop();
			blueprintController?.Stop();
			if (GodotObject.IsInstanceValid(scriptEditor))
			{
				try
				{
					await scriptEditor.StopDebugSessionAsync(500);
				}
				catch
				{
				}
			}
			blueprintEditor?.CancelSafePreview();
		}
		finally
		{
			GD.Print($"[MOD_EDITOR_UNIFIED_DEBUG_RUNTIME_PROBE] realF3={realF3} csharpPausedWithoutFreeze={csharpPausedWithoutFreeze} csharpWorkbench={csharpWorkbench} csharpStep={csharpStep} csharpContinue={csharpContinue} csharpStoppedUnloaded={csharpStoppedUnloaded} blueprintBreakpoint={blueprintBreakpoint} blueprintFocused={blueprintFocused} blueprintWorkbench={blueprintWorkbench} blueprintStep={blueprintStep} blueprintContinue={blueprintContinue} inspectorUntouched={inspectorUntouched} hiddenSuspended={hiddenSuspended} closeClean={closeClean} failures={_failures.Count}");
			CleanupSandbox();
			GetTree().Quit((_failures.Count != 0) ? 1 : 0);
		}
	}

	private static string BuildDebugSource(string markerPath)
	{
		string text = markerPath.Replace("\"", "\"\"");
		return "using System.Collections.Generic;\nusing System.IO;\nusing System.Threading.Tasks;\nusing PVZHE.ModEditor.Debugging;\nusing PVZHE.ModEditor.ScriptEditor;\npublic static class UnifiedDebugEntry\n{\n    public static async Task ModEditorDebugEntry(XWScriptDebugSession.DebugContext context)\n    {\n        File.WriteAllText(@\"" + text + "\", \"entry\\n\");\n        var first = await context.BreakAsync(new Dictionary<string, object>\n        {\n            [\"checkpoint\"] = \"first\",\n            [\"card_cost\"] = 50\n        });\n        if (first == XWModDebugCheckpointResult.Stopped) return;\n        File.AppendAllText(@\"" + text + "\", \"after-first\\n\");\n        var second = await context.CheckpointAsync(new Dictionary<string, object>\n        {\n            [\"checkpoint\"] = \"second\",\n            [\"wave\"] = 2\n        });\n        if (second == XWModDebugCheckpointResult.Stopped) return;\n        File.AppendAllText(@\"" + text + "\", \"completed\\n\");\n    }\n}\n";
	}

	private static void BuildBlueprintGraph(XWBPGraphData graph)
	{
		graph.Clear();
		Add(graph, new XWBPNodeOnStart().CreateNodeData(), 1, new Vector2(80f, 120f));
		Add(graph, new XWBPNodeBreakpoint().CreateNodeData(), 2, new Vector2(340f, 120f));
		Add(graph, new XWBPNodePrint().CreateNodeData(), 3, new Vector2(600f, 120f));
		XWBPNodeData xWBPNodeData = new XWBPNodeLiteral().CreateNodeData();
		xWBPNodeData.OutputPorts[0].Value = Variant.From<string>("unified-debug-output");
		Add(graph, xWBPNodeData, 4, new Vector2(340f, 340f));
		Connect(graph, 1, 0, 2, 0);
		Connect(graph, 2, 0, 3, 0);
		Connect(graph, 4, 0, 3, 1);
	}

	private static void Add(XWBPGraphData graph, XWBPNodeData node, int id, Vector2 position)
	{
		node.Id = id;
		node.Position = position;
		graph.AddNodePreserveId(node);
	}

	private static void Connect(XWBPGraphData graph, int fromNode, int fromPort, int toNode, int toPort)
	{
		if (!graph.AddConnection(fromNode, fromPort, toNode, toPort))
		{
			throw new InvalidOperationException($"Could not connect {fromNode}:{fromPort} to {toNode}:{toPort}.");
		}
	}

	private static XWBPGraphData FirstGraph(XWBPScriptData data)
	{
		if (data == null)
		{
			return null;
		}
		using (Dictionary<int, XWBPGraphData>.ValueCollection.Enumerator enumerator = data.Graphs.Values.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				return enumerator.Current;
			}
		}
		return null;
	}

	private async Task<XWModDebugSnapshot> WaitForCSharpPause(XWModDebugController controller, string variableValue, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWModDebugSnapshot xWModDebugSnapshot = controller?.Snapshot;
			if (xWModDebugSnapshot != null && xWModDebugSnapshot.State == XWModDebugState.Paused)
			{
				XWModDebugCheckpoint currentCheckpoint = xWModDebugSnapshot.CurrentCheckpoint;
				if (currentCheckpoint != null && currentCheckpoint.Kind == XWModDebugCheckpointKind.CSharpLine && xWModDebugSnapshot.Variables.TryGetValue("checkpoint", out var value) && value.DisplayValue == variableValue)
				{
					return xWModDebugSnapshot;
				}
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<XWModDebugSnapshot> WaitForBlueprintPause(XWModDebugController controller, string nodeId, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWModDebugSnapshot xWModDebugSnapshot = controller?.Snapshot;
			if (xWModDebugSnapshot != null && xWModDebugSnapshot.State == XWModDebugState.Paused)
			{
				XWModDebugCheckpoint currentCheckpoint = xWModDebugSnapshot.CurrentCheckpoint;
				if (currentCheckpoint != null && currentCheckpoint.Kind == XWModDebugCheckpointKind.BlueprintNode && xWModDebugSnapshot.CurrentCheckpoint.BlueprintNodeId == nodeId)
				{
					return xWModDebugSnapshot;
				}
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> WaitForFileText(string path, string text, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (File.Exists(path) && File.ReadAllText(path).Contains(text, StringComparison.Ordinal))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForWorkbenchSnapshot(XWModDebugWorkbench workbench, long sequence, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (GodotObject.IsInstanceValid(workbench) && workbench.LastAppliedSequence >= sequence)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<ModEditorPanel> WaitForPanel(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			ModEditorPanel modEditorPanel = XWEditorInterface.Instance?.GetEditorPanel() as ModEditorPanel;
			if (GodotObject.IsInstanceValid(modEditorPanel) && modEditorPanel.IsInsideTree())
			{
				return modEditorPanel;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<(XWScriptEditor Script, XWBPEditor Blueprint)> WaitForEditors(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWScriptEditor xWScriptEditor = XWEditorInterface.Instance?.GetScriptEditor();
			XWBPEditor xWBPEditor = XWEditorInterface.Instance?.GetBlueprintEditor() as XWBPEditor;
			if (GodotObject.IsInstanceValid(xWScriptEditor) && xWScriptEditor.IsInsideTree() && GodotObject.IsInstanceValid(xWBPEditor) && xWBPEditor.IsInsideTree())
			{
				return (Script: xWScriptEditor, Blueprint: xWBPEditor);
			}
			await WaitFrames(1);
		}
		return (Script: null, Blueprint: null);
	}

	private static Window FindAncestorWindow(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is Window result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private static void PressF3()
	{
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
			GD.PrintErr("[MOD_EDITOR_UNIFIED_DEBUG_RUNTIME_PROBE_FAILURE] " + message);
		}
	}

	private void CleanupSandbox()
	{
		if (string.IsNullOrWhiteSpace(_sandboxRoot) || !Directory.Exists(_sandboxRoot))
		{
			return;
		}
		try
		{
			Directory.Delete(_sandboxRoot, recursive: true);
		}
		catch (IOException)
		{
		}
		catch (UnauthorizedAccessException)
		{
		}
	}
}
