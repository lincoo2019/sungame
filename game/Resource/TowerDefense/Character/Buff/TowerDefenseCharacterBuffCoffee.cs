using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffCoffee : TowerDefenseCharacterBuffConfig
{
	[Export(PropertyHint.None, "")]
	public double timeScaleValue = 3.0;

	[Export(PropertyHint.None, "")]
	public double time = 15.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	[Export(PropertyHint.None, "")]
	public bool blink;

	public override void _Init()
	{
		key = "Coffee";
	}

	public override void Enter()
	{
	}

	public override void EnterReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", blink);
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		character.timeScale *= timeScaleValue;
		if (!blink && currentTime >= time - 3.0)
		{
			character.SetSpriteGroupShaderParameter("blink", true);
			blink = true;
		}
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void SyncPresentationReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", blink);
	}

	public override void Exit()
	{
		character.SetSpriteGroupShaderParameter("blink", false);
	}

	public override void ExitReadOnlyClient()
	{
		character.SetSpriteGroupShaderParameter("blink", false);
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffCoffee towerDefenseCharacterBuffCoffee)
		{
			timeScaleValue = towerDefenseCharacterBuffCoffee.timeScaleValue;
			time = towerDefenseCharacterBuffCoffee.time;
			currentTime = 0.0;
			blink = false;
			character.SetSpriteGroupShaderParameter("blink", false);
		}
	}
}
