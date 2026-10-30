using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Debugging;

public partial class ModEditorBlueprintSafeRuntimeDebugProbe : Node
{
	private const string BlueprintPath = "user://debug/blueprint_runtime_probe.xwbp";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		GD.Print("[MOD_EDITOR_BLUEPRINT_SAFE_RUNTIME_DEBUG_TRACE] ready");
		bool checkpoints = false;
		bool metadata = false;
		bool callDepth = false;
		bool variables = false;
		bool forcedBreakpoint = false;
		bool step = false;
		bool continueRun = false;
		bool pauseBudgetExcluded = false;
		bool stopped = false;
		bool reusable = false;
		bool immutableSnapshot = false;
		try
		{
			XWBPGraphData graph = BuildGraph();
			XWModDebugController controller = new XWModDebugController();
			Require(controller.RequestPause(), "The debugger did not accept an initial pause request.");
			Task<XWBlueprintRuntimeResult> run = XWBlueprintSafeRuntime.ExecuteGraphAsync(graph, BuildContext(controller));
			XWModDebugSnapshot first = await WaitForPause(controller, "1");
			await AbortOnMissingPause(controller, run, first, "1");
			await Task.Delay(450);
			XWModDebugSnapshot second = await StepIntoAndWait(controller, "2");
			await AbortOnMissingPause(controller, run, second, "2");
			await Task.Delay(450);
			XWModDebugSnapshot breakpoint = await StepIntoAndWait(controller, "3");
			await AbortOnMissingPause(controller, run, breakpoint, "3");
			await Task.Delay(450);
			XWModDebugSnapshot fourth = await StepIntoAndWait(controller, "4");
			await AbortOnMissingPause(controller, run, fourth, "4");
			checkpoints = first != null && second != null && breakpoint != null && fourth != null && new XWModDebugSnapshot[4] { first, second, breakpoint, fourth }.Select((XWModDebugSnapshot snapshot) => snapshot.CurrentCheckpoint?.BlueprintNodeId).SequenceEqual(new string[4] { "1", "2", "3", "4" });
			Require(checkpoints, "Flow-node checkpoints did not arrive in exact execution order.");
			XWModDebugCheckpoint xWModDebugCheckpoint = breakpoint?.CurrentCheckpoint;
			metadata = xWModDebugCheckpoint != null && xWModDebugCheckpoint.SourcePath == "user://debug/blueprint_runtime_probe.xwbp" && xWModDebugCheckpoint.BlueprintNodeId == "3" && xWModDebugCheckpoint.BlueprintNodeTypeId == "__XWBPGraphNode_Breakpoint" && !string.IsNullOrWhiteSpace(xWModDebugCheckpoint.DisplayName) && xWModDebugCheckpoint.DisplayName != xWModDebugCheckpoint.BlueprintNodeTypeId;
			callDepth = xWModDebugCheckpoint != null && xWModDebugCheckpoint.FrameDepth == 2 && xWModDebugCheckpoint.CallStack.Select((XWModDebugStackFrame frame) => frame.BlueprintNodeId).SequenceEqual(new string[3] { "1", "2", "3" });
			forcedBreakpoint = breakpoint != null && breakpoint.PauseReason == XWModDebugPauseReason.ForcedBreakpoint;
			int num;
			if (second != null && second.PauseReason == XWModDebugPauseReason.Step)
			{
				num = ((fourth != null && fourth.PauseReason == XWModDebugPauseReason.Step) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			step = (byte)num != 0;
			Require(metadata, "Blueprint checkpoint node id, display name, type, or source path is incomplete.");
			Require(callDepth, "Blueprint checkpoint call depth or stack frames are incorrect.");
			Require(forcedBreakpoint, "A Breakpoint flow node did not force the debugger to pause.");
			Require(step, "StepInto did not pause on the next flow node.");
			IReadOnlyDictionary<string, XWModDebugVariable> readOnlyDictionary = xWModDebugCheckpoint?.Variables;
			XWModDebugVariable value;
			bool flag = readOnlyDictionary != null && readOnlyDictionary.TryGetValue("蓝图变量[17]", out value) && value.DisplayValue == "42";
			XWModDebugVariable value2;
			bool flag2 = readOnlyDictionary != null && readOnlyDictionary.TryGetValue("局部变量.test_local", out value2) && value2.DisplayValue == "9";
			XWModDebugVariable dynamic = readOnlyDictionary?.Values.FirstOrDefault((XWModDebugVariable variable) => variable.Name.StartsWith("动态值[", StringComparison.Ordinal));
			variables = flag && flag2 && dynamic != null && dynamic.Children.Count == 5;
			string frozenDynamicValue = dynamic?.DisplayValue ?? string.Empty;
			Require(variables, "Breakpoint snapshot is missing Blueprint, local, or dynamic port values.");
			Require(controller.Continue(), "The debugger did not continue from the stepped node.");
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult = await run;
			continueRun = xWBlueprintRuntimeResult.Success && xWBlueprintRuntimeResult.OutputMessages.SequenceEqual(new string[1] { "5" });
			pauseBudgetExcluded = continueRun && _stopwatch.ElapsedMilliseconds >= 1350;
			immutableSnapshot = dynamic != null && dynamic.DisplayValue == frozenDynamicValue && dynamic.Children.Count == 5;
			Require(continueRun, "Continue did not complete the safe Blueprint flow.");
			Require(pauseBudgetExcluded, "Time spent paused in the debugger incorrectly consumed the runtime time budget.");
			Require(immutableSnapshot, "A published debug snapshot changed after execution resumed.");
			Require(controller.RequestPause(), "The debugger did not accept a stop-session pause request.");
			Task<XWBlueprintRuntimeResult> stoppedRun = XWBlueprintSafeRuntime.ExecuteGraphAsync(graph, BuildContext(controller));
			XWModDebugSnapshot stopPause = await WaitForPause(controller, "1");
			await AbortOnMissingPause(controller, stoppedRun, stopPause, "1");
			bool stopAccepted = stopPause != null && controller.Stop();
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult2 = await stoppedRun;
			stopped = stopAccepted && xWBlueprintRuntimeResult2.WasCancelled && !xWBlueprintRuntimeResult2.Success && xWBlueprintRuntimeResult2.StopReason.Contains("debugger", StringComparison.OrdinalIgnoreCase);
			reusable = controller.Start() && controller.State == XWModDebugState.Running;
			Require(stopped, "Debugger Stop did not cancel the paused Blueprint runtime.");
			Require(reusable, "A stopped debugger controller could not start a fresh session.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_BLUEPRINT_SAFE_RUNTIME_DEBUG_PROBE] checkpoints={checkpoints} metadata={metadata} callDepth={callDepth} variables={variables} forcedBreakpoint={forcedBreakpoint} step={step} continueRun={continueRun} pauseBudgetExcluded={pauseBudgetExcluded} stopped={stopped} reusable={reusable} immutableSnapshot={immutableSnapshot} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		Finish();
	}

	private static XWBlueprintRuntimeContext BuildContext(XWModDebugController controller)
	{
		return new XWBlueprintRuntimeContext
		{
			DebugController = controller,
			BlueprintSourcePath = "user://debug/blueprint_runtime_probe.xwbp",
			BlueprintVariables = new Dictionary<int, Variant> { [17] = Variant.From<long>(42L) },
			Limits = new XWBlueprintRuntimeLimits
			{
				MaxExecutionMilliseconds = 1000,
				MaxSteps = 64
			}
		};
	}

	private static XWBPGraphData BuildGraph()
	{
		XWBPGraphData obj = new XWBPGraphData
		{
			Name = "SafeRuntimeDebugProbe"
		};
		Add(obj, new XWBPNodeOnStart().CreateNodeData(), 1);
		Add(obj, new XWBPNodeArrayAdd().CreateNodeData(), 2);
		Add(obj, new XWBPNodeBreakpoint().CreateNodeData(), 3);
		Add(obj, new XWBPNodePrint().CreateNodeData(), 4);
		XWBPNodeData xWBPNodeData = new XWBPNodeMakeArray().CreateNodeData();
		xWBPNodeData.InputPorts[0].Value = Variant.From<long>(1L);
		xWBPNodeData.InputPorts[1].Value = Variant.From<long>(2L);
		xWBPNodeData.InputPorts[2].Value = Variant.From<long>(3L);
		xWBPNodeData.InputPorts[3].Value = Variant.From<long>(4L);
		Add(obj, xWBPNodeData, 5);
		XWBPNodeData xWBPNodeData2 = new XWBPNodeLocalVariable().CreateNodeData();
		xWBPNodeData2.SetMetaData("VarName", "test_local");
		xWBPNodeData2.InputPorts[0].Value = Variant.From<long>(9L);
		Add(obj, xWBPNodeData2, 6);
		Add(obj, new XWBPNodeArrayLength().CreateNodeData(), 7);
		Connect(obj, 1, 0, 2, 0);
		Connect(obj, 2, 0, 3, 0);
		Connect(obj, 3, 0, 4, 0);
		Connect(obj, 5, 0, 2, 1);
		Connect(obj, 6, 0, 2, 2);
		Connect(obj, 5, 0, 7, 0);
		Connect(obj, 7, 0, 4, 1);
		return obj;
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
			throw new InvalidOperationException($"Could not connect {fromNode}:{fromPort} to {toNode}:{toPort}.");
		}
	}

	private static async Task<XWModDebugSnapshot> StepIntoAndWait(XWModDebugController controller, string expectedNodeId)
	{
		if (!controller.StepInto())
		{
			return null;
		}
		return await WaitForPause(controller, expectedNodeId);
	}

	private static async Task<XWModDebugSnapshot> WaitForPause(XWModDebugController controller, string expectedNodeId)
	{
		for (int attempt = 0; attempt < 50; attempt++)
		{
			XWModDebugSnapshot snapshot = controller.Snapshot;
			if (snapshot != null && snapshot.State == XWModDebugState.Paused && snapshot.CurrentCheckpoint?.BlueprintNodeId == expectedNodeId)
			{
				GD.Print($"[MOD_EDITOR_BLUEPRINT_SAFE_RUNTIME_DEBUG_TRACE] paused={expectedNodeId} reason={snapshot.PauseReason}");
				return snapshot;
			}
			await Task.Delay(1);
		}
		return null;
	}

	private static async Task AbortOnMissingPause(XWModDebugController controller, Task<XWBlueprintRuntimeResult> run, XWModDebugSnapshot snapshot, string expectedNodeId)
	{
		if (snapshot != null)
		{
			return;
		}
		XWModDebugSnapshot actual = controller.Snapshot;
		string runDetails = run.Status.ToString();
		controller.Stop();
		try
		{
			XWBlueprintRuntimeResult xWBlueprintRuntimeResult = await run.WaitAsync(TimeSpan.FromSeconds(2L));
			runDetails += $", success={xWBlueprintRuntimeResult.Success}, stop={xWBlueprintRuntimeResult.StopReason}";
		}
		catch (Exception ex)
		{
			runDetails = runDetails + ", error=" + ex.GetType().Name + ": " + ex.Message;
		}
		throw new InvalidOperationException($"Expected pause on node {expectedNodeId}, but debugger state was {actual?.State} at node {actual?.CurrentCheckpoint?.BlueprintNodeId ?? "<none>"}; runtime={runDetails}.");
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_SAFE_RUNTIME_DEBUG_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_SAFE_RUNTIME_DEBUG_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
