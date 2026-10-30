using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseBattleFeatureBrain : TowerDefenseBattleFeature
{
	public const int GridMaxSize = 51;

	public Array<TowerDefenseItem> brainLine = new Array<TowerDefenseItem>();

	public TowerDefenseBattleFeatureBrainConfig config;

	private TowerDefensePacketConfig _brainPacket;

	private TowerDefenseBattleFeatureProgress _progressFeature;

	public override void Init(Dictionary _data)
	{
		base.Init(_data);
		config = new TowerDefenseBattleFeatureBrainConfig();
		config.Init(data);
		brainLine.Clear();
		brainLine.Resize(51);
	}

	public override void OnReady()
	{
		_brainPacket = TowerDefenseManager.GetPacketConfig(config.packetName);
		_progressFeature = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
	}

	public override Task GameReady()
	{
		return Task.CompletedTask;
	}

	public override void Destroy()
	{
		for (int i = 0; i < brainLine.Count; i++)
		{
			if (brainLine[i] is TowerDefenseItemBrain towerDefenseItemBrain && GodotObject.IsInstanceValid(towerDefenseItemBrain))
			{
				towerDefenseItemBrain.OnBrainDestroy -= BrainDestroy;
			}
		}
		_brainPacket = null;
		config = null;
		_progressFeature = null;
		brainLine.Clear();
		base.Destroy();
	}

	public override Dictionary SyncSerialize()
	{
		NormalizeBrainLineSize();
		Array array = new Array();
		Array array2 = new Array();
		for (int i = 0; i < 51; i++)
		{
			if (GodotObject.IsInstanceValid(brainLine[i]) && !brainLine[i].isDestroy)
			{
				array.Add(i);
				array2.Add(new Dictionary
				{
					["line"] = i,
					["sync_id"] = brainLine[i].syncId
				});
			}
		}
		return new Dictionary
		{
			["brains"] = array2,
			["brain_lines"] = array
		};
	}

	public override void SyncDeserialize(Dictionary _data)
	{
		if (!_data.ContainsKey("brains") && !_data.ContainsKey("brain_lines"))
		{
			return;
		}
		NormalizeBrainLineSize();
		System.Collections.Generic.Dictionary<int, int> dictionary = new System.Collections.Generic.Dictionary<int, int>();
		if (_data.TryGetValue("brains", out var value) && value.VariantType == Variant.Type.Array)
		{
			foreach (Variant item in value.AsGodotArray())
			{
				if (item.VariantType == Variant.Type.Dictionary)
				{
					Dictionary dictionary2 = item.AsGodotDictionary();
					int num = dictionary2.GetValueOrDefault("line", -1).AsInt32();
					if ((uint)num < 51u)
					{
						dictionary[num] = dictionary2.GetValueOrDefault("sync_id", -1).AsInt32();
					}
				}
			}
		}
		else
		{
			foreach (Variant item2 in _data["brain_lines"].AsGodotArray())
			{
				int num2 = item2.AsInt32();
				if ((uint)num2 < 51u)
				{
					dictionary[num2] = -1;
				}
			}
		}
		for (int i = 0; i < 51; i++)
		{
			if (GodotObject.IsInstanceValid(brainLine[i]) && !brainLine[i].isDestroy && !dictionary.ContainsKey(i))
			{
				DestroyComponent destroyComponent = brainLine[i].destroyComponent;
				if (destroyComponent != null && !destroyComponent.IsReleased)
				{
					brainLine[i].destroyComponent.isRemoteDestroy = true;
				}
				brainLine[i].Destroy();
				brainLine[i] = null;
			}
		}
		foreach (KeyValuePair<int, int> item3 in dictionary)
		{
			int key = item3.Key;
			int value2 = item3.Value;
			TowerDefenseItem towerDefenseItem = brainLine[key];
			if (!GodotObject.IsInstanceValid(towerDefenseItem) || towerDefenseItem.isDestroy)
			{
				towerDefenseItem = CreateBrain(key, value2);
			}
			else if (value2 >= 0 && towerDefenseItem.syncId != value2 && GodotObject.IsInstanceValid(control))
			{
				control.DetachSyncCharacter(towerDefenseItem);
				control.RegisterSyncCharacter(value2, towerDefenseItem);
			}
		}
	}

	public void BrainInit()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (mapFeature == null)
		{
			return;
		}
		if (_brainPacket == null)
		{
			_brainPacket = TowerDefenseManager.GetPacketConfig(config.packetName);
		}
		for (int i = 1; i <= mapFeature.config.gridNum.Y; i++)
		{
			if (mapFeature.lineUse[i])
			{
				CreateBrain(i);
			}
		}
	}

	public TowerDefenseItem CreateBrain(int line, int syncId = -1)
	{
		if (line <= 0 || line >= 51)
		{
			return null;
		}
		NormalizeBrainLineSize();
		if (GodotObject.IsInstanceValid(brainLine[line]))
		{
			return null;
		}
		if (_brainPacket == null)
		{
			_brainPacket = TowerDefenseManager.GetPacketConfig(config.packetName);
		}
		if (!GodotObject.IsInstanceValid(_brainPacket))
		{
			return null;
		}
		Vector2 pos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, line)) + new Vector2((float)config.horizontalOffset, 0f);
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(1, line));
		if (!GodotObject.IsInstanceValid(characterNode) || !GodotObject.IsInstanceValid(mapCell))
		{
			return null;
		}
		TowerDefenseItemBrain towerDefenseItemBrain = _brainPacket.Create(pos, new Vector2I(0, line)) as TowerDefenseItemBrain;
		if (!GodotObject.IsInstanceValid(towerDefenseItemBrain))
		{
			return null;
		}
		towerDefenseItemBrain.characterFilter = config.characterFilter;
		BindBrainDestroySignal(towerDefenseItemBrain);
		towerDefenseItemBrain.groundHeight = mapCell.GetGroundHeight(0.0);
		towerDefenseItemBrain.z = towerDefenseItemBrain.groundHeight;
		characterNode.AddChild(towerDefenseItemBrain, forceReadableName: false, Node.InternalMode.Disabled);
		brainLine[line] = towerDefenseItemBrain;
		if (Global.IsMultiplayerMode && GodotObject.IsInstanceValid(control))
		{
			if (syncId < 0 && MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost)
			{
				syncId = control.GetNextSyncId();
			}
			if (syncId >= 0)
			{
				control.RegisterSyncCharacter(syncId, towerDefenseItemBrain);
			}
		}
		return towerDefenseItemBrain;
	}

	public void NormalizeBrainLineSize()
	{
		for (int i = 51; i < brainLine.Count; i++)
		{
			if (brainLine[i] is TowerDefenseItemBrain towerDefenseItemBrain && GodotObject.IsInstanceValid(towerDefenseItemBrain))
			{
				towerDefenseItemBrain.OnBrainDestroy -= BrainDestroy;
			}
		}
		if (brainLine.Count != 51)
		{
			brainLine.Resize(51);
		}
	}

	public void RebindBrainDestroySignals()
	{
		NormalizeBrainLineSize();
		for (int i = 0; i < brainLine.Count; i++)
		{
			if (brainLine[i] is TowerDefenseItemBrain towerDefenseItemBrain && GodotObject.IsInstanceValid(towerDefenseItemBrain))
			{
				BindBrainDestroySignal(towerDefenseItemBrain);
			}
			else if (!GodotObject.IsInstanceValid(brainLine[i]))
			{
				brainLine[i] = null;
			}
		}
	}

	private void BindBrainDestroySignal(TowerDefenseItemBrain brain)
	{
		brain.OnBrainDestroy -= BrainDestroy;
		brain.OnBrainDestroy += BrainDestroy;
	}

	public int GetAliveBrainCount()
	{
		NormalizeBrainLineSize();
		int num = 0;
		for (int i = 0; i < brainLine.Count; i++)
		{
			TowerDefenseItem towerDefenseItem = brainLine[i];
			if (GodotObject.IsInstanceValid(towerDefenseItem) && !towerDefenseItem.isDestroy && !towerDefenseItem.IsDie())
			{
				num++;
			}
		}
		return num;
	}

	public bool TryGetAliveBrain(int line, out TowerDefenseItemBrain brain)
	{
		NormalizeBrainLineSize();
		brain = null;
		if (line <= 0 || line >= brainLine.Count)
		{
			return false;
		}
		if (!(brainLine[line] is TowerDefenseItemBrain towerDefenseItemBrain) || !GodotObject.IsInstanceValid(towerDefenseItemBrain) || towerDefenseItemBrain.isDestroy || towerDefenseItemBrain.IsDie())
		{
			return false;
		}
		brain = towerDefenseItemBrain;
		return true;
	}

	public bool TryGetFirstAliveBrain(out TowerDefenseItem brain)
	{
		NormalizeBrainLineSize();
		for (int i = 0; i < brainLine.Count; i++)
		{
			brain = brainLine[i];
			if (GodotObject.IsInstanceValid(brain) && !brain.isDestroy && !brain.IsDie())
			{
				return true;
			}
		}
		brain = null;
		return false;
	}

	public void BrainDestroy(TowerDefenseItemBrain brain)
	{
		if (GodotObject.IsInstanceValid(brain))
		{
			int y = brain.gridPos.Y;
			if ((uint)y < (uint)brainLine.Count && brainLine[y] == brain)
			{
				brainLine[y] = null;
			}
			brain.OnBrainDestroy -= BrainDestroy;
		}
		if (!Global.IsMultiplayerMode || (MultiPlayerManager.Instance != null && MultiPlayerManager.Instance.isHost))
		{
			if (_progressFeature == null)
			{
				_progressFeature = GetFeature("Progress") as TowerDefenseBattleFeatureProgress;
			}
			_progressFeature?.IncrementPreviewWave();
		}
	}
}
