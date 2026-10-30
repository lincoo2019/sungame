using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorInspectorNestedContinuousRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWInspector _inspector;

	private XWUndoRedoManager _history;

	private int _changeNotifications;

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
			bool flag = await WaitForInspector(900);
			Require(flag, "F3 did not initialize the real Inspector within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_inspector) != null;
			Require(window, "Inspector is not mounted under the F3 ModEditor window.");
			_inspector.ObjectPropertyChanged += OnObjectPropertyChanged;
			InspectorNestedContinuousProbeResource resource = new InspectorNestedContinuousProbeResource();
			await Inspect(resource);
			bool arraySingleCommit = await ProbeArray(resource);
			bool dictionarySingleCommit = await ProbeTypedDictionary(resource);
			bool value = await ProbeNestedSwitchCancel(resource);
			GD.Print($"[MOD_EDITOR_INSPECTOR_NESTED_CONTINUOUS_PROBE] window={window} arraySingleCommit={arraySingleCommit} dictionarySingleCommit={dictionarySingleCommit} typedDictionaryPreserved={IsTypedDictionaryValid(resource)} switchCancel={value} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> ProbeArray(InspectorNestedContinuousProbeResource resource)
	{
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = await ExpandProperty("ProbeNames", "ArrayButton");
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase2 = FindInnerEditor(xWInspectorPropertyEditorBase, Variant.Type.String);
		LineEdit lineEdit = FindDescendant<LineEdit>(xWInspectorPropertyEditorBase2);
		Require(GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase) && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase2) && GodotObject.IsInstanceValid(lineEdit), "Expanded ProbeNames did not create a string element editor.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return false;
		}
		_history.ClearHistory();
		_changeNotifications = 0;
		int beforeVersion = _history.GetVersion();
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		bool sessionBegan = xWInspectorPropertyEditorBase.IsContinuousEditActive && xWInspectorPropertyEditorBase2.ContinuousEditOwner == xWInspectorPropertyEditorBase;
		string[] array = new string[3] { "g", "game", "game-ready" };
		for (int i = 0; i < array.Length; i++)
		{
			string text = (lineEdit.Text = array[i]);
			lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, text);
		}
		await WaitFrames(2);
		bool previewOnly = resource.ProbeNames[0] == "game-ready" && sessionBegan && _history.GetVersion() == beforeVersion && !_history.HasUndo() && _changeNotifications == 0;
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, "game-ready");
		await WaitFrames(3);
		bool committedOnce = _history.GetVersion() - beforeVersion == 1 && _history.HasUndo() && _changeNotifications == 1;
		bool undoCalled = _history.Undo();
		await WaitFrames(2);
		bool undone = undoCalled && resource.ProbeNames[0] == "alpha";
		bool redoCalled = _history.Redo();
		await WaitFrames(2);
		bool flag = redoCalled && resource.ProbeNames[0] == "game-ready";
		bool flag2 = previewOnly && committedOnce && undone && flag;
		GD.Print($"[MOD_EDITOR_INSPECTOR_NESTED_CONTINUOUS_DETAIL] array sessionBegan={sessionBegan} previewOnly={previewOnly} committedOnce={committedOnce} undone={undone} redone={flag} notifications={_changeNotifications}");
		Require(flag2, "Array element input did not preview silently and single-commit the root Array property.");
		return flag2;
	}

	private async Task<bool> ProbeTypedDictionary(InspectorNestedContinuousProbeResource resource)
	{
		await Inspect(resource);
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = await ExpandProperty("ProbeWeights", "DictButton");
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase2 = FindInnerEditor(xWInspectorPropertyEditorBase, Variant.Type.Int);
		SpinBox spinBox = FindDescendant<SpinBox>(xWInspectorPropertyEditorBase2);
		Require(GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase) && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase2) && GodotObject.IsInstanceValid(spinBox), "Expanded ProbeWeights did not create an integer value editor.");
		if (!GodotObject.IsInstanceValid(spinBox))
		{
			return false;
		}
		_history.ClearHistory();
		_changeNotifications = 0;
		int beforeVersion = _history.GetVersion();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		bool sessionBegan = xWInspectorPropertyEditorBase.IsContinuousEditActive && xWInspectorPropertyEditorBase2.ContinuousEditOwner == xWInspectorPropertyEditorBase;
		double[] array = new double[3] { 7.0, 11.0, 19.0 };
		foreach (double num in array)
		{
			spinBox.SetValueNoSignal(num);
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, num);
		}
		await WaitFrames(2);
		bool previewOnly = GetWeight(resource, "pea") == 19 && sessionBegan && _history.GetVersion() == beforeVersion && !_history.HasUndo() && _changeNotifications == 0 && IsTypedDictionaryValid(resource);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool committedOnce = _history.GetVersion() - beforeVersion == 1 && _history.HasUndo() && _changeNotifications == 1 && IsTypedDictionaryValid(resource);
		bool undoCalled = _history.Undo();
		await WaitFrames(2);
		bool undone = undoCalled && GetWeight(resource, "pea") == 3 && IsTypedDictionaryValid(resource);
		bool redoCalled = _history.Redo();
		await WaitFrames(2);
		bool flag = redoCalled && GetWeight(resource, "pea") == 19 && IsTypedDictionaryValid(resource);
		bool flag2 = previewOnly && committedOnce && undone && flag;
		GD.Print($"[MOD_EDITOR_INSPECTOR_NESTED_CONTINUOUS_DETAIL] dictionary sessionBegan={sessionBegan} previewOnly={previewOnly} committedOnce={committedOnce} undone={undone} redone={flag} typed={IsTypedDictionaryValid(resource)} notifications={_changeNotifications}");
		Require(flag2, "Typed Dictionary value input did not single-commit or preserve key/value typing through UndoRedo.");
		return flag2;
	}

	private async Task<bool> ProbeNestedSwitchCancel(InspectorNestedContinuousProbeResource resource)
	{
		await Inspect(resource);
		XWInspectorPropertyEditorBase root = await ExpandProperty("ProbeNames", "ArrayButton");
		LineEdit lineEdit = FindDescendant<LineEdit>(FindInnerEditor(root, Variant.Type.String));
		Require(GodotObject.IsInstanceValid(lineEdit), "ProbeNames nested editor is missing before switch-cancel test.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return false;
		}
		string before = resource.ProbeNames[0];
		_history.ClearHistory();
		_changeNotifications = 0;
		lineEdit.EmitSignal(Control.SignalName.FocusEntered);
		lineEdit.Text = "discard-me";
		lineEdit.EmitSignal(LineEdit.SignalName.TextChanged, "discard-me");
		await WaitFrames(1);
		bool previewApplied = resource.ProbeNames[0] == "discard-me" && root.IsContinuousEditActive;
		InspectorNestedContinuousProbeResource obj = new InspectorNestedContinuousProbeResource();
		_inspector.EditObject(obj);
		await WaitFrames(5);
		bool flag = previewApplied && resource.ProbeNames[0] == before && !_history.HasUndo() && _changeNotifications == 0;
		Require(flag, "Switching inspected objects did not cancel and restore an unfinished nested Array preview.");
		return flag;
	}

	private async Task<XWInspectorPropertyEditorBase> ExpandProperty(string propertyName, string buttonName)
	{
		XWInspectorPropertyEditorBase root = FindPropertyEditor(propertyName);
		Require(GodotObject.IsInstanceValid(root), propertyName + " root editor was not created.");
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		Button nodeOrNull = root.GetNodeOrNull<Button>("%" + buttonName);
		Require(GodotObject.IsInstanceValid(nodeOrNull), $"{propertyName} ({root.GetType().Name}) has no {buttonName} visual expansion button.");
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			return root;
		}
		nodeOrNull.ButtonPressed = true;
		nodeOrNull.EmitSignal(BaseButton.SignalName.Toggled, true);
		await WaitFrames(5);
		return root;
	}

	private XWInspectorPropertyEditorBase FindInnerEditor(Node root, Variant.Type expectedType)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is XWInspectorPropertyEditorBase { IsInner: not false } xWInspectorPropertyEditorBase && xWInspectorPropertyEditorBase.ContinuousEditOwner == root && xWInspectorPropertyEditorBase.GetPropertyValue().VariantType == expectedType)
			{
				return xWInspectorPropertyEditorBase;
			}
		}
		return null;
	}

	private static bool IsTypedDictionaryValid(InspectorNestedContinuousProbeResource resource)
	{
		if (resource.ProbeWeights == null || !resource.ProbeWeights.ContainsKey("pea") || !resource.ProbeWeights.ContainsKey("sun") || GetWeight(resource, "sun") != 5)
		{
			return false;
		}
		foreach (Variant key in resource.ProbeWeights.Keys)
		{
			if (key.VariantType != Variant.Type.String || resource.ProbeWeights[key].VariantType != Variant.Type.Int)
			{
				return false;
			}
		}
		return true;
	}

	private static int GetWeight(InspectorNestedContinuousProbeResource resource, string key)
	{
		if (resource.ProbeWeights == null || !resource.ProbeWeights.ContainsKey(key))
		{
			return -2147483648;
		}
		return resource.ProbeWeights[key].AsInt32();
	}

	private async Task Inspect(GodotObject obj)
	{
		_inspector.EditObject(obj);
		XWEditorInterface.Instance.FocusPanel("inspector");
		await WaitFrames(5);
	}

	private XWInspectorPropertyEditorBase FindPropertyEditor(string propertyName)
	{
		foreach (Node item in _inspector.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is XWInspectorPropertyEditorBase { IsInner: false } xWInspectorPropertyEditorBase && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.Property) && xWInspectorPropertyEditorBase.Property.PropName.ToString() == propertyName)
			{
				return xWInspectorPropertyEditorBase;
			}
		}
		return null;
	}

	private static T FindDescendant<T>(Node root) where T : Node
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return null;
		}
		foreach (Node item in root.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is T result)
			{
				return result;
			}
		}
		return null;
	}

	private async Task<bool> WaitForInspector(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (XWEditorInterface.Instance?.GetInspector() is XWInspector xWInspector && GodotObject.IsInstanceValid(xWInspector))
			{
				_inspector = xWInspector;
				_history = XWEditorInterface.Instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private void OnObjectPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		_changeNotifications++;
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
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_NESTED_CONTINUOUS_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (_failures.Count > 0)
		{
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_INSPECTOR_NESTED_CONTINUOUS_PROBE_FAILURE] " + failure);
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
