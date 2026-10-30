using System.Collections.Generic;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.FileSystem;
using PVZHE.ModEditor.GUI;
using PVZHE.ModEditor.Layout;
using PVZHE.ModEditor.Registry.BP;
using PVZHE.ModEditor.Registry.Class;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPGraphEdit : GraphEdit
{
	public const long CanvasMenuAddNodeId = 100L;

	public const long CanvasMenuCopyId = 200L;

	public const long CanvasMenuCutId = 201L;

	public const long CanvasMenuDuplicateId = 202L;

	public const long CanvasMenuPasteId = 203L;

	public const long CanvasMenuDeleteId = 204L;

	private XWBPNodeRegistry _registry;

	private Button _extendsClassButton;

	private Button _generateScriptButton;

	private Button _runPreviewButton;

	private HBoxContainer _navigationTools;

	private PanelContainer _previewStatusPanel;

	private Label _previewStatusLabel;

	private Button _minimapToggleButton;

	private Button _resetZoomButton;

	private Button _focusSelectionButton;

	private Label _nodeCountLabel;

	private Label _connectionCountLabel;

	private TextureRect _nodeCountIcon;

	private TextureRect _connectionCountIcon;

	private PopupMenu _variablePopupMenu;

	private PopupMenu _signalPopupMenu;

	private PopupMenu _canvasContextMenu;

	private XWBPVariableData _pendingVariableData;

	private XWBPSignalData _pendingSignalData;

	private Vector2 _pendingDropPosition = Vector2.Zero;

	private Vector2 _lastPointerGraphPosition = Vector2.Zero;

	private bool _isDraggingConnection;

	private StringName _dragFromNode = "";

	private int _dragFromPort = -1;

	private bool _dragIsOutput = true;

	private bool _connectionMadeDuringDrag;

	private bool _disconnectionDuringDrag;

	private bool _recentlyDisconnected;

	private XWBPGraphNode _runtimeHighlightedNode;

	private readonly List<XWBPNodeData> _clipboardNodes = new List<XWBPNodeData>();

	private readonly List<(int fromLocalIdx, int fromPort, int toLocalIdx, int toPort)> _clipboardConnections = new List<(int, int, int, int)>();

	public XWBPEditor Editor { get; set; }

	public XWBPGraphData GraphData { get; private set; }

	public System.Collections.Generic.Dictionary<int, XWBPGraphNode> GraphNodeDictionary { get; } = new System.Collections.Generic.Dictionary<int, XWBPGraphNode>();

	public int ClipboardNodeCount => _clipboardNodes.Count;

	public int ClipboardConnectionCount => _clipboardConnections.Count;

	public Vector2 LastPointerGraphPosition => _lastPointerGraphPosition;

	public XWBPGraphEdit()
	{
		base.ConnectionLinesThickness = 4f;
		base.ConnectionLinesCurvature = 0.5f;
	}

	public override void _Ready()
	{
		_registry = XWBPNodeRegistry.Instance;
		base.ConnectionRequest += OnConnectionRequest;
		base.DisconnectionRequest += OnDisconnectionRequest;
		base.ConnectionDragStarted += OnConnectionDragStarted;
		base.ConnectionDragEnded += OnConnectionDragEnded;
		base.NodeSelected += OnGraphNodeSelected;
		base.NodeDeselected += OnGraphNodeDeselected;
		base.DeleteNodesRequest += OnDeleteNodesRequest;
		base.CopyNodesRequest += OnCopyNodesRequest;
		base.CutNodesRequest += OnCutNodesRequest;
		base.PasteNodesRequest += OnPasteNodesRequest;
		base.DuplicateNodesRequest += OnDuplicateNodesRequest;
		base.PopupRequest += OnPopupRequest;
		_extendsClassButton = GetNodeOrNull<Button>("%ExtendsClassButton");
		_generateScriptButton = GetNodeOrNull<Button>("%GenerateScriptButton");
		_runPreviewButton = GetNodeOrNull<Button>("%RunPreviewButton");
		_navigationTools = GetNodeOrNull<HBoxContainer>("%BlueprintNavigationTools");
		_previewStatusPanel = GetNodeOrNull<PanelContainer>("%PreviewStatusPanel");
		_previewStatusLabel = GetNodeOrNull<Label>("%PreviewStatusLabel");
		_minimapToggleButton = GetNodeOrNull<Button>("%MinimapToggleButton");
		_resetZoomButton = GetNodeOrNull<Button>("%ResetZoomButton");
		_focusSelectionButton = GetNodeOrNull<Button>("%FocusSelectionButton");
		_nodeCountLabel = GetNodeOrNull<Label>("%NodeCountLabel");
		_connectionCountLabel = GetNodeOrNull<Label>("%ConnectionCountLabel");
		_nodeCountIcon = GetNodeOrNull<TextureRect>("%NodeCountIcon");
		_connectionCountIcon = GetNodeOrNull<TextureRect>("%ConnectionCountIcon");
		_variablePopupMenu = GetNodeOrNull<PopupMenu>("%VariablePopupMenu");
		_signalPopupMenu = GetNodeOrNull<PopupMenu>("%SignalPopupMenu");
		CreateExtendsClassUI();
		CreateNavigationUI();
		BuildCanvasContextMenu();
		UpdateExtendsClassLabel();
		if (_extendsClassButton != null)
		{
			_extendsClassButton.Pressed += ExtendsClassButtonPressed;
		}
		if (_generateScriptButton != null)
		{
			_generateScriptButton.Pressed += GenerateScriptButtonPressed;
		}
		if (_runPreviewButton != null)
		{
			_runPreviewButton.Pressed += RunPreviewButtonPressed;
		}
		if (_variablePopupMenu != null)
		{
			_variablePopupMenu.IdPressed += OnVariablePopupMenuIdPressed;
		}
		if (_signalPopupMenu != null)
		{
			_signalPopupMenu.IdPressed += OnSignalPopupMenuIdPressed;
		}
		RefreshNavigationOverview();
	}

	public bool HasGraph()
	{
		return GodotObject.IsInstanceValid(GraphData);
	}

	private bool HasUndoRedoManager()
	{
		if (GodotObject.IsInstanceValid(Editor))
		{
			return GodotObject.IsInstanceValid(Editor.UndoRedoManager);
		}
		return false;
	}

	private void CreateExtendsClassUI()
	{
		HBoxContainer hBoxContainer = Call("get_menu_hbox").As<HBoxContainer>();
		if (GodotObject.IsInstanceValid(hBoxContainer))
		{
			if (GodotObject.IsInstanceValid(_extendsClassButton) && _extendsClassButton.GetParent() != hBoxContainer)
			{
				_extendsClassButton.Reparent(hBoxContainer);
			}
			if (GodotObject.IsInstanceValid(_generateScriptButton) && _generateScriptButton.GetParent() != hBoxContainer)
			{
				_generateScriptButton.Reparent(hBoxContainer);
			}
			if (GodotObject.IsInstanceValid(_runPreviewButton) && _runPreviewButton.GetParent() != hBoxContainer)
			{
				_runPreviewButton.Reparent(hBoxContainer);
			}
			if (GodotObject.IsInstanceValid(_generateScriptButton))
			{
				_generateScriptButton.Icon = XWClassRegistry.Instance.GetUIIcon("TransitionSyncAuto");
			}
			if (GodotObject.IsInstanceValid(_runPreviewButton))
			{
				_runPreviewButton.Icon = XWClassRegistry.Instance.GetUIIcon("MainPlay");
			}
		}
	}

	private void CreateNavigationUI()
	{
		HBoxContainer hBoxContainer = Call("get_menu_hbox").As<HBoxContainer>();
		if (!GodotObject.IsInstanceValid(hBoxContainer))
		{
			return;
		}
		base.ShowMinimapButton = false;
		base.ShowZoomButtons = true;
		base.ShowZoomLabel = true;
		base.MinimapEnabled = true;
		base.MinimapSize = new Vector2(220f, 140f);
		if (GodotObject.IsInstanceValid(_navigationTools) && _navigationTools.GetParent() != hBoxContainer)
		{
			_navigationTools.Reparent(hBoxContainer);
		}
		SetRuntimePreviewState(running: false, "● 预览待机", new Color("7f93a8"), "在受限纯数据环境中运行当前拼图；不会载入或执行生成的 Mod 程序集");
		XWClassRegistry instance = XWClassRegistry.Instance;
		if (GodotObject.IsInstanceValid(_minimapToggleButton))
		{
			_minimapToggleButton.Icon = instance.GetUIIcon("GridMinimap");
			_minimapToggleButton.ButtonPressed = base.MinimapEnabled;
			_minimapToggleButton.Toggled += OnMinimapToggled;
		}
		if (GodotObject.IsInstanceValid(_resetZoomButton))
		{
			_resetZoomButton.Icon = instance.GetUIIcon("ZoomReset");
			_resetZoomButton.Pressed += ResetZoom;
		}
		if (GodotObject.IsInstanceValid(_focusSelectionButton))
		{
			_focusSelectionButton.Icon = instance.GetUIIcon("CenterView");
			_focusSelectionButton.Pressed += delegate
			{
				FocusSelectedNode();
			};
		}
		if (GodotObject.IsInstanceValid(_nodeCountIcon))
		{
			_nodeCountIcon.Texture = instance.GetUIIcon("GraphNode");
		}
		if (GodotObject.IsInstanceValid(_connectionCountIcon))
		{
			_connectionCountIcon.Texture = instance.GetUIIcon("GuiGraphNodePort");
		}
	}

	private void BuildCanvasContextMenu()
	{
		if (!GodotObject.IsInstanceValid(_canvasContextMenu))
		{
			_canvasContextMenu = new PopupMenu
			{
				Name = "BlueprintCanvasContextMenu"
			};
			AddChild(_canvasContextMenu, forceReadableName: false, InternalMode.Disabled);
			Texture2D texture = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Add.svg", null, ResourceLoader.CacheMode.Reuse);
			Texture2D texture2 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCopy.svg", null, ResourceLoader.CacheMode.Reuse);
			Texture2D texture3 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCut.svg", null, ResourceLoader.CacheMode.Reuse);
			Texture2D texture4 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Duplicate.svg", null, ResourceLoader.CacheMode.Reuse);
			Texture2D texture5 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionPaste.svg", null, ResourceLoader.CacheMode.Reuse);
			Texture2D texture6 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse);
			_canvasContextMenu.AddIconItem(texture, "添加拼图", 100, Key.None);
			_canvasContextMenu.AddSeparator();
			_canvasContextMenu.AddIconItem(texture2, "复制", 200, Key.None);
			_canvasContextMenu.AddIconItem(texture3, "剪切", 201, Key.None);
			_canvasContextMenu.AddIconItem(texture4, "重复", 202, Key.None);
			_canvasContextMenu.AddIconItem(texture5, "粘贴到这里", 203, Key.None);
			_canvasContextMenu.AddIconItem(texture6, "删除", 204, Key.None);
			_canvasContextMenu.IdPressed += OnCanvasContextMenuIdPressed;
		}
	}

	public PopupMenu GetCanvasContextMenu()
	{
		return _canvasContextMenu;
	}

	private void OnMinimapToggled(bool enabled)
	{
		base.MinimapEnabled = enabled;
		if (GodotObject.IsInstanceValid(_minimapToggleButton))
		{
			_minimapToggleButton.TooltipText = (enabled ? "隐藏蓝图小地图" : "显示蓝图小地图");
		}
	}

	public void ResetZoom()
	{
		base.Zoom = 1f;
	}

	public bool FocusSelectedNode()
	{
		foreach (KeyValuePair<int, XWBPGraphNode> item in GraphNodeDictionary)
		{
			if (GodotObject.IsInstanceValid(item.Value) && item.Value.Selected)
			{
				FocusNode(item.Key);
				return true;
			}
		}
		return false;
	}

	public void RefreshNavigationOverview()
	{
		int num = (HasGraph() ? GraphNodeDictionary.Count : 0);
		int num2 = (HasGraph() ? GraphData.Connections.Count : 0);
		if (GodotObject.IsInstanceValid(_nodeCountLabel))
		{
			_nodeCountLabel.Text = num.ToString();
		}
		if (GodotObject.IsInstanceValid(_connectionCountLabel))
		{
			_connectionCountLabel.Text = num2.ToString();
		}
		bool flag = false;
		foreach (XWBPGraphNode value in GraphNodeDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value) && value.Selected)
			{
				flag = true;
				break;
			}
		}
		if (GodotObject.IsInstanceValid(_focusSelectionButton))
		{
			_focusSelectionButton.Disabled = !flag;
		}
	}

	private void OnGraphNodeSelected(Node node)
	{
		Editor?.OnGraphNodeSelected(node as XWBPGraphNode);
		RefreshNavigationOverview();
	}

	private void OnGraphNodeDeselected(Node _)
	{
		Editor?.OnGraphNodeDeselected();
		RefreshNavigationOverview();
	}

	public void UpdateExtendsClassLabel()
	{
		if (!GodotObject.IsInstanceValid(_extendsClassButton))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(Editor) || !GodotObject.IsInstanceValid(Editor.BpScriptData))
		{
			_extendsClassButton.Text = "无继承";
			_extendsClassButton.Icon = null;
			return;
		}
		string text = Editor.BpScriptData.ExtendsClass.ToString();
		if (string.IsNullOrEmpty(text))
		{
			_extendsClassButton.Text = "无继承";
			_extendsClassButton.Icon = null;
		}
		else
		{
			_extendsClassButton.Text = text;
			_extendsClassButton.Icon = XWClassRegistry.Instance.GetClassIcon(text);
		}
	}

	private void ExtendsClassButtonPressed()
	{
		if (GodotObject.IsInstanceValid(Editor) && Editor.HasEditScript())
		{
			XWWindowExtendsClassSelector xWWindowExtendsClassSelector = XWWindowExtendsClassSelector.Create();
			xWWindowExtendsClassSelector.ContextEditor = Editor;
			xWWindowExtendsClassSelector.ClassSelected += OnExtendsClassSelected;
			AddChild(xWWindowExtendsClassSelector, forceReadableName: false, InternalMode.Disabled);
			xWWindowExtendsClassSelector.PopupCentered();
		}
	}

	private void OnExtendsClassSelected(StringName className)
	{
		if (GodotObject.IsInstanceValid(Editor) && GodotObject.IsInstanceValid(Editor.BpScriptData))
		{
			Editor.BpScriptData.ExtendsClass = className;
			UpdateExtendsClassLabel();
		}
	}

	private void GenerateScriptButtonPressed()
	{
		Editor?.GenerateScriptButtonPressed();
	}

	private async void RunPreviewButtonPressed()
	{
		if (Editor != null)
		{
			await Editor.RunSafePreviewAsync();
		}
	}

	public Button GetRunPreviewButton()
	{
		return _runPreviewButton;
	}

	public Label GetPreviewStatusLabel()
	{
		return _previewStatusLabel;
	}

	public void SetRuntimePreviewState(bool running, string text, Color accent, string tooltip = "")
	{
		if (GodotObject.IsInstanceValid(_runPreviewButton))
		{
			_runPreviewButton.Disabled = running || !HasGraph();
		}
		if (GodotObject.IsInstanceValid(_previewStatusLabel))
		{
			_previewStatusLabel.Text = text ?? string.Empty;
			_previewStatusLabel.Modulate = accent;
		}
		if (GodotObject.IsInstanceValid(_previewStatusPanel))
		{
			_previewStatusPanel.TooltipText = tooltip ?? string.Empty;
			StyleBoxFlat stylebox = new StyleBoxFlat
			{
				BgColor = new Color(accent, 0.12f),
				BorderColor = new Color(accent, 0.58f),
				CornerRadiusTopLeft = 7,
				CornerRadiusTopRight = 7,
				CornerRadiusBottomLeft = 7,
				CornerRadiusBottomRight = 7,
				BorderWidthLeft = 1,
				BorderWidthTop = 1,
				BorderWidthRight = 1,
				BorderWidthBottom = 1,
				ContentMarginLeft = 8f,
				ContentMarginRight = 8f,
				ContentMarginTop = 4f,
				ContentMarginBottom = 4f
			};
			_previewStatusPanel.AddThemeStyleboxOverride("panel", stylebox);
		}
	}

	public void HighlightRuntimeNode(int nodeId)
	{
		ClearRuntimeHighlight();
		if (GraphNodeDictionary.TryGetValue(nodeId, out var value) && GodotObject.IsInstanceValid(value))
		{
			_runtimeHighlightedNode = value;
			value.Modulate = new Color("ffd261");
			FocusNode(nodeId);
		}
	}

	public void ClearRuntimeHighlight()
	{
		if (GodotObject.IsInstanceValid(_runtimeHighlightedNode))
		{
			_runtimeHighlightedNode.Modulate = Colors.White;
		}
		_runtimeHighlightedNode = null;
	}

	public void Init(XWBPGraphData graphData)
	{
		if (GraphData == graphData)
		{
			return;
		}
		ClearRuntimeHighlight();
		Clear();
		GraphData = graphData;
		if (!HasGraph())
		{
			if (GodotObject.IsInstanceValid(_runPreviewButton))
			{
				_runPreviewButton.Disabled = true;
			}
			return;
		}
		InitNodes();
		InitConnections();
		RefreshNavigationOverview();
		if (GodotObject.IsInstanceValid(_runPreviewButton))
		{
			_runPreviewButton.Disabled = false;
		}
	}

	private void InitNodes()
	{
		if (!HasGraph())
		{
			return;
		}
		foreach (KeyValuePair<int, XWBPNodeData> node in GraphData.Nodes)
		{
			XWBPNodeData value = node.Value;
			if (value != null)
			{
				XWBPNodeType nodeType = _registry.GetNodeType(value.TypeId.ToString());
				if (nodeType == null)
				{
					GD.PrintErr($"[XWBPGraphEdit] 未知节点类型: {value.TypeId}");
					continue;
				}
				nodeType.LoadNodeData(value);
				XWBPGraphNode xWBPGraphNode = XWBPGraphNode.Create();
				xWBPGraphNode.Editor = Editor;
				AddChild(xWBPGraphNode, forceReadableName: false, InternalMode.Disabled);
				xWBPGraphNode.Init(value, nodeType);
				GraphNodeDictionary[value.Id] = xWBPGraphNode;
			}
		}
	}

	private void InitConnections()
	{
		if (!HasGraph())
		{
			return;
		}
		foreach (XWBPNodeConnectionData connection in GraphData.Connections)
		{
			if (IsConnectionDrawable(connection))
			{
				XWBPGraphNode graphNode = GetGraphNode(connection.FromNodeId);
				XWBPGraphNode graphNode2 = GetGraphNode(connection.ToNodeId);
				if (graphNode != null && graphNode2 != null && !IsNodeConnected(graphNode.Name, connection.FromPortIndex, graphNode2.Name, connection.ToPortIndex))
				{
					ConnectNode(graphNode.Name, connection.FromPortIndex, graphNode2.Name, connection.ToPortIndex);
				}
			}
		}
	}

	private bool IsConnectionDrawable(XWBPNodeConnectionData connection)
	{
		if (!HasGraph() || connection == null)
		{
			return false;
		}
		if (!GraphData.IsConnectionStructurallyValid(connection, ignoreOccupiedPorts: true))
		{
			return false;
		}
		if (GetGraphNode(connection.FromNodeId) != null)
		{
			return GetGraphNode(connection.ToNodeId) != null;
		}
		return false;
	}

	public void Clear()
	{
		ClearConnections();
		foreach (Node child in GetChildren())
		{
			if (child is XWBPGraphNode xWBPGraphNode)
			{
				xWBPGraphNode.QueueFree();
			}
		}
		GraphNodeDictionary.Clear();
		GraphData = null;
		RefreshNavigationOverview();
	}

	public XWBPGraphNode GetGraphNode(int nodeId)
	{
		if (!GraphNodeDictionary.TryGetValue(nodeId, out var value))
		{
			return null;
		}
		return value;
	}

	public void RefreshNodeTransform(int nodeId)
	{
		XWBPGraphNode graphNode = GetGraphNode(nodeId);
		XWBPNodeData xWBPNodeData = GraphData?.GetNode(nodeId);
		if (GodotObject.IsInstanceValid(graphNode) && xWBPNodeData != null)
		{
			graphNode.PositionOffset = xWBPNodeData.Position;
			if (xWBPNodeData.Size != Vector2.Zero)
			{
				graphNode.Size = xWBPNodeData.Size;
			}
			QueueRedraw();
		}
	}

	public void RefreshPortEditor(XWBPNodePortData port)
	{
		if (!HasGraph() || !GodotObject.IsInstanceValid(port))
		{
			return;
		}
		foreach (var (nodeId, xWBPNodeData2) in GraphData.Nodes)
		{
			if (xWBPNodeData2 != null && xWBPNodeData2.InputPorts.Contains(port))
			{
				XWBPGraphNode graphNode = GetGraphNode(nodeId);
				if (GodotObject.IsInstanceValid(graphNode))
				{
					graphNode.PortChange();
				}
				break;
			}
		}
	}

	public int GetGraphNodeId(StringName nodeName)
	{
		foreach (KeyValuePair<int, XWBPGraphNode> item in GraphNodeDictionary)
		{
			if (item.Value.Name == (StringName)nodeName.ToString())
			{
				return item.Key;
			}
		}
		return -1;
	}

	public XWBPGraphNode GetGraphNodeByName(StringName nodeName)
	{
		int graphNodeId = GetGraphNodeId(nodeName);
		if (graphNodeId < 0)
		{
			return null;
		}
		return GetGraphNode(graphNodeId);
	}

	private void ReserveNodeId(XWBPNodeData nodeData)
	{
		if (HasGraph() && nodeData != null && nodeData.Id <= 0)
		{
			if (GraphData.NextNodeId < 1)
			{
				GraphData.NextNodeId = 1;
			}
			while (GraphData.Nodes.ContainsKey(GraphData.NextNodeId))
			{
				GraphData.NextNodeId++;
			}
			nodeData.Id = GraphData.NextNodeId;
			GraphData.NextNodeId++;
		}
	}

	private bool HasConnection(int fromNodeId, int fromPort, int toNodeId, int toPort)
	{
		if (!HasGraph())
		{
			return false;
		}
		foreach (XWBPNodeConnectionData connection in GraphData.Connections)
		{
			if (connection != null && connection.FromNodeId == fromNodeId && connection.FromPortIndex == fromPort && connection.ToNodeId == toNodeId && connection.ToPortIndex == toPort)
			{
				return true;
			}
		}
		return false;
	}

	private static XWBPNodeConnectionData CloneConnectionData(XWBPNodeConnectionData connection)
	{
		if (connection != null)
		{
			return new XWBPNodeConnectionData(connection.FromNodeId, connection.FromPortIndex, connection.ToNodeId, connection.ToPortIndex);
		}
		return null;
	}

	public override bool _IsNodeHoverValid(StringName fromNode, int fromPort, StringName toNode, int toPort)
	{
		if (fromNode == toNode || !HasGraph())
		{
			return false;
		}
		int graphNodeId = GetGraphNodeId(fromNode);
		int graphNodeId2 = GetGraphNodeId(toNode);
		if (graphNodeId < 0 || graphNodeId2 < 0)
		{
			return false;
		}
		return GraphData.CanConnect(graphNodeId, fromPort, graphNodeId2, toPort);
	}

	private void OnDeleteNodesRequest(Array<StringName> nodes)
	{
		if (!HasGraph())
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (StringName node2 in nodes)
		{
			int graphNodeId = GetGraphNodeId(node2);
			if (graphNodeId >= 0)
			{
				XWBPNodeData node = GraphData.GetNode(graphNodeId);
				if (node != null && !node.Lock)
				{
					list.Add(graphNodeId);
				}
			}
		}
		RemoveNodesWithUndo(list, "删除节点");
	}

	private void RemoveNodesWithUndo(List<int> nodeIds, string actionName)
	{
		if (!HasGraph() || nodeIds == null || nodeIds.Count == 0)
		{
			return;
		}
		HashSet<int> hashSet = new HashSet<int>();
		Array from = new Array();
		foreach (int nodeId in nodeIds)
		{
			XWBPNodeData from2 = GraphData.GetNode(nodeId);
			if (from2 != null && !from2.Lock && hashSet.Add(nodeId))
			{
				from.Add(Variant.From(in from2));
			}
		}
		if (from.Count == 0)
		{
			return;
		}
		Array from3 = new Array();
		foreach (XWBPNodeConnectionData connection in GraphData.Connections)
		{
			if (connection != null && (hashSet.Contains(connection.FromNodeId) || hashSet.Contains(connection.ToNodeId)))
			{
				from3.Add(Variant.From<XWBPNodeConnectionData>(CloneConnectionData(connection)));
			}
		}
		if (!HasUndoRedoManager())
		{
			DoRemoveNodes(from);
			return;
		}
		Editor.CreateBlueprintAction(actionName);
		Editor.UndoRedoManager.AddDoMethod(this, "DoRemoveNodes", Variant.From(in from));
		Editor.UndoRedoManager.AddUndoMethod(this, "UndoRemoveNodes", Variant.From(in from), Variant.From(in from3));
		Editor.UndoRedoManager.CommitAction();
	}

	public void DoRemoveNodes(Array nodes)
	{
		if (!HasGraph() || nodes == null)
		{
			return;
		}
		foreach (Variant node in nodes)
		{
			XWBPNodeData xWBPNodeData = node.As<XWBPNodeData>();
			if (xWBPNodeData != null)
			{
				DoRemoveNode(xWBPNodeData.Id);
			}
		}
	}

	public void UndoRemoveNodes(Array nodes, Array connections)
	{
		if (!HasGraph() || nodes == null)
		{
			return;
		}
		DeselectAllGraphNodes();
		foreach (Variant node in nodes)
		{
			XWBPNodeData xWBPNodeData = node.As<XWBPNodeData>();
			if (xWBPNodeData != null)
			{
				DoCreateNode(xWBPNodeData);
				XWBPGraphNode graphNode = GetGraphNode(xWBPNodeData.Id);
				if (GodotObject.IsInstanceValid(graphNode))
				{
					graphNode.Selected = true;
				}
			}
		}
		if (connections != null)
		{
			foreach (Variant connection in connections)
			{
				XWBPNodeConnectionData xWBPNodeConnectionData = connection.As<XWBPNodeConnectionData>();
				if (xWBPNodeConnectionData != null)
				{
					DoAddConnection(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex, xWBPNodeConnectionData.ToNodeId, xWBPNodeConnectionData.ToPortIndex);
				}
			}
		}
		RefreshNavigationOverview();
	}

	public void AddConnection(StringName fromNode, int fromPort, StringName toNode, int toPort)
	{
		if (!HasGraph())
		{
			return;
		}
		int from = GetGraphNodeId(fromNode);
		int from2 = GetGraphNodeId(toNode);
		if (from >= 0 && from2 >= 0 && GraphData.CanConnect(from, fromPort, from2, toPort))
		{
			if (HasUndoRedoManager())
			{
				Editor.CreateBlueprintAction("连接节点");
				Editor.UndoRedoManager.AddDoMethod(this, "DoAddConnection", Variant.From(in from), Variant.From(in fromPort), Variant.From(in from2), Variant.From(in toPort));
				Editor.UndoRedoManager.AddUndoMethod(this, "UndoRemoveConnection", Variant.From(in from), Variant.From(in fromPort), Variant.From(in from2), Variant.From(in toPort));
				Editor.UndoRedoManager.CommitAction();
			}
			else
			{
				DoAddConnection(from, fromPort, from2, toPort);
			}
		}
	}

	public void DoAddConnection(int fromNodeId, int fromPort, int toNodeId, int toPort)
	{
		if (!HasGraph())
		{
			return;
		}
		if (!GraphData.AddConnection(fromNodeId, fromPort, toNodeId, toPort))
		{
			XWBPNodeConnectionData connection = new XWBPNodeConnectionData(fromNodeId, fromPort, toNodeId, toPort);
			if (!IsConnectionDrawable(connection))
			{
				return;
			}
		}
		XWBPGraphNode graphNode = GetGraphNode(fromNodeId);
		XWBPGraphNode graphNode2 = GetGraphNode(toNodeId);
		if (graphNode != null && graphNode2 != null)
		{
			if (!IsNodeConnected(graphNode.Name, fromPort, graphNode2.Name, toPort))
			{
				ConnectNode(graphNode.Name, fromPort, graphNode2.Name, toPort);
			}
		}
		else
		{
			GraphData.RemoveConnection(fromNodeId, fromPort, toNodeId, toPort);
		}
		RefreshNavigationOverview();
	}

	public void RemoveConnection(StringName fromNode, int fromPort, StringName toNode, int toPort)
	{
		if (!HasGraph())
		{
			return;
		}
		int from = GetGraphNodeId(fromNode);
		int from2 = GetGraphNodeId(toNode);
		if (from >= 0 && from2 >= 0 && HasConnection(from, fromPort, from2, toPort))
		{
			if (HasUndoRedoManager())
			{
				Editor.CreateBlueprintAction("断开连接");
				Editor.UndoRedoManager.AddDoMethod(this, "UndoRemoveConnection", Variant.From(in from), Variant.From(in fromPort), Variant.From(in from2), Variant.From(in toPort));
				Editor.UndoRedoManager.AddUndoMethod(this, "DoAddConnection", Variant.From(in from), Variant.From(in fromPort), Variant.From(in from2), Variant.From(in toPort));
				Editor.UndoRedoManager.CommitAction();
			}
			else
			{
				UndoRemoveConnection(from, fromPort, from2, toPort);
			}
		}
	}

	public void UndoRemoveConnection(int fromNodeId, int fromPort, int toNodeId, int toPort)
	{
		if (HasGraph())
		{
			XWBPGraphNode graphNode = GetGraphNode(fromNodeId);
			XWBPGraphNode graphNode2 = GetGraphNode(toNodeId);
			if (graphNode != null && graphNode2 != null && IsNodeConnected(graphNode.Name, fromPort, graphNode2.Name, toPort))
			{
				DisconnectNode(graphNode.Name, fromPort, graphNode2.Name, toPort);
			}
			GraphData.RemoveConnection(fromNodeId, fromPort, toNodeId, toPort);
			RefreshNavigationOverview();
		}
	}

	public XWBPGraphNode AddNode(string typeId, Vector2 position)
	{
		if (!HasGraph())
		{
			return null;
		}
		XWBPNodeType nodeType = _registry.GetNodeType(typeId);
		if (nodeType == null)
		{
			GD.PrintErr("[XWBPGraphEdit] 未知节点类型: " + typeId);
			return null;
		}
		XWBPNodeData xWBPNodeData = nodeType.CreateNodeData();
		xWBPNodeData.Position = position;
		return CreateNodeWithUndo(xWBPNodeData);
	}

	public XWBPGraphNode AddNode(XWBPNodeType nodeType, Vector2 position)
	{
		if (!HasGraph() || nodeType == null)
		{
			return null;
		}
		XWBPNodeData xWBPNodeData = nodeType.CreateNodeData();
		xWBPNodeData.NodeType = nodeType;
		xWBPNodeData.Position = position;
		return CreateNodeWithUndo(xWBPNodeData);
	}

	private XWBPGraphNode CreateNodeWithUndo(XWBPNodeData nodeData, string actionName = "创建节点")
	{
		if (!HasGraph() || nodeData == null)
		{
			return null;
		}
		ReserveNodeId(nodeData);
		if (HasUndoRedoManager())
		{
			if (actionName == "创建节点")
			{
				Editor.CreateBlueprintAction("创建节点");
			}
			else
			{
				Editor.CreateBlueprintAction(actionName);
			}
			Editor.UndoRedoManager.AddDoMethod(this, "DoCreateNode", Variant.From(in nodeData));
			Editor.UndoRedoManager.AddUndoMethod(this, "UndoCreateNode", Variant.From(in nodeData));
			Editor.UndoRedoManager.CommitAction();
			return GetGraphNode(nodeData.Id);
		}
		DoCreateNode(nodeData);
		return GetGraphNode(nodeData.Id);
	}

	public void DoCreateNode(XWBPNodeData nodeData)
	{
		if (HasGraph() && nodeData != null && !GraphNodeDictionary.ContainsKey(nodeData.Id))
		{
			XWBPNodeType xWBPNodeType = nodeData.NodeType ?? _registry.GetNodeType(nodeData.TypeId.ToString());
			if (xWBPNodeType == null)
			{
				GD.PrintErr($"[XWBPGraphEdit] 未知节点类型: {nodeData.TypeId}");
				return;
			}
			xWBPNodeType.LoadNodeData(nodeData);
			nodeData.NodeType = xWBPNodeType;
			GraphData.AddNodePreserveId(nodeData);
			int id = nodeData.Id;
			XWBPGraphNode xWBPGraphNode = XWBPGraphNode.Create();
			xWBPGraphNode.Editor = Editor;
			AddChild(xWBPGraphNode, forceReadableName: false, InternalMode.Disabled);
			xWBPGraphNode.Init(nodeData, xWBPNodeType);
			GraphNodeDictionary[id] = xWBPGraphNode;
			RefreshNavigationOverview();
		}
	}

	public void UndoCreateNode(XWBPNodeData nodeData)
	{
		if (nodeData != null)
		{
			DoRemoveNode(nodeData.Id);
		}
	}

	public void RemoveGraphNodeWithName(string nodeName)
	{
		int graphNodeId = GetGraphNodeId(nodeName);
		if (graphNodeId >= 0)
		{
			RemoveGraphNode(graphNodeId);
		}
	}

	public void RemoveGraphNode(int nodeId)
	{
		if (!HasGraph())
		{
			return;
		}
		XWBPNodeData from = GraphData.GetNode(nodeId);
		if (from != null && !from.Lock)
		{
			Array from2 = GetNodeConnections(nodeId);
			if (HasUndoRedoManager())
			{
				Editor.CreateBlueprintAction("删除节点");
				Editor.UndoRedoManager.AddDoMethod(this, "DoRemoveNode", Variant.From(in nodeId));
				Editor.UndoRedoManager.AddUndoMethod(this, "UndoRemoveNode", Variant.From(in from), Variant.From(in from2));
				Editor.UndoRedoManager.CommitAction();
			}
			else
			{
				DoRemoveNode(nodeId);
			}
		}
	}

	public Array GetNodeConnections(int nodeId)
	{
		Array array = new Array();
		if (!HasGraph())
		{
			return array;
		}
		foreach (XWBPNodeConnectionData connection in GraphData.Connections)
		{
			if (connection != null && (connection.FromNodeId == nodeId || connection.ToNodeId == nodeId))
			{
				array.Add(Variant.From<XWBPNodeConnectionData>(CloneConnectionData(connection)));
			}
		}
		return array;
	}

	public void DoRemoveNode(int nodeId)
	{
		if (!HasGraph() || !GraphNodeDictionary.TryGetValue(nodeId, out var value))
		{
			return;
		}
		foreach (Variant nodeConnection in GetNodeConnections(nodeId))
		{
			XWBPNodeConnectionData xWBPNodeConnectionData = nodeConnection.As<XWBPNodeConnectionData>();
			if (xWBPNodeConnectionData != null)
			{
				UndoRemoveConnection(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex, xWBPNodeConnectionData.ToNodeId, xWBPNodeConnectionData.ToPortIndex);
			}
		}
		GraphData.RemoveNode(nodeId);
		value.QueueFree();
		GraphNodeDictionary.Remove(nodeId);
		RefreshNavigationOverview();
	}

	public void UndoRemoveNode(XWBPNodeData nodeData, Array connections)
	{
		if (!HasGraph() || nodeData == null)
		{
			return;
		}
		DoCreateNode(nodeData);
		foreach (Variant connection in connections)
		{
			XWBPNodeConnectionData xWBPNodeConnectionData = connection.As<XWBPNodeConnectionData>();
			if (xWBPNodeConnectionData != null)
			{
				DoAddConnection(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex, xWBPNodeConnectionData.ToNodeId, xWBPNodeConnectionData.ToPortIndex);
			}
		}
	}

	public override void _GuiInput(InputEvent @event)
	{
		if (@event is InputEventMouseMotion inputEventMouseMotion)
		{
			_lastPointerGraphPosition = ToGraphPosition(inputEventMouseMotion.Position);
		}
		else if (@event is InputEventMouseButton inputEventMouseButton)
		{
			_lastPointerGraphPosition = ToGraphPosition(inputEventMouseButton.Position);
		}
		base._GuiInput(@event);
	}

	private void OnPopupRequest(Vector2 atPosition)
	{
		if (GodotObject.IsInstanceValid(_canvasContextMenu) && HasGraph())
		{
			_lastPointerGraphPosition = ToGraphPosition(atPosition);
			ConfigureCanvasContextMenu();
			Vector2 vector = GetScreenPosition() + atPosition;
			_canvasContextMenu.Position = new Vector2I(Mathf.RoundToInt(vector.X), Mathf.RoundToInt(vector.Y));
			_canvasContextMenu.Popup();
		}
	}

	private void ConfigureCanvasContextMenu()
	{
		if (GodotObject.IsInstanceValid(_canvasContextMenu))
		{
			bool flag = GetEditableSelectedNodeIds().Count > 0;
			SetCanvasMenuItemDisabled(100L, !HasGraph());
			SetCanvasMenuItemDisabled(200L, !flag);
			SetCanvasMenuItemDisabled(201L, !flag);
			SetCanvasMenuItemDisabled(202L, !flag);
			SetCanvasMenuItemDisabled(203L, _clipboardNodes.Count == 0);
			SetCanvasMenuItemDisabled(204L, !flag);
		}
	}

	private void SetCanvasMenuItemDisabled(long id, bool disabled)
	{
		int num = _canvasContextMenu?.GetItemIndex((int)id) ?? (-1);
		if (num >= 0)
		{
			_canvasContextMenu.SetItemDisabled(num, disabled);
		}
	}

	private void OnCanvasContextMenuIdPressed(long id)
	{
		if (id != 100)
		{
			long num = id - 200;
			if ((ulong)num <= 4uL)
			{
				switch ((int)num)
				{
				case 0:
					OnCopyNodesRequest();
					break;
				case 1:
					OnCutNodesRequest();
					break;
				case 2:
					OnDuplicateNodesRequest();
					break;
				case 3:
					OnPasteNodesRequest();
					break;
				case 4:
					OnDeleteNodesRequest(GetSelectedGraphNodeNames());
					break;
				}
			}
		}
		else
		{
			ShowNodeSelector(_lastPointerGraphPosition);
		}
	}

	private Array<StringName> GetSelectedGraphNodeNames()
	{
		Array<StringName> array = new Array<StringName>();
		foreach (XWBPGraphNode value in GraphNodeDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value) && value.Selected)
			{
				array.Add(value.Name);
			}
		}
		return array;
	}

	private void ShowNodeSelector(Vector2 position)
	{
		if (HasGraph())
		{
			XWWindowBPNodeSelector xWWindowBPNodeSelector = XWWindowBPNodeSelector.Create();
			xWWindowBPNodeSelector.Editor = Editor;
			xWWindowBPNodeSelector.CurrentGraph = GraphData;
			AddChild(xWWindowBPNodeSelector, forceReadableName: false, InternalMode.Disabled);
			xWWindowBPNodeSelector.NodeTypeSelected += delegate(XWBPNodeType nodeType)
			{
				AddNode(nodeType, position);
			};
			xWWindowBPNodeSelector.PopupCentered();
		}
	}

	private Vector2 ToGraphPosition(Vector2 localPosition)
	{
		return base.ScrollOffset + localPosition / Mathf.Max(base.Zoom, 0.001f);
	}

	private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		_connectionMadeDuringDrag = true;
		AddConnection(fromNode, (int)fromPort, toNode, (int)toPort);
	}

	private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		if (_isDraggingConnection)
		{
			_disconnectionDuringDrag = true;
		}
		else
		{
			_recentlyDisconnected = true;
			CallDeferred("ResetRecentlyDisconnected");
		}
		RemoveConnection(fromNode, (int)fromPort, toNode, (int)toPort);
	}

	public void ResetRecentlyDisconnected()
	{
		_recentlyDisconnected = false;
	}

	private void OnConnectionDragStarted(StringName fromNode, long fromPort, bool isOutput)
	{
		_isDraggingConnection = true;
		_dragFromNode = fromNode;
		_dragFromPort = (int)fromPort;
		_dragIsOutput = isOutput;
		_connectionMadeDuringDrag = false;
		_disconnectionDuringDrag = _recentlyDisconnected;
		_recentlyDisconnected = false;
	}

	private void OnConnectionDragEnded()
	{
		if (!_isDraggingConnection)
		{
			return;
		}
		_isDraggingConnection = false;
		if (_connectionMadeDuringDrag || _disconnectionDuringDrag)
		{
			ResetConnectionDragState();
			return;
		}
		if (!HasGraph())
		{
			ResetConnectionDragState();
			return;
		}
		int graphNodeId = GetGraphNodeId(_dragFromNode);
		XWBPNodeData node = GraphData.GetNode(graphNodeId);
		if (!GodotObject.IsInstanceValid(node))
		{
			ResetConnectionDragState();
			return;
		}
		XWBPNodePortData xWBPNodePortData = (_dragIsOutput ? node.GetOutputPort(_dragFromPort) : node.GetInputPort(_dragFromPort));
		if (!GodotObject.IsInstanceValid(xWBPNodePortData))
		{
			ResetConnectionDragState();
			return;
		}
		ShowFilteredNodeSelector(graphNodeId, _dragFromPort, _dragIsOutput, xWBPNodePortData);
		ResetConnectionDragState();
	}

	private void ResetConnectionDragState()
	{
		_connectionMadeDuringDrag = false;
		_disconnectionDuringDrag = false;
		_recentlyDisconnected = false;
		_dragFromNode = "";
		_dragFromPort = -1;
	}

	private void ShowFilteredNodeSelector(int sourceNodeId, int sourcePortIndex, bool sourceIsOutput, XWBPNodePortData portData)
	{
		if (HasGraph())
		{
			XWWindowBPNodeSelector xWWindowBPNodeSelector = XWWindowBPNodeSelector.Create();
			xWWindowBPNodeSelector.Editor = Editor;
			xWWindowBPNodeSelector.CurrentGraph = GraphData;
			xWWindowBPNodeSelector.SetPortFilter(portData, sourceIsOutput);
			AddChild(xWWindowBPNodeSelector, forceReadableName: false, InternalMode.Disabled);
			xWWindowBPNodeSelector.NodeTypeSelected += delegate(XWBPNodeType nodeType)
			{
				OnNodeSelectedFromPortDrag(nodeType, sourceNodeId, sourcePortIndex, sourceIsOutput);
			};
			xWWindowBPNodeSelector.PopupCentered();
		}
	}

	private void OnNodeSelectedFromPortDrag(XWBPNodeType nodeType, int sourceNodeId, int sourcePortIndex, bool sourceIsOutput)
	{
		if (!HasGraph() || nodeType == null)
		{
			return;
		}
		XWBPNodeData node = GraphData.GetNode(sourceNodeId);
		if (!GodotObject.IsInstanceValid(node))
		{
			return;
		}
		XWBPNodePortData xWBPNodePortData = (sourceIsOutput ? node.GetOutputPort(sourcePortIndex) : node.GetInputPort(sourcePortIndex));
		if (!GodotObject.IsInstanceValid(xWBPNodePortData))
		{
			return;
		}
		int from = (sourceIsOutput ? FindCompatibleInputPort(nodeType, xWBPNodePortData) : FindCompatibleOutputPort(nodeType, xWBPNodePortData));
		XWBPNodeData from2 = nodeType.CreateNodeData();
		from2.NodeType = nodeType;
		from2.Position = ToGraphPosition(GetLocalMousePosition());
		if (from < 0)
		{
			CreateNodeWithUndo(from2);
			return;
		}
		ReserveNodeId(from2);
		if (HasUndoRedoManager())
		{
			Editor.CreateBlueprintAction("创建并连接节点");
			Editor.UndoRedoManager.AddDoMethod(this, "DoCreateAndConnectNode", Variant.From(in from2), Variant.From(in sourceNodeId), Variant.From(in sourcePortIndex), Variant.From(in sourceIsOutput), Variant.From(in from));
			Editor.UndoRedoManager.AddUndoMethod(this, "UndoCreateAndConnectNode", Variant.From(in from2), Variant.From(in sourceNodeId), Variant.From(in sourcePortIndex), Variant.From(in sourceIsOutput), Variant.From(in from));
			Editor.UndoRedoManager.CommitAction();
		}
		else
		{
			DoCreateAndConnectNode(from2, sourceNodeId, sourcePortIndex, sourceIsOutput, from);
		}
	}

	public void DoCreateAndConnectNode(XWBPNodeData nodeData, int sourceNodeId, int sourcePortIndex, bool sourceIsOutput, int targetPortIndex)
	{
		if (HasGraph() && nodeData != null)
		{
			DoCreateNode(nodeData);
			if (sourceIsOutput)
			{
				DoAddConnection(sourceNodeId, sourcePortIndex, nodeData.Id, targetPortIndex);
			}
			else
			{
				DoAddConnection(nodeData.Id, targetPortIndex, sourceNodeId, sourcePortIndex);
			}
		}
	}

	public void UndoCreateAndConnectNode(XWBPNodeData nodeData, int sourceNodeId, int sourcePortIndex, bool sourceIsOutput, int targetPortIndex)
	{
		if (nodeData != null)
		{
			if (sourceIsOutput)
			{
				UndoRemoveConnection(sourceNodeId, sourcePortIndex, nodeData.Id, targetPortIndex);
			}
			else
			{
				UndoRemoveConnection(nodeData.Id, targetPortIndex, sourceNodeId, sourcePortIndex);
			}
			UndoCreateNode(nodeData);
		}
	}

	private static int FindCompatibleInputPort(XWBPNodeType nodeType, XWBPNodePortData sourcePortData)
	{
		int result = -1;
		int num = 0;
		for (int i = 0; i < nodeType.InputPorts.Count; i++)
		{
			XWBPNodePortData xWBPNodePortData = nodeType.InputPorts[i];
			if (xWBPNodePortData != null && sourcePortData.CanConnectTo(xWBPNodePortData))
			{
				int num2 = ScorePortMatch(sourcePortData, xWBPNodePortData);
				if (num2 > num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	private static int FindCompatibleOutputPort(XWBPNodeType nodeType, XWBPNodePortData sourcePortData)
	{
		int result = -1;
		int num = 0;
		for (int i = 0; i < nodeType.OutputPorts.Count; i++)
		{
			XWBPNodePortData xWBPNodePortData = nodeType.OutputPorts[i];
			if (xWBPNodePortData != null && xWBPNodePortData.CanConnectTo(sourcePortData))
			{
				int num2 = ScorePortMatch(xWBPNodePortData, sourcePortData);
				if (num2 > num)
				{
					num = num2;
					result = i;
				}
			}
		}
		return result;
	}

	private static int ScorePortMatch(XWBPNodePortData fromPort, XWBPNodePortData toPort)
	{
		if (fromPort.PortTypeValue == toPort.PortTypeValue)
		{
			if (XWBPNodePortData.IsObjectPortType(fromPort.PortTypeValue) && fromPort.ClassName == toPort.ClassName)
			{
				return 4;
			}
			return 3;
		}
		if (fromPort.PortTypeValue != XWBPNodePortData.PortType.Any && toPort.PortTypeValue != XWBPNodePortData.PortType.Any)
		{
			return 2;
		}
		return 1;
	}

	public override bool _CanDropData(Vector2 atPosition, Variant data)
	{
		if (!HasGraph())
		{
			return false;
		}
		if (IsResourceDropData(data))
		{
			return true;
		}
		if (!XWDragData.IsValidDragData(data))
		{
			return false;
		}
		if (data.Obj is XWDragData xWDragData)
		{
			if (!xWDragData.IsType(XWDragData.Type.BpFunction) && !xWDragData.IsType(XWDragData.Type.BpVariable))
			{
				return xWDragData.IsType(XWDragData.Type.BpSignal);
			}
			return true;
		}
		return false;
	}

	public override void _DropData(Vector2 atPosition, Variant data)
	{
		if (!HasGraph())
		{
			return;
		}
		Vector2 graphPosition = ToGraphPosition(atPosition);
		if (IsResourceDropData(data))
		{
			DropResourceData(graphPosition, data);
		}
		else if (XWDragData.IsValidDragData(data) && data.Obj is XWDragData xWDragData)
		{
			if (xWDragData.IsType(XWDragData.Type.BpFunction))
			{
				DropFunctionData(graphPosition, xWDragData);
			}
			else if (xWDragData.IsType(XWDragData.Type.BpVariable))
			{
				DropVariableData(atPosition, graphPosition, xWDragData);
			}
			else if (xWDragData.IsType(XWDragData.Type.BpSignal))
			{
				DropSignalData(atPosition, graphPosition, xWDragData);
			}
		}
	}

	private static bool IsResourceDropData(Variant data)
	{
		foreach (string item in XWFileSystemDropHelper.ExtractDropPaths(data))
		{
			if (TryLoadDroppedResource(item) != null)
			{
				return true;
			}
		}
		return false;
	}

	private void DropResourceData(Vector2 graphPosition, Variant data)
	{
		int num = 0;
		foreach (string item in XWFileSystemDropHelper.ExtractDropPaths(data))
		{
			Resource resource = TryLoadDroppedResource(item);
			if (GodotObject.IsInstanceValid(resource))
			{
				XWBPNodeLoadResource xWBPNodeLoadResource = XWBPNodeLoadResource.CreateForPath(item, resource);
				if (xWBPNodeLoadResource != null)
				{
					AddNode(xWBPNodeLoadResource, graphPosition + new Vector2(32f * (float)num, 96f * (float)num));
					num++;
				}
			}
		}
	}

	private static Resource TryLoadDroppedResource(string path)
	{
		string text = XWBPNodeLoadResource.NormalizeResourcePath(path);
		if (string.IsNullOrWhiteSpace(text))
		{
			return null;
		}
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(text))
		{
			return null;
		}
		if (DirAccess.DirExistsAbsolute(text))
		{
			return null;
		}
		if (!ResourceLoader.Exists(text))
		{
			return null;
		}
		Resource resource = ResourceLoader.Load<Resource>(text, null, ResourceLoader.CacheMode.Reuse);
		if (!XWCSharpOnlyPolicy.IsSupportedModScriptResource(resource))
		{
			return null;
		}
		return resource;
	}

	private void DropFunctionData(Vector2 graphPosition, XWDragData dragData)
	{
		if (dragData.Data.Obj is XWBPFunctionData xWBPFunctionData)
		{
			XWBPNodeCallMethod xWBPNodeCallMethod = new XWBPNodeCallMethod
			{
				MethodType = XWBPNodeCallMethod.Type.Bp,
				FunctionId = xWBPFunctionData.Id
			};
			xWBPNodeCallMethod.BuildFunction(xWBPFunctionData);
			AddNode(xWBPNodeCallMethod, graphPosition);
		}
	}

	private void DropVariableData(Vector2 localPosition, Vector2 graphPosition, XWDragData dragData)
	{
		if (dragData.Data.Obj is XWBPVariableData pendingVariableData)
		{
			_pendingVariableData = pendingVariableData;
			_pendingDropPosition = graphPosition;
			PopupAtLocalPosition(_variablePopupMenu, localPosition);
		}
	}

	private void OnVariablePopupMenuIdPressed(long id)
	{
		if (GodotObject.IsInstanceValid(_pendingVariableData))
		{
			switch (id)
			{
			case 0L:
				CreateGetNode(_pendingVariableData, _pendingDropPosition);
				break;
			case 1L:
				CreateSetNode(_pendingVariableData, _pendingDropPosition);
				break;
			}
			_pendingVariableData = null;
			_pendingDropPosition = Vector2.Zero;
		}
	}

	private void CreateGetNode(XWBPVariableData variableData, Vector2 graphPosition)
	{
		XWBPNodeGetProperty xWBPNodeGetProperty = new XWBPNodeGetProperty
		{
			MethodType = XWBPNodeGetProperty.Type.Bp,
			VariableId = variableData.Id
		};
		xWBPNodeGetProperty.BuildVariable(variableData);
		AddNode(xWBPNodeGetProperty, graphPosition);
	}

	private void CreateSetNode(XWBPVariableData variableData, Vector2 graphPosition)
	{
		XWBPNodeSetProperty xWBPNodeSetProperty = new XWBPNodeSetProperty
		{
			MethodType = XWBPNodeSetProperty.Type.Bp,
			VariableId = variableData.Id
		};
		xWBPNodeSetProperty.BuildVariable(variableData);
		AddNode(xWBPNodeSetProperty, graphPosition);
	}

	private void DropSignalData(Vector2 localPosition, Vector2 graphPosition, XWDragData dragData)
	{
		if (dragData.Data.Obj is XWBPSignalData pendingSignalData)
		{
			_pendingSignalData = pendingSignalData;
			_pendingDropPosition = graphPosition;
			PopupAtLocalPosition(_signalPopupMenu, localPosition);
		}
	}

	private void OnSignalPopupMenuIdPressed(long id)
	{
		if (GodotObject.IsInstanceValid(_pendingSignalData))
		{
			switch (id)
			{
			case 0L:
				CreateSignalEventNode(_pendingSignalData, _pendingDropPosition);
				break;
			case 1L:
				CreateEmitSignalNode(_pendingSignalData, _pendingDropPosition);
				break;
			}
			_pendingSignalData = null;
			_pendingDropPosition = Vector2.Zero;
		}
	}

	private void CreateSignalEventNode(XWBPSignalData signalData, Vector2 graphPosition)
	{
		XWBPNodeSignalEvent xWBPNodeSignalEvent = new XWBPNodeSignalEvent
		{
			SignalId = signalData.Id
		};
		xWBPNodeSignalEvent.BuildSignal(signalData);
		AddNode(xWBPNodeSignalEvent, graphPosition);
	}

	private void CreateEmitSignalNode(XWBPSignalData signalData, Vector2 graphPosition)
	{
		XWBPNodeEmitSignal xWBPNodeEmitSignal = new XWBPNodeEmitSignal
		{
			SignalId = signalData.Id
		};
		xWBPNodeEmitSignal.BuildSignal(signalData);
		AddNode(xWBPNodeEmitSignal, graphPosition);
	}

	private void PopupAtLocalPosition(PopupMenu popupMenu, Vector2 localPosition)
	{
		if (GodotObject.IsInstanceValid(popupMenu))
		{
			popupMenu.Position = new Vector2I((int)((float)GetWindow().Position.X + base.GlobalPosition.X + localPosition.X), (int)((float)GetWindow().Position.Y + base.GlobalPosition.Y + localPosition.Y));
			popupMenu.Popup();
		}
	}

	private List<int> GetEditableSelectedNodeIds()
	{
		List<int> list = new List<int>();
		if (!HasGraph())
		{
			return list;
		}
		foreach (KeyValuePair<int, XWBPGraphNode> item in GraphNodeDictionary)
		{
			if (GodotObject.IsInstanceValid(item.Value) && item.Value.Selected)
			{
				XWBPNodeData node = GraphData.GetNode(item.Key);
				if (node != null && !node.Lock)
				{
					list.Add(item.Key);
				}
			}
		}
		list.Sort();
		return list;
	}

	private void OnCopyNodesRequest()
	{
		CopySelectedNodes();
	}

	private void OnCutNodesRequest()
	{
		CutSelectedNodes();
	}

	private void OnPasteNodesRequest()
	{
		PasteNodes(_lastPointerGraphPosition);
	}

	private void OnDuplicateNodesRequest()
	{
		DuplicateSelectedNodes();
	}

	public void CopySelectedNodes()
	{
		List<int> editableSelectedNodeIds = GetEditableSelectedNodeIds();
		if (editableSelectedNodeIds.Count != 0)
		{
			CopyNodeIdsToClipboard(editableSelectedNodeIds);
		}
	}

	private void CopyNodeIdsToClipboard(List<int> selectedIds)
	{
		_clipboardNodes.Clear();
		_clipboardConnections.Clear();
		System.Collections.Generic.Dictionary<int, int> dictionary = new System.Collections.Generic.Dictionary<int, int>();
		for (int i = 0; i < selectedIds.Count; i++)
		{
			XWBPNodeData node = GraphData.GetNode(selectedIds[i]);
			if (node != null && !node.Lock)
			{
				XWBPNodeData xWBPNodeData = CloneNodeData(node);
				if (xWBPNodeData != null)
				{
					dictionary[selectedIds[i]] = _clipboardNodes.Count;
					_clipboardNodes.Add(xWBPNodeData);
				}
			}
		}
		foreach (XWBPNodeConnectionData connection in GraphData.Connections)
		{
			if (connection != null && dictionary.TryGetValue(connection.FromNodeId, out var value) && dictionary.TryGetValue(connection.ToNodeId, out var value2))
			{
				_clipboardConnections.Add((value, connection.FromPortIndex, value2, connection.ToPortIndex));
			}
		}
	}

	public void CutSelectedNodes()
	{
		List<int> editableSelectedNodeIds = GetEditableSelectedNodeIds();
		if (editableSelectedNodeIds.Count != 0)
		{
			CopyNodeIdsToClipboard(editableSelectedNodeIds);
			RemoveNodesWithUndo(editableSelectedNodeIds, "剪切节点");
		}
	}

	public void PasteNodes(Vector2 position, string actionName = "粘贴节点")
	{
		if (_clipboardNodes.Count == 0 || !HasGraph())
		{
			return;
		}
		Vector2 position2 = _clipboardNodes[0].Position;
		foreach (XWBPNodeData clipboardNode in _clipboardNodes)
		{
			if (clipboardNode.Position.X < position2.X)
			{
				position2.X = clipboardNode.Position.X;
			}
			if (clipboardNode.Position.Y < position2.Y)
			{
				position2.Y = clipboardNode.Position.Y;
			}
		}
		Vector2 vector = position - position2;
		System.Collections.Generic.Dictionary<int, int> dictionary = new System.Collections.Generic.Dictionary<int, int>();
		Array from = new Array();
		for (int i = 0; i < _clipboardNodes.Count; i++)
		{
			XWBPNodeData from2 = CloneNodeData(_clipboardNodes[i]);
			if (from2 != null)
			{
				from2.Position = _clipboardNodes[i].Position + vector;
				ReserveNodeId(from2);
				dictionary[i] = from2.Id;
				from.Add(Variant.From(in from2));
			}
		}
		Array from3 = new Array();
		foreach (var clipboardConnection in _clipboardConnections)
		{
			if (dictionary.TryGetValue(clipboardConnection.fromLocalIdx, out var value) && dictionary.TryGetValue(clipboardConnection.toLocalIdx, out var value2))
			{
				from3.Add(Variant.From<XWBPNodeConnectionData>(new XWBPNodeConnectionData(value, clipboardConnection.fromPort, value2, clipboardConnection.toPort)));
			}
		}
		if (from.Count != 0)
		{
			if (HasUndoRedoManager())
			{
				Editor.CreateBlueprintAction(actionName);
				Editor.UndoRedoManager.AddDoMethod(this, "DoPasteNodes", Variant.From(in from), Variant.From(in from3));
				Editor.UndoRedoManager.AddUndoMethod(this, "UndoPasteNodes", Variant.From(in from), Variant.From(in from3));
				Editor.UndoRedoManager.CommitAction();
			}
			else
			{
				DoPasteNodes(from, from3);
			}
		}
	}

	public void DoPasteNodes(Array nodes, Array connections)
	{
		if (!HasGraph())
		{
			return;
		}
		DeselectAllGraphNodes();
		foreach (Variant node in nodes)
		{
			XWBPNodeData xWBPNodeData = node.As<XWBPNodeData>();
			if (xWBPNodeData != null)
			{
				DoCreateNode(xWBPNodeData);
				XWBPGraphNode graphNode = GetGraphNode(xWBPNodeData.Id);
				if (graphNode != null)
				{
					graphNode.Selected = true;
				}
			}
		}
		foreach (Variant connection in connections)
		{
			XWBPNodeConnectionData xWBPNodeConnectionData = connection.As<XWBPNodeConnectionData>();
			if (xWBPNodeConnectionData != null)
			{
				DoAddConnection(xWBPNodeConnectionData.FromNodeId, xWBPNodeConnectionData.FromPortIndex, xWBPNodeConnectionData.ToNodeId, xWBPNodeConnectionData.ToPortIndex);
			}
		}
		RefreshNavigationOverview();
	}

	public void UndoPasteNodes(Array nodes, Array connections)
	{
		if (!HasGraph())
		{
			return;
		}
		for (int num = nodes.Count - 1; num >= 0; num--)
		{
			XWBPNodeData xWBPNodeData = nodes[num].As<XWBPNodeData>();
			if (xWBPNodeData != null)
			{
				DoRemoveNode(xWBPNodeData.Id);
			}
		}
	}

	public void DuplicateSelectedNodes()
	{
		List<int> editableSelectedNodeIds = GetEditableSelectedNodeIds();
		if (editableSelectedNodeIds.Count == 0)
		{
			return;
		}
		CopyNodeIdsToClipboard(editableSelectedNodeIds);
		if (_clipboardNodes.Count == 0)
		{
			return;
		}
		Vector2 position = _clipboardNodes[0].Position;
		foreach (XWBPNodeData clipboardNode in _clipboardNodes)
		{
			if (clipboardNode.Position.X < position.X)
			{
				position.X = clipboardNode.Position.X;
			}
			if (clipboardNode.Position.Y < position.Y)
			{
				position.Y = clipboardNode.Position.Y;
			}
		}
		PasteNodes(position + new Vector2(40f, 40f), "重复节点");
	}

	private void DeselectAllGraphNodes()
	{
		foreach (XWBPGraphNode value in GraphNodeDictionary.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.Selected = false;
			}
		}
	}

	private static XWBPNodeData CloneNodeData(XWBPNodeData source)
	{
		if (source == null)
		{
			return null;
		}
		XWBPNodeData xWBPNodeData = new XWBPNodeData
		{
			Id = 0,
			Lock = source.Lock,
			TypeId = source.TypeId,
			Position = source.Position,
			Size = source.Size,
			MetaData = new Dictionary()
		};
		if (source.MetaData != null)
		{
			foreach (KeyValuePair<Variant, Variant> item in source.MetaData)
			{
				xWBPNodeData.MetaData[item.Key] = item.Value;
			}
		}
		foreach (XWBPNodePortData inputPort in source.InputPorts)
		{
			if (inputPort != null)
			{
				xWBPNodeData.InputPorts.Add(new XWBPNodePortData(inputPort.Name, inputPort.PortDirection, inputPort.PortTypeValue, inputPort.ClassName, inputPort.DefaultValue));
			}
		}
		foreach (XWBPNodePortData outputPort in source.OutputPorts)
		{
			if (outputPort != null)
			{
				xWBPNodeData.OutputPorts.Add(new XWBPNodePortData(outputPort.Name, outputPort.PortDirection, outputPort.PortTypeValue, outputPort.ClassName, outputPort.DefaultValue));
			}
		}
		xWBPNodeData.RebuildPortMap();
		return xWBPNodeData;
	}

	private XWBPGraphNode AddNodeFromData(XWBPNodeData nodeData)
	{
		if (!HasGraph() || nodeData == null)
		{
			return null;
		}
		return CreateNodeWithUndo(nodeData);
	}

	public void FocusNode(int nodeId)
	{
		if (GraphNodeDictionary.TryGetValue(nodeId, out var value))
		{
			value.Selected = true;
			Vector2 positionOffset = value.PositionOffset;
			Vector2 size = base.Size;
			base.ScrollOffset = positionOffset - size * 0.5f + value.Size * 0.5f;
		}
	}
}
