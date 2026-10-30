using Godot;

[Tool]
public partial class TowerDefenseZombieFootballGargantuarBlackWhiteEyes : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_FOOTBALL_GARGANTUAR_HEAD2 = "uid://d2h300mowbih5";

	private const string ZOMBIE_FOOTBALL_GARGANTUAR_R_BODY12 = "uid://pcqrhw4twpms";

	private const string ZOMBIE_FOOTBALL_GARGANTUAR_R_BODY13 = "uid://v14ui28vj0nv";

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (!(damagePointName == "Arm"))
		{
			if (damagePointName == "Head")
			{
				base.sprite.SetAtlasReplace("Zombie_football_gargantuar_head.png", "uid://d2h300mowbih5");
				base.sprite.SetAtlasReplace("Zombie_football_gargantuar_body1.png", "uid://v14ui28vj0nv");
			}
		}
		else
		{
			base.sprite.SetAtlasReplace("Zombie_football_gargantuar_body1.png", "uid://pcqrhw4twpms");
		}
	}
}
