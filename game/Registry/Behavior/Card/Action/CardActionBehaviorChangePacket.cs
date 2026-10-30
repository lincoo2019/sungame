using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorChangePacket : CardActionBehaviorDefinition
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseLevelPacketConfig levelPacketConfig;

	public TowerDefensePacketConfig packetConfig;

	[Export(PropertyHint.None, "")]
	public int count = 1;

	public override void ImportConfiguration(Dictionary data)
	{
		levelPacketConfig = new TowerDefenseLevelPacketConfig();
		levelPacketConfig.Init(data);
		count = data.GetValueOrDefault("Count", 1).AsInt32();
	}

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		if (count > 1)
		{
			count--;
			return;
		}
		if (!GodotObject.IsInstanceValid(packetConfig) && GodotObject.IsInstanceValid(levelPacketConfig))
		{
			packetConfig = levelPacketConfig.GetPacket();
		}
		if (GodotObject.IsInstanceValid(packetConfig))
		{
			packet.Init(packetConfig);
		}
	}

	public override Dictionary ExportConfiguration()
	{
		Dictionary dictionary = (GodotObject.IsInstanceValid(levelPacketConfig) ? levelPacketConfig.Export() : new Dictionary());
		if (count > 1)
		{
			dictionary["Count"] = count;
		}
		return new Dictionary
		{
			["ActionId"] = "ChangePacket",
			["Configuration"] = dictionary
		};
	}
}
