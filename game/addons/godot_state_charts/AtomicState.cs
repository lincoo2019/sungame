using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/atomic_state.svg")]
public partial class AtomicState : StateChartState
{
	public override void _HandleTransition(Transition transition, StateChartState source)
	{
		if (transition.ResolveTarget() == null)
		{
			GD.PushError(string.Concat(new string[5]
			{
				"The target state '",
				transition.to.ToString(),
				"' of the transition from '",
				source.Name,
				"' is not a state."
			}));
		}
		else
		{
			((StateChartState)GetParent())._HandleTransition(transition, source);
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>(base._GetConfigurationWarnings());
		foreach (Node child in GetChildren())
		{
			if (child is StateChartState)
			{
				list.Add("Atomic states cannot have child states. These will be ignored.");
				break;
			}
		}
		return list.ToArray();
	}
}
