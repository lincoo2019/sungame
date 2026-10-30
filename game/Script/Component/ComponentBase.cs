using System;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class ComponentBase : Node2D
{
	private bool _alive = true;

	private StateMachineDefinition _stateMachineDefinition;

	private ComponentManager _stateMachineManager;

	private bool _stateRuntimeAttached;

	private bool _stateMachineInitialEntered;

	private bool _stateMachineDisposed;

	private int _authoritativeStateRestoreDepth;

	private StateMachineSnapshot _pendingStateMachineSnapshot;

	private bool _pendingStateMachineSnapshotIsRemote;

	private bool _pendingStateMachineSnapshotSuppressEffects;

	private long _lastRemoteStateMachineRevision = -1L;

	private ulong _wireManagerInstanceId;

	private string _wireTypeName = string.Empty;

	private int _wireTypeIndex = -1;

	private bool _usingSharedPhysicsBatch;

	private bool _sharedPhysicsBatchWorkEnabled = true;

	private bool _sharedBatchUsesInheritedProcessMode = true;

	private bool _sharedBatchTreeExitConnected;

	[Export(PropertyHint.None, "")]
	public bool alive
	{
		get
		{
			return _alive;
		}
		set
		{
			SetAlive(value);
		}
	}

	internal bool IsSharedBatchDispatchActive
	{
		get
		{
			if (_usingSharedPhysicsBatch && _sharedPhysicsBatchWorkEnabled && _alive)
			{
				return CanDispatchPhysicsWork;
			}
			return false;
		}
	}

	internal bool SharedBatchUsesInheritedProcessMode => _sharedBatchUsesInheritedProcessMode;

	protected virtual bool UseSharedPhysicsBatch => false;

	protected virtual bool AllowPhysicsOutsideComponentBattlefield => false;

	protected virtual bool AllowStateMachineOutsideComponentBattlefield => false;

	private bool IsInsideComponentBattlefield
	{
		get
		{
			TowerDefenseCharacter parentCharacter = GetParentCharacter<TowerDefenseCharacter>();
			if (GodotObject.IsInstanceValid(parentCharacter))
			{
				return parentCharacter.IsInsideComponentBattlefield;
			}
			return true;
		}
	}

	internal bool CanDispatchPhysicsWork
	{
		get
		{
			if (!AllowPhysicsOutsideComponentBattlefield)
			{
				return IsInsideComponentBattlefield;
			}
			return true;
		}
	}

	internal bool CanDispatchStateMachineWork
	{
		get
		{
			if (!AllowStateMachineOutsideComponentBattlefield)
			{
				return IsInsideComponentBattlefield;
			}
			return true;
		}
	}

	[Export(PropertyHint.None, "")]
	public StateMachineDefinition StateMachineDefinition
	{
		get
		{
			return _stateMachineDefinition;
		}
		set
		{
			if (_stateMachineDefinition == value)
			{
				return;
			}
			_stateMachineDefinition = value;
			if (StateMachine != null || _stateRuntimeAttached || GodotObject.IsInstanceValid(_stateMachineManager))
			{
				DisposeStateMachine();
				if (GodotObject.IsInstanceValid(_stateMachineManager))
				{
					AttachStateRuntime();
				}
			}
		}
	}

	public IStateMachineController StateMachine { get; private set; }

	protected virtual bool IsStateMachineDispatchEligible => true;

	protected bool IsStateMachineRegistered
	{
		get
		{
			if (IsInsideTree() && GodotObject.IsInstanceValid(_stateMachineManager))
			{
				return GetParent() == _stateMachineManager.parent;
			}
			return false;
		}
	}

	internal bool HasStateMachine
	{
		get
		{
			if (StateMachine != null)
			{
				return StateMachine.IsInitialized;
			}
			return false;
		}
	}

	internal bool HasConfiguredStateMachine => StateMachineDefinition != null;

	internal bool HasStateMachineProcessWork
	{
		get
		{
			if (alive && IsStateMachineDispatchEligible)
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
			if (alive && IsStateMachineDispatchEligible)
			{
				return StateMachine?.HasPhysicsWork ?? false;
			}
			return false;
		}
	}

	protected bool ShouldApplyLegacyStateField => _authoritativeStateRestoreDepth == 0;

	public override void _EnterTree()
	{
		if (!_sharedBatchTreeExitConnected)
		{
			base.TreeExiting += OnSharedBatchTreeExiting;
			_sharedBatchTreeExitConnected = true;
		}
		if (Global.Instance != null && Global.Instance.isEditor && SceneManager.CurrentScene == "LevelEditorStage")
		{
			base.ProcessMode = ProcessModeEnum.Disabled;
		}
		else
		{
			Callable.From(ConfigureSharedPhysicsBatch).CallDeferred();
		}
	}

	private void OnSharedBatchTreeExiting()
	{
		if (_usingSharedPhysicsBatch)
		{
			ComponentPhysicsBatch.Unregister(this);
			_usingSharedPhysicsBatch = false;
		}
	}

	internal void PrepareSharedBatchRegistration()
	{
		_sharedBatchUsesInheritedProcessMode = base.ProcessMode == ProcessModeEnum.Inherit;
	}

	internal virtual void SharedBatchPhysicsProcess(double delta)
	{
		_PhysicsProcess(delta);
	}

	protected void SetSharedPhysicsBatchWorkEnabled(bool enabled)
	{
		_sharedPhysicsBatchWorkEnabled = enabled;
		if (!IsInsideTree())
		{
			return;
		}
		if (!UseSharedPhysicsBatch || Engine.IsEditorHint())
		{
			SetPhysicsProcess(_alive && enabled);
		}
		else if (!enabled)
		{
			if (_usingSharedPhysicsBatch)
			{
				ComponentPhysicsBatch.Unregister(this);
				_usingSharedPhysicsBatch = false;
			}
			SetPhysicsProcess(enable: false);
		}
		else
		{
			SetPhysicsProcess(_alive);
			ConfigureSharedPhysicsBatch();
		}
	}

	public virtual string _GetName()
	{
		return "";
	}

	public virtual Dictionary ExportComponentSave()
	{
		return new Dictionary();
	}

	public virtual void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
	}

	public virtual Dictionary SyncSerialize()
	{
		return new Dictionary();
	}

	public virtual void SyncDeserialize(Dictionary _data)
	{
	}

	protected bool StartComponentTimer(StringName timerName, double duration, bool useOwnerTimeScale = false)
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return _stateMachineManager.StartComponentTimer(this, timerName, duration, useOwnerTimeScale);
		}
		return false;
	}

	protected bool StopComponentTimer(StringName timerName)
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return _stateMachineManager.StopComponentTimer(this, timerName);
		}
		return false;
	}

	protected bool PauseComponentTimer(StringName timerName)
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return _stateMachineManager.PauseComponentTimer(this, timerName);
		}
		return false;
	}

	protected bool ResumeComponentTimer(StringName timerName)
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return _stateMachineManager.ResumeComponentTimer(this, timerName);
		}
		return false;
	}

	protected bool IsComponentTimerRunning(StringName timerName)
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return _stateMachineManager.IsComponentTimerRunning(this, timerName);
		}
		return false;
	}

	protected double GetComponentTimerRemaining(StringName timerName)
	{
		if (!GodotObject.IsInstanceValid(_stateMachineManager))
		{
			return 0.0;
		}
		return _stateMachineManager.GetComponentTimerRemaining(this, timerName);
	}

	protected virtual void OnComponentTimerTimeout(StringName timerName)
	{
	}

	internal void DispatchComponentTimerTimeout(StringName timerName)
	{
		OnComponentTimerTimeout(timerName);
	}

	internal void ApplyAuthoritativeSync(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		StateMachineSnapshot snapshot = null;
		bool flag = data.ContainsKey("sm") && TryDecodeStateMachineSnapshot(data["sm"].AsGodotDictionary(), out snapshot) && CanRestoreStateMachineSnapshot(snapshot);
		bool flag2 = HasConfiguredStateMachine || flag;
		if (flag2)
		{
			BeginAuthoritativeStateRestore();
		}
		try
		{
			SyncDeserialize(data);
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

	internal static bool TryDecodeStateMachineSnapshot(Dictionary data, out StateMachineSnapshot snapshot)
	{
		snapshot = StateMachineSnapshotCodec.Decode(data);
		return snapshot != null;
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
		if (IsStateMachineRegistered)
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

	public virtual void SetAlive(bool _alive)
	{
		bool num = this._alive != _alive;
		this._alive = _alive;
		SetPhysicsProcess(_alive && _sharedPhysicsBatchWorkEnabled);
		ConfigureSharedPhysicsBatch();
		if (num && GodotObject.IsInstanceValid(_stateMachineManager))
		{
			_stateMachineManager.NotifyComponentAliveChanged(this);
		}
	}

	public bool SendStateEvent(StringName eventName, bool allowWhenInactive = false)
	{
		if (!IsStateMachineRegistered || _authoritativeStateRestoreDepth > 0 || !CanDispatchStateMachineWork || (!allowWhenInactive && !alive))
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

	protected void RefreshStateMachineDispatchEligibility()
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager))
		{
			_stateMachineManager.RefreshStateMachineRegistration(this);
		}
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

	internal void AttachToManager(ComponentManager manager)
	{
		if (!GodotObject.IsInstanceValid(manager) || !IsInsideTree() || GetParent() != manager.parent)
		{
			return;
		}
		if (_stateMachineManager == manager && _stateRuntimeAttached)
		{
			manager.RefreshStateMachineRegistration(this);
			return;
		}
		if (GodotObject.IsInstanceValid(_stateMachineManager) && _stateMachineManager != manager)
		{
			_stateMachineManager.RefreshStateMachineRegistration(this);
		}
		_stateMachineManager = manager;
		AttachStateRuntime();
	}

	internal void DetachFromManager(ComponentManager manager)
	{
		if (_stateMachineManager == manager)
		{
			_stateMachineManager = null;
		}
	}

	private void AttachStateRuntime()
	{
		ComponentManager stateMachineManager = _stateMachineManager;
		if (!GodotObject.IsInstanceValid(stateMachineManager))
		{
			return;
		}
		if (!EnsureStateMachineInitialized())
		{
			stateMachineManager.RefreshStateMachineRegistration(this);
			return;
		}
		if (!_stateRuntimeAttached)
		{
			OnStateRuntimeAttached();
			_stateRuntimeAttached = true;
		}
		stateMachineManager.RefreshStateMachineRegistration(this);
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
		if (StateMachineDefinition == null)
		{
			return false;
		}
		_stateMachineDisposed = false;
		StateMachine = new StateMachineController();
		if (StateMachine.Initialize(StateMachineDefinition, this))
		{
			return true;
		}
		GD.PushError($"Component '{base.Name}' state machine initialization failed: {StateMachine.InitializationError}");
		StateMachine.Dispose();
		StateMachine = null;
		return false;
	}

	private void DisposeStateMachine()
	{
		if (!_stateMachineDisposed || StateMachine != null)
		{
			_stateMachineDisposed = true;
			if (_stateRuntimeAttached)
			{
				OnStateRuntimeDetaching();
				_stateRuntimeAttached = false;
			}
			_stateMachineInitialEntered = false;
			StateMachine?.Dispose();
			StateMachine = null;
			if (GodotObject.IsInstanceValid(_stateMachineManager))
			{
				_stateMachineManager.RefreshStateMachineRegistration(this);
			}
		}
	}

	internal bool TryGetWireSlotToken(ulong managerInstanceId, string typeName, out int typeIndex)
	{
		typeIndex = _wireTypeIndex;
		if (_wireManagerInstanceId == managerInstanceId && _wireTypeIndex >= 0)
		{
			return string.Equals(_wireTypeName, typeName, StringComparison.Ordinal);
		}
		return false;
	}

	internal void SetWireSlotToken(ulong managerInstanceId, string typeName, int typeIndex)
	{
		_wireManagerInstanceId = managerInstanceId;
		_wireTypeName = typeName ?? string.Empty;
		_wireTypeIndex = typeIndex;
	}

	public override void _Notification(int what)
	{
		if ((long)what == 1)
		{
			DisposeStateMachine();
		}
	}

	private void ConfigureSharedPhysicsBatch()
	{
		if (!UseSharedPhysicsBatch || Engine.IsEditorHint() || !IsInsideTree())
		{
			return;
		}
		if (_alive && _sharedPhysicsBatchWorkEnabled)
		{
			_usingSharedPhysicsBatch = ComponentPhysicsBatch.Register(this);
			if (_usingSharedPhysicsBatch)
			{
				SetPhysicsProcess(enable: false);
			}
			return;
		}
		if (_usingSharedPhysicsBatch)
		{
			ComponentPhysicsBatch.Unregister(this);
			_usingSharedPhysicsBatch = false;
		}
		SetPhysicsProcess(enable: false);
	}

	protected T GetParentCharacter<T>() where T : class
	{
		if (GodotObject.IsInstanceValid(_stateMachineManager?.parent))
		{
			return _stateMachineManager.parent as T;
		}
		Node parent = GetParent();
		if (parent is T result)
		{
			return result;
		}
		return parent?.GetParent() as T;
	}

	protected static bool SyncForceState(IStateMachineController state, string targetState)
	{
		if (state == null || !state.IsInitialized || string.IsNullOrEmpty(targetState))
		{
			return false;
		}
		StateHandle currentStateHandle = state.CurrentStateHandle;
		StateHandle stateHandle = state.ResolveState(targetState, allowDisplayNameFallback: true);
		if (currentStateHandle == null || !currentStateHandle.IsValid || stateHandle == null || !stateHandle.IsValid)
		{
			return false;
		}
		if (currentStateHandle.StableId == stateHandle.StableId)
		{
			return true;
		}
		StateMachineSnapshot stateMachineSnapshot = state.CaptureSnapshot();
		if (stateMachineSnapshot == null)
		{
			return false;
		}
		stateMachineSnapshot.ActiveStateIds.Clear();
		stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
		stateMachineSnapshot.PendingTransitionIds.Clear();
		stateMachineSnapshot.PendingDelayRemaining.Clear();
		return state.RestoreSnapshot(stateMachineSnapshot);
	}
}
