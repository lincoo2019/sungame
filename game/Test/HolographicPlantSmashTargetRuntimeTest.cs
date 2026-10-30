using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class HolographicPlantSmashTargetRuntimeTest : Node
{
	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string ZamboniPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres";

	private const string ZamboniScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn";

	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn";

	private static readonly Vector2I ZamboniGrid = new Vector2I(4, 2);

	private static readonly Vector2I GargantuarGrid = new Vector2I(4, 3);

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<TowerDefenseCharacter> _spawnedCharacters = new List<TowerDefenseCharacter>();

	private GameplayResourceLoadState _previousGameplayResourceLoadState;

	private bool _gameplayResourceLoadStateOverridden;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		HolographicPlantSmashTargetControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					return;
				}
				RegisterRealFixtures();
				PublishFocusedGameplayResourceReady();
				control = new HolographicPlantSmashTargetControlStub
				{
					Name = "HolographicPlantSmashTargetControl",
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
				TowerDefensePlant zamboniProjection = SpawnCharacter("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", ZamboniGrid) as TowerDefensePlant;
				TowerDefensePlant gargantuarProjection = SpawnCharacter("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", GargantuarGrid) as TowerDefensePlant;
				await WaitFrames(5);
				ConfigureHologramPlant(zamboniProjection);
				ConfigureHologramPlant(gargantuarProjection);
				TowerDefenseZombieZamboni zamboni = SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres", ZamboniGrid) as TowerDefenseZombieZamboni;
				TowerDefenseZombieGargantuar gargantuar = SpawnCharacter("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", GargantuarGrid) as TowerDefenseZombieGargantuar;
				await WaitFrames(6);
				Check(zamboniProjection?.config?.name == "PlantSunFlower" && gargantuarProjection?.config?.name == "PlantSunFlower" && zamboni?.config?.name == "ZombieZamboni" && gargantuar?.config?.name == "ZombieGargantuar", "The regression must use real Sunflower, Zamboni, and Gargantuar production scenes.");
				HolographicPlantSmashTargetRuntimeTest holographicPlantSmashTargetRuntimeTest = this;
				int condition;
				if (zamboni != null && zamboni.attackComponent?.IsReleased == false && zamboni.attackComponent.attackType == "Smash")
				{
					if (gargantuar != null && gargantuar.attackComponent?.IsReleased == false && gargantuar.attackComponent.attackType == "Smash")
					{
						GargantuarSmashComponent gargantuarSmashComponent = gargantuar.gargantuarSmashComponent;
						condition = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
						goto IL_0523;
					}
				}
				condition = 0;
				goto IL_0523;
				IL_0523:
				holographicPlantSmashTargetRuntimeTest.Check((byte)condition != 0, "Both production attackers must expose their authored Smash runtimes.");
				if (!AllValid(zamboniProjection, gargantuarProjection, zamboni, gargantuar) || (zamboni.attackComponent?.IsReleased ?? true) || (gargantuar.attackComponent?.IsReleased ?? true) || (gargantuar.gargantuarSmashComponent?.IsReleased ?? true))
				{
					goto end_IL_00c9;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ZamboniGrid);
				TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(GargantuarGrid);
				Check(zamboniProjection.instance.hologram && !zamboniProjection.instance.canBeCollection && GodotObject.IsInstanceValid(mapCell) && mapCell.characterList.Contains(zamboniProjection), "The Zamboni target must remain a registered non-collectible hologram after releasing grid occupancy.");
				Check(gargantuarProjection.instance.hologram && !gargantuarProjection.instance.canBeCollection && GodotObject.IsInstanceValid(mapCell2) && mapCell2.characterList.Contains(gargantuarProjection), "The Gargantuar target must remain a registered non-collectible hologram after releasing grid occupancy.");
				foreach (TowerDefenseCharacter spawnedCharacter in _spawnedCharacters)
				{
					spawnedCharacter.ProcessMode = ProcessModeEnum.Disabled;
				}
				zamboni.GlobalPosition = zamboniProjection.GlobalPosition;
				zamboni.gridPos = ZamboniGrid;
				gargantuar.GlobalPosition = gargantuarProjection.GlobalPosition;
				gargantuar.gridPos = GargantuarGrid;
				control.isGameRunning = true;
				zamboni.attackComponent.attackType = "Eat";
				Check(!GodotObject.IsInstanceValid(zamboni.attackComponent.GetTarget()), "A normal attack must continue to ignore a non-collectible hologram plant.");
				zamboni.attackComponent.attackType = "Smash";
				Check(!GodotObject.IsInstanceValid(zamboni.attackComponent.GetTarget()), "Vehicle crush targeting must ignore projections.");
				zamboni.attackComponent.target = zamboniProjection;
				Check(!zamboni.attackComponent.CanAttack(), "A cached vehicle target must also reject a projection.");
				gargantuar.attackComponent.attackType = "Smash";
				Check(!GodotObject.IsInstanceValid(gargantuar.attackComponent.GetTarget()), "Gargantuar targeting must ignore projections.");
				gargantuar.attackComponent.target = gargantuarProjection;
				Check(!gargantuar.attackComponent.CanAttack(), "A cached hammer target must also reject a projection.");
				zamboniProjection.instance.hologram = false;
				zamboniProjection.instance.canBeCollection = true;
				gargantuarProjection.instance.hologram = false;
				gargantuarProjection.instance.canBeCollection = true;
				Check(zamboni.attackComponent.GetTarget() == zamboniProjection, "Vehicles must still target tangible plants.");
				Check(gargantuar.attackComponent.GetTarget() == gargantuarProjection, "Gargantuars must still target tangible plants.");
				Check(((TowerDefenseZombieConfig)zamboni.config).smashAttack == 10000.0 && ((TowerDefenseZombieConfig)gargantuar.config).smashAttack == 1800.0, "Both scenarios must retain their authored production Smash payloads.");
				goto end_IL_00b7;
				end_IL_00c9:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[HolographicPlantSmashTargetRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			foreach (TowerDefenseCharacter spawnedCharacter2 in _spawnedCharacters)
			{
				if (GodotObject.IsInstanceValid(spawnedCharacter2) && !spawnedCharacter2.IsQueuedForDeletion())
				{
					spawnedCharacter2.QueueFree();
				}
			}
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
			RestoreGameplayResourceLoadState();
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"HOLOGRAPHIC_PLANT_SMASH_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void PublishFocusedGameplayResourceReady()
	{
		FieldInfo field = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new MissingFieldException(typeof(ResourceManager).FullName, "_gameplayResourceLoadState");
		}
		_previousGameplayResourceLoadState = (GameplayResourceLoadState)field.GetValue(ResourceManager.Instance);
		field.SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
		_gameplayResourceLoadStateOverridden = true;
	}

	private void RestoreGameplayResourceLoadState()
	{
		if (_gameplayResourceLoadStateOverridden && GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			FieldInfo field = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(ResourceManager.Instance, _previousGameplayResourceLoadState);
			}
			_gameplayResourceLoadStateOverridden = false;
		}
	}

	private static void ConfigureHologramPlant(TowerDefensePlant plant)
	{
		if (GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(plant.instance))
		{
			plant.instance.hologram = true;
			plant.instance.canBeCollection = false;
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(plant.gridPos);
			if (GodotObject.IsInstanceValid(mapCell))
			{
				mapCell.ReleaseHologramGridOccupancy(plant);
			}
		}
	}

	private TowerDefenseCharacter SpawnCharacter(string path, Vector2I grid)
	{
		TowerDefenseCharacter towerDefenseCharacter = LoadPacket(path)?.Plant(grid, playAudio: false);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			_spawnedCharacters.Add(towerDefenseCharacter);
		}
		return towerDefenseCharacter;
	}

	private static bool AllValid(params TowerDefenseCharacter[] characters)
	{
		for (int i = 0; i < characters.Length; i++)
		{
			if (!GodotObject.IsInstanceValid(characters[i]))
			{
				return false;
			}
		}
		return true;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = gridNum,
			gridBeginPos = Vector2.Zero,
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, gridNum.X, gridNum.Y)
		});
		for (int i = 1; i <= gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		Register("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
		Register("ZombieZamboni", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn");
		Register("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn");
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
			GD.PushError("[HolographicPlantSmashTargetRuntimeTest] " + message);
		}
	}
}
