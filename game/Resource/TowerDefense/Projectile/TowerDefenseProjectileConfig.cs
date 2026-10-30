using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseProjectileConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string name = "";

	[Export(PropertyHint.None, "")]
	public bool isStar;

	private StringName _nameSN;

	[Export(PropertyHint.None, "")]
	public StringName skinName = new StringName("Default");

	[Export(PropertyHint.None, "")]
	public Vector2 size = new Vector2(28f, 28f);

	[Export(PropertyHint.None, "")]
	public Vector2 scale = new Vector2(1f, 1f);

	[Export(PropertyHint.None, "")]
	public double baseDamage = 20.0;

	[Export(PropertyHint.None, "")]
	public PackedScene projectileScene;

	[Export(PropertyHint.None, "")]
	public ObjectManagerConfig.OBJECT projectileObject;

	[Export(PropertyHint.Enum, "Particles,Sprite")]
	public string splatSceneType = "Particles";

	[Export(PropertyHint.None, "")]
	public string splatAudio = "SplatNormal";

	[Export(PropertyHint.None, "")]
	public PackedScene splatScene;

	[Export(PropertyHint.None, "")]
	public PackedScene hitEffect;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitTargetEventList = new Array<TowerDefenseCharacterEventBase>();

	private Array<TowerDefenseCharacterEventBase> _hitCharacterEventList = new Array<TowerDefenseCharacterEventBase>();

	private TowerDefenseCharacterEventBase[] _hitCharacterEvents = System.Array.Empty<TowerDefenseCharacterEventBase>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitGroundEventList = new Array<TowerDefenseCharacterEventBase>();

	[ExportGroup("Init", "")]
	[Export(PropertyHint.None, "")]
	public double hitChestsScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitNutScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitFrozenScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double hitMachineScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double blockHurt = -1.0;

	[Export(PropertyHint.None, "")]
	public bool rotateFollowVelocity;

	[Export(PropertyHint.None, "")]
	public double rotateScale;

	[Export(PropertyHint.None, "")]
	public bool hitBody;

	[ExportGroup("Range", "")]
	private string _rangeType = "Default";

	private bool _usesDefaultDamage = true;

	private bool _usesExplosionDamage;

	[Export(PropertyHint.None, "")]
	public bool useRange;

	[Export(PropertyHint.None, "")]
	public Vector2 rangeSize = new Vector2(0.5f, 0.5f);

	[Export(PropertyHint.None, "")]
	public double hitPesontage = 0.25;

	[ExportGroup("Track", "")]
	[Export(PropertyHint.None, "")]
	public int trackSearchInterval = 15;

	[ExportGroup("", "")]
	[Export(PropertyHint.None, "")]
	public int damageFlags = 3;

	[Export(PropertyHint.None, "")]
	public int fireMethodFlags = 1;

	[Export(PropertyHint.None, "")]
	public int collisionFlags = 9;

	[Export(PropertyHint.None, "")]
	public double catapultHeight = 300.0;

	[Export(PropertyHint.None, "")]
	public int penetrateNum = 3;

	[Export(PropertyHint.None, "")]
	public bool penetrateOverBack = true;

	[Export(PropertyHint.None, "")]
	public bool useDurabilityBlockingSweep;

	[Export(PropertyHint.None, "")]
	public double durabilityBlockingThreshold = 2000.0;

	[Export(PropertyHint.None, "")]
	public bool backOutGround = true;

	[Export(PropertyHint.None, "")]
	public double backDuration = 1.0;

	[ExportCategory("Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<StringName> behaviorIds = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public Array<ProjectileBehaviorDefinition> behaviors = new Array<ProjectileBehaviorDefinition>();

	public StringName NameSN => _nameSN ?? (_nameSN = new StringName(name));

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseCharacterEventBase> hitCharacterEventList
	{
		get
		{
			return _hitCharacterEventList;
		}
		set
		{
			_hitCharacterEventList = value ?? new Array<TowerDefenseCharacterEventBase>();
			RebuildHitCharacterEvents();
		}
	}

	internal TowerDefenseCharacterEventBase[] HitCharacterEvents => _hitCharacterEvents;

	[Export(PropertyHint.Enum, "Default,Bomb")]
	public string rangeType
	{
		get
		{
			return _rangeType;
		}
		set
		{
			_rangeType = value ?? "Default";
			_usesDefaultDamage = string.Equals(_rangeType, "Default", StringComparison.Ordinal);
			_usesExplosionDamage = string.Equals(_rangeType, "Bomb", StringComparison.Ordinal);
		}
	}

	internal bool UsesDefaultDamage => _usesDefaultDamage;

	internal bool UsesExplosionDamage => _usesExplosionDamage;

	private void RebuildHitCharacterEvents()
	{
		int count = _hitCharacterEventList.Count;
		if (count == 0)
		{
			_hitCharacterEvents = System.Array.Empty<TowerDefenseCharacterEventBase>();
			return;
		}
		_hitCharacterEvents = new TowerDefenseCharacterEventBase[count];
		for (int i = 0; i < count; i++)
		{
			_hitCharacterEvents[i] = _hitCharacterEventList[i];
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
		string[] names = Enum.GetNames<TowerDefenseEnum.PROJECTILE_DAMAGE_FLAG>();
		array.Add(new Dictionary
		{
			["name"] = "Flag/Damage",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", names),
			["usage"] = num
		});
		string[] names2 = Enum.GetNames<TowerDefenseEnum.PROJECTILE_FIRE_METHOD_FLAG>();
		array.Add(new Dictionary
		{
			["name"] = "Flag/FireMethod",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", names2),
			["usage"] = num
		});
		string[] names3 = Enum.GetNames<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>();
		array.Add(new Dictionary
		{
			["name"] = "Flag/Collision",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", names3),
			["usage"] = num
		});
		bool num2 = (fireMethodFlags & 2) != 0;
		bool flag = (fireMethodFlags & 4) != 0;
		bool flag2 = (fireMethodFlags & 8) != 0;
		if (num2)
		{
			array.Add(new Dictionary
			{
				["name"] = "Catapult/Height",
				["type"] = 3,
				["usage"] = num
			});
		}
		if (flag)
		{
			array.Add(new Dictionary
			{
				["name"] = "Penetrate/Num",
				["type"] = 2,
				["usage"] = num
			});
			if (flag2)
			{
				array.Add(new Dictionary
				{
					["name"] = "Penetrate/OverBack",
					["type"] = 1,
					["usage"] = num
				});
			}
		}
		if (flag2)
		{
			array.Add(new Dictionary
			{
				["name"] = "Back/OutOfGround",
				["type"] = 1,
				["usage"] = num
			});
			array.Add(new Dictionary
			{
				["name"] = "Back/Duration",
				["type"] = 3,
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
			"Catapult/Height" => catapultHeight, 
			"Penetrate/Num" => penetrateNum, 
			"Penetrate/OverBack" => penetrateOverBack, 
			"Back/OutOfGround" => backOutGround, 
			"Back/Duration" => backDuration, 
			_ => default(Variant), 
		};
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		switch (property.ToString())
		{
		case "Catapult/Height":
		case "Flag/FireMethod":
		case "Back/Duration":
		case "Penetrate/Num":
		case "Flag/Damage":
		case "Flag/Collision":
		case "Penetrate/OverBack":
		case "Back/OutOfGround":
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
			"Catapult/Height" => 400.0, 
			"Penetrate/Num" => 3, 
			"Penetrate/OverBack" => true, 
			"Back/OutOfGround" => true, 
			"Back/Duration" => 1.0, 
			_ => default(Variant), 
		};
	}

	public bool CanCollision(int maskFlags)
	{
		return (maskFlags & collisionFlags) != 0;
	}
}
