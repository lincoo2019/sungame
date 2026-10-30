using System;
using Godot;

[Tool]
public partial class StateMachineGraphNode : GraphNode
{
	private Label _kindBadge;

	private Label _identityLabel;

	private Label _callbackLabel;

	private Label _hierarchyBadge;

	private Label _initialChildBadge;

	private Label _rootStateBadge;

	private Label _lockLabel;

	private Label _simulationBadge;

	private Button _inspectButton;

	private Button _initialButton;

	private Button _collapseButton;

	private Color _baseColor = Colors.White;

	private bool _simulationActive;

	private bool _simulationPending;

	public StateMachineStateDefinition State { get; private set; }

	public string StableId => State?.StableId ?? string.Empty;

	public bool IsInherited { get; private set; }

	public bool IsReadOnly { get; private set; }

	public bool IsSimulationActive => _simulationActive;

	public bool IsSimulationPending => _simulationPending;

	public int HierarchyChildCount { get; private set; }

	public bool IsHierarchyCollapsed { get; private set; }

	public bool IsInitialChild { get; private set; }

	public bool IsHierarchyRoot { get; private set; }

	public bool CanSetAsInitial { get; private set; }

	public string ParentStableId { get; private set; } = string.Empty;

	public event Action<StateMachineGraphNode> InspectRequested;

	public event Action<StateMachineGraphNode> InitialStateRequested;

	public event Action<StateMachineGraphNode, bool> CollapseRequested;

	public override void _Ready()
	{
		BuildPuzzleTile();
		RefreshVisuals();
	}

	public void SetSimulationState(bool active, bool pending)
	{
		if (_simulationActive != active || _simulationPending != pending)
		{
			_simulationActive = active;
			_simulationPending = pending;
			RefreshSimulationVisuals();
		}
	}

	public void Bind(StateMachineStateDefinition state, bool isInherited, bool isReadOnly)
	{
		State = state;
		IsInherited = isInherited;
		IsReadOnly = isReadOnly;
		base.Name = $"State_{GetInstanceId()}";
		RefreshVisuals();
	}

	public void BindHierarchy(string parentStableId, string parentDisplayName, int childCount, bool isInitialChild, bool isHierarchyRoot, bool canSetAsInitial, bool collapsed)
	{
		ParentStableId = parentStableId ?? string.Empty;
		HierarchyChildCount = Math.Max(0, childCount);
		IsInitialChild = isInitialChild;
		IsHierarchyRoot = isHierarchyRoot;
		CanSetAsInitial = canSetAsInitial;
		IsHierarchyCollapsed = collapsed && HierarchyChildCount > 0;
		RefreshHierarchyVisuals(parentDisplayName);
	}

