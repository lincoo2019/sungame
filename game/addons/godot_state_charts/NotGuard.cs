using Godot;

[GlobalClass]
[Icon("res://addons/godot_state_charts/not_guard.svg")]
public partial class NotGuard : Guard
{
	[Export(PropertyHint.None, "")]
	public Guard guard { get; set; }

	public override bool IsSatisfied(Transition contextTransition, StateChartState contextState)
	{
		if (guard == null)
		{
			return true;
		}
		return !guard.IsSatisfied(contextTransition, contextState);
	}

	public override int GetSupportedTriggerTypes()
	{
		if (guard == null)
		{
			return 0;
		}
		return guard.GetSupportedTriggerTypes();
	}
}
