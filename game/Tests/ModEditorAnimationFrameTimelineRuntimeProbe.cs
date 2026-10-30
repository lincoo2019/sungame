using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorAnimationFrameTimelineRuntimeProbe : Node
{
	private const string FixturePath = "res://Asset/Anime/Splat/Spike/SpikeSplat.tres";

	private const string DraftPath = "user://mod_editor_animation_frame_timeline_probe.tres";

	private const int LongFrameCount = 2000;

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWAnimationVisualResourceEditor _editor;

	public override async void _Ready()
	{
		_ = 20;
		try
		{
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Splat/Spike/SpikeSplat.tres", null, ResourceLoader.CacheMode.Reuse);
			Require(GodotObject.IsInstanceValid(adobeAnimateData) && adobeAnimateData.HasPackedRuntimeData(), "Real SpikeSplat animation fixture is unavailable.");
			if (!GodotObject.IsInstanceValid(adobeAnimateData) || !adobeAnimateData.HasPackedRuntimeData())
			{
				Finish();
				return;
			}
			AdobeAnimateData draft = adobeAnimateData.Duplicate(deep: true) as AdobeAnimateData;
			Require(GodotObject.IsInstanceValid(draft), "Could not duplicate the real animation fixture.");
			if (!GodotObject.IsInstanceValid(draft))
			{
				Finish();
				return;
			}
			draft.ResourceName = "ModEditorAnimationFrameTimelineProbe";
			Require(BuildLongSparseTimeline(draft, adobeAnimateData), "Could not construct the 2000-frame sparse timeline fixture.");
			Require(draft.ValidatePackedFrameEditingState(out var error), "Long timeline packed state is invalid: " + error);
			Error error2 = ResourceSaver.Save(draft, "user://mod_editor_animation_frame_timeline_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error2 == Error.Ok, $"Could not save the isolated animation resource: {error2}.");
			if (error2 != Error.Ok)
			{
				Finish();
				return;
			}
			draft = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_frame_timeline_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(draft) && draft.EditableFrameCount == 2000, "Long animation resource did not reload from disk.");
			if (!GodotObject.IsInstanceValid(draft))
			{
				Finish();
				return;
			}
			Require(draft.TryGetFrameSlices(0, out var slices, out var error3) && slices.Length != 0, "Frame 0 has no editable slice: " + error3);
			if (slices.Length == 0)
			{
				Finish();
				return;
			}
			AdobeAnimateData.FrameSlice originalSlice = slices[0];
			Stage($"fixture-ready authored={draft.HasAuthoredPackedFrames}");
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
			Stage("f3-open");
			Require(flag, "F3 did not initialize the animation editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Stage("editor-open");
			Require(flag2, "F3 editor did not finish loading its main surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			XWEditorInterface.Instance.EditResource(draft, XWResourceEditContext.ForRoot(draft, "user://mod_editor_animation_frame_timeline_probe.tres", "animation_editor"));
			XWEditorInterface.Instance.FocusPanel("animation_editor");
			AdobeAnimateInspectorPreview preview = await WaitForPreview(900);
			Stage("preview-ready");
			Require(GodotObject.IsInstanceValid(preview), "Frame authoring did not mount the real AdobeAnimateSprite preview.");
			if (!GodotObject.IsInstanceValid(preview))
			{
				Finish();
				return;
			}
			TabBar tabBar = _editor.FindChild("AnimationWorkbenchTabs", recursive: true, owned: false) as TabBar;
			Require(GodotObject.IsInstanceValid(tabBar) && tabBar.CurrentTab == 0, "Animation workbench did not prioritize the runtime preview tab.");
			if (GodotObject.IsInstanceValid(tabBar))
			{
				tabBar.CurrentTab = 2;
				await WaitFrames(3);
			}
			XWAnimationFrameTimeline timeline = _editor.FindChild("FrameTimeline", recursive: true, owned: false) as XWAnimationFrameTimeline;
			SpinBox positionX = _editor.FindChild("PositionXSpinBox", recursive: true, owned: false) as SpinBox;
			SpinBox instance = _editor.FindChild("RotationSpinBox", recursive: true, owned: false) as SpinBox;
			HSlider alpha = _editor.FindChild("AlphaSlider", recursive: true, owned: false) as HSlider;
			OptionButton media = _editor.FindChild("MediaOption", recursive: true, owned: false) as OptionButton;
			OptionButton instance2 = _editor.FindChild("LayerOption", recursive: true, owned: false) as OptionButton;
			TextureRect instance3 = _editor.FindChild("MediaThumbnail", recursive: true, owned: false) as TextureRect;
			Button duplicate = _editor.FindChild("DuplicateKeyframeButton", recursive: true, owned: false) as Button;
			Button moveLater = _editor.FindChild("MoveKeyframeLaterButton", recursive: true, owned: false) as Button;
			Button delete = _editor.FindChild("DeleteKeyframeButton", recursive: true, owned: false) as Button;
			Button reload = _editor.FindChild("ReloadButton", recursive: true, owned: false) as Button;
			ConfirmationDialog reimportConfirm = _editor.FindChild("ReimportConfirmDialog", recursive: true, owned: false) as ConfirmationDialog;
			VBoxContainer vBoxContainer = _editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
			VBoxContainer vBoxContainer2 = _editor.FindChild("DirectPropertiesHost", recursive: true, owned: false) as VBoxContainer;
			bool directVisual = GodotObject.IsInstanceValid(timeline) && GodotObject.IsInstanceValid(positionX) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(alpha) && GodotObject.IsInstanceValid(media) && GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(instance3) && GodotObject.IsInstanceValid(duplicate) && GodotObject.IsInstanceValid(moveLater) && GodotObject.IsInstanceValid(delete) && timeline.IsVisibleInTree();
			bool inspectorUntouched = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0;
			bool directSchemaHidden = GodotObject.IsInstanceValid(vBoxContainer2);
			if (directSchemaHidden)
			{
				foreach (Node child in vBoxContainer2.GetChildren())
				{
					if (child is Control { Visible: not false })
					{
						directSchemaHidden = false;
						break;
					}
				}
			}
			Require(directVisual, "Game-style frame timeline/property surface is not visible in the animation main panel.");
			Require(inspectorUntouched, "Frame authoring seized the raw embedded Inspector.");
			Require(directSchemaHidden, "Animation authoring expanded the generic raw schema below its specialized visual controls.");
			if (!directVisual)
			{
				Finish();
				return;
			}
			Stage($"select-before currentFrame={preview.CurrentFrame} playing={preview.IsPlaying}");
			bool selected = timeline.SelectSliceKey(0, originalSlice.SliceKey);
			Stage("select-call-returned");
			await WaitFrames(3);
			bool previewSelectionSync = selected && _editor.SelectedAnimationFrame == 0 && _editor.SelectedAnimationSliceKey == originalSlice.SliceKey && preview.CurrentFrame == 0;
			Require(previewSelectionSync, "Selecting a visual frame diamond did not synchronize the real runtime preview.");
			Stage("select");
			int indexBuildsBefore = timeline.TimelineIndexRebuildCount;
			Stopwatch playheadWatch = Stopwatch.StartNew();
			for (int i = 0; i < 2000; i++)
			{
				timeline.SetPlayhead(i);
			}
			playheadWatch.Stop();
			await WaitFrames(3);
			bool longTimelineFast = playheadWatch.ElapsedMilliseconds < 250 && timeline.TimelineIndexRebuildCount == indexBuildsBefore && timeline.LastDrawVisitedFrameCount > 0 && timeline.LastDrawVisitedFrameCount < 120 && timeline.LastDrawVisitedCellCount < 120;
			Require(longTimelineFast, $"2000-frame playhead/culling budget failed: elapsed={playheadWatch.ElapsedMilliseconds}ms rebuilds={timeline.TimelineIndexRebuildCount - indexBuildsBefore} visitedFrames={timeline.LastDrawVisitedFrameCount} visitedCells={timeline.LastDrawVisitedCellCount}.");
			Stage("playhead-2000");
			timeline.SetPlayhead(0);
			timeline.SelectSliceKey(0, originalSlice.SliceKey);
			await WaitFrames(2);
			float editedX = originalSlice.Transform.Origin.X + 17f;
			positionX.EmitSignal(Control.SignalName.FocusEntered);
			positionX.Value = editedX;
			positionX.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice;
			string error4;
			bool transformEdit = draft.TryGetFrameSlice(0, originalSlice.SliceKey, out slice, out error4) && Mathf.IsEqualApprox(slice.Transform.Origin.X, editedX) && draft.HasAuthoredPackedFrames && _editor.AnimationFrameMutationCount > 0 && preview.CurrentFrame == 0;
			Require(transformEdit, $"Direct position editing did not update authored packed data and the real preview: actualX={slice.Transform.Origin.X} expectedX={editedX} authored={draft.HasAuthoredPackedFrames} mutations={_editor.AnimationFrameMutationCount} previewFrame={preview.CurrentFrame}.");
			Stage("position");
			XWUndoRedoManager undoRedo = XWEditorInterface.Instance.GetUndoRedoManager();
			bool undoTransform = undoRedo.Undo();
			await WaitFrames(2);
			AdobeAnimateData.FrameSlice undoSlice;
			bool undoTransformValue = draft.TryGetFrameSlice(0, originalSlice.SliceKey, out undoSlice, out error4) && Mathf.IsEqualApprox(undoSlice.Transform.Origin.X, originalSlice.Transform.Origin.X) && Mathf.IsEqualApprox((float)positionX.Value, originalSlice.Transform.Origin.X) && timeline.SelectedFrame == 0 && timeline.SelectedSliceKey == originalSlice.SliceKey;
			bool redoTransform = undoRedo.Redo();
			await WaitFrames(2);
			AdobeAnimateData.FrameSlice slice2;
			bool flag3 = draft.TryGetFrameSlice(0, originalSlice.SliceKey, out slice2, out error4) && Mathf.IsEqualApprox(slice2.Transform.Origin.X, editedX) && Mathf.IsEqualApprox((float)positionX.Value, editedX) && timeline.SelectedFrame == 0 && timeline.SelectedSliceKey == originalSlice.SliceKey;
			bool undoRedoPassed = undoTransform && undoTransformValue && redoTransform && flag3;
			Require(undoRedoPassed, $"Frame transform edit did not survive global Undo/Redo: undo={undoTransform}/{undoTransformValue} actualUndoX={undoSlice.Transform.Origin.X} redo={redoTransform}/{flag3} actualRedoX={slice2.Transform.Origin.X}.");
			Stage("undo-redo");
			alpha.EmitSignal(Slider.SignalName.DragStarted);
			alpha.Value = 0.42;
			alpha.EmitSignal(Slider.SignalName.DragEnded, true);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice3;
			bool alphaEdit = draft.TryGetFrameSlice(0, originalSlice.SliceKey, out slice3, out error4) && Mathf.IsEqualApprox(slice3.Alpha, 0.42f);
			Require(alphaEdit, "Visual alpha slider did not update the selected frame slice.");
			Stage("alpha");
			int expectedMedia = slice3.MediaId;
			if (media.ItemCount > 1)
			{
				int num = (media.Selected + 1) % media.ItemCount;
				expectedMedia = media.GetItemMetadata(num).AsInt32();
				media.Select(num);
				media.EmitSignal(OptionButton.SignalName.ItemSelected, num);
				await WaitFrames(3);
			}
			AdobeAnimateData.FrameSlice slice4;
			bool mediaEdit = draft.TryGetFrameSlice(0, originalSlice.SliceKey, out slice4, out error4) && slice4.MediaId == expectedMedia;
			Require(mediaEdit, "Media thumbnail selector did not update the selected frame slice.");
			Stage("media");
			duplicate.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice5;
			bool duplicated = draft.TryGetFrameSlice(1, originalSlice.SliceKey, out slice5, out error4) && Mathf.IsEqualApprox(slice5.Transform.Origin.X, editedX) && Mathf.IsEqualApprox(slice5.Alpha, 0.42f);
			Require(duplicated, "Duplicate frame-state button did not copy the selected slice into the next empty frame.");
			Stage("duplicate");
			moveLater.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice6;
			bool movedByButton = !draft.TryGetFrameSlice(1, originalSlice.SliceKey, out slice6, out error4) && draft.TryGetFrameSlice(2, originalSlice.SliceKey, out slice6, out error4);
			Require(movedByButton, "Move-later frame-state button did not preserve the slice while changing frames.");
			Stage("move-button");
			int selectedLayer = timeline.SelectedLayer;
			Vector2 vector = new Vector2(228f, 32f + (float)selectedLayer * 34f + 17f);
			Vector2 vector2 = new Vector2(256f, vector.Y);
			timeline._GuiInput(new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = true,
				Position = vector
			});
			timeline._GuiInput(new InputEventMouseMotion
			{
				Position = vector2,
				Relative = vector2 - vector
			});
			timeline._GuiInput(new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = false,
				Position = vector2
			});
			await WaitFrames(3);
			bool dragMove = !draft.TryGetFrameSlice(2, originalSlice.SliceKey, out slice6, out error4) && draft.TryGetFrameSlice(3, originalSlice.SliceKey, out slice6, out error4);
			Require(dragMove, "Dragging a timeline diamond did not move the frame slice to the target frame.");
			Stage("move-drag");
			delete.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			bool deleted = !draft.TryGetFrameSlice(3, originalSlice.SliceKey, out slice6, out error4);
			bool undoDelete = undoRedo.Undo();
			await WaitFrames(2);
			AdobeAnimateData.FrameSlice slice7;
			bool deleteRecovered = draft.TryGetFrameSlice(3, originalSlice.SliceKey, out slice7, out error4) && Mathf.IsEqualApprox(slice7.Transform.Origin.X, editedX) && Mathf.IsEqualApprox(slice7.Alpha, 0.42f);
			Require(deleted && undoDelete && deleteRecovered, "Deleted visual frame state did not recover exactly through global Undo.");
			Stage("delete-undo");
			reload.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			bool reimportGuard = GodotObject.IsInstanceValid(reimportConfirm) && reimportConfirm.Visible && draft.HasAuthoredPackedFrames && draft.TryGetFrameSlice(3, originalSlice.SliceKey, out slice6, out error4);
			Require(reimportGuard, "Authored frames were not protected by the explicit re-import confirmation dialog.");
			if (GodotObject.IsInstanceValid(reimportConfirm))
			{
				reimportConfirm.EmitSignal("canceled");
				reimportConfirm.Hide();
			}
			Stage("reimport-guard");
			int visitedFrames = timeline.LastDrawVisitedFrameCount;
			int visitedCells = timeline.LastDrawVisitedCellCount;
			Button button = FindButtonByText(_editor, "保存");
			Require(GodotObject.IsInstanceValid(button) && !button.Disabled, "Animation resource save action is unavailable.");
			long savePersistenceMs = -1L;
			long saveRefreshMs = -1L;
			long saveTotalMs = -1L;
			if (GodotObject.IsInstanceValid(button) && !button.Disabled)
			{
				Stage("save-start");
				ulong ticksMsec = Time.GetTicksMsec();
				button.EmitSignal(BaseButton.SignalName.Pressed);
				ulong ticksMsec2 = Time.GetTicksMsec();
				savePersistenceMs = ((_editor.LastAnimationSavePersistedTicks >= ticksMsec) ? ((long)(_editor.LastAnimationSavePersistedTicks - ticksMsec)) : (-1L));
				saveRefreshMs = ((_editor.LastAnimationSavePersistedTicks <= ticksMsec2) ? ((long)(ticksMsec2 - _editor.LastAnimationSavePersistedTicks)) : (-1L));
				saveTotalMs = (long)(ticksMsec2 - ticksMsec);
				await WaitFrames(5);
			}
			Stage($"save-refresh-complete persistMs={savePersistenceMs} refreshMs={saveRefreshMs} totalMs={saveTotalMs}");
			Require(saveTotalMs >= 0 && saveTotalMs < 1000, $"Animation save blocked the UI too long: persist={savePersistenceMs}ms refresh={saveRefreshMs}ms total={saveTotalMs}ms.");
			AdobeAnimateData adobeAnimateData2 = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_frame_timeline_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			AdobeAnimateData.FrameSlice slice8;
			bool saveReload = GodotObject.IsInstanceValid(adobeAnimateData2) && adobeAnimateData2.EditableFrameCount == 2000 && adobeAnimateData2.ValidatePackedFrameEditingState(out error4) && adobeAnimateData2.TryGetFrameSlice(3, originalSlice.SliceKey, out slice8, out error4) && Mathf.IsEqualApprox(slice8.Transform.Origin.X, editedX) && Mathf.IsEqualApprox(slice8.Alpha, 0.42f) && slice8.MediaId == expectedMedia;
			Require(saveReload, "Frame transform/media/alpha/move edits did not survive an uncached disk reload.");
			Stage("save-reload");
			AdobeAnimateSprite adobeAnimateSprite = (await WaitForPreview(300))?.FindChild("InspectorAnimationSprite", recursive: true, owned: false) as AdobeAnimateSprite;
			bool flag4 = GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.flashAnimeData == draft;
			Require(flag4, "Frame editor preview is not bound to the edited AdobeAnimateData resource.");
			bool value = FindAncestorWindow(_editor) != null;
			GD.Print($"[MOD_EDITOR_ANIMATION_FRAME_TIMELINE_PROBE] window={value} directVisual={directVisual} inspectorUntouched={inspectorUntouched} directSchemaHidden={directSchemaHidden} previewSelectionSync={previewSelectionSync} longTimelineFast={longTimelineFast} transformEdit={transformEdit} undoRedo={undoRedoPassed} alphaEdit={alphaEdit} mediaEdit={mediaEdit} duplicated={duplicated} movedByButton={movedByButton} dragMove={dragMove} deleteRecovery={deleted && undoDelete && deleteRecovered} reimportGuard={reimportGuard} saveReload={saveReload} previewBound={flag4} visitedFrames={visitedFrames} visitedCells={visitedCells} playheadMs={playheadWatch.ElapsedMilliseconds} savePersistMs={savePersistenceMs} saveRefreshMs={saveRefreshMs} saveTotalMs={saveTotalMs} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static bool BuildLongSparseTimeline(AdobeAnimateData draft, AdobeAnimateData source)
	{
		if (!source.TryGetFrameSlices(0, out var slices, out var error) || slices.Length == 0)
		{
			return false;
		}
		int num = slices.Length;
		draft.frameMax = 2000;
		draft.frameOffsets = new int[2000];
		draft.frameCounts = new int[2000];
		draft.frameCounts[0] = num;
		for (int i = 1; i < 2000; i++)
		{
			draft.frameOffsets[i] = num;
		}
		draft.sliceKeys = new int[num];
		draft.sliceMediaIds = new int[num];
		draft.sliceLayerIds = new int[num];
		draft.sliceDrawOrders = new int[num];
		draft.sliceFlags = new int[num];
		draft.sliceTransforms = new float[num * 6];
		draft.sliceAlpha = new float[num];
		for (int j = 0; j < num; j++)
		{
			AdobeAnimateData.FrameSlice frameSlice = slices[j];
			draft.sliceKeys[j] = frameSlice.SliceKey;
			draft.sliceMediaIds[j] = frameSlice.MediaId;
			draft.sliceLayerIds[j] = frameSlice.LayerId;
			draft.sliceDrawOrders[j] = frameSlice.DrawOrder;
			draft.sliceFlags[j] = frameSlice.Flags;
			int num2 = j * 6;
			draft.sliceTransforms[num2] = frameSlice.Transform.X.X;
			draft.sliceTransforms[num2 + 1] = frameSlice.Transform.X.Y;
			draft.sliceTransforms[num2 + 2] = frameSlice.Transform.Y.X;
			draft.sliceTransforms[num2 + 3] = frameSlice.Transform.Y.Y;
			draft.sliceTransforms[num2 + 4] = frameSlice.Transform.Origin.X;
			draft.sliceTransforms[num2 + 5] = frameSlice.Transform.Origin.Y;
			draft.sliceAlpha[j] = frameSlice.Alpha;
		}
		draft.events = new Array<Godot.Collections.Array>();
		draft.events.Resize(2000);
		for (int k = 0; k < 2000; k++)
		{
			draft.events[k] = new Godot.Collections.Array();
		}
		draft.clips = new Dictionary { ["all"] = new Vector2I(0, 2000) };
		AdobeAnimateDefinitionCache.Invalidate(draft);
		draft.EmitChanged();
		AdobeAnimateData.FrameSlice frameSlice2 = slices[0];
		float alpha = ((frameSlice2.Alpha >= 0.9995f) ? 0.999f : Math.Min(1f, frameSlice2.Alpha + 0.001f));
		if (draft.TryUpdateFrameSlice(0, frameSlice2.SliceKey, frameSlice2.MediaId, frameSlice2.LayerId, frameSlice2.Transform, alpha, out error))
		{
			return draft.HasAuthoredPackedFrames;
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("animation_editor") is XWAnimationVisualResourceEditor xWAnimationVisualResourceEditor && GodotObject.IsInstanceValid(xWAnimationVisualResourceEditor))
			{
				_editor = xWAnimationVisualResourceEditor;
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("animation_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<AdobeAnimateInspectorPreview> WaitForPreview(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			AdobeAnimateInspectorPreview adobeAnimateInspectorPreview = _editor?.FindChild("RuntimeInspectorPreview", recursive: true, owned: false) as AdobeAnimateInspectorPreview;
			AdobeAnimateSprite instance = adobeAnimateInspectorPreview?.FindChild("InspectorAnimationSprite", recursive: true, owned: false) as AdobeAnimateSprite;
			if (GodotObject.IsInstanceValid(adobeAnimateInspectorPreview) && GodotObject.IsInstanceValid(instance))
			{
				return adobeAnimateInspectorPreview;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static Button FindButtonByText(Node root, string text)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		if (root is Button button && button.Text.Contains(text, StringComparison.Ordinal))
		{
			return button;
		}
		foreach (Node child in root.GetChildren())
		{
			Button button2 = FindButtonByText(child, text);
			if (GodotObject.IsInstanceValid(button2))
			{
				return button2;
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
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_ANIMATION_FRAME_TIMELINE_PROBE_FAILURE] " + message);
		}
	}

	private void Stage(string name)
	{
		GD.Print($"[MOD_EDITOR_ANIMATION_FRAME_TIMELINE_STAGE] {name} elapsedMs={_stopwatch.ElapsedMilliseconds}");
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_ANIMATION_FRAME_TIMELINE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
