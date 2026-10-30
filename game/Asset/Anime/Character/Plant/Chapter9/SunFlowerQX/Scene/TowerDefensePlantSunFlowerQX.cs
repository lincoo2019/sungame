using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantSunFlowerQX : TowerDefensePlant, INetworkSpawnStateReceiver
{
	private const double ReviveTime = 30.0;

	private const int SpikeFlag = 16;

	private const int BaseMaskFlags = 8;

	private ProduceComponent _produceComponent;

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

	private double _produceInterval = 25.0;

	private int _sunNum = 25;

	private string _produceType = "QXSun";

	[Export(PropertyHint.None, "")]
	public double produceInterval
	{
		get
		{
			return _produceInterval;
		}
		set
		{
			_produceInterval = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceInterval = (float)value;
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public int sunNum
	{
		get
		{
			return _sunNum;
		}
		set
		{
			_sunNum = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.num = value;
				}
			}
		}
	}

	[Export(PropertyHint.Enum, "Sun,BrainSun,JalaSun,Coin,QXSun")]
	public string produceType
	{
		get
		{
			return _produceType;
		}
		set
		{
			_produceType = value;
			if (IsNodeReady() && _produceComponent != null)
			{
				ProduceComponent produceComponent = _produceComponent;
				if (produceComponent != null && !produceComponent.IsReleased)
				{
					_produceComponent.produceType = value;
				}
			}
		}
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode && inGame)
		{
			_originalPhysiqueTypeFlags = instance.physiqueTypeFlags;
			_originalMaskFlags = instance.maskFlags;
			_originalCollisionFlags = instance.collisionFlags;
			_produceComponent = componentManager.GetRuntime<ProduceComponent>();
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.produceInterval = (float)produceInterval;
				_produceComponent.num = sunNum;
				_produceComponent.produceType = produceType;
			}
			_downState = base.StateMachine?.GetStateById("plant.sunflower_qx.down");
			_downIdleState = base.StateMachine?.GetStateById("plant.sunflower_qx.down_idle");
			_upState = base.StateMachine?.GetStateById("plant.sunflower_qx.up");
			ConnectStateSignals();
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
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
			_produceComponent?.SetAlive(alive: false);
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
		_produceComponent?.SetAlive(alive: true);
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
			ProduceComponent produceComponent = _produceComponent;
			if (produceComponent != null && !produceComponent.IsReleased)
			{
				_produceComponent.SetAlive(alive: false);
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
			["originalMaskFlags"] = _originalMaskFlags,
			["produceInterval"] = produceInterval,
			["sunNum"] = sunNum
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
		produceInterval = (data.ContainsKey("produceInterval") ? data["produceInterval"].AsDouble() : 25.0);
		sunNum = (data.ContainsKey("sunNum") ? data["sunNum"].AsInt32() : 25);
		if (isDowned && IsNodeReady())
		{
			SetupBaseState();
		}
	}
}
