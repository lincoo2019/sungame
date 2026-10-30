using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombiePogo : TowerDefenseZombie
{
	private AttackComponent _attackComponent2;

	private Tween _jumpMoveTween;

	private ulong _jumpMoveGeneration;

	public bool isJump;

	private bool _hasPogo = true;

	public bool pogoPlant;

	public double jumpToPos;

	public int jumpWait = 1;

	private StateHandle _pogoStateHandle;

	private bool _roleStateSignalsConnected;

	public bool hasPogo
	{
		get
		{
			return _hasPogo;
		}
		set
		{
			_hasPogo = value;
			if (!_hasPogo)
			{
				CancelJumpMove();
				isJump = false;
				idleAnimeClip = "Idle";
			}
		}
	}

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_pogoStateHandle = base.StateMachine?.GetStateById("zombie.pogo.pogo");
			StateHandle pogoStateHandle = _pogoStateHandle;
			if (pogoStateHandle != null && pogoStateHandle.IsValid)
			{
				_pogoStateHandle.Entered += PogoEntered;
				_pogoStateHandle.Exited += PogoExited;
				_pogoStateHandle.PhysicsProcessing += PogoProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_pogoStateHandle != null)
			{
				_pogoStateHandle.Entered -= PogoEntered;
				_pogoStateHandle.Exited -= PogoExited;
				_pogoStateHandle.PhysicsProcessing -= PogoProcessing;
			}
			_pogoStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelJumpMove();
		base.OnLand -= Land;
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
			base.OnLand += Land;
			ySpeed = -300.0;
			ConnectRoleStateSignals();
		}
	}

	public override void Walk()
	{
		if (hasPogo)
		{
			SendStateEvent("ToPogo");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void PogoEntered()
	{
		base.sprite.SetAnimation("Pogo", loop: true, 0.2);
		if (isGround)
		{
			ySpeed = -300.0;
		}
	}

	public void PogoProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		if (!pogoPlant)
		{
			bool pause = base.sprite.pause;
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if (!pause)
			{
				double num = 30.0 * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (base.sprite.playBack ? (-1.0) : 1.0);
				if ((double)globalPositionForPhysicsFrame.X > groundRight)
				{
					num *= 2.0;
				}
				globalPositionForPhysicsFrame.X -= (float)num;
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if (!pause && attackComponent.CanAttack() && !TowerDefenseManager.Instance.backZombie)
			{
				pogoPlant = true;
				float x = base.Scale.X;
				jumpToPos = globalPositionForPhysicsFrame.X - TowerDefenseManager.Instance.GetMapGridSize().X * x - 10f * x;
			}
		}
		else if (isJump && _attackComponent2.HasAttackGridTargetCandidates() && _attackComponent2.CanAttack() && GodotObject.IsInstanceValid(_attackComponent2.target) && _attackComponent2.target.instance.height >= TowerDefenseEnum.CHARACTER_HEIGHT.TALL)
		{
			instance.ArmorDelete("Pogo");
			hasPogo = false;
			isJump = false;
			AudioManager.Instance.AudioPlay("Bonk");
			Walk();
		}
	}

	public void PogoExited()
	{
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Pogo")
		{
			if (instance.ArmorHas("SpecialHelmet"))
			{
				instance.unUseBuffFlags = -1;
			}
			else
			{
				instance.unUseBuffFlags = 0;
			}
			isJump = false;
			hasPogo = false;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
			if (base.inWater)
			{
				base.groundHeight = 0.0 - base.waterHeight;
			}
			Walk();
		}
	}

	public override void InWater()
	{
		base.InWater();
		if (hasPogo)
		{
			base.groundHeight = 0.0;
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = false;
			}
		}
	}

	public override void OutWater()
	{
		if (hasPogo)
		{
			GroundHeightComponent obj = groundHeightComponent;
			if (obj != null && !obj.IsReleased)
			{
				groundHeightComponent.handleWaterHeight = true;
			}
		}
		base.OutWater();
		if (hasPogo)
		{
			ySpeed = -300.0;
		}
	}

	public async void Land()
	{
		if (!hasPogo)
		{
			return;
		}
		ulong jumpMoveGeneration = _jumpMoveGeneration;
		isJump = false;
		gravity = 490.0;
		ySpeed = -300.0;
		if (base.IsRemoteNetworkReplica || !pogoPlant || TowerDefenseManager.Instance.backZombie)
		{
			return;
		}
		isJump = true;
		if (jumpWait > 0)
		{
			jumpWait--;
		}
		else
		{
			jumpWait = 1;
			StartJumpMoveTween();
			Tween tween = _jumpMoveTween;
			jumpMoveGeneration = _jumpMoveGeneration;
			ySpeed = -400.0;
			await ToSignal(tween, Tween.SignalName.Finished);
			if (!IsCurrentJumpMove(tween, jumpMoveGeneration))
			{
				return;
			}
			_jumpMoveTween = null;
			pogoPlant = false;
		}
		await ToSignal(GetTree().CreateTimer(0.2, processAlways: false), SceneTreeTimer.SignalName.Timeout);
		if (hasPogo && jumpMoveGeneration == _jumpMoveGeneration && IsInsideTree())
		{
			isJump = false;
		}
	}

	private void StartJumpMoveTween()
	{
		CancelJumpMove(clearPogoPlant: false);
		_jumpMoveTween = CreateTween();
		_jumpMoveTween.SetEase(Tween.EaseType.InOut);
		_jumpMoveTween.SetTrans(Tween.TransitionType.Sine);
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 vector = new Vector2((float)jumpToPos, logicalGlobalPosition.Y);
		_jumpMoveTween.TweenMethod(Callable.From<Vector2>(base.SetLogicalGlobalPosition), logicalGlobalPosition, vector, 0.5);
	}

	private bool IsCurrentJumpMove(Tween tween, ulong generation)
	{
		if (hasPogo && generation == _jumpMoveGeneration && _jumpMoveTween == tween && GodotObject.IsInstanceValid(tween))
		{
			return IsInsideTree();
		}
		return false;
	}

	private void CancelJumpMove(bool clearPogoPlant = true)
	{
		_jumpMoveGeneration++;
		Tween jumpMoveTween = _jumpMoveTween;
		_jumpMoveTween = null;
		if (GodotObject.IsInstanceValid(jumpMoveTween))
		{
			jumpMoveTween.Kill();
		}
		if (clearPogoPlant)
		{
			pogoPlant = false;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (!(command == "pogo"))
		{
			_ = command == "jump_check";
		}
	}

	public override bool CanBlock()
	{
		return hasPogo;
	}

	public override string BlockType()
	{
		return "Jump";
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "isJump", isJump },
			{ "hasPogo", hasPogo },
			{ "pogoPlant", pogoPlant },
			{ "jumpToPos", jumpToPos },
			{ "jumpWait", jumpWait }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isJump = data.GetValueOrDefault("isJump", false).AsBool();
		hasPogo = data.GetValueOrDefault("hasPogo", true).AsBool();
		pogoPlant = data.GetValueOrDefault("pogoPlant", false).AsBool();
		jumpToPos = data.GetValueOrDefault("jumpToPos", 0.0).AsDouble();
		jumpWait = data.GetValueOrDefault("jumpWait", 1).AsInt32();
		if (!hasPogo)
		{
			pogoPlant = false;
			isJump = false;
		}
	}

	public override void Block(TowerDefenseCharacter target)
	{
		instance.ArmorDelete("Pogo");
		hasPogo = false;
		isJump = false;
		GroundHeightComponent obj = groundHeightComponent;
		if (obj != null && !obj.IsReleased)
		{
			groundHeightComponent.handleWaterHeight = true;
		}
		if (base.inWater)
		{
			base.groundHeight = 0.0 - base.waterHeight;
		}
		AudioManager.Instance.AudioPlay("Bonk");
		Walk();
	}
}
