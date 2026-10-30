using System;
using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
public partial class StateChartState : Node
{
	public delegate void StateEnteredEventHandler();

	public delegate void StateExitedEventHandler();

	public delegate void EventReceivedEventHandler(StringName eventName);

	public delegate void StateProcessingEventHandler(double delta);

	public delegate void StatePhysicsProcessingEventHandler(double delta);

	public delegate void StateSteppedEventHandler();

	public delegate void StateInputEventHandler(InputEvent event_);

	public delegate void StateUnhandledInputEventHandler(InputEvent event_);

	public delegate void TransitionPendingEventHandler(double initialDelay, double remainingDelay);

	private StatePhysicsProcessingEventHandler _onStatePhysicsProcessing;

	public bool _stateActive;

	public static bool UsePhysicsBatch = true;

	private bool _usingPhysicsBatch;

	private StatePhysicsProcessingEventHandler _statePhysicsCallbackCacheSource;

	private Delegate[] _statePhysicsCallbackCache;

	private string[] _statePhysicsCallbackMetricNames;

	private Transition _pendingTransition;

	private double _pendingTransitionRemainingDelay;

	private double _pendingTransitionInitialDelay;

	private List<Transition> _transitions = new List<Transition>();

	public StateChart _chart;

	public StateMachineProcessFlags ObservedProcessFlags
	{
		get
		{
			StateMachineProcessFlags stateMachineProcessFlags = StateMachineProcessFlags.None;
			if (this.OnStateProcessing != null)
			{
				stateMachineProcessFlags |= StateMachineProcessFlags.Process;
			}
			if (_onStatePhysicsProcessing != null)
			{
				stateMachineProcessFlags |= StateMachineProcessFlags.PhysicsProcess;
			}
			if (this.OnStateInput != null)
			{
				stateMachineProcessFlags |= StateMachineProcessFlags.Input;
			}
			if (this.OnStateUnhandledInput != null)
			{
				stateMachineProcessFlags |= StateMachineProcessFlags.UnhandledInput;
			}
			return stateMachineProcessFlags;
		}
	}

	public bool active => _stateActive;

	internal bool IsPhysicsBatchDispatchActive
	{
		get
		{
			if (_usingPhysicsBatch)
			{
				return _stateActive;
			}
			return false;
		}
	}

	internal StatePhysicsProcessingEventHandler PhysicsBatchCallback => _onStatePhysicsProcessing;

	public event StateEnteredEventHandler OnStateEntered;

	public event StateExitedEventHandler OnStateExited;

	public event EventReceivedEventHandler OnEventReceived;

	public event StateProcessingEventHandler OnStateProcessing;

	public event StatePhysicsProcessingEventHandler OnStatePhysicsProcessing
	{
		add
		{
			_onStatePhysicsProcessing = (StatePhysicsProcessingEventHandler)Delegate.Combine(_onStatePhysicsProcessing, value);
			StateChartStatePhysicsBatch.UpdateCallback(this, _onStatePhysicsProcessing);
		}
		remove
		{
			_onStatePhysicsProcessing = (StatePhysicsProcessingEventHandler)Delegate.Remove(_onStatePhysicsProcessing, value);
			StateChartStatePhysicsBatch.UpdateCallback(this, _onStatePhysicsProcessing);
		}
	}

	public event StateSteppedEventHandler OnStateStepped;

	public event StateInputEventHandler OnStateInput;

	public event StateUnhandledInputEventHandler OnStateUnhandledInput;

	public event TransitionPendingEventHandler OnTransitionPending;

	protected void RaiseEventReceived(StringName event_)
	{
		this.OnEventReceived?.Invoke(event_);
	}

	public override void _Ready()
	{
		if (!Engine.IsEditorHint())
		{
			_chart = _FindChart(GetParent());
		}
	}

	private StateChart _FindChart(Node parent)
	{
		while (parent != null)
		{
			if (parent is StateChart result)
			{
				return result;
			}
			parent = parent.GetParent();
		}
		return null;
	}

	public void _RunTransition(Transition transition, bool immediately = false)
	{
		double num = transition.EvaluateDelay();
		if (!immediately && num > 0.0)
		{
			_QueueTransition(transition, num);
		}
		else
		{
			_chart._RunTransition(transition, this);
		}
	}

