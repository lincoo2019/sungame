using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.SceneEditor;

public partial class ModEditor2DAnimationTimelineRuntimeProbe : Node
{
	private const string SceneAPath = "user://mod_editor_2d_animation_a.tscn";

	private const string SceneBPath = "user://mod_editor_2d_animation_b.tscn";

	private const string SeedAnimation = "安全预览样例";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		bool f3 = false;
		bool window = false;
		bool sceneLoaded = false;
		bool treeLinked = false;
		bool animationCreate = false;
		bool valueTrack = false;
		bool methodVisible = false;
		bool safePreview = false;
		bool modeControls = false;
		bool interpolationSwitch = false;
		bool updateModeSwitch = false;
		bool loopWrapSwitch = false;
		bool preciseTransitionInput = false;
		bool curveDrag = false;
		bool midpointDifference = false;
		bool modeUndoRedo = false;
		bool keyAdd = false;
		bool keyMove = false;
		bool keyDelete = false;
		bool undoRedo = false;
		bool savedReload = false;
		bool saveSettingsReload = false;
		bool focusedSaveCommit = false;
		bool hiddenStopped = false;
		bool hiddenDragRollback = false;
		bool tabIsolation = false;
		bool previewCleanup = false;
		bool inspectorUntouched = false;
		float linearMidX = 0f / 0f;
		float nearestMidX = 0f / 0f;
		float discreteMidX = 0f / 0f;
		float easedMidX = 0f / 0f;
		float draggedTransition = 0f / 0f;
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager 无法实例化。");
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
			f3 = true;
			XW2DSceneEditor editor = await WaitForSceneEditor(900);
			window = FindAncestorWindow(editor) != null;
			Require(window, "2D 编辑器未挂载到 F3 ModEditor 窗口。");
			if (!GodotObject.IsInstanceValid(editor))
			{
				Finish();
				return;
			}
			Require(CreateFixture("user://mod_editor_2d_animation_a.tscn", "动画场景A", new Vector2(12f, 18f)), "无法创建动画场景 A。");
			Require(CreateFixture("user://mod_editor_2d_animation_b.tscn", "动画场景B", new Vector2(27f, 33f)), "无法创建动画场景 B。");
			XWInspector inspector = FindNodeOfType<XWInspector>(GetTree().Root);
			Node inspectorSentinel = new Node
			{
				Name = "AnimationTimelineInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance?.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_animation_a.tscn");
			await WaitFrames(10);
			Button timelineButton = editor.FindChild("AnimationTimelineButton", recursive: true, owned: false) as Button;
			timelineButton?.SetPressedNoSignal(pressed: true);
			timelineButton?.EmitSignal(BaseButton.SignalName.Toggled, true);
			await WaitFrames(4);
			XW2DAnimationTimelinePanel panel = FindNodeOfType<XW2DAnimationTimelinePanel>(editor);
			Node2D targetA = editor.CurrentSceneInstance?.FindChild("Target", recursive: true, owned: false) as Node2D;
			AnimationPlayer sourcePlayerA = editor.CurrentSceneInstance?.FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer;
			sceneLoaded = GodotObject.IsInstanceValid(panel) && panel.Visible && GodotObject.IsInstanceValid(targetA) && GodotObject.IsInstanceValid(sourcePlayerA) && panel.SelectAnimation("AnimationPlayer", "", "安全预览样例");
			Require(sceneLoaded, "真实 2D 动画工作台未绑定场景内 AnimationPlayer。");
			if (!sceneLoaded)
			{
				Finish();
				return;
			}
			treeLinked = DescendantLabelContains(panel, "AnimationTree");
			Require(treeLinked, "动画工作台未显示 AnimationTree 关联。");
			Animation currentAnimation = panel.CurrentAnimation;
			methodVisible = GodotObject.IsInstanceValid(currentAnimation) && currentAnimation.GetTrackCount() == 2 && currentAnimation.TrackGetType(1) == Animation.TrackType.Method && panel.PreviewUnsafeTrackCount == 1;
			Require(methodVisible, "方法轨没有显示，或安全预览未识别不安全轨。");
			Vector2 baselineA = targetA.Position;
			panel.SeekPreview(0.5);
			await WaitFrames(3);
			bool previewApplied = targetA.Position.DistanceTo(baselineA) > 0.5f;
			panel.StopPreview();
			await WaitFrames(2);
			safePreview = previewApplied && targetA.Position.IsEqualApprox(baselineA) && !sourcePlayerA.IsPlaying() && panel.PreviewUnsafeTrackCount == 1;
			Require(safePreview, "安全预览没有应用/恢复属性基线，或启动了原 AnimationPlayer。");
			animationCreate = panel.CreateAnimation("测试动画") && panel.CurrentAnimationName == "测试动画" && GodotObject.IsInstanceValid(panel.CurrentAnimation);
			Require(animationCreate, "无法通过工作台创建本地动画片段。");
			valueTrack = panel.AddValueTrack(targetA, "position") && panel.TrackCount == 1 && panel.CurrentAnimation.TrackGetType(0) == Animation.TrackType.Value && panel.CurrentAnimation.TrackGetPath(0).ToString().EndsWith(":position");
			Require(valueTrack, "无法通过属性图库路径创建 Value 轨。");
			OptionButton interpolationOption = panel.FindChild("TrackInterpolationOption", recursive: true, owned: false) as OptionButton;
			OptionButton updateModeOption = panel.FindChild("ValueUpdateModeOption", recursive: true, owned: false) as OptionButton;
			CheckButton loopWrapCheck = panel.FindChild("InterpolationLoopWrapCheck", recursive: true, owned: false) as CheckButton;
			XW2DAnimationEaseCurveCanvas curveCanvas = panel.FindChild("KeyTransitionCurve", recursive: true, owned: false) as XW2DAnimationEaseCurveCanvas;
			SpinBox transitionSpin = panel.FindChild("KeyTransitionSpin", recursive: true, owned: false) as SpinBox;
			ScrollContainer workbenchScroll = panel.FindChild("AnimationWorkbenchScroll", recursive: true, owned: false) as ScrollContainer;
			VSplitContainer canvasTimelineSplit = panel.GetParent() as VSplitContainer;
			Control mainVBox = canvasTimelineSplit?.GetParent() as Control;
			Control workbenchRoot = panel.FindChild("AnimationWorkbench", recursive: true, owned: false) as Control;
			Control motionCard = panel.FindChild("MotionCurveCard", recursive: true, owned: false) as Control;
			await WaitFrames(6);
			workbenchScroll?.EnsureControlVisible(curveCanvas);
			await WaitFrames(6);
			bool flag = GodotObject.IsInstanceValid(workbenchScroll) && GodotObject.IsInstanceValid(curveCanvas) && workbenchScroll.GetGlobalRect().Intersects(curveCanvas.GetGlobalRect());
			modeControls = GodotObject.IsInstanceValid(interpolationOption) && GodotObject.IsInstanceValid(updateModeOption) && GodotObject.IsInstanceValid(loopWrapCheck) && GodotObject.IsInstanceValid(curveCanvas) && GodotObject.IsInstanceValid(transitionSpin) && flag && DescendantLabelContains(panel, "运动曲线") && inspector.CurrentObject == inspectorSentinel;
			Require(modeControls, $"运动曲线的图标分段、循环衔接或拖拽画布没有挂载到真实 F3 面板。 scroll={GodotObject.IsInstanceValid(workbenchScroll)} reachable={flag} scrollRect={workbenchScroll?.GetGlobalRect()} curveRect={curveCanvas?.GetGlobalRect()} panelRect={panel.GetGlobalRect()} splitRect={canvasTimelineSplit?.GetGlobalRect()} mainRect={mainVBox?.GetGlobalRect()} editorRect={editor.GetGlobalRect()} rootRect={workbenchRoot?.GetGlobalRect()} rootMin={workbenchRoot?.GetCombinedMinimumSize()} cardRect={motionCard?.GetGlobalRect()} curveMin={curveCanvas?.GetCombinedMinimumSize()} scrollRange={workbenchScroll?.GetVScrollBar()?.MaxValue}/{workbenchScroll?.GetVScrollBar()?.Page} inspector={inspector.CurrentObject == inspectorSentinel}");
			panel.SeekPreview(0.0);
			bool flag2 = panel.InsertCurrentKey(Variant.From(in baselineA));
			panel.SeekPreview(1.0);
			bool flag3 = panel.InsertCurrentKey(Variant.From<Vector2>(baselineA + new Vector2(80f, 40f)));
			Require(flag2 && flag3 && panel.SelectedTrackKeyCount == 2, "运动曲线测试无法建立两个端点关键帧。");
			XWUndoRedoManager history = XWEditorInterface.Instance?.GetUndoRedoManager();
			SelectOptionById(interpolationOption, 1);
			SelectOptionById(updateModeOption, 0);
			await WaitFrames(3);
			Vector2 linearMid = await PreviewPosition(panel, targetA, 0.5);
			linearMidX = linearMid.X;
			SelectOptionById(interpolationOption, 0);
			await WaitFrames(3);
			Vector2 vector = await PreviewPosition(panel, targetA, 0.5);
			nearestMidX = vector.X;
			interpolationSwitch = panel.SelectedTrackInterpolation == Animation.InterpolationType.Nearest && vector.DistanceTo(linearMid) > 20f;
			bool undoInterpolation = history?.Undo() ?? false;
			await WaitFrames(3);
			bool interpolationUndoValue = panel.SelectedTrackInterpolation == Animation.InterpolationType.Linear;
			bool redoInterpolation = history?.Redo() ?? false;
			await WaitFrames(3);
			bool interpolationRedoValue = panel.SelectedTrackInterpolation == Animation.InterpolationType.Nearest;
			Require(interpolationSwitch, "插值图标没有改变真实中点预览。");
			SelectOptionById(interpolationOption, 1);
			SelectOptionById(updateModeOption, 1);
			await WaitFrames(3);
			Vector2 vector2 = await PreviewPosition(panel, targetA, 0.5);
			discreteMidX = vector2.X;
			updateModeSwitch = panel.SelectedValueUpdateMode == Animation.UpdateMode.Discrete && vector2.DistanceTo(linearMid) > 20f;
			bool undoUpdateMode = history?.Undo() ?? false;
			await WaitFrames(3);
			bool updateUndoValue = panel.SelectedValueUpdateMode == Animation.UpdateMode.Continuous;
			bool redoUpdateMode = history?.Redo() ?? false;
			await WaitFrames(3);
			bool updateRedoValue = panel.SelectedValueUpdateMode == Animation.UpdateMode.Discrete;
			Require(updateModeSwitch, "Value 更新方式没有在连续与离散中点间产生真实差异。");
			SelectOptionById(updateModeOption, 0);
			loopWrapCheck.SetPressedNoSignal(pressed: false);
			loopWrapCheck.EmitSignal(BaseButton.SignalName.Toggled, false);
			await WaitFrames(3);
			loopWrapSwitch = !panel.SelectedTrackInterpolationLoopWrap;
			Require(loopWrapSwitch, "循环衔接图形开关没有写入轨道设置。");
			bool selectedEaseKey = panel.SelectKey(0, 0.0);
			await WaitFrames(2);
			float transitionBeforeSpin = panel.SelectedKeyTransition;
			LineEdit transitionLineEdit = transitionSpin?.GetLineEdit();
			transitionLineEdit?.EmitSignal(Control.SignalName.FocusEntered);
			if (GodotObject.IsInstanceValid(transitionSpin))
			{
				transitionSpin.Value = 2.0;
			}
			bool spinPreviewOnly = Math.Abs(panel.SelectedKeyTransition - transitionBeforeSpin) <= 0.001f;
			transitionLineEdit?.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(3);
			bool spinCommitted = Math.Abs(panel.SelectedKeyTransition - 2f) <= 0.001f;
			bool undoSpin = history?.Undo() ?? false;
			await WaitFrames(3);
			bool spinUndoValue = Math.Abs(panel.SelectedKeyTransition - transitionBeforeSpin) <= 0.001f;
			bool redoSpin = history?.Redo() ?? false;
			await WaitFrames(3);
			bool flag4 = Math.Abs(panel.SelectedKeyTransition - 2f) <= 0.001f;
			preciseTransitionInput = selectedEaseKey && spinPreviewOnly && spinCommitted && undoSpin && spinUndoValue && redoSpin && flag4;
			Require(preciseTransitionInput, "精确曲率输入没有在失焦时只提交一次，或无法单步撤销/重做。");
			float transitionBeforeDrag = panel.SelectedKeyTransition;
			Vector2 vector3 = curveCanvas.Size * new Vector2(0.62f, 0.62f);
			curveCanvas._GuiInput(new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = true,
				Position = vector3
			});
			curveCanvas._GuiInput(new InputEventMouseMotion
			{
				Position = vector3 + new Vector2(0f, -24f),
				Relative = new Vector2(0f, -24f)
			});
			curveCanvas._GuiInput(new InputEventMouseMotion
			{
				Position = vector3 + new Vector2(0f, -42f),
				Relative = new Vector2(0f, -18f)
			});
			curveCanvas._GuiInput(new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = false,
				Position = vector3 + new Vector2(0f, -42f)
			});
			await WaitFrames(4);
			float transitionAfterDrag = panel.SelectedKeyTransition;
			draggedTransition = transitionAfterDrag;
			Vector2 vector4 = await PreviewPosition(panel, targetA, 0.5);
			easedMidX = vector4.X;
			curveDrag = selectedEaseKey && transitionAfterDrag > transitionBeforeDrag + 0.2f && panel.SelectedTrack == 0 && Math.Abs(panel.SelectedKeyTime) <= 0.0001;
			midpointDifference = vector4.DistanceTo(linearMid) > 8f && vector4.DistanceTo(panel.CurrentAnimation.ValueTrackInterpolate(0, 0.5).AsVector2()) <= 1f;
			bool undoCurve = history?.Undo() ?? false;
			await WaitFrames(3);
			bool curveUndoValue = Math.Abs(panel.SelectedKeyTransition - transitionBeforeDrag) <= 0.001f;
			bool redoCurve = history?.Redo() ?? false;
			await WaitFrames(3);
			bool flag5 = Math.Abs(panel.SelectedKeyTransition - transitionAfterDrag) <= 0.001f;
			modeUndoRedo = undoInterpolation && interpolationUndoValue && redoInterpolation && interpolationRedoValue && undoUpdateMode && updateUndoValue && redoUpdateMode && updateRedoValue && undoCurve && curveUndoValue && redoCurve && flag5;
			Require(curveDrag, "缓动曲线真实拖动没有单次提交 transition，或提交后丢失关键帧选择。");
			Require(midpointDifference, "缓动曲率没有改变安全预览的真实中点结果。");
			Require(modeUndoRedo, "插值、更新方式或多次 motion 的曲线拖动没有单步撤销/重做。");
			panel.SeekPreview(0.25);
			keyAdd = panel.InsertCurrentKey(Variant.From<Vector2>(new Vector2(31f, 41f))) && panel.SelectedTrackKeyCount == 3 && HasKeyAt(panel.CurrentAnimation, 0, 0.25);
			Require(keyAdd, "无法在播放头插入关键帧。");
			keyMove = panel.SelectKey(0, 0.25) && panel.MoveSelectedKey(0.5) && HasKeyAt(panel.CurrentAnimation, 0, 0.5) && !HasKeyAt(panel.CurrentAnimation, 0, 0.25);
			Require(keyMove, "关键帧拖动/移动没有落到目标时间。");
			bool undoMove = GodotObject.IsInstanceValid(history) && history.Undo();
			await WaitFrames(3);
			bool undoValue = HasKeyAt(panel.CurrentAnimation, 0, 0.25);
			bool redoMove = history?.Redo() ?? false;
			await WaitFrames(3);
			bool flag6 = HasKeyAt(panel.CurrentAnimation, 0, 0.5);
			undoRedo = undoMove && undoValue && redoMove && flag6;
			Require(undoRedo, "动画修改没有进入当前场景撤销/重做历史。");
			keyDelete = panel.SelectKey(0, 0.5) && panel.DeleteSelectedKey() && panel.SelectedTrackKeyCount == 2 && !HasKeyAt(panel.CurrentAnimation, 0, 0.5);
			Require(keyDelete, "无法删除选中的关键帧。");
			bool restoredDeletedKey = history?.Undo() ?? false;
			await WaitFrames(3);
			Require(restoredDeletedKey && HasKeyAt(panel.CurrentAnimation, 0, 0.5), "删除关键帧后撤销没有恢复关键帧。");
			SelectOptionById(interpolationOption, 1);
			SelectOptionById(updateModeOption, 0);
			panel.SelectKey(0, 0.0);
			panel.SeekPreview(0.1);
			panel.PlayPreview();
			await WaitFrames(5);
			XW2DAnimationTimelineCanvas timelineCanvas = FindNodeOfType<XW2DAnimationTimelineCanvas>(panel);
			float transitionBeforeHiddenDrag = panel.SelectedKeyTransition;
			Vector2 vector5 = curveCanvas.Size * new Vector2(0.55f, 0.55f);
			curveCanvas._GuiInput(new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = true,
				Position = vector5
			});
			curveCanvas._GuiInput(new InputEventMouseMotion
			{
				Position = vector5 + new Vector2(0f, -26f),
				Relative = new Vector2(0f, -26f)
			});
			bool hiddenDragPreviewed = curveCanvas.IsDragging && Math.Abs(curveCanvas.Transition - transitionBeforeHiddenDrag) > 0.1f && Math.Abs(panel.SelectedKeyTransition - transitionBeforeHiddenDrag) <= 0.001f;
			double previewBeforeHidden = panel.PreviewPosition;
			Vector2 targetBeforeHidden = targetA.Position;
			int indexBeforeHidden = timelineCanvas?.TimelineIndexRebuildCount ?? (-1);
			timelineButton.SetPressedNoSignal(pressed: false);
			timelineButton.EmitSignal(BaseButton.SignalName.Toggled, false);
			await WaitFrames(2);
			int curveRevisionAfterCancel = curveCanvas.RedrawRevision;
			bool hiddenDragCancelled = !curveCanvas.IsDragging && Math.Abs(curveCanvas.Transition - transitionBeforeHiddenDrag) <= 0.001f && Math.Abs(panel.SelectedKeyTransition - transitionBeforeHiddenDrag) <= 0.001f;
			await WaitFrames(16);
			hiddenStopped = !panel.IsPreviewPlaying && !panel.IsProcessing() && Math.Abs(panel.PreviewPosition - previewBeforeHidden) <= 0.0001 && targetA.Position.DistanceTo(targetBeforeHidden) <= 0.001f && timelineCanvas != null && timelineCanvas.TimelineIndexRebuildCount == indexBeforeHidden && timelineCanvas.LastDrawVisitedTrackCount == 0 && timelineCanvas.LastDrawVisitedKeyCount == 0 && curveCanvas.RedrawRevision == curveRevisionAfterCancel && !curveCanvas.IsProcessing();
			Require(hiddenStopped, "收起 2D 动画面板后仍在推进预览、重建索引或刷新曲线。");
			timelineButton.SetPressedNoSignal(pressed: true);
			timelineButton.EmitSignal(BaseButton.SignalName.Toggled, true);
			await WaitFrames(4);
			float selectedKeyTransition = panel.SelectedKeyTransition;
			hiddenDragRollback = hiddenDragPreviewed && hiddenDragCancelled && Math.Abs(curveCanvas.Transition - selectedKeyTransition) <= 0.001f && Math.Abs((float)transitionSpin.Value - selectedKeyTransition) <= 0.001f;
			Require(hiddenDragRollback, $"拖动曲线时收起面板没有回退未提交值并同步精确曲率。 previewed={hiddenDragPreviewed} cancelled={hiddenDragCancelled} before={transitionBeforeHiddenDrag:0.###} curve={curveCanvas.Transition:0.###} restored={selectedKeyTransition:0.###} spin={transitionSpin.Value:0.###}");
			SelectOptionById(interpolationOption, 0);
			SelectOptionById(updateModeOption, 1);
			panel.SelectKey(0, 0.0);
			await WaitFrames(3);
			float transitionBeforeFocusedSave = panel.SelectedKeyTransition;
			transitionLineEdit = transitionSpin?.GetLineEdit();
			transitionLineEdit?.GrabFocus();
			await WaitFrames(2);
			if (GodotObject.IsInstanceValid(transitionLineEdit))
			{
				transitionLineEdit.Text = "3.25";
			}
			bool saveInputStillPending = GodotObject.IsInstanceValid(transitionLineEdit) && transitionLineEdit.HasFocus() && Math.Abs(panel.SelectedKeyTransition - transitionBeforeFocusedSave) <= 0.001f;
			panel.SeekPreview(0.5);
			await WaitFrames(2);
			bool savedA = editor.SaveCurrentScene();
			await WaitFrames(4);
			float selectedKeyTransition2 = panel.SelectedKeyTransition;
			savedReload = savedA && ReloadedAnimationHasKey("user://mod_editor_2d_animation_a.tscn", "测试动画", 0, 0.5);
			saveSettingsReload = savedA && ReloadedAnimationHasSettings("user://mod_editor_2d_animation_a.tscn", "测试动画", 0, Animation.InterpolationType.Nearest, Animation.UpdateMode.Discrete, loopWrap: false, selectedKeyTransition2);
			focusedSaveCommit = saveInputStillPending && Math.Abs(selectedKeyTransition2 - 3.25f) <= 0.001f && saveSettingsReload;
			Require(savedReload, "动画关键帧保存后无法通过 CacheMode.Ignore 重载。");
			Require(saveSettingsReload, "插值、更新方式、循环衔接或关键帧缓动保存后没有完整重载。");
			Require(focusedSaveCommit, "曲率输入框仍聚焦时保存，没有先提交可见的新值。");
			editor.LoadPackedSceneFromPath("user://mod_editor_2d_animation_b.tscn");
			await WaitFrames(8);
			timelineButton = editor.FindChild("AnimationTimelineButton", recursive: true, owned: false) as Button;
			timelineButton?.SetPressedNoSignal(pressed: true);
			timelineButton?.EmitSignal(BaseButton.SignalName.Toggled, true);
			await WaitFrames(3);
			panel = FindNodeOfType<XW2DAnimationTimelinePanel>(editor);
			TabBar tabs = editor.FindChild("SceneTabBar", recursive: true, owned: false) as TabBar;
			bool createdB = (panel?.SelectAnimation("AnimationPlayer", "", "安全预览样例") ?? false) && panel.CreateAnimation("B动画");
			Node2D targetB = editor.CurrentSceneInstance?.FindChild("Target", recursive: true, owned: false) as Node2D;
			Vector2 baselineB = (GodotObject.IsInstanceValid(targetB) ? targetB.Position : Vector2.Zero);
			panel?.SelectAnimation("AnimationPlayer", "", "安全预览样例");
			panel?.SeekPreview(0.5);
			await WaitFrames(2);
			bool bPreviewApplied = GodotObject.IsInstanceValid(targetB) && targetB.Position.DistanceTo(baselineB) > 0.5f;
			if (GodotObject.IsInstanceValid(tabs))
			{
				tabs.CurrentTab = 0;
			}
			await WaitFrames(6);
			bool restoredAState = editor.CurrentSceneInstance?.FindChild("Target", recursive: true, owned: false) == targetA && panel.CurrentAnimationName == "测试动画" && panel.Visible;
			previewCleanup = bPreviewApplied && GodotObject.IsInstanceValid(targetB) && targetB.Position.IsEqualApprox(baselineB) && !HasInternalPreviewPlayer(targetB.GetParent());
			if (GodotObject.IsInstanceValid(tabs))
			{
				tabs.CurrentTab = 1;
			}
			await WaitFrames(5);
			bool flag7 = panel.CurrentAnimationName == "安全预览样例" || panel.CurrentAnimationName == "B动画";
			tabIsolation = createdB && restoredAState && flag7 && editor.GetSceneHistoryIdAt(0L) != editor.GetSceneHistoryIdAt(1L);
			Require(tabIsolation, "两个 2D 场景标签的动画选择或撤销历史发生串扰。");
			Require(previewCleanup, "切换标签没有恢复预览基线或释放内部播放器。");
			inspectorUntouched = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "2D 动画直编工作台占用了原始 Inspector。");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_2D_ANIMATION_TIMELINE_PROBE] f3={f3} window={window} sceneLoaded={sceneLoaded} treeLinked={treeLinked} animationCreate={animationCreate} valueTrack={valueTrack} methodVisible={methodVisible} safePreview={safePreview} modeControls={modeControls} interpolationSwitch={interpolationSwitch} updateModeSwitch={updateModeSwitch} loopWrapSwitch={loopWrapSwitch} preciseTransitionInput={preciseTransitionInput} curveDrag={curveDrag} midpointDifference={midpointDifference} modeUndoRedo={modeUndoRedo} keyAdd={keyAdd} keyMove={keyMove} keyDelete={keyDelete} undoRedo={undoRedo} savedReload={savedReload} saveSettingsReload={saveSettingsReload} focusedSaveCommit={focusedSaveCommit} hiddenStopped={hiddenStopped} hiddenDragRollback={hiddenDragRollback} tabIsolation={tabIsolation} previewCleanup={previewCleanup} inspectorUntouched={inspectorUntouched} linearMidX={linearMidX:0.###} nearestMidX={nearestMidX:0.###} discreteMidX={discreteMidX:0.###} easedMidX={easedMidX:0.###} draggedTransition={draggedTransition:0.###} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		Finish();
	}

	private static bool CreateFixture(string path, string rootName, Vector2 baseline)
	{
		Node2D node2D = new Node2D
		{
			Name = rootName
		};
		Node2D node2D2 = new Node2D
		{
			Name = "Target",
			Position = baseline
		};
		node2D.AddChild(node2D2, forceReadableName: false, InternalMode.Disabled);
		node2D2.Owner = node2D;
		AnimationPlayer animationPlayer = new AnimationPlayer
		{
			Name = "AnimationPlayer",
			RootNode = new NodePath("..")
		};
		node2D.AddChild(animationPlayer, forceReadableName: false, InternalMode.Disabled);
		animationPlayer.Owner = node2D;
		Animation animation = new Animation
		{
			Length = 1.0,
			Step = 1f / 30f,
			ResourceLocalToScene = true
		};
		int trackIdx = animation.AddTrack(Animation.TrackType.Value);
		animation.TrackSetPath(trackIdx, new NodePath("Target:position"));
		animation.TrackInsertKey(trackIdx, 0.0, Variant.From(in baseline));
		animation.TrackInsertKey(trackIdx, 1.0, Variant.From<Vector2>(baseline + new Vector2(80f, 40f)));
		int trackIdx2 = animation.AddTrack(Animation.TrackType.Method);
		animation.TrackSetPath(trackIdx2, new NodePath("Target"));
		Dictionary from = new Dictionary
		{
			["method"] = "unsafe_probe_must_not_run",
			["args"] = new Godot.Collections.Array()
		};
		animation.TrackInsertKey(trackIdx2, 0.5, Variant.From(in from));
		AnimationLibrary animationLibrary = new AnimationLibrary
		{
			ResourceLocalToScene = true
		};
		if (animationLibrary.AddAnimation("安全预览样例", animation) != Error.Ok || animationPlayer.AddAnimationLibrary(new StringName(), animationLibrary) != Error.Ok)
		{
			node2D.Free();
			return false;
		}
		AnimationTree animationTree = new AnimationTree
		{
			Name = "AnimationTree",
			AnimPlayer = new NodePath("../AnimationPlayer"),
			TreeRoot = new AnimationNodeAnimation
			{
				Animation = "安全预览样例"
			},
			Active = false
		};
		node2D.AddChild(animationTree, forceReadableName: false, InternalMode.Disabled);
		animationTree.Owner = node2D;
		PackedScene packedScene = new PackedScene
		{
			ResourceName = rootName
		};
		bool result = packedScene.Pack(node2D) == Error.Ok && ResourceSaver.Save(packedScene, path, ResourceSaver.SaverFlags.None) == Error.Ok;
		node2D.Free();
		return result;
	}

	private async Task<Vector2> PreviewPosition(XW2DAnimationTimelinePanel panel, Node2D target, double time)
	{
		if (!GodotObject.IsInstanceValid(panel) || !GodotObject.IsInstanceValid(target))
		{
			return new Vector2(1f / 0f, 1f / 0f);
		}
		panel.SeekPreview(time);
		await WaitFrames(3);
		return target.Position;
	}

	private static bool SelectOptionById(OptionButton option, int id)
	{
		if (!GodotObject.IsInstanceValid(option))
		{
			return false;
		}
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemId(i) == id)
			{
				option.Select(i);
				option.EmitSignal(OptionButton.SignalName.ItemSelected, i);
				return true;
			}
		}
		return false;
	}

	private static bool ReloadedAnimationHasSettings(string path, string animationName, int track, Animation.InterpolationType interpolation, Animation.UpdateMode updateMode, bool loopWrap, float transition)
	{
		Node node = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
		Animation animation = ((node?.FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer)?.GetAnimationLibrary(new StringName()))?.GetAnimation(animationName);
		bool result = GodotObject.IsInstanceValid(animation) && track >= 0 && track < animation.GetTrackCount() && animation.TrackGetInterpolationType(track) == interpolation && animation.ValueTrackGetUpdateMode(track) == updateMode && animation.TrackGetInterpolationLoopWrap(track) == loopWrap && animation.TrackGetKeyCount(track) > 0 && Math.Abs(animation.TrackGetKeyTransition(track, 0) - transition) <= 0.001f;
		node?.Free();
		return result;
	}

	private static bool ReloadedAnimationHasKey(string path, string animationName, int track, double time)
	{
		Node node = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore)?.Instantiate(PackedScene.GenEditState.Disabled);
		bool result = HasKeyAt(((node?.FindChild("AnimationPlayer", recursive: true, owned: false) as AnimationPlayer)?.GetAnimationLibrary(new StringName()))?.GetAnimation(animationName), track, time);
		node?.Free();
		return result;
	}

	private static bool HasKeyAt(Animation animation, int track, double time)
	{
		if (!GodotObject.IsInstanceValid(animation) || track < 0 || track >= animation.GetTrackCount())
		{
			return false;
		}
		for (int i = 0; i < animation.TrackGetKeyCount(track); i++)
		{
			if (Math.Abs(animation.TrackGetKeyTime(track, i) - time) <= 0.0001)
			{
				return true;
			}
		}
		return false;
	}

	private static bool DescendantLabelContains(Node root, string text)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		if (root is Label label && label.Text.Contains(text, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		foreach (Node child in root.GetChildren())
		{
			if (DescendantLabelContains(child, text))
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasInternalPreviewPlayer(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		if (root is AnimationPlayer && root.Name.ToString().StartsWith("__XW2DAnimationSafePreview_", StringComparison.Ordinal))
		{
			return true;
		}
		foreach (Node child in root.GetChildren(includeInternal: true))
		{
			if (HasInternalPreviewPlayer(child))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<XW2DSceneEditor> WaitForSceneEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XW2DSceneEditor xW2DSceneEditor = FindNodeOfType<XW2DSceneEditor>(GetTree().Root);
			if (GodotObject.IsInstanceValid(xW2DSceneEditor) && xW2DSceneEditor.IsNodeReady())
			{
				return xW2DSceneEditor;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		_failures.Add("等待 F3 2D 编辑器超时。");
		return null;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
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
		foreach (Node child in root.GetChildren(includeInternal: true))
		{
			T val = FindNodeOfType<T>(child);
			if (GodotObject.IsInstanceValid(val))
			{
				return val;
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

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PushError("[MOD_EDITOR_2D_ANIMATION_TIMELINE_PROBE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
