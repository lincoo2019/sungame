using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieMagicYeti : TowerDefenseZombie
{
	private const string RageStateId = "zombie.magic_yeti.rage";

	private const string RageStateEvent = "ToRage";

	private const string AngryClip = "Angry";

	private const string IdleAngryClip = "IdleAngry";

	private const string EatAngryClip = "EatAngry";

	private const string DeathAngryClip = "DeathAngry";

	private const string AngryWalkClip = "WalkAngry";

	private static readonly StringName[] LockLayers = new StringName[9] { "lock1", "lock2_1", "lock2_2", "lock3_1", "lock3_2", "lock4_1", "lock4_2", "lock5_1", "lock5_2" };

	private const double RageTriggerTime = 30.0;

	private const double RageTriggerHitpoints = 1000.0;

	private const double RageBonusHitpoints = 2000.0;

	private const double NormalSpeedScale = 1.5;

	private const double RageSpeedScale = 3.0;

	private const double RageScaleMultiplier = 1.25;

	private const string SmashAttackType = "Smash";

	private const double ChainDuration = 10.0;

	private const double ChainSpeedScale = 0.5;

	private const double RageAnimeFallbackTime = 3.0;

	private StateHandle _rageStateHandle;

	private bool _stateSignalsConnected;

	private bool _rageEntered;

	private bool _rageRequested;

	private bool _rageStateActive;

	private bool _rageSpeedApplied;

	private bool _rageHealApplied;

	private bool _keepAliveBeforeRage;

	private double _rageElapsed;

	private double _aliveTime;

	private double _chainTimer;

	private bool _chainActive;

	private double _appliedSpeedScale = 1.0;

	public override void _Ready()
	{
		base._Ready();
		SyncLockLayers();
		ApplySpeedScale();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			ConnectStateSignals();
			if (GodotObject.IsInstanceValid(base.sprite))
			{
				base.sprite.OnAnimeStarted += OnSpriteAnimeStarted;
			}
			SyncLockLayers();
		}
	}

	public override void _ExitTree()
	{
		DisconnectStateSignals();
		if (GodotObject.IsInstanceValid(base.sprite))
		{
			base.sprite.OnAnimeStarted -= OnSpriteAnimeStarted;
		}
		base._ExitTree();
	}

	private void ConnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_rageStateHandle = base.StateMachine.GetStateById("zombie.magic_yeti.rage");
			StateHandle rageStateHandle = _rageStateHandle;
			if (rageStateHandle != null && rageStateHandle.IsValid)
			{
				_rageStateHandle.Entered += RageEntered;
				_rageStateHandle.Exited += RageExited;
				_rageStateHandle.PhysicsProcessing += RageProcessing;
				_stateSignalsConnected = true;
			}
		}
	}

	private void DisconnectStateSignals()
	{
		if (_stateSignalsConnected)
		{
			if (_rageStateHandle != null)
			{
				_rageStateHandle.Entered -= RageEntered;
				_rageStateHandle.Exited -= RageExited;
				_rageStateHandle.PhysicsProcessing -= RageProcessing;
			}
			_rageStateHandle = null;
			_stateSignalsConnected = false;
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		UpdatePassive(delta);
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		UpdatePassive(delta);
	}

	public override void AttackProcessing(double delta)
	{
		if (!_rageEntered)
		{
			base.AttackProcessing(delta);
			UpdatePassive(delta);
		}
		else if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			if (!attackComponent.CanAttack())
			{
				Walk();
			}
			base.sprite.timeScale = timeScale * 2.0;
			UpdatePassive(delta);
		}
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		HideChain();
	}

	private void UpdatePassive(double delta)
	{
		if (!_stateSignalsConnected)
		{
			ConnectStateSignals();
		}
		_aliveTime += delta;
		HideChainIfNeeded(delta);
		UpdateRageTrigger();
		SyncLockLayers();
	}

	private void UpdateRageTrigger()
	{
		if (!_rageEntered && !_rageRequested && !die && !nearDie && !isDestroy && GodotObject.IsInstanceValid(instance) && (!(_aliveTime < 30.0) || !(instance.hitpoints > 1000.0)))
		{
			_rageRequested = SendStateEvent("ToRage");
		}
	}

	public virtual void RageEntered()
	{
		_rageEntered = true;
		_rageStateActive = true;
		_rageElapsed = 0.0;
		if (GodotObject.IsInstanceValid(instance))
		{
			_keepAliveBeforeRage = instance.keepAlive;
			instance.keepAlive = true;
			if (instance.hitpoints < 1.0)
			{
				instance.hitpoints = 1.0;
			}
		}
		ApplyRageStats();
		PlayRageScaleTween();
		base.sprite.SetAnimation("Angry", loop: false, 0.1);
	}

	public virtual void RageProcessing(double delta)
	{
		HideChainIfNeeded(delta);
		SyncLockLayers();
		if (GodotObject.IsInstanceValid(instance) && instance.hitpoints < 1.0)
		{
			instance.hitpoints = 1.0;
		}
		base.sprite.timeScale = timeScale;
		_rageElapsed += delta;
		if (_rageElapsed < 3.0)
		{
			return;
		}
		ApplyRageSpeedUp();
		Walk();
		if (_rageStateActive && _rageElapsed >= 6.0)
		{
			_rageStateActive = false;
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.keepAlive = _keepAliveBeforeRage;
			}
		}
	}

	public virtual void RageExited()
	{
		_rageStateActive = false;
		_rageElapsed = 0.0;
		ApplyRageSpeedUp();
		if (GodotObject.IsInstanceValid(instance) && !instance.die)
		{
			instance.keepAlive = _keepAliveBeforeRage;
			if (instance.hitpoints < 1.0)
			{
				instance.hitpoints = 1.0;
			}
		}
	}

	private void ApplyRageStats()
	{
		if (GodotObject.IsInstanceValid(instance))
		{
			if (!_rageHealApplied)
			{
				_rageHealApplied = true;
				instance.hitpointsSave += 2000.0;
				double num = instance.hitpointsSave - instance.hitpoints;
				if (num > 0.0)
				{
					Health(num);
				}
			}
			instance.unUseBuffFlags |= 1;
			showHealthComponent?.MarkDirty();
		}
		buff?.DeleteBuff("IceSpeedDown");
		ApplyAngryAnimationSet();
		ApplyAngryAttackProfile();
		ApplySpeedScale();
	}

	private void ApplyAngryAnimationSet()
	{
		base.walkAnimeClip = "WalkAngry";
		base.swimAnimeClip = "WalkAngry";
		idleAnimeClip = "IdleAngry";
		base.attackAnimeClip = "EatAngry";
		base.attackWaterAnimeClip = "EatAngry";
		base.dieAnimeClip = "DeathAngry";
		base.dieWaterAnimeClip = "DeathAngry";
	}

	private void ApplyAngryAttackProfile()
	{
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			attackComponent.attackType = "Smash";
		}
	}

	private void ApplySpeedScale()
	{
		double num = 1.5;
		if (_chainActive)
		{
			num = 0.5;
		}
		else if (_rageEntered && _rageSpeedApplied)
		{
			num = 3.0;
		}
		if (!Mathf.IsEqualApprox(num, _appliedSpeedScale))
		{
			double num2 = num / _appliedSpeedScale;
			_appliedSpeedScale = num;
			timeScaleInit *= num2;
		}
	}

	private void ApplyRageSpeedUp()
	{
		if (!_rageSpeedApplied)
		{
			_rageSpeedApplied = true;
			ApplySpeedScale();
		}
	}

	private void PlayRageScaleTween()
	{
		if (GodotObject.IsInstanceValid(transformPoint))
		{
			Tween tween = CreateTween();
			tween.SetEase(Tween.EaseType.Out);
			tween.SetTrans(Tween.TransitionType.Quint);
			tween.TweenProperty(transformPoint, "scale", transformPoint.Scale * 1.25f, 0.5);
		}
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Angry" && _rageStateActive)
		{
			ApplyRageSpeedUp();
			Walk();
		}
		else if (clip == "EatAngry" && _rageEntered && !_rageStateActive)
		{
			TriggerAngrySmash();
		}
	}

	private void TriggerAngrySmash()
	{
		if (!die && !nearDie && !isDestroy && startAttack)
		{
			AttackComponent obj = attackComponent;
			if (obj != null && !obj.IsReleased && attackComponent.CanAttack() && GodotObject.IsInstanceValid(base.sprite) && !base.sprite.pause && !(base.sprite.timeScale <= 0.0))
			{
				attackComponent.AttackExecute(((TowerDefenseZombieConfig)base.config).smashAttack);
				PlayEatAudio();
				PlayPlantDefeatAudioIfAny();
			}
		}
	}

	private void PlayEatAudio()
	{
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased)
		{
			string eatAudio = attackComponent.eatAudio;
			if (!string.IsNullOrEmpty(eatAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(eatAudio);
			}
		}
	}

	private void PlayPlantDefeatAudioIfAny()
	{
		AttackComponent obj = attackComponent;
		if (obj != null && !obj.IsReleased && attackComponent.playPlantDefeatAudio && attackComponent.target is TowerDefensePlant { instance: not null } towerDefensePlant && towerDefensePlant.instance.die)
		{
			string plantDefeatAudio = attackComponent.plantDefeatAudio;
			if (!string.IsNullOrEmpty(plantDefeatAudio) && GodotObject.IsInstanceValid(AudioManager.Instance))
			{
				AudioManager.Instance.AudioPlay(plantDefeatAudio);
			}
		}
	}

	public override double ProjectileHurt(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		bool magic = (GodotObject.IsInstanceValid(projectile) ? IsMagicFlags(projectile.damageFlags) : IsMagicProjectile(projectileConfig));
		double chainTriggerHitpoints = GetChainTriggerHitpoints(magic);
		double result = base.ProjectileHurt(projectile, projectileConfig, playSplatAudio, velocity, isRange);
		TriggerChainAfterDamage(chainTriggerHitpoints);
		return result;
	}

	public override double ProjectileHurt(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		bool magic = ((info.useRuntimeOverrides || info.onSourceDespawn != null) ? IsMagicFlags(info.damageFlags) : IsMagicProjectile(projectileConfig));
		double chainTriggerHitpoints = GetChainTriggerHitpoints(magic);
		double result = base.ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange);
		TriggerChainAfterDamage(chainTriggerHitpoints);
		return result;
	}

	public override double HurtWithAttackConfig(AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		double chainTriggerHitpoints = GetChainTriggerHitpoints(GodotObject.IsInstanceValid(attackConfig) && IsMagicFlags(attackConfig.damageFlags));
		double result = base.HurtWithAttackConfig(attackConfig, playSplatAudio, velocity, createDamagePart);
		TriggerChainAfterDamage(chainTriggerHitpoints);
		return result;
	}

	public override double FlagHurt(double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		double chainTriggerHitpoints = GetChainTriggerHitpoints(IsMagicFlags(damageFlags));
		double result = base.FlagHurt(num, damageFlags, playSplatAudio, velocity);
		TriggerChainAfterDamage(chainTriggerHitpoints);
		return result;
	}

	private double GetChainTriggerHitpoints(bool magic)
	{
		if (!magic || !_rageEntered || die || nearDie || isDestroy || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || !GodotObject.IsInstanceValid(instance))
		{
			return -1.0;
		}
		return instance.hitpoints;
	}

	private void TriggerChainAfterDamage(double hitpointsBeforeHit)
	{
		if (hitpointsBeforeHit > 0.0 && GodotObject.IsInstanceValid(instance) && instance.hitpoints < hitpointsBeforeHit && !instance.die && !die && !nearDie && !isDestroy)
		{
			TriggerChain();
		}
	}

	private static bool IsMagicFlags(int damageFlags)
	{
		return (damageFlags & 0x40) != 0;
	}

	private static bool IsMagicProjectile(TowerDefenseProjectileConfig projectileConfig)
	{
		if (GodotObject.IsInstanceValid(projectileConfig))
		{
			return IsMagicFlags(projectileConfig.damageFlags);
		}
		return false;
	}

	private void TriggerChain()
	{
		if (_rageEntered)
		{
			if (!_chainActive)
			{
				_chainActive = true;
				ApplySpeedScale();
			}
			_chainTimer = 10.0;
			SyncLockLayers();
		}
	}

	private void HideChainIfNeeded(double delta)
	{
		if (_chainActive && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			_chainTimer -= delta;
			if (_chainTimer <= 0.0)
			{
				HideChain();
			}
		}
	}

	private void HideChain()
	{
		if (!_chainActive)
		{
			SyncLockLayers();
			return;
		}
		_chainActive = false;
		_chainTimer = 0.0;
		ApplySpeedScale();
		SyncLockLayers();
	}

	private void SyncLockLayers()
	{
		if (!GodotObject.IsInstanceValid(base.sprite))
		{
			return;
		}
		bool flag = _rageEntered && _chainActive && _chainTimer > 0.0 && !die && !nearDie && !isDestroy;
		StringName[] lockLayers = LockLayers;
		foreach (StringName layerName in lockLayers)
		{
			if (base.sprite.GetFliter(layerName) != flag)
			{
				base.sprite.SetFliter(layerName, flag);
			}
		}
	}

	private void OnSpriteAnimeStarted(string clip)
	{
		SyncLockLayers();
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["rageEntered"] = _rageEntered;
		dictionary["rageSpeedApplied"] = _rageSpeedApplied;
		dictionary["rageHealApplied"] = _rageHealApplied;
		dictionary["appliedSpeedScale"] = _appliedSpeedScale;
		dictionary["aliveTime"] = _aliveTime;
		dictionary["chainActive"] = _chainActive;
		dictionary["chainTimer"] = _chainTimer;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_aliveTime = data.GetValueOrDefault("aliveTime", 0.0).AsDouble();
		_rageHealApplied = data.GetValueOrDefault("rageHealApplied", false).AsBool();
		_rageEntered = data.GetValueOrDefault("rageEntered", false).AsBool();
		_rageSpeedApplied = data.GetValueOrDefault("rageSpeedApplied", _rageEntered).AsBool();
		_rageRequested = _rageEntered;
		_chainTimer = Mathf.Max(0.0, data.GetValueOrDefault("chainTimer", 0.0).AsDouble());
		_chainActive = _rageEntered && _chainTimer > 0.0 && data.GetValueOrDefault("chainActive", false).AsBool();
		if (!_chainActive)
		{
			_chainTimer = 0.0;
		}
		if (_rageEntered)
		{
			ApplyAngryAnimationSet();
			ApplyAngryAttackProfile();
		}
		_appliedSpeedScale = data.GetValueOrDefault("appliedSpeedScale", _chainActive ? 0.5 : (_rageSpeedApplied ? 3.0 : 1.5)).AsDouble();
		ApplySpeedScale();
		SyncLockLayers();
	}
}
