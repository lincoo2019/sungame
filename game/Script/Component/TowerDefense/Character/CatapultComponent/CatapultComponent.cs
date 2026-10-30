using System;
using Godot;
using Godot.Collections;

public sealed class CatapultComponent : CharacterComponentRuntime
{
	public delegate void FireEventEventHandler();

	public delegate void ProjectileDepletedEventHandler();

	public delegate void DamagePointReachedEventHandler(string damagePointName);

	private const int PrimaryFireCheckIndex = 0;

	private const double MinimumAnimationDivisor = 0.001;

	public double speed = 50.0;

	public double damageSpeedReduction = 40.0;

	public double lowHealthThreshold = 0.2;

	public double lowHealthDeceleration = 1.0;

	public double lowHealthMinSpeed = 5.0;

	public bool lowHealthShake = true;

	public double outsideMapSpeedMultiplier = 2.0;

	public double idleBoundaryOffsetRatio = 0.5;

	public double fireInterval = 3.0;

	public int fireNum = 1;

	public int projectileNum = 20;

	public string projectileName = "Basketball";

	public double fireSpeedMultiplier = 3.0;

	public string fireAudioName = "Basketball";

	public bool useCanFireCheck = true;

	public bool refreshInFireEntered = true;

	public bool hostAuthoritativeRuntime = true;

	public StringName fireStateEvent = "ToFire";

	public StringName idleStateEvent = "ToIdle";

	public string walkAnimeClip = "Walk";

	public string fireAnimeClip = "Fire";

	public double walkAnimationSpeedMultiplier = 0.5;

	public double fireAnimationBaseSpeed = 1.75;

	public double fireAnimationIntervalOffset = 0.25;

	public double fireAnimationStartScale = 1.0 / 30.0;

	public double fireAnimationStartIntervalOffset = 4.5;

	public double repeatFireAnimationStart = 0.1;

	public string speedDamagePointName = "DamagePoint2";

	public bool showSmokeOnSpeedDamage = true;

	public PackedScene explosionEffect;

	public Vector2 explosionOffset = Vector2.Zero;

	public string explosionAudioName = "ZamboniExplosion";

	public bool cameraShakeEnabled = true;

	public double cameraShakeStrength = 5.0;

	public double cameraShakeDuration = 0.05;

	public int cameraShakeFrequency = 4;

	public Vector2 cameraShakeRange = Vector2.One;

	public GpuParticles2D smokeParticle;

	public AdobeAnimateSlot fireSlot;

	public TowerDefenseZombie parent;

	public FireComponent fireComponent;

	public AttackComponent attackComponent;

	public int currentProjectileNum;

	public int currentFireNum;

	public bool fireOver;

	public bool isFire;

	private double _groundRight;

	private float _gridSizeX;

	private bool _initialized;

	private bool _ammoInitialized;

	private bool _initialCooldownPrepared;

	private bool _waitingForParentReady;

	private StateHandle _fireState;

	private bool _fireStateSignalsConnected;

	private bool _configured;

	private ulong _lastFireAttemptPhysicsFrame;

	private bool _hasFireAttemptPhysicsFrame;

	protected override bool AllowPhysicsOutsideComponentBattlefield => true;

	protected override bool AllowStateMachineOutsideComponentBattlefield => true;

	private CatapultComponentDefinition Definition => base.ComponentDefinition as CatapultComponentDefinition;

	internal override bool WantsPhysicsProcess => true;

	public event FireEventEventHandler OnFireEvent;

	public event ProjectileDepletedEventHandler OnProjectileDepleted;

	public event DamagePointReachedEventHandler OnDamagePointReached;

