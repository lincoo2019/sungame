using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorSetCooldown : CardActionBehaviorDefinition
{
	[Export(PropertyHint.None, "")]
	public double value;

	public override void ImportConfiguration(Dictionary data)
	{
		value = data.GetValueOrDefault("Value", 0.0).AsDouble();
	}

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		packet.coldDownOpen = true;
		packet.coldDownTimer = value;
	}

	public override Dictionary ExportConfiguration()
	{
		return new Dictionary
		{
			["ActionId"] = "ChangeCurrentColdDown",
			["Configuration"] = new Dictionary { ["Value"] = value }
		};
	}
}
