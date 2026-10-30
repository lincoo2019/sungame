using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DOnCanvasGizmoRuntimeProbe : Node
{
	private const string FixtureScenePath = "user://mod_editor_2d_on_canvas_gizmo_probe.tscn";

	private const int CanvasItemCount = 1000;

	private const double HitDragP95BudgetMilliseconds = 16.0;

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _elapsed = Stopwatch.StartNew();

	private bool _singleHistory = true;

	private bool _undoRedo = true;

	private bool _handleDispatch = true;

	private Viewport _inputViewport;

	private int _viewportGuiInputCount;

	private Vector2 _lastViewportGuiPosition;

	private Vector2 _lastViewportGuiGlobalPosition;

	private string _lastViewportGuiEventType = "<none>";

	public override async void _Ready()
	{
		try
		{
			Require(SaveFixtureScene(), "Could not save the isolated 1000-CanvasItem gizmo fixture.");
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
			Require(await WaitForModEditorReady(900), "F3 ModEditor did not finish its loading queue.");
			XW2DSceneEditor editor = await WaitForSceneEditor(60);
			bool window = FindAncestorWindow(editor) != null;
			Require(window, "The real F3 ModEditor window did not mount the 2D scene editor.");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			bool flag = await EnterEditorSurface(editor, 60);
			Require(flag, "F3 ModEditor did not enter the real 2D editing surface.");
			if (!flag)
			{
				Finish();
				return;
			}
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_on_canvas_gizmo_probe.tscn");
			await WaitFrames(10);
			ActivateEditorDock(editor);
			TabContainer nodeOrNull = editor.GetNodeOrNull<TabContainer>("%WorkspaceTabs");
			if (GodotObject.IsInstanceValid(nodeOrNull))
			{
				nodeOrNull.CurrentTab = 0;
			}
			await WaitFrames(3);
			XW2DViewport viewport = editor.FindChild("Viewport2D", recursive: true, owned: false) as XW2DViewport;
			Node2D target = editor.CurrentSceneInstance?.FindChild("GizmoNode2D", recursive: true, owned: false) as Node2D;
			Control control = editor.CurrentSceneInstance?.FindChild("GizmoControl", recursive: true, owned: false) as Control;
			XWSceneTreeDock sceneDock = XWEditorInterface.Instance?.GetSceneTreeDock();
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			bool flag2 = IsEditorDockActive(editor);
			Require(flag2, "The outer center dock did not activate the real 2D editor tab.");
			Require(GodotObject.IsInstanceValid(viewport), "The real 2D viewport is missing.");
			Require(GodotObject.IsInstanceValid(viewport) && viewport.IsVisibleInTree(), "The 2D canvas dock was not active after loading the scene.");
			Require(GodotObject.IsInstanceValid(target), "The Node2D gizmo target is missing.");
			Require(GodotObject.IsInstanceValid(control), "The Control gizmo target is missing.");
			Require(GodotObject.IsInstanceValid(history), "The shared scene history is missing.");
			if (!flag2 || !GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(target) || !GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(history))
			{
				Finish();
				return;
			}
			_inputViewport = FindAncestorWindow(editor) ?? viewport.GetViewport();
			_inputViewport?.NotifyMouseEntered();
			viewport.GuiInput += OnProbeViewportGuiInput;
			int num = CountCanvasItems(editor.CurrentSceneInstance);
			Require(num == 1000, $"The performance fixture must contain exactly {1000} CanvasItems, found {num}.");
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node inspectorSentinel = new Node
			{
				Name = "InspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			int historyId = editor.GetCurrentSceneHistoryId();
			DisableSnapping(viewport);
			SelectOnly(target, sceneDock);
			viewport.CenterAt(viewport.GetCanvasItemWorldOrigin(target));
			await WaitFrames(3);
			string[] scaleHandles = new string[8] { "ScaleTopLeft", "ScaleTop", "ScaleTopRight", "ScaleRight", "ScaleBottomRight", "ScaleBottom", "ScaleBottomLeft", "ScaleLeft" };
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			await WaitFrames(2);
			await PrintInputDiagnostics(editor, viewport);
			bool flag3 = await VerifyMoveFreePressDispatch(viewport);
			Require(flag3, "MoveFree preflight press did not reach the real 2D viewport.");
			if (!flag3)
			{
				Finish();
				return;
			}
			bool flag4 = await VerifyNonLeftButtonsRejected(viewport);
			_handleDispatch &= flag4;
			Require(flag4, "Right or middle mouse button incorrectly started a transform gizmo drag.");
			SelectOnly(target, sceneDock);
			viewport.CenterAt(viewport.GetCanvasItemWorldOrigin(target));
			await WaitFrames(2);
			Vector2 position;
			bool moveHandles = TryGetHandlePosition(viewport, "MoveFree", out position) && TryGetHandlePosition(viewport, "MoveX", out position) && TryGetHandlePosition(viewport, "MoveY", out position);
			viewport.SetToolMode(XW2DViewport.ToolMode.Rotate);
			await WaitFrames(2);
			bool rotateHandle = TryGetHandlePosition(viewport, "Rotate", out position);
			viewport.SetToolMode(XW2DViewport.ToolMode.Scale);
			await WaitFrames(2);
			bool allScaleHandles = scaleHandles.All((string name) => TryGetHandlePosition(viewport, name, out var _));
			viewport.SetToolMode(XW2DViewport.ToolMode.Pivot);
			await WaitFrames(2);
			bool flag5 = TryGetHandlePosition(viewport, "Pivot", out position);
			bool handles = moveHandles && rotateHandle && allScaleHandles && flag5;
			Require(handles, "The selected Node2D did not expose Move/Rotate/Pivot and all eight Scale handles.");
			ResetHistory(history, historyId);
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			await WaitFrames(2);
			Vector2 freeBefore = target.Position;
			bool flag6 = await DragHandle(viewport, "MoveFree", new Vector2(26f, 18f));
			Vector2 freeAfter = target.Position;
			bool freeApplied = flag6 && Mathf.Abs(freeAfter.X - freeBefore.X) > 4f && Mathf.Abs(freeAfter.Y - freeBefore.Y) > 4f;
			bool moveFree = freeApplied & await VerifySingleHistoryRoundTrip(history, () => target.Position.IsEqualApprox(freeBefore), () => target.Position.IsEqualApprox(freeAfter));
			Require(moveFree, "MoveFree did not move both axes through one undoable real viewport drag.");
			ResetHistory(history, historyId);
			Vector2 xBefore = target.Position;
			bool flag7 = await DragHandle(viewport, "MoveX", new Vector2(31f, 19f));
			Vector2 xAfter = target.Position;
			bool xApplied = flag7 && Mathf.Abs(xAfter.X - xBefore.X) > 4f && Mathf.IsEqualApprox(xAfter.Y, xBefore.Y);
			bool moveX = xApplied & await VerifySingleHistoryRoundTrip(history, () => target.Position.IsEqualApprox(xBefore), () => target.Position.IsEqualApprox(xAfter));
			Require(moveX, "MoveX did not constrain the real viewport drag to the X axis.");
			ResetHistory(history, historyId);
			Vector2 yBefore = target.Position;
			bool flag8 = await DragHandle(viewport, "MoveY", new Vector2(23f, 29f));
			Vector2 yAfter = target.Position;
			bool yApplied = flag8 && Mathf.IsEqualApprox(yAfter.X, yBefore.X) && Mathf.Abs(yAfter.Y - yBefore.Y) > 4f;
			bool moveY = yApplied & await VerifySingleHistoryRoundTrip(history, () => target.Position.IsEqualApprox(yBefore), () => target.Position.IsEqualApprox(yAfter));
			Require(moveY, "MoveY did not constrain the real viewport drag to the Y axis.");
			ResetHistory(history, historyId);
			bool cancelRestore = await VerifyEscapeRestores(viewport, target, history);
			Require(cancelRestore, "Escape did not restore the transform preview without committing history.");
			ResetHistory(history, historyId);
			viewport.SetToolMode(XW2DViewport.ToolMode.Rotate);
			await WaitFrames(2);
			float rotateBefore = target.Rotation;
			Vector2 position2;
			bool num2 = TryGetHandlePosition(viewport, "Rotate", out position2);
			Vector2 vector = TestGetSelectionPivotViewPosition(viewport);
			Vector2 end = vector + (position2 - vector).Rotated(Mathf.DegToRad(42f));
			bool flag9 = num2;
			if (flag9)
			{
				flag9 = await DragHandleTo(viewport, "Rotate", position2, end);
			}
			bool flag10 = flag9;
			float rotateAfter = target.Rotation;
			bool rotateApplied = flag10 && Mathf.Abs(Mathf.AngleDifference(rotateBefore, rotateAfter)) > 0.1f;
			bool rotate = rotateApplied & await VerifySingleHistoryRoundTrip(history, () => Mathf.IsEqualApprox(target.Rotation, rotateBefore), () => Mathf.IsEqualApprox(target.Rotation, rotateAfter));
			Require(rotate, "Rotate ring did not apply one undoable angular drag around the visible pivot.");
			ResetHistory(history, historyId);
			viewport.SetToolMode(XW2DViewport.ToolMode.Scale);
			await WaitFrames(2);
			Vector2 scaleBefore = target.Scale;
			Vector2 position3;
			bool num3 = TryGetHandlePosition(viewport, "ScaleTopLeft", out position3);
			Vector2 vector2 = TestGetSelectionPivotViewPosition(viewport);
			Vector2 end2 = vector2 + (position3 - vector2) * 1.35f;
			flag9 = num3;
			if (flag9)
			{
				flag9 = await DragHandleTo(viewport, "ScaleTopLeft", position3, end2);
			}
			bool flag11 = flag9;
			Vector2 scaleAfter = target.Scale;
			bool scaleApplied = flag11 && Mathf.Abs(scaleAfter.X - scaleBefore.X) > 0.05f && Mathf.Abs(scaleAfter.Y - scaleBefore.Y) > 0.05f;
			bool scaleCorner = scaleApplied & await VerifySingleHistoryRoundTrip(history, () => target.Scale.IsEqualApprox(scaleBefore), () => target.Scale.IsEqualApprox(scaleAfter));
			Require(scaleCorner, "The ScaleTopLeft handle did not scale both axes through one history action.");
			ResetHistory(history, historyId);
			viewport.SetToolMode(XW2DViewport.ToolMode.Pivot);
			await WaitFrames(2);
			bool pivotHadBefore = target.HasMeta("_edit_pivot_");
			Variant pivotBefore = (pivotHadBefore ? target.GetMeta("_edit_pivot_") : Variant.From<Vector2>(Vector2.Zero));
			bool flag12 = await DragHandle(viewport, "Pivot", new Vector2(19f, -14f));
			bool flag13 = target.HasMeta("_edit_pivot_");
			Vector2 pivotAfter = (flag13 ? target.GetMeta("_edit_pivot_").AsVector2() : Vector2.Zero);
			bool pivotApplied = flag12 && flag13 && (!pivotHadBefore || !pivotAfter.IsEqualApprox(pivotBefore.AsVector2()));
			bool pivot = pivotApplied & await VerifySingleHistoryRoundTrip(history, () => target.HasMeta("_edit_pivot_") == pivotHadBefore && (!pivotHadBefore || target.GetMeta("_edit_pivot_").AsVector2().IsEqualApprox(pivotBefore.AsVector2())), () => target.HasMeta("_edit_pivot_") && target.GetMeta("_edit_pivot_").AsVector2().IsEqualApprox(pivotAfter));
			Require(pivot, "The Pivot handle did not persist one undoable custom Node2D pivot.");
			SelectOnly(control, sceneDock);
			viewport.CenterAt(viewport.GetCanvasItemWorldOrigin(control));
			await WaitFrames(3);
			ResetHistory(history, historyId);
			bool num4 = await ActivateFirstAvailableHandle(viewport, "AnchorTopLeft", XW2DViewport.ToolMode.Select, XW2DViewport.ToolMode.Move, XW2DViewport.ToolMode.Pivot);
			Vector4 anchorsBefore = ReadAnchors(control);
			flag9 = num4;
			if (flag9)
			{
				flag9 = await DragHandle(viewport, "AnchorTopLeft", new Vector2(20f, 14f));
			}
			bool flag14 = flag9;
			Vector4 anchorsAfter = ReadAnchors(control);
			bool anchorApplied = flag14 && (!Mathf.IsEqualApprox(anchorsAfter.X, anchorsBefore.X) || !Mathf.IsEqualApprox(anchorsAfter.Y, anchorsBefore.Y));
			bool controlAnchor = anchorApplied & await VerifySingleHistoryRoundTrip(history, () => AnchorsEqual(ReadAnchors(control), anchorsBefore), () => AnchorsEqual(ReadAnchors(control), anchorsAfter));
			Require(controlAnchor, "The Control top-left anchor handle did not edit anchors with one undo action.");
			ResetHistory(history, historyId);
			bool num5 = await ActivateFirstAvailableHandle(viewport, "ControlSizeBottomRight", XW2DViewport.ToolMode.Scale, XW2DViewport.ToolMode.Select);
			Vector2 sizeBefore = control.Size;
			flag9 = num5;
			if (flag9)
			{
				flag9 = await DragHandle(viewport, "ControlSizeBottomRight", new Vector2(27f, 21f));
			}
			bool flag15 = flag9;
			Vector2 sizeAfter = control.Size;
			bool sizeApplied = flag15 && sizeAfter.X > sizeBefore.X + 4f && sizeAfter.Y > sizeBefore.Y + 4f;
			bool controlSize = sizeApplied & await VerifySingleHistoryRoundTrip(history, () => control.Size.IsEqualApprox(sizeBefore), () => control.Size.IsEqualApprox(sizeAfter));
			Require(controlSize, "The Control bottom-right size handle did not resize both axes with one undo action.");
			Vector2 savedPosition = target.Position;
			Vector2 savedScale = target.Scale;
			float savedRotation = target.Rotation;
			bool savedPivot = target.HasMeta("_edit_pivot_");
			Vector2 savedPivotValue = (savedPivot ? target.GetMeta("_edit_pivot_").AsVector2() : Vector2.Zero);
			Vector4 savedAnchors = ReadAnchors(control);
			Vector2 savedSize = control.Size;
			bool saved = editor.SaveCurrentScene();
			await WaitFrames(3);
			Node obj = ResourceLoader.Load<PackedScene>("user://mod_editor_2d_on_canvas_gizmo_probe.tscn", "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
			Node2D node2D = obj?.FindChild("GizmoNode2D", recursive: true, owned: false) as Node2D;
			Control control2 = obj?.FindChild("GizmoControl", recursive: true, owned: false) as Control;
			bool cacheReload = saved && GodotObject.IsInstanceValid(node2D) && GodotObject.IsInstanceValid(control2) && node2D.Position.IsEqualApprox(savedPosition) && node2D.Scale.IsEqualApprox(savedScale) && Mathf.IsEqualApprox(node2D.Rotation, savedRotation) && node2D.HasMeta("_edit_pivot_") == savedPivot && (!savedPivot || node2D.GetMeta("_edit_pivot_").AsVector2().IsEqualApprox(savedPivotValue)) && AnchorsEqual(ReadAnchors(control2), savedAnchors) && control2.Size.IsEqualApprox(savedSize);
			obj?.Free();
			Require(cacheReload, "Gizmo edits did not survive uncached scene save/reload.");
			SelectOnly(target, sceneDock);
			viewport.CenterAt(viewport.GetCanvasItemWorldOrigin(target));
			viewport.SetToolMode(XW2DViewport.ToolMode.Move);
			await WaitFrames(3);
			var (p95Ms, perf1k) = MeasureWorstOrderHitDragP95(viewport, target, history, historyId);
			Require(perf1k, $"1000-CanvasItem hit/drag P95 exceeded {16.0:0.0} ms or missed the target; P95={p95Ms:0.###} ms.");
			bool inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorSentinel && inspector.CurrentObject != target && inspector.CurrentObject != control;
			Require(inspectorUntouched, "On-canvas gizmo editing redirected the raw Inspector.");
			Vector2 hiddenBefore = target.Position;
			Vector2 hiddenStart;
			bool hiddenHandle = TryGetHandlePosition(viewport, "MoveFree", out hiddenStart);
			editor.Hide();
			await WaitFrames(4);
			if (hiddenHandle)
			{
				PushMouseDrag(viewport, hiddenStart, hiddenStart + new Vector2(40f, 28f));
			}
			await WaitFrames(3);
			Node node = viewport.FindChild("Overlay", recursive: true, owned: false);
			bool flag16 = !editor.IsVisibleInTree() && !viewport.IsProcessing() && (!GodotObject.IsInstanceValid(node) || !node.IsProcessing()) && target.Position.IsEqualApprox(hiddenBefore);
			Require(flag16, "The hidden 2D editor continued idle processing or accepted a gizmo drag.");
			GD.Print($"[MOD_EDITOR_2D_ON_CANVAS_GIZMO_PROBE] window={window} handles={handles} handleDispatch={_handleDispatch} moveFree={moveFree} moveX={moveX} moveY={moveY} rotate={rotate} scaleCorner={scaleCorner} pivot={pivot} controlAnchor={controlAnchor} controlSize={controlSize} cancelRestore={cancelRestore} singleHistory={_singleHistory} undoRedo={_undoRedo} cacheReload={cacheReload} inspectorUntouched={inspectorUntouched} hiddenIdle={flag16} perf1k={perf1k} p95Ms={p95Ms:0.###} failures={_failures.Count} elapsedMs={_elapsed.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
			GD.PrintErr(ex.ToString());
		}
		Finish();
	}

	private static bool SaveFixtureScene()
	{
		Node2D node2D = new Node2D
		{
			Name = "OnCanvasGizmoRoot"
		};
		Node2D node2D2 = new Node2D
		{
			Name = "GizmoNode2D",
			Position = new Vector2(240f, 180f)
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		for (int i = 0; i < 996; i++)
		{
			Node2D node2D3 = new Node2D
			{
				Name = $"PerfCanvasItem{i:0000}",
				Position = new Vector2(1000 + i % 40 * 32, 900 + i / 40 * 32)
			};
			node2D.AddChild(node2D3, forceReadableName: false, InternalMode.Disabled);
			node2D3.Owner = node2D;
		}
		Control control = new Control
		{
			Name = "GizmoControlParent",
			Position = new Vector2(560f, 220f),
			Size = new Vector2(640f, 420f)
		};
		ColorRect colorRect = new ColorRect
		{
			Name = "GizmoControl",
			Position = new Vector2(120f, 90f),
			Size = new Vector2(112f, 84f),
			PivotOffset = new Vector2(56f, 42f),
			Color = new Color(0.22f, 0.68f, 0.94f, 0.9f)
		};
		node2D.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		control.Owner = node2D;
		control.AddChild(colorRect, forceReadableName: false, InternalMode.Disabled);
		colorRect.Owner = node2D;
		if (CountCanvasItems(node2D) != 1000)
		{
			node2D.Free();
			return false;
		}
		PackedScene packedScene = new PackedScene
		{
			ResourceName = "OnCanvasGizmoProbe"
		};
		Error error = packedScene.Pack(node2D);
		if (error == Error.Ok)
		{
			error = ResourceSaver.Save(packedScene, "user://mod_editor_2d_on_canvas_gizmo_probe.tscn", ResourceSaver.SaverFlags.None);
		}
		node2D.Free();
		return error == Error.Ok;
	}

	private static int CountCanvasItems(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return 0;
		}
		int num = ((root is CanvasItem) ? 1 : 0);
		foreach (Node child in root.GetChildren())
		{
			num += CountCanvasItems(child);
		}
		return num;
	}

	private static void DisableSnapping(XW2DViewport viewport)
	{
		viewport.GridSnapActive = false;
		viewport.SmartSnapActive = false;
		viewport.MoveSnapStep = Vector2.Zero;
		viewport.RotationSnapStepDegrees = 0f;
		viewport.ScaleSnapStep = 0f;
	}

	private static void ResetHistory(XWUndoRedoManager history, int historyId)
	{
		history.ClearHistory(historyId);
		history.SetCurrentHistoryType(historyId);
	}

	private async Task<bool> VerifySingleHistoryRoundTrip(XWUndoRedoManager history, Func<bool> isBefore, Func<bool> isAfter)
	{
		bool hasOneAction = history.HasUndo();
		bool undoCalled = history.Undo();
		await WaitFrames(2);
		bool undo = undoCalled && isBefore();
		bool noSecondAction = !history.HasUndo();
		bool redoCalled = history.Redo();
		await WaitFrames(2);
		bool flag = redoCalled && isAfter();
		_singleHistory &= hasOneAction && noSecondAction;
		_undoRedo &= undo && flag;
		return hasOneAction && noSecondAction && undo && flag;
	}

	private async Task<bool> ActivateFirstAvailableHandle(XW2DViewport viewport, string handleName, params XW2DViewport.ToolMode[] modes)
	{
		foreach (XW2DViewport.ToolMode toolMode in modes)
		{
			viewport.SetToolMode(toolMode);
			await WaitFrames(2);
			if (TryGetHandlePosition(viewport, handleName, out var position) && TestTryHitGizmo(viewport, position, handleName))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<bool> DragHandle(XW2DViewport viewport, string handleName, Vector2 delta)
	{
		if (!TryGetHandlePosition(viewport, handleName, out var position))
		{
			return false;
		}
		return await DragHandleTo(viewport, handleName, position, position + delta);
	}

	private async Task<bool> VerifyNonLeftButtonsRejected(XW2DViewport viewport)
	{
		if (!TryGetHandlePosition(viewport, "MoveFree", out var position))
		{
			return false;
		}
		Vector2 point = ToViewportPoint(viewport, position);
		MouseButton[] array = new MouseButton[2]
		{
			MouseButton.Right,
			MouseButton.Middle
		};
		foreach (MouseButton button in array)
		{
			_inputViewport.PushInput(new InputEventMouseButton
			{
				Position = point,
				GlobalPosition = point,
				ButtonIndex = button,
				Pressed = true
			}, inLocalCoords: true);
			await WaitFrames(1);
			if (CurrentHandleName(viewport) != "None")
			{
				return false;
			}
			_inputViewport.PushInput(new InputEventMouseButton
			{
				Position = point,
				GlobalPosition = point,
				ButtonIndex = button,
				Pressed = false
			}, inLocalCoords: true);
			await WaitFrames(1);
			if (button == MouseButton.Right)
			{
				_inputViewport.PushInput(new InputEventKey
				{
					Keycode = Key.Escape,
					PhysicalKeycode = Key.Escape,
					Pressed = true
				}, inLocalCoords: true);
				await WaitFrames(1);
			}
		}
		return true;
	}

	private async Task<bool> VerifyMoveFreePressDispatch(XW2DViewport viewport)
	{
		if (!TryGetHandlePosition(viewport, "MoveFree", out var position))
		{
			return false;
		}
		Vector2 point = ToViewportPoint(viewport, position);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = point,
			GlobalPosition = point,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		}, inLocalCoords: true);
		await WaitFrames(1);
		bool dispatched = CurrentHandleName(viewport) == "MoveFree";
		if (!dispatched)
		{
			GD.Print($"[MOD_EDITOR_2D_GIZMO_PREFLIGHT] guiInputCount={_viewportGuiInputCount} lastType={_lastViewportGuiEventType} lastPosition={_lastViewportGuiPosition} lastGlobal={_lastViewportGuiGlobalPosition} inputHandled={_inputViewport.IsInputHandled()} hovered={DescribeControl(_inputViewport.GuiGetHoveredControl())}");
		}
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = Key.Escape,
			PhysicalKeycode = Key.Escape,
			Pressed = true
		}, inLocalCoords: true);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = point,
			GlobalPosition = point,
			ButtonIndex = MouseButton.Left,
			Pressed = false
		}, inLocalCoords: true);
		await WaitFrames(1);
		return dispatched && CurrentHandleName(viewport) == "None";
	}

	private async Task PrintInputDiagnostics(XW2DSceneEditor editor, XW2DViewport viewport)
	{
		TryGetHandlePosition(viewport, "MoveFree", out var handleLocal);
		Vector2 handleGlobal = ToViewportPoint(viewport, handleLocal);
		_inputViewport.PushInput(new InputEventMouseMotion
		{
			Position = handleGlobal,
			GlobalPosition = handleGlobal
		}, inLocalCoords: true);
		await WaitFrames(2);
		Control control = _inputViewport.GuiGetHoveredControl();
		TabContainer nodeOrNull = editor.GetNodeOrNull<TabContainer>("%WorkspaceTabs");
		Node instance = FindAncestorWindow(editor)?.FindChild("LoadingOverlay", recursive: true, owned: false);
		Vector2 value = viewport.GetGlobalTransformWithCanvas().AffineInverse() * handleGlobal;
		if (!viewport.IsVisibleInTree() || !GodotObject.IsInstanceValid(control))
		{
			GD.Print($"[MOD_EDITOR_2D_GIZMO_INPUT_DIAGNOSTIC] local={handleLocal} global={handleGlobal} roundTrip={value} globalRect={viewport.GetGlobalRect()} size={viewport.Size} visible={viewport.IsVisibleInTree()} mouseFilter={viewport.MouseFilter} clip={viewport.ClipContents} hovered={DescribeControl(control)} hoveredAncestors={DescribeAncestors(control)} workspaceTab={(GodotObject.IsInstanceValid(nodeOrNull) ? nodeOrNull.CurrentTab : (-1))} tabChain={DescribeTabChain(viewport)} loadingValid={GodotObject.IsInstanceValid(instance)}");
		}
	}

	private static string DescribeControl(Control control)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return "<null>";
		}
		return $"{control.GetPath()}[{control.GetType().Name},visible={control.IsVisibleInTree()},mouse={control.MouseFilter}]";
	}

	private static string DescribeAncestors(Node node)
	{
		List<string> list = new List<string>();
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			list.Add($"{node2.Name}:{node2.GetType().Name}");
			if (list.Count >= 12)
			{
				break;
			}
			node2 = node2.GetParent();
		}
		return string.Join(" <- ", list);
	}

	private static string DescribeTabChain(Node node)
	{
		List<string> list = new List<string>();
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 is TabContainer tabContainer)
			{
				list.Add($"{tabContainer.Name}={tabContainer.CurrentTab}/{tabContainer.GetTabCount()}");
			}
			node2 = node2.GetParent();
		}
		return string.Join(",", list);
	}

	private void OnProbeViewportGuiInput(InputEvent inputEvent)
	{
		_viewportGuiInputCount++;
		_lastViewportGuiEventType = inputEvent?.GetType().Name ?? "<null>";
		if (inputEvent is InputEventMouse inputEventMouse)
		{
			_lastViewportGuiPosition = inputEventMouse.Position;
			_lastViewportGuiGlobalPosition = inputEventMouse.GlobalPosition;
		}
	}

	private async Task<bool> VerifyEscapeRestores(XW2DViewport viewport, Node2D target, XWUndoRedoManager history)
	{
		if (!TryGetHandlePosition(viewport, "MoveFree", out var position))
		{
			return false;
		}
		Vector2 before = target.Position;
		Vector2 startGlobal = ToViewportPoint(viewport, position);
		Vector2 endGlobal = ToViewportPoint(viewport, position + new Vector2(24f, 17f));
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = startGlobal,
			GlobalPosition = startGlobal,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		}, inLocalCoords: true);
		await WaitFrames(1);
		_inputViewport.PushInput(new InputEventMouseMotion
		{
			Position = endGlobal,
			GlobalPosition = endGlobal,
			Relative = endGlobal - startGlobal,
			ButtonMask = MouseButtonMask.Left
		}, inLocalCoords: true);
		await WaitFrames(1);
		bool previewChanged = !target.Position.IsEqualApprox(before);
		_inputViewport.PushInput(new InputEventKey
		{
			Keycode = Key.Escape,
			PhysicalKeycode = Key.Escape,
			Pressed = true
		}, inLocalCoords: true);
		await WaitFrames(2);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = endGlobal,
			GlobalPosition = endGlobal,
			ButtonIndex = MouseButton.Left,
			Pressed = false
		}, inLocalCoords: true);
		return previewChanged && target.Position.IsEqualApprox(before) && !history.HasUndo() && CurrentHandleName(viewport) == "None";
	}

	private async Task<bool> DragHandleTo(XW2DViewport viewport, string handleName, Vector2 start, Vector2 end)
	{
		bool hit = TestTryHitGizmo(viewport, start, handleName);
		_handleDispatch &= hit;
		Vector2 startGlobal = ToViewportPoint(viewport, start);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = startGlobal,
			GlobalPosition = startGlobal,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		}, inLocalCoords: true);
		await WaitFrames(1);
		bool current = CurrentHandleName(viewport) == handleName;
		_handleDispatch &= current;
		Vector2 endGlobal = ToViewportPoint(viewport, end);
		_inputViewport.PushInput(new InputEventMouseMotion
		{
			Position = endGlobal,
			GlobalPosition = endGlobal,
			Relative = endGlobal - startGlobal,
			ButtonMask = MouseButtonMask.Left
		}, inLocalCoords: true);
		await WaitFrames(1);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = endGlobal,
			GlobalPosition = endGlobal,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		}, inLocalCoords: true);
		await WaitFrames(2);
		return hit && current;
	}

	private void PushMouseDrag(XW2DViewport viewport, Vector2 start, Vector2 end)
	{
		Vector2 vector = ToViewportPoint(viewport, start);
		Vector2 vector2 = ToViewportPoint(viewport, end);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = vector,
			GlobalPosition = vector,
			ButtonIndex = MouseButton.Left,
			ButtonMask = MouseButtonMask.Left,
			Pressed = true
		}, inLocalCoords: true);
		_inputViewport.PushInput(new InputEventMouseMotion
		{
			Position = vector2,
			GlobalPosition = vector2,
			Relative = vector2 - vector,
			ButtonMask = MouseButtonMask.Left
		}, inLocalCoords: true);
		_inputViewport.PushInput(new InputEventMouseButton
		{
			Position = vector2,
			GlobalPosition = vector2,
			ButtonIndex = MouseButton.Left,
			ButtonMask = (MouseButtonMask)0L,
			Pressed = false
		}, inLocalCoords: true);
	}

	private (double P95Milliseconds, bool Passed) MeasureWorstOrderHitDragP95(XW2DViewport viewport, Node2D expectedTarget, XWUndoRedoManager history, int historyId)
	{
		List<double> list = new List<double>(101);
		bool flag = true;
		bool flag2 = true;
		ResetHistory(history, historyId);
		for (int i = 0; i < 101; i++)
		{
			if (!TryGetHandlePosition(viewport, "MoveFree", out var position))
			{
				flag = false;
				break;
			}
			Vector2 position2 = expectedTarget.Position;
			long timestamp = Stopwatch.GetTimestamp();
			CanvasItem canvasItem = TestFindCanvasItemAt(viewport, viewport.Size * 0.5f);
			bool flag3 = TestTryHitGizmo(viewport, position, "MoveFree");
			PushMouseDrag(viewport, position, position + new Vector2(9f, 7f));
			bool flag4 = !expectedTarget.Position.IsEqualApprox(position2);
			bool flag5 = history.HasUndo();
			bool flag6 = flag5 && history.Undo() && expectedTarget.Position.IsEqualApprox(position2);
			long timestamp2 = Stopwatch.GetTimestamp();
			list.Add((double)(timestamp2 - timestamp) * 1000.0 / (double)Stopwatch.Frequency);
			flag = flag && canvasItem == expectedTarget && flag3;
			flag2 &= flag4 && flag5 && flag6 && !history.HasUndo();
		}
		double num = Percentile(list, 0.95);
		return (P95Milliseconds: num, Passed: flag && flag2 && list.Count == 101 && num <= 16.0);
	}

	private static double Percentile(List<double> samples, double percentile)
	{
		if (samples.Count == 0)
		{
			return 1.0 / 0.0;
		}
		samples.Sort();
		int index = Math.Clamp((int)Math.Ceiling((double)samples.Count * percentile) - 1, 0, samples.Count - 1);
		return samples[index];
	}

	private static bool TryGetHandlePosition(XW2DViewport viewport, string handleName, out Vector2 position)
	{
		position = Vector2.Zero;
		Type nestedType = typeof(XW2DViewport).GetNestedType("GizmoHandleType", BindingFlags.Public);
		MethodInfo method = typeof(XW2DViewport).GetMethod("TryGetGizmoHandleViewPosition", BindingFlags.Instance | BindingFlags.Public);
		if (nestedType == null || method == null)
		{
			return false;
		}
		object obj;
		try
		{
			obj = Enum.Parse(nestedType, handleName, ignoreCase: false);
		}
		catch (ArgumentException)
		{
			return false;
		}
		object[] array = new object[2]
		{
			obj,
			Vector2.Zero
		};
		object obj2 = method.Invoke(viewport, array);
		if (!(obj2 is bool) || !(bool)obj2)
		{
			return false;
		}
		position = (Vector2)array[1];
		return position.IsFinite();
	}

	private static bool TestTryHitGizmo(XW2DViewport viewport, Vector2 viewPosition, string expectedHandleName)
	{
		MethodInfo method = typeof(XW2DViewport).GetMethod("TestTryHitGizmo", BindingFlags.Instance | BindingFlags.Public);
		Type nestedType = typeof(XW2DViewport).GetNestedType("GizmoHandleType", BindingFlags.Public);
		if (method == null || nestedType == null)
		{
			return false;
		}
		object value = Enum.GetValues(nestedType).GetValue(0);
		object[] array = new object[2] { viewPosition, value };
		object obj = method.Invoke(viewport, array);
		bool flag = default(bool);
		int num;
		if (obj is bool)
		{
			flag = (bool)obj;
			num = 1;
		}
		else
		{
			num = 0;
		}
		if (((uint)num & (flag ? 1u : 0u)) != 0)
		{
			return string.Equals(array[1]?.ToString(), expectedHandleName, StringComparison.Ordinal);
		}
		return false;
	}

	private static CanvasItem TestFindCanvasItemAt(XW2DViewport viewport, Vector2 viewPosition)
	{
		return typeof(XW2DViewport).GetMethod("TestFindCanvasItemAt", BindingFlags.Instance | BindingFlags.Public)?.Invoke(viewport, new object[1] { viewPosition }) as CanvasItem;
	}

	private static Vector2 TestGetSelectionPivotViewPosition(XW2DViewport viewport)
	{
		object obj = typeof(XW2DViewport).GetMethod("TestGetSelectionPivotViewPosition", BindingFlags.Instance | BindingFlags.Public)?.Invoke(viewport, Array.Empty<object>());
		if (obj is Vector2)
		{
			return (Vector2)obj;
		}
		return Vector2.Zero;
	}

	private static string CurrentHandleName(XW2DViewport viewport)
	{
		return typeof(XW2DViewport).GetProperty("CurrentGizmoHandleType", BindingFlags.Instance | BindingFlags.Public)?.GetValue(viewport)?.ToString() ?? string.Empty;
	}

	private static Vector2 ToViewportPoint(Control control, Vector2 localPoint)
	{
		return control.GetGlobalTransformWithCanvas() * localPoint;
	}

	private static Vector4 ReadAnchors(Control control)
	{
		return new Vector4(control.AnchorLeft, control.AnchorTop, control.AnchorRight, control.AnchorBottom);
	}

	private static bool AnchorsEqual(Vector4 left, Vector4 right)
	{
		if (Mathf.IsEqualApprox(left.X, right.X) && Mathf.IsEqualApprox(left.Y, right.Y) && Mathf.IsEqualApprox(left.Z, right.Z))
		{
			return Mathf.IsEqualApprox(left.W, right.W);
		}
		return false;
	}

	private static void SelectOnly(Node node, XWSceneTreeDock sceneDock)
	{
		XWEditorInterface.Instance?.ClearSelection();
		XWEditorInterface.Instance?.SelectNode(node);
		sceneDock?.SelectNode(node, emitSignal: false);
	}

	private static void ActivateEditorDock(Control editor)
	{
		XWEditorInterface.Instance?.FocusPanel("2d_editor");
	}

	private static bool IsEditorDockActive(Control editor)
	{
		Node node = editor;
		while (GodotObject.IsInstanceValid(node))
		{
			if (node.GetParent() is TabContainer tabContainer && node is Control control)
			{
				int num = -1;
				for (int i = 0; i < tabContainer.GetTabCount(); i++)
				{
					if (tabContainer.GetTabControl(i) == control)
					{
						num = i;
						break;
					}
				}
				if (num < 0 || tabContainer.CurrentTab != num)
				{
					return false;
				}
			}
			node = node.GetParent();
		}
		return true;
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

	private async Task<bool> EnterEditorSurface(XW2DSceneEditor editor, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Control instance = obj?.GetNodeOrNull<Control>("%LoadingOverlay");
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance?.FocusPanel("2d_editor");
				await WaitFrames(3);
				return editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForModEditorReady(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Control instance = obj?.GetNodeOrNull<Control>("%LoadingOverlay");
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance))
			{
				return true;
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
			GD.PrintErr("[MOD_EDITOR_2D_ON_CANVAS_GIZMO_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (GodotObject.IsInstanceValid(_inputViewport))
		{
			_inputViewport.NotifyMouseExited();
		}
		_inputViewport = null;
		if (FileAccess.FileExists("user://mod_editor_2d_on_canvas_gizmo_probe.tscn"))
		{
			DirAccess.RemoveAbsolute(ProjectSettings.GlobalizePath("user://mod_editor_2d_on_canvas_gizmo_probe.tscn"));
		}
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_2D_ON_CANVAS_GIZMO_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
