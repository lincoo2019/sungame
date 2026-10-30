using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorEventResourceEntryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 5;
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
			bool f3 = await WaitForEditor(900);
			Require(f3, "F3 did not initialize the event-resource editors.");
			if (!f3)
			{
				Finish();
				return;
			}
			string text = ProjectSettings.GlobalizePath("user://EventResourceEntryProbe");
			string text2 = Path.Combine(text, "Resources", "UnlockConditions");
			string text3 = Path.Combine(text, "Resources", "PacketEvents");
			string text4 = Path.Combine(text, "Resources", "ToolEvents");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			bool actions = HasAction(text2, text, "new-unlock-condition") && HasAction(text3, text, "new-packet-event") && HasAction(text4, text, "new-tool-event");
			Require(actions, "One or more event-resource directories have no create action.");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-unlock-condition", text2, "EntryUnlockCondition");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = XWResourceCreateRoute.CreateFromAction("new-packet-event", text3, "EntryPacketEvent");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult3 = XWResourceCreateRoute.CreateFromAction("new-tool-event", text4, "EntryToolEvent");
			string unlockPath = LocalPath(templateCreateResult);
			string packetPath = LocalPath(templateCreateResult2);
			string toolPath = LocalPath(templateCreateResult3);
			UnlockConditionLevelFinishConfig unlock = (templateCreateResult.Success ? ResourceLoader.Load<UnlockConditionLevelFinishConfig>(unlockPath, "", ResourceLoader.CacheMode.Replace) : null);
			CardActionBehaviorChangeCost packetEvent = (templateCreateResult2.Success ? ResourceLoader.Load<CardActionBehaviorChangeCost>(packetPath, "", ResourceLoader.CacheMode.Replace) : null);
			MowerEventCreateSunConfig toolEvent = (templateCreateResult3.Success ? ResourceLoader.Load<MowerEventCreateSunConfig>(toolPath, "", ResourceLoader.CacheMode.Replace) : null);
			bool unlockCreated = GodotObject.IsInstanceValid(unlock) && unlock.ResourceName == "EntryUnlockCondition" && unlock.levelSaveKey == "Unlock";
			bool packetCreated = GodotObject.IsInstanceValid(packetEvent) && packetEvent.ResourceName == "EntryPacketEvent" && packetEvent.method == CardActionBehaviorChangeCost.METHOD.ADD && Math.Abs(packetEvent.value) < 0.001 && packetEvent._min == -1 && packetEvent._max == -1;
			bool toolCreated = GodotObject.IsInstanceValid(toolEvent) && toolEvent.ResourceName == "EntryToolEvent" && toolEvent.num == 25;
			Require(unlockCreated, "Unlock-condition create entry produced an invalid resource: " + templateCreateResult.Error);
			Require(packetCreated, "Packet-event create entry produced an invalid resource: " + templateCreateResult2.Error);
			Require(toolCreated, "Tool-event create entry produced an invalid resource: " + templateCreateResult3.Error);
			XWVisualEditorDescriptor descriptor;
			bool unlockRoute = XWResourceEditorRegistry.TryGetEditor(unlock, unlockPath, out descriptor) && descriptor?.DockKey == "unlock_condition_editor";
			XWVisualEditorDescriptor descriptor2;
			bool packetRoute = XWResourceEditorRegistry.TryGetEditor(packetEvent, packetPath, out descriptor2) && descriptor2?.DockKey == "packet_event_editor";
			XWVisualEditorDescriptor descriptor3;
			bool toolRoute = XWResourceEditorRegistry.TryGetEditor(toolEvent, toolPath, out descriptor3) && descriptor3?.DockKey == "tool_event_editor";
			Require(unlockRoute, "Created unlock condition did not route to unlock_condition_editor.");
			Require(packetRoute, "Created packet event did not route to packet_event_editor.");
			Require(toolRoute, "Created tool event did not route to tool_event_editor.");
			Node inspectorSentinel = new Node
			{
				Name = "EventEntryInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			bool inspectorSeeded = await SeedInspectorSentinel(inspectorSentinel, 120);
			Require(inspectorSeeded, "The global Inspector was not ready to install its sentinel.");
			(bool, bool, bool) tuple = await ProbeUnlock(unlock, unlockPath);
			bool unlockDirect = tuple.Item1;
			bool unlockUndoRedo = tuple.Item2;
			bool unlockSave = tuple.Item3;
			tuple = await ProbePacketEvent(packetEvent, packetPath);
			bool packetDirect = tuple.Item1;
			bool packetUndoRedo = tuple.Item2;
			bool packetSave = tuple.Item3;
			(bool, bool, bool) obj = await ProbeToolEvent(toolEvent, toolPath);
			bool item = obj.Item1;
			bool item2 = obj.Item2;
			bool item3 = obj.Item3;
			XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool flag = inspectorSeeded && GodotObject.IsInstanceValid(xWInspector) && xWInspector.CurrentObject == inspectorSentinel;
			Require(flag, "Event-resource entries replaced the raw Inspector object.");
			GD.Print($"[MOD_EDITOR_EVENT_RESOURCE_ENTRY_PROBE] f3={f3} actions={actions} unlockCreated={unlockCreated} unlockRoute={unlockRoute} unlockDirect={unlockDirect} unlockUndoRedo={unlockUndoRedo} unlockSave={unlockSave} packetCreated={packetCreated} packetRoute={packetRoute} packetDirect={packetDirect} packetUndoRedo={packetUndoRedo} packetSave={packetSave} toolCreated={toolCreated} toolRoute={toolRoute} toolDirect={item} toolUndoRedo={item2} toolSave={item3} inspectorUntouched={flag} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeUnlock(UnlockConditionLevelFinishConfig unlock, string path)
	{
		XWEditorInterface.Instance.EditResource(unlock);
		XWEditorInterface.Instance.FocusPanel("unlock_condition_editor");
		await WaitFrames(5);
		XWUnlockConditionVisualResourceEditor xWUnlockConditionVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("unlock_condition_editor") as XWUnlockConditionVisualResourceEditor;
		LineEdit lineEdit = xWUnlockConditionVisualResourceEditor?.FindChild("LevelFinishKey", recursive: true, owned: false) as LineEdit;
		bool direct = GodotObject.IsInstanceValid(xWUnlockConditionVisualResourceEditor) && GodotObject.IsInstanceValid(lineEdit) && lineEdit.Visible && InspectorIsHidden(xWUnlockConditionVisualResourceEditor);
		Require(direct, "Unlock-condition entry did not mount its inspector-free direct-edit workbench.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		lineEdit.Text = "Level9_9";
		lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		await WaitFrames(2);
		bool applied = unlock.levelSaveKey == "Level9_9" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && unlock.levelSaveKey == "Unlock";
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && unlock.levelSaveKey == "Level9_9";
		bool flag = applied && undone && redone;
		Require(flag, "Unlock-condition direct edit did not round-trip through global UndoRedo.");
		UnlockConditionLevelFinishConfig unlockConditionLevelFinishConfig = ((ResourceSaver.Save(unlock, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<UnlockConditionLevelFinishConfig>(path, "", ResourceLoader.CacheMode.Replace) : null);
		bool flag2 = GodotObject.IsInstanceValid(unlockConditionLevelFinishConfig) && unlockConditionLevelFinishConfig.levelSaveKey == "Level9_9";
		Require(flag2, "Unlock-condition resource did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbePacketEvent(CardActionBehaviorChangeCost packetEvent, string path)
	{
		XWEditorInterface.Instance.EditResource(packetEvent);
		XWEditorInterface.Instance.FocusPanel("packet_event_editor");
		await WaitFrames(5);
		XWPacketEventVisualResourceEditor xWPacketEventVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("packet_event_editor") as XWPacketEventVisualResourceEditor;
		SpinBox spinBox = xWPacketEventVisualResourceEditor?.FindChild("CostValue", recursive: true, owned: false) as SpinBox;
		bool direct = GodotObject.IsInstanceValid(xWPacketEventVisualResourceEditor) && GodotObject.IsInstanceValid(spinBox) && spinBox.Visible && InspectorIsHidden(xWPacketEventVisualResourceEditor);
		Require(direct, "Packet-event entry did not mount its inspector-free direct-edit workbench.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.Value = 37.0;
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool applied = Math.Abs(packetEvent.value - 37.0) < 0.001 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && Math.Abs(packetEvent.value) < 0.001;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && Math.Abs(packetEvent.value - 37.0) < 0.001;
		bool flag = applied && undone && redone;
		Require(flag, "Packet-event direct edit did not round-trip through global UndoRedo.");
		CardActionBehaviorChangeCost cardActionBehaviorChangeCost = ((ResourceSaver.Save(packetEvent, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<CardActionBehaviorChangeCost>(path, "", ResourceLoader.CacheMode.Replace) : null);
		bool flag2 = GodotObject.IsInstanceValid(cardActionBehaviorChangeCost) && Math.Abs(cardActionBehaviorChangeCost.value - 37.0) < 0.001 && cardActionBehaviorChangeCost._min == -1 && cardActionBehaviorChangeCost._max == -1;
		Require(flag2, "Packet-event resource did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeToolEvent(MowerEventCreateSunConfig toolEvent, string path)
	{
		XWEditorInterface.Instance.EditResource(toolEvent);
		XWEditorInterface.Instance.FocusPanel("tool_event_editor");
		await WaitFrames(5);
		XWToolEventVisualResourceEditor xWToolEventVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("tool_event_editor") as XWToolEventVisualResourceEditor;
		SpinBox spinBox = xWToolEventVisualResourceEditor?.FindChild("SunNum", recursive: true, owned: false) as SpinBox;
		bool direct = GodotObject.IsInstanceValid(xWToolEventVisualResourceEditor) && GodotObject.IsInstanceValid(spinBox) && spinBox.Visible && InspectorIsHidden(xWToolEventVisualResourceEditor);
		Require(direct, "Tool-event entry did not mount its inspector-free direct-edit workbench.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.Value = 75.0;
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool applied = toolEvent.num == 75 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && toolEvent.num == 25;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && toolEvent.num == 75;
		bool flag = applied && undone && redone;
		Require(flag, "Tool-event direct edit did not round-trip through global UndoRedo.");
		MowerEventCreateSunConfig mowerEventCreateSunConfig = ((ResourceSaver.Save(toolEvent, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<MowerEventCreateSunConfig>(path, "", ResourceLoader.CacheMode.Replace) : null);
		bool flag2 = GodotObject.IsInstanceValid(mowerEventCreateSunConfig) && mowerEventCreateSunConfig.num == 75;
		Require(flag2, "Tool-event resource did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
	}

	private static bool HasAction(string directory, string root, string actionId)
	{
		foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directory, root))
		{
			if (item.Id == actionId)
			{
				return true;
			}
		}
		return false;
	}

	private static string LocalPath(XWTemplateLibrary.TemplateCreateResult result)
	{
		if (!result.Success || string.IsNullOrWhiteSpace(result.CreatedPath))
		{
			return "";
		}
		return ProjectSettings.LocalizePath(result.CreatedPath).Replace('\\', '/');
	}

	private static bool InspectorIsHidden(XWGenericVisualResourceEditor editor)
	{
		PanelContainer panelContainer = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		if (GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible)
		{
			return editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetResourceEditor("unlock_condition_editor") is XWUnlockConditionVisualResourceEditor && instance.GetResourceEditor("packet_event_editor") is XWPacketEventVisualResourceEditor && instance.GetResourceEditor("tool_event_editor") is XWToolEventVisualResourceEditor)
			{
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
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

	private async Task<bool> SeedInspectorSentinel(Node inspectorSentinel, int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			GodotObject godotObject = XWEditorInterface.Instance?.GetInspector();
			if (godotObject is XWInspector inspector && GodotObject.IsInstanceValid(inspector))
			{
				XWEditorInterface.Instance.InspectObject(inspectorSentinel);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				if (inspector.CurrentObject == inspectorSentinel)
				{
					return true;
				}
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		return false;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_EVENT_RESOURCE_ENTRY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_EVENT_RESOURCE_ENTRY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
