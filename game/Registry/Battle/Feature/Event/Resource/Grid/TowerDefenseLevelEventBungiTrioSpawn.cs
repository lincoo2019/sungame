using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventBungiTrioSpawn : TowerDefenseLevelEventBase
{
	public override string _GetName()
	{
		return "LEVLE_EVENT_BUNGI_TRIO_SPAWN";
	}

	public override Dictionary Export()
	{
		return new Dictionary { ["EventName"] = "BungiTrioSpawn" };
	}

	public override void Execute()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			TowerDefenseBattleFeatureWave.Instance?.ScheduleTrioAmbush(coral: false);
		}
	}
}
