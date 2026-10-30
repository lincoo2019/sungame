using System.Collections.Generic;
using Godot;

[GlobalClass]
[Tool]
[Icon("res://addons/godot_state_charts/history_state.svg")]
public partial class HistoryState : StateChartState
{
	[Export(PropertyHint.None, "")]
	public bool deep;

	private NodePath _defaultStatePath = new NodePath();

	public SavedState history;

	[Export(PropertyHint.None, "")]
	public NodePath defaultState
	{
		get
		{
			return _defaultStatePath;
		}
		set
		{
			_defaultStatePath = value;
			UpdateConfigurationWarnings();
		}
	}

	public override void _StateSave(SavedState savedState, int childLevels = -1)
	{
		SavedState savedState2 = new SavedState();
		savedState2.history = history;
		savedState.AddSubstate(this, savedState2);
	}

	public override void _StateRestore(SavedState savedState, int childLevels = -1)
	{
		SavedState substateOrNull = savedState.GetSubstateOrNull(this);
		if (substateOrNull != null)
		{
			history = substateOrNull.history;
		}
	}

	public override string[] _GetConfigurationWarnings()
	{
		List<string> list = new List<string>(base._GetConfigurationWarnings());
		if (!(GetParent() is CompoundState))
		{
			list.Add("A history state must be a child of a compound state.");
		}
		StateChartState nodeOrNull = GetNodeOrNull<StateChartState>(_defaultStatePath);
		if (nodeOrNull == null)
		{
			list.Add("The default state is not set or is not a state.");
		}
		else if (!GetParent().IsAncestorOf(nodeOrNull))
		{
			list.Add("The default state must be a child of the parent state.");
		}
		if (GetChildCount() > 0)
		{
			list.Add("History states cannot have child nodes.");
		}
		return list.ToArray();
	}
}
