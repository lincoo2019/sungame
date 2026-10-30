using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelPacketConfig : Resource
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

	public void Init(Variant data)
	{
		if (data.VariantType == Variant.Type.String)
		{
			packetName = data.AsString();
		}
		if (data.VariantType == Variant.Type.Dictionary)
		{
			Dictionary dictionary = data.AsGodotDictionary();
			packetName = dictionary.GetValueOrDefault("PacketName", "").AsString();
			if (dictionary.ContainsKey("Override"))
			{
				overrideVal = new TowerDefensePacketOverride();
				overrideVal.Init(dictionary.GetValueOrDefault("Override", new Dictionary()).AsGodotDictionary());
			}
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
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetName);
			packetConfig._override = overrideVal.Duplicate(deep: true) as TowerDefensePacketOverride;
			return packetConfig;
		}
		return TowerDefenseManager.GetPacketConfigReadOnly(packetName);
	}
}
