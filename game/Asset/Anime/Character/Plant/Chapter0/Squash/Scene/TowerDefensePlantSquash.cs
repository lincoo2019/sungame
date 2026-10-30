using Godot;

[Tool]
public partial class TowerDefensePlantSquash : TowerDefensePlant
{
	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}
}
