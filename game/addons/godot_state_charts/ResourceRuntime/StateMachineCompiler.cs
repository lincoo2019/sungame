using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Godot;
using Godot.Collections;

public static class StateMachineCompiler
{
	internal sealed class GuardSnapshot
	{
		public StateMachineGuardKind Kind;

		public StringName PropertyName;

		public StateMachineComparisonOperator Operator;

		public Variant ExpectedValue;

		public bool Negate;

		public StringName CallbackKey;

		public GuardSnapshot[] Children = System.Array.Empty<GuardSnapshot>();

		public bool HasCycle;

		public bool ExceededLimit;
	}

	internal readonly struct StateSnapshot(StateMachineStateDefinition state)
	{
		public readonly bool IsNull = state == null;

		public readonly string StableId = state?.StableId ?? string.Empty;

		public readonly StringName DisplayName = state?.DisplayName ?? new StringName();

		public readonly StateMachineStateKind Kind = state?.Kind ?? StateMachineStateKind.Atomic;

		public readonly string ParentId = state?.ParentId ?? string.Empty;

		public readonly string InitialChildId = state?.InitialChildId ?? string.Empty;

		public readonly StateMachineProcessFlags ProcessFlags = state?.ProcessFlags ?? StateMachineProcessFlags.None;

		public readonly StringName CallbackKey = state?.CallbackKey ?? new StringName();

		public readonly StringName EnterCallbackKey = state?.EnterCallbackKey ?? new StringName();

		public readonly StringName ExitCallbackKey = state?.ExitCallbackKey ?? new StringName();

		public readonly StringName ProcessCallbackKey = state?.ProcessCallbackKey ?? new StringName();

		public readonly StringName PhysicsProcessCallbackKey = state?.PhysicsProcessCallbackKey ?? new StringName();

		public readonly string ExtensionTypeId = state?.GetType().FullName ?? string.Empty;

		public readonly StateMachineExtensionProperty[] ExtensionProperties = StateMachineExtensionProperties.CaptureStorage(state);
	}

	internal readonly struct TransitionSnapshot(StateMachineTransitionDefinition transition)
	{
		public readonly bool IsNull = transition == null;

		public readonly string StableId = transition?.StableId ?? string.Empty;

		public readonly string SourceStateId = transition?.SourceStateId ?? string.Empty;

		public readonly string TargetStateId = transition?.TargetStateId ?? string.Empty;

		public readonly StateMachineTriggerKind TriggerKind = transition?.TriggerKind ?? StateMachineTriggerKind.Event;

		public readonly StringName EventName = transition?.EventName ?? new StringName();

		public readonly double DelaySeconds = transition?.DelaySeconds ?? 0.0;

		public readonly int Priority = transition?.Priority ?? 0;

		public readonly int DeclarationOrder = transition?.DeclarationOrder ?? 0;

		public readonly GuardSnapshot Guard = CaptureGuard(transition?.GuardDefinition as StateMachineGuardDefinition);

		public readonly string ExtensionTypeId = transition?.GetType().FullName ?? string.Empty;

		public readonly StateMachineExtensionProperty[] ExtensionProperties = StateMachineExtensionProperties.CaptureStorage(transition);
	}

	internal sealed class CompilationSnapshot
	{
		public int SchemaVersion;

		public string DefinitionId;

		public string RootStateId;

		public string ResourcePath;

		public ulong InstanceId;

		public StateSnapshot[] States;

		public TransitionSnapshot[] Transitions;

		public KeyValuePair<string, string>[] Aliases;

