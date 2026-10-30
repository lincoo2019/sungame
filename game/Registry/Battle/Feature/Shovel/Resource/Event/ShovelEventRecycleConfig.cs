using Godot;

[GlobalClass]
public partial class ShovelEventRecycleConfig : ShovelEventConfig
{
	[Export(PropertyHint.None, "")]
	public double percentage = 0.2;

	[Export(PropertyHint.None, "")]
	public bool destroy = true;

	public override void Execute(TowerDefenseCharacter character)
	{
		character.Recycle(percentage, destroy);
	}
}
