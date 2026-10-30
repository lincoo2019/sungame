using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieAircraft : TowerDefenseZombie
{
	private StateHandle _flyStateHandle;

	private StateHandle _swingStateHandle;

	private StateHandle _popStateHandle;

	private bool _stateSignalsConnected;

	public bool pop;

	public bool swing;

	public double speed = 25.0;

	public bool audioPlay;

	private CharacterTimerComponent _timerComponent;

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
		_flyStateHandle = base.StateMachine?.GetStateById("zombie.aircraft.fly");
		_swingStateHandle = base.StateMachine?.GetStateById("zombie.aircraft.swing");
		_popStateHandle = base.StateMachine?.GetStateById("zombie.aircraft.pop");
		StateHandle flyStateHandle = _flyStateHandle;
		if (flyStateHandle == null || !flyStateHandle.IsValid)
		{
			return;
		}
		StateHandle swingStateHandle = _swingStateHandle;
		if (swingStateHandle != null && swingStateHandle.IsValid)
		{
			StateHandle popStateHandle = _popStateHandle;
			if (popStateHandle != null && popStateHandle.IsValid)
			{
				_flyStateHandle.Entered += FlyEntered;
				_flyStateHandle.Exited += FlyExited;
				_flyStateHandle.PhysicsProcessing += FlyProcessing;
				_swingStateHandle.Entered += SwingEntered;
				_swingStateHandle.Exited += SwingExited;
				_swingStateHandle.PhysicsProcessing += SwingProcessing;
				_popStateHandle.Entered += PopEntered;
				_popStateHandle.Exited += PopExited;
				_popStateHandle.PhysicsProcessing += PopProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_flyStateHandle != null)
			{
				_flyStateHandle.Entered -= FlyEntered;
				_flyStateHandle.Exited -= FlyExited;
				_flyStateHandle.PhysicsProcessing -= FlyProcessing;
			}
			_flyStateHandle = null;
			if (_swingStateHandle != null)
			{
				_swingStateHandle.Entered -= SwingEntered;
				_swingStateHandle.Exited -= SwingExited;
				_swingStateHandle.PhysicsProcessing -= SwingProcessing;
			}
			_swingStateHandle = null;
			if (_popStateHandle != null)
			{
				_popStateHandle.Entered -= PopEntered;
				_popStateHandle.Exited -= PopExited;
				_popStateHandle.PhysicsProcessing -= PopProcessing;
			}
			_popStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
			_timerComponent.OnTimeout += Timeout;
			ConnectStateSignals();
		}
	}

	public override void _ExitTree()
	{
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
		DisconnectStateSignals();
		base._ExitTree();
	}

	public override void BatchUpdate(double delta)
	{
		base.BatchUpdate(delta);
		if (!Engine.IsEditorHint() && TowerDefenseManager.Instance.IsGameRunning() && inGame && !_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 15.0);
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn" && !pop)
		{
			if (!base.sprite.pause)
			{
				SendStateEvent("ToSwing");
			}
			_timerComponent.Run("Spawn", 15.0);
		}
	}

	public void FlyEntered()
	{
		base.sprite.SetAnimation("Idle", loop: true, 0.2);
	}

	public void FlyProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (base.IsRemoteNetworkReplica)
		{
			return;
		}
		ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
		Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
		if (!base.sprite.pause)
		{
			globalPositionForPhysicsFrame.X -= (float)(speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
			SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
		}
		if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 20.0)
		{
			instance.ArmorDelete("Aircraft");
			return;
		}
		if (!audioPlay && (double)globalPositionForPhysicsFrame.X < groundRight)
		{
			AudioManager.Instance.AudioPlay("BalloonInflate");
			audioPlay = true;
		}
		if (!base.sprite.pause && attackComponent.CanAttack() && GodotObject.IsInstanceValid(attackComponent.target) && attackComponent.target is TowerDefenseZombie)
		{
			attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
		}
	}

	public void FlyExited()
	{
	}

	public void SwingEntered()
	{
		swing = true;
		base.sprite.SetAnimation("Swing", loop: true, 0.2);
		CompleteSwingAsync();
	}

	private async Task CompleteSwingAsync()
	{
		if (await WaitForStateDelayAsync(_swingStateHandle, 2.0))
		{
			Walk();
		}
	}

	public void SwingProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (!base.IsRemoteNetworkReplica)
		{
			ulong currentPhysicsFrame = TowerDefenseProcessModeDispatch.CurrentPhysicsFrame;
			Vector2 globalPositionForPhysicsFrame = GetGlobalPositionForPhysicsFrame(currentPhysicsFrame);
			if (!base.sprite.pause)
			{
				globalPositionForPhysicsFrame.X -= (float)(3.0 * speed * delta * base.sprite.timeScale * (double)transformPoint.Scale.X * (double)base.Scale.X * (double)((!base.sprite.playBack) ? 1 : (-1)));
				SetGlobalPositionForPhysicsFrame(globalPositionForPhysicsFrame, currentPhysicsFrame);
			}
			if ((double)globalPositionForPhysicsFrame.X < TowerDefenseManager.Instance.GetMapGroundLeft() + 20.0)
			{
				instance.ArmorDelete("Aircraft");
			}
		}
	}

	public void SwingExited()
	{
		swing = false;
	}

	public void PopEntered()
	{
		base.sprite.SetAnimation("Pop", loop: false, 0.2);
	}

	public void PopProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 2.0;
	}

	public void PopExited()
	{
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void Walk()
	{
		if (pop)
		{
			SendStateEvent("ToWalk");
		}
		else
		{
			SendStateEvent("ToFly");
		}
	}

	public override void Blow()
	{
		if (!pop && !swing)
		{
			BlowBack(0.5, 0.2);
			SendStateEvent("ToSwing");
		}
	}

	public override void BlowBack(double num, double time = 1.0)
	{
		if (!pop && !swing)
		{
			base.BlowBack(num, time);
			SendStateEvent("ToSwing");
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Pop")
		{
			Walk();
		}
	}

	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Aircraft")
		{
			_timerComponent.SetAlive(alive: false);
			pop = true;
			AudioManager.Instance.AudioPlay("BalloonPop");
			instance.collisionFlags = 1;
			instance.maskFlags = 9;
			instance.unUseBuffFlags = 0;
			SendStateEvent("ToPop");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["pop"] = pop,
			["speed"] = speed,
			["swing"] = swing
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		pop = data.GetValueOrDefault("pop", false).AsBool();
		speed = data.GetValueOrDefault("speed", 25.0).AsDouble();
		swing = data.GetValueOrDefault("swing", false).AsBool();
	}
}
