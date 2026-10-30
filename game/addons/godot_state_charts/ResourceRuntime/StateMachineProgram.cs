using System;
using System.Collections.Generic;
using System.Threading;
using Godot;

public sealed class StateMachineProgram
{
	private sealed class EventTransitionCacheEntry
	{
		public readonly StringName EventName;

		public readonly bool Found;

		public readonly StateMachineTransitionSlice Slice;

		public EventTransitionCacheEntry(StringName eventName, bool found, StateMachineTransitionSlice slice)
		{
			EventName = eventName;
			Found = found;
			Slice = slice;
		}
	}

	public const int ProgramFormatVersion = 7;

	private readonly CompiledStateMachineState[] _states;

	private readonly CompiledStateMachineTransition[] _transitions;

	private readonly int[] _eventTransitionIndices;

	private readonly int[] _automaticTransitionIndices;

	private readonly int[] _processStateIndices;

	private readonly int[] _physicsStateIndices;

	private readonly int[] _processStateOrders;

	private readonly int[] _physicsStateOrders;

	private readonly int[] _flatDirectTransitionByTarget;

	private readonly StringName[] _guardCallbackKeys;

	private readonly Dictionary<StringName, StateMachineTransitionSlice> _eventTransitionSlices;

	private EventTransitionCacheEntry _eventTransitionCache0;

	private EventTransitionCacheEntry _eventTransitionCache1;

	private EventTransitionCacheEntry _eventTransitionCache2;

	private EventTransitionCacheEntry _eventTransitionCache3;

	private readonly Dictionary<string, int> _stateIndices;

	private readonly Dictionary<string, int> _transitionIndices;

	private readonly Dictionary<string, string> _aliases;

	public string DefinitionId { get; }

	public int SchemaVersion { get; }

	public string ContentHash { get; }

	public long ProgramGeneration { get; }

	public int RootStateIndex { get; }

	public ReadOnlySpan<CompiledStateMachineState> States => _states;

	public ReadOnlySpan<CompiledStateMachineTransition> Transitions => _transitions;

	public ReadOnlySpan<int> EventTransitionIndices => _eventTransitionIndices;

	public ReadOnlySpan<int> AutomaticTransitionIndices => _automaticTransitionIndices;

	public ReadOnlySpan<int> ProcessStateIndices => _processStateIndices;

	public ReadOnlySpan<int> PhysicsStateIndices => _physicsStateIndices;

	public ReadOnlySpan<StringName> GuardCallbackKeys => _guardCallbackKeys;

	public bool HasDelayedTransitions { get; }

	public bool HasHistoryStates { get; }

	public bool UsesExecutableCallbacks { get; }

	public bool SupportsFlatDirectStateTransitions => _flatDirectTransitionByTarget.Length != 0;

	public int StateCount => _states.Length;

	public int TransitionCount => _transitions.Length;

	internal int[] ProcessStateOrdersStorage => _processStateOrders;

	internal int[] PhysicsStateOrdersStorage => _physicsStateOrders;

	internal StateMachineProgram(string definitionId, int schemaVersion, string contentHash, long programGeneration, int rootStateIndex, CompiledStateMachineState[] states, CompiledStateMachineTransition[] transitions, int[] eventTransitionIndices, int[] automaticTransitionIndices, StringName[] guardCallbackKeys, Dictionary<StringName, StateMachineTransitionSlice> eventTransitionSlices, Dictionary<string, int> stateIndices, Dictionary<string, string> aliases)
	{
		DefinitionId = definitionId;
		SchemaVersion = schemaVersion;
		ContentHash = contentHash;
		ProgramGeneration = programGeneration;
		RootStateIndex = rootStateIndex;
		_states = states;
		_transitions = transitions;
		_eventTransitionIndices = eventTransitionIndices;
		_automaticTransitionIndices = automaticTransitionIndices;
		_guardCallbackKeys = guardCallbackKeys ?? Array.Empty<StringName>();
		_processStateIndices = BuildProcessStateIndices(states, StateMachineProcessFlags.Process);
		_physicsStateIndices = BuildProcessStateIndices(states, StateMachineProcessFlags.PhysicsProcess);
		_processStateOrders = BuildProcessStateOrders(states.Length, _processStateIndices);
		_physicsStateOrders = BuildProcessStateOrders(states.Length, _physicsStateIndices);
		_flatDirectTransitionByTarget = BuildFlatDirectTransitionByTarget(rootStateIndex, states, transitions, automaticTransitionIndices);
		HasDelayedTransitions = BuildHasDelayedTransitions(transitions);
		HasHistoryStates = BuildHasHistoryStates(states);
		UsesExecutableCallbacks = BuildUsesExecutableCallbacks(states, _guardCallbackKeys);
		_eventTransitionSlices = eventTransitionSlices;
		_stateIndices = stateIndices;
		_transitionIndices = BuildTransitionIndices(transitions);
		_aliases = aliases;
	}

