using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorStateGuardRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	private string _probeRoot = "";

	public override async void _Ready()
	{
		bool undoRedo = false;
		bool saveReload = false;
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
			Require(f3, "F3 did not initialize state_guard_editor.");
			if (!f3)
			{
				Finish();
				return;
			}
			_probeRoot = ProjectSettings.GlobalizePath("user://StateGuardRuntimeProbe");
			if (Directory.Exists(_probeRoot))
			{
				Directory.Delete(_probeRoot, recursive: true);
			}
			XWModProjectLayout.EnsureProjectLayout(_probeRoot);
			string text = Path.Combine(_probeRoot, "Resources", "StateMachines", "Conditions");
			string[] array = new string[6] { "new-state-property-guard", "new-expression-guard", "new-state-active-guard", "new-all-of-guard", "new-any-of-guard", "new-not-guard" };
			bool actions = true;
			string[] array2 = array;
			foreach (string id in array2)
			{
				actions &= HasAction(text, _probeRoot, id);
			}
			Require(actions, "StateMachine/Conditions is missing one or more visual create actions.");
			System.Collections.Generic.Dictionary<string, XWTemplateLibrary.TemplateCreateResult> dictionary = new System.Collections.Generic.Dictionary<string, XWTemplateLibrary.TemplateCreateResult>();
			array2 = array;
			foreach (string text2 in array2)
			{
				dictionary[text2] = XWResourceCreateRoute.CreateFromAction(text2, text, ToResourceName(text2));
			}
			string propertyPath = LocalPath(dictionary["new-state-property-guard"]);
			string text3 = LocalPath(dictionary["new-expression-guard"]);
			string text4 = LocalPath(dictionary["new-state-active-guard"]);
			string text5 = LocalPath(dictionary["new-all-of-guard"]);
			string text6 = LocalPath(dictionary["new-any-of-guard"]);
			string text7 = LocalPath(dictionary["new-not-guard"]);
			StateMachineGuardDefinition propertyGuard = Load<StateMachineGuardDefinition>(propertyPath);
			ExpressionGuard expressionGuard = Load<ExpressionGuard>(text3);
			StateIsActiveGuard stateIsActiveGuard = Load<StateIsActiveGuard>(text4);
			AllOfGuard allGuard = Load<AllOfGuard>(text5);
			AnyOfGuard anyOfGuard = Load<AnyOfGuard>(text6);
			NotGuard notGuard = Load<NotGuard>(text7);
			bool created = GodotObject.IsInstanceValid(propertyGuard) && propertyGuard.ComparedProperty == new StringName("state") && propertyGuard.ExpectedValue.AsString() == "ready" && GodotObject.IsInstanceValid(expressionGuard) && expressionGuard.expression == "health <= 0" && GodotObject.IsInstanceValid(stateIsActiveGuard) && stateIsActiveGuard.state == new NodePath("../Playing") && GodotObject.IsInstanceValid(allGuard) && GodotObject.IsInstanceValid(anyOfGuard) && GodotObject.IsInstanceValid(notGuard);
			Require(created, "One or more strong StateGuard templates failed to load with typed defaults.");
			Resource[] array3 = new Resource[6] { propertyGuard, expressionGuard, stateIsActiveGuard, allGuard, anyOfGuard, notGuard };
			string[] array4 = new string[6] { propertyPath, text3, text4, text5, text6, text7 };
			bool routes = true;
			for (int j = 0; j < array3.Length; j++)
			{
				routes &= RoutesToStateGuard(array3[j], array4[j]);
			}
			Require(routes, "One or more guard resources did not route to state_guard_editor.");
			Node inspectorSentinel = new Node
			{
				Name = "StateGuardInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool inspectorBaseline = InspectorTargets(inspector, inspectorSentinel);
			Require(inspectorBaseline, "Could not establish raw Inspector sentinel.");
			XWEditorInterface.Instance.EditResource(propertyGuard);
			XWEditorInterface.Instance.FocusPanel("state_guard_editor");
			await WaitFrames(7);
			XWStateGuardVisualResourceEditor editor = XWEditorInterface.Instance.GetResourceEditor("state_guard_editor") as XWStateGuardVisualResourceEditor;
			LineEdit propertyEdit = Find<LineEdit>(editor, "ComparedPropertyEdit");
			LineEdit instance = Find<LineEdit>(editor, "ExpectedText");
			LineEdit lineEdit = Find<LineEdit>(editor, "ActualText");
			Button instance2 = Find<Button>(editor, "OperatorGreater");
			bool direct = GodotObject.IsInstanceValid(editor) && GodotObject.IsInstanceValid(propertyEdit) && GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(lineEdit) && GodotObject.IsInstanceValid(instance2) && HasAll(editor, "ExpectedTypeString", "ExpectedTypeFloat", "ExpectedTypeBool", "NegateResult", "PreviewCanvas", "ResultBadge", "WorkbenchFlow") && InspectorIsHidden(editor) && editor.EditingGuard == propertyGuard;
			Require(direct, "StateGuard did not mount its complete Inspector-free direct visual workbench.");
			if (direct)
			{
				lineEdit.Text = "ready";
				lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, "ready");
				await WaitFrames(2);
				bool condition = (editor.GuardPreviewCanvas?.PreviewResult == true) ?? false;
				Require(condition, "StateGuard preview did not evaluate the current value against the expected value.");
				_history.ClearHistory();
				EditText(propertyEdit, "health");
				await WaitFrames(3);
				bool applied = propertyGuard.ComparedProperty == new StringName("health") && _history.HasUndo();
				bool undone = _history.Undo();
				await WaitFrames(3);
				undone &= propertyGuard.ComparedProperty == new StringName("state");
				bool redone = _history.Redo();
				await WaitFrames(3);
				redone &= propertyGuard.ComparedProperty == new StringName("health");
				undoRedo = applied && undone && redone;
				Require(undoRedo, "StateGuard property edit did not round-trip through shared Undo/Redo.");
				Button saveGuardButton = editor.SaveGuardButton;
				bool saveButtonWasValid = GodotObject.IsInstanceValid(saveGuardButton);
				ulong saveButtonInstanceId = (saveButtonWasValid ? saveGuardButton.GetInstanceId() : 0);
				saveGuardButton?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(7);
				Button button = (XWEditorInterface.Instance.GetResourceEditor("state_guard_editor") as XWStateGuardVisualResourceEditor)?.SaveGuardButton;
				bool flag = GodotObject.IsInstanceValid(button) && button.GetInstanceId() != saveButtonInstanceId;
				StateMachineGuardDefinition stateMachineGuardDefinition = Load<StateMachineGuardDefinition>(propertyPath);
				saveReload = saveButtonWasValid && flag && GodotObject.IsInstanceValid(stateMachineGuardDefinition) && stateMachineGuardDefinition.ComparedProperty == new StringName("health") && stateMachineGuardDefinition.ExpectedValue.AsString() == "ready";
				Require(saveReload, "StateGuard direct edit did not survive toolbar Save and cache-ignore reload. " + $"buttonBefore={saveButtonWasValid} buttonRebuilt={flag} " + $"reloaded={GodotObject.IsInstanceValid(stateMachineGuardDefinition)} " + $"property={stateMachineGuardDefinition?.ComparedProperty} expected={stateMachineGuardDefinition?.ExpectedValue.AsString()} path={propertyPath}");
			}
			XWEditorInterface.Instance.EditResource(allGuard);
			XWEditorInterface.Instance.FocusPanel("state_guard_editor");
			await WaitFrames(6);
			editor = XWEditorInterface.Instance.GetResourceEditor("state_guard_editor") as XWStateGuardVisualResourceEditor;
			Button addExpression = Find<Button>(editor, "AddExpressionGuard");
			_history.ClearHistory();
			addExpression?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(4);
			int num;
			if (GodotObject.IsInstanceValid(addExpression))
			{
				Array<Guard> guards = allGuard.guards;
				if (guards != null && guards.Count == 1 && allGuard.guards[0] is ExpressionGuard)
				{
					XWStateGuardVisualResourceEditor xWStateGuardVisualResourceEditor = editor;
					if (xWStateGuardVisualResourceEditor != null && xWStateGuardVisualResourceEditor.CompositionItemCount == 1)
					{
						num = (_history.HasUndo() ? 1 : 0);
						goto IL_0e4d;
					}
				}
			}
			num = 0;
			goto IL_0e4d;
			IL_0e4d:
			bool childAdded = (byte)num != 0;
			bool childUndone = _history.Undo();
			await WaitFrames(4);
			bool num2 = childUndone;
			Array<Guard> guards2 = allGuard.guards;
			childUndone = num2 & (guards2 != null && guards2.Count == 0);
			bool childRedone = _history.Redo();
			await WaitFrames(4);
			bool num3 = childRedone;
			Array<Guard> guards3 = allGuard.guards;
			childRedone = num3 & (guards3 != null && guards3.Count == 1 && allGuard.guards[0] is ExpressionGuard);
			bool composite = childAdded && childUndone && childRedone;
			Require(composite, "Composite guard child did not add and round-trip through shared Undo/Redo.");
			bool inspectorUntouched = InspectorTargets(inspector, inspectorSentinel);
			Require(inspectorUntouched, "StateGuard editor replaced the raw Inspector target.");
			editor?.Hide();
			await WaitFrames(3);
			bool flag2 = GodotObject.IsInstanceValid(editor) && editor.ProcessMode == ProcessModeEnum.Disabled && GodotObject.IsInstanceValid(editor.GuardPreviewCanvas) && !editor.GuardPreviewCanvas.IsProcessing();
			Require(flag2, "Hidden StateGuard editor kept processing.");
			GD.Print($"[MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE] f3={f3} actions={actions} created={created} routes={routes} direct={direct} undoRedo={undoRedo} saveReload={saveReload} composite={composite} inspectorBaseline={inspectorBaseline} inspectorUntouched={inspectorUntouched} hiddenStopped={flag2} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetInspector() is XWInspector && instance.GetResourceEditor("state_guard_editor") is XWStateGuardVisualResourceEditor)
			{
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static bool HasAction(string directory, string root, string id)
	{
		foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directory, root))
		{
			if (item.Id == id)
			{
				return true;
			}
		}
		return false;
	}

	private static string ToResourceName(string actionId)
	{
		return actionId.Replace("new-", "", StringComparison.Ordinal).Replace("-", "_", StringComparison.Ordinal);
	}

	private static string LocalPath(XWTemplateLibrary.TemplateCreateResult result)
	{
		if (!result.Success || string.IsNullOrWhiteSpace(result.CreatedPath))
		{
			return "";
		}
		return ProjectSettings.LocalizePath(result.CreatedPath).Replace('\\', '/');
	}

	private static T Load<T>(string path) where T : Resource
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Ignore);
		}
		return null;
	}

	private static bool RoutesToStateGuard(Resource resource, string path)
	{
		if (GodotObject.IsInstanceValid(resource) && XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor) && descriptor?.Category == "StateGuard")
		{
			return descriptor.DockKey == "state_guard_editor";
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

	private static bool InspectorIsHidden(XWGenericVisualResourceEditor editor)
	{
		PanelContainer panelContainer = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		if (GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible)
		{
			return editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
		}
		return false;
	}

	private static bool HasAll(Node root, params string[] names)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return false;
		}
		foreach (string pattern in names)
		{
			if (!GodotObject.IsInstanceValid(root.FindChild(pattern, recursive: true, owned: false)))
			{
				return false;
			}
		}
		return true;
	}

	private static T Find<T>(Node root, string name) where T : Node
	{
		return root?.FindChild(name, recursive: true, owned: false) as T;
	}

	private static Button FindButtonByText(Node root, string text)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.Text == text)
			{
				return button;
			}
		}
		return null;
	}

	private static void EditText(LineEdit edit, string text)
	{
		if (GodotObject.IsInstanceValid(edit))
		{
			edit.EmitSignal(Control.SignalName.FocusEntered);
			edit.Text = text;
			edit.EmitSignal(LineEdit.SignalName.TextChanged, text);
			edit.EmitSignal(Control.SignalName.FocusExited);
		}
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
			GD.PrintErr("[MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_STATE_GUARD_RUNTIME_PROBE_FAILURE] " + failure);
		}
		if (!string.IsNullOrWhiteSpace(_probeRoot) && Directory.Exists(_probeRoot))
		{
			try
			{
				Directory.Delete(_probeRoot, recursive: true);
			}
			catch
			{
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