		public StateMachineDefinition CreateValidationDefinition()
		{
			StateMachineDefinition stateMachineDefinition = new StateMachineDefinition
			{
				SchemaVersion = SchemaVersion,
				DefinitionId = DefinitionId,
				RootStateId = RootStateId
			};
			for (int i = 0; i < States.Length; i++)
			{
				StateSnapshot stateSnapshot = States[i];
				if (stateSnapshot.IsNull)
				{
					stateMachineDefinition.States.Add(null);
					continue;
				}
				stateMachineDefinition.States.Add(new StateMachineStateDefinition
				{
					StableId = stateSnapshot.StableId,
					DisplayName = stateSnapshot.DisplayName,
					Kind = stateSnapshot.Kind,
					ParentId = stateSnapshot.ParentId,
					InitialChildId = stateSnapshot.InitialChildId,
					ProcessFlags = stateSnapshot.ProcessFlags,
					CallbackKey = stateSnapshot.CallbackKey,
					EnterCallbackKey = stateSnapshot.EnterCallbackKey,
					ExitCallbackKey = stateSnapshot.ExitCallbackKey,
					ProcessCallbackKey = stateSnapshot.ProcessCallbackKey,
					PhysicsProcessCallbackKey = stateSnapshot.PhysicsProcessCallbackKey
				});
			}
			for (int j = 0; j < Transitions.Length; j++)
			{
				TransitionSnapshot transitionSnapshot = Transitions[j];
				if (transitionSnapshot.IsNull)
				{
					stateMachineDefinition.Transitions.Add(null);
					continue;
				}
				stateMachineDefinition.Transitions.Add(new StateMachineTransitionDefinition
				{
					StableId = transitionSnapshot.StableId,
					SourceStateId = transitionSnapshot.SourceStateId,
					TargetStateId = transitionSnapshot.TargetStateId,
					TriggerKind = transitionSnapshot.TriggerKind,
					EventName = transitionSnapshot.EventName,
					DelaySeconds = transitionSnapshot.DelaySeconds,
					Priority = transitionSnapshot.Priority,
					DeclarationOrder = transitionSnapshot.DeclarationOrder,
					GuardDefinition = CreateGuardDefinition(transitionSnapshot.Guard)
				});
			}
			for (int k = 0; k < Aliases.Length; k++)
			{
				stateMachineDefinition.Aliases[Aliases[k].Key] = Aliases[k].Value;
			}
			return stateMachineDefinition;
		}
	}

	private sealed class TransitionCandidate
	{
		public TransitionSnapshot Definition;

		public int SourceDepth;
	}

	public const int CompilerVersion = 6;

	internal const int MaximumGuardDepth = 32;

	internal const int MaximumGuardNodes = 256;

	private static long _nextProgramGeneration;

	public static bool TryCompile(StateMachineDefinition definition, out StateMachineProgram program, out StateMachineValidationResult validation)
	{
		if (!TryPrepare(definition, out validation, out var snapshot, out var contentHash))
		{
			program = null;
			return false;
		}
		program = CompileValidated(snapshot, contentHash);
		return true;
	}

	internal static bool TryPrepare(StateMachineDefinition definition, out StateMachineValidationResult validation, out CompilationSnapshot snapshot, out string contentHash)
	{
		if (!StateMachineDefinitionComposer.TryCompose(definition, out var composed, out validation))
		{
			snapshot = null;
			contentHash = string.Empty;
			return false;
		}
		try
		{
			snapshot = CaptureSnapshot(composed);
		}
		finally
		{
			DisposeTemporaryDefinition(composed, disposeGuardTrees: false);
		}
		if (definition != null)
		{
			snapshot.ResourcePath = definition.ResourcePath ?? string.Empty;
			snapshot.InstanceId = definition.GetInstanceId();
		}
		StateMachineDefinition definition2 = snapshot?.CreateValidationDefinition();
		try
		{
			validation = StateMachineValidator.ValidateComposed(definition2);
		}
		finally
		{
			DisposeTemporaryDefinition(definition2, disposeGuardTrees: true);
		}
		if (HasInvalidGuardSnapshot(snapshot, out var error))
		{
			List<StateMachineDiagnostic> diagnostics = new List<StateMachineDiagnostic>(validation.Diagnostics)
			{
				new StateMachineDiagnostic("SM015", "InvalidGuardTree", StateMachineDiagnosticSeverity.Error, error)
			};
			validation = new StateMachineValidationResult(diagnostics);
		}
		if (!validation.IsValid)
		{
			contentHash = string.Empty;
			return false;
		}
		contentHash = ComputeContentHash(snapshot);
		return true;
	}

