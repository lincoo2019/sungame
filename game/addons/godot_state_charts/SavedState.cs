using Godot;
using Godot.Collections;

[GlobalClass]
public partial class SavedState : Resource
{
	[Export(PropertyHint.None, "")]
	public Dictionary childStates { get; set; } = new Dictionary();

	[Export(PropertyHint.None, "")]
	public NodePath pendingTransitionName { get; set; }

	[Export(PropertyHint.None, "")]
	public float pendingTransitionRemainingDelay { get; set; }

	[Export(PropertyHint.None, "")]
	public float pendingTransitionInitialDelay { get; set; }

	[Export(PropertyHint.None, "")]
	public SavedState history { get; set; }

	public void AddSubstate(StateChartState state, SavedState savedState)
	{
		childStates[state.Name] = savedState;
	}

	public SavedState GetSubstateOrNull(StateChartState state)
	{
		if (childStates.ContainsKey(state.Name))
		{
			return childStates[state.Name].As<SavedState>();
		}
		return null;
	}
}
