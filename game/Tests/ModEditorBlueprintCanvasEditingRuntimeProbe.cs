using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorBlueprintCanvasEditingRuntimeProbe : Node
{
	private const string DraftPath = "user://mod_editor_blueprint_canvas_editing_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWBPEditor _editor;

	private XWBPGraphEdit _graphEdit;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 9;
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
			Require(flag, "F3 did not initialize the real Blueprint editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_editor) != null;
			Require(window, "Blueprint editor is not mounted under the F3 ModEditor window.");
			XWBPScript xWBPScript = XWBPScript.Create();
			xWBPScript.ResourceName = "BlueprintCanvasEditingProbe";
			Error error = ResourceSaver.Save(xWBPScript, "user://mod_editor_blueprint_canvas_editing_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not create the isolated Blueprint resource: {error}.");
			if (error != Error.Ok)
			{
				Finish();
				return;
			}
			xWBPScript = ResourceLoader.Load<XWBPScript>("user://mod_editor_blueprint_canvas_editing_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(xWBPScript), "Isolated Blueprint resource did not reload before editing.");
			if (!GodotObject.IsInstanceValid(xWBPScript))
			{
				Finish();
				return;
			}
			XWBPScript isolationSentinel = XWBPScript.Create();
			isolationSentinel.ResourceName = "BlueprintCanvasIsolationSentinel";
			int sentinelGraphCount = isolationSentinel.Graphs.Count;
			_editor.Init(xWBPScript);
			await WaitFrames(4);
			XWBPGraphData graph = FirstGraph(_editor.BpScriptData);
			Require(graph != null, "Blueprint script did not expose its initial graph.");
			if (graph == null)
			{
				Finish();
				return;
			}
			XWBPNodeData first = new XWBPNodeBranch().CreateNodeData();
			first.Id = 701;
			first.Position = new Vector2(120f, 100f);
			XWBPNodeData second = new XWBPNodeBranch().CreateNodeData();
			second.Id = 702;
			second.Position = new Vector2(360f, 100f);
			graph.AddNodePreserveId(first);
			graph.AddNodePreserveId(second);
			bool condition = graph.AddConnection(first.Id, 0, second.Id, 0);
			Require(condition, "Probe could not create the initial internal Blueprint connection.");
			_graphEdit.Init(null);
			_graphEdit.Init(graph);
			await WaitFrames(4);
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node inspectorSentinel = new Node
			{
				Name = "BlueprintCanvasInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			var (visualMenu, pointerPosition, disabledStates) = await ProbeContextMenu();
			var (signalClipboard, internalConnections, pasteUndoRedo) = await ProbeCopyPaste(graph, first.Id, second.Id);
			var (cutSingleAction, lockedSafe) = await ProbeCut(graph, first, second.Id);
			var (menuAction, duplicateSingleAction) = await ProbeMenuPasteAndDuplicate(graph, first.Id, second.Id);
			await WaitSeconds(0.65);
			bool value = ProbeSavedGraph(graph.Nodes.Count, graph.Connections.Count);
			bool flag2 = inspector == null || inspector.CurrentObject == inspectorSentinel;
			bool flag3 = isolationSentinel.ResourceName == "BlueprintCanvasIsolationSentinel" && isolationSentinel.Graphs.Count == sentinelGraphCount;
			Require(flag2, "Blueprint canvas commands replaced the raw Inspector object.");
			Require(flag3, "Blueprint canvas commands mutated an unrelated Blueprint resource.");
			GD.Print($"[MOD_EDITOR_BLUEPRINT_CANVAS_EDITING_PROBE] window={window} visualMenu={visualMenu} pointerPosition={pointerPosition} disabledStates={disabledStates} signalClipboard={signalClipboard} internalConnections={internalConnections} pasteUndoRedo={pasteUndoRedo} cutSingleAction={cutSingleAction} lockedSafe={lockedSafe} menuAction={menuAction} duplicateSingleAction={duplicateSingleAction} saveReload={value} inspectorUntouched={flag2} resourceIsolated={flag3} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool visualMenu, bool pointerPosition, bool disabledStates)> ProbeContextMenu()
	{
		SelectOnly();
		Vector2 localPosition = new Vector2(214f, 162f);
		Vector2 expectedGraphPosition = ToExpectedGraphPosition(localPosition);
		_graphEdit.EmitSignal(GraphEdit.SignalName.PopupRequest, localPosition);
		await WaitFrames(2);
		PopupMenu canvasContextMenu = _graphEdit.GetCanvasContextMenu();
		long[] obj = new long[6] { 100L, 200L, 201L, 202L, 203L, 204L };
		bool flag = GodotObject.IsInstanceValid(canvasContextMenu) && canvasContextMenu.Visible;
		bool flag2 = flag;
		long[] array = obj;
		foreach (long num in array)
		{
			int num2 = canvasContextMenu?.GetItemIndex((int)num) ?? (-1);
			flag = flag && num2 >= 0;
			flag2 &= num2 >= 0 && GodotObject.IsInstanceValid(canvasContextMenu.GetItemIcon(num2));
		}
		bool flag3 = flag && flag2;
		Require(flag3, "Blueprint canvas context menu is missing visible icon-backed actions.");
		Vector2 to = _graphEdit.GetScreenPosition() + localPosition;
		bool flag4 = canvasContextMenu != null && new Vector2(canvasContextMenu.Position.X, canvasContextMenu.Position.Y).DistanceTo(to) <= 2f && _graphEdit.LastPointerGraphPosition.IsEqualApprox(expectedGraphPosition);
		Require(flag4, "Blueprint canvas menu/paste anchor did not preserve the requested graph position.");
		bool flag5 = !IsMenuDisabled(canvasContextMenu, 100L) && IsMenuDisabled(canvasContextMenu, 200L) && IsMenuDisabled(canvasContextMenu, 201L) && IsMenuDisabled(canvasContextMenu, 202L) && IsMenuDisabled(canvasContextMenu, 203L) && IsMenuDisabled(canvasContextMenu, 204L);
		Require(flag5, "Blueprint canvas menu did not disable unavailable selection/clipboard actions.");
		canvasContextMenu?.Hide();
		return (visualMenu: flag3, pointerPosition: flag4, disabledStates: flag5);
	}

	private async Task<(bool signalClipboard, bool internalConnections, bool pasteUndoRedo)> ProbeCopyPaste(XWBPGraphData graph, int firstId, int secondId)
	{
		SelectOnly(firstId, secondId);
		_history.ClearHistory();
		_graphEdit.EmitSignal(GraphEdit.SignalName.CopyNodesRequest);
		bool copied = _graphEdit.ClipboardNodeCount == 2 && _graphEdit.ClipboardConnectionCount == 1;
		Vector2 vector = new Vector2(486f, 238f);
		Vector2 expectedPastePosition = ToExpectedGraphPosition(vector);
		_graphEdit.EmitSignal(GraphEdit.SignalName.PopupRequest, vector);
		await WaitFrames(1);
		PopupMenu canvasContextMenu = _graphEdit.GetCanvasContextMenu();
		bool enabledAfterCopy = !IsMenuDisabled(canvasContextMenu, 200L) && !IsMenuDisabled(canvasContextMenu, 203L);
		canvasContextMenu?.Hide();
		HashSet<int> beforeIds = CaptureNodeIds(graph);
		int historyVersion = _history.GetVersion();
		_graphEdit.EmitSignal(GraphEdit.SignalName.PasteNodesRequest);
		await WaitFrames(3);
		List<int> pastedIds = FindAddedNodeIds(graph, beforeIds);
		bool flag = graph.Nodes.Count == 4 && graph.Connections.Count == 2 && pastedIds.Count == 2 && _history.GetVersion() == historyVersion + 1 && _history.GetCurrentActionName() == "粘贴节点";
		bool signalClipboard = copied && enabledAfterCopy && flag;
		Require(signalClipboard, "GraphEdit copy/paste requests did not apply as one Blueprint action.");
		bool internalConnections = pastedIds.Count == 2 && graph.Connections.Any((XWBPNodeConnectionData connection) => connection != null && pastedIds.Contains(connection.FromNodeId) && pastedIds.Contains(connection.ToNodeId));
		if (pastedIds.Count == 2)
		{
			internalConnections &= new Vector2(Mathf.Min(graph.GetNode(pastedIds[0]).Position.X, graph.GetNode(pastedIds[1]).Position.X), Mathf.Min(graph.GetNode(pastedIds[0]).Position.Y, graph.GetNode(pastedIds[1]).Position.Y)).IsEqualApprox(expectedPastePosition);
		}
		Require(internalConnections, "Pasted Blueprint nodes lost their internal connection or pointer placement.");
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && graph.Nodes.Count == 2 && graph.Connections.Count == 1;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag2 = redoCalled && graph.Nodes.Count == 4 && graph.Connections.Count == 2;
		bool flag3 = undone && flag2;
		Require(flag3, "Pasted Blueprint subgraph did not round-trip as one Undo/Redo action.");
		return (signalClipboard: signalClipboard, internalConnections: internalConnections, pasteUndoRedo: flag3);
	}

	private async Task<(bool cutSingleAction, bool lockedSafe)> ProbeCut(XWBPGraphData graph, XWBPNodeData lockedNode, int otherOriginalId)
	{
		List<int> second = (from id in graph.Nodes.Keys
			where id != lockedNode.Id && id != otherOriginalId
			orderby id
			select id).ToList();
		lockedNode.Lock = true;
		SelectOnly(new int[1] { lockedNode.Id }.Concat(second).ToArray());
		_history.ClearHistory();
		int versionBefore = _history.GetVersion();
		_graphEdit.EmitSignal(GraphEdit.SignalName.CutNodesRequest);
		await WaitFrames(3);
		bool applied = graph.Nodes.Count == 2 && graph.Connections.Count == 1 && graph.GetNode(lockedNode.Id) == lockedNode && _graphEdit.ClipboardNodeCount == 2 && _graphEdit.ClipboardConnectionCount == 1 && _history.GetVersion() == versionBefore + 1 && _history.GetCurrentActionName() == "剪切节点";
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && graph.Nodes.Count == 4 && graph.Connections.Count == 2;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && graph.Nodes.Count == 2 && graph.Connections.Count == 1;
		bool cutSingleAction = applied && undone && flag;
		Require(cutSingleAction, "Cut did not remove and restore a connected Blueprint subgraph as one action.");
		SelectOnly(lockedNode.Id);
		_history.ClearHistory();
		int lockedVersion = _history.GetVersion();
		_graphEdit.EmitSignal(GraphEdit.SignalName.CutNodesRequest);
		await WaitFrames(2);
		_graphEdit.EmitSignal(GraphEdit.SignalName.PopupRequest, new Vector2(176f, 132f));
		await WaitFrames(1);
		PopupMenu canvasContextMenu = _graphEdit.GetCanvasContextMenu();
		bool flag2 = graph.GetNode(lockedNode.Id) == lockedNode && graph.Nodes.Count == 2 && _history.GetVersion() == lockedVersion && IsMenuDisabled(canvasContextMenu, 200L) && IsMenuDisabled(canvasContextMenu, 201L) && IsMenuDisabled(canvasContextMenu, 202L) && IsMenuDisabled(canvasContextMenu, 204L) && !IsMenuDisabled(canvasContextMenu, 203L);
		canvasContextMenu?.Hide();
		Require(flag2, "Locked Blueprint nodes were cut or exposed destructive context actions.");
		lockedNode.Lock = false;
		return (cutSingleAction: cutSingleAction, lockedSafe: flag2);
	}

	private async Task<(bool menuAction, bool duplicateSingleAction)> ProbeMenuPasteAndDuplicate(XWBPGraphData graph, int firstId, int secondId)
	{
		Vector2 vector = new Vector2(548f, 314f);
		Vector2 expectedPastePosition = ToExpectedGraphPosition(vector);
		_graphEdit.EmitSignal(GraphEdit.SignalName.PopupRequest, vector);
		await WaitFrames(1);
		PopupMenu canvasContextMenu = _graphEdit.GetCanvasContextMenu();
		HashSet<int> beforeMenuPaste = CaptureNodeIds(graph);
		canvasContextMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 203L);
		await WaitFrames(3);
		List<int> list = FindAddedNodeIds(graph, beforeMenuPaste);
		bool menuAction = list.Count == 2 && graph.Nodes.Count == 4 && graph.Connections.Count == 2;
		if (list.Count == 2)
		{
			menuAction &= new Vector2(Mathf.Min(graph.GetNode(list[0]).Position.X, graph.GetNode(list[1]).Position.X), Mathf.Min(graph.GetNode(list[0]).Position.Y, graph.GetNode(list[1]).Position.Y)).IsEqualApprox(expectedPastePosition);
		}
		Require(menuAction, "Visual Blueprint context-menu paste did not use the requested canvas position.");
		SelectOnly(list.ToArray());
		_history.ClearHistory();
		int versionBefore = _history.GetVersion();
		_graphEdit.EmitSignal(GraphEdit.SignalName.DuplicateNodesRequest);
		await WaitFrames(3);
		bool applied = graph.Nodes.Count == 6 && graph.Connections.Count == 3 && _history.GetVersion() == versionBefore + 1 && _history.GetCurrentActionName() == "重复节点";
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && graph.Nodes.Count == 4 && graph.Connections.Count == 2;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && graph.Nodes.Count == 6 && graph.Connections.Count == 3;
		bool flag2 = applied && undone && flag;
		Require(flag2, "Duplicate did not round-trip a connected Blueprint subgraph as one action.");
		return (menuAction: menuAction, duplicateSingleAction: flag2);
	}

	private bool ProbeSavedGraph(int expectedNodes, int expectedConnections)
	{
		XWBPGraphData xWBPGraphData = FirstGraph(ResourceLoader.Load<XWBPScript>("user://mod_editor_blueprint_canvas_editing_probe.tres", "", ResourceLoader.CacheMode.Ignore)?.Deserialize());
		bool flag = xWBPGraphData != null && xWBPGraphData.Nodes.Count == expectedNodes && xWBPGraphData.Connections.Count == expectedConnections;
		Require(flag, "Blueprint canvas edits did not survive CacheMode.Ignore disk reload.");
		return flag;
	}

	private void SelectOnly(params int[] nodeIds)
	{
		HashSet<int> hashSet = new HashSet<int>(nodeIds ?? Array.Empty<int>());
		foreach (KeyValuePair<int, XWBPGraphNode> item in _graphEdit.GraphNodeDictionary)
		{
			if (GodotObject.IsInstanceValid(item.Value))
			{
				item.Value.Selected = hashSet.Contains(item.Key);
			}
		}
		_graphEdit.RefreshNavigationOverview();
	}

	private Vector2 ToExpectedGraphPosition(Vector2 localPosition)
	{
		return _graphEdit.ScrollOffset + localPosition / Mathf.Max(_graphEdit.Zoom, 0.001f);
	}

	private static HashSet<int> CaptureNodeIds(XWBPGraphData graph)
	{
		return new HashSet<int>(graph.Nodes.Keys);
	}

	private static List<int> FindAddedNodeIds(XWBPGraphData graph, HashSet<int> beforeIds)
	{
		return (from id in graph.Nodes.Keys
			where !beforeIds.Contains(id)
			orderby id
			select id).ToList();
	}

	private static bool IsMenuDisabled(PopupMenu menu, long id)
	{
		int num = menu?.GetItemIndex((int)id) ?? (-1);
		if (num >= 0)
		{
			return menu.IsItemDisabled(num);
		}
		return true;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (XWEditorInterface.Instance?.GetBlueprintEditor() is XWBPEditor xWBPEditor && GodotObject.IsInstanceValid(xWBPEditor) && xWBPEditor.IsInsideTree())
			{
				_editor = xWBPEditor;
				_graphEdit = xWBPEditor.GetGraphEditor();
				_history = XWEditorInterface.Instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_graphEdit) && _graphEdit.IsInsideTree() && GodotObject.IsInstanceValid(_history);
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
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_CANVAS_EDITING_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_CANVAS_EDITING_PROBE_FAILURE] " + failure);
		}
		string path = ProjectSettings.GlobalizePath("user://mod_editor_blueprint_canvas_editing_probe.tres");
		if (FileAccess.FileExists("user://mod_editor_blueprint_canvas_editing_probe.tres"))
		{
			DirAccess.RemoveAbsolute(path);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
