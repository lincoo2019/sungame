using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DLayoutMetaRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private string _scenePath = "";

	public override async void _Ready()
	{
		_ = 6;
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
			Require(window, "2D editor is not mounted under the real F3 ModEditor window.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			_scenePath = $"res://Tests/.mod_editor_2d_layout_meta_{Guid.NewGuid():N}.tscn";
			Require(SaveSourceScene(_scenePath), "Could not save the 2D layout source scene.");
			editor.LoadPackedSceneFromPath(_scenePath);
			await WaitFrames(10);
			Control control = editor.CurrentSceneInstance as Control;
			ColorRect layoutTarget = control?.FindChild("LayoutTarget", recursive: false, owned: false) as ColorRect;
			Node2D nodeTarget = control?.FindChild("NodeTarget", recursive: false, owned: false) as Node2D;
			ColorRect controlTarget = control?.FindChild("ControlTarget", recursive: false, owned: false) as ColorRect;
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWSceneNodeTree tree = FindNodeOfType<XWSceneNodeTree>(dock);
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Require(GodotObject.IsInstanceValid(layoutTarget), "Layout target is missing.");
			Require(GodotObject.IsInstanceValid(nodeTarget) && GodotObject.IsInstanceValid(controlTarget), "Multi-selection targets are missing.");
			Require(GodotObject.IsInstanceValid(dock) && GodotObject.IsInstanceValid(tree), "Real scene tree surface is missing.");
			Require(GodotObject.IsInstanceValid(history), "Scene history is missing.");
			if (!GodotObject.IsInstanceValid(layoutTarget) || !GodotObject.IsInstanceValid(nodeTarget) || !GodotObject.IsInstanceValid(controlTarget) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			history.ClearHistory(1);
			history.SetCurrentHistoryType(1);
			history.SetHistoryAsSaved(1);
			GodotObject inspectorBefore = inspector?.CurrentObject;
			SelectOnly(layoutTarget, dock);
			editor.RefreshDirectTransformSurface();
			PanelContainer nodeOrNull = editor.GetNodeOrNull<PanelContainer>("%ControlLayoutPanel");
			int num;
			if (GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Visible && editor.GetNodeOrNull<Button>("%PresetFullRectButton")?.Icon != null && (editor.GetNodeOrNull<SpinBox>("%AnchorLeftSpin")?.Editable ?? false))
			{
				OptionButton nodeOrNull2 = editor.GetNodeOrNull<OptionButton>("%GrowHorizontalOption");
				num = ((nodeOrNull2 != null && nodeOrNull2.ItemCount == 3) ? 1 : 0);
			}
			else
			{
				num = 0;
			}
			bool layoutSurface = (byte)num != 0;
			Require(layoutSurface, "Visual Control layout surface is incomplete or hidden.");
			bool flag = editor.EditSelectedControlLayoutFieldWithHistory("anchor_left", 0.2) && Mathf.IsEqualApprox(layoutTarget.AnchorLeft, 0.2f);
			bool flag2 = history.Undo() && Mathf.IsEqualApprox(layoutTarget.AnchorLeft, 0f);
			bool flag3 = history.Redo() && Mathf.IsEqualApprox(layoutTarget.AnchorLeft, 0.2f);
			bool anchors = flag && flag2 && flag3;
			Require(anchors, "Anchor direct edit did not apply, undo, and redo.");
			bool flag4 = editor.EditSelectedControlLayoutFieldWithHistory("offset_left", 12.0) && Mathf.IsEqualApprox(layoutTarget.OffsetLeft, 12f);
			bool flag5 = history.Undo() && !Mathf.IsEqualApprox(layoutTarget.OffsetLeft, 12f);
			bool flag6 = history.Redo() && Mathf.IsEqualApprox(layoutTarget.OffsetLeft, 12f);
			bool offsets = flag4 && flag5 && flag6;
			Require(offsets, "Offset direct edit did not apply, undo, and redo.");
			bool flag7 = editor.ApplySelectedControlLayoutPresetWithHistory(Control.LayoutPreset.FullRect) && Mathf.IsEqualApprox(layoutTarget.AnchorLeft, 0f) && Mathf.IsEqualApprox(layoutTarget.AnchorTop, 0f) && Mathf.IsEqualApprox(layoutTarget.AnchorRight, 1f) && Mathf.IsEqualApprox(layoutTarget.AnchorBottom, 1f) && Mathf.IsZeroApprox(layoutTarget.OffsetLeft) && Mathf.IsZeroApprox(layoutTarget.OffsetTop) && Mathf.IsZeroApprox(layoutTarget.OffsetRight) && Mathf.IsZeroApprox(layoutTarget.OffsetBottom);
			bool flag8 = history.Undo() && Mathf.IsEqualApprox(layoutTarget.AnchorLeft, 0.2f) && Mathf.IsEqualApprox(layoutTarget.OffsetLeft, 12f);
			bool flag9 = history.Redo() && Mathf.IsEqualApprox(layoutTarget.AnchorRight, 1f) && Mathf.IsZeroApprox(layoutTarget.OffsetLeft);
			bool preset = flag7 && flag8 && flag9;
			Require(preset, "FullRect visual preset did not apply, undo, and redo exact layout state.");
			Button growHorizontalBoth = editor.GetNodeOrNull<Button>("%GrowHorizontalBoth");
			Button growVerticalEnd = editor.GetNodeOrNull<Button>("%GrowVerticalEnd");
			growHorizontalBoth?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			int num2;
			if (layoutTarget.GrowHorizontal == Control.GrowDirection.Both)
			{
				OptionButton nodeOrNull3 = editor.GetNodeOrNull<OptionButton>("%GrowHorizontalOption");
				if (nodeOrNull3 != null && nodeOrNull3.Selected == 1)
				{
					num2 = ((growHorizontalBoth?.ButtonPressed ?? false) ? 1 : 0);
					goto IL_08c9;
				}
			}
			num2 = 0;
			goto IL_08c9;
			IL_08c9:
			bool growH = (byte)num2 != 0;
			growVerticalEnd?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			int num3;
			if (layoutTarget.GrowVertical == Control.GrowDirection.End)
			{
				OptionButton nodeOrNull4 = editor.GetNodeOrNull<OptionButton>("%GrowVerticalOption");
				if (nodeOrNull4 != null && nodeOrNull4.Selected == 2)
				{
					num3 = ((growVerticalEnd?.ButtonPressed ?? false) ? 1 : 0);
					goto IL_0993;
				}
			}
			num3 = 0;
			goto IL_0993;
			IL_0993:
			bool flag10 = (byte)num3 != 0;
			bool flag11 = history.Undo() && layoutTarget.GrowVertical != Control.GrowDirection.End;
			bool flag12 = history.Redo() && layoutTarget.GrowVertical == Control.GrowDirection.End;
			bool grow = growH && flag10 && flag11 && flag12;
			Require(grow, "Grow direction history is incomplete.");
			Require(editor.EditSelectedControlLayoutFieldWithHistory("anchor_left", 0.1), "Final anchor state could not be authored.");
			Require(editor.EditSelectedControlLayoutFieldWithHistory("offset_left", 12.0), "Final offset state could not be authored.");
			SelectMultiple(dock, nodeTarget, controlTarget);
			editor.RefreshDirectTransformSurface();
			bool mixed = editor.IsDirectTransformFieldMixed("position_x");
			float y = nodeTarget.Position.Y;
			float y2 = controlTarget.Position.Y;
			bool flag13 = editor.ApplySelectedTransformFieldWithHistory("position_x", 55.0) && Mathf.IsEqualApprox(nodeTarget.Position.X, 55f) && Mathf.IsEqualApprox(controlTarget.Position.X, 55f) && Mathf.IsEqualApprox(nodeTarget.Position.Y, y) && Mathf.IsEqualApprox(controlTarget.Position.Y, y2) && history.GetCurrentActionName() == "批量编辑 2D 变换";
			bool flag14 = history.Undo() && Mathf.IsEqualApprox(nodeTarget.Position.X, 10f) && Mathf.IsEqualApprox(controlTarget.Position.X, 30f);
			bool flag15 = history.HasRedo();
			bool flag16 = editor.SetSelectionGroupWithHistory(enabled: true) && nodeTarget.HasMeta("_edit_group_") && controlTarget.HasMeta("_edit_group_") && !history.HasRedo();
			bool redoBranch = flag15 && flag16;
			bool flag17 = history.Undo() && !nodeTarget.HasMeta("_edit_group_") && !controlTarget.HasMeta("_edit_group_");
			bool flag18 = history.Redo() && nodeTarget.HasMeta("_edit_group_") && controlTarget.HasMeta("_edit_group_");
			bool multi = mixed && flag13 && flag14;
			bool group = flag16 && flag17 && flag18;
			Require(multi, "Mixed multi-selection did not batch-write only the edited field with history.");
			Require(group, "Group metadata did not use one Scene undo/redo action.");
			Require(redoBranch, "Metadata action did not discard the transform redo branch.");
			bool flag19 = editor.SetSelectionLockWithHistory(enabled: true) && nodeTarget.HasMeta("_edit_lock_") && controlTarget.HasMeta("_edit_lock_");
			bool flag20 = history.Undo() && !nodeTarget.HasMeta("_edit_lock_") && !controlTarget.HasMeta("_edit_lock_") && !tree.IsNodeLocked(nodeTarget) && !tree.IsNodeLocked(controlTarget) && GodotObject.IsInstanceValid(tree.FindItemForNode(nodeTarget));
			bool flag21 = history.Redo() && nodeTarget.HasMeta("_edit_lock_") && controlTarget.HasMeta("_edit_lock_") && tree.IsNodeLocked(nodeTarget) && tree.IsNodeLocked(controlTarget);
			bool meta = flag19 && flag20 && flag21;
			Require(meta, "Lock metadata did not use one Scene undo/redo action.");
			await WaitFrames(3);
			TabBar tabs = editor.GetNodeOrNull<TabBar>("%SceneTabBar");
			bool dirty = history.IsHistoryUnsaved(1) && tabs != null && tabs.GetTabTitle(tabs.CurrentTab).StartsWith("(*) ");
			Require(dirty, "Layout/meta actions did not mark the current scene tab dirty.");
			bool saved = editor.SaveCurrentScene();
			await WaitFrames(3);
			bool flag22 = saved && !history.IsHistoryUnsaved(1) && tabs != null && !tabs.GetTabTitle(tabs.CurrentTab).StartsWith("(*) ");
			Require(flag22, "Save did not clear the Scene dirty state.");
			Node node = ReloadSceneUncached(_scenePath);
			ColorRect colorRect = node?.FindChild("LayoutTarget", recursive: false, owned: false) as ColorRect;
			Node node2 = node?.FindChild("NodeTarget", recursive: false, owned: false);
			Node node3 = node?.FindChild("ControlTarget", recursive: false, owned: false);
			bool flag23 = GodotObject.IsInstanceValid(colorRect) && Mathf.IsEqualApprox(colorRect.AnchorLeft, 0.1f) && Mathf.IsEqualApprox(colorRect.AnchorRight, 1f) && Mathf.IsEqualApprox(colorRect.OffsetLeft, 12f) && colorRect.GrowHorizontal == Control.GrowDirection.Both && colorRect.GrowVertical == Control.GrowDirection.End && node2 != null && node2.HasMeta("_edit_lock_") && node2.HasMeta("_edit_group_") && node3 != null && node3.HasMeta("_edit_lock_") && node3.HasMeta("_edit_group_");
			node?.Free();
			Require(flag23, "Layout and metadata did not survive uncached save/reload.");
			bool flag24 = dock.GetSelectedNode() == controlTarget && GodotObject.IsInstanceValid(tree.FindItemForNode(nodeTarget)) && GodotObject.IsInstanceValid(tree.FindItemForNode(controlTarget)) && tree.IsNodeLocked(nodeTarget) && tree.IsNodeLocked(controlTarget);
			bool flag25 = XWEditorInterface.Instance.GetEditorSelection().IsSelected(nodeTarget) && XWEditorInterface.Instance.GetEditorSelection().IsSelected(controlTarget);
			bool flag26 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorBefore && inspector.CurrentObject != layoutTarget && inspector.CurrentObject != nodeTarget && inspector.CurrentObject != controlTarget;
			Require(flag24, "Tree state/highlight is not synchronized after metadata history.");
			Require(flag25, "Canvas multi-selection was lost during metadata history.");
			Require(flag26, "Direct 2D surfaces redirected the raw Inspector.");
			GD.Print($"[MOD_EDITOR_2D_LAYOUT_META_PROBE] window={window} layoutSurface={layoutSurface} anchors={anchors} offsets={offsets} preset={preset} grow={grow} mixed={mixed} multi={multi} meta={meta} group={group} redoBranch={redoBranch} dirty={dirty} saved={flag22} uncachedReload={flag23} treeSynced={flag24} canvasSelection={flag25} inspectorUntouched={flag26} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PushError(ex.ToString());
		}
		Finish();
	}

	private static bool SaveSourceScene(string path)
	{
		Control control = new Control
		{
			Name = "LayoutMetaRoot",
			Size = new Vector2(800f, 600f)
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "LayoutTarget",
			Position = new Vector2(20f, 20f),
			Size = new Vector2(120f, 60f),
			Color = new Color(0.2f, 0.6f, 0.95f),
			GrowHorizontal = Control.GrowDirection.Begin,
			GrowVertical = Control.GrowDirection.Begin
		};
		Node2D node2D = new Node2D
		{
			Name = "NodeTarget",
			Position = new Vector2(10f, 15f)
		};
		ColorRect colorRect2 = new ColorRect
		{
			Name = "ControlTarget",
			Position = new Vector2(30f, 35f),
			Size = new Vector2(48f, 36f),
			Color = new Color(0.9f, 0.5f, 0.2f)
		};
		control.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		control.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
		control.AddChild(colorRect2, forceReadableName: false, InternalMode.Disabled);
		colorRect.Owner = control;
		node2D.Owner = control;
		colorRect2.Owner = control;
		PackedScene packedScene = new PackedScene();
		Error error = packedScene.Pack(control);
		if (error == Error.Ok)
		{
			error = ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None);
		}
		control.Free();
		return error == Error.Ok;
	}

	private static void SelectOnly(Node node, XWSceneTreeDock dock)
	{
		XWEditorInterface.Instance.ClearSelection();
		XWEditorInterface.Instance.SelectNode(node);
		dock?.SelectNode(node, emitSignal: false);
	}

	private static void SelectMultiple(XWSceneTreeDock dock, params Node[] nodes)
	{
		XWEditorInterface.Instance.ClearSelection();
		foreach (Node node in nodes)
		{
			XWEditorInterface.Instance.SelectNode(node);
		}
		if (nodes.Length != 0)
		{
			dock?.SelectNode(nodes[^1], emitSignal: false);
		}
	}

	private static Node ReloadSceneUncached(string path)
	{
		return ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int frames)
	{
		for (int i = 0; i < frames; i++)
		{
			XW2DSceneEditor xW2DSceneEditor = XWEditorInterface.Instance?.Get2DSceneEditor();
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsInsideTree())
			{
				return xW2DSceneEditor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return null;
	}

	private async Task WaitFrames(int frames)
	{
		for (int i = 0; i < frames; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static Window FindAncestorWindow(Node node)
	{
		for (Node node2 = node; node2 != null; node2 = node2.GetParent())
		{
			if (node2 is Window result)
			{
				return result;
			}
		}
		return null;
	}

	private static T FindNodeOfType<T>(Node root) where T : class
	{
		if (root is T result)
		{
			return result;
		}
		if (root == null)
		{
			return null;
		}
		foreach (Node child in root.GetChildren())
		{
			T val = FindNodeOfType<T>(child);
			if (val != null)
			{
				return val;
			}
		}
		return null;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		if (!string.IsNullOrWhiteSpace(_scenePath) && FileAccess.FileExists(_scenePath))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath(_scenePath));
		}
		if (_failures.Count > 0)
		{
			foreach (string failure in _failures)
			{
				GD.PushError(failure);
			}
			GetTree().Quit(1);
		}
		else
		{
			GetTree().Quit();
		}
	}
}
