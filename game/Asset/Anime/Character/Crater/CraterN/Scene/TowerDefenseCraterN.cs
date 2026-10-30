using Godot;

[Tool]
public partial class TowerDefenseCraterN : TowerDefenseCrater
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
		SpawnWeatheredZombie("ZombieNecromancer");
	}
}
