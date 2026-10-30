using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffJalaHit : TowerDefenseCharacterBuffConfig
{
	public override void _Init()
	{
		key = "JalaHit";
	}

	public override bool Step(double delta)
	{
		return true;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}
}
