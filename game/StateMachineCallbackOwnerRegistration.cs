using System.Collections.Generic;
using System.Reflection;

internal sealed class StateMachineCallbackOwnerRegistration
{
	public string OwnerId = string.Empty;

	public Assembly Assembly;

	public readonly List<StateMachineCallbackSlot> Slots = new List<StateMachineCallbackSlot>();

	public int LeaseCount;

	public long NextLeaseId;

	public readonly Dictionary<long, StateMachineUnloadBlocker> Blockers = new Dictionary<long, StateMachineUnloadBlocker>();
}
