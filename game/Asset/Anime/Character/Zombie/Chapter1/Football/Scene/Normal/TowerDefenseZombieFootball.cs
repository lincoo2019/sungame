using Godot;

[Tool]
public partial class TowerDefenseZombieFootball : TowerDefenseZombie
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube", "Zombie_whitewater");
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 3.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}
}
