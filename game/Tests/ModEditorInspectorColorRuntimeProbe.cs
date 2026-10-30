using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

public partial class ModEditorInspectorColorRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWInspector _inspector;

	private XWInspectorPropertyEditorColor _colorEditor;

	private XWUndoRedoManager _history;

	[Export(PropertyHint.None, "")]
	public Color ProbeColor { get; set; } = new Color(0.22f, 0.34f, 0.48f, 0.42f);

	public override async void _Ready()
	{
		_ = 12;
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
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 editor did not finish loading its Inspector surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_inspector) != null;
			_history.ClearHistory();
			Color initial = ProbeColor;
			XWEditorInterface.Instance.InspectObject(this);
			XWEditorInterface.Instance.FocusPanel("inspector");
			bool mounted = await WaitForColorEditor(240);
			Require(mounted, "Exported Color property did not mount the specialized visual editor.");
			if (!mounted)
			{
				Finish();
				return;
			}
			Control control = _colorEditor.FindChild("ColorPreview", recursive: true, owned: false) as Control;
			GridContainer gridContainer = _colorEditor.FindChild("ColorPalette", recursive: true, owned: false) as GridContainer;
			ColorPickerButton picker = _colorEditor.FindChild("ColorPickerButton", recursive: true, owned: false) as ColorPickerButton;
			Label valueLabel = _colorEditor.FindChild("ColorValueLabel", recursive: true, owned: false) as Label;
			bool visualSurface = GodotObject.IsInstanceValid(control) && control.CustomMinimumSize.X >= 120f && control.CustomMinimumSize.Y >= 44f && GodotObject.IsInstanceValid(gridContainer) && gridContainer.GetChildCount() == 7 && GodotObject.IsInstanceValid(picker) && GodotObject.IsInstanceValid(valueLabel) && valueLabel.Text.Contains("A 42%");
			Require(visualSurface, "Color property still lacks the alpha-aware preview/palette surface.");
			(_colorEditor.FindChild("PaletteButton3", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			Color paletteExpected = new Color("65c466")
			{
				A = initial.A
			};
			bool paletteApplied = ProbeColor.IsEqualApprox(paletteExpected) && _history.HasUndo();
			Require(paletteApplied, "Visual palette did not apply through the Inspector history.");
			bool undo = _history.Undo();
			await WaitFrames(15);
			undo = undo && ProbeColor.IsEqualApprox(initial) && ((Color)_colorEditor.GetValue()).IsEqualApprox(initial) && !_history.HasUndo();
			bool redo = _history.Redo();
			await WaitFrames(15);
			redo = redo && ProbeColor.IsEqualApprox(paletteExpected) && ((Color)_colorEditor.GetValue()).IsEqualApprox(paletteExpected);
			Require(undo && redo, "Palette edit did not round-trip through global Undo/Redo.");
			_history.ClearHistory();
			Color color = new Color(0.45f, 0.22f, 0.71f, 0.51f);
			Color preciseStep2 = new Color(0.72f, 0.26f, 0.38f, 0.59f);
			Color preciseExpected = new Color(0.91f, 0.31f, 0.16f, 0.67f);
			_colorEditor.BeginContinuousEdit();
			picker.EmitSignal(ColorPickerButton.SignalName.ColorChanged, color);
			await WaitFrames(2);
			picker.EmitSignal(ColorPickerButton.SignalName.ColorChanged, preciseStep2);
			await WaitFrames(2);
			picker.EmitSignal(ColorPickerButton.SignalName.ColorChanged, preciseExpected);
			await WaitFrames(3);
			bool previewWithoutHistory = ProbeColor.IsEqualApprox(preciseExpected) && !_history.HasUndo();
			_colorEditor.CommitContinuousEdit();
			await WaitFrames(3);
			bool pickerCommittedHistory = _history.HasUndo();
			bool pickerUndoCalled = pickerCommittedHistory && _history.Undo();
			await WaitFrames(15);
			bool pickerUndoColor = ProbeColor.IsEqualApprox(paletteExpected);
			bool pickerSingleHistory = !_history.HasUndo();
			bool pickerRedoCalled = pickerUndoCalled && pickerUndoColor && pickerSingleHistory && _history.Redo();
			await WaitFrames(15);
			bool flag3 = pickerCommittedHistory && pickerUndoCalled && pickerUndoColor && pickerSingleHistory && pickerRedoCalled;
			bool flag4 = previewWithoutHistory && flag3 && ProbeColor.IsEqualApprox(preciseExpected) && ((Color)_colorEditor.GetValue()).IsEqualApprox(preciseExpected) && valueLabel.Text.Contains("A 67%");
			Require(flag4, "Native precise picker did not collapse continuous changes into one UndoRedo action.");
			GD.Print($"[MOD_EDITOR_INSPECTOR_COLOR_PROBE] window={window} mounted={mounted} visualSurface={visualSurface} paletteApplied={paletteApplied} undoRedo={undo && redo} precisePicker={flag4} previewWithoutHistory={previewWithoutHistory} pickerCommittedHistory={pickerCommittedHistory} pickerUndoColor={pickerUndoColor} pickerSingleHistory={pickerSingleHistory} pickerRedoCalled={pickerRedoCalled} label='{valueLabel.Text}' failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
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

	private async Task<bool> WaitForColorEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (_inspector.FindChild("XWInspectorPropertyEditorColor", recursive: true, owned: false) is XWInspectorPropertyEditorColor xWInspectorPropertyEditorColor && GodotObject.IsInstanceValid(xWInspectorPropertyEditorColor) && GodotObject.IsInstanceValid(xWInspectorPropertyEditorColor.Property) && xWInspectorPropertyEditorColor.Property.Object == this)
			{
				_colorEditor = xWInspectorPropertyEditorColor;
				return true;
			}
			await WaitFrames(1);
		}
		return false;
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
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_COLOR_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_INSPECTOR_COLOR_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
