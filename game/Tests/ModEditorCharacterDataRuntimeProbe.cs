using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorCharacterDataRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWGenericVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	private bool _metadataUndoRedo;

	public override async void _Ready()
	{
		_ = 4;
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
			Require(GodotObject.IsInstanceValid(modEditorManager), "ModEditorManager scene could not be instantiated.");
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
			Require(flag, "F3 did not initialize the real CharacterData editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool window = FindAncestorWindow(_editor) != null;
			Require(window, "CharacterData editor is not mounted under the F3 ModEditor window.");
			bool inspectorHidden = await ProbeInspectorAndMetadata();
			bool metadataUndoRedo = _metadataUndoRedo;
			(bool, bool) tuple = await ProbeStandaloneDirectEdit();
			bool standaloneDirect = tuple.Item1;
			bool continuousMerged = tuple.Item2;
			bool value = await ProbeConeArmorStages();
			GD.Print($"[MOD_EDITOR_CHARACTER_DATA_PROBE] window={window} inspectorHidden={inspectorHidden} metadataUndoRedo={metadataUndoRedo} standaloneDirect={standaloneDirect} conePreserved={value} continuousMerged={continuousMerged} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<bool> ProbeInspectorAndMetadata()
	{
		CharacterArmorData armorData = new CharacterArmorData
		{
			ResourceName = "CharacterDataMetadataBefore",
			ResourceLocalToScene = false
		};
		await OpenResource(armorData);
		VBoxContainer vBoxContainer = FindControl<VBoxContainer>("EmbeddedInspectorHost");
		PanelContainer panelContainer = _editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		bool inspectorHidden = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
		Require(inspectorHidden, "CharacterData workspace exposed or instantiated an embedded Inspector.");
		LineEdit lineEdit = FindControl<LineEdit>("ResourceNameEdit");
		Require(GodotObject.IsInstanceValid(lineEdit), "ResourceNameEdit is missing from CharacterData visual metadata.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return inspectorHidden;
		}
		_history.ClearHistory();
		string before = armorData.ResourceName;
		SimulateTextSession(lineEdit, "CharacterDataMetadataAfter");
		await WaitFrames(2);
		bool applied = armorData.ResourceName == "CharacterDataMetadataAfter" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(2);
		undone = undone && armorData.ResourceName == before;
		bool redone = _history.Redo();
		await WaitFrames(2);
		redone = redone && armorData.ResourceName == "CharacterDataMetadataAfter";
		GD.Print($"[MOD_EDITOR_CHARACTER_DATA_PROBE_DETAIL] metadataApplied={applied} metadataUndone={undone} metadataRedone={redone} metadataValue={armorData.ResourceName} historyVersion={_history.GetVersion()} hasUndo={_history.HasUndo()} hasRedo={_history.HasRedo()}");
		_metadataUndoRedo = applied && undone && redone;
		Require(_metadataUndoRedo, "CharacterData resource_name did not round-trip through global undo/redo.");
		return inspectorHidden;
	}

	private async Task<(bool standaloneDirect, bool continuousMerged)> ProbeStandaloneDirectEdit()
	{
		CharacterCustomConfig custom = new CharacterCustomConfig
		{
			ResourceName = "StandaloneCustomResource",
			customName = "probe-custom-before"
		};
		await OpenResource(custom);
		VBoxContainer vBoxContainer = FindControl<VBoxContainer>("StandaloneEditorHost");
		LineEdit customName = FindLineEditByText("probe-custom-before");
		bool directSurface = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() > 0 && GodotObject.IsInstanceValid(customName);
		Require(directSurface, "Standalone CharacterCustomConfig has no populated direct-edit surface.");
		if (!directSurface)
		{
			return (standaloneDirect: false, continuousMerged: false);
		}
		_history.ClearHistory();
		int beforeVersion = _history.GetVersion();
		customName.EmitSignal(Control.SignalName.FocusEntered);
		string[] array = new string[3] { "probe-custom-a", "probe-custom-ab", "probe-custom-final" };
		for (int i = 0; i < array.Length; i++)
		{
			string text = (customName.Text = array[i]);
			customName.EmitSignal(LineEdit.SignalName.TextChanged, text);
		}
		customName.EmitSignal(LineEdit.SignalName.TextSubmitted, "probe-custom-final");
		customName.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool flag = GodotObject.IsInstanceValid(customName) && customName == FindLineEditByText("probe-custom-final");
		bool flag2 = _history.GetVersion() - beforeVersion == 1 && _history.HasUndo();
		bool continuousMerged = custom.customName == "probe-custom-final" && flag2 && flag;
		GD.Print($"[MOD_EDITOR_CHARACTER_DATA_PROBE_DETAIL] customValue={custom.customName} sameControl={flag} oneHistoryAction={flag2} versionDelta={_history.GetVersion() - beforeVersion} hasUndo={_history.HasUndo()}");
		Require(continuousMerged, "Continuous CharacterCustomConfig input rebuilt the editor or produced more than one history action.");
		bool undo = _history.Undo();
		await WaitFrames(2);
		undo = undo && custom.customName == "probe-custom-before";
		bool redo = _history.Redo();
		await WaitFrames(2);
		redo = redo && custom.customName == "probe-custom-final";
		GD.Print($"[MOD_EDITOR_CHARACTER_DATA_PROBE_DETAIL] customUndo={undo} customRedo={redo} customFinal={custom.customName} hasUndo={_history.HasUndo()} hasRedo={_history.HasRedo()}");
		bool flag3 = directSurface && undo && redo;
		Require(flag3, "Standalone CharacterCustomConfig direct edit did not undo/redo.");
		return (standaloneDirect: flag3, continuousMerged: continuousMerged);
	}

	private async Task<bool> ProbeConeArmorStages()
	{
		TowerDefenseArmorTypeData cone = new TowerDefenseArmorTypeData
		{
			ResourceName = "ConeProbe",
			armorName = "Cone",
			stagePersontage = new Array<double> { 0.72, 0.33 },
			stageAnimeTexturePaths = new Array<string> { "res://probe/cone0.png", "res://probe/cone1.png", "res://probe/cone2.png" }
		};
		await OpenResource(cone);
		await WaitFrames(3);
		VBoxContainer vBoxContainer = FindControl<VBoxContainer>("StandaloneEditorHost");
		bool flag = GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() > 0 && cone.stagePersontage.Count == 2 && Math.Abs(cone.stagePersontage[0] - 0.72) < 0.0001 && Math.Abs(cone.stagePersontage[1] - 0.33) < 0.0001 && cone.stageAnimeTexturePaths.Count == 3 && cone.stageAnimeTexturePaths[0] == "res://probe/cone0.png" && cone.stageAnimeTexturePaths[1] == "res://probe/cone1.png" && cone.stageAnimeTexturePaths[2] == "res://probe/cone2.png";
		Require(flag, "Opening Cone armor changed its two thresholds or three stage textures.");
		return flag;
	}

	private async Task OpenResource(Resource resource)
	{
		XWEditorInterface.Instance.EditResource(resource);
		await WaitFrames(4);
		Control resourceEditor = XWEditorInterface.Instance.GetResourceEditor("character_data_editor");
		Require(resourceEditor == _editor, "CharacterData routing switched away from the registered editor.");
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetResourceEditor("character_data_editor") is XWGenericVisualResourceEditor xWGenericVisualResourceEditor && GodotObject.IsInstanceValid(xWGenericVisualResourceEditor))
			{
				_editor = xWGenericVisualResourceEditor;
				_history = instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private T FindControl<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
	}

	private LineEdit FindLineEditByText(string text)
	{
		if (!GodotObject.IsInstanceValid(_editor))
		{
			return null;
		}
		foreach (Node item in _editor.FindChildren("*", "LineEdit", recursive: true, owned: false))
		{
			if (item is LineEdit lineEdit && lineEdit.Text == text)
			{
				return lineEdit;
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

	private static void SimulateTextSession(LineEdit control, string value)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.Text = value;
		control.EmitSignal(LineEdit.SignalName.TextChanged, value);
		control.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
		control.EmitSignal(Control.SignalName.FocusExited);
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
			GD.PrintErr("[MOD_EDITOR_CHARACTER_DATA_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (_failures.Count > 0)
		{
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_CHARACTER_DATA_PROBE_FAILURE] " + failure);
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
