using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.Tools;

public partial class ModEditorCharacterCombatDataEntryRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Dictionary<string, string> _createdPaths = new Dictionary<string, string>(StringComparer.Ordinal);

	private XWUndoRedoManager _history;

	private XWCharacterCombatVisualResourceEditor _combatEditor;

	private XWCharacterDataVisualResourceEditor _dataEditor;

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
			bool f3 = await WaitForEditors(900);
			Require(f3, "F3 did not initialize CharacterCombat and CharacterData editors.");
			if (!f3)
			{
				Finish();
				return;
			}
			string text = ProjectSettings.GlobalizePath("user://CharacterCombatDataEntryProbe");
			if (Directory.Exists(text))
			{
				Directory.Delete(text, recursive: true);
			}
			XWModProjectLayout.EnsureProjectLayout(text);
			bool actions = HasAllCreateActions(text);
			Require(actions, "One or more CharacterCombat/CharacterData directories lack their strong create action.");
			var (created, routes) = CreateAndValidateResources(text);
			Require(created, "One or more strong templates failed to load as their concrete Resource type/defaults.");
			Require(routes, "One or more created resources did not route to CharacterCombat/CharacterData.");
			Node inspectorSentinel = new Node
			{
				Name = "CharacterCombatDataInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			bool inspectorSeeded = await SeedInspectorSentinel(inspectorSentinel, 120);
			Require(inspectorSeeded, "The global Inspector was not ready to install its sentinel.");
			(bool, bool, bool) tuple2 = await ProbeCombatDirectEdit();
			bool combatDirect = tuple2.Item1;
			bool combatUndoRedo = tuple2.Item2;
			bool combatSave = tuple2.Item3;
			bool arrayUndoRedo = await ProbeCombatArrayHistory();
			(bool, bool, bool) obj = await ProbeCharacterDataDirectEdit();
			bool item = obj.Item1;
			bool item2 = obj.Item2;
			bool item3 = obj.Item3;
			XWInspector xWInspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			bool flag = inspectorSeeded && GodotObject.IsInstanceValid(xWInspector) && xWInspector.CurrentObject == inspectorSentinel;
			Require(flag, "Dedicated resource surfaces replaced the global Inspector object.");
			GD.Print($"[MOD_EDITOR_CHARACTER_COMBAT_DATA_ENTRY_PROBE] f3={f3} actions={actions} created={created} routes={routes} combatDirect={combatDirect} combatUndoRedo={combatUndoRedo} combatSave={combatSave} arrayUndoRedo={arrayUndoRedo} dataDirect={item} dataUndoRedo={item2} dataSave={item3} inspectorUntouched={flag} failures={_failures.Count}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private bool HasAllCreateActions(string root)
	{
		(string, string[])[] array = new(string, string[])[6]
		{
			("Resources/CharacterCombat/Attacks", new string[1] { "new-character-attack" }),
			("Resources/CharacterCombat/Buffs", new string[4] { "new-character-buff-frozen", "new-character-buff-burn", "new-character-buff-poison", "new-character-buff-hypnoses" }),
			("Resources/CharacterCombat/Events", new string[5] { "new-character-event-hurt", "new-character-event-config-hurt", "new-character-event-add-buff", "new-character-event-random", "new-character-event-lucky-draw" }),
			("Resources/CharacterData/Armor", new string[3] { "new-character-armor-data", "new-character-armor-slot", "new-character-armor-type" }),
			("Resources/CharacterData/Custom", new string[2] { "new-character-custom-data", "new-character-custom-config" }),
			("Resources/CharacterData/DamagePoints", new string[2] { "new-character-damage-point-data", "new-character-damage-point-config" })
		};
		for (int i = 0; i < array.Length; i++)
		{
			(string, string[]) tuple = array[i];
			string item = tuple.Item1;
			string[] item2 = tuple.Item2;
			IReadOnlyList<XWResourceCreateRoute.CreateAction> actionsForDirectory = XWResourceCreateRoute.GetActionsForDirectory(Path.Combine(root, item.Replace('/', Path.DirectorySeparatorChar)), root);
			string[] array2 = item2;
			foreach (string text in array2)
			{
				bool flag = false;
				foreach (XWResourceCreateRoute.CreateAction item3 in actionsForDirectory)
				{
					if (item3.Id == text)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					return false;
				}
			}
		}
		return true;
	}

	private (bool Created, bool Routes) CreateAndValidateResources(string root)
	{
		(string, string, string, Type)[] obj = new(string, string, string, Type)[17]
		{
			("new-character-attack", "Resources/CharacterCombat/Attacks", "ProbeAttack", typeof(AttackConfig)),
			("new-character-buff-frozen", "Resources/CharacterCombat/Buffs", "ProbeFrozen", typeof(TowerDefenseCharacterBuffFrozen)),
			("new-character-buff-burn", "Resources/CharacterCombat/Buffs", "ProbeBurn", typeof(TowerDefenseCharacterBuffBurn)),
			("new-character-buff-poison", "Resources/CharacterCombat/Buffs", "ProbePoison", typeof(TowerDefenseCharacterBuffPoisoning)),
			("new-character-buff-hypnoses", "Resources/CharacterCombat/Buffs", "ProbeHypnoses", typeof(TowerDefenseCharacterBuffHypnoses)),
			("new-character-event-hurt", "Resources/CharacterCombat/Events", "ProbeHurt", typeof(TowerDefenseCharacterEventHurt)),
			("new-character-event-config-hurt", "Resources/CharacterCombat/Events", "ProbeConfigHurt", typeof(TowerDefenseCharacterEventHurtWithConfig)),
			("new-character-event-add-buff", "Resources/CharacterCombat/Events", "ProbeAddBuff", typeof(TowerDefenseCharacterEventAddBuff)),
			("new-character-event-random", "Resources/CharacterCombat/Events", "ProbeRandom", typeof(TowerDefenseCharacterEventConditionRandom)),
			("new-character-event-lucky-draw", "Resources/CharacterCombat/Events", "ProbeLuckyDraw", typeof(TowerDefenseCharacterEventLuckyDraw)),
			("new-character-armor-data", "Resources/CharacterData/Armor", "ProbeArmorData", typeof(CharacterArmorData)),
			("new-character-armor-slot", "Resources/CharacterData/Armor", "ProbeArmorSlot", typeof(ArmorSlotConfig)),
			("new-character-armor-type", "Resources/CharacterData/Armor", "ProbeArmorType", typeof(TowerDefenseArmorTypeData)),
			("new-character-custom-data", "Resources/CharacterData/Custom", "ProbeCustomData", typeof(CharacterCustomData)),
			("new-character-custom-config", "Resources/CharacterData/Custom", "ProbeCustomConfig", typeof(CharacterCustomConfig)),
			("new-character-damage-point-data", "Resources/CharacterData/DamagePoints", "ProbeDamageData", typeof(CharacterDamagePointData)),
			("new-character-damage-point-config", "Resources/CharacterData/DamagePoints", "ProbeDamagePoint", typeof(CharacterDamagePointConfig))
		};
		bool flag = true;
		bool flag2 = true;
		(string, string, string, Type)[] array = obj;
		for (int i = 0; i < array.Length; i++)
		{
			(string, string, string, Type) tuple = array[i];
			string item = tuple.Item1;
			string item2 = tuple.Item2;
			string item3 = tuple.Item3;
			Type item4 = tuple.Item4;
			string directoryPath = Path.Combine(root, item2.Replace('/', Path.DirectorySeparatorChar));
			XWTemplateLibrary.TemplateCreateResult templateCreateResult = XWResourceCreateRoute.CreateFromAction(item, directoryPath, item3);
			string text = LocalPath(templateCreateResult);
			Resource resource = Load<Resource>(text);
			_createdPaths[item] = text;
			bool flag3 = templateCreateResult.Success && GodotObject.IsInstanceValid(resource) && resource.GetType() == item4;
			flag = flag && flag3;
			if (!flag3)
			{
				Require(condition: false, $"{item} creation/type failed: success={templateCreateResult.Success}, error={templateCreateResult.Error}, path={text}, type={resource?.GetType().Name}");
			}
			else
			{
				string text2 = (item2.Contains("CharacterCombat", StringComparison.Ordinal) ? "CharacterCombat" : "CharacterData");
				string dockKey = ((text2 == "CharacterCombat") ? "character_combat_editor" : "character_data_editor");
				flag2 &= RoutesTo(resource, text, text2, dockKey);
			}
		}
		AttackConfig attackConfig = Load<AttackConfig>(_createdPaths["new-character-attack"]);
		TowerDefenseCharacterBuffFrozen towerDefenseCharacterBuffFrozen = Load<TowerDefenseCharacterBuffFrozen>(_createdPaths["new-character-buff-frozen"]);
		CharacterDamagePointConfig characterDamagePointConfig = Load<CharacterDamagePointConfig>(_createdPaths["new-character-damage-point-config"]);
		TowerDefenseArmorTypeData towerDefenseArmorTypeData = Load<TowerDefenseArmorTypeData>(_createdPaths["new-character-armor-type"]);
		flag &= GodotObject.IsInstanceValid(attackConfig) && Math.Abs(attackConfig.num - 20.0) < 0.0001 && attackConfig.collisionFlags == 9;
		flag &= GodotObject.IsInstanceValid(towerDefenseCharacterBuffFrozen) && Math.Abs(towerDefenseCharacterBuffFrozen.time - 8.0) < 0.0001 && towerDefenseCharacterBuffFrozen.key == "Frozen";
		flag &= GodotObject.IsInstanceValid(characterDamagePointConfig) && characterDamagePointConfig.damagePointName == "ProbeDamagePoint" && Math.Abs(characterDamagePointConfig.damagePersontage - 0.5) < 0.0001;
		flag &= GodotObject.IsInstanceValid(towerDefenseArmorTypeData) && towerDefenseArmorTypeData.height == TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL && towerDefenseArmorTypeData.armorMethodFlags == 200;
		return (Created: flag, Routes: flag2);
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeCombatDirectEdit()
	{
		string attackPath = _createdPaths["new-character-attack"];
		AttackConfig attack = Load<AttackConfig>(attackPath);
		await OpenCombat(attack);
		SpinBox instance = Find<SpinBox>(_combatEditor, "CombatProperty_num");
		LineEdit instance2 = Find<LineEdit>(_combatEditor, "CombatProperty_resource_name");
		bool attackDirect = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(instance2) && InspectorIsHidden(_combatEditor);
		TowerDefenseCharacterBuffFrozen resource = Load<TowerDefenseCharacterBuffFrozen>(_createdPaths["new-character-buff-frozen"]);
		await OpenCombat(resource);
		bool buffDirect = GodotObject.IsInstanceValid(Find<SpinBox>(_combatEditor, "CombatProperty_time")) && GodotObject.IsInstanceValid(Find<CheckButton>(_combatEditor, "CombatProperty_refresh")) && InspectorIsHidden(_combatEditor);
		TowerDefenseCharacterEventCreateEffect resource2 = new TowerDefenseCharacterEventCreateEffect
		{
			ResourceName = "NullEffectProbe",
			effectScene = null
		};
		await OpenCombat(resource2);
		bool nullResourceDirect = GodotObject.IsInstanceValid(Find<Control>(_combatEditor, "CombatProperty_effectScene")) && InspectorIsHidden(_combatEditor);
		TowerDefenseCharacterEventLuckyDrawItem resource3 = new TowerDefenseCharacterEventLuckyDrawItem
		{
			ResourceName = "LuckyItemProbe",
			_event = null,
			weight = 2.0
		};
		await OpenCombat(resource3);
		bool flag = GodotObject.IsInstanceValid(Find<Control>(_combatEditor, "CombatProperty__event")) && GodotObject.IsInstanceValid(Find<SpinBox>(_combatEditor, "CombatProperty_weight")) && InspectorIsHidden(_combatEditor);
		bool direct = attackDirect && buffDirect && nullResourceDirect && flag;
		Require(direct, "Attack/Buff/Event did not expose complete inspector-free direct controls, including a null Resource field.");
		await OpenCombat(attack);
		instance = Find<SpinBox>(_combatEditor, "CombatProperty_num");
		_history.ClearHistory();
		instance.SetValueNoSignal(37.0);
		instance.EmitSignal(Godot.Range.SignalName.ValueChanged, 37.0);
		await WaitFrames(3);
		bool applied = Math.Abs(attack.num - 37.0) < 0.0001 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(4);
		undone = undone && Math.Abs(attack.num - 20.0) < 0.0001;
		bool redone = _history.Redo();
		await WaitFrames(4);
		redone = redone && Math.Abs(attack.num - 37.0) < 0.0001;
		bool undoRedo = applied && undone && redone;
		Require(undoRedo, "CharacterCombat scalar edit did not round-trip through shared Undo/Redo.");
		Button button = FindButtonByText(_combatEditor, "保存");
		Require(GodotObject.IsInstanceValid(button), "CharacterCombat toolbar has no Save button.");
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(6);
		AttackConfig attackConfig = Load<AttackConfig>(attackPath);
		bool flag2 = GodotObject.IsInstanceValid(attackConfig) && Math.Abs(attackConfig.num - 37.0) < 0.0001;
		Require(flag2, "CharacterCombat edit did not survive Save/Reload.");
		return (Direct: direct, UndoRedo: undoRedo, Save: flag2);
	}

	private async Task<bool> ProbeCombatArrayHistory()
	{
		TowerDefenseCharacterEventAddBuff addBuff = Load<TowerDefenseCharacterEventAddBuff>(_createdPaths["new-character-event-add-buff"]);
		await OpenCombat(addBuff);
		Control control = Find<Control>(_combatEditor, "CombatProperty_buffList");
		Button button = Find<Button>(control, "AddButton");
		bool flag = GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(button) && InspectorIsHidden(_combatEditor);
		Require(flag, "AddBuff.eventList has no direct visual array card.");
		if (!flag)
		{
			return false;
		}
		_history.ClearHistory();
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		bool applied = addBuff.buffList.Count == 1 && addBuff.buffList[0] is TowerDefenseCharacterBuffFrozen && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(4);
		undone = undone && addBuff.buffList.Count == 0;
		bool redone = _history.Redo();
		await WaitFrames(4);
		redone = redone && addBuff.buffList.Count == 1 && addBuff.buffList[0] is TowerDefenseCharacterBuffFrozen;
		bool flag2 = applied && undone && redone;
		Require(flag2, "CharacterCombat typed subresource array did not round-trip through shared Undo/Redo.");
		return flag2;
	}

	private async Task<(bool Direct, bool UndoRedo, bool Save)> ProbeCharacterDataDirectEdit()
	{
		string path = _createdPaths["new-character-damage-point-config"];
		CharacterDamagePointConfig config = Load<CharacterDamagePointConfig>(path);
		await OpenData(config);
		LineEdit instance = Find<LineEdit>(_dataEditor, "DamagePointNameLineEdit");
		SpinBox spinBox = Find<SpinBox>(_dataEditor, "DamagePercentageSpinBox");
		bool direct = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(spinBox) && GodotObject.IsInstanceValid(Find<Control>(_dataEditor, "DamageEffectPicker")) && GodotObject.IsInstanceValid(Find<Control>(_dataEditor, "DamageReplaceTexturePicker")) && InspectorIsHidden(_dataEditor);
		Require(direct, "CharacterDamagePointConfig did not expose its complete inspector-free direct surface.");
		if (!direct)
		{
			return (Direct: false, UndoRedo: false, Save: false);
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.SetValueNoSignal(0.72);
		spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 0.72);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool applied = Math.Abs(config.damagePersontage - 0.72) < 0.0001 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(4);
		undone = undone && Math.Abs(config.damagePersontage - 0.5) < 0.0001;
		bool redone = _history.Redo();
		await WaitFrames(4);
		redone = redone && Math.Abs(config.damagePersontage - 0.72) < 0.0001;
		bool undoRedo = applied && undone && redone;
		Require(undoRedo, "CharacterData direct edit did not round-trip through shared Undo/Redo.");
		Button button = FindButtonByText(_dataEditor, "保存");
		Require(GodotObject.IsInstanceValid(button), "CharacterData toolbar has no Save button.");
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(6);
		CharacterDamagePointConfig characterDamagePointConfig = Load<CharacterDamagePointConfig>(path);
		bool flag = GodotObject.IsInstanceValid(characterDamagePointConfig) && Math.Abs(characterDamagePointConfig.damagePersontage - 0.72) < 0.0001;
		Require(flag, "CharacterData direct edit did not survive Save/Reload.");
		return (Direct: direct, UndoRedo: undoRedo, Save: flag);
	}

	private async Task OpenCombat(Resource resource)
	{
		XWEditorInterface.Instance.EditResource(resource);
		XWEditorInterface.Instance.FocusPanel("character_combat_editor");
		await WaitFrames(6);
		Require(XWEditorInterface.Instance.GetResourceEditor("character_combat_editor") == _combatEditor, "CharacterCombat routing switched away from the registered editor.");
	}

	private async Task OpenData(Resource resource)
	{
		XWEditorInterface.Instance.EditResource(resource);
		XWEditorInterface.Instance.FocusPanel("character_data_editor");
		await WaitFrames(6);
		Require(XWEditorInterface.Instance.GetResourceEditor("character_data_editor") == _dataEditor, "CharacterData routing switched away from the registered editor.");
	}

	private async Task<bool> WaitForEditors(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetResourceEditor("character_combat_editor") is XWCharacterCombatVisualResourceEditor combatEditor && instance.GetResourceEditor("character_data_editor") is XWCharacterDataVisualResourceEditor dataEditor)
			{
				_combatEditor = combatEditor;
				_dataEditor = dataEditor;
				_history = instance.GetUndoRedoManager();
				(instance.GetEditorPanel()?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control)?.Hide();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
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

	private static bool RoutesTo(Resource resource, string path, string category, string dockKey)
	{
		if (GodotObject.IsInstanceValid(resource) && XWResourceEditorRegistry.TryGetEditor(resource, path, out var descriptor) && descriptor?.Category == category)
		{
			return descriptor.DockKey == dockKey;
		}
		return false;
	}

	private static T Load<T>(string path) where T : Resource
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			return ResourceLoader.Load<T>(path, "", ResourceLoader.CacheMode.Ignore);
		}
		return null;
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
			GD.PrintErr("[MOD_EDITOR_CHARACTER_COMBAT_DATA_ENTRY_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_CHARACTER_COMBAT_DATA_ENTRY_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
