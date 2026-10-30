using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public static class StateMachineValidator
{
	private readonly struct AutomaticEdge(int transitionIndex, int targetIndex)
	{
		public int TransitionIndex { get; } = transitionIndex;

		public int TargetIndex { get; } = targetIndex;
	}

	public static StateMachineValidationResult Validate(StateMachineDefinition definition)
	{
		if (!StateMachineDefinitionComposer.TryCompose(definition, out var composed, out var validation))
		{
			return validation;
		}
		try
		{
			return ValidateComposed(composed);
		}
		finally
		{
			StateMachineCompiler.DisposeTemporaryDefinition(composed, disposeGuardTrees: false);
		}
	}

	internal static StateMachineValidationResult ValidateComposed(StateMachineDefinition definition)
	{
		List<StateMachineDiagnostic> list = new List<StateMachineDiagnostic>();
		if (definition == null)
		{
			list.Add(Error("SM001", "MissingDefinition", "State machine definition is missing."));
			return new StateMachineValidationResult(list);
		}
		Array<StateMachineStateDefinition> states = definition.States;
		Array<StateMachineTransitionDefinition> transitions = definition.Transitions;
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		if (states != null)
		{
			for (int i = 0; i < states.Count; i++)
			{
				StateMachineStateDefinition stateMachineStateDefinition = states[i];
				if (stateMachineStateDefinition != null && !string.IsNullOrWhiteSpace(stateMachineStateDefinition.StableId) && !dictionary.ContainsKey(stateMachineStateDefinition.StableId))
				{
					dictionary.Add(stateMachineStateDefinition.StableId, i);
				}
			}
		}
		int value = -1;
		bool flag = !string.IsNullOrWhiteSpace(definition.RootStateId) && dictionary.TryGetValue(definition.RootStateId, out value);
		StateMachineStateDefinition stateMachineStateDefinition2 = (flag ? states[value] : null);
		if (!flag || stateMachineStateDefinition2 == null || !string.IsNullOrWhiteSpace(stateMachineStateDefinition2.ParentId))
		{
			list.Add(Error("SM003", "InvalidRoot", "RootStateId must resolve to exactly one top-level state.", definition.RootStateId));
		}
		if (states != null)
		{
			for (int j = 0; j < states.Count; j++)
			{
				StateMachineStateDefinition stateMachineStateDefinition3 = states[j];
				string text = stateMachineStateDefinition3?.StableId ?? string.Empty;
				if (stateMachineStateDefinition3 == null || string.IsNullOrWhiteSpace(text) || !hashSet.Add(text))
				{
					list.Add(Error("SM002", "DuplicateStableId", "State stable IDs must be non-empty and unique.", text));
					continue;
				}
				bool flag2 = string.Equals(text, definition.RootStateId, StringComparison.Ordinal);
				if (!flag2 && !HasValidParentHierarchy(stateMachineStateDefinition3, definition.RootStateId, flag, states, dictionary))
				{
					list.Add(Error("SM004", "InvalidParent", "Every non-root state must have a Compound or Parallel parent chain that terminates at RootStateId.", text));
				}
				if (stateMachineStateDefinition3.Kind == StateMachineStateKind.Compound)
				{
					if (string.IsNullOrWhiteSpace(stateMachineStateDefinition3.InitialChildId) || !dictionary.TryGetValue(stateMachineStateDefinition3.InitialChildId, out var value2) || states[value2] == null || !string.Equals(states[value2].ParentId, text, StringComparison.Ordinal))
					{
						list.Add(Error("SM005", "InvalidInitialChild", "Compound InitialChildId must resolve to a direct child.", text));
					}
				}
				else if (!string.IsNullOrWhiteSpace(stateMachineStateDefinition3.InitialChildId))
				{
					list.Add(Error("SM005", "InvalidInitialChild", "Only Compound states can define InitialChildId.", text));
				}
				if (stateMachineStateDefinition3.Kind == StateMachineStateKind.History)
				{
					int num;
					if (!string.IsNullOrWhiteSpace(stateMachineStateDefinition3.ParentId) && dictionary.TryGetValue(stateMachineStateDefinition3.ParentId, out var value3))
					{
						StateMachineStateDefinition stateMachineStateDefinition4 = states[value3];
						num = ((stateMachineStateDefinition4 != null && stateMachineStateDefinition4.Kind == StateMachineStateKind.Compound) ? 1 : 0);
					}
					else
					{
						num = 0;
					}
					bool flag3 = (byte)num != 0;
					bool flag4 = false;
					for (int k = 0; k < states.Count; k++)
					{
						if (flag4)
						{
							break;
						}
						flag4 = string.Equals(states[k]?.ParentId, text, StringComparison.Ordinal);
					}
					if (!flag3 || flag4 || flag2)
					{
						list.Add(Error("SM012", "InvalidHistory", "History states must be childless markers under a Compound state.", text));
					}
				}
				if (stateMachineStateDefinition3.Kind == StateMachineStateKind.Parallel)
				{
					bool flag5 = false;
					for (int l = 0; l < states.Count; l++)
					{
						if (flag5)
						{
							break;
						}
						int num2;
						if (string.Equals(states[l]?.ParentId, text, StringComparison.Ordinal))
						{
							StateMachineStateDefinition stateMachineStateDefinition5 = states[l];
							num2 = ((stateMachineStateDefinition5 == null || stateMachineStateDefinition5.Kind != StateMachineStateKind.History) ? 1 : 0);
						}
						else
						{
							num2 = 0;
						}
						flag5 = (byte)num2 != 0;
					}
					if (!flag5)
					{
						list.Add(Error("SM005", "InvalidInitialChild", "Parallel states require at least one direct region child.", text));
					}
				}
				ValidateLifecycleCallbackKey(stateMachineStateDefinition3.CallbackKey, "CallbackKey", text, list, allowLegacyMetricLabel: true);
				ValidateLifecycleCallbackKey(stateMachineStateDefinition3.EnterCallbackKey, "EnterCallbackKey", text, list);
				ValidateLifecycleCallbackKey(stateMachineStateDefinition3.ExitCallbackKey, "ExitCallbackKey", text, list);
				ValidateLifecycleCallbackKey(stateMachineStateDefinition3.ProcessCallbackKey, "ProcessCallbackKey", text, list);
				ValidateLifecycleCallbackKey(stateMachineStateDefinition3.PhysicsProcessCallbackKey, "PhysicsProcessCallbackKey", text, list);
			}
		}
		HashSet<int> hashSet2 = FindAutomaticCycleTransitions(transitions, dictionary, states?.Count ?? 0);
		if (transitions != null)
		{
			for (int m = 0; m < transitions.Count; m++)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition = transitions[m];
				string text2 = stateMachineTransitionDefinition?.StableId ?? string.Empty;
				if (stateMachineTransitionDefinition == null || string.IsNullOrWhiteSpace(text2) || !hashSet.Add(text2))
				{
					list.Add(Error("SM002", "DuplicateStableId", "Transition stable IDs must be non-empty and unique across the definition.", text2));
					if (stateMachineTransitionDefinition == null)
					{
						continue;
					}
				}
				if (!dictionary.ContainsKey(stateMachineTransitionDefinition.SourceStateId ?? string.Empty))
				{
					list.Add(Error("SM006", "InvalidTransitionSource", "Transition SourceStateId does not resolve.", text2));
				}
				if (!dictionary.ContainsKey(stateMachineTransitionDefinition.TargetStateId ?? string.Empty))
				{
					list.Add(Error("SM007", "InvalidTransitionTarget", "Transition TargetStateId does not resolve.", text2));
				}
				if (!double.IsFinite(stateMachineTransitionDefinition.DelaySeconds) || stateMachineTransitionDefinition.DelaySeconds < 0.0)
				{
					list.Add(Error("SM008", "NegativeDelay", "Transition DelaySeconds must be finite and non-negative.", text2));
				}
				if (hashSet2.Contains(m))
				{
					list.Add(Error("SM009", "AutomaticCycle", "Zero-delay automatic transitions cannot form a cycle.", text2));
				}
				if (stateMachineTransitionDefinition.TriggerKind == StateMachineTriggerKind.Delay && stateMachineTransitionDefinition.DelaySeconds <= 0.0)
				{
					list.Add(Error("SM008", "NegativeDelay", "Delay transitions require DelaySeconds greater than zero.", text2));
				}
				Resource guardDefinition = stateMachineTransitionDefinition.GuardDefinition;
				if (guardDefinition != null && !(guardDefinition is StateMachineGuardDefinition))
				{
					list.Add(Error("SM010", "UnsupportedGuard", "GuardDefinition must be a StateMachineGuardDefinition resource.", text2));
				}
				if (stateMachineTransitionDefinition.GuardDefinition is StateMachineGuardDefinition root)
				{
					ValidateGuardTree(root, text2, list);
				}
			}
		}
		return new StateMachineValidationResult(list);
	}

	private static void ValidateLifecycleCallbackKey(StringName callbackKeyValue, string fieldName, string stableId, List<StateMachineDiagnostic> diagnostics, bool allowLegacyMetricLabel = false)
	{
		string text = callbackKeyValue.ToString();
		if (!string.IsNullOrWhiteSpace(text) && (!allowLegacyMetricLabel || StateMachineCallbackKey.UsesExecutablePrefix(text)) && !StateMachineCallbackKey.TryParse(text, out var _, out var _))
		{
			diagnostics.Add(Error("SM016", "InvalidCallbackKey", fieldName + " must use builtin/<key> or mod/<canonical-mod-id>/<key>." + (allowLegacyMetricLabel ? " Legacy unprefixed CallbackKey values remain metric labels only." : string.Empty), stableId));
		}
	}

	private static void ValidateGuardTree(StateMachineGuardDefinition root, string stableId, List<StateMachineDiagnostic> diagnostics)
	{
		HashSet<ulong> visiting = new HashSet<ulong>();
		int nodes = 0;
		ValidateGuardTreeCore(root, stableId, diagnostics, visiting, 0, ref nodes);
	}

	private static void ValidateGuardTreeCore(StateMachineGuardDefinition guard, string stableId, List<StateMachineDiagnostic> diagnostics, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (guard == null)
		{
			diagnostics.Add(Error("SM015", "InvalidGuardTree", "Guard children cannot be empty.", stableId));
			return;
		}
		if (depth >= 32 || ++nodes > 256)
		{
			diagnostics.Add(Error("SM015", "InvalidGuardTree", "Guard tree exceeds the supported nesting limit.", stableId));
			return;
		}
		ulong instanceId = guard.GetInstanceId();
		if (!visiting.Add(instanceId))
		{
			diagnostics.Add(Error("SM015", "InvalidGuardTree", "Guard children cannot contain a cycle.", stableId));
			return;
		}
		try
		{
			int num = guard.Children?.Count ?? 0;
			if (num > 256 - nodes)
			{
				diagnostics.Add(Error("SM015", "InvalidGuardTree", "Guard tree exceeds the supported node limit.", stableId));
				return;
			}
			if (guard.Kind == StateMachineGuardKind.ExpressionProperty)
			{
				Variant.Type variantType = guard.ExpectedValue.VariantType;
				bool flag = (((ulong)variantType <= 4uL || variantType == Variant.Type.StringName) ? true : false);
				bool flag2 = flag;
				if (num != 0 || string.IsNullOrWhiteSpace(guard.ComparedProperty.ToString()) || !flag2)
				{
					diagnostics.Add(Error("SM013", "InvalidGuard", "Expression guards require a property name, scalar expected value, and no children.", stableId));
				}
				return;
			}
			if (guard.Kind == StateMachineGuardKind.Callback)
			{
				if (num != 0 || !StateMachineCallbackKey.TryParse(guard.CallbackKey.ToString(), out var _, out var _))
				{
					diagnostics.Add(Error("SM013", "InvalidGuard", "Callback guards require a valid full callback key and no children.", stableId));
				}
				return;
			}
			bool flag3 = ((guard.Kind == StateMachineGuardKind.Not) ? (num == 1) : (num >= 1));
			StateMachineGuardKind kind = guard.Kind;
			if ((kind != StateMachineGuardKind.All && kind != StateMachineGuardKind.Any && kind != StateMachineGuardKind.Not) || !flag3)
			{
				diagnostics.Add(Error("SM015", "InvalidGuardTree", "All/Any require at least one child and Not requires exactly one child.", stableId));
				return;
			}
			for (int i = 0; i < num; i++)
			{
				ValidateGuardTreeCore(guard.Children[i] as StateMachineGuardDefinition, stableId, diagnostics, visiting, depth + 1, ref nodes);
			}
		}
		finally
		{
			visiting.Remove(instanceId);
		}
	}

	private static bool HasValidParentHierarchy(StateMachineStateDefinition state, string rootStateId, bool rootExists, Array<StateMachineStateDefinition> states, System.Collections.Generic.Dictionary<string, int> stateIndexes)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { state.StableId };
		StateMachineStateDefinition stateMachineStateDefinition = state;
		while (true)
		{
			string text = stateMachineStateDefinition.ParentId ?? string.Empty;
			if (string.IsNullOrWhiteSpace(text))
			{
				return !rootExists;
			}
			if (!hashSet.Add(text) || !stateIndexes.TryGetValue(text, out var value))
			{
				return false;
			}
			StateMachineStateDefinition stateMachineStateDefinition2 = states[value];
			if (stateMachineStateDefinition2 == null || (stateMachineStateDefinition2.Kind != StateMachineStateKind.Compound && stateMachineStateDefinition2.Kind != StateMachineStateKind.Parallel))
			{
				return false;
			}
			if (rootExists && string.Equals(text, rootStateId, StringComparison.Ordinal))
			{
				break;
			}
			stateMachineStateDefinition = stateMachineStateDefinition2;
		}
		return true;
	}

	private static HashSet<int> FindAutomaticCycleTransitions(Array<StateMachineTransitionDefinition> transitions, System.Collections.Generic.Dictionary<string, int> stateIndexes, int stateSlotCount)
	{
		HashSet<int> hashSet = new HashSet<int>();
		if (transitions == null || transitions.Count == 0 || stateSlotCount == 0)
		{
			return hashSet;
		}
		List<AutomaticEdge>[] array = new List<AutomaticEdge>[stateSlotCount];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = new List<AutomaticEdge>();
		}
		for (int j = 0; j < transitions.Count; j++)
		{
			StateMachineTransitionDefinition stateMachineTransitionDefinition = transitions[j];
			if (stateMachineTransitionDefinition != null && stateMachineTransitionDefinition.TriggerKind == StateMachineTriggerKind.Automatic && double.IsFinite(stateMachineTransitionDefinition.DelaySeconds) && stateMachineTransitionDefinition.DelaySeconds == 0.0 && stateIndexes.TryGetValue(stateMachineTransitionDefinition.SourceStateId ?? string.Empty, out var value) && stateIndexes.TryGetValue(stateMachineTransitionDefinition.TargetStateId ?? string.Empty, out var value2))
			{
				array[value].Add(new AutomaticEdge(j, value2));
			}
		}
		byte[] array2 = new byte[array.Length];
		for (int k = 0; k < array.Length; k++)
		{
			if (array2[k] == 0)
			{
				VisitAutomaticEdges(k, array, array2, hashSet);
			}
		}
		return hashSet;
	}

	private static void VisitAutomaticEdges(int stateIndex, List<AutomaticEdge>[] adjacency, byte[] colors, HashSet<int> cycleTransitionIndices)
	{
		colors[stateIndex] = 1;
		List<AutomaticEdge> list = adjacency[stateIndex];
		for (int i = 0; i < list.Count; i++)
		{
			AutomaticEdge automaticEdge = list[i];
			if (colors[automaticEdge.TargetIndex] == 1)
			{
				cycleTransitionIndices.Add(automaticEdge.TransitionIndex);
			}
			else if (colors[automaticEdge.TargetIndex] == 0)
			{
				VisitAutomaticEdges(automaticEdge.TargetIndex, adjacency, colors, cycleTransitionIndices);
			}
		}
		colors[stateIndex] = 2;
	}

	private static StateMachineDiagnostic Error(string code, string name, string message, string stableId = "")
	{
		return new StateMachineDiagnostic(code, name, StateMachineDiagnosticSeverity.Error, message, stableId);
	}
}
