using System;
using System.Collections.Generic;

public static class StateMachineDefinitionComposer
{
	public static bool TryValidateInheritanceChain(StateMachineDefinition definition, out StateMachineValidationResult validation)
	{
		if (definition == null)
		{
			validation = Failure("SM001", "MissingDefinition", "State machine definition is missing.");
			return false;
		}
		HashSet<ulong> hashSet = new HashSet<ulong>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (StateMachineDefinition stateMachineDefinition = definition; stateMachineDefinition != null; stateMachineDefinition = stateMachineDefinition.BaseDefinition)
		{
			string text = NormalizeResourcePath(stateMachineDefinition.ResourcePath);
			if (!hashSet.Add(stateMachineDefinition.GetInstanceId()) || (!string.IsNullOrWhiteSpace(text) && !hashSet2.Add(text)))
			{
				validation = Failure("SM011", "BaseDefinitionCycle", "StateMachineDefinition BaseDefinition contains a cycle.", stateMachineDefinition.DefinitionId);
				return false;
			}
		}
		validation = new StateMachineValidationResult(Array.Empty<StateMachineDiagnostic>());
		return true;
	}

	public static bool TryCompose(StateMachineDefinition definition, out StateMachineDefinition composed, out StateMachineValidationResult validation)
	{
		composed = null;
		if (definition == null)
		{
			validation = Failure("SM001", "MissingDefinition", "State machine definition is missing.");
			return false;
		}
		List<StateMachineDefinition> list = new List<StateMachineDefinition>();
		HashSet<ulong> hashSet = new HashSet<ulong>();
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (StateMachineDefinition stateMachineDefinition = definition; stateMachineDefinition != null; stateMachineDefinition = stateMachineDefinition.BaseDefinition)
		{
			ulong instanceId = stateMachineDefinition.GetInstanceId();
			string text = NormalizeResourcePath(stateMachineDefinition.ResourcePath);
			if (!hashSet.Add(instanceId) || (!string.IsNullOrWhiteSpace(text) && !hashSet2.Add(text)))
			{
				validation = Failure("SM011", "BaseDefinitionCycle", "StateMachineDefinition BaseDefinition contains a cycle.", stateMachineDefinition.DefinitionId);
				return false;
			}
			list.Add(stateMachineDefinition);
		}
		list.Reverse();
		try
		{
			StateMachineDefinition stateMachineDefinition2 = StateMachineResourceClone.CreateCompositionShell(definition);
			for (int i = 0; i < list.Count; i++)
			{
				StateMachineDefinition stateMachineDefinition3 = list[i];
				stateMachineDefinition2.SchemaVersion = stateMachineDefinition3.SchemaVersion;
				if (!string.IsNullOrWhiteSpace(stateMachineDefinition3.DefinitionId))
				{
					stateMachineDefinition2.DefinitionId = stateMachineDefinition3.DefinitionId;
				}
				if (!string.IsNullOrWhiteSpace(stateMachineDefinition3.RootStateId))
				{
					stateMachineDefinition2.RootStateId = stateMachineDefinition3.RootStateId;
				}
				MergeStates(stateMachineDefinition2, stateMachineDefinition3);
				MergeTransitions(stateMachineDefinition2, stateMachineDefinition3);
				MergeAliases(stateMachineDefinition2, stateMachineDefinition3);
			}
			composed = stateMachineDefinition2;
		}
		catch (Exception ex) when (((ex is InvalidOperationException || ex is ArgumentException) ? 1 : 0) != 0)
		{
			composed = null;
			validation = Failure("SM014", "CompositionResourceFailure", "State-machine inheritance could not clone an authored resource: " + ex.Message, definition.DefinitionId);
			return false;
		}
		validation = new StateMachineValidationResult(Array.Empty<StateMachineDiagnostic>());
		return true;
	}

