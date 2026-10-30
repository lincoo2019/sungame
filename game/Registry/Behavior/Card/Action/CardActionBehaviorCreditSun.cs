using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorCreditSun : CardActionBehaviorDefinition
{
	[Export(PropertyHint.None, "")]
	public long amount;

	public override void ImportConfiguration(Dictionary data)
	{
		amount = data.GetValueOrDefault("Amount", 0L).AsInt64();
	}

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		if (amount > 0 && GodotObject.IsInstanceValid(packet) && packet.HasSunAccount)
		{
			TowerDefenseManager instance = TowerDefenseManager.Instance;
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.CreditSun(packet.SunAccountId, amount);
			}
		}
	}

	public override Dictionary ExportConfiguration()
	{
		return new Dictionary
		{
			["ActionId"] = "CreditSun",
			["Configuration"] = new Dictionary { ["Amount"] = amount }
		};
	}
}
