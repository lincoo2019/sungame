using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantWallnutQX : TowerDefensePlant, INetworkSpawnStateReceiver
{
	private const double ReviveTime = 30.0;

	private const int SpikeFlag = 16;

	private const int BaseMaskFlags = 8;

	private AttackComponent _attackComponent;

	private StateHandle _downState;

	private StateHandle _downIdleState;

	private StateHandle _upState;

	private bool _stateSignalsConnected;

	public bool isDowned;

	private double _reviveTimer;

	private int _originalPhysiqueTypeFlags;

	private int _originalMaskFlags;

	private int _originalCollisionFlags;

	private bool _baseSetupDone;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_originalPhysiqueTypeFlags = instance.physiqueTypeFlags;
			_originalMaskFlags = instance.maskFlags;
			_originalCollisionFlags = instance.collisionFlags;
			_attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			AttackComponent attackComponent = _attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				_attackComponent.OnAttack += Attack;
			}
			_downState = base.StateMachine?.GetStateById("plant.wallnut_qx.down");
			_downIdleState = base.StateMachine?.GetStateById("plant.wallnut_qx.down_idle");
			_upState = base.StateMachine?.GetStateById("plant.wallnut_qx.up");
			ConnectStateSignals();
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.OnAttack -= Attack;
		}
		base._ExitTree();
	}

	private void ConnectStateSignals()
	{
		if (!_stateSignalsConnected)
		{
			StateHandle downState = _downState;
			if (downState != null && downState.IsValid)
			{
				_downState.Entered += DownEntered;
			}
			StateHandle downIdleState = _downIdleState;
			if (downIdleState != null && downIdleState.IsValid)
			{
				_downIdleState.Entered += DownIdleEntered;
				_downIdleState.PhysicsProcessing += DownIdleProcessing;
			}
			StateHandle upState = _upState;
			if (upState != null && upState.IsValid)
			{
				_upState.Entered += UpEntered;
			}
			_stateSignalsConnected = true;
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			StateHandle downState = _downState;
			if (downState != null && downState.IsValid)
			{
				_downState.Entered -= DownEntered;
			}
			StateHandle downIdleState = _downIdleState;
			if (downIdleState != null && downIdleState.IsValid)
			{
				_downIdleState.Entered -= DownIdleEntered;
				_downIdleState.PhysicsProcessing -= DownIdleProcessing;
			}
			StateHandle upState = _upState;
			if (upState != null && upState.IsValid)
			{
				_upState.Entered -= UpEntered;
			}
			_stateSignalsConnected = false;
		}
	}

	private void Attack()
	{
		if (!isDowned)
		{
			AudioManager.Instance.AudioPlay("Spike");
			_attackComponent.AttackEventExecute();
		}
	}

	public void SetupBaseState()
	{
		if (!_baseSetupDone)
		{
			_baseSetupDone = true;
			isDowned = true;
			die = false;
			nearDie = false;
			instance.die = false;
			instance.nearDie = false;
			destroyComponent?.EndDeathSettlement();
			instance.invincible = false;
			instance.invincibleHurt = false;
			instance.invincibleSmash = false;
			instance.hitpoints = instance.hitpointsSave;
			instance.collisionFlags = _originalCollisionFlags;
			instance.maskFlags = 8;
			instance.physiqueTypeFlags = _originalPhysiqueTypeFlags | 0x10;
			instance.spikeHurt = instance.hitpoints;
			AttackComponent attackComponent = _attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				_attackComponent.alive = false;
			}
			SendStateEvent("ToDown");
		}
	}

	public override void IdleEntered()
	{
		base.IdleEntered();
		if (isDowned)
		{
			SetupBaseState();
		}
	}

	public override void Destroy(bool freeInstance = true)
	{
		if (!isDowned && !isShovel && (!die || !(instance.hitpoints > 0.0)))
		{
			SetupBaseState();
		}
		else
		{
			base.Destroy(freeInstance);
		}
	}

	public void RestoreNormalState()
	{
		isDowned = false;
		_baseSetupDone = false;
		instance.maskFlags = _originalMaskFlags;
		instance.physiqueTypeFlags = _originalPhysiqueTypeFlags;
		instance.spikeHurt = 0.0;
		RestoreFullHealthAfterRevive();
		AttackComponent attackComponent = _attackComponent;
		if (attackComponent != null && !attackComponent.IsReleased)
		{
			_attackComponent.alive = true;
		}
	}

	private void DownEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("Down"))
		{
			base.sprite.SetAnimation("Down", loop: false);
		}
	}

	private void DownIdleEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("DownIdle"))
		{
			base.sprite.SetAnimation("DownIdle");
		}
		if (_reviveTimer <= 0.0 || _reviveTimer > 30.0)
		{
			_reviveTimer = 30.0;
		}
		if (isDowned)
		{
			AttackComponent attackComponent = _attackComponent;
			if (attackComponent != null && !attackComponent.IsReleased)
			{
				_attackComponent.alive = false;
			}
		}
	}

	private void DownIdleProcessing(double delta)
	{
		if (base.sprite != null)
		{
			base.sprite.timeScale = timeScale;
		}
		_reviveTimer -= delta;
		if (_reviveTimer <= 0.0)
		{
			SendStateEvent("ToUp");
		}
	}

	private void UpEntered()
	{
		if (base.sprite != null && base.sprite.HasClip("Up"))
		{
			base.sprite.SetAnimation("Up", loop: false);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Down")
		{
			SendStateEvent("ToDownIdle");
		}
		else if (clip == "Up")
		{
			RestoreNormalState();
			SendStateEvent("ToRevived");
		}
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		bool flag = isDowned;
		isDowned = data?.GetValueOrDefault("isDowned", false).AsBool() ?? false;
		if (isDowned && !flag && IsNodeReady())
		{
			SetupBaseState();
		}
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return new Dictionary { ["isDowned"] = isDowned };
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		isDowned = data?.GetValueOrDefault("isDowned", false).AsBool() ?? false;
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return isDowned ? 1 : 0;
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["isDowned"] = isDowned,
			["reviveTimer"] = _reviveTimer,
			["originalPhysiqueTypeFlags"] = _originalPhysiqueTypeFlags,
			["originalMaskFlags"] = _originalMaskFlags
		};
	}

	public override bool ImportVariantSaveWhenEmpty()
	{
		return true;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		isDowned = data.GetValueOrDefault("isDowned", false).AsBool();
		_reviveTimer = data.GetValueOrDefault("reviveTimer", 30.0).AsDouble();
		_originalPhysiqueTypeFlags = data.GetValueOrDefault("originalPhysiqueTypeFlags", base.config.physiqueTypeFlags).AsInt32();
		_originalMaskFlags = data.GetValueOrDefault("originalMaskFlags", base.config.maskFlags).AsInt32();
		if (isDowned && IsNodeReady())
		{
			SetupBaseState();
		}
	}
}
