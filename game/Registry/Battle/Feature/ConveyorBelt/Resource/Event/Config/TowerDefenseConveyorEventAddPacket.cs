using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseConveyorEventAddPacket : TowerDefenseConveyorEventBase
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseConveyorPacketConfig packet;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		packet = new TowerDefenseConveyorPacketConfig();
		packet.Init(data);
	}

	public override void Execute()
	{
		TowerDefenseBattleFeatureConveyorBelt conveyorBeltFeature = TowerDefenseManager.Instance.GetConveyorBeltFeature();
		if (GodotObject.IsInstanceValid(conveyorBeltFeature))
		{
			conveyorBeltFeature.packetList.Add(packet);
		}
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "AddPacket",
			["Value"] = packet.Export()
		};
	}
}
