using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class PlantColumnBasePlacementRuntimeTest : Node
{
	private const string PotPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres";

	private const string PotScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private static readonly Vector2I TestGridSize = new Vector2I(1, 5);

	private int _checks;

	private int _failures;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		PlantColumnBasePlacementRuntimeControlStub control = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseInGamePacketShow packetCard = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0079;
				}
				RegisterRealFixtures();
				control = new PlantColumnBasePlacementRuntimeControlStub
				{
					Name = "PlantColumnBasePlacementRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig
					{
						packetColdDownUse = false
					}
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D node2D = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = node2D;
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = TestGridSize;
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres");
				TowerDefensePacketConfig sunflowerPacket = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres");
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real flower-pot packet must load.");
				Check(GodotObject.IsInstanceValid(sunflowerPacket), "The real sunflower packet must load.");
				if (!GodotObject.IsInstanceValid(towerDefensePacketConfig) || !GodotObject.IsInstanceValid(sunflowerPacket))
				{
					goto end_IL_0079;
				}
				for (int i = 1; i <= TestGridSize.Y; i++)
				{
					TowerDefenseCharacter instance = towerDefensePacketConfig.Plant(new Vector2I(1, i), playAudio: false);
					Check(GodotObject.IsInstanceValid(instance), $"Flower pot creation must succeed on row {i}.");
				}
				await WaitFrames(4);
				List<Vector2I> list = new List<Vector2I>();
				for (int j = 1; j <= TestGridSize.Y; j++)
				{
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(1, j));
					Check(FindCellCharacter(mapCell, "PlantPot") != null, $"Row {j} must contain its flower-pot base before column planting.");
					Check(mapCell.CanPacketPlant(sunflowerPacket), $"Row {j} must accept a sunflower on its flower-pot base.");
					list.Add(new Vector2I(1, j));
				}
				packetCard = new TowerDefenseInGamePacketShow
				{
					Name = "ColumnPacketCard",
					config = sunflowerPacket,
					alive = true,
					onlyDraw = true,
					useCost = false
				};
				List<TowerDefenseCharacter> list2 = packetCard.PlantColumnBatch(list);
				Check(list2.Count == TestGridSize.Y, $"Column batch must create {TestGridSize.Y} sunflowers before deferred registration; got {list2.Count}.");
				await WaitFrames(6);
				for (int k = 1; k <= TestGridSize.Y; k++)
				{
					TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(new Vector2I(1, k));
					TowerDefenseCharacter first = FindCellCharacter(mapCell2, "PlantPot");
					TowerDefenseCharacter towerDefenseCharacter = FindCellCharacter(mapCell2, "PlantSunFlower");
					Check(GodotObject.IsInstanceValid(towerDefenseCharacter), $"Row {k} must contain the column-planted sunflower.");
					Check(AreSlotRelated(mapCell2, first, towerDefenseCharacter), $"Row {k} must retain the flower-pot and sunflower slot relationship.");
				}
				goto end_IL_0067;
				end_IL_0079:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantColumnBasePlacementRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0067;
			}
			return;
			end_IL_0067:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(packetCard))
			{
				packetCard.Free();
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(2);
		}
		bool flag = _failures == 0;
		GD.Print($"PLANT_COLUMN_BASE_PLACEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = TestGridSize
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		towerDefenseBattleFeatureMap.iceCapList.Resize(TestGridSize.Y + 1);
		for (int i = 1; i <= TestGridSize.Y; i++)
		{
			towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(1, i)).Init(new TowerDefenseCellConfig());
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static TowerDefenseCharacter FindCellCharacter(TowerDefenseCellInstance cell, string configName)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return null;
		}
		cell.ClearEmpty();
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character.config?.name == configName)
			{
				return character;
			}
		}
		return null;
	}

	private static bool AreSlotRelated(TowerDefenseCellInstance cell, TowerDefenseCharacter first, TowerDefenseCharacter second)
	{
		if (!GodotObject.IsInstanceValid(cell) || !GodotObject.IsInstanceValid(first) || !GodotObject.IsInstanceValid(second))
		{
			return false;
		}
		if (!cell.characterSlotDictionary.TryGetValue(first, out var value) || value != second)
		{
			if (cell.characterSlotDictionary.TryGetValue(second, out var value2))
			{
				return value2 == first;
			}
			return false;
		}
		return true;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPot", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres");
		RegisterPacket("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres");
		RegisterCharacter("PlantPot", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn");
		RegisterCharacter("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
	}

	private void RegisterPacket(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETS.TryGetValue(key, out var value))
		{
			_previousPackets[key] = value;
		}
		else
		{
			_missingPackets.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RegisterCharacter(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value))
		{
			_previousCharacters[key] = value;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			return;
		}
		foreach (string missingPacket in _missingPackets)
		{
			instance.TOWERDEFENSE_PACKETS.Remove(missingPacket);
		}
		foreach (KeyValuePair<string, Resource> previousPacket in _previousPackets)
		{
			instance.TOWERDEFENSE_PACKETS[previousPacket.Key] = previousPacket.Value;
		}
		foreach (string missingCharacter in _missingCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS.Remove(missingCharacter);
		}
		foreach (KeyValuePair<string, Resource> previousCharacter in _previousCharacters)
		{
			instance.TOWERDEFENSE_CHARCATERS[previousCharacter.Key] = previousCharacter.Value;
		}
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PlantColumnBasePlacementRuntimeTest] " + message);
		}
	}
}
