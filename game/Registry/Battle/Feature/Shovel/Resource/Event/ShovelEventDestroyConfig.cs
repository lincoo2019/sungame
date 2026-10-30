using Godot;

[GlobalClass]
public partial class ShovelEventDestroyConfig : ShovelEventConfig
{
	public override void Execute(TowerDefenseCharacter character)
	{
		character.ShovelDestroy();
	}
}
