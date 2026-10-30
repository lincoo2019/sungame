using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffRedHeat : TowerDefenseCharacterBuffConfig
{
	private static readonly Color RED_HEAT_COLOR = new Color(0.78f, 0f, 0f);

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => RED_HEAT_COLOR;

	public override void _Init()
	{
		key = "RedHeat";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x10) != 0)
		{
			character.buff.DeleteBuff("RedHeat");
		}
		else
		{
			character.buff.ApplyFireHit();
		}
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
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
		TowerDefenseCharacterBuffRedHeat towerDefenseCharacterBuffRedHeat = config as TowerDefenseCharacterBuffRedHeat;
		time = Mathf.Max(time, towerDefenseCharacterBuffRedHeat.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
