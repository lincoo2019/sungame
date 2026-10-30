public readonly struct StateMachineCallbackCatalogEntry(string key, string ownerId, StateMachineCallbackPhaseFlags phases, StateMachineCallbackSourceKind sourceKind = StateMachineCallbackSourceKind.CSharp, string displayName = "", string sourcePath = "")
{
	public string Key { get; } = key ?? string.Empty;

	public string OwnerId { get; } = ownerId ?? string.Empty;

	public StateMachineCallbackPhaseFlags Phases { get; } = phases;

	public StateMachineCallbackSourceKind SourceKind { get; } = sourceKind;

	public string DisplayName { get; } = displayName ?? string.Empty;

	public string SourcePath { get; } = sourcePath ?? string.Empty;
}
