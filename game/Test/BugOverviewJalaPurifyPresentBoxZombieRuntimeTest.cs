using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewJalaPurifyPresentBoxZombieRuntimeTest : Node
{
	private const string PresentBoxPlantPacketPath = "res://Asset/Anime/Character/Plant/Special/PresentBox/Packet/PlantPresentBox.tres";

	private const string PresentBoxPlantScenePath = "res://Asset/Anime/Character/Plant/Special/PresentBox/Scene/TowerDefensePlantPresentBox.tscn";

	private static readonly string[] PresentBoxZombieScenePaths = new string[3] { "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/PresentBox/TowerDefenseZombieNormalPresentBox.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PresentBox/TowerDefenseZombieImpPresentBox.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/PresentBox/TowerDefenseZombieGargantuarPresentBox.tscn" };

	private static readonly string[] PresentBoxZombiePacketPaths = new string[3] { "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalPresentBox.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/PresentBox/ZombieImpPresentBox.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/PresentBox/ZombieGargantuarPresentBox.tres" };

	private static readonly string[] PresentBoxZombieNames = new string[3] { "ZombieNormalPresentBox", "ZombieImpPresentBox", "ZombieGargantuarPresentBox" };

	private static readonly Vector2I[] PresentBoxZombieGrids = new Vector2I[3]
	{
		new Vector2I(6, 2),
		new Vector2I(6, 3),
		new Vector2I(6, 4)
	};

	private const double DurableHitpoints = 100000.0;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		JalaPurifyPresentBoxZombieRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00ce;
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("BugOverviewJalaPurifyPresentBoxZombieRuntimeTest");
				RegisterRealFixtures();
				control = new JalaPurifyPresentBoxZombieRuntimeControlStub
				{
					Name = "JalaPurifyPresentBoxZombieRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = Vector2.Zero;
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				for (int index = 0; index < PresentBoxZombieScenePaths.Length; index++)
				{
					string zombieName = PresentBoxZombieNames[index];
					Vector2I zombieGrid = PresentBoxZombieGrids[index];
					TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(PresentBoxZombiePacketPaths[index], null, ResourceLoader.CacheMode.Ignore);
					TowerDefenseCharacter zombie = towerDefensePacketConfig?.Plant(zombieGrid);
					Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && towerDefensePacketConfig.saveKey == zombieName && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == zombieName, "The real " + zombieName + " packet must create its character scene.");
					if (!GodotObject.IsInstanceValid(zombie))
					{
						continue;
					}
					await WaitFrames(5);
					TowerDefenseCellInstance zombieCell = TowerDefenseManager.GetMapCell(zombieGrid);
					Check(GodotObject.IsInstanceValid(zombieCell), "The real " + zombieName + " grid must resolve its live map cell.");
					if (GodotObject.IsInstanceValid(zombieCell))
					{
						zombie.cell = zombieCell;
					}
					Check(manager.GetCharacterLineList(zombieGrid.Y, fliterGraveStone: false).Contains(zombie), "The real " + zombieName + " must be discoverable by line effects.");
					zombie.instance.hitpoints = 100000.0;
					zombie.instance.hitpointsBase = 100000.0;
					TowerDefenseCharacterEventPurify.Run(zombie);
					await WaitFrames(4);
					Check(zombie.isDestroy, "Purify must destroy " + zombieName + " even when ordinary fire damage cannot kill it.");
					Check(!manager.GetCharacterLineList(zombieGrid.Y, fliterGraveStone: false).Contains(zombie), "Purify must remove " + zombieName + " from live battle targeting.");
					Check(GodotObject.IsInstanceValid(zombieCell) && zombieCell.HasCharacter("PlantPresentBox"), "Purify must restore PlantPresentBox in the original " + zombieName + " cell.");
					TowerDefenseCharacter towerDefenseCharacter = null;
					if (GodotObject.IsInstanceValid(zombieCell))
					{
						foreach (TowerDefenseCharacter character in zombieCell.characterList)
						{
							if (GodotObject.IsInstanceValid(character) && character.config?.name == "PlantPresentBox")
							{
								towerDefenseCharacter = character;
								break;
							}
						}
					}
					Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter is TowerDefensePlant && !towerDefenseCharacter.IsSleep(), "The " + zombieName + " replacement must be the real awake PlantPresentBox.");
				}
				goto end_IL_00b7;
				end_IL_00ce:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewJalaPurifyPresentBoxZombieRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 23;
		GD.Print($"JALA_PURIFY_PRESENT_BOX_ZOMBIE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantPresentBox", "res://Asset/Anime/Character/Plant/Special/PresentBox/Packet/PlantPresentBox.tres");
		RegisterCharacter("PlantPresentBox", "res://Asset/Anime/Character/Plant/Special/PresentBox/Scene/TowerDefensePlantPresentBox.tscn");
		for (int i = 0; i < PresentBoxZombieNames.Length; i++)
		{
			RegisterCharacter(PresentBoxZombieNames[i], PresentBoxZombieScenePaths[i]);
		}
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
			GD.PushError("[BugOverviewJalaPurifyPresentBoxZombieRuntimeTest] " + message);
		}
	}
}
