using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffRuneMagic : TowerDefenseCharacterBuffConfig
{
	public const string StormSlow = "RuneStormSlow";

	public const string FogHaste = "RuneFogHaste";

	public const string FogDizzyImmune = "RuneFogDizzyImmune";

	[Export(PropertyHint.None, "")]
	public double timeScaleValue = 1.0;

	[Export(PropertyHint.None, "")]
	public double time = 0.75;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override void _Init()
	{
		if (key == null)
		{
			key = "RuneStormSlow";
		}
	}

	public override TowerDefenseCharacterBuffConfig CreateRuntimeInstance()
	{
		return new TowerDefenseCharacterBuffRuneMagic
		{
			key = key,
			refresh = refresh,
			canFliter = canFliter,
			timeScaleValue = timeScaleValue,
			time = time,
			currentTime = currentTime
		};
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		if (currentTime >= time)
		{
			return true;
		}
		character.timeScale *= timeScaleValue;
		return false;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		TowerDefenseCharacterBuffRuneMagic towerDefenseCharacterBuffRuneMagic = (TowerDefenseCharacterBuffRuneMagic)config;
		time = Mathf.Max(time, currentTime + towerDefenseCharacterBuffRuneMagic.time);
		timeScaleValue = towerDefenseCharacterBuffRuneMagic.timeScaleValue;
	}
}
