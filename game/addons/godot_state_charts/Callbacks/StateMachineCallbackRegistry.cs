using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

public sealed class StateMachineCallbackRegistry
{
	private sealed class CatalogAccumulator
	{
		public string OwnerId = string.Empty;

		public StateMachineCallbackPhaseFlags Phases;

		public StateMachineCallbackSourceKind SourceKind;

		public string DisplayName = string.Empty;

		public string SourcePath = string.Empty;

		public bool HasMetadata;
	}

	private readonly object _gate = new object();

	private readonly Dictionary<string, StateMachineCallbackOwnerRegistration> _owners = new Dictionary<string, StateMachineCallbackOwnerRegistration>(StringComparer.Ordinal);

	private readonly Dictionary<StateMachineCallbackSlot, Delegate> _callbacks = new Dictionary<StateMachineCallbackSlot, Delegate>();

	private readonly Dictionary<StateMachineCallbackSlot, StateMachineCallbackCatalogMetadata> _catalogMetadata = new Dictionary<StateMachineCallbackSlot, StateMachineCallbackCatalogMetadata>();

	public static StateMachineCallbackRegistry Shared { get; } = new StateMachineCallbackRegistry();

	internal bool WithRegistrationGate(Func<bool> commit)
	{
		lock (_gate)
		{
			return commit();
		}
	}

	public StateMachineCallbackRegistrationResult RegisterAssembly(string ownerId, Assembly assembly)
	{
		if (!StateMachineCallbackKey.TryNormalizeOwnerId(ownerId, out var normalized))
		{
			return new StateMachineCallbackRegistrationResult(success: false, "State-machine callback owner ID is invalid.");
		}
		lock (_gate)
		{
			if (_owners.TryGetValue(normalized, out var value))
			{
				if ((object)value.Assembly == assembly)
				{
					return new StateMachineCallbackRegistrationResult(success: true, "", value.Slots.Count);
				}
				return new StateMachineCallbackRegistrationResult(success: false, "State-machine callback owner '" + normalized + "' is already registered by another assembly.");
			}
		}
		if (!StateMachineCallbackAssemblyScanner.TryScan(normalized, assembly, out var callbacks, out var error))
		{
			return new StateMachineCallbackRegistrationResult(success: false, error);
		}
		lock (_gate)
		{
			if (_owners.TryGetValue(normalized, out var value2))
			{
				return new StateMachineCallbackRegistrationResult((object)value2.Assembly == assembly, ((object)value2.Assembly == assembly) ? string.Empty : ("State-machine callback owner '" + normalized + "' is already registered by another assembly."), value2.Slots.Count);
			}
			for (int i = 0; i < callbacks.Length; i++)
			{
				StateMachineCallbackSlot key = new StateMachineCallbackSlot(callbacks[i].FullKey, callbacks[i].Phase);
				if (_callbacks.ContainsKey(key))
				{
					return new StateMachineCallbackRegistrationResult(success: false, $"State-machine callback '{key.FullKey}' phase '{key.Phase}' is already registered.");
				}
			}
			StateMachineCallbackOwnerRegistration stateMachineCallbackOwnerRegistration = new StateMachineCallbackOwnerRegistration
			{
				OwnerId = normalized,
				Assembly = assembly
			};
			for (int j = 0; j < callbacks.Length; j++)
			{
				StateMachineCallbackSlot stateMachineCallbackSlot = new StateMachineCallbackSlot(callbacks[j].FullKey, callbacks[j].Phase);
				stateMachineCallbackOwnerRegistration.Slots.Add(stateMachineCallbackSlot);
				_callbacks.Add(stateMachineCallbackSlot, callbacks[j].Callback);
				_catalogMetadata.Add(stateMachineCallbackSlot, new StateMachineCallbackCatalogMetadata(callbacks[j].SourceKind, callbacks[j].DisplayName, callbacks[j].SourcePath));
			}
			_owners.Add(normalized, stateMachineCallbackOwnerRegistration);
			return new StateMachineCallbackRegistrationResult(success: true, "", callbacks.Length);
		}
	}

	public StateMachineCallbackRegistrationResult EnsureBuiltinAssembly(Assembly assembly)
	{
		return RegisterAssembly("builtin", assembly);
	}

