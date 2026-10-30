using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelVaseConfig : Resource
{
	[Export(PropertyHint.None, "")]
	public string packetName = "";

	public TowerDefensePacketOverride overrideVal;

	[Export(PropertyHint.None, "")]
	public Vector2I gridPos;

	[Export(PropertyHint.None, "")]
	public string type;

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
		type = data.GetValueOrDefault("Type", "Normal").AsString();
		Array array = data.GetValueOrDefault("GridPos", new Array { 0, 0 }).AsGodotArray();
		gridPos = new Vector2I(array[0].AsInt32(), array[1].AsInt32());
	}

	public Dictionary Export()
	{
		Dictionary dictionary = new Dictionary
		{
			["PacketName"] = packetName,
			["Type"] = type,
			["GridPos"] = new Array { gridPos.X, gridPos.Y }
		};
		if (GodotObject.IsInstanceValid(overrideVal))
		{
			dictionary["Override"] = overrideVal.Export();
		}
		return dictionary;
	}

	public TowerDefensePacketConfig GetPacket()
	{
		if (packetName == "")
		{
			return null;
		}
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
