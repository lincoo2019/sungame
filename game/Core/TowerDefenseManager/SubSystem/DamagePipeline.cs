using System;
using Godot;

[GlobalClass]
public partial class DamagePipeline : RefCounted
{
	public double ApplyHurtWithAttackConfig(TowerDefenseCharacter character, AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedHurtWithAttackConfig(character, instance, attackConfig, playSplatAudio, velocity, createDamagePart);
	}

	internal double ApplyPreparedHurtWithAttackConfig(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		double num = instance.HurtWithAttackConfig(attackConfig, playSplatAudio, velocity, createDamagePart);
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num, null);
		return num;
	}

	public double ApplyHurt(TowerDefenseCharacter character, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true, double damageLimit = -1.0)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedHurt(character, instance, num, playSplatAudio, velocity, hitShield, createDamagePart, damageLimit);
	}

	internal double ApplyPreparedHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true, double damageLimit = -1.0)
	{
		double num2 = instance.Hurt(num, playSplatAudio, velocity, hitShield, createDamagePart, damageLimit);
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num2, null);
		return num2;
	}

	private static bool IsRemoteSyncedCharacter(TowerDefenseCharacter character)
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(character))
		{
			return character.syncId >= 0;
		}
		return false;
	}

	internal static bool TryPrepareCharacter(TowerDefenseCharacter character, out TowerDefenseCharacterInstance instance)
	{
		instance = null;
		if (!GodotObject.IsInstanceValid(character) || IsRemoteSyncedCharacter(character))
		{
			return false;
		}
		instance = character.instance;
		return GodotObject.IsInstanceValid(instance);
	}

	internal bool TryPrepareDamage(TowerDefenseCharacter character, out TowerDefenseCharacterInstance instance)
	{
		return TryPrepareCharacter(character, out instance);
	}

	public double ApplyExplodeHurt(TowerDefenseCharacter character, double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedExplodeHurt(character, instance, num, damageKind, playSplatAudio, velocity);
	}

	internal double ApplyPreparedExplodeHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		BattleEventBus instance2 = BattleEventBus.Instance;
		bool num2 = TowerDefensePerfProfiler.DetailedHotPathMetrics && TowerDefensePerfProfiler.Enabled;
		bool flag = instance2?.HasCharacterHurtSubscribers ?? false;
		if (!num2 && !flag)
		{
			return ApplyExplosionState(character, instance, num, damageKind, playSplatAudio, velocity);
		}
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		double num3 = ApplyExplosionState(character, instance, num, damageKind, playSplatAudio, velocity);
		TowerDefensePerfProfiler.End("damage.explode", startTicks, 1);
		instance2?.EmitCharacterHurt(character, (int)num3, null);
		return num3;
	}

	private static double ApplyExplosionState(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind, bool playSplatAudio, Vector2 velocity)
	{
		character.isExplode = true;
		try
		{
			return instance.ExplodeHurt(num, damageKind, playSplatAudio, velocity);
		}
		finally
		{
			character.isExplode = false;
		}
	}

	public double ApplyFlagHurt(TowerDefenseCharacter character, double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, bool isRange = false, int projectileHeight = -1)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedFlagHurt(character, instance, num, damageFlags, playSplatAudio, velocity, createDamagePart, isRange, projectileHeight);
	}

	internal double ApplyPreparedFlagHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, bool isRange = false, int projectileHeight = -1)
	{
		double num2 = instance.FlagHurt(num, damageFlags, playSplatAudio, velocity, createDamagePart, isRange, projectileHeight);
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num2, null);
		return num2;
	}

	private static ProjectileHitInfo CreateProjectileHitInfo(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig)
	{
		ProjectileHitInfo result = new ProjectileHitInfo
		{
			damage = projectileConfig.baseDamage,
			damageFlags = projectileConfig.damageFlags,
			position = default(Vector2),
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = projectileConfig,
			fireCharacter = null,
			camp = TowerDefenseEnum.CHARACTER_CAMP.NOONE,
			collisionFlags = 0,
			gridPos = Vector2I.Zero,
			height = 0.0,
			onSourceDespawn = null
		};
		if (GodotObject.IsInstanceValid(projectile))
		{
			result.damage = projectile.damage;
			result.damageFlags = projectile.damageFlags;
			result.position = projectile.GlobalPosition;
			result.projectileHeight = projectile.projectileHeight;
			result.onSourceDespawn = projectile.Over;
		}
		return result;
	}

	public double ApplyProjectileHurt(TowerDefenseCharacter character, in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedProjectileHurt(character, instance, in info, projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
	}

	internal double ApplyPreparedProjectileHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		BattleEventBus instance2 = BattleEventBus.Instance;
		if ((!TowerDefensePerfProfiler.DetailedHotPathMetrics || !TowerDefensePerfProfiler.Enabled) && instance2 != null && !instance2.HasCharacterHurtSubscribers)
		{
			return instance.ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
		}
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		double num = instance.ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
		TowerDefensePerfProfiler.End("damage.projectile", startTicks, 1);
		TowerDefensePerfProfiler.EndSpikeProbe("damage.projectile", in probe, 1);
		instance2.EmitCharacterHurt(character, (int)num, null);
		return num;
	}

	public double ApplyProjectileHurt(TowerDefenseCharacter character, TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedProjectileHurt(character, instance, projectile, projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
	}

	internal double ApplyPreparedProjectileHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		double num = instance.ProjectileHurt(CreateProjectileHitInfo(projectile, projectileConfig), projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
		TowerDefensePerfProfiler.End("damage.projectile", startTicks, 1);
		TowerDefensePerfProfiler.EndSpikeProbe("damage.projectile", in probe, 1);
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num, projectile);
		return num;
	}

	public double ApplySkipInvincibleHurt(TowerDefenseCharacter character, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedSkipInvincibleHurt(character, instance, num, playSplatAudio, velocity, hitShield, createDamagePart);
	}

	internal double ApplyPreparedSkipInvincibleHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		double num2 = instance.SkipInvincibleHurt(num, playSplatAudio, velocity, hitShield, createDamagePart);
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num2, null);
		return num2;
	}

	public double ApplySmashHurt(TowerDefenseCharacter character, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (!TryPrepareDamage(character, out var instance))
		{
			return 0.0;
		}
		return ApplyPreparedSmashHurt(character, instance, num, playSplatAudio, velocity);
	}

	internal double ApplyPreparedSmashHurt(TowerDefenseCharacter character, TowerDefenseCharacterInstance instance, double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (character is TowerDefensePlant)
		{
			double num2 = 100000.0;
			if (instance.smashHurt != -1.0)
			{
				num2 = Math.Min(instance.smashHurt, num2);
			}
			if (GodotObject.IsInstanceValid(character.cell))
			{
				bool num3 = character.cell.smashAbsorbedFrame == Engine.GetPhysicsFrames();
				TowerDefenseCharacter slot = character.cell.GetSlot(character);
				if ((num3 || (GodotObject.IsInstanceValid(slot) && (slot.instance.invincibleSmash || slot.instance.hitpoints - num2 > 0.0))) && instance.hitpoints - num2 <= 0.0)
				{
					return num;
				}
			}
		}
		character.isSmash = true;
		double num4;
		try
		{
			num4 = instance.SmashHurtApply(num, playSplatAudio, velocity);
		}
		finally
		{
			character.isSmash = false;
		}
		BattleEventBus.Instance.EmitCharacterHurt(character, (int)num4, null);
		return num4;
	}
}
