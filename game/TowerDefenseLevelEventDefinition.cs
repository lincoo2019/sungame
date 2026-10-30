using System;

public sealed class TowerDefenseLevelEventDefinition
{
	public string Id { get; }

	public string DisplayKey { get; }

	public Type EventType { get; }

	internal Func<TowerDefenseLevelEventBase> Factory { get; }

	internal TowerDefenseLevelEventDefinition(string id, string displayKey, Type eventType, Func<TowerDefenseLevelEventBase> factory)
	{
		Id = id;
		DisplayKey = displayKey;
		EventType = eventType;
		Factory = factory;
	}
}
