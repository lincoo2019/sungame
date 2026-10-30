public partial class LookStarWaveLoopRuntimeProcess : TowerDefenseBattleProcessWave
{
	public int finishCount;

	public override void Finish()
	{
		finishCount++;
	}
}
