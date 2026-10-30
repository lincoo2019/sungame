using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombiePolestriker : TowerDefenseZombie
{
	private StateHandle _runStateHandle;

	private StateHandle _jumpStateHandle;

	private bool _stateSignalsConnected;

	private AttackComponent _attackComponent2;

	private const int JumpTargetCheckStride = 2;

	private const float JumpLandingOffset = 148f;

	private float _jumpLandingX;

	private bool _hasJumpLanding;

	public bool jumpOver;

	public bool jumpMove;

	public bool isJump;

	public bool isBlock;

	public Tween moveTween;

	public TowerDefenseCharacter jumpTarget;

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

	private float GlobalPositionX
	{
		get
		{
			return GetLogicalGlobalPosition().X;
		}
		set
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X = value;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_runStateHandle = base.StateMachine?.GetStateById("zombie.polestriker.run");
		_jumpStateHandle = base.StateMachine?.GetStateById("zombie.polestriker.jump");
		StateHandle runStateHandle = _runStateHandle;
		if (runStateHandle != null && runStateHandle.IsValid)
		{
			StateHandle jumpStateHandle = _jumpStateHandle;
			if (jumpStateHandle != null && jumpStateHandle.IsValid)
			{
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_jumpStateHandle.Entered += JumpEntered;
				_jumpStateHandle.Exited += JumpExited;
				_jumpStateHandle.PhysicsProcessing += JumpProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_runStateHandle != null)
			{
				_runStateHandle.Entered -= RunEntered;
				_runStateHandle.Exited -= RunExited;
				_runStateHandle.PhysicsProcessing -= RunProcessing;
			}
			_runStateHandle = null;
			if (_jumpStateHandle != null)
			{
				_jumpStateHandle.Entered -= JumpEntered;
				_jumpStateHandle.Exited -= JumpExited;
				_jumpStateHandle.PhysicsProcessing -= JumpProcessing;
			}
			_jumpStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			ConnectStateSignals();
		}
	}

	public override void WalkEntered()
	{
		if (isBlock)
		{
			isBlock = false;
			if (base.inWater)
			{
				base.sprite.SetAnimation(base.swimAnimeClip);
			}
			else
			{
				base.sprite.SetAnimation(base.walkAnimeClip);
			}
		}
		else if (jumpMove)
		{
			jumpMove = false;
			base.sprite.SetAnimation(base.walkAnimeClip);
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		ActivateGroundMovementAfterStateDelay(base.WalkStateHandle);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 4.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public virtual void RunEntered()
	{
		base.sprite.SetAnimation("Run", loop: true, 0.2);
		groundMoveComponent.SetAlive(true);
	}

	public void RunProcessing(double delta)
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
			if (!base.sprite.pause && !(base.sprite.timeScale <= 0.0) && !IsRemoteClient && !nearDie && !TowerDefenseManager.Instance.backZombie && ShouldCheckJumpTargetThisFrame() && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target))
			{
				SendStateEvent("ToJump");
			}
		}
	}

	private bool ShouldCheckJumpTargetThisFrame()
	{
		return (ulong)((long)Engine.GetPhysicsFrames() + (long)randFreshIndex) % 2uL == 0;
	}

	public void RunExited()
	{
		groundMoveComponent.SetAlive(false);
	}

	public virtual void JumpEntered()
	{
		shadowSprite.Visible = false;
		base.sprite.SetAnimation("Jump", loop: false, 0.2);
		instance.collisionFlags = 0;
		instance.maskFlags = 0;
		jumpTarget = _attackComponent2.target;
		PrepareJumpLanding();
	}

	public void ApplyRemoteJumpStartFromSync()
	{
		if (!IsRemoteClient)
		{
			return;
		}
		GroundMoveComponent obj = groundMoveComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundMoveComponent.SetAlive(false);
		}
		if (!IsJumpVisualActive())
		{
			IStateMachineController stateMachine = base.StateMachine;
			if (stateMachine != null && stateMachine.IsInitialized)
			{
				SendStateEvent("ToJump");
			}
		}
	}

	public virtual void JumpProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
		if (isJump && !IsRemoteClient)
		{
			TowerDefenseCharacter blocker = ResolveTallJumpBlocker();
			if (GodotObject.IsInstanceValid(blocker))
			{
				StopAtTallJumpBlocker(blocker);
			}
		}
	}

	private TowerDefenseCharacter ResolveTallJumpBlocker()
	{
		if (IsTallJumpBlocker(jumpTarget))
		{
			return jumpTarget;
		}
		AttackComponent attackComponent = _attackComponent2;
		if (attackComponent == null || attackComponent.IsReleased || TowerDefenseManager.Instance == null || !_attackComponent2.TryGetCheckAreaWorldRect(out var worldRect))
		{
			return null;
		}
		foreach (TowerDefenseCharacter characterTargetLineFromRectWithCollisionFlag in TowerDefenseManager.Instance.GetCharacterTargetLineFromRectWithCollisionFlags(this, 1, worldRect))
		{
			if (IsTallJumpBlocker(characterTargetLineFromRectWithCollisionFlag))
			{
				return characterTargetLineFromRectWithCollisionFlag;
			}
		}
		return null;
	}

	private static bool IsTallJumpBlocker(TowerDefenseCharacter candidate)
	{
		if (!GodotObject.IsInstanceValid(candidate) || !GodotObject.IsInstanceValid(candidate.instance))
		{
			return false;
		}
		if (candidate.instance.height < TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
		{
			return false;
		}
		if (candidate is TowerDefensePlant && GodotObject.IsInstanceValid(candidate.cell) && GodotObject.IsInstanceValid(candidate.cell.characterLadder))
		{
			return false;
		}
		return true;
	}

	private void StopAtTallJumpBlocker(TowerDefenseCharacter blocker)
	{
		jumpOver = true;
		GlobalPositionX = blocker.GetLogicalGlobalPosition().X + 40f;
		base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
		isBlock = true;
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
		CancelJumpMovement();
	}

	public void JumpExited()
	{
		isJump = false;
		jumpTarget = null;
		CancelJumpMovement();
		if (!base.inWater)
		{
			shadowSprite.Visible = !base.invisible;
		}
		instance.collisionFlags = 1;
		instance.maskFlags = 1;
	}

	public override void Walk()
	{
		if (jumpOver)
		{
			SendStateEvent("ToWalk");
		}
		else
		{
			SendStateEvent("ToRun");
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			base.sprite.SetFliters(new Godot.Collections.Array { "Zombie_polevaulter_innerarm_lower", "Zombie_polevaulter_innerarm_upper", "Zombie_polevaulter_innerhand", "Zombie_polevaulter_pole", "Zombie_polevaulter_pole2" }, open: false);
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		switch (command)
		{
		case "audio":
			AudioManager.Instance.AudioPlay("Polevault");
			break;
		case "check":
			isJump = true;
			if (!TowerDefenseManager.Instance.backZombie && _hasJumpLanding)
			{
				StopMoveTween();
				moveTween = CreateTween();
				float jumpDirection = GetJumpDirection();
				moveTween.TweenMethod(Callable.From(delegate(float value)
				{
					GlobalPositionX = value;
				}), GlobalPositionX, _jumpLandingX + jumpDirection * 148f, 0.5 / timeScaleInit);
			}
			break;
		case "jumpOver":
			jumpOver = true;
			break;
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Jump" && !(clip != base.sprite.clip))
		{
			jumpMove = true;
			StopMoveTween();
			if (!TowerDefenseManager.Instance.backZombie && _hasJumpLanding)
			{
				GlobalPositionX = _jumpLandingX;
			}
			_hasJumpLanding = false;
			base.gridPos = TowerDefenseManager.Instance.GetMapGridPos(GetLogicalGlobalPosition());
			base.sprite.QueueRedraw();
			Walk();
		}
	}

	public override void HitBoxDestroy()
	{
		base.HitBoxDestroy();
		_attackComponent2?.SetCheckAreaEnabled(enabled: false);
	}

	public override bool CanBlock()
	{
		return !jumpOver;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	private bool IsJumpVisualActive()
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			return base.sprite.clip == "Jump";
		}
		return false;
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary
		{
			["rev"] = GetNetworkSpecialStateRevision(),
			["jumpOver"] = jumpOver,
			["jumpMove"] = jumpMove,
			["isJump"] = isJump,
			["isBlock"] = isBlock,
			["hasJumpLanding"] = _hasJumpLanding,
			["jumpLandingX"] = Mathf.Snapped(_jumpLandingX, 0.1f)
		};
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		jumpOver = data.GetValueOrDefault("jumpOver", jumpOver).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", jumpMove).AsBool();
		isJump = data.GetValueOrDefault("isJump", isJump).AsBool();
		isBlock = data.GetValueOrDefault("isBlock", isBlock).AsBool();
		_hasJumpLanding = data.GetValueOrDefault("hasJumpLanding", _hasJumpLanding).AsBool();
		_jumpLandingX = (float)data.GetValueOrDefault("jumpLandingX", _jumpLandingX).AsDouble();
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return HashCode.Combine((int)((jumpOver ? 1u : 0u) | (uint)(jumpMove ? 2 : 0) | (uint)(isJump ? 4 : 0) | (uint)(isBlock ? 8 : 0)) | (_hasJumpLanding ? 16 : 0), Mathf.RoundToInt(_jumpLandingX * 10f));
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		if (!isJump)
		{
			return IsJumpVisualActive();
		}
		return true;
	}

	public override void OnRemoteNetworkAnimationStart(string clipName)
	{
		if (clipName == "Jump")
		{
			ApplyRemoteJumpStartFromSync();
		}
	}

	public override void Block(TowerDefenseCharacter target)
	{
		jumpOver = true;
		isBlock = true;
		isJump = false;
		jumpTarget = null;
		CancelJumpMovement();
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}

	private void PrepareJumpLanding()
	{
		_hasJumpLanding = !TowerDefenseManager.Instance.backZombie;
		if (_hasJumpLanding)
		{
			float jumpDirection = GetJumpDirection();
			float num = TowerDefenseManager.Instance.GetMapGridSize().X + 148f;
			_jumpLandingX = GetLogicalGlobalPosition().X - jumpDirection * num;
		}
	}

	private float GetJumpDirection()
	{
		float num = Mathf.Sign(base.Scale.X * transformPoint.Scale.X);
		if (!Mathf.IsZeroApprox(num))
		{
			return num;
		}
		return 1f;
	}

	private void CancelJumpMovement()
	{
		StopMoveTween();
		_hasJumpLanding = false;
	}

	private void StopMoveTween()
	{
		if (GodotObject.IsInstanceValid(moveTween) && moveTween.IsRunning())
		{
			moveTween.Kill();
		}
		moveTween = null;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "jumpOver", jumpOver },
			{ "jumpMove", jumpMove },
			{ "isJump", isJump },
			{ "isBlock", isBlock }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		jumpOver = data.GetValueOrDefault("jumpOver", false).AsBool();
		jumpMove = data.GetValueOrDefault("jumpMove", false).AsBool();
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		isBlock = data.GetValueOrDefault("isBlock", false).AsBool();
	}
}
