using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseCharacterInstance : Resource
{
	private readonly struct ArmorLayerResult(double remainingDamage, bool passedToBody)
	{
		public readonly double RemainingDamage = remainingDamage;

		public readonly bool PassedToBody = passedToBody;
	}

	public const double MagicDamageScaleAgainstNonMagic = 1.5;

	public TowerDefenseCharacter character;

	[Export(PropertyHint.None, "")]
	public TowerDefenseCharacterConfig config;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.ZOMBIE_PHYSIQUE zombiePhysique = TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL;

	[Export(PropertyHint.None, "")]
	public double explosionHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public double smashHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public double dragHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public double spikeHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public double biteHurt = -1.0;

	public double explosionHurtSave = -1.0;

	[Export(PropertyHint.None, "")]
	public double hitpointsSave = 200.0;

	[Export(PropertyHint.None, "")]
	public double hitpoints = 200.0;

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_HEIGHT height = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;

	[Export(PropertyHint.None, "")]
	public int collisionFlags = 1;

	private int _maskFlags = 1;

	[Export(PropertyHint.None, "")]
	public int unUseBuffFlags;

	[Export(PropertyHint.None, "")]
	public int physiqueTypeFlags;

	[Export(PropertyHint.None, "")]
	public int elementFlags;

	[Export(PropertyHint.None, "")]
	public double hitpointsNearDeath;

	[Export(PropertyHint.None, "")]
	public double hitpointsBase = 300.0;

	[Export(PropertyHint.None, "")]
	public string impactAudio = "";

	[Export(PropertyHint.None, "")]
	public PackedScene ashScene;

	[Export(PropertyHint.None, "")]
	public CharacterArmorData armorData;

	[Export(PropertyHint.None, "")]
	public CharacterCustomData customData;

	[Export(PropertyHint.None, "")]
	public bool isChests;

	[Export(PropertyHint.None, "")]
	public double dealHurtScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double dealHurtReduce;

	public bool skipDealHurtReduce;

	[Export(PropertyHint.None, "")]
	public CharacterDamagePointData damagePointData;

	[Export(PropertyHint.None, "")]
	public Array<Dictionary> damagePoints = new Array<Dictionary>();

	[Export(PropertyHint.None, "")]
	public int damagePointIndex;

	private double[] _damagePointPercentages = System.Array.Empty<double>();

	private string[] _damagePointNames = System.Array.Empty<string>();

	private string[] _damagePointAudios = System.Array.Empty<string>();

	private bool _networkExplodeDestroyDeferred;

	[Export(PropertyHint.None, "")]
	public bool canCollection = true;

	[Export(PropertyHint.None, "")]
	public bool canBeCollection = true;

	[Export(PropertyHint.None, "")]
	public bool keepArmor;

	[Export(PropertyHint.None, "")]
	public bool keepAlive;

	public double damageHitpointsFloor;

	[Export(PropertyHint.None, "")]
	public bool die;

	[Export(PropertyHint.None, "")]
	public bool nearDie;

	[Export(PropertyHint.None, "")]
	public bool invincible;

	[Export(PropertyHint.None, "")]
	public bool invincibleHurt;

	[Export(PropertyHint.None, "")]
	public bool invincibleSmash;

	[Export(PropertyHint.None, "")]
	public bool sleep;

	[Export(PropertyHint.None, "")]
	public bool wakeUp;

	[Export(PropertyHint.None, "")]
	public bool hypnoses;

	[Export(PropertyHint.None, "")]
	public bool hologram;

	[Export(PropertyHint.None, "")]
	public double hitpointScaleSave = 1.0;

	private double _hitpointScale = 1.0;

	[Export(PropertyHint.None, "")]
	public int armorOverrideUnUseBuffFlagSave;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseArmorInstance> armorList = new Array<TowerDefenseArmorInstance>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseArmorInstance> armorShield = new Array<TowerDefenseArmorInstance>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseArmorInstance> armorHelm = new Array<TowerDefenseArmorInstance>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseArmorInstance> armorBody = new Array<TowerDefenseArmorInstance>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseArmorInstance> armorHeadCover = new Array<TowerDefenseArmorInstance>();

	private List<TowerDefenseArmorInstance> _armorRuntime;

	private List<TowerDefenseArmorInstance> _armorShieldRuntime;

	private List<TowerDefenseArmorInstance> _armorHelmRuntime;

	private List<TowerDefenseArmorInstance> _armorBodyRuntime;

	private List<TowerDefenseArmorInstance> _armorHeadCoverRuntime;

	public bool IsMagicPhysique => (physiqueTypeFlags & 0x1000) != 0;

	[Export(PropertyHint.None, "")]
	public int maskFlags
	{
		get
		{
			return _maskFlags;
		}
		set
		{
			if (_maskFlags != value)
			{
				_maskFlags = value;
				if (GodotObject.IsInstanceValid(character))
				{
					character.targetRegistrationComponent?.NotifyTargetStateChanged();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double hitpointScale
	{
		get
		{
			return _hitpointScale;
		}
		set
		{
			if (_hitpointScale == value)
			{
				return;
			}
			_hitpointScale = value;
			hitpointsSave *= value / hitpointScaleSave;
			hitpoints *= value / hitpointScaleSave;
			foreach (TowerDefenseArmorInstance armor in armorList)
			{
				if (!armor.isRemove)
				{
					armor.hitpointScale = _hitpointScale;
				}
			}
			hitpointScaleSave = _hitpointScale;
			TowerDefenseCharacter towerDefenseCharacter = character;
			if (towerDefenseCharacter != null && towerDefenseCharacter.showHealthComponent?.IsReleased == false)
			{
				character.showHealthComponent.MarkDirty();
			}
		}
	}

	internal int ArmorCount => _armorRuntime?.Count ?? 0;

	internal bool HasAnyArmor => ArmorCount != 0;

	internal bool HasShieldArmor => (_armorShieldRuntime?.Count ?? 0) != 0;

	internal bool HasHelmetArmor => (_armorHelmRuntime?.Count ?? 0) != 0;

	internal bool HasBodyArmor => (_armorBodyRuntime?.Count ?? 0) != 0;

	internal bool HasHeadCoverArmor => (_armorHeadCoverRuntime?.Count ?? 0) != 0;

	public event Action<string> damagePointReach;

	public event Action<string, int> armorDamagePointReach;

	public event Action hitpointsNearDie;

	public event Action hitpointsEmpty;

	public event Action<string> armorHitpointsEmpty;

	public static bool IsMagicDamage(int damageFlags)
	{
		return (damageFlags & 0x40) != 0;
	}

	internal bool ProjectileEffectsBlockedByArmor(int damageFlags, int fireMethodFlags)
	{
		if ((damageFlags & 0x10) != 0)
		{
			return false;
		}
		if ((fireMethodFlags & 0x40) == 0 && (damageFlags & 1) != 0 && HasBlockingProjectileArmor(_armorShieldRuntime))
		{
			return true;
		}
		if ((damageFlags & 8) != 0 && HasBlockingProjectileArmor(_armorHeadCoverRuntime))
		{
			return true;
		}
		if ((damageFlags & 2) != 0 && (HasBlockingProjectileArmor(_armorHelmRuntime) || HasBlockingProjectileArmor(_armorBodyRuntime)))
		{
			return true;
		}
		return false;
	}

	private static bool HasBlockingProjectileArmor(List<TowerDefenseArmorInstance> armors)
	{
		int num = armors?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armors[i];
			if (towerDefenseArmorInstance != null && !towerDefenseArmorInstance.isRemove && (towerDefenseArmorInstance.armorMethodFlags & 0x2000) != 0)
			{
				return true;
			}
		}
		return false;
	}

	internal double GetProjectileDamageableDurability(int damageFlags)
	{
		double num = 0.0;
		if ((damageFlags & 8) != 0)
		{
			num += SumActiveArmorDurability(_armorHeadCoverRuntime);
		}
		if ((damageFlags & 1) != 0)
		{
			num += SumActiveArmorDurability(_armorShieldRuntime);
		}
		if ((damageFlags & 2) != 0)
		{
			num += SumActiveArmorDurability(_armorHelmRuntime);
			num += SumActiveArmorDurability(_armorBodyRuntime);
			num += Math.Max(0.0, hitpoints);
		}
		return num;
	}

	private static double SumActiveArmorDurability(List<TowerDefenseArmorInstance> armors)
	{
		double num = 0.0;
		int num2 = armors?.Count ?? 0;
		for (int i = 0; i < num2; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armors[i];
			if (towerDefenseArmorInstance != null && !towerDefenseArmorInstance.isRemove)
			{
				num += Math.Max(0.0, towerDefenseArmorInstance.hitPoints);
			}
		}
		return num;
	}

	public void EmitHitpointsNearDie()
	{
		this.hitpointsNearDie?.Invoke();
	}

	public void EmitHitpointsEmpty()
	{
		this.hitpointsEmpty?.Invoke();
	}

	public void ClearHitpointsEmptyListeners()
	{
		this.hitpointsEmpty = null;
	}

	public void _Init(TowerDefenseCharacter _character, TowerDefenseCharacterConfig _config)
	{
		character = _character;
		config = _config;
		RefreshArmorRuntimeIndex();
		explosionHurt = config.explosionHurt;
		explosionHurtSave = config.explosionHurt;
		smashHurt = config.smashHurt;
		dragHurt = config.dragHurt;
		spikeHurt = config.spikeHurt;
		biteHurt = config.biteHurt;
		hitpointsNearDeath = config.hitpointsNearDeath;
		hitpointsBase = config.hitpoints;
		hitpoints = hitpointsBase + hitpointsNearDeath;
		hitpointsSave = hitpoints;
		height = config.height;
		ashScene = config.ashScene;
		armorData = config.armorData;
		customData = config.customData;
		collisionFlags = config.collisionFlags;
		maskFlags = config.maskFlags;
		unUseBuffFlags = config.unUseBuffFlags;
		physiqueTypeFlags = config.physiqueTypeFlags;
		elementFlags = config.elementFlags;
		damagePointData = config.damagePointData;
		_damagePointPercentages = System.Array.Empty<double>();
		_damagePointNames = System.Array.Empty<string>();
		_damagePointAudios = System.Array.Empty<string>();
		if (damagePointData != null)
		{
			Array<CharacterDamagePointConfig> damagePointList = damagePointData.damagePointList;
			int count = damagePointList.Count;
			_damagePointPercentages = new double[count];
			_damagePointNames = new string[count];
			_damagePointAudios = new string[count];
			for (int i = 0; i < count; i++)
			{
				CharacterDamagePointConfig characterDamagePointConfig = damagePointList[i];
				_damagePointPercentages[i] = characterDamagePointConfig.damagePersontage;
				_damagePointNames[i] = characterDamagePointConfig.damagePointName;
				_damagePointAudios[i] = characterDamagePointConfig.damageAudio;
				Dictionary item = new Dictionary
				{
					["Persontage"] = characterDamagePointConfig.damagePersontage,
					["Name"] = characterDamagePointConfig.damagePointName,
					["DamageAudio"] = characterDamagePointConfig.damageAudio
				};
				damagePoints.Add(item);
			}
		}
		if (_config is TowerDefenseZombieConfig towerDefenseZombieConfig)
		{
			zombiePhysique = towerDefenseZombieConfig.physique;
			impactAudio = towerDefenseZombieConfig.impactAudio;
		}
		if (_config is TowerDefenseGravestoneConfig towerDefenseGravestoneConfig)
		{
			isChests = towerDefenseGravestoneConfig.isChests;
		}
		foreach (string item2 in character.currentArmor)
		{
			ArmorAdd(item2);
		}
	}

	private ArmorLayerResult ProcessArmorLayer(List<TowerDefenseArmorInstance> armorList, double num, bool playSplatAudio, Vector2 velocity, bool createDamagePart, bool isRange, bool skipBodyInvincible, int projectileHeight = -1)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		int num2 = 0;
		bool passedToBody = false;
		int num3 = 0;
		while (num3 < armorList.Count)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armorList[num3];
			num2++;
			if (towerDefenseArmorInstance == null || towerDefenseArmorInstance.isRemove)
			{
				num3++;
				continue;
			}
			if (projectileHeight >= 0 && projectileHeight > (int)towerDefenseArmorInstance.typeData.height)
			{
				num3++;
				continue;
			}
			if ((towerDefenseArmorInstance.armorMethodFlags & 0x20) != 0)
			{
				num = 0.0;
			}
			if ((towerDefenseArmorInstance.armorMethodFlags & 0x100) != 0)
			{
				if (skipBodyInvincible)
				{
					SkipInvincibleDealHurt(num, playSplatAudio, velocity, createDamagePart);
				}
				else
				{
					DealHurt(num, playSplatAudio, velocity, createDamagePart);
				}
				passedToBody = true;
			}
			if (isRange)
			{
				towerDefenseArmorInstance.Hurt(num, playSplatAudio, velocity, createDamagePart);
			}
			else
			{
				num = towerDefenseArmorInstance.Hurt(num, playSplatAudio, velocity, createDamagePart);
			}
			if (num3 < armorList.Count && armorList[num3] == towerDefenseArmorInstance)
			{
				num3++;
			}
			if (num <= 0.0)
			{
				break;
			}
		}
		TowerDefensePerfProfiler.End("damage.armor", startTicks, num2);
		return new ArmorLayerResult(num, passedToBody);
	}

	public double HurtWithAttackConfig(AttackConfig attackConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (HasAnyArmor)
		{
			return FlagHurt(attackConfig.num * attackConfig.armorAttackScale, attackConfig.damageFlags, playSplatAudio, velocity, createDamagePart);
		}
		return FlagHurt(attackConfig.num * attackConfig.attackScale, attackConfig.damageFlags, playSplatAudio, velocity, createDamagePart);
	}

	public double ProjectileHurt(TowerDefenseProjectile projectile, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		ProjectileHitInfo info = new ProjectileHitInfo
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
			info.damage = projectile.damage;
			info.damageFlags = projectile.damageFlags;
			info.useRuntimeOverrides = true;
			info.fireMethodFlags = projectile.fireMethodFlags;
			info.position = projectile.GlobalPosition;
			info.projectileHeight = projectile.projectileHeight;
			info.onSourceDespawn = projectile.Over;
		}
		return ProjectileHurt(in info, projectileConfig, playSplatAudio, velocity, isRange, createDamagePart);
	}

	private static bool IsMeteorProjectile(TowerDefenseProjectileConfig projectileConfig)
	{
		bool flag = projectileConfig != null;
		if (flag)
		{
			string name = projectileConfig.name;
			bool flag2 = ((name == "MeteorStar" || name == "MeteorStarS") ? true : false);
			flag = flag2;
		}
		return flag;
	}

	private bool TryApplyStableProjectileBodyHit(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, double num, int damageFlags, int fireMethodFlags, bool hasSourceBullet, bool playSplatAudio, Vector2 velocity, bool isRange, bool createDamagePart, out double result)
	{
		result = 0.0;
		if (!hasSourceBullet || isRange || !projectileConfig.UsesDefaultDamage || (damageFlags & -65) != 2 || HasHelmetArmor || HasBodyArmor || invincible)
		{
			return false;
		}
		bool flag = (fireMethodFlags & 4) != 0;
		bool flag2 = (fireMethodFlags & 0x40) != 0;
		bool num2 = projectileConfig.useRange || (flag && !flag2);
		bool flag3 = character is TowerDefensePlant;
		bool hasIncomingDamageModifiers = character.buff.HasIncomingDamageModifiers;
		if (num2 && (HasShieldArmor || flag3 || hasIncomingDamageModifiers))
		{
			return false;
		}
		if (flag)
		{
			List<TowerDefenseArmorInstance> armorRuntime = _armorRuntime;
			int num3 = armorRuntime?.Count ?? 0;
			for (int i = 0; i < num3; i++)
			{
				TowerDefenseArmorInstance towerDefenseArmorInstance = armorRuntime[i];
				if (!towerDefenseArmorInstance.isRemove && (towerDefenseArmorInstance.armorMethodFlags & 0x400) != 0)
				{
					return false;
				}
			}
			if ((physiqueTypeFlags & 0x400) != 0)
			{
				return false;
			}
		}
		if ((fireMethodFlags & 1) != 0)
		{
			float num4 = (info.hasTargetTransform ? info.targetOriginX : character.GetLogicalGlobalPosition().X);
			float num5 = (info.hasTargetTransform ? info.targetScaleX : character.Scale.X);
			float num6 = info.position.X - num4;
			if ((num6 > 30f && num5 > 0f) || (num6 < -30f && num5 < 0f))
			{
				return false;
			}
		}
		if (hasIncomingDamageModifiers)
		{
			num = character.buff.SetAttackNum(num);
		}
		num *= dealHurtScale;
		num = ApplyDealHurtReduce(num);
		if (flag3)
		{
			num = _ShieldAbsorbDamage(num);
		}
		result = ((num <= 0.0) ? 0.0 : ((hasIncomingDamageModifiers || flag3) ? DealHurt(num, playSplatAudio, velocity, createDamagePart) : SkipInvincibleDealHurt(num, playSplatAudio, velocity, createDamagePart)));
		return true;
	}

	public double ProjectileHurt(in ProjectileHitInfo info, TowerDefenseProjectileConfig projectileConfig, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool isRange = false, bool createDamagePart = true)
	{
		if (die)
		{
			return 1000000.0;
		}
		double num = projectileConfig.baseDamage;
		int num2 = projectileConfig.damageFlags;
		bool flag = info.onSourceDespawn != null;
		if (flag || info.useRuntimeOverrides)
		{
			num = info.damage;
			num2 = info.damageFlags;
		}
		int num3 = (info.useRuntimeOverrides ? info.fireMethodFlags : projectileConfig.fireMethodFlags);
		bool flag2 = false;
		if (isChests)
		{
			num *= projectileConfig.hitChestsScale;
		}
		if ((physiqueTypeFlags & 1) != 0)
		{
			num *= projectileConfig.hitNutScale;
		}
		if (character.buff.HasActiveBuffs && character.buff.BuffHas("Frozen"))
		{
			num *= projectileConfig.hitFrozenScale;
		}
		if ((physiqueTypeFlags & 0x800) != 0)
		{
			num *= projectileConfig.hitMachineScale;
		}
		if (IsMagicDamage(num2) && !IsMagicPhysique)
		{
			num *= 1.5;
		}
		if (isRange)
		{
			num *= projectileConfig.hitPesontage;
		}
		else if (projectileConfig.useRange && (num2 & 1) == 0)
		{
			flag2 = true;
		}
		if (TryApplyStableProjectileBodyHit(in info, projectileConfig, num, num2, num3, flag, playSplatAudio, velocity, isRange, createDamagePart, out var result))
		{
			return result;
		}
		bool num4 = (num3 & 4) != 0;
		bool flag3 = (num3 & 0x40) != 0;
		if (num4)
		{
			flag2 = !flag3;
			if (flag)
			{
				List<TowerDefenseArmorInstance> armorRuntime = _armorRuntime;
				int num5 = armorRuntime?.Count ?? 0;
				for (int i = 0; i < num5; i++)
				{
					TowerDefenseArmorInstance towerDefenseArmorInstance = armorRuntime[i];
					if (!towerDefenseArmorInstance.isRemove && (towerDefenseArmorInstance.armorMethodFlags & 0x400) != 0)
					{
						info.onSourceDespawn?.Invoke();
						if ((num2 & 1) == 0)
						{
							num2 |= 1;
							flag2 = false;
						}
					}
				}
				if ((physiqueTypeFlags & 0x400) != 0)
				{
					info.onSourceDespawn?.Invoke();
					if ((num2 & 1) == 0)
					{
						num2 |= 1;
						flag2 = false;
					}
				}
			}
		}
		if (flag2 && (invincible || HasShieldArmor || character is TowerDefensePlant || character.buff.HasIncomingDamageModifiers))
		{
			FlagHurt(num, 1, playSplatAudio, velocity, createDamagePart, isRange);
		}
		if (flag && (num3 & 1) != 0)
		{
			float num6 = (info.hasTargetTransform ? info.targetOriginX : character.GetLogicalGlobalPosition().X);
			float num7 = (info.hasTargetTransform ? info.targetScaleX : character.Scale.X);
			float num8 = info.position.X - num6;
			if ((num8 > 30f && num7 > 0f) || (num8 < -30f && num7 < 0f))
			{
				num2 ^= 1;
			}
		}
		if (projectileConfig.UsesExplosionDamage)
		{
			if (!isRange && IsMeteorProjectile(projectileConfig))
			{
				if (flag)
				{
					return FlagHurt(num, num2, playSplatAudio, velocity, createDamagePart, isRange, (int)info.projectileHeight);
				}
				return FlagHurt(num, num2, playSplatAudio, velocity, createDamagePart, isRange);
			}
			return ExplodeHurtCore(num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio, velocity);
		}
		if (projectileConfig.UsesDefaultDamage && flag)
		{
			return FlagHurt(num, num2, playSplatAudio, velocity, createDamagePart, isRange, (int)info.projectileHeight);
		}
		return FlagHurt(num, num2, playSplatAudio, velocity, createDamagePart, isRange);
	}

	public double FlagHurt(double num, int damageFlags, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true, bool isRange = false, int projectileHeight = 2)
	{
		damageFlags &= -65;
		if (invincible)
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		if (die)
		{
			return 1000000.0;
		}
		num = character.buff.SetAttackNum(num);
		num *= dealHurtScale;
		num = ApplyDealHurtReduce(num);
		num = _ShieldAbsorbDamage(num);
		if (num <= 0.0)
		{
			return 0.0;
		}
		if ((damageFlags & 0x10) != 0)
		{
			return DealHurt(num, playSplatAudio, velocity, createDamagePart);
		}
		if (!isRange && damageFlags == 2 && !HasHelmetArmor && !HasBodyArmor)
		{
			return DealHurt(num, playSplatAudio, velocity, createDamagePart);
		}
		bool flag = false;
		if ((damageFlags & 4) != 0 && character.buff.BuffHas("RedHeat"))
		{
			num *= 2.0;
		}
		if (num > 0.0 && (isRange || (damageFlags & 8) != 0) && HasHeadCoverArmor)
		{
			ArmorLayerResult armorLayerResult = ProcessArmorLayer(_armorHeadCoverRuntime, num, playSplatAudio, velocity, createDamagePart, isRange, skipBodyInvincible: false, projectileHeight);
			num = armorLayerResult.RemainingDamage;
			flag = flag || armorLayerResult.PassedToBody;
		}
		if (num > 0.0 && (isRange || (damageFlags & 1) != 0) && HasShieldArmor)
		{
			ArmorLayerResult armorLayerResult2 = ProcessArmorLayer(_armorShieldRuntime, num, playSplatAudio, velocity, createDamagePart, isRange, skipBodyInvincible: false, projectileHeight);
			num = armorLayerResult2.RemainingDamage;
			flag = flag || armorLayerResult2.PassedToBody;
		}
		if (damageFlags == 1)
		{
			return 0.0;
		}
		if (num > 0.0 && (damageFlags & 2) != 0)
		{
			if ((damageFlags & 4) != 0)
			{
				character.buff.ApplyFireHit();
			}
			if (HasHelmetArmor)
			{
				ArmorLayerResult armorLayerResult3 = ProcessArmorLayer(_armorHelmRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: false);
				num = armorLayerResult3.RemainingDamage;
				flag = flag || armorLayerResult3.PassedToBody;
			}
			if (HasBodyArmor)
			{
				ArmorLayerResult armorLayerResult4 = ProcessArmorLayer(_armorBodyRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: false);
				num = armorLayerResult4.RemainingDamage;
				flag = flag || armorLayerResult4.PassedToBody;
			}
		}
		if (num > 0.0 && !flag)
		{
			num = DealHurt(num, playSplatAudio, velocity, createDamagePart);
		}
		return num;
	}

	public double ExplodeHurt(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind = TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		return ExplodeHurtCore(num, damageKind, playSplatAudio, velocity);
	}

	private double ExplodeHurtCore(double num, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind, bool playSplatAudio, Vector2 velocity)
	{
		if (invincible)
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		num *= dealHurtScale;
		num = ApplyDealHurtReduce(num);
		bool flag = character is TowerDefensePlant;
		if (flag && num >= 1800.0)
		{
			num = 10000000.0;
		}
		if (flag && num > 0.0 && GodotObject.IsInstanceValid(character.cell) && HasExplosionShieldInCell(character))
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		if (flag && GodotObject.IsInstanceValid(character.cell))
		{
			TowerDefenseCharacter slot = character.cell.GetSlot(character);
			if (GodotObject.IsInstanceValid(slot) && GodotObject.IsInstanceValid(slot.instance))
			{
				double num2 = ((explosionHurt < 0.0) ? num : Mathf.Min(explosionHurt, num));
				if (slot.instance.hitpoints - num2 > 0.0 && hitpoints - num2 <= 0.0)
				{
					return num2;
				}
			}
		}
		num = _ShieldAbsorbDamage(num);
		if (num <= 0.0)
		{
			return 0.0;
		}
		if (explosionHurt >= 0.0)
		{
			num = Mathf.Min(explosionHurt, num);
		}
		if (damageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.JALA)
		{
			character.buff.ApplyFireHit();
			if (character.buff.BuffHas("RedHeat"))
			{
				num *= 2.0;
			}
		}
		if (HasHeadCoverArmor)
		{
			num = ProcessExplosionArmorLayer(_armorHeadCoverRuntime, num, playSplatAudio, velocity, consumeOverflow: false);
		}
		if (num > 0.0 && HasShieldArmor)
		{
			num = ProcessExplosionArmorLayer(_armorShieldRuntime, num, playSplatAudio, velocity, consumeOverflow: false);
		}
		if (num > 0.0 && HasHelmetArmor)
		{
			num = ProcessExplosionArmorLayer(_armorHelmRuntime, num, playSplatAudio, velocity, consumeOverflow: true);
		}
		if (num > 0.0 && HasBodyArmor)
		{
			num = ProcessExplosionArmorLayer(_armorBodyRuntime, num, playSplatAudio, velocity, consumeOverflow: true);
		}
		bool createDamagePart = damageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.OTHER;
		bool flag2 = false;
		if (num != 0.0)
		{
			bool num3 = hitpoints > 0.0 && !die;
			num = DealHurt(num, playSplatAudio, velocity, createDamagePart);
			flag2 = num3 && die && hitpoints <= 0.0;
		}
		if (num > 0.0)
		{
			EmitHitpointsNearDie();
			nearDie = true;
			if (!(Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && flag))
			{
				DieMethod();
			}
		}
		if ((num > 0.0 || flag2) && (!(character is TowerDefenseZombie) || zombiePhysique != TowerDefenseEnum.ZOMBIE_PHYSIQUE.BOSS) && damageKind != TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.OTHER)
		{
			CreateExplosionAshEffect();
			DeferNetworkExplosionDestroy(damageKind);
		}
		return num;
	}

	private bool HasExplosionShieldInCell(TowerDefenseCharacter target)
	{
		if (target.cell == null)
		{
			return false;
		}
		List<TowerDefenseCharacter> characterList = target.cell.GetCharacterList();
		for (int i = 0; i < characterList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = characterList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy && towerDefenseCharacter.config != null && towerDefenseCharacter.config.name == "PlantPumpkinRobot" && towerDefenseCharacter.camp == target.camp)
			{
				return true;
			}
		}
		return false;
	}

	private void CreateExplosionAshEffect()
	{
		if (!character.inWater && ashScene != null)
		{
			TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = TowerDefenseManager.CreateEffectSpriteOnce(ashScene, character.gridPos, "Idle");
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			towerDefenseEffectSpriteOnce.GlobalPosition = character.GetLogicalGlobalPosition(character.sprite);
			towerDefenseEffectSpriteOnce.Scale = character.Scale * character.transformPoint.Scale;
			characterNode.AddChild(towerDefenseEffectSpriteOnce, forceReadableName: false, Node.InternalMode.Disabled);
			towerDefenseEffectSpriteOnce.ZIndex -= 6;
		}
	}

	private void DeferNetworkExplosionDestroy(TowerDefenseEnum.EXPLOSION_DAMAGE_KIND damageKind)
	{
		if (!Global.IsMultiplayerMode || !MultiPlayerManager.IsHost || character.syncId < 0 || damageKind == TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.OTHER)
		{
			character.Destroy();
		}
		else
		{
			if (_networkExplodeDestroyDeferred)
			{
				return;
			}
			_networkExplodeDestroyDeferred = true;
			TowerDefenseCharacter target = character;
			bool explosionFlag = target.isExplode;
			Callable.From(delegate
			{
				_networkExplodeDestroyDeferred = false;
				if (!GodotObject.IsInstanceValid(target) || target.isDestroy)
				{
					return;
				}
				bool isExplode = target.isExplode;
				target.isExplode = explosionFlag;
				try
				{
					target.Destroy();
				}
				finally
				{
					if (GodotObject.IsInstanceValid(target))
					{
						target.isExplode = isExplode;
					}
				}
			}).CallDeferred();
		}
	}

	private static double ProcessExplosionArmorLayer(List<TowerDefenseArmorInstance> armorList, double num, bool playSplatAudio, Vector2 velocity, bool consumeOverflow)
	{
		int num2 = 0;
		while (num2 < armorList.Count)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = armorList[num2];
			if (towerDefenseArmorInstance == null || towerDefenseArmorInstance.isRemove)
			{
				num2++;
				continue;
			}
			double explodePersontage = towerDefenseArmorInstance.typeData.explodePersontage;
			double num3 = towerDefenseArmorInstance.DealHurt(num * explodePersontage, playSplatAudio, velocity);
			if (explodePersontage == 0.0)
			{
				return 0.0;
			}
			if (consumeOverflow)
			{
				num = num3;
			}
			if (num2 < armorList.Count && armorList[num2] == towerDefenseArmorInstance)
			{
				num2++;
			}
			if (num <= 0.0)
			{
				break;
			}
		}
		return num;
	}

	public double SkipInvincibleHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true)
	{
		if (die)
		{
			return 1000000.0;
		}
		num *= dealHurtScale;
		num = ApplyDealHurtReduce(num);
		num = _ShieldAbsorbDamage(num);
		if (num <= 0.0)
		{
			return 0.0;
		}
		bool flag = false;
		if (num > 0.0)
		{
			if (hitShield && HasShieldArmor)
			{
				ArmorLayerResult armorLayerResult = ProcessArmorLayer(_armorShieldRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: true);
				num = armorLayerResult.RemainingDamage;
				flag = flag || armorLayerResult.PassedToBody;
			}
			if (HasHelmetArmor)
			{
				ArmorLayerResult armorLayerResult2 = ProcessArmorLayer(_armorHelmRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: true);
				num = armorLayerResult2.RemainingDamage;
				flag = flag || armorLayerResult2.PassedToBody;
			}
			if (HasBodyArmor)
			{
				ArmorLayerResult armorLayerResult3 = ProcessArmorLayer(_armorBodyRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: true);
				num = armorLayerResult3.RemainingDamage;
				flag = flag || armorLayerResult3.PassedToBody;
			}
			if (num > 0.0 && !flag)
			{
				num = SkipInvincibleDealHurt(num, playSplatAudio, velocity, createDamagePart);
			}
		}
		return num;
	}

	public double Hurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool hitShield = true, bool createDamagePart = true, double damageLimit = -1.0)
	{
		if (invincible)
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		if (invincibleHurt)
		{
			return 0.0;
		}
		if (die)
		{
			return 1000000.0;
		}
		num *= dealHurtScale;
		num = ApplyDealHurtReduce(num);
		num = _ShieldAbsorbDamage(num);
		if (num <= 0.0)
		{
			return 0.0;
		}
		if (damageLimit >= 0.0)
		{
			num = Mathf.Min(num, ApplyDealHurtReduce(damageLimit * dealHurtScale));
		}
		bool flag = false;
		if (num > 0.0)
		{
			if (hitShield && HasShieldArmor)
			{
				ArmorLayerResult armorLayerResult = ProcessArmorLayer(_armorShieldRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: false);
				num = armorLayerResult.RemainingDamage;
				flag = flag || armorLayerResult.PassedToBody;
			}
			if (HasHelmetArmor)
			{
				ArmorLayerResult armorLayerResult2 = ProcessArmorLayer(_armorHelmRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: false);
				num = armorLayerResult2.RemainingDamage;
				flag = flag || armorLayerResult2.PassedToBody;
			}
			if (HasBodyArmor)
			{
				ArmorLayerResult armorLayerResult3 = ProcessArmorLayer(_armorBodyRuntime, num, playSplatAudio, velocity, createDamagePart, isRange: false, skipBodyInvincible: false);
				num = armorLayerResult3.RemainingDamage;
				flag = flag || armorLayerResult3.PassedToBody;
			}
			if (num > 0.0 && !flag)
			{
				num = DealHurt(num, playSplatAudio, velocity, createDamagePart);
			}
		}
		return num;
	}

	public double SmashHurtApply(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2))
	{
		if (invincible)
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		if (invincibleSmash)
		{
			ComponentManager componentManager = character.componentManager;
			if (componentManager != null && componentManager.GetRuntime<PotatoComponent>()?.TryExplodeOnSmash() == true && GodotObject.IsInstanceValid(character.cell))
			{
				character.cell.smashAbsorbedFrame = Engine.GetPhysicsFrames();
			}
			return 0.0;
		}
		if (character is TowerDefensePlant)
		{
			double num2 = 100000.0;
			if (smashHurt != -1.0)
			{
				num2 = Mathf.Min(smashHurt, num2);
			}
			if (GodotObject.IsInstanceValid(character.cell))
			{
				TowerDefenseCharacter slot = character.cell.GetSlot(character);
				if (GodotObject.IsInstanceValid(slot) && slot.instance.hitpoints - num2 > 0.0 && character.instance.hitpoints - num2 <= 0.0)
				{
					return num;
				}
			}
		}
		character.isSmash = true;
		if (character is TowerDefensePlant)
		{
			num = 100000.0;
		}
		num = Hurt(num, playSplatAudio, velocity, hitShield: true, createDamagePart: true, smashHurt);
		character.isSmash = false;
		return num;
	}

	public double SkipInvincibleDealHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (character is TowerDefenseGravestone { IsPermanentObstacle: not false })
		{
			return num;
		}
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && character is TowerDefenseZombie)
		{
			return num;
		}
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		if (character is TowerDefenseZombie && playSplatAudio)
		{
			if (impactAudio != "")
			{
				AudioManager.Instance.AudioPlay(impactAudio);
			}
			else
			{
				AudioManager.Instance.AudioPlay("SplatNormal");
			}
		}
		TowerDefensePerfProfiler.EndSpikeProbe("damage.body.audio", in probe, playSplatAudio ? 1 : 0);
		TowerDefensePerfProfiler.End("damage.body.audio", probe.StartTicks, playSplatAudio ? 1 : 0);
		TowerDefensePerfProfiler.SpikeProbe probe2 = TowerDefensePerfProfiler.BeginSpikeProbe();
		if (damageHitpointsFloor > 0.0 && num > 0.0 && hitpoints > 0.0 && !die)
		{
			num = Math.Min(num, Math.Max(0.0, hitpoints - damageHitpointsFloor));
		}
		if (hitpoints > num)
		{
			double num2 = num;
			character.EmitBodyHurt((int)num);
			hitpoints -= num;
			num = 0.0;
			if (num2 > 0.0 && CanSkipStableBodyDamagePostProcessing())
			{
				TowerDefensePerfProfiler.EndSpikeProbe("damage.body.applyStable", in probe2, 1);
				return 0.0;
			}
		}
		else
		{
			character.EmitBodyHurt((int)hitpoints);
			num -= hitpoints;
			hitpoints = 0.0;
		}
		TowerDefensePerfProfiler.EndSpikeProbe("damage.body.apply", in probe2, 1);
		TowerDefensePerfProfiler.SpikeProbe probe3 = TowerDefensePerfProfiler.BeginSpikeProbe();
		if (damagePointData != null)
		{
			double num3 = (hitpoints - hitpointsNearDeath) / (hitpointsSave - hitpointsNearDeath);
			if ((uint)damagePointIndex < (uint)_damagePointPercentages.Length)
			{
				while ((uint)damagePointIndex < (uint)_damagePointPercentages.Length && num3 <= _damagePointPercentages[damagePointIndex])
				{
					string text = _damagePointNames[damagePointIndex];
					string text2 = _damagePointAudios[damagePointIndex];
					if (createDamagePart)
					{
						TowerDefensePerfProfiler.SpikeProbe probe4 = TowerDefensePerfProfiler.BeginSpikeProbe();
						velocity = ((!(velocity == default(Vector2))) ? new Vector2(velocity.X * (float)GD.RandRange(0.75, 1.25), velocity.Y * (float)GD.RandRange(0.75, 1.25)) : new Vector2((float)GD.RandRange(-100.0, 100.0) * (float)GD.RandRange(0.75, 1.25), -300f * (float)GD.RandRange(0.75, 1.25)));
						character.DamagePartCreate(text, null, velocity, keepSlotScale: true, default(Vector2), fromSync: false, null, 0L);
						TowerDefensePerfProfiler.End("damage.stage.part", probe4.StartTicks, 1);
						TowerDefensePerfProfiler.EndSpikeProbe("damage.stage.part", in probe4, 1, 0.5);
					}
					TowerDefensePerfProfiler.SpikeProbe probe5 = TowerDefensePerfProfiler.BeginSpikeProbe();
					damagePointData.CreateEffect(character.sprite, text, character.gridPos);
					TowerDefensePerfProfiler.End("damage.stage.effect", probe5.StartTicks, 1);
					TowerDefensePerfProfiler.EndSpikeProbe("damage.stage.effect", in probe5, 1, 0.5);
					if (playSplatAudio && text2 != "")
					{
						long startTicks = TowerDefensePerfProfiler.BeginHotPath();
						AudioManager.Instance.AudioPlay(text2);
						TowerDefensePerfProfiler.End("damage.stage.audio", startTicks, 1);
					}
					long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
					SetDamageStage(damagePointIndex);
					TowerDefensePerfProfiler.End("damage.stage.state", startTicks2, 1);
					damagePointIndex++;
					if (damagePointIndex >= damagePoints.Count)
					{
						break;
					}
				}
			}
		}
		TowerDefensePerfProfiler.EndSpikeProbe("damage.body.damageStages", in probe3, damagePointIndex);
		TowerDefensePerfProfiler.End("damage.body.damageStages", probe3.StartTicks, damagePointIndex);
		TowerDefensePerfProfiler.SpikeProbe probe6 = TowerDefensePerfProfiler.BeginSpikeProbe();
		if (!nearDie && hitpoints <= hitpointsNearDeath)
		{
			if (!keepArmor)
			{
				long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
				ArmorClear();
				TowerDefensePerfProfiler.End("damage.terminal.armorClear", startTicks3, 1);
			}
			TowerDefensePerfProfiler.SpikeProbe probe7 = TowerDefensePerfProfiler.BeginSpikeProbe();
			EmitHitpointsNearDie();
			TowerDefensePerfProfiler.End("damage.terminal.nearDieCallbacks", probe7.StartTicks, 1);
			TowerDefensePerfProfiler.EndSpikeProbe("damage.terminal.nearDieCallbacks", in probe7, 1, 0.5);
			nearDie = true;
		}
		if (!keepAlive && hitpoints <= 0.0)
		{
			if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && character is TowerDefensePlant)
			{
				hitpoints = 1.0;
				return num;
			}
			if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost && character is TowerDefenseZombie)
			{
				hitpoints = 1.0;
			}
			else
			{
				long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
				DieMethod();
				TowerDefensePerfProfiler.End("damage.terminal.dieMethod", startTicks4, 1);
			}
			TowerDefensePerfProfiler.SpikeProbe probe8 = TowerDefensePerfProfiler.BeginSpikeProbe();
			EmitHitpointsEmpty();
			TowerDefensePerfProfiler.End("damage.terminal.emptyCallbacks", probe8.StartTicks, 1);
			TowerDefensePerfProfiler.EndSpikeProbe("damage.terminal.emptyCallbacks", in probe8, 1, 0.5);
		}
		TowerDefensePerfProfiler.EndSpikeProbe("damage.body.terminal", in probe6, (hitpoints <= 0.0) ? 1 : 0);
		TowerDefensePerfProfiler.End("damage.body.terminal", probe6.StartTicks, (hitpoints <= 0.0) ? 1 : 0);
		return num;
	}

	private bool CanSkipStableBodyDamagePostProcessing()
	{
		if (hitpoints <= 0.0 || (!nearDie && hitpoints <= hitpointsNearDeath))
		{
			return false;
		}
		if (damagePointData == null || (uint)damagePointIndex >= (uint)_damagePointPercentages.Length)
		{
			return true;
		}
		double num = hitpointsSave - hitpointsNearDeath;
		if (!(num > 0.0))
		{
			return false;
		}
		double num2 = hitpointsNearDeath + num * _damagePointPercentages[damagePointIndex];
		return hitpoints > num2;
	}

	public double DealHurt(double num, bool playSplatAudio = true, Vector2 velocity = default(Vector2), bool createDamagePart = true)
	{
		if (invincible)
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		return SkipInvincibleDealHurt(num, playSplatAudio, velocity, createDamagePart);
	}

	public void Health(double num)
	{
		hitpoints += num;
		RefreshDamagePoint();
	}

	internal void RefreshArmorRuntimeIndex()
	{
		RebuildArmorRuntimeList(armorList, ref _armorRuntime);
		RebuildArmorRuntimeList(armorShield, ref _armorShieldRuntime);
		RebuildArmorRuntimeList(armorHelm, ref _armorHelmRuntime);
		RebuildArmorRuntimeList(armorBody, ref _armorBodyRuntime);
		RebuildArmorRuntimeList(armorHeadCover, ref _armorHeadCoverRuntime);
	}

	private static void RebuildArmorRuntimeList(Array<TowerDefenseArmorInstance> source, ref List<TowerDefenseArmorInstance> runtime)
	{
		runtime?.Clear();
		int num = source?.Count ?? 0;
		if (num != 0)
		{
			if (runtime == null)
			{
				runtime = new List<TowerDefenseArmorInstance>(num);
			}
			for (int i = 0; i < num; i++)
			{
				runtime.Add(source[i]);
			}
		}
	}

	private void AddArmorRuntimeReferences(TowerDefenseArmorInstance instance, int flags)
	{
		(_armorRuntime ?? (_armorRuntime = new List<TowerDefenseArmorInstance>(1))).Add(instance);
		if ((flags & 4) != 0)
		{
			(_armorShieldRuntime ?? (_armorShieldRuntime = new List<TowerDefenseArmorInstance>(1))).Add(instance);
		}
		if ((flags & 8) != 0)
		{
			(_armorHelmRuntime ?? (_armorHelmRuntime = new List<TowerDefenseArmorInstance>(1))).Add(instance);
		}
		if ((flags & 2) != 0)
		{
			(_armorBodyRuntime ?? (_armorBodyRuntime = new List<TowerDefenseArmorInstance>(1))).Add(instance);
		}
		if ((flags & 0x800) != 0)
		{
			(_armorHeadCoverRuntime ?? (_armorHeadCoverRuntime = new List<TowerDefenseArmorInstance>(1))).Add(instance);
		}
	}

	internal void RemoveArmorRuntimeReferences(TowerDefenseArmorInstance instance)
	{
		armorList.Remove(instance);
		_armorRuntime?.Remove(instance);
		int num = instance?.armorMethodFlags ?? 0;
		if ((num & 4) != 0)
		{
			armorShield.Remove(instance);
			_armorShieldRuntime?.Remove(instance);
		}
		if ((num & 8) != 0)
		{
			armorHelm.Remove(instance);
			_armorHelmRuntime?.Remove(instance);
		}
		if ((num & 2) != 0)
		{
			armorBody.Remove(instance);
			_armorBodyRuntime?.Remove(instance);
		}
		if ((num & 0x800) != 0)
		{
			armorHeadCover.Remove(instance);
			_armorHeadCoverRuntime?.Remove(instance);
		}
	}

	private double ApplyDealHurtReduce(double num)
	{
		if (skipDealHurtReduce)
		{
			return num;
		}
		if (dealHurtReduce > 0.0 && num > 0.0)
		{
			num = Math.Max(0.0, num - dealHurtReduce);
			if (num <= 0.0)
			{
				character.EmitDamageBlocked();
			}
		}
		return num;
	}

	private double _ShieldAbsorbDamage(double num)
	{
		if (num <= 0.0)
		{
			return 0.0;
		}
		if (!(character is TowerDefensePlant))
		{
			return num;
		}
		if (!GodotObject.IsInstanceValid(character.cell))
		{
			return num;
		}
		TowerDefenseItemSheild itemShield = character.cell.itemShield;
		if (!GodotObject.IsInstanceValid(itemShield) || itemShield == character)
		{
			return num;
		}
		if (itemShield.instance.hypnoses != character.instance.hypnoses)
		{
			return num;
		}
		if (hitpoints - num <= 0.0 && itemShield.ShieldBlockLethal())
		{
			character.EmitBodyHurt(0);
			return 0.0;
		}
		return itemShield.ShieldAbsorbDamage(num);
	}

	public bool ArmorHas(string armorName)
	{
		foreach (TowerDefenseArmorInstance armor in armorList)
		{
			if (armor.slotConfig.armorName == armorName)
			{
				return true;
			}
		}
		return false;
	}

	internal bool HasBurstWheelArmor()
	{
		if (_armorRuntime == null)
		{
			return false;
		}
		for (int i = 0; i < _armorRuntime.Count; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = _armorRuntime[i];
			if (towerDefenseArmorInstance != null && !towerDefenseArmorInstance.isRemove && !(towerDefenseArmorInstance.hitPoints <= 0.0) && (towerDefenseArmorInstance.armorMethodFlags & 0x1000) != 0)
			{
				return true;
			}
		}
		return false;
	}

	public void ArmorAdd(string armorName)
	{
		if (!GodotObject.IsInstanceValid(armorData) || string.IsNullOrEmpty(armorName) || ArmorHas(armorName))
		{
			return;
		}
		ArmorSlotConfig orCreateSlotConfig = armorData.GetOrCreateSlotConfig(armorName);
		if (orCreateSlotConfig == null)
		{
			return;
		}
		TowerDefenseArmorTypeData armorType = TowerDefenseArmorRegistry.GetArmorType(armorName);
		if (IsExclusiveWearableHelmet(armorType))
		{
			foreach (TowerDefenseArmorInstance item in armorList.Duplicate())
			{
				if (IsExclusiveWearableHelmet(item?.typeData))
				{
					item.RemoveArmor(ArmorRemovalReason.Replaced);
					ArmorDestroy(item);
				}
			}
		}
		if (armorName == "SpecialHelmet")
		{
			armorOverrideUnUseBuffFlagSave = unUseBuffFlags;
			unUseBuffFlags = -1;
		}
		TowerDefenseArmorInstance towerDefenseArmorInstance = new TowerDefenseArmorInstance(character, orCreateSlotConfig);
		towerDefenseArmorInstance.remove += ArmorDestroy;
		towerDefenseArmorInstance.hitpointsEmpty += ArmorDestroy;
		towerDefenseArmorInstance.damagePointReach += ArmorDamagePointReachHandler;
		towerDefenseArmorInstance.hitpointScale = hitpointScale;
		armorList.Add(towerDefenseArmorInstance);
		if (armorType != null && (armorType.armorMethodFlags & 4) != 0)
		{
			armorShield.Add(towerDefenseArmorInstance);
		}
		if (armorType != null && (armorType.armorMethodFlags & 8) != 0)
		{
			armorHelm.Add(towerDefenseArmorInstance);
		}
		if (armorType != null && (armorType.armorMethodFlags & 2) != 0)
		{
			armorBody.Add(towerDefenseArmorInstance);
		}
		if (armorType != null && (armorType.armorMethodFlags & 0x800) != 0)
		{
			armorHeadCover.Add(towerDefenseArmorInstance);
		}
		if (armorType != null && (armorType.armorMethodFlags & 0x80) != 0 && !character.damagePart.ContainsKey(armorName))
		{
			character.damagePart[armorName] = orCreateSlotConfig;
		}
		AddArmorRuntimeReferences(towerDefenseArmorInstance, towerDefenseArmorInstance.armorMethodFlags);
	}

	private static bool IsExclusiveWearableHelmet(TowerDefenseArmorTypeData typeData)
	{
		if (!GodotObject.IsInstanceValid(typeData))
		{
			return false;
		}
		int armorMethodFlags = typeData.armorMethodFlags;
		if ((armorMethodFlags & 8) != 0 && (armorMethodFlags & 0x80) != 0)
		{
			return (armorMethodFlags & 2) == 0;
		}
		return false;
	}

	public void ArmorDelete(string armorName, bool createDamagePart = true)
	{
		foreach (TowerDefenseArmorInstance item in armorList.Duplicate())
		{
			if (item.slotConfig.armorName == armorName)
			{
				item.Hurt(10000000.0, playSplatAudio: false, Vector2.Zero, createDamagePart);
			}
		}
	}

	public void ArmorDamagePointReachHandler(TowerDefenseArmorInstance instance, int stage)
	{
		this.armorDamagePointReach?.Invoke(instance.slotConfig.armorName, stage);
	}

	public void ArmorDestroy(TowerDefenseArmorInstance instance)
	{
		if (instance.slotConfig.armorName == "SpecialHelmet")
		{
			unUseBuffFlags = armorOverrideUnUseBuffFlagSave;
		}
		instance.isRemove = true;
		RemoveArmorRuntimeReferences(instance);
		this.armorHitpointsEmpty?.Invoke(instance.slotConfig.armorName);
	}

	public void ArmorClear()
	{
		foreach (TowerDefenseArmorInstance item in armorList.Duplicate())
		{
			item.Hurt(10000000.0, playSplatAudio: false, Vector2.Zero, createDamagePart: true, ignoreLimit: true);
		}
	}

	public TowerDefenseMagnet ArmorDraw(TowerDefenseArmorInstance instance)
	{
		if (!GodotObject.IsInstanceValid(instance) || instance.isRemove || !armorList.Contains(instance))
		{
			return null;
		}
		TowerDefenseMagnet towerDefenseMagnet = instance.Draw();
		if (!GodotObject.IsInstanceValid(towerDefenseMagnet))
		{
			return null;
		}
		character.EmitArmorHurt((int)instance.hitPoints);
		return towerDefenseMagnet;
	}

	public void RefeshHitPoint()
	{
		hitpoints = hitpointsBase;
	}

	public void DieMethod()
	{
		collisionFlags = 0;
		maskFlags = 4;
		if (!keepArmor)
		{
			ArmorClear();
		}
		die = true;
	}

	public void SetDamageStage(int index)
	{
		string text = (string)damagePoints[damagePointIndex]["Name"];
		damagePointData.SetDamagePointFliters(character.sprite, text);
		if (customData != null)
		{
			foreach (string item in character.currentCustom)
			{
				customData.SetDamagePoint(character.sprite, item, index);
			}
		}
		this.damagePointReach?.Invoke(text);
	}

	public void RefreshDamagePoint()
	{
		if (damagePointData == null || damagePointIndex == 0 || character is TowerDefenseZombie || character is TowerDefenseZombieGargantuarBase || !GodotObject.IsInstanceValid(character) || !GodotObject.IsInstanceValid(character.sprite))
		{
			return;
		}
		double num = (hitpoints - hitpointsNearDeath) / (hitpointsSave - hitpointsNearDeath);
		int num2 = 0;
		if (hitpoints < hitpointsSave)
		{
			for (int i = 0; i < damagePoints.Count && num <= (double)damagePoints[i]["Persontage"]; i++)
			{
				num2 = i + 1;
			}
		}
		if (num2 == damagePointIndex)
		{
			return;
		}
		damagePointData.ClearDamagePointAll(character.sprite);
		if (customData != null)
		{
			foreach (string item in character.currentCustom)
			{
				customData.ClearDamagePoint(character.sprite, item);
			}
		}
		for (int j = 0; j < num2; j++)
		{
			string damagePointName = (string)damagePoints[j]["Name"];
			damagePointData.SetDamagePointFliters(character.sprite, damagePointName);
			if (customData == null)
			{
				continue;
			}
			foreach (string item2 in character.currentCustom)
			{
				customData.SetDamagePoint(character.sprite, item2, j);
			}
		}
		damagePointIndex = num2;
	}

	public Dictionary ExportSave()
	{
		Dictionary dictionary = new Dictionary
		{
			["hitpoints"] = hitpoints,
			["hitpointsSave"] = hitpointsSave,
			["hitpointScale"] = hitpointScale,
			["hitpointScaleSave"] = hitpointScaleSave,
			["nearDie"] = nearDie,
			["die"] = die,
			["invincible"] = invincible,
			["invincibleHurt"] = invincibleHurt,
			["invincibleSmash"] = invincibleSmash,
			["sleep"] = sleep,
			["wakeUp"] = wakeUp,
			["hypnoses"] = hypnoses,
			["hologram"] = hologram,
			["damagePointIndex"] = damagePointIndex,
			["dealHurtScale"] = dealHurtScale,
			["dealHurtReduce"] = dealHurtReduce,
			["collisionFlags"] = collisionFlags,
			["maskFlags"] = maskFlags,
			["unUseBuffFlags"] = unUseBuffFlags,
			["armorOverrideUnUseBuffFlagSave"] = armorOverrideUnUseBuffFlagSave,
			["explosionHurt"] = explosionHurt,
			["explosionHurtSave"] = explosionHurtSave,
			["smashHurt"] = smashHurt,
			["dragHurt"] = dragHurt,
			["spikeHurt"] = spikeHurt,
			["biteHurt"] = biteHurt,
			["canCollection"] = canCollection,
			["canBeCollection"] = canBeCollection,
			["keepArmor"] = keepArmor,
			["keepAlive"] = keepAlive,
			["physiqueTypeFlags"] = physiqueTypeFlags,
			["elementFlags"] = elementFlags,
			["hitpointsNearDeath"] = hitpointsNearDeath,
			["hitpointsBase"] = hitpointsBase,
			["zombiePhysique"] = (int)zombiePhysique,
			["height"] = (int)height,
			["isChests"] = isChests
		};
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (TowerDefenseArmorInstance armor in armorList)
		{
			if (!armor.isRemove)
			{
				array.Add(armor.ExportSave());
			}
		}
		dictionary["armorList"] = array;
		return dictionary;
	}

	public void ImportSave(Dictionary data)
	{
		hitpointScaleSave = (data.ContainsKey("hitpointScaleSave") ? data["hitpointScaleSave"].AsDouble() : hitpointScaleSave);
		hitpointScale = (data.ContainsKey("hitpointScale") ? data["hitpointScale"].AsDouble() : hitpointScale);
		hitpointsSave = (data.ContainsKey("hitpointsSave") ? data["hitpointsSave"].AsDouble() : hitpointsSave);
		hitpoints = (data.ContainsKey("hitpoints") ? data["hitpoints"].AsDouble() : hitpoints);
		nearDie = (data.ContainsKey("nearDie") ? data["nearDie"].AsBool() : nearDie);
		die = (data.ContainsKey("die") ? data["die"].AsBool() : die);
		invincible = (data.ContainsKey("invincible") ? data["invincible"].AsBool() : invincible);
		invincibleHurt = (data.ContainsKey("invincibleHurt") ? data["invincibleHurt"].AsBool() : invincibleHurt);
		invincibleSmash = (data.ContainsKey("invincibleSmash") ? data["invincibleSmash"].AsBool() : invincibleSmash);
		sleep = (data.ContainsKey("sleep") ? data["sleep"].AsBool() : sleep);
		wakeUp = (data.ContainsKey("wakeUp") ? data["wakeUp"].AsBool() : wakeUp);
		hypnoses = (data.ContainsKey("hypnoses") ? data["hypnoses"].AsBool() : hypnoses);
		hologram = (data.ContainsKey("hologram") ? data["hologram"].AsBool() : hologram);
		damagePointIndex = (data.ContainsKey("damagePointIndex") ? data["damagePointIndex"].AsInt32() : damagePointIndex);
		dealHurtScale = (data.ContainsKey("dealHurtScale") ? data["dealHurtScale"].AsDouble() : dealHurtScale);
		dealHurtReduce = (data.ContainsKey("dealHurtReduce") ? data["dealHurtReduce"].AsDouble() : dealHurtReduce);
		collisionFlags = (data.ContainsKey("collisionFlags") ? data["collisionFlags"].AsInt32() : collisionFlags);
		maskFlags = (data.ContainsKey("maskFlags") ? data["maskFlags"].AsInt32() : maskFlags);
		unUseBuffFlags = (data.ContainsKey("unUseBuffFlags") ? data["unUseBuffFlags"].AsInt32() : unUseBuffFlags);
		armorOverrideUnUseBuffFlagSave = (data.ContainsKey("armorOverrideUnUseBuffFlagSave") ? data["armorOverrideUnUseBuffFlagSave"].AsInt32() : armorOverrideUnUseBuffFlagSave);
		explosionHurt = (data.ContainsKey("explosionHurt") ? data["explosionHurt"].AsDouble() : explosionHurt);
		explosionHurtSave = (data.ContainsKey("explosionHurtSave") ? data["explosionHurtSave"].AsDouble() : explosionHurtSave);
		smashHurt = (data.ContainsKey("smashHurt") ? data["smashHurt"].AsDouble() : smashHurt);
		dragHurt = (data.ContainsKey("dragHurt") ? data["dragHurt"].AsDouble() : dragHurt);
		spikeHurt = (data.ContainsKey("spikeHurt") ? data["spikeHurt"].AsDouble() : spikeHurt);
		biteHurt = (data.ContainsKey("biteHurt") ? data["biteHurt"].AsDouble() : biteHurt);
		canCollection = (data.ContainsKey("canCollection") ? data["canCollection"].AsBool() : canCollection);
		canBeCollection = (data.ContainsKey("canBeCollection") ? data["canBeCollection"].AsBool() : canBeCollection);
		keepArmor = (data.ContainsKey("keepArmor") ? data["keepArmor"].AsBool() : keepArmor);
		keepAlive = (data.ContainsKey("keepAlive") ? data["keepAlive"].AsBool() : keepAlive);
		physiqueTypeFlags = (data.ContainsKey("physiqueTypeFlags") ? data["physiqueTypeFlags"].AsInt32() : physiqueTypeFlags);
		elementFlags = (GodotObject.IsInstanceValid(config) ? config.elementFlags : elementFlags);
		hitpointsNearDeath = (data.ContainsKey("hitpointsNearDeath") ? data["hitpointsNearDeath"].AsDouble() : hitpointsNearDeath);
		hitpointsBase = (data.ContainsKey("hitpointsBase") ? data["hitpointsBase"].AsDouble() : hitpointsBase);
		zombiePhysique = (data.ContainsKey("zombiePhysique") ? ((TowerDefenseEnum.ZOMBIE_PHYSIQUE)data["zombiePhysique"].AsInt32()) : zombiePhysique);
		height = (data.ContainsKey("height") ? ((TowerDefenseEnum.CHARACTER_HEIGHT)data["height"].AsInt32()) : height);
		isChests = (data.ContainsKey("isChests") ? data["isChests"].AsBool() : isChests);
		if (!data.ContainsKey("armorList"))
		{
			return;
		}
		foreach (Variant item in data["armorList"].AsGodotArray())
		{
			Dictionary dictionary = item.AsGodotDictionary();
			string text = (dictionary.ContainsKey("armorName") ? dictionary["armorName"].AsString() : "");
			if (text == "")
			{
				continue;
			}
			foreach (TowerDefenseArmorInstance armor in armorList)
			{
				if (armor.slotConfig.armorName == text)
				{
					armor.ImportSave(dictionary);
					break;
				}
			}
		}
	}
}
