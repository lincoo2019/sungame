using Godot;

[Tool]
[GlobalClass]
public partial class TowerDefenseZombieSleeper : TowerDefenseZombie
{
	public override void _Ready()
	{
		base._Ready();
		Engine.IsEditorHint();
	}

	public override void AttackProcessing(double delta)
	{
		base.AttackProcessing(delta);
		base.sprite.timeScale = timeScale * 4.0;
	}

	public override void DieProcessing(double delta)
	{
		base.DieProcessing(delta);
		base.sprite.timeScale = timeScale * 2.0;
	}
}
