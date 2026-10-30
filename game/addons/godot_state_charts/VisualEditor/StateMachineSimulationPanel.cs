using System;
using System.Collections.Generic;
using System.Globalization;
using Godot;
using Godot.Collections;

[Tool]
public partial class StateMachineSimulationPanel : PanelContainer
{
	private sealed class RuntimeTraceEntry
	{
		public string Kind { get; init; } = string.Empty;

		public string Summary { get; init; } = string.Empty;

		public string TargetStableId { get; init; } = string.Empty;

		public double ElapsedSeconds { get; init; }

		public StateMachineSnapshot Snapshot { get; init; }
	}

	private sealed class CompletedTransitionTrace
	{
		public string StableId { get; init; } = string.Empty;

		public string Summary { get; init; } = string.Empty;

		public string TargetStableId { get; init; } = string.Empty;

		public double ElapsedSeconds { get; init; }

		public StateMachineSnapshot Snapshot { get; init; }
	}

	private enum ExpressionValueType
	{
		Boolean,
		Integer,
		Number,
		Text
	}

	private const double AutoPublishIntervalSeconds = 0.1;

	private const double SingleFrameSeconds = 1.0 / 60.0;

	private const double CompletedTransitionHighlightSeconds = 1.2;

	private const int MaxRuntimeTraceEntries = 128;

	private static readonly System.Collections.Generic.Dictionary<string, Texture2D> TraceIcons = new System.Collections.Generic.Dictionary<string, Texture2D>(StringComparer.Ordinal);

	private readonly List<StateMachineSnapshot> _backStack = new List<StateMachineSnapshot>();

	private readonly List<string> _transitionHistory = new List<string>();

	private readonly List<string> _transitionTargets = new List<string>();

	private readonly List<RuntimeTraceEntry> _runtimeTrace = new List<RuntimeTraceEntry>();

	private readonly List<CompletedTransitionTrace> _completedTransitionQueue = new List<CompletedTransitionTrace>();

	private readonly System.Collections.Generic.Dictionary<string, StateMachineTransitionDefinition> _transitions = new System.Collections.Generic.Dictionary<string, StateMachineTransitionDefinition>(StringComparer.Ordinal);

	private StateMachineDefinition _definition;

	private IStateMachineController _controller;

	private StateMachineRuntimeDebugTarget _runtimeTarget;

	private StateMachineSnapshot _lastSnapshot;

	private OptionButton _eventPicker;

	private Button _sendEventButton;

	private Button _startButton;

	private Button _resumeButton;

	private Button _pauseButton;

	private Button _stepButton;

	private Button _backButton;

	private Button _resetButton;

	private CheckButton _autoAdvance;

	private Label _status;

	private Label _pendingLabel;

	private ProgressBar _pendingProgress;

	private ItemList _historyList;

	private LineEdit _expressionKey;

	private OptionButton _expressionType;

	private HBoxContainer _expressionTypeSegments;

	private readonly System.Collections.Generic.Dictionary<ExpressionValueType, Button> _expressionTypeButtons = new System.Collections.Generic.Dictionary<ExpressionValueType, Button>();

	private LineEdit _expressionValue;

	private CheckButton _expressionBoolean;

	private Button _setExpressionButton;

	private ItemList _expressionList;

	private readonly List<StringName> _expressionKeys = new List<StringName>();

	private bool _running;

	private bool _ownsController;

	private bool _workbenchActive;

	private bool _completedTraceFlushQueued;

	private double _autoPublishElapsed;

	private double _elapsedSimulationSeconds;

	private string _lastCompletedTransitionStableId = string.Empty;

	private double _lastCompletedTransitionHighlightRemaining;

	public bool IsRunning
	{
		get
		{
			if (_running)
			{
				return _controller?.IsInitialized ?? false;
			}
			return false;
		}
	}

	public bool AutoAdvance => _autoAdvance?.ButtonPressed ?? false;

	public bool IsPaused
	{
		get
		{
			if (IsRunning)
			{
				return !AutoAdvance;
			}
			return false;
		}
	}

	public bool RuntimeAttached => _controller?.IsInitialized ?? false;

	public bool UsesBorrowedRuntime
	{
		get
		{
			if (RuntimeAttached)
			{
				return !_ownsController;
			}
			return false;
		}
	}

	public IStateMachineController BoundRuntimeController => _controller;

	public string RuntimeSourceName => _runtimeTarget?.DisplayName ?? "隔离编译沙盒";

	public int TransitionHistoryCount => _transitionHistory.Count;

	public int RuntimeTraceCount => _runtimeTrace.Count;

	public string LastRuntimeTraceKind
	{
		get
		{
			if (_runtimeTrace.Count != 0)
			{
				List<RuntimeTraceEntry> runtimeTrace = _runtimeTrace;
				return runtimeTrace[runtimeTrace.Count - 1].Kind;
			}
			return string.Empty;
		}
	}

	public long CurrentSnapshotRevision => _lastSnapshot?.Revision ?? (-1);

	public double PendingDelayRemaining => ResolvePendingDelay(_lastSnapshot);

	public StateMachineSnapshot CurrentSnapshot => _lastSnapshot;

	public string LastCompletedTransitionStableId
	{
		get
		{
			if (!(_lastCompletedTransitionHighlightRemaining > 0.0))
			{
				return string.Empty;
			}
			return _lastCompletedTransitionStableId;
		}
	}

	public bool IsQuiescent
	{
		get
		{
			if (!IsRunning)
			{
				return !IsProcessing();
			}
			return false;
		}
	}

	public int ExpressionPropertyCount => (_lastSnapshot?.ExpressionProperties?.Count).GetValueOrDefault();

	public int SnapshotPublishCount { get; private set; }

	public int AutoSnapshotPublishCount { get; private set; }

	public event Action<StateMachineSnapshot> StateChanged;

	public event Action<bool> RunningChanged;

	public event Action<string> NavigateRequested;

	public override void _Ready()
	{
		BuildVisualWorkbench();
		_workbenchActive = IsVisibleInTree();
		SetProcess(enable: false);
		base.VisibilityChanged += UpdateProcessingEligibility;
		RefreshEventChoices();
		RefreshControls();
	}

	public override void _ExitTree()
	{
		StopSimulation();
		base.VisibilityChanged -= UpdateProcessingEligibility;
	}

