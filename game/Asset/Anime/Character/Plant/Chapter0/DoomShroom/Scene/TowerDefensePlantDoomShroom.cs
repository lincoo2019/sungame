using Godot;

[Tool]
public partial class TowerDefensePlantDoomShroom : TowerDefensePlant
{
	public override void SleepEntered()
	{
		base.SleepEntered();
		instance.invincible = false;
	}
}
