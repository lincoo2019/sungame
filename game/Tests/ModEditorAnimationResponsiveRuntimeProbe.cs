using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.AdobeAnimateEditor.Inspector;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorAnimationResponsiveRuntimeProbe : Node
{
	private const string FixturePath = "res://Asset/Anime/Splat/Spike/SpikeSplat.tres";

	private const string DraftPath = "user://mod_editor_animation_responsive_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWAnimationVisualResourceEditor _editor;

	public override async void _Ready()
	{
		_ = 24;
		try
		{
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>("res://Asset/Anime/Splat/Spike/SpikeSplat.tres", null, ResourceLoader.CacheMode.Reuse);
			Require(GodotObject.IsInstanceValid(adobeAnimateData) && adobeAnimateData.HasPackedRuntimeData(), "Real SpikeSplat animation is unavailable.");
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
			draft.ResourceName = "ModEditorAnimationResponsiveProbe";
			Error error = ResourceSaver.Save(draft, "user://mod_editor_animation_responsive_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not save isolated animation fixture: {error}.");
			if (error != Error.Ok)
			{
				Finish();
				return;
			}
			draft = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_responsive_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(draft) && draft.HasPackedRuntimeData(), "Isolated animation did not reload with packed data.");
			if (!GodotObject.IsInstanceValid(draft) || !draft.HasPackedRuntimeData())
			{
				Finish();
				return;
			}
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
			if (flag)
			{
				flag = await EnterEditorSurface(900);
			}
			bool f3 = flag;
			Require(f3, "F3 did not initialize the animation main panel.");
			if (!f3)
			{
				Finish();
				return;
			}
			XWEditorInterface.Instance.EditResource(draft, XWResourceEditContext.ForRoot(draft, "user://mod_editor_animation_responsive_probe.tres", "animation_editor"));
			XWEditorInterface.Instance.FocusPanel("animation_editor");
			AdobeAnimateInspectorPreview preview = await WaitForPreview(900);
			Require(GodotObject.IsInstanceValid(preview), "Real animation preview did not mount.");
			if (!GodotObject.IsInstanceValid(preview))
			{
				Finish();
				return;
			}
			Control workbench = _editor.FindChild("AnimationWorkbench", recursive: true, owned: false) as Control;
			TabBar tabs = _editor.FindChild("AnimationWorkbenchTabs", recursive: true, owned: false) as TabBar;
			ScrollContainer timelineScroll = _editor.FindChild("FrameTimelineScroll", recursive: true, owned: false) as ScrollContainer;
			Control directSettings = _editor.FindChild("DirectSettingsPanel", recursive: true, owned: false) as Control;
			Control keyframePanel = _editor.FindChild("KeyframePropertyPanel", recursive: true, owned: false) as Control;
			Control legacyStageSplit = _editor.FindChild("StageLayout", recursive: true, owned: false) as Control;
			Control previewHost = _editor.FindChild("PreviewHost", recursive: true, owned: false) as Control;
			_editor.FindChild("FrameTimelinePanel", recursive: true, owned: false);
			Control control = _editor.FindChild("ClipEditorPanel", recursive: true, owned: false) as Control;
			Control control2 = _editor.FindChild("ReplacementLibraryPanel", recursive: true, owned: false) as Control;
			Control sourceContainer = _editor.FindChild("AnimationRuntimePreview", recursive: true, owned: false) as Control;
			bool pagesReachable = GodotObject.IsInstanceValid(tabs) && tabs.TabCount == 6;
			float widestPageMinimum = 0f;
			if (pagesReachable)
			{
				Control[] activeSurfaces = new Control[6] { previewHost, directSettings, timelineScroll, keyframePanel, control, control2 };
				for (int tab = 0; tab < tabs.TabCount; tab++)
				{
					tabs.CurrentTab = tab;
					await WaitFrames(2);
					Control control3 = activeSurfaces[tab];
					pagesReachable &= GodotObject.IsInstanceValid(control3) && control3.IsVisibleInTree();
					if (GodotObject.IsInstanceValid(sourceContainer))
					{
						widestPageMinimum = Math.Max(widestPageMinimum, sourceContainer.GetCombinedMinimumSize().X);
					}
					GD.Print($"[MOD_EDITOR_ANIMATION_RESPONSIVE_PAGE] tab={tab} min={sourceContainer?.GetCombinedMinimumSize().X:0.##} active={control3?.IsVisibleInTree()}");
				}
			}
			bool width820 = GodotObject.IsInstanceValid(workbench) && workbench.GetCombinedMinimumSize().X <= 820f && widestPageMinimum <= 820f && GodotObject.IsInstanceValid(timelineScroll) && timelineScroll.CustomMinimumSize.X <= 0.01f && GodotObject.IsInstanceValid(directSettings) && directSettings.CustomMinimumSize.X <= 0.01f && GodotObject.IsInstanceValid(keyframePanel) && keyframePanel.CustomMinimumSize.X <= 0.01f && GodotObject.IsInstanceValid(legacyStageSplit) && (!previewHost.Visible || !directSettings.Visible) && (!timelineScroll.Visible || !keyframePanel.Visible);
			Control control4 = _editor.FindChild("TitleRow", recursive: true, owned: false) as Control;
			Control control5 = _editor.FindChild("RuntimeInspectorPreview", recursive: true, owned: false) as Control;
			GD.Print($"[MOD_EDITOR_ANIMATION_RESPONSIVE_WIDTH] title={control4?.GetCombinedMinimumSize().X:0.##} stage={legacyStageSplit?.GetCombinedMinimumSize().X:0.##} previewHost={previewHost?.GetCombinedMinimumSize().X:0.##} runtime={control5?.GetCombinedMinimumSize().X:0.##} settings={directSettings?.GetCombinedMinimumSize().X:0.##} source={sourceContainer?.GetCombinedMinimumSize().X:0.##}");
			string[] array = new string[4] { "Header", "ClipOption", "Controls", "PreviewViewportContainer" };
			foreach (string text in array)
			{
				Control control6 = control5?.FindChild(text, recursive: true, owned: false) as Control;
				GD.Print($"[MOD_EDITOR_ANIMATION_RESPONSIVE_WIDTH_CHILD] name={text} min={control6?.GetCombinedMinimumSize().X:0.##} size={control6?.Size.X:0.##}");
			}
			Require(width820 && pagesReachable, $"Responsive 820px layout is incomplete: workbenchMin={workbench?.GetCombinedMinimumSize().X} widestPage={widestPageMinimum} pages={pagesReachable}.");
			tabs.CurrentTab = 0;
			await WaitFrames(4);
			AdobeAnimateSprite adobeAnimateSprite = preview.FindChild("InspectorAnimationSprite", recursive: true, owned: false) as AdobeAnimateSprite;
			AdobeAnimateCpuPreviewCanvas instance = preview.FindChild("CpuPreviewCanvas", recursive: true, owned: false) as AdobeAnimateCpuPreviewCanvas;
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(draft);
			bool realLoaded = GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.flashAnimeData == draft && GodotObject.IsInstanceValid(instance) && orBuild != null && orBuild.LocalBounds.Size.X > 0.01f && orBuild.LocalBounds.Size.Y > 0.01f;
			preview.FitToPanel();
			await WaitFrames(3);
			Require(realLoaded && preview.IsVisibleInTree(), "Real packed animation or fitted CPU preview bounds are unavailable.");
			tabs.CurrentTab = 2;
			await WaitFrames(3);
			XWAnimationFrameTimeline timeline = _editor.FindChild("FrameTimeline", recursive: true, owned: false) as XWAnimationFrameTimeline;
			bool selectedFrame = GodotObject.IsInstanceValid(timeline) && timeline.SelectFirstOccupiedCell();
			int initialFrame = timeline?.SelectedFrame ?? (-1);
			int sliceKey = timeline?.SelectedSliceKey ?? (-1);
			if (selectedFrame && initialFrame + 1 < draft.EditableFrameCount)
			{
				selectedFrame = timeline.SelectSliceKey(initialFrame + 1, sliceKey);
			}
			await WaitFrames(3);
			bool frameSwitch = selectedFrame && _editor.SelectedAnimationFrame == initialFrame + 1 && preview.CurrentFrame == initialFrame + 1;
			Require(frameSwitch, "Timeline frame switching did not synchronize the runtime preview.");
			if (!draft.TryGetFrameSlice(_editor.SelectedAnimationFrame, sliceKey, out var beforeChoice, out var error2))
			{
				Require(condition: false, "Selected frame slice is unavailable: " + error2);
				Finish();
				return;
			}
			tabs.CurrentTab = 3;
			await WaitFrames(3);
			XWAnimationVisualChoiceGrid mediaGrid = _editor.FindChild("MediaChoiceGrid", recursive: true, owned: false) as XWAnimationVisualChoiceGrid;
			XWAnimationVisualChoiceGrid layerGrid = _editor.FindChild("LayerChoiceGrid", recursive: true, owned: false) as XWAnimationVisualChoiceGrid;
			int nextMedia = FindDifferentChoiceId(mediaGrid, beforeChoice.MediaId);
			int nextLayer = FindDifferentChoiceId(layerGrid, beforeChoice.LayerId);
			bool visualCards = GodotObject.IsInstanceValid(mediaGrid) && mediaGrid.IsVisibleInTree() && GodotObject.IsInstanceValid(layerGrid) && layerGrid.IsVisibleInTree() && mediaGrid.ChoiceCount > 1 && layerGrid.ChoiceCount > 1 && nextMedia >= 0 && nextLayer >= 0;
			Require(visualCards, "Thumbnail media cards or status-colored layer cards are unavailable.");
			if (!visualCards)
			{
				Finish();
				return;
			}
			mediaGrid.SelectChoice(nextMedia, emitSignal: true);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice;
			string error3;
			bool mediaVisualSelection = draft.TryGetFrameSlice(_editor.SelectedAnimationFrame, sliceKey, out slice, out error3) && slice.MediaId == nextMedia;
			layerGrid.SelectChoice(nextLayer, emitSignal: true);
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice2;
			bool layerSwitch = draft.TryGetFrameSlice(_editor.SelectedAnimationFrame, sliceKey, out slice2, out error3) && slice2.LayerId == nextLayer && timeline.SelectedLayer == nextLayer;
			Require(mediaVisualSelection && layerSwitch, "Visual media/layer card selection did not update the selected packed frame slice.");
			XWUndoRedoManager undoRedo = XWEditorInterface.Instance.GetUndoRedoManager();
			bool undo = undoRedo.Undo();
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice3;
			bool undoValue = draft.TryGetFrameSlice(_editor.SelectedAnimationFrame, sliceKey, out slice3, out error3) && slice3.LayerId == beforeChoice.LayerId && slice3.MediaId == nextMedia;
			bool redo = undoRedo.Redo();
			await WaitFrames(3);
			AdobeAnimateData.FrameSlice slice4;
			bool flag2 = draft.TryGetFrameSlice(_editor.SelectedAnimationFrame, sliceKey, out slice4, out error3) && slice4.LayerId == nextLayer && slice4.MediaId == nextMedia;
			bool history = undo && undoValue && redo && flag2;
			Require(history, "Visual card edits did not survive global Undo/Redo.");
			int editedFrame = _editor.SelectedAnimationFrame;
			Button button = FindButtonByText(_editor, "保存");
			Require(GodotObject.IsInstanceValid(button) && !button.Disabled, "Animation save action is unavailable.");
			if (GodotObject.IsInstanceValid(button))
			{
				button.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(5);
			}
			AdobeAnimateData adobeAnimateData2 = ResourceLoader.Load<AdobeAnimateData>("user://mod_editor_animation_responsive_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			AdobeAnimateData.FrameSlice slice5;
			bool saveReload = GodotObject.IsInstanceValid(adobeAnimateData2) && adobeAnimateData2.TryGetFrameSlice(editedFrame, sliceKey, out slice5, out error3) && slice5.MediaId == nextMedia && slice5.LayerId == nextLayer;
			Require(saveReload, "Visual media/layer edits did not survive save and uncached reload.");
			VBoxContainer vBoxContainer = _editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
			VBoxContainer vBoxContainer2 = _editor.FindChild("DirectPropertiesHost", recursive: true, owned: false) as VBoxContainer;
			bool inspectorUntouched = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && GodotObject.IsInstanceValid(vBoxContainer2) && !HasVisibleControlChild(vBoxContainer2);
			Require(inspectorUntouched, "Responsive animation workbench touched the raw Inspector/schema surface.");
			preview = await WaitForPreview(300);
			timeline = _editor.FindChild("FrameTimeline", recursive: true, owned: false) as XWAnimationFrameTimeline;
			tabs = _editor.FindChild("AnimationWorkbenchTabs", recursive: true, owned: false) as TabBar;
			tabs.CurrentTab = 0;
			await WaitFrames(4);
			if (preview.IsPlaying)
			{
				preview.TogglePlayback();
			}
			await WaitFrames(2);
			preview.TogglePlayback();
			await WaitFrames(5);
			int playingFrame = preview.CurrentFrame;
			bool playing = preview.IsPlaying;
			preview.TogglePlayback();
			await WaitFrames(2);
			int pausedFrame = preview.CurrentFrame;
			await WaitFrames(8);
			bool playPause = playing && !preview.IsPlaying && preview.CurrentFrame == pausedFrame;
			Require(playPause, $"Runtime preview play/pause is unstable: playing={playing} before={playingFrame} paused={pausedFrame} after={preview.CurrentFrame}.");
			preview.TogglePlayback();
			await WaitFrames(3);
			_editor.Hide();
			await WaitFrames(2);
			int hiddenStateUpdates = _editor.AnimationStateUpdateCount;
			int hiddenPreviewRefreshes = preview.ControlRefreshCount;
			int hiddenTimelineRedraws = timeline.VisibleRedrawRequestCount;
			await WaitFrames(18);
			bool hiddenStopped = !_editor.AnimationHighFrequencyActive && !preview.IsPlaying && _editor.AnimationStateUpdateCount == hiddenStateUpdates && preview.ControlRefreshCount == hiddenPreviewRefreshes && timeline.VisibleRedrawRequestCount == hiddenTimelineRedraws;
			Require(hiddenStopped, "Hidden animation editor continued preview or timeline high-frequency updates.");
			_editor.Show();
			await WaitFrames(3);
			GD.Print($"[MOD_EDITOR_ANIMATION_RESPONSIVE_PROBE] f3={f3} width820={width820} pagesReachable={pagesReachable} realLoaded={realLoaded} visualCards={visualCards} frameSwitch={frameSwitch} layerSwitch={layerSwitch} mediaSelection={mediaVisualSelection} playPause={playPause} history={history} saveReload={saveReload} inspectorUntouched={inspectorUntouched} hiddenStopped={hiddenStopped} mediaCards={mediaGrid.ChoiceCount} layerCards={layerGrid.ChoiceCount} widestPage={widestPageMinimum:0.##} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static int FindDifferentChoiceId(XWAnimationVisualChoiceGrid grid, int currentId)
	{
		if (!GodotObject.IsInstanceValid(grid))
		{
			return -1;
		}
		return FindDifferentChoiceIdRecursive(grid, currentId);
	}

	private static int FindDifferentChoiceIdRecursive(Node root, int currentId)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is XWAnimationVisualChoiceCard xWAnimationVisualChoiceCard && xWAnimationVisualChoiceCard.ChoiceId != currentId)
			{
				return xWAnimationVisualChoiceCard.ChoiceId;
			}
			int num = FindDifferentChoiceIdRecursive(child, currentId);
			if (num >= 0)
			{
				return num;
			}
		}
		return -1;
	}

	private static bool HasVisibleControlChild(Node root)
	{
		foreach (Node child in root.GetChildren())
		{
			if (child is Control control && control.IsVisibleInTree())
			{
				return true;
			}
			if (HasVisibleControlChild(child))
			{
				return true;
			}
		}
		return false;
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
			GD.PrintErr("[MOD_EDITOR_ANIMATION_RESPONSIVE_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_ANIMATION_RESPONSIVE_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
