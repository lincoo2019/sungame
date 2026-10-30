using System;
using System.Collections.Generic;
using Godot;

internal static class StateMachineCallbackBinder
{
	public static bool TryBind(StateMachineCallbackRegistry registry, string expectedOwnerId, StateMachineProgram program, object host, out StateMachineCallbackBinding binding, out StateMachineBindingDiagnostic diagnostic)
	{
		binding = null;
		diagnostic = default(StateMachineBindingDiagnostic);
		if (registry == null)
		{
			registry = StateMachineCallbackRegistry.Shared;
		}
		if (program == null)
		{
			diagnostic = new StateMachineBindingDiagnostic("SMB001", "State-machine callback binding requires a compiled program.");
			return false;
		}
		string normalized = string.Empty;
		if (!string.IsNullOrWhiteSpace(expectedOwnerId) && !StateMachineCallbackKey.TryNormalizeOwnerId(expectedOwnerId, out normalized))
		{
			diagnostic = new StateMachineBindingDiagnostic("SMB002", "Expected callback owner ID is invalid.");
			return false;
		}
		int stateCount = program.StateCount;
		StateMachineLifecycleCallback[] array = new StateMachineLifecycleCallback[stateCount];
		StateMachineLifecycleCallback[] array2 = new StateMachineLifecycleCallback[stateCount];
		StateMachineDeltaCallback[] array3 = new StateMachineDeltaCallback[stateCount];
		StateMachineDeltaCallback[] array4 = new StateMachineDeltaCallback[stateCount];
		StateMachineGuardCallback[] array5 = new StateMachineGuardCallback[program.GuardCallbackKeys.Length];
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
		string inferredModOwner = string.Empty;
		ReadOnlySpan<CompiledStateMachineState> states = program.States;
		for (int i = 0; i < states.Length; i++)
		{
			CompiledStateMachineState state = states[i];
			string text = state.CallbackKey.ToString();
			if (UsesLegacyLifecycleFallback(in state) && StateMachineCallbackKey.UsesExecutablePrefix(text) && (!registry.TryGetPhases(text, out var phases) || (phases & (StateMachineCallbackPhaseFlags.Enter | StateMachineCallbackPhaseFlags.Exit | StateMachineCallbackPhaseFlags.Process | StateMachineCallbackPhaseFlags.PhysicsProcess)) == 0))
			{
				diagnostic = MissingCallback(text, state.StableId, "no lifecycle phase is registered");
				return false;
			}
			if (!TryBindLifecyclePhase<StateMachineLifecycleCallback>(registry, in state, StateMachineCallbackPhase.Enter, normalized, ref inferredModOwner, hashSet, out array[i], out diagnostic))
			{
				return false;
			}
			if (!TryBindLifecyclePhase<StateMachineLifecycleCallback>(registry, in state, StateMachineCallbackPhase.Exit, normalized, ref inferredModOwner, hashSet, out array2[i], out diagnostic))
			{
				return false;
			}
			if (!TryBindLifecyclePhase<StateMachineDeltaCallback>(registry, in state, StateMachineCallbackPhase.Process, normalized, ref inferredModOwner, hashSet, out array3[i], out diagnostic))
			{
				return false;
			}
			if (!TryBindLifecyclePhase<StateMachineDeltaCallback>(registry, in state, StateMachineCallbackPhase.PhysicsProcess, normalized, ref inferredModOwner, hashSet, out array4[i], out diagnostic))
			{
				return false;
			}
		}
		ReadOnlySpan<StringName> guardCallbackKeys = program.GuardCallbackKeys;
		for (int j = 0; j < guardCallbackKeys.Length; j++)
		{
			string text2 = guardCallbackKeys[j].ToString();
			if (!TryValidateKeyOwner(text2, normalized, ref inferredModOwner, out var ownerId, out diagnostic, string.Empty))
			{
				return false;
			}
			array5[j] = Resolve<StateMachineGuardCallback>(registry, text2, StateMachineCallbackPhase.Guard);
			if (array5[j] == null)
			{
				diagnostic = MissingCallback(text2, string.Empty, "the Guard phase is not registered");
				return false;
			}
			hashSet.Add(ownerId);
		}
		if (!registry.TryAcquireOwnerLeases(hashSet, program.DefinitionId, host, out var releases, out diagnostic))
		{
			return false;
		}
		binding = new StateMachineCallbackBinding(host, array, array2, array3, array4, array5, releases);
		return true;
	}

