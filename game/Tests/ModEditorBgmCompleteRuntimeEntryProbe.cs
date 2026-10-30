using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorBgmCompleteRuntimeEntryProbe : Node
{
	private static readonly HashSet<string> InternalOnlyResourceTypes = new HashSet<string>(StringComparer.Ordinal)
	{
		"AdobeAnimateExternalVisualTextureRegistry", "AdobeAnimateGlobalAtlasManifest", "CommandArg", "CommandConfig", "GameConfigSaveConfigCSharp", "GameSaveConfigCSharp", "Guard", "RandomTransformationComponentPacketBankConfig", "SavedState", "StateMachineLayout",
		"StateMachineSnapshot", "StateMachineStateDefinition", "StateMachineTransitionDefinition", "TowerDefenseArmorInstance", "TowerDefenseBattleComponentBase", "TowerDefenseBattleDependenceData", "TowerDefenseCellInstance", "TowerDefenseCharacterInstance", "TowerDefenseCharacterSaveConfigCSharp", "TowerDefenseConveyorEventBase",
		"TowerDefenseConveyorEventEnum", "TowerDefenseDropItemSaveConfigCSharp", "TowerDefenseLevelBaseConfig", "TowerDefenseLevelSaveConfigCSharp", "TowerDefenseLevelSurvivalRunner", "TowerDefenseNodeSaveConfigCSharp", "CardActionBehaviorFactory", "TowerDefenseProjectileSaveConfigCSharp", "WeightPickItemBase", "XWBPFunctionSerializeData",
		"XWBPGraphSerializeData", "XWBPNodeConnectionSerializeData", "XWBPNodePortSerializeData", "XWBPNodeSerializeData", "XWBPScript", "XWBPSignalSerializeData", "XWBPVariableSerializeData"
	};

	private readonly List<string> _failures = new List<string>();

	private XWUndoRedoManager _history;

	private XWAudioVisualResourceEditor _editor;

	private string _resourcePath = "";

	private TowerDefenseBackgroundMusicConfig _bgm;

	public override async void _Ready()
	{
		bool action = false;
		bool undoRedo = false;
		bool save = false;
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
			Require(f3, "F3 did not initialize the real BGM editor.");
			if (!f3)
			{
				Finish();
				return;
			}
			string text = ProjectSettings.GlobalizePath("user://BgmCompleteRuntimeEntryProbe");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			XWModProjectLayout.EnsureProjectLayout(text);
			string directoryPath = Path.Combine(text, "Resources", "BGMConfigs");
			foreach (XWResourceCreateRoute.CreateAction item in XWResourceCreateRoute.GetActionsForDirectory(directoryPath, text))
			{
				action |= item.Id == "new-bgm-config";
			}
			Require(action, "Resources/BGMConfigs has no strong BGM create action.");
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction("new-bgm-config", directoryPath, "ProbeBgm");
			_resourcePath = (templateCreateResult.Success ? ProjectSettings.LocalizePath(templateCreateResult.CreatedPath).Replace('\\', '/') : "");
			_bgm = LoadBgm(_resourcePath);
			bool created = templateCreateResult.Success && GodotObject.IsInstanceValid(_bgm) && _bgm.GetType() == typeof(TowerDefenseBackgroundMusicConfig) && _bgm.translate == "ProbeBgm" && _bgm.entry == "" && _bgm.flag1 == "" && _bgm.drums == "" && _bgm.win == "" && _bgm.drumsZombieThreshold == 10 && Mathf.IsEqualApprox(_bgm.drumsFadeSpeed, 1f) && Mathf.IsEqualApprox(_bgm.drumsCheckInterval, 0.25f);
			Require(created, $"Strong BGM template failed: success={templateCreateResult.Success}; error={templateCreateResult.Error}; path={_resourcePath}");
			XWVisualEditorDescriptor descriptor;
			bool route = GodotObject.IsInstanceValid(_bgm) && XWResourceEditorRegistry.TryGetEditor(_bgm, _resourcePath, out descriptor) && descriptor.Category == "BGM" && descriptor.DockKey == "bgm_editor";
			Require(route, "Created BGM did not route to bgm_editor.");
			Node sentinel = new Node
			{
				Name = "BgmCompleteInspectorSentinel"
			};
			AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
			XWEditorInterface.Instance.InspectObject(sentinel);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool inspectorBaseline = GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == sentinel;
			Require(inspectorBaseline, "BGM probe could not establish the global Inspector sentinel.");
			XWEditorInterface.Instance.EditResource(_bgm);
			XWEditorInterface.Instance.FocusPanel("bgm_editor");
			await WaitFrames(8);
			LineEdit instance = Find<LineEdit>(_editor, "ResourceNameEdit");
			LineEdit instance2 = Find<LineEdit>(_editor, "TranslateEdit");
			CheckButton instance3 = Find<CheckButton>(_editor, "LocalToSceneCheck");
			SpinBox spinBox = Find<SpinBox>(_editor, "DrumsZombieThreshold");
			SpinBox instance4 = Find<SpinBox>(_editor, "DrumsFadeSpeed");
			SpinBox instance5 = Find<SpinBox>(_editor, "DrumsCheckInterval");
			bool direct = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance2) && GodotObject.IsInstanceValid(instance3) && GodotObject.IsInstanceValid(spinBox) && GodotObject.IsInstanceValid(instance4) && GodotObject.IsInstanceValid(instance5) && GodotObject.IsInstanceValid(GetAudioEdit("EntryRow")) && GodotObject.IsInstanceValid(GetAudioEdit("Flag1Row")) && GodotObject.IsInstanceValid(GetAudioEdit("DrumsRow")) && GodotObject.IsInstanceValid(GetAudioEdit("WinRow")) && InspectorIsHidden(_editor);
			Require(direct, "BGM workbench did not expose all properties without the raw Inspector.");
			if (direct)
			{
				_history.ClearHistory();
				EditNumber(spinBox, 18.0);
				await WaitFrames(5);
				bool thresholdApplied = _bgm.drumsZombieThreshold == 18 && _history.HasUndo();
				bool thresholdUndone = _history.Undo();
				await WaitFrames(5);
				thresholdUndone &= _bgm.drumsZombieThreshold == 10;
				bool thresholdRedone = _history.Redo();
				await WaitFrames(5);
				thresholdRedone &= _bgm.drumsZombieThreshold == 18;
				EditText(GetAudioEdit("EntryRow"), "ProbeEntry");
				await WaitFrames(5);
				bool entryApplied = _bgm.entry == "ProbeEntry" && _history.HasUndo();
				bool entryUndone = _history.Undo();
				await WaitFrames(5);
				entryUndone &= _bgm.entry == "";
				bool entryRedone = _history.Redo();
				await WaitFrames(5);
				entryRedone &= _bgm.entry == "ProbeEntry";
				undoRedo = thresholdApplied && thresholdUndone && thresholdRedone && entryApplied && entryUndone && entryRedone;
				Require(undoRedo, "BGM number/audio-key edits did not round-trip through shared Undo/Redo.");
				Button button = FindButtonByText(_editor, "保存");
				Require(GodotObject.IsInstanceValid(button), "BGM toolbar has no Save button.");
				button?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(8);
				TowerDefenseBackgroundMusicConfig towerDefenseBackgroundMusicConfig = LoadBgm(_resourcePath);
				save = GodotObject.IsInstanceValid(towerDefenseBackgroundMusicConfig) && towerDefenseBackgroundMusicConfig.drumsZombieThreshold == 18 && towerDefenseBackgroundMusicConfig.entry == "ProbeEntry";
				Require(save, "BGM edits did not survive cache-ignore Save/Reload.");
			}
			string category;
			string key;
			bool flag = ModLoader.InferRuntimeEntry("Resources/BGMConfigs/ProbeBgm.tres", out category, out key) && category == "BGM" && key == "ProbeBgm";
			Require(flag, "ModLoader did not infer Resources/BGMConfigs as category BGM.");
			string ownerMod = "ModEditor.BgmCompleteProbe." + Guid.NewGuid().ToString("N");
			string key2 = "__BgmCompleteProbe_" + Guid.NewGuid().ToString("N");
			XWModRuntimeRegistry.UnregisterOwner(ownerMod);
			TowerDefenseBackgroundMusicConfig from = new TowerDefenseBackgroundMusicConfig
			{
				entry = "ProbeRuntimeEntry"
			};
			Resource value;
			bool flag2 = GodotObject.IsInstanceValid(ResourceManager.Instance) && XWModRuntimeRegistry.Register(ownerMod, "BGM", key2, Variant.From(in from)) && ResourceManager.Instance.BGMS.TryGetValue(key2, out value) && value == from && XWModRuntimeRegistry.UnregisterOwner(ownerMod) == 1 && !ResourceManager.Instance.BGMS.ContainsKey(key2);
			Require(flag2, "BGM runtime registry did not register and cleanly unregister the resource.");
			var (value2, value3, value4, value5, flag3) = AuditResourceCoverage();
			Require(flag3, "Concrete GlobalClass Resource coverage changed or contains a new authoring gap.");
			bool flag4 = inspectorBaseline && GodotObject.IsInstanceValid(inspector) && inspector.CurrentObject == sentinel;
			Require(flag4, "BGM editor replaced the global Inspector object.");
			GD.Print($"[MOD_EDITOR_BGM_COMPLETE_RUNTIME_ENTRY_PROBE] f3={f3} action={action} created={created} route={route} direct={direct} undoRedo={undoRedo} save={save} infer={flag} registry={flag2} types={value2} covered={value3} internalOnly={value4} authoringGaps={value5} coverage={flag3} inspectorUntouched={flag4} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private static (int ResourceTypes, int Covered, int InternalOnly, int AuthoringGaps, bool Complete) AuditResourceCoverage()
	{
		Type globalClassAttribute = typeof(GlobalClassAttribute);
		Type resourceBase = typeof(Resource);
		List<Type> list = (from type2 in typeof(TowerDefenseBackgroundMusicConfig).Assembly.GetTypes()
			where !type2.IsAbstract && resourceBase.IsAssignableFrom(type2) && type2.GetCustomAttributes(globalClassAttribute, inherit: false).Length != 0
			select type2).ToList();
		IReadOnlyList<XWVisualEditorDescriptor> allEditors = XWResourceEditorRegistry.GetAllEditors();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		int num = 0;
		foreach (Type item in list)
		{
			bool flag = false;
			Type type = item;
			while (type != null && !flag)
			{
				foreach (XWVisualEditorDescriptor item2 in allEditors)
				{
					if (item2.Category != "General" && item2.ResourceClassNames.Contains(type.Name))
					{
						flag = true;
						break;
					}
				}
				type = type.BaseType;
			}
			if (flag)
			{
				num++;
			}
			else
			{
				hashSet.Add(item.Name);
			}
		}
		int num2 = hashSet.Count((string name) => !InternalOnlyResourceTypes.Contains(name));
		bool flag2 = num2 == 0 && hashSet.SetEquals(InternalOnlyResourceTypes);
		if (!flag2)
		{
			GD.Print("[MOD_EDITOR_RESOURCE_COVERAGE_DIAGNOSTIC] uncovered=" + string.Join(",", hashSet.OrderBy((string name) => name)));
			GD.Print("[MOD_EDITOR_RESOURCE_COVERAGE_DIAGNOSTIC] expectedInternal=" + string.Join(",", InternalOnlyResourceTypes.OrderBy((string name) => name)));
		}
		return (ResourceTypes: list.Count, Covered: num, InternalOnly: hashSet.Count, AuthoringGaps: num2, Complete: flag2);
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetResourceEditor("bgm_editor") is XWAudioVisualResourceEditor editor && instance.GetInspector() is XWInspector)
			{
				_editor = editor;
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		GD.Print($"[MOD_EDITOR_BGM_COMPLETE_RUNTIME_ENTRY_PROBE_DIAGNOSTIC] interface={XWEditorInterface.Instance != null} editor={XWEditorInterface.Instance?.GetResourceEditor("bgm_editor")?.GetType().Name ?? "null"}");
		return false;
	}

	private LineEdit GetAudioEdit(string rowName)
	{
		return (_editor?.FindChild(rowName, recursive: true, owned: false))?.GetNodeOrNull<LineEdit>("Value");
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

	private static void EditNumber(SpinBox spin, double value)
	{
		if (GodotObject.IsInstanceValid(spin))
		{
			spin.EmitSignal(Control.SignalName.FocusEntered);
			spin.Value = value;
			spin.EmitSignal(Godot.Range.SignalName.ValueChanged, value);
			spin.EmitSignal(Control.SignalName.FocusExited);
		}
	}

	private static TowerDefenseBackgroundMusicConfig LoadBgm(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return ResourceLoader.Load<TowerDefenseBackgroundMusicConfig>(path, "", ResourceLoader.CacheMode.Ignore);
		}
		return null;
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

	private static T Find<T>(Node root, string name) where T : Node
	{
		return root?.FindChild(name, recursive: true, owned: false) as T;
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
			GD.PrintErr("[MOD_EDITOR_BGM_COMPLETE_RUNTIME_ENTRY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_BGM_COMPLETE_RUNTIME_ENTRY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
