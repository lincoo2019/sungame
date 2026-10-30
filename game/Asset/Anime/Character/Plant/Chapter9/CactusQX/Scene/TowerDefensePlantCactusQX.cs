using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantCactusQX : TowerDefensePlant, INetworkSpawnStateReceiver
{
	private const double ReviveTime = 30.0;

	private const int SpikeFlag = 16;

	private const int BaseMaskFlags = 8;

	private FireComponentExtendCactus fireComponentExtendCactus;

	private FireComponent fireComponent;

	private StateHandle _downState;

	private StateHandle _downIdleState;

	private StateHandle _upState;

	private bool _stateSignalsConnected;

	private double _fireInterval = 2.0;

	private int _fireNum = 1;

	private string _projectileName = "SpikeQX";

	public bool isDowned;

	private double _reviveTimer;

	private int _originalPhysiqueTypeFlags;

	private int _originalMaskFlags;

	private int _originalCollisionFlags;

	private bool _baseSetupDone;

	[Export(PropertyHint.None, "")]
	public double fireInterval
	{
		get
		{
			return _fireInterval;
		}
		set
		{
			_fireInterval = value;
			ApplyFireComponentSettings();
		}
	}

	[Export(PropertyHint.None, "")]
	public int fireNum
	{
		get
		{
			return _fireNum;
		}
		set
		{
			_fireNum = value;
			ApplyFireComponentSettings();
		}
	}

	[Export(PropertyHint.None, "")]
	public string projectileName
	{
		get
		{
			return _projectileName;
		}
		set
		{
			_projectileName = value;
			ApplyFireComponentSettings();
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
			fireComponentExtendCactus = componentManager.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
			fireComponent = componentManager.GetRuntime<FireComponent>("character.fire");
			ApplyFireComponentSettings();
			FireComponent obj = fireComponent;
			if (obj != null && !obj.IsReleased)
			{
				fireComponent.onlyEmitSignal = true;
				fireComponent.OnFireVolley += FireVolley;
			}
			FireComponent obj2 = fireComponent;
			if (obj2 != null && !obj2.IsReleased)
			{
				fireComponent.alive = true;
			}
			FireComponentExtendCactus obj3 = fireComponentExtendCactus;
			if (obj3 != null && !obj3.IsReleased)
			{
				fireComponentExtendCactus.alive = true;
			}
			_downState = base.StateMachine?.GetStateById("plant.cactusqx.down");
			_downIdleState = base.StateMachine?.GetStateById("plant.cactusqx.down_idle");
			_upState = base.StateMachine?.GetStateById("plant.cactusqx.up");
			ConnectStateSignals();
		}
	}

	public override void _ExitTree()
	{
		FireComponent obj = fireComponent;
		if (obj != null && !obj.IsReleased)
		{
			fireComponent.OnFireVolley -= FireVolley;
		}
		DisconnectStateSignals();
		base._ExitTree();
	}

	private void ApplyFireComponentSettings()
	{
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.fireInterval = (float)_fireInterval;
			this.fireComponent.fireNum = _fireNum;
			if (this.fireComponent.fireCheckList.Count > 0 && this.fireComponent.fireCheckList[0].projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
			{
				fireComponentProjectileSingle.projectileName = _projectileName;
			}
			if (this.fireComponent.fireCheckList.Count > 1 && this.fireComponent.fireCheckList[1].projectile is FireComponentProjectileSingle fireComponentProjectileSingle2)
			{
				fireComponentProjectileSingle2.projectileName = _projectileName;
			}
		}
	}

	private void FireVolley(ulong randomSeed)
	{
		FireComponentCheckConfig runningCheck = fireComponent.runningCheck;
		if (!GodotObject.IsInstanceValid(runningCheck) || !GodotObject.IsInstanceValid(runningCheck.projectile))
		{
			return;
		}
		TowerDefenseProjectileCreateData projectile = runningCheck.projectile.GetProjectile();
		if (projectile == null)
		{
			return;
		}
		int collisionFlags = runningCheck.GetCollisionFlags();
		int num = ResolveVolleyBulletCount(runningCheck, _fireNum);
		if (num <= 0)
		{
			return;
		}
		for (int i = 0; i < num; i++)
		{
			BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
			{
				flipXOverride = (base.Scale.X < 0f)
			};
			if (i != 0)
			{
				overrides.spawnTweenOffset = new Vector2(i * 60, 0f);
				overrides.spawnTweenDuration = 0.03f;
				overrides.spawnTweenEase = Tween.EaseType.Out;
				overrides.spawnTweenTrans = Tween.TransitionType.Quad;
			}
			fireComponent.CreateProjectile(0, new Vector2(300f, 0f), projectile, collisionFlags, base.camp, Vector2.Zero, 0, filterByLine: false, overrides);
		}
	}

	internal static int ResolveVolleyBulletCount(FireComponentCheckConfig activeCheck, int fireNum)
	{
		if (!GodotObject.IsInstanceValid(activeCheck))
		{
			return 0;
		}
		bool flag = !activeCheck.useParentCollision && (activeCheck.collisionFlags & 2) != 0;
		return fireNum + (flag ? 1 : 0);
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
			FireComponent obj = fireComponent;
			if (obj != null && !obj.IsReleased)
			{
				fireComponent.alive = false;
			}
			FireComponentExtendCactus obj2 = fireComponentExtendCactus;
			if (obj2 != null && !obj2.IsReleased)
			{
				fireComponentExtendCactus.alive = false;
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
		else if (fireComponentExtendCactus != null && fireComponentExtendCactus.IsUp())
		{
			base.sprite.SetAnimation("IdleHigh", loop: true, 0.1);
		}
		else
		{
			base.sprite.SetAnimation("Idle", loop: true, 0.1);
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
		FireComponent obj = fireComponent;
		if (obj != null && !obj.IsReleased)
		{
			fireComponent.alive = true;
		}
		FireComponentExtendCactus obj2 = fireComponentExtendCactus;
		if (obj2 != null && !obj2.IsReleased)
		{
			fireComponentExtendCactus.alive = true;
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
			FireComponent obj = fireComponent;
			if (obj != null && !obj.IsReleased)
			{
				fireComponent.alive = false;
			}
		}
		if (isDowned)
		{
			FireComponentExtendCactus obj2 = fireComponentExtendCactus;
			if (obj2 != null && !obj2.IsReleased)
			{
				fireComponentExtendCactus.alive = false;
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
			["fireNum"] = fireNum,
			["projectileName"] = projectileName,
			["fireInterval"] = fireInterval,
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
		fireNum = data.GetValueOrDefault("fireNum", 1).AsInt32();
		projectileName = data.GetValueOrDefault("projectileName", "SpikeQX").AsString();
		fireInterval = data.GetValueOrDefault("fireInterval", 2.0).AsDouble();
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
