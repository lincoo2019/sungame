using System;

internal readonly struct StateMachineScannedCallback(string fullKey, string ownerId, StateMachineCallbackPhase phase, Delegate callback, string source, StateMachineCallbackSourceKind sourceKind, string displayName, string sourcePath)
{
	public string FullKey { get; } = fullKey;

	public string OwnerId { get; } = ownerId;

	public StateMachineCallbackPhase Phase { get; } = phase;

	public Delegate Callback { get; } = callback;

	public string Source { get; } = source;

	public StateMachineCallbackSourceKind SourceKind { get; } = sourceKind;

	public string DisplayName { get; } = displayName ?? string.Empty;

	public string SourcePath { get; } = sourcePath ?? string.Empty;
}
