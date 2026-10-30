using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseArmorTypeData : Resource
{
	[Export(PropertyHint.None, "")]
	public string armorName = "";

	[Export(PropertyHint.None, "")]
	public double damagePoint = 370.0;

	private Array<double> _stagePersontage = new Array<double>();

	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.CHARACTER_HEIGHT height = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;

	[Export(PropertyHint.File, "*.png,*.webp,*.jpg,*.jpeg,*.svg,*.bmp,*.tga")]
	public Array<string> stageAnimeTexturePaths = new Array<string>();

	[Export(PropertyHint.None, "")]
	public string impactAudio = "";

	[Export(PropertyHint.None, "")]
	public string damageAudio = "";

	[ExportCategory("Behavior")]
	[Export(PropertyHint.None, "")]
	public Array<StringName> behaviorIds = new Array<StringName>();

	[Export(PropertyHint.None, "")]
	public Array<ArmorBehaviorDefinition> behaviors = new Array<ArmorBehaviorDefinition>();

	[ExportGroup("Method", "")]
	[Export(PropertyHint.None, "")]
	public double limitMaxHit = -1.0;

	[Export(PropertyHint.None, "")]
	public double explodePersontage = 1.0;

	[ExportGroup("", "")]
	[Export(PropertyHint.None, "")]
	public int armorMethodFlags = 200;

	[Export(PropertyHint.None, "")]
	public Array<double> stagePersontage
	{
		get
		{
			return _stagePersontage;
		}
		set
		{
			_stagePersontage = value;
			List<double> list = _stagePersontage.ToList();
			list.Sort((double a, double b) => b.CompareTo(a));
			_stagePersontage = new Array<double>(list);
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		return new Array<Dictionary>
		{
			new Dictionary
			{
				["name"] = "Flag/Method",
				["type"] = 2,
				["hint"] = 6,
				["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.ARMOR_METHOD_FLAGS>()),
				["usage"] = num
			}
		};
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property == (StringName)"Flag/Method")
		{
			armorMethodFlags = value.AsInt32();
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property == (StringName)"Flag/Method")
		{
			return armorMethodFlags;
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (property == (StringName)"Flag/Method")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (property == (StringName)"Flag/Method")
		{
			return 200;
		}
		return default(Variant);
	}
}
