using Godot;

[Tool]
public partial class TowerDefenseWorkerSign : TowerDefenseGravestone
{
	private TowerDefenseBattleFeatureWave _wave;

	public override void _Ready()
	{
		base._Ready();
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame)
		{
			return;
		}
		RemoveFromGroup("Gravestone");
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		BindWave();
		foreach (Node item in GetTree().GetNodesInGroup("WorkerSign"))
		{
			if (item is TowerDefenseWorkerSign towerDefenseWorkerSign && towerDefenseWorkerSign != this && towerDefenseWorkerSign.inGame && !towerDefenseWorkerSign.editorPreviewMode && towerDefenseWorkerSign.gridPos.Y == base.gridPos.Y && !towerDefenseWorkerSign.die && !towerDefenseWorkerSign.isDestroy && !towerDefenseWorkerSign.IsQueuedForDeletion())
			{
				towerDefenseWorkerSign.Destroy();
			}
		}
	}

	public override void ActivateGameplayProcessing()
	{
		base.ActivateGameplayProcessing();
		BindWave();
	}

	private void BindWave()
	{
		if (Engine.IsEditorHint() || editorPreviewMode || !inGame || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost))
		{
			return;
		}
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		TowerDefenseBattleFeatureWave towerDefenseBattleFeatureWave = (GodotObject.IsInstanceValid(currentControl) ? (currentControl.GetFeature("Wave") as TowerDefenseBattleFeatureWave) : null);
		if (_wave != towerDefenseBattleFeatureWave)
		{
			UnbindWave();
			_wave = towerDefenseBattleFeatureWave;
			if (GodotObject.IsInstanceValid(_wave))
			{
				_wave.OnCollectWaveReinforcements += AddWaveReinforcement;
			}
		}
	}

	private void UnbindWave()
	{
		if (GodotObject.IsInstanceValid(_wave))
		{
			_wave.OnCollectWaveReinforcements -= AddWaveReinforcement;
		}
		_wave = null;
	}

	private void AddWaveReinforcement(TowerDefenseBattleFeatureWave.AddWaveReinforcementHandler addSpawn)
	{
		if (inGame && !editorPreviewMode && !die && !isDestroy && !IsQueuedForDeletion() && GodotObject.IsInstanceValid(instance) && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			TowerDefenseLevelSpawnConfig towerDefenseLevelSpawnConfig = new TowerDefenseLevelSpawnConfig
			{
				zombie = "ZombieWorker"
			};
			if (instance.hypnoses)
			{
				towerDefenseLevelSpawnConfig.overrideVal = new TowerDefenseCharacterOverride
				{
					hypnoses = true
				};
			}
			addSpawn(base.gridPos.Y, towerDefenseLevelSpawnConfig);
		}
	}

	public override void _ExitTree()
	{
		UnbindWave();
		base._ExitTree();
	}

	public static TowerDefenseWorkerSign FindInLane(SceneTree tree, int lane, bool? hypnoses = null)
	{
		if (tree == null)
		{
			return null;
		}
		foreach (Node item in tree.GetNodesInGroup("WorkerSign"))
		{
			if (item is TowerDefenseWorkerSign { inGame: not false, editorPreviewMode: false, die: false, isDestroy: false } towerDefenseWorkerSign && !towerDefenseWorkerSign.IsQueuedForDeletion() && towerDefenseWorkerSign.gridPos.Y == lane && (!hypnoses.HasValue || (GodotObject.IsInstanceValid(towerDefenseWorkerSign.instance) && towerDefenseWorkerSign.instance.hypnoses == hypnoses.Value)))
			{
				return towerDefenseWorkerSign;
			}
		}
		return null;
	}
}