	public override void _Process(double delta)
	{
		if (!IsRunning || !AutoAdvance || !IsVisibleInTree())
		{
			UpdateProcessingEligibility();
		}
		else
		{
			AdvanceAutoSimulation(Math.Min(Math.Max(delta, 0.0), 0.1));
		}
	}

	public void BindDefinition(StateMachineDefinition definition)
	{
		StopSimulation();
		_definition = definition;
		IndexTransitions();
		RefreshEventChoices();
		_status.Text = ((definition == null) ? "未载入状态机" : ((_runtimeTarget == null) ? "沙盒待命 · 不会运行角色或关卡逻辑" : ("真实运行预览待命 · " + _runtimeTarget.DisplayName)));
		RefreshControls();
	}

	public void BindRuntimeTarget(StateMachineRuntimeDebugTarget target)
	{
		if (_runtimeTarget != target)
		{
			StopSimulation();
			_runtimeTarget = target;
			if (_status != null)
			{
				_status.Text = ((target != null) ? ("真实运行预览待命 · " + target.DisplayName) : ((_definition == null) ? "未载入状态机" : "沙盒待命 · 不会运行角色或关卡逻辑"));
			}
			RefreshControls();
		}
	}

	public void InvalidateRuntimeTarget(string reason)
	{
		if (_runtimeTarget != null)
		{
			StopSimulation();
			SetStatus(string.IsNullOrWhiteSpace(reason) ? "状态机配置已变化，重新启动时会重建真实预览。" : reason, error: false);
		}
	}

	public void SetWorkbenchActive(bool active)
	{
		_workbenchActive = active;
		if (!active)
		{
			StopSimulation();
		}
		else
		{
			UpdateProcessingEligibility();
		}
	}

	public bool StartSimulation()
	{
		StopSimulation();
		if (_definition == null)
		{
			SetStatus("没有可模拟的状态机", error: true);
			return false;
		}
		if (_runtimeTarget != null)
		{
			Func<bool> ensureCurrent = _runtimeTarget.EnsureCurrent;
			if (ensureCurrent == null || !ensureCurrent())
			{
				SetStatus("真实组件运行时无法重建，请先检查组件配置。", error: true);
				return false;
			}
			_controller = _runtimeTarget.ResolveController?.Invoke();
			_ownsController = false;
			IStateMachineController controller = _controller;
			if (controller == null || !controller.IsInitialized)
			{
				SetStatus("真实组件没有可调试的状态机控制器。", error: true);
				_controller = null;
				return false;
			}
		}
		else
		{
			StateMachineController stateMachineController = new StateMachineController();
			if (!stateMachineController.Initialize(_definition))
			{
				SetStatus("编译失败：" + stateMachineController.InitializationError, error: true);
				stateMachineController.Dispose();
				return false;
			}
			_controller = stateMachineController;
			_ownsController = true;
		}
		ConnectControllerSignals();
		_running = true;
		_autoPublishElapsed = 0.0;
		_elapsedSimulationSeconds = 0.0;
		_backStack.Clear();
		_transitionHistory.Clear();
		_transitionTargets.Clear();
		_runtimeTrace.Clear();
		_completedTransitionQueue.Clear();
		RefreshHistoryList();
		if (_ownsController && !_controller.EnterInitialState())
		{
			SetStatus("无法进入初始状态，请先检查入口配置", error: true);
			StopSimulation();
			return false;
		}
		CaptureAndPublish((_runtimeTarget == null) ? "模拟运行中" : ("已连接真实运行时：" + _runtimeTarget.DisplayName), "start", (_runtimeTarget == null) ? "进入沙盒初始状态" : "连接真实组件预览", recordTrace: true);
		this.RunningChanged?.Invoke(obj: true);
		UpdateProcessingEligibility();
		return true;
	}

	public bool SendSimulationEvent(StringName eventName)
	{
		if (!EnsureRunning() || eventName.IsEmpty)
		{
			return false;
		}
		bool flag = ((_runtimeTarget?.SendEvent != null) ? _runtimeTarget.SendEvent(eventName) : _controller.SendEvent(eventName));
		FlushCompletedTransitionTrace();
		CaptureAndPublish(flag ? $"已发送事件：{eventName}" : $"事件未接收：{eventName}", flag ? "event" : "blocked", flag ? $"事件 {eventName}" : $"拒绝事件 {eventName}", recordTrace: true);
		return flag;
	}

	public bool SetExpressionProperty(StringName key, Variant value)
	{
		if (!EnsureRunning() || key.IsEmpty || !StateMachineSnapshot.IsSupportedExpressionVariant(value.VariantType))
		{
			return false;
		}
		_controller.SetExpressionProperty(key, value);
		FlushCompletedTransitionTrace();
		CaptureAndPublish($"表达式属性：{key} = {FormatExpressionValue(value)}", "property", $"{key} = {FormatExpressionValue(value)}", recordTrace: true);
		return true;
	}

	public void AdvanceSimulation(double seconds)
	{
		if (EnsureRunning())
		{
			double num = Math.Max(0.0, seconds);
			SetAutoAdvance(enabled: false);
			if (!((_runtimeTarget?.Step != null) ? _runtimeTarget.Step(num) : StepOwnedController(num)))
			{
				SetStatus("真实组件运行时无法继续单步。", error: true);
				return;
			}
			_elapsedSimulationSeconds += num;
			_lastCompletedTransitionHighlightRemaining = Math.Max(0.0, _lastCompletedTransitionHighlightRemaining - num);
			_autoPublishElapsed = 0.0;
			FlushCompletedTransitionTrace();
			CaptureAndPublish((num > 0.0) ? $"时间推进 {num:0.###} 秒" : "刷新模拟状态", "step", (num > 0.0) ? $"单步 +{num:0.###} 秒" : "刷新状态", recordTrace: true);
		}
	}