	public virtual void _StateInit()
	{
		base.ProcessMode = ProcessModeEnum.Disabled;
		_stateActive = false;
		_ToggleProcessing(isActive: false);
		_transitions.Clear();
		foreach (Node child in GetChildren())
		{
			if (child is Transition item)
			{
				_transitions.Add(item);
			}
		}
	}

	public virtual void _StateEnter(StateChartState transitionTarget)
	{
		_stateActive = true;
		base.ProcessMode = ProcessModeEnum.Inherit;
		_ToggleProcessing(isActive: true);
		this.OnStateEntered?.Invoke();
		_ProcessTransitions(StateChart.TriggerType.StateEnter);
	}

	public virtual void _StateExit()
	{
		_pendingTransition = null;
		_pendingTransitionRemainingDelay = 0.0;
		_pendingTransitionInitialDelay = 0.0;
		_stateActive = false;
		base.ProcessMode = ProcessModeEnum.Disabled;
		_ToggleProcessing(isActive: false);
		this.OnStateExited?.Invoke();
	}

	public void StateSave(SavedState savedState, int childLevels = -1)
	{
		_StateSave(savedState, childLevels);
	}

	public void StateRestore(SavedState savedState, int childLevels = -1)
	{
		_StateRestore(savedState, childLevels);
	}

