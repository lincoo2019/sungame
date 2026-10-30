using System;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseCellConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public Vector4I pos = Vector4I.Zero;

	[Export(PropertyHint.None, "")]
	public CurveTexture groundHeightCurve;

	[ExportCategory("Setting")]
	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseEnum.PLANTGRIDTYPE> gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
	{
		TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
		TowerDefenseEnum.PLANTGRIDTYPE.AIR
	};

	private int _elementFlags;

	[Export(PropertyHint.None, "")]
	public int ElementFlags
	{
		get
		{
			return _elementFlags;
		}
		set
		{
			_elementFlags = value;
		}
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		long num = 4L;
		return new Array<Dictionary>
		{
			new Dictionary
			{
				["name"] = "Flag/Elemet",
				["type"] = 2,
				["hint"] = 6,
				["hint_string"] = string.Join(",", Enum.GetNames<TowerDefenseEnum.ELEMENT_SYSTEM>()),
				["usage"] = num
			}
		};
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "Flag/Elemet")
		{
			_elementFlags = value.AsInt32();
			NotifyPropertyListChanged();
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property.ToString() == "Flag/Elemet")
		{
			return _elementFlags;
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (property.ToString() == "Flag/Elemet")
		{
			return true;
		}
		return false;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (property.ToString() == "Flag/Elemet")
		{
			return 0;
		}
		return default(Variant);
	}
}
