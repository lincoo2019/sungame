using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public sealed class StateMachineEditCommand
{
	private readonly Action _apply;

	private readonly Action _revert;

	private readonly ReadOnlyCollection<string> _affectedStableIds;

	public Action ApplyAction => _apply;

	public Action RevertAction => _revert;

	public IReadOnlyList<string> AffectedStableIds => _affectedStableIds;

	public StateMachineEditCommand(Action apply, Action revert, IEnumerable<string> affectedStableIds)
	{
		_apply = apply ?? throw new ArgumentNullException("apply");
		_revert = revert ?? throw new ArgumentNullException("revert");
		_affectedStableIds = new List<string>(affectedStableIds ?? Array.Empty<string>()).AsReadOnly();
	}

	public void Apply()
	{
		_apply();
	}

	public void Revert()
	{
		_revert();
	}
}
