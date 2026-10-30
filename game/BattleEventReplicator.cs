using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class BattleEventReplicator : NetworkReplicatorBase
{
	private sealed class PendingComponentOperation
	{
		public int SyncId;

		public long BarrierGeneration;

		public Dictionary Envelope;

		public double Elapsed;

		public double RetryRemaining;

		public TowerDefenseCharacter Target;

		public bool HasBoundTarget;
	}

	private sealed class PendingDancerRelation
	{
		public int ChildSyncId;

		public int ParentSyncId;

		public int Slot;

		public double Elapsed;

		public double RetryRemaining;
	}

	private sealed class PendingBobsledRelation
	{
		public int ChildSyncId;

		public int ParentSyncId;

		public int Slot;

		public double Elapsed;

		public double RetryRemaining;

		public bool HasTerminalOperation;

		public bool Cascade;

		public bool WaitForParentGeneration;

		public TowerDefenseCharacter Child;

		public bool HasBoundChild;

		public TowerDefenseCharacter Parent;

		public bool HasBoundParent;

		public TowerDefenseCharacter TerminalParent;
	}

	private sealed class BobsledTerminalState
	{
		public TowerDefenseCharacter Parent;

		public bool Cascade;

		public double Elapsed;
	}

	private readonly HashSet<int> _trioSpawnIds = new HashSet<int>();

	private readonly IBattleEventNetworkContext _eventContext;

	private readonly IPacketNetworkContext _packetContext;

	private readonly HashSet<int> _appliedVaseBreakIds = new HashSet<int>();

	private readonly HashSet<int> _appliedWaveEventIds = new HashSet<int>();

	private readonly HashSet<long> _appliedFallingObjectSpawnSequences = new HashSet<long>();

	private readonly List<PendingComponentOperation> _pendingComponentOperations = new List<PendingComponentOperation>();

	private readonly List<PendingDancerRelation> _pendingDancerRelations = new List<PendingDancerRelation>();

	private readonly List<PendingBobsledRelation> _pendingBobsledRelations = new List<PendingBobsledRelation>();

	private readonly System.Collections.Generic.Dictionary<int, BobsledTerminalState> _bobsledTerminalByParent = new System.Collections.Generic.Dictionary<int, BobsledTerminalState>();

	private readonly System.Collections.Generic.Dictionary<ulong, ulong> _pendingBobsledReleaseGenerations = new System.Collections.Generic.Dictionary<ulong, ulong>();

	private ulong _nextBobsledReleaseGeneration;

	private const int MaxPendingComponentOperations = 256;

	private const int MaxPendingBobsledRelations = 256;

	private const double PendingComponentOperationTimeout = 5.0;

	private const double PendingComponentOperationRetryInterval = 0.05;

	private const double BobsledTerminalStateTimeout = 10.0;

	private CharacterComponentOperationDestroyBarrier _componentOperationDestroyBarrier;

	public BattleEventReplicator()
	{
	}

	public BattleEventReplicator(IBattleEventNetworkContext eventContext, IPacketNetworkContext packetContext)
	{
		_eventContext = eventContext;
		_packetContext = packetContext;
	}

	public override void Initialize(BattleNetworkSession session)
	{
		base.Initialize(session);
		_componentOperationDestroyBarrier = CharacterComponentOperationDestroyBarrier.ForSession(session);
	}

	public void BindPendingCharacterGeneration(int syncId, TowerDefenseCharacter character)
	{
		if (syncId < 0 || !GodotObject.IsInstanceValid(character))
		{
			return;
		}
		foreach (PendingComponentOperation pendingComponentOperation in _pendingComponentOperations)
		{
			if (pendingComponentOperation.SyncId == syncId && !pendingComponentOperation.HasBoundTarget)
			{
				pendingComponentOperation.Target = character;
				pendingComponentOperation.HasBoundTarget = true;
			}
		}
		foreach (PendingBobsledRelation pendingBobsledRelation in _pendingBobsledRelations)
		{
			if (pendingBobsledRelation.ChildSyncId == syncId && !pendingBobsledRelation.HasBoundChild)
			{
				pendingBobsledRelation.Child = character;
				pendingBobsledRelation.HasBoundChild = true;
			}
			if (pendingBobsledRelation.ParentSyncId == syncId && !pendingBobsledRelation.HasBoundParent)
			{
				pendingBobsledRelation.Parent = character;
				pendingBobsledRelation.HasBoundParent = true;
			}
		}
	}

	public void ApplyCharacterComponentOperation(Dictionary envelope)
	{
		bool terminal = default(bool);
		if (_eventContext == null || envelope == null || TryApplyCharacterComponentOperation(envelope, out terminal) || terminal)
		{
			return;
		}
		int num = envelope.GetValueOrDefault("sync_id", -1).AsInt32();
		if (num >= 0)
		{
			if (_pendingComponentOperations.Count >= 256)
			{
				RemovePendingComponentOperationAt(0);
			}
			PendingComponentOperation pendingComponentOperation = new PendingComponentOperation
			{
				SyncId = num,
				Envelope = envelope.Duplicate(deep: true),
				RetryRemaining = 0.05
			};
			if (_eventContext.SyncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
			{
				pendingComponentOperation.Target = value;
				pendingComponentOperation.HasBoundTarget = true;
			}
			_pendingComponentOperations.Add(pendingComponentOperation);
			pendingComponentOperation.BarrierGeneration = _componentOperationDestroyBarrier?.TrackArrival(num) ?? 0;
		}
	}

	private void RemovePendingComponentOperationAt(int index)
	{
		if (index >= 0 && index < _pendingComponentOperations.Count)
		{
			PendingComponentOperation pendingComponentOperation = _pendingComponentOperations[index];
			_pendingComponentOperations.RemoveAt(index);
			_componentOperationDestroyBarrier?.MarkResolved(pendingComponentOperation.SyncId, pendingComponentOperation.BarrierGeneration);
		}
	}

	private void CapturePendingComponentOperationTarget(PendingComponentOperation pending)
	{
		if (pending != null && !pending.HasBoundTarget && _eventContext != null && _eventContext.SyncCharacters.TryGetValue(pending.SyncId, out var value) && GodotObject.IsInstanceValid(value))
		{
			pending.Target = value;
			pending.HasBoundTarget = true;
		}
	}

	private bool TryApplyCharacterComponentOperation(Dictionary envelope, out bool terminal, TowerDefenseCharacter expectedCharacter = null, bool hasExpectedCharacter = false)
	{
		terminal = true;
		int num = envelope.GetValueOrDefault("sync_id", -1).AsInt32();
		string text = envelope.GetValueOrDefault("component_instance_id", "").AsString();
		string text2 = envelope.GetValueOrDefault("component_type_id", "").AsString();
		string text3 = envelope.GetValueOrDefault("operation_name", "").AsString();
		long num2 = envelope.GetValueOrDefault("sequence", -1L).AsInt64();
		if (num < 0 || text == "" || text2 == "" || text3 == "" || num2 < 0)
		{
			return false;
		}
		if (!_eventContext.SyncCharacters.TryGetValue(num, out var value))
		{
			terminal = hasExpectedCharacter;
			return false;
		}
		if (hasExpectedCharacter && (!GodotObject.IsInstanceValid(expectedCharacter) || value != expectedCharacter))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(value) || value.isDestroy)
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(value.componentManager) || !value.componentManager.TryGetRuntimeByInstanceId(text, out var runtime))
		{
			terminal = false;
			return false;
		}
		if (!string.Equals(runtime.ComponentDefinition?.ComponentTypeId, text2, StringComparison.Ordinal))
		{
			return false;
		}
		Dictionary data = (envelope.ContainsKey("data") ? envelope["data"].AsGodotDictionary() : new Dictionary());
		long num3 = ((runtime is BobsledTeamComponent bobsledTeamComponent) ? bobsledTeamComponent.LastAppliedOperationSequence : (-1));
		runtime.ApplyNetworkOperation(text3, num2, data);
		if (runtime is BobsledTeamComponent bobsledTeamComponent2 && bobsledTeamComponent2.LastAppliedOperationSequence == num2 && num2 > num3 && (text3 == "release_all" || text3 == "cascade_kill"))
		{
			bool cascade = text3 == "cascade_kill";
			_bobsledTerminalByParent[num] = new BobsledTerminalState
			{
				Parent = value,
				Cascade = cascade
			};
			for (int i = 0; i < _pendingBobsledRelations.Count; i++)
			{
				PendingBobsledRelation pendingBobsledRelation = _pendingBobsledRelations[i];
				if (pendingBobsledRelation.ParentSyncId == num)
				{
					CapturePendingBobsledParent(pendingBobsledRelation);
					if (!pendingBobsledRelation.HasBoundParent || (GodotObject.IsInstanceValid(pendingBobsledRelation.Parent) && pendingBobsledRelation.Parent == value))
					{
						pendingBobsledRelation.HasTerminalOperation = true;
						pendingBobsledRelation.Cascade = cascade;
						pendingBobsledRelation.TerminalParent = value;
						pendingBobsledRelation.WaitForParentGeneration = false;
						pendingBobsledRelation.Elapsed = 0.0;
						pendingBobsledRelation.RetryRemaining = 0.0;
					}
				}
			}
		}
		return true;
	}

	public override void Process(double delta)
	{
		int num = 0;
		while (num < _pendingComponentOperations.Count)
		{
			PendingComponentOperation pendingComponentOperation = _pendingComponentOperations[num];
			CapturePendingComponentOperationTarget(pendingComponentOperation);
			if (_componentOperationDestroyBarrier != null && !_componentOperationDestroyBarrier.IsCurrent(pendingComponentOperation.SyncId, pendingComponentOperation.BarrierGeneration))
			{
				RemovePendingComponentOperationAt(num);
				continue;
			}
			pendingComponentOperation.Elapsed += delta;
			if (pendingComponentOperation.Elapsed >= 5.0)
			{
				RemovePendingComponentOperationAt(num);
				continue;
			}
			pendingComponentOperation.RetryRemaining -= delta;
			if (pendingComponentOperation.RetryRemaining > 0.0)
			{
				num++;
				continue;
			}
			pendingComponentOperation.RetryRemaining = 0.05;
			if (TryApplyCharacterComponentOperation(pendingComponentOperation.Envelope, out var terminal, pendingComponentOperation.Target, pendingComponentOperation.HasBoundTarget) || terminal)
			{
				RemovePendingComponentOperationAt(num);
			}
			else
			{
				num++;
			}
		}
		for (int num2 = _pendingDancerRelations.Count - 1; num2 >= 0; num2--)
		{
			PendingDancerRelation pendingDancerRelation = _pendingDancerRelations[num2];
			pendingDancerRelation.Elapsed += delta;
			if (pendingDancerRelation.Elapsed >= 5.0)
			{
				_pendingDancerRelations.RemoveAt(num2);
			}
			else
			{
				pendingDancerRelation.RetryRemaining -= delta;
				if (!(pendingDancerRelation.RetryRemaining > 0.0))
				{
					pendingDancerRelation.RetryRemaining = 0.05;
					if (TryApplyDancerRelation(pendingDancerRelation.ChildSyncId, pendingDancerRelation.ParentSyncId, pendingDancerRelation.Slot, out var terminal2) || terminal2)
					{
						_pendingDancerRelations.RemoveAt(num2);
					}
				}
			}
		}
		for (int num3 = _pendingBobsledRelations.Count - 1; num3 >= 0; num3--)
		{
			PendingBobsledRelation pendingBobsledRelation = _pendingBobsledRelations[num3];
			CapturePendingBobsledChild(pendingBobsledRelation);
			CapturePendingBobsledParent(pendingBobsledRelation);
			if (pendingBobsledRelation.HasBoundChild && (!GodotObject.IsInstanceValid(pendingBobsledRelation.Child) || !_eventContext.SyncCharacters.TryGetValue(pendingBobsledRelation.ChildSyncId, out var value) || value != pendingBobsledRelation.Child))
			{
				FailSafeReleaseBobsledRelation(pendingBobsledRelation);
				_pendingBobsledRelations.RemoveAt(num3);
				continue;
			}
			if (!pendingBobsledRelation.HasTerminalOperation && pendingBobsledRelation.HasBoundParent && (!GodotObject.IsInstanceValid(pendingBobsledRelation.Parent) || !_eventContext.SyncCharacters.TryGetValue(pendingBobsledRelation.ParentSyncId, out var value2) || value2 != pendingBobsledRelation.Parent))
			{
				FailSafeReleaseBobsledRelation(pendingBobsledRelation);
				_pendingBobsledRelations.RemoveAt(num3);
				continue;
			}
			pendingBobsledRelation.Elapsed += delta;
			if (pendingBobsledRelation.HasTerminalOperation && pendingBobsledRelation.WaitForParentGeneration)
			{
				if (_eventContext.SyncCharacters.TryGetValue(pendingBobsledRelation.ParentSyncId, out var value3) && GodotObject.IsInstanceValid(value3))
				{
					pendingBobsledRelation.WaitForParentGeneration = false;
					if (value3 != pendingBobsledRelation.TerminalParent)
					{
						_bobsledTerminalByParent.Remove(pendingBobsledRelation.ParentSyncId);
						FailSafeReleaseBobsledRelation(pendingBobsledRelation);
						_pendingBobsledRelations.RemoveAt(num3);
						continue;
					}
					pendingBobsledRelation.Elapsed = 0.0;
					pendingBobsledRelation.RetryRemaining = 0.0;
				}
				else
				{
					if (pendingBobsledRelation.Elapsed < 5.0)
					{
						continue;
					}
					pendingBobsledRelation.WaitForParentGeneration = false;
					pendingBobsledRelation.Elapsed = 0.0;
					pendingBobsledRelation.RetryRemaining = 0.0;
				}
			}
			if (pendingBobsledRelation.Elapsed >= 5.0)
			{
				FailSafeReleaseBobsledRelation(pendingBobsledRelation);
				_pendingBobsledRelations.RemoveAt(num3);
				continue;
			}
			pendingBobsledRelation.RetryRemaining -= delta;
			if (pendingBobsledRelation.RetryRemaining > 0.0)
			{
				continue;
			}
			pendingBobsledRelation.RetryRemaining = 0.05;
			bool terminal3;
			bool flag = (pendingBobsledRelation.HasTerminalOperation ? TryFinalizeBobsledTerminalRelation(pendingBobsledRelation, out terminal3) : TryApplyBobsledRelation(pendingBobsledRelation, out terminal3));
			if (flag || terminal3)
			{
				if (!flag)
				{
					FailSafeReleaseBobsledRelation(pendingBobsledRelation);
				}
				_pendingBobsledRelations.RemoveAt(num3);
			}
		}
		ProcessBobsledTerminalStates(delta);
	}

	private void ProcessBobsledTerminalStates(double delta)
	{
		if (_bobsledTerminalByParent.Count == 0)
		{
			return;
		}
		List<int> list = new List<int>();
		foreach (KeyValuePair<int, BobsledTerminalState> item in _bobsledTerminalByParent)
		{
			item.Value.Elapsed += Math.Max(0.0, delta);
			if (item.Value.Elapsed >= 10.0)
			{
				list.Add(item.Key);
			}
		}
		foreach (int item2 in list)
		{
			_bobsledTerminalByParent.Remove(item2);
		}
	}

	public override void Dispose()
	{
		for (int num = _pendingComponentOperations.Count - 1; num >= 0; num--)
		{
			RemovePendingComponentOperationAt(num);
		}
		_pendingComponentOperations.Clear();
		_pendingDancerRelations.Clear();
		_trioSpawnIds.Clear();
		foreach (PendingBobsledRelation pendingBobsledRelation in _pendingBobsledRelations)
		{
			FailSafeReleaseBobsledRelation(pendingBobsledRelation);
		}
		_pendingBobsledRelations.Clear();
		_bobsledTerminalByParent.Clear();
		_componentOperationDestroyBarrier = null;
		base.Dispose();
	}

	private void ApplyDancerRelationOrQueue(int childSyncId, Dictionary spawnState)
	{
		if (childSyncId < 0 || spawnState == null || !spawnState.ContainsKey("dancer_parent_sync_id"))
		{
			return;
		}
		int parentSyncId = spawnState["dancer_parent_sync_id"].AsInt32();
		int slot = spawnState.GetValueOrDefault("dancer_slot", -1).AsInt32();
		if (parentSyncId >= 0 && slot >= 0 && slot < 4)
		{
			_pendingDancerRelations.RemoveAll((PendingDancerRelation pending) => pending.ChildSyncId == childSyncId || (pending.ParentSyncId == parentSyncId && pending.Slot == slot));
			if (!(TryApplyDancerRelation(childSyncId, parentSyncId, slot, out var terminal) || terminal))
			{
				_pendingDancerRelations.Add(new PendingDancerRelation
				{
					ChildSyncId = childSyncId,
					ParentSyncId = parentSyncId,
					Slot = slot,
					RetryRemaining = 0.05
				});
			}
		}
	}

	private bool TryApplyDancerRelation(int childSyncId, int parentSyncId, int slot, out bool terminal)
	{
		terminal = true;
		if (_eventContext == null || childSyncId < 0 || parentSyncId < 0 || slot < 0 || slot >= 4)
		{
			return false;
		}
		if (!_eventContext.SyncCharacters.TryGetValue(childSyncId, out var value) || !_eventContext.SyncCharacters.TryGetValue(parentSyncId, out var value2))
		{
			terminal = false;
			return false;
		}
		if (!GodotObject.IsInstanceValid(value) || !GodotObject.IsInstanceValid(value2) || value.isDestroy || value2.isDestroy)
		{
			return false;
		}
		if (!(value is IDancer) || !(value2 is INetworkDancerOwner networkDancerOwner))
		{
			return false;
		}
		networkDancerOwner.SetNetworkDancer(slot, value);
		return true;
	}

	private void ApplyBobsledRelationOrQueue(int childSyncId, Dictionary spawnState)
	{
		if (childSyncId < 0 || spawnState == null || !spawnState.ContainsKey("bobsled_parent_sync_id"))
		{
			return;
		}
		int num = spawnState["bobsled_parent_sync_id"].AsInt32();
		int num2 = spawnState.GetValueOrDefault("bobsled_slot", -1).AsInt32();
		if (num < 0 || num2 < 0 || num2 >= 4)
		{
			return;
		}
		RemoveConflictingPendingBobsledRelations(childSyncId, num, num2);
		PendingBobsledRelation pendingBobsledRelation = new PendingBobsledRelation
		{
			ChildSyncId = childSyncId,
			ParentSyncId = num,
			Slot = num2,
			RetryRemaining = 0.05
		};
		CapturePendingBobsledChild(pendingBobsledRelation);
		CapturePendingBobsledParent(pendingBobsledRelation);
		if (_bobsledTerminalByParent.TryGetValue(num, out var value))
		{
			if (!_eventContext.SyncCharacters.TryGetValue(num, out var value2) || !GodotObject.IsInstanceValid(value2) || value2 == value.Parent)
			{
				pendingBobsledRelation.HasTerminalOperation = true;
				pendingBobsledRelation.Cascade = value.Cascade;
				pendingBobsledRelation.TerminalParent = value.Parent;
				pendingBobsledRelation.Parent = value.Parent;
				pendingBobsledRelation.HasBoundParent = true;
				TowerDefenseCharacter value3;
				bool flag = _eventContext.SyncCharacters.TryGetValue(num, out value3) && GodotObject.IsInstanceValid(value3);
				pendingBobsledRelation.WaitForParentGeneration = !flag;
				if (flag)
				{
					bool terminal;
					bool flag2 = TryFinalizeBobsledTerminalRelation(pendingBobsledRelation, out terminal);
					if (flag2 || terminal)
					{
						if (!flag2)
						{
							FailSafeReleaseBobsledRelation(pendingBobsledRelation);
						}
						return;
					}
				}
				QueuePendingBobsledRelation(pendingBobsledRelation);
				return;
			}
			_bobsledTerminalByParent.Remove(num);
		}
		bool terminal2;
		bool flag3 = TryApplyBobsledRelation(pendingBobsledRelation, out terminal2);
		if (flag3 || terminal2)
		{
			if (!flag3)
			{
				FailSafeReleaseBobsledRelation(pendingBobsledRelation);
			}
		}
		else
		{
			QueuePendingBobsledRelation(pendingBobsledRelation);
		}
	}

	private void QueuePendingBobsledRelation(PendingBobsledRelation relation)
	{
		if (relation != null)
		{
			while (_pendingBobsledRelations.Count >= 256)
			{
				FailSafeReleaseBobsledRelation(_pendingBobsledRelations[0]);
				_pendingBobsledRelations.RemoveAt(0);
			}
			_pendingBobsledRelations.Add(relation);
		}
	}

	private void RemoveConflictingPendingBobsledRelations(int childSyncId, int parentSyncId, int slot)
	{
		for (int num = _pendingBobsledRelations.Count - 1; num >= 0; num--)
		{
			PendingBobsledRelation pendingBobsledRelation = _pendingBobsledRelations[num];
			if (pendingBobsledRelation.ChildSyncId == childSyncId || (pendingBobsledRelation.ParentSyncId == parentSyncId && pendingBobsledRelation.Slot == slot))
			{
				FailSafeReleaseBobsledRelation(pendingBobsledRelation);
				_pendingBobsledRelations.RemoveAt(num);
			}
		}
	}

	private void CapturePendingBobsledChild(PendingBobsledRelation pending)
	{
		if (pending != null && !pending.HasBoundChild && _eventContext != null && _eventContext.SyncCharacters.TryGetValue(pending.ChildSyncId, out var value) && GodotObject.IsInstanceValid(value))
		{
			pending.Child = value;
			pending.HasBoundChild = true;
		}
	}

	private void CapturePendingBobsledParent(PendingBobsledRelation pending)
	{
		if (pending != null && !pending.HasBoundParent && _eventContext != null && _eventContext.SyncCharacters.TryGetValue(pending.ParentSyncId, out var value) && GodotObject.IsInstanceValid(value))
		{
			pending.Parent = value;
			pending.HasBoundParent = true;
		}
	}

	private void FailSafeReleaseBobsledRelation(PendingBobsledRelation pending)
	{
		CapturePendingBobsledChild(pending);
		if (pending?.Child is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie) && !towerDefenseZombie.isDestroy && towerDefenseZombie.syncId == pending.ChildSyncId)
		{
			ReleaseBobsledPassenger(towerDefenseZombie);
		}
	}

	private void ReleaseBobsledPassenger(TowerDefenseZombie passenger)
	{
		ReleaseBobsledPassengerNow(passenger);
		if (GodotObject.IsInstanceValid(passenger))
		{
			ulong instanceId = passenger.GetInstanceId();
			_nextBobsledReleaseGeneration++;
			if (_nextBobsledReleaseGeneration == 0L)
			{
				_nextBobsledReleaseGeneration = 1uL;
			}
			ulong generation = _nextBobsledReleaseGeneration;
			_pendingBobsledReleaseGenerations[instanceId] = generation;
			Callable.From(delegate
			{
				CompletePendingBobsledRelease(passenger, instanceId, generation);
			}).CallDeferred();
		}
	}

	private void CompletePendingBobsledRelease(TowerDefenseZombie passenger, ulong instanceId, ulong generation)
	{
		if (_pendingBobsledReleaseGenerations.TryGetValue(instanceId, out var value) && value == generation)
		{
			if (!GodotObject.IsInstanceValid(passenger) || passenger.isDestroy)
			{
				_pendingBobsledReleaseGenerations.Remove(instanceId);
				return;
			}
			if (!passenger.IsNodeReady())
			{
				passenger.Ready += CompleteWhenReady;
				return;
			}
			_pendingBobsledReleaseGenerations.Remove(instanceId);
			ReleaseBobsledPassengerNow(passenger);
			passenger.WalkReady();
		}
		void CompleteWhenReady()
		{
			if (GodotObject.IsInstanceValid(passenger))
			{
				passenger.Ready -= CompleteWhenReady;
			}
			CompletePendingBobsledRelease(passenger, instanceId, generation);
		}
	}

	private void CancelPendingBobsledRelease(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			_pendingBobsledReleaseGenerations.Remove(character.GetInstanceId());
		}
	}

	private static void ReleaseBobsledPassengerNow(TowerDefenseZombie passenger)
	{
		if (GodotObject.IsInstanceValid(passenger) && !passenger.isDestroy)
		{
			passenger.invisible = false;
			passenger.Visible = true;
			passenger.isPause = false;
		}
	}

	private bool TryFinalizeBobsledTerminalRelation(PendingBobsledRelation pending, out bool terminal)
	{
		terminal = true;
		if (_eventContext == null || pending == null || pending.ChildSyncId < 0)
		{
			return false;
		}
		CapturePendingBobsledChild(pending);
		if (pending.Cascade && _eventContext.SyncCharacters.TryGetValue(pending.ParentSyncId, out var value) && (!GodotObject.IsInstanceValid(value) || value != pending.TerminalParent))
		{
			return false;
		}
		if (!_eventContext.SyncCharacters.TryGetValue(pending.ChildSyncId, out var value2))
		{
			terminal = false;
			return false;
		}
		if (pending.HasBoundChild && (!GodotObject.IsInstanceValid(pending.Child) || value2 != pending.Child))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(value2) || value2.isDestroy)
		{
			return false;
		}
		if (!(value2 is TowerDefenseZombie towerDefenseZombie) || !towerDefenseZombie.IsNodeReady())
		{
			terminal = !(value2 is TowerDefenseZombie);
			return false;
		}
		if (pending.Cascade)
		{
			DestroyComponent destroyComponent = towerDefenseZombie.destroyComponent;
			if (destroyComponent == null || destroyComponent.IsReleased)
			{
				terminal = false;
				return false;
			}
			towerDefenseZombie.destroyComponent.isRemoteDestroy = true;
			towerDefenseZombie.skipDestroySet = true;
			towerDefenseZombie.Destroy();
		}
		else
		{
			ReleaseBobsledPassenger(towerDefenseZombie);
		}
		return true;
	}

	private bool TryApplyBobsledRelation(PendingBobsledRelation pending, out bool terminal)
	{
		terminal = true;
		if (_eventContext == null || pending == null || pending.ChildSyncId < 0 || pending.ParentSyncId < 0 || pending.ChildSyncId == pending.ParentSyncId || pending.Slot < 0 || pending.Slot >= 4)
		{
			return false;
		}
		CapturePendingBobsledChild(pending);
		CapturePendingBobsledParent(pending);
		if (!_eventContext.SyncCharacters.TryGetValue(pending.ChildSyncId, out var value) || !_eventContext.SyncCharacters.TryGetValue(pending.ParentSyncId, out var value2))
		{
			terminal = false;
			return false;
		}
		if (pending.HasBoundChild && (!GodotObject.IsInstanceValid(pending.Child) || value != pending.Child))
		{
			return false;
		}
		if (pending.HasBoundParent && (!GodotObject.IsInstanceValid(pending.Parent) || value2 != pending.Parent))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(value) || !GodotObject.IsInstanceValid(value2) || value.isDestroy || value2.isDestroy)
		{
			return false;
		}
		if (!value.IsNodeReady() || !value2.IsNodeReady())
		{
			terminal = false;
			return false;
		}
		if (!(value is TowerDefenseZombie) || !(value2 is INetworkBobsledOwner networkBobsledOwner))
		{
			return false;
		}
		if (networkBobsledOwner.TrySetNetworkBobsledPassenger(pending.Slot, value))
		{
			CancelPendingBobsledRelease(value);
			return true;
		}
		terminal = false;
		return false;
	}

	public Godot.Collections.Array BuildCharacterRoster()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		if (_eventContext == null)
		{
			return array;
		}
		foreach (KeyValuePair<int, TowerDefenseCharacter> syncCharacter in _eventContext.SyncCharacters)
		{
			TowerDefenseCharacter value = syncCharacter.Value;
			if (!GodotObject.IsInstanceValid(value) || value.isDestroy || !GodotObject.IsInstanceValid(value.packet) || value.packet.saveKey == "")
			{
				continue;
			}
			Vector2 logicalGlobalPosition = value.GetLogicalGlobalPosition();
			Dictionary dictionary = new Dictionary
			{
				["packet_name"] = value.packet.saveKey,
				["grid_x"] = value.gridPos.X,
				["grid_y"] = value.gridPos.Y,
				["sync_id"] = syncCharacter.Key,
				["hitpoint_scale"] = (GodotObject.IsInstanceValid(value.instance) ? value.instance.hitpointScale : 1.0),
				["scale"] = (GodotObject.IsInstanceValid(value.transformPoint) ? value.transformPoint.Scale.X : 1f),
				["hypnoses"] = GodotObject.IsInstanceValid(value.instance) && value.instance.hypnoses,
				["use_create"] = !(value is TowerDefensePlant),
				["pos_x"] = logicalGlobalPosition.X,
				["pos_y"] = logicalGlobalPosition.Y,
				["ground_height"] = value.groundHeight,
				["economy_owner"] = value.EconomyOwnerAccountId.ToString()
			};
			Dictionary dictionary2 = value.ExportNetworkSpawnState();
			foreach (KeyValuePair<Variant, Variant> item in TrioAmbushMember.ExportSpawnState(value))
			{
				dictionary2[item.Key] = item.Value;
			}
			if (dictionary2.Count > 0)
			{
				dictionary["spawn_state"] = dictionary2;
			}
			array.Add(dictionary);
		}
		return array;
	}

	public void ApplyCharacterRoster(Godot.Collections.Array roster)
	{
		if (_eventContext == null || roster == null)
		{
			return;
		}
		foreach (Variant item in roster)
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary = item.AsGodotDictionary();
				int num = dictionary.GetValueOrDefault("sync_id", -1).AsInt32();
				if (num >= 0 && !TryGetLiveCharacter(num, out var _))
				{
					ApplySpawnCharacterAt(dictionary);
				}
			}
		}
	}

	public void ApplySpawnCharacterAt(Dictionary data)
	{
		if (_eventContext == null || data == null)
		{
			return;
		}
		string text = data.GetValueOrDefault("packet_name", "").AsString();
		if (text == "")
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		Vector2I vector2I = new Vector2I(data.GetValueOrDefault("grid_x", 0).AsInt32(), data.GetValueOrDefault("grid_y", 0).AsInt32());
		int num = data.GetValueOrDefault("sync_id", -1).AsInt32();
		double hitpointScale = data.GetValueOrDefault("hitpoint_scale", 1.0).AsDouble();
		double scaleVal = data.GetValueOrDefault("scale", 1.0).AsDouble();
		bool hypnoses = data.GetValueOrDefault("hypnoses", false).AsBool();
		double riseDuration = data.GetValueOrDefault("rise_duration", 0.0).AsDouble();
		bool flag = data.GetValueOrDefault("use_create", false).AsBool();
		bool walkAfterSpawn = data.GetValueOrDefault("walk_after_spawn", false).AsBool();
		double num2 = data.GetValueOrDefault("ground_height", 0.0).AsDouble();
		EconomyAccountId.TryParse(data.GetValueOrDefault("economy_owner", "").AsString(), out var accountId);
		string sizeVal = data.GetValueOrDefault("size", "").AsString();
		Dictionary spawnState = (data.ContainsKey("spawn_state") ? data["spawn_state"].AsGodotDictionary() : null);
		Dictionary dictionary = spawnState;
		if (dictionary != null && dictionary.ContainsKey("trio_ambush"))
		{
			if (num < 0)
			{
				return;
			}
			if (TryGetLiveCharacter(num, out var character))
			{
				TrioAmbushMember.ApplyNetworkState(character, spawnState);
				return;
			}
			if (!_trioSpawnIds.Add(num))
			{
				return;
			}
		}
		TowerDefenseCharacter character2;
		if (flag)
		{
			Vector2 pos = new Vector2((float)data.GetValueOrDefault("pos_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("pos_y", 0.0).AsDouble());
			character2 = (accountId.IsValid ? packetConfig.Create(accountId, pos, vector2I, (float)num2) : packetConfig.Create(pos, vector2I, (float)num2));
			if (GodotObject.IsInstanceValid(character2))
			{
				ApplySpawnCharacterPreState(character2, spawnState);
			}
			if (GodotObject.IsInstanceValid(character2) && character2 is INetworkSpawnStateReceiver networkSpawnStateReceiver && spawnState != null)
			{
				networkSpawnStateReceiver.ImportNetworkSpawnState(spawnState);
			}
			if (GodotObject.IsInstanceValid(character2))
			{
				TowerDefenseGroundItemBase.characterNode.CallDeferred("add_child", character2);
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(character2))
					{
						ApplySpawnCharacterPostState(character2, hitpointScale, scaleVal, hypnoses, riseDuration, walkAfterSpawn, sizeVal, spawnState);
					}
				}).CallDeferred();
			}
		}
		else
		{
			bool playAudio = spawnState?.GetValueOrDefault("plant_play_audio", true).AsBool() ?? true;
			bool noLimit = spawnState?.GetValueOrDefault("plant_no_limit", true).AsBool() ?? true;
			bool flag2 = spawnState?.GetValueOrDefault("plant_skip_placement_check", false).AsBool() ?? false;
			TowerDefenseCharacter towerDefenseCharacter;
			if (!accountId.IsValid)
			{
				Vector2I gridPos = vector2I;
				bool skipPlacementCheck = flag2;
				towerDefenseCharacter = packetConfig.Plant(gridPos, playAudio, noLimit, default(EconomyAccountId), skipPlacementCheck);
			}
			else
			{
				towerDefenseCharacter = packetConfig.Plant(accountId, vector2I, playAudio, noLimit, flag2);
			}
			character2 = towerDefenseCharacter;
			if (GodotObject.IsInstanceValid(character2) && character2 is INetworkSpawnStateReceiver networkSpawnStateReceiver2 && spawnState != null)
			{
				networkSpawnStateReceiver2.ImportNetworkSpawnState(spawnState);
			}
			if (GodotObject.IsInstanceValid(character2))
			{
				Callable.From(delegate
				{
					if (GodotObject.IsInstanceValid(character2))
					{
						ApplySpawnCharacterPostState(character2, hitpointScale, scaleVal, hypnoses, riseDuration, walkAfterSpawn, sizeVal, spawnState);
					}
				}).CallDeferred();
			}
		}
		if (GodotObject.IsInstanceValid(character2) && num >= 0)
		{
			_eventContext.RegisterCharacter(num, character2);
			ApplyDancerRelationOrQueue(num, spawnState);
			ApplyBobsledRelationOrQueue(num, spawnState);
		}
	}

	public void ApplyConveyorSpawn(Dictionary data)
	{
		if (_eventContext == null || data == null)
		{
			return;
		}
		string text = data.GetValueOrDefault("packet_name", "").AsString();
		string packetType = data.GetValueOrDefault("packet_type", "Default").AsString();
		if (text == "")
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			TowerDefenseBattleFeatureConveyorBelt towerDefenseBattleFeatureConveyorBelt = _eventContext.GetFeature("ConveyorBelt") as TowerDefenseBattleFeatureConveyorBelt;
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureConveyorBelt))
			{
				towerDefenseBattleFeatureConveyorBelt.SpawnPacketFromSync(packetConfig, packetType);
			}
		}
	}

	public void ApplyVaseBreak(Dictionary data)
	{
		if (_eventContext == null || data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("break_id", -1).AsInt32();
		if (num < 0 || _appliedVaseBreakIds.Add(num))
		{
			Vector2I gridPos = new Vector2I(data.GetValueOrDefault("grid_x", 0).AsInt32(), data.GetValueOrDefault("grid_y", 0).AsInt32());
			BreakVaseAt(gridPos);
			string text = data.GetValueOrDefault("content_type", "none").AsString();
			string text2 = data.GetValueOrDefault("content_name", "").AsString();
			if (text == "zombie" && text2 != "")
			{
				ApplyVaseZombieContent(data, gridPos, text2);
			}
			else if (text == "plant" && data.ContainsKey("packet_show"))
			{
				ApplyVasePacketContent(data["packet_show"].AsGodotDictionary());
			}
		}
	}

	public void ApplySpawnCoin(Dictionary data)
	{
		if (data != null && TowerDefenseManager.Instance != null)
		{
			Vector2 pos = new Vector2((float)data.GetValueOrDefault("pos_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("pos_y", 0.0).AsDouble());
			Vector2 velocity = new Vector2((float)data.GetValueOrDefault("velocity_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("velocity_y", -400.0).AsDouble());
			TowerDefenseManager.Instance.CoinCreate(pos, data.GetValueOrDefault("num", 10).AsInt32(), (float)data.GetValueOrDefault("height", 0.0).AsDouble(), velocity, (float)data.GetValueOrDefault("gravity", 980.0).AsDouble(), data.GetValueOrDefault("collect", false).AsBool());
		}
	}

	public void ApplySpawnFallingObject(Dictionary data)
	{
		if (data == null || TowerDefenseManager.Instance == null)
		{
			return;
		}
		ObjectManagerConfig.OBJECT oBJECT = (ObjectManagerConfig.OBJECT)data.GetValueOrDefault("object_id", 0).AsInt32();
		if (oBJECT == ObjectManagerConfig.OBJECT.NOONE || DropItemRegistry.GetById(oBJECT) == null)
		{
			return;
		}
		long num = data.GetValueOrDefault("spawn_sequence", 0).AsInt64();
		if (num > 0 && _appliedFallingObjectSpawnSequences.Add(num))
		{
			Vector2 pos = new Vector2((float)data.GetValueOrDefault("pos_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("pos_y", 0.0).AsDouble());
			Vector2 velocity = new Vector2((float)data.GetValueOrDefault("velocity_x", 0.0).AsDouble(), (float)data.GetValueOrDefault("velocity_y", -400.0).AsDouble());
			TowerDefenseGroundItemBase towerDefenseGroundItemBase = TowerDefenseManager.Instance.FallingObjectItemCreate(oBJECT, pos, (float)data.GetValueOrDefault("height", 0.0).AsDouble(), velocity, (float)data.GetValueOrDefault("gravity", 980.0).AsDouble());
			if (GodotObject.IsInstanceValid(towerDefenseGroundItemBase))
			{
				towerDefenseGroundItemBase.gridPos = new Vector2I(data.GetValueOrDefault("grid_x", towerDefenseGroundItemBase.gridPos.X).AsInt32(), data.GetValueOrDefault("grid_y", towerDefenseGroundItemBase.gridPos.Y).AsInt32());
			}
		}
	}

	public void ApplyDamagePart(DamagePartDto dto)
	{
		if (_eventContext != null && dto != null && dto.sync_id >= 0 && !string.IsNullOrEmpty(dto.part_name) && double.IsFinite(dto.px) && double.IsFinite(dto.py) && double.IsFinite(dto.vx) && double.IsFinite(dto.vy) && TryGetLiveCharacter(dto.sync_id, out var character))
		{
			character.DamagePartCreate(new StringName(dto.part_name), null, new Vector2((float)dto.vx, (float)dto.vy), keepSlotScale: true, Vector2.Zero, fromSync: true, new Vector2((float)dto.px, (float)dto.py), dto.seq);
		}
	}

	public void ApplyDamagePointReach(DamagePointReachDto dto)
	{
		if (_eventContext != null && dto != null && dto.sync_id >= 0 && !(dto.damage_point_name == "") && TryGetLiveCharacter(dto.sync_id, out var character))
		{
			character.DamagePointReach(dto.damage_point_name);
		}
	}

	public void ApplyArmorDamagePointReach(ArmorDamagePointReachDto dto)
	{
		if (_eventContext != null && dto != null && dto.sync_id >= 0 && !(dto.armor_name == "") && TryGetLiveCharacter(dto.sync_id, out var character))
		{
			character.ArmorDamagePointReach(dto.armor_name, dto.stage);
		}
	}

	public void ApplyArmorHitpointsEmpty(ArmorHitpointsEmptyDto dto)
	{
		if (_eventContext != null && dto != null && dto.sync_id >= 0 && !(dto.armor_name == "") && TryGetLiveCharacter(dto.sync_id, out var character))
		{
			character.ArmorHitpointsEmpty(dto.armor_name);
		}
	}

	public void ApplyCraterCreate(CraterCreateDto dto)
	{
		if (dto == null)
		{
			return;
		}
		Vector2I gridPos = new Vector2I(dto.grid_x, dto.grid_y);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		if (GodotObject.IsInstanceValid(mapCell) && mapCell.CanCraterCreate())
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(dto.crater_name);
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				packetConfig.Plant(gridPos, playAudio: false, noLimit: true);
			}
		}
	}

	public void ApplyProjectileEffectSpawn(ProjectileEffectSpawnDto dto)
	{
		if (dto != null)
		{
			IBattleNetworkContext context = base.Context;
			if ((context == null || !context.IsHost) && !NetworkProjectileEffectRegistry.TrySpawnReplay(dto))
			{
				GD.PushWarning("Dropped invalid or unknown projectile effect spawn '" + dto.effect_id + "'.");
			}
		}
	}

	public void ApplyEventExecute(Dictionary data)
	{
		if (_eventContext == null || data == null)
		{
			return;
		}
		IBattleNetworkContext context = base.Context;
		if (context != null && context.IsHost)
		{
			return;
		}
		string text = data.GetValueOrDefault("phase", "").AsString();
		Variant variant = Json.ParseString(data.GetValueOrDefault("events", "").AsString());
		if (!(text == "") && variant.VariantType == Variant.Type.Array)
		{
			TowerDefenseBattleFeatureEvent towerDefenseBattleFeatureEvent = _eventContext.GetFeature("Event") as TowerDefenseBattleFeatureEvent;
			if (!GodotObject.IsInstanceValid(towerDefenseBattleFeatureEvent))
			{
				_eventContext.EnsureFeature("Event");
				towerDefenseBattleFeatureEvent = _eventContext.GetFeature("Event") as TowerDefenseBattleFeatureEvent;
			}
			if (GodotObject.IsInstanceValid(towerDefenseBattleFeatureEvent))
			{
				towerDefenseBattleFeatureEvent.ApplyRemoteEventExecute(text, variant.AsGodotArray());
			}
			else
			{
				GD.PushWarning("[Event] Dropped remote phase '" + text + "' because the Event feature could not be created.");
			}
		}
	}

	public void ApplyWaveEventExecute(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		IBattleNetworkContext context = base.Context;
		if (context != null && context.IsHost)
		{
			return;
		}
		Variant variant = Json.ParseString(data.GetValueOrDefault("events", "").AsString());
		if (variant.VariantType != Variant.Type.Array)
		{
			return;
		}
		int value = data.GetValueOrDefault("wave_id", -1).AsInt32();
		int num = data.GetValueOrDefault("event_id", -1).AsInt32();
		if (num >= 0 && !_appliedWaveEventIds.Add(num))
		{
			return;
		}
		List<TowerDefenseLevelEventBase> list = new List<TowerDefenseLevelEventBase>();
		Godot.Collections.Array array = variant.AsGodotArray();
		for (int i = 0; i < array.Count; i++)
		{
			try
			{
				if (array[i].VariantType != Variant.Type.Dictionary)
				{
					continue;
				}
				Dictionary dictionary = array[i].AsGodotDictionary();
				string text = dictionary.GetValueOrDefault("EventName", "").AsString();
				if (!(text == ""))
				{
					TowerDefenseLevelEventBase towerDefenseLevelEventBase = TowerDefenseLevelEventRegistry.Create(text);
					if (!GodotObject.IsInstanceValid(towerDefenseLevelEventBase))
					{
						GD.PushError($"[WaveEvent] Unknown remote event '{text}' in wave {value}.");
					}
					else
					{
						Dictionary valueDictionary = dictionary.GetValueOrDefault("Value", new Dictionary()).AsGodotDictionary();
						towerDefenseLevelEventBase.Init(valueDictionary);
						list.Add(towerDefenseLevelEventBase);
					}
				}
			}
			catch (Exception value2)
			{
				GD.PushError($"[WaveEvent] Failed to initialize wave {value} item {i}: {value2}");
			}
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.ExecuteLevelEvent(list);
		}
	}

	private bool TryGetLiveCharacter(int syncId, out TowerDefenseCharacter character)
	{
		character = null;
		if (_eventContext == null || !_eventContext.SyncCharacters.ContainsKey(syncId))
		{
			return false;
		}
		character = _eventContext.SyncCharacters[syncId];
		if (GodotObject.IsInstanceValid(character))
		{
			return !character.isDestroy;
		}
		return false;
	}

	private static void ApplySpawnCharacterPreState(TowerDefenseCharacter character, Dictionary spawnState)
	{
		if (!GodotObject.IsInstanceValid(character) || spawnState == null)
		{
			return;
		}
		if (spawnState.GetValueOrDefault("component_gameplay_until_battlefield_entry", false).AsBool())
		{
			character.EnableComponentGameplayUntilBattlefieldEntry();
		}
		if (character is TowerDefenseVase towerDefenseVase)
		{
			if (spawnState.ContainsKey("vase_use_enter_anime"))
			{
				towerDefenseVase.useEnterAnime = spawnState["vase_use_enter_anime"].AsBool();
			}
			if (spawnState.ContainsKey("vase_packet_bank"))
			{
				towerDefenseVase.packetBank = spawnState["vase_packet_bank"].AsString();
			}
		}
	}

	private static void ApplySpawnCharacterPostState(TowerDefenseCharacter character, double hitpointScale, double scaleVal, bool hypnoses, double riseDuration, bool walkAfterSpawn, string sizeVal, Dictionary spawnState)
	{
		if (sizeVal != "")
		{
			if (character is TowerDefenseItemSnowBall towerDefenseItemSnowBall)
			{
				towerDefenseItemSnowBall.SetSize(sizeVal);
			}
			else if (character.HasMethod("SetSize"))
			{
				character.CallDeferred("SetSize", sizeVal);
			}
		}
		if (hitpointScale != 1.0 && GodotObject.IsInstanceValid(character.instance))
		{
			character.instance.hitpointScale = hitpointScale;
		}
		if (scaleVal != 1.0 && GodotObject.IsInstanceValid(character.transformPoint))
		{
			character.transformPoint.Scale = new Vector2((float)scaleVal, (float)scaleVal);
		}
		if (hypnoses)
		{
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.Hypnoses();
				}
			}).CallDeferred();
		}
		TrioAmbushMember.ApplyNetworkState(character, spawnState);
		if (spawnState != null && spawnState.ContainsKey("spawn_invisible"))
		{
			character.invisible = spawnState["spawn_invisible"].AsBool();
		}
		if (spawnState != null && spawnState.GetValueOrDefault("wake_up", false).AsBool())
		{
			character.WakeUp();
		}
		if (riseDuration > 0.0)
		{
			double riseDelay = spawnState?.GetValueOrDefault("rise_delay", 0.0).AsDouble() ?? 0.0;
			bool riseCreateDirt = spawnState?.GetValueOrDefault("rise_create_dirt", true).AsBool() ?? true;
			bool riseChangeState = spawnState?.GetValueOrDefault("rise_change_state", true).AsBool() ?? true;
			double riseFrom = spawnState?.GetValueOrDefault("rise_from", 150.0).AsDouble() ?? 150.0;
			bool riseEmitOver = spawnState?.GetValueOrDefault("rise_emit_over", true).AsBool() ?? true;
			Callable.From(delegate
			{
				if (GodotObject.IsInstanceValid(character))
				{
					character.Rise(riseDuration, riseDelay, riseCreateDirt, riseChangeState, riseFrom, riseEmitOver);
				}
			}).CallDeferred();
		}
		if (spawnState != null && spawnState.GetValueOrDefault("walk_ready_after_spawn", false).AsBool() && character is TowerDefenseZombie towerDefenseZombie)
		{
			towerDefenseZombie.CallDeferred("WalkReady");
		}
		else if (walkAfterSpawn)
		{
			double num = spawnState?.GetValueOrDefault("walk_delay", 0.0).AsDouble() ?? 0.0;
			if (num > 0.0 && GodotObject.IsInstanceValid(character.GetTree()))
			{
				character.GetTree().CreateTimer(num, processAlways: false).Timeout += delegate
				{
					if (GodotObject.IsInstanceValid(character))
					{
						character.Call("Walk");
					}
				};
			}
			else
			{
				character.CallDeferred("Walk");
			}
		}
		if (spawnState != null && spawnState.ContainsKey("rotation_duration"))
		{
			double num2 = spawnState["rotation_duration"].AsDouble();
			double num3 = spawnState.GetValueOrDefault("rotation_from_degrees", character.RotationDegrees).AsDouble();
			double num4 = spawnState.GetValueOrDefault("rotation_to_degrees", character.RotationDegrees).AsDouble();
			if (num2 > 0.0)
			{
				character.CreateTween().TweenProperty(character, "rotation_degrees", num4, num2).From(num3);
			}
		}
	}

	private void BreakVaseAt(Vector2I gridPos)
	{
		foreach (Node item in _eventContext.GetNodesInGroup("Vase"))
		{
			if (item is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase) && !towerDefenseVase.over && towerDefenseVase.gridPos == gridPos)
			{
				towerDefenseVase.MultiplayerBreak();
				break;
			}
		}
	}

	private void ApplyVaseZombieContent(Dictionary data, Vector2I gridPos, string contentName)
	{
		if (!TowerDefensePacketRuntimeState.TryCreate(contentName, data, "content_override", "content_can_change_cost", "content_change_cost_list", out var packetConfig) || !GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		int num = data.GetValueOrDefault("sync_id", -1).AsInt32();
		if (num >= 0 && _eventContext.SyncCharacters.TryGetValue(num, out var value) && GodotObject.IsInstanceValid(value))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(gridPos);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			if (num >= 0)
			{
				_eventContext.RegisterCharacter(num, towerDefenseCharacter);
			}
			ApplyVaseZombiePostStateAsync(towerDefenseCharacter, data);
		}
	}

	private static async Task ApplyVaseZombiePostStateAsync(TowerDefenseCharacter zombie, Dictionary data)
	{
		try
		{
			SceneTree tree = TowerDefenseManager.CurrentControl?.GetTree();
			for (int frame = 0; frame < 2; frame++)
			{
				if (!GodotObject.IsInstanceValid(zombie))
				{
					break;
				}
				if (zombie.IsNodeReady())
				{
					break;
				}
				if (!GodotObject.IsInstanceValid(tree))
				{
					return;
				}
				await tree.ToSignal(tree, SceneTree.SignalName.ProcessFrame);
			}
			if (GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(zombie.instance))
			{
				zombie.instance.wakeUp = true;
				zombie.groundHeight = data.GetValueOrDefault("ground_height", 0.0).AsDouble();
				if (data.GetValueOrDefault("hypnoses", false).AsBool())
				{
					zombie.Hypnoses();
				}
			}
		}
		catch (Exception value)
		{
			GD.PushError($"[VaseBreak] Failed to apply zombie post state: {value}");
		}
	}

	private void ApplyVasePacketContent(Dictionary packetData)
	{
		if (_packetContext == null || packetData == null)
		{
			return;
		}
		string text = packetData.GetValueOrDefault("packet_name", "").AsString();
		if (text == "")
		{
			return;
		}
		int num = packetData.GetValueOrDefault("sync_id", -1).AsInt32();
		if (num >= 0 && _packetContext.TryGetPacket(num, out var _))
		{
			return;
		}
		IPacketNetworkContext packetContext = _packetContext;
		Vector2 position = new Vector2((float)packetData.GetValueOrDefault("pos_x", 0.0).AsDouble(), (float)packetData.GetValueOrDefault("pos_y", 0.0).AsDouble());
		double aliveTime = packetData.GetValueOrDefault("alive_time", 15.0).AsDouble();
		Vector2 velocity = new Vector2((float)packetData.GetValueOrDefault("velocity_x", 0.0).AsDouble(), (float)packetData.GetValueOrDefault("velocity_y", -300.0).AsDouble());
		int zIndex = packetData.GetValueOrDefault("z_index", 0).AsInt32();
		Dictionary runtimeState = TowerDefensePacketRuntimeState.Normalize(packetData, "packet_override", "can_change_cost", "change_cost_list");
		double gravity = packetData.GetValueOrDefault("gravity", 980.0).AsDouble();
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = packetContext.CreatePacket(new PacketSpawnState(num, text, position, aliveTime, isFall: false, useCost: false, velocity, zIndex, 0.0, 0.0, runtimeState, default(EconomyAccountId), gravity));
		if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
		{
			towerDefenseInGamePacketShow.AddToGroup("VasePacketShow");
			if (num >= 0)
			{
				_packetContext.RegisterPacket(num, towerDefenseInGamePacketShow);
			}
		}
	}
}
