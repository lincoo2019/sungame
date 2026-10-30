using Godot;

public partial class CharacterStressControlStub : TowerDefenseControlNew
{
	public override void _Ready()
	{
	}

	public override void _PhysicsProcess(double delta)
	{
		if (isGameRunning && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.runGameTime += delta;
		}
	}
}
