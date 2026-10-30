using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieBossDaveAbilityConfig : Resource
{
	[ExportGroup("Placement", "")]
	[Export(PropertyHint.None, "")]
	public int plantDropCount = 3;

	[Export(PropertyHint.None, "")]
	public int plantColumnMin = 6;

	[Export(PropertyHint.None, "")]
	public int plantColumnMax = 9;

	[ExportGroup("Bungee", "")]
	[Export(PropertyHint.None, "")]
	public int bungeeCount = 3;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double steelBungeeChance = 0.1;

	[Export(PropertyHint.Range, "0,1,0.01")]
	public double hypnotistChance = 0.25;

	[ExportGroup("Missile", "")]
	[Export(PropertyHint.None, "")]
	public int missileCount = 4;

	[Export(PropertyHint.None, "")]
	public double missileDamage = 1800.0;

	[Export(PropertyHint.None, "")]
	public TowerDefenseProjectileConfig missileProjectileConfig;

	[ExportGroup("Doom", "")]
	[Export(PropertyHint.None, "")]
	public double doomChargeSeconds = 20.0;

	[Export(PropertyHint.None, "")]
	public double doomDamage = 1800.0;

	[ExportGroup("Stage Pools", "")]
	[Export(PropertyHint.None, "")]
	public Array stageZombiePools = new Array();

	[Export(PropertyHint.None, "")]
	public Array stagePlantPools = new Array();

	public Array GetZombieStage(int stage)
	{
		if (stageZombiePools.Count == 0)
		{
			return new Array();
		}
		int index = Mathf.Clamp(stage, 0, stageZombiePools.Count - 1);
		return stageZombiePools[index].AsGodotArray();
	}

	public Array GetPlantPool(int stage)
	{
		if (stagePlantPools.Count == 0)
		{
			return new Array();
		}
		int index = Mathf.Clamp(stage, 0, stagePlantPools.Count - 1);
		return stagePlantPools[index].AsGodotArray();
	}
}
