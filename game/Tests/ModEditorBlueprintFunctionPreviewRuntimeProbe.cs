using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Debugging;

public partial class ModEditorBlueprintFunctionPreviewRuntimeProbe : Node
{
	private const string BlueprintPath = "user://BlueprintFunctionPreview/Probe.xwbp";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWBPEditor _editor;

	private XWBPGraphEdit _graphEdit;

	public override async void _Ready()
	{
		bool f3 = false;
		bool normalFunction = false;
		bool nestedCall = false;
		bool returnValue = false;
		bool missingFunction = false;
		bool recursionGate = false;
		bool depthGate = false;
		bool budgetShared = false;
		bool debugTrace = false;
		bool hiddenStopped = false;
		bool saveReload = false;
		try
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager could not be instantiated.");
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
			Require(f3, "F3 did not mount the real Blueprint editor.");
			if (!f3)
			{
				Finish(f3: false, normalFunction: false, nestedCall: false, returnValue: false, missingFunction: false, recursionGate: false, depthGate: false, budgetShared: false, debugTrace: false, hiddenStopped: false, saveReload: false);
				return;
			}
			XWBPScript script = XWBPScript.Create();
			script.ResourceName = "BlueprintFunctionPreviewProbe";
			_editor.Init(script);
			await WaitFrames(4);
			XWBPScriptData data = _editor.BpScriptData;
			XWBPGraphData root = FirstGraph(data);
			Require(root != null, "The probe XWBPScript has no root graph.");
			XWBPFunctionData addOne = BuildAddOneFunction(1, "AddOne");
			XWBPFunctionData xWBPFunctionData = BuildNestedFunction(2, "AddTwo", addOne);
			data.AddFunction(addOne, 1);
			data.AddFunction(xWBPFunctionData, 2);
			BuildRootGraph(root, xWBPFunctionData, 5L);
			_editor.OpenGraph(root);
			await WaitFrames(3);
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult = await _editor.RunSafePreviewAsync();
			normalFunction = xWBlueprintRuntimeResult?.Success ?? false;
			nestedCall = normalFunction && xWBlueprintRuntimeResult.TraceLocations.Any((string location) => location.StartsWith("function:AddTwo#2/", StringComparison.Ordinal)) && xWBlueprintRuntimeResult.TraceLocations.Count((string location) => location.StartsWith("function:AddOne#1/", StringComparison.Ordinal)) >= 2;
			returnValue = normalFunction && xWBlueprintRuntimeResult.OutputMessages.SequenceEqual(new string[1] { "7" });
			Require(normalFunction, "The real F3 preview did not execute a Blueprint function: " + xWBlueprintRuntimeResult?.StopReason);
			Require(nestedCall, "Trace did not cross AddTwo into both AddOne calls.");
			Require(returnValue, "Nested Blueprint return values were not mapped back to the caller output.");
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult2 = await XWBlueprintSafeRuntime.ExecuteGraphAsync(BuildMissingFunctionGraph(), BuildContext(data));
			missingFunction = xWBlueprintRuntimeResult2 != null && !xWBlueprintRuntimeResult2.Success && xWBlueprintRuntimeResult2.StopReason.Contains("CallMethod node 2", StringComparison.Ordinal) && xWBlueprintRuntimeResult2.StopReason.Contains("missing Blueprint function 999", StringComparison.Ordinal);
			Require(missingFunction, "A missing current-script function was not localized to its CallMethod node.");
			XWBPScriptData xWBPScriptData = new XWBPScriptData();
			XWBPFunctionData xWBPFunctionData2 = BuildRecursiveFunction(3, "Recursive");
			xWBPScriptData.AddFunction(xWBPFunctionData2, 3);
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult3 = await XWBlueprintSafeRuntime.ExecuteGraphAsync(BuildVoidRootGraph(xWBPFunctionData2), BuildContext(xWBPScriptData));
			recursionGate = xWBlueprintRuntimeResult3 != null && !xWBlueprintRuntimeResult3.Success && xWBlueprintRuntimeResult3.StopReason.Contains("recursive call cycle", StringComparison.Ordinal) && xWBlueprintRuntimeResult3.StopReason.Contains("Recursive", StringComparison.Ordinal);
			Require(recursionGate, "Direct Blueprint recursion was not rejected by the active-function guard.");
			XWBPFunctionData entry;
			XWBPScriptData data2 = BuildDepthScriptData(out entry);
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult4 = await XWBlueprintSafeRuntime.ExecuteGraphAsync(BuildVoidRootGraph(entry), BuildContext(data2, new XWBlueprintRuntimeLimits
			{
				MaxCallDepth = 2,
				MaxSteps = 64
			}));
			depthGate = xWBlueprintRuntimeResult4 != null && !xWBlueprintRuntimeResult4.Success && xWBlueprintRuntimeResult4.StopReason.Contains("call-depth preview limit", StringComparison.Ordinal) && xWBlueprintRuntimeResult4.StopReason.Contains("DepthC", StringComparison.Ordinal);
			Require(depthGate, "Distinct nested functions did not share the call-depth limit.");
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult5 = await XWBlueprintSafeRuntime.ExecuteGraphAsync(root, BuildContext(data, new XWBlueprintRuntimeLimits
			{
				MaxSteps = 5,
				MaxCallDepth = 16,
				MaxExecutionMilliseconds = 2000
			}));
			budgetShared = xWBlueprintRuntimeResult5 != null && !xWBlueprintRuntimeResult5.Success && xWBlueprintRuntimeResult5.StopReason.Contains("step limit", StringComparison.Ordinal) && xWBlueprintRuntimeResult5.StopReason.Contains("Blueprint function", StringComparison.Ordinal) && xWBlueprintRuntimeResult5.TraceLocations.Any((string location) => location.StartsWith("function:", StringComparison.Ordinal));
			Require(budgetShared, "Nested function calls reset or escaped the caller step budget.");
			InsertBreakpoint(addOne);
			XWBPGraphData graph = BuildRootGraphForFunction(addOne, 5L);
			XWModDebugController controller = new XWModDebugController();
			controller.Start();
			Task<XWBlueprintRuntimeResult> debugRun = XWBlueprintSafeRuntime.ExecuteGraphAsync(graph, BuildContext(data, null, controller));
			XWModDebugSnapshot xWModDebugSnapshot = await WaitForPause(controller, 240);
			bool enteredFunctionBreakpoint = xWModDebugSnapshot?.CurrentCheckpoint?.BlueprintNodeId == "1:4" && xWModDebugSnapshot.CurrentCheckpoint.DisplayName.Contains("AddOne", StringComparison.Ordinal) && xWModDebugSnapshot.CurrentCheckpoint.CallStack.Any((XWModDebugStackFrame frame) => frame.BlueprintNodeId.StartsWith("1:", StringComparison.Ordinal));
			if (xWModDebugSnapshot == null)
			{
				controller.Stop();
			}
			else
			{
				controller.Continue();
			}
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult6 = await debugRun;
			debugTrace = enteredFunctionBreakpoint && xWBlueprintRuntimeResult6.Success && xWBlueprintRuntimeResult6.TraceLocations.Contains("function:AddOne#1/node:4");
			Require(debugTrace, "Debugger checkpoint/trace did not cross into the called function graph.");
			script.Serialize(data);
			string saveDirectory = ProjectSettings.GlobalizePath($"user://BlueprintFunctionPreview/{Guid.NewGuid():N}");
			Directory.CreateDirectory(saveDirectory);
			string path = Path.Combine(saveDirectory, "FunctionPreview.xwbp.tres");
			Error saveError = ResourceSaver.Save(script, path, ResourceSaver.SaverFlags.None);
			XWBPScriptData reloadedData = ResourceLoader.Load<XWBPScript>(path, "", ResourceLoader.CacheMode.Ignore)?.Deserialize();
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult7 = await XWBlueprintSafeRuntime.ExecuteGraphAsync(FirstGraph(reloadedData), BuildContext(reloadedData));
			int num;
			if (saveError == Error.Ok)
			{
				if (reloadedData != null && reloadedData.Functions.Count == 2 && xWBlueprintRuntimeResult7 != null && xWBlueprintRuntimeResult7.Success)
				{
					num = (xWBlueprintRuntimeResult7.OutputMessages.SequenceEqual(new string[1] { "7" }) ? 1 : 0);
					goto IL_0be4;
				}
			}
			num = 0;
			goto IL_0be4;
			IL_0be4:
			saveReload = (byte)num != 0;
			Require(saveReload, "Function preview failed after XWBPScript save/reload: " + xWBlueprintRuntimeResult7?.StopReason);
			BuildInfiniteGraph(root);
			_editor.OpenGraph(root);
			_editor.Show();
			Task<XWBlueprintRuntimeResult> hiddenRun = _editor.RunSafePreviewAsync();
			await WaitFrames(2);
			bool wasRunning = _editor.SafePreviewRunning;
			_editor.Hide();
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult8 = await hiddenRun;
			hiddenStopped = wasRunning && xWBlueprintRuntimeResult8 != null && xWBlueprintRuntimeResult8.WasCancelled && !xWBlueprintRuntimeResult8.Success;
			_editor.Show();
			Require(hiddenStopped, "Hiding the real Blueprint editor did not stop its running preview.");
			try
			{
				if (Directory.Exists(saveDirectory))
				{
					Directory.Delete(saveDirectory, recursive: true);
				}
			}
			catch (IOException)
			{
			}
		}
		catch (Exception ex2)
		{
			_failures.Add(ex2.ToString());
		}
		Finish(f3, normalFunction, nestedCall, returnValue, missingFunction, recursionGate, depthGate, budgetShared, debugTrace, hiddenStopped, saveReload);
	}

	private static XWBlueprintRuntimeContext BuildContext(XWBPScriptData data, XWBlueprintRuntimeLimits limits = null, XWModDebugController debugController = null)
	{
		return new XWBlueprintRuntimeContext
		{
			ScriptData = data,
			BlueprintSourcePath = "user://BlueprintFunctionPreview/Probe.xwbp",
			DebugController = debugController,
			Limits = (limits ?? new XWBlueprintRuntimeLimits
			{
				MaxSteps = 256,
				MaxCallDepth = 16,
				MaxExecutionMilliseconds = 2000
			})
		};
	}

	private static XWBPFunctionData BuildAddOneFunction(int id, string name)
	{
		XWBPFunctionData xWBPFunctionData = CreateFunction(id, name, hasInput: true, hasOutput: true);
		XWBPNodeData node = xWBPFunctionData.GetNode(-100000);
		XWBPNodeData node2 = new XWBPNodeAddInt().CreateNodeData();
		Add(xWBPFunctionData, node2, 2);
		XWBPNodeData xWBPNodeData = new XWBPNodeLiteral().CreateNodeData();
		xWBPNodeData.OutputPorts[0].Value = Variant.From<long>(1L);
		Add(xWBPFunctionData, xWBPNodeData, 3);
		XWBPNodeData node3 = CreateReturnNode(xWBPFunctionData);
		Add(xWBPFunctionData, node3, 5);
		Connect(xWBPFunctionData, node.Id, 0, 5, 0);
		Connect(xWBPFunctionData, node.Id, 1, 2, 0);
		Connect(xWBPFunctionData, 3, 0, 2, 1);
		Connect(xWBPFunctionData, 2, 0, 5, 1);
		return xWBPFunctionData;
	}

	private static void InsertBreakpoint(XWBPFunctionData function)
	{
		function.RemoveConnection(-100000, 0, 5, 0);
		Add(function, new XWBPNodeBreakpoint().CreateNodeData(), 4);
		Connect(function, -100000, 0, 4, 0);
		Connect(function, 4, 0, 5, 0);
	}

	private static XWBPFunctionData BuildNestedFunction(int id, string name, XWBPFunctionData addOne)
	{
		XWBPFunctionData xWBPFunctionData = CreateFunction(id, name, hasInput: true, hasOutput: true);
		XWBPNodeData node = xWBPFunctionData.GetNode(-100000);
		XWBPNodeData node2 = CreateFunctionCall(addOne);
		XWBPNodeData node3 = CreateFunctionCall(addOne);
		XWBPNodeData node4 = CreateReturnNode(xWBPFunctionData);
		Add(xWBPFunctionData, node2, 10);
		Add(xWBPFunctionData, node3, 11);
		Add(xWBPFunctionData, node4, 12);
		Connect(xWBPFunctionData, node.Id, 0, 10, 0);
		Connect(xWBPFunctionData, 10, 0, 11, 0);
		Connect(xWBPFunctionData, 11, 0, 12, 0);
		Connect(xWBPFunctionData, node.Id, 1, 10, 1);
		Connect(xWBPFunctionData, 10, 1, 11, 1);
		Connect(xWBPFunctionData, 11, 1, 12, 1);
		return xWBPFunctionData;
	}

	private static XWBPFunctionData BuildRecursiveFunction(int id, string name)
	{
		XWBPFunctionData xWBPFunctionData = CreateFunction(id, name, hasInput: false, hasOutput: false);
		XWBPNodeData node = CreateFunctionCall(xWBPFunctionData);
		Add(xWBPFunctionData, node, 7);
		Connect(xWBPFunctionData, -100000, 0, 7, 0);
		return xWBPFunctionData;
	}

	private static XWBPScriptData BuildDepthScriptData(out XWBPFunctionData entry)
	{
		XWBPScriptData xWBPScriptData = new XWBPScriptData();
		XWBPFunctionData xWBPFunctionData = CreateFunction(6, "DepthC", hasInput: false, hasOutput: false);
		XWBPFunctionData xWBPFunctionData2 = CreateFunction(5, "DepthB", hasInput: false, hasOutput: false);
		XWBPFunctionData xWBPFunctionData3 = CreateFunction(4, "DepthA", hasInput: false, hasOutput: false);
		XWBPNodeData node = CreateFunctionCall(xWBPFunctionData);
		XWBPNodeData node2 = CreateFunctionCall(xWBPFunctionData2);
		Add(xWBPFunctionData2, node, 8);
		Connect(xWBPFunctionData2, -100000, 0, 8, 0);
		Add(xWBPFunctionData3, node2, 9);
		Connect(xWBPFunctionData3, -100000, 0, 9, 0);
		xWBPScriptData.AddFunction(xWBPFunctionData3, 4);
		xWBPScriptData.AddFunction(xWBPFunctionData2, 5);
		xWBPScriptData.AddFunction(xWBPFunctionData, 6);
		entry = xWBPFunctionData3;
		return xWBPScriptData;
	}

	private static XWBPFunctionData CreateFunction(int id, string name, bool hasInput, bool hasOutput)
	{
		XWBPFunctionData xWBPFunctionData = XWBPFunctionData.Create();
		xWBPFunctionData.Id = id;
		xWBPFunctionData.Name = name;
		if (hasInput)
		{
			xWBPFunctionData.Inputs.Add(new XWBPNodePortData("value", XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Integer, "", Variant.From<long>(0L)));
		}
		if (hasOutput)
		{
			xWBPFunctionData.Outputs.Add(new XWBPNodePortData("result", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Integer, "", Variant.From<long>(0L)));
		}
		XWBPNodeMethodEntry xWBPNodeMethodEntry = new XWBPNodeMethodEntry();
		xWBPNodeMethodEntry.FunctionId = id;
		xWBPNodeMethodEntry.MethodType = XWBPNodeMethodEntry.Type.Bp;
		xWBPNodeMethodEntry.BuildFunction(xWBPFunctionData);
		XWBPNodeData node = xWBPFunctionData.GetNode(-100000);
		xWBPNodeMethodEntry.BuildNodeData(node);
		node.SetMetaData("FunctionId", id);
		node.SetMetaData("MethodType", 0);
		return xWBPFunctionData;
	}

	private static XWBPNodeData CreateFunctionCall(XWBPFunctionData function)
	{
		XWBPNodeCallMethod xWBPNodeCallMethod = new XWBPNodeCallMethod();
		xWBPNodeCallMethod.FunctionId = function.Id;
		xWBPNodeCallMethod.MethodType = XWBPNodeCallMethod.Type.Bp;
		xWBPNodeCallMethod.BuildFunction(function);
		return xWBPNodeCallMethod.CreateNodeData();
	}

	private static XWBPNodeData CreateReturnNode(XWBPFunctionData function)
	{
		XWBPNodeReturn xWBPNodeReturn = new XWBPNodeReturn();
		xWBPNodeReturn.BuildReturnPorts(function);
		return xWBPNodeReturn.CreateNodeData();
	}

	private static void BuildRootGraph(XWBPGraphData graph, XWBPFunctionData function, long input)
	{
		graph.Clear();
		Add(graph, new XWBPNodeOnStart().CreateNodeData(), 1);
		Add(graph, CreateFunctionCall(function), 2);
		Add(graph, new XWBPNodePrint().CreateNodeData(), 3);
		XWBPNodeData xWBPNodeData = new XWBPNodeLiteral().CreateNodeData();
		xWBPNodeData.OutputPorts[0].Value = Variant.From(in input);
		Add(graph, xWBPNodeData, 4);
		Connect(graph, 1, 0, 2, 0);
		Connect(graph, 2, 0, 3, 0);
		Connect(graph, 4, 0, 2, 1);
		Connect(graph, 2, 1, 3, 1);
	}

	private static XWBPGraphData BuildRootGraphForFunction(XWBPFunctionData function, long input)
	{
		XWBPGraphData obj = new XWBPGraphData
		{
			Id = 50,
			Name = "DebugRoot"
		};
		BuildRootGraph(obj, function, input);
		return obj;
	}

	private static XWBPGraphData BuildVoidRootGraph(XWBPFunctionData function)
	{
		XWBPGraphData obj = new XWBPGraphData
		{
			Id = 60,
			Name = "VoidRoot"
		};
		Add(obj, new XWBPNodeOnStart().CreateNodeData(), 1);
		Add(obj, CreateFunctionCall(function), 2);
		Connect(obj, 1, 0, 2, 0);
		return obj;
	}

	private static XWBPGraphData BuildMissingFunctionGraph()
	{
		XWBPFunctionData function = CreateFunction(999, "MissingShape", hasInput: false, hasOutput: false);
		XWBPGraphData obj = new XWBPGraphData
		{
			Id = 70,
			Name = "MissingRoot"
		};
		Add(obj, new XWBPNodeOnStart().CreateNodeData(), 1);
		Add(obj, CreateFunctionCall(function), 2);
		Connect(obj, 1, 0, 2, 0);
		return obj;
	}

	private static void BuildInfiniteGraph(XWBPGraphData graph)
	{
		graph.Clear();
		Add(graph, new XWBPNodeOnStart().CreateNodeData(), 1);
		Add(graph, new XWBPNodeWhileLoop().CreateNodeData(), 2);
		XWBPNodeData xWBPNodeData = new XWBPNodeLiteral().CreateNodeData();
		xWBPNodeData.OutputPorts[0].Value = Variant.From<bool>(true);
		Add(graph, xWBPNodeData, 3);
		Connect(graph, 1, 0, 2, 0);
		Connect(graph, 3, 0, 2, 1);
	}

	private static void Add(XWBPGraphData graph, XWBPNodeData node, int id)
	{
		node.Id = id;
		graph.AddNodePreserveId(node);
	}

	private static void Connect(XWBPGraphData graph, int fromNode, int fromPort, int toNode, int toPort)
	{
		if (!graph.AddConnection(fromNode, fromPort, toNode, toPort))
		{
			throw new InvalidOperationException($"Could not connect {graph.Name} {fromNode}:{fromPort} to {toNode}:{toPort}.");
		}
	}

	private async Task<XWModDebugSnapshot> WaitForPause(XWModDebugController controller, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWModDebugSnapshot snapshot = controller.Snapshot;
			if (snapshot != null && snapshot.State == XWModDebugState.Paused)
			{
				return snapshot;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor && GodotObject.IsInstanceValid(xWBPEditor) && xWBPEditor.IsInsideTree())
			{
				_editor = xWBPEditor;
				_graphEdit = xWBPEditor.GetGraphEditor();
				return GodotObject.IsInstanceValid(_graphEdit) && _graphEdit.IsInsideTree();
			}
			await WaitFrames(1);
		}
		return false;
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

	private void Finish(bool f3, bool normalFunction, bool nestedCall, bool returnValue, bool missingFunction, bool recursionGate, bool depthGate, bool budgetShared, bool debugTrace, bool hiddenStopped, bool saveReload)
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_FUNCTION_PREVIEW_PROBE_FAILURE] " + failure);
		}
		GD.Print($"[MOD_EDITOR_BLUEPRINT_FUNCTION_PREVIEW_PROBE] f3={f3} normalFunction={normalFunction} nestedCall={nestedCall} returnValue={returnValue} missingFunction={missingFunction} recursionGate={recursionGate} depthGate={depthGate} budgetShared={budgetShared} debugTrace={debugTrace} hiddenStopped={hiddenStopped} saveReload={saveReload} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