	private static bool UsesLegacyLifecycleFallback(in CompiledStateMachineState state)
	{
		if (string.IsNullOrWhiteSpace(state.EnterCallbackKey.ToString()) || string.IsNullOrWhiteSpace(state.ExitCallbackKey.ToString()))
		{
			return true;
		}
		if ((state.ProcessFlags & StateMachineProcessFlags.Process) != StateMachineProcessFlags.None && string.IsNullOrWhiteSpace(state.ProcessCallbackKey.ToString()))
		{
			return true;
		}
		if ((state.ProcessFlags & StateMachineProcessFlags.PhysicsProcess) != StateMachineProcessFlags.None)
		{
			return string.IsNullOrWhiteSpace(state.PhysicsProcessCallbackKey.ToString());
		}
		return false;
	}

	private static bool TryBindLifecyclePhase<T>(StateMachineCallbackRegistry registry, in CompiledStateMachineState state, StateMachineCallbackPhase phase, string normalizedExpectedOwner, ref string inferredModOwner, HashSet<string> usedOwners, out T callback, out StateMachineBindingDiagnostic diagnostic) where T : Delegate
	{
		callback = null;
		diagnostic = default(StateMachineBindingDiagnostic);
		bool flag = !string.IsNullOrWhiteSpace((phase switch
		{
			StateMachineCallbackPhase.Enter => state.EnterCallbackKey, 
			StateMachineCallbackPhase.Exit => state.ExitCallbackKey, 
			StateMachineCallbackPhase.Process => state.ProcessCallbackKey, 
			StateMachineCallbackPhase.PhysicsProcess => state.PhysicsProcessCallbackKey, 
			_ => new StringName(), 
		}).ToString());
		string text = state.GetLifecycleCallbackKey(phase).ToString();
		if (string.IsNullOrWhiteSpace(text) || !StateMachineCallbackKey.UsesExecutablePrefix(text))
		{
			return true;
		}
		if (!TryValidateKeyOwner(text, normalizedExpectedOwner, ref inferredModOwner, out var ownerId, out diagnostic, state.StableId))
		{
			return false;
		}
		callback = Resolve<T>(registry, text, phase);
		bool flag2 = flag || (phase == StateMachineCallbackPhase.Process && (state.ProcessFlags & StateMachineProcessFlags.Process) != StateMachineProcessFlags.None) || (phase == StateMachineCallbackPhase.PhysicsProcess && (state.ProcessFlags & StateMachineProcessFlags.PhysicsProcess) != 0);
		if (callback == null)
		{
			if (!flag2)
			{
				return true;
			}
			diagnostic = MissingCallback(text, state.StableId, $"the {phase} phase is not registered");
			return false;
		}
		usedOwners.Add(ownerId);
		return true;
	}

	private static T Resolve<T>(StateMachineCallbackRegistry registry, string fullKey, StateMachineCallbackPhase phase) where T : Delegate
	{
		if (!registry.TryGetCallback(fullKey, phase, out var callback))
		{
			return null;
		}
		return callback as T;
	}

	private static bool TryValidateKeyOwner(string callbackKey, string expectedOwner, ref string inferredModOwner, out string ownerId, out StateMachineBindingDiagnostic diagnostic, string stableId)
	{
		diagnostic = default(StateMachineBindingDiagnostic);
		if (!StateMachineCallbackKey.TryParse(callbackKey, out ownerId, out var _))
		{
			diagnostic = new StateMachineBindingDiagnostic("SMB004", "Callback key must use builtin/<key> or mod/<canonical-mod-id>/<key>.", callbackKey, stableId);
			return false;
		}
		if (ownerId == "builtin")
		{
			return true;
		}
		if (!string.IsNullOrEmpty(expectedOwner) && !string.Equals(ownerId, expectedOwner, StringComparison.Ordinal))
		{
			diagnostic = new StateMachineBindingDiagnostic("SMB005", $"Callback '{callbackKey}' belongs to Mod '{ownerId}', not expected owner '{expectedOwner}'.", callbackKey, stableId);
			return false;
		}
		if (string.IsNullOrEmpty(inferredModOwner))
		{
			inferredModOwner = ownerId;
			return true;
		}
		if (string.Equals(inferredModOwner, ownerId, StringComparison.Ordinal))
		{
			return true;
		}
		diagnostic = new StateMachineBindingDiagnostic("SMB006", "One state-machine program cannot bind callbacks from " + $"multiple Mods ('{inferredModOwner}' and '{ownerId}').", callbackKey, stableId);
		return false;
	}

	private static StateMachineBindingDiagnostic MissingCallback(string callbackKey, string stableId, string reason)
	{
		return new StateMachineBindingDiagnostic("SMB007", "State-machine callback '" + callbackKey + "' cannot bind because " + reason + ".", callbackKey, stableId);
	}
}
