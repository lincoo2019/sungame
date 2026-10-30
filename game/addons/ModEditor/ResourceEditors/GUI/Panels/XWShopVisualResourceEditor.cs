using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ResourceEditors.GUI.GameplayLogic;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWShopVisualResourceEditor : XWGenericVisualResourceEditor
{
	private enum ShopEditScope
	{
		Shop,
		Page,
		Item,
		Stage
	}

	private sealed class ShopOptionItem
	{
		public string Name = "";

		public string SourceLabel = "";

		public string SourcePath = "";

		public string Description = "";
	}

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShopVisualEditorLayout.tscn";

	private const string PurchaseSimulationScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShopPurchaseSimulation.tscn";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _purchaseSimulationScene;

	private ShopConfig _editingShop;

	private XWVisualPropertyBinding _shopPropertyBinding;

	private ShopEditScope _editScope;

	private Shop _runtimeShopPreview;

	private Label _summaryLabel;

	private Label _pageSummaryLabel;

	private Label _itemSummaryLabel;

	private Label _stageSummaryLabel;

	private ItemList _pageList;

	private ItemList _itemList;

	private ItemList _stageList;

	private TextureRect _stageTexturePreview;

	private Label _stagePreviewTitle;

	private Label _stagePreviewMeta;

	private Label _stagePreviewStock;

	private SpinBox _purchaseBalanceSpin;

	private SpinBox _purchaseOwnedSpin;

	private SpinBox _purchaseQuantitySpin;

	private ProgressBar _purchaseStockProgress;

	private Label _purchasePreviewResult;

	private LineEdit _itemTypeEdit;

	private LineEdit _resourceNameEdit;

	private CheckButton _localToSceneCheck;

	private Button _jsonSourceButton;

	private FileDialog _jsonSourceDialog;

	private Label _dataStatus;

	private Button _clearJsonButton;

	private ConfirmationDialog _clearJsonConfirmationDialog;

	private OptionButton _stageSaveTypeOption;

	private XWVisualSegmentedOption _stageSaveTypeVisualChoices;

	private LineEdit _stageSaveKeyEdit;

	private XWResourcePicker _stageTexturePicker;

	private LineEdit _stageDescribeEdit;

	private LineEdit _stageNpcTalkEdit;

	private OptionButton _stageCostTypeOption;

	private XWVisualSegmentedOption _stageCostTypeVisualChoices;

	private SpinBox _stageCostSpin;

	private SpinBox _stageOpenMinSpin;

	private SpinBox _stageOpenMaxSpin;

	private SpinBox _stageAddNumSpin;

	private int _selectedPageIndex = -1;

	private int _selectedItemIndex = -1;

	private int _selectedStageIndex = -1;

	private ShopPageConfig _selectedPageIdentity;

	private ShopItemConfig _selectedItemIdentity;

	private ShopItemStageConfig _selectedStageIdentity;

	private ShopItemConfig _activeItemTypeEditTarget;

	private ShopItemStageConfig _activeStageEditTarget;

	private string _activeStageEditProperty = "";

	private bool _updatingControls;

	private XWGameplayResourcePickerWindow _shopOptionSelectorWindow;

	private readonly List<ShopOptionItem> _shopOptionSelectorOptions = new List<ShopOptionItem>();

	private Action<ShopOptionItem> _shopOptionSelectorConfirmed;

	private const string CharacterResourceRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string TalkResourceRegistryPath = "res://Asset/Config/Npc/TalkResource.json";

	public override void _ExitTree()
	{
		DisposeStageVisualChoices();
		_shopPropertyBinding?.Dispose();
		_shopPropertyBinding = null;
		if (GodotObject.IsInstanceValid(_shopOptionSelectorWindow))
		{
			_shopOptionSelectorWindow.QueueFree();
		}
		_shopOptionSelectorWindow = null;
		if (GodotObject.IsInstanceValid(_clearJsonConfirmationDialog))
		{
			_clearJsonConfirmationDialog.QueueFree();
		}
		_clearJsonConfirmationDialog = null;
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeStageVisualChoices();
		_shopPropertyBinding?.Dispose();
		_shopPropertyBinding = null;
		_editingShop = null;
		_runtimeShopPreview = null;
		_selectedPageIdentity = null;
		_selectedItemIdentity = null;
		_selectedStageIdentity = null;
		_activeItemTypeEditTarget = null;
		_activeStageEditTarget = null;
		_activeStageEditProperty = "";
		_shopPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnShopPropertyEdited);
		ShopConfig shopConfig = CreateShopPreviewAdapter(base.CurrentResource);
		if (!GodotObject.IsInstanceValid(shopConfig))
		{
			_shopPropertyBinding.Dispose();
			_shopPropertyBinding = null;
		}
		else
		{
			_editingShop = shopConfig;
			RenderShopEditor(shopConfig);
		}
	}

	private ShopConfig CreateShopPreviewAdapter(Resource resource)
	{
		if (!(resource is ShopConfig result))
		{
			if (!(resource is ShopPageConfig item))
			{
				if (!(resource is ShopItemConfig item2))
				{
					if (resource is ShopItemStageConfig item3)
					{
						_editScope = ShopEditScope.Stage;
						ShopItemConfig shopItemConfig = new ShopItemConfig
						{
							type = "Item"
						};
						shopItemConfig.stageList.Add(item3);
						ShopPageConfig shopPageConfig = new ShopPageConfig();
						shopPageConfig.itemList.Add(shopItemConfig);
						return new ShopConfig
						{
							pageList = { shopPageConfig }
						};
					}
					return null;
				}
				_editScope = ShopEditScope.Item;
				ShopPageConfig shopPageConfig2 = new ShopPageConfig();
				shopPageConfig2.itemList.Add(item2);
				return new ShopConfig
				{
					pageList = { shopPageConfig2 }
				};
			}
			_editScope = ShopEditScope.Page;
			return new ShopConfig
			{
				pageList = { item }
			};
		}
		_editScope = ShopEditScope.Shop;
		return result;
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteShopVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteShopVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(ShopConfig)) && !(type == typeof(ShopPageConfig)) && !(type == typeof(ShopItemConfig)))
		{
			return type == typeof(ShopItemStageConfig);
		}
		return true;
	}

	private void RenderShopEditor(ShopConfig shop)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShopVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindShopLayout(vBoxContainer, shop);
				NormalizeSelection();
				RefreshAll();
				AddSummaryRows(shop);
			}
		}
	}

	private void BindShopLayout(VBoxContainer root, ShopConfig shop)
	{
		_resourceNameEdit = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToSceneCheck = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_shopPropertyBinding.BindText(_resourceNameEdit, base.CurrentResource, "resource_name", RefreshResourceMetadata, this, "RefreshShopEditorFromHistory");
		_shopPropertyBinding.BindToggle(_localToSceneCheck, base.CurrentResource, "resource_local_to_scene", RefreshResourceMetadata, this, "RefreshShopEditorFromHistory");
		_jsonSourceButton = root.GetNode<Button>("%JsonSourceButton");
		_jsonSourceDialog = root.GetNode<FileDialog>("%JsonSourceDialog");
		_dataStatus = root.GetNode<Label>("%DataStatus");
		_clearJsonButton = root.GetNode<Button>("%ClearJsonButton");
		_jsonSourceButton.Pressed += OpenJsonSourceDialog;
		_jsonSourceDialog.FileSelected += ApplyJsonSource;
		_clearJsonButton.Pressed += ConfirmClearJsonSource;
		_clearJsonConfirmationDialog = new ConfirmationDialog
		{
			Title = "清空商店页面",
			DialogText = "确认清除 JSON 来源并清空全部商店页面吗？此操作可以撤销。",
			OkButtonText = "清空页面"
		};
		_clearJsonConfirmationDialog.Confirmed += ClearJsonSource;
		root.AddChild(_clearJsonConfirmationDialog, forceReadableName: false, InternalMode.Disabled);
		_runtimeShopPreview = root.GetNode<Shop>("%RuntimeShop");
		_runtimeShopPreview.EditPreviewConfig(shop);
		_summaryLabel = root.GetNode<Label>("%ShopSummaryLabel");
		_summaryLabel.Text = BuildSummary(shop);
		root.GetNode<Button>("%AddPageTopButton").Pressed += AddShopPage;
		root.GetNode<Button>("%SaveButton").Pressed += base.SaveCurrentResource;
		_pageSummaryLabel = root.GetNode<Label>("%PageSummaryLabel");
		root.GetNode<Button>("%PageAddButton").Pressed += AddShopPage;
		root.GetNode<Button>("%PageRemoveButton").Pressed += RemoveSelectedShopPage;
		root.GetNode<Button>("%PageUpButton").Pressed += delegate
		{
			MoveSelectedShopPage(-1);
		};
		root.GetNode<Button>("%PageDownButton").Pressed += delegate
		{
			MoveSelectedShopPage(1);
		};
		_pageList = root.GetNode<ItemList>("%ShopPageList");
		_pageList.ItemSelected += delegate(long index)
		{
			SelectShopPage((int)index);
		};
		_itemSummaryLabel = root.GetNode<Label>("%ItemSummaryLabel");
		_itemTypeEdit = root.GetNode<LineEdit>("%ShopItemTypeLineEdit");
		_itemTypeEdit.FocusEntered += BeginItemTypeEdit;
		_itemTypeEdit.TextChanged += PreviewItemType;
		_itemTypeEdit.TextSubmitted += delegate(string text)
		{
			if (!_updatingControls)
			{
				SetItemType(text);
			}
		};
		_itemTypeEdit.FocusExited += delegate
		{
			if (!_updatingControls)
			{
				SetItemType(_itemTypeEdit.Text);
			}
		};
		root.GetNode<Button>("%ItemTypeButton").Pressed += delegate
		{
			SetItemType("Item");
		};
		root.GetNode<Button>("%PacketTypeButton").Pressed += delegate
		{
			SetItemType("Packet");
		};
		root.GetNode<Button>("%FeatureTypeButton").Pressed += delegate
		{
			SetItemType("Feature");
		};
		root.GetNode<Button>("%ItemAddButton").Pressed += AddShopItem;
		root.GetNode<Button>("%ItemRemoveButton").Pressed += RemoveSelectedShopItem;
		root.GetNode<Button>("%ItemUpButton").Pressed += delegate
		{
			MoveSelectedShopItem(-1);
		};
		root.GetNode<Button>("%ItemDownButton").Pressed += delegate
		{
			MoveSelectedShopItem(1);
		};
		_itemList = root.GetNode<ItemList>("%ShopItemList");
		_itemList.ItemSelected += delegate(long index)
		{
			SelectShopItem((int)index);
		};
		_stageSummaryLabel = root.GetNode<Label>("%StageSummaryLabel");
		root.GetNode<Button>("%StageAddButton").Pressed += AddShopStage;
		root.GetNode<Button>("%StageRemoveButton").Pressed += RemoveSelectedShopStage;
		root.GetNode<Button>("%StageUpButton").Pressed += delegate
		{
			MoveSelectedShopStage(-1);
		};
		root.GetNode<Button>("%StageDownButton").Pressed += delegate
		{
			MoveSelectedShopStage(1);
		};
		_stageList = root.GetNode<ItemList>("%ShopStageList");
		_stageList.ItemSelected += delegate(long index)
		{
			SelectShopStage((int)index);
		};
		BindStageFields(root);
		_stageTexturePreview = root.GetNode<TextureRect>("%StageTexturePreview");
		_stagePreviewTitle = root.GetNode<Label>("%StagePreviewTitle");
		_stagePreviewMeta = root.GetNode<Label>("%StagePreviewMeta");
		_stagePreviewStock = root.GetNode<Label>("%StagePreviewStock");
		root.GetNode<VBoxContainer>("%PurchaseSimulationHost").AddChild(CreatePurchaseSimulationPanel(), forceReadableName: false, InternalMode.Disabled);
		ConfigureScopeControls(root);
	}

	private void ConfigureScopeControls(VBoxContainer root)
	{
		bool enabled = _editScope == ShopEditScope.Shop;
		bool flag = _editScope == ShopEditScope.Shop;
		ShopEditScope editScope = _editScope;
		bool flag2 = (uint)editScope <= 1u;
		bool enabled2 = flag2;
		bool flag3 = _editScope != ShopEditScope.Stage;
		bool enabled3 = _editScope != ShopEditScope.Stage;
		SetScopeButton(root, "%AddPageTopButton", enabled, "该资源本身不是完整商店，不能增删预览包装页");
		string[] array = new string[4] { "%PageAddButton", "%PageRemoveButton", "%PageUpButton", "%PageDownButton" };
		foreach (string nodePath in array)
		{
			SetScopeButton(root, nodePath, enabled, "当前只编辑这一页，不能改动预览包装层");
		}
		array = new string[4] { "%ItemAddButton", "%ItemRemoveButton", "%ItemUpButton", "%ItemDownButton" };
		foreach (string nodePath2 in array)
		{
			SetScopeButton(root, nodePath2, enabled2, "当前资源边界不允许增删或移动商品");
		}
		array = new string[4] { "%StageAddButton", "%StageRemoveButton", "%StageUpButton", "%StageDownButton" };
		foreach (string nodePath3 in array)
		{
			SetScopeButton(root, nodePath3, enabled3, "当前只编辑这一商品阶段，不能改动预览包装层");
		}
		array = new string[3] { "%ItemTypeButton", "%PacketTypeButton", "%FeatureTypeButton" };
		foreach (string nodePath4 in array)
		{
			SetScopeButton(root, nodePath4, flag3, "商品类型属于上级商品资源");
		}
		if (GodotObject.IsInstanceValid(_itemTypeEdit))
		{
			_itemTypeEdit.Editable = flag3;
			_itemTypeEdit.TooltipText = (flag3 ? "编辑商品类型" : "商品类型属于上级商品资源");
		}
		if (GodotObject.IsInstanceValid(_jsonSourceButton))
		{
			_jsonSourceButton.Disabled = !flag;
			_jsonSourceButton.TooltipText = (flag ? "选择 ShopConfig JSON 来源" : "仅完整 ShopConfig 可设置 JSON 来源；当前是嵌套资源编辑范围。");
		}
		if (GodotObject.IsInstanceValid(_clearJsonButton))
		{
			_clearJsonButton.Disabled = !flag;
			_clearJsonButton.TooltipText = (flag ? "清除 JSON 来源并清空页面" : "仅完整 ShopConfig 可清除 JSON 来源；当前是嵌套资源编辑范围。");
		}
		RefreshDataStatus();
	}

	private static void SetScopeButton(Node root, string nodePath, bool enabled, string disabledReason)
	{
		Button nodeOrNull = root.GetNodeOrNull<Button>(nodePath);
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			nodeOrNull.Disabled = !enabled;
			nodeOrNull.TooltipText = (enabled ? "" : disabledReason);
		}
	}

	private void BindStageFields(VBoxContainer root)
	{
		_stageSaveTypeOption = root.GetNode<OptionButton>("%StageSaveTypeOption");
		string[] array = new string[2] { "Feature", "TowerDefensePacket" };
		foreach (string label in array)
		{
			_stageSaveTypeOption.AddItem(label);
		}
		_stageSaveTypeVisualChoices = new XWVisualSegmentedOption(_stageSaveTypeOption, root.GetNode<HFlowContainer>("%StageSaveTypeVisualChoices"));
		_stageSaveTypeVisualChoices.Rebuild();
		_stageSaveTypeOption.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				SetStageField("saveType", Variant.From<string>(_stageSaveTypeOption.GetItemText((int)index)));
			}
		};
		_stageSaveKeyEdit = root.GetNode<LineEdit>("%StageSaveKeyLineEdit");
		BindContinuousStageText(_stageSaveKeyEdit, "saveKey", delegate(Variant value)
		{
			SetStageField("saveKey", value);
		});
		root.GetNode<Button>("%ChoosePacketButton").Pressed += delegate
		{
			ShowShopOptionSelector("选择卡片", BuildPacketKeySelectionOptions(), delegate(ShopOptionItem item)
			{
				SetPacketStageFields("TowerDefensePacket", item.Name);
			});
		};
		_stageTexturePicker = XWResourcePicker.Create();
		_stageTexturePicker.Name = "StageTexturePicker";
		_stageTexturePicker.Setup("Texture2D");
		_stageTexturePicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_stageTexturePicker.ResourceChanged += delegate(Resource resource)
		{
			if (!_updatingControls)
			{
				SetStageField("texture", Variant.From(in resource));
			}
		};
		root.GetNode<HBoxContainer>("%TexturePickerHost").AddChild(_stageTexturePicker, forceReadableName: false, InternalMode.Disabled);
		_stageDescribeEdit = root.GetNode<LineEdit>("%StageDescribeLineEdit");
		BindContinuousStageText(_stageDescribeEdit, "describe", delegate(Variant value)
		{
			SetStageField("describe", value);
		});
		_stageNpcTalkEdit = root.GetNode<LineEdit>("%StageNpcTalkLineEdit");
		BindContinuousStageText(_stageNpcTalkEdit, "npcTalk", delegate(Variant value)
		{
			SetStageField("npcTalk", value);
		});
		root.GetNode<Button>("%ChooseTalkButton").Pressed += delegate
		{
			ShowShopOptionSelector("选择对话", BuildNpcTalkSelectionOptions(), delegate(ShopOptionItem item)
			{
				SetStageField("npcTalk", Variant.From(in item.Name));
			});
		};
		_stageCostTypeOption = root.GetNode<OptionButton>("%StageCostTypeOption");
		_stageCostTypeOption.AddItem("Coin");
		_stageCostTypeVisualChoices = new XWVisualSegmentedOption(_stageCostTypeOption, root.GetNode<HFlowContainer>("%StageCostTypeVisualChoices"));
		_stageCostTypeVisualChoices.Rebuild();
		_stageCostTypeOption.ItemSelected += delegate(long index)
		{
			if (!_updatingControls)
			{
				SetStageField("type", Variant.From<string>(_stageCostTypeOption.GetItemText((int)index)));
			}
		};
		_stageCostSpin = root.GetNode<SpinBox>("%StageCostSpin");
		_stageOpenMinSpin = root.GetNode<SpinBox>("%StageOpenMinSpin");
		_stageOpenMaxSpin = root.GetNode<SpinBox>("%StageOpenMaxSpin");
		_stageAddNumSpin = root.GetNode<SpinBox>("%StageAddNumSpin");
		BindContinuousStageNumber(_stageCostSpin, "cost", delegate(Variant value)
		{
			SetStageField("cost", value);
		});
		BindContinuousStageNumber(_stageOpenMinSpin, "openMinNum", delegate(Variant value)
		{
			SetStageField("openMinNum", value);
		});
		BindContinuousStageNumber(_stageOpenMaxSpin, "openMaxNum", delegate(Variant value)
		{
			SetStageField("openMaxNum", value);
		});
		BindContinuousStageNumber(_stageAddNumSpin, "addNum", delegate(Variant value)
		{
			SetStageField("addNum", value);
		});
	}

	private void BindContinuousStageText(LineEdit control, string propertyName, Action<Variant> commit)
	{
		control.FocusEntered += delegate
		{
			BeginStageFieldEdit(propertyName);
		};
		control.TextChanged += delegate(string text)
		{
			if (!_updatingControls)
			{
				PreviewStageField(propertyName, Variant.From(in text));
			}
		};
		control.TextSubmitted += delegate(string text)
		{
			if (!_updatingControls)
			{
				commit(Variant.From(in text));
			}
		};
		control.FocusExited += delegate
		{
			if (!_updatingControls)
			{
				commit(Variant.From<string>(control.Text));
			}
		};
	}

	private void BindContinuousStageNumber(SpinBox control, string propertyName, Action<Variant> commit)
	{
		control.FocusEntered += delegate
		{
			BeginStageFieldEdit(propertyName);
		};
		control.ValueChanged += delegate(double value)
		{
			if (!_updatingControls)
			{
				PreviewStageField(propertyName, Variant.From<int>((int)Math.Round(value)));
			}
		};
		control.FocusExited += delegate
		{
			if (!_updatingControls)
			{
				commit(Variant.From<int>((int)Math.Round(control.Value)));
			}
		};
	}

	private Control CreatePurchaseSimulationPanel()
	{
		if (_purchaseSimulationScene == null)
		{
			_purchaseSimulationScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWShopPurchaseSimulation.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_purchaseSimulationScene))
		{
			return new Label
			{
				Text = "商店购买模拟场景加载失败"
			};
		}
		Control control = _purchaseSimulationScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		_purchaseBalanceSpin = control.GetNode<SpinBox>("Layout/Inputs/Balance");
		_purchaseOwnedSpin = control.GetNode<SpinBox>("Layout/Inputs/Owned");
		_purchaseQuantitySpin = control.GetNode<SpinBox>("Layout/Inputs/Quantity");
		_purchaseStockProgress = control.GetNode<ProgressBar>("Layout/StockProgress");
		_purchasePreviewResult = control.GetNode<Label>("Layout/Result");
		_purchaseBalanceSpin.ValueChanged += delegate
		{
			RefreshPurchaseSimulation();
		};
		_purchaseOwnedSpin.ValueChanged += delegate
		{
			RefreshPurchaseSimulation();
		};
		_purchaseQuantitySpin.ValueChanged += delegate
		{
			RefreshPurchaseSimulation();
		};
		RefreshPurchaseSimulation();
		return control;
	}

	private void RefreshPurchaseSimulation()
	{
		if (!GodotObject.IsInstanceValid(_purchasePreviewResult))
		{
			return;
		}
		ShopItemStageConfig selectedStage = GetSelectedStage();
		if (!GodotObject.IsInstanceValid(selectedStage))
		{
			_purchasePreviewResult.Text = "选择商品阶段后可模拟购买。";
			_purchasePreviewResult.Modulate = new Color(0.72f, 0.75f, 0.8f);
			return;
		}
		long num = (long)(_purchaseBalanceSpin?.Value ?? 0.0);
		long num2 = (long)(_purchaseOwnedSpin?.Value ?? 0.0);
		long num3 = Math.Max(1L, (long)(_purchaseQuantitySpin?.Value ?? 1.0));
		long num4 = Math.Max(0L, selectedStage.cost) * num3;
		long num5 = num2 + selectedStage.addNum * num3;
		bool num6 = num2 >= selectedStage.openMinNum;
		bool flag = selectedStage.openMaxNum < 0 || num5 <= selectedStage.openMaxNum;
		bool flag2 = num >= num4;
		bool flag3 = num6 && flag && flag2;
		if (GodotObject.IsInstanceValid(_purchaseStockProgress))
		{
			_purchaseStockProgress.MinValue = 0.0;
			_purchaseStockProgress.MaxValue = Math.Max(1, selectedStage.openMaxNum);
			_purchaseStockProgress.Value = Math.Clamp(num5, 0L, Math.Max(1, selectedStage.openMaxNum));
		}
		string text = ((!num6) ? $"尚未达到开放数量 {selectedStage.openMinNum}" : ((!flag) ? $"购买后数量 {num5} 超过上限 {selectedStage.openMaxNum}" : ((!flag2) ? $"余额不足，还差 {num4 - num} {selectedStage.type}" : $"将花费 {num4} {selectedStage.type}，数量 {num2} → {num5}，余额 {num - num4}")));
		_purchasePreviewResult.Text = (flag3 ? ("✓ 可以购买 · " + text) : ("✗ 无法购买 · " + text));
		_purchasePreviewResult.Modulate = (flag3 ? new Color(0.47f, 0.92f, 0.56f) : new Color(1f, 0.54f, 0.45f));
	}

	private void SelectShopPage(int index)
	{
		_selectedPageIndex = index;
		_selectedItemIndex = 0;
		_selectedStageIndex = 0;
		NormalizeSelection();
		CaptureSelectionIdentity();
		RefreshAll();
	}

	private void SelectShopItem(int index)
	{
		_selectedItemIndex = index;
		_selectedStageIndex = 0;
		NormalizeSelection();
		CaptureSelectionIdentity();
		RefreshAll();
	}

	private void SelectShopStage(int index)
	{
		_selectedStageIndex = index;
		NormalizeSelection();
		CaptureSelectionIdentity();
		RefreshAll();
	}

	private void AddShopPage()
	{
		if (_editingShop != null && _editScope == ShopEditScope.Shop && _shopPropertyBinding != null)
		{
			ShopPageConfig shopPageConfig = new ShopPageConfig();
			Array<ShopPageConfig> array = CopyPages(_editingShop.pageList);
			array.Add(shopPageConfig);
			_selectedPageIndex = array.Count - 1;
			_selectedItemIndex = -1;
			_selectedStageIndex = -1;
			_selectedPageIdentity = shopPageConfig;
			_selectedItemIdentity = null;
			_selectedStageIdentity = null;
			_shopPropertyBinding.SetValue(_editingShop, "pageList", array, "添加商店页面", this, "RefreshShopEditorFromHistory");
		}
	}

	private void RemoveSelectedShopPage()
	{
		if (_editScope == ShopEditScope.Shop && _editingShop?.pageList != null && _selectedPageIndex >= 0 && _selectedPageIndex < _editingShop.pageList.Count)
		{
			Array<ShopPageConfig> array = CopyPages(_editingShop.pageList);
			array.RemoveAt(_selectedPageIndex);
			_selectedPageIndex = Math.Min(_selectedPageIndex, array.Count - 1);
			_selectedItemIndex = 0;
			_selectedStageIndex = 0;
			_shopPropertyBinding.SetValue(_editingShop, "pageList", array, "删除商店页面", this, "RefreshShopEditorFromHistory");
		}
	}

	private void MoveSelectedShopPage(int direction)
	{
		if (_editScope == ShopEditScope.Shop && _editingShop?.pageList != null && _selectedPageIndex >= 0 && direction != 0)
		{
			int num = Math.Clamp(_selectedPageIndex + direction, 0, _editingShop.pageList.Count - 1);
			if (num != _selectedPageIndex)
			{
				Array<ShopPageConfig> array = CopyPages(_editingShop.pageList);
				ShopPageConfig item = array[_selectedPageIndex];
				array.RemoveAt(_selectedPageIndex);
				array.Insert(num, item);
				_selectedPageIndex = num;
				_shopPropertyBinding.SetValue(_editingShop, "pageList", array, "调整商店页面顺序", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void AddShopItem()
	{
		ShopEditScope editScope = _editScope;
		if ((uint)(editScope - 2) > 1u)
		{
			ShopPageConfig selectedPage = GetSelectedPage();
			if (!GodotObject.IsInstanceValid(selectedPage))
			{
				AddShopPage();
				selectedPage = GetSelectedPage();
			}
			if (GodotObject.IsInstanceValid(selectedPage))
			{
				ShopItemConfig shopItemConfig = new ShopItemConfig
				{
					type = "Item"
				};
				Array<ShopItemConfig> array = CopyItems(selectedPage.itemList);
				array.Add(shopItemConfig);
				_selectedItemIndex = array.Count - 1;
				_selectedStageIndex = -1;
				_selectedItemIdentity = shopItemConfig;
				_selectedStageIdentity = null;
				_shopPropertyBinding.SetValue(selectedPage, "itemList", array, "添加商店商品", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void RemoveSelectedShopItem()
	{
		ShopEditScope editScope = _editScope;
		if ((uint)(editScope - 2) > 1u)
		{
			ShopPageConfig selectedPage = GetSelectedPage();
			if (GodotObject.IsInstanceValid(selectedPage) && selectedPage.itemList != null && _selectedItemIndex >= 0 && _selectedItemIndex < selectedPage.itemList.Count)
			{
				Array<ShopItemConfig> array = CopyItems(selectedPage.itemList);
				array.RemoveAt(_selectedItemIndex);
				_selectedItemIndex = Math.Min(_selectedItemIndex, array.Count - 1);
				_selectedStageIndex = 0;
				_shopPropertyBinding.SetValue(selectedPage, "itemList", array, "删除商店商品", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void MoveSelectedShopItem(int direction)
	{
		ShopEditScope editScope = _editScope;
		if ((uint)(editScope - 2) <= 1u)
		{
			return;
		}
		ShopPageConfig selectedPage = GetSelectedPage();
		if (GodotObject.IsInstanceValid(selectedPage) && selectedPage.itemList != null && _selectedItemIndex >= 0 && direction != 0)
		{
			int num = Math.Clamp(_selectedItemIndex + direction, 0, selectedPage.itemList.Count - 1);
			if (num != _selectedItemIndex)
			{
				Array<ShopItemConfig> array = CopyItems(selectedPage.itemList);
				ShopItemConfig item = array[_selectedItemIndex];
				array.RemoveAt(_selectedItemIndex);
				array.Insert(num, item);
				_selectedItemIndex = num;
				_shopPropertyBinding.SetValue(selectedPage, "itemList", array, "调整商店商品顺序", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void AddShopStage()
	{
		if (_editScope != ShopEditScope.Stage)
		{
			ShopItemConfig selectedItem = GetSelectedItem();
			if (!GodotObject.IsInstanceValid(selectedItem))
			{
				AddShopItem();
				selectedItem = GetSelectedItem();
			}
			if (GodotObject.IsInstanceValid(selectedItem))
			{
				ShopItemStageConfig shopItemStageConfig = new ShopItemStageConfig
				{
					saveType = "Feature",
					type = "Coin",
					openMaxNum = 1,
					addNum = 1
				};
				Array<ShopItemStageConfig> array = CopyStages(selectedItem.stageList);
				array.Add(shopItemStageConfig);
				_selectedStageIndex = array.Count - 1;
				_selectedStageIdentity = shopItemStageConfig;
				_shopPropertyBinding.SetValue(selectedItem, "stageList", array, "添加商店阶段", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void RemoveSelectedShopStage()
	{
		if (_editScope != ShopEditScope.Stage)
		{
			ShopItemConfig selectedItem = GetSelectedItem();
			if (GodotObject.IsInstanceValid(selectedItem) && selectedItem.stageList != null && _selectedStageIndex >= 0 && _selectedStageIndex < selectedItem.stageList.Count)
			{
				Array<ShopItemStageConfig> array = CopyStages(selectedItem.stageList);
				array.RemoveAt(_selectedStageIndex);
				_selectedStageIndex = Math.Min(_selectedStageIndex, array.Count - 1);
				_shopPropertyBinding.SetValue(selectedItem, "stageList", array, "删除商店阶段", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void MoveSelectedShopStage(int direction)
	{
		if (_editScope == ShopEditScope.Stage)
		{
			return;
		}
		ShopItemConfig selectedItem = GetSelectedItem();
		if (GodotObject.IsInstanceValid(selectedItem) && selectedItem.stageList != null && _selectedStageIndex >= 0 && direction != 0)
		{
			int num = Math.Clamp(_selectedStageIndex + direction, 0, selectedItem.stageList.Count - 1);
			if (num != _selectedStageIndex)
			{
				Array<ShopItemStageConfig> array = CopyStages(selectedItem.stageList);
				ShopItemStageConfig item = array[_selectedStageIndex];
				array.RemoveAt(_selectedStageIndex);
				array.Insert(num, item);
				_selectedStageIndex = num;
				_shopPropertyBinding.SetValue(selectedItem, "stageList", array, "调整商店阶段顺序", this, "RefreshShopEditorFromHistory");
			}
		}
	}

	private void SetItemType(string value)
	{
		if (_editScope != ShopEditScope.Stage && _shopPropertyBinding != null)
		{
			ShopItemConfig shopItemConfig = (GodotObject.IsInstanceValid(_activeItemTypeEditTarget) ? _activeItemTypeEditTarget : GetSelectedItem());
			if (GodotObject.IsInstanceValid(shopItemConfig))
			{
				_shopPropertyBinding.SetValue(shopItemConfig, "type", value.StripEdges(), "修改商品类型", this, "RefreshShopEditorFromHistory");
				_activeItemTypeEditTarget = null;
			}
		}
	}

	private void BeginItemTypeEdit()
	{
		if (!_updatingControls && _editScope != ShopEditScope.Stage)
		{
			ShopItemConfig selectedItem = GetSelectedItem();
			if (GodotObject.IsInstanceValid(selectedItem))
			{
				_activeItemTypeEditTarget = selectedItem;
				_shopPropertyBinding?.BeginEdit(selectedItem, "type");
			}
		}
	}

	private void PreviewItemType(string value)
	{
		if (!_updatingControls && _editScope != ShopEditScope.Stage)
		{
			ShopItemConfig shopItemConfig = (GodotObject.IsInstanceValid(_activeItemTypeEditTarget) ? _activeItemTypeEditTarget : GetSelectedItem());
			if (GodotObject.IsInstanceValid(shopItemConfig))
			{
				_shopPropertyBinding?.PreviewValue(shopItemConfig, "type", value.StripEdges());
			}
		}
	}

	private void SetStageField(string propertyName, Variant value)
	{
		ShopItemStageConfig shopItemStageConfig = ((string.Equals(_activeStageEditProperty, propertyName, StringComparison.Ordinal) && GodotObject.IsInstanceValid(_activeStageEditTarget)) ? _activeStageEditTarget : GetSelectedStage());
		if (GodotObject.IsInstanceValid(shopItemStageConfig) && !string.IsNullOrWhiteSpace(propertyName) && _shopPropertyBinding != null)
		{
			_shopPropertyBinding.SetValue(shopItemStageConfig, propertyName, value, "修改商品阶段 " + propertyName, this, "RefreshShopEditorFromHistory");
			_activeStageEditTarget = null;
			_activeStageEditProperty = "";
		}
	}

	private void BeginStageFieldEdit(string propertyName)
	{
		if (!_updatingControls)
		{
			ShopItemStageConfig selectedStage = GetSelectedStage();
			if (GodotObject.IsInstanceValid(selectedStage))
			{
				_activeStageEditTarget = selectedStage;
				_activeStageEditProperty = propertyName;
				_shopPropertyBinding?.BeginEdit(selectedStage, propertyName);
			}
		}
	}

	private void PreviewStageField(string propertyName, Variant value)
	{
		if (!_updatingControls)
		{
			ShopItemStageConfig shopItemStageConfig = ((string.Equals(_activeStageEditProperty, propertyName, StringComparison.Ordinal) && GodotObject.IsInstanceValid(_activeStageEditTarget)) ? _activeStageEditTarget : GetSelectedStage());
			if (GodotObject.IsInstanceValid(shopItemStageConfig))
			{
				_shopPropertyBinding?.PreviewValue(shopItemStageConfig, propertyName, value);
			}
		}
	}

	private void SetPacketStageFields(string saveType, string saveKey)
	{
		ShopItemStageConfig selectedStage = GetSelectedStage();
		if (GodotObject.IsInstanceValid(selectedStage) && (!string.Equals(selectedStage.saveType, saveType, StringComparison.Ordinal) || !string.Equals(selectedStage.saveKey, saveKey, StringComparison.Ordinal)))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				selectedStage.Set("saveType", saveType);
				selectedStage.Set("saveKey", saveKey);
			}
			else
			{
				Variant value = selectedStage.Get("saveType");
				Variant value2 = selectedStage.Get("saveKey");
				xWUndoRedoManager.CreateAction("选择商店卡片");
				xWUndoRedoManager.AddDoProperty(selectedStage, "saveType", saveType);
				xWUndoRedoManager.AddDoProperty(selectedStage, "saveKey", saveKey);
				xWUndoRedoManager.AddUndoProperty(selectedStage, "saveType", value);
				xWUndoRedoManager.AddUndoProperty(selectedStage, "saveKey", value2);
				xWUndoRedoManager.AddDoMethod(this, "RefreshShopEditorFromHistory");
				xWUndoRedoManager.AddUndoMethod(this, "RefreshShopEditorFromHistory");
				xWUndoRedoManager.CommitAction();
			}
			OnShopPropertyEdited(committed: true);
		}
	}

	private void ShowShopOptionSelector(string title, List<ShopOptionItem> options, Action<ShopOptionItem> confirmed)
	{
		EnsureShopOptionSelectorWindow();
		if (GodotObject.IsInstanceValid(_shopOptionSelectorWindow))
		{
			_shopOptionSelectorWindow.Title = title;
			_shopOptionSelectorOptions.Clear();
			_shopOptionSelectorOptions.AddRange(options ?? new List<ShopOptionItem>());
			_shopOptionSelectorConfirmed = confirmed;
			_shopOptionSelectorWindow.OpenChoices(title, "从游戏内容与当前 Mod 资源中选择，单击查看来源", "", BuildShopOptionChoices(), SelectShopOptionChoice, "选用此项");
		}
	}

	private void EnsureShopOptionSelectorWindow()
	{
		if (!GodotObject.IsInstanceValid(_shopOptionSelectorWindow))
		{
			_shopOptionSelectorWindow = XWGameplayResourcePickerWindow.Create();
			if (GodotObject.IsInstanceValid(_shopOptionSelectorWindow))
			{
				AddChild(_shopOptionSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			}
		}
	}

	private List<XWGameplayResourceChoice> BuildShopOptionChoices()
	{
		List<XWGameplayResourceChoice> list = new List<XWGameplayResourceChoice>();
		foreach (ShopOptionItem shopOptionSelectorOption in _shopOptionSelectorOptions)
		{
			if (shopOptionSelectorOption != null && !string.IsNullOrWhiteSpace(shopOptionSelectorOption.Name))
			{
				list.Add(new XWGameplayResourceChoice(shopOptionSelectorOption.Name, shopOptionSelectorOption.Name, shopOptionSelectorOption.SourcePath ?? "", null, XWGameplayResourceKind.Resource, (shopOptionSelectorOption.SourceLabel ?? "").Contains("Mod", StringComparison.OrdinalIgnoreCase)));
			}
		}
		return list;
	}

	private void SelectShopOptionChoice(XWGameplayResourceChoice choice)
	{
		if (!(choice == null))
		{
			ShopOptionItem shopOptionItem = _shopOptionSelectorOptions.Find((ShopOptionItem option) => option != null && string.Equals(option.Name, choice.Key, StringComparison.OrdinalIgnoreCase) && string.Equals(option.SourcePath ?? "", choice.ResourcePath ?? "", StringComparison.OrdinalIgnoreCase)) ?? _shopOptionSelectorOptions.Find((ShopOptionItem option) => option != null && string.Equals(option.Name, choice.Key, StringComparison.OrdinalIgnoreCase));
			if (shopOptionItem != null)
			{
				RegisterProjectSelectionPath(shopOptionItem.SourcePath);
				_shopOptionSelectorConfirmed?.Invoke(shopOptionItem);
			}
		}
	}

	private List<ShopOptionItem> BuildPacketKeySelectionOptions()
	{
		List<ShopOptionItem> options = new List<ShopOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Character/CharacterResource.json", "游戏内卡片", "内置 Character/Packet");
		CollectProjectResourceKeyOptions(options, "Resources/Cards", "Mod 卡片");
		CollectProjectResourceKeyOptions(options, "Resources/Characters", "Mod 角色");
		return SortShopOptions(options);
	}

	private List<ShopOptionItem> BuildNpcTalkSelectionOptions()
	{
		List<ShopOptionItem> options = new List<ShopOptionItem>();
		LoadRegisteredKeysFromJson(options, "res://Asset/Config/Npc/TalkResource.json", "游戏内对话", "内置 NPC Talk");
		CollectProjectResourceKeyOptions(options, "Resources/NpcTalks", "Mod 对话");
		return SortShopOptions(options);
	}

	private static void LoadRegisteredKeysFromJson(List<ShopOptionItem> options, string registryPath, string sourceLabel, string description)
	{
		if (options == null || string.IsNullOrWhiteSpace(registryPath) || !ResourceLoader.Exists(registryPath))
		{
			return;
		}
		try
		{
			Json json = ResourceLoader.Load<Json>(registryPath, null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
			{
				return;
			}
			Dictionary dictionary = json.Data.AsGodotDictionary();
			foreach (Variant key in dictionary.Keys)
			{
				AddShopOption(options, key.AsString(), sourceLabel, registryPath, description);
				if (dictionary[key].VariantType != Variant.Type.Dictionary)
				{
					continue;
				}
				Variant valueOrDefault = dictionary[key].AsGodotDictionary().GetValueOrDefault("Packet", new Dictionary());
				if (valueOrDefault.VariantType != Variant.Type.Dictionary)
				{
					continue;
				}
				foreach (Variant key2 in valueOrDefault.AsGodotDictionary().Keys)
				{
					AddShopOption(options, key2.AsString(), sourceLabel, registryPath, "Packet of " + key.AsString());
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Shop selector registry load failed: " + registryPath + " " + ex.Message);
		}
	}

	private static void CollectProjectResourceKeyOptions(List<ShopOptionItem> options, string relativeFolder, string sourceLabel)
	{
		string currentProjectPathForShopEditor = GetCurrentProjectPathForShopEditor();
		if (options == null || string.IsNullOrWhiteSpace(currentProjectPathForShopEditor) || string.IsNullOrWhiteSpace(relativeFolder))
		{
			return;
		}
		string path = Path.Combine(currentProjectPathForShopEditor, relativeFolder.Replace('/', Path.DirectorySeparatorChar));
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			switch (Path.GetExtension(item).ToLowerInvariant())
			{
			case ".tres":
			case ".res":
			case ".json":
			case ".csv":
			{
				string text = ReadSaveKeyFromResourceFile(item);
				if (string.IsNullOrWhiteSpace(text))
				{
					text = Path.GetFileNameWithoutExtension(item);
				}
				AddShopOption(options, text, sourceLabel, item, ToProjectRelativeDisplayPath(item, currentProjectPathForShopEditor));
				break;
			}
			}
		}
	}

	private void RefreshResourceMetadata()
	{
		if (GodotObject.IsInstanceValid(_summaryLabel) && GodotObject.IsInstanceValid(_editingShop))
		{
			_summaryLabel.Text = BuildSummary(_editingShop);
		}
	}

	private void OpenJsonSourceDialog()
	{
		if (_editScope == ShopEditScope.Shop && GodotObject.IsInstanceValid(_jsonSourceDialog))
		{
			_jsonSourceDialog.PopupCenteredClamped(new Vector2I(900, 620), 0.9f);
		}
	}

	private void ApplyJsonSource(string path)
	{
		if (_editScope != ShopEditScope.Shop || !(base.CurrentResource is ShopConfig shop) || string.IsNullOrWhiteSpace(path))
		{
			return;
		}
		Json json = ResourceLoader.Load<Json>(path, "", ResourceLoader.CacheMode.Ignore);
		if (!TryValidateShopJson(json, out var reason))
		{
			if (GodotObject.IsInstanceValid(_dataStatus))
			{
				_dataStatus.Text = reason;
			}
			XWEditorInterface.Instance?.ShowToast(reason, 1);
			return;
		}
		ShopConfig shopConfig;
		try
		{
			shopConfig = new ShopConfig
			{
				data = json
			};
		}
		catch (Exception ex)
		{
			string text = "商店 JSON 解析失败：" + ex.Message;
			if (GodotObject.IsInstanceValid(_dataStatus))
			{
				_dataStatus.Text = text;
			}
			XWEditorInterface.Instance?.ShowToast(text, 1);
			return;
		}
		SetShopDataWithPageSnapshot(shop, json, CopyPages(shopConfig.pageList), "更换商店 JSON 来源");
	}

	private static bool TryValidateShopJson(Json source, out string reason)
	{
		if (!GodotObject.IsInstanceValid(source))
		{
			reason = "无法加载商店 JSON。";
			return false;
		}
		if (source.Data.VariantType != Variant.Type.Dictionary)
		{
			reason = "商店 JSON 根节点必须是 Dictionary。";
			return false;
		}
		Variant valueOrDefault = source.Data.AsGodotDictionary().GetValueOrDefault("Page");
		if (valueOrDefault.VariantType != Variant.Type.Array)
		{
			reason = "商店 JSON 的 Page 必须是 Array。";
			return false;
		}
		Godot.Collections.Array array = valueOrDefault.AsGodotArray();
		for (int i = 0; i < array.Count; i++)
		{
			Variant variant = array[i];
			if (variant.VariantType != Variant.Type.Dictionary)
			{
				reason = $"商店 JSON 的 Page[{i}] 必须是 Dictionary。";
				return false;
			}
			Variant valueOrDefault2 = variant.AsGodotDictionary().GetValueOrDefault("Item");
			if (valueOrDefault2.VariantType != Variant.Type.Array)
			{
				reason = $"商店 JSON 的 Page[{i}].Item 必须是 Array。";
				return false;
			}
			Godot.Collections.Array array2 = valueOrDefault2.AsGodotArray();
			for (int j = 0; j < array2.Count; j++)
			{
				Variant variant2 = array2[j];
				if (variant2.VariantType != Variant.Type.Dictionary)
				{
					reason = $"商店 JSON 的 Page[{i}].Item[{j}] 必须是 Dictionary。";
					return false;
				}
				Variant valueOrDefault3 = variant2.AsGodotDictionary().GetValueOrDefault("Stage");
				if (valueOrDefault3.VariantType == Variant.Type.Nil)
				{
					continue;
				}
				if (valueOrDefault3.VariantType != Variant.Type.Array)
				{
					reason = $"商店 JSON 的 Page[{i}].Item[{j}].Stage 必须是 Array。";
					return false;
				}
				Godot.Collections.Array array3 = valueOrDefault3.AsGodotArray();
				for (int k = 0; k < array3.Count; k++)
				{
					if (array3[k].VariantType != Variant.Type.Dictionary)
					{
						reason = $"商店 JSON 的 Page[{i}].Item[{j}].Stage[{k}] 必须是 Dictionary。";
						return false;
					}
				}
			}
		}
		reason = "";
		return true;
	}

	private void ConfirmClearJsonSource()
	{
		if (_editScope == ShopEditScope.Shop && GodotObject.IsInstanceValid(_clearJsonConfirmationDialog))
		{
			_clearJsonConfirmationDialog.PopupCentered(new Vector2I(520, 220));
		}
	}

	private void ClearJsonSource()
	{
		if (_editScope != ShopEditScope.Shop || !(base.CurrentResource is ShopConfig shopConfig))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(shopConfig.data))
		{
			Array<ShopPageConfig> pageList = shopConfig.pageList;
			if (pageList == null || pageList.Count == 0)
			{
				return;
			}
		}
		SetShopDataWithPageSnapshot(shopConfig, null, new Array<ShopPageConfig>(), "清空商店页面");
	}

	private void SetShopDataWithPageSnapshot(ShopConfig shop, Json nextData, Array<ShopPageConfig> nextPages, string actionName)
	{
		if (GodotObject.IsInstanceValid(shop))
		{
			Json from = shop.data;
			Array<ShopPageConfig> array = CopyPages(shop.pageList);
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			if (xWUndoRedoManager == null)
			{
				shop.Set("pageList", new Array<ShopPageConfig>());
				shop.Set("data", nextData);
				shop.Set("pageList", nextPages);
			}
			else
			{
				Array<ShopPageConfig> array2 = new Array<ShopPageConfig>();
				Array<ShopPageConfig> array3 = new Array<ShopPageConfig>();
				xWUndoRedoManager.CreateAction(actionName);
				xWUndoRedoManager.AddDoProperty(shop, "pageList", array2);
				xWUndoRedoManager.AddDoProperty(shop, "data", Variant.From(in nextData));
				xWUndoRedoManager.AddDoProperty(shop, "pageList", nextPages);
				xWUndoRedoManager.AddUndoProperty(shop, "pageList", array3);
				xWUndoRedoManager.AddUndoProperty(shop, "data", Variant.From(in from));
				xWUndoRedoManager.AddUndoProperty(shop, "pageList", array);
				xWUndoRedoManager.AddDoMethod(this, "RefreshShopEditorFromHistory");
				xWUndoRedoManager.AddUndoMethod(this, "RefreshShopEditorFromHistory");
				xWUndoRedoManager.CommitAction();
			}
			_selectedPageIndex = ((nextPages.Count <= 0) ? (-1) : 0);
			_selectedItemIndex = 0;
			_selectedStageIndex = 0;
			OnShopPropertyEdited(committed: true);
		}
	}

	private void RefreshDataStatus()
	{
		if (GodotObject.IsInstanceValid(_dataStatus))
		{
			if (_editScope != ShopEditScope.Shop)
			{
				_dataStatus.Text = "嵌套资源范围：JSON 来源只可在完整 ShopConfig 中编辑。";
			}
			else
			{
				_dataStatus.Text = (GodotObject.IsInstanceValid(_editingShop?.data) ? ("JSON 来源：" + _editingShop.data.ResourcePath.GetFile()) : "无 JSON 来源；当前页面为手工配置。");
			}
		}
	}

	private void RefreshAll()
	{
		if (_editingShop == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			NormalizeSelection();
			if (GodotObject.IsInstanceValid(_summaryLabel))
			{
				_summaryLabel.Text = BuildSummary(_editingShop);
			}
			RefreshPageList();
			RefreshItemList();
			RefreshStageList();
			RefreshStageControls();
			RefreshDataStatus();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshPageList()
	{
		if (GodotObject.IsInstanceValid(_pageList))
		{
			_pageList.Clear();
			int valueOrDefault = (_editingShop?.pageList?.Count).GetValueOrDefault();
			if (valueOrDefault == 0)
			{
				_pageList.AddItem("没有页面");
			}
			for (int i = 0; i < valueOrDefault; i++)
			{
				int valueOrDefault2 = (_editingShop.pageList[i]?.itemList?.Count).GetValueOrDefault();
				_pageList.AddItem($"页面 {i + 1}  商品 {valueOrDefault2}");
			}
			if (_selectedPageIndex >= 0 && _selectedPageIndex < valueOrDefault)
			{
				_pageList.Select(_selectedPageIndex);
			}
			if (GodotObject.IsInstanceValid(_pageSummaryLabel))
			{
				_pageSummaryLabel.Text = $"页面数: {valueOrDefault}";
			}
		}
	}

	private void RefreshItemList()
	{
		if (GodotObject.IsInstanceValid(_itemList))
		{
			_itemList.Clear();
			ShopPageConfig selectedPage = GetSelectedPage();
			int valueOrDefault = (selectedPage?.itemList?.Count).GetValueOrDefault();
			if (valueOrDefault == 0)
			{
				_itemList.AddItem("没有商品");
			}
			for (int i = 0; i < valueOrDefault; i++)
			{
				ShopItemConfig shopItemConfig = selectedPage.itemList[i];
				_itemList.AddItem($"{i + 1}. {EmptyToPlaceholder(shopItemConfig?.type)}  阶段 {(shopItemConfig?.stageList?.Count).GetValueOrDefault()}");
			}
			if (_selectedItemIndex >= 0 && _selectedItemIndex < valueOrDefault)
			{
				_itemList.Select(_selectedItemIndex);
			}
			if (GodotObject.IsInstanceValid(_itemSummaryLabel))
			{
				_itemSummaryLabel.Text = $"当前页面: {DisplayIndex(_selectedPageIndex)}  商品数: {valueOrDefault}";
			}
			if (GodotObject.IsInstanceValid(_itemTypeEdit))
			{
				_itemTypeEdit.Text = GetSelectedItem()?.type ?? "";
			}
		}
	}

	private void RefreshStageList()
	{
		if (GodotObject.IsInstanceValid(_stageList))
		{
			_stageList.Clear();
			ShopItemConfig selectedItem = GetSelectedItem();
			int valueOrDefault = (selectedItem?.stageList?.Count).GetValueOrDefault();
			if (valueOrDefault == 0)
			{
				_stageList.AddItem("没有阶段");
			}
			for (int i = 0; i < valueOrDefault; i++)
			{
				ShopItemStageConfig shopItemStageConfig = selectedItem.stageList[i];
				_stageList.AddItem($"{i + 1}. {EmptyToPlaceholder(shopItemStageConfig?.saveKey)}  {shopItemStageConfig?.cost ?? 0} {EmptyToPlaceholder(shopItemStageConfig?.type)}");
			}
			if (_selectedStageIndex >= 0 && _selectedStageIndex < valueOrDefault)
			{
				_stageList.Select(_selectedStageIndex);
			}
			if (GodotObject.IsInstanceValid(_stageSummaryLabel))
			{
				_stageSummaryLabel.Text = $"当前商品: {DisplayIndex(_selectedItemIndex)}  阶段数: {valueOrDefault}";
			}
		}
	}

	private void RefreshStageControls()
	{
		ShopItemStageConfig selectedStage = GetSelectedStage();
		bool flag = GodotObject.IsInstanceValid(selectedStage);
		if (GodotObject.IsInstanceValid(_stageSaveTypeOption))
		{
			SelectOptionByText(_stageSaveTypeOption, flag ? selectedStage.saveType : "Feature");
		}
		_stageSaveTypeVisualChoices?.RefreshSelection();
		if (GodotObject.IsInstanceValid(_stageSaveKeyEdit))
		{
			_stageSaveKeyEdit.Text = (flag ? selectedStage.saveKey : "");
		}
		if (GodotObject.IsInstanceValid(_stageTexturePicker))
		{
			_stageTexturePicker.SetEditedResource(flag ? selectedStage.texture : null);
		}
		if (GodotObject.IsInstanceValid(_stageDescribeEdit))
		{
			_stageDescribeEdit.Text = (flag ? selectedStage.describe : "");
		}
		if (GodotObject.IsInstanceValid(_stageNpcTalkEdit))
		{
			_stageNpcTalkEdit.Text = (flag ? selectedStage.npcTalk : "");
		}
		if (GodotObject.IsInstanceValid(_stageCostTypeOption))
		{
			SelectOptionByText(_stageCostTypeOption, flag ? selectedStage.type : "Coin");
		}
		_stageCostTypeVisualChoices?.RefreshSelection();
		SetSpinValue(_stageCostSpin, flag ? selectedStage.cost : 0);
		SetSpinValue(_stageOpenMinSpin, flag ? selectedStage.openMinNum : 0);
		SetSpinValue(_stageOpenMaxSpin, (!flag) ? 1 : selectedStage.openMaxNum);
		SetSpinValue(_stageAddNumSpin, (!flag) ? 1 : selectedStage.addNum);
		if (GodotObject.IsInstanceValid(_stageTexturePreview))
		{
			_stageTexturePreview.Texture = (flag ? selectedStage.texture : null);
		}
		if (GodotObject.IsInstanceValid(_stagePreviewTitle))
		{
			_stagePreviewTitle.Text = (flag ? ("商品: " + EmptyToPlaceholder(selectedStage.saveKey)) : "商品: 未选择");
		}
		if (GodotObject.IsInstanceValid(_stagePreviewMeta))
		{
			_stagePreviewMeta.Text = (flag ? $"价格: {selectedStage.cost} {EmptyToPlaceholder(selectedStage.type)}" : "价格: -");
		}
		if (GodotObject.IsInstanceValid(_stagePreviewStock))
		{
			_stagePreviewStock.Text = (flag ? $"库存: {selectedStage.openMinNum}-{selectedStage.openMaxNum} +{selectedStage.addNum}" : "库存: -");
		}
		RefreshPurchaseSimulation();
	}

	private void DisposeStageVisualChoices()
	{
		_stageSaveTypeVisualChoices?.Dispose();
		_stageSaveTypeVisualChoices = null;
		_stageCostTypeVisualChoices?.Dispose();
		_stageCostTypeVisualChoices = null;
	}

	private void NormalizeSelection()
	{
		int valueOrDefault = (_editingShop?.pageList?.Count).GetValueOrDefault();
		_selectedPageIndex = NormalizeIndex(_selectedPageIndex, valueOrDefault);
		int valueOrDefault2 = (GetSelectedPage()?.itemList?.Count).GetValueOrDefault();
		_selectedItemIndex = NormalizeIndex(_selectedItemIndex, valueOrDefault2);
		int valueOrDefault3 = (GetSelectedItem()?.stageList?.Count).GetValueOrDefault();
		_selectedStageIndex = NormalizeIndex(_selectedStageIndex, valueOrDefault3);
	}

	private static int NormalizeIndex(int index, int count)
	{
		if (count <= 0)
		{
			return -1;
		}
		if (index < 0)
		{
			return 0;
		}
		return Math.Min(index, count - 1);
	}

	private ShopPageConfig GetSelectedPage()
	{
		if (_editingShop?.pageList == null || _selectedPageIndex < 0 || _selectedPageIndex >= _editingShop.pageList.Count)
		{
			return null;
		}
		return _editingShop.pageList[_selectedPageIndex];
	}

	private ShopItemConfig GetSelectedItem()
	{
		ShopPageConfig selectedPage = GetSelectedPage();
		if (!GodotObject.IsInstanceValid(selectedPage) || selectedPage.itemList == null || _selectedItemIndex < 0 || _selectedItemIndex >= selectedPage.itemList.Count)
		{
			return null;
		}
		return selectedPage.itemList[_selectedItemIndex];
	}

	private ShopItemStageConfig GetSelectedStage()
	{
		ShopItemConfig selectedItem = GetSelectedItem();
		if (!GodotObject.IsInstanceValid(selectedItem) || selectedItem.stageList == null || _selectedStageIndex < 0 || _selectedStageIndex >= selectedItem.stageList.Count)
		{
			return null;
		}
		return selectedItem.stageList[_selectedStageIndex];
	}

	private static Array<ShopPageConfig> CopyPages(Array<ShopPageConfig> source)
	{
		if (source != null)
		{
			return new Array<ShopPageConfig>(source);
		}
		return new Array<ShopPageConfig>();
	}

	private static Array<ShopItemConfig> CopyItems(Array<ShopItemConfig> source)
	{
		if (source != null)
		{
			return new Array<ShopItemConfig>(source);
		}
		return new Array<ShopItemConfig>();
	}

	private static Array<ShopItemStageConfig> CopyStages(Array<ShopItemStageConfig> source)
	{
		if (source != null)
		{
			return new Array<ShopItemStageConfig>(source);
		}
		return new Array<ShopItemStageConfig>();
	}

	private void OnShopPropertyEdited(bool committed)
	{
		if (_editingShop == null || !GodotObject.IsInstanceValid(base.CurrentResource))
		{
			return;
		}
		if (committed)
		{
			NotifyCurrentResourceEdited();
			if (base.CurrentResource != _editingShop)
			{
				_editingShop.EmitChanged();
			}
			if (GodotObject.IsInstanceValid(_runtimeShopPreview))
			{
				_runtimeShopPreview.EditPreviewConfig(_editingShop);
			}
			RefreshAll();
		}
		else
		{
			base.CurrentResource.EmitChanged();
			if (base.CurrentResource != _editingShop)
			{
				_editingShop.EmitChanged();
			}
			MarkCurrentResourceDirty();
			RefreshLiveEditPreview();
		}
		CaptureSelectionIdentity();
	}

	private void RefreshLiveEditPreview()
	{
		ShopItemConfig selectedItem = GetSelectedItem();
		if (GodotObject.IsInstanceValid(_itemList) && GodotObject.IsInstanceValid(selectedItem) && _selectedItemIndex >= 0 && _selectedItemIndex < _itemList.ItemCount)
		{
			_itemList.SetItemText(_selectedItemIndex, $"{_selectedItemIndex + 1}. {EmptyToPlaceholder(selectedItem.type)}  阶段 {selectedItem.stageList?.Count ?? 0}");
		}
		ShopItemStageConfig selectedStage = GetSelectedStage();
		if (GodotObject.IsInstanceValid(selectedStage))
		{
			if (GodotObject.IsInstanceValid(_stageList) && _selectedStageIndex >= 0 && _selectedStageIndex < _stageList.ItemCount)
			{
				_stageList.SetItemText(_selectedStageIndex, $"{_selectedStageIndex + 1}. {EmptyToPlaceholder(selectedStage.saveKey)}  {selectedStage.cost} {EmptyToPlaceholder(selectedStage.type)}");
			}
			if (GodotObject.IsInstanceValid(_stagePreviewTitle))
			{
				_stagePreviewTitle.Text = "商品: " + EmptyToPlaceholder(selectedStage.saveKey);
			}
			if (GodotObject.IsInstanceValid(_stagePreviewMeta))
			{
				_stagePreviewMeta.Text = $"价格: {selectedStage.cost} {EmptyToPlaceholder(selectedStage.type)}";
			}
			if (GodotObject.IsInstanceValid(_stagePreviewStock))
			{
				_stagePreviewStock.Text = $"库存: {selectedStage.openMinNum}-{selectedStage.openMaxNum} +{selectedStage.addNum}";
			}
			RefreshPurchaseSimulation();
		}
	}

	private void CaptureSelectionIdentity()
	{
		_selectedPageIdentity = GetSelectedPage();
		_selectedItemIdentity = GetSelectedItem();
		_selectedStageIdentity = GetSelectedStage();
	}

	private void RestoreSelectionIdentity()
	{
		int num = IndexOfIdentity(_editingShop?.pageList, _selectedPageIdentity);
		if (num >= 0)
		{
			_selectedPageIndex = num;
		}
		int num2 = IndexOfIdentity(GetSelectedPage()?.itemList, _selectedItemIdentity);
		if (num2 >= 0)
		{
			_selectedItemIndex = num2;
		}
		int num3 = IndexOfIdentity(GetSelectedItem()?.stageList, _selectedStageIdentity);
		if (num3 >= 0)
		{
			_selectedStageIndex = num3;
		}
		NormalizeSelection();
	}

	private static int IndexOfIdentity<[MustBeVariant] T>(Array<T> values, T target) where T : GodotObject
	{
		if (values == null || !GodotObject.IsInstanceValid(target))
		{
			return -1;
		}
		for (int i = 0; i < values.Count; i++)
		{
			if (values[i] == target)
			{
				return i;
			}
		}
		return -1;
	}

	public void RefreshShopEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager == null || (!xWUndoRedoManager.IsUndoing() && !xWUndoRedoManager.IsRedoing()) || base.CanvasGrid == null)
		{
			return;
		}
		_shopPropertyBinding?.Dispose();
		_shopPropertyBinding = null;
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		RenderCustomVisualPreset(null);
		RestoreSelectionIdentity();
		RefreshAll();
		CaptureSelectionIdentity();
	}

	private void AddSummaryRows(ShopConfig shop)
	{
		AddItemIfMissing(base.PreviewList, BuildSummary(shop));
		AddItemIfMissing(base.TimelineList, $"pageList -> {(shop?.pageList?.Count).GetValueOrDefault()}");
		AddItemIfMissing(base.GraphList, "商店 -> pageList -> itemList -> stageList -> saveKey/texture/npcTalk");
		AddItemIfMissing(base.ReferenceList, "商店资源 -> " + base.CurrentResourcePath);
	}

	private static string BuildSummary(ShopConfig shop)
	{
		int valueOrDefault = (shop?.pageList?.Count).GetValueOrDefault();
		int num = 0;
		int num2 = 0;
		if (shop?.pageList != null)
		{
			foreach (ShopPageConfig page in shop.pageList)
			{
				num += (page?.itemList?.Count).GetValueOrDefault();
				if (page?.itemList == null)
				{
					continue;
				}
				foreach (ShopItemConfig item in page.itemList)
				{
					num2 += (item?.stageList?.Count).GetValueOrDefault();
				}
			}
		}
		return $"商店: 页面 {valueOrDefault}, 商品 {num}, 阶段 {num2}";
	}

	private static void AddShopOption(List<ShopOptionItem> options, string name, string sourceLabel, string sourcePath, string description)
	{
		if (options == null || string.IsNullOrWhiteSpace(name))
		{
			return;
		}
		string text = name.StripEdges();
		foreach (ShopOptionItem option in options)
		{
			if (string.Equals(option.Name, text, StringComparison.OrdinalIgnoreCase))
			{
				MergeText(ref option.SourceLabel, sourceLabel);
				if (!string.IsNullOrWhiteSpace(sourcePath))
				{
					option.SourcePath = sourcePath;
				}
				MergeText(ref option.Description, description);
				return;
			}
		}
		options.Add(new ShopOptionItem
		{
			Name = text,
			SourceLabel = (sourceLabel ?? ""),
			SourcePath = (sourcePath ?? ""),
			Description = (description ?? "")
		});
	}

	private static List<ShopOptionItem> SortShopOptions(List<ShopOptionItem> options)
	{
		options.Sort((ShopOptionItem a, ShopOptionItem b) => string.Compare(a?.Name, b?.Name, StringComparison.OrdinalIgnoreCase));
		return options;
	}

	private static void MergeText(ref string target, string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			if (string.IsNullOrWhiteSpace(target))
			{
				target = value;
			}
			else if (!target.Contains(value, StringComparison.OrdinalIgnoreCase))
			{
				target = target + "+" + value;
			}
		}
	}

	private static string ReadSaveKeyFromResourceFile(string filePath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
		{
			return "";
		}
		try
		{
			foreach (string item in File.ReadLines(filePath))
			{
				string text = item.Trim();
				if (text.StartsWith("saveKey", StringComparison.Ordinal))
				{
					int num = text.IndexOf('=');
					if (num >= 0)
					{
						string text2 = text;
						int num2 = num + 1;
						return CleanResourceTextValue(text2.Substring(num2, text2.Length - num2));
					}
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Shop editor key scan failed: " + filePath + " " + ex.Message);
		}
		return "";
	}

	private static string CleanResourceTextValue(string value)
	{
		string text = (value ?? "").Trim();
		if (text.StartsWith("&", StringComparison.Ordinal))
		{
			string text2 = text;
			text = text2.Substring(1, text2.Length - 1).TrimStart();
		}
		if (text.Length >= 2 && text[0] == '"')
		{
			string text3 = text;
			if (text3[text3.Length - 1] == '"')
			{
				string text2 = text;
				text = text2.Substring(1, text2.Length - 1 - 1);
			}
		}
		return text.Replace("\\\"", "\"").StripEdges();
	}

	private static void RegisterProjectSelectionPath(string sourcePath)
	{
		string currentProjectPathForShopEditor = GetCurrentProjectPathForShopEditor();
		if (!string.IsNullOrWhiteSpace(currentProjectPathForShopEditor) && !string.IsNullOrWhiteSpace(sourcePath))
		{
			XWModManifestSyncService.RegisterPath(currentProjectPathForShopEditor, sourcePath);
		}
	}

	private static string GetCurrentProjectPathForShopEditor()
	{
		string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		if (XWEditorInterface.Instance?.GetEditorPanel() is ModEditorPanel modEditorPanel)
		{
			return modEditorPanel.GetCurrentProject()?.ProjectPath ?? "";
		}
		return "";
	}

	private static string ToProjectRelativeDisplayPath(string filePath, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(projectPath))
		{
			return filePath ?? "";
		}
		try
		{
			return Path.GetRelativePath(projectPath, filePath).Replace('\\', '/');
		}
		catch
		{
			return filePath;
		}
	}

	private static void SelectOptionByText(OptionButton option, string text)
	{
		if (!GodotObject.IsInstanceValid(option))
		{
			return;
		}
		for (int i = 0; i < option.ItemCount; i++)
		{
			if (!(option.GetItemText(i) != text))
			{
				option.Select(i);
				return;
			}
		}
		if (option.ItemCount > 0)
		{
			option.Select(0);
		}
	}

	private static void SetSpinValue(SpinBox spin, double value)
	{
		if (GodotObject.IsInstanceValid(spin))
		{
			spin.SetValueNoSignal(value);
		}
	}

	private static string DisplayIndex(int index)
	{
		if (index >= 0)
		{
			return (index + 1).ToString();
		}
		return "未选择";
	}

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}

	private static string FormatResource(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "未配置";
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath.GetFile();
		}
		return resource.ResourceName;
	}

	private static void AddItemIfMissing(ItemList list, string text)
	{
		if (!GodotObject.IsInstanceValid(list) || string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		for (int i = 0; i < list.ItemCount; i++)
		{
			if (list.GetItemText(i) == text)
			{
				return;
			}
		}
		list.AddItem(text);
	}
}
