public partial class PlantAttackIntervalProjectileProbe : FireComponentProjectileSingle
{
	public int Checks { get; private set; }

	public bool Available { get; set; }

	public override bool CanFire(FireComponent fireComponent, int collisionFlag)
	{
		Checks++;
		return Available;
	}
}
