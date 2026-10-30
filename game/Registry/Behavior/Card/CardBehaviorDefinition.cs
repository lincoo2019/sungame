using Godot;

[Tool]
[GlobalClass]
public abstract partial class CardBehaviorDefinition : BehaviorDefinitionBase
{
	public CardBehaviorDefinition()
	{
		base.BehaviorTypeId = new StringName("card");
	}

	public virtual CardBehaviorRuntime CreateRuntime()
	{
		return null;
	}

	public virtual void ModifyCost(ref CardCostContext context)
	{
	}
}
