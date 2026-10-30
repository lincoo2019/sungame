using System;
using System.Collections.Generic;
using Godot;

public sealed class StateMachineGraphClipboard
{
	public StateMachineGraphClipboardData CopySubgraph(StateMachineDefinition definition, StateMachineLayout layout, IEnumerable<string> stableIds)
	{
		ArgumentNullException.ThrowIfNull(definition, "definition");
		HashSet<string> hashSet = new HashSet<string>(stableIds ?? Array.Empty<string>(), StringComparer.Ordinal);
		StateMachineGraphClipboardData stateMachineGraphClipboardData = new StateMachineGraphClipboardData();
		StateMachineDefinition stateMachineDefinition = ComposeOrThrow(definition);
		for (int i = 0; i < stateMachineDefinition.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = stateMachineDefinition.States[i];
			if (stateMachineStateDefinition != null && hashSet.Contains(stateMachineStateDefinition.StableId))
			{
				stateMachineGraphClipboardData.States.Add(StateMachineResourceClone.CloneState(stateMachineStateDefinition));
				if (layout != null && layout.Positions.TryGetValue(stateMachineStateDefinition.StableId, out var value))
				{
					stateMachineGraphClipboardData.Positions[stateMachineStateDefinition.StableId] = value;
				}
			}
		}
		for (int j = 0; j < stateMachineDefinition.Transitions.Count; j++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = stateMachineDefinition.Transitions[j];
			if (stateMachineTransitionDefinition != null && hashSet.Contains(stateMachineTransitionDefinition.SourceStateId) && hashSet.Contains(stateMachineTransitionDefinition.TargetStateId))
			{
				stateMachineGraphClipboardData.Transitions.Add(StateMachineResourceClone.CloneTransition(stateMachineTransitionDefinition));
			}
		}
		return stateMachineGraphClipboardData;
	}

	public StateMachineGraphPasteResult PasteSubgraph(StateMachineGraphClipboardData data, IStateMachineStableIdProvider stableIdProvider, Vector2 offset, StateMachineDefinition targetDefinition = null)
	{
		ArgumentNullException.ThrowIfNull(data, "data");
		ArgumentNullException.ThrowIfNull(stableIdProvider, "stableIdProvider");
		StateMachineGraphPasteResult stateMachineGraphPasteResult = new StateMachineGraphPasteResult();
		HashSet<string> existingStateIds = CollectVisibleStateIds(targetDefinition);
		for (int i = 0; i < data.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = data.States[i];
			string value = stableIdProvider.CreateStableId("state");
			stateMachineGraphPasteResult.StableIdRemap[stateMachineStateDefinition.StableId] = value;
		}
		for (int j = 0; j < data.States.Count; j++)
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = data.States[j];
			StateMachineStateDefinition stateMachineStateDefinition3 = StateMachineResourceClone.CloneState(stateMachineStateDefinition2);
			stateMachineStateDefinition3.StableId = stateMachineGraphPasteResult.StableIdRemap[stateMachineStateDefinition2.StableId];
			stateMachineStateDefinition3.ParentId = RemapOrKeepExisting(stateMachineStateDefinition2.ParentId, stateMachineGraphPasteResult.StableIdRemap, existingStateIds);
			stateMachineStateDefinition3.InitialChildId = RemapOrEmpty(stateMachineStateDefinition2.InitialChildId, stateMachineGraphPasteResult.StableIdRemap);
			stateMachineGraphPasteResult.States.Add(stateMachineStateDefinition3);
			if (data.Positions.TryGetValue(stateMachineStateDefinition2.StableId, out var value2))
			{
				stateMachineGraphPasteResult.Positions[stateMachineStateDefinition3.StableId] = value2 + offset;
			}
		}
		for (int k = 0; k < data.Transitions.Count; k++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = data.Transitions[k];
			StateMachineTransitionDefinition stateMachineTransitionDefinition2 = StateMachineResourceClone.CloneTransition(stateMachineTransitionDefinition);
			stateMachineTransitionDefinition2.StableId = stableIdProvider.CreateStableId("transition");
			stateMachineTransitionDefinition2.SourceStateId = RemapOrUnresolved(stateMachineTransitionDefinition.SourceStateId, stateMachineGraphPasteResult.StableIdRemap);
			stateMachineTransitionDefinition2.TargetStateId = RemapOrUnresolved(stateMachineTransitionDefinition.TargetStateId, stateMachineGraphPasteResult.StableIdRemap);
			stateMachineGraphPasteResult.Transitions.Add(stateMachineTransitionDefinition2);
		}
		return stateMachineGraphPasteResult;
	}

	private static string RemapOrUnresolved(string stableId, Dictionary<string, string> remap)
	{
		if (string.IsNullOrWhiteSpace(stableId))
		{
			return string.Empty;
		}
		if (!remap.TryGetValue(stableId, out var value))
		{
			return "unresolved:" + stableId;
		}
		return value;
	}

	private static string RemapOrKeepExisting(string stableId, Dictionary<string, string> remap, HashSet<string> existingStateIds)
	{
		if (string.IsNullOrWhiteSpace(stableId))
		{
			return string.Empty;
		}
		if (remap.TryGetValue(stableId, out var value))
		{
			return value;
		}
		if (!existingStateIds.Contains(stableId))
		{
			return "unresolved:" + stableId;
		}
		return stableId;
	}

	private static string RemapOrEmpty(string stableId, Dictionary<string, string> remap)
	{
		if (string.IsNullOrWhiteSpace(stableId))
		{
			return string.Empty;
		}
		if (!remap.TryGetValue(stableId, out var value))
		{
			return string.Empty;
		}
		return value;
	}

	private static HashSet<string> CollectVisibleStateIds(StateMachineDefinition definition)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (definition == null)
		{
			return hashSet;
		}
		StateMachineDefinition stateMachineDefinition = ComposeOrThrow(definition);
		if (stateMachineDefinition?.States == null)
		{
			return hashSet;
		}
		for (int i = 0; i < stateMachineDefinition.States.Count; i++)
		{
			string text = stateMachineDefinition.States[i]?.StableId ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text))
			{
				hashSet.Add(text);
			}
		}
		return hashSet;
	}

	private static StateMachineDefinition ComposeOrThrow(StateMachineDefinition definition)
	{
		if (StateMachineDefinitionComposer.TryCompose(definition, out var composed, out var validation))
		{
			return composed;
		}
		StateMachineDiagnostic stateMachineDiagnostic = ((validation != null && validation.Diagnostics?.Count > 0) ? validation.Diagnostics[0] : null);
		throw new InvalidOperationException((stateMachineDiagnostic == null) ? "State-machine inheritance could not be composed." : ("[" + stateMachineDiagnostic.Code + "] " + stateMachineDiagnostic.Message));
	}
}
