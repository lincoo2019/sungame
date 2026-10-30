using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.ResourceEditors.GUI;

public partial class ModEditorResourceChoiceBatchRuntimeProbe : Node
{
	private readonly List<string> _failures = new List<string>();

	private readonly Stopwatch _stopwatch = Stopwatch.StartNew();

	private XWUndoRedoManager _history;

	private bool _inspectorUntouched = true;

	public override async void _Ready()
	{
		bool window = false;
		bool collectVisual = false;
		bool collectUndoRedo = false;
		bool collectResponsive = false;
		bool dropVisual = false;
		bool dropCategory = false;
		bool dropObject = false;
		bool dropHandler = false;
		bool dropResponsive = false;
		bool fallingVisual = false;
		bool fallingObject = false;
		bool weightItemObject = false;
		bool fallingResponsive = false;
		bool hiddenStopped = false;
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
			bool flag = await EnterEditorSurface(900);
			Require(flag, "F3 did not initialize the main ModEditor surface.");
			if (!flag)
			{
				Finish();
				return;
			}
			_history = XWEditorInterface.Instance?.GetUndoRedoManager();
			Require(GodotObject.IsInstanceValid(_history), "Global ModEditor UndoRedo manager is unavailable.");
			Window window2 = FindAncestorWindow(XWEditorInterface.Instance?.GetEditorPanel());
			if (GodotObject.IsInstanceValid(window2))
			{
				window2.Size = new Vector2I(820, 720);
				window = true;
			}
			CollectableConfig collectable = new CollectableConfig
			{
				ResourceName = "CollectableVisualProbe",
				saveKey = "collectable_visual_probe",
				config = new ShovelConfig
				{
					ResourceName = "OriginalShovelConfig"
				}
			};
			Resource originalCollectableConfig = collectable.config;
			XWCollectableVisualResourceEditor collectEditor = await OpenEditor<XWCollectableVisualResourceEditor>(collectable, "collectable_editor", "CollectableVisualEditorLayout");
			if (GodotObject.IsInstanceValid(collectEditor))
			{
				Control control = collectEditor.FindChild("CollectableVisualEditorLayout", recursive: true, owned: false) as Control;
				XWGameVisualChoiceCard xWGameVisualChoiceCard = FindVisualCard(collectEditor.FindChild("KindCards", recursive: true, owned: false) as HFlowContainer, "2");
				collectVisual = collectEditor.CollectableKindVisualCardCount == 5 && GodotObject.IsInstanceValid(xWGameVisualChoiceCard) && GodotObject.IsInstanceValid(xWGameVisualChoiceCard.Icon);
				collectResponsive = GodotObject.IsInstanceValid(control) && control.GetCombinedMinimumSize().X <= 500f && control.Size.X <= 820f;
				_history.ClearHistory();
				if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
				{
					xWGameVisualChoiceCard.EmitSignal(BaseButton.SignalName.Pressed);
					await WaitFrames(3);
					bool applied = collectable.config is AwardSettlementConfig && collectable.config.HasMeta("mod_collectable_kind") && collectable.config.GetMeta("mod_collectable_kind").AsString() == "Sun" && _history.HasUndo();
					bool undone = _history.Undo();
					await WaitFrames(3);
					undone = undone && collectable.config == originalCollectableConfig;
					bool redone = _history.Redo();
					await WaitFrames(3);
					redone = redone && collectable.config is AwardSettlementConfig;
					collectUndoRedo = applied && undone && redone;
				}
				_inspectorUntouched &= InspectorUntouched(collectEditor);
				collectEditor.Hide();
				await WaitFrames(3);
				bool collectHidden = !collectEditor.IsCollectablePreviewRendering && !collectEditor.IsProcessing();
				collectEditor.Show();
				XWEditorInterface.Instance.FocusPanel("collectable_editor");
				await WaitFrames(2);
				hiddenStopped = collectHidden;
			}
			Require(collectVisual, "Collectable kind choices are not five icon cards.");
			Require(collectUndoRedo, "Collectable kind card did not round-trip through UndoRedo.");
			Require(collectResponsive, "Collectable visual surface did not fit the 820px probe window.");
			DropItemConfig drop = new DropItemConfig
			{
				ResourceName = "DropItemVisualProbe",
				Name = "DropVisualProbe",
				Category = TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN,
				Id = ObjectManagerConfig.OBJECT.SUN,
				Value = 25,
				Handler = null
			};
			XWDropItemVisualResourceEditor dropEditor = await OpenEditor<XWDropItemVisualResourceEditor>(drop, "drop_item_editor", "DropItemVisualEditorLayout");
			if (GodotObject.IsInstanceValid(dropEditor))
			{
				Control control2 = dropEditor.FindChild("DropItemVisualEditorLayout", recursive: true, owned: false) as Control;
				HFlowContainer host = dropEditor.FindChild("CategoryCards", recursive: true, owned: false) as HFlowContainer;
				HFlowContainer host2 = dropEditor.FindChild("HandlerCards", recursive: true, owned: false) as HFlowContainer;
				_ = dropEditor.FindChild("DropItemObjectLibrary", recursive: true, owned: false) is XWObjectPoolVisualPicker;
				dropVisual = dropEditor.DropCategoryVisualCardCount == 3 && dropEditor.DropHandlerVisualCardCount >= 7 && dropEditor.HasObjectLibrarySearchAndPaging && VisualCardsHaveIcons(host) && VisualCardsHaveIcons(host2);
				dropResponsive = GodotObject.IsInstanceValid(control2) && control2.GetCombinedMinimumSize().X <= 500f && control2.Size.X <= 820f;
				_history.ClearHistory();
				XWGameVisualChoiceCard xWGameVisualChoiceCard2 = FindVisualCard(host, 1.ToString());
				if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard2))
				{
					xWGameVisualChoiceCard2.EmitSignal(BaseButton.SignalName.Pressed);
					await WaitFrames(2);
					bool collectHidden = drop.Category == TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN && _history.HasUndo();
					bool redone = _history.Undo();
					await WaitFrames(3);
					redone = redone && drop.Category == TowerDefenseEnum.DROP_ITEM_CATEGORY.SUN;
					bool undone = _history.Redo();
					await WaitFrames(3);
					dropCategory = collectHidden && redone && undone && drop.Category == TowerDefenseEnum.DROP_ITEM_CATEGORY.COIN;
				}
				XWObjectPoolVisualPicker objectLibrary = dropEditor.FindChild("DropItemObjectLibrary", recursive: true, owned: false) as XWObjectPoolVisualPicker;
				_history.ClearHistory();
				if (GodotObject.IsInstanceValid(objectLibrary))
				{
					objectLibrary.SetSearchText("COIN_GOLD");
					await WaitFrames(2);
					bool undone = SelectCatalogChoice(objectLibrary, "COIN_GOLD");
					await WaitFrames(2);
					bool redone = undone && drop.Id == ObjectManagerConfig.OBJECT.COIN_GOLD && _history.HasUndo();
					bool collectHidden = _history.Undo();
					await WaitFrames(3);
					collectHidden = collectHidden && drop.Id == ObjectManagerConfig.OBJECT.SUN;
					bool applied = _history.Redo();
					await WaitFrames(3);
					dropObject = redone && collectHidden && applied && drop.Id == ObjectManagerConfig.OBJECT.COIN_GOLD;
				}
				host2 = dropEditor.FindChild("HandlerCards", recursive: true, owned: false) as HFlowContainer;
				_history.ClearHistory();
				XWGameVisualChoiceCard xWGameVisualChoiceCard3 = FindVisualCard(host2, "2");
				if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard3))
				{
					xWGameVisualChoiceCard3.EmitSignal(BaseButton.SignalName.Pressed);
					await WaitFrames(2);
					bool applied = drop.Handler is CoinDropItemHandler && _history.HasUndo();
					bool collectHidden = _history.Undo();
					await WaitFrames(3);
					collectHidden = collectHidden && drop.Handler == null;
					bool redone = _history.Redo();
					await WaitFrames(3);
					dropHandler = applied && collectHidden && redone && drop.Handler is CoinDropItemHandler;
				}
				_inspectorUntouched &= InspectorUntouched(dropEditor);
				(dropEditor.FindChild("DropButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(1);
				dropEditor.Hide();
				await WaitFrames(4);
				hiddenStopped &= !dropEditor.IsDropItemPreviewRendering && !dropEditor.IsProcessing();
				dropEditor.Show();
				XWEditorInterface.Instance.FocusPanel("drop_item_editor");
				await WaitFrames(2);
			}
			Require(dropVisual, "DropItem category/object/handler visual selectors are incomplete.");
			Require(dropCategory, "DropItem category visual card did not round-trip through UndoRedo.");
			Require(dropObject, "DropItem searchable object catalog did not round-trip through UndoRedo.");
			Require(dropHandler, "DropItem handler visual card did not round-trip through UndoRedo.");
			Require(dropResponsive, "DropItem visual surface did not fit the 820px probe window.");
			FallingObjectWeightItemConfig firstWeight = new FallingObjectWeightItemConfig
			{
				ResourceName = "FirstWeight",
				item = ObjectManagerConfig.OBJECT.SUN,
				weight = 100
			};
			FallingObjectConfig fallingObjectConfig = new FallingObjectConfig
			{
				ResourceName = "FallingVisualProbe"
			};
			fallingObjectConfig.weightItem.Add(firstWeight);
			fallingObjectConfig.weightItem.Add(new FallingObjectWeightItemConfig
			{
				ResourceName = "CoinWeight",
				item = ObjectManagerConfig.OBJECT.COIN,
				weight = 50
			});
			XWFallingObjectVisualResourceEditor fallingEditor = await OpenEditor<XWFallingObjectVisualResourceEditor>(fallingObjectConfig, "falling_object_editor", "FallingObjectVisualEditorLayout");
			if (GodotObject.IsInstanceValid(fallingEditor))
			{
				Control control3 = fallingEditor.FindChild("FallingObjectVisualEditorLayout", recursive: true, owned: false) as Control;
				HFlowContainer host3 = fallingEditor.FindChild("CategoryGrid", recursive: true, owned: false) as HFlowContainer;
				_ = fallingEditor.FindChild("FallingObjectLibrary", recursive: true, owned: false) is XWObjectPoolVisualPicker;
				fallingVisual = fallingEditor.FallingCategoryVisualCardCount == 7 && fallingEditor.HasFallingObjectLibrarySearchAndPaging && VisualCardsHaveIcons(host3);
				fallingResponsive = GodotObject.IsInstanceValid(control3) && control3.GetCombinedMinimumSize().X <= 500f && control3.Size.X <= 820f;
				FindVisualCard(host3, "货币")?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(2);
				XWObjectPoolVisualPicker objectLibrary = fallingEditor.FindChild("FallingObjectLibrary", recursive: true, owned: false) as XWObjectPoolVisualPicker;
				_history.ClearHistory();
				if (GodotObject.IsInstanceValid(objectLibrary))
				{
					objectLibrary.SetSearchText("COIN_GOLD");
					await WaitFrames(2);
					bool redone = SelectCatalogChoice(objectLibrary, "COIN_GOLD");
					await WaitFrames(2);
					bool collectHidden = redone && firstWeight.item == ObjectManagerConfig.OBJECT.COIN_GOLD && _history.HasUndo();
					bool applied = _history.Undo();
					await WaitFrames(3);
					applied = applied && firstWeight.item == ObjectManagerConfig.OBJECT.SUN;
					bool undone = _history.Redo();
					await WaitFrames(3);
					fallingObject = collectHidden && applied && undone && firstWeight.item == ObjectManagerConfig.OBJECT.COIN_GOLD;
				}
				_inspectorUntouched &= InspectorUntouched(fallingEditor);
				(fallingEditor.FindChild("SimulateButton", recursive: true, owned: false) as Button)?.EmitSignal(BaseButton.SignalName.Pressed);
				await WaitFrames(1);
				int sequenceBeforeHide = fallingEditor.PreviewSequenceGeneration;
				fallingEditor.Hide();
				await WaitFrames(4);
				hiddenStopped &= !fallingEditor.IsFallingPreviewRendering && fallingEditor.PreviewSequenceGeneration > sequenceBeforeHide;
				fallingEditor.Show();
				XWEditorInterface.Instance.FocusPanel("falling_object_editor");
				await WaitFrames(2);
			}
			Require(fallingVisual, "FallingObject category cards or searchable object library are incomplete.");
			Require(fallingObject, "FallingObject weight object catalog did not round-trip through UndoRedo.");
			Require(fallingResponsive, "FallingObject visual surface did not fit the 820px probe window.");
			FallingObjectWeightItemConfig standaloneWeight = new FallingObjectWeightItemConfig
			{
				ResourceName = "StandaloneWeightProbe",
				item = ObjectManagerConfig.OBJECT.SUN,
				weight = 75
			};
			fallingEditor = await OpenEditor<XWFallingObjectVisualResourceEditor>(standaloneWeight, "falling_object_editor", "FallingObjectWeightItemPreview");
			if (GodotObject.IsInstanceValid(fallingEditor))
			{
				XWObjectPoolVisualPicker objectLibrary = fallingEditor.FindChild("WeightItemObjectLibrary", recursive: true, owned: false) as XWObjectPoolVisualPicker;
				_history.ClearHistory();
				if (GodotObject.IsInstanceValid(objectLibrary) && fallingEditor.HasWeightItemLibrarySearchAndPaging)
				{
					objectLibrary.SetSearchText("SUN_JALAPENO");
					await WaitFrames(2);
					bool undone = SelectCatalogChoice(objectLibrary, "SUN_JALAPENO");
					await WaitFrames(2);
					bool applied = undone && standaloneWeight.item == ObjectManagerConfig.OBJECT.SUN_JALAPENO && _history.HasUndo();
					bool collectHidden = _history.Undo();
					await WaitFrames(3);
					collectHidden = collectHidden && standaloneWeight.item == ObjectManagerConfig.OBJECT.SUN;
					bool redone = _history.Redo();
					await WaitFrames(3);
					weightItemObject = applied && collectHidden && redone && standaloneWeight.item == ObjectManagerConfig.OBJECT.SUN_JALAPENO;
				}
				_inspectorUntouched &= InspectorUntouched(fallingEditor);
				fallingEditor.Hide();
				await WaitFrames(3);
				hiddenStopped &= !fallingEditor.IsFallingPreviewRendering;
			}
			Require(weightItemObject, "Standalone weight-item object catalog did not round-trip through UndoRedo.");
			Require(hiddenStopped, "A hidden resource-choice editor kept processing or rendering its preview.");
			Require(_inspectorUntouched, "A target resource editor populated or relied on the raw Inspector.");
			GD.Print($"[MOD_EDITOR_RESOURCE_CHOICE_BATCH_PROBE] window={window} collectVisual={collectVisual} collectUndoRedo={collectUndoRedo} collectResponsive={collectResponsive} dropVisual={dropVisual} dropCategory={dropCategory} dropObject={dropObject} dropHandler={dropHandler} dropResponsive={dropResponsive} fallingVisual={fallingVisual} fallingObject={fallingObject} weightItemObject={weightItemObject} fallingResponsive={fallingResponsive} hiddenStopped={hiddenStopped} inspectorUntouched={_inspectorUntouched} failures={_failures.Count} elapsedMs={_stopwatch.ElapsedMilliseconds}");
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		Finish();
	}

	private async Task<T> OpenEditor<T>(Resource resource, string dockKey, string rootName) where T : XWGenericVisualResourceEditor
	{
		XWEditorInterface.Instance.EditResource(resource);
		XWEditorInterface.Instance.FocusPanel(dockKey);
		for (int i = 0; i < 360; i++)
		{
			T editor = XWEditorInterface.Instance.GetResourceEditor(dockKey) as T;
			if (GodotObject.IsInstanceValid(editor) && editor.IsVisibleInTree() && GodotObject.IsInstanceValid(editor.FindChild(rootName, recursive: true, owned: false)))
			{
				await WaitFrames(2);
				return editor;
			}
			await WaitFrames(1);
		}
		Require(condition: false, dockKey + " did not mount " + rootName + ".");
		return null;
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
				await WaitFrames(3);
				return true;
			}
			await WaitFrames(1);
		}
		return false;
	}

	private static bool InspectorUntouched(XWGenericVisualResourceEditor editor)
	{
		VBoxContainer vBoxContainer = editor?.FindChild("EmbeddedInspectorHost", recursive: true, owned: false) as VBoxContainer;
		if (GodotObject.IsInstanceValid(vBoxContainer) && vBoxContainer.GetChildCount() == 0)
		{
			return editor.FindChild("EmbeddedResourceInspector", recursive: true, owned: false) == null;
		}
		return false;
	}

	private static XWGameVisualChoiceCard FindVisualCard(Node host, string key)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return null;
		}
		foreach (Node child in host.GetChildren())
		{
			if (child is XWGameVisualChoiceCard xWGameVisualChoiceCard && xWGameVisualChoiceCard.ChoiceKey == key)
			{
				return xWGameVisualChoiceCard;
			}
		}
		return null;
	}

	private static bool VisualCardsHaveIcons(Node host)
	{
		if (!GodotObject.IsInstanceValid(host) || host.GetChildCount() == 0)
		{
			return false;
		}
		foreach (Node child in host.GetChildren())
		{
			if (!(child is XWGameVisualChoiceCard xWGameVisualChoiceCard) || !GodotObject.IsInstanceValid(xWGameVisualChoiceCard.Icon))
			{
				return false;
			}
		}
		return true;
	}

	private static bool SelectCatalogChoice(XWObjectPoolVisualPicker picker, string title)
	{
		ItemList itemList = picker?.FindChild("ChoiceList", recursive: true, owned: false) as ItemList;
		if (!GodotObject.IsInstanceValid(itemList))
		{
			return false;
		}
		for (int i = 0; i < itemList.ItemCount; i++)
		{
			if (string.Equals(itemList.GetItemText(i), title, StringComparison.OrdinalIgnoreCase))
			{
				itemList.Select(i);
				itemList.EmitSignal(ItemList.SignalName.ItemSelected, (long)i);
				return true;
			}
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
			GD.PrintErr("[MOD_EDITOR_RESOURCE_CHOICE_BATCH_PROBE_FAILURE] " + message);
		}
	}

	private void Finish()
	{
		foreach (string failure in _failures)
		{
			GD.PrintErr("[MOD_EDITOR_RESOURCE_CHOICE_BATCH_PROBE_FAILURE] " + failure);
		}
		GetTree().Quit((_failures.Count != 0) ? 1 : 0);
	}
}
