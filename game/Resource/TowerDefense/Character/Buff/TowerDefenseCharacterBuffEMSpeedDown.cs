using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffEMSpeedDown : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double time = 0.35;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override void _Init()
	{
		key = "EMSpeedDown";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 1) != 0)
		{
			character.buff.DeleteBuff(key);
		}
		else if (character.buff.BuffHas("IceSpeedDown") || character.buff.BuffHas("Frozen"))
		{
			character.buff.DeleteBuff(key);
		}
		else
		{
			character.emSpeedDown = true;
		}
	}

	public override void EnterReadOnlyClient()
	{
		character.emSpeedDown = true;
	}

	public override bool Step(double delta)
	{
		if (character.buff.BuffHas("IceSpeedDown") || character.buff.BuffHas("Frozen"))
		{
			return true;
		}
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
		character.emSpeedDown = false;
	}

	public override void ExitReadOnlyClient()
	{
		character.emSpeedDown = false;
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		time = Mathf.Max(time, (config as TowerDefenseCharacterBuffEMSpeedDown)?.time ?? 0.35);
		currentTime = 0.0;
	}
}
