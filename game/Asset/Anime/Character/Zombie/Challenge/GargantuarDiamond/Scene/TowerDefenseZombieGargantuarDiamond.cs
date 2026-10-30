using Godot;

[Tool]
public partial class TowerDefenseZombieGargantuarDiamond : TowerDefenseZombieGargantuarBase
{
	private const string ZOMBIE_GARGANTUAR_HEAD2 = "uid://cfx6iqy08xujv";

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			ConfigureWaterLineVisualLayers("Zombie_duckytube");
		}
	}

	public override void DamagePointReach(string damagePointName)
	{
		base.DamagePointReach(damagePointName);
		if (damagePointName == "Head")
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
