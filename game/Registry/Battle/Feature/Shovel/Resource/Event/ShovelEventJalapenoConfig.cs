using Godot;

[GlobalClass]
public partial class ShovelEventJalapenoConfig : ShovelEventConfig
{
	[Export(PropertyHint.None, "")]
	public bool destroy = true;

	public override void Execute(TowerDefenseCharacter character)
	{
		if (character.cost > 0.0)
		{
			TowerDefenseCharacter.CreateJalapenoFire(TowerDefenseEnum.CHARACTER_CAMP.PLANT, character.gridPos, character.cost);
		}
		if (destroy)
		{
			character.ShovelDestroy();
		}
	}
}
