using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

public partial class ModEditorStateMachineProbe : Node
{
	private const string SaveProbePath = "user://mod_editor_state_machine_workbench_probe.tres";

	private const string NestedSaveAsProbePath = "user://mod_editor_state_machine_nested_save_as_probe.tres";

	private const string ExternalGuardSourcePath = "user://mod_editor_external_guard_source.tres";

	private const string ExternalGuardSaveAsPath = "user://mod_editor_external_guard_save_as.tres";

	private const string SwitchProbePath = "user://mod_editor_state_machine_switch_probe.tres";

	private const string InheritedOverrideCheckpointPath = "user://mod_editor_state_machine_inherited_override_checkpoint.tres";

	private const string InheritedOverrideRestoredPath = "user://mod_editor_state_machine_inherited_override_restored.tres";

	private const string DerivedDefinitionProbePath = "user://mod_editor_state_machine_derived_definition_probe.tres";

	private const string DerivedElementProbePath = "user://mod_editor_state_machine_derived_element_probe.tres";

	private const string DerivedTypeCreationProbePath = "user://mod_editor_state_machine_derived_type_creation_probe.tres";

	private const string CompositionFailureProbePath = "user://mod_editor_state_machine_composition_failure_probe.tres";

	private const string CollapseProbePath = "user://mod_editor_state_machine_collapse_probe.tres";

	private const string CollapseSwitchProbePath = "user://mod_editor_state_machine_collapse_switch_probe.tres";

	private const string DerivedStateAuthorTypeId = "registered:mod_editor_probe_derived_state";

	private const string DerivedTransitionAuthorTypeId = "registered:mod_editor_probe_derived_transition";

	private readonly List<string> _failures = new List<string>();

