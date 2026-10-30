using Godot;

[Tool]
public partial class TowerDefensePlantBowlingWallnutHole : TowerDefensePlantBowlingBase
{
	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			bowlingComponent.OnBowling += Bowling;
		}
	}

	public override void _ExitTree()
	{
		base._ExitTree();
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.OnBowling -= Bowling;
		}
	}

	public void Bowling(TowerDefenseCharacter character)
	{
		CraterCreate();
		Destroy();
	}
}