	public bool TryAcquireBinding(string ownerId, StateMachineProgram program, object host, out StateMachineCallbackBinding binding, out StateMachineBindingDiagnostic diagnostic)
	{
		lock (_gate)
		{
			return StateMachineCallbackBinder.TryBind(this, ownerId, program, host, out binding, out diagnostic);
		}
	}

	public bool TryAcquireBinding(StateMachineProgram program, object host, out StateMachineCallbackBinding binding, out StateMachineBindingDiagnostic diagnostic)
	{
		return TryAcquireBinding(string.Empty, program, host, out binding, out diagnostic);
	}

	public bool TryUnloadOwner(string ownerId, out StateMachineUnloadBlockers blockers)
	{
		blockers = new StateMachineUnloadBlockers(0, Array.Empty<StateMachineUnloadBlocker>());
		if (!StateMachineCallbackKey.TryNormalizeOwnerId(ownerId, out var normalized))
		{
			return false;
		}
		lock (_gate)
		{
			if (!_owners.TryGetValue(normalized, out var value))
			{
				return false;
			}
			if (value.LeaseCount != 0)
			{
				StateMachineUnloadBlocker[] array = new StateMachineUnloadBlocker[value.Blockers.Count];
				value.Blockers.Values.CopyTo(array, 0);
				Array.Sort(array, CompareBlockers);
				blockers = new StateMachineUnloadBlockers(value.LeaseCount, array);
				return false;
			}
			for (int i = 0; i < value.Slots.Count; i++)
			{
				_callbacks.Remove(value.Slots[i]);
				_catalogMetadata.Remove(value.Slots[i]);
			}
			_owners.Remove(normalized);
			value.Slots.Clear();
			value.Blockers.Clear();
			value.Assembly = null;
			return true;
		}
	}

	public bool CanUnloadOwner(string ownerId, out StateMachineUnloadBlockers blockers)
	{
		blockers = new StateMachineUnloadBlockers(0, Array.Empty<StateMachineUnloadBlocker>());
		if (!StateMachineCallbackKey.TryNormalizeOwnerId(ownerId, out var normalized))
		{
			return false;
		}
		lock (_gate)
		{
			if (!_owners.TryGetValue(normalized, out var value))
			{
				return true;
			}
			if (value.LeaseCount == 0)
			{
				return true;
			}
			StateMachineUnloadBlocker[] array = new StateMachineUnloadBlocker[value.Blockers.Count];
			value.Blockers.Values.CopyTo(array, 0);
			Array.Sort(array, CompareBlockers);
			blockers = new StateMachineUnloadBlockers(value.LeaseCount, array);
			return false;
		}
	}

	public StateMachineCallbackCatalogSnapshot GetCatalogSnapshot()
	{
		lock (_gate)
		{
			Dictionary<string, CatalogAccumulator> dictionary = new Dictionary<string, CatalogAccumulator>(StringComparer.Ordinal);
			foreach (KeyValuePair<StateMachineCallbackSlot, Delegate> callback in _callbacks)
			{
				if (StateMachineCallbackKey.TryParse(callback.Key.FullKey, out var ownerId, out var _))
				{
					if (!dictionary.TryGetValue(callback.Key.FullKey, out var value))
					{
						value = new CatalogAccumulator
						{
							OwnerId = ownerId
						};
						dictionary.Add(callback.Key.FullKey, value);
					}
					value.Phases |= StateMachineCallbackKey.ToFlag(callback.Key.Phase);
					if (_catalogMetadata.TryGetValue(callback.Key, out var value2))
					{
						MergeCatalogMetadata(value, value2);
					}
				}
			}
			StateMachineCallbackCatalogEntry[] array = new StateMachineCallbackCatalogEntry[dictionary.Count];
			int num = 0;
			foreach (KeyValuePair<string, CatalogAccumulator> item in dictionary)
			{
				array[num++] = new StateMachineCallbackCatalogEntry(item.Key, item.Value.OwnerId, item.Value.Phases, item.Value.SourceKind, item.Value.DisplayName, item.Value.SourcePath);
			}
			Array.Sort(array, CompareCatalogEntries);
			return new StateMachineCallbackCatalogSnapshot
			{
				Entries = array
			};
		}
	}

