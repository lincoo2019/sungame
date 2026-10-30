using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentVaseShuffleCompatibilityRuntimeTest : Node
{
	private const int ShuffleRounds = 48;

	private const string NormalVasePacketPath = "res://Asset/Anime/Character/Vase/Normal/Packet/VaseNormal.tres";

	private const string NormalVaseScenePath = "res://Asset/Anime/Character/Vase/Normal/Scene/TowerDefenseVaseNormal.tscn";

	private const string ZombieVasePacketPath = "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres";

	private const string ZombieVaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	private BugDepartmentVaseShuffleCompatibilityControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		TowerDefenseBattleProcessVase process = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00c0;
				}
				RegisterRealFixtures();
				SetupRealMap(manager);
				process = CreateShuffledVaseProcess();
				_control.process = process;
				Check(process.config.shuffle && process.config.vaseList.Count == 2 && process.config.vaseList[0].type == "Normal" && process.config.vaseList[1].type == "Zombie", "The fixture must use the real shuffled Vase config with only Normal and Zombie shells.");
				int completedRounds = 0;
				int totalNormalVases = 0;
				int totalZombieVases = 0;
				int plantInZombieVase = 0;
				int zombieInNormalVase = 0;
				int missingOrUnexpectedVases = 0;
				for (int round = 0; round < 48; round++)
				{
					await process.GameInit();
					await WaitFrames(5);
					List<TowerDefenseVase> list = FindLiveVases();
					if (list.Count != 2)
					{
						missingOrUnexpectedVases++;
					}
					foreach (TowerDefenseVase item in list)
					{
						if (item is TowerDefenseVaseNormal)
						{
							totalNormalVases++;
							if (!(item.packetConfig?.characterConfig is TowerDefensePlantConfig))
							{
								zombieInNormalVase++;
							}
						}
						else if (item is TowerDefenseVaseZombie)
						{
							totalZombieVases++;
							if (item.packetConfig?.characterConfig is TowerDefensePlantConfig)
							{
								plantInZombieVase++;
							}
							if (!(item.packetConfig?.characterConfig is TowerDefenseZombieConfig))
							{
								missingOrUnexpectedVases++;
							}
						}
						else
						{
							missingOrUnexpectedVases++;
						}
					}
					completedRounds++;
					CleanupVases(list);
					await WaitFrames(3);
				}
				Check(completedRounds == 48, $"The production shuffle path must complete {48} rounds; got {completedRounds}.");
				Check(totalNormalVases == 48 && totalZombieVases == 48, $"Every round must create one real Normal vase and one real Zombie vase; normal={totalNormalVases}, zombie={totalZombieVases}.");
				Check(plantInZombieVase == 0, $"A plant packet must never be shuffled into a Zombie vase; mismatches={plantInZombieVase}.");
				Check(zombieInNormalVase == 0, $"With one matching Zombie shell available, the Normal vase must retain the plant packet; mismatches={zombieInNormalVase}.");
				Check(missingOrUnexpectedVases == 0, $"All shuffled shells must keep valid, matching real contents; unexpected={missingOrUnexpectedVases}.");
				goto end_IL_00a9;
				end_IL_00c0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentVaseShuffleCompatibilityRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00a9;
			}
			return;
			end_IL_00a9:;
		}
		finally
		{
			process?.Destroy();
			if (GodotObject.IsInstanceValid(_control))
			{
				CleanupVases(FindLiveVases());
				_control.QueueFree();
			}
			_mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			RestoreRealFixtures();
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 7;
		GD.Print($"VASE_SHUFFLE_COMPATIBILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetupRealMap(TowerDefenseManager manager)
	{
		_control = new BugDepartmentVaseShuffleCompatibilityControlStub
		{
			Name = "VaseShuffleCompatibilityControl",
			isGameRunning = false,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.gridBeginPos = Vector2.Zero;
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = manager.gridNum,
				gridBeginPos = manager.gridBeginPos,
				gridSize = manager.gridSize,
				plantOffset = 50.0
			}
		};
		_mapFeature.mapConfig = _mapFeature.config;
		_mapControl.mapFeature = _mapFeature;
		CreateGrid(_mapFeature, manager.gridNum);
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
	}

	private TowerDefenseBattleProcessVase CreateShuffledVaseProcess()
	{
		TowerDefenseBattleProcessVase towerDefenseBattleProcessVase = new TowerDefenseBattleProcessVase();
		towerDefenseBattleProcessVase.control = _control;
		towerDefenseBattleProcessVase.Init(new Dictionary
		{
			["Shuffle"] = true,
			["Vase"] = new Godot.Collections.Array
			{
				new Dictionary
				{
					["PacketName"] = "PlantSunFlower",
					["GridPos"] = new Godot.Collections.Array { 3, 2 },
					["Type"] = "Normal"
				},
				new Dictionary
				{
					["PacketName"] = "ZombieNormal",
					["GridPos"] = new Godot.Collections.Array { 4, 2 },
					["Type"] = "Zombie"
				}
			},
			["VaseFill"] = new Godot.Collections.Array()
		});
		return towerDefenseBattleProcessVase;
	}

	private static void CreateGrid(TowerDefenseBattleFeatureMap feature, Vector2I gridNum)
	{
		feature.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
				array[j] = towerDefenseCellInstance;
			}
			feature.plantGrid[i] = array;
		}
		feature.iceCapList.Resize(gridNum.Y + 1);
		feature.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			feature.lineUse[k] = true;
		}
	}

	private List<TowerDefenseVase> FindLiveVases()
	{
		List<TowerDefenseVase> list = new List<TowerDefenseVase>();
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return list;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase) && !towerDefenseVase.IsQueuedForDeletion())
			{
				list.Add(towerDefenseVase);
			}
		}
		return list;
	}

	private void CleanupVases(IEnumerable<TowerDefenseVase> vases)
	{
		foreach (TowerDefenseVase vase in vases)
		{
			if (GodotObject.IsInstanceValid(vase))
			{
				_control?.CleanupCharacterCell(vase);
				if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
				{
					TowerDefenseManager.Instance.CharacterUnregister(vase);
				}
				vase.RemoveFromGroup("Vase");
				vase.RemoveFromGroup("Character");
				if (!vase.IsQueuedForDeletion())
				{
					vase.QueueFree();
				}
			}
		}
	}

	private void RegisterRealFixtures()
	{
		Register("VaseNormal", "res://Asset/Anime/Character/Vase/Normal/Packet/VaseNormal.tres", "res://Asset/Anime/Character/Vase/Normal/Scene/TowerDefenseVaseNormal.tscn");
		Register("VaseZombie", "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn");
		Register("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
		Register("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
	}

	private void Register(string key, string packetPath, string scenePath)
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
		if (instance.TOWERDEFENSE_CHARCATERS.TryGetValue(key, out var value2))
		{
			_previousCharacters[key] = value2;
		}
		else
		{
			_missingCharacters.Add(key);
		}
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
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
			GD.PushError("[BugDepartmentVaseShuffleCompatibilityRuntimeTest] " + message);
		}
	}
}
