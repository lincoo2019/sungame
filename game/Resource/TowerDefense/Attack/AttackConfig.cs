using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AttackConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public double num = 20.0;

	[Export(PropertyHint.None, "")]
	public double attackScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double armorAttackScale = 1.0;

	private int _damageFlags = 3;

	private int _collisionFlags = 9;

	[Export(PropertyHint.None, "")]
	public int damageFlags
	{
		get
		{
			return _damageFlags;
		}
		set
		{
			_damageFlags = value;
		}
	}

	[Export(PropertyHint.None, "")]
	public int collisionFlags
	{
		get
		{
			return _collisionFlags;
		}
		set
		{
			_collisionFlags = value;
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		return new Array<Dictionary>
		{
			new Dictionary
			{
				["name"] = "Flag/Damage",
				["type"] = 2,
				["hint"] = 6,
				["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.PROJECTILE_DAMAGE_FLAG>()),
				["usage"] = num
			},
			new Dictionary
			{
				["name"] = "Flag/Collision",
				["type"] = 2,
				["hint"] = 6,
				["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>()),
				["usage"] = num
			}
		};
	}

	public override bool _Set(StringName property, Variant value)
	{
		string text = property.ToString();
		if (!(text == "Flag/Damage"))
		{
			if (text == "Flag/Collision")
			{
				_collisionFlags = value.AsInt32();
				return true;
			}
			return false;
		}
		_damageFlags = value.AsInt32();
		return true;
	}

	public override Variant _Get(StringName property)
	{
		string text = property.ToString();
		if (!(text == "Flag/Damage"))
		{
			if (text == "Flag/Collision")
			{
				return _collisionFlags;
			}
			return default(Variant);
		}
		return _damageFlags;
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		string text = property.ToString();
		if (text == "Flag/Damage" || text == "Flag/Collision")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		string text = property.ToString();
		if (!(text == "Flag/Damage"))
		{
			if (text == "Flag/Collision")
			{
				return 9;
			}
			return default(Variant);
		}
		return 3;
	}
}
