using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class CannonComponent : CharacterComponentRuntime
{
	public delegate void FireEventHandler();

	public delegate void RestEventHandler();

	public delegate void ChargeEventHandler();

	private const string MarkerMode = "Marker";

	private const string LineMode = "Line";

	private const string RestTimerName = "Rest";

	public string mode = "Marker";

	public bool autoAttack;

	public double autoAttackMaxXOffset;

	public MousePressComponent mousePressComponent;

	public AdobeAnimateSlot projectileSlot;

	public Node2D projectileNode;

	public TowerDefenseProjectileCreateData projectileData;

	public FireComponent projectileLineFireComponent;

	public Marker2D projectileLineMarker;

	public AdobeAnimateSprite sprite;

	public double restTime = 30.0;

	public double firstRestTime = 3.0;

	public bool disableRestTimeout;

	public double markerProjectileHeight = 600.0;

	public double markerProjectileFallSpeed = 800.0;

	public Vector2 markerProjectileHitBoxScale = new Vector2(2f, 1f);

	public double markerVisualTravelHeight = 600.0;

	public double markerVisualTravelDuration = 0.75;

	public bool markerProjectileStraightMove;

	public double markerProjectileStraightSpeed = 600.0;

	public Vector2 lineProjectileVelocity = new Vector2(800f, 0f);

	public int lineFirePositionIndex;

	public bool lineProjectileFlipWithParent = true;

	public string fireReadyEventName = "fire_ready";

	public string fireEventName = "fire";

	public string restAnimeClips = "Rest";

	public double restAnimeTimeScale = 1.0;

	public string chargeAnimeClips = "Charge";

	public double chargeAnimeTimeScale = 1.0;

	public string fireAnimeClips = "Fire";

	public double fireAnimeTimeScale = 1.0;

	public double animationStartPosition = 0.2;

	public string chargeAudioName = "Shoop";

	public string fireReadyAudioName = "CobLaunch";

	public StringName idleStateEvent = "ToIdle";

	public StringName restStateEvent = "ToRest";

	public StringName chargeStateEvent = "ToCharge";

	public StringName fireStateEvent = "ToFire";

	public TowerDefenseCharacter parent;

	private bool _canFire;

	public Vector2 targetPos = Vector2.Zero;

	private StateHandle _idleState;

	private StateHandle _restState;

	private StateHandle _chargeState;

	private StateHandle _fireState;

	private Tween _launchTween;

	private bool _signalsConnected;

	private bool _waitingForParentReady;

	private bool _runtimeInitialized;

	private bool _runtimeStateImported;

	private bool _restTimerPaused;

	private bool _pendingRestTimeout;

	private double _pausedRestTimerRemaining;

	private double _restTimerRemaining;

	private bool _restTimerRunning;

	private AttackComponent _sharedAnimationAttackComponent;

	private bool _animationControlYielded;

	private bool _sleepInterrupted;

	private string _pendingStateName = "";

	private double _groundRight;

	private bool _configured;

	private readonly List<TowerDefenseCharacter> _autoAttackFilterBuffer = new List<TowerDefenseCharacter>();

	internal override bool WantsPhysicsProcess => true;

	public string projectileName
	{
		get
		{
			if (!GodotObject.IsInstanceValid(projectileData))
			{
				return "";
			}
			return projectileData.projectileName;
		}
	}

	public bool canFire
	{
		get
		{
			return _canFire;
		}
		set
		{
			if (_canFire != value)
			{
				_canFire = value;
				MousePressComponent obj = mousePressComponent;
				if (obj != null && !obj.IsReleased)
				{
					mousePressComponent.SetAlive(value);
				}
			}
		}
	}

	private CannonComponentDefinition Definition => base.ComponentDefinition as CannonComponentDefinition;

	public event FireEventHandler OnFire;

	public event RestEventHandler OnRest;

	public event ChargeEventHandler OnCharge;

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		ConfigureLineProjectileMarker();
		ConnectSignals();
		if (GodotObject.IsInstanceValid(projectileNode))
		{
			projectileNode.Visible = false;
		}
		if (GodotObject.IsInstanceValid(parent))
		{
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				RefreshMapMetrics();
			}
		}
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		Callable.From(ReattachRuntimeAfterTreeReentry).CallDeferred();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		PauseRestTimer();
		KillLaunchTween();
		_animationControlYielded = false;
		canFire = false;
	}

	protected override void OnAliveChanged(bool value)
	{
		if (!value)
		{
			PauseRestTimer();
		}
		if (!value)
		{
			canFire = false;
			KillLaunchTween();
			_animationControlYielded = false;
		}
		else
		{
			ResumeRestTimerOrTimeout();
		}
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			CannonComponentDefinition definition = Definition;
			mode = definition.mode;
			autoAttack = definition.autoAttack;
			autoAttackMaxXOffset = definition.autoAttackMaxXOffset;
			projectileData = definition.projectileData?.Duplicate(deep: true) as TowerDefenseProjectileCreateData;
			restTime = definition.restTime;
			firstRestTime = definition.firstRestTime;
			disableRestTimeout = definition.disableRestTimeout;
			markerProjectileHeight = definition.markerProjectileHeight;
			markerProjectileFallSpeed = definition.markerProjectileFallSpeed;
			markerProjectileHitBoxScale = definition.markerProjectileHitBoxScale;
			markerVisualTravelHeight = definition.markerVisualTravelHeight;
			markerVisualTravelDuration = definition.markerVisualTravelDuration;
			markerProjectileStraightMove = definition.markerProjectileStraightMove;
			markerProjectileStraightSpeed = definition.markerProjectileStraightSpeed;
			lineProjectileVelocity = definition.lineProjectileVelocity;
			lineFirePositionIndex = definition.lineFirePositionIndex;
			lineProjectileFlipWithParent = definition.lineProjectileFlipWithParent;
			fireReadyEventName = definition.fireReadyEventName;
			fireEventName = definition.fireEventName;
			restAnimeClips = definition.restAnimeClips;
			restAnimeTimeScale = definition.restAnimeTimeScale;
			chargeAnimeClips = definition.chargeAnimeClips;
			chargeAnimeTimeScale = definition.chargeAnimeTimeScale;
			fireAnimeClips = definition.fireAnimeClips;
			fireAnimeTimeScale = definition.fireAnimeTimeScale;
			animationStartPosition = definition.animationStartPosition;
			chargeAudioName = definition.chargeAudioName;
			fireReadyAudioName = definition.fireReadyAudioName;
			idleStateEvent = definition.idleStateEvent;
			restStateEvent = definition.restStateEvent;
			chargeStateEvent = definition.chargeStateEvent;
			fireStateEvent = definition.fireStateEvent;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		CannonComponentDefinition definition = Definition;
		if (GodotObject.IsInstanceValid(parent) && definition != null)
		{
			mousePressComponent = base.Manager?.GetRuntime<MousePressComponent>();
			projectileSlot = ResolveNode<AdobeAnimateSlot>(definition.projectileSlotPath);
			projectileNode = ResolveNode<Node2D>(definition.projectileNodePath);
			projectileLineFireComponent = base.Manager?.GetRuntime<FireComponent>(definition.projectileLineFireComponentInstanceId);
			projectileLineMarker = ResolveNode<Marker2D>(definition.projectileLineMarkerPath);
			sprite = ResolveNode<AdobeAnimateSprite>(definition.spritePath);
			ResolveSharedAnimationAttackComponent();
		}
	}

	private void ResolveSharedAnimationAttackComponent()
	{
		_sharedAnimationAttackComponent = null;
		if (!GodotObject.IsInstanceValid(sprite) || base.Manager == null)
		{
			return;
		}
		foreach (CharacterComponentRuntime resourceComponent in base.Manager.ResourceComponents)
		{
			if (resourceComponent is AttackComponent { IsReleased: false } attackComponent && attackComponent.sprite == sprite)
			{
				_sharedAnimationAttackComponent = attackComponent;
				break;
			}
		}
	}

	private T ResolveNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(parent))
		{
			return parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("cannon.idle");
		_restState = base.StateMachine?.GetStateById("cannon.rest");
		_chargeState = base.StateMachine?.GetStateById("cannon.charge");
		_fireState = base.StateMachine?.GetStateById("cannon.fire");
		ConnectStateSignals();
		Callable.From(InitializeRuntime).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		InitializeRuntime();
		if (_runtimeInitialized && !string.IsNullOrEmpty(_pendingStateName))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingStateName))
			{
				_pendingStateName = "";
			}
		}
		ResumeRestTimerOrTimeout();
	}

	protected override void OnStateRuntimeDetaching()
	{
		KillLaunchTween();
		canFire = false;
		DisconnectStateSignals();
		_idleState = null;
		_restState = null;
		_chargeState = null;
		_fireState = null;
	}

	protected override void OnReleased()
	{
		DisconnectSignals();
		StopRestTimer();
		KillLaunchTween();
		canFire = false;
		_runtimeInitialized = false;
		parent = null;
		mousePressComponent = null;
		projectileSlot = null;
		projectileNode = null;
		projectileLineFireComponent = null;
		projectileLineMarker = null;
		sprite = null;
		projectileData = null;
		_sharedAnimationAttackComponent = null;
		_animationControlYielded = false;
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

	private void ConnectSignals()
	{
		if (!_signalsConnected)
		{
			ConnectMouseSignals();
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.OnAnimeCompleted += AnimeCompleted;
				sprite.OnAnimeEvent += AnimeEvent;
			}
			_signalsConnected = true;
		}
	}

	private void ReattachRuntimeAfterTreeReentry()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree())
		{
			ResolveReferences();
			ConnectMouseSignals();
			InitializeRuntime();
			ResumeRestTimerOrTimeout();
		}
	}

	private void DisconnectSignals()
	{
		DisconnectParentReady();
		if (mousePressComponent != null)
		{
			mousePressComponent.OnFinishPressed -= FireAt;
			mousePressComponent.OnPressed -= FireAt;
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= AnimeCompleted;
			sprite.OnAnimeEvent -= AnimeEvent;
		}
		_signalsConnected = false;
	}

	private void ConnectStateSignals()
	{
		StateHandle idleState = _idleState;
		if (idleState != null && idleState.IsValid)
		{
			_idleState.Entered += IdleEntered;
			_idleState.Exited += IdleExited;
			_idleState.PhysicsProcessing += IdleProcessing;
		}
		StateHandle restState = _restState;
		if (restState != null && restState.IsValid)
		{
			_restState.Entered += RestEntered;
			_restState.Exited += RestExited;
			_restState.PhysicsProcessing += RestProcessing;
		}
		StateHandle chargeState = _chargeState;
		if (chargeState != null && chargeState.IsValid)
		{
			_chargeState.Entered += ChargeEntered;
			_chargeState.Exited += ChargeExited;
			_chargeState.PhysicsProcessing += ChargeProcessing;
		}
		StateHandle fireState = _fireState;
		if (fireState != null && fireState.IsValid)
		{
			_fireState.Entered += FireEntered;
			_fireState.Exited += FireExited;
			_fireState.PhysicsProcessing += FireProcessing;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_idleState != null)
		{
			_idleState.Entered -= IdleEntered;
			_idleState.Exited -= IdleExited;
			_idleState.PhysicsProcessing -= IdleProcessing;
		}
		if (_restState != null)
		{
			_restState.Entered -= RestEntered;
			_restState.Exited -= RestExited;
			_restState.PhysicsProcessing -= RestProcessing;
		}
		if (_chargeState != null)
		{
			_chargeState.Entered -= ChargeEntered;
			_chargeState.Exited -= ChargeExited;
			_chargeState.PhysicsProcessing -= ChargeProcessing;
		}
		if (_fireState != null)
		{
			_fireState.Entered -= FireEntered;
			_fireState.Exited -= FireExited;
			_fireState.PhysicsProcessing -= FireProcessing;
		}
	}

	private void ConnectMouseSignals()
	{
		MousePressComponent obj = mousePressComponent;
		if (obj != null && !obj.IsReleased)
		{
			mousePressComponent.OnFinishPressed -= FireAt;
			mousePressComponent.OnPressed -= FireAt;
			if (NormalizeMode(mode) == "Line" && !mousePressComponent.ToggleMode)
			{
				mousePressComponent.OnPressed += FireAt;
			}
			else
			{
				mousePressComponent.OnFinishPressed += FireAt;
			}
			mousePressComponent.SetAlive(alive: false);
		}
	}

	private void InitializeRuntime()
	{
		if (!base.IsStateMachineRegistered || _runtimeInitialized || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		if (!parent.IsNodeReady())
		{
			ConnectParentReady();
			return;
		}
		RefreshMapMetrics();
		if (!_runtimeStateImported && !IsRemoteSyncedClient())
		{
			StartRestTimer(Math.Max(0.0, firstRestTime));
			SendStateEvent(restStateEvent);
		}
		else if (!string.IsNullOrEmpty(_pendingStateName) && SyncForceState(base.StateMachine, _pendingStateName))
		{
			_pendingStateName = "";
		}
		if (!IsRemoteSyncedClient())
		{
			parent.Component();
		}
		canFire = false;
		_runtimeInitialized = true;
		ResumeRestTimerOrTimeout();
	}

	public void RefreshConfiguration()
	{
		ConfigureLineProjectileMarker();
		ConnectMouseSignals();
		RefreshMapMetrics();
	}

	private void ConfigureLineProjectileMarker()
	{
		FireComponent fireComponent = projectileLineFireComponent;
		if (fireComponent != null && !fireComponent.IsReleased && GodotObject.IsInstanceValid(projectileLineMarker) && lineFirePositionIndex >= 0)
		{
			fireComponent = projectileLineFireComponent;
			if (fireComponent.firePosMarker == null)
			{
				Array<Marker2D> array = (fireComponent.firePosMarker = new Array<Marker2D>());
			}
			while (projectileLineFireComponent.firePosMarker.Count <= lineFirePositionIndex)
			{
				projectileLineFireComponent.firePosMarker.Add(null);
			}
			projectileLineFireComponent.firePosMarker[lineFirePositionIndex] = projectileLineMarker;
			projectileLineFireComponent.RefreshConfiguration();
		}
	}

	public void RefreshMapMetrics()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			_groundRight = instance.GetMapGroundRight();
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent))
		{
			PhysicsProcessValidated(delta);
		}
	}

	private void PhysicsProcessValidated(double delta)
	{
		if (!_runtimeInitialized)
		{
			return;
		}
		if (!parent.inGame)
		{
			PauseRestTimer();
			return;
		}
		if (GodotObject.IsInstanceValid(parent.instance) && parent.instance.sleep)
		{
			_sleepInterrupted = true;
			canFire = false;
			PauseRestTimer();
			return;
		}
		if (_sleepInterrupted && IsParentOperational())
		{
			_sleepInterrupted = false;
			StateHandle fireState = _fireState;
			if (fireState != null && fireState.IsActive)
			{
				StartRestTimer(Math.Max(0.0, restTime));
				SendStateEvent(restStateEvent);
			}
			else
			{
				StateHandle chargeState = _chargeState;
				if (chargeState != null && chargeState.IsActive)
				{
					canFire = true;
					if (TryBeginAnimationControl())
					{
						PlayChargeAnimation();
					}
				}
				else
				{
					StateHandle restState = _restState;
					if (restState != null && restState.IsActive)
					{
						if (TryBeginAnimationControl())
						{
							PlayRestAnimation();
						}
					}
					else
					{
						StateHandle idleState = _idleState;
						if (idleState != null && idleState.IsActive)
						{
							PlayReadyAnimation();
						}
					}
				}
			}
		}
		TickRestTimer(delta);
		ResumeRestTimerOrTimeout();
		if (!autoAttack || !canFire)
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseBattleCharacterRegistry towerDefenseBattleCharacterRegistry = instance?.characterRegistry;
		Vector2 position;
		if (towerDefenseBattleCharacterRegistry == null || !towerDefenseBattleCharacterRegistry.HasRegisteredCharacters)
		{
			if (_autoAttackFilterBuffer.Count != 0)
			{
				_autoAttackFilterBuffer.Clear();
			}
		}
		else if (GodotObject.IsInstanceValid(instance) && GodotObject.IsInstanceValid(towerDefenseBattleCharacterRegistry) && !IsRemoteSyncedClient() && TrySelectAutoAttackTarget(towerDefenseBattleCharacterRegistry, out position))
		{
			FireAt(position);
		}
	}

	private bool TrySelectAutoAttackTarget(TowerDefenseBattleCharacterRegistry registry, out Vector2 position)
	{
		position = Vector2.Zero;
		registry.FillCampTargets(parent.camp, _autoAttackFilterBuffer);
		int num = 0;
		for (int i = 0; i < _autoAttackFilterBuffer.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _autoAttackFilterBuffer[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && GodotObject.IsInstanceValid(towerDefenseCharacter.instance) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && towerDefenseCharacter.instance.canBeCollection && !towerDefenseCharacter.instance.invincible && !((double)towerDefenseCharacter.GetLogicalGlobalPosition().X > _groundRight + autoAttackMaxXOffset))
			{
				_autoAttackFilterBuffer[num++] = towerDefenseCharacter;
			}
		}
		if (num < _autoAttackFilterBuffer.Count)
		{
			_autoAttackFilterBuffer.RemoveRange(num, _autoAttackFilterBuffer.Count - num);
		}
		if (num == 0)
		{
			return false;
		}
		int index = (int)(GD.Randi() % (uint)num);
		position = _autoAttackFilterBuffer[index].GetLogicalGlobalPosition();
		return true;
	}

	public void FireAt(Vector2 pos)
	{
		if (!IsRemoteSyncedClient() && CanFire() && CanFireWithCurrentMode())
		{
			parent.EmitComponentChange();
			this.OnFire?.Invoke();
			canFire = false;
			targetPos = pos;
			if (!(parent is TowerDefenseZombie))
			{
				parent.Component();
			}
			SendStateEvent(fireStateEvent);
		}
	}

	public bool CanFire()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _runtimeInitialized && canFire && GodotObject.IsInstanceValid(parent))
		{
			return base.StateMachine?.IsInitialized ?? false;
		}
		return false;
	}

	public bool Canfire()
	{
		return CanFire();
	}

	private bool CanFireWithCurrentMode()
	{
		if (projectileData == null)
		{
			return false;
		}
		if (NormalizeMode(mode) == "Line")
		{
			FireComponent fireComponent = projectileLineFireComponent;
			if (fireComponent != null)
			{
				return !fireComponent.IsReleased;
			}
			return false;
		}
		return true;
	}

	public void IdleEntered()
	{
		if (IsRemoteSyncedClient() || !IsParentOperational())
		{
			return;
		}
		if (IsRestTimerRunning())
		{
			SendStateEvent(restStateEvent);
			parent.Component();
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || !instance.IsGameRunning() || !(parent is TowerDefenseZombie))
		{
			parent.Idle();
		}
		canFire = true;
	}

	public void IdleProcessing(double delta)
	{
		if (!IsRemoteSyncedClient())
		{
			canFire = IsParentOperational();
		}
		RestoreReadyAnimationAfterYield();
	}

	public void IdleExited()
	{
	}

	public void RestEntered()
	{
		this.OnRest?.Invoke();
		if (TryBeginAnimationControl())
		{
			PlayRestAnimation();
		}
	}

	public void RestProcessing(double delta)
	{
		if (TryResumeAnimationControl(PlayRestAnimation))
		{
			ApplyAnimationTimeScale(restAnimeTimeScale);
		}
	}

	public void RestExited()
	{
	}

	public void ChargeEntered()
	{
		this.OnCharge?.Invoke();
		if (!IsRemoteSyncedClient())
		{
			canFire = IsParentOperational();
		}
		if (TryBeginAnimationControl())
		{
			PlayChargeAnimation();
		}
		PlayAudio(chargeAudioName);
	}

	public void ChargeProcessing(double delta)
	{
		if (TryResumeAnimationControl(PlayChargeAnimation))
		{
			ApplyAnimationTimeScale(chargeAnimeTimeScale);
		}
	}

	public void ChargeExited()
	{
	}

	public void FireEntered()
	{
		_animationControlYielded = false;
		PlayFireAnimation();
	}

	public void FireProcessing(double delta)
	{
		ApplyAnimationTimeScale(fireAnimeTimeScale);
	}

	public void FireExited()
	{
	}

	private bool TryBeginAnimationControl()
	{
		if (IsSharedAnimationAttackActive())
		{
			_animationControlYielded = true;
			return false;
		}
		_animationControlYielded = false;
		return true;
	}

	private bool TryResumeAnimationControl(Action restoreAnimation)
	{
		if (IsSharedAnimationAttackActive())
		{
			_animationControlYielded = true;
			return false;
		}
		if (_animationControlYielded)
		{
			_animationControlYielded = false;
			restoreAnimation?.Invoke();
		}
		return true;
	}

	private void RestoreReadyAnimationAfterYield()
	{
		if (IsSharedAnimationAttackActive())
		{
			_animationControlYielded = true;
		}
		else if (_animationControlYielded)
		{
			_animationControlYielded = false;
			PlayReadyAnimation();
			ApplyAnimationTimeScale(chargeAnimeTimeScale);
		}
	}

	private bool IsSharedAnimationAttackActive()
	{
		AttackComponent sharedAnimationAttackComponent = _sharedAnimationAttackComponent;
		if (sharedAnimationAttackComponent == null || sharedAnimationAttackComponent.IsReleased || !sharedAnimationAttackComponent.Alive || sharedAnimationAttackComponent.Lifecycle != ComponentRuntimeLifecycle.Active || sharedAnimationAttackComponent.sprite != sprite)
		{
			return false;
		}
		StateHandle stateHandle = sharedAnimationAttackComponent.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			return stateHandle.StableId == "attack.attack";
		}
		return false;
	}

	private void PlayReadyAnimation()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(chargeAnimeClips, loop: false, (float)Math.Max(0.0, animationStartPosition));
		}
	}

	private void PlayRestAnimation()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(restAnimeClips, loop: true, (float)Math.Max(0.0, animationStartPosition));
		}
	}

	private void PlayChargeAnimation()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(chargeAnimeClips, loop: false, (float)Math.Max(0.0, animationStartPosition));
		}
	}

	private void PlayFireAnimation()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(fireAnimeClips, loop: false, (float)Math.Max(0.0, animationStartPosition));
		}
	}

	private void ApplyAnimationTimeScale(double multiplier)
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			bool flag = GodotObject.IsInstanceValid(instance) && instance.IsIZMMode();
			double num = ((!GodotObject.IsInstanceValid(parent)) ? 1.0 : (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0));
			sprite.timeScale = ((flag || !GodotObject.IsInstanceValid(parent)) ? (num * multiplier) : (parent.timeScale * num * multiplier));
		}
	}

	private bool TryFireMarkerProjectile()
	{
		if (projectileData == null || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return false;
		}
		Vector2I mapGridPosFromMouse = instance.GetMapGridPosFromMouse(targetPos);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(mapGridPosFromMouse);
		Vector2 pos = new Vector2(targetPos.X, (float)TowerDefenseManager.GetMapLineY(mapGridPosFromMouse.Y));
		if (markerProjectileStraightMove)
		{
			FireMarkerExplosion(pos, mapGridPosFromMouse.Y);
			return true;
		}
		double height = (GodotObject.IsInstanceValid(mapCell) ? (0.0 - mapCell.GetGroundHeight()) : 0.0);
		double value = Math.Max(0.0, markerProjectileHeight);
		double value2 = Math.Max(0.0, markerProjectileFallSpeed);
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			useFall = true,
			gridYOverride = mapGridPosFromMouse.Y,
			zOverride = value,
			ySpeedOverride = value2,
			hitBoxScaleOverride = markerProjectileHitBoxScale,
			fireMethodFlagsOverride = (projectileData.fireMethodFlags & -33)
		};
		FireComponent.CreateProjectilePosition(null, null, height, pos, Vector2.Zero, projectileData, -1, parent.camp, default(Vector2), overrides);
		return true;
	}

	private void FireMarkerExplosion(Vector2 pos, int gridY)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		double num = projectileData?.baseDamage ?? 0.0;
		TowerDefenseCharacterEventExplodeHurt item = new TowerDefenseCharacterEventExplodeHurt
		{
			num = (float)num
		};
		Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase> { item };
		TowerDefenseExplode.CreateExplode(pos, new Vector2(3f, 3f), eventList, new Array<TowerDefenseCharacter>(), parent.camp, -1);
		PackedScene packedScene = projectileData?.BuildConfig()?.hitEffect;
		if (GodotObject.IsInstanceValid(packedScene))
		{
			Node node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
			if (node is TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase)
			{
				Vector2I mapGridPosFromMouse = instance.GetMapGridPosFromMouse(pos);
				towerDefenseProjectileEffectBase.Init(mapGridPosFromMouse, parent.camp, projectileData.collisionFlags, null);
			}
			if (node is Node2D node2D && GodotObject.IsInstanceValid(TowerDefenseManager.GetCharacterNode()))
			{
				node2D.GlobalPosition = pos;
				TowerDefenseManager.GetCharacterNode().AddChild(node2D, forceReadableName: false, Node.InternalMode.Disabled);
			}
			else
			{
				node.QueueFree();
			}
		}
	}

	private bool TryFireLineProjectile()
	{
		if (projectileData != null && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance))
		{
			FireComponent fireComponent = projectileLineFireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				ConfigureLineProjectileMarker();
				BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
				{
					gridYOverride = parent.gridPos.Y,
					flipXOverride = (lineProjectileFlipWithParent ? new bool?(parent.Scale.X < 0f) : ((bool?)null))
				};
				projectileLineFireComponent.CreateProjectile(lineFirePositionIndex, lineProjectileVelocity, projectileData, -1, parent.camp, Vector2.Zero, 0, filterByLine: false, overrides);
				return true;
			}
		}
		return false;
	}

	public void AnimeCompleted(string clip)
	{
		if (IsRemoteSyncedClient())
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			if (clip == chargeAnimeClips)
			{
				SendStateEvent(idleStateEvent);
			}
			else if (clip == fireAnimeClips)
			{
				StartRestTimer(Math.Max(0.0, restTime));
				SendStateEvent(restStateEvent);
			}
		}
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!string.IsNullOrEmpty(fireReadyEventName) && command == fireReadyEventName)
		{
			PlayAudio(fireReadyAudioName);
		}
		else if (!(command != fireEventName) && !IsRemoteSyncedClient())
		{
			if (NormalizeMode(mode) == "Line")
			{
				TryFireLineProjectile();
			}
			else
			{
				StartMarkerLaunchVisual();
			}
		}
	}

	private void StartMarkerLaunchVisual()
	{
		if (GodotObject.IsInstanceValid(projectileSlot))
		{
			projectileSlot.Update();
		}
		if (!GodotObject.IsInstanceValid(projectileNode) || !GodotObject.IsInstanceValid(projectileSlot))
		{
			TryFireMarkerProjectile();
			return;
		}
		KillLaunchTween();
		projectileNode.Visible = true;
		projectileNode.GlobalTransform = parent.GetLogicalGlobalTransform(projectileSlot);
		if (markerProjectileStraightMove)
		{
			Vector2 globalPosition = (GodotObject.IsInstanceValid(projectileSlot) ? parent.GetLogicalGlobalPosition(projectileSlot) : parent.GetLogicalGlobalPosition());
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			Vector2I vector2I = (GodotObject.IsInstanceValid(instance) ? instance.GetMapGridPosFromMouse(targetPos) : new Vector2I(0, 0));
			Vector2 vector = new Vector2(targetPos.X, (float)TowerDefenseManager.GetMapLineY(vector2I.Y));
			double num = globalPosition.DistanceTo(vector);
			double num2 = Math.Max(0.05, num / Math.Max(1.0, markerProjectileStraightSpeed));
			projectileNode.GlobalPosition = globalPosition;
			projectileNode.Rotation = 0f;
			projectileNode.GlobalScale = Vector2.One;
			_launchTween = parent.CreateTween();
			_launchTween.SetParallel();
			_launchTween.TweenProperty(projectileNode, "global_position", vector, (float)num2);
			_launchTween.TweenProperty(projectileNode, "rotation", (float)Math.PI * 6f, (float)num2);
			_launchTween.Chain().TweenCallback(Callable.From(CompleteMarkerLaunch));
		}
		else
		{
			_launchTween = parent.CreateTween();
			_launchTween.TweenProperty(projectileNode, "global_position:y", (double)parent.GetLogicalGlobalPosition().Y - Math.Max(0.0, markerVisualTravelHeight), Math.Max(0.0, markerVisualTravelDuration));
			_launchTween.TweenCallback(Callable.From(CompleteMarkerLaunch));
		}
	}

	private void CompleteMarkerLaunch()
	{
		_launchTween = null;
		if (GodotObject.IsInstanceValid(projectileNode))
		{
			projectileNode.Visible = false;
		}
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && base.Alive && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree())
		{
			TryFireMarkerProjectile();
		}
	}

	private void KillLaunchTween()
	{
		if (GodotObject.IsInstanceValid(_launchTween))
		{
			_launchTween.Kill();
		}
		_launchTween = null;
		if (GodotObject.IsInstanceValid(projectileNode))
		{
			projectileNode.Visible = false;
		}
	}

	private void PlayAudio(string audioName)
	{
		if (!string.IsNullOrEmpty(audioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(audioName);
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName != "Rest")
		{
			return;
		}
		_restTimerPaused = false;
		_pausedRestTimerRemaining = 0.0;
		if (IsRemoteSyncedClient() || disableRestTimeout)
		{
			return;
		}
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				_pendingRestTimeout = !SendStateEvent(chargeStateEvent);
				return;
			}
		}
		_pendingRestTimeout = true;
	}

	private void PauseRestTimer()
	{
		if (IsRestTimerRunning())
		{
			_pausedRestTimerRemaining = GetRestTimerRemaining();
			_restTimerRunning = false;
			_restTimerPaused = true;
			_pendingRestTimeout = false;
		}
		else if (!_restTimerPaused && !_pendingRestTimeout)
		{
			StateHandle restState = _restState;
			if (restState != null && restState.IsActive)
			{
				_pendingRestTimeout = true;
			}
		}
	}

	private void ResumeRestTimerOrTimeout()
	{
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !base.IsStateMachineRegistered || !_runtimeInitialized)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized || (GodotObject.IsInstanceValid(parent) && !parent.inGame))
		{
			return;
		}
		if (_pendingRestTimeout)
		{
			_pendingRestTimeout = !SendStateEvent(chargeStateEvent);
		}
		else if (_restTimerPaused)
		{
			double num = Math.Max(0.0, _pausedRestTimerRemaining);
			_restTimerPaused = false;
			_pausedRestTimerRemaining = 0.0;
			if (num <= 0.0)
			{
				_pendingRestTimeout = !SendStateEvent(chargeStateEvent);
			}
			else
			{
				StartRestTimer(num);
			}
		}
	}

	public override Dictionary ExportComponentSave()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "canFire", canFire },
			{ "targetPosX", targetPos.X },
			{ "targetPosY", targetPos.Y },
			{ "restTimerPaused", _restTimerPaused },
			{ "restTimerPausedRemaining", _pausedRestTimerRemaining },
			{ "pendingRestTimeout", _pendingRestTimeout }
		};
		dictionary["timerState"] = ExportRestTimerState();
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		canFire = data.GetValueOrDefault("canFire", true).AsBool();
		targetPos = new Vector2(data.GetValueOrDefault("targetPosX", 0f).AsSingle(), data.GetValueOrDefault("targetPosY", 0f).AsSingle());
		_restTimerPaused = data.GetValueOrDefault("restTimerPaused", false).AsBool();
		_pendingRestTimeout = data.GetValueOrDefault("pendingRestTimeout", false).AsBool();
		bool flag = data.ContainsKey("restTimerPausedRemaining");
		_pausedRestTimerRemaining = data.GetValueOrDefault("restTimerPausedRemaining", 0.0).AsDouble();
		ApplyTimerState(data, owner);
		if (_restTimerPaused || _pendingRestTimeout)
		{
			if (_restTimerPaused && !flag && IsRestTimerRunning())
			{
				_pausedRestTimerRemaining = GetRestTimerRemaining();
			}
			StopRestTimer();
		}
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
		_runtimeStateImported = true;
	}

	public override Dictionary SyncSerialize()
	{
		Dictionary dictionary = new Dictionary
		{
			{ "canFire", canFire },
			{ "targetPosX", targetPos.X },
			{ "targetPosY", targetPos.Y },
			{ "restTimerPaused", _restTimerPaused },
			{ "pendingRestTimeout", _pendingRestTimeout }
		};
		bool flag = IsRestTimerRunning();
		dictionary["restTimerRunning"] = flag;
		dictionary["restTimerRemaining"] = (_restTimerPaused ? Math.Max(0.0, _pausedRestTimerRemaining) : GetRestTimerRemaining());
		string activeStateName = GetActiveStateName();
		if (!string.IsNullOrEmpty(activeStateName))
		{
			dictionary["state"] = activeStateName;
		}
		return dictionary;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		canFire = data.GetValueOrDefault("canFire", canFire).AsBool();
		targetPos = new Vector2(data.GetValueOrDefault("targetPosX", targetPos.X).AsSingle(), data.GetValueOrDefault("targetPosY", targetPos.Y).AsSingle());
		_restTimerPaused = data.GetValueOrDefault("restTimerPaused", _restTimerPaused).AsBool();
		_pendingRestTimeout = data.GetValueOrDefault("pendingRestTimeout", _pendingRestTimeout).AsBool();
		if (_restTimerPaused)
		{
			_pausedRestTimerRemaining = Math.Max(0.0, data.GetValueOrDefault("restTimerRemaining", _pausedRestTimerRemaining).AsDouble());
		}
		ApplyCompactTimerState(data);
		if (base.ShouldApplyLegacyStateField)
		{
			ApplyStateName(data.GetValueOrDefault("state", "").AsString());
		}
		_runtimeStateImported = true;
	}

	private double GetRestTimerRemaining()
	{
		return Math.Max(0.0, _restTimerRemaining);
	}

	private void StartRestTimer(double duration)
	{
		_restTimerRemaining = Math.Max(0.0, duration);
		_restTimerRunning = true;
	}

	private void StopRestTimer()
	{
		_restTimerRunning = false;
		_restTimerRemaining = 0.0;
	}

	private bool IsRestTimerRunning()
	{
		return _restTimerRunning;
	}

	private void TickRestTimer(double delta)
	{
		if (_restTimerRunning && TowerDefenseManager._IsGameRunning())
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			bool flag = GodotObject.IsInstanceValid(instance) && instance.IsIZMMode();
			double num = (GodotObject.IsInstanceValid(parent) ? ((flag ? 1.0 : parent.timeScale) * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0)) : 1.0);
			_restTimerRemaining -= Math.Max(0.0, delta * num);
			if (!(_restTimerRemaining > 0.0))
			{
				_restTimerRunning = false;
				_restTimerRemaining = 0.0;
				Timeout("Rest");
			}
		}
	}

	private void ApplyCompactTimerState(Dictionary data)
	{
		if (data.ContainsKey("restTimerRunning"))
		{
			if (_restTimerPaused || _pendingRestTimeout)
			{
				StopRestTimer();
			}
			else if (data.GetValueOrDefault("restTimerRunning", false).AsBool())
			{
				double duration = Math.Max(0.0, data.GetValueOrDefault("restTimerRemaining", restTime).AsDouble());
				StartRestTimer(duration);
			}
			else
			{
				StopRestTimer();
			}
		}
	}

	private void ApplyTimerState(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (data.ContainsKey("timerState"))
		{
			Dictionary dictionary = data["timerState"].AsGodotDictionary();
			Dictionary dictionary2 = dictionary.GetValueOrDefault("timerRunning", new Dictionary()).AsGodotDictionary();
			Dictionary dictionary3 = dictionary.GetValueOrDefault("timerWaitTime", new Dictionary()).AsGodotDictionary();
			Dictionary dictionary4 = dictionary.GetValueOrDefault("timerCurrent", new Dictionary()).AsGodotDictionary();
			bool flag = dictionary2.GetValueOrDefault("Rest", false).AsBool();
			double num = dictionary3.GetValueOrDefault("Rest", 0.0).AsDouble();
			double num2 = dictionary4.GetValueOrDefault("Rest", 0.0).AsDouble();
			double duration = Math.Max(0.0, num - num2);
			if (flag)
			{
				StartRestTimer(duration);
			}
			else
			{
				StopRestTimer();
			}
		}
	}

	private Dictionary ExportRestTimerState()
	{
		double num = (_restTimerPaused ? Math.Max(0.0, _pausedRestTimerRemaining) : GetRestTimerRemaining());
		Dictionary dictionary = new Dictionary { ["Rest"] = IsRestTimerRunning() };
		Dictionary dictionary2 = new Dictionary { ["Rest"] = num };
		Dictionary dictionary3 = new Dictionary { ["Rest"] = 0.0 };
		return new Dictionary
		{
			["timerRunning"] = dictionary,
			["timerWaitTime"] = dictionary2,
			["timerCurrent"] = dictionary3,
			["timeScale"] = 1.0
		};
	}

	private void ApplyStateName(string stateName)
	{
		if (string.IsNullOrEmpty(stateName))
		{
			return;
		}
		if (base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, stateName))
			{
				_pendingStateName = "";
				return;
			}
		}
		_pendingStateName = stateName;
	}

	private string GetActiveStateName()
	{
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle == null || !stateHandle.IsValid)
		{
			return "";
		}
		return stateHandle.StableId;
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

	private bool IsParentOperational()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance) && parent.inGame && !parent.instance.sleep)
		{
			return parent.componentAlive;
		}
		return false;
	}

	private bool IsRemoteSyncedClient()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	private static string NormalizeMode(string value)
	{
		if (!string.Equals(value, "Line", StringComparison.OrdinalIgnoreCase))
		{
			return "Marker";
		}
		return "Line";
	}
}
