using System.Collections.Generic;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureMower : TowerDefenseBattleFeature
{
	private sealed class MowerSyncState
	{
		public int SyncId = -1;

		public bool Running;

		public Vector2 Position;

		public bool HasPosition;
	}

	public const int GridMaxSize = 51;

	private static PackedScene _towerDefenseMowerManager;

	public TowerDefenseMowerManager mowerManager;

	public TowerDefenseBattleFeatureMowerConfig config;

	public Array<TowerDefenseMower> mowerLine = new Array<TowerDefenseMower>();

	public bool mowerHasRun;

	public Array<TowerDefenseCharacter> targetZombieLine = new Array<TowerDefenseCharacter>();

	private static PackedScene TOWER_DEFENSE_MOWER_MANAGER => _towerDefenseMowerManager ?? (_towerDefenseMowerManager = GD.Load<PackedScene>("uid://c0xrja6fjw4rr"));

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleFeatureMowerConfig();
		config.Init(data);
		mowerLine.Clear();
		targetZombieLine.Clear();
		mowerLine.Resize(51);
		targetZombieLine.Resize(51);
		mowerManager = TOWER_DEFENSE_MOWER_MANAGER.Instantiate<TowerDefenseMowerManager>(PackedScene.GenEditState.Disabled);
		mowerManager.mowerFeature = this;
		mowerManager.config = config;
		control.AddNode(mowerManager);
	}

	public void MowerInit()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return;
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (mapFeature.lineUse[i])
			{
				CreateMower(i);
			}
		}
	}

	public TowerDefenseMower CreateMower(int line, int syncId = -1)
	{
		if (!IsValidLine(line))
		{
			return null;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(mowerLine[line]))
		{
			return null;
		}
		string text = config.mowerPacketName;
		if (text == "")
		{
			text = GameSaveManager.Instance.GetKeyValue("CurrentMower").AsString();
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		if (mapFeature.LineHasType(line, TowerDefenseEnum.PLANTGRIDTYPE.WATER))
		{
			packetConfig = TowerDefenseManager.GetPacketConfig(config.waterMowerPacketName);
		}
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, line)) + new Vector2((float)config.mowerSpawnOffsetX, 0f);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseMower towerDefenseMower = packetConfig.Create(pos, new Vector2I(0, line)) as TowerDefenseMower;
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(1, line));
		if (!GodotObject.IsInstanceValid(towerDefenseMower) || !GodotObject.IsInstanceValid(characterNode) || !GodotObject.IsInstanceValid(mapCell))
		{
			if (GodotObject.IsInstanceValid(towerDefenseMower))
			{
				towerDefenseMower.QueueFree();
			}
			return null;
		}
		towerDefenseMower.characterFilter = true;
		towerDefenseMower.OnRunning += MowerRun;
		towerDefenseMower.groundHeight = mapCell.GetGroundHeight(0.0);
		towerDefenseMower.z = towerDefenseMower.groundHeight;
		characterNode.AddChild(towerDefenseMower, forceReadableName: false, Node.InternalMode.Disabled);
		mowerLine[line] = towerDefenseMower;
		RegisterNetworkCharacter(towerDefenseMower, ref syncId);
		return towerDefenseMower;
	}

	public void MowerRun(TowerDefenseMower mower)
	{
		if (GodotObject.IsInstanceValid(mower))
		{
			mower.OnRunning -= MowerRun;
			int num = mowerLine.IndexOf(mower);
			if (num >= 0)
			{
				mowerLine[num] = null;
			}
			mowerHasRun = true;
		}
	}

	public void IZM2Init()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return;
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (mapFeature.lineUse[i])
			{
				CreateTargetZombie(i);
			}
		}
	}

	public TowerDefenseCharacter CreateTargetZombie(int line, int syncId = -1)
	{
		if (!IsValidLine(line))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(targetZombieLine[line]))
		{
			return null;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(config.targetZombiePacketName);
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(packetConfig) || !GodotObject.IsInstanceValid(instance))
		{
			return null;
		}
		Vector2 pos = new Vector2((float)(instance.GetMapGroundRight() + config.targetSpawnOffsetX), TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, line)).Y);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Create(pos, new Vector2I(0, line));
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(1, line));
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || !GodotObject.IsInstanceValid(characterNode) || !GodotObject.IsInstanceValid(mapCell))
		{
			if (GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				towerDefenseCharacter.QueueFree();
			}
			return null;
		}
		towerDefenseCharacter.OnDestroy += TargetZombieDestroy;
		towerDefenseCharacter.groundHeight = mapCell.GetGroundHeight(0.0);
		towerDefenseCharacter.z = towerDefenseCharacter.groundHeight;
		characterNode.AddChild(towerDefenseCharacter, forceReadableName: false, Node.InternalMode.Disabled);
		towerDefenseCharacter.Rise();
		targetZombieLine[line] = towerDefenseCharacter;
		RegisterNetworkCharacter(towerDefenseCharacter, ref syncId);
		return towerDefenseCharacter;
	}

	public void CheckIZM2Fail()
	{
		if ((Global.IsMultiplayerMode && (MultiPlayerManager.Instance == null || !MultiPlayerManager.Instance.isHost)) || (CommandManager.Instance.debug && CommandManager.Instance.debugNoLose))
		{
			return;
		}
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature))
		{
			return;
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (mapFeature.lineUse[i] && GodotObject.IsInstanceValid(targetZombieLine[i]))
			{
				TowerDefenseManager.CurrentControl.GameFail(null);
			}
		}
	}

	public void TargetZombieDestroy(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			character.OnDestroy -= TargetZombieDestroy;
		}
		int num = targetZombieLine.IndexOf(character);
		if (num >= 0)
		{
			targetZombieLine[num] = null;
		}
		if (base.IsLifetimeActive && (!Global.IsMultiplayerMode || (MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost)) && (!CommandManager.Instance.debug || !CommandManager.Instance.debugNoLose))
		{
			TowerDefenseInGameLevelControl instance = TowerDefenseInGameLevelControl.instance;
			if (!GodotObject.IsInstanceValid(instance) || !instance.awardCreate)
			{
				TowerDefenseManager.CurrentControl.GameFail(null);
			}
		}
	}

	public override void GameFail()
	{
		foreach (TowerDefenseMower item in mowerLine)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.ProcessMode = Node.ProcessModeEnum.Disabled;
			}
		}
	}

	public override Dictionary SyncSerialize()
	{
		Array array = new Array();
		foreach (KeyValuePair<int, TowerDefenseMower> item in GetLiveMowersByLine())
		{
			TowerDefenseMower value = item.Value;
			Vector2 logicalGlobalPosition = value.GetLogicalGlobalPosition();
			array.Add(new Dictionary
			{
				["line"] = item.Key,
				["sync_id"] = value.syncId,
				["running"] = value.run,
				["x"] = logicalGlobalPosition.X,
				["y"] = logicalGlobalPosition.Y
			});
		}
		Array array2 = new Array();
		for (int i = 1; i < targetZombieLine.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = targetZombieLine[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy && !towerDefenseCharacter.IsDie())
			{
				array2.Add(new Dictionary
				{
					["line"] = i,
					["sync_id"] = towerDefenseCharacter.syncId
				});
			}
		}
		return new Dictionary
		{
			["mowers"] = array,
			["targets"] = array2,
			["mower_has_run"] = mowerHasRun
		};
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		mowerHasRun = _data.GetValueOrDefault("mower_has_run", mowerHasRun).AsBool();
		if (_data.TryGetValue("mowers", out var value) && value.VariantType == Variant.Type.Array)
		{
			System.Collections.Generic.Dictionary<int, MowerSyncState> dictionary = new System.Collections.Generic.Dictionary<int, MowerSyncState>();
			foreach (Variant item in value.AsGodotArray())
			{
				if (item.VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary2 = item.AsGodotDictionary();
					int num = dictionary2.GetValueOrDefault("line", 0).AsInt32();
					if (IsValidLine(num))
					{
						dictionary[num] = new MowerSyncState
						{
							SyncId = dictionary2.GetValueOrDefault("sync_id", -1).AsInt32(),
							Running = dictionary2.GetValueOrDefault("running", false).AsBool(),
							Position = new Vector2((float)dictionary2.GetValueOrDefault("x", 0.0).AsDouble(), (float)dictionary2.GetValueOrDefault("y", 0.0).AsDouble()),
							HasPosition = (dictionary2.ContainsKey("x") && dictionary2.ContainsKey("y"))
						};
					}
				}
			}
			System.Collections.Generic.Dictionary<int, TowerDefenseMower> liveMowersByLine = GetLiveMowersByLine();
			foreach (KeyValuePair<int, TowerDefenseMower> item2 in liveMowersByLine)
			{
				if (!dictionary.ContainsKey(item2.Key))
				{
					DestroyRemoteMower(item2.Key, item2.Value);
				}
			}
			foreach (KeyValuePair<int, MowerSyncState> item3 in dictionary)
			{
				if (!liveMowersByLine.TryGetValue(item3.Key, out var value2) || !GodotObject.IsInstanceValid(value2))
				{
					value2 = CreateMower(item3.Key, item3.Value.SyncId);
				}
				else
				{
					BindNetworkCharacter(value2, item3.Value.SyncId);
				}
				if (!GodotObject.IsInstanceValid(value2))
				{
					continue;
				}
				if (!item3.Value.Running && value2.run)
				{
					DestroyRemoteMower(item3.Key, value2);
					value2 = CreateMower(item3.Key, item3.Value.SyncId);
					if (!GodotObject.IsInstanceValid(value2))
					{
						continue;
					}
				}
				if (item3.Value.HasPosition && float.IsFinite(item3.Value.Position.X) && float.IsFinite(item3.Value.Position.Y))
				{
					value2.SetLogicalGlobalPosition(item3.Value.Position);
				}
				if (item3.Value.Running && !value2.run)
				{
					value2.Run();
					value2.run = true;
				}
			}
		}
		if (_data.TryGetValue("targets", out var value3) && value3.VariantType == Variant.Type.Array)
		{
			ApplyTargetState(value3.AsGodotArray());
		}
	}

	private System.Collections.Generic.Dictionary<int, TowerDefenseMower> GetLiveMowersByLine()
	{
		System.Collections.Generic.Dictionary<int, TowerDefenseMower> dictionary = new System.Collections.Generic.Dictionary<int, TowerDefenseMower>();
		foreach (TowerDefenseMower item in mowerLine)
		{
			if (GodotObject.IsInstanceValid(item) && !item.isDestroy && !item.IsDie())
			{
				dictionary[item.gridPos.Y] = item;
			}
		}
		if (Engine.GetMainLoop() is SceneTree sceneTree)
		{
			foreach (Node item2 in sceneTree.GetNodesInGroup("Mower"))
			{
				if (item2 is TowerDefenseMower towerDefenseMower && GodotObject.IsInstanceValid(towerDefenseMower) && !towerDefenseMower.isDestroy && !towerDefenseMower.IsDie())
				{
					dictionary[towerDefenseMower.gridPos.Y] = towerDefenseMower;
				}
			}
		}
		return dictionary;
	}

	private void ApplyTargetState(Array targets)
	{
		System.Collections.Generic.Dictionary<int, int> dictionary = new System.Collections.Generic.Dictionary<int, int>();
		foreach (Variant target in targets)
		{
			if (target.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary2 = target.AsGodotDictionary();
				int num = dictionary2.GetValueOrDefault("line", 0).AsInt32();
				if (IsValidLine(num))
				{
					dictionary[num] = dictionary2.GetValueOrDefault("sync_id", -1).AsInt32();
				}
			}
		}
		for (int i = 1; i < targetZombieLine.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = targetZombieLine[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !dictionary.ContainsKey(i))
			{
				DestroyRemoteTarget(i, towerDefenseCharacter);
			}
		}
		foreach (KeyValuePair<int, int> item in dictionary)
		{
			TowerDefenseCharacter towerDefenseCharacter2 = targetZombieLine[item.Key];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter2) || towerDefenseCharacter2.isDestroy)
			{
				towerDefenseCharacter2 = CreateTargetZombie(item.Key, item.Value);
			}
			else
			{
				BindNetworkCharacter(towerDefenseCharacter2, item.Value);
			}
		}
	}

	private void RegisterNetworkCharacter(TowerDefenseCharacter character, ref int syncId)
	{
		if (Global.IsMultiplayerMode && GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(character))
		{
			if (syncId < 0 && MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost)
			{
				syncId = control.GetNextSyncId();
			}
			if (syncId >= 0)
			{
				control.RegisterSyncCharacter(syncId, character);
			}
		}
	}

	private void BindNetworkCharacter(TowerDefenseCharacter character, int syncId)
	{
		if (syncId >= 0 && GodotObject.IsInstanceValid(control) && GodotObject.IsInstanceValid(character) && character.syncId != syncId)
		{
			control.DetachSyncCharacter(character);
			control.RegisterSyncCharacter(syncId, character);
		}
	}

	private void DestroyRemoteMower(int line, TowerDefenseMower mower)
	{
		if (IsValidLine(line) && mowerLine[line] == mower)
		{
			mowerLine[line] = null;
		}
		mower.OnRunning -= MowerRun;
		MarkRemoteDestroy(mower);
	}

	private void DestroyRemoteTarget(int line, TowerDefenseCharacter target)
	{
		if (IsValidLine(line) && targetZombieLine[line] == target)
		{
			targetZombieLine[line] = null;
		}
		target.OnDestroy -= TargetZombieDestroy;
		MarkRemoteDestroy(target);
	}

	private static void MarkRemoteDestroy(TowerDefenseCharacter character)
	{
		if (GodotObject.IsInstanceValid(character))
		{
			DestroyComponent destroyComponent = character.destroyComponent;
			if (destroyComponent != null && !destroyComponent.IsReleased)
			{
				character.destroyComponent.isRemoteDestroy = true;
			}
			character.Destroy();
		}
	}

	public override Dictionary SaveFeature()
	{
		return new Dictionary { ["mowerHasRun"] = mowerHasRun };
	}

	public override void LoadFeature(Dictionary _data, TowerDefenseLevelSaveConfigCSharp _owner)
	{
		mowerHasRun = _data.GetValueOrDefault("mowerHasRun", false).AsBool();
	}

	private bool IsValidLine(int line)
	{
		if (line > 0 && line < mowerLine.Count)
		{
			return line < targetZombieLine.Count;
		}
		return false;
	}

	public override void Destroy()
	{
		foreach (TowerDefenseMower item in mowerLine)
		{
			if (GodotObject.IsInstanceValid(item))
			{
				item.OnRunning -= MowerRun;
			}
		}
		foreach (TowerDefenseCharacter item2 in targetZombieLine)
		{
			if (GodotObject.IsInstanceValid(item2))
			{
				item2.OnDestroy -= TargetZombieDestroy;
			}
		}
		mowerLine.Clear();
		targetZombieLine.Clear();
		if (GodotObject.IsInstanceValid(mowerManager))
		{
			mowerManager.DisposeBattleState();
			mowerManager.QueueFree();
		}
		mowerManager = null;
		config = null;
		base.Destroy();
	}
}
