using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DSelectionLayoutRuntimeProbe : Node
{
	private const string FixtureScenePath = "user://mod_editor_2d_selection_layout_probe.tscn";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		_ = 40;
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
			Require(SaveSourceScene(), "Could not save the nested 2D source scene.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_selection_layout_probe.tscn");
			await WaitFrames(10);
			XW2DViewport viewport = editor.GetNodeOrNull<XW2DViewport>("%Viewport2D");
			Node2D root = editor.CurrentSceneInstance as Node2D;
			Node2D parentA = root?.FindChild("ParentA", recursive: false, owned: false) as Node2D;
			Node2D node2D = root?.FindChild("ParentB", recursive: false, owned: false) as Node2D;
			Node2D node2D2 = root?.FindChild("ParentC", recursive: false, owned: false) as Node2D;
			Node2D itemA = parentA?.FindChild("ItemA", recursive: false, owned: false) as Node2D;
			Node2D itemB = node2D?.FindChild("ItemB", recursive: false, owned: false) as Node2D;
			Node2D itemC = node2D2?.FindChild("ItemC", recursive: false, owned: false) as Node2D;
			Node2D lockedItem = root?.FindChild("LockedItem", recursive: true, owned: false) as Node2D;
			Node2D snapSource = root?.FindChild("SnapSource", recursive: false, owned: false) as Node2D;
			Node2D snapTarget = root?.FindChild("SnapTarget", recursive: false, owned: false) as Node2D;
			Node2D overlapTop = root?.FindChild("OverlapTop", recursive: false, owned: false) as Node2D;
			Node2D overlapBottom = root?.FindChild("OverlapBottom", recursive: false, owned: false) as Node2D;
			AabbProbe2D helperProbe = new AabbProbe2D
			{
				Name = "HelperProbe",
				Position = new Vector2(1180f, 520f)
			};
			root?.AddChild(helperProbe, forceReadableName: false, InternalMode.Disabled);
			viewport?.NotifySceneStructureChanged();
			XWSceneTreeDock dock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node inspectorSentinel = new Node
			{
				Name = "SelectionLayoutInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			Require(GodotObject.IsInstanceValid(viewport), "The real 2D viewport is missing.");
			Require(GodotObject.IsInstanceValid(itemA) && GodotObject.IsInstanceValid(itemB) && GodotObject.IsInstanceValid(itemC) && GodotObject.IsInstanceValid(lockedItem) && GodotObject.IsInstanceValid(snapSource) && GodotObject.IsInstanceValid(snapTarget) && GodotObject.IsInstanceValid(overlapTop) && GodotObject.IsInstanceValid(overlapBottom) && GodotObject.IsInstanceValid(helperProbe), "2D interaction and layout targets are missing.");
			Require(GodotObject.IsInstanceValid(history), "Scene history is missing.");
			if (!GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(itemA) || !GodotObject.IsInstanceValid(itemB) || !GodotObject.IsInstanceValid(itemC) || !GodotObject.IsInstanceValid(lockedItem) || !GodotObject.IsInstanceValid(snapSource) || !GodotObject.IsInstanceValid(snapTarget) || !GodotObject.IsInstanceValid(overlapTop) || !GodotObject.IsInstanceValid(overlapBottom) || !GodotObject.IsInstanceValid(helperProbe) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			int historyId = editor.GetCurrentSceneHistoryId();
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			history.SetHistoryAsSaved(historyId);
			viewport.SetZoom(1f);
			viewport.CenterAt(Vector2.Zero);
			viewport.ShowRulers = true;
			viewport.ShowGuides = true;
			await WaitFrames(3);
			Vector2 vector = viewport.Size * 0.5f;
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(vector.X, 8f), pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(new Vector2(vector.X + 24f, 96f), new Vector2(24f, 88f)));
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(vector.X + 24f, 96f), pressed: false));
			bool flag = viewport.VerticalGuideCount == 1;
			float verticalGuide = viewport.GetVerticalGuide(0);
			Vector2 vector2 = vector + new Vector2(verticalGuide, 0f);
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(vector2.X, 96f), pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(new Vector2(vector2.X + 32f, 96f), new Vector2(32f, 0f)));
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(vector2.X + 32f, 96f), pressed: false));
			bool flag2 = viewport.VerticalGuideCount == 1 && Same(viewport.GetVerticalGuide(0), verticalGuide + 32f);
			Dictionary state = viewport.GetState();
			viewport.ClearGuides();
			viewport.SetState(state);
			bool flag3 = viewport.VerticalGuideCount == 1 && Same(viewport.GetVerticalGuide(0), verticalGuide + 32f);
			float verticalGuide2 = viewport.GetVerticalGuide(0);
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(vector.X + verticalGuide2, 96f), pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(new Vector2(8f, 8f), new Vector2(8f - vector.X - verticalGuide2, -88f)));
			EmitCanvasInput(viewport, MouseButtonEvent(new Vector2(8f, 8f), pressed: false));
			bool flag4 = viewport.VerticalGuideCount == 0;
			bool guides = flag && flag2 && flag3 && flag4;
			Require(guides, "Guide creation, movement, deletion, or viewport-state restore failed.");
			viewport.SetToolMode(XW2DViewport.ToolMode.Ruler);
			viewport.GridSnapActive = false;
			EmitCanvasInput(viewport, MouseButtonEvent(vector, pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(vector + new Vector2(30f, 40f), new Vector2(30f, 40f)));
			EmitCanvasInput(viewport, MouseButtonEvent(vector + new Vector2(30f, 40f), pressed: false));
			bool ruler = viewport.RulerMeasurementVisible && Same(viewport.RulerLength, 50f) && Same(viewport.RulerAngleDegrees, Mathf.RadToDeg(Mathf.Atan2(40f, 30f)));
			EmitCanvasInput(viewport, new InputEventKey
			{
				Keycode = Key.Escape,
				Pressed = true
			});
			bool rulerClear = !viewport.RulerMeasurementVisible;
			Require(ruler && rulerClear, "Ruler measurement or Escape clearing failed.");
			viewport.SetShowHelpers(enabled: false);
			await WaitFrames(2);
			bool helperOff = !helperProbe.IsProcessing();
			viewport.SetShowHelpers(enabled: true);
			await WaitFrames(2);
			bool helperOn = helperProbe.IsProcessing();
			viewport.SetShowHelpers(enabled: false);
			await WaitFrames(2);
			bool flag5 = !helperProbe.IsProcessing();
			bool helperToggle = helperOff && helperOn && flag5;
			Require(helperToggle, "Show Helpers did not control collision preview processing.");
			viewport.SetToolMode(XW2DViewport.ToolMode.ListSelect);
			viewport.CenterAt(overlapTop.Position);
			await WaitFrames(3);
			EmitCanvasInput(viewport, MouseButtonEvent(viewport.Size * 0.5f, pressed: true));
			await WaitFrames(2);
			PopupMenu listMenu = viewport.GetNodeOrNull<PopupMenu>("CanvasItemListSelectMenu");
			bool listPopup = GodotObject.IsInstanceValid(listMenu) && listMenu.Visible && viewport.ListSelectCandidateCount == 2 && listMenu.GetItemText(0).Contains("OverlapTop") && listMenu.GetItemText(1).Contains("OverlapBottom");
			listMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 1L);
			await WaitFrames(2);
			bool listSelect = listPopup && XWEditorInterface.Instance.GetEditorSelection().IsSelected(overlapBottom) && !XWEditorInterface.Instance.GetEditorSelection().IsSelected(overlapTop);
			viewport.SetToolMode(XW2DViewport.ToolMode.ListSelect);
			EmitCanvasInput(viewport, MouseButtonEvent(viewport.Size * 0.5f, pressed: true));
			await WaitFrames(1);
			EmitCanvasInput(viewport, new InputEventKey
			{
				Keycode = Key.Escape,
				Pressed = true
			});
			bool listSelectCancel = GodotObject.IsInstanceValid(listMenu) && !listMenu.Visible && XWEditorInterface.Instance.GetEditorSelection().IsSelected(overlapBottom);
			Require(listSelect && listSelectCancel, "Visual overlap list selection or Escape cancel failed.");
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			SelectOnly(snapSource, dock);
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			viewport.UseLocalSpace = false;
			viewport.SmartSnapActive = true;
			viewport.GridSnapActive = false;
			Vector2 snapBefore = snapSource.Position;
			viewport.CenterAt(snapSource.Position);
			await WaitFrames(3);
			Vector2 vector3 = viewport.Size * 0.5f;
			Vector2 vector4 = new Vector2(72f, 0f);
			EmitCanvasInput(viewport, MouseButtonEvent(vector3, pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(vector3 + vector4, vector4));
			bool smartFeedback = viewport.HasActiveSmartSnap;
			EmitCanvasInput(viewport, MouseButtonEvent(vector3 + vector4, pressed: false));
			bool smartSnap = Same(snapSource.Position.X - snapBefore.X, 76f) && Same(snapSource.Position.Y, snapBefore.Y) && history.GetCurrentActionName() == "移动 2D 节点";
			bool smartSnapUndoRedo = history.Undo() && snapSource.Position.IsEqualApprox(snapBefore) && history.Redo() && Same(snapSource.Position.X - snapBefore.X, 76f);
			Require(smartFeedback && smartSnap && smartSnapUndoRedo, "Smart snap feedback, exact alignment, or undo/redo failed.");
			Require(history.Undo(), "Could not restore smart snap source.");
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			SelectOnly(itemA, dock);
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			viewport.SmartSnapActive = false;
			viewport.UseLocalSpace = true;
			Vector2 localBefore = itemA.Position;
			Vector2 localWorldBefore = viewport.GetCanvasItemWorldOrigin(itemA);
			viewport.CenterAt(localWorldBefore);
			await WaitFrames(3);
			Vector2 vector5 = viewport.Size * 0.5f;
			Vector2 vector6 = new Vector2(38f, 27f);
			EmitCanvasInput(viewport, MouseButtonEvent(vector5, pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(vector5 + vector6, vector6, shift: true));
			Vector2 left = viewport.GetCanvasItemWorldOrigin(itemA) - localWorldBefore;
			Vector2 with = itemA.GetGlobalTransform().X.Normalized();
			Vector2 with2 = itemA.GetGlobalTransform().Y.Normalized();
			bool localSpace = left.Length() > 1f && (Mathf.Abs(left.Cross(with)) < 0.1f || Mathf.Abs(left.Cross(with2)) < 0.1f) && !Approximately(left, vector6);
			EmitCanvasInput(viewport, new InputEventKey
			{
				Keycode = Key.Escape,
				Pressed = true
			});
			bool localSpaceCancel = itemA.Position.IsEqualApprox(localBefore) && !history.HasUndo();
			Require(localSpace && localSpaceCancel, "Local-axis Shift constraint or Escape transform restore failed.");
			SelectOnly(snapSource, dock);
			viewport.SetToolMode(XW2DViewport.ToolMode.Pivot);
			viewport.UseLocalSpace = false;
			viewport.CenterAt(snapSource.Position);
			await WaitFrames(2);
			Vector2 vector7 = viewport.Size * 0.5f;
			EmitCanvasInput(viewport, MouseButtonEvent(vector7, pressed: true));
			EmitCanvasInput(viewport, MouseMotionEvent(vector7 + new Vector2(20f, 18f), new Vector2(20f, 18f)));
			EmitCanvasInput(viewport, MouseButtonEvent(vector7 + new Vector2(20f, 18f), pressed: false));
			bool pivotApplied = snapSource.HasMeta("_edit_pivot_") && history.GetCurrentActionName() == "移动 2D 轴心";
			bool pivotUndoRedo = history.Undo() && !snapSource.HasMeta("_edit_pivot_") && history.Redo() && snapSource.HasMeta("_edit_pivot_");
			Require(pivotApplied && pivotUndoRedo, "Pivot edit did not use reversible scene history.");
			Require(history.Undo(), "Could not restore pivot metadata.");
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			viewport.UseLocalSpace = false;
			viewport.SmartSnapActive = false;
			Node[] array = new Node[6]
			{
				snapSource,
				snapTarget,
				overlapTop,
				overlapBottom,
				root.FindChild("OverlapLocked", recursive: false, owned: false),
				helperProbe
			};
			foreach (Node node in array)
			{
				if (GodotObject.IsInstanceValid(node) && node.GetParent() == root)
				{
					root.RemoveChild(node);
					node.QueueFree();
				}
			}
			viewport.NotifySceneStructureChanged();
			await WaitFrames(3);
			Vector2 nestedBeforeCenter = Center(viewport.GetCanvasItemWorldBounds(itemA));
			Vector2 nestedBeforeLocal = itemA.Position;
			SelectOnly(itemA, dock);
			editor.RefreshDirectTransformSurface();
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			viewport.SetZoom(1f);
			viewport.CenterAt(nestedBeforeCenter);
			await WaitFrames(4);
			Vector2 dragDelta = new Vector2(48f, 32f);
			Vector2 vector8 = viewport.Size * 0.5f;
			EmitCanvasInput(viewport, new InputEventMouseButton
			{
				Position = vector8,
				ButtonIndex = MouseButton.Left,
				ButtonMask = MouseButtonMask.Left,
				Pressed = true
			});
			EmitCanvasInput(viewport, new InputEventMouseMotion
			{
				Position = vector8 + dragDelta,
				Relative = dragDelta,
				ButtonMask = MouseButtonMask.Left
			});
			EmitCanvasInput(viewport, new InputEventMouseButton
			{
				Position = vector8 + dragDelta,
				ButtonIndex = MouseButton.Left,
				ButtonMask = (MouseButtonMask)0L,
				Pressed = false
			});
			await WaitFrames(4);
			Vector2 vector9 = Center(viewport.GetCanvasItemWorldBounds(itemA));
			bool nestedMove = (vector9 - nestedBeforeCenter).IsEqualApprox(dragDelta) && !itemA.Position.IsEqualApprox(nestedBeforeLocal + dragDelta) && history.GetCurrentActionName() == "移动 2D 节点";
			bool nestedUndo = history.Undo() && itemA.Position.IsEqualApprox(nestedBeforeLocal);
			bool nestedRedo = history.Redo() && Center(viewport.GetCanvasItemWorldBounds(itemA)).IsEqualApprox(vector9);
			Require(nestedMove, "Nested canvas drag did not preserve the requested world delta.");
			Require(nestedUndo && nestedRedo, "Nested canvas drag did not undo and redo.");
			Require(history.Undo(), "Could not restore the nested node before layout checks.");
			await WaitFrames(2);
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			SelectMultiple(dock, itemA, itemB, itemC);
			editor.RefreshDirectTransformSurface();
			await WaitFrames(2);
			Rect2 rect = MergeBounds(GetBounds(viewport, itemA, itemB, itemC));
			Vector2 rotatePivot = Center(rect);
			Vector2[] rotatePositionsBefore = new Vector2[3] { itemA.Position, itemB.Position, itemC.Position };
			float[] rotationsBefore = new float[3] { itemA.Rotation, itemB.Rotation, itemC.Rotation };
			Vector2[] rotateOriginsBefore = new Vector2[3]
			{
				viewport.GetCanvasItemWorldOrigin(itemA),
				viewport.GetCanvasItemWorldOrigin(itemB),
				viewport.GetCanvasItemWorldOrigin(itemC)
			};
			float angleDelta = Mathf.DegToRad(34f);
			viewport.SetToolMode(XW2DViewport.ToolMode.Rotate);
			viewport.SetZoom(1f);
			viewport.CenterAt(rotatePivot);
			await WaitFrames(3);
			Vector2 vector10 = rotateOriginsBefore[1];
			Vector2 endWorld = rotatePivot + (vector10 - rotatePivot).Rotated(angleDelta);
			EmitWorldDrag(viewport, rotatePivot, vector10, endWorld);
			await WaitFrames(3);
			Node2D[] multiItems = new Node2D[3] { itemA, itemB, itemC };
			bool rotatePivotApplied = true;
			for (int j = 0; j < multiItems.Length; j++)
			{
				Vector2 right = rotatePivot + (rotateOriginsBefore[j] - rotatePivot).Rotated(angleDelta);
				rotatePivotApplied &= Approximately(viewport.GetCanvasItemWorldOrigin(multiItems[j]), right) && Same(multiItems[j].Rotation, rotationsBefore[j] + angleDelta);
			}
			bool multiDragPreserved = multiItems.All(XWEditorInterface.Instance.GetEditorSelection().IsSelected);
			bool rotateHistory = history.GetCurrentActionName() == "旋转 2D 节点";
			bool rotateUndo = history.Undo();
			for (int k = 0; k < multiItems.Length; k++)
			{
				rotateUndo &= multiItems[k].Position.IsEqualApprox(rotatePositionsBefore[k]) && Same(multiItems[k].Rotation, rotationsBefore[k]);
			}
			bool rotateRedo = history.Redo();
			await WaitFrames(2);
			bool num = rotateRedo;
			Vector2 canvasItemWorldOrigin = viewport.GetCanvasItemWorldOrigin(itemA);
			Vector2 value = rotateOriginsBefore[0] - rotatePivot;
			rotateRedo = num & Approximately(canvasItemWorldOrigin, rotatePivot + value.Rotated(angleDelta));
			bool rotatePivotResult = rotatePivotApplied && multiDragPreserved && rotateHistory && rotateUndo && rotateRedo;
			Require(rotatePivotResult, "Multi-rotation did not preserve selection and rotate all world origins around one pivot.");
			Require(history.Undo(), "Could not restore the multi-rotation input state.");
			await WaitFrames(2);
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			rect = MergeBounds(GetBounds(viewport, itemA, itemB, itemC));
			Vector2 scalePivot = Center(rect);
			Vector2[] scalePositionsBefore = new Vector2[3] { itemA.Position, itemB.Position, itemC.Position };
			Vector2[] scalesBefore = new Vector2[3] { itemA.Scale, itemB.Scale, itemC.Scale };
			Vector2[] scaleOriginsBefore = new Vector2[3]
			{
				viewport.GetCanvasItemWorldOrigin(itemA),
				viewport.GetCanvasItemWorldOrigin(itemB),
				viewport.GetCanvasItemWorldOrigin(itemC)
			};
			viewport.SetToolMode(XW2DViewport.ToolMode.Scale);
			viewport.CenterAt(scalePivot);
			await WaitFrames(3);
			Vector2 vector11 = scaleOriginsBefore[2];
			Vector2 endWorld2 = scalePivot + (vector11 - scalePivot) * 1.35f;
			EmitWorldDrag(viewport, scalePivot, vector11, endWorld2);
			await WaitFrames(3);
			bool scalePivotApplied = true;
			for (int l = 0; l < multiItems.Length; l++)
			{
				Vector2 right2 = scalePivot + (scaleOriginsBefore[l] - scalePivot) * 1.35f;
				scalePivotApplied &= Approximately(viewport.GetCanvasItemWorldOrigin(multiItems[l]), right2) && Approximately(multiItems[l].Scale, scalesBefore[l] * 1.35f);
			}
			bool scaleHistory = history.GetCurrentActionName() == "缩放 2D 节点";
			bool scaleUndo = history.Undo();
			for (int m = 0; m < multiItems.Length; m++)
			{
				bool num2 = scaleUndo;
				value = multiItems[m].Position;
				scaleUndo = num2 & (value.IsEqualApprox(scalePositionsBefore[m]) && Approximately(multiItems[m].Scale, scalesBefore[m]));
			}
			bool scaleRedo = history.Redo();
			await WaitFrames(2);
			scaleRedo &= Approximately(viewport.GetCanvasItemWorldOrigin(itemC), scalePivot + (scaleOriginsBefore[2] - scalePivot) * 1.35f);
			bool scalePivotResult = scalePivotApplied && scaleHistory && scaleUndo && scaleRedo;
			Require(scalePivotResult, "Multi-scale did not move and scale all nodes around one world pivot.");
			Require(history.Undo(), "Could not restore the multi-scale input state.");
			await WaitFrames(2);
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			Vector2 lockedBefore = lockedItem.Position;
			SelectMultiple(dock, itemA, itemB, itemC, lockedItem);
			editor.RefreshDirectTransformSurface();
			await WaitFrames(2);
			PanelContainer nodeOrNull = editor.GetNodeOrNull<PanelContainer>("%SelectionLayoutPanel");
			Button[] layoutButtons = new Button[8]
			{
				editor.GetNodeOrNull<Button>("%AlignSelectionLeftButton"),
				editor.GetNodeOrNull<Button>("%AlignSelectionHorizontalCenterButton"),
				editor.GetNodeOrNull<Button>("%AlignSelectionRightButton"),
				editor.GetNodeOrNull<Button>("%AlignSelectionTopButton"),
				editor.GetNodeOrNull<Button>("%AlignSelectionVerticalCenterButton"),
				editor.GetNodeOrNull<Button>("%AlignSelectionBottomButton"),
				editor.GetNodeOrNull<Button>("%DistributeSelectionHorizontalButton"),
				editor.GetNodeOrNull<Button>("%DistributeSelectionVerticalButton")
			};
			bool visual = GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Visible && layoutButtons.All((Button button) => GodotObject.IsInstanceValid(button) && !button.Disabled && button.Icon != null) && (editor.GetNodeOrNull<Label>("%SelectionLayoutLabel")?.Text.Contains("3 项") ?? false);
			Require(visual, "The iconized selection layout surface is incomplete.");
			Vector2[] alignBefore = new Vector2[3] { itemA.Position, itemB.Position, itemC.Position };
			layoutButtons[0]?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			Rect2[] bounds = GetBounds(viewport, itemA, itemB, itemC);
			int num3;
			if (Same(bounds[0].Position.X, bounds[1].Position.X) && Same(bounds[1].Position.X, bounds[2].Position.X))
			{
				value = lockedItem.Position;
				num3 = (value.IsEqualApprox(lockedBefore) ? 1 : 0);
			}
			else
			{
				num3 = 0;
			}
			bool align = (byte)num3 != 0;
			bool singleHistory = history.HasUndo() && history.GetCurrentActionName() == "左对齐 2D 节点";
			int num4;
			if (history.Undo())
			{
				value = itemA.Position;
				if (value.IsEqualApprox(alignBefore[0]))
				{
					value = itemB.Position;
					if (value.IsEqualApprox(alignBefore[1]))
					{
						value = itemC.Position;
						num4 = (value.IsEqualApprox(alignBefore[2]) ? 1 : 0);
						goto IL_2b6d;
					}
				}
			}
			num4 = 0;
			goto IL_2b6d;
			IL_2b6d:
			bool alignUndo = (byte)num4 != 0;
			bool alignRedo = history.Redo();
			await WaitFrames(2);
			Rect2[] bounds2 = GetBounds(viewport, itemA, itemB, itemC);
			alignRedo &= Same(bounds2[0].Position.X, bounds2[1].Position.X) && Same(bounds2[1].Position.X, bounds2[2].Position.X);
			Require(align, "Left alignment did not align world-space bounds or changed the locked node.");
			Require(singleHistory && alignUndo && alignRedo, "Alignment did not use one undoable Scene action.");
			Require(history.Undo(), "Could not restore the alignment input positions.");
			await WaitFrames(2);
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			layoutButtons[6]?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			int num5;
			if (HasEqualGaps(GetBounds(viewport, itemA, itemB, itemC), horizontal: true) && history.GetCurrentActionName() == "水平等距分布 2D 节点")
			{
				value = lockedItem.Position;
				num5 = (value.IsEqualApprox(lockedBefore) ? 1 : 0);
			}
			else
			{
				num5 = 0;
			}
			bool distributeH = (byte)num5 != 0;
			bool distributeHUndo = history.Undo();
			bool distributeHRedo = history.Redo();
			await WaitFrames(2);
			distributeHRedo &= HasEqualGaps(GetBounds(viewport, itemA, itemB, itemC), horizontal: true);
			Require(distributeH && distributeHUndo && distributeHRedo, "Horizontal distribution or its history roundtrip failed.");
			Require(history.Undo(), "Could not restore the horizontal distribution input positions.");
			await WaitFrames(2);
			history.ClearHistory(historyId);
			history.SetCurrentHistoryType(historyId);
			layoutButtons[7]?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			int num6;
			if (HasEqualGaps(GetBounds(viewport, itemA, itemB, itemC), horizontal: false) && history.GetCurrentActionName() == "垂直等距分布 2D 节点")
			{
				value = lockedItem.Position;
				num6 = (value.IsEqualApprox(lockedBefore) ? 1 : 0);
			}
			else
			{
				num6 = 0;
			}
			bool distributeV = (byte)num6 != 0;
			bool distributeVUndo = history.Undo();
			bool distributeVRedo = history.Redo();
			await WaitFrames(2);
			distributeVRedo &= HasEqualGaps(GetBounds(viewport, itemA, itemB, itemC), horizontal: false);
			Require(distributeV && distributeVUndo && distributeVRedo, "Vertical distribution or its history roundtrip failed.");
			Vector2 childBeforeAncestorLayout = itemA.Position;
			SelectMultiple(dock, parentA, itemA, itemB);
			editor.RefreshDirectTransformSurface();
			await WaitFrames(2);
			bool ancestorPanelCount = editor.GetNodeOrNull<Label>("%SelectionLayoutLabel")?.Text.Contains("2 项") ?? false;
			bool ancestorApplied = editor.ApplySelectionLayoutWithHistory(XW2DSceneEditor.SelectionLayoutOperation.AlignTop);
			await WaitFrames(2);
			int num7;
			if (ancestorPanelCount && ancestorApplied)
			{
				value = itemA.Position;
				num7 = (value.IsEqualApprox(childBeforeAncestorLayout) ? 1 : 0);
			}
			else
			{
				num7 = 0;
			}
			bool ancestorFiltered = (byte)num7 != 0;
			Require(ancestorFiltered, "Selected descendant was transformed twice with its ancestor.");
			SelectMultiple(dock, itemA, itemB, itemC, lockedItem);
			editor.RefreshDirectTransformSurface();
			await WaitFrames(2);
			System.Collections.Generic.Dictionary<string, Vector2> savedPositions = new System.Collections.Generic.Dictionary<string, Vector2>
			{
				["ItemA"] = itemA.Position,
				["ItemB"] = itemB.Position,
				["ItemC"] = itemC.Position,
				["LockedItem"] = lockedItem.Position
			};
			bool saved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node node2 = ResourceLoader.Load<PackedScene>("user://mod_editor_2d_selection_layout_probe.tscn", "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
			bool flag6 = saved && GodotObject.IsInstanceValid(node2);
			foreach (KeyValuePair<string, Vector2> item in savedPositions)
			{
				item.Deconstruct(out var key, out value);
				string pattern = key;
				Vector2 other = value;
				Node2D node2D3 = node2?.FindChild(pattern, recursive: true, owned: false) as Node2D;
				bool num8 = flag6;
				int num9;
				if (GodotObject.IsInstanceValid(node2D3))
				{
					value = node2D3.Position;
					num9 = (value.IsEqualApprox(other) ? 1 : 0);
				}
				else
				{
					num9 = 0;
				}
				flag6 = (byte)((num8 ? 1u : 0u) & (uint)num9) != 0;
			}
			node2?.Free();
			Require(flag6, "Selection layout positions did not survive uncached save/reload.");
			bool flag7 = XWEditorInterface.Instance.GetEditorSelection().IsSelected(itemA) && XWEditorInterface.Instance.GetEditorSelection().IsSelected(itemB) && XWEditorInterface.Instance.GetEditorSelection().IsSelected(itemC) && XWEditorInterface.Instance.GetEditorSelection().IsSelected(lockedItem);
			bool flag8 = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorSentinel && inspector.CurrentObject != itemA && inspector.CurrentObject != itemB && inspector.CurrentObject != itemC;
			Require(flag7, "Layout actions did not preserve canvas selection.");
			Require(flag8, "Selection layout redirected the raw Inspector.");
			GD.Print($"[MOD_EDITOR_2D_SELECTION_LAYOUT_PROBE] window={window} visual={visual} nestedMove={nestedMove} nestedUndoRedo={nestedUndo && nestedRedo} align={align} rotatePivot={rotatePivotResult} rotateApplied={rotatePivotApplied} rotateHistory={rotateHistory} rotateUndo={rotateUndo} rotateRedo={rotateRedo} scalePivot={scalePivotResult} multiDragPreserved={multiDragPreserved} smartSnap={smartSnap && smartFeedback} smartSnapUndoRedo={smartSnapUndoRedo} guides={guides} ruler={ruler && rulerClear} listSelect={listSelect && listSelectCancel} localSpace={localSpace && localSpaceCancel} helpers={helperToggle} pivotUndoRedo={pivotApplied && pivotUndoRedo} distributeH={distributeH} distributeV={distributeV} lockedIgnored={lockedItem.Position.IsEqualApprox(lockedBefore)} singleHistory={singleHistory} undo={alignUndo} redo={alignRedo} ancestorFiltered={ancestorFiltered} saved={saved} reloaded={flag6} selectionPreserved={flag7} inspectorUntouched={flag8} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PrintErr(ex.ToString());
		}
		Finish();
	}

	private static bool SaveSourceScene()
	{
		Node2D node2D = new Node2D
		{
			Name = "SelectionLayoutRoot"
		};
		Node2D node2D2 = CreateParent("ParentA", new Vector2(90f, 90f), 24f, new Vector2(1.25f, 0.75f));
		Node2D node2D3 = CreateParent("ParentB", new Vector2(310f, 210f), -18f, new Vector2(0.8f, 1.3f));
		Node2D node2D4 = CreateParent("ParentC", new Vector2(560f, 390f), 31f, new Vector2(1.1f, 0.9f));
		Node2D node2D5 = new Node2D
		{
			Name = "ItemA",
			Position = new Vector2(18f, -12f)
		};
		Node2D node2D6 = new Node2D
		{
			Name = "ItemB",
			Position = new Vector2(-26f, 20f)
		};
		Node2D node2D7 = new Node2D
		{
			Name = "ItemC",
			Position = new Vector2(32f, 16f)
		};
		Node2D node2D8 = new Node2D
		{
			Name = "LockedItem",
			Position = new Vector2(720f, 130f)
		};
		Node2D node2D9 = new Node2D
		{
			Name = "SnapSource",
			Position = new Vector2(900f, 400f)
		};
		Node2D node2D10 = new Node2D
		{
			Name = "SnapTarget",
			Position = new Vector2(1000f, 400f)
		};
		Node2D node2D11 = new Node2D
		{
			Name = "OverlapBottom",
			Position = new Vector2(1100f, 500f)
		};
		Node2D node2D12 = new Node2D
		{
			Name = "OverlapTop",
			Position = new Vector2(1100f, 500f)
		};
		Node2D node2D13 = new Node2D
		{
			Name = "OverlapLocked",
			Position = new Vector2(1100f, 500f)
		};
		node2D8.SetMeta("_edit_lock_", true);
		node2D13.SetMeta("_edit_lock_", true);
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D4, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D8, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D9, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D10, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D11, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D12, forceReadableName: false, InternalMode.Disabled);
		node2D.AddChild(node2D13, forceReadableName: false, InternalMode.Disabled);
		node2D2.AddChild(node2D5, forceReadableName: false, InternalMode.Disabled);
		node2D3.AddChild(node2D6, forceReadableName: false, InternalMode.Disabled);
		node2D4.AddChild(node2D7, forceReadableName: false, InternalMode.Disabled);
		Node[] array = new Node[12]
		{
			node2D2, node2D3, node2D4, node2D5, node2D6, node2D7, node2D8, node2D9, node2D10, node2D11,
			node2D12, node2D13
		};
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Owner = node2D;
		}
		PackedScene packedScene = new PackedScene
		{
			ResourceName = "SelectionLayoutProbe"
		};
		Error error = packedScene.Pack(node2D);
		if (error == Error.Ok)
		{
			error = ResourceSaver.Save(packedScene, "user://mod_editor_2d_selection_layout_probe.tscn", ResourceSaver.SaverFlags.None);
		}
		node2D.Free();
		return error == Error.Ok;
	}

	private static Node2D CreateParent(string name, Vector2 position, float rotationDegrees, Vector2 scale)
	{
		return new Node2D
		{
			Name = name,
			Position = position,
			RotationDegrees = rotationDegrees,
			Scale = scale
		};
	}

	private static Rect2[] GetBounds(XW2DViewport viewport, params CanvasItem[] items)
	{
		return items.Select(viewport.GetCanvasItemWorldBounds).ToArray();
	}

	private static Vector2 Center(Rect2 rect)
	{
		return rect.Position + rect.Size * 0.5f;
	}

	private static bool Same(float left, float right)
	{
		if (!Mathf.IsEqualApprox(left, right))
		{
			return Mathf.Abs(left - right) <= 0.05f;
		}
		return true;
	}

	private static bool Approximately(Vector2 left, Vector2 right)
	{
		if (!left.IsEqualApprox(right))
		{
			return left.DistanceTo(right) <= 0.1f;
		}
		return true;
	}

	private static Rect2 MergeBounds(Rect2[] bounds)
	{
		Rect2 result = bounds[0];
		for (int i = 1; i < bounds.Length; i++)
		{
			result = result.Merge(bounds[i]);
		}
		return result;
	}

	private static bool HasEqualGaps(Rect2[] bounds, bool horizontal)
	{
		System.Array.Sort(bounds, (Rect2 rect, Rect2 rect2) => AxisStart(rect, horizontal).CompareTo(AxisStart(rect2, horizontal)));
		float left = AxisStart(bounds[1], horizontal) - AxisEnd(bounds[0], horizontal);
		float right = AxisStart(bounds[2], horizontal) - AxisEnd(bounds[1], horizontal);
		return Same(left, right);
	}

	private static float AxisStart(Rect2 rect, bool horizontal)
	{
		if (!horizontal)
		{
			return rect.Position.Y;
		}
		return rect.Position.X;
	}

	private static float AxisEnd(Rect2 rect, bool horizontal)
	{
		return AxisStart(rect, horizontal) + (horizontal ? rect.Size.X : rect.Size.Y);
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

	private static void EmitCanvasInput(XW2DViewport viewport, InputEvent inputEvent)
	{
		viewport.EmitSignal(Control.SignalName.GuiInput, inputEvent);
	}

	private static InputEventMouseButton MouseButtonEvent(Vector2 position, bool pressed)
	{
		return new InputEventMouseButton
		{
			Position = position,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)(pressed ? 1 : 0),
			Pressed = pressed
		};
	}

	private static InputEventMouseMotion MouseMotionEvent(Vector2 position, Vector2 relative, bool shift = false)
	{
		return new InputEventMouseMotion
		{
			Position = position,
			Relative = relative,
			ButtonMask = MouseButtonMask.Left,
			ShiftPressed = shift
		};
	}

	private static void EmitWorldDrag(XW2DViewport viewport, Vector2 centeredWorld, Vector2 startWorld, Vector2 endWorld)
	{
		Vector2 vector = viewport.Size * 0.5f;
		Vector2 vector2 = vector + (startWorld - centeredWorld) * viewport.Zoom;
		Vector2 vector3 = vector + (endWorld - centeredWorld) * viewport.Zoom;
		EmitCanvasInput(viewport, new InputEventMouseButton
		{
			Position = vector2,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		});
		EmitCanvasInput(viewport, new InputEventMouseMotion
		{
			Position = vector3,
			Relative = vector3 - vector2,
			ButtonMask = MouseButtonMask.Left
		});
		EmitCanvasInput(viewport, new InputEventMouseButton
		{
			Position = vector3,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		});
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
			GD.PrintErr("[MOD_EDITOR_2D_SELECTION_LAYOUT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (FileAccess.FileExists("user://mod_editor_2d_selection_layout_probe.tscn"))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://mod_editor_2d_selection_layout_probe.tscn"));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_SELECTION_LAYOUT_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
