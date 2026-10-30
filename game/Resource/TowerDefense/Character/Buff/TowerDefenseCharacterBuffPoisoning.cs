using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffPoisoning : TowerDefenseCharacterBuffConfig
{
	private static readonly Color POISONING_COLOR = new Color(0.35f, 1f, 0f);

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public double timer;

	public override Color FrameMeshColorMultiplier => POISONING_COLOR;

	public override void _Init()
	{
		key = "Poisoning";
	}

	public override void Enter()
	{
		if ((character.instance.unUseBuffFlags & 0x40) != 0)
		{
			character.buff.DeleteBuff("Poisoning");
		}
	}

	public override bool Step(double delta)
	{
		if (character.instance.die)
		{
			return true;
		}
		if (timer < 1.5)
		{
			timer += delta;
		}
		else
		{
			timer = 0.0;
			character.instance.DealHurt(20.0);
			ShowHealthComponent showHealthComponent = character.showHealthComponent;
			if (showHealthComponent != null && !showHealthComponent.IsReleased)
			{
				character.showHealthComponent.MarkDirty();
			}
		}
		currentTime += delta;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		if (!character.instance.die)
		{
			currentTime += delta;
		}
	}

	public override void Exit()
	{
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffPoisoning towerDefenseCharacterBuffPoisoning = config as TowerDefenseCharacterBuffPoisoning;
		time = Mathf.Max(time, towerDefenseCharacterBuffPoisoning.time + GD.RandRange(-0.2, 0.2));
		currentTime = 0.0;
	}
}
