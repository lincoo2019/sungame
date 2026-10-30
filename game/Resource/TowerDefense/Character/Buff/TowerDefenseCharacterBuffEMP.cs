using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffEMP : TowerDefenseCharacterBuffConfig
{
	private static readonly Color EMP_COLOR = new Color(0.15f, 0.15f, 0.15f, 0.8f);

	[Export(PropertyHint.None, "")]
	public double time = 8.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => EMP_COLOR;

	public override void _Init()
	{
		key = "EMP";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x1000) != 0)
		{
			character.buff.DeleteBuff("EMP");
		}
	}

	public override void EnterReadOnlyClient()
	{
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		character.timeScale *= 0.0;
		if (!character.nearDie && !character.die)
		{
			return currentTime >= time;
		}
		return true;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Exit()
	{
	}

	public override void Remove()
	{
		ExitReadOnlyClient();
	}

	public override void ExitReadOnlyClient()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffEMP towerDefenseCharacterBuffEMP = config as TowerDefenseCharacterBuffEMP;
		time = Mathf.Max(time, towerDefenseCharacterBuffEMP.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
