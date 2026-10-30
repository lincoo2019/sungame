using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class StateMachineCallbackAttribute : Attribute
{
	public string LocalKey { get; }

	public StateMachineCallbackPhase Phase { get; }

	public StateMachineCallbackSourceKind SourceKind { get; set; }

	public string DisplayName { get; set; } = string.Empty;

	public string SourcePath { get; set; } = string.Empty;

	public StateMachineCallbackAttribute(string localKey, StateMachineCallbackPhase phase)
	{
		LocalKey = localKey;
		Phase = phase;
	}
}
