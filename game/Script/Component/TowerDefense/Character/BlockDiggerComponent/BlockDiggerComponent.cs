using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class BlockDiggerComponent : CharacterComponentRuntime
{
	public TowerDefenseCharacter parent;

	private int _characterCheckFrame;

	private int _blockRevision;

	private int _lastReceivedBlockRevision = -1;

	private long _lastAppliedNetworkOperationSequence = -1L;

	private readonly HashSet<int> _blockedTargetSyncIds = new HashSet<int>();

	private readonly HashSet<int> _authoritativeBlockedTargetSyncIds = new HashSet<int>();

	private readonly HashSet<int> _appliedBlockedTargetSyncIds = new HashSet<int>();

	private static bool IsRemoteClient
	{
		get
		{
			if (Global.IsMultiplayerMode)
			{
				return !MultiPlayerManager.IsHost;
			}
			return false;
		}
	}

	private BlockDiggerComponentDefinition Definition => base.ComponentDefinition as BlockDiggerComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	protected override void OnBound()
	{
		parent = base.Owner;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		parent = null;
		_characterCheckFrame = 0;
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			ResetSynchronizedState();
		}
	}

	protected override void OnReleased()
	{
		parent = null;
		_characterCheckFrame = 0;
		ResetSynchronizedState();
	}

	private void ResetSynchronizedState()
	{
		_blockRevision = 0;
		_lastReceivedBlockRevision = -1;
		_lastAppliedNetworkOperationSequence = -1L;
		_blockedTargetSyncIds.Clear();
		_authoritativeBlockedTargetSyncIds.Clear();
		_appliedBlockedTargetSyncIds.Clear();
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		BlockDiggerComponentDefinition definition = Definition;
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || definition == null || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (IsRemoteClient)
		{
			ApplyAuthoritativeBlockedTargets();
		}
		else
		{
			if (!TowerDefenseManager._IsGameRunning() || parent.die || parent.nearDie || !parent.inGame || !parent.componentAlive || ++_characterCheckFrame < Mathf.Max(1, definition.characterCheckEveryPhysicsFrames))
			{
				return;
			}
			_characterCheckFrame = 0;
			if (!GodotObject.IsInstanceValid(definition.checkShape) || !definition.checkShape.TryGetWorldRect(parent.GetGlobalTransformForPhysicsFrame(physicsFrame), out var rect))
			{
				return;
			}
			foreach (TowerDefenseCharacter item in definition.blockSameCamp ? TowerDefenseManager.Instance.GetCharactersIntersectingRect(rect) : TowerDefenseManager.Instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, parent.camp))
			{
				if (GodotObject.IsInstanceValid(item) && GodotObject.IsInstanceValid(item.instance) && item != parent && !item.die && !item.nearDie && (definition.blockSameCamp || item.camp != parent.camp) && (item.instance.collisionFlags & 0x10) != 0 && item.CanDiggerBlock())
				{
					if (Global.IsMultiplayerMode && item.syncId >= 0 && _blockedTargetSyncIds.Add(item.syncId))
					{
						int num = ++_blockRevision;
						Dictionary data = new Dictionary
						{
							["target_sync_id"] = item.syncId,
							["revision"] = num
						};
						SendNetworkOperation("block_digger", num, data);
					}
					item.BlockDigger(parent);
				}
			}
		}
	}

	private void ApplyAuthoritativeBlockedTargets()
	{
		if (_authoritativeBlockedTargetSyncIds.Count == 0 || !GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl))
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		foreach (int authoritativeBlockedTargetSyncId in _authoritativeBlockedTargetSyncIds)
		{
			if (currentControl._syncCharacters.TryGetValue(authoritativeBlockedTargetSyncId, out var value) && GodotObject.IsInstanceValid(value) && value.IsNodeReady() && !value.die && !value.nearDie)
			{
				bool flag = (value.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty).EndsWith(".dig", StringComparison.Ordinal);
				if (!_appliedBlockedTargetSyncIds.Contains(authoritativeBlockedTargetSyncId) || flag)
				{
					value.BlockDigger(parent);
					_appliedBlockedTargetSyncIds.Add(authoritativeBlockedTargetSyncId);
				}
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		Array<int> array = new Array<int>();
		foreach (int blockedTargetSyncId in _blockedTargetSyncIds)
		{
			array.Add(blockedTargetSyncId);
		}
		return new Dictionary
		{
			["revision"] = _blockRevision,
			["blocked_target_sync_ids"] = array
		};
	}

	public override void SyncDeserialize(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("revision", 0).AsInt32();
		if (num < _lastReceivedBlockRevision)
		{
			return;
		}
		_lastReceivedBlockRevision = num;
		if (!data.ContainsKey("blocked_target_sync_ids"))
		{
			return;
		}
		foreach (Variant item in data["blocked_target_sync_ids"].AsGodotArray())
		{
			int num2 = item.AsInt32();
			if (num2 >= 0)
			{
				_authoritativeBlockedTargetSyncIds.Add(num2);
			}
		}
		ApplyAuthoritativeBlockedTargets();
	}

	public override void ApplyNetworkOperation(string operationName, long sequence, Dictionary data)
	{
		if (!IsRemoteClient || operationName != "block_digger" || data == null || sequence <= _lastAppliedNetworkOperationSequence)
		{
			return;
		}
		_lastAppliedNetworkOperationSequence = sequence;
		int num = data.GetValueOrDefault("revision", 0).AsInt32();
		if (num >= _lastReceivedBlockRevision)
		{
			_lastReceivedBlockRevision = num;
			int num2 = data.GetValueOrDefault("target_sync_id", -1).AsInt32();
			if (num2 >= 0)
			{
				_authoritativeBlockedTargetSyncIds.Add(num2);
				ApplyAuthoritativeBlockedTargets();
			}
		}
	}
}
