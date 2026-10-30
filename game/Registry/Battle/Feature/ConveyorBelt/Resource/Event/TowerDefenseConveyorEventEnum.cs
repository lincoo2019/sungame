using Godot;

[GlobalClass]
public partial class TowerDefenseConveyorEventEnum : Resource
{
	public static TowerDefenseConveyorEventBase EventGet(string eventName)
	{
		if (eventName == "AddPacket")
		{
			return new TowerDefenseConveyorEventAddPacket();
		}
		return null;
	}
}
