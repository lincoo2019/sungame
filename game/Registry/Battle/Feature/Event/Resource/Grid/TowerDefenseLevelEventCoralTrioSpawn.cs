using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventCoralTrioSpawn : TowerDefenseLevelEventBase
{
	public override string _GetName()
	{
		return "LEVLE_EVENT_CORAL_TRIO_SPAWN";
	}

	public override Dictionary Export()
	{
		return new Dictionary { ["EventName"] = "CoralTrioSpawn" };
	}

	public override void Execute()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			TowerDefenseBattleFeatureWave.Instance?.ScheduleTrioAmbush(coral: true);
		}
	}
}
