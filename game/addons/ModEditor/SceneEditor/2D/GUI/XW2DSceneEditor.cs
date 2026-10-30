using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ResourceEditors.GUI;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DSceneEditor : PanelContainer
{
	private enum SnapMenuId
	{
		SmartSnap = 1,
		GridSnap,
		ShowGrid,
		ShowGridWhenSnapping,
		HideGrid,
		OpenVisualSettings
	}

	private enum ViewMenuId
	{
		CenterSelection = 1,
		FrameSelection = 2,
		ZoomIn = 3,
		ZoomOut = 4,
		ZoomReset = 5,
		ShowOrigin = 10,
		ShowViewport = 11,
		ShowRulers = 12,
		ShowGuides = 13,
		ShowHelpers = 14
	}

	private enum SkeletonMenuId
	{
		MakeBones = 1,
		ClearBones
	}

	private enum ViewportContextMenuId
	{
		AddNode = 1,
		InstantiateScene = 2,
		SaveScene = 3,
		Rename = 10,
		Duplicate = 11,
		MoveUp = 12,
		MoveDown = 13,
		Delete = 14,
		LockSelection = 15,
		UnlockSelection = 16,
		GroupSelection = 17,
		UngroupSelection = 18,
		CopyNodePath = 19,
		MakeRoot = 20,
		SaveBranchAsScene = 21,
		CreateScript = 30,
		DetachScript = 31,
		ExtendScript = 32,
		CenterSelection = 40,
		FrameSelection = 41,
		ZoomIn = 42,
		ZoomOut = 43,
		ZoomReset = 44,
		ShowOrigin = 50,
		ShowViewport = 51,
		ShowRulers = 52,
		ShowGuides = 53,
		ShowHelpers = 54
	}

	private class SceneTabInfo
	{
		public string Key = "";

		public string DisplayName = "";

		public string SourcePath = "";

		public PackedScene PackedScene;

		public Node SceneInstance;

		public bool ScriptlessPreview;

		public int HistoryId = 1;

		public Dictionary ViewportState = new Dictionary();

		public Dictionary AnimationTimelineState = new Dictionary();
	}

	private readonly struct DirectTransformState(Vector2 position, Vector2 scale, float rotation, Vector2 size, Vector2 pivot, bool isControl)
	{
		public readonly Vector2 Position = position;

		public readonly Vector2 Scale = scale;

		public readonly float Rotation = rotation;

		public readonly Vector2 Size = size;

		public readonly Vector2 Pivot = pivot;

		public readonly bool IsControl = isControl;
	}

	private readonly struct ControlLayoutState(Control control)
	{
		public readonly float AnchorLeft = control.AnchorLeft;

		public readonly float AnchorTop = control.AnchorTop;

		public readonly float AnchorRight = control.AnchorRight;

		public readonly float AnchorBottom = control.AnchorBottom;

		public readonly float OffsetLeft = control.OffsetLeft;

		public readonly float OffsetTop = control.OffsetTop;

		public readonly float OffsetRight = control.OffsetRight;

		public readonly float OffsetBottom = control.OffsetBottom;

		public readonly GrowDirection GrowHorizontal = control.GrowHorizontal;

		public readonly GrowDirection GrowVertical = control.GrowVertical;
	}

	public enum SelectionLayoutOperation
	{
		AlignLeft,
		AlignHorizontalCenter,
		AlignRight,
		AlignTop,
		AlignVerticalCenter,
		AlignBottom,
		DistributeHorizontal,
		DistributeVertical
	}

	private readonly struct SelectionLayoutEntry(Node node, DirectTransformState state, Rect2 worldBounds)
	{
		public readonly Node Node = node;

		public readonly DirectTransformState State = state;

		public readonly Rect2 WorldBounds = worldBounds;
	}

	private readonly struct SpriteAtlasState
	{
		public readonly int Columns;

		public readonly int Rows;

		public readonly int Frame;

		public readonly bool RegionEnabled;

		public readonly Rect2 RegionRect;

		public SpriteAtlasState(Sprite2D sprite)
		{
			Columns = Math.Max(1, sprite.Hframes);
			Rows = Math.Max(1, sprite.Vframes);
			Frame = Math.Clamp(sprite.Frame, 0, Columns * Rows - 1);
			RegionEnabled = sprite.RegionEnabled;
			RegionRect = sprite.RegionRect;
		}

		public SpriteAtlasState(int columns, int rows, int frame, bool regionEnabled, Rect2 regionRect)
		{
			Columns = Math.Clamp(columns, 1, 256);
			Rows = Math.Clamp(rows, 1, 256);
			Frame = Math.Clamp(frame, 0, Columns * Rows - 1);
			RegionEnabled = regionEnabled;
			RegionRect = regionRect;
		}
	}

	private Button _animationTimelineButton;

	private VSplitContainer _canvasTimelineSplit;

	private XW2DAnimationTimelinePanel _sceneAnimationTimelinePanel;

	private bool _sceneAnimationSelectionConnected;

	private const string EditLockMeta = "_edit_lock_";

	private const string EditGroupMeta = "_edit_group_";

	private const string GeneratedSkeletonMeta = "_xw_generated_skeleton_2d";

	private const string BoneSourcePathMeta = "_xw_bone_source_path";

	private const string RuntimeCharacterComponentManagerName = "ComponentManager";

	private TabBar _sceneTabBar;

	private XW2DViewport _viewport;

	private Label _mousePositionLabel;

	private Label _zoomLabel;

	private Button _selectButton;

	private Button _moveButton;

	private Button _rotateButton;

	private Button _scaleButton;

	private Button _listSelectButton;

	private Button _pivotButton;

	private Button _panButton;

	private Button _rulerButton;

	private Button _pathButton;

	private Button _polygonButton;

	private Button _localSpaceButton;

	private Button _smartSnapButton;

	private Button _gridSnapButton;

	private Button _lockButton;

	private Button _unlockButton;

	private Button _groupButton;

	private Button _ungroupButton;

	private Button _centerViewButton;

	private Button _zoomOutButton;

	private Button _zoomResetButton;

	private Button _zoomInButton;

	private MenuButton _snapConfigMenu;

	private MenuButton _skeletonMenu;

	private MenuButton _viewMenu;

	private PopupMenu _viewportContextMenu;

	private TabContainer _workspaceTabs;

	private Button _allPropertiesButton;

	private XWDirectPropertySurface _nodeDirectPropertySurface;

	private PanelContainer _transformDirectPanel;

	private Label _transformSelectionLabel;

	private HBoxContainer _controlFields;

	private SpinBox _positionXSpin;

	private SpinBox _positionYSpin;

	private SpinBox _rotationSpin;

	private SpinBox _scaleXSpin;

	private SpinBox _scaleYSpin;

	private SpinBox _sizeXSpin;

	private SpinBox _sizeYSpin;

	private SpinBox _pivotXSpin;

	private SpinBox _pivotYSpin;

	private readonly System.Collections.Generic.Dictionary<string, SceneTabInfo> _openTabs = new System.Collections.Generic.Dictionary<string, SceneTabInfo>();

	private readonly List<string> _tabKeys = new List<string>();

	private string _currentTabKey = "";

	private bool _isSwitchingTab;

	private Vector2 _contextMenuWorldPosition;

	private Node _directTransformNode;

	private bool _directTransformSessionActive;

	private bool _updatingDirectTransformSurface;

	private Dictionary _sceneRootHistoryViewportState = new Dictionary();

	private XWUndoRedoManager _sceneHistoryManager;

	private ConfirmationDialog _unsavedSceneCloseDialog;

	private Button _discardUnsavedSceneButton;

	private string _pendingCloseTabKey = "";

	private const string GridSettingPrefix = "2d_editor_grid/";

	private PopupPanel _gridSettingsPopup;

	private XW2DGridSettingsPanel _gridSettingsPanel;

	private Timer _gridSettingsSaveTimer;

	private static readonly StringName InlineTextPropertyFallback = new StringName("text");

	private PanelContainer _inlineTextOverlay;

	private Label _inlineTextModeLabel;

	private LineEdit _inlineTextLineEdit;

	private TextEdit _inlineTextMultilineEdit;

	private Button _inlineTextCommitButton;

	private Button _inlineTextCancelButton;

	private XWUiMotion _inlineTextOverlayMotion;

	private Node _inlineTextTarget;

	private StringName _inlineTextProperty = InlineTextPropertyFallback;

	private Variant.Type _inlineTextValueType = Variant.Type.String;

	private PropertyHint _inlineTextHint;

	private string _inlineTextBefore = "";

	private int _inlineTextHistoryId = 1;

	private int _inlineTextSessionToken;

	private bool _inlineTextMultiline;

	private bool _inlineTextApplyingPreview;

	private bool _inlineTextGeometryRefreshQueued;

	private readonly System.Collections.Generic.Dictionary<ulong, bool> _inlineTextCapabilityCache = new System.Collections.Generic.Dictionary<ulong, bool>();

	private readonly List<Node> _directTransformNodes = new List<Node>();

	private readonly System.Collections.Generic.Dictionary<Node, DirectTransformState> _directTransformStarts = new System.Collections.Generic.Dictionary<Node, DirectTransformState>();

	private readonly HashSet<SpinBox> _directTransformTouchedSpins = new HashSet<SpinBox>();

	private readonly System.Collections.Generic.Dictionary<SpinBox, string> _directSpinBaseSuffixes = new System.Collections.Generic.Dictionary<SpinBox, string>();

	private readonly HashSet<SpinBox> _directMixedSpins = new HashSet<SpinBox>();

	private PanelContainer _controlLayoutPanel;

	private SpinBox _anchorLeftSpin;

	private SpinBox _anchorTopSpin;

	private SpinBox _anchorRightSpin;

	private SpinBox _anchorBottomSpin;

	private SpinBox _offsetLeftSpin;

	private SpinBox _offsetTopSpin;

	private SpinBox _offsetRightSpin;

	private SpinBox _offsetBottomSpin;

	private OptionButton _growHorizontalOption;

	private OptionButton _growVerticalOption;

	private Button[] _growHorizontalButtons;

	private Button[] _growVerticalButtons;

	private Button _presetTopLeftButton;

	private Button _presetCenterButton;

	private Button _presetFullRectButton;

	private Button _presetHCenterWideButton;

	private Button _presetVCenterWideButton;

	private Control _layoutControl;

	private ControlLayoutState _layoutStart;

	private bool _layoutSessionActive;

	private bool _updatingLayoutSurface;

	private readonly List<Node> _selectionLayoutNodes = new List<Node>();

	private PanelContainer _selectionLayoutPanel;

	private Label _selectionLayoutLabel;

	private Button[] _selectionAlignButtons;

	private Button[] _selectionDistributeButtons;

	private XW2DSpriteAtlasPanel _spriteAtlasPanel;

	private Button _spriteAtlasButton;

	private int _spriteAtlasTabIndex = -1;

	private bool _spriteAtlasSelectionConnected;

	public PackedScene CurrentPackedScene { get; private set; }

	public Node CurrentSceneInstance { get; private set; }

	public bool InlineTextEditorVisible
	{
		get
		{
			if (GodotObject.IsInstanceValid(_inlineTextOverlay))
			{
				return _inlineTextOverlay.Visible;
			}
			return false;
		}
	}

	public Node InlineTextTarget
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_inlineTextTarget))
			{
				return null;
			}
			return _inlineTextTarget;
		}
	}

	public StringName InlineTextProperty => _inlineTextProperty;

	public Variant.Type InlineTextValueType => _inlineTextValueType;

	public PropertyHint InlineTextHint => _inlineTextHint;

	public string InlineTextDraft
	{
		get
		{
			object obj;
			if (_inlineTextMultiline)
			{
				obj = _inlineTextMultilineEdit?.Text ?? "";
			}
			else
			{
				obj = _inlineTextLineEdit?.Text;
				if (obj == null)
				{
					return "";
				}
			}
			return (string)obj;
		}
	}

	public int InlineTextCommitCount { get; private set; }

	public int InlineTextCancelCount { get; private set; }

	public int InlineTextPreviewCount { get; private set; }

	public Rect2 InlineTextOverlayRect
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_inlineTextOverlay))
			{
				return default(Rect2);
			}
			return new Rect2(_inlineTextOverlay.Position, _inlineTextOverlay.Size);
		}
	}

	public Rect2 InlineTextInputRect
	{
		get
		{
			Control control = (_inlineTextMultiline ? ((Control)_inlineTextMultilineEdit) : ((Control)_inlineTextLineEdit));
			return GetInlineTextControlRectInViewport(control);
		}
	}

	public XW2DSpriteAtlasPanel SpriteAtlasPanel => _spriteAtlasPanel;

	private void InitializeSceneAnimationSurface()
	{
		_animationTimelineButton = GetNodeOrNull<Button>("%AnimationTimelineButton") ?? (FindChild("AnimationTimelineButton", recursive: true, owned: false) as Button);
		_canvasTimelineSplit = GetNodeOrNull<VSplitContainer>("%CanvasTimelineSplit") ?? (FindChild("CanvasTimelineSplit", recursive: true, owned: false) as VSplitContainer);
		_sceneAnimationTimelinePanel = GetNodeOrNull<XW2DAnimationTimelinePanel>("%SceneAnimationTimelinePanel") ?? (FindChild("SceneAnimationTimelinePanel", recursive: true, owned: false) as XW2DAnimationTimelinePanel);
		if (!GodotObject.IsInstanceValid(_animationTimelineButton) || !GodotObject.IsInstanceValid(_canvasTimelineSplit) || !GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			GD.PushError("2D 动画时间轴界面缺少按钮、分割面板或动画工作台节点。");
			return;
		}
		_sceneAnimationTimelinePanel.BindEditor(this);
		_animationTimelineButton.Toggled += SetSceneAnimationTimelineVisible;
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection != null)
		{
			xWEditorSelection.SelectionChanged += OnSceneAnimationSelectionChanged;
			_sceneAnimationSelectionConnected = true;
		}
		SetSceneAnimationTimelineVisible(visible: false);
	}

	private void ShutdownSceneAnimationSurface()
	{
		if (_sceneAnimationSelectionConnected)
		{
			XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
			if (xWEditorSelection != null)
			{
				xWEditorSelection.SelectionChanged -= OnSceneAnimationSelectionChanged;
			}
			_sceneAnimationSelectionConnected = false;
		}
		_sceneAnimationTimelinePanel?.ClearSurface();
	}

	private void SetSceneAnimationTimelineVisible(bool visible)
	{
		if (!GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			return;
		}
		_animationTimelineButton?.SetPressedNoSignal(visible);
		_sceneAnimationTimelinePanel.Visible = visible;
		if (GodotObject.IsInstanceValid(_canvasTimelineSplit))
		{
			_canvasTimelineSplit.Collapsed = !visible;
			if (visible)
			{
				int num = Mathf.Clamp(Mathf.RoundToInt(base.Size.Y * 0.42f), 260, 520);
				_canvasTimelineSplit.SplitOffsets = new int[1] { -num };
			}
		}
		if (visible)
		{
			_sceneAnimationTimelinePanel.RefreshFromSceneHistory();
			_sceneAnimationTimelinePanel.RefreshSelection();
		}
	}

	private void OnSceneAnimationSelectionChanged()
	{
		if (!GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			return;
		}
		_sceneAnimationTimelinePanel.RefreshSelection();
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			if (item is AnimationPlayer || item is AnimationTree)
			{
				SetSceneAnimationTimelineVisible(visible: true);
				break;
			}
		}
	}

	private void SaveSceneAnimationTabState(SceneTabInfo tabInfo)
	{
		if (tabInfo != null && GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			tabInfo.AnimationTimelineState = _sceneAnimationTimelinePanel.CaptureState();
			tabInfo.AnimationTimelineState["expanded"] = GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel) && _sceneAnimationTimelinePanel.Visible;
		}
	}

	private void LoadSceneAnimationTab(SceneTabInfo tabInfo)
	{
		if (GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			_sceneAnimationTimelinePanel.BindScene(tabInfo?.SceneInstance, tabInfo?.AnimationTimelineState);
			bool sceneAnimationTimelineVisible = tabInfo != null && tabInfo.AnimationTimelineState != null && tabInfo.AnimationTimelineState.GetValueOrDefault("expanded", false).AsBool();
			SetSceneAnimationTimelineVisible(sceneAnimationTimelineVisible);
		}
	}

	private void RebindSceneAnimationRoot(Node sceneRoot)
	{
		if (GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			_sceneAnimationTimelinePanel.BindScene(sceneRoot, null);
		}
	}

	private bool SuspendSceneAnimationPreviewForSave()
	{
		if (!GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			return false;
		}
		_sceneAnimationTimelinePanel.SuspendForSave();
		return true;
	}

	private void ResumeSceneAnimationPreviewAfterSave(bool resume)
	{
		if (resume && GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel))
		{
			_sceneAnimationTimelinePanel.ResumeAfterSave();
		}
	}

	private void RefreshSceneAnimationAfterHistory()
	{
		_sceneAnimationTimelinePanel?.RefreshFromSceneHistory();
	}

	private void RefreshSceneAnimationAfterHistoryIfVisible()
	{
		if (GodotObject.IsInstanceValid(_sceneAnimationTimelinePanel) && _sceneAnimationTimelinePanel.Visible && _sceneAnimationTimelinePanel.IsVisibleInTree())
		{
			_sceneAnimationTimelinePanel.RefreshFromSceneHistory();
		}
	}

	public void NotifySceneAnimationHistoryApplied()
	{
		CallDeferred("RefreshSceneAnimationAfterHistory");
		CallDeferred("UpdateCurrentSceneDirtyMarker");
	}

	public override void _Ready()
	{
		_sceneTabBar = GetNode<TabBar>("%SceneTabBar");
		_viewport = GetNode<XW2DViewport>("%Viewport2D");
		_mousePositionLabel = GetNode<Label>("%MousePositionLabel");
		_zoomLabel = GetNode<Label>("%ZoomLabel");
		_selectButton = GetNode<Button>("%SelectButton");
		_moveButton = GetNode<Button>("%MoveButton");
		_rotateButton = GetNode<Button>("%RotateButton");
		_scaleButton = GetNode<Button>("%ScaleButton");
		_listSelectButton = GetNode<Button>("%ListSelectButton");
		_pivotButton = GetNode<Button>("%PivotButton");
		_panButton = GetNode<Button>("%PanButton");
		_rulerButton = GetNode<Button>("%RulerButton");
		_pathButton = GetNode<Button>("%PathButton");
		_polygonButton = GetNode<Button>("%PolygonButton");
		_localSpaceButton = GetNode<Button>("%LocalSpaceButton");
		_smartSnapButton = GetNode<Button>("%SmartSnapButton");
		_gridSnapButton = GetNode<Button>("%GridSnapButton");
		_lockButton = GetNode<Button>("%LockButton");
		_unlockButton = GetNode<Button>("%UnlockButton");
		_groupButton = GetNode<Button>("%GroupButton");
		_ungroupButton = GetNode<Button>("%UngroupButton");
		_centerViewButton = GetNode<Button>("%CenterViewButton");
		_zoomOutButton = GetNode<Button>("%ZoomOutButton");
		_zoomResetButton = GetNode<Button>("%ZoomResetButton");
		_zoomInButton = GetNode<Button>("%ZoomInButton");
		_snapConfigMenu = GetNode<MenuButton>("%SnapConfigMenu");
		_skeletonMenu = GetNode<MenuButton>("%SkeletonMenu");
		_viewMenu = GetNode<MenuButton>("%ViewMenu");
		_viewportContextMenu = GetNode<PopupMenu>("%ViewportContextMenu");
		_workspaceTabs = GetNode<TabContainer>("%WorkspaceTabs");
		_allPropertiesButton = GetNode<Button>("%AllPropertiesButton");
		_nodeDirectPropertySurface = GetNode<XWDirectPropertySurface>("%NodeDirectPropertySurface");
		_unsavedSceneCloseDialog = GetNode<ConfirmationDialog>("%UnsavedSceneCloseDialog");
		_discardUnsavedSceneButton = _unsavedSceneCloseDialog.AddButton("不保存", right: true);
		_discardUnsavedSceneButton.Name = "DiscardUnsavedSceneButton";
		_discardUnsavedSceneButton.UniqueNameInOwner = true;
		_transformDirectPanel = GetNode<PanelContainer>("%TransformDirectPanel");
		_transformSelectionLabel = GetNode<Label>("%TransformSelectionLabel");
		_controlFields = GetNode<HBoxContainer>("%ControlFields");
		_positionXSpin = GetNode<SpinBox>("%PositionXSpin");
		_positionYSpin = GetNode<SpinBox>("%PositionYSpin");
		_rotationSpin = GetNode<SpinBox>("%RotationSpin");
		_scaleXSpin = GetNode<SpinBox>("%ScaleXSpin");
		_scaleYSpin = GetNode<SpinBox>("%ScaleYSpin");
		_sizeXSpin = GetNode<SpinBox>("%SizeXSpin");
		_sizeYSpin = GetNode<SpinBox>("%SizeYSpin");
		_pivotXSpin = GetNode<SpinBox>("%PivotXSpin");
		_pivotYSpin = GetNode<SpinBox>("%PivotYSpin");
		_workspaceTabs.SetTabTitle(0, "画布");
		_workspaceTabs.SetTabTitle(1, "节点属性");
		_workspaceTabs.SetTabIcon(1, XWClassRegistry.Instance.GetUIIcon("Object"));
		_nodeDirectPropertySurface.PropertyEdited += OnDirectNodePropertyEdited;
		InitializeControlLayoutSurface();
		InitializeSelectionLayoutSurface();
		InitializeSceneAnimationSurface();
		InitializeSceneGridSettings();
		InitializeSpriteAtlasSurface();
		_viewport.SetEditor(this);
		InitializeInlineTextEditing();
		_sceneHistoryManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (GodotObject.IsInstanceValid(_sceneHistoryManager))
		{
			_sceneHistoryManager.HistoryChanged += OnSceneHistoryChanged;
		}
		BuildMenus();
		ConnectSignals();
		SetToolMode(XW2DViewport.ToolMode.Select);
		UpdateSnapButtons();
		UpdateZoomLabel();
		UpdateMousePositionLabel(Vector2.Zero);
		UpdateSelectionActionButtons();
		UpdateDirectTransformSurface();
	}

	public override void _ExitTree()
	{
		ShutdownInlineTextEditing();
		ShutdownSpriteAtlasSurface();
		ShutdownSceneGridSettings();
		ShutdownSceneAnimationSurface();
		if (GodotObject.IsInstanceValid(_sceneHistoryManager))
		{
			_sceneHistoryManager.HistoryChanged -= OnSceneHistoryChanged;
		}
		_viewport?.ClearSceneInstance();
		XWSceneTreeDock xWSceneTreeDock = XWEditorInterface.Instance?.GetSceneTreeDock();
		foreach (SceneTabInfo value in _openTabs.Values)
		{
			_viewport?.ReleaseHistoryResources(value.HistoryId);
			xWSceneTreeDock?.ReleaseHistoryResources(value.HistoryId);
			if (GodotObject.IsInstanceValid(_sceneHistoryManager))
			{
				_sceneHistoryManager.ReleaseHistory(value.HistoryId);
			}
			FreeSceneInstance(value.SceneInstance);
		}
		_openTabs.Clear();
		_tabKeys.Clear();
		base._ExitTree();
	}

	public void LoadPackedScene(PackedScene packedScene, string sourcePath = "")
	{
		if (packedScene == null || !GodotObject.IsInstanceValid(packedScene))
		{
			XWEditorInterface.Instance?.ShowToast("无法加载场景资源");
			return;
		}
		string sceneKey = GetSceneKey(packedScene);
		if (_openTabs.ContainsKey(sceneKey))
		{
			TryRecoverScriptlessPreviewTab(_openTabs[sceneKey], showToast: true);
			SwitchToTab(sceneKey);
			return;
		}
		SaveCurrentTabState();
		bool scriptlessPreview;
		Node node = InstantiateSceneForEditing(packedScene, sourcePath, out scriptlessPreview);
		if (node != null)
		{
			Node sceneInstance = node;
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			int num = ((!GodotObject.IsInstanceValid(xWUndoRedoManager)) ? 1 : xWUndoRedoManager.CreateHistoryScope());
			SceneTabInfo sceneTabInfo = new SceneTabInfo
			{
				Key = sceneKey,
				DisplayName = GetSceneDisplayName(packedScene),
				SourcePath = (string.IsNullOrWhiteSpace(sourcePath) ? packedScene.ResourcePath : sourcePath),
				PackedScene = packedScene,
				SceneInstance = sceneInstance,
				ScriptlessPreview = scriptlessPreview,
				HistoryId = num
			};
			SeedSceneGridTabState(sceneTabInfo);
			_openTabs[sceneKey] = sceneTabInfo;
			_tabKeys.Add(sceneKey);
			_isSwitchingTab = true;
			_sceneTabBar.AddTab(sceneTabInfo.DisplayName, LoadIcon("res://addons/ModEditor/Icons/PackedScene.svg"));
			_sceneTabBar.CurrentTab = _tabKeys.Count - 1;
			_isSwitchingTab = false;
			if (GodotObject.IsInstanceValid(xWUndoRedoManager))
			{
				xWUndoRedoManager.SetHistoryAsSaved(num);
			}
			LoadTabData(sceneKey);
			XWEditorInterface.Instance?.FocusPanel("2d_editor");
			XWEditorInterface.Instance?.ShowToast("已打开场景: " + sceneTabInfo.DisplayName);
		}
		else
		{
			XWEditorInterface.Instance?.ShowToast("该场景没有有效根节点");
		}
	}

	public void LoadPackedSceneFromPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || !ResourceLoader.Exists(path))
		{
			XWEditorInterface.Instance?.ShowToast("场景不存在: " + path);
			return;
		}
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			XWEditorInterface.Instance?.ShowToast("无法加载场景: " + path);
		}
		else
		{
			LoadPackedScene(packedScene, path);
		}
	}

	public bool SaveCurrentScene()
	{
		if (string.IsNullOrEmpty(_currentTabKey) || !_openTabs.TryGetValue(_currentTabKey, out var value))
		{
			XWEditorInterface.Instance?.ShowToast("没有打开的 2D 场景");
			return false;
		}
		return SaveSceneTab(value, showToast: true);
	}

	public int SaveAllScenes(bool showToast = true)
	{
		int num = 0;
		int num2 = 0;
		foreach (string tabKey in _tabKeys)
		{
			if (_openTabs.TryGetValue(tabKey, out var value) && GodotObject.IsInstanceValid(value.SceneInstance) && (!GodotObject.IsInstanceValid(_sceneHistoryManager) || _sceneHistoryManager.IsHistoryUnsaved(value.HistoryId)))
			{
				if (SaveSceneTab(value, showToast: false))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast((num2 == 0) ? $"已保存全部 2D 场景（{num}）" : $"2D 场景保存完成：成功 {num}，失败 {num2}");
		}
		if (num2 != 0)
		{
			return -num2;
		}
		return num;
	}

	private bool SaveSceneTab(SceneTabInfo tabInfo, bool showToast)
	{
		if (tabInfo == null || !GodotObject.IsInstanceValid(tabInfo.SceneInstance))
		{
			return false;
		}
		CommitInlineTextEditForScene(tabInfo.SceneInstance);
		if (tabInfo.ScriptlessPreview && !TryRecoverScriptlessPreviewTab(tabInfo, showToast: false))
		{
			if (showToast)
			{
				XWEditorInterface.Instance?.ShowToast("当前场景仍是无脚本预览；请先成功编译 Mod C# 脚本再保存。");
			}
			return false;
		}
		string text = ((!string.IsNullOrWhiteSpace(tabInfo.SourcePath)) ? tabInfo.SourcePath : (tabInfo.PackedScene?.ResourcePath ?? ""));
		if (string.IsNullOrWhiteSpace(text))
		{
			if (showToast)
			{
				XWEditorInterface.Instance?.ShowToast("当前场景没有保存路径");
			}
			return false;
		}
		bool resume = SuspendSceneAnimationPreviewForSave();
		PackedScene packedScene = new PackedScene();
		Error error;
		try
		{
			error = packedScene.Pack(tabInfo.SceneInstance);
			if (error == Error.Ok)
			{
				packedScene.TakeOverPath(text);
				error = ResourceSaver.Save(packedScene, text, ResourceSaver.SaverFlags.None);
			}
		}
		finally
		{
			ResumeSceneAnimationPreviewAfterSave(resume);
		}
		if (error == Error.Ok)
		{
			tabInfo.PackedScene = packedScene;
			if (tabInfo.Key == _currentTabKey)
			{
				CurrentPackedScene = packedScene;
			}
			XWEditorInterface.Instance?.GetUndoRedoManager()?.SetHistoryAsSaved(tabInfo.HistoryId);
			UpdateSceneDirtyMarker(tabInfo.Key);
		}
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast((error == Error.Ok) ? ("已保存场景: " + text.GetFile()) : $"保存场景失败: {error}");
		}
		return error == Error.Ok;
	}

	private Node InstantiateSceneForEditing(PackedScene packedScene, string sourcePath, out bool scriptlessPreview)
	{
		scriptlessPreview = false;
		Node node = TryInstantiateFullScene(packedScene, sourcePath);
		if (GodotObject.IsInstanceValid(node))
		{
			return node;
		}
		PackedScene packedScene2 = LoadScriptlessPreviewPackedScene(sourcePath);
		if (GodotObject.IsInstanceValid(packedScene2))
		{
			Node node2 = packedScene2.Instantiate(PackedScene.GenEditState.Disabled);
			if (node2 != null)
			{
				Node result = node2;
				scriptlessPreview = true;
				return result;
			}
		}
		return null;
	}

	private static Node TryInstantiateFullScene(PackedScene packedScene, string sourcePath)
	{
		if (!AreAllExternalCSharpScriptsRegistered(sourcePath))
		{
			return null;
		}
		Node node;
		try
		{
			node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
		}
		catch (Exception ex)
		{
			GD.PushWarning("完整场景实例化失败，改用无脚本预览: " + ex.GetBaseException().Message);
			return null;
		}
		if (!GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		return node;
	}

	private static bool AreAllExternalCSharpScriptsRegistered(string sourcePath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || !Godot.FileAccess.FileExists(sourcePath))
		{
			return true;
		}
		string fileAsString = Godot.FileAccess.GetFileAsString(sourcePath);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string[] array = fileAsString.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		for (int i = 0; i < array.Length; i++)
		{
			if (TryReadExternalScriptResource(array[i], out var _, out var scriptPath))
			{
				if (!TryResolveExternalScriptPath(sourcePath, scriptPath, out var resolvedPath))
				{
					return false;
				}
				hashSet.Add(NormalizeComparablePath(resolvedPath));
			}
		}
		if (hashSet.Count == 0)
		{
			return true;
		}
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (Dictionary globalClass in ProjectSettings.GetGlobalClassList())
		{
			if (globalClass.ContainsKey("path"))
			{
				string text = NormalizeScriptResourcePath(sourcePath, globalClass["path"].AsString());
				if (!string.IsNullOrWhiteSpace(text))
				{
					hashSet2.Add(text);
				}
			}
		}
		return hashSet.IsSubsetOf(hashSet2);
	}

	private static PackedScene LoadScriptlessPreviewPackedScene(string sourcePath)
	{
		if (string.IsNullOrWhiteSpace(sourcePath) || !Godot.FileAccess.FileExists(sourcePath))
		{
			return null;
		}
		if (sourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || sourcePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return null;
		}
		string sceneText = StripUncompiledCSharpScriptResources(Godot.FileAccess.GetFileAsString(sourcePath), out var changed);
		sceneText = StripRuntimeCharacterScenePreviewBase(sceneText, out var changed2);
		if (!(changed || changed2))
		{
			return null;
		}
		string path = sourcePath.GetBaseDir().PathJoin($".modeditor_scene_scriptless_preview_{Guid.NewGuid():N}.tscn");
		using Godot.FileAccess fileAccess = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Write);
		if (fileAccess == null)
		{
			return null;
		}
		fileAccess.StoreString(sceneText);
		fileAccess.Close();
		PackedScene result = ResourceLoader.Load<PackedScene>(path, "", ResourceLoader.CacheMode.Ignore);
		DirAccess.RemoveAbsolute(path);
		return result;
	}

	private bool TryRecoverScriptlessPreviewTab(SceneTabInfo tabInfo, bool showToast)
	{
		if (tabInfo == null || !tabInfo.ScriptlessPreview)
		{
			return true;
		}
		string text = ((!string.IsNullOrWhiteSpace(tabInfo.SourcePath)) ? tabInfo.SourcePath : (tabInfo.PackedScene?.ResourcePath ?? ""));
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(text, "", ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			return false;
		}
		Node node = TryInstantiateFullScene(packedScene, text);
		if (!GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		bool num = tabInfo.Key == _currentTabKey;
		if (num)
		{
			tabInfo.ViewportState = _viewport.GetState();
			_viewport.ClearSceneInstance();
		}
		_viewport.ReleaseHistoryResources(tabInfo.HistoryId);
		XWEditorInterface.Instance?.GetSceneTreeDock()?.ReleaseHistoryResources(tabInfo.HistoryId);
		_sceneHistoryManager?.ReleaseHistory(tabInfo.HistoryId);
		FreeSceneInstance(tabInfo.SceneInstance);
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		int num2 = ((!GodotObject.IsInstanceValid(xWUndoRedoManager)) ? 1 : xWUndoRedoManager.CreateHistoryScope());
		tabInfo.PackedScene = packedScene;
		tabInfo.SceneInstance = node;
		tabInfo.ScriptlessPreview = false;
		tabInfo.HistoryId = num2;
		xWUndoRedoManager?.SetHistoryAsSaved(num2);
		if (num)
		{
			LoadTabData(tabInfo.Key);
		}
		UpdateSceneDirtyMarker(tabInfo.Key);
		if (showToast)
		{
			XWEditorInterface.Instance?.ShowToast("Mod C# 脚本已可用，场景已恢复为完整可编辑模式。");
		}
		return true;
	}

	private static string NormalizeScriptResourcePath(string sourcePath, string scriptResourcePath)
	{
		if (string.IsNullOrWhiteSpace(scriptResourcePath))
		{
			return "";
		}
		if (IsExternalCSharpScriptPath(scriptResourcePath) && TryResolveExternalScriptPath(sourcePath, scriptResourcePath, out var resolvedPath))
		{
			return NormalizeComparablePath(resolvedPath);
		}
		try
		{
			return NormalizeComparablePath(scriptResourcePath.StartsWith("res://", StringComparison.OrdinalIgnoreCase) ? ProjectSettings.GlobalizePath(scriptResourcePath) : scriptResourcePath);
		}
		catch
		{
			return "";
		}
	}

	private static bool TryResolveExternalScriptPath(string sourcePath, string scriptPath, out string resolvedPath)
	{
		resolvedPath = "";
		if (string.IsNullOrWhiteSpace(sourcePath) || !IsExternalCSharpScriptPath(scriptPath))
		{
			return false;
		}
		try
		{
			string directoryName = Path.GetDirectoryName(Path.GetFullPath(sourcePath));
			if (string.IsNullOrWhiteSpace(directoryName))
			{
				return false;
			}
			resolvedPath = Path.GetFullPath(Path.Combine(directoryName, scriptPath.Replace('/', Path.DirectorySeparatorChar))).Replace('\\', '/');
			return File.Exists(resolvedPath);
		}
		catch
		{
			resolvedPath = "";
			return false;
		}
	}

	private static string NormalizeComparablePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		try
		{
			return Path.GetFullPath(path).Replace('\\', '/').TrimEnd('/');
		}
		catch
		{
			return path.Replace('\\', '/').TrimEnd('/');
		}
	}

	private static string StripUncompiledCSharpScriptResources(string sceneText, out bool changed)
	{
		changed = false;
		if (string.IsNullOrWhiteSpace(sceneText))
		{
			return sceneText ?? "";
		}
		HashSet<string> hashSet = new HashSet<string>();
		List<string> list = new List<string>();
		string[] array = sceneText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		foreach (string text in array)
		{
			if (TryReadExternalScriptResource(text, out var scriptId))
			{
				hashSet.Add(scriptId);
				changed = true;
			}
			else
			{
				list.Add(text);
			}
		}
		if (hashSet.Count == 0)
		{
			return sceneText;
		}
		List<string> list2 = new List<string>();
		foreach (string item in list)
		{
			if (IsScriptAssignmentToStrippedResource(item, hashSet))
			{
				changed = true;
			}
			else
			{
				list2.Add(item);
			}
		}
		return string.Join("\n", list2);
	}

	private static string StripRuntimeCharacterScenePreviewBase(string sceneText, out bool changed)
	{
		changed = false;
		if (!IsModCharacterSceneText(sceneText) || !TryReadCharacterBaseSceneResourceId(sceneText, out var baseSceneId))
		{
			return sceneText ?? "";
		}
		List<string> list = new List<string>();
		string[] array = sceneText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		string[] array2 = array;
		foreach (string text in array2)
		{
			if (IsCharacterBaseSceneResourceLine(text, baseSceneId))
			{
				changed = true;
				continue;
			}
			if (!flag && TryReplaceCharacterRootNodeForPreview(text, baseSceneId, out var replacementLine))
			{
				list.Add(replacementLine);
				list.Add("metadata/modeditor_scriptless_preview = true");
				flag = true;
				flag2 = true;
				changed = true;
				continue;
			}
			if (flag && text.StartsWith("[node "))
			{
				if (!flag3)
				{
					AddMinimalCharacterSpritePreviewHierarchy(list);
					flag3 = true;
					changed = true;
				}
				flag2 = false;
			}
			if (flag2 && IsRuntimeCharacterRootAssignment(text))
			{
				changed = true;
			}
			else if (IsRuntimeCharacterComponentPreviewLine(text))
			{
				changed = true;
			}
			else
			{
				list.Add(text);
			}
		}
		if (flag && !flag3)
		{
			AddMinimalCharacterSpritePreviewHierarchy(list);
			changed = true;
		}
		if (!changed)
		{
			return sceneText;
		}
		return string.Join("\n", list);
	}

	private static bool IsModCharacterSceneText(string sceneText)
	{
		if (!string.IsNullOrWhiteSpace(sceneText) && sceneText.Contains("metadata/mod_resource_kind = \"Character\"", StringComparison.OrdinalIgnoreCase))
		{
			return sceneText.Contains("metadata/mod_character_sprite_scene", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool TryReadCharacterBaseSceneResourceId(string sceneText, out string baseSceneId)
	{
		baseSceneId = "";
		if (string.IsNullOrWhiteSpace(sceneText))
		{
			return false;
		}
		string[] array = sceneText.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
		foreach (string text in array)
		{
			if (text.StartsWith("[ext_resource") && text.Contains("type=\"PackedScene\"") && text.Contains("res://Prefab/TowerDefense/Character/TowerDefense"))
			{
				baseSceneId = ReadSceneResourceAttribute(text, "id");
				return !string.IsNullOrWhiteSpace(baseSceneId);
			}
		}
		return false;
	}

	private static bool IsCharacterBaseSceneResourceLine(string line, string baseSceneId)
	{
		if (!string.IsNullOrWhiteSpace(baseSceneId) && !string.IsNullOrWhiteSpace(line) && line.StartsWith("[ext_resource"))
		{
			return line.Contains("id=\"" + baseSceneId + "\"");
		}
		return false;
	}

	private static bool TryReplaceCharacterRootNodeForPreview(string line, string baseSceneId, out string replacementLine)
	{
		replacementLine = "";
		if (string.IsNullOrWhiteSpace(line) || string.IsNullOrWhiteSpace(baseSceneId) || !line.StartsWith("[node ") || !line.Contains("instance=ExtResource(\"" + baseSceneId + "\")"))
		{
			return false;
		}
		string text = ReadSceneResourceAttribute(line, "name");
		if (string.IsNullOrWhiteSpace(text))
		{
			text = "CharacterPreview";
		}
		replacementLine = "[node name=\"" + text + "\" type=\"Node2D\"]";
		return true;
	}

	private static bool IsRuntimeCharacterRootAssignment(string line)
	{
		string text = line?.Trim() ?? "";
		if (!text.StartsWith("script = ExtResource") && !text.StartsWith("config = ") && !text.StartsWith("sprite = "))
		{
			return text.StartsWith("node_paths=");
		}
		return true;
	}

	private static bool IsRuntimeCharacterComponentPreviewLine(string line)
	{
		if (string.IsNullOrWhiteSpace(line))
		{
			return false;
		}
		if (!line.Contains("name=\"ComponentManager\"") && !line.Contains("parent=\"ComponentManager\"") && !line.Contains("parent=\"ComponentManager/"))
		{
			return line.Contains("parent=\"./ComponentManager");
		}
		return true;
	}

	private static void AddMinimalCharacterSpritePreviewHierarchy(List<string> lines)
	{
		lines.Add("");
		lines.Add("[node name=\"SpriteGroup\" type=\"Node2D\" parent=\".\"]");
		lines.Add("");
		lines.Add("[node name=\"TransformPoint\" type=\"Marker2D\" parent=\"SpriteGroup\"]");
		lines.Add("");
	}

	private static bool TryReadExternalScriptResource(string line, out string scriptId)
	{
		string scriptPath;
		return TryReadExternalScriptResource(line, out scriptId, out scriptPath);
	}

	private static bool TryReadExternalScriptResource(string line, out string scriptId, out string scriptPath)
	{
		scriptId = "";
		scriptPath = "";
		if (string.IsNullOrWhiteSpace(line) || !line.StartsWith("[ext_resource") || !line.Contains("type=\"Script\""))
		{
			return false;
		}
		scriptPath = ReadSceneResourceAttribute(line, "path");
		if (!IsExternalCSharpScriptPath(scriptPath))
		{
			return false;
		}
		scriptId = ReadSceneResourceAttribute(line, "id");
		return !string.IsNullOrWhiteSpace(scriptId);
	}

	private static bool IsScriptAssignmentToStrippedResource(string line, HashSet<string> scriptIds)
	{
		if (scriptIds == null || scriptIds.Count == 0 || string.IsNullOrWhiteSpace(line))
		{
			return false;
		}
		string text = line.Trim();
		if (!text.StartsWith("script = ExtResource"))
		{
			return false;
		}
		foreach (string scriptId in scriptIds)
		{
			if (text == "script = ExtResource(\"" + scriptId + "\")")
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsExternalCSharpScriptPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		string text = path.Replace('\\', '/');
		if (text.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
		{
			return !text.StartsWith("uid://", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static string ReadSceneResourceAttribute(string line, string attributeName)
	{
		Match match = Regex.Match(line ?? "", "\\b" + Regex.Escape(attributeName) + "=\"([^\"]*)\"");
		if (!match.Success)
		{
			return "";
		}
		return match.Groups[1].Value;
	}

	public void RefreshSceneTree()
	{
		XWEditorInterface.Instance?.GetSceneTreeDock()?.RefreshScene();
	}

	private void BuildMenus()
	{
		PopupMenu popup = _snapConfigMenu.GetPopup();
		popup.Clear();
		popup.AddCheckItem("智能吸附", 1, Key.None);
		popup.AddCheckItem("网格吸附", 2, Key.None);
		popup.AddSeparator();
		popup.AddRadioCheckItem("显示网格", 3, Key.None);
		popup.AddRadioCheckItem("吸附时显示网格", 4, Key.None);
		popup.AddRadioCheckItem("隐藏网格", 5, Key.None);
		popup.AddSeparator();
		popup.AddItem("打开可视化网格与吸附面板…", 6, Key.None);
		popup.IdPressed += OnSnapMenuItem;
		PopupMenu popup2 = _skeletonMenu.GetPopup();
		popup2.Clear();
		popup2.AddItem("从节点生成骨骼", 1, Key.None);
		popup2.AddItem("清除骨骼", 2, Key.None);
		popup2.IdPressed += OnSkeletonMenuItem;
		PopupMenu popup3 = _viewMenu.GetPopup();
		popup3.Clear();
		popup3.AddItem("居中所选项", 1, Key.None);
		popup3.AddItem("聚焦所选项", 2, Key.None);
		popup3.AddSeparator();
		popup3.AddItem("放大", 3, Key.None);
		popup3.AddItem("缩小", 4, Key.None);
		popup3.AddItem("重置缩放", 5, Key.None);
		popup3.AddSeparator();
		popup3.AddCheckItem("显示原点", 10, Key.None);
		popup3.AddCheckItem("显示视口边框", 11, Key.None);
		popup3.AddCheckItem("显示标尺", 12, Key.None);
		popup3.AddCheckItem("显示参考线", 13, Key.None);
		popup3.AddCheckItem("显示辅助线", 14, Key.None);
		popup3.IdPressed += OnViewMenuItem;
		BuildViewportContextMenu();
		UpdateSnapMenuChecks();
		UpdateViewMenuChecks();
	}

	private void BuildViewportContextMenu()
	{
		_viewportContextMenu.Clear();
		XWClassRegistry instance = XWClassRegistry.Instance;
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Add"), "添加子节点", 1, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Instance"), "实例化场景...", 2, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Save"), "保存场景", 3, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Rename"), "重命名", 10, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Duplicate"), "复制", 11, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("MoveUp"), "上移", 12, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("MoveDown"), "下移", 13, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Remove"), "删除", 14, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Lock"), "锁定所选项", 15, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("Unlock"), "解锁所选项", 16, Key.None);
		_viewportContextMenu.AddIconItem(LoadIcon("res://addons/ModEditor/Icons/ClassIcon/Group.svg"), "分组所选项", 17, Key.None);
		_viewportContextMenu.AddIconItem(LoadIcon("res://addons/ModEditor/Icons/ClassIcon/Ungroup.svg"), "取消所选分组", 18, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddItem("复制节点路径", 19, Key.None);
		_viewportContextMenu.AddItem("设为场景根节点", 20, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("PackedScene"), "保存分支为场景...", 21, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("ScriptCreate"), "创建脚本", 30, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("ScriptRemove"), "分离脚本", 31, Key.None);
		_viewportContextMenu.AddIconItem(instance.GetUIIcon("ScriptExtend"), "扩展脚本", 32, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddIconItem(LoadIcon("res://addons/ModEditor/Icons/ClassIcon/CenterView.svg"), "居中所选项", 40, Key.None);
		_viewportContextMenu.AddItem("聚焦所选项", 41, Key.None);
		_viewportContextMenu.AddItem("放大", 42, Key.None);
		_viewportContextMenu.AddItem("缩小", 43, Key.None);
		_viewportContextMenu.AddItem("重置缩放", 44, Key.None);
		_viewportContextMenu.AddSeparator();
		_viewportContextMenu.AddCheckItem("显示原点", 50, Key.None);
		_viewportContextMenu.AddCheckItem("显示视口边框", 51, Key.None);
		_viewportContextMenu.AddCheckItem("显示标尺", 52, Key.None);
		_viewportContextMenu.AddCheckItem("显示参考线", 53, Key.None);
		_viewportContextMenu.AddCheckItem("显示辅助线", 54, Key.None);
		_viewportContextMenu.IdPressed += OnViewportContextMenuItem;
	}

	private void ConnectSignals()
	{
		_sceneTabBar.TabChanged += OnTabChanged;
		_sceneTabBar.TabClosePressed += OnTabClosePressed;
		_unsavedSceneCloseDialog.Confirmed += OnUnsavedSceneCloseConfirmed;
		_unsavedSceneCloseDialog.Canceled += OnUnsavedSceneCloseCanceled;
		_discardUnsavedSceneButton.Pressed += OnDiscardUnsavedScenePressed;
		_selectButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Select);
		};
		_moveButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Move);
		};
		_rotateButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Rotate);
		};
		_scaleButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Scale);
		};
		_listSelectButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.ListSelect);
		};
		_pivotButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Pivot);
		};
		_panButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Pan);
		};
		_rulerButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Ruler);
		};
		_pathButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Path);
		};
		_polygonButton.Pressed += delegate
		{
			SetToolMode(XW2DViewport.ToolMode.Polygon);
		};
		_localSpaceButton.Toggled += delegate(bool pressed)
		{
			_viewport.UseLocalSpace = pressed;
		};
		_smartSnapButton.Toggled += delegate(bool pressed)
		{
			_viewport.SmartSnapActive = pressed;
			UpdateSnapMenuChecks();
			_viewport.QueueRedrawAll();
		};
		_gridSnapButton.Toggled += delegate(bool pressed)
		{
			_viewport.GridSnapActive = pressed;
			UpdateSnapMenuChecks();
			_viewport.QueueRedrawAll();
		};
		_lockButton.Pressed += delegate
		{
			SetSelectionMetaWithHistory("_edit_lock_", enabled: true);
		};
		_unlockButton.Pressed += delegate
		{
			SetSelectionMetaWithHistory("_edit_lock_", enabled: false);
		};
		_groupButton.Pressed += delegate
		{
			SetSelectionMetaWithHistory("_edit_group_", enabled: true);
		};
		_ungroupButton.Pressed += delegate
		{
			SetSelectionMetaWithHistory("_edit_group_", enabled: false);
		};
		_centerViewButton.Pressed += delegate
		{
			_viewport.CenterSelection();
		};
		_zoomOutButton.Pressed += delegate
		{
			_viewport.SetZoom(_viewport.Zoom / 1.1f);
		};
		_zoomResetButton.Pressed += delegate
		{
			_viewport.SetZoom(1f);
		};
		_zoomInButton.Pressed += delegate
		{
			_viewport.SetZoom(_viewport.Zoom * 1.1f);
		};
		_viewport.ZoomChanged += UpdateZoomLabel;
		_viewport.SelectionChanged += UpdateSelectionActionButtons;
		_viewport.MousePositionChanged += UpdateMousePositionLabel;
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection != null)
		{
			xWEditorSelection.SelectionChanged += UpdateSelectionActionButtons;
			xWEditorSelection.SelectionChanged += UpdateDirectTransformSurface;
		}
		ConnectDirectTransformSurface();
		_allPropertiesButton.Pressed += delegate
		{
			ShowDirectNodeProperties();
		};
		_workspaceTabs.TabChanged += OnWorkspaceTabChanged;
	}

	private void ConnectDirectTransformSurface()
	{
		SpinBox[] directTransformSpins = GetDirectTransformSpins();
		foreach (SpinBox spinBox in directTransformSpins)
		{
			SpinBox capturedSpin = spinBox;
			spinBox.ValueChanged += delegate
			{
				OnDirectTransformValueChanged(capturedSpin);
			};
			LineEdit lineEdit = spinBox.GetLineEdit();
			lineEdit.FocusEntered += BeginDirectTransformEditSession;
			lineEdit.FocusExited += CommitDirectTransformEditSession;
		}
	}

	private SpinBox[] GetDirectTransformSpins()
	{
		return new SpinBox[9] { _positionXSpin, _positionYSpin, _rotationSpin, _scaleXSpin, _scaleYSpin, _sizeXSpin, _sizeYSpin, _pivotXSpin, _pivotYSpin };
	}

	private void UpdateDirectTransformSurface()
	{
		if (_directTransformSessionActive)
		{
			CommitDirectTransformEditSession();
		}
		List<Node> obj = XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>();
		_directTransformNodes.Clear();
		foreach (Node item in obj)
		{
			if ((item is Node2D || item is Control) ? true : false)
			{
				_directTransformNodes.Add(item);
			}
		}
		Node node = (_directTransformNode = ((_directTransformNodes.Count > 0) ? _directTransformNodes[0] : null));
		bool flag = _directTransformNodes.Count > 0;
		bool flag2 = flag;
		foreach (Node directTransformNode in _directTransformNodes)
		{
			flag2 = flag2 && directTransformNode is Control;
		}
		_transformSelectionLabel.Text = ((!flag) ? "未选择 2D 节点" : ((_directTransformNodes.Count == 1) ? $"{node.Name}  ·  {node.GetType().Name}" : $"{_directTransformNodes.Count} 个 2D 节点（批量）"));
		_transformDirectPanel.TooltipText = (flag ? "直接编辑所选节点；混合值以 • 标识，修改一个字段会批量写回该字段" : "在画布或场景树中选择 Node2D / Control");
		SpinBox[] directTransformSpins = GetDirectTransformSpins();
		for (int i = 0; i < directTransformSpins.Length; i++)
		{
			directTransformSpins[i].Editable = flag;
		}
		_controlFields.Visible = flag2;
		_updatingDirectTransformSurface = true;
		List<DirectTransformState> list = new List<DirectTransformState>();
		foreach (Node directTransformNode2 in _directTransformNodes)
		{
			if (TryCaptureDirectTransformState(directTransformNode2, out var state))
			{
				list.Add(state);
			}
		}
		if (list.Count > 0)
		{
			SetDirectSpinValue(_positionXSpin, list, (DirectTransformState directTransformState) => directTransformState.Position.X);
			SetDirectSpinValue(_positionYSpin, list, (DirectTransformState directTransformState) => directTransformState.Position.Y);
			SetDirectSpinValue(_rotationSpin, list, (DirectTransformState directTransformState) => Mathf.RadToDeg(directTransformState.Rotation));
			SetDirectSpinValue(_scaleXSpin, list, (DirectTransformState directTransformState) => directTransformState.Scale.X);
			SetDirectSpinValue(_scaleYSpin, list, (DirectTransformState directTransformState) => directTransformState.Scale.Y);
			SetDirectSpinValue(_sizeXSpin, list, (DirectTransformState directTransformState) => directTransformState.Size.X, flag2);
			SetDirectSpinValue(_sizeYSpin, list, (DirectTransformState directTransformState) => directTransformState.Size.Y, flag2);
			SetDirectSpinValue(_pivotXSpin, list, (DirectTransformState directTransformState) => directTransformState.Pivot.X, flag2);
			SetDirectSpinValue(_pivotYSpin, list, (DirectTransformState directTransformState) => directTransformState.Pivot.Y, flag2);
		}
		else
		{
			directTransformSpins = GetDirectTransformSpins();
			foreach (SpinBox spinBox in directTransformSpins)
			{
				spinBox.SetValueNoSignal(0.0);
				ClearDirectSpinMixedMarker(spinBox);
			}
		}
		_updatingDirectTransformSurface = false;
		UpdateSelectionLayoutSurface();
		UpdateControlLayoutSurface();
		RefreshDirectNodePropertySurface();
	}

	public void ShowDirectNodeProperties(Node node = null, bool focusTab = true)
	{
		Node target = (GodotObject.IsInstanceValid(node) ? node : ResolveDirectPropertyTarget());
		BindDirectNodePropertyTarget(target);
		if (focusTab && GodotObject.IsInstanceValid(_workspaceTabs))
		{
			_workspaceTabs.CurrentTab = 1;
		}
	}

	public void RefreshDirectNodePropertySurface()
	{
		BindDirectNodePropertyTarget(ResolveDirectPropertyTarget());
	}

	private Node ResolveDirectPropertyTarget()
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			if (GodotObject.IsInstanceValid(item))
			{
				return item;
			}
		}
		if (!GodotObject.IsInstanceValid(CurrentSceneInstance))
		{
			return null;
		}
		return CurrentSceneInstance;
	}

	private void BindDirectNodePropertyTarget(Node target)
	{
		if (GodotObject.IsInstanceValid(_nodeDirectPropertySurface))
		{
			XWUndoRedoManager undoRedo = XWEditorInterface.Instance?.GetUndoRedoManager();
			_nodeDirectPropertySurface.BindObject(target, undoRedo, GetSpecialized2DPropertyNames(target), GetCurrentSceneHistoryId(), includeScript: true);
			bool flag = GodotObject.IsInstanceValid(target);
			if (GodotObject.IsInstanceValid(_allPropertiesButton))
			{
				_allPropertiesButton.Disabled = !flag;
				_allPropertiesButton.TooltipText = (flag ? $"在 2D 主工作区打开 {target.Name} 的全部属性拼图" : "请先打开场景或选择节点");
			}
		}
	}

	private static IReadOnlyCollection<string> GetSpecialized2DPropertyNames(Node target)
	{
		HashSet<string> hashSet = new HashSet<string> { "position", "rotation", "rotation_degrees", "scale" };
		if (target is Control)
		{
			hashSet.UnionWith(new string[12]
			{
				"size", "pivot_offset", "anchor_left", "anchor_top", "anchor_right", "anchor_bottom", "offset_left", "offset_top", "offset_right", "offset_bottom",
				"grow_horizontal", "grow_vertical"
			});
		}
		return hashSet;
	}

	private void OnDirectNodePropertyEdited(GodotObject target, StringName property, StringName field, Variant value)
	{
		MarkCurrentSceneHistorySurfaceDirty();
		_nodeDirectPropertySurface?.RefreshValues();
		_viewport?.NotifySceneStructureChanged();
		_viewport?.QueueRedrawAll();
		UpdateCurrentSceneDirtyMarker();
	}

	private void OnWorkspaceTabChanged(long tab)
	{
		if (tab == 1)
		{
			XWEditorInterface.Instance?.GetUndoRedoManager()?.SetCurrentHistoryType(GetCurrentSceneHistoryId());
			RefreshDirectNodePropertySurface();
		}
	}

	private void BeginDirectTransformEditSession()
	{
		if (_updatingDirectTransformSurface || _directTransformSessionActive || _directTransformNodes.Count == 0)
		{
			return;
		}
		_directTransformStarts.Clear();
		foreach (Node directTransformNode in _directTransformNodes)
		{
			if (TryCaptureDirectTransformState(directTransformNode, out var state))
			{
				_directTransformStarts[directTransformNode] = state;
			}
		}
		if (_directTransformStarts.Count != 0)
		{
			_directTransformTouchedSpins.Clear();
			_directTransformSessionActive = true;
		}
	}

	private void OnDirectTransformValueChanged(SpinBox changedSpin)
	{
		if (_updatingDirectTransformSurface || _directTransformNodes.Count == 0)
		{
			return;
		}
		if (!_directTransformSessionActive)
		{
			BeginDirectTransformEditSession();
		}
		if (!_directTransformSessionActive)
		{
			return;
		}
		_directTransformTouchedSpins.Add(changedSpin);
		ClearDirectSpinMixedMarker(changedSpin);
		foreach (Node directTransformNode in _directTransformNodes)
		{
			ApplyDirectSpinValue(directTransformNode, changedSpin);
		}
		_viewport.QueueRedrawAll();
	}

	private void CommitDirectTransformEditSession()
	{
		if (!_directTransformSessionActive)
		{
			return;
		}
		_directTransformSessionActive = false;
		List<(Node, DirectTransformState, DirectTransformState)> list = new List<(Node, DirectTransformState, DirectTransformState)>();
		foreach (var (node2, directTransformState2) in _directTransformStarts)
		{
			if (TryCaptureDirectTransformState(node2, out var state) && HasDirectTransformChanged(directTransformState2, state))
			{
				list.Add((node2, directTransformState2, state));
			}
		}
		_directTransformStarts.Clear();
		_directTransformTouchedSpins.Clear();
		if (list.Count == 0)
		{
			return;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			return;
		}
		xWUndoRedoManager.CreateAction((list.Count == 1) ? "直接编辑 2D 变换" : "批量编辑 2D 变换", mergeMode: false, GetCurrentSceneHistoryId());
		foreach (var item in list)
		{
			AddDirectTransformHistoryProperties(xWUndoRedoManager, item.Item1, item.Item2, item.Item3);
		}
		xWUndoRedoManager.AddDoMethod(this, "NotifyDirectTransformHistoryApplied");
		xWUndoRedoManager.AddUndoMethod(this, "NotifyDirectTransformHistoryApplied");
		xWUndoRedoManager.CommitAction();
		_viewport.ActivateSceneTransformHistoryDeferred();
	}

	private static void AddDirectTransformHistoryProperties(XWUndoRedoManager undoRedo, Node target, DirectTransformState before, DirectTransformState after)
	{
		undoRedo.AddDoProperty(target, "position", Variant.From(in after.Position));
		undoRedo.AddUndoProperty(target, "position", Variant.From(in before.Position));
		undoRedo.AddDoProperty(target, "rotation", Variant.From(in after.Rotation));
		undoRedo.AddUndoProperty(target, "rotation", Variant.From(in before.Rotation));
		undoRedo.AddDoProperty(target, "scale", Variant.From(in after.Scale));
		undoRedo.AddUndoProperty(target, "scale", Variant.From(in before.Scale));
		if (target is Control)
		{
			undoRedo.AddDoProperty(target, "size", Variant.From(in after.Size));
			undoRedo.AddUndoProperty(target, "size", Variant.From(in before.Size));
			undoRedo.AddDoProperty(target, "pivot_offset", Variant.From(in after.Pivot));
			undoRedo.AddUndoProperty(target, "pivot_offset", Variant.From(in before.Pivot));
		}
	}

	private static bool TryCaptureDirectTransformState(Node node, out DirectTransformState state)
	{
		state = default(DirectTransformState);
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		if (node is Control control)
		{
			state = new DirectTransformState(control.Position, control.Scale, control.Rotation, control.Size, control.PivotOffset, isControl: true);
			return true;
		}
		if (node is Node2D node2D)
		{
			state = new DirectTransformState(node2D.Position, node2D.Scale, node2D.Rotation, Vector2.Zero, Vector2.Zero, isControl: false);
			return true;
		}
		return false;
	}

	private static bool HasDirectTransformChanged(DirectTransformState before, DirectTransformState after)
	{
		if (before.IsControl == after.IsControl && before.Position.IsEqualApprox(after.Position) && before.Scale.IsEqualApprox(after.Scale) && Mathf.IsEqualApprox(before.Rotation, after.Rotation))
		{
			if (before.IsControl)
			{
				if (before.Size.IsEqualApprox(after.Size))
				{
					return !before.Pivot.IsEqualApprox(after.Pivot);
				}
				return true;
			}
			return false;
		}
		return true;
	}

	public void NotifyDirectTransformHistoryApplied()
	{
		MarkCurrentSceneHistorySurfaceDirty();
		UpdateDirectTransformSurface();
		_viewport.QueueRedrawAll();
	}

	public void RefreshDirectTransformSurface()
	{
		UpdateDirectTransformSurface();
	}

	public void DetachCurrentSceneRootForHistory()
	{
		_sceneRootHistoryViewportState = _viewport?.GetState() ?? new Dictionary();
		_viewport?.DetachSceneInstanceForHistory();
	}

	public void ApplySceneRootFromHistory(Node sceneRoot)
	{
		if (sceneRoot != null && GodotObject.IsInstanceValid(sceneRoot))
		{
			CancelInlineTextEdit();
			if (_openTabs.TryGetValue(_currentTabKey, out var value))
			{
				value.SceneInstance = sceneRoot;
			}
			CurrentSceneInstance = sceneRoot;
			XWEditorInterface.Instance?.ClearSelection();
			_viewport?.SetSceneInstanceFromHistory(sceneRoot);
			XWEditorInterface.Instance?.SetEditedSceneRoot(sceneRoot);
			if (_sceneRootHistoryViewportState.Count > 0)
			{
				_viewport?.SetState(_sceneRootHistoryViewportState);
			}
			XWEditorInterface.Instance?.GetSceneTreeDock()?.ApplySceneRootFromHistory(sceneRoot);
			RebindSceneAnimationRoot(sceneRoot);
			UpdateSelectionActionButtons();
			UpdateDirectTransformSurface();
			QueueViewportRedraw();
		}
	}

	private void SaveCurrentTabState()
	{
		if (!string.IsNullOrEmpty(_currentTabKey) && _openTabs.TryGetValue(_currentTabKey, out var value))
		{
			value.ViewportState = _viewport.GetState();
			SaveSceneAnimationTabState(value);
		}
	}

	private void LoadTabData(string tabKey)
	{
		if (_openTabs.TryGetValue(tabKey, out var value))
		{
			CommitInlineTextEdit();
			CurrentPackedScene = value.PackedScene;
			CurrentSceneInstance = value.SceneInstance;
			_currentTabKey = tabKey;
			XWEditorInterface.Instance?.GetUndoRedoManager()?.SetCurrentHistoryType(value.HistoryId);
			XWEditorInterface.Instance?.ClearSelection();
			XWEditorInterface.Instance?.SetEditedSceneRoot(CurrentSceneInstance);
			_viewport.SetSceneInstance(CurrentSceneInstance);
			_viewport.SetState(value.ViewportState);
			SyncGridSettingsPanelFromViewport();
			SyncToolbarFromViewportState();
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SetScene(CurrentSceneInstance);
			LoadSceneAnimationTab(value);
			ShowDirectNodeProperties(CurrentSceneInstance, focusTab: false);
			UpdateZoomLabel();
			UpdateSelectionActionButtons();
			UpdateCurrentSceneDirtyMarker();
		}
	}

	private void SwitchToTab(string tabKey)
	{
		int num = _tabKeys.IndexOf(tabKey);
		if (num >= 0)
		{
			SaveCurrentTabState();
			_isSwitchingTab = true;
			_sceneTabBar.CurrentTab = num;
			_isSwitchingTab = false;
			LoadTabData(tabKey);
			XWEditorInterface.Instance?.FocusPanel("2d_editor");
		}
	}

	private void OnTabChanged(long tabIdx)
	{
		if (!_isSwitchingTab && tabIdx >= 0 && tabIdx < _tabKeys.Count)
		{
			string text = _tabKeys[(int)tabIdx];
			if (!(text == _currentTabKey))
			{
				SaveCurrentTabState();
				LoadTabData(text);
			}
		}
	}

	private void OnTabClosePressed(long tabIdx)
	{
		if (tabIdx >= 0 && tabIdx < _tabKeys.Count)
		{
			RequestCloseSceneTab(_tabKeys[(int)tabIdx]);
		}
	}

	public int GetCurrentSceneHistoryId()
	{
		if (!_openTabs.TryGetValue(_currentTabKey, out var value))
		{
			return 1;
		}
		return value.HistoryId;
	}

	public int GetSceneHistoryIdAt(long tabIndex)
	{
		if (tabIndex < 0 || tabIndex >= _tabKeys.Count || !_openTabs.TryGetValue(_tabKeys[(int)tabIndex], out var value))
		{
			return -1;
		}
		return value.HistoryId;
	}

	public bool IsSceneTabDirty(long tabIndex)
	{
		int sceneHistoryIdAt = GetSceneHistoryIdAt(tabIndex);
		if (sceneHistoryIdAt >= 0 && GodotObject.IsInstanceValid(_sceneHistoryManager))
		{
			return _sceneHistoryManager.IsHistoryUnsaved(sceneHistoryIdAt);
		}
		return false;
	}

	private void RequestCloseSceneTab(string tabKey)
	{
		if (_openTabs.TryGetValue(tabKey, out var value))
		{
			if (tabKey == _currentTabKey)
			{
				CommitInlineTextEdit();
			}
			if (GodotObject.IsInstanceValid(_sceneHistoryManager) && _sceneHistoryManager.IsHistoryUnsaved(value.HistoryId))
			{
				_pendingCloseTabKey = tabKey;
				_unsavedSceneCloseDialog.DialogText = "场景“" + value.DisplayName + "”仍有未保存的修改。";
				_unsavedSceneCloseDialog.PopupCentered();
			}
			else
			{
				CloseSceneTabImmediately(tabKey);
			}
		}
	}

	private void OnUnsavedSceneCloseConfirmed()
	{
		string pendingCloseTabKey = _pendingCloseTabKey;
		if (string.IsNullOrEmpty(pendingCloseTabKey) || !_openTabs.ContainsKey(pendingCloseTabKey))
		{
			_pendingCloseTabKey = "";
			return;
		}
		if (pendingCloseTabKey != _currentTabKey)
		{
			SwitchToTab(pendingCloseTabKey);
		}
		if (SaveCurrentScene())
		{
			_pendingCloseTabKey = "";
			CloseSceneTabImmediately(pendingCloseTabKey);
		}
	}

	private void OnUnsavedSceneCloseCanceled()
	{
		_pendingCloseTabKey = "";
	}

	private void OnDiscardUnsavedScenePressed()
	{
		string pendingCloseTabKey = _pendingCloseTabKey;
		_pendingCloseTabKey = "";
		_unsavedSceneCloseDialog.Hide();
		if (!string.IsNullOrEmpty(pendingCloseTabKey))
		{
			CloseSceneTabImmediately(pendingCloseTabKey);
		}
	}

	private void CloseSceneTabImmediately(string tabKey)
	{
		int num = _tabKeys.IndexOf(tabKey);
		if (num < 0 || !_openTabs.TryGetValue(tabKey, out var value))
		{
			return;
		}
		bool flag = tabKey == _currentTabKey;
		if (flag)
		{
			_viewport.ClearSceneInstance();
		}
		_viewport.ReleaseHistoryResources(value.HistoryId);
		XWEditorInterface.Instance?.GetSceneTreeDock()?.ReleaseHistoryResources(value.HistoryId);
		_sceneHistoryManager?.ReleaseHistory(value.HistoryId);
		FreeSceneInstance(value.SceneInstance);
		_openTabs.Remove(tabKey);
		_tabKeys.RemoveAt(num);
		_sceneTabBar.RemoveTab(num);
		if (_tabKeys.Count == 0)
		{
			ClearEditor();
			return;
		}
		int a = (flag ? Mathf.Min(num, _tabKeys.Count - 1) : _tabKeys.IndexOf(_currentTabKey));
		_isSwitchingTab = true;
		_sceneTabBar.CurrentTab = Mathf.Max(a, 0);
		_isSwitchingTab = false;
		if (flag)
		{
			LoadTabData(_tabKeys[Mathf.Max(a, 0)]);
		}
	}

	private void OnSceneHistoryChanged(int historyId)
	{
		foreach (var (text2, sceneTabInfo2) in _openTabs)
		{
			if (sceneTabInfo2.HistoryId == historyId)
			{
				UpdateSceneDirtyMarker(text2);
				if (text2 == _currentTabKey)
				{
					RefreshSceneAnimationAfterHistoryIfVisible();
				}
				break;
			}
		}
	}

	private void ClearEditor()
	{
		CancelInlineTextEdit();
		_currentTabKey = "";
		CurrentPackedScene = null;
		CurrentSceneInstance = null;
		_viewport.ClearSceneInstance();
		XWEditorInterface.Instance?.GetUndoRedoManager()?.SetCurrentHistoryType(0);
		XWEditorInterface.Instance?.ClearSelection();
		XWEditorInterface.Instance?.GetSceneTreeDock()?.SetScene(null);
		_sceneAnimationTimelinePanel?.ClearSurface();
		SetSceneAnimationTimelineVisible(visible: false);
		BindDirectNodePropertyTarget(null);
		if (GodotObject.IsInstanceValid(_workspaceTabs))
		{
			_workspaceTabs.CurrentTab = 0;
		}
		UpdateSelectionActionButtons();
	}

	public void OpenViewportContextMenu(Vector2 viewPosition, Vector2 worldPosition, Node hitNode)
	{
		if (hitNode != null && GodotObject.IsInstanceValid(hitNode))
		{
			XWEditorInterface.Instance?.SetSelectedNode(hitNode);
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(hitNode);
			ShowDirectNodeProperties(hitNode, focusTab: false);
			UpdateSelectionActionButtons();
		}
		_contextMenuWorldPosition = worldPosition;
		UpdateViewportContextMenuState();
		_viewportContextMenu.Position = (Vector2I)(_viewport.GetScreenPosition() + viewPosition);
		_viewportContextMenu.Popup();
	}

	public void QueueViewportRedraw()
	{
		_viewport?.NotifySceneStructureChanged();
	}

	public void PrepareForSceneHistoryBranchChange()
	{
		_viewport?.PrepareForSceneHistoryBranchChange();
	}

	private void SetToolMode(XW2DViewport.ToolMode mode)
	{
		_viewport.SetToolMode(mode);
		SetToolButtonPressed(_selectButton, mode == XW2DViewport.ToolMode.Select);
		SetToolButtonPressed(_moveButton, mode == XW2DViewport.ToolMode.Move);
		SetToolButtonPressed(_rotateButton, mode == XW2DViewport.ToolMode.Rotate);
		SetToolButtonPressed(_scaleButton, mode == XW2DViewport.ToolMode.Scale);
		SetToolButtonPressed(_listSelectButton, mode == XW2DViewport.ToolMode.ListSelect);
		SetToolButtonPressed(_pivotButton, mode == XW2DViewport.ToolMode.Pivot);
		SetToolButtonPressed(_panButton, mode == XW2DViewport.ToolMode.Pan);
		SetToolButtonPressed(_rulerButton, mode == XW2DViewport.ToolMode.Ruler);
		SetToolButtonPressed(_pathButton, mode == XW2DViewport.ToolMode.Path);
		SetToolButtonPressed(_polygonButton, mode == XW2DViewport.ToolMode.Polygon);
	}

	private void OnSnapMenuItem(long id)
	{
		switch ((SnapMenuId)id)
		{
		case SnapMenuId.SmartSnap:
			_viewport.SmartSnapActive = !_viewport.SmartSnapActive;
			break;
		case SnapMenuId.GridSnap:
			_viewport.GridSnapActive = !_viewport.GridSnapActive;
			break;
		case SnapMenuId.ShowGrid:
			_viewport.SetGridVisibility(XW2DViewport.GridVisibility.Show);
			break;
		case SnapMenuId.ShowGridWhenSnapping:
			_viewport.SetGridVisibility(XW2DViewport.GridVisibility.ShowWhenSnapping);
			break;
		case SnapMenuId.HideGrid:
			_viewport.SetGridVisibility(XW2DViewport.GridVisibility.Hide);
			break;
		case SnapMenuId.OpenVisualSettings:
			OpenGridSettingsPanel();
			break;
		}
		UpdateSnapButtons();
		UpdateSnapMenuChecks();
		_viewport.QueueRedrawAll();
	}

	private void OnViewMenuItem(long id)
	{
		switch ((ViewMenuId)id)
		{
		case ViewMenuId.CenterSelection:
			_viewport.CenterSelection();
			break;
		case ViewMenuId.FrameSelection:
			_viewport.FrameSelection();
			break;
		case ViewMenuId.ZoomIn:
			_viewport.SetZoom(_viewport.Zoom * 1.1f);
			break;
		case ViewMenuId.ZoomOut:
			_viewport.SetZoom(_viewport.Zoom / 1.1f);
			break;
		case ViewMenuId.ZoomReset:
			_viewport.SetZoom(1f);
			break;
		case ViewMenuId.ShowOrigin:
			_viewport.ShowOrigin = !_viewport.ShowOrigin;
			break;
		case ViewMenuId.ShowViewport:
			_viewport.ShowViewportRect = !_viewport.ShowViewportRect;
			break;
		case ViewMenuId.ShowRulers:
			_viewport.ShowRulers = !_viewport.ShowRulers;
			break;
		case ViewMenuId.ShowGuides:
			_viewport.ShowGuides = !_viewport.ShowGuides;
			break;
		case ViewMenuId.ShowHelpers:
			_viewport.ShowHelpers = !_viewport.ShowHelpers;
			break;
		}
		UpdateViewMenuChecks();
		_viewport.QueueRedrawAll();
	}

	private void OnViewportContextMenuItem(long id)
	{
		XWSceneTreeDock xWSceneTreeDock = XWEditorInterface.Instance?.GetSceneTreeDock();
		switch ((ViewportContextMenuId)id)
		{
		case ViewportContextMenuId.AddNode:
			xWSceneTreeDock?.PopupAddNodeMenuAtMouse(_contextMenuWorldPosition);
			break;
		case ViewportContextMenuId.InstantiateScene:
			xWSceneTreeDock?.ShowInstantiateDialog();
			break;
		case ViewportContextMenuId.SaveScene:
			SaveCurrentScene();
			break;
		case ViewportContextMenuId.Rename:
			xWSceneTreeDock?.ShowRenameSelectedDialog();
			break;
		case ViewportContextMenuId.Duplicate:
			xWSceneTreeDock?.DuplicateSelectedNode();
			break;
		case ViewportContextMenuId.MoveUp:
			xWSceneTreeDock?.MoveSelectedNode(-1);
			break;
		case ViewportContextMenuId.MoveDown:
			xWSceneTreeDock?.MoveSelectedNode(1);
			break;
		case ViewportContextMenuId.Delete:
			xWSceneTreeDock?.RemoveSelectedNode();
			break;
		case ViewportContextMenuId.LockSelection:
			SetSelectionMetaWithHistory("_edit_lock_", enabled: true);
			break;
		case ViewportContextMenuId.UnlockSelection:
			SetSelectionMetaWithHistory("_edit_lock_", enabled: false);
			break;
		case ViewportContextMenuId.GroupSelection:
			SetSelectionMetaWithHistory("_edit_group_", enabled: true);
			break;
		case ViewportContextMenuId.UngroupSelection:
			SetSelectionMetaWithHistory("_edit_group_", enabled: false);
			break;
		case ViewportContextMenuId.CopyNodePath:
			xWSceneTreeDock?.CopySelectedNodePath();
			break;
		case ViewportContextMenuId.MakeRoot:
			if (xWSceneTreeDock != null && xWSceneTreeDock.MakeSelectedNodeRoot())
			{
				XWEditorInterface.Instance?.ShowToast("已设置场景根节点");
			}
			break;
		case ViewportContextMenuId.SaveBranchAsScene:
			xWSceneTreeDock?.ShowSaveBranchDialogForSelected();
			break;
		case ViewportContextMenuId.CreateScript:
			xWSceneTreeDock?.ShowCreateScriptDialog();
			break;
		case ViewportContextMenuId.DetachScript:
			xWSceneTreeDock?.DetachSelectedScript();
			break;
		case ViewportContextMenuId.ExtendScript:
			xWSceneTreeDock?.ExtendSelectedScript();
			break;
		case ViewportContextMenuId.CenterSelection:
			_viewport.CenterSelection();
			break;
		case ViewportContextMenuId.FrameSelection:
			_viewport.FrameSelection();
			break;
		case ViewportContextMenuId.ZoomIn:
			_viewport.SetZoom(_viewport.Zoom * 1.1f);
			break;
		case ViewportContextMenuId.ZoomOut:
			_viewport.SetZoom(_viewport.Zoom / 1.1f);
			break;
		case ViewportContextMenuId.ZoomReset:
			_viewport.SetZoom(1f);
			break;
		case ViewportContextMenuId.ShowOrigin:
			_viewport.ShowOrigin = !_viewport.ShowOrigin;
			break;
		case ViewportContextMenuId.ShowViewport:
			_viewport.ShowViewportRect = !_viewport.ShowViewportRect;
			break;
		case ViewportContextMenuId.ShowRulers:
			_viewport.ShowRulers = !_viewport.ShowRulers;
			break;
		case ViewportContextMenuId.ShowGuides:
			_viewport.ShowGuides = !_viewport.ShowGuides;
			break;
		case ViewportContextMenuId.ShowHelpers:
			_viewport.ShowHelpers = !_viewport.ShowHelpers;
			break;
		}
		UpdateSelectionActionButtons();
		UpdateViewMenuChecks();
		UpdateViewportContextMenuState();
		_viewport.QueueRedrawAll();
	}

	private void OnSkeletonMenuItem(long id)
	{
		switch ((SkeletonMenuId)id)
		{
		case SkeletonMenuId.MakeBones:
			GenerateSkeletonFromSelectionWithHistory();
			break;
		case SkeletonMenuId.ClearBones:
			ClearGeneratedSkeletonWithHistory();
			break;
		}
	}

	public Skeleton2D GenerateSkeletonFromSelectionWithHistory()
	{
		Node currentSceneInstance = CurrentSceneInstance;
		if (currentSceneInstance == null || !GodotObject.IsInstanceValid(currentSceneInstance))
		{
			XWEditorInterface.Instance?.ShowToast("请先打开 2D 场景");
			return null;
		}
		Skeleton2D skeleton2D = FindGeneratedSkeleton(currentSceneInstance);
		if (GodotObject.IsInstanceValid(skeleton2D))
		{
			XWEditorInterface.Instance?.ShowToast("当前场景已有生成骨骼，请先清除");
			return skeleton2D;
		}
		List<Node2D> list = CollectBoneSourceNodes(currentSceneInstance);
		if (list.Count == 0)
		{
			XWEditorInterface.Instance?.ShowToast("请选择至少一个 Node2D 节点生成骨骼");
			return null;
		}
		list.Sort((Node2D left, Node2D right) => GetNodeDepth(left).CompareTo(GetNodeDepth(right)));
		Skeleton2D skeleton2D2 = new Skeleton2D
		{
			Name = BuildUniqueChildName(currentSceneInstance, "GeneratedSkeleton2D")
		};
		skeleton2D2.SetMeta("_xw_generated_skeleton_2d", true);
		System.Collections.Generic.Dictionary<Node2D, Bone2D> dictionary = new System.Collections.Generic.Dictionary<Node2D, Bone2D>();
		foreach (Node2D item in list)
		{
			Node2D node2D = FindNearestBoneSourceAncestor(item, dictionary);
			Node node = ((node2D != null) ? ((Node2D)dictionary[node2D]) : ((Node2D)skeleton2D2));
			Transform2D boneRelativeTransform = GetBoneRelativeTransform(currentSceneInstance, item, node2D);
			Bone2D bone2D = new Bone2D
			{
				Name = BuildUniqueChildName(node, $"{item.Name}Bone"),
				Transform = boneRelativeTransform
			};
			bone2D.Rest = bone2D.Transform;
			bone2D.SetAutocalculateLengthAndAngle(autoCalculate: false);
			bone2D.SetLength(32f);
			bone2D.SetBoneAngle(0f);
			bone2D.SetMeta("_xw_bone_source_path", currentSceneInstance.GetPathTo(item).ToString());
			node.AddChild(bone2D, forceReadableName: false, InternalMode.Disabled);
			dictionary[item] = bone2D;
		}
		foreach (Bone2D value in dictionary.Values)
		{
			UpdateGeneratedBoneGizmo(value);
		}
		if ((XWEditorInterface.Instance?.GetSceneTreeDock())?.AddNodeToParentWithHistory(currentSceneInstance, skeleton2D2, "从节点生成 2D 骨骼") != skeleton2D2)
		{
			if (GodotObject.IsInstanceValid(skeleton2D2) && skeleton2D2.GetParent() == null)
			{
				skeleton2D2.Free();
			}
			XWEditorInterface.Instance?.ShowToast("无法把生成骨骼加入当前场景");
			return null;
		}
		XWEditorInterface.Instance?.ShowToast($"已生成 {dictionary.Count} 根 2D 骨骼");
		QueueViewportRedraw();
		return skeleton2D2;
	}

	public bool ClearGeneratedSkeletonWithHistory()
	{
		Node currentSceneInstance = CurrentSceneInstance;
		if (currentSceneInstance == null || !GodotObject.IsInstanceValid(currentSceneInstance))
		{
			return false;
		}
		Skeleton2D skeleton2D = FindSelectedGeneratedSkeleton() ?? FindGeneratedSkeleton(currentSceneInstance);
		if (!GodotObject.IsInstanceValid(skeleton2D))
		{
			XWEditorInterface.Instance?.ShowToast("当前场景没有可清除的生成骨骼");
			return false;
		}
		XWEditorInterface instance = XWEditorInterface.Instance;
		int num;
		if (instance == null)
		{
			num = 0;
		}
		else
		{
			num = ((instance.GetSceneTreeDock()?.RemoveNodeWithHistory(skeleton2D, "清除 2D 骨骼") == true) ? 1 : 0);
			if (num != 0)
			{
				XWEditorInterface.Instance?.ShowToast("已清除生成的 2D 骨骼");
				QueueViewportRedraw();
			}
		}
		return (byte)num != 0;
	}

	private List<Node2D> CollectBoneSourceNodes(Node sceneRoot)
	{
		List<Node2D> list = new List<Node2D>();
		HashSet<Node2D> seen = new HashSet<Node2D>();
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			AddBoneSourceNode(sceneRoot, item, list, seen);
		}
		if (list.Count == 0)
		{
			AddBoneSourceNode(sceneRoot, XWEditorInterface.Instance?.GetSceneTreeDock()?.GetSelectedNode(), list, seen);
		}
		return list;
	}

	private static void AddBoneSourceNode(Node sceneRoot, Node candidate, List<Node2D> result, HashSet<Node2D> seen)
	{
		Node2D node2D = candidate as Node2D;
		bool flag = node2D == null;
		if (!flag)
		{
			bool flag2 = ((candidate is Bone2D || candidate is Skeleton2D) ? true : false);
			flag = flag2;
		}
		if (!flag && GodotObject.IsInstanceValid(candidate) && candidate != sceneRoot && (sceneRoot.IsAncestorOf(candidate) || sceneRoot == candidate) && seen.Add(node2D))
		{
			result.Add(node2D);
		}
	}

	private static Node2D FindNearestBoneSourceAncestor(Node2D source, System.Collections.Generic.Dictionary<Node2D, Bone2D> sourceToBone)
	{
		for (Node parent = source.GetParent(); parent != null; parent = parent.GetParent())
		{
			if (parent is Node2D node2D && sourceToBone.ContainsKey(node2D))
			{
				return node2D;
			}
		}
		return null;
	}

	private static Transform2D GetBoneRelativeTransform(Node sceneRoot, Node2D source, Node2D parentSource)
	{
		if (GodotObject.IsInstanceValid(parentSource))
		{
			return parentSource.GlobalTransform.AffineInverse() * source.GlobalTransform;
		}
		if (sceneRoot is CanvasItem canvasItem)
		{
			return canvasItem.GetGlobalTransform().AffineInverse() * source.GlobalTransform;
		}
		return source.Transform;
	}

	private static void UpdateGeneratedBoneGizmo(Bone2D bone)
	{
		Bone2D bone2D = null;
		foreach (Node child in bone.GetChildren())
		{
			if (child is Bone2D bone2D2)
			{
				bone2D = bone2D2;
				break;
			}
		}
		Vector2 vector = bone2D?.Position ?? new Vector2(32f, 0f);
		float length = Mathf.Max(vector.Length(), 16f);
		bone.SetLength(length);
		bone.SetBoneAngle((vector.LengthSquared() > 0.001f) ? vector.Angle() : 0f);
		bone.Rest = bone.Transform;
	}

	private Skeleton2D FindSelectedGeneratedSkeleton()
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			for (Node node = item; node != null; node = node.GetParent())
			{
				if (node is Skeleton2D skeleton2D && IsGeneratedSkeleton(skeleton2D))
				{
					return skeleton2D;
				}
			}
		}
		return null;
	}

	private static Skeleton2D FindGeneratedSkeleton(Node node)
	{
		if (node is Skeleton2D skeleton2D && IsGeneratedSkeleton(skeleton2D))
		{
			return skeleton2D;
		}
		foreach (Node child in node.GetChildren())
		{
			Skeleton2D skeleton2D2 = FindGeneratedSkeleton(child);
			if (GodotObject.IsInstanceValid(skeleton2D2))
			{
				return skeleton2D2;
			}
		}
		return null;
	}

	private static bool IsGeneratedSkeleton(Skeleton2D skeleton)
	{
		if (GodotObject.IsInstanceValid(skeleton) && skeleton.HasMeta("_xw_generated_skeleton_2d"))
		{
			return skeleton.GetMeta("_xw_generated_skeleton_2d").AsBool();
		}
		return false;
	}

	private static int GetNodeDepth(Node node)
	{
		int num = 0;
		for (Node node2 = node?.GetParent(); node2 != null; node2 = node2.GetParent())
		{
			num++;
		}
		return num;
	}

	private static StringName BuildUniqueChildName(Node parent, string preferredName)
	{
		string text = preferredName;
		int num = 2;
		while (parent.HasNode(new NodePath(text)))
		{
			text = $"{preferredName}{num++}";
		}
		return new StringName(text);
	}

	private bool SetSelectionMetaWithHistory(string metaName, bool enabled)
	{
		return CommitSelectionMetaHistory(metaName, enabled);
	}

	private void UpdateSelectionActionButtons()
	{
		int valueOrDefault = (XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedCount()).GetValueOrDefault();
		_lockButton.Disabled = valueOrDefault == 0;
		_unlockButton.Disabled = valueOrDefault == 0;
		_groupButton.Disabled = valueOrDefault == 0;
		_ungroupButton.Disabled = valueOrDefault == 0;
		_centerViewButton.Disabled = valueOrDefault == 0;
	}

	private void UpdateSnapButtons()
	{
		_smartSnapButton.SetPressedNoSignal(_viewport.SmartSnapActive);
		_gridSnapButton.SetPressedNoSignal(_viewport.GridSnapActive);
	}

	private void SyncToolbarFromViewportState()
	{
		_localSpaceButton?.SetPressedNoSignal(_viewport.UseLocalSpace);
		UpdateSnapButtons();
		UpdateSnapMenuChecks();
		UpdateViewMenuChecks();
		UpdateZoomLabel();
	}

	private void UpdateSnapMenuChecks()
	{
		PopupMenu popup = _snapConfigMenu.GetPopup();
		SetMenuChecked(popup, 1, _viewport.SmartSnapActive);
		SetMenuChecked(popup, 2, _viewport.GridSnapActive);
		SetMenuChecked(popup, 3, _viewport.GridMode == XW2DViewport.GridVisibility.Show);
		SetMenuChecked(popup, 4, _viewport.GridMode == XW2DViewport.GridVisibility.ShowWhenSnapping);
		SetMenuChecked(popup, 5, _viewport.GridMode == XW2DViewport.GridVisibility.Hide);
	}

	private void UpdateViewMenuChecks()
	{
		PopupMenu popup = _viewMenu.GetPopup();
		SetMenuChecked(popup, 10, _viewport.ShowOrigin);
		SetMenuChecked(popup, 11, _viewport.ShowViewportRect);
		SetMenuChecked(popup, 12, _viewport.ShowRulers);
		SetMenuChecked(popup, 13, _viewport.ShowGuides);
		SetMenuChecked(popup, 14, _viewport.ShowHelpers);
	}

	private void UpdateViewportContextMenuState()
	{
		XWSceneTreeDock obj = XWEditorInterface.Instance?.GetSceneTreeDock();
		Node node = obj?.GetSelectedNode();
		bool flag = CurrentSceneInstance != null && GodotObject.IsInstanceValid(CurrentSceneInstance);
		bool flag2 = node != null && GodotObject.IsInstanceValid(node);
		bool flag3 = obj?.IsSelectedNodeRoot() ?? false;
		XWEditorInterface instance = XWEditorInterface.Instance;
		bool flag4 = instance != null && instance.GetEditorSelection()?.GetSelectedCount() > 0;
		bool flag5 = obj?.SelectedNodeHasScript() ?? false;
		SetMenuDisabled(_viewportContextMenu, 1, !flag);
		SetMenuDisabled(_viewportContextMenu, 2, !flag);
		SetMenuDisabled(_viewportContextMenu, 3, !flag);
		SetMenuDisabled(_viewportContextMenu, 10, !flag2);
		SetMenuDisabled(_viewportContextMenu, 11, !flag2 || flag3);
		SetMenuDisabled(_viewportContextMenu, 12, !flag2 || flag3);
		SetMenuDisabled(_viewportContextMenu, 13, !flag2 || flag3);
		SetMenuDisabled(_viewportContextMenu, 14, !flag2 || flag3);
		SetMenuDisabled(_viewportContextMenu, 15, !flag4);
		SetMenuDisabled(_viewportContextMenu, 16, !flag4);
		SetMenuDisabled(_viewportContextMenu, 17, !flag4);
		SetMenuDisabled(_viewportContextMenu, 18, !flag4);
		SetMenuDisabled(_viewportContextMenu, 19, !flag2);
		SetMenuDisabled(_viewportContextMenu, 20, !flag2 || flag3);
		SetMenuDisabled(_viewportContextMenu, 21, !flag2);
		SetMenuDisabled(_viewportContextMenu, 30, !flag2 || flag5);
		SetMenuDisabled(_viewportContextMenu, 31, !flag2 || !flag5);
		SetMenuDisabled(_viewportContextMenu, 32, !flag2 || !flag5);
		SetMenuDisabled(_viewportContextMenu, 40, !flag4);
		SetMenuDisabled(_viewportContextMenu, 41, !flag4);
		SetMenuChecked(_viewportContextMenu, 50, _viewport.ShowOrigin);
		SetMenuChecked(_viewportContextMenu, 51, _viewport.ShowViewportRect);
		SetMenuChecked(_viewportContextMenu, 52, _viewport.ShowRulers);
		SetMenuChecked(_viewportContextMenu, 53, _viewport.ShowGuides);
		SetMenuChecked(_viewportContextMenu, 54, _viewport.ShowHelpers);
	}

	private void UpdateZoomLabel()
	{
		_zoomLabel.Text = $"{Mathf.RoundToInt(_viewport.Zoom * 100f)}%";
	}

	private void UpdateMousePositionLabel(Vector2 position)
	{
		_mousePositionLabel.Text = $"{Mathf.RoundToInt(position.X)}, {Mathf.RoundToInt(position.Y)}";
	}

	private static string GetSceneKey(PackedScene scene)
	{
		if (string.IsNullOrWhiteSpace(scene.ResourcePath))
		{
			return scene.GetInstanceId().ToString();
		}
		return scene.ResourcePath;
	}

	private static string GetSceneDisplayName(PackedScene scene)
	{
		if (!string.IsNullOrWhiteSpace(scene.ResourcePath))
		{
			return scene.ResourcePath.GetFile().GetBaseName();
		}
		if (!string.IsNullOrWhiteSpace(scene.ResourceName))
		{
			return scene.ResourceName;
		}
		return "未命名";
	}

	private static void SetToolButtonPressed(Button button, bool pressed)
	{
		button?.SetPressedNoSignal(pressed);
	}

	private static void SetMenuChecked(PopupMenu popup, int itemId, bool checkedState)
	{
		int itemIndex = popup.GetItemIndex(itemId);
		if (itemIndex >= 0)
		{
			popup.SetItemChecked(itemIndex, checkedState);
		}
	}

	private static void SetMenuDisabled(PopupMenu popup, int itemId, bool disabled)
	{
		int itemIndex = popup.GetItemIndex(itemId);
		if (itemIndex >= 0)
		{
			popup.SetItemDisabled(itemIndex, disabled);
		}
	}

	private static Texture2D LoadIcon(string path)
	{
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return XWTextureSafety.SafeIcon(ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse));
	}

	private static void FreeSceneInstance(Node sceneInstance)
	{
		if (sceneInstance != null && GodotObject.IsInstanceValid(sceneInstance))
		{
			sceneInstance.GetParent()?.RemoveChild(sceneInstance);
			sceneInstance.QueueFree();
		}
	}

	private void InitializeSceneGridSettings()
	{
		_gridSettingsPopup = GetNodeOrNull<PopupPanel>("%GridSettingsPopup") ?? (FindChild("GridSettingsPopup", recursive: true, owned: false) as PopupPanel);
		_gridSettingsPanel = GetNodeOrNull<XW2DGridSettingsPanel>("%GridSettingsPanel") ?? (FindChild("GridSettingsPanel", recursive: true, owned: false) as XW2DGridSettingsPanel);
		if (!GodotObject.IsInstanceValid(_gridSettingsPopup) || !GodotObject.IsInstanceValid(_gridSettingsPanel))
		{
			GD.PushError("2D 编辑器缺少可视化网格与吸附配置面板。");
			return;
		}
		LoadPersistedGridSettings();
		_gridSettingsSaveTimer = new Timer
		{
			Name = "GridSettingsSaveDebounce",
			OneShot = true,
			WaitTime = 0.35,
			ProcessCallback = Timer.TimerProcessCallback.Idle
		};
		AddChild(_gridSettingsSaveTimer, forceReadableName: false, InternalMode.Disabled);
		_gridSettingsSaveTimer.Timeout += FlushGridSettingsSave;
		_gridSettingsPanel.SettingsChanged += OnGridSettingsChanged;
		SyncGridSettingsPanelFromViewport();
	}

	private void ShutdownSceneGridSettings()
	{
		if (GodotObject.IsInstanceValid(_gridSettingsPanel))
		{
			_gridSettingsPanel.SettingsChanged -= OnGridSettingsChanged;
		}
		if (GodotObject.IsInstanceValid(_gridSettingsSaveTimer))
		{
			_gridSettingsSaveTimer.Timeout -= FlushGridSettingsSave;
			if (!_gridSettingsSaveTimer.IsStopped())
			{
				FlushGridSettingsSave();
			}
		}
	}

	private void OpenGridSettingsPanel()
	{
		if (GodotObject.IsInstanceValid(_gridSettingsPopup) && GodotObject.IsInstanceValid(_gridSettingsPanel))
		{
			SyncGridSettingsPanelFromViewport();
			_gridSettingsPopup.PopupCentered(new Vector2I(900, 720));
		}
	}

	private void SyncGridSettingsPanelFromViewport()
	{
		if (GodotObject.IsInstanceValid(_gridSettingsPanel) && GodotObject.IsInstanceValid(_viewport))
		{
			_gridSettingsPanel.BindState(BuildGridPanelState());
		}
	}

	private Dictionary BuildGridPanelState()
	{
		return new Dictionary
		{
			["grid_offset"] = _viewport?.GridOffset ?? Vector2.Zero,
			["grid_step"] = _viewport?.GridStep ?? new Vector2(8f, 8f),
			["primary_grid_step"] = _viewport?.PrimaryGridStep ?? new Vector2I(8, 8),
			["move_snap_step"] = _viewport?.MoveSnapStep ?? new Vector2(8f, 8f),
			["rotate_snap_step"] = _viewport?.RotationSnapStepDegrees ?? 15f,
			["scale_snap_step"] = _viewport?.ScaleSnapStep ?? 0.1f,
			["smart_snap_threshold"] = _viewport?.SmartSnapThreshold ?? 8f
		};
	}

	private void OnGridSettingsChanged()
	{
		if (GodotObject.IsInstanceValid(_gridSettingsPanel) && GodotObject.IsInstanceValid(_viewport))
		{
			_viewport.GridOffset = _gridSettingsPanel.GridOffset;
			_viewport.GridStep = _gridSettingsPanel.GridStep;
			_viewport.PrimaryGridStep = _gridSettingsPanel.PrimaryGridStep;
			_viewport.MoveSnapStep = _gridSettingsPanel.MoveSnapStep;
			_viewport.RotationSnapStepDegrees = _gridSettingsPanel.RotateSnapStep;
			_viewport.ScaleSnapStep = _gridSettingsPanel.ScaleSnapStep;
			_viewport.SmartSnapThreshold = _gridSettingsPanel.SmartSnapThreshold;
			_viewport.QueueRedrawAll();
			PersistGridSettings();
			SaveCurrentTabState();
		}
	}

	private void LoadPersistedGridSettings()
	{
		XWEditorSettings xWEditorSettings = XWEditorInterface.Instance?.GetEditorSettings();
		if (xWEditorSettings != null && GodotObject.IsInstanceValid(_viewport))
		{
			RegisterGridSettingDefaults(xWEditorSettings);
			_viewport.GridOffset = xWEditorSettings.GetSetting("2d_editor_grid/offset", Vector2.Zero);
			_viewport.GridStep = xWEditorSettings.GetSetting("2d_editor_grid/grid_step", new Vector2(8f, 8f));
			_viewport.PrimaryGridStep = xWEditorSettings.GetSetting("2d_editor_grid/primary_step", new Vector2I(8, 8));
			_viewport.MoveSnapStep = xWEditorSettings.GetSetting("2d_editor_grid/move_step", new Vector2(8f, 8f));
			_viewport.RotationSnapStepDegrees = xWEditorSettings.GetSetting("2d_editor_grid/rotation_degrees", 15f);
			_viewport.ScaleSnapStep = xWEditorSettings.GetSetting("2d_editor_grid/scale_step", 0.1f);
			_viewport.SmartSnapThreshold = xWEditorSettings.GetSetting("2d_editor_grid/smart_threshold", 8f);
		}
	}

	private void PersistGridSettings()
	{
		XWEditorSettings xWEditorSettings = XWEditorInterface.Instance?.GetEditorSettings();
		if (xWEditorSettings != null && GodotObject.IsInstanceValid(_viewport))
		{
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/offset", _viewport.GridOffset);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/grid_step", _viewport.GridStep);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/primary_step", _viewport.PrimaryGridStep);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/move_step", _viewport.MoveSnapStep);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/rotation_degrees", _viewport.RotationSnapStepDegrees);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/scale_step", _viewport.ScaleSnapStep);
			SetGridSettingIfChanged(xWEditorSettings, "2d_editor_grid/smart_threshold", _viewport.SmartSnapThreshold);
			if (GodotObject.IsInstanceValid(_gridSettingsSaveTimer))
			{
				_gridSettingsSaveTimer.Start();
			}
			else
			{
				xWEditorSettings.Save();
			}
		}
	}

	private static void SetGridSettingIfChanged(XWEditorSettings settings, string key, Variant value)
	{
		if (!settings.HasSetting(key) || !settings.GetSetting(key).Equals(value))
		{
			settings.SetSetting(key, value);
		}
	}

	private void FlushGridSettingsSave()
	{
		XWEditorInterface.Instance?.GetEditorSettings()?.Save();
		_gridSettingsSaveTimer?.Stop();
	}

	private static void RegisterGridSettingDefaults(XWEditorSettings settings)
	{
		settings.RegisterSetting("2d_editor_grid/offset", Vector2.Zero);
		settings.RegisterSetting("2d_editor_grid/grid_step", new Vector2(8f, 8f));
		settings.RegisterSetting("2d_editor_grid/primary_step", new Vector2I(8, 8));
		settings.RegisterSetting("2d_editor_grid/move_step", new Vector2(8f, 8f));
		settings.RegisterSetting("2d_editor_grid/rotation_degrees", 15f);
		settings.RegisterSetting("2d_editor_grid/scale_step", 0.1f);
		settings.RegisterSetting("2d_editor_grid/smart_threshold", 8f);
	}

	private void SeedSceneGridTabState(SceneTabInfo tabInfo)
	{
		if (tabInfo != null)
		{
			Dictionary dictionary = BuildGridPanelState();
			tabInfo.ViewportState = new Dictionary
			{
				["zoom"] = 1f,
				["ofs"] = Vector2.Zero,
				["grid_offset"] = dictionary["grid_offset"],
				["grid_step"] = dictionary["grid_step"],
				["primary_grid_step"] = dictionary["primary_grid_step"],
				["move_snap_step"] = dictionary["move_snap_step"],
				["rotation_snap_step_degrees"] = dictionary["rotate_snap_step"],
				["scale_snap_step"] = dictionary["scale_snap_step"],
				["smart_snap_threshold"] = dictionary["smart_snap_threshold"],
				["smart_snap_active"] = false,
				["grid_snap_active"] = false,
				["grid_visibility"] = 1,
				["show_origin"] = true,
				["show_viewport"] = true,
				["show_rulers"] = true,
				["show_guides"] = true,
				["show_helpers"] = false,
				["use_local_space"] = false,
				["vertical_guides"] = new Godot.Collections.Array(),
				["horizontal_guides"] = new Godot.Collections.Array()
			};
		}
	}

	private void InitializeInlineTextEditing()
	{
		if (GodotObject.IsInstanceValid(_viewport) && !GodotObject.IsInstanceValid(_inlineTextOverlay))
		{
			_inlineTextOverlay = new PanelContainer
			{
				Name = "InlineTextEditorOverlay",
				Visible = false,
				ZIndex = 500,
				MouseFilter = MouseFilterEnum.Stop,
				TooltipText = "直接覆盖在游戏文字位置；输入时即时预览，提交后进入当前场景的撤销历史。"
			};
			_inlineTextOverlay.AddThemeStyleboxOverride("panel", CreateInlineTextOverlayStyle());
			_viewport.AddChild(_inlineTextOverlay, forceReadableName: false, InternalMode.Disabled);
			MarginContainer marginContainer = new MarginContainer
			{
				Name = "InlineTextEditorMargin",
				MouseFilter = MouseFilterEnum.Pass
			};
			marginContainer.AddThemeConstantOverride("margin_left", 8);
			marginContainer.AddThemeConstantOverride("margin_top", 6);
			marginContainer.AddThemeConstantOverride("margin_right", 8);
			marginContainer.AddThemeConstantOverride("margin_bottom", 7);
			_inlineTextOverlay.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "InlineTextEditorStack",
				MouseFilter = MouseFilterEnum.Pass
			};
			vBoxContainer.AddThemeConstantOverride("separation", 5);
			marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = "InlineTextEditorHeader",
				MouseFilter = MouseFilterEnum.Pass
			};
			hBoxContainer.AddThemeConstantOverride("separation", 5);
			vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
			TextureRect node = new TextureRect
			{
				Name = "InlineTextEditorIcon",
				Texture = LoadInlineTextIcon("Edit"),
				CustomMinimumSize = new Vector2(18f, 18f),
				ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
				StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
				MouseFilter = MouseFilterEnum.Ignore,
				Modulate = ModEditorTheme.SunGoldColor
			};
			hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_inlineTextModeLabel = new Label
			{
				Name = "InlineTextEditorMode",
				Text = "画面文字 · 即时预览",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				VerticalAlignment = VerticalAlignment.Center,
				ClipText = true,
				TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
				MouseFilter = MouseFilterEnum.Ignore
			};
			_inlineTextModeLabel.AddThemeColorOverride("font_color", ModEditorTheme.SunGoldColor);
			_inlineTextModeLabel.AddThemeFontOverride("font", ModEditorTheme.BoldFont);
			_inlineTextModeLabel.AddThemeFontSizeOverride("font_size", 12);
			hBoxContainer.AddChild(_inlineTextModeLabel, forceReadableName: false, InternalMode.Disabled);
			_inlineTextCancelButton = CreateInlineTextHeaderButton("InlineTextCancelButton", "Close", "取消本次文字修改（Esc）");
			_inlineTextCommitButton = CreateInlineTextHeaderButton("InlineTextCommitButton", "Save", "完成并写入一次撤销历史（Enter）");
			hBoxContainer.AddChild(_inlineTextCancelButton, forceReadableName: false, InternalMode.Disabled);
			hBoxContainer.AddChild(_inlineTextCommitButton, forceReadableName: false, InternalMode.Disabled);
			_inlineTextLineEdit = new LineEdit
			{
				Name = "InlineTextLineEdit",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0f, 34f),
				SelectAllOnFocus = false,
				PlaceholderText = "在画面中输入文字"
			};
			vBoxContainer.AddChild(_inlineTextLineEdit, forceReadableName: false, InternalMode.Disabled);
			_inlineTextMultilineEdit = new TextEdit
			{
				Name = "InlineTextMultilineEdit",
				Visible = false,
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				SizeFlagsVertical = SizeFlags.ExpandFill,
				CustomMinimumSize = new Vector2(0f, 76f),
				PlaceholderText = "在画面中输入多行文字\nCtrl+Enter 完成"
			};
			vBoxContainer.AddChild(_inlineTextMultilineEdit, forceReadableName: false, InternalMode.Disabled);
			_inlineTextLineEdit.TextChanged += OnInlineTextLineChanged;
			_inlineTextLineEdit.TextSubmitted += delegate
			{
				CommitInlineTextEdit();
			};
			_inlineTextLineEdit.FocusExited += QueueInlineTextFocusExitCommit;
			_inlineTextLineEdit.GuiInput += OnInlineTextEditorGuiInput;
			_inlineTextMultilineEdit.TextChanged += OnInlineTextMultilineChanged;
			_inlineTextMultilineEdit.FocusExited += QueueInlineTextFocusExitCommit;
			_inlineTextMultilineEdit.GuiInput += OnInlineTextEditorGuiInput;
			_inlineTextCancelButton.Pressed += CancelInlineTextEdit;
			_inlineTextCommitButton.Pressed += delegate
			{
				CommitInlineTextEdit();
			};
			base.VisibilityChanged += OnInlineTextHostVisibilityChanged;
			_viewport.VisibilityChanged += OnInlineTextViewportVisibilityChanged;
			_inlineTextOverlayMotion = XWUiMotion.BindPanel(_inlineTextOverlay);
			XWUiMotion.BindButton(_inlineTextCancelButton);
			XWUiMotion.BindButton(_inlineTextCommitButton);
		}
	}

	private void ShutdownInlineTextEditing()
	{
		CancelInlineTextEdit();
		base.VisibilityChanged -= OnInlineTextHostVisibilityChanged;
		if (GodotObject.IsInstanceValid(_viewport))
		{
			_viewport.VisibilityChanged -= OnInlineTextViewportVisibilityChanged;
		}
		if (GodotObject.IsInstanceValid(_inlineTextOverlay))
		{
			_inlineTextOverlay.QueueFree();
		}
		_inlineTextOverlay = null;
		_inlineTextModeLabel = null;
		_inlineTextLineEdit = null;
		_inlineTextMultilineEdit = null;
		_inlineTextCommitButton = null;
		_inlineTextCancelButton = null;
		_inlineTextOverlayMotion = null;
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (!InlineTextEditorVisible || !(inputEvent is InputEventKey { Pressed: not false, Echo: false } inputEventKey))
		{
			return;
		}
		Control obj = GetViewport()?.GuiGetFocusOwner();
		Control control = (_inlineTextMultiline ? ((Control)_inlineTextMultilineEdit) : ((Control)_inlineTextLineEdit));
		if (obj != control)
		{
			return;
		}
		if (inputEventKey.Keycode == Key.Escape)
		{
			if (!CancelPendingInlineTextIme())
			{
				CancelInlineTextEdit();
			}
			GetViewport()?.SetInputAsHandled();
			return;
		}
		Key keycode = inputEventKey.Keycode;
		bool flag = (((ulong)(keycode - 4194309) <= 1uL) ? true : false);
		if (flag && (!_inlineTextMultiline || inputEventKey.CtrlPressed || inputEventKey.MetaPressed))
		{
			CommitInlineTextEdit();
			GetViewport()?.SetInputAsHandled();
		}
	}

	public bool CanInlineEditText(Node candidate)
	{
		if (!GodotObject.IsInstanceValid(candidate))
		{
			return false;
		}
		ulong instanceId = candidate.GetInstanceId();
		if (_inlineTextCapabilityCache.TryGetValue(instanceId, out var value))
		{
			return value;
		}
		Node target;
		StringName property;
		Variant.Type valueType;
		PropertyHint hint;
		string text;
		bool multiline;
		bool flag = TryResolveInlineTextTarget(candidate, out target, out property, out valueType, out hint, out text, out multiline);
		_inlineTextCapabilityCache[instanceId] = flag;
		return flag;
	}

	public void ClearInlineTextCapabilityCache()
	{
		_inlineTextCapabilityCache.Clear();
	}

	public bool BeginInlineTextEdit(Node candidate)
	{
		if (_openTabs.TryGetValue(_currentTabKey, out var value) && value.ScriptlessPreview)
		{
			XWEditorInterface.Instance?.ShowToast("当前是外部 C# 场景的无脚本预览；请先完成后台编译，再直接编辑画面文字。");
			return false;
		}
		if (!TryResolveInlineTextTarget(candidate, out var target, out var property, out var valueType, out var hint, out var text, out var multiline))
		{
			return false;
		}
		if (InlineTextEditorVisible && _inlineTextTarget == target && _inlineTextProperty == property)
		{
			RefreshInlineTextEditorGeometry();
			QueueInlineTextEditorFocus(selectAll: false);
			return true;
		}
		CommitInlineTextEdit();
		_inlineTextTarget = target;
		_inlineTextProperty = property;
		_inlineTextValueType = valueType;
		_inlineTextHint = hint;
		_inlineTextBefore = text;
		_inlineTextHistoryId = GetCurrentSceneHistoryId();
		_inlineTextMultiline = multiline;
		_inlineTextSessionToken++;
		_inlineTextApplyingPreview = true;
		_inlineTextLineEdit.Text = text;
		_inlineTextMultilineEdit.Text = text;
		_inlineTextApplyingPreview = false;
		_inlineTextLineEdit.Visible = !multiline;
		_inlineTextMultilineEdit.Visible = multiline;
		ConfigureInlineTextInputForTarget(target, property, hint);
		_inlineTextModeLabel.Text = BuildInlineTextModeLabel(target, property, multiline);
		_inlineTextOverlay.Visible = true;
		if (_inlineTextOverlay.GetThemeStylebox("panel") is StyleBoxFlat styleBoxFlat)
		{
			styleBoxFlat.ShadowSize = ((!XWUiMotion.LowPerformanceMode) ? 8 : 0);
		}
		XWEditorInterface.Instance?.SetSelectedNode(target);
		XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(target, emitSignal: false);
		RefreshInlineTextEditorGeometry();
		QueueInlineTextEditorFocus(selectAll: true);
		_inlineTextOverlayMotion?.PlayPanelReveal(0f);
		_viewport?.QueueEditorOverlayRedraw();
		return true;
	}

	public void SetInlineTextDraft(string text)
	{
		if (InlineTextEditorVisible)
		{
			string text2 = text ?? "";
			_inlineTextApplyingPreview = true;
			if (_inlineTextMultiline)
			{
				_inlineTextMultilineEdit.Text = text2;
			}
			else
			{
				_inlineTextLineEdit.Text = text2;
			}
			_inlineTextApplyingPreview = false;
			ApplyInlineTextPreview(text2);
		}
	}

	public bool CommitPendingAuthoringInput()
	{
		return CommitInlineTextEdit();
	}

	public bool CommitInlineTextEdit()
	{
		if (!InlineTextEditorVisible || !GodotObject.IsInstanceValid(_inlineTextTarget))
		{
			HideInlineTextEditor();
			return false;
		}
		ApplyPendingInlineTextIme();
		Node from = _inlineTextTarget;
		StringName inlineTextProperty = _inlineTextProperty;
		Variant.Type inlineTextValueType = _inlineTextValueType;
		string inlineTextBefore = _inlineTextBefore;
		string inlineTextDraft = InlineTextDraft;
		int inlineTextHistoryId = _inlineTextHistoryId;
		ApplyInlineTextValue(from, inlineTextProperty, inlineTextValueType, inlineTextDraft);
		HideInlineTextEditor();
		if (string.Equals(inlineTextBefore, inlineTextDraft, StringComparison.Ordinal))
		{
			NotifyInlineTextHistoryApplied(from);
			return false;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			InlineTextCommitCount++;
			NotifyInlineTextHistoryApplied(from);
			return true;
		}
		xWUndoRedoManager.CreateAction($"编辑画面文字 · {from.Name}", mergeMode: false, inlineTextHistoryId);
		xWUndoRedoManager.AddUndoMethod(this, "NotifyInlineTextHistoryApplied", Variant.From(in from));
		xWUndoRedoManager.AddDoProperty(from, inlineTextProperty, MakeInlineTextVariant(inlineTextValueType, inlineTextDraft));
		xWUndoRedoManager.AddUndoProperty(from, inlineTextProperty, MakeInlineTextVariant(inlineTextValueType, inlineTextBefore));
		xWUndoRedoManager.AddDoMethod(this, "NotifyInlineTextHistoryApplied", Variant.From(in from));
		xWUndoRedoManager.CommitAction();
		InlineTextCommitCount++;
		_viewport?.ActivateSceneTransformHistoryDeferred();
		return true;
	}

	public void CancelInlineTextEdit()
	{
		if (!InlineTextEditorVisible)
		{
			HideInlineTextEditor();
			return;
		}
		Node inlineTextTarget = _inlineTextTarget;
		if (GodotObject.IsInstanceValid(inlineTextTarget))
		{
			ApplyInlineTextValue(inlineTextTarget, _inlineTextProperty, _inlineTextValueType, _inlineTextBefore);
		}
		InlineTextCancelCount++;
		HideInlineTextEditor();
		if (GodotObject.IsInstanceValid(inlineTextTarget))
		{
			NotifyInlineTextHistoryApplied(inlineTextTarget);
		}
	}

	public void RefreshInlineTextEditorGeometry()
	{
		_inlineTextGeometryRefreshQueued = false;
		if (InlineTextEditorVisible && GodotObject.IsInstanceValid(_viewport) && _inlineTextTarget is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
		{
			Rect2 canvasItemViewportBounds = _viewport.GetCanvasItemViewportBounds(canvasItem);
			Vector2 size = _viewport.Size;
			float num = (_inlineTextMultiline ? Mathf.Max(82f, canvasItemViewportBounds.Size.Y) : Mathf.Max(36f, canvasItemViewportBounds.Size.Y));
			Vector2 vector = new Vector2(Mathf.Clamp(Mathf.Max(190f, canvasItemViewportBounds.Size.X + 16f), 190f, Mathf.Max(190f, size.X - 12f)), Mathf.Clamp(num + 38f, 70f, Mathf.Max(70f, size.Y - 12f)));
			Vector2 vector2 = canvasItemViewportBounds.Position + new Vector2(-8f, -36f);
			vector2.X = Mathf.Clamp(vector2.X, 6f, Mathf.Max(6f, size.X - vector.X - 6f));
			vector2.Y = Mathf.Clamp(vector2.Y, 6f, Mathf.Max(6f, size.Y - vector.Y - 6f));
			_inlineTextOverlay.Position = vector2.Round();
			_inlineTextOverlay.Size = vector.Round();
		}
	}

	public void NotifyInlineTextHistoryApplied(Node target)
	{
		if (GodotObject.IsInstanceValid(target))
		{
			XWEditorInterface.Instance?.SetSelectedNode(target);
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(target, emitSignal: false);
		}
		MarkCurrentSceneHistorySurfaceDirty();
		_nodeDirectPropertySurface?.RefreshValues();
		if (target is CanvasItem canvasItem)
		{
			canvasItem.QueueRedraw();
		}
		_viewport?.QueueEditorOverlayRedraw();
	}

	private bool CommitInlineTextEditForScene(Node sceneRoot)
	{
		if (!InlineTextEditorVisible || !GodotObject.IsInstanceValid(_inlineTextTarget))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(sceneRoot) || (sceneRoot != _inlineTextTarget && !sceneRoot.IsAncestorOf(_inlineTextTarget)))
		{
			return false;
		}
		return CommitInlineTextEdit();
	}

	private void OnInlineTextLineChanged(string text)
	{
		if (!_inlineTextApplyingPreview)
		{
			ApplyInlineTextPreview(text);
		}
	}

	private void OnInlineTextMultilineChanged()
	{
		if (!_inlineTextApplyingPreview)
		{
			ApplyInlineTextPreview(_inlineTextMultilineEdit.Text);
		}
	}

	private void ApplyInlineTextPreview(string text)
	{
		if (InlineTextEditorVisible && GodotObject.IsInstanceValid(_inlineTextTarget))
		{
			ApplyInlineTextValue(_inlineTextTarget, _inlineTextProperty, _inlineTextValueType, text ?? "");
			InlineTextPreviewCount++;
			QueueInlineTextGeometryRefresh();
		}
	}

	private static void ApplyInlineTextValue(Node target, StringName property, Variant.Type valueType, string text)
	{
		if (GodotObject.IsInstanceValid(target))
		{
			target.Set(property, MakeInlineTextVariant(valueType, text));
			if (target is CanvasItem canvasItem)
			{
				canvasItem.QueueRedraw();
			}
		}
	}

	private static Variant MakeInlineTextVariant(Variant.Type valueType, string text)
	{
		string from = text ?? "";
		if (valueType != Variant.Type.StringName)
		{
			return Variant.From(in from);
		}
		return Variant.From<StringName>(new StringName(from));
	}

	private void QueueInlineTextFocusExitCommit()
	{
		int token = _inlineTextSessionToken;
		Callable.From(delegate
		{
			if (token == _inlineTextSessionToken && InlineTextEditorVisible)
			{
				CommitInlineTextEdit();
			}
		}).CallDeferred();
	}

	private void OnInlineTextEditorGuiInput(InputEvent inputEvent)
	{
		if (!(inputEvent is InputEventKey { Pressed: not false, Echo: false } inputEventKey))
		{
			return;
		}
		if (inputEventKey.Keycode == Key.Escape)
		{
			if (CancelPendingInlineTextIme())
			{
				GetViewport()?.SetInputAsHandled();
				return;
			}
			CancelInlineTextEdit();
			GetViewport()?.SetInputAsHandled();
			return;
		}
		bool flag = !_inlineTextMultiline;
		if (flag)
		{
			Key keycode = inputEventKey.Keycode;
			bool flag2 = (((ulong)(keycode - 4194309) <= 1uL) ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			CommitInlineTextEdit();
			GetViewport()?.SetInputAsHandled();
			return;
		}
		flag = _inlineTextMultiline;
		if (flag)
		{
			Key keycode = inputEventKey.Keycode;
			bool flag2 = (((ulong)(keycode - 4194309) <= 1uL) ? true : false);
			flag = flag2;
		}
		if (flag && (inputEventKey.CtrlPressed || inputEventKey.MetaPressed))
		{
			CommitInlineTextEdit();
			GetViewport()?.SetInputAsHandled();
		}
	}

	private void OnInlineTextHostVisibilityChanged()
	{
		if (!IsVisibleInTree() && InlineTextEditorVisible)
		{
			CommitInlineTextEdit();
		}
	}

	private void OnInlineTextViewportVisibilityChanged()
	{
		if (GodotObject.IsInstanceValid(_viewport) && !_viewport.IsVisibleInTree() && InlineTextEditorVisible)
		{
			CommitInlineTextEdit();
		}
	}

	private void QueueInlineTextGeometryRefresh()
	{
		if (!_inlineTextGeometryRefreshQueued && InlineTextEditorVisible)
		{
			_inlineTextGeometryRefreshQueued = true;
			Callable.From(delegate
			{
				_inlineTextGeometryRefreshQueued = false;
				RefreshInlineTextEditorGeometry();
			}).CallDeferred();
		}
	}

	private void ConfigureInlineTextInputForTarget(Node target, StringName property, PropertyHint hint)
	{
		LineEdit lineEdit = target as LineEdit;
		bool flag = hint == PropertyHint.Password || (GodotObject.IsInstanceValid(lineEdit) && lineEdit.Secret && string.Equals(property.ToString(), "text", StringComparison.OrdinalIgnoreCase));
		_inlineTextLineEdit.Secret = flag;
		_inlineTextLineEdit.SecretCharacter = "•";
		if (flag && GodotObject.IsInstanceValid(lineEdit))
		{
			_inlineTextLineEdit.SecretCharacter = lineEdit.SecretCharacter;
		}
		Control control = (_inlineTextMultiline ? ((Control)_inlineTextMultilineEdit) : ((Control)_inlineTextLineEdit));
		if (GodotObject.IsInstanceValid(control) && target is Control control2)
		{
			control.RemoveThemeFontOverride("font");
			control.RemoveThemeFontSizeOverride("font_size");
			string text = ((target is RichTextLabel) ? "normal_font" : "font");
			string text2 = ((target is RichTextLabel) ? "normal_font_size" : "font_size");
			Font themeFont = control2.GetThemeFont(text);
			if (GodotObject.IsInstanceValid(themeFont))
			{
				control.AddThemeFontOverride("font", themeFont);
			}
			int fontSize = Mathf.Clamp(Mathf.RoundToInt((float)control2.GetThemeFontSize(text2) * Mathf.Max(_viewport?.Zoom ?? 1f, 0.1f)), 14, 28);
			control.AddThemeFontSizeOverride("font_size", fontSize);
			if (!_inlineTextMultiline)
			{
				LineEdit inlineTextLineEdit = _inlineTextLineEdit;
				HorizontalAlignment alignment = ((target is Label label) ? label.HorizontalAlignment : ((target is Button button) ? button.Alignment : ((!(target is LineEdit lineEdit2)) ? HorizontalAlignment.Left : lineEdit2.Alignment)));
				inlineTextLineEdit.Alignment = alignment;
			}
		}
	}

	private void ApplyPendingInlineTextIme()
	{
		if (_inlineTextMultiline)
		{
			if (GodotObject.IsInstanceValid(_inlineTextMultilineEdit) && _inlineTextMultilineEdit.HasImeText())
			{
				_inlineTextMultilineEdit.ApplyIme();
			}
		}
		else if (GodotObject.IsInstanceValid(_inlineTextLineEdit) && _inlineTextLineEdit.HasImeText())
		{
			_inlineTextLineEdit.ApplyIme();
		}
	}

	private bool CancelPendingInlineTextIme()
	{
		if (_inlineTextMultiline)
		{
			if (!GodotObject.IsInstanceValid(_inlineTextMultilineEdit) || !_inlineTextMultilineEdit.HasImeText())
			{
				return false;
			}
			_inlineTextMultilineEdit.CancelIme();
			return true;
		}
		if (!GodotObject.IsInstanceValid(_inlineTextLineEdit) || !_inlineTextLineEdit.HasImeText())
		{
			return false;
		}
		_inlineTextLineEdit.CancelIme();
		return true;
	}

	private void GrabInlineTextEditorFocus(bool selectAll)
	{
		Control control = (_inlineTextMultiline ? ((Control)_inlineTextMultilineEdit) : ((Control)_inlineTextLineEdit));
		if (!GodotObject.IsInstanceValid(control))
		{
			return;
		}
		control.GrabFocus();
		if (selectAll)
		{
			if (control is LineEdit lineEdit)
			{
				lineEdit.SelectAll();
			}
			else if (control is TextEdit textEdit)
			{
				textEdit.SelectAll();
			}
		}
	}

	private void QueueInlineTextEditorFocus(bool selectAll)
	{
		int token = _inlineTextSessionToken;
		Callable.From(delegate
		{
			if (token == _inlineTextSessionToken && InlineTextEditorVisible)
			{
				GrabInlineTextEditorFocus(selectAll);
			}
		}).CallDeferred();
	}

	private void HideInlineTextEditor()
	{
		_inlineTextSessionToken++;
		if (GodotObject.IsInstanceValid(_inlineTextOverlay))
		{
			_inlineTextOverlay.Visible = false;
		}
		_inlineTextTarget = null;
		_inlineTextProperty = InlineTextPropertyFallback;
		_inlineTextValueType = Variant.Type.String;
		_inlineTextHint = PropertyHint.None;
		_inlineTextBefore = "";
		_inlineTextMultiline = false;
		_inlineTextApplyingPreview = false;
		_inlineTextGeometryRefreshQueued = false;
		if (GodotObject.IsInstanceValid(_viewport) && _viewport.IsInsideTree())
		{
			_viewport.GrabFocus();
		}
		_viewport?.QueueEditorOverlayRedraw();
	}

	private bool TryResolveInlineTextTarget(Node candidate, out Node target, out StringName property, out Variant.Type valueType, out PropertyHint hint, out string text, out bool multiline)
	{
		target = null;
		property = InlineTextPropertyFallback;
		valueType = Variant.Type.String;
		hint = PropertyHint.None;
		text = "";
		multiline = false;
		if (!GodotObject.IsInstanceValid(candidate) || !GodotObject.IsInstanceValid(CurrentSceneInstance))
		{
			return false;
		}
		Node node = candidate;
		while (GodotObject.IsInstanceValid(node))
		{
			if (node is CanvasItem && TryFindInlineTextProperty(node, out property, out valueType, out hint, out text))
			{
				target = node;
				bool flag = node is TextEdit || node is RichTextLabel || ((!(node is Label) && !(node is Button) && !(node is LinkButton) && !(node is LineEdit)) ? (hint == PropertyHint.MultilineText || text.Contains('\n')) : text.Contains('\n'));
				multiline = flag;
				return true;
			}
			if (node == CurrentSceneInstance)
			{
				break;
			}
			node = node.GetParent();
		}
		return false;
	}

	private static bool TryFindInlineTextProperty(Node node, out StringName property, out Variant.Type valueType, out PropertyHint hint, out string text)
	{
		property = InlineTextPropertyFallback;
		valueType = Variant.Type.String;
		hint = PropertyHint.None;
		text = "";
		if (node.HasMeta("_xw_inline_text_property"))
		{
			string requestedName = node.GetMeta("_xw_inline_text_property").AsString();
			if (TryReadInlineTextProperty(node, requestedName, out property, out valueType, out hint, out text))
			{
				return true;
			}
		}
		if (node is LineEdit lineEdit && string.IsNullOrEmpty(lineEdit.Text) && !string.IsNullOrEmpty(lineEdit.PlaceholderText) && TryReadInlineTextProperty(node, "placeholder_text", out property, out valueType, out hint, out text))
		{
			return true;
		}
		if (node is TextEdit textEdit && string.IsNullOrEmpty(textEdit.Text) && !string.IsNullOrEmpty(textEdit.PlaceholderText) && TryReadInlineTextProperty(node, "placeholder_text", out property, out valueType, out hint, out text))
		{
			return true;
		}
		string[] array = new string[10] { "text", "caption", "title", "content", "display_text", "button_text", "label_text", "dialogue", "description", "placeholder_text" };
		foreach (string requestedName2 in array)
		{
			if (TryReadInlineTextProperty(node, requestedName2, out property, out valueType, out hint, out text))
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryReadInlineTextProperty(Node node, string requestedName, out StringName property, out Variant.Type valueType, out PropertyHint hint, out string text)
	{
		property = InlineTextPropertyFallback;
		valueType = Variant.Type.String;
		hint = PropertyHint.None;
		text = "";
		if (string.IsNullOrWhiteSpace(requestedName) || string.Equals(requestedName, "name", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		bool flag;
		foreach (Dictionary property2 in node.GetPropertyList())
		{
			if (!property2.ContainsKey("name") || !property2.ContainsKey("type"))
			{
				continue;
			}
			string text2 = property2["name"].AsString();
			if (!string.Equals(text2, requestedName, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			Variant.Type type = (Variant.Type)property2["type"].AsInt32();
			flag = ((type == Variant.Type.String || type == Variant.Type.StringName) ? true : false);
			if (!flag)
			{
				flag = false;
			}
			else
			{
				PropertyUsageFlags propertyUsageFlags = (PropertyUsageFlags)(property2.ContainsKey("usage") ? property2["usage"].AsInt64() : 6);
				if ((propertyUsageFlags & PropertyUsageFlags.Editor) == PropertyUsageFlags.None || (propertyUsageFlags & PropertyUsageFlags.ReadOnly) != PropertyUsageFlags.None)
				{
					flag = false;
				}
				else
				{
					property = new StringName(text2);
					valueType = type;
					hint = (PropertyHint)(property2.ContainsKey("hint") ? property2["hint"].AsInt32() : 0);
					text = node.Get(property).AsString();
					flag = true;
				}
			}
			goto IL_018b;
		}
		return false;
		IL_018b:
		return flag;
	}

	private static string BuildInlineTextModeLabel(Node target, StringName property, bool multiline)
	{
		if (string.Equals(property.ToString(), "placeholder_text", StringComparison.OrdinalIgnoreCase))
		{
			return "输入占位文字 · 即时预览";
		}
		string text = ((target is RichTextLabel) ? "富文本" : ((target is TextEdit) ? "多行输入" : ((!(target is Button) && !(target is LinkButton)) ? ((target is LineEdit) ? "输入框文字" : ((!(target is Label)) ? (multiline ? "自定义多行文字" : "自定义画面文字") : (multiline ? "多行标签" : "标签文字"))) : "按钮文字")));
		return text + " · 即时预览";
	}

	private Rect2 GetInlineTextControlRectInViewport(Control control)
	{
		if (!GodotObject.IsInstanceValid(control) || !GodotObject.IsInstanceValid(_viewport))
		{
			return default(Rect2);
		}
		Transform2D globalTransformWithCanvas = _viewport.GetGlobalTransformWithCanvas();
		Transform2D globalTransformWithCanvas2 = control.GetGlobalTransformWithCanvas();
		if (Mathf.IsZeroApprox(globalTransformWithCanvas.Determinant()) || Mathf.IsZeroApprox(globalTransformWithCanvas2.Determinant()))
		{
			return default(Rect2);
		}
		Transform2D transform2D = globalTransformWithCanvas.AffineInverse() * globalTransformWithCanvas2;
		Vector2 position = transform2D * Vector2.Zero;
		Vector2 to = transform2D * new Vector2(control.Size.X, 0f);
		Vector2 to2 = transform2D * control.Size;
		Vector2 to3 = transform2D * new Vector2(0f, control.Size.Y);
		return new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3);
	}

	private static Button CreateInlineTextHeaderButton(string name, string iconName, string tooltip)
	{
		return new Button
		{
			Name = name,
			Icon = LoadInlineTextIcon(iconName),
			Flat = true,
			FocusMode = FocusModeEnum.None,
			CustomMinimumSize = new Vector2(27f, 27f),
			TooltipText = tooltip
		};
	}

	private static Texture2D LoadInlineTextIcon(string iconName)
	{
		string path = "res://addons/ModEditor/Icons/" + iconName + ".svg";
		if (!ResourceLoader.Exists(path))
		{
			return null;
		}
		return ResourceLoader.Load<Texture2D>(path, null, ResourceLoader.CacheMode.Reuse);
	}

	private static StyleBoxFlat CreateInlineTextOverlayStyle()
	{
		return new StyleBoxFlat
		{
			BgColor = new Color(0.035f, 0.09f, 0.052f, 0.98f),
			BorderColor = ModEditorTheme.SunGoldColor,
			BorderWidthLeft = 2,
			BorderWidthTop = 2,
			BorderWidthRight = 2,
			BorderWidthBottom = 2,
			CornerRadiusTopLeft = 10,
			CornerRadiusTopRight = 10,
			CornerRadiusBottomRight = 10,
			CornerRadiusBottomLeft = 10,
			ShadowColor = new Color(0f, 0f, 0f, 0.48f),
			ShadowSize = 8
		};
	}

	private void InitializeControlLayoutSurface()
	{
		_controlLayoutPanel = GetNode<PanelContainer>("%ControlLayoutPanel");
		_anchorLeftSpin = GetNode<SpinBox>("%AnchorLeftSpin");
		_anchorTopSpin = GetNode<SpinBox>("%AnchorTopSpin");
		_anchorRightSpin = GetNode<SpinBox>("%AnchorRightSpin");
		_anchorBottomSpin = GetNode<SpinBox>("%AnchorBottomSpin");
		_offsetLeftSpin = GetNode<SpinBox>("%OffsetLeftSpin");
		_offsetTopSpin = GetNode<SpinBox>("%OffsetTopSpin");
		_offsetRightSpin = GetNode<SpinBox>("%OffsetRightSpin");
		_offsetBottomSpin = GetNode<SpinBox>("%OffsetBottomSpin");
		_growHorizontalOption = GetNode<OptionButton>("%GrowHorizontalOption");
		_growVerticalOption = GetNode<OptionButton>("%GrowVerticalOption");
		_growHorizontalButtons = new Button[3]
		{
			GetNode<Button>("%GrowHorizontalBegin"),
			GetNode<Button>("%GrowHorizontalBoth"),
			GetNode<Button>("%GrowHorizontalEnd")
		};
		_growVerticalButtons = new Button[3]
		{
			GetNode<Button>("%GrowVerticalBegin"),
			GetNode<Button>("%GrowVerticalBoth"),
			GetNode<Button>("%GrowVerticalEnd")
		};
		_presetTopLeftButton = GetNode<Button>("%PresetTopLeftButton");
		_presetCenterButton = GetNode<Button>("%PresetCenterButton");
		_presetFullRectButton = GetNode<Button>("%PresetFullRectButton");
		_presetHCenterWideButton = GetNode<Button>("%PresetHCenterWideButton");
		_presetVCenterWideButton = GetNode<Button>("%PresetVCenterWideButton");
		AddGrowItems(_growHorizontalOption);
		AddGrowItems(_growVerticalOption);
		BindGrowDirectionButtons(_growHorizontalButtons, horizontal: true);
		BindGrowDirectionButtons(_growVerticalButtons, horizontal: false);
		SpinBox[] controlLayoutSpins = GetControlLayoutSpins();
		foreach (SpinBox spinBox in controlLayoutSpins)
		{
			SpinBox captured = spinBox;
			spinBox.ValueChanged += delegate
			{
				OnControlLayoutSpinChanged(captured);
			};
			spinBox.GetLineEdit().FocusEntered += BeginControlLayoutSession;
			spinBox.GetLineEdit().FocusExited += CommitControlLayoutSession;
		}
		_growHorizontalOption.ItemSelected += delegate(long index)
		{
			if (!_updatingLayoutSurface && GodotObject.IsInstanceValid(_layoutControl))
			{
				SetControlGrowWithHistory(_layoutControl, horizontal: true, GrowDirectionFromVisualIndex((int)index));
			}
		};
		_growVerticalOption.ItemSelected += delegate(long index)
		{
			if (!_updatingLayoutSurface && GodotObject.IsInstanceValid(_layoutControl))
			{
				SetControlGrowWithHistory(_layoutControl, horizontal: false, GrowDirectionFromVisualIndex((int)index));
			}
		};
		_presetTopLeftButton.Pressed += delegate
		{
			ApplySelectedControlLayoutPresetWithHistory(LayoutPreset.TopLeft);
		};
		_presetCenterButton.Pressed += delegate
		{
			ApplySelectedControlLayoutPresetWithHistory(LayoutPreset.Center);
		};
		_presetFullRectButton.Pressed += delegate
		{
			ApplySelectedControlLayoutPresetWithHistory(LayoutPreset.FullRect);
		};
		_presetHCenterWideButton.Pressed += delegate
		{
			ApplySelectedControlLayoutPresetWithHistory(LayoutPreset.HcenterWide);
		};
		_presetVCenterWideButton.Pressed += delegate
		{
			ApplySelectedControlLayoutPresetWithHistory(LayoutPreset.VcenterWide);
		};
	}

	private static void AddGrowItems(OptionButton option)
	{
		option.Clear();
		option.AddItem("起点", 0);
		option.AddItem("两侧", 2);
		option.AddItem("终点", 1);
	}

	private static GrowDirection GrowDirectionFromVisualIndex(int index)
	{
		return index switch
		{
			1 => GrowDirection.Both, 
			2 => GrowDirection.End, 
			_ => GrowDirection.Begin, 
		};
	}

	private static int GetGrowDirectionVisualIndex(GrowDirection direction)
	{
		return direction switch
		{
			GrowDirection.Both => 1, 
			GrowDirection.End => 2, 
			_ => 0, 
		};
	}

	private void BindGrowDirectionButtons(Button[] buttons, bool horizontal)
	{
		ButtonGroup buttonGroup = new ButtonGroup
		{
			AllowUnpress = false
		};
		for (int i = 0; i < buttons.Length; i++)
		{
			Button obj = buttons[i];
			int directionIndex = i;
			obj.ButtonGroup = buttonGroup;
			obj.Pressed += delegate
			{
				if (!_updatingLayoutSurface && GodotObject.IsInstanceValid(_layoutControl))
				{
					GrowDirection direction = GrowDirectionFromVisualIndex(directionIndex);
					if (horizontal)
					{
						_growHorizontalOption.Select(directionIndex);
					}
					else
					{
						_growVerticalOption.Select(directionIndex);
					}
					SetControlGrowWithHistory(_layoutControl, horizontal, direction);
				}
			};
		}
	}

	private SpinBox[] GetControlLayoutSpins()
	{
		return new SpinBox[8] { _anchorLeftSpin, _anchorTopSpin, _anchorRightSpin, _anchorBottomSpin, _offsetLeftSpin, _offsetTopSpin, _offsetRightSpin, _offsetBottomSpin };
	}

	private void SetDirectSpinValue(SpinBox spin, List<DirectTransformState> states, Func<DirectTransformState, float> getter, bool available = true)
	{
		if (!_directSpinBaseSuffixes.ContainsKey(spin))
		{
			_directSpinBaseSuffixes[spin] = spin.Suffix;
		}
		if (!available || states.Count == 0)
		{
			spin.SetValueNoSignal(0.0);
			ClearDirectSpinMixedMarker(spin);
			return;
		}
		float num = getter(states[0]);
		bool flag = false;
		for (int i = 1; i < states.Count; i++)
		{
			if (!Mathf.IsEqualApprox(num, getter(states[i])))
			{
				flag = true;
				break;
			}
		}
		spin.SetValueNoSignal(num);
		if (flag)
		{
			_directMixedSpins.Add(spin);
			spin.Suffix = _directSpinBaseSuffixes[spin] + " •";
			spin.TooltipText = "所选节点包含混合值；输入后只批量修改此字段";
		}
		else
		{
			ClearDirectSpinMixedMarker(spin);
		}
	}

	private void ClearDirectSpinMixedMarker(SpinBox spin)
	{
		_directMixedSpins.Remove(spin);
		if (_directSpinBaseSuffixes.TryGetValue(spin, out var value))
		{
			spin.Suffix = value;
		}
		spin.TooltipText = "";
	}

	private void ApplyDirectSpinValue(Node node, SpinBox spin)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is Control control)
		{
			if (spin == _positionXSpin)
			{
				control.Position = new Vector2((float)spin.Value, control.Position.Y);
			}
			else if (spin == _positionYSpin)
			{
				control.Position = new Vector2(control.Position.X, (float)spin.Value);
			}
			else if (spin == _rotationSpin)
			{
				control.Rotation = Mathf.DegToRad((float)spin.Value);
			}
			else if (spin == _scaleXSpin)
			{
				control.Scale = new Vector2((float)spin.Value, control.Scale.Y);
			}
			else if (spin == _scaleYSpin)
			{
				control.Scale = new Vector2(control.Scale.X, (float)spin.Value);
			}
			else if (spin == _sizeXSpin)
			{
				control.Size = new Vector2((float)spin.Value, control.Size.Y);
			}
			else if (spin == _sizeYSpin)
			{
				control.Size = new Vector2(control.Size.X, (float)spin.Value);
			}
			else if (spin == _pivotXSpin)
			{
				control.PivotOffset = new Vector2((float)spin.Value, control.PivotOffset.Y);
			}
			else if (spin == _pivotYSpin)
			{
				control.PivotOffset = new Vector2(control.PivotOffset.X, (float)spin.Value);
			}
		}
		else if (node is Node2D node2D)
		{
			if (spin == _positionXSpin)
			{
				node2D.Position = new Vector2((float)spin.Value, node2D.Position.Y);
			}
			else if (spin == _positionYSpin)
			{
				node2D.Position = new Vector2(node2D.Position.X, (float)spin.Value);
			}
			else if (spin == _rotationSpin)
			{
				node2D.Rotation = Mathf.DegToRad((float)spin.Value);
			}
			else if (spin == _scaleXSpin)
			{
				node2D.Scale = new Vector2((float)spin.Value, node2D.Scale.Y);
			}
			else if (spin == _scaleYSpin)
			{
				node2D.Scale = new Vector2(node2D.Scale.X, (float)spin.Value);
			}
		}
	}

	public bool IsDirectTransformFieldMixed(string fieldName)
	{
		SpinBox spinBox = FindDirectTransformSpin(fieldName);
		if (spinBox != null)
		{
			return _directMixedSpins.Contains(spinBox);
		}
		return false;
	}

	public bool ApplySelectedTransformFieldWithHistory(string fieldName, double value)
	{
		UpdateDirectTransformSurface();
		SpinBox spinBox = FindDirectTransformSpin(fieldName);
		if (spinBox == null || !spinBox.Editable || _directTransformNodes.Count == 0)
		{
			return false;
		}
		BeginDirectTransformEditSession();
		spinBox.Value = value;
		CommitDirectTransformEditSession();
		return true;
	}

	private SpinBox FindDirectTransformSpin(string fieldName)
	{
		return fieldName?.ToLowerInvariant() switch
		{
			"position_x" => _positionXSpin, 
			"position_y" => _positionYSpin, 
			"rotation" => _rotationSpin, 
			"scale_x" => _scaleXSpin, 
			"scale_y" => _scaleYSpin, 
			"size_x" => _sizeXSpin, 
			"size_y" => _sizeYSpin, 
			"pivot_x" => _pivotXSpin, 
			"pivot_y" => _pivotYSpin, 
			_ => null, 
		};
	}

	private void UpdateControlLayoutSurface()
	{
		if (_layoutSessionActive)
		{
			CommitControlLayoutSession();
		}
		_layoutControl = ((_directTransformNodes.Count == 1) ? (_directTransformNodes[0] as Control) : null);
		bool flag = GodotObject.IsInstanceValid(_layoutControl);
		_controlLayoutPanel.Visible = flag;
		if (flag)
		{
			ControlLayoutState controlLayoutState = new ControlLayoutState(_layoutControl);
			_updatingLayoutSurface = true;
			_anchorLeftSpin.SetValueNoSignal(controlLayoutState.AnchorLeft);
			_anchorTopSpin.SetValueNoSignal(controlLayoutState.AnchorTop);
			_anchorRightSpin.SetValueNoSignal(controlLayoutState.AnchorRight);
			_anchorBottomSpin.SetValueNoSignal(controlLayoutState.AnchorBottom);
			_offsetLeftSpin.SetValueNoSignal(controlLayoutState.OffsetLeft);
			_offsetTopSpin.SetValueNoSignal(controlLayoutState.OffsetTop);
			_offsetRightSpin.SetValueNoSignal(controlLayoutState.OffsetRight);
			_offsetBottomSpin.SetValueNoSignal(controlLayoutState.OffsetBottom);
			_growHorizontalOption.Select(GetGrowDirectionVisualIndex(controlLayoutState.GrowHorizontal));
			_growVerticalOption.Select(GetGrowDirectionVisualIndex(controlLayoutState.GrowVertical));
			SelectGrowDirectionButton(_growHorizontalButtons, controlLayoutState.GrowHorizontal);
			SelectGrowDirectionButton(_growVerticalButtons, controlLayoutState.GrowVertical);
			_updatingLayoutSurface = false;
		}
	}

	private static void SelectGrowDirectionButton(Button[] buttons, GrowDirection direction)
	{
		int num = Mathf.Clamp(GetGrowDirectionVisualIndex(direction), 0, buttons.Length - 1);
		for (int i = 0; i < buttons.Length; i++)
		{
			buttons[i].SetPressedNoSignal(i == num);
		}
	}

	private void BeginControlLayoutSession()
	{
		if (!_updatingLayoutSurface && !_layoutSessionActive && GodotObject.IsInstanceValid(_layoutControl))
		{
			_layoutStart = new ControlLayoutState(_layoutControl);
			_layoutSessionActive = true;
		}
	}

	private void OnControlLayoutSpinChanged(SpinBox spin)
	{
		if (_updatingLayoutSurface || !GodotObject.IsInstanceValid(_layoutControl))
		{
			return;
		}
		if (!_layoutSessionActive)
		{
			BeginControlLayoutSession();
		}
		if (_layoutSessionActive)
		{
			if (spin == _anchorLeftSpin)
			{
				_layoutControl.AnchorLeft = (float)spin.Value;
			}
			else if (spin == _anchorTopSpin)
			{
				_layoutControl.AnchorTop = (float)spin.Value;
			}
			else if (spin == _anchorRightSpin)
			{
				_layoutControl.AnchorRight = (float)spin.Value;
			}
			else if (spin == _anchorBottomSpin)
			{
				_layoutControl.AnchorBottom = (float)spin.Value;
			}
			else if (spin == _offsetLeftSpin)
			{
				_layoutControl.OffsetLeft = (float)spin.Value;
			}
			else if (spin == _offsetTopSpin)
			{
				_layoutControl.OffsetTop = (float)spin.Value;
			}
			else if (spin == _offsetRightSpin)
			{
				_layoutControl.OffsetRight = (float)spin.Value;
			}
			else if (spin == _offsetBottomSpin)
			{
				_layoutControl.OffsetBottom = (float)spin.Value;
			}
			_viewport.QueueRedrawAll();
		}
	}

	private void CommitControlLayoutSession()
	{
		if (_layoutSessionActive)
		{
			_layoutSessionActive = false;
			if (GodotObject.IsInstanceValid(_layoutControl))
			{
				CommitControlLayoutHistory(after: new ControlLayoutState(_layoutControl), control: _layoutControl, before: _layoutStart, actionName: "编辑 Control 布局");
			}
		}
	}

	public bool BeginViewportControlLayoutSession(Control control)
	{
		if (!GodotObject.IsInstanceValid(control) || _layoutSessionActive)
		{
			return false;
		}
		_layoutControl = control;
		_layoutStart = new ControlLayoutState(control);
		_layoutSessionActive = true;
		return true;
	}

	public bool CommitViewportControlLayoutSession(string actionName)
	{
		if (!_layoutSessionActive || !GodotObject.IsInstanceValid(_layoutControl))
		{
			return false;
		}
		Control layoutControl = _layoutControl;
		ControlLayoutState layoutStart = _layoutStart;
		ControlLayoutState after = new ControlLayoutState(layoutControl);
		_layoutSessionActive = false;
		return CommitControlLayoutHistory(layoutControl, layoutStart, after, string.IsNullOrWhiteSpace(actionName) ? "拖动 Control 布局手柄" : actionName);
	}

	public void CancelViewportControlLayoutSession()
	{
		if (_layoutSessionActive)
		{
			_layoutSessionActive = false;
			if (GodotObject.IsInstanceValid(_layoutControl))
			{
				ApplyControlLayoutState(_layoutControl, _layoutStart);
				NotifyControlLayoutHistoryApplied(_layoutControl);
			}
		}
	}

	public bool EditSelectedControlLayoutFieldWithHistory(string fieldName, double value)
	{
		UpdateControlLayoutSurface();
		SpinBox spinBox = fieldName?.ToLowerInvariant() switch
		{
			"anchor_left" => _anchorLeftSpin, 
			"anchor_top" => _anchorTopSpin, 
			"anchor_right" => _anchorRightSpin, 
			"anchor_bottom" => _anchorBottomSpin, 
			"offset_left" => _offsetLeftSpin, 
			"offset_top" => _offsetTopSpin, 
			"offset_right" => _offsetRightSpin, 
			"offset_bottom" => _offsetBottomSpin, 
			_ => null, 
		};
		if (spinBox == null || !GodotObject.IsInstanceValid(_layoutControl))
		{
			return false;
		}
		BeginControlLayoutSession();
		spinBox.Value = value;
		CommitControlLayoutSession();
		return true;
	}

	public bool ApplySelectedControlLayoutPresetWithHistory(LayoutPreset preset)
	{
		UpdateControlLayoutSurface();
		return ApplyControlLayoutPresetWithHistory(_layoutControl, preset);
	}

	public bool ApplyControlLayoutPresetWithHistory(Control control, LayoutPreset preset)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return false;
		}
		ControlLayoutState controlLayoutState = new ControlLayoutState(control);
		control.SetAnchorsAndOffsetsPreset(preset, LayoutPresetMode.Minsize);
		ControlLayoutState after = new ControlLayoutState(control);
		ApplyControlLayoutState(control, controlLayoutState);
		return CommitControlLayoutHistory(control, controlLayoutState, after, $"应用布局预设 {preset}");
	}

	public bool SetControlGrowWithHistory(Control control, bool horizontal, GrowDirection direction)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return false;
		}
		ControlLayoutState controlLayoutState = new ControlLayoutState(control);
		if (horizontal)
		{
			control.GrowHorizontal = direction;
		}
		else
		{
			control.GrowVertical = direction;
		}
		ControlLayoutState after = new ControlLayoutState(control);
		ApplyControlLayoutState(control, controlLayoutState);
		return CommitControlLayoutHistory(control, controlLayoutState, after, horizontal ? "设置水平增长方向" : "设置垂直增长方向");
	}

	private bool CommitControlLayoutHistory(Control control, ControlLayoutState before, ControlLayoutState after, string actionName)
	{
		if (!HasControlLayoutChanged(before, after))
		{
			return false;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			ApplyControlLayoutState(control, after);
			return true;
		}
		xWUndoRedoManager.CreateAction(actionName, mergeMode: false, GetCurrentSceneHistoryId());
		AddControlLayoutProperties(xWUndoRedoManager, control, before, after);
		xWUndoRedoManager.AddDoMethod(this, "NotifyControlLayoutHistoryApplied", Variant.From(in control));
		xWUndoRedoManager.AddUndoMethod(this, "NotifyControlLayoutHistoryApplied", Variant.From(in control));
		xWUndoRedoManager.CommitAction();
		_viewport.ActivateSceneTransformHistoryDeferred();
		return true;
	}

	private static void AddControlLayoutProperties(XWUndoRedoManager undoRedo, Control control, ControlLayoutState before, ControlLayoutState after)
	{
		AddHistoryProperty(undoRedo, control, "anchor_left", before.AnchorLeft, after.AnchorLeft);
		AddHistoryProperty(undoRedo, control, "anchor_top", before.AnchorTop, after.AnchorTop);
		AddHistoryProperty(undoRedo, control, "anchor_right", before.AnchorRight, after.AnchorRight);
		AddHistoryProperty(undoRedo, control, "anchor_bottom", before.AnchorBottom, after.AnchorBottom);
		AddHistoryProperty(undoRedo, control, "offset_left", before.OffsetLeft, after.OffsetLeft);
		AddHistoryProperty(undoRedo, control, "offset_top", before.OffsetTop, after.OffsetTop);
		AddHistoryProperty(undoRedo, control, "offset_right", before.OffsetRight, after.OffsetRight);
		AddHistoryProperty(undoRedo, control, "offset_bottom", before.OffsetBottom, after.OffsetBottom);
		undoRedo.AddDoProperty(control, "grow_horizontal", Variant.From<int>((int)after.GrowHorizontal));
		undoRedo.AddUndoProperty(control, "grow_horizontal", Variant.From<int>((int)before.GrowHorizontal));
		undoRedo.AddDoProperty(control, "grow_vertical", Variant.From<int>((int)after.GrowVertical));
		undoRedo.AddUndoProperty(control, "grow_vertical", Variant.From<int>((int)before.GrowVertical));
	}

	private static void AddHistoryProperty(XWUndoRedoManager undoRedo, GodotObject target, StringName property, float before, float after)
	{
		undoRedo.AddDoProperty(target, property, Variant.From(in after));
		undoRedo.AddUndoProperty(target, property, Variant.From(in before));
	}

	private static void ApplyControlLayoutState(Control control, ControlLayoutState state)
	{
		control.AnchorLeft = state.AnchorLeft;
		control.AnchorTop = state.AnchorTop;
		control.AnchorRight = state.AnchorRight;
		control.AnchorBottom = state.AnchorBottom;
		control.OffsetLeft = state.OffsetLeft;
		control.OffsetTop = state.OffsetTop;
		control.OffsetRight = state.OffsetRight;
		control.OffsetBottom = state.OffsetBottom;
		control.GrowHorizontal = state.GrowHorizontal;
		control.GrowVertical = state.GrowVertical;
	}

	private static bool HasControlLayoutChanged(ControlLayoutState before, ControlLayoutState after)
	{
		if (Mathf.IsEqualApprox(before.AnchorLeft, after.AnchorLeft) && Mathf.IsEqualApprox(before.AnchorTop, after.AnchorTop) && Mathf.IsEqualApprox(before.AnchorRight, after.AnchorRight) && Mathf.IsEqualApprox(before.AnchorBottom, after.AnchorBottom) && Mathf.IsEqualApprox(before.OffsetLeft, after.OffsetLeft) && Mathf.IsEqualApprox(before.OffsetTop, after.OffsetTop) && Mathf.IsEqualApprox(before.OffsetRight, after.OffsetRight) && Mathf.IsEqualApprox(before.OffsetBottom, after.OffsetBottom) && before.GrowHorizontal == after.GrowHorizontal)
		{
			return before.GrowVertical != after.GrowVertical;
		}
		return true;
	}

	public void NotifyControlLayoutHistoryApplied(Control control)
	{
		if (GodotObject.IsInstanceValid(control))
		{
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(control, emitSignal: false);
		}
		MarkCurrentSceneHistorySurfaceDirty();
		UpdateDirectTransformSurface();
		_viewport.QueueRedrawAll();
	}

	public bool SetSelectionLockWithHistory(bool enabled)
	{
		return CommitSelectionMetaHistory("_edit_lock_", enabled);
	}

	public bool SetSelectionGroupWithHistory(bool enabled)
	{
		return CommitSelectionMetaHistory("_edit_group_", enabled);
	}

	private bool CommitSelectionMetaHistory(string metaName, bool enabled)
	{
		List<Node> list = new List<Node>();
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem && item.HasMeta(metaName) != enabled)
			{
				list.Add(item);
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			foreach (Node item2 in list)
			{
				ApplyNodeEditMeta(item2, metaName, enabled);
			}
			NotifySelectionMetaHistoryApplied(list[list.Count - 1]);
			return true;
		}
		string text = ((metaName == "_edit_lock_") ? "锁定" : "分组");
		string name = (enabled ? ("批量" + text + " 2D 节点") : ("批量取消" + text + " 2D 节点"));
		xWUndoRedoManager.CreateAction(name, mergeMode: false, GetCurrentSceneHistoryId());
		Variant[] array = new Variant[1];
		array[0] = Variant.From<Node>(list[list.Count - 1]);
		xWUndoRedoManager.AddUndoMethod(this, "NotifySelectionMetaHistoryApplied", array);
		foreach (Node item3 in list)
		{
			Node from = item3;
			xWUndoRedoManager.AddDoMethod(this, "ApplyNodeEditMeta", Variant.From(in from), Variant.From(in metaName), Variant.From(in enabled));
			xWUndoRedoManager.AddUndoMethod(this, "ApplyNodeEditMeta", Variant.From(in from), Variant.From(in metaName), Variant.From<bool>(!enabled));
		}
		Variant[] array2 = new Variant[1];
		array2[0] = Variant.From<Node>(list[list.Count - 1]);
		xWUndoRedoManager.AddDoMethod(this, "NotifySelectionMetaHistoryApplied", array2);
		xWUndoRedoManager.CommitAction();
		return true;
	}

	public void ApplyNodeEditMeta(Node node, string metaName, bool enabled)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (metaName == "_edit_lock_")
		{
			XWSceneNodeTree xWSceneNodeTree = XWEditorInterface.Instance?.GetSceneTreeDock()?.GetNodeOrNull<XWSceneNodeTree>("%SceneTree");
			if (GodotObject.IsInstanceValid(xWSceneNodeTree))
			{
				xWSceneNodeTree.ApplyNodeLockStorage(node, enabled);
				return;
			}
		}
		if (enabled)
		{
			node.SetMeta(metaName, true);
		}
		else if (node.HasMeta(metaName))
		{
			node.RemoveMeta(metaName);
		}
	}

	public void NotifySelectionMetaHistoryApplied(Node focus)
	{
		RefreshSceneTree();
		if (focus != null && GodotObject.IsInstanceValid(focus))
		{
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(focus, emitSignal: false);
		}
		MarkCurrentSceneHistorySurfaceDirty();
		UpdateSelectionActionButtons();
		UpdateDirectTransformSurface();
		_viewport.QueueRedrawAll();
	}

	private void PrepareSceneSurfaceHistoryMutation()
	{
		XWEditorInterface.Instance?.GetSceneTreeDock()?.PrepareForSceneHistoryBranchChange();
		PrepareForSceneHistoryBranchChange();
	}

	private void MarkCurrentSceneHistorySurfaceDirty()
	{
		CallDeferred("UpdateCurrentSceneDirtyMarker");
	}

	public void UpdateCurrentSceneDirtyMarker()
	{
		UpdateSceneDirtyMarker(_currentTabKey);
	}

	private void UpdateSceneDirtyMarker(string tabKey)
	{
		int num = _tabKeys.IndexOf(tabKey);
		if (num >= 0 && _openTabs.TryGetValue(tabKey, out var value))
		{
			XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
			bool flag = GodotObject.IsInstanceValid(xWUndoRedoManager) && xWUndoRedoManager.IsHistoryUnsaved(value.HistoryId);
			_sceneTabBar.SetTabTitle(num, flag ? ("(*) " + value.DisplayName) : value.DisplayName);
		}
	}

	private void InitializeSelectionLayoutSurface()
	{
		_selectionLayoutPanel = GetNode<PanelContainer>("%SelectionLayoutPanel");
		_selectionLayoutLabel = GetNode<Label>("%SelectionLayoutLabel");
		Button node = GetNode<Button>("%AlignSelectionLeftButton");
		Button node2 = GetNode<Button>("%AlignSelectionHorizontalCenterButton");
		Button node3 = GetNode<Button>("%AlignSelectionRightButton");
		Button node4 = GetNode<Button>("%AlignSelectionTopButton");
		Button node5 = GetNode<Button>("%AlignSelectionVerticalCenterButton");
		Button node6 = GetNode<Button>("%AlignSelectionBottomButton");
		Button node7 = GetNode<Button>("%DistributeSelectionHorizontalButton");
		Button node8 = GetNode<Button>("%DistributeSelectionVerticalButton");
		_selectionAlignButtons = new Button[6] { node, node2, node3, node4, node5, node6 };
		_selectionDistributeButtons = new Button[2] { node7, node8 };
		node.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignLeft);
		};
		node2.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignHorizontalCenter);
		};
		node3.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignRight);
		};
		node4.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignTop);
		};
		node5.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignVerticalCenter);
		};
		node6.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.AlignBottom);
		};
		node7.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.DistributeHorizontal);
		};
		node8.Pressed += delegate
		{
			ApplySelectionLayoutWithHistory(SelectionLayoutOperation.DistributeVertical);
		};
	}

	private void UpdateSelectionLayoutSurface()
	{
		CollectSelectionLayoutNodes();
		int count = _selectionLayoutNodes.Count;
		if (GodotObject.IsInstanceValid(_selectionLayoutPanel))
		{
			_selectionLayoutPanel.Visible = count >= 2;
		}
		if (GodotObject.IsInstanceValid(_selectionLayoutLabel))
		{
			_selectionLayoutLabel.Text = ((count >= 2) ? $"多选排版 · {count} 项" : "多选排版");
		}
		Button[] array = _selectionAlignButtons ?? System.Array.Empty<Button>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Disabled = count < 2;
		}
		array = _selectionDistributeButtons ?? System.Array.Empty<Button>();
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Disabled = count < 3;
		}
	}

	private void CollectSelectionLayoutNodes()
	{
		_selectionLayoutNodes.Clear();
		HashSet<Node> hashSet = new HashSet<Node>();
		foreach (Node directTransformNode in _directTransformNodes)
		{
			if (directTransformNode != null && GodotObject.IsInstanceValid(directTransformNode) && !directTransformNode.HasMeta("_edit_lock_") && directTransformNode is CanvasItem)
			{
				hashSet.Add(directTransformNode);
			}
		}
		foreach (Node directTransformNode2 in _directTransformNodes)
		{
			if (hashSet.Contains(directTransformNode2) && !HasSelectedTransformAncestor(directTransformNode2, hashSet))
			{
				_selectionLayoutNodes.Add(directTransformNode2);
			}
		}
	}

	private static bool HasSelectedTransformAncestor(Node node, HashSet<Node> selectedNodes)
	{
		Node node2 = node?.GetParent();
		while (node2 != null && GodotObject.IsInstanceValid(node2))
		{
			if (selectedNodes.Contains(node2))
			{
				return true;
			}
			node2 = node2.GetParent();
		}
		return false;
	}

	public bool ApplySelectionLayoutWithHistory(SelectionLayoutOperation operation)
	{
		if (!GodotObject.IsInstanceValid(_viewport))
		{
			return false;
		}
		CollectSelectionLayoutNodes();
		bool flag = (uint)(operation - 6) <= 1u;
		int num = (flag ? 3 : 2);
		if (_selectionLayoutNodes.Count < num)
		{
			return false;
		}
		List<SelectionLayoutEntry> list = new List<SelectionLayoutEntry>();
		foreach (Node selectionLayoutNode in _selectionLayoutNodes)
		{
			if (selectionLayoutNode is CanvasItem item && TryCaptureDirectTransformState(selectionLayoutNode, out var state))
			{
				list.Add(new SelectionLayoutEntry(selectionLayoutNode, state, _viewport.GetCanvasItemWorldBounds(item)));
			}
		}
		if (list.Count < num)
		{
			return false;
		}
		System.Collections.Generic.Dictionary<Node, Vector2> dictionary = BuildSelectionLayoutWorldDeltas(list, operation);
		List<(Node, DirectTransformState, DirectTransformState)> list2 = new List<(Node, DirectTransformState, DirectTransformState)>();
		foreach (SelectionLayoutEntry item2 in list)
		{
			if (dictionary.TryGetValue(item2.Node, out var value) && !value.IsEqualApprox(Vector2.Zero) && _viewport.TryResolveLocalPositionForWorldDelta(item2.Node, item2.State.Position, value, out var localTarget) && !localTarget.IsEqualApprox(item2.State.Position))
			{
				list2.Add(new ValueTuple<Node, DirectTransformState, DirectTransformState>(item3: new DirectTransformState(localTarget, item2.State.Scale, item2.State.Rotation, item2.State.Size, item2.State.Pivot, item2.State.IsControl), item1: item2.Node, item2: item2.State));
			}
		}
		if (list2.Count == 0)
		{
			return false;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			return false;
		}
		xWUndoRedoManager.CreateAction(GetSelectionLayoutHistoryName(operation), mergeMode: false, GetCurrentSceneHistoryId());
		foreach (var item3 in list2)
		{
			AddDirectTransformHistoryProperties(xWUndoRedoManager, item3.Item1, item3.Item2, item3.Item3);
		}
		xWUndoRedoManager.AddDoMethod(this, "NotifyDirectTransformHistoryApplied");
		xWUndoRedoManager.AddUndoMethod(this, "NotifyDirectTransformHistoryApplied");
		xWUndoRedoManager.CommitAction();
		_viewport.ActivateSceneTransformHistoryDeferred();
		return true;
	}

	private static System.Collections.Generic.Dictionary<Node, Vector2> BuildSelectionLayoutWorldDeltas(List<SelectionLayoutEntry> entries, SelectionLayoutOperation operation)
	{
		System.Collections.Generic.Dictionary<Node, Vector2> dictionary = new System.Collections.Generic.Dictionary<Node, Vector2>();
		Rect2 rect = entries[0].WorldBounds;
		for (int i = 1; i < entries.Count; i++)
		{
			rect = rect.Merge(entries[i].WorldBounds);
		}
		if ((uint)(operation - 6) <= 1u)
		{
			bool horizontal = operation == SelectionLayoutOperation.DistributeHorizontal;
			entries.Sort((SelectionLayoutEntry left, SelectionLayoutEntry right) => GetAxisStart(left.WorldBounds, horizontal).CompareTo(GetAxisStart(right.WorldBounds, horizontal)));
			float num = 0f;
			foreach (SelectionLayoutEntry entry in entries)
			{
				num += GetAxisSize(entry.WorldBounds, horizontal);
			}
			float axisStart = GetAxisStart(entries[0].WorldBounds, horizontal);
			float num2 = (GetAxisEnd(entries[entries.Count - 1].WorldBounds, horizontal) - axisStart - num) / (float)(entries.Count - 1);
			float num3 = axisStart;
			for (int num4 = 0; num4 < entries.Count; num4++)
			{
				SelectionLayoutEntry selectionLayoutEntry = entries[num4];
				float num5 = ((num4 == 0 || num4 == entries.Count - 1) ? 0f : (num3 - GetAxisStart(selectionLayoutEntry.WorldBounds, horizontal)));
				dictionary[selectionLayoutEntry.Node] = (horizontal ? new Vector2(num5, 0f) : new Vector2(0f, num5));
				num3 += GetAxisSize(selectionLayoutEntry.WorldBounds, horizontal) + num2;
			}
			return dictionary;
		}
		float num6 = operation switch
		{
			SelectionLayoutOperation.AlignLeft => rect.Position.X, 
			SelectionLayoutOperation.AlignHorizontalCenter => rect.Position.X + rect.Size.X * 0.5f, 
			SelectionLayoutOperation.AlignRight => rect.Position.X + rect.Size.X, 
			SelectionLayoutOperation.AlignTop => rect.Position.Y, 
			SelectionLayoutOperation.AlignVerticalCenter => rect.Position.Y + rect.Size.Y * 0.5f, 
			SelectionLayoutOperation.AlignBottom => rect.Position.Y + rect.Size.Y, 
			_ => 0f, 
		};
		foreach (SelectionLayoutEntry entry2 in entries)
		{
			Rect2 worldBounds = entry2.WorldBounds;
			Vector2 value = operation switch
			{
				SelectionLayoutOperation.AlignLeft => new Vector2(num6 - worldBounds.Position.X, 0f), 
				SelectionLayoutOperation.AlignHorizontalCenter => new Vector2(num6 - worldBounds.Position.X - worldBounds.Size.X * 0.5f, 0f), 
				SelectionLayoutOperation.AlignRight => new Vector2(num6 - worldBounds.Position.X - worldBounds.Size.X, 0f), 
				SelectionLayoutOperation.AlignTop => new Vector2(0f, num6 - worldBounds.Position.Y), 
				SelectionLayoutOperation.AlignVerticalCenter => new Vector2(0f, num6 - worldBounds.Position.Y - worldBounds.Size.Y * 0.5f), 
				SelectionLayoutOperation.AlignBottom => new Vector2(0f, num6 - worldBounds.Position.Y - worldBounds.Size.Y), 
				_ => Vector2.Zero, 
			};
			dictionary[entry2.Node] = value;
		}
		return dictionary;
	}

	private static float GetAxisStart(Rect2 rect, bool horizontal)
	{
		if (!horizontal)
		{
			return rect.Position.Y;
		}
		return rect.Position.X;
	}

	private static float GetAxisSize(Rect2 rect, bool horizontal)
	{
		if (!horizontal)
		{
			return rect.Size.Y;
		}
		return rect.Size.X;
	}

	private static float GetAxisEnd(Rect2 rect, bool horizontal)
	{
		return GetAxisStart(rect, horizontal) + GetAxisSize(rect, horizontal);
	}

	private static string GetSelectionLayoutHistoryName(SelectionLayoutOperation operation)
	{
		return operation switch
		{
			SelectionLayoutOperation.AlignLeft => "左对齐 2D 节点", 
			SelectionLayoutOperation.AlignHorizontalCenter => "水平居中对齐 2D 节点", 
			SelectionLayoutOperation.AlignRight => "右对齐 2D 节点", 
			SelectionLayoutOperation.AlignTop => "顶部对齐 2D 节点", 
			SelectionLayoutOperation.AlignVerticalCenter => "垂直居中对齐 2D 节点", 
			SelectionLayoutOperation.AlignBottom => "底部对齐 2D 节点", 
			SelectionLayoutOperation.DistributeHorizontal => "水平等距分布 2D 节点", 
			SelectionLayoutOperation.DistributeVertical => "垂直等距分布 2D 节点", 
			_ => "排版 2D 节点", 
		};
	}

	private void InitializeSpriteAtlasSurface()
	{
		_spriteAtlasPanel = new XW2DSpriteAtlasPanel
		{
			Name = "SpriteAtlasWorkbench",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			SizeFlagsVertical = SizeFlags.ExpandFill
		};
		_workspaceTabs.AddChild(_spriteAtlasPanel, forceReadableName: false, InternalMode.Disabled);
		_spriteAtlasTabIndex = _workspaceTabs.GetTabCount() - 1;
		_workspaceTabs.SetTabTitle(_spriteAtlasTabIndex, "图集切片");
		_workspaceTabs.SetTabIcon(_spriteAtlasTabIndex, XWClassRegistry.Instance.GetUIIcon("AtlasTexture"));
		_spriteAtlasPanel.BindEditor(this);
		_spriteAtlasButton = new Button
		{
			Name = "SpriteAtlasButton",
			Text = "图集",
			TooltipText = "打开 Sprite2D 可视化图集切片台",
			Flat = true,
			Visible = false,
			CustomMinimumSize = new Vector2(56f, 28f),
			Icon = XWClassRegistry.Instance.GetUIIcon("AtlasTexture")
		};
		Control control = _animationTimelineButton?.GetParent() as Control;
		control?.AddChild(_spriteAtlasButton, forceReadableName: false, InternalMode.Disabled);
		if (GodotObject.IsInstanceValid(control))
		{
			int index = _animationTimelineButton.GetIndex();
			control.MoveChild(_spriteAtlasButton, index + 1);
		}
		_spriteAtlasButton.Pressed += OpenSelectedSpriteAtlas;
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection != null)
		{
			xWEditorSelection.SelectionChanged += OnSpriteAtlasSelectionChanged;
			_spriteAtlasSelectionConnected = true;
		}
		OnSpriteAtlasSelectionChanged();
	}

	private void ShutdownSpriteAtlasSurface()
	{
		if (_spriteAtlasSelectionConnected)
		{
			XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
			if (xWEditorSelection != null)
			{
				xWEditorSelection.SelectionChanged -= OnSpriteAtlasSelectionChanged;
			}
			_spriteAtlasSelectionConnected = false;
		}
		if (GodotObject.IsInstanceValid(_spriteAtlasButton))
		{
			_spriteAtlasButton.Pressed -= OpenSelectedSpriteAtlas;
		}
		_spriteAtlasPanel?.BindSprite(null);
	}

	private void OnSpriteAtlasSelectionChanged()
	{
		Sprite2D sprite2D = ResolveSingleSelectedSprite();
		_spriteAtlasPanel?.BindSprite(sprite2D);
		if (GodotObject.IsInstanceValid(_spriteAtlasButton))
		{
			_spriteAtlasButton.Visible = GodotObject.IsInstanceValid(sprite2D);
		}
	}

	private Sprite2D ResolveSingleSelectedSprite()
	{
		Sprite2D sprite2D = null;
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			if (item is Sprite2D sprite2D2)
			{
				if (GodotObject.IsInstanceValid(sprite2D))
				{
					return null;
				}
				sprite2D = sprite2D2;
			}
		}
		return sprite2D;
	}

	private void OpenSelectedSpriteAtlas()
	{
		if (_spriteAtlasTabIndex >= 0 && GodotObject.IsInstanceValid(_spriteAtlasPanel))
		{
			_workspaceTabs.CurrentTab = _spriteAtlasTabIndex;
			_spriteAtlasPanel.RefreshFromSprite();
		}
	}

	public bool OpenSpriteAtlasForSelection()
	{
		Sprite2D sprite2D = ResolveSingleSelectedSprite();
		if (!GodotObject.IsInstanceValid(sprite2D))
		{
			return false;
		}
		_spriteAtlasPanel.BindSprite(sprite2D);
		OpenSelectedSpriteAtlas();
		return _workspaceTabs.CurrentTab == _spriteAtlasTabIndex;
	}

	public bool CommitSpriteAtlasStateWithHistory(Sprite2D sprite, int columns, int rows, int frame, bool regionEnabled, Rect2 regionRect, string actionName)
	{
		if (!GodotObject.IsInstanceValid(sprite) || !IsNodeInCurrentScene(sprite))
		{
			return false;
		}
		SpriteAtlasState left = new SpriteAtlasState(sprite);
		SpriteAtlasState right = new SpriteAtlasState(columns, rows, frame, regionEnabled, regionRect);
		if (SpriteAtlasStatesEqual(left, right))
		{
			return true;
		}
		PrepareSceneSurfaceHistoryMutation();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			ApplySpriteAtlasState(sprite, right.Columns, right.Rows, right.Frame, right.RegionEnabled, right.RegionRect);
			NotifySpriteAtlasHistoryApplied(sprite);
			return true;
		}
		string text = (string.IsNullOrWhiteSpace(actionName) ? "编辑精灵图集" : actionName);
		bool mergeMode = text == "选择精灵图集帧";
		xWUndoRedoManager.CreateAction(text, mergeMode, GetCurrentSceneHistoryId());
		xWUndoRedoManager.AddDoMethod(this, "ApplySpriteAtlasState", Variant.From(in sprite), Variant.From(in right.Columns), Variant.From(in right.Rows), Variant.From(in right.Frame), Variant.From(in right.RegionEnabled), Variant.From(in right.RegionRect));
		xWUndoRedoManager.AddUndoMethod(this, "NotifySpriteAtlasHistoryApplied", Variant.From(in sprite));
		xWUndoRedoManager.AddUndoMethod(this, "ApplySpriteAtlasState", Variant.From(in sprite), Variant.From(in left.Columns), Variant.From(in left.Rows), Variant.From(in left.Frame), Variant.From(in left.RegionEnabled), Variant.From(in left.RegionRect));
		xWUndoRedoManager.AddDoMethod(this, "NotifySpriteAtlasHistoryApplied", Variant.From(in sprite));
		xWUndoRedoManager.CommitAction();
		return true;
	}

	public void ApplySpriteAtlasState(Sprite2D sprite, int columns, int rows, int frame, bool regionEnabled, Rect2 regionRect)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			int num = Math.Clamp(columns, 1, 256);
			int num2 = Math.Clamp(rows, 1, 256);
			sprite.Frame = 0;
			sprite.Hframes = num;
			sprite.Vframes = num2;
			sprite.Frame = Math.Clamp(frame, 0, num * num2 - 1);
			sprite.RegionRect = regionRect;
			sprite.RegionEnabled = regionEnabled;
		}
	}

	public void NotifySpriteAtlasHistoryApplied(Sprite2D sprite)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			XWSceneTreeDock xWSceneTreeDock = XWEditorInterface.Instance?.GetSceneTreeDock();
			if (xWSceneTreeDock?.GetSelectedNode() != sprite)
			{
				xWSceneTreeDock?.SelectNode(sprite, emitSignal: false);
			}
			XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
			if (xWEditorSelection != null && !xWEditorSelection.IsSelected(sprite))
			{
				XWEditorInterface.Instance?.SetSelectedNode(sprite);
			}
			_spriteAtlasPanel?.BindSprite(sprite);
		}
		QueueViewportRedraw();
	}

	private bool IsNodeInCurrentScene(Node node)
	{
		if (!GodotObject.IsInstanceValid(node) || !GodotObject.IsInstanceValid(CurrentSceneInstance))
		{
			return false;
		}
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2))
		{
			if (node2 == CurrentSceneInstance)
			{
				return true;
			}
			node2 = node2.GetParent();
		}
		return false;
	}

	private static bool SpriteAtlasStatesEqual(SpriteAtlasState left, SpriteAtlasState right)
	{
		if (left.Columns == right.Columns && left.Rows == right.Rows && left.Frame == right.Frame && left.RegionEnabled == right.RegionEnabled)
		{
			return left.RegionRect.IsEqualApprox(right.RegionRect);
		}
		return false;
	}
}
