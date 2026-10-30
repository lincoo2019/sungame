using Godot;
using Godot.Collections;

public sealed class PlantGridReplicator : NetworkReplicatorBase
{
	private const double PlantFullSyncInterval = 5.0;

	private readonly IPlantGridNetworkContext _plantContext;

	private double _syncTimer;

	public PlantGridReplicator()
	{
	}

	public PlantGridReplicator(IPlantGridNetworkContext plantContext)
	{
		_plantContext = plantContext;
	}

	public override void Process(double delta)
	{
		if (base.Context != null && base.Context.IsMultiplayerActive && _plantContext != null && base.Context.IsHost)
		{
			_syncTimer += delta;
			if (_syncTimer >= 5.0)
			{
				_syncTimer = 0.0;
				BroadcastSnapshot();
			}
		}
	}

	public void BroadcastSnapshot()
	{
		if (base.Context != null && base.Context.IsMultiplayerActive && _plantContext != null && _plantContext.IsHost)
		{
			_plantContext.SendPlantGridSnapshot(Json.Stringify(BuildSnapshot()));
		}
	}

	public Array BuildSnapshot()
	{
		Array array = new Array();
		if (_plantContext == null)
		{
			return array;
		}
		foreach (Variant plant in _plantContext.GetPlants())
		{
			if (plant.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy)
			{
				Dictionary dictionary = new Dictionary
				{
					["gx"] = towerDefenseCharacter.gridPos.X,
					["gy"] = towerDefenseCharacter.gridPos.Y,
					["n"] = (GodotObject.IsInstanceValid(towerDefenseCharacter.packet) ? towerDefenseCharacter.packet.saveKey : ""),
					["si"] = towerDefenseCharacter.syncId,
					["owner"] = towerDefenseCharacter.EconomyOwnerAccountId.ToString()
				};
				array.Add(dictionary);
			}
		}
		foreach (Variant gravestone in _plantContext.GetGravestones())
		{
			GodotObject godotObject = gravestone.AsGodotObject();
			if (GodotObject.IsInstanceValid(godotObject) && godotObject is TowerDefenseGravestone { isDestroy: false } towerDefenseGravestone)
			{
				Dictionary dictionary2 = new Dictionary
				{
					["gx"] = towerDefenseGravestone.gridPos.X,
					["gy"] = towerDefenseGravestone.gridPos.Y,
					["n"] = (GodotObject.IsInstanceValid(towerDefenseGravestone.packet) ? towerDefenseGravestone.packet.saveKey : ""),
					["si"] = towerDefenseGravestone.syncId,
					["gt"] = true,
					["owner"] = towerDefenseGravestone.EconomyOwnerAccountId.ToString()
				};
				array.Add(dictionary2);
			}
		}
		return array;
	}

	public void ApplySnapshot(Variant plantsDataVariant)
	{
		if (_plantContext == null || _plantContext.IsHost || plantsDataVariant.VariantType != Variant.Type.Array)
		{
			return;
		}
		Array array = plantsDataVariant.AsGodotArray();
		Dictionary dictionary = new Dictionary();
		foreach (Variant item in array)
		{
			if (item.VariantType == Variant.Type.Dictionary)
			{
				Dictionary dictionary2 = item.AsGodotDictionary();
				int num = dictionary2.GetValueOrDefault("gx", 0).AsInt32();
				int num2 = dictionary2.GetValueOrDefault("gy", 0).AsInt32();
				string text = dictionary2.GetValueOrDefault("n", "").AsString();
				int num3 = dictionary2.GetValueOrDefault("si", -1).AsInt32();
				bool flag = dictionary2.GetValueOrDefault("gt", false).AsBool();
				string text2 = dictionary2.GetValueOrDefault("owner", "").AsString();
				long num4 = EncodeGridKey(num, num2, flag);
				dictionary[num4] = new Dictionary
				{
					["gx"] = num,
					["gy"] = num2,
					["n"] = text,
					["si"] = num3,
					["gt"] = flag,
					["owner"] = text2
				};
			}
		}
		Dictionary dictionary3 = new Dictionary();
		foreach (Variant plant in _plantContext.GetPlants())
		{
			if (plant.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.isDestroy)
			{
				long num5 = EncodeGridKey(towerDefenseCharacter.gridPos.X, towerDefenseCharacter.gridPos.Y, isGravestone: false);
				dictionary3[num5] = towerDefenseCharacter;
			}
		}
		foreach (Variant gravestone in _plantContext.GetGravestones())
		{
			GodotObject godotObject = gravestone.AsGodotObject();
			if (GodotObject.IsInstanceValid(godotObject) && godotObject is TowerDefenseGravestone { isDestroy: false } towerDefenseGravestone)
			{
				long num6 = EncodeGridKey(towerDefenseGravestone.gridPos.X, towerDefenseGravestone.gridPos.Y, isGravestone: true);
				dictionary3[num6] = towerDefenseGravestone;
			}
		}
		foreach (Variant key7 in dictionary.Keys)
		{
			Dictionary dictionary4 = dictionary[key7].AsGodotDictionary();
			if (!dictionary3.ContainsKey(key7))
			{
				PlantSnapshotEntry(dictionary4);
				continue;
			}
			TowerDefenseCharacter towerDefenseCharacter2 = dictionary3[key7].AsGodotObject() as TowerDefenseCharacter;
			string obj = ((GodotObject.IsInstanceValid(towerDefenseCharacter2) && GodotObject.IsInstanceValid(towerDefenseCharacter2.packet)) ? towerDefenseCharacter2.packet.saveKey : "");
			int num7 = (GodotObject.IsInstanceValid(towerDefenseCharacter2) ? towerDefenseCharacter2.syncId : (-1));
			string text3 = (GodotObject.IsInstanceValid(towerDefenseCharacter2) ? towerDefenseCharacter2.EconomyOwnerAccountId.ToString() : "");
			if (!(obj == dictionary4["n"].AsString()) || num7 != dictionary4["si"].AsInt32() || !(text3 == dictionary4.GetValueOrDefault("owner", "").AsString()))
			{
				_plantContext.DestroyRemoteCharacter(towerDefenseCharacter2);
				PlantSnapshotEntry(dictionary4);
			}
		}
		foreach (Variant key8 in dictionary3.Keys)
		{
			if (!dictionary.ContainsKey(key8))
			{
				TowerDefenseCharacter character = dictionary3[key8].AsGodotObject() as TowerDefenseCharacter;
				_plantContext.DestroyRemoteCharacter(character);
			}
		}
	}

	private void PlantSnapshotEntry(Dictionary hostPlant)
	{
		string text = hostPlant.GetValueOrDefault("n", "").AsString();
		if (!(text == ""))
		{
			Vector2I gridPos = new Vector2I(hostPlant.GetValueOrDefault("gx", 0).AsInt32(), hostPlant.GetValueOrDefault("gy", 0).AsInt32());
			int syncId = hostPlant.GetValueOrDefault("si", -1).AsInt32();
			if (EconomyAccountId.TryParse(hostPlant.GetValueOrDefault("owner", "").AsString(), out var accountId))
			{
				_plantContext.PlantAt(accountId, text, gridPos, syncId);
			}
			else
			{
				_plantContext.PlantAt(text, gridPos, syncId);
			}
		}
	}

	private static long EncodeGridKey(int gx, int gy, bool isGravestone)
	{
		long num = ((long)(gx & 0x7FFF) << 16) | (gy & 0xFFFF);
		if (!isGravestone)
		{
			return num;
		}
		return num | 0x80000000u;
	}
}
