using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.Layout;

public partial class ModEditorBlueprintDirectRuntimeProbe : Node
{
	private const string DraftPath = "user://mod_editor_blueprint_direct_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWBPEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 12;
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
			if (!GodotObject.IsInstanceValid(modEditorManager))
			{
				Finish();
				return;
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
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not initialize the real blueprint editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_editor) != null;
			Require(window, "Blueprint editor is not mounted under the F3 ModEditor window.");
			XWBPScript xWBPScript = XWBPScript.Create();
			xWBPScript.ResourceName = "BlueprintDirectProbe";
			Error error = ResourceSaver.Save(xWBPScript, "user://mod_editor_blueprint_direct_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not create the isolated Blueprint resource: {error}.");
			if (error != Error.Ok)
			{
				Finish();
				return;
			}
			xWBPScript = ResourceLoader.Load<XWBPScript>("user://mod_editor_blueprint_direct_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(xWBPScript), "Isolated Blueprint resource did not reload before editing.");
			if (!GodotObject.IsInstanceValid(xWBPScript))
			{
				Finish();
				return;
			}
			_editor.Init(xWBPScript);
			await WaitFrames(4);
			XWBPVariableData variable = new XWBPVariableData
			{
				Name = "probe_value",
				Type = Variant.Type.String,
				DefaultValue = Variant.From<string>("before")
			};
			_editor.AddVariableWithUndo(variable);
			await WaitFrames(2);
			XWBPGraphData graph = FirstGraph(_editor.BpScriptData);
			XWBPNodeData node = new XWBPNodeBranch().CreateNodeData();
			node.Id = 77;
			node.Position = new Vector2(12f, 18f);
			node.Size = new Vector2(180f, 96f);
			graph?.AddNodePreserveId(node);
			_editor.GetGraphEditor().Init(null);
			_editor.GetGraphEditor().Init(graph);
			await WaitFrames(3);
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node sentinel = new Node
			{
				Name = "BlueprintInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(sentinel);
			XWLayoutManager layout = XWEditorInterface.Instance?.GetLayoutManager();
			layout?.FocusPanel("bp_editor");
			await WaitFrames(2);
			(bool, bool, bool, bool, bool, bool, bool) tuple = await ProbeStateMachineCallbackWorkbench();
			bool callbackPanel = tuple.Item1;
			bool callbackEdit = tuple.Item2;
			bool callbackUndoRedo = tuple.Item3;
			bool callbackSaveReload = tuple.Item4;
			bool callbackGenerated = tuple.Item5;
			bool callbackCallsiteProtected = tuple.Item6;
			bool callbackReachabilitySafe = tuple.Item7;
			(bool, bool, bool) tuple2 = await ProbeVariableType(variable);
			bool visualType = tuple2.Item1;
			bool dynamicType = tuple2.Item2;
			bool variableUndoRedo = tuple2.Item3;
			(bool, bool) tuple3 = await ProbeVariableTextCommit(variable);
			bool focusCommit = tuple3.Item1;
			bool doubleCommit = tuple3.Item2;
			tuple3 = await ProbePortTextCommit(node);
			bool portFocusCommit = tuple3.Item1;
			bool portDoubleCommit = tuple3.Item2;
			bool nodeUndoRedo = await ProbeNodePosition(node);
			var (nodeDragUndoRedo, nodeResizeUndoRedo) = await ProbeGraphNodeTransforms(node);
			await WaitSeconds(0.55);
			bool value = ProbeSavedState(variable.Id, graph?.Id ?? 0, node.Id);
			bool flag2 = HasPopulatedDirectSurface();
			bool flag3 = inspector == null || inspector.CurrentObject == sentinel;
			bool flag4 = IsBlueprintDockCurrent(layout);
			Require(flag2, "BlueprintDirectPropertyHost was not populated with direct controls.");
			Require(flag3, "Selecting blueprint objects replaced the raw Inspector object.");
			Require(flag4, "Blueprint property editing moved focus away from the blueprint dock.");
			GD.Print($"[MOD_EDITOR_BLUEPRINT_DIRECT_PROBE] window={window} directSurface={flag2} inspectorUntouched={flag3} blueprintFocused={flag4} callbackPanel={callbackPanel} callbackEdit={callbackEdit} callbackUndoRedo={callbackUndoRedo} callbackSaveReload={callbackSaveReload} callbackGenerated={callbackGenerated} callbackCallsiteProtected={callbackCallsiteProtected} callbackReachabilitySafe={callbackReachabilitySafe} visualType={visualType} dynamicType={dynamicType} variableUndoRedo={variableUndoRedo} focusCommit={focusCommit} doubleCommit={doubleCommit} portFocusCommit={portFocusCommit} portDoubleCommit={portDoubleCommit} saveReload={value} nodeUndoRedo={nodeUndoRedo} nodeDragUndoRedo={nodeDragUndoRedo} nodeResizeUndoRedo={nodeResizeUndoRedo} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool panel, bool edited, bool history, bool saved, bool generated, bool callsiteProtected, bool reachabilitySafe)> ProbeStateMachineCallbackWorkbench()
	{
		XWBPFunctionData function = XWBPFunctionData.Create();
		function.Name = "每帧动作";
		_editor.AddFunctionWithUndo(function, "创建状态机蓝图动作");
		await WaitFrames(2);
		_editor.EditBlueprintObject(function);
		await WaitFrames(3);
		VBoxContainer vBoxContainer = FindDirectHost();
		Control control = vBoxContainer?.FindChild("BlueprintStateMachineCallbackWorkbench", recursive: true, owned: false) as Control;
		CheckButton checkButton = vBoxContainer?.FindChild("BlueprintStateMachineCallbackEnabled", recursive: true, owned: false) as CheckButton;
		Button instance = vBoxContainer?.FindChild("BlueprintStateMachineCallbackPhaseProcess", recursive: true, owned: false) as Button;
		bool mounted = GodotObject.IsInstanceValid(control) && control.IsVisibleInTree() && GodotObject.IsInstanceValid(checkButton) && GodotObject.IsInstanceValid(instance);
		Require(mounted, "状态机蓝图动作拼图没有挂载到真实函数直接属性面板。");
		if (!mounted)
		{
			return (panel: false, edited: false, history: false, saved: false, generated: false, callsiteProtected: false, reachabilitySafe: false);
		}
		checkButton.EmitSignal(BaseButton.SignalName.Toggled, true);
		await WaitFrames(3);
		if (FindDirectHost()?.FindChild("BlueprintStateMachineCallbackPhaseProcess", recursive: true, owned: false) is Button button)
		{
			button.EmitSignal(BaseButton.SignalName.Pressed);
		}
		await WaitFrames(3);
		LineEdit lineEdit = FindDirectHost()?.FindChild("BlueprintStateMachineCallbackLocalKey", recursive: true, owned: false) as LineEdit;
		if (GodotObject.IsInstanceValid(lineEdit))
		{
			lineEdit.Text = "角色.待机.每帧";
			lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		}
		await WaitFrames(3);
		(FindDirectHost()?.FindChild("BlueprintStateMachineCallbackRepairButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		bool edited = function.StateMachineCallbackEnabled && function.StateMachineCallbackPhase == StateMachineCallbackPhase.Process && function.StateMachineCallbackLocalKey == "角色.待机.每帧" && function.Inputs.Count == 1 && function.Inputs[0].PortTypeValue == XWBPNodePortData.PortType.Float && function.Outputs.Count == 0 && FindDirectHost()?.FindChild("BlueprintStateMachineCallbackSignatureStatus", recursive: true, owned: false) is Label label && label.Text.StartsWith("✓", StringComparison.Ordinal);
		Require(edited, "状态机蓝图动作的启用、阶段、中文键或一键签名修复没有生效。");
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone = undone && function.Inputs.Count == 0;
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone = redone && function.Inputs.Count == 1 && function.Inputs[0].PortTypeValue == XWBPNodePortData.PortType.Float;
		bool history = undone && redone;
		Require(history, "状态机蓝图动作签名修复没有作为单次 Undo/Redo 动作往返。");
		bool flag = _editor.FlushBlueprintPersistence();
		XWBPScriptData xWBPScriptData = ResourceLoader.Load<XWBPScript>("user://mod_editor_blueprint_direct_probe.tres", "", ResourceLoader.CacheMode.Ignore)?.Deserialize();
		XWBPFunctionData savedFunction;
		bool saved = flag && xWBPScriptData != null && xWBPScriptData.Functions.TryGetValue(function.Id, out savedFunction) && savedFunction.StateMachineCallbackEnabled && savedFunction.StateMachineCallbackPhase == StateMachineCallbackPhase.Process && savedFunction.StateMachineCallbackLocalKey == "角色.待机.每帧" && savedFunction.Inputs.Count == 1;
		Require(saved, "状态机蓝图动作绑定没有通过磁盘保存与重新加载。");
		string text;
		try
		{
			text = new XWBPCodeGenerator(_editor.BpScriptData)
			{
				ClassName = "StateMachineBlueprintProbe",
				BlueprintSourcePath = "Scripts/中文状态机动作.tres"
			}.Generate();
		}
		catch (Exception ex)
		{
			text = ex.ToString();
		}
		XWBPFunctionData xWBPFunctionData = function.Duplicate();
		XWBPNodeData xWBPNodeData = new XWBPNodeDelay().CreateNodeData();
		xWBPNodeData.Id = xWBPFunctionData.NextNodeId++;
		xWBPFunctionData.AddNodePreserveId(xWBPNodeData);
		string error;
		bool flag2 = !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(xWBPFunctionData, StateMachineCallbackPhase.Process, out error) && error.Contains("延迟", StringComparison.Ordinal);
		XWBPFunctionData xWBPFunctionData2 = XWBPFunctionData.Create();
		xWBPFunctionData2.Name = "条件动作";
		xWBPFunctionData2.Outputs.Add(new XWBPNodePortData("result", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Bool, null, Variant.From<bool>(false)));
		xWBPFunctionData2.OutputsSet();
		XWBPNodeData xWBPNodeData2 = new XWBPNodeRandomInt().CreateNodeData();
		xWBPNodeData2.Id = xWBPFunctionData2.NextNodeId++;
		xWBPFunctionData2.AddNodePreserveId(xWBPNodeData2);
		string error2;
		bool flag3 = !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(xWBPFunctionData2, StateMachineCallbackPhase.Guard, out error2) && error2.Contains("无副作用", StringComparison.Ordinal);
		bool generated = text.Contains("[StateMachineCallback(\"角色.待机.每帧\", StateMachineCallbackPhase.Process", StringComparison.Ordinal) && text.Contains("SourceKind = StateMachineCallbackSourceKind.Blueprint", StringComparison.Ordinal) && text.Contains("SourcePath = \"Scripts/中文状态机动作.tres\"", StringComparison.Ordinal) && text.Contains("public static void __XWStateMachineCallback_", StringComparison.Ordinal) && text.Contains("in StateMachineCallbackContext context, double delta", StringComparison.Ordinal) && flag2 && flag3;
		Require(generated, "蓝图生成器没有生成带来源信息的同步状态机 Process 包装方法。");
		bool item = await ProbeCallbackSignatureRepairCallsiteProtection();
		bool item2 = ProbeCallbackReachabilitySafety();
		return (panel: mounted, edited: edited, history: history, saved: saved, generated: generated, callsiteProtected: item, reachabilitySafe: item2);
	}

	private async Task<bool> ProbeCallbackSignatureRepairCallsiteProtection()
	{
		XWBPGraphData callerGraph = FirstGraph(_editor.BpScriptData);
		XWBPFunctionData target = XWBPFunctionData.Create();
		target.Name = "被调用的状态动作";
		target.Inputs.Add(new XWBPNodePortData("旧参数", XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Bool, null, Variant.From<bool>(false)));
		target.InputsSet();
		target.StateMachineCallbackPhase = StateMachineCallbackPhase.Enter;
		_editor.BpScriptData.AddFunction(target);
		XWBPNodeCallMethod xWBPNodeCallMethod = new XWBPNodeCallMethod
		{
			MethodType = XWBPNodeCallMethod.Type.Bp,
			FunctionId = target.Id
		};
		xWBPNodeCallMethod.BuildFunction(target);
		XWBPNodeData callNode = xWBPNodeCallMethod.CreateNodeData();
		callerGraph?.AddNode(callNode);
		int callInputCountBefore = callNode.InputPorts.Count;
		int callOutputCountBefore = callNode.OutputPorts.Count;
		int callerConnectionCountBefore = callerGraph?.Connections.Count ?? 0;
		_history.ClearHistory();
		_editor.EditBlueprintObject(target);
		await WaitFrames(3);
		VBoxContainer vBoxContainer = FindDirectHost();
		Label label = vBoxContainer?.FindChild("BlueprintStateMachineCallbackCallsiteStatus", recursive: true, owned: false) as Label;
		Button button = vBoxContainer?.FindChild("BlueprintStateMachineCallbackRepairButton", recursive: true, owned: false) as Button;
		bool warningVisible = GodotObject.IsInstanceValid(label) && label.Text.Contains("1", StringComparison.Ordinal) && GodotObject.IsInstanceValid(button) && button.Text.Contains("1", StringComparison.Ordinal) && !button.Disabled;
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool flag = target.Inputs.Count == 1 && target.Inputs[0].Name == "旧参数" && target.Outputs.Count == 0 && callNode.InputPorts.Count == callInputCountBefore && callNode.OutputPorts.Count == callOutputCountBefore && (callerGraph?.Connections.Count ?? 0) == callerConnectionCountBefore && !_history.HasUndo();
		if (GodotObject.IsInstanceValid(callerGraph) && GodotObject.IsInstanceValid(callNode))
		{
			callerGraph.RemoveNode(callNode.Id);
		}
		_editor.BpScriptData.RemoveFunction(target.Id);
		bool flag2 = warningVisible && flag;
		Require(flag2, "Signature repair mutated a called function or failed to expose the blocking callsite.");
		return flag2;
	}

	private static bool ProbeCallbackReachabilitySafety()
	{
		XWBPScriptData xWBPScriptData = new XWBPScriptData();
		XWBPFunctionData xWBPFunctionData = CreateProcessCallbackFunction("间接延迟入口");
		XWBPFunctionData xWBPFunctionData2 = CreateVoidFunction("包含延迟");
		xWBPScriptData.AddFunction(xWBPFunctionData, 101);
		xWBPScriptData.AddFunction(xWBPFunctionData2, 102);
		AddBlueprintFunctionCall(xWBPFunctionData, xWBPFunctionData2.Id);
		xWBPFunctionData2.AddNode(new XWBPNodeDelay().CreateNodeData());
		xWBPFunctionData.StateMachineCallbackEnabled = true;
		xWBPFunctionData.StateMachineCallbackLocalKey = "safety.indirect_delay";
		xWBPFunctionData.StateMachineCallbackPhase = StateMachineCallbackPhase.Process;
		string error;
		bool flag = !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(xWBPScriptData, xWBPFunctionData, StateMachineCallbackPhase.Process, out error) && error.Contains("[BP_CALLBACK_DELAY]", StringComparison.Ordinal);
		bool flag2 = false;
		try
		{
			new XWBPCodeGenerator(xWBPScriptData).Generate();
		}
		catch (InvalidOperationException ex)
		{
			flag2 = ex.Message.Contains("[BP_CALLBACK_DELAY]", StringComparison.Ordinal);
		}
		bool num = flag && flag2;
		XWBPScriptData xWBPScriptData2 = new XWBPScriptData();
		XWBPFunctionData xWBPFunctionData3 = CreateGuardCallbackFunction("间接副作用入口");
		XWBPFunctionData xWBPFunctionData4 = CreateVoidFunction("写入属性");
		xWBPScriptData2.AddFunction(xWBPFunctionData3, 201);
		xWBPScriptData2.AddFunction(xWBPFunctionData4, 202);
		AddBlueprintFunctionCall(xWBPFunctionData3, xWBPFunctionData4.Id);
		xWBPFunctionData4.AddNode(new XWBPNodeSetProperty().CreateNodeData());
		string error2;
		bool flag3 = !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(xWBPScriptData2, xWBPFunctionData3, StateMachineCallbackPhase.Guard, out error2) && error2.Contains("[BP_CALLBACK_GUARD_SIDE_EFFECT]", StringComparison.Ordinal);
		XWBPScriptData xWBPScriptData3 = new XWBPScriptData();
		XWBPFunctionData xWBPFunctionData5 = CreateProcessCallbackFunction("循环入口");
		XWBPFunctionData xWBPFunctionData6 = CreateVoidFunction("循环子函数");
		xWBPScriptData3.AddFunction(xWBPFunctionData5, 301);
		xWBPScriptData3.AddFunction(xWBPFunctionData6, 302);
		AddBlueprintFunctionCall(xWBPFunctionData5, xWBPFunctionData6.Id);
		AddBlueprintFunctionCall(xWBPFunctionData6, xWBPFunctionData5.Id);
		string error3;
		bool flag4 = !XWBPCodeGenerator.TryValidateStateMachineCallbackSignature(xWBPScriptData3, xWBPFunctionData5, StateMachineCallbackPhase.Process, out error3) && error3.Contains("[BP_CALLBACK_RECURSION]", StringComparison.Ordinal);
		return num && flag3 && flag4;
	}

	private static XWBPFunctionData CreateProcessCallbackFunction(string name)
	{
		XWBPFunctionData xWBPFunctionData = CreateVoidFunction(name);
		xWBPFunctionData.Inputs.Add(new XWBPNodePortData("delta", XWBPNodePortData.Direction.Input, XWBPNodePortData.PortType.Float, null, Variant.From<double>(0.0)));
		xWBPFunctionData.InputsSet();
		return xWBPFunctionData;
	}

	private static XWBPFunctionData CreateGuardCallbackFunction(string name)
	{
		XWBPFunctionData xWBPFunctionData = CreateVoidFunction(name);
		xWBPFunctionData.Outputs.Add(new XWBPNodePortData("result", XWBPNodePortData.Direction.Output, XWBPNodePortData.PortType.Bool, null, Variant.From<bool>(false)));
		xWBPFunctionData.OutputsSet();
		return xWBPFunctionData;
	}

	private static XWBPFunctionData CreateVoidFunction(string name)
	{
		XWBPFunctionData xWBPFunctionData = XWBPFunctionData.Create();
		xWBPFunctionData.Name = name;
		return xWBPFunctionData;
	}

	private static void AddBlueprintFunctionCall(XWBPFunctionData caller, int calledFunctionId)
	{
		XWBPNodeCallMethod xWBPNodeCallMethod = new XWBPNodeCallMethod
		{
			MethodType = XWBPNodeCallMethod.Type.Bp,
			FunctionId = calledFunctionId
		};
		caller.AddNode(xWBPNodeCallMethod.CreateNodeData());
	}

	private async Task<(bool drag, bool resize)> ProbeGraphNodeTransforms(XWBPNodeData node)
	{
		XWBPGraphNode graphNode = _editor.GetGraphEditor().GetGraphNode(node.Id);
		Require(GodotObject.IsInstanceValid(graphNode), "The real blueprint GraphNode was not mounted.");
		if (!GodotObject.IsInstanceValid(graphNode))
		{
			return (drag: false, resize: false);
		}
		_history.ClearHistory();
		Vector2 dragBefore = graphNode.PositionOffset;
		Vector2 dragAfter = (graphNode.PositionOffset = dragBefore + new Vector2(96f, 32f));
		graphNode.EmitSignal(GraphElement.SignalName.Dragged, dragBefore, dragAfter);
		await WaitFrames(2);
		bool drag = node.Position.IsEqualApprox(dragAfter) && _history.HasUndo() && _history.GetCurrentActionName() == "移动蓝图节点" && _history.Undo();
		await WaitFrames(2);
		drag = drag && node.Position.IsEqualApprox(dragBefore) && graphNode.PositionOffset.IsEqualApprox(dragBefore) && _history.Redo();
		await WaitFrames(2);
		drag = drag && node.Position.IsEqualApprox(dragAfter) && graphNode.PositionOffset.IsEqualApprox(dragAfter);
		Require(drag, "GraphNode drag did not round-trip as one shared UndoRedo action.");
		_history.ClearHistory();
		Vector2 dataSizeBefore = node.Size;
		Vector2 viewSizeBefore = graphNode.Size;
		Vector2 sizeAfter = viewSizeBefore + new Vector2(48f, 28f);
		graphNode.EmitSignal(GraphElement.SignalName.ResizeRequest, sizeAfter);
		graphNode.EmitSignal(GraphElement.SignalName.ResizeEnd, sizeAfter);
		await WaitFrames(2);
		bool resize = node.Size.IsEqualApprox(sizeAfter) && _history.HasUndo() && _history.GetCurrentActionName() == "缩放蓝图节点";
		GD.Print($"[MOD_EDITOR_BLUEPRINT_RESIZE_DEBUG] dataBefore={dataSizeBefore} viewBefore={viewSizeBefore} requested={sizeAfter} dataAfter={node.Size} viewAfter={graphNode.Size} hasUndo={_history.HasUndo()} action={_history.GetCurrentActionName()}");
		bool resizeUndoCalled = _history.Undo();
		await WaitFrames(2);
		GD.Print($"[MOD_EDITOR_BLUEPRINT_RESIZE_DEBUG] undoData={node.Size} undoView={graphNode.Size} hasRedo={_history.HasRedo()}");
		bool resizeUndo = resizeUndoCalled && node.Size.IsEqualApprox(dataSizeBefore) && graphNode.Size.IsEqualApprox(viewSizeBefore);
		bool resizeRedoCalled = _history.Redo();
		await WaitFrames(2);
		GD.Print($"[MOD_EDITOR_BLUEPRINT_RESIZE_DEBUG] redoData={node.Size} redoView={graphNode.Size}");
		bool flag = resizeRedoCalled && node.Size.IsEqualApprox(sizeAfter) && graphNode.Size.IsEqualApprox(sizeAfter);
		resize = resize && resizeUndo && flag;
		Require(resize, "GraphNode resize did not round-trip as one shared UndoRedo action.");
		return (drag: drag, resize: resize);
	}

	private async Task<(bool visual, bool dynamic, bool history)> ProbeVariableType(XWBPVariableData variable)
	{
		_history.ClearHistory();
		_editor.EditBlueprintObject(variable);
		await WaitFrames(2);
		VBoxContainer vBoxContainer = FindDirectHost();
		OptionButton optionButton = vBoxContainer?.FindChild("BlueprintVariableTypeSource", recursive: true, owned: false) as OptionButton;
		HFlowContainer hFlowContainer = vBoxContainer?.FindChild("BlueprintVariableTypeChoices", recursive: true, owned: false) as HFlowContainer;
		Require(GodotObject.IsInstanceValid(optionButton), "Variable type picker is missing from the direct property host.");
		Require(GodotObject.IsInstanceValid(hFlowContainer), "Variable type visual choices are missing from the direct property host.");
		if (!GodotObject.IsInstanceValid(optionButton) || !GodotObject.IsInstanceValid(hFlowContainer))
		{
			return (visual: false, dynamic: false, history: false);
		}
		int num = FindItemById(optionButton, 24);
		Button button = hFlowContainer.FindChild($"VisualOption{num}", recursive: false, owned: false) as Button;
		bool visual = num >= 0 && !optionButton.Visible && hFlowContainer.Visible && GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(button.Icon);
		Require(visual, "Variable type is not exposed through visible icon-backed game-style choices.");
		if (!GodotObject.IsInstanceValid(button))
		{
			return (visual: visual, dynamic: false, history: false);
		}
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool applied = variable.Type == Variant.Type.Object && _history.HasUndo();
		bool objectFieldAppeared = FindDirectHost()?.FindChild("BlueprintVariableClass", recursive: true, owned: false) is LineEdit;
		bool undone = _history.Undo();
		await WaitFrames(3);
		bool objectFieldDisappeared = FindDirectHost()?.FindChild("BlueprintVariableClass", recursive: true, owned: false) == null;
		undone = undone && variable.Type == Variant.Type.String && objectFieldDisappeared;
		bool redone = _history.Redo();
		await WaitFrames(3);
		bool flag = FindDirectHost()?.FindChild("BlueprintVariableClass", recursive: true, owned: false) is LineEdit;
		redone = redone && variable.Type == Variant.Type.Object && flag;
		bool flag2 = objectFieldAppeared && objectFieldDisappeared && flag;
		bool flag3 = applied && undone && redone;
		Require(flag2, "Object resource-class field did not appear/disappear after Apply/Undo/Redo.");
		Require(flag3, "Variable type did not round-trip through the shared UndoRedo manager.");
		return (visual: visual, dynamic: flag2, history: flag3);
	}

	private async Task<(bool focus, bool deduplicated)> ProbeVariableTextCommit(XWBPVariableData variable)
	{
		LineEdit lineEdit = FindDirectHost()?.FindChild("BlueprintVariableClass", recursive: true, owned: false) as LineEdit;
		Require(GodotObject.IsInstanceValid(lineEdit), "Object variable resource-class field is unavailable.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return (focus: false, deduplicated: false);
		}
		int beforeFocus = _history.GetVersion();
		lineEdit.Text = "Resource";
		lineEdit.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool focus = variable.ClassName == "Resource" && _history.GetVersion() == beforeFocus + 1;
		Require(focus, "Variable resource class did not commit on FocusExited.");
		LineEdit lineEdit2 = FindDirectHost()?.FindChild("BlueprintVariableDefault", recursive: true, owned: false) as LineEdit;
		Require(GodotObject.IsInstanceValid(lineEdit2), "Variable default field is unavailable after dynamic refresh.");
		if (!GodotObject.IsInstanceValid(lineEdit2))
		{
			return (focus: focus, deduplicated: false);
		}
		int beforeDouble = _history.GetVersion();
		lineEdit2.Text = "persisted_default";
		lineEdit2.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit2.Text);
		lineEdit2.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool flag = variable.DefaultValue.AsString() == "persisted_default" && _history.GetVersion() == beforeDouble + 1;
		Require(flag, "Variable default TextSubmitted/FocusExited path committed more than once.");
		return (focus: focus, deduplicated: flag);
	}

	private async Task<(bool focus, bool deduplicated)> ProbePortTextCommit(XWBPNodeData node)
	{
		_editor.EditBlueprintObject(node);
		await WaitFrames(2);
		LineEdit lineEdit = FindDirectHost()?.FindChild("BlueprintPortName_0_1", recursive: true, owned: false) as LineEdit;
		Require(GodotObject.IsInstanceValid(lineEdit), "Node input port name field is unavailable.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return (focus: false, deduplicated: false);
		}
		int beforeFocus = _history.GetVersion();
		lineEdit.Text = "condition_focus";
		lineEdit.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool focus = node.InputPorts[1].Name == "condition_focus" && _history.GetVersion() == beforeFocus + 1;
		Require(focus, "Node port name did not commit on FocusExited.");
		lineEdit = FindDirectHost()?.FindChild("BlueprintPortName_0_1", recursive: true, owned: false) as LineEdit;
		Require(GodotObject.IsInstanceValid(lineEdit), "Node port name field did not survive direct-property refresh.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return (focus: focus, deduplicated: false);
		}
		int beforeDouble = _history.GetVersion();
		lineEdit.Text = "condition_saved";
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		lineEdit.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool flag = node.InputPorts[1].Name == "condition_saved" && _history.GetVersion() == beforeDouble + 1;
		Require(flag, "Node port TextSubmitted/FocusExited path committed more than once.");
		return (focus: focus, deduplicated: flag);
	}

	private bool ProbeSavedState(int variableId, int graphId, int nodeId)
	{
		XWBPScriptData xWBPScriptData = ResourceLoader.Load<XWBPScript>("user://mod_editor_blueprint_direct_probe.tres", "", ResourceLoader.CacheMode.Ignore)?.Deserialize();
		XWBPVariableData value;
		XWBPGraphData value2;
		XWBPNodeData value3;
		bool flag = xWBPScriptData != null && xWBPScriptData.Variables.TryGetValue(variableId, out value) && value.Type == Variant.Type.Object && value.ClassName == "Resource" && value.DefaultValue.AsString() == "persisted_default" && xWBPScriptData.Graphs.TryGetValue(graphId, out value2) && value2.Nodes.TryGetValue(nodeId, out value3) && value3.InputPorts.Count > 1 && value3.InputPorts[1].Name == "condition_saved";
		Require(flag, "Blueprint direct-property edits did not survive CacheMode.Ignore disk reload.");
		return flag;
	}

	private async Task<bool> ProbeNodePosition(XWBPNodeData node)
	{
		_history.ClearHistory();
		_editor.EditBlueprintObject(node);
		await WaitFrames(2);
		LineEdit lineEdit = FindLabeledLineEdit(FindDirectHost(), "位置");
		Require(GodotObject.IsInstanceValid(lineEdit), "Node position field is missing from the direct property host.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return false;
		}
		Vector2 before = node.Position;
		Vector2 after = new Vector2(64f, 40f);
		lineEdit.Text = "64, 40";
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		await WaitFrames(2);
		bool applied = node.Position.IsEqualApprox(after) && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && node.Position.IsEqualApprox(before);
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && node.Position.IsEqualApprox(after);
		bool flag = applied && undone && redone;
		Require(flag, "Node position did not round-trip through the shared UndoRedo manager.");
		return flag;
	}

	private bool HasPopulatedDirectSurface()
	{
		VBoxContainer vBoxContainer = FindDirectHost();
		Label label = _editor?.FindChild("BlueprintDirectPropertyTitle", recursive: true, owned: false) as Label;
		if (GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() >= 5 && GodotObject.IsInstanceValid(label))
		{
			return label.Text.Contains("节点");
		}
		return false;
	}

	private VBoxContainer FindDirectHost()
	{
		return _editor?.FindChild("BlueprintDirectPropertyHost", recursive: true, owned: false) as VBoxContainer;
	}

	private static LineEdit FindLabeledLineEdit(Node root, string labelText)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", "HBoxContainer", recursive: true, owned: false))
		{
			if (!(item is HBoxContainer hBoxContainer))
			{
				continue;
			}
			bool flag = false;
			LineEdit lineEdit = null;
			foreach (Node child in hBoxContainer.GetChildren())
			{
				if (child is Label label && label.Text == labelText)
				{
					flag = true;
				}
				if (child is LineEdit lineEdit2)
				{
					lineEdit = lineEdit2;
				}
			}
			if (flag && GodotObject.IsInstanceValid(lineEdit))
			{
				return lineEdit;
			}
		}
		return null;
	}

	private static int FindItemById(OptionButton picker, int id)
	{
		for (int i = 0; i < picker.ItemCount; i++)
		{
			if (picker.GetItemId(i) == id)
			{
				return i;
			}
		}
		return -1;
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

	private static bool IsBlueprintDockCurrent(XWLayoutManager layout)
	{
		XWEditorDock xWEditorDock = layout?.GetDock("bp_editor");
		if (!GodotObject.IsInstanceValid(xWEditorDock))
		{
			return false;
		}
		if (xWEditorDock.GetParent() is TabContainer { CurrentTab: >=0 } tabContainer)
		{
			return tabContainer.GetTabControl(tabContainer.CurrentTab) == xWEditorDock;
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor && GodotObject.IsInstanceValid(xWBPEditor) && xWBPEditor.IsInsideTree())
			{
				_editor = xWBPEditor;
				_history = XWEditorInterface.Instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
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

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_DIRECT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_DIRECT_PROBE_FAILURE] " + failure);
		}
		string path = ProjectSettings.GlobalizePath("user://mod_editor_blueprint_direct_probe.tres");
		if (FileAccess.FileExists("user://mod_editor_blueprint_direct_probe.tres"))
		{
			DirAccess.RemoveAbsolute(path);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
