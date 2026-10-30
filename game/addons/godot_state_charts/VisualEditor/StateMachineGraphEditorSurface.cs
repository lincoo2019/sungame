using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

[Tool]
public partial class StateMachineGraphEditorSurface : GraphEdit
{
	private enum DetailsContextKind
	{
		Definition,
		State,
		Transition
	}

	private enum ToolbarAction
	{
		Validate = 1,
		Compile,
		ToggleDetails,
		ToggleDiagnostics
	}

	private const float NarrowLayoutWidth = 620f;

	public const long CanvasMenuAddAtomicId = 100L;

	public const long CanvasMenuAddCompoundId = 101L;

	public const long CanvasMenuAddParallelId = 102L;

	public const long CanvasMenuAddHistoryId = 103L;

	public const long CanvasMenuCopyId = 200L;

	public const long CanvasMenuCutId = 201L;

	public const long CanvasMenuDuplicateId = 202L;

	public const long CanvasMenuPasteId = 203L;

	public const long CanvasMenuDeleteId = 204L;

	private readonly System.Collections.Generic.Dictionary<string, StateMachineGraphNode> _nodes = new System.Collections.Generic.Dictionary<string, StateMachineGraphNode>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, Vector2> _moveOrigins = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, string> _parentStateIds = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);

	private readonly HashSet<(string Source, string Target)> _visualConnections = new HashSet<(string, string)>();

	private readonly HashSet<string> _visibleStateIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<string, StateMachineTransitionChip> _transitionChips = new System.Collections.Generic.Dictionary<string, StateMachineTransitionChip>(StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<(string Source, string Target), List<StateMachineTransitionChip>> _transitionChipGroups = new System.Collections.Generic.Dictionary<(string, string), List<StateMachineTransitionChip>>();

	private readonly System.Collections.Generic.Dictionary<string, List<StateMachineTransitionChip>> _transitionChipsByState = new System.Collections.Generic.Dictionary<string, List<StateMachineTransitionChip>>(StringComparer.Ordinal);

	private readonly HashSet<(string Source, string Target)> _transitionGroupsNeedingLayout = new HashSet<(string, string)>();

	private readonly HashSet<string> _activeStateProjectionIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _pendingStateProjectionIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _activePendingTransitionChipIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _nextPendingTransitionChipIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly HashSet<string> _layoutMutationMovedStateIds = new HashSet<string>(StringComparer.Ordinal);

	private readonly List<string> _movingStateIds = new List<string>();

	private readonly System.Collections.Generic.Dictionary<string, Vector2> _movingStatePositions = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);

	private StateMachineDetailsPanel _details;

	private StateMachineDiagnosticsPanel _diagnostics;

	private StateMachineSimulationPanel _simulation;

	private OptionButton _newStateKind;

	private OptionButton _newStateType;

	private MenuButton _compactStateTypeMenu;

	private Button _addStateButton;

	private MenuButton _narrowAddStateMenu;

	private Button _detailsToggleButton;

	private Button _simulationToggle;

	private PanelContainer _emptyStatePanel;

	private Label _emptyStateHint;

	private Button _createDefinitionButton;

	private Button _openDefinitionButton;

	private Button _initializeDefinitionButton;

	private PanelContainer _compositionFailureBanner;

	private HBoxContainer _compactToolbar;

	private bool _detailsRequestedVisible = true;

	private bool _narrowDetailsVisible;

	private bool _refreshing;

	private StateMachineDefinition _renderedDefinition;

	private DetailsContextKind _detailsContext;

	private string _detailsStableId = string.Empty;

	private bool _viewportDirty;

	private bool _viewportCaptureQueued;

	private Vector2 _pendingScrollOffset;

	private float _pendingZoom = 1f;

	private Timer _viewportDebounceTimer;

	private PopupMenu _canvasContextMenu;

	private Vector2 _lastPointerGraphPosition;

	private bool _surfaceSignalsConnected;

	private bool _trackingNodeMove;

	private bool _transitionChipLayoutQueued;

	private string _queuedTransitionChipLayoutReason = string.Empty;

	private string _lastCompletedTransitionChipId = string.Empty;

	private string _selectedTransitionChipId = string.Empty;

	public StateMachineGraphController GraphController { get; private set; }

	public IStateMachineUndoAdapter UndoAdapter { get; private set; }

	public StateMachineDefinition Definition => GraphController?.Definition;

	public StateMachineLayout Layout => GraphController?.Layout;

	public StateMachineSimulationPanel SimulationPanel => _simulation;

	public StateMachineDetailsPanel DetailsPanel => _details;

	public bool IsWorkbenchActive { get; private set; }

	public bool IsHiddenWorkQuiescent
	{
		get
		{
			if (!IsWorkbenchActive && !IsProcessing() && !_transitionChipLayoutQueued && (_simulation == null || _simulation.IsQuiescent))
			{
				if (_details != null)
				{
					return _details.ActiveNumericDebounceTimerCount == 0;
				}
				return true;
			}
			return false;
		}
	}

	public string ActiveDetailsKind => _detailsContext.ToString();

	public string ActiveDetailsStableId => _detailsStableId;

	public Vector2 LastPointerGraphPosition => _lastPointerGraphPosition;

	public int FullGraphRebuildCount { get; private set; }

	public int IncrementalGraphRefreshCount { get; private set; }

	public int GraphNodeCreateCount { get; private set; }

	public int LastConnectionSelectionGroupSize { get; private set; }

	public int VisibleStateNodeCount => _visibleStateIds.Count;

	public int VisibleConnectionCount => _visualConnections.Count;

	public int TransitionChipCount => _transitionChips.Count;

	public int VisibleTransitionChipCount
	{
		get
		{
			int num = 0;
			foreach (StateMachineTransitionChip value in _transitionChips.Values)
			{
				if (GodotObject.IsInstanceValid(value) && value.Visible)
				{
					num++;
				}
			}
			return num;
		}
	}

	public int TransitionChipCreateCount { get; private set; }

	public int TransitionChipSynchronizeCount { get; private set; }

	public int TransitionChipPositionUpdateCount { get; private set; }

	public int LastTransitionChipRelayoutCount { get; private set; }

	public string LastTransitionChipRelayoutReason { get; private set; } = string.Empty;

	public int ActiveViewportDebounceTimerCount
	{
		get
		{
			if (!GodotObject.IsInstanceValid(_viewportDebounceTimer) || _viewportDebounceTimer.IsStopped())
			{
				return 0;
			}
			return 1;
		}
	}

	public event Action<Resource, string> ResourcePickerRequested;

	public event Action<Resource, Resource, string, int> GuardEditRequested;

	public event Action<StateMachineCallbackCatalogEntry, StateMachineCallbackPhase> CallbackSourceOpenRequested;

	public event Action DefinitionChanged;

	public event Action LayoutChanged;

	public event Action CreateDefinitionRequested;

	public event Action OpenDefinitionRequested;

	public override void _Ready()
	{
		StateMachineAuthorTypeRegistry.Changed += OnAuthorTypesChanged;
		ConnectSurfaceSignals();
		SetProcess(enable: false);
		_viewportDebounceTimer = new Timer
		{
			Name = "ViewportDebounceTimer",
			OneShot = true,
			WaitTime = 0.18,
			ProcessMode = ProcessModeEnum.Always
		};
		_viewportDebounceTimer.Timeout += FlushViewportState;
		AddChild(_viewportDebounceTimer, forceReadableName: false, InternalMode.Disabled);
		BuildGameToolbar();
		BindZoomButtons();
		BuildOverlayPanels();
		BuildCanvasContextMenu();
		CallDeferred("ApplyResponsiveLayout");
		SetWorkbenchActive(IsVisibleInTree());
	}

	public override void _ExitTree()
	{
		StateMachineAuthorTypeRegistry.Changed -= OnAuthorTypesChanged;
		FlushViewportState();
		DisconnectSurfaceSignals();
		if (GodotObject.IsInstanceValid(_viewportDebounceTimer))
		{
			_viewportDebounceTimer.Stop();
			_viewportDebounceTimer.Timeout -= FlushViewportState;
		}
		DisconnectOverlaySignals();
		if (GraphController != null)
		{
			GraphController.MutationApplied -= OnControllerMutationApplied;
		}
		SetWorkbenchActive(active: false);
	}

	public override void _Process(double delta)
	{
		if (!_trackingNodeMove || !IsWorkbenchActive || !IsVisibleInTree())
		{
			SetProcess(enable: false);
		}
		else
		{
			UpdateMovingTransitionChipPositions();
		}
	}

	private void ConnectSurfaceSignals()
	{
		if (!_surfaceSignalsConnected)
		{
			base.ConnectionRequest += OnConnectionRequest;
			base.DisconnectionRequest += OnDisconnectionRequest;
			base.DeleteNodesRequest += OnDeleteNodesRequest;
			base.CopyNodesRequest += OnCopyNodesRequest;
			base.CutNodesRequest += OnCutNodesRequest;
			base.PasteNodesRequest += OnPasteNodesRequest;
			base.DuplicateNodesRequest += OnDuplicateNodesRequest;
			base.PopupRequest += OnPopupRequest;
			base.BeginNodeMove += OnBeginNodeMove;
			base.EndNodeMove += OnEndNodeMove;
			base.NodeSelected += OnNodeSelected;
			base.ScrollOffsetChanged += OnScrollOffsetChanged;
			base.GuiInput += OnGraphGuiInput;
			base.VisibilityChanged += OnSurfaceVisibilityChanged;
			base.Resized += ApplyResponsiveLayout;
			_surfaceSignalsConnected = true;
		}
	}

	private void DisconnectSurfaceSignals()
	{
		if (_surfaceSignalsConnected)
		{
			base.ConnectionRequest -= OnConnectionRequest;
			base.DisconnectionRequest -= OnDisconnectionRequest;
			base.DeleteNodesRequest -= OnDeleteNodesRequest;
			base.CopyNodesRequest -= OnCopyNodesRequest;
			base.CutNodesRequest -= OnCutNodesRequest;
			base.PasteNodesRequest -= OnPasteNodesRequest;
			base.DuplicateNodesRequest -= OnDuplicateNodesRequest;
			base.PopupRequest -= OnPopupRequest;
			base.BeginNodeMove -= OnBeginNodeMove;
			base.EndNodeMove -= OnEndNodeMove;
			base.NodeSelected -= OnNodeSelected;
			base.ScrollOffsetChanged -= OnScrollOffsetChanged;
			base.GuiInput -= OnGraphGuiInput;
			base.VisibilityChanged -= OnSurfaceVisibilityChanged;
			base.Resized -= ApplyResponsiveLayout;
			_surfaceSignalsConnected = false;
		}
	}

	public void SetWorkbenchActive(bool active)
	{
		if (!active)
		{
			_details?.FlushPendingNumericCommit();
			FlushViewportState();
		}
		IsWorkbenchActive = active;
		_simulation?.SetWorkbenchActive(IsWorkbenchActive);
		_details?.SetEditingActive(IsWorkbenchActive);
		if (IsWorkbenchActive)
		{
			QueueTransitionChipLayoutAfterGraphSettles("SetWorkbenchActive");
			SetProcess(_trackingNodeMove);
			return;
		}
		_trackingNodeMove = false;
		_movingStateIds.Clear();
		_movingStatePositions.Clear();
		SetProcess(enable: false);
		if (_simulationToggle != null)
		{
			_simulationToggle.ButtonPressed = false;
		}
		if (_simulation != null)
		{
			_simulation.Visible = false;
		}
	}

	public StateMachineGraphNode FindStateNode(string stableId)
	{
		if (!_nodes.TryGetValue(stableId ?? string.Empty, out var value))
		{
			return null;
		}
		return value;
	}

	public StateMachineTransitionChip FindTransitionChip(string stableId)
	{
		if (!_transitionChips.TryGetValue(stableId ?? string.Empty, out var value))
		{
			return null;
		}
		return value;
	}

	public bool HasVisualConnection(string sourceStableId, string targetStableId)
	{
		string text = sourceStableId ?? string.Empty;
		string text2 = targetStableId ?? string.Empty;
		if (_visualConnections.Contains((text, text2)) && _visibleStateIds.Contains(text) && _visibleStateIds.Contains(text2) && _nodes.TryGetValue(text, out var value) && _nodes.TryGetValue(targetStableId ?? string.Empty, out var value2))
		{
			return IsNodeConnected(value.Name, 0, value2.Name, 0);
		}
		return false;
	}

	public bool IsStateVisible(string stableId)
	{
		return _visibleStateIds.Contains(stableId ?? string.Empty);
	}

	public bool IsStateCollapsed(string stableId)
	{
		StateMachineLayout stateMachineLayout = GraphController?.Layout;
		bool value = default(bool);
		return stateMachineLayout?.Collapsed != null && stateMachineLayout.Collapsed.TryGetValue(stableId ?? string.Empty, out value) && value;
	}

	public void Bind(StateMachineGraphController controller, IStateMachineUndoAdapter undoAdapter)
	{
		if (GraphController != null)
		{
			GraphController.MutationApplied -= OnControllerMutationApplied;
		}
		GraphController = controller;
		UndoAdapter = undoAdapter;
		GraphController?.BindUndoAdapter(undoAdapter);
		if (GraphController != null)
		{
			GraphController.MutationApplied += OnControllerMutationApplied;
		}
		RefreshGraph(preserveContext: false);
	}

	private void OnControllerMutationApplied(StateMachineGraphChange change)
	{
		if (change != null)
		{
			if ((change.Kind & StateMachineGraphChangeKind.Definition) != StateMachineGraphChangeKind.None)
			{
				RefreshGraph();
			}
			else if ((change.Kind & StateMachineGraphChangeKind.Layout) != StateMachineGraphChangeKind.None)
			{
				RefreshLayoutOnly();
			}
			if ((change.Kind & StateMachineGraphChangeKind.Definition) != StateMachineGraphChangeKind.None)
			{
				this.DefinitionChanged?.Invoke();
			}
			if ((change.Kind & StateMachineGraphChangeKind.Layout) != StateMachineGraphChangeKind.None)
			{
				this.LayoutChanged?.Invoke();
			}
		}
	}

	private void RefreshLayoutOnly()
	{
		if (!IsInsideTree() || GraphController?.ViewModel == null)
		{
			return;
		}
		_refreshing = true;
		try
		{
			IncrementalGraphRefreshCount++;
			for (int i = 0; i < GraphController.ViewModel.Nodes.Count; i++)
			{
				StateMachineNodeViewModel stateMachineNodeViewModel = GraphController.ViewModel.Nodes[i];
				if (stateMachineNodeViewModel != null && _nodes.TryGetValue(stateMachineNodeViewModel.StableId, out var value))
				{
					Vector2 vector = ResolvePosition(stateMachineNodeViewModel.StableId, i);
					if (!value.PositionOffset.IsEqualApprox(vector))
					{
						value.PositionOffset = vector;
					}
				}
			}
			SynchronizeHierarchyPresentation(GraphController.ViewModel);
			SynchronizeGraphConnections(GraphController.ViewModel);
			SynchronizeTransitionChipVisibility();
			if (_layoutMutationMovedStateIds.Count > 0)
			{
				UpdateTransitionChipPositionsForStates(_layoutMutationMovedStateIds, "RefreshLayoutOnly");
			}
			else
			{
				UpdateAllTransitionChipPositions("RefreshLayoutOnly");
			}
			ApplyTransitionChipRuntimeState(_simulation?.CurrentSnapshot);
		}
		finally
		{
			_refreshing = false;
		}
	}

	public void LoadDefinition(StateMachineDefinition definition, StateMachineLayout layout, bool readOnly = false)
	{
		_details?.FlushPendingNumericCommit();
		FlushViewportState();
		GraphController?.SetReadOnly(readOnly);
		GraphController?.LoadDefinition(definition, layout);
		RefreshStateAuthorTypes();
		_viewportDirty = false;
		_detailsContext = DetailsContextKind.Definition;
		_detailsStableId = string.Empty;
		RefreshGraph(preserveContext: false);
	}

	public void RefreshGraph()
	{
		RefreshGraph(preserveContext: true);
	}

	private void RefreshGraph(bool preserveContext)
	{
		if (!IsInsideTree())
		{
			return;
		}
		StateMachineGraphViewModel stateMachineGraphViewModel = GraphController?.ViewModel;
		StateMachineDefinition stateMachineDefinition = stateMachineGraphViewModel?.StateMachineDefinition;
		HashSet<string> selectedStableIds = CaptureSelectedStableIds();
		bool flag = preserveContext && stateMachineDefinition != null && _renderedDefinition == stateMachineDefinition;
		Vector2 scrollOffset = (flag ? base.ScrollOffset : (stateMachineGraphViewModel?.StateMachineLayout?.ScrollOffset ?? Vector2.Zero));
		float value = (flag ? base.Zoom : (stateMachineGraphViewModel?.StateMachineLayout?.Zoom ?? 1f));
		_refreshing = true;
		try
		{
			ConfigureMutationControls(stateMachineGraphViewModel);
			if (stateMachineGraphViewModel?.StateMachineDefinition == null)
			{
				RebuildGraphNodes(null);
				SynchronizeTransitionChips(null);
				_renderedDefinition = null;
				_detailsContext = DetailsContextKind.Definition;
				_detailsStableId = string.Empty;
				ApplyTransitionChipSelection(string.Empty);
				ConfigureEmptyState(missingDefinition: true, emptyDefinition: false, readOnly: true);
				_details?.ShowDefinition(null, isReadOnly: true);
				_simulation?.BindDefinition(null);
				return;
			}
			ConfigureEmptyState(missingDefinition: false, stateMachineGraphViewModel.Nodes.Count == 0, !stateMachineGraphViewModel.CanMutate);
			_renderedDefinition = stateMachineGraphViewModel.StateMachineDefinition;
			if (flag)
			{
				IncrementalGraphRefreshCount++;
				SynchronizeGraphNodes(stateMachineGraphViewModel);
			}
			else
			{
				RebuildGraphNodes(stateMachineGraphViewModel);
			}
			SynchronizeHierarchyPresentation(stateMachineGraphViewModel);
			SynchronizeTransitionChips(stateMachineGraphViewModel);
			SynchronizeGraphConnections(stateMachineGraphViewModel);
			base.Zoom = Mathf.Clamp(value, 0.25f, 2f);
			base.ScrollOffset = scrollOffset;
			_pendingScrollOffset = base.ScrollOffset;
			_pendingZoom = base.Zoom;
			QueueTransitionChipLayoutAfterGraphSettles("RefreshGraph");
			_details?.SetStateChoices(stateMachineGraphViewModel.Nodes);
			RestoreDetailsContext(stateMachineGraphViewModel);
			RestoreSelectedStableIds(selectedStableIds);
			_diagnostics?.ShowDiagnostics(stateMachineGraphViewModel.Diagnostics);
			if (stateMachineGraphViewModel.HasCompositionFailure && _diagnostics != null)
			{
				_diagnostics.Visible = true;
			}
			_simulation?.BindDefinition(stateMachineGraphViewModel.StateMachineDefinition);
		}
		finally
		{
			_refreshing = false;
		}
	}

	private HashSet<string> CaptureSelectedStableIds()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			if (value.Selected && !string.IsNullOrWhiteSpace(value.StableId))
			{
				hashSet.Add(value.StableId);
			}
		}
		return hashSet;
	}

	private void RestoreSelectedStableIds(HashSet<string> selectedStableIds)
	{
		if (selectedStableIds == null || selectedStableIds.Count == 0)
		{
			return;
		}
		foreach (string selectedStableId in selectedStableIds)
		{
			if (_visibleStateIds.Contains(selectedStableId) && _nodes.TryGetValue(selectedStableId, out var value))
			{
				value.Selected = true;
			}
		}
	}

	private void RebuildGraphNodes(StateMachineGraphViewModel viewModel)
	{
		FullGraphRebuildCount++;
		ClearConnections();
		_visualConnections.Clear();
		_visibleStateIds.Clear();
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			RemoveChild(value);
			value.QueueFree();
		}
		_nodes.Clear();
		if (viewModel == null)
		{
			return;
		}
		for (int i = 0; i < viewModel.Nodes.Count; i++)
		{
			StateMachineNodeViewModel stateMachineNodeViewModel = viewModel.Nodes[i];
			if (stateMachineNodeViewModel?.State != null && !string.IsNullOrWhiteSpace(stateMachineNodeViewModel.StableId))
			{
				StateMachineGraphNode stateMachineGraphNode = CreateGraphNode(stateMachineNodeViewModel);
				stateMachineGraphNode.PositionOffset = ResolvePosition(stateMachineNodeViewModel.StableId, i);
				_nodes[stateMachineNodeViewModel.StableId] = stateMachineGraphNode;
			}
		}
	}

	private void SynchronizeGraphNodes(StateMachineGraphViewModel viewModel)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < viewModel.Nodes.Count; i++)
		{
			StateMachineNodeViewModel stateMachineNodeViewModel = viewModel.Nodes[i];
			if (stateMachineNodeViewModel?.State != null && !string.IsNullOrWhiteSpace(stateMachineNodeViewModel.StableId))
			{
				hashSet.Add(stateMachineNodeViewModel.StableId);
				if (!_nodes.TryGetValue(stateMachineNodeViewModel.StableId, out var value))
				{
					value = CreateGraphNode(stateMachineNodeViewModel);
					_nodes[stateMachineNodeViewModel.StableId] = value;
				}
				else
				{
					value.Bind(stateMachineNodeViewModel.State, stateMachineNodeViewModel.IsInherited, stateMachineNodeViewModel.IsReadOnly);
				}
				Vector2 vector = ResolvePosition(stateMachineNodeViewModel.StableId, i);
				if (!value.PositionOffset.IsEqualApprox(vector))
				{
					value.PositionOffset = vector;
				}
			}
		}
		List<string> list = new List<string>();
		foreach (string key in _nodes.Keys)
		{
			if (!hashSet.Contains(key))
			{
				list.Add(key);
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			string text = list[j];
			RemoveVisualConnectionsForNode(text);
			StateMachineGraphNode stateMachineGraphNode = _nodes[text];
			_nodes.Remove(text);
			RemoveChild(stateMachineGraphNode);
			stateMachineGraphNode.QueueFree();
		}
	}

	private StateMachineGraphNode CreateGraphNode(StateMachineNodeViewModel item)
	{
		StateMachineGraphNode stateMachineGraphNode = new StateMachineGraphNode();
		AddChild(stateMachineGraphNode, forceReadableName: false, InternalMode.Disabled);
		stateMachineGraphNode.Bind(item.State, item.IsInherited, item.IsReadOnly);
		stateMachineGraphNode.InspectRequested += delegate(StateMachineGraphNode selected)
		{
			ShowStateDetails(selected.StableId);
		};
		stateMachineGraphNode.InitialStateRequested += delegate(StateMachineGraphNode selected)
		{
			SetAsInitialState(selected.State);
		};
		stateMachineGraphNode.CollapseRequested += OnNodeCollapseRequested;
		GraphNodeCreateCount++;
		return stateMachineGraphNode;
	}

	private void SynchronizeHierarchyPresentation(StateMachineGraphViewModel viewModel)
	{
		_visibleStateIds.Clear();
		_parentStateIds.Clear();
		if (viewModel == null)
		{
			return;
		}
		System.Collections.Generic.Dictionary<string, StateMachineStateDefinition> statesById = new System.Collections.Generic.Dictionary<string, StateMachineStateDefinition>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, StateMachineNodeViewModel> dictionary = new System.Collections.Generic.Dictionary<string, StateMachineNodeViewModel>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> dictionary2 = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < viewModel.Nodes.Count; i++)
		{
			StateMachineNodeViewModel stateMachineNodeViewModel = viewModel.Nodes[i];
			StateMachineStateDefinition stateMachineStateDefinition = stateMachineNodeViewModel?.State;
			if (stateMachineStateDefinition != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId))
			{
				statesById[stateMachineStateDefinition.StableId] = stateMachineStateDefinition;
				dictionary[stateMachineStateDefinition.StableId] = stateMachineNodeViewModel;
				_parentStateIds[stateMachineStateDefinition.StableId] = stateMachineStateDefinition.ParentId ?? string.Empty;
				if (!string.IsNullOrWhiteSpace(stateMachineStateDefinition.ParentId))
				{
					dictionary2.TryGetValue(stateMachineStateDefinition.ParentId, out var value);
					dictionary2[stateMachineStateDefinition.ParentId] = value + 1;
				}
				if (!string.IsNullOrWhiteSpace(stateMachineStateDefinition.InitialChildId))
				{
					hashSet.Add(stateMachineStateDefinition.InitialChildId);
				}
			}
		}
		HashSet<string> collapsedIds = new HashSet<string>(StringComparer.Ordinal);
		StateMachineLayout stateMachineLayout = viewModel.StateMachineLayout;
		if (stateMachineLayout?.Collapsed != null)
		{
			StateMachineStateDefinition value2 = default(StateMachineStateDefinition);
			foreach (KeyValuePair<string, bool> item in stateMachineLayout.Collapsed)
			{
				bool flag = !item.Value || !statesById.TryGetValue(item.Key, out value2) || !dictionary2.ContainsKey(item.Key);
				if (!flag)
				{
					StateMachineStateKind kind = value2.Kind;
					bool flag2 = (uint)(kind - 1) <= 1u;
					flag = !flag2;
				}
				if (!flag)
				{
					collapsedIds.Add(item.Key);
				}
			}
		}
		System.Collections.Generic.Dictionary<string, bool> visibility = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal);
		HashSet<string> resolving = new HashSet<string>(StringComparer.Ordinal);
		for (int j = 0; j < viewModel.Nodes.Count; j++)
		{
			StateMachineNodeViewModel stateMachineNodeViewModel2 = viewModel.Nodes[j];
			if (stateMachineNodeViewModel2?.State != null && !string.IsNullOrWhiteSpace(stateMachineNodeViewModel2.StableId) && _nodes.TryGetValue(stateMachineNodeViewModel2.StableId, out var value3))
			{
				StateMachineStateDefinition state = stateMachineNodeViewModel2.State;
				dictionary2.TryGetValue(stateMachineNodeViewModel2.StableId, out var value4);
				StateMachineStateDefinition value5;
				string parentDisplayName = (statesById.TryGetValue(state.ParentId ?? string.Empty, out value5) ? value5.DisplayName.ToString() : string.Empty);
				StateMachineNodeViewModel value6;
				bool canSetAsInitial = value5 != null && value5.Kind == StateMachineStateKind.Compound && !stateMachineNodeViewModel2.IsReadOnly && dictionary.TryGetValue(state.ParentId ?? string.Empty, out value6) && !value6.IsReadOnly;
				value3.BindHierarchy(state.ParentId, parentDisplayName, value4, hashSet.Contains(stateMachineNodeViewModel2.StableId), string.Equals(stateMachineNodeViewModel2.StableId, viewModel.EffectiveDefinition?.RootStateId ?? viewModel.StateMachineDefinition?.RootStateId, StringComparison.Ordinal), canSetAsInitial, collapsedIds.Contains(stateMachineNodeViewModel2.StableId));
				bool flag3 = ResolveVisible(stateMachineNodeViewModel2.StableId);
				if (!flag3)
				{
					value3.Selected = false;
				}
				value3.Visible = flag3;
				if (flag3)
				{
					_visibleStateIds.Add(stateMachineNodeViewModel2.StableId);
				}
			}
		}
		EnsureDetailsContextVisible();
		ApplySimulationSnapshot(_simulation?.CurrentSnapshot);
		bool ResolveVisible(string stableId)
		{
			if (visibility.TryGetValue(stableId, out var value7))
			{
				return value7;
			}
			if (!statesById.TryGetValue(stableId, out var value8))
			{
				return true;
			}
			if (!resolving.Add(stableId))
			{
				return true;
			}
			string text = value8.ParentId ?? string.Empty;
			bool flag4 = string.IsNullOrWhiteSpace(text) || !statesById.ContainsKey(text) || (!collapsedIds.Contains(text) && ResolveVisible(text));
			resolving.Remove(stableId);
			visibility[stableId] = flag4;
			return flag4;
		}
	}

	private void EnsureDetailsContextVisible()
	{
		if (_detailsContext == DetailsContextKind.State && !_visibleStateIds.Contains(_detailsStableId))
		{
			string text = ResolveVisibleHierarchyProjection(_detailsStableId);
			if (!string.IsNullOrWhiteSpace(text) && !string.Equals(text, _detailsStableId, StringComparison.Ordinal))
			{
				ShowStateDetails(text, selectNode: true);
			}
		}
		else
		{
			if (_detailsContext != DetailsContextKind.Transition)
			{
				return;
			}
			StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = FindTransition(_detailsStableId);
			string text2 = stateMachineGraphTransitionViewModel?.Transition?.SourceStateId ?? string.Empty;
			string text3 = stateMachineGraphTransitionViewModel?.Transition?.TargetStateId ?? string.Empty;
			if (!_visibleStateIds.Contains(text2) || !_visibleStateIds.Contains(text3))
			{
				string text4 = ResolveVisibleHierarchyProjection(text2);
				if (!_visibleStateIds.Contains(text4))
				{
					text4 = ResolveVisibleHierarchyProjection(text3);
				}
				if (_visibleStateIds.Contains(text4))
				{
					ShowStateDetails(text4, selectNode: true);
				}
			}
		}
	}

	private void SynchronizeGraphConnections(StateMachineGraphViewModel viewModel)
	{
		HashSet<(string, string)> hashSet = new HashSet<(string, string)>();
		foreach (StateMachineGraphTransitionViewModel transition in viewModel.Transitions)
		{
			StateMachineTransitionDefinition obj = transition?.Transition;
			string text = obj?.SourceStateId ?? string.Empty;
			string text2 = obj?.TargetStateId ?? string.Empty;
			if (_visibleStateIds.Contains(text) && _visibleStateIds.Contains(text2) && _nodes.ContainsKey(text) && _nodes.ContainsKey(text2))
			{
				hashSet.Add((text, text2));
			}
		}
		List<(string, string)> list = new List<(string, string)>();
		foreach (var visualConnection in _visualConnections)
		{
			if (!hashSet.Contains(visualConnection))
			{
				list.Add(visualConnection);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			RemoveVisualConnection(list[i]);
		}
		foreach (var item in hashSet)
		{
			StateMachineGraphNode stateMachineGraphNode = _nodes[item.Item1];
			StateMachineGraphNode stateMachineGraphNode2 = _nodes[item.Item2];
			if (!IsNodeConnected(stateMachineGraphNode.Name, 0, stateMachineGraphNode2.Name, 0))
			{
				ConnectNode(stateMachineGraphNode.Name, 0, stateMachineGraphNode2.Name, 0);
			}
			_visualConnections.Add(item);
		}
	}

	private void SynchronizeTransitionChips(StateMachineGraphViewModel viewModel)
	{
		TransitionChipSynchronizeCount++;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<(string, string), int> dictionary = new System.Collections.Generic.Dictionary<(string, string), int>();
		if (viewModel != null)
		{
			foreach (StateMachineGraphTransitionViewModel transition in viewModel.Transitions)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition = transition?.Transition;
				if (stateMachineTransitionDefinition != null && !string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.StableId))
				{
					(string, string) key = (stateMachineTransitionDefinition.SourceStateId ?? string.Empty, stateMachineTransitionDefinition.TargetStateId ?? string.Empty);
					dictionary.TryGetValue(key, out var value);
					dictionary[key] = value + 1;
				}
			}
		}
		_transitionChipGroups.Clear();
		_transitionChipsByState.Clear();
		if (viewModel != null)
		{
			foreach (StateMachineGraphTransitionViewModel transition2 in viewModel.Transitions)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition2 = transition2?.Transition;
				if (stateMachineTransitionDefinition2 != null && !string.IsNullOrWhiteSpace(stateMachineTransitionDefinition2.StableId))
				{
					hashSet.Add(stateMachineTransitionDefinition2.StableId);
					if (!_transitionChips.TryGetValue(stateMachineTransitionDefinition2.StableId, out var value2))
					{
						value2 = new StateMachineTransitionChip();
						value2.Bind(transition2, ResolveStateDisplayName(stateMachineTransitionDefinition2.SourceStateId), ResolveStateDisplayName(stateMachineTransitionDefinition2.TargetStateId), dictionary[(stateMachineTransitionDefinition2.SourceStateId ?? string.Empty, stateMachineTransitionDefinition2.TargetStateId ?? string.Empty)] > 1);
						value2.InspectRequested += OnTransitionChipInspectRequested;
						AddChild(value2, forceReadableName: false, InternalMode.Disabled);
						_transitionChips[stateMachineTransitionDefinition2.StableId] = value2;
						TransitionChipCreateCount++;
					}
					else
					{
						value2.Bind(transition2, ResolveStateDisplayName(stateMachineTransitionDefinition2.SourceStateId), ResolveStateDisplayName(stateMachineTransitionDefinition2.TargetStateId), dictionary[(stateMachineTransitionDefinition2.SourceStateId ?? string.Empty, stateMachineTransitionDefinition2.TargetStateId ?? string.Empty)] > 1);
					}
					(string, string) key2 = (value2.SourceStateId, value2.TargetStateId);
					if (!_transitionChipGroups.TryGetValue(key2, out var value3))
					{
						value3 = new List<StateMachineTransitionChip>();
						_transitionChipGroups[key2] = value3;
					}
					value3.Add(value2);
					AddTransitionChipStateIndex(value2.SourceStateId, value2);
					if (!string.Equals(value2.SourceStateId, value2.TargetStateId, StringComparison.Ordinal))
					{
						AddTransitionChipStateIndex(value2.TargetStateId, value2);
					}
				}
			}
		}
		List<string> list = new List<string>();
		foreach (string key3 in _transitionChips.Keys)
		{
			if (!hashSet.Contains(key3))
			{
				list.Add(key3);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			string text = list[i];
			StateMachineTransitionChip stateMachineTransitionChip = _transitionChips[text];
			_transitionChips.Remove(text);
			_activePendingTransitionChipIds.Remove(text);
			_nextPendingTransitionChipIds.Remove(text);
			if (string.Equals(_lastCompletedTransitionChipId, text, StringComparison.Ordinal))
			{
				_lastCompletedTransitionChipId = string.Empty;
			}
			stateMachineTransitionChip.InspectRequested -= OnTransitionChipInspectRequested;
			RemoveChild(stateMachineTransitionChip);
			stateMachineTransitionChip.QueueFree();
		}
		foreach (List<StateMachineTransitionChip> value4 in _transitionChipGroups.Values)
		{
			value4.Sort(CompareTransitionChips);
		}
		SynchronizeTransitionChipVisibility();
		ApplyTransitionChipSelection(_detailsStableId);
		ApplyTransitionChipRuntimeState(_simulation?.CurrentSnapshot);
		QueueTransitionChipLayoutAfterGraphSettles("SynchronizeTransitionChips");
	}

	private void AddTransitionChipStateIndex(string stableId, StateMachineTransitionChip chip)
	{
		if (!string.IsNullOrWhiteSpace(stableId))
		{
			if (!_transitionChipsByState.TryGetValue(stableId, out var value))
			{
				value = new List<StateMachineTransitionChip>();
				_transitionChipsByState[stableId] = value;
			}
			value.Add(chip);
		}
	}

	private void SynchronizeTransitionChipVisibility()
	{
		foreach (StateMachineTransitionChip value in _transitionChips.Values)
		{
			value.Visible = _visibleStateIds.Contains(value.SourceStateId) && _visibleStateIds.Contains(value.TargetStateId) && _nodes.ContainsKey(value.SourceStateId) && _nodes.ContainsKey(value.TargetStateId);
		}
	}

	private void UpdateAllTransitionChipPositions([CallerMemberName] string reason = "")
	{
		LastTransitionChipRelayoutReason = reason ?? string.Empty;
		LastTransitionChipRelayoutCount = 0;
		if (!IsWorkbenchActive)
		{
			LastTransitionChipRelayoutReason += ":hidden";
			return;
		}
		foreach (KeyValuePair<(string, string), List<StateMachineTransitionChip>> transitionChipGroup in _transitionChipGroups)
		{
			UpdateTransitionChipGroupPosition(transitionChipGroup.Key, transitionChipGroup.Value);
		}
		TransitionChipPositionUpdateCount += LastTransitionChipRelayoutCount;
	}

	private void QueueTransitionChipLayoutAfterGraphSettles([CallerMemberName] string reason = "")
	{
		_queuedTransitionChipLayoutReason = reason ?? string.Empty;
		if (!_transitionChipLayoutQueued && IsInsideTree())
		{
			_transitionChipLayoutQueued = true;
			CallDeferred("UpdateTransitionChipLayoutAfterGraphSettles");
		}
	}

	private void UpdateTransitionChipLayoutAfterGraphSettles()
	{
		_transitionChipLayoutQueued = false;
		if (GodotObject.IsInstanceValid(this) && IsInsideTree())
		{
			if (!IsWorkbenchActive)
			{
				_queuedTransitionChipLayoutReason = string.Empty;
				return;
			}
			SynchronizeTransitionChipVisibility();
			UpdateAllTransitionChipPositions(_queuedTransitionChipLayoutReason + ":deferred");
			_queuedTransitionChipLayoutReason = string.Empty;
		}
	}

	private void UpdateTransitionChipPositionsForStates(IEnumerable<string> stableIds, [CallerMemberName] string reason = "")
	{
		LastTransitionChipRelayoutReason = (reason ?? string.Empty) + ":adjacent";
		LastTransitionChipRelayoutCount = 0;
		if (!IsWorkbenchActive)
		{
			LastTransitionChipRelayoutReason += ":hidden";
			return;
		}
		_transitionGroupsNeedingLayout.Clear();
		foreach (string stableId in stableIds)
		{
			if (_transitionChipsByState.TryGetValue(stableId ?? string.Empty, out var value))
			{
				for (int i = 0; i < value.Count; i++)
				{
					StateMachineTransitionChip stateMachineTransitionChip = value[i];
					_transitionGroupsNeedingLayout.Add((stateMachineTransitionChip.SourceStateId, stateMachineTransitionChip.TargetStateId));
				}
			}
		}
		foreach (var item in _transitionGroupsNeedingLayout)
		{
			if (_transitionChipGroups.TryGetValue(item, out var value2))
			{
				UpdateTransitionChipGroupPosition(item, value2);
			}
		}
		TransitionChipPositionUpdateCount += LastTransitionChipRelayoutCount;
	}

	private void UpdateTransitionChipGroupPosition((string Source, string Target) pair, List<StateMachineTransitionChip> group)
	{
		if (group == null || group.Count == 0)
		{
			return;
		}
		if (!_visibleStateIds.Contains(pair.Source) || !_visibleStateIds.Contains(pair.Target) || !_nodes.TryGetValue(pair.Source, out var value) || !_nodes.TryGetValue(pair.Target, out var value2))
		{
			for (int i = 0; i < group.Count; i++)
			{
				group[i].Visible = false;
			}
			return;
		}
		Vector2 graphPosition;
		Vector2 vector;
		if (string.Equals(pair.Source, pair.Target, StringComparison.Ordinal))
		{
			graphPosition = value.PositionOffset + new Vector2(value.Size.X + 82f, value.Size.Y * 0.18f);
			vector = Vector2.Right;
		}
		else
		{
			GetConnectionEndpoints(value, value2, out var from, out var to);
			GetConnectionCurveControls(from, to, base.ConnectionLinesCurvature, out var controlFrom, out var controlTo);
			graphPosition = from.BezierInterpolate(controlFrom, controlTo, to, 0.52f);
			vector = CubicBezierTangent(from, controlFrom, controlTo, to, 0.52f);
		}
		Vector2 vector2 = ToLocalPosition(graphPosition);
		Vector2 vector3 = vector * Mathf.Max(base.Zoom, 0.001f);
		Vector2 vector4 = ((vector3.LengthSquared() > 0.001f) ? new Vector2(0f - vector3.Y, vector3.X).Normalized() : Vector2.Up);
		float num = ((string.CompareOrdinal(pair.Source, pair.Target) <= 0) ? 1f : (-1f));
		Vector2 vector5 = (string.Equals(pair.Source, pair.Target, StringComparison.Ordinal) ? Vector2.Zero : (vector4 * 17f * num));
		float num2 = (float)(group.Count - 1) * 0.5f;
		for (int j = 0; j < group.Count; j++)
		{
			StateMachineTransitionChip stateMachineTransitionChip = group[j];
			stateMachineTransitionChip.Visible = true;
			Vector2 vector6 = stateMachineTransitionChip.Size;
			if (vector6.X < 1f || vector6.Y < 1f)
			{
				vector6 = stateMachineTransitionChip.CustomMinimumSize;
			}
			Vector2 vector7 = vector4 * (((float)j - num2) * 32f);
			stateMachineTransitionChip.Position = (vector2 + vector5 + vector7 - vector6 * 0.5f).Round();
			LastTransitionChipRelayoutCount++;
		}
	}

	private void UpdateMovingTransitionChipPositions()
	{
		bool flag = false;
		for (int i = 0; i < _movingStateIds.Count; i++)
		{
			string key = _movingStateIds[i];
			if (_nodes.TryGetValue(key, out var value) && (!_movingStatePositions.TryGetValue(key, out var value2) || !value2.IsEqualApprox(value.PositionOffset)))
			{
				_movingStatePositions[key] = value.PositionOffset;
				flag = true;
			}
		}
		if (flag)
		{
			UpdateTransitionChipPositionsForStates(_movingStateIds, "UpdateMovingTransitionChipPositions");
		}
	}

	private void OnTransitionChipInspectRequested(string stableId)
	{
		ShowTransitionDetails(stableId, openDetails: true);
	}

	private string ResolveStateDisplayName(string stableId)
	{
		if (_nodes.TryGetValue(stableId ?? string.Empty, out var value) && value.State != null && !value.State.DisplayName.IsEmpty)
		{
			return value.State.DisplayName.ToString();
		}
		return stableId ?? string.Empty;
	}

	private static int CompareTransitionChips(StateMachineTransitionChip left, StateMachineTransitionChip right)
	{
		int num = right.Priority.CompareTo(left.Priority);
		if (num != 0)
		{
			return num;
		}
		int num2 = left.DeclarationOrder.CompareTo(right.DeclarationOrder);
		if (num2 == 0)
		{
			return string.CompareOrdinal(left.StableId, right.StableId);
		}
		return num2;
	}

	private Vector2 ToLocalPosition(Vector2 graphPosition)
	{
		return (graphPosition - base.ScrollOffset) * Mathf.Max(base.Zoom, 0.001f);
	}

	private static void GetConnectionEndpoints(StateMachineGraphNode source, StateMachineGraphNode target, out Vector2 from, out Vector2 to)
	{
		from = source.PositionOffset + ((source.GetOutputPortCount() > 0) ? source.GetOutputPortPosition(0) : GetSlotEdgePosition(source, output: true));
		to = target.PositionOffset + ((target.GetInputPortCount() > 0) ? target.GetInputPortPosition(0) : GetSlotEdgePosition(target, output: false));
	}

	private static Vector2 GetSlotEdgePosition(StateMachineGraphNode node, bool output)
	{
		Control control = node?.GetNodeOrNull<Control>("PuzzleTile");
		float y = ((GodotObject.IsInstanceValid(control) && control.Size.Y > 0f) ? (control.Position.Y + control.Size.Y * 0.5f) : (node.Size.Y * 0.5f));
		return new Vector2(output ? node.Size.X : 0f, y);
	}

	private static void GetConnectionCurveControls(Vector2 from, Vector2 to, float curvature, out Vector2 controlFrom, out Vector2 controlTo)
	{
		float num = Mathf.Abs(to.X - from.X);
		float num2 = Mathf.Max(24f, num * Mathf.Clamp(curvature, 0f, 1f));
		if (to.X < from.X)
		{
			num2 = Mathf.Max(num2, from.DistanceTo(to) * 0.5f);
		}
		controlFrom = from + Vector2.Right * num2;
		controlTo = to - Vector2.Right * num2;
	}

	private static Vector2 CubicBezierTangent(Vector2 from, Vector2 controlFrom, Vector2 controlTo, Vector2 to, float amount)
	{
		float num = 1f - amount;
		return 3f * num * num * (controlFrom - from) + 6f * num * amount * (controlTo - controlFrom) + 3f * amount * amount * (to - controlTo);
	}

	private void RemoveVisualConnectionsForNode(string stableId)
	{
		List<(string, string)> list = new List<(string, string)>();
		foreach (var visualConnection in _visualConnections)
		{
			if (string.Equals(visualConnection.Source, stableId, StringComparison.Ordinal) || string.Equals(visualConnection.Target, stableId, StringComparison.Ordinal))
			{
				list.Add(visualConnection);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			RemoveVisualConnection(list[i]);
		}
	}

	private void RemoveVisualConnection((string Source, string Target) pair)
	{
		if (_nodes.TryGetValue(pair.Source, out var value) && _nodes.TryGetValue(pair.Target, out var value2) && IsNodeConnected(value.Name, 0, value2.Name, 0))
		{
			DisconnectNode(value.Name, 0, value2.Name, 0);
		}
		_visualConnections.Remove(pair);
	}

	public void NavigateToStableId(string stableId)
	{
		if (_nodes.TryGetValue(stableId ?? string.Empty, out var value))
		{
			ShowStateDetails(stableId, selectNode: true);
			base.ScrollOffset = value.PositionOffset - base.Size * 0.5f / Mathf.Max(base.Zoom, 0.001f) + value.Size * 0.5f;
			return;
		}
		StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = FindTransition(stableId);
		if (stateMachineGraphTransitionViewModel != null)
		{
			ShowTransitionDetails(stateMachineGraphTransitionViewModel.StableId, openDetails: true);
		}
	}

	public void ApplyPickedResource(Resource owner, string propertyName, Resource pickedResource)
	{
		ApplyFieldChange(owner, new StringName(propertyName ?? string.Empty), Variant.From(in pickedResource));
	}

	public Array<StringName> GetSelectedStateNodes()
	{
		Array<StringName> selectedNodes = GetSelectedNodes();
		Array<StringName> array = new Array<StringName>();
		foreach (StringName item in selectedNodes)
		{
			StateMachineGraphNode stateMachineGraphNode = FindNodeByGraphName(item);
			if (stateMachineGraphNode != null)
			{
				array.Add(stateMachineGraphNode.StableId);
			}
		}
		return array;
	}

	public Array<StringName> GetSelectedNodes()
	{
		Array<StringName> array = new Array<StringName>();
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			if (value.Visible && value.Selected)
			{
				array.Add(value.Name);
			}
		}
		return array;
	}

	private void BuildGameToolbar()
	{
		HBoxContainer hBoxContainer = Call("get_menu_hbox").As<HBoxContainer>();
		if (hBoxContainer == null)
		{
			return;
		}
		_compactToolbar = new HBoxContainer
		{
			Name = "StateMachineCompactToolbar",
			SizeFlagsHorizontal = SizeFlags.ShrinkBegin
		};
		hBoxContainer.AddChild(_compactToolbar, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.MoveChild(_compactToolbar, 0);
		_compactToolbar.AddChild(new Label
		{
			Name = "StateMachineToolbarTitle",
			Text = "  状态机"
		}, forceReadableName: false, InternalMode.Disabled);
		_newStateKind = new OptionButton
		{
			Name = "NewStateKind",
			TooltipText = "选择新拼图种类",
			CustomMinimumSize = new Vector2(92f, 0f)
		};
		_newStateKind.AddItem("◆ 原子", 0);
		_newStateKind.AddItem("▣ 复合", 1);
		_newStateKind.AddItem("▦ 并行", 2);
		_newStateKind.AddItem("◉ 历史", 3);
		_compactToolbar.AddChild(_newStateKind, forceReadableName: false, InternalMode.Disabled);
		_newStateType = new OptionButton
		{
			Name = "NewStateTypeOption",
			TooltipText = "选择状态拼图的 C# Resource 类型",
			CustomMinimumSize = new Vector2(136f, 0f)
		};
		_newStateType.ItemSelected += delegate
		{
			RefreshCompactStateAuthorTypes();
		};
		_compactToolbar.AddChild(_newStateType, forceReadableName: false, InternalMode.Disabled);
		RefreshStateAuthorTypes();
		_compactStateTypeMenu = new MenuButton
		{
			Name = "CompactStateTypeMenu",
			Text = "♯",
			TooltipText = "选择状态拼图的 C# Resource 类型",
			Visible = false
		};
		_compactStateTypeMenu.GetPopup().IdPressed += delegate(long id)
		{
			SelectStateAuthorTypeIndex((int)id);
		};
		_compactToolbar.AddChild(_compactStateTypeMenu, forceReadableName: false, InternalMode.Disabled);
		RefreshCompactStateAuthorTypes();
		_addStateButton = new Button
		{
			Name = "AddStatePuzzleButton",
			Text = "+",
			TooltipText = "在画布中心放置状态拼图"
		};
		_addStateButton.Pressed += AddStateAtViewportCenter;
		_compactToolbar.AddChild(_addStateButton, forceReadableName: false, InternalMode.Disabled);
		_narrowAddStateMenu = new MenuButton
		{
			Name = "NarrowAddStateMenu",
			Text = "＋◆",
			TooltipText = "选择并新增状态拼图",
			Visible = false
		};
		PopupMenu popup = _narrowAddStateMenu.GetPopup();
		AddStateKindMenuItem(popup, StateMachineStateKind.Atomic, "新增原子状态", "res://addons/godot_state_charts/atomic_state.svg");
		AddStateKindMenuItem(popup, StateMachineStateKind.Compound, "新增复合状态", "res://addons/godot_state_charts/compound_state.svg");
		AddStateKindMenuItem(popup, StateMachineStateKind.Parallel, "新增并行状态", "res://addons/godot_state_charts/parallel_state.svg");
		AddStateKindMenuItem(popup, StateMachineStateKind.History, "新增历史状态", "res://addons/godot_state_charts/history_state.svg");
		popup.IdPressed += delegate(long id)
		{
			AddStateAtViewportCenter((StateMachineStateKind)id);
		};
		_compactToolbar.AddChild(_narrowAddStateMenu, forceReadableName: false, InternalMode.Disabled);
		Button button = new Button
		{
			Name = "DefinitionOverviewButton",
			Text = "◎",
			TooltipText = "返回状态机总览与继承配置"
		};
		button.Pressed += ShowDefinitionOverview;
		_compactToolbar.AddChild(button, forceReadableName: false, InternalMode.Disabled);
		_detailsToggleButton = new Button
		{
			Name = "ToggleStateDetailsButton",
			Text = "◇",
			TooltipText = "显示或隐藏当前配置拼图"
		};
		_detailsToggleButton.Pressed += ToggleDetailsPanel;
		_compactToolbar.AddChild(_detailsToggleButton, forceReadableName: false, InternalMode.Disabled);
		Button button2 = new Button
		{
			Name = "UndoStateMachineButton",
			Text = "↶",
			TooltipText = "撤销"
		};
		button2.Pressed += delegate
		{
			GraphController?.Undo();
		};
		_compactToolbar.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
		Button button3 = new Button
		{
			Name = "RedoStateMachineButton",
			Text = "↷",
			TooltipText = "重做"
		};
		button3.Pressed += delegate
		{
			GraphController?.Redo();
		};
		_compactToolbar.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
		_simulationToggle = new Button
		{
			Name = "SimulationToggle",
			Text = "▶",
			TooltipText = "打开无副作用状态机沙盒",
			ToggleMode = true,
			ButtonPressed = false
		};
		_simulationToggle.Toggled += delegate(bool visible)
		{
			if (_simulation != null)
			{
				_simulation.Visible = visible && IsWorkbenchActive;
			}
		};
		_compactToolbar.AddChild(_simulationToggle, forceReadableName: false, InternalMode.Disabled);
		MenuButton menuButton = new MenuButton
		{
			Name = "StateMachineMoreMenu",
			Text = "⋯",
			TooltipText = "诊断、预览和面板"
		};
		PopupMenu popup2 = menuButton.GetPopup();
		popup2.AddItem("✓ 运行诊断", 1, Key.None);
		popup2.AddItem("▶ 编译预览", 2, Key.None);
		popup2.AddSeparator();
		popup2.AddItem("显示 / 隐藏配置拼图", 3, Key.None);
		popup2.AddItem("显示 / 隐藏诊断", 4, Key.None);
		popup2.IdPressed += OnToolbarActionPressed;
		_compactToolbar.AddChild(menuButton, forceReadableName: false, InternalMode.Disabled);
	}

	private void RefreshStateAuthorTypes()
	{
		if (!GodotObject.IsInstanceValid(_newStateType))
		{
			return;
		}
		string selectedStateAuthorTypeId = GetSelectedStateAuthorTypeId();
		_newStateType.Clear();
		Texture2D texture2D = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/atomic_state.svg", null, ResourceLoader.CacheMode.Reuse);
		_newStateType.AddIconItem(texture2D, "基础状态");
		_newStateType.SetItemMetadata(0, string.Empty);
		int idx = 0;
		IReadOnlyList<StateMachineAuthorTypeDescriptor> stateTypes = StateMachineAuthorTypeRegistry.GetStateTypes();
		for (int i = 0; i < stateTypes.Count; i++)
		{
			StateMachineAuthorTypeDescriptor stateMachineAuthorTypeDescriptor = stateTypes[i];
			Texture2D texture = ((!string.IsNullOrWhiteSpace(stateMachineAuthorTypeDescriptor.IconPath)) ? ResourceLoader.Load<Texture2D>(stateMachineAuthorTypeDescriptor.IconPath, null, ResourceLoader.CacheMode.Reuse) : texture2D);
			_newStateType.AddIconItem(texture, stateMachineAuthorTypeDescriptor.DisplayName);
			int num = _newStateType.ItemCount - 1;
			_newStateType.SetItemMetadata(num, stateMachineAuthorTypeDescriptor.TypeId);
			_newStateType.GetPopup().SetItemTooltip(num, string.IsNullOrWhiteSpace(stateMachineAuthorTypeDescriptor.Description) ? stateMachineAuthorTypeDescriptor.TypeId : stateMachineAuthorTypeDescriptor.Description);
			if (string.Equals(stateMachineAuthorTypeDescriptor.TypeId, selectedStateAuthorTypeId, StringComparison.Ordinal))
			{
				idx = num;
			}
		}
		_newStateType.Select(idx);
		RefreshCompactStateAuthorTypes();
	}

	private void OnAuthorTypesChanged()
	{
		RefreshStateAuthorTypes();
		ApplyResponsiveLayout();
		if (IsInsideTree() && GraphController?.ViewModel?.StateMachineDefinition != null)
		{
			RefreshGraph();
		}
	}

	private string GetSelectedStateAuthorTypeId()
	{
		if (!GodotObject.IsInstanceValid(_newStateType) || _newStateType.Selected < 0 || _newStateType.Selected >= _newStateType.ItemCount)
		{
			return string.Empty;
		}
		return _newStateType.GetItemMetadata(_newStateType.Selected).AsString();
	}

	private void RefreshCompactStateAuthorTypes()
	{
		if (GodotObject.IsInstanceValid(_compactStateTypeMenu) && GodotObject.IsInstanceValid(_newStateType))
		{
			PopupMenu popup = _compactStateTypeMenu.GetPopup();
			popup.Clear();
			for (int i = 0; i < _newStateType.ItemCount; i++)
			{
				popup.AddIconRadioCheckItem(_newStateType.GetItemIcon(i), _newStateType.GetItemText(i), i, Key.None);
				popup.SetItemChecked(i, i == _newStateType.Selected);
				popup.SetItemMetadata(i, _newStateType.GetItemMetadata(i));
				popup.SetItemTooltip(i, _newStateType.GetPopup().GetItemTooltip(i));
			}
			int num = Mathf.Max(0, _newStateType.Selected);
			_compactStateTypeMenu.TooltipText = ((num < _newStateType.ItemCount) ? ("C# 状态类型：" + _newStateType.GetItemText(num)) : "选择状态拼图的 C# Resource 类型");
		}
	}

	private void SelectStateAuthorTypeIndex(int index)
	{
		if (GodotObject.IsInstanceValid(_newStateType) && index >= 0 && index < _newStateType.ItemCount)
		{
			_newStateType.Select(index);
			_newStateType.EmitSignal(OptionButton.SignalName.ItemSelected, index);
			RefreshCompactStateAuthorTypes();
		}
	}

	private void ConfigureMutationControls(StateMachineGraphViewModel viewModel)
	{
		bool flag = viewModel?.CanMutate ?? false;
		if (GodotObject.IsInstanceValid(_newStateKind))
		{
			_newStateKind.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_newStateType))
		{
			_newStateType.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_compactStateTypeMenu))
		{
			_compactStateTypeMenu.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_addStateButton))
		{
			_addStateButton.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_narrowAddStateMenu))
		{
			_narrowAddStateMenu.Disabled = !flag;
		}
		if (GodotObject.IsInstanceValid(_simulationToggle))
		{
			_simulationToggle.Disabled = viewModel?.HasCompositionFailure ?? false;
			if (_simulationToggle.Disabled)
			{
				_simulationToggle.ButtonPressed = false;
			}
		}
		if (GodotObject.IsInstanceValid(_compositionFailureBanner))
		{
			_compositionFailureBanner.Visible = viewModel?.HasCompositionFailure ?? false;
		}
	}

	private void ShowCompositionFailureDiagnostics()
	{
		if (GodotObject.IsInstanceValid(_diagnostics))
		{
			_diagnostics.ShowDiagnostics(GraphController?.Diagnostics);
			_diagnostics.Visible = true;
		}
	}

	private static void AddStateKindMenuItem(PopupMenu popup, StateMachineStateKind kind, string label, string iconPath)
	{
		Texture2D texture = ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse);
		popup.AddIconItem(texture, label, (int)kind, Key.None);
	}

	private void BindZoomButtons()
	{
		HBoxContainer zoomHBox = GetZoomHBox();
		if (zoomHBox == null)
		{
			return;
		}
		foreach (Node child in zoomHBox.GetChildren())
		{
			if (child is BaseButton baseButton)
			{
				baseButton.Pressed += QueueViewportCapture;
			}
		}
	}

	private void ApplyResponsiveLayout()
	{
		bool flag = base.Size.X <= 620f;
		if (_newStateKind != null)
		{
			_newStateKind.Visible = !flag;
		}
		bool flag2 = !flag && base.Size.X > 920f;
		if (_newStateType != null)
		{
			_newStateType.Visible = flag2;
			_newStateType.CustomMinimumSize = new Vector2(136f, 0f);
		}
		if (_compactStateTypeMenu != null)
		{
			_compactStateTypeMenu.Visible = !flag2 && _newStateType != null && _newStateType.ItemCount > 1;
		}
		if (_addStateButton != null)
		{
			_addStateButton.Visible = !flag;
		}
		if (_narrowAddStateMenu != null)
		{
			_narrowAddStateMenu.Visible = flag;
		}
		Label label = _compactToolbar?.GetNodeOrNull<Label>("StateMachineToolbarTitle");
		if (label != null)
		{
			label.Visible = !flag;
		}
		if (_details != null)
		{
			if (flag)
			{
				_details.AnchorLeft = 0f;
				_details.AnchorTop = 0.38f;
				_details.AnchorRight = 1f;
				_details.AnchorBottom = 1f;
				_details.OffsetLeft = 12f;
				_details.OffsetTop = 0f;
				_details.OffsetRight = -12f;
				_details.OffsetBottom = -12f;
				_details.Visible = _narrowDetailsVisible;
			}
			else
			{
				_details.AnchorLeft = 1f;
				_details.AnchorTop = 0f;
				_details.AnchorRight = 1f;
				_details.AnchorBottom = 0.72f;
				_details.OffsetLeft = -310f;
				_details.OffsetTop = 48f;
				_details.OffsetRight = -10f;
				_details.OffsetBottom = -8f;
				_details.Visible = _detailsRequestedVisible;
			}
		}
		if (_diagnostics != null)
		{
			_diagnostics.OffsetRight = (flag ? (-12) : (-322));
		}
		QueueTransitionChipLayoutAfterGraphSettles("ApplyResponsiveLayout");
	}

	private void ToggleDetailsPanel()
	{
		if (base.Size.X <= 620f)
		{
			_narrowDetailsVisible = !_narrowDetailsVisible;
		}
		else
		{
			_detailsRequestedVisible = !_detailsRequestedVisible;
		}
		ApplyResponsiveLayout();
	}

	private void OpenDetailsForSelection()
	{
		if (!(base.Size.X > 620f) && !_narrowDetailsVisible)
		{
			_narrowDetailsVisible = true;
			ApplyResponsiveLayout();
		}
	}

	private void BuildLegacyToolbar()
	{
		HBoxContainer hBoxContainer = Call("get_menu_hbox").As<HBoxContainer>();
		if (hBoxContainer != null)
		{
			Label node = new Label
			{
				Text = "  状态拼图库"
			};
			hBoxContainer.AddChild(node, forceReadableName: false, InternalMode.Disabled);
			_newStateKind = new OptionButton
			{
				TooltipText = "选择新拼图种类"
			};
			_newStateKind.AddItem("◆ 原子", 0);
			_newStateKind.AddItem("▣ 复合", 1);
			_newStateKind.AddItem("▥ 并行", 2);
			_newStateKind.AddItem("◴ 历史", 3);
			hBoxContainer.AddChild(_newStateKind, forceReadableName: false, InternalMode.Disabled);
			Button button = new Button
			{
				Text = "+ 放置状态"
			};
			button.Pressed += AddStateAtViewportCenter;
			hBoxContainer.AddChild(button, forceReadableName: false, InternalMode.Disabled);
			Button button2 = new Button
			{
				Text = "↶",
				TooltipText = "撤销状态机操作"
			};
			button2.Pressed += delegate
			{
				GraphController?.Undo();
			};
			hBoxContainer.AddChild(button2, forceReadableName: false, InternalMode.Disabled);
			Button button3 = new Button
			{
				Text = "↷",
				TooltipText = "重做状态机操作"
			};
			button3.Pressed += delegate
			{
				GraphController?.Redo();
			};
			hBoxContainer.AddChild(button3, forceReadableName: false, InternalMode.Disabled);
			Button button4 = new Button
			{
				Text = "✓ 运行诊断"
			};
			button4.Pressed += RunDiagnostics;
			hBoxContainer.AddChild(button4, forceReadableName: false, InternalMode.Disabled);
			Button button5 = new Button
			{
				Text = "▶ 编译预览",
				TooltipText = "不启动关卡，先编译并检查状态机"
			};
			button5.Pressed += CompilePreview;
			hBoxContainer.AddChild(button5, forceReadableName: false, InternalMode.Disabled);
			Button button6 = new Button
			{
				Text = "▶ 沙盒",
				TooltipText = "可视模拟事件、自动与延迟转换；不启动角色或关卡",
				ToggleMode = true,
				ButtonPressed = true
			};
			button6.Toggled += delegate(bool visible)
			{
				_simulation.Visible = visible;
			};
			hBoxContainer.AddChild(button6, forceReadableName: false, InternalMode.Disabled);
			Button button7 = new Button
			{
				Text = "配置",
				ToggleMode = true,
				ButtonPressed = true
			};
			button7.Toggled += delegate(bool visible)
			{
				_details.Visible = visible;
			};
			hBoxContainer.AddChild(button7, forceReadableName: false, InternalMode.Disabled);
			Button button8 = new Button
			{
				Text = "诊断",
				ToggleMode = true,
				ButtonPressed = true
			};
			button8.Toggled += delegate(bool visible)
			{
				_diagnostics.Visible = visible;
			};
			hBoxContainer.AddChild(button8, forceReadableName: false, InternalMode.Disabled);
		}
	}

	private void BuildOverlayPanels()
	{
		_compositionFailureBanner = new PanelContainer
		{
			Name = "CompositionFailureBanner",
			LayoutMode = 1,
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 0f,
			OffsetLeft = 12f,
			OffsetTop = 44f,
			OffsetRight = -322f,
			OffsetBottom = 76f,
			MouseFilter = MouseFilterEnum.Ignore,
			ZIndex = 20,
			Visible = false
		};
		_compositionFailureBanner.AddChild(new Label
		{
			Text = "⚠ 继承合成失败：图结构和剪贴板已锁定，请在右侧更换或清除基定义。",
			HorizontalAlignment = HorizontalAlignment.Center,
			VerticalAlignment = VerticalAlignment.Center,
			ThemeTypeVariation = "HeaderSmall",
			Modulate = new Color(1f, 0.42f, 0.34f)
		}, forceReadableName: false, InternalMode.Disabled);
		AddChild(_compositionFailureBanner, forceReadableName: false, InternalMode.Disabled);
		_emptyStatePanel = new PanelContainer
		{
			Name = "StateMachineEmptyStatePanel",
			LayoutMode = 1,
			AnchorLeft = 0.5f,
			AnchorTop = 0.5f,
			AnchorRight = 0.5f,
			AnchorBottom = 0.5f,
			OffsetLeft = -190f,
			OffsetTop = -92f,
			OffsetRight = 190f,
			OffsetBottom = 92f,
			MouseFilter = MouseFilterEnum.Stop,
			ZIndex = 40,
			Visible = false
		};
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 20);
		marginContainer.AddThemeConstantOverride("margin_top", 16);
		marginContainer.AddThemeConstantOverride("margin_right", 20);
		marginContainer.AddThemeConstantOverride("margin_bottom", 16);
		_emptyStatePanel.AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "状态机拼图工作台",
			HorizontalAlignment = HorizontalAlignment.Center,
			ThemeTypeVariation = "HeaderSmall"
		}, forceReadableName: false, InternalMode.Disabled);
		_emptyStateHint = new Label
		{
			Text = "新建一个状态机，或打开已有 .tres / .res 资源",
			HorizontalAlignment = HorizontalAlignment.Center
		};
		vBoxContainer.AddChild(_emptyStateHint, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer
		{
			Alignment = BoxContainer.AlignmentMode.Center
		};
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		_createDefinitionButton = new Button
		{
			Name = "CreateDefinitionButton",
			Text = "+ 新建状态机"
		};
		_createDefinitionButton.Pressed += delegate
		{
			this.CreateDefinitionRequested?.Invoke();
		};
		hBoxContainer.AddChild(_createDefinitionButton, forceReadableName: false, InternalMode.Disabled);
		_openDefinitionButton = new Button
		{
			Name = "OpenDefinitionButton",
			Text = "打开状态机"
		};
		_openDefinitionButton.Pressed += delegate
		{
			this.OpenDefinitionRequested?.Invoke();
		};
		hBoxContainer.AddChild(_openDefinitionButton, forceReadableName: false, InternalMode.Disabled);
		_initializeDefinitionButton = new Button
		{
			Name = "InitializeDefinitionButton",
			Text = "✦ 初始化入口与初始状态",
			Visible = false
		};
		_initializeDefinitionButton.Pressed += InitializeEmptyDefinition;
		hBoxContainer.AddChild(_initializeDefinitionButton, forceReadableName: false, InternalMode.Disabled);
		AddChild(_emptyStatePanel, forceReadableName: false, InternalMode.Disabled);
		_details = new StateMachineDetailsPanel
		{
			Name = "DetailsPanel",
			LayoutMode = 1,
			AnchorsPreset = 11,
			AnchorLeft = 1f,
			AnchorTop = 0f,
			AnchorRight = 1f,
			AnchorBottom = 0.72f,
			OffsetLeft = -310f,
			OffsetTop = 48f,
			OffsetRight = -10f,
			OffsetBottom = -8f,
			MouseFilter = MouseFilterEnum.Stop,
			ZIndex = 40
		};
		AddChild(_details, forceReadableName: false, InternalMode.Disabled);
		_details.FieldChanged += ApplyFieldChange;
		_details.ResourcePickerRequested += OnDetailsResourcePickerRequested;
		_details.GuardEditRequested += OnDetailsGuardEditRequested;
		_details.CallbackSourceOpenRequested += OnDetailsCallbackSourceOpenRequested;
		_details.AliasAddRequested += OnAliasAddRequested;
		_details.AliasRemoveRequested += OnAliasRemoveRequested;
		_details.NavigateRequested += NavigateToStableId;
		_details.TransitionCreateRequested += OnTransitionCreateRequested;
		_details.TransitionRemoveRequested += OnTransitionRemoveRequested;
		_details.StateOverrideCreateRequested += OnStateOverrideCreateRequested;
		_details.StateOverrideRestoreRequested += OnStateOverrideRestoreRequested;
		_details.TransitionOverrideCreateRequested += OnTransitionOverrideCreateRequested;
		_details.TransitionOverrideRestoreRequested += OnTransitionOverrideRestoreRequested;
		_diagnostics = new StateMachineDiagnosticsPanel
		{
			Name = "DiagnosticsPanel",
			LayoutMode = 1,
			AnchorLeft = 0f,
			AnchorTop = 1f,
			AnchorRight = 1f,
			AnchorBottom = 1f,
			OffsetLeft = 12f,
			OffsetTop = -166f,
			OffsetRight = -322f,
			OffsetBottom = -12f,
			MouseFilter = MouseFilterEnum.Stop,
			ZIndex = 40,
			Visible = false
		};
		AddChild(_diagnostics, forceReadableName: false, InternalMode.Disabled);
		_diagnostics.NavigateRequested += NavigateToStableId;
		_simulation = new StateMachineSimulationPanel
		{
			Name = "SimulationPanel",
			LayoutMode = 1,
			AnchorLeft = 0f,
			AnchorTop = 0f,
			AnchorRight = 0f,
			AnchorBottom = 0f,
			OffsetLeft = 12f,
			OffsetTop = 48f,
			OffsetRight = 372f,
			OffsetBottom = 304f,
			MouseFilter = MouseFilterEnum.Stop,
			ZIndex = 40,
			Visible = false
		};
		AddChild(_simulation, forceReadableName: false, InternalMode.Disabled);
		_simulation.StateChanged += ApplySimulationSnapshot;
		_simulation.NavigateRequested += NavigateToStableId;
	}

	private void ConfigureEmptyState(bool missingDefinition, bool emptyDefinition, bool readOnly)
	{
		if (!GodotObject.IsInstanceValid(_emptyStatePanel))
		{
			return;
		}
		bool flag = missingDefinition || emptyDefinition;
		_emptyStatePanel.Visible = flag;
		if (flag)
		{
			if (GodotObject.IsInstanceValid(_emptyStateHint))
			{
				_emptyStateHint.Text = (missingDefinition ? "新建一个状态机，或打开已有 .tres / .res 资源" : (readOnly ? "这个只读状态机还没有可显示的状态" : "这个状态机还没有入口；点击初始化即可开始拼图"));
			}
			if (GodotObject.IsInstanceValid(_createDefinitionButton))
			{
				_createDefinitionButton.Visible = missingDefinition;
			}
			if (GodotObject.IsInstanceValid(_openDefinitionButton))
			{
				_openDefinitionButton.Visible = missingDefinition;
			}
			if (GodotObject.IsInstanceValid(_initializeDefinitionButton))
			{
				_initializeDefinitionButton.Visible = emptyDefinition;
				_initializeDefinitionButton.Disabled = readOnly;
			}
		}
	}

	private void InitializeEmptyDefinition()
	{
		StateMachineGraphViewModel stateMachineGraphViewModel = GraphController?.ViewModel;
		if (stateMachineGraphViewModel?.StateMachineDefinition != null && stateMachineGraphViewModel.CanMutate && stateMachineGraphViewModel.Nodes.Count <= 0)
		{
			string addedStableId = string.Empty;
			Vector2 position = base.ScrollOffset + base.Size * 0.5f / Mathf.Max(base.Zoom, 0.001f);
			ExecuteMutation(delegate
			{
				addedStableId = GraphController.AddStateHierarchySafeAtPosition("状态机入口", StateMachineStateKind.Compound, position, string.Empty);
			});
			if (!string.IsNullOrWhiteSpace(addedStableId))
			{
				ShowStateDetails(addedStableId, selectNode: true);
			}
		}
	}

	private void DisconnectOverlaySignals()
	{
		if (GodotObject.IsInstanceValid(_details))
		{
			_details.FieldChanged -= ApplyFieldChange;
			_details.ResourcePickerRequested -= OnDetailsResourcePickerRequested;
			_details.GuardEditRequested -= OnDetailsGuardEditRequested;
			_details.CallbackSourceOpenRequested -= OnDetailsCallbackSourceOpenRequested;
			_details.AliasAddRequested -= OnAliasAddRequested;
			_details.AliasRemoveRequested -= OnAliasRemoveRequested;
			_details.NavigateRequested -= NavigateToStableId;
			_details.TransitionCreateRequested -= OnTransitionCreateRequested;
			_details.TransitionRemoveRequested -= OnTransitionRemoveRequested;
			_details.StateOverrideCreateRequested -= OnStateOverrideCreateRequested;
			_details.StateOverrideRestoreRequested -= OnStateOverrideRestoreRequested;
			_details.TransitionOverrideCreateRequested -= OnTransitionOverrideCreateRequested;
			_details.TransitionOverrideRestoreRequested -= OnTransitionOverrideRestoreRequested;
		}
		if (GodotObject.IsInstanceValid(_diagnostics))
		{
			_diagnostics.NavigateRequested -= NavigateToStableId;
		}
		if (GodotObject.IsInstanceValid(_simulation))
		{
			_simulation.StateChanged -= ApplySimulationSnapshot;
			_simulation.NavigateRequested -= NavigateToStableId;
		}
		if (GodotObject.IsInstanceValid(_canvasContextMenu))
		{
			_canvasContextMenu.IdPressed -= OnCanvasContextAction;
		}
	}

	private void OnDetailsResourcePickerRequested(Resource resource, string property)
	{
		this.ResourcePickerRequested?.Invoke(resource, property);
	}

	private void OnDetailsGuardEditRequested(Resource guard, Resource owner, string property, int index)
	{
		this.GuardEditRequested?.Invoke(guard, owner, property, index);
	}

	private void OnDetailsCallbackSourceOpenRequested(StateMachineCallbackCatalogEntry entry, StateMachineCallbackPhase phase)
	{
		this.CallbackSourceOpenRequested?.Invoke(entry, phase);
	}

	private void OnAliasAddRequested(string alias, string target)
	{
		ExecuteMutation(delegate
		{
			GraphController.AddAlias(alias, target);
		});
	}

	private void OnAliasRemoveRequested(string alias)
	{
		ExecuteMutation(delegate
		{
			GraphController.RemoveAlias(alias);
		});
	}

	private void OnTransitionCreateRequested(string sourceStateId, string targetStateId, StateMachineTriggerKind triggerKind, string eventName, double delaySeconds, int priority, string authorTypeId)
	{
		StateMachineGraphController graphController = GraphController;
		if (graphController != null && graphController.ViewModel?.CanMutate == true)
		{
			string addedTransitionId = string.Empty;
			ExecuteMutation(delegate
			{
				addedTransitionId = GraphController.AddTransition(sourceStateId, targetStateId, triggerKind, eventName, delaySeconds, priority, authorTypeId);
			});
			if (!string.IsNullOrWhiteSpace(addedTransitionId))
			{
				ShowTransitionDetails(addedTransitionId, openDetails: true);
			}
		}
	}

	private void OnTransitionRemoveRequested(string stableId)
	{
		ExecuteMutation(delegate
		{
			GraphController.RemoveTransition(stableId);
		});
	}

	private void OnStateOverrideCreateRequested(string stableId)
	{
		ExecuteMutation(delegate
		{
			GraphController.CreateStateOverride(stableId);
		});
	}

	private void OnStateOverrideRestoreRequested(string stableId)
	{
		ExecuteMutation(delegate
		{
			GraphController.RestoreInheritedState(stableId);
		});
	}

	private void OnTransitionOverrideCreateRequested(string stableId)
	{
		ExecuteMutation(delegate
		{
			GraphController.CreateTransitionOverride(stableId);
		});
	}

	private void OnTransitionOverrideRestoreRequested(string stableId)
	{
		ExecuteMutation(delegate
		{
			GraphController.RestoreInheritedTransition(stableId);
		});
	}

	private void BuildCanvasContextMenu()
	{
		_canvasContextMenu = new PopupMenu
		{
			Name = "StateMachineCanvasContextMenu"
		};
		AddChild(_canvasContextMenu, forceReadableName: false, InternalMode.Disabled);
		Texture2D texture = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/atomic_state.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture2 = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/compound_state.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture3 = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/parallel_state.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture4 = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/history_state.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture5 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCopy.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture6 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionCut.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture7 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Duplicate.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture8 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ActionPaste.svg", null, ResourceLoader.CacheMode.Reuse);
		Texture2D texture9 = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/Remove.svg", null, ResourceLoader.CacheMode.Reuse);
		_canvasContextMenu.AddIconItem(texture, "新增原子状态", 100, Key.None);
		_canvasContextMenu.AddIconItem(texture2, "新增复合状态", 101, Key.None);
		_canvasContextMenu.AddIconItem(texture3, "新增并行状态", 102, Key.None);
		_canvasContextMenu.AddIconItem(texture4, "新增历史状态", 103, Key.None);
		_canvasContextMenu.AddSeparator();
		_canvasContextMenu.AddIconItem(texture5, "复制", 200, Key.None);
		_canvasContextMenu.AddIconItem(texture6, "剪切", 201, Key.None);
		_canvasContextMenu.AddIconItem(texture7, "重复", 202, Key.None);
		_canvasContextMenu.AddIconItem(texture8, "粘贴到这里", 203, Key.None);
		_canvasContextMenu.AddIconItem(texture9, "删除", 204, Key.None);
		_canvasContextMenu.IdPressed += OnCanvasContextAction;
	}

	private void OnSurfaceVisibilityChanged()
	{
		SetWorkbenchActive(IsVisibleInTree());
	}

	private void OnToolbarActionPressed(long id)
	{
		switch ((ToolbarAction)id)
		{
		case ToolbarAction.Validate:
			RunDiagnostics();
			_diagnostics.Visible = true;
			break;
		case ToolbarAction.Compile:
			CompilePreview();
			_diagnostics.Visible = true;
			break;
		case ToolbarAction.ToggleDetails:
			ToggleDetailsPanel();
			break;
		case ToolbarAction.ToggleDiagnostics:
			_diagnostics.Visible = !_diagnostics.Visible;
			break;
		}
	}

	private void ApplySimulationSnapshot(StateMachineSnapshot snapshot)
	{
		_activeStateProjectionIds.Clear();
		_pendingStateProjectionIds.Clear();
		if (snapshot != null)
		{
			foreach (string activeStateId in snapshot.ActiveStateIds)
			{
				_activeStateProjectionIds.Add(ResolveVisibleHierarchyProjection(activeStateId));
			}
			foreach (string pendingTransitionId in snapshot.PendingTransitionIds)
			{
				StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = FindTransition(pendingTransitionId);
				if (stateMachineGraphTransitionViewModel?.Transition != null)
				{
					_pendingStateProjectionIds.Add(ResolveVisibleHierarchyProjection(stateMachineGraphTransitionViewModel.Transition.SourceStateId ?? string.Empty));
				}
			}
		}
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			value.SetSimulationState(_activeStateProjectionIds.Contains(value.StableId), _pendingStateProjectionIds.Contains(value.StableId));
		}
		ApplyTransitionChipRuntimeState(snapshot);
	}

	private void ApplyTransitionChipRuntimeState(StateMachineSnapshot snapshot)
	{
		_nextPendingTransitionChipIds.Clear();
		if (snapshot?.PendingTransitionIds != null)
		{
			foreach (string pendingTransitionId in snapshot.PendingTransitionIds)
			{
				if (!string.IsNullOrWhiteSpace(pendingTransitionId))
				{
					_nextPendingTransitionChipIds.Add(pendingTransitionId);
				}
			}
		}
		string text = _simulation?.LastCompletedTransitionStableId ?? string.Empty;
		foreach (string activePendingTransitionChipId in _activePendingTransitionChipIds)
		{
			if (!_nextPendingTransitionChipIds.Contains(activePendingTransitionChipId) && _transitionChips.TryGetValue(activePendingTransitionChipId, out var value))
			{
				value.SetRuntimeState(pending: false, 0.0, string.Equals(activePendingTransitionChipId, text, StringComparison.Ordinal));
			}
		}
		foreach (string nextPendingTransitionChipId in _nextPendingTransitionChipIds)
		{
			if (_transitionChips.TryGetValue(nextPendingTransitionChipId, out var value2))
			{
				double value3 = 0.0;
				snapshot?.PendingDelayRemaining?.TryGetValue(nextPendingTransitionChipId, out value3);
				value2.SetRuntimeState(pending: true, value3, recentlyCompleted: false);
			}
		}
		if (!string.Equals(_lastCompletedTransitionChipId, text, StringComparison.Ordinal))
		{
			if (!string.IsNullOrWhiteSpace(_lastCompletedTransitionChipId) && !_nextPendingTransitionChipIds.Contains(_lastCompletedTransitionChipId) && _transitionChips.TryGetValue(_lastCompletedTransitionChipId, out var value4))
			{
				value4.SetRuntimeState(pending: false, 0.0, recentlyCompleted: false);
			}
			if (!string.IsNullOrWhiteSpace(text) && !_nextPendingTransitionChipIds.Contains(text) && _transitionChips.TryGetValue(text, out var value5))
			{
				value5.SetRuntimeState(pending: false, 0.0, recentlyCompleted: true);
			}
			_lastCompletedTransitionChipId = text;
		}
		_activePendingTransitionChipIds.Clear();
		foreach (string nextPendingTransitionChipId2 in _nextPendingTransitionChipIds)
		{
			_activePendingTransitionChipIds.Add(nextPendingTransitionChipId2);
		}
	}

	private void ApplyTransitionChipSelection(string stableId)
	{
		string text = ((_detailsContext == DetailsContextKind.Transition) ? (stableId ?? string.Empty) : string.Empty);
		if (!string.Equals(_selectedTransitionChipId, text, StringComparison.Ordinal))
		{
			if (!string.IsNullOrWhiteSpace(_selectedTransitionChipId) && _transitionChips.TryGetValue(_selectedTransitionChipId, out var value))
			{
				value.SetSelectedTransition(selected: false);
			}
			_selectedTransitionChipId = text;
			if (!string.IsNullOrWhiteSpace(_selectedTransitionChipId) && _transitionChips.TryGetValue(_selectedTransitionChipId, out var value2))
			{
				value2.SetSelectedTransition(selected: true);
			}
		}
	}

	private string ResolveVisibleHierarchyProjection(string stableId)
	{
		string value = stableId ?? string.Empty;
		int num = _parentStateIds.Count + 1;
		while (!string.IsNullOrWhiteSpace(value) && num-- > 0)
		{
			if (_visibleStateIds.Contains(value))
			{
				return value;
			}
			if (!_parentStateIds.TryGetValue(value, out value))
			{
				break;
			}
		}
		return stableId ?? string.Empty;
	}

	private Vector2 ResolvePosition(string stableId, int index)
	{
		if (Layout != null && Layout.Positions.TryGetValue(stableId, out var value))
		{
			return value;
		}
		return new Vector2(80 + index % 4 * 292, 96 + index / 4 * 184);
	}

	private void AddStateAtViewportCenter()
	{
		StateMachineStateKind kind = ((_newStateKind != null) ? ((StateMachineStateKind)_newStateKind.GetSelectedId()) : StateMachineStateKind.Atomic);
		AddStateAtViewportCenter(kind);
	}

	private void AddStateAtViewportCenter(StateMachineStateKind kind)
	{
		if (GraphController?.ViewModel != null && GraphController.ViewModel.StateMachineDefinition != null && GraphController.ViewModel.CanMutate)
		{
			Vector2 position = base.ScrollOffset + base.Size * 0.5f / Mathf.Max(base.Zoom, 0.001f);
			AddStateAtGraphPosition(kind, position);
		}
	}

	private void AddStateAtGraphPosition(StateMachineStateKind kind, Vector2 position)
	{
		if (GraphController?.ViewModel != null && GraphController.ViewModel.StateMachineDefinition != null && GraphController.ViewModel.CanMutate)
		{
			string displayName = kind switch
			{
				StateMachineStateKind.Compound => "新复合状态", 
				StateMachineStateKind.Parallel => "新并行状态", 
				StateMachineStateKind.History => "新历史状态", 
				_ => "新原子状态", 
			};
			string addedStableId = string.Empty;
			string parentId = ResolveParentForNewState(kind);
			ExecuteMutation(delegate
			{
				addedStableId = GraphController.AddStateHierarchySafeAtPosition(displayName, kind, position, parentId, GetSelectedStateAuthorTypeId());
			});
			if (!string.IsNullOrWhiteSpace(addedStableId))
			{
				ShowStateDetails(addedStableId, selectNode: true);
			}
		}
	}

	private string ResolveParentForNewState(StateMachineStateKind kind)
	{
		StateMachineGraphViewModel stateMachineGraphViewModel = GraphController?.ViewModel;
		if (stateMachineGraphViewModel?.StateMachineDefinition == null)
		{
			return string.Empty;
		}
		foreach (StringName selectedStateNode in GetSelectedStateNodes())
		{
			_nodes.TryGetValue(selectedStateNode.ToString(), out var value);
			if (CanContainStateKind(value?.State, kind))
			{
				return value.StableId;
			}
			StateMachineStateDefinition stateMachineStateDefinition = FindVisibleState(value?.State?.ParentId);
			if (CanContainStateKind(stateMachineStateDefinition, kind))
			{
				return stateMachineStateDefinition.StableId;
			}
		}
		StateMachineStateDefinition stateMachineStateDefinition2 = FindVisibleState(stateMachineGraphViewModel.StateMachineDefinition.RootStateId);
		if (CanContainStateKind(stateMachineStateDefinition2, kind) || (stateMachineStateDefinition2 != null && stateMachineStateDefinition2.Kind == StateMachineStateKind.Atomic && kind != StateMachineStateKind.History))
		{
			return stateMachineStateDefinition2.StableId;
		}
		return string.Empty;
	}

	private StateMachineStateDefinition FindVisibleState(string stableId)
	{
		if (string.IsNullOrWhiteSpace(stableId) || GraphController?.ViewModel == null)
		{
			return null;
		}
		foreach (StateMachineNodeViewModel node in GraphController.ViewModel.Nodes)
		{
			if (node?.State != null && string.Equals(node.StableId, stableId, StringComparison.Ordinal))
			{
				return node.State;
			}
		}
		return null;
	}

	private static bool CanContainStateKind(StateMachineStateDefinition state, StateMachineStateKind childKind)
	{
		if (state != null)
		{
			if (state.Kind != StateMachineStateKind.Compound)
			{
				if (state.Kind == StateMachineStateKind.Parallel)
				{
					return childKind != StateMachineStateKind.History;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private void OnConnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		if (_refreshing || GraphController == null)
		{
			return;
		}
		StateMachineGraphNode source = FindNodeByGraphName(fromNode);
		StateMachineGraphNode target = FindNodeByGraphName(toNode);
		if (source != null && target != null && GraphController.ViewModel.CanMutate)
		{
			string addedTransitionId = string.Empty;
			ExecuteMutation(delegate
			{
				addedTransitionId = GraphController.AddTransition(source.StableId, target.StableId, StateMachineTriggerKind.Event, "", 0.0, 0, _details?.SelectedTransitionAuthorTypeId ?? string.Empty);
			});
			if (!string.IsNullOrWhiteSpace(addedTransitionId))
			{
				ShowTransitionDetails(addedTransitionId, openDetails: true);
			}
		}
	}

	private void OnDisconnectionRequest(StringName fromNode, long fromPort, StringName toNode, long toPort)
	{
		if (_refreshing || GraphController == null)
		{
			return;
		}
		StateMachineGraphNode stateMachineGraphNode = FindNodeByGraphName(fromNode);
		StateMachineGraphNode stateMachineGraphNode2 = FindNodeByGraphName(toNode);
		if (stateMachineGraphNode == null || stateMachineGraphNode2 == null)
		{
			return;
		}
		List<StateMachineGraphTransitionViewModel> list = new List<StateMachineGraphTransitionViewModel>();
		foreach (StateMachineGraphTransitionViewModel transition2 in GraphController.ViewModel.Transitions)
		{
			StateMachineTransitionDefinition transition = transition2.Transition;
			if (!transition2.IsReadOnly && transition.SourceStateId == stateMachineGraphNode.StableId && transition.TargetStateId == stateMachineGraphNode2.StableId)
			{
				list.Add(transition2);
			}
		}
		if (list.Count == 1)
		{
			string stableId = list[0].StableId;
			ExecuteMutation(delegate
			{
				GraphController.RemoveTransition(stableId);
			});
		}
		else
		{
			if (list.Count <= 1)
			{
				return;
			}
			StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = list.Find((StateMachineGraphTransitionViewModel item) => _detailsContext == DetailsContextKind.Transition && item.StableId == _detailsStableId);
			if (stateMachineGraphTransitionViewModel != null)
			{
				string stableId2 = stateMachineGraphTransitionViewModel.StableId;
				ExecuteMutation(delegate
				{
					GraphController.RemoveTransition(stableId2);
				});
			}
			else
			{
				LastConnectionSelectionGroupSize = list.Count;
				ShowTransitionDetails(list[0].StableId, openDetails: true);
			}
		}
	}

	private void OnDeleteNodesRequest(Array<StringName> requested)
	{
		if (GraphController == null)
		{
			return;
		}
		List<string> stableIds = new List<string>();
		foreach (StringName item in requested)
		{
			StateMachineGraphNode stateMachineGraphNode = FindNodeByGraphName(item);
			if (stateMachineGraphNode != null && !stateMachineGraphNode.IsReadOnly && stateMachineGraphNode.StableId != GraphController.Definition?.RootStateId)
			{
				stableIds.Add(stateMachineGraphNode.StableId);
			}
		}
		if (stableIds.Count > 0)
		{
			ExecuteMutation(delegate
			{
				GraphController.RemoveStates(stableIds);
			});
		}
	}

	private void OnCopyNodesRequest()
	{
		if (GraphController == null || !GraphController.CanCopySubgraph)
		{
			return;
		}
		List<string> list = new List<string>();
		System.Collections.Generic.Dictionary<string, Vector2> dictionary = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);
		foreach (StringName selectedStateNode in GetSelectedStateNodes())
		{
			string text = selectedStateNode.ToString();
			list.Add(text);
			if (_nodes.TryGetValue(text, out var value))
			{
				dictionary[text] = value.PositionOffset;
			}
		}
		GraphController.CopySubgraph(list, dictionary);
	}

	private void OnCutNodesRequest()
	{
		OnCopyNodesRequest();
		OnDeleteNodesRequest(GetSelectedNodes());
	}

	private void OnPasteNodesRequest()
	{
		if (GraphController == null || !GraphController.CanPasteSubgraph)
		{
			return;
		}
		IReadOnlyDictionary<string, string> pasted = null;
		ExecuteMutation(delegate
		{
			pasted = GraphController.PasteSubgraphAt(_lastPointerGraphPosition);
		});
		if (pasted == null || pasted.Count == 0)
		{
			return;
		}
		foreach (StateMachineGraphNode value2 in _nodes.Values)
		{
			value2.Selected = false;
		}
		string text = string.Empty;
		foreach (string value3 in pasted.Values)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				text = value3;
			}
			if (_nodes.TryGetValue(value3, out var value))
			{
				value.Selected = true;
			}
		}
		if (!string.IsNullOrWhiteSpace(text))
		{
			ShowStateDetails(text);
		}
	}

	private void OnDuplicateNodesRequest()
	{
		OnCopyNodesRequest();
		OnPasteNodesRequest();
	}

	private void OnPopupRequest(Vector2 atPosition)
	{
		if (_canvasContextMenu != null)
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
		bool flag = GraphController?.ViewModel?.StateMachineDefinition == null || !GraphController.ViewModel.CanMutate;
		bool flag2 = GetSelectedStateNodes().Count > 0;
		long[] array = new long[4] { 100L, 101L, 102L, 103L };
		foreach (long num in array)
		{
			_canvasContextMenu.SetItemDisabled(_canvasContextMenu.GetItemIndex((int)num), flag);
		}
		PopupMenu canvasContextMenu = _canvasContextMenu;
		int itemIndex = _canvasContextMenu.GetItemIndex(200);
		StateMachineGraphController graphController = GraphController;
		canvasContextMenu.SetItemDisabled(itemIndex, graphController == null || !graphController.CanCopySubgraph || !flag2);
		_canvasContextMenu.SetItemDisabled(_canvasContextMenu.GetItemIndex(201), flag || !flag2);
		_canvasContextMenu.SetItemDisabled(_canvasContextMenu.GetItemIndex(202), flag || !flag2);
		PopupMenu canvasContextMenu2 = _canvasContextMenu;
		int itemIndex2 = _canvasContextMenu.GetItemIndex(203);
		int disabled;
		if (!flag)
		{
			StateMachineGraphController graphController2 = GraphController;
			disabled = ((graphController2 == null || !graphController2.CanPasteSubgraph) ? 1 : 0);
		}
		else
		{
			disabled = 1;
		}
		canvasContextMenu2.SetItemDisabled(itemIndex2, (byte)disabled != 0);
		_canvasContextMenu.SetItemDisabled(_canvasContextMenu.GetItemIndex(204), flag || !flag2);
	}

	private void OnCanvasContextAction(long id)
	{
		long num = id - 100;
		if ((ulong)num <= 3uL)
		{
			switch ((int)num)
			{
			case 0:
				AddStateAtGraphPosition(StateMachineStateKind.Atomic, _lastPointerGraphPosition);
				return;
			case 1:
				AddStateAtGraphPosition(StateMachineStateKind.Compound, _lastPointerGraphPosition);
				return;
			case 2:
				AddStateAtGraphPosition(StateMachineStateKind.Parallel, _lastPointerGraphPosition);
				return;
			case 3:
				AddStateAtGraphPosition(StateMachineStateKind.History, _lastPointerGraphPosition);
				return;
			}
		}
		long num2 = id - 200;
		if ((ulong)num2 <= 4uL)
		{
			switch ((int)num2)
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
				OnDeleteNodesRequest(GetSelectedNodes());
				break;
			}
		}
	}

	private Vector2 ToGraphPosition(Vector2 localPosition)
	{
		return base.ScrollOffset + localPosition / Mathf.Max(base.Zoom, 0.001f);
	}

	private void OnBeginNodeMove()
	{
		_moveOrigins.Clear();
		_movingStateIds.Clear();
		_movingStatePositions.Clear();
		foreach (StringName selectedStateNode in GetSelectedStateNodes())
		{
			if (_nodes.TryGetValue(selectedStateNode.ToString(), out var value))
			{
				_moveOrigins[value.StableId] = value.PositionOffset;
				_movingStateIds.Add(value.StableId);
				_movingStatePositions[value.StableId] = value.PositionOffset;
			}
		}
		_trackingNodeMove = _movingStateIds.Count > 0;
		SetProcess(_trackingNodeMove && IsWorkbenchActive);
	}

	private void OnEndNodeMove()
	{
		UpdateMovingTransitionChipPositions();
		_trackingNodeMove = false;
		SetProcess(enable: false);
		if (_refreshing || GraphController == null)
		{
			_moveOrigins.Clear();
			_movingStateIds.Clear();
			_movingStatePositions.Clear();
			return;
		}
		System.Collections.Generic.Dictionary<string, Vector2> dictionary = new System.Collections.Generic.Dictionary<string, Vector2>(StringComparer.Ordinal);
		foreach (KeyValuePair<string, Vector2> moveOrigin in _moveOrigins)
		{
			if (_nodes.TryGetValue(moveOrigin.Key, out var value))
			{
				if (value.IsReadOnly)
				{
					value.PositionOffset = moveOrigin.Value;
				}
				else if (!value.PositionOffset.IsEqualApprox(moveOrigin.Value))
				{
					dictionary[moveOrigin.Key] = value.PositionOffset;
				}
			}
		}
		UpdateTransitionChipPositionsForStates(_movingStateIds, "OnEndNodeMove");
		_moveOrigins.Clear();
		_movingStateIds.Clear();
		_movingStatePositions.Clear();
		if (dictionary.Count == 0)
		{
			return;
		}
		_layoutMutationMovedStateIds.Clear();
		foreach (string key in dictionary.Keys)
		{
			_layoutMutationMovedStateIds.Add(key);
		}
		try
		{
			GraphController.MoveGraphNodes(dictionary);
		}
		finally
		{
			_layoutMutationMovedStateIds.Clear();
		}
	}

	private void OnScrollOffsetChanged(Vector2 scrollOffset)
	{
		if (!_refreshing)
		{
			QueueTransitionChipLayoutAfterGraphSettles("OnScrollOffsetChanged");
			QueueViewportPersistence(scrollOffset, base.Zoom);
		}
	}

	private void OnGraphGuiInput(InputEvent inputEvent)
	{
		if (inputEvent is InputEventMouseMotion inputEventMouseMotion)
		{
			_lastPointerGraphPosition = ToGraphPosition(inputEventMouseMotion.Position);
		}
		else if (inputEvent is InputEventMouseButton inputEventMouseButton)
		{
			_lastPointerGraphPosition = ToGraphPosition(inputEventMouseButton.Position);
		}
		if (inputEvent is InputEventMouseButton { Pressed: not false } inputEventMouseButton2 && inputEventMouseButton2.ButtonIndex == MouseButton.Left && TrySelectConnectionAt(inputEventMouseButton2.Position))
		{
			AcceptEvent();
			return;
		}
		if (inputEvent is InputEventKey { Pressed: not false, Echo: false, CtrlPressed: not false } inputEventKey)
		{
			bool num = inputEventKey.Keycode == Key.Z && !inputEventKey.ShiftPressed;
			bool flag = inputEventKey.Keycode == Key.Y || (inputEventKey.Keycode == Key.Z && inputEventKey.ShiftPressed);
			if (num)
			{
				IStateMachineUndoAdapter undoAdapter = UndoAdapter;
				if (undoAdapter != null && undoAdapter.CanUndo)
				{
					GraphController.Undo();
					AcceptEvent();
					return;
				}
			}
			if (flag)
			{
				IStateMachineUndoAdapter undoAdapter2 = UndoAdapter;
				if (undoAdapter2 != null && undoAdapter2.CanRedo)
				{
					GraphController.Redo();
					AcceptEvent();
					return;
				}
			}
		}
		InputEventMouseButton inputEventMouseButton3 = inputEvent as InputEventMouseButton;
		bool flag2 = inputEventMouseButton3?.Pressed ?? false;
		if (flag2)
		{
			MouseButton buttonIndex = inputEventMouseButton3.ButtonIndex;
			bool flag3 = (((ulong)(buttonIndex - 4) <= 1uL) ? true : false);
			flag2 = flag3;
		}
		if (flag2)
		{
			QueueViewportCapture();
		}
		else if (inputEvent is InputEventMagnifyGesture)
		{
			QueueViewportCapture();
		}
	}

	private bool TrySelectConnectionAt(Vector2 localPosition)
	{
		if (GraphController?.ViewModel == null)
		{
			return false;
		}
		Vector2 point = ToGraphPosition(localPosition);
		float num = 18f / Mathf.Max(base.Zoom, 0.001f);
		foreach (var visualConnection in _visualConnections)
		{
			if (!_nodes.TryGetValue(visualConnection.Source, out var value) || !_nodes.TryGetValue(visualConnection.Target, out var value2))
			{
				continue;
			}
			GetConnectionEndpoints(value, value2, out var from, out var to);
			if (DistanceToConnectionCurve(point, from, to, base.ConnectionLinesCurvature) > num)
			{
				continue;
			}
			List<StateMachineGraphTransitionViewModel> list = new List<StateMachineGraphTransitionViewModel>();
			foreach (StateMachineGraphTransitionViewModel transition in GraphController.ViewModel.Transitions)
			{
				if (transition?.Transition?.SourceStateId == visualConnection.Source && transition.Transition.TargetStateId == visualConnection.Target)
				{
					list.Add(transition);
				}
			}
			if (list.Count != 0)
			{
				int num2 = list.FindIndex((StateMachineGraphTransitionViewModel item) => _detailsContext == DetailsContextKind.Transition && item.StableId == _detailsStableId);
				StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = list[(num2 + 1) % list.Count];
				LastConnectionSelectionGroupSize = list.Count;
				return ShowTransitionDetails(stateMachineGraphTransitionViewModel.StableId, openDetails: true);
			}
		}
		LastConnectionSelectionGroupSize = 0;
		return false;
	}

	private static float DistanceToSegment(Vector2 point, Vector2 from, Vector2 to)
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

	private static float DistanceToConnectionCurve(Vector2 point, Vector2 from, Vector2 to, float curvature)
	{
		GetConnectionCurveControls(from, to, curvature, out var controlFrom, out var controlTo);
		float num = 3.4028235E+38f;
		Vector2 vector = from;
		for (int i = 1; i <= 24; i++)
		{
			float t = (float)i / 24f;
			Vector2 vector2 = from.BezierInterpolate(controlFrom, controlTo, to, t);
			num = Mathf.Min(num, DistanceToSegment(point, vector, vector2));
			vector = vector2;
		}
		return num;
	}

	private void QueueViewportCapture()
	{
		if (!_viewportCaptureQueued)
		{
			_viewportCaptureQueued = true;
			CallDeferred("CaptureViewportAfterInput");
		}
	}

	private void CaptureViewportAfterInput()
	{
		_viewportCaptureQueued = false;
		if (GodotObject.IsInstanceValid(this) && !_refreshing)
		{
			QueueTransitionChipLayoutAfterGraphSettles("CaptureViewportAfterInput");
			QueueViewportPersistence(base.ScrollOffset, base.Zoom);
		}
	}

	private void QueueViewportPersistence(Vector2 scrollOffset, float zoom)
	{
		if (!_refreshing && GraphController?.Layout != null)
		{
			_pendingScrollOffset = scrollOffset;
			_pendingZoom = Mathf.Clamp(zoom, 0.25f, 2f);
			_viewportDirty = true;
			_viewportDebounceTimer?.Start();
		}
	}

	public void FlushViewportState()
	{
		if (_viewportDirty && GraphController?.Layout != null)
		{
			_viewportDebounceTimer?.Stop();
			_viewportDirty = false;
			if (GraphController.UpdateViewportState(_pendingScrollOffset, _pendingZoom))
			{
				this.LayoutChanged?.Invoke();
			}
		}
	}

	private void OnNodeSelected(Node selected)
	{
		if (!_refreshing && selected is StateMachineGraphNode stateMachineGraphNode)
		{
			ShowStateDetails(stateMachineGraphNode.StableId);
		}
	}

	private void OnNodeCollapseRequested(StateMachineGraphNode node, bool collapsed)
	{
		if (!_refreshing && GraphController?.Layout != null && node != null && node.HierarchyChildCount > 0 && node.IsHierarchyCollapsed != collapsed)
		{
			ShowStateDetails(node.StableId, selectNode: true);
			ExecuteMutation(delegate
			{
				GraphController.SetCollapsed(node.StableId, collapsed);
			});
		}
	}

	private bool ShowStateDetails(string stableId, bool selectNode = false)
	{
		_details?.FlushPendingNumericCommit();
		if (GraphController == null)
		{
			return false;
		}
		foreach (StateMachineNodeViewModel node in GraphController.ViewModel.Nodes)
		{
			if (!(node.StableId == stableId))
			{
				continue;
			}
			_detailsContext = DetailsContextKind.State;
			_detailsStableId = stableId;
			ApplyTransitionChipSelection(string.Empty);
			if (selectNode && _visibleStateIds.Contains(stableId) && _nodes.TryGetValue(stableId, out var value))
			{
				foreach (StateMachineGraphNode value2 in _nodes.Values)
				{
					value2.Selected = false;
				}
				value.Selected = true;
			}
			_details.ShowState(node.State, node.IsInherited, node.IsLocalOverride, GraphController.ViewModel.IsReadOnly || GraphController.ViewModel.HasCompositionFailure);
			OpenDetailsForSelection();
			return true;
		}
		return false;
	}

	private bool ShowTransitionDetails(string stableId, bool openDetails = false)
	{
		_details?.FlushPendingNumericCommit();
		StateMachineGraphTransitionViewModel stateMachineGraphTransitionViewModel = FindTransition(stableId);
		if (stateMachineGraphTransitionViewModel == null)
		{
			return false;
		}
		_detailsContext = DetailsContextKind.Transition;
		_detailsStableId = stateMachineGraphTransitionViewModel.StableId;
		ApplyTransitionChipSelection(stateMachineGraphTransitionViewModel.StableId);
		_details?.ShowTransition(stateMachineGraphTransitionViewModel.Transition, stateMachineGraphTransitionViewModel.IsInherited, stateMachineGraphTransitionViewModel.IsLocalOverride, GraphController.ViewModel.IsReadOnly || GraphController.ViewModel.HasCompositionFailure);
		if (openDetails)
		{
			OpenDetailsForSelection();
		}
		return true;
	}

	public void ShowDefinitionOverview()
	{
		_details?.FlushPendingNumericCommit();
		if (GraphController?.ViewModel?.StateMachineDefinition == null)
		{
			return;
		}
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			value.Selected = false;
		}
		_detailsContext = DetailsContextKind.Definition;
		_detailsStableId = string.Empty;
		ApplyTransitionChipSelection(string.Empty);
		_details?.ShowDefinition(GraphController.ViewModel.StateMachineDefinition, GraphController.ViewModel.IsReadOnly, GraphController.ViewModel.HasCompositionFailure);
		OpenDetailsForSelection();
	}

	private void RestoreDetailsContext(StateMachineGraphViewModel viewModel)
	{
		if ((_detailsContext != DetailsContextKind.State || !ShowStateDetails(_detailsStableId, selectNode: true)) && (_detailsContext != DetailsContextKind.Transition || !ShowTransitionDetails(_detailsStableId)))
		{
			_detailsContext = DetailsContextKind.Definition;
			_detailsStableId = string.Empty;
			ApplyTransitionChipSelection(string.Empty);
			_details?.ShowDefinition(viewModel?.StateMachineDefinition, viewModel?.IsReadOnly ?? true, viewModel?.HasCompositionFailure ?? false);
		}
	}

	private StateMachineGraphTransitionViewModel FindTransition(string stableId)
	{
		if (GraphController == null)
		{
			return null;
		}
		foreach (StateMachineGraphTransitionViewModel transition in GraphController.ViewModel.Transitions)
		{
			if (transition.StableId == stableId)
			{
				return transition;
			}
		}
		return null;
	}

	private void SetAsInitialState(StateMachineStateDefinition child)
	{
		if (child == null || string.IsNullOrWhiteSpace(child.ParentId))
		{
			return;
		}
		StateMachineNodeViewModel stateMachineNodeViewModel = null;
		StateMachineNodeViewModel stateMachineNodeViewModel2 = null;
		foreach (StateMachineNodeViewModel item in GraphController?.ViewModel?.Nodes ?? System.Array.Empty<StateMachineNodeViewModel>())
		{
			if (item?.StableId == child.StableId)
			{
				stateMachineNodeViewModel = item;
			}
			if (item?.StableId == child.ParentId)
			{
				stateMachineNodeViewModel2 = item;
			}
		}
		if (stateMachineNodeViewModel == null || stateMachineNodeViewModel.IsReadOnly || stateMachineNodeViewModel2 == null || stateMachineNodeViewModel2.IsReadOnly)
		{
			return;
		}
		StateMachineStateDefinition state = stateMachineNodeViewModel2.State;
		if (state != null && state.Kind == StateMachineStateKind.Compound)
		{
			ExecuteMutation(delegate
			{
				GraphController.SetInitialState(child.ParentId, child.StableId);
			});
		}
	}

	private void ApplyFieldChange(GodotObject owner, StringName propertyName, Variant value)
	{
		if (GraphController == null || owner == null)
		{
			return;
		}
		string text = propertyName.ToString();
		if (owner is StateMachineDefinition)
		{
			if (text == "RootStateId")
			{
				ExecuteMutation(delegate
				{
					GraphController.SetRootState(value.AsString());
				});
			}
			else if (text == "BaseDefinition")
			{
				StateMachineDefinition candidate = ((value.VariantType == Variant.Type.Nil) ? null : (value.AsGodotObject() as StateMachineDefinition));
				bool accepted = false;
				ExecuteMutation(delegate
				{
					accepted = GraphController.SetBaseDefinition(candidate);
				});
				if (!accepted)
				{
					ShowCompositionFailureDiagnostics();
				}
			}
			else
			{
				ExecuteMutation(delegate
				{
					GraphController.SetDefinitionProperty(propertyName, value);
				});
			}
			return;
		}
		StateMachineStateDefinition state = owner as StateMachineStateDefinition;
		if (state != null)
		{
			switch (text)
			{
			case "DisplayName":
				ExecuteMutation(delegate
				{
					GraphController.RenameState(state.StableId, value.AsString());
				});
				break;
			case "Kind":
				ExecuteMutation(delegate
				{
					GraphController.SetStateKind(state.StableId, (StateMachineStateKind)value.AsInt32());
				});
				break;
			case "ParentId":
				ExecuteMutation(delegate
				{
					GraphController.SetStateParent(state.StableId, value.AsString());
				});
				break;
			case "InitialChildId":
				if (string.IsNullOrWhiteSpace(value.AsString()))
				{
					ExecuteMutation(delegate
					{
						GraphController.SetStateProperty(state.StableId, propertyName, value);
					});
				}
				else
				{
					ExecuteMutation(delegate
					{
						GraphController.SetInitialState(state.StableId, value.AsString());
					});
				}
				break;
			case "ProcessFlags":
				ExecuteMutation(delegate
				{
					GraphController.SetStateProcessFlags(state.StableId, (StateMachineProcessFlags)value.AsInt32());
				});
				break;
			case "CallbackKey":
				ExecuteMutation(delegate
				{
					GraphController.SetStateCallbackKey(state.StableId, value.AsString());
				});
				break;
			case "EnterCallbackKey":
				ExecuteMutation(delegate
				{
					GraphController.SetStateLifecycleCallbackKey(state.StableId, StateMachineCallbackPhase.Enter, value.AsString());
				});
				break;
			case "ExitCallbackKey":
				ExecuteMutation(delegate
				{
					GraphController.SetStateLifecycleCallbackKey(state.StableId, StateMachineCallbackPhase.Exit, value.AsString());
				});
				break;
			case "ProcessCallbackKey":
				ExecuteMutation(delegate
				{
					GraphController.SetStateLifecycleCallbackKey(state.StableId, StateMachineCallbackPhase.Process, value.AsString());
				});
				break;
			case "PhysicsProcessCallbackKey":
				ExecuteMutation(delegate
				{
					GraphController.SetStateLifecycleCallbackKey(state.StableId, StateMachineCallbackPhase.PhysicsProcess, value.AsString());
				});
				break;
			default:
				ExecuteMutation(delegate
				{
					GraphController.SetStateProperty(state.StableId, propertyName, value);
				});
				break;
			}
			return;
		}
		StateMachineTransitionDefinition transition = owner as StateMachineTransitionDefinition;
		if (transition == null)
		{
			return;
		}
		if (text == "GuardDefinition")
		{
			ExecuteMutation(delegate
			{
				GraphController.SetTransitionGuard(transition.StableId, value.As<Resource>());
			});
		}
		else
		{
			ExecuteMutation(delegate
			{
				GraphController.SetTransitionProperty(transition.StableId, propertyName, value);
			});
		}
	}

	private void RunDiagnostics()
	{
		StateMachineValidationResult result = GraphController?.Validate();
		_diagnostics.ShowDiagnostics(result);
	}

	private void CompilePreview()
	{
		if (GraphController != null)
		{
			GraphController.CompilePreview(out var _, out var validation);
			_diagnostics.ShowDiagnostics(validation);
		}
	}

	private void ExecuteMutation(Action mutation)
	{
		if (GraphController == null || mutation == null)
		{
			return;
		}
		try
		{
			mutation();
		}
		catch (Exception ex)
		{
			GD.PushWarning("状态机操作未完成：" + ex.Message);
			RunDiagnostics();
		}
	}

	private StateMachineGraphNode FindNodeByGraphName(StringName graphName)
	{
		foreach (StateMachineGraphNode value in _nodes.Values)
		{
			if (value.Name == graphName)
			{
				return value;
			}
		}
		return null;
	}
}
