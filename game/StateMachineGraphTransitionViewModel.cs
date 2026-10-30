public sealed class StateMachineGraphTransitionViewModel
{
	public StateMachineTransitionDefinition Transition { get; init; }

	public string StableId => Transition?.StableId ?? string.Empty;

	public bool IsInherited { get; init; }

	public bool IsLocalOverride { get; init; }

	public bool IsReadOnly { get; init; }
}
