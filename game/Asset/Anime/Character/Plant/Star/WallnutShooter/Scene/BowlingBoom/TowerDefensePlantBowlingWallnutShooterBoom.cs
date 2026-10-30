using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefensePlantBowlingWallnutShooterBoom : TowerDefensePlantBowlingBase
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			BowlingComponent obj = bowlingComponent;
			if (obj != null && !obj.IsReleased)
			{
				bowlingComponent.OnBowling += Bowling;
				bowlingComponent.Refresh();
				bowlingComponent.ChangeCheck();
			}
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
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.Explode();
			Destroy();
		}
	}

	public void ArmExplosionImmediately()
	{
		BowlingComponent obj = bowlingComponent;
		if (obj != null && !obj.IsReleased)
		{
			bowlingComponent.Refresh();
			bowlingComponent.ChangeCheck();
		}
	}
}
