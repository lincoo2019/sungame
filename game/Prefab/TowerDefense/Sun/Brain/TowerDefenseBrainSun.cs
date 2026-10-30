using Godot;

[GlobalClass]
public partial class TowerDefenseBrainSun : TowerDefenseSunBase
{
	public override void _Ready()
	{
		base._Ready();
		base.dieDownTimer.Timeout += DieDown;
	}

	public override string GetGroupName()
	{
		return "BrainSun";
	}

	public override ObjectManagerConfig.OBJECT GetPoolKey()
	{
		return ObjectManagerConfig.OBJECT.SUN_BRAIN;
	}

	public override void OnInitScaleTween(Tween tween)
	{
		tween.Finished += base.Collection;
	}
}
