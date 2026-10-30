using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantSunTransfer : TowerDefensePlant
{
	private StateHandle _shootState;

	private StateHandle _transferState;

	private bool _stateSignalsConnected;

	private bool _shootPending;

	private bool _produced;

	private bool _restoredFromProgress;

	private bool _over;

	private CharacterTimerComponent _lifetimeTimer;

	public override void PrepareForProgressRestore()
	{
		base.PrepareForProgressRestore();
		_restoredFromProgress = true;
	}

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame)
		{
			return;
		}
		useIdleAnimeReset = false;
		_lifetimeTimer = componentManager.GetRuntime<CharacterTimerComponent>("character.timer");
		_lifetimeTimer.OnTimeout += LifetimeTimeout;
		if (!_lifetimeTimer.IsRunning("Destroy"))
		{
			_lifetimeTimer.Run("Destroy");
		}
		if (!_produced && !_restoredFromProgress)
		{
			_produced = true;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(spriteGroup);
			if (instance.hypnoses)
			{
				BrainSunCreate(logicalGlobalPosition, 300L);
			}
			else
			{
				SunCreate(logicalGlobalPosition, 300L);
			}
		}
		_shootState = base.StateMachine?.GetStateById("plant.sun_transfer.shoot");
		_transferState = base.StateMachine?.GetStateById("plant.sun_transfer.transfer");
		ConnectStateSignals();
		_shootPending = true;
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		CharacterTimerComponent lifetimeTimer = _lifetimeTimer;
		if (lifetimeTimer != null && !lifetimeTimer.IsReleased)
		{
			_lifetimeTimer.OnTimeout -= LifetimeTimeout;
		}
		_lifetimeTimer = null;
		base._ExitTree();
	}

	private void LifetimeTimeout(string timerName)
	{
		if (!(timerName != "Destroy"))
		{
			Destroy();
		}
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			StateHandle shootState = _shootState;
			if (shootState != null && shootState.IsValid)
			{
				_shootState.Entered += ShootEntered;
				_shootState.PhysicsProcessing += ShootProcessing;
			}
			StateHandle transferState = _transferState;
			if (transferState != null && transferState.IsValid)
			{
				_transferState.Entered += TransferEntered;
				_transferState.PhysicsProcessing += TransferProcessing;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			StateHandle shootState = _shootState;
			if (shootState != null && shootState.IsValid)
			{
				_shootState.Entered -= ShootEntered;
				_shootState.PhysicsProcessing -= ShootProcessing;
			}
			StateHandle transferState = _transferState;
			if (transferState != null && transferState.IsValid)
			{
				_transferState.Entered -= TransferEntered;
				_transferState.PhysicsProcessing -= TransferProcessing;
			}
			_shootState = null;
			_transferState = null;
			_stateSignalsConnected = false;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		if (_shootPending)
		{
			_shootPending = false;
			SendStateEvent("ToShoot");
		}
	}

	private void ShootEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("Shooting"))
		{
			base.sprite.SetAnimation("Shooting", loop: false);
		}
	}

	private void ShootProcessing(double delta)
	{
		if (base.sprite != null)
		{
			base.sprite.timeScale = timeScale;
		}
	}

	private void TransferEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("IdleB"))
		{
			base.sprite.SetAnimation("IdleB");
		}
	}

	private void TransferProcessing(double delta)
	{
		if (base.sprite != null)
		{
			base.sprite.timeScale = timeScale;
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Shooting")
		{
			SendStateEvent("ToTransfer");
		}
	}

	public override void DestroySet()
	{
		if (!_over)
		{
			_over = true;
			Vector2 logicalGlobalPosition = GetLogicalGlobalPosition(spriteGroup);
			ReplicatedEventSunCreate(logicalGlobalPosition, 300L, !instance.hypnoses);
			base.DestroySet();
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["over"] = _over,
			["produced"] = _produced
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_over = data.GetValueOrDefault("over", false).AsBool();
		_produced = data.GetValueOrDefault("produced", false).AsBool();
	}
}
