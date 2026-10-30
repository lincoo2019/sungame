using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class BowlingComponent : CharacterComponentRuntime
{
	public delegate void BowlingEventHandler(TowerDefenseCharacter character);

	public delegate void EdgeReboundEventHandler();

	public CharacterMoveComponent moveComponent;

	public double rollXVelocityMax = 250.0;

	public double rollXVelocityMin = 200.0;

	public double rollYVelocity = 250.0;

	public bool selfRotateUse;

	public double selfRotationDivisor = 1000.0;

	public AabbArea2D checkArea;

	public bool useParentHitBox;

	public Array<TowerDefenseCharacterEventBase> hitEvent = new Array<TowerDefenseCharacterEventBase>();

	public int maxHitNum = -1;

	private bool _hitLineUse;

	public bool hitLineBackUse;

	private bool _edgeReboundUse;

	public double offscreenDestroyMargin = 300.0;

	public double topBoundaryOffset = 50.0;

	public double bottomBoundaryOffset = 20.0;

	public int collisionCheckIntervalPhysicsFrames = 1;

	public bool fallCoinUse = true;

	public Array<int> hitCoinRewards = new Array<int> { 0, 0, 0, 10, 10, 10, 20 };

	public AdobeAnimateSprite sprite;

	public string rollAnimeClips = "Roll";

	public double rollAnimeTimeScale = 1.0;

	public string dieAnimeClip = "Death";

	public double rollAnimationStartPosition = 0.2;

	public string rollAudioName = "Bowling";

	public string impactAudioName = "BowlingImpact";

	public StringName rollStateEvent = "ToRoll";

	public StringName idleStateEvent = "ToIdle";

	public Vector2 cameraShakeRange = new Vector2(1f, 1f);

	public double cameraShakeStrength = 2.0;

	public double cameraShakeDuration = 0.05;

	public int cameraShakeFrequency = 4;

	public TowerDefenseCharacter parent;

	public int hitNum;

	public TowerDefenseCharacter hitCharacter;

	public int hitLineSave = -1;

	public List<TowerDefenseCharacter> hitLineCharacterList = new List<TowerDefenseCharacter>();

	public int coinNum;

	public bool isRoll;

	public int _sync_change_dir;

	public bool _sync_deserializing;

	private StateHandle _idleState;

	private StateHandle _rollState;

	private bool _signalsConnected;

	private bool _waitingForParentReady;

	private bool _runtimeInitialized;

	private bool _dying;

	private bool _pendingLegacyRollState;

	private bool _pendingRollRequest;

	private bool _configured;

	private double _groundLeft;

	private double _groundRight;

	private double _topBoundary;

	private double _bottomBoundary;

	private int _gridRowCount;

	private int _collisionFrameOffset;

	private readonly HashSet<TowerDefenseCharacter> _currentOverlapCharacters = new HashSet<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _overlapScratch = new HashSet<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _overlapRemovalBuffer = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _collisionCandidateBuffer = new List<TowerDefenseCharacter>();

	private readonly HashSet<TowerDefenseCharacter> _hitLineCharacters = new HashSet<TowerDefenseCharacter>();

	protected override bool AllowPhysicsOutsideComponentBattlefield => true;

	protected override bool AllowStateMachineOutsideComponentBattlefield => true;

	internal override bool WantsPhysicsProcess => true;

	public bool hitLineUse
	{
		get
		{
			return _hitLineUse;
		}
		set
		{
			if (_hitLineUse != value)
			{
				_hitLineUse = value;
				Refresh();
			}
		}
	}

	public bool edgeReboundUse
	{
		get
		{
			return _edgeReboundUse;
		}
		set
		{
			if (_edgeReboundUse != value)
			{
				_edgeReboundUse = value;
				Refresh();
			}
		}
	}

	public bool alive
	{
		get
		{
			return base.Alive;
		}
		set
		{
			SetAlive(value);
		}
	}

	private BowlingComponentDefinition Definition => base.ComponentDefinition as BowlingComponentDefinition;

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

	public event BowlingEventHandler OnBowling;

	public event EdgeReboundEventHandler OnEdgeRebound;

	protected override void OnBound()
	{
		parent = base.Owner;
		if (GodotObject.IsInstanceValid(parent))
		{
			ApplyDefinitionOnce();
			ResolveReferences();
			_collisionFrameOffset = Math.Abs(parent.randFreshIndex) % Math.Max(1, collisionCheckIntervalPhysicsFrames);
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
		Callable.From(ApplyPendingLegacyRollState).CallDeferred();
	}

	protected override void OnAliveChanged(bool value)
	{
		if (!value)
		{
			isRoll = false;
			ClearCollisionState();
		}
		else if (_runtimeInitialized && GodotObject.IsInstanceValid(parent) && parent.inGame)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance) && instance.IsGameRunning())
			{
				isRoll = true;
			}
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("bowling.idle");
		_rollState = base.StateMachine?.GetStateById("bowling.roll");
		ConnectSignals();
		Callable.From(ApplyPendingLegacyRollState).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingLegacyRollState();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectSignals();
		_idleState = null;
		_rollState = null;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectParentReady();
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted -= OnDieAnimeCompleted;
		}
		ClearCollisionState();
		_runtimeInitialized = false;
		moveComponent = null;
		checkArea = null;
		sprite = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		this.OnBowling = null;
		this.OnEdgeRebound = null;
		ClearCollisionState();
		_hitLineCharacters.Clear();
		hitLineCharacterList.Clear();
		hitEvent.Clear();
		hitCharacter = null;
		moveComponent = null;
		checkArea = null;
		sprite = null;
		parent = null;
	}

	private void ApplyDefinitionOnce()
	{
		if (_configured || Definition == null)
		{
			return;
		}
		BowlingComponentDefinition definition = Definition;
		rollXVelocityMax = definition.rollXVelocityMax;
		rollXVelocityMin = definition.rollXVelocityMin;
		rollYVelocity = definition.rollYVelocity;
		selfRotateUse = definition.selfRotateUse;
		selfRotationDivisor = definition.selfRotationDivisor;
		useParentHitBox = definition.useParentHitBox;
		hitEvent = ((definition.hitEvent != null) ? definition.hitEvent.Duplicate(deep: true) : new Array<TowerDefenseCharacterEventBase>());
		for (int i = 0; i < hitEvent.Count; i++)
		{
			if (hitEvent[i] != null)
			{
				hitEvent[i] = (TowerDefenseCharacterEventBase)hitEvent[i].Duplicate(deep: true);
			}
		}
		maxHitNum = definition.maxHitNum;
		_hitLineUse = definition.hitLineUse;
		hitLineBackUse = definition.hitLineBackUse;
		_edgeReboundUse = definition.edgeReboundUse;
		offscreenDestroyMargin = definition.offscreenDestroyMargin;
		topBoundaryOffset = definition.topBoundaryOffset;
		bottomBoundaryOffset = definition.bottomBoundaryOffset;
		collisionCheckIntervalPhysicsFrames = Math.Max(1, definition.collisionCheckIntervalPhysicsFrames);
		fallCoinUse = definition.fallCoinUse;
		hitCoinRewards = ((definition.hitCoinRewards != null) ? definition.hitCoinRewards.Duplicate(deep: true) : new Array<int>());
		rollAnimeClips = definition.rollAnimeClips ?? "Roll";
		rollAnimeTimeScale = definition.rollAnimeTimeScale;
		dieAnimeClip = definition.dieAnimeClip ?? "Death";
		rollAnimationStartPosition = definition.rollAnimationStartPosition;
		rollAudioName = definition.rollAudioName ?? "Bowling";
		impactAudioName = definition.impactAudioName ?? "BowlingImpact";
		rollStateEvent = definition.rollStateEvent;
		idleStateEvent = definition.idleStateEvent;
		cameraShakeRange = definition.cameraShakeRange;
		cameraShakeStrength = definition.cameraShakeStrength;
		cameraShakeDuration = definition.cameraShakeDuration;
		cameraShakeFrequency = definition.cameraShakeFrequency;
		_configured = true;
	}

	private void ResolveReferences()
	{
		moveComponent = base.Manager?.GetRuntime<CharacterMoveComponent>();
		BowlingComponentDefinition definition = Definition;
		if (GodotObject.IsInstanceValid(parent) && definition != null)
		{
			checkArea = ResolveNode<AabbArea2D>(definition.checkAreaPath);
			sprite = ResolveNode<AdobeAnimateSprite>(definition.spritePath);
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
			StateHandle idleState = _idleState;
			if (idleState != null && idleState.IsValid)
			{
				_idleState.Entered += IdleEntered;
				_idleState.Exited += IdleExited;
				_idleState.PhysicsProcessing += IdleProcessing;
			}
			StateHandle rollState = _rollState;
			if (rollState != null && rollState.IsValid)
			{
				_rollState.Entered += RollEntered;
				_rollState.Exited += RollExited;
				_rollState.PhysicsProcessing += RollProcessing;
			}
			_signalsConnected = true;
		}
	}

	private void DisconnectSignals()
	{
		if (_idleState != null)
		{
			_idleState.Entered -= IdleEntered;
			_idleState.Exited -= IdleExited;
			_idleState.PhysicsProcessing -= IdleProcessing;
		}
		if (_rollState != null)
		{
			_rollState.Entered -= RollEntered;
			_rollState.Exited -= RollExited;
			_rollState.PhysicsProcessing -= RollProcessing;
		}
		_signalsConnected = false;
	}

	private void InitializeRuntime()
	{
		if (!_runtimeInitialized && GodotObject.IsInstanceValid(parent))
		{
			RefreshMapMetrics();
			_runtimeInitialized = true;
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (alive && HasCheckGeometry() && parent.inGame && GodotObject.IsInstanceValid(instance) && instance.IsGameRunning())
			{
				isRoll = true;
			}
			ChangeCheck();
		}
	}

	public void RefreshMapMetrics()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		_groundLeft = instance.GetMapGroundLeft();
		_groundRight = instance.GetMapGroundRight();
		_topBoundary = instance.GetMapGroundUp() + topBoundaryOffset;
		_bottomBoundary = instance.GetMapGroundDown() - bottomBoundaryOffset;
		_gridRowCount = Math.Max(0, Mathf.RoundToInt(instance.GetMapGridNum().Y));
		if (_gridRowCount > 0)
		{
			double mapLineY = TowerDefenseManager.GetMapLineY(1);
			if (double.IsFinite(mapLineY) && mapLineY >= instance.GetMapGroundUp() && mapLineY <= instance.GetMapGroundDown())
			{
				_topBoundary = Math.Min(_topBoundary, mapLineY);
			}
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		ProcessRollingPhysics(physicsFrame);
	}

	private void ProcessRollingPhysics(ulong physicsFrame)
	{
		if (!alive || !isRoll)
		{
			_currentOverlapCharacters.Clear();
		}
		else
		{
			if (IsRemoteClient || !GodotObject.IsInstanceValid(parent))
			{
				return;
			}
			Vector2 globalPositionForPhysicsFrame = parent.GetGlobalPositionForPhysicsFrame(physicsFrame);
			if (!edgeReboundUse)
			{
				if ((double)globalPositionForPhysicsFrame.X > _groundRight + Math.Max(0.0, offscreenDestroyMargin) || (double)globalPositionForPhysicsFrame.X < _groundLeft)
				{
					parent.Destroy();
					return;
				}
			}
			else
			{
				CharacterMoveComponent characterMoveComponent = moveComponent;
				if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
				{
					if ((double)globalPositionForPhysicsFrame.X > _groundRight)
					{
						parent.SetGlobalPositionForPhysicsFrame(new Vector2((float)_groundRight, globalPositionForPhysicsFrame.Y), physicsFrame);
						moveComponent.velocity = new Vector2(0f - moveComponent.velocity.X, moveComponent.velocity.Y);
						this.OnEdgeRebound?.Invoke();
						ChangeCheck();
						return;
					}
					if ((double)globalPositionForPhysicsFrame.X < _groundLeft)
					{
						parent.SetGlobalPositionForPhysicsFrame(new Vector2((float)_groundLeft, globalPositionForPhysicsFrame.Y), physicsFrame);
						moveComponent.velocity = new Vector2(0f - moveComponent.velocity.X, moveComponent.velocity.Y);
						this.OnEdgeRebound?.Invoke();
						ChangeCheck();
						return;
					}
				}
			}
			if (ShouldCheckCollisions())
			{
				ProcessAabbHits();
			}
		}
	}

	private bool ShouldCheckCollisions()
	{
		int num = Math.Max(1, collisionCheckIntervalPhysicsFrames);
		if (num != 1)
		{
			return (ulong)((long)Engine.GetPhysicsFrames() + (long)_collisionFrameOffset) % (ulong)num == 0;
		}
		return true;
	}

	public void Refresh()
	{
		hitLineCharacterList.Clear();
		_hitLineCharacters.Clear();
		ClearCollisionState();
		hitCharacter = null;
		hitLineSave = -1;
	}

	private void ClearCollisionState()
	{
		_currentOverlapCharacters.Clear();
		_overlapScratch.Clear();
		_overlapRemovalBuffer.Clear();
		_collisionCandidateBuffer.Clear();
	}

	public void StartRoll()
	{
		_dying = false;
		SetAlive(alive: true);
		_pendingRollRequest = true;
		if (!TryRequestRollState())
		{
			Callable.From(RequestRollStateDeferred).CallDeferred();
		}
	}

	private void RequestRollStateDeferred()
	{
		TryRequestRollState();
	}

	private bool TryRequestRollState()
	{
		if (!base.IsReleased && base.Lifecycle == ComponentRuntimeLifecycle.Active && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				bool num = SendStateEvent(rollStateEvent);
				if (num)
				{
					_pendingRollRequest = false;
					_pendingLegacyRollState = false;
				}
				return num;
			}
		}
		return false;
	}

	private void ApplyPendingLegacyRollState()
	{
		if ((_pendingLegacyRollState || _pendingRollRequest) && base.IsStateMachineRegistered)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized && TryRequestRollState())
			{
				_pendingLegacyRollState = false;
			}
		}
	}

	public void IdleEntered()
	{
	}

	public void IdleProcessing(double delta)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (alive && GodotObject.IsInstanceValid(instance) && instance.IsGameRunning() && GodotObject.IsInstanceValid(parent) && parent.inGame)
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SendStateEvent(rollStateEvent);
			}
		}
	}

	public void IdleExited()
	{
	}

	public void RollEntered()
	{
		if (alive && GodotObject.IsInstanceValid(parent))
		{
			isRoll = true;
			ChangeCheck();
			PlayAudio(rollAudioName);
			if (GodotObject.IsInstanceValid(sprite) && sprite.HasClip(rollAnimeClips))
			{
				sprite.SetAnimation(rollAnimeClips, loop: true, (float)Math.Max(0.0, rollAnimationStartPosition));
			}
			CharacterMoveComponent characterMoveComponent = moveComponent;
			if (characterMoveComponent != null && !characterMoveComponent.IsReleased && !_sync_deserializing && Mathf.Abs(moveComponent.velocity.X) <= 0.01f)
			{
				double num = Math.Min(rollXVelocityMin, rollXVelocityMax);
				double to = Math.Max(rollXVelocityMin, rollXVelocityMax);
				moveComponent.velocity = new Vector2((float)GD.RandRange(num, to), moveComponent.velocity.Y);
			}
		}
	}

	public void RollProcessing(double delta)
	{
		if (!alive || !isRoll)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(sprite))
		{
			if (sprite.clip == rollAnimeClips)
			{
				sprite.timeScale = rollAnimeTimeScale;
			}
			if (selfRotateUse)
			{
				CharacterMoveComponent characterMoveComponent = moveComponent;
				if (characterMoveComponent != null && !characterMoveComponent.IsReleased && Math.Abs(selfRotationDivisor) > 0.0001)
				{
					sprite.Rotate(moveComponent.velocity.X / (float)selfRotationDivisor);
				}
			}
		}
		if (IsRemoteClient)
		{
			return;
		}
		CharacterMoveComponent characterMoveComponent2 = moveComponent;
		if (characterMoveComponent2 != null && !characterMoveComponent2.IsReleased)
		{
			Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
			if ((double)logicalGlobalPosition.Y < _topBoundary)
			{
				hitLineSave = -1;
				parent.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, (float)_topBoundary));
				ChangeSpeed();
			}
			else if ((double)logicalGlobalPosition.Y > _bottomBoundary)
			{
				hitLineSave = -1;
				parent.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X, (float)_bottomBoundary));
				ChangeSpeed();
			}
		}
	}

	public void RollExited()
	{
	}

	public void HitCheck(AabbArea2D area)
	{
		if (alive && isRoll && !IsRemoteClient && GodotObject.IsInstanceValid(area) && area.GetParent() is TowerDefenseCharacter character)
		{
			HitCheckCharacter(character);
		}
	}

	private bool HitCheckCharacter(TowerDefenseCharacter character)
	{
		if (alive && isRoll && !IsRemoteClient && GodotObject.IsInstanceValid(character) && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.instance))
		{
			TargetRegistrationComponent targetRegistrationComponent = character.targetRegistrationComponent;
			if (targetRegistrationComponent != null && !targetRegistrationComponent.IsReleased && GodotObject.IsInstanceValid(character.instance))
			{
				if (!hitLineUse && hitCharacter == character)
				{
					return true;
				}
				if (!character.IsTargetableFromLine(parent.gridPos.Y))
				{
					return false;
				}
				if (!character.targetRegistrationComponent.canProjectileCheck || !character.instance.canBeCollection)
				{
					return false;
				}
				if (!parent.CanTarget(character) || !parent.CanCollision(character.instance.maskFlags))
				{
					return false;
				}
				if (hitLineUse)
				{
					if (!hitLineBackUse)
					{
						if (_hitLineCharacters.Add(character))
						{
							BowlingHit(character);
							hitLineCharacterList.Add(character);
						}
					}
					else
					{
						BowlingHit(character);
					}
				}
				else if (hitLineSave != character.gridPos.Y)
				{
					BowlingHit(character);
				}
				return true;
			}
		}
		return false;
	}

	public void BowlingHit(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!hitLineUse)
		{
			hitLineSave = character.gridPos.Y;
		}
		hitCharacter = character;
		ChangeSpeed();
		if (IsRemoteClient)
		{
			return;
		}
		hitNum++;
		int currentHitReward = GetCurrentHitReward();
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		if (fallCoinUse)
		{
			coinNum += currentHitReward;
			if (coinNum > 0)
			{
				parent.CoinCreate(logicalGlobalPosition, coinNum, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f));
			}
		}
		PlayAudio(impactAudioName);
		if (GodotObject.IsInstanceValid(ViewManager.Instance))
		{
			Vector2 dir = new Vector2((float)GD.RandRange(0f - Math.Abs(cameraShakeRange.X), Math.Abs(cameraShakeRange.X)), (float)GD.RandRange(0f - Math.Abs(cameraShakeRange.Y), Math.Abs(cameraShakeRange.Y)));
			ViewManager.Instance.CameraShake(dir, (float)Math.Max(0.0, cameraShakeStrength), (float)Math.Max(0.0, cameraShakeDuration), Math.Max(0, cameraShakeFrequency));
		}
		if (hitEvent != null)
		{
			foreach (TowerDefenseCharacterEventBase item in hitEvent)
			{
				item?.Execute(logicalGlobalPosition, character);
			}
		}
		this.OnBowling?.Invoke(character);
		CheckMaxHitDeath();
	}

	private int GetCurrentHitReward()
	{
		int num = hitNum - 1;
		if (hitCoinRewards == null || num < 0 || num >= hitCoinRewards.Count)
		{
			return 0;
		}
		return Math.Max(0, hitCoinRewards[num]);
	}

	private void CheckMaxHitDeath()
	{
		if (maxHitNum > 0 && hitNum >= maxHitNum && !_dying)
		{
			TriggerDeath();
		}
	}

	private void TriggerDeath(bool fromSync = false)
	{
		_dying = true;
		isRoll = false;
		ClearCollisionState();
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.velocity = Vector2.Zero;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			SendStateEvent(idleStateEvent);
		}
		SetAlive(alive: false);
		if (GodotObject.IsInstanceValid(sprite) && sprite.HasClip(dieAnimeClip))
		{
			if (!fromSync)
			{
				sprite.OnAnimeCompleted -= OnDieAnimeCompleted;
				sprite.OnAnimeCompleted += OnDieAnimeCompleted;
			}
			sprite.SetAnimation(dieAnimeClip, loop: false);
		}
		else if (!fromSync && GodotObject.IsInstanceValid(parent))
		{
			parent.Destroy();
		}
	}

	private void OnDieAnimeCompleted(string clip)
	{
		if (!(clip != dieAnimeClip))
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.OnAnimeCompleted -= OnDieAnimeCompleted;
			}
			if (GodotObject.IsInstanceValid(parent))
			{
				parent.Destroy();
			}
		}
	}

	public void ChangeSpeed()
	{
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent == null || characterMoveComponent.IsReleased)
		{
			return;
		}
		if (hitLineUse)
		{
			if (hitLineBackUse)
			{
				moveComponent.velocity = new Vector2(0f - moveComponent.velocity.X, moveComponent.velocity.Y);
			}
		}
		else if (hitLineSave == 1)
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, (float)rollYVelocity);
		}
		else if (_gridRowCount > 0 && hitLineSave == _gridRowCount)
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, (float)(0.0 - rollYVelocity));
		}
		else if (Mathf.Abs(moveComponent.velocity.Y) > 0.01f)
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, 0f - moveComponent.velocity.Y);
		}
		else if (_sync_deserializing && _sync_change_dir != 0)
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, (float)(rollYVelocity * (double)_sync_change_dir));
			_sync_deserializing = false;
		}
		else if (GD.Randf() > 0.5f)
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, (float)rollYVelocity);
			_sync_change_dir = 1;
		}
		else
		{
			moveComponent.velocity = new Vector2(moveComponent.velocity.X, (float)(0.0 - rollYVelocity));
			_sync_change_dir = -1;
		}
	}

	public void ChangeCheck()
	{
		ProcessAabbHits(processAllCurrent: true);
	}

	private void ProcessAabbHits(bool processAllCurrent = false)
	{
		if (!alive || !isRoll || IsRemoteClient || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!TryGetCheckRect(out var rect))
		{
			_currentOverlapCharacters.Clear();
			return;
		}
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance) || instance.characterRegistry == null)
		{
			return;
		}
		instance.characterRegistry.FillCharactersIntersectingRectDirectListExcludingCamp(rect, parent.camp, _collisionCandidateBuffer, parent.gridPos.Y, includeAllLineCheck: true);
		_overlapScratch.Clear();
		for (int i = 0; i < _collisionCandidateBuffer.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _collisionCandidateBuffer[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && (processAllCurrent || !_currentOverlapCharacters.Contains(towerDefenseCharacter)) && HitCheckCharacter(towerDefenseCharacter))
			{
				_overlapScratch.Add(towerDefenseCharacter);
			}
		}
		_overlapRemovalBuffer.Clear();
		foreach (TowerDefenseCharacter currentOverlapCharacter in _currentOverlapCharacters)
		{
			if (!GodotObject.IsInstanceValid(currentOverlapCharacter) || !_overlapScratch.Contains(currentOverlapCharacter))
			{
				_overlapRemovalBuffer.Add(currentOverlapCharacter);
			}
		}
		for (int j = 0; j < _overlapRemovalBuffer.Count; j++)
		{
			_currentOverlapCharacters.Remove(_overlapRemovalBuffer[j]);
		}
		foreach (TowerDefenseCharacter item in _overlapScratch)
		{
			_currentOverlapCharacters.Add(item);
		}
	}

	private bool HasCheckGeometry()
	{
		if (!GodotObject.IsInstanceValid(checkArea))
		{
			if (useParentHitBox && GodotObject.IsInstanceValid(parent))
			{
				return parent.HasHitBox;
			}
			return false;
		}
		return true;
	}

	private bool TryGetCheckRect(out Rect2 rect)
	{
		if (GodotObject.IsInstanceValid(checkArea))
		{
			if (checkArea.ProcessMode == Node.ProcessModeEnum.Disabled)
			{
				rect = default(Rect2);
				return false;
			}
			rect = ProjectileZoneHelper.ComputeAreaWorldRect(checkArea);
			return true;
		}
		if (useParentHitBox && GodotObject.IsInstanceValid(parent) && parent.TryGetActiveWorldHitRect(out rect))
		{
			return true;
		}
		rect = default(Rect2);
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		CharacterMoveComponent characterMoveComponent = moveComponent;
		Vector2 vector = ((characterMoveComponent != null && !characterMoveComponent.IsReleased) ? moveComponent.velocity : Vector2.Zero);
		return new Dictionary
		{
			{ "isRoll", isRoll },
			{ "hitNum", hitNum },
			{ "hitLineSave", hitLineSave },
			{ "coinNum", coinNum },
			{ "change_dir", _sync_change_dir },
			{ "velocity_x", vector.X },
			{ "velocity_y", vector.Y },
			{ "alive", alive },
			{ "dying", _dying }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		ApplyRuntimeState(data);
		if (base.ShouldApplyLegacyStateField && isRoll)
		{
			_pendingLegacyRollState = true;
			ApplyPendingLegacyRollState();
		}
	}

	public override Dictionary SyncSerialize()
	{
		return ExportComponentSave();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		bool flag = isRoll;
		bool dying = _dying;
		ApplyLegacyConfiguration(data);
		ApplyRuntimeState(data);
		if (base.ShouldApplyLegacyStateField && isRoll && !flag)
		{
			try
			{
				_sync_deserializing = true;
				_pendingLegacyRollState = true;
				ApplyPendingLegacyRollState();
			}
			finally
			{
				_sync_deserializing = false;
			}
		}
		if (_dying && !dying)
		{
			TriggerDeath(fromSync: true);
		}
	}

	private void ApplyRuntimeState(Dictionary data)
	{
		isRoll = data.GetValueOrDefault("isRoll", isRoll).AsBool();
		hitNum = data.GetValueOrDefault("hitNum", hitNum).AsInt32();
		hitLineSave = data.GetValueOrDefault("hitLineSave", hitLineSave).AsInt32();
		coinNum = data.GetValueOrDefault("coinNum", coinNum).AsInt32();
		_sync_change_dir = data.GetValueOrDefault("change_dir", _sync_change_dir).AsInt32();
		_dying = data.GetValueOrDefault("dying", _dying).AsBool();
		SetAlive(data.GetValueOrDefault("alive", alive).AsBool());
		CharacterMoveComponent characterMoveComponent = moveComponent;
		if (characterMoveComponent != null && !characterMoveComponent.IsReleased)
		{
			moveComponent.velocity = new Vector2(data.GetValueOrDefault("velocity_x", moveComponent.velocity.X).AsSingle(), data.GetValueOrDefault("velocity_y", moveComponent.velocity.Y).AsSingle());
		}
	}

	private void ApplyLegacyConfiguration(Dictionary data)
	{
		if (data.ContainsKey("edgeReboundUse"))
		{
			edgeReboundUse = data["edgeReboundUse"].AsBool();
		}
		if (data.ContainsKey("hitLineUse"))
		{
			hitLineUse = data["hitLineUse"].AsBool();
		}
		if (data.ContainsKey("hitLineBackUse"))
		{
			hitLineBackUse = data["hitLineBackUse"].AsBool();
		}
		if (data.ContainsKey("rollXVelocityMax"))
		{
			rollXVelocityMax = data["rollXVelocityMax"].AsDouble();
		}
		if (data.ContainsKey("rollXVelocityMin"))
		{
			rollXVelocityMin = data["rollXVelocityMin"].AsDouble();
		}
	}

	private static void PlayAudio(string audioName)
	{
		if (!string.IsNullOrEmpty(audioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(audioName);
		}
	}
}
