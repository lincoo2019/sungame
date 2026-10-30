using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorAnimationFrameScaleRuntimeProbe : Node
{
	private const string FixturePath = "res://Asset/Anime/Splat/Spike/SpikeSplat.tres";

	private const string DraftPath = "user://mod_editor_animation_frame_scale_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private XWAnimationVisualResourceEditor _editor;

	public override async void _Ready()
	{
		bool f3 = false;
		bool uiNonBlocking = false;
		bool authoredConfirm = false;
		bool cancelPreserved = false;
		bool asyncPending = false;
		bool scaleApplied = false;
		bool dataConsistent = false;
		bool undoRedo = false;
		bool staleIgnored = false;
		bool hiddenCleanup = false;
		bool saveReload = false;
		bool inspectorUntouched = false;
		try
		{
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Splat/Spike/SpikeSplat.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(adobeAnimateData) && adobeAnimateData.HasPackedRuntimeData(), "Real SpikeSplat animation fixture is unavailable.");
			if (!GodotObject.IsInstanceValid(adobeAnimateData) || !adobeAnimateData.HasPackedRuntimeData())
			{
				Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
				return;
			}
			AdobeAnimateData draft = adobeAnimateData.Duplicate(deep: true) as AdobeAnimateData;
			Require(GodotObject.IsInstanceValid(draft), "Could not duplicate the real animation fixture.");
			if (!GodotObject.IsInstanceValid(draft))
			{
				Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
				return;
			}
			draft.ResourceName = "ModEditorAnimationFrameScaleProbe";
			draft.ClearFrameAuthoringFlag();
			Require(ResourceSaver.Save(draft, "user://mod_editor_animation_frame_scale_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Could not save the isolated animation fixture.");
			draft = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_frame_scale_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(draft) && draft.ValidatePackedFrameEditingState(out var error), "Isolated animation fixture did not reload with valid packed frames.");
			int initialScale = Math.Max(1, draft.frameScale);
			int sourceFrameCount = (draft.frameMax + initialScale - 1) / initialScale;
			double sourceFrameRate = draft.frameRate / (double)initialScale;
			int initialFrameMax = draft.frameMax;
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
				Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
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
			if (flag)
			{
				flag = await EnterEditorSurface(900);
			}
			f3 = flag;
			Require(f3, "F3 did not initialize the animation visual editor.");
			if (!f3)
			{
				Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
				return;
			}
			Node inspectorSentinel = new Node
			{
				Name = "AnimationFrameScaleInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWEditorInterface.Instance.EditResource(draft, XWResourceEditContext.ForRoot(draft, "user://mod_editor_animation_frame_scale_probe.tres", "animation_editor"));
			XWEditorInterface.Instance.FocusPanel("animation_editor");
			Require(GodotObject.IsInstanceValid(await WaitForPreview(900)), "Animation runtime preview did not mount.");
			TabBar tabBar = _editor.FindChild("AnimationWorkbenchTabs", recursive: true, owned: false) as TabBar;
			SpinBox frameScale = _editor.FindChild("FrameScaleSpinBox", recursive: true, owned: false) as SpinBox;
			ConfirmationDialog confirmation = _editor.FindChild("ReimportConfirmDialog", recursive: true, owned: false) as ConfirmationDialog;
			Require(GodotObject.IsInstanceValid(tabBar) && GodotObject.IsInstanceValid(frameScale) && GodotObject.IsInstanceValid(confirmation), "Frame-scale controls or confirmation dialog are missing from the main visual surface.");
			if (!GodotObject.IsInstanceValid(frameScale) || !GodotObject.IsInstanceValid(confirmation))
			{
				Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
				return;
			}
			tabBar.CurrentTab = 1;
			await WaitFrames(3);
			XWUndoRedoManager history = XWEditorInterface.Instance.GetUndoRedoManager();
			history.ClearHistory();
			int firstScale = initialScale + 1;
			Stopwatch stopwatch = Stopwatch.StartNew();
			frameScale.EmitSignal(Control.SignalName.FocusEntered);
			frameScale.Value = firstScale;
			stopwatch.Stop();
			bool flag2 = draft.frameScale == initialScale && draft.frameMax == initialFrameMax && stopwatch.ElapsedMilliseconds < 100;
			Stopwatch stopwatch2 = Stopwatch.StartNew();
			frameScale.EmitSignal(Control.SignalName.FocusExited);
			stopwatch2.Stop();
			asyncPending = _editor.RequestedAnimationHydrationFrameScale == firstScale && draft.frameScale == initialScale && draft.frameMax == initialFrameMax;
			uiNonBlocking = flag2 && stopwatch2.ElapsedMilliseconds < 100;
			Require(uiNonBlocking, $"Frame-scale UI blocked or mutated during preview: preview={stopwatch.ElapsedMilliseconds}ms commit={stopwatch2.ElapsedMilliseconds}ms.");
			Require(asyncPending, "Frame-scale commit did not register an atomic background request.");
			bool flag3 = await WaitForHydrationResult(draft, firstScale, 900);
			int expectedFirstFrameMax = sourceFrameCount * firstScale - firstScale + 1;
			scaleApplied = flag3 && draft.frameMax == expectedFirstFrameMax && Math.Abs(draft.frameRate - sourceFrameRate * (double)firstScale) < 0.01 && draft.ValidatePackedFrameEditingState(out error);
			Require(scaleApplied, "Background frame-scale result was not atomically applied.");
			bool firstUndo = history.Undo();
			await WaitFrames(3);
			bool firstUndoState = firstUndo && draft.frameScale == initialScale && draft.frameMax == initialFrameMax && (int)Math.Round(frameScale.Value) == initialScale;
			bool firstRedo = history.Redo();
			await WaitFrames(3);
			bool firstRedoState = firstRedo && draft.frameScale == firstScale && draft.frameMax == expectedFirstFrameMax && (int)Math.Round(frameScale.Value) == firstScale;
			Require(firstUndoState && firstRedoState, "Initial frame-scale change did not restore complete data through Undo/Redo.");
			Require(TryFindFirstSlice(draft, out var authoredFrame, out var authoredSlice), "Rebuilt animation has no editable slice.");
			float authoredAlpha = ((authoredSlice.Alpha > 0.8f) ? (authoredSlice.Alpha - 0.125f) : (authoredSlice.Alpha + 0.125f));
			Require(draft.TryUpdateFrameSlice(authoredFrame, authoredSlice.SliceKey, authoredSlice.MediaId, authoredSlice.LayerId, authoredSlice.Transform, authoredAlpha, out var error2), "Could not create an authored frame: " + error2);
			long authoredRevision = draft.AuthoringRevision;
			int authoredFrameMax = draft.frameMax;
			int versionBeforeCancel = history.GetVersion();
			int authoredScaleTarget = firstScale + 1;
			frameScale.EmitSignal(Control.SignalName.FocusEntered);
			frameScale.Value = authoredScaleTarget;
			frameScale.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(2);
			authoredConfirm = confirmation.Visible && _editor.PendingAnimationFrameScale == authoredScaleTarget && !_editor.AnimationHydrationPending && draft.frameScale == firstScale && draft.frameMax == authoredFrameMax && draft.AuthoringRevision == authoredRevision && draft.HasAuthoredPackedFrames;
			Require(authoredConfirm, "Authored frames were changed before explicit frame-scale confirmation.");
			confirmation.EmitSignal(AcceptDialog.SignalName.Canceled);
			confirmation.Hide();
			await WaitFrames(2);
			cancelPreserved = draft.frameScale == firstScale && draft.frameMax == authoredFrameMax && draft.AuthoringRevision == authoredRevision && draft.HasAuthoredPackedFrames && draft.TryGetFrameSlice(authoredFrame, authoredSlice.SliceKey, out var slice, out error) && Mathf.IsEqualApprox(slice.Alpha, authoredAlpha) && (int)Math.Round(frameScale.Value) == firstScale && history.GetVersion() == versionBeforeCancel && _editor.RequestedAnimationHydrationFrameScale < 0;
			Require(cancelPreserved, "Canceling authored frame-scale confirmation changed resource data or history.");
			frameScale.EmitSignal(Control.SignalName.FocusEntered);
			frameScale.Value = authoredScaleTarget;
			frameScale.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(1);
			confirmation.EmitSignal(AcceptDialog.SignalName.Confirmed);
			confirmation.Hide();
			bool condition = _editor.RequestedAnimationHydrationFrameScale == authoredScaleTarget && draft.frameScale == firstScale && draft.HasAuthoredPackedFrames;
			Require(condition, "Confirmed authored frame-scale request mutated the resource before background completion.");
			bool flag4 = await WaitForHydrationResult(draft, authoredScaleTarget, 900);
			int expectedAuthoredFrameMax = sourceFrameCount * authoredScaleTarget - authoredScaleTarget + 1;
			dataConsistent = flag4 && draft.frameMax == expectedAuthoredFrameMax && Math.Abs(draft.frameRate - sourceFrameRate * (double)authoredScaleTarget) < 0.01 && !draft.HasAuthoredPackedFrames && draft.ValidatePackedFrameEditingState(out error);
			Require(dataConsistent, "Confirmed frame-scale rebuild left frame/clip/event/packed data inconsistent.");
			bool authoredUndo = history.Undo();
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice2;
			bool authoredUndoState = authoredUndo && draft.frameScale == firstScale && draft.frameMax == authoredFrameMax && draft.HasAuthoredPackedFrames && draft.TryGetFrameSlice(authoredFrame, authoredSlice.SliceKey, out slice2, out error) && Mathf.IsEqualApprox(slice2.Alpha, authoredAlpha) && (int)Math.Round(frameScale.Value) == firstScale;
			bool authoredRedo = history.Redo();
			await WaitFrames(3);
			bool flag5 = authoredRedo && draft.frameScale == authoredScaleTarget && draft.frameMax == expectedAuthoredFrameMax && !draft.HasAuthoredPackedFrames && (int)Math.Round(frameScale.Value) == authoredScaleTarget;
			undoRedo = firstUndoState && firstRedoState && authoredUndoState && flag5;
			Require(undoRedo, "Authored frame-scale rebuild did not round-trip complete snapshots through Undo/Redo.");
			int cleanupTarget = authoredScaleTarget + 1;
			frameScale.EmitSignal(Control.SignalName.FocusEntered);
			frameScale.Value = cleanupTarget;
			frameScale.EmitSignal(Control.SignalName.FocusExited);
			bool cleanupRegistered = _editor.RequestedAnimationHydrationFrameScale == cleanupTarget && draft.frameScale == authoredScaleTarget;
			_editor.Hide();
			await WaitFrames(4);
			hiddenCleanup = cleanupRegistered && !_editor.AnimationHydrationPending && _editor.RequestedAnimationHydrationFrameScale < 0 && draft.frameScale == authoredScaleTarget;
			Require(hiddenCleanup, "Hiding the animation editor did not cancel and release frame-scale hydration.");
			_editor.Show();
			await WaitFrames(3);
			frameScale.EmitSignal(Control.SignalName.FocusEntered);
			frameScale.Value = cleanupTarget;
			frameScale.EmitSignal(Control.SignalName.FocusExited);
			bool staleRegistered = _editor.RequestedAnimationHydrationFrameScale == cleanupTarget && draft.frameScale == authoredScaleTarget;
			Require(TryFindFirstSlice(draft, out var staleFrame, out var staleSlice), "Animation has no slice for the stale-result guard.");
			float staleAlpha = ((staleSlice.Alpha > 0.7f) ? (staleSlice.Alpha - 0.07f) : (staleSlice.Alpha + 0.07f));
			Require(draft.TryUpdateFrameSlice(staleFrame, staleSlice.SliceKey, staleSlice.MediaId, staleSlice.LayerId, staleSlice.Transform, staleAlpha, out var error3), "Could not create a concurrent frame edit: " + error3);
			await WaitForHydrationCleared(900);
			staleIgnored = staleRegistered && draft.frameScale == authoredScaleTarget && draft.HasAuthoredPackedFrames && draft.TryGetFrameSlice(staleFrame, staleSlice.SliceKey, out var slice3, out error) && Mathf.IsEqualApprox(slice3.Alpha, staleAlpha) && _editor.RequestedAnimationHydrationFrameScale < 0;
			Require(staleIgnored, "A stale background result overwrote a newer authored frame edit.");
			Require(ResourceSaver.Save(draft, "user://mod_editor_animation_frame_scale_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Could not save final frame-scale resource.");
			AdobeAnimateData adobeAnimateData2 = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_frame_scale_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			saveReload = GodotObject.IsInstanceValid(adobeAnimateData2) && adobeAnimateData2.frameScale == authoredScaleTarget && adobeAnimateData2.frameMax == expectedAuthoredFrameMax && adobeAnimateData2.HasAuthoredPackedFrames && adobeAnimateData2.ValidatePackedFrameEditingState(out error) && adobeAnimateData2.TryGetFrameSlice(staleFrame, staleSlice.SliceKey, out var slice4, out error) && Mathf.IsEqualApprox(slice4.Alpha, staleAlpha);
			Require(saveReload, "Frame-scale and preserved authored data did not survive cache-ignoring reload.");
			inspectorUntouched = !(XWEditorInterface.Instance.GetInspector() is XWInspector xWInspector) || xWInspector.CurrentObject == inspectorSentinel;
			Require(inspectorUntouched, "Animation frame-scale editing replaced the raw Inspector object.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish(f3, uiNonBlocking, authoredConfirm, cancelPreserved, asyncPending, scaleApplied, dataConsistent, undoRedo, staleIgnored, hiddenCleanup, saveReload, inspectorUntouched);
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
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
		for (int frame = 0; frame < maxFrames; frame++)
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
		for (int frame = 0; frame < maxFrames; frame++)
		{
			AdobeAnimateInspectorPreview adobeAnimateInspectorPreview = _editor?.FindChild("RuntimeInspectorPreview", recursive: true, owned: false) as AdobeAnimateInspectorPreview;
			if (GodotObject.IsInstanceValid(adobeAnimateInspectorPreview))
			{
				return adobeAnimateInspectorPreview;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> WaitForHydrationResult(AdobeAnimateData animation, int expectedScale, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (_editor.RequestedAnimationHydrationFrameScale < 0 && !_editor.AnimationHydrationPending && animation.frameScale == expectedScale)
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task WaitForHydrationCleared(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			if (_editor.RequestedAnimationHydrationFrameScale < 0 && !_editor.AnimationHydrationPending)
			{
				break;
			}
			await WaitFrames(1);
		}
	}

	private static bool TryFindFirstSlice(AdobeAnimateData animation, out int frame, out AdobeAnimateData.FrameSlice slice)
	{
		frame = -1;
		slice = default(AdobeAnimateData.FrameSlice);
		for (int i = 0; i < animation.EditableFrameCount; i++)
		{
			if (animation.TryGetFrameSlices(i, out var slices, out var _) && slices.Length != 0)
			{
				frame = i;
				slice = slices[0];
				return true;
			}
		}
		return false;
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
			GD.PrintErr("[MOD_EDITOR_ANIMATION_FRAME_SCALE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish(bool f3, bool uiNonBlocking, bool authoredConfirm, bool cancelPreserved, bool asyncPending, bool scaleApplied, bool dataConsistent, bool undoRedo, bool staleIgnored, bool hiddenCleanup, bool saveReload, bool inspectorUntouched)
	{
		GD.Print($"[MOD_EDITOR_ANIMATION_FRAME_SCALE_PROBE] f3={f3} uiNonBlocking={uiNonBlocking} authoredConfirm={authoredConfirm} cancelPreserved={cancelPreserved} asyncPending={asyncPending} scaleApplied={scaleApplied} dataConsistent={dataConsistent} undoRedo={undoRedo} staleIgnored={staleIgnored} hiddenCleanup={hiddenCleanup} saveReload={saveReload} inspectorUntouched={inspectorUntouched} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_ANIMATION_FRAME_SCALE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
