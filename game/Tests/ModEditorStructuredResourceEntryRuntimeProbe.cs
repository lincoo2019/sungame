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

public partial class ModEditorStructuredResourceEntryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		_ = 3;
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
			Require(f3, "F3 did not initialize the structured-resource editors.");
			if (!f3)
			{
				Finish();
				return;
			}
			string text = ProjectSettings.GlobalizePath("user://StructuredResourceEntryProbe");
			string directoryPath = Path.Combine(text, "Resources", "StateMachines");
			string directoryPath2 = Path.Combine(text, "Resources", "CharacterComponents");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-state-machine", directoryPath, "EntryStateMachine");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult2 = XWResourceCreateRoute.CreateFromAction("new-character-component-set", directoryPath2, "EntryComponentSet");
			string path = ProjectSettings.LocalizePath(templateCreateResult.CreatedPath).Replace('\\', '/');
			string componentPath = ProjectSettings.LocalizePath(templateCreateResult2.CreatedPath).Replace('\\', '/');
			StateMachineDefinition stateMachineDefinition = (templateCreateResult.Success ? ResourceLoader.Load<StateMachineDefinition>(path, "", ResourceLoader.CacheMode.Ignore) : null);
			CharacterComponentSet componentSet = (templateCreateResult2.Success ? ResourceLoader.Load<CharacterComponentSet>(componentPath, "", ResourceLoader.CacheMode.Ignore) : null);
			bool stateCreated = GodotObject.IsInstanceValid(stateMachineDefinition) && stateMachineDefinition.DefinitionId == "EntryStateMachine" && stateMachineDefinition.RootStateId == "EntryStateMachine.root" && stateMachineDefinition.States.Count == 1 && stateMachineDefinition.States[0].StableId == stateMachineDefinition.RootStateId;
			bool componentCreated = GodotObject.IsInstanceValid(componentSet) && componentSet.Components != null && componentSet.Components.Count == 0 && componentSet.RemovedInstanceIds != null;
			Require(stateCreated, "State-machine create entry produced an invalid resource: " + templateCreateResult.Error);
			Require(componentCreated, "Character-component create entry produced an invalid resource: " + templateCreateResult2.Error);
			XWVisualEditorDescriptor descriptor;
			bool stateRoute = XWResourceEditorRegistry.TryGetEditor(stateMachineDefinition, path, out descriptor) && descriptor?.DockKey == "state_machine_editor";
			XWVisualEditorDescriptor descriptor2;
			bool componentRoute = XWResourceEditorRegistry.TryGetEditor(componentSet, componentPath, out descriptor2) && descriptor2?.DockKey == "character_component_editor";
			Require(stateRoute, "Created state machine did not route to state_machine_editor.");
			Require(componentRoute, "Created component set did not route to character_component_editor.");
			Node inspectorSentinel = new Node
			{
				Name = "StructuredEntryInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool inspectorBaseline = InspectorTargets(inspector, inspectorSentinel);
			Require(inspectorBaseline, "Structured-resource probe could not establish the raw Inspector sentinel.");
			(bool, bool, bool) tuple = await ProbeStateMachine(stateMachineDefinition, path);
			bool stateDirect = tuple.Item1;
			bool stateUndoRedo = tuple.Item2;
			bool stateSave = tuple.Item3;
			(bool, bool, bool) obj = await ProbeComponentSet(componentSet, componentPath);
			bool item = obj.Item1;
			bool item2 = obj.Item2;
			bool item3 = obj.Item3;
			bool flag = InspectorTargets(inspector, inspectorSentinel);
			Require(flag, "Structured-resource entry replaced the raw Inspector object.");
			GD.Print($"[MOD_EDITOR_STRUCTURED_ENTRY_PROBE] f3={f3} stateCreated={stateCreated} stateRoute={stateRoute} stateDirect={stateDirect} stateUndoRedo={stateUndoRedo} stateSave={stateSave} componentCreated={componentCreated} componentRoute={componentRoute} componentDirect={item} componentUndoRedo={item2} componentSave={item3} inspectorBaseline={inspectorBaseline} inspectorUntouched={flag} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeStateMachine(StateMachineDefinition definition, string path)
	{
		XWEditorInterface.Instance.EditResource(definition);
		XWEditorInterface.Instance.FocusPanel("state_machine_editor");
		await WaitFrames(5);
		XWStateMachineVisualResourceEditor xWStateMachineVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("state_machine_editor") as XWStateMachineVisualResourceEditor;
		bool direct = GodotObject.IsInstanceValid(xWStateMachineVisualResourceEditor) && GodotObject.IsInstanceValid(xWStateMachineVisualResourceEditor.GraphSurface) && xWStateMachineVisualResourceEditor.GraphSurface.Definition == definition && InspectorIsHidden(xWStateMachineVisualResourceEditor);
		Require(direct, "State-machine entry did not mount its inspector-free graph surface.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		string addedId = xWStateMachineVisualResourceEditor.GraphSurface.GraphController.AddState("入口验证", StateMachineStateKind.Atomic, definition.RootStateId);
		await WaitFrames(2);
		bool applied = !string.IsNullOrWhiteSpace(addedId) && definition.States.Count == 2 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && definition.States.Count == 1;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && definition.States.Count == 2;
		bool flag = applied && undone && redone;
		Require(flag, "State-machine graph edit did not round-trip through global UndoRedo.");
		StateMachineDefinition stateMachineDefinition = ((ResourceSaver.Save(definition, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<StateMachineDefinition>(path, "", ResourceLoader.CacheMode.Ignore) : null);
		bool flag2 = GodotObject.IsInstanceValid(stateMachineDefinition) && stateMachineDefinition.States.Count == 2;
		Require(flag2, "State-machine resource did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeComponentSet(CharacterComponentSet componentSet, string path)
	{
		XWEditorInterface.Instance.EditResource(componentSet);
		XWEditorInterface.Instance.FocusPanel("character_component_editor");
		await WaitFrames(5);
		XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor = XWEditorInterface.Instance.GetResourceEditor("character_component_editor") as XWCharacterComponentVisualResourceEditor;
		LineEdit lineEdit = xWCharacterComponentVisualResourceEditor?.FindChild("ResourceNameEdit", recursive: true, owned: false) as LineEdit;
		bool direct = GodotObject.IsInstanceValid(xWCharacterComponentVisualResourceEditor) && GodotObject.IsInstanceValid(lineEdit) && InspectorIsHidden(xWCharacterComponentVisualResourceEditor);
		Require(direct, "Character-component entry did not mount its inspector-free puzzle surface.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		string originalName = componentSet.ResourceName;
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		lineEdit.Text = "组件入口验证";
		lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, lineEdit.Text);
		lineEdit.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool applied = componentSet.ResourceName == "组件入口验证" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && componentSet.ResourceName == originalName;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && componentSet.ResourceName == "组件入口验证";
		bool flag = applied && undone && redone;
		Require(flag, "Character-component header edit did not round-trip through global UndoRedo.");
		CharacterComponentSet characterComponentSet = ((ResourceSaver.Save(componentSet, path, ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<CharacterComponentSet>(path, "", ResourceLoader.CacheMode.Ignore) : null);
		bool flag2 = GodotObject.IsInstanceValid(characterComponentSet) && characterComponentSet.ResourceName == "组件入口验证";
		Require(flag2, "Character-component resource did not survive save/reload.");
		return (Direct: direct, UndoRedo: flag, Save: flag2);
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

	private static bool InspectorTargets(XWInspector inspector, GodotObject expected)
	{
		if (GodotObject.IsInstanceValid(inspector) && GodotObject.IsInstanceValid(inspector.CurrentObject) && GodotObject.IsInstanceValid(expected))
		{
			return inspector.CurrentObject.GetInstanceId() == expected.GetInstanceId();
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetInspector() is XWInspector && instance.GetResourceEditor("state_machine_editor") is XWStateMachineVisualResourceEditor && instance.GetResourceEditor("character_component_editor") is XWCharacterComponentVisualResourceEditor)
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

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_STRUCTURED_ENTRY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_STRUCTURED_ENTRY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