	internal static void DisposeTemporaryDefinition(StateMachineDefinition definition, bool disposeGuardTrees)
	{
		if (!GodotObject.IsInstanceValid(definition))
		{
			return;
		}
		if (definition.States != null)
		{
			for (int i = 0; i < definition.States.Count; i++)
			{
				definition.States[i]?.Dispose();
			}
			definition.States.Clear();
		}
		if (definition.Transitions != null)
		{
			HashSet<ulong> disposed = (disposeGuardTrees ? new HashSet<ulong>() : null);
			for (int j = 0; j < definition.Transitions.Count; j++)
			{
				StateMachineTransitionDefinition stateMachineTransitionDefinition = definition.Transitions[j];
				if (stateMachineTransitionDefinition != null)
				{
					if (disposeGuardTrees && stateMachineTransitionDefinition.GuardDefinition is StateMachineGuardDefinition guard)
					{
						DisposeTemporaryGuardTree(guard, disposed);
					}
					stateMachineTransitionDefinition.GuardDefinition = null;
					stateMachineTransitionDefinition.Dispose();
				}
			}
			definition.Transitions.Clear();
		}
		definition.Dispose();
	}

	private static void DisposeTemporaryGuardTree(StateMachineGuardDefinition guard, HashSet<ulong> disposed)
	{
		if (!GodotObject.IsInstanceValid(guard) || disposed == null || !disposed.Add(guard.GetInstanceId()))
		{
			return;
		}
		if (guard.Children != null)
		{
			for (int i = 0; i < guard.Children.Count; i++)
			{
				DisposeTemporaryGuardTree(guard.Children[i] as StateMachineGuardDefinition, disposed);
			}
			guard.Children.Clear();
		}
		guard.Dispose();
	}

	private static bool HasInvalidGuardSnapshot(CompilationSnapshot snapshot, out string error)
	{
		error = string.Empty;
		if (snapshot?.Transitions == null)
		{
			return false;
		}
		for (int i = 0; i < snapshot.Transitions.Length; i++)
		{
			if (FindInvalidGuardSnapshot(snapshot.Transitions[i].Guard, out error))
			{
				return true;
			}
		}
		return false;
	}

	private static bool FindInvalidGuardSnapshot(GuardSnapshot guard, out string error)
	{
		error = string.Empty;
		if (guard == null)
		{
			return false;
		}
		if (guard.HasCycle)
		{
			error = "Guard children cannot contain a cycle.";
			return true;
		}
		if (guard.ExceededLimit)
		{
			error = $"Guard nesting is limited to {32} levels and {256} nodes.";
			return true;
		}
		for (int i = 0; i < guard.Children.Length; i++)
		{
			if (FindInvalidGuardSnapshot(guard.Children[i], out error))
			{
				return true;
			}
		}
		return false;
	}

