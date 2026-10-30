using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Godot;
using Godot.Collections;

public sealed class StateMachineSceneImporter
{
	private const float HorizontalSpacing = 280f;

	private const float VerticalSpacing = 130f;

	public StateMachineSceneImportResult Import(StateChart legacyChart, string sourceScenePath, Godot.Collections.Dictionary<string, string> persistedImportMap = null)
	{
		return ImportCore(legacyChart, sourceScenePath, null, persistedImportMap);
	}

	public StateMachineSceneImportResult Import(StateChart legacyChart, string sourceScenePath, StateMachineDefinition baseDefinition, Godot.Collections.Dictionary<string, string> persistedImportMap = null)
	{
		return ImportCore(legacyChart, sourceScenePath, baseDefinition, persistedImportMap);
	}

	private StateMachineSceneImportResult ImportCore(StateChart legacyChart, string sourceScenePath, StateMachineDefinition baseDefinition, Godot.Collections.Dictionary<string, string> persistedImportMap)
	{
		List<StateMachineDiagnostic> list = new List<StateMachineDiagnostic>();
		List<StateChartState> list2 = new List<StateChartState>();
		List<Transition> list3 = new List<Transition>();
		if (legacyChart == null || !GodotObject.IsInstanceValid(legacyChart))
		{
			list.Add(Error("SMI001", "InvalidLegacyChart", "Legacy StateChart is missing or invalid."));
			return BlockingResult(list);
		}
		StateChartState stateChartState = null;
		foreach (Node child in legacyChart.GetChildren())
		{
			if (child is StateChartState stateChartState2)
			{
				if (stateChartState != null)
				{
					list.Add(Error("SMI001", "InvalidLegacyChart", "Legacy StateChart must contain exactly one root state."));
					return BlockingResult(list);
				}
				stateChartState = stateChartState2;
			}
		}
		if (stateChartState == null)
		{
			list.Add(Error("SMI001", "InvalidLegacyChart", "Legacy StateChart does not contain a root state."));
			return BlockingResult(list);
		}
		CollectGraph(stateChartState, list2, list3);
		Preflight(legacyChart, list2, list3, list);
		if (ContainsErrors(list))
		{
			return new StateMachineSceneImportResult
			{
				Definition = null,
				Diagnostics = new StateMachineValidationResult(list)
			};
		}
		string text = NormalizeSourceScenePath(sourceScenePath);
		Godot.Collections.Dictionary<string, string> dictionary = BuildStableIdMap(legacyChart, text, list2, list3, persistedImportMap, list);
		if (ContainsErrors(list))
		{
			return BlockingResult(list);
		}
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			DefinitionId = "import." + Hash128Hex(text),
			RootStateId = dictionary[GetLegacyPath(legacyChart, stateChartState)]
		};
		StateMachineLayout stateMachineLayout = new StateMachineLayout();
		for (int i = 0; i < list2.Count; i++)
		{
			StateChartState stateChartState3 = list2[i];
			string legacyPath = GetLegacyPath(legacyChart, stateChartState3);
			string text2 = dictionary[legacyPath];
			string parentId = ((stateChartState3.GetParent() is StateChartState node) ? dictionary[GetLegacyPath(legacyChart, node)] : string.Empty);
			StateMachineStateDefinition stateMachineStateDefinition = new StateMachineStateDefinition
			{
				StableId = text2,
				DisplayName = stateChartState3.Name,
				Kind = GetStateKind(stateChartState3),
				ParentId = parentId,
				ProcessFlags = stateChartState3.ObservedProcessFlags
			};
			if (stateChartState3 is CompoundState compoundState)
			{
				StateChartState nodeOrNull = compoundState.GetNodeOrNull<StateChartState>(compoundState.initialState);
				stateMachineStateDefinition.InitialChildId = dictionary[GetLegacyPath(legacyChart, nodeOrNull)];
			}
			stateMachineDefinition.States.Add(stateMachineStateDefinition);
			stateMachineLayout.Positions[text2] = new Vector2((float)GetStateDepth(stateChartState3) * 280f, (float)i * 130f);
		}
		for (int j = 0; j < list3.Count; j++)
		{
			Transition transition = list3[j];
			StateChartState node2 = (StateChartState)transition.GetParent();
			StateChartState nodeOrNull2 = transition.GetNodeOrNull<StateChartState>(transition.to);
			string text3 = transition.@event.ToString();
			double.TryParse(transition.delayInSeconds, NumberStyles.Float, CultureInfo.InvariantCulture, out var result);
			stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
			{
				StableId = dictionary[GetLegacyPath(legacyChart, transition)],
				SourceStateId = dictionary[GetLegacyPath(legacyChart, node2)],
				TargetStateId = dictionary[GetLegacyPath(legacyChart, nodeOrNull2)],
				TriggerKind = (string.IsNullOrEmpty(text3) ? StateMachineTriggerKind.Automatic : StateMachineTriggerKind.Event),
				EventName = new StringName(text3),
				DelaySeconds = result,
				DeclarationOrder = j
			});
		}
		if (baseDefinition != null)
		{
			if (!StateMachineDefinitionComposer.TryCompose(baseDefinition, out var composed, out var validation))
			{
				return new StateMachineSceneImportResult
				{
					Definition = null,
					Diagnostics = validation
				};
			}
			stateMachineDefinition = CreateDerivedLayerDefinition(stateMachineDefinition, baseDefinition, composed);
		}
		StateMachineValidationResult stateMachineValidationResult = StateMachineValidator.Validate(stateMachineDefinition);
		if (!stateMachineValidationResult.IsValid)
		{
			return new StateMachineSceneImportResult
			{
				Definition = null,
				Diagnostics = stateMachineValidationResult
			};
		}
		return new StateMachineSceneImportResult
		{
			Definition = stateMachineDefinition,
			Layout = stateMachineLayout,
			Diagnostics = stateMachineValidationResult,
			LegacyPathToStableId = dictionary
		};
	}

	private static StateMachineDefinition CreateDerivedLayerDefinition(StateMachineDefinition fullDefinition, StateMachineDefinition baseDefinition, StateMachineDefinition composedBase)
	{
		StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
		{
			SchemaVersion = fullDefinition.SchemaVersion,
			DefinitionId = fullDefinition.DefinitionId,
			BaseDefinition = baseDefinition,
			RootStateId = (string.Equals(fullDefinition.RootStateId, composedBase.RootStateId, StringComparison.Ordinal) ? string.Empty : fullDefinition.RootStateId)
		};
		System.Collections.Generic.Dictionary<string, StateMachineStateDefinition> dictionary = new System.Collections.Generic.Dictionary<string, StateMachineStateDefinition>(StringComparer.Ordinal);
		for (int i = 0; i < composedBase.States.Count; i++)
		{
			StateMachineStateDefinition stateMachineStateDefinition = composedBase.States[i];
			if (stateMachineStateDefinition != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId))
			{
				dictionary[stateMachineStateDefinition.StableId] = stateMachineStateDefinition;
			}
		}
		for (int j = 0; j < fullDefinition.States.Count; j++)
		{
			StateMachineStateDefinition stateMachineStateDefinition2 = fullDefinition.States[j];
			if (stateMachineStateDefinition2 != null && dictionary.TryGetValue(stateMachineStateDefinition2.StableId, out var value))
			{
				stateMachineStateDefinition2.ProcessFlags |= value.ProcessFlags;
				if (StatesEquivalent(stateMachineStateDefinition2, value))
				{
					continue;
				}
			}
			stateMachineDefinition.States.Add(stateMachineStateDefinition2);
		}
		System.Collections.Generic.Dictionary<string, StateMachineTransitionDefinition> dictionary2 = new System.Collections.Generic.Dictionary<string, StateMachineTransitionDefinition>(StringComparer.Ordinal);
		for (int k = 0; k < composedBase.Transitions.Count; k++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = composedBase.Transitions[k];
			if (stateMachineTransitionDefinition != null && !string.IsNullOrWhiteSpace(stateMachineTransitionDefinition.StableId))
			{
				dictionary2[stateMachineTransitionDefinition.StableId] = stateMachineTransitionDefinition;
			}
		}
		for (int l = 0; l < fullDefinition.Transitions.Count; l++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition2 = fullDefinition.Transitions[l];
			if (stateMachineTransitionDefinition2 == null || !dictionary2.TryGetValue(stateMachineTransitionDefinition2.StableId, out var value2) || !TransitionsEquivalent(stateMachineTransitionDefinition2, value2))
			{
				stateMachineDefinition.Transitions.Add(stateMachineTransitionDefinition2);
			}
		}
		foreach (string key in fullDefinition.Aliases.Keys)
		{
			if (!composedBase.Aliases.TryGetValue(key, out var value3) || !string.Equals(value3, fullDefinition.Aliases[key], StringComparison.Ordinal))
			{
				stateMachineDefinition.Aliases[key] = fullDefinition.Aliases[key];
			}
		}
		return stateMachineDefinition;
	}

	private static bool StatesEquivalent(StateMachineStateDefinition left, StateMachineStateDefinition right)
	{
		if (string.Equals(left.StableId, right.StableId, StringComparison.Ordinal) && left.DisplayName == right.DisplayName && left.Kind == right.Kind && string.Equals(left.ParentId, right.ParentId, StringComparison.Ordinal) && string.Equals(left.InitialChildId, right.InitialChildId, StringComparison.Ordinal) && left.ProcessFlags == right.ProcessFlags && left.CallbackKey == right.CallbackKey && left.EnterCallbackKey == right.EnterCallbackKey && left.ExitCallbackKey == right.ExitCallbackKey && left.ProcessCallbackKey == right.ProcessCallbackKey)
		{
			return left.PhysicsProcessCallbackKey == right.PhysicsProcessCallbackKey;
		}
		return false;
	}

	private static bool TransitionsEquivalent(StateMachineTransitionDefinition left, StateMachineTransitionDefinition right)
	{
		if (string.Equals(left.StableId, right.StableId, StringComparison.Ordinal) && string.Equals(left.SourceStateId, right.SourceStateId, StringComparison.Ordinal) && string.Equals(left.TargetStateId, right.TargetStateId, StringComparison.Ordinal) && left.TriggerKind == right.TriggerKind && left.EventName == right.EventName && BitConverter.DoubleToInt64Bits(left.DelaySeconds) == BitConverter.DoubleToInt64Bits(right.DelaySeconds) && left.Priority == right.Priority && left.DeclarationOrder == right.DeclarationOrder)
		{
			return left.GuardDefinition == right.GuardDefinition;
		}
		return false;
	}

	public static string NormalizeSourceScenePath(string sourceScenePath)
	{
		string text = (sourceScenePath ?? string.Empty).Trim().Replace('\\', '/');
		if (text.StartsWith("res:/", StringComparison.OrdinalIgnoreCase))
		{
			string text2 = text;
			text = "res://" + text2.Substring(5, text2.Length - 5).TrimStart('/');
		}
		else
		{
			while (text.Contains("//", StringComparison.Ordinal))
			{
				text = text.Replace("//", "/", StringComparison.Ordinal);
			}
		}
		return text.ToLowerInvariant();
	}

	private static void CollectGraph(StateChartState state, List<StateChartState> states, List<Transition> transitions)
	{
		states.Add(state);
		foreach (Node child in state.GetChildren())
		{
			if (child is Transition item)
			{
				transitions.Add(item);
			}
		}
		foreach (Node child2 in state.GetChildren())
		{
			if (child2 is StateChartState state2)
			{
				CollectGraph(state2, states, transitions);
			}
		}
	}

	private static void Preflight(StateChart chart, List<StateChartState> states, List<Transition> transitions, List<StateMachineDiagnostic> diagnostics)
	{
		for (int i = 0; i < states.Count; i++)
		{
			StateChartState stateChartState = states[i];
			string legacyPath = GetLegacyPath(chart, stateChartState);
			StateMachineStateKind stateKind = GetStateKind(stateChartState);
			if ((uint)(stateKind - 2) <= 1u)
			{
				diagnostics.Add(Error("SMI002", "UnsupportedLegacyState", $"Legacy state kind '{stateKind}' is not supported by this Resource runtime gate.", legacyPath));
			}
			if (stateChartState is CompoundState compoundState)
			{
				StateChartState nodeOrNull = compoundState.GetNodeOrNull<StateChartState>(compoundState.initialState);
				if (nodeOrNull == null || nodeOrNull.GetParent() != compoundState)
				{
					diagnostics.Add(Error("SMI006", "InvalidLegacyInitialState", "Compound initialState must resolve to a direct child state.", legacyPath));
				}
			}
		}
		for (int j = 0; j < transitions.Count; j++)
		{
			Transition transition = transitions[j];
			string legacyPath2 = GetLegacyPath(chart, transition);
			if (transition.guard != null)
			{
				diagnostics.Add(Error("SMI003", "UnsupportedLegacyGuard", "Legacy guard '" + transition.guard.GetType().Name + "' is not supported by this Resource runtime gate.", legacyPath2));
			}
			if (!double.TryParse(transition.delayInSeconds, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) || !double.IsFinite(result) || result < 0.0)
			{
				diagnostics.Add(Error("SMI004", "UnsupportedDelayExpression", "Only finite, non-negative constant legacy delays can be imported before expression support is enabled.", legacyPath2));
			}
			if (((transition.to == null || transition.to.IsEmpty) ? null : transition.GetNodeOrNull<StateChartState>(transition.to)) == null)
			{
				diagnostics.Add(Error("SMI005", "UnresolvedLegacyTarget", "Legacy transition target cannot be resolved.", legacyPath2));
			}
		}
	}

	private static Godot.Collections.Dictionary<string, string> BuildStableIdMap(StateChart chart, string normalizedSourcePath, List<StateChartState> states, List<Transition> transitions, Godot.Collections.Dictionary<string, string> persistedImportMap, List<StateMachineDiagnostic> diagnostics)
	{
		Godot.Collections.Dictionary<string, string> dictionary = new Godot.Collections.Dictionary<string, string>();
		HashSet<string> usedStableIds = new HashSet<string>(StringComparer.Ordinal);
		for (int i = 0; i < states.Count; i++)
		{
			AddStableId(chart, normalizedSourcePath, states[i], "state", persistedImportMap, dictionary, usedStableIds, diagnostics);
		}
		for (int j = 0; j < transitions.Count; j++)
		{
			AddStableId(chart, normalizedSourcePath, transitions[j], "transition", persistedImportMap, dictionary, usedStableIds, diagnostics);
		}
		return dictionary;
	}

	private static void AddStableId(StateChart chart, string normalizedSourcePath, Node legacyNode, string kind, Godot.Collections.Dictionary<string, string> persistedImportMap, Godot.Collections.Dictionary<string, string> map, HashSet<string> usedStableIds, List<StateMachineDiagnostic> diagnostics)
	{
		string legacyPath = GetLegacyPath(chart, legacyNode);
		string text = null;
		if (persistedImportMap != null && persistedImportMap.TryGetValue(legacyPath, out var value) && !string.IsNullOrWhiteSpace(value))
		{
			text = value;
		}
		if (text == null)
		{
			text = "legacy." + Slug(legacyNode.Name.ToString(), kind) + "." + Hash128Hex(normalizedSourcePath + "|" + legacyPath + "|" + legacyNode.GetType().Name);
		}
		if (!usedStableIds.Add(text))
		{
			diagnostics.Add(Error("SMI007", "DuplicatePersistedStableId", "Persisted import stable ID '" + text + "' is duplicated.", legacyPath));
		}
		else
		{
			map[legacyPath] = text;
		}
	}

	private static StateMachineStateKind GetStateKind(StateChartState state)
	{
		if (state is CompoundState)
		{
			return StateMachineStateKind.Compound;
		}
		if (state is ParallelState)
		{
			return StateMachineStateKind.Parallel;
		}
		if (state is HistoryState)
		{
			return StateMachineStateKind.History;
		}
		return StateMachineStateKind.Atomic;
	}

	private static int GetStateDepth(StateChartState state)
	{
		int num = 0;
		Node parent = state.GetParent();
		while (parent is StateChartState)
		{
			num++;
			parent = parent.GetParent();
		}
		return num;
	}

	private static string GetLegacyPath(StateChart chart, Node node)
	{
		if (node == null)
		{
			return string.Empty;
		}
		return chart.GetPathTo(node).ToString().Replace('\\', '/')
			.Trim('/');
	}

	private static string Slug(string value, string fallback)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return fallback;
		}
		StringBuilder stringBuilder = new StringBuilder(Math.Min(value.Length, 32));
		for (int i = 0; i < value.Length; i++)
		{
			if (stringBuilder.Length >= 32)
			{
				break;
			}
			char c = char.ToLowerInvariant(value[i]);
			if (char.IsLetterOrDigit(c))
			{
				stringBuilder.Append(c);
			}
			else if (stringBuilder.Length > 0)
			{
				if (stringBuilder[stringBuilder.Length - 1] != '-')
				{
					stringBuilder.Append('-');
				}
			}
		}
		string text = stringBuilder.ToString().Trim('-');
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return fallback;
	}

	private static string Hash128Hex(string value)
	{
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)), 0, 16).ToLowerInvariant();
	}

	private static bool ContainsErrors(List<StateMachineDiagnostic> diagnostics)
	{
		for (int i = 0; i < diagnostics.Count; i++)
		{
			if (diagnostics[i].Severity == StateMachineDiagnosticSeverity.Error)
			{
				return true;
			}
		}
		return false;
	}

	private static StateMachineSceneImportResult BlockingResult(List<StateMachineDiagnostic> diagnostics)
	{
		return new StateMachineSceneImportResult
		{
			Definition = null,
			Diagnostics = new StateMachineValidationResult(diagnostics)
		};
	}

	private static StateMachineDiagnostic Error(string code, string name, string message, string stableId = "")
	{
		return new StateMachineDiagnostic(code, name, StateMachineDiagnosticSeverity.Error, message, stableId);
	}
}
