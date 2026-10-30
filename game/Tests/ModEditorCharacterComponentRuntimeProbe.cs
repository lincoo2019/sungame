using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class ModEditorCharacterComponentRuntimeProbe : Node
{
	private const string DraftPath = "user://mod_editor_character_component_responsive_probe.tres";

	private const string SetDraftPath = "user://mod_editor_character_component_set_probe.tres";

	private const string CannonDraftPath = "user://mod_editor_character_component_cannon_probe.tres";

	private const string AttackDraftPath = "user://mod_editor_character_component_attack_probe.tres";

	private const string AabbDraftPath = "user://mod_editor_character_component_aabb_probe.tres";

	private const string SquashDraftPath = "user://mod_editor_character_component_squash_probe.tres";

	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWCharacterComponentVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	private string _modProjectPath;

	private string _parentFixturePath;

	private string _shapeFixturePath;

	private string _parentFixtureUri;

	private string _shapeFixtureUri;

	private string _stateMachineBaseFixturePath;

	private string _stateMachineBaseFixtureUri;

	private int _structuralFullRefreshDelta;

	private int _structuralLeafRefreshDelta;

	private bool _structuralFullRefresh;

	private int _leafIncrementalRefreshDelta;

	private int _leafFullRefreshDelta;

	private int _leafRuntimeBuildDelta;

	private int _leafStateBindDelta;

	private bool _leafNodeStable;

	private bool _leafRuntimeStable;

	private bool _removeSelectionValid;

	public override async void _Ready()
	{
		_ = 10;
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
			bool flag = await WaitForEditor(900);
			Require(flag, "F3 did not initialize the character-component editor within 900 frames.");
			if (!flag)
			{
				Finish();
				return;
			}
			bool flag2 = await EnterEditorSurface(900);
			Require(flag2, "F3 editor did not finish loading its main editing surface.");
			if (!flag2)
			{
				Finish();
				return;
			}
			bool condition = PrepareModResourceFixtures();
			Require(condition, "Could not prepare the real Mod resource-library fixtures.");
			Window window = FindAncestorWindow(_editor);
			bool window2 = GodotObject.IsInstanceValid(window);
			Require(window2, "Character-component editor is not mounted under the F3 ModEditor window.");
			if (window2)
			{
				window.MinSize = Vector2I.Zero;
				window.Size = new Vector2I(820, 720);
				await WaitFrames(8);
			}
			CharacterComponentProbeDefinition item = CreateDefinition("parent-probe", 3);
			CharacterComponentProbeDefinition local = CreateDefinition("local-probe", 7);
			CharacterComponentSet parentSetFixture = new CharacterComponentSet
			{
				ResourceName = "ProbeParentSet",
				Components = new Array<CharacterComponentDefinition> { item }
			};
			CharacterComponentSet childSet = new CharacterComponentSet
			{
				ResourceName = "ProbeChildSet",
				ParentSet = parentSetFixture,
				Components = new Array<CharacterComponentDefinition> { local }
			};
			(bool, bool, bool, bool, bool, bool) tuple = await ProbeAggregateSet(childSet, local);
			bool route = tuple.Item1;
			bool inspectorHidden = tuple.Item2;
			bool library = tuple.Item3;
			bool inheritance = tuple.Item4;
			bool inheritedReadOnly = tuple.Item5;
			bool arrayUndoRedo = tuple.Item6;
			(bool, bool) tuple2 = await ProbeParentSetEditing(childSet, parentSetFixture);
			bool parentSet = tuple2.Item1;
			bool parentCycle = tuple2.Item2;
			bool stringEnum = await ProbeStringEnumWithRealCannonDefinition();
			bool resourceArrayFirst = await ProbeEmptyTypedResourceArrayWithRealAttackDefinition();
			bool transform2D = await ProbeTransform2DWithRealAabbDefinition();
			(bool, bool, bool, bool, bool) tuple3 = await ProbeSquashOutsideBattlefieldCompletionToggle();
			bool squashOutsideToggle = tuple3.Item1;
			bool squashOutsideUndoRedo = tuple3.Item2;
			bool squashOutsideSavedReloaded = tuple3.Item3;
			bool squashOutsideFeedback = tuple3.Item4;
			bool squashOutsideRuntime = tuple3.Item5;
			Error error = ResourceSaver.Save(local, "user://mod_editor_character_component_responsive_probe.tres", ResourceSaver.SaverFlags.None);
			Require(error == Error.Ok, $"Could not save the real component draft: {error}.");
			CharacterComponentProbeDefinition characterComponentProbeDefinition = ResourceLoader.Load<CharacterComponentProbeDefinition>("user://mod_editor_character_component_responsive_probe.tres", "", ResourceLoader.CacheMode.Replace);
			Require(GodotObject.IsInstanceValid(characterComponentProbeDefinition), "Saved component draft did not reload as a real Resource.");
			(bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool, bool) obj = await ProbeDynamicDefinition(characterComponentProbeDefinition);
			bool item2 = obj.Item1;
			bool item3 = obj.Item2;
			bool item4 = obj.Item3;
			bool item5 = obj.Item4;
			bool item6 = obj.Item5;
			bool item7 = obj.Item6;
			bool item8 = obj.Item7;
			bool item9 = obj.Rest.Item1;
			bool item10 = obj.Rest.Item2;
			bool item11 = obj.Rest.Item3;
			bool item12 = obj.Rest.Item4;
			bool item13 = obj.Rest.Item5;
			bool item14 = obj.Rest.Item6;
			bool item15 = obj.Rest.Item7;
			bool item16 = obj.Rest.Rest.Item1;
			bool item17 = obj.Rest.Rest.Item2;
			bool item18 = obj.Rest.Rest.Item3;
			bool item19 = obj.Rest.Rest.Item4;
			bool item20 = obj.Rest.Rest.Item5;
			bool item21 = obj.Rest.Rest.Item6;
			bool item22 = obj.Rest.Rest.Item7;
			bool item23 = obj.Rest.Rest.Rest.Item1;
			GD.Print($"[MOD_EDITOR_CHARACTER_COMPONENT_PROBE] window={window2} route={route} inspectorHidden={inspectorHidden} library={library} inheritance={inheritance} inheritedReadOnly={inheritedReadOnly} dynamicExports={item2} arrayUndoRedo={arrayUndoRedo} typedArrayUndoRedo={item3} typedDictionaryUndoRedo={item4} stringEnum={stringEnum} parentSet={parentSet} parentCycle={parentCycle} resourceArrayFirst={resourceArrayFirst} packedInt32={item18} transform2D={transform2D} squashOutsideToggle={squashOutsideToggle} squashOutsideUndoRedo={squashOutsideUndoRedo} squashOutsideSavedReloaded={squashOutsideSavedReloaded} squashOutsideFeedback={squashOutsideFeedback} squashOutsideRuntime={squashOutsideRuntime} stateMachine={item5} embeddedLayout={item6} embeddedPickerIncremental={item7} realRuntimeBound={item8} realRuntimeTrace={item9} borrowedRuntimeSafe={item10} realRuntimeReset={item11} hiddenRuntimeDetached={item12} runtimePreview={item13} eventSimulation={item14} responsive={item15} visualSelection={item16} propertyEdit={item17} lazySurfaces={item22} leafIncremental={item23} leafNodeStable={_leafNodeStable} leafRuntimeStable={_leafRuntimeStable} structuralFullRefresh={_structuralFullRefresh} removeSelectionValid={_removeSelectionValid} hiddenStopped={item19} saveReload={item20} inspectorUntouched={item21} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
			GD.Print($"[MOD_EDITOR_CHARACTER_COMPONENT_REFRESH] leafIncrementalDelta={_leafIncrementalRefreshDelta} leafFullDelta={_leafFullRefreshDelta} leafNodeStable={_leafNodeStable} leafRuntimeStable={_leafRuntimeStable} leafRuntimeBuildDelta={_leafRuntimeBuildDelta} leafStateBindDelta={_leafStateBindDelta} structuralIncrementalDelta={_structuralLeafRefreshDelta} structuralFullDelta={_structuralFullRefreshDelta}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private bool PrepareModResourceFixtures()
	{
		_modProjectPath = ProjectSettings.GlobalizePath("user://mod_editor_character_component_fixture_mod");
		string text = Path.Combine(_modProjectPath, "Resources", "CharacterComponents");
		string text2 = Path.Combine(_modProjectPath, "Resources");
		string text3 = Path.Combine(text2, "StateMachines");
		Directory.CreateDirectory(text);
		Directory.CreateDirectory(text2);
		Directory.CreateDirectory(text3);
		_parentFixturePath = Path.Combine(text, "ProbeParentSet.tres");
		_shapeFixturePath = Path.Combine(text2, "ProbeAabbShape.tres");
		_parentFixtureUri = "user://mod_editor_character_component_fixture_mod/Resources/CharacterComponents/ProbeParentSet.tres";
		_shapeFixtureUri = "user://mod_editor_character_component_fixture_mod/Resources/ProbeAabbShape.tres";
		_stateMachineBaseFixturePath = Path.Combine(text3, "ProbeBaseStateMachine.tres");
		_stateMachineBaseFixtureUri = "user://mod_editor_character_component_fixture_mod/Resources/StateMachines/ProbeBaseStateMachine.tres";
		Error num = ResourceSaver.Save(new AabbShape2DResource
		{
			ResourceName = "ProbeAabbShape"
		}, _shapeFixtureUri, ResourceSaver.SaverFlags.None);
		Error error = ResourceSaver.Save(new StateMachineDefinition
		{
			ResourceName = "组件状态机基定义",
			DefinitionId = "component-picker-base",
			RootStateId = "probe.root",
			States = new Array<StateMachineStateDefinition>
			{
				new StateMachineStateDefinition
				{
					StableId = "probe.root",
					DisplayName = "基状态入口",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "base.idle"
				},
				new StateMachineStateDefinition
				{
					StableId = "base.idle",
					DisplayName = "基状态待机",
					ParentId = "probe.root",
					Kind = StateMachineStateKind.Atomic
				}
			}
		}, _stateMachineBaseFixtureUri, ResourceSaver.SaverFlags.None);
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		singleton?.SetProjectFolderPath(_modProjectPath);
		if (num == Error.Ok && error == Error.Ok && singleton != null)
		{
			return SamePhysicalPath(singleton.ProjectFolderPath, _modProjectPath);
		}
		return false;
	}

	private static bool SamePhysicalPath(string left, string right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		string a = Path.GetFullPath(left.Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string b = Path.GetFullPath(right.Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
	}

	private static CharacterComponentProbeDefinition CreateDefinition(string instanceId, int wireIndex)
	{
		return new CharacterComponentProbeDefinition
		{
			ResourceName = instanceId,
			ComponentTypeId = "probe.dynamic",
			DefinitionId = "definition-" + instanceId,
			InstanceId = instanceId,
			WireIndex = wireIndex,
			StateMachineDefinition = CreateProbeStateMachine(instanceId)
		};
	}

	private static StateMachineDefinition CreateProbeStateMachine(string instanceId)
	{
		return new StateMachineDefinition
		{
			DefinitionId = "machine-" + instanceId,
			RootStateId = "probe.root",
			States = new Array<StateMachineStateDefinition>
			{
				new StateMachineStateDefinition
				{
					StableId = "probe.root",
					DisplayName = "Probe Root",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "probe.idle"
				},
				new StateMachineStateDefinition
				{
					StableId = "probe.idle",
					DisplayName = "Probe Idle",
					ParentId = "probe.root",
					Kind = StateMachineStateKind.Atomic
				},
				new StateMachineStateDefinition
				{
					StableId = "probe.triggered",
					DisplayName = "Probe Triggered",
					ParentId = "probe.root",
					Kind = StateMachineStateKind.Atomic
				}
			},
			Transitions = new Array<StateMachineTransitionDefinition>
			{
				new StateMachineTransitionDefinition
				{
					StableId = "probe.to_triggered",
					SourceStateId = "probe.root",
					TargetStateId = "probe.triggered",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "damage"
				}
			}
		};
	}

	private static bool SnapshotHasActive(StateMachineSnapshot snapshot, string stableId)
	{
		if (snapshot?.ActiveStateIds == null || string.IsNullOrWhiteSpace(stableId))
		{
			return false;
		}
		foreach (string activeStateId in snapshot.ActiveStateIds)
		{
			if (string.Equals(activeStateId, stableId, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private async Task<(bool route, bool inspectorHidden, bool library, bool inheritance, bool inheritedReadOnly, bool arrayUndoRedo)> ProbeAggregateSet(CharacterComponentSet componentSet, CharacterComponentDefinition local)
	{
		XWVisualEditorDescriptor descriptor;
		bool descriptorRoute = XWResourceEditorRegistry.TryGetEditor(componentSet, "res://Resources/CharacterComponents/ProbeChildSet.tres", out descriptor) && descriptor?.Category == "CharacterComponent" && descriptor.DockKey == "character_component_editor";
		await OpenResource(componentSet);
		bool route = descriptorRoute && XWEditorInterface.Instance.GetResourceEditor("character_component_editor") == _editor;
		Require(route, "CharacterComponentSet did not route to character_component_editor.");
		PanelContainer panelContainer = FindControl<PanelContainer>("InspectorPanel");
		VBoxContainer vBoxContainer = FindControl<VBoxContainer>("EmbeddedInspectorHost");
		bool inspectorHidden = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null && _editor.FindChild("InlineTextSurface", recursive: true, owned: false) == null && (!(_editor.FindChild("DirectPropertySurface", recursive: true, owned: false) is Control control) || !control.Visible);
		Require(inspectorHidden, "CharacterComponentSet still shows a raw Inspector, duplicate inline-text rail, or duplicate direct-property surface.");
		HFlowContainer libraryGrid = FindControl<HFlowContainer>("ComponentTypeGrid");
		Control librarySearch = FindControl<Control>("ComponentLibrarySearch");
		int libraryRenderBefore = _editor.ComponentLibraryRenderCount;
		bool libraryDeferred = GodotObject.IsInstanceValid(libraryGrid) && libraryGrid.GetChildCount() == 0;
		_editor.SetWorkbenchPage(2);
		await WaitFrames(3);
		int libraryRenderAfter = _editor.ComponentLibraryRenderCount;
		_editor.SetWorkbenchPage(2);
		await WaitFrames(1);
		bool library = libraryDeferred && GodotObject.IsInstanceValid(libraryGrid) && libraryGrid.GetChildCount() > 0 && libraryGrid.GetChild(0) is XWGameVisualChoiceCard && GodotObject.IsInstanceValid(librarySearch) && libraryRenderAfter == libraryRenderBefore + 1 && _editor.ComponentLibraryRenderCount == libraryRenderAfter;
		Require(library, "The component icon library was not deferred until its page or rendered exactly once.");
		Tree tree = FindControl<Tree>("InheritanceTree");
		ItemList effective = FindControl<ItemList>("EffectiveComponentList");
		ItemList localList = FindControl<ItemList>("LocalComponentList");
		string text = CollectVisibleText(_editor);
		bool inheritance = GodotObject.IsInstanceValid(tree) && tree.GetRoot() != null && GodotObject.IsInstanceValid(effective) && effective.ItemCount >= 2 && GodotObject.IsInstanceValid(localList) && localList.ItemCount >= 1 && text.Contains("parent-probe", StringComparison.OrdinalIgnoreCase) && text.Contains("local-probe", StringComparison.OrdinalIgnoreCase);
		Require(inheritance, "Parent/local/effective component composition was not rendered.");
		ModEditorCharacterComponentRuntimeProbe modEditorCharacterComponentRuntimeProbe = this;
		CharacterComponentSet parentSet = componentSet.ParentSet;
		bool inheritedReadOnly = await modEditorCharacterComponentRuntimeProbe.ProbeInheritedDefinitionReadOnly(effective, (parentSet != null && parentSet.Components.Count > 0) ? (componentSet.ParentSet.Components[0] as CharacterComponentProbeDefinition) : null);
		bool flag = false;
		Button removeButton = FindControl<Button>("RemoveComponentButton");
		if (GodotObject.IsInstanceValid(localList) && localList.ItemCount > 0 && GodotObject.IsInstanceValid(removeButton))
		{
			_history.ClearHistory();
			int before = componentSet.Components.Count;
			localList.Select(0);
			localList.EmitSignal(ItemList.SignalName.ItemSelected, 0L);
			await WaitFrames(2);
			int structuralFullBefore = _editor.FullSurfaceRefreshCount;
			int structuralLeafBefore = _editor.LeafPropertyRefreshCount;
			removeButton.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			bool removed = componentSet.Components.Count == before - 1 && !componentSet.Components.Contains(local) && _history.HasUndo();
			int deleteDoFullDelta = _editor.FullSurfaceRefreshCount - structuralFullBefore;
			int deleteDoLeafDelta = _editor.LeafPropertyRefreshCount - structuralLeafBefore;
			CharacterComponentSet parentSet2 = componentSet.ParentSet;
			CharacterComponentDefinition remaining = ((parentSet2 != null && parentSet2.Components?.Count > 0) ? componentSet.ParentSet.Components[0] : null);
			int[] selectedItems = effective.GetSelectedItems();
			int[] selectedItems2 = localList.GetSelectedItems();
			_removeSelectionValid = GodotObject.IsInstanceValid(remaining) && _editor.SelectedDefinition == remaining && selectedItems.Length == 1 && effective.GetItemMetadata(selectedItems[0]).As<CharacterComponentDefinition>() == remaining && selectedItems2.Length == 0;
			FindControl<Button>("OverrideComponentButton")?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			bool overrideApplied = componentSet.Components.Count == before && componentSet.Components.Any((CharacterComponentDefinition candidate) => GodotObject.IsInstanceValid(candidate) && candidate != local && candidate.InstanceId == remaining?.InstanceId);
			bool overrideUndone = overrideApplied && _history.Undo();
			await WaitFrames(3);
			overrideUndone &= componentSet.Components.Count == before - 1 && _editor.SelectedDefinition == remaining;
			_removeSelectionValid &= overrideApplied && overrideUndone;
			int deleteUndoRedoFullBefore = _editor.FullSurfaceRefreshCount;
			int deleteUndoRedoLeafBefore = _editor.LeafPropertyRefreshCount;
			bool undone = _history.Undo();
			await WaitFrames(3);
			undone = undone && componentSet.Components.Count == before && componentSet.Components.Contains(local);
			bool redone = _history.Redo();
			await WaitFrames(3);
			redone = redone && componentSet.Components.Count == before - 1 && !componentSet.Components.Contains(local);
			flag = removed && undone && redone;
			_structuralFullRefreshDelta = deleteDoFullDelta + _editor.FullSurfaceRefreshCount - deleteUndoRedoFullBefore;
			_structuralLeafRefreshDelta = deleteDoLeafDelta + _editor.LeafPropertyRefreshCount - deleteUndoRedoLeafBefore;
			_structuralFullRefresh = _structuralFullRefreshDelta == 3 && _structuralLeafRefreshDelta == 0;
		}
		Require(flag, "Local Components removal did not round-trip through global undo/redo.");
		Require(_structuralFullRefresh, $"Structural component commit/undo/redo did not use three full refreshes and zero leaf refreshes: full={_structuralFullRefreshDelta} leaf={_structuralLeafRefreshDelta}.");
		Require(_removeSelectionValid, "Removing the selected local component cleared the valid inherited replacement selection after the structural refresh.");
		return (route: route, inspectorHidden: inspectorHidden, library: library, inheritance: inheritance, inheritedReadOnly: inheritedReadOnly, arrayUndoRedo: flag);
	}

	private async Task<(bool parentSet, bool parentCycle)> ProbeParentSetEditing(CharacterComponentSet componentSet, CharacterComponentSet originalParent)
	{
		Error error = ResourceSaver.Save(originalParent, _parentFixtureUri, ResourceSaver.SaverFlags.None);
		Require(error == Error.Ok, $"Could not save ParentSet picker fixture: {error}.");
		Button instance = FindControl<Button>("ParentSetChooseButton");
		Button button = FindControl<Button>("ParentSetClearButton");
		bool visual = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(button) && !button.Disabled;
		Require(visual, "ParentSet choose/clear controls disappeared after the set selected a component.");
		_history.ClearHistory();
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool cleared = componentSet.ParentSet == null && _history.HasUndo();
		bool clearUndone = _history.Undo();
		await WaitFrames(3);
		clearUndone &= componentSet.ParentSet == originalParent;
		bool clearRedone = _history.Redo();
		await WaitFrames(3);
		clearRedone &= componentSet.ParentSet == null;
		instance = FindControl<Button>("ParentSetChooseButton");
		_history.ClearHistory();
		instance?.EmitSignal(BaseButton.SignalName.Pressed);
		bool pickerChosen = await ChooseOpenResourcePicker(_parentFixturePath, 600);
		await WaitFrames(4);
		bool selected = pickerChosen && SameResourcePath(componentSet.ParentSet, _parentFixturePath) && _history.HasUndo();
		bool selectUndone = _history.Undo();
		await WaitFrames(3);
		selectUndone &= componentSet.ParentSet == null;
		bool selectRedone = _history.Redo();
		await WaitFrames(3);
		selectRedone &= SameResourcePath(componentSet.ParentSet, _parentFixturePath);
		Error num = ResourceSaver.Save(componentSet, "user://mod_editor_character_component_set_probe.tres", ResourceSaver.SaverFlags.ChangePath);
		if (num == Error.Ok)
		{
			componentSet.TakeOverPath("user://mod_editor_character_component_set_probe.tres");
		}
		CharacterComponentSet characterComponentSet = ResourceLoader.Load<CharacterComponentSet>("user://mod_editor_character_component_set_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag = num == Error.Ok && GodotObject.IsInstanceValid(characterComponentSet) && SameResourcePath(characterComponentSet.ParentSet, _parentFixturePath);
		bool parentSet = visual && cleared && clearUndone && clearRedone && selected && selectUndone && selectRedone && flag;
		Require(parentSet, $"ParentSet visual clear/select did not round-trip: visual={visual} cleared={cleared} clearUndo={clearUndone} clearRedo={clearRedone} picker={pickerChosen} selected={selected} selectUndo={selectUndone} selectRedo={selectRedone} saved={flag} currentPath={componentSet.ParentSet?.ResourcePath ?? "<null>"} expectedPath={_parentFixturePath}.");
		CharacterComponentSet candidate = new CharacterComponentSet
		{
			ResourceName = "CycleCandidate",
			ParentSet = componentSet
		};
		_history.ClearHistory();
		bool accepted = _editor.TryAssignParentSet(candidate);
		await WaitFrames(2);
		CharacterComponentSet duplicateOwner = ResourceLoader.Load<CharacterComponentSet>("user://mod_editor_character_component_set_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool duplicateAccepted = _editor.TryAssignParentSet(duplicateOwner);
		await WaitFrames(2);
		bool flag2 = !accepted && !duplicateAccepted && SameResourcePath(componentSet.ParentSet, _parentFixturePath) && !_history.HasUndo();
		Require(flag2, $"ParentSet reference/path-identity cycle candidate was accepted or polluted UndoRedo history: referenceAccepted={accepted} duplicateAccepted={duplicateAccepted} ownerPath={componentSet.ResourcePath} duplicatePath={duplicateOwner?.ResourcePath ?? "<null>"} parentPath={componentSet.ParentSet?.ResourcePath ?? "<null>"} history={_history.HasUndo()}.");
		return (parentSet: parentSet, parentCycle: flag2);
	}

	private async Task<bool> ProbeStringEnumWithRealCannonDefinition()
	{
		CannonComponentDefinition definition = new CannonComponentDefinition
		{
			mode = "Marker"
		};
		await OpenResource(definition);
		_editor.SetWorkbenchPage(1);
		await WaitFrames(2);
		OptionButton optionButton = FindPropertyEditor<OptionButton>("mode");
		bool controlFound = GodotObject.IsInstanceValid(optionButton);
		Button button = (optionButton?.GetParent()?.FindChild("EnumVisual_mode", recursive: true, owned: false) as HFlowContainer)?.FindChild("VisualOption1", recursive: true, owned: false) as Button;
		bool rawHidden = controlFound && !optionButton.Visible;
		bool visualFound = GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(button.Icon);
		_history.ClearHistory();
		if (visualFound)
		{
			button.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
		}
		bool applied = definition.mode == "Line" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone &= definition.mode == "Marker";
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone &= definition.mode == "Line";
		Error error = ResourceSaver.Save(definition, "user://mod_editor_character_component_cannon_probe.tres", ResourceSaver.SaverFlags.None);
		CannonComponentDefinition cannonComponentDefinition = ResourceLoader.Load<CannonComponentDefinition>("user://mod_editor_character_component_cannon_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag = controlFound && rawHidden && visualFound && applied && undone && redone && error == Error.Ok && cannonComponentDefinition?.mode == "Line";
		Require(flag, $"Real Cannon visual string Enum did not preserve its string value through UndoRedo/save: source={controlFound} rawHidden={rawHidden} visual={visualFound} applied={applied} undo={undone} redo={redone} reload={cannonComponentDefinition?.mode ?? "<null>"}.");
		return flag;
	}

	private async Task<bool> ProbeEmptyTypedResourceArrayWithRealAttackDefinition()
	{
		AttackComponentDefinition definition = new AttackComponentDefinition();
		await OpenResource(definition);
		_editor.SetWorkbenchPage(1);
		await WaitFrames(2);
		Button button = FindPropertyButton("checkShapeResources");
		bool addFound = GodotObject.IsInstanceValid(button);
		_history.ClearHistory();
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		bool pickerChosen = await ChooseOpenResourcePicker(_shapeFixturePath, 600);
		await WaitFrames(4);
		bool applied = pickerChosen && definition.checkShapeResources.Count == 1 && definition.checkShapeResources[0] != null && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone &= definition.checkShapeResources.Count == 0;
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone &= definition.checkShapeResources.Count == 1 && definition.checkShapeResources[0] != null;
		Error error = ResourceSaver.Save(definition, "user://mod_editor_character_component_attack_probe.tres", ResourceSaver.SaverFlags.None);
		AttackComponentDefinition attackComponentDefinition = ResourceLoader.Load<AttackComponentDefinition>("user://mod_editor_character_component_attack_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag = addFound && applied && undone && redone && error == Error.Ok && attackComponentDefinition != null && attackComponentDefinition.checkShapeResources.Count == 1 && attackComponentDefinition.checkShapeResources[0] != null;
		Require(flag, $"Real Attack empty typed Resource Array could not add its first picker resource: add={addFound} picker={pickerChosen} applied={applied} undo={undone} redo={redone} reload={attackComponentDefinition?.checkShapeResources.Count ?? (-1)}.");
		return flag;
	}

	private async Task<bool> ProbeTransform2DWithRealAabbDefinition()
	{
		CharacterAabbAreaComponentDefinition definition = new CharacterAabbAreaComponentDefinition
		{
			localTransform = Transform2D.Identity
		};
		await OpenResource(definition);
		_editor.SetWorkbenchPage(1);
		await WaitFrames(2);
		SpinBox spinBox = FindControl<SpinBox>("Transform2D_localTransform_OX");
		bool controlFound = GodotObject.IsInstanceValid(spinBox);
		_history.ClearHistory();
		if (controlFound)
		{
			spinBox.EmitSignal(Control.SignalName.FocusEntered);
			spinBox.SetValueNoSignal(12.5);
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 12.5);
			spinBox.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(3);
		}
		bool applied = Mathf.IsEqualApprox(definition.localTransform.Origin.X, 12.5f) && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone &= definition.localTransform.IsEqualApprox(Transform2D.Identity);
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone &= Mathf.IsEqualApprox(definition.localTransform.Origin.X, 12.5f);
		Error error = ResourceSaver.Save(definition, "user://mod_editor_character_component_aabb_probe.tres", ResourceSaver.SaverFlags.None);
		CharacterAabbAreaComponentDefinition characterAabbAreaComponentDefinition = ResourceLoader.Load<CharacterAabbAreaComponentDefinition>("user://mod_editor_character_component_aabb_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag = controlFound && applied && undone && redone && error == Error.Ok && characterAabbAreaComponentDefinition != null && Mathf.IsEqualApprox(characterAabbAreaComponentDefinition.localTransform.Origin.X, 12.5f);
		Require(flag, $"Real CharacterAabbArea Transform2D numeric editor failed UndoRedo/save: control={controlFound} applied={applied} undo={undone} redo={redone} reload={characterAabbAreaComponentDefinition?.localTransform.Origin.X ?? (0f / 0f)}.");
		return flag;
	}

	private async Task<(bool toggle, bool undoRedo, bool savedReloaded, bool feedback, bool runtime)> ProbeSquashOutsideBattlefieldCompletionToggle()
	{
		SquashComponentDefinition definition = ResourceLoader.Load<SquashComponentDefinition>("res://Script/Component/TowerDefense/Character/SquashComponent/Definitions/BowlingWallnutSquashDefinition.tres", "", ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as SquashComponentDefinition;
		bool flag = GodotObject.IsInstanceValid(definition) && definition.completeStartedSequenceOutsideComponentBattlefield;
		if (flag)
		{
			flag = ResourceSaver.Save(definition, "user://mod_editor_character_component_squash_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok;
		}
		definition = (flag ? ResourceLoader.Load<SquashComponentDefinition>("user://mod_editor_character_component_squash_probe.tres", "", ResourceLoader.CacheMode.Replace) : null);
		Require(GodotObject.IsInstanceValid(definition), "Could not clone the real Bowling Wallnut Squash component into the Mod draft.");
		if (!GodotObject.IsInstanceValid(definition))
		{
			return (toggle: false, undoRedo: false, savedReloaded: false, feedback: false, runtime: false);
		}
		await OpenResource(definition);
		_editor.SetWorkbenchPage(1);
		await WaitFrames(3);
		PanelContainer instance = FindControl<PanelContainer>("SquashBattlefieldContinuationCard");
		OptionButton optionButton = FindControl<OptionButton>("SquashContinuationModeSource");
		HFlowContainer hFlowContainer = FindControl<HFlowContainer>("SquashContinuationVisualModes");
		Button button = hFlowContainer?.FindChild("VisualOption0", recursive: true, owned: false) as Button;
		Button button2 = hFlowContainer?.FindChild("VisualOption1", recursive: true, owned: false) as Button;
		Label label = FindControl<Label>("SquashContinuationPreviewStatus");
		bool visualReady = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(optionButton) && !optionButton.Visible && optionButton.Selected == 1 && GodotObject.IsInstanceValid(button) && GodotObject.IsInstanceValid(button.Icon) && GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(button2.Icon) && (label?.Text.Contains("连续模式", StringComparison.Ordinal) ?? false);
		_history.ClearHistory();
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		Label label2 = FindControl<Label>("SquashContinuationPreviewStatus");
		bool toggle = visualReady && !definition.completeStartedSequenceOutsideComponentBattlefield && _history.HasUndo();
		bool pausedFeedback = label2?.Text.Contains("暂停模式", StringComparison.Ordinal) ?? false;
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone &= definition.completeStartedSequenceOutsideComponentBattlefield && (FindControl<Label>("SquashContinuationPreviewStatus")?.Text.Contains("连续模式", StringComparison.Ordinal) ?? false);
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone &= !definition.completeStartedSequenceOutsideComponentBattlefield && (FindControl<Label>("SquashContinuationPreviewStatus")?.Text.Contains("暂停模式", StringComparison.Ordinal) ?? false);
		bool undoRedo = undone && redone;
		(FindControl<HFlowContainer>("SquashContinuationVisualModes")?.FindChild("VisualOption1", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool enabledAgain = definition.completeStartedSequenceOutsideComponentBattlefield;
		bool feedback = pausedFeedback && (FindControl<Label>("SquashContinuationPreviewStatus")?.Text.Contains("连续模式", StringComparison.Ordinal) ?? false);
		_editor.SetWorkbenchPage(0);
		await WaitFrames(4);
		bool runtime = _editor.CurrentRuntimePreview is SquashComponent { Lifecycle: ComponentRuntimeLifecycle.Active } squashComponent && squashComponent.completeStartedSequenceOutsideComponentBattlefield;
		bool saveResult = _editor.SaveActiveResource();
		await WaitFrames(5);
		SquashComponentDefinition squashComponentDefinition = ResourceLoader.Load<SquashComponentDefinition>("user://mod_editor_character_component_squash_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag2 = saveResult && enabledAgain && (squashComponentDefinition?.completeStartedSequenceOutsideComponentBattlefield ?? false);
		Require(toggle, "The Squash outside-battlefield visual card did not commit the real definition toggle.");
		Require(undoRedo, "The Squash outside-battlefield visual choice did not round-trip through Undo/Redo.");
		Require(feedback, "The Squash outside-battlefield visual card did not show pause/continue gameplay feedback.");
		Require(runtime, "The real Squash runtime preview did not receive the enabled outside-battlefield completion policy.");
		Require(flag2, "The Squash outside-battlefield completion policy did not survive editor save/reload.");
		return (toggle: toggle, undoRedo: undoRedo, savedReloaded: flag2, feedback: feedback, runtime: runtime);
	}

	private async Task<bool> ProbeInheritedDefinitionReadOnly(ItemList effective, CharacterComponentProbeDefinition inherited)
	{
		if (!GodotObject.IsInstanceValid(effective) || inherited == null)
		{
			Require(condition: false, "Inherited probe definition is missing from the effective component list.");
			return false;
		}
		int num = -1;
		for (int i = 0; i < effective.ItemCount; i++)
		{
			if (effective.GetItemMetadata(i).As<CharacterComponentDefinition>() == inherited)
			{
				num = i;
				break;
			}
		}
		Require(num >= 0, "Inherited component row could not be selected for the read-only probe.");
		if (num < 0)
		{
			return false;
		}
		effective.Select(num);
		effective.EmitSignal(ItemList.SignalName.ItemSelected, (long)num);
		await WaitFrames(3);
		SpinBox spinBox = FindPropertyEditor<SpinBox>("ProbePower");
		bool visuallyReadOnly = !GodotObject.IsInstanceValid(spinBox) || !spinBox.Editable;
		_history.ClearHistory();
		int before = inherited.ProbePower;
		if (GodotObject.IsInstanceValid(spinBox))
		{
			spinBox.SetValueNoSignal(91.0);
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 91.0);
			await WaitFrames(2);
		}
		bool mutationBlocked = inherited.ProbePower == before && !_history.HasUndo();
		_editor.SetWorkbenchPage(3);
		await WaitFrames(3);
		StateMachineGraphEditorSurface inheritedSurface = _editor.FindChild("StateMachineGraphEditorSurface", recursive: true, owned: false) as StateMachineGraphEditorSurface;
		int inheritedStateCount = inherited.StateMachineDefinition?.States?.Count ?? (-1);
		(inheritedSurface?.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		int num2;
		if (_editor.EmbeddedStateMachineIsVisible && _editor.EmbeddedStateMachineIsReadOnly && inheritedSurface?.Definition == inherited.StateMachineDefinition && inheritedSurface?.FindStateNode("probe.idle") != null)
		{
			StateMachineDefinition stateMachineDefinition = inherited.StateMachineDefinition;
			if (stateMachineDefinition != null && stateMachineDefinition.States?.Count == inheritedStateCount)
			{
				num2 = ((!_history.HasUndo()) ? 1 : 0);
				goto IL_046c;
			}
		}
		num2 = 0;
		goto IL_046c;
		IL_046c:
		bool flag = (byte)num2 != 0;
		bool flag2 = visuallyReadOnly && mutationBlocked && flag;
		Require(flag2, "An inherited component or its state-machine graph was hidden/editable before creating a local override.");
		return flag2;
	}

	private async Task<(bool dynamicExports, bool typedArrayUndoRedo, bool typedDictionaryUndoRedo, bool stateMachine, bool embeddedLayout, bool embeddedPickerIncremental, bool realRuntimeBound, bool realRuntimeTrace, bool borrowedRuntimeSafe, bool realRuntimeReset, bool hiddenRuntimeDetached, bool runtimePreview, bool eventSimulation, bool responsive, bool visualSelection, bool propertyEdit, bool packedInt32, bool hiddenStopped, bool saveReload, bool inspectorUntouched, bool lazySurfaces, bool leafIncremental)> ProbeDynamicDefinition(CharacterComponentProbeDefinition definition)
	{
		await OpenResource(definition);
		Control layout = FindControl<Control>("CharacterComponentEditorLayout");
		TabContainer pages = _editor.WorkbenchPages;
		bool pagesReachable = GodotObject.IsInstanceValid(pages) && pages.GetTabCount() == 4;
		if (pagesReachable)
		{
			for (int page = 0; page < pages.GetTabCount(); page++)
			{
				_editor.SetWorkbenchPage(page);
				await WaitFrames(2);
				pagesReachable &= pages.GetTabControl(page).IsVisibleInTree();
			}
		}
		bool responsive = GodotObject.IsInstanceValid(layout) && layout.CustomMinimumSize.X <= 420f && layout.GetCombinedMinimumSize().X <= 500f && layout.Size.X <= 820f && pagesReachable;
		Require(responsive, $"Character-component pages did not remain reachable in the 820px editor: layout={layout?.Size.X ?? (-1f)}; minimum={layout?.GetCombinedMinimumSize().X ?? (-1f)}; tabs={pages?.GetTabCount() ?? (-1)}.");
		_editor.SetWorkbenchPage(0);
		await WaitFrames(2);
		bool visualSelection = _editor.HasVisualCharacterSelector && _editor.VisualEventCardCount == 7 && FindControl<Button>("PreviewCharacterVisualButton")?.Icon != null && FindControl<Button>("EventTypeVisualButton")?.Icon != null && FindControl<OptionButton>("PreviewCharacterPicker") == null && FindControl<OptionButton>("EventTypePicker") == null;
		_editor.SelectVisualEvent("damage");
		visualSelection &= _editor.SelectedVisualEventName == "damage";
		Require(visualSelection, "Character/event selectors did not use the visual card contract.");
		PanelContainer panelContainer = FindControl<PanelContainer>("InspectorPanel");
		Require(GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible, "Unknown Mod CharacterComponentDefinition still shows InspectorPanel.");
		string text = CollectVisibleText(_editor).Replace(" ", string.Empty).Replace("_", string.Empty);
		bool dynamicExports = text.Contains("ProbePower", StringComparison.OrdinalIgnoreCase) && text.Contains("ProbeMode", StringComparison.OrdinalIgnoreCase) && text.Contains("ProbeTags", StringComparison.OrdinalIgnoreCase) && text.Contains("ProbeOffset", StringComparison.OrdinalIgnoreCase) && text.Contains("ProbeTint", StringComparison.OrdinalIgnoreCase);
		Require(dynamicExports, "Unknown Mod definition exports were not rendered dynamically.");
		_editor.SetWorkbenchPage(1);
		await WaitFrames(2);
		Require(await ProbeVectorAndColorVisualEditors(definition), "Vector2/Color exports did not use direct visual controls with Undo/Redo.");
		int offPagePreviewBuilds = _editor.RuntimePreviewBuildCount;
		CharacterComponentRuntime leafRuntime = _editor.CurrentRuntimePreview;
		IStateMachineController leafRuntimeController = leafRuntime?.StateMachine;
		Control propertyHostBefore = FindControl<Control>("ComponentPropertyHost");
		int refreshBeforeProperty = _editor.FullSurfaceRefreshCount;
		int leafRefreshBeforeProperty = _editor.LeafPropertyRefreshCount;
		int runtimeBuildBeforeProperty = _editor.RuntimePreviewBuildCount;
		int stateBindBeforeProperty = _editor.StateMachineSurfaceBindCount;
		SpinBox powerEditor = FindPropertyEditor<SpinBox>("ProbePower");
		_history.ClearHistory();
		int originalPower = definition.ProbePower;
		if (GodotObject.IsInstanceValid(powerEditor))
		{
			powerEditor.EmitSignal(Control.SignalName.FocusEntered);
			powerEditor.SetValueNoSignal(42.0);
			powerEditor.EmitSignal(Godot.Range.SignalName.ValueChanged, 42.0);
			powerEditor.EmitSignal(Control.SignalName.FocusExited);
			await WaitFrames(3);
		}
		bool propertyApplied = GodotObject.IsInstanceValid(powerEditor) && definition.ProbePower == 42 && _history.HasUndo() && powerEditor == FindPropertyEditor<SpinBox>("ProbePower") && Math.Abs(powerEditor.Value - 42.0) < 0.001;
		bool propertyUndone = _history.Undo();
		await WaitFrames(3);
		propertyUndone &= GodotObject.IsInstanceValid(powerEditor) && definition.ProbePower == originalPower && powerEditor == FindPropertyEditor<SpinBox>("ProbePower") && Math.Abs(powerEditor.Value - (double)originalPower) < 0.001;
		bool propertyRedone = _history.Redo();
		await WaitFrames(3);
		propertyRedone &= GodotObject.IsInstanceValid(powerEditor) && definition.ProbePower == 42 && powerEditor == FindPropertyEditor<SpinBox>("ProbePower") && Math.Abs(powerEditor.Value - 42.0) < 0.001;
		_leafIncrementalRefreshDelta = _editor.LeafPropertyRefreshCount - leafRefreshBeforeProperty;
		_leafFullRefreshDelta = _editor.FullSurfaceRefreshCount - refreshBeforeProperty;
		_leafRuntimeBuildDelta = _editor.RuntimePreviewBuildCount - runtimeBuildBeforeProperty;
		_leafStateBindDelta = _editor.StateMachineSurfaceBindCount - stateBindBeforeProperty;
		_leafNodeStable = propertyHostBefore == FindControl<Control>("ComponentPropertyHost") && powerEditor == FindPropertyEditor<SpinBox>("ProbePower");
		ModEditorCharacterComponentRuntimeProbe modEditorCharacterComponentRuntimeProbe = this;
		int leafRuntimeStable;
		if (leafRuntime == _editor.CurrentRuntimePreview && leafRuntimeController == _editor.CurrentRuntimePreview?.StateMachine)
		{
			leafRuntimeStable = ((leafRuntime != null && leafRuntime.Lifecycle == ComponentRuntimeLifecycle.Active) ? 1 : 0);
		}
		else
		{
			leafRuntimeStable = 0;
		}
		modEditorCharacterComponentRuntimeProbe._leafRuntimeStable = (byte)leafRuntimeStable != 0;
		bool leafIncremental = _leafIncrementalRefreshDelta == 3 && _leafFullRefreshDelta == 0 && _leafRuntimeBuildDelta == 0 && _leafStateBindDelta == 0 && _leafNodeStable && _leafRuntimeStable;
		Require(leafIncremental, $"Leaf commit/undo/redo rebuilt the surface or runtime: leaf={_leafIncrementalRefreshDelta} full={_leafFullRefreshDelta} runtimeBuild={_leafRuntimeBuildDelta} stateBind={_leafStateBindDelta} nodeStable={_leafNodeStable} runtimeStable={_leafRuntimeStable}.");
		bool propertyEdit = propertyApplied && propertyUndone && propertyRedone;
		Require(propertyEdit, $"Unknown component numeric property did not round-trip through global Undo/Redo: control={GodotObject.IsInstanceValid(powerEditor)} applied={propertyApplied} undone={propertyUndone} redone={propertyRedone} value={definition.ProbePower}.");
		bool typedArrayUndoRedo = await ProbeTypedStringArrayEdit(definition);
		bool typedDictionaryUndoRedo = await ProbeTypedDictionaryAdd(definition);
		bool packedInt32 = await ProbePackedInt32ArrayEdit(definition);
		bool offPagePreviewDeferred = _editor.RuntimePreviewBuildCount == offPagePreviewBuilds;
		int stateBindBefore = _editor.StateMachineSurfaceBindCount;
		int statePreviewBuildBefore = _editor.RuntimePreviewBuildCount;
		_editor.SetWorkbenchPage(3);
		await WaitFrames(3);
		bool previewBuiltForStateMachine = _editor.RuntimePreviewBuildCount == statePreviewBuildBefore + 1;
		StateMachineGraphEditorSurface stateSurface = _editor.FindChild("StateMachineGraphEditorSurface", recursive: true, owned: false) as StateMachineGraphEditorSurface;
		bool stateMachine = GodotObject.IsInstanceValid(stateSurface) && stateSurface.Definition == definition.StateMachineDefinition && _editor.StateMachineSurfaceBindCount == stateBindBefore + 1;
		Require(stateMachine, "The reusable state-machine graph surface was not bound to the component definition.");
		bool embeddedPickerIncremental = await ProbeEmbeddedStateMachineResourcePicker(definition, stateSurface);
		StateMachineSimulationPanel realSimulator = stateSurface?.SimulationPanel;
		Button runtimeStart = realSimulator?.FindChild("SimulationStartButton", recursive: true, owned: false) as Button;
		Button runtimeReset = realSimulator?.FindChild("SimulationResetButton", recursive: true, owned: false) as Button;
		OptionButton runtimeEvent = realSimulator?.FindChild("SimulationEventPicker", recursive: true, owned: false) as OptionButton;
		Button runtimeSend = realSimulator?.FindChild("SimulationSendEventButton", recursive: true, owned: false) as Button;
		bool visualRuntimeControls = GodotObject.IsInstanceValid(runtimeStart) && GodotObject.IsInstanceValid(runtimeStart.Icon) && GodotObject.IsInstanceValid(runtimeReset) && GodotObject.IsInstanceValid(runtimeReset.Icon) && GodotObject.IsInstanceValid(runtimeEvent) && GodotObject.IsInstanceValid(runtimeSend) && GodotObject.IsInstanceValid(runtimeSend.Icon);
		runtimeStart?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		IStateMachineController firstController = _editor.CurrentRuntimePreview?.StateMachine;
		bool realRuntimeBound = visualRuntimeControls && (firstController?.IsInitialized ?? false) && _editor.EmbeddedStateMachineUsesRuntimePreview && _editor.EmbeddedStateMachineBoundController == firstController && realSimulator?.BoundRuntimeController == firstController && (realSimulator?.RuntimeSourceName.Contains("真实组件", StringComparison.Ordinal) ?? false) && SnapshotHasActive(realSimulator.CurrentSnapshot, "probe.idle");
		Require(realRuntimeBound, "Embedded state-machine debugger did not borrow the real component preview controller.");
		runtimeStart?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		bool borrowedRuntimeSafe = (firstController?.IsInitialized ?? false) && !_editor.EmbeddedStateMachineUsesRuntimePreview && _editor.CurrentRuntimePreview?.StateMachine == firstController;
		runtimeStart?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		borrowedRuntimeSafe &= (firstController?.IsInitialized ?? false) && _editor.EmbeddedStateMachineUsesRuntimePreview && _editor.EmbeddedStateMachineBoundController == firstController;
		Require(borrowedRuntimeSafe, "Stopping the embedded debugger disposed or replaced its borrowed gameplay controller.");
		if (runtimeEvent != null)
		{
			for (int i = 0; i < runtimeEvent.ItemCount; i++)
			{
				if (runtimeEvent.GetItemText(i) == "damage")
				{
					runtimeEvent.Select(i);
					break;
				}
			}
		}
		runtimeSend?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		int num;
		if (((_editor.CurrentRuntimePreview is CharacterComponentProbeRuntime characterComponentProbeRuntime) ? characterComponentProbeRuntime.ActiveStateId : null) == "probe.triggered" && SnapshotHasActive(realSimulator?.CurrentSnapshot, "probe.triggered"))
		{
			if (stateSurface != null && stateSurface.FindStateNode("probe.triggered")?.IsSimulationActive == true)
			{
				if (stateSurface != null && stateSurface.FindStateNode("probe.idle")?.IsSimulationActive == false)
				{
					if (realSimulator != null && realSimulator.TransitionHistoryCount == 1 && realSimulator.RuntimeTraceCount >= 3 && realSimulator.FindChild("RuntimeTraceList", recursive: true, owned: false) is ItemList itemList)
					{
						num = ((itemList.ItemCount == realSimulator.RuntimeTraceCount) ? 1 : 0);
						goto IL_14f7;
					}
				}
			}
		}
		num = 0;
		goto IL_14f7;
		IL_1656:
		int num2;
		bool realRuntimeReset = (byte)num2 != 0;
		Require(realRuntimeReset, "Reset did not rebuild the complete component preview and attach its replacement controller.");
		_editor.SetWorkbenchPage(1);
		await WaitFrames(3);
		int num3;
		IStateMachineController afterResetController;
		if (realSimulator != null && !realSimulator.RuntimeAttached)
		{
			if (realSimulator != null && !realSimulator.IsRunning)
			{
				num3 = ((afterResetController?.IsInitialized ?? false) ? 1 : 0);
				goto IL_171c;
			}
		}
		num3 = 0;
		goto IL_171c;
		IL_171c:
		bool hiddenRuntimeDetached = (byte)num3 != 0;
		Require(hiddenRuntimeDetached, "Leaving the state-machine page kept debugger callbacks attached or disposed the borrowed controller.");
		_editor.SetWorkbenchPage(3);
		await WaitFrames(3);
		bool embeddedLayout = false;
		if (stateMachine)
		{
			string layoutIdentity = _editor.EmbeddedStateMachineLayoutIdentity;
			StateMachineGraphNode stateMachineGraphNode = stateSurface.FindStateNode("probe.idle");
			Vector2 embeddedPosition = new Vector2(438f, 246f);
			Vector2 embeddedScroll = new Vector2(172f, 96f);
			if (stateMachineGraphNode != null)
			{
				stateMachineGraphNode.Selected = true;
				stateSurface.EmitSignal(GraphEdit.SignalName.BeginNodeMove);
				stateMachineGraphNode.PositionOffset = embeddedPosition;
				stateSurface.EmitSignal(GraphEdit.SignalName.EndNodeMove);
			}
			stateSurface.Zoom = 1.25f;
			stateSurface.ScrollOffset = embeddedScroll;
			stateSurface.FlushViewportState();
			await WaitFrames(3);
			if (!string.IsNullOrWhiteSpace(layoutIdentity))
			{
				ResourceLoader.Load<StateMachineLayout>(StateMachineLayoutStore.GetLayoutPath(layoutIdentity), "", ResourceLoader.CacheMode.Ignore);
			}
			CharacterComponentProbeDefinition resource = CreateDefinition("layout-alternate", 98);
			await OpenResource(resource);
			await OpenResource(definition);
			_editor.SetWorkbenchPage(3);
			await WaitFrames(3);
			StateMachineGraphEditorSurface stateMachineGraphEditorSurface = _editor.FindChild("StateMachineGraphEditorSurface", recursive: true, owned: false) as StateMachineGraphEditorSurface;
			StateMachineLayout stateMachineLayout = ResourceLoader.Load<StateMachineLayout>(StateMachineLayoutStore.GetLayoutPath(layoutIdentity), "", ResourceLoader.CacheMode.Ignore);
			embeddedLayout = !string.IsNullOrWhiteSpace(layoutIdentity) && layoutIdentity.Contains(definition.InstanceId, StringComparison.Ordinal) && stateMachineLayout != null && stateMachineLayout.Positions.TryGetValue("probe.idle", out var diskPosition) && diskPosition.IsEqualApprox(embeddedPosition) && stateMachineLayout.ScrollOffset.IsEqualApprox(embeddedScroll) && Math.Abs(stateMachineLayout.Zoom - 1.25f) < 0.001f && stateMachineGraphEditorSurface != null && stateMachineGraphEditorSurface.FindStateNode("probe.idle")?.PositionOffset.IsEqualApprox(embeddedPosition) == true && stateMachineGraphEditorSurface.ScrollOffset.IsEqualApprox(embeddedScroll) && Math.Abs(stateMachineGraphEditorSurface.Zoom - 1.25f) < 0.001f;
		}
		Require(embeddedLayout, "Embedded component state-machine position/viewport did not persist through a real resource switch and rebind.");
		int previewBuildBefore = _editor.RuntimePreviewBuildCount;
		_editor.SetWorkbenchPage(0);
		await WaitFrames(3);
		SubViewport instance = FindControl<SubViewport>("ComponentPreviewViewport");
		Label previewStatus = FindControl<Label>("PreviewStatusLabel");
		CharacterComponentProbeRuntime runtime = CharacterComponentProbeDefinition.LastRuntime;
		bool runtimePreview = GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(previewStatus) && CharacterComponentProbeDefinition.RuntimeCreateCount > 0 && runtime != null && runtime.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(runtime.Owner) && GodotObject.IsInstanceValid(runtime.Manager) && runtime.ComponentDefinition == definition && _editor.IsPreviewSurfaceActive && _editor.IsPreviewViewportRendering && !previewStatus.Text.Contains("失败", StringComparison.OrdinalIgnoreCase) && !previewStatus.Text.Contains("error", StringComparison.OrdinalIgnoreCase);
		bool lazySurfaces = offPagePreviewDeferred && previewBuiltForStateMachine && _editor.RuntimePreviewBuildCount == previewBuildBefore;
		Require(lazySurfaces, "Hidden component pages rebuilt a runtime preview, or the preview page rebuilt it more than once.");
		Require(runtimePreview, "The preview runtime is not Active or is missing its real Owner/Manager/ComponentDefinition binding.");
		string statusBefore = previewStatus?.Text ?? string.Empty;
		Control eventSimulator = FindControl<Control>("EventSimulator");
		Button button = FindControl<Button>("SimulateEventButton");
		bool eventSimulation = false;
		if (GodotObject.IsInstanceValid(eventSimulator) && GodotObject.IsInstanceValid(button))
		{
			_editor.SelectVisualEvent("damage");
			button.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			string b = previewStatus?.Text ?? string.Empty;
			eventSimulation = runtime != null && runtime.ActiveStateId == "probe.triggered" && !string.Equals(statusBefore, b, StringComparison.Ordinal);
		}
		Require(eventSimulation, "The event simulator changed status text but the bound runtime did not receive damage and transition state.");
		_editor.SetWorkbenchPage(1);
		await WaitFrames(2);
		bool pageStopped = !_editor.IsPreviewSurfaceActive && !_editor.IsPreviewViewportRendering && eventSimulator.ProcessMode == ProcessModeEnum.Disabled;
		_editor.Hide();
		await WaitFrames(2);
		bool hiddenStopped = pageStopped && !_editor.IsPreviewSurfaceActive && !_editor.IsPreviewViewportRendering && eventSimulator.ProcessMode == ProcessModeEnum.Disabled;
		_editor.Show();
		XWEditorInterface.Instance.FocusPanel("character_component_editor");
		_editor.SetWorkbenchPage(0);
		await WaitFrames(3);
		hiddenStopped &= _editor.IsPreviewSurfaceActive && _editor.IsPreviewViewportRendering;
		Require(hiddenStopped, "Hidden/off-page component preview or event sandbox kept processing.");
		Button button2 = FindButtonByText(_editor, "保存");
		Require(GodotObject.IsInstanceValid(button2), "Character-component toolbar save button is unavailable.");
		if (GodotObject.IsInstanceValid(button2))
		{
			button2.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(6);
		}
		CharacterComponentProbeDefinition characterComponentProbeDefinition = ResourceLoader.Load<CharacterComponentProbeDefinition>("user://mod_editor_character_component_responsive_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool flag = GodotObject.IsInstanceValid(characterComponentProbeDefinition) && characterComponentProbeDefinition.ProbePower == 42 && characterComponentProbeDefinition.ProbeTags.Count == 2 && characterComponentProbeDefinition.ProbeTags[0] == "alpha-edited" && characterComponentProbeDefinition.ProbeWeights.ContainsKey("new_key") && characterComponentProbeDefinition.ProbePackedFrames.Length == 3 && characterComponentProbeDefinition.ProbePackedFrames[0] == 21 && characterComponentProbeDefinition.ProbeOffset.IsEqualApprox(new Vector2(25.5f, -12.75f)) && characterComponentProbeDefinition.ProbeTint.IsEqualApprox(new Color(0.9f, 0.3f, 0.15f, 0.5f)) && SameResourcePath(characterComponentProbeDefinition.StateMachineDefinition?.BaseDefinition, _stateMachineBaseFixturePath);
		Require(flag, $"Component direct-property and collection edits did not survive save/reload: loaded={GodotObject.IsInstanceValid(characterComponentProbeDefinition)} power={characterComponentProbeDefinition?.ProbePower ?? (-1)} tags={characterComponentProbeDefinition?.ProbeTags?.Count ?? (-1)} firstTag={((characterComponentProbeDefinition != null && characterComponentProbeDefinition.ProbeTags?.Count > 0) ? characterComponentProbeDefinition.ProbeTags[0] : "<missing>")} newKey={characterComponentProbeDefinition?.ProbeWeights?.ContainsKey("new_key") == true}.");
		PanelContainer panelContainer2 = FindControl<PanelContainer>("InspectorPanel");
		VBoxContainer vBoxContainer = FindControl<VBoxContainer>("EmbeddedInspectorHost");
		bool flag2 = GodotObject.IsInstanceValid(panelContainer2) && !panelContainer2.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0;
		Require(flag2, "Character-component authoring touched the raw embedded Inspector.");
		bool realRuntimeTrace;
		return (dynamicExports: dynamicExports, typedArrayUndoRedo: typedArrayUndoRedo, typedDictionaryUndoRedo: typedDictionaryUndoRedo, stateMachine: stateMachine, embeddedLayout: embeddedLayout, embeddedPickerIncremental: embeddedPickerIncremental, realRuntimeBound: realRuntimeBound, realRuntimeTrace: realRuntimeTrace, borrowedRuntimeSafe: borrowedRuntimeSafe, realRuntimeReset: realRuntimeReset, hiddenRuntimeDetached: hiddenRuntimeDetached, runtimePreview: runtimePreview, eventSimulation: eventSimulation, responsive: responsive, visualSelection: visualSelection, propertyEdit: propertyEdit, packedInt32: packedInt32, hiddenStopped: hiddenStopped, saveReload: flag, inspectorUntouched: flag2, lazySurfaces: lazySurfaces, leafIncremental: leafIncremental);
		IL_14f7:
		realRuntimeTrace = (byte)num != 0;
		Require(realRuntimeTrace, "Real component event did not publish an exact transition trace and graph highlight.");
		CharacterComponentRuntime beforeResetRuntime = _editor.CurrentRuntimePreview;
		IStateMachineController beforeResetController = beforeResetRuntime?.StateMachine;
		runtimeReset?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		CharacterComponentRuntime currentRuntimePreview = _editor.CurrentRuntimePreview;
		afterResetController = currentRuntimePreview?.StateMachine;
		if (beforeResetRuntime != currentRuntimePreview && beforeResetController != afterResetController)
		{
			if (beforeResetController != null && !beforeResetController.IsInitialized && (afterResetController?.IsInitialized ?? false) && _editor.EmbeddedStateMachineUsesRuntimePreview && _editor.EmbeddedStateMachineBoundController == afterResetController)
			{
				num2 = (SnapshotHasActive(realSimulator?.CurrentSnapshot, "probe.idle") ? 1 : 0);
				goto IL_1656;
			}
		}
		num2 = 0;
		goto IL_1656;
	}

	private async Task<bool> ProbeVectorAndColorVisualEditors(CharacterComponentProbeDefinition definition)
	{
		SpinBox spinBox = FindControl<SpinBox>("Vector2X_ProbeOffset");
		SpinBox y = FindControl<SpinBox>("Vector2Y_ProbeOffset");
		ColorPickerButton color = FindControl<ColorPickerButton>("ColorPicker_ProbeTint");
		bool visualControls = GodotObject.IsInstanceValid(spinBox) && GodotObject.IsInstanceValid(y) && GodotObject.IsInstanceValid(color) && FindControl<Control>("Vector2Editor_ProbeOffset") != null && FindControl<Control>("ColorEditor_ProbeTint") != null;
		if (!visualControls)
		{
			return false;
		}
		_history.ClearHistory();
		Vector2 originalOffset = definition.ProbeOffset;
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.SetValueNoSignal(25.5);
		spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 25.5);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		y.EmitSignal(Control.SignalName.FocusEntered);
		y.SetValueNoSignal(-12.75);
		y.EmitSignal(Godot.Range.SignalName.ValueChanged, -12.75);
		y.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(2);
		bool vectorApplied = definition.ProbeOffset.IsEqualApprox(new Vector2(25.5f, -12.75f));
		bool vectorUndone = _history.Undo();
		await WaitFrames(2);
		Vector2 offsetAfterUndo = definition.ProbeOffset;
		vectorUndone &= offsetAfterUndo.IsEqualApprox(new Vector2(25.5f, originalOffset.Y)) || offsetAfterUndo.IsEqualApprox(originalOffset);
		bool vectorRedone = _history.Redo();
		await WaitFrames(2);
		vectorRedone &= definition.ProbeOffset.IsEqualApprox(new Vector2(25.5f, -12.75f));
		_history.ClearHistory();
		Color desired = new Color(0.9f, 0.3f, 0.15f, 0.5f);
		Color originalTint = definition.ProbeTint;
		color.EmitSignal(BaseButton.SignalName.Pressed);
		color.Color = desired;
		color.EmitSignal(ColorPickerButton.SignalName.ColorChanged, desired);
		color.EmitSignal(ColorPickerButton.SignalName.PopupClosed);
		await WaitFrames(2);
		bool colorApplied = definition.ProbeTint.IsEqualApprox(desired);
		bool colorUndone = _history.Undo();
		await WaitFrames(2);
		colorUndone &= definition.ProbeTint.IsEqualApprox(originalTint);
		bool colorRedone = _history.Redo();
		await WaitFrames(2);
		colorRedone &= definition.ProbeTint.IsEqualApprox(desired) && color.Color.IsEqualApprox(desired);
		GD.Print($"[MOD_EDITOR_CHARACTER_COMPONENT_VECTOR_COLOR] controls={visualControls} vectorApplied={vectorApplied} vectorUndone={vectorUndone} vectorRedone={vectorRedone} colorApplied={colorApplied} colorUndone={colorUndone} colorRedone={colorRedone} offsetAfterUndo={offsetAfterUndo} offset={definition.ProbeOffset} tint={definition.ProbeTint}");
		return vectorApplied && vectorUndone && vectorRedone && colorApplied && colorUndone && colorRedone;
	}

	private async Task<bool> ProbeEmbeddedStateMachineResourcePicker(CharacterComponentProbeDefinition component, StateMachineGraphEditorSurface surface)
	{
		StateMachineDefinition definition = component?.StateMachineDefinition;
		if (!GodotObject.IsInstanceValid(definition) || !GodotObject.IsInstanceValid(surface))
		{
			Require(condition: false, "Embedded state-machine picker probe has no bound definition or graph surface.");
			return false;
		}
		surface.ShowDefinitionOverview();
		await WaitFrames(2);
		Button button = surface.FindChild("BaseDefinitionPickerButton", recursive: true, owned: false) as Button;
		Require(GodotObject.IsInstanceValid(button), "Embedded state-machine definition details have no BaseDefinition visual picker.");
		if (!GodotObject.IsInstanceValid(button))
		{
			return false;
		}
		int fullRefreshBefore = _editor.FullSurfaceRefreshCount;
		int leafRefreshBefore = _editor.LeafPropertyRefreshCount;
		int stateBindBefore = _editor.StateMachineSurfaceBindCount;
		int runtimeBuildBefore = _editor.RuntimePreviewBuildCount;
		StateMachineGraphController controller = surface.GraphController;
		CharacterComponentRuntime runtime = _editor.CurrentRuntimePreview;
		StateMachineGraphNode localNode = surface.FindStateNode("probe.idle");
		Vector2 scrollBefore = surface.ScrollOffset;
		float zoomBefore = surface.Zoom;
		_history.ClearHistory();
		button.EmitSignal(BaseButton.SignalName.Pressed);
		var (pickerConfirmed, projectPath, typeFilter) = await ChooseOpenStateMachineBasePicker(_stateMachineBaseFixturePath, 600);
		await WaitFrames(4);
		bool assigned = pickerConfirmed && SameResourcePath(definition.BaseDefinition, _stateMachineBaseFixturePath) && (surface.UndoAdapter?.CanUndo ?? false);
		bool compositionAssigned = GodotObject.IsInstanceValid(surface.FindStateNode("base.idle"));
		bool undoCalled = surface.UndoAdapter?.CanUndo ?? false;
		if (undoCalled)
		{
			surface.GraphController.Undo();
		}
		await WaitFrames(3);
		bool undone = undoCalled && definition.BaseDefinition == null && !GodotObject.IsInstanceValid(surface.FindStateNode("base.idle"));
		bool redoCalled = surface.UndoAdapter?.CanRedo ?? false;
		if (redoCalled)
		{
			surface.GraphController.Redo();
		}
		await WaitFrames(3);
		bool flag = redoCalled && SameResourcePath(definition.BaseDefinition, _stateMachineBaseFixturePath) && GodotObject.IsInstanceValid(surface.FindStateNode("base.idle"));
		bool flag2 = _editor.FullSurfaceRefreshCount == fullRefreshBefore && _editor.LeafPropertyRefreshCount == leafRefreshBefore && _editor.StateMachineSurfaceBindCount == stateBindBefore && _editor.RuntimePreviewBuildCount == runtimeBuildBefore && surface == _editor.FindChild("StateMachineGraphEditorSurface", recursive: true, owned: false) && controller == surface.GraphController && runtime == _editor.CurrentRuntimePreview && localNode == surface.FindStateNode("probe.idle") && surface.ScrollOffset.IsEqualApprox(scrollBefore) && Math.Abs(surface.Zoom - zoomBefore) < 0.001f && surface.Definition == definition && _editor.EmbeddedStateMachineIsVisible;
		bool flag3 = projectPath && typeFilter && assigned && compositionAssigned && undone && flag && flag2;
		GD.Print($"[MOD_EDITOR_CHARACTER_COMPONENT_STATE_PICKER] fullDelta={_editor.FullSurfaceRefreshCount - fullRefreshBefore} leafDelta={_editor.LeafPropertyRefreshCount - leafRefreshBefore} stateBindDelta={_editor.StateMachineSurfaceBindCount - stateBindBefore} runtimeBuildDelta={_editor.RuntimePreviewBuildCount - runtimeBuildBefore} surfaceStable={surface == _editor.FindChild("StateMachineGraphEditorSurface", recursive: true, owned: false)} controllerStable={controller == surface.GraphController} localNodeStable={localNode == surface.FindStateNode("probe.idle")} projectPath={projectPath} typeFilter={typeFilter} assigned={assigned} composition={compositionAssigned} undoRedo={undone && flag} stable={flag2}");
		Require(flag3, $"Embedded BaseDefinition picker bypassed the state controller or rebuilt the component surface: picker={pickerConfirmed} project={projectPath} typeFilter={typeFilter} assigned={assigned}/{compositionAssigned} undo={undone} redo={flag} stable={flag2} full={fullRefreshBefore}->{_editor.FullSurfaceRefreshCount} stateBind={stateBindBefore}->{_editor.StateMachineSurfaceBindCount} runtimeBuild={runtimeBuildBefore}->{_editor.RuntimePreviewBuildCount}.");
		return flag3;
	}

	private async Task<bool> ProbePackedInt32ArrayEdit(CharacterComponentProbeDefinition definition)
	{
		SpinBox spinBox = FindControl<SpinBox>("PackedInt32_ProbePackedFrames_0");
		bool flag = GodotObject.IsInstanceValid(spinBox);
		Require(flag, "ProbePackedFrames PackedInt32Array has no direct numeric element control.");
		if (!flag)
		{
			return false;
		}
		_history.ClearHistory();
		spinBox.EmitSignal(Control.SignalName.FocusEntered);
		spinBox.SetValueNoSignal(21.0);
		spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, 21.0);
		spinBox.EmitSignal(Control.SignalName.FocusExited);
		await WaitFrames(3);
		bool applied = definition.ProbePackedFrames.Length == 3 && definition.ProbePackedFrames[0] == 21 && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone &= definition.ProbePackedFrames.Length == 3 && definition.ProbePackedFrames[0] == 2;
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone &= definition.ProbePackedFrames.Length == 3 && definition.ProbePackedFrames[0] == 21;
		bool flag2 = applied && undone && redone;
		Require(flag2, "PackedInt32Array numeric element edit did not preserve its typed snapshot through UndoRedo.");
		return flag2;
	}

	private async Task<bool> ProbeTypedDictionaryAdd(CharacterComponentProbeDefinition definition)
	{
		Button button = FindPropertyButton("ProbeWeights");
		Require(GodotObject.IsInstanceValid(button), "ProbeWeights Dictionary<string,int> has no visual add control.");
		if (!GodotObject.IsInstanceValid(button))
		{
			return false;
		}
		_history.ClearHistory();
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool applied = definition.ProbeWeights.Count == 2 && definition.ProbeWeights.ContainsKey("new_key") && definition.ProbeWeights["new_key"] == 0 && _history.HasUndo();
		bool undoCalled = _history.Undo();
		await WaitFrames(3);
		bool undone = undoCalled && definition.ProbeWeights.Count == 1 && !definition.ProbeWeights.ContainsKey("new_key") && definition.ProbeWeights["initial"] == 7;
		bool redoCalled = _history.Redo();
		await WaitFrames(3);
		bool flag = redoCalled && definition.ProbeWeights.Count == 2 && definition.ProbeWeights.ContainsKey("new_key") && definition.ProbeWeights["new_key"] == 0;
		bool flag2 = applied && undone && flag;
		Require(flag2, "Typed Dictionary<string,int> add did not preserve key/value types through UndoRedo.");
		return flag2;
	}

	private async Task<bool> ProbeTypedStringArrayEdit(CharacterComponentProbeDefinition definition)
	{
		LineEdit lineEdit = FindLineEditByText("alpha");
		Require(GodotObject.IsInstanceValid(lineEdit), "ProbeTags Array<string> has no direct editable element control.");
		if (!GodotObject.IsInstanceValid(lineEdit))
		{
			return false;
		}
		_history.ClearHistory();
		SimulateTextSession(lineEdit, "alpha-edited");
		await WaitFrames(3);
		bool applied = definition.ProbeTags.Count == 2 && definition.ProbeTags[0] == "alpha-edited" && definition.ProbeTags[1] == "beta" && _history.HasUndo();
		bool undone = _history.Undo();
		await WaitFrames(3);
		undone = undone && definition.ProbeTags.Count == 2 && definition.ProbeTags[0] == "alpha" && definition.ProbeTags[1] == "beta";
		bool redone = _history.Redo();
		await WaitFrames(3);
		redone = redone && definition.ProbeTags.Count == 2 && definition.ProbeTags[0] == "alpha-edited" && definition.ProbeTags[1] == "beta";
		bool flag = applied && undone && redone;
		Require(flag, "Typed Array<string> element editing did not preserve element type and round-trip through UndoRedo.");
		return flag;
	}

	private async Task OpenResource(Resource resource)
	{
		XWEditorInterface.Instance.EditResource(resource);
		await WaitFrames(5);
		Require(XWEditorInterface.Instance.GetResourceEditor("character_component_editor") == _editor, "Character-component routing switched away from the registered editor.");
	}

	private async Task<bool> ChooseOpenResourcePicker(string resourcePath, int maxFrames)
	{
		string normalizedPath = NormalizePath(resourcePath);
		string diagnostic = "picker not created";
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWGameplayResourcePickerWindow[] source = _editor.FindChildren("*", "", recursive: true, owned: false).OfType<XWGameplayResourcePickerWindow>().Where(GodotObject.IsInstanceValid)
				.ToArray();
			XWGameplayResourcePickerWindow picker = source.FirstOrDefault((XWGameplayResourcePickerWindow candidate) => candidate.Visible) ?? source.LastOrDefault((XWGameplayResourcePickerWindow candidate) => !string.IsNullOrWhiteSpace(candidate.ResourceLibraryProjectPath)) ?? source.LastOrDefault();
			Tree tree = picker?.FindChild("ChoiceTree", recursive: true, owned: false) as Tree;
			TreeItem treeItem = FindPickerTreeItem(tree?.GetRoot()?.GetFirstChild(), normalizedPath);
			if (GodotObject.IsInstanceValid(picker))
			{
				IReadOnlyList<XWGameplayResourceChoice> indexedChoices = picker.GetIndexedChoices(XWGameplayResourceKind.Resource);
				diagnostic = $"visible={picker.Visible} indexing={picker.IsResourceLibraryIndexing} project={picker.ResourceLibraryProjectPath} choices={indexedChoices.Count} paths={string.Join("|", from choice in indexedChoices.Take(6)
					select choice.ResourcePath)}";
				if (picker.Visible && !picker.IsResourceLibraryIndexing && treeItem == null)
				{
					XWGameplayResourceChoice xWGameplayResourceChoice = indexedChoices.FirstOrDefault((XWGameplayResourceChoice choice) => string.Equals(CanonicalResourcePath(choice.ResourcePath), CanonicalResourcePath(resourcePath), StringComparison.OrdinalIgnoreCase));
					if (xWGameplayResourceChoice != null && picker.TryConfirmIndexedResource(xWGameplayResourceChoice.ResourcePath))
					{
						return true;
					}
				}
			}
			if (GodotObject.IsInstanceValid(picker) && picker.Visible && treeItem != null)
			{
				treeItem.Select(0);
				tree.EmitSignal(Tree.SignalName.ItemSelected);
				await WaitFrames(1);
				Button button = picker.FindChild("ConfirmButton", recursive: true, owned: false) as Button;
				if (GodotObject.IsInstanceValid(button) && !button.Disabled)
				{
					button.EmitSignal(BaseButton.SignalName.Pressed);
					return true;
				}
			}
			await WaitFrames(1);
		}
		GD.Print("[MOD_EDITOR_CHARACTER_COMPONENT_PICKER_TIMEOUT] target=" + resourcePath + " " + diagnostic);
		return false;
	}

	private async Task<(bool Confirmed, bool ProjectPath, bool TypeFilter)> ChooseOpenStateMachineBasePicker(string resourcePath, int maxFrames)
	{
		string diagnostic = "picker not created";
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWGameplayResourcePickerWindow xWGameplayResourcePickerWindow = _editor.FindChildren("*", "", recursive: true, owned: false).OfType<XWGameplayResourcePickerWindow>().Where(GodotObject.IsInstanceValid)
				.FirstOrDefault((XWGameplayResourcePickerWindow candidate) => candidate.Visible);
			if (GodotObject.IsInstanceValid(xWGameplayResourcePickerWindow) && !xWGameplayResourcePickerWindow.IsResourceLibraryIndexing)
			{
				IReadOnlyList<XWGameplayResourceChoice> indexedChoices = xWGameplayResourcePickerWindow.GetIndexedChoices(XWGameplayResourceKind.Resource);
				XWGameplayResourceChoice xWGameplayResourceChoice = indexedChoices.FirstOrDefault((XWGameplayResourceChoice choice) => choice.IsModResource && string.Equals(CanonicalResourcePath(choice.ResourcePath), CanonicalResourcePath(resourcePath), StringComparison.OrdinalIgnoreCase));
				bool item = SamePhysicalPath(xWGameplayResourcePickerWindow.ResourceLibraryProjectPath, _modProjectPath);
				bool flag = !indexedChoices.Any((XWGameplayResourceChoice choice) => string.Equals(CanonicalResourcePath(choice.ResourcePath), CanonicalResourcePath(_shapeFixturePath), StringComparison.OrdinalIgnoreCase));
				bool item2 = xWGameplayResourceChoice != null && flag;
				return (Confirmed: xWGameplayResourceChoice != null && xWGameplayResourcePickerWindow.TryConfirmIndexedResource(xWGameplayResourceChoice.ResourcePath), ProjectPath: item, TypeFilter: item2);
			}
			await WaitFrames(1);
		}
		GD.Print($"[MOD_EDITOR_CHARACTER_COMPONENT_STATE_PICKER_TIMEOUT] target={resourcePath} {diagnostic}");
		return (Confirmed: false, ProjectPath: false, TypeFilter: false);
	}

	private static TreeItem FindPickerTreeItem(TreeItem item, string normalizedPath)
	{
		for (TreeItem treeItem = item; treeItem != null; treeItem = treeItem.GetNext())
		{
			if (NormalizePath(treeItem.GetTooltipText(0)).Contains(normalizedPath, StringComparison.OrdinalIgnoreCase))
			{
				return treeItem;
			}
			TreeItem treeItem2 = FindPickerTreeItem(treeItem.GetFirstChild(), normalizedPath);
			if (treeItem2 != null)
			{
				return treeItem2;
			}
		}
		return null;
	}

	private static bool SameResourcePath(Resource resource, string expectedPath)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			return string.Equals(CanonicalResourcePath(resource.ResourcePath), CanonicalResourcePath(expectedPath), StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string CanonicalResourcePath(string path)
	{
		string text = NormalizePath(path);
		if (text.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return NormalizePath(ProjectSettings.GlobalizePath(text));
		}
		return text;
	}

	private static string NormalizePath(string path)
	{
		return (path ?? string.Empty).Replace('\\', '/').Trim();
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance != null && instance.GetResourceEditor("character_component_editor") is XWCharacterComponentVisualResourceEditor xWCharacterComponentVisualResourceEditor && GodotObject.IsInstanceValid(xWCharacterComponentVisualResourceEditor))
			{
				_editor = xWCharacterComponentVisualResourceEditor;
				_history = instance.GetUndoRedoManager();
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
				XWEditorInterface.Instance.FocusPanel("character_component_editor");
				await WaitFrames(3);
				return _editor.IsVisibleInTree();
			}
			await WaitFrames(1);
		}
		return false;
	}

	private T FindControl<T>(string name) where T : Node
	{
		return _editor?.FindChild(name, recursive: true, owned: false) as T;
	}

	private T FindPropertyEditor<T>(string propertyName) where T : Control
	{
		Control control = FindControl<Control>("ComponentPropertyHost");
		if (!GodotObject.IsInstanceValid(control))
		{
			return null;
		}
		string text = NormalizePropertyLabel(propertyName);
		foreach (Node item in control.FindChildren("*", "Label", recursive: true, owned: false))
		{
			if (!(item is Label label) || NormalizePropertyLabel(label.Text) != text)
			{
				continue;
			}
			Node parent = label.GetParent();
			while (GodotObject.IsInstanceValid(parent) && parent != control)
			{
				foreach (Node item2 in parent.FindChildren("*", "", recursive: true, owned: false))
				{
					if (item2 is T val && GodotObject.IsInstanceValid(val))
					{
						return val;
					}
				}
				parent = parent.GetParent();
			}
		}
		return null;
	}

	private Button FindPropertyButton(string propertyName)
	{
		Control control = FindControl<Control>("ComponentPropertyHost");
		if (!GodotObject.IsInstanceValid(control))
		{
			return null;
		}
		string text = NormalizePropertyLabel(propertyName);
		foreach (Node item in control.FindChildren("*", "Label", recursive: true, owned: false))
		{
			if (!(item is Label label) || NormalizePropertyLabel(label.Text) != text)
			{
				continue;
			}
			foreach (Node item2 in label.GetParent().FindChildren("*", "Button", recursive: true, owned: false))
			{
				if (item2 is Button result)
				{
					return result;
				}
			}
		}
		return null;
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

	private static string NormalizePropertyLabel(string value)
	{
		return (value ?? string.Empty).Replace(" ", string.Empty).Replace("_", string.Empty).Trim();
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

	private static string CollectVisibleText(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return string.Empty;
		}
		List<string> list = new List<string>();
		CollectVisibleText(root, list);
		return string.Join("\n", list);
	}

	private static void CollectVisibleText(Node node, List<string> parts)
	{
		if (!(node is Label label))
		{
			if (!(node is Button button))
			{
				if (!(node is LineEdit lineEdit))
				{
					if (!(node is ItemList itemList))
					{
						if (node is Tree tree)
						{
							CollectTreeText(tree.GetRoot(), parts);
						}
					}
					else
					{
						for (int i = 0; i < itemList.ItemCount; i++)
						{
							parts.Add(itemList.GetItemText(i));
						}
					}
				}
				else
				{
					parts.Add(lineEdit.Text);
					parts.Add(lineEdit.PlaceholderText);
				}
			}
			else
			{
				parts.Add(button.Text);
			}
		}
		else
		{
			parts.Add(label.Text);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectVisibleText(child, parts);
		}
	}

	private static void CollectTreeText(TreeItem item, List<string> parts)
	{
		for (TreeItem treeItem = item; treeItem != null; treeItem = treeItem.GetNext())
		{
			parts.Add(treeItem.GetText(0));
			CollectTreeText(treeItem.GetFirstChild(), parts);
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private static void SimulateTextSession(LineEdit control, string value)
	{
		control.EmitSignal(Control.SignalName.FocusEntered);
		control.Text = value;
		control.EmitSignal(LineEdit.SignalName.TextChanged, value);
		control.EmitSignal(LineEdit.SignalName.TextSubmitted, value);
		control.EmitSignal(Control.SignalName.FocusExited);
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
			GD.PrintErr("[MOD_EDITOR_CHARACTER_COMPONENT_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		if (_failures.Count > 0)
		{
			foreach (string failure in _failures)
			{
				GD.PrintErr("[MOD_EDITOR_CHARACTER_COMPONENT_PROBE_FAILURE] " + failure);
			}
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
