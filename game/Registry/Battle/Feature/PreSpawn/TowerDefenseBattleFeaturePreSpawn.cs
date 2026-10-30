using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeaturePreSpawn : TowerDefenseBattleFeature
{
	public TowerDefenseBattleFeaturePreSpawnConfig config;

	public Array<TowerDefenseCharacter> preSpawnList = new Array<TowerDefenseCharacter>();

	private bool _pendingPlacementReservations;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleFeaturePreSpawnConfig();
		config.Init(data);
		_pendingPlacementReservations = config.preSpawnList.Count > 0;
	}

	public override async Task GameEntry()
	{
		_ = 1;
		try
		{
			if (!base.IsLifetimeActive || config == null || (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost) || control.hasProgress || !control.isInit)
			{
				return;
			}
			TowerDefenseLevelConfig towerDefenseLevelConfig = control.levelConfig as TowerDefenseLevelConfig;
			if (towerDefenseLevelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM && towerDefenseLevelConfig.izmManager.shuffle && control.process is TowerDefenseBattleProcessIZM towerDefenseBattleProcessIZM)
			{
				List<TowerDefenseCharacter> list = await towerDefenseBattleProcessIZM.Execute(config.preSpawnList);
				if (!base.IsLifetimeActive)
				{
					DestroyPendingCharacters(list);
					return;
				}
				{
					foreach (TowerDefenseCharacter item in list)
					{
						RegisterPreSpawnCharacter(item);
						preSpawnList.Add(item);
					}
					return;
				}
			}
			List<TowerDefenseCharacter> list2 = await SpawnPreSpawnCharactersWithRetry(config.preSpawnList);
			if (!base.IsLifetimeActive)
			{
				DestroyPendingCharacters(list2);
				return;
			}
			foreach (TowerDefenseCharacter item2 in list2)
			{
				RegisterPreSpawnCharacter(item2);
				preSpawnList.Add(item2);
			}
		}
		finally
		{
			ReleasePendingPlacementReservationsDeferred();
		}
	}

	public bool HasPendingGravestoneReservation(Vector2I gridPos)
	{
		if (!_pendingPlacementReservations || config == null)
		{
			return false;
		}
		foreach (TowerDefenseLevelPreSpawnConfig preSpawn in config.preSpawnList)
		{
			if (GodotObject.IsInstanceValid(preSpawn) && !(preSpawn.gridPos != gridPos))
			{
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(preSpawn.packetName);
				if (GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig is TowerDefenseGravestoneConfig)
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ReleasePendingPlacementReservationsDeferred()
	{
		if (_pendingPlacementReservations)
		{
			Callable.From(() => _pendingPlacementReservations = false).CallDeferred();
		}
	}

	private async Task<List<TowerDefenseCharacter>> SpawnPreSpawnCharactersWithRetry(IList<TowerDefenseLevelPreSpawnConfig> preSpawnConfigs)
	{
		List<TowerDefenseCharacter> spawnedCharacters = new List<TowerDefenseCharacter>();
		List<TowerDefenseLevelPreSpawnConfig> pending = new List<TowerDefenseLevelPreSpawnConfig>(preSpawnConfigs);
		int retryPass = 0;
		while (pending.Count > 0 && retryPass < config.maxRetryPasses)
		{
			List<TowerDefenseLevelPreSpawnConfig> list = new List<TowerDefenseLevelPreSpawnConfig>();
			int num = 0;
			foreach (TowerDefenseLevelPreSpawnConfig item in pending)
			{
				TowerDefenseCharacter towerDefenseCharacter = item.SpawnCharacter();
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					list.Add(item);
					continue;
				}
				num++;
				spawnedCharacters.Add(towerDefenseCharacter);
				if (GodotObject.IsInstanceValid(item.characterOverride))
				{
					item.characterOverride.ExecuteCharacter(towerDefenseCharacter);
				}
			}
			pending = list;
			if (pending.Count == 0 || num == 0)
			{
				break;
			}
			retryPass++;
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			if (!base.IsLifetimeActive)
			{
				break;
			}
		}
		return spawnedCharacters;
	}

	private void RegisterPreSpawnCharacter(TowerDefenseCharacter character)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost && GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(character) && character.syncId < 0)
		{
			int nextSyncId = control.GetNextSyncId();
			control.RegisterSyncCharacter(nextSyncId, character);
		}
	}

	public override Task GameStart()
	{
		if (!base.IsLifetimeActive)
		{
			return Task.CompletedTask;
		}
		foreach (TowerDefenseCharacter preSpawn in preSpawnList)
		{
			if (!GodotObject.IsInstanceValid(preSpawn))
			{
				continue;
			}
			preSpawn.ActivateGameplayProcessing();
			preSpawn.CallDeferred("ActivateGameplayProcessing");
			preSpawn.PreSpawn();
			if (preSpawn is TowerDefensePlant towerDefensePlant)
			{
				if (towerDefensePlant.CanSleep())
				{
					towerDefensePlant.CallDeferred("Sleep");
				}
				else if (!towerDefensePlant.componentRunning)
				{
					towerDefensePlant.CallDeferred("Idle");
				}
			}
			if (preSpawn is TowerDefenseZombie towerDefenseZombie)
			{
				towerDefenseZombie.CallDeferred("Walk");
			}
		}
		preSpawnList.Clear();
		return Task.CompletedTask;
	}

	private static void DestroyPendingCharacters(IEnumerable<TowerDefenseCharacter> characters)
	{
		if (characters == null)
		{
			return;
		}
		foreach (TowerDefenseCharacter character in characters)
		{
			if (GodotObject.IsInstanceValid(character))
			{
				character.Destroy();
			}
		}
	}

	public override void Destroy()
	{
		_pendingPlacementReservations = false;
		DestroyPendingCharacters(preSpawnList);
		preSpawnList.Clear();
		config?.Clear();
		config = null;
		base.Destroy();
	}
}
