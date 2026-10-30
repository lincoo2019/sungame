using System;

public sealed class StateMachineUnloadBlockers
{
	public int LeaseCount { get; }

	public StateMachineUnloadBlocker[] Entries { get; }

	public StateMachineUnloadBlockers(int leaseCount, StateMachineUnloadBlocker[] entries)
	{
		LeaseCount = leaseCount;
		Entries = entries ?? Array.Empty<StateMachineUnloadBlocker>();
	}
}
