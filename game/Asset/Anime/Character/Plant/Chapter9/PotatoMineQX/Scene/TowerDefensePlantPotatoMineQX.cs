using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPotatoMineQX : TowerDefensePlant, INetworkSpawnStateReceiver
{
	private const double ReviveTime = 30.0;

	private const int SpikeFlag = 16;

	private const int BaseMaskFlags = 8;

	private PotatoComponent _potatoComponent;

	private ExplodeComponent _explodeComponent;

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

	private double _readyTime = 15.0;

	[Export(PropertyHint.None, "")]
	public double readyTime
	{
		get
		{
			return _readyTime;
		}
		set
		{
			_readyTime = value;
			if (!IsNodeReady())
			{
				return;
			}
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				PotatoComponent potatoComponent2 = _potatoComponent;
				if (potatoComponent2 != null && !potatoComponent2.IsReleased)
				{
					_potatoComponent.readyTime = (float)value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_potatoComponent = componentManager.GetRuntime<PotatoComponent>();
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.readyTime = (float)readyTime;
			}
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.OnExplode += Explode;
			}
			_originalPhysiqueTypeFlags = instance.physiqueTypeFlags;
			_originalMaskFlags = instance.maskFlags;
			_originalCollisionFlags = instance.collisionFlags;
			_downState = base.StateMachine?.GetStateById("plant.potatomine_qx.down");
			_downIdleState = base.StateMachine?.GetStateById("plant.potatomine_qx.down_idle");
			_upState = base.StateMachine?.GetStateById("plant.potatomine_qx.up");
			ConnectStateSignals();
			CallDeferred("InitializePotatoAlive");
			if (isDowned)
			{
				SetupBaseState();
			}
		}
	}

	private void InitializePotatoAlive()
	{
		bool alive = !isDowned;
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.SetAlive(alive);
		}
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.SetAlive(alive);
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
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

	public void Explode()
	{
		if (!isDowned)
		{
			if (base.sprite != null && base.sprite.HasClip("Mashed"))
			{
				base.sprite.SetAnimation("Mashed", loop: false);
			}
			SetupBaseState();
		}
	}

	public void ReadyRise()
	{
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.ReadyRise();
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
			PotatoComponent potatoComponent = _potatoComponent;
			if (potatoComponent != null && !potatoComponent.IsReleased)
			{
				_potatoComponent.over = true;
				_potatoComponent.SetAlive(alive: false);
			}
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.SetAlive(false);
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
		if (!isSmash && !isDowned && !isShovel && (!die || !(instance.hitpoints > 0.0)))
		{
			SetupBaseState();
		}
		else
		{
			base.Destroy(freeInstance);
		}
	}

	public override void Idle()
	{
		if (!isDowned)
		{
			base.Idle();
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
			CallDeferred("ReadyRise");
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
		PotatoComponent potatoComponent = _potatoComponent;
		if (potatoComponent != null && !potatoComponent.IsReleased)
		{
			_potatoComponent.over = false;
			_potatoComponent.rise = false;
			_potatoComponent.SetAlive(alive: true);
		}
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.SetAlive(true);
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
			["readyTime"] = readyTime,
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
		readyTime = (data.ContainsKey("readyTime") ? data["readyTime"].AsDouble() : 15.0);
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
