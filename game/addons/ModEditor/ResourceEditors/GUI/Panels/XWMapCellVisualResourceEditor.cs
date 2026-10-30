using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.Inspector;

namespace PVZHE.ModEditor.ResourceEditors.GUI;

public partial class XWMapCellVisualResourceEditor : XWGenericVisualResourceEditor
{
	private readonly record struct GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE Type, string Label, Color Color);

	private enum CurvePreset
	{
		Flat,
		Rise,
		Fall,
		Ridge,
		Valley
	}

	private const string LayoutScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapCellVisualEditorLayout.tscn";

	private const string VisualChoiceCardScenePath = "res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn";

	private const int PreviewColumns = 9;

	private const int PreviewRows = 6;

	private static PackedScene _layoutScene;

	private static PackedScene _visualChoiceCardScene;

	private readonly System.Collections.Generic.Dictionary<TowerDefenseEnum.PLANTGRIDTYPE, Button> _gridTypeButtons = new System.Collections.Generic.Dictionary<TowerDefenseEnum.PLANTGRIDTYPE, Button>();

	private readonly System.Collections.Generic.Dictionary<int, Button> _elementButtons = new System.Collections.Generic.Dictionary<int, Button>();

	private readonly List<Button> _boardButtons = new List<Button>();

	private TowerDefenseCellConfig _editingCell;

	private XWVisualPropertyBinding _binding;

	private XWVisualPropertyBinding _metadataBinding;

	private VBoxContainer _layoutRoot;

	private TabContainer _pages;

	private LineEdit _resourceNameEdit;

	private CheckButton _localToSceneCheck;

	private SpinBox _startX;

	private SpinBox _startY;

	private SpinBox _endX;

	private SpinBox _endY;

	private GridContainer _boardGrid;

	private Label _boardHint;

	private Label _selectionSummary;

	private Label _coverageBadge;

	private Label _curveStatus;

	private Control _curveGraph;

	private XWResourcePicker _curvePicker;

	private Vector2I? _pendingGridAnchor;

	private bool _updatingControls;

	private bool _responsiveSignalsConnected;

	private static readonly GridTypeVisual[] GridTypeVisuals = new GridTypeVisual[14]
	{
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.NOONE, "⛔ 禁止", new Color(0.52f, 0.26f, 0.22f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.ALL, "✦ 全部", new Color(0.78f, 0.63f, 0.24f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.GROUND, "▰ 陆地", new Color(0.31f, 0.62f, 0.22f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.WATER, "≈ 水面", new Color(0.18f, 0.48f, 0.83f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.AIR, "△ 空中", new Color(0.55f, 0.37f, 0.8f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.SOIL, "▣ 泥土", new Color(0.45f, 0.3f, 0.14f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD, "◉ 睡莲", new Color(0.18f, 0.58f, 0.47f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.POT, "∪ 花盆", new Color(0.61f, 0.36f, 0.16f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.SURROUND, "◎ 环绕", new Color(0.62f, 0.55f, 0.23f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.GRAVESTONE, "▥ 墓碑", new Color(0.36f, 0.39f, 0.43f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.CRATER, "● 弹坑", new Color(0.32f, 0.2f, 0.12f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.BRICK, "▦ 砖块", new Color(0.64f, 0.3f, 0.22f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.ICECAP, "◇ 冰面", new Color(0.42f, 0.72f, 0.85f)),
		new GridTypeVisual(TowerDefenseEnum.PLANTGRIDTYPE.PLANT, "♣ 植物", new Color(0.26f, 0.67f, 0.3f))
	};

	private static readonly (int Flag, string Label, Color Color)[] ElementVisuals = new(int, string, Color)[4]
	{
		(1, "❄ 冰", new Color(0.37f, 0.72f, 0.96f)),
		(2, "♨ 火", new Color(0.94f, 0.38f, 0.18f)),
		(4, "☀ 日", new Color(0.94f, 0.72f, 0.2f)),
		(8, "☾ 夜", new Color(0.48f, 0.42f, 0.82f))
	};

	public TabContainer MapCellPages => _pages;

	public bool IsBoardSurfaceActive { get; private set; }

	public bool IsCurveSurfaceActive { get; private set; }

	public int GridVisualCardCount => _gridTypeButtons.Count;

	public int ElementVisualCardCount => _elementButtons.Count;

	public override void _ExitTree()
	{
		DisposeCellBinding();
		base._ExitTree();
	}

	protected override void OnVisualEditorVisibilityChanged(bool visible)
	{
		base.OnVisualEditorVisibilityChanged(visible);
		UpdateResponsiveSurfaceProcessing();
	}

	protected override void RenderCustomVisualPreset(XWVisualEditorPreset preset)
	{
		DisposeCellBinding();
		if (base.CurrentResource is TowerDefenseCellConfig editingCell && base.CanvasGrid != null)
		{
			_editingCell = editingCell;
			base.CanvasGrid.Columns = 1;
			if (_layoutScene == null)
			{
				_layoutScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWMapCellVisualEditorLayout.tscn", null, ResourceLoader.CacheMode.Reuse);
			}
			VBoxContainer vBoxContainer = _layoutScene?.Instantiate<VBoxContainer>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(vBoxContainer))
			{
				_layoutRoot = vBoxContainer;
				base.CanvasGrid.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
				BindWorkbench(vBoxContainer);
				PopulateControls();
				AddSummaryRows();
			}
		}
	}

	protected override bool ShouldUpdateEmbeddedInspector(Resource resource, string path, XWVisualEditorDescriptor descriptor)
	{
		if (!(resource is TowerDefenseCellConfig))
		{
			return base.ShouldUpdateEmbeddedInspector(resource, path, descriptor);
		}
		return false;
	}

	private void BindWorkbench(VBoxContainer root)
	{
		_pages = root.GetNode<TabContainer>("%MapCellPages");
		_resourceNameEdit = root.GetNode<LineEdit>("%ResourceNameEdit");
		_localToSceneCheck = root.GetNode<CheckButton>("%LocalToSceneCheck");
		_startX = root.GetNode<SpinBox>("%StartX");
		_startY = root.GetNode<SpinBox>("%StartY");
		_endX = root.GetNode<SpinBox>("%EndX");
		_endY = root.GetNode<SpinBox>("%EndY");
		_boardGrid = root.GetNode<GridContainer>("%BoardGrid");
		_boardHint = root.GetNode<Label>("%BoardHint");
		_selectionSummary = root.GetNode<Label>("%SelectionSummary");
		_coverageBadge = root.GetNode<Label>("%CoverageBadge");
		_curveStatus = root.GetNode<Label>("%CurveStatus");
		_curveGraph = root.GetNode<Control>("%CurveGraph");
		_curvePicker = root.GetNode<XWResourcePicker>("%GroundCurvePicker");
		_binding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCellPropertyEdited);
		BindResourceHeaderControls();
		BindPositionSpinner(_startX);
		BindPositionSpinner(_startY);
		BindPositionSpinner(_endX);
		BindPositionSpinner(_endY);
		BuildBoardButtons();
		BuildGridTypeButtons(root.GetNode<HFlowContainer>("%GridTypeButtons"));
		BuildElementButtons(root.GetNode<HFlowContainer>("%ElementButtons"));
		_curveGraph.Draw += DrawCurveGraph;
		_curvePicker.Setup("CurveTexture");
		_curvePicker.ResourceChanged += OnCurveResourceChanged;
		_curvePicker.ResourceSelected += OnCurveResourceSelected;
		root.GetNode<Button>("%FlatCurve").Pressed += delegate
		{
			ApplyCurvePreset(CurvePreset.Flat);
		};
		root.GetNode<Button>("%RiseCurve").Pressed += delegate
		{
			ApplyCurvePreset(CurvePreset.Rise);
		};
		root.GetNode<Button>("%FallCurve").Pressed += delegate
		{
			ApplyCurvePreset(CurvePreset.Fall);
		};
		root.GetNode<Button>("%RidgeCurve").Pressed += delegate
		{
			ApplyCurvePreset(CurvePreset.Ridge);
		};
		root.GetNode<Button>("%ValleyCurve").Pressed += delegate
		{
			ApplyCurvePreset(CurvePreset.Valley);
		};
		root.GetNode<Button>("%ClearCurve").Pressed += ClearCurve;
		root.GetNode<Button>("%SaveButton").Pressed += base.SaveCurrentResource;
		ConnectResponsiveSignals();
	}

	private void DisposeCellBinding()
	{
		DisconnectResponsiveSignals();
		_binding?.Dispose();
		_binding = null;
		_metadataBinding?.Dispose();
		_metadataBinding = null;
		_editingCell = null;
		_gridTypeButtons.Clear();
		_elementButtons.Clear();
		_boardButtons.Clear();
		_pendingGridAnchor = null;
		_layoutRoot = null;
		_pages = null;
		_resourceNameEdit = null;
		_localToSceneCheck = null;
		_boardGrid = null;
		_curveGraph = null;
	}

	private void BindResourceHeaderControls()
	{
		_metadataBinding?.Dispose();
		_metadataBinding = new XWVisualPropertyBinding(XWEditorInterface.Instance?.GetUndoRedoManager(), OnCellPropertyEdited);
		_metadataBinding.BindText(_resourceNameEdit, _editingCell, "resource_name", RefreshVisualState, this, "RefreshMapCellEditorFromHistory");
		_metadataBinding.BindToggle(_localToSceneCheck, _editingCell, "resource_local_to_scene", RefreshVisualState, this, "RefreshMapCellEditorFromHistory");
	}

	private void ConnectResponsiveSignals()
	{
		if (!_responsiveSignalsConnected && GodotObject.IsInstanceValid(_pages))
		{
			_pages.TabChanged += OnMapCellPageChanged;
			_responsiveSignalsConnected = true;
			UpdateResponsiveSurfaceProcessing();
		}
	}

	private void DisconnectResponsiveSignals()
	{
		if (_responsiveSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(_pages))
			{
				_pages.TabChanged -= OnMapCellPageChanged;
			}
			_responsiveSignalsConnected = false;
			SetResponsiveSurfaceProcessing(boardActive: false, curveActive: false);
		}
	}

	private void OnMapCellPageChanged(long _)
	{
		UpdateResponsiveSurfaceProcessing();
	}

	private void UpdateResponsiveSurfaceProcessing()
	{
		bool flag = IsVisibleInTree() && GodotObject.IsInstanceValid(_layoutRoot) && _layoutRoot.IsVisibleInTree() && GodotObject.IsInstanceValid(_pages);
		SetResponsiveSurfaceProcessing(flag && _pages.CurrentTab == 0, flag && _pages.CurrentTab == 2);
	}

	private void SetResponsiveSurfaceProcessing(bool boardActive, bool curveActive)
	{
		IsBoardSurfaceActive = boardActive;
		IsCurveSurfaceActive = curveActive;
		if (GodotObject.IsInstanceValid(_boardGrid))
		{
			_boardGrid.ProcessMode = (ProcessModeEnum)(boardActive ? 0 : 4);
		}
		if (GodotObject.IsInstanceValid(_curveGraph))
		{
			_curveGraph.ProcessMode = (ProcessModeEnum)(curveActive ? 0 : 4);
		}
	}

	public void SetMapCellPage(int page)
	{
		if (GodotObject.IsInstanceValid(_pages) && page >= 0 && page < _pages.GetTabCount())
		{
			_pages.CurrentTab = page;
			UpdateResponsiveSurfaceProcessing();
		}
	}

	private void BindPositionSpinner(SpinBox spinner)
	{
		spinner.FocusEntered += delegate
		{
			_binding?.BeginEdit(_editingCell, "pos");
		};
		spinner.ValueChanged += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCell))
			{
				_binding.PreviewValue(_editingCell, "pos", Variant.From<Vector4I>(ReadNormalizedPosition()));
				RefreshVisualState();
			}
		};
		spinner.FocusExited += delegate
		{
			if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCell))
			{
				_binding.CommitEdit(_editingCell, "pos", Variant.From<Vector4I>(ReadNormalizedPosition()), "调整地图格子区域", this, "RefreshMapCellEditorFromHistory");
				PopulatePositionControls();
			}
		};
	}

	private Vector4I ReadNormalizedPosition()
	{
		int val = Mathf.Max(1, Mathf.RoundToInt(_startX.Value));
		int val2 = Mathf.Max(1, Mathf.RoundToInt(_startY.Value));
		int val3 = Mathf.Max(1, Mathf.RoundToInt(_endX.Value));
		int val4 = Mathf.Max(1, Mathf.RoundToInt(_endY.Value));
		return new Vector4I(Math.Min(val, val3), Math.Min(val2, val4), Math.Max(val, val3), Math.Max(val2, val4));
	}

	private void BuildBoardButtons()
	{
		for (int i = 0; i < 6; i++)
		{
			for (int j = 0; j < 9; j++)
			{
				Vector2I cell = new Vector2I(j + 1, i + 1);
				Button button = new Button
				{
					Name = $"MapCellGridButton_{j}_{i}",
					Text = $"{j + 1}\n{i + 1}",
					CustomMinimumSize = new Vector2(48f, 48f),
					SizeFlagsHorizontal = SizeFlags.ExpandFill,
					SizeFlagsVertical = SizeFlags.ExpandFill,
					TooltipText = $"地图格子 ({j + 1}, {i + 1})"
				};
				button.Pressed += delegate
				{
					OnBoardCellPressed(cell);
				};
				_boardGrid.AddChild(button, forceReadableName: false, InternalMode.Disabled);
				_boardButtons.Add(button);
			}
		}
	}

	private void OnBoardCellPressed(Vector2I cell)
	{
		if (_binding != null && GodotObject.IsInstanceValid(_editingCell))
		{
			if (!_pendingGridAnchor.HasValue)
			{
				_pendingGridAnchor = cell;
				_boardHint.Text = $"起点 ({cell.X}, {cell.Y}) 已锁定；再点一个格子完成区域";
				UpdateBoardPreview();
			}
			else
			{
				Vector2I value = _pendingGridAnchor.Value;
				_pendingGridAnchor = null;
				Vector4I from = new Vector4I(Math.Min(value.X, cell.X), Math.Min(value.Y, cell.Y), Math.Max(value.X, cell.X), Math.Max(value.Y, cell.Y));
				_binding.SetValue(_editingCell, "pos", Variant.From(in from), "框选地图格子区域", this, "RefreshMapCellEditorFromHistory");
				_boardHint.Text = "区域已应用；继续连点两个格子可重新框选";
				PopulatePositionControls();
				RefreshVisualState();
			}
		}
	}

	private void BuildGridTypeButtons(HFlowContainer container)
	{
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		Texture2D icon = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ResourceMap.svg", null, ResourceLoader.CacheMode.Reuse);
		GridTypeVisual[] gridTypeVisuals = GridTypeVisuals;
		for (int i = 0; i < gridTypeVisuals.Length; i++)
		{
			GridTypeVisual visual = gridTypeVisuals[i];
			XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Name = $"GridType_{visual.Type}";
				xWGameVisualChoiceCard.CustomMinimumSize = new Vector2(150f, 68f);
				xWGameVisualChoiceCard.Configure(visual.Type.ToString(), visual.Label, "放置层图块", icon, selected: false);
				xWGameVisualChoiceCard.TooltipText = $"切换 {visual.Type} 放置层";
				xWGameVisualChoiceCard.AddThemeColorOverride("font_pressed_color", Colors.White);
				xWGameVisualChoiceCard.Modulate = visual.Color.Lightened(0.18f);
				xWGameVisualChoiceCard.Toggled += delegate(bool pressed)
				{
					OnGridTypeToggled(visual.Type, pressed);
				};
				container.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
				_gridTypeButtons[visual.Type] = xWGameVisualChoiceCard;
			}
		}
	}

	private void OnGridTypeToggled(TowerDefenseEnum.PLANTGRIDTYPE type, bool pressed)
	{
		if (_updatingControls || _binding == null || !GodotObject.IsInstanceValid(_editingCell))
		{
			return;
		}
		Array<TowerDefenseEnum.PLANTGRIDTYPE> from = ((_editingCell.gridType == null) ? new Array<TowerDefenseEnum.PLANTGRIDTYPE>() : new Array<TowerDefenseEnum.PLANTGRIDTYPE>(_editingCell.gridType));
		if ((uint)(type - -1) <= 1u)
		{
			from.Clear();
			if (pressed)
			{
				from.Add(type);
			}
		}
		else
		{
			from.Remove(TowerDefenseEnum.PLANTGRIDTYPE.ALL);
			from.Remove(TowerDefenseEnum.PLANTGRIDTYPE.NOONE);
			if (pressed && !from.Contains(type))
			{
				from.Add(type);
			}
			else if (!pressed)
			{
				from.Remove(type);
			}
		}
		if (from.Count == 0)
		{
			from.Add(TowerDefenseEnum.PLANTGRIDTYPE.NOONE);
		}
		_binding.SetValue(_editingCell, "gridType", Variant.From(in from), "切换地图格子放置层", this, "RefreshMapCellEditorFromHistory");
		PopulateGridTypeButtons();
		RefreshVisualState();
	}

	private void BuildElementButtons(HFlowContainer container)
	{
		if (_visualChoiceCardScene == null)
		{
			_visualChoiceCardScene = ResourceLoader.Load<PackedScene>("res://addons/ModEditor/ResourceEditors/GUI/Panels/XWGameVisualChoiceCard.tscn", null, ResourceLoader.CacheMode.Reuse);
		}
		(int, string, Color)[] elementVisuals = ElementVisuals;
		for (int i = 0; i < elementVisuals.Length; i++)
		{
			(int Flag, string Label, Color Color) visual = elementVisuals[i];
			XWGameVisualChoiceCard xWGameVisualChoiceCard = _visualChoiceCardScene?.Instantiate<XWGameVisualChoiceCard>(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(xWGameVisualChoiceCard))
			{
				xWGameVisualChoiceCard.Name = $"ElementFlag_{visual.Flag}";
				xWGameVisualChoiceCard.CustomMinimumSize = new Vector2(150f, 68f);
				xWGameVisualChoiceCard.Configure(visual.Flag.ToString(), visual.Label, "环境元素", LoadElementIcon(visual.Flag), selected: false);
				xWGameVisualChoiceCard.TooltipText = "该格子携带的游戏元素环境标记";
				xWGameVisualChoiceCard.Modulate = visual.Color.Lightened(0.15f);
				xWGameVisualChoiceCard.Toggled += delegate(bool pressed)
				{
					OnElementToggled(visual.Flag, pressed);
				};
				container.AddChild(xWGameVisualChoiceCard, forceReadableName: false, InternalMode.Disabled);
				_elementButtons[visual.Flag] = xWGameVisualChoiceCard;
			}
		}
	}

	private static Texture2D LoadElementIcon(int flag)
	{
		string path = flag switch
		{
			1 => "res://addons/ModEditor/Icons/ClassIcon/ColorPick.svg", 
			2 => "res://addons/ModEditor/Icons/StatusError.svg", 
			4 => "res://addons/ModEditor/Icons/ClassIcon/Environment.svg", 
			8 => "res://addons/ModEditor/Icons/ClassIcon/Environment.svg", 
			_ => "res://addons/ModEditor/Icons/Signals.svg", 
		};
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	private void OnElementToggled(int flag, bool pressed)
	{
		if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCell))
		{
			int num = (pressed ? (_editingCell.ElementFlags | flag) : (_editingCell.ElementFlags & ~flag));
			_binding.SetValue(_editingCell, "ElementFlags", num, "切换地图格子元素标记", this, "RefreshMapCellEditorFromHistory");
			PopulateElementButtons();
		}
	}

	private void ApplyCurvePreset(CurvePreset preset)
	{
		if (_binding != null && GodotObject.IsInstanceValid(_editingCell))
		{
			Curve curve = new Curve
			{
				MinValue = 0f,
				MaxValue = 1f
			};
			switch (preset)
			{
			case CurvePreset.Flat:
				curve.AddPoint(new Vector2(0f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(1f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				break;
			case CurvePreset.Rise:
				curve.AddPoint(new Vector2(0f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(1f, 1f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				break;
			case CurvePreset.Fall:
				curve.AddPoint(new Vector2(0f, 1f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(1f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				break;
			case CurvePreset.Ridge:
				curve.AddPoint(new Vector2(0f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(0.5f, 1f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(1f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				break;
			case CurvePreset.Valley:
				curve.AddPoint(new Vector2(0f, 1f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(0.5f, 0f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				curve.AddPoint(new Vector2(1f, 1f), 0f, 0f, Curve.TangentMode.Free, Curve.TangentMode.Free);
				break;
			}
			CurveTexture from = new CurveTexture
			{
				Curve = curve,
				ResourceLocalToScene = true
			};
			_binding.SetValue(_editingCell, "groundHeightCurve", Variant.From(in from), "应用" + GetCurvePresetName(preset) + "高度曲线", this, "RefreshMapCellEditorFromHistory");
			PopulateCurveControls();
		}
	}

	private void ClearCurve()
	{
		_binding?.SetValue(_editingCell, "groundHeightCurve", default(Variant), "清除地图格子高度曲线", this, "RefreshMapCellEditorFromHistory");
		PopulateCurveControls();
	}

	private void OnCurveResourceChanged(Resource resource)
	{
		if (!_updatingControls && _binding != null && GodotObject.IsInstanceValid(_editingCell) && (!GodotObject.IsInstanceValid(resource) || resource is CurveTexture))
		{
			_binding.SetValue(_editingCell, "groundHeightCurve", Variant.From(in resource), "选择地图格子高度曲线", this, "RefreshMapCellEditorFromHistory");
			PopulateCurveControls();
		}
	}

	private static void OnCurveResourceSelected(Resource resource)
	{
		if (GodotObject.IsInstanceValid(resource))
		{
			XWEditorInterface.Instance?.EditResource(resource);
		}
	}

	private void PopulateControls()
	{
		_updatingControls = true;
		try
		{
			PopulatePositionControls();
			PopulateGridTypeButtons();
			PopulateElementButtons();
			PopulateCurveControls();
		}
		finally
		{
			_updatingControls = false;
		}
		RefreshVisualState();
	}

	private void PopulatePositionControls()
	{
		if (GodotObject.IsInstanceValid(_editingCell) && GodotObject.IsInstanceValid(_startX))
		{
			bool updatingControls = _updatingControls;
			_updatingControls = true;
			Vector4I vector4I = NormalizePosition(_editingCell.pos);
			_startX.SetValueNoSignal(vector4I.X);
			_startY.SetValueNoSignal(vector4I.Y);
			_endX.SetValueNoSignal(vector4I.Z);
			_endY.SetValueNoSignal(vector4I.W);
			_updatingControls = updatingControls;
		}
	}

	private void PopulateGridTypeButtons()
	{
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		foreach (KeyValuePair<TowerDefenseEnum.PLANTGRIDTYPE, Button> gridTypeButton in _gridTypeButtons)
		{
			Button value = gridTypeButton.Value;
			TowerDefenseCellConfig editingCell = _editingCell;
			value.SetPressedNoSignal(editingCell != null && editingCell.gridType?.Contains(gridTypeButton.Key) == true);
		}
		_updatingControls = updatingControls;
	}

	private void PopulateElementButtons()
	{
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		foreach (KeyValuePair<int, Button> elementButton in _elementButtons)
		{
			elementButton.Value.SetPressedNoSignal((_editingCell.ElementFlags & elementButton.Key) != 0);
		}
		_updatingControls = updatingControls;
	}

	private void PopulateCurveControls()
	{
		bool updatingControls = _updatingControls;
		_updatingControls = true;
		_curvePicker?.SetEditedResource(_editingCell?.groundHeightCurve);
		if (GodotObject.IsInstanceValid(_curveStatus))
		{
			if (GodotObject.IsInstanceValid(_editingCell?.groundHeightCurve?.Curve))
			{
				Curve curve = _editingCell.groundHeightCurve.Curve;
				_curveStatus.Text = $"曲线已绑定 · {curve.PointCount} 个控制点 · 高度 {curve.MinValue:0.##}～{curve.MaxValue:0.##}";
			}
			else
			{
				_curveStatus.Text = "未配置高度曲线：整块地面高度为 0";
			}
		}
		_curveGraph?.QueueRedraw();
		_updatingControls = updatingControls;
	}

	private void RefreshVisualState()
	{
		if (GodotObject.IsInstanceValid(_editingCell))
		{
			UpdateBoardPreview();
			Vector4I vector4I = NormalizePosition(_editingCell.pos);
			int num = vector4I.Z - vector4I.X + 1;
			int num2 = vector4I.W - vector4I.Y + 1;
			int value = num * num2;
			if (GodotObject.IsInstanceValid(_selectionSummary))
			{
				_selectionSummary.Text = $"生效区域：({vector4I.X}, {vector4I.Y}) → ({vector4I.Z}, {vector4I.W})，共 {value} 格";
			}
			if (GodotObject.IsInstanceValid(_coverageBadge))
			{
				_coverageBadge.Text = $"区域 {num} × {num2}";
			}
			_curveGraph?.QueueRedraw();
		}
	}

	private void UpdateBoardPreview()
	{
		if (GodotObject.IsInstanceValid(_editingCell))
		{
			Vector4I vector4I = NormalizePosition(_editingCell.pos);
			Color cellColor = GetCellColor(_editingCell);
			for (int i = 0; i < _boardButtons.Count; i++)
			{
				int num = i % 9 + 1;
				int num2 = i / 9 + 1;
				bool flag = num >= vector4I.X && num <= vector4I.Z && num2 >= vector4I.Y && num2 <= vector4I.W;
				bool flag2 = _pendingGridAnchor == new Vector2I(num, num2);
				Button button = _boardButtons[i];
				button.Modulate = (flag2 ? new Color(1f, 0.74f, 0.24f) : (flag ? cellColor : new Color(0.36f, 0.32f, 0.2f, 0.72f)));
				button.AddThemeColorOverride("font_color", (flag || flag2) ? Colors.White : new Color(0.72f, 0.69f, 0.56f));
			}
		}
	}

	private void DrawCurveGraph()
	{
		if (GodotObject.IsInstanceValid(_curveGraph))
		{
			Vector2 size = _curveGraph.Size;
			Rect2 rect = new Rect2(new Vector2(14f, 12f), new Vector2(Math.Max(10f, size.X - 28f), Math.Max(10f, size.Y - 24f)));
			for (int i = 0; i <= 4; i++)
			{
				float num = (float)i / 4f;
				_curveGraph.DrawLine(new Vector2(rect.Position.X, rect.Position.Y + rect.Size.Y * num), new Vector2(rect.End.X, rect.Position.Y + rect.Size.Y * num), new Color(0.24f, 0.3f, 0.2f, 0.65f), 1f);
			}
			Curve curve = _editingCell?.groundHeightCurve?.Curve;
			Vector2[] array = new Vector2[65];
			float num2 = (GodotObject.IsInstanceValid(curve) ? curve.MinValue : 0f);
			float num3 = (GodotObject.IsInstanceValid(curve) ? curve.MaxValue : 1f);
			float num4 = Math.Max(0.0001f, num3 - num2);
			for (int j = 0; j < array.Length; j++)
			{
				float num5 = (float)j / (float)(array.Length - 1);
				float num6 = Mathf.Clamp(((GodotObject.IsInstanceValid(curve) ? curve.Sample(num5) : 0f) - num2) / num4, 0f, 1f);
				array[j] = new Vector2(rect.Position.X + rect.Size.X * num5, rect.End.Y - rect.Size.Y * num6);
			}
			_curveGraph.DrawPolyline(array, new Color(0.54f, 0.9f, 0.3f), 3f, antialiased: true);
		}
	}

	private void OnCellPropertyEdited(bool committed)
	{
		if (GodotObject.IsInstanceValid(_editingCell))
		{
			if (committed)
			{
				NotifyCurrentResourceEdited();
			}
			else
			{
				MarkCurrentResourceDirty();
				_editingCell.EmitChanged();
			}
			RefreshVisualState();
		}
	}

	public void RefreshMapCellEditorFromHistory()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (xWUndoRedoManager != null && (xWUndoRedoManager.IsUndoing() || xWUndoRedoManager.IsRedoing()))
		{
			_pendingGridAnchor = null;
			BindResourceHeaderControls();
			PopulateControls();
		}
	}

	private void AddSummaryRows()
	{
		Vector4I vector4I = NormalizePosition(_editingCell.pos);
		base.PreviewList?.AddItem($"地图格子区域 · ({vector4I.X}, {vector4I.Y}) → ({vector4I.Z}, {vector4I.W})");
		base.PreviewList?.AddItem("放置层 · " + BuildGridTypeSummary());
		base.GraphList?.AddItem("格子画笔 → 地图区域 → 放置层 / 元素环境 / 地面高度");
		string text = ((!GodotObject.IsInstanceValid(_editingCell.groundHeightCurve)) ? "未配置" : (string.IsNullOrWhiteSpace(_editingCell.groundHeightCurve.ResourcePath) ? "内置 CurveTexture" : _editingCell.groundHeightCurve.ResourcePath));
		base.ReferenceList?.AddItem("高度曲线 → " + text);
	}

	private string BuildGridTypeSummary()
	{
		if (_editingCell.gridType == null || _editingCell.gridType.Count == 0)
		{
			return "未配置";
		}
		List<string> list = new List<string>();
		foreach (TowerDefenseEnum.PLANTGRIDTYPE item in _editingCell.gridType)
		{
			list.Add(item.ToString());
		}
		return string.Join(" + ", list);
	}

	private static Vector4I NormalizePosition(Vector4I pos)
	{
		int num = Math.Max(1, Math.Min(pos.X, pos.Z));
		int num2 = Math.Max(1, Math.Min(pos.Y, pos.W));
		int z = Math.Max(num, Math.Max(pos.X, pos.Z));
		int w = Math.Max(num2, Math.Max(pos.Y, pos.W));
		return new Vector4I(num, num2, z, w);
	}

	private static Color GetCellColor(TowerDefenseCellConfig cell)
	{
		if (cell != null && cell.gridType?.Contains(TowerDefenseEnum.PLANTGRIDTYPE.WATER) == true)
		{
			return new Color(0.18f, 0.53f, 0.92f);
		}
		if (cell != null && cell.gridType?.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) == true && cell.gridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR))
		{
			return new Color(0.26f, 0.69f, 0.34f);
		}
		if (cell != null && cell.gridType?.Contains(TowerDefenseEnum.PLANTGRIDTYPE.GROUND) == true)
		{
			return new Color(0.36f, 0.72f, 0.25f);
		}
		if (cell != null && cell.gridType?.Contains(TowerDefenseEnum.PLANTGRIDTYPE.AIR) == true)
		{
			return new Color(0.61f, 0.44f, 0.9f);
		}
		return new Color(0.65f, 0.55f, 0.24f);
	}

	private static string GetCurvePresetName(CurvePreset preset)
	{
		return preset switch
		{
			CurvePreset.Flat => "平地", 
			CurvePreset.Rise => "上坡", 
			CurvePreset.Fall => "下坡", 
			CurvePreset.Ridge => "屋脊", 
			CurvePreset.Valley => "凹地", 
			_ => "地面", 
		};
	}
}