	public void AdvanceAutoSimulation(double seconds)
	{
		if (!EnsureRunning())
		{
			return;
		}
		double num = Math.Max(0.0, seconds);
		if (!((_runtimeTarget?.Step != null) ? _runtimeTarget.Step(num) : StepOwnedController(num)))
		{
			PauseSimulation();
			SetStatus("真实组件运行时已经失效，自动计时已暂停。", error: true);
			return;
		}
		_elapsedSimulationSeconds += num;
		_lastCompletedTransitionHighlightRemaining = Math.Max(0.0, _lastCompletedTransitionHighlightRemaining - num);
		_autoPublishElapsed += num;
		if (!(_autoPublishElapsed + 1E-07 < 0.1))
		{
			_autoPublishElapsed = Math.Max(0.0, _autoPublishElapsed - 0.1);
			AutoSnapshotPublishCount++;
			bool recordTrace = _completedTransitionQueue.Count > 0;
			FlushCompletedTransitionTrace();
			CaptureAndPublish((_runtimeTarget == null) ? "模拟运行中" : ("真实运行中 · " + _runtimeTarget.DisplayName), "tick", "自动计时", recordTrace);
		}
	}

	public bool PauseSimulation()
	{
		if (!IsRunning)
		{
			return false;
		}
		SetAutoAdvance(enabled: false);
		AddRuntimeTrace("pause", "暂停自动计时", _lastSnapshot);
		SetStatus("已暂停 · " + RuntimeSourceName, error: false);
		RefreshControls();
		UpdateProcessingEligibility();
		return true;
	}

	public bool ResumeSimulation()
	{
		if (!EnsureRunning())
		{
			return false;
		}
		SetAutoAdvance(enabled: true);
		AddRuntimeTrace("resume", "继续自动计时", _lastSnapshot);
		SetStatus("运行中 · " + RuntimeSourceName, error: false);
		RefreshControls();
		UpdateProcessingEligibility();
		return true;
	}

	public void StepSingleFrame()
	{
		AdvanceSimulation(1.0 / 60.0);
	}

	public bool StepBack()
	{
		if (!IsRunning || _backStack.Count == 0)
		{
			return false;
		}
		List<StateMachineSnapshot> backStack = _backStack;
		StateMachineSnapshot snapshot = backStack[backStack.Count - 1];
		if (!_controller.RestoreSnapshot(snapshot, suppressEntryEffects: true))
		{
			SetStatus("无法回退：状态机结构已变化", error: true);
			return false;
		}
		_backStack.RemoveAt(_backStack.Count - 1);
		if (_transitionHistory.Count > 0)
		{
			_transitionHistory.RemoveAt(_transitionHistory.Count - 1);
		}
		if (_transitionTargets.Count > 0)
		{
			_transitionTargets.RemoveAt(_transitionTargets.Count - 1);
		}
		ClearCompletedTransitionHighlight();
		CaptureAndPublish("已回退到上一个转换前", "rewind", "回退到上一个转换前", recordTrace: true);
		return true;
	}

	public bool ResetSimulation()
	{
		if (_runtimeTarget == null)
		{
			return StartSimulation();
		}
		StopSimulation();
		Func<bool> reset = _runtimeTarget.Reset;
		if (reset == null || !reset())
		{
			SetStatus("真实组件预览重建失败。", error: true);
			return false;
		}
		return StartSimulation();
	}

	public void StopSimulation()
	{
		bool num = IsRunning || _running;
		_running = false;
		_autoPublishElapsed = 0.0;
		SetProcess(enable: false);
		ReleaseController();
		_backStack.Clear();
		_transitionHistory.Clear();
		_transitionTargets.Clear();
		_runtimeTrace.Clear();
		_completedTransitionQueue.Clear();
		_completedTraceFlushQueued = false;
		ClearCompletedTransitionHighlight();
		_lastSnapshot = null;
		RefreshHistoryList();
		RefreshPending(null);
		RefreshExpressionProperties(null);
		this.StateChanged?.Invoke(null);
		if (num)
		{
			this.RunningChanged?.Invoke(obj: false);
		}
		RefreshControls();
	}

