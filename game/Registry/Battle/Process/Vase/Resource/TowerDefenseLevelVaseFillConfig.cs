using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelVaseFillConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

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
		packetName = data.GetValueOrDefault("PacketName", "").AsString();
		if (data.ContainsKey("Override"))
		{
			overrideVal = new TowerDefensePacketOverride();
			overrideVal.Init(data.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary());
		}
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary { ["PacketName"] = packetName };
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			dictionary["Override"] = overrideVal.Export();
		}
		return dictionary;
	}

	public TowerDefensePacketConfig GetPacket()
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			packetConfig._override = overrideVal.Duplicate(deep: true) as TowerDefensePacketOverride;
		}
		return packetConfig;
	}
}
