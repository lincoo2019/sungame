using System;
using Godot;
using Godot.Collections;

public sealed class BobsledTeamComponent : CharacterComponentRuntime
{
	private const double PendingResolveInterval = 0.25;

	private const string ReleaseOperation = "release_all";

	private const string CascadeOperation = "cascade_kill";

	private readonly TowerDefenseZombie[] _passengers = new TowerDefenseZombie[4];

	private readonly int[] _pendingSyncIds = new int[4] { -1, -1, -1, -1 };

	private readonly string[] _pendingNodeNames = new string[4] { "", "", "", "" };

	private readonly int[] _originalMaskFlags = new int[4];

	private readonly bool[] _originalVisible = new bool[4];

	private readonly bool[] _originalPaused = new bool[4];

	private readonly bool[] _originalGrounded = new bool[4];

	private readonly double[] _originalGroundHeight = new double[4];

	private readonly bool[] _hasOriginalState = new bool[4];

	private readonly Array<int> _syncPassengerIds = new Array<int> { -1, -1, -1, -1 };

	private Node2D[] _markers = new Node2D[4];

	private TowerDefenseZombie _parent;

	private int _slotCount = 4;

	private int _releasedMask;

	private int _cascadeMask;

	private int _occupiedMask;

	private long _operationSequence;

	private long _lastAppliedOperation = -1L;

	private double _pendingResolveRemaining;

	private BobsledTeamComponentDefinition Definition => base.ComponentDefinition as BobsledTeamComponentDefinition;

	public long LastAppliedOperationSequence => _lastAppliedOperation;

