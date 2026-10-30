using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieConfig : TowerDefenseCharacterConfig
{
	[Export(PropertyHint.None, "")]
	public TowerDefenseEnum.ZOMBIE_PHYSIQUE physique = TowerDefenseEnum.ZOMBIE_PHYSIQUE.NORMAL;

	[Export(PropertyHint.None, "")]
	public double attack;

	[Export(PropertyHint.None, "")]
	public double smashAttack;

	[Export(PropertyHint.None, "")]
	public string impactAudio = "";

	[ExportCategory("Spawn")]
	[Export(PropertyHint.None, "")]
	public bool preview = true;

	[Export(PropertyHint.None, "")]
	public int weight = 1000;

	[Export(PropertyHint.None, "")]
	public int wavePointCost = 100;

	[Export(PropertyHint.None, "")]
	public bool canSpawnPlantfood = true;

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseEnum.PLANTGRIDTYPE> excludeLineGridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>();

	[Export(PropertyHint.None, "")]
	public Array<TowerDefenseEnum.PLANTGRIDTYPE> spawnLineNeed = new Array<TowerDefenseEnum.PLANTGRIDTYPE>();
}
