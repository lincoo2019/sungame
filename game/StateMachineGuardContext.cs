using System;
using Godot;

public readonly struct StateMachineGuardContext(object host, StateMachineRuntime runtime, string sourceStateId, string targetStateId, StringName eventName)
{
	public object Host { get; } = host;

	public StateMachineRuntime Runtime { get; } = runtime;

	public string SourceStateId { get; } = sourceStateId ?? string.Empty;

	public string TargetStateId { get; } = targetStateId ?? string.Empty;

	public StringName EventName { get; } = eventName;

	public Variant GetProperty(StringName name, Variant fallback = default(Variant))
	{
		return Runtime?.GetExpressionProperty(name, fallback) ?? fallback;
	}

	public T RequireHost<T>() where T : class
	{
		return (Host as T) ?? throw new InvalidOperationException($"State-machine guard requires host '{typeof(T).FullName}', but received '{Host?.GetType().FullName ?? "<null>"}'.");
	}
}
