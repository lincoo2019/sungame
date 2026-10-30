public partial class ObjectBehaviorCountingPacketEvent : CardActionBehaviorDefinition
{
	public int Executions;

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		Executions++;
	}
}
