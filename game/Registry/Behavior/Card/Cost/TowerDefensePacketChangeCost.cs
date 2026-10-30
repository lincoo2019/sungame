using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefensePacketChangeCost : CardBehaviorDefinition
{
	[Export(PropertyHint.Enum, "Increase,Decrease,Set")]
	public string method = "Increase";

	[Export(PropertyHint.None, "")]
	public Dictionary amontDictionary = new Dictionary
	{
		[0] = 25,
		[1] = 25,
		[2] = 25,
		[3] = 25,
		[4] = 25,
		[5] = 25,
		[6] = 25,
		[7] = 25,
		[8] = 25
	};

	[Export(PropertyHint.None, "")]
	public string key = "";

	[Export(PropertyHint.None, "")]
	public bool lockCost;

	[Export(PropertyHint.None, "")]
	public bool skip;

	[Export(PropertyHint.None, "")]
	public bool consumeOnPurchase;

	public int Execute(int num, TowerDefensePacketConfig packet)
	{
		CardCostContext context = new CardCostContext(packet, num);
		ModifyCost(ref context);
		return context.Cost;
	}

	public override void ModifyCost(ref CardCostContext context)
	{
		switch (method)
		{
		case "Increase":
			if (context.CanIncrease)
			{
				int type2 = (int)context.Packet.type;
				if (amontDictionary.ContainsKey(type2))
				{
					context.Cost += (int)amontDictionary[type2];
				}
			}
			break;
		case "Decrease":
		{
			int type3 = (int)context.Packet.type;
			if (amontDictionary.ContainsKey(type3))
			{
				context.Cost -= (int)amontDictionary[type3];
			}
			break;
		}
		case "Set":
		{
			int type = (int)context.Packet.type;
			if (amontDictionary.ContainsKey(type))
			{
				context.Cost = (int)amontDictionary[type];
			}
			break;
		}
		}
		if (lockCost)
		{
			context.CanIncrease = false;
		}
		if (skip)
		{
			context.StopCurrentStage = true;
		}
	}

	public Dictionary ExportSave()
	{
		Dictionary dictionary = new Dictionary();
		foreach (Variant key in amontDictionary.Keys)
		{
			dictionary[key] = amontDictionary[key];
		}
		return new Dictionary
		{
			["method"] = method,
			["amontDictionary"] = dictionary,
			["key"] = this.key,
			["lockCost"] = lockCost,
			["skip"] = skip,
			["consumeOnPurchase"] = consumeOnPurchase
		};
	}

	public static TowerDefensePacketChangeCost ImportSave(Dictionary data)
	{
		TowerDefensePacketChangeCost towerDefensePacketChangeCost = new TowerDefensePacketChangeCost();
		towerDefensePacketChangeCost.method = (data.ContainsKey("method") ? ((string)data["method"]) : "Increase");
		Dictionary dictionary = (data.ContainsKey("amontDictionary") ? ((Dictionary)data["amontDictionary"]) : new Dictionary());
		foreach (Variant key in dictionary.Keys)
		{
			towerDefensePacketChangeCost.amontDictionary[key] = dictionary[key];
		}
		towerDefensePacketChangeCost.key = (data.ContainsKey("key") ? ((string)data["key"]) : "");
		towerDefensePacketChangeCost.lockCost = data.ContainsKey("lockCost") && (bool)data["lockCost"];
		towerDefensePacketChangeCost.skip = data.ContainsKey("skip") && (bool)data["skip"];
		towerDefensePacketChangeCost.consumeOnPurchase = data.GetValueOrDefault("consumeOnPurchase", false).AsBool();
		return towerDefensePacketChangeCost;
	}
}
