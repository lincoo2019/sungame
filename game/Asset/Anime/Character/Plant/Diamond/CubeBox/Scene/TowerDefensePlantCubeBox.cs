using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantCubeBox : TowerDefensePlant
{
	private const double HologramDuration = 30.0;

	private ExplodeComponent _explodeComponent;

	private bool _converted;

	private bool _ownCellSummoned;

	private List<TowerDefenseCharacter> _pendingHolograms = new List<TowerDefenseCharacter>();

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint() && !editorPreviewMode)
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			ExplodeComponent explodeComponent = _explodeComponent;
			if (explodeComponent != null && !explodeComponent.IsReleased)
			{
				_explodeComponent.OnExplode += OnExplode;
			}
			Callable.From(ConvertOnPlanted).CallDeferred();
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= OnExplode;
		}
		base._ExitTree();
	}

	private void ConvertOnPlanted()
	{
		if (!_converted)
		{
			_converted = true;
			if ((!Global.IsMultiplayerMode || MultiPlayerManager.IsHost) && !editorPreviewMode)
			{
				SummonSurroundingHolograms();
			}
		}
	}

	private void OnExplode()
	{
		if (!Global.IsMultiplayerMode || MultiPlayerManager.IsHost)
		{
			SummonOwnCellHologram();
		}
	}

	private void SummonSurroundingHolograms()
	{
		List<TowerDefenseCharacter> list = new List<TowerDefenseCharacter>();
		for (int i = -1; i <= 1; i++)
		{
			for (int j = -1; j <= 1; j++)
			{
				if (i == 0 && j == 0)
				{
					continue;
				}
				Vector2I targetGridPos = base.gridPos + new Vector2I(i, j);
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(targetGridPos);
				if (GodotObject.IsInstanceValid(mapCell))
				{
					TowerDefenseCharacter item = TrySummonHologram(mapCell, targetGridPos);
					if (GodotObject.IsInstanceValid(item))
					{
						list.Add(item);
					}
				}
			}
		}
		ScheduleExpire(list);
	}

	private void SummonOwnCellHologram()
	{
		if (!_ownCellSummoned)
		{
			_ownCellSummoned = true;
			if (GodotObject.IsInstanceValid(cell))
			{
				cell.RemoveCharacter(this);
			}
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(base.gridPos);
			TowerDefenseCharacter item = null;
			if (GodotObject.IsInstanceValid(mapCell))
			{
				item = TrySummonHologram(mapCell, base.gridPos);
			}
			if (GodotObject.IsInstanceValid(item))
			{
				ScheduleExpire(new List<TowerDefenseCharacter> { item });
			}
		}
	}

	private TowerDefenseCharacter TrySummonHologram(TowerDefenseCellInstance cell, Vector2I targetGridPos)
	{
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData(((double)GD.Randf() > 0.5) ? "PlantPresentBox" : "GeneralPlant");
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			return null;
		}
		Array plantList = packetBankData.GetPlantList();
		if (plantList.Count == 0)
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(packet))
		{
			plantList.Remove(packet.saveKey);
		}
		if (plantList.Count == 0)
		{
			return null;
		}
		string text = plantList.PickRandom().AsString();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(text);
		while (plantList.Count > 1 && GodotObject.IsInstanceValid(packetConfig) && ((instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0) || !cell.CanPacketPlant(packetConfig)))
		{
			plantList.Remove(text);
			text = plantList.PickRandom().AsString();
			packetConfig = TowerDefenseManager.GetPacketConfig(text);
		}
		if (plantList.Count == 0 || !GodotObject.IsInstanceValid(packetConfig) || !cell.CanPacketPlant(packetConfig) || (instance.hypnoses && (packetConfig.characterConfig.unUseBuffFlags & 8) != 0))
		{
			return null;
		}
		bool hypnoses = instance.hypnoses;
		TowerDefenseCharacter plant = packetConfig.Plant(targetGridPos, playAudio: false);
		if (!GodotObject.IsInstanceValid(plant))
		{
			return null;
		}
		_pendingHolograms.Add(plant);
		if (!TowerDefenseManager.Instance.IsGameRunning() && TowerDefenseManager.CurrentControl.GetFeature("PreSpawn") is TowerDefenseBattleFeaturePreSpawn towerDefenseBattleFeaturePreSpawn)
		{
			towerDefenseBattleFeaturePreSpawn.preSpawnList.Add(plant);
		}
		SyncHologramSpawn(plant, text, targetGridPos, hypnoses);
		Callable.From(delegate
		{
			if (GodotObject.IsInstanceValid(plant))
			{
				if (hypnoses)
				{
					plant.Hypnoses();
				}
				ApplyHologramState(plant);
				plant.CallDeferred("WakeUp");
			}
		}).CallDeferred();
		return plant;
	}

	private static void ApplyHologramState(TowerDefenseCharacter character)
	{
		character.SetSpriteGroupShaderParameter("hologram", true);
		character.instance.hologram = true;
		character.instance.canBeCollection = false;
		character.skipDestroySet = true;
		ForceRefreshSprites(character.spriteGroup);
	}

	private void SyncHologramSpawn(TowerDefenseCharacter plant, string plantName, Vector2I targetGridPos, bool hypnoses)
	{
		if (Global.IsMultiplayerMode && MultiPlayerManager.IsHost)
		{
			TowerDefenseControlNew currentControl = TowerDefenseManager.CurrentControl;
			if (GodotObject.IsInstanceValid(currentControl))
			{
				int nextSyncId = currentControl.GetNextSyncId();
				currentControl.RegisterSyncCharacter(nextSyncId, plant);
				Dictionary spawnState = new Dictionary
				{
					["hologram"] = true,
					["plant_play_audio"] = false
				};
				MultiPlayerManager.Instance.SendSpawnCharacterAt(plantName, targetGridPos.X, targetGridPos.Y, nextSyncId, 1.0, 1.0, hypnoses, 0.0, useCreate: false, 0.0, 0.0, walkAfterSpawn: false, 0.0, "", spawnState);
			}
		}
	}

	private void ScheduleExpire(List<TowerDefenseCharacter> batch)
	{
		if (batch != null && batch.Count != 0)
		{
			GetTree().CreateTimer(30.0, processAlways: false).Timeout += delegate
			{
				ExpireHolograms(batch);
			};
		}
	}

	protected internal override void OnHypnosisStateChanged()
	{
		base.OnHypnosisStateChanged();
		bool hypnoses = instance.hypnoses;
		foreach (TowerDefenseCharacter pendingHologram in _pendingHolograms)
		{
			if (GodotObject.IsInstanceValid(pendingHologram) && !pendingHologram.isDestroy)
			{
				pendingHologram.SyncHologramHypnoses(hypnoses);
			}
		}
	}

	private static void ExpireHolograms(List<TowerDefenseCharacter> holograms)
	{
		GD.Print($"[CubeBox] ExpireHolograms count={holograms.Count}");
		foreach (TowerDefenseCharacter hologram in holograms)
		{
			if (GodotObject.IsInstanceValid(hologram) && !hologram.isDestroy)
			{
				RemoveHologram(hologram);
			}
		}
	}

	private static void RemoveHologram(TowerDefenseCharacter character)
	{
		character.EmitDestroy();
		if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			TowerDefenseManager.Instance.CharacterUnregister(character);
		}
		character.RemoveFromGroup("Character");
		character.QueueFree();
	}

	private static void ForceRefreshSprites(Node root)
	{
		if (GodotObject.IsInstanceValid(root))
		{
			if (root is AdobeAnimateSpriteBase adobeAnimateSpriteBase)
			{
				adobeAnimateSpriteBase.SetRenderColorMultiplier(new Color(1f, 1f, 1f, 0.999f));
				adobeAnimateSpriteBase.SetRenderColorMultiplier(Colors.White);
			}
			for (int i = 0; i < root.GetChildCount(); i++)
			{
				ForceRefreshSprites(root.GetChild(i));
			}
		}
	}

	public override Dictionary ExportVariantSave()
	{
		return new Dictionary
		{
			{ "converted", _converted },
			{ "ownCellSummoned", _ownCellSummoned }
		};
	}

	public override void ImportVariantSave(Dictionary data)
	{
		_converted = data.GetValueOrDefault("converted", false).AsBool();
		_ownCellSummoned = data.GetValueOrDefault("ownCellSummoned", false).AsBool();
		if (_converted || _ownCellSummoned)
		{
			Callable.From(RestorePendingHolograms).CallDeferred();
		}
	}

	private void RestorePendingHolograms()
	{
		if (Global.IsMultiplayerMode && !MultiPlayerManager.IsHost)
		{
			return;
		}
		List<TowerDefenseCharacter> cleanCharactersList = TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList();
		for (int i = 0; i < cleanCharactersList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = cleanCharactersList[i];
			if (towerDefenseCharacter != this && GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter is TowerDefensePlant towerDefensePlant && towerDefensePlant.instance.hologram && !towerDefensePlant.isDestroy && towerDefensePlant.camp == base.camp)
			{
				_pendingHolograms.Add(towerDefensePlant);
			}
		}
		if (_pendingHolograms.Count > 0)
		{
			ScheduleExpire(new List<TowerDefenseCharacter>(_pendingHolograms));
		}
	}
}
