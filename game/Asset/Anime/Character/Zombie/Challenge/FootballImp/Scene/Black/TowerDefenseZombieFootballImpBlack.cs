using Godot;

[Tool]
public partial class TowerDefenseZombieFootballImpBlack : TowerDefenseZombieImpBase
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
			if (TowerDefenseManager.Instance.IsIZMMode())
			{
				instance.hitpointScale = 14.0 / 27.0;
			}
		}
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}

	public override void InWater()
	{
		base.InWater();
		base.sprite.SetFliter("Zombie_whitewater", open: true);
	}

	public override void OutWater()
	{
		base.OutWater();
		base.sprite.SetFliter("Zombie_whitewater", open: false);
	}
}
