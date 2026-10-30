public partial class FireHitModRuntimeProbe : TowerDefenseCharacterBuffFireHit
{
	public static int RuntimeInstanceCreates;

	public int RefreshCount;

	public override TowerDefenseCharacterBuffConfig CreateRuntimeInstance()
	{
		RuntimeInstanceCreates++;
		return new FireHitModRuntimeProbe
		{
			key = key,
			refresh = refresh,
			canFliter = canFliter
		};
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		RefreshCount++;
	}
}
