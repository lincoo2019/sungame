using System;
using Godot;
using Godot.Collections;

public abstract class CharacterComponentRuntime
{
	private Dictionary _pendingAuthoritativeSync;

	private Dictionary _defaultSavePayload;

	private Dictionary _defaultSyncPayload;

	private StateMachineSnapshot _pendingStateMachineSnapshot;

	private bool _pendingStateMachineSnapshotIsRemote;

	private bool _pendingStateMachineSnapshotSuppressEffects;

	private long _lastRemoteStateMachineRevision = -1L;

	private bool _stateRuntimeAttached;

	private bool _stateRuntimeRegistered;

	private bool _stateMachineInitialEntered;

	private int _authoritativeStateRestoreDepth;

	private IDisposable _modLease;

	public CharacterComponentDefinition ComponentDefinition { get; private set; }

	public ComponentManager Manager { get; private set; }

	public TowerDefenseCharacter Owner { get; private set; }

	public ComponentRuntimeLifecycle Lifecycle { get; private set; }

	public bool Alive { get; private set; } = true;

	public bool IsAttached
	{
		get
		{
			ComponentRuntimeLifecycle lifecycle = Lifecycle;
			if ((uint)(lifecycle - 1) <= 1u)
			{
				return true;
			}
			return false;
		}
	}

	public bool IsReleased => Lifecycle == ComponentRuntimeLifecycle.Released;

	public IStateMachineController StateMachine { get; private set; }

	protected virtual bool IsStateMachineDispatchEligible => true;

	protected virtual bool AllowPhysicsOutsideComponentBattlefield => false;

	protected virtual bool AllowStateMachineOutsideComponentBattlefield => false;

	protected virtual bool AllowInputOutsideComponentBattlefield => false;

	internal virtual bool WantsPhysicsProcess => false;

	internal virtual bool WantsInput => false;

	internal virtual bool WantsUnhandledInput => false;

	internal virtual bool HasOwnerGameplayActivationWork => false;

	protected bool IsInsideComponentBattlefield
	{
		get
		{
			if (Owner != null)
			{
				return Owner.IsInsideComponentBattlefieldForRuntime;
			}
			return true;
		}
	}

	protected bool CanExecuteGameplay => IsInsideComponentBattlefield;

	internal bool CanDispatchPhysicsWork => CanDispatchPhysicsWorkForOwnerState(IsInsideComponentBattlefield);

	internal bool CanDispatchStateMachineWork => CanDispatchStateMachineWorkForOwnerState(IsInsideComponentBattlefield);

	internal bool CanDispatchInputWork
	{
		get
		{
			if (!AllowInputOutsideComponentBattlefield)
			{
				return IsInsideComponentBattlefield;
			}
			return true;
		}
	}

	protected bool IsStateMachineRegistered => _stateRuntimeRegistered;

	internal bool HasStateMachine => StateMachine?.IsInitialized ?? false;

	internal bool HasStateMachineProcessWork
	{
		get
		{
			if (Alive && Lifecycle == ComponentRuntimeLifecycle.Active && IsStateMachineDispatchEligible)
			{
				return StateMachine?.HasProcessWork ?? false;
			}
			return false;
		}
	}

	internal bool HasStateMachinePhysicsWork
	{
		get
		{
			if (Alive && Lifecycle == ComponentRuntimeLifecycle.Active && IsStateMachineDispatchEligible)
			{
				return StateMachine?.HasPhysicsWork ?? false;
			}
			return false;
		}
	}

	internal bool HasRuntimePhysicsWork
	{
		get
		{
			if (Alive && Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return WantsPhysicsProcess;
			}
			return false;
		}
	}

	internal bool HasRuntimeInputWork
	{
		get
		{
			if (Alive && Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return WantsInput;
			}
			return false;
		}
	}

	internal bool HasRuntimeUnhandledInputWork
	{
		get
		{
			if (Alive && Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return WantsUnhandledInput;
			}
			return false;
		}
	}

	protected bool ShouldApplyLegacyStateField => _authoritativeStateRestoreDepth == 0;

	internal void AdoptModLease(IDisposable lease)
	{
		_modLease = lease;
	}

