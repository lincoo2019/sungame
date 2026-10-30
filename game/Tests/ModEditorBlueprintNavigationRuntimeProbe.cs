using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Blueprint;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorBlueprintNavigationRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWBPEditor _editor;

	private XWBPGraphEdit _graphEdit;

	public override async void _Ready()
	{
		_ = 4;
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
			xWBPScript.ResourceName = "BlueprintNavigationProbe";
			_editor.Init(xWBPScript);
			await WaitFrames(4);
			XWBPGraphData xWBPGraphData = FirstGraph(_editor.BpScriptData);
			Require(xWBPGraphData != null, "Blueprint script did not create a graph for the navigation probe.");
			if (xWBPGraphData == null)
			{
				Finish();
				return;
			}
			XWBPNodeData first = new XWBPNodeBranch().CreateNodeData();
			first.Id = 501;
			first.Position = new Vector2(120f, 100f);
			XWBPNodeData second = new XWBPNodeBranch().CreateNodeData();
			second.Id = 502;
			second.Position = new Vector2(680f, 360f);
			xWBPGraphData.AddNodePreserveId(first);
			xWBPGraphData.AddNodePreserveId(second);
			bool condition = xWBPGraphData.AddConnection(first.Id, 0, second.Id, 0);
			Require(condition, "Probe graph could not create a valid flow connection.");
			_graphEdit.Init(null);
			_graphEdit.Init(xWBPGraphData);
			await WaitFrames(4);
			HBoxContainer hBoxContainer = _graphEdit.FindChild("BlueprintNavigationTools", recursive: true, owned: false) as HBoxContainer;
			Button button = _graphEdit.FindChild("MinimapToggleButton", recursive: true, owned: false) as Button;
			Button button2 = _graphEdit.FindChild("ResetZoomButton", recursive: true, owned: false) as Button;
			Button button3 = _graphEdit.FindChild("FocusSelectionButton", recursive: true, owned: false) as Button;
			Label nodeCount = _graphEdit.FindChild("NodeCountLabel", recursive: true, owned: false) as Label;
			Label connectionCount = _graphEdit.FindChild("ConnectionCountLabel", recursive: true, owned: false) as Label;
			TextureRect textureRect = _graphEdit.FindChild("NodeCountIcon", recursive: true, owned: false) as TextureRect;
			TextureRect textureRect2 = _graphEdit.FindChild("ConnectionCountIcon", recursive: true, owned: false) as TextureRect;
			bool directToolbar = GodotObject.IsInstanceValid(hBoxContainer) && GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(button3) && IsDescendantOf(hBoxContainer, _graphEdit);
			bool icons = GodotObject.IsInstanceValid(button?.Icon) && GodotObject.IsInstanceValid(button2?.Icon) && GodotObject.IsInstanceValid(button3?.Icon) && GodotObject.IsInstanceValid(textureRect?.Texture) && GodotObject.IsInstanceValid(textureRect2?.Texture);
			Require(directToolbar, "Blueprint navigation tools are not mounted in the main GraphEdit surface.");
			Require(icons, "Blueprint navigation tools did not load their visual icons.");
			bool minimapToggle = ProbeMinimap(button);
			bool zoomReset = ProbeZoomReset(button2);
			bool focusSelected = ProbeFocusSelection(second.Id, button3);
			bool overviewLive = ProbeOverview(nodeCount, connectionCount, first.Id, second.Id);
			XWInspector inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
			Node sentinel = new Node
			{
				Name = "BlueprintNavigationInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(sentinel);
			(XWEditorInterface.Instance?.GetLayoutManager())?.FocusPanel("bp_editor");
			_graphEdit.FocusSelectedNode();
			await WaitFrames(2);
			bool flag2 = inspector == null || inspector.CurrentObject == sentinel;
			Require(flag2, "Blueprint navigation replaced the raw Inspector object.");
			GD.Print($"[MOD_EDITOR_BLUEPRINT_NAVIGATION_PROBE] window={window} directToolbar={directToolbar} icons={icons} minimapToggle={minimapToggle} zoomReset={zoomReset} focusSelection={focusSelected} overviewLive={overviewLive} inspectorUntouched={flag2} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private bool ProbeMinimap(Button minimap)
	{
		if (!GodotObject.IsInstanceValid(minimap))
		{
			return false;
		}
		minimap.ButtonPressed = false;
		minimap.EmitSignal(BaseButton.SignalName.Toggled, false);
		bool flag = !_graphEdit.MinimapEnabled;
		minimap.ButtonPressed = true;
		minimap.EmitSignal(BaseButton.SignalName.Toggled, true);
		bool minimapEnabled = _graphEdit.MinimapEnabled;
		bool flag2 = flag && minimapEnabled;
		Require(flag2, "Visual minimap toggle did not change GraphEdit.MinimapEnabled.");
		return flag2;
	}

	private bool ProbeZoomReset(Button resetZoom)
	{
		if (!GodotObject.IsInstanceValid(resetZoom))
		{
			return false;
		}
		_graphEdit.Zoom = 1.44f;
		resetZoom.EmitSignal(BaseButton.SignalName.Pressed);
		bool flag = Mathf.IsEqualApprox(_graphEdit.Zoom, 1f);
		Require(flag, "Visual reset button did not restore 100% graph zoom.");
		return flag;
	}

	private bool ProbeFocusSelection(int nodeId, Button focusSelection)
	{
		XWBPGraphNode graphNode = _graphEdit.GetGraphNode(nodeId);
		if (!GodotObject.IsInstanceValid(graphNode) || !GodotObject.IsInstanceValid(focusSelection))
		{
			return false;
		}
		graphNode.Selected = true;
		_graphEdit.RefreshNavigationOverview();
		Vector2 other = graphNode.PositionOffset - _graphEdit.Size * 0.5f + graphNode.Size * 0.5f;
		_graphEdit.ScrollOffset = Vector2.Zero;
		focusSelection.EmitSignal(BaseButton.SignalName.Pressed);
		bool flag = !focusSelection.Disabled && _graphEdit.ScrollOffset.IsEqualApprox(other);
		Require(flag, "Visual focus-selection button did not center the selected blueprint node.");
		return flag;
	}

	private bool ProbeOverview(Label nodeCount, Label connectionCount, int fromNodeId, int toNodeId)
	{
		if (!GodotObject.IsInstanceValid(nodeCount) || !GodotObject.IsInstanceValid(connectionCount))
		{
			return false;
		}
		bool num = nodeCount.Text == "2" && connectionCount.Text == "1";
		_graphEdit.UndoRemoveConnection(fromNodeId, 0, toNodeId, 0);
		bool flag = connectionCount.Text == "0";
		_graphEdit.DoAddConnection(fromNodeId, 0, toNodeId, 0);
		bool flag2 = connectionCount.Text == "1";
		bool flag3 = num && flag && flag2;
		Require(flag3, "Node/connection overview did not refresh immediately after graph mutations.");
		return flag3;
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

	private static bool IsDescendantOf(Node node, Node expectedAncestor)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 == expectedAncestor)
			{
				return true;
			}
			node2 = node2.GetParent();
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

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_NAVIGATION_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BLUEPRINT_NAVIGATION_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
