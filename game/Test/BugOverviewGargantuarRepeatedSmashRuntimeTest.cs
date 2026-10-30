using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewGargantuarRepeatedSmashRuntimeTest : Node
{
	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn";

	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private static readonly Vector2I ScenarioGrid = new Vector2I(4, 2);

	private const int SmashCycles = 3;

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
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		GargantuarRepeatedSmashControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieGargantuar gargantuar = null;
		TowerDefensePlant wallnut = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00e8;
				}
				RegisterRealFixtures();
				control = new GargantuarRepeatedSmashControlStub
				{
					Name = "GargantuarRepeatedSmashControl",
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
				gargantuar = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres")?.Plant(ScenarioGrid, playAudio: false) as TowerDefenseZombieGargantuar;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(gargantuar) && gargantuar.config?.name == "ZombieGargantuar", "The fixture must instantiate the real Gargantuar packet and scene.");
				BugOverviewGargantuarRepeatedSmashRuntimeTest bugOverviewGargantuarRepeatedSmashRuntimeTest = this;
				TowerDefenseZombieGargantuar towerDefenseZombieGargantuar = gargantuar;
				int condition;
				if (towerDefenseZombieGargantuar != null && towerDefenseZombieGargantuar.attackComponent?.IsReleased == false)
				{
					GargantuarSmashComponent gargantuarSmashComponent = gargantuar.gargantuarSmashComponent;
					condition = ((gargantuarSmashComponent != null && !gargantuarSmashComponent.IsReleased) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewGargantuarRepeatedSmashRuntimeTest.Check((byte)condition != 0, "The real Gargantuar attack and smash component runtimes must be active.");
				BugOverviewGargantuarRepeatedSmashRuntimeTest bugOverviewGargantuarRepeatedSmashRuntimeTest2 = this;
				GroundMoveComponent groundMoveComponent = gargantuar?.groundMoveComponent;
				bugOverviewGargantuarRepeatedSmashRuntimeTest2.Check(groundMoveComponent != null && !groundMoveComponent.IsReleased && groundMoveComponent.HasMovementSource, "The real Gargantuar must retain its authored GroundSlot movement source.");
				if (!GodotObject.IsInstanceValid(gargantuar) || (gargantuar.attackComponent?.IsReleased ?? true) || (gargantuar.gargantuarSmashComponent?.IsReleased ?? true))
				{
					goto end_IL_00e8;
				}
				gargantuar.timeScale = 8.0;
				gargantuar.gridPos = ScenarioGrid;
				gargantuar.Walk();
				await WaitFrames(4);
				Check(gargantuar.CurrentStateHandle?.StableId == "zombie.walk" && gargantuar.groundMoveComponent.Alive, "The scenario must begin through the real Gargantuar walk state.");
				control.isGameRunning = true;
				TowerDefensePacketConfig wallnutPacket = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
				for (int cycle = 1; cycle <= 3; cycle++)
				{
					wallnut = wallnutPacket?.Plant(ScenarioGrid, playAudio: false) as TowerDefensePlant;
					await WaitFrames(5);
					Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", $"Smash cycle {cycle} must instantiate the real Wall-nut packet and scene.");
					TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ScenarioGrid);
					Check(GodotObject.IsInstanceValid(mapCell) && GodotObject.IsInstanceValid(wallnut) && mapCell.characterList.Contains(wallnut) && wallnut.cell == mapCell, $"Smash cycle {cycle} must place the real Wall-nut in the real map cell.");
					if (!GodotObject.IsInstanceValid(wallnut))
					{
						continue;
					}
					gargantuar.GlobalPosition = wallnut.GlobalPosition + new Vector2(45f, 0f);
					gargantuar.gridPos = ScenarioGrid;
					bool attackPoseObserved = false;
					bool targetObserved = false;
					bool impactObserved = false;
					int frame = 0;
					while (frame < 240)
					{
						await WaitFrames(1);
						attackPoseObserved |= gargantuar.CurrentStateHandle?.StableId == "zombie.attack" && gargantuar.sprite?.clip == gargantuar.attackAnimeClip;
						targetObserved |= gargantuar.attackComponent.target == wallnut;
						if (!wallnut.die && !wallnut.nearDie && !wallnut.isDestroy)
						{
							TowerDefenseCharacterInstance instance = wallnut.instance;
							if (instance == null || !(instance.hitpoints <= 0.0))
							{
								frame++;
								continue;
							}
						}
						impactObserved = true;
						break;
					}
					Check(attackPoseObserved, $"Smash cycle {cycle} must visibly enter the real Smash animation state.");
					Check(targetObserved, $"Smash cycle {cycle} must naturally acquire the replacement real Wall-nut.");
					Check(impactObserved, $"Smash cycle {cycle} must emit its authored impact and destroy the real Wall-nut.");
					if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
					{
						wallnut.QueueFree();
					}
					wallnut = null;
					await WaitFrames(2);
				}
				bool returnedToWalk = false;
				for (int cycle = 0; cycle < 240; cycle++)
				{
					await WaitFrames(1);
					if (gargantuar.CurrentStateHandle?.StableId == "zombie.walk")
					{
						returnedToWalk = true;
						break;
					}
				}
				Check(returnedToWalk && !GodotObject.IsInstanceValid(gargantuar.attackComponent.target), "After three completed Smash cycles, the Gargantuar must release the missing target and resume walking.");
				goto end_IL_00c5;
				end_IL_00e8:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewGargantuarRepeatedSmashRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00c5;
			}
			return;
			end_IL_00c5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(wallnut) && !wallnut.IsQueuedForDeletion())
			{
				wallnut.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gargantuar) && !gargantuar.IsQueuedForDeletion())
			{
				gargantuar.QueueFree();
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
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 22;
		GD.Print($"GARGANTUAR_REPEATED_SMASH_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
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
		RegisterPacket("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
		RegisterPacket("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres");
		RegisterCharacter("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
		RegisterCharacter("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn");
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
			GD.PushError("[BugOverviewGargantuarRepeatedSmashRuntimeTest] " + message);
		}
	}
}