	public override async void _Ready()
	{
		bool window = false;
		bool autoloadEntry = false;
		bool dedicatedGraphInput = false;
		bool modEmptySurface = false;
		bool route = false;
		bool graph = false;
		bool responsive820 = false;
		bool createSelectRename = false;
		bool detailsSticky = false;
		bool guardContinuous = false;
		bool recursiveGuardPuzzle = false;
		bool nestedGuardRootSave = false;
		bool nestedGuardRootSaveAs = false;
		bool nestedGuardUndoRedo = false;
		bool guardKindAtomicUndo = false;
		bool malformedGuardUiSafe = false;
		bool singleUndo = false;
		bool toolbarUndoRouting = false;
		bool numericDebounce = false;
		bool debounceSingleTimer = false;
		bool viewportDebounce = false;
		bool viewportReload = false;
		bool groupedMoveUndo = false;
		bool hiddenViewportFlush = false;
		bool switchViewportFlush = false;
		bool surfaceSignals = false;
		bool contextMenu = false;
		bool clipboardSignals = false;
		bool pointerPlacement = false;
		bool shortcutUndoRedo = false;
		bool connection = false;
		bool conditionEdit = false;
		bool actionEdit = false;
		bool curvedConnectionHit = false;
		bool parentCycleRejected = false;
		bool rejectedClean = false;
		bool hierarchyGuards = false;
		bool kindInvariant = false;
		bool rootSwitchInvariant = false;
		bool undoRedo = false;
		bool saveReload = false;
		bool modResourcePicker = false;
		bool inspectorUntouched = false;
		bool hiddenStopped = false;
		bool sharedSurface = false;
		bool nativeEmpty = false;
		bool narrow420 = false;
		bool toolbarInteraction = false;
		bool narrowDirectEdit = false;
		bool narrowKinds = false;
		bool hierarchySafeAdd = false;
		bool nestedParentAdd = false;
		bool emptyInitialization = false;
		bool clipboardParent = false;
		bool connectionDetails = false;
		bool transitionWorkbench = false;
		bool transitionChips = false;
		bool multiTransitionChips = false;
		bool transitionChipDirectEdit = false;
		bool transitionChipViewportFollow = false;
		bool transitionChipCollapse = false;
		bool transitionChipSaveReload = false;
		bool transitionChipInspectorUntouched = false;
		bool transitionChipRuntime = false;
		bool multiConnectionSelect = false;
		bool diagnostics = false;
		bool simulation = false;
		bool expressionProperties = false;
		bool guardedTransition = false;
		bool activeHighlight = false;
		bool delayed = false;
		bool automatic = false;
		bool historyBack = false;
		bool reset = false;
		bool sideEffectFree = false;
		bool idleSimulationCoalesced = false;
		bool compiledRuntimeTrace = false;
		bool visualDebugControls = false;
		bool pauseStable = false;
		bool singleStepControl = false;
		bool traceBounded = false;
		bool highlightTrace = false;
		bool traceRestore = false;
		bool resetControl = false;
		bool hiddenRuntimeStopped = false;
		bool inspectorAfterTrace = false;
		bool collapseControl = false;
		bool collapseDescendants = false;
		bool collapseConnections = false;
		bool collapseSelectionClean = false;
		bool collapseUndoRedo = false;
		bool collapseSaveReload = false;
		bool collapseExpandRestore = false;
		bool collapseInspectorUntouched = false;
		bool initialButtonGuard = false;
		bool largeGraphIncremental = false;
		bool largeCollapseIncremental = false;
		bool largeTransitionChipIncremental = false;
		bool largeTransitionChipMoveScope = false;
		bool signalLifecycleClean = false;
		bool resourcePaletteForeground = false;
		bool nodeInputContract = false;
		bool derivedTransitionOwner = false;
		bool inheritedOverrideWorkbench = false;
		bool derivedDefinitionWorkbench = false;
		bool derivedElementWorkbench = false;
		bool derivedTypeCreation = false;
		bool compositionFailureGuard = false;
		try
		{
			string a = StateMachineAuthorTypeRegistry.RegisterState<ModEditorStateMachineProbeDerivedState>("派生探针状态", "F3 探针注册的 C# 派生状态", "", "mod_editor_probe_derived_state");
			string a2 = StateMachineAuthorTypeRegistry.RegisterTransition<ModEditorStateMachineProbeDerivedTransition>("派生探针转换", "F3 探针注册的 C# 派生转换", "", "mod_editor_probe_derived_transition");
			bool authorTypesRegistered = string.Equals(a, "registered:mod_editor_probe_derived_state", StringComparison.Ordinal) && string.Equals(a2, "registered:mod_editor_probe_derived_transition", StringComparison.Ordinal);
			Require(authorTypesRegistered, "C# derived state/transition author types were not registered before opening F3 ModEditor.");
			ModEditorManager manager = ModEditorManager.Instance;
			autoloadEntry = GodotObject.IsInstanceValid(manager);
			Require(autoloadEntry, "ModEditorManager is not active from project.godot; the F3 entry would be unavailable in the real game.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				manager = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/Core/ModEditorManager.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<ModEditorManager>(PackedScene.GenEditState.Disabled);
				if (GodotObject.IsInstanceValid(manager))
				{
					AddChild(manager, forceReadableName: false, InternalMode.Disabled);
				}
			}
			Require(GodotObject.IsInstanceValid(manager), "ModEditorManager could not be instantiated.");
			resourcePaletteForeground = await ProbeResourcePaletteForeground();
			Require(resourcePaletteForeground, "Resource workspace palette was not exclusive or lost its visible-session state when reopened.");
			XWStateMachineVisualResourceEditor editor;
			XWInspector inspector;
			Node sentinel;
			StateMachineDefinition definition;
			StateMachineGraphEditorSurface surface;
			string newStateId;
			Vector2 localMidpoint;
			StateMachineSimulationPanel simulator;
			Node sceneBeforeSimulation;
			LineEdit expressionKey;
			Button expressionNumberType;
			LineEdit expressionValue;
			Button setExpression;
			Button visualResume;
			Button visualStep;
			Button visualReset;
			OptionButton visualEvent;
			Button visualSend;
			ItemList runtimeTrace;
			int num8;
			if (GodotObject.IsInstanceValid(manager))
			{
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
				editor = await WaitForEditor(900);
				window = editor != null && FindAncestorWindow(editor) != null;
				dedicatedGraphInput = editor?.UsesDedicatedGraphInputHost ?? false;
				Require(window, "State-machine editor was not mounted in the F3 ModEditor window.");
				Require(dedicatedGraphInput, "The generic outer scroll host still intercepts state-machine graph gestures.");
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface = editor?.GraphSurface;
				modEmptySurface = stateMachineGraphEditorSurface != null && stateMachineGraphEditorSurface.Definition == null && stateMachineGraphEditorSurface.SceneFilePath == "res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn" && stateMachineGraphEditorSurface.FindChild("StateMachineEmptyStatePanel", recursive: true, owned: false) is Control { Visible: not false } && stateMachineGraphEditorSurface.FindChild("CreateDefinitionButton", recursive: true, owned: false) is Button && stateMachineGraphEditorSurface.FindChild("OpenDefinitionButton", recursive: true, owned: false) is Button;
				Require(modEmptySurface, "ModEditor did not mount the shared graph surface and create/open entry before a resource was opened.");
				inspector = XWEditorInterface.Instance?.GetInspector() as XWInspector;
				sentinel = new Node
				{
					Name = "StateMachineInspectorSentinel"
				};
				AddChild(sentinel, forceReadableName: false, InternalMode.Disabled);
				XWEditorInterface.Instance?.InspectObject(sentinel);
				await WaitFrames(2);
				definition = CreateSimulationDefinition();
				route = XWResourceEditorRegistry.TryGetEditor(definition, "user://mod_editor_state_machine_workbench_probe.tres", out var descriptor) && descriptor?.DockKey == "state_machine_editor" && XWResourceEditorRegistry.TryOpen(definition, "user://mod_editor_state_machine_workbench_probe.tres");
				await WaitFrames(3);
				graph = editor?.GraphSurface != null;
				Require(route && graph, "StateMachineDefinition did not route to the reusable graph surface.");
				surface = editor?.GraphSurface;
				sharedSurface = surface != null && surface.SceneFilePath == "res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn";
				Require(sharedSurface, "ModEditor did not instance the canonical shared state-machine scene.");
				responsive820 = await ProbeResponsiveSurface820(definition);
				Require(responsive820, "Canonical state-machine surface clipped or lost its compact toolbar at 820px.");
				(nativeEmpty, narrow420, toolbarInteraction, narrowDirectEdit, narrowKinds) = await ProbeNativeEmptyAndNarrowSurface();
				Require(nativeEmpty, "Native shared state-machine surface has no create/open empty state.");
				Require(narrow420, "Native shared state-machine surface does not fit a 420px main screen.");
				Require(toolbarInteraction, "Native empty-state create/open controls did not dispatch host requests.");
				Require(narrowDirectEdit, "Narrow state-machine layout could not open and use its direct-edit drawer.");
				Require(narrowKinds, "Narrow state-machine layout could not visually create every state kind.");
				emptyInitialization = await ProbeEmptyDefinitionInitialization();
				Require(emptyInitialization, "A non-null empty definition had no usable visual initialization action.");
				surface?.SetWorkbenchActive(active: true);
				nodeInputContract = ProbeStateMachineNodeInputContract(surface);
				Require(nodeInputContract, "State node decorations or action buttons did not preserve the native GraphEdit input contract.");
				StateMachineGraphNode stateMachineGraphNode = surface?.FindStateNode("Root");
				if (stateMachineGraphNode != null)
				{
					stateMachineGraphNode.Selected = true;
				}
				OptionButton stateKindPicker = surface?.FindChild("NewStateKind", recursive: true, owned: false) as OptionButton;
				Button addStateButton = surface?.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
				int stateCountBeforeHierarchyAdd = definition.States.Count;
				if (stateKindPicker != null && addStateButton != null)
				{
					int itemIndex = stateKindPicker.GetItemIndex(1);
					stateKindPicker.Select(itemIndex);
					addStateButton.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(3);
				StateMachineStateDefinition addedContainer = null;
				foreach (StateMachineStateDefinition state in definition.States)
				{
					if (state != null && state.Kind == StateMachineStateKind.Compound && state.StableId != "Root")
					{
						addedContainer = state;
					}
				}
				StateMachineStateDefinition addedDefaultChild = FindState(definition, addedContainer?.InitialChildId);
				hierarchySafeAdd = definition.States.Count == stateCountBeforeHierarchyAdd + 2 && addedContainer?.ParentId == "Root" && addedDefaultChild?.ParentId == addedContainer.StableId && StateMachineValidator.Validate(definition).IsValid;
				Require(hierarchySafeAdd, "Toolbar state creation left the definition in an invalid hierarchy or split one add across multiple repair steps.");
				StateMachineGraphNode stateMachineGraphNode2 = surface?.FindStateNode(addedContainer?.StableId);
				if (stateMachineGraphNode2 != null && stateKindPicker != null && addStateButton != null)
				{
					foreach (StringName selectedStateNode in surface.GetSelectedStateNodes())
					{
						surface.FindStateNode(selectedStateNode.ToString()).Selected = false;
					}
					stateMachineGraphNode2.Selected = true;
					int itemIndex2 = stateKindPicker.GetItemIndex(0);
					stateKindPicker.Select(itemIndex2);
					addStateButton.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(2);
				foreach (StateMachineStateDefinition state2 in definition.States)
				{
					if (state2 != null && state2.ParentId == addedContainer?.StableId && state2.StableId != addedDefaultChild?.StableId)
					{
						nestedParentAdd = true;
					}
				}
				Require(nestedParentAdd, "A child created while a nested compound state was selected fell back to the root.");
				newStateId = surface?.GraphController?.AddState("ProbeCreated", StateMachineStateKind.Atomic, "Root");
				surface?.RefreshGraph();
				surface?.NavigateToStableId(newStateId);
				await WaitFrames(2);
				if (surface?.FindChild("DisplayNameEdit", recursive: true, owned: false) is LineEdit lineEdit)
				{
					lineEdit.Text = "ProbeRenamed";
					lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
					lineEdit.EmitSignal(Control.SignalName.FocusExited);
				}
				await WaitFrames(2);
				surface?.RefreshGraph();
				await WaitFrames(1);
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface2 = surface;
				detailsSticky = stateMachineGraphEditorSurface2 != null && stateMachineGraphEditorSurface2.FindStateNode(newStateId)?.Selected == true && surface.FindChild("DisplayNameEdit", recursive: true, owned: false) is LineEdit lineEdit2 && lineEdit2.Text == "ProbeRenamed";
				Require(detailsSticky, "State edit refresh lost the selected state or its direct details panel.");
				surface?.GraphController?.Undo();
				surface?.RefreshGraph();
				await WaitFrames(1);
				bool oneUndoRestoredOriginal = FindState(definition, newStateId)?.DisplayName.ToString() == "ProbeCreated";
				surface?.GraphController?.Redo();
				surface?.RefreshGraph();
				await WaitFrames(1);
				singleUndo = oneUndoRestoredOriginal && FindState(definition, newStateId)?.DisplayName.ToString() == "ProbeRenamed";
				Require(singleUndo, "TextSubmitted plus FocusExited created more than one rename Undo step.");
				toolbarUndoRouting = await ProbeTopToolbarUndoRouting(editor, definition);
				Require(toolbarUndoRouting, "Top resource-toolbar Undo/Redo rebuilt the graph surface or bypassed the state-machine controller history.");
				int num;
				if (!string.IsNullOrWhiteSpace(newStateId) && FindState(definition, newStateId)?.DisplayName.ToString() == "ProbeRenamed")
				{
					StateMachineGraphEditorSurface stateMachineGraphEditorSurface3 = surface;
					num = ((stateMachineGraphEditorSurface3 != null && stateMachineGraphEditorSurface3.FindStateNode(newStateId)?.Selected == true) ? 1 : 0);
				}
				else
				{
					num = 0;
				}
				createSelectRename = (byte)num != 0;
				Require(createSelectRename, "Create/select/rename did not round-trip through the central visual state node.");
				(parentCycleRejected, rejectedClean, hierarchyGuards) = ProbeHierarchyEditGuards(surface, newStateId);
				Require(parentCycleRejected, "State hierarchy accepted a parent that is one of the state's descendants.");
				Require(hierarchyGuards, "State hierarchy accepted a root/empty/Atomic/History/History-to-Parallel parent assignment.");
				Require(rejectedClean, "Rejected hierarchy edit produced Undo or dirty/Changed state.");
				kindInvariant = ProbeStateKindInvariants();
				Require(kindInvariant, "SetStateKind produced an invalid hierarchy or failed to preserve atomic Undo/Redo semantics.");
				rootSwitchInvariant = ProbeRootSwitchInvariant();
				Require(rootSwitchInvariant, "SetRootState did not atomically repair/promote the hierarchy with valid Undo/Redo.");
				StateMachineGraphNode connectionSource = surface?.FindStateNode("Idle");
				StateMachineGraphNode connectionTarget = surface?.FindStateNode(newStateId);
				if (connectionSource != null && connectionTarget != null)
				{
					surface.EmitSignal(GraphEdit.SignalName.ConnectionRequest, connectionSource.Name, 0, connectionTarget.Name, 0);
				}
				await WaitFrames(3);
				string visualTransitionId = FindTransitionId(definition, "Idle", newStateId);
				connection = !string.IsNullOrWhiteSpace(visualTransitionId) && (surface?.HasVisualConnection("Idle", newStateId) ?? false);
				Require(connection, "New transition was not rendered as a visual graph edge.");
				LineEdit lineEdit3 = surface?.FindChild("EventNameEdit", recursive: true, owned: false) as LineEdit;
				connectionDetails = surface?.ActiveDetailsKind == "Transition" && surface.ActiveDetailsStableId == visualTransitionId && lineEdit3 != null;
				if (lineEdit3 != null)
				{
					lineEdit3.Text = "ProbeEvent";
					lineEdit3.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit3.Text);
				}
				await WaitFrames(2);
				connectionDetails &= FindTransition(definition, visualTransitionId)?.EventName.ToString() == "ProbeEvent";
				Require(connectionDetails, "Dragging a connection did not immediately open and edit its transition puzzle.");
				curvedConnectionHit = ProbeCurvedConnectionHit(surface, "Idle", newStateId, visualTransitionId);
				Require(curvedConnectionHit, "Curved/reverse graph connections could not be selected at their 25/50/75-percent Bezier positions.");
				string parallelTransitionId = surface?.GraphController?.AddTransition("Idle", newStateId);
				await WaitFrames(2);
				if (!string.IsNullOrWhiteSpace(parallelTransitionId) && connectionSource != null && connectionTarget != null)
				{
					Vector2 vector = (ResolveConnectionEndpointForProbe(connectionSource, output: true) + ResolveConnectionEndpointForProbe(connectionTarget, output: false)) * 0.5f;
					localMidpoint = (vector - surface.ScrollOffset) * surface.Zoom;
					ClickConnection();
					string activeDetailsStableId = surface.ActiveDetailsStableId;
					ClickConnection();
					string activeDetailsStableId2 = surface.ActiveDetailsStableId;
					multiConnectionSelect = surface.LastConnectionSelectionGroupSize == 2 && activeDetailsStableId != activeDetailsStableId2 && new HashSet<string> { activeDetailsStableId, activeDetailsStableId2 }.SetEquals(new string[2] { visualTransitionId, parallelTransitionId });
					surface.GraphController.RemoveTransition(parallelTransitionId);
					await WaitFrames(1);
				}
				Require(multiConnectionSelect, "Multiple transitions sharing endpoints could not be selected and cycled from the visual connection.");
				surface?.NavigateToStableId(visualTransitionId);
				await WaitFrames(2);
				if (surface?.FindChild("GuardEnabled", recursive: true, owned: false) is CheckButton checkButton)
				{
					checkButton.ButtonPressed = true;
				}
				await WaitFrames(2);
				LineEdit lineEdit4 = surface?.FindChild("GuardProperty", recursive: true, owned: false) as LineEdit;
				bool guardStayedVisibleAfterEnable = lineEdit4 != null;
				if (lineEdit4 != null)
				{
					lineEdit4.Text = "health";
					lineEdit4.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit4.Text);
				}
				await WaitFrames(2);
				OptionButton optionButton = surface?.FindChild("GuardValueType", recursive: true, owned: false) as OptionButton;
				bool guardStayedVisibleAfterProperty = optionButton != null;
				if (optionButton != null)
				{
					int itemIndex3 = optionButton.GetItemIndex(1);
					optionButton.Select(itemIndex3);
					optionButton.EmitSignal(OptionButton.SignalName.ItemSelected, itemIndex3);
				}
				await WaitFrames(2);
				LineEdit lineEdit5 = surface?.FindChild("GuardExpected", recursive: true, owned: false) as LineEdit;
				bool guardStayedVisibleAfterType = lineEdit5 != null;
				if (lineEdit5 != null)
				{
					lineEdit5.Text = "10";
					lineEdit5.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit5.Text);
				}
				await WaitFrames(2);
				guardContinuous = guardStayedVisibleAfterEnable && guardStayedVisibleAfterProperty && guardStayedVisibleAfterType && surface?.FindChild("GuardExpected", recursive: true, owned: false) is LineEdit;
				Require(guardContinuous, "Transition guard editing required navigating back to the transition between field changes.");
				conditionEdit = FindTransition(definition, visualTransitionId)?.GuardDefinition is StateMachineGuardDefinition stateMachineGuardDefinition && stateMachineGuardDefinition.ComparedProperty.ToString() == "health" && stateMachineGuardDefinition.ExpectedValue.VariantType == Variant.Type.Float && Math.Abs(stateMachineGuardDefinition.ExpectedValue.AsDouble() - 10.0) < 0.001;
				Require(conditionEdit, "Condition puzzle did not directly edit the transition guard.");
				recursiveGuardPuzzle = await ProbeRecursiveGuardPuzzle(surface, definition, visualTransitionId);
				Require(recursiveGuardPuzzle, "Recursive Guard puzzle did not author A && (B || !C) through real Details and Guard controls.");
				StateMachineGuardDefinition recursiveGuard = FindTransition(definition, visualTransitionId)?.GuardDefinition as StateMachineGuardDefinition;
				nestedGuardRootSave = await ProbeNestedGuardRootPersistence(definition, visualTransitionId, "user://mod_editor_state_machine_workbench_probe.tres");
				Require(nestedGuardRootSave, "Saving a deeply nested Guard did not preserve the root StateMachineDefinition file.");
				nestedGuardRootSaveAs = await ProbeNestedGuardRootSaveAs(definition, visualTransitionId);
				Require(nestedGuardRootSaveAs, "Save As from an unsaved nested Guard did not persist the root StateMachineDefinition.");
				nestedGuardUndoRedo = await ProbeNestedGuardUndoRedo(recursiveGuard);
				Require(nestedGuardUndoRedo, "Nested Guard delete did not complete one independent Undo/Redo round trip through visible controls.");
				guardKindAtomicUndo = await ProbeGuardKindAtomicUndo(recursiveGuard);
				Require(guardKindAtomicUndo, "Changing a composite Guard kind did not remain one atomic Undo/Redo action.");
				malformedGuardUiSafe = await ProbeMalformedGuardUiSafety();
				Require(malformedGuardUiSafe, "Malformed Guard did not remain bounded in Preview, Details, and validation.");
				surface?.SetWorkbenchActive(active: true);
				SpinBox prioritySpin = surface?.FindChild("PrioritySpin", recursive: true, owned: false) as SpinBox;
				int numericDefinitionChanges = 0;
				Action onNumericDefinitionChanged = delegate
				{
					numericDefinitionChanges++;
				};
				if (surface != null)
				{
					surface.DefinitionChanged += onNumericDefinitionChanged;
				}
				if (prioritySpin != null)
				{
					prioritySpin.Value = 1.0;
					prioritySpin.Value = 2.0;
					prioritySpin.Value = 3.0;
				}
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface4 = surface;
				bool numericSingleTimerWhilePending = stateMachineGraphEditorSurface4 != null && stateMachineGraphEditorSurface4.DetailsPanel?.ActiveNumericDebounceTimerCount == 1;
				int numericImmediatePriority = FindTransition(definition, visualTransitionId)?.Priority ?? (-2147483648);
				bool numericWaitedForGesture = numericImmediatePriority == 0;
				await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
				await WaitFrames(2);
				int num2 = FindTransition(definition, visualTransitionId)?.Priority ?? (-2147483648);
				numericDebounce = numericWaitedForGesture && num2 == 3 && numericDefinitionChanges == 1;
				int num3;
				if (numericSingleTimerWhilePending)
				{
					StateMachineGraphEditorSurface stateMachineGraphEditorSurface5 = surface;
					num3 = ((stateMachineGraphEditorSurface5 != null && stateMachineGraphEditorSurface5.DetailsPanel?.ActiveNumericDebounceTimerCount == 0) ? 1 : 0);
				}
				else
				{
					num3 = 0;
				}
				debounceSingleTimer = (byte)num3 != 0;
				if (surface != null)
				{
					surface.DefinitionChanged -= onNumericDefinitionChanged;
				}
				if (!numericDebounce)
				{
					GD.Print($"[MOD_EDITOR_STATE_MACHINE_NUMERIC_DIAGNOSTIC] spin={prioritySpin != null} immediate={numericImmediatePriority} final={num2} definitionChanges={numericDefinitionChanges} editing={surface?.DetailsPanel?.IsEditingActive} workbench={surface?.IsWorkbenchActive} visible={surface?.IsVisibleInTree()}");
				}
				Require(numericDebounce, "Numeric state-machine fields rebuilt the graph during a continuous value gesture or lost the final value.");
				surface?.NavigateToStableId(newStateId);
				await WaitFrames(2);
				bool actionWorkbenchVisible = surface?.FindChild("ActionWorkbenchRow", recursive: true, owned: false) != null;
				if (surface?.FindChild("CallbackKeyEdit", recursive: true, owned: false) is LineEdit lineEdit6)
				{
					lineEdit6.Text = "probe_action";
					lineEdit6.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit6.Text);
				}
				await WaitFrames(2);
				actionEdit = FindState(definition, newStateId)?.CallbackKey.ToString() == "probe_action" && actionWorkbenchVisible;
				Require(actionEdit, "Action puzzle did not directly edit the state callback key.");
				surface?.GraphController?.Undo();
				surface?.RefreshGraph();
				bool flag = FindState(definition, newStateId)?.CallbackKey.IsEmpty ?? false;
				surface?.GraphController?.Redo();
				surface?.RefreshGraph();
				undoRedo = flag && FindState(definition, newStateId)?.CallbackKey.ToString() == "probe_action";
				Require(undoRedo, "Visual state-machine edit did not survive Undo/Redo.");
				(bool, bool, bool, bool, bool, bool, bool, bool) tuple3 = await ProbeOverviewTransitionPuzzle(surface, definition, "user://mod_editor_state_machine_workbench_probe.tres", inspector, sentinel);
				transitionWorkbench = tuple3.Item1;
				transitionChips = tuple3.Item2;
				multiTransitionChips = tuple3.Item3;
				transitionChipDirectEdit = tuple3.Item4;
				transitionChipViewportFollow = tuple3.Item5;
				transitionChipCollapse = tuple3.Item6;
				transitionChipSaveReload = tuple3.Item7;
				transitionChipInspectorUntouched = tuple3.Rest.Item1;
				Require(transitionWorkbench, "Definition overview transition puzzle did not create, navigate, delete, undo/redo, and reload parallel event self-loops.");
				Require(transitionChips, "Authored transitions did not render as directly clickable visual chips.");
				Require(multiTransitionChips, "Transitions sharing the same source and target did not keep distinct chip identity, text, tooltip, or position.");
				Require(transitionChipDirectEdit, "Pressing and editing a transition chip did not target and refresh the exact transition.");
				Require(transitionChipViewportFollow, "Transition chips did not follow live node movement, zoom, and scroll.");
				Require(transitionChipCollapse, "Hierarchy collapse did not hide and restore transition chips incrementally.");
				Require(transitionChipSaveReload, "Transition chips or their edited fields did not survive cache-ignored Save/Reload.");
				Require(transitionChipInspectorUntouched, "Transition chip interaction opened or replaced the raw Inspector.");
				StateMachineDefinition definition2 = ((ResourceSaver.Save(definition, "user://mod_editor_state_machine_workbench_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok) ? ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_workbench_probe.tres", "", ResourceLoader.CacheMode.Ignore) : null);
				saveReload = FindState(definition2, newStateId)?.DisplayName.ToString() == "ProbeRenamed" && FindState(definition2, newStateId)?.CallbackKey.ToString() == "probe_action" && IsRecursiveGuardPuzzle(FindTransition(definition2, visualTransitionId)?.GuardDefinition as StateMachineGuardDefinition);
				Require(saveReload, "State-machine visual edits did not survive Save/Reload.");
				if (!saveReload)
				{
					StateMachineGuardDefinition stateMachineGuardDefinition2 = FindTransition(definition2, visualTransitionId)?.GuardDefinition as StateMachineGuardDefinition;
					StateMachineGuardDefinition guardChild = GetGuardChild(stateMachineGuardDefinition2, 0);
					string value = GetGuardChild(stateMachineGuardDefinition2, 1)?.Kind.ToString() ?? "<missing>";
					GD.Print($"[MOD_EDITOR_RECURSIVE_GUARD_RELOAD] root={stateMachineGuardDefinition2?.Kind} children={stateMachineGuardDefinition2?.Children?.Count} a={guardChild?.ComparedProperty} any={value}");
				}
				StateMachineCompiler.DisposeTemporaryDefinition(definition2, disposeGuardTrees: true);
				inspectorUntouched = inspector == null || inspector.CurrentObject == sentinel;
				Require(inspectorUntouched, "State-machine canvas replaced the global raw Inspector selection.");
				diagnostics = editor?.GraphSurface?.FindChild("*Diagnostics*", recursive: true, owned: false) != null;
				Require(diagnostics, "Graph diagnostics panel is missing from the live surface.");
				simulator = surface?.SimulationPanel;
				sceneBeforeSimulation = GetTree().CurrentScene;
				simulation = simulator?.StartSimulation() ?? false;
				activeHighlight = simulation && SnapshotHasActive(simulator.CurrentSnapshot, "Idle") && (FindStateNode(surface, "Idle")?.IsSimulationActive ?? false);
				Require(simulation && activeHighlight, "State-machine sandbox did not highlight the initial state.");
				bool guardBlocked = (simulator?.SendSimulationEvent("ToAttack") ?? false) && SnapshotHasActive(simulator?.CurrentSnapshot, "Idle") && simulator.PendingDelayRemaining <= 0.0;
				expressionKey = simulator?.FindChild("ExpressionPropertyKey", recursive: true, owned: false) as LineEdit;
				expressionNumberType = simulator?.FindChild("ExpressionPropertyTypeNumberButton", recursive: true, owned: false) as Button;
				Button button = simulator?.FindChild("ExpressionPropertyTypeBooleanButton", recursive: true, owned: false) as Button;
				expressionValue = simulator?.FindChild("ExpressionPropertyValue", recursive: true, owned: false) as LineEdit;
				CheckButton expressionBoolean = simulator?.FindChild("ExpressionPropertyBoolean", recursive: true, owned: false) as CheckButton;
				setExpression = simulator?.FindChild("SetExpressionPropertyButton", recursive: true, owned: false) as Button;
				if (expressionKey != null && expressionNumberType != null && button != null && expressionValue != null && expressionBoolean != null && setExpression != null)
				{
					expressionKey.Text = "health";
					expressionNumberType.EmitSignal(BaseButton.SignalName.Pressed);
					expressionValue.Text = "10";
					setExpression.EmitSignal(BaseButton.SignalName.Pressed);
					expressionKey.Text = "stunned";
					button.EmitSignal(BaseButton.SignalName.Pressed);
					expressionBoolean.ButtonPressed = true;
					setExpression.EmitSignal(BaseButton.SignalName.Pressed);
					expressionKey.Text = "has_target";
					expressionBoolean.ButtonPressed = false;
					setExpression.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(2);
				bool blockedWithStun = (simulator?.SendSimulationEvent("ToAttack") ?? false) && SnapshotHasActive(simulator?.CurrentSnapshot, "Idle");
				if (expressionKey != null && expressionBoolean != null && setExpression != null)
				{
					expressionKey.Text = "has_target";
					expressionBoolean.ButtonPressed = true;
					setExpression.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(2);
				Variant health = default(Variant);
				Variant stunned = default(Variant);
				Variant hasTarget = default(Variant);
				expressionProperties = simulator != null && simulator.ExpressionPropertyCount == 3 && (simulator.CurrentSnapshot?.ExpressionProperties.TryGetValue(new StringName("health"), out health) ?? false) && health.VariantType == Variant.Type.Float && Math.Abs(health.AsDouble() - 10.0) < 0.001 && (simulator.CurrentSnapshot?.ExpressionProperties.TryGetValue(new StringName("stunned"), out stunned) ?? false) && stunned.VariantType == Variant.Type.Bool && stunned.AsBool() && (simulator.CurrentSnapshot?.ExpressionProperties.TryGetValue(new StringName("has_target"), out hasTarget) ?? false) && hasTarget.VariantType == Variant.Type.Bool && hasTarget.AsBool();
				Require(expressionProperties, "Visual expression key/type/value controls did not update all recursive Guard properties.");
				guardedTransition = guardBlocked && blockedWithStun && (simulator?.SendSimulationEvent("ToAttack") ?? false);
				Require(guardedTransition, "Recursive AND/OR/NOT Guard did not block then unlock through visual expression controls.");
				delayed = guardedTransition && simulator.PendingDelayRemaining > 0.0;
				Require(delayed, "Event-driven delayed transition was not scheduled visually.");
				simulator?.AdvanceSimulation(0.25);
				automatic = simulator != null && SnapshotHasActive(simulator.CurrentSnapshot, "Attack") && simulator.PendingDelayRemaining > 0.0;
				Require(automatic, "Automatic transition countdown was not scheduled after the event transition.");
				simulator?.AdvanceSimulation(0.2);
				historyBack = simulator != null && SnapshotHasActive(simulator.CurrentSnapshot, "Recover") && simulator.TransitionHistoryCount >= 2 && simulator.StepBack() && SnapshotHasActive(simulator.CurrentSnapshot, "Attack");
				Require(historyBack, "Transition history could not restore the previous active state.");
				reset = simulator != null && simulator.ResetSimulation() && SnapshotHasActive(simulator.CurrentSnapshot, "Idle");
				Require(reset, "State-machine sandbox reset did not return to the initial state.");
				simulator?.SetProcess(enable: false);
				int num4 = simulator?.AutoSnapshotPublishCount ?? 0;
				for (int num5 = 0; num5 < 50; num5++)
				{
					simulator?.AdvanceAutoSimulation(0.01);
				}
				int num6 = (simulator?.AutoSnapshotPublishCount ?? 0) - num4;
				idleSimulationCoalesced = num6 >= 4 && num6 <= 6;
				Require(idleSimulationCoalesced, $"Idle simulation published {num6} snapshots for 50 process steps instead of a coalesced cadence.");
				Button button2 = surface?.FindChild("SimulationToggle", recursive: true, owned: false) as Button;
				Button visualStart = simulator?.FindChild("SimulationStartButton", recursive: true, owned: false) as Button;
				visualResume = simulator?.FindChild("SimulationResumeButton", recursive: true, owned: false) as Button;
				Button visualPause = simulator?.FindChild("SimulationPauseButton", recursive: true, owned: false) as Button;
				visualStep = simulator?.FindChild("SimulationStepButton", recursive: true, owned: false) as Button;
				visualReset = simulator?.FindChild("SimulationResetButton", recursive: true, owned: false) as Button;
				visualEvent = simulator?.FindChild("SimulationEventPicker", recursive: true, owned: false) as OptionButton;
				visualSend = simulator?.FindChild("SimulationSendEventButton", recursive: true, owned: false) as Button;
				runtimeTrace = simulator?.FindChild("RuntimeTraceList", recursive: true, owned: false) as ItemList;
				visualDebugControls = GodotObject.IsInstanceValid(button2) && GodotObject.IsInstanceValid(visualStart?.Icon) && GodotObject.IsInstanceValid(visualResume?.Icon) && GodotObject.IsInstanceValid(visualPause?.Icon) && GodotObject.IsInstanceValid(visualStep?.Icon) && GodotObject.IsInstanceValid(visualReset?.Icon) && GodotObject.IsInstanceValid(visualSend?.Icon) && GodotObject.IsInstanceValid(visualEvent) && GodotObject.IsInstanceValid(runtimeTrace);
				Require(visualDebugControls, "State-machine debugger lacks named icon controls for start, pause, resume, step, reset, event, or trace.");
				button2?.SetPressedNoSignal(pressed: true);
				button2?.EmitSignal(BaseButton.SignalName.Toggled, true);
				if (simulator?.IsRunning ?? false)
				{
					visualStart?.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(2);
				visualStart?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				compiledRuntimeTrace = (simulator?.IsRunning ?? false) && !simulator.UsesBorrowedRuntime && simulator.RuntimeSourceName == "隔离编译沙盒" && simulator.RuntimeTraceCount == 1 && simulator.LastRuntimeTraceKind == "start" && SnapshotHasActive(simulator.CurrentSnapshot, "Idle") && simulator.Visible;
				Require(compiledRuntimeTrace, "Standalone state-machine resource did not start its compiled runtime through the visual control.");
				visualResume?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(4);
				visualPause?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				long pausedRevision = simulator?.CurrentSnapshotRevision ?? (-1);
				int pausedTraceCount = simulator?.RuntimeTraceCount ?? (-1);
				await WaitFrames(5);
				pauseStable = (simulator?.IsPaused ?? false) && !simulator.IsProcessing() && simulator.CurrentSnapshotRevision == pausedRevision && simulator.RuntimeTraceCount == pausedTraceCount && SnapshotHasActive(simulator.CurrentSnapshot, "Idle");
				Require(pauseStable, "Visual pause did not retain the compiled controller and freeze automatic time.");
				visualStep?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				singleStepControl = (simulator?.IsPaused ?? false) && simulator.RuntimeTraceCount == pausedTraceCount + 1 && simulator.LastRuntimeTraceKind == "step" && SnapshotHasActive(simulator.CurrentSnapshot, "Idle");
				Require(singleStepControl, "Visual single-step did not advance exactly one paused trace step.");
				for (int num7 = 0; num7 < 140; num7++)
				{
					visualStep?.EmitSignal(BaseButton.SignalName.Pressed);
				}
				await WaitFrames(2);
				if (simulator != null && simulator.RuntimeTraceCount == 128)
				{
					ItemList itemList = runtimeTrace;
					if (itemList != null && itemList.ItemCount == 128)
					{
						num8 = (simulator.IsPaused ? 1 : 0);
						goto IL_3d2b;
					}
				}
				num8 = 0;
				goto IL_3d2b;
			}
			goto end_IL_042d;
			IL_40c5:
			int num9;
			bool attackHighlight = (byte)num9 != 0;
			StateMachineTransitionChip idleToAttackChip;
			StateMachineTransitionChip attackToRecoverChip;
			bool eventCompletedAndAutomaticPending = idleToAttackChip != null && !idleToAttackChip.IsPending && (idleToAttackChip?.IsRecentlyCompleted ?? false) && (attackToRecoverChip?.IsPending ?? false) && !attackToRecoverChip.IsRecentlyCompleted;
			for (int num10 = 0; num10 < 10; num10++)
			{
				visualStep?.EmitSignal(BaseButton.SignalName.Pressed);
			}
			await WaitFrames(3);
			int num11;
			if (SnapshotHasActive(simulator?.CurrentSnapshot, "Recover"))
			{
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface6 = surface;
				if (stateMachineGraphEditorSurface6 != null && stateMachineGraphEditorSurface6.FindStateNode("Recover")?.IsSimulationActive == true)
				{
					StateMachineGraphEditorSurface stateMachineGraphEditorSurface7 = surface;
					num11 = ((stateMachineGraphEditorSurface7 != null && stateMachineGraphEditorSurface7.FindStateNode("Attack")?.IsSimulationActive == false) ? 1 : 0);
					goto IL_4261;
				}
			}
			num11 = 0;
			goto IL_4261;
			IL_3d2b:
			traceBounded = (byte)num8 != 0;
			Require(traceBounded, "Runtime timeline exceeded its bounded 128-step UI/memory budget.");
			if (expressionKey != null && expressionNumberType != null && expressionValue != null && setExpression != null)
			{
				expressionKey.Text = "health";
				expressionNumberType.EmitSignal(BaseButton.SignalName.Pressed);
				expressionValue.Text = "12";
				setExpression.EmitSignal(BaseButton.SignalName.Pressed);
			}
			if (visualEvent != null)
			{
				for (int num12 = 0; num12 < visualEvent.ItemCount; num12++)
				{
					if (visualEvent.GetItemText(num12) == "ToAttack")
					{
						visualEvent.Select(num12);
						break;
					}
				}
			}
			visualSend?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			idleToAttackChip = surface?.FindTransitionChip("IdleToAttack");
			attackToRecoverChip = surface?.FindTransitionChip("AttackToRecover");
			bool pendingTransitionChip = simulator != null && simulator.PendingDelayRemaining > 0.0 && (idleToAttackChip?.IsPending ?? false) && !idleToAttackChip.IsRecentlyCompleted && idleToAttackChip.Text.Contains("s", StringComparison.Ordinal);
			int num13;
			if (simulator != null && simulator.PendingDelayRemaining > 0.0)
			{
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface8 = surface;
				num13 = ((stateMachineGraphEditorSurface8 != null && stateMachineGraphEditorSurface8.FindStateNode("Idle")?.IsSimulationPending == true) ? 1 : 0);
			}
			else
			{
				num13 = 0;
			}
			bool pendingHighlight = (byte)((uint)num13 & (pendingTransitionChip ? 1u : 0u)) != 0;
			for (int num14 = 0; num14 < 13; num14++)
			{
				visualStep?.EmitSignal(BaseButton.SignalName.Pressed);
			}
			await WaitFrames(3);
			if (SnapshotHasActive(simulator?.CurrentSnapshot, "Attack"))
			{
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface9 = surface;
				if (stateMachineGraphEditorSurface9 != null && stateMachineGraphEditorSurface9.FindStateNode("Attack")?.IsSimulationActive == true)
				{
					StateMachineGraphEditorSurface stateMachineGraphEditorSurface10 = surface;
					num9 = ((stateMachineGraphEditorSurface10 != null && stateMachineGraphEditorSurface10.FindStateNode("Idle")?.IsSimulationActive == false) ? 1 : 0);
					goto IL_40c5;
				}
			}
			num9 = 0;
			goto IL_40c5;
			IL_4261:
			bool recoverHighlight = (byte)num11 != 0;
			int num15;
			if (idleToAttackChip != null && !idleToAttackChip.IsRecentlyCompleted)
			{
				if (attackToRecoverChip != null && !attackToRecoverChip.IsPending)
				{
					num15 = ((attackToRecoverChip?.IsRecentlyCompleted ?? false) ? 1 : 0);
					goto IL_42aa;
				}
			}
			num15 = 0;
			goto IL_42aa;
			IL_4a56:
			int num16;
			sideEffectFree = (byte)num16 != 0;
			Require(sideEffectFree, "State-machine sandbox created gameplay owners or replaced the probe scene.");
			(groupedMoveUndo, surfaceSignals) = await ProbeGroupedNodeMove(surface, definition, newStateId);
			Require(groupedMoveUndo, "Moving two selected graph nodes did not create one atomic Undo/Redo action.");
			Require(surfaceSignals, "ModEditor shared GraphEdit move signals did not traverse the surface event chain.");
			(contextMenu, clipboardSignals, pointerPlacement, shortcutUndoRedo, clipboardParent) = await ProbeCanvasEditingSignals(surface, definition);
			Require(contextMenu, "GraphEdit PopupRequest did not show the canvas context menu at the requested point.");
			Require(clipboardSignals, "GraphEdit copy/cut/duplicate/paste/delete signals did not complete their visual edit chain.");
			Require(pointerPlacement, "Paste or duplicate ignored the latest mouse graph position.");
			Require(shortcutUndoRedo, "Canvas Ctrl+Z/Ctrl+Y changed more or less than one edit action.");
			Require(clipboardParent, "Copying one child state broke its external parent or produced unresolved hierarchy references.");
			Button definitionOverview = surface?.FindChild("DefinitionOverviewButton", recursive: true, owned: false) as Button;
			definitionOverview?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(1);
			bool flag2 = definitionOverview != null;
			if (flag2)
			{
				flag2 = await ProbeModBaseDefinitionPicker(editor, surface, definition);
			}
			modResourcePicker = flag2;
			Require(modResourcePicker, "BaseDefinition picker did not index and select the current Mod project's state machine.");
			(bool, bool) tuple6 = await ProbeViewportDebounce(surface, "user://mod_editor_state_machine_workbench_probe.tres");
			viewportDebounce = tuple6.Item1;
			bool item = tuple6.Item2;
			debounceSingleTimer = debounceSingleTimer && item;
			Require(viewportDebounce, "Scroll/zoom did not debounce into the state-machine layout sidecar.");
			Require(debounceSingleTimer, "Continuous state-machine input created overlapping debounce timers.");
			Vector2 switchScroll = new Vector2(286f, 174f);
			EmitViewportGesture(surface, switchScroll, 1.35f);
			StateMachineDefinition resource = CreateHierarchyDefinition();
			bool switched = XWResourceEditorRegistry.TryOpen(resource, "user://mod_editor_state_machine_switch_probe.tres");
			await WaitFrames(3);
			StateMachineLayout layout = LoadLayoutSidecar("user://mod_editor_state_machine_workbench_probe.tres");
			switchViewportFlush = switched && ViewportMatches(layout, switchScroll, 1.35f);
			Require(switchViewportFlush, "Switching resources discarded a pending graph viewport update.");
			bool reopenedOriginal = XWResourceEditorRegistry.TryOpen(definition, "user://mod_editor_state_machine_workbench_probe.tres");
			await WaitFrames(3);
			surface = editor?.GraphSurface;
			viewportReload = reopenedOriginal && surface != null && surface.ScrollOffset.IsEqualApprox(switchScroll) && Math.Abs(surface.Zoom - 1.35f) < 0.001f;
			Require(viewportReload, "Reloading the state-machine sidecar did not restore scroll and zoom.");
			Vector2 hiddenScroll = new Vector2(344f, 228f);
			EmitViewportGesture(surface, hiddenScroll, 0.85f);
			surface?.SetWorkbenchActive(active: false);
			await WaitFrames(1);
			StateMachineLayout layout2 = LoadLayoutSidecar("user://mod_editor_state_machine_workbench_probe.tres");
			hiddenViewportFlush = ViewportMatches(layout2, hiddenScroll, 0.85f);
			Require(hiddenViewportFlush, "Hiding the workbench discarded a pending graph viewport update.");
			hiddenStopped = surface?.IsHiddenWorkQuiescent ?? false;
			Require(hiddenStopped, "Hidden state-machine workbench kept simulation or processing alive.");
			inheritedOverrideWorkbench = await ProbeInheritedOverrideWorkbench(editor);
			Require(inheritedOverrideWorkbench, "Inherited state/transition override controls did not create, edit, undo/redo, restore inheritance, and reload.");
			derivedTransitionOwner = await ProbeDerivedTransitionOwnerBinding(editor);
			Require(derivedTransitionOwner, "A detached local-derived transition owner could not rebind to the active definition by StableId.");
			derivedDefinitionWorkbench = await ProbeDerivedDefinitionWorkbench(editor, inspector, sentinel);
			Require(derivedDefinitionWorkbench, "A C#-derived StateMachineDefinition did not retain its dedicated workbench, direct extension field, Undo/Redo, Save/Reload, and Inspector isolation.");
			derivedElementWorkbench = await ProbeDerivedElementWorkbench(editor, inspector, sentinel);
			Require(derivedElementWorkbench, "C#-derived states/transitions lost direct editing, type identity, inheritance, clipboard, persistence, or runtime properties.");
			flag2 = authorTypesRegistered;
			if (flag2)
			{
				flag2 = await ProbeDerivedTypeCreation(editor, inspector, sentinel);
			}
			derivedTypeCreation = flag2;
			Require(derivedTypeCreation, "Registered C# state/transition types could not be created visually with exact defaults, Undo/Redo, persistence, and Inspector isolation.");
			compositionFailureGuard = await ProbeCompositionFailureGuard(editor, inspector, sentinel);
			Require(compositionFailureGuard, "Inheritance-cycle preflight or composition-failure edit gates allowed a partial graph mutation/history entry.");
			(bool, bool, bool, bool, bool, bool, bool, bool, bool) tuple7 = await ProbeHierarchyCollapseWorkbench(editor, inspector, sentinel);
			collapseControl = tuple7.Item1;
			collapseDescendants = tuple7.Item2;
			collapseConnections = tuple7.Item3;
			collapseSelectionClean = tuple7.Item4;
			collapseUndoRedo = tuple7.Item5;
			collapseSaveReload = tuple7.Item6;
			collapseExpandRestore = tuple7.Item7;
			collapseInspectorUntouched = tuple7.Rest.Item1;
			initialButtonGuard = tuple7.Rest.Item2;
			Require(collapseControl, "Compound/parallel hierarchy tiles did not expose the icon collapse control and hierarchy badges.");
			Require(collapseDescendants, "Collapsing a compound state did not recursively hide only its descendants.");
			Require(collapseConnections, "Collapsed descendants kept visible transition curves or hid a still-visible parent transition.");
			Require(collapseSelectionClean, "Collapsing a hierarchy left hidden nodes or transitions selected in the workbench.");
			Require(collapseUndoRedo, "Hierarchy collapse did not complete one incremental top-toolbar Undo/Redo round trip.");
			Require(collapseSaveReload, "Hierarchy collapse did not survive sidecar save, resource switch, and cache-ignored reload.");
			Require(collapseExpandRestore, "Expanding a reloaded hierarchy did not restore every descendant and transition without stale selection.");
			Require(collapseInspectorUntouched, "Hierarchy collapse opened or replaced the raw global Inspector.");
			Require(initialButtonGuard, "Parallel-state children still exposed the invalid compound initial-child action.");
			(largeGraphIncremental, largeCollapseIncremental, largeTransitionChipIncremental, largeTransitionChipMoveScope) = await ProbeLargeGraphIncremental(editor);
			Require(largeGraphIncremental, "Large state-machine graph rebuilt stable nodes or emitted duplicate mutations.");
			Require(largeCollapseIncremental, "Collapsing and expanding a 500-node graph rebuilt nodes or failed to remove and restore 800 curves incrementally.");
			Require(largeTransitionChipIncremental, "Repeated refresh or hierarchy collapse rebuilt, lost, or duplicated the 800 transition chips.");
			Require(largeTransitionChipMoveScope, "Moving one large-graph node relaid out transitions outside its adjacent groups.");
			signalLifecycleClean = await ProbeStateMachineSignalLifecycle();
			Require(signalLifecycleClean, "State-machine surface kept debounce or signal callbacks alive after teardown.");
			goto end_IL_042d;
			IL_42aa:
			bool automaticCompletedChip = (byte)num15 != 0;
			for (int num17 = 0; num17 < 73; num17++)
			{
				visualStep?.EmitSignal(BaseButton.SignalName.Pressed);
			}
			await WaitFrames(3);
			bool completedHighlightExpired = attackToRecoverChip != null && !attackToRecoverChip.IsRecentlyCompleted && string.IsNullOrWhiteSpace(simulator?.LastCompletedTransitionStableId);
			bool flag3 = runtimeTrace != null && runtimeTrace.ItemCount == simulator?.RuntimeTraceCount && Enumerable.Range(0, runtimeTrace.ItemCount).Any((int index) => runtimeTrace.GetItemText(index).Contains("Idle", StringComparison.Ordinal) && runtimeTrace.GetItemText(index).Contains("Attack", StringComparison.Ordinal)) && Enumerable.Range(0, runtimeTrace.ItemCount).Any((int index) => runtimeTrace.GetItemText(index).Contains("Attack", StringComparison.Ordinal) && runtimeTrace.GetItemText(index).Contains("Recover", StringComparison.Ordinal));
			int num18;
			if (pendingHighlight && attackHighlight && recoverHighlight)
			{
				num18 = ((simulator != null && simulator.TransitionHistoryCount >= 2) ? 1 : 0);
			}
			else
			{
				num18 = 0;
			}
			highlightTrace = (byte)((uint)num18 & (flag3 ? 1u : 0u)) != 0;
			Require(highlightTrace, "Visual stepping did not show pending, Attack, Recover, and exact transition timeline states.");
			runtimeTrace?.EmitSignal(ItemList.SignalName.ItemActivated, 0L);
			await WaitFrames(3);
			int num19;
			if ((simulator?.IsPaused ?? false) && simulator.LastRuntimeTraceKind == "rewind" && SnapshotHasActive(simulator.CurrentSnapshot, "Idle"))
			{
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface11 = surface;
				num19 = ((stateMachineGraphEditorSurface11 != null && stateMachineGraphEditorSurface11.FindStateNode("Idle")?.IsSimulationActive == true) ? 1 : 0);
			}
			else
			{
				num19 = 0;
			}
			traceRestore = (byte)num19 != 0;
			Require(traceRestore, "Double-clicking the visual runtime timeline did not restore its exact snapshot.");
			visualReset?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			resetControl = (simulator?.IsRunning ?? false) && SnapshotHasActive(simulator.CurrentSnapshot, "Idle") && simulator.RuntimeTraceCount == 1 && simulator.LastRuntimeTraceKind == "start";
			Require(resetControl, "Visual reset did not rebuild the compiled debugger at its initial state.");
			int num20;
			if (idleToAttackChip != null && !idleToAttackChip.IsPending)
			{
				if (idleToAttackChip != null && !idleToAttackChip.IsRecentlyCompleted)
				{
					if (attackToRecoverChip != null && !attackToRecoverChip.IsPending)
					{
						num20 = ((attackToRecoverChip != null && !attackToRecoverChip.IsRecentlyCompleted) ? 1 : 0);
						goto IL_46de;
					}
				}
			}
			num20 = 0;
			goto IL_46de;
			IL_46de:
			bool flag4 = (byte)num20 != 0;
			transitionChipRuntime = pendingTransitionChip && eventCompletedAndAutomaticPending && automaticCompletedChip && completedHighlightExpired && flag4;
			Require(transitionChipRuntime, "Exact pending/completed transition identity was not projected onto its visual chip or cleared by reset.");
			visualResume?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			editor.WorkbenchTabs.CurrentTab = 1;
			await WaitFrames(3);
			int num21;
			if (simulator != null && !simulator.IsRunning && !simulator.RuntimeAttached && simulator.IsQuiescent)
			{
				StateMachineGraphEditorSurface stateMachineGraphEditorSurface12 = surface;
				if (stateMachineGraphEditorSurface12 != null && stateMachineGraphEditorSurface12.FindStateNode("Idle")?.IsSimulationActive == false)
				{
					StateMachineGraphEditorSurface stateMachineGraphEditorSurface13 = surface;
					num21 = ((stateMachineGraphEditorSurface13 != null && stateMachineGraphEditorSurface13.FindStateNode("Idle")?.IsSimulationPending == false) ? 1 : 0);
					goto IL_48de;
				}
			}
			num21 = 0;
			goto IL_48de;
			IL_48de:
			hiddenRuntimeStopped = (byte)num21 != 0;
			Require(hiddenRuntimeStopped, "Switching the real ModEditor workbench tab left debugger processing, callbacks, or highlights alive.");
			editor.WorkbenchTabs.CurrentTab = 0;
			await WaitFrames(3);
			PanelContainer panelContainer = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
			VBoxContainer vBoxContainer = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
			inspectorAfterTrace = (inspector == null || inspector.CurrentObject == sentinel) && (panelContainer == null || !panelContainer.Visible) && (vBoxContainer == null || vBoxContainer.GetChildCount() == 0);
			Require(inspectorAfterTrace, "State-machine runtime debugging changed the global or embedded raw Inspector.");
			if (sceneBeforeSimulation == GetTree().CurrentScene)
			{
				if (simulator != null && simulator.FindChildren("*", "TowerDefenseCharacter", recursive: true, owned: false).Count == 0)
				{
					num16 = ((simulator != null && simulator.FindChildren("*", "ComponentManager", recursive: true, owned: false).Count == 0) ? 1 : 0);
					goto IL_4a56;
				}
			}
			num16 = 0;
			goto IL_4a56;
			end_IL_042d:
			void ClickConnection()
			{
				surface.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					Pressed = true,
					Position = localMidpoint
				});
			}
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		GD.Print($"[MOD_EDITOR_STATE_MACHINE_PROBE] autoloadEntry={autoloadEntry} window={window} dedicatedGraphInput={dedicatedGraphInput} resourcePaletteForeground={resourcePaletteForeground} modEmptySurface={modEmptySurface} route={route} graph={graph} sharedSurface={sharedSurface} responsive820={responsive820} nativeEmpty={nativeEmpty} emptyInitialization={emptyInitialization} nodeInputContract={nodeInputContract} narrow420={narrow420} toolbarInteraction={toolbarInteraction} narrowDirectEdit={narrowDirectEdit} narrowKinds={narrowKinds} hierarchySafeAdd={hierarchySafeAdd} nestedParentAdd={nestedParentAdd} createSelectRename={createSelectRename} detailsSticky={detailsSticky} guardContinuous={guardContinuous} recursiveGuardPuzzle={recursiveGuardPuzzle} nestedGuardRootSave={nestedGuardRootSave} nestedGuardRootSaveAs={nestedGuardRootSaveAs} nestedGuardUndoRedo={nestedGuardUndoRedo} guardKindAtomicUndo={guardKindAtomicUndo} malformedGuardUiSafe={malformedGuardUiSafe} singleUndo={singleUndo} toolbarUndoRouting={toolbarUndoRouting} numericDebounce={numericDebounce} debounceSingleTimer={debounceSingleTimer} viewportDebounce={viewportDebounce} viewportReload={viewportReload} groupedMoveUndo={groupedMoveUndo} hiddenViewportFlush={hiddenViewportFlush} switchViewportFlush={switchViewportFlush} surfaceSignals={surfaceSignals} contextMenu={contextMenu} clipboardSignals={clipboardSignals} clipboardParent={clipboardParent} pointerPlacement={pointerPlacement} shortcutUndoRedo={shortcutUndoRedo} connection={connection} connectionDetails={connectionDetails} transitionWorkbench={transitionWorkbench} transitionChips={transitionChips} transitionChipRuntime={transitionChipRuntime} curvedConnectionHit={curvedConnectionHit} multiConnectionSelect={multiConnectionSelect} transitionLabelDistinct={multiTransitionChips} transitionLabelClick={transitionChipDirectEdit} transitionLabelFollow={transitionChipViewportFollow} transitionLabelCollapse={transitionChipCollapse} transitionLabelReload={transitionChipSaveReload} transitionLabelInspectorUntouched={transitionChipInspectorUntouched} multiTransitionChips={multiTransitionChips} transitionChipDirectEdit={transitionChipDirectEdit} transitionChipViewportFollow={transitionChipViewportFollow} transitionChipCollapse={transitionChipCollapse} transitionChipSaveReload={transitionChipSaveReload} transitionChipInspectorUntouched={transitionChipInspectorUntouched} conditionEdit={conditionEdit} actionEdit={actionEdit} parentCycleRejected={parentCycleRejected} hierarchyGuards={hierarchyGuards} rejectedClean={rejectedClean} kindInvariant={kindInvariant} rootSwitchInvariant={rootSwitchInvariant} undoRedo={undoRedo} saveReload={saveReload} modResourcePicker={modResourcePicker} derivedTransitionOwner={derivedTransitionOwner} inheritedOverrideWorkbench={inheritedOverrideWorkbench} derivedDefinitionWorkbench={derivedDefinitionWorkbench} derivedElementWorkbench={derivedElementWorkbench} derivedTypeCreation={derivedTypeCreation} compositionFailureGuard={compositionFailureGuard} inspectorUntouched={inspectorUntouched} diagnostics={diagnostics} simulation={simulation} expressionProperties={expressionProperties} guardedTransition={guardedTransition} activeHighlight={activeHighlight} transitionRuntimeHighlight={transitionChipRuntime} delayed={delayed} automatic={automatic} historyBack={historyBack} reset={reset} idleSimulationCoalesced={idleSimulationCoalesced} compiledRuntimeTrace={compiledRuntimeTrace} visualDebugControls={visualDebugControls} pauseStable={pauseStable} singleStepControl={singleStepControl} traceBounded={traceBounded} highlightTrace={highlightTrace} traceRestore={traceRestore} resetControl={resetControl} hiddenRuntimeStopped={hiddenRuntimeStopped} inspectorAfterTrace={inspectorAfterTrace} collapseControl={collapseControl} collapseDescendants={collapseDescendants} collapseConnections={collapseConnections} collapseSelectionClean={collapseSelectionClean} collapseUndoRedo={collapseUndoRedo} collapseSaveReload={collapseSaveReload} collapseExpandRestore={collapseExpandRestore} collapseInspectorUntouched={collapseInspectorUntouched} initialButtonGuard={initialButtonGuard} sideEffectFree={sideEffectFree} hiddenStopped={hiddenStopped} largeGraphIncremental={largeGraphIncremental} largeTransitionLabelIncremental={largeTransitionChipIncremental} largeCollapseIncremental={largeCollapseIncremental} largeTransitionChipIncremental={largeTransitionChipIncremental} largeTransitionChipMoveScope={largeTransitionChipMoveScope} signalLifecycleClean={signalLifecycleClean} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_STATE_MACHINE_PROBE_FAILURE] " + failure);
		}
		RemoveProbeResource("user://mod_editor_state_machine_workbench_probe.tres");
		RemoveProbeResource("user://mod_editor_state_machine_nested_save_as_probe.tres");
		RemoveProbeResource("user://mod_editor_external_guard_source.tres");
		RemoveProbeResource("user://mod_editor_external_guard_save_as.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_workbench_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_switch_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_switch_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_inherited_override_checkpoint.tres");
		RemoveProbeResource("user://mod_editor_state_machine_inherited_override_restored.tres");
		RemoveProbeResource("user://mod_editor_state_machine_derived_definition_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_definition_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_derived_element_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_element_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_derived_type_creation_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_type_creation_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_composition_failure_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_composition_failure_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_collapse_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_collapse_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_collapse_switch_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_collapse_switch_probe.tres"));
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}

	private async Task<(bool workbench, bool chips, bool multiple, bool directEdit, bool viewportFollow, bool collapse, bool saveReload, bool inspectorUntouched)> ProbeOverviewTransitionPuzzle(StateMachineGraphEditorSurface surface, StateMachineDefinition definition, string savePath, XWInspector inspector, GodotObject inspectorSentinel)
	{
		if (surface == null || definition?.Transitions == null || string.IsNullOrWhiteSpace(savePath))
		{
			return default((bool, bool, bool, bool, bool, bool, bool, bool));
		}
		HashSet<string> beforeFirst = CaptureTransitionIds(definition);
		bool firstPressed = await PressTransitionCreatePuzzle(surface, "Idle", "Idle", "OverviewSelfLoopPrimary", 0.15, 31);
		string firstStableId = FindAddedTransitionId(definition, beforeFirst);
		StateMachineTransitionDefinition transition = FindTransition(definition, firstStableId);
		bool firstCreated = firstPressed && MatchesEventTransition(transition, "Idle", "Idle", "OverviewSelfLoopPrimary", 0.15, 31);
		HashSet<string> beforeSecond = CaptureTransitionIds(definition);
		bool secondPressed = await PressTransitionCreatePuzzle(surface, "Idle", "Idle", "OverviewSelfLoopSecondary", 0.35, 47);
		string secondStableId = FindAddedTransitionId(definition, beforeSecond);
		StateMachineTransitionDefinition transition2 = FindTransition(definition, secondStableId);
		bool secondCreated = secondPressed && !string.IsNullOrWhiteSpace(secondStableId) && secondStableId != firstStableId && MatchesEventTransition(transition2, "Idle", "Idle", "OverviewSelfLoopSecondary", 0.35, 47);
		bool bothPresent = firstCreated && secondCreated && definition.Transitions.Count((StateMachineTransitionDefinition stateMachineTransitionDefinition2) => stateMachineTransitionDefinition2?.SourceStateId == "Idle" && stateMachineTransitionDefinition2.TargetStateId == "Idle" && stateMachineTransitionDefinition2.TriggerKind == StateMachineTriggerKind.Event && (stateMachineTransitionDefinition2.EventName.ToString() == "OverviewSelfLoopPrimary" || stateMachineTransitionDefinition2.EventName.ToString() == "OverviewSelfLoopSecondary")) == 2;
		await WaitFrames(2);
		StateMachineTransitionChip firstChip = surface.FindTransitionChip(firstStableId);
		StateMachineTransitionChip stateMachineTransitionChip = surface.FindTransitionChip(secondStableId);
		int authoredTransitionCount = surface.GraphController?.ViewModel?.Transitions.Count ?? (-1);
		bool chips = GodotObject.IsInstanceValid(firstChip) && GodotObject.IsInstanceValid(stateMachineTransitionChip) && surface.TransitionChipCount == authoredTransitionCount && surface.VisibleTransitionChipCount == authoredTransitionCount;
		bool multiple = chips && firstChip.GetInstanceId() != stateMachineTransitionChip.GetInstanceId() && !firstChip.Position.IsEqualApprox(stateMachineTransitionChip.Position) && !string.Equals(firstChip.Text, stateMachineTransitionChip.Text, StringComparison.Ordinal) && firstChip.Text.Contains("OverviewSelfLoopPrimary", StringComparison.Ordinal) && stateMachineTransitionChip.Text.Contains("OverviewSelfLoopSecondary", StringComparison.Ordinal) && firstChip.TooltipText.Contains(firstStableId, StringComparison.Ordinal) && stateMachineTransitionChip.TooltipText.Contains(secondStableId, StringComparison.Ordinal);
		bool firstNavigated = await NavigateOverviewTransitionChip(surface, firstStableId);
		bool secondNavigated = await NavigateOverviewTransitionChip(surface, secondStableId);
		bool firstRenavigated = await NavigateOverviewTransitionChip(surface, firstStableId);
		LineEdit eventEdit = surface.FindChild("EventNameEdit", recursive: true, owned: false) as LineEdit;
		if (eventEdit != null)
		{
			eventEdit.Text = "OverviewSelfLoopPrimaryEdited";
			eventEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, eventEdit.Text);
		}
		await WaitFrames(2);
		SpinBox priorityEdit = surface.FindChild("PrioritySpin", recursive: true, owned: false) as SpinBox;
		if (priorityEdit != null)
		{
			priorityEdit.Value = 36.0;
			surface.DetailsPanel?.FlushPendingNumericCommit();
		}
		await WaitFrames(3);
		StateMachineTransitionChip editedFirstChip = surface.FindTransitionChip(firstStableId);
		StateMachineTransitionChip untouchedSecondChip = surface.FindTransitionChip(secondStableId);
		int num;
		if (firstNavigated && secondNavigated && firstRenavigated && eventEdit != null && priorityEdit != null && FindTransition(definition, firstStableId)?.EventName.ToString() == "OverviewSelfLoopPrimaryEdited")
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = FindTransition(definition, firstStableId);
			if (stateMachineTransitionDefinition != null && stateMachineTransitionDefinition.Priority == 36 && editedFirstChip?.GetInstanceId() == firstChip?.GetInstanceId() && (editedFirstChip?.IsSelectedTransition ?? false) && editedFirstChip.Text.Contains("OverviewSelfLoopPrimaryEdited", StringComparison.Ordinal) && editedFirstChip.Text.Contains($"P{36}", StringComparison.Ordinal) && editedFirstChip.TooltipText.Contains("OverviewSelfLoopPrimaryEdited", StringComparison.Ordinal) && (untouchedSecondChip?.Text.Contains("OverviewSelfLoopSecondary", StringComparison.Ordinal) ?? false))
			{
				num = ((!untouchedSecondChip.IsSelectedTransition) ? 1 : 0);
				goto IL_0951;
			}
		}
		num = 0;
		goto IL_0951;
		IL_0fcf:
		int num2;
		bool flag = (byte)num2 != 0;
		bool collapsedChips;
		bool collapse = collapsedChips && flag;
		bool firstDeleteRoundTrip = await ProbeTransitionDeleteUndoRedo(surface, definition, firstStableId, secondStableId);
		bool secondDeleteRoundTrip = await ProbeTransitionDeleteUndoRedo(surface, definition, secondStableId, firstStableId);
		ulong firstChipAfterUndoId = surface.FindTransitionChip(firstStableId)?.GetInstanceId() ?? 0;
		ulong secondChipAfterUndoId = surface.FindTransitionChip(secondStableId)?.GetInstanceId() ?? 0;
		Error saveError = ResourceSaver.Save(definition, savePath, ResourceSaver.SaverFlags.None);
		StateMachineDefinition reloaded = ((saveError == Error.Ok) ? ResourceLoader.Load<StateMachineDefinition>(savePath, "", ResourceLoader.CacheMode.Ignore) : null);
		bool reloadedBoth = MatchesEventTransition(FindTransition(reloaded, firstStableId), "Idle", "Idle", "OverviewSelfLoopPrimaryEdited", 0.15, 36) && MatchesEventTransition(FindTransition(reloaded, secondStableId), "Idle", "Idle", "OverviewSelfLoopSecondary", 0.35, 47);
		StateMachineLayout activeLayout = surface.Layout;
		int chipCreateBeforeReload = surface.TransitionChipCreateCount;
		if (reloaded != null)
		{
			surface.LoadDefinition(reloaded, activeLayout);
			await WaitFrames(3);
		}
		StateMachineTransitionChip stateMachineTransitionChip2 = surface.FindTransitionChip(firstStableId);
		StateMachineTransitionChip stateMachineTransitionChip3 = surface.FindTransitionChip(secondStableId);
		bool reloadedChips = reloadedBoth && firstChipAfterUndoId != 0L && secondChipAfterUndoId != 0L && stateMachineTransitionChip2 != null && stateMachineTransitionChip2.GetInstanceId() == firstChipAfterUndoId && stateMachineTransitionChip3 != null && stateMachineTransitionChip3.GetInstanceId() == secondChipAfterUndoId && stateMachineTransitionChip2 != null && stateMachineTransitionChip2.Text.Contains("OverviewSelfLoopPrimaryEdited", StringComparison.Ordinal) && stateMachineTransitionChip2.Text.Contains($"P{36}", StringComparison.Ordinal) && stateMachineTransitionChip3 != null && stateMachineTransitionChip3.Text.Contains("OverviewSelfLoopSecondary", StringComparison.Ordinal) && surface.TransitionChipCount == authoredTransitionCount && surface.VisibleTransitionChipCount == authoredTransitionCount && surface.TransitionChipCreateCount == chipCreateBeforeReload;
		surface.LoadDefinition(definition, activeLayout);
		await WaitFrames(3);
		int num3;
		if (surface.Definition == definition)
		{
			StateMachineTransitionChip stateMachineTransitionChip4 = surface.FindTransitionChip(firstStableId);
			if (stateMachineTransitionChip4 != null && stateMachineTransitionChip4.GetInstanceId() == firstChipAfterUndoId)
			{
				StateMachineTransitionChip stateMachineTransitionChip5 = surface.FindTransitionChip(secondStableId);
				if (stateMachineTransitionChip5 != null && stateMachineTransitionChip5.GetInstanceId() == secondChipAfterUndoId)
				{
					num3 = ((surface.TransitionChipCreateCount == chipCreateBeforeReload) ? 1 : 0);
					goto IL_1487;
				}
			}
		}
		num3 = 0;
		goto IL_1487;
		IL_0e80:
		int num4;
		collapsedChips = (byte)num4 != 0;
		Button collapseButton;
		collapseButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		int chipCreateBeforeCollapse;
		ulong firstChipInstanceId;
		ulong secondChipInstanceId;
		if (!surface.IsStateCollapsed("Root") && surface.VisibleTransitionChipCount == authoredTransitionCount && surface.TransitionChipCreateCount == chipCreateBeforeCollapse)
		{
			StateMachineTransitionChip stateMachineTransitionChip6 = surface.FindTransitionChip(firstStableId);
			if (stateMachineTransitionChip6 != null && stateMachineTransitionChip6.GetInstanceId() == firstChipInstanceId)
			{
				StateMachineTransitionChip stateMachineTransitionChip7 = surface.FindTransitionChip(secondStableId);
				if (stateMachineTransitionChip7 != null && stateMachineTransitionChip7.GetInstanceId() == secondChipInstanceId && (surface.FindTransitionChip(firstStableId)?.Visible ?? false))
				{
					num2 = ((surface.FindTransitionChip(secondStableId)?.Visible ?? false) ? 1 : 0);
					goto IL_0fcf;
				}
			}
		}
		num2 = 0;
		goto IL_0fcf;
		IL_0951:
		bool directEdit = (byte)num != 0;
		foreach (StringName selectedStateNode in surface.GetSelectedStateNodes())
		{
			StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode(selectedStateNode.ToString());
			if (stateMachineGraphNode != null)
			{
				stateMachineGraphNode.Selected = false;
			}
		}
		StateMachineGraphNode movingNode = surface.FindStateNode("Idle");
		if (movingNode != null)
		{
			movingNode.Selected = true;
		}
		Vector2 firstPositionBeforeMove = editedFirstChip?.Position ?? Vector2.Zero;
		Vector2 secondPositionBeforeMove = untouchedSecondChip?.Position ?? Vector2.Zero;
		Vector2 vector = movingNode?.PositionOffset ?? Vector2.Zero;
		surface.EmitSignal(GraphEdit.SignalName.BeginNodeMove);
		if (movingNode != null)
		{
			movingNode.PositionOffset = vector + new Vector2(56f, 34f);
		}
		await WaitFrames(2);
		bool nodeMoveFollow = movingNode != null && editedFirstChip != null && untouchedSecondChip != null && !editedFirstChip.Position.IsEqualApprox(firstPositionBeforeMove) && !untouchedSecondChip.Position.IsEqualApprox(secondPositionBeforeMove);
		surface.EmitSignal(GraphEdit.SignalName.EndNodeMove);
		await WaitFrames(2);
		Vector2 firstPositionBeforeViewport = editedFirstChip?.Position ?? Vector2.Zero;
		Vector2 secondPositionBeforeViewport = untouchedSecondChip?.Position ?? Vector2.Zero;
		Vector2 scrollOffset = surface.ScrollOffset + new Vector2(73f, 41f);
		float zoom = Mathf.Clamp(surface.Zoom + 0.2f, 0.4f, 1.8f);
		EmitViewportGesture(surface, scrollOffset, zoom);
		await WaitFrames(2);
		string nativeCurveDiagnostic;
		bool nativeCurveAligned = ProbeTransitionChipNativeCurveAlignment(surface, out nativeCurveDiagnostic);
		bool viewportFollow = nodeMoveFollow && editedFirstChip != null && untouchedSecondChip != null && !editedFirstChip.Position.IsEqualApprox(firstPositionBeforeViewport) && !untouchedSecondChip.Position.IsEqualApprox(secondPositionBeforeViewport) && editedFirstChip.Visible && untouchedSecondChip.Visible && nativeCurveAligned;
		collapseButton = surface.FindStateNode("Root")?.FindChild("CollapseSubtreeButton", recursive: true, owned: false) as Button;
		firstChipInstanceId = editedFirstChip?.GetInstanceId() ?? 0;
		secondChipInstanceId = untouchedSecondChip?.GetInstanceId() ?? 0;
		chipCreateBeforeCollapse = surface.TransitionChipCreateCount;
		collapseButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		if (collapseButton != null && surface.IsStateCollapsed("Root") && surface.VisibleTransitionChipCount == 0 && surface.TransitionChipCount == authoredTransitionCount)
		{
			StateMachineTransitionChip stateMachineTransitionChip8 = surface.FindTransitionChip(firstStableId);
			if (stateMachineTransitionChip8 != null && !stateMachineTransitionChip8.Visible)
			{
				StateMachineTransitionChip stateMachineTransitionChip9 = surface.FindTransitionChip(secondStableId);
				num4 = ((stateMachineTransitionChip9 != null && !stateMachineTransitionChip9.Visible) ? 1 : 0);
				goto IL_0e80;
			}
		}
		num4 = 0;
		goto IL_0e80;
		IL_1487:
		bool flag2 = (byte)num3 != 0;
		bool flag3 = reloadedChips && flag2;
		StateMachineCompiler.DisposeTemporaryDefinition(reloaded, disposeGuardTrees: true);
		reloaded = null;
		Control obj = XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor");
		PanelContainer panelContainer = obj?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer vBoxContainer = obj?.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		bool flag4 = inspector == null || (inspector.CurrentObject == inspectorSentinel && (panelContainer == null || !panelContainer.Visible) && (vBoxContainer == null || vBoxContainer.GetChildCount() == 0));
		bool num5 = bothPresent && firstNavigated && secondNavigated && firstDeleteRoundTrip && secondDeleteRoundTrip && reloadedBoth;
		if (!(num5 && chips && multiple && directEdit && viewportFollow && collapse && flag3 && flag4))
		{
			StateMachineTransitionDefinition transition3 = FindTransition(reloaded, firstStableId);
			StateMachineTransitionDefinition transition4 = FindTransition(reloaded, secondStableId);
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_TRANSITION_WORKBENCH_DIAGNOSTIC] firstPressed={firstPressed} firstId={firstStableId} firstCreated={firstCreated} secondPressed={secondPressed} secondId={secondStableId} secondCreated={secondCreated} bothPresent={bothPresent} firstNavigate={firstNavigated} secondNavigate={secondNavigated} chips={chips} multiple={multiple} directEdit={directEdit} viewportFollow={viewportFollow} nativeCurveAligned={nativeCurveAligned} curve={nativeCurveDiagnostic} collapse={collapse} saveReload={flag3} inspector={flag4} firstDelete={firstDeleteRoundTrip} secondDelete={secondDeleteRoundTrip} save={saveError} reloadCount={reloaded?.Transitions?.Count ?? (-1)} reloadFirst={FormatTransitionDiagnostic(transition3)} reloadSecond={FormatTransitionDiagnostic(transition4)} reloadedBoth={reloadedBoth}");
		}
		GD.Print($"[MOD_EDITOR_STATE_MACHINE_TRANSITION_CHIP_PROBE] chips={chips} multiple={multiple} directEdit={directEdit} viewportFollow={viewportFollow} collapse={collapse} saveReload={flag3} inspectorUntouched={flag4} count={surface.TransitionChipCount}/{surface.VisibleTransitionChipCount} created={surface.TransitionChipCreateCount}");
		return (workbench: num5, chips: chips, multiple: multiple, directEdit: directEdit, viewportFollow: viewportFollow, collapse: collapse, saveReload: flag3, inspectorUntouched: flag4);
	}

	private async Task<bool> PressTransitionCreatePuzzle(StateMachineGraphEditorSurface surface, string sourceStateId, string targetStateId, string eventName, double delaySeconds, int priority)
	{
		surface.ShowDefinitionOverview();
		await WaitFrames(2);
		OptionButton optionButton = surface.FindChild("TransitionSourceOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton2 = surface.FindChild("TransitionTargetOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton3 = surface.FindChild("TransitionTriggerOption", recursive: true, owned: false) as OptionButton;
		LineEdit lineEdit = surface.FindChild("TransitionEventNameEdit", recursive: true, owned: false) as LineEdit;
		SpinBox spinBox = surface.FindChild("TransitionDelaySecondsEdit", recursive: true, owned: false) as SpinBox;
		SpinBox spinBox2 = surface.FindChild("TransitionPriorityEdit", recursive: true, owned: false) as SpinBox;
		Button button = surface.FindChild("CreateTransitionButton", recursive: true, owned: false) as Button;
		int num = FindStateOptionIndex(optionButton, sourceStateId);
		int num2 = FindStateOptionIndex(optionButton2, targetStateId);
		int num3 = optionButton3?.GetItemIndex(0) ?? (-1);
		if (num < 0 || num2 < 0 || num3 < 0 || lineEdit == null || spinBox == null || spinBox2 == null || (button?.Disabled ?? true))
		{
			return false;
		}
		optionButton.Select(num);
		optionButton.EmitSignal(OptionButton.SignalName.ItemSelected, num);
		optionButton2.Select(num2);
		optionButton2.EmitSignal(OptionButton.SignalName.ItemSelected, num2);
		optionButton3.Select(num3);
		optionButton3.EmitSignal(OptionButton.SignalName.ItemSelected, num3);
		lineEdit.Text = eventName;
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		spinBox.Value = delaySeconds;
		spinBox2.Value = priority;
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		return true;
	}

	private async Task<bool> NavigateOverviewTransitionChip(StateMachineGraphEditorSurface surface, string stableId)
	{
		await WaitFrames(2);
		StateMachineTransitionChip chip = surface.FindTransitionChip(stableId);
		chip?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		return chip != null && surface.ActiveDetailsKind == "Transition" && surface.ActiveDetailsStableId == stableId && chip.IsSelectedTransition && surface.FindTransitionChip(stableId) == chip;
	}

	private async Task<bool> ProbeTransitionDeleteUndoRedo(StateMachineGraphEditorSurface surface, StateMachineDefinition definition, string deleteStableId, string preservedStableId)
	{
		surface.ShowDefinitionOverview();
		await WaitFrames(2);
		Button inspect = FindTransitionChipButton(surface, deleteStableId);
		Button remove = inspect?.GetParent()?.GetChildren().OfType<Button>().LastOrDefault((Button button) => button != inspect);
		remove?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		bool deleted = remove != null && FindTransition(definition, deleteStableId) == null && FindTransition(definition, preservedStableId) != null;
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool undone = FindTransition(definition, deleteStableId) != null && FindTransition(definition, preservedStableId) != null;
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		bool redone = FindTransition(definition, deleteStableId) == null && FindTransition(definition, preservedStableId) != null;
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool flag = FindTransition(definition, deleteStableId) != null && FindTransition(definition, preservedStableId) != null;
		return deleted && undone && redone && flag;
	}

	private static Button FindTransitionChipButton(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null || string.IsNullOrWhiteSpace(stableId))
		{
			return null;
		}
		string value = "StableId: " + stableId;
		foreach (Node item in surface.FindChildren("TransitionChip_*", "Button", recursive: true, owned: false))
		{
			if (item is Button button && button.TooltipText.EndsWith(value, StringComparison.Ordinal))
			{
				return button;
			}
		}
		foreach (Node item2 in surface.FindChildren("*", "Button", recursive: true, owned: false))
		{
			if (item2 is Button button2 && button2.TooltipText.EndsWith(value, StringComparison.Ordinal))
			{
				return button2;
			}
		}
		return null;
	}

	private static int FindStateOptionIndex(OptionButton option, string stableId)
	{
		if (option == null || string.IsNullOrWhiteSpace(stableId))
		{
			return -1;
		}
		string value = "  ·  " + stableId;
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (option.GetItemText(i).EndsWith(value, StringComparison.Ordinal))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool SelectOptionByMetadata(OptionButton option, string metadata)
	{
		if (option == null || string.IsNullOrWhiteSpace(metadata))
		{
			return false;
		}
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (string.Equals(option.GetItemMetadata(i).AsString(), metadata, StringComparison.Ordinal))
			{
				option.Select(i);
				option.EmitSignal(OptionButton.SignalName.ItemSelected, i);
				return option.Selected == i;
			}
		}
		return false;
	}

	private static bool IsPopupItemDisabled(PopupMenu menu, long id)
	{
		if (menu == null)
		{
			return false;
		}
		int itemIndex = menu.GetItemIndex((int)id);
		if (itemIndex >= 0)
		{
			return menu.IsItemDisabled(itemIndex);
		}
		return false;
	}

	private static bool MatchesEventTransition(StateMachineTransitionDefinition transition, string sourceStateId, string targetStateId, string eventName, double delaySeconds, int priority)
	{
		if (transition != null && transition.SourceStateId == sourceStateId && transition.TargetStateId == targetStateId && transition.TriggerKind == StateMachineTriggerKind.Event && transition.EventName.ToString() == eventName && Math.Abs(transition.DelaySeconds - delaySeconds) < 0.001)
		{
			return transition.Priority == priority;
		}
		return false;
	}

	private static string FormatTransitionDiagnostic(StateMachineTransitionDefinition transition)
	{
		if (transition == null)
		{
			return "<missing>";
		}
		return $"{transition.SourceStateId}->{transition.TargetStateId}:{transition.TriggerKind}:{transition.EventName}:{transition.DelaySeconds:0.###}:{transition.Priority}";
	}

	private static HashSet<string> CaptureTransitionIds(StateMachineDefinition definition)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (definition?.Transitions == null)
		{
			return hashSet;
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (!string.IsNullOrWhiteSpace(transition?.StableId))
			{
				hashSet.Add(transition.StableId);
			}
		}
		return hashSet;
	}

	private static string FindAddedTransitionId(StateMachineDefinition definition, HashSet<string> before)
	{
		if (definition?.Transitions == null)
		{
			return string.Empty;
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition != null && !before.Contains(transition.StableId))
			{
				return transition.StableId;
			}
		}
		return string.Empty;
	}

	private async Task<(bool groupedUndo, bool signals)> ProbeGroupedNodeMove(StateMachineGraphEditorSurface surface, StateMachineDefinition definition, string secondStableId)
	{
		StateMachineGraphNode stateMachineGraphNode = surface?.FindStateNode("Idle");
		StateMachineGraphNode stateMachineGraphNode2 = surface?.FindStateNode(secondStableId);
		if (stateMachineGraphNode == null || stateMachineGraphNode2 == null)
		{
			return (groupedUndo: false, signals: false);
		}
		foreach (StateMachineStateDefinition state in definition.States)
		{
			StateMachineGraphNode stateMachineGraphNode3 = surface.FindStateNode(state?.StableId);
			if (stateMachineGraphNode3 != null)
			{
				stateMachineGraphNode3.Selected = false;
			}
		}
		stateMachineGraphNode.Selected = true;
		stateMachineGraphNode2.Selected = true;
		bool selectedTwo = surface.GetSelectedStateNodes().Count == 2;
		Vector2 firstBefore = stateMachineGraphNode.PositionOffset;
		Vector2 secondBefore = stateMachineGraphNode2.PositionOffset;
		Vector2 firstAfter = firstBefore + new Vector2(72f, 36f);
		Vector2 secondAfter = secondBefore + new Vector2(-48f, 64f);
		surface.EmitSignal(GraphEdit.SignalName.BeginNodeMove);
		stateMachineGraphNode.PositionOffset = firstAfter;
		stateMachineGraphNode2.PositionOffset = secondAfter;
		surface.EmitSignal(GraphEdit.SignalName.EndNodeMove);
		await WaitFrames(2);
		bool movedTogether = (surface.FindStateNode("Idle")?.PositionOffset.IsEqualApprox(firstAfter) ?? false) && (surface.FindStateNode(secondStableId)?.PositionOffset.IsEqualApprox(secondAfter) ?? false);
		surface.GraphController?.Undo();
		surface.RefreshGraph();
		await WaitFrames(1);
		bool undoneTogether = (surface.FindStateNode("Idle")?.PositionOffset.IsEqualApprox(firstBefore) ?? false) && (surface.FindStateNode(secondStableId)?.PositionOffset.IsEqualApprox(secondBefore) ?? false);
		surface.GraphController?.Redo();
		surface.RefreshGraph();
		await WaitFrames(1);
		bool flag = (surface.FindStateNode("Idle")?.PositionOffset.IsEqualApprox(firstAfter) ?? false) && (surface.FindStateNode(secondStableId)?.PositionOffset.IsEqualApprox(secondAfter) ?? false);
		return (groupedUndo: selectedTwo && movedTogether && undoneTogether && flag, signals: true);
	}

	private async Task<(bool contextMenu, bool clipboardSignals, bool pointerPlacement, bool shortcutUndoRedo, bool clipboardParent)> ProbeCanvasEditingSignals(StateMachineGraphEditorSurface surface, StateMachineDefinition definition)
	{
		if (surface == null || definition == null || surface.FindStateNode("Idle") == null)
		{
			return (contextMenu: false, clipboardSignals: false, pointerPlacement: false, shortcutUndoRedo: false, clipboardParent: false);
		}
		bool menuActions = true;
		bool placements = true;
		Vector2 popupLocal = new Vector2(156f, 118f);
		Vector2 popupGraph = ToExpectedGraphPosition(surface, popupLocal);
		surface.EmitSignal(GraphEdit.SignalName.PopupRequest, popupLocal);
		await WaitFrames(1);
		PopupMenu menu = surface.FindChild("StateMachineCanvasContextMenu", recursive: true, owned: false) as PopupMenu;
		Vector2 to = surface.GetScreenPosition() + popupLocal;
		bool popupAtPointer = (menu?.Visible ?? false) && new Vector2(menu.Position.X, menu.Position.Y).DistanceTo(to) <= 2f && surface.LastPointerGraphPosition.IsEqualApprox(popupGraph);
		HashSet<string> beforeMenuAdd = CaptureStateIds(definition);
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 100L);
		await WaitFrames(2);
		string menuAddedId = FindAddedStateId(definition, beforeMenuAdd);
		menuActions &= !string.IsNullOrWhiteSpace(menuAddedId);
		placements &= PositionMatches(surface.Layout, menuAddedId, popupGraph);
		SelectOnly(surface, definition, menuAddedId);
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 204L);
		await WaitFrames(2);
		menuActions &= FindState(definition, menuAddedId) == null;
		SelectOnly(surface, definition, "Idle");
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 200L);
		Vector2 vector = new Vector2(248f, 164f);
		Vector2 menuPasteGraph = ToExpectedGraphPosition(surface, vector);
		surface.EmitSignal(GraphEdit.SignalName.PopupRequest, vector);
		await WaitFrames(1);
		HashSet<string> beforeMenuPaste = CaptureStateIds(definition);
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 203L);
		await WaitFrames(2);
		string menuPastedId = FindAddedStateId(definition, beforeMenuPaste);
		menuActions &= !string.IsNullOrWhiteSpace(menuPastedId);
		placements &= PositionMatches(surface.Layout, menuPastedId, menuPasteGraph);
		bool parentsPreserved = FindState(definition, menuPastedId)?.ParentId == "Root";
		SelectOnly(surface, definition, "Idle");
		Vector2 vector2 = new Vector2(326f, 206f);
		Vector2 menuDuplicateGraph = ToExpectedGraphPosition(surface, vector2);
		surface.EmitSignal(GraphEdit.SignalName.PopupRequest, vector2);
		await WaitFrames(1);
		HashSet<string> beforeMenuDuplicate = CaptureStateIds(definition);
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 202L);
		await WaitFrames(2);
		string menuDuplicatedId = FindAddedStateId(definition, beforeMenuDuplicate);
		menuActions &= !string.IsNullOrWhiteSpace(menuDuplicatedId);
		placements &= PositionMatches(surface.Layout, menuDuplicatedId, menuDuplicateGraph);
		SelectOnly(surface, definition, menuDuplicatedId);
		int beforeMenuCut = definition.States.Count;
		menu?.EmitSignal(PopupMenu.SignalName.IdPressed, 201L);
		await WaitFrames(2);
		menuActions &= definition.States.Count == beforeMenuCut - 1 && FindState(definition, menuDuplicatedId) == null;
		menu?.Hide();
		SelectOnly(surface, definition, "Idle");
		surface.EmitSignal(GraphEdit.SignalName.CopyNodesRequest);
		Vector2 localPosition = new Vector2(196f, 252f);
		Vector2 pasteGraph = EmitPointerMotion(surface, localPosition);
		HashSet<string> beforePaste = CaptureStateIds(definition);
		surface.EmitSignal(GraphEdit.SignalName.PasteNodesRequest);
		await WaitFrames(2);
		string pastedId = FindAddedStateId(definition, beforePaste);
		bool directSignals = !string.IsNullOrWhiteSpace(pastedId);
		placements &= PositionMatches(surface.Layout, pastedId, pasteGraph);
		parentsPreserved &= FindState(definition, pastedId)?.ParentId == "Root";
		SelectOnly(surface, definition, "Idle");
		Vector2 localPosition2 = new Vector2(372f, 286f);
		Vector2 duplicateGraph = EmitPointerMotion(surface, localPosition2);
		HashSet<string> beforeDuplicate = CaptureStateIds(definition);
		surface.EmitSignal(GraphEdit.SignalName.DuplicateNodesRequest);
		await WaitFrames(2);
		string duplicatedId = FindAddedStateId(definition, beforeDuplicate);
		directSignals &= !string.IsNullOrWhiteSpace(duplicatedId);
		placements &= PositionMatches(surface.Layout, duplicatedId, duplicateGraph);
		SelectOnly(surface, definition, duplicatedId);
		int beforeCut = definition.States.Count;
		surface.EmitSignal(GraphEdit.SignalName.CutNodesRequest);
		await WaitFrames(2);
		directSignals &= definition.States.Count == beforeCut - 1 && FindState(definition, duplicatedId) == null;
		Vector2 localPosition3 = new Vector2(424f, 318f);
		Vector2 cutPasteGraph = EmitPointerMotion(surface, localPosition3);
		HashSet<string> beforeCutPaste = CaptureStateIds(definition);
		surface.EmitSignal(GraphEdit.SignalName.PasteNodesRequest);
		await WaitFrames(2);
		string cutPastedId = FindAddedStateId(definition, beforeCutPaste);
		directSignals &= !string.IsNullOrWhiteSpace(cutPastedId);
		placements &= PositionMatches(surface.Layout, cutPastedId, cutPasteGraph);
		StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode(cutPastedId);
		Array<StringName> deleteRequest = new Array<StringName>();
		if (stateMachineGraphNode != null)
		{
			deleteRequest.Add(stateMachineGraphNode.Name);
		}
		int beforeDelete = definition.States.Count;
		surface.EmitSignal(GraphEdit.SignalName.DeleteNodesRequest, deleteRequest);
		await WaitFrames(2);
		directSignals &= deleteRequest.Count == 1 && definition.States.Count == beforeDelete - 1 && FindState(definition, cutPastedId) == null;
		SelectOnly(surface, definition, "Idle");
		surface.EmitSignal(GraphEdit.SignalName.CopyNodesRequest);
		EmitPointerMotion(surface, new Vector2(474f, 346f));
		int shortcutStart = definition.States.Count;
		surface.EmitSignal(GraphEdit.SignalName.PasteNodesRequest);
		await WaitFrames(2);
		int shortcutApplied = definition.States.Count;
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		int shortcutUndone = definition.States.Count;
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		int count = definition.States.Count;
		bool item = shortcutApplied == shortcutStart + 1 && shortcutUndone == shortcutStart && count == shortcutApplied;
		StateMachineValidationResult stateMachineValidationResult = StateMachineValidator.Validate(definition);
		parentsPreserved &= stateMachineValidationResult.IsValid;
		if (!parentsPreserved)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_CLIPBOARD_DIAGNOSTIC] menuParent={FindState(definition, menuPastedId)?.ParentId} directParent={FindState(definition, pastedId)?.ParentId} valid={stateMachineValidationResult.IsValid} diagnostics={string.Join("|", stateMachineValidationResult.Diagnostics.Select((StateMachineDiagnostic stateMachineDiagnostic) => $"{stateMachineDiagnostic.Code}:{stateMachineDiagnostic.StableId}:{stateMachineDiagnostic.Message}"))}");
		}
		return (contextMenu: popupAtPointer && menuActions, clipboardSignals: directSignals, pointerPlacement: placements, shortcutUndoRedo: item, clipboardParent: parentsPreserved);
	}

	private static Vector2 EmitPointerMotion(StateMachineGraphEditorSurface surface, Vector2 localPosition)
	{
		InputEventMouseMotion inputEventMouseMotion = new InputEventMouseMotion
		{
			Position = localPosition
		};
		surface.EmitSignal(Control.SignalName.GuiInput, inputEventMouseMotion);
		return ToExpectedGraphPosition(surface, localPosition);
	}

	private static void EmitSurfaceShortcut(StateMachineGraphEditorSurface surface, Key key, bool shiftPressed = false)
	{
		InputEventKey inputEventKey = new InputEventKey
		{
			Keycode = key,
			PhysicalKeycode = key,
			CtrlPressed = true,
			ShiftPressed = shiftPressed,
			Pressed = true
		};
		surface.EmitSignal(Control.SignalName.GuiInput, inputEventKey);
	}

	private static Vector2 ToExpectedGraphPosition(StateMachineGraphEditorSurface surface, Vector2 localPosition)
	{
		return surface.ScrollOffset + localPosition / Math.Max(surface.Zoom, 0.001f);
	}

	private static void SelectOnly(StateMachineGraphEditorSurface surface, StateMachineDefinition definition, string stableId)
	{
		if (surface == null || definition?.States == null)
		{
			return;
		}
		foreach (StateMachineStateDefinition state in definition.States)
		{
			StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode(state?.StableId);
			if (stateMachineGraphNode != null)
			{
				stateMachineGraphNode.Selected = state?.StableId == stableId;
			}
		}
	}

	private static HashSet<string> CaptureStateIds(StateMachineDefinition definition)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (definition?.States == null)
		{
			return hashSet;
		}
		foreach (StateMachineStateDefinition state in definition.States)
		{
			if (!string.IsNullOrWhiteSpace(state?.StableId))
			{
				hashSet.Add(state.StableId);
			}
		}
		return hashSet;
	}

	private static string FindAddedStateId(StateMachineDefinition definition, HashSet<string> before)
	{
		if (definition?.States == null)
		{
			return string.Empty;
		}
		foreach (StateMachineStateDefinition state in definition.States)
		{
			if (state != null && !before.Contains(state.StableId))
			{
				return state.StableId;
			}
		}
		return string.Empty;
	}

	private static bool PositionMatches(StateMachineLayout layout, string stableId, Vector2 expected)
	{
		if (layout != null && !string.IsNullOrWhiteSpace(stableId) && layout.Positions.TryGetValue(stableId, out var value))
		{
			return value.IsEqualApprox(expected);
		}
		return false;
	}

	private async Task<(bool persisted, bool singleTimer)> ProbeViewportDebounce(StateMachineGraphEditorSurface surface, string definitionPath)
	{
		if (surface?.Layout == null)
		{
			return (persisted: false, singleTimer: false);
		}
		Vector2 scrollOffset = surface.Layout.ScrollOffset;
		float zoom = surface.Layout.Zoom;
		Vector2 targetScroll = new Vector2(214f, 132f);
		EmitViewportGesture(surface, targetScroll, 1.2f);
		bool singleTimerWhilePending = surface.ActiveViewportDebounceTimerCount == 1;
		bool deferred = surface.Layout.ScrollOffset.IsEqualApprox(scrollOffset) && Math.Abs(surface.Layout.Zoom - zoom) < 0.001f;
		await ToSignal(GetTree().CreateTimer(0.35), SceneTreeTimer.SignalName.Timeout);
		await WaitFrames(2);
		StateMachineLayout layout = LoadLayoutSidecar(definitionPath);
		return (persisted: deferred && ViewportMatches(surface.Layout, targetScroll, 1.2f) && ViewportMatches(layout, targetScroll, 1.2f), singleTimer: singleTimerWhilePending && surface.ActiveViewportDebounceTimerCount == 0);
	}

	private async Task<(bool collapseControl, bool collapseDescendants, bool collapseConnections, bool collapseSelectionClean, bool collapseUndoRedo, bool collapseSaveReload, bool collapseExpandRestore, bool collapseInspectorUntouched, bool initialButtonGuard)> ProbeHierarchyCollapseWorkbench(XWStateMachineVisualResourceEditor editor, XWInspector inspector, GodotObject sentinel)
	{
		RemoveProbeResource("user://mod_editor_state_machine_collapse_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_collapse_probe.tres"));
		RemoveProbeResource("user://mod_editor_state_machine_collapse_switch_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_collapse_switch_probe.tres"));
		StateMachineDefinition definition = CreateHierarchyCollapseDefinition();
		StateMachineDefinition switchDefinition = CreateSimulationDefinition();
		Error error = ResourceSaver.Save(definition, "user://mod_editor_state_machine_collapse_probe.tres", ResourceSaver.SaverFlags.None);
		Error error2 = ResourceSaver.Save(switchDefinition, "user://mod_editor_state_machine_collapse_switch_probe.tres", ResourceSaver.SaverFlags.None);
		bool opened = error == Error.Ok && error2 == Error.Ok && XWResourceEditorRegistry.TryOpen(definition, "user://mod_editor_state_machine_collapse_probe.tres");
		await WaitFrames(4);
		StateMachineGraphEditorSurface surface = editor?.GraphSurface;
		if (!opened || surface?.GraphController?.ViewModel == null)
		{
			return default((bool, bool, bool, bool, bool, bool, bool, bool, bool));
		}
		StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode("CollapseRoot");
		StateMachineGraphNode branch = surface.FindStateNode("CollapseBranch");
		StateMachineGraphNode nested = surface.FindStateNode("CollapseNested");
		StateMachineGraphNode leaf = surface.FindStateNode("CollapseLeaf");
		StateMachineGraphNode outside = surface.FindStateNode("CollapseOutside");
		StateMachineGraphNode parallel = surface.FindStateNode("CollapseParallel");
		StateMachineGraphNode parallelLeaf = surface.FindStateNode("CollapseParallelLeaf");
		Button collapseButton = branch?.FindChild("CollapseSubtreeButton", recursive: true, owned: false) as Button;
		Button button = leaf?.FindChild("CollapseSubtreeButton", recursive: true, owned: false) as Button;
		Label label = stateMachineGraphNode?.FindChild("RootStateBadge", recursive: true, owned: false) as Label;
		Label label2 = branch?.FindChild("InitialChildBadge", recursive: true, owned: false) as Label;
		Label label3 = branch?.FindChild("HierarchyBadge", recursive: true, owned: false) as Label;
		Button button2 = parallelLeaf?.FindChild("InitialButton", recursive: true, owned: false) as Button;
		bool collapseControl = StateMachineValidator.Validate(definition).IsValid && surface.VisibleStateNodeCount == 7 && surface.VisibleConnectionCount == 3 && surface.GetConnectionList().Count == 3 && (collapseButton?.Visible ?? false) && collapseButton.Icon != null && collapseButton.TooltipText.Contains("折叠", StringComparison.Ordinal) && button != null && !button.Visible && branch.HierarchyChildCount == 1 && label3 != null && label3.Text.Contains("1", StringComparison.Ordinal) && label != null && label.Visible && (label2?.Visible ?? false);
		int num;
		if (button2 != null && !button2.Visible)
		{
			if (parallelLeaf != null && !parallelLeaf.CanSetAsInitial)
			{
				num = ((branch?.CanSetAsInitial ?? false) ? 1 : 0);
				goto IL_03d8;
			}
		}
		num = 0;
		goto IL_03d8;
		IL_03d8:
		bool initialButtonGuard = (byte)num != 0;
		System.Collections.Generic.Dictionary<string, ulong> stableInstanceIds = new System.Collections.Generic.Dictionary<string, ulong>(StringComparer.Ordinal);
		string[] array = new string[7] { "CollapseRoot", "CollapseBranch", "CollapseNested", "CollapseLeaf", "CollapseOutside", "CollapseParallel", "CollapseParallelLeaf" };
		foreach (string text in array)
		{
			stableInstanceIds[text] = surface.FindStateNode(text)?.GetInstanceId() ?? 0;
		}
		int fullBefore = surface.FullGraphRebuildCount;
		int incrementalBefore = surface.IncrementalGraphRefreshCount;
		int createdBefore = surface.GraphNodeCreateCount;
		nested.Selected = true;
		leaf.Selected = true;
		surface.NavigateToStableId("CollapseLeafToOutside");
		collapseButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool collapseDescendants = surface.IsStateCollapsed("CollapseBranch") && branch.Visible && !nested.Visible && !leaf.Visible && outside.Visible && parallel.Visible && parallelLeaf.Visible && surface.VisibleStateNodeCount == 5 && (collapseButton?.TooltipText.Contains("展开", StringComparison.Ordinal) ?? false);
		bool collapseConnections = surface.HasVisualConnection("CollapseBranch", "CollapseOutside") && !surface.HasVisualConnection("CollapseNested", "CollapseOutside") && !surface.HasVisualConnection("CollapseLeaf", "CollapseOutside") && surface.VisibleConnectionCount == 1 && surface.GetConnectionList().Count == 1;
		bool collapseSelectionClean = !nested.Selected && !leaf.Selected && branch.Selected && surface.ActiveDetailsKind == "State" && surface.ActiveDetailsStableId == "CollapseBranch" && !surface.GetSelectedStateNodes().Contains("CollapseNested") && !surface.GetSelectedStateNodes().Contains("CollapseLeaf");
		HBoxContainer owner = editor?.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button undo = FindButtonByText(owner, "撤销");
		FindButtonByText(owner, "重做");
		undo?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool undoRestored = !surface.IsStateCollapsed("CollapseBranch") && surface.VisibleStateNodeCount == 7 && surface.VisibleConnectionCount == 3 && nested.Visible && leaf.Visible;
		owner = editor?.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button redo = FindButtonByText(owner, "重做");
		redo?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool flag = surface.IsStateCollapsed("CollapseBranch") && surface.VisibleStateNodeCount == 5 && surface.VisibleConnectionCount == 1 && !nested.Visible && !leaf.Visible;
		bool flag2 = true;
		foreach (KeyValuePair<string, ulong> item in stableInstanceIds)
		{
			flag2 &= surface.FindStateNode(item.Key)?.GetInstanceId() == item.Value;
		}
		bool collapseUndoRedo = undo != null && redo != null && undoRestored && flag && flag2 && surface.FullGraphRebuildCount == fullBefore && surface.IncrementalGraphRefreshCount == incrementalBefore + 3 && surface.GraphNodeCreateCount == createdBefore;
		await WaitFrames(3);
		StateMachineLayout layout = LoadLayoutSidecar("user://mod_editor_state_machine_collapse_probe.tres");
		bool sidecarCollapsed = IsLayoutStateCollapsed(layout, "CollapseBranch");
		bool switchOpened = XWResourceEditorRegistry.TryOpen(switchDefinition, "user://mod_editor_state_machine_collapse_switch_probe.tres");
		await WaitFrames(3);
		StateMachineDefinition stateMachineDefinition = ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_collapse_probe.tres", "", ResourceLoader.CacheMode.Ignore);
		bool reopened = switchOpened && stateMachineDefinition != null && XWResourceEditorRegistry.TryOpen(stateMachineDefinition, "user://mod_editor_state_machine_collapse_probe.tres");
		await WaitFrames(4);
		surface = editor?.GraphSurface;
		branch = surface?.FindStateNode("CollapseBranch");
		nested = surface?.FindStateNode("CollapseNested");
		leaf = surface?.FindStateNode("CollapseLeaf");
		collapseButton = branch?.FindChild("CollapseSubtreeButton", recursive: true, owned: false) as Button;
		int num2;
		if (reopened && (surface?.IsStateCollapsed("CollapseBranch") ?? false) && (branch?.Visible ?? false))
		{
			StateMachineGraphNode stateMachineGraphNode2 = nested;
			if (stateMachineGraphNode2 != null && !stateMachineGraphNode2.Visible)
			{
				StateMachineGraphNode stateMachineGraphNode3 = leaf;
				if (stateMachineGraphNode3 != null && !stateMachineGraphNode3.Visible && surface.VisibleStateNodeCount == 5)
				{
					num2 = ((surface.VisibleConnectionCount == 1) ? 1 : 0);
					goto IL_0c6e;
				}
			}
		}
		num2 = 0;
		goto IL_0c6e;
		IL_0c6e:
		bool flag3 = (byte)num2 != 0;
		bool collapseSaveReload = sidecarCollapsed && flag3;
		collapseButton?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		StateMachineLayout layout2 = LoadLayoutSidecar("user://mod_editor_state_machine_collapse_probe.tres");
		StateMachineGraphEditorSurface stateMachineGraphEditorSurface = surface;
		bool flag4 = stateMachineGraphEditorSurface != null && !stateMachineGraphEditorSurface.IsStateCollapsed("CollapseBranch") && (branch?.Visible ?? false) && (nested?.Visible ?? false) && (leaf?.Visible ?? false) && surface.VisibleStateNodeCount == 7 && surface.VisibleConnectionCount == 3 && surface.GetConnectionList().Count == 3 && !nested.Selected && !leaf.Selected && !IsLayoutStateCollapsed(layout2, "CollapseBranch");
		PanelContainer panelContainer = editor?.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer vBoxContainer = editor?.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		bool flag5 = inspector == null || (inspector.CurrentObject == sentinel && (panelContainer == null || !panelContainer.Visible) && (vBoxContainer == null || vBoxContainer.GetChildCount() == 0));
		if (!(collapseControl && collapseDescendants && collapseConnections && collapseSelectionClean && collapseUndoRedo && collapseSaveReload && flag4 && flag5 && initialButtonGuard))
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_COLLAPSE_DIAGNOSTIC] control={collapseControl} descendants={collapseDescendants} connections={collapseConnections} selection={collapseSelectionClean} undoRedo={collapseUndoRedo} saveReload={collapseSaveReload} expand={flag4} inspector={flag5} initialGuard={initialButtonGuard} visible={surface?.VisibleStateNodeCount ?? (-1)}/{surface?.VisibleConnectionCount ?? (-1)} fullDelta={(surface?.FullGraphRebuildCount ?? fullBefore) - fullBefore} incrementalDelta={(surface?.IncrementalGraphRefreshCount ?? incrementalBefore) - incrementalBefore}");
		}
		return (collapseControl: collapseControl, collapseDescendants: collapseDescendants, collapseConnections: collapseConnections, collapseSelectionClean: collapseSelectionClean, collapseUndoRedo: collapseUndoRedo, collapseSaveReload: collapseSaveReload, collapseExpandRestore: flag4, collapseInspectorUntouched: flag5, initialButtonGuard: initialButtonGuard);
	}

	private async Task<(bool graphIncremental, bool collapseIncremental, bool transitionChipIncremental, bool transitionChipMoveScope)> ProbeLargeGraphIncremental(XWStateMachineVisualResourceEditor editor)
	{
		StateMachineDefinition definition = CreateLargeGraphDefinition();
		ulong openStarted = Time.GetTicksMsec();
		bool opened = XWResourceEditorRegistry.TryOpen(definition, "user://mod_editor_state_machine_large_probe.tres");
		await WaitFrames(4);
		ulong openSettledElapsed = Time.GetTicksMsec() - openStarted;
		StateMachineGraphEditorSurface surface = editor?.GraphSurface;
		if (!opened || surface?.GraphController?.ViewModel == null)
		{
			return default((bool, bool, bool, bool));
		}
		bool graphComplete = StateMachineValidator.Validate(definition).IsValid && surface.GraphController.ViewModel.Nodes.Count == 500 && surface.GraphController.ViewModel.Transitions.Count == 800 && surface.GetChildren().OfType<StateMachineGraphNode>().Count() == 500 && surface.GetConnectionList().Count == 800 && surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 800;
		StateMachineGraphNode stableA = surface.FindStateNode("LargeState100");
		StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode("LargeState498");
		StateMachineTransitionChip stateMachineTransitionChip = surface.FindTransitionChip("LargeEdgeA100");
		StateMachineTransitionChip stateMachineTransitionChip2 = surface.FindTransitionChip("LargeEdgeB100");
		ulong stableAId = stableA?.GetInstanceId() ?? 0;
		ulong stableBId = stateMachineGraphNode?.GetInstanceId() ?? 0;
		ulong stableChipAId = stateMachineTransitionChip?.GetInstanceId() ?? 0;
		ulong stableChipBId = stateMachineTransitionChip2?.GetInstanceId() ?? 0;
		int fullBefore = surface.FullGraphRebuildCount;
		int incrementalBefore = surface.IncrementalGraphRefreshCount;
		int createdBefore = surface.GraphNodeCreateCount;
		int chipCreatedBefore = surface.TransitionChipCreateCount;
		ulong refreshStarted = Time.GetTicksMsec();
		for (int i = 0; i < 3; i++)
		{
			surface.RefreshGraph();
		}
		await WaitFrames(3);
		ulong refreshSettledElapsed = Time.GetTicksMsec() - refreshStarted;
		int num;
		if (surface.FullGraphRebuildCount == fullBefore && surface.IncrementalGraphRefreshCount == incrementalBefore + 3 && surface.GraphNodeCreateCount == createdBefore)
		{
			StateMachineGraphNode stateMachineGraphNode2 = surface.FindStateNode("LargeState100");
			if (stateMachineGraphNode2 != null && stateMachineGraphNode2.GetInstanceId() == stableAId)
			{
				StateMachineGraphNode stateMachineGraphNode3 = surface.FindStateNode("LargeState498");
				num = ((stateMachineGraphNode3 != null && stateMachineGraphNode3.GetInstanceId() == stableBId) ? 1 : 0);
				goto IL_03e6;
			}
		}
		num = 0;
		goto IL_03e6;
		IL_03e6:
		bool explicitRefreshReused = (byte)num != 0;
		int num2;
		if (surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 800 && surface.TransitionChipCreateCount == chipCreatedBefore)
		{
			StateMachineTransitionChip stateMachineTransitionChip3 = surface.FindTransitionChip("LargeEdgeA100");
			if (stateMachineTransitionChip3 != null && stateMachineTransitionChip3.GetInstanceId() == stableChipAId)
			{
				StateMachineTransitionChip stateMachineTransitionChip4 = surface.FindTransitionChip("LargeEdgeB100");
				num2 = ((stateMachineTransitionChip4 != null && stateMachineTransitionChip4.GetInstanceId() == stableChipBId) ? 1 : 0);
				goto IL_0470;
			}
		}
		num2 = 0;
		goto IL_0470;
		IL_0c5f:
		int num3;
		bool transitionChipMoveScope = (byte)num3 != 0;
		surface.EmitSignal(GraphEdit.SignalName.EndNodeMove);
		await WaitFrames(2);
		int relayoutAfterMoveCommit = surface.LastTransitionChipRelayoutCount;
		string relayoutAfterMoveReason = surface.LastTransitionChipRelayoutReason;
		bool moveCommitReused = surface.TransitionChipCreateCount == chipCreatedBefore && surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 800;
		bool adjacentCommitLayout = relayoutAfterMoveCommit == 4 && relayoutAfterMoveReason.EndsWith(":adjacent", StringComparison.Ordinal);
		bool coalescedViewportLayout = relayoutAfterMoveCommit == 800 && relayoutAfterMoveReason.StartsWith("OnScrollOffsetChanged:", StringComparison.Ordinal) && relayoutAfterMoveReason.EndsWith(":deferred", StringComparison.Ordinal);
		transitionChipMoveScope = transitionChipMoveScope && (adjacentCommitLayout || coalescedViewportLayout) && moveCommitReused;
		StateMachineGraphNode root = surface.FindStateNode("Root");
		Button collapse = root?.FindChild("CollapseSubtreeButton", recursive: true, owned: false) as Button;
		int fullBeforeCollapse = surface.FullGraphRebuildCount;
		int incrementalBeforeCollapse = surface.IncrementalGraphRefreshCount;
		int createdBeforeCollapse = surface.GraphNodeCreateCount;
		ulong collapseStarted = Time.GetTicksMsec();
		collapse?.EmitSignal(BaseButton.SignalName.Pressed);
		ulong collapseElapsed = Time.GetTicksMsec() - collapseStarted;
		await WaitFrames(3);
		ulong collapseSettledElapsed = Time.GetTicksMsec() - collapseStarted;
		int num4;
		if (collapse != null && surface.IsStateCollapsed("Root") && surface.VisibleStateNodeCount == 1 && surface.VisibleConnectionCount == 0 && surface.GetConnectionList().Count == 0 && surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 0 && root.Visible && !surface.FindStateNode("LargeState100").Visible && surface.FullGraphRebuildCount == fullBeforeCollapse && surface.IncrementalGraphRefreshCount == incrementalBeforeCollapse + 1 && surface.GraphNodeCreateCount == createdBeforeCollapse)
		{
			StateMachineGraphNode stateMachineGraphNode4 = surface.FindStateNode("LargeState100");
			if (stateMachineGraphNode4 != null && stateMachineGraphNode4.GetInstanceId() == stableAId)
			{
				StateMachineGraphNode stateMachineGraphNode5 = surface.FindStateNode("LargeState498");
				if (stateMachineGraphNode5 != null && stateMachineGraphNode5.GetInstanceId() == stableBId && surface.TransitionChipCreateCount == chipCreatedBefore)
				{
					StateMachineTransitionChip stateMachineTransitionChip5 = surface.FindTransitionChip("LargeEdgeA100");
					if (stateMachineTransitionChip5 != null && stateMachineTransitionChip5.GetInstanceId() == stableChipAId)
					{
						StateMachineTransitionChip stateMachineTransitionChip6 = surface.FindTransitionChip("LargeEdgeB100");
						if (stateMachineTransitionChip6 != null && stateMachineTransitionChip6.GetInstanceId() == stableChipBId)
						{
							num4 = ((collapseElapsed < 2500) ? 1 : 0);
							goto IL_107d;
						}
					}
				}
			}
		}
		num4 = 0;
		goto IL_107d;
		IL_107d:
		bool collapsedIncrementally = (byte)num4 != 0;
		ulong expandStarted = Time.GetTicksMsec();
		collapse?.EmitSignal(BaseButton.SignalName.Pressed);
		ulong expandElapsed = Time.GetTicksMsec() - expandStarted;
		await WaitFrames(3);
		ulong num5 = Time.GetTicksMsec() - expandStarted;
		int num6;
		if (!surface.IsStateCollapsed("Root") && surface.VisibleStateNodeCount == 500 && surface.VisibleConnectionCount == 800 && surface.GetConnectionList().Count == 800 && surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 800 && surface.FindStateNode("LargeState100").Visible && surface.FullGraphRebuildCount == fullBeforeCollapse && surface.IncrementalGraphRefreshCount == incrementalBeforeCollapse + 2 && surface.GraphNodeCreateCount == createdBeforeCollapse)
		{
			StateMachineGraphNode stateMachineGraphNode6 = surface.FindStateNode("LargeState100");
			if (stateMachineGraphNode6 != null && stateMachineGraphNode6.GetInstanceId() == stableAId)
			{
				StateMachineGraphNode stateMachineGraphNode7 = surface.FindStateNode("LargeState498");
				if (stateMachineGraphNode7 != null && stateMachineGraphNode7.GetInstanceId() == stableBId && surface.TransitionChipCreateCount == chipCreatedBefore)
				{
					StateMachineTransitionChip stateMachineTransitionChip7 = surface.FindTransitionChip("LargeEdgeA100");
					if (stateMachineTransitionChip7 != null && stateMachineTransitionChip7.GetInstanceId() == stableChipAId)
					{
						StateMachineTransitionChip stateMachineTransitionChip8 = surface.FindTransitionChip("LargeEdgeB100");
						if (stateMachineTransitionChip8 != null && stateMachineTransitionChip8.GetInstanceId() == stableChipBId)
						{
							num6 = ((expandElapsed < 2500) ? 1 : 0);
							goto IL_12c8;
						}
					}
				}
			}
		}
		num6 = 0;
		goto IL_12c8;
		IL_0785:
		int num7;
		bool mutationReused = (byte)num7 != 0;
		Action onControllerChanged;
		surface.GraphController.Changed -= onControllerChanged;
		Action onDefinitionChanged;
		surface.DefinitionChanged -= onDefinitionChanged;
		Action onLayoutChanged;
		surface.LayoutChanged -= onLayoutChanged;
		bool graphIncremental = graphComplete && explicitRefreshReused && mutationReused;
		ulong viewportSettledElapsed = 0uL;
		if (stableA != null)
		{
			ulong viewportStarted = Time.GetTicksMsec();
			EmitViewportGesture(surface, stableA.PositionOffset - new Vector2(240f, 180f), surface.Zoom);
			await WaitFrames(2);
			viewportSettledElapsed = Time.GetTicksMsec() - viewportStarted;
		}
		foreach (StringName selectedStateNode in surface.GetSelectedStateNodes())
		{
			StateMachineGraphNode stateMachineGraphNode8 = surface.FindStateNode(selectedStateNode.ToString());
			if (stateMachineGraphNode8 != null)
			{
				stateMachineGraphNode8.Selected = false;
			}
		}
		if (stableA != null)
		{
			stableA.Selected = true;
		}
		StateMachineTransitionChip incomingA = surface.FindTransitionChip("LargeEdgeA99");
		StateMachineTransitionChip outgoingA = surface.FindTransitionChip("LargeEdgeA100");
		StateMachineTransitionChip incomingB = surface.FindTransitionChip("LargeEdgeB98");
		StateMachineTransitionChip outgoingB = surface.FindTransitionChip("LargeEdgeB100");
		StateMachineTransitionChip unrelated = surface.FindTransitionChip("LargeEdgeA300");
		Vector2 incomingABefore = incomingA?.Position ?? Vector2.Zero;
		Vector2 outgoingABefore = outgoingA?.Position ?? Vector2.Zero;
		Vector2 incomingBBefore = incomingB?.Position ?? Vector2.Zero;
		Vector2 outgoingBBefore = outgoingB?.Position ?? Vector2.Zero;
		Vector2 unrelatedBefore = unrelated?.Position ?? Vector2.Zero;
		Vector2 vector = stableA?.PositionOffset ?? Vector2.Zero;
		surface.EmitSignal(GraphEdit.SignalName.BeginNodeMove);
		if (stableA != null)
		{
			stableA.PositionOffset = vector + new Vector2(64f, 40f);
		}
		await WaitFrames(2);
		int relayoutDuringMove = surface.LastTransitionChipRelayoutCount;
		bool incomingAMoved = incomingA != null && !incomingA.Position.IsEqualApprox(incomingABefore);
		bool outgoingAMoved = outgoingA != null && !outgoingA.Position.IsEqualApprox(outgoingABefore);
		bool incomingBMoved = incomingB != null && !incomingB.Position.IsEqualApprox(incomingBBefore);
		bool outgoingBMoved = outgoingB != null && !outgoingB.Position.IsEqualApprox(outgoingBBefore);
		bool unrelatedStayed = unrelated?.Position.IsEqualApprox(unrelatedBefore) ?? false;
		if (stableA != null && incomingA != null && outgoingA != null && incomingB != null && outgoingB != null && unrelated != null && relayoutDuringMove == 4 && incomingAMoved && outgoingAMoved && incomingBMoved && outgoingBMoved && unrelatedStayed)
		{
			StateMachineTransitionChip stateMachineTransitionChip9 = surface.FindTransitionChip("LargeEdgeA100");
			if (stateMachineTransitionChip9 != null && stateMachineTransitionChip9.GetInstanceId() == stableChipAId)
			{
				StateMachineTransitionChip stateMachineTransitionChip10 = surface.FindTransitionChip("LargeEdgeB100");
				num3 = ((stateMachineTransitionChip10 != null && stateMachineTransitionChip10.GetInstanceId() == stableChipBId) ? 1 : 0);
				goto IL_0c5f;
			}
		}
		num3 = 0;
		goto IL_0c5f;
		IL_0470:
		bool explicitChipRefreshReused = (byte)num2 != 0;
		int controllerChanges = 0;
		int definitionChanges = 0;
		int layoutChanges = 0;
		onControllerChanged = delegate
		{
			controllerChanges++;
		};
		onDefinitionChanged = delegate
		{
			definitionChanges++;
		};
		onLayoutChanged = delegate
		{
			layoutChanges++;
		};
		surface.GraphController.Changed += onControllerChanged;
		surface.DefinitionChanged += onDefinitionChanged;
		surface.LayoutChanged += onLayoutChanged;
		int incrementalBeforeMutation = surface.IncrementalGraphRefreshCount;
		ulong frameBefore = Engine.GetProcessFrames();
		ulong ticksMsec = Time.GetTicksMsec();
		(surface.FindStateNode("LargeState250")?.FindChild("InitialButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		ulong mutationElapsed = Time.GetTicksMsec() - ticksMsec;
		await WaitFrames(4);
		if (FindState(definition, "Root")?.InitialChildId == "LargeState250" && controllerChanges == 1 && definitionChanges == 1 && layoutChanges == 0 && surface.FullGraphRebuildCount == fullBefore && surface.IncrementalGraphRefreshCount == incrementalBeforeMutation + 1 && surface.GraphNodeCreateCount == createdBefore)
		{
			StateMachineGraphNode stateMachineGraphNode9 = surface.FindStateNode("LargeState100");
			if (stateMachineGraphNode9 != null && stateMachineGraphNode9.GetInstanceId() == stableAId)
			{
				StateMachineGraphNode stateMachineGraphNode10 = surface.FindStateNode("LargeState498");
				if (stateMachineGraphNode10 != null && stateMachineGraphNode10.GetInstanceId() == stableBId && surface.TransitionChipCount == 800 && surface.VisibleTransitionChipCount == 800 && surface.TransitionChipCreateCount == chipCreatedBefore)
				{
					StateMachineTransitionChip stateMachineTransitionChip11 = surface.FindTransitionChip("LargeEdgeA100");
					if (stateMachineTransitionChip11 != null && stateMachineTransitionChip11.GetInstanceId() == stableChipAId)
					{
						StateMachineTransitionChip stateMachineTransitionChip12 = surface.FindTransitionChip("LargeEdgeB100");
						if (stateMachineTransitionChip12 != null && stateMachineTransitionChip12.GetInstanceId() == stableChipBId && mutationElapsed < 2500)
						{
							num7 = ((Engine.GetProcessFrames() >= frameBefore + 3) ? 1 : 0);
							goto IL_0785;
						}
					}
				}
			}
		}
		num7 = 0;
		goto IL_0785;
		IL_12c8:
		bool flag = (byte)num6 != 0;
		bool flag2 = collapsedIncrementally && flag;
		bool flag3 = openSettledElapsed < 8000 && refreshSettledElapsed < 6000 && viewportSettledElapsed < 4000 && collapseSettledElapsed < 6000 && num5 < 6000;
		bool flag4 = graphComplete && explicitChipRefreshReused && mutationReused && collapsedIncrementally && flag && flag3;
		if (!(graphIncremental && flag2 && flag4 && transitionChipMoveScope))
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_LARGE_DIAGNOSTIC] graph={graphComplete} explicitReuse={explicitRefreshReused} mutationReuse={mutationReused} chipReuse={explicitChipRefreshReused}/{flag4} chipMoveScope={transitionChipMoveScope} relayout={relayoutDuringMove}/{relayoutAfterMoveCommit} reason={relayoutAfterMoveReason} isolated={adjacentCommitLayout}/{coalescedViewportLayout} moved={incomingAMoved}/{outgoingAMoved}/{incomingBMoved}/{outgoingBMoved} unrelatedStayed={unrelatedStayed} commitReused={moveCommitReused} collapse={collapsedIncrementally}/{flag} elapsedMs={mutationElapsed}/{collapseElapsed}/{expandElapsed} settledMs={openSettledElapsed}/{refreshSettledElapsed}/{viewportSettledElapsed}/{collapseSettledElapsed}/{num5} renderBudget={flag3} controller={controllerChanges} definition={definitionChanges} layout={layoutChanges} visible={surface.VisibleStateNodeCount}/{surface.VisibleConnectionCount} full={surface.FullGraphRebuildCount - fullBefore} incremental={surface.IncrementalGraphRefreshCount - incrementalBefore} created={surface.GraphNodeCreateCount - createdBefore} chipCreated={surface.TransitionChipCreateCount - chipCreatedBefore}");
		}
		GD.Print($"[MOD_EDITOR_STATE_MACHINE_LARGE_TRANSITION_CHIP_PROBE] count={surface.TransitionChipCount}/{surface.VisibleTransitionChipCount} refreshReuse={explicitChipRefreshReused} collapseReuse={collapsedIncrementally && flag} moveScope={transitionChipMoveScope} relayout={relayoutDuringMove} renderBudget={flag3} settledMs={openSettledElapsed}/{refreshSettledElapsed}/{viewportSettledElapsed}/{collapseSettledElapsed}/{num5} createdDelta={surface.TransitionChipCreateCount - chipCreatedBefore}");
		return (graphIncremental: graphIncremental, collapseIncremental: flag2, transitionChipIncremental: flag4, transitionChipMoveScope: transitionChipMoveScope);
	}

	private async Task<bool> ProbeStateMachineSignalLifecycle()
	{
		StateMachineGraphEditorSurface surface = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
		if (surface == null)
		{
			return false;
		}
		SubViewport viewport = new SubViewport
		{
			Size = new Vector2I(900, 640)
		};
		AddChild(viewport, forceReadableName: false, InternalMode.Disabled);
		viewport.AddChild(surface, forceReadableName: false, InternalMode.Disabled);
		surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize);
		surface.Size = new Vector2(900f, 640f);
		StateMachineGraphController controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter undoAdapter = new InMemoryStateMachineUndoAdapter();
		surface.Bind(controller, undoAdapter);
		StateMachineDefinition definition = CreateSimulationDefinition();
		surface.LoadDefinition(definition, new StateMachineLayout());
		surface.NavigateToStableId("IdleToAttack");
		await WaitFrames(2);
		SpinBox spinBox = surface.FindChild("PrioritySpin", recursive: true, owned: false) as SpinBox;
		int priorityBefore = FindTransition(definition, "IdleToAttack")?.Priority ?? (-2147483648);
		if (spinBox != null)
		{
			spinBox.Value = priorityBefore + 7;
		}
		EmitViewportGesture(surface, new Vector2(132f, 84f), 1.1f);
		int layoutChanges = 0;
		Action value = delegate
		{
			layoutChanges++;
		};
		surface.LayoutChanged += value;
		StateMachineDetailsPanel detailsPanel = surface.DetailsPanel;
		bool timersArmed = detailsPanel != null && detailsPanel.ActiveNumericDebounceTimerCount == 1 && surface.ActiveViewportDebounceTimerCount == 1;
		viewport.RemoveChild(surface);
		surface.QueueFree();
		await WaitFrames(2);
		await ToSignal(GetTree().CreateTimer(0.3), SceneTreeTimer.SignalName.Timeout);
		await WaitFrames(1);
		int result;
		if (timersArmed)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = FindTransition(definition, "IdleToAttack");
			if (stateMachineTransitionDefinition != null && stateMachineTransitionDefinition.Priority == priorityBefore && layoutChanges == 1)
			{
				result = ((!GodotObject.IsInstanceValid(surface)) ? 1 : 0);
				goto IL_03fb;
			}
		}
		result = 0;
		goto IL_03fb;
		IL_03fb:
		RemoveChild(viewport);
		viewport.QueueFree();
		controller.Dispose();
		return (byte)result != 0;
	}

	private static void EmitViewportGesture(StateMachineGraphEditorSurface surface, Vector2 scrollOffset, float zoom)
	{
		if (surface != null)
		{
			surface.ScrollOffset = scrollOffset;
			surface.Zoom = zoom;
			surface.EmitSignal(GraphEdit.SignalName.ScrollOffsetChanged, scrollOffset);
		}
	}

	private static StateMachineLayout LoadLayoutSidecar(string definitionPath)
	{
		return ResourceLoader.Load<StateMachineLayout>(StateMachineLayoutStore.GetLayoutPath(definitionPath), "", ResourceLoader.CacheMode.Ignore);
	}

	private static bool ViewportMatches(StateMachineLayout layout, Vector2 scrollOffset, float zoom)
	{
		if (layout != null && layout.ScrollOffset.IsEqualApprox(scrollOffset))
		{
			return Math.Abs(layout.Zoom - zoom) < 0.001f;
		}
		return false;
	}

	private static void RemoveProbeResource(string path)
	{
		if (!string.IsNullOrWhiteSpace(path))
		{
			string path2 = ProjectSettings.GlobalizePath(path);
			if (File.Exists(path2))
			{
				DirAccess.RemoveAbsolute(path2);
			}
		}
	}

	private async Task<bool> ProbeEmptyDefinitionInitialization()
	{
		StateMachineGraphEditorSurface surface = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
		if (surface == null)
		{
			return false;
		}
		SubViewport viewport = new SubViewport
		{
			Size = new Vector2I(760, 520)
		};
		AddChild(viewport, forceReadableName: false, InternalMode.Disabled);
		viewport.AddChild(surface, forceReadableName: false, InternalMode.Disabled);
		surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize);
		surface.Size = new Vector2(760f, 520f);
		StateMachineGraphController controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		controller.BindUndoAdapter(new InMemoryStateMachineUndoAdapter());
		surface.Bind(controller, new InMemoryStateMachineUndoAdapter());
		StateMachineDefinition definition = new StateMachineDefinition();
		surface.LoadDefinition(definition, new StateMachineLayout());
		surface.SetWorkbenchActive(active: true);
		await WaitFrames(2);
		Button button = surface.FindChild("InitializeDefinitionButton", recursive: true, owned: false) as Button;
		bool offered = button != null && button.Visible && !button.Disabled;
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		StateMachineValidationResult stateMachineValidationResult = StateMachineValidator.Validate(definition);
		bool result = offered && definition.States.Count == 2 && !string.IsNullOrWhiteSpace(definition.RootStateId) && stateMachineValidationResult.IsValid;
		surface.SetWorkbenchActive(active: false);
		viewport.RemoveChild(surface);
		surface.QueueFree();
		RemoveChild(viewport);
		viewport.QueueFree();
		controller.Dispose();
		return result;
	}

	private async Task<bool> ProbeResponsiveSurface820(StateMachineDefinition definition)
	{
		StateMachineGraphEditorSurface surface = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
		if (surface == null)
		{
			return false;
		}
		SubViewport viewport = new SubViewport
		{
			Size = new Vector2I(820, 620)
		};
		AddChild(viewport, forceReadableName: false, InternalMode.Disabled);
		viewport.AddChild(surface, forceReadableName: false, InternalMode.Disabled);
		surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize);
		surface.Position = Vector2.Zero;
		surface.Size = new Vector2(820f, 620f);
		surface.CustomMinimumSize = Vector2.Zero;
		StateMachineGraphController controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter undoAdapter = new InMemoryStateMachineUndoAdapter();
		surface.Bind(controller, undoAdapter);
		surface.LoadDefinition(definition.Duplicate(deep: true) as StateMachineDefinition, new StateMachineLayout());
		await WaitFrames(3);
		HBoxContainer hBoxContainer = surface.Call("get_menu_hbox").As<HBoxContainer>();
		StateMachineDetailsPanel detailsPanel = surface.DetailsPanel;
		bool num = Math.Abs(surface.Size.X - 820f) <= 1f && detailsPanel != null && detailsPanel.Position.X >= 500f && detailsPanel.Position.X + detailsPanel.Size.X <= 821f && hBoxContainer != null && hBoxContainer.Size.X <= 820f && surface.FindChild("StateMachineMoreMenu", recursive: true, owned: false) is MenuButton && surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) is Button;
		if (!num)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_RESPONSIVE_DIAGNOSTIC] width={surface.Size.X} details={detailsPanel?.Position}/{detailsPanel?.Size} toolbar={hBoxContainer?.Size} more={surface.FindChild("StateMachineMoreMenu", recursive: true, owned: false) != null} add={surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) != null} wide={DescribeWidestControls(detailsPanel)}");
		}
		surface.SetWorkbenchActive(active: false);
		viewport.RemoveChild(surface);
		surface.QueueFree();
		RemoveChild(viewport);
		viewport.QueueFree();
		controller.Dispose();
		return num;
	}

	private async Task<(bool nativeEmpty, bool narrow420, bool toolbarInteraction, bool narrowDirectEdit, bool narrowKinds)> ProbeNativeEmptyAndNarrowSurface()
	{
		StateMachineGraphEditorSurface surface = ResourceLoader.Load<PackedScene>("res://addons/godot_state_charts/VisualEditor/StateMachineGraphEditorSurface.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<StateMachineGraphEditorSurface>(PackedScene.GenEditState.Disabled);
		if (surface == null)
		{
			return (nativeEmpty: false, narrow420: false, toolbarInteraction: false, narrowDirectEdit: false, narrowKinds: false);
		}
		SubViewport viewport = new SubViewport
		{
			Size = new Vector2I(420, 620)
		};
		AddChild(viewport, forceReadableName: false, InternalMode.Disabled);
		viewport.AddChild(surface, forceReadableName: false, InternalMode.Disabled);
		surface.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft, Control.LayoutPresetMode.Minsize);
		surface.Position = Vector2.Zero;
		surface.Size = new Vector2(420f, 620f);
		surface.CustomMinimumSize = Vector2.Zero;
		StateMachineGraphController controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter undoAdapter = new InMemoryStateMachineUndoAdapter();
		surface.Bind(controller, undoAdapter);
		int createRequests = 0;
		int openRequests = 0;
		surface.CreateDefinitionRequested += delegate
		{
			createRequests++;
		};
		surface.OpenDefinitionRequested += delegate
		{
			openRequests++;
		};
		await WaitFrames(3);
		Control control = surface.FindChild("StateMachineEmptyStatePanel", recursive: true, owned: false) as Control;
		Button button = surface.FindChild("CreateDefinitionButton", recursive: true, owned: false) as Button;
		Button button2 = surface.FindChild("OpenDefinitionButton", recursive: true, owned: false) as Button;
		HBoxContainer hBoxContainer = surface.FindChild("StateMachineCompactToolbar", recursive: true, owned: false) as HBoxContainer;
		StateMachineDetailsPanel details = surface.DetailsPanel;
		bool empty = control != null && control.Visible && button != null && button2 != null;
		int num;
		if (Math.Abs(surface.Size.X - 420f) <= 1f && hBoxContainer != null && hBoxContainer.Size.X <= 420f)
		{
			num = ((details != null && !details.Visible) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool fits = (byte)num != 0;
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		button2?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		bool interaction = createRequests == 1 && openRequests == 1;
		StateMachineDefinition narrowDefinition = CreateSimulationDefinition();
		surface.LoadDefinition(narrowDefinition, new StateMachineLayout());
		surface.NavigateToStableId("Idle");
		await WaitFrames(2);
		Button detailsToggle = surface.FindChild("ToggleStateDetailsButton", recursive: true, owned: false) as Button;
		LineEdit lineEdit = surface.FindChild("DisplayNameEdit", recursive: true, owned: false) as LineEdit;
		bool drawerOpened = (details?.Visible ?? false) && details.Position.X >= 0f && details.Position.X + details.Size.X <= 421f && details.Position.Y >= 200f && details.Position.Y + details.Size.Y <= 621f && (lineEdit?.Editable ?? false);
		if (!drawerOpened)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_NARROW_DRAWER_DIAGNOSTIC] visible={details?.Visible} position={details?.Position} size={details?.Size} name={lineEdit != null} editable={lineEdit?.Editable} wide={DescribeWidestControls(details)}");
		}
		if (lineEdit != null)
		{
			lineEdit.Text = "NarrowIdle";
			lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		}
		await WaitFrames(2);
		detailsToggle?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		bool drawerHidden = details != null && !details.Visible;
		detailsToggle?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(1);
		bool directEdit = drawerOpened && drawerHidden && (details?.Visible ?? false) && FindState(narrowDefinition, "Idle")?.DisplayName.ToString() == "NarrowIdle";
		MenuButton narrowAdd = surface.FindChild("NarrowAddStateMenu", recursive: true, owned: false) as MenuButton;
		Button wideAdd = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		HashSet<string> beforeKinds = CaptureStateIds(narrowDefinition);
		StateMachineStateKind[] array = new StateMachineStateKind[4]
		{
			StateMachineStateKind.Atomic,
			StateMachineStateKind.Compound,
			StateMachineStateKind.Parallel,
			StateMachineStateKind.History
		};
		foreach (StateMachineStateKind stateMachineStateKind in array)
		{
			narrowAdd?.GetPopup().EmitSignal(PopupMenu.SignalName.IdPressed, (long)stateMachineStateKind);
			await WaitFrames(1);
		}
		HashSet<StateMachineStateKind> hashSet = new HashSet<StateMachineStateKind>();
		foreach (StateMachineStateDefinition state in narrowDefinition.States)
		{
			if (state != null && !beforeKinds.Contains(state.StableId))
			{
				hashSet.Add(state.Kind);
			}
		}
		int num3;
		if (narrowAdd?.Visible ?? false)
		{
			if (wideAdd != null && !wideAdd.Visible && hashSet.Contains(StateMachineStateKind.Atomic) && hashSet.Contains(StateMachineStateKind.Compound) && hashSet.Contains(StateMachineStateKind.Parallel) && hashSet.Contains(StateMachineStateKind.History))
			{
				num3 = (StateMachineValidator.Validate(narrowDefinition).IsValid ? 1 : 0);
				goto IL_0a1e;
			}
		}
		num3 = 0;
		goto IL_0a1e;
		IL_0a1e:
		bool item = (byte)num3 != 0;
		surface.SetWorkbenchActive(active: false);
		viewport.RemoveChild(surface);
		surface.QueueFree();
		RemoveChild(viewport);
		viewport.QueueFree();
		controller.Dispose();
		return (nativeEmpty: empty, narrow420: fits, toolbarInteraction: interaction, narrowDirectEdit: directEdit, narrowKinds: item);
	}

	private static string DescribeWidestControls(Control root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return "<none>";
		}
		List<Control> controls = new List<Control>();
		Collect(root);
		return string.Join(",", from control in controls.OrderByDescending((Control control) => control.GetCombinedMinimumSize().X).Take(8)
			select $"{control.Name}:{control.GetCombinedMinimumSize().X:0.#}");
		void Collect(Node node)
		{
			foreach (Node child in node.GetChildren())
			{
				if (child is Control item)
				{
					controls.Add(item);
				}
				Collect(child);
			}
		}
	}

	private async Task<bool> ProbeResourcePaletteForeground()
	{
		XWResourceWorkspacePalette palette = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/GUI/XWResourceWorkspacePalette.tscn", null, ResourceLoader.CacheMode.Reuse)?.Instantiate<XWResourceWorkspacePalette>(PackedScene.GenEditState.Disabled);
		if (palette == null)
		{
			return false;
		}
		AddChild(palette, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(2);
		palette.OpenPalette();
		await WaitFrames(2);
		LineEdit search = palette.FindChild("SearchEdit", recursive: true, owned: false) as LineEdit;
		bool firstOpen = palette.Exclusive && palette.Visible && search != null;
		if (search != null)
		{
			search.Text = "state-machine-reopen-sentinel";
		}
		palette.OpenPalette();
		await WaitFrames(1);
		bool reopened = palette.Visible && search?.Text == "state-machine-reopen-sentinel";
		palette.Hide();
		RemoveChild(palette);
		palette.QueueFree();
		await WaitFrames(1);
		return firstOpen && reopened;
	}

	private async Task<bool> ProbeTopToolbarUndoRouting(XWStateMachineVisualResourceEditor editor, StateMachineDefinition definition)
	{
		StateMachineGraphEditorSurface surfaceBefore = editor?.GraphSurface;
		StateMachineGraphController controllerBefore = surfaceBefore?.GraphController;
		HBoxContainer owner = editor?.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button undo = FindButtonByText(owner, "撤销");
		Button redo = FindButtonByText(owner, "重做");
		if (surfaceBefore == null || controllerBefore == null || undo == null || redo == null)
		{
			return false;
		}
		string stableId = controllerBefore.AddState("ToolbarUndoProbe", StateMachineStateKind.Atomic, "Root");
		await WaitFrames(2);
		bool applied = !string.IsNullOrWhiteSpace(stableId) && FindState(definition, stableId) != null;
		undo.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		bool undone = FindState(definition, stableId) == null;
		bool sameAfterUndo = editor.GraphSurface == surfaceBefore && editor.GraphSurface?.GraphController == controllerBefore;
		redo.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		bool flag = FindState(definition, stableId) != null;
		bool flag2 = editor.GraphSurface == surfaceBefore && editor.GraphSurface?.GraphController == controllerBefore;
		return applied && undone && flag && sameAfterUndo && flag2;
	}

	private static Button FindButtonByText(Node owner, string text)
	{
		if (owner == null)
		{
			return null;
		}
		foreach (Node child in owner.GetChildren())
		{
			if (child is Button button && button.Text == text)
			{
				return button;
			}
		}
		return null;
	}

	private static bool ProbeTransitionChipNativeCurveAlignment(StateMachineGraphEditorSurface surface, out string diagnostic)
	{
		diagnostic = "surface";
		if (surface?.GraphController?.ViewModel == null)
		{
			return false;
		}
		StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = null;
		foreach (StateMachineGraphTransitionViewModel transition2 in surface.GraphController.ViewModel.Transitions)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = transition2?.Transition;
			if (stateMachineTransitionDefinition == null || string.Equals(stateMachineTransitionDefinition.SourceStateId, stateMachineTransitionDefinition.TargetStateId, StringComparison.Ordinal))
			{
				continue;
			}
			int num = 0;
			foreach (StateMachineGraphTransitionViewModel transition3 in surface.GraphController.ViewModel.Transitions)
			{
				if (transition3?.Transition?.SourceStateId == stateMachineTransitionDefinition.SourceStateId && transition3.Transition.TargetStateId == stateMachineTransitionDefinition.TargetStateId)
				{
					num++;
				}
			}
			if (num == 1)
			{
				stateMachineGraphTransitionViewModel = transition2;
				break;
			}
		}
		if (stateMachineGraphTransitionViewModel?.Transition == null)
		{
			diagnostic = "unique-pair";
			return false;
		}
		StateMachineTransitionDefinition transition = stateMachineGraphTransitionViewModel.Transition;
		StateMachineGraphNode stateMachineGraphNode = surface.FindStateNode(transition.SourceStateId);
		StateMachineGraphNode stateMachineGraphNode2 = surface.FindStateNode(transition.TargetStateId);
		StateMachineTransitionChip stateMachineTransitionChip = surface.FindTransitionChip(transition.StableId);
		if (stateMachineGraphNode == null || stateMachineGraphNode2 == null || stateMachineTransitionChip == null)
		{
			diagnostic = $"instances:{stateMachineGraphNode != null}/{stateMachineGraphNode2 != null}/{stateMachineTransitionChip != null}";
			return false;
		}
		int outputPortCount = stateMachineGraphNode.GetOutputPortCount();
		int inputPortCount = stateMachineGraphNode2.GetInputPortCount();
		Control nodeOrNull = stateMachineGraphNode.GetNodeOrNull<Control>("PuzzleTile");
		Control nodeOrNull2 = stateMachineGraphNode2.GetNodeOrNull<Control>("PuzzleTile");
		bool flag = GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Size.Y > 0f;
		bool flag2 = GodotObject.IsInstanceValid(nodeOrNull2) && nodeOrNull2.Size.Y > 0f;
		if ((outputPortCount <= 0 && !flag) || (inputPortCount <= 0 && !flag2))
		{
			diagnostic = $"ports:{outputPortCount}/{inputPortCount}:slots={flag}/{flag2}";
			return false;
		}
		Vector2 vector = ((outputPortCount > 0) ? stateMachineGraphNode.GetOutputPortPosition(0) : new Vector2(stateMachineGraphNode.Size.X, nodeOrNull.Position.Y + nodeOrNull.Size.Y * 0.5f));
		Vector2 vector2 = ((inputPortCount > 0) ? stateMachineGraphNode2.GetInputPortPosition(0) : new Vector2(0f, nodeOrNull2.Position.Y + nodeOrNull2.Size.Y * 0.5f));
		Vector2 vector3 = stateMachineGraphNode.PositionOffset + vector;
		Vector2 vector4 = stateMachineGraphNode2.PositionOffset + vector2;
		float num2 = Mathf.Abs(vector4.X - vector3.X);
		float num3 = Mathf.Max(24f, num2 * Mathf.Clamp(surface.ConnectionLinesCurvature, 0f, 1f));
		if (vector4.X < vector3.X)
		{
			num3 = Mathf.Max(num3, vector3.DistanceTo(vector4) * 0.5f);
		}
		Vector2 vector5 = vector3 + Vector2.Right * num3;
		Vector2 vector6 = vector4 - Vector2.Right * num3;
		Vector2 vector7 = vector3.BezierInterpolate(vector5, vector6, vector4, 0.52f);
		float num4 = 0.48000002f;
		Vector2 vector8 = 3f * num4 * num4 * (vector5 - vector3) + 6f * num4 * 0.52f * (vector6 - vector5) + 0.81119996f * (vector4 - vector6);
		Vector2 vector9 = ((vector8.LengthSquared() > 0.001f) ? new Vector2(0f - vector8.Y, vector8.X).Normalized() : Vector2.Up);
		float num5 = ((string.CompareOrdinal(transition.SourceStateId, transition.TargetStateId) <= 0) ? 1f : (-1f));
		Vector2 to = (vector7 - surface.ScrollOffset) * Mathf.Max(surface.Zoom, 0.001f) + vector9 * 17f * num5;
		Vector2 vector10 = ((stateMachineTransitionChip.Size.X >= 1f && stateMachineTransitionChip.Size.Y >= 1f) ? stateMachineTransitionChip.Size : stateMachineTransitionChip.CustomMinimumSize);
		Vector2 vector11 = stateMachineTransitionChip.Position + vector10 * 0.5f;
		float num6 = vector11.DistanceTo(to);
		diagnostic = $"{transition.StableId}:{num6:0.###}:{vector11.X:0.##},{vector11.Y:0.##}/{to.X:0.##},{to.Y:0.##}:ports={outputPortCount}/{inputPortCount}:slots={flag}/{flag2}";
		return num6 <= 2f;
	}

	private static Vector2 ResolveConnectionEndpointForProbe(StateMachineGraphNode node, bool output)
	{
		if (output && node.GetOutputPortCount() > 0)
		{
			return node.PositionOffset + node.GetOutputPortPosition(0);
		}
		if (!output && node.GetInputPortCount() > 0)
		{
			return node.PositionOffset + node.GetInputPortPosition(0);
		}
		Control nodeOrNull = node.GetNodeOrNull<Control>("PuzzleTile");
		float y = ((GodotObject.IsInstanceValid(nodeOrNull) && nodeOrNull.Size.Y > 0f) ? (nodeOrNull.Position.Y + nodeOrNull.Size.Y * 0.5f) : (node.Size.Y * 0.5f));
		return node.PositionOffset + new Vector2(output ? node.Size.X : 0f, y);
	}

	private static bool ProbeCurvedConnectionHit(StateMachineGraphEditorSurface surface, string sourceStableId, string targetStableId, string transitionStableId)
	{
		StateMachineGraphNode stateMachineGraphNode = surface?.FindStateNode(sourceStableId);
		StateMachineGraphNode stateMachineGraphNode2 = surface?.FindStateNode(targetStableId);
		if (stateMachineGraphNode == null || stateMachineGraphNode2 == null || string.IsNullOrWhiteSpace(transitionStableId))
		{
			return false;
		}
		stateMachineGraphNode.PositionOffset = new Vector2(760f, 120f);
		stateMachineGraphNode2.PositionOffset = new Vector2(100f, 500f);
		Vector2 vector = ResolveConnectionEndpointForProbe(stateMachineGraphNode, output: true);
		Vector2 vector2 = ResolveConnectionEndpointForProbe(stateMachineGraphNode2, output: false);
		float connectionLinesCurvature = surface.ConnectionLinesCurvature;
		float num = Mathf.Abs(vector2.X - vector.X);
		float num2 = Mathf.Max(24f, num * Mathf.Clamp(connectionLinesCurvature, 0f, 1f));
		if (vector2.X < vector.X)
		{
			num2 = Mathf.Max(num2, vector.DistanceTo(vector2) * 0.5f);
		}
		Vector2 control = vector + Vector2.Right * num2;
		Vector2 control2 = vector2 - Vector2.Right * num2;
		Vector2 point = vector.BezierInterpolate(control, control2, vector2, 0.25f);
		Vector2 point2 = vector.BezierInterpolate(control, control2, vector2, 0.75f);
		bool flag = DistanceToSegmentForProbe(point, vector, vector2) > 24f && DistanceToSegmentForProbe(point2, vector, vector2) > 24f;
		bool flag2 = true;
		float[] array = new float[3] { 0.25f, 0.5f, 0.75f };
		foreach (float t in array)
		{
			Vector2 position = (vector.BezierInterpolate(control, control2, vector2, t) - surface.ScrollOffset) * surface.Zoom;
			surface.EmitSignal(Control.SignalName.GuiInput, new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				Pressed = true,
				Position = position
			});
			flag2 &= surface.ActiveDetailsKind == "Transition" && surface.ActiveDetailsStableId == transitionStableId;
		}
		return flag && flag2;
	}

	private static float DistanceToSegmentForProbe(Vector2 point, Vector2 from, Vector2 to)
	{
		Vector2 vector = to - from;
		float num = vector.LengthSquared();
		if (num <= 0.001f)
		{
			return point.DistanceTo(from);
		}
		float num2 = Mathf.Clamp((point - from).Dot(vector) / num, 0f, 1f);
		return point.DistanceTo(from + vector * num2);
	}

	private static (bool cycleRejected, bool rejectedClean, bool hierarchyGuards) ProbeHierarchyEditGuards(StateMachineGraphEditorSurface mountedSurface, string mountedDescendantId)
	{
		mountedSurface?.NavigateToStableId("Root");
		OptionButton optionButton = mountedSurface?.FindChild("ParentIdOption", recursive: true, owned: false) as OptionButton;
		bool flag = false;
		if (optionButton != null)
		{
			for (int i = 0; i < optionButton.ItemCount; i++)
			{
				if (optionButton.GetItemText(i).Contains(mountedDescendantId, StringComparison.Ordinal))
				{
					flag = optionButton.IsItemDisabled(i);
					break;
				}
			}
		}
		StateMachineDefinition stateMachineDefinition = CreateHierarchyGuardDefinition();
		StateMachineGraphController controller = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter undo = new InMemoryStateMachineUndoAdapter();
		controller.BindUndoAdapter(undo);
		controller.LoadDefinition(stateMachineDefinition, new StateMachineLayout());
		int controllerChanges = 0;
		int resourceChanges = 0;
		controller.Changed += delegate
		{
			controllerChanges++;
		};
		stateMachineDefinition.Changed += delegate
		{
			resourceChanges++;
		};
		bool flag2 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyBranch", "HierarchyNestedContainer");
		});
		bool flag3 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyRoot", "HierarchyBranch");
		});
		bool flag4 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyLeaf", string.Empty);
		});
		bool flag5 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyLeaf", "HierarchyAtomicParent");
		});
		bool flag6 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyLeaf", "HierarchyHistoryParent");
		});
		bool flag7 = RejectCleanly(delegate
		{
			controller.SetStateParent("HierarchyHistory", "HierarchyParallel");
		});
		bool item = flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && !undo.CanUndo && controllerChanges == 0 && resourceChanges == 0;
		controller.SetStateParent("HierarchyMovable", "HierarchyRoot");
		bool flag8 = FindState(stateMachineDefinition, "HierarchyMovable")?.ParentId == "HierarchyRoot";
		controller.Undo();
		bool flag9 = FindState(stateMachineDefinition, "HierarchyMovable")?.ParentId == "HierarchyBranch";
		controller.Redo();
		bool flag10 = FindState(stateMachineDefinition, "HierarchyMovable")?.ParentId == "HierarchyRoot";
		bool item2 = flag3 && flag4 && flag5 && flag6 && flag7 && StateMachineValidator.Validate(stateMachineDefinition).IsValid;
		controller.Dispose();
		return (cycleRejected: flag && flag2 && flag8 && flag9 && flag10, rejectedClean: item, hierarchyGuards: item2);
		bool RejectCleanly(Action edit)
		{
			int num = controllerChanges;
			int num2 = resourceChanges;
			try
			{
				edit();
				return false;
			}
			catch (Exception ex) when (((ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
			{
				return !undo.CanUndo && controllerChanges == num && resourceChanges == num2;
			}
		}
	}

	private static bool ProbeStateKindInvariants()
	{
		bool flag = ProbeConversion(CreateHierarchyDefinition(), "HierarchyBranch", StateMachineStateKind.Parallel, (StateMachineDefinition definition) => string.IsNullOrWhiteSpace(FindState(definition, "HierarchyBranch")?.InitialChildId));
		bool flag2 = ProbeConversion(CreateParallelHierarchyDefinition(), "HierarchyParallel", StateMachineStateKind.Compound, (StateMachineDefinition definition) => FindState(definition, "HierarchyParallel")?.InitialChildId == "HierarchyRegion");
		bool flag3 = ProbeConversion(CreateHierarchyDefinition(), "HierarchyLeaf", StateMachineStateKind.Compound, (StateMachineDefinition definition) => FindState(definition, FindState(definition, "HierarchyLeaf")?.InitialChildId)?.ParentId == "HierarchyLeaf");
		bool flag4 = ProbeConversion(CreateHierarchyDefinition(), "HierarchyLeaf", StateMachineStateKind.Parallel, (StateMachineDefinition definition) => definition.States.Any((StateMachineStateDefinition state) => state?.ParentId == "HierarchyLeaf" && state.Kind != StateMachineStateKind.History));
		StateMachineDefinition stateMachineDefinition = CreateHierarchyDefinition();
		StateMachineGraphController stateMachineGraphController = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter inMemoryStateMachineUndoAdapter = new InMemoryStateMachineUndoAdapter();
		stateMachineGraphController.BindUndoAdapter(inMemoryStateMachineUndoAdapter);
		stateMachineGraphController.LoadDefinition(stateMachineDefinition, new StateMachineLayout());
		int controllerChanges = 0;
		int resourceChanges = 0;
		stateMachineGraphController.Changed += delegate
		{
			controllerChanges++;
		};
		stateMachineDefinition.Changed += delegate
		{
			resourceChanges++;
		};
		bool flag5 = false;
		try
		{
			stateMachineGraphController.SetStateKind("HierarchyBranch", StateMachineStateKind.Atomic);
		}
		catch (InvalidOperationException)
		{
			int num;
			if (!inMemoryStateMachineUndoAdapter.CanUndo && controllerChanges == 0 && resourceChanges == 0)
			{
				StateMachineStateDefinition stateMachineStateDefinition = FindState(stateMachineDefinition, "HierarchyBranch");
				if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind == StateMachineStateKind.Compound)
				{
					num = (StateMachineValidator.Validate(stateMachineDefinition).IsValid ? 1 : 0);
					goto IL_0185;
				}
			}
			num = 0;
			goto IL_0185;
			IL_0185:
			flag5 = (byte)num != 0;
		}
		stateMachineGraphController.Dispose();
		return flag && flag2 && flag3 && flag4 && flag5;
		static bool ProbeConversion(StateMachineDefinition definition, string stableId, StateMachineStateKind nextKind, Func<StateMachineDefinition, bool> appliedInvariant)
		{
			StateMachineGraphController stateMachineGraphController2 = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
			InMemoryStateMachineUndoAdapter inMemoryStateMachineUndoAdapter2 = new InMemoryStateMachineUndoAdapter();
			stateMachineGraphController2.BindUndoAdapter(inMemoryStateMachineUndoAdapter2);
			stateMachineGraphController2.LoadDefinition(definition, new StateMachineLayout());
			StateMachineStateKind stateMachineStateKind = FindState(definition, stableId)?.Kind ?? StateMachineStateKind.Atomic;
			stateMachineGraphController2.SetStateKind(stableId, nextKind);
			StateMachineStateDefinition stateMachineStateDefinition2 = FindState(definition, stableId);
			bool flag6 = stateMachineStateDefinition2 != null && stateMachineStateDefinition2.Kind == nextKind && StateMachineValidator.Validate(definition).IsValid && appliedInvariant(definition) && inMemoryStateMachineUndoAdapter2.CanUndo;
			stateMachineGraphController2.Undo();
			StateMachineStateDefinition stateMachineStateDefinition3 = FindState(definition, stableId);
			bool flag7 = stateMachineStateDefinition3 != null && stateMachineStateDefinition3.Kind == stateMachineStateKind && StateMachineValidator.Validate(definition).IsValid;
			stateMachineGraphController2.Redo();
			StateMachineStateDefinition stateMachineStateDefinition4 = FindState(definition, stableId);
			bool flag8 = stateMachineStateDefinition4 != null && stateMachineStateDefinition4.Kind == nextKind && StateMachineValidator.Validate(definition).IsValid && appliedInvariant(definition);
			stateMachineGraphController2.Dispose();
			return flag6 && flag7 && flag8;
		}
	}

	private static bool ProbeRootSwitchInvariant()
	{
		StateMachineDefinition stateMachineDefinition = CreateRootSwitchDefinition();
		StateMachineGraphController stateMachineGraphController = new StateMachineGraphController(new GuidStateMachineStableIdProvider());
		InMemoryStateMachineUndoAdapter inMemoryStateMachineUndoAdapter = new InMemoryStateMachineUndoAdapter();
		stateMachineGraphController.BindUndoAdapter(inMemoryStateMachineUndoAdapter);
		stateMachineGraphController.LoadDefinition(stateMachineDefinition, new StateMachineLayout());
		stateMachineGraphController.SetRootState("NextRoot");
		int num;
		if (stateMachineDefinition.RootStateId == "NextRoot")
		{
			StateMachineStateDefinition stateMachineStateDefinition = FindState(stateMachineDefinition, "NextRoot");
			if (stateMachineStateDefinition != null && stateMachineStateDefinition.Kind == StateMachineStateKind.Compound && string.IsNullOrWhiteSpace(FindState(stateMachineDefinition, "NextRoot")?.ParentId) && FindState(stateMachineDefinition, "OldRoot")?.ParentId == "NextRoot" && FindState(stateMachineDefinition, "OldRoot")?.InitialChildId == "RootSibling" && StateMachineValidator.Validate(stateMachineDefinition).IsValid)
			{
				num = (inMemoryStateMachineUndoAdapter.CanUndo ? 1 : 0);
				goto IL_00e0;
			}
		}
		num = 0;
		goto IL_00e0;
		IL_00e0:
		bool flag = (byte)num != 0;
		stateMachineGraphController.Undo();
		int num2;
		if (stateMachineDefinition.RootStateId == "OldRoot")
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = FindState(stateMachineDefinition, "NextRoot");
			if (stateMachineStateDefinition2 != null && stateMachineStateDefinition2.Kind == StateMachineStateKind.Atomic && FindState(stateMachineDefinition, "NextRoot")?.ParentId == "OldRoot" && FindState(stateMachineDefinition, "OldRoot")?.InitialChildId == "NextRoot" && StateMachineValidator.Validate(stateMachineDefinition).IsValid)
			{
				num2 = (inMemoryStateMachineUndoAdapter.CanRedo ? 1 : 0);
				goto IL_0171;
			}
		}
		num2 = 0;
		goto IL_0171;
		IL_0171:
		bool flag2 = (byte)num2 != 0;
		stateMachineGraphController.Redo();
		int num3;
		if (stateMachineDefinition.RootStateId == "NextRoot")
		{
			StateMachineStateDefinition stateMachineStateDefinition3 = FindState(stateMachineDefinition, "NextRoot");
			if (stateMachineStateDefinition3 != null && stateMachineStateDefinition3.Kind == StateMachineStateKind.Compound && string.IsNullOrWhiteSpace(FindState(stateMachineDefinition, "NextRoot")?.ParentId) && FindState(stateMachineDefinition, "OldRoot")?.ParentId == "NextRoot")
			{
				num3 = (StateMachineValidator.Validate(stateMachineDefinition).IsValid ? 1 : 0);
				goto IL_01f5;
			}
		}
		num3 = 0;
		goto IL_01f5;
		IL_01f5:
		bool flag3 = (byte)num3 != 0;
		stateMachineGraphController.Dispose();
		return flag && flag2 && flag3;
	}

	private async Task<bool> ProbeInheritedOverrideWorkbench(XWStateMachineVisualResourceEditor editor)
	{
		StateMachineDefinition baseDefinition = CreateSimulationDefinition();
		StateMachineDefinition derived = new StateMachineDefinition
		{
			DefinitionId = "derived-override-workbench-probe",
			BaseDefinition = baseDefinition
		};
		if (!XWResourceEditorRegistry.TryGetEditor(derived, "user://mod_editor_state_machine_inherited_override_restored.tres", out var descriptor))
		{
			return false;
		}
		editor.LoadResource(derived, "user://mod_editor_state_machine_inherited_override_restored.tres", descriptor);
		await WaitFrames(3);
		StateMachineGraphEditorSurface surface = editor?.GraphSurface;
		surface?.SetWorkbenchActive(active: true);
		if (surface?.GraphController?.ViewModel == null)
		{
			return false;
		}
		surface.NavigateToStableId("Idle");
		await WaitFrames(2);
		Button button = surface.FindChild("CreateStateOverrideButton", recursive: true, owned: false) as Button;
		bool stateInitiallyInherited = IsInheritedState(surface, "Idle") && !IsLocalOverrideState(surface, "Idle") && FindState(derived, "Idle") == null && button != null && !button.Disabled;
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		LineEdit lineEdit = surface.FindChild("DisplayNameEdit", recursive: true, owned: false) as LineEdit;
		Button restoreState = surface.FindChild("RestoreInheritedStateButton", recursive: true, owned: false) as Button;
		int num;
		if (FindState(derived, "Idle") != null && !IsInheritedState(surface, "Idle") && IsLocalOverrideState(surface, "Idle") && lineEdit != null && lineEdit.Editable)
		{
			Button button2 = restoreState;
			num = ((button2 != null && !button2.Disabled) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool stateOverrideCreated = (byte)num != 0;
		if (lineEdit != null)
		{
			lineEdit.Text = "DerivedIdleOverride";
			lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit.Text);
		}
		await WaitFrames(2);
		bool stateEdited = FindState(derived, "Idle")?.DisplayName.ToString() == "DerivedIdleOverride";
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool stateEditUndone = FindState(derived, "Idle")?.DisplayName.ToString() == "Idle" && IsLocalOverrideState(surface, "Idle");
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		bool stateEditRedone = FindState(derived, "Idle")?.DisplayName.ToString() == "DerivedIdleOverride" && IsLocalOverrideState(surface, "Idle");
		surface.NavigateToStableId("IdleToAttack");
		await WaitFrames(2);
		Button button3 = surface.FindChild("CreateTransitionOverrideButton", recursive: true, owned: false) as Button;
		bool transitionInitiallyInherited = IsInheritedTransition(surface, "IdleToAttack") && !IsLocalOverrideTransition(surface, "IdleToAttack") && FindTransition(derived, "IdleToAttack") == null && button3 != null && !button3.Disabled;
		button3?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		LineEdit lineEdit2 = surface.FindChild("EventNameEdit", recursive: true, owned: false) as LineEdit;
		Button restoreTransition = surface.FindChild("RestoreInheritedTransitionButton", recursive: true, owned: false) as Button;
		int num2;
		if (FindTransition(derived, "IdleToAttack") != null && !IsInheritedTransition(surface, "IdleToAttack") && IsLocalOverrideTransition(surface, "IdleToAttack") && lineEdit2 != null && lineEdit2.Editable)
		{
			Button button4 = restoreTransition;
			num2 = ((button4 != null && !button4.Disabled) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool transitionOverrideCreated = (byte)num2 != 0;
		if (lineEdit2 != null)
		{
			lineEdit2.Text = "DerivedAttackOverride";
			lineEdit2.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit2.Text);
		}
		await WaitFrames(2);
		bool transitionEdited = FindTransition(derived, "IdleToAttack")?.EventName.ToString() == "DerivedAttackOverride";
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool transitionEditUndone = FindTransition(derived, "IdleToAttack")?.EventName.ToString() == "ToAttack" && IsLocalOverrideTransition(surface, "IdleToAttack");
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		bool transitionEditRedone = FindTransition(derived, "IdleToAttack")?.EventName.ToString() == "DerivedAttackOverride" && IsLocalOverrideTransition(surface, "IdleToAttack");
		Error checkpointSave = ResourceSaver.Save(derived, "user://mod_editor_state_machine_inherited_override_checkpoint.tres", ResourceSaver.SaverFlags.None);
		StateMachineDefinition stateMachineDefinition = ((checkpointSave == Error.Ok) ? ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_inherited_override_checkpoint.tres", "", ResourceLoader.CacheMode.Ignore) : null);
		bool overrideReloaded = stateMachineDefinition?.BaseDefinition != null && FindState(stateMachineDefinition, "Idle")?.DisplayName.ToString() == "DerivedIdleOverride" && FindTransition(stateMachineDefinition, "IdleToAttack")?.EventName.ToString() == "DerivedAttackOverride";
		surface.NavigateToStableId("Idle");
		await WaitFrames(2);
		restoreState = surface.FindChild("RestoreInheritedStateButton", recursive: true, owned: false) as Button;
		restoreState?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool stateRestored = restoreState != null && FindState(derived, "Idle") == null && IsInheritedState(surface, "Idle") && !IsLocalOverrideState(surface, "Idle");
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool stateRestoreUndone = FindState(derived, "Idle")?.DisplayName.ToString() == "DerivedIdleOverride" && IsLocalOverrideState(surface, "Idle");
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		bool stateRestoreRedone = FindState(derived, "Idle") == null && IsInheritedState(surface, "Idle") && !IsLocalOverrideState(surface, "Idle");
		surface.NavigateToStableId("IdleToAttack");
		await WaitFrames(2);
		restoreTransition = surface.FindChild("RestoreInheritedTransitionButton", recursive: true, owned: false) as Button;
		restoreTransition?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		bool transitionRestored = restoreTransition != null && FindTransition(derived, "IdleToAttack") == null && IsInheritedTransition(surface, "IdleToAttack") && !IsLocalOverrideTransition(surface, "IdleToAttack");
		EmitSurfaceShortcut(surface, Key.Z);
		await WaitFrames(2);
		bool transitionRestoreUndone = FindTransition(derived, "IdleToAttack")?.EventName.ToString() == "DerivedAttackOverride" && IsLocalOverrideTransition(surface, "IdleToAttack");
		EmitSurfaceShortcut(surface, Key.Y);
		await WaitFrames(2);
		bool flag = FindTransition(derived, "IdleToAttack") == null && IsInheritedTransition(surface, "IdleToAttack") && !IsLocalOverrideTransition(surface, "IdleToAttack");
		Error error = ResourceSaver.Save(derived, "user://mod_editor_state_machine_inherited_override_restored.tres", ResourceSaver.SaverFlags.None);
		StateMachineDefinition stateMachineDefinition2 = ((error == Error.Ok) ? ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_inherited_override_restored.tres", "", ResourceLoader.CacheMode.Ignore) : null);
		StateMachineDefinition composed;
		StateMachineValidationResult validation;
		bool flag2 = stateMachineDefinition2?.BaseDefinition != null && FindState(stateMachineDefinition2, "Idle") == null && FindTransition(stateMachineDefinition2, "IdleToAttack") == null && StateMachineDefinitionComposer.TryCompose(stateMachineDefinition2, out composed, out validation) && validation.IsValid && FindState(composed, "Idle")?.DisplayName.ToString() == "Idle" && FindTransition(composed, "IdleToAttack")?.EventName.ToString() == "ToAttack";
		surface.SetWorkbenchActive(active: false);
		bool num3 = stateInitiallyInherited && stateOverrideCreated && stateEdited && stateEditUndone && stateEditRedone && transitionInitiallyInherited && transitionOverrideCreated && transitionEdited && transitionEditUndone && transitionEditRedone && overrideReloaded && stateRestored && stateRestoreUndone && stateRestoreRedone && transitionRestored && transitionRestoreUndone && flag && flag2;
		if (!num3)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_INHERITED_OVERRIDE_DIAGNOSTIC] stateInitial={stateInitiallyInherited} stateCreate={stateOverrideCreated} stateEdit={stateEdited}/{stateEditUndone}/{stateEditRedone} transitionInitial={transitionInitiallyInherited} transitionCreate={transitionOverrideCreated} transitionEdit={transitionEdited}/{transitionEditUndone}/{transitionEditRedone} checkpoint={checkpointSave}/{overrideReloaded} stateRestore={stateRestored}/{stateRestoreUndone}/{stateRestoreRedone} transitionRestore={transitionRestored}/{transitionRestoreUndone}/{flag} restored={error}/{flag2}");
		}
		return num3;
	}

	private static bool IsInheritedState(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null)
		{
			return false;
		}
		return surface.GraphController?.ViewModel?.Nodes.FirstOrDefault((StateMachineNodeViewModel node) => node?.StableId == stableId)?.IsInherited == true;
	}

	private static bool IsLocalOverrideState(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null)
		{
			return false;
		}
		return surface.GraphController?.ViewModel?.Nodes.FirstOrDefault((StateMachineNodeViewModel node) => node?.StableId == stableId)?.IsLocalOverride == true;
	}

	private static bool IsInheritedTransition(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null)
		{
			return false;
		}
		return surface.GraphController?.ViewModel?.Transitions.FirstOrDefault((StateMachineGraphTransitionViewModel transition) => transition?.StableId == stableId)?.IsInherited == true;
	}

	private static bool IsLocalOverrideTransition(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null)
		{
			return false;
		}
		return surface.GraphController?.ViewModel?.Transitions.FirstOrDefault((StateMachineGraphTransitionViewModel transition) => transition?.StableId == stableId)?.IsLocalOverride == true;
	}

	private async Task<bool> ProbeDerivedTransitionOwnerBinding(XWStateMachineVisualResourceEditor editor)
	{
		StateMachineDefinition baseDefinition = CreateSimulationDefinition();
		StateMachineTransitionDefinition localOverride = new StateMachineTransitionDefinition
		{
			StableId = "IdleToAttack",
			SourceStateId = "Idle",
			TargetStateId = "Attack",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "DerivedAttack"
		};
		StateMachineDefinition derived = new StateMachineDefinition
		{
			DefinitionId = "derived-transition-owner-probe",
			BaseDefinition = baseDefinition
		};
		derived.Transitions.Add(localOverride);
		if (!XWResourceEditorRegistry.TryGetEditor(derived, "user://derived_transition_owner_probe.tres", out var descriptor))
		{
			return false;
		}
		editor.LoadResource(derived, "user://derived_transition_owner_probe.tres", descriptor);
		await WaitFrames(3);
		StateMachineTransitionDefinition stateMachineTransitionDefinition = new StateMachineTransitionDefinition
		{
			StableId = localOverride.StableId
		};
		StateMachineTransitionDefinition stateMachineTransitionDefinition2 = new StateMachineTransitionDefinition
		{
			StableId = "unknown-transition-owner"
		};
		MethodInfo method = typeof(XWStateMachineVisualResourceEditor).GetMethod("IsOwnerBoundToCurrentDefinition", BindingFlags.Instance | BindingFlags.NonPublic);
		object obj = method?.Invoke(editor, new object[1] { stateMachineTransitionDefinition });
		bool flag = obj is bool && (bool)obj;
		obj = method?.Invoke(editor, new object[1] { stateMachineTransitionDefinition2 });
		bool flag2 = obj is bool && !(bool)obj;
		return editor.ActiveResource == derived && flag && flag2;
	}

	private async Task<bool> ProbeDerivedDefinitionWorkbench(XWStateMachineVisualResourceEditor editor, XWInspector inspector, Node inspectorSentinel)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return false;
		}
		RemoveProbeResource("user://mod_editor_state_machine_derived_definition_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_definition_probe.tres"));
		ModEditorStateMachineProbeDerivedDefinition derived = new ModEditorStateMachineProbeDerivedDefinition
		{
			DefinitionId = "derived-definition-workbench-probe",
			RootStateId = "DerivedRoot",
			ModAuthorNote = "derived-before"
		};
		derived.States.Add(new StateMachineStateDefinition
		{
			StableId = "DerivedRoot",
			DisplayName = "Derived Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "DerivedIdle"
		});
		derived.States.Add(new StateMachineStateDefinition
		{
			StableId = "DerivedIdle",
			DisplayName = "Derived Idle",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "DerivedRoot"
		});
		bool initialSave = ResourceSaver.Save(derived, "user://mod_editor_state_machine_derived_definition_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok;
		XWVisualEditorDescriptor descriptor;
		bool routed = initialSave && XWResourceEditorRegistry.TryGetEditor(derived, "user://mod_editor_state_machine_derived_definition_probe.tres", out descriptor) && descriptor?.Category == "StateMachine" && descriptor.DockKey == "state_machine_editor" && XWResourceEditorRegistry.TryOpen(derived, "user://mod_editor_state_machine_derived_definition_probe.tres");
		await WaitFrames(6);
		StateMachineGraphEditorSurface graphSurface = editor.GraphSurface;
		XWDirectPropertySurface extensionSurface = editor.ExtensionPropertySurface;
		string[] array = new string[7] { "SchemaVersion", "DefinitionId", "BaseDefinition", "RootStateId", "States", "Transitions", "Aliases" };
		bool dedicatedWorkbench = routed && editor.ActiveResource == derived && graphSurface?.Definition == derived && graphSurface?.GraphController?.Definition == derived && GodotObject.IsInstanceValid(editor.WorkbenchTabs) && editor.WorkbenchTabs.Name == (StringName)"StateMachineWorkbenchTabs" && editor.WorkbenchTabs.GetChildCount() >= 2 && GodotObject.IsInstanceValid(extensionSurface) && extensionSurface.Name == (StringName)"StateMachineExtensionPropertySurface";
		bool directCoverage = GodotObject.IsInstanceValid(extensionSurface) && extensionSurface.Visible && extensionSurface.EditablePropertyNames.Contains("ModAuthorNote") && extensionSurface.SpecializedPropertyCount == array.Length && extensionSurface.MissingPropertyCount == 0 && extensionSurface.FindChild("DirectRow_ModAuthorNote", recursive: true, owned: false) != null && array.All((string property) => extensionSurface.FindChild("DirectRow_" + property, recursive: true, owned: false) == null);
		PanelContainer panelContainer = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer vBoxContainer = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		bool inspectorIsolatedBeforeEdit = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && (inspector == null || inspector.CurrentObject == inspectorSentinel);
		LineEdit authorNote = (extensionSurface?.FindChild("Direct_ModAuthorNote", recursive: true, owned: false))?.FindChild("LineEdit", recursive: true, owned: false) as LineEdit;
		HBoxContainer nodeOrNull = editor.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button button = FindButtonByText(nodeOrNull, "自动保存");
		if (GodotObject.IsInstanceValid(button) && !button.ButtonPressed)
		{
			button.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			nodeOrNull = editor.GetNodeOrNull<HBoxContainer>("%Toolbar");
			button = FindButtonByText(nodeOrNull, "自动保存");
		}
		FindButtonByText(nodeOrNull, "撤销");
		FindButtonByText(nodeOrNull, "重做");
		bool autosaveEnabled = GodotObject.IsInstanceValid(button) && button.ButtonPressed;
		if (GodotObject.IsInstanceValid(authorNote))
		{
			authorNote.EmitSignal(Control.SignalName.FocusEntered);
			authorNote.Text = "derived-after";
			authorNote.EmitSignal(LineEdit.SignalName.TextChanged, authorNote.Text);
			authorNote.EmitSignal(Control.SignalName.FocusExited);
		}
		await WaitFrames(5);
		bool edited = derived.ModAuthorNote == "derived-after";
		nodeOrNull = editor.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button button2 = FindButtonByText(nodeOrNull, "撤销");
		bool undoButtonAvailable = GodotObject.IsInstanceValid(button2);
		button2?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		LineEdit lineEdit = editor.ExtensionPropertySurface?.FindChild("Direct_ModAuthorNote", recursive: true, owned: false)?.FindChild("LineEdit", recursive: true, owned: false) as LineEdit;
		bool undone = derived.ModAuthorNote == "derived-before" && lineEdit?.Text == "derived-before";
		string text = typeof(XWGenericVisualResourceEditor).GetMethod("GetAutosaveId", BindingFlags.Instance | BindingFlags.NonPublic)?.Invoke(editor, null) as string;
		Resource resource = (string.IsNullOrWhiteSpace(text) ? null : new XWAutosaveManager().RecoverResourceDraft(text));
		bool undoDraftSynchronized = resource is ModEditorStateMachineProbeDerivedDefinition modEditorStateMachineProbeDerivedDefinition && modEditorStateMachineProbeDerivedDefinition.ModAuthorNote == "derived-before";
		nodeOrNull = editor.GetNodeOrNull<HBoxContainer>("%Toolbar");
		Button button3 = FindButtonByText(nodeOrNull, "重做");
		bool redoButtonAvailable = GodotObject.IsInstanceValid(button3);
		button3?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		LineEdit redoneAuthorNote = editor.ExtensionPropertySurface?.FindChild("Direct_ModAuthorNote", recursive: true, owned: false)?.FindChild("LineEdit", recursive: true, owned: false) as LineEdit;
		bool redone = derived.ModAuthorNote == "derived-after" && redoneAuthorNote?.Text == "derived-after";
		bool undoRedo = GodotObject.IsInstanceValid(redoneAuthorNote) && undoButtonAvailable && redoButtonAvailable && autosaveEnabled && edited && undone && redone && undoDraftSynchronized;
		editor.WorkbenchTabs.CurrentTab = 1;
		await WaitFrames(3);
		StateMachineGraphEditorSurface graphSurface2 = editor.GraphSurface;
		bool extensionTabQuiescent = GodotObject.IsInstanceValid(graphSurface2) && !graphSurface2.IsWorkbenchActive && graphSurface2.IsHiddenWorkQuiescent;
		editor.Hide();
		await WaitFrames(2);
		editor.Show();
		await WaitFrames(3);
		StateMachineGraphEditorSurface graphSurface3 = editor.GraphSurface;
		bool extensionTabRemainsQuiescent = editor.WorkbenchTabs.CurrentTab == 1 && GodotObject.IsInstanceValid(graphSurface3) && !graphSurface3.IsWorkbenchActive && graphSurface3.IsHiddenWorkQuiescent;
		editor.WorkbenchTabs.CurrentTab = 0;
		await WaitFrames(3);
		StateMachineGraphEditorSurface graphSurface4 = editor.GraphSurface;
		bool graphTabReactivated = GodotObject.IsInstanceValid(graphSurface4) && graphSurface4.IsWorkbenchActive;
		bool tabLifecycle = extensionTabQuiescent && extensionTabRemainsQuiescent && graphTabReactivated;
		bool saved = editor.SaveActiveResource();
		await WaitFrames(6);
		Resource resource2 = (saved ? ResourceLoader.Load<Resource>("user://mod_editor_state_machine_derived_definition_probe.tres", "", ResourceLoader.CacheMode.Ignore) : null);
		bool flag = resource2 is ModEditorStateMachineProbeDerivedDefinition { ModAuthorNote: "derived-after" } modEditorStateMachineProbeDerivedDefinition2 && modEditorStateMachineProbeDerivedDefinition2.DefinitionId == derived.DefinitionId && modEditorStateMachineProbeDerivedDefinition2.RootStateId == "DerivedRoot" && FindState(modEditorStateMachineProbeDerivedDefinition2, "DerivedIdle")?.ParentId == "DerivedRoot";
		panelContainer = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		vBoxContainer = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		bool flag2 = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && (inspector == null || inspector.CurrentObject == inspectorSentinel);
		bool num = initialSave && dedicatedWorkbench && directCoverage && inspectorIsolatedBeforeEdit && undoRedo && tabLifecycle && flag && flag2;
		if (!num)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_DERIVED_DEFINITION_DIAGNOSTIC] initialSave={initialSave} routed={routed} dedicated={dedicatedWorkbench} direct={directCoverage} editor={GodotObject.IsInstanceValid(redoneAuthorNote)} autosave={autosaveEnabled} edit={edited}/{undone}/{redone} draft={undoDraftSynchronized} tabs={extensionTabQuiescent}/{extensionTabRemainsQuiescent}/{graphTabReactivated} save={saved}/{flag} inspector={inspectorIsolatedBeforeEdit}/{flag2} loadedType={resource2?.GetType().Name ?? "<null>"}");
		}
		return num;
	}

	private async Task<bool> ProbeDerivedElementWorkbench(XWStateMachineVisualResourceEditor editor, XWInspector inspector, Node inspectorSentinel)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return false;
		}
		RemoveProbeResource("user://mod_editor_state_machine_derived_element_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_element_probe.tres"));
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "derived-element-base",
			RootStateId = "DerivedElementRoot"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "DerivedElementRoot",
			DisplayName = "Derived Element Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "DerivedA"
		});
		stateMachineDefinition.States.Add(new ModEditorStateMachineProbeDerivedState
		{
			StableId = "DerivedA",
			DisplayName = "Derived A",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "DerivedElementRoot",
			ModStateNote = "state-base",
			RuntimeWeight = 3
		});
		stateMachineDefinition.States.Add(new ModEditorStateMachineProbeDerivedState
		{
			StableId = "DerivedB",
			DisplayName = "Derived B",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "DerivedElementRoot",
			ModStateNote = "state-b",
			RuntimeWeight = 5
		});
		stateMachineDefinition.Transitions.Add(new ModEditorStateMachineProbeDerivedTransition
		{
			StableId = "DerivedAToB",
			SourceStateId = "DerivedA",
			TargetStateId = "DerivedB",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "derived_go",
			ModTransitionNote = "transition-base",
			RuntimeScale = 1.25
		});
		StateMachineDefinition derived = new StateMachineDefinition
		{
			DefinitionId = "derived-element-workbench",
			BaseDefinition = stateMachineDefinition
		};
		bool initialSave = ResourceSaver.Save(derived, "user://mod_editor_state_machine_derived_element_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok;
		XWVisualEditorDescriptor descriptor;
		bool routed = initialSave && XWResourceEditorRegistry.TryGetEditor(derived, "user://mod_editor_state_machine_derived_element_probe.tres", out descriptor) && XWResourceEditorRegistry.TryOpen(derived, "user://mod_editor_state_machine_derived_element_probe.tres");
		await WaitFrames(6);
		StateMachineGraphEditorSurface surface = editor.GraphSurface;
		surface?.SetWorkbenchActive(active: true);
		if (!routed || surface?.DetailsPanel == null)
		{
			return false;
		}
		PanelContainer inspectorPanel = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer embeddedInspectorHost = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		surface.NavigateToStableId("DerivedA");
		await WaitFrames(3);
		LineEdit lineEdit = surface.FindChild("Direct_ModStateNote", recursive: true, owned: false) as LineEdit;
		Button button = surface.FindChild("CreateStateOverrideButton", recursive: true, owned: false) as Button;
		bool stateInherited = IsInheritedState(surface, "DerivedA") && lineEdit != null && !lineEdit.Editable && button != null && !button.Disabled && surface.DetailsPanel.VisibleExtensionPropertyCount >= 6 && surface.DetailsPanel.MissingExtensionPropertyCount == 0;
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		ModEditorStateMachineProbeDerivedState localState = FindState(derived, "DerivedA") as ModEditorStateMachineProbeDerivedState;
		LineEdit lineEdit2 = surface.FindChild("Direct_ModStateNote", recursive: true, owned: false) as LineEdit;
		bool stateOverride = localState?.ModStateNote == "state-base" && localState.RuntimeWeight == 3 && (lineEdit2?.Editable ?? false);
		if (lineEdit2 != null)
		{
			lineEdit2.Text = "state-after";
			lineEdit2.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit2.Text);
		}
		await WaitFrames(3);
		bool stateEdited = localState?.ModStateNote == "state-after";
		surface.GraphController?.Undo();
		await WaitFrames(3);
		bool stateUndone = localState?.ModStateNote == "state-base" && (surface.FindChild("Direct_ModStateNote", recursive: true, owned: false) as LineEdit)?.Text == "state-base";
		surface.GraphController?.Redo();
		await WaitFrames(3);
		bool stateRedone = localState?.ModStateNote == "state-after" && (surface.FindChild("Direct_ModStateNote", recursive: true, owned: false) as LineEdit)?.Text == "state-after";
		if (surface.FindChild("Direct_PreviewOffset_X", recursive: true, owned: false) is SpinBox spinBox)
		{
			spinBox.Value = 24.0;
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, spinBox.Value);
			surface.DetailsPanel.FlushPendingNumericCommit();
		}
		await WaitFrames(3);
		if (surface.FindChild("Direct_RuntimeTags_Data", recursive: true, owned: false) is TextEdit textEdit)
		{
			textEdit.Text = "[\"probe\", \"edited\"]";
			textEdit.EmitSignal(Control.SignalName.FocusExited);
		}
		await WaitFrames(3);
		int num;
		if (localState != null && localState.PreviewOffset.X == 24f)
		{
			Array<string> runtimeTags = localState.RuntimeTags;
			if (runtimeTags != null && runtimeTags.Count == 2)
			{
				num = ((localState.RuntimeTags[1] == "edited") ? 1 : 0);
				goto IL_0935;
			}
		}
		num = 0;
		goto IL_0935;
		IL_12b7:
		int num2;
		bool flag = (byte)num2 != 0;
		bool flag2 = InspectorIsolated();
		bool stateVisualTypesEdited;
		bool transitionInherited;
		bool transitionOverride;
		bool transitionEdited;
		bool transitionUndone;
		bool transitionRedone;
		bool transitionStructuredEdited;
		bool composedOk;
		bool clipboardPreserved;
		bool runtimeRead;
		bool hashInvalidates;
		bool num3 = stateInherited && stateOverride && stateEdited && stateUndone && stateRedone && stateVisualTypesEdited && transitionInherited && transitionOverride && transitionEdited && transitionUndone && transitionRedone && transitionStructuredEdited && composedOk && clipboardPreserved && runtimeRead && hashInvalidates && flag && flag2;
		bool saved;
		ModEditorStateMachineProbeDerivedTransition localTransition;
		if (!num3)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_DERIVED_ELEMENT_DIAGNOSTIC] route={routed}/{initialSave} stateDirect={stateInherited}/{stateOverride}/{stateEdited}/{stateUndone}/{stateRedone}/{stateVisualTypesEdited} transitionDirect={transitionInherited}/{transitionOverride}/{transitionEdited}/{transitionUndone}/{transitionRedone}/{transitionStructuredEdited} compose={composedOk} clipboard={clipboardPreserved} runtimeRead={runtimeRead} hashInvalidates={hashInvalidates} saveReload={saved}/{flag} inspector={flag2} stateType={localState?.GetType().Name ?? "<null>"} transitionType={localTransition?.GetType().Name ?? "<null>"}");
		}
		return num3;
		IL_0935:
		stateVisualTypesEdited = (byte)num != 0;
		surface.NavigateToStableId("DerivedAToB");
		await WaitFrames(3);
		LineEdit lineEdit3 = surface.FindChild("Direct_ModTransitionNote", recursive: true, owned: false) as LineEdit;
		Button button2 = surface.FindChild("CreateTransitionOverrideButton", recursive: true, owned: false) as Button;
		transitionInherited = IsInheritedTransition(surface, "DerivedAToB") && lineEdit3 != null && !lineEdit3.Editable && button2 != null && !button2.Disabled && surface.DetailsPanel.VisibleExtensionPropertyCount >= 5 && surface.DetailsPanel.MissingExtensionPropertyCount == 0;
		button2?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
		localTransition = FindTransition(derived, "DerivedAToB") as ModEditorStateMachineProbeDerivedTransition;
		LineEdit lineEdit4 = surface.FindChild("Direct_ModTransitionNote", recursive: true, owned: false) as LineEdit;
		transitionOverride = localTransition?.ModTransitionNote == "transition-base" && Math.Abs(localTransition.RuntimeScale - 1.25) < 0.0001 && (lineEdit4?.Editable ?? false);
		if (lineEdit4 != null)
		{
			lineEdit4.Text = "transition-after";
			lineEdit4.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit4.Text);
		}
		await WaitFrames(3);
		transitionEdited = localTransition?.ModTransitionNote == "transition-after";
		surface.GraphController?.Undo();
		await WaitFrames(3);
		transitionUndone = localTransition?.ModTransitionNote == "transition-base";
		surface.GraphController?.Redo();
		await WaitFrames(3);
		transitionRedone = localTransition?.ModTransitionNote == "transition-after";
		if (surface.FindChild("Direct_RuntimeCosts_Data", recursive: true, owned: false) is TextEdit textEdit2)
		{
			textEdit2.Text = "{\"energy\": 7}";
			textEdit2.EmitSignal(Control.SignalName.FocusExited);
		}
		await WaitFrames(3);
		transitionStructuredEdited = localTransition?.RuntimeCosts != null && localTransition.RuntimeCosts.TryGetValue("energy", out var value) && value == 7;
		composedOk = StateMachineDefinitionComposer.TryCompose(derived, out var composed, out var validation) && validation.IsValid && FindState(composed, "DerivedA") is ModEditorStateMachineProbeDerivedState { ModStateNote: "state-after" } && FindTransition(composed, "DerivedAToB") is ModEditorStateMachineProbeDerivedTransition modEditorStateMachineProbeDerivedTransition && modEditorStateMachineProbeDerivedTransition.ModTransitionNote == "transition-after";
		StateMachineGraphClipboard stateMachineGraphClipboard = new StateMachineGraphClipboard();
		StateMachineGraphClipboardData stateMachineGraphClipboardData = (composedOk ? stateMachineGraphClipboard.CopySubgraph(composed, null, new string[2] { "DerivedA", "DerivedB" }) : null);
		StateMachineGraphPasteResult stateMachineGraphPasteResult = ((stateMachineGraphClipboardData == null) ? null : stateMachineGraphClipboard.PasteSubgraph(stateMachineGraphClipboardData, new GuidStateMachineStableIdProvider(), new Vector2(96f, 64f), composed));
		clipboardPreserved = stateMachineGraphPasteResult != null && stateMachineGraphPasteResult.States.Count == 2 && stateMachineGraphPasteResult.States.All((StateMachineStateDefinition state) => state is ModEditorStateMachineProbeDerivedState) && stateMachineGraphPasteResult.Transitions.Count == 1 && stateMachineGraphPasteResult.Transitions[0] is ModEditorStateMachineProbeDerivedTransition { ModTransitionNote: "transition-after" } modEditorStateMachineProbeDerivedTransition2 && stateMachineGraphPasteResult.States.Any((StateMachineStateDefinition state) => state is ModEditorStateMachineProbeDerivedState modEditorStateMachineProbeDerivedState3 && modEditorStateMachineProbeDerivedState3.ModStateNote == "state-after") && stateMachineGraphPasteResult.States.Any(delegate(StateMachineStateDefinition state)
		{
			if (state is ModEditorStateMachineProbeDerivedState modEditorStateMachineProbeDerivedState3 && modEditorStateMachineProbeDerivedState3.PreviewOffset.X == 24f)
			{
				Array<string> runtimeTags3 = modEditorStateMachineProbeDerivedState3.RuntimeTags;
				if (runtimeTags3 != null && runtimeTags3.Count == 2)
				{
					return modEditorStateMachineProbeDerivedState3.RuntimeTags[1] == "edited";
				}
			}
			return false;
		}) && modEditorStateMachineProbeDerivedTransition2.RuntimeCosts.TryGetValue("energy", out var value2) && value2 == 7 && stateMachineGraphPasteResult.StableIdRemap.TryGetValue("DerivedA", out var value3) && stateMachineGraphPasteResult.StableIdRemap.TryGetValue("DerivedB", out var value4) && modEditorStateMachineProbeDerivedTransition2.SourceStateId == value3 && modEditorStateMachineProbeDerivedTransition2.TargetStateId == value4;
		runtimeRead = StateMachineCompiler.TryCompile(derived, out var program, out var validation2) && validation2.IsValid && program.TryGetStateExtensionProperty("DerivedA", "ModStateNote", out var value5) && value5.AsString() == "state-after" && program.TryGetTransitionExtensionProperty("DerivedAToB", "RuntimeScale", out var value6) && Math.Abs(value6.AsDouble() - 1.25) < 0.0001;
		string a = program?.ContentHash ?? string.Empty;
		if (localState != null)
		{
			localState.RuntimeWeight = 17;
		}
		hashInvalidates = StateMachineCompiler.TryCompile(derived, out var program2, out var validation3) && validation3.IsValid && !string.Equals(a, program2.ContentHash, StringComparison.Ordinal) && program2.TryGetStateExtensionProperty("DerivedA", "RuntimeWeight", out var value7) && value7.AsInt64() == 17;
		saved = editor.SaveActiveResource();
		await WaitFrames(6);
		StateMachineDefinition definition = (saved ? ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_derived_element_probe.tres", "", ResourceLoader.CacheMode.Ignore) : null);
		if (FindState(definition, "DerivedA") is ModEditorStateMachineProbeDerivedState { ModStateNote: "state-after", RuntimeWeight: 17 } modEditorStateMachineProbeDerivedState2 && modEditorStateMachineProbeDerivedState2.PreviewOffset.X == 24f)
		{
			Array<string> runtimeTags2 = modEditorStateMachineProbeDerivedState2.RuntimeTags;
			if (runtimeTags2 != null && runtimeTags2.Count == 2 && modEditorStateMachineProbeDerivedState2.RuntimeTags[1] == "edited" && FindTransition(definition, "DerivedAToB") is ModEditorStateMachineProbeDerivedTransition { ModTransitionNote: "transition-after" } modEditorStateMachineProbeDerivedTransition3 && Math.Abs(modEditorStateMachineProbeDerivedTransition3.RuntimeScale - 1.25) < 0.0001 && modEditorStateMachineProbeDerivedTransition3.RuntimeCosts.TryGetValue("energy", out var value8))
			{
				num2 = ((value8 == 7) ? 1 : 0);
				goto IL_12b7;
			}
		}
		num2 = 0;
		goto IL_12b7;
		bool InspectorIsolated()
		{
			if (GodotObject.IsInstanceValid(inspectorPanel) && !inspectorPanel.Visible && GodotObject.IsInstanceValid(embeddedInspectorHost) && embeddedInspectorHost.GetChildCount() == 0)
			{
				if (inspector != null)
				{
					return inspector.CurrentObject == inspectorSentinel;
				}
				return true;
			}
			return false;
		}
	}

	private async Task<bool> ProbeDerivedTypeCreation(XWStateMachineVisualResourceEditor editor, XWInspector inspector, Node inspectorSentinel)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return false;
		}
		RemoveProbeResource("user://mod_editor_state_machine_derived_type_creation_probe.tres");
		RemoveProbeResource(StateMachineLayoutStore.GetLayoutPath("user://mod_editor_state_machine_derived_type_creation_probe.tres"));
		StateMachineDefinition definition = new StateMachineDefinition
		{
			DefinitionId = "derived-type-creation",
			RootStateId = "AuthorRoot"
		};
		definition.States.Add(new StateMachineStateDefinition
		{
			StableId = "AuthorRoot",
			DisplayName = "Author Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "AuthorTarget"
		});
		definition.States.Add(new StateMachineStateDefinition
		{
			StableId = "AuthorTarget",
			DisplayName = "Author Target",
			Kind = StateMachineStateKind.Atomic,
			ParentId = "AuthorRoot"
		});
		bool initialSave = ResourceSaver.Save(definition, "user://mod_editor_state_machine_derived_type_creation_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok;
		XWVisualEditorDescriptor descriptor;
		bool routed = initialSave && XWResourceEditorRegistry.TryGetEditor(definition, "user://mod_editor_state_machine_derived_type_creation_probe.tres", out descriptor) && XWResourceEditorRegistry.TryOpen(definition, "user://mod_editor_state_machine_derived_type_creation_probe.tres");
		await WaitFrames(6);
		StateMachineGraphEditorSurface surface = editor.GraphSurface;
		surface?.SetWorkbenchActive(active: true);
		if (!routed || surface?.GraphController == null)
		{
			return false;
		}
		PanelContainer inspectorPanel = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer embeddedInspectorHost = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		OptionButton option = surface.FindChild("NewStateTypeOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton = surface.FindChild("NewStateKind", recursive: true, owned: false) as OptionButton;
		Button button = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		int num = optionButton?.GetItemIndex(0) ?? (-1);
		bool stateTypeSelected = SelectOptionByMetadata(option, "registered:mod_editor_probe_derived_state") && num >= 0 && button != null && !button.Disabled;
		if (stateTypeSelected)
		{
			optionButton.Select(num);
			optionButton.EmitSignal(OptionButton.SignalName.ItemSelected, num);
			SelectOnly(surface, definition, "AuthorRoot");
		}
		HashSet<string> stateIdsBefore = CaptureStateIds(definition);
		if (stateTypeSelected)
		{
			button.EmitSignal(BaseButton.SignalName.Pressed);
		}
		await WaitFrames(4);
		string createdStateId = FindAddedStateId(definition, stateIdsBefore);
		ModEditorStateMachineProbeDerivedState createdState = FindState(definition, createdStateId) as ModEditorStateMachineProbeDerivedState;
		int num2;
		if (createdState?.GetType() == typeof(ModEditorStateMachineProbeDerivedState) && createdState.Kind == StateMachineStateKind.Atomic && createdState.ParentId == "AuthorRoot" && createdState.ModStateNote == "state-base" && createdState.RuntimeWeight == 3 && createdState.RuntimeEnabled && createdState.AccentColor.IsEqualApprox(new Color("62d6ff")) && createdState.PreviewOffset.IsEqualApprox(new Vector2(12f, -4f)))
		{
			Array<string> runtimeTags = createdState.RuntimeTags;
			if (runtimeTags != null && runtimeTags.Count == 2 && createdState.RuntimeTags[0] == "probe")
			{
				num2 = ((createdState.RuntimeTags[1] == "state") ? 1 : 0);
				goto IL_051c;
			}
		}
		num2 = 0;
		goto IL_051c;
		IL_051c:
		bool stateDefaults = (byte)num2 != 0;
		bool stateDirect = surface.ActiveDetailsKind == "State" && surface.ActiveDetailsStableId == createdStateId && surface.FindChild("Direct_ModStateNote", recursive: true, owned: false) is LineEdit { Editable: not false, Text: "state-base" } && surface.DetailsPanel.VisibleExtensionPropertyCount >= 6 && surface.DetailsPanel.MissingExtensionPropertyCount == 0;
		surface.GraphController.Undo();
		await WaitFrames(3);
		bool stateUndo = FindState(definition, createdStateId) == null;
		surface.GraphController.Redo();
		await WaitFrames(3);
		bool stateRedo = FindState(definition, createdStateId)?.GetType() == typeof(ModEditorStateMachineProbeDerivedState);
		surface.ShowDefinitionOverview();
		await WaitFrames(3);
		OptionButton option2 = surface.FindChild("TransitionTypeOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton2 = surface.FindChild("TransitionSourceOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton3 = surface.FindChild("TransitionTargetOption", recursive: true, owned: false) as OptionButton;
		OptionButton optionButton4 = surface.FindChild("TransitionTriggerOption", recursive: true, owned: false) as OptionButton;
		LineEdit lineEdit2 = surface.FindChild("TransitionEventNameEdit", recursive: true, owned: false) as LineEdit;
		Button button2 = surface.FindChild("CreateTransitionButton", recursive: true, owned: false) as Button;
		int num3 = FindStateOptionIndex(optionButton2, createdStateId);
		int num4 = FindStateOptionIndex(optionButton3, "AuthorTarget");
		int num5 = optionButton4?.GetItemIndex(0) ?? (-1);
		bool transitionTypeSelected = SelectOptionByMetadata(option2, "registered:mod_editor_probe_derived_transition") && num3 >= 0 && num4 >= 0 && num5 >= 0 && lineEdit2 != null && button2 != null && !button2.Disabled;
		if (transitionTypeSelected)
		{
			optionButton2.Select(num3);
			optionButton2.EmitSignal(OptionButton.SignalName.ItemSelected, num3);
			optionButton3.Select(num4);
			optionButton3.EmitSignal(OptionButton.SignalName.ItemSelected, num4);
			optionButton4.Select(num5);
			optionButton4.EmitSignal(OptionButton.SignalName.ItemSelected, num5);
			lineEdit2.Text = "derived_visual_create";
		}
		HashSet<string> transitionIdsBefore = CaptureTransitionIds(definition);
		if (transitionTypeSelected)
		{
			button2.EmitSignal(BaseButton.SignalName.Pressed);
		}
		await WaitFrames(4);
		string createdTransitionId = FindAddedTransitionId(definition, transitionIdsBefore);
		ModEditorStateMachineProbeDerivedTransition createdTransition = FindTransition(definition, createdTransitionId) as ModEditorStateMachineProbeDerivedTransition;
		int num6;
		if (createdTransition?.GetType() == typeof(ModEditorStateMachineProbeDerivedTransition) && createdTransition.SourceStateId == createdStateId && createdTransition.TargetStateId == "AuthorTarget" && createdTransition.TriggerKind == StateMachineTriggerKind.Event && createdTransition.EventName.ToString() == "derived_visual_create" && createdTransition.ModTransitionNote == "transition-base" && Math.Abs(createdTransition.RuntimeScale - 1.25) < 0.0001 && createdTransition.WireColor.IsEqualApprox(new Color("ffb84d")) && createdTransition.RuntimeImpulse.IsEqualApprox(new Vector3(1f, 2f, 3f)))
		{
			Godot.Collections.Dictionary<string, int> runtimeCosts = createdTransition.RuntimeCosts;
			if (runtimeCosts != null && runtimeCosts.Count == 1 && createdTransition.RuntimeCosts.TryGetValue("energy", out var value))
			{
				num6 = ((value == 2) ? 1 : 0);
				goto IL_0af1;
			}
		}
		num6 = 0;
		goto IL_0af1;
		IL_0af1:
		bool transitionDefaults = (byte)num6 != 0;
		bool transitionDirect = surface.ActiveDetailsKind == "Transition" && surface.ActiveDetailsStableId == createdTransitionId && surface.FindChild("Direct_ModTransitionNote", recursive: true, owned: false) is LineEdit { Editable: not false, Text: "transition-base" } && surface.DetailsPanel.VisibleExtensionPropertyCount >= 5 && surface.DetailsPanel.MissingExtensionPropertyCount == 0;
		surface.GraphController.Undo();
		await WaitFrames(3);
		bool transitionUndo = FindTransition(definition, createdTransitionId) == null;
		surface.GraphController.Redo();
		await WaitFrames(3);
		bool transitionRedo = FindTransition(definition, createdTransitionId)?.GetType() == typeof(ModEditorStateMachineProbeDerivedTransition);
		bool saved = editor.SaveActiveResource();
		await WaitFrames(6);
		StateMachineDefinition definition2 = (saved ? ResourceLoader.Load<StateMachineDefinition>("user://mod_editor_state_machine_derived_type_creation_probe.tres", "", ResourceLoader.CacheMode.Ignore) : null);
		bool flag = FindState(definition2, createdStateId)?.GetType() == typeof(ModEditorStateMachineProbeDerivedState) && FindState(definition2, createdStateId) is ModEditorStateMachineProbeDerivedState { ModStateNote: "state-base", RuntimeWeight: 3 } && FindTransition(definition2, createdTransitionId)?.GetType() == typeof(ModEditorStateMachineProbeDerivedTransition) && FindTransition(definition2, createdTransitionId) is ModEditorStateMachineProbeDerivedTransition { ModTransitionNote: "transition-base" } modEditorStateMachineProbeDerivedTransition && Math.Abs(modEditorStateMachineProbeDerivedTransition.RuntimeScale - 1.25) < 0.0001;
		bool flag2 = InspectorIsolated();
		bool num7 = stateTypeSelected && stateDefaults && stateDirect && stateUndo && stateRedo && transitionTypeSelected && transitionDefaults && transitionDirect && transitionUndo && transitionRedo && flag && flag2;
		if (!num7)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_DERIVED_TYPE_CREATION_DIAGNOSTIC] route={routed}/{initialSave} state={stateTypeSelected}/{stateDefaults}/{stateDirect}/{stateUndo}/{stateRedo} transition={transitionTypeSelected}/{transitionDefaults}/{transitionDirect}/{transitionUndo}/{transitionRedo} save={saved}/{flag} inspector={flag2} stateId={createdStateId} stateType={createdState?.GetType().Name ?? "<null>"} transitionId={createdTransitionId} transitionType={createdTransition?.GetType().Name ?? "<null>"}");
		}
		return num7;
		bool InspectorIsolated()
		{
			if (GodotObject.IsInstanceValid(inspectorPanel) && !inspectorPanel.Visible && GodotObject.IsInstanceValid(embeddedInspectorHost) && embeddedInspectorHost.GetChildCount() == 0)
			{
				if (inspector != null)
				{
					return inspector.CurrentObject == inspectorSentinel;
				}
				return true;
			}
			return false;
		}
	}

	private async Task<bool> ProbeCompositionFailureGuard(XWStateMachineVisualResourceEditor editor, XWInspector inspector, Node inspectorSentinel)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return false;
		}
		StateMachineGraphEditorSurface healthySurface = editor.GraphSurface;
		StateMachineDefinition healthyDefinition = healthySurface?.Definition;
		StateMachineGraphController healthyController = healthySurface?.GraphController;
		if (healthyDefinition == null || healthyController == null)
		{
			return false;
		}
		string originalDefinitionId = healthyDefinition.DefinitionId;
		string historyProbeDefinitionId = originalDefinitionId + "-history";
		healthyController.SetDefinitionProperty("DefinitionId", historyProbeDefinitionId);
		await WaitFrames(2);
		healthyController.Undo();
		await WaitFrames(2);
		bool redoStaged = healthyDefinition.DefinitionId == originalDefinitionId && (healthySurface.UndoAdapter?.CanRedo ?? false);
		StateMachineDefinition baseDefinition = new StateMachineDefinition
		{
			DefinitionId = "indirect-cycle-candidate",
			BaseDefinition = healthyDefinition
		};
		bool rejected = !healthyController.SetBaseDefinition(baseDefinition) && healthyDefinition.BaseDefinition == null && (healthySurface.UndoAdapter?.CanRedo ?? false);
		healthyController.Redo();
		await WaitFrames(2);
		bool rejectionHistoryClean = healthyDefinition.DefinitionId == historyProbeDefinitionId && healthyDefinition.BaseDefinition == null;
		StateMachineDefinition broken = CreateHierarchyDefinition();
		broken.DefinitionId = "composition-failure-self-cycle";
		broken.BaseDefinition = broken;
		XWVisualEditorDescriptor descriptor;
		bool opened = XWResourceEditorRegistry.TryGetEditor(broken, "user://mod_editor_state_machine_composition_failure_probe.tres", out descriptor) && XWResourceEditorRegistry.TryOpen(broken, "user://mod_editor_state_machine_composition_failure_probe.tres");
		await WaitFrames(6);
		StateMachineGraphEditorSurface surface = editor.GraphSurface;
		surface?.SetWorkbenchActive(active: true);
		if (!opened || surface?.GraphController == null)
		{
			return false;
		}
		surface.ShowDefinitionOverview();
		await WaitFrames(3);
		Button addState = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		Button button = surface.FindChild("CreateTransitionButton", recursive: true, owned: false) as Button;
		Control compositionBanner = surface.FindChild("CompositionFailureBanner", recursive: true, owned: false) as Control;
		bool authoringButtonsLocked = (addState?.Disabled ?? false) && (button?.Disabled ?? false);
		button?.EmitSignal(BaseButton.SignalName.Pressed);
		SelectOnly(surface, broken, "HierarchyLeaf");
		surface.EmitSignal(GraphEdit.SignalName.PopupRequest, new Vector2(180f, 140f));
		await WaitFrames(1);
		PopupMenu popupMenu = surface.FindChild("StateMachineCanvasContextMenu", recursive: true, owned: false) as PopupMenu;
		bool menuDisabled = IsPopupItemDisabled(popupMenu, 100L) && IsPopupItemDisabled(popupMenu, 200L) && IsPopupItemDisabled(popupMenu, 201L) && IsPopupItemDisabled(popupMenu, 202L) && IsPopupItemDisabled(popupMenu, 203L) && IsPopupItemDisabled(popupMenu, 204L);
		bool failureVisible = surface.GraphController.HasCompositionError && !surface.GraphController.ViewModel.CanMutate && !surface.GraphController.CanCopySubgraph && !surface.GraphController.CanPasteSubgraph && authoringButtonsLocked && (compositionBanner?.Visible ?? false) && menuDisabled;
		bool brokenSaveRejected = !editor.SaveActiveResource();
		int statesBefore = broken.States.Count;
		int transitionsBefore = broken.Transitions.Count;
		string rootBefore = broken.RootStateId;
		bool copyRejected = !surface.GraphController.CopySubgraph(new string[1] { "HierarchyLeaf" });
		IReadOnlyDictionary<string, string> directPaste = surface.GraphController.PasteSubgraphAt(new Vector2(320f, 220f));
		addState?.EmitSignal(BaseButton.SignalName.Pressed);
		surface.EmitSignal(GraphEdit.SignalName.CopyNodesRequest);
		surface.EmitSignal(GraphEdit.SignalName.PasteNodesRequest);
		surface.EmitSignal(GraphEdit.SignalName.CutNodesRequest);
		surface.EmitSignal(GraphEdit.SignalName.DuplicateNodesRequest);
		popupMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 100L);
		popupMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 200L);
		popupMenu?.EmitSignal(PopupMenu.SignalName.IdPressed, 203L);
		await WaitFrames(3);
		bool blockedActionsClean = copyRejected && (directPaste == null || directPaste.Count == 0) && broken.States.Count == statesBefore && broken.Transitions.Count == transitionsBefore && broken.RootStateId == rootBefore && broken.BaseDefinition == broken;
		bool repaired = surface.GraphController.SetBaseDefinition(null);
		await WaitFrames(4);
		addState = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		bool flag = surface.GraphController.CopySubgraph(new string[1] { "HierarchyLeaf" });
		int num;
		if (repaired && broken.BaseDefinition == null && !surface.GraphController.HasCompositionError && surface.GraphController.ViewModel.CanMutate && surface.GraphController.CanCopySubgraph && flag && surface.GraphController.CanPasteSubgraph)
		{
			Button button2 = addState;
			num = ((button2 != null && !button2.Disabled) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool recovered = (byte)num != 0;
		surface.GraphController.Undo();
		await WaitFrames(4);
		addState = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		bool undoRestoredGate = broken.BaseDefinition == broken && surface.GraphController.HasCompositionError && !surface.GraphController.ViewModel.CanMutate && !surface.GraphController.CanCopySubgraph && !surface.GraphController.CanPasteSubgraph && (addState?.Disabled ?? false);
		surface.GraphController.Redo();
		await WaitFrames(4);
		addState = surface.FindChild("AddStatePuzzleButton", recursive: true, owned: false) as Button;
		int num2;
		if (broken.BaseDefinition == null && !surface.GraphController.HasCompositionError && surface.GraphController.ViewModel.CanMutate && surface.GraphController.CanCopySubgraph && surface.GraphController.CanPasteSubgraph)
		{
			Button button3 = addState;
			num2 = ((button3 != null && !button3.Disabled) ? 1 : 0);
		}
		else
		{
			num2 = 0;
		}
		bool flag2 = (byte)num2 != 0;
		PanelContainer panelContainer = editor.FindChild("InspectorPanel", recursive: true, owned: false) as PanelContainer;
		VBoxContainer vBoxContainer = editor.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		bool flag3 = GodotObject.IsInstanceValid(panelContainer) && !panelContainer.Visible && GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0 && (inspector == null || inspector.CurrentObject == inspectorSentinel);
		bool num3 = redoStaged && rejected && rejectionHistoryClean && failureVisible && brokenSaveRejected && blockedActionsClean && recovered && undoRestoredGate && flag2 && flag3;
		if (!num3)
		{
			GD.Print($"[MOD_EDITOR_STATE_MACHINE_COMPOSITION_FAILURE_DIAGNOSTIC] candidate={redoStaged}/{rejected}/{rejectionHistoryClean} opened={opened} failure={failureVisible}/{menuDisabled}/saveRejected:{brokenSaveRejected} blocked={blockedActionsClean} repair={repaired}/{recovered}/{undoRestoredGate}/{flag2} inspector={flag3} counts={statesBefore}->{broken.States.Count}/{transitionsBefore}->{broken.Transitions.Count} hasError={surface.GraphController.HasCompositionError} canMutate={surface.GraphController.ViewModel.CanMutate} canCopy={surface.GraphController.CanCopySubgraph} canPaste={surface.GraphController.CanPasteSubgraph}");
		}
		return num3;
	}

	private async Task<bool> ProbeModBaseDefinitionPicker(XWStateMachineVisualResourceEditor editor, StateMachineGraphEditorSurface surface, StateMachineDefinition owner)
	{
		string text = "mod_editor_state_machine_picker_" + Guid.NewGuid().ToString("N");
		string text2 = "user://" + text;
		string rootPath = ProjectSettings.GlobalizePath(text2);
		string path = Path.Combine(rootPath, "Resources", "StateMachines");
		string baseUri = text2 + "/Resources/StateMachines/ProbeBase.tres";
		try
		{
			Directory.CreateDirectory(path);
			StateMachineDefinition stateMachineDefinition = CreateHierarchyDefinition();
			stateMachineDefinition.DefinitionId = "mod-picker-base";
			Error error = ResourceSaver.Save(stateMachineDefinition, baseUri, ResourceSaver.SaverFlags.None);
			if (error != Error.Ok)
			{
				GD.Print($"[MOD_EDITOR_STATE_MACHINE_PICKER_DIAGNOSTIC] stage=save error={error} path={baseUri}");
				return false;
			}
			XWFileSystem.GetSingleton().SetProjectFolderPath(rootPath);
			surface?.RefreshGraph();
			await WaitFrames(2);
			if (!(surface?.FindChild("BaseDefinitionPickerButton", recursive: true, owned: false) is Button button))
			{
				GD.Print("[MOD_EDITOR_STATE_MACHINE_PICKER_DIAGNOSTIC] stage=button missing=True");
				return false;
			}
			button.EmitSignal(BaseButton.SignalName.Pressed);
			for (int frame = 0; frame < 360; frame++)
			{
				XWGameplayResourcePickerWindow xWGameplayResourcePickerWindow = FindVisibleResourcePicker(editor);
				if (xWGameplayResourcePickerWindow != null && xWGameplayResourcePickerWindow.Visible && !xWGameplayResourcePickerWindow.IsResourceLibraryIndexing)
				{
					break;
				}
				await WaitFrames(1);
			}
			XWGameplayResourcePickerWindow picker = FindVisibleResourcePicker(editor);
			if (picker == null || picker.IsResourceLibraryIndexing)
			{
				GD.Print($"[MOD_EDITOR_STATE_MACHINE_PICKER_DIAGNOSTIC] stage=window picker={picker != null} active={GodotObject.IsInstanceValid(editor?.ActiveResourcePicker)} activeVisible={editor?.ActiveResourcePicker?.Visible} indexing={picker?.IsResourceLibraryIndexing}");
				return false;
			}
			XWGameplayResourceChoice modChoice = null;
			foreach (XWGameplayResourceChoice indexedChoice in picker.GetIndexedChoices(XWGameplayResourceKind.Resource))
			{
				if (indexedChoice.IsModResource && SamePhysicalResourcePath(indexedChoice.ResourcePath, baseUri))
				{
					modChoice = indexedChoice;
					break;
				}
			}
			bool projectPathForwarded = SamePhysicalResourcePath(picker.ResourceLibraryProjectPath, rootPath);
			bool confirmed = modChoice != null && picker.TryConfirmIndexedResource(modChoice.ResourcePath);
			await WaitFrames(3);
			bool flag = owner?.BaseDefinition != null && SamePhysicalResourcePath(owner.BaseDefinition.ResourcePath, baseUri);
			if (!projectPathForwarded || modChoice == null || !confirmed || !flag)
			{
				GD.Print($"[MOD_EDITOR_STATE_MACHINE_PICKER_DIAGNOSTIC] project={projectPathForwarded} choice={modChoice != null} confirmed={confirmed} assigned={flag} pickerPath={picker.ResourceLibraryProjectPath} choicePath={modChoice?.ResourcePath ?? "<null>"} assignedPath={owner?.BaseDefinition?.ResourcePath ?? "<null>"}");
			}
			return projectPathForwarded && confirmed && flag;
		}
		finally
		{
			editor?.ActiveResourcePicker?.Dismiss();
			if (Directory.Exists(rootPath))
			{
				Directory.Delete(rootPath, recursive: true);
			}
		}
	}

	private static XWGameplayResourcePickerWindow FindVisibleResourcePicker(XWStateMachineVisualResourceEditor editor)
	{
		if (!GodotObject.IsInstanceValid(editor))
		{
			return null;
		}
		foreach (Node item in editor.FindChildren("*", "", recursive: true, owned: false))
		{
			if (item is XWGameplayResourcePickerWindow xWGameplayResourcePickerWindow && GodotObject.IsInstanceValid(xWGameplayResourcePickerWindow) && xWGameplayResourcePickerWindow.Visible)
			{
				return xWGameplayResourcePickerWindow;
			}
		}
		if (!GodotObject.IsInstanceValid(editor.ActiveResourcePicker))
		{
			return null;
		}
		return editor.ActiveResourcePicker;
	}

	private static bool SamePhysicalResourcePath(string left, string right)
	{
		if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right))
		{
			return false;
		}
		try
		{
			string a = NormalizePhysicalResourcePath(left);
			string b = NormalizePhysicalResourcePath(right);
			return string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
		}
		catch (Exception ex) when (((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? 1 : 0) != 0)
		{
			return string.Equals(left.Replace('\\', '/').TrimEnd('/'), right.Replace('\\', '/').TrimEnd('/'), StringComparison.OrdinalIgnoreCase);
		}
	}

	private static string NormalizePhysicalResourcePath(string path)
	{
		return Path.GetFullPath(((path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase)) ? ProjectSettings.GlobalizePath(path) : path).Replace('/', Path.DirectorySeparatorChar)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
	}

	private async Task<XWStateMachineVisualResourceEditor> WaitForEditor(int maxFrames)
	{
		for (int i = 0; i < maxFrames; i++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("state_machine_editor") is XWStateMachineVisualResourceEditor result)
			{
				return result;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
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

	private async Task<bool> ProbeRecursiveGuardPuzzle(StateMachineGraphEditorSurface surface, StateMachineDefinition definition, string transitionId)
	{
		surface?.NavigateToStableId(transitionId);
		await WaitFrames(2);
		(surface?.FindChild("GuardKindAll", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		(surface?.FindChild("GuardAddExpressionProperty", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		(surface?.FindChild("GuardAddAny", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		StateMachineGuardDefinition root = FindTransition(definition, transitionId)?.GuardDefinition as StateMachineGuardDefinition;
		if (root == null || root.Kind != StateMachineGuardKind.All || root.Children.Count != 2)
		{
			return false;
		}
		(surface.FindChild("GuardEditChild0", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		XWStateGuardVisualResourceEditor editor = await WaitForStateGuardEditor();
		if (!(await ConfigureRuntimeGuardLeaf(editor, "health", 10.0, boolean: false)))
		{
			return false;
		}
		(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (editor == null)
		{
			return false;
		}
		(editor.FindChild("RuntimeGuardEditChild1", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (editor == null)
		{
			return false;
		}
		(editor.FindChild("AddRuntimeExpressionProperty", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		(editor.FindChild("AddRuntimeNot", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		StateMachineGuardDefinition guardChild = GetGuardChild(root, 1);
		if (guardChild == null || guardChild.Kind != StateMachineGuardKind.Any || guardChild.Children.Count != 2)
		{
			return false;
		}
		(editor.FindChild("RuntimeGuardEditChild0", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (!(await ConfigureRuntimeGuardLeaf(editor, "has_target", 0.0, boolean: true)))
		{
			return false;
		}
		(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		((await WaitForStateGuardEditor())?.FindChild("RuntimeGuardEditChild1", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (editor == null)
		{
			return false;
		}
		(editor.FindChild("AddRuntimeExpressionProperty", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(3);
		(editor.FindChild("RuntimeGuardEditChild0", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (!(await ConfigureRuntimeGuardLeaf(editor, "stunned", 0.0, boolean: true)))
		{
			return false;
		}
		(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (!(editor?.EditingGuard is StateMachineGuardDefinition { Kind: StateMachineGuardKind.Not }))
		{
			return false;
		}
		(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		editor = await WaitForStateGuardEditor();
		if (!(editor?.EditingGuard is StateMachineGuardDefinition { Kind: StateMachineGuardKind.Any }))
		{
			return false;
		}
		(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
		if (!((await WaitForStateGuardEditor())?.EditingGuard is StateMachineGuardDefinition { Kind: StateMachineGuardKind.All }))
		{
			return false;
		}
		StateMachineGuardDefinition guardChild2 = GetGuardChild(root, 0);
		StateMachineGuardDefinition guardChild3 = GetGuardChild(root, 1);
		StateMachineGuardDefinition guardChild4 = GetGuardChild(guardChild3, 1);
		int result;
		if (guardChild2 != null && guardChild2.Kind == StateMachineGuardKind.ExpressionProperty && guardChild3 != null && guardChild3.Kind == StateMachineGuardKind.Any)
		{
			StateMachineGuardDefinition guardChild5 = GetGuardChild(guardChild3, 0);
			if (guardChild5 != null && guardChild5.Kind == StateMachineGuardKind.ExpressionProperty && guardChild4 != null && guardChild4.Kind == StateMachineGuardKind.Not && guardChild4.Children.Count == 1)
			{
				StateMachineGuardDefinition guardChild6 = GetGuardChild(guardChild4, 0);
				result = ((guardChild6 != null && guardChild6.Kind == StateMachineGuardKind.ExpressionProperty) ? 1 : 0);
				goto IL_0d75;
			}
		}
		result = 0;
		goto IL_0d75;
		IL_0d75:
		return (byte)result != 0;
	}

	private static bool ProbeStateMachineNodeInputContract(StateMachineGraphEditorSurface surface)
	{
		StateMachineGraphNode obj = surface?.FindStateNode("Idle");
		Control control = obj?.GetNodeOrNull<Control>("PuzzleTile");
		Control control2 = obj?.GetNodeOrNull<Control>("PuzzleTile/HeadingRow");
		Control control3 = obj?.GetNodeOrNull<Control>("PuzzleTile/HierarchyRow");
		Control control4 = obj?.GetNodeOrNull<Control>("PuzzleTile/ActionRow");
		Label control5 = obj?.GetNodeOrNull<Label>("PuzzleTile/HeadingRow/KindBadge");
		Label control6 = obj?.GetNodeOrNull<Label>("PuzzleTile/HeadingRow/SimulationBadge");
		Label control7 = obj?.GetNodeOrNull<Label>("PuzzleTile/HeadingRow/InheritanceBadge");
		Label control8 = obj?.GetNodeOrNull<Label>("PuzzleTile/StableIdLabel");
		Label control9 = obj?.GetNodeOrNull<Label>("PuzzleTile/CallbackLabel");
		Button control10 = obj?.GetNodeOrNull<Button>("PuzzleTile/ActionRow/InspectButton");
		bool flag = Uses(control, Control.MouseFilterEnum.Pass) && Uses(control2, Control.MouseFilterEnum.Pass) && Uses(control3, Control.MouseFilterEnum.Pass) && Uses(control4, Control.MouseFilterEnum.Pass);
		bool flag2 = Uses(control5, Control.MouseFilterEnum.Ignore) && Uses(control6, Control.MouseFilterEnum.Ignore) && Uses(control7, Control.MouseFilterEnum.Ignore) && Uses(control8, Control.MouseFilterEnum.Ignore) && Uses(control9, Control.MouseFilterEnum.Ignore);
		bool flag3 = Uses(control10, Control.MouseFilterEnum.Stop);
		GD.Print($"[MOD_EDITOR_STATE_MACHINE_INPUT_CONTRACT] body={flag} decorations={flag2} actions={flag3}");
		return flag && flag2 && flag3;
		static bool Uses(Control control11, Control.MouseFilterEnum filter)
		{
			if (GodotObject.IsInstanceValid(control11))
			{
				return control11.MouseFilter == filter;
			}
			return false;
		}
	}

	private async Task<bool> ProbeNestedGuardRootPersistence(StateMachineDefinition definition, string transitionId, string savePath)
	{
		StateMachineGuardDefinition root = FindTransition(definition, transitionId)?.GuardDefinition as StateMachineGuardDefinition;
		StateMachineGuardDefinition any = GetGuardChild(root, 1);
		StateMachineGuardDefinition not = GetGuardChild(any, 1);
		StateMachineGuardDefinition leaf = GetGuardChild(not, 0);
		if (root != null && root.Kind == StateMachineGuardKind.All)
		{
			if (any != null && any.Kind == StateMachineGuardKind.Any)
			{
				if (not != null && not.Kind == StateMachineGuardKind.Not)
				{
					if (leaf != null && leaf.Kind == StateMachineGuardKind.ExpressionProperty)
					{
						((await WaitForStateGuardEditor(root))?.FindChild("RuntimeGuardEditChild1", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						((await WaitForStateGuardEditor(any))?.FindChild("RuntimeGuardEditChild1", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						((await WaitForStateGuardEditor(not))?.FindChild("RuntimeGuardEditChild0", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						XWStateGuardVisualResourceEditor editor = await WaitForStateGuardEditor(leaf);
						LineEdit lineEdit = editor?.FindChild("ComparedPropertyEdit", recursive: true, owned: false) as LineEdit;
						Button save = editor?.SaveGuardButton;
						if (lineEdit == null || save == null)
						{
							return false;
						}
						lineEdit.Text = "stunned_nested_saved";
						lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, "stunned_nested_saved");
						await WaitFrames(3);
						bool edited = leaf.ComparedProperty == new StringName("stunned_nested_saved");
						XWResourceEditContext xWResourceEditContext = typeof(XWGenericVisualResourceEditor).GetProperty("CurrentEditContext", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(editor) as XWResourceEditContext;
						GD.Print($"[MOD_EDITOR_NESTED_GUARD_CONTEXT] currentPath={editor.ActiveResourcePath} resourcePath={leaf.ResourcePath} owner={xWResourceEditContext?.OwnerResource?.GetType().Name ?? "<null>"} ownerPath={xWResourceEditContext?.OwnerPath ?? "<null>"} root={xWResourceEditContext?.PersistenceRootResource?.GetType().Name ?? "<null>"} rootPath={xWResourceEditContext?.PersistenceRootPath ?? "<null>"}");
						save.EmitSignal(BaseButton.SignalName.Pressed);
						await WaitFrames(7);
						Resource resource = ResourceLoader.Load<Resource>(savePath, "", ResourceLoader.CacheMode.Ignore);
						StateMachineDefinition stateMachineDefinition = resource as StateMachineDefinition;
						StateMachineGuardDefinition guardChild = GetGuardChild(GetGuardChild(GetGuardChild(FindTransition(stateMachineDefinition, transitionId)?.GuardDefinition as StateMachineGuardDefinition, 1), 1), 0);
						bool rootPreserved = stateMachineDefinition != null && stateMachineDefinition.DefinitionId == definition.DefinitionId && stateMachineDefinition.RootStateId == definition.RootStateId && stateMachineDefinition.States.Count == definition.States.Count && stateMachineDefinition.Transitions.Count == definition.Transitions.Count;
						bool nestedValuePersisted = guardChild?.ComparedProperty == new StringName("stunned_nested_saved");
						GD.Print($"[MOD_EDITOR_NESTED_GUARD_SAVE] edited={edited} loadedType={resource?.GetType().Name ?? "<null>"} rootPreserved={rootPreserved} states={stateMachineDefinition?.States?.Count ?? (-1)}/{definition.States.Count} transitions={stateMachineDefinition?.Transitions?.Count ?? (-1)}/{definition.Transitions.Count} loadedLeaf={guardChild?.ComparedProperty.ToString() ?? "<null>"} nestedValuePersisted={nestedValuePersisted}");
						StateMachineCompiler.DisposeTemporaryDefinition(stateMachineDefinition, disposeGuardTrees: true);
						editor = await WaitForStateGuardEditor(leaf);
						if (editor?.FindChild("ComparedPropertyEdit", recursive: true, owned: false) is LineEdit lineEdit2)
						{
							lineEdit2.Text = "stunned";
							lineEdit2.EmitSignal(LineEdit.SignalName.TextSubmitted, lineEdit2.Text);
							await WaitFrames(3);
						}
						(editor?.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						((await WaitForStateGuardEditor(not))?.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						((await WaitForStateGuardEditor(any))?.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						editor = await WaitForStateGuardEditor(root);
						return edited && rootPreserved && nestedValuePersisted && leaf.ComparedProperty == new StringName("stunned") && editor != null;
					}
				}
			}
		}
		return false;
	}

	private async Task<bool> ProbeNestedGuardRootSaveAs(StateMachineDefinition source, string transitionId)
	{
		RemoveProbeResource("user://mod_editor_state_machine_nested_save_as_probe.tres");
		if (!(source?.Duplicate(deep: true) is StateMachineDefinition unsaved))
		{
			return false;
		}
		ClearStateMachineResourcePaths(unsaved);
		StateMachineGuardDefinition root = FindTransition(unsaved, transitionId)?.GuardDefinition as StateMachineGuardDefinition;
		StateMachineGuardDefinition guardChild = GetGuardChild(GetGuardChild(root, 1), 1);
		StateMachineGuardDefinition guardChild2 = GetGuardChild(guardChild, 0);
		if (guardChild2 == null)
		{
			return false;
		}
		XWResourceEditContext context = XWResourceEditContext.ForProperty(guardChild2, guardChild, "", "", "Children", 0, "state_guard_editor", isBuiltInSource: false, "", unsaved);
		XWEditorInterface.Instance?.EditResource(guardChild2, context);
		XWStateGuardVisualResourceEditor editor = await WaitForStateGuardEditor(guardChild2);
		if (!(editor?.FindChild("ComparedPropertyEdit", recursive: true, owned: false) is LineEdit lineEdit))
		{
			return false;
		}
		lineEdit.Text = "nested_save_as_root";
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, "nested_save_as_root");
		await WaitFrames(3);
		MethodInfo saveAs = typeof(XWGenericVisualResourceEditor).GetMethod("TrySaveCurrentResourceAs", BindingFlags.Instance | BindingFlags.NonPublic);
		object obj = saveAs?.Invoke(editor, new object[1] { "user://mod_editor_state_machine_nested_save_as_probe.tres" });
		bool saved = obj is bool && (bool)obj;
		await WaitFrames(5);
		StateMachineDefinition stateMachineDefinition = ResourceLoader.Load<Resource>("user://mod_editor_state_machine_nested_save_as_probe.tres", "", ResourceLoader.CacheMode.Ignore) as StateMachineDefinition;
		StateMachineGuardDefinition guardChild3 = GetGuardChild(GetGuardChild(GetGuardChild(FindTransition(stateMachineDefinition, transitionId)?.GuardDefinition as StateMachineGuardDefinition, 1), 1), 0);
		bool passed = saved && stateMachineDefinition != null && stateMachineDefinition.States.Count == unsaved.States.Count && stateMachineDefinition.Transitions.Count == unsaved.Transitions.Count && guardChild3?.ComparedProperty == new StringName("nested_save_as_root") && string.IsNullOrWhiteSpace(editor.ActiveResourcePath) && unsaved.ResourcePath == "user://mod_editor_state_machine_nested_save_as_probe.tres";
		StateMachineCompiler.DisposeTemporaryDefinition(stateMachineDefinition, disposeGuardTrees: true);
		RemoveProbeResource("user://mod_editor_external_guard_source.tres");
		RemoveProbeResource("user://mod_editor_external_guard_save_as.tres");
		StateMachineGuardDefinition externalGuard = new StateMachineGuardDefinition
		{
			Kind = StateMachineGuardKind.ExpressionProperty,
			ComparedProperty = "external_guard_source",
			ExpectedValue = true
		};
		bool externalSourceSaved = ResourceSaver.Save(externalGuard, "user://mod_editor_external_guard_source.tres", ResourceSaver.SaverFlags.None) == Error.Ok;
		if (externalSourceSaved)
		{
			externalGuard.ResourcePath = "user://mod_editor_external_guard_source.tres";
		}
		XWResourceEditContext context2 = XWResourceEditContext.ForProperty(externalGuard, root, "user://mod_editor_external_guard_source.tres", "user://mod_editor_state_machine_nested_save_as_probe.tres", "Children", 0, "state_guard_editor", isBuiltInSource: false, "", unsaved, "user://mod_editor_state_machine_nested_save_as_probe.tres");
		XWEditorInterface.Instance?.EditResource(externalGuard, context2);
		editor = await WaitForStateGuardEditor(externalGuard);
		int num;
		if (externalSourceSaved)
		{
			obj = saveAs?.Invoke(editor, new object[1] { "user://mod_editor_external_guard_save_as.tres" });
			num = ((obj is bool && (bool)obj) ? 1 : 0);
		}
		else
		{
			num = 0;
		}
		bool externalSavedAs = (byte)num != 0;
		await WaitFrames(4);
		Resource resource = ResourceLoader.Load<Resource>("user://mod_editor_external_guard_save_as.tres", "", ResourceLoader.CacheMode.Ignore);
		passed &= externalSavedAs && resource is StateMachineGuardDefinition stateMachineGuardDefinition && stateMachineGuardDefinition.ComparedProperty == new StringName("external_guard_source") && externalGuard.ResourcePath == "user://mod_editor_external_guard_save_as.tres" && unsaved.ResourcePath == "user://mod_editor_state_machine_nested_save_as_probe.tres";
		StateMachineGuardDefinition stateMachineGuardDefinition2 = FindTransition(source, transitionId)?.GuardDefinition as StateMachineGuardDefinition;
		XWEditorInterface.Instance?.EditResource(stateMachineGuardDefinition2, XWResourceEditContext.ForNestedResourceView(stateMachineGuardDefinition2, stateMachineGuardDefinition2?.ResourcePath, "state_guard_editor", isBuiltInSource: false, source, "user://mod_editor_state_machine_workbench_probe.tres"));
		await WaitForStateGuardEditor(stateMachineGuardDefinition2);
		return passed;
	}

	private static void ClearStateMachineResourcePaths(StateMachineDefinition definition)
	{
		definition.ResourcePath = "";
		foreach (StateMachineStateDefinition state in definition.States)
		{
			if (state != null)
			{
				state.ResourcePath = "";
			}
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition != null)
			{
				transition.ResourcePath = "";
				ClearGuardResourcePaths(transition.GuardDefinition as StateMachineGuardDefinition);
			}
		}
	}

	private static void ClearGuardResourcePaths(StateMachineGuardDefinition guard)
	{
		if (guard == null)
		{
			return;
		}
		guard.ResourcePath = "";
		foreach (Resource child in guard.Children)
		{
			ClearGuardResourcePaths(child as StateMachineGuardDefinition);
		}
	}

	private async Task<bool> ProbeNestedGuardUndoRedo(StateMachineGuardDefinition root)
	{
		if (root != null && root.Kind == StateMachineGuardKind.All)
		{
			Array<Resource> children = root.Children;
			if (children != null && children.Count == 2)
			{
				StateMachineGuardDefinition any = GetGuardChild(root, 1);
				if (any != null && any.Kind == StateMachineGuardKind.Any)
				{
					Array<Resource> children2 = any.Children;
					if (children2 != null && children2.Count == 2)
					{
						if (!((await WaitForStateGuardEditor(root))?.FindChild("RuntimeGuardEditChild1", recursive: true, owned: false) is Button button))
						{
							return false;
						}
						button.EmitSignal(BaseButton.SignalName.Pressed);
						XWStateGuardVisualResourceEditor editor = await WaitForStateGuardEditor(any);
						if (editor == null)
						{
							return false;
						}
						StateMachineGuardDefinition first = GetGuardChild(any, 0);
						StateMachineGuardDefinition second = GetGuardChild(any, 1);
						XWUndoRedoManager undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager();
						Button button2 = editor.FindChild("RuntimeGuardRemoveChild0", recursive: true, owned: false) as Button;
						if (undoRedo == null || button2 == null)
						{
							return false;
						}
						button2.EmitSignal(BaseButton.SignalName.Pressed);
						await WaitFrames(2);
						bool removed = any.Children.Count == 1 && any.Children[0] == second && undoRedo.GetCurrentActionName() == "移除条件拼图";
						bool undone = undoRedo.Undo();
						await WaitFrames(2);
						bool restored = any.Children.Count == 2 && any.Children[0] == first && any.Children[1] == second;
						bool redone = undoRedo.Redo();
						await WaitFrames(2);
						bool removedAgain = any.Children.Count == 1 && any.Children[0] == second;
						bool restoredForRemainingProbe = undoRedo.Undo();
						await WaitFrames(2);
						bool finalTree = IsRecursiveGuardPuzzle(root);
						(editor.FindChild("ReturnToParentGuard", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
						XWStateGuardVisualResourceEditor xWStateGuardVisualResourceEditor = await WaitForStateGuardEditor(root);
						return removed && undone && restored && redone && removedAgain && restoredForRemainingProbe && finalTree && xWStateGuardVisualResourceEditor != null;
					}
				}
				return false;
			}
		}
		return false;
	}

	private async Task<bool> ProbeGuardKindAtomicUndo(StateMachineGuardDefinition returnGuard)
	{
		StateMachineGuardDefinition first = new StateMachineGuardDefinition
		{
			Kind = StateMachineGuardKind.ExpressionProperty,
			ComparedProperty = "probe_a",
			ExpectedValue = true
		};
		StateMachineGuardDefinition second = new StateMachineGuardDefinition
		{
			Kind = StateMachineGuardKind.ExpressionProperty,
			ComparedProperty = "probe_b",
			ExpectedValue = false
		};
		StateMachineGuardDefinition probe = new StateMachineGuardDefinition
		{
			Kind = StateMachineGuardKind.All
		};
		probe.Children.Add(first);
		probe.Children.Add(second);
		XWEditorInterface.Instance?.EditResource(probe, XWResourceEditContext.ForRoot(probe, string.Empty, "state_guard_editor"));
		Button button = (await WaitForStateGuardEditor(probe))?.FindChild("RuntimeKindNot", recursive: true, owned: false) as Button;
		XWUndoRedoManager undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (button == null || undoRedo == null)
		{
			return false;
		}
		button.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(2);
		bool applied = probe.Kind == StateMachineGuardKind.Not && probe.Children.Count == 1 && probe.Children[0] == first && undoRedo.GetCurrentActionName() == "切换条件拼图类型";
		bool undo = undoRedo.Undo();
		await WaitFrames(2);
		bool oneStepRestored = probe.Kind == StateMachineGuardKind.All && probe.Children.Count == 2 && probe.Children[0] == first && probe.Children[1] == second;
		bool redo = undoRedo.Redo();
		await WaitFrames(2);
		bool oneStepReapplied = probe.Kind == StateMachineGuardKind.Not && probe.Children.Count == 1 && probe.Children[0] == first;
		bool leaveRestored = undoRedo.Undo();
		await WaitFrames(2);
		bool result = applied && undo && oneStepRestored && redo && oneStepReapplied && leaveRestored && probe.Kind == StateMachineGuardKind.All && probe.Children.Count == 2;
		XWEditorInterface.Instance?.EditResource(returnGuard, XWResourceEditContext.ForRoot(returnGuard, string.Empty, "state_guard_editor"));
		await WaitForStateGuardEditor(returnGuard);
		undoRedo.ClearHistory();
		probe.Children.Clear();
		probe.Dispose();
		first.Dispose();
		second.Dispose();
		return result;
	}

	private async Task<bool> ProbeMalformedGuardUiSafety()
	{
		StateMachineDetailsPanel details = null;
		XWStateGuardPreviewCanvas preview = null;
		StateMachineTransitionDefinition transition = null;
		StateMachineDefinition definition = null;
		List<StateMachineGuardDefinition> malformedGuards = new List<StateMachineGuardDefinition>();
		try
		{
			StateMachineGuardDefinition cyclic = new StateMachineGuardDefinition
			{
				Kind = StateMachineGuardKind.All
			};
			malformedGuards.Add(cyclic);
			StateMachineGuardDefinition stateMachineGuardDefinition = cyclic;
			for (int i = 0; i < 40; i++)
			{
				StateMachineGuardDefinition stateMachineGuardDefinition2 = new StateMachineGuardDefinition
				{
					Kind = ((i != 39) ? StateMachineGuardKind.Not : StateMachineGuardKind.ExpressionProperty),
					ComparedProperty = ((i == 39) ? ((StringName)"too_deep") : new StringName()),
					ExpectedValue = (i == 39)
				};
				malformedGuards.Add(stateMachineGuardDefinition2);
				stateMachineGuardDefinition.Children.Add(stateMachineGuardDefinition2);
				stateMachineGuardDefinition = stateMachineGuardDefinition2;
			}
			transition = new StateMachineTransitionDefinition
			{
				StableId = "CycleGuardTransition",
				SourceStateId = "Idle",
				TargetStateId = "Idle",
				EventName = "CycleGuardEvent",
				GuardDefinition = cyclic
			};
			definition = new StateMachineDefinition
			{
				DefinitionId = "cycle-guard-ui-probe",
				RootStateId = "Root"
			};
			definition.States.Add(new StateMachineStateDefinition
			{
				StableId = "Root",
				DisplayName = "Root",
				Kind = StateMachineStateKind.Compound,
				InitialChildId = "Idle"
			});
			definition.States.Add(new StateMachineStateDefinition
			{
				StableId = "Idle",
				DisplayName = "Idle",
				ParentId = "Root"
			});
			definition.Transitions.Add(transition);
			details = new StateMachineDetailsPanel
			{
				Name = "CycleGuardDetailsProbe"
			};
			AddChild(details, forceReadableName: false, InternalMode.Disabled);
			preview = new XWStateGuardPreviewCanvas
			{
				Name = "CycleGuardPreviewProbe",
				CustomMinimumSize = new Vector2(480f, 180f)
			};
			AddChild(preview, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(1);
			details.ShowTransition(transition, isInherited: false, isLocalOverride: false, definitionReadOnly: false);
			preview.Bind(cyclic, default(Variant));
			await WaitFrames(2);
			bool detailsSafe = GodotObject.IsInstanceValid(details) && details.FindChild("GuardWorkbench", recursive: true, owned: false) != null && details.FindChild("GuardChild0", recursive: true, owned: false) != null;
			bool previewSafe = GodotObject.IsInstanceValid(preview) && preview.PreviewResult.HasValue;
			StateMachineValidationResult stateMachineValidationResult = StateMachineValidator.ValidateComposed(definition);
			bool rejected = stateMachineValidationResult != null && !stateMachineValidationResult.IsValid && stateMachineValidationResult.Diagnostics.Any((StateMachineDiagnostic diagnostic) => diagnostic.Code == "SM015");
			await WaitFrames(1);
			return detailsSafe && previewSafe && rejected && GodotObject.IsInstanceValid(details) && GodotObject.IsInstanceValid(preview);
		}
		catch (Exception ex)
		{
			GD.PrintErr("[MOD_EDITOR_CYCLE_GUARD_UI_FAILURE] " + ex);
			return false;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(preview))
			{
				preview.Bind(null, default(Variant));
			}
			if (GodotObject.IsInstanceValid(details))
			{
				details.ShowTransition(null, isInherited: false, isLocalOverride: false, definitionReadOnly: false);
			}
			if (GodotObject.IsInstanceValid(transition))
			{
				transition.GuardDefinition = null;
			}
			if (GodotObject.IsInstanceValid(definition))
			{
				definition.Transitions.Clear();
			}
			for (int num = 0; num < malformedGuards.Count; num++)
			{
				if (GodotObject.IsInstanceValid(malformedGuards[num]))
				{
					malformedGuards[num].Children.Clear();
				}
			}
			if (GodotObject.IsInstanceValid(details))
			{
				details.Free();
			}
			if (GodotObject.IsInstanceValid(preview))
			{
				preview.Free();
			}
			if (GodotObject.IsInstanceValid(transition))
			{
				transition.Dispose();
			}
			if (GodotObject.IsInstanceValid(definition))
			{
				foreach (StateMachineStateDefinition state in definition.States)
				{
					state?.Dispose();
				}
				definition.States.Clear();
				definition.Dispose();
			}
			for (int num2 = malformedGuards.Count - 1; num2 >= 0; num2--)
			{
				if (GodotObject.IsInstanceValid(malformedGuards[num2]))
				{
					malformedGuards[num2].Dispose();
				}
			}
			malformedGuards.Clear();
		}
	}

	private async Task<XWStateGuardVisualResourceEditor> WaitForStateGuardEditor(Resource expected = null)
	{
		for (int frame = 0; frame < 180; frame++)
		{
			if (XWEditorInterface.Instance?.GetResourceEditor("state_guard_editor") is XWStateGuardVisualResourceEditor { Visible: not false } xWStateGuardVisualResourceEditor && (!GodotObject.IsInstanceValid(expected) || xWStateGuardVisualResourceEditor.EditingGuard == expected))
			{
				return xWStateGuardVisualResourceEditor;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private async Task<bool> ConfigureRuntimeGuardLeaf(XWStateGuardVisualResourceEditor editor, string property, double number, bool boolean)
	{
		Resource resource = editor?.EditingGuard;
		if (!(resource is StateMachineGuardDefinition guard))
		{
			return false;
		}
		LineEdit lineEdit = editor.FindChild("ComparedPropertyEdit", recursive: true, owned: false) as LineEdit;
		Button button = editor.FindChild("OperatorEqual", recursive: true, owned: false) as Button;
		if (lineEdit == null || button == null)
		{
			return false;
		}
		lineEdit.Text = property;
		lineEdit.EmitSignal(LineEdit.SignalName.TextSubmitted, property);
		button.EmitSignal(BaseButton.SignalName.Pressed);
		if (boolean)
		{
			(editor.FindChild("ExpectedTypeBool", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			if (!(editor.FindChild("ExpectedBool", recursive: true, owned: false) is CheckButton checkButton))
			{
				return false;
			}
			checkButton.ButtonPressed = true;
			checkButton.EmitSignal(BaseButton.SignalName.Toggled, true);
		}
		else
		{
			(editor.FindChild("ExpectedTypeFloat", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(2);
			if (!(editor.FindChild("ExpectedNumber", recursive: true, owned: false) is SpinBox spinBox))
			{
				return false;
			}
			spinBox.EmitSignal(Control.SignalName.FocusEntered);
			spinBox.Value = number;
			spinBox.EmitSignal(Godot.Range.SignalName.ValueChanged, number);
			spinBox.EmitSignal(Control.SignalName.FocusExited);
		}
		await WaitFrames(2);
		return guard.ComparedProperty == new StringName(property);
	}

	private static bool IsRecursiveGuardPuzzle(StateMachineGuardDefinition root)
	{
		if (root != null && root.Kind == StateMachineGuardKind.All)
		{
			Array<Resource> children = root.Children;
			if (children != null && children.Count == 2)
			{
				StateMachineGuardDefinition guardChild = GetGuardChild(root, 0);
				StateMachineGuardDefinition guardChild2 = GetGuardChild(root, 1);
				StateMachineGuardDefinition guard = ((guardChild2 != null && guardChild2.Children?.Count == 2) ? GetGuardChild(guardChild2, 0) : null);
				StateMachineGuardDefinition stateMachineGuardDefinition = ((guardChild2 != null && guardChild2.Children?.Count == 2) ? GetGuardChild(guardChild2, 1) : null);
				StateMachineGuardDefinition guard2 = ((stateMachineGuardDefinition != null && stateMachineGuardDefinition.Children?.Count == 1) ? GetGuardChild(stateMachineGuardDefinition, 0) : null);
				if (IsGuardLeaf(guardChild, "health", Variant.Type.Float, 10.0) && IsGuardLeaf(guard, "has_target", Variant.Type.Bool, 1.0) && stateMachineGuardDefinition != null && stateMachineGuardDefinition.Kind == StateMachineGuardKind.Not)
				{
					return IsGuardLeaf(guard2, "stunned", Variant.Type.Bool, 1.0);
				}
				return false;
			}
		}
		return false;
	}

	private static bool IsGuardLeaf(StateMachineGuardDefinition guard, string property, Variant.Type type, double number)
	{
		if (guard != null && guard.Kind == StateMachineGuardKind.ExpressionProperty && guard.ComparedProperty == new StringName(property) && guard.Operator == StateMachineComparisonOperator.Equal && guard.ExpectedValue.VariantType == type)
		{
			if (type == Variant.Type.Bool)
			{
				return guard.ExpectedValue.AsBool() == (number != 0.0);
			}
			return Math.Abs(guard.ExpectedValue.AsDouble() - number) < 0.001;
		}
		return false;
	}

	private static StateMachineGuardDefinition GetGuardChild(StateMachineGuardDefinition guard, int index)
	{
		if (guard?.Children == null || index < 0 || index >= guard.Children.Count)
		{
			return null;
		}
		return guard.Children[index] as StateMachineGuardDefinition;
	}

	private static StateMachineDefinition CreateSimulationDefinition()
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "mod-editor-simulation-probe",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "Idle"
		});
		string[] array = new string[3] { "Idle", "Attack", "Recover" };
		foreach (string text in array)
		{
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = text,
				DisplayName = text,
				ParentId = "Root"
			});
		}
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "IdleToAttack",
			SourceStateId = "Idle",
			TargetStateId = "Attack",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "ToAttack",
			DelaySeconds = 0.2,
			GuardDefinition = new StateMachineGuardDefinition
			{
				Kind = StateMachineGuardKind.ExpressionProperty,
				ComparedProperty = "health",
				Operator = StateMachineComparisonOperator.GreaterOrEqual,
				ExpectedValue = Variant.From<double>(10.0)
			}
		});
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "AttackToRecover",
			SourceStateId = "Attack",
			TargetStateId = "Recover",
			TriggerKind = StateMachineTriggerKind.Automatic,
			DelaySeconds = 0.15
		});
		stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
		{
			StableId = "RecoverToIdle",
			SourceStateId = "Recover",
			TargetStateId = "Idle",
			TriggerKind = StateMachineTriggerKind.Event,
			EventName = "ToIdle"
		});
		return stateMachineDefinition;
	}

	private static StateMachineDefinition CreateLargeGraphDefinition()
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "mod-editor-large-graph-probe",
			RootStateId = "Root"
		};
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "Root",
			DisplayName = "Root",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "LargeState1"
		});
		for (int i = 1; i < 500; i++)
		{
			stateMachineDefinition.States.Add(new StateMachineStateDefinition
			{
				StableId = $"LargeState{i}",
				DisplayName = $"Large State {i}",
				ParentId = "Root"
			});
		}
		for (int j = 1; j < 500; j++)
		{
			int value = ((j == 499) ? 1 : (j + 1));
			stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
			{
				StableId = $"LargeEdgeA{j}",
				SourceStateId = $"LargeState{j}",
				TargetStateId = $"LargeState{value}",
				TriggerKind = StateMachineTriggerKind.Event,
				EventName = $"Advance{j}"
			});
		}
		for (int k = 1; k <= 301; k++)
		{
			int value2 = (k + 1) % 499 + 1;
			stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
			{
				StableId = $"LargeEdgeB{k}",
				SourceStateId = $"LargeState{k}",
				TargetStateId = $"LargeState{value2}",
				TriggerKind = StateMachineTriggerKind.Event,
				EventName = $"Skip{k}"
			});
		}
		return stateMachineDefinition;
	}

	private static StateMachineDefinition CreateHierarchyCollapseDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "mod-editor-hierarchy-collapse-probe",
			RootStateId = "CollapseRoot",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "CollapseRoot",
					DisplayName = "折叠根",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "CollapseBranch"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseBranch",
					DisplayName = "可折叠分支",
					Kind = StateMachineStateKind.Compound,
					ParentId = "CollapseRoot",
					InitialChildId = "CollapseNested"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseNested",
					DisplayName = "嵌套容器",
					Kind = StateMachineStateKind.Compound,
					ParentId = "CollapseBranch",
					InitialChildId = "CollapseLeaf"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseLeaf",
					DisplayName = "分支叶节点",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "CollapseNested"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseOutside",
					DisplayName = "外部节点",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "CollapseRoot"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseParallel",
					DisplayName = "并行区域",
					Kind = StateMachineStateKind.Parallel,
					ParentId = "CollapseRoot"
				},
				new StateMachineStateDefinition
				{
					StableId = "CollapseParallelLeaf",
					DisplayName = "并行子节点",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "CollapseParallel"
				}
			},
			Transitions = 
			{
				new StateMachineTransitionDefinition
				{
					StableId = "CollapseBranchToOutside",
					SourceStateId = "CollapseBranch",
					TargetStateId = "CollapseOutside",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "BranchOut"
				},
				new StateMachineTransitionDefinition
				{
					StableId = "CollapseNestedToOutside",
					SourceStateId = "CollapseNested",
					TargetStateId = "CollapseOutside",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "NestedOut"
				},
				new StateMachineTransitionDefinition
				{
					StableId = "CollapseLeafToOutside",
					SourceStateId = "CollapseLeaf",
					TargetStateId = "CollapseOutside",
					TriggerKind = StateMachineTriggerKind.Event,
					EventName = "LeafOut"
				}
			}
		};
	}

	private static StateMachineDefinition CreateHierarchyDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "hierarchy-probe",
			RootStateId = "HierarchyRoot",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "HierarchyRoot",
					DisplayName = "HierarchyRoot",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "HierarchyBranch"
				},
				new StateMachineStateDefinition
				{
					StableId = "HierarchyBranch",
					DisplayName = "HierarchyBranch",
					Kind = StateMachineStateKind.Compound,
					ParentId = "HierarchyRoot",
					InitialChildId = "HierarchyLeaf"
				},
				new StateMachineStateDefinition
				{
					StableId = "HierarchyLeaf",
					DisplayName = "HierarchyLeaf",
					ParentId = "HierarchyBranch"
				}
			}
		};
	}

	private static StateMachineDefinition CreateHierarchyGuardDefinition()
	{
		StateMachineDefinition stateMachineDefinition = CreateHierarchyDefinition();
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyMovable",
			DisplayName = "HierarchyMovable",
			ParentId = "HierarchyBranch"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyNestedContainer",
			DisplayName = "HierarchyNestedContainer",
			ParentId = "HierarchyBranch",
			Kind = StateMachineStateKind.Compound,
			InitialChildId = "HierarchyNestedLeaf"
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyNestedLeaf",
			DisplayName = "HierarchyNestedLeaf",
			ParentId = "HierarchyNestedContainer",
			Kind = StateMachineStateKind.Atomic
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyAtomicParent",
			DisplayName = "HierarchyAtomicParent",
			ParentId = "HierarchyBranch",
			Kind = StateMachineStateKind.Atomic
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyHistoryParent",
			DisplayName = "HierarchyHistoryParent",
			ParentId = "HierarchyBranch",
			Kind = StateMachineStateKind.History
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyHistory",
			DisplayName = "HierarchyHistory",
			ParentId = "HierarchyBranch",
			Kind = StateMachineStateKind.History
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyParallel",
			DisplayName = "HierarchyParallel",
			ParentId = "HierarchyRoot",
			Kind = StateMachineStateKind.Parallel
		});
		stateMachineDefinition.States.Add(new StateMachineStateDefinition
		{
			StableId = "HierarchyRegion",
			DisplayName = "HierarchyRegion",
			ParentId = "HierarchyParallel",
			Kind = StateMachineStateKind.Atomic
		});
		return stateMachineDefinition;
	}

	private static StateMachineDefinition CreateParallelHierarchyDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "parallel-hierarchy-probe",
			RootStateId = "HierarchyRoot",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "HierarchyRoot",
					DisplayName = "HierarchyRoot",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "HierarchyParallel"
				},
				new StateMachineStateDefinition
				{
					StableId = "HierarchyParallel",
					DisplayName = "HierarchyParallel",
					Kind = StateMachineStateKind.Parallel,
					ParentId = "HierarchyRoot"
				},
				new StateMachineStateDefinition
				{
					StableId = "HierarchyRegion",
					DisplayName = "HierarchyRegion",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "HierarchyParallel"
				}
			}
		};
	}

	private static StateMachineDefinition CreateRootSwitchDefinition()
	{
		return new StateMachineDefinition
		{
			DefinitionId = "root-switch-probe",
			RootStateId = "OldRoot",
			States = 
			{
				new StateMachineStateDefinition
				{
					StableId = "OldRoot",
					DisplayName = "OldRoot",
					Kind = StateMachineStateKind.Compound,
					InitialChildId = "NextRoot"
				},
				new StateMachineStateDefinition
				{
					StableId = "NextRoot",
					DisplayName = "NextRoot",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "OldRoot"
				},
				new StateMachineStateDefinition
				{
					StableId = "RootSibling",
					DisplayName = "RootSibling",
					Kind = StateMachineStateKind.Atomic,
					ParentId = "OldRoot"
				}
			}
		};
	}

	private static bool SnapshotHasActive(StateMachineSnapshot snapshot, string stableId)
	{
		if (snapshot?.ActiveStateIds == null)
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

	private static bool IsLayoutStateCollapsed(StateMachineLayout layout, string stableId)
	{
		bool value = default(bool);
		return layout?.Collapsed != null && layout.Collapsed.TryGetValue(stableId ?? string.Empty, out value) && value;
	}

	private static StateMachineGraphNode FindStateNode(StateMachineGraphEditorSurface surface, string stableId)
	{
		if (surface == null)
		{
			return null;
		}
		foreach (Node child in surface.GetChildren())
		{
			if (child is StateMachineGraphNode stateMachineGraphNode && stateMachineGraphNode.StableId == stableId)
			{
				return stateMachineGraphNode;
			}
		}
		return null;
	}

	private static StateMachineStateDefinition FindState(StateMachineDefinition definition, string stableId)
	{
		if (definition?.States == null)
		{
			return null;
		}
		foreach (StateMachineStateDefinition state in definition.States)
		{
			if (state?.StableId == stableId)
			{
				return state;
			}
		}
		return null;
	}

	private static StateMachineTransitionDefinition FindTransition(StateMachineDefinition definition, string stableId)
	{
		if (definition?.Transitions == null)
		{
			return null;
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition?.StableId == stableId)
			{
				return transition;
			}
		}
		return null;
	}

	private static string FindTransitionId(StateMachineDefinition definition, string sourceStableId, string targetStableId)
	{
		if (definition?.Transitions == null)
		{
			return string.Empty;
		}
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition?.SourceStateId == sourceStableId && transition.TargetStateId == targetStableId)
			{
				return transition.StableId ?? string.Empty;
			}
		}
		return string.Empty;
	}

	private void Require(bool condition, string message)
	{
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
