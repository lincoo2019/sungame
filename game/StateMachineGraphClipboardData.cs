using System;
using System.Collections.Generic;
using Godot;

public sealed class StateMachineGraphClipboardData
{
	public List<StateMachineStateDefinition> States { get; } = new List<StateMachineStateDefinition>();

	public List<StateMachineTransitionDefinition> Transitions { get; } = new List<StateMachineTransitionDefinition>();

	public Dictionary<string, Vector2> Positions { get; } = new Dictionary<string, Vector2>(StringComparer.Ordinal);
}
