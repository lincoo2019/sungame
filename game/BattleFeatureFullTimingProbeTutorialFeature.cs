using System.Threading.Tasks;
using Godot.Collections;

public partial class BattleFeatureFullTimingProbeTutorialFeature : TowerDefenseBattleFeatureTutorial
{
	private string _id = "Tutorial";

	public override void Init(Dictionary data)
	{
		base.Init(data);
		_id = data.GetValueOrDefault("Id", _id).AsString();
		config = new TutorialConfig
		{
			step = new Array<TutorialStepConfig>
			{
				new TutorialStepConfig
				{
					conditionList = new Array<TutorialConditionConfig>
					{
						new BattleFeatureFullTimingPendingTutorialCondition()
					}
				}
			}
		};
		BattleFeatureFullTimingProbeLog.Add("Init:" + _id);
	}

	public override void OnReady()
	{
		BattleFeatureFullTimingProbeLog.Add("OnReady:" + _id);
	}

	public override async Task GameStart()
	{
		BattleFeatureFullTimingProbeLog.Add("GameStart.begin:" + _id);
		await base.GameStart();
		if (base.IsLifetimeActive)
		{
			BattleFeatureFullTimingProbeLog.Add("GameStart.end:" + _id);
		}
	}

	public override async Task GameStartFromProgress()
	{
		BattleFeatureFullTimingProbeLog.Add("GameStartFromProgress.begin:" + _id);
		await base.GameStartFromProgress();
		if (base.IsLifetimeActive)
		{
			BattleFeatureFullTimingProbeLog.Add("GameStartFromProgress.end:" + _id);
		}
	}

	public override void Destroy()
	{
		BattleFeatureFullTimingProbeLog.Add("Destroy:" + _id);
		base.Destroy();
	}
}
