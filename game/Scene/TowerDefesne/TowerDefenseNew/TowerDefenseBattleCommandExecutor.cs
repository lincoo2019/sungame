using Godot;

public sealed class TowerDefenseBattleCommandExecutor
{
	private readonly TowerDefenseControlNew _control;

	public TowerDefenseBattleCommandExecutor(TowerDefenseControlNew control)
	{
		_control = control;
	}

	public bool RemovePlantAt(Vector2I gridPos)
	{
		foreach (Variant item in TowerDefenseManager.Instance.GetPlant())
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.gridPos == gridPos)
			{
				DestroyPlantFromNetwork(towerDefenseCharacter, shovel: false);
				return true;
			}
		}
		return false;
	}

	public bool ShovelPlantAt(Vector2I gridPos)
	{
		foreach (Variant item in TowerDefenseManager.Instance.GetPlant())
		{
			if (item.AsGodotObject() is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.gridPos == gridPos)
			{
				DestroyPlantFromNetwork(towerDefenseCharacter, shovel: true);
				return true;
			}
		}
		return false;
	}

	public bool MoveCharacter(int syncId, Vector2I fromGridPos, Vector2I toGridPos, string moveKind = "drag", double duration = 0.5)
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(_control) || !GodotObject.IsInstanceValid(instance) || syncId < 0 || fromGridPos == toGridPos || !instance.CheckMapGridPosIn(fromGridPos) || !instance.CheckMapGridPosIn(toGridPos) || !_control._syncCharacters.TryGetValue(syncId, out var value) || !GodotObject.IsInstanceValid(value) || value.die || value.gridPos != fromGridPos)
		{
			return false;
		}
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(fromGridPos);
		TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(toGridPos);
		if (!GodotObject.IsInstanceValid(mapCell) || !GodotObject.IsInstanceValid(mapCell2) || !CanUseMoveKind(value, mapCell, moveKind) || (moveKind != "magnet" && !mapCell2.CanMoveCharacterHere(value)))
		{
			return false;
		}
		duration = ((moveKind == "magnet") ? 2.0 : 0.5);
		value.EmitDestroy();
		mapCell.MoveCharacterToCell(value, mapCell2, duration);
		return true;
	}

	private bool CanUseMoveKind(TowerDefenseCharacter character, TowerDefenseCellInstance fromCell, string moveKind)
	{
		return moveKind switch
		{
			"drag" => character.componentManager?.GetRuntime<DragMoveComponent>() != null, 
			"magnet" => character is TowerDefensePlant && fromCell.characterList.Contains(character), 
			"glove" => character is TowerDefensePlant && GodotObject.IsInstanceValid(_control.GetFeature("Glove")) && fromCell.characterList.Contains(character), 
			_ => false, 
		};
	}

	public TowerDefenseCharacter PlantAt(string plantName, Vector2I gridPos, int syncId = -1, string overrideStr = "")
	{
		return PlantAtCore(default(EconomyAccountId), plantName, gridPos, syncId, overrideStr);
	}

	public TowerDefenseCharacter PlantAt(EconomyAccountId economyOwnerAccountId, string plantName, Vector2I gridPos, int syncId = -1, string overrideStr = "")
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return PlantAtCore(economyOwnerAccountId, plantName, gridPos, syncId, overrideStr);
	}

	public TowerDefenseCharacter PlantOnCharacter(string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId = -1, string overrideStr = "")
	{
		return PlantOnCharacterCore(default(EconomyAccountId), plantName, targetSyncId, placementKind, hypnoses, syncId, overrideStr);
	}

	public TowerDefenseCharacter PlantOnCharacter(EconomyAccountId economyOwnerAccountId, string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId = -1, string overrideStr = "")
	{
		if (!economyOwnerAccountId.IsValid)
		{
			return null;
		}
		return PlantOnCharacterCore(economyOwnerAccountId, plantName, targetSyncId, placementKind, hypnoses, syncId, overrideStr);
	}

	private TowerDefenseCharacter PlantOnCharacterCore(EconomyAccountId economyOwnerAccountId, string plantName, int targetSyncId, string placementKind, bool hypnoses, int syncId, string overrideStr)
	{
		if (!GodotObject.IsInstanceValid(_control) || targetSyncId < 0 || !_control._syncCharacters.TryGetValue(targetSyncId, out var value) || !IsValidPlacementTarget(value, placementKind))
		{
			return null;
		}
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(plantName);
		if (!GodotObject.IsInstanceValid(packetConfig) || (placementKind == "zombie" && (!packetConfig.canPlaceOnZombie || hypnoses != packetConfig.GetHypnoses())) || (placementKind == "plant" && (!GodotObject.IsInstanceValid(packetConfig.characterConfig) || !packetConfig.characterConfig.plantGridType.Contains(TowerDefenseEnum.PLANTGRIDTYPE.PLANT))) || (placementKind == "zombie" && value.camp != ((!hypnoses) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT)))
		{
			return null;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = null;
		TowerDefensePacketOverride towerDefensePacketOverride2 = packetConfig._override;
		try
		{
			if (overrideStr != "")
			{
				Variant variant = Json.ParseString(overrideStr);
				if (variant.VariantType == Variant.Type.Dictionary)
				{
					towerDefensePacketOverride = new TowerDefensePacketOverride();
					towerDefensePacketOverride.Init(variant.AsGodotDictionary());
					packetConfig._override = towerDefensePacketOverride;
				}
			}
			if (placementKind == "jala_vase")
			{
				TowerDefensePlantJalaVase towerDefensePlantJalaVase = (TowerDefensePlantJalaVase)value;
				if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
				{
					return towerDefensePlantJalaVase;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(towerDefensePlantJalaVase.gridPos);
				return (GodotObject.IsInstanceValid(mapCell) && !mapCell.NotifyBlockedPlanting(packetConfig) && hypnoses == packetConfig.GetHypnoses() && towerDefensePlantJalaVase.TryAddJala(packetConfig)) ? towerDefensePlantJalaVase : null;
			}
			TowerDefenseCharacter towerDefenseCharacter = ((!(placementKind == "plant")) ? (economyOwnerAccountId.IsValid ? packetConfig.PlantOnZombie(economyOwnerAccountId, value, hypnoses) : packetConfig.PlantOnZombie(value, hypnoses)) : (economyOwnerAccountId.IsValid ? packetConfig.PlantOnPlant(economyOwnerAccountId, value) : packetConfig.PlantOnPlant(value)));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && syncId >= 0)
			{
				_control.RegisterSyncCharacter(syncId, towerDefenseCharacter);
			}
			return towerDefenseCharacter;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(towerDefensePacketOverride))
			{
				packetConfig._override = towerDefensePacketOverride2;
			}
		}
	}

	private static bool IsValidPlacementTarget(TowerDefenseCharacter target, string placementKind)
	{
		if (!GodotObject.IsInstanceValid(target) || target.die || target.nearDie || !GodotObject.IsInstanceValid(target.instance) || target.instance.invincible || !target.instance.canBeCollection)
		{
			return false;
		}
		return placementKind switch
		{
			"plant" => target is TowerDefensePlant, 
			"jala_vase" => target is TowerDefensePlantJalaVase, 
			"zombie" => target is TowerDefenseZombie || target.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, 
			_ => false, 
		};
	}

	private TowerDefenseCharacter PlantAtCore(EconomyAccountId economyOwnerAccountId, string plantName, Vector2I gridPos, int syncId, string overrideStr)
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(plantName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return null;
		}
		TowerDefensePacketOverride towerDefensePacketOverride = null;
		TowerDefensePacketOverride towerDefensePacketOverride2 = packetConfig._override;
		try
		{
			if (overrideStr != "")
			{
				Variant variant = Json.ParseString(overrideStr);
				if (variant.VariantType == Variant.Type.Dictionary)
				{
					towerDefensePacketOverride = new TowerDefensePacketOverride();
					towerDefensePacketOverride.Init(variant.AsGodotDictionary());
					packetConfig._override = towerDefensePacketOverride;
				}
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
			if (!GodotObject.IsInstanceValid(mapCell))
			{
				return null;
			}
			bool flag = false;
			if (mapCell.NotifyBlockedPlanting(packetConfig))
			{
				return null;
			}
			if (TowerDefenseManager.GetMapFeature().isPlantColumn && (!Global.IsMultiplayerMode || MultiPlayerManager.Instance.isHost) && !new ColumnPlantGridBudget(packetConfig).CanPlace(gridPos))
			{
				return null;
			}
			bool flag2 = packetConfig.GetPlantCover().Count > 0 || packetConfig.characterConfig.plantCoverSelf;
			if (Global.IsMultiplayerMode && !MultiPlayerManager.Instance.isHost)
			{
				if (flag2)
				{
					TowerDefenseCharacter towerDefenseCharacter = FindCoverTarget(mapCell, packetConfig);
					if (GodotObject.IsInstanceValid(towerDefenseCharacter))
					{
						_control.DetachSyncCharacter(towerDefenseCharacter);
						DestroyPlantFromNetwork(towerDefenseCharacter, shovel: false);
					}
				}
				flag = true;
			}
			else
			{
				flag = !flag2;
				if (!mapCell.CanPacketPlant(packetConfig, flag))
				{
					if (!flag2)
					{
						TowerDefenseCharacter shovelCharacter = mapCell.GetShovelCharacter(0.5);
						if (GodotObject.IsInstanceValid(shovelCharacter))
						{
							if (Global.IsMultiplayerMode && MultiPlayerManager.Instance.isHost && shovelCharacter.syncId >= 0)
							{
								MultiPlayerManager.Instance.SendCharacterDestroy(shovelCharacter.syncId);
							}
							DestroyPlantFromNetwork(shovelCharacter, shovel: false);
						}
					}
					else
					{
						TowerDefenseCharacter towerDefenseCharacter2 = FindCoverTarget(mapCell, packetConfig);
						if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
						{
							_control.DetachSyncCharacter(towerDefenseCharacter2);
							DestroyPlantFromNetwork(towerDefenseCharacter2, shovel: false);
						}
						flag = true;
					}
				}
			}
			TowerDefenseCharacter towerDefenseCharacter3 = (economyOwnerAccountId.IsValid ? packetConfig.Plant(economyOwnerAccountId, gridPos, playAudio: true, flag) : packetConfig.Plant(gridPos, playAudio: true, flag));
			if (GodotObject.IsInstanceValid(towerDefenseCharacter3) && syncId >= 0)
			{
				_control.RegisterSyncCharacter(syncId, towerDefenseCharacter3);
			}
			return towerDefenseCharacter3;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(towerDefensePacketOverride))
			{
				packetConfig._override = towerDefensePacketOverride2;
			}
		}
	}

	public void SpawnZombie(string zombieName, int line, float offsetX, int syncId = -1, string spawnOverrideStr = "", string spawnConfigOverrideStr = "")
	{
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(zombieName);
		if (!GodotObject.IsInstanceValid(packetConfig))
		{
			return;
		}
		TowerDefenseCharacter towerDefenseCharacter = packetConfig.Spawn(line, offsetX);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			ApplyCharacterOverride(towerDefenseCharacter, spawnOverrideStr);
			ApplyCharacterOverride(towerDefenseCharacter, spawnConfigOverrideStr);
			if (syncId >= 0)
			{
				_control.RegisterSyncCharacter(syncId, towerDefenseCharacter);
			}
		}
	}

	public void BreakVaseRequest(Vector2I gridPos)
	{
		if (!MultiPlayerManager.Instance.isHost)
		{
			return;
		}
		foreach (Node item in _control.GetTree().GetNodesInGroup("Vase"))
		{
			if (GodotObject.IsInstanceValid(item) && item is TowerDefenseVase { over: false } towerDefenseVase && towerDefenseVase.gridPos == gridPos)
			{
				towerDefenseVase.Destroy();
				break;
			}
		}
	}

	private void DestroyPlantFromNetwork(TowerDefenseCharacter plant, bool shovel)
	{
		_control.CleanupCharacterCell(plant);
		DestroyComponent destroyComponent = plant.destroyComponent;
		if (destroyComponent != null && !destroyComponent.IsReleased)
		{
			plant.destroyComponent.isRemoteDestroy = true;
		}
		if (shovel)
		{
			plant.ShovelDestroy();
		}
		else
		{
			plant.Destroy();
		}
	}

	private static TowerDefenseCharacter FindCoverTarget(TowerDefenseCellInstance cell, TowerDefensePacketConfig packetConfig)
	{
		if (packetConfig.GetPlantCover().Count > 0)
		{
			foreach (TowerDefenseCharacter character in cell.characterList)
			{
				if (GodotObject.IsInstanceValid(character) && !character.isDestroy && packetConfig.GetPlantCover().Contains(character.config.name))
				{
					return character;
				}
			}
		}
		if (packetConfig.characterConfig.plantCoverSelf)
		{
			foreach (TowerDefenseCharacter character2 in cell.characterList)
			{
				if (GodotObject.IsInstanceValid(character2) && !character2.isDestroy && character2.config.name == packetConfig.characterConfig.name)
				{
					return character2;
				}
			}
		}
		return null;
	}

	private static void ApplyCharacterOverride(TowerDefenseCharacter character, string overrideStr)
	{
		if (overrideStr == "")
		{
			return;
		}
		Variant variant = Json.ParseString(overrideStr);
		if (variant.VariantType == Variant.Type.Dictionary)
		{
			TowerDefenseCharacterOverride towerDefenseCharacterOverride = new TowerDefenseCharacterOverride();
			if (GodotObject.IsInstanceValid(towerDefenseCharacterOverride))
			{
				towerDefenseCharacterOverride.Init(variant.AsGodotDictionary());
				towerDefenseCharacterOverride.ExecuteCharacter(character);
			}
		}
	}
}
