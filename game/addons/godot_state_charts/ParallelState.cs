using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/parallel_state.svg")]
public partial class ParallelState : StateChartState
{
	private List<StateChartState> _subStates = new List<StateChartState>();

	public override void _StateInit()
	{
		base._StateInit();
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState stateChartState)
			{
				_subStates.Add(stateChartState);
				stateChartState._StateInit();
			}
		}
	}

	public override void _HandleTransition(Transition transition, StateChartState source)
	{
		StateChartState stateChartState = transition.ResolveTarget();
		if (stateChartState == null)
		{
			GD.PushError(string.Concat(new string[5]
			{
				"The target state '",
				transition.to.ToString(),
				"' of the transition from '",
				source.Name,
				"' is not a state."
			}));
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
			return;
		}
		if (IsAncestorOf(stateChartState))
		{
			foreach (Node child2 in GetChildren())
			{
				if (child2 is StateChartState stateChartState2 && stateChartState2.IsAncestorOf(stateChartState))
				{
					stateChartState2._HandleTransition(transition, source);
					break;
				}
			}
			return;
		}
		((StateChartState)GetParent())._HandleTransition(transition, source);
	}

	public override void _StateEnter(StateChartState transitionTarget)
	{
		base._StateEnter(transitionTarget);
		foreach (StateChartState subState in _subStates)
		{
			subState._StateEnter(transitionTarget);
		}
	}

	public override void _StateExit()
	{
		foreach (StateChartState subState in _subStates)
		{
			subState._StateExit();
		}
		base._StateExit();
	}

	public override void _StateStep()
	{
		base._StateStep();
		foreach (StateChartState subState in _subStates)
		{
			subState._StateStep();
		}
	}

	public override bool _ProcessTransitions(StateChart.TriggerType triggerType, StringName event_ = null)
	{
		if (!base.active)
		{
			return false;
		}
		bool flag = false;
		foreach (StateChartState subState in _subStates)
		{
			bool flag2 = subState._ProcessTransitions(triggerType, event_);
			flag = flag || flag2;
		}
		if (flag)
		{
			if (triggerType == StateChart.TriggerType.Event)
			{
				RaiseEventReceived(event_);
			}
			return true;
		}
		return base._ProcessTransitions(triggerType, event_);
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
		if (num < 2)
		{
			list.Add("Parallel states should have at least two child states.");
		}
		return list.ToArray();
	}
}
