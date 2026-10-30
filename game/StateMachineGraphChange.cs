using System;
using System.Collections.Generic;

public sealed class StateMachineGraphChange
{
	public StateMachineGraphChangeKind Kind { get; init; }

	public IReadOnlyList<string> AffectedStableIds { get; init; } = Array.Empty<string>();
}
