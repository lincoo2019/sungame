using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBalloonBomb : TowerDefenseZombie
{
	private static PackedScene _JACKBOX_EXPLOSION;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>();

	public double speed = 100.0;

	public bool audioPlay;

	private StateHandle _flyStateHandle;

	private StateHandle _flyAttackStateHandle;

	private bool _roleStateSignalsConnected;

	private Tween _logicalBlowTween;

	private static PackedScene JACKBOX_EXPLOSION => _JACKBOX_EXPLOSION ?? (_JACKBOX_EXPLOSION = GD.Load<PackedScene>("uid://cxnt2jbnk48fp"));

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine == null || !stateMachine.IsInitialized)
		{
			return;
		}
		_flyStateHandle = base.StateMachine?.GetStateById("zombie.balloon_bomb.fly");
		_flyAttackStateHandle = base.StateMachine?.GetStateById("zombie.balloon_bomb.fly_attack");
		StateHandle flyStateHandle = _flyStateHandle;
		if (flyStateHandle != null && flyStateHandle.IsValid)
		{
			StateHandle flyAttackStateHandle = _flyAttackStateHandle;
			if (flyAttackStateHandle != null && flyAttackStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.Exited += FlyExited;
				_flyStateHandle.PhysicsProcessing += FlyProcessing;
				_flyAttackStateHandle.Entered += FlyAttackEntered;
				_flyAttackStateHandle.Exited += FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing += FlyAttackProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_flyStateHandle != null)
			{
				_flyStateHandle.Entered -= FlyEntered;
				_flyStateHandle.Exited -= FlyExited;
				_flyStateHandle.PhysicsProcessing -= FlyProcessing;
			}
			_flyStateHandle = null;
			if (_flyAttackStateHandle != null)
			{
				_flyAttackStateHandle.Entered -= FlyAttackEntered;
				_flyAttackStateHandle.Exited -= FlyAttackExited;
				_flyAttackStateHandle.PhysicsProcessing -= FlyAttackProcessing;
			}
			_flyAttackStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		CancelLogicalBlowTween();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl) && TowerDefenseManager.Instance.currentControl.isGameRunning && inGame)
		{
			targetRegistrationComponent.canCarry = false;
			AudioManager.Instance.AudioPlay("BalloonInflate");
			ConnectRoleStateSignals();
		}
	}

	public void FlyEntered()
	{
		base.sprite.SetAnimation("Idle", loop: true, 0.2);
	}

	public void FlyProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!base.IsRemoteNetworkReplica)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if (!base.sprite.pause)
			{
				globalPositionForPhysicsFrame.X -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 20.0)
			{
				SendStateEvent("ToFlyAttack");
			}
			else if (!base.sprite.pause && attackComponent.CanAttack())
			{
				SendStateEvent("ToFlyAttack");
			}
		}
	}

	public void FlyExited()
	{
	}

	public void FlyAttackEntered()
	{
		HitBoxDestroy();
		base.sprite.SetAnimation("Attack", loop: false, 0.2);
	}

	public void FlyAttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void FlyAttackExited()
	{
	}

	public override void Walk()
	{
		SendStateEvent("ToFly");
	}

	public override void Attack()
	{
		if (die)
		{
			SendStateEvent("ToDie");
		}
		else
		{
			SendStateEvent("ToFly");
		}
	}

	public override void Blow()
	{
		HitBoxDestroy();
		CancelLogicalBlowTween();
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		Vector2 targetPosition = logicalGlobalPosition + new Vector2(TowerDefenseManager.Instance.GetMapGridSize().Y * (float)TowerDefenseManager.Instance.GetMapGridNum().Y * 2f, 0f);
		Tween tween = CreateTween();
		_logicalBlowTween = tween;
		tween.TweenMethod(Callable.From<Vector2>(base.SetLogicalGlobalPosition), logicalGlobalPosition, targetPosition, 1.0);
		tween.TweenCallback(Callable.From(delegate
		{
			CompleteLogicalBlowTween(tween, targetPosition);
		}));
	}

	public override void DieEntered()
	{
		CancelLogicalBlowTween();
		base.DieEntered();
	}

	private void CompleteLogicalBlowTween(Tween tween, Vector2 targetPosition)
	{
		if (_logicalBlowTween == tween && !isDestroy)
		{
			_logicalBlowTween = null;
			SetLogicalGlobalPosition(targetPosition);
			Destroy();
		}
	}

	private void CancelLogicalBlowTween()
	{
		if (GodotObject.IsInstanceValid(_logicalBlowTween))
		{
			_logicalBlowTween.Kill();
		}
		_logicalBlowTween = null;
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == "attack")
		{
			CreateEffect();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Attack")
		{
			Destroy();
		}
	}

	public void CreateEffect()
	{
		ViewManager.Instance.CameraShake(new Vector2(GD.RandRange(-1, 1), GD.RandRange(-1, 1)), 5.0, 0.05, 4);
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(JACKBOX_EXPLOSION, base.gridPos);
		Vector2 pos = (towerDefenseEffectParticlesOnce.GlobalPosition = GetLogicalGlobalPosition());
		TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseExplode.CreateExplode(pos, new Vector2(1.25f, 1.25f), eventList, new Array<TowerDefenseCharacter>(), base.camp, -1, suppressDeathrattles: true);
		AudioManager.Instance.AudioPlay("ExplodeCherrybomb");
	}
}
