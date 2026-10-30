using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.ModSystem;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWFallingObjectVisualResourceEditor : XWGenericVisualResourceEditor
{
	private sealed class FallingObjectObjectCategory
	{
		public string Name { get; }

		public string Description { get; }

		public HashSet<ObjectManagerConfig.OBJECT> Objects { get; }

		public FallingObjectObjectCategory(string name, string description, params ObjectManagerConfig.OBJECT[] objects)
		{
			Name = name;
			Description = description;
			Objects = new HashSet<ObjectManagerConfig.OBJECT>(objects);
		}

		public bool Contains(ObjectManagerConfig.OBJECT value)
		{
			if (!(Name == "全部"))
			{
				return Objects.Contains(value);
			}
			return true;
		}
	}

	private const string EditorLayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectVisualEditorLayout.tscn";

	private const string SimulationPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectSimulationPreview.tscn";

	private const string WeightItemPreviewScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectWeightItemPreview.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const string ObjectPoolVisualPickerScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWObjectPoolVisualPicker.tscn";

	private static PackedScene _editorLayoutScene;

	private static PackedScene _simulationPreviewScene;

	private static PackedScene _weightItemPreviewScene;

	private static PackedScene _visualChoiceCardScene;

	private static PackedScene _objectPoolVisualPickerScene;

	private static readonly FallingObjectObjectCategory[] FallingObjectCategories = new FallingObjectObjectCategory[7]
	{
		new FallingObjectObjectCategory("全部", "显示所有对象池条目。"),
		new FallingObjectObjectCategory("空项", "权重命中后不生成任何掉落物。", default(ObjectManagerConfig.OBJECT)),
		new FallingObjectObjectCategory("阳光", "阳光、脑子阳光和特殊阳光。", ObjectManagerConfig.OBJECT.GAMECOLLECT, ObjectManagerConfig.OBJECT.SUN, ObjectManagerConfig.OBJECT.SUN_BRAIN, ObjectManagerConfig.OBJECT.SUN_JALAPENO),
		new FallingObjectObjectCategory("货币", "银币、金币、钻石和其它货币掉落。", ObjectManagerConfig.OBJECT.COIN, ObjectManagerConfig.OBJECT.COIN_SILVER, ObjectManagerConfig.OBJECT.COIN_GOLD, ObjectManagerConfig.OBJECT.COIN_DIAMOND, ObjectManagerConfig.OBJECT.COIN_LUCKY_BAG, ObjectManagerConfig.OBJECT.COIN_TQ, ObjectManagerConfig.OBJECT.COIN_YB1, ObjectManagerConfig.OBJECT.COIN_YB2, ObjectManagerConfig.OBJECT.COIN_GOLD_SHARD),
		new FallingObjectObjectCategory("战斗", "子弹、伤害部件等战斗对象。", ObjectManagerConfig.OBJECT.PROJECTILE, ObjectManagerConfig.OBJECT.damagePart),
		new FallingObjectObjectCategory("特效", "一次性粒子和地面特效。", ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, ObjectManagerConfig.OBJECT.PARTICLES_RISE_DIRT, ObjectManagerConfig.OBJECT.PARTICLES_ICE_TRAP),
		new FallingObjectObjectCategory("其它", "未被上面分类收纳的对象池条目。")
	};

	private FallingObjectConfig _editingFallingObject;

	private Label _summaryLabel;

	private Label _pickPreviewLabel;

	private Label _lastPickLabel;

	private Label _categorySummaryLabel;

	private ItemList _weightList;

	private GridContainer _weightGrid;

	private GridContainer _simulationGrid;

	private Node2D _dropPreviewRoot;

	private HFlowContainer _categoryGrid;

	private XWObjectPoolVisualPicker _objectPicker;

	private SpinBox _weightSpin;

	private CheckBox _emptyCheck;

	private string _selectedCategory = "全部";

	private int _selectedWeightIndex = -1;

	private int _simulationRuns;

	private readonly System.Collections.Generic.Dictionary<int, int> _simulationHits = new System.Collections.Generic.Dictionary<int, int>();

	private readonly RandomNumberGenerator _previewRandom = new RandomNumberGenerator();

	private bool _updatingControls;

	private Timer _fallingObjectSaveTimer;

	private XWVisualPropertyBinding _fallingObjectPropertyBinding;

	private XWVisualPropertyBinding _weightItemPropertyBinding;

	private FallingObjectWeightItemConfig _editingWeightItem;

	private SpinBox _weightItemComparisonSpin;

	private ProgressBar _weightItemProbability;

	private Label _weightItemProbabilityLabel;

	private Label _weightItemTitleLabel;

	private Label _weightItemStatusLabel;

	private Node2D _weightItemDropRoot;

	private XWObjectPoolVisualPicker _weightItemObjectPicker;

	private SpinBox _weightItemWeightSpin;

	private CheckBox _weightItemEmptyCheck;

	private SubViewport _simulationViewport;

	private SubViewport _weightItemPreviewViewport;

	private int _previewSequenceGeneration;

	public int FallingCategoryVisualCardCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_categoryGrid))
			{
				return 0;
			}
			return _categoryGrid.GetChildCount();
		}
	}

	public bool HasFallingObjectLibrarySearchAndPaging
	{
		get
		{
			if (GodotObject.IsInstanceValid(_objectPicker))
			{
				return _objectPicker.HasSearchAndPaging;
			}
			return false;
		}
	}

	public bool HasWeightItemLibrarySearchAndPaging
	{
		get
		{
			if (GodotObject.IsInstanceValid(_weightItemObjectPicker))
			{
				return _weightItemObjectPicker.HasSearchAndPaging;
			}
			return false;
		}
	}

	public int PreviewSequenceGeneration => _previewSequenceGeneration;

	public bool IsFallingPreviewRendering
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_simulationViewport) || _simulationViewport.RenderTargetUpdateMode == SubViewport.UpdateMode.Disabled)
			{
				if (GodotObject.IsInstanceValid(_weightItemPreviewViewport))
				{
					return _weightItemPreviewViewport.RenderTargetUpdateMode != SubViewport.UpdateMode.Disabled;
				}
				return false;
			}
			return true;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		_fallingObjectSaveTimer = new Timer
		{
			WaitTime = 0.35,
			OneShot = true
		};
		_fallingObjectSaveTimer.Timeout += SavePendingFallingObjectResource;
		AddChild(_fallingObjectSaveTimer, forceReadableName: false, InternalMode.Disabled);
	}

	public override void _ExitTree()
	{
		DisposeVisualPropertyBindings();
		if (GodotObject.IsInstanceValid(_fallingObjectSaveTimer))
		{
			_fallingObjectSaveTimer.Stop();
		}
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		SubViewport.UpdateMode renderTargetUpdateMode = (SubViewport.UpdateMode)(visible ? 4 : 0);
		if (GodotObject.IsInstanceValid(_simulationViewport))
		{
			_simulationViewport.RenderTargetUpdateMode = renderTargetUpdateMode;
		}
		if (GodotObject.IsInstanceValid(_weightItemPreviewViewport))
		{
			_weightItemPreviewViewport.RenderTargetUpdateMode = renderTargetUpdateMode;
		}
		if (!visible)
		{
			_previewSequenceGeneration++;
		}
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeVisualPropertyBindings();
		Resource currentResource = base.CurrentResource;
		if (!(currentResource is FallingObjectConfig fallingObjectConfig))
		{
			if (currentResource is FallingObjectWeightItemConfig fallingObjectWeightItemConfig)
			{
				_editingFallingObject = null;
				_editingWeightItem = fallingObjectWeightItemConfig;
				RenderWeightItemPreview(fallingObjectWeightItemConfig);
			}
		}
		else
		{
			_editingFallingObject = fallingObjectConfig;
			_editingWeightItem = null;
			RenderFallingObjectEditor(fallingObjectConfig);
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!HasCompleteFallingObjectVisualCoverage(resource))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private static bool HasCompleteFallingObjectVisualCoverage(Resource resource)
	{
		Type type = resource?.GetType();
		if (!(type == typeof(FallingObjectConfig)))
		{
			return type == typeof(FallingObjectWeightItemConfig);
		}
		return true;
	}

	private void DisposeVisualPropertyBindings()
	{
		_fallingObjectPropertyBinding?.Dispose();
		_fallingObjectPropertyBinding = null;
		_weightItemPropertyBinding?.Dispose();
		_weightItemPropertyBinding = null;
		_editingFallingObject = null;
		_editingWeightItem = null;
	}

	private void RenderWeightItemPreview(FallingObjectWeightItemConfig weightItem)
	{
		if (base.CanvasGrid == null || !GodotObject.IsInstanceValid(weightItem))
		{
			return;
		}
		if (_weightItemPreviewScene == null)
		{
			_weightItemPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectWeightItemPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Control control = _weightItemPreviewScene?.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		if (GodotObject.IsInstanceValid(control))
		{
			base.CanvasGrid.Columns = 1;
			_weightItemComparisonSpin = control.GetNode<SpinBox>("Layout/ComparisonRow/ComparisonWeight");
			_weightItemProbability = control.GetNode<ProgressBar>("Layout/ComparisonRow/Probability");
			_weightItemProbabilityLabel = control.GetNode<Label>("Layout/ComparisonRow/ProbabilityLabel");
			_weightItemTitleLabel = control.GetNode<Label>("Layout/Header/Title");
			_weightItemStatusLabel = control.GetNode<Label>("Layout/Header/Status");
			_weightItemDropRoot = control.GetNode<Node2D>("%DropRoot");
			_weightItemPreviewViewport = control.GetNode<SubViewport>("Layout/GameStage/ViewportContainer/Viewport");
			_weightItemPreviewViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(IsVisibleInTree() ? 4 : 0);
			_weightItemWeightSpin = control.GetNode<SpinBox>("%WeightSpinBox");
			_weightItemEmptyCheck = control.GetNode<CheckBox>("%EmptyCheckBox");
			MountWeightItemObjectLibrary(control.GetNode<VBoxContainer>("%ObjectLibraryHost"), weightItem.item);
			BindWeightItemDirectFields(control, weightItem);
			_weightItemComparisonSpin.ValueChanged += delegate
			{
				UpdateWeightItemPreview();
			};
			control.GetNode<Button>("Layout/ComparisonRow/DropButton").Pressed += delegate
			{
				SpawnFallingObjectPreview(_editingWeightItem, _weightItemDropRoot);
			};
			base.CanvasGrid.AddChild(control, forceReadableName: false, InternalMode.Disabled);
			base.PreviewList?.AddItem("对照权重只用于计算单项相对概率，不会修改资源。");
			UpdateWeightItemPreview();
		}
	}

	private void BindWeightItemDirectFields(Control preview, FallingObjectWeightItemConfig weightItem)
	{
		_weightItemPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnWeightItemVisualPropertyEdited);
		_weightItemPropertyBinding.BindText(preview.GetNode<LineEdit>("%ResourceNameEdit"), weightItem, "resource_name", UpdateWeightItemPreview, this, "RefreshWeightItemEditorFromHistory");
		_weightItemPropertyBinding.BindToggle(preview.GetNode<CheckButton>("%LocalToSceneCheck"), weightItem, "resource_local_to_scene", UpdateWeightItemPreview, this, "RefreshWeightItemEditorFromHistory");
		_weightItemPropertyBinding.BindNumber(_weightItemWeightSpin, weightItem, "weight", UpdateWeightItemPreview, this, "RefreshWeightItemEditorFromHistory");
		_weightItemPropertyBinding.BindToggle(_weightItemEmptyCheck, weightItem, "empty", UpdateWeightItemPreview, this, "RefreshWeightItemEditorFromHistory");
	}

	private void MountWeightItemObjectLibrary(VBoxContainer host, ObjectManagerConfig.OBJECT selected)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		if (_objectPoolVisualPickerScene == null)
		{
			_objectPoolVisualPickerScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWObjectPoolVisualPicker.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_weightItemObjectPicker = _objectPoolVisualPickerScene?.Instantiate<XWObjectPoolVisualPicker>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_weightItemObjectPicker))
		{
			return;
		}
		_weightItemObjectPicker.Name = "WeightItemObjectLibrary";
		host.AddChild(_weightItemObjectPicker, forceReadableName: false, InternalMode.Disabled);
		_weightItemObjectPicker.Configure(BuildObjectVisualChoices(GetObjectsForCategory("全部")), Convert.ToInt32(selected));
		_weightItemObjectPicker.ChoiceSelected += delegate(int id)
		{
			if (!_updatingControls && _weightItemPropertyBinding != null && GodotObject.IsInstanceValid(_editingWeightItem))
			{
				_weightItemPropertyBinding.SetValue(_editingWeightItem, "item", id, "修改掉落对象", this, "RefreshWeightItemEditorFromHistory");
				UpdateWeightItemPreview();
			}
		};
	}

	private void UpdateWeightItemPreview()
	{
		if (GodotObject.IsInstanceValid(_editingWeightItem))
		{
			double num = (GodotObject.IsInstanceValid(_weightItemComparisonSpin) ? _weightItemComparisonSpin.Value : 100.0);
			double num2 = Math.Max(0, _editingWeightItem.weight);
			double num3 = ((num2 + num <= 0.0) ? 0.0 : (num2 / (num2 + num)));
			if (GodotObject.IsInstanceValid(_weightItemProbability))
			{
				_weightItemProbability.Value = num3 * 100.0;
			}
			if (GodotObject.IsInstanceValid(_weightItemProbabilityLabel))
			{
				_weightItemProbabilityLabel.Text = $"概率 {num3:P1}";
			}
			if (GodotObject.IsInstanceValid(_weightItemTitleLabel))
			{
				_weightItemTitleLabel.Text = (string.IsNullOrWhiteSpace(_editingWeightItem.ResourceName) ? "单项掉落权重预览" : (_editingWeightItem.ResourceName + " · 掉落权重"));
			}
			if (GodotObject.IsInstanceValid(_weightItemStatusLabel))
			{
				_weightItemStatusLabel.Text = (_editingWeightItem.empty ? $"空掉落 · 权重 {_editingWeightItem.weight}" : $"{_editingWeightItem.item} · 权重 {_editingWeightItem.weight}");
			}
		}
	}

	private void RenderFallingObjectEditor(FallingObjectConfig fallingObject)
	{
		if (base.CanvasGrid != null)
		{
			base.CanvasGrid.Columns = 1;
			if (_editorLayoutScene == null)
			{
				_editorLayoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _editorLayoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindFallingObjectLayout(vBoxContainer, fallingObject);
				RefreshAll();
				BindSelectedWeightItemFields();
				AddSummaryRows(fallingObject);
			}
		}
	}

	private void BindFallingObjectLayout(VBoxContainer root, FallingObjectConfig fallingObject)
	{
		_fallingObjectPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnFallingObjectVisualPropertyEdited);
		_fallingObjectPropertyBinding.BindText(root.GetNode<LineEdit>("%ResourceNameEdit"), fallingObject, "resource_name", null, this, "RefreshFallingObjectEditorFromHistory");
		_fallingObjectPropertyBinding.BindToggle(root.GetNode<CheckButton>("%LocalToSceneCheck"), fallingObject, "resource_local_to_scene", null, this, "RefreshFallingObjectEditorFromHistory");
		root.GetNode<VBoxContainer>("%SimulationHost").AddChild(CreateFallingObjectPreviewPanel(fallingObject), forceReadableName: false, InternalMode.Disabled);
		_categorySummaryLabel = root.GetNode<Label>("%CategorySummaryLabel");
		_categoryGrid = root.GetNode<HFlowContainer>("%CategoryGrid");
		MountFallingObjectLibrary(root.GetNode<VBoxContainer>("%ObjectLibraryHost"));
		_weightSpin = root.GetNode<SpinBox>("%WeightSpinBox");
		_emptyCheck = root.GetNode<CheckBox>("%EmptyCheckBox");
		root.GetNode<Button>("%AddWeightButton").Pressed += AddWeightItem;
		root.GetNode<Button>("%RemoveWeightButton").Pressed += RemoveSelectedWeightItem;
		root.GetNode<Button>("%WeightUpButton").Pressed += delegate
		{
			MoveSelectedWeightItem(-1);
		};
		root.GetNode<Button>("%WeightDownButton").Pressed += delegate
		{
			MoveSelectedWeightItem(1);
		};
		_weightList = root.GetNode<ItemList>("%WeightList");
		_weightList.ItemSelected += delegate(long index)
		{
			SelectWeightItem((int)index);
		};
		_weightGrid = root.GetNode<GridContainer>("%WeightGrid");
	}

	private void MountFallingObjectLibrary(VBoxContainer host)
	{
		if (!GodotObject.IsInstanceValid(host))
		{
			return;
		}
		if (_objectPoolVisualPickerScene == null)
		{
			_objectPoolVisualPickerScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWObjectPoolVisualPicker.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		_objectPicker = _objectPoolVisualPickerScene?.Instantiate<XWObjectPoolVisualPicker>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_objectPicker))
		{
			return;
		}
		_objectPicker.Name = "FallingObjectLibrary";
		host.AddChild(_objectPicker, forceReadableName: false, InternalMode.Disabled);
		RefreshObjectLibraryForCategory();
		_objectPicker.ChoiceSelected += delegate(int id)
		{
			if (!_updatingControls)
			{
				SetWeightItemProperty("item", Variant.From(in id));
			}
		};
	}

	private Control CreateFallingObjectPreviewPanel(FallingObjectConfig fallingObject)
	{
		if (_simulationPreviewScene == null)
		{
			_simulationPreviewScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWFallingObjectSimulationPreview.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		if (!GodotObject.IsInstanceValid(_simulationPreviewScene))
		{
			return new Label
			{
				Text = "掉落模拟场景加载失败"
			};
		}
		Control control = _simulationPreviewScene.Instantiate<Control>(PackedScene.GenEditState.Disabled);
		_summaryLabel = control.GetNode<Label>("Layout/Summary");
		_pickPreviewLabel = control.GetNode<Label>("Layout/PickSummary");
		_lastPickLabel = control.GetNode<Label>("Layout/Toolbar/LastPick");
		_simulationGrid = control.GetNode<GridContainer>("Layout/SimulationGrid");
		_dropPreviewRoot = control.GetNode<Node2D>("%DropRoot");
		_simulationViewport = control.GetNode<SubViewport>("Layout/GameStage/ViewportContainer/Viewport");
		_simulationViewport.RenderTargetUpdateMode = (SubViewport.UpdateMode)(IsVisibleInTree() ? 4 : 0);
		control.GetNode<SpinBox>("Layout/Toolbar/SimulationCount");
		control.GetNode<Button>("Layout/Toolbar/PickOnceButton").Pressed += RunSinglePickPreview;
		control.GetNode<Button>("Layout/Toolbar/SimulateButton").Pressed += RunDropSequencePreview;
		control.GetNode<Button>("Layout/Toolbar/ClearButton").Pressed += ClearDropPreview;
		_summaryLabel.Text = BuildSummary(fallingObject);
		_previewRandom.Randomize();
		return control;
	}

	private void AddWeightItem()
	{
		if (_editingFallingObject != null && _fallingObjectPropertyBinding != null)
		{
			Array<FallingObjectWeightItemConfig> array = DuplicateWeightItems(_editingFallingObject.weightItem);
			bool flag = _selectedCategory == "空项";
			FallingObjectWeightItemConfig item = new FallingObjectWeightItemConfig
			{
				item = ((!flag) ? ReadSelectedObject() : ObjectManagerConfig.OBJECT.NOONE),
				weight = Mathf.Max(0, Mathf.RoundToInt((float)(_weightSpin?.Value ?? 100.0))),
				empty = (flag || (_emptyCheck?.ButtonPressed ?? false))
			};
			array.Add(item);
			_selectedWeightIndex = array.Count - 1;
			SetFallingObjectWeightItems(array, "添加掉落权重项");
		}
	}

	private void RemoveSelectedWeightItem()
	{
		if (_editingFallingObject?.weightItem != null && _selectedWeightIndex >= 0 && _selectedWeightIndex < _editingFallingObject.weightItem.Count)
		{
			Array<FallingObjectWeightItemConfig> array = DuplicateWeightItems(_editingFallingObject.weightItem);
			array.RemoveAt(_selectedWeightIndex);
			_selectedWeightIndex = Math.Min(_selectedWeightIndex, array.Count - 1);
			SetFallingObjectWeightItems(array, "删除掉落权重项");
		}
	}

	private void MoveSelectedWeightItem(int direction)
	{
		if (_editingFallingObject?.weightItem != null && _selectedWeightIndex >= 0 && direction != 0)
		{
			int num = Math.Clamp(_selectedWeightIndex + direction, 0, _editingFallingObject.weightItem.Count - 1);
			if (num != _selectedWeightIndex)
			{
				Array<FallingObjectWeightItemConfig> array = DuplicateWeightItems(_editingFallingObject.weightItem);
				FallingObjectWeightItemConfig item = array[_selectedWeightIndex];
				array.RemoveAt(_selectedWeightIndex);
				array.Insert(num, item);
				_selectedWeightIndex = num;
				SetFallingObjectWeightItems(array, (direction < 0) ? "上移掉落权重项" : "下移掉落权重项");
			}
		}
	}

	private static Array<FallingObjectWeightItemConfig> DuplicateWeightItems(Array<FallingObjectWeightItemConfig> source)
	{
		if (source != null)
		{
			return source.Duplicate();
		}
		return new Array<FallingObjectWeightItemConfig>();
	}

	private void SetFallingObjectWeightItems(Array<FallingObjectWeightItemConfig> nextItems, string actionName)
	{
		if (_fallingObjectPropertyBinding != null && GodotObject.IsInstanceValid(_editingFallingObject))
		{
			_weightItemPropertyBinding?.Dispose();
			_weightItemPropertyBinding = null;
			_editingWeightItem = null;
			_fallingObjectPropertyBinding.SetValue(_editingFallingObject, "weightItem", nextItems, actionName, this, "RefreshFallingObjectEditorFromHistory");
			BindSelectedWeightItemFields();
		}
	}

	private void SetWeightItemProperty(string propertyName, Variant value)
	{
		if (!_updatingControls && _editingFallingObject?.weightItem != null && _selectedWeightIndex >= 0 && _selectedWeightIndex < _editingFallingObject.weightItem.Count)
		{
			FallingObjectWeightItemConfig fallingObjectWeightItemConfig = _editingFallingObject.weightItem[_selectedWeightIndex];
			if (fallingObjectWeightItemConfig != null && _weightItemPropertyBinding != null && propertyName == "item")
			{
				_weightItemPropertyBinding.SetValue(fallingObjectWeightItemConfig, "item", value, "修改掉落对象", this, "RefreshFallingObjectEditorFromHistory");
			}
		}
	}

	private void BindSelectedWeightItemFields()
	{
		_weightItemPropertyBinding?.Dispose();
		_weightItemPropertyBinding = null;
		_editingWeightItem = GetSelectedWeightItem();
		if (GodotObject.IsInstanceValid(_editingWeightItem))
		{
			_weightItemPropertyBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnWeightItemVisualPropertyEdited);
			_weightItemPropertyBinding.BindNumber(_weightSpin, _editingWeightItem, "weight", null, this, "RefreshFallingObjectEditorFromHistory");
			_weightItemPropertyBinding.BindToggle(_emptyCheck, _editingWeightItem, "empty", null, this, "RefreshFallingObjectEditorFromHistory");
		}
	}

	private void OnFallingObjectVisualPropertyEdited(bool committed)
	{
		OnFallingObjectChanged();
	}

	private void OnFallingObjectChanged()
	{
		if (GodotObject.IsInstanceValid(_editingFallingObject) && base.CurrentResource is FallingObjectConfig fallingObjectConfig && fallingObjectConfig == _editingFallingObject)
		{
			MarkCurrentResourceDirty();
			_editingFallingObject.EmitChanged();
			_editingFallingObject.NotifyPropertyListChanged();
			ScheduleFallingObjectSave();
			ResetSimulationResults();
			RefreshAll();
		}
	}

	private void OnWeightItemVisualPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(_editingWeightItem))
		{
			if (base.CurrentResource is FallingObjectWeightItemConfig fallingObjectWeightItemConfig && fallingObjectWeightItemConfig == _editingWeightItem)
			{
				MarkCurrentResourceDirty();
				fallingObjectWeightItemConfig.EmitChanged();
				ScheduleFallingObjectSave();
				UpdateWeightItemPreview();
			}
			else if (base.CurrentResource is FallingObjectConfig fallingObjectConfig && fallingObjectConfig == _editingFallingObject)
			{
				MarkCurrentResourceDirty();
				fallingObjectConfig.EmitChanged();
				fallingObjectConfig.NotifyPropertyListChanged();
				ScheduleFallingObjectSave();
				RefreshFallingObjectVisualsAfterWeightItemEdit();
			}
		}
	}

	private void ScheduleFallingObjectSave()
	{
		if (GodotObject.IsInstanceValid(_fallingObjectSaveTimer))
		{
			_fallingObjectSaveTimer.Start();
		}
	}

	private void RefreshFallingObjectVisualsAfterWeightItemEdit()
	{
		ResetSimulationResults();
		if (GodotObject.IsInstanceValid(_summaryLabel))
		{
			_summaryLabel.Text = BuildSummary(_editingFallingObject);
		}
		if (GodotObject.IsInstanceValid(_pickPreviewLabel))
		{
			_pickPreviewLabel.Text = $"Pick 预览: 总权重 {GetTotalWeight(_editingFallingObject)}, 空项 {CountEmpty(_editingFallingObject)}";
		}
		if (GodotObject.IsInstanceValid(_categorySummaryLabel))
		{
			_categorySummaryLabel.Text = BuildCategorySummary(_editingFallingObject);
		}
		RefreshCategoryGrid();
		RefreshWeightList();
		RefreshWeightGrid();
	}

	public void RefreshFallingObjectEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			MarkCurrentResourceDirty();
			base.CurrentResource?.EmitChanged();
			ScheduleFallingObjectSave();
			RebuildCurrentFallingObjectEditor();
		}
	}

	public void RefreshWeightItemEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			MarkCurrentResourceDirty();
			base.CurrentResource?.EmitChanged();
			ScheduleFallingObjectSave();
			RebuildCurrentFallingObjectEditor();
		}
	}

	private void RebuildCurrentFallingObjectEditor()
	{
		if (base.CanvasGrid == null)
		{
			return;
		}
		Resource currentResource = base.CurrentResource;
		if (currentResource == null)
		{
			return;
		}
		DisposeVisualPropertyBindings();
		foreach (Node child in base.CanvasGrid.GetChildren())
		{
			base.CanvasGrid.RemoveChild(child);
			child.QueueFree();
		}
		if (!(currentResource is FallingObjectConfig fallingObjectConfig))
		{
			if (currentResource is FallingObjectWeightItemConfig fallingObjectWeightItemConfig)
			{
				_editingFallingObject = null;
				_editingWeightItem = fallingObjectWeightItemConfig;
				RenderWeightItemPreview(fallingObjectWeightItemConfig);
			}
		}
		else
		{
			_editingFallingObject = fallingObjectConfig;
			_editingWeightItem = null;
			_selectedWeightIndex = Math.Min(_selectedWeightIndex, (fallingObjectConfig.weightItem?.Count ?? 0) - 1);
			RenderFallingObjectEditor(fallingObjectConfig);
		}
	}

	private void SavePendingFallingObjectResource()
	{
		Resource currentResource = base.CurrentResource;
		if (!(currentResource is FallingObjectConfig resource))
		{
			if (currentResource is FallingObjectWeightItemConfig resource2)
			{
				SaveFallingObjectResource(resource2);
			}
		}
		else
		{
			SaveFallingObjectResource(resource);
		}
	}

	private void SaveFallingObjectResource(Resource resource)
	{
		string currentResourcePath = base.CurrentResourcePath;
		if (string.IsNullOrWhiteSpace(currentResourcePath) || !GodotObject.IsInstanceValid(resource))
		{
			return;
		}
		Error error = ResourceSaver.Save(resource, currentResourcePath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushWarning($"FallingObject editor save failed: {error} {currentResourcePath}");
		}
		else
		{
			string text = XWFileSystem.GetSingleton()?.ProjectFolderPath ?? "";
			if (!string.IsNullOrWhiteSpace(text))
			{
				XWModManifestSyncService.RegisterPath(text, currentResourcePath);
			}
		}
	}

	private void RunSinglePickPreview()
	{
		int num = PickWeightedIndex();
		if (num < 0)
		{
			if (GodotObject.IsInstanceValid(_lastPickLabel))
			{
				_lastPickLabel.Text = "无法抽取：总权重为 0";
			}
			return;
		}
		RecordSimulationHit(num);
		FallingObjectWeightItemConfig fallingObjectWeightItemConfig = _editingFallingObject.weightItem[num];
		if (GodotObject.IsInstanceValid(_lastPickLabel))
		{
			_lastPickLabel.Text = ((fallingObjectWeightItemConfig != null && fallingObjectWeightItemConfig.empty) ? "本次结果：空掉落" : $"本次结果：{fallingObjectWeightItemConfig?.item}");
		}
		_selectedWeightIndex = num;
		RefreshWeightList();
		RefreshSelectedWeightControls();
		RefreshWeightGrid();
		RefreshSimulationGrid();
		BindSelectedWeightItemFields();
		SpawnFallingObjectPreview(fallingObjectWeightItemConfig, _dropPreviewRoot);
	}

	private async void RunDropSequencePreview()
	{
		int generation = ++_previewSequenceGeneration;
		for (int i = 0; i < 8; i++)
		{
			if (generation != _previewSequenceGeneration)
			{
				break;
			}
			if (!IsVisibleInTree())
			{
				break;
			}
			RunSinglePickPreview();
			if (!IsInsideTree())
			{
				break;
			}
			await ToSignal(GetTree().CreateTimer(0.18, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		}
	}

	private void SpawnFallingObjectPreview(FallingObjectWeightItemConfig item, Node2D previewRoot)
	{
		if (!GodotObject.IsInstanceValid(previewRoot) || !GodotObject.IsInstanceValid(item) || item.empty || item.item == ObjectManagerConfig.OBJECT.NOONE)
		{
			return;
		}
		PackedScene poolScene = ObjectManager.GetPoolScene(item.item);
		if (!GodotObject.IsInstanceValid(poolScene))
		{
			if (GodotObject.IsInstanceValid(_lastPickLabel))
			{
				_lastPickLabel.Text = $"{item.item} 的对象池场景尚未加载";
			}
			return;
		}
		try
		{
			Node node = poolScene.Instantiate(PackedScene.GenEditState.Disabled);
			Node2D node2D = new Node2D
			{
				Name = $"FallingObject_{item.item}",
				Position = new Vector2(_previewRandom.RandfRange(90f, 610f), -55f)
			};
			previewRoot.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
			node2D.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			if (node is CanvasItem canvasItem)
			{
				canvasItem.Show();
			}
			Tween tween = node2D.CreateTween();
			tween.SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.In);
			tween.TweenProperty(node2D, "position:y", _previewRandom.RandfRange(205f, 255f), 0.72);
			tween.SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
			tween.TweenProperty(node2D, "scale", new Vector2(1.06f, 0.94f), 0.12);
			tween.TweenProperty(node2D, "scale", Vector2.One, 0.18);
		}
		catch (Exception ex)
		{
			GD.PushWarning($"Falling object game preview failed: {item.item} {ex.Message}");
		}
	}

	private void ClearDropPreview()
	{
		_previewSequenceGeneration++;
		if (GodotObject.IsInstanceValid(_dropPreviewRoot))
		{
			foreach (Node child in _dropPreviewRoot.GetChildren())
			{
				child.QueueFree();
			}
		}
		ResetSimulationResults();
		if (GodotObject.IsInstanceValid(_lastPickLabel))
		{
			_lastPickLabel.Text = "草坪已清空";
		}
	}

	private void RunBatchPickPreview(int sampleCount)
	{
		ResetSimulationResults();
		int num = Math.Clamp(sampleCount, 1, 100000);
		for (int i = 0; i < num; i++)
		{
			int num2 = PickWeightedIndex();
			if (num2 < 0)
			{
				break;
			}
			RecordSimulationHit(num2);
		}
		if (GodotObject.IsInstanceValid(_lastPickLabel))
		{
			_lastPickLabel.Text = ((_simulationRuns > 0) ? $"已模拟 {_simulationRuns:N0} 次" : "无法模拟：总权重为 0");
		}
		RefreshSimulationGrid();
	}

	private int PickWeightedIndex()
	{
		Array<FallingObjectWeightItemConfig> array = _editingFallingObject?.weightItem;
		int totalWeight = GetTotalWeight(_editingFallingObject);
		if (array == null || array.Count == 0 || totalWeight <= 0)
		{
			return -1;
		}
		int num = _previewRandom.RandiRange(0, totalWeight - 1);
		for (int i = 0; i < array.Count; i++)
		{
			int num2 = Math.Max(0, array[i]?.weight ?? 0);
			if (num < num2)
			{
				return i;
			}
			num -= num2;
		}
		return -1;
	}

	private void RecordSimulationHit(int index)
	{
		_simulationRuns++;
		_simulationHits[index] = _simulationHits.GetValueOrDefault(index) + 1;
	}

	private void ResetSimulationResults()
	{
		_simulationRuns = 0;
		_simulationHits.Clear();
		if (GodotObject.IsInstanceValid(_lastPickLabel))
		{
			_lastPickLabel.Text = "等待模拟";
		}
		RefreshSimulationGrid();
	}

	private void RefreshSimulationGrid()
	{
		if (!GodotObject.IsInstanceValid(_simulationGrid))
		{
			return;
		}
		foreach (Node child in _simulationGrid.GetChildren())
		{
			_simulationGrid.RemoveChild(child);
			child.QueueFree();
		}
		Array<FallingObjectWeightItemConfig> array = _editingFallingObject?.weightItem;
		if (array == null || array.Count == 0)
		{
			_simulationGrid.AddChild(new Label
			{
				Text = "添加权重项后可运行掉落模拟。"
			}, forceReadableName: false, InternalMode.Disabled);
			return;
		}
		int num = Math.Max(1, GetTotalWeight(_editingFallingObject));
		for (int i = 0; i < array.Count; i++)
		{
			FallingObjectWeightItemConfig fallingObjectWeightItemConfig = array[i];
			float expected = (float)Math.Max(0, fallingObjectWeightItemConfig?.weight ?? 0) / (float)num;
			int valueOrDefault = _simulationHits.GetValueOrDefault(i);
			float observed = ((_simulationRuns > 0) ? ((float)valueOrDefault / (float)_simulationRuns) : 0f);
			_simulationGrid.AddChild(CreateSimulationResultCard(fallingObjectWeightItemConfig, expected, observed, valueOrDefault), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private Control CreateSimulationResultCard(FallingObjectWeightItemConfig item, float expected, float observed, int hits)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = "FallingObjectSimulationResultCard";
		panelContainer.CustomMinimumSize = new Vector2(165f, 58f);
		panelContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle((item != null && item.empty) ? new Color(0.24f, 0.17f, 0.14f) : new Color(0.12f, 0.18f, 0.23f), 1, 3));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 2);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = FormatWeightItem(item),
			ClipText = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = ((_simulationRuns > 0) ? $"期望 {expected:P1} · 实测 {observed:P1} ({hits})" : $"期望 {expected:P1} · 尚未模拟"),
			ClipText = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		ColorRect node = new ColorRect
		{
			Color = new Color(0.35f, 0.68f, 0.91f),
			CustomMinimumSize = new Vector2(Mathf.Clamp((_simulationRuns > 0) ? observed : expected, 0f, 1f) * 145f, 5f)
		};
		vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private void SelectWeightItem(int index)
	{
		_weightItemPropertyBinding?.Dispose();
		_weightItemPropertyBinding = null;
		_selectedWeightIndex = index;
		RefreshSelectedWeightControls();
		RefreshWeightGrid();
		BindSelectedWeightItemFields();
	}

	private void RefreshAll()
	{
		if (_editingFallingObject == null)
		{
			return;
		}
		_updatingControls = true;
		try
		{
			if (GodotObject.IsInstanceValid(_summaryLabel))
			{
				_summaryLabel.Text = BuildSummary(_editingFallingObject);
			}
			if (GodotObject.IsInstanceValid(_pickPreviewLabel))
			{
				_pickPreviewLabel.Text = $"Pick 预览: 总权重 {GetTotalWeight(_editingFallingObject)}, 空项 {CountEmpty(_editingFallingObject)}";
			}
			if (GodotObject.IsInstanceValid(_categorySummaryLabel))
			{
				_categorySummaryLabel.Text = BuildCategorySummary(_editingFallingObject);
			}
			RefreshObjectLibraryForCategory();
			RefreshCategoryGrid();
			RefreshWeightList();
			RefreshSelectedWeightControls();
			RefreshWeightGrid();
			RefreshSimulationGrid();
		}
		finally
		{
			_updatingControls = false;
		}
	}

	private void RefreshWeightList()
	{
		if (!GodotObject.IsInstanceValid(_weightList))
		{
			return;
		}
		_weightList.Clear();
		Array<FallingObjectWeightItemConfig> array = _editingFallingObject?.weightItem;
		if (array == null || array.Count == 0)
		{
			_weightList.AddItem("未配置掉落项");
			_selectedWeightIndex = -1;
			return;
		}
		for (int i = 0; i < array.Count; i++)
		{
			FallingObjectWeightItemConfig item = array[i];
			_weightList.AddItem($"{i + 1}. {FormatWeightItem(item)}");
		}
		if (_selectedWeightIndex >= 0 && _selectedWeightIndex < array.Count)
		{
			_weightList.Select(_selectedWeightIndex);
		}
		else
		{
			_selectedWeightIndex = 0;
		}
	}

	private void RefreshSelectedWeightControls()
	{
		FallingObjectWeightItemConfig selectedWeightItem = GetSelectedWeightItem();
		if (selectedWeightItem != null)
		{
			_objectPicker?.SetSelectedId(Convert.ToInt32(selectedWeightItem.item));
			if (GodotObject.IsInstanceValid(_weightSpin))
			{
				_weightSpin.Value = selectedWeightItem.weight;
			}
			if (GodotObject.IsInstanceValid(_emptyCheck))
			{
				_emptyCheck.ButtonPressed = selectedWeightItem.empty;
			}
		}
	}

	private void RefreshWeightGrid()
	{
		if (!GodotObject.IsInstanceValid(_weightGrid))
		{
			return;
		}
		foreach (Node child in _weightGrid.GetChildren())
		{
			_weightGrid.RemoveChild(child);
			child.QueueFree();
		}
		Array<FallingObjectWeightItemConfig> array = _editingFallingObject?.weightItem;
		if (array == null || array.Count == 0)
		{
			_weightGrid.AddChild(CreateWeightCard("未配置", "添加权重项后可预览 Pick 分布", 0f, selected: false), forceReadableName: false, InternalMode.Disabled);
			return;
		}
		int num = Math.Max(1, GetTotalWeight(_editingFallingObject));
		for (int i = 0; i < array.Count; i++)
		{
			FallingObjectWeightItemConfig fallingObjectWeightItemConfig = array[i];
			float num2 = ((fallingObjectWeightItemConfig == null) ? 0f : ((float)fallingObjectWeightItemConfig.weight / (float)num));
			_weightGrid.AddChild(CreateWeightCard(FormatWeightItem(fallingObjectWeightItemConfig), $"{num2:P1}", num2, i == _selectedWeightIndex), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private Control CreateWeightCard(string title, string subtitle, float ratio, bool selected)
	{
		PanelContainer panelContainer = new PanelContainer();
		panelContainer.Name = "FallingObjectWeightCard";
		panelContainer.CustomMinimumSize = new Vector2(210f, 82f);
		panelContainer.SizeFlagsHorizontal = SizeFlags.ExpandFill;
		panelContainer.AddThemeStyleboxOverride("panel", CreatePanelStyle(selected ? new Color(0.18f, 0.3f, 0.46f) : new Color(0.13f, 0.135f, 0.14f), (!selected) ? 1 : 2, 4));
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		vBoxContainer.AddThemeConstantOverride("separation", 4);
		panelContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = title,
			ClipText = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = subtitle,
			ClipText = true,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		}, forceReadableName: false, InternalMode.Disabled);
		ColorRect node = new ColorRect
		{
			Name = "WeightRatioBar",
			Color = new Color(0.42f, 0.7f, 0.35f, 0.95f),
			CustomMinimumSize = new Vector2(Mathf.Clamp(ratio, 0f, 1f) * 180f, 6f)
		};
		vBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
		return panelContainer;
	}

	private void SetFallingObjectCategory(string categoryName)
	{
		FallingObjectObjectCategory categoryByName = GetCategoryByName(categoryName);
		_selectedCategory = categoryByName.Name;
		if (GodotObject.IsInstanceValid(_emptyCheck) && _selectedCategory == "空项")
		{
			_emptyCheck.ButtonPressed = true;
		}
		RefreshObjectLibraryForCategory();
		if (GodotObject.IsInstanceValid(_categorySummaryLabel))
		{
			_categorySummaryLabel.Text = BuildCategorySummary(_editingFallingObject);
		}
		RefreshCategoryGrid();
	}

	private void RefreshObjectLibraryForCategory()
	{
		if (GodotObject.IsInstanceValid(_objectPicker))
		{
			ObjectManagerConfig.OBJECT oBJECT = GetSelectedWeightItem()?.item ?? ReadSelectedObject();
			List<XWObjectPoolVisualChoice> list = BuildObjectVisualChoices(GetObjectsForCategory(_selectedCategory));
			int selectedId = Convert.ToInt32(oBJECT);
			if (list.Count > 0 && list.Find((XWObjectPoolVisualChoice choice) => choice.Id == selectedId) == null)
			{
				selectedId = list[0].Id;
			}
			_objectPicker.Configure(list, selectedId);
		}
	}

	private static List<XWObjectPoolVisualChoice> BuildObjectVisualChoices(IEnumerable<ObjectManagerConfig.OBJECT> values)
	{
		List<XWObjectPoolVisualChoice> list = new List<XWObjectPoolVisualChoice>();
		foreach (ObjectManagerConfig.OBJECT value in values)
		{
			string objectCategory = GetObjectCategory(value);
			list.Add(new XWObjectPoolVisualChoice(Convert.ToInt32(value), value.ToString(), objectCategory, ResourceLoader.Load<Texture2D>(GetObjectCategoryIconPath(objectCategory), null, ResourceLoader.CacheMode.Reuse)));
		}
		return list;
	}

	private void RefreshCategoryGrid()
	{
		if (!GodotObject.IsInstanceValid(_categoryGrid))
		{
			return;
		}
		foreach (Node child in _categoryGrid.GetChildren())
		{
			_categoryGrid.RemoveChild(child);
			child.QueueFree();
		}
		FallingObjectObjectCategory[] fallingObjectCategories = FallingObjectCategories;
		foreach (FallingObjectObjectCategory category in fallingObjectCategories)
		{
			_categoryGrid.AddChild(CreateCategoryCard(category), forceReadableName: false, InternalMode.Disabled);
		}
	}

	private IEnumerable<ObjectManagerConfig.OBJECT> GetObjectsForCategory(string categoryName)
	{
		ObjectManagerConfig.OBJECT[] values = Enum.GetValues<ObjectManagerConfig.OBJECT>();
		foreach (ObjectManagerConfig.OBJECT oBJECT in values)
		{
			if (oBJECT != ObjectManagerConfig.OBJECT.MAX && (categoryName == "全部" || GetObjectCategory(oBJECT) == categoryName))
			{
				yield return oBJECT;
			}
		}
	}

	private static string GetObjectCategory(ObjectManagerConfig.OBJECT value)
	{
		FallingObjectObjectCategory[] fallingObjectCategories = FallingObjectCategories;
		foreach (FallingObjectObjectCategory fallingObjectObjectCategory in fallingObjectCategories)
		{
			string name = fallingObjectObjectCategory.Name;
			bool flag = ((name == "全部" || name == "其它") ? true : false);
			if (!flag && fallingObjectObjectCategory.Contains(value))
			{
				return fallingObjectObjectCategory.Name;
			}
		}
		return "其它";
	}

	private static string GetObjectCategoryIconPath(string categoryName)
	{
		return categoryName switch
		{
			"全部" => "res://addons/ModEditor/Icons/AssetLib.svg", 
			"空项" => "res://addons/ModEditor/Icons/Clear.svg", 
			"阳光" => "res://addons/ModEditor/Icons/ResourceCollectable.svg", 
			"货币" => "res://addons/ModEditor/Icons/Favorites.svg", 
			"战斗" => "res://addons/ModEditor/Icons/ResourceProjectile.svg", 
			"特效" => "res://addons/ModEditor/Icons/ResourceAnimation.svg", 
			_ => "res://addons/ModEditor/Icons/ResourceFallingObject.svg", 
		};
	}

	private string BuildCategorySummary(FallingObjectConfig fallingObject)
	{
		FallingObjectObjectCategory categoryByName = GetCategoryByName(_selectedCategory);
		int num = 0;
		foreach (ObjectManagerConfig.OBJECT item in GetObjectsForCategory(_selectedCategory))
		{
			_ = item;
			num++;
		}
		int num2 = 0;
		int num3 = 0;
		if (fallingObject?.weightItem != null)
		{
			foreach (FallingObjectWeightItemConfig item2 in fallingObject.weightItem)
			{
				if (item2 != null)
				{
					string text = (item2.empty ? "空项" : GetObjectCategory(item2.item));
					if (!(_selectedCategory != "全部") || !(text != _selectedCategory))
					{
						num2++;
						num3 += Math.Max(0, item2.weight);
					}
				}
			}
		}
		return $"当前分类: {_selectedCategory}  可选 {num}  已用 {num2}  权重 {num3}  {categoryByName?.Description ?? ""}";
	}

	private Control CreateCategoryCard(FallingObjectObjectCategory category)
	{
		string categoryName = category?.Name ?? "其它";
		int num = 0;
		int num2 = 0;
		if (_editingFallingObject?.weightItem != null)
		{
			foreach (FallingObjectWeightItemConfig item in _editingFallingObject.weightItem)
			{
				if (item != null)
				{
					string text = (item.empty ? "空项" : GetObjectCategory(item.item));
					if (!(categoryName != "全部") || !(text != categoryName))
					{
						num++;
						num2 += Math.Max(0, item.weight);
					}
				}
			}
		}
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
		{
			return new Label
			{
				Text = categoryName
			};
		}
		xWGameVisualChoiceCard.Name = "FallingObjectCategoryCard";
		xWGameVisualChoiceCard.CustomMinimumSize = new Vector2(142f, 64f);
		xWGameVisualChoiceCard.Configure(categoryName, categoryName, $"{num} 项 / 权重 {num2}", ResourceLoader.Load<Texture2D>(GetObjectCategoryIconPath(categoryName), null, ResourceLoader.CacheMode.Reuse), categoryName == _selectedCategory);
		xWGameVisualChoiceCard.TooltipText = category?.Description ?? string.Empty;
		xWGameVisualChoiceCard.Pressed += delegate
		{
			SetFallingObjectCategory(categoryName);
		};
		return xWGameVisualChoiceCard;
	}

	private static FallingObjectObjectCategory GetCategoryByName(string categoryName)
	{
		FallingObjectObjectCategory[] fallingObjectCategories = FallingObjectCategories;
		foreach (FallingObjectObjectCategory fallingObjectObjectCategory in fallingObjectCategories)
		{
			if (fallingObjectObjectCategory.Name == categoryName)
			{
				return fallingObjectObjectCategory;
			}
		}
		return FallingObjectCategories[0];
	}

	private ObjectManagerConfig.OBJECT ReadSelectedObject()
	{
		if (!GodotObject.IsInstanceValid(_objectPicker))
		{
			return ObjectManagerConfig.OBJECT.NOONE;
		}
		return (ObjectManagerConfig.OBJECT)_objectPicker.SelectedChoiceId;
	}

	private FallingObjectWeightItemConfig GetSelectedWeightItem()
	{
		Array<FallingObjectWeightItemConfig> array = _editingFallingObject?.weightItem;
		if (array == null || _selectedWeightIndex < 0 || _selectedWeightIndex >= array.Count)
		{
			return null;
		}
		return array[_selectedWeightIndex];
	}

	private void AddSummaryRows(FallingObjectConfig fallingObject)
	{
		AddItemIfMissing(base.PreviewList, BuildSummary(fallingObject));
		AddItemIfMissing(base.TimelineList, $"weightItem -> {fallingObject.weightItem?.Count ?? 0}");
		AddItemIfMissing(base.TimelineList, $"Pick -> totalWeight={GetTotalWeight(fallingObject)}");
		AddItemIfMissing(base.GraphList, "掉落物 -> weightItem -> item / weight / empty -> Pick");
		AddItemIfMissing(base.ReferenceList, "掉落物资源 -> " + base.CurrentResourcePath);
	}

	private static string BuildSummary(FallingObjectConfig fallingObject)
	{
		string value = (string.IsNullOrWhiteSpace(fallingObject?.ResourceName) ? "未命名掉落表" : fallingObject.ResourceName);
		return $"{value}: 条目 {(fallingObject?.weightItem?.Count).GetValueOrDefault()}, 总权重 {GetTotalWeight(fallingObject)}";
	}

	private static int GetTotalWeight(FallingObjectConfig fallingObject)
	{
		if (fallingObject?.weightItem == null)
		{
			return 0;
		}
		int num = 0;
		foreach (FallingObjectWeightItemConfig item in fallingObject.weightItem)
		{
			num += Math.Max(0, item?.weight ?? 0);
		}
		return num;
	}

	private static int CountEmpty(FallingObjectConfig fallingObject)
	{
		if (fallingObject?.weightItem == null)
		{
			return 0;
		}
		int num = 0;
		foreach (FallingObjectWeightItemConfig item in fallingObject.weightItem)
		{
			if (item != null && item.empty)
			{
				num++;
			}
		}
		return num;
	}

	private static string FormatWeightItem(FallingObjectWeightItemConfig item)
	{
		if (item == null)
		{
			return "空";
		}
		if (item.empty)
		{
			return $"[空项] empty weight={item.weight}";
		}
		return $"[{GetObjectCategory(item.item)}] {item.item} weight={item.weight}";
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
}
