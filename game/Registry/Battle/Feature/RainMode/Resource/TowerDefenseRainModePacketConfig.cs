using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseRainModePacketConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string name = "";

	[Export(PropertyHint.None, "")]
	public int weight = 10;

	[Export(PropertyHint.None, "")]
	public int maxNum = -1;

	[Export(PropertyHint.None, "")]
	public double maxMagnification = 0.1;

	[Export(PropertyHint.None, "")]
	public int minNum = -1;

	[Export(PropertyHint.None, "")]
	public double minMagnification = 2.0;

	public TowerDefensePacketOverride overrideVal;

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketOverride @override
	{
		get
		{
			return overrideVal;
		}
		set
		{
			overrideVal = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "overrideVal")
		{
			overrideVal = value.As<TowerDefensePacketOverride>();
			return true;
		}
		return false;
	}

	public void Init(Dictionary data)
	{
		name = data.GetValueOrDefault("Name", "").AsString();
		weight = data.GetValueOrDefault("Weight", 0).AsInt32();
		maxNum = data.GetValueOrDefault("MaxNum", -1).AsInt32();
		maxMagnification = data.GetValueOrDefault("MaxMagnification", 0).AsDouble();
		minNum = data.GetValueOrDefault("MinNum", -1).AsInt32();
		minMagnification = data.GetValueOrDefault("MinMagnification", 0).AsDouble();
		if (data.ContainsKey("Override"))
		{
			overrideVal = new TowerDefensePacketOverride();
			overrideVal.Init(data.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary());
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["Name"] = name,
			["Weight"] = weight,
			["MaxNum"] = maxNum,
			["MaxMagnification"] = maxMagnification,
			["MinNum"] = minNum,
			["MinMagnification"] = minMagnification
		};
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			dictionary["Override"] = overrideVal.Export();
		}
		return dictionary;
	}

	public TowerDefensePacketConfig GetPacket()
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(name);
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			packetConfig._override = overrideVal.Duplicate(deep: true) as TowerDefensePacketOverride;
		}
		return packetConfig;
	}
}