	internal bool TryGetFlatDirectTransition(int targetStateIndex, StringName eventName, out int transitionIndex)
	{
		transitionIndex = -1;
		if ((uint)targetStateIndex >= (uint)_flatDirectTransitionByTarget.Length)
		{
			return false;
		}
		transitionIndex = _flatDirectTransitionByTarget[targetStateIndex];
		if (transitionIndex >= 0)
		{
			return _transitions[transitionIndex].EventName == eventName;
		}
		return false;
	}

	public bool TryGetStateIndex(string stableId, out int stateIndex)
	{
		return _stateIndices.TryGetValue(stableId ?? string.Empty, out stateIndex);
	}

	public bool TryResolveStateIndex(string stableId, out int stateIndex)
	{
		string text = stableId ?? string.Empty;
		for (int i = 0; i <= _aliases.Count; i++)
		{
			if (_stateIndices.TryGetValue(text, out stateIndex))
			{
				return true;
			}
			if (!_aliases.TryGetValue(text, out var value) || string.Equals(text, value, StringComparison.Ordinal))
			{
				break;
			}
			text = value;
		}
		stateIndex = -1;
		return false;
	}

	public bool TryGetEventTransitionSlice(StringName eventName, out StateMachineTransitionSlice slice)
	{
		EventTransitionCacheEntry eventTransitionCacheEntry = Volatile.Read(in _eventTransitionCache0);
		if (eventTransitionCacheEntry != null && eventTransitionCacheEntry.EventName == eventName)
		{
			slice = eventTransitionCacheEntry.Slice;
			return eventTransitionCacheEntry.Found;
		}
		eventTransitionCacheEntry = Volatile.Read(in _eventTransitionCache1);
		if (eventTransitionCacheEntry != null && eventTransitionCacheEntry.EventName == eventName)
		{
			slice = eventTransitionCacheEntry.Slice;
			return eventTransitionCacheEntry.Found;
		}
		eventTransitionCacheEntry = Volatile.Read(in _eventTransitionCache2);
		if (eventTransitionCacheEntry != null && eventTransitionCacheEntry.EventName == eventName)
		{
			slice = eventTransitionCacheEntry.Slice;
			return eventTransitionCacheEntry.Found;
		}
		eventTransitionCacheEntry = Volatile.Read(in _eventTransitionCache3);
		if (eventTransitionCacheEntry != null && eventTransitionCacheEntry.EventName == eventName)
		{
			slice = eventTransitionCacheEntry.Slice;
			return eventTransitionCacheEntry.Found;
		}
		bool flag = _eventTransitionSlices.TryGetValue(eventName, out slice);
		EventTransitionCacheEntry value = new EventTransitionCacheEntry(eventName, flag, slice);
		Volatile.Write(ref _eventTransitionCache3, Volatile.Read(in _eventTransitionCache2));
		Volatile.Write(ref _eventTransitionCache2, Volatile.Read(in _eventTransitionCache1));
		Volatile.Write(ref _eventTransitionCache1, Volatile.Read(in _eventTransitionCache0));
		Volatile.Write(ref _eventTransitionCache0, value);
		return flag;
	}

	public bool TryGetStateExtensionProperty(string stableId, StringName propertyName, out Variant value)
	{
		if (_stateIndices.TryGetValue(stableId ?? string.Empty, out var value2))
		{
			return _states[value2].TryGetExtensionProperty(propertyName, out value);
		}
		value = default(Variant);
		return false;
	}

	public bool TryGetTransitionExtensionProperty(string stableId, StringName propertyName, out Variant value)
	{
		if (_transitionIndices.TryGetValue(stableId ?? string.Empty, out var value2))
		{
			return _transitions[value2].TryGetExtensionProperty(propertyName, out value);
		}
		value = default(Variant);
		return false;
	}

