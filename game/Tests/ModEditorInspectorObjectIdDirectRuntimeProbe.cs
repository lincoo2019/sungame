using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorInspectorObjectIdDirectRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _watch = Stopwatch.StartNew();

	public override async void _Ready()
	{
		_ = 11;
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
			XWInspector inspector = await WaitForInspector(900);
			Require(GodotObject.IsInstanceValid(inspector), "F3 did not mount the Inspector.");
			if (!GodotObject.IsInstanceValid(inspector))
			{
				Finish();
				return;
			}
			Node target = new Node
			{
				Name = "ObjectIdDirectTarget"
			};
			AddChild(target, forceReadableName: false, InternalMode.Disabled);
			long targetId = (long)target.GetInstanceId();
			InspectorObjectIdDirectProbeResource resource = new InspectorObjectIdDirectProbeResource();
			XWEditorInterface.Instance.InspectObject(resource);
			XWEditorInterface.Instance.FocusPanel("inspector");
			await WaitFrames(8);
			XWInspectorPropertyEditorObjectID xWInspectorPropertyEditorObjectID = FindNode<XWInspectorPropertyEditorObjectID>(inspector);
			LineEdit input = xWInspectorPropertyEditorObjectID?.FindChild("IdInput", recursive: true, owned: false) as LineEdit;
			Label status = xWInspectorPropertyEditorObjectID?.FindChild("StatusLabel", recursive: true, owned: false) as Label;
			Button jump = xWInspectorPropertyEditorObjectID?.FindChild("JumpButton", recursive: true, owned: false) as Button;
			Require(GodotObject.IsInstanceValid(xWInspectorPropertyEditorObjectID), "ObjectId did not use its direct editor.");
			Require(GodotObject.IsInstanceValid(input), "ObjectId exact integer input is missing.");
			Require(GodotObject.IsInstanceValid(status), "ObjectId visual status is missing.");
			Require(GodotObject.IsInstanceValid(jump), "ObjectId jump button is missing.");
			if (!GodotObject.IsInstanceValid(input) || !GodotObject.IsInstanceValid(status) || !GodotObject.IsInstanceValid(jump))
			{
				Finish();
				return;
			}
			XWUndoRedoManager history = XWEditorInterface.Instance.GetUndoRedoManager();
			history.ClearHistory();
			await EnterId(input, targetId.ToString(CultureInfo.InvariantCulture));
			bool directEdit = resource.TargetObjectId == targetId && history.HasUndo();
			bool validState = !jump.Disabled && status.Text.Contains(target.Name.ToString());
			Require(directEdit, "ObjectId input did not directly edit the exported value.");
			Require(validState, "Valid ObjectId did not show a navigable object state.");
			bool undoCalled = history.Undo();
			await WaitFrames(3);
			bool undone = undoCalled && resource.TargetObjectId == 0;
			bool redoCalled = history.Redo();
			await WaitFrames(3);
			bool flag = redoCalled && resource.TargetObjectId == targetId;
			bool undoRedo = undone && flag;
			Require(undoRedo, "ObjectId direct edit did not round-trip through UndoRedo.");
			await EnterId(input, 9223372036854775807L.ToString(CultureInfo.InvariantCulture));
			bool invalidSafe = resource.TargetObjectId == 9223372036854775807L && jump.Disabled && status.Text.Contains("无法确认");
			Require(invalidSafe, "Out-of-range ObjectId was not presented as a safe unverified state.");
			jump.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			invalidSafe &= inspector.CurrentObject == resource;
			Require(inspector.CurrentObject == resource, "Invalid ObjectId jump changed the inspected object.");
			await EnterId(input, "");
			bool emptySafe = resource.TargetObjectId == 0L && jump.Disabled && status.Text.Contains("未指定");
			Require(emptySafe, "Empty ObjectId was not normalized to a safe unassigned state.");
			Resource resource2 = new Resource();
			long referenceId = (long)resource2.GetInstanceId();
			await EnterId(input, referenceId.ToString(CultureInfo.InvariantCulture));
			bool refCounted = referenceId < 0 && resource.TargetObjectId == referenceId && !jump.Disabled && status.Text.Contains("Resource");
			Require(refCounted, "Reference-counted ObjectId bit pattern was treated as an invalid negative number.");
			await EnterId(input, targetId.ToString(CultureInfo.InvariantCulture));
			jump.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			bool flag2 = inspector.CurrentObject == target;
			Require(flag2, "Valid ObjectId jump did not navigate the real Inspector.");
			bool value = FindAncestorWindow(inspector) != null;
			GD.Print($"[MOD_EDITOR_INSPECTOR_OBJECT_ID_PROBE] window={value} directEdit={directEdit} undoRedo={undoRedo} validState={validState} invalidSafe={invalidSafe} emptySafe={emptySafe} refCounted={refCounted} jumped={flag2} failures={_failures.Count} elapsedMs={_watch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task EnterId(LineEdit input, string text)
	{
		input.GrabFocus();
		await WaitFrames(1);
		input.Text = text;
		input.EmitSignal(LineEdit.SignalName.TextChanged, text);
		await WaitFrames(2);
		input.ReleaseFocus();
		await WaitFrames(3);
	}

	private async Task<XWInspector> WaitForInspector(int frames)
	{
		for (int i = 0; i < frames; i++)
		{
			if (XWEditorInterface.Instance?.GetInspector() is XWInspector xWInspector && GodotObject.IsInstanceValid(xWInspector))
			{
				return xWInspector;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static T FindNode<T>(Node root) where T : Node
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
			T val = FindNode<T>(child);
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
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_OBJECT_ID_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_OBJECT_ID_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
