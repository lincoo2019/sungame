using Godot;

[GlobalClass]
public partial class TowerDefenseSun : TowerDefenseSunBase
{
	public override void _Ready()
	{
		base._Ready();
		base.dieDownTimer.Timeout += DieDown;
	}
}
