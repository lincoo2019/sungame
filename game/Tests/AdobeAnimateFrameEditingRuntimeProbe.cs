using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class AdobeAnimateFrameEditingRuntimeProbe : Node
{
	private const string DraftPath = "user://adobe_animate_frame_editing_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		bool f3 = false;
		bool query = false;
		bool update = false;
		bool cache = false;
		bool undoRedo = false;
		bool insert = false;
		bool move = false;
		bool delete = false;
		bool globalFrameOps = false;
		bool snapshot = false;
		bool atomic = false;
		bool empty = false;
		bool saveReload = false;
		bool previewParse = false;
		bool sourceProtected = false;
		bool inspectorUntouched = false;
		try
		{
			f3 = await OpenModEditor();
			Require(f3, "F3 did not open the ModEditor main surface.");
			AdobeAnimateData adobeAnimateData = CreateFixture();
			Require(adobeAnimateData.ValidatePackedFrameEditingState(out var error), "Initial packed state is invalid: " + error);
			Dictionary from = adobeAnimateData.CaptureFrameEditSnapshot();
			query = adobeAnimateData.TryGetFrameSlices(0, out var slices, out var error2) && slices.Length == 2 && slices[0].SliceKey == 10 && slices[1].SliceKey == 11 && adobeAnimateData.TryGetFrameSlices(2, out var slices2, out var error3) && slices2.Length == 0;
			Require(query, "Frame/slice query failed: " + error2);
			AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateData);
			Transform2D transform2D = new Transform2D(1.2f, 0.1f, -0.2f, 0.8f, 14f, -7f);
			long authoringRevision = adobeAnimateData.AuthoringRevision;
			update = adobeAnimateData.TryUpdateFrameSlice(0, 10, 7, 6, transform2D, 0.35f, out var error4) && adobeAnimateData.HasAuthoredPackedFrames && adobeAnimateData.AuthoringRevision > authoringRevision && adobeAnimateData.TryGetFrameSlice(0, 10, out var slice, out error3) && slice.MediaId == 7 && slice.LayerId == 6 && slice.Transform == transform2D && Mathf.IsEqualApprox(slice.Alpha, 0.35f);
			Require(update, "Frame slice update failed: " + error4);
			AdobeAnimateRuntimeDefinition orBuild2 = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateData);
			PackedSlicePose slice2;
			bool flag = orBuild2 != null && orBuild2.Frames.Length == 3 && AdobeAnimateDefinitionCache.TryGetSliceInFrame(orBuild2, 0, orBuild2.Frames[0], 10, out slice2) && slice2.MediaId == 7 && slice2.LayerId == 6 && Mathf.IsEqualApprox(slice2.Ox, 14f) && Mathf.IsEqualApprox(slice2.Alpha, 0.35f);
			cache = orBuild != null && orBuild2 != null && orBuild != orBuild2 && flag;
			Require(cache, "Definition cache did not rebuild from the authored packed state.");
			Dictionary from2 = adobeAnimateData.CaptureFrameEditSnapshot();
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager != null && adobeAnimateData.TryRestoreFrameEditSnapshot(from, out error3))
			{
				xWUndoRedoManager.ClearHistory(0);
				xWUndoRedoManager.CreateAction("Frame snapshot contract probe");
				xWUndoRedoManager.AddDoMethod(adobeAnimateData, "ApplyFrameEditSnapshot", Variant.From(in from2));
				xWUndoRedoManager.AddUndoMethod(adobeAnimateData, "ApplyFrameEditSnapshot", Variant.From(in from));
				xWUndoRedoManager.CommitAction();
				AdobeAnimateData.FrameSlice slice3;
				bool flag2 = adobeAnimateData.TryGetFrameSlice(0, 10, out slice3, out error3) && slice3.MediaId == 7;
				AdobeAnimateData.FrameSlice slice4;
				bool flag3 = xWUndoRedoManager.Undo() && adobeAnimateData.TryGetFrameSlice(0, 10, out slice4, out error3) && slice4.MediaId == 0;
				AdobeAnimateData.FrameSlice slice5;
				bool flag4 = xWUndoRedoManager.Redo() && adobeAnimateData.TryGetFrameSlice(0, 10, out slice5, out error3) && slice5.MediaId == 7;
				undoRedo = flag2 && flag3 && flag4;
			}
			Require(undoRedo, "Global Undo/Redo could not invoke the serializable frame snapshot contract.");
			insert = adobeAnimateData.TryCopyInsertClipFrame("main", 0, 1, out var error5) && adobeAnimateData.frameMax == 4 && adobeAnimateData.GetClip("main") == new Vector2I(0, 4) && adobeAnimateData.GetClip("tail") == new Vector2I(2, 4) && adobeAnimateData.events.Count == 4 && EventArgument(adobeAnimateData.events[0]) == "zero" && EventArgument(adobeAnimateData.events[1]) == "zero" && adobeAnimateData.TryGetFrameSlice(1, 10, out var slice6, out error3) && slice6.Transform == transform2D && adobeAnimateData.ValidatePackedFrameEditingState(out error3);
			Require(insert, "Clip-local frame copy/insert failed: " + error5);
			move = adobeAnimateData.TryMoveClipFrame("main", 0, 2, out var error6) && EventArgument(adobeAnimateData.events[0]) == "zero" && EventArgument(adobeAnimateData.events[1]) == "one" && EventArgument(adobeAnimateData.events[2]) == "zero" && adobeAnimateData.TryGetFrameSlice(1, 20, out var slice7, out error3) && adobeAnimateData.GetClip("main") == new Vector2I(0, 4) && adobeAnimateData.ValidatePackedFrameEditingState(out error3);
			Require(move, "Clip-local frame move failed: " + error6);
			delete = adobeAnimateData.TryDeleteClipFrame("main", 1, out var error7) && adobeAnimateData.frameMax == 3 && adobeAnimateData.GetClip("main") == new Vector2I(0, 3) && adobeAnimateData.GetClip("tail") == new Vector2I(1, 3) && adobeAnimateData.events.Count == 3 && adobeAnimateData.ValidatePackedFrameEditingState(out error3);
			Require(delete, "Clip-local frame delete failed: " + error7);
			AdobeAnimateData adobeAnimateData2 = CreateFixture();
			string error8 = "";
			string error9 = "";
			string error10 = "";
			globalFrameOps = adobeAnimateData2.TryCopyInsertFrame(1, adobeAnimateData2.EditableFrameCount, out error8) && adobeAnimateData2.frameMax == 4 && EventArgument(adobeAnimateData2.events[3]) == "one" && adobeAnimateData2.TryMoveFrame(3, 0, out error9) && EventArgument(adobeAnimateData2.events[0]) == "one" && adobeAnimateData2.TryGetFrameSlice(0, 20, out slice7, out error3) && adobeAnimateData2.TryDeleteFrame(0, out error10) && adobeAnimateData2.frameMax == 3 && EventArgument(adobeAnimateData2.events[0]) == "zero" && adobeAnimateData2.TryGetFrameSlice(0, 10, out slice7, out error3) && adobeAnimateData2.ValidatePackedFrameEditingState(out error3);
			Require(globalFrameOps, $"Global frame insert/move/delete round-trip failed: insert={error8} move={error9} delete={error10}");
			adobeAnimateData.mediaDictionary["mutated"] = 99;
			adobeAnimateData.layerDictionary["mutated"] = 99;
			adobeAnimateData.replaceSlotDictionary["mutated"] = 99;
			adobeAnimateData.mediaRects = new Vector4[1]
			{
				new Vector4(99f, 99f, 99f, 99f)
			};
			adobeAnimateData.animeFile = "res://missing/authored_source.dat";
			snapshot = adobeAnimateData.TryRestoreFrameEditSnapshot(from, out var error11) && adobeAnimateData.frameMax == 3 && adobeAnimateData.animeFile == "" && adobeAnimateData.mediaRects.Length == 8 && !adobeAnimateData.mediaDictionary.ContainsKey("mutated") && !adobeAnimateData.layerDictionary.ContainsKey("mutated") && !adobeAnimateData.replaceSlotDictionary.ContainsKey("mutated") && adobeAnimateData.GetClip("main") == new Vector2I(0, 3) && adobeAnimateData.events.Count == 3 && adobeAnimateData.ValidatePackedFrameEditingState(out error3);
			Require(snapshot, "Full authoring snapshot did not restore atomically: " + error11);
			int[] expected = Clone(adobeAnimateData.sliceKeys);
			long authoringRevision2 = adobeAnimateData.AuthoringRevision;
			AdobeAnimateData.FrameSlice slice8 = new AdobeAnimateData.FrameSlice(10, 5, 5, 5, 0, Transform2D.Identity, 1f);
			string error12;
			bool flag5 = !adobeAnimateData.TryAddFrameSlice(0, slice8, out error12);
			bool flag6 = adobeAnimateData.TryCopyFrameSlice(0, 1, 10, out error3);
			int[] expected2 = Clone(adobeAnimateData.sliceKeys);
			long authoringRevision3 = adobeAnimateData.AuthoringRevision;
			string error13;
			bool flag7 = !adobeAnimateData.TryMoveFrameSlice(0, 1, 10, out error13) && error13.Contains("already contains", StringComparison.Ordinal) && ArraysEqual(expected2, adobeAnimateData.sliceKeys) && adobeAnimateData.AuthoringRevision == authoringRevision3;
			bool flag8 = adobeAnimateData.TryRemoveFrameSlice(1, 10, out error3);
			atomic = flag5 && error12.Contains("already contains", StringComparison.Ordinal) && ArraysEqual(expected, adobeAnimateData.sliceKeys) && authoringRevision3 > authoringRevision2 && flag6 && flag7 && flag8 && adobeAnimateData.ValidatePackedFrameEditingState(out error3);
			Require(atomic, "A failed duplicate-key mutation changed packed state or revision.");
			AdobeAnimateData adobeAnimateData3 = CreateEmptyFixture();
			AdobeAnimateData.FrameSlice slice9 = new AdobeAnimateData.FrameSlice(30, 123456, -7, 4, 9, new Transform2D(0.5f, 0f, 0f, 0.75f, 3f, 4f), 1.25f);
			int num;
			if (adobeAnimateData3.HasPackedRuntimeData() && adobeAnimateData3.ValidatePackedFrameEditingState(out error3))
			{
				AdobeAnimateRuntimeDefinition orBuild3 = AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateData3);
				if (orBuild3 != null && orBuild3.Frames.Length == 2 && adobeAnimateData3.TryAddFrameSlice(1, slice9, out error3) && adobeAnimateData3.TryGetFrameSlice(1, 30, out var slice10, out error3) && slice10.MediaId == 123456 && slice10.LayerId == -7 && adobeAnimateData3.TryRemoveFrameSlice(1, 30, out error3) && adobeAnimateData3.TryGetFrameSlices(1, out var slices3, out error3) && slices3.Length == 0)
				{
					num = (adobeAnimateData3.HasPackedRuntimeData() ? 1 : 0);
					goto IL_0ae2;
				}
			}
			num = 0;
			goto IL_0ae2;
			IL_0ae2:
			empty = (byte)num != 0;
			Require(empty, "Legal empty frames/all-empty animation did not support add/remove round-trip.");
			AdobeAnimateData.FrameSlice slice11 = new AdobeAnimateData.FrameSlice(30, 3, 4, 8, 2, new Transform2D(0.9f, 0.2f, -0.1f, 1.1f, 8f, 9f), 0.6f);
			Require(adobeAnimateData.TryAddFrameSlice(2, slice11, out var error14), "Could not prepare persisted frame: " + error14);
			int frameMax = adobeAnimateData.frameMax;
			int[] expected3 = Clone(adobeAnimateData.sliceKeys);
			adobeAnimateData.animeFile = "res://missing/authored_source.dat";
			sourceProtected = adobeAnimateData.HasAuthoredPackedFrames && adobeAnimateData.frameMax == frameMax && ArraysEqual(expected3, adobeAnimateData.sliceKeys);
			Require(sourceProtected, "Assigning an authored DAT source silently destroyed packed frames.");
			Error error15 = ResourceSaver.Save(adobeAnimateData, "user://adobe_animate_frame_editing_probe.tres", ResourceSaver.SaverFlags.None);
			AdobeAnimateData adobeAnimateData4 = ((error15 == Error.Ok) ? ResourceLoader.Load<AdobeAnimateData>("user://adobe_animate_frame_editing_probe.tres", "", ResourceLoader.CacheMode.Ignore) : null);
			saveReload = error15 == Error.Ok && GodotObject.IsInstanceValid(adobeAnimateData4) && adobeAnimateData4.HasAuthoredPackedFrames && adobeAnimateData4.animeFile == "res://missing/authored_source.dat" && adobeAnimateData4.frameMax == 3 && adobeAnimateData4.TryGetFrameSlice(2, 30, out var slice12, out error3) && slice12.MediaId == 3 && slice12.LayerId == 4 && slice12.Transform == slice11.Transform && Mathf.IsEqualApprox(slice12.Alpha, 0.6f) && adobeAnimateData4.ValidatePackedFrameEditingState(out error3);
			Require(saveReload, $"Authored packed state did not survive uncached disk reload: {error15}");
			AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = (GodotObject.IsInstanceValid(adobeAnimateData4) ? AdobeAnimateDefinitionCache.GetOrBuild(adobeAnimateData4) : null);
			previewParse = adobeAnimateRuntimeDefinition != null && adobeAnimateRuntimeDefinition.Frames.Length == 3 && AdobeAnimateDefinitionCache.TryGetSliceInFrame(adobeAnimateRuntimeDefinition, 2, adobeAnimateRuntimeDefinition.Frames[2], 30, out var slice13) && slice13.MediaId == 3 && slice13.LayerId == 4 && Mathf.IsEqualApprox(slice13.Ox, 8f) && Mathf.IsEqualApprox(slice13.Alpha, 0.6f);
			Require(previewParse, "Uncached authored data did not parse into the same runtime preview pose.");
			XWAnimationVisualResourceEditor editor = XWEditorInterface.Instance?.GetResourceEditor("animation_editor") as XWAnimationVisualResourceEditor;
			if (GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(adobeAnimateData4))
			{
				XWEditorInterface.Instance.EditResource(adobeAnimateData4, XWResourceEditContext.ForRoot(adobeAnimateData4, "user://adobe_animate_frame_editing_probe.tres", "animation_editor"));
				XWEditorInterface.Instance.FocusPanel("animation_editor");
				await WaitFrames(5);
				VBoxContainer vBoxContainer = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
				inspectorUntouched = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0;
			}
			Require(inspectorUntouched, "Frame authoring mounted a raw Inspector in the animation main panel.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[ADOBE_ANIMATE_FRAME_EDITING_PROBE] f3={f3} query={query} update={update} cache={cache} undoRedo={undoRedo} insert={insert} move={move} delete={delete} globalFrameOps={globalFrameOps} snapshot={snapshot} atomic={atomic} empty={empty} saveReload={saveReload} previewParse={previewParse} sourceProtected={sourceProtected} inspectorUntouched={inspectorUntouched} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		Finish();
	}

	private async Task<bool> OpenModEditor()
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
		if (!GodotObject.IsInstanceValid(modEditorManager))
		{
			return false;
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
		for (int i = 0; i < 900; i++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance))
			{
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static AdobeAnimateData CreateFixture()
	{
		AdobeAnimateData adobeAnimateData = new AdobeAnimateData();
		adobeAnimateData.frameRate = 30.0;
		adobeAnimateData.frameMax = 3;
		adobeAnimateData.frameOffsets = new int[3] { 0, 2, 3 };
		adobeAnimateData.frameCounts = new int[3] { 2, 1, 0 };
		adobeAnimateData.sliceKeys = new int[3] { 10, 11, 20 };
		adobeAnimateData.sliceMediaIds = new int[3] { 0, 1, 2 };
		adobeAnimateData.sliceLayerIds = new int[3] { 0, 1, 2 };
		adobeAnimateData.sliceDrawOrders = new int[3] { 0, 1, 0 };
		adobeAnimateData.sliceFlags = new int[3] { 0, 2, 4 };
		adobeAnimateData.sliceTransforms = new float[18]
		{
			1f, 0f, 0f, 1f, 1f, 2f, 0.8f, 0.1f, -0.1f, 0.8f,
			3f, 4f, 1.1f, 0f, 0f, 0.9f, 5f, 6f
		};
		adobeAnimateData.sliceAlpha = new float[3] { 1f, 0.8f, 0.5f };
		adobeAnimateData.mediaRects = new Vector4[8]
		{
			new Vector4(0f, 0f, 16f, 16f),
			new Vector4(16f, 0f, 16f, 16f),
			new Vector4(32f, 0f, 16f, 16f),
			new Vector4(48f, 0f, 16f, 16f),
			new Vector4(64f, 0f, 16f, 16f),
			new Vector4(80f, 0f, 16f, 16f),
			new Vector4(96f, 0f, 16f, 16f),
			new Vector4(112f, 0f, 16f, 16f)
		};
		adobeAnimateData.clips = new Dictionary
		{
			["main"] = new Vector2I(0, 3),
			["tail"] = new Vector2I(1, 3)
		};
		adobeAnimateData.events = new Array<Godot.Collections.Array>
		{
			CreateEventFrame("zero"),
			CreateEventFrame("one"),
			CreateEventFrame("two")
		};
		AdobeAnimateData adobeAnimateData2 = adobeAnimateData;
		for (int i = 0; i < 8; i++)
		{
			adobeAnimateData2.mediaDictionary[$"media_{i}"] = i;
		}
		adobeAnimateData2.layerDictionary["base"] = 0;
		adobeAnimateData2.layerDictionary["fx"] = 1;
		adobeAnimateData2.replaceSlotDictionary["media_0"] = "slot";
		return adobeAnimateData2;
	}

	private static AdobeAnimateData CreateEmptyFixture()
	{
		AdobeAnimateData adobeAnimateData = new AdobeAnimateData();
		adobeAnimateData.frameRate = 24.0;
		adobeAnimateData.frameMax = 2;
		adobeAnimateData.frameOffsets = new int[2];
		adobeAnimateData.frameCounts = new int[2];
		adobeAnimateData.sliceKeys = System.Array.Empty<int>();
		adobeAnimateData.sliceMediaIds = System.Array.Empty<int>();
		adobeAnimateData.sliceLayerIds = System.Array.Empty<int>();
		adobeAnimateData.sliceDrawOrders = System.Array.Empty<int>();
		adobeAnimateData.sliceFlags = System.Array.Empty<int>();
		adobeAnimateData.sliceTransforms = System.Array.Empty<float>();
		adobeAnimateData.sliceAlpha = System.Array.Empty<float>();
		adobeAnimateData.clips = new Dictionary { ["empty"] = new Vector2I(0, 2) };
		adobeAnimateData.events = new Array<Godot.Collections.Array>
		{
			new Godot.Collections.Array(),
			new Godot.Collections.Array()
		};
		return adobeAnimateData;
	}

	private static Godot.Collections.Array CreateEventFrame(string argument)
	{
		return new Godot.Collections.Array
		{
			new Dictionary
			{
				["Command"] = "probe",
				["Argument"] = argument
			}
		};
	}

	private static string EventArgument(Godot.Collections.Array frame)
	{
		if (frame == null || frame.Count == 0 || frame[0].VariantType != Variant.Type.Dictionary)
		{
			return "";
		}
		if (!frame[0].AsGodotDictionary().TryGetValue("Argument", out var value))
		{
			return "";
		}
		return value.AsString();
	}

	private static T[] Clone<T>(T[] source)
	{
		if (source != null)
		{
			return (T[])source.Clone();
		}
		return System.Array.Empty<T>();
	}

	private static bool ArraysEqual<T>(T[] expected, T[] actual)
	{
		if (expected == null || actual == null || expected.Length != actual.Length)
		{
			return false;
		}
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		for (int i = 0; i < expected.Length; i++)
		{
			if (!equalityComparer.Equals(expected[i], actual[i]))
			{
				return false;
			}
		}
		return true;
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
			GD.PrintErr("[ADOBE_ANIMATE_FRAME_EDITING_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[ADOBE_ANIMATE_FRAME_EDITING_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
