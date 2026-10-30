using Godot;
using Godot.Collections;

[GlobalClass]
public partial class TowerDefenseLevelEventGravestoneCreateRandom : TowerDefenseLevelEventBase
{
	[Export(PropertyHint.None, "")]
	public Array gravestoneNames = new Array { "GraveStoneDefault" };

	[Export(PropertyHint.None, "")]
	public int gravestoneNum = 5;

	[Export(PropertyHint.None, "")]
	public Vector4I gravestonePos = new Vector4I(3, 1, 9, 5);

	public override string _GetName()
	{
		return "LEVLE_EVENT_GRAVESTONE_CREATE_RANDOM";
	}

	public override void Execute()
	{
		if (!(gravestonePos == new Vector4I(-1, -1, -1, -1)) && (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost))
		{
			if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.GetCurrentMap()))
			{
				TowerDefenseManager.GetMapFeature()?.TryEnqueuePendingMapAction(ExecuteInternal);
			}
			else
			{
				ExecuteInternal();
			}
		}
	}

	private void ExecuteInternal()
	{
		Array<Vector2I> array = new Array<Vector2I>();
		for (int i = gravestonePos.X; i <= gravestonePos.Z; i++)
		{
			for (int j = gravestonePos.Y; j <= gravestonePos.W; j++)
			{
				Vector2I vector2I = new Vector2I(i, j);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(vector2I);
				bool flag = true;
				if (GodotObject.IsInstanceValid(mapCell.GetSurround()))
				{
					flag = false;
				}
				else
				{
					foreach (Variant gravestoneName in gravestoneNames)
					{
						TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig((string)gravestoneName);
						if (!CanCreateGravestoneAtCell(mapCell, packetConfig))
						{
							flag = false;
							break;
						}
					}
				}
				if (flag)
				{
					array.Add(vector2I);
				}
			}
		}
		for (int num = Mathf.Min(array.Count, gravestoneNum); num > 0; num--)
		{
			TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig((string)gravestoneNames[GD.RandRange(0, gravestoneNames.Count - 1)]);
			Vector2I vector2I2 = array[GD.RandRange(0, array.Count - 1)];
			TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(vector2I2);
			if (CanCreateGravestoneAtCell(mapCell2, packetConfig2))
			{
				TowerDefenseCharacter towerDefenseCharacter = packetConfig2.Plant(vector2I2, playAudio: false);
				if (GodotObject.IsInstanceValid(towerDefenseCharacter))
				{
					mapCell2.ReserveRandomGravestonePlacementUntilDeferredBind();
				}
				if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
				{
					TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
					if (GodotObject.IsInstanceValid(currentControl) && GodotObject.IsInstanceValid(towerDefenseCharacter))
					{
						int nextSyncId = currentControl.GetNextSyncId();
						currentControl.RegisterSyncCharacter(nextSyncId, towerDefenseCharacter);
						MultiPlayerManager.Instance.SendSpawnCharacterAt(packetConfig2.saveKey, vector2I2.X, vector2I2.Y, nextSyncId);
					}
				}
			}
			array.Remove(vector2I2);
		}
	}

	private static bool CanCreateGravestoneAtCell(TowerDefenseCellInstance cell, TowerDefensePacketConfig packet)
	{
		if (!GodotObject.IsInstanceValid(cell) || !GodotObject.IsInstanceValid(packet))
		{
			return false;
		}
		if (!cell.CanPacketPlant(packet))
		{
			return false;
		}
		if (cell.HasPendingRandomGravestonePlacement())
		{
			return false;
		}
		if (HasPendingPreSpawnGravestoneReservation(cell.gridPos))
		{
			return false;
		}
		if (packet.characterConfig.name != "GraveStoneTargetQX")
		{
			return true;
		}
		bool flag = false;
		bool flag2 = false;
		foreach (TowerDefenseCharacter item in cell.GetCharacterListSave())
		{
			if (GodotObject.IsInstanceValid(item) && item is TowerDefensePlant)
			{
				flag = true;
				if ((item.config.physiqueTypeFlags & 2) != 0)
				{
					flag2 = true;
				}
			}
		}
		return !flag || flag2;
	}

	private static bool HasPendingPreSpawnGravestoneReservation(Vector2I gridPos)
	{
		TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
		if (!GodotObject.IsInstanceValid(currentControl))
		{
			return false;
		}
		TowerDefenseBattleFeaturePreSpawn feature = currentControl.GetFeature<TowerDefenseBattleFeaturePreSpawn>("PreSpawn");
		if (GodotObject.IsInstanceValid(feature))
		{
			return feature.HasPendingGravestoneReservation(gridPos);
		}
		return false;
	}

	public override void Init(Dictionary valueDictionary)
	{
		gravestoneNames = valueDictionary.GetValueOrDefault("GravestoneNames", new Array()).AsGodotArray();
		gravestoneNum = valueDictionary.GetValueOrDefault("GravestoneNum", 1).AsInt32();
		Dictionary dictionary = valueDictionary.GetValueOrDefault("GravestonePos", new Dictionary()).AsGodotDictionary();
		gravestonePos = new Vector4I(dictionary.GetValueOrDefault("x", -1).AsInt32(), dictionary.GetValueOrDefault("y", -1).AsInt32(), dictionary.GetValueOrDefault("z", -1).AsInt32(), dictionary.GetValueOrDefault("w", -1).AsInt32());
	}

	public override Dictionary Export()
	{
		return new Dictionary
		{
			["EventName"] = "GravestoneCreateRandom",
			["Value"] = new Dictionary
			{
				["GravestoneNames"] = gravestoneNames,
				["GravestoneNum"] = gravestoneNum,
				["GravestonePos"] = new Dictionary
				{
					["x"] = gravestonePos.X,
					["y"] = gravestonePos.Y,
					["z"] = gravestonePos.Z,
					["w"] = gravestonePos.W
				}
			}
		};
	}

	public override Dictionary GetProperty()
	{
		Dictionary property = base.GetProperty();
		property["随机创建墓碑"] = new Dictionary
		{
			["数量"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Int",
				["Property"] = "gravestoneNum",
				["Rest"] = 5
			},
			["范围"] = new Dictionary
			{
				["Object"] = this,
				["Type"] = "Vector4I",
				["Property"] = "gravestonePos",
				["Rest"] = new Vector4I(3, 1, 9, 5)
			}
		};
		return property;
	}
}
