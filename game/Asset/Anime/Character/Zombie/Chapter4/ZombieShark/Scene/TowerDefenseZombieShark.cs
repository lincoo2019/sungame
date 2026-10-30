using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieShark : TowerDefenseZombie
{
	private ChomperComponent _chomperComponent;

	public bool audioPlay;

	public double originalWaterHeight;

	private StateHandle _runStateHandle;

	private bool _roleStateSignalsConnected;

	private bool _roleReady;

	private void ConnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_runStateHandle = base.StateMachine?.GetStateById("zombie.shark.run");
			StateHandle runStateHandle = _runStateHandle;
			if (runStateHandle != null && runStateHandle.IsValid)
			{
				_runStateHandle.Entered += RunEntered;
				_runStateHandle.Exited += RunExited;
				_runStateHandle.PhysicsProcessing += RunProcessing;
				_roleStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectRoleStateSignals()
	{
		if (_roleStateSignalsConnected)
		{
			if (_runStateHandle != null)
			{
				_runStateHandle.Entered -= RunEntered;
				_runStateHandle.Exited -= RunExited;
				_runStateHandle.PhysicsProcessing -= RunProcessing;
			}
			_runStateHandle = null;
			_roleStateSignalsConnected = false;
		}
	}

	public override void _Ready()
	{
		originalWaterHeight = base.waterHeight;
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_chomperComponent = componentManager.GetRuntime<ChomperComponent>();
			base.sprite.OnAnimeStarted += AnimeStarted;
			if (_chomperComponent != null && !_chomperComponent.IsReleased)
			{
				_chomperComponent.OnChewBegin += ChewBegin;
				_chomperComponent.OnChewOver += ChewOver;
			}
			ConnectRoleStateSignals();
			_roleReady = true;
			if (base.inWater)
			{
				Callable.From(QueueSpawnWaterEntryAnimation).CallDeferred();
			}
		}
	}

	public override void _ExitTree()
	{
		_roleReady = false;
		base._ExitTree();
		if (_chomperComponent != null && !_chomperComponent.IsReleased)
		{
			_chomperComponent.OnChewBegin -= ChewBegin;
			_chomperComponent.OnChewOver -= ChewOver;
		}
	}

	public override void BlowBack(double num, double time = 1.0)
	{
		if (!base.inWater)
		{
			base.BlowBack(num, time);
		}
	}

	public override void AttackEntered()
	{
		base.AttackEntered();
		if (base.inWater)
		{
			instance.maskFlags = 9;
		}
	}

	public override void AttackExited()
	{
		base.AttackExited();
		if (base.inWater)
		{
			instance.maskFlags = 32;
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		if (!audioPlay && (double)GetGlobalPositionForPhysicsFrame(currentPhysicsFrame).X < TowerDefenseManager.Instance.GetMapGroundRight())
		{
			AudioManager.Instance.AudioPlay("DolphinAppears");
			audioPlay = true;
		}
	}

	public override void Walk()
	{
		if (base.inWater)
		{
			SendStateEvent("ToRun");
		}
		else
		{
			SendStateEvent("ToWalk");
		}
	}

	public void RunEntered()
	{
		if (base.inWater)
		{
			if (!BeginSwimEntryAnimationIfNeeded() && base.sprite.clip != base.inSwimAnimeClip)
			{
				base.sprite.SetAnimation(base.swimAnimeClip, loop: true, 0.2);
			}
		}
		else
		{
			base.sprite.SetAnimation(base.walkAnimeClip, loop: true, 0.2);
		}
		groundMoveComponent.SetAlive(true);
	}

	public void RunProcessing(double delta)
	{
		if (base.sprite.clip == base.inSwimAnimeClip)
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 2.0;
		}
		else
		{
			base.sprite.timeScale = timeScale * base.walkSpeedScale * 0.5;
		}
		if (!nearDie && base.sprite.clip == "SwimRun")
		{
			_chomperComponent.alive = true;
		}
	}

	public void RunExited()
	{
		groundMoveComponent.SetAlive(false);
	}

	public override void InWater()
	{
		base.waterHeight = originalWaterHeight;
		base.InWater();
		if (_roleReady)
		{
			BeginSwimEntryAnimationIfNeeded();
		}
		base.useAttackDps = false;
		instance.maskFlags = 32;
	}

	private bool BeginSwimEntryAnimationIfNeeded()
	{
		if (!base.inWater || inSwimPlay || string.IsNullOrEmpty(base.inSwimAnimeClip) || !GodotObject.IsInstanceValid(base.sprite))
		{
			return false;
		}
		base.sprite.SetAnimation(base.inSwimAnimeClip, loop: false, 0.2);
		base.sprite.AddAnimation(base.swimAnimeClip, 0.0, loop: true, 0.2);
		inSwimPlay = true;
		return true;
	}

	private void QueueSpawnWaterEntryAnimation()
	{
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(this))
			{
				BeginSwimEntryAnimationIfNeeded();
			}
		}).CallDeferred();
	}

	public override void OutWater()
	{
		base.OutWater();
		CreateTween().TweenProperty(base.sprite, "offset", new Vector2(-40f, -92f), 0.25);
		base.useAttackDps = true;
		_chomperComponent.alive = false;
		base.waterHeight = 0.0;
		if (!base.IsRemoteNetworkReplica)
		{
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
			logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 10f;
			SetLogicalGlobalPosition(logicalGlobalPosition);
		}
		instance.maskFlags = 9;
	}

	public override void DieEntered()
	{
		base.DieEntered();
		base.sprite.offset = new Vector2(-40f, -92f);
		if (base.inWater)
		{
			base.waterHeight = 60.0;
			base.groundHeight = -60.0;
			base.z = -60.0;
			Tween tween = CreateTween();
			tween.SetParallel();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Cubic);
			tween.TweenProperty(this, "groundHeight", -100.0, 1.0);
		}
	}

	public void AnimeStarted(string clip)
	{
		if (clip == "DolphinRun")
		{
			base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (!(clip == "ChewOver"))
		{
			if (clip == "JumpInWater")
			{
				if (!base.IsRemoteNetworkReplica)
				{
					Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
					logicalGlobalPosition.X -= base.Scale.X * transformPoint.Scale.X * 64f;
					SetLogicalGlobalPosition(logicalGlobalPosition);
				}
				base.sprite.offset = new Vector2(24f, base.sprite.offset.Y);
			}
		}
		else
		{
			Walk();
		}
	}

	public void ChewOver()
	{
		if (base.inWater)
		{
			instance.maskFlags = 32;
		}
	}

	public void ChewBegin()
	{
		instance.maskFlags = 9;
	}
}
