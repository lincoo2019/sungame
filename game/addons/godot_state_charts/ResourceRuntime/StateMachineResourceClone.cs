using System;
using Godot;
using Godot.Collections;

public static class StateMachineResourceClone
{
	public static StateMachineDefinition CreateCompositionShell(StateMachineDefinition source)
	{
		ArgumentNullException.ThrowIfNull(source, "source");
		StateMachineDefinition stateMachineDefinition = DuplicateExact(source, deep: false, "definition");
		stateMachineDefinition.BaseDefinition = null;
		stateMachineDefinition.States = new Array<StateMachineStateDefinition>();
		stateMachineDefinition.Transitions = new Array<StateMachineTransitionDefinition>();
		stateMachineDefinition.Aliases = new Dictionary<string, string>();
		return stateMachineDefinition;
	}

	public static StateMachineStateDefinition CloneState(StateMachineStateDefinition source, bool deep = true)
	{
		if (source != null)
		{
			return DuplicateExact(source, deep, "state");
		}
		return null;
	}

	public static StateMachineTransitionDefinition CloneTransition(StateMachineTransitionDefinition source, bool deep = true)
	{
		if (source != null)
		{
			return DuplicateExact(source, deep, "transition");
		}
		return null;
	}

	private static T DuplicateExact<T>(T source, bool deep, string role) where T : Resource
	{
		Resource resource;
		try
		{
			resource = source.Duplicate(deep);
		}
		catch (Exception innerException)
		{
			throw new InvalidOperationException($"Unable to duplicate state-machine {role} resource '{source.GetType().FullName}'.", innerException);
		}
		T obj = resource as T;
		if (obj == null || resource.GetType() != source.GetType())
		{
			resource?.Dispose();
			throw new InvalidOperationException($"State-machine {role} clone changed C# type from '{source.GetType().FullName}' to '{resource?.GetType().FullName ?? "<null>"}'.");
		}
		return obj;
	}
}