	internal static StateMachineProgram CompileValidated(CompilationSnapshot snapshot, string contentHash)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(snapshot.States.Length, StringComparer.Ordinal);
		for (int i = 0; i < snapshot.States.Length; i++)
		{
			dictionary.Add(snapshot.States[i].StableId, i);
		}
		int[] array = new int[snapshot.States.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = ComputeStateDepth(j, snapshot, dictionary);
		}
		CompiledStateMachineState[] array2 = new CompiledStateMachineState[snapshot.States.Length];
		for (int k = 0; k < array2.Length; k++)
		{
			StateSnapshot stateSnapshot = snapshot.States[k];
			int parentIndex = (string.IsNullOrWhiteSpace(stateSnapshot.ParentId) ? (-1) : dictionary[stateSnapshot.ParentId]);
			int initialChildIndex = (string.IsNullOrWhiteSpace(stateSnapshot.InitialChildId) ? (-1) : dictionary[stateSnapshot.InitialChildId]);
			array2[k] = new CompiledStateMachineState(stateSnapshot.StableId, stateSnapshot.DisplayName, stateSnapshot.Kind, parentIndex, initialChildIndex, array[k], stateSnapshot.ProcessFlags, stateSnapshot.CallbackKey, stateSnapshot.EnterCallbackKey, stateSnapshot.ExitCallbackKey, stateSnapshot.ProcessCallbackKey, stateSnapshot.PhysicsProcessCallbackKey, stateSnapshot.ExtensionProperties);
		}
		List<TransitionCandidate> list = BuildSortedTransitionCandidates(snapshot, dictionary, array);
		CompiledStateMachineTransition[] array3 = new CompiledStateMachineTransition[list.Count];
		List<int> list2 = new List<int>(list.Count);
		SortedDictionary<string, List<int>> sortedDictionary = new SortedDictionary<string, List<int>>(StringComparer.Ordinal);
		System.Collections.Generic.Dictionary<string, int> callbackIndices = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		List<StringName> list3 = new List<StringName>();
		for (int l = 0; l < list.Count; l++)
		{
			TransitionSnapshot definition = list[l].Definition;
			array3[l] = new CompiledStateMachineTransition(dictionary[definition.SourceStateId], dictionary[definition.TargetStateId], definition.TriggerKind, definition.EventName, definition.DelaySeconds, definition.Priority, definition.DeclarationOrder, definition.StableId, CompileGuard(definition.Guard, callbackIndices, list3), definition.ExtensionProperties);
			StateMachineTriggerKind triggerKind = definition.TriggerKind;
			if ((uint)(triggerKind - 1) <= 1u)
			{
				list2.Add(l);
				continue;
			}
			string key = definition.EventName.ToString();
			if (!sortedDictionary.TryGetValue(key, out var value))
			{
				value = new List<int>();
				sortedDictionary.Add(key, value);
			}
			value.Add(l);
		}
		List<int> list4 = new List<int>(list.Count);
		System.Collections.Generic.Dictionary<StringName, StateMachineTransitionSlice> dictionary2 = new System.Collections.Generic.Dictionary<StringName, StateMachineTransitionSlice>();
		foreach (KeyValuePair<string, List<int>> item in sortedDictionary)
		{
			int count = list4.Count;
			list4.AddRange(item.Value);
			dictionary2.Add(new StringName(item.Key), new StateMachineTransitionSlice(count, item.Value.Count));
		}
		System.Collections.Generic.Dictionary<string, string> dictionary3 = new System.Collections.Generic.Dictionary<string, string>(snapshot.Aliases.Length, StringComparer.Ordinal);
		for (int m = 0; m < snapshot.Aliases.Length; m++)
		{
			dictionary3[snapshot.Aliases[m].Key] = snapshot.Aliases[m].Value;
		}
		long programGeneration = Interlocked.Increment(ref _nextProgramGeneration);
		return new StateMachineProgram(snapshot.DefinitionId, snapshot.SchemaVersion, contentHash, programGeneration, dictionary[snapshot.RootStateId], array2, array3, list4.ToArray(), list2.ToArray(), list3.ToArray(), dictionary2, dictionary, dictionary3);
	}

	internal static string ComputeContentHash(CompilationSnapshot snapshot)
	{
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(snapshot.States.Length, StringComparer.Ordinal);
		for (int i = 0; i < snapshot.States.Length; i++)
		{
			dictionary.Add(snapshot.States[i].StableId, i);
		}
		int[] array = new int[snapshot.States.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = ComputeStateDepth(j, snapshot, dictionary);
		}
		List<TransitionCandidate> list = BuildSortedTransitionCandidates(snapshot, dictionary, array);
		using MemoryStream memoryStream = new MemoryStream(4096);
		using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
		{
			binaryWriter.Write(6);
			binaryWriter.Write(7);
			WriteString(binaryWriter, snapshot.DefinitionId);
			binaryWriter.Write(snapshot.SchemaVersion);
			WriteString(binaryWriter, snapshot.RootStateId);
			binaryWriter.Write(snapshot.States.Length);
			for (int k = 0; k < snapshot.States.Length; k++)
			{
				StateSnapshot stateSnapshot = snapshot.States[k];
				WriteString(binaryWriter, stateSnapshot.StableId);
				WriteString(binaryWriter, stateSnapshot.DisplayName.ToString());
				binaryWriter.Write((int)stateSnapshot.Kind);
				WriteString(binaryWriter, stateSnapshot.ParentId);
				WriteString(binaryWriter, stateSnapshot.InitialChildId);
				binaryWriter.Write((int)stateSnapshot.ProcessFlags);
				WriteString(binaryWriter, stateSnapshot.CallbackKey.ToString());
				WriteString(binaryWriter, stateSnapshot.EnterCallbackKey.ToString());
				WriteString(binaryWriter, stateSnapshot.ExitCallbackKey.ToString());
				WriteString(binaryWriter, stateSnapshot.ProcessCallbackKey.ToString());
				WriteString(binaryWriter, stateSnapshot.PhysicsProcessCallbackKey.ToString());
				WriteString(binaryWriter, stateSnapshot.ExtensionTypeId);
				WriteExtensionProperties(binaryWriter, stateSnapshot.ExtensionProperties);
			}
			binaryWriter.Write(list.Count);
			for (int l = 0; l < list.Count; l++)
			{
				TransitionSnapshot definition = list[l].Definition;
				WriteString(binaryWriter, definition.StableId);
				WriteString(binaryWriter, definition.SourceStateId);
				WriteString(binaryWriter, definition.TargetStateId);
				binaryWriter.Write((int)definition.TriggerKind);
				WriteString(binaryWriter, definition.EventName.ToString());
				binaryWriter.Write(BitConverter.DoubleToInt64Bits(definition.DelaySeconds));
				binaryWriter.Write(definition.Guard != null);
				if (definition.Guard != null)
				{
					WriteGuardSnapshot(binaryWriter, definition.Guard);
				}
				binaryWriter.Write(definition.Priority);
				binaryWriter.Write(definition.DeclarationOrder);
				WriteString(binaryWriter, definition.ExtensionTypeId);
				WriteExtensionProperties(binaryWriter, definition.ExtensionProperties);
			}
			binaryWriter.Write(snapshot.Aliases.Length);
			for (int m = 0; m < snapshot.Aliases.Length; m++)
			{
				WriteString(binaryWriter, snapshot.Aliases[m].Key);
				WriteString(binaryWriter, snapshot.Aliases[m].Value);
			}
		}
		memoryStream.Position = 0L;
		return Convert.ToHexString(SHA256.HashData(memoryStream));
	}

	private static CompilationSnapshot CaptureSnapshot(StateMachineDefinition definition)
	{
		if (definition == null)
		{
			return null;
		}
		int num = definition.States?.Count ?? 0;
		StateSnapshot[] array = new StateSnapshot[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new StateSnapshot(definition.States[i]);
		}
		int num2 = definition.Transitions?.Count ?? 0;
		TransitionSnapshot[] array2 = new TransitionSnapshot[num2];
		for (int j = 0; j < num2; j++)
		{
			array2[j] = new TransitionSnapshot(definition.Transitions[j]);
		}
		List<KeyValuePair<string, string>> list = new List<KeyValuePair<string, string>>();
		if (definition.Aliases != null)
		{
			foreach (string key in definition.Aliases.Keys)
			{
				list.Add(new KeyValuePair<string, string>(key ?? string.Empty, definition.Aliases[key] ?? string.Empty));
			}
		}
		list.Sort((KeyValuePair<string, string> left, KeyValuePair<string, string> right) => string.Compare(left.Key, right.Key, StringComparison.Ordinal));
		return new CompilationSnapshot
		{
			SchemaVersion = definition.SchemaVersion,
			DefinitionId = (definition.DefinitionId ?? string.Empty),
			RootStateId = (definition.RootStateId ?? string.Empty),
			ResourcePath = (definition.ResourcePath ?? string.Empty),
			InstanceId = definition.GetInstanceId(),
			States = array,
			Transitions = array2,
			Aliases = list.ToArray()
		};
	}

	private static GuardSnapshot CaptureGuard(StateMachineGuardDefinition guard)
	{
		if (guard == null)
		{
			return null;
		}
		HashSet<ulong> visiting = new HashSet<ulong>();
		int nodes = 0;
		return CaptureGuardCore(guard, visiting, 0, ref nodes);
	}

	private static GuardSnapshot CaptureGuardCore(StateMachineGuardDefinition guard, HashSet<ulong> visiting, int depth, ref int nodes)
	{
		if (guard == null)
		{
			return null;
		}
		GuardSnapshot guardSnapshot = new GuardSnapshot
		{
			Kind = guard.Kind,
			PropertyName = guard.ComparedProperty,
			Operator = guard.Operator,
			ExpectedValue = guard.ExpectedValue,
			Negate = guard.Negate,
			CallbackKey = guard.CallbackKey
		};
		ulong instanceId = guard.GetInstanceId();
		if (depth >= 32 || ++nodes > 256)
		{
			guardSnapshot.ExceededLimit = true;
			return guardSnapshot;
		}
		if (!visiting.Add(instanceId))
		{
			guardSnapshot.HasCycle = true;
			return guardSnapshot;
		}
		try
		{
			int num = guard.Children?.Count ?? 0;
			int num2 = 256 - nodes;
			if (num > num2)
			{
				guardSnapshot.ExceededLimit = true;
				return guardSnapshot;
			}
			if (num == 0)
			{
				return guardSnapshot;
			}
			guardSnapshot.Children = new GuardSnapshot[num];
			for (int i = 0; i < num; i++)
			{
				guardSnapshot.Children[i] = CaptureGuardCore(guard.Children[i] as StateMachineGuardDefinition, visiting, depth + 1, ref nodes);
			}
			return guardSnapshot;
		}
		finally
		{
			visiting.Remove(instanceId);
		}
	}

	private static StateMachineGuardDefinition CreateGuardDefinition(GuardSnapshot snapshot)
	{
		if (snapshot == null)
		{
			return null;
		}
		StateMachineGuardDefinition stateMachineGuardDefinition = new StateMachineGuardDefinition
		{
			Kind = snapshot.Kind,
			ComparedProperty = snapshot.PropertyName,
			Operator = snapshot.Operator,
			ExpectedValue = snapshot.ExpectedValue,
			Negate = snapshot.Negate,
			CallbackKey = snapshot.CallbackKey
		};
		for (int i = 0; i < snapshot.Children.Length; i++)
		{
			stateMachineGuardDefinition.Children.Add(CreateGuardDefinition(snapshot.Children[i]));
		}
		return stateMachineGuardDefinition;
	}

	private static CompiledStateMachineGuard CompileGuard(GuardSnapshot snapshot, System.Collections.Generic.Dictionary<string, int> callbackIndices, List<StringName> callbackKeys)
	{
		if (snapshot == null)
		{
			return default(CompiledStateMachineGuard);
		}
		CompiledStateMachineGuard[] array = new CompiledStateMachineGuard[snapshot.Children.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = CompileGuard(snapshot.Children[i], callbackIndices, callbackKeys);
		}
		int value = -1;
		string text = snapshot.CallbackKey.ToString();
		if (snapshot.Kind == StateMachineGuardKind.Callback && !string.IsNullOrWhiteSpace(text) && !callbackIndices.TryGetValue(text, out value))
		{
			value = callbackKeys.Count;
			callbackIndices.Add(text, value);
			callbackKeys.Add(snapshot.CallbackKey);
		}
		return new CompiledStateMachineGuard(isDefined: true, snapshot.Kind, snapshot.PropertyName, snapshot.Operator, snapshot.ExpectedValue, snapshot.Negate, snapshot.CallbackKey, value, array);
	}

	private static void WriteGuardSnapshot(BinaryWriter writer, GuardSnapshot snapshot)
	{
		writer.Write((int)snapshot.Kind);
		WriteString(writer, snapshot.PropertyName.ToString());
		writer.Write((int)snapshot.Operator);
		writer.Write((int)snapshot.ExpectedValue.VariantType);
		WriteGuardVariant(writer, snapshot.ExpectedValue);
		writer.Write(snapshot.Negate);
		WriteString(writer, snapshot.CallbackKey.ToString());
		writer.Write(snapshot.HasCycle);
		writer.Write(snapshot.ExceededLimit);
		writer.Write(snapshot.Children.Length);
		for (int i = 0; i < snapshot.Children.Length; i++)
		{
			WriteGuardSnapshot(writer, snapshot.Children[i]);
		}
	}

	private static List<TransitionCandidate> BuildSortedTransitionCandidates(CompilationSnapshot snapshot, System.Collections.Generic.Dictionary<string, int> stateIndices, int[] depths)
	{
		List<TransitionCandidate> list = new List<TransitionCandidate>(snapshot.Transitions.Length);
		for (int i = 0; i < snapshot.Transitions.Length; i++)
		{
			TransitionSnapshot definition = snapshot.Transitions[i];
			list.Add(new TransitionCandidate
			{
				Definition = definition,
				SourceDepth = depths[stateIndices[definition.SourceStateId]]
			});
		}
		list.Sort(CompareTransitionCandidates);
		return list;
	}

	private static int ComputeStateDepth(int stateIndex, CompilationSnapshot snapshot, System.Collections.Generic.Dictionary<string, int> stateIndices)
	{
		int num = 0;
		StateSnapshot stateSnapshot = snapshot.States[stateIndex];
		while (!string.IsNullOrWhiteSpace(stateSnapshot.ParentId))
		{
			num++;
			stateSnapshot = snapshot.States[stateIndices[stateSnapshot.ParentId]];
		}
		return num;
	}

	private static int CompareTransitionCandidates(TransitionCandidate left, TransitionCandidate right)
	{
		int num = right.SourceDepth.CompareTo(left.SourceDepth);
		if (num != 0)
		{
			return num;
		}
		num = right.Definition.Priority.CompareTo(left.Definition.Priority);
		if (num != 0)
		{
			return num;
		}
		num = left.Definition.DeclarationOrder.CompareTo(right.Definition.DeclarationOrder);
		if (num != 0)
		{
			return num;
		}
		return string.Compare(left.Definition.StableId, right.Definition.StableId, StringComparison.Ordinal);
	}

	private static void WriteString(BinaryWriter writer, string value)
	{
		writer.Write(value ?? string.Empty);
	}

	private static void WriteGuardVariant(BinaryWriter writer, Variant value)
	{
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 1:
				writer.Write(value.AsBool());
				return;
			case 2:
				writer.Write(value.AsInt64());
				return;
			case 3:
				writer.Write(BitConverter.DoubleToInt64Bits(value.AsDouble()));
				return;
			case 4:
				goto IL_0062;
			case 0:
				return;
			}
		}
		if (variantType != Variant.Type.StringName)
		{
			WriteString(writer, value.ToString());
			return;
		}
		goto IL_0062;
		IL_0062:
		WriteString(writer, value.AsString());
	}

	private static void WriteExtensionProperties(BinaryWriter writer, StateMachineExtensionProperty[] properties)
	{
		if (properties == null)
		{
			properties = System.Array.Empty<StateMachineExtensionProperty>();
		}
		writer.Write(properties.Length);
		HashSet<ulong> visitedResources = new HashSet<ulong>();
		for (int i = 0; i < properties.Length; i++)
		{
			StateMachineExtensionProperty stateMachineExtensionProperty = properties[i];
			WriteString(writer, stateMachineExtensionProperty.Name.ToString());
			writer.Write((int)stateMachineExtensionProperty.DeclaredType);
			WriteStableVariant(writer, stateMachineExtensionProperty.Value, visitedResources, 0);
		}
	}

	private static void WriteStableVariant(BinaryWriter writer, Variant value, HashSet<ulong> visitedResources, int depth)
	{
		writer.Write((int)value.VariantType);
		if (depth >= 24)
		{
			WriteString(writer, "<depth-limit>");
			return;
		}
		Variant.Type variantType = value.VariantType;
		if ((ulong)variantType <= 4uL)
		{
			switch ((int)variantType)
			{
			case 0:
				return;
			case 1:
				writer.Write(value.AsBool());
				return;
			case 2:
				writer.Write(value.AsInt64());
				return;
			case 3:
				writer.Write(BitConverter.DoubleToInt64Bits(value.AsDouble()));
				return;
			case 4:
				goto IL_00b5;
			}
		}
		Variant.Type num = variantType - 21;
		if ((ulong)num > 7uL)
		{
			goto IL_03d2;
		}
		switch ((int)num)
		{
		case 0:
		case 1:
			break;
		case 7:
		{
			Godot.Collections.Array array = value.AsGodotArray();
			writer.Write(array.Count);
			for (int num3 = 0; num3 < array.Count; num3++)
			{
				WriteStableVariant(writer, array[num3], visitedResources, depth + 1);
			}
			return;
		}
		case 6:
		{
			Dictionary dictionary = value.AsGodotDictionary();
			List<(byte[], Variant, Variant)> list = new List<(byte[], Variant, Variant)>(dictionary.Count);
			foreach (Variant key in dictionary.Keys)
			{
				using MemoryStream memoryStream = new MemoryStream();
				using (BinaryWriter writer2 = new BinaryWriter(memoryStream, Encoding.UTF8, leaveOpen: true))
				{
					WriteStableVariant(writer2, key, new HashSet<ulong>(), depth + 1);
				}
				list.Add((memoryStream.ToArray(), key, dictionary[key]));
			}
			list.Sort(((byte[] keyBytes, Variant key, Variant item) left, (byte[] keyBytes, Variant key, Variant item) right) => CompareBytes(left.keyBytes, right.keyBytes));
			writer.Write(list.Count);
			for (int num2 = 0; num2 < list.Count; num2++)
			{
				WriteStableVariant(writer, list[num2].Item2, visitedResources, depth + 1);
				WriteStableVariant(writer, list[num2].Item3, visitedResources, depth + 1);
			}
			return;
		}
		case 3:
			goto IL_0218;
		default:
			goto IL_03d2;
		}
		goto IL_00b5;
		IL_03d2:
		WriteString(writer, value.ToString());
		return;
		IL_00b5:
		WriteString(writer, value.AsString());
		return;
		IL_0218:
		GodotObject godotObject = value.AsGodotObject();
		if (godotObject == null)
		{
			WriteString(writer, "<null-object>");
			return;
		}
		WriteString(writer, godotObject.GetType().FullName ?? godotObject.GetClass());
		if (!(godotObject is Resource resource))
		{
			WriteString(writer, godotObject.GetClass());
			return;
		}
		WriteString(writer, resource.ResourcePath ?? string.Empty);
		ulong instanceId = resource.GetInstanceId();
		if (!visitedResources.Add(instanceId))
		{
			WriteString(writer, "<resource-cycle>");
			return;
		}
		List<(string, Variant)> list2 = new List<(string, Variant)>();
		foreach (Dictionary property in resource.GetPropertyList())
		{
			string name;
			Variant.Type type;
			PropertyUsageFlags usage;
			PropertyHint hint;
			string hintString;
			bool flag = !StateMachineExtensionProperties.TryReadMetadata(property, out name, out type, out usage, out hint, out hintString) || (usage & PropertyUsageFlags.Storage) == 0;
			if (!flag)
			{
				bool flag2 = ((name == "script" || name == "resource_path") ? true : false);
				flag = flag2;
			}
			if (!flag && !name.StartsWith("metadata/", StringComparison.Ordinal))
			{
				list2.Add((name, resource.Get(name)));
			}
		}
		list2.Sort(((string name, Variant value) left, (string name, Variant value) right) => string.Compare(left.name, right.name, StringComparison.Ordinal));
		writer.Write(list2.Count);
		for (int num4 = 0; num4 < list2.Count; num4++)
		{
			WriteString(writer, list2[num4].Item1);
			WriteStableVariant(writer, list2[num4].Item2, visitedResources, depth + 1);
		}
		visitedResources.Remove(instanceId);
	}

	private static int CompareBytes(byte[] left, byte[] right)
	{
		int num = Math.Min(left.Length, right.Length);
		for (int i = 0; i < num; i++)
		{
			int num2 = left[i].CompareTo(right[i]);
			if (num2 != 0)
			{
				return num2;
			}
		}
		return left.Length.CompareTo(right.Length);
	}
}
