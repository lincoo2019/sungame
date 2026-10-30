using Godot;

[Tool]
public partial class TowerDefenseItemLadder : TowerDefenseItem
{
	public override void ArmorHitpointsEmpty(string armorName)
	{
		base.ArmorHitpointsEmpty(armorName);
		if (armorName == "Ladder")
		{
			Destroy();
		}
	}

	public override void Block(TowerDefenseCharacter target)
	{
		ClearArmorAll();
		Destroy();
	}
}
