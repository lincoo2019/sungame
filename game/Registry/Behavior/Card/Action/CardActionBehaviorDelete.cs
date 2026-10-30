using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorDelete : CardActionBehaviorDefinition
{
	public override void ImportConfiguration(Dictionary data)
	{
	}

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		TowerDefenseManager.Instance.GetSeedBank().packetList.Remove(packet);
		packet.QueueFree();
	}

	public override Dictionary ExportConfiguration()
	{
		return new Dictionary
		{
			["ActionId"] = "Delete",
			["Configuration"] = new Dictionary()
		};
	}
}
