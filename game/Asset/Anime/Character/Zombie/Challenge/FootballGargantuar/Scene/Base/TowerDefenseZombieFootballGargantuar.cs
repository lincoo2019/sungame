using Godot;

[Tool]
public partial class TowerDefenseZombieFootballGargantuar : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_FOOTBALL_GARGANTUAR_BODY12 = "uid://cho4e0w7335lx";

	private const string ZOMBIE_FOOTBALL_GARGANTUAR_BODY13 = "uid://rm3iebyhewoi";

	private const string ZOMBIE_FOOTBALL_GARGANTUAR_HEAD2 = "uid://d2h300mowbih5";

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				base.sprite.SetAtlasReplace("Zombie_football_gargantuar_head.png", "uid://d2h300mowbih5");
				base.sprite.SetAtlasReplace("Zombie_football_gargantuar_body1.png", "uid://rm3iebyhewoi");
			}
		}
		else
		{
			base.sprite.SetAtlasReplace("Zombie_football_gargantuar_body1.png", "uid://cho4e0w7335lx");
		}
	}
}
