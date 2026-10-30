using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/compound_state.svg")]
public partial class CompoundState : StateChartState
{
	public delegate void ChildStateEnteredEventHandler();

	public delegate void ChildStateExitedEventHandler();

	private NodePath _initialStatePath = new NodePath();

	public StateChartState _activeState;

	private StateChartState _initialState;

	private List<HistoryState> _historyStates = new List<HistoryState>();

	private bool _needsDeepHistory;

	[Export(PropertyHint.None, "")]
	public NodePath initialState
	{
		get
		{
			return _initialStatePath;
		}
		set
		{
			_initialStatePath = value;
			UpdateConfigurationWarnings();
		}
	}

	public StateChartState activeState
	{
		get
		{
			if (_activeState == null || !GodotObject.IsInstanceValid(_activeState))
			{
				return null;
			}
			return _activeState;
		}
	}

	public event ChildStateEnteredEventHandler OnChildStateEntered;

	public event ChildStateExitedEventHandler OnChildStateExited;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			base.ChildEnteredTree += delegate(Node child)
			{
				if (child is StateChartState && (_initialStatePath == null || _initialStatePath == (NodePath)""))
				{
					Callable.From(delegate
					{
						initialState = GetPathTo(child);
					}).CallDeferred();
				}
			};
		}
		_initialState = GetNodeOrNull<StateChartState>(_initialStatePath);
	}

	public override void _StateInit()
	{
		base._StateInit();
		foreach (Node child in GetChildren())
		{
			if (child is HistoryState historyState)
			{
				_historyStates.Add(historyState);
				_needsDeepHistory = _needsDeepHistory || historyState.deep;
			}
		}
		foreach (Node child2 in GetChildren())
		{
			if (child2 is StateChartState stateChartState)
			{
				stateChartState._StateInit();
				stateChartState.OnStateEntered += delegate
				{
					this.OnChildStateEntered?.Invoke();
				};
				stateChartState.OnStateExited += delegate
				{
					this.OnChildStateExited?.Invoke();
				};
			}
		}
	}

	public override void _StateEnter(StateChartState transitionTarget)
	{
		base._StateEnter(transitionTarget);
		bool flag = false;
		if (transitionTarget != null && IsAncestorOf(transitionTarget))
		{
			flag = true;
		}
		if (flag || GodotObject.IsInstanceValid(_activeState) || !_stateActive)
		{
			return;
		}
		if (_initialState != null)
		{
			if (_initialState is HistoryState)
			{
				_RestoreHistoryState((HistoryState)_initialState);
				return;
			}
			_activeState = _initialState;
			_activeState._StateEnter(null);
		}
		else
		{
			GD.PushError(string.Concat("No initial state set for state '", base.Name, "'."));
		}
	}

	public override void _StateStep()
	{
		base._StateStep();
		if (_activeState != null)
		{
			_activeState._StateStep();
		}
	}

	public override void _StateSave(SavedState savedState, int childLevels = -1)
	{
		base._StateSave(savedState, childLevels);
		SavedState substateOrNull = savedState.GetSubstateOrNull(this);
		if (substateOrNull == null)
		{
			GD.PushError(string.Concat("Probably a bug: The state of '", base.Name, "' was not saved."));
			return;
		}
		foreach (HistoryState historyState in _historyStates)
		{
			historyState._StateSave(substateOrNull, childLevels);
		}
	}

	public override void _StateRestore(SavedState savedState, int childLevels = -1)
	{
		base._StateRestore(savedState, childLevels);
		if (!base.active)
		{
			return;
		}
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState { active: not false } stateChartState)
			{
				_activeState = stateChartState;
				break;
			}
		}
	}

	public override void _StateExit()
	{
		if (_historyStates.Count > 0)
		{
			SavedState savedState = new SavedState();
			_StateSave(savedState, (!_needsDeepHistory) ? 1 : (-1));
			foreach (HistoryState historyState in _historyStates)
			{
				historyState.history = savedState;
			}
		}
		if (_activeState != null)
		{
			_activeState._StateExit();
			_activeState = null;
		}
		base._StateExit();
	}

	public override bool _ProcessTransitions(StateChart.TriggerType triggerType, StringName event_ = null)
	{
		if (!base.active)
		{
			return false;
		}
		if (GodotObject.IsInstanceValid(_activeState) && _activeState._ProcessTransitions(triggerType, event_))
		{
			if (triggerType == StateChart.TriggerType.Event)
			{
				RaiseEventReceived(event_);
			}
			return true;
		}
		return base._ProcessTransitions(triggerType, event_);
	}

	public override void _HandleTransition(Transition transition, StateChartState source)
	{
		StateChartState stateChartState = transition.ResolveTarget();
		if (stateChartState == null)
		{
			GD.PushError($"[CompoundState] 目标状态 '{transition.to}' 不是 StateChartState, target={stateChartState?.GetType().Name ?? "null"}");
			return;
		}
		if (stateChartState == this)
		{
			_StateExit();
			_StateEnter(stateChartState);
			return;
		}
		bool flag = false;
		foreach (Node child in GetChildren())
		{
			if (child == stateChartState)
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			if (GodotObject.IsInstanceValid(_activeState))
			{
				_activeState._StateExit();
			}
			if (stateChartState is HistoryState)
			{
				_RestoreHistoryState((HistoryState)stateChartState);
				return;
			}
			_activeState = stateChartState;
			_activeState._StateEnter(stateChartState);
			return;
		}
		if (IsAncestorOf(stateChartState))
		{
			foreach (Node child2 in GetChildren())
			{
				if (child2 is StateChartState stateChartState2 && stateChartState2.IsAncestorOf(stateChartState))
				{
					if (_activeState != child2)
					{
						if (GodotObject.IsInstanceValid(_activeState))
						{
							_activeState._StateExit();
						}
						_activeState = stateChartState2;
						_activeState._StateEnter(stateChartState);
					}
					stateChartState2._HandleTransition(transition, source);
					break;
				}
			}
			return;
		}
		((StateChartState)GetParent())._HandleTransition(transition, source);
	}

	public void _RestoreHistoryState(HistoryState target)
	{
		SavedState history = target.history;
		if (history != null)
		{
			_StateRestore(history, (!target.deep) ? 1 : (-1));
			return;
		}
		StateChartState nodeOrNull = target.GetNodeOrNull<StateChartState>(target.defaultState);
		if (GodotObject.IsInstanceValid(nodeOrNull))
		{
			_activeState = nodeOrNull;
			_activeState._StateEnter(null);
			return;
		}
		GD.PushError(string.Concat(new string[5]
		{
			"The default state '",
			target.defaultState.ToString(),
			"' of the history state '",
			target.Name,
			"' cannot be found."
		}));
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>(base._GetConfigurationWarnings());
		int num = 0;
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState)
			{
				num++;
			}
		}
		if (num < 1)
		{
			list.Add("Compound states should have at least one child state.");
		}
		else if (num < 2)
		{
			list.Add("Compound states with only one child state are not very useful. Consider adding more child states or removing this compound state.");
		}
		StateChartState nodeOrNull = GetNodeOrNull<StateChartState>(_initialStatePath);
		if (!GodotObject.IsInstanceValid(nodeOrNull))
		{
			list.Add("Initial state could not be resolved, is the path correct?");
		}
		else if (nodeOrNull.GetParent() != this)
		{
			list.Add("Initial state must be a direct child of this compound state.");
		}
		return list.ToArray();
	}
}