	protected override void OnBound()
	{
		parent = base.Owner as TowerDefenseZombie;
		if (GodotObject.IsInstanceValid(parent))
		{
			ApplyDefinitionOnce();
			ResolveReferences();
			InitializeAmmoOnce();
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				InitializeComponent();
			}
		}
	}

	protected override void OnActivated()
	{
		Callable.From(ReattachAfterTreeEntry).CallDeferred();
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		DisconnectParentReady();
		fireComponent = null;
		attackComponent = null;
		smokeParticle = null;
		fireSlot = null;
		parent = null;
		_initialized = false;
		_hasFireAttemptPhysicsFrame = false;
	}

	protected override void OnReleased()
	{
		DisconnectParentReady();
		this.OnFireEvent = null;
		this.OnProjectileDepleted = null;
		this.OnDamagePointReached = null;
		fireComponent = null;
		attackComponent = null;
		smokeParticle = null;
		fireSlot = null;
		explosionEffect = null;
		parent = null;
		_initialized = false;
		_hasFireAttemptPhysicsFrame = false;
	}

	private void ApplyDefinitionOnce()
	{
		if (!_configured && Definition != null)
		{
			CatapultComponentDefinition definition = Definition;
			speed = definition.speed;
			damageSpeedReduction = definition.damageSpeedReduction;
			lowHealthThreshold = definition.lowHealthThreshold;
			lowHealthDeceleration = definition.lowHealthDeceleration;
			lowHealthMinSpeed = definition.lowHealthMinSpeed;
			lowHealthShake = definition.lowHealthShake;
			outsideMapSpeedMultiplier = definition.outsideMapSpeedMultiplier;
			idleBoundaryOffsetRatio = definition.idleBoundaryOffsetRatio;
			fireInterval = definition.fireInterval;
			fireNum = definition.fireNum;
			projectileNum = definition.projectileNum;
			projectileName = definition.projectileName ?? "Basketball";
			fireSpeedMultiplier = definition.fireSpeedMultiplier;
			fireAudioName = definition.fireAudioName ?? "Basketball";
			useCanFireCheck = definition.useCanFireCheck;
			refreshInFireEntered = definition.refreshInFireEntered;
			hostAuthoritativeRuntime = definition.hostAuthoritativeRuntime;
			fireStateEvent = definition.fireStateEvent;
			idleStateEvent = definition.idleStateEvent;
			walkAnimeClip = definition.walkAnimeClip ?? "Walk";
			fireAnimeClip = definition.fireAnimeClip ?? "Fire";
			walkAnimationSpeedMultiplier = definition.walkAnimationSpeedMultiplier;
			fireAnimationBaseSpeed = definition.fireAnimationBaseSpeed;
			fireAnimationIntervalOffset = definition.fireAnimationIntervalOffset;
			fireAnimationStartScale = definition.fireAnimationStartScale;
			fireAnimationStartIntervalOffset = definition.fireAnimationStartIntervalOffset;
			repeatFireAnimationStart = definition.repeatFireAnimationStart;
			speedDamagePointName = definition.speedDamagePointName ?? "DamagePoint2";
			showSmokeOnSpeedDamage = definition.showSmokeOnSpeedDamage;
			explosionEffect = definition.explosionEffect;
			explosionOffset = definition.explosionOffset;
			explosionAudioName = definition.explosionAudioName ?? "ZamboniExplosion";
			cameraShakeEnabled = definition.cameraShakeEnabled;
			cameraShakeStrength = definition.cameraShakeStrength;
			cameraShakeDuration = definition.cameraShakeDuration;
			cameraShakeFrequency = definition.cameraShakeFrequency;
			cameraShakeRange = definition.cameraShakeRange;
			_configured = true;
		}
	}

	private void ResolveReferences()
	{
		fireComponent = base.Manager?.GetComponent<FireComponent>();
		attackComponent = base.Manager?.GetRuntime<AttackComponent>("character.attack.0");
		CatapultComponentDefinition definition = Definition;
		if (GodotObject.IsInstanceValid(parent) && definition != null)
		{
			smokeParticle = ResolveNode<GpuParticles2D>(definition.smokeParticlePath);
			fireSlot = ResolveNode<AdobeAnimateSlot>(definition.fireSlotPath);
		}
	}

	private T ResolveNode<T>(NodePath path) where T : Node
	{
		if (!(path == null) && !path.IsEmpty && GodotObject.IsInstanceValid(parent))
		{
			return parent.GetNodeOrNull<T>(path);
		}
		return null;
	}

	private void InitializeAmmoOnce()
	{
		if (!_ammoInitialized)
		{
			currentProjectileNum = Math.Max(0, projectileNum);
			_ammoInitialized = true;
		}
	}

	private void ReattachAfterTreeEntry()
	{
		if (base.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(parent) && parent.IsInsideTree())
		{
			ResolveReferences();
			if (!parent.IsNodeReady())
			{
				ConnectParentReady();
			}
			else
			{
				InitializeComponent();
			}
		}
	}

	private void ConnectParentReady()
	{
		if (!_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready += OnParentReady;
			_waitingForParentReady = true;
		}
	}

	private void DisconnectParentReady()
	{
		if (_waitingForParentReady && GodotObject.IsInstanceValid(parent))
		{
			parent.Ready -= OnParentReady;
		}
		_waitingForParentReady = false;
	}

	private void OnParentReady()
	{
		DisconnectParentReady();
		InitializeComponent();
	}

	private void InitializeComponent()
	{
		if (_initialized || !GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		ResolveReferences();
		InitializeAmmoOnce();
		RefreshMapMetrics();
		_initialized = true;
		ApplyFireConfiguration();
		if (!_initialCooldownPrepared)
		{
			FireComponent fireComponent = this.fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				this.fireComponent.Refresh();
				_initialCooldownPrepared = true;
			}
		}
	}

	protected override void OnStateRuntimeAttached()
	{
		_fireState = base.StateMachine?.GetStateById("catapult.fire");
		ConnectFireStateSignals();
		Callable.From(InitializeComponent).CallDeferred();
	}

	protected override void OnStateRuntimeRegistered()
	{
		InitializeComponent();
	}

	protected override void OnStateRuntimeDetaching()
	{
		DisconnectFireStateSignals();
	}

	private void ConnectFireStateSignals()
	{
		if (_fireStateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			StateHandle fireState = _fireState;
			if (fireState != null && fireState.IsValid)
			{
				_fireState.Entered += FireEntered;
				_fireState.PhysicsProcessing += FireProcessing;
				_fireStateSignalsConnected = true;
			}
		}
	}

	private void DisconnectFireStateSignals()
	{
		if (_fireStateSignalsConnected)
		{
			if (_fireState != null)
			{
				_fireState.Entered -= FireEntered;
				_fireState.PhysicsProcessing -= FireProcessing;
			}
			_fireState = null;
			_fireStateSignalsConnected = false;
		}
	}

	public void RefreshMapMetrics()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			_groundRight = instance.GetMapGroundRight();
			_gridSizeX = instance.GetMapGridSize().X;
		}
	}

	public void ApplyFireConfiguration()
	{
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent != null && !fireComponent.IsReleased)
		{
			this.fireComponent.fireInterval = (float)Math.Max(0.0, fireInterval);
			this.fireComponent.fireNum = GetEffectiveFireNum();
			if (TryGetPrimaryProjectile(out var projectile) && projectile is FireComponentProjectileSingle fireComponentProjectileSingle)
			{
				fireComponentProjectileSingle.projectileName = projectileName;
			}
		}
	}

	internal override void PhysicsProcess(double delta, ulong physicsFrame)
	{
		if (IsOperational() && !IsRemoteSyncedClient() && TowerDefenseManager._IsGameRunning() && parent.inGame && !parent.die && !parent.nearDie)
		{
			if (base.CanExecuteGameplay)
			{
				TryBeginFiring(physicsFrame);
			}
			if (!isFire)
			{
				MoveForward(delta, stopAtIdleBoundary: false, physicsFrame);
			}
			ProcessContactAttack();
			ApplyLowHealthMovement(delta);
		}
	}

	public void IdleProcessing(double delta)
	{
		if (!IsOperational() || IsRemoteSyncedClient())
		{
			return;
		}
		StateHandle fireState = _fireState;
		if (fireState != null && fireState.IsActive)
		{
			return;
		}
		if (!isFire)
		{
			MoveForward(delta, stopAtIdleBoundary: true);
		}
		if (fireOver || !isFire)
		{
			StopFiring();
			return;
		}
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent == null || fireComponent.IsReleased)
		{
			StopFiring();
		}
		else if (useCanFireCheck)
		{
			bool flag = this.fireComponent.timer <= 0f && this.fireComponent.checkIntreval <= 0;
			if (TryGetPrimaryProjectileData(out var projectileData) && this.fireComponent.CanFireByData(projectileData))
			{
				SendFireStateEvent();
			}
			else if (flag)
			{
				StopFiring();
			}
		}
		else if (this.fireComponent.timer <= 0f)
		{
			SendFireStateEvent();
		}
	}

	public void WalkEntered()
	{
		if (IsOperational())
		{
			parent.sprite.SetAnimation(walkAnimeClip);
		}
	}

	public void WalkProcessing(double delta)
	{
		if (!IsOperational())
		{
			return;
		}
		parent.sprite.timeScale = parent.timeScale * walkAnimationSpeedMultiplier;
		if (!IsRemoteSyncedClient())
		{
			if (!fireOver && !isFire)
			{
				TryBeginFiring(TowerDefenseProcessModeDispatch.CurrentPhysicsFrame);
			}
			else if (isFire)
			{
				parent.Idle();
			}
		}
	}

	private bool TryBeginFiring(ulong physicsFrame)
	{
		if (_hasFireAttemptPhysicsFrame && _lastFireAttemptPhysicsFrame == physicsFrame)
		{
			return isFire;
		}
		_hasFireAttemptPhysicsFrame = true;
		_lastFireAttemptPhysicsFrame = physicsFrame;
		if (!CanStartFire(physicsFrame))
		{
			return false;
		}
		isFire = true;
		parent.Idle();
		return true;
	}

	public void FireEntered()
	{
		if (!IsOperational())
		{
			return;
		}
		if (!IsRemoteSyncedClient() && refreshInFireEntered)
		{
			FireComponent fireComponent = this.fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				this.fireComponent.Refresh();
			}
		}
		double val = fireAnimationStartScale * (fireInterval + fireAnimationStartIntervalOffset);
		parent.sprite.SetAnimation(fireAnimeClip, loop: true, (float)Math.Max(0.0, val));
	}

	public void FireProcessing(double delta)
	{
		if (IsOperational())
		{
			double num = Math.Max(0.001, fireInterval + fireAnimationIntervalOffset);
			double num2 = (TowerDefenseProcessModeDispatch.IsIZMModeForCurrentPhysicsFrame ? 1.0 : parent.timeScale);
			parent.sprite.timeScale = num2 * (parent.buff?.GetAttackSpeedMultiplier() ?? 1.0) * fireSpeedMultiplier * (fireAnimationBaseSpeed / num);
		}
	}

	public void OnFireAnimeEvent()
	{
		if (!IsOperational() || fireOver)
		{
			return;
		}
		if (!string.IsNullOrEmpty(fireAudioName))
		{
			AudioManager.Instance.AudioPlay(fireAudioName);
		}
		if (GodotObject.IsInstanceValid(fireSlot))
		{
			fireSlot.Update();
		}
		if (IsRemoteSyncedClient())
		{
			return;
		}
		if (!refreshInFireEntered)
		{
			FireComponent fireComponent = this.fireComponent;
			if (fireComponent != null && !fireComponent.IsReleased)
			{
				this.fireComponent.Refresh();
			}
		}
		this.OnFireEvent?.Invoke();
		currentProjectileNum = Math.Max(0, currentProjectileNum - 1);
		currentFireNum++;
		if (currentProjectileNum <= 0)
		{
			isFire = false;
			fireOver = true;
			this.OnProjectileDepleted?.Invoke();
		}
		if (fireOver || currentFireNum >= GetEffectiveFireNum())
		{
			currentFireNum = 0;
		}
		else
		{
			parent.sprite.SetAnimation(fireAnimeClip, loop: true, (float)Math.Max(0.0, repeatFireAnimationStart));
		}
	}

	public void OnFireAnimeCompleted()
	{
		if (IsOperational() && currentFireNum == 0)
		{
			SendIdleStateEvent();
			parent.Idle();
		}
	}

	public void OnDamagePoint(string damagePointName)
	{
		if (!CanHandleDamagePoint())
		{
			return;
		}
		if (damagePointName == speedDamagePointName)
		{
			speed = damageSpeedReduction;
			if (showSmokeOnSpeedDamage && GodotObject.IsInstanceValid(smokeParticle))
			{
				smokeParticle.Visible = true;
			}
		}
		this.OnDamagePointReached?.Invoke(damagePointName);
	}

	public void CreateDeathEffect()
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return;
		}
		if (!string.IsNullOrEmpty(explosionAudioName))
		{
			AudioManager.Instance.AudioPlay(explosionAudioName);
		}
		if (cameraShakeEnabled && GodotObject.IsInstanceValid(ViewManager.Instance))
		{
			Vector2 dir = new Vector2((float)GD.RandRange(0f - cameraShakeRange.X, cameraShakeRange.X), (float)GD.RandRange(0f - cameraShakeRange.Y, cameraShakeRange.Y));
			ViewManager.Instance.CameraShake(dir, Math.Max(0.0, cameraShakeStrength), Math.Max(0.0, cameraShakeDuration), Math.Max(0, cameraShakeFrequency));
		}
		if (explosionEffect == null)
		{
			return;
		}
		TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce = TowerDefenseManager.CreateEffectParticlesOnce(explosionEffect, parent.gridPos);
		if (GodotObject.IsInstanceValid(towerDefenseEffectParticlesOnce))
		{
			towerDefenseEffectParticlesOnce.GlobalPosition = parent.GetLogicalGlobalPosition() + explosionOffset;
			if (GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
			{
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectParticlesOnce, forceReadableName: false, Node.InternalMode.Disabled);
			}
			else
			{
				towerDefenseEffectParticlesOnce.QueueFree();
			}
		}
	}

	public bool CanStartFire()
	{
		if (!base.CanExecuteGameplay || !IsOperational() || IsRemoteSyncedClient() || fireOver || isFire)
		{
			return false;
		}
		return CanStartFireAtPosition(parent.GetLogicalGlobalPosition());
	}

	private bool CanStartFire(ulong physicsFrame)
	{
		if (!base.CanExecuteGameplay || !IsOperational() || IsRemoteSyncedClient() || fireOver || isFire)
		{
			return false;
		}
		return CanStartFireAtPosition(parent.GetGlobalPositionForPhysicsFrame(physicsFrame));
	}

	private bool CanStartFireAtPosition(Vector2 parentPosition)
	{
		if ((!GodotObject.IsInstanceValid(parent.instance) || !parent.instance.hypnoses) && (double)parentPosition.X >= GetIdleBoundaryX())
		{
			return false;
		}
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent == null || fireComponent.IsReleased)
		{
			return false;
		}
		if (!useCanFireCheck)
		{
			return this.fireComponent.timer <= 0f;
		}
		if (TryGetPrimaryProjectileData(out var projectileData))
		{
			return this.fireComponent.CanFireByData(projectileData);
		}
		return false;
	}

	public int GetDepletionLevel(int numLevels = 4)
	{
		if (numLevels <= 0)
		{
			return 0;
		}
		if (projectileNum <= 0 || currentProjectileNum <= 0)
		{
			return numLevels;
		}
		int num = projectileNum - currentProjectileNum;
		if (num <= 0)
		{
			return 0;
		}
		double num2 = Math.Clamp((double)num / (double)projectileNum, 0.0, 1.0);
		return Math.Min(numLevels, (int)Math.Ceiling(num2 * (double)numLevels));
	}

	public override Dictionary ExportComponentSave()
	{
		return new Dictionary
		{
			{ "currentProjectileNum", currentProjectileNum },
			{ "currentFireNum", currentFireNum },
			{ "fireOver", fireOver },
			{ "isFire", isFire },
			{ "speed", speed }
		};
	}

	public override void ImportComponentSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner)
	{
		currentProjectileNum = Math.Max(0, data.GetValueOrDefault("currentProjectileNum", projectileNum).AsInt32());
		currentFireNum = Math.Max(0, data.GetValueOrDefault("currentFireNum", 0).AsInt32());
		fireOver = data.GetValueOrDefault("fireOver", false).AsBool();
		isFire = data.GetValueOrDefault("isFire", false).AsBool();
		speed = data.GetValueOrDefault("speed", speed).AsDouble();
		_ammoInitialized = true;
	}

	public override Dictionary SyncSerialize()
	{
		return ExportComponentSave();
	}

	public override void SyncDeserialize(Dictionary data)
	{
		currentProjectileNum = Math.Max(0, data.GetValueOrDefault("currentProjectileNum", currentProjectileNum).AsInt32());
		currentFireNum = Math.Max(0, data.GetValueOrDefault("currentFireNum", currentFireNum).AsInt32());
		fireOver = data.GetValueOrDefault("fireOver", fireOver).AsBool();
		isFire = data.GetValueOrDefault("isFire", isFire).AsBool();
		speed = data.GetValueOrDefault("speed", speed).AsDouble();
		_ammoInitialized = true;
	}

	private void MoveForward(double delta, bool stopAtIdleBoundary)
	{
		MoveForward(delta, stopAtIdleBoundary, 0uL, usePhysicsFrame: false);
	}

	private void MoveForward(double delta, bool stopAtIdleBoundary, ulong physicsFrame)
	{
		MoveForward(delta, stopAtIdleBoundary, physicsFrame, usePhysicsFrame: true);
	}

	private void MoveForward(double delta, bool stopAtIdleBoundary, ulong physicsFrame, bool usePhysicsFrame)
	{
		Vector2 vector = (usePhysicsFrame ? parent.GetGlobalPositionForPhysicsFrame(physicsFrame) : parent.GetLogicalGlobalPosition());
		if (!parent.sprite.pause && (!stopAtIdleBoundary || !((double)vector.X <= GetIdleBoundaryX())))
		{
			double num = (parent.sprite.playBack ? (-1.0) : 1.0);
			double num2 = (((double)vector.X > _groundRight) ? Math.Max(0.0, outsideMapSpeedMultiplier) : 1.0);
			double num3 = parent.timeScale * walkAnimationSpeedMultiplier;
			double num4 = speed * delta * num3 * (double)parent.transformPoint.Scale.X * (double)parent.Scale.X * num2 * num;
			Vector2 vector2 = new Vector2(vector.X - (float)num4, vector.Y);
			if (usePhysicsFrame)
			{
				parent.SetGlobalPositionForPhysicsFrame(vector2, physicsFrame);
			}
			else
			{
				parent.SetLogicalGlobalPosition(vector2);
			}
		}
	}

	private void ProcessContactAttack()
	{
		AttackComponent obj = attackComponent;
		if (obj == null || obj.IsReleased || !attackComponent.CanAttack())
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = attackComponent.target;
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			return;
		}
		if (GodotObject.IsInstanceValid(towerDefenseCharacter.cell) && towerDefenseCharacter.cell.HasSpike())
		{
			TowerDefenseCharacter spike = towerDefenseCharacter.cell.GetSpike();
			if (!GodotObject.IsInstanceValid(spike) || !GodotObject.IsInstanceValid(spike.instance))
			{
				return;
			}
			towerDefenseCharacter = spike;
			attackComponent.target = spike;
		}
		if ((towerDefenseCharacter.instance.physiqueTypeFlags & 0x10) == 0)
		{
			if (parent.config is TowerDefenseZombieConfig towerDefenseZombieConfig)
			{
				attackComponent.SmashAttackCell(towerDefenseZombieConfig.smashAttack);
			}
			return;
		}
		if (towerDefenseCharacter.instance.spikeHurt != -1.0)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = towerDefenseCharacter;
			double spikeHurt = towerDefenseCharacter.instance.spikeHurt;
			towerDefenseCharacter2.Hurt(100000.0, playSplatAudio: true, default(Vector2), createDamagePart: true, spikeHurt);
		}
		parent.Die();
	}

	private void ApplyLowHealthMovement(double delta)
	{
		if (!GodotObject.IsInstanceValid(parent.instance) || !GodotObject.IsInstanceValid(parent.config))
		{
			return;
		}
		double num = parent.config.hitpoints + parent.config.hitpointsNearDeath;
		double num2 = Math.Clamp(lowHealthThreshold, 0.0, 1.0);
		if (!(num <= 0.0) && !(parent.instance.hitpoints >= num * num2))
		{
			if (speed > lowHealthMinSpeed)
			{
				speed = Math.Max(lowHealthMinSpeed, speed - Math.Max(0.0, lowHealthDeceleration) * delta);
			}
			if (lowHealthShake)
			{
				parent.sprite.Set("shake", true);
			}
		}
	}

	private void StopFiring()
	{
		isFire = false;
		SendIdleStateEvent();
		parent.Walk();
	}

	private void SendFireStateEvent()
	{
		SendStateEvent(fireStateEvent);
	}

	private void SendIdleStateEvent()
	{
		SendStateEvent(idleStateEvent);
	}

	private bool TryGetPrimaryProjectile(out FireComponentProjectileResource projectile)
	{
		projectile = null;
		FireComponent fireComponent = this.fireComponent;
		if (fireComponent == null || fireComponent.IsReleased || this.fireComponent.fireCheckList == null || this.fireComponent.fireCheckList.Count <= 0)
		{
			return false;
		}
		FireComponentCheckConfig fireComponentCheckConfig = this.fireComponent.fireCheckList[0];
		if (!GodotObject.IsInstanceValid(fireComponentCheckConfig) || !GodotObject.IsInstanceValid(fireComponentCheckConfig.projectile))
		{
			return false;
		}
		projectile = fireComponentCheckConfig.projectile;
		return true;
	}

	private bool TryGetPrimaryProjectileData(out TowerDefenseProjectileCreateData projectileData)
	{
		projectileData = null;
		if (!TryGetPrimaryProjectile(out var projectile))
		{
			return false;
		}
		projectileData = projectile.GetProjectile();
		return GodotObject.IsInstanceValid(projectileData);
	}

	private bool IsOperational()
	{
		if (base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _initialized && GodotObject.IsInstanceValid(parent) && GodotObject.IsInstanceValid(parent.sprite))
		{
			return GodotObject.IsInstanceValid(parent.transformPoint);
		}
		return false;
	}

	private bool CanHandleDamagePoint()
	{
		if (!IsOperational())
		{
			if (_initialized && GodotObject.IsInstanceValid(parent))
			{
				return parent.nearDie;
			}
			return false;
		}
		return true;
	}

	private bool IsRemoteSyncedClient()
	{
		if (hostAuthoritativeRuntime && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(parent))
		{
			return parent.syncId >= 0;
		}
		return false;
	}

	private int GetEffectiveFireNum()
	{
		return Math.Max(1, fireNum);
	}

	private double GetIdleBoundaryX()
	{
		return _groundRight - (double)_gridSizeX * Math.Max(0.0, idleBoundaryOffsetRatio);
	}
}
