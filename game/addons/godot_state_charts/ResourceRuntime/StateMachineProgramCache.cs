using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using Godot;

public static class StateMachineProgramCache
{
	private readonly struct CacheKey(string identity, string contentHash, int compilerVersion, int programFormatVersion) : IEquatable<CacheKey>
	{
		public readonly string Identity = identity;

		public readonly string ContentHash = contentHash;

		public readonly int CompilerVersion = compilerVersion;

		public readonly int ProgramFormatVersion = programFormatVersion;

		public bool Equals(CacheKey other)
		{
			if (CompilerVersion == other.CompilerVersion && ProgramFormatVersion == other.ProgramFormatVersion && string.Equals(Identity, other.Identity, StringComparison.Ordinal))
			{
				return string.Equals(ContentHash, other.ContentHash, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is CacheKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Identity, ContentHash, CompilerVersion, ProgramFormatVersion);
		}
	}

	private sealed class CacheEntry
	{
		public int RefCount;

		public bool Pinned;

		public long LastReleasedTick;

		public StateMachineProgram Program;
	}

	private sealed class DefinitionFastEntry
	{
		public CacheKey Key;

		public StateMachineProgram Program;

		public bool Dirty = true;

		public readonly List<StateMachineDefinition> ObservedDefinitions = new List<StateMachineDefinition>();

		public readonly Action ChangedHandler;

		public DefinitionFastEntry()
		{
			ChangedHandler = Invalidate;
		}

		private void Invalidate()
		{
			lock (Sync)
			{
				Dirty = true;
			}
		}
	}

	private static readonly object Sync = new object();

	private static readonly Dictionary<CacheKey, CacheEntry> Entries = new Dictionary<CacheKey, CacheEntry>();

	private static readonly Dictionary<StateMachineProgram, CacheKey> ProgramKeys = new Dictionary<StateMachineProgram, CacheKey>();

	private static readonly ConditionalWeakTable<StateMachineDefinition, DefinitionFastEntry> DefinitionFastEntries = new ConditionalWeakTable<StateMachineDefinition, DefinitionFastEntry>();

	public static StateMachineProgram Acquire(StateMachineDefinition definition, bool pinned = false)
	{
		if (TryAcquirePreparedDefinition(definition, pinned, out var program))
		{
			return program;
		}
		if (!StateMachineCompiler.TryPrepare(definition, out var validation, out var snapshot, out var contentHash))
		{
			throw new InvalidOperationException(BuildValidationFailureMessage(validation));
		}
		string identity = (string.IsNullOrWhiteSpace(snapshot.ResourcePath) ? ("instance:" + snapshot.InstanceId.ToString(CultureInfo.InvariantCulture)) : ("path:" + snapshot.ResourcePath));
		CacheKey cacheKey = new CacheKey(identity, contentHash, 6, 7);
		lock (Sync)
		{
			if (Entries.TryGetValue(cacheKey, out var value))
			{
				value.RefCount++;
				value.Pinned |= pinned;
				UpdatePreparedDefinitionLocked(definition, cacheKey, value.Program);
				return value.Program;
			}
			StateMachineProgram stateMachineProgram = StateMachineCompiler.CompileValidated(snapshot, contentHash);
			value = new CacheEntry
			{
				RefCount = 1,
				Pinned = pinned,
				LastReleasedTick = 0L,
				Program = stateMachineProgram
			};
			Entries.Add(cacheKey, value);
			ProgramKeys.Add(stateMachineProgram, cacheKey);
			UpdatePreparedDefinitionLocked(definition, cacheKey, stateMachineProgram);
			return stateMachineProgram;
		}
	}

	private static bool TryAcquirePreparedDefinition(StateMachineDefinition definition, bool pinned, out StateMachineProgram program)
	{
		program = null;
		if (definition == null || Engine.IsEditorHint())
		{
			return false;
		}
		lock (Sync)
		{
			if (!DefinitionFastEntries.TryGetValue(definition, out var value) || value.Dirty || value.Program == null || !Entries.TryGetValue(value.Key, out var value2) || value2.Program != value.Program)
			{
				return false;
			}
			value2.RefCount++;
			value2.Pinned |= pinned;
			program = value2.Program;
			return true;
		}
	}

	private static void UpdatePreparedDefinitionLocked(StateMachineDefinition definition, CacheKey key, StateMachineProgram program)
	{
		if (definition != null && !Engine.IsEditorHint())
		{
			DefinitionFastEntry value = DefinitionFastEntries.GetValue(definition, (StateMachineDefinition _) => new DefinitionFastEntry());
			for (int num = 0; num < value.ObservedDefinitions.Count; num++)
			{
				value.ObservedDefinitions[num].Changed -= value.ChangedHandler;
			}
			value.ObservedDefinitions.Clear();
			HashSet<ulong> hashSet = new HashSet<ulong>();
			StateMachineDefinition stateMachineDefinition = definition;
			while (stateMachineDefinition != null && hashSet.Add(stateMachineDefinition.GetInstanceId()))
			{
				stateMachineDefinition.Changed += value.ChangedHandler;
				value.ObservedDefinitions.Add(stateMachineDefinition);
				stateMachineDefinition = stateMachineDefinition.BaseDefinition;
			}
			value.Key = key;
			value.Program = program;
			value.Dirty = false;
		}
	}

	public static bool Release(StateMachineProgram program)
	{
		if (program == null)
		{
			return false;
		}
		lock (Sync)
		{
			if (!ProgramKeys.TryGetValue(program, out var value) || !Entries.TryGetValue(value, out var value2) || value2.RefCount <= 0)
			{
				return false;
			}
			value2.RefCount--;
			if (value2.RefCount == 0)
			{
				value2.LastReleasedTick = System.Environment.TickCount64;
			}
			return true;
		}
	}

	public static bool SetPinned(StateMachineProgram program, bool pinned)
	{
		if (program == null)
		{
			return false;
		}
		lock (Sync)
		{
			if (!ProgramKeys.TryGetValue(program, out var value) || !Entries.TryGetValue(value, out var value2))
			{
				return false;
			}
			value2.Pinned = pinned;
			return true;
		}
	}

	public static int ClearUnused()
	{
		lock (Sync)
		{
			List<CacheKey> list = new List<CacheKey>();
			foreach (KeyValuePair<CacheKey, CacheEntry> entry in Entries)
			{
				if (entry.Value.RefCount == 0 && !entry.Value.Pinned)
				{
					list.Add(entry.Key);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				CacheKey key = list[i];
				StateMachineProgram program = Entries[key].Program;
				Entries.Remove(key);
				ProgramKeys.Remove(program);
			}
			return list.Count;
		}
	}

	public static StateMachineProgramCacheStats GetStats()
	{
		lock (Sync)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			foreach (CacheEntry value in Entries.Values)
			{
				if (value.RefCount > 0)
				{
					num++;
				}
				if (value.Pinned)
				{
					num2++;
				}
				num3 += value.RefCount;
			}
			return new StateMachineProgramCacheStats(Entries.Count, num, num2, num3);
		}
	}

	private static string BuildValidationFailureMessage(StateMachineValidationResult validation)
	{
		if (validation == null || validation.Diagnostics.Count == 0)
		{
			return "State machine definition could not be compiled.";
		}
		string[] array = new string[validation.Diagnostics.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = validation.Diagnostics[i].Code;
		}
		return "State machine definition is invalid: " + string.Join(',', array);
	}
}
