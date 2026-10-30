using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ResourceEditors;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorCardBehaviorRuntimeProbe : Node
{
	private const string DraftPath = "user://mod_editor_card_behavior_probe.tres";

	private static readonly StringName RegisteredId = new StringName("probe.card.registered");

	private static readonly StringName LateId = new StringName("probe.card.late");

	private static readonly StringName AddedId = new StringName("probe.card.added");

	private static readonly StringName MissingId = new StringName("probe.card.missing");

	private static readonly StringName WrongTypeId = new StringName("probe.card.wrong_type");

	private readonly List<string> _failures = new List<string>();

	private XWCardVisualResourceEditor _editor;

	private XWUndoRedoManager _history;

	public override async void _Ready()
	{
		bool f3 = false;
		bool visualEntries = false;
		bool typeFilter = false;
		bool authoredDisplayName = false;
		bool idAddRemoveSort = false;
		bool inlineAddRemoveSort = false;
		bool openDirect = false;
		bool undoRedo = false;
		bool saveReload = false;
		bool registryRevision = false;
		bool diagnostics = false;
		bool inspectorUntouched = false;
		bool previewSideEffectFree = false;
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
				Finish(f3, visualEntries, typeFilter, authoredDisplayName, idAddRemoveSort, inlineAddRemoveSort, openDirect, undoRedo, saveReload, registryRevision, diagnostics, inspectorUntouched, previewSideEffectFree);
				return;
			}
			await WaitFrames(2);
			PressF3();
			bool flag = await WaitForEditor(900);
			if (flag)
			{
				flag = await EnterEditorSurface(900);
			}
			f3 = flag;
			Require(f3, "F3 did not initialize the Card behavior editor.");
			if (!f3)
			{
				Finish(f3, visualEntries, typeFilter, authoredDisplayName, idAddRemoveSort, inlineAddRemoveSort, openDirect, undoRedo, saveReload, registryRevision, diagnostics, inspectorUntouched, previewSideEffectFree);
				return;
			}
			ModEditorCardBehaviorProbeDefinition registered = new ModEditorCardBehaviorProbeDefinition
			{
				DefinitionId = RegisteredId,
				InstanceId = new StringName("probe.card.registered.instance")
			};
			ModEditorCardBehaviorProbeDefinition late = new ModEditorCardBehaviorProbeDefinition
			{
				DefinitionId = LateId,
				InstanceId = new StringName("probe.card.late.instance")
			};
			ModEditorCardBehaviorProbeDefinition added = new ModEditorCardBehaviorProbeDefinition
			{
				DefinitionId = AddedId,
				InstanceId = new StringName("probe.card.added.instance")
			};
			ProjectileBehaviorDefinition definition = new ProjectileBehaviorDefinition
			{
				DefinitionId = WrongTypeId,
				InstanceId = new StringName("probe.card.wrong.instance")
			};
			Require(TowerDefenseBehaviorRegistry.RegisterBehavior(RegisteredId, registered), "Registered card behavior fixture could not be registered.");
			Require(TowerDefenseBehaviorRegistry.RegisterBehavior(WrongTypeId, definition), "Wrong-type behavior fixture could not be registered.");
			TowerDefensePacketConfig packet = new TowerDefensePacketConfig
			{
				saveKey = "ModEditorCardBehaviorProbe",
				name = "卡牌行为专项探针",
				behaviorIds = new Array<StringName> { RegisteredId, LateId, MissingId, WrongTypeId },
				behaviors = new Array<CardBehaviorDefinition>
				{
					new TowerDefensePacketChangeCost
					{
						key = "inline-a",
						method = "Increase"
					},
					new TowerDefensePacketChangeCost
					{
						key = "inline-b",
						method = "Decrease"
					}
				}
			};
			Require(ResourceSaver.Save(packet, "user://mod_editor_card_behavior_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Card behavior draft could not be saved.");
			packet = ResourceLoader.Load<TowerDefensePacketConfig>("user://mod_editor_card_behavior_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			Require(GodotObject.IsInstanceValid(packet), "Card behavior draft could not be reloaded.");
			if (!GodotObject.IsInstanceValid(packet))
			{
				Finish(f3, visualEntries, typeFilter, authoredDisplayName, idAddRemoveSort, inlineAddRemoveSort, openDirect, undoRedo, saveReload, registryRevision, diagnostics, inspectorUntouched, previewSideEffectFree);
				return;
			}
			Node inspectorSentinel = new Node
			{
				Name = "CardBehaviorInspectorSentinel"
			};
			AddChild(inspectorSentinel, forceReadableName: false, InternalMode.Disabled);
			XWInspector inspector = XWEditorInterface.Instance.GetInspector() as XWInspector;
			XWEditorInterface.Instance.InspectObject(inspectorSentinel);
			await WaitFrames(2);
			bool condition = InspectorTargets(inspector, inspectorSentinel);
			Require(condition, "Card behavior probe could not establish the Inspector sentinel.");
			ModEditorCardBehaviorProbeDefinition.ResetCounters();
			await OpenCard(packet);
			ItemList idList = FindControl<ItemList>("RegistryBehaviorList");
			ItemList inlineList = FindControl<ItemList>("InlineBehaviorList");
			PopupMenu idPicker = FindControl<PopupMenu>("RegistryBehaviorPicker");
			Label behaviorStatus = FindControl<Label>("RegistryBehaviorStatusLabel");
			LineEdit lineEdit = FindControl<LineEdit>("CustomBehaviorIdEdit");
			Button pickId = FindControl<Button>("PickRegistryBehaviorButton");
			Button button = FindControl<Button>("AddCustomBehaviorIdButton");
			Button refreshRegistry = FindControl<Button>("RefreshRegistryBehaviorButton");
			Button removeId = FindControl<Button>("RemoveRegistryBehaviorButton");
			Button moveIdUp = FindControl<Button>("MoveRegistryBehaviorUpButton");
			Button button2 = FindControl<Button>("MoveRegistryBehaviorDownButton");
			Button button3 = FindControl<Button>("OpenRegistryBehaviorButton");
			Button addInline = FindControl<Button>("AddInlineBehaviorButton");
			Button removeInline = FindControl<Button>("RemoveInlineBehaviorButton");
			Button button4 = FindControl<Button>("MoveInlineBehaviorUpButton");
			Button button5 = FindControl<Button>("MoveInlineBehaviorDownButton");
			Button button6 = FindControl<Button>("OpenInlineBehaviorButton");
			visualEntries = new Node[17]
			{
				idList, inlineList, idPicker, behaviorStatus, lineEdit, pickId, button, refreshRegistry, removeId, moveIdUp,
				button2, button3, addInline, removeInline, button4, button5, button6
			}.All((Node node) => GodotObject.IsInstanceValid(node));
			Require(visualEntries, "Card behavior visual workbench controls are incomplete.");
			if (!visualEntries)
			{
				Finish(f3, visualEntries, typeFilter, authoredDisplayName, idAddRemoveSort, inlineAddRemoveSort, openDirect, undoRedo, saveReload, registryRevision, diagnostics, inspectorUntouched, previewSideEffectFree);
				return;
			}
			string text = BuildListDiagnosticText(idList, behaviorStatus);
			bool missingDiagnostic = text.Contains("断开", StringComparison.Ordinal) && text.Contains(MissingId.ToString(), StringComparison.Ordinal);
			bool wrongTypeDiagnostic = text.Contains("类型错误", StringComparison.Ordinal) && text.Contains(WrongTypeId.ToString(), StringComparison.Ordinal);
			ulong revisionBefore = TowerDefenseBehaviorRegistry.Revision;
			Require(TowerDefenseBehaviorRegistry.RegisterBehavior(LateId, late), "Late card behavior fixture could not be registered.");
			refreshRegistry.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(8);
			ulong revision = TowerDefenseBehaviorRegistry.Revision;
			string text2 = BuildListDiagnosticText(idList, behaviorStatus);
			registryRevision = revision > revisionBefore && behaviorStatus.Text.Contains(revision.ToString(), StringComparison.Ordinal) && text2.Contains($"{LateId}  [已连接]", StringComparison.Ordinal);
			diagnostics = missingDiagnostic && wrongTypeDiagnostic && text2.Contains("断开", StringComparison.Ordinal) && text2.Contains(MissingId.ToString(), StringComparison.Ordinal) && text2.Contains("类型错误", StringComparison.Ordinal);
			Require(registryRevision, "Card behavior UI did not refresh after registry Revision changed.");
			Require(diagnostics, "Missing-ID or wrong-type card behavior diagnostics are incomplete.");
			Require(TowerDefenseBehaviorRegistry.RegisterBehavior(AddedId, added), "Addable card behavior fixture could not be registered.");
			refreshRegistry.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(5);
			_history.ClearHistory();
			pickId.EmitSignal(BaseButton.SignalName.Pressed);
			await WaitFrames(3);
			int addedPickerIndex = FindPopupItem(idPicker, AddedId.ToString());
			int num = FindPopupItem(idPicker, WrongTypeId.ToString());
			bool registryTypeFilter = addedPickerIndex >= 0 && !idPicker.IsItemDisabled(addedPickerIndex) && num >= 0 && idPicker.IsItemDisabled(num);
			if (addedPickerIndex >= 0)
			{
				idPicker.EmitSignal(PopupMenu.SignalName.IndexPressed, addedPickerIndex);
			}
			await WaitFrames(5);
			bool idAdded = addedPickerIndex >= 0 && packet.behaviorIds.Contains(AddedId);
			bool idUndo = _history.HasUndo() && _history.Undo();
			await WaitFrames(3);
			idUndo = idUndo && !packet.behaviorIds.Contains(AddedId);
			bool idRedo = _history.Redo();
			await WaitFrames(3);
			idRedo = idRedo && packet.behaviorIds.Contains(AddedId);
			int addedIndex = packet.behaviorIds.IndexOf(AddedId);
			bool idMoved = addedIndex > 0 && SelectAndPress(idList, addedIndex, moveIdUp);
			await WaitFrames(3);
			idMoved = idMoved && packet.behaviorIds.IndexOf(AddedId) == addedIndex - 1;
			int num2 = packet.behaviorIds.IndexOf(WrongTypeId);
			bool idRemoved = num2 >= 0 && SelectAndPress(idList, num2, removeId);
			await WaitFrames(3);
			idRemoved = idRemoved && !packet.behaviorIds.Contains(WrongTypeId);
			idAddRemoveSort = idAdded && idMoved && idRemoved;
			int inlineBefore = packet.behaviors.Count;
			XWPacketEventSelectorWindow selector = await OpenInlineSelector(addInline);
			authoredDisplayName = SelectorContainsDisplayName(selector, "示例行为中文名");
			Require(authoredDisplayName, "Inline behavior picker did not show the authored Chinese eventName.");
			bool inlineTypeFilter = SelectorContains(selector, "TowerDefensePacketChangeCost") && SelectorContains(selector, "ModEditorCardBehaviorProbeDefinition") && !SelectorContains(selector, "ProjectileBehaviorDefinition") && !SelectorContains(selector, "ProjectileBehaviorYMoveSin");
			bool inlineSelection = SelectInlineType(selector, "TowerDefensePacketChangeCost");
			await WaitFrames(6);
			bool inlineAdded = inlineSelection && packet.behaviors.Count == inlineBefore + 1 && packet.behaviors[packet.behaviors.Count - 1] is TowerDefensePacketChangeCost;
			bool inlineUndo = _history.HasUndo() && _history.Undo();
			await WaitFrames(3);
			inlineUndo = inlineUndo && packet.behaviors.Count == inlineBefore;
			bool inlineRedo = _history.Redo();
			await WaitFrames(3);
			inlineRedo = inlineRedo && packet.behaviors.Count == inlineBefore + 1;
			await OpenCard(packet);
			inlineList = FindControl<ItemList>("InlineBehaviorList");
			button4 = FindControl<Button>("MoveInlineBehaviorUpButton");
			removeInline = FindControl<Button>("RemoveInlineBehaviorButton");
			int inlineAddedIndex = packet.behaviors.Count - 1;
			bool inlineMoved = SelectAndPress(inlineList, inlineAddedIndex, button4);
			await WaitFrames(3);
			inlineMoved = inlineMoved && packet.behaviors.Count == inlineBefore + 1 && packet.behaviors[inlineAddedIndex - 1] is TowerDefensePacketChangeCost;
			bool inlineRemoved = SelectAndPress(inlineList, packet.behaviors.Count - 1, removeInline);
			await WaitFrames(3);
			inlineRemoved = inlineRemoved && packet.behaviors.Count == inlineBefore;
			inlineAddRemoveSort = inlineAdded && inlineMoved && inlineRemoved;
			undoRedo = idUndo && idRedo && inlineUndo && inlineRedo;
			typeFilter = registryTypeFilter && inlineTypeFilter;
			Require(typeFilter, "Inline behavior picker accepted a non-card type or omitted card behavior types.");
			idList = FindControl<ItemList>("RegistryBehaviorList");
			button3 = FindControl<Button>("OpenRegistryBehaviorButton");
			int num3 = packet.behaviorIds.IndexOf(RegisteredId);
			flag = num3 >= 0 && SelectAndPress(idList, num3, button3);
			if (flag)
			{
				flag = await WaitForOpenedResource(registered, 180);
			}
			bool openedRegistered = flag;
			await OpenCard(packet);
			FindControl<ItemList>("RegistryBehaviorList");
			inlineList = FindControl<ItemList>("InlineBehaviorList");
			button6 = FindControl<Button>("OpenInlineBehaviorButton");
			CardBehaviorDefinition cardBehaviorDefinition = ((packet.behaviors.Count > 0) ? packet.behaviors[0] : null);
			flag = GodotObject.IsInstanceValid(cardBehaviorDefinition) && SelectAndPress(inlineList, 0, button6);
			if (flag)
			{
				flag = await WaitForOpenedResource(cardBehaviorDefinition, 180);
			}
			bool flag2 = flag;
			GD.Print($"[MOD_EDITOR_CARD_BEHAVIOR_OPEN_PROBE] registered={openedRegistered} inline={flag2}");
			openDirect = openedRegistered && flag2;
			Require(openDirect, "Registered or inline card behavior did not open for direct editing.");
			await OpenCard(packet);
			Require(ResourceSaver.Save(packet, "user://mod_editor_card_behavior_probe.tres", ResourceSaver.SaverFlags.None) == Error.Ok, "Edited card behavior resource could not be saved.");
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("user://mod_editor_card_behavior_probe.tres", "", ResourceLoader.CacheMode.Ignore);
			saveReload = GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.behaviorIds.SequenceEqual(packet.behaviorIds) && towerDefensePacketConfig.behaviors.Count == packet.behaviors.Count && towerDefensePacketConfig.behaviors.All((CardBehaviorDefinition behavior) => behavior != null);
			Require(saveReload, "Card behavior collections did not survive save/reload.");
			inspectorUntouched = InspectorTargets(inspector, inspectorSentinel) && _editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
			Require(inspectorUntouched, "Card behavior authoring touched a raw or embedded Inspector.");
			previewSideEffectFree = ModEditorCardBehaviorProbeDefinition.CreateRuntimeCalls == 0 && ModEditorCardBehaviorProbeDefinition.ModifyCostCalls == 0 && ModEditorCardBehaviorProbeDefinition.BoundCalls == 0 && ModEditorCardBehaviorProbeDefinition.PressedCalls == 0 && ModEditorCardBehaviorProbeDefinition.UseSucceededCalls == 0;
			Require(previewSideEffectFree, $"Card editor executed behavior gameplay code: create={ModEditorCardBehaviorProbeDefinition.CreateRuntimeCalls}, cost={ModEditorCardBehaviorProbeDefinition.ModifyCostCalls}, bound={ModEditorCardBehaviorProbeDefinition.BoundCalls}, pressed={ModEditorCardBehaviorProbeDefinition.PressedCalls}, use={ModEditorCardBehaviorProbeDefinition.UseSucceededCalls}.");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish(f3, visualEntries, typeFilter, authoredDisplayName, idAddRemoveSort, inlineAddRemoveSort, openDirect, undoRedo, saveReload, registryRevision, diagnostics, inspectorUntouched, previewSideEffectFree);
	}

	private async Task OpenCard(TowerDefensePacketConfig packet)
	{
		XWEditorInterface.Instance.EditResource(packet, XWResourceEditContext.ForRoot(packet, "user://mod_editor_card_behavior_probe.tres", "card_editor"));
		XWEditorInterface.Instance.FocusPanel("card_editor");
		await WaitFrames(12);
	}

	private async Task<bool> ActivatePickerItem(Button addButton, ItemList pickerList, string token)
	{
		await ShowPicker(addButton);
		int num = FindItem(pickerList, token);
		if (num < 0)
		{
			return false;
		}
		pickerList.Select(num);
		pickerList.EmitSignal(ItemList.SignalName.ItemActivated, num);
		await WaitFrames(5);
		return true;
	}

	private async Task ShowPicker(Button addButton)
	{
		addButton.EmitSignal(BaseButton.SignalName.Pressed);
		await WaitFrames(4);
	}

	private async Task<XWPacketEventSelectorWindow> OpenInlineSelector(Button addButton)
	{
		addButton.EmitSignal(BaseButton.SignalName.Pressed);
		for (int frame = 0; frame < 120; frame++)
		{
			XWPacketEventSelectorWindow xWPacketEventSelectorWindow = _editor.FindChildren("*", "", recursive: true, owned: false).OfType<XWPacketEventSelectorWindow>().LastOrDefault((XWPacketEventSelectorWindow candidate) => GodotObject.IsInstanceValid(candidate) && candidate.Visible);
			if (GodotObject.IsInstanceValid(xWPacketEventSelectorWindow) && xWPacketEventSelectorWindow.FindChild("ChoiceList", recursive: true, owned: false) is VBoxContainer)
			{
				return xWPacketEventSelectorWindow;
			}
			await WaitFrames(1);
		}
		return null;
	}

	private static bool SelectorContains(XWPacketEventSelectorWindow selector, string token)
	{
		VBoxContainer vBoxContainer = selector?.FindChild("ChoiceList", recursive: true, owned: false) as VBoxContainer;
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			return false;
		}
		foreach (Node child in vBoxContainer.GetChildren())
		{
			Label label = child.FindChild("ClassName", recursive: true, owned: false) as Label;
			if (GodotObject.IsInstanceValid(label) && label.Text.Contains(token, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool SelectorContainsDisplayName(XWPacketEventSelectorWindow selector, string token)
	{
		VBoxContainer vBoxContainer = selector?.FindChild("ChoiceList", recursive: true, owned: false) as VBoxContainer;
		if (!GodotObject.IsInstanceValid(vBoxContainer))
		{
			return false;
		}
		foreach (Node child in vBoxContainer.GetChildren())
		{
			Label label = child.FindChild("EventName", recursive: true, owned: false) as Label;
			if (GodotObject.IsInstanceValid(label) && label.Text.Contains(token, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}

	private static bool SelectInlineType(XWPacketEventSelectorWindow selector, string token)
	{
		VBoxContainer vBoxContainer = selector?.FindChild("ChoiceList", recursive: true, owned: false) as VBoxContainer;
		Button button = selector?.FindChild("ConfirmButton", recursive: true, owned: false) as Button;
		if (!GodotObject.IsInstanceValid(vBoxContainer) || !GodotObject.IsInstanceValid(button))
		{
			return false;
		}
		foreach (Node child in vBoxContainer.GetChildren())
		{
			Label label = child.FindChild("ClassName", recursive: true, owned: false) as Label;
			Button button2 = child.FindChild("SelectSurface", recursive: true, owned: false) as Button;
			if (GodotObject.IsInstanceValid(label) && GodotObject.IsInstanceValid(button2) && label.Text.Contains(token, StringComparison.OrdinalIgnoreCase))
			{
				button2.EmitSignal(BaseButton.SignalName.Pressed);
				button.EmitSignal(BaseButton.SignalName.Pressed);
				return true;
			}
		}
		return false;
	}

	private static bool SelectAndPress(ItemList list, int index, Button button)
	{
		if (!GodotObject.IsInstanceValid(list) || !GodotObject.IsInstanceValid(button) || index < 0 || index >= list.ItemCount)
		{
			return false;
		}
		list.Select(index);
		list.EmitSignal(ItemList.SignalName.ItemSelected, index);
		button.EmitSignal(BaseButton.SignalName.Pressed);
		return true;
	}

	private static int FindItem(ItemList list, string token)
	{
		if (!GodotObject.IsInstanceValid(list))
		{
			return -1;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			string obj = list.GetItemText(i) ?? "";
			string text = list.GetItemMetadata(i).ToString();
			if (obj.Contains(token, StringComparison.OrdinalIgnoreCase) || text.Contains(token, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private static bool ListContains(ItemList list, string token)
	{
		return FindItem(list, token) >= 0;
	}

	private static int FindPopupItem(PopupMenu popup, string token)
	{
		if (!GodotObject.IsInstanceValid(popup))
		{
			return -1;
		}
		for (int i = 0; i < popup.ItemCount; i++)
		{
			if ((popup.GetItemText(i) ?? "").Contains(token, StringComparison.OrdinalIgnoreCase))
			{
				return i;
			}
		}
		return -1;
	}

	private static string BuildListDiagnosticText(ItemList list, Label status)
	{
		List<string> list2 = new List<string> { status?.Text ?? "" };
		if (!GodotObject.IsInstanceValid(list))
		{
			return string.Join("\n", list2);
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			list2.Add(list.GetItemText(i) ?? "");
			list2.Add(list.GetItemTooltip(i) ?? "");
		}
		return string.Join("\n", list2);
	}

	private async Task<bool> WaitForOpenedResource(Resource expected, int maxFrames)
	{
		XWResourceEditorRegistry.TryGetEditor(expected, expected?.ResourcePath ?? "", out var descriptor);
		for (int frame = 0; frame < maxFrames; frame++)
		{
			foreach (string item in new string[6]
			{
				descriptor?.DockKey ?? "",
				"resource_editor",
				"card_editor",
				"packet_event_editor",
				"character_component_editor",
				"state_machine_editor"
			}.Where((string key) => !string.IsNullOrWhiteSpace(key)).Distinct<string>(StringComparer.Ordinal))
			{
				if (XWEditorInterface.Instance?.GetResourceEditor(item) is XWGenericVisualResourceEditor xWGenericVisualResourceEditor && GodotObject.IsInstanceValid(xWGenericVisualResourceEditor.ActiveResource) && xWGenericVisualResourceEditor.ActiveResource.GetInstanceId() == expected.GetInstanceId())
				{
					return true;
				}
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> WaitForEditor(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			XWEditorInterface instance = XWEditorInterface.Instance;
			if (instance?.GetInspector() is XWInspector && instance.GetResourceEditor("card_editor") is XWCardVisualResourceEditor editor)
			{
				_editor = editor;
				_history = instance.GetUndoRedoManager();
				return GodotObject.IsInstanceValid(_history);
			}
			await WaitFrames(1);
		}
		return false;
	}

	private async Task<bool> EnterEditorSurface(int maxFrames)
	{
		for (int frame = 0; frame < maxFrames; frame++)
		{
			Control obj = XWEditorInterface.Instance?.GetEditorPanel();
			Node instance = obj?.FindChild("LoadingOverlay", recursive: true, owned: false);
			Control control = obj?.FindChild("ProjectManagerPanel", recursive: true, owned: false) as Control;
			if (GodotObject.IsInstanceValid(obj) && !GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(control))
			{
				control.Hide();
				XWEditorInterface.Instance.FocusPanel("card_editor");
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

	private static bool InspectorTargets(XWInspector inspector, GodotObject expected)
	{
		if (GodotObject.IsInstanceValid(inspector) && GodotObject.IsInstanceValid(inspector.CurrentObject) && GodotObject.IsInstanceValid(expected))
		{
			return inspector.CurrentObject.GetInstanceId() == expected.GetInstanceId();
		}
		return false;
	}

	private static void PressF3()
	{
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
			GD.PrintErr("[MOD_EDITOR_CARD_BEHAVIOR_PROBE_FAILURE] " + message);
		}
	}

	private void Finish(bool f3, bool visualEntries, bool typeFilter, bool authoredDisplayName, bool idAddRemoveSort, bool inlineAddRemoveSort, bool openDirect, bool undoRedo, bool saveReload, bool registryRevision, bool diagnostics, bool inspectorUntouched, bool previewSideEffectFree)
	{
		TowerDefenseBehaviorRegistry.UnregisterBehavior(RegisteredId);
		TowerDefenseBehaviorRegistry.UnregisterBehavior(LateId);
		TowerDefenseBehaviorRegistry.UnregisterBehavior(AddedId);
		TowerDefenseBehaviorRegistry.UnregisterBehavior(WrongTypeId);
		GD.Print($"[MOD_EDITOR_CARD_BEHAVIOR_PROBE] f3={f3} visualEntries={visualEntries} typeFilter={typeFilter} authoredDisplayName={authoredDisplayName} idAddRemoveSort={idAddRemoveSort} inlineAddRemoveSort={inlineAddRemoveSort} openDirect={openDirect} undoRedo={undoRedo} saveReload={saveReload} registryRevision={registryRevision} diagnostics={diagnostics} inspectorUntouched={inspectorUntouched} previewSideEffectFree={previewSideEffectFree} createRuntimeCalls={ModEditorCardBehaviorProbeDefinition.CreateRuntimeCalls} modifyCostCalls={ModEditorCardBehaviorProbeDefinition.ModifyCostCalls} boundCalls={ModEditorCardBehaviorProbeDefinition.BoundCalls} pressedCalls={ModEditorCardBehaviorProbeDefinition.PressedCalls} useSucceededCalls={ModEditorCardBehaviorProbeDefinition.UseSucceededCalls} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_CARD_BEHAVIOR_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
