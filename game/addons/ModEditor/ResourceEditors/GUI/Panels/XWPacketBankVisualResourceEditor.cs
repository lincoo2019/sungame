using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Inspector;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWPacketBankVisualResourceEditor : XWGenericVisualResourceEditor
{
	private sealed class PacketSelectionOption
	{
		public string Key = "";

		public string SourceLabel = "";

		public string SourcePath = "";

		public string Description = "";

		public string DetailText => $"{Key}\n来源: {SourceLabel}\n路径: {SourcePath}\n说明: {Description}";

		public bool Matches(string query)
		{
			if (string.IsNullOrWhiteSpace(query))
			{
				return true;
			}
			if (!(Key ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) && !(SourceLabel ?? "").Contains(query, StringComparison.OrdinalIgnoreCase) && !(SourcePath ?? "").Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				return (Description ?? "").Contains(query, StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}
	}

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankVisualEditorLayout.tscn";

	private const string CompactPacketSelectorRowScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn";

	private const string PacketSelectorScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankPacketSelectorWindow.tscn";

	private const string PoolPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankPoolPreview.tscn";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _compactPacketSelectorRowScene;

	private static PackedScene _packetSelectorScene;

	private static PackedScene _poolPreviewScene;

	private static readonly string[] DefaultCategories = new string[9] { "White", "Gold", "Diamond", "Colour", "Star", "Original", "Item", "GraveStone", "Zombie" };

	private TowerDefensePacketBankData _editingPacketBank;

	private LevelEditorPacketBank _runtimePacketBankPreview;

	private Label _summaryLabel;

	private Label _categorySummaryLabel;

	private Label _packetSummaryLabel;

	private OptionButton _poolPreviewOption;

	private XWVisualSegmentedOption _poolPreviewVisualChoices;

	private Label _poolPreviewResultLabel;

	private ItemList _categoryList;

	private ItemList _packetList;

	private GridContainer _packetGrid;

	private LineEdit _categoryNameEdit;

	private LineEdit _packetKeyEdit;

	private LineEdit _resourceName;

	private CheckButton _localToScene;

	private XWResourcePicker _packetPicker;

	private XWVisualPropertyBinding _propertyBinding;

	private string _selectedCategory = "";

	private int _selectedCategoryIndex = -1;

	private int _selectedPacketIndex = -1;

	private bool _updatingControls;

	private Window _packetSelectorWindow;

	private LineEdit _packetSelectorFilter;

	private GridContainer _packetSelectorGrid;

	private Label _packetSelectorDescription;

	private readonly List<PacketSelectionOption> _packetSelectionOptions = new List<PacketSelectionOption>();

	private readonly List<PacketSelectionOption> _filteredPacketSelectionOptions = new List<PacketSelectionOption>();

	private readonly RandomNumberGenerator _poolPreviewRandom = new RandomNumberGenerator();

	private PacketSelectionOption _selectedPacketSelectionOption;

	private const string CharacterResourceRegistryPath = "res://Asset/Config/Character/CharacterResource.json";

	private const string ModCardResourceFolder = "Resources/Cards";

	public override void _ExitTree()
	{
		DisposePacketBankPropertyBinding();
		base._ExitTree();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposePacketBankPropertyBinding();
		if (base.CurrentResource is TowerDefensePacketBankData towerDefensePacketBankData)
		{
			_editingPacketBank = towerDefensePacketBankData;
			RenderPacketBankEditor(towerDefensePacketBankData);
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompletePacketBankVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompletePacketBankVisualCoverage(Resource resource)
	{
		return resource?.GetType() == typeof(TowerDefensePacketBankData);
	}

	private void DisposePacketBankPropertyBinding()
	{
		_poolPreviewVisualChoices?.Dispose();
		_poolPreviewVisualChoices = null;
		_propertyBinding?.Dispose();
		_propertyBinding = null;
		_editingPacketBank = null;
		_runtimePacketBankPreview = null;
		_packetPicker = null;
		_selectedPacketSelectionOption = null;
	}

	private void RenderPacketBankEditor(TowerDefensePacketBankData packetBank)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindPacketBankLayout(vBoxContainer, packetBank);
				RefreshAll();
				AddSummaryRows(packetBank);
			}
		}
	}

	private void BindPacketBankLayout(VBoxContainer root, TowerDefensePacketBankData packetBank)
	{
		_resourceName = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToScene = root.GetNode<CheckButton>("%LocalToSceneCheck");
		BindDirectEditControls(packetBank);
		_runtimePacketBankPreview = root.GetNode<LevelEditorPacketBank>("%RuntimePacketBank");
		_runtimePacketBankPreview.Init(packetBank);
		_summaryLabel = root.GetNode<Label>("%SummaryLabel");
		_summaryLabel.Text = BuildSummary(packetBank);
		string[] defaultCategories = DefaultCategories;
		foreach (string categoryName in defaultCategories)
		{
			root.GetNode<Button>("%" + categoryName + "Button").Pressed += delegate
			{
				AddCategoryEntry(categoryName);
			};
		}
		if (_poolPreviewScene == null)
		{
			_poolPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankPoolPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (GodotObject.IsInstanceValid(_poolPreviewScene))
		{
			Control control = _poolPreviewScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
			_poolPreviewOption = control.GetNode<OptionButton>("PoolOption");
			_poolPreviewVisualChoices = new XWVisualSegmentedOption(_poolPreviewOption, control.GetNode<HFlowContainer>("%PoolVisualChoices"));
			_poolPreviewVisualChoices.Rebuild();
			_poolPreviewResultLabel = control.GetNode<Label>("Result");
			control.GetNode<Button>("DrawButton").Pressed += DrawRandomPacketFromPool;
			root.GetNode<VBoxContainer>("%PoolPreviewHost").AddChild(control, forceReadableName: false, InternalMode.Disabled);
		}
		_poolPreviewRandom.Randomize();
		_categorySummaryLabel = root.GetNode<Label>("%CategorySummaryLabel");
		_categoryNameEdit = root.GetNode<LineEdit>("%CategoryNameLineEdit");
		root.GetNode<Button>("%AddCategoryButton").Pressed += AddCategoryEntry;
		root.GetNode<Button>("%RemoveCategoryButton").Pressed += RemoveSelectedCategoryEntry;
		_categoryList = root.GetNode<ItemList>("%CategoryList");
		_categoryList.ItemSelected += delegate(long index)
		{
			SelectCategory((int)index);
		};
		_packetSummaryLabel = root.GetNode<Label>("%PacketSummaryLabel");
		_packetKeyEdit = root.GetNode<LineEdit>("%PacketKeyLineEdit");
		_packetPicker = XWResourcePicker.Create();
		_packetPicker.Name = "PacketConfigPicker";
		_packetPicker.Setup("TowerDefensePacketConfig");
		_packetPicker.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		_packetPicker.ResourceChanged += OnPacketResourcePicked;
		root.GetNode<HBoxContainer>("%PacketPickerHost").AddChild(_packetPicker, forceReadableName: false, InternalMode.Disabled);
		root.GetNode<Button>("%ChoosePacketButton").Pressed += ShowPacketSelector;
		root.GetNode<Button>("%AddPacketButton").Pressed += AddPacketToSelectedCategory;
		root.GetNode<Button>("%RemovePacketButton").Pressed += RemoveSelectedPacketEntry;
		root.GetNode<Button>("%PacketUpButton").Pressed += delegate
		{
			MoveSelectedPacketEntry(-1);
		};
		root.GetNode<Button>("%PacketDownButton").Pressed += delegate
		{
			MoveSelectedPacketEntry(1);
		};
		_packetList = root.GetNode<ItemList>("%PacketList");
		_packetList.ItemSelected += delegate(long index)
		{
			SelectPacket((int)index);
		};
		_packetGrid = root.GetNode<GridContainer>("%PacketGrid");
	}

	private void BindDirectEditControls(TowerDefensePacketBankData packetBank)
	{
		_propertyBinding?.Dispose();
		_propertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnPacketBankPropertyEdited);
		_propertyBinding.BindText(_resourceName, packetBank, "resource_name", RefreshResourceFieldsPreview, this, "RefreshPacketBankFromHistory");
		_propertyBinding.BindToggle(_localToScene, packetBank, "resource_local_to_scene", RefreshResourceFieldsPreview, this, "RefreshPacketBankFromHistory");
	}

	private void OnPacketBankPropertyEdited(bool committed)
	{
		if (base.CurrentResource is TowerDefensePacketBankData towerDefensePacketBankData && towerDefensePacketBankData == _editingPacketBank)
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				towerDefensePacketBankData.EmitChanged();
			}
			if (GodotObject.IsInstanceValid(_runtimePacketBankPreview))
			{
				_runtimePacketBankPreview.Init(_editingPacketBank);
			}
			RefreshAll();
		}
	}

	private void RefreshResourceFieldsPreview()
	{
		if (GodotObject.IsInstanceValid(_summaryLabel) && GodotObject.IsInstanceValid(_editingPacketBank))
		{
			_summaryLabel.Text = BuildSummary(_editingPacketBank);
		}
	}

	public void RefreshPacketBankFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()) && base.CurrentResource is TowerDefensePacketBankData towerDefensePacketBankData && towerDefensePacketBankData == _editingPacketBank)
		{
			BindDirectEditControls(towerDefensePacketBankData);
			if (GodotObject.IsInstanceValid(_runtimePacketBankPreview))
			{
				_runtimePacketBankPreview.Init(towerDefensePacketBankData);
			}
			RefreshAll();
		}
	}

	private void DrawRandomPacketFromPool()
	{
		if (_editingPacketBank?.category == null)
		{
			return;
		}
		int num = (GodotObject.IsInstanceValid(_poolPreviewOption) ? _poolPreviewOption.Selected : 0);
		List<(string, int, string)> list = new List<(string, int, string)>();
		foreach (Variant key in _editingPacketBank.category.Keys)
		{
			string text = key.AsString();
			if ((num == 1 && !IsPlantPacketCategory(text)) || (num == 2 && text != "Zombie") || (num == 3 && text != _selectedCategory))
			{
				continue;
			}
			Godot.Collections.Array categoryArray = GetCategoryArray(_editingPacketBank.category, text, create: false);
			for (int i = 0; i < categoryArray.Count; i++)
			{
				string text2 = FormatVariant(categoryArray[i]);
				bool flag = num == 1;
				if (flag)
				{
					bool flag2 = ((text2 == "PlantPresentBox" || text2 == "PlantLuckyBlover") ? true : false);
					flag = flag2;
				}
				if (!flag && !string.IsNullOrWhiteSpace(text2))
				{
					list.Add((text, i, text2));
				}
			}
		}
		if (list.Count == 0)
		{
			if (GodotObject.IsInstanceValid(_poolPreviewResultLabel))
			{
				_poolPreviewResultLabel.Text = "当前卡池没有可抽取卡片";
			}
			return;
		}
		(string, int, string) tuple = list[_poolPreviewRandom.RandiRange(0, list.Count - 1)];
		_selectedCategory = tuple.Item1;
		_selectedCategoryIndex = FindCategoryIndex(tuple.Item1);
		_selectedPacketIndex = tuple.Item2;
		RefreshAll();
		if (GodotObject.IsInstanceValid(_poolPreviewResultLabel))
		{
			_poolPreviewResultLabel.Text = $"抽中: {tuple.Item3} · {tuple.Item1} #{tuple.Item2 + 1}";
		}
	}

	private int FindCategoryIndex(string categoryName)
	{
		if (_editingPacketBank?.category == null)
		{
			return -1;
		}
		int num = 0;
		foreach (Variant key in _editingPacketBank.category.Keys)
		{
			if (key.AsString() == categoryName)
			{
				return num;
			}
			num++;
		}
		return -1;
	}

	private static bool IsPlantPacketCategory(string category)
	{
		switch (category)
		{
		case "White":
		case "Gold":
		case "Diamond":
		case "Colour":
		case "Star":
		case "Original":
			return true;
		default:
			return false;
		}
	}

	private void AddCategoryEntry()
	{
		AddCategoryEntry(_categoryNameEdit?.Text ?? "");
	}

	private void AddCategoryEntry(string categoryName)
	{
		if (_editingPacketBank == null)
		{
			return;
		}
		string text = categoryName.StripEdges();
		if (!string.IsNullOrWhiteSpace(text))
		{
			Dictionary dictionary = DuplicateCategoryDictionary();
			if (!dictionary.ContainsKey(text))
			{
				dictionary[text] = new Godot.Collections.Array();
			}
			_selectedCategory = text;
			SetCategoryDictionary(dictionary, "添加卡牌库分类 " + text);
		}
	}

	private void RemoveSelectedCategoryEntry()
	{
		if (_editingPacketBank != null && !string.IsNullOrWhiteSpace(_selectedCategory))
		{
			string selectedCategory = _selectedCategory;
			Dictionary dictionary = DuplicateCategoryDictionary();
			dictionary.Remove(selectedCategory);
			_selectedCategory = "";
			_selectedCategoryIndex = -1;
			_selectedPacketIndex = -1;
			SetCategoryDictionary(dictionary, "删除卡牌库分类 " + selectedCategory);
		}
	}

	private void AddPacketToSelectedCategory()
	{
		if (_editingPacketBank == null)
		{
			return;
		}
		EnsureCategorySelected();
		if (!string.IsNullOrWhiteSpace(_selectedCategory))
		{
			string text = _packetKeyEdit?.Text.StripEdges() ?? "";
			if (string.IsNullOrWhiteSpace(text) && GodotObject.IsInstanceValid(_packetPicker?.GetEditedResource()))
			{
				text = ReadPacketKey(_packetPicker.GetEditedResource());
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				Dictionary dictionary = DuplicateCategoryDictionary();
				Godot.Collections.Array categoryArray = GetCategoryArray(dictionary, _selectedCategory, create: true);
				categoryArray.Add(text);
				dictionary[_selectedCategory] = categoryArray;
				_selectedPacketIndex = categoryArray.Count - 1;
				SetCategoryDictionary(dictionary, "向 " + _selectedCategory + " 添加卡牌 " + text);
			}
		}
	}

	private void RemoveSelectedPacketEntry()
	{
		if (_editingPacketBank != null && !string.IsNullOrWhiteSpace(_selectedCategory) && _selectedPacketIndex >= 0)
		{
			Dictionary dictionary = DuplicateCategoryDictionary();
			Godot.Collections.Array categoryArray = GetCategoryArray(dictionary, _selectedCategory, create: false);
			if (_selectedPacketIndex < categoryArray.Count)
			{
				categoryArray.RemoveAt(_selectedPacketIndex);
				dictionary[_selectedCategory] = categoryArray;
				_selectedPacketIndex = Math.Min(_selectedPacketIndex, categoryArray.Count - 1);
				SetCategoryDictionary(dictionary, "从 " + _selectedCategory + " 移除卡牌");
			}
		}
	}

	private void MoveSelectedPacketEntry(int direction)
	{
		if (_editingPacketBank != null && !string.IsNullOrWhiteSpace(_selectedCategory) && _selectedPacketIndex >= 0 && direction != 0)
		{
			Dictionary dictionary = DuplicateCategoryDictionary();
			Godot.Collections.Array categoryArray = GetCategoryArray(dictionary, _selectedCategory, create: false);
			int num = Math.Clamp(_selectedPacketIndex + direction, 0, categoryArray.Count - 1);
			if (num != _selectedPacketIndex)
			{
				Variant item = categoryArray[_selectedPacketIndex];
				categoryArray.RemoveAt(_selectedPacketIndex);
				categoryArray.Insert(num, item);
				dictionary[_selectedCategory] = categoryArray;
				_selectedPacketIndex = num;
				SetCategoryDictionary(dictionary, "调整 " + _selectedCategory + " 卡牌顺序");
			}
		}
	}

	private void SetCategoryDictionary(Dictionary category, string actionName)
	{
		if (_editingPacketBank != null && _propertyBinding != null)
		{
			_propertyBinding.SetValue(_editingPacketBank, "category", category ?? new Dictionary(), actionName, this, "RefreshPacketBankFromHistory");
		}
	}

	private void SelectCategory(int index)
	{
		_selectedCategoryIndex = index;
		_selectedCategory = GetCategoryAt(index);
		_selectedPacketIndex = -1;
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_categoryNameEdit))
			{
				_categoryNameEdit.Text = _selectedCategory;
			}
		}
		finally
		{
			_updatingControls = false;
		}
		RefreshPacketList();
		RefreshPacketGrid();
		if (GodotObject.IsInstanceValid(_runtimePacketBankPreview) && !string.IsNullOrWhiteSpace(_selectedCategory))
		{
			_runtimePacketBankPreview.CategoryChoose(_selectedCategory, reFresh: true);
		}
	}

	private void SelectPacket(int index)
	{
		_selectedPacketIndex = index;
		Godot.Collections.Array selectedPacketArray = GetSelectedPacketArray();
		if (index < 0 || index >= selectedPacketArray.Count)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_packetKeyEdit))
			{
				_packetKeyEdit.Text = FormatVariant(selectedPacketArray[index]);
			}
		}
		finally
		{
			_updatingControls = false;
		}
		RefreshPacketGrid();
	}

	private void OnPacketResourcePicked(Resource resource)
	{
		if (!_updatingControls && GodotObject.IsInstanceValid(resource))
		{
			string text = ReadPacketKey(resource);
			if (GodotObject.IsInstanceValid(_packetKeyEdit))
			{
				_packetKeyEdit.Text = text;
			}
		}
	}

	private void ShowPacketSelector()
	{
		EnsurePacketSelectorWindow();
		if (GodotObject.IsInstanceValid(_packetSelectorWindow))
		{
			_packetSelectionOptions.Clear();
			_packetSelectionOptions.AddRange(CollectPacketSelectionOptions());
			if (GodotObject.IsInstanceValid(_packetSelectorFilter))
			{
				_packetSelectorFilter.Text = "";
			}
			FilterPacketSelector();
			_packetSelectorWindow.PopupCenteredClamped(new Vector2I(760, 520), 0.9f);
		}
	}

	private void EnsurePacketSelectorWindow()
	{
		if (GodotObject.IsInstanceValid(_packetSelectorWindow))
		{
			return;
		}
		if (_packetSelectorScene == null)
		{
			_packetSelectorScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWPacketBankPacketSelectorWindow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_packetSelectorWindow = _packetSelectorScene?.Instantiate<Window>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(_packetSelectorWindow))
		{
			_packetSelectorWindow.CloseRequested += delegate
			{
				_packetSelectorWindow.Hide();
			};
			AddChild(_packetSelectorWindow, forceReadableName: false, InternalMode.Disabled);
			_packetSelectorFilter = _packetSelectorWindow.GetNode<LineEdit>("Layout/Filter");
			_packetSelectorFilter.TextChanged += delegate
			{
				FilterPacketSelector();
			};
			_packetSelectorGrid = _packetSelectorWindow.GetNode<GridContainer>("Layout/PacketScroll/PacketGrid");
			_packetSelectorDescription = _packetSelectorWindow.GetNode<Label>("Layout/Description");
			_packetSelectorWindow.GetNode<Button>("Layout/Controls/AddButton").Pressed += ConfirmPacketSelectorSelection;
			_packetSelectorWindow.GetNode<Button>("Layout/Controls/CancelButton").Pressed += delegate
			{
				_packetSelectorWindow.Hide();
			};
		}
	}

	private void FilterPacketSelector()
	{
		if (!GodotObject.IsInstanceValid(_packetSelectorGrid))
		{
			return;
		}
		foreach (Node child in _packetSelectorGrid.GetChildren())
		{
			child.QueueFree();
		}
		_filteredPacketSelectionOptions.Clear();
		_selectedPacketSelectionOption = null;
		string query = _packetSelectorFilter?.Text?.StripEdges() ?? "";
		foreach (PacketSelectionOption packetSelectionOption in _packetSelectionOptions)
		{
			if (packetSelectionOption.Matches(query))
			{
				_filteredPacketSelectionOptions.Add(packetSelectionOption);
				_packetSelectorGrid.AddChild(CreatePacketSelectorItem(packetSelectionOption), forceReadableName: false, InternalMode.Disabled);
			}
		}
		if (_filteredPacketSelectionOptions.Count == 0)
		{
			_packetSelectorGrid.AddChild(new Label
			{
				Text = "没有可用卡片",
				CustomMinimumSize = new Vector2(240f, 64f)
			}, forceReadableName: false, InternalMode.Disabled);
			UpdatePacketSelectorDescription(null);
		}
		else
		{
			SelectPacketSelectorOption(_filteredPacketSelectionOptions[0]);
		}
	}

	private void UpdatePacketSelectorDescription(PacketSelectionOption option)
	{
		if (GodotObject.IsInstanceValid(_packetSelectorDescription))
		{
			_packetSelectorDescription.Text = ((option != null) ? option.DetailText : "没有选择卡片");
		}
	}

	private void ConfirmPacketSelectorSelection()
	{
		ConfirmPacketSelectorSelection(_selectedPacketSelectionOption ?? ((_filteredPacketSelectionOptions.Count > 0) ? _filteredPacketSelectionOptions[0] : null));
	}

	private void ConfirmPacketSelectorSelection(PacketSelectionOption option)
	{
		if (option != null)
		{
			AddSelectedPacketOption(option);
			if (GodotObject.IsInstanceValid(_packetSelectorWindow))
			{
				_packetSelectorWindow.Hide();
			}
		}
	}

	private Control CreatePacketSelectorItem(PacketSelectionOption option)
	{
		if (_compactPacketSelectorRowScene == null)
		{
			_compactPacketSelectorRowScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Control control = _compactPacketSelectorRowScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(control))
		{
			throw new InvalidOperationException("Cannot instantiate selector row: res://addons/ModEditor/ResourceEditors/GUI/Panels/XWCompactPacketSelectorRow.tscn");
		}
		control.Name = "PacketSelectorItem";
		Button node = control.GetNode<Button>("%SelectSurface");
		node.ToggleMode = false;
		node.Pressed += delegate
		{
			ConfirmPacketSelectorSelection(option);
		};
		node.MouseEntered += delegate
		{
			SelectPacketSelectorOption(option);
		};
		control.GetNode<Label>("%PacketName").Text = option.Key;
		control.GetNode<Label>("%PacketCost").Text = option.SourceLabel;
		Label node2 = control.GetNode<Label>("%LoadingBadge");
		node2.Text = "卡";
		node2.Visible = true;
		Control node3 = control.GetNode<Control>("%PreviewHost");
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacketConfigForSelector(option);
		if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
			try
			{
				towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.Name = "PacketSelectorPacketShow";
					towerDefenseInGamePacketShow.Position = new Vector2(16f, 18f);
					towerDefenseInGamePacketShow.Scale = new Vector2(0.23f, 0.23f);
					towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
					node3.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
					ConfigurePacketSelectorPacketShow(towerDefenseInGamePacketShow, towerDefensePacketConfig);
					node2.Visible = false;
				}
			}
			catch (Exception ex)
			{
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.QueueFree();
				}
				GD.PushWarning("PacketBank selector PacketShow failed: " + option.Key + " " + ex.Message);
			}
		}
		return control;
	}

	private static Label CreatePacketSelectorFallbackLabel(PacketSelectionOption option)
	{
		Label label = new Label();
		label.Text = option?.Key ?? "Packet";
		label.HorizontalAlignment = HorizontalAlignment.Center;
		label.VerticalAlignment = VerticalAlignment.Center;
		label.AutowrapMode = TextServer.AutowrapMode.WordSmart;
		label.MouseFilter = MouseFilterEnum.Ignore;
		label.SetAnchorsPreset(LayoutPreset.FullRect);
		return label;
	}

	private void SelectPacketSelectorOption(PacketSelectionOption option)
	{
		_selectedPacketSelectionOption = option;
		UpdatePacketSelectorDescription(option);
	}

	private static TowerDefensePacketConfig LoadPacketConfigForSelector(PacketSelectionOption option)
	{
		if (option == null || string.IsNullOrWhiteSpace(option.Key))
		{
			return null;
		}
		if (!string.IsNullOrWhiteSpace(option.SourcePath))
		{
			string text = Path.GetExtension(option.SourcePath).ToLowerInvariant();
			if ((text == ".tres" || text == ".res") ? true : false)
			{
				try
				{
					TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(option.SourcePath, null, ResourceLoader.CacheMode.Reuse);
					if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
					{
						return towerDefensePacketConfig;
					}
				}
				catch (Exception ex)
				{
					GD.PushWarning("PacketBank selector resource load failed: " + option.SourcePath + " " + ex.Message);
				}
			}
		}
		try
		{
			TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly(option.Key);
			if (GodotObject.IsInstanceValid(packetConfigReadOnly))
			{
				return packetConfigReadOnly;
			}
		}
		catch
		{
		}
		try
		{
			TowerDefensePacketConfig packetConfigReadOnlyByCharacterName = TowerDefenseManager.GetPacketConfigReadOnlyByCharacterName(option.Key);
			if (GodotObject.IsInstanceValid(packetConfigReadOnlyByCharacterName))
			{
				return packetConfigReadOnlyByCharacterName;
			}
		}
		catch
		{
		}
		return null;
	}

	private static void ConfigurePacketSelectorPacketShow(TowerDefenseInGamePacketShow packetShow, TowerDefensePacketConfig packet)
	{
		if (GodotObject.IsInstanceValid(packetShow) && GodotObject.IsInstanceValid(packet))
		{
			packetShow.onlyDraw = true;
			packetShow.setPcLayout = true;
			packetShow.showLove = false;
			packetShow.select = false;
			packetShow.alive = true;
			packetShow.@lock = false;
			packetShow.MouseFilter = MouseFilterEnum.Ignore;
			packetShow.Init(packet);
			packetShow.onlyDraw = true;
			packetShow.RefreshPreview();
		}
	}

	private void AddSelectedPacketOption(PacketSelectionOption option)
	{
		if (_editingPacketBank != null && option != null && !string.IsNullOrWhiteSpace(option.Key))
		{
			EnsureCategorySelected();
			if (string.IsNullOrWhiteSpace(_selectedCategory))
			{
				_selectedCategory = ((DefaultCategories.Length != 0) ? DefaultCategories[0] : "Default");
			}
			if (GodotObject.IsInstanceValid(_packetKeyEdit))
			{
				_packetKeyEdit.Text = option.Key;
			}
			Dictionary dictionary = DuplicateCategoryDictionary();
			Godot.Collections.Array categoryArray = GetCategoryArray(dictionary, _selectedCategory, create: true);
			categoryArray.Add(option.Key);
			dictionary[_selectedCategory] = categoryArray;
			_selectedPacketIndex = categoryArray.Count - 1;
			RegisterPacketSelectionPath(option.SourcePath);
			SetCategoryDictionary(dictionary, "向 " + _selectedCategory + " 添加卡牌 " + option.Key);
			XWEditorInterface.Instance?.AddOutputMessage("卡牌库: 添加卡片 " + option.Key);
		}
	}

	private List<PacketSelectionOption> CollectPacketSelectionOptions()
	{
		List<PacketSelectionOption> list = new List<PacketSelectionOption>();
		LoadRegisteredPacketKeys(list);
		CollectModCardOptions(list);
		list.Sort((PacketSelectionOption a, PacketSelectionOption b) => string.Compare(a?.Key, b?.Key, StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static void LoadRegisteredPacketKeys(List<PacketSelectionOption> options)
	{
		if (options == null || !ResourceLoader.Exists("res://Asset/Config/Character/CharacterResource.json"))
		{
			return;
		}
		try
		{
			Json json = ResourceLoader.Load<Json>("res://Asset/Config/Character/CharacterResource.json", null, ResourceLoader.CacheMode.Reuse);
			if (!GodotObject.IsInstanceValid(json) || json.Data.VariantType != Variant.Type.Dictionary)
			{
				return;
			}
			Dictionary dictionary = json.Data.AsGodotDictionary();
			foreach (Variant key in dictionary.Keys)
			{
				AddPacketSelectionOption(options, key.AsString(), "游戏内卡片", "res://Asset/Config/Character/CharacterResource.json", "内置 Character 注册表");
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
					AddPacketSelectionOption(options, key2.AsString(), "游戏内卡片", "res://Asset/Config/Character/CharacterResource.json", "Packet of " + key.AsString());
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("PacketBank selector registry load failed: res://Asset/Config/Character/CharacterResource.json " + ex.Message);
		}
	}

	private static void CollectModCardOptions(List<PacketSelectionOption> options)
	{
		string currentProjectPathForPacketBankEditor = GetCurrentProjectPathForPacketBankEditor();
		if (options == null || string.IsNullOrWhiteSpace(currentProjectPathForPacketBankEditor))
		{
			return;
		}
		string path = Path.Combine(currentProjectPathForPacketBankEditor, "Resources/Cards".Replace('/', Path.DirectorySeparatorChar));
		if (!Directory.Exists(path))
		{
			return;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
		{
			string text = Path.GetExtension(item).ToLowerInvariant();
			if (text == ".tres" || text == ".res")
			{
				string text2 = ReadPacketKeyFromResourceFile(item);
				if (string.IsNullOrWhiteSpace(text2))
				{
					text2 = Path.GetFileNameWithoutExtension(item);
				}
				AddPacketSelectionOption(options, text2, "Mod 卡片", item, ToProjectRelativeDisplayPath(item, currentProjectPathForPacketBankEditor));
			}
		}
	}

	private static string ReadPacketKeyFromResourceFile(string filePath)
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
			GD.PushWarning("PacketBank selector card scan failed: " + filePath + " " + ex.Message);
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

	private static void AddPacketSelectionOption(List<PacketSelectionOption> options, string key, string sourceLabel, string sourcePath, string description)
	{
		if (options == null || string.IsNullOrWhiteSpace(key))
		{
			return;
		}
		string text = key.StripEdges();
		foreach (PacketSelectionOption option in options)
		{
			if (string.Equals(option.Key, text, StringComparison.OrdinalIgnoreCase))
			{
				MergePacketSelectionText(ref option.SourceLabel, sourceLabel);
				if (!string.IsNullOrWhiteSpace(sourcePath))
				{
					option.SourcePath = sourcePath;
				}
				MergePacketSelectionText(ref option.Description, description);
				return;
			}
		}
		options.Add(new PacketSelectionOption
		{
			Key = text,
			SourceLabel = (sourceLabel ?? ""),
			SourcePath = (sourcePath ?? ""),
			Description = (description ?? "")
		});
	}

	private static void MergePacketSelectionText(ref string target, string value)
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

	private static void RegisterPacketSelectionPath(string sourcePath)
	{
		string currentProjectPathForPacketBankEditor = GetCurrentProjectPathForPacketBankEditor();
		if (!string.IsNullOrWhiteSpace(currentProjectPathForPacketBankEditor) && !string.IsNullOrWhiteSpace(sourcePath))
		{
			XWModManifestSyncService.RegisterPath(currentProjectPathForPacketBankEditor, sourcePath);
		}
	}

	private static string GetCurrentProjectPathForPacketBankEditor()
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

	private void RefreshAll()
	{
		if (_editingPacketBank == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_summaryLabel))
			{
				_summaryLabel.Text = BuildSummary(_editingPacketBank);
			}
			RefreshCategories();
			RefreshPacketList();
			RefreshPacketGrid();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshCategories()
	{
		if (!GodotObject.IsInstanceValid(_categoryList) || _editingPacketBank == null)
		{
			return;
		}
		_categoryList.Clear();
		Dictionary dictionary = _editingPacketBank.category ?? new Dictionary();
		int num = 0;
		int num2 = -1;
		foreach (Variant key in dictionary.Keys)
		{
			string text = key.AsString();
			Godot.Collections.Array categoryArray = GetCategoryArray(dictionary, text, create: false);
			_categoryList.AddItem($"{text} ({categoryArray.Count})");
			if (text == _selectedCategory)
			{
				num2 = num;
			}
			num++;
		}
		if (dictionary.Count == 0)
		{
			_categoryList.AddItem("未配置分类");
			_selectedCategory = "";
			_selectedCategoryIndex = -1;
		}
		else
		{
			if (num2 < 0)
			{
				SelectCategory(0);
				return;
			}
			_categoryList.Select(num2);
			_selectedCategoryIndex = num2;
		}
		if (GodotObject.IsInstanceValid(_categorySummaryLabel))
		{
			_categorySummaryLabel.Text = $"分类数量: {dictionary.Count}";
		}
	}

	private void RefreshPacketList()
	{
		if (!GodotObject.IsInstanceValid(_packetList))
		{
			return;
		}
		_packetList.Clear();
		Godot.Collections.Array selectedPacketArray = GetSelectedPacketArray();
		if (selectedPacketArray.Count == 0)
		{
			_packetList.AddItem(string.IsNullOrWhiteSpace(_selectedCategory) ? "未选择分类" : "当前分类没有卡片");
		}
		else
		{
			for (int i = 0; i < selectedPacketArray.Count; i++)
			{
				_packetList.AddItem($"{i + 1}. {FormatVariant(selectedPacketArray[i])}");
			}
			if (_selectedPacketIndex >= 0 && _selectedPacketIndex < selectedPacketArray.Count)
			{
				_packetList.Select(_selectedPacketIndex);
			}
		}
		if (GodotObject.IsInstanceValid(_packetSummaryLabel))
		{
			_packetSummaryLabel.Text = $"当前分类: {EmptyToPlaceholder(_selectedCategory)}  卡片数量: {selectedPacketArray.Count}";
		}
	}

	private void RefreshPacketGrid()
	{
		if (!GodotObject.IsInstanceValid(_packetGrid))
		{
			return;
		}
		foreach (Node child in _packetGrid.GetChildren())
		{
			_packetGrid.RemoveChild(child);
			child.QueueFree();
		}
		Godot.Collections.Array selectedPacketArray = GetSelectedPacketArray();
		if (selectedPacketArray.Count == 0)
		{
			_packetGrid.AddChild(CreatePacketCard("未配置", "选择分类后添加卡片 saveKey", selected: false, -1), forceReadableName: false, InternalMode.Disabled);
			return;
		}
		for (int i = 0; i < selectedPacketArray.Count; i++)
		{
			_packetGrid.AddChild(CreatePacketCard(FormatVariant(selectedPacketArray[i]), $"#{i + 1}", i == _selectedPacketIndex, i), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private Control CreatePacketCard(string packetKey, string subtitle, bool selected, int packetIndex)
	{
		Button button = new Button
		{
			Name = "PacketPreviewCard",
			Text = "",
			CustomMinimumSize = new Vector2(148f, 188f),
			ClipContents = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			FocusMode = FocusModeEnum.None
		};
		button.AddThemeStyleboxOverride("normal", CreatePanelStyle(selected ? new Color(0.18f, 0.3f, 0.46f) : new Color(0.13f, 0.135f, 0.14f), (!selected) ? 1 : 2, 4));
		button.AddThemeStyleboxOverride("hover", CreatePanelStyle(new Color(0.2f, 0.32f, 0.48f), 2, 4));
		button.AddThemeStyleboxOverride("pressed", CreatePanelStyle(new Color(0.14f, 0.25f, 0.39f), 2, 4));
		if (packetIndex >= 0)
		{
			button.Pressed += delegate
			{
				SelectPacket(packetIndex);
			};
		}
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			MouseFilter = MouseFilterEnum.Ignore
		};
		vBoxContainer.SetAnchorsPreset(LayoutPreset.FullRect);
		vBoxContainer.OffsetLeft = 6f;
		vBoxContainer.OffsetTop = 6f;
		vBoxContainer.OffsetRight = -6f;
		vBoxContainer.OffsetBottom = -6f;
		vBoxContainer.AddThemeConstantOverride("separation", 4);
		button.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		Control control = new Control
		{
			Name = "PacketBankCardGamePreview",
			CustomMinimumSize = new Vector2(130f, 140f),
			ClipContents = true,
			MouseFilter = MouseFilterEnum.Ignore
		};
		vBoxContainer.AddChild(control, forceReadableName: false, InternalMode.Disabled);
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacketConfigForSelector(new PacketSelectionOption
		{
			Key = packetKey
		});
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		if (GodotObject.IsInstanceValid(towerDefensePacketConfig))
		{
			try
			{
				towerDefenseInGamePacketShow = TowerDefenseManager.CreatePacketShow();
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.Name = "PacketBankCardPacketShow";
					towerDefenseInGamePacketShow.Position = new Vector2(65f, 73f);
					towerDefenseInGamePacketShow.Scale = new Vector2(0.88f, 0.88f);
					towerDefenseInGamePacketShow.MouseFilter = MouseFilterEnum.Ignore;
					control.AddChild(towerDefenseInGamePacketShow, forceReadableName: false, InternalMode.Disabled);
					ConfigurePacketSelectorPacketShow(towerDefenseInGamePacketShow, towerDefensePacketConfig);
				}
			}
			catch (Exception ex)
			{
				if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
				{
					towerDefenseInGamePacketShow.QueueFree();
				}
				GD.PushWarning("PacketBank card preview failed: " + packetKey + " " + ex.Message);
			}
		}
		if (!GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			control.AddChild(CreatePacketSelectorFallbackLabel(new PacketSelectionOption
			{
				Key = packetKey
			}), forceReadableName: false, InternalMode.Disabled);
		}
		vBoxContainer.AddChild(new Label
		{
			Text = packetKey,
			HorizontalAlignment = HorizontalAlignment.Center,
			ClipText = true,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = (selected ? (subtitle + " · 当前选择") : subtitle),
			HorizontalAlignment = HorizontalAlignment.Center,
			ClipText = true,
			MouseFilter = MouseFilterEnum.Ignore,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		return button;
	}

	private Dictionary DuplicateCategoryDictionary()
	{
		if (_editingPacketBank?.category == null)
		{
			return new Dictionary();
		}
		return _editingPacketBank.category.Duplicate(deep: true);
	}

	private Godot.Collections.Array GetSelectedPacketArray()
	{
		if (_editingPacketBank == null || string.IsNullOrWhiteSpace(_selectedCategory))
		{
			return new Godot.Collections.Array();
		}
		return GetCategoryArray(_editingPacketBank.category, _selectedCategory, create: false);
	}

	private static Godot.Collections.Array GetCategoryArray(Dictionary dict, string categoryName, bool create)
	{
		if (dict == null || string.IsNullOrWhiteSpace(categoryName))
		{
			return new Godot.Collections.Array();
		}
		if (!dict.ContainsKey(categoryName))
		{
			if (!create)
			{
				return new Godot.Collections.Array();
			}
			dict[categoryName] = new Godot.Collections.Array();
		}
		Variant variant = dict[categoryName];
		if (variant.VariantType == Variant.Type.Array)
		{
			return variant.AsGodotArray();
		}
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (create)
		{
			dict[categoryName] = array;
		}
		return array;
	}

	private void EnsureCategorySelected()
	{
		if (string.IsNullOrWhiteSpace(_selectedCategory))
		{
			Dictionary dictionary = _editingPacketBank?.category;
			if (dictionary != null && dictionary.Count != 0)
			{
				_selectedCategory = GetCategoryAt(0);
				_selectedCategoryIndex = (string.IsNullOrWhiteSpace(_selectedCategory) ? (-1) : 0);
			}
		}
	}

	private string GetCategoryAt(int index)
	{
		if (_editingPacketBank?.category == null || index < 0)
		{
			return "";
		}
		int num = 0;
		foreach (Variant key in _editingPacketBank.category.Keys)
		{
			if (num++ == index)
			{
				return key.AsString();
			}
		}
		return "";
	}

	private static string ReadPacketKey(Resource resource)
	{
		if (!GodotObject.IsInstanceValid(resource))
		{
			return "";
		}
		if (resource is TowerDefensePacketConfig towerDefensePacketConfig)
		{
			return towerDefensePacketConfig.saveKey;
		}
		try
		{
			Variant variant = resource.Get("saveKey");
			if (variant.VariantType != Variant.Type.Nil)
			{
				return variant.AsString();
			}
		}
		catch
		{
		}
		if (!string.IsNullOrWhiteSpace(resource.ResourcePath))
		{
			return resource.ResourcePath.GetFile().GetBaseName();
		}
		return resource.ResourceName;
	}

	private void AddSummaryRows(TowerDefensePacketBankData packetBank)
	{
		AddItemIfMissing(base.PreviewList, BuildSummary(packetBank));
		foreach (Variant key in packetBank.category.Keys)
		{
			string text = key.AsString();
			AddItemIfMissing(base.PreviewList, $"分类 {text}: {GetCategoryArray(packetBank.category, text, create: false).Count}");
		}
		AddItemIfMissing(base.TimelineList, $"category -> {packetBank.category.Count}");
		AddItemIfMissing(base.TimelineList, $"packet total -> {CountPackets(packetBank)}");
		AddItemIfMissing(base.GraphList, "卡牌库 -> category -> packet saveKey -> 卡片资源");
		AddItemIfMissing(base.ReferenceList, "卡牌库资源 -> " + base.CurrentResourcePath);
	}

	private static string BuildSummary(TowerDefensePacketBankData packetBank)
	{
		string value = (string.IsNullOrWhiteSpace(packetBank?.ResourceName) ? "未命名卡牌库" : packetBank.ResourceName);
		string value2 = ((packetBank != null && packetBank.ResourceLocalToScene) ? " · 仅当前场景" : "");
		return $"{value}{value2} · 分类 {(packetBank?.category?.Count).GetValueOrDefault()}, 卡片 {CountPackets(packetBank)}";
	}

	private static int CountPackets(TowerDefensePacketBankData packetBank)
	{
		if (packetBank?.category == null)
		{
			return 0;
		}
		int num = 0;
		foreach (Variant key in packetBank.category.Keys)
		{
			num += GetCategoryArray(packetBank.category, key.AsString(), create: false).Count;
		}
		return num;
	}

	private static StyleBoxFlat CreatePanelStyle(Color color, int borderWidth, int cornerRadius)
	{
		return new StyleBoxFlat
		{
			BgColor = color,
			CornerRadiusTopLeft = cornerRadius,
			CornerRadiusTopRight = cornerRadius,
			CornerRadiusBottomLeft = cornerRadius,
			CornerRadiusBottomRight = cornerRadius,
			ContentMarginLeft = 10f,
			ContentMarginRight = 10f,
			ContentMarginTop = 10f,
			ContentMarginBottom = 10f,
			BorderColor = color.Lightened(0.22f),
			BorderWidthLeft = borderWidth,
			BorderWidthRight = borderWidth,
			BorderWidthTop = borderWidth,
			BorderWidthBottom = borderWidth
		};
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

	private static string FormatVariant(Variant value)
	{
		switch (value.VariantType)
		{
		case Variant.Type.Nil:
			return "空";
		case Variant.Type.String:
		case Variant.Type.StringName:
			return value.AsString();
		case Variant.Type.Object:
			return (value.AsGodotObject() is Resource resource) ? ReadPacketKey(resource) : (value.AsGodotObject()?.GetType().Name ?? "空");
		default:
			return value.ToString();
		}
	}

	private static string EmptyToPlaceholder(string value)
	{
		if (!string.IsNullOrWhiteSpace(value))
		{
			return value;
		}
		return "未配置";
	}
}
