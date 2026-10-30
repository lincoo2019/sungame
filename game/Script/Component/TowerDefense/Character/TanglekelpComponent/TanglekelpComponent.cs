using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public sealed class TanglekelpComponent : CharacterComponentRuntime
{
	public delegate void DragBeginEventHandler(TowerDefenseCharacter target);

	public delegate void DragEventHandler(TowerDefenseCharacter target, bool success);

	public AttackComponent attackComponent;

	public int grabNum = 1;

	public bool destroyUse = true;

	public bool targetDestroyUse = true;

	public float dragDelay = 0.5f;

	public string dragAudio = "Floop";

	public string entrySplashAudio = "PlantWater";

	public string submergeAudio = "ZombieEnteringWater";

	public bool createTargetSplash = true;

	public bool createParentSplash = true;

	public StringName dragStateEvent = "ToDrag";

	public StringName idleStateEvent = "ToIdle";

	public PackedScene grabSpriteScene;

	public string grabAnimeClips = "Grab";

	public float grabAnimeTimeScale = 1f;

	public string[] grabFliterOpen = System.Array.Empty<string>();

	public string[] grabFliterClose = System.Array.Empty<string>();

	public TowerDefenseCharacter parent;

	public TowerDefenseCharacter target;

	public int currentGrabNum;

	private readonly Godot.Collections.Array _openFilters = new Godot.Collections.Array();

	private readonly Godot.Collections.Array _closeFilters = new Godot.Collections.Array();

	private string[] _cachedOpenSource;

	private string[] _cachedCloseSource;

	private StringName _attackComponentName = "AttackComponent";

	private AdobeAnimateSprite _grabSprite;

	private TowerDefenseCharacter _lockedTarget;

	private bool _savedTargetSpritePause;

	private bool _targetInteractionLocked;

	private bool _savedParentInvincible;

	private TowerDefenseEnum.LAYER_GROUNDITEM _savedParentItemLayer;

	private bool _parentStateSaved;

	private bool _stateSignalsConnected;

	private bool _dragRunning;

	private bool _configured;

	private ulong _dragVersion;

	private StateHandle _idleState;

	private StateHandle _dragState;

	private string _pendingSyncedState;

	private TanglekelpComponentDefinition Definition => base.ComponentDefinition as TanglekelpComponentDefinition;

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

	public event DragBeginEventHandler OnDragBegin;

	public event DragEventHandler OnDrag;

	protected override void OnBound()
	{
		ConfigureOnce();
		parent = base.Owner;
		ResolveDependencies();
	}

	protected override void OnActivated()
	{
		ResolveDependencies();
		ApplyPendingSyncedState();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		CancelActiveDragRuntime();
		parent = null;
		attackComponent = null;
	}

	protected override void OnRuntimeReleased()
	{
		CancelActiveDragRuntime();
		DisconnectStateSignals();
		this.OnDragBegin = null;
		this.OnDrag = null;
		parent = null;
		target = null;
		attackComponent = null;
		grabSpriteScene = null;
		grabFliterOpen = System.Array.Empty<string>();
		grabFliterClose = System.Array.Empty<string>();
		_cachedOpenSource = null;
		_cachedCloseSource = null;
		_openFilters.Clear();
		_closeFilters.Clear();
		_idleState = null;
		_dragState = null;
		_pendingSyncedState = null;
		_configured = false;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelActiveDragRuntime();
		}
		else if (base.Lifecycle == ComponentRuntimeLifecycle.Active)
		{
			ApplyPendingSyncedState();
		}
	}

	private void ConfigureOnce()
	{
		if (!_configured)
		{
			TanglekelpComponentDefinition definition = Definition;
			_attackComponentName = definition?.attackComponentName ?? new StringName("AttackComponent");
			grabNum = Math.Max(1, definition?.grabNum ?? 1);
			destroyUse = definition?.destroyUse ?? true;
			targetDestroyUse = definition?.targetDestroyUse ?? true;
			dragDelay = Mathf.Max(0f, definition?.dragDelay ?? 0.5f);
			dragAudio = definition?.dragAudio ?? "Floop";
			entrySplashAudio = definition?.entrySplashAudio ?? "PlantWater";
			submergeAudio = definition?.submergeAudio ?? "ZombieEnteringWater";
			createTargetSplash = definition?.createTargetSplash ?? true;
			createParentSplash = definition?.createParentSplash ?? true;
			dragStateEvent = definition?.dragStateEvent ?? new StringName("ToDrag");
			idleStateEvent = definition?.idleStateEvent ?? new StringName("ToIdle");
			grabSpriteScene = definition?.grabSpriteScene;
			grabAnimeClips = definition?.grabAnimeClips ?? "Grab";
			grabAnimeTimeScale = Mathf.Max(0.01f, definition?.grabAnimeTimeScale ?? 1f);
			grabFliterOpen = CopyFilters(definition?.grabFliterOpen);
			grabFliterClose = CopyFilters(definition?.grabFliterClose);
			_configured = true;
		}
	}

	private static string[] CopyFilters(Array<string> source)
	{
		if (source == null || source.Count == 0)
		{
			return System.Array.Empty<string>();
		}
		string[] array = new string[source.Count];
		for (int i = 0; i < source.Count; i++)
		{
			array[i] = source[i];
		}
		return array;
	}

	private void ResolveDependencies()
	{
		if (base.Manager != null)
		{
			string text = _attackComponentName.ToString();
			attackComponent = ((!string.IsNullOrEmpty(text) && base.Manager.TryGetRuntimeByLegacyNodeName(text, out var runtime)) ? (runtime as AttackComponent) : null);
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("tanglekelp.idle");
		_dragState = base.StateMachine?.GetStateById("tanglekelp.drag");
		ConnectStateSignals();
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
		if (!_dragRunning && base.StateMachine?.CurrentStateHandle?.StableId == "tanglekelp.drag")
		{
			SendStateEvent(idleStateEvent);
		}
	}

	protected override void OnStateRuntimeDetaching()
	{
		CancelActiveDragRuntime();
		DisconnectStateSignals();
		_idleState = null;
		_dragState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		TowerDefenseCharacter towerDefenseCharacter = target;
		CancelActiveDragRuntime();
		target = towerDefenseCharacter;
		_pendingSyncedState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			if (stateHandle.StableId == "tanglekelp.drag")
			{
				DowngradeUnsafeAuthoritativeDrag();
			}
			else
			{
				target = null;
			}
		}
	}

	private void DowngradeUnsafeAuthoritativeDrag()
	{
		CancelActiveDragRuntime();
		StateHandle stateHandle = base.StateMachine?.GetStateById("tanglekelp.idle");
		StateMachineSnapshot stateMachineSnapshot = base.StateMachine?.CaptureSnapshot();
		if (stateHandle != null && stateHandle.IsValid && stateMachineSnapshot != null)
		{
			stateMachineSnapshot.ActiveStateIds.Clear();
			stateMachineSnapshot.ActiveStateIds.Add(stateHandle.StableId);
			stateMachineSnapshot.PendingTransitionIds.Clear();
			stateMachineSnapshot.PendingDelayRemaining.Clear();
			base.StateMachine.RestoreSnapshot(stateMachineSnapshot, suppressEntryEffects: true);
		}
	}

	private void CancelActiveDragRuntime()
	{
		_dragVersion++;
		CleanupGrabSprite();
		RestoreTargetInteraction();
		RestoreParentState();
		_dragRunning = false;
		target = null;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_dragState, DragEntered, DragExited, DragProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_dragState, DragEntered, DragExited, DragProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null && state.IsValid)
		{
			state.Entered += entered;
			state.Exited += exited;
			state.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle state, Action entered, Action exited, Action<double> processing)
	{
		if (state != null)
		{
			state.Entered -= entered;
			state.Exited -= exited;
			state.PhysicsProcessing -= processing;
		}
	}

	public void IdleEntered()
	{
		if (TryGetParent(out var owner) && owner.componentRunning)
		{
			owner.Idle();
		}
	}

	public void IdleProcessing(double delta)
	{
		if (!IsRemoteClient && !_dragRunning && CanStartDrag() && attackComponent.CanAttackOnce())
		{
			target = attackComponent.target;
			SendStateEvent(dragStateEvent);
		}
	}

	public void IdleExited()
	{
	}

	public void DragEntered()
	{
		Drag(target);
	}

	public void DragProcessing(double delta)
	{
	}

	public void DragExited()
	{
	}

	public void Drag(TowerDefenseCharacter character)
	{
		bool flag = character is TowerDefenseZombie && GodotObject.IsInstanceValid(character.instance) && character.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS;
		if (!(IsRemoteClient || _dragRunning || !TryGetParent(out var _) || !GodotObject.IsInstanceValid(character) || character.IsHardControlImmune || flag))
		{
			_dragRunning = true;
			target = character;
			DragAsync(character, ++_dragVersion);
		}
	}

	private async Task DragAsync(TowerDefenseCharacter character, ulong version)
	{
		bool parentWillDestroy = false;
		try
		{
			this.OnDragBegin?.Invoke(character);
			if (!IsCurrent(version, out var owner))
			{
				return;
			}
			bool dragSuccess = CanDragTarget(character);
			SaveParentState(owner);
			owner.instance.invincible = true;
			owner.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.EFFECT;
			PlayAudio(dragAudio);
			PlayAudio(entrySplashAudio);
			if (!CreateGrabSprite(owner, character))
			{
				dragSuccess = false;
			}
			if (dragSuccess)
			{
				LockTargetInteraction(character);
			}
			if (!(await WaitDragDelayAsync(version)) || !IsCurrent(version, out owner))
			{
				return;
			}
			if (createParentSplash)
			{
				owner.CreateSplash();
			}
			PlayAudio(submergeAudio);
			CleanupGrabSprite();
			this.OnDrag?.Invoke(character, dragSuccess);
			currentGrabNum++;
			if (dragSuccess)
			{
				ApplyTargetResult(character);
			}
			if (destroyUse && currentGrabNum >= Math.Max(1, grabNum))
			{
				parentWillDestroy = true;
				PlayAudio(entrySplashAudio);
				if (createTargetSplash && GodotObject.IsInstanceValid(character))
				{
					character.CreateSplash();
				}
				owner.Destroy();
			}
			else
			{
				RestoreParentState();
				SendStateEvent(idleStateEvent);
			}
		}
		finally
		{
			CleanupGrabSprite();
			RestoreTargetInteraction();
			if (!parentWillDestroy)
			{
				RestoreParentState();
			}
			if (version == _dragVersion)
			{
				_dragRunning = false;
			}
		}
	}

	private bool CreateGrabSprite(TowerDefenseCharacter owner, TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(grabSpriteScene) || !GodotObject.IsInstanceValid(owner.spriteGroup))
		{
			return false;
		}
		_grabSprite = grabSpriteScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(_grabSprite))
		{
			return false;
		}
		EnsureFilterCache();
		if (_openFilters.Count > 0)
		{
			_grabSprite.SetFliters(_openFilters, open: true);
		}
		if (_closeFilters.Count > 0)
		{
			_grabSprite.SetFliters(_closeFilters, open: false);
		}
		owner.spriteGroup.AddChild(_grabSprite, forceReadableName: false, Node.InternalMode.Disabled);
		_grabSprite.Visible = true;
		if (!string.IsNullOrEmpty(grabAnimeClips))
		{
			_grabSprite.SetAnimation(grabAnimeClips, loop: false);
		}
		_grabSprite.timeScale = Mathf.Max(0.01f, grabAnimeTimeScale);
		_grabSprite.GlobalPosition = (GodotObject.IsInstanceValid(character) ? character.GetLogicalGlobalPosition() : owner.GetLogicalGlobalPosition());
		return true;
	}

	private void EnsureFilterCache()
	{
		if (_cachedOpenSource != grabFliterOpen || _cachedCloseSource != grabFliterClose)
		{
			_cachedOpenSource = grabFliterOpen;
			_cachedCloseSource = grabFliterClose;
			_openFilters.Clear();
			_closeFilters.Clear();
			AddFilters(_openFilters, grabFliterOpen);
			AddFilters(_closeFilters, grabFliterClose);
		}
	}

	private static void AddFilters(Godot.Collections.Array destination, string[] source)
	{
		if (source == null)
		{
			return;
		}
		for (int i = 0; i < source.Length; i++)
		{
			if (!string.IsNullOrEmpty(source[i]))
			{
				destination.Add(source[i]);
			}
		}
	}

	private void LockTargetInteraction(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			_lockedTarget = character;
			if (GodotObject.IsInstanceValid(character.sprite))
			{
				_savedTargetSpritePause = character.sprite.pause;
				character.sprite.pause = true;
			}
			character.SetHitBoxMonitorSuppressed(TowerDefenseCharacter.HitBoxSuppressionReason.Tanglekelp, suppressed: true);
			if (createTargetSplash)
			{
				character.CreateSplash();
			}
			_targetInteractionLocked = true;
		}
	}

	private void RestoreTargetInteraction()
	{
		if (!_targetInteractionLocked)
		{
			return;
		}
		_targetInteractionLocked = false;
		TowerDefenseCharacter lockedTarget = _lockedTarget;
		_lockedTarget = null;
		if (GodotObject.IsInstanceValid(lockedTarget))
		{
			if (GodotObject.IsInstanceValid(lockedTarget.sprite))
			{
				lockedTarget.sprite.pause = _savedTargetSpritePause;
			}
			lockedTarget.SetHitBoxMonitorSuppressed(TowerDefenseCharacter.HitBoxSuppressionReason.Tanglekelp, suppressed: false);
		}
	}

	private void ApplyTargetResult(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return;
		}
		if (!targetDestroyUse)
		{
			RestoreTargetInteraction();
			return;
		}
		double num = character.config?.dragHurt ?? (-1.0);
		if (num != -1.0)
		{
			double damageLimit = num;
			character.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, damageLimit);
			RestoreTargetInteraction();
		}
		else if (TryBlockLethalDragWithShield(character))
		{
			RestoreTargetInteraction();
		}
		else
		{
			_targetInteractionLocked = false;
			_lockedTarget = null;
			character.die = true;
			character.Destroy();
		}
	}

	private void SaveParentState(TowerDefenseCharacter owner)
	{
		if (!_parentStateSaved && GodotObject.IsInstanceValid(owner.instance))
		{
			_savedParentInvincible = owner.instance.invincible;
			_savedParentItemLayer = owner.itemLayer;
			_parentStateSaved = true;
		}
	}

	private void RestoreParentState()
	{
		if (!_parentStateSaved)
		{
			return;
		}
		_parentStateSaved = false;
		if (GodotObject.IsInstanceValid(parent))
		{
			if (GodotObject.IsInstanceValid(parent.instance))
			{
				parent.instance.invincible = _savedParentInvincible;
			}
			parent.itemLayer = _savedParentItemLayer;
		}
	}

	private async Task<bool> WaitDragDelayAsync(ulong version)
	{
		float num = Mathf.Max(0f, dragDelay);
		TowerDefenseCharacter owner;
		if (num <= 0f)
		{
			return IsCurrent(version, out owner);
		}
		if (!IsCurrent(version, out var owner2))
		{
			return false;
		}
		SceneTree tree = owner2.GetTree();
		if (!GodotObject.IsInstanceValid(tree))
		{
			return false;
		}
		SceneTreeTimer source = tree.CreateTimer(num, processAlways: false);
		await owner2.ToSignal(source, SceneTreeTimer.SignalName.Timeout);
		return IsCurrent(version, out owner);
	}

	private void CleanupGrabSprite()
	{
		if (GodotObject.IsInstanceValid(_grabSprite))
		{
			_grabSprite.QueueFree();
		}
		_grabSprite = null;
	}

	private bool CanStartDrag()
	{
		if (TryGetParent(out var owner))
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased)
			{
				IStateMachineController stateMachine = base.StateMachine;
				if (stateMachine != null && stateMachine.IsInitialized && GodotObject.IsInstanceValid(TowerDefenseManager.Instance) && TowerDefenseManager.Instance.IsGameRunning() && owner.inGame && owner.componentAlive)
				{
					return !owner.componentRunning;
				}
			}
		}
		return false;
	}

	private static bool CanDragTarget(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || character.config == null || character.IsHardControlImmune)
		{
			return false;
		}
		if (!character.config.canDragIntoWater)
		{
			return character.config.dragHurt != -1.0;
		}
		return true;
	}

	private static void PlayAudio(string audio)
	{
		if (!string.IsNullOrEmpty(audio) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(audio);
		}
	}

	private static bool TryBlockLethalDragWithShield(TowerDefenseCharacter character)
	{
		if (!(character is TowerDefensePlant) || !GodotObject.IsInstanceValid(character.cell))
		{
			return false;
		}
		TowerDefenseItemSheild itemShield = character.cell.itemShield;
		if (!GodotObject.IsInstanceValid(itemShield) || itemShield == character || !GodotObject.IsInstanceValid(itemShield.instance) || !GodotObject.IsInstanceValid(character.instance) || itemShield.instance.hypnoses != character.instance.hypnoses)
		{
			return false;
		}
		return itemShield.ShieldBlockLethal();
	}

	private bool IsCurrent(ulong version, out TowerDefenseCharacter owner)
	{
		owner = null;
		if (version == _dragVersion)
		{
			return TryGetParent(out owner);
		}
		return false;
	}

	private bool TryGetParent(out TowerDefenseCharacter owner)
	{
		owner = parent;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(owner))
		{
			return GodotObject.IsInstanceValid(owner.instance);
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary { { "currentGrabNum", currentGrabNum } };
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		currentGrabNum = data.GetValueOrDefault("currentGrabNum", currentGrabNum).AsInt32();
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary { { "currentGrabNum", currentGrabNum } };
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["targetSyncId"] = target.syncId;
		}
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		currentGrabNum = data.GetValueOrDefault("currentGrabNum", currentGrabNum).AsInt32();
		if (data.ContainsKey("targetSyncId"))
		{
			int num = data["targetSyncId"].AsInt32();
			target = null;
			if (num >= 0 && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl._syncCharacters.TryGetValue(num, out var value))
			{
				target = value;
			}
		}
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && GodotObject.IsInstanceValid(parent) && SyncForceState(base.StateMachine, _pendingSyncedState))
			{
				_pendingSyncedState = null;
			}
		}
	}

	private static bool SyncForceState(IStateMachineController state, string targetState)
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
