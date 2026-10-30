using Godot;

[Tool]
public partial class TowerDefenseMowerDefault : TowerDefenseMower
{
	public override void InWater()
	{
		base.InWater();
		CreateSplash();
		Destroy();
	}
}
