using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieImpScaredy : TowerDefenseZombie
{
	private const string RecoveryTimerName = "Spawn";

	private const double HiddenRecoveryHealth = 25.0;

	private StateHandle _scareStateHandle;

	private StateHandle _upStateHandle;

	private bool _stateSignalsConnected;

	private CharacterTimerComponent _timerComponent;

	public bool scare;

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
		_scareStateHandle = base.StateMachine?.GetStateById("zombie.imp_scaredy.scare");
		_upStateHandle = base.StateMachine?.GetStateById("zombie.imp_scaredy.up");
		StateHandle scareStateHandle = _scareStateHandle;
		if (scareStateHandle != null && scareStateHandle.IsValid)
		{
			StateHandle upStateHandle = _upStateHandle;
			if (upStateHandle != null && upStateHandle.IsValid)
			{
				_scareStateHandle.Entered += ScareEntered;
				_scareStateHandle.Exited += ScareExited;
				_scareStateHandle.PhysicsProcessing += ScareProcessing;
				_upStateHandle.Entered += UpEntered;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_scareStateHandle != null)
			{
				_scareStateHandle.Entered -= ScareEntered;
				_scareStateHandle.Exited -= ScareExited;
				_scareStateHandle.PhysicsProcessing -= ScareProcessing;
			}
			_scareStateHandle = null;
			if (_upStateHandle != null)
			{
				_upStateHandle.Entered -= UpEntered;
			}
			_upStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint())
		{
			return;
		}
		_timerComponent = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
		_timerComponent.OnTimeout += Timeout;
		base.OnBodyHurt += delegate
		{
			if (!base.sprite.pause && !scare && base.sprite.timeScale > 0.0)
			{
				ChangeLine();
			}
		};
		ConnectStateSignals();
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		CharacterTimerComponent timerComponent = _timerComponent;
		if (timerComponent != null && !timerComponent.IsReleased)
		{
			_timerComponent.OnTimeout -= Timeout;
		}
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		if (base.IsInsideComponentBattlefield && !base.sprite.pause && base.sprite.timeScale > 0.0 && instance.hitpoints <= instance.hitpointsSave / 2.0)
		{
			SendStateEvent("ToScare");
		}
	}

	public void ScareEntered()
	{
		base.sprite.timeScale = timeScale * 2.0;
		if (base.inWater)
		{
			instance.maskFlags = 32;
			base.sprite.SetAnimation("Scaredwater", loop: false);
			base.sprite.AddAnimation("IdleWater", 0.0);
		}
		else
		{
			instance.maskFlags = 16;
			base.sprite.SetAnimation("Scared", loop: false);
			base.sprite.AddAnimation("IdleScare", 0.0);
		}
	}

	public void ScareProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		scare = true;
		RecoverHiddenNearDieState();
		_timerComponent.SetAlive(alive: true);
		if (!_timerComponent.IsRunning("Spawn"))
		{
			_timerComponent.Run("Spawn", 1.0);
		}
		if (instance.hitpoints >= instance.hitpointsSave)
		{
			instance.hitpoints = instance.hitpointsSave;
			if (!base.sprite.pause && base.sprite.timeScale > 0.0)
			{
				SendStateEvent("ToUp");
			}
		}
	}

	public void ScareExited()
	{
		scare = false;
		_timerComponent.SetAlive(alive: false);
		instance.maskFlags = 9;
	}

	public void UpEntered()
	{
		base.sprite.timeScale = timeScale * 2.0;
		if (base.inWater)
		{
			base.sprite.SetAnimation("UpWater", loop: false);
		}
		else
		{
			base.sprite.SetAnimation("Up", loop: false);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Up" || clip == "UpWater")
		{
			Walk();
		}
	}

	public void Timeout(string timerName)
	{
		if (timerName == "Spawn")
		{
			TryRecoverHiddenHealth();
			if (scare)
			{
				_timerComponent.Run("Spawn", 1.0);
			}
		}
	}

	private void RecoverHiddenNearDieState()
	{
		if (CanRecoverHiddenNearDie())
		{
			nearDie = false;
			instance.nearDie = false;
			destroyComponent?.EndDeathSettlement();
			targetRegistrationComponent?.NotifyTargetStateChanged();
		}
	}

	private bool CanRecoverHiddenNearDie()
	{
		if (scare && !die && !instance.die && instance.hitpoints > 0.0)
		{
			if (!nearDie)
			{
				return instance.nearDie;
			}
			return true;
		}
		return false;
	}

	private void TryRecoverHiddenHealth()
	{
		if (!scare || die || instance.die || instance.hitpoints <= 0.0)
		{
			return;
		}
		RecoverHiddenNearDieState();
		if (!base.sprite.pause && !(base.sprite.timeScale <= 0.0))
		{
			Health(25.0);
			if (instance.hitpoints > instance.hitpointsSave)
			{
				instance.hitpoints = instance.hitpointsSave;
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary { ["scare"] = scare };
	}

	public override void ImportVariantSave(Dictionary data)
	{
		scare = data.GetValueOrDefault("scare", false).AsBool();
	}
}
