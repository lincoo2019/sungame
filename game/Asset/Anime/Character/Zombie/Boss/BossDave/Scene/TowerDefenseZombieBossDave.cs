using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBossDave : TowerDefenseZombie, INetworkSpawnStateReceiver
{
	private const int BossDaveSaveVersion = 4;

	private const string DoomShieldArmorName = "BossDaveDoomShield";

	private StateHandle _headIdleStateHandle;

	private StateHandle _headAttackStateHandle;

	private StateHandle _headExitedStateHandle;

	private StateHandle _spawnStateHandle;

	private StateHandle _stompStateHandle;

	private StateHandle _bungeeStateHandle;

	private StateHandle _rvStateHandle;

	private StateHandle _plantStateHandle;

	private StateHandle _missileStateHandle;

	private StateHandle _doomStartStateHandle;

	private StateHandle _doomIdleStateHandle;

	private StateHandle _doomEndStateHandle;

	private StateHandle _doomCancelStateHandle;

	private bool _stateSignalsConnected;

	private bool _bossGameplayInitialized;

	private static PackedScene _FIRE_BALL;

	private static PackedScene _ICE_BALL;

	private static PackedScene _EXPLOSION;

	private static PackedScene _JACK_MISSILE;

	private static PackedScene _DOOMSHROOM_EXPLOSION;

	private static PackedScene _JACKBOX_EXPLOSION;

	private static Texture2D _CROSSHAIR_TEX;

	private AttackComponent _attackComponent1;

	private AttackComponent _attackComponent2;

	private AttackComponent _attackComponent3;

	private AttackComponent _attackComponent4;

	private AttackComponent _attackComponent5;

	[Export(PropertyHint.None, "")]
	public TowerDefenseZombieBossDaveAbilityConfig abilityConfig;

	public Godot.Collections.Array stateMethodList = new Godot.Collections.Array();

	public bool useEnterAnime = true;

	public string stateNow = "";

	public int spawnZombieNumOnce = 5;

	public int spawnZombieNumNow;

	public double spawnTime = 5.0;

	public int spawnLine = 1;

	public double spawnRestTime = 4.0;

	public double spawnRestTimer;

	public string spawnZomie;

	public int spawnNum;

	public bool headAttackOver;

	public int headAttackLine = 1;

	public bool headAttackIsFire = true;

	public double headAttackRestTime = 10.0;

	public double headAttackRestTimer;

	public bool bungeeIsSpawn;

	public Godot.Collections.Array bungeeList = new Godot.Collections.Array();

	public int stompId = -1;

	public Vector2I rvPos;

	public bool isRest = true;

	public double restTime = 5.0;

	public double restTimer;

	public int headIdleNum;

	public int stage;

	public string activeCrouchSkill = "";

	public Array<string> pendingCrouchShowcases = new Array<string>();

	public Array<Vector2I> missileTargets = new Array<Vector2I>();

	public int missileTargetIndex;

	public double doomChargeRemaining;

	private bool _missileAttackActive;

	private bool _plantAttackActive;

	private bool _plantSpawnResolved;

	private bool _doomCharging;

	private bool _doomAttackResolved;

	private bool _rvAttackArmed;

	private bool _missileShowcaseQueued;

	private bool _doomShowcaseQueued;

	private bool _doomShieldSignalsConnected;

	private bool _removingDoomShield;

	private bool _hasPendingLegacyDoomShieldDamage;

	private double _pendingLegacyDoomShieldDamage;

	private int _plantActionSequence;

	private int _missileActionSequence;

	private int _doomActionSequence;

	private int _networkSpecialStateRevision;

	private int _remoteMissileVisualIndex;

	private int _remoteMissileActionSequence = -1;

	private bool _clearLegacyMissilePauseOnFinalize;

	private bool _spawnLegacyDelayedMissileOnFinalize;

	private int _legacyMissileEventFrame = -1;

	private int missileResolvedMask;

	private bool _missileCrosshairTrackingActive;

	private bool _missileLifecycleSignalsConnected;

	private double _headAttackPauseRemaining;

	private int _headAttackPauseSequence;

	private int _remoteHeadAttackPauseSequence = -1;

	private bool _headAttackPauseOwned;

	private bool _preserveProgressStateOnNextWalk;

	private bool _clearLegacyHeadAttackPauseOnFinalize;

	private double _networkSyncAccumulator;

	private bool _hasPendingNetworkPresentation;

	private readonly System.Collections.Generic.Dictionary<int, Sprite2D> _missileCrosshairs = new System.Collections.Generic.Dictionary<int, Sprite2D>();

	private Array<string> _pendingBungeeNodeNames = new Array<string>();

	private static PackedScene FIRE_BALL => _FIRE_BALL ?? (_FIRE_BALL = GD.Load<PackedScene>("uid://ulproewvrqwb"));

	private static PackedScene ICE_BALL => _ICE_BALL ?? (_ICE_BALL = GD.Load<PackedScene>("uid://calwfqgd47fn7"));

	private static PackedScene EXPLOSION => _EXPLOSION ?? (_EXPLOSION = GD.Load<PackedScene>("uid://c8xarvk5gxpf0"));

	private static PackedScene JACK_MISSILE => _JACK_MISSILE ?? (_JACK_MISSILE = GD.Load<PackedScene>("uid://bednjhda3x6ky"));

	private static PackedScene DOOMSHROOM_EXPLOSION => _DOOMSHROOM_EXPLOSION ?? (_DOOMSHROOM_EXPLOSION = GD.Load<PackedScene>("uid://0hfxonqijrv0"));

	private static PackedScene JACKBOX_EXPLOSION => _JACKBOX_EXPLOSION ?? (_JACKBOX_EXPLOSION = GD.Load<PackedScene>("uid://cxnt2jbnk48fp"));

	private static Texture2D CROSSHAIR_TEX => _CROSSHAIR_TEX ?? (_CROSSHAIR_TEX = GD.Load<Texture2D>("uid://c07s22oke4426"));

	public override bool IsHardControlImmune => _doomCharging;

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
		_headIdleStateHandle = base.StateMachine?.GetStateById("zombie.boss.head_idle");
		_headAttackStateHandle = base.StateMachine?.GetStateById("zombie.boss.head_attack");
		_headExitedStateHandle = base.StateMachine?.GetStateById("zombie.boss.head_exited");
		_spawnStateHandle = base.StateMachine?.GetStateById("zombie.boss.spawn");
		_stompStateHandle = base.StateMachine?.GetStateById("zombie.boss.stomp");
		_bungeeStateHandle = base.StateMachine?.GetStateById("zombie.boss.bungee");
		_rvStateHandle = base.StateMachine?.GetStateById("zombie.boss.rv");
		_plantStateHandle = base.StateMachine?.GetStateById("zombie.boss.plant");
		_missileStateHandle = base.StateMachine?.GetStateById("zombie.boss.missile");
		_doomStartStateHandle = base.StateMachine?.GetStateById("zombie.boss.doom_start");
		_doomIdleStateHandle = base.StateMachine?.GetStateById("zombie.boss.doom_idle");
		_doomEndStateHandle = base.StateMachine?.GetStateById("zombie.boss.doom_end");
		_doomCancelStateHandle = base.StateMachine?.GetStateById("zombie.boss.doom_cancel");
		StateHandle headIdleStateHandle = _headIdleStateHandle;
		if (headIdleStateHandle == null || !headIdleStateHandle.IsValid)
		{
			return;
		}
		StateHandle headAttackStateHandle = _headAttackStateHandle;
		if (headAttackStateHandle == null || !headAttackStateHandle.IsValid)
		{
			return;
		}
		StateHandle headExitedStateHandle = _headExitedStateHandle;
		if (headExitedStateHandle == null || !headExitedStateHandle.IsValid)
		{
			return;
		}
		StateHandle spawnStateHandle = _spawnStateHandle;
		if (spawnStateHandle == null || !spawnStateHandle.IsValid)
		{
			return;
		}
		StateHandle stompStateHandle = _stompStateHandle;
		if (stompStateHandle == null || !stompStateHandle.IsValid)
		{
			return;
		}
		StateHandle bungeeStateHandle = _bungeeStateHandle;
		if (bungeeStateHandle == null || !bungeeStateHandle.IsValid)
		{
			return;
		}
		StateHandle rvStateHandle = _rvStateHandle;
		if (rvStateHandle == null || !rvStateHandle.IsValid)
		{
			return;
		}
		StateHandle plantStateHandle = _plantStateHandle;
		if (plantStateHandle == null || !plantStateHandle.IsValid)
		{
			return;
		}
		StateHandle missileStateHandle = _missileStateHandle;
		if (missileStateHandle == null || !missileStateHandle.IsValid)
		{
			return;
		}
		StateHandle doomStartStateHandle = _doomStartStateHandle;
		if (doomStartStateHandle == null || !doomStartStateHandle.IsValid)
		{
			return;
		}
		StateHandle doomIdleStateHandle = _doomIdleStateHandle;
		if (doomIdleStateHandle == null || !doomIdleStateHandle.IsValid)
		{
			return;
		}
		StateHandle doomEndStateHandle = _doomEndStateHandle;
		if (doomEndStateHandle != null && doomEndStateHandle.IsValid)
		{
			StateHandle doomCancelStateHandle = _doomCancelStateHandle;
			if (doomCancelStateHandle != null && doomCancelStateHandle.IsValid)
			{
				_headIdleStateHandle.Entered += HeadIdleEntered;
				_headIdleStateHandle.Exited += HeadIdleExited;
				_headIdleStateHandle.PhysicsProcessing += HeadIdleProcessing;
				_headAttackStateHandle.Entered += HeadAttackEntered;
				_headAttackStateHandle.Exited += HeadAttackExited;
				_headAttackStateHandle.PhysicsProcessing += HeadAttackProcessing;
				_headExitedStateHandle.Entered += HeadExitedEntered;
				_headExitedStateHandle.Exited += HeadExitedExited;
				_headExitedStateHandle.PhysicsProcessing += HeadExitedProcessing;
				_spawnStateHandle.Entered += SpawnEntered;
				_spawnStateHandle.Exited += SpawnExited;
				_spawnStateHandle.PhysicsProcessing += SpawnProcessing;
				_stompStateHandle.Entered += StompEntered;
				_stompStateHandle.Exited += StompExited;
				_stompStateHandle.PhysicsProcessing += StompProcessing;
				_bungeeStateHandle.Entered += BungeeEntered;
				_bungeeStateHandle.Exited += BungeeExited;
				_bungeeStateHandle.PhysicsProcessing += BungeeProcessing;
				_rvStateHandle.Entered += RVEntered;
				_rvStateHandle.Exited += RVExited;
				_rvStateHandle.PhysicsProcessing += RVProcessing;
				_plantStateHandle.Entered += PlantEntered;
				_plantStateHandle.Exited += PlantExited;
				_plantStateHandle.PhysicsProcessing += PlantProcessing;
				_missileStateHandle.Entered += MissileEntered;
				_missileStateHandle.Exited += MissileExited;
				_missileStateHandle.PhysicsProcessing += MissileProcessing;
				_doomStartStateHandle.Entered += DoomStartEntered;
				_doomStartStateHandle.Exited += DoomStartExited;
				_doomStartStateHandle.PhysicsProcessing += DoomStartProcessing;
				_doomIdleStateHandle.Entered += DoomIdleEntered;
				_doomIdleStateHandle.Exited += DoomIdleExited;
				_doomIdleStateHandle.PhysicsProcessing += DoomIdleProcessing;
				_doomEndStateHandle.Entered += DoomEndEntered;
				_doomEndStateHandle.Exited += DoomEndExited;
				_doomEndStateHandle.PhysicsProcessing += DoomEndProcessing;
				_doomCancelStateHandle.Entered += DoomCancelEntered;
				_doomCancelStateHandle.Exited += DoomCancelExited;
				_doomCancelStateHandle.PhysicsProcessing += DoomCancelProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_headIdleStateHandle != null)
			{
				_headIdleStateHandle.Entered -= HeadIdleEntered;
				_headIdleStateHandle.Exited -= HeadIdleExited;
				_headIdleStateHandle.PhysicsProcessing -= HeadIdleProcessing;
			}
			_headIdleStateHandle = null;
			if (_headAttackStateHandle != null)
			{
				_headAttackStateHandle.Entered -= HeadAttackEntered;
				_headAttackStateHandle.Exited -= HeadAttackExited;
				_headAttackStateHandle.PhysicsProcessing -= HeadAttackProcessing;
			}
			_headAttackStateHandle = null;
			if (_headExitedStateHandle != null)
			{
				_headExitedStateHandle.Entered -= HeadExitedEntered;
				_headExitedStateHandle.Exited -= HeadExitedExited;
				_headExitedStateHandle.PhysicsProcessing -= HeadExitedProcessing;
			}
			_headExitedStateHandle = null;
			if (_spawnStateHandle != null)
			{
				_spawnStateHandle.Entered -= SpawnEntered;
				_spawnStateHandle.Exited -= SpawnExited;
				_spawnStateHandle.PhysicsProcessing -= SpawnProcessing;
			}
			_spawnStateHandle = null;
			if (_stompStateHandle != null)
			{
				_stompStateHandle.Entered -= StompEntered;
				_stompStateHandle.Exited -= StompExited;
				_stompStateHandle.PhysicsProcessing -= StompProcessing;
			}
			_stompStateHandle = null;
			if (_bungeeStateHandle != null)
			{
				_bungeeStateHandle.Entered -= BungeeEntered;
				_bungeeStateHandle.Exited -= BungeeExited;
				_bungeeStateHandle.PhysicsProcessing -= BungeeProcessing;
			}
			_bungeeStateHandle = null;
			if (_rvStateHandle != null)
			{
				_rvStateHandle.Entered -= RVEntered;
				_rvStateHandle.Exited -= RVExited;
				_rvStateHandle.PhysicsProcessing -= RVProcessing;
			}
			_rvStateHandle = null;
			if (_plantStateHandle != null)
			{
				_plantStateHandle.Entered -= PlantEntered;
				_plantStateHandle.Exited -= PlantExited;
				_plantStateHandle.PhysicsProcessing -= PlantProcessing;
			}
			_plantStateHandle = null;
			if (_missileStateHandle != null)
			{
				_missileStateHandle.Entered -= MissileEntered;
				_missileStateHandle.Exited -= MissileExited;
				_missileStateHandle.PhysicsProcessing -= MissileProcessing;
			}
			_missileStateHandle = null;
			if (_doomStartStateHandle != null)
			{
				_doomStartStateHandle.Entered -= DoomStartEntered;
				_doomStartStateHandle.Exited -= DoomStartExited;
				_doomStartStateHandle.PhysicsProcessing -= DoomStartProcessing;
			}
			_doomStartStateHandle = null;
			if (_doomIdleStateHandle != null)
			{
				_doomIdleStateHandle.Entered -= DoomIdleEntered;
				_doomIdleStateHandle.Exited -= DoomIdleExited;
				_doomIdleStateHandle.PhysicsProcessing -= DoomIdleProcessing;
			}
			_doomIdleStateHandle = null;
			if (_doomEndStateHandle != null)
			{
				_doomEndStateHandle.Entered -= DoomEndEntered;
				_doomEndStateHandle.Exited -= DoomEndExited;
				_doomEndStateHandle.PhysicsProcessing -= DoomEndProcessing;
			}
			_doomEndStateHandle = null;
			if (_doomCancelStateHandle != null)
			{
				_doomCancelStateHandle.Entered -= DoomCancelEntered;
				_doomCancelStateHandle.Exited -= DoomCancelExited;
				_doomCancelStateHandle.PhysicsProcessing -= DoomCancelProcessing;
			}
			_doomCancelStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		DisconnectDoomShieldSignals();
		DisconnectMissileLifecycleSignals();
		ClearMissileCrosshairs();
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ResolveBossRuntimeComponents();
			InitializeBossGameplay();
			ConnectMissileLifecycleSignals();
			Callable.From(ApplyPendingBossNetworkPresentation).CallDeferred();
		}
	}

	public override void FinalizeProgressRestore()
	{
		string text = base.CurrentStateHandle?.StableId.ToString() ?? "";
		_preserveProgressStateOnNextWalk = text.StartsWith("zombie.boss.");
		base.FinalizeProgressRestore();
		ResolveSavedBungeeRelations();
		_hasPendingNetworkPresentation = true;
		ApplyPendingBossNetworkPresentation();
		if (_clearLegacyHeadAttackPauseOnFinalize && _headAttackPauseRemaining <= 0.0 && !_headAttackPauseOwned && spritePause && base.sprite?.clip == "HeadAttack4")
		{
			spritePause = false;
		}
		_clearLegacyHeadAttackPauseOnFinalize = false;
	}

	protected override bool IsProgressStateMachineDefinitionIdCompatible(string savedDefinitionId, string currentDefinitionId)
	{
		if (!base.IsProgressStateMachineDefinitionIdCompatible(savedDefinitionId, currentDefinitionId))
		{
			if (savedDefinitionId == "builtin.character.zombie.boss")
			{
				return currentDefinitionId == "builtin.character.zombie.boss_dave";
			}
			return false;
		}
		return true;
	}

	private void ResolveBossRuntimeComponents()
	{
		_attackComponent1 = componentManager.GetRuntime<AttackComponent>("character.attack.0");
		_attackComponent2 = componentManager.GetRuntime<AttackComponent>("character.attack.1");
		_attackComponent3 = componentManager.GetRuntime<AttackComponent>("character.attack.2");
		_attackComponent4 = componentManager.GetRuntime<AttackComponent>("character.attack.3");
		_attackComponent5 = componentManager.GetRuntime<AttackComponent>("character.attack.4");
	}

	private void InitializeBossGameplay()
	{
		if (!_bossGameplayInitialized && inGame && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			_bossGameplayInitialized = true;
			Vector2 size = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f, TowerDefenseManager.Instance.GetMapGridNum().Y) / new Vector2(1f, 2.5f);
			_attackComponent1.SetCheckAreaRectangleSize(0, size);
			_attackComponent2.SetCheckAreaRectangleSize(0, size);
			_attackComponent3.SetCheckAreaRectangleSize(0, size);
			_attackComponent4.SetCheckAreaRectangleSize(0, size);
			_attackComponent5.SetCheckAreaRectangleSize(0, TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(3f, 2f));
			Vector2 vector = TowerDefenseManager.Instance.GetMapGridSize() * new Vector2((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f, TowerDefenseManager.Instance.GetMapGridNum().Y) / new Vector2(1f, 4f);
			_attackComponent1.SetCheckAreaShapeLocalOrigin(0, new Vector2(-100f, (0f - vector.Y) * 1.5f));
			_attackComponent2.SetCheckAreaShapeLocalOrigin(0, new Vector2(-100f, (0f - vector.Y) * 0.5f));
			_attackComponent3.SetCheckAreaShapeLocalOrigin(0, new Vector2(-100f, vector.Y * 0.5f));
			_attackComponent4.SetCheckAreaShapeLocalOrigin(0, new Vector2(-100f, vector.Y * 1.5f));
			base.gridPos = new Vector2I(TowerDefenseManager.Instance.GetMapGridNum().X, (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 1);
			SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(base.gridPos));
			SetStateList();
			instance.invincible = true;
			instance.canBeCollection = false;
			targetRegistrationComponent.allLineCheck = false;
			targetRegistrationComponent.canProjectileCheck = false;
			instance.unUseBuffFlags = -1;
			ConnectStateSignals();
			ConnectDoomShieldSignals();
			RefreshMissileCrosshairs();
			WaitForPhysicsFrame();
		}
	}

	private void ConnectDoomShieldSignals()
	{
		if (!_doomShieldSignalsConnected && GodotObject.IsInstanceValid(instance))
		{
			instance.armorHitpointsEmpty += OnDoomShieldBroken;
			_doomShieldSignalsConnected = true;
		}
	}

	private void DisconnectDoomShieldSignals()
	{
		if (_doomShieldSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.armorHitpointsEmpty -= OnDoomShieldBroken;
			}
			_doomShieldSignalsConnected = false;
		}
	}

	private void ConnectMissileLifecycleSignals()
	{
		if (!_missileLifecycleSignalsConnected && GodotObject.IsInstanceValid(BulletField.Instance))
		{
			BulletField.Instance.OnBulletLifecycleEnded += OnMissileLifecycleEnded;
			_missileLifecycleSignalsConnected = true;
		}
	}

	private void DisconnectMissileLifecycleSignals()
	{
		if (_missileLifecycleSignalsConnected)
		{
			if (GodotObject.IsInstanceValid(BulletField.Instance))
			{
				BulletField.Instance.OnBulletLifecycleEnded -= OnMissileLifecycleEnded;
			}
			_missileLifecycleSignalsConnected = false;
		}
	}

	private async Task WaitForPhysicsFrame()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		useEnterAnime = true;
	}

	private bool ShouldFreezeBossGameplay()
	{
		if (Engine.IsEditorHint() || !inGame)
		{
			return false;
		}
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager) && towerDefenseManager.IsGameRunning())
		{
			return towerDefenseManager.pauseZombie;
		}
		return false;
	}

	private void ApplyBossGlobalPausePlayback()
	{
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
			base.sprite.pause = true;
			base.sprite.playBack = GodotObject.IsInstanceValid(towerDefenseManager) && (towerDefenseManager.backZombie || isGarlicBird);
		}
	}

	public override void BatchUpdate(double delta)
	{
		if (ShouldFreezeBossGameplay())
		{
			ApplyBossGlobalPausePlayback();
			return;
		}
		base.BatchUpdate(delta);
		if (Engine.IsEditorHint() || !inGame || !TowerDefenseManager.Instance.IsGameRunning() || IsBossGameplayPaused())
		{
			return;
		}
		UpdateHeadAttackPause(delta);
		if (Engine.GetPhysicsFrames() % 5 == 0L)
		{
			SetSpriteGroupShaderParameter("discardDownPos", 10000.0);
		}
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		_networkSyncAccumulator += delta;
		if (_networkSyncAccumulator >= 0.5)
		{
			_networkSyncAccumulator = 0.0;
			MarkBossStateDirty();
		}
		if (!isRest)
		{
			return;
		}
		if (restTimer < restTime)
		{
			restTimer += delta;
			return;
		}
		if (stateMethodList.Count == 0)
		{
			SetStateList();
		}
		if (stateMethodList.Count != 0)
		{
			restTimer = 0.0;
			stateNow = (string)stateMethodList[0];
			stateMethodList.RemoveAt(0);
			isRest = false;
			if (stateNow != "HeadIdle" && stateMethodList.Count == 0)
			{
				stateMethodList.Add("HeadIdle");
			}
			MarkBossStateDirty();
		}
	}

	public override void Walk()
	{
		if (_preserveProgressStateOnNextWalk)
		{
			_preserveProgressStateOnNextWalk = false;
		}
		else
		{
			Idle();
		}
	}

	public void HeadIdleEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		AudioManager.Instance.AudioPlay("Hydraulic");
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			headAttackRestTimer = 0.0;
		}
		if (!headAttackOver)
		{
			base.sprite.SetAnimation("HeadEnter", loop: false, 0.2);
			base.sprite.AddAnimation("HeadIdle", 0.0);
		}
		else
		{
			base.sprite.SetAnimation("HeadIdle", loop: true, 0.2);
		}
	}

	public void HeadIdleProcessing(double delta)
	{
		SetTransientRenderZIndex(1000, "zomboss-head-idle");
		base.sprite.timeScale = timeScale * 1.0;
		if (IsBossGameplayPaused() || !TowerDefenseManager.HasGameplayAuthority || base.sprite.pause)
		{
			return;
		}
		if (headAttackRestTimer < headAttackRestTime)
		{
			headAttackRestTimer += delta * timeScale;
			return;
		}
		headAttackRestTimer = 0.0;
		if (!headAttackOver)
		{
			StartNextCrouchSkill();
		}
		else
		{
			SendStateEvent("ToHeadExited");
		}
	}

	public void HeadIdleExited()
	{
	}

	public void HeadAttackEntered()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			headAttackOver = true;
		}
		SetTransientRenderZIndex((int)(headAttackLine * 15 + base.itemLayer), "zomboss-head-attack");
		ZombieBossDave zombieBossDave = base.sprite as ZombieBossDave;
		if (GodotObject.IsInstanceValid(zombieBossDave))
		{
			zombieBossDave.SetHeadAttack(headAttackLine);
			zombieBossDave.SetHeadAttackBall((activeCrouchSkill == "Fire") ? "Fire" : "Ice");
		}
		base.sprite.SetAnimation("HeadAttack4", loop: false, 0.2);
	}

	public void HeadAttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void HeadAttackExited()
	{
		ReleaseHeadAttackPause(markDirty: false);
	}

	public void HeadExitedEntered()
	{
		SetRetractedHeadState();
		base.sprite.SetAnimation("HeadExited", loop: false);
	}

	public void HeadExitedProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void HeadExitedExited()
	{
	}

	public void SpawnEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			spawnZomie = GetSpawnZombie();
			spawnLine = GD.RandRange(1, TowerDefenseManager.Instance.GetMapGridNum().Y);
			MarkBossStateDirty();
		}
		base.sprite.SetAnimation("Spawn1", loop: false, 0.2);
		ZombieBossDave zombieBossDave = base.sprite as ZombieBossDave;
		if (GodotObject.IsInstanceValid(zombieBossDave))
		{
			zombieBossDave.SetSpawn(spawnLine);
		}
	}

	public void SpawnProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void SpawnExited()
	{
	}

	public void StompEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			Array<int> availableStompIds = GetAvailableStompIds();
			stompId = ((availableStompIds.Count > 0) ? availableStompIds.PickRandom() : 3);
			MarkBossStateDirty();
		}
		switch (stompId)
		{
		case 1:
			base.sprite.SetAnimation("Stomp1", loop: false, 0.2);
			break;
		case 2:
			base.sprite.SetAnimation("Stomp2", loop: false, 0.2);
			break;
		case 3:
			base.sprite.SetAnimation("Stomp3", loop: false, 0.2);
			break;
		case 4:
			base.sprite.SetAnimation("Stomp4", loop: false, 0.2);
			break;
		}
	}

	public void StompProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void StompExited()
	{
	}

	public void BungeeEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		if (!base.IsProgressRestoreInFlight)
		{
			bungeeIsSpawn = false;
		}
		base.sprite.SetAnimation("BungeeEnter", loop: false, 0.2);
	}

	public void BungeeProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (IsBossGameplayPaused() || !TowerDefenseManager.HasGameplayAuthority || !bungeeIsSpawn)
		{
			return;
		}
		for (int num = bungeeList.Count - 1; num >= 0; num--)
		{
			if (!GodotObject.IsInstanceValid(bungeeList[num].AsGodotObject()))
			{
				bungeeList.RemoveAt(num);
				break;
			}
		}
		if (bungeeList.Count <= 0)
		{
			base.sprite.SetAnimation("BungeeExited", loop: false, 0.2);
			bungeeIsSpawn = false;
		}
	}

	public void BungeeExited()
	{
	}

	public void RVEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			_rvAttackArmed = false;
			rvPos = new Vector2I((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f - 1f), TowerDefenseManager.Instance.GetMapGridNum().Y - 1);
			MarkBossStateDirty();
		}
		base.sprite.SetAnimation("RV", loop: false, 0.2);
	}

	public void RVProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void RVExited()
	{
	}

	public void PlantEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			_plantAttackActive = true;
			_plantSpawnResolved = false;
			_plantActionSequence++;
			MarkBossStateDirty();
		}
		base.sprite.SetAnimation("PlantEnter", loop: false, 0.2);
	}

	public void PlantProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void PlantExited()
	{
	}

	public void MissileEntered()
	{
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			PrepareMissileAttack();
		}
		SetExposedHeadDamageableState();
		SetCrouchPresentation("Green");
		base.sprite.SetAnimation("Missile", loop: false, 0.2);
		CatchUpRemoteMissileVisuals(missileTargetIndex);
	}

	public void MissileProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void MissileExited()
	{
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			EndMissileAttack(markDirty: true, missileTargetIndex >= missileTargets.Count);
		}
	}

	public void DoomStartEntered()
	{
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			RemoveDoomShield();
			_doomCharging = true;
			_doomAttackResolved = false;
			doomChargeRemaining = abilityConfig?.doomChargeSeconds ?? 20.0;
			_doomActionSequence++;
			EnsureDoomShield(reset: true);
			MarkBossStateDirty();
		}
		else if (_doomCharging)
		{
			EnsureDoomShield(reset: false);
		}
		SetDoomDamageableState();
		SetCrouchPresentation("Purple");
		base.sprite.SetAnimation("DoomStart", loop: false, 0.2);
	}

	public void DoomStartProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		AdvanceDoomCharge(delta);
	}

	public void DoomStartExited()
	{
	}

	public void DoomIdleEntered()
	{
		SetDoomDamageableState();
		SetCrouchPresentation("Purple");
		base.sprite.SetAnimation("DoomIdle", loop: true, 0.2);
	}

	public void DoomIdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		AdvanceDoomCharge(delta);
	}

	public void DoomIdleExited()
	{
	}

	public void DoomEndEntered()
	{
		RemoveDoomShield();
		SetExposedHeadDamageableState();
		SetCrouchPresentation("Purple");
		base.sprite.SetAnimation("DoomEnd", loop: false, 0.2);
	}

	public void DoomEndProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void DoomEndExited()
	{
	}

	public void DoomCancelEntered()
	{
		RemoveDoomShield();
		SetExposedHeadDamageableState();
		SetCrouchPresentation("Purple");
		if (base.sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.PlayDriverDamage();
		}
		base.sprite.SetAnimation("DoomCancel", loop: false, 0.2);
	}

	public void DoomCancelProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void DoomCancelExited()
	{
	}

	public override void IdleEntered()
	{
		if (!inGame)
		{
			base.sprite.SetAnimation("HeadIdle");
			return;
		}
		instance.invincible = true;
		instance.canBeCollection = false;
		if (useEnterAnime)
		{
			base.sprite.SetAnimation("Enter", loop: false, 0.2);
			base.sprite.AddAnimation("Idle", 0.0);
			useEnterAnime = false;
		}
		else
		{
			base.sprite.SetAnimation("Idle", loop: true, 0.2);
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		StateRunning(delta);
	}

	public override void DieEntered()
	{
		bool flag = false;
		ZombieBossDave zombieBossDave = base.sprite as ZombieBossDave;
		if (base.sprite.clip == "HeadIdle" || base.sprite.clip == "HeadAttack4" || base.sprite.clip == "Missile" || base.sprite.clip == "DoomIdle" || base.sprite.clip == "DoomStart" || base.sprite.clip == "DoomEnd" || base.sprite.clip == "DoomCancel")
		{
			flag = true;
		}
		ReleaseHeadAttackPause(markDirty: false);
		_doomCharging = false;
		RemoveDoomShield();
		_plantAttackActive = false;
		EndMissileAttack(markDirty: false);
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			MarkBossStateDirty();
		}
		base.DieEntered();
		if (GodotObject.IsInstanceValid(zombieBossDave))
		{
			zombieBossDave.SetHeadAttack((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2, 0.5);
		}
		spritePause = false;
		SetTransientRenderZIndex((int)(headAttackLine * 15 + base.itemLayer), "zomboss-death");
		if (flag)
		{
			base.sprite.SetAnimation("HeadExited", loop: false, 0.2);
			base.sprite.AddAnimation("Death", 0.0, loop: false);
		}
		else
		{
			base.sprite.SetAnimation("Death", loop: false, 0.2);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 0.5;
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		ZombieBossDave zombieBossDave = base.sprite as ZombieBossDave;
		if (GodotObject.IsInstanceValid(zombieBossDave))
		{
			zombieBossDave.DamagePointSet(damagePointName);
		}
		switch (damagePointName)
		{
		case "Stage1":
			stage = 1;
			restTime = 4.5;
			spawnRestTime = 3.5;
			QueueCrouchShowcase("Missile");
			break;
		case "Stage2":
			stage = 2;
			restTime = 4.0;
			spawnRestTime = 3.0;
			QueueCrouchShowcase("Doom");
			break;
		case "Stage3":
			stage = 3;
			ExplosionMethod();
			break;
		case "Death":
			timeScaleInit = 3.0;
			AudioManager.Instance.AudioPlay("Bossexplosion");
			break;
		}
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			MarkBossStateDirty();
		}
	}

	public override void AnimeCompleted(string clip)
	{
		ZombieDeathComponent obj = zombieDeathComponent;
		if (obj != null && !obj.IsReleased && zombieDeathComponent.IsLandDeathAnimationClip(clip))
		{
			RemoveFromGroup("Zombie");
			return;
		}
		if (die)
		{
			HitBoxDestroy();
			Die();
		}
		if (base.sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.AnimeCompleted(clip);
		}
		if (clip == null)
		{
			return;
		}
		switch (clip.Length)
		{
		case 6:
			switch (clip[5])
			{
			case '1':
				if (!(clip == "Spawn1"))
				{
					if (!(clip == "Stomp1"))
					{
						break;
					}
					goto IL_02c5;
				}
				if (!die)
				{
					Idle();
				}
				break;
			case '2':
				if (!(clip == "Stomp2"))
				{
					break;
				}
				goto IL_02c5;
			case '3':
				if (!(clip == "Stomp3"))
				{
					break;
				}
				goto IL_02c5;
			case '4':
				{
					if (!(clip == "Stomp4"))
					{
						break;
					}
					goto IL_02c5;
				}
				IL_02c5:
				FinishStandingSpecial();
				break;
			}
			break;
		case 9:
			switch (clip[0])
			{
			case 'H':
				if (clip == "HeadEnter")
				{
					SetExposedHeadDamageableState();
				}
				break;
			case 'D':
				if (clip == "DoomStart" && TowerDefenseManager.HasGameplayAuthority && _doomCharging)
				{
					SendStateEvent("ToDoomIdle");
				}
				break;
			}
			break;
		case 10:
			switch (clip[5])
			{
			default:
				return;
			case 'x':
				if (!(clip == "HeadExited"))
				{
					return;
				}
				FreshZIndex();
				if (TowerDefenseManager.HasGameplayAuthority)
				{
					activeCrouchSkill = "";
					SetStateList();
					isRest = true;
					restTimer = 2.0;
					MarkBossStateDirty();
					if (!die)
					{
						Idle();
					}
				}
				return;
			case 'E':
				if (clip == "PlantEnter")
				{
					base.sprite.SetAnimation("PlantLeave", loop: false);
				}
				return;
			case 'L':
				if (clip == "PlantLeave")
				{
					if (TowerDefenseManager.HasGameplayAuthority)
					{
						_plantAttackActive = false;
						MarkBossStateDirty();
					}
					FinishStandingSpecial();
				}
				return;
			case 'a':
				break;
			}
			if (!(clip == "DoomCancel"))
			{
				break;
			}
			goto IL_0367;
		case 7:
			switch (clip[0])
			{
			default:
				return;
			case 'M':
				if (clip == "Missile" && TowerDefenseManager.HasGameplayAuthority)
				{
					EndMissileAttack(markDirty: true, preserveInFlightCrosshairs: true);
					SendStateEvent("ToHeadExited");
				}
				return;
			case 'D':
				break;
			}
			if (!(clip == "DoomEnd"))
			{
				break;
			}
			goto IL_0367;
		case 11:
			if (clip == "HeadAttack4" && TowerDefenseManager.HasGameplayAuthority)
			{
				SendStateEvent("ToHeadIdle");
			}
			break;
		case 12:
			if (clip == "BungeeExited")
			{
				FreshZIndex();
				FinishStandingSpecial();
			}
			break;
		case 2:
			if (clip == "RV")
			{
				if (TowerDefenseManager.HasGameplayAuthority && _rvAttackArmed)
				{
					_rvAttackArmed = false;
					MarkBossStateDirty();
				}
				FinishStandingSpecial();
			}
			break;
		case 3:
		case 4:
		case 5:
		case 8:
			break;
			IL_0367:
			if (TowerDefenseManager.HasGameplayAuthority)
			{
				SendStateEvent("ToHeadExited");
			}
			break;
		}
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		base.AnimeEvent(command, argument);
		if (command == null)
		{
			return;
		}
		switch (command.Length)
		{
		case 5:
			switch (command[1])
			{
			case 'p':
				if (command == "spawn")
				{
					ZombieSpawn();
				}
				break;
			case 't':
				if (!(command == "stomp"))
				{
					break;
				}
				AudioManager.Instance.AudioPlay("GargantuarThump");
				if (TowerDefenseManager.HasGameplayAuthority)
				{
					switch (stompId)
					{
					case 1:
						_attackComponent1.SmashAttackAll(100000.0);
						break;
					case 2:
						_attackComponent2.SmashAttackAll(100000.0);
						break;
					case 3:
						_attackComponent3.SmashAttackAll(100000.0);
						break;
					case 4:
						_attackComponent4.SmashAttackAll(100000.0);
						break;
					}
				}
				break;
			}
			break;
		case 10:
			switch (command[0])
			{
			default:
				return;
			case 's':
				break;
			case 'h':
				if (command == "headAttack")
				{
					BallSpawn();
					if (TowerDefenseManager.HasGameplayAuthority)
					{
						BeginHeadAttackPause();
					}
				}
				return;
			}
			if (!(command == "spawnPlant"))
			{
				break;
			}
			goto IL_0198;
		case 7:
			switch (command[0])
			{
			case 't':
				if (command == "throwRV")
				{
					ZombieBossDave zombieBossDave = base.sprite as ZombieBossDave;
					if (GodotObject.IsInstanceValid(zombieBossDave))
					{
						zombieBossDave.SetRVVisible(visible: true);
					}
					RVSpawn();
				}
				break;
			case 'e':
				if (command == "explode")
				{
					ExplosionMethod();
				}
				break;
			case 'm':
				if (command == "missile")
				{
					SpawnNextMissile();
				}
				break;
			}
			break;
		case 8:
			switch (command[0])
			{
			case 'R':
				if (command == "RVAttack")
				{
					AudioManager.Instance.AudioPlay("RVThrow");
					if (TowerDefenseManager.HasGameplayAuthority && _rvAttackArmed)
					{
						_attackComponent5.SmashAttackAll(100000.0);
						_rvAttackArmed = false;
						MarkBossStateDirty();
					}
				}
				break;
			case 'f':
				if (command == "footstep")
				{
					AudioManager.Instance.AudioPlay("GargantuarThump");
				}
				break;
			}
			break;
		case 11:
			if (command == "spawnBubgee")
			{
				BungeeSpawn();
			}
			break;
		case 9:
			if (!(command == "plantDrop"))
			{
				break;
			}
			goto IL_0198;
		case 14:
			if (command == "headAttackOver")
			{
				ZombieBossDave zombieBossDave2 = base.sprite as ZombieBossDave;
				if (GodotObject.IsInstanceValid(zombieBossDave2))
				{
					zombieBossDave2.SetHeadAttack((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2, 0.5);
				}
			}
			break;
		case 4:
			if (command == "doom")
			{
				ReleaseDoomAttack();
			}
			break;
		case 6:
		case 12:
		case 13:
			break;
			IL_0198:
			SpawnHypnotizedPlants();
			break;
		}
	}

	private void UpdateHeadAttackPause(double delta)
	{
		if (!IsBossGameplayPaused() && !(_headAttackPauseRemaining <= 0.0) && (TowerDefenseManager.HasGameplayAuthority || !(base.sprite?.clip != "HeadAttack4")))
		{
			_headAttackPauseRemaining = Mathf.Max(0.0, _headAttackPauseRemaining - delta);
			if (!(_headAttackPauseRemaining > 0.0))
			{
				ReleaseHeadAttackPause(TowerDefenseManager.HasGameplayAuthority);
			}
		}
	}

	private void BeginHeadAttackPause()
	{
		_headAttackPauseSequence++;
		if (_headAttackPauseSequence <= 0)
		{
			_headAttackPauseSequence = 1;
		}
		_headAttackPauseRemaining = 1.0;
		_headAttackPauseOwned = true;
		spritePause = true;
		MarkBossStateDirty();
	}

	private void ReleaseHeadAttackPause(bool markDirty)
	{
		bool num = _headAttackPauseRemaining > 0.0 || _headAttackPauseOwned;
		_headAttackPauseRemaining = 0.0;
		if (_headAttackPauseOwned)
		{
			spritePause = false;
		}
		_headAttackPauseOwned = false;
		if (num)
		{
			SetTransientRenderZIndex((int)(headAttackLine * 15 + base.itemLayer), "zomboss-head-attack-delay");
		}
		if (num && markDirty && TowerDefenseManager.HasGameplayAuthority)
		{
			MarkBossStateDirty();
		}
	}

	private void ApplyRemoteHeadAttackPause(int incomingSequence, double incomingRemaining)
	{
		if (incomingSequence < _remoteHeadAttackPauseSequence)
		{
			return;
		}
		if (incomingSequence > _remoteHeadAttackPauseSequence)
		{
			_remoteHeadAttackPauseSequence = incomingSequence;
			_headAttackPauseSequence = incomingSequence;
			if (incomingRemaining > 0.0)
			{
				_headAttackPauseRemaining = incomingRemaining;
				_headAttackPauseOwned = true;
				spritePause = true;
			}
			else
			{
				ReleaseHeadAttackPause(markDirty: false);
			}
		}
		else if (incomingRemaining <= 0.0)
		{
			ReleaseHeadAttackPause(markDirty: false);
		}
		else if (_headAttackPauseOwned)
		{
			_headAttackPauseRemaining = Mathf.Min(_headAttackPauseRemaining, incomingRemaining);
		}
	}

	public void StateRunning(double delta)
	{
		if (IsBossGameplayPaused() || !TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		string text = stateNow;
		if (text == null)
		{
			return;
		}
		switch (text.Length)
		{
		case 5:
			switch (text[1])
			{
			case 'p':
				if (text == "Spawn")
				{
					RunSpawnSequence(delta, spawnZombieNumOnce, completesSpawnGroup: true);
				}
				break;
			case 't':
				if (text == "Stomp")
				{
					SendStateEvent("ToStomp");
					stateNow = "";
					MarkBossStateDirty();
				}
				break;
			}
			break;
		case 18:
			if (text == "SpawnBeforeSpecial")
			{
				RunSpawnSequence(delta, 2, completesSpawnGroup: false);
			}
			break;
		case 17:
			if (text == "SpawnAfterSpecial")
			{
				RunSpawnSequence(delta, 3, completesSpawnGroup: true);
			}
			break;
		case 8:
			if (text == "HeadIdle")
			{
				headAttackOver = false;
				headIdleNum++;
				SendStateEvent("ToHeadIdle");
				stateNow = "";
				MarkBossStateDirty();
			}
			break;
		case 11:
			if (text == "BungeeSpawn")
			{
				SendStateEvent("ToBungee");
				stateNow = "";
				MarkBossStateDirty();
			}
			break;
		case 10:
			if (text == "PlantSpawn")
			{
				SendStateEvent("ToPlant");
				stateNow = "";
				MarkBossStateDirty();
			}
			break;
		case 2:
			if (text == "RV")
			{
				SendStateEvent("ToRV");
				stateNow = "";
				MarkBossStateDirty();
			}
			break;
		case 15:
		{
			if (!(text == "StandingSpecial"))
			{
				break;
			}
			Array<string> validStandingSpecials = GetValidStandingSpecials();
			if (validStandingSpecials.Count == 0)
			{
				stateNow = "";
				isRest = true;
				restTimer = 0.0;
				MarkBossStateDirty();
				break;
			}
			string text2 = validStandingSpecials.PickRandom();
			stateNow = "";
			MarkBossStateDirty();
			switch (text2)
			{
			case "PlantSpawn":
				SendStateEvent("ToPlant");
				break;
			case "BungeeSpawn":
				SendStateEvent("ToBungee");
				break;
			case "Stomp":
				SendStateEvent("ToStomp");
				break;
			case "RV":
				SendStateEvent("ToRV");
				break;
			}
			break;
		}
		}
	}

	private void RunSpawnSequence(double delta, int targetCount, bool completesSpawnGroup)
	{
		if (spawnRestTimer < spawnRestTime)
		{
			spawnRestTimer += delta;
			return;
		}
		SendStateEvent("ToSpawn");
		spawnZombieNumNow++;
		spawnRestTimer = 0.0;
		if (spawnZombieNumNow >= targetCount)
		{
			if (completesSpawnGroup)
			{
				spawnNum++;
			}
			spawnZombieNumNow = 0;
			isRest = true;
			stateNow = "";
			MarkBossStateDirty();
		}
	}

	public void ZombieSpawn()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(spawnZomie);
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		Vector2 vector = new Vector2((base.sprite as ZombieBossDave)?.GetSpawnMarkerGlobalPos(this).X ?? GetLogicalGlobalPosition().X, TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, spawnLine)).Y);
		Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(vector);
		mapGridPos.Y = spawnLine;
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(node2D.ToLocal(vector), mapGridPos);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return;
		}
		towerDefenseCharacter.EnableComponentGameplayUntilBattlefieldEntry();
		node2D.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		towerDefenseCharacter.gridPos = mapGridPos;
		towerDefenseCharacter.SetLogicalGlobalPosition(vector);
		ShadowComponent obj = towerDefenseCharacter.shadowComponent;
		if (obj != null && !obj.IsReleased)
		{
			towerDefenseCharacter.shadowComponent.Init();
			towerDefenseCharacter.shadowComponent.UpdateShadow();
		}
		towerDefenseCharacter.CallDeferred("WalkReady");
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
				MultiPlayerManager.Instance.SendSpawnCharacterAt(spawnZomie, mapGridPos.X, mapGridPos.Y, nextSyncId, 1.0, 1.0, hypnoses: false, 0.0, useCreate: true, vector.X, vector.Y, walkAfterSpawn: true, 0.0, "", new Dictionary
				{
					["component_gameplay_until_battlefield_entry"] = true,
					["walk_ready_after_spawn"] = true
				});
			}
		}
	}

	public void BungeeSpawn()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		Array<Vector2I> bungeeTargetPositions = GetBungeeTargetPositions();
		bungeeList.Clear();
		int num = Mathf.Min(abilityConfig?.bungeeCount ?? 3, bungeeTargetPositions.Count);
		double num2 = abilityConfig?.steelBungeeChance ?? 0.1;
		double num3 = abilityConfig?.hypnotistChance ?? 0.25;
		for (int i = 0; i < num; i++)
		{
			Vector2I item = bungeeTargetPositions.PickRandom();
			bungeeTargetPositions.Remove(item);
			double num4 = GD.Randf();
			string packetName = ((num4 < num2) ? "ZombieBungiSP" : ((num4 < num2 + num3) ? "ZombieHypnotist" : "ZombieBungi"));
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter = packetConfig.Plant(item, playAudio: false);
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				Dictionary dictionary = new Dictionary();
				if (towerDefenseCharacter is TowerDefenseZombieBungi towerDefenseZombieBungi)
				{
					towerDefenseZombieBungi.skipBungeeTarget = true;
					dictionary["skipBungeeTarget"] = true;
				}
				else if (towerDefenseCharacter is TowerDefenseZombieBungiSP towerDefenseZombieBungiSP)
				{
					towerDefenseZombieBungiSP.suppressBungeeTarget = true;
					dictionary["suppressBungeeTarget"] = true;
				}
				bungeeList.Add(towerDefenseCharacter);
				TowerDefenseManager.PublishSpawnedCharacter(packetName, towerDefenseCharacter, useCreate: false, 0.0, walkAfterSpawn: false, "", (dictionary.Count > 0) ? dictionary : null);
			}
		}
		bungeeIsSpawn = true;
		MarkBossStateDirty();
	}

	public void RVSpawn()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		int num = (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f) - 1;
		foreach (TowerDefenseCharacter item in cleanCharactersList)
		{
			if (item is TowerDefensePlant && item.camp != base.camp && item.gridPos.X <= num)
			{
				list.Add(item);
			}
		}
		Array<Vector2I> array = new Array<Vector2I>();
		foreach (TowerDefenseCharacter item2 in list)
		{
			if (!array.Contains(item2.gridPos))
			{
				array.Add(item2.gridPos);
			}
		}
		if (array.Count == 0)
		{
			_rvAttackArmed = false;
			if (base.sprite is ZombieBossDave zombieBossDave)
			{
				zombieBossDave.SetRVVisible(visible: false);
			}
			return;
		}
		Vector2I vector2I = array.PickRandom();
		if (vector2I.Y >= TowerDefenseManager.Instance.GetMapGridNum().Y)
		{
			vector2I.Y--;
		}
		rvPos = vector2I;
		_rvAttackArmed = true;
		_attackComponent5.SetCheckAreaShapeWorldOrigin(0, TowerDefenseManager.Instance.GetMapCellPos(rvPos) + TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(1.5f, 1f));
		ZombieBossDave zombieBossDave2 = base.sprite as ZombieBossDave;
		if (GodotObject.IsInstanceValid(zombieBossDave2))
		{
			zombieBossDave2.SetRVPos(rvPos);
		}
		MarkBossStateDirty();
	}

	public void BallSpawn()
	{
		AudioManager.Instance.AudioPlay("Bossboulderattack");
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		Vector2 vector = new Vector2((base.sprite as ZombieBossDave)?.GetBallSpawnMarkerGlobalPos(this).X ?? GetLogicalGlobalPosition().X, TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, headAttackLine)).Y);
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		TowerDefensePacketConfig packetConfig;
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce;
		if (headAttackIsFire)
		{
			packetConfig = TowerDefenseManager.GetPacketConfig("ItemFireball");
			TowerDefenseCharacter character = CreateBallAt(packetConfig, node2D, vector);
			if (!GodotObject.IsInstanceValid(character))
			{
				return;
			}
			towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(FIRE_BALL, new Vector2I(100, headAttackLine));
			node2D.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectParticlesOnce.GlobalPosition = vector;
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, character);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("ItemFireball", 100, headAttackLine, nextSyncId, 1.0, 1.0, hypnoses: false, 0.0, useCreate: true, vector.X, vector.Y);
				}
			}
			return;
		}
		packetConfig = TowerDefenseManager.GetPacketConfig("ItemIceball");
		TowerDefenseCharacter character2 = CreateBallAt(packetConfig, node2D, vector);
		if (!GodotObject.IsInstanceValid(character2))
		{
			return;
		}
		towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(ICE_BALL, new Vector2I(100, headAttackLine));
		node2D.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		towerDefenseEffectParticlesOnce.GlobalPosition = vector;
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl2 = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl2))
			{
				int nextSyncId2 = currentControl2.GetNextSyncId();
				currentControl2.RegisterSyncCharacter(nextSyncId2, character2);
				MultiPlayerManager.Instance.SendSpawnCharacterAt("ItemIceball", 100, headAttackLine, nextSyncId2, 1.0, 1.0, hypnoses: false, 0.0, useCreate: true, vector.X, vector.Y);
			}
		}
	}

	private TowerDefenseCharacter CreateBallAt(TowerDefensePacketConfig packetConfig, Node2D characterNode, Vector2 ballPos)
	{
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(characterNode.ToLocal(ballPos), new Vector2I(100, headAttackLine));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		towerDefenseCharacter.SetLogicalGlobalPosition(ballPos);
		return towerDefenseCharacter;
	}

	public string GetSpawnZombie()
	{
		Godot.Collections.Array array = abilityConfig?.GetZombieStage(stage) ?? new Godot.Collections.Array();
		if (array.Count == 0)
		{
			return "ZombieNormal";
		}
		Godot.Collections.Array array2 = ((spawnNum >= array.Count) ? array[array.Count - 1].AsGodotArray() : array[spawnNum].AsGodotArray());
		if (array2.Count == 0)
		{
			return "ZombieNormal";
		}
		return TowerDefenseManager.Instance.PickRandomZomie(array2);
	}

	public void SetStateList()
	{
		if (TowerDefenseManager.HasGameplayAuthority && !base.IsProgressRestoreInFlight)
		{
			if (stage >= 2)
			{
				stateMethodList = new Godot.Collections.Array { "SpawnBeforeSpecial", "StandingSpecial", "SpawnAfterSpecial", "StandingSpecial" };
			}
			else
			{
				stateMethodList = ((headIdleNum == 0) ? new Godot.Collections.Array { "Spawn", "Spawn" } : new Godot.Collections.Array { "Spawn" });
				stateMethodList.Add("StandingSpecial");
			}
			MarkBossStateDirty();
		}
	}

	private Array<string> GetValidStandingSpecials()
	{
		Array<string> array = new Array<string>();
		if (CanSpawnHypnotizedPlant())
		{
			array.Add("PlantSpawn");
		}
		if (stage > 1)
		{
			if (GetBungeeTargetPositions().Count > 0)
			{
				array.Add("BungeeSpawn");
			}
			if (HasRvTarget())
			{
				array.Add("RV");
			}
			if (GetAvailableStompIds().Count > 0)
			{
				array.Add("Stomp");
			}
		}
		return array;
	}

	private Array<int> GetAvailableStompIds()
	{
		Array<int> array = new Array<int>();
		AttackComponent attackComponent = _attackComponent1;
		if (attackComponent != null && attackComponent.CanAttackOnce())
		{
			array.Add(1);
		}
		AttackComponent attackComponent2 = _attackComponent2;
		if (attackComponent2 != null && attackComponent2.CanAttackOnce())
		{
			array.Add(2);
		}
		AttackComponent attackComponent3 = _attackComponent3;
		if (attackComponent3 != null && attackComponent3.CanAttackOnce())
		{
			array.Add(3);
		}
		AttackComponent attackComponent4 = _attackComponent4;
		if (attackComponent4 != null && attackComponent4.CanAttackOnce())
		{
			array.Add(4);
		}
		return array;
	}

	private Array<Vector2I> GetBungeeTargetPositions()
	{
		Array<Vector2I> array = new Array<Vector2I>();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return array;
		}
		int num = (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f) + 1;
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (cleanCharacters is TowerDefensePlant towerDefensePlant && towerDefensePlant.camp != base.camp && !(towerDefensePlant is TowerDefensePlantBowlingBase) && towerDefensePlant.instance.canBeCollection && towerDefensePlant.gridPos.X <= num && !array.Contains(towerDefensePlant.gridPos))
			{
				array.Add(towerDefensePlant.gridPos);
			}
		}
		return array;
	}

	private bool HasRvTarget()
	{
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return false;
		}
		int num = (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f) - 1;
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (cleanCharacters is TowerDefensePlant towerDefensePlant && towerDefensePlant.camp != base.camp && towerDefensePlant.gridPos.X <= num)
			{
				return true;
			}
		}
		return false;
	}

	private List<(string PacketName, Vector2I Position)> GetHypnotizedPlantPlacements()
	{
		List<(string, Vector2I)> list = new List<(string, Vector2I)>();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return list;
		}
		Godot.Collections.Array obj = abilityConfig?.GetPlantPool(stage) ?? new Godot.Collections.Array();
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		int num = Mathf.Clamp(abilityConfig?.plantColumnMin ?? 6, 1, mapGridNum.X);
		int num2 = Mathf.Clamp(abilityConfig?.plantColumnMax ?? 9, num, mapGridNum.X);
		foreach (Variant item in obj)
		{
			string text = item.AsString();
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
			if (!GodotObject.IsInstanceValid(packetConfig))
			{
				continue;
			}
			for (int i = num; i <= num2; i++)
			{
				for (int j = 1; j <= mapGridNum.Y; j++)
				{
					Vector2I vector2I = new Vector2I(i, j);
					if (CanForcePlantOnEmptyCell(packetConfig, vector2I))
					{
						list.Add((text, vector2I));
					}
				}
			}
		}
		return list;
	}

	private static bool CanForcePlantOnEmptyCell(TowerDefensePacketConfig packetConfig, Vector2I position)
	{
		if (!GodotObject.IsInstanceValid(packetConfig) || !(packetConfig.characterConfig is TowerDefensePlantConfig towerDefensePlantConfig) || !IsPlantFootprintCellEmpty(position))
		{
			return false;
		}
		foreach (Vector2I item in towerDefensePlantConfig.extendGrid)
		{
			if (!IsPlantFootprintCellEmpty(position + item))
			{
				return false;
			}
		}
		return true;
	}

	private static bool IsPlantFootprintCellEmpty(Vector2I position)
	{
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(position);
		if (!GodotObject.IsInstanceValid(mapCell))
		{
			return false;
		}
		mapCell.ClearEmpty();
		return mapCell.characterList.Count == 0;
	}

	private bool CanSpawnHypnotizedPlant()
	{
		return GetHypnotizedPlantPlacements().Count > 0;
	}

	private void SpawnHypnotizedPlants()
	{
		if (!TowerDefenseManager.HasGameplayAuthority || _plantSpawnResolved)
		{
			return;
		}
		_plantSpawnResolved = true;
		List<(string, Vector2I)> hypnotizedPlantPlacements = GetHypnotizedPlantPlacements();
		int num = Mathf.Min(abilityConfig?.plantDropCount ?? 3, hypnotizedPlantPlacements.Count);
		int num2 = 0;
		while (num2 < num && hypnotizedPlantPlacements.Count > 0)
		{
			int index = GD.RandRange(0, hypnotizedPlantPlacements.Count - 1);
			(string PacketName, Vector2I Position) candidate = hypnotizedPlantPlacements[index];
			hypnotizedPlantPlacements.RemoveAll(((string PacketName, Vector2I Position) value) => value.Position == candidate.Position);
			if (CanForcePlantOnEmptyCell(TowerDefenseManager.GetPacketConfig(candidate.PacketName), candidate.Position) && GodotObject.IsInstanceValid(TowerDefenseManager.Instance.BungiSpawn(candidate.PacketName, candidate.Position, null, hypnoses: true, -1, skipPlacementCheck: true)))
			{
				num2++;
			}
		}
		MarkBossStateDirty();
	}

	private void StartNextCrouchSkill()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		string text;
		if (pendingCrouchShowcases.Count > 0)
		{
			text = pendingCrouchShowcases[0];
			pendingCrouchShowcases.RemoveAt(0);
		}
		else
		{
			Array<string> array = new Array<string> { "Fire", "Ice" };
			if (stage >= 1)
			{
				array.Add("Missile");
			}
			if (stage >= 2)
			{
				array.Add("Doom");
			}
			text = array.PickRandom();
		}
		activeCrouchSkill = text;
		headAttackOver = true;
		int y = TowerDefenseManager.Instance.GetMapGridNum().Y;
		bool flag = text == "Doom";
		headAttackLine = (flag ? Mathf.Clamp((int)Mathf.Floor((float)y / 2f) + 2, 1, y) : GD.RandRange(1, y));
		headAttackIsFire = text == "Fire";
		MarkBossStateDirty();
		if (!(text == "Missile"))
		{
			if (text == "Doom")
			{
				SendStateEvent("ToDoomStart");
			}
			else
			{
				SendStateEvent("ToHeadAttack");
			}
		}
		else
		{
			SendStateEvent("ToMissile");
		}
	}

	private void QueueCrouchShowcase(string skill)
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			return;
		}
		if (skill == "Missile")
		{
			if (_missileShowcaseQueued)
			{
				return;
			}
			_missileShowcaseQueued = true;
			int num = pendingCrouchShowcases.IndexOf("Doom");
			if (num >= 0)
			{
				pendingCrouchShowcases.Insert(num, skill);
			}
			else
			{
				pendingCrouchShowcases.Add(skill);
			}
		}
		else if (skill == "Doom")
		{
			if (_doomShowcaseQueued)
			{
				return;
			}
			_doomShowcaseQueued = true;
			pendingCrouchShowcases.Add(skill);
		}
		MarkBossStateDirty();
	}

	private void SetCrouchPresentation(string ballKind)
	{
		int y = TowerDefenseManager.Instance.GetMapGridNum().Y;
		int num = ((ballKind == "Purple") ? Mathf.Clamp((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2, 1, y) : Mathf.Clamp(headAttackLine, 1, y));
		SetTransientRenderZIndex((int)(num * 15 + base.itemLayer), "zomboss-crouch-skill");
		if (base.sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.SetHeadAttack(num);
			zombieBossDave.SetHeadAttackBall(ballKind);
		}
	}

	private void PrepareMissileAttack()
	{
		if (_missileAttackActive && missileTargets.Count > 0)
		{
			RefreshMissileCrosshairs();
			return;
		}
		missileTargets.Clear();
		missileTargetIndex = 0;
		int num = abilityConfig?.missileCount ?? 4;
		List<Vector2I> list = new List<Vector2I>();
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (cleanCharacters is TowerDefensePlant towerDefensePlant && towerDefensePlant.camp != base.camp && !list.Contains(towerDefensePlant.gridPos))
			{
				list.Add(towerDefensePlant.gridPos);
			}
		}
		while (missileTargets.Count < num && list.Count > 0)
		{
			int index = GD.RandRange(0, list.Count - 1);
			missileTargets.Add(list[index]);
			list.RemoveAt(index);
		}
		Vector2I mapGridNum = TowerDefenseManager.Instance.GetMapGridNum();
		List<Vector2I> list2 = new List<Vector2I>();
		for (int i = 1; i <= mapGridNum.X; i++)
		{
			for (int j = 1; j <= mapGridNum.Y; j++)
			{
				Vector2I item = new Vector2I(i, j);
				if (!missileTargets.Contains(item))
				{
					list2.Add(item);
				}
			}
		}
		while (missileTargets.Count < num && list2.Count > 0)
		{
			int index2 = GD.RandRange(0, list2.Count - 1);
			missileTargets.Add(list2[index2]);
			list2.RemoveAt(index2);
		}
		_missileAttackActive = true;
		_missileCrosshairTrackingActive = true;
		missileResolvedMask = 0;
		_missileActionSequence++;
		ConnectMissileLifecycleSignals();
		RefreshMissileCrosshairs();
		MarkBossStateDirty();
	}

	private void SpawnNextMissile()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			CatchUpRemoteMissileVisuals(missileTargetIndex);
		}
		else if (_missileAttackActive && missileTargetIndex >= 0 && missileTargetIndex < missileTargets.Count)
		{
			int num = missileTargetIndex;
			bool num2 = SpawnMissileProjectile(num, missileTargets[num], suppressGameplay: false);
			missileTargetIndex++;
			if (!num2)
			{
				ResolveMissileSlot(num, markDirty: false);
			}
			RefreshMissileCrosshairs();
			MarkBossStateDirty();
		}
	}

	private void CatchUpRemoteMissileVisuals(int firedCount)
	{
		if (TowerDefenseManager.HasGameplayAuthority || !IsNodeReady() || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(base.sprite) || (!_missileAttackActive && activeCrouchSkill != "Missile" && base.sprite.clip != "Missile"))
		{
			return;
		}
		int num = Mathf.Clamp(firedCount, 0, missileTargets.Count);
		while (_remoteMissileVisualIndex < num)
		{
			int remoteMissileVisualIndex = _remoteMissileVisualIndex;
			_remoteMissileVisualIndex++;
			if (!IsMissileSlotResolved(remoteMissileVisualIndex))
			{
				SpawnMissileProjectile(remoteMissileVisualIndex, missileTargets[remoteMissileVisualIndex], suppressGameplay: true);
			}
		}
	}

	private bool SpawnMissileProjectile(int slot, Vector2I targetGrid, bool suppressGameplay)
	{
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = abilityConfig?.missileProjectileConfig;
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig))
		{
			return false;
		}
		Vector2 vector = ((base.sprite is ZombieBossDave zombieBossDave) ? zombieBossDave.GetBallSpawnMarkerGlobalPos(this) : GetLogicalGlobalPosition());
		BulletFieldSpawnOverrides overrides = new BulletFieldSpawnOverrides
		{
			gridYOverride = targetGrid.Y,
			baseDamageOverride = (abilityConfig?.missileDamage ?? 1800.0),
			catapultStartPositionOverride = vector,
			catapultTargetPositionOverride = TowerDefenseManager.GetMapCellPlantPos(targetGrid),
			catapultSkyDrop = true,
			catapultSkyDropAscentHorizontalOffset = 240f,
			catapultSkyDropOffscreenWaitSeconds = 1.5,
			catapultSkyDropVisualRadius = 60f,
			spriteRotationOverride = -(float)Math.PI / 2f,
			lifecycleSubscribed = true,
			lifecycleOwnerSequence = _missileActionSequence,
			lifecycleSlot = slot,
			suppressGameplay = suppressGameplay
		};
		int bulletIndex;
		return FireComponent.TryCreateProjectilePositionByConfig(this, null, 0.0, vector, Vector2.Zero, towerDefenseProjectileConfig, out bulletIndex, towerDefenseProjectileConfig.collisionFlags, base.camp, Vector2.Zero, overrides);
	}

	private void OnMissileLifecycleEnded(TowerDefenseCharacter source, int ownerSequence, int slot, BulletField.BulletLifecycleTerminalReason reason)
	{
		if (source == this && ownerSequence == _missileActionSequence)
		{
			ResolveMissileSlot(slot, TowerDefenseManager.HasGameplayAuthority);
		}
	}

	private bool IsMissileSlotResolved(int slot)
	{
		if (slot >= 0 && slot < 31)
		{
			return (missileResolvedMask & (1 << slot)) != 0;
		}
		return false;
	}

	private int GetAllMissileSlotsMask()
	{
		if (missileTargets.Count <= 0)
		{
			return 0;
		}
		if (missileTargets.Count < 31)
		{
			return (1 << missileTargets.Count) - 1;
		}
		return -1;
	}

	private void ResolveMissileSlot(int slot, bool markDirty)
	{
		if (slot >= 0 && slot < missileTargets.Count && slot < 31 && !IsMissileSlotResolved(slot))
		{
			missileResolvedMask |= 1 << slot;
			if (_missileCrosshairs.Remove(slot, out var value) && GodotObject.IsInstanceValid(value))
			{
				value.QueueFree();
			}
			if ((missileResolvedMask & GetAllMissileSlotsMask()) == GetAllMissileSlotsMask())
			{
				_missileCrosshairTrackingActive = false;
				ClearMissileCrosshairs();
			}
			if (markDirty)
			{
				MarkBossStateDirty();
			}
		}
	}

	private void RefreshMissileCrosshairs()
	{
		ClearMissileCrosshairs();
		if ((!_missileAttackActive && !_missileCrosshairTrackingActive) || !GodotObject.IsInstanceValid(CROSSHAIR_TEX) || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		for (int i = 0; i < missileTargets.Count; i++)
		{
			if (!IsMissileSlotResolved(i))
			{
				Sprite2D sprite2D = new Sprite2D
				{
					Texture = CROSSHAIR_TEX,
					ZAsRelative = false,
					ZIndex = 2000,
					GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(missileTargets[i])
				};
				TowerDefenseManager.GetCharacterNode().AddChild(sprite2D, forceReadableName: false, InternalMode.Disabled);
				_missileCrosshairs[i] = sprite2D;
			}
		}
	}

	private void ClearMissileCrosshairs()
	{
		foreach (Sprite2D value in _missileCrosshairs.Values)
		{
			if (GodotObject.IsInstanceValid(value))
			{
				value.QueueFree();
			}
		}
		_missileCrosshairs.Clear();
	}

	private void EndMissileAttack(bool markDirty = true, bool preserveInFlightCrosshairs = false)
	{
		bool num = _missileAttackActive || _missileCrosshairTrackingActive;
		_missileAttackActive = false;
		bool flag = missileTargetIndex >= missileTargets.Count;
		if (!preserveInFlightCrosshairs || !flag)
		{
			missileResolvedMask = GetAllMissileSlotsMask();
			_missileCrosshairTrackingActive = false;
			ClearMissileCrosshairs();
		}
		if (num && markDirty && TowerDefenseManager.HasGameplayAuthority)
		{
			MarkBossStateDirty();
		}
	}

	private void SetExposedHeadDamageableState()
	{
		instance.invincible = false;
		instance.canBeCollection = true;
		targetRegistrationComponent.canProjectileCheck = true;
		targetRegistrationComponent.allLineCheck = true;
		instance.unUseBuffFlags = -4;
	}

	private void SetRetractedHeadState()
	{
		instance.invincible = true;
		instance.canBeCollection = false;
		targetRegistrationComponent.canProjectileCheck = false;
		targetRegistrationComponent.allLineCheck = false;
		instance.unUseBuffFlags = -1;
		if (buff.BuffHas("Frozen"))
		{
			buff.DeleteBuff("Frozen");
		}
		if (buff.BuffHas("IceSpeedDown"))
		{
			buff.DeleteBuff("IceSpeedDown");
		}
	}

	private void SetDoomDamageableState()
	{
		instance.invincible = false;
		instance.canBeCollection = true;
		targetRegistrationComponent.canProjectileCheck = true;
		targetRegistrationComponent.allLineCheck = true;
		instance.unUseBuffFlags = -1;
		if (buff.BuffHas("Frozen"))
		{
			buff.DeleteBuff("Frozen");
		}
		if (buff.BuffHas("IceSpeedDown"))
		{
			buff.DeleteBuff("IceSpeedDown");
		}
	}

	private void ApplyBossPresentationForClip(string clipName, bool playOneShot)
	{
		if (clipName == null)
		{
			return;
		}
		switch (clipName.Length)
		{
		case 8:
		{
			char c = clipName[0];
			if (c != 'D')
			{
				if (c != 'H' || !(clipName == "HeadIdle"))
				{
					break;
				}
				goto IL_0149;
			}
			if (!(clipName == "DoomIdle"))
			{
				break;
			}
			goto IL_019a;
		}
		case 7:
			switch (clipName[0])
			{
			default:
				return;
			case 'M':
				if (clipName == "Missile")
				{
					RemoveDoomShield();
					SetExposedHeadDamageableState();
					SetCrouchPresentation("Green");
				}
				return;
			case 'D':
				break;
			}
			if (!(clipName == "DoomEnd"))
			{
				break;
			}
			goto IL_01bc;
		case 10:
		{
			char c = clipName[0];
			if (c != 'D')
			{
				if (c != 'H' || !(clipName == "HeadExited"))
				{
					break;
				}
				goto IL_01f9;
			}
			if (!(clipName == "DoomCancel"))
			{
				break;
			}
			goto IL_01bc;
		}
		case 5:
			switch (clipName[0])
			{
			default:
				return;
			case 'E':
				if (!(clipName == "Enter"))
				{
					return;
				}
				break;
			case 'D':
				if (!(clipName == "Death"))
				{
					return;
				}
				break;
			}
			goto IL_01f9;
		case 11:
			if (!(clipName == "HeadAttack4"))
			{
				break;
			}
			goto IL_0149;
		case 9:
			if (!(clipName == "DoomStart"))
			{
				break;
			}
			goto IL_019a;
		case 4:
			if (!(clipName == "Idle"))
			{
				break;
			}
			goto IL_01f9;
		case 6:
			break;
			IL_01bc:
			RemoveDoomShield();
			SetExposedHeadDamageableState();
			SetCrouchPresentation("Purple");
			if (playOneShot && clipName == "DoomCancel" && base.sprite is ZombieBossDave zombieBossDave)
			{
				zombieBossDave.PlayDriverDamage();
			}
			break;
			IL_0149:
			RemoveDoomShield();
			SetExposedHeadDamageableState();
			if (base.sprite is ZombieBossDave zombieBossDave2)
			{
				zombieBossDave2.SetHeadAttackBall(headAttackIsFire ? "Fire" : "Ice");
			}
			break;
			IL_01f9:
			RemoveDoomShield();
			SetRetractedHeadState();
			break;
			IL_019a:
			if (_doomCharging)
			{
				EnsureDoomShield(reset: false);
			}
			SetDoomDamageableState();
			SetCrouchPresentation("Purple");
			break;
		}
	}

	private void AdvanceDoomCharge(double delta)
	{
		if (!IsBossGameplayPaused() && TowerDefenseManager.HasGameplayAuthority && _doomCharging && !base.sprite.pause)
		{
			doomChargeRemaining = Mathf.Max(0.0, doomChargeRemaining - delta);
			if (!(doomChargeRemaining > 0.0))
			{
				_doomCharging = false;
				RemoveDoomShield();
				MarkBossStateDirty();
				SendStateEvent("ToDoomEnd");
			}
		}
	}

	private TowerDefenseArmorInstance EnsureDoomShield(bool reset)
	{
		if (!GodotObject.IsInstanceValid(instance))
		{
			return null;
		}
		TowerDefenseArmorInstance towerDefenseArmorInstance = GetArmorFromName("BossDaveDoomShield");
		if (reset && GodotObject.IsInstanceValid(towerDefenseArmorInstance))
		{
			RemoveDoomShield();
			towerDefenseArmorInstance = null;
		}
		if (!GodotObject.IsInstanceValid(towerDefenseArmorInstance))
		{
			instance.ArmorAdd("BossDaveDoomShield");
			towerDefenseArmorInstance = GetArmorFromName("BossDaveDoomShield");
		}
		if (!GodotObject.IsInstanceValid(towerDefenseArmorInstance))
		{
			return null;
		}
		towerDefenseArmorInstance.hitpointScale = 1.0;
		ApplyPendingLegacyDoomShieldDamage(towerDefenseArmorInstance);
		return towerDefenseArmorInstance;
	}

	private void RemoveDoomShield()
	{
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		TowerDefenseArmorInstance armorFromName = GetArmorFromName("BossDaveDoomShield");
		if (!GodotObject.IsInstanceValid(armorFromName) || armorFromName.isRemove)
		{
			return;
		}
		_removingDoomShield = true;
		try
		{
			armorFromName.RemoveArmor(ArmorRemovalReason.Replaced);
			instance.ArmorDestroy(armorFromName);
		}
		finally
		{
			_removingDoomShield = false;
		}
	}

	private void OnDoomShieldBroken(string armorName)
	{
		if (!_removingDoomShield && !(armorName != "BossDaveDoomShield") && TowerDefenseManager.HasGameplayAuthority && _doomCharging)
		{
			_doomCharging = false;
			MarkBossStateDirty();
			SendStateEvent("ToDoomCancel");
		}
	}

	private void ApplyPendingLegacyDoomShieldDamage(TowerDefenseArmorInstance shield)
	{
		if (_hasPendingLegacyDoomShieldDamage && GodotObject.IsInstanceValid(shield))
		{
			double num = Mathf.Max(0.0, _pendingLegacyDoomShieldDamage);
			_hasPendingLegacyDoomShieldDamage = false;
			_pendingLegacyDoomShieldDamage = 0.0;
			double num2 = Mathf.Max(0.0, shield.hitpointsSave - num);
			if (num2 <= 0.0)
			{
				shield.Hurt(shield.hitPoints, playSplatAudio: false, Vector2.Zero, createDamagePart: false, ignoreLimit: true);
				return;
			}
			shield.hitPoints = num2;
			shield.RefreshDamageStageFromHitPoints();
		}
	}

	private void ReleaseDoomAttack()
	{
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			PlayDoomReleaseEffect();
		}
		else
		{
			if (_doomAttackResolved)
			{
				return;
			}
			_doomAttackResolved = true;
			_doomCharging = false;
			RemoveDoomShield();
			PlayDoomReleaseEffect();
			double num = abilityConfig?.doomDamage ?? 1800.0;
			foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
			{
				if (cleanCharacters is TowerDefensePlant towerDefensePlant && towerDefensePlant.camp != base.camp && !towerDefensePlant.die)
				{
					towerDefensePlant.Hurt(num);
				}
			}
			MarkBossStateDirty();
		}
	}

	private void PlayDoomReleaseEffect()
	{
		AudioManager.Instance.AudioPlay("Doomshroom");
		Vector2 globalPosition = ((base.sprite is ZombieBossDave zombieBossDave) ? zombieBossDave.GetBallSpawnMarkerGlobalPos(this) : GetLogicalGlobalPosition());
		if (base.sprite is ZombieBossDave zombieBossDave2 && zombieBossDave2.TryResolveLayerIdForRender("DoomBall2", out var layerId) && zombieBossDave2.TryGetManagedLayerPositionForRender(layerId, new Vector2(70.5f, 70.5f), out var position))
		{
			globalPosition = zombieBossDave2.GlobalTransform * position;
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(DOOMSHROOM_EXPLOSION, new Vector2I(100, 100));
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		towerDefenseEffectParticlesOnce.GlobalPosition = globalPosition;
	}

	private void FinishStandingSpecial()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			isRest = true;
			MarkBossStateDirty();
			if (!die)
			{
				Idle();
			}
		}
	}

	private void MarkBossStateDirty()
	{
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_networkSpecialStateRevision++;
			if (_networkSpecialStateRevision <= 0)
			{
				_networkSpecialStateRevision = 1;
			}
		}
	}

	private static bool IsBossGameplayPaused()
	{
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.pauseZombie;
		}
		return false;
	}

	public void ExplosionMethod()
	{
		AudioManager.Instance.AudioPlay("ZamboniExplosion");
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(EXPLOSION, new Vector2I(100, 100));
		TowerDefenseManager.GetCharacterNode().AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, InternalMode.Disabled);
		towerDefenseEffectParticlesOnce.Position = base.Position - new Vector2(GD.RandRange(-50, 50), GD.RandRange(-100, 100));
	}

	public override void InWater()
	{
	}

	public override void OutWater()
	{
	}

	public override void InWaterDiscardSet()
	{
	}

	public override void OutWaterDiscardSet()
	{
	}

	public void ImportNetworkSpawnState(Dictionary data)
	{
		ImportBossNetworkState(data);
	}

	public override Dictionary ExportNetworkSpawnState()
	{
		return ExportBossNetworkState();
	}

	public override Dictionary ExportNetworkSpecialState()
	{
		return ExportBossNetworkState();
	}

	public override void ImportNetworkSpecialState(Dictionary data)
	{
		ImportBossNetworkState(data);
	}

	public override int GetNetworkSpecialStateRevision()
	{
		return _networkSpecialStateRevision;
	}

	public override bool IsNetworkSpecialMovementActive()
	{
		if (!_plantAttackActive && !_missileAttackActive && !_missileCrosshairTrackingActive)
		{
			return _doomCharging;
		}
		return true;
	}

	public override void OnRemoteNetworkAnimationStart(string clipName)
	{
		if (clipName != "HeadAttack4")
		{
			ReleaseHeadAttackPause(markDirty: false);
		}
		ApplyBossPresentationForClip(clipName, playOneShot: true);
		switch (clipName)
		{
		case "Missile":
			CatchUpRemoteMissileVisuals(missileTargetIndex);
			RefreshMissileCrosshairs();
			break;
		case "HeadExited":
			CatchUpRemoteMissileVisuals(missileTargetIndex);
			RefreshMissileCrosshairs();
			break;
		case "Death":
			_missileCrosshairTrackingActive = false;
			ClearMissileCrosshairs();
			break;
		}
	}

	private Dictionary ExportBossNetworkState()
	{
		return new Dictionary
		{
			["schemaVersion"] = 4,
			["rev"] = _networkSpecialStateRevision,
			["stage"] = stage,
			["stateNow"] = stateNow,
			["stateMethodList"] = stateMethodList,
			["isRest"] = isRest,
			["restTimer"] = restTimer,
			["spawnZombieNumNow"] = spawnZombieNumNow,
			["spawnRestTimer"] = spawnRestTimer,
			["spawnLine"] = spawnLine,
			["spawnZomie"] = spawnZomie ?? "",
			["spawnNum"] = spawnNum,
			["headIdleNum"] = headIdleNum,
			["headAttackOver"] = headAttackOver,
			["headAttackLine"] = headAttackLine,
			["headAttackIsFire"] = headAttackIsFire,
			["headAttackRestTimer"] = headAttackRestTimer,
			["headAttackPauseRemaining"] = _headAttackPauseRemaining,
			["headAttackPauseSequence"] = _headAttackPauseSequence,
			["activeCrouchSkill"] = activeCrouchSkill ?? "",
			["pendingCrouchShowcases"] = pendingCrouchShowcases,
			["missileShowcaseQueued"] = _missileShowcaseQueued,
			["doomShowcaseQueued"] = _doomShowcaseQueued,
			["plantAttackActive"] = _plantAttackActive,
			["plantSpawnResolved"] = _plantSpawnResolved,
			["plantActionSequence"] = _plantActionSequence,
			["missileAttackActive"] = _missileAttackActive,
			["missileTargets"] = SerializeGridPositions(missileTargets),
			["missileTargetIndex"] = missileTargetIndex,
			["missileActionSequence"] = _missileActionSequence,
			["missileResolvedMask"] = missileResolvedMask,
			["missileCrosshairTrackingActive"] = _missileCrosshairTrackingActive,
			["doomCharging"] = _doomCharging,
			["doomChargeRemaining"] = doomChargeRemaining,
			["doomAttackResolved"] = _doomAttackResolved,
			["doomActionSequence"] = _doomActionSequence,
			["bungeeIsSpawn"] = bungeeIsSpawn,
			["rvAttackArmed"] = _rvAttackArmed,
			["stompId"] = stompId,
			["rvPosX"] = rvPos.X,
			["rvPosY"] = rvPos.Y
		};
	}

	private void ImportBossNetworkState(Dictionary data)
	{
		if (data == null)
		{
			return;
		}
		int num = data.GetValueOrDefault("schemaVersion", 0).AsInt32();
		int num2 = data.GetValueOrDefault("rev", 0).AsInt32();
		if (!TowerDefenseManager.HasGameplayAuthority && num2 < _networkSpecialStateRevision)
		{
			return;
		}
		_networkSpecialStateRevision = Mathf.Max(_networkSpecialStateRevision, num2);
		stage = data.GetValueOrDefault("stage", stage).AsInt32();
		stateNow = data.GetValueOrDefault("stateNow", stateNow).AsString();
		stateMethodList = data.GetValueOrDefault("stateMethodList", stateMethodList).AsGodotArray();
		isRest = data.GetValueOrDefault("isRest", isRest).AsBool();
		restTimer = data.GetValueOrDefault("restTimer", restTimer).AsDouble();
		spawnZombieNumNow = data.GetValueOrDefault("spawnZombieNumNow", spawnZombieNumNow).AsInt32();
		spawnRestTimer = data.GetValueOrDefault("spawnRestTimer", spawnRestTimer).AsDouble();
		spawnLine = data.GetValueOrDefault("spawnLine", spawnLine).AsInt32();
		spawnZomie = data.GetValueOrDefault("spawnZomie", spawnZomie ?? "").AsString();
		spawnNum = data.GetValueOrDefault("spawnNum", spawnNum).AsInt32();
		headIdleNum = data.GetValueOrDefault("headIdleNum", headIdleNum).AsInt32();
		headAttackOver = data.GetValueOrDefault("headAttackOver", headAttackOver).AsBool();
		headAttackLine = data.GetValueOrDefault("headAttackLine", headAttackLine).AsInt32();
		headAttackIsFire = data.GetValueOrDefault("headAttackIsFire", headAttackIsFire).AsBool();
		headAttackRestTimer = data.GetValueOrDefault("headAttackRestTimer", headAttackRestTimer).AsDouble();
		double num3 = data.GetValueOrDefault("headAttackPauseRemaining", _headAttackPauseRemaining).AsDouble();
		int num4 = data.GetValueOrDefault("headAttackPauseSequence", _headAttackPauseSequence).AsInt32();
		if (TowerDefenseManager.HasGameplayAuthority)
		{
			_headAttackPauseSequence = num4;
			_headAttackPauseRemaining = num3;
			_headAttackPauseOwned = num3 > 0.0;
		}
		else
		{
			ApplyRemoteHeadAttackPause(num4, num3);
		}
		activeCrouchSkill = data.GetValueOrDefault("activeCrouchSkill", activeCrouchSkill).AsString();
		pendingCrouchShowcases = ReadStringArray(data.GetValueOrDefault("pendingCrouchShowcases", pendingCrouchShowcases));
		_missileShowcaseQueued = data.GetValueOrDefault("missileShowcaseQueued", _missileShowcaseQueued).AsBool();
		_doomShowcaseQueued = data.GetValueOrDefault("doomShowcaseQueued", _doomShowcaseQueued).AsBool();
		_plantAttackActive = data.GetValueOrDefault("plantAttackActive", _plantAttackActive).AsBool();
		_plantSpawnResolved = data.GetValueOrDefault("plantSpawnResolved", _plantSpawnResolved).AsBool();
		_plantActionSequence = data.GetValueOrDefault("plantActionSequence", _plantActionSequence).AsInt32();
		_missileAttackActive = data.GetValueOrDefault("missileAttackActive", _missileAttackActive).AsBool();
		missileTargets = DeserializeGridPositions(data.GetValueOrDefault("missileTargets", SerializeGridPositions(missileTargets)));
		int firedCount = data.GetValueOrDefault("missileTargetIndex", missileTargetIndex).AsInt32();
		int num5 = data.GetValueOrDefault("missileActionSequence", _missileActionSequence).AsInt32();
		missileResolvedMask = data.GetValueOrDefault("missileResolvedMask", missileResolvedMask).AsInt32();
		_missileCrosshairTrackingActive = data.GetValueOrDefault("missileCrosshairTrackingActive", _missileAttackActive).AsBool();
		_missileActionSequence = num5;
		if (!TowerDefenseManager.HasGameplayAuthority)
		{
			if (num5 != _remoteMissileActionSequence)
			{
				_remoteMissileActionSequence = num5;
				_remoteMissileVisualIndex = 0;
			}
			CatchUpRemoteMissileVisuals(firedCount);
		}
		missileTargetIndex = firedCount;
		_doomCharging = data.GetValueOrDefault("doomCharging", _doomCharging).AsBool();
		doomChargeRemaining = data.GetValueOrDefault("doomChargeRemaining", doomChargeRemaining).AsDouble();
		if (num < 4 && data.ContainsKey("doomAccumulatedDamage"))
		{
			_pendingLegacyDoomShieldDamage = data["doomAccumulatedDamage"].AsDouble();
			_hasPendingLegacyDoomShieldDamage = _doomCharging;
		}
		_doomAttackResolved = data.GetValueOrDefault("doomAttackResolved", _doomAttackResolved).AsBool();
		_doomActionSequence = data.GetValueOrDefault("doomActionSequence", _doomActionSequence).AsInt32();
		bungeeIsSpawn = data.GetValueOrDefault("bungeeIsSpawn", bungeeIsSpawn).AsBool();
		_rvAttackArmed = data.GetValueOrDefault("rvAttackArmed", _rvAttackArmed).AsBool();
		stompId = data.GetValueOrDefault("stompId", stompId).AsInt32();
		rvPos = new Vector2I(data.GetValueOrDefault("rvPosX", rvPos.X).AsInt32(), data.GetValueOrDefault("rvPosY", rvPos.Y).AsInt32());
		_hasPendingNetworkPresentation = true;
		if (IsNodeReady())
		{
			Callable.From(ApplyPendingBossNetworkPresentation).CallDeferred();
		}
	}

	private static Array<int> SerializeGridPositions(Array<Vector2I> positions)
	{
		Array<int> array = new Array<int>();
		foreach (Vector2I position in positions)
		{
			array.Add(position.X);
			array.Add(position.Y);
		}
		return array;
	}

	private static Array<Vector2I> DeserializeGridPositions(Variant value)
	{
		Array<Vector2I> array = new Array<Vector2I>();
		Godot.Collections.Array array2 = value.AsGodotArray();
		for (int i = 0; i + 1 < array2.Count; i += 2)
		{
			array.Add(new Vector2I(array2[i].AsInt32(), array2[i + 1].AsInt32()));
		}
		return array;
	}

	private static Array<string> ReadStringArray(Variant value)
	{
		Array<string> array = new Array<string>();
		foreach (Variant item in value.AsGodotArray())
		{
			array.Add(item.AsString());
		}
		return array;
	}

	private Array<string> GetSavedBungeeNodeNames()
	{
		Array<string> array = new Array<string>();
		foreach (Variant bungee in bungeeList)
		{
			TowerDefenseCharacter towerDefenseCharacter = bungee.AsGodotObject() as TowerDefenseCharacter;
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				array.Add(towerDefenseCharacter.Name.ToString());
			}
		}
		return array;
	}

	private void ResolveSavedBungeeRelations()
	{
		bungeeList.Clear();
		if (_pendingBungeeNodeNames.Count == 0 || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return;
		}
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (GodotObject.IsInstanceValid(cleanCharacters) && _pendingBungeeNodeNames.Contains(cleanCharacters.Name.ToString()))
			{
				bungeeList.Add(cleanCharacters);
			}
		}
		_pendingBungeeNodeNames.Clear();
	}

	private void ApplyPendingBossNetworkPresentation()
	{
		if (!_hasPendingNetworkPresentation || !IsNodeReady() || !GodotObject.IsInstanceValid(TowerDefenseManager.Instance) || !GodotObject.IsInstanceValid(base.sprite))
		{
			return;
		}
		_hasPendingNetworkPresentation = false;
		ApplyStagePresentation();
		if (base.sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.SetRVPos(rvPos);
		}
		ApplyBossPresentationForClip(base.sprite.clip, playOneShot: false);
		if (_clearLegacyMissilePauseOnFinalize)
		{
			if (_spawnLegacyDelayedMissileOnFinalize && TowerDefenseManager.HasGameplayAuthority && _missileAttackActive && missileTargetIndex < missileTargets.Count)
			{
				SpawnNextMissile();
			}
			if (_legacyMissileEventFrame >= 0 && base.sprite.clip == "Missile")
			{
				base.sprite.frameIndex = Mathf.Clamp(_legacyMissileEventFrame + 1, base.sprite.clipRange.X, Mathf.Max(base.sprite.clipRange.X, base.sprite.clipRange.Y - 1));
				base.sprite.elapsedTimer = 0.0;
			}
			spritePause = false;
			_clearLegacyMissilePauseOnFinalize = false;
			_spawnLegacyDelayedMissileOnFinalize = false;
			_legacyMissileEventFrame = -1;
		}
		if (_headAttackPauseRemaining > 0.0)
		{
			_headAttackPauseOwned = true;
			spritePause = true;
		}
		else
		{
			ReleaseHeadAttackPause(markDirty: false);
		}
		CatchUpRemoteMissileVisuals(missileTargetIndex);
		RefreshMissileCrosshairs();
	}

	private void ApplyStagePresentation()
	{
		if (stage >= 1 && base.sprite is ZombieBossDave zombieBossDave)
		{
			zombieBossDave.DamagePointSet("Stage1");
		}
		if (stage >= 2 && base.sprite is ZombieBossDave zombieBossDave2)
		{
			zombieBossDave2.DamagePointSet("Stage2");
		}
		if (stage >= 3 && base.sprite is ZombieBossDave zombieBossDave3)
		{
			zombieBossDave3.DamagePointSet("Stage3");
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			["bossDaveSaveVersion"] = 4,
			["stage"] = stage,
			["stateNow"] = stateNow,
			["useEnterAnime"] = useEnterAnime,
			["spawnZombieNumOnce"] = spawnZombieNumOnce,
			["spawnZombieNumNow"] = spawnZombieNumNow,
			["spawnTime"] = spawnTime,
			["spawnLine"] = spawnLine,
			["spawnRestTime"] = spawnRestTime,
			["spawnRestTimer"] = spawnRestTimer,
			["spawnZomie"] = spawnZomie,
			["spawnNum"] = spawnNum,
			["headAttackOver"] = headAttackOver,
			["headAttackLine"] = headAttackLine,
			["headAttackIsFire"] = headAttackIsFire,
			["headAttackRestTime"] = headAttackRestTime,
			["headAttackRestTimer"] = headAttackRestTimer,
			["headAttackPauseRemaining"] = _headAttackPauseRemaining,
			["headAttackPauseSequence"] = _headAttackPauseSequence,
			["headIdleNum"] = headIdleNum,
			["bungeeIsSpawn"] = bungeeIsSpawn,
			["bungeeNodeNames"] = GetSavedBungeeNodeNames(),
			["rvAttackArmed"] = _rvAttackArmed,
			["stompId"] = stompId,
			["isRest"] = isRest,
			["restTime"] = restTime,
			["restTimer"] = restTimer,
			["stateMethodList"] = stateMethodList,
			["activeCrouchSkill"] = activeCrouchSkill,
			["pendingCrouchShowcases"] = pendingCrouchShowcases,
			["missileShowcaseQueued"] = _missileShowcaseQueued,
			["doomShowcaseQueued"] = _doomShowcaseQueued,
			["plantAttackActive"] = _plantAttackActive,
			["plantSpawnResolved"] = _plantSpawnResolved,
			["plantActionSequence"] = _plantActionSequence,
			["missileAttackActive"] = _missileAttackActive,
			["missileTargets"] = SerializeGridPositions(missileTargets),
			["missileTargetIndex"] = missileTargetIndex,
			["missileActionSequence"] = _missileActionSequence,
			["missileResolvedMask"] = missileResolvedMask,
			["missileCrosshairTrackingActive"] = _missileCrosshairTrackingActive,
			["doomCharging"] = _doomCharging,
			["doomChargeRemaining"] = doomChargeRemaining,
			["doomAttackResolved"] = _doomAttackResolved,
			["doomActionSequence"] = _doomActionSequence,
			["rvPosX"] = rvPos.X,
			["rvPosY"] = rvPos.Y,
			["networkSpecialStateRevision"] = _networkSpecialStateRevision
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		int num = data.GetValueOrDefault("bossDaveSaveVersion", 0).AsInt32();
		stage = data.GetValueOrDefault("stage", 0).AsInt32();
		stateNow = data.GetValueOrDefault("stateNow", "").AsString();
		useEnterAnime = data.GetValueOrDefault("useEnterAnime", true).AsBool();
		spawnZombieNumOnce = data.GetValueOrDefault("spawnZombieNumOnce", 5).AsInt32();
		spawnZombieNumNow = data.GetValueOrDefault("spawnZombieNumNow", 0).AsInt32();
		spawnTime = data.GetValueOrDefault("spawnTime", 5).AsDouble();
		spawnLine = data.GetValueOrDefault("spawnLine", 1).AsInt32();
		spawnRestTime = data.GetValueOrDefault("spawnRestTime", 4.0).AsDouble();
		spawnRestTimer = data.GetValueOrDefault("spawnRestTimer", 0.0).AsDouble();
		spawnZomie = data.GetValueOrDefault("spawnZomie", "").AsString();
		spawnNum = data.GetValueOrDefault("spawnNum", 0).AsInt32();
		headAttackOver = data.GetValueOrDefault("headAttackOver", false).AsBool();
		headAttackLine = data.GetValueOrDefault("headAttackLine", 1).AsInt32();
		headAttackIsFire = data.GetValueOrDefault("headAttackIsFire", true).AsBool();
		headAttackRestTime = data.GetValueOrDefault("headAttackRestTime", 10.0).AsDouble();
		headAttackRestTimer = data.GetValueOrDefault("headAttackRestTimer", 0.0).AsDouble();
		_headAttackPauseRemaining = data.GetValueOrDefault("headAttackPauseRemaining", 0.0).AsDouble();
		_headAttackPauseSequence = data.GetValueOrDefault("headAttackPauseSequence", 0).AsInt32();
		_headAttackPauseOwned = _headAttackPauseRemaining > 0.0;
		_clearLegacyHeadAttackPauseOnFinalize = !data.ContainsKey("headAttackPauseRemaining");
		headIdleNum = data.GetValueOrDefault("headIdleNum", 0).AsInt32();
		bungeeIsSpawn = data.GetValueOrDefault("bungeeIsSpawn", false).AsBool();
		_pendingBungeeNodeNames = ReadStringArray(data.GetValueOrDefault("bungeeNodeNames", new Array<string>()));
		_rvAttackArmed = data.GetValueOrDefault("rvAttackArmed", false).AsBool();
		stompId = data.GetValueOrDefault("stompId", -1).AsInt32();
		isRest = data.GetValueOrDefault("isRest", true).AsBool();
		restTime = data.GetValueOrDefault("restTime", 5.0).AsDouble();
		restTimer = data.GetValueOrDefault("restTimer", 0.0).AsDouble();
		stateMethodList = data.GetValueOrDefault("stateMethodList", new Godot.Collections.Array()).AsGodotArray();
		activeCrouchSkill = data.GetValueOrDefault("activeCrouchSkill", "").AsString();
		pendingCrouchShowcases = ReadStringArray(data.GetValueOrDefault("pendingCrouchShowcases", new Array<string>()));
		_missileShowcaseQueued = data.GetValueOrDefault("missileShowcaseQueued", false).AsBool();
		_doomShowcaseQueued = data.GetValueOrDefault("doomShowcaseQueued", false).AsBool();
		_plantAttackActive = data.GetValueOrDefault("plantAttackActive", false).AsBool();
		_plantSpawnResolved = data.GetValueOrDefault("plantSpawnResolved", false).AsBool();
		_plantActionSequence = data.GetValueOrDefault("plantActionSequence", 0).AsInt32();
		_missileAttackActive = data.GetValueOrDefault("missileAttackActive", false).AsBool();
		missileTargets = DeserializeGridPositions(data.GetValueOrDefault("missileTargets", new Array<int>()));
		missileTargetIndex = data.GetValueOrDefault("missileTargetIndex", 0).AsInt32();
		_missileActionSequence = data.GetValueOrDefault("missileActionSequence", 0).AsInt32();
		missileResolvedMask = data.GetValueOrDefault("missileResolvedMask", 0).AsInt32();
		_missileCrosshairTrackingActive = data.GetValueOrDefault("missileCrosshairTrackingActive", _missileAttackActive).AsBool();
		_doomCharging = data.GetValueOrDefault("doomCharging", false).AsBool();
		doomChargeRemaining = data.GetValueOrDefault("doomChargeRemaining", 0.0).AsDouble();
		_doomAttackResolved = data.GetValueOrDefault("doomAttackResolved", false).AsBool();
		_doomActionSequence = data.GetValueOrDefault("doomActionSequence", 0).AsInt32();
		rvPos = new Vector2I(data.GetValueOrDefault("rvPosX", 0).AsInt32(), data.GetValueOrDefault("rvPosY", 0).AsInt32());
		_networkSpecialStateRevision = data.GetValueOrDefault("networkSpecialStateRevision", 0).AsInt32();
		if (num < 1)
		{
			pendingCrouchShowcases.Clear();
			_missileShowcaseQueued = stage >= 1;
			_doomShowcaseQueued = stage >= 2;
			if (_missileShowcaseQueued)
			{
				pendingCrouchShowcases.Add("Missile");
			}
			if (_doomShowcaseQueued)
			{
				pendingCrouchShowcases.Add("Doom");
			}
		}
		if (num < 2)
		{
			missileResolvedMask = 0;
			_missileCrosshairTrackingActive = _missileAttackActive;
		}
		if (num < 3)
		{
			double num2 = data.GetValueOrDefault("missileLaunchDelayRemaining", 0.0).AsDouble();
			if (data.GetValueOrDefault("missileHoldActive", num2 > 0.0).AsBool() || num2 > 0.0)
			{
				_clearLegacyMissilePauseOnFinalize = true;
				_spawnLegacyDelayedMissileOnFinalize = _missileAttackActive && missileTargetIndex < missileTargets.Count;
				_legacyMissileEventFrame = data.GetValueOrDefault("missileHoldFrameIndex", 863).AsInt32();
			}
		}
		if (num < 4 && data.ContainsKey("doomAccumulatedDamage"))
		{
			_pendingLegacyDoomShieldDamage = data["doomAccumulatedDamage"].AsDouble();
			_hasPendingLegacyDoomShieldDamage = _doomCharging;
		}
		_hasPendingNetworkPresentation = true;
		if (!base.IsProgressRestoreInFlight && IsNodeReady())
		{
			Callable.From(ApplyPendingBossNetworkPresentation).CallDeferred();
		}
	}
}
