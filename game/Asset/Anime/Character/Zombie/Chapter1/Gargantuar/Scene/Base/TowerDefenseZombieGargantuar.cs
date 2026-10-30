using Godot;

[Tool]
public partial class TowerDefenseZombieGargantuar : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_HEAD2 = "uid://cfx6iqy08xujv";

	private const string ZOMBIE_GARGANTUAR_DUCKXING = "uid://6dy81rx4gaue";

	private const string ZOMBIE_GARGANTUAR_ZOMBIE = "uid://dtrl03qm2d0u7";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			double num = GD.Randf();
			if (num < 0.3)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://6dy81rx4gaue");
			}
			else if (num < 0.6)
			{
				base.sprite.SetAtlasReplace("Zombie_gargantuar_telephonepole.png", "uid://dtrl03qm2d0u7");
			}
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
		}
	}

	public override void DamagePointReach(string damangePointName)
	{
		base.DamagePointReach(damangePointName);
		if (damangePointName == "Head")
		{
			base.sprite.SetAtlasReplace("Zombie_gargantuar_head.png", "uid://cfx6iqy08xujv");
		}
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