	public virtual void _StateSave(SavedState savedState, int childLevels = -1)
	{
		if (!active)
		{
			GD.PushError("_StateSave should only be called if the state is active.");
			return;
		}
		SavedState savedState2 = new SavedState();
		savedState2.pendingTransitionName = ((_pendingTransition != null) ? _pendingTransition.GetPath() : new NodePath());
		savedState2.pendingTransitionRemainingDelay = (float)_pendingTransitionRemainingDelay;
		savedState2.pendingTransitionInitialDelay = (float)_pendingTransitionInitialDelay;
		savedState.AddSubstate(this, savedState2);
		int num;
		switch (childLevels)
		{
		case 0:
			return;
		default:
			num = childLevels - 1;
			break;
		case -1:
			num = -1;
			break;
		}
		int childLevels2 = num;
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState { active: not false } stateChartState)
			{
				stateChartState._StateSave(savedState2, childLevels2);
			}
		}
	}

	public virtual void _StateRestore(SavedState savedState, int childLevels = -1)
	{
		SavedState substateOrNull = savedState.GetSubstateOrNull(this);
		if (substateOrNull == null)
		{
			if (active)
			{
				_StateExit();
			}
			return;
		}
		if (!active)
		{
			_StateEnter(null);
		}
		_pendingTransition = GetNodeOrNull<Transition>(substateOrNull.pendingTransitionName);
		_pendingTransitionRemainingDelay = substateOrNull.pendingTransitionRemainingDelay;
		_pendingTransitionInitialDelay = substateOrNull.pendingTransitionInitialDelay;
		int num;
		switch (childLevels)
		{
		case 0:
			return;
		default:
			num = childLevels - 1;
			break;
		case -1:
			num = -1;
			break;
		}
		int childLevels2 = num;
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState stateChartState)
			{
				stateChartState._StateRestore(substateOrNull, childLevels2);
			}
		}
	}

	public override void _Process(double delta)
	{
		if (Engine.IsEditorHint())
		{
			return;
		}
		this.OnStateProcessing?.Invoke(delta);
		if (_pendingTransition != null)
		{
			_pendingTransitionRemainingDelay -= delta;
			this.OnTransitionPending?.Invoke(_pendingTransition.delaySeconds, Mathf.Max(0.0, _pendingTransitionRemainingDelay));
			if (_pendingTransitionRemainingDelay <= 0.0)
			{
				Transition pendingTransition = _pendingTransition;
				_pendingTransition = null;
				_pendingTransitionRemainingDelay = 0.0;
				_chart._RunTransition(pendingTransition, this);
			}
		}
	}

	public virtual void _HandleTransition(Transition transition, StateChartState source)
	{
		GD.PushError(string.Concat("State ", base.Name, " cannot handle transitions."));
	}

	public override void _PhysicsProcess(double delta)
	{
		if (!UsePhysicsBatch || !_usingPhysicsBatch)
		{
			BatchPhysicsProcess(delta);
		}
	}

	public void BatchPhysicsProcess(double delta)
	{
		if (!Engine.IsEditorHint() && active)
		{
			BatchPhysicsProcessValidated(delta);
		}
	}

	internal void BatchPhysicsProcessValidated(double delta)
	{
		InvokeStatePhysicsProcessing(delta);
	}

	private void InvokeStatePhysicsProcessing(double delta)
	{
		StatePhysicsProcessingEventHandler onStatePhysicsProcessing = _onStatePhysicsProcessing;
		if (onStatePhysicsProcessing == null)
		{
			return;
		}
		if (!TowerDefensePerfProfiler.Enabled || !TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			onStatePhysicsProcessing(delta);
			return;
		}
		EnsureStatePhysicsCallbackCache(onStatePhysicsProcessing);
		for (int i = 0; i < _statePhysicsCallbackCache.Length; i++)
		{
			StatePhysicsProcessingEventHandler statePhysicsProcessingEventHandler = (StatePhysicsProcessingEventHandler)_statePhysicsCallbackCache[i];
			string name = _statePhysicsCallbackMetricNames[i];
			long startTicks = TowerDefensePerfProfiler.Begin();
			try
			{
				statePhysicsProcessingEventHandler(delta);
			}
			finally
			{
				TowerDefensePerfProfiler.End(name, startTicks);
			}
		}
	}

	private void EnsureStatePhysicsCallbackCache(StatePhysicsProcessingEventHandler handler)
	{
		if (_statePhysicsCallbackCacheSource != handler || _statePhysicsCallbackCache == null || _statePhysicsCallbackMetricNames == null)
		{
			_statePhysicsCallbackCacheSource = handler;
			_statePhysicsCallbackCache = handler.GetInvocationList();
			_statePhysicsCallbackMetricNames = new string[_statePhysicsCallbackCache.Length];
			for (int i = 0; i < _statePhysicsCallbackCache.Length; i++)
			{
				_statePhysicsCallbackMetricNames[i] = TowerDefensePerfProfiler.GetCallbackMetricName("statePhysics", _statePhysicsCallbackCache[i]);
			}
		}
	}

	public virtual void _StateStep()
	{
		this.OnStateStepped?.Invoke();
	}

	public override void _Input(InputEvent event_)
	{
		this.OnStateInput?.Invoke(event_);
	}

	public override void _UnhandledInput(InputEvent event_)
	{
		this.OnStateUnhandledInput?.Invoke(event_);
	}

	public virtual bool _ProcessTransitions(StateChart.TriggerType triggerType, StringName event_ = null)
	{
		if (!active)
		{
			return false;
		}
		if (triggerType == StateChart.TriggerType.Event)
		{
			RaiseEventReceived(event_);
		}
		foreach (Transition transition in _transitions)
		{
			if (transition.IsTriggeredBy(triggerType) && (event_ == null || event_ == (StringName)"" || transition.@event == event_) && transition.EvaluateGuard())
			{
				if (transition != _pendingTransition)
				{
					_RunTransition(transition);
				}
				return true;
			}
		}
		return false;
	}

	private void _QueueTransition(Transition transition, double initialDelay)
	{
		_pendingTransition = transition;
		_pendingTransitionInitialDelay = initialDelay;
		_pendingTransitionRemainingDelay = initialDelay;
		SetProcess(enable: true);
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>();
		Node parent = GetParent();
		bool flag = false;
		while (GodotObject.IsInstanceValid(parent))
		{
			if (parent is StateChart)
			{
				flag = true;
				break;
			}
			parent = parent.GetParent();
		}
		if (!flag)
		{
			list.Add("State is not a child of a StateChart. This will not work.");
		}
		return list.ToArray();
	}

	protected void _ToggleProcessing(bool isActive)
	{
		SetProcess(isActive && this.OnStateProcessing != null);
		bool flag = isActive && _onStatePhysicsProcessing != null;
		if (UsePhysicsBatch && flag)
		{
			_usingPhysicsBatch = StateChartStatePhysicsBatch.Register(this);
			SetPhysicsProcess(!_usingPhysicsBatch);
		}
		else
		{
			if (_usingPhysicsBatch)
			{
				StateChartStatePhysicsBatch.Unregister(this);
				_usingPhysicsBatch = false;
			}
			SetPhysicsProcess(flag);
		}
		SetProcessInput(isActive && this.OnStateInput != null);
		SetProcessUnhandledInput(isActive && this.OnStateUnhandledInput != null);
	}

	public override void _ExitTree()
	{
		if (_usingPhysicsBatch)
		{
			StateChartStatePhysicsBatch.Unregister(this);
			_usingPhysicsBatch = false;
		}
	}
}