	private void BuildPuzzleTile()
	{
		if (GetNodeOrNull<Control>("PuzzleTile") == null)
		{
			VBoxContainer vBoxContainer = new VBoxContainer
			{
				Name = "PuzzleTile",
				CustomMinimumSize = new Vector2(236f, 136f),
				MouseFilter = MouseFilterEnum.Pass
			};
			AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer hBoxContainer = new HBoxContainer
			{
				Name = "HeadingRow",
				MouseFilter = MouseFilterEnum.Pass
			};
			vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
			_kindBadge = new Label
			{
				Name = "KindBadge",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer.AddChild(_kindBadge, forceReadableName: false, InternalMode.Disabled);
			_simulationBadge = new Label
			{
				Name = "SimulationBadge",
				Visible = false,
				TooltipText = "状态机沙盒中的实时状态",
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer.AddChild(_simulationBadge, forceReadableName: false, InternalMode.Disabled);
			_lockLabel = new Label
			{
				Name = "InheritanceBadge",
				TooltipText = "继承节点为只读；可在基定义中修改",
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer.AddChild(_lockLabel, forceReadableName: false, InternalMode.Disabled);
			_identityLabel = new Label
			{
				Name = "StableIdLabel",
				AutowrapMode = TextServer.AutowrapMode.WordSmart,
				MouseFilter = MouseFilterEnum.Ignore
			};
			vBoxContainer.AddChild(_identityLabel, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer hBoxContainer2 = new HBoxContainer
			{
				Name = "HierarchyRow",
				MouseFilter = MouseFilterEnum.Pass
			};
			vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
			_collapseButton = new Button
			{
				Name = "CollapseSubtreeButton",
				CustomMinimumSize = new Vector2(38f, 28f),
				FocusMode = FocusModeEnum.None,
				Visible = false,
				MouseFilter = MouseFilterEnum.Stop,
				Icon = ResourceLoader.Load<Texture2D>("res://addons/godot_state_charts/toggle_sidebar.svg", null, ResourceLoader.CacheMode.Reuse)
			};
			_collapseButton.Pressed += delegate
			{
				this.CollapseRequested?.Invoke(this, !IsHierarchyCollapsed);
			};
			hBoxContainer2.AddChild(_collapseButton, forceReadableName: false, InternalMode.Disabled);
			_hierarchyBadge = new Label
			{
				Name = "HierarchyBadge",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				Modulate = new Color(0.72f, 0.82f, 0.96f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer2.AddChild(_hierarchyBadge, forceReadableName: false, InternalMode.Disabled);
			_initialChildBadge = new Label
			{
				Name = "InitialChildBadge",
				Text = "★ 入口",
				TooltipText = "父复合状态进入时首先激活这个子状态",
				Visible = false,
				Modulate = new Color(1f, 0.78f, 0.28f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer2.AddChild(_initialChildBadge, forceReadableName: false, InternalMode.Disabled);
			_rootStateBadge = new Label
			{
				Name = "RootStateBadge",
				Text = "♛ 根",
				TooltipText = "状态机层级根状态",
				Visible = false,
				Modulate = new Color(0.56f, 0.92f, 1f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			hBoxContainer2.AddChild(_rootStateBadge, forceReadableName: false, InternalMode.Disabled);
			_callbackLabel = new Label
			{
				Name = "CallbackLabel",
				Modulate = new Color(0.76f, 0.82f, 0.9f),
				MouseFilter = MouseFilterEnum.Ignore
			};
			vBoxContainer.AddChild(_callbackLabel, forceReadableName: false, InternalMode.Disabled);
			HBoxContainer hBoxContainer3 = new HBoxContainer
			{
				Name = "ActionRow",
				MouseFilter = MouseFilterEnum.Pass
			};
			vBoxContainer.AddChild(hBoxContainer3, forceReadableName: false, InternalMode.Disabled);
			_inspectButton = new Button
			{
				Name = "InspectButton",
				Text = "配置拼图",
				SizeFlagsHorizontal = SizeFlags.ExpandFill,
				MouseFilter = MouseFilterEnum.Stop
			};
			_inspectButton.Pressed += delegate
			{
				this.InspectRequested?.Invoke(this);
			};
			hBoxContainer3.AddChild(_inspectButton, forceReadableName: false, InternalMode.Disabled);
			_initialButton = new Button
			{
				Name = "InitialButton",
				Text = "设为入口",
				TooltipText = "设为父复合状态的初始子状态",
				MouseFilter = MouseFilterEnum.Stop
			};
			_initialButton.Pressed += delegate
			{
				this.InitialStateRequested?.Invoke(this);
			};
			hBoxContainer3.AddChild(_initialButton, forceReadableName: false, InternalMode.Disabled);
			SetSlot(0, enableLeftPort: true, 0, new Color(0.28f, 0.78f, 1f), enableRightPort: true, 0, new Color(1f, 0.66f, 0.24f));
		}
	}

	private void RefreshVisuals()
	{
		if (_kindBadge != null)
		{
			StateMachineStateKind stateMachineStateKind = State?.Kind ?? StateMachineStateKind.Atomic;
			base.Title = ((State == null || State.DisplayName.IsEmpty) ? "未命名状态" : State.DisplayName.ToString());
			Label kindBadge = _kindBadge;
			kindBadge.Text = stateMachineStateKind switch
			{
				StateMachineStateKind.Atomic => "◆ 原子状态", 
				StateMachineStateKind.Compound => "▣ 复合状态", 
				StateMachineStateKind.Parallel => "▥ 并行状态", 
				StateMachineStateKind.History => "◴ 历史状态", 
				_ => stateMachineStateKind.ToString(), 
			};
			_identityLabel.Text = (string.IsNullOrWhiteSpace(StableId) ? "ID 尚未生成" : StableId);
			int independentActionCount = GetIndependentActionCount(State);
			if (independentActionCount > 0)
			{
				_callbackLabel.Text = $"◆ 生命周期拼图 {independentActionCount}/4";
			}
			else
			{
				string text = State?.CallbackKey.ToString() ?? string.Empty;
				_callbackLabel.Text = (string.IsNullOrWhiteSpace(text) ? "生命周期动作：无" : (StateMachineCallbackKey.UsesExecutablePrefix(text) ? ("生命周期动作：" + text) : ("旧版性能标签：" + text)));
			}
			_lockLabel.Text = (IsInherited ? "\ud83d\udd12 继承" : (IsReadOnly ? "\ud83d\udd12 只读" : "可编辑"));
			_inspectButton.Disabled = State == null;
			_initialButton.Disabled = IsReadOnly || State == null || string.IsNullOrWhiteSpace(State.ParentId);
			Color color = stateMachineStateKind switch
			{
				StateMachineStateKind.Atomic => new Color(0.16f, 0.58f, 0.84f), 
				StateMachineStateKind.Compound => new Color(0.48f, 0.34f, 0.82f), 
				StateMachineStateKind.Parallel => new Color(0.18f, 0.7f, 0.52f), 
				StateMachineStateKind.History => new Color(0.84f, 0.54f, 0.18f), 
				_ => new Color(0.35f, 0.4f, 0.5f), 
			};
			_baseColor = (IsInherited ? color.Darkened(0.38f) : color);
			RefreshHierarchyVisuals();
			RefreshSimulationVisuals();
		}
	}

	private static int GetIndependentActionCount(StateMachineStateDefinition state)
	{
		if (state == null)
		{
			return 0;
		}
		int num = 0;
		if (!state.EnterCallbackKey.IsEmpty)
		{
			num++;
		}
		if (!state.ExitCallbackKey.IsEmpty)
		{
			num++;
		}
		if (!state.ProcessCallbackKey.IsEmpty)
		{
			num++;
		}
		if (!state.PhysicsProcessCallbackKey.IsEmpty)
		{
			num++;
		}
		return num;
	}

	private void RefreshHierarchyVisuals(string parentDisplayName = "")
	{
		if (_hierarchyBadge != null && _collapseButton != null && _initialChildBadge != null && _rootStateBadge != null)
		{
			StateMachineStateKind stateMachineStateKind = State?.Kind ?? StateMachineStateKind.Atomic;
			bool flag = (uint)(stateMachineStateKind - 1) <= 1u;
			bool flag2 = flag && HierarchyChildCount > 0;
			_collapseButton.Visible = flag2;
			_collapseButton.Text = (IsHierarchyCollapsed ? "▸" : "▾");
			_collapseButton.TooltipText = (IsHierarchyCollapsed ? $"展开 {HierarchyChildCount} 个直属子状态" : $"折叠并收纳 {HierarchyChildCount} 个直属子状态及其后代");
			_initialChildBadge.Visible = IsInitialChild;
			_rootStateBadge.Visible = IsHierarchyRoot;
			if (_initialButton != null)
			{
				_initialButton.Visible = CanSetAsInitial;
				_initialButton.Disabled = !CanSetAsInitial;
			}
			string text = (string.IsNullOrWhiteSpace(parentDisplayName) ? ParentStableId : parentDisplayName);
			if (flag2)
			{
				_hierarchyBadge.Text = (IsHierarchyCollapsed ? $"◫ {HierarchyChildCount} 子状态已收纳" : $"◩ {HierarchyChildCount} 子状态");
			}
			else if (!string.IsNullOrWhiteSpace(text))
			{
				_hierarchyBadge.Text = "↳ " + text;
			}
			else
			{
				_hierarchyBadge.Text = "◇ 层级根";
			}
		}
	}

	private void RefreshSimulationVisuals()
	{
		if (_simulationBadge != null)
		{
			_simulationBadge.Visible = _simulationActive || _simulationPending;
			_simulationBadge.Text = (_simulationPending ? "⏱ 等待" : (_simulationActive ? "● 激活" : string.Empty));
			_simulationBadge.Modulate = (_simulationPending ? new Color(1f, 0.78f, 0.28f) : new Color(0.42f, 1f, 0.58f));
			base.SelfModulate = (_simulationPending ? _baseColor.Lerp(new Color(1f, 0.72f, 0.18f), 0.46f) : (_simulationActive ? _baseColor.Lightened(0.28f) : (IsHierarchyCollapsed ? _baseColor.Lerp(new Color(0.16f, 0.22f, 0.34f), 0.14f) : _baseColor)));
		}
	}
}
