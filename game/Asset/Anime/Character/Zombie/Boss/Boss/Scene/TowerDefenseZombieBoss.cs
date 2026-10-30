using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefenseZombieBoss : TowerDefenseZombie
{
	private StateHandle _headIdleStateHandle;

	private StateHandle _headAttackStateHandle;

	private StateHandle _headExitedStateHandle;

	private StateHandle _spawnStateHandle;

	private StateHandle _stompStateHandle;

	private StateHandle _bungeeStateHandle;

	private StateHandle _rvStateHandle;

	private bool _stateSignalsConnected;

	private bool _bossGameplayInitialized;

	private static PackedScene _FIRE_BALL;

	private static PackedScene _ICE_BALL;

	private static PackedScene _EXPLOSION;

	private AttackComponent _attackComponent1;

	private AttackComponent _attackComponent2;

	private AttackComponent _attackComponent3;

	private AttackComponent _attackComponent4;

	private AttackComponent _attackComponent5;

	public Array stageSpawnList = new Array
	{
		new Array
		{
			new Array { "ZombieNormal" },
			new Array { "ZombieNormalCone" },
			new Array { "ZombieNormalBucket" },
			new Array { "ZombieNormalScreendoor", "ZombiePolevaulter", "ZombieSleeper", "ZombieFootball", "ZombieJackbox", "ZombiePogo", "ZombieLadder", "ZombieGargantuar", "ZombieZamboni", "ZombieCatapult" }
		},
		new Array
		{
			new Array { "ZombieNormalScreendoor", "ZombiePolevaulter", "ZombieSleeper", "ZombieFootball", "ZombieJackbox", "ZombiePogo", "ZombieLadder", "ZombieGargantuar", "ZombieZamboni", "ZombieCatapult" }
		},
		new Array
		{
			new Array { "ZombieNormalScreendoor", "ZombiePolevaulter", "ZombieSleeper", "ZombieFootball", "ZombieJackbox", "ZombiePogo", "ZombieLadder", "ZombieGargantuar", "ZombieZamboni", "ZombieCatapult" }
		},
		new Array
		{
			new Array { "ZombieNormalScreendoor", "ZombiePolevaulter", "ZombieSleeper", "ZombieFootball", "ZombieJackbox", "ZombiePogo", "ZombieLadder", "ZombieGargantuar", "ZombieZamboni", "ZombieCatapult" }
		}
	};

	public Array stateMethodList = new Array();

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

	public Array bungeeList = new Array();

	public int stompId = -1;

	public Vector2I rvPos;

	public bool isRest = true;

	public double restTime = 5.0;

	public double restTimer;

	public int headIdleNum;

	public int stage;

	private double _headAttackPauseRemaining;

	private bool _headAttackPauseOwned;

	private static PackedScene FIRE_BALL => _FIRE_BALL ?? (_FIRE_BALL = GD.Load<PackedScene>("uid://ulproewvrqwb"));

	private static PackedScene ICE_BALL => _ICE_BALL ?? (_ICE_BALL = GD.Load<PackedScene>("uid://calwfqgd47fn7"));

	private static PackedScene EXPLOSION => _EXPLOSION ?? (_EXPLOSION = GD.Load<PackedScene>("uid://c8xarvk5gxpf0"));

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
		if (bungeeStateHandle != null && bungeeStateHandle.IsValid)
		{
			StateHandle rvStateHandle = _rvStateHandle;
			if (rvStateHandle != null && rvStateHandle.IsValid)
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
			_stateSignalsConnected = false;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
	}

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ResolveBossRuntimeComponents();
			InitializeBossGameplay();
		}
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
			WaitForPhysicsFrame();
		}
	}

	private async Task WaitForPhysicsFrame()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		useEnterAnime = true;
	}

	private static bool IsBossGameplayPaused()
	{
		TowerDefenseManager towerDefenseManager = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(towerDefenseManager))
		{
			return towerDefenseManager.pauseZombie;
		}
		return false;
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
		if (!isRest)
		{
			return;
		}
		if (restTimer < restTime)
		{
			restTimer += delta;
			return;
		}
		restTimer = 0.0;
		stateNow = (string)stateMethodList[0];
		stateMethodList.RemoveAt(0);
		isRest = false;
		if (stateNow != "HeadIdle" && stateMethodList.Count == 0)
		{
			stateMethodList.Add("HeadIdle");
		}
	}

	public override void Walk()
	{
		Idle();
	}

	public void HeadIdleEntered()
	{
		AudioManager.Instance.AudioPlay("HydraulicShort");
		AudioManager.Instance.AudioPlay("Hydraulic");
		headAttackRestTimer = 0.0;
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
		if (IsBossGameplayPaused() || base.sprite.pause)
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
			SendStateEvent("ToHeadAttack");
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
		headAttackOver = true;
		headAttackLine = GD.RandRange(1, TowerDefenseManager.Instance.GetMapGridNum().Y);
		headAttackIsFire = (double)GD.Randf() > 0.5;
		SetTransientRenderZIndex((int)(headAttackLine * 15 + base.itemLayer), "zomboss-head-attack");
		ZombieBoss zombieBoss = base.sprite as ZombieBoss;
		if (GodotObject.IsInstanceValid(zombieBoss))
		{
			zombieBoss.SetHeadAttack(headAttackLine);
			zombieBoss.SetHeadAttackBall(headAttackIsFire);
		}
		base.sprite.SetAnimation("HeadAttack4", loop: false, 0.2);
	}

	public void HeadAttackProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void HeadAttackExited()
	{
		ReleaseHeadAttackPause(refreshLayer: false);
	}

	public void HeadExitedEntered()
	{
		instance.invincible = true;
		instance.canBeCollection = false;
		targetRegistrationComponent.canProjectileCheck = false;
		instance.unUseBuffFlags = -1;
		targetRegistrationComponent.allLineCheck = false;
		if (buff.BuffHas("Frozen"))
		{
			buff.DeleteBuff("Frozen");
		}
		if (buff.BuffHas("IceSpeedDown"))
		{
			buff.DeleteBuff("IceSpeedDown");
		}
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
		spawnZomie = GetSpawnZombie();
		spawnLine = GD.RandRange(1, TowerDefenseManager.Instance.GetMapGridNum().Y);
		base.sprite.SetAnimation("Spawn1", loop: false, 0.2);
		ZombieBoss zombieBoss = base.sprite as ZombieBoss;
		if (GodotObject.IsInstanceValid(zombieBoss))
		{
			zombieBoss.SetSpawn(spawnLine);
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
		Array<int> array = new Array<int>();
		if (_attackComponent1.CanAttackOnce())
		{
			array.Add(1);
		}
		if (_attackComponent2.CanAttackOnce())
		{
			array.Add(2);
		}
		if (_attackComponent3.CanAttackOnce())
		{
			array.Add(3);
		}
		if (_attackComponent4.CanAttackOnce())
		{
			array.Add(4);
		}
		if (array.Count > 0)
		{
			stompId = array.PickRandom();
		}
		else
		{
			stompId = 3;
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
		bungeeIsSpawn = false;
		base.sprite.SetAnimation("BungeeEnter", loop: false, 0.2);
	}

	public void BungeeProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
		if (!bungeeIsSpawn)
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
		rvPos = new Vector2I((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f - 1f), TowerDefenseManager.Instance.GetMapGridNum().Y - 1);
		base.sprite.SetAnimation("RV", loop: false, 0.2);
	}

	public void RVProcessing(double delta)
	{
		base.sprite.timeScale = timeScale * 1.0;
	}

	public void RVExited()
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
		ZombieBoss zombieBoss = base.sprite as ZombieBoss;
		if (base.sprite.clip == "HeadIdle" || base.sprite.clip == "HeadAttack4")
		{
			flag = true;
		}
		ReleaseHeadAttackPause(refreshLayer: false);
		base.DieEntered();
		if (GodotObject.IsInstanceValid(zombieBoss))
		{
			zombieBoss.SetHeadAttack((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2, 0.5);
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
		ZombieBoss zombieBoss = base.sprite as ZombieBoss;
		if (GodotObject.IsInstanceValid(zombieBoss))
		{
			zombieBoss.DamagePointSet(damagePointName);
		}
		switch (damagePointName)
		{
		case "Stage1":
			stage = 1;
			restTime = 4.5;
			spawnRestTime = 3.5;
			break;
		case "Stage2":
			stage = 2;
			restTime = 4.0;
			spawnRestTime = 3.0;
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
		if (base.sprite is ZombieBoss zombieBoss)
		{
			zombieBoss.AnimeCompleted(clip);
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
					goto IL_0219;
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
				goto IL_0219;
			case '3':
				if (!(clip == "Stomp3"))
				{
					break;
				}
				goto IL_0219;
			case '4':
				{
					if (!(clip == "Stomp4"))
					{
						break;
					}
					goto IL_0219;
				}
				IL_0219:
				isRest = true;
				if (!die)
				{
					Idle();
				}
				break;
			}
			break;
		case 9:
			if (clip == "HeadEnter")
			{
				instance.invincible = false;
				instance.canBeCollection = true;
				targetRegistrationComponent.canProjectileCheck = true;
				targetRegistrationComponent.allLineCheck = true;
				instance.unUseBuffFlags = -4;
			}
			break;
		case 10:
			if (clip == "HeadExited")
			{
				FreshZIndex();
				SetStateList();
				isRest = true;
				restTimer = 2.0;
				if (!die)
				{
					Idle();
				}
			}
			break;
		case 11:
			if (clip == "HeadAttack4")
			{
				SendStateEvent("ToHeadIdle");
			}
			break;
		case 12:
			if (clip == "BungeeExited")
			{
				FreshZIndex();
				isRest = true;
				if (!die)
				{
					Idle();
				}
			}
			break;
		case 2:
			if (clip == "RV")
			{
				isRest = true;
				if (!die)
				{
					Idle();
				}
			}
			break;
		case 3:
		case 4:
		case 5:
		case 7:
		case 8:
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
				if (command == "stomp")
				{
					AudioManager.Instance.AudioPlay("GargantuarThump");
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
		case 7:
			switch (command[0])
			{
			case 't':
				if (command == "throwRV")
				{
					ZombieBoss zombieBoss2 = base.sprite as ZombieBoss;
					if (GodotObject.IsInstanceValid(zombieBoss2))
					{
						zombieBoss2.SetRVVisible(visible: true);
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
			}
			break;
		case 8:
			switch (command[0])
			{
			case 'R':
				if (command == "RVAttack")
				{
					AudioManager.Instance.AudioPlay("RVThrow");
					_attackComponent5.SmashAttackAll(100000.0);
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
		case 10:
			if (command == "headAttack")
			{
				BallSpawn();
				BeginHeadAttackPause();
			}
			break;
		case 14:
			if (command == "headAttackOver")
			{
				ZombieBoss zombieBoss = base.sprite as ZombieBoss;
				if (GodotObject.IsInstanceValid(zombieBoss))
				{
					zombieBoss.SetHeadAttack((int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 2, 0.5);
				}
			}
			break;
		case 6:
		case 9:
		case 12:
		case 13:
			break;
		}
	}

	private void BeginHeadAttackPause()
	{
		_headAttackPauseRemaining = 1.0;
		_headAttackPauseOwned = true;
		spritePause = true;
	}

	private void UpdateHeadAttackPause(double delta)
	{
		if (!IsBossGameplayPaused() && !(_headAttackPauseRemaining <= 0.0))
		{
			_headAttackPauseRemaining = Mathf.Max(0.0, _headAttackPauseRemaining - delta);
			if (!(_headAttackPauseRemaining > 0.0))
			{
				ReleaseHeadAttackPause(refreshLayer: true);
			}
		}
	}

	private void ReleaseHeadAttackPause(bool refreshLayer)
	{
		bool num = _headAttackPauseRemaining > 0.0 || _headAttackPauseOwned;
		_headAttackPauseRemaining = 0.0;
		if (_headAttackPauseOwned)
		{
			spritePause = false;
		}
		_headAttackPauseOwned = false;
		if (num && refreshLayer)
		{
			SetTransientRenderZIndex((int)(headAttackLine * 15 + base.itemLayer), "zomboss-head-attack-delay");
		}
	}

	public void StateRunning(double delta)
	{
		if (IsBossGameplayPaused())
		{
			return;
		}
		switch (stateNow)
		{
		case "Spawn":
			if (spawnRestTimer < spawnRestTime)
			{
				spawnRestTimer += delta;
				break;
			}
			SendStateEvent("ToSpawn");
			spawnZombieNumNow++;
			spawnRestTimer = 0.0;
			if (spawnZombieNumNow >= spawnZombieNumOnce)
			{
				spawnNum++;
				spawnZombieNumNow = 0;
				isRest = true;
				stateNow = "";
			}
			break;
		case "HeadIdle":
			headAttackOver = false;
			headIdleNum++;
			SendStateEvent("ToHeadIdle");
			stateNow = "";
			break;
		case "BungeeSpawn":
			SendStateEvent("ToBungee");
			stateNow = "";
			break;
		case "Stomp":
			SendStateEvent("ToStomp");
			stateNow = "";
			break;
		case "RV":
			SendStateEvent("ToRV");
			stateNow = "";
			break;
		}
	}

	public void ZombieSpawn()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(spawnZomie);
		Node2D node2D = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(node2D))
		{
			return;
		}
		Vector2 vector = new Vector2((base.sprite as ZombieBoss)?.GetSpawnMarkerGlobalPos(this).X ?? GetLogicalGlobalPosition().X, TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, spawnLine)).Y);
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
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		int num = (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f) + 1;
		foreach (TowerDefenseCharacter item2 in cleanCharactersList)
		{
			if (item2 is TowerDefensePlant && item2.gridPos.X <= num)
			{
				list.Add(item2);
			}
		}
		Array<Vector2I> array = new Array<Vector2I>();
		foreach (TowerDefenseCharacter item3 in list)
		{
			if (!array.Contains(item3.gridPos))
			{
				array.Add(item3.gridPos);
			}
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieBungi");
		Array<Vector2I> array2 = new Array<Vector2I>();
		for (int i = 0; i < Mathf.Min(3, array.Count); i++)
		{
			Vector2I item = array.PickRandom();
			array.Remove(item);
			array2.Add(item);
			TowerDefenseZombieBungi towerDefenseZombieBungi = packetConfig.Plant(item, playAudio: false) as TowerDefenseZombieBungi;
			towerDefenseZombieBungi.skipBungeeTarget = true;
			bungeeList.Add(towerDefenseZombieBungi);
			if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
			{
				TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
				if (GodotObject.IsInstanceValid(currentControl))
				{
					int nextSyncId = currentControl.GetNextSyncId();
					currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseZombieBungi);
					MultiPlayerManager.Instance.SendSpawnCharacterAt("ZombieBungi", item.X, item.Y, nextSyncId);
				}
			}
		}
		bungeeIsSpawn = true;
	}

	public void RVSpawn()
	{
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		int num = (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().X / 2f) - 1;
		foreach (TowerDefenseCharacter item in cleanCharactersList)
		{
			if (item is TowerDefensePlant && item.gridPos.X <= num)
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
		Vector2I vector2I = ((array.Count <= 0) ? new Vector2I(1, 2) : array.PickRandom());
		if (vector2I.Y >= TowerDefenseManager.Instance.GetMapGridNum().Y)
		{
			vector2I.Y--;
		}
		rvPos = vector2I;
		_attackComponent5.SetCheckAreaShapeWorldOrigin(0, TowerDefenseManager.Instance.GetMapCellPos(rvPos) + TowerDefenseManager.Instance.GetMapGridSize() * new Vector2(1.5f, 0.5f));
		ZombieBoss zombieBoss = base.sprite as ZombieBoss;
		if (GodotObject.IsInstanceValid(zombieBoss))
		{
			zombieBoss.SetRVPos(rvPos);
		}
	}

	public void BallSpawn()
	{
		AudioManager.Instance.AudioPlay("Bossboulderattack");
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		Vector2 vector = new Vector2((base.sprite as ZombieBoss)?.GetBallSpawnMarkerGlobalPos(this).X ?? GetLogicalGlobalPosition().X, TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, headAttackLine)).Y);
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
		Array array = new Array();
		Array array2 = ((stage >= stageSpawnList.Count) ? ((Array)stageSpawnList[stageSpawnList.Count - 1]) : ((Array)stageSpawnList[stage]));
		array = ((spawnNum >= array2.Count) ? ((Array)array2[array2.Count - 1]) : ((Array)array2[spawnNum]));
		return TowerDefenseManager.Instance.PickRandomZomie(array);
	}

	public void SetStateList()
	{
		switch (headIdleNum)
		{
		case 0:
			stateMethodList = new Array { "Spawn", "Spawn" };
			break;
		case 1:
			stateMethodList = new Array { "Spawn" };
			break;
		default:
			stateMethodList = new Array { "Spawn" };
			break;
		}
		if (stage > 1)
		{
			if ((double)GD.Randf() > 0.25)
			{
				stateMethodList.Insert(0, "BungeeSpawn");
			}
			else
			{
				stateMethodList.Insert(0, "RV");
			}
			if (_attackComponent1.CanAttackOnce() || _attackComponent2.CanAttackOnce() || _attackComponent3.CanAttackOnce() || _attackComponent4.CanAttackOnce())
			{
				stateMethodList.Insert(0, "Stomp");
			}
		}
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

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
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
			["headIdleNum"] = headIdleNum,
			["bungeeIsSpawn"] = bungeeIsSpawn,
			["stompId"] = stompId,
			["isRest"] = isRest,
			["restTime"] = restTime,
			["restTimer"] = restTimer,
			["stateMethodList"] = stateMethodList
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
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
		headIdleNum = data.GetValueOrDefault("headIdleNum", 0).AsInt32();
		bungeeIsSpawn = data.GetValueOrDefault("bungeeIsSpawn", false).AsBool();
		stompId = data.GetValueOrDefault("stompId", -1).AsInt32();
		isRest = data.GetValueOrDefault("isRest", true).AsBool();
		restTime = data.GetValueOrDefault("restTime", 5.0).AsDouble();
		restTimer = data.GetValueOrDefault("restTimer", 0.0).AsDouble();
		stateMethodList = data.GetValueOrDefault("stateMethodList", new Array()).AsGodotArray();
		if (stage >= 1 && GodotObject.IsInstanceValid(base.sprite) && base.sprite is ZombieBoss zombieBoss)
		{
			zombieBoss.DamagePointSet("Stage1");
		}
		if (stage >= 2 && GodotObject.IsInstanceValid(base.sprite) && base.sprite is ZombieBoss zombieBoss2)
		{
			zombieBoss2.DamagePointSet("Stage2");
		}
		if (stage >= 3 && GodotObject.IsInstanceValid(base.sprite) && base.sprite is ZombieBoss zombieBoss3)
		{
			zombieBoss3.DamagePointSet("Stage3");
		}
	}
}
