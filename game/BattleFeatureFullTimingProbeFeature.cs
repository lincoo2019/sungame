using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BattleFeatureFullTimingProbeFeature : TowerDefenseBattleFeature
{
	private string _id = "Feature";

	private bool _canLoadProgress = true;

	public override void Init(Dictionary data)
	{
		base.Init(data);
		_id = data.GetValueOrDefault("Id", _id).AsString();
		_canLoadProgress = data.GetValueOrDefault("CanLoadProgress", true).AsBool();
		BattleFeatureFullTimingProbeLog.Add("Init:" + _id);
	}

	public override void OnReady()
	{
		BattleFeatureFullTimingProbeLog.Add("OnReady:" + _id);
	}

	public override bool CanLoadProgress()
	{
		return _canLoadProgress;
	}

	public override Task GameInit()
	{
		return RecordAsync("GameInit");
	}

	public override Task GameInitFromProgress()
	{
		return RecordAsync("GameInitFromProgress");
	}

	public override Task GameEntry()
	{
		return RecordAsync("GameEntry");
	}

	public override Task GameReady()
	{
		return RecordAsync("GameReady");
	}

	public override Task GameStart()
	{
		return RecordAsync("GameStart");
	}

	public override Task GameStartFromProgress()
	{
		return RecordAsync("GameStartFromProgress");
	}

	public override void Process(double delta)
	{
		BattleFeatureFullTimingProbeLog.Add("Process:" + _id);
	}

	public override void GameFail()
	{
		BattleFeatureFullTimingProbeLog.Add("GameFail:" + _id);
	}

	public override void ZombieEnterHouse(TowerDefenseCharacter character)
	{
		BattleFeatureFullTimingProbeLog.Add("ZombieEnterHouse:" + _id);
	}

	public override void Destroy()
	{
		BattleFeatureFullTimingProbeLog.Add("Destroy:" + _id);
		base.Destroy();
	}

	private async Task RecordAsync(string phase)
	{
		BattleFeatureFullTimingProbeLog.Add(phase + ".begin:" + _id);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		if (base.IsLifetimeActive)
		{
			BattleFeatureFullTimingProbeLog.Add(phase + ".end:" + _id);
		}
	}
}
