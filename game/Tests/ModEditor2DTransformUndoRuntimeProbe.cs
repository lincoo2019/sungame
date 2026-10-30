using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DTransformUndoRuntimeProbe : Node
{
	private const string FixtureScenePath = "user://mod_editor_2d_direct_node_property_probe.tscn";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		_ = 15;
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
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			bool window = FindAncestorWindow(editor) != null;
			Require(window, "2D scene editor is not mounted under the F3 ModEditor window.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			Node2D node2D = new Node2D
			{
				Name = "TransformUndoProbeRoot"
			};
			ColorRect colorRect = new ColorRect
			{
				Name = "TransformUndoProbeNode",
				Position = new Vector2(-24f, -24f),
				Size = new Vector2(48f, 48f),
				Color = new Color(0.3f, 0.8f, 0.35f)
			};
			node2D.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
			colorRect.Owner = node2D;
			PackedScene packedScene = new PackedScene
			{
				ResourceName = "TransformUndoProbe"
			};
			Require(packedScene.Pack(node2D) == Error.Ok, "Could not pack the in-memory 2D probe scene.");
			Require(ResourceSaver.Save(packedScene, "user://mod_editor_2d_direct_node_property_probe.tscn", ResourceSaver.SaverFlags.None) == Error.Ok, "Could not save the isolated 2D direct-node fixture.");
			node2D.Free();
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_direct_node_property_probe.tscn");
			await WaitFrames(8);
			XW2DViewport viewport = editor.FindChild("Viewport2D", recursive: true, owned: false) as XW2DViewport;
			Control editedNode = editor.CurrentSceneInstance?.FindChild("TransformUndoProbeNode", recursive: true, owned: false) as Control;
			Require(GodotObject.IsInstanceValid(viewport), "The real 2D viewport was not created.");
			Require(GodotObject.IsInstanceValid(editedNode), "The editable probe node was not instantiated.");
			if (!GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(editedNode))
			{
				Finish();
				return;
			}
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node inspectorSentinel = new Node
			{
				Name = "DirectNodePropertyInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			GodotObject inspectedBeforeCanvasSelection = inspector?.CurrentObject;
			XWSceneTreeDock sceneTreeDock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWSceneNodeTree sceneNodeTree = FindNodeOfType<XWSceneNodeTree>(sceneTreeDock);
			int sceneTreeSelectionSignals = 0;
			if (GodotObject.IsInstanceValid(sceneNodeTree))
			{
				sceneNodeTree.NodeSelected += delegate
				{
					sceneTreeSelectionSignals++;
				};
			}
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			viewport.CenterAt(Vector2.Zero);
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			Require(GodotObject.IsInstanceValid(history), "The global ModEditor undo manager is missing.");
			history?.ClearHistory();
			Vector2 before = editedNode.Position;
			Vector2 vector = viewport.Size * 0.5f;
			Vector2 delta = new Vector2(40f, 24f);
			EmitCanvasInput(viewport, new InputEventMouseButton
			{
				Position = vector,
				ButtonIndex = MouseButton.Left,
				ButtonMask = MouseButtonMask.Left,
				Pressed = true
			});
			EmitCanvasInput(viewport, new InputEventMouseMotion
			{
				Position = vector + delta,
				Relative = delta,
				ButtonMask = MouseButtonMask.Left
			});
			EmitCanvasInput(viewport, new InputEventMouseButton
			{
				Position = vector + delta,
				ButtonIndex = MouseButton.Left,
				ButtonMask = (MouseButtonMask)0L,
				Pressed = false
			});
			await WaitFrames(3);
			Vector2 after = editedNode.Position;
			bool dragApplied = after.IsEqualApprox(before + delta);
			bool singleHistory = history != null && history.HasUndo() && history.GetCurrentActionName() == "移动 2D 节点";
			GD.Print($"[MOD_EDITOR_2D_TRANSFORM_UNDO_DEBUG] type={history?.GetCurrentHistoryType()} hasUndo={history?.HasUndo()} action={history?.GetCurrentActionName()} before={before} after={after}");
			Require(dragApplied, $"Canvas drag did not apply the expected transform: {before} -> {after}.");
			Require(singleHistory, "Canvas drag did not create the expected single scene-history action.");
			bool undoCalled = history?.Undo() ?? false;
			await WaitFrames(2);
			bool undo = undoCalled && editedNode.Position.IsEqualApprox(before);
			GD.Print($"[MOD_EDITOR_2D_TRANSFORM_UNDO_DEBUG] undoCalled={undoCalled} undoPosition={editedNode.Position} hasRedo={history?.HasRedo()}");
			bool redoCalled = history?.Redo() ?? false;
			await WaitFrames(2);
			bool redo = redoCalled && editedNode.Position.IsEqualApprox(after);
			Require(undo, "Undo did not restore the pre-drag 2D position.");
			Require(redo, "Redo did not restore the completed 2D drag position.");
			PanelContainer panelContainer = editor.FindChild("TransformDirectPanel", recursive: true, owned: false) as PanelContainer;
			HBoxContainer hBoxContainer = editor.FindChild("ControlFields", recursive: true, owned: false) as HBoxContainer;
			SpinBox spinBox = editor.FindChild("PositionXSpin", recursive: true, owned: false) as SpinBox;
			SpinBox spinBox2 = editor.FindChild("SizeXSpin", recursive: true, owned: false) as SpinBox;
			SpinBox spinBox3 = editor.FindChild("PivotXSpin", recursive: true, owned: false) as SpinBox;
			bool directSurface = GodotObject.IsInstanceValid(panelContainer) && panelContainer.Visible && GodotObject.IsInstanceValid(hBoxContainer) && hBoxContainer.Visible && GodotObject.IsInstanceValid(spinBox) && GodotObject.IsInstanceValid(spinBox2) && GodotObject.IsInstanceValid(spinBox3) && Mathf.IsEqualApprox((float)spinBox.Value, after.X) && Mathf.IsEqualApprox((float)spinBox2.Value, editedNode.Size.X) && Mathf.IsEqualApprox((float)spinBox3.Value, editedNode.PivotOffset.X);
			Require(directSurface, "The selected Control was not represented by the embedded direct transform surface.");
			bool inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectedBeforeCanvasSelection && inspector.CurrentObject != editedNode;
			bool treeSynced = GodotObject.IsInstanceValid(sceneTreeDock) && sceneTreeDock.GetSelectedNode() == editedNode;
			bool silentTree = GodotObject.IsInstanceValid(sceneNodeTree) && sceneTreeSelectionSignals == 0;
			GD.Print($"[MOD_EDITOR_2D_INSPECTOR_DEBUG] inspector={GodotObject.IsInstanceValid(inspector)} before={DescribeObject(inspectedBeforeCanvasSelection)} after={DescribeObject(inspector?.CurrentObject)} edited={DescribeObject(editedNode)}");
			Require(inspectorUntouched, "Canvas selection replaced the raw Inspector target instead of staying in the 2D surface.");
			Require(treeSynced, "Canvas selection did not synchronize the scene-tree highlight.");
			Require(silentTree, "Silent scene-tree synchronization emitted a selection signal and can still redirect the Inspector.");
			history?.ClearHistory();
			float directBefore = editedNode.Position.X;
			LineEdit lineEdit = spinBox?.GetLineEdit();
			if (GodotObject.IsInstanceValid(lineEdit) && GodotObject.IsInstanceValid(spinBox))
			{
				lineEdit.EmitSignal(Control.SignalName.FocusEntered);
				spinBox.SetValueNoSignal(directBefore + 12f);
				spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, (double)directBefore + 12.0);
				spinBox.SetValueNoSignal(directBefore + 36f);
				spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, (double)directBefore + 36.0);
				lineEdit.EmitSignal(Control.SignalName.FocusExited);
			}
			await WaitFrames(4);
			float directAfter = editedNode.Position.X;
			bool directApplied = Mathf.IsEqualApprox(directAfter, directBefore + 36f);
			bool directSingleHistory = history != null && history.HasUndo() && history.GetCurrentActionName() == "直接编辑 2D 变换";
			bool directUndoCalled = history?.Undo() ?? false;
			await WaitFrames(2);
			bool directUndo = directUndoCalled && Mathf.IsEqualApprox(editedNode.Position.X, directBefore);
			bool directRedoCalled = history?.Redo() ?? false;
			await WaitFrames(2);
			bool flag = directRedoCalled && Mathf.IsEqualApprox(editedNode.Position.X, directAfter);
			bool directUndoRedo = directApplied && directSingleHistory && directUndo && flag;
			Require(directUndoRedo, "Continuous direct transform input did not merge into one Scene undo/redo action.");
			Button allPropertiesButton = editor.FindChild("AllPropertiesButton", recursive: true, owned: false) as Button;
			TabContainer workspaceTabs = editor.FindChild("WorkspaceTabs", recursive: true, owned: false) as TabContainer;
			XWDirectPropertySurface nodePropertySurface = editor.FindChild("NodeDirectPropertySurface", recursive: true, owned: false) as XWDirectPropertySurface;
			XWEditorInterface.Instance?.FocusPanel("2d_editor");
			await WaitFrames(4);
			allPropertiesButton?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = nodePropertySurface?.FindChild("Direct_visible", recursive: true, owned: false) as XWInspectorPropertyEditorBase;
			bool nodePropertySurfaceReady = GodotObject.IsInstanceValid(allPropertiesButton) && !allPropertiesButton.Disabled && GodotObject.IsInstanceValid(workspaceTabs) && workspaceTabs.CurrentTab == 1 && GodotObject.IsInstanceValid(nodePropertySurface) && nodePropertySurface.IsVisibleInTree() && nodePropertySurface.EditablePropertyCount > 0 && nodePropertySurface.MissingPropertyCount == 0 && nodePropertySurface.MountedPropertyCount == nodePropertySurface.EditablePropertyCount && nodePropertySurface.SpecializedPropertyCount > 0 && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase);
			GD.Print($"[MOD_EDITOR_2D_NODE_PROPERTY_SURFACE_DEBUG] button={GodotObject.IsInstanceValid(allPropertiesButton)} disabled={allPropertiesButton?.Disabled} tabs={GodotObject.IsInstanceValid(workspaceTabs)} tab={workspaceTabs?.CurrentTab} editorVisible={editor.IsVisibleInTree()} surface={GodotObject.IsInstanceValid(nodePropertySurface)} localVisible={nodePropertySurface?.Visible} visible={nodePropertySurface?.IsVisibleInTree()} editable={nodePropertySurface?.EditablePropertyCount} mounted={nodePropertySurface?.MountedPropertyCount} specialized={nodePropertySurface?.SpecializedPropertyCount} missing={nodePropertySurface?.MissingPropertyCount} visibleEditor={GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase)}");
			Require(nodePropertySurfaceReady, $"Node property cards are incomplete: editable={nodePropertySurface?.EditablePropertyCount}; mounted={nodePropertySurface?.MountedPropertyCount}; specialized={nodePropertySurface?.SpecializedPropertyCount}; missing={nodePropertySurface?.MissingPropertyCount}.");
			history?.ClearHistory(1);
			history?.SetCurrentHistoryType(1);
			xWInspectorPropertyEditorBase?.ValueChange(Variant.From<bool>(false));
			await WaitFrames(3);
			bool nodePropertyApplied = !editedNode.Visible && history?.GetCurrentHistoryType() == editor.GetCurrentSceneHistoryId() && history.HasUndo() && history.GetCurrentActionName().Contains("visible", StringComparison.OrdinalIgnoreCase);
			bool nodePropertyUndoCalled = history?.Undo() ?? false;
			await WaitFrames(2);
			bool nodePropertyUndo = nodePropertyUndoCalled && editedNode.Visible;
			bool nodePropertyRedoCalled = history?.Redo() ?? false;
			await WaitFrames(2);
			bool flag2 = nodePropertyRedoCalled && !editedNode.Visible;
			bool nodePropertyUndoRedo = nodePropertyApplied && nodePropertyUndo && flag2;
			Require(nodePropertyUndoRedo, "A non-transform node property did not use the shared Scene undo/redo history.");
			bool sceneSaved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node obj = ResourceLoader.Load<PackedScene>("user://mod_editor_2d_direct_node_property_probe.tscn", "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
			CanvasItem canvasItem = obj?.FindChild("TransformUndoProbeNode", recursive: true, owned: false) as CanvasItem;
			bool flag3 = sceneSaved && GodotObject.IsInstanceValid(canvasItem) && !canvasItem.Visible;
			GD.Print($"[MOD_EDITOR_2D_NODE_PROPERTY_SAVE_DEBUG] saved={sceneSaved} path={editor.CurrentPackedScene?.ResourcePath} reloaded={GodotObject.IsInstanceValid(canvasItem)} visible={!GodotObject.IsInstanceValid(canvasItem) || canvasItem.Visible}");
			obj?.Free();
			Require(flag3, "The non-transform node property did not persist through scene save/reload.");
			bool flag4 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorSentinel && inspector.CurrentObject != editedNode;
			Require(flag4, "Opening or editing the 2D node property cards changed the raw Inspector target.");
			GD.Print($"[MOD_EDITOR_2D_TRANSFORM_UNDO_PROBE] window={window} viewport=True drag={dragApplied} singleHistory={singleHistory} undo={undo} redo={redo} directSurface={directSurface} inspectorUntouched={inspectorUntouched} treeSynced={treeSynced} silentTree={silentTree} directUndoRedo={directUndoRedo} nodePropertySurface={nodePropertySurfaceReady} nodePropertyUndoRedo={nodePropertyUndoRedo} nodePropertySavedReloaded={flag3} nodePropertyInspectorUntouched={flag4} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static void EmitCanvasInput(XW2DViewport viewport, InputEvent inputEvent)
	{
		viewport.EmitSignal(Control.SignalName.GuiInput, inputEvent);
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await WaitFrames(1);
		}
		Require(condition: false, $"F3 did not initialize the 2D scene editor within {maxFrames} frames.");
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

	private static T FindNodeOfType<T>(Node root) where T : Node
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
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
			}
		}
		return null;
	}

	private static string DescribeObject(GodotObject value)
	{
		if (!GodotObject.IsInstanceValid(value))
		{
			return "null";
		}
		if (value is Node node)
		{
			return $"{node.Name}:{node.GetInstanceId()}";
		}
		return $"{value.GetType().Name}:{value.GetInstanceId()}";
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
			GD.PrintErr("[MOD_EDITOR_2D_TRANSFORM_UNDO_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (FileAccess.FileExists("user://mod_editor_2d_direct_node_property_probe.tscn"))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://mod_editor_2d_direct_node_property_probe.tscn"));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_TRANSFORM_UNDO_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
