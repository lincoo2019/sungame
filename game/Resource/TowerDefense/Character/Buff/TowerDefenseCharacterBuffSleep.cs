using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffSleep : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override void _Init()
	{
		key = "Sleep";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x100) != 0)
		{
			character.buff.DeleteBuff("Sleep");
		}
		else
		{
			character.Sleep();
		}
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		character.timeScale *= 0.5;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffSleep towerDefenseCharacterBuffSleep = config as TowerDefenseCharacterBuffSleep;
		time = Mathf.Max(time, towerDefenseCharacterBuffSleep.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
