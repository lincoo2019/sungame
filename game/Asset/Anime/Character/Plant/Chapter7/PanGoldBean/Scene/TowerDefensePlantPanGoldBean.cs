using System.Collections.Generic;
using Godot;
using Godot.Collections;

[Tool]
public partial class TowerDefensePlantPanGoldBean : TowerDefensePlant
{
	private ExplodeComponent _explodeComponent;

	public override void _Ready()
	{
		base._Ready();
		if (!Engine.IsEditorHint())
		{
			_explodeComponent = componentManager.GetRuntime<ExplodeComponent>();
			_explodeComponent.OnExplode += Explode;
		}
	}

	public override void _ExitTree()
	{
		ExplodeComponent explodeComponent = _explodeComponent;
		if (explodeComponent != null && !explodeComponent.IsReleased)
		{
			_explodeComponent.OnExplode -= Explode;
		}
		base._ExitTree();
	}

	public void Explode()
	{
		if (!TowerDefenseManager.HasGameplayAuthority || !GodotObject.IsInstanceValid(cell))
		{
			return;
		}
		Vector2 logicalGlobalPosition = GetLogicalGlobalPosition();
		double height = GetGroundHeight(logicalGlobalPosition.Y);
		List<TowerDefenseCharacter> characterList = cell.GetCharacterList();
		Array<TowerDefenseCharacter> filteredList = new Array<TowerDefenseCharacter>();
		foreach (TowerDefenseCharacter item in characterList)
		{
			if (item is TowerDefensePlant && item.config.name != base.config.name && item.camp == base.camp)
			{
				filteredList.Add(item);
			}
		}
		foreach (TowerDefenseCharacter item2 in filteredList)
		{
			if (!GodotObject.IsInstanceValid(item2))
			{
				continue;
			}
			item2.WakeUp();
			if (item2.cost > 0.0 && !instance.hypnoses)
			{
				int num = (int)Mathf.Floor(item2.cost / 50.0);
				for (int i = 0; i < num; i++)
				{
					TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_GOLD, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0).gridPos = base.gridPos;
				}
				int num2 = (int)Mathf.Floor((item2.cost - (double)(num * 50)) / 10.0);
				for (int j = 0; j < num2; j++)
				{
					TowerDefenseManager.Instance.FallingObjectItemCreate(ObjectManagerConfig.OBJECT.COIN_SILVER, logicalGlobalPosition, height, new Vector2((float)GD.RandRange(-50.0, 50.0), -400f), 980.0).gridPos = base.gridPos;
				}
			}
		}
		if (filteredList.Count <= 0 || !((double)GD.Randf() < 0.05))
		{
			return;
		}
		foreach (TowerDefenseCharacter item3 in filteredList)
		{
			if (GodotObject.IsInstanceValid(item3))
			{
				cell.RemoveCharacter(item3);
			}
		}
		TowerDefensePacketBankData packetBankData = TowerDefenseManager.GetPacketBankData("GeneralPlant");
		if (!GodotObject.IsInstanceValid(packetBankData))
		{
			RestoreOriginalPlants(cell, filteredList);
			return;
		}
		Array category = packetBankData.GetCategory("Gold");
		if (category == null || category.Count == 0)
		{
			RestoreOriginalPlants(cell, filteredList);
			return;
		}
		string packetRandom = category.PickRandom().AsString();
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig(packetRandom);
		while ((!GodotObject.IsInstanceValid(packetConfig) || !cell.CanPacketPlant(packetConfig)) && category.Count > 1)
		{
			category.Remove(packetRandom);
			packetRandom = category.PickRandom().AsString();
			packetConfig = TowerDefenseManager.GetPacketConfig(packetRandom);
		}
		if (GodotObject.IsInstanceValid(packetConfig) && cell.CanPacketPlant(packetConfig))
		{
			TowerDefenseCharacter plant = (base.HasEconomyOwner ? packetConfig.Plant(base.EconomyOwnerAccountId, base.gridPos) : packetConfig.Plant(base.gridPos));
			if (!GodotObject.IsInstanceValid(plant))
			{
				RestoreOriginalPlants(cell, filteredList);
				return;
			}
			TowerDefenseCellInstance replacementCell = cell;
			bool shouldHypnotize = instance.hypnoses;
			Callable.From(delegate
			{
				FinalizeReplacementPlant(plant, packetRandom, filteredList, replacementCell, shouldHypnotize, 3);
			}).CallDeferred();
		}
		else
		{
			RestoreOriginalPlants(cell, filteredList);
		}
	}

	private static void FinalizeReplacementPlant(TowerDefenseCharacter plant, string packetName, Array<TowerDefenseCharacter> originalPlants, TowerDefenseCellInstance replacementCell, bool shouldHypnotize, int readyRetries)
	{
		if (!GodotObject.IsInstanceValid(plant))
		{
			RestoreOriginalPlants(replacementCell, originalPlants);
			return;
		}
		if (!plant.IsNodeReady())
		{
			if (readyRetries > 0)
			{
				Callable.From(delegate
				{
					FinalizeReplacementPlant(plant, packetName, originalPlants, replacementCell, shouldHypnotize, readyRetries - 1);
				}).CallDeferred();
				return;
			}
			if (GodotObject.IsInstanceValid(replacementCell))
			{
				replacementCell.RemoveCharacter(plant);
			}
			plant.QueueFree();
			RestoreOriginalPlants(replacementCell, originalPlants);
			return;
		}
		plant.WakeUp();
		if (shouldHypnotize)
		{
			plant.Hypnoses();
		}
		Dictionary spawnState = new Dictionary { ["wake_up"] = true };
		TowerDefenseManager.PublishSpawnedCharacter(packetName, plant, useCreate: false, 0.0, walkAfterSpawn: false, "", spawnState);
		foreach (TowerDefenseCharacter originalPlant in originalPlants)
		{
			if (GodotObject.IsInstanceValid(originalPlant))
			{
				originalPlant.Destroy(freeInstance: false);
				originalPlant.QueueFree();
			}
		}
	}

	private static void RestoreOriginalPlants(TowerDefenseCellInstance replacementCell, Array<TowerDefenseCharacter> originalPlants)
	{
		if (!GodotObject.IsInstanceValid(replacementCell))
		{
			return;
		}
		foreach (TowerDefenseCharacter originalPlant in originalPlants)
		{
			if (GodotObject.IsInstanceValid(originalPlant) && !originalPlant.IsQueuedForDeletion())
			{
				replacementCell.CharacterPlant(originalPlant.packet, originalPlant);
			}
		}
	}
}
