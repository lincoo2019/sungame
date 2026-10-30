public readonly struct StateMachineUnloadBlocker(string definitionId, string hostType)
{
	public string DefinitionId { get; } = definitionId ?? string.Empty;

	public string HostType { get; } = hostType ?? string.Empty;
}
