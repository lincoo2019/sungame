using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseCharacterConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string name = "";

	[Export(PropertyHint.None, "")]
	public double hitpointsNearDeath;

	[Export(PropertyHint.None, "")]
	public double hitpoints = 300.0;

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

	[Export(PropertyHint.None, "")]
	public bool canDragIntoWater = true;

	[Export(PropertyHint.None, "")]
	public bool canImitate = true;

	[Export(PropertyHint.None, "")]
	public bool canCopy = true;

	[Export(PropertyHint.None, "")]
	public bool warnningLineFliter;

	[Export(PropertyHint.Enum, "Never,Night,Day")]
	public string sleepTime = "Never";

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_HEIGHT height = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;

	[Export(PropertyHint.None, "")]
	public CharacterDamagePointData damagePointData;

	[Export(PropertyHint.None, "")]
	public CharacterArmorData armorData;

	[Export(PropertyHint.None, "")]
	public CharacterCustomData customData;

	[Export(PropertyHint.None, "")]
	public PackedScene ashScene;

	[ExportCategory("packet")]
	[Export(PropertyHint.None, "")]
	public GeneralEnum.HOMEWORLD homeWorld;

	[Export(PropertyHint.None, "")]
	public int costRise = -1;

	[Export(PropertyHint.None, "")]
	public int cost = 100;

	[Export(PropertyHint.None, "")]
	public int costNight = -1;

	[Export(PropertyHint.None, "")]
	public double costMultiple = -1.0;

	[Export(PropertyHint.None, "")]
	public double packetCooldown = 5.0;

	[Export(PropertyHint.None, "")]
	public double startingCooldown;

	[Export(PropertyHint.None, "")]
	public bool plantCoverAll;

	[Export(PropertyHint.None, "")]
	public bool plantCoverSelf;

	[Export(PropertyHint.None, "")]
	public Array<string> plantCover = new Array<string>();

	[Export(PropertyHint.None, "")]
	public Array<int> plantCoverRecycle = new Array<int>();

	[Export(PropertyHint.None, "")]
	public bool plantCanHasSurround = true;

	[Export(PropertyHint.None, "")]
	public bool plantSurroundCanPlantWater;

	[Export(PropertyHint.None, "")]
	public bool plantSurroundCanHasSlot = true;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseEnum.PLANTGRIDTYPE> plantGridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
	{
		TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
		TowerDefenseEnum.PLANTGRIDTYPE.POT,
		TowerDefenseEnum.PLANTGRIDTYPE.LILYPAD
	};

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.PLANTGRIDTYPE plantGridOverrideType;

	[Export(PropertyHint.None, "")]
	public int collisionFlags = 1;

	[Export(PropertyHint.None, "")]
	public int maskFlags = 1;

	[Export(PropertyHint.None, "")]
	public int unUseBuffFlags;

	[Export(PropertyHint.None, "")]
	public int physiqueTypeFlags;

	[Export(PropertyHint.None, "")]
	public int elementFlags;

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		Array<Dictionary> array = new Array<Dictionary>();
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
			["name"] = "Flag/Mask",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.CHARACTER_COLLISION_FLAGS>()),
			["usage"] = num
		});
		string text = "";
		string[] names = Enum.GetNames<TowerDefenseEnum.CHARACTER_BUFF_FLAGS>();
		TowerDefenseEnum.CHARACTER_BUFF_FLAGS[] values = Enum.GetValues<TowerDefenseEnum.CHARACTER_BUFF_FLAGS>();
		for (int i = 0; i < names.Length; i++)
		{
			string value = names[i];
			int value2 = (int)values.GetValue(i);
			text += $"{value}:{value2}";
			if (i != names.Length - 1)
			{
				text += ",";
			}
		}
		array.Add(new Dictionary
		{
			["name"] = "Flag/UnUseBuff",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = text,
			["usage"] = num
		});
		array.Add(new Dictionary
		{
			["name"] = "Flag/PhysiqueType",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.CHARACTER_PHYSIQUE_TYPE>()),
			["usage"] = num
		});
		array.Add(new Dictionary
		{
			["name"] = "Flag/Elemet",
			["type"] = 2,
			["hint"] = 6,
			["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.ELEMENT_SYSTEM>()),
			["usage"] = num
		});
		return array;
	}

	public override bool _Set(StringName property, Variant value)
	{
		switch (property.ToString())
		{
		case "Flag/Collision":
			collisionFlags = value.AsInt32();
			return true;
		case "Flag/Mask":
			maskFlags = value.AsInt32();
			return true;
		case "Flag/UnUseBuff":
			unUseBuffFlags = value.AsInt32();
			return true;
		case "Flag/PhysiqueType":
			physiqueTypeFlags = value.AsInt32();
			return true;
		case "Flag/Elemet":
			elementFlags = value.AsInt32();
			return true;
		default:
			return false;
		}
	}

	public override Variant _Get(StringName property)
	{
		return property.ToString() switch
		{
			"Flag/Collision" => Variant.From(in collisionFlags), 
			"Flag/Mask" => Variant.From(in maskFlags), 
			"Flag/UnUseBuff" => Variant.From(in unUseBuffFlags), 
			"Flag/PhysiqueType" => Variant.From(in physiqueTypeFlags), 
			"Flag/Elemet" => Variant.From(in elementFlags), 
			_ => default(Variant), 
		};
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		switch (property.ToString())
		{
		case "Flag/Collision":
		case "Flag/Mask":
		case "Flag/UnUseBuff":
		case "Flag/PhysiqueType":
		case "Flag/Elemet":
			return true;
		default:
			return false;
		}
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		return property.ToString() switch
		{
			"Flag/Collision" => Variant.From<int>(1), 
			"Flag/Mask" => Variant.From<int>(1), 
			"Flag/UnUseBuff" => Variant.From<int>(0), 
			"Flag/PhysiqueType" => Variant.From<int>(0), 
			"Flag/Elemet" => Variant.From<int>(0), 
			_ => default(Variant), 
		};
	}
}
