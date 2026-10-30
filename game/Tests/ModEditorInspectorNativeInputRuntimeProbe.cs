using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorInspectorNativeInputRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWInspector _inspector;

	private XWUndoRedoManager _history;

	private int _notifications;

	private string _lastNotification = "";

	[Export(PropertyHint.None, "")]
	public string ProbeTitle { get; set; } = "focus-target";

	[Export(PropertyHint.None, "")]
	public double ProbeSpeed { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public Color ProbeColor { get; set; } = new Color(0.18f, 0.31f, 0.47f, 0.44f);

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
			SendKey(Key.F3);
			bool flag = await WaitForInspector(900);
			Require(flag, "F3 did not initialize the real Inspector.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 Inspector surface did not become visible.");
			if (!flag2)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_inspector) != null;
			_inspector.ObjectPropertyChanged += OnPropertyChanged;
			XWEditorInterface.Instance.InspectObject(this);
			XWEditorInterface.Instance.FocusPanel("inspector");
			await WaitFrames(8);
			var (nativeSpinFocus, singleSpinHistory) = await ProbeNativeSpinInput();
			var (value, value2, value3) = await ProbeNativeColorPopup();
			GD.Print($"[MOD_EDITOR_INSPECTOR_NATIVE_INPUT_PROBE] window={window} nativeSpinFocus={nativeSpinFocus} singleSpinHistory={singleSpinHistory} popupOpened={value} nativeColorPopup={value2} singleColorHistory={value3} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<(bool focus, bool history)> ProbeNativeSpinInput()
	{
		XWInspectorPropertyEditorBase speedEditor = FindPropertyEditor("ProbeSpeed");
		XWInspectorPropertyEditorBase root = FindPropertyEditor("ProbeTitle");
		LineEdit spinLine = FindDescendant<SpinBox>(speedEditor)?.GetLineEdit();
		LineEdit titleLine = FindDescendant<LineEdit>(root);
		Require(GodotObject.IsInstanceValid(spinLine) && GodotObject.IsInstanceValid(titleLine), "SpinBox internal LineEdit or focus target was not created.");
		if (!GodotObject.IsInstanceValid(spinLine) || !GodotObject.IsInstanceValid(titleLine))
		{
			return (focus: false, history: false);
		}
		double before = ProbeSpeed;
		_history.ClearHistory();
		_notifications = 0;
		_lastNotification = "";
		int beforeVersion = _history.GetVersion();
		spinLine.GrabFocus();
		await WaitFrames(2);
		bool focus = spinLine.HasFocus() && speedEditor.IsContinuousEditActive;
		spinLine.SelectAll();
		string text = "12.25";
		for (int i = 0; i < text.Length; i++)
		{
			SendTextCharacter(text[i]);
			await WaitFrames(1);
		}
		await WaitFrames(2);
		double valueAfterTyping = ProbeSpeed;
		string textAfterTyping = spinLine.Text;
		bool historyAfterTyping = _history.HasUndo();
		int notificationsAfterTyping = _notifications;
		bool preview = textAfterTyping == "12.25" && !historyAfterTyping && notificationsAfterTyping == 0;
		titleLine.GrabFocus();
		await WaitFrames(5);
		bool oneAction = _history.GetVersion() - beforeVersion == 1 && _history.HasUndo() && _notifications == 1 && !speedEditor.IsContinuousEditActive;
		bool undo = oneAction && _history.Undo();
		await WaitFrames(3);
		undo = undo && Math.Abs(ProbeSpeed - before) < 0.001 && !_history.HasUndo();
		bool redo = undo && _history.Redo();
		await WaitFrames(3);
		redo = redo && Math.Abs(ProbeSpeed - 12.25) < 0.001;
		bool flag = preview && oneAction && undo && redo;
		GD.Print($"[MOD_EDITOR_INSPECTOR_NATIVE_INPUT_DETAIL] spin focus={focus} preview={preview} oneAction={oneAction} undo={undo} redo={redo} value={ProbeSpeed} typedValue={valueAfterTyping} typedText='{textAfterTyping}' typedHistory={historyAfterTyping} typedNotifications={notificationsAfterTyping} text='{spinLine.Text}' notifications={_notifications} versionDelta={_history.GetVersion() - beforeVersion} active={speedEditor.IsContinuousEditActive}");
		Require(focus, "Real SpinBox LineEdit focus did not begin a continuous edit session.");
		Require(flag, "Real SpinBox keyboard staging/focus migration did not create exactly one undo action.");
		return (focus: focus, history: flag);
	}

	private async Task<(bool opened, bool popup, bool history)> ProbeNativeColorPopup()
	{
		XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase = FindPropertyEditor("ProbeColor");
		XWInspectorPropertyEditorColor colorEditor = xWInspectorPropertyEditorBase as XWInspectorPropertyEditorColor;
		ColorPickerButton picker = FindDescendant<ColorPickerButton>(colorEditor);
		PopupPanel popup = picker?.GetPopup();
		ColorPicker nativePicker = picker?.GetPicker();
		Require(GodotObject.IsInstanceValid(colorEditor) && GodotObject.IsInstanceValid(picker) && GodotObject.IsInstanceValid(popup) && GodotObject.IsInstanceValid(nativePicker), "Native color picker controls were not created.");
		if (!GodotObject.IsInstanceValid(picker) || !GodotObject.IsInstanceValid(popup) || !GodotObject.IsInstanceValid(nativePicker))
		{
			return (opened: false, popup: false, history: false);
		}
		Color before = ProbeColor;
		_history.ClearHistory();
		_notifications = 0;
		_lastNotification = "";
		int beforeVersion = _history.GetVersion();
		picker.GrabFocus();
		await WaitFrames(1);
		SendKey(Key.Enter);
		await WaitFrames(4);
		bool opened = popup.Visible && colorEditor.IsContinuousEditActive;
		if (!opened)
		{
			picker.EmitSignal(BaseButton.SignalName.Pressed);
			popup.PopupCentered();
			await WaitFrames(3);
			opened = popup.Visible && colorEditor.IsContinuousEditActive;
		}
		bool activeAtOpen = colorEditor.IsContinuousEditActive;
		int versionAtOpen = _history.GetVersion() - beforeVersion;
		int notificationsAtOpen = _notifications;
		string notificationAtOpen = _lastNotification;
		Color color = new Color(0.34f, 0.72f, 0.29f, 0.51f);
		Color final = new Color(0.91f, 0.28f, 0.16f, 0.68f);
		nativePicker.Color = color;
		nativePicker.EmitSignal(ColorPicker.SignalName.ColorChanged, color);
		await WaitFrames(2);
		bool activeAfterStep1 = colorEditor.IsContinuousEditActive;
		int versionAfterStep1 = _history.GetVersion() - beforeVersion;
		int notificationsAfterStep1 = _notifications;
		nativePicker.Color = final;
		nativePicker.EmitSignal(ColorPicker.SignalName.ColorChanged, final);
		await WaitFrames(3);
		bool preview = ProbeColor.IsEqualApprox(final) && !_history.HasUndo() && _notifications == 0;
		popup.Hide();
		await WaitFrames(6);
		bool popupClosed = !popup.Visible && !colorEditor.IsContinuousEditActive;
		bool oneAction = _history.GetVersion() - beforeVersion == 1 && _history.HasUndo() && _notifications == 1;
		bool undo = oneAction && _history.Undo();
		await WaitFrames(3);
		undo = undo && ProbeColor.IsEqualApprox(before) && !_history.HasUndo();
		bool redo = undo && _history.Redo();
		await WaitFrames(3);
		redo = redo && ProbeColor.IsEqualApprox(final);
		bool flag = preview && popupClosed;
		bool flag2 = flag && oneAction && undo && redo;
		GD.Print($"[MOD_EDITOR_INSPECTOR_NATIVE_INPUT_DETAIL] color opened={opened} preview={preview} popupClosed={popupClosed} oneAction={oneAction} undo={undo} redo={redo} value={ProbeColor} notifications={_notifications} versionDelta={_history.GetVersion() - beforeVersion} activeAtOpen={activeAtOpen} openVersion={versionAtOpen} openNotifications={notificationsAtOpen} openNotification='{notificationAtOpen}' lastNotification='{_lastNotification}' step1Active={activeAfterStep1} step1Version={versionAfterStep1} step1Notifications={notificationsAfterStep1} visible={popup.Visible} active={colorEditor.IsContinuousEditActive}");
		Require(opened, "ColorPickerButton popup did not open a continuous edit session.");
		Require(flag, "Native ColorPicker popup did not preview and close through PopupClosed.");
		Require(flag2, "Native ColorPicker popup lifecycle did not create exactly one undo action.");
		return (opened: opened, popup: flag, history: flag2);
	}

	private static void SendTextCharacter(char character)
	{
		Key key = (Key)char.ToUpperInvariant(character);
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Unicode = character,
			Pressed = true
		});
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Unicode = character,
			Pressed = false
		});
	}

	private static void SendKey(Key key)
	{
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = true
		});
		Input.ParseInputEvent(new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			Pressed = false
		});
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
				XWEditorInterface.Instance.FocusPanel("inspector");
				await WaitFrames(3);
				return _inspector.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private XWInspectorPropertyEditorBase FindPropertyEditor(string propertyName)
	{
		foreach (Node item in _inspector.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is XWInspectorPropertyEditorBase xWInspectorPropertyEditorBase && GodotObject.IsInstanceValid(xWInspectorPropertyEditorBase.Property) && xWInspectorPropertyEditorBase.Property.PropName.ToString() == propertyName)
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

	private void OnPropertyChanged(GodotObject obj, StringName property, StringName field, Variant value)
	{
		_notifications++;
		_lastNotification = $"{property}:{field}={value}";
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
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_NATIVE_INPUT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_NATIVE_INPUT_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
