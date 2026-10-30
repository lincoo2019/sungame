using System;
using System.Collections.Generic;
using Godot;

public sealed class StateMachineGraphPasteResult
{
	public List<StateMachineStateDefinition> States { get; } = new List<StateMachineStateDefinition>();

	public List<StateMachineTransitionDefinition> Transitions { get; } = new List<StateMachineTransitionDefinition>();

	public Dictionary<string, Vector2> Positions { get; } = new Dictionary<string, Vector2>(StringComparer.Ordinal);

	public Dictionary<string, string> StableIdRemap { get; } = new Dictionary<string, string>(StringComparer.Ordinal);
}
