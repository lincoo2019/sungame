using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class PreSpawnRoofBaseOrderingIzmRuntimeTest : Node
{
	private const string PotPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres";

	private const string PotScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private static readonly Vector2I TestGridPosition = new Vector2I(1, 1);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		PreSpawnRoofBaseOrderingIzmRuntimeControlStub control = null;
		TowerDefenseBattleFeaturePreSpawn preSpawnFeature = null;
		TowerDefenseBattleProcessIZM izmProcess = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseMapControl mapControl = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0080;
				}
				RegisterRealFixtures();
				TowerDefenseLevelConfig towerDefenseLevelConfig = new TowerDefenseLevelConfig
				{
					finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM,
					izmManager = new TowerDefenseLevelIZMManagerConfig
					{
						shuffle = true
					}
				};
				control = new PreSpawnRoofBaseOrderingIzmRuntimeControlStub
				{
					Name = "PreSpawnIzmRuntimeControl",
					isGameRunning = true,
					isInit = true,
					levelConfig = towerDefenseLevelConfig
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
				manager.gridNum = Vector2I.One;
				TowerDefenseCellInstance roofCell = CreateRoofCell();
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, roofCell);
				control.featureDictionary[new StringName("Map")] = mapFeature;
				izmProcess = new TowerDefenseBattleProcessIZM
				{
					control = control,
					mapFeature = mapFeature
				};
				izmProcess.Init(new Dictionary
				{
					["Shuffle"] = true,
					["PreSpawnMaxRetryPasses"] = 8
				});
				control.process = izmProcess;
				Check(towerDefenseLevelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM, "The real PreSpawn feature must be configured for the IZM finish method.");
				Check(towerDefenseLevelConfig.izmManager.shuffle, "The IZM pre-spawn shuffle branch must be enabled.");
				Check(control.process == izmProcess && izmProcess.GetType() == typeof(TowerDefenseBattleProcessIZM), "The control must use a real TowerDefenseBattleProcessIZM instance.");
				TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig
				{
					packetName = "PlantSunFlower",
					gridPos = TestGridPosition
				};
				Check(towerDefenseLevelPreSpawnConfig.SpawnCharacter() == null, "A real sunflower must not pre-spawn directly on an empty BRICK roof cell.");
				preSpawnFeature = new TowerDefenseBattleFeaturePreSpawn
				{
					control = control
				};
				preSpawnFeature.Init(CreatePlantBeforePotData());
				Check(preSpawnFeature.config.maxRetryPasses == 1 && izmProcess.config.preSpawnMaxRetryPasses == 8, "Only the IZM process may retry enough times for the dependent sunflower to spawn.");
				await preSpawnFeature.GameEntry();
				await WaitPhysicsFrames(4);
				Check(preSpawnFeature.preSpawnList.Count == 2, $"The IZM PreSpawn chain must retain both entries; got {preSpawnFeature.preSpawnList.Count}.");
				TowerDefenseCharacter towerDefenseCharacter = FindCellCharacter(roofCell, "PlantPot");
				TowerDefenseCharacter towerDefenseCharacter2 = FindCellCharacter(roofCell, "PlantSunFlower");
				Check(GodotObject.IsInstanceValid(towerDefenseCharacter), "The real flower pot must be registered in the shuffled IZM roof cell.");
				Check(GodotObject.IsInstanceValid(towerDefenseCharacter2), "The sunflower listed before its pot must succeed through the IZM retry path.");
				Check(AreSlotRelated(roofCell, towerDefenseCharacter, towerDefenseCharacter2), "The IZM roof cell must retain the real pot-to-sunflower slot relationship.");
				goto end_IL_006e;
				end_IL_0080:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PreSpawnRoofBaseOrderingIzmRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006e;
			}
			return;
			end_IL_006e:;
		}
		finally
		{
			preSpawnFeature?.Destroy();
			izmProcess?.Destroy();
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			mapFeature?.Destroy();
			RestoreRealFixtures();
			for (int frame = 0; frame < 4; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"PRESPAWN_ROOF_BASE_ORDERING_IZM_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseCellInstance CreateRoofCell()
	{
		return new TowerDefenseCellInstance
		{
			gridPos = TestGridPosition,
			gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
			{
				TowerDefenseEnum.PLANTGRIDTYPE.BRICK,
				TowerDefenseEnum.PLANTGRIDTYPE.AIR
			},
			slot = 
			{
				[TowerDefenseEnum.PLANTGRIDTYPE.BRICK] = null,
				[TowerDefenseEnum.PLANTGRIDTYPE.AIR] = null
			}
		};
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseCellInstance roofCell)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = Vector2I.One
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(2);
		for (int i = 0; i < towerDefenseBattleFeatureMap.plantGrid.Count; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(2);
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.plantGrid[TestGridPosition.X][TestGridPosition.Y] = roofCell;
		towerDefenseBattleFeatureMap.iceCapList.Resize(2);
		return towerDefenseBattleFeatureMap;
	}

	private static Dictionary CreatePlantBeforePotData()
	{
		return new Dictionary
		{
			["MaxRetryPasses"] = 1,
			["Packet"] = new Godot.Collections.Array
			{
				new Dictionary
				{
					["Name"] = "PlantSunFlower",
					["GridPos"] = new Godot.Collections.Array { TestGridPosition.X, TestGridPosition.Y }
				},
				new Dictionary
				{
					["Name"] = "PlantPot",
					["GridPos"] = new Godot.Collections.Array { TestGridPosition.X, TestGridPosition.Y }
				}
			}
		};
	}

	private static TowerDefenseCharacter FindCellCharacter(TowerDefenseCellInstance cell, string configName)
	{
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
		if (!GodotObject.IsInstanceValid(first) || !GodotObject.IsInstanceValid(second))
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

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[PreSpawnRoofBaseOrderingIzmRuntimeTest] " + message);
		}
	}
}
