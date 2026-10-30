using Godot;

[GlobalClass]
[Icon("res://addons/godot_state_charts/state_is_active_guard.svg")]
public partial class StateIsActiveGuard : Guard
{
	[Export(PropertyHint.None, "")]
	public NodePath state { get; set; }

	public override bool IsSatisfied(Transition contextTransition, StateChartState contextState)
	{
		StateChartState nodeOrNull = contextTransition.GetNodeOrNull<StateChartState>(state);
		if (nodeOrNull == null)
		{
			GD.PushWarning(string.Concat(new string[5]
			{
				"State ",
				state,
				" referenced in StateIsActiveGuard below ",
				DebugUtil.PathOf(contextState),
				" could not be resolved. Verify that the node path is correct."
			}));
			return false;
		}
		return nodeOrNull.active;
	}

	public override int GetSupportedTriggerTypes()
	{
		return 8;
	}
}
