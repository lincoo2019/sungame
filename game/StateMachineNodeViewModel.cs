public sealed class StateMachineNodeViewModel
{
	public StateMachineStateDefinition State { get; init; }

	public string StableId => State?.StableId ?? string.Empty;

	public bool IsInherited { get; init; }

	public bool IsLocalOverride { get; init; }

	public bool IsReadOnly { get; init; }
}
