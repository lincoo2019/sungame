using Godot;

[Tool]
public partial class TowerDefenseCraterNG : TowerDefenseCrater
{
	public override bool HasPendingRevival
	{
		get
		{
			if (inGame && !isDestroy)
			{
				return !weatheringDisabled;
			}
			return false;
		}
	}

	public override void DieDown()
	{
		SpawnWeatheredZombie("ZombieNecromancerG");
	}
}
