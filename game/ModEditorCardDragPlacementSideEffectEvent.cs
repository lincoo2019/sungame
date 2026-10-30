public partial class ModEditorCardDragPlacementSideEffectEvent : CardActionBehaviorDefinition
{
	public static int ExecutionCount { get; set; }

	public override void ExecuteAction(TowerDefenseInGamePacketShow packet)
	{
		ExecutionCount++;
	}
}
