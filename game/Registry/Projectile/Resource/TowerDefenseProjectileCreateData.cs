using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseProjectileCreateData : Resource
{
	[Export(PropertyHint.None, "")]
	public StringName projectileName;

	private StringName _skinName = new StringName("Default");

	[Export(PropertyHint.None, "")]
	public Vector2 size = new Vector2(28f, 28f);

	[Export(PropertyHint.None, "")]
	public Vector2 scale = new Vector2(1f, 1f);

	[Export(PropertyHint.None, "")]
	public double baseDamage = -1.0;

	[Export(PropertyHint.None, "")]
	public double hitChestsScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitNutScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitFrozenScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitMachineScale = 1.0;

	[Export(PropertyHint.None, "")]
	public int damageFlags = 3;

	[Export(PropertyHint.None, "")]
	public int collisionFlags = 1;

	[Export(PropertyHint.None, "")]
	public int fireMethodFlags = 1;

	[Export(PropertyHint.None, "")]
	public double catapultHeight = 300.0;

	[Export(PropertyHint.None, "")]
	public int penetrateNum = 3;

	[Export(PropertyHint.None, "")]
	public bool penetrateOverBack = true;

	[Export(PropertyHint.None, "")]
	public bool backOutGround = true;

	[Export(PropertyHint.None, "")]
	public double backDuration = 1.0;

	[Export(PropertyHint.None, "")]
	public bool rotateFollowVelocity;

	[Export(PropertyHint.None, "")]
	public bool overrideHitChestsScale;

	[Export(PropertyHint.None, "")]
	public bool overrideHitNutScale;

	[Export(PropertyHint.None, "")]
	public bool overrideHitFrozenScale;

	[Export(PropertyHint.None, "")]
	public bool overrideHitMachineScale;

	[Export(PropertyHint.None, "")]
	public bool overrideCatapultHeight;

	[Export(PropertyHint.None, "")]
	public bool overridePenetrateNum;

	[Export(PropertyHint.None, "")]
	public bool overridePenetrateOverBack;

	[Export(PropertyHint.None, "")]
	public bool overrideBackOutGround;

	[Export(PropertyHint.None, "")]
	public bool overrideBackDuration;

	[Export(PropertyHint.None, "")]
	public bool overrideRotateFollowVelocity;

	[Export(PropertyHint.None, "")]
	public bool rangeOverride;

	[Export(PropertyHint.None, "")]
	public string rangeType = "Default";

	[Export(PropertyHint.None, "")]
	public bool useRange;

	[Export(PropertyHint.None, "")]
	public Vector2 rangeSize = new Vector2(0.5f, 0.5f);

	[Export(PropertyHint.None, "")]
	public double hitPesontage = 0.25;

	[Export(PropertyHint.None, "")]
	public bool overrideHitTargetEvent;

	[Export(PropertyHint.None, "")]
	public bool overrideHitCharacterEvent;

	[Export(PropertyHint.None, "")]
	public bool overrideHitGroundEvent;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitTargetEventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitCharacterEventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitGroundEventList = new Array<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<StringName> behaviorIds = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public Array<ProjectileBehaviorDefinition> behaviors = new Array<ProjectileBehaviorDefinition>();

	private TowerDefenseProjectileConfig _cachedConfig;

	private long _cachedRegistrationRevision = -1L;

	[Export(PropertyHint.None, "")]
	public StringName skinName
	{
		get
		{
			return _skinName;
		}
		set
		{
			if (!(_skinName == value))
			{
				_skinName = value;
				_cachedConfig = null;
			}
		}
	}

	public TowerDefenseProjectileCreateData()
		: this(null)
	{
	}

	public TowerDefenseProjectileCreateData(StringName _projectileName)
	{
		projectileName = _projectileName;
	}

	public void InvalidateConfigCache()
	{
		_cachedConfig = null;
		_cachedRegistrationRevision = -1L;
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		array.Add(new Dictionary
		{
			["name"] = "Flag/Damage",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.PROJECTILE_DAMAGE_FLAG>()),
			["usage"] = num
		});
		array.Add(new Dictionary
		{
			["name"] = "Flag/FireMethod",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.PROJECTILE_FIRE_METHOD_FLAG>()),
			["usage"] = num
		});
		array.Add(new Dictionary
		{
			["name"] = "Flag/Collision",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>()),
			["usage"] = num
		});
		array.Add(new Dictionary
		{
			["name"] = "Override/Hit Chests Scale",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideHitChestsScale)
		{
			array.Add(new Dictionary
			{
				["name"] = "HitChests/Scale",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Hit Nut Scale",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideHitNutScale)
		{
			array.Add(new Dictionary
			{
				["name"] = "HitNut/Scale",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Hit Frozen Scale",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideHitFrozenScale)
		{
			array.Add(new Dictionary
			{
				["name"] = "HitFrozen/Scale",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Hit Machine Scale",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideHitMachineScale)
		{
			array.Add(new Dictionary
			{
				["name"] = "HitMachine/Scale",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Catapult Height",
			["type"] = 1,
			["usage"] = num
		});
		bool flag = (fireMethodFlags & 2) != 0;
		if (overrideCatapultHeight && flag)
		{
			array.Add(new Dictionary
			{
				["name"] = "Catapult/Height",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Penetrate Num",
			["type"] = 1,
			["usage"] = num
		});
		bool flag2 = (fireMethodFlags & 4) != 0;
		if (overridePenetrateNum && flag2)
		{
			array.Add(new Dictionary
			{
				["name"] = "Penetrate/Num",
				["type"] = 2,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Penetrate Over Back",
			["type"] = 1,
			["usage"] = num
		});
		bool flag3 = (fireMethodFlags & 8) != 0;
		if (overridePenetrateOverBack && flag2 && flag3)
		{
			array.Add(new Dictionary
			{
				["name"] = "Penetrate/OverBack",
				["type"] = 1,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Back Out Ground",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideBackOutGround && flag3)
		{
			array.Add(new Dictionary
			{
				["name"] = "Back/OutOfGround",
				["type"] = 1,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Back Duration",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideBackDuration && flag3)
		{
			array.Add(new Dictionary
			{
				["name"] = "Back/Duration",
				["type"] = 3,
				["usage"] = num
			});
		}
		array.Add(new Dictionary
		{
			["name"] = "Override/Rotate Follow Velocity",
			["type"] = 1,
			["usage"] = num
		});
		if (overrideRotateFollowVelocity)
		{
			array.Add(new Dictionary
			{
				["name"] = "Rotate/FollowVelocity",
				["type"] = 1,
				["usage"] = num
			});
		}
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		switch (property.ToString())
		{
		case "methods":
			behaviors = ProjectileBehaviorDefinition.ReadLegacyArray(value);
			InvalidateConfigCache();
			return true;
		case "Flag/Damage":
			damageFlags = value.AsInt32();
			return true;
		case "Flag/FireMethod":
			fireMethodFlags = value.AsInt32();
			NotifyPropertyListChanged();
			return true;
		case "Flag/Collision":
			collisionFlags = value.AsInt32();
			return true;
		case "Override/Hit Chests Scale":
			overrideHitChestsScale = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Hit Nut Scale":
			overrideHitNutScale = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Hit Frozen Scale":
			overrideHitFrozenScale = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Hit Machine Scale":
			overrideHitMachineScale = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Catapult Height":
			overrideCatapultHeight = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Penetrate Num":
			overridePenetrateNum = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Penetrate Over Back":
			overridePenetrateOverBack = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Back Out Ground":
			overrideBackOutGround = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Back Duration":
			overrideBackDuration = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "Override/Rotate Follow Velocity":
			overrideRotateFollowVelocity = value.AsBool();
			NotifyPropertyListChanged();
			return true;
		case "HitChests/Scale":
			hitChestsScale = value.AsDouble();
			return true;
		case "HitNut/Scale":
			hitNutScale = value.AsDouble();
			return true;
		case "HitFrozen/Scale":
			hitFrozenScale = value.AsDouble();
			return true;
		case "HitMachine/Scale":
			hitMachineScale = value.AsDouble();
			return true;
		case "Catapult/Height":
			catapultHeight = value.AsDouble();
			return true;
		case "Penetrate/Num":
			penetrateNum = value.AsInt32();
			return true;
		case "Penetrate/OverBack":
			penetrateOverBack = value.AsBool();
			return true;
		case "Back/OutOfGround":
			backOutGround = value.AsBool();
			return true;
		case "Back/Duration":
			backDuration = value.AsDouble();
			return true;
		case "Rotate/FollowVelocity":
			rotateFollowVelocity = value.AsBool();
			return true;
		default:
			return false;
		}
	}

	public override Variant _Get(StringName property)
	{
		return property.ToString() switch
		{
			"Flag/Damage" => damageFlags, 
			"Flag/FireMethod" => fireMethodFlags, 
			"Flag/Collision" => collisionFlags, 
			"Override/Hit Chests Scale" => overrideHitChestsScale, 
			"Override/Hit Nut Scale" => overrideHitNutScale, 
			"Override/Hit Frozen Scale" => overrideHitFrozenScale, 
			"Override/Hit Machine Scale" => overrideHitMachineScale, 
			"Override/Catapult Height" => overrideCatapultHeight, 
			"Override/Penetrate Num" => overridePenetrateNum, 
			"Override/Penetrate Over Back" => overridePenetrateOverBack, 
			"Override/Back Out Ground" => overrideBackOutGround, 
			"Override/Back Duration" => overrideBackDuration, 
			"Override/Rotate Follow Velocity" => overrideRotateFollowVelocity, 
			"HitChests/Scale" => hitChestsScale, 
			"HitNut/Scale" => hitNutScale, 
			"HitFrozen/Scale" => hitFrozenScale, 
			"HitMachine/Scale" => hitMachineScale, 
			"Catapult/Height" => catapultHeight, 
			"Penetrate/Num" => penetrateNum, 
			"Penetrate/OverBack" => penetrateOverBack, 
			"Back/OutOfGround" => backOutGround, 
			"Back/Duration" => backDuration, 
			"Rotate/FollowVelocity" => rotateFollowVelocity, 
			_ => default(Variant), 
		};
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		switch (property.ToString())
		{
		case "Catapult/Height":
		case "Flag/FireMethod":
		case "HitChests/Scale":
		case "HitFrozen/Scale":
		case "Override/Hit Chests Scale":
		case "Override/Hit Frozen Scale":
		case "Override/Back Duration":
		case "Override/Hit Nut Scale":
		case "Override/Penetrate Num":
		case "Override/Back Out Ground":
		case "Override/Catapult Height":
		case "Back/OutOfGround":
		case "HitMachine/Scale":
		case "Back/Duration":
		case "Penetrate/Num":
		case "Flag/Damage":
		case "Flag/Collision":
		case "Override/Hit Machine Scale":
		case "Override/Penetrate Over Back":
		case "Override/Rotate Follow Velocity":
		case "HitNut/Scale":
		case "Penetrate/OverBack":
		case "Rotate/FollowVelocity":
			return true;
		default:
			return false;
		}
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		return property.ToString() switch
		{
			"Flag/Damage" => 3, 
			"Flag/FireMethod" => 1, 
			"Flag/Collision" => 1, 
			"Override/Hit Chests Scale" => false, 
			"Override/Hit Nut Scale" => false, 
			"Override/Hit Frozen Scale" => false, 
			"Override/Hit Machine Scale" => false, 
			"Override/Catapult Height" => false, 
			"Override/Penetrate Num" => false, 
			"Override/Penetrate Over Back" => false, 
			"Override/Back Out Ground" => false, 
			"Override/Back Duration" => false, 
			"Override/Rotate Follow Velocity" => false, 
			"HitChests/Scale" => 1.0, 
			"HitNut/Scale" => 1.0, 
			"HitFrozen/Scale" => 1.0, 
			"HitMachine/Scale" => 1.0, 
			"Catapult/Height" => 400.0, 
			"Penetrate/Num" => 3, 
			"Penetrate/OverBack" => true, 
			"Back/OutOfGround" => true, 
			"Back/Duration" => 1.0, 
			"Rotate/FollowVelocity" => false, 
			_ => default(Variant), 
		};
	}

	public void SetbaseDamage(double _baseDamage)
	{
		baseDamage = _baseDamage;
	}

	public void SetSize(Vector2 _size)
	{
		size = _size;
	}

	public void SetScale(Vector2 _scale)
	{
		scale = _scale;
	}

	public void SetHitChestsScale(double _hitChestsScale)
	{
		hitChestsScale = _hitChestsScale;
	}

	public void SetHitNutScale(double _hitNutScale)
	{
		hitNutScale = _hitNutScale;
	}

	public void SetHitFrozenScale(double _hitFrozenScale)
	{
		hitFrozenScale = _hitFrozenScale;
	}

	public void SetHitMachineScale(double _hitMachineScale)
	{
		hitMachineScale = _hitMachineScale;
	}

	public void SetDamageFlags(int _damageFlags)
	{
		damageFlags = _damageFlags;
	}

	public void SetCollisionFlags(int _collisionFlags)
	{
		collisionFlags = _collisionFlags;
	}

	public void SetFireMethodFlags(int _fireMethodFlags)
	{
		fireMethodFlags = _fireMethodFlags;
	}

	public void SetCatapultHeight(double _catapultHeight)
	{
		catapultHeight = _catapultHeight;
	}

	public void SetPenetrateNum(int _penetrateNum)
	{
		penetrateNum = _penetrateNum;
	}

	public void SetPenetrateOverBack(bool _penetrateOverBack)
	{
		penetrateOverBack = _penetrateOverBack;
	}

	public void SetBackOutGround(bool _backOutGround)
	{
		backOutGround = _backOutGround;
	}

	public void SetBackDuration(double _backDuration)
	{
		backDuration = _backDuration;
	}

	public void SetRotateFollowVelocity(bool _rotateFollowVelocity)
	{
		rotateFollowVelocity = _rotateFollowVelocity;
	}

	public void SetRangeOverride(bool _rangeOverride)
	{
		rangeOverride = _rangeOverride;
	}

	public void SetRangeType(string _rangeType)
	{
		rangeType = _rangeType;
	}

	public void SetUseRange(bool _useRange)
	{
		useRange = _useRange;
	}

	public void SetRangeSize(Vector2 _rangeSize)
	{
		rangeSize = _rangeSize;
	}

	public void SetHitPesontage(double _hitPesontage)
	{
		hitPesontage = _hitPesontage;
	}

	public void SetOverrideHitTargetEvent(bool _overrideHitTargetEvent)
	{
		overrideHitTargetEvent = _overrideHitTargetEvent;
	}

	public void SetOverrideHitCharacterEvent(bool _overrideHitCharacterEvent)
	{
		overrideHitCharacterEvent = _overrideHitCharacterEvent;
	}

	public void SetOverrideHitGroundEvent(bool _overrideHitGroundEvent)
	{
		overrideHitGroundEvent = _overrideHitGroundEvent;
	}

	public TowerDefenseProjectileConfig BuildConfig()
	{
		long registrationRevision = TowerDefenseProjectileRegistry.RegistrationRevision;
		if (_cachedConfig != null && !Engine.IsEditorHint() && _cachedRegistrationRevision == registrationRevision)
		{
			return _cachedConfig;
		}
		if (projectileName == null)
		{
			return null;
		}
		TowerDefenseProjectileData projectile = TowerDefenseProjectileRegistry.GetProjectile(projectileName);
		if (projectile != null)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileConfig();
			towerDefenseProjectileConfig.damageFlags = damageFlags;
			towerDefenseProjectileConfig.collisionFlags = collisionFlags;
			towerDefenseProjectileConfig.fireMethodFlags = fireMethodFlags;
			if (projectile.isFire)
			{
				towerDefenseProjectileConfig.damageFlags |= 4;
			}
			if (projectile.isMagic)
			{
				towerDefenseProjectileConfig.damageFlags |= 64;
			}
			towerDefenseProjectileConfig.name = projectile.name;
			towerDefenseProjectileConfig.isStar = projectile.isStar;
			towerDefenseProjectileConfig.skinName = skinName;
			towerDefenseProjectileConfig.size = projectile.size;
			towerDefenseProjectileConfig.scale = projectile.scale;
			towerDefenseProjectileConfig.projectileScene = projectile.projectileScene;
			towerDefenseProjectileConfig.splatSceneType = projectile.splatSceneType;
			towerDefenseProjectileConfig.splatAudio = projectile.splatAudio;
			towerDefenseProjectileConfig.splatScene = projectile.splatScene;
			towerDefenseProjectileConfig.hitEffect = projectile.hitEffect;
			towerDefenseProjectileConfig.hitTargetEventList = projectile.hitTargetEventList;
			towerDefenseProjectileConfig.hitCharacterEventList = projectile.hitCharacterEventList;
			towerDefenseProjectileConfig.hitGroundEventList = projectile.hitGroundEventList;
			towerDefenseProjectileConfig.blockHurt = projectile.blockHurt;
			towerDefenseProjectileConfig.rotateFollowVelocity = projectile.rotateFollowVelocity;
			towerDefenseProjectileConfig.rotateScale = projectile.rotateScale;
			towerDefenseProjectileConfig.hitBody = projectile.hitBody;
			towerDefenseProjectileConfig.rangeType = projectile.rangeType;
			towerDefenseProjectileConfig.useRange = projectile.useRange;
			towerDefenseProjectileConfig.rangeSize = projectile.rangeSize;
			towerDefenseProjectileConfig.hitPesontage = projectile.hitPesontage;
			towerDefenseProjectileConfig.baseDamage = projectile.baseDamage;
			TowerDefenseProjectileConfig projectileConfig = TowerDefenseManager.GetProjectileConfig(projectileName.ToString());
			if (projectileConfig != null)
			{
				towerDefenseProjectileConfig.penetrateNum = projectileConfig.penetrateNum;
				towerDefenseProjectileConfig.penetrateOverBack = projectileConfig.penetrateOverBack;
				towerDefenseProjectileConfig.backOutGround = projectileConfig.backOutGround;
				towerDefenseProjectileConfig.backDuration = projectileConfig.backDuration;
				towerDefenseProjectileConfig.catapultHeight = projectileConfig.catapultHeight;
				towerDefenseProjectileConfig.splatSceneType = projectileConfig.splatSceneType;
				towerDefenseProjectileConfig.hitChestsScale = projectileConfig.hitChestsScale;
				towerDefenseProjectileConfig.hitNutScale = projectileConfig.hitNutScale;
				towerDefenseProjectileConfig.hitFrozenScale = projectileConfig.hitFrozenScale;
				towerDefenseProjectileConfig.hitMachineScale = projectileConfig.hitMachineScale;
				towerDefenseProjectileConfig.trackSearchInterval = projectileConfig.trackSearchInterval;
				towerDefenseProjectileConfig.useDurabilityBlockingSweep = projectileConfig.useDurabilityBlockingSweep;
				towerDefenseProjectileConfig.durabilityBlockingThreshold = projectileConfig.durabilityBlockingThreshold;
				if (towerDefenseProjectileConfig.projectileScene == null)
				{
					towerDefenseProjectileConfig.projectileScene = projectileConfig.projectileScene;
				}
				if (towerDefenseProjectileConfig.splatScene == null)
				{
					towerDefenseProjectileConfig.splatScene = projectileConfig.splatScene;
				}
				penetrateNum = projectileConfig.penetrateNum;
				penetrateOverBack = projectileConfig.penetrateOverBack;
				backOutGround = projectileConfig.backOutGround;
				backDuration = projectileConfig.backDuration;
				catapultHeight = projectileConfig.catapultHeight;
				hitChestsScale = projectileConfig.hitChestsScale;
				hitNutScale = projectileConfig.hitNutScale;
				hitFrozenScale = projectileConfig.hitFrozenScale;
				hitMachineScale = projectileConfig.hitMachineScale;
				rotateFollowVelocity = projectileConfig.rotateFollowVelocity;
			}
			if (TowerDefenseProjectileRegistry.HasProjectileSkin(projectileName, skinName))
			{
				PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(projectileName, skinName);
				if (projectileSkinProjectileScene != null)
				{
					towerDefenseProjectileConfig.projectileScene = projectileSkinProjectileScene;
				}
				PackedScene projectileSkinSplatScene = TowerDefenseProjectileRegistry.GetProjectileSkinSplatScene(projectileName, skinName);
				if (projectileSkinSplatScene != null)
				{
					towerDefenseProjectileConfig.splatScene = projectileSkinSplatScene;
				}
			}
			ApplyOverride(towerDefenseProjectileConfig);
			_cachedConfig = towerDefenseProjectileConfig;
			_cachedRegistrationRevision = registrationRevision;
			return towerDefenseProjectileConfig;
		}
		TowerDefenseProjectileConfig projectileConfig2 = TowerDefenseManager.GetProjectileConfig(projectileName.ToString());
		if (projectileConfig2 != null)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = (TowerDefenseProjectileConfig)projectileConfig2.Duplicate(deep: true);
			if (towerDefenseProjectileConfig2 != null)
			{
				ApplyOverride(towerDefenseProjectileConfig2);
				_cachedConfig = towerDefenseProjectileConfig2;
				_cachedRegistrationRevision = registrationRevision;
				return towerDefenseProjectileConfig2;
			}
		}
		return null;
	}

	public void ApplyOverride(TowerDefenseProjectileConfig config)
	{
		if (baseDamage >= 0.0)
		{
			config.baseDamage = baseDamage;
		}
		config.size = size;
		config.scale = scale;
		if (overrideHitChestsScale)
		{
			config.hitChestsScale = hitChestsScale;
		}
		if (overrideHitNutScale)
		{
			config.hitNutScale = hitNutScale;
		}
		if (overrideHitFrozenScale)
		{
			config.hitFrozenScale = hitFrozenScale;
		}
		if (overrideHitMachineScale)
		{
			config.hitMachineScale = hitMachineScale;
		}
		if (overrideCatapultHeight)
		{
			config.catapultHeight = catapultHeight;
		}
		if (overridePenetrateNum)
		{
			config.penetrateNum = penetrateNum;
		}
		if (overridePenetrateOverBack)
		{
			config.penetrateOverBack = penetrateOverBack;
		}
		if (overrideBackOutGround)
		{
			config.backOutGround = backOutGround;
		}
		if (overrideBackDuration)
		{
			config.backDuration = backDuration;
		}
		if (overrideRotateFollowVelocity)
		{
			config.rotateFollowVelocity = rotateFollowVelocity;
		}
		if (behaviors.Count > 0)
		{
			config.behaviors = behaviors;
		}
		if (behaviorIds.Count > 0)
		{
			config.behaviorIds = behaviorIds;
		}
		if (rangeOverride)
		{
			config.rangeType = rangeType;
			config.useRange = useRange;
			config.rangeSize = rangeSize;
			config.hitPesontage = hitPesontage;
		}
		if (overrideHitTargetEvent)
		{
			config.hitTargetEventList = hitTargetEventList;
		}
		if (overrideHitCharacterEvent)
		{
			config.hitCharacterEventList = hitCharacterEventList;
		}
		if (overrideHitGroundEvent)
		{
			config.hitGroundEventList = hitGroundEventList;
		}
	}
}
