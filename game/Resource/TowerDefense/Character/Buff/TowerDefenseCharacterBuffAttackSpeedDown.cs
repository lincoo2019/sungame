using System;
using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffAttackSpeedDown : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double timeScaleValue = 0.5;

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override void _Init()
	{
		key = "AttackSpeedDown";
	}

	public override void Enter()
	{
	}

	public override void EnterReadOnlyClient()
	{
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		return currentTime >= time;
	}

	public double GetAttackSpeedMultiplier()
	{
		if (!(currentTime < time))
		{
			return 1.0;
		}
		return Math.Max(0.0, timeScaleValue);
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
	}

	public override void ExitReadOnlyClient()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffAttackSpeedDown towerDefenseCharacterBuffAttackSpeedDown)
		{
			timeScaleValue = towerDefenseCharacterBuffAttackSpeedDown.timeScaleValue;
			time = towerDefenseCharacterBuffAttackSpeedDown.time;
			currentTime = 0.0;
		}
	}
}