	private void BuildVisualWorkbench()
	{
		base.Name = "SimulationPanel";
		base.CustomMinimumSize = new Vector2(340f, 344f);
		base.MouseFilter = MouseFilterEnum.Stop;
		MarginContainer marginContainer = new MarginContainer();
		marginContainer.AddThemeConstantOverride("margin_left", 12);
		marginContainer.AddThemeConstantOverride("margin_top", 10);
		marginContainer.AddThemeConstantOverride("margin_right", 12);
		marginContainer.AddThemeConstantOverride("margin_bottom", 10);
		AddChild(marginContainer, forceReadableName: false, InternalMode.Disabled);
		VBoxContainer vBoxContainer = new VBoxContainer();
		vBoxContainer.AddThemeConstantOverride("separation", 8);
		marginContainer.AddChild(vBoxContainer, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer = new HBoxContainer();
		vBoxContainer.AddChild(hBoxContainer, forceReadableName: false, InternalMode.Disabled);
		hBoxContainer.AddChild(new Label
		{
			Text = "状态机调试台",
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "独立资源使用安全沙盒；组件工作台连接真实预览运行时"
		}, forceReadableName: false, InternalMode.Disabled);
		_startButton = CreateIconButton("SimulationStartButton", "res://addons/ModEditor/Icons/ClassIcon/PlayScene.svg", "启动调试运行时");
		_startButton.Pressed += ToggleSimulation;
		hBoxContainer.AddChild(_startButton, forceReadableName: false, InternalMode.Disabled);
		_resumeButton = CreateIconButton("SimulationResumeButton", "res://addons/ModEditor/Icons/ClassIcon/DebugContinue.svg", "继续自动计时");
		_resumeButton.Pressed += delegate
		{
			ResumeSimulation();
		};
		hBoxContainer.AddChild(_resumeButton, forceReadableName: false, InternalMode.Disabled);
		_pauseButton = CreateIconButton("SimulationPauseButton", "res://addons/ModEditor/Icons/ClassIcon/Pause.svg", "暂停并保留当前运行状态");
		_pauseButton.Pressed += delegate
		{
			PauseSimulation();
		};
		hBoxContainer.AddChild(_pauseButton, forceReadableName: false, InternalMode.Disabled);
		_stepButton = CreateIconButton("SimulationStepButton", "res://addons/ModEditor/Icons/ClassIcon/DebugStep.svg", "暂停状态下单步推进一帧");
		_stepButton.Pressed += StepSingleFrame;
		hBoxContainer.AddChild(_stepButton, forceReadableName: false, InternalMode.Disabled);
		_resetButton = CreateIconButton("SimulationResetButton", "res://addons/ModEditor/Icons/ClassIcon/ZoomReset.svg", "重建真实预览或重新进入沙盒初始状态");
		_resetButton.Pressed += delegate
		{
			ResetSimulation();
		};
		hBoxContainer.AddChild(_resetButton, forceReadableName: false, InternalMode.Disabled);
		_backButton = CreateIconButton("SimulationBackButton", "res://addons/ModEditor/Icons/ClassIcon/PlayBackwards.svg", "回到最近一次转换之前");
		_backButton.Pressed += delegate
		{
			StepBack();
		};
		hBoxContainer.AddChild(_backButton, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer();
		vBoxContainer.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		hFlowContainer.AddChild(new Label
		{
			Text = "事件"
		}, forceReadableName: false, InternalMode.Disabled);
		_eventPicker = new OptionButton
		{
			Name = "SimulationEventPicker",
			CustomMinimumSize = new Vector2(156f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill,
			TooltipText = "来自事件转换的可用事件"
		};
		hFlowContainer.AddChild(_eventPicker, forceReadableName: false, InternalMode.Disabled);
		_sendEventButton = CreateIconButton("SimulationSendEventButton", "res://addons/ModEditor/Icons/ClassIcon/DebugNext.svg", "发送选中的状态事件");
		_sendEventButton.Pressed += SendSelectedEvent;
		hFlowContainer.AddChild(_sendEventButton, forceReadableName: false, InternalMode.Disabled);
		_autoAdvance = new CheckButton
		{
			Name = "SimulationAutoAdvanceSource",
			Text = "自动计时",
			ButtonPressed = false,
			Visible = false
		};
		_autoAdvance.Toggled += delegate
		{
			UpdateProcessingEligibility();
		};
		hFlowContainer.AddChild(_autoAdvance, forceReadableName: false, InternalMode.Disabled);
		BuildExpressionPropertyWorkbench(vBoxContainer);
		_status = new Label
		{
			Text = "沙盒待命",
			AutowrapMode = TextServer.AutowrapMode.WordSmart
		};
		vBoxContainer.AddChild(_status, forceReadableName: false, InternalMode.Disabled);
		HBoxContainer hBoxContainer2 = new HBoxContainer();
		vBoxContainer.AddChild(hBoxContainer2, forceReadableName: false, InternalMode.Disabled);
		_pendingLabel = new Label
		{
			Text = "延迟：无",
			CustomMinimumSize = new Vector2(146f, 0f)
		};
		hBoxContainer2.AddChild(_pendingLabel, forceReadableName: false, InternalMode.Disabled);
		_pendingProgress = new ProgressBar
		{
			MinValue = 0.0,
			MaxValue = 1.0,
			Value = 0.0,
			ShowPercentage = false,
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hBoxContainer2.AddChild(_pendingProgress, forceReadableName: false, InternalMode.Disabled);
		vBoxContainer.AddChild(new Label
		{
			Text = "运行时间线",
			TooltipText = "单击定位状态，双击把运行时恢复到该时刻；最多保留 128 步"
		}, forceReadableName: false, InternalMode.Disabled);
		_historyList = new ItemList
		{
			Name = "RuntimeTraceList",
			CustomMinimumSize = new Vector2(0f, 76f),
			SizeFlagsVertical = SizeFlags.ExpandFill,
			AllowReselect = true
		};
		_historyList.ItemSelected += delegate(long index)
		{
			NavigateToHistoryItem((int)index);
		};
		_historyList.ItemActivated += delegate(long index)
		{
			RestoreRuntimeTrace((int)index);
		};
		vBoxContainer.AddChild(_historyList, forceReadableName: false, InternalMode.Disabled);
	}

	private static Button CreateIconButton(string name, string iconPath, string tooltip)
	{
		return new Button
		{
			Name = name,
			Icon = ResourceLoader.Load<Texture2D>(iconPath, null, ResourceLoader.CacheMode.Reuse),
			TooltipText = tooltip,
			CustomMinimumSize = new Vector2(34f, 30f)
		};
	}

	private void BuildExpressionPropertyWorkbench(VBoxContainer stack)
	{
		stack.AddChild(new Label
		{
			Text = "守卫变量拼图 · 只影响当前沙盒",
			TooltipText = "设置 expressionProperties，立即重新评估自动转换与事件守卫"
		}, forceReadableName: false, InternalMode.Disabled);
		HFlowContainer hFlowContainer = new HFlowContainer
		{
			Name = "ExpressionPropertyWorkbench"
		};
		stack.AddChild(hFlowContainer, forceReadableName: false, InternalMode.Disabled);
		_expressionKey = new LineEdit
		{
			Name = "ExpressionPropertyKey",
			PlaceholderText = "属性键，例如 health",
			CustomMinimumSize = new Vector2(120f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		hFlowContainer.AddChild(_expressionKey, forceReadableName: false, InternalMode.Disabled);
		_expressionType = new OptionButton
		{
			Name = "ExpressionPropertyType",
			TooltipText = "选择守卫值类型",
			CustomMinimumSize = new Vector2(92f, 0f),
			Visible = false
		};
		_expressionType.AddItem("◉ 开关", 0);
		_expressionType.AddItem("# 整数", 1);
		_expressionType.AddItem("0.0 数值", 2);
		_expressionType.AddItem("T 文本", 3);
		_expressionType.ItemSelected += delegate
		{
			RefreshExpressionValueEditor();
		};
		hFlowContainer.AddChild(_expressionType, forceReadableName: false, InternalMode.Disabled);
		_expressionTypeSegments = new HBoxContainer
		{
			Name = "ExpressionPropertyTypeSegments"
		};
		hFlowContainer.AddChild(_expressionTypeSegments, forceReadableName: false, InternalMode.Disabled);
		AddExpressionTypeSegment(ExpressionValueType.Boolean, "ExpressionPropertyTypeBooleanButton", "◉", "开关");
		AddExpressionTypeSegment(ExpressionValueType.Integer, "ExpressionPropertyTypeIntegerButton", "#", "整数");
		AddExpressionTypeSegment(ExpressionValueType.Number, "ExpressionPropertyTypeNumberButton", "0.0", "数值");
		AddExpressionTypeSegment(ExpressionValueType.Text, "ExpressionPropertyTypeTextButton", "T", "文本");
		_expressionValue = new LineEdit
		{
			Name = "ExpressionPropertyValue",
			PlaceholderText = "值",
			CustomMinimumSize = new Vector2(86f, 0f),
			SizeFlagsHorizontal = SizeFlags.ExpandFill
		};
		_expressionValue.TextSubmitted += delegate
		{
			ApplyExpressionPropertyFromWorkbench();
		};
		hFlowContainer.AddChild(_expressionValue, forceReadableName: false, InternalMode.Disabled);
		_expressionBoolean = new CheckButton
		{
			Name = "ExpressionPropertyBoolean",
			Text = "开启",
			ButtonPressed = false
		};
		hFlowContainer.AddChild(_expressionBoolean, forceReadableName: false, InternalMode.Disabled);
		_setExpressionButton = new Button
		{
			Name = "SetExpressionPropertyButton",
			Text = "应用到沙盒",
			TooltipText = "调用 StateMachineController.SetExpressionProperty"
		};
		_setExpressionButton.Pressed += ApplyExpressionPropertyFromWorkbench;
		hFlowContainer.AddChild(_setExpressionButton, forceReadableName: false, InternalMode.Disabled);
		_expressionList = new ItemList
		{
			Name = "ExpressionPropertyList",
			CustomMinimumSize = new Vector2(0f, 54f),
			SelectMode = ItemList.SelectModeEnum.Single
		};
		_expressionList.ItemSelected += delegate(long index)
		{
			SelectExpressionProperty((int)index);
		};
		stack.AddChild(_expressionList, forceReadableName: false, InternalMode.Disabled);
		RefreshExpressionValueEditor();
		RefreshExpressionProperties(null);
	}

	private void AddExpressionTypeSegment(ExpressionValueType valueType, string nodeName, string glyph, string tooltip)
	{
		Button button = new Button
		{
			Name = nodeName,
			Text = glyph,
			TooltipText = tooltip,
			ToggleMode = true,
			CustomMinimumSize = new Vector2(42f, 0f)
		};
		button.Pressed += delegate
		{
			SelectExpressionValueType(valueType);
		};
		_expressionTypeButtons[valueType] = button;
		_expressionTypeSegments.AddChild(button, forceReadableName: false, InternalMode.Disabled);
	}

	private void SelectExpressionValueType(ExpressionValueType valueType)
	{
		int num = _expressionType?.GetItemIndex((int)valueType) ?? (-1);
		if (num >= 0)
		{
			_expressionType.Select(num);
		}
		RefreshExpressionValueEditor();
	}

	private void ApplyExpressionPropertyFromWorkbench()
	{
		string text = _expressionKey?.Text?.StripEdges() ?? string.Empty;
		if (string.IsNullOrWhiteSpace(text))
		{
			SetStatus("表达式属性键不能为空", error: true);
			return;
		}
		ExpressionValueType selectedId = (ExpressionValueType)_expressionType.GetSelectedId();
		string from = _expressionValue?.Text ?? string.Empty;
		Variant value;
		switch (selectedId)
		{
		case ExpressionValueType.Boolean:
			value = Variant.From<bool>(_expressionBoolean.ButtonPressed);
			break;
		case ExpressionValueType.Integer:
		{
			if (!long.TryParse(from, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
			{
				SetStatus("整数值格式不正确", error: true);
				return;
			}
			value = Variant.From(in result2);
			break;
		}
		case ExpressionValueType.Number:
		{
			if (!double.TryParse(from, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				SetStatus("数值格式不正确", error: true);
				return;
			}
			value = Variant.From(in result);
			break;
		}
		default:
			value = Variant.From(in from);
			break;
		}
		SetExpressionProperty(new StringName(text), value);
	}

	private void RefreshExpressionValueEditor()
	{
		if (_expressionType == null || _expressionValue == null || _expressionBoolean == null)
		{
			return;
		}
		bool flag = _expressionType.GetSelectedId() == 0;
		_expressionValue.Visible = !flag;
		_expressionBoolean.Visible = flag;
		ExpressionValueType selectedId = (ExpressionValueType)_expressionType.GetSelectedId();
		foreach (KeyValuePair<ExpressionValueType, Button> expressionTypeButton in _expressionTypeButtons)
		{
			expressionTypeButton.Value.ButtonPressed = expressionTypeButton.Key == selectedId;
		}
	}

	private void RefreshExpressionProperties(StateMachineSnapshot snapshot)
	{
		if (_expressionList == null)
		{
			return;
		}
		string text = ((_expressionList.GetSelectedItems().Length != 0 && _expressionList.GetSelectedItems()[0] < _expressionKeys.Count) ? _expressionKeys[_expressionList.GetSelectedItems()[0]].ToString() : (_expressionKey?.Text ?? string.Empty));
		_expressionList.Clear();
		_expressionKeys.Clear();
		List<StringName> list = new List<StringName>();
		if (snapshot?.ExpressionProperties != null)
		{
			foreach (StringName key in snapshot.ExpressionProperties.Keys)
			{
				list.Add(key);
			}
		}
		list.Sort((StringName left, StringName right) => string.Compare(left.ToString(), right.ToString(), StringComparison.Ordinal));
		int num = -1;
		foreach (StringName item in list)
		{
			Variant value = snapshot.ExpressionProperties[item];
			_expressionKeys.Add(item);
			_expressionList.AddItem($"{item}  ·  {ExpressionTypeGlyph(value)}  {FormatExpressionValue(value)}");
			if (item.ToString() == text)
			{
				num = _expressionKeys.Count - 1;
			}
		}
		if (_expressionKeys.Count == 0)
		{
			_expressionList.AddItem("尚未设置守卫变量");
			_expressionList.SetItemDisabled(0, disabled: true);
		}
		else if (num >= 0)
		{
			_expressionList.Select(num);
		}
	}

	private void SelectExpressionProperty(int index)
	{
		if (_lastSnapshot?.ExpressionProperties == null || (uint)index >= (uint)_expressionKeys.Count)
		{
			return;
		}
		StringName stringName = _expressionKeys[index];
		Variant value = _lastSnapshot.ExpressionProperties[stringName];
		_expressionKey.Text = stringName.ToString();
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num > 2uL)
		{
			goto IL_0089;
		}
		switch ((int)num)
		{
		case 0:
			break;
		case 1:
			goto IL_0081;
		case 2:
			goto IL_0085;
		default:
			goto IL_0089;
		}
		ExpressionValueType expressionValueType = ExpressionValueType.Boolean;
		goto IL_008b;
		IL_008b:
		ExpressionValueType id = expressionValueType;
		_expressionType.Select(Math.Max(0, _expressionType.GetItemIndex((int)id)));
		_expressionBoolean.ButtonPressed = value.VariantType == Variant.Type.Bool && value.AsBool();
		_expressionValue.Text = FormatExpressionValue(value);
		RefreshExpressionValueEditor();
		return;
		IL_0089:
		expressionValueType = ExpressionValueType.Text;
		goto IL_008b;
		IL_0085:
		expressionValueType = ExpressionValueType.Number;
		goto IL_008b;
		IL_0081:
		expressionValueType = ExpressionValueType.Integer;
		goto IL_008b;
	}

	private static string ExpressionTypeGlyph(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		Variant.Type num = variantType - 1;
		if ((ulong)num <= 2uL)
		{
			switch ((int)num)
			{
			case 0:
				return "◉";
			case 1:
				return "#";
			case 2:
				return "0.0";
			}
		}
		return "T";
	}

	private static string FormatExpressionValue(Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 3uL)
		{
			switch ((int)variantType)
			{
			case 1:
				return value.AsBool() ? "true" : "false";
			case 2:
				return value.AsInt64().ToString(CultureInfo.InvariantCulture);
			case 3:
				return value.AsDouble().ToString("0.###", CultureInfo.InvariantCulture);
			case 0:
				return "null";
			}
		}
		return value.AsString();
	}

	private void SendSelectedEvent()
	{
		if (_eventPicker != null && _eventPicker.ItemCount != 0)
		{
			SendSimulationEvent(new StringName(_eventPicker.GetItemText(_eventPicker.Selected)));
		}
	}

	private bool EnsureRunning()
	{
		if (!IsRunning)
		{
			return StartSimulation();
		}
		return true;
	}

	private void OnTransitionTaken(CompiledStateMachineTransition transition)
	{
		StateMachineSnapshot stateMachineSnapshot = _controller?.CaptureSnapshot();
		if (stateMachineSnapshot != null)
		{
			_backStack.Add(stateMachineSnapshot);
		}
		string item = FormatTransition(transition.StableId);
		_transitionHistory.Add(item);
		_transitionTargets.Add(ResolveTransitionTarget(transition.StableId));
	}

	private void OnTransitionCompleted(CompiledStateMachineTransition transition)
	{
		StateMachineSnapshot stateMachineSnapshot = _controller?.CaptureSnapshot();
		if (stateMachineSnapshot != null)
		{
			_completedTransitionQueue.Add(new CompletedTransitionTrace
			{
				StableId = (transition.StableId ?? string.Empty),
				Summary = FormatTransition(transition.StableId),
				TargetStableId = ResolveTransitionTarget(transition.StableId),
				ElapsedSeconds = _elapsedSimulationSeconds,
				Snapshot = CloneSnapshot(stateMachineSnapshot)
			});
			if (!_completedTraceFlushQueued)
			{
				_completedTraceFlushQueued = true;
				CallDeferred("FlushCompletedTransitionTraceDeferred");
			}
		}
	}

	private void FlushCompletedTransitionTraceDeferred()
	{
		_completedTraceFlushQueued = false;
		if (!IsRunning)
		{
			_completedTransitionQueue.Clear();
			return;
		}
		FlushCompletedTransitionTrace();
		StateMachineSnapshot stateMachineSnapshot = _controller?.CaptureSnapshot();
		if (stateMachineSnapshot != null)
		{
			_lastSnapshot = stateMachineSnapshot;
			RefreshPending(stateMachineSnapshot);
			RefreshExpressionProperties(stateMachineSnapshot);
			RefreshControls();
			this.StateChanged?.Invoke(stateMachineSnapshot);
		}
	}

	private void FlushCompletedTransitionTrace()
	{
		if (_completedTransitionQueue.Count == 0)
		{
			return;
		}
		foreach (CompletedTransitionTrace item in _completedTransitionQueue)
		{
			_lastCompletedTransitionStableId = item.StableId;
			_lastCompletedTransitionHighlightRemaining = 1.2;
			AddRuntimeTrace("transition", item.Summary, item.Snapshot, item.TargetStableId, item.ElapsedSeconds);
		}
		_completedTransitionQueue.Clear();
	}

	private void OnRuntimeDiagnostic(string code)
	{
		if (!string.IsNullOrWhiteSpace(code))
		{
			SetStatus("运行诊断：" + code, error: true);
		}
	}

	private void CaptureAndPublish(string status, string traceKind, string traceSummary, bool recordTrace)
	{
		SnapshotPublishCount++;
		_lastSnapshot = _controller?.CaptureSnapshot();
		if (recordTrace)
		{
			AddRuntimeTrace(traceKind, traceSummary, _lastSnapshot);
		}
		SetStatus(status, error: false);
		RefreshPending(_lastSnapshot);
		RefreshExpressionProperties(_lastSnapshot);
		RefreshControls();
		this.StateChanged?.Invoke(_lastSnapshot);
	}

	private void RefreshPending(StateMachineSnapshot snapshot)
	{
		double num = ResolvePendingDelay(snapshot);
		string text = ResolvePendingTransitionId(snapshot, num);
		if (num <= 0.0 || string.IsNullOrWhiteSpace(text))
		{
			_pendingLabel.Text = "延迟：无";
			_pendingProgress.MaxValue = 1.0;
			_pendingProgress.Value = 0.0;
			return;
		}
		StateMachineTransitionDefinition value;
		double num2 = (_transitions.TryGetValue(text, out value) ? Math.Max(num, value.DelaySeconds) : num);
		_pendingLabel.Text = $"延迟：{num:0.###} 秒";
		_pendingProgress.MaxValue = Math.Max(num2, 0.001);
		_pendingProgress.Value = Math.Clamp(num2 - num, 0.0, num2);
	}

	private static double ResolvePendingDelay(StateMachineSnapshot snapshot)
	{
		if (snapshot?.PendingDelayRemaining == null || snapshot.PendingDelayRemaining.Count == 0)
		{
			return 0.0;
		}
		double num = 1.0 / 0.0;
		foreach (string key in snapshot.PendingDelayRemaining.Keys)
		{
			num = Math.Min(num, snapshot.PendingDelayRemaining[key]);
		}
		if (!double.IsPositiveInfinity(num))
		{
			return Math.Max(0.0, num);
		}
		return 0.0;
	}

	private static string ResolvePendingTransitionId(StateMachineSnapshot snapshot, double remaining)
	{
		if (snapshot?.PendingDelayRemaining == null)
		{
			return string.Empty;
		}
		foreach (string key in snapshot.PendingDelayRemaining.Keys)
		{
			if (Math.Abs(snapshot.PendingDelayRemaining[key] - remaining) < 1E-06)
			{
				return key;
			}
		}
		return string.Empty;
	}

	private void IndexTransitions()
	{
		_transitions.Clear();
		IndexTransitionsRecursive(_definition, new HashSet<ulong>());
	}

	private void IndexTransitionsRecursive(StateMachineDefinition definition, HashSet<ulong> visited)
	{
		if (definition == null || !visited.Add(definition.GetInstanceId()))
		{
			return;
		}
		IndexTransitionsRecursive(definition.BaseDefinition, visited);
		foreach (StateMachineTransitionDefinition transition in definition.Transitions)
		{
			if (transition != null && !string.IsNullOrWhiteSpace(transition.StableId))
			{
				_transitions[transition.StableId] = transition;
			}
		}
	}

	private void RefreshEventChoices()
	{
		if (_eventPicker == null)
		{
			return;
		}
		string text = ((_eventPicker.ItemCount > 0) ? _eventPicker.GetItemText(_eventPicker.Selected) : string.Empty);
		_eventPicker.Clear();
		SortedSet<string> sortedSet = new SortedSet<string>(StringComparer.Ordinal);
		foreach (StateMachineTransitionDefinition value in _transitions.Values)
		{
			if (value.TriggerKind == StateMachineTriggerKind.Event && !value.EventName.IsEmpty)
			{
				sortedSet.Add(value.EventName.ToString());
			}
		}
		int idx = 0;
		foreach (string item in sortedSet)
		{
			_eventPicker.AddItem(item);
			if (item == text)
			{
				idx = _eventPicker.ItemCount - 1;
			}
		}
		if (_eventPicker.ItemCount > 0)
		{
			_eventPicker.Select(idx);
		}
		RefreshControls();
	}

	private string FormatTransition(string stableId)
	{
		if (!_transitions.TryGetValue(stableId ?? string.Empty, out var value))
		{
			return stableId ?? "转换";
		}
		string value2 = value.TriggerKind switch
		{
			StateMachineTriggerKind.Event => value.EventName.IsEmpty ? "事件" : value.EventName.ToString(), 
			StateMachineTriggerKind.Automatic => "自动", 
			StateMachineTriggerKind.Delay => $"延迟 {value.DelaySeconds:0.###}s", 
			_ => value.TriggerKind.ToString(), 
		};
		return $"{value.SourceStateId}  →  {value.TargetStateId}  [{value2}]";
	}

	private string ResolveTransitionTarget(string stableId)
	{
		if (!_transitions.TryGetValue(stableId ?? string.Empty, out var value))
		{
			return string.Empty;
		}
		return value.TargetStateId ?? string.Empty;
	}

	private void RefreshHistoryList()
	{
		if (_historyList == null)
		{
			return;
		}
		_historyList.Clear();
		if (_runtimeTrace.Count == 0)
		{
			_historyList.AddItem("尚无运行轨迹");
			_historyList.SetItemDisabled(0, disabled: true);
			return;
		}
		for (int i = 0; i < _runtimeTrace.Count; i++)
		{
			AppendRuntimeTraceItem(_runtimeTrace[i], i, select: false);
		}
		_historyList.Select(_runtimeTrace.Count - 1);
		_historyList.EnsureCurrentIsVisible();
	}

	private void AppendRuntimeTraceItem(RuntimeTraceEntry entry, int index, bool select)
	{
		if (_historyList != null && entry != null)
		{
			string text = ResolvePrimaryActiveState(entry.Snapshot);
			string value = (string.IsNullOrWhiteSpace(text) ? string.Empty : (" · " + text));
			_historyList.AddItem($"{index + 1:00}  {entry.ElapsedSeconds,6:0.000}s  {entry.Summary}{value}", ResolveTraceIcon(entry.Kind));
			int idx = _historyList.ItemCount - 1;
			_historyList.SetItemTooltip(idx, $"类型：{entry.Kind}\n快照版本：{entry.Snapshot?.Revision ?? (-1)}\n双击恢复到这一时刻");
			if (select)
			{
				_historyList.Select(idx);
				_historyList.EnsureCurrentIsVisible();
			}
		}
	}

	private void NavigateToHistoryItem(int index)
	{
		if ((uint)index < (uint)_runtimeTrace.Count)
		{
			string text = _runtimeTrace[index].TargetStableId;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = ResolvePrimaryActiveState(_runtimeTrace[index].Snapshot);
			}
			if (!string.IsNullOrWhiteSpace(text))
			{
				this.NavigateRequested?.Invoke(text);
			}
		}
	}

	public bool RestoreRuntimeTrace(int index)
	{
		if (!IsRunning || (uint)index >= (uint)_runtimeTrace.Count)
		{
			return false;
		}
		RuntimeTraceEntry runtimeTraceEntry = _runtimeTrace[index];
		if (runtimeTraceEntry.Snapshot == null || !_controller.RestoreSnapshot(runtimeTraceEntry.Snapshot, suppressEntryEffects: true))
		{
			SetStatus("无法恢复该轨迹：运行时结构已经变化。", error: true);
			return false;
		}
		SetAutoAdvance(enabled: false);
		_elapsedSimulationSeconds = runtimeTraceEntry.ElapsedSeconds;
		_backStack.Clear();
		_transitionHistory.Clear();
		_transitionTargets.Clear();
		_completedTransitionQueue.Clear();
		ClearCompletedTransitionHighlight();
		CaptureAndPublish($"已恢复到轨迹 {index + 1}", "rewind", $"恢复轨迹 {index + 1}", recordTrace: true);
		return true;
	}

	private void AddRuntimeTrace(string kind, string summary, StateMachineSnapshot snapshot, string targetStableId = null, double? elapsedSeconds = null)
	{
		if (snapshot == null)
		{
			return;
		}
		RuntimeTraceEntry runtimeTraceEntry = new RuntimeTraceEntry
		{
			Kind = (kind ?? string.Empty),
			Summary = (summary ?? string.Empty),
			TargetStableId = (string.IsNullOrWhiteSpace(targetStableId) ? ResolvePrimaryActiveState(snapshot) : targetStableId),
			ElapsedSeconds = (elapsedSeconds ?? _elapsedSimulationSeconds),
			Snapshot = CloneSnapshot(snapshot)
		};
		_runtimeTrace.Add(runtimeTraceEntry);
		bool flag = false;
		if (_runtimeTrace.Count > 128)
		{
			_runtimeTrace.RemoveAt(0);
			flag = true;
		}
		if (flag)
		{
			RefreshHistoryList();
			return;
		}
		if (_runtimeTrace.Count == 1)
		{
			_historyList?.Clear();
		}
		AppendRuntimeTraceItem(runtimeTraceEntry, _runtimeTrace.Count - 1, select: true);
	}

	private static StateMachineSnapshot CloneSnapshot(StateMachineSnapshot snapshot)
	{
		return (snapshot?.Duplicate(deep: true) as StateMachineSnapshot) ?? snapshot;
	}

	private void ClearCompletedTransitionHighlight()
	{
		_lastCompletedTransitionStableId = string.Empty;
		_lastCompletedTransitionHighlightRemaining = 0.0;
	}

	private static string ResolvePrimaryActiveState(StateMachineSnapshot snapshot)
	{
		if (snapshot?.ActiveStateIds == null || snapshot.ActiveStateIds.Count == 0)
		{
			return string.Empty;
		}
		Array<string> activeStateIds = snapshot.ActiveStateIds;
		return activeStateIds[activeStateIds.Count - 1] ?? string.Empty;
	}

	private static Texture2D ResolveTraceIcon(string kind)
	{
		string text = kind switch
		{
			"start" => "PlayScene", 
			"transition" => "DebugNext", 
			"event" => "KeyNext", 
			"blocked" => "DebugSkipBreakpointsOn", 
			"property" => "Edit", 
			"pause" => "Pause", 
			"resume" => "DebugContinue", 
			"step" => "DebugStep", 
			"rewind" => "PlayBackwards", 
			_ => "Time", 
		};
		if (!TraceIcons.TryGetValue(text, out var value) || !GodotObject.IsInstanceValid(value))
		{
			value = ResourceLoader.Load<Texture2D>("res://addons/ModEditor/Icons/ClassIcon/" + text + ".svg", null, ResourceLoader.CacheMode.Reuse);
			TraceIcons[text] = value;
		}
		return value;
	}

	private void RefreshControls()
	{
		if (_startButton == null)
		{
			return;
		}
		_startButton.Icon = ResourceLoader.Load<Texture2D>(IsRunning ? "res://addons/ModEditor/Icons/ClassIcon/Stop.svg" : "res://addons/ModEditor/Icons/ClassIcon/PlayScene.svg", null, ResourceLoader.CacheMode.Reuse);
		_startButton.TooltipText = (IsRunning ? "结束当前调试运行时" : "启动调试运行时");
		_startButton.Disabled = _definition == null;
		_resetButton.Disabled = _definition == null;
		_resumeButton.Disabled = !IsRunning || AutoAdvance;
		_pauseButton.Disabled = !IsRunning || !AutoAdvance;
		_stepButton.Disabled = !IsRunning;
		_backButton.Disabled = !IsRunning || _backStack.Count == 0;
		_sendEventButton.Disabled = _eventPicker.ItemCount == 0 || _definition == null;
		bool flag = !IsRunning;
		if (_expressionKey != null)
		{
			_expressionKey.Editable = !flag;
		}
		if (_expressionType != null)
		{
			_expressionType.Disabled = flag;
		}
		foreach (Button value in _expressionTypeButtons.Values)
		{
			value.Disabled = flag;
		}
		if (_expressionValue != null)
		{
			_expressionValue.Editable = !flag;
		}
		if (_expressionBoolean != null)
		{
			_expressionBoolean.Disabled = flag;
		}
		if (_setExpressionButton != null)
		{
			_setExpressionButton.Disabled = flag;
		}
	}

	private void ToggleSimulation()
	{
		if (IsRunning)
		{
			StopSimulation();
		}
		else
		{
			StartSimulation();
		}
	}

	private void SetAutoAdvance(bool enabled)
	{
		if (_autoAdvance != null)
		{
			_autoAdvance.SetPressedNoSignal(enabled);
			UpdateProcessingEligibility();
		}
	}

	private bool StepOwnedController(double seconds)
	{
		IStateMachineController controller = _controller;
		if (controller == null || !controller.IsInitialized)
		{
			return false;
		}
		_controller.TickProcess(seconds);
		_controller.TickPhysics(seconds);
		return true;
	}

	private void SetStatus(string text, bool error)
	{
		if (_status != null)
		{
			_status.Text = text ?? string.Empty;
			_status.Modulate = (error ? new Color(1f, 0.46f, 0.4f) : new Color(0.72f, 0.9f, 0.78f));
		}
	}

	private void UpdateProcessingEligibility()
	{
		SetProcess(_workbenchActive && IsRunning && AutoAdvance && IsVisibleInTree());
	}

	private void ConnectControllerSignals()
	{
		if (_controller != null)
		{
			_controller.TransitionTaken += OnTransitionTaken;
			_controller.TransitionCompleted += OnTransitionCompleted;
			_controller.Diagnostic += OnRuntimeDiagnostic;
		}
	}

	private void ReleaseController()
	{
		if (_controller != null)
		{
			_controller.TransitionTaken -= OnTransitionTaken;
			_controller.TransitionCompleted -= OnTransitionCompleted;
			_controller.Diagnostic -= OnRuntimeDiagnostic;
			if (_ownsController)
			{
				_controller.Dispose();
			}
			_controller = null;
			_ownsController = false;
		}
	}
}
