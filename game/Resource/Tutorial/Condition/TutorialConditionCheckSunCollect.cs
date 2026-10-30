using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TutorialConditionCheckSunCollect : TutorialConditionConfig
{
	[Export(PropertyHint.None, "")]
	public int num = 25;

	public long currentNum;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		num = data.GetValueOrDefault("Num", 1).AsInt32();
	}

	public override void Enter()
	{
		base.Enter();
		TowerDefenseBattleFeatureSun sunFeature = TowerDefenseManager.Instance.GetSunFeature();
		if (GodotObject.IsInstanceValid(sunFeature))
		{
			sunFeature.OnSunCollect += SunCollect;
		}
	}

	public override bool Step()
	{
		return currentNum >= num;
	}

	public void SunCollect(long _sun)
	{
		currentNum += _sun;
	}
}
