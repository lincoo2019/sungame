using Godot;

[GlobalClass]
public partial class TowerDefenseCharacterBuffTimeMagic : TowerDefenseCharacterBuffConfig
{
	private static readonly Color TIME_MAGIC_COLOR = new Color(0.7f, 0.85f, 1f);

	[Export(PropertyHint.None, "")]
	public double timeScaleValue = 0.5;

	[Export(PropertyHint.None, "")]
	public double time = 5.0;

	[Export(PropertyHint.None, "")]
	public double currentTime;

	public override Color FrameMeshColorMultiplier => TIME_MAGIC_COLOR;

	public override void _Init()
	{
		key = "TimeMagic";
	}

	public override bool Step(double delta)
	{
		currentTime += delta;
		character.timeScale *= timeScaleValue;
		return currentTime >= time;
	}

	public override void StepReadOnlyClient(double delta)
	{
		Step(delta);
	}

	public override void Refresh(TowerDefenseCharacterBuffConfig config)
	{
		if (config is TowerDefenseCharacterBuffTimeMagic towerDefenseCharacterBuffTimeMagic)
		{
			timeScaleValue = towerDefenseCharacterBuffTimeMagic.timeScaleValue;
			time = Mathf.Max(time, towerDefenseCharacterBuffTimeMagic.time);
			currentTime = 0.0;
		}
	}
}
