internal readonly struct StateMachineCallbackCatalogMetadata(StateMachineCallbackSourceKind sourceKind, string displayName, string sourcePath)
{
	public StateMachineCallbackSourceKind SourceKind { get; } = sourceKind;

	public string DisplayName { get; } = displayName ?? string.Empty;

	public string SourcePath { get; } = sourcePath ?? string.Empty;
}
