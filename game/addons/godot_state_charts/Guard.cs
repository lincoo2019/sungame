using Godot;

[GlobalClass]
[Icon("res://addons/godot_state_charts/guard.svg")]
public partial class Guard : Resource
{
	public virtual bool IsSatisfied(Transition contextTransition, StateChartState contextState)
	{
		GD.PushError("Guard.IsSatisfied() is not implemented. Did you forget to override it?");
		return false;
	}

	public virtual int GetSupportedTriggerTypes()
	{
		GD.PushError("Guard.GetSupportedTriggerTypes() is not implemented. Did you forget to override it?");
		return 0;
	}
}
