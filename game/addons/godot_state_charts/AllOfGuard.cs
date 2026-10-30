using Godot;
using Godot.Collections;

[GlobalClass]
[Icon("res://addons/godot_state_charts/all_of_guard.svg")]
public partial class AllOfGuard : Guard
{
	[Export(PropertyHint.None, "")]
	public Array<Guard> guards { get; set; } = new Array<Guard>();

	public override bool IsSatisfied(Transition contextTransition, StateChartState contextState)
	{
		foreach (Guard guard in guards)
		{
			if (!guard.IsSatisfied(contextTransition, contextState))
			{
				return false;
			}
		}
		return true;
	}

	public override int GetSupportedTriggerTypes()
	{
		int num = 0;
		foreach (Guard guard in guards)
		{
			num |= guard.GetSupportedTriggerTypes();
		}
		return num;
	}
}
