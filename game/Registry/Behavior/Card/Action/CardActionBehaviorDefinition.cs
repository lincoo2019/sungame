using Godot;
using Godot.Collections;

[GlobalClass]
public partial class CardActionBehaviorDefinition : CardBehaviorDefinition
{
	private sealed class ActionRuntime : CardBehaviorRuntime
	{
		private readonly CardActionBehaviorDefinition _definition;

		private readonly CardActionBehaviorTrigger _trigger;

		public ActionRuntime(CardActionBehaviorDefinition definition, CardActionBehaviorTrigger trigger)
		{
			_definition = definition;
			_trigger = trigger;
		}

		public override void OnPressed()
		{
			if ((_trigger & CardActionBehaviorTrigger.Pressed) != CardActionBehaviorTrigger.None)
			{
				_definition.ExecuteAction(base.Owner);
			}
		}

		public override void OnUseSucceeded(TowerDefenseCharacter createdCharacter)
		{
			if ((_trigger & CardActionBehaviorTrigger.UseSucceeded) != CardActionBehaviorTrigger.None)
			{
				_definition.ExecuteAction(base.Owner);
			}
		}
	}

	[Export(PropertyHint.Flags, "Pressed,UseSucceeded")]
	public int triggerFlags = 2;

	public virtual void ImportConfiguration(Dictionary data)
	{
	}

	public virtual void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
	}

	public virtual Dictionary ExportConfiguration()
	{
		return new Dictionary();
	}

	public static Array<CardActionBehaviorDefinition> ReadLegacyArray(Variant value, CardActionBehaviorTrigger trigger)
	{
		Array<CardActionBehaviorDefinition> array = new Array<CardActionBehaviorDefinition>();
		if (value.VariantType != Variant.Type.Array)
		{
			return array;
		}
		foreach (Variant item in value.AsGodotArray())
		{
			if (item.VariantType == Variant.Type.Object && item.AsGodotObject() is CardActionBehaviorDefinition cardActionBehaviorDefinition)
			{
				cardActionBehaviorDefinition.triggerFlags = (int)trigger;
				array.Add(cardActionBehaviorDefinition);
			}
		}
		return array;
	}

	public override CardBehaviorRuntime CreateRuntime()
	{
		return CreateRuntimeForTrigger((CardActionBehaviorTrigger)triggerFlags);
	}

	internal CardBehaviorRuntime CreateRuntimeForTrigger(CardActionBehaviorTrigger trigger)
	{
		return new ActionRuntime(this, trigger);
	}
}
