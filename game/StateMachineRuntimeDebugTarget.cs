using System;
using Godot;

public sealed class StateMachineRuntimeDebugTarget
{
	public string DisplayName { get; }

	public Func<bool> EnsureCurrent { get; }

	public Func<IStateMachineController> ResolveController { get; }

	public Func<StringName, bool> SendEvent { get; }

	public Func<double, bool> Step { get; }

	public Func<bool> Reset { get; }

	public StateMachineRuntimeDebugTarget(string displayName, Func<bool> ensureCurrent, Func<IStateMachineController> resolveController, Func<StringName, bool> sendEvent, Func<double, bool> step, Func<bool> reset)
	{
		DisplayName = displayName ?? string.Empty;
		EnsureCurrent = ensureCurrent;
		ResolveController = resolveController;
		SendEvent = sendEvent;
		Step = step;
		Reset = reset;
	}
}
