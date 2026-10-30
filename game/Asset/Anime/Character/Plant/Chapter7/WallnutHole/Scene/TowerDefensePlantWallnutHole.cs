using Godot;

[Tool]
public partial class TowerDefensePlantWallnutHole : TowerDefensePlant
{
	public override void DestroySet()
	{
		base.DestroySet();
		CraterCreate();
	}
}
