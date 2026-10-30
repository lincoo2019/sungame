using Godot;

[GlobalClass]
public partial class CardActionBehaviorFactory : Resource
{
	public static CardActionBehaviorDefinition Create(string actionName)
	{
		return actionName switch
		{
			"ChangeCurrentColdDown" => new CardActionBehaviorSetCooldown(), 
			"ChangePacket" => new CardActionBehaviorChangePacket(), 
			"ChangeCost" => new CardActionBehaviorChangeCost(), 
			"CreditSun" => new CardActionBehaviorCreditSun(), 
			"Delete" => new CardActionBehaviorDelete(), 
			_ => null, 
		};
	}
}
