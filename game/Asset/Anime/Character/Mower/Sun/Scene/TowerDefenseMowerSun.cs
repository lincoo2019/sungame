using Godot;

[Tool]
public partial class TowerDefenseMowerSun : TowerDefenseMower
{
	public override void InWater()
	{
		base.InWater();
		CreateSplash();
		Destroy();
	}
}