	protected virtual bool CanSendStateEventOutsideComponentBattlefield(StringName eventName)
	{
		return AllowStateMachineOutsideComponentBattlefield;
	}

	internal virtual bool TryTickFlatStateMachinePhysics(double delta)
	{
		return false;
	}

	internal bool CanDispatchPhysicsWorkForOwnerState(bool ownerInsideComponentBattlefield)
	{
		return AllowPhysicsOutsideComponentBattlefield || ownerInsideComponentBattlefield;
	}

	internal bool CanDispatchStateMachineWorkForOwnerState(bool ownerInsideComponentBattlefield)
	{
		return AllowStateMachineOutsideComponentBattlefield || ownerInsideComponentBattlefield;
	}

	protected bool TryGetConfiguration(StringName name, out Variant value)
	{
		if (ComponentDefinition is ModCharacterComponentDefinition modCharacterComponentDefinition)
		{
			return modCharacterComponentDefinition.TryGetConfiguration(name, out value);
		}
		value = default(Variant);
		return false;
	}

	protected T GetConfiguration<[MustBeVariant] T>(StringName name, T fallback = default(T))
	{
		if (!TryGetConfiguration(name, out var value))
		{
			return fallback;
		}
		try
		{
			return value.As<T>();
		}
		catch
		{
			return fallback;
		}
	}

	internal void Bind(ComponentManager manager, TowerDefenseCharacter owner, CharacterComponentDefinition definition)
	{
		if (manager == null || owner == null || definition == null || IsReleased || (Lifecycle != ComponentRuntimeLifecycle.Created && Lifecycle != ComponentRuntimeLifecycle.Detached))
		{
			return;
		}
		if (_modLease == null)
		{
			_modLease = CharacterComponentRuntimeTypeRegistry.Acquire(GetType());
		}
		if (ComponentDefinition == null)
		{
			CharacterComponentDefinition characterComponentDefinition = (ComponentDefinition = definition);
		}
		if (ComponentDefinition == definition)
		{
			Manager = manager;
			Owner = owner;
			if (Lifecycle == ComponentRuntimeLifecycle.Created)
			{
				Alive = definition.InitiallyAlive;
			}
			Lifecycle = ComponentRuntimeLifecycle.Bound;
			OnBound();
		}
	}

	internal void Activate()
	{
		if (Lifecycle == ComponentRuntimeLifecycle.Bound)
		{
			Lifecycle = ComponentRuntimeLifecycle.Active;
			if (EnsureStateMachineInitialized() && !_stateRuntimeAttached)
			{
				OnStateRuntimeAttached();
				_stateRuntimeAttached = true;
			}
			OnActivated();
			if (_pendingAuthoritativeSync != null)
			{
				Dictionary pendingAuthoritativeSync = _pendingAuthoritativeSync;
				_pendingAuthoritativeSync = null;
				ApplyAuthoritativeSyncCore(pendingAuthoritativeSync);
			}
		}
	}

	internal void OwnerGameplayActivated()
	{
		if (Lifecycle == ComponentRuntimeLifecycle.Active && Alive)
		{
			OnOwnerGameplayActivated();
		}
	}

	protected virtual void OnOwnerGameplayActivated()
	{
	}

	internal void OwnerBeforeDestroy()
	{
		if (Lifecycle == ComponentRuntimeLifecycle.Active && Alive)
		{
			OnOwnerBeforeDestroy();
		}
	}

	protected virtual void OnOwnerBeforeDestroy()
	{
	}

	internal void RegisterStateRuntime()
	{
		if (Lifecycle == ComponentRuntimeLifecycle.Active && !_stateRuntimeRegistered)
		{
			AttachStateRuntime();
		}
	}

	internal void Detach(ComponentDetachReason reason)
	{
		if (!IsAttached)
		{
			return;
		}
		_stateRuntimeRegistered = false;
		try
		{
			OnDetaching(reason);
		}
		finally
		{
			Manager = null;
			Owner = null;
			Lifecycle = ComponentRuntimeLifecycle.Detached;
		}
	}