	private static int[] BuildProcessStateIndices(CompiledStateMachineState[] states, StateMachineProcessFlags requiredFlag)
	{
		int num = 0;
		for (int i = 0; i < states.Length; i++)
		{
			if ((states[i].ProcessFlags & requiredFlag) != StateMachineProcessFlags.None)
			{
				num++;
			}
		}
		int[] array = new int[num];
		int num2 = 0;
		for (int j = 0; j < states.Length; j++)
		{
			if ((states[j].ProcessFlags & requiredFlag) != StateMachineProcessFlags.None)
			{
				array[num2++] = j;
			}
		}
		return array;
	}

	private static int[] BuildProcessStateOrders(int stateCount, int[] stateIndices)
	{
		int[] array = new int[stateCount];
		Array.Fill(array, -1);
		for (int i = 0; i < stateIndices.Length; i++)
		{
			array[stateIndices[i]] = i;
		}
		return array;
	}

	private static int[] BuildFlatDirectTransitionByTarget(int rootStateIndex, CompiledStateMachineState[] states, CompiledStateMachineTransition[] transitions, int[] automaticTransitionIndices)
	{
		if (states.Length < 2 || transitions.Length == 0 || (uint)rootStateIndex >= (uint)states.Length || states[rootStateIndex].Kind != StateMachineStateKind.Compound || states[rootStateIndex].ParentIndex >= 0 || automaticTransitionIndices.Length != 0)
		{
			return Array.Empty<int>();
		}
		for (int i = 0; i < states.Length; i++)
		{
			if (i != rootStateIndex && (states[i].Kind != StateMachineStateKind.Atomic || states[i].ParentIndex != rootStateIndex || states[i].InitialChildIndex >= 0))
			{
				return Array.Empty<int>();
			}
		}
		int[] array = new int[states.Length];
		Array.Fill(array, -1);
		for (int j = 0; j < transitions.Length; j++)
		{
			ref CompiledStateMachineTransition reference = ref transitions[j];
			if (reference.TriggerKind != StateMachineTriggerKind.Event || reference.SourceIndex != rootStateIndex || reference.TargetIndex == rootStateIndex || (uint)reference.TargetIndex >= (uint)states.Length || reference.DelaySeconds > 0.0 || reference.Guard.IsDefined || reference.EventName.IsEmpty || array[reference.TargetIndex] >= 0)
			{
				return Array.Empty<int>();
			}
			array[reference.TargetIndex] = j;
		}
		return array;
	}

	private static bool BuildHasDelayedTransitions(CompiledStateMachineTransition[] transitions)
	{
		for (int i = 0; i < transitions.Length; i++)
		{
			if (transitions[i].DelaySeconds > 0.0)
			{
				return true;
			}
		}
		return false;
	}

	private static bool BuildHasHistoryStates(CompiledStateMachineState[] states)
	{
		for (int i = 0; i < states.Length; i++)
		{
			if (states[i].Kind == StateMachineStateKind.History)
			{
				return true;
			}
		}
		return false;
	}

	private static bool BuildUsesExecutableCallbacks(CompiledStateMachineState[] states, StringName[] guardCallbackKeys)
	{
		if (guardCallbackKeys.Length != 0)
		{
			return true;
		}
		for (int i = 0; i < states.Length; i++)
		{
			ref CompiledStateMachineState reference = ref states[i];
			if (UsesExecutableCallback(reference.EnterCallbackKey, reference.CallbackKey) || UsesExecutableCallback(reference.ExitCallbackKey, reference.CallbackKey) || UsesExecutableCallback(reference.ProcessCallbackKey, reference.CallbackKey) || UsesExecutableCallback(reference.PhysicsProcessCallbackKey, reference.CallbackKey))
			{
				return true;
			}
		}
		return false;
	}

	private static bool UsesExecutableCallback(StringName phaseKey, StringName fallbackKey)
	{
		return StateMachineCallbackKey.UsesExecutablePrefix(phaseKey.IsEmpty ? fallbackKey.ToString() : phaseKey.ToString());
	}

	private static Dictionary<string, int> BuildTransitionIndices(CompiledStateMachineTransition[] transitions)
	{
		Dictionary<string, int> dictionary = new Dictionary<string, int>(transitions.Length, StringComparer.Ordinal);
		for (int i = 0; i < transitions.Length; i++)
		{
			string text = transitions[i].StableId ?? string.Empty;
			if (!string.IsNullOrWhiteSpace(text) && !dictionary.ContainsKey(text))
			{
				dictionary.Add(text, i);
			}
		}
		return dictionary;
	}
}