	public static bool TryValidateBaseDefinitionCandidate(StateMachineDefinition owner, StateMachineDefinition candidate, out StateMachineValidationResult validation)
	{
		if (owner == null)
		{
			validation = Failure("SM001", "MissingDefinition", "State machine definition is missing.");
			return false;
		}
		if (candidate == null)
		{
			validation = new StateMachineValidationResult(Array.Empty<StateMachineDiagnostic>());
			return true;
		}
		HashSet<ulong> hashSet = new HashSet<ulong> { owner.GetInstanceId() };
		HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		string text = NormalizeResourcePath(owner.ResourcePath);
		if (!string.IsNullOrWhiteSpace(text))
		{
			hashSet2.Add(text);
		}
		for (StateMachineDefinition stateMachineDefinition = candidate; stateMachineDefinition != null; stateMachineDefinition = stateMachineDefinition.BaseDefinition)
		{
			string text2 = NormalizeResourcePath(stateMachineDefinition.ResourcePath);
			if (!hashSet.Add(stateMachineDefinition.GetInstanceId()) || (!string.IsNullOrWhiteSpace(text2) && !hashSet2.Add(text2)))
			{
				validation = Failure("SM011", "BaseDefinitionCycle", "BaseDefinition selection would create or preserve an inheritance cycle.", owner.DefinitionId);
				return false;
			}
		}
		StateMachineDefinition composed;
		return TryCompose(candidate, out composed, out validation);
	}

	private static void MergeStates(StateMachineDefinition target, StateMachineDefinition layer)
	{
		if (layer.States == null)
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < target.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = target.States[i];
			if (stateMachineStateDefinition != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId) && !dictionary.ContainsKey(stateMachineStateDefinition.StableId))
			{
				dictionary.Add(stateMachineStateDefinition.StableId, i);
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int j = 0; j < layer.States.Count; j++)
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = layer.States[j];
			StateMachineStateDefinition stateMachineStateDefinition3 = StateMachineResourceClone.CloneState(stateMachineStateDefinition2, deep: false);
			string text = stateMachineStateDefinition2?.StableId ?? string.Empty;
			bool flag = !string.IsNullOrWhiteSpace(text) && hashSet.Add(text);
			if (flag && dictionary.TryGetValue(text, out var value))
			{
				target.States[value] = stateMachineStateDefinition3;
				continue;
			}
			target.States.Add(stateMachineStateDefinition3);
			if (flag)
			{
				dictionary[text] = target.States.Count - 1;
			}
		}
	}

	private static void MergeTransitions(StateMachineDefinition target, StateMachineDefinition layer)
	{
		if (layer.Transitions == null)
		{
			return;
		}
		Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < target.Transitions.Count; i++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = target.Transitions[i];
			if (stateMachineTransitionDefinition != null && !string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.StableId) && !dictionary.ContainsKey(stateMachineTransitionDefinition.StableId))
			{
				dictionary.Add(stateMachineTransitionDefinition.StableId, i);
			}
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		for (int j = 0; j < layer.Transitions.Count; j++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition2 = layer.Transitions[j];
			StateMachineTransitionDefinition stateMachineTransitionDefinition3 = StateMachineResourceClone.CloneTransition(stateMachineTransitionDefinition2, deep: false);
			string text = stateMachineTransitionDefinition2?.StableId ?? string.Empty;
			bool flag = !string.IsNullOrWhiteSpace(text) && hashSet.Add(text);
			if (flag && dictionary.TryGetValue(text, out var value))
			{
				target.Transitions[value] = stateMachineTransitionDefinition3;
				continue;
			}
			target.Transitions.Add(stateMachineTransitionDefinition3);
			if (flag)
			{
				dictionary[text] = target.Transitions.Count - 1;
			}
		}
	}

	private static void MergeAliases(StateMachineDefinition target, StateMachineDefinition layer)
	{
		if (layer.Aliases == null)
		{
			return;
		}
		foreach (string key in layer.Aliases.Keys)
		{
			target.Aliases[key ?? string.Empty] = layer.Aliases[key] ?? string.Empty;
		}
	}

	private static StateMachineValidationResult Failure(string code, string name, string message, string stableId = "")
	{
		return new StateMachineValidationResult(new StateMachineDiagnostic[1]
		{
			new StateMachineDiagnostic(code, name, StateMachineDiagnosticSeverity.Error, message, stableId)
		});
	}

	private static string NormalizeResourcePath(string resourcePath)
	{
		if (!string.IsNullOrWhiteSpace(resourcePath))
		{
			return resourcePath.Trim().Replace('\\', '/');
		}
		return string.Empty;
	}
}