	internal void Release(ComponentDetachReason reason = ComponentDetachReason.OwnerReleased)
	{
		if (IsReleased)
		{
			return;
		}
		if (_stateRuntimeAttached)
		{
			Cleanup(OnStateRuntimeDetaching);
		}
		_stateRuntimeAttached = false;
		if (IsAttached)
		{
			Cleanup(delegate
			{
				Detach(reason);
			});
		}
		Cleanup(OnReleased);
		Cleanup(OnRuntimeReleased);
		Cleanup(delegate
		{
			StateMachine?.Dispose();
		});
		StateMachine = null;
		Manager = null;
		Owner = null;
		ComponentDefinition = null;
		_pendingAuthoritativeSync = null;
		Cleanup(delegate
		{
			_defaultSavePayload?.Clear();
		});
		Cleanup(delegate
		{
			_defaultSyncPayload?.Clear();
		});
		_pendingStateMachineSnapshot = null;
		Lifecycle = ComponentRuntimeLifecycle.Released;
		_modLease?.Dispose();
		_modLease = null;
		void Cleanup(Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				GD.PushWarning("[ComponentRuntime] release " + GetType().FullName + ": " + ex.Message);
			}
		}
	}

	public virtual void SetAlive(bool alive)
	{
		if (!IsReleased && Alive != alive)
		{
			Alive = alive;
			OnAliveChanged(alive);
			Manager?.NotifyRuntimeAliveChanged(this);
		}
	}

	protected void RefreshStateMachineDispatchEligibility()
	{
		Manager?.RefreshRuntimeStateMachineRegistration(this);
	}

	protected void RefreshStateMachineWorkEligibility()
	{
		Manager?.RefreshRuntimeStateMachineWorkRegistration(this);
	}

	protected void RefreshPhysicsProcessEligibility()
	{
		Manager?.RefreshRuntimePhysicsRegistration(this);
	}

	internal virtual void PhysicsProcess(double delta, ulong physicsFrame)
	{
	}

	internal virtual void ProcessInput(InputEvent inputEvent)
	{
	}

	internal virtual void ProcessUnhandledInput(InputEvent inputEvent)
	{
	}

	public bool SendStateEvent(StringName eventName, bool allowWhenInactive = false)
	{
		if (!CanSendStateEvent(eventName, allowWhenInactive))
		{
			return false;
		}
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			return StateMachine.SendEvent(eventName);
		}
		return false;
	}

	protected bool SetFlatState(StateHandle targetState, StringName sourceEvent, bool allowWhenInactive = false)
	{
		if (CanSendStateEvent(sourceEvent, allowWhenInactive))
		{
			IStateMachineController stateMachine = StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				if (!StateMachine.TrySetFlatState(targetState, sourceEvent))
				{
					return StateMachine.SendEvent(sourceEvent);
				}
				return true;
			}
		}
		return false;
	}

	private bool CanSendStateEvent(StringName eventName, bool allowWhenInactive)
	{
		if (_stateRuntimeRegistered && _authoritativeStateRestoreDepth == 0 && (IsInsideComponentBattlefield || CanSendStateEventOutsideComponentBattlefield(eventName)))
		{
			if (!allowWhenInactive)
			{
				return Alive;
			}
			return true;
		}
		return false;
	}

	public virtual Dictionary ExportComponentSave()
	{
		return _defaultSavePayload ?? (_defaultSavePayload = new Dictionary());
	}

	public virtual bool CanImportComponentSave(string definitionId, int schemaVersion)
	{
		CharacterComponentDefinition componentDefinition = ComponentDefinition;
		if (componentDefinition == null)
		{
			return false;
		}
		if (!string.IsNullOrEmpty(definitionId) && !string.Equals(componentDefinition.DefinitionId, definitionId, StringComparison.Ordinal))
		{
			return false;
		}
		if (schemaVersion > 0)
		{
			return schemaVersion == componentDefinition.SchemaVersion;
		}
		return true;
	}

	public virtual void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
	}

	public virtual Dictionary SyncSerialize()
	{
		return _defaultSyncPayload ?? (_defaultSyncPayload = new Dictionary());
	}

	protected Dictionary GetReusableSyncPayload()
	{
		return _defaultSyncPayload ?? (_defaultSyncPayload = new Dictionary());
	}

	protected void ClearReusableSyncPayload()
	{
		_defaultSyncPayload?.Clear();
	}

	public virtual void SyncDeserialize(Dictionary data)
	{
	}

	public virtual void ApplyNetworkOperation(string operationName, long sequence, Dictionary data)
	{
	}

	protected bool SendNetworkOperation(string operationName, long sequence, Dictionary data)
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || Owner == null || Owner.syncId < 0 || ComponentDefinition == null || string.IsNullOrWhiteSpace(ComponentDefinition.InstanceId) || string.IsNullOrWhiteSpace(ComponentDefinition.ComponentTypeId) || string.IsNullOrWhiteSpace(operationName) || sequence < 0)
		{
			return false;
		}
		MultiPlayerManager instance = MultiPlayerManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		instance.SendCharacterComponentOperation(Owner.syncId, ComponentDefinition.InstanceId, ComponentDefinition.ComponentTypeId, sequence, operationName, data ?? new Dictionary());
		return true;
	}

	internal void ApplyAuthoritativeSync(Dictionary data)
	{
		if (IsReleased || data == null)
		{
			return;
		}
		if (Lifecycle != ComponentRuntimeLifecycle.Active)
		{
			Dictionary dictionary = data.Duplicate(deep: true);
			if (!dictionary.ContainsKey("sm") && _pendingAuthoritativeSync != null && _pendingAuthoritativeSync.ContainsKey("sm"))
			{
				dictionary["sm"] = _pendingAuthoritativeSync["sm"];
			}
			_pendingAuthoritativeSync = dictionary;
		}
		else
		{
			ApplyAuthoritativeSyncCore(data);
		}
	}

	private void ApplyAuthoritativeSyncCore(Dictionary data)
	{
		StateMachineSnapshot snapshot = null;
		bool flag = data.ContainsKey("sm") && TryDecodeStateMachineSnapshot(data["sm"].AsGodotDictionary(), out snapshot) && CanRestoreStateMachineSnapshot(snapshot);
		bool flag2 = ComponentDefinition?.StateMachineDefinition != null || flag;
		if (flag2)
		{
			BeginAuthoritativeStateRestore();
		}
		try
		{
			SyncDeserialize(data);
			if (data.ContainsKey("_alive"))
			{
				SetAlive(data["_alive"].AsBool());
			}
		}
		finally
		{
			if (flag2)
			{
				EndAuthoritativeStateRestore();
			}
		}
		if (flag)
		{
			RestoreStateMachineSnapshot(snapshot, remote: true, suppressEntryEffects: true);
		}
	}

	internal Dictionary CaptureStateMachineSnapshotData()
	{
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return new Dictionary();
		}
		return StateMachineSnapshotCodec.Encode(StateMachine.CaptureSnapshot());
	}

	internal bool RestoreStateMachineSnapshot(StateMachineSnapshot snapshot, bool remote, bool suppressEntryEffects, bool progressCompatible = false)
	{
		if (snapshot == null || !CanRestoreStateMachineSnapshot(snapshot, progressCompatible))
		{
			return false;
		}
		if (remote && snapshot.Revision < _lastRemoteStateMachineRevision)
		{
			return true;
		}
		if (_stateRuntimeRegistered)
		{
			IStateMachineController stateMachine = StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				return ApplyStateMachineSnapshot(snapshot, remote, suppressEntryEffects);
			}
		}
		if (_pendingStateMachineSnapshot == null || !remote || !_pendingStateMachineSnapshotIsRemote || snapshot.Revision >= _pendingStateMachineSnapshot.Revision)
		{
			_pendingStateMachineSnapshot = snapshot;
			_pendingStateMachineSnapshotIsRemote = remote;
			_pendingStateMachineSnapshotSuppressEffects = suppressEntryEffects;
		}
		return true;
	}

	internal bool CanRestoreStateMachineSnapshot(StateMachineSnapshot snapshot, bool progressCompatible = false)
	{
		if (snapshot == null || !EnsureStateMachineInitialized())
		{
			return false;
		}
		if (progressCompatible)
		{
			StateMachineSnapshot stateMachineSnapshot = StateMachine.CaptureSnapshot();
			if (stateMachineSnapshot == null || !string.Equals(snapshot.DefinitionId, stateMachineSnapshot.DefinitionId, StringComparison.Ordinal) || snapshot.SchemaVersion != stateMachineSnapshot.SchemaVersion)
			{
				return false;
			}
			snapshot.ContentHash = stateMachineSnapshot.ContentHash;
		}
		return StateMachine.CanRestoreSnapshot(snapshot);
	}

	internal void BeginAuthoritativeStateRestore()
	{
		_authoritativeStateRestoreDepth++;
	}

	internal void EndAuthoritativeStateRestore()
	{
		if (_authoritativeStateRestoreDepth > 0)
		{
			_authoritativeStateRestoreDepth--;
		}
	}

	private void AttachStateRuntime()
	{
		if (!EnsureStateMachineInitialized())
		{
			return;
		}
		_stateRuntimeRegistered = true;
		if (!_stateMachineInitialEntered)
		{
			_stateMachineInitialEntered = true;
			if (_pendingStateMachineSnapshot != null)
			{
				ApplyPendingStateMachineSnapshot();
			}
			else
			{
				StateMachine.EnterInitialState();
			}
		}
		else
		{
			ApplyPendingStateMachineSnapshot();
		}
		OnStateRuntimeRegistered();
	}

	private void ApplyPendingStateMachineSnapshot()
	{
		if (_pendingStateMachineSnapshot != null)
		{
			IStateMachineController stateMachine = StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				StateMachineSnapshot pendingStateMachineSnapshot = _pendingStateMachineSnapshot;
				bool pendingStateMachineSnapshotIsRemote = _pendingStateMachineSnapshotIsRemote;
				bool pendingStateMachineSnapshotSuppressEffects = _pendingStateMachineSnapshotSuppressEffects;
				_pendingStateMachineSnapshot = null;
				_pendingStateMachineSnapshotIsRemote = false;
				_pendingStateMachineSnapshotSuppressEffects = false;
				ApplyStateMachineSnapshot(pendingStateMachineSnapshot, pendingStateMachineSnapshotIsRemote, pendingStateMachineSnapshotSuppressEffects);
			}
		}
	}

	private bool ApplyStateMachineSnapshot(StateMachineSnapshot snapshot, bool remote, bool suppressEntryEffects)
	{
		if (remote && snapshot.Revision < _lastRemoteStateMachineRevision)
		{
			return true;
		}
		OnAuthoritativeStateRestorePreparing(remote);
		bool num = StateMachine.RestoreSnapshot(snapshot, suppressEntryEffects);
		if (num && remote)
		{
			_lastRemoteStateMachineRevision = Math.Max(_lastRemoteStateMachineRevision, snapshot.Revision);
		}
		if (num)
		{
			OnAuthoritativeStateRestored(snapshot, remote);
		}
		return num;
	}

	private bool EnsureStateMachineInitialized()
	{
		IStateMachineController stateMachine = StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			return true;
		}
		if (ComponentDefinition?.StateMachineDefinition == null)
		{
			return false;
		}
		StateMachine = new StateMachineController();
		CharacterComponentRuntimeTypeRegistry.TryGetOwner(GetType(), out var owner);
		if (StateMachine.Initialize(ComponentDefinition.StateMachineDefinition, this, owner))
		{
			return true;
		}
		GD.PushError("Resource component '" + ComponentDefinition.InstanceId + "' state machine initialization failed: " + StateMachine.InitializationError);
		StateMachine.Dispose();
		StateMachine = null;
		return false;
	}

	private static bool TryDecodeStateMachineSnapshot(Dictionary data, out StateMachineSnapshot snapshot)
	{
		snapshot = StateMachineSnapshotCodec.Decode(data);
		return snapshot != null;
	}

	protected virtual void OnBound()
	{
	}

	protected virtual void OnActivated()
	{
	}

	protected virtual void OnDetaching(ComponentDetachReason reason)
	{
	}

	protected virtual void OnReleased()
	{
	}

	protected virtual void OnRuntimeReleased()
	{
	}

	protected virtual void OnAliveChanged(bool alive)
	{
	}

	protected virtual void OnStateRuntimeAttached()
	{
	}

	protected virtual void OnStateRuntimeRegistered()
	{
	}

	protected virtual void OnStateRuntimeDetaching()
	{
	}

	protected virtual void OnAuthoritativeStateRestorePreparing(bool remote)
	{
	}

	protected virtual void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
	}
}
