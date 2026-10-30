using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffIceSpeedDown : TowerDefenseCharacterBuffConfig
{
	private static readonly Color ICE_SPEED_DOWN_COLOR = new Color(0.2f, 0.35f, 1f);

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => ICE_SPEED_DOWN_COLOR;

	public override void _Init()
	{
		key = "IceSpeedDown";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 1) != 0)
		{
			character.buff.DeleteBuff("IceSpeedDown");
		}
		else
		{
			character.iceSpeedDown = true;
		}
	}

	public override void EnterReadOnlyClient()
	{
		character.iceSpeedDown = true;
	}

	public override bool Step(double delta)
	{
		if (character.buff.BuffHas("Frozen"))
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
		character.iceSpeedDown = false;
	}

	public override void ExitReadOnlyClient()
	{
		character.iceSpeedDown = false;
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffIceSpeedDown towerDefenseCharacterBuffIceSpeedDown = config as TowerDefenseCharacterBuffIceSpeedDown;
		time = Mathf.Max(time, towerDefenseCharacterBuffIceSpeedDown.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
