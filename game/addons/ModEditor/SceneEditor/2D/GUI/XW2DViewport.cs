using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.Core;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.Registry.Class;

namespace PVZHE.ModEditor.SceneEditor;

public partial class XW2DViewport : Control
{
	public enum ToolMode
	{
		Select,
		Move,
		Rotate,
		Scale,
		ListSelect,
		Pivot,
		Pan,
		Ruler,
		Path,
		Polygon
	}

	public enum GridVisibility
	{
		Show,
		ShowWhenSnapping,
		Hide
	}

	private enum DragMode
	{
		None,
		BoxSelection,
		DragQueued,
		DragMove,
		DragRotate,
		DragScale,
		DragPivot
	}

	private enum PreviewDragMode
	{
		None,
		Pan,
		Resize
	}

	private enum PointEditMode
	{
		None,
		MovePathPoint,
		MovePathInHandle,
		MovePathOutHandle,
		MovePolygonPoint
	}

	private enum GuideAxis
	{
		None,
		Vertical,
		Horizontal
	}

	private readonly struct DragTransformState(Vector2 position, Vector2 scale, float rotation, Vector2 worldOrigin)
	{
		public readonly Vector2 Position = position;

		public readonly Vector2 Scale = scale;

		public readonly float Rotation = rotation;

		public readonly Vector2 WorldOrigin = worldOrigin;
	}

	private readonly struct PivotTransformState(Vector2 value, bool hadMetadata = false)
	{
		public readonly Vector2 Value = value;

		public readonly bool HadMetadata = hadMetadata;
	}

	[Signal]
	public delegate void ZoomChangedEventHandler();

	[Signal]
	public delegate void SelectionChangedEventHandler();

	[Signal]
	public delegate void MousePositionChangedEventHandler(Vector2 position);

	private sealed class DroppedResourceBatch
	{
		public int HistoryId;

		public Node Parent;

		public readonly List<Node> Nodes = new List<Node>();

		public readonly List<Vector2> WorldPositions = new List<Vector2>();

		public bool Applied;
	}

	public enum GizmoHandleType
	{
		None,
		MoveFree,
		MoveX,
		MoveY,
		Rotate,
		ScaleTopLeft,
		ScaleTop,
		ScaleTopRight,
		ScaleRight,
		ScaleBottomRight,
		ScaleBottom,
		ScaleBottomLeft,
		ScaleLeft,
		Pivot,
		AnchorTopLeft,
		AnchorTopRight,
		AnchorBottomRight,
		AnchorBottomLeft,
		ControlSizeTopLeft,
		ControlSizeTop,
		ControlSizeTopRight,
		ControlSizeRight,
		ControlSizeBottomRight,
		ControlSizeBottom,
		ControlSizeBottomLeft,
		ControlSizeLeft
	}

	private const string EditLockMeta = "_edit_lock_";

	private const string EditGroupMeta = "_edit_group_";

	private const float MinZoom = 0.05f;

	private const float MaxZoom = 32f;

	private const float DragThreshold = 6f;

	private const float RulerWidth = 16f;

	private const float NavigationPreviewMargin = 12f;

	private const float NavigationPreviewMinWidth = 120f;

	private const float NavigationPreviewMaxWidth = 220f;

	private const float NavigationPreviewAspect = 0.62f;

	private const float NavigationPreviewHandleSize = 8f;

	private const float NavigationPreviewMinWorldSize = 8f;

	private const float EditablePointHandleRadius = 5f;

	private const float EditablePointHitRadius = 9f;

	private const float BezierHandleRadius = 4.5f;

	private const float DefaultBezierHandleLength = 32f;

	private const float GuideHitThresholdPixels = 6f;

	private readonly Color _gridMinorColor = new Color(1f, 1f, 1f, 0.045f);

	private readonly Color _gridMajorColor = new Color(1f, 1f, 1f, 0.095f);

	private readonly Color _axisXColor = new Color(0.95f, 0.35f, 0.35f, 0.55f);

	private readonly Color _axisYColor = new Color(0.35f, 0.95f, 0.45f, 0.55f);

	private readonly Color _selectionColor = new Color(0.337f, 0.62f, 1f, 0.95f);

	private readonly Color _selectionFillColor = new Color(0.337f, 0.62f, 1f, 0.12f);

	private readonly Color _boxSelectionFillColor = new Color(0.337f, 0.62f, 1f, 0.18f);

	private readonly Color _boxSelectionStrokeColor = new Color(0.337f, 0.62f, 1f, 0.95f);

	private readonly Color _lockedColor = new Color(1f, 0.47f, 0.42f, 0.95f);

	private readonly Color _groupColor = new Color(0.45f, 0.95f, 0.5f, 0.95f);

	private readonly Color _editableLineColor = new Color(1f, 0.68f, 0.18f, 0.92f);

	private readonly Color _editablePointColor = new Color(1f, 0.8f, 0.3f, 0.96f);

	private readonly Color _editablePointSelectedColor = new Color(0.37f, 0.72f, 1f);

	private readonly Color _bezierInHandleColor = new Color(0.54f, 0.84f, 1f, 0.98f);

	private readonly Color _bezierOutHandleColor = new Color(1f, 0.48f, 0.66f, 0.98f);

	private readonly Color _guideColor = new Color(0.25f, 0.88f, 1f, 0.88f);

	private readonly Color _snapLineColor = new Color(1f, 0.76f, 0.16f, 0.96f);

	private readonly Color _localXAxisColor = new Color(1f, 0.35f, 0.3f, 0.96f);

	private readonly Color _localYAxisColor = new Color(0.32f, 0.95f, 0.48f, 0.96f);

	private readonly Color _rulerMeasureColor = new Color(1f, 0.72f, 0.18f, 0.98f);

	private SubViewportContainer _sceneViewportContainer;

	private SubViewport _sceneViewport;

	private Node2D _sceneContainer;

	private XW2DViewportOverlay _overlay;

	private Node _sceneInstance;

	private readonly List<CanvasLayer> _canvasLayerCache = new List<CanvasLayer>();

	private bool _isPanning;

	private Vector2 _lastMousePosition;

	private DragMode _dragMode;

	private Vector2 _dragStartViewPosition;

	private Vector2 _dragCurrentViewPosition;

	private Vector2 _dragStartWorldPosition;

	private Vector2 _dragCurrentWorldPosition;

	private Vector2 _dragPivotWorldPosition;

	private bool _dragAppendSelection;

	private readonly System.Collections.Generic.Dictionary<Node, DragTransformState> _dragStartStates = new System.Collections.Generic.Dictionary<Node, DragTransformState>();

	private readonly System.Collections.Generic.Dictionary<Node, PivotTransformState> _pivotStartStates = new System.Collections.Generic.Dictionary<Node, PivotTransformState>();

	private readonly System.Collections.Generic.Dictionary<long, DroppedResourceBatch> _droppedResourceBatches = new System.Collections.Generic.Dictionary<long, DroppedResourceBatch>();

	private long _nextDroppedResourceBatchId = 1L;

	private PreviewDragMode _previewDragMode;

	private Rect2 _previewDragPreviewRect;

	private Rect2 _previewDragWorldRect;

	private Vector2 _previewResizeAnchorWorld;

	private PointEditMode _pointEditMode;

	private Node _pointEditNode;

	private int _pointEditIndex = -1;

	private Variant _pointEditBeforeState;

	private Node _pointEditBeforeSelectedNode;

	private int _pointEditBeforeSelectedIndex = -1;

	private bool _pointEditCreatedPoint;

	private Node _selectedPointNode;

	private int _selectedPointIndex = -1;

	private PopupMenu _listSelectMenu;

	private Texture2D _inlineTextEditIcon;

	private CanvasItem _inlineTextDoubleClickCandidate;

	private bool _inlineTextDoubleClickCandidateCaptured;

	private ulong _inlineTextDoubleClickCandidateTicks;

	private Vector2 _inlineTextDoubleClickCandidateGlobalPosition;

	private readonly List<CanvasItem> _listSelectCandidates = new List<CanvasItem>();

	private readonly List<float> _verticalGuides = new List<float>();

	private readonly List<float> _horizontalGuides = new List<float>();

	private GuideAxis _guideDragAxis;

	private int _guideDragIndex = -1;

	private float _guideDragWorldPosition;

	private float? _activeSnapX;

	private float? _activeSnapY;

	private readonly List<float> _smartSnapXTargets = new List<float>();

	private readonly List<float> _smartSnapYTargets = new List<float>();

	private Rect2 _dragStartSelectionBounds;

	private bool _rulerDragging;

	private bool _rulerMeasurementVisible;

	private Vector2 _rulerStartWorld;

	private Vector2 _rulerEndWorld;

	private Vector2 _moveSnapStep = new Vector2(8f, 8f);

	private float _rotationSnapStepDegrees = 15f;

	private float _scaleSnapStep = 0.1f;

	private float _smartSnapThreshold = 8f;

	private bool _showHelpers;

	private const float GizmoHandleRadius = 6f;

	private const float GizmoHitRadius = 10f;

	private const float MoveAxisLength = 46f;

	private const float RotateRingPadding = 22f;

	private const float RotateRingMinimumRadius = 38f;

	private GizmoHandleType _activeGizmoHandle;

	private GizmoHandleType _hoveredGizmoHandle;

	private Vector2 _gizmoAxisXWorld = Vector2.Right;

	private Vector2 _gizmoAxisYWorld = Vector2.Down;

	private Vector2 _gizmoScaleAnchorWorld;

	private Vector2 _gizmoScaleStartVector;

	private Control _gizmoControl;

	private Rect2 _gizmoControlStartRect;

	private Vector2 _gizmoControlParentSize;

	private static readonly GizmoHandleType[] ScaleHandles = new GizmoHandleType[8]
	{
		GizmoHandleType.ScaleTopLeft,
		GizmoHandleType.ScaleTop,
		GizmoHandleType.ScaleTopRight,
		GizmoHandleType.ScaleRight,
		GizmoHandleType.ScaleBottomRight,
		GizmoHandleType.ScaleBottom,
		GizmoHandleType.ScaleBottomLeft,
		GizmoHandleType.ScaleLeft
	};

	private static readonly GizmoHandleType[] AnchorHandles = new GizmoHandleType[4]
	{
		GizmoHandleType.AnchorTopLeft,
		GizmoHandleType.AnchorTopRight,
		GizmoHandleType.AnchorBottomRight,
		GizmoHandleType.AnchorBottomLeft
	};

	private static readonly GizmoHandleType[] ControlSizeHandles = new GizmoHandleType[8]
	{
		GizmoHandleType.ControlSizeTopLeft,
		GizmoHandleType.ControlSizeTop,
		GizmoHandleType.ControlSizeTopRight,
		GizmoHandleType.ControlSizeRight,
		GizmoHandleType.ControlSizeBottomRight,
		GizmoHandleType.ControlSizeBottom,
		GizmoHandleType.ControlSizeBottomLeft,
		GizmoHandleType.ControlSizeLeft
	};

	private static readonly GizmoHandleType[] SelectControlHandles = new GizmoHandleType[15]
	{
		GizmoHandleType.ControlSizeTopLeft,
		GizmoHandleType.ControlSizeTop,
		GizmoHandleType.ControlSizeTopRight,
		GizmoHandleType.ControlSizeRight,
		GizmoHandleType.ControlSizeBottomRight,
		GizmoHandleType.ControlSizeBottom,
		GizmoHandleType.ControlSizeBottomLeft,
		GizmoHandleType.ControlSizeLeft,
		GizmoHandleType.AnchorTopLeft,
		GizmoHandleType.AnchorTopRight,
		GizmoHandleType.AnchorBottomRight,
		GizmoHandleType.AnchorBottomLeft,
		GizmoHandleType.MoveX,
		GizmoHandleType.MoveY,
		GizmoHandleType.MoveFree
	};

	public int CanvasLayerCacheRebuildCount { get; private set; }

	public XW2DSceneEditor Editor { get; private set; }

	public ToolMode CurrentToolMode { get; private set; }

	public float Zoom { get; private set; } = 1f;

	public Vector2 ViewOffset { get; private set; } = Vector2.Zero;

	public Vector2 GridOffset { get; set; } = Vector2.Zero;

	public Vector2 GridStep { get; set; } = new Vector2(8f, 8f);

	public Vector2I PrimaryGridStep { get; set; } = new Vector2I(8, 8);

	public GridVisibility GridMode { get; private set; } = GridVisibility.ShowWhenSnapping;

	public bool ShowRulers { get; set; } = true;

	public bool ShowGuides { get; set; } = true;

	public bool ShowOrigin { get; set; } = true;

	public bool ShowViewportRect { get; set; } = true;

	public bool ShowHelpers
	{
		get
		{
			return _showHelpers;
		}
		set
		{
			SetShowHelpers(value);
		}
	}

	public bool SmartSnapActive { get; set; }

	public bool GridSnapActive { get; set; }

	public bool UseLocalSpace { get; set; }

	public Vector2 MoveSnapStep
	{
		get
		{
			return _moveSnapStep;
		}
		set
		{
			Vector2 vector = new Vector2(Mathf.Clamp(value.X, 0f, 100000f), Mathf.Clamp(value.Y, 0f, 100000f));
			if (!_moveSnapStep.IsEqualApprox(vector))
			{
				_moveSnapStep = vector;
				QueueRedrawAll();
			}
		}
	}

	public float RotationSnapStepDegrees
	{
		get
		{
			return _rotationSnapStepDegrees;
		}
		set
		{
			float num = Mathf.Clamp(value, 0f, 360f);
			if (!Mathf.IsEqualApprox(_rotationSnapStepDegrees, num))
			{
				_rotationSnapStepDegrees = num;
				QueueRedrawAll();
			}
		}
	}

	public float ScaleSnapStep
	{
		get
		{
			return _scaleSnapStep;
		}
		set
		{
			float num = Mathf.Clamp(value, 0f, 10f);
			if (!Mathf.IsEqualApprox(_scaleSnapStep, num))
			{
				_scaleSnapStep = num;
				QueueRedrawAll();
			}
		}
	}

	public float SmartSnapThreshold
	{
		get
		{
			return _smartSnapThreshold;
		}
		set
		{
			float num = Mathf.Clamp(value, 0f, 128f);
			if (!Mathf.IsEqualApprox(_smartSnapThreshold, num))
			{
				_smartSnapThreshold = num;
				QueueRedrawAll();
			}
		}
	}

	public float SmartSnapThresholdPixels
	{
		get
		{
			return SmartSnapThreshold;
		}
		set
		{
			SmartSnapThreshold = value;
		}
	}

	public int VerticalGuideCount => _verticalGuides.Count;

	public int HorizontalGuideCount => _horizontalGuides.Count;

	public bool HasActiveSmartSnap
	{
		get
		{
			if (!_activeSnapX.HasValue)
			{
				return _activeSnapY.HasValue;
			}
			return true;
		}
	}

	public bool RulerMeasurementVisible => _rulerMeasurementVisible;

	public Vector2 RulerStartWorld => _rulerStartWorld;

	public Vector2 RulerEndWorld => _rulerEndWorld;

	public float RulerLength => _rulerStartWorld.DistanceTo(_rulerEndWorld);

	public float RulerAngleDegrees => Mathf.RadToDeg((_rulerEndWorld - _rulerStartWorld).Angle());

	public int ListSelectCandidateCount => _listSelectCandidates.Count;

	public GizmoHandleType ActiveGizmoHandle => _activeGizmoHandle;

	public GizmoHandleType CurrentGizmoHandleType => _activeGizmoHandle;

	public bool IsGizmoInteractionActive => _activeGizmoHandle != GizmoHandleType.None;

	public bool IsInputIdle
	{
		get
		{
			if (_activeGizmoHandle == GizmoHandleType.None && _dragMode == DragMode.None && !_isPanning && _pointEditMode == PointEditMode.None && _guideDragAxis == GuideAxis.None)
			{
				return _previewDragMode == PreviewDragMode.None;
			}
			return false;
		}
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		ReleaseDroppedResourceBatchesIfHistoryWasCleared();
		if (_sceneInstance == null || !GodotObject.IsInstanceValid(_sceneInstance))
		{
			return false;
		}
		foreach (string item in XWFileSystemDropHelper.ExtractDropPaths(data))
		{
			if (CanCreateCanvasNodeFromResourcePath(item))
			{
				return true;
			}
		}
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (_sceneInstance == null || !GodotObject.IsInstanceValid(_sceneInstance))
		{
			return;
		}
		int num = CurrentSceneHistoryId();
		DroppedResourceBatch droppedResourceBatch = new DroppedResourceBatch
		{
			HistoryId = num,
			Parent = _sceneInstance
		};
		Vector2 vector = ApplyGridSnapToWorld(ViewToWorld(atPosition));
		int num2 = 0;
		foreach (string item in XWFileSystemDropHelper.ExtractDropPaths(data))
		{
			Node node = CreateCanvasNodeFromResourcePath(item);
			if (node != null)
			{
				droppedResourceBatch.Nodes.Add(node);
				droppedResourceBatch.WorldPositions.Add(vector + new Vector2((float)num2 * 24f, (float)num2 * 24f));
				num2++;
			}
		}
		if (droppedResourceBatch.Nodes.Count == 0)
		{
			XWEditorInterface.Instance?.ShowToast("拖入的资源不能创建 2D 节点");
			return;
		}
		long from = _nextDroppedResourceBatchId++;
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		PrepareForSceneHistoryBranchChange();
		XWEditorInterface.Instance?.GetSceneTreeDock()?.PrepareForSceneHistoryBranchChange();
		_droppedResourceBatches[from] = droppedResourceBatch;
		if (GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			string name = ((droppedResourceBatch.Nodes.Count == 1) ? "拖入资源到 2D 画布" : $"拖入 {droppedResourceBatch.Nodes.Count} 个资源到 2D 画布");
			xWUndoRedoManager.CreateAction(name, mergeMode: false, num);
			xWUndoRedoManager.AddDoMethod(this, "ApplyDroppedResourceBatch", Variant.From(in from));
			xWUndoRedoManager.AddUndoMethod(this, "RemoveDroppedResourceBatch", Variant.From(in from));
			xWUndoRedoManager.CommitAction();
			ActivateSceneTransformHistoryDeferred();
		}
		else
		{
			ApplyDroppedResourceBatch(from);
		}
		XWEditorInterface.Instance?.ShowToast((droppedResourceBatch.Nodes.Count == 1) ? $"已在画布创建 {droppedResourceBatch.Nodes[0].Name}" : $"已在画布创建 {droppedResourceBatch.Nodes.Count} 个资源节点");
	}

	public void ApplyDroppedResourceBatch(long batchId)
	{
		if (!_droppedResourceBatches.TryGetValue(batchId, out var value) || value.Parent == null || !GodotObject.IsInstanceValid(value.Parent))
		{
			return;
		}
		Node lastAdded = null;
		for (int i = 0; i < value.Nodes.Count; i++)
		{
			Node node = value.Nodes[i];
			if (node != null && GodotObject.IsInstanceValid(node))
			{
				node.GetParent()?.RemoveChild(node);
				value.Parent.AddChild(node, forceReadableName: true, InternalMode.Disabled);
				AssignOwnerForAddedNode(node, value.Parent);
				SetCanvasNodeWorldPosition(node, value.WorldPositions[i]);
				lastAdded = node;
			}
		}
		value.Applied = true;
		Editor?.RefreshSceneTree();
		SelectDroppedResourceNodes(value.Nodes, lastAdded);
		NotifySceneStructureChanged();
	}

	public void RemoveDroppedResourceBatch(long batchId)
	{
		if (!_droppedResourceBatches.TryGetValue(batchId, out var value))
		{
			return;
		}
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		foreach (Node node in value.Nodes)
		{
			if (node != null && GodotObject.IsInstanceValid(node))
			{
				if (xWEditorSelection != null && xWEditorSelection.IsSelected(node))
				{
					xWEditorSelection.DeselectNode(node);
				}
				node.GetParent()?.RemoveChild(node);
			}
		}
		value.Applied = false;
		Editor?.RefreshSceneTree();
		if (value.Parent != null && GodotObject.IsInstanceValid(value.Parent))
		{
			xWEditorSelection?.Clear();
			xWEditorSelection?.SelectNode(value.Parent);
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(value.Parent, emitSignal: false);
		}
		NotifySceneStructureChanged();
	}

	private static bool CanCreateCanvasNodeFromResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path) || DirAccess.DirExistsAbsolute(path))
		{
			return false;
		}
		string text = path.GetExtension().ToLowerInvariant();
		if ((text == "tscn" || text == "scn") ? true : false)
		{
			return Is2DPackedScene(ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Reuse));
		}
		if (text != null)
		{
			int length = text.Length;
			if (length != 3)
			{
				if (length == 4)
				{
					char c = text[0];
					if (c != 'j')
					{
						if (c != 'k')
						{
							if (c == 'w' && text == "webp")
							{
								goto IL_0209;
							}
						}
						else if (text == "ktx2")
						{
							goto IL_0209;
						}
					}
					else if (text == "jpeg")
					{
						goto IL_0209;
					}
				}
			}
			else
			{
				switch (text[0])
				{
				case 'p':
					break;
				case 'j':
					goto IL_011b;
				case 's':
					goto IL_0130;
				case 'b':
					goto IL_0145;
				case 't':
					goto IL_015a;
				case 'd':
					goto IL_016f;
				case 'k':
					goto IL_0184;
				case 'e':
					goto IL_0193;
				case 'h':
					goto IL_01a2;
				case 'w':
					goto IL_01b1;
				case 'o':
					goto IL_01c0;
				case 'm':
					goto IL_01cf;
				default:
					goto IL_020d;
				}
				if (text == "png")
				{
					goto IL_0209;
				}
			}
		}
		goto IL_020d;
		IL_01cf:
		if (text == "mp3")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_01c0:
		if (text == "ogg")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_015a:
		if (text == "tga")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_01b1:
		if (text == "wav")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_0130:
		if (text == "svg")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_0184:
		if (text == "ktx")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_020d:
		bool flag = false;
		goto IL_020f;
		IL_0209:
		flag = true;
		goto IL_020f;
		IL_020f:
		if (flag)
		{
			return ResourceLoader.Exists(path);
		}
		flag = ((text == "tres" || text == "res") ? true : false);
		if (!flag || !ResourceLoader.Exists(path))
		{
			return false;
		}
		Resource resource = ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Reuse);
		if ((!(resource is Texture2D) && !(resource is AudioStream)) || 1 == 0)
		{
			if (resource is PackedScene scene)
			{
				return Is2DPackedScene(scene);
			}
			return false;
		}
		return true;
		IL_0145:
		if (text == "bmp")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_016f:
		if (text == "dds")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_0193:
		if (text == "exr")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_01a2:
		if (text == "hdr")
		{
			goto IL_0209;
		}
		goto IL_020d;
		IL_011b:
		if (text == "jpg")
		{
			goto IL_0209;
		}
		goto IL_020d;
	}

	private static Node CreateCanvasNodeFromResourcePath(string path)
	{
		if (!CanCreateCanvasNodeFromResourcePath(path))
		{
			return null;
		}
		Resource resource = ResourceLoader.Load(path, "", ResourceLoader.CacheMode.Ignore);
		CanvasItem canvasItem;
		if (!(resource is PackedScene packedScene))
		{
			if (!(resource is Texture2D texture))
			{
				if (!(resource is AudioStream stream))
				{
					goto IL_007c;
				}
				canvasItem = new AudioStreamPlayer2D
				{
					Stream = stream,
					Autoplay = false
				};
			}
			else
			{
				canvasItem = new Sprite2D
				{
					Texture = texture
				};
			}
		}
		else
		{
			if (!Is2DPackedScene(packedScene))
			{
				goto IL_007c;
			}
			canvasItem = packedScene.Instantiate(PackedScene.GenEditState.Disabled) as CanvasItem;
		}
		goto IL_007f;
		IL_007c:
		canvasItem = null;
		goto IL_007f;
		IL_007f:
		Node node = canvasItem;
		if (node == null)
		{
			return null;
		}
		node.Name = path.GetFile().GetBaseName();
		return node;
	}

	private static bool Is2DPackedScene(PackedScene scene)
	{
		if (scene == null || !GodotObject.IsInstanceValid(scene))
		{
			return false;
		}
		SceneState state = scene.GetState();
		if (state == null || !GodotObject.IsInstanceValid(state) || state.GetNodeCount() == 0)
		{
			return false;
		}
		string text = state.GetNodeType(0).ToString();
		if (ClassDB.ClassExists(text))
		{
			return ClassDB.IsParentClass(text, "CanvasItem");
		}
		return false;
	}

	private void SetCanvasNodeWorldPosition(Node node, Vector2 worldPosition)
	{
		Vector2 position = worldPosition;
		if (node.GetParent() is CanvasItem canvasItem && _sceneContainer != null && GodotObject.IsInstanceValid(_sceneContainer))
		{
			Vector2 vector = _sceneContainer.GetGlobalTransform() * worldPosition;
			position = canvasItem.GetGlobalTransform().AffineInverse() * vector;
		}
		if (node is Node2D node2D)
		{
			node2D.Position = position;
		}
		else if (node is Control control)
		{
			control.Position = position;
		}
	}

	private void SelectDroppedResourceNodes(List<Node> nodes, Node lastAdded)
	{
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		xWEditorSelection?.Clear();
		foreach (Node node in nodes)
		{
			if (node != null && GodotObject.IsInstanceValid(node))
			{
				xWEditorSelection?.SelectNode(node);
			}
		}
		if (lastAdded != null && GodotObject.IsInstanceValid(lastAdded))
		{
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(lastAdded, emitSignal: false);
		}
		EmitSignal(SignalName.SelectionChanged);
	}

	private void ReleaseDroppedResourceBatches(bool releaseAll, int historyId = -1)
	{
		List<long> list = new List<long>();
		foreach (var (item, droppedResourceBatch2) in _droppedResourceBatches)
		{
			if ((historyId >= 0 && droppedResourceBatch2.HistoryId != historyId) || (!releaseAll && droppedResourceBatch2.Applied))
			{
				continue;
			}
			foreach (Node node in droppedResourceBatch2.Nodes)
			{
				if (node != null && GodotObject.IsInstanceValid(node) && node.GetParent() == null)
				{
					node.Free();
				}
			}
			list.Add(item);
		}
		foreach (long item2 in list)
		{
			_droppedResourceBatches.Remove(item2);
		}
	}

	private void ReleaseDroppedResourceBatchesIfHistoryWasCleared()
	{
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		foreach (DroppedResourceBatch value in _droppedResourceBatches.Values)
		{
			if (!xWUndoRedoManager.HasHistory(value.HistoryId))
			{
				hashSet.Add(value.HistoryId);
			}
		}
		foreach (int item in hashSet)
		{
			ReleaseDroppedResourceBatches(releaseAll: true, item);
		}
	}

	public void ReleaseHistoryResources(int historyId)
	{
		ReleaseDroppedResourceBatches(releaseAll: true, historyId);
	}

	public int GetTrackedDroppedResourceBatchCount(int historyId = -1)
	{
		if (historyId < 0)
		{
			return _droppedResourceBatches.Count;
		}
		int num = 0;
		foreach (DroppedResourceBatch value in _droppedResourceBatches.Values)
		{
			if (value.HistoryId == historyId)
			{
				num++;
			}
		}
		return num;
	}

	public void PrepareForSceneHistoryBranchChange()
	{
		ReleaseDroppedResourceBatchesIfHistoryWasCleared();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		int num = CurrentSceneHistoryId();
		if (GodotObject.IsInstanceValid(xWUndoRedoManager) && xWUndoRedoManager.HasRedo(num))
		{
			ReleaseDroppedResourceBatches(releaseAll: false, num);
		}
	}

	private int CurrentSceneHistoryId()
	{
		if (!GodotObject.IsInstanceValid(Editor))
		{
			return XWEditorInterface.Instance?.GetCurrentSceneHistoryId() ?? 1;
		}
		return Editor.GetCurrentSceneHistoryId();
	}

	public Node GetSceneInstance()
	{
		return _sceneInstance;
	}

	private void DrawEditablePoints(Control canvas)
	{
		if (!IsPointToolMode())
		{
			return;
		}
		Node editablePointNode = GetEditablePointNode();
		if (!(editablePointNode is CanvasItem) || !GodotObject.IsInstanceValid(editablePointNode))
		{
			return;
		}
		int editablePointCount = GetEditablePointCount(editablePointNode);
		if (editablePointCount <= 0)
		{
			return;
		}
		Vector2[] array = new Vector2[editablePointCount];
		for (int i = 0; i < editablePointCount; i++)
		{
			array[i] = WorldToView(GetEditablePointWorldPosition(editablePointNode, i));
		}
		if (editablePointNode is Path2D { Curve: not null } path2D)
		{
			Vector2[] array2 = path2D.Curve.Tessellate();
			if (array2.Length > 1)
			{
				Vector2[] array3 = new Vector2[array2.Length];
				for (int j = 0; j < array2.Length; j++)
				{
					array3[j] = WorldToView(NodeLocalToSceneWorld(path2D, array2[j]));
				}
				canvas.DrawPolyline(array3, _editableLineColor, 2f, antialiased: true);
			}
		}
		else if (editablePointCount > 1)
		{
			canvas.DrawPolyline(array, _editableLineColor, 2f, antialiased: true);
			if (IsClosedPointNode(editablePointNode) && editablePointCount > 2)
			{
				canvas.DrawLine(array[^1], array[0], _editableLineColor, 2f, antialiased: true);
			}
		}
		for (int k = 0; k < editablePointCount; k++)
		{
			bool flag = GodotObject.IsInstanceValid(_selectedPointNode) && _selectedPointNode == editablePointNode && _selectedPointIndex == k;
			canvas.DrawCircle(array[k], 7f, new Color(0f, 0f, 0f, 0.55f));
			canvas.DrawCircle(array[k], 5f, flag ? _editablePointSelectedColor : _editablePointColor);
		}
		if (editablePointNode is Path2D path2D2 && _selectedPointNode == path2D2 && _selectedPointIndex >= 0 && _selectedPointIndex < path2D2.Curve.PointCount)
		{
			DrawPathTangentHandles(canvas, path2D2, _selectedPointIndex);
		}
	}

	private void DrawPathTangentHandles(Control canvas, Path2D path, int index)
	{
		Vector2 vector = WorldToView(GetEditablePointWorldPosition(path, index));
		Vector2 vector2 = WorldToView(GetPathTangentHandleWorldPosition(path, index, incoming: true));
		Vector2 vector3 = WorldToView(GetPathTangentHandleWorldPosition(path, index, incoming: false));
		canvas.DrawLine(vector, vector2, _bezierInHandleColor, 1.5f, antialiased: true);
		canvas.DrawLine(vector, vector3, _bezierOutHandleColor, 1.5f, antialiased: true);
		canvas.DrawCircle(vector2, 6f, new Color(0f, 0f, 0f, 0.58f));
		canvas.DrawCircle(vector3, 6f, new Color(0f, 0f, 0f, 0.58f));
		canvas.DrawCircle(vector2, 4.5f, _bezierInHandleColor);
		canvas.DrawCircle(vector3, 4.5f, _bezierOutHandleColor);
	}

	private bool TryBeginPointEdit(Vector2 viewPosition)
	{
		if (!IsPointToolMode())
		{
			return false;
		}
		Node editablePointNode = GetEditablePointNode();
		if (editablePointNode == null || !GodotObject.IsInstanceValid(editablePointNode) || IsNodeLocked(editablePointNode))
		{
			return false;
		}
		Variant beforeState = CaptureEditablePointState(editablePointNode);
		Node selectedPointNode = _selectedPointNode;
		int selectedPointIndex = _selectedPointIndex;
		if (editablePointNode is Path2D path && TryHitPathTangentHandle(path, viewPosition, out var index, out var incoming))
		{
			StartPathTangentEditDrag(path, index, incoming, beforeState, selectedPointNode, selectedPointIndex);
			return true;
		}
		if (TryHitEditablePoint(editablePointNode, viewPosition, out var index2))
		{
			StartPointEditDrag(editablePointNode, index2, beforeState, selectedPointNode, selectedPointIndex, createdPoint: false);
			return true;
		}
		Vector2 worldPosition = ApplyGridSnapToWorld(ViewToWorld(viewPosition));
		if (CurrentToolMode == ToolMode.Path && editablePointNode is Path2D path2)
		{
			index2 = AddPathPoint(path2, worldPosition);
		}
		else
		{
			if (CurrentToolMode != ToolMode.Polygon)
			{
				return false;
			}
			index2 = AddPolygonPoint(editablePointNode, worldPosition);
		}
		if (index2 < 0)
		{
			return false;
		}
		StartPointEditDrag(editablePointNode, index2, beforeState, selectedPointNode, selectedPointIndex, createdPoint: true);
		Editor?.RefreshSceneTree();
		QueueRedrawAll();
		return true;
	}

	private void StartPointEditDrag(Node node, int index, Variant beforeState, Node beforeSelectedNode, int beforeSelectedIndex, bool createdPoint)
	{
		_pointEditNode = node;
		_pointEditIndex = index;
		_pointEditBeforeState = beforeState;
		_pointEditBeforeSelectedNode = beforeSelectedNode;
		_pointEditBeforeSelectedIndex = beforeSelectedIndex;
		_pointEditCreatedPoint = createdPoint;
		_pointEditMode = ((CurrentToolMode == ToolMode.Path) ? PointEditMode.MovePathPoint : PointEditMode.MovePolygonPoint);
		SelectEditablePoint(node, index);
	}

	private void StartPathTangentEditDrag(Path2D path, int index, bool incoming, Variant beforeState, Node beforeSelectedNode, int beforeSelectedIndex)
	{
		_pointEditNode = path;
		_pointEditIndex = index;
		_pointEditBeforeState = beforeState;
		_pointEditBeforeSelectedNode = beforeSelectedNode;
		_pointEditBeforeSelectedIndex = beforeSelectedIndex;
		_pointEditCreatedPoint = false;
		_pointEditMode = (incoming ? PointEditMode.MovePathInHandle : PointEditMode.MovePathOutHandle);
		SelectEditablePoint(path, index);
	}

	private void UpdatePointEditDrag(Vector2 viewPosition)
	{
		if (_pointEditNode == null || !GodotObject.IsInstanceValid(_pointEditNode) || _pointEditIndex < 0)
		{
			CancelPointEdit();
			return;
		}
		Path2D path2D = _pointEditNode as Path2D;
		bool flag = path2D != null;
		if (flag)
		{
			PointEditMode pointEditMode = _pointEditMode;
			bool flag2 = (uint)(pointEditMode - 2) <= 1u;
			flag = flag2;
		}
		if (flag)
		{
			SetPathTangentHandleWorldPosition(path2D, _pointEditIndex, _pointEditMode == PointEditMode.MovePathInHandle, ViewToWorld(viewPosition));
		}
		else
		{
			SetEditablePointWorldPosition(_pointEditNode, _pointEditIndex, ApplyGridSnapToWorld(ViewToWorld(viewPosition)));
		}
		QueueRedrawAll();
	}

	private void FinishPointEditDrag()
	{
		CommitPointEditDragHistory();
		ResetPointEditSession();
		Editor?.RefreshSceneTree();
		QueueRedrawAll();
	}

	private void CancelPointEdit()
	{
		if (_pointEditMode != PointEditMode.None && _pointEditNode != null && GodotObject.IsInstanceValid(_pointEditNode))
		{
			int selectedIndex = ((_pointEditBeforeSelectedNode == _pointEditNode) ? _pointEditBeforeSelectedIndex : (-1));
			ApplyEditablePointState(_pointEditNode, _pointEditBeforeState, selectedIndex);
		}
		ResetPointEditSession();
		QueueRedrawAll();
	}

	private bool TryRemoveEditablePointAt(Vector2 viewPosition)
	{
		if (!IsPointToolMode())
		{
			return false;
		}
		Node editablePointNode = GetEditablePointNode();
		if (editablePointNode == null || !GodotObject.IsInstanceValid(editablePointNode) || IsNodeLocked(editablePointNode))
		{
			return false;
		}
		if (!TryHitEditablePoint(editablePointNode, viewPosition, out var index))
		{
			return false;
		}
		return RemoveEditablePointWithHistory(editablePointNode, index);
	}

	private bool RemoveSelectedEditablePoint()
	{
		if (_selectedPointNode == null || !GodotObject.IsInstanceValid(_selectedPointNode) || _selectedPointIndex < 0)
		{
			return false;
		}
		if (!IsEditablePointNodeForCurrentTool(_selectedPointNode))
		{
			return false;
		}
		return RemoveEditablePointWithHistory(_selectedPointNode, _selectedPointIndex);
	}

	private bool TryHitEditablePoint(Node node, Vector2 viewPosition, out int index)
	{
		index = -1;
		for (int num = GetEditablePointCount(node) - 1; num >= 0; num--)
		{
			if (WorldToView(GetEditablePointWorldPosition(node, num)).DistanceTo(viewPosition) <= 9f)
			{
				index = num;
				return true;
			}
		}
		return false;
	}

	private bool TryHitPathTangentHandle(Path2D path, Vector2 viewPosition, out int index, out bool incoming)
	{
		index = -1;
		incoming = false;
		if (path?.Curve == null || _selectedPointNode != path || _selectedPointIndex < 0 || _selectedPointIndex >= path.Curve.PointCount)
		{
			return false;
		}
		int selectedPointIndex = _selectedPointIndex;
		Vector2 vector = WorldToView(GetPathTangentHandleWorldPosition(path, selectedPointIndex, incoming: true));
		Vector2 vector2 = WorldToView(GetPathTangentHandleWorldPosition(path, selectedPointIndex, incoming: false));
		if (vector.DistanceTo(viewPosition) <= 9f)
		{
			index = selectedPointIndex;
			incoming = true;
			return true;
		}
		if (vector2.DistanceTo(viewPosition) <= 9f)
		{
			index = selectedPointIndex;
			return true;
		}
		return false;
	}

	private int AddPathPoint(Path2D path, Vector2 worldPosition)
	{
		if (path == null || !GodotObject.IsInstanceValid(path))
		{
			return -1;
		}
		if (path.Curve == null)
		{
			Curve2D curve2D = (path.Curve = new Curve2D());
		}
		path.Curve.AddPoint(SceneWorldToNodeLocal(path, worldPosition));
		int num = path.Curve.PointCount - 1;
		SelectEditablePoint(path, num);
		return num;
	}

	private int AddPolygonPoint(Node polygonNode, Vector2 worldPosition)
	{
		if (!(polygonNode is CanvasItem canvasItem) || !GodotObject.IsInstanceValid(canvasItem))
		{
			return -1;
		}
		Vector2 point = SceneWorldToNodeLocal(canvasItem, worldPosition);
		if (polygonNode is Polygon2D polygon2D)
		{
			polygon2D.Polygon = AppendPoint(polygon2D.Polygon, point);
			int num = polygon2D.Polygon.Length - 1;
			SelectEditablePoint(polygonNode, num);
			return num;
		}
		if (polygonNode is CollisionPolygon2D collisionPolygon2D)
		{
			collisionPolygon2D.Polygon = AppendPoint(collisionPolygon2D.Polygon, point);
			int num2 = collisionPolygon2D.Polygon.Length - 1;
			SelectEditablePoint(polygonNode, num2);
			return num2;
		}
		if (polygonNode is Line2D line2D)
		{
			line2D.Points = AppendPoint(line2D.Points, point);
			int num3 = line2D.Points.Length - 1;
			SelectEditablePoint(polygonNode, num3);
			return num3;
		}
		return -1;
	}

	private void SetEditablePointWorldPosition(Node node, int index, Vector2 worldPosition)
	{
		if (node is CanvasItem item && index >= 0)
		{
			Vector2 vector = SceneWorldToNodeLocal(item, worldPosition);
			if (node is Path2D { Curve: not null } path2D && index < path2D.Curve.PointCount)
			{
				path2D.Curve.SetPointPosition(index, vector);
			}
			else if (node is Polygon2D polygon2D && index < polygon2D.Polygon.Length)
			{
				polygon2D.Polygon = SetPoint(polygon2D.Polygon, index, vector);
			}
			else if (node is CollisionPolygon2D collisionPolygon2D && index < collisionPolygon2D.Polygon.Length)
			{
				collisionPolygon2D.Polygon = SetPoint(collisionPolygon2D.Polygon, index, vector);
			}
			else if (node is Line2D line2D && index < line2D.Points.Length)
			{
				line2D.Points = SetPoint(line2D.Points, index, vector);
			}
		}
	}

	private Vector2 GetPathTangentHandleWorldPosition(Path2D path, int index, bool incoming)
	{
		if (path?.Curve == null || index < 0 || index >= path.Curve.PointCount)
		{
			return Vector2.Zero;
		}
		Vector2 vector = (incoming ? path.Curve.GetPointIn(index) : path.Curve.GetPointOut(index));
		if (vector.LengthSquared() <= 0.001f)
		{
			vector = new Vector2(incoming ? (-32f) : 32f, 0f);
		}
		Vector2 localPosition = path.Curve.GetPointPosition(index) + vector;
		return NodeLocalToSceneWorld(path, localPosition);
	}

	private void SetPathTangentHandleWorldPosition(Path2D path, int index, bool incoming, Vector2 worldPosition)
	{
		if (path?.Curve != null && index >= 0 && index < path.Curve.PointCount)
		{
			Vector2 position = SceneWorldToNodeLocal(path, worldPosition) - path.Curve.GetPointPosition(index);
			if (incoming)
			{
				path.Curve.SetPointIn(index, position);
			}
			else
			{
				path.Curve.SetPointOut(index, position);
			}
		}
	}

	private void RemoveEditablePoint(Node node, int index)
	{
		if (index >= 0)
		{
			if (node is Path2D { Curve: not null } path2D && index < path2D.Curve.PointCount)
			{
				path2D.Curve.RemovePoint(index);
			}
			else if (node is Polygon2D polygon2D && index < polygon2D.Polygon.Length)
			{
				polygon2D.Polygon = RemovePoint(polygon2D.Polygon, index);
			}
			else if (node is CollisionPolygon2D collisionPolygon2D && index < collisionPolygon2D.Polygon.Length)
			{
				collisionPolygon2D.Polygon = RemovePoint(collisionPolygon2D.Polygon, index);
			}
			else if (node is Line2D line2D && index < line2D.Points.Length)
			{
				line2D.Points = RemovePoint(line2D.Points, index);
			}
		}
	}

	private void CommitPointEditDragHistory()
	{
		if (_pointEditNode != null && GodotObject.IsInstanceValid(_pointEditNode))
		{
			Variant variant = CaptureEditablePointState(_pointEditNode);
			if (!AreEditablePointStatesEqual(_pointEditNode, _pointEditBeforeState, variant))
			{
				int beforeSelectedIndex = ((_pointEditBeforeSelectedNode == _pointEditNode) ? _pointEditBeforeSelectedIndex : (-1));
				string actionName = _pointEditMode switch
				{
					PointEditMode.MovePathInHandle => "调整 2D 贝塞尔入切线", 
					PointEditMode.MovePathOutHandle => "调整 2D 贝塞尔出切线", 
					_ => (!_pointEditCreatedPoint) ? "移动 2D 控制点" : "添加 2D 控制点", 
				};
				CommitEditablePointStateAction(_pointEditNode, actionName, _pointEditBeforeState, beforeSelectedIndex, variant, _pointEditIndex);
			}
		}
	}

	private bool RemoveEditablePointWithHistory(Node node, int index)
	{
		if (node == null || !GodotObject.IsInstanceValid(node) || IsNodeLocked(node) || index < 0 || index >= GetEditablePointCount(node))
		{
			return false;
		}
		Variant beforeState = CaptureEditablePointState(node);
		RemoveEditablePoint(node, index);
		Variant afterState = CaptureEditablePointState(node);
		int editablePointCount = GetEditablePointCount(node);
		int afterSelectedIndex = ((editablePointCount == 0) ? (-1) : Mathf.Min(index, editablePointCount - 1));
		CommitEditablePointStateAction(node, "删除 2D 控制点", beforeState, index, afterState, afterSelectedIndex);
		return true;
	}

	private void CommitEditablePointStateAction(Node node, string actionName, Variant beforeState, int beforeSelectedIndex, Variant afterState, int afterSelectedIndex)
	{
		PrepareForSceneHistoryBranchChange();
		XWEditorInterface.Instance?.GetSceneTreeDock()?.PrepareForSceneHistoryBranchChange();
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			xWUndoRedoManager.CreateAction(actionName, mergeMode: false, CurrentSceneHistoryId());
			xWUndoRedoManager.AddDoMethod(this, "ApplyEditablePointState", Variant.From(in node), afterState, Variant.From(in afterSelectedIndex));
			xWUndoRedoManager.AddUndoMethod(this, "ApplyEditablePointState", Variant.From(in node), beforeState, Variant.From(in beforeSelectedIndex));
			xWUndoRedoManager.CommitAction();
			ActivateSceneTransformHistoryDeferred();
		}
		else
		{
			ApplyEditablePointState(node, afterState, afterSelectedIndex);
		}
	}

	public void ApplyEditablePointState(Node node, Variant state, int selectedIndex)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is Path2D path2D)
		{
			path2D.Curve = ((state.VariantType == Variant.Type.Object) ? state.As<Curve2D>() : null)?.Duplicate(deep: true) as Curve2D;
		}
		else
		{
			Vector2[] array = (Vector2[])((state.VariantType == Variant.Type.PackedVector2Array) ? state.AsVector2Array() : System.Array.Empty<Vector2>()).Clone();
			if (node is Polygon2D polygon2D)
			{
				polygon2D.Polygon = array;
			}
			else if (node is CollisionPolygon2D collisionPolygon2D)
			{
				collisionPolygon2D.Polygon = array;
			}
			else if (node is Line2D line2D)
			{
				line2D.Points = array;
			}
		}
		int editablePointCount = GetEditablePointCount(node);
		_selectedPointNode = ((selectedIndex >= 0 && selectedIndex < editablePointCount) ? node : null);
		_selectedPointIndex = ((_selectedPointNode == null) ? (-1) : selectedIndex);
		Editor?.RefreshSceneTree();
		XWEditorSelection obj = XWEditorInterface.Instance?.GetEditorSelection();
		obj?.Clear();
		obj?.SelectNode(node);
		XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(node, emitSignal: false);
		EmitSignal(SignalName.SelectionChanged);
		Editor?.RefreshDirectTransformSurface();
		QueueRedrawAll();
	}

	private static Variant CaptureEditablePointState(Node node)
	{
		if (node is Path2D path2D)
		{
			Curve2D from = path2D.Curve?.Duplicate(deep: true) as Curve2D;
			if (from != null)
			{
				return Variant.From(in from);
			}
			return default(Variant);
		}
		if (node is Polygon2D polygon2D)
		{
			return Variant.From<Vector2[]>((Vector2[])polygon2D.Polygon.Clone());
		}
		if (node is CollisionPolygon2D collisionPolygon2D)
		{
			return Variant.From<Vector2[]>((Vector2[])collisionPolygon2D.Polygon.Clone());
		}
		if (node is Line2D line2D)
		{
			return Variant.From<Vector2[]>((Vector2[])line2D.Points.Clone());
		}
		return default(Variant);
	}

	private static bool AreEditablePointStatesEqual(Node node, Variant left, Variant right)
	{
		if (node is Path2D)
		{
			Curve2D curve2D = ((left.VariantType == Variant.Type.Object) ? left.As<Curve2D>() : null);
			Curve2D curve2D2 = ((right.VariantType == Variant.Type.Object) ? right.As<Curve2D>() : null);
			if (curve2D == null || curve2D2 == null)
			{
				if (curve2D == null)
				{
					return curve2D2 == null;
				}
				return false;
			}
			if (curve2D.PointCount != curve2D2.PointCount)
			{
				return false;
			}
			for (int i = 0; i < curve2D.PointCount; i++)
			{
				if (!curve2D.GetPointPosition(i).IsEqualApprox(curve2D2.GetPointPosition(i)) || !curve2D.GetPointIn(i).IsEqualApprox(curve2D2.GetPointIn(i)) || !curve2D.GetPointOut(i).IsEqualApprox(curve2D2.GetPointOut(i)))
				{
					return false;
				}
			}
			return true;
		}
		Vector2[] array = ((left.VariantType == Variant.Type.PackedVector2Array) ? left.AsVector2Array() : System.Array.Empty<Vector2>());
		Vector2[] array2 = ((right.VariantType == Variant.Type.PackedVector2Array) ? right.AsVector2Array() : System.Array.Empty<Vector2>());
		if (array.Length != array2.Length)
		{
			return false;
		}
		for (int j = 0; j < array.Length; j++)
		{
			if (!array[j].IsEqualApprox(array2[j]))
			{
				return false;
			}
		}
		return true;
	}

	private void ResetPointEditSession()
	{
		_pointEditMode = PointEditMode.None;
		_pointEditNode = null;
		_pointEditIndex = -1;
		_pointEditBeforeState = default(Variant);
		_pointEditBeforeSelectedNode = null;
		_pointEditBeforeSelectedIndex = -1;
		_pointEditCreatedPoint = false;
	}

	public int GetSelectedEditablePointIndex()
	{
		return _selectedPointIndex;
	}

	private int GetEditablePointCount(Node node)
	{
		if (!(node is Path2D path2D))
		{
			if (!(node is Polygon2D polygon2D))
			{
				if (!(node is CollisionPolygon2D collisionPolygon2D))
				{
					if (node is Line2D line2D)
					{
						Vector2[] points = line2D.Points;
						return (points != null) ? points.Length : 0;
					}
					return 0;
				}
				Vector2[] polygon = collisionPolygon2D.Polygon;
				return (polygon != null) ? polygon.Length : 0;
			}
			Vector2[] polygon2 = polygon2D.Polygon;
			return (polygon2 != null) ? polygon2.Length : 0;
		}
		return path2D.Curve?.PointCount ?? 0;
	}

	private Vector2 GetEditablePointWorldPosition(Node node, int index)
	{
		if (!(node is CanvasItem item) || index < 0)
		{
			return Vector2.Zero;
		}
		if (node is Path2D { Curve: not null } path2D && index < path2D.Curve.PointCount)
		{
			return NodeLocalToSceneWorld(path2D, path2D.Curve.GetPointPosition(index));
		}
		if (node is Polygon2D polygon2D && index < polygon2D.Polygon.Length)
		{
			return NodeLocalToSceneWorld(item, polygon2D.Polygon[index]);
		}
		if (node is CollisionPolygon2D collisionPolygon2D && index < collisionPolygon2D.Polygon.Length)
		{
			return NodeLocalToSceneWorld(item, collisionPolygon2D.Polygon[index]);
		}
		if (node is Line2D line2D && index < line2D.Points.Length)
		{
			return NodeLocalToSceneWorld(item, line2D.Points[index]);
		}
		return Vector2.Zero;
	}

	private Node GetEditablePointNode()
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (IsEditablePointNodeForCurrentTool(item))
			{
				return item;
			}
		}
		Node node = XWEditorInterface.Instance?.GetSceneTreeDock()?.GetSelectedNode();
		if (!IsEditablePointNodeForCurrentTool(node))
		{
			return null;
		}
		return node;
	}

	private bool IsPointToolMode()
	{
		if (CurrentToolMode != ToolMode.Path)
		{
			return CurrentToolMode == ToolMode.Polygon;
		}
		return true;
	}

	private bool IsEditablePointNodeForCurrentTool(Node node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		return CurrentToolMode switch
		{
			ToolMode.Path => node is Path2D, 
			ToolMode.Polygon => (node is Polygon2D || node is CollisionPolygon2D || node is Line2D) ? true : false, 
			_ => false, 
		};
	}

	private static bool IsClosedPointNode(Node node)
	{
		if (node is Polygon2D || node is CollisionPolygon2D)
		{
			return true;
		}
		return false;
	}

	private void SelectEditablePoint(Node node, int index)
	{
		_selectedPointNode = node;
		_selectedPointIndex = index;
	}

	private Vector2 ApplyGridSnapToWorld(Vector2 worldPosition)
	{
		if (!GridSnapActive)
		{
			return worldPosition;
		}
		return new Vector2(SnapValue(worldPosition.X - GridOffset.X, Mathf.Max(GridStep.X, 1f)) + GridOffset.X, SnapValue(worldPosition.Y - GridOffset.Y, Mathf.Max(GridStep.Y, 1f)) + GridOffset.Y);
	}

	private Vector2 NodeLocalToSceneWorld(CanvasItem item, Vector2 localPosition)
	{
		Vector2 canvasPosition = item.GetGlobalTransform() * localPosition;
		return CanvasToSceneWorld(canvasPosition);
	}

	private Vector2 SceneWorldToNodeLocal(CanvasItem item, Vector2 worldPosition)
	{
		Vector2 vector = ((_sceneContainer != null && GodotObject.IsInstanceValid(_sceneContainer)) ? (_sceneContainer.GetGlobalTransform() * worldPosition) : worldPosition);
		return item.GetGlobalTransform().AffineInverse() * vector;
	}

	private static Vector2[] AppendPoint(Vector2[] source, Vector2 point)
	{
		Vector2[] array = source ?? System.Array.Empty<Vector2>();
		Vector2[] array2 = new Vector2[array.Length + 1];
		System.Array.Copy(array, array2, array.Length);
		array2[^1] = point;
		return array2;
	}

	private static Vector2[] SetPoint(Vector2[] source, int index, Vector2 point)
	{
		Vector2[] array = source ?? System.Array.Empty<Vector2>();
		if (index < 0 || index >= array.Length)
		{
			return array;
		}
		Vector2[] array2 = new Vector2[array.Length];
		System.Array.Copy(array, array2, array.Length);
		array2[index] = point;
		return array2;
	}

	private static Vector2[] RemovePoint(Vector2[] source, int index)
	{
		Vector2[] array = source ?? System.Array.Empty<Vector2>();
		if (index < 0 || index >= array.Length)
		{
			return array;
		}
		Vector2[] array2 = new Vector2[array.Length - 1];
		if (index > 0)
		{
			System.Array.Copy(array, 0, array2, 0, index);
		}
		if (index < array.Length - 1)
		{
			System.Array.Copy(array, index + 1, array2, index, array.Length - index - 1);
		}
		return array2;
	}

	private Vector2 GetViewportLocalPointer(InputEventMouse mouseEvent)
	{
		if (mouseEvent == null)
		{
			return Vector2.Zero;
		}
		Transform2D globalTransformWithCanvas = GetGlobalTransformWithCanvas();
		if (!Mathf.IsZeroApprox(globalTransformWithCanvas.Determinant()))
		{
			return globalTransformWithCanvas.AffineInverse() * mouseEvent.GlobalPosition;
		}
		return mouseEvent.Position;
	}

	private Vector2 GetViewportLocalRelative(InputEventMouseMotion motion)
	{
		Transform2D globalTransformWithCanvas = GetGlobalTransformWithCanvas();
		if (Mathf.IsZeroApprox(globalTransformWithCanvas.Determinant()))
		{
			return motion.Relative;
		}
		Transform2D transform2D = globalTransformWithCanvas.AffineInverse();
		return transform2D * motion.GlobalPosition - transform2D * (motion.GlobalPosition - motion.Relative);
	}

	private bool IsTransformGizmoTool()
	{
		ToolMode currentToolMode = CurrentToolMode;
		if ((uint)currentToolMode <= 3u || currentToolMode == ToolMode.Pivot)
		{
			return true;
		}
		return false;
	}

	private bool TryBeginTransformGizmoDrag(Vector2 viewPosition)
	{
		if (!IsVisibleInTree() || !IsTransformGizmoTool() || !TestTryHitGizmo(viewPosition, out var type) || type == GizmoHandleType.None)
		{
			return false;
		}
		_activeGizmoHandle = type;
		_dragStartViewPosition = viewPosition;
		_dragCurrentViewPosition = viewPosition;
		_dragStartWorldPosition = ViewToWorld(viewPosition);
		_dragCurrentWorldPosition = _dragStartWorldPosition;
		GetGizmoAxesWorld(out _gizmoAxisXWorld, out _gizmoAxisYWorld);
		if (IsControlLayoutHandle(type))
		{
			_gizmoControl = GetSingleSelectedControl();
			if (GodotObject.IsInstanceValid(_gizmoControl) && !IsNodeLocked(_gizmoControl))
			{
				XW2DSceneEditor editor = Editor;
				if (editor != null && editor.BeginViewportControlLayoutSession(_gizmoControl))
				{
					_gizmoControlStartRect = new Rect2(_gizmoControl.Position, _gizmoControl.Size);
					_gizmoControlParentSize = GetControlParentSize(_gizmoControl);
					return true;
				}
			}
			ResetGizmoState();
			return false;
		}
		CaptureDragStartStates();
		if (_dragStartStates.Count == 0 && type != GizmoHandleType.Pivot)
		{
			ResetDragMode();
			return false;
		}
		_dragPivotWorldPosition = GetDragSelectionPivotWorldPosition();
		_dragStartSelectionBounds = GetDragSelectionBounds();
		BuildSmartSnapSession();
		switch (type)
		{
		case GizmoHandleType.Pivot:
			CapturePivotStartStates();
			_dragMode = DragMode.DragPivot;
			break;
		case GizmoHandleType.Rotate:
			_dragMode = DragMode.DragRotate;
			break;
		default:
			if (IsScaleHandle(type))
			{
				_dragMode = DragMode.DragScale;
				Vector2 vector = ViewToWorld(GetGizmoHandleViewPositionUnchecked(type));
				_gizmoScaleAnchorWorld = GetOppositeScalePointWorld(type);
				_gizmoScaleStartVector = ToGizmoBasis(vector - _gizmoScaleAnchorWorld);
			}
			else
			{
				_dragMode = DragMode.DragMove;
			}
			break;
		}
		return true;
	}

	private void UpdateActiveGizmoDrag(Vector2 viewPosition, bool shiftPressed)
	{
		if (_activeGizmoHandle != GizmoHandleType.None)
		{
			if (IsControlLayoutHandle(_activeGizmoHandle))
			{
				UpdateControlLayoutGizmo(viewPosition);
				QueueRedrawAll();
			}
			else
			{
				UpdateTransformDrag(viewPosition, shiftPressed);
				QueueRedrawAll();
			}
		}
	}

	private void FinishActiveGizmoDrag()
	{
		if (_activeGizmoHandle != GizmoHandleType.None)
		{
			if (IsControlLayoutHandle(_activeGizmoHandle))
			{
				Editor?.CommitViewportControlLayoutSession(IsAnchorHandle(_activeGizmoHandle) ? "拖动 Control 锚点" : "拖动 Control 尺寸");
				ResetGizmoState();
				QueueRedrawAll();
			}
			else
			{
				FinishTransformDrag();
			}
		}
	}

	private void CancelActiveGizmoDrag()
	{
		if (_activeGizmoHandle != GizmoHandleType.None)
		{
			if (IsControlLayoutHandle(_activeGizmoHandle))
			{
				Editor?.CancelViewportControlLayoutSession();
				ResetGizmoState();
				QueueRedrawAll();
			}
			else
			{
				CancelActiveInteraction(restoreTransform: true);
			}
		}
	}

	private void ResetGizmoState()
	{
		_activeGizmoHandle = GizmoHandleType.None;
		_hoveredGizmoHandle = GizmoHandleType.None;
		_gizmoControl = null;
		_gizmoControlStartRect = default(Rect2);
		_gizmoControlParentSize = Vector2.Zero;
		_gizmoScaleAnchorWorld = Vector2.Zero;
		_gizmoScaleStartVector = Vector2.Zero;
	}

	private void OnViewportVisibilityChanged()
	{
		UpdateCanvasLayerProcessing();
		if (!IsVisibleInTree())
		{
			if (_activeGizmoHandle != GizmoHandleType.None)
			{
				CancelActiveGizmoDrag();
			}
			else if (_dragMode != DragMode.None || _isPanning)
			{
				CancelActiveInteraction(restoreTransform: true);
			}
		}
	}

	private void DrawTransformGizmos(Control canvas)
	{
		if (!IsVisibleInTree() || !IsTransformGizmoTool() || !HasTransformableSelection())
		{
			return;
		}
		switch (CurrentToolMode)
		{
		case ToolMode.Select:
		case ToolMode.Move:
			DrawMoveGizmo(canvas);
			if (CurrentToolMode == ToolMode.Select && GetSingleSelectedControl() != null)
			{
				DrawControlLayoutGizmos(canvas);
			}
			break;
		case ToolMode.Rotate:
			DrawRotateGizmo(canvas);
			break;
		case ToolMode.Scale:
			DrawScaleGizmo(canvas);
			break;
		case ToolMode.Pivot:
			DrawPivotGizmo(canvas);
			break;
		case ToolMode.ListSelect:
			break;
		}
	}

	private void DrawMoveGizmo(Control canvas)
	{
		Vector2 vector = TestGetSelectionPivotViewPosition();
		GetGizmoAxesView(out var axisX, out var axisY);
		Vector2 vector2 = vector + axisX * 46f;
		Vector2 vector3 = vector + axisY * 46f;
		DrawGizmoLine(canvas, vector, vector2, _localXAxisColor, 3f);
		DrawGizmoLine(canvas, vector, vector3, _localYAxisColor, 3f);
		DrawArrowHead(canvas, vector2, axisX, _localXAxisColor);
		DrawArrowHead(canvas, vector3, axisY, _localYAxisColor);
		DrawSquareHandle(canvas, vector, GizmoHandleType.MoveFree, new Color(0.96f, 0.82f, 0.28f));
	}

	private void DrawRotateGizmo(Control canvas)
	{
		Vector2 vector = TestGetSelectionPivotViewPosition();
		float rotateRingRadius = GetRotateRingRadius();
		Color color = ((_activeGizmoHandle == GizmoHandleType.Rotate || _hoveredGizmoHandle == GizmoHandleType.Rotate) ? new Color(1f, 0.86f, 0.28f) : new Color(0.46f, 0.76f, 1f));
		canvas.DrawArc(vector, rotateRingRadius, 0f, (float)Math.PI * 2f, 64, new Color(0f, 0f, 0f, 0.72f), 5f, antialiased: true);
		canvas.DrawArc(vector, rotateRingRadius, 0f, (float)Math.PI * 2f, 64, color, 2f, antialiased: true);
		DrawCircleHandle(canvas, vector + Vector2.Up * rotateRingRadius, GizmoHandleType.Rotate, color, 4.5f);
	}

	private void DrawScaleGizmo(Control canvas)
	{
		GetScaleFrameView(out var center, out var axisX, out var axisY, out var halfX, out var halfY);
		Vector2 vector = center - axisX * halfX - axisY * halfY;
		Vector2 vector2 = center + axisX * halfX - axisY * halfY;
		Vector2 vector3 = center + axisX * halfX + axisY * halfY;
		Vector2 vector4 = center - axisX * halfX + axisY * halfY;
		canvas.DrawPolyline(new Vector2[5] { vector, vector2, vector3, vector4, vector }, _selectionColor, 2f, antialiased: true);
		GizmoHandleType[] scaleHandles = ScaleHandles;
		foreach (GizmoHandleType handle in scaleHandles)
		{
			DrawSquareHandle(canvas, GetScaleHandleViewPosition(handle), handle, _selectionColor);
		}
	}

	private void DrawPivotGizmo(Control canvas)
	{
		Vector2 vector = TestGetSelectionPivotViewPosition();
		Color color = new Color(1f, 0.62f, 0.2f);
		canvas.DrawCircle(vector, 10f, new Color(0f, 0f, 0f, 0.56f));
		canvas.DrawCircle(vector, 7f, color, filled: false, 2f, antialiased: true);
		canvas.DrawLine(vector - Vector2.Right * 12f, vector + Vector2.Right * 12f, color, 2f, antialiased: true);
		canvas.DrawLine(vector - Vector2.Down * 12f, vector + Vector2.Down * 12f, color, 2f, antialiased: true);
	}

	private void DrawControlLayoutGizmos(Control canvas)
	{
		Control singleSelectedControl = GetSingleSelectedControl();
		if (GodotObject.IsInstanceValid(singleSelectedControl))
		{
			Vector2[] array = new Vector2[4]
			{
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.AnchorTopLeft),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.AnchorTopRight),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.AnchorBottomRight),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.AnchorBottomLeft)
			};
			Vector2[] array2 = new Vector2[4]
			{
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.ControlSizeTopLeft),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.ControlSizeTopRight),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.ControlSizeBottomRight),
				GetControlHandleViewPosition(singleSelectedControl, GizmoHandleType.ControlSizeBottomLeft)
			};
			for (int i = 0; i < 4; i++)
			{
				canvas.DrawDashedLine(array[i], array2[i], new Color(0.42f, 0.88f, 1f, 0.75f), 1f, 5f);
			}
			GizmoHandleType[] anchorHandles = AnchorHandles;
			foreach (GizmoHandleType handle in anchorHandles)
			{
				DrawDiamondHandle(canvas, GetControlHandleViewPosition(singleSelectedControl, handle), handle);
			}
			anchorHandles = ControlSizeHandles;
			foreach (GizmoHandleType handle2 in anchorHandles)
			{
				DrawSquareHandle(canvas, GetControlHandleViewPosition(singleSelectedControl, handle2), handle2, new Color(0.9f, 0.94f, 1f));
			}
		}
	}

	private void DrawGizmoLine(Control canvas, Vector2 from, Vector2 to, Color color, float width)
	{
		canvas.DrawLine(from, to, new Color(0f, 0f, 0f, 0.66f), width + 3f, antialiased: true);
		canvas.DrawLine(from, to, color, width, antialiased: true);
	}

	private static void DrawArrowHead(Control canvas, Vector2 endpoint, Vector2 direction, Color color)
	{
		Vector2 vector = new Vector2(0f - direction.Y, direction.X);
		Vector2 vector2 = endpoint - direction * 10f;
		canvas.DrawColoredPolygon(new Vector2[3]
		{
			endpoint,
			vector2 + vector * 5f,
			vector2 - vector * 5f
		}, color);
	}

	private void DrawSquareHandle(Control canvas, Vector2 position, GizmoHandleType handle, Color color)
	{
		float num = (IsHandleHighlighted(handle) ? 7.5f : 6f);
		canvas.DrawRect(new Rect2(position - Vector2.One * (num + 2f), Vector2.One * (num + 2f) * 2f), new Color(0f, 0f, 0f, 0.72f));
		canvas.DrawRect(new Rect2(position - Vector2.One * num, Vector2.One * num * 2f), color);
	}

	private void DrawCircleHandle(Control canvas, Vector2 position, GizmoHandleType handle, Color color, float radius)
	{
		canvas.DrawCircle(position, radius + 2f, new Color(0f, 0f, 0f, 0.72f));
		canvas.DrawCircle(position, IsHandleHighlighted(handle) ? (radius + 1f) : radius, color);
	}

	private void DrawDiamondHandle(Control canvas, Vector2 position, GizmoHandleType handle)
	{
		float num = (IsHandleHighlighted(handle) ? 7.5f : 6.5f);
		Vector2[] points = new Vector2[4]
		{
			position + Vector2.Up * num,
			position + Vector2.Right * num,
			position + Vector2.Down * num,
			position + Vector2.Left * num
		};
		canvas.DrawColoredPolygon(points, new Color(0f, 0f, 0f, 0.76f));
		float num2 = num - 2f;
		canvas.DrawColoredPolygon(new Vector2[4]
		{
			position + Vector2.Up * num2,
			position + Vector2.Right * num2,
			position + Vector2.Down * num2,
			position + Vector2.Left * num2
		}, new Color(0.24f, 0.87f, 1f));
	}

	private bool IsHandleHighlighted(GizmoHandleType handle)
	{
		if (_activeGizmoHandle != handle)
		{
			return _hoveredGizmoHandle == handle;
		}
		return true;
	}

	private void UpdateGizmoHover(Vector2 viewPosition)
	{
		GizmoHandleType hoveredGizmoHandle = _hoveredGizmoHandle;
		_hoveredGizmoHandle = (TestTryHitGizmo(viewPosition, out var type) ? type : GizmoHandleType.None);
		if (hoveredGizmoHandle != _hoveredGizmoHandle)
		{
			QueueRedrawAll();
		}
	}

	public bool TestTryHitGizmo(Vector2 viewPosition, out GizmoHandleType type)
	{
		type = GizmoHandleType.None;
		if (!IsVisibleInTree() || !IsTransformGizmoTool() || !HasTransformableSelection())
		{
			return false;
		}
		GizmoHandleType[] array;
		switch (CurrentToolMode)
		{
		case ToolMode.Scale:
			array = ScaleHandles;
			break;
		case ToolMode.Pivot:
			array = new GizmoHandleType[1] { GizmoHandleType.Pivot };
			break;
		case ToolMode.Rotate:
			array = new GizmoHandleType[1] { GizmoHandleType.Rotate };
			break;
		case ToolMode.Select:
			if (GetSingleSelectedControl() != null)
			{
				array = SelectControlHandles;
				break;
			}
			goto default;
		default:
			array = new GizmoHandleType[3]
			{
				GizmoHandleType.MoveFree,
				GizmoHandleType.MoveX,
				GizmoHandleType.MoveY
			};
			break;
		}
		IEnumerable<GizmoHandleType> enumerable = array;
		if (CurrentToolMode == ToolMode.Rotate)
		{
			Vector2 to = TestGetSelectionPivotViewPosition();
			if (Mathf.Abs(viewPosition.DistanceTo(to) - GetRotateRingRadius()) <= 10f)
			{
				type = GizmoHandleType.Rotate;
				return true;
			}
			return false;
		}
		foreach (GizmoHandleType item in enumerable)
		{
			if (TryGetGizmoHandleViewPosition(item, out var position) && viewPosition.DistanceSquaredTo(position) <= 100f)
			{
				type = item;
				return true;
			}
		}
		return false;
	}

	public bool TryGetGizmoHandleViewPosition(GizmoHandleType type, out Vector2 position)
	{
		position = Vector2.Zero;
		if (type == GizmoHandleType.None || !IsGizmoHandleAvailableForCurrentTool(type) || !HasTransformableSelection())
		{
			return false;
		}
		if (IsControlLayoutHandle(type) && GetSingleSelectedControl() == null)
		{
			return false;
		}
		position = GetGizmoHandleViewPositionUnchecked(type);
		return position.IsFinite();
	}

	private bool IsGizmoHandleAvailableForCurrentTool(GizmoHandleType type)
	{
		if ((uint)(type - 1) <= 2u)
		{
			ToolMode currentToolMode = CurrentToolMode;
			if ((uint)currentToolMode <= 1u)
			{
				return true;
			}
			return false;
		}
		if (type == GizmoHandleType.Rotate)
		{
			return CurrentToolMode == ToolMode.Rotate;
		}
		if (IsScaleHandle(type))
		{
			return CurrentToolMode == ToolMode.Scale;
		}
		if (type == GizmoHandleType.Pivot)
		{
			return CurrentToolMode == ToolMode.Pivot;
		}
		if (IsControlLayoutHandle(type))
		{
			return CurrentToolMode == ToolMode.Select;
		}
		return false;
	}

	private Vector2 GetGizmoHandleViewPositionUnchecked(GizmoHandleType type)
	{
		Vector2 vector = TestGetSelectionPivotViewPosition();
		GetGizmoAxesView(out var axisX, out var axisY);
		switch (type)
		{
		case GizmoHandleType.MoveFree:
		case GizmoHandleType.Pivot:
			return vector;
		case GizmoHandleType.MoveX:
			return vector + axisX * 46f;
		case GizmoHandleType.MoveY:
			return vector + axisY * 46f;
		case GizmoHandleType.Rotate:
			return vector + Vector2.Up * GetRotateRingRadius();
		default:
			if (IsScaleHandle(type))
			{
				return GetScaleHandleViewPosition(type);
			}
			if (IsControlLayoutHandle(type))
			{
				return GetControlHandleViewPosition(GetSingleSelectedControl(), type);
			}
			return vector;
		}
	}

	public CanvasItem TestFindCanvasItemAt(Vector2 viewPosition)
	{
		return FindCanvasItemAt(_sceneInstance, viewPosition);
	}

	public Vector2 TestGetSelectionPivotViewPosition()
	{
		return WorldToView(GetSelectionPivotWorldPosition());
	}

	private bool HasTransformableSelection()
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem instance && GodotObject.IsInstanceValid(instance) && !IsNodeLocked(item))
			{
				return true;
			}
		}
		return false;
	}

	private Control GetSingleSelectedControl()
	{
		Control result = null;
		int num = 0;
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem && GodotObject.IsInstanceValid(item) && !IsNodeLocked(item))
			{
				num++;
				if (item is Control control)
				{
					result = control;
				}
			}
		}
		if (num != 1)
		{
			return null;
		}
		return result;
	}

	private void GetGizmoAxesView(out Vector2 axisX, out Vector2 axisY)
	{
		GetGizmoAxesWorld(out var axisX2, out var axisY2);
		Vector2 vector = WorldToView(Vector2.Zero);
		axisX = (WorldToView(axisX2) - vector).Normalized();
		axisY = (WorldToView(axisY2) - vector).Normalized();
		if (axisX.LengthSquared() < 0.5f)
		{
			axisX = Vector2.Right;
		}
		if (axisY.LengthSquared() < 0.5f)
		{
			axisY = Vector2.Down;
		}
	}

	private void GetGizmoAxesWorld(out Vector2 axisX, out Vector2 axisY)
	{
		axisX = Vector2.Right;
		axisY = Vector2.Down;
		if (!UseLocalSpace)
		{
			return;
		}
		CanvasItem canvasItem = null;
		int num = 0;
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem canvasItem2 && GodotObject.IsInstanceValid(canvasItem2) && !IsNodeLocked(item))
			{
				canvasItem = canvasItem2;
				num++;
			}
		}
		if (num == 1 && canvasItem != null)
		{
			Transform2D transform2D = GetCanvasLayerPreviewTransformForItem(canvasItem) * canvasItem.GetGlobalTransform();
			Vector2 vector = CanvasToSceneWorld(transform2D.Origin);
			Vector2 vector2 = CanvasToSceneWorld(transform2D.Origin + transform2D.X);
			Vector2 vector3 = CanvasToSceneWorld(transform2D.Origin + transform2D.Y);
			if (!vector2.IsEqualApprox(vector))
			{
				axisX = (vector2 - vector).Normalized();
			}
			if (!vector3.IsEqualApprox(vector))
			{
				axisY = (vector3 - vector).Normalized();
			}
		}
	}

	private float GetRotateRingRadius()
	{
		Rect2 selectionBounds = GetSelectionBounds();
		return Mathf.Max(38f, Mathf.Max(selectionBounds.Size.X, selectionBounds.Size.Y) * Zoom * 0.5f + 22f);
	}

	private void GetScaleFrameView(out Vector2 center, out Vector2 axisX, out Vector2 axisY, out float halfX, out float halfY)
	{
		Rect2 selectionBounds = GetSelectionBounds();
		center = WorldToView(selectionBounds.GetCenter());
		GetGizmoAxesView(out axisX, out axisY);
		halfX = Mathf.Max(selectionBounds.Size.X * Zoom * 0.5f, 10f);
		halfY = Mathf.Max(selectionBounds.Size.Y * Zoom * 0.5f, 10f);
	}

	private Vector2 GetScaleHandleViewPosition(GizmoHandleType handle)
	{
		GetScaleFrameView(out var center, out var axisX, out var axisY, out var halfX, out var halfY);
		GetScaleHandleSigns(handle, out var x, out var y);
		return center + axisX * (halfX * (float)x) + axisY * (halfY * (float)y);
	}

	private Vector2 GetOppositeScalePointWorld(GizmoHandleType handle)
	{
		GetScaleHandleSigns(handle, out var x, out var y);
		Rect2 selectionBounds = GetSelectionBounds();
		Vector2 center = selectionBounds.GetCenter();
		GetGizmoAxesWorld(out var axisX, out var axisY);
		float num = Mathf.Max(selectionBounds.Size.X * 0.5f, 0.01f);
		float num2 = Mathf.Max(selectionBounds.Size.Y * 0.5f, 0.01f);
		return center - axisX * (num * (float)x) - axisY * (num2 * (float)y);
	}

	private Vector2 ToGizmoBasis(Vector2 worldVector)
	{
		return new Vector2(worldVector.Dot(_gizmoAxisXWorld), worldVector.Dot(_gizmoAxisYWorld));
	}

	private Vector2 FromGizmoBasis(Vector2 value)
	{
		return _gizmoAxisXWorld * value.X + _gizmoAxisYWorld * value.Y;
	}

	private Vector2 ConstrainActiveMoveDelta(Vector2 delta)
	{
		Vector2 vector = ((_activeGizmoHandle == GizmoHandleType.MoveX) ? _gizmoAxisXWorld : _gizmoAxisYWorld);
		return vector * delta.Dot(vector);
	}

	private void UpdateHandleScale(Vector2 currentWorld, bool keepAspect)
	{
		Vector2 vector = ToGizmoBasis(currentWorld - _gizmoScaleAnchorWorld);
		GetScaleHandleSigns(_activeGizmoHandle, out var x, out var y);
		float num = ((x == 0) ? 1f : SafeScaleRatio(vector.X, _gizmoScaleStartVector.X));
		float num2 = ((y == 0) ? 1f : SafeScaleRatio(vector.Y, _gizmoScaleStartVector.Y));
		if (keepAspect && x != 0 && y != 0)
		{
			num2 = (num = ((Mathf.Abs(num - 1f) >= Mathf.Abs(num2 - 1f)) ? num : num2));
		}
		if (x != 0)
		{
			num = ApplyScaleSnap(num);
		}
		if (y != 0)
		{
			num2 = ApplyScaleSnap(num2);
		}
		ScaleSelectedBy(new Vector2(num, num2), _gizmoScaleAnchorWorld);
	}

	private static float SafeScaleRatio(float current, float start)
	{
		if (Mathf.Abs(start) < 0.001f)
		{
			return 1f;
		}
		float num = current / start;
		if (Mathf.Abs(num) < 0.01f)
		{
			num = 0.01f * ((num < 0f) ? (-1f) : 1f);
		}
		return Mathf.Clamp(num, -100f, 100f);
	}

	private void ScaleSelectedBy(Vector2 factor, Vector2 anchorWorld)
	{
		foreach (var (node2, dragTransformState2) in _dragStartStates)
		{
			if (GodotObject.IsInstanceValid(node2) && !IsNodeLocked(node2) && node2 is CanvasItem item)
			{
				SetNodePosition(node2, dragTransformState2.Position);
				SetNodeScale(node2, dragTransformState2.Scale * factor);
				Vector2 vector = ToGizmoBasis(dragTransformState2.WorldOrigin - anchorWorld);
				Vector2 vector2 = anchorWorld + FromGizmoBasis(vector * factor);
				Vector2 canvasItemWorldOrigin = GetCanvasItemWorldOrigin(item);
				if (TryResolveLocalPositionForWorldDelta(node2, dragTransformState2.Position, vector2 - canvasItemWorldOrigin, out var localTarget))
				{
					SetNodePosition(node2, localTarget);
				}
			}
		}
	}

	private void UpdateControlLayoutGizmo(Vector2 viewPosition)
	{
		if (!GodotObject.IsInstanceValid(_gizmoControl))
		{
			return;
		}
		Vector2 point = SceneWorldToControlParentLocal(_gizmoControl, ViewToWorld(viewPosition));
		if (IsAnchorHandle(_activeGizmoHandle))
		{
			Vector2 vector = new Vector2(Mathf.Max(_gizmoControlParentSize.X, 1f), Mathf.Max(_gizmoControlParentSize.Y, 1f));
			Vector2 anchor = new Vector2(Mathf.Clamp(point.X / vector.X, -10f, 10f), Mathf.Clamp(point.Y / vector.Y, -10f, 10f));
			if (GridSnapActive)
			{
				anchor = new Vector2(SnapValue(anchor.X, 0.01f), SnapValue(anchor.Y, 0.01f));
			}
			ApplyDraggedAnchor(_gizmoControl, _activeGizmoHandle, anchor);
		}
		else
		{
			Rect2 rect = ResizeControlRect(_gizmoControlStartRect, _activeGizmoHandle, point);
			_gizmoControl.Position = rect.Position;
			_gizmoControl.Size = rect.Size;
		}
		Editor?.RefreshDirectTransformSurface();
	}

	private static void ApplyDraggedAnchor(Control control, GizmoHandleType handle, Vector2 anchor)
	{
		switch (handle)
		{
		case GizmoHandleType.AnchorTopLeft:
			control.AnchorLeft = anchor.X;
			control.AnchorTop = anchor.Y;
			break;
		case GizmoHandleType.AnchorTopRight:
			control.AnchorRight = anchor.X;
			control.AnchorTop = anchor.Y;
			break;
		case GizmoHandleType.AnchorBottomRight:
			control.AnchorRight = anchor.X;
			control.AnchorBottom = anchor.Y;
			break;
		case GizmoHandleType.AnchorBottomLeft:
			control.AnchorLeft = anchor.X;
			control.AnchorBottom = anchor.Y;
			break;
		}
	}

	private static Rect2 ResizeControlRect(Rect2 start, GizmoHandleType handle, Vector2 point)
	{
		float num = start.Position.X;
		float num2 = start.Position.Y;
		float num3 = start.End.X;
		float num4 = start.End.Y;
		if (HandleAffectsLeft(handle))
		{
			num = Mathf.Min(point.X, num3 - 1f);
		}
		if (HandleAffectsRight(handle))
		{
			num3 = Mathf.Max(point.X, num + 1f);
		}
		if (HandleAffectsTop(handle))
		{
			num2 = Mathf.Min(point.Y, num4 - 1f);
		}
		if (HandleAffectsBottom(handle))
		{
			num4 = Mathf.Max(point.Y, num2 + 1f);
		}
		return new Rect2(new Vector2(num, num2), new Vector2(num3 - num, num4 - num2));
	}

	private Vector2 GetControlHandleViewPosition(Control control, GizmoHandleType handle)
	{
		if (!GodotObject.IsInstanceValid(control))
		{
			return Vector2.Zero;
		}
		if (IsAnchorHandle(handle))
		{
			Vector2 controlParentSize = GetControlParentSize(control);
			return WorldToView(ControlParentLocalToSceneWorld(control, handle switch
			{
				GizmoHandleType.AnchorTopLeft => new Vector2(control.AnchorLeft * controlParentSize.X, control.AnchorTop * controlParentSize.Y), 
				GizmoHandleType.AnchorTopRight => new Vector2(control.AnchorRight * controlParentSize.X, control.AnchorTop * controlParentSize.Y), 
				GizmoHandleType.AnchorBottomRight => new Vector2(control.AnchorRight * controlParentSize.X, control.AnchorBottom * controlParentSize.Y), 
				_ => new Vector2(control.AnchorLeft * controlParentSize.X, control.AnchorBottom * controlParentSize.Y), 
			}));
		}
		Vector2 rectHandlePoint = GetRectHandlePoint(new Rect2(control.Position, control.Size), handle);
		return WorldToView(ControlParentLocalToSceneWorld(control, rectHandlePoint));
	}

	private Vector2 ControlParentLocalToSceneWorld(Control control, Vector2 parentLocal)
	{
		if (!(control.GetParent() is CanvasItem canvasItem))
		{
			return parentLocal;
		}
		Transform2D transform2D = GetCanvasLayerPreviewTransformForItem(canvasItem) * canvasItem.GetGlobalTransform();
		return CanvasToSceneWorld(transform2D * parentLocal);
	}

	private Vector2 SceneWorldToControlParentLocal(Control control, Vector2 sceneWorld)
	{
		if (!(control.GetParent() is CanvasItem canvasItem))
		{
			return sceneWorld;
		}
		Vector2 vector = _sceneContainer.GetGlobalTransform() * sceneWorld;
		Transform2D canvasLayerPreviewTransformForItem = GetCanvasLayerPreviewTransformForItem(canvasItem);
		if (!Mathf.IsZeroApprox(canvasLayerPreviewTransformForItem.Determinant()))
		{
			vector = canvasLayerPreviewTransformForItem.AffineInverse() * vector;
		}
		Transform2D globalTransform = canvasItem.GetGlobalTransform();
		if (!Mathf.IsZeroApprox(globalTransform.Determinant()))
		{
			return globalTransform.AffineInverse() * vector;
		}
		return control.Position;
	}

	private static Vector2 GetControlParentSize(Control control)
	{
		if (control.GetParent() is Control control2)
		{
			return control2.Size;
		}
		int a = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
		return new Vector2(y: Mathf.Max((int)ProjectSettings.GetSetting("display/window/size/viewport_height"), 1), x: Mathf.Max(a, 1));
	}

	private static Vector2 GetRectHandlePoint(Rect2 rect, GizmoHandleType handle)
	{
		float x = rect.Position.X;
		float y = rect.Position.Y;
		float x2 = rect.End.X;
		float y2 = rect.End.Y;
		float x3 = (x + x2) * 0.5f;
		float y3 = (y + y2) * 0.5f;
		return handle switch
		{
			GizmoHandleType.ControlSizeTopLeft => new Vector2(x, y), 
			GizmoHandleType.ControlSizeTop => new Vector2(x3, y), 
			GizmoHandleType.ControlSizeTopRight => new Vector2(x2, y), 
			GizmoHandleType.ControlSizeRight => new Vector2(x2, y3), 
			GizmoHandleType.ControlSizeBottomRight => new Vector2(x2, y2), 
			GizmoHandleType.ControlSizeBottom => new Vector2(x3, y2), 
			GizmoHandleType.ControlSizeBottomLeft => new Vector2(x, y2), 
			_ => new Vector2(x, y3), 
		};
	}

	private static void GetScaleHandleSigns(GizmoHandleType handle, out int x, out int y)
	{
		int num;
		switch (handle)
		{
		case GizmoHandleType.ScaleTopLeft:
		case GizmoHandleType.ScaleBottomLeft:
		case GizmoHandleType.ScaleLeft:
			num = -1;
			break;
		case GizmoHandleType.ScaleTopRight:
		case GizmoHandleType.ScaleRight:
		case GizmoHandleType.ScaleBottomRight:
			num = 1;
			break;
		default:
			num = 0;
			break;
		}
		x = num;
		switch (handle)
		{
		case GizmoHandleType.ScaleTopLeft:
		case GizmoHandleType.ScaleTop:
		case GizmoHandleType.ScaleTopRight:
			num = -1;
			break;
		case GizmoHandleType.ScaleBottomRight:
		case GizmoHandleType.ScaleBottom:
		case GizmoHandleType.ScaleBottomLeft:
			num = 1;
			break;
		default:
			num = 0;
			break;
		}
		y = num;
	}

	private static bool IsScaleHandle(GizmoHandleType handle)
	{
		if (handle >= GizmoHandleType.ScaleTopLeft)
		{
			return handle <= GizmoHandleType.ScaleLeft;
		}
		return false;
	}

	private static bool IsAnchorHandle(GizmoHandleType handle)
	{
		if (handle >= GizmoHandleType.AnchorTopLeft)
		{
			return handle <= GizmoHandleType.AnchorBottomLeft;
		}
		return false;
	}

	private static bool IsControlSizeHandle(GizmoHandleType handle)
	{
		if (handle >= GizmoHandleType.ControlSizeTopLeft)
		{
			return handle <= GizmoHandleType.ControlSizeLeft;
		}
		return false;
	}

	private static bool IsControlLayoutHandle(GizmoHandleType handle)
	{
		if (!IsAnchorHandle(handle))
		{
			return IsControlSizeHandle(handle);
		}
		return true;
	}

	private static bool HandleAffectsLeft(GizmoHandleType handle)
	{
		if (handle == GizmoHandleType.ControlSizeTopLeft || (uint)(handle - 24) <= 1u)
		{
			return true;
		}
		return false;
	}

	private static bool HandleAffectsRight(GizmoHandleType handle)
	{
		if ((uint)(handle - 20) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static bool HandleAffectsTop(GizmoHandleType handle)
	{
		if ((uint)(handle - 18) <= 2u)
		{
			return true;
		}
		return false;
	}

	private static bool HandleAffectsBottom(GizmoHandleType handle)
	{
		if ((uint)(handle - 22) <= 2u)
		{
			return true;
		}
		return false;
	}

	private bool TryBeginGuideDrag(Vector2 viewPosition)
	{
		if (!ShowGuides && !ShowRulers)
		{
			return false;
		}
		if (ShowGuides)
		{
			for (int num = _verticalGuides.Count - 1; num >= 0; num--)
			{
				if (Mathf.Abs(WorldToView(new Vector2(_verticalGuides[num], 0f)).X - viewPosition.X) <= 6f)
				{
					_guideDragAxis = GuideAxis.Vertical;
					_guideDragIndex = num;
					_guideDragWorldPosition = _verticalGuides[num];
					return true;
				}
			}
			for (int num2 = _horizontalGuides.Count - 1; num2 >= 0; num2--)
			{
				if (Mathf.Abs(WorldToView(new Vector2(0f, _horizontalGuides[num2])).Y - viewPosition.Y) <= 6f)
				{
					_guideDragAxis = GuideAxis.Horizontal;
					_guideDragIndex = num2;
					_guideDragWorldPosition = _horizontalGuides[num2];
					return true;
				}
			}
		}
		if (!ShowRulers)
		{
			return false;
		}
		if (viewPosition.Y <= 16f && viewPosition.X > 16f)
		{
			_guideDragAxis = GuideAxis.Vertical;
			_guideDragIndex = -1;
			_guideDragWorldPosition = ViewToWorld(viewPosition).X;
			return true;
		}
		if (viewPosition.X <= 16f && viewPosition.Y > 16f)
		{
			_guideDragAxis = GuideAxis.Horizontal;
			_guideDragIndex = -1;
			_guideDragWorldPosition = ViewToWorld(viewPosition).Y;
			return true;
		}
		return false;
	}

	private void UpdateGuideDrag(Vector2 viewPosition)
	{
		Vector2 vector = ViewToWorld(viewPosition);
		_guideDragWorldPosition = ((_guideDragAxis == GuideAxis.Vertical) ? vector.X : vector.Y);
		QueueRedrawAll();
	}

	private void FinishGuideDrag(Vector2 viewPosition)
	{
		if (_guideDragAxis == GuideAxis.None)
		{
			return;
		}
		bool flag = viewPosition.X < 16f || viewPosition.Y < 16f || viewPosition.X > base.Size.X || viewPosition.Y > base.Size.Y;
		List<float> list = ((_guideDragAxis == GuideAxis.Vertical) ? _verticalGuides : _horizontalGuides);
		if (_guideDragIndex >= 0 && _guideDragIndex < list.Count)
		{
			if (flag)
			{
				list.RemoveAt(_guideDragIndex);
			}
			else
			{
				list[_guideDragIndex] = _guideDragWorldPosition;
			}
		}
		else if (!flag)
		{
			list.Add(_guideDragWorldPosition);
		}
		list.Sort();
		ResetGuideDrag();
		QueueRedrawAll();
	}

	private void ResetGuideDrag()
	{
		_guideDragAxis = GuideAxis.None;
		_guideDragIndex = -1;
		_guideDragWorldPosition = 0f;
		QueueRedrawAll();
	}

	private void BeginRulerMeasurement(Vector2 viewPosition)
	{
		_rulerStartWorld = ApplyGridSnapToWorld(ViewToWorld(viewPosition));
		_rulerEndWorld = _rulerStartWorld;
		_rulerMeasurementVisible = true;
		_rulerDragging = true;
		QueueRedrawAll();
	}

	private void UpdateRulerMeasurement(Vector2 viewPosition)
	{
		_rulerEndWorld = ApplyGridSnapToWorld(ViewToWorld(viewPosition));
		QueueRedrawAll();
	}

	private void FinishRulerMeasurement(Vector2 viewPosition)
	{
		UpdateRulerMeasurement(viewPosition);
		_rulerDragging = false;
	}

	public void ClearRulerMeasurement()
	{
		_rulerDragging = false;
		_rulerMeasurementVisible = false;
		_rulerStartWorld = Vector2.Zero;
		_rulerEndWorld = Vector2.Zero;
		QueueRedrawAll();
	}

	private void OpenListSelectMenu(Vector2 viewPosition)
	{
		if (!GodotObject.IsInstanceValid(_listSelectMenu))
		{
			return;
		}
		_listSelectCandidates.Clear();
		_listSelectMenu.Clear();
		FindCanvasItemsAt(_sceneInstance, viewPosition, _listSelectCandidates);
		if (_listSelectCandidates.Count == 0)
		{
			_listSelectMenu.Hide();
			XWEditorInterface.Instance?.ShowToast("此处没有可编辑的 2D 节点");
			return;
		}
		for (int i = 0; i < _listSelectCandidates.Count; i++)
		{
			CanvasItem canvasItem = _listSelectCandidates[i];
			string label = $"{canvasItem.Name}  ·  {canvasItem.GetClass()}";
			Texture2D texture2D = XWClassRegistry.Instance?.GetClassIcon(canvasItem.GetClass());
			if (GodotObject.IsInstanceValid(texture2D))
			{
				_listSelectMenu.AddIconItem(texture2D, label, i, Key.None);
			}
			else
			{
				_listSelectMenu.AddItem(label, i, Key.None);
			}
			_listSelectMenu.SetItemTooltip(i, canvasItem.GetPath().ToString());
		}
		Vector2I position = (Vector2I)(GetScreenPosition() + viewPosition);
		_listSelectMenu.Popup(new Rect2I(position, new Vector2I(300, 0)));
	}

	private void FindCanvasItemsAt(Node node, Vector2 viewPosition, List<CanvasItem> result)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		Array<Node> children = node.GetChildren();
		for (int num = children.Count - 1; num >= 0; num--)
		{
			Node node2 = children[num];
			if (node2 != null)
			{
				FindCanvasItemsAt(node2, viewPosition, result);
			}
		}
		if (node is CanvasItem { Visible: not false } canvasItem && !IsNodeLocked(node) && GetViewportRectForItem(canvasItem).HasPoint(viewPosition))
		{
			result.Add(canvasItem);
		}
	}

	private void OnListSelectMenuIdPressed(long id)
	{
		int num = (int)id;
		if (num >= 0 && num < _listSelectCandidates.Count)
		{
			CanvasItem canvasItem = _listSelectCandidates[num];
			if (GodotObject.IsInstanceValid(canvasItem) && !IsNodeLocked(canvasItem))
			{
				SelectNode(canvasItem);
			}
		}
		_listSelectMenu?.Hide();
		_listSelectCandidates.Clear();
	}

	private void DrawLocksAndGroups(Control canvas, Node node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is CanvasItem { Visible: not false } canvasItem)
		{
			Vector2 vector = GetViewportRectForItem(canvasItem).Position + new Vector2(3f, 3f);
			if (node.HasMeta("_edit_lock_"))
			{
				canvas.DrawCircle(vector + new Vector2(4f, 4f), 4f, _lockedColor);
			}
			if (node.HasMeta("_edit_group_"))
			{
				canvas.DrawCircle(vector + new Vector2(14f, 4f), 4f, _groupColor);
			}
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				DrawLocksAndGroups(canvas, node2);
			}
		}
	}

	private void DrawGuides(Control canvas)
	{
		if (!ShowGuides)
		{
			return;
		}
		foreach (float verticalGuide in _verticalGuides)
		{
			float x = WorldToView(new Vector2(verticalGuide, 0f)).X;
			canvas.DrawLine(new Vector2(x, 0f), new Vector2(x, base.Size.Y), _guideColor, 1f);
		}
		foreach (float horizontalGuide in _horizontalGuides)
		{
			float y = WorldToView(new Vector2(0f, horizontalGuide)).Y;
			canvas.DrawLine(new Vector2(0f, y), new Vector2(base.Size.X, y), _guideColor, 1f);
		}
		if (_guideDragAxis == GuideAxis.Vertical)
		{
			float x2 = WorldToView(new Vector2(_guideDragWorldPosition, 0f)).X;
			canvas.DrawLine(new Vector2(x2, 0f), new Vector2(x2, base.Size.Y), _guideColor, 2f);
		}
		else if (_guideDragAxis == GuideAxis.Horizontal)
		{
			float y2 = WorldToView(new Vector2(0f, _guideDragWorldPosition)).Y;
			canvas.DrawLine(new Vector2(0f, y2), new Vector2(base.Size.X, y2), _guideColor, 2f);
		}
	}

	private void DrawSmartSnapLines(Control canvas)
	{
		if (_activeSnapX.HasValue)
		{
			float x = WorldToView(new Vector2(_activeSnapX.Value, 0f)).X;
			canvas.DrawLine(new Vector2(x, 0f), new Vector2(x, base.Size.Y), _snapLineColor, 1.5f);
		}
		if (_activeSnapY.HasValue)
		{
			float y = WorldToView(new Vector2(0f, _activeSnapY.Value)).Y;
			canvas.DrawLine(new Vector2(0f, y), new Vector2(base.Size.X, y), _snapLineColor, 1.5f);
		}
	}

	private void DrawTransformHelpers(Control canvas)
	{
		if (!ShowHelpers && !UseLocalSpace)
		{
			return;
		}
		List<Node> list = XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>();
		if (list.Count == 0)
		{
			return;
		}
		foreach (Node item in list)
		{
			if (item is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Vector2 canvasItemWorldOrigin = GetCanvasItemWorldOrigin(canvasItem);
				Vector2 vector = WorldToView(canvasItemWorldOrigin);
				Vector2 vector2 = Vector2.Right;
				Vector2 vector3 = Vector2.Down;
				if (UseLocalSpace && list.Count == 1)
				{
					Transform2D transform2D = GetCanvasLayerPreviewTransformForItem(canvasItem) * canvasItem.GetGlobalTransform();
					vector2 = transform2D.X.Normalized();
					vector3 = transform2D.Y.Normalized();
				}
				canvas.DrawLine(vector, vector + vector2 * 36f, _localXAxisColor, 2f);
				canvas.DrawLine(vector, vector + vector3 * 36f, _localYAxisColor, 2f);
				canvas.DrawCircle(vector, 3.5f, _selectionColor);
			}
		}
	}

	private void DrawRulerMeasurement(Control canvas)
	{
		if (_rulerMeasurementVisible)
		{
			Vector2 vector = WorldToView(_rulerStartWorld);
			Vector2 vector2 = WorldToView(_rulerEndWorld);
			Vector2 vector3 = _rulerEndWorld - _rulerStartWorld;
			canvas.DrawLine(vector, vector2, _rulerMeasureColor, 2f);
			canvas.DrawCircle(vector, 4f, _rulerMeasureColor);
			canvas.DrawCircle(vector2, 4f, _rulerMeasureColor);
			string text = $"长度 {vector3.Length():0.##}  ΔX {vector3.X:0.##}  ΔY {vector3.Y:0.##}  角度 {Mathf.RadToDeg(vector3.Angle()):0.#}°";
			Vector2 pos = (vector + vector2) * 0.5f + new Vector2(8f, -8f);
			canvas.DrawString(GetThemeDefaultFont(), pos, text, HorizontalAlignment.Left, -1f, 13, _rulerMeasureColor, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
		}
	}

	private void DrawRulers(Control canvas)
	{
		if (ShowRulers)
		{
			Color color = new Color(0f, 0f, 0f, 0.35f);
			Color color2 = new Color(1f, 1f, 1f, 0.12f);
			canvas.DrawRect(new Rect2(Vector2.Zero, new Vector2(base.Size.X, 16f)), color);
			canvas.DrawRect(new Rect2(Vector2.Zero, new Vector2(16f, base.Size.Y)), color);
			canvas.DrawLine(new Vector2(0f, 16f), new Vector2(base.Size.X, 16f), color2);
			canvas.DrawLine(new Vector2(16f, 0f), new Vector2(16f, base.Size.Y), color2);
			float rulerWorldStep = GetRulerWorldStep();
			float x = ViewToWorld(new Vector2(16f, 0f)).X;
			float x2 = ViewToWorld(new Vector2(base.Size.X, 0f)).X;
			float y = ViewToWorld(new Vector2(0f, 16f)).Y;
			float y2 = ViewToWorld(new Vector2(0f, base.Size.Y)).Y;
			Color color3 = new Color(1f, 1f, 1f, 0.45f);
			Color value = new Color(1f, 1f, 1f, 0.68f);
			for (float num = Mathf.Floor(x / rulerWorldStep) * rulerWorldStep; num <= x2; num += rulerWorldStep)
			{
				float x3 = WorldToView(new Vector2(num, 0f)).X;
				canvas.DrawLine(new Vector2(x3, 10f), new Vector2(x3, 16f), color3);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(x3 + 2f, 10f), FormatRulerValue(num), HorizontalAlignment.Left, 56f, 10, value, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
			for (float num2 = Mathf.Floor(y / rulerWorldStep) * rulerWorldStep; num2 <= y2; num2 += rulerWorldStep)
			{
				float y3 = WorldToView(new Vector2(0f, num2)).Y;
				canvas.DrawLine(new Vector2(10f, y3), new Vector2(16f, y3), color3);
				canvas.DrawString(GetThemeDefaultFont(), new Vector2(1f, y3 - 2f), FormatRulerValue(num2), HorizontalAlignment.Left, 14f, 9, value, TextServer.JustificationFlag.Kashida | TextServer.JustificationFlag.WordBound, TextServer.Direction.Auto, TextServer.Orientation.Horizontal, 0f);
			}
		}
	}

	private float GetRulerWorldStep()
	{
		float num = 80f / Mathf.Max(Zoom, 0.001f);
		float num2 = Mathf.Pow(10f, Mathf.Floor(Mathf.Log(num) / Mathf.Log(10f)));
		float num3 = num / num2;
		return Mathf.Max((float)((num3 <= 1f) ? 1 : ((num3 <= 2f) ? 2 : ((num3 <= 5f) ? 5 : 10))) * num2, 0.0001f);
	}

	private static string FormatRulerValue(float value)
	{
		if (!(Mathf.Abs(value) >= 1000f))
		{
			return value.ToString("0.##");
		}
		return value.ToString("0");
	}

	private void DrawViewportRect(Control canvas)
	{
		if (ShowViewportRect)
		{
			int num = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
			int num2 = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
			if (num > 0 && num2 > 0)
			{
				Rect2 rect = new Rect2(WorldToView(Vector2.Zero), new Vector2(num, num2) * Zoom);
				canvas.DrawRect(rect, new Color(0.337f, 0.62f, 1f, 0.55f), filled: false, 1f);
			}
		}
	}

	private void DrawNavigationPreview(Control canvas)
	{
		Rect2 navigationPreviewRect = GetNavigationPreviewRect();
		if (!(navigationPreviewRect.Size == Vector2.Zero))
		{
			Rect2 navigationPreviewWorldRect = GetNavigationPreviewWorldRect();
			if (!(navigationPreviewWorldRect.Size.X <= 0f) && !(navigationPreviewWorldRect.Size.Y <= 0f))
			{
				Color color = new Color(0.08f, 0.09f, 0.11f, 0.86f);
				Color color2 = new Color(1f, 1f, 1f, 0.18f);
				Color fill = new Color(0.72f, 0.77f, 0.84f, 0.18f);
				Color stroke = new Color(0.72f, 0.77f, 0.84f, 0.36f);
				Color color3 = new Color(0.337f, 0.62f, 1f, 0.18f);
				Color color4 = new Color(0.337f, 0.62f, 1f, 0.92f);
				canvas.DrawRect(navigationPreviewRect, color);
				DrawNavigationPreviewItems(canvas, _sceneInstance, navigationPreviewRect, navigationPreviewWorldRect, fill, stroke);
				canvas.DrawRect(navigationPreviewRect, color2, filled: false, 1f);
				Rect2 rect = ClampRectToRect(GetNavigationPreviewViewportRect(navigationPreviewRect, navigationPreviewWorldRect), navigationPreviewRect);
				canvas.DrawRect(rect, color3);
				canvas.DrawRect(rect, color4, filled: false, 1f);
				DrawNavigationPreviewHandles(canvas, rect, color4);
			}
		}
	}

	private void DrawNavigationPreviewItems(Control canvas, Node node, Rect2 previewRect, Rect2 worldRect, Color fill, Color stroke)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is CanvasItem { Visible: not false } canvasItem)
		{
			Rect2 rect = WorldRectToNavigationPreview(GetWorldRectForItem(canvasItem), previewRect, worldRect);
			rect = ClampRectToRect(rect, previewRect);
			if (rect.Size.X > 1f && rect.Size.Y > 1f)
			{
				canvas.DrawRect(rect, fill);
				canvas.DrawRect(rect, stroke, filled: false, 1f);
			}
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				DrawNavigationPreviewItems(canvas, node2, previewRect, worldRect, fill, stroke);
			}
		}
	}

	private static void DrawNavigationPreviewHandles(Control canvas, Rect2 viewportRect, Color color)
	{
		float num = 4f;
		Vector2[] array = new Vector2[4]
		{
			viewportRect.Position,
			viewportRect.Position + new Vector2(viewportRect.Size.X, 0f),
			viewportRect.Position + viewportRect.Size,
			viewportRect.Position + new Vector2(0f, viewportRect.Size.Y)
		};
		foreach (Vector2 vector in array)
		{
			canvas.DrawRect(new Rect2(vector - Vector2.One * num, Vector2.One * 8f), color);
		}
	}

	private bool TryBeginNavigationPreviewDrag(Vector2 viewPosition)
	{
		Rect2 navigationPreviewRect = GetNavigationPreviewRect();
		if (navigationPreviewRect.Size == Vector2.Zero || !navigationPreviewRect.HasPoint(viewPosition))
		{
			return false;
		}
		_previewDragPreviewRect = navigationPreviewRect;
		_previewDragWorldRect = GetNavigationPreviewWorldRect();
		if (IsNearNavigationPreviewEdge(GetNavigationPreviewViewportRect(_previewDragPreviewRect, _previewDragWorldRect), viewPosition))
		{
			_previewDragMode = PreviewDragMode.Resize;
			_previewResizeAnchorWorld = GetVisibleWorldRect().GetCenter();
			ResizeNavigationPreview(viewPosition);
			QueueRedrawAll();
			return true;
		}
		_previewDragMode = PreviewDragMode.Pan;
		UpdateNavigationPreviewDrag(viewPosition);
		return true;
	}

	private void UpdateNavigationPreviewDrag(Vector2 viewPosition)
	{
		if (_previewDragMode == PreviewDragMode.Resize)
		{
			ResizeNavigationPreview(viewPosition);
		}
		else if (_previewDragMode == PreviewDragMode.Pan)
		{
			Vector2 worldPosition = NavigationPreviewToWorld(viewPosition, _previewDragPreviewRect, _previewDragWorldRect);
			CenterAt(worldPosition);
		}
	}

	private void ResizeNavigationPreview(Vector2 viewPosition)
	{
		Vector2 vector = AbsVector2(NavigationPreviewToWorld(viewPosition, _previewDragPreviewRect, _previewDragWorldRect) - _previewResizeAnchorWorld);
		float num = Mathf.Max(vector.X * 2f, 8f);
		float num2 = Mathf.Max(vector.Y * 2f, 8f);
		float num3 = Mathf.Clamp(Mathf.Min(base.Size.X / num, base.Size.Y / num2), 0.05f, 32f);
		if (!Mathf.IsEqualApprox(Zoom, num3))
		{
			EmitSignal(SignalName.ZoomChanged);
		}
		Zoom = num3;
		ViewOffset = _previewResizeAnchorWorld;
		UpdateSceneTransform();
		QueueRedrawAll();
	}

	private void ResetNavigationPreviewDrag()
	{
		_previewDragMode = PreviewDragMode.None;
		QueueRedrawAll();
	}

	private Rect2 GetNavigationPreviewRect()
	{
		if (base.Size.X < 144f || base.Size.Y < 110f)
		{
			return new Rect2(Vector2.Zero, Vector2.Zero);
		}
		float num = Mathf.Min(220f, Mathf.Max(120f, base.Size.X * 0.22f));
		float num2 = Mathf.Max(74f, num * 0.62f);
		if (num2 > base.Size.Y - 24f)
		{
			num2 = Mathf.Max(64f, base.Size.Y - 24f);
			num = Mathf.Max(120f, num2 / 0.62f);
		}
		return new Rect2(new Vector2(base.Size.X - num - 12f, base.Size.Y - num2 - 12f), new Vector2(num, num2));
	}

	private Rect2 GetNavigationPreviewViewportRect(Rect2 previewRect, Rect2 worldRect)
	{
		return WorldRectToNavigationPreview(GetVisibleWorldRect(), previewRect, worldRect);
	}

	private Rect2 GetNavigationPreviewWorldRect()
	{
		Rect2 visibleWorldRect = GetVisibleWorldRect();
		Rect2 rect = visibleWorldRect;
		if (TryGetSceneWorldBounds(_sceneInstance, out var bounds))
		{
			rect = bounds.Merge(visibleWorldRect);
		}
		float amount = Mathf.Max(Mathf.Max(rect.Size.X, rect.Size.Y) * 0.08f, 64f);
		return GrowRect(rect, amount);
	}

	private Rect2 GetVisibleWorldRect()
	{
		Vector2 vector = ViewToWorld(Vector2.Zero);
		Vector2 vector2 = ViewToWorld(base.Size);
		return NormalizeRect(new Rect2(vector, vector2 - vector));
	}

	private bool TryGetSceneWorldBounds(Node node, out Rect2 bounds)
	{
		bounds = new Rect2(Vector2.Zero, Vector2.Zero);
		bool hasBounds = false;
		CollectSceneWorldBounds(node, ref bounds, ref hasBounds);
		return hasBounds;
	}

	private void CollectSceneWorldBounds(Node node, ref Rect2 bounds, ref bool hasBounds)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is CanvasItem { Visible: not false } canvasItem)
		{
			Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
			if (worldRectForItem.Size != Vector2.Zero)
			{
				bounds = (hasBounds ? bounds.Merge(worldRectForItem) : worldRectForItem);
				hasBounds = true;
			}
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				CollectSceneWorldBounds(node2, ref bounds, ref hasBounds);
			}
		}
	}

	private static bool IsNearNavigationPreviewEdge(Rect2 viewportRect, Vector2 point)
	{
		if (!GrowRect(viewportRect, 8f).HasPoint(point))
		{
			return false;
		}
		if (!(Mathf.Abs(point.X - viewportRect.Position.X) <= 8f) && !(Mathf.Abs(point.X - (viewportRect.Position.X + viewportRect.Size.X)) <= 8f) && !(Mathf.Abs(point.Y - viewportRect.Position.Y) <= 8f))
		{
			return Mathf.Abs(point.Y - (viewportRect.Position.Y + viewportRect.Size.Y)) <= 8f;
		}
		return true;
	}

	private static Vector2 NavigationPreviewToWorld(Vector2 point, Rect2 previewRect, Rect2 worldRect)
	{
		float num = Mathf.Clamp((point.X - previewRect.Position.X) / Mathf.Max(previewRect.Size.X, 1f), 0f, 1f);
		float num2 = Mathf.Clamp((point.Y - previewRect.Position.Y) / Mathf.Max(previewRect.Size.Y, 1f), 0f, 1f);
		return worldRect.Position + new Vector2(worldRect.Size.X * num, worldRect.Size.Y * num2);
	}

	private static Rect2 WorldRectToNavigationPreview(Rect2 worldSourceRect, Rect2 previewRect, Rect2 worldRect)
	{
		Vector2 p = WorldToNavigationPreview(worldSourceRect.Position, previewRect, worldRect);
		Vector2 p2 = WorldToNavigationPreview(worldSourceRect.Position + new Vector2(worldSourceRect.Size.X, 0f), previewRect, worldRect);
		Vector2 p3 = WorldToNavigationPreview(worldSourceRect.Position + worldSourceRect.Size, previewRect, worldRect);
		Vector2 p4 = WorldToNavigationPreview(worldSourceRect.Position + new Vector2(0f, worldSourceRect.Size.Y), previewRect, worldRect);
		return BuildRectFromPoints(p, p2, p3, p4);
	}

	private static Vector2 WorldToNavigationPreview(Vector2 world, Rect2 previewRect, Rect2 worldRect)
	{
		float num = (world.X - worldRect.Position.X) / Mathf.Max(worldRect.Size.X, 1f);
		float num2 = (world.Y - worldRect.Position.Y) / Mathf.Max(worldRect.Size.Y, 1f);
		return previewRect.Position + new Vector2(previewRect.Size.X * num, previewRect.Size.Y * num2);
	}

	private static Rect2 ClampRectToRect(Rect2 rect, Rect2 bounds)
	{
		Vector2 vector = new Vector2(Mathf.Clamp(rect.Position.X, bounds.Position.X, bounds.Position.X + bounds.Size.X), Mathf.Clamp(rect.Position.Y, bounds.Position.Y, bounds.Position.Y + bounds.Size.Y));
		Vector2 vector2 = new Vector2(Mathf.Clamp(rect.Position.X + rect.Size.X, bounds.Position.X, bounds.Position.X + bounds.Size.X), Mathf.Clamp(rect.Position.Y + rect.Size.Y, bounds.Position.Y, bounds.Position.Y + bounds.Size.Y));
		return NormalizeRect(new Rect2(vector, vector2 - vector));
	}

	private void OnGuiInput(InputEvent @event)
	{
		ReleaseDroppedResourceBatchesIfHistoryWasCleared();
		if (!IsVisibleInTree())
		{
			return;
		}
		if (@event is InputEventKey inputEventKey)
		{
			if (!inputEventKey.Pressed || inputEventKey.Echo)
			{
				return;
			}
			if (inputEventKey.Keycode == Key.Escape)
			{
				if (_activeGizmoHandle != GizmoHandleType.None)
				{
					CancelActiveGizmoDrag();
				}
				else if (GodotObject.IsInstanceValid(_listSelectMenu) && _listSelectMenu.Visible)
				{
					_listSelectMenu.Hide();
					_listSelectCandidates.Clear();
				}
				else if (_guideDragAxis != GuideAxis.None)
				{
					ResetGuideDrag();
				}
				else if (_rulerDragging || _rulerMeasurementVisible)
				{
					ClearRulerMeasurement();
				}
				else if (_previewDragMode != PreviewDragMode.None)
				{
					ResetNavigationPreviewDrag();
				}
				else if (_pointEditMode != PointEditMode.None)
				{
					CancelPointEdit();
				}
				else if (_dragMode != DragMode.None || _isPanning)
				{
					CancelActiveInteraction(restoreTransform: true);
				}
				else
				{
					XWEditorInterface.Instance?.ClearSelection();
				}
				QueueRedrawAll();
				AcceptEvent();
				return;
			}
			if (inputEventKey.Keycode == Key.Delete)
			{
				if (IsPointToolMode() && RemoveSelectedEditablePoint())
				{
					QueueRedrawAll();
					AcceptEvent();
				}
				else
				{
					XWEditorInterface.Instance?.GetSceneTreeDock()?.RemoveSelectedNode();
					QueueRedrawAll();
					AcceptEvent();
				}
				return;
			}
		}
		if (@event is InputEventMouseMotion inputEventMouseMotion)
		{
			Vector2 viewportLocalPointer = GetViewportLocalPointer(inputEventMouseMotion);
			EmitSignal(SignalName.MousePositionChanged, ViewToWorld(viewportLocalPointer));
			if (_activeGizmoHandle != GizmoHandleType.None)
			{
				UpdateActiveGizmoDrag(viewportLocalPointer, inputEventMouseMotion.ShiftPressed);
				AcceptEvent();
				return;
			}
			if (_guideDragAxis != GuideAxis.None)
			{
				UpdateGuideDrag(viewportLocalPointer);
				AcceptEvent();
				return;
			}
			if (_rulerDragging)
			{
				UpdateRulerMeasurement(viewportLocalPointer);
				AcceptEvent();
				return;
			}
			if (_previewDragMode != PreviewDragMode.None)
			{
				UpdateNavigationPreviewDrag(viewportLocalPointer);
				AcceptEvent();
				return;
			}
			if (_pointEditMode != PointEditMode.None)
			{
				UpdatePointEditDrag(viewportLocalPointer);
				AcceptEvent();
				return;
			}
			if (_isPanning)
			{
				ViewOffset -= GetViewportLocalRelative(inputEventMouseMotion) / Zoom;
				UpdateSceneTransform();
				QueueRedrawAll();
				AcceptEvent();
				return;
			}
			if (_dragMode == DragMode.BoxSelection)
			{
				UpdateBoxSelection(viewportLocalPointer);
				AcceptEvent();
				return;
			}
			if (_dragMode == DragMode.DragQueued && _dragStartViewPosition.DistanceTo(viewportLocalPointer) >= 6f)
			{
				BeginQueuedTransformDrag();
			}
			DragMode dragMode = _dragMode;
			if ((uint)(dragMode - 3) <= 3u)
			{
				UpdateTransformDrag(viewportLocalPointer, inputEventMouseMotion.ShiftPressed);
				QueueRedrawAll();
				AcceptEvent();
			}
			else
			{
				UpdateGizmoHover(viewportLocalPointer);
			}
		}
		else
		{
			if (!(@event is InputEventMouseButton inputEventMouseButton))
			{
				return;
			}
			Vector2 viewportLocalPointer2 = GetViewportLocalPointer(inputEventMouseButton);
			EmitSignal(SignalName.MousePositionChanged, ViewToWorld(viewportLocalPointer2));
			if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex != MouseButton.Left)
			{
				ClearInlineTextDoubleClickCandidate();
			}
			else if (inputEventMouseButton.Pressed && inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.ShiftPressed)
			{
				ClearInlineTextDoubleClickCandidate();
			}
			if (inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed && _activeGizmoHandle != GizmoHandleType.None)
			{
				FinishActiveGizmoDrag();
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelUp && inputEventMouseButton.Pressed)
			{
				SetZoom(Zoom * 1.1f, viewportLocalPointer2);
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.WheelDown && inputEventMouseButton.Pressed)
			{
				SetZoom(Zoom / 1.1f, viewportLocalPointer2);
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed && _previewDragMode != PreviewDragMode.None)
			{
				ResetNavigationPreviewDrag();
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed && _guideDragAxis != GuideAxis.None)
			{
				FinishGuideDrag(viewportLocalPointer2);
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed && _rulerDragging)
			{
				FinishRulerMeasurement(viewportLocalPointer2);
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && !inputEventMouseButton.Pressed && _pointEditMode != PointEditMode.None)
			{
				FinishPointEditDrag();
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Middle || CurrentToolMode == ToolMode.Pan)
			{
				_isPanning = inputEventMouseButton.Pressed;
				_lastMousePosition = viewportLocalPointer2;
				if (_isPanning)
				{
					ResetDragMode();
				}
				AcceptEvent();
			}
			else if (inputEventMouseButton.ButtonIndex == MouseButton.Right)
			{
				if (_dragMode == DragMode.BoxSelection)
				{
					CancelBoxSelection();
					AcceptEvent();
					return;
				}
				if (inputEventMouseButton.Pressed)
				{
					if (TryRemoveEditablePointAt(viewportLocalPointer2))
					{
						AcceptEvent();
						return;
					}
					GrabFocus();
					_lastMousePosition = viewportLocalPointer2;
					CanvasItem canvasItem = FindCanvasItemAt(_sceneInstance, viewportLocalPointer2);
					Node hitNode = null;
					if (canvasItem != null && !IsNodeLocked(canvasItem))
					{
						SelectNode(canvasItem);
						hitNode = canvasItem;
					}
					else if ((XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedCount()).GetValueOrDefault() == 0 && _sceneInstance != null && GodotObject.IsInstanceValid(_sceneInstance))
					{
						SelectNode(_sceneInstance);
					}
					Editor?.OpenViewportContextMenu(viewportLocalPointer2, ViewToWorld(viewportLocalPointer2), hitNode);
				}
				AcceptEvent();
			}
			else
			{
				if (inputEventMouseButton.ButtonIndex != MouseButton.Left)
				{
					return;
				}
				if (inputEventMouseButton.Pressed && inputEventMouseButton.DoubleClick && !inputEventMouseButton.ShiftPressed && TryBeginInlineTextEditFromDoubleClick(viewportLocalPointer2, inputEventMouseButton.GlobalPosition))
				{
					CancelActiveInteraction(restoreTransform: true);
					AcceptEvent();
					return;
				}
				if (inputEventMouseButton.Pressed && !inputEventMouseButton.DoubleClick && !inputEventMouseButton.ShiftPressed)
				{
					RememberInlineTextDoubleClickCandidate(viewportLocalPointer2, inputEventMouseButton.GlobalPosition);
				}
				if (inputEventMouseButton.Pressed && TryBeginGuideDrag(viewportLocalPointer2))
				{
					GrabFocus();
					AcceptEvent();
				}
				else if (inputEventMouseButton.Pressed && CurrentToolMode == ToolMode.Ruler)
				{
					BeginRulerMeasurement(viewportLocalPointer2);
					GrabFocus();
					AcceptEvent();
				}
				else if (CurrentToolMode == ToolMode.Ruler)
				{
					AcceptEvent();
				}
				else if (inputEventMouseButton.Pressed && CurrentToolMode == ToolMode.ListSelect)
				{
					OpenListSelectMenu(viewportLocalPointer2);
					GrabFocus();
					AcceptEvent();
				}
				else if (CurrentToolMode == ToolMode.ListSelect)
				{
					AcceptEvent();
				}
				else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed && TryBeginTransformGizmoDrag(viewportLocalPointer2))
				{
					GrabFocus();
					AcceptEvent();
				}
				else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed && TryBeginNavigationPreviewDrag(viewportLocalPointer2))
				{
					GrabFocus();
					AcceptEvent();
				}
				else if (inputEventMouseButton.ButtonIndex == MouseButton.Left && inputEventMouseButton.Pressed && TryBeginPointEdit(viewportLocalPointer2))
				{
					GrabFocus();
					AcceptEvent();
				}
				else if (inputEventMouseButton.Pressed)
				{
					GrabFocus();
					_lastMousePosition = viewportLocalPointer2;
					CanvasItem canvasItem2 = FindCanvasItemAt(_sceneInstance, viewportLocalPointer2);
					bool flag = inputEventMouseButton.ShiftPressed && canvasItem2 != null;
					if (canvasItem2 != null && !IsNodeLocked(canvasItem2) && !flag)
					{
						SelectNode(canvasItem2, inputEventMouseButton.ShiftPressed);
						if (CanStartTransformDrag())
						{
							StartQueuedDrag(viewportLocalPointer2);
						}
					}
					else
					{
						StartBoxSelection(viewportLocalPointer2, inputEventMouseButton.ShiftPressed);
					}
					AcceptEvent();
				}
				else
				{
					if (_dragMode == DragMode.BoxSelection)
					{
						FinishBoxSelection();
					}
					else
					{
						FinishTransformDrag();
					}
					AcceptEvent();
				}
			}
		}
	}

	public override void _Draw()
	{
		DrawRect(new Rect2(Vector2.Zero, base.Size), ModEditorTheme.BackgroundColor);
		DrawGrid();
		DrawOrigin();
	}

	internal void DrawOverlay(Control canvas)
	{
		DrawViewportRect(canvas);
		DrawGuides(canvas);
		DrawSmartSnapLines(canvas);
		DrawSelection(canvas);
		DrawInlineTextEditHandle(canvas);
		DrawTransformHelpers(canvas);
		DrawTransformGizmos(canvas);
		DrawBoxSelection(canvas);
		DrawEditablePoints(canvas);
		DrawLocksAndGroups(canvas, _sceneInstance);
		DrawRulers(canvas);
		DrawRulerMeasurement(canvas);
		DrawNavigationPreview(canvas);
	}

	public override void _Ready()
	{
		base.ClipContents = true;
		base.FocusMode = FocusModeEnum.All;
		base.MouseFilter = MouseFilterEnum.Stop;
		_sceneViewportContainer = new SubViewportContainer
		{
			Name = "SceneViewportContainer"
		};
		_sceneViewportContainer.SetAnchorsPreset(LayoutPreset.FullRect);
		_sceneViewportContainer.MouseFilter = MouseFilterEnum.Ignore;
		_sceneViewportContainer.Stretch = false;
		AddChild(_sceneViewportContainer, forceReadableName: false, InternalMode.Disabled);
		_sceneViewport = new SubViewport
		{
			Name = "SceneViewport",
			Disable3D = true,
			TransparentBg = true,
			GuiDisableInput = true,
			RenderTargetUpdateMode = SubViewport.UpdateMode.WhenVisible
		};
		_sceneViewportContainer.AddChild(_sceneViewport, forceReadableName: false, InternalMode.Disabled);
		_sceneContainer = new Node2D
		{
			Name = "SceneContainer"
		};
		_sceneViewport.AddChild(_sceneContainer, forceReadableName: false, InternalMode.Disabled);
		_overlay = new XW2DViewportOverlay
		{
			Name = "Overlay",
			Viewport = this
		};
		_overlay.SetAnchorsPreset(LayoutPreset.FullRect);
		_overlay.MouseFilter = MouseFilterEnum.Ignore;
		_overlay.ZIndex = 100;
		AddChild(_overlay, forceReadableName: false, InternalMode.Disabled);
		_listSelectMenu = new PopupMenu
		{
			Name = "CanvasItemListSelectMenu",
			MinSize = new Vector2I(260, 0)
		};
		_listSelectMenu.IdPressed += OnListSelectMenuIdPressed;
		AddChild(_listSelectMenu, forceReadableName: false, InternalMode.Disabled);
		_inlineTextEditIcon = (ResourceLoader.Exists("res://addons/ModEditor/Icons/Edit.svg") ? ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Edit.svg", null, ResourceLoader.CacheMode.Reuse) : null);
		base.GuiInput += OnGuiInput;
		base.VisibilityChanged += OnViewportVisibilityChanged;
		base.Resized += delegate
		{
			UpdateSceneViewportSize();
			UpdateSceneTransform();
			QueueRedrawAll();
		};
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection != null)
		{
			xWEditorSelection.SelectionChanged += QueueRedrawAll;
		}
		UpdateSceneViewportSize();
		UpdateSceneTransform();
		UpdateCachedCanvasLayerTransforms();
		SetProcess(enable: false);
	}

	public override void _ExitTree()
	{
		ReleaseDroppedResourceBatches(releaseAll: true);
	}

	public override void _Process(double delta)
	{
		UpdateCachedCanvasLayerTransforms();
	}

	public void SetEditor(XW2DSceneEditor editor)
	{
		Editor = editor;
	}

	public void SetSceneInstance(Node sceneInstance)
	{
		SetSceneInstanceInternal(sceneInstance, releaseHistory: false);
	}

	public void SetSceneInstanceFromHistory(Node sceneInstance)
	{
		SetSceneInstanceInternal(sceneInstance, releaseHistory: false);
	}

	private void SetSceneInstanceInternal(Node sceneInstance, bool releaseHistory)
	{
		ClearSceneInstanceInternal(releaseHistory);
		Editor?.ClearInlineTextCapabilityCache();
		_sceneInstance = sceneInstance;
		if (_sceneInstance != null && GodotObject.IsInstanceValid(_sceneInstance))
		{
			_sceneInstance.GetParent()?.RemoveChild(_sceneInstance);
			_sceneContainer.AddChild(_sceneInstance, forceReadableName: false, InternalMode.Disabled);
			SetCollisionPreviewRecursive(_sceneInstance, ShowHelpers);
			CenterAt(Vector2.Zero);
			RebuildCanvasLayerCache();
			UpdateCachedCanvasLayerTransforms();
			QueueRedrawAll();
		}
	}

	public void ClearSceneInstance()
	{
		ClearSceneInstanceInternal(releaseHistory: false);
	}

	public void DetachSceneInstanceForHistory()
	{
		ClearSceneInstanceInternal(releaseHistory: false);
	}

	private void ClearSceneInstanceInternal(bool releaseHistory)
	{
		ClearInlineTextDoubleClickCandidate();
		if (_activeGizmoHandle != GizmoHandleType.None)
		{
			CancelActiveGizmoDrag();
		}
		else if (_dragMode != DragMode.None || _isPanning)
		{
			CancelActiveInteraction(restoreTransform: true);
		}
		if (releaseHistory)
		{
			ReleaseDroppedResourceBatches(releaseAll: true);
		}
		if (_sceneInstance != null && GodotObject.IsInstanceValid(_sceneInstance))
		{
			SetCollisionPreviewRecursive(_sceneInstance, enabled: false);
			if (_sceneInstance.GetParent() == _sceneContainer)
			{
				_sceneContainer.RemoveChild(_sceneInstance);
			}
		}
		_sceneInstance = null;
		_canvasLayerCache.Clear();
		SetProcess(enable: false);
		XWEditorInterface.Instance?.SetEditedSceneRoot(null);
		QueueRedrawAll();
	}

	private static void SetCollisionPreviewRecursive(Node node, bool enabled)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is IAabbCollisionPreview2D aabbCollisionPreview2D)
		{
			aabbCollisionPreview2D.SetCollisionPreviewDraw(enabled);
		}
		foreach (Node child in node.GetChildren())
		{
			SetCollisionPreviewRecursive(child, enabled);
		}
	}

	public void SetShowHelpers(bool enabled)
	{
		if (_showHelpers != enabled)
		{
			_showHelpers = enabled;
			if (_sceneInstance != null && GodotObject.IsInstanceValid(_sceneInstance))
			{
				SetCollisionPreviewRecursive(_sceneInstance, enabled);
			}
			QueueRedrawAll();
		}
	}

	public void NotifySceneStructureChanged()
	{
		Editor?.ClearInlineTextCapabilityCache();
		RebuildCanvasLayerCache();
		UpdateCachedCanvasLayerTransforms();
		QueueRedrawAll();
	}

	private void StartBoxSelection(Vector2 viewPosition, bool appendSelection)
	{
		_dragMode = DragMode.BoxSelection;
		_dragAppendSelection = appendSelection;
		_dragStartViewPosition = viewPosition;
		_dragCurrentViewPosition = viewPosition;
		_dragStartWorldPosition = ViewToWorld(viewPosition);
		_dragCurrentWorldPosition = _dragStartWorldPosition;
		if (!appendSelection)
		{
			XWEditorInterface.Instance?.ClearSelection();
		}
		QueueRedrawAll();
	}

	private void UpdateBoxSelection(Vector2 viewPosition)
	{
		_dragCurrentViewPosition = viewPosition;
		_dragCurrentWorldPosition = ViewToWorld(viewPosition);
		QueueRedrawAll();
	}

	private void FinishBoxSelection()
	{
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection == null)
		{
			ResetDragMode();
			return;
		}
		if (!_dragAppendSelection)
		{
			xWEditorSelection.Clear();
		}
		List<CanvasItem> list = new List<CanvasItem>();
		if (_dragStartViewPosition.DistanceTo(_dragCurrentViewPosition) <= 6f)
		{
			CanvasItem canvasItem = FindCanvasItemAt(_sceneInstance, _dragStartViewPosition);
			if (canvasItem != null && !IsNodeLocked(canvasItem))
			{
				list.Add(canvasItem);
			}
		}
		else
		{
			FindCanvasItemsInRect(GetBoxSelectionWorldRect(), _sceneInstance, list);
		}
		Node node = null;
		foreach (CanvasItem item in list)
		{
			if (GodotObject.IsInstanceValid(item) && !IsNodeLocked(item))
			{
				xWEditorSelection.SelectNode(item);
				node = item;
			}
		}
		if (node != null)
		{
			XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(node, emitSignal: false);
		}
		ResetDragMode();
		EmitSignal(SignalName.SelectionChanged);
		QueueRedrawAll();
	}

	private void CancelBoxSelection()
	{
		ResetDragMode();
		QueueRedrawAll();
	}

	private Rect2 GetBoxSelectionRect()
	{
		return NormalizeRect(new Rect2(_dragStartViewPosition, _dragCurrentViewPosition - _dragStartViewPosition));
	}

	private Rect2 GetBoxSelectionWorldRect()
	{
		return NormalizeRect(new Rect2(_dragStartWorldPosition, _dragCurrentWorldPosition - _dragStartWorldPosition));
	}

	private void FindCanvasItemsInRect(Rect2 worldRect, Node node, List<CanvasItem> result)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		Array<Node> children = node.GetChildren();
		for (int num = children.Count - 1; num >= 0; num--)
		{
			Node node2 = children[num];
			if (node2 != null)
			{
				FindCanvasItemsInRect(worldRect, node2, result);
			}
		}
		if (node is CanvasItem { Visible: not false } canvasItem && !IsNodeLocked(node))
		{
			Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
			if (ContainsRect(worldRect, worldRectForItem))
			{
				result.Add(canvasItem);
			}
		}
	}

	private void DrawBoxSelection(Control canvas)
	{
		if (_dragMode == DragMode.BoxSelection)
		{
			Rect2 boxSelectionRect = GetBoxSelectionRect();
			canvas.DrawRect(boxSelectionRect, _boxSelectionFillColor);
			canvas.DrawRect(boxSelectionRect, _boxSelectionStrokeColor, filled: false, 1f);
		}
	}

	private CanvasItem FindCanvasItemAt(Node node, Vector2 viewportPosition)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return null;
		}
		Array<Node> children = node.GetChildren();
		for (int num = children.Count - 1; num >= 0; num--)
		{
			Node node2 = children[num];
			if (node2 != null)
			{
				CanvasItem canvasItem = FindCanvasItemAt(node2, viewportPosition);
				if (canvasItem != null)
				{
					return canvasItem;
				}
			}
		}
		if (node is CanvasItem canvasItem2 && canvasItem2.IsVisibleInTree() && GetViewportRectForItem(canvasItem2).HasPoint(viewportPosition))
		{
			return canvasItem2;
		}
		return null;
	}

	public bool BeginInlineTextEditAtViewPosition(Vector2 viewportPosition)
	{
		if (CurrentToolMode != ToolMode.Select || !IsVisibleInTree() || !viewportPosition.IsFinite() || !GodotObject.IsInstanceValid(Editor))
		{
			return false;
		}
		CanvasItem canvasItem = FindCanvasItemAt(_sceneInstance, viewportPosition);
		if (!GodotObject.IsInstanceValid(canvasItem) || IsInlineTextCandidateLocked(canvasItem))
		{
			return false;
		}
		return Editor.BeginInlineTextEdit(canvasItem);
	}

	private void RememberInlineTextDoubleClickCandidate(Vector2 viewportPosition, Vector2 globalPosition)
	{
		ClearInlineTextDoubleClickCandidate();
		_inlineTextDoubleClickCandidateCaptured = true;
		_inlineTextDoubleClickCandidateTicks = Time.GetTicksMsec();
		_inlineTextDoubleClickCandidateGlobalPosition = globalPosition;
		if (CurrentToolMode == ToolMode.Select && GodotObject.IsInstanceValid(Editor))
		{
			CanvasItem canvasItem = FindCanvasItemAt(_sceneInstance, viewportPosition);
			if (GodotObject.IsInstanceValid(canvasItem) && !IsInlineTextCandidateLocked(canvasItem) && Editor.CanInlineEditText(canvasItem))
			{
				_inlineTextDoubleClickCandidate = canvasItem;
			}
		}
	}

	private bool TryBeginInlineTextEditFromDoubleClick(Vector2 viewportPosition, Vector2 globalPosition)
	{
		bool result = ((_inlineTextDoubleClickCandidateCaptured && Time.GetTicksMsec() - _inlineTextDoubleClickCandidateTicks <= 750 && _inlineTextDoubleClickCandidateGlobalPosition.DistanceTo(globalPosition) <= 16f) ? BeginRememberedInlineTextEdit() : BeginInlineTextEditAtViewPosition(viewportPosition));
		ClearInlineTextDoubleClickCandidate();
		return result;
	}

	private void ClearInlineTextDoubleClickCandidate()
	{
		_inlineTextDoubleClickCandidate = null;
		_inlineTextDoubleClickCandidateCaptured = false;
		_inlineTextDoubleClickCandidateTicks = 0uL;
		_inlineTextDoubleClickCandidateGlobalPosition = Vector2.Zero;
	}

	private bool BeginRememberedInlineTextEdit()
	{
		CanvasItem inlineTextDoubleClickCandidate = _inlineTextDoubleClickCandidate;
		if (!GodotObject.IsInstanceValid(inlineTextDoubleClickCandidate) || !inlineTextDoubleClickCandidate.IsVisibleInTree() || IsInlineTextCandidateLocked(inlineTextDoubleClickCandidate) || !GodotObject.IsInstanceValid(_sceneInstance) || (inlineTextDoubleClickCandidate != _sceneInstance && !_sceneInstance.IsAncestorOf(inlineTextDoubleClickCandidate)) || !GodotObject.IsInstanceValid(Editor))
		{
			return false;
		}
		return Editor.BeginInlineTextEdit(inlineTextDoubleClickCandidate);
	}

	private bool IsInlineTextCandidateLocked(Node candidate)
	{
		Node node = candidate;
		while (GodotObject.IsInstanceValid(node))
		{
			if (IsNodeLocked(node))
			{
				return true;
			}
			if (node == _sceneInstance)
			{
				break;
			}
			node = node.GetParent();
		}
		return false;
	}

	private Rect2 GetSelectionBounds()
	{
		bool flag = false;
		Rect2 result = new Rect2(Vector2.Zero, Vector2.Zero);
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
				if (!flag)
				{
					result = worldRectForItem;
					flag = true;
				}
				else
				{
					result = result.Merge(worldRectForItem);
				}
			}
		}
		return result;
	}

	private Rect2 GetViewportRectForItem(CanvasItem item)
	{
		Rect2 worldRectForItem = GetWorldRectForItem(item);
		Vector2 vector = WorldToView(worldRectForItem.Position);
		Vector2 vector2 = WorldToView(worldRectForItem.Position + new Vector2(worldRectForItem.Size.X, 0f));
		Vector2 vector3 = WorldToView(worldRectForItem.Position + worldRectForItem.Size);
		Vector2 vector4 = WorldToView(worldRectForItem.Position + new Vector2(0f, worldRectForItem.Size.Y));
		float num = Mathf.Min(Mathf.Min(vector.X, vector2.X), Mathf.Min(vector3.X, vector4.X));
		float num2 = Mathf.Min(Mathf.Min(vector.Y, vector2.Y), Mathf.Min(vector3.Y, vector4.Y));
		float num3 = Mathf.Max(Mathf.Max(vector.X, vector2.X), Mathf.Max(vector3.X, vector4.X));
		float num4 = Mathf.Max(Mathf.Max(vector.Y, vector2.Y), Mathf.Max(vector3.Y, vector4.Y));
		return new Rect2(new Vector2(num, num2), new Vector2(num3 - num, num4 - num2));
	}

	private Rect2 GetWorldRectForItem(CanvasItem item)
	{
		if (item is Control control)
		{
			return BuildWorldRectFromCanvasRect(control, control.GetGlobalRect());
		}
		if (item is Sprite2D { Texture: not null } sprite2D)
		{
			Vector2 size = sprite2D.Texture.GetSize();
			Vector2 position = sprite2D.Offset - (sprite2D.Centered ? (size * 0.5f) : Vector2.Zero);
			return BuildWorldRectFromLocalRect(sprite2D, new Rect2(position, size));
		}
		if (item is Node2D item2)
		{
			return BuildWorldRectFromLocalRect(item2, new Rect2(Vector2.One * -12f, Vector2.One * 24f));
		}
		return new Rect2(Vector2.Zero, Vector2.Zero);
	}

	public Rect2 GetCanvasItemWorldBounds(CanvasItem item)
	{
		if (item == null || !GodotObject.IsInstanceValid(item))
		{
			return new Rect2(Vector2.Zero, Vector2.Zero);
		}
		return GetWorldRectForItem(item);
	}

	public Rect2 GetCanvasItemViewportBounds(CanvasItem item)
	{
		if (item == null || !GodotObject.IsInstanceValid(item))
		{
			return new Rect2(Vector2.Zero, Vector2.Zero);
		}
		return GetViewportRectForItem(item);
	}

	public Vector2 GetCanvasItemWorldOrigin(CanvasItem item)
	{
		if (item == null || !GodotObject.IsInstanceValid(item))
		{
			return Vector2.Zero;
		}
		return CanvasToSceneWorld((GetCanvasLayerPreviewTransformForItem(item) * item.GetGlobalTransform()).Origin);
	}

	public bool TryResolveLocalPositionForWorldDelta(Node node, Vector2 startLocalPosition, Vector2 worldDelta, out Vector2 localTarget)
	{
		localTarget = startLocalPosition;
		if (!(node is CanvasItem canvasItem) || !GodotObject.IsInstanceValid(canvasItem) || (!(node is Control) && !(node is Node2D)) || _sceneContainer == null || !GodotObject.IsInstanceValid(_sceneContainer) || !startLocalPosition.IsFinite() || !worldDelta.IsFinite())
		{
			return false;
		}
		Transform2D globalTransform = _sceneContainer.GetGlobalTransform();
		Transform2D canvasLayerPreviewTransformForItem = GetCanvasLayerPreviewTransformForItem(canvasItem);
		if (Mathf.IsZeroApprox(globalTransform.Determinant()) || Mathf.IsZeroApprox(canvasLayerPreviewTransformForItem.Determinant()))
		{
			return false;
		}
		Vector2 vector = TransformVector(globalTransform, worldDelta);
		Vector2 vector2 = InverseTransformVector(canvasLayerPreviewTransformForItem, vector);
		Transform2D transform = Transform2D.Identity;
		if (!canvasItem.TopLevel && canvasItem.GetParent() is CanvasItem canvasItem2)
		{
			transform = canvasItem2.GetGlobalTransform();
		}
		if (Mathf.IsZeroApprox(transform.Determinant()))
		{
			return false;
		}
		Vector2 vector3 = InverseTransformVector(transform, vector2);
		localTarget = startLocalPosition + vector3;
		return localTarget.IsFinite();
	}

	private static Vector2 TransformVector(Transform2D transform, Vector2 vector)
	{
		return transform * vector - transform.Origin;
	}

	private static Vector2 InverseTransformVector(Transform2D transform, Vector2 vector)
	{
		Transform2D transform2D = transform.AffineInverse();
		return transform2D * vector - transform2D.Origin;
	}

	private Rect2 BuildWorldRectFromLocalRect(CanvasItem item, Rect2 localRect)
	{
		Transform2D transform2D = GetCanvasLayerPreviewTransformForItem(item) * item.GetGlobalTransform();
		Vector2 p = CanvasToSceneWorld(transform2D * localRect.Position);
		Vector2 p2 = CanvasToSceneWorld(transform2D * (localRect.Position + new Vector2(localRect.Size.X, 0f)));
		Vector2 p3 = CanvasToSceneWorld(transform2D * (localRect.Position + localRect.Size));
		Vector2 p4 = CanvasToSceneWorld(transform2D * (localRect.Position + new Vector2(0f, localRect.Size.Y)));
		return BuildRectFromPoints(p, p2, p3, p4);
	}

	private Rect2 BuildWorldRectFromCanvasRect(CanvasItem item, Rect2 canvasRect)
	{
		Transform2D canvasLayerPreviewTransformForItem = GetCanvasLayerPreviewTransformForItem(item);
		Vector2 p = CanvasToSceneWorld(canvasLayerPreviewTransformForItem * canvasRect.Position);
		Vector2 p2 = CanvasToSceneWorld(canvasLayerPreviewTransformForItem * (canvasRect.Position + new Vector2(canvasRect.Size.X, 0f)));
		Vector2 p3 = CanvasToSceneWorld(canvasLayerPreviewTransformForItem * (canvasRect.Position + canvasRect.Size));
		Vector2 p4 = CanvasToSceneWorld(canvasLayerPreviewTransformForItem * (canvasRect.Position + new Vector2(0f, canvasRect.Size.Y)));
		return BuildRectFromPoints(p, p2, p3, p4);
	}

	private Transform2D GetCanvasLayerPreviewTransformForItem(CanvasItem item)
	{
		CanvasLayer canvasLayer = FindCanvasLayerAncestor(item);
		if (!GodotObject.IsInstanceValid(canvasLayer))
		{
			return Transform2D.Identity;
		}
		return GetCanvasLayerPreviewTransform() * canvasLayer.Transform;
	}

	private CanvasLayer FindCanvasLayerAncestor(Node node)
	{
		Node node2 = node;
		while (GodotObject.IsInstanceValid(node2) && node2 != _sceneContainer)
		{
			if (node2 is CanvasLayer result)
			{
				return result;
			}
			node2 = node2.GetParent();
		}
		return null;
	}

	private Vector2 CanvasToSceneWorld(Vector2 canvasPosition)
	{
		if (_sceneContainer == null || !GodotObject.IsInstanceValid(_sceneContainer))
		{
			return canvasPosition;
		}
		return _sceneContainer.GetGlobalTransform().AffineInverse() * canvasPosition;
	}

	private bool CanStartTransformDrag()
	{
		if (CurrentToolMode != ToolMode.Select && CurrentToolMode != ToolMode.Move && CurrentToolMode != ToolMode.Rotate && CurrentToolMode != ToolMode.Scale)
		{
			return CurrentToolMode == ToolMode.Pivot;
		}
		return true;
	}

	private void SelectNode(Node node, bool appendSelection = false)
	{
		XWEditorSelection xWEditorSelection = XWEditorInterface.Instance?.GetEditorSelection();
		if (xWEditorSelection == null)
		{
			return;
		}
		if (!appendSelection)
		{
			if (!xWEditorSelection.IsSelected(node))
			{
				xWEditorSelection.Clear();
				xWEditorSelection.SelectNode(node);
			}
		}
		else if (xWEditorSelection.IsSelected(node))
		{
			xWEditorSelection.DeselectNode(node);
		}
		else
		{
			xWEditorSelection.SelectNode(node);
		}
		XWEditorInterface.Instance?.GetSceneTreeDock()?.SelectNode(node, emitSignal: false);
		EmitSignal(SignalName.SelectionChanged);
		QueueRedrawAll();
	}

	private void MoveSelectedBy(Vector2 delta)
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (!IsNodeLocked(item))
			{
				if (item is Control control)
				{
					control.Position += delta;
				}
				else if (item is Node2D node2D)
				{
					node2D.Position += delta;
				}
			}
		}
	}

	private void StartQueuedDrag(Vector2 viewPosition)
	{
		_dragMode = DragMode.DragQueued;
		_dragStartViewPosition = viewPosition;
		_dragCurrentViewPosition = viewPosition;
		_dragStartWorldPosition = ViewToWorld(viewPosition);
		_dragCurrentWorldPosition = _dragStartWorldPosition;
		_dragPivotWorldPosition = GetSelectionPivotWorldPosition();
		_dragStartStates.Clear();
	}

	private void BeginQueuedTransformDrag()
	{
		CaptureDragStartStates();
		if (_dragStartStates.Count == 0 && CurrentToolMode != ToolMode.Pivot)
		{
			ResetDragMode();
			return;
		}
		if (_dragStartStates.Count > 0)
		{
			_dragPivotWorldPosition = GetDragSelectionPivotWorldPosition();
			_dragStartSelectionBounds = GetDragSelectionBounds();
			BuildSmartSnapSession();
		}
		if (CurrentToolMode == ToolMode.Pivot)
		{
			CapturePivotStartStates();
		}
		_dragMode = CurrentToolMode switch
		{
			ToolMode.Rotate => DragMode.DragRotate, 
			ToolMode.Scale => DragMode.DragScale, 
			ToolMode.Pivot => DragMode.DragPivot, 
			_ => DragMode.DragMove, 
		};
	}

	private void UpdateTransformDrag(Vector2 viewPosition, bool constrainToLocalAxis = false)
	{
		_dragCurrentViewPosition = viewPosition;
		_dragCurrentWorldPosition = ViewToWorld(viewPosition);
		switch (_dragMode)
		{
		case DragMode.DragMove:
		{
			Vector2 delta = ApplyGridSnap(_dragCurrentWorldPosition - _dragStartWorldPosition);
			GizmoHandleType activeGizmoHandle = _activeGizmoHandle;
			if ((uint)(activeGizmoHandle - 2) <= 1u)
			{
				delta = ConstrainActiveMoveDelta(delta);
			}
			if (UseLocalSpace && constrainToLocalAxis)
			{
				delta = ConstrainDeltaToLocalAxis(delta);
			}
			delta = ApplySmartSnap(delta);
			activeGizmoHandle = _activeGizmoHandle;
			if ((uint)(activeGizmoHandle - 2) <= 1u)
			{
				delta = ConstrainActiveMoveDelta(delta);
			}
			ApplySelectionDelta(delta);
			break;
		}
		case DragMode.DragRotate:
		{
			Vector2 vector = _dragStartWorldPosition - _dragPivotWorldPosition;
			Vector2 to = _dragCurrentWorldPosition - _dragPivotWorldPosition;
			if (vector.LengthSquared() > 0.001f && to.LengthSquared() > 0.001f)
			{
				RotateSelectedBy(ApplyRotationSnap(vector.AngleTo(to)));
			}
			break;
		}
		case DragMode.DragScale:
		{
			if (IsScaleHandle(_activeGizmoHandle))
			{
				UpdateHandleScale(_dragCurrentWorldPosition, constrainToLocalAxis);
				break;
			}
			float num = Mathf.Max((_dragStartWorldPosition - _dragPivotWorldPosition).Length(), 0.001f);
			float scaleFactor = Mathf.Clamp(Mathf.Max((_dragCurrentWorldPosition - _dragPivotWorldPosition).Length(), 0.001f) / num, 0.01f, 100f);
			ScaleSelectedBy(ApplyScaleSnap(scaleFactor));
			break;
		}
		case DragMode.DragPivot:
			MovePivotTo(_dragCurrentWorldPosition);
			break;
		}
	}

	private void FinishTransformDrag()
	{
		CommitTransformDragUndo();
		ResetDragMode();
		QueueRedrawAll();
	}

	private void CommitTransformDragUndo()
	{
		if (_dragMode == DragMode.DragPivot)
		{
			CommitPivotDragUndo();
			return;
		}
		DragMode dragMode = _dragMode;
		bool flag = (uint)(dragMode - 3) <= 2u;
		if (!flag || _dragStartStates.Count == 0)
		{
			return;
		}
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			return;
		}
		string name = _dragMode switch
		{
			DragMode.DragRotate => "旋转 2D 节点", 
			DragMode.DragScale => "缩放 2D 节点", 
			_ => "移动 2D 节点", 
		};
		List<(Node, DragTransformState, DragTransformState)> list = new List<(Node, DragTransformState, DragTransformState)>();
		foreach (var (node2, dragTransformState2) in _dragStartStates)
		{
			if (TryGetTransformState(node2, out var state) && HasTransformChanged(dragTransformState2, state))
			{
				list.Add((node2, dragTransformState2, state));
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		xWUndoRedoManager.CreateAction(name, mergeMode: false, CurrentSceneHistoryId());
		foreach (var item in list)
		{
			var (obj, dragTransformState3, dragTransformState4) = item;
			xWUndoRedoManager.AddDoProperty(obj, "position", Variant.From(in dragTransformState4.Position));
			xWUndoRedoManager.AddUndoProperty(obj, "position", Variant.From(in dragTransformState3.Position));
			xWUndoRedoManager.AddDoProperty(obj, "scale", Variant.From(in dragTransformState4.Scale));
			xWUndoRedoManager.AddUndoProperty(obj, "scale", Variant.From(in dragTransformState3.Scale));
			xWUndoRedoManager.AddDoProperty(obj, "rotation", Variant.From(in dragTransformState4.Rotation));
			xWUndoRedoManager.AddUndoProperty(obj, "rotation", Variant.From(in dragTransformState3.Rotation));
		}
		xWUndoRedoManager.AddDoMethod(this, "NotifyTransformHistoryApplied");
		xWUndoRedoManager.AddUndoMethod(this, "NotifyTransformHistoryApplied");
		xWUndoRedoManager.CommitAction();
		ActivateSceneTransformHistoryDeferred();
	}

	private bool TryGetTransformState(Node node, out DragTransformState state)
	{
		state = default(DragTransformState);
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return false;
		}
		if (node is Control control)
		{
			state = new DragTransformState(control.Position, control.Scale, control.Rotation, GetCanvasItemWorldOrigin(control));
			return true;
		}
		if (node is Node2D node2D)
		{
			state = new DragTransformState(node2D.Position, node2D.Scale, node2D.Rotation, GetCanvasItemWorldOrigin(node2D));
			return true;
		}
		return false;
	}

	private static bool HasTransformChanged(DragTransformState before, DragTransformState after)
	{
		if (before.Position.IsEqualApprox(after.Position) && before.Scale.IsEqualApprox(after.Scale))
		{
			return !Mathf.IsEqualApprox(before.Rotation, after.Rotation);
		}
		return true;
	}

	public void NotifyTransformHistoryApplied()
	{
		Editor?.RefreshDirectTransformSurface();
		QueueRedrawAll();
	}

	public async void ActivateSceneTransformHistoryDeferred()
	{
		int historyId = CurrentSceneHistoryId();
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (historyId == CurrentSceneHistoryId())
		{
			XWEditorInterface.Instance?.GetUndoRedoManager()?.SetCurrentHistoryType(historyId);
		}
	}

	private void ResetDragMode()
	{
		_dragMode = DragMode.None;
		_dragStartStates.Clear();
		_pivotStartStates.Clear();
		_activeSnapX = null;
		_activeSnapY = null;
		_smartSnapXTargets.Clear();
		_smartSnapYTargets.Clear();
		ResetGizmoState();
	}

	private void CaptureDragStartStates()
	{
		_dragStartStates.Clear();
		HashSet<Node> hashSet = new HashSet<Node>();
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item != null && GodotObject.IsInstanceValid(item) && !IsNodeLocked(item) && ((item is Control || item is Node2D) ? true : false))
			{
				hashSet.Add(item);
			}
		}
		foreach (Node item2 in hashSet)
		{
			if (!HasSelectedTransformAncestor(item2, hashSet) && TryGetTransformState(item2, out var state))
			{
				_dragStartStates[item2] = state;
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

	private void ApplySelectionDelta(Vector2 delta)
	{
		foreach (var (node2, dragTransformState2) in _dragStartStates)
		{
			if (GodotObject.IsInstanceValid(node2) && !IsNodeLocked(node2) && TryResolveLocalPositionForWorldDelta(node2, dragTransformState2.Position, delta, out var localTarget))
			{
				SetNodePosition(node2, localTarget);
			}
		}
	}

	private Vector2 ApplyGridSnap(Vector2 delta)
	{
		if (!GridSnapActive)
		{
			return delta;
		}
		return ApplyMoveSnap(_dragPivotWorldPosition, delta);
	}

	private Vector2 ApplyMoveSnap(Vector2 startWorldAnchor, Vector2 delta)
	{
		if (!GridSnapActive)
		{
			return delta;
		}
		Vector2 vector = startWorldAnchor + delta;
		return new Vector2(SnapValue(vector.X - GridOffset.X, MoveSnapStep.X) + GridOffset.X, SnapValue(vector.Y - GridOffset.Y, MoveSnapStep.Y) + GridOffset.Y) - startWorldAnchor;
	}

	private float ApplyRotationSnap(float angleDelta)
	{
		if (!GridSnapActive || RotationSnapStepDegrees <= 0f)
		{
			return angleDelta;
		}
		float step = Mathf.DegToRad(RotationSnapStepDegrees);
		return SnapValue(angleDelta, step);
	}

	private float ApplyScaleSnap(float scaleFactor)
	{
		if (!GridSnapActive || ScaleSnapStep <= 0f)
		{
			return scaleFactor;
		}
		return Mathf.Max(0.01f, 1f + SnapValue(scaleFactor - 1f, ScaleSnapStep));
	}

	public Vector2 TestApplyGridSnapToWorld(Vector2 worldPosition)
	{
		return ApplyGridSnapToWorld(worldPosition);
	}

	public Vector2 TestApplyMoveSnap(Vector2 startWorldAnchor, Vector2 delta)
	{
		return ApplyMoveSnap(startWorldAnchor, delta);
	}

	public float TestApplyRotationSnap(float angleDeltaRadians)
	{
		return ApplyRotationSnap(angleDeltaRadians);
	}

	public float TestApplyScaleSnap(float scaleFactor)
	{
		return ApplyScaleSnap(scaleFactor);
	}

	private Rect2 GetDragSelectionBounds()
	{
		bool flag = false;
		Rect2 result = default(Rect2);
		foreach (Node key in _dragStartStates.Keys)
		{
			if (key is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
				result = (flag ? result.Merge(worldRectForItem) : worldRectForItem);
				flag = true;
			}
		}
		if (!flag)
		{
			return new Rect2(_dragStartWorldPosition, Vector2.Zero);
		}
		return result;
	}

	private Vector2 ConstrainDeltaToLocalAxis(Vector2 delta)
	{
		if (_dragStartStates.Count != 1 || delta.LengthSquared() <= 0.0001f)
		{
			return delta;
		}
		using (System.Collections.Generic.Dictionary<Node, DragTransformState>.KeyCollection.Enumerator enumerator = _dragStartStates.Keys.GetEnumerator())
		{
			if (enumerator.MoveNext() && enumerator.Current is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Transform2D transform2D = GetCanvasLayerPreviewTransformForItem(canvasItem) * canvasItem.GetGlobalTransform();
				Vector2 vector = transform2D.X.Normalized();
				Vector2 vector2 = transform2D.Y.Normalized();
				float num = delta.Dot(vector);
				float num2 = delta.Dot(vector2);
				return (Mathf.Abs(num) >= Mathf.Abs(num2)) ? (vector * num) : (vector2 * num2);
			}
		}
		return delta;
	}

	private Vector2 ApplySmartSnap(Vector2 delta)
	{
		_activeSnapX = null;
		_activeSnapY = null;
		if (!SmartSnapActive || _dragStartStates.Count == 0)
		{
			return delta;
		}
		float threshold = SmartSnapThreshold / Mathf.Max(Zoom, 0.001f);
		float[] movingValues = new float[3]
		{
			_dragStartSelectionBounds.Position.X + delta.X,
			_dragStartSelectionBounds.GetCenter().X + delta.X,
			_dragStartSelectionBounds.End.X + delta.X
		};
		float[] movingValues2 = new float[3]
		{
			_dragStartSelectionBounds.Position.Y + delta.Y,
			_dragStartSelectionBounds.GetCenter().Y + delta.Y,
			_dragStartSelectionBounds.End.Y + delta.Y
		};
		if (TryFindBestSnap(movingValues, _smartSnapXTargets, threshold, out var correction, out var target))
		{
			delta.X += correction;
			_activeSnapX = target;
		}
		if (TryFindBestSnap(movingValues2, _smartSnapYTargets, threshold, out var correction2, out var target2))
		{
			delta.Y += correction2;
			_activeSnapY = target2;
		}
		return delta;
	}

	private void BuildSmartSnapSession()
	{
		_smartSnapXTargets.Clear();
		_smartSnapYTargets.Clear();
		if (SmartSnapActive)
		{
			_smartSnapXTargets.AddRange(_verticalGuides);
			_smartSnapYTargets.AddRange(_horizontalGuides);
			_smartSnapXTargets.Add(0f);
			_smartSnapYTargets.Add(0f);
			int num = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
			int num2 = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
			if (num > 0)
			{
				_smartSnapXTargets.Add((float)num * 0.5f);
			}
			if (num2 > 0)
			{
				_smartSnapYTargets.Add((float)num2 * 0.5f);
			}
			CollectSmartSnapTargets(_sceneInstance, _smartSnapXTargets, _smartSnapYTargets);
		}
	}

	private void CollectSmartSnapTargets(Node node, List<float> xTargets, List<float> yTargets)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is CanvasItem { Visible: not false } canvasItem && !BelongsToDraggedSelection(node))
		{
			Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
			xTargets.Add(worldRectForItem.Position.X);
			xTargets.Add(worldRectForItem.GetCenter().X);
			xTargets.Add(worldRectForItem.End.X);
			yTargets.Add(worldRectForItem.Position.Y);
			yTargets.Add(worldRectForItem.GetCenter().Y);
			yTargets.Add(worldRectForItem.End.Y);
		}
		foreach (Node child in node.GetChildren())
		{
			CollectSmartSnapTargets(child, xTargets, yTargets);
		}
	}

	private bool BelongsToDraggedSelection(Node node)
	{
		Node node2 = node;
		while (node2 != null && GodotObject.IsInstanceValid(node2))
		{
			if (_dragStartStates.ContainsKey(node2))
			{
				return true;
			}
			node2 = node2.GetParent();
		}
		return false;
	}

	private static bool TryFindBestSnap(IReadOnlyList<float> movingValues, IReadOnlyList<float> targets, float threshold, out float correction, out float target)
	{
		correction = 0f;
		target = 0f;
		float num = threshold + 0.001f;
		foreach (float movingValue in movingValues)
		{
			foreach (float target2 in targets)
			{
				float num2 = Mathf.Abs(target2 - movingValue);
				if (!(num2 >= num))
				{
					num = num2;
					correction = target2 - movingValue;
					target = target2;
				}
			}
		}
		return num <= threshold;
	}

	private void RotateSelectedBy(float angleDelta)
	{
		bool flag = _dragStartStates.Count > 1;
		foreach (var (node2, dragTransformState2) in _dragStartStates)
		{
			if (!GodotObject.IsInstanceValid(node2) || IsNodeLocked(node2))
			{
				continue;
			}
			SetNodePosition(node2, dragTransformState2.Position);
			SetNodeRotation(node2, dragTransformState2.Rotation + angleDelta);
			if (flag && node2 is CanvasItem item)
			{
				Vector2 vector = _dragPivotWorldPosition + (dragTransformState2.WorldOrigin - _dragPivotWorldPosition).Rotated(angleDelta);
				Vector2 canvasItemWorldOrigin = GetCanvasItemWorldOrigin(item);
				if (TryResolveLocalPositionForWorldDelta(node2, dragTransformState2.Position, vector - canvasItemWorldOrigin, out var localTarget))
				{
					SetNodePosition(node2, localTarget);
				}
			}
		}
	}

	private void ScaleSelectedBy(float scaleFactor)
	{
		bool flag = _dragStartStates.Count > 1;
		foreach (var (node2, dragTransformState2) in _dragStartStates)
		{
			if (!GodotObject.IsInstanceValid(node2) || IsNodeLocked(node2))
			{
				continue;
			}
			Vector2 scale = dragTransformState2.Scale * scaleFactor;
			SetNodePosition(node2, dragTransformState2.Position);
			SetNodeScale(node2, scale);
			if (flag && node2 is CanvasItem item)
			{
				Vector2 vector = _dragPivotWorldPosition + (dragTransformState2.WorldOrigin - _dragPivotWorldPosition) * scaleFactor;
				Vector2 canvasItemWorldOrigin = GetCanvasItemWorldOrigin(item);
				if (TryResolveLocalPositionForWorldDelta(node2, dragTransformState2.Position, vector - canvasItemWorldOrigin, out var localTarget))
				{
					SetNodePosition(node2, localTarget);
				}
			}
		}
	}

	private static void SetNodePosition(Node node, Vector2 position)
	{
		if (node is Control control)
		{
			control.Position = position;
		}
		else if (node is Node2D node2D)
		{
			node2D.Position = position;
		}
	}

	private static void SetNodeRotation(Node node, float rotation)
	{
		if (node is Control control)
		{
			control.Rotation = rotation;
		}
		else if (node is Node2D node2D)
		{
			node2D.Rotation = rotation;
		}
	}

	private static void SetNodeScale(Node node, Vector2 scale)
	{
		if (node is Control control)
		{
			control.Scale = scale;
		}
		else if (node is Node2D node2D)
		{
			node2D.Scale = scale;
		}
	}

	private void MovePivotTo(Vector2 worldPosition)
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (GodotObject.IsInstanceValid(item) && !IsNodeLocked(item))
			{
				if (item is Control control)
				{
					control.PivotOffset = control.GetGlobalTransform().AffineInverse() * worldPosition;
				}
				else if (item is Sprite2D sprite2D)
				{
					sprite2D.Offset = sprite2D.GetGlobalTransform().AffineInverse() * worldPosition;
				}
				else
				{
					item.SetMeta("_edit_pivot_", worldPosition);
				}
			}
		}
	}

	private void CapturePivotStartStates()
	{
		_pivotStartStates.Clear();
		foreach (Node key in _dragStartStates.Keys)
		{
			if (key is Control control)
			{
				_pivotStartStates[key] = new PivotTransformState(control.PivotOffset);
			}
			else if (key is Sprite2D sprite2D)
			{
				_pivotStartStates[key] = new PivotTransformState(sprite2D.Offset);
			}
			else
			{
				_pivotStartStates[key] = new PivotTransformState(key.HasMeta("_edit_pivot_") ? key.GetMeta("_edit_pivot_").AsVector2() : Vector2.Zero, key.HasMeta("_edit_pivot_"));
			}
		}
	}

	private void CommitPivotDragUndo()
	{
		if (_pivotStartStates.Count == 0)
		{
			return;
		}
		XWUndoRedoManager xWUndoRedoManager = XWEditorInterface.Instance?.GetUndoRedoManager();
		if (!GodotObject.IsInstanceValid(xWUndoRedoManager))
		{
			return;
		}
		List<(Node, PivotTransformState, PivotTransformState)> list = new List<(Node, PivotTransformState, PivotTransformState)>();
		foreach (var (node2, item) in _pivotStartStates)
		{
			if (GodotObject.IsInstanceValid(node2))
			{
				PivotTransformState pivotState = GetPivotState(node2);
				if (!item.Value.IsEqualApprox(pivotState.Value) || item.HadMetadata != pivotState.HadMetadata)
				{
					list.Add((node2, item, pivotState));
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		xWUndoRedoManager.CreateAction("移动 2D 轴心", mergeMode: false, CurrentSceneHistoryId());
		foreach (var item2 in list)
		{
			(Node, PivotTransformState, PivotTransformState) current = item2;
			xWUndoRedoManager.AddDoMethod(this, "ApplyPivotState", current.Item1, Variant.From(in current.Item3.Value), Variant.From(in current.Item3.HadMetadata));
			xWUndoRedoManager.AddUndoMethod(this, "ApplyPivotState", current.Item1, Variant.From(in current.Item2.Value), Variant.From(in current.Item2.HadMetadata));
		}
		xWUndoRedoManager.CommitAction();
		ActivateSceneTransformHistoryDeferred();
	}

	private static PivotTransformState GetPivotState(Node node)
	{
		if (node is Control control)
		{
			return new PivotTransformState(control.PivotOffset);
		}
		if (node is Sprite2D sprite2D)
		{
			return new PivotTransformState(sprite2D.Offset);
		}
		return new PivotTransformState(node.HasMeta("_edit_pivot_") ? node.GetMeta("_edit_pivot_").AsVector2() : Vector2.Zero, node.HasMeta("_edit_pivot_"));
	}

	public void ApplyPivotState(Node node, Vector2 value, bool hasMetadata)
	{
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			if (node is Control control)
			{
				control.PivotOffset = value;
			}
			else if (node is Sprite2D sprite2D)
			{
				sprite2D.Offset = value;
			}
			else if (hasMetadata)
			{
				node.SetMeta("_edit_pivot_", value);
			}
			else if (node.HasMeta("_edit_pivot_"))
			{
				node.RemoveMeta("_edit_pivot_");
			}
			NotifyTransformHistoryApplied();
		}
	}

	private void CancelActiveInteraction(bool restoreTransform)
	{
		if (restoreTransform)
		{
			Node key;
			foreach (KeyValuePair<Node, DragTransformState> dragStartState in _dragStartStates)
			{
				dragStartState.Deconstruct(out key, out var value);
				Node node = key;
				DragTransformState dragTransformState = value;
				if (GodotObject.IsInstanceValid(node))
				{
					SetNodePosition(node, dragTransformState.Position);
					SetNodeScale(node, dragTransformState.Scale);
					SetNodeRotation(node, dragTransformState.Rotation);
				}
			}
			foreach (KeyValuePair<Node, PivotTransformState> pivotStartState in _pivotStartStates)
			{
				pivotStartState.Deconstruct(out key, out var value2);
				Node node2 = key;
				PivotTransformState pivotTransformState = value2;
				ApplyPivotState(node2, pivotTransformState.Value, pivotTransformState.HadMetadata);
			}
		}
		_isPanning = false;
		ResetDragMode();
		ResetGuideDrag();
		if (_pointEditMode != PointEditMode.None)
		{
			CancelPointEdit();
		}
		if (_previewDragMode != PreviewDragMode.None)
		{
			ResetNavigationPreviewDrag();
		}
		QueueRedrawAll();
	}

	private Vector2 GetSelectionPivotWorldPosition()
	{
		Rect2 selectionBounds = GetSelectionBounds();
		if (!(selectionBounds.Size == Vector2.Zero))
		{
			return selectionBounds.GetCenter();
		}
		return _dragStartWorldPosition;
	}

	private Vector2 GetDragSelectionPivotWorldPosition()
	{
		bool flag = false;
		Rect2 rect = new Rect2(Vector2.Zero, Vector2.Zero);
		foreach (Node key in _dragStartStates.Keys)
		{
			if (key is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Rect2 worldRectForItem = GetWorldRectForItem(canvasItem);
				rect = (flag ? rect.Merge(worldRectForItem) : worldRectForItem);
				flag = true;
			}
		}
		if (!flag)
		{
			return _dragStartWorldPosition;
		}
		return rect.GetCenter();
	}

	private void DrawGrid()
	{
		if (IsGridVisible())
		{
			Vector2 vector = new Vector2(Mathf.Max(GridStep.X, 1f), Mathf.Max(GridStep.Y, 1f));
			Vector2 vector2 = ViewToWorld(Vector2.Zero);
			Vector2 vector3 = ViewToWorld(base.Size);
			int num = Mathf.FloorToInt((vector2.X - GridOffset.X) / vector.X) - 1;
			int num2 = Mathf.CeilToInt((vector3.X - GridOffset.X) / vector.X) + 1;
			int num3 = Mathf.FloorToInt((vector2.Y - GridOffset.Y) / vector.Y) - 1;
			int num4 = Mathf.CeilToInt((vector3.Y - GridOffset.Y) / vector.Y) + 1;
			for (int i = num; i <= num2; i++)
			{
				float x = GridOffset.X + (float)i * vector.X;
				float x2 = WorldToView(new Vector2(x, 0f)).X;
				bool flag = PrimaryGridStep.X > 0 && i % PrimaryGridStep.X == 0;
				DrawLine(new Vector2(x2, 0f), new Vector2(x2, base.Size.Y), flag ? _gridMajorColor : _gridMinorColor);
			}
			for (int j = num3; j <= num4; j++)
			{
				float y = GridOffset.Y + (float)j * vector.Y;
				float y2 = WorldToView(new Vector2(0f, y)).Y;
				bool flag2 = PrimaryGridStep.Y > 0 && j % PrimaryGridStep.Y == 0;
				DrawLine(new Vector2(0f, y2), new Vector2(base.Size.X, y2), flag2 ? _gridMajorColor : _gridMinorColor);
			}
		}
	}

	private void DrawOrigin()
	{
		if (ShowOrigin)
		{
			Vector2 vector = WorldToView(Vector2.Zero);
			DrawLine(new Vector2(0f, vector.Y), new Vector2(base.Size.X, vector.Y), _axisXColor, 1f);
			DrawLine(new Vector2(vector.X, 0f), new Vector2(vector.X, base.Size.Y), _axisYColor, 1f);
		}
	}

	private void DrawSelection(Control canvas)
	{
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetTransformableSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem))
			{
				Rect2 viewportRectForItem = GetViewportRectForItem(canvasItem);
				canvas.DrawRect(viewportRectForItem, _selectionFillColor);
				canvas.DrawRect(viewportRectForItem, IsNodeLocked(item) ? _lockedColor : _selectionColor, filled: false, 2f);
			}
		}
	}

	private void DrawInlineTextEditHandle(Control canvas)
	{
		if (!GodotObject.IsInstanceValid(Editor) || Editor.InlineTextEditorVisible)
		{
			return;
		}
		foreach (Node item in XWEditorInterface.Instance?.GetEditorSelection()?.GetSelectedNodes() ?? new List<Node>())
		{
			if (item is CanvasItem canvasItem && GodotObject.IsInstanceValid(canvasItem) && !IsInlineTextCandidateLocked(item) && Editor.CanInlineEditText(item))
			{
				Rect2 viewportRectForItem = GetViewportRectForItem(canvasItem);
				Vector2 position = viewportRectForItem.Position + new Vector2(viewportRectForItem.Size.X - 12f, -12f);
				position.X = Mathf.Clamp(position.X, 2f, Mathf.Max(2f, base.Size.X - 24f - 2f));
				position.Y = Mathf.Clamp(position.Y, 2f, Mathf.Max(2f, base.Size.Y - 24f - 2f));
				Rect2 rect = new Rect2(position, Vector2.One * 24f);
				canvas.DrawRect(rect, new Color(0.035f, 0.09f, 0.052f, 0.94f));
				canvas.DrawRect(rect, ModEditorTheme.SunGoldColor, filled: false, 2f);
				if (GodotObject.IsInstanceValid(_inlineTextEditIcon))
				{
					Rect2 rect2 = rect.Grow(-4f);
					canvas.DrawTextureRect(_inlineTextEditIcon, rect2, tile: false, ModEditorTheme.SunGoldColor);
				}
				break;
			}
		}
	}

	private Vector2 WorldToView(Vector2 world)
	{
		return (world - ViewOffset) * Zoom + base.Size * 0.5f;
	}

	private Vector2 ViewToWorld(Vector2 view)
	{
		return (view - base.Size * 0.5f) / Zoom + ViewOffset;
	}

	private void UpdateSceneViewportSize()
	{
		if (_sceneViewport != null && GodotObject.IsInstanceValid(_sceneViewport))
		{
			Vector2I vector2I = new Vector2I(Mathf.Max(1, Mathf.RoundToInt(base.Size.X)), Mathf.Max(1, Mathf.RoundToInt(base.Size.Y)));
			if (_sceneViewport.Size != vector2I)
			{
				_sceneViewport.Size = vector2I;
			}
			UpdateCachedCanvasLayerTransforms();
		}
	}

	private void UpdateSceneTransform()
	{
		if (_sceneContainer != null)
		{
			_sceneContainer.Position = base.Size * 0.5f - ViewOffset * Zoom;
			_sceneContainer.Scale = Vector2.One * Zoom;
			UpdateCachedCanvasLayerTransforms();
			Editor?.RefreshInlineTextEditorGeometry();
		}
	}

	private void RebuildCanvasLayerCache()
	{
		CanvasLayerCacheRebuildCount++;
		_canvasLayerCache.Clear();
		CollectCanvasLayersForCache(_sceneInstance);
		UpdateCanvasLayerProcessing();
	}

	private void CollectCanvasLayersForCache(Node node)
	{
		if (node == null || !GodotObject.IsInstanceValid(node))
		{
			return;
		}
		if (node is CanvasLayer item)
		{
			_canvasLayerCache.Add(item);
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				CollectCanvasLayersForCache(node2);
			}
		}
	}

	private void UpdateCachedCanvasLayerTransforms()
	{
		if (!GodotObject.IsInstanceValid(_sceneViewport) || _sceneInstance == null || !GodotObject.IsInstanceValid(_sceneInstance))
		{
			return;
		}
		Transform2D canvasLayerPreviewTransform = GetCanvasLayerPreviewTransform();
		bool flag = false;
		for (int num = _canvasLayerCache.Count - 1; num >= 0; num--)
		{
			CanvasLayer canvasLayer = _canvasLayerCache[num];
			if (!GodotObject.IsInstanceValid(canvasLayer) || !canvasLayer.IsInsideTree() || (canvasLayer != _sceneInstance && !_sceneInstance.IsAncestorOf(canvasLayer)))
			{
				_canvasLayerCache.RemoveAt(num);
				flag = true;
			}
			else
			{
				RenderingServer.ViewportSetCanvasTransform(_sceneViewport.GetViewportRid(), canvasLayer.GetCanvas(), canvasLayerPreviewTransform * canvasLayer.Transform);
			}
		}
		if (flag)
		{
			UpdateCanvasLayerProcessing();
		}
	}

	private void UpdateCanvasLayerProcessing()
	{
		SetProcess(IsVisibleInTree() && _canvasLayerCache.Count > 0);
	}

	private Transform2D GetCanvasLayerPreviewTransform()
	{
		Vector2 projectViewportSize = GetProjectViewportSize();
		Vector2 vector = new Vector2(Mathf.Max(1f, _sceneViewport.Size.X), Mathf.Max(1f, _sceneViewport.Size.Y));
		Vector2 vector2 = new Vector2(projectViewportSize.X / vector.X, projectViewportSize.Y / vector.Y) * Zoom;
		Vector2 originPos = WorldToView(Vector2.Zero);
		return new Transform2D(new Vector2(vector2.X, 0f), new Vector2(0f, vector2.Y), originPos);
	}

	private static Vector2 GetProjectViewportSize()
	{
		int b = (int)ProjectSettings.GetSetting("display/window/size/viewport_width");
		int b2 = (int)ProjectSettings.GetSetting("display/window/size/viewport_height");
		return new Vector2(Mathf.Max(1, b), Mathf.Max(1, b2));
	}

	public void QueueRedrawAll()
	{
		QueueRedraw();
		_overlay?.QueueRedraw();
	}

	public void QueueEditorOverlayRedraw()
	{
		_overlay?.QueueRedraw();
	}

	private static Vector2 AbsVector2(Vector2 value)
	{
		return new Vector2(Mathf.Abs(value.X), Mathf.Abs(value.Y));
	}

	private static Rect2 BuildRectFromPoints(Vector2 p0, Vector2 p1, Vector2 p2, Vector2 p3)
	{
		float num = Mathf.Min(Mathf.Min(p0.X, p1.X), Mathf.Min(p2.X, p3.X));
		float num2 = Mathf.Min(Mathf.Min(p0.Y, p1.Y), Mathf.Min(p2.Y, p3.Y));
		float num3 = Mathf.Max(Mathf.Max(p0.X, p1.X), Mathf.Max(p2.X, p3.X));
		float num4 = Mathf.Max(Mathf.Max(p0.Y, p1.Y), Mathf.Max(p2.Y, p3.Y));
		return new Rect2(new Vector2(num, num2), new Vector2(num3 - num, num4 - num2));
	}

	private static float SnapValue(float value, float step)
	{
		if (!(step <= 0f))
		{
			return Mathf.Round(value / step) * step;
		}
		return value;
	}

	private static Rect2 NormalizeRect(Rect2 rect)
	{
		Vector2 position = rect.Position;
		Vector2 size = rect.Size;
		if (size.X < 0f)
		{
			position.X += size.X;
			size.X = 0f - size.X;
		}
		if (size.Y < 0f)
		{
			position.Y += size.Y;
			size.Y = 0f - size.Y;
		}
		return new Rect2(position, size);
	}

	private static Rect2 GrowRect(Rect2 rect, float amount)
	{
		return new Rect2(rect.Position - Vector2.One * amount, rect.Size + Vector2.One * amount * 2f);
	}

	private static bool ContainsRect(Rect2 outer, Rect2 inner)
	{
		if (outer.HasPoint(inner.Position) && outer.HasPoint(inner.Position + new Vector2(inner.Size.X, 0f)) && outer.HasPoint(inner.Position + inner.Size))
		{
			return outer.HasPoint(inner.Position + new Vector2(0f, inner.Size.Y));
		}
		return false;
	}

	private static bool IsNodeLocked(Node node)
	{
		if (node != null && GodotObject.IsInstanceValid(node))
		{
			return node.HasMeta("_edit_lock_");
		}
		return false;
	}

	private static void AssignOwnerForAddedNode(Node node, Node owner)
	{
		if (node != owner && node.Owner == null)
		{
			node.Owner = owner;
		}
		if (!string.IsNullOrWhiteSpace(node.SceneFilePath))
		{
			return;
		}
		foreach (Node child in node.GetChildren())
		{
			if (child != null)
			{
				Node node2 = child;
				if (node2.Owner == null)
				{
					AssignOwnerForAddedNode(node2, owner);
				}
			}
		}
	}

	public void SetToolMode(ToolMode mode)
	{
		if (CurrentToolMode != mode)
		{
			ClearInlineTextDoubleClickCandidate();
			if (_activeGizmoHandle != GizmoHandleType.None)
			{
				CancelActiveGizmoDrag();
			}
			else
			{
				CancelActiveInteraction(restoreTransform: true);
			}
			_listSelectMenu?.Hide();
			_listSelectCandidates.Clear();
			ClearRulerMeasurement();
		}
		CurrentToolMode = mode;
		QueueRedrawAll();
	}

	public void SetZoom(float zoom, Vector2? pivot = null)
	{
		float zoom2 = Zoom;
		float num = Mathf.Clamp(zoom, 0.05f, 32f);
		if (!Mathf.IsEqualApprox(zoom2, num))
		{
			Vector2 vector = pivot ?? (base.Size * 0.5f);
			Vector2 vector2 = ViewToWorld(vector);
			Zoom = num;
			ViewOffset = vector2 - (vector - base.Size * 0.5f) / Zoom;
			UpdateSceneTransform();
			EmitSignal(SignalName.ZoomChanged);
			QueueRedrawAll();
		}
	}

	public void SetViewOffset(Vector2 offset)
	{
		ViewOffset = offset;
		UpdateSceneTransform();
		QueueRedrawAll();
	}

	public void CenterAt(Vector2 worldPosition)
	{
		ViewOffset = worldPosition;
		UpdateSceneTransform();
		QueueRedrawAll();
	}

	public void CenterSelection()
	{
		Rect2 selectionBounds = GetSelectionBounds();
		if (!(selectionBounds.Size == Vector2.Zero))
		{
			CenterAt(selectionBounds.GetCenter());
		}
	}

	public void FrameSelection()
	{
		Rect2 selectionBounds = GetSelectionBounds();
		if (!(selectionBounds.Size == Vector2.Zero))
		{
			float num = Mathf.Max(selectionBounds.Size.X, 1f);
			float num2 = Mathf.Max(selectionBounds.Size.Y, 1f);
			float value = Mathf.Min(base.Size.X / num, base.Size.Y / num2) * 0.75f;
			Zoom = Mathf.Clamp(value, 0.05f, 32f);
			ViewOffset = selectionBounds.GetCenter();
			UpdateSceneTransform();
			EmitSignal(SignalName.ZoomChanged);
			QueueRedrawAll();
		}
	}

	public void SetGridVisibility(GridVisibility visibility)
	{
		GridMode = visibility;
		QueueRedrawAll();
	}

	public bool IsGridVisible()
	{
		if (GridMode != GridVisibility.Show)
		{
			if (GridMode == GridVisibility.ShowWhenSnapping)
			{
				return GridSnapActive;
			}
			return false;
		}
		return true;
	}

	public Dictionary GetState()
	{
		return new Dictionary
		{
			{ "zoom", Zoom },
			{ "ofs", ViewOffset },
			{ "grid_offset", GridOffset },
			{ "grid_step", GridStep },
			{ "primary_grid_step", PrimaryGridStep },
			{ "smart_snap_active", SmartSnapActive },
			{ "grid_snap_active", GridSnapActive },
			{ "move_snap_step", MoveSnapStep },
			{ "rotation_snap_step_degrees", RotationSnapStepDegrees },
			{ "scale_snap_step", ScaleSnapStep },
			{ "smart_snap_threshold", SmartSnapThreshold },
			{
				"grid_visibility",
				(int)GridMode
			},
			{ "show_origin", ShowOrigin },
			{ "show_viewport", ShowViewportRect },
			{ "show_rulers", ShowRulers },
			{ "show_guides", ShowGuides },
			{ "show_helpers", ShowHelpers },
			{ "use_local_space", UseLocalSpace },
			{
				"vertical_guides",
				BuildGuideState(_verticalGuides)
			},
			{
				"horizontal_guides",
				BuildGuideState(_horizontalGuides)
			}
		};
	}

	public void SetState(Dictionary state)
	{
		if (state != null)
		{
			if (state.ContainsKey("zoom"))
			{
				Zoom = Mathf.Clamp(state["zoom"].AsSingle(), 0.05f, 32f);
			}
			if (state.ContainsKey("ofs"))
			{
				ViewOffset = state["ofs"].AsVector2();
			}
			if (state.ContainsKey("grid_offset"))
			{
				GridOffset = state["grid_offset"].AsVector2();
			}
			if (state.ContainsKey("grid_step"))
			{
				GridStep = state["grid_step"].AsVector2();
			}
			if (state.ContainsKey("primary_grid_step"))
			{
				PrimaryGridStep = state["primary_grid_step"].AsVector2I();
			}
			if (state.ContainsKey("smart_snap_active"))
			{
				SmartSnapActive = state["smart_snap_active"].AsBool();
			}
			if (state.ContainsKey("grid_snap_active"))
			{
				GridSnapActive = state["grid_snap_active"].AsBool();
			}
			if (state.ContainsKey("move_snap_step"))
			{
				MoveSnapStep = state["move_snap_step"].AsVector2();
			}
			if (state.ContainsKey("rotation_snap_step_degrees"))
			{
				RotationSnapStepDegrees = state["rotation_snap_step_degrees"].AsSingle();
			}
			if (state.ContainsKey("scale_snap_step"))
			{
				ScaleSnapStep = state["scale_snap_step"].AsSingle();
			}
			if (state.ContainsKey("smart_snap_threshold"))
			{
				SmartSnapThreshold = state["smart_snap_threshold"].AsSingle();
			}
			if (state.ContainsKey("grid_visibility"))
			{
				GridMode = (GridVisibility)state["grid_visibility"].AsInt32();
			}
			if (state.ContainsKey("show_origin"))
			{
				ShowOrigin = state["show_origin"].AsBool();
			}
			if (state.ContainsKey("show_viewport"))
			{
				ShowViewportRect = state["show_viewport"].AsBool();
			}
			if (state.ContainsKey("show_rulers"))
			{
				ShowRulers = state["show_rulers"].AsBool();
			}
			if (state.ContainsKey("show_guides"))
			{
				ShowGuides = state["show_guides"].AsBool();
			}
			if (state.ContainsKey("show_helpers"))
			{
				ShowHelpers = state["show_helpers"].AsBool();
			}
			if (state.ContainsKey("use_local_space"))
			{
				UseLocalSpace = state["use_local_space"].AsBool();
			}
			if (state.ContainsKey("vertical_guides"))
			{
				RestoreGuideState(state["vertical_guides"], _verticalGuides);
			}
			if (state.ContainsKey("horizontal_guides"))
			{
				RestoreGuideState(state["horizontal_guides"], _horizontalGuides);
			}
			UpdateSceneTransform();
			EmitSignal(SignalName.ZoomChanged);
			QueueRedrawAll();
		}
	}

	private static Godot.Collections.Array BuildGuideState(List<float> guides)
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (float guide in guides)
		{
			array.Add(guide);
		}
		return array;
	}

	private static void RestoreGuideState(Variant value, List<float> guides)
	{
		guides.Clear();
		if (value.VariantType != Variant.Type.Array)
		{
			return;
		}
		foreach (Variant item in value.AsGodotArray())
		{
			guides.Add(item.AsSingle());
		}
	}

	public float GetVerticalGuide(int index)
	{
		if (index < 0 || index >= _verticalGuides.Count)
		{
			return 0f / 0f;
		}
		return _verticalGuides[index];
	}

	public float GetHorizontalGuide(int index)
	{
		if (index < 0 || index >= _horizontalGuides.Count)
		{
			return 0f / 0f;
		}
		return _horizontalGuides[index];
	}

	public void ClearGuides()
	{
		_verticalGuides.Clear();
		_horizontalGuides.Clear();
		ResetGuideDrag();
		QueueRedrawAll();
	}
}
