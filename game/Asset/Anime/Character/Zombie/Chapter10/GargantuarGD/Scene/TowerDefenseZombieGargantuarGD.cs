using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieGargantuarGD : TowerDefenseZombieGargantuarBase
{
	private const string ChargeStateId = "zombie.gargantuar_gd.charge";

	private const string MagicClip = "Magic";

	private const string MagicIdleClip = "MagicIdle";

	private const string MagicSmashClip = "MagicSmash";

	private const double ChargeHpRatio = 0.5;

	private const double ChargeDuration = 5.0;

	private const double ChargeRegenPerSecond = 500.0;

	private const double ChargeDamageLimit = 100.0;

	private const double ChargeRegenTickInterval = 1.0;

	private const double ChargeTimerEpsilon = 1E-09;

	private StateHandle _chargeStateHandle;

	private bool _chargeStateConnected;

	private bool _chargeArmed;

	private bool _charging;

	private bool _chargeEnding;

	private double _chargeElapsed;

	private double _chargeRegenAccumulator;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			impThrowFlag = false;
			ConnectChargeState();
		}
	}

	public override void _ExitTree()
	{
		DisconnectChargeState();
		base._ExitTree();
	}

	private void ConnectChargeState()
	{
		if (_chargeStateConnected)
		{
			return;
		}
		IStateMachineController stateMachine = base.StateMachine;
		if (stateMachine != null && stateMachine.IsInitialized)
		{
			_chargeStateHandle = base.StateMachine?.GetStateById("zombie.gargantuar_gd.charge");
			StateHandle chargeStateHandle = _chargeStateHandle;
			if (chargeStateHandle != null && chargeStateHandle.IsValid)
			{
				_chargeStateHandle.Entered += ChargeEntered;
				_chargeStateHandle.Exited += ChargeExited;
				_chargeStateHandle.PhysicsProcessing += ChargeProcessing;
				_chargeStateConnected = true;
			}
		}
	}

	private void DisconnectChargeState()
	{
		if (_chargeStateConnected)
		{
			if (_chargeStateHandle != null)
			{
				_chargeStateHandle.Entered -= ChargeEntered;
				_chargeStateHandle.Exited -= ChargeExited;
				_chargeStateHandle.PhysicsProcessing -= ChargeProcessing;
			}
			_chargeStateHandle = null;
			_chargeStateConnected = false;
		}
	}

	private void UpdateChargeTrigger(double delta)
	{
		if (_charging || die || nearDie || isDestroy || !GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		double num = instance.hitpointsSave * 0.5;
		if (!_chargeArmed)
		{
			if (instance.hitpoints >= num)
			{
				_chargeArmed = true;
			}
		}
		else if (instance.hitpoints < num)
		{
			_chargeArmed = false;
			SendStateEvent("ToCharge");
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		UpdateChargeTrigger(delta);
	}

	public override void WalkProcessing(double delta)
	{
		base.WalkProcessing(delta);
		UpdateChargeTrigger(delta);
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		UpdateChargeTrigger(delta);
	}

	public virtual void ChargeEntered()
	{
		_charging = true;
		_chargeEnding = false;
		_chargeElapsed = 0.0;
		_chargeRegenAccumulator = 0.0;
		base.sprite.SetAnimation("Magic", loop: false, 0.1);
	}

	public virtual void ChargeProcessing(double delta)
	{
		base.sprite.timeScale = timeScale;
		if (_charging && !_chargeEnding && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && GodotObject.IsInstanceValid(instance))
		{
			double num = Mathf.Clamp(delta, 0.0, 5.0 - _chargeElapsed);
			_chargeElapsed += num;
			_chargeRegenAccumulator += num;
			double hitpointsSave = instance.hitpointsSave;
			while (_chargeRegenAccumulator + 1E-09 >= 1.0 && instance.hitpoints < hitpointsSave)
			{
				_chargeRegenAccumulator = Mathf.Max(0.0, _chargeRegenAccumulator - 1.0);
				Health(Mathf.Min(500.0, hitpointsSave - instance.hitpoints));
			}
			if (instance.hitpoints >= hitpointsSave || _chargeElapsed + 1E-09 >= 5.0)
			{
				_chargeEnding = true;
				base.sprite.SetAnimation("MagicSmash", loop: false, 0.1);
			}
		}
	}

	public virtual void ChargeExited()
	{
		_charging = false;
		_chargeEnding = false;
	}

	public override void AnimeCompleted(string clip)
	{
		base.AnimeCompleted(clip);
		if (clip == "Magic" && _charging && !_chargeEnding)
		{
			base.sprite.SetAnimation("MagicIdle", loop: true, 0.1);
		}
		else if (clip == "MagicSmash" && _charging)
		{
			Walk();
		}
	}

	private double LimitChargeHurt(double num)
	{
		if (!_charging)
		{
			return num;
		}
		return Mathf.Min(num, 100.0);
	}

	public override double Hurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, double damageLimit = -1.0)
	{
		return base.Hurt(LimitChargeHurt(num), playSplatAudio, velocity, createDamagePart, damageLimit);
	}

	public override double SkipInvincibleHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		return base.SkipInvincibleHurt(LimitChargeHurt(num), playSplatAudio, velocity, createDamagePart);
	}

	public override double BowlingHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		return base.BowlingHurt(LimitChargeHurt(num), playSplatAudio, velocity, hitShield, createDamagePart);
	}

	public override double SmashHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return base.SmashHurt(LimitChargeHurt(num), playSplatAudio, velocity);
	}

	public override double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return base.ExplodeHurt(LimitChargeHurt(num), damageKind, playSplatAudio, velocity);
	}

	public override double FlagHurt(double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return base.FlagHurt(LimitChargeHurt(num), damageFlags, playSplatAudio, velocity);
	}

	public override double HurtWithAttackConfig(AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (!_charging || !GodotObject.IsInstanceValid(attackConfig))
		{
			return base.HurtWithAttackConfig(attackConfig, playSplatAudio, velocity, createDamagePart);
		}
		double num = attackConfig.num;
		attackConfig.num = Mathf.Min(num, 100.0);
		try
		{
			return base.HurtWithAttackConfig(attackConfig, playSplatAudio, velocity, createDamagePart);
		}
		finally
		{
			attackConfig.num = num;
		}
	}

	public override double ProjectileHurt(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		if (!_charging)
		{
			return base.ProjectileHurt(projectile, projectileConfig, playSplatAudio, velocity, isRange);
		}
		bool flag = GodotObject.IsInstanceValid(projectile);
		bool flag2 = GodotObject.IsInstanceValid(projectileConfig);
		double num = (flag ? projectile.damage : 0.0);
		double num2 = (flag2 ? projectileConfig.baseDamage : 0.0);
		if (flag)
		{
			projectile.damage = Mathf.Min(num, 100.0);
		}
		if (flag2)
		{
			projectileConfig.baseDamage = Mathf.Min(num2, 100.0);
		}
		try
		{
			return base.ProjectileHurt(projectile, projectileConfig, playSplatAudio, velocity, isRange);
		}
		finally
		{
			if (flag && GodotObject.IsInstanceValid(projectile))
			{
				projectile.damage = num;
			}
			if (flag2)
			{
				projectileConfig.baseDamage = num2;
			}
		}
	}

	public override double ProjectileHurt(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		if (!_charging)
		{
			return base.ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange);
		}
		ProjectileHitInfo info2 = info;
		info2.damage = Mathf.Min(info.damage, 100.0);
		return base.ProjectileHurt(in info2, projectileConfig, playSplatAudio, velocity, isRange);
	}

	public override void AnimeEvent(string command, Variant argument)
	{
		if (command != smashAnimeEvent)
		{
			base.AnimeEvent(command, argument);
		}
		else if (_charging || (GodotObject.IsInstanceValid(base.sprite) && base.sprite.clip == "MagicSmash"))
		{
			gargantuarSmashComponent?.SmashAttack(1);
		}
		else
		{
			base.AnimeEvent(command, argument);
		}
	}

	public override Dictionary ExportVariantSave()
	{
		Dictionary dictionary = base.ExportVariantSave();
		dictionary["chargeArmed"] = _chargeArmed;
		return dictionary;
	}

	public override void ImportVariantSave(Dictionary data)
	{
		base.ImportVariantSave(data);
		_chargeArmed = data.GetValueOrDefault("chargeArmed", false).AsBool();
	}
}
