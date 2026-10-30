using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseCharacterEventHurt : TowerDefenseCharacterEventBase
{
	[Export(PropertyHint.None, "")]
	public double num = 20.0;

	[Export(PropertyHint.None, "")]
	public bool playSplatAudio = true;

	[Export(PropertyHint.None, "")]
	public int damageFlags = 3;

	[Export(PropertyHint.None, "")]
	public int collisionFlags = 9;

	public override void Execute(Vector2 pos, TowerDefenseCharacter target)
	{
		Run(pos, target, num, damageFlags, playSplatAudio);
	}

	public override void ExecuteDps(Vector2 pos, TowerDefenseCharacter target, double delta)
	{
		Run(pos, target, num * delta, damageFlags, playSplatAudio);
	}

	public override void ExecuteProject(TowerDefenseProjectile projectile, TowerDefenseCharacter target)
	{
		Run(projectile.GlobalPosition, target, num, damageFlags, playSplatAudio);
	}

	public static void Run(Vector2 pos, TowerDefenseCharacter target, double num = 20.0, int damageFlags = 3, bool playSplatAudio = true)
	{
		target.FlagHurt(num, damageFlags, playSplatAudio);
		target.AttackDeal(null, "Default", num);
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
				collisionFlags = value.AsInt32();
				return true;
			}
			return false;
		}
		damageFlags = value.AsInt32();
		return true;
	}

	public override Variant _Get(StringName property)
	{
		string text = property.ToString();
		if (!(text == "Flag/Damage"))
		{
			if (text == "Flag/Collision")
			{
				return Variant.From(in collisionFlags);
			}
			return default(Variant);
		}
		return Variant.From(in damageFlags);
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
				return Variant.From<int>(9);
			}
			return default(Variant);
		}
		return Variant.From<int>(3);
	}
}
