using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class SquashComponent : CharacterComponentRuntime
{
	public delegate void JumpDownSmashEventHandler();

	public delegate void HitCharactersEventHandler(List<TowerDefenseCharacter> characterList);

	public delegate void HitAliveCharactersEventHandler(List<TowerDefenseCharacter> characterList);

	private const double AirborneHeightEpsilon = 0.001;

	public AdobeAnimateSprite sprite;

	public AttackComponent attackComponent;

	public AabbShape2DResource checkShape;

	public Vector2 hurtRange = new Vector2(0.625f, 0.2f);

	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public bool checkAliveCharacter;

	public int aliveCheckPhysicsFrames = 2;

	public double cleanupDelay = 0.5;

	public double jumpHeight = 120.0;

	public double targetPositionDelay;

	public double noLookFallbackDelay = 0.1;

	public double lookCompletionDelay = 0.5;

	public double jumpUpLeadDelay = 0.3;

	public double jumpTravelDuration = 0.4;

	public double noJumpUpLandingDelay = 0.1;

	public double descentStartDelay = 0.4;

	public double descentDuration = 0.1;

	public bool trackMovingTargetUntilImpact;

	public bool completeStartedSequenceOutsideComponentBattlefield;

	public string lookLeftAnimeClip = "LookLeft";

	public string lookRightAnimeClip = "LookRight";

	public float lookAnimeTimeScale = 2f;

	public string jumpUpAnimeClip = "JumpUp";

	public string jumpDownAnimeClip = "JumpDown";

	public float jumpAnimeTimeScale = 3f;

	public float lookAnimationStartPosition = 0.1f;

	public float jumpUpAnimationStartPosition = 0.2f;

	public float jumpDownAnimationStartPosition = 0.1f;

	public string readyAudioName = "SquasHmm";

	public string smashAudioName = "GargantuarThump";

	public string waterAudioName = "PlantWater";

	public Vector2 cameraShakeRange = Vector2.One;

	public double cameraShakeStrength = 5.0;

	public double cameraShakeDuration = 0.05;

	public int cameraShakeFrequency = 4;

	public StringName readyStateEvent = "ToReady";

	public StringName jumpStateEvent = "ToJump";

	public TowerDefenseCharacter parent;

	public TowerDefenseCharacter target;

	public Vector2 savePos;

	public bool over;

	public bool running;

	private StateHandle _idleState;

	private StateHandle _readyState;

	private StateHandle _jumpState;

	private bool _spriteSignalsConnected;

	private bool _stateSignalsConnected;

	private bool _waitingForParentReady;

	private bool _runtimeInitialized;

	private bool _runtimeStateImported;

	private bool _retiredAsAttackTarget;

	private bool _configured;

	private bool _resumeAfterTemporaryDetach;

	private string _pendingStateName = "";

	private string _attackComponentInstanceId = "character.attack.0";

	private NodePath _spritePath = new NodePath();

	private Tween _readyTween;

	private Tween _jumpTween;

	private Tween _descentDelayTween;

	private Tween _descentTween;

	private Tween _cleanupTween;

	private SceneTree _aliveCheckTree;

	private Action _aliveCheckCallback;

	private int _aliveCheckFramesRemaining;

	private Vector2 _trackedMovementStart;

	private float _trackedMovementDirectionX;

	private readonly Array<TowerDefenseCharacter> _explodeExcludeList = new Array<TowerDefenseCharacter>();

	public bool ProtectsFromBites
	{
		get
		{
			if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active)
			{
				return running;
			}
			return false;
		}
	}

	private SquashComponentDefinition Definition => base.ComponentDefinition as SquashComponentDefinition;

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

	protected override bool AllowStateMachineOutsideComponentBattlefield
	{
		get
		{
			StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
			if (completeStartedSequenceOutsideComponentBattlefield && running && stateHandle != null && stateHandle.IsValid)
			{
				if (!(stateHandle.StableId == "squash.ready"))
				{
					return stateHandle.StableId == "squash.jump";
				}
				return true;
			}
			return false;
		}
	}

	public event JumpDownSmashEventHandler OnJumpDownSmash;

	public event HitCharactersEventHandler OnHitCharacters;

	public event HitAliveCharactersEventHandler OnHitAliveCharacters;

	protected override bool CanSendStateEventOutsideComponentBattlefield(StringName eventName)
	{
		if (!completeStartedSequenceOutsideComponentBattlefield || !running || (!(eventName == readyStateEvent) && !(eventName == jumpStateEvent)))
		{
			return base.CanSendStateEventOutsideComponentBattlefield(eventName);
		}
		return true;
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ConfigureOnce();
		ResolveReferences();
		ConnectSpriteSignals();
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				InitializeRuntime();
			}
		}
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		ConnectSpriteSignals();
		if (GodotObject.IsInstanceValid(parent) && parent.IsNodeReady())
		{
			InitializeRuntime();
		}
		else
		{
			ConnectParentReady();
		}
		ResumeAliveCheck();
		ApplyStateName(_pendingStateName);
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSpriteSignals();
		DisconnectParentReady();
		CancelMovementTweens();
		KillTween(ref _cleanupTween);
		SuspendAliveCheck();
		_resumeAfterTemporaryDetach = reason == ComponentDetachReason.TemporaryTreeExit && running;
		attackComponent = null;
		sprite = null;
		parent = null;
		if (reason != ComponentDetachReason.TemporaryTreeExit)
		{
			DisconnectAliveCheck();
		}
	}

	protected override void OnRuntimeReleased()
	{
		CancelRuntimeCallbacks();
		DisconnectSpriteSignals();
		DisconnectParentReady();
		DisconnectStateSignals();
		this.OnJumpDownSmash = null;
		this.OnHitCharacters = null;
		this.OnHitAliveCharacters = null;
		eventList.Clear();
		checkShape = null;
		sprite = null;
		attackComponent = null;
		parent = null;
		target = null;
		_idleState = null;
		_readyState = null;
		_jumpState = null;
		_pendingStateName = "";
		_configured = false;
	}

	protected override void OnAliveChanged(bool alive)
	{
		if (!alive)
		{
			CancelRuntimeCallbacks();
		}
	}

	private void ConfigureOnce()
	{
		if (_configured || Definition == null)
		{
			return;
		}
		SquashComponentDefinition definition = Definition;
		_attackComponentInstanceId = definition.attackComponentInstanceId ?? "character.attack.0";
		_spritePath = definition.spritePath ?? new NodePath();
		checkShape = (GodotObject.IsInstanceValid(definition.checkShape) ? (definition.checkShape.Duplicate(deep: true) as AabbShape2DResource) : null);
		hurtRange = definition.hurtRange;
		eventList = ((definition.eventList != null) ? definition.eventList.Duplicate(deep: true) : new Array<TowerDefenseCharacterEventBase>());
		for (int i = 0; i < eventList.Count; i++)
		{
			if (eventList[i] != null)
			{
				eventList[i] = (TowerDefenseCharacterEventBase)eventList[i].Duplicate(deep: true);
			}
		}
		checkAliveCharacter = definition.checkAliveCharacter;
		aliveCheckPhysicsFrames = Math.Max(0, definition.aliveCheckPhysicsFrames);
		cleanupDelay = Math.Max(0.0, definition.cleanupDelay);
		jumpHeight = definition.jumpHeight;
		targetPositionDelay = Math.Max(0.0, definition.targetPositionDelay);
		noLookFallbackDelay = Math.Max(0.0, definition.noLookFallbackDelay);
		lookCompletionDelay = Math.Max(0.0, definition.lookCompletionDelay);
		jumpUpLeadDelay = Math.Max(0.0, definition.jumpUpLeadDelay);
		jumpTravelDuration = Math.Max(0.0, definition.jumpTravelDuration);
		noJumpUpLandingDelay = Math.Max(0.0, definition.noJumpUpLandingDelay);
		descentStartDelay = Math.Max(0.0, definition.descentStartDelay);
		descentDuration = Math.Max(0.0, definition.descentDuration);
		trackMovingTargetUntilImpact = definition.trackMovingTargetUntilImpact;
		completeStartedSequenceOutsideComponentBattlefield = definition.completeStartedSequenceOutsideComponentBattlefield;
		lookLeftAnimeClip = definition.lookLeftAnimeClip ?? "LookLeft";
		lookRightAnimeClip = definition.lookRightAnimeClip ?? "LookRight";
		lookAnimeTimeScale = definition.lookAnimeTimeScale;
		jumpUpAnimeClip = definition.jumpUpAnimeClip ?? "JumpUp";
		jumpDownAnimeClip = definition.jumpDownAnimeClip ?? "JumpDown";
		jumpAnimeTimeScale = definition.jumpAnimeTimeScale;
		lookAnimationStartPosition = definition.lookAnimationStartPosition;
		jumpUpAnimationStartPosition = definition.jumpUpAnimationStartPosition;
		jumpDownAnimationStartPosition = definition.jumpDownAnimationStartPosition;
		readyAudioName = definition.readyAudioName ?? "SquasHmm";
		smashAudioName = definition.smashAudioName ?? "GargantuarThump";
		waterAudioName = definition.waterAudioName ?? "PlantWater";
		cameraShakeRange = definition.cameraShakeRange;
		cameraShakeStrength = definition.cameraShakeStrength;
		cameraShakeDuration = definition.cameraShakeDuration;
		cameraShakeFrequency = definition.cameraShakeFrequency;
		readyStateEvent = definition.readyStateEvent;
		jumpStateEvent = definition.jumpStateEvent;
		_configured = true;
	}

	private void ResolveReferences()
	{
		if (GodotObject.IsInstanceValid(parent) && base.Manager != null)
		{
			attackComponent = (string.IsNullOrEmpty(_attackComponentInstanceId) ? null : base.Manager.GetRuntime<AttackComponent>(_attackComponentInstanceId));
			sprite = ((!_spritePath.IsEmpty) ? parent.GetNodeOrNull<AdobeAnimateSprite>(_spritePath) : parent.sprite);
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased)
			{
				attackComponent.checkGravestone = true;
			}
		}
	}

	public bool SetRuntimeCheckRectangleSize(Vector2 size)
	{
		if (!GodotObject.IsInstanceValid(checkShape) || !(checkShape.Geometry is RectangleShape2D rectangleShape2D))
		{
			return false;
		}
		rectangleShape2D.Size = new Vector2(Mathf.Max(0f, size.X), Mathf.Max(0f, size.Y));
		return true;
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("squash.idle");
		_readyState = base.StateMachine?.GetStateById("squash.ready");
		_jumpState = base.StateMachine?.GetStateById("squash.jump");
		ConnectStateSignals();
		if (_runtimeInitialized)
		{
			ApplyStateName(_pendingStateName);
		}
	}

	protected override void OnStateRuntimeRegistered()
	{
		if (_runtimeInitialized)
		{
			ApplyStateName(_pendingStateName);
		}
		if (!_resumeAfterTemporaryDetach)
		{
			return;
		}
		_resumeAfterTemporaryDetach = false;
		string text = (base.StateMachine?.CurrentStateHandle)?.StableId.ToString();
		if (!(text == "squash.ready"))
		{
			if (text == "squash.jump")
			{
				MaterializeAuthoritativeJump(IsRemoteClient);
			}
		}
		else
		{
			MaterializeAuthoritativeReady();
		}
	}

	protected override void OnStateRuntimeDetaching()
	{
		CaptureCurrentStateForReattach();
		DisconnectStateSignals();
		CancelRuntimeCallbacks();
		_idleState = null;
		_readyState = null;
		_jumpState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingStateName = "";
		CancelRuntimeCallbacks();
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		switch (stateHandle.StableId.ToString())
		{
		case "squash.idle":
			if (!running)
			{
				target = null;
			}
			break;
		case "squash.ready":
			MaterializeAuthoritativeReady();
			break;
		case "squash.jump":
			MaterializeAuthoritativeJump(remote);
			break;
		default:
			DowngradeInvalidAuthoritativeState();
			break;
		}
	}

	private void MaterializeAuthoritativeReady()
	{
		if (!running || over || !savePos.IsFinite() || !TryGetRuntime(out var _))
		{
			DowngradeInvalidAuthoritativeState();
			return;
		}
		_retiredAsAttackTarget = true;
		RestoreRetiredState();
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		string clip = ((savePos.X < logicalGlobalPosition.X || (Mathf.IsEqualApprox(savePos.X, logicalGlobalPosition.X) && savePos.Y < logicalGlobalPosition.Y)) ? lookLeftAnimeClip : lookRightAnimeClip);
		if (SetAuthoritativeAnimation(clip, loop: false, lookAnimationStartPosition, lookAnimeTimeScale))
		{
			Schedule(ref _readyTween, lookCompletionDelay, RequestJumpState);
		}
		else
		{
			Schedule(ref _readyTween, noLookFallbackDelay, RequestJumpState);
		}
	}

	private void MaterializeAuthoritativeJump(bool remote)
	{
		if (!running || !GodotObject.IsInstanceValid(parent?.instance) || !savePos.IsFinite())
		{
			DowngradeInvalidAuthoritativeState();
			return;
		}
		_retiredAsAttackTarget = true;
		RestoreRetiredState();
		if (!(parent is TowerDefensePlant) || parent is TowerDefensePlantBowlingBase || over)
		{
			parent.instance.maskFlags = 0;
		}
		parent.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PROJECTILE;
		parent.gravity = 0.0;
		if (over)
		{
			parent.SetLogicalGlobalPosition(savePos);
			parent.z = parent.groundHeight;
			parent.isGround = true;
			SetAuthoritativeAnimation(jumpDownAnimeClip, loop: false, jumpDownAnimationStartPosition, jumpAnimeTimeScale);
			if (!remote)
			{
				Schedule(ref _cleanupTween, cleanupDelay, FinishDestroy);
			}
		}
		else
		{
			bool flag = SetAuthoritativeAnimation(jumpUpAnimeClip, loop: false, jumpUpAnimationStartPosition, jumpAnimeTimeScale);
			StartJumpMovement(!flag || sprite.clipRange.Y - sprite.clipRange.X <= 1);
		}
	}

	private bool SetAuthoritativeAnimation(string clip, bool loop, float startPosition, float timeScale)
	{
		if (!GodotObject.IsInstanceValid(sprite) || string.IsNullOrEmpty(clip) || !sprite.HasClip(clip))
		{
			return false;
		}
		sprite.SetAnimation(clip, loop, Math.Max(0f, startPosition));
		ApplyAnimationScale(timeScale);
		return true;
	}

	private void DowngradeInvalidAuthoritativeState()
	{
		CancelRuntimeCallbacks();
		running = false;
		over = false;
		target = null;
		RestoreStateSilently("squash.idle");
	}

	private void RestoreStateSilently(string stableId)
	{
		StateHandle stateHandle = base.StateMachine?.GetStateById(stableId);
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

	private void ConnectParentReady()
	{
		if (!_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready += OnParentReady;
			_waitingForParentReady = true;
		}
	}

	private void DisconnectParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
	}

	private void OnParentReady()
	{
		DisconnectParentReady();
		InitializeRuntime();
	}

	private void InitializeRuntime()
	{
		if (!_runtimeInitialized && GodotObject.IsInstanceValid(parent))
		{
			if (!_runtimeStateImported)
			{
				savePos = parent.GetLogicalGlobalPosition();
			}
			_runtimeInitialized = true;
			RestoreRetiredState();
			ApplyStateName(_pendingStateName);
		}
	}

	private void ConnectSpriteSignals()
	{
		if (!_spriteSignalsConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			_spriteSignalsConnected = true;
		}
	}

	private void DisconnectSpriteSignals()
	{
		if (_spriteSignalsConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
		}
		_spriteSignalsConnected = false;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_readyState, ReadyEntered, ReadyExited, ReadyProcessing);
			ConnectState(_jumpState, JumpEntered, JumpExited, JumpProcessing);
			_stateSignalsConnected = true;
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

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_readyState, ReadyEntered, ReadyExited, ReadyProcessing);
			DisconnectState(_jumpState, JumpEntered, JumpExited, JumpProcessing);
			_stateSignalsConnected = false;
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

	public void Execute(TowerDefenseCharacter newTarget)
	{
		if (!IsRemoteClient && !running && !over && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SetAlive(alive: true);
				running = true;
				target = newTarget;
				parent.Component();
				SendStateEvent(readyStateEvent);
			}
		}
	}

	public bool IsRunning()
	{
		return running;
	}

	public void IdleEntered()
	{
	}

	public void IdleProcessing(double delta)
	{
		if (IsRemoteClient || running || !TryGetRuntime(out var manager) || !manager.IsGameRunning() || !parent.inGame || !parent.componentAlive || parent.componentRunning)
		{
			return;
		}
		AttackComponent obj = attackComponent;
		if (obj == null || obj.IsReleased || !attackComponent.CanAttackOnContact())
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = attackComponent.target;
		if (towerDefenseCharacter is TowerDefenseReverseRake)
		{
			towerDefenseCharacter = null;
			foreach (TowerDefenseCharacter target in attackComponent.GetTargetList())
			{
				if (GodotObject.IsInstanceValid(target) && !(target is TowerDefenseReverseRake))
				{
					attackComponent.target = target;
					if (attackComponent.IsCurrentTargetReachable(target))
					{
						towerDefenseCharacter = target;
						break;
					}
				}
			}
			attackComponent.target = towerDefenseCharacter;
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			Execute(towerDefenseCharacter);
		}
	}

	public void IdleExited()
	{
	}

	public void ReadyEntered()
	{
		if (TryGetRuntime(out var _))
		{
			PlayAudio(readyAudioName);
			RetireParentAsAttackTarget();
			CaptureTargetPosition();
			Schedule(ref _readyTween, targetPositionDelay, CompleteReadyTargetDelay);
		}
	}

	private void CompleteReadyTargetDelay()
	{
		CaptureTargetPosition();
		if (TryGetRuntime(out var _) && !over && GodotObject.IsInstanceValid(sprite))
		{
			Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
			string text = ((savePos.X < logicalGlobalPosition.X || (Mathf.IsEqualApprox(savePos.X, logicalGlobalPosition.X) && savePos.Y < logicalGlobalPosition.Y)) ? lookLeftAnimeClip : lookRightAnimeClip);
			if (!string.IsNullOrEmpty(text) && sprite.HasClip(text))
			{
				sprite.SetAnimation(text, loop: false, Math.Max(0f, lookAnimationStartPosition));
			}
			else
			{
				Schedule(ref _readyTween, noLookFallbackDelay, RequestJumpState);
			}
		}
	}

	private void CaptureTargetPosition()
	{
		if (GodotObject.IsInstanceValid(target))
		{
			savePos = target.GetLogicalGlobalPosition();
		}
	}

	private void RequestJumpState()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !over)
		{
			SendStateEvent(jumpStateEvent);
		}
	}

	public void ReadyProcessing(double delta)
	{
		ApplyAnimationScale(lookAnimeTimeScale);
	}

	public void ReadyExited()
	{
		KillTween(ref _readyTween);
	}

	private void RetireParentAsAttackTarget()
	{
		if (!_retiredAsAttackTarget && GodotObject.IsInstanceValid(parent))
		{
			_retiredAsAttackTarget = true;
			RestoreRetiredState();
		}
	}

	private void RestoreRetiredState()
	{
		if (_retiredAsAttackTarget && GodotObject.IsInstanceValid(parent) && (!(parent is TowerDefensePlant) || parent is TowerDefensePlantBowlingBase || over))
		{
			if (GodotObject.IsInstanceValid(parent.instance))
			{
				parent.instance.canBeCollection = false;
				parent.instance.invincible = true;
			}
			TargetRegistrationComponent targetRegistrationComponent = parent.targetRegistrationComponent;
			if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased)
			{
				parent.targetRegistrationComponent.UnregisterTarget();
			}
			DestroyComponent destroyComponent = parent.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				parent.HitBoxDestroy();
			}
		}
	}

	public void JumpEntered()
	{
		if (TryGetRuntime(out var _) && GodotObject.IsInstanceValid(parent.instance))
		{
			RetireParentAsAttackTarget();
			parent.EmitDestroy();
			if (!(parent is TowerDefensePlant) || parent is TowerDefensePlantBowlingBase)
			{
				parent.instance.maskFlags = 0;
			}
			parent.itemLayer = TowerDefenseEnum.LAYER_GROUNDITEM.PROJECTILE;
			parent.gravity = 0.0;
			bool flag = GodotObject.IsInstanceValid(sprite) && !string.IsNullOrEmpty(jumpUpAnimeClip) && sprite.HasClip(jumpUpAnimeClip);
			if (flag)
			{
				sprite.SetAnimation(jumpUpAnimeClip, loop: false, Math.Max(0f, jumpUpAnimationStartPosition));
			}
			bool descendOnFinish = !flag || sprite.clipRange.Y - sprite.clipRange.X <= 1;
			Schedule(ref _jumpTween, flag ? jumpUpLeadDelay : 0.0, delegate
			{
				StartJumpMovement(descendOnFinish);
			});
		}
	}

	private void StartJumpMovement(bool descendOnFinish)
	{
		if (!TryGetRuntime(out var _) || over)
		{
			return;
		}
		KillTween(ref _jumpTween);
		parent.isGround = false;
		parent.z = Math.Max(parent.z, parent.groundHeight + 0.001);
		_jumpTween = parent.CreateTween().SetParallel().SetEase(Tween.EaseType.Out)
			.SetTrans(Tween.TransitionType.Quart);
		double duration = Math.Max(0.0, jumpTravelDuration);
		_jumpTween.TweenProperty(parent, "z", parent.groundHeight + jumpHeight, duration);
		if (trackMovingTargetUntilImpact)
		{
			BeginTrackedTargetMovement();
			_jumpTween.TweenMethod(Callable.From<double>(TrackTargetPosition), 0.0, 1.0, duration);
		}
		else
		{
			_jumpTween.TweenMethod(Callable.From<Vector2>(parent.SetLogicalGlobalPosition), parent.GetLogicalGlobalPosition(), savePos, duration);
		}
		if (descendOnFinish)
		{
			_jumpTween.Chain().TweenCallback(Callable.From(delegate
			{
				Schedule(ref _descentDelayTween, noJumpUpLandingDelay, StartDescent);
			}));
		}
	}

	private void StartDescent()
	{
		if (!TryGetRuntime(out var _) || over)
		{
			return;
		}
		RefreshLandingMetadata();
		bool flag = GodotObject.IsInstanceValid(sprite) && !string.IsNullOrEmpty(jumpDownAnimeClip) && sprite.HasClip(jumpDownAnimeClip);
		if (flag)
		{
			sprite.SetAnimation(jumpDownAnimeClip, loop: false, Math.Max(0f, jumpDownAnimationStartPosition));
		}
		KillTween(ref _descentTween);
		double duration = Math.Max(0.0, descentDuration);
		if (trackMovingTargetUntilImpact)
		{
			_descentTween = parent.CreateTween().SetParallel().SetEase(Tween.EaseType.Out)
				.SetTrans(Tween.TransitionType.Expo);
			_descentTween.TweenProperty(parent, "z", parent.groundHeight, duration);
			BeginTrackedTargetMovement();
			_descentTween.TweenMethod(Callable.From<double>(TrackTargetPosition), 0.0, 1.0, duration);
			if (!flag)
			{
				_descentTween.Chain().TweenCallback(Callable.From(JumpDownSmashAction));
			}
		}
		else
		{
			_descentTween = parent.CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Expo);
			_descentTween.TweenProperty(parent, "z", parent.groundHeight, duration);
			if (!flag)
			{
				_descentTween.TweenCallback(Callable.From(JumpDownSmashAction));
			}
		}
	}

	private void BeginTrackedTargetMovement()
	{
		CaptureTargetPosition();
		_trackedMovementStart = parent.GetLogicalGlobalPosition();
		_trackedMovementDirectionX = Math.Sign(savePos.X - _trackedMovementStart.X);
	}

	private void TrackTargetPosition(double progress)
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			CaptureTargetPosition();
			Vector2 logicalGlobalPosition = _trackedMovementStart.Lerp(savePos, Mathf.Clamp((float)progress, 0f, 1f));
			if (_trackedMovementDirectionX > 0f)
			{
				logicalGlobalPosition.X = Math.Min(logicalGlobalPosition.X, savePos.X);
			}
			else if (_trackedMovementDirectionX < 0f)
			{
				logicalGlobalPosition.X = Math.Max(logicalGlobalPosition.X, savePos.X);
			}
			parent.SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	private void SnapToTrackedTargetPosition()
	{
		if (trackMovingTargetUntilImpact && GodotObject.IsInstanceValid(parent))
		{
			CaptureTargetPosition();
			parent.SetLogicalGlobalPosition(savePos);
		}
	}

	private void RefreshLandingMetadata()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(parent))
		{
			parent.gridPos = instance.GetMapGridPos(parent.GetLogicalGlobalPosition());
			if (GodotObject.IsInstanceValid(parent.cell))
			{
				parent.groundHeight = parent.cell.GetGroundHeight();
			}
		}
	}

	public void JumpProcessing(double delta)
	{
		ApplyAnimationScale(jumpAnimeTimeScale);
	}

	public void JumpExited()
	{
	}

	public void AnimeCompleted(string clip)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && !over && !string.IsNullOrEmpty(clip))
		{
			if (clip == jumpUpAnimeClip)
			{
				Schedule(ref _descentDelayTween, descentStartDelay, StartDescent);
			}
			else if (clip == jumpDownAnimeClip)
			{
				JumpDownSmashAction();
			}
			else if (clip == lookLeftAnimeClip || clip == lookRightAnimeClip)
			{
				Schedule(ref _readyTween, lookCompletionDelay, RequestJumpState);
			}
		}
	}

	public void JumpDownSmashAction()
	{
		if (over || !TryGetRuntime(out var manager))
		{
			return;
		}
		SnapToTrackedTargetPosition();
		over = true;
		RestoreRetiredState();
		CancelMovementTweens();
		parent.z = parent.groundHeight;
		parent.isGround = true;
		if (IsRemoteClient)
		{
			return;
		}
		this.OnJumpDownSmash?.Invoke();
		ShakeCamera();
		if (!GodotObject.IsInstanceValid(parent.instance))
		{
			FinishDestroy();
			return;
		}
		TowerDefenseExplode.CreateExplode(parent.GetLogicalGlobalPosition(), hurtRange, eventList ?? new Array<TowerDefenseCharacterEventBase>(), _explodeExcludeList, parent.camp, parent.instance.collisionFlags);
		RefreshLandingMetadata();
		if (GodotObject.IsInstanceValid(parent.cell) && !checkAliveCharacter && parent.cell.isWater)
		{
			JumpDownWater();
			return;
		}
		PlayAudio(smashAudioName);
		if (TryGetCheckTargets(manager, out var targets))
		{
			this.OnHitCharacters?.Invoke(targets);
			if (checkAliveCharacter)
			{
				StartAliveCheckDelay();
				return;
			}
		}
		ScheduleCleanup();
	}

	private bool TryGetCheckTargets(TowerDefenseManager manager, out List<TowerDefenseCharacter> targets)
	{
		targets = null;
		if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(checkShape) || !checkShape.TryGetWorldRect(parent.GetLogicalGlobalTransform(parent), out var rect))
		{
			return false;
		}
		targets = manager.GetCharacterTargetFromRectList(parent, rect);
		return true;
	}

	private void StartAliveCheckDelay()
	{
		DisconnectAliveCheck();
		_aliveCheckFramesRemaining = Math.Max(0, aliveCheckPhysicsFrames);
		if (_aliveCheckFramesRemaining == 0)
		{
			CompleteAliveCheck();
			return;
		}
		_aliveCheckTree = parent?.GetTree();
		if (!GodotObject.IsInstanceValid(_aliveCheckTree))
		{
			CompleteAliveCheck();
			return;
		}
		_aliveCheckCallback = OnAliveCheckPhysicsFrame;
		_aliveCheckTree.PhysicsFrame += _aliveCheckCallback;
	}

	private void OnAliveCheckPhysicsFrame()
	{
		if (--_aliveCheckFramesRemaining <= 0)
		{
			DisconnectAliveCheck();
			CompleteAliveCheck();
		}
	}

	private void CompleteAliveCheck()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (TryGetCheckTargets(instance, out var targets))
		{
			this.OnHitAliveCharacters?.Invoke(targets);
		}
		if (GodotObject.IsInstanceValid(parent?.cell) && parent.cell.isWater)
		{
			JumpDownWater();
		}
		else
		{
			ScheduleCleanup();
		}
	}

	private void ScheduleCleanup()
	{
		Schedule(ref _cleanupTween, cleanupDelay, FinishDestroy);
	}

	private void FinishDestroy()
	{
		if (GodotObject.IsInstanceValid(parent) && !parent.isDestroy)
		{
			parent.isDestroy = true;
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.CharacterUnregister(parent);
			}
			parent.RemoveFromGroup("Character");
			parent.QueueFree();
		}
	}

	public void JumpDownWater()
	{
		if (GodotObject.IsInstanceValid(parent))
		{
			CancelRuntimeCallbacks();
			parent.CreateSplash();
			PlayAudio(waterAudioName);
			ScheduleCleanup();
		}
	}

	private void ApplyAnimationScale(float scale)
	{
		if (GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(parent))
		{
			sprite.timeScale = parent.timeScale * (double)scale;
		}
	}

	private void ShakeCamera()
	{
		if (GodotObject.IsInstanceValid(ViewManager.Instance))
		{
			Vector2 dir = new Vector2((float)GD.RandRange(0f - Math.Abs(cameraShakeRange.X), Math.Abs(cameraShakeRange.X)), (float)GD.RandRange(0f - Math.Abs(cameraShakeRange.Y), Math.Abs(cameraShakeRange.Y)));
			ViewManager.Instance.CameraShake(dir, Math.Max(0.0, cameraShakeStrength), Math.Max(0.0, cameraShakeDuration), Math.Max(0, cameraShakeFrequency));
		}
	}

	private static void PlayAudio(string name)
	{
		if (!string.IsNullOrEmpty(name) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(name);
		}
	}

	private void Schedule(ref Tween tween, double delay, Action callback)
	{
		KillTween(ref tween);
		if (TryGetRuntime(out var _) && parent.IsInsideTree())
		{
			tween = parent.CreateTween();
			if (delay > 0.0)
			{
				tween.TweenInterval(delay);
			}
			tween.TweenCallback(Callable.From(callback));
		}
	}

	private static void KillTween(ref Tween tween)
	{
		if (GodotObject.IsInstanceValid(tween))
		{
			tween.Kill();
		}
		tween = null;
	}

	private void CancelMovementTweens()
	{
		KillTween(ref _readyTween);
		KillTween(ref _jumpTween);
		KillTween(ref _descentDelayTween);
		KillTween(ref _descentTween);
	}

	private void CancelRuntimeCallbacks()
	{
		CancelMovementTweens();
		KillTween(ref _cleanupTween);
		DisconnectAliveCheck();
	}

	private void DisconnectAliveCheck()
	{
		SuspendAliveCheck();
		_aliveCheckFramesRemaining = 0;
	}

	private void SuspendAliveCheck()
	{
		if (_aliveCheckCallback != null && GodotObject.IsInstanceValid(_aliveCheckTree))
		{
			_aliveCheckTree.PhysicsFrame -= _aliveCheckCallback;
		}
		_aliveCheckTree = null;
		_aliveCheckCallback = null;
	}

	private void ResumeAliveCheck()
	{
		if (_aliveCheckFramesRemaining > 0 && _aliveCheckCallback == null && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree())
		{
			_aliveCheckTree = parent.GetTree();
			if (GodotObject.IsInstanceValid(_aliveCheckTree))
			{
				_aliveCheckCallback = OnAliveCheckPhysicsFrame;
				_aliveCheckTree.PhysicsFrame += _aliveCheckCallback;
			}
		}
	}

	private bool TryGetRuntime(out TowerDefenseManager manager)
	{
		manager = TowerDefenseManager.Instance;
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			return GodotObject.IsInstanceValid(manager);
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "savePos", savePos },
			{ "over", over },
			{ "running", running },
			{ "alive", base.Alive },
			{ "retiredAsAttackTarget", _retiredAsAttackTarget }
		};
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["target"] = StringExtensions.ValidateNodeName(target.Name);
		}
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		savePos = data.GetValueOrDefault("savePos", Vector2.Zero).AsVector2();
		over = data.GetValueOrDefault("over", false).AsBool();
		running = data.GetValueOrDefault("running", false).AsBool();
		_retiredAsAttackTarget = data.GetValueOrDefault("retiredAsAttackTarget", running).AsBool();
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		string text = data.GetValueOrDefault("target", "").AsString();
		if (text != "" && owner?.charcterDicionary != null && owner.charcterDicionary.ContainsKey(text))
		{
			target = owner.charcterDicionary[text];
		}
		_runtimeStateImported = true;
		RestoreRetiredState();
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "over", over },
			{ "running", running },
			{ "savePosX", savePos.X },
			{ "savePosY", savePos.Y },
			{ "alive", base.Alive },
			{ "retiredAsAttackTarget", _retiredAsAttackTarget }
		};
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["targetSyncId"] = target.syncId;
		}
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		over = data.GetValueOrDefault("over", over).AsBool();
		running = data.GetValueOrDefault("running", running).AsBool();
		savePos = new Vector2(data.GetValueOrDefault("savePosX", savePos.X).AsSingle(), data.GetValueOrDefault("savePosY", savePos.Y).AsSingle());
		_retiredAsAttackTarget = data.GetValueOrDefault("retiredAsAttackTarget", _retiredAsAttackTarget).AsBool();
		SetAlive(data.GetValueOrDefault("alive", base.Alive).AsBool());
		if (data.ContainsKey("targetSyncId") && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl))
		{
			int key = data["targetSyncId"].AsInt32();
			TowerDefenseManager.CurrentControl._syncCharacters.TryGetValue(key, out target);
		}
		RestoreRetiredState();
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
	}

	private void CaptureCurrentStateForReattach()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (string.IsNullOrEmpty(_pendingStateName) && stateHandle != null && stateHandle.IsValid)
		{
			_pendingStateName = stateHandle.StableId;
		}
	}

	private void ApplyStateName(string name)
	{
		if (string.IsNullOrEmpty(name))
		{
			return;
		}
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true)
			{
				_pendingStateName = (SyncForceState(base.StateMachine, name) ? "" : name);
				return;
			}
		}
		_pendingStateName = name;
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId.ToString();
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
