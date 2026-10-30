public struct CardCostContext(TowerDefensePacketConfig packet, int baseCost)
{
	public TowerDefensePacketConfig Packet = packet;

	public readonly int BaseCost = baseCost;

	public int Cost = baseCost;

	public bool CanIncrease = packet?.canChangeCost ?? true;

	public bool StopCurrentStage = false;
}
