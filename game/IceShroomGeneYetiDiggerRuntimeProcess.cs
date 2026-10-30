using Godot;

public partial class IceShroomGeneYetiDiggerRuntimeProcess : TowerDefenseBattleProcess
{
	public int houseEntries;

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		houseEntries++;
		GD.Print($"GENE_YETI_HOUSE name={character.config.name} position={character.GetLogicalGlobalPosition()} grid={character.gridPos}");
	}
}
