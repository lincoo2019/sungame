using Godot;

[Tool]
public partial class TowerDefensePlantSquashKing : TowerDefensePlant
{
	public AttackComponent attackComponent;

	public SquashComponent squashComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			attackComponent = componentManager.GetRuntime<AttackComponent>("character.attack.0");
			squashComponent = componentManager.GetRuntime<SquashComponent>();
		}
	}

	public override void IdleProcessing(double delta)
	{
		base.IdleProcessing(delta);
		base.sprite.timeScale = timeScale;
	}
}