	private bool HasPending
	{
		get
		{
			for (int i = 0; i < _slotCount; i++)
			{
				if (_pendingSyncIds[i] >= 0 || !string.IsNullOrEmpty(_pendingNodeNames[i]))
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool HasAttachedPassengers => _occupiedMask != 0;

	internal override bool WantsPhysicsProcess
	{
		get
		{
			if (!HasPending)
			{
				return HasAttachedPassengers;
			}
			return true;
		}
	}

	internal override bool HasOwnerGameplayActivationWork => true;

	protected override void OnBound()
	{
		_parent = base.Owner as TowerDefenseZombie;
		_slotCount = Math.Clamp(Definition?.slotCount ?? 4, 1, 4);
		ResolveMarkers();
		ResolvePendingRelations();
		RefreshPhysicsRegistration();
	}

	protected override void OnActivated()
	{
		ResolvePendingRelations();
		RefreshPhysicsRegistration();
	}

	protected override void OnOwnerGameplayActivated()
	{
		EnsureAutoSpawnPassengers();
	}

	public void EnsureAutoSpawnPassengers()
	{
		BobsledTeamComponentDefinition definition = Definition;
		if ((definition == null || definition.autoSpawnPassengers) && GodotObject.IsInstanceValid(_parent) && !_parent.IsProgressRestoreInFlight)
		{
			SpawnAndAttachPassengers();
		}
	}

	protected override void OnOwnerBeforeDestroy()
	{
		if ((Definition?.ownerDestroyMode ?? BobsledOwnerDestroyMode.Release) == BobsledOwnerDestroyMode.Cascade)
		{
			CascadeKill(sendNetworkOperation: true, requireAuthority: true);
		}
		else
		{
			ReleaseAll(sendNetworkOperation: true, requireAuthority: true);
		}
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			SilentCleanup();
		}
		_parent = null;
		System.Array.Clear(_markers, 0, _markers.Length);
	}

	protected override void OnRuntimeReleased()
	{
		SilentCleanup();
		_parent = null;
		System.Array.Clear(_markers, 0, _markers.Length);
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		FollowPassengers(physicsFrame);
		if (HasPending)
		{
			_pendingResolveRemaining -= delta;
			if (_pendingResolveRemaining <= 0.0)
			{
				ResolvePendingRelations();
			}
		}
	}

	public TowerDefenseCharacter GetPassenger(int slot)
	{
		if (slot < 0 || slot >= _slotCount)
		{
			return null;
		}
		return _passengers[slot];
	}

	public bool IsSlotReleased(int slot)
	{
		if (slot >= 0 && slot < _slotCount)
		{
			return (_releasedMask & (1 << slot)) != 0;
		}
		return false;
	}

	public void ResolveReleasedNetworkPassenger(int slot, TowerDefenseCharacter character)
	{
		if (IsSlotReleased(slot) && character is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			if ((_cascadeMask & (1 << slot)) != 0)
			{
				DestroyPassengerForCascade(towerDefenseZombie);
			}
			else
			{
				RevealReleasedPassenger(towerDefenseZombie);
			}
		}
	}

	public void SpawnAndAttachPassengers()
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(_parent) || _parent.die || _parent.isDestroy || !GodotObject.IsInstanceValid(_parent.instance) || !GodotObject.IsInstanceValid(_parent.transformPoint))
		{
			return;
		}
		Node characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return;
		}
		for (int i = 0; i < _slotCount; i++)
		{
			if ((_releasedMask & (1 << i)) != 0 || GodotObject.IsInstanceValid(_passengers[i]))
			{
				continue;
			}
			string packetName = GetPacketName(i);
			if (string.IsNullOrWhiteSpace(packetName))
			{
				continue;
			}
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			if (GodotObject.IsInstanceValid(packetConfig))
			{
				Vector2 slotPosition = GetSlotPosition(i, 0uL);
				if ((_parent.EconomyOwnerAccountId.IsValid ? packetConfig.Create(_parent.EconomyOwnerAccountId, slotPosition, _parent.gridPos, _parent.groundHeight) : packetConfig.Create(slotPosition, _parent.gridPos, _parent.groundHeight)) is TowerDefenseZombie towerDefenseZombie)
				{
					characterNode.CallDeferred("add_child", towerDefenseZombie);
					towerDefenseZombie.CallDeferred("SetHitpointAndScale", _parent.instance.hitpointScale, _parent.transformPoint.Scale);
					SetNetworkPassenger(i, towerDefenseZombie);
					Dictionary spawnState = new Dictionary
					{
						["spawn_invisible"] = true,
						["bobsled_parent_sync_id"] = _parent.syncId,
						["bobsled_slot"] = i
					};
					TowerDefenseManager.PublishSpawnedCharacter(packetName, towerDefenseZombie, useCreate: true, 0.0, walkAfterSpawn: false, "", spawnState);
				}
			}
		}
	}

	public void SetNetworkPassenger(int slot, TowerDefenseCharacter character)
	{
		if (slot < 0 || slot >= _slotCount)
		{
			return;
		}
		TowerDefenseZombie towerDefenseZombie = character as TowerDefenseZombie;
		if ((GodotObject.IsInstanceValid(towerDefenseZombie) && (_releasedMask & (1 << slot)) != 0) || (towerDefenseZombie?.bobsledTeamOwner != null && towerDefenseZombie.bobsledTeamOwner != this))
		{
			return;
		}
		int num = -1;
		if (GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			for (int i = 0; i < _slotCount; i++)
			{
				if (_passengers[i] == towerDefenseZombie)
				{
					num = i;
					break;
				}
			}
		}
		TowerDefenseZombie towerDefenseZombie2 = _passengers[slot];
		if (GodotObject.IsInstanceValid(towerDefenseZombie2) && towerDefenseZombie2 != towerDefenseZombie)
		{
			DetachPassenger(slot, restore: true, destroy: false, markReleased: false, cascade: false);
		}
		if (num >= 0 && num != slot)
		{
			MovePassengerSlotState(num, slot);
		}
		if (!GodotObject.IsInstanceValid(towerDefenseZombie))
		{
			_passengers[slot] = null;
			_occupiedMask &= ~(1 << slot);
			_pendingSyncIds[slot] = -1;
			_pendingNodeNames[slot] = "";
			RefreshPhysicsRegistration();
		}
		else
		{
			towerDefenseZombie.bobsledTeamOwner = this;
			towerDefenseZombie.OnDestroy -= OnPassengerDestroyed;
			towerDefenseZombie.OnDestroy += OnPassengerDestroyed;
			_passengers[slot] = towerDefenseZombie;
			_occupiedMask |= 1 << slot;
			ApplyOrDeferAttachedPassengerState(slot, towerDefenseZombie);
			_pendingSyncIds[slot] = -1;
			_pendingNodeNames[slot] = "";
			RefreshPhysicsRegistration();
		}
	}

	private void MovePassengerSlotState(int sourceSlot, int targetSlot)
	{
		bool num = _hasOriginalState[sourceSlot];
		int num2 = _originalMaskFlags[sourceSlot];
		bool flag = _originalVisible[sourceSlot];
		bool flag2 = _originalPaused[sourceSlot];
		bool flag3 = _originalGrounded[sourceSlot];
		double num3 = _originalGroundHeight[sourceSlot];
		DetachPassenger(sourceSlot, restore: false, destroy: false, markReleased: false, cascade: false);
		if (num)
		{
			_hasOriginalState[targetSlot] = true;
			_originalMaskFlags[targetSlot] = num2;
			_originalVisible[targetSlot] = flag;
			_originalPaused[targetSlot] = flag2;
			_originalGrounded[targetSlot] = flag3;
			_originalGroundHeight[targetSlot] = num3;
		}
	}

	private void ApplyOrDeferAttachedPassengerState(int slot, TowerDefenseZombie passenger)
	{
		if (!passenger.IsNodeReady() || !GodotObject.IsInstanceValid(passenger.instance))
		{
			Callable.From(delegate
			{
				ApplyAttachedPassengerState(slot, passenger);
			}).CallDeferred();
		}
		else
		{
			ApplyAttachedPassengerState(slot, passenger);
		}
	}

	private void ApplyAttachedPassengerState(int slot, TowerDefenseZombie passenger)
	{
		if (GodotObject.IsInstanceValid(passenger) && passenger.IsNodeReady() && GodotObject.IsInstanceValid(passenger.instance) && _passengers[slot] == passenger && passenger.bobsledTeamOwner == this)
		{
			if (!_hasOriginalState[slot] && GodotObject.IsInstanceValid(passenger.instance))
			{
				_originalMaskFlags[slot] = passenger.instance.maskFlags;
				_originalVisible[slot] = passenger.Visible;
				_originalPaused[slot] = passenger.isPause;
				_originalGrounded[slot] = passenger.isGround;
				_originalGroundHeight[slot] = passenger.groundHeight;
				_hasOriginalState[slot] = true;
			}
			if (GodotObject.IsInstanceValid(passenger.instance))
			{
				passenger.instance.maskFlags = 0;
			}
			SynchronizePassengerHypnosis(passenger);
			passenger.isPause = true;
			passenger.SetDeferred("isPause", true);
			BobsledTeamComponentDefinition definition = Definition;
			if (definition == null || definition.hideAttachedPassengers)
			{
				passenger.Visible = false;
			}
			passenger.invisible = false;
		}
	}

	public void SynchronizePassengerHypnosis()
	{
		for (int i = 0; i < _slotCount; i++)
		{
			SynchronizePassengerHypnosis(_passengers[i]);
		}
	}

	private void SynchronizePassengerHypnosis(TowerDefenseZombie passenger)
	{
		bool desiredHypnosis;
		double remaining;
		if (TowerDefenseManager.HasGameplayAuthority && GodotObject.IsInstanceValid(_parent) && GodotObject.IsInstanceValid(_parent.instance) && GodotObject.IsInstanceValid(passenger) && !passenger.isDestroy)
		{
			desiredHypnosis = _parent.instance.hypnoses;
			TowerDefenseCharacterBuffHypnoses towerDefenseCharacterBuffHypnoses = _parent.BuffGet("Hypnoses") as TowerDefenseCharacterBuffHypnoses;
			remaining = ((towerDefenseCharacterBuffHypnoses == null || towerDefenseCharacterBuffHypnoses.time < 0.0) ? (-1.0) : Math.Max(0.0, towerDefenseCharacterBuffHypnoses.time - towerDefenseCharacterBuffHypnoses.currentTime));
			if (passenger.IsNodeReady() && GodotObject.IsInstanceValid(passenger.instance))
			{
				ApplyHypnosis();
			}
			else
			{
				Callable.From(ApplyHypnosis).CallDeferred();
			}
		}
		void ApplyHypnosis()
		{
			if (GodotObject.IsInstanceValid(passenger) && !passenger.isDestroy && GodotObject.IsInstanceValid(passenger.instance) && passenger.instance.hypnoses != desiredHypnosis)
			{
				if (!desiredHypnosis)
				{
					passenger.BuffDelete("Hypnoses");
				}
				else
				{
					passenger.BuffAdd(new TowerDefenseCharacterBuffHypnoses
					{
						canFliter = false,
						time = remaining
					});
				}
			}
		}
	}

	public void ReleaseAll()
	{
		ReleaseAll(sendNetworkOperation: true, requireAuthority: true);
	}

	private void ReleaseAll(bool sendNetworkOperation, bool requireAuthority)
	{
		if (!requireAuthority || TowerDefenseManager.HasGameplayAuthority)
		{
			if (sendNetworkOperation && (HasAttachedPassengers || HasPending))
			{
				SendNetworkOperation("release_all", _operationSequence++, new Dictionary());
			}
			for (int i = 0; i < _slotCount; i++)
			{
				DetachPassenger(i, restore: true, destroy: false, markReleased: true, cascade: false);
			}
			RefreshPhysicsRegistration();
		}
	}

	public void CascadeKill()
	{
		CascadeKill(sendNetworkOperation: true, requireAuthority: true);
	}

	private void CascadeKill(bool sendNetworkOperation, bool requireAuthority)
	{
		if (!requireAuthority || TowerDefenseManager.HasGameplayAuthority)
		{
			if (sendNetworkOperation && (HasAttachedPassengers || HasPending))
			{
				SendNetworkOperation("cascade_kill", _operationSequence++, new Dictionary());
			}
			for (int i = 0; i < _slotCount; i++)
			{
				DetachPassenger(i, restore: false, destroy: true, markReleased: true, cascade: true);
			}
			RefreshPhysicsRegistration();
		}
	}

	public void SilentCleanup()
	{
		for (int i = 0; i < _slotCount; i++)
		{
			TowerDefenseZombie towerDefenseZombie = _passengers[i];
			if (GodotObject.IsInstanceValid(towerDefenseZombie))
			{
				towerDefenseZombie.OnDestroy -= OnPassengerDestroyed;
				towerDefenseZombie.bobsledTeamOwner = null;
				towerDefenseZombie.skipDestroySet = true;
				if (!towerDefenseZombie.IsQueuedForDeletion())
				{
					towerDefenseZombie.QueueFree();
				}
			}
			ClearSlot(i);
		}
		RefreshPhysicsRegistration();
	}

	private void DetachPassenger(int slot, bool restore, bool destroy, bool markReleased, bool cascade)
	{
		TowerDefenseZombie towerDefenseZombie = _passengers[slot];
		if (GodotObject.IsInstanceValid(towerDefenseZombie) && towerDefenseZombie.bobsledTeamOwner == this)
		{
			if (restore && !destroy)
			{
				SynchronizePassengerHypnosis(towerDefenseZombie);
			}
			towerDefenseZombie.OnDestroy -= OnPassengerDestroyed;
			towerDefenseZombie.bobsledTeamOwner = null;
			if (destroy)
			{
				DestroyPassengerForCascade(towerDefenseZombie);
			}
			else if (restore)
			{
				if (GodotObject.IsInstanceValid(_parent))
				{
					Vector2 logicalGlobalPosition = towerDefenseZombie.GetLogicalGlobalPosition();
					logicalGlobalPosition.Y = _parent.GetLogicalGlobalPosition().Y;
					towerDefenseZombie.SetLogicalGlobalPosition(logicalGlobalPosition);
				}
				RevealReleasedPassenger(towerDefenseZombie);
				if (_hasOriginalState[slot] && GodotObject.IsInstanceValid(towerDefenseZombie.instance))
				{
					towerDefenseZombie.instance.maskFlags = _originalMaskFlags[slot];
				}
				towerDefenseZombie.Visible = !_hasOriginalState[slot] || _originalVisible[slot];
				towerDefenseZombie.groundHeight = (_hasOriginalState[slot] ? _originalGroundHeight[slot] : (_parent?.groundHeight ?? towerDefenseZombie.groundHeight));
				towerDefenseZombie.isGround = _hasOriginalState[slot] && _originalGrounded[slot];
				towerDefenseZombie.isPause = _hasOriginalState[slot] && _originalPaused[slot];
				towerDefenseZombie.SetDeferred("isPause", towerDefenseZombie.isPause);
				if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
				{
					towerDefenseZombie.gridPos = TowerDefenseManager.Instance.GetMapGridPos(towerDefenseZombie.GetLogicalGlobalPosition());
				}
				towerDefenseZombie.CallDeferred("WalkReady");
			}
		}
		if (markReleased)
		{
			_releasedMask |= 1 << slot;
			if (cascade)
			{
				_cascadeMask |= 1 << slot;
			}
			else
			{
				_cascadeMask &= ~(1 << slot);
			}
		}
		ClearSlot(slot);
	}

	private static void RevealReleasedPassenger(TowerDefenseZombie passenger)
	{
		if (!GodotObject.IsInstanceValid(passenger))
		{
			return;
		}
		passenger.invisible = false;
		passenger.Visible = true;
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(passenger) && !passenger.isDestroy && passenger.bobsledTeamOwner == null)
			{
				passenger.invisible = false;
				passenger.Visible = true;
			}
		}).CallDeferred();
	}

	private static void DestroyPassengerForCascade(TowerDefenseZombie passenger)
	{
		if (!GodotObject.IsInstanceValid(passenger))
		{
			return;
		}
		if (passenger.IsNodeReady())
		{
			DestroyComponent destroyComponent = passenger.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				DestroyReadyPassenger();
				return;
			}
		}
		Callable.From(DestroyReadyPassenger).CallDeferred();
		void DestroyReadyPassenger()
		{
			if (GodotObject.IsInstanceValid(passenger) && !passenger.isDestroy)
			{
				DestroyComponent destroyComponent2 = passenger.destroyComponent;
				if (destroyComponent2 != null && !destroyComponent2.IsReleased)
				{
					passenger.destroyComponent.isRemoteDestroy = true;
				}
				passenger.skipDestroySet = true;
				passenger.Destroy();
			}
		}
	}

	private void ClearSlot(int slot)
	{
		_passengers[slot] = null;
		_pendingSyncIds[slot] = -1;
		_pendingNodeNames[slot] = "";
		_hasOriginalState[slot] = false;
		_occupiedMask &= ~(1 << slot);
	}

	private void OnPassengerDestroyed(TowerDefenseCharacter character)
	{
		for (int i = 0; i < _slotCount; i++)
		{
			if (_passengers[i] == character)
			{
				if (character is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.bobsledTeamOwner == this)
				{
					towerDefenseZombie.bobsledTeamOwner = null;
				}
				_releasedMask |= 1 << i;
				_cascadeMask &= ~(1 << i);
				ClearSlot(i);
				RefreshPhysicsRegistration();
				break;
			}
		}
	}

	private void FollowPassengers(ulong physicsFrame)
	{
		if (!GodotObject.IsInstanceValid(_parent))
		{
			return;
		}
		bool flag = false;
		for (int i = 0; i < _slotCount; i++)
		{
			TowerDefenseZombie towerDefenseZombie = _passengers[i];
			if (!GodotObject.IsInstanceValid(towerDefenseZombie) || towerDefenseZombie.isDestroy || towerDefenseZombie.IsQueuedForDeletion())
			{
				if (_passengers[i] == towerDefenseZombie)
				{
					ClearSlot(i);
					flag = true;
				}
			}
			else
			{
				towerDefenseZombie.SetGlobalPositionForPhysicsFrame(GetSlotPosition(i, physicsFrame), physicsFrame);
				towerDefenseZombie.groundHeight = _parent.groundHeight + (Definition?.passengerHeightOffset ?? 20.0);
				towerDefenseZombie.z = towerDefenseZombie.groundHeight;
			}
		}
		if (flag)
		{
			RefreshPhysicsRegistration();
		}
	}

	private Vector2 GetSlotPosition(int slot, ulong physicsFrame)
	{
		if (slot >= 0 && slot < _markers.Length && GodotObject.IsInstanceValid(_markers[slot]))
		{
			if (_markers[slot] is AdobeAnimateSlot adobeAnimateSlot)
			{
				adobeAnimateSlot.Update();
			}
			return _markers[slot].GlobalPosition;
		}
		Vector2 result = (GodotObject.IsInstanceValid(_parent) ? _parent.GetGlobalPositionForPhysicsFrame(physicsFrame) : Vector2.Zero);
		if (Definition?.passengerOffsets != null && slot < Definition.passengerOffsets.Count)
		{
			result += Definition.passengerOffsets[slot];
		}
		return result;
	}

	private string GetPacketName(int slot)
	{
		if (Definition?.passengerPacketNames == null || Definition.passengerPacketNames.Count == 0)
		{
			return "";
		}
		if (slot >= Definition.passengerPacketNames.Count)
		{
			return Definition.passengerPacketNames[Definition.passengerPacketNames.Count - 1];
		}
		return Definition.passengerPacketNames[slot];
	}

	private void ResolveMarkers()
	{
		if (!GodotObject.IsInstanceValid(_parent) || Definition?.passengerMarkerPaths == null)
		{
			return;
		}
		for (int i = 0; i < _slotCount && i < Definition.passengerMarkerPaths.Count; i++)
		{
			NodePath nodePath = Definition.passengerMarkerPaths[i];
			if (!nodePath.IsEmpty)
			{
				_markers[i] = _parent.GetNodeOrNull<Node2D>(nodePath);
			}
		}
	}

	private void ResolvePendingRelations()
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		for (int i = 0; i < _slotCount; i++)
		{
			if ((_releasedMask & (1 << i)) != 0)
			{
				_pendingSyncIds[i] = -1;
				_pendingNodeNames[i] = "";
				continue;
			}
			int num = _pendingSyncIds[i];
			if (num >= 0 && GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num, out var value))
			{
				SetNetworkPassenger(i, value);
				continue;
			}
			string text = _pendingNodeNames[i];
			if (!string.IsNullOrEmpty(text) && GodotObject.IsInstanceValid(characterNode))
			{
				TowerDefenseCharacter nodeOrNull = characterNode.GetNodeOrNull<TowerDefenseCharacter>(new NodePath(text));
				if (GodotObject.IsInstanceValid(nodeOrNull))
				{
					SetNetworkPassenger(i, nodeOrNull);
				}
			}
		}
		_pendingResolveRemaining = (HasPending ? 0.25 : 0.0);
		RefreshPhysicsRegistration();
	}

	private void RefreshPhysicsRegistration()
	{
		RefreshPhysicsProcessEligibility();
	}

	public override Dictionary ExportComponentSave()
	{
		Array<string> array = new Array<string>();
		Array<Dictionary> array2 = new Array<Dictionary>();
		for (int i = 0; i < _slotCount; i++)
		{
			array.Add(GodotObject.IsInstanceValid(_passengers[i]) ? _passengers[i].Name.ToString() : _pendingNodeNames[i]);
			array2.Add(new Dictionary
			{
				["hasState"] = _hasOriginalState[i],
				["maskFlags"] = _originalMaskFlags[i],
				["visible"] = _originalVisible[i],
				["paused"] = _originalPaused[i],
				["grounded"] = _originalGrounded[i],
				["groundHeight"] = _originalGroundHeight[i]
			});
		}
		return new Dictionary
		{
			["passengerNodeNames"] = array,
			["passengerOriginalStates"] = array2,
			["occupiedMask"] = _occupiedMask,
			["releasedMask"] = _releasedMask,
			["cascadeMask"] = _cascadeMask
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		int num = (1 << _slotCount) - 1;
		_releasedMask = data.GetValueOrDefault("releasedMask", 0).AsInt32() & num;
		_cascadeMask = data.GetValueOrDefault("cascadeMask", 0).AsInt32() & _releasedMask;
		RestoreOriginalStates(data);
		if (!data.ContainsKey("passengerNodeNames"))
		{
			return;
		}
		Godot.Collections.Array array = data["passengerNodeNames"].AsGodotArray();
		for (int i = 0; i < _slotCount; i++)
		{
			if ((_releasedMask & (1 << i)) != 0)
			{
				ReconcileReleasedSlot(i);
				continue;
			}
			SetNetworkPassenger(i, null);
			string text = ((i < array.Count) ? array[i].AsString() : "");
			if (!string.IsNullOrEmpty(text))
			{
				_pendingNodeNames[i] = text;
			}
		}
		RefreshPhysicsRegistration();
		Callable.From(ResolvePendingRelations).CallDeferred();
	}

	private void RestoreOriginalStates(Dictionary data)
	{
		for (int i = 0; i < _slotCount; i++)
		{
			_hasOriginalState[i] = false;
		}
		if (!data.ContainsKey("passengerOriginalStates"))
		{
			return;
		}
		Godot.Collections.Array array = data["passengerOriginalStates"].AsGodotArray();
		for (int j = 0; j < _slotCount && j < array.Count; j++)
		{
			Dictionary dictionary = array[j].AsGodotDictionary();
			if (dictionary.GetValueOrDefault("hasState", false).AsBool())
			{
				_hasOriginalState[j] = true;
				_originalMaskFlags[j] = dictionary.GetValueOrDefault("maskFlags", 0).AsInt32();
				_originalVisible[j] = dictionary.GetValueOrDefault("visible", true).AsBool();
				_originalPaused[j] = dictionary.GetValueOrDefault("paused", false).AsBool();
				_originalGrounded[j] = dictionary.GetValueOrDefault("grounded", false).AsBool();
				_originalGroundHeight[j] = dictionary.GetValueOrDefault("groundHeight", 0.0).AsDouble();
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary reusableSyncPayload = GetReusableSyncPayload();
		reusableSyncPayload.Clear();
		for (int i = 0; i < _slotCount; i++)
		{
			_syncPassengerIds[i] = (GodotObject.IsInstanceValid(_passengers[i]) ? _passengers[i].syncId : _pendingSyncIds[i]);
		}
		reusableSyncPayload["passengerSyncIds"] = _syncPassengerIds;
		reusableSyncPayload["occupiedMask"] = _occupiedMask;
		reusableSyncPayload["releasedMask"] = _releasedMask;
		reusableSyncPayload["cascadeMask"] = _cascadeMask;
		return reusableSyncPayload;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		int num = (1 << _slotCount) - 1;
		_releasedMask = data.GetValueOrDefault("releasedMask", _releasedMask).AsInt32() & num;
		_cascadeMask = data.GetValueOrDefault("cascadeMask", _cascadeMask).AsInt32() & _releasedMask;
		if (!data.ContainsKey("passengerSyncIds"))
		{
			return;
		}
		Godot.Collections.Array array = data["passengerSyncIds"].AsGodotArray();
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		for (int i = 0; i < _slotCount; i++)
		{
			if ((_releasedMask & (1 << i)) != 0)
			{
				ReconcileReleasedSlot(i);
				continue;
			}
			int num2 = ((i < array.Count) ? array[i].AsInt32() : (-1));
			if (num2 < 0)
			{
				SetNetworkPassenger(i, null);
				continue;
			}
			if (GodotObject.IsInstanceValid(currentControl) && currentControl._syncCharacters.TryGetValue(num2, out var value))
			{
				SetNetworkPassenger(i, value);
				continue;
			}
			SetNetworkPassenger(i, null);
			_pendingSyncIds[i] = num2;
		}
		ResolvePendingRelations();
	}

	private void ReconcileReleasedSlot(int slot)
	{
		if ((_cascadeMask & (1 << slot)) != 0)
		{
			DetachPassenger(slot, restore: false, destroy: true, markReleased: false, cascade: true);
		}
		else
		{
			SetNetworkPassenger(slot, null);
		}
		_pendingSyncIds[slot] = -1;
		_pendingNodeNames[slot] = "";
	}

	public override void ApplyNetworkOperation(string operationName, long sequence, Dictionary data)
	{
		if (sequence > _lastAppliedOperation)
		{
			_lastAppliedOperation = sequence;
			if (operationName == "release_all")
			{
				ReleaseAll(sendNetworkOperation: false, requireAuthority: false);
			}
			else if (operationName == "cascade_kill")
			{
				CascadeKill(sendNetworkOperation: false, requireAuthority: false);
			}
		}
	}
}
