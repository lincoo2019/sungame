using System;
using System.Collections.Generic;
using Godot;

public static class CardBehaviorDispatcher
{
	public static int ApplyCost(TowerDefensePacketConfig packet, int baseCost, bool skipGlobalChangeCost)
	{
		CardCostContext context = new CardCostContext(packet, baseCost);
		ApplyStage(TowerDefenseBehaviorRegistry.Resolve(packet?.behaviorIds, packet?.behaviors), ref context);
		ApplyStage(packet?.changeCostList, ref context);
		if (!skipGlobalChangeCost)
		{
			ApplyStage(TowerDefenseManager.GetChangeCostList(), ref context);
		}
		if (baseCost < 0 || context.Cost >= 0)
		{
			return context.Cost;
		}
		return 0;
	}

	private static void ApplyStage(IEnumerable<CardBehaviorDefinition> definitions, ref CardCostContext context)
	{
		if (definitions == null)
		{
			return;
		}
		context.StopCurrentStage = false;
		foreach (CardBehaviorDefinition definition in definitions)
		{
			if (GodotObject.IsInstanceValid(definition) && definition.InitiallyEnabled)
			{
				try
				{
					definition.ModifyCost(ref context);
				}
				catch (Exception ex)
				{
					GD.PushError($"[CardBehavior:E_COST] behavior='{definition.GetDiagnosticName()}' packet='{context.Packet?.saveKey}' reason='{ex.Message}'");
				}
				if (context.StopCurrentStage)
				{
					break;
				}
			}
		}
	}
}
