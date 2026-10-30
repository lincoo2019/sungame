using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCurrentMapUseStripe : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public int row = 3;

	public override string _GetName()
	{
		return "LEVLE_EVENT_CURRENTMAP_USE_STRIPE";
	}

	public override void Execute()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature != null)
		{
			mapFeature.stripeRow = row;
		}
		TowerDefenseMap currentMap = TowerDefenseManager.Instance.GetCurrentMap();
		if (GodotObject.IsInstanceValid(currentMap))
		{
			currentMap.UseStripe(row);
		}
	}

	public override void Init(Dictionary valueDictionary)
	{
		row = valueDictionary.GetValueOrDefault("Row", new Array()).AsInt32();
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "CurrentMapUseStripe",
			["Value"] = new Dictionary { ["Row"] = row }
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["使用红线"] = new Dictionary { ["列"] = new Dictionary
		{
			["Object"] = this,
			["Type"] = "Int",
			["Property"] = "row",
			["Rest"] = 3
		} };
		return property;
	}
}
