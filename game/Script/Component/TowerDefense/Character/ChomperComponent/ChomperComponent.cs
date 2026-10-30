using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed class ChomperComponent : CharacterComponentRuntime
{
	public delegate void BiteStartEventHandler();

	public delegate void BiteFailEventHandler();

	public delegate void ChewProcessingEventHandler(double delta);

	public delegate void ChewBeginEventHandler();

	public delegate void ChewOverEventHandler();

	public delegate void SwallowOverEventHandler();

	private StateHandle _idleState;

	private StateHandle _attackState;

	private StateHandle _chewState;

	private StateHandle _swallowState;

	private bool _stateSignalsConnected;

	private bool _spriteSignalsConnected;

	private bool _configured;

	private string _pendingSyncedState;

	private TowerDefenseCharacter _suckedTargetInteractionTarget;

	private AabbShape2DResource _suckShape;

	private const float BiteForwardSideTolerance = 4f;

	public TowerDefenseCharacter parent;

	public AttackComponent attackComponent;

	public float chewTime = 30f;

	public float chewTimePercentage = -1f;

	public float biteAttack = -1f;

	public Array<TowerDefenseCharacterEventBase> biteEvent = new Array<TowerDefenseCharacterEventBase>();

	public bool biteOnly;

	public bool biteNoLimit;

	public bool ignoreBiteHurt;

	public float healthPercentage = -1f;

	public AdobeAnimateSprite sprite;

	public string biteEventName = "attack";

	public string biteStartAnimeClips = "Bite";

	public float biteStartAnimeTimeScale = 1.5f;

	public string biteLoopAnimeClips = "BiteLoop";

	public float biteLoopAnimeTimeScale = 1.5f;

	public string biteEndAnimeClips = "BiteEnd";

	public float biteEndAnimeTimeScale = 1.5f;

	public string chewReadyAnimeClips = "";

	public float chewReadyAnimeTimeScale = 1f;

	public string chewAnimeClips = "Chew";

	public float chewAnimeTimeScale = 1f;

	public string swallowAnimeClips = "Swallow";

	public float swallowAnimeTimeScale = 1f;

	public bool isPartSprite;

	public string partIdleAnimeClips = "Idle";

	public float partIdleAnimeTimeScale = 1.5f;

	public bool playBiteAudio = true;

	public string biteAudioName = "BigChomp";

	public StringName attackStateEvent = "ToAttack";

	public StringName chewStateEvent = "ToChew";

	public StringName swallowStateEvent = "ToSwallow";

	public StringName idleStateEvent = "ToIdle";

	public bool suckUse;

	public TowerDefenseCharacter target;

	public float chewTimer;

	public bool isSuck;

	public bool isChew;

	public bool eatCharacter;

	public bool canEnterChew = true;

	public float healthNum;

	public float currentChewTime;

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

	private ChomperComponentDefinition Definition => base.ComponentDefinition as ChomperComponentDefinition;

	public event BiteStartEventHandler OnBiteStart;

	public event BiteFailEventHandler OnBiteFail;

	public event ChewProcessingEventHandler OnChewProcessing;

	public event ChewBeginEventHandler OnChewBegin;

	public event ChewOverEventHandler OnChewOver;

	public event SwallowOverEventHandler OnSwallowOver;

	protected override bool CanSendStateEventOutsideComponentBattlefield(StringName eventName)
	{
		if (!(parent is TowerDefenseZombie) || !(eventName == idleStateEvent) || !IsInterruptibleActionState(base.StateMachine?.CurrentStateHandle))
		{
			return base.CanSendStateEventOutsideComponentBattlefield(eventName);
		}
		return true;
	}

	protected override void OnBound()
	{
		parent = base.Owner;
		ApplyDefinitionOnce();
		ResolveReferences();
		ConnectSpriteSignals();
	}

	protected override void OnActivated()
	{
		ResolveReferences();
		ConnectSpriteSignals();
		ApplyPendingSyncedState();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectSpriteSignals();
		ReleaseSuckedTarget();
		attackComponent = null;
		sprite = null;
		parent = null;
	}

	protected override void OnReleased()
	{
		DisconnectSpriteSignals();
		ReleaseSuckedTarget();
		this.OnBiteStart = null;
		this.OnBiteFail = null;
		this.OnChewProcessing = null;
		this.OnChewBegin = null;
		this.OnChewOver = null;
		this.OnSwallowOver = null;
		biteEvent?.Clear();
		_suckShape = null;
		attackComponent = null;
		sprite = null;
		parent = null;
	}

	protected override void OnAliveChanged(bool value)
	{
		if (!value)
		{
			ReleaseSuckedTarget();
		}
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			ChomperComponentDefinition definition = Definition;
			chewTime = definition.chewTime;
			chewTimePercentage = definition.chewTimePercentage;
			biteAttack = definition.biteAttack;
			biteEvent = ((definition.biteEvent != null) ? definition.biteEvent.Duplicate(deep: true) : new Array<TowerDefenseCharacterEventBase>());
			biteOnly = definition.biteOnly;
			biteNoLimit = definition.biteNoLimit;
			ignoreBiteHurt = definition.ignoreBiteHurt;
			healthPercentage = definition.healthPercentage;
			biteEventName = definition.biteEventName;
			biteStartAnimeClips = definition.biteStartAnimeClips;
			biteStartAnimeTimeScale = definition.biteStartAnimeTimeScale;
			biteLoopAnimeClips = definition.biteLoopAnimeClips;
			biteLoopAnimeTimeScale = definition.biteLoopAnimeTimeScale;
			biteEndAnimeClips = definition.biteEndAnimeClips;
			biteEndAnimeTimeScale = definition.biteEndAnimeTimeScale;
			chewReadyAnimeClips = definition.chewReadyAnimeClips;
			chewReadyAnimeTimeScale = definition.chewReadyAnimeTimeScale;
			chewAnimeClips = definition.chewAnimeClips;
			chewAnimeTimeScale = definition.chewAnimeTimeScale;
			swallowAnimeClips = definition.swallowAnimeClips;
			swallowAnimeTimeScale = definition.swallowAnimeTimeScale;
			isPartSprite = definition.isPartSprite;
			partIdleAnimeClips = definition.partIdleAnimeClips;
			partIdleAnimeTimeScale = definition.partIdleAnimeTimeScale;
			playBiteAudio = definition.playBiteAudio;
			biteAudioName = definition.biteAudioName;
			attackStateEvent = definition.attackStateEvent;
			chewStateEvent = definition.chewStateEvent;
			swallowStateEvent = definition.swallowStateEvent;
			idleStateEvent = definition.idleStateEvent;
			suckUse = definition.suckUse;
			_suckShape = definition.suckShape;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		if (GodotObject.IsInstanceValid(parent) && Definition != null)
		{
			string text = Definition.attackComponentPath?.ToString() ?? string.Empty;
			int num = text.LastIndexOf('/');
			string text2;
			if (num < 0)
			{
				text2 = text;
			}
			else
			{
				string text3 = text;
				int num2 = num + 1;
				text2 = text3.Substring(num2, text3.Length - num2);
			}
			string text4 = text2;
			attackComponent = ((!string.IsNullOrEmpty(text4) && base.Manager != null && base.Manager.TryGetRuntimeByLegacyNodeName(text4, out var runtime)) ? (runtime as AttackComponent) : null);
			sprite = ResolveOwnerNode<AdobeAnimateSprite>(Definition.spritePath);
		}
	}

	private T ResolveOwnerNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(parent))
		{
			return parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	private void ConnectSpriteSignals()
	{
		if (!_spriteSignalsConnected && GodotObject.IsInstanceValid(sprite))
		{
			sprite.OnAnimeCompleted += AnimeCompleted;
			sprite.OnAnimeEvent += AnimeEvent;
			_spriteSignalsConnected = true;
		}
	}

	private void DisconnectSpriteSignals()
	{
		if (_spriteSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.OnAnimeCompleted -= AnimeCompleted;
				sprite.OnAnimeEvent -= AnimeEvent;
			}
			_spriteSignalsConnected = false;
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_idleState = base.StateMachine?.GetStateById("chomper.idle");
		_attackState = base.StateMachine?.GetStateById("chomper.attack");
		_chewState = base.StateMachine?.GetStateById("chomper.chew");
		_swallowState = base.StateMachine?.GetStateById("chomper.swallow");
		ConnectStateSignals();
		Callable.From(ApplyPendingSyncedState).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		ApplyPendingSyncedState();
	}

	protected override void OnStateRuntimeDetaching()
	{
		ReleaseSuckedTarget();
		DisconnectStateSignals();
		_idleState = null;
		_attackState = null;
		_chewState = null;
		_swallowState = null;
	}

	protected override void OnAuthoritativeStateRestorePreparing(bool remote)
	{
		_pendingSyncedState = null;
	}

	protected override void OnAuthoritativeStateRestored(StateMachineSnapshot snapshot, bool remote)
	{
		string text = base.StateMachine?.CurrentStateHandle?.StableId ?? string.Empty;
		switch (text)
		{
		case "chomper.idle":
			isChew = false;
			isSuck = false;
			break;
		case "chomper.attack":
			isChew = false;
			break;
		case "chomper.chew":
			isChew = true;
			isSuck = false;
			break;
		case "chomper.swallow":
			isChew = false;
			isSuck = false;
			break;
		}
		int num;
		object obj;
		if (text == "chomper.attack" && suckUse && isSuck)
		{
			num = (GodotObject.IsInstanceValid(target) ? 1 : 0);
			if (num != 0)
			{
				obj = target;
				goto IL_00cc;
			}
		}
		else
		{
			num = 0;
		}
		obj = null;
		goto IL_00cc;
		IL_00cc:
		TowerDefenseCharacter towerDefenseCharacter = (TowerDefenseCharacter)obj;
		if (_suckedTargetInteractionTarget != towerDefenseCharacter)
		{
			RestoreSuckedTargetInteraction();
		}
		if (num != 0)
		{
			SetSuckedTargetInteraction(target, locked: true);
		}
		RestoreAuthoritativeAnimation(text, remote);
	}

	private void RestoreAuthoritativeAnimation(string stateId, bool remote)
	{
		if (!remote || !GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		string text;
		switch (stateId)
		{
		case "chomper.idle":
			if (isPartSprite)
			{
				text = partIdleAnimeClips;
				break;
			}
			goto default;
		case "chomper.attack":
			text = ((!isSuck) ? biteStartAnimeClips : biteLoopAnimeClips);
			break;
		case "chomper.chew":
			text = chewAnimeClips;
			break;
		case "chomper.swallow":
			text = swallowAnimeClips;
			break;
		default:
			text = string.Empty;
			break;
		}
		string text2 = text;
		if (!string.IsNullOrEmpty(text2) && !(sprite.clip == text2) && sprite.HasClip(text2))
		{
			int num;
			switch (stateId)
			{
			case "chomper.attack":
				num = (isSuck ? 1 : 0);
				break;
			default:
				num = 0;
				break;
			case "chomper.idle":
			case "chomper.chew":
				num = 1;
				break;
			}
			bool loop = (byte)num != 0;
			sprite.SetAnimation(text2, loop);
		}
	}

	private void ReleaseSuckedTarget()
	{
		TowerDefenseCharacter towerDefenseCharacter = target;
		bool num = suckUse && (isSuck || _suckedTargetInteractionTarget == towerDefenseCharacter);
		target = null;
		isSuck = false;
		if (num && GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			SetSuckedTargetInteraction(towerDefenseCharacter, locked: false);
			if (towerDefenseCharacter is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.Walk();
			}
		}
	}

	private void SetSuckedTargetInteraction(TowerDefenseCharacter character, bool locked)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			if (GodotObject.IsInstanceValid(character.instance))
			{
				character.instance.canCollection = !locked;
			}
			IStateMachineController stateMachine = character.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				character.SetMainStateMachineDispatchEnabled(!locked);
			}
			_suckedTargetInteractionTarget = (locked ? character : null);
		}
	}

	private void RestoreSuckedTargetInteraction()
	{
		TowerDefenseCharacter suckedTargetInteractionTarget = _suckedTargetInteractionTarget;
		_suckedTargetInteractionTarget = null;
		if (GodotObject.IsInstanceValid(suckedTargetInteractionTarget))
		{
			if (GodotObject.IsInstanceValid(suckedTargetInteractionTarget.instance))
			{
				suckedTargetInteractionTarget.instance.canCollection = true;
			}
			IStateMachineController stateMachine = suckedTargetInteractionTarget.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				suckedTargetInteractionTarget.SetMainStateMachineDispatchEnabled(enabled: true);
			}
		}
	}

	public bool TryRecoverOffFieldZombieMovement()
	{
		if (base.IsInsideComponentBattlefield || !(parent is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie.CurrentStateHandle?.StableId != "character.component" || !IsInterruptibleActionState(base.StateMachine?.CurrentStateHandle))
		{
			return false;
		}
		if (!SendStateEvent(idleStateEvent))
		{
			return false;
		}
		towerDefenseZombie.Walk();
		return true;
	}

	private static bool IsInterruptibleActionState(StateHandle state)
	{
		bool flag = state?.IsValid ?? false;
		if (flag)
		{
			bool flag2;
			switch (state.StableId)
			{
			case "chomper.attack":
			case "chomper.chew":
			case "chomper.swallow":
				flag2 = true;
				break;
			default:
				flag2 = false;
				break;
			}
			flag = flag2;
		}
		return flag;
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			ConnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			ConnectState(_attackState, AttackEntered, AttackExited, AttackProcessing);
			ConnectState(_chewState, ChewEntered, ChewExited, ChewProcessing);
			ConnectState(_swallowState, SwallowEntered, SwallowExited, SwallowProcessing);
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			DisconnectState(_idleState, IdleEntered, IdleExited, IdleProcessing);
			DisconnectState(_attackState, AttackEntered, AttackExited, AttackProcessing);
			DisconnectState(_chewState, ChewEntered, ChewExited, ChewProcessing);
			DisconnectState(_swallowState, SwallowEntered, SwallowExited, SwallowProcessing);
			_stateSignalsConnected = false;
		}
	}

	private static void ConnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null && node.IsValid)
		{
			node.Entered += entered;
			node.Exited += exited;
			node.PhysicsProcessing += processing;
		}
	}

	private static void DisconnectState(StateHandle node, Action entered, Action exited, Action<double> processing)
	{
		if (node != null)
		{
			node.Entered -= entered;
			node.Exited -= exited;
			node.PhysicsProcessing -= processing;
		}
	}

	public void IdleEntered()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (parent.componentRunning)
		{
			if (parent is TowerDefensePlant towerDefensePlant)
			{
				towerDefensePlant.Idle();
			}
			else if (parent is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.Walk();
			}
		}
		if (isPartSprite)
		{
			sprite.SetAnimation(partIdleAnimeClips, loop: true, 0.2);
		}
	}

	public void IdleProcessing(double delta)
	{
		if (!base.Alive || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		AttackComponent obj = attackComponent;
		if (obj == null || obj.IsReleased)
		{
			return;
		}
		if (isPartSprite)
		{
			sprite.timeScale = parent.timeScale * (double)partIdleAnimeTimeScale;
		}
		if (!TowerDefenseManager.Instance.IsGameRunning() || !parent.inGame || !parent.componentAlive || parent.componentRunning)
		{
			return;
		}
		if (attackComponent.CanAttack())
		{
			if (attackComponent.target is TowerDefenseItemBrain)
			{
				attackComponent.target = null;
			}
			else if (!IsTargetInBiteForwardSide(attackComponent.target))
			{
				attackComponent.target = null;
			}
			else
			{
				if (!(attackComponent.target is TowerDefenseZombie towerDefenseZombie) || towerDefenseZombie.instance.zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
				{
					isSuck = false;
					parent.Component();
					SendStateEvent(attackStateEvent);
					return;
				}
				attackComponent.target = null;
			}
		}
		if (suckUse)
		{
			GetTarget();
			if (GodotObject.IsInstanceValid(target))
			{
				isSuck = true;
				parent.Component();
				SendStateEvent(attackStateEvent);
			}
		}
	}

	public void IdleExited()
	{
	}

	public void AttackEntered()
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		this.OnBiteStart?.Invoke();
		sprite.SetAnimation(biteStartAnimeClips, loop: false, 0.5);
		if (!isSuck)
		{
			target = attackComponent.target;
		}
		if (suckUse)
		{
			if (isSuck)
			{
				sprite.AddAnimation(biteLoopAnimeClips, 0.0, loop: true, 0.2);
			}
			else
			{
				sprite.AddAnimation(biteEndAnimeClips, 0.0, loop: false, 0.2);
			}
		}
	}

	public void AttackProcessing(double delta)
	{
		if (InterruptWhenParentCannotRunComponents() || !GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		double num = parent.buff?.GetAttackSpeedMultiplier() ?? 1.0;
		string clip = sprite.clip;
		if (clip == biteStartAnimeClips)
		{
			sprite.timeScale = parent.timeScale * num * (double)biteStartAnimeTimeScale;
		}
		else if (clip == biteLoopAnimeClips)
		{
			sprite.timeScale = parent.timeScale * num * (double)biteLoopAnimeTimeScale;
		}
		else if (clip == biteEndAnimeClips)
		{
			sprite.timeScale = parent.timeScale * num * (double)biteEndAnimeTimeScale;
		}
		if (!suckUse || !isSuck || !(sprite.clip == biteLoopAnimeClips))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(target))
		{
			ReleaseSuckedTarget();
			SendStateEvent(idleStateEvent);
		}
		else if (target.die || target.nearDie)
		{
			ReleaseSuckedTarget();
			SendStateEvent(idleStateEvent);
		}
		else if (target.gridPos.Y != parent.gridPos.Y)
		{
			ReleaseSuckedTarget();
			SendStateEvent(idleStateEvent);
		}
		else if (target is TowerDefenseZombie { isPause: not false })
		{
			ReleaseSuckedTarget();
			SendStateEvent(idleStateEvent);
		}
		else if (parent.CanTarget(target) && parent.CanCollision(target.instance.maskFlags))
		{
			Vector2 logicalGlobalPosition = target.GetLogicalGlobalPosition();
			if (logicalGlobalPosition.X > parent.GetLogicalGlobalPosition().X + 80f)
			{
				SetSuckedTargetInteraction(target, locked: true);
				target.SetLogicalGlobalPosition(new Vector2(logicalGlobalPosition.X - 200f * (float)(delta * num), logicalGlobalPosition.Y));
			}
			else if (!string.IsNullOrEmpty(biteEndAnimeClips))
			{
				sprite.SetAnimation(biteEndAnimeClips, loop: false, 0.2);
			}
		}
		else
		{
			ReleaseSuckedTarget();
			SendStateEvent(idleStateEvent);
		}
	}

	public void AttackExited()
	{
		ReleaseSuckedTarget();
		eatCharacter = false;
	}

	public void ChewEntered()
	{
		this.OnChewBegin?.Invoke();
		chewTimer = 0f;
		isChew = true;
		if (GodotObject.IsInstanceValid(sprite))
		{
			if (chewReadyAnimeClips != "")
			{
				sprite.SetAnimation(chewReadyAnimeClips, loop: false, 0.2);
				sprite.AddAnimation(chewAnimeClips, 0.0, loop: true, 0.2);
			}
			else
			{
				sprite.SetAnimation(chewAnimeClips, loop: true, 0.2);
			}
		}
	}

	public void ChewProcessing(double delta)
	{
		if (InterruptWhenParentCannotRunComponents() || !base.Alive || !GodotObject.IsInstanceValid(sprite))
		{
			return;
		}
		double num = parent.buff?.GetAttackSpeedMultiplier() ?? 1.0;
		sprite.timeScale = parent.timeScale * num * (double)chewAnimeTimeScale;
		double num2 = delta * num;
		this.OnChewProcessing?.Invoke(num2);
		if (sprite.clip == chewAnimeClips)
		{
			if (chewTimer < currentChewTime)
			{
				chewTimer += (float)num2;
				return;
			}
			this.OnChewOver?.Invoke();
			SendStateEvent(swallowStateEvent);
		}
	}

	public void ChewExited()
	{
		isChew = false;
	}

	public void SwallowEntered()
	{
		if (GodotObject.IsInstanceValid(sprite))
		{
			sprite.SetAnimation(swallowAnimeClips, loop: false, 0.2);
		}
	}

	public void SwallowProcessing(double delta)
	{
		if (!InterruptWhenParentCannotRunComponents() && GodotObject.IsInstanceValid(sprite))
		{
			sprite.timeScale = parent.timeScale * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0) * (double)swallowAnimeTimeScale;
		}
	}

	private bool InterruptWhenParentCannotRunComponents()
	{
		if (!GodotObject.IsInstanceValid(parent) || parent.componentAlive)
		{
			return false;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			SendStateEvent(idleStateEvent);
		}
		return true;
	}

	public void SwallowExited()
	{
		if (healthNum > 0f)
		{
			parent.Health(healthNum);
			if (parent.instance.hitpoints >= parent.instance.hitpointsSave)
			{
				parent.instance.hitpoints = parent.instance.hitpointsSave;
			}
		}
	}

	public void AnimeCompleted(string clip)
	{
		if (!base.Alive)
		{
			return;
		}
		if (clip == biteStartAnimeClips)
		{
			if (suckUse)
			{
				return;
			}
			if (eatCharacter)
			{
				if (canEnterChew)
				{
					SendStateEvent(chewStateEvent);
				}
				else
				{
					SendStateEvent(idleStateEvent);
				}
			}
			else
			{
				this.OnBiteFail?.Invoke();
				SendStateEvent(idleStateEvent);
			}
		}
		else if (clip == biteEndAnimeClips)
		{
			if (eatCharacter)
			{
				if (canEnterChew)
				{
					SendStateEvent(chewStateEvent);
				}
				else
				{
					SendStateEvent(idleStateEvent);
				}
			}
			else
			{
				this.OnBiteFail?.Invoke();
				SendStateEvent(idleStateEvent);
			}
		}
		else if (clip == swallowAnimeClips)
		{
			SendStateEvent(idleStateEvent);
			this.OnSwallowOver?.Invoke();
		}
	}

	public void AnimeEvent(string command, Variant argument)
	{
		if (!base.Alive || !(command == biteEventName))
		{
			return;
		}
		if (playBiteAudio && !string.IsNullOrEmpty(biteAudioName) && GodotObject.IsInstanceValid(AudioManager.Instance))
		{
			AudioManager.Instance.AudioPlay(biteAudioName);
		}
		if (suckUse && isSuck)
		{
			if (GodotObject.IsInstanceValid(target))
			{
				BitCharacter(target);
			}
			else
			{
				this.OnBiteFail?.Invoke();
			}
			return;
		}
		TowerDefenseCharacter attackTarget = target;
		if (IsAttackTargetStillReachable(attackTarget))
		{
			BitCharacter(attackTarget);
		}
		else
		{
			this.OnBiteFail?.Invoke();
		}
	}

	private bool IsAttackTargetStillReachable(TowerDefenseCharacter attackTarget)
	{
		if (GodotObject.IsInstanceValid(attackTarget))
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased)
			{
				if (!IsTargetInBiteForwardSide(attackTarget))
				{
					return false;
				}
				return attackComponent.IsCurrentTargetReachable(attackTarget);
			}
		}
		return false;
	}

	public void BitCharacter(TowerDefenseCharacter _target)
	{
		canEnterChew = true;
		if (_target.die || _target.nearDie)
		{
			return;
		}
		if (!IsTargetInBiteForwardSide(_target))
		{
			this.OnBiteFail?.Invoke();
		}
		else
		{
			if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && _target is TowerDefenseZombie)
			{
				return;
			}
			if (_target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
			{
				this.OnBiteFail?.Invoke();
				return;
			}
			if ((parent.instance.physiqueTypeFlags & 0x10) != 0 && _target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.CAR)
			{
				canEnterChew = false;
				if (parent.CanTarget(_target) && parent.CanCollision(_target.instance.maskFlags))
				{
					BiteEventExecute(_target);
					if (_target is TowerDefenseZombie towerDefenseZombie)
					{
						towerDefenseZombie.Die();
					}
				}
				return;
			}
			bool flag = false;
			bool num = ignoreBiteHurt || _target.instance.biteHurt == -1.0;
			float num2 = chewTime;
			float num3 = 0f;
			if (parent.CanTarget(_target) && parent.CanCollision(_target.instance.maskFlags))
			{
				if (chewTimePercentage >= 0f && chewTimePercentage <= 1f)
				{
					num2 = (float)(_target.GetCurrentHitPoint() * (double)chewTimePercentage);
					if (num2 > chewTime)
					{
						num2 = chewTime;
					}
				}
				if (healthPercentage >= 0f && healthPercentage <= 1f)
				{
					num3 = (float)(_target.GetCurrentHitPoint() * (double)healthPercentage);
				}
				BiteEventExecute(_target);
				double num4 = ((biteAttack == -1f && (biteNoLimit || !ignoreBiteHurt)) ? _target.instance.biteHurt : (-1.0));
				if (biteNoLimit)
				{
					if (_target is TowerDefenseZombie && _target.instance.biteHurt == -1.0 && !biteOnly)
					{
						flag = true;
					}
					else
					{
						double num5 = ((biteAttack == -1f) ? _target.instance.biteHurt : ((double)biteAttack));
						_target.AttackDeal(parent, attackComponent.attackType, num5);
						_target.Hurt((num4 >= 0.0) ? 100000.0 : num5, playSplatAudio: true, Vector2.Zero, createDamagePart: false, num4);
					}
				}
				else
				{
					double num5;
					if (ignoreBiteHurt || _target.instance.biteHurt == -1.0)
					{
						if (_target.instance.zombiePhysique == TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS)
						{
							num5 = ((biteAttack == -1f) ? _target.instance.biteHurt : ((double)biteAttack));
						}
						else
						{
							if (biteAttack == -1f)
							{
								_target.instance.ArmorClear();
							}
							num5 = ((biteAttack == -1f) ? 100000.0 : ((double)biteAttack));
						}
					}
					else
					{
						num5 = ((biteAttack == -1f) ? _target.instance.biteHurt : ((double)biteAttack));
					}
					_target.AttackDeal(parent, attackComponent.attackType, num5);
					_target.Hurt((num4 >= 0.0) ? 100000.0 : num5, playSplatAudio: true, Vector2.Zero, createDamagePart: false, num4);
				}
			}
			if (num && (_target.die || _target.nearDie))
			{
				flag = true;
			}
			if (!biteOnly && flag)
			{
				currentChewTime = num2;
				healthNum = num3;
				_target.isChomp = true;
				_target.Destroy();
				eatCharacter = true;
			}
		}
	}

	public void BiteEventExecute(TowerDefenseCharacter _target)
	{
		foreach (TowerDefenseCharacterEventBase item in biteEvent)
		{
			item.Execute(parent.GetLogicalGlobalPosition(), _target);
		}
	}

	public TowerDefenseCharacter GetTarget()
	{
		target = null;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(instance?.characterRegistry) || !TryGetSuckWorldRect(out var rect))
		{
			return null;
		}
		float num = 1f / 0f;
		float x = parent.GetLogicalGlobalPosition().X;
		List<TowerDefenseCharacter> charactersIntersectingRectListExcludingCamp = instance.characterRegistry.GetCharactersIntersectingRectListExcludingCamp(rect, parent.camp, parent.gridPos.Y, includeAllLineCheck: true);
		for (int i = 0; i < charactersIntersectingRectListExcludingCamp.Count; i++)
		{
			if (charactersIntersectingRectListExcludingCamp[i] is TowerDefenseZombie towerDefenseZombie && GodotObject.IsInstanceValid(towerDefenseZombie.instance) && !towerDefenseZombie.die && !towerDefenseZombie.nearDie && !towerDefenseZombie.instance.invincible && towerDefenseZombie.instance.canBeCollection && towerDefenseZombie.instance.zombiePhysique <= TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL && IsTargetInBiteForwardSide(towerDefenseZombie) && parent.CanTarget(towerDefenseZombie) && parent.CanCollision(towerDefenseZombie.instance.maskFlags) && towerDefenseZombie.IsTargetableFromLine(parent.gridPos.Y))
			{
				float num2 = Mathf.Abs(towerDefenseZombie.GetLogicalGlobalPosition().X - x);
				if (!(num2 >= num))
				{
					num = num2;
					target = towerDefenseZombie;
				}
			}
		}
		return target;
	}

	private bool IsTargetInBiteForwardSide(TowerDefenseCharacter biteTarget)
	{
		if (!GodotObject.IsInstanceValid(biteTarget) || !GodotObject.IsInstanceValid(parent))
		{
			return false;
		}
		if (!(parent is TowerDefensePlant))
		{
			return true;
		}
		if (!TryGetBiteForwardVector(out var forward))
		{
			return true;
		}
		Vector2 logicalGlobalPosition = parent.GetLogicalGlobalPosition();
		if (biteTarget.HasHitBox)
		{
			return GetRectForwardProjectionMax(biteTarget.WorldHitRect, logicalGlobalPosition, forward) >= -4f;
		}
		return (biteTarget.GetLogicalGlobalPosition() - logicalGlobalPosition).Dot(forward) >= -4f;
	}

	private bool TryGetBiteForwardVector(out Vector2 forward)
	{
		forward = Vector2.Zero;
		AttackComponent obj = attackComponent;
		if (obj == null || obj.IsReleased || !GodotObject.IsInstanceValid(parent) || !attackComponent.TryGetCheckAreaSegmentEnd(0, out var endpoint) || endpoint.LengthSquared() <= 0.0001f)
		{
			return false;
		}
		forward = parent.GetLogicalGlobalTransform(parent).BasisXform(endpoint);
		if (forward.LengthSquared() <= 0.0001f)
		{
			return false;
		}
		forward = forward.Normalized();
		return true;
	}

	private static float GetRectForwardProjectionMax(Rect2 rect, Vector2 origin, Vector2 forward)
	{
		Vector2 end = rect.End;
		return Mathf.Max(Mathf.Max(Mathf.Max((rect.Position - origin).Dot(forward), (new Vector2(end.X, rect.Position.Y) - origin).Dot(forward)), (new Vector2(rect.Position.X, end.Y) - origin).Dot(forward)), (end - origin).Dot(forward));
	}

	private bool TryGetSuckWorldRect(out Rect2 rect)
	{
		rect = default(Rect2);
		if (GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(_suckShape))
		{
			return _suckShape.TryGetWorldRect(parent.GetLogicalGlobalTransform(parent), out rect);
		}
		return false;
	}

	public override Dictionary ExportComponentSave()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>
		{
			{ "chewTimer", chewTimer },
			{ "isSuck", isSuck },
			{ "isChew", isChew },
			{ "eatCharacter", eatCharacter },
			{ "canEnterChew", canEnterChew },
			{ "healthNum", healthNum },
			{ "currentChewTime", currentChewTime }
		};
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["target"] = target.Name.ToString().ValidateNodeName();
		}
		Dictionary dictionary2 = new Dictionary();
		foreach (KeyValuePair<string, Variant> item in dictionary)
		{
			dictionary2[item.Key] = item.Value;
		}
		return dictionary2;
	}

	public override void ImportComponentSave(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		chewTimer = _data.GetValueOrDefault("chewTimer", 0.0).AsSingle();
		isSuck = _data.GetValueOrDefault("isSuck", false).AsBool();
		isChew = _data.GetValueOrDefault("isChew", false).AsBool();
		eatCharacter = _data.GetValueOrDefault("eatCharacter", false).AsBool();
		canEnterChew = _data.GetValueOrDefault("canEnterChew", true).AsBool();
		healthNum = _data.GetValueOrDefault("healthNum", 0).AsSingle();
		currentChewTime = _data.GetValueOrDefault("currentChewTime", 0f).AsSingle();
		string text = _data.GetValueOrDefault("target", "").AsString();
		if (text != "" && _owner?.charcterDicionary != null && _owner.charcterDicionary.ContainsKey(text))
		{
			target = _owner.charcterDicionary[text];
		}
	}

	public override Dictionary SyncSerialize()
	{
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>
		{
			{ "chewTimer", chewTimer },
			{ "isSuck", isSuck },
			{ "isChew", isChew },
			{ "eatCharacter", eatCharacter },
			{ "canEnterChew", canEnterChew },
			{ "healthNum", healthNum },
			{ "currentChewTime", currentChewTime }
		};
		if (GodotObject.IsInstanceValid(target))
		{
			dictionary["targetSyncId"] = target.syncId;
		}
		StateHandle stateHandle = base.StateMachine?.CurrentStateHandle;
		if (stateHandle != null && stateHandle.IsValid)
		{
			dictionary["state"] = stateHandle.StableId;
		}
		Dictionary dictionary2 = new Dictionary();
		foreach (KeyValuePair<string, Variant> item in dictionary)
		{
			dictionary2[item.Key] = item.Value;
		}
		return dictionary2;
	}

	public override void SyncDeserialize(Dictionary data)
	{
		chewTimer = data.GetValueOrDefault("chewTimer", chewTimer).AsSingle();
		isSuck = data.GetValueOrDefault("isSuck", isSuck).AsBool();
		isChew = data.GetValueOrDefault("isChew", isChew).AsBool();
		eatCharacter = data.GetValueOrDefault("eatCharacter", eatCharacter).AsBool();
		canEnterChew = data.GetValueOrDefault("canEnterChew", canEnterChew).AsBool();
		healthNum = data.GetValueOrDefault("healthNum", healthNum).AsSingle();
		currentChewTime = data.GetValueOrDefault("currentChewTime", currentChewTime).AsSingle();
		if (data.ContainsKey("targetSyncId"))
		{
			int num = data["targetSyncId"].AsInt32();
			TowerDefenseCharacter towerDefenseCharacter = null;
			if (num >= 0 && GodotObject.IsInstanceValid(TowerDefenseManager.CurrentControl) && TowerDefenseManager.CurrentControl._syncCharacters.TryGetValue(num, out var value))
			{
				towerDefenseCharacter = value;
			}
			if (_suckedTargetInteractionTarget != towerDefenseCharacter)
			{
				RestoreSuckedTargetInteraction();
			}
			target = towerDefenseCharacter;
		}
		else
		{
			RestoreSuckedTargetInteraction();
			target = null;
		}
		if (base.ShouldApplyLegacyStateField && data.ContainsKey("state"))
		{
			_pendingSyncedState = data["state"].AsString();
			ApplyPendingSyncedState();
		}
	}

	private void ApplyPendingSyncedState()
	{
		if (base.IsStateMachineRegistered && !string.IsNullOrEmpty(_pendingSyncedState))
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.CurrentStateHandle?.IsValid == true && SyncForceState(base.StateMachine, _pendingSyncedState))
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