	private static void MergeCatalogMetadata(CatalogAccumulator current, StateMachineCallbackCatalogMetadata metadata)
	{
		if (!current.HasMetadata)
		{
			current.SourceKind = metadata.SourceKind;
			current.DisplayName = metadata.DisplayName;
			current.SourcePath = metadata.SourcePath;
			current.HasMetadata = true;
			return;
		}
		if (current.SourceKind != metadata.SourceKind)
		{
			current.SourceKind = StateMachineCallbackSourceKind.Mixed;
		}
		current.DisplayName = SelectCatalogText(current.DisplayName, metadata.DisplayName);
		current.SourcePath = SelectCatalogText(current.SourcePath, metadata.SourcePath);
	}

	private static string SelectCatalogText(string current, string candidate)
	{
		if (string.IsNullOrWhiteSpace(current))
		{
			return candidate ?? string.Empty;
		}
		if (string.IsNullOrWhiteSpace(candidate))
		{
			return current;
		}
		if (string.Compare(candidate, current, StringComparison.Ordinal) >= 0)
		{
			return current;
		}
		return candidate;
	}

	internal bool TryGetCallback(string fullKey, StateMachineCallbackPhase phase, out Delegate callback)
	{
		lock (_gate)
		{
			return _callbacks.TryGetValue(new StateMachineCallbackSlot(fullKey, phase), out callback);
		}
	}

	internal bool TryGetPhases(string fullKey, out StateMachineCallbackPhaseFlags phases)
	{
		phases = StateMachineCallbackPhaseFlags.None;
		lock (_gate)
		{
			StateMachineCallbackPhase[] values = Enum.GetValues<StateMachineCallbackPhase>();
			foreach (StateMachineCallbackPhase phase in values)
			{
				if (_callbacks.ContainsKey(new StateMachineCallbackSlot(fullKey, phase)))
				{
					phases |= StateMachineCallbackKey.ToFlag(phase);
				}
			}
		}
		return phases != StateMachineCallbackPhaseFlags.None;
	}

	internal bool TryAcquireOwnerLeases(IReadOnlyCollection<string> ownerIds, string definitionId, object host, out List<Action> releases, out StateMachineBindingDiagnostic diagnostic)
	{
		releases = new List<Action>(ownerIds?.Count ?? 0);
		diagnostic = default(StateMachineBindingDiagnostic);
		if (ownerIds == null || ownerIds.Count == 0)
		{
			return true;
		}
		lock (_gate)
		{
			foreach (string ownerId2 in ownerIds)
			{
				if (!_owners.ContainsKey(ownerId2))
				{
					diagnostic = new StateMachineBindingDiagnostic("SMB003", "Callback owner '" + ownerId2 + "' is not registered.");
					return false;
				}
			}
			string hostType = host?.GetType().FullName ?? "<null>";
			foreach (string ownerId in ownerIds)
			{
				StateMachineCallbackOwnerRegistration stateMachineCallbackOwnerRegistration = _owners[ownerId];
				long leaseId = Interlocked.Increment(ref stateMachineCallbackOwnerRegistration.NextLeaseId);
				stateMachineCallbackOwnerRegistration.LeaseCount++;
				stateMachineCallbackOwnerRegistration.Blockers.Add(leaseId, new StateMachineUnloadBlocker(definitionId, hostType));
				releases.Add(delegate
				{
					ReleaseOwnerLease(ownerId, leaseId);
				});
			}
			return true;
		}
	}

	private void ReleaseOwnerLease(string ownerId, long leaseId)
	{
		lock (_gate)
		{
			if (_owners.TryGetValue(ownerId, out var value) && value.Blockers.Remove(leaseId) && value.LeaseCount > 0)
			{
				value.LeaseCount--;
			}
		}
	}

	private static int CompareCatalogEntries(StateMachineCallbackCatalogEntry left, StateMachineCallbackCatalogEntry right)
	{
		return string.Compare(left.Key, right.Key, StringComparison.Ordinal);
	}

	private static int CompareBlockers(StateMachineUnloadBlocker left, StateMachineUnloadBlocker right)
	{
		int num = string.Compare(left.DefinitionId, right.DefinitionId, StringComparison.Ordinal);
		if (num == 0)
		{
			return string.Compare(left.HostType, right.HostType, StringComparison.Ordinal);
		}
		return num;
	}
}
