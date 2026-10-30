using Godot;

public sealed class HurtComponent : CharacterComponentRuntime
{
	private const string DefaultHealthEffectPath = "uid://b8c40r4tk45sf";

	private static PackedScene _healthCache;

	private DamagePipeline _pipeline;

	private TowerDefenseCharacter _damageOwner;

	private TowerDefenseCharacterInstance _damageInstance;

	private bool _damageRuntimeReady;

	private HurtComponentDefinition Definition => base.ComponentDefinition as HurtComponentDefinition;

	public PackedScene healthEffectScene { get; set; }

	public int maxHealthEffectCount { get; set; } = 100;

	public StringName healthEffectAnimation { get; set; } = "Idle";

	public bool flashOnDamage { get; set; } = true;

	public bool flashOnHeal { get; set; } = true;

	public bool markHealthBarDirty { get; set; } = true;

	protected override void OnBound()
	{
		HurtComponentDefinition definition = Definition;
		healthEffectScene = definition?.healthEffectScene ?? _healthCache ?? (_healthCache = GD.Load<PackedScene>("uid://b8c40r4tk45sf"));
		maxHealthEffectCount = definition?.maxHealthEffectCount ?? 100;
		healthEffectAnimation = definition?.healthEffectAnimation ?? new StringName("Idle");
		flashOnDamage = definition?.flashOnDamage ?? true;
		flashOnHeal = definition?.flashOnHeal ?? true;
		markHealthBarDirty = definition?.markHealthBarDirty ?? true;
		_pipeline = TowerDefenseManager.Instance?.damagePipeline;
	}

	protected override void OnActivated()
	{
		RefreshDamageRuntimeCache();
	}

	protected override void OnAliveChanged(bool alive)
	{
		_damageRuntimeReady = alive && _damageOwner != null && _damageInstance != null && _pipeline != null;
	}

	protected override void OnDetaching(ComponentDetachReason reason)
	{
		_damageRuntimeReady = false;
		_damageOwner = null;
		_damageInstance = null;
		_pipeline = null;
	}

	public double HurtWithAttackConfig(AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedHurtWithAttackConfig(owner, instance, attackConfig, playSplatAudio, velocity, createDamagePart);
	}

	public double Hurt(float num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, double damageLimit = -1.0)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedHurt(owner, instance, num, playSplatAudio, velocity, hitShield: true, createDamagePart, damageLimit);
	}

	public double SkipInvincibleHurt(float num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedSkipInvincibleHurt(owner, instance, num, playSplatAudio, velocity, hitShield: true, createDamagePart);
	}

	public void Health(float num)
	{
		if (TryPrepareCharacter(out var owner, flashOnHeal))
		{
			owner.instance.Health(num);
			CreateHealthEffect(owner);
		}
	}

	public double BowlingHurt(float num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedSkipInvincibleHurt(owner, instance, num, playSplatAudio, velocity, hitShield, createDamagePart);
	}

	public double SmashHurt(float num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedSmashHurt(owner, instance, num, playSplatAudio, velocity);
	}

	public double ExplodeHurt(float num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedExplodeHurt(owner, instance, num, damageKind, playSplatAudio, velocity);
	}

	public double FlagHurt(float num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedFlagHurt(owner, instance, num, damageFlags, playSplatAudio, velocity);
	}

	public double ProjectileHurt(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedProjectileHurt(owner, instance, projectile, projectileConfig, playSplatAudio, velocity, isRange);
	}

	public double ProjectileHurt(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false)
	{
		if (!TryPrepareDamage(out var pipeline, out var owner, out var instance))
		{
			return 0.0;
		}
		return pipeline.ApplyPreparedProjectileHurt(owner, instance, in info, projectileConfig, playSplatAudio, velocity, isRange);
	}

	private bool TryPrepareDamage(out DamagePipeline pipeline, out TowerDefenseCharacter owner, out TowerDefenseCharacterInstance instance)
	{
		pipeline = _pipeline;
		owner = _damageOwner;
		instance = _damageInstance;
		if (!_damageRuntimeReady)
		{
			RefreshDamageRuntimeCache();
			pipeline = _pipeline;
			owner = _damageOwner;
			instance = _damageInstance;
			if (!_damageRuntimeReady)
			{
				return false;
			}
		}
		if (owner.syncId >= 0 && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return false;
		}
		ApplyFeedback(owner, flashOnDamage, damageFlash: true);
		return true;
	}

	private void RefreshDamageRuntimeCache()
	{
		if (_pipeline == null)
		{
			_pipeline = TowerDefenseManager.Instance?.damagePipeline;
		}
		_damageOwner = base.Owner;
		_damageInstance = _damageOwner?.instance;
		_damageRuntimeReady = base.Alive && base.Lifecycle == ComponentRuntimeLifecycle.Active && _pipeline != null && _damageOwner != null && _damageInstance != null;
	}

	private bool TryPrepareCharacter(out TowerDefenseCharacter owner, bool flash)
	{
		owner = _damageOwner;
		TowerDefenseCharacter owner2 = base.Owner;
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = owner2?.instance;
		if (!_damageRuntimeReady || owner != owner2 || _damageInstance != towerDefenseCharacterInstance)
		{
			RefreshDamageRuntimeCache();
			owner = _damageOwner;
		}
		if (!base.Alive || base.Lifecycle != ComponentRuntimeLifecycle.Active || !GodotObject.IsInstanceValid(owner) || !GodotObject.IsInstanceValid(_damageInstance))
		{
			return false;
		}
		if (owner.syncId >= 0 && Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return false;
		}
		ApplyFeedback(owner, flash, damageFlash: false);
		return true;
	}

	private void ApplyFeedback(TowerDefenseCharacter owner, bool flash, bool damageFlash)
	{
		if (flash)
		{
			if (damageFlash)
			{
				owner.White(0.5, 0.0, 0.2);
			}
			else
			{
				owner.Bright();
			}
		}
		if (markHealthBarDirty)
		{
			ShowHealthComponent showHealthComponent = owner.showHealthComponent;
			if (showHealthComponent != null && !showHealthComponent.IsReleased)
			{
				owner.showHealthComponent.MarkDirty();
			}
		}
	}

	private void CreateHealthEffect(TowerDefenseCharacter owner)
	{
		PackedScene packedScene = healthEffectScene;
		StringName stringName = healthEffectAnimation;
		if (GodotObject.IsInstanceValid(packedScene) && !stringName.IsEmpty && CanCreateHealthEffect() && GodotObject.IsInstanceValid(TowerDefenseGroundItemBase.characterNode))
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(packedScene, owner.gridPos, stringName.ToString());
			if (GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce))
			{
				TowerDefenseGroundItemBase.characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
				towerDefenseEffectSpriteOnce.gridPos = owner.gridPos;
				towerDefenseEffectSpriteOnce.GlobalPosition = GetHealthEffectPosition(owner);
			}
		}
	}

	private bool CanCreateHealthEffect()
	{
		if (maxHealthEffectCount < 0)
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return TowerDefenseManager.Instance.GetEffectCount() < maxHealthEffectCount;
		}
		return false;
	}

	private static Vector2 GetHealthEffectPosition(TowerDefenseCharacter owner)
	{
		if (GodotObject.IsInstanceValid(owner.shadowSprite))
		{
			ShadowComponent shadowComponent = owner.shadowComponent;
			if (shadowComponent != null && !shadowComponent.IsReleased)
			{
				return new Vector2(owner.GetLogicalGlobalPosition(owner.shadowSprite).X, owner.shadowComponent.GetShadowPosition().Y);
			}
		}
		return owner.GetLogicalGlobalPosition();
	}
}
