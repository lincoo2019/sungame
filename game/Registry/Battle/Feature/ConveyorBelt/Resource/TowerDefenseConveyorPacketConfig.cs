using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseConveyorPacketConfig : Resource
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

	public TowerDefensePacketOverride _override;

	[Export(PropertyHint.None, "")]
	public TowerDefensePacketOverride @override
	{
		get
		{
			return _override;
		}
		set
		{
			_override = value;
		}
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property.ToString() == "_override")
		{
			_override = value.As<TowerDefensePacketOverride>();
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
			_override = new TowerDefensePacketOverride();
			_override.Init(data.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary());
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
		if (GodotObject.IsInstanceValid(_override))
		{
			dictionary["Override"] = _override.Export();
		}
		return dictionary;
	}

	public TowerDefensePacketConfig GetPacket()
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(name);
		if (GodotObject.IsInstanceValid(_override))
		{
			packetConfig._override = _override.Duplicate(deep: true) as TowerDefensePacketOverride;
		}
		return packetConfig;
	}
}
