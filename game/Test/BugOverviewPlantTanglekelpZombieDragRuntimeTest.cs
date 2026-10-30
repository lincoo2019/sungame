using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewPlantTanglekelpZombieDragRuntimeTest : Node
{
	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Packet/PlantTanglekelp.tres";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Scene/TowerDefensePlantTanglekelp.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I EncounterGrid = new Vector2I(4, 3);

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
		PlantTanglekelpZombieDragRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required runtime autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new PlantTanglekelpZombieDragRuntimeControlStub
				{
					Name = "PlantTanglekelpZombieDragRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
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
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePlantTanglekelp plant = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Packet/PlantTanglekelp.tres")?.Plant(EncounterGrid, playAudio: false) as TowerDefensePlantTanglekelp;
				await WaitFrames(75);
				TowerDefenseZombieNormal zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(EncounterGrid, playAudio: false) as TowerDefenseZombieNormal;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(plant), "The real TowerDefensePlantTanglekelp scene must spawn.");
				Check(GodotObject.IsInstanceValid(zombie), "The real TowerDefenseZombieNormal scene must spawn.");
				if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(zombie))
				{
					throw new InvalidOperationException("The real Tanglekelp encounter did not spawn.");
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				plant.inGame = true;
				zombie.inGame = true;
				zombie.gridPos = EncounterGrid;
				zombie.GlobalPosition = plant.GlobalPosition + new Vector2(28f, 0f);
				await WaitFrames(2);
				control.isGameRunning = true;
				TanglekelpComponent runtime = plant.componentManager.GetRuntime<TanglekelpComponent>();
				AttackComponent attack = plant.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				Check(runtime != null && !runtime.IsReleased && runtime.Alive, "The real Tanglekelp plant must expose its active drag runtime.");
				Check(attack != null && !attack.IsReleased, "The real Tanglekelp plant must expose its production attack runtime.");
				Check(runtime.attackComponent == attack, "The Tanglekelp runtime must bind the real attack runtime after all components are created.");
				Check(!plant.componentRunning, "The real Tanglekelp plant must finish its planting animation before target acquisition.");
				if (runtime == null || runtime.IsReleased || attack == null || attack.IsReleased)
				{
					throw new InvalidOperationException("The real Tanglekelp runtimes are unavailable.");
				}
				bool dragBegan = false;
				bool dragCompleted = false;
				runtime.dragDelay = 0f;
				runtime.OnDragBegin += delegate(TowerDefenseCharacter target)
				{
					if (target == zombie)
					{
						dragBegan = true;
					}
				};
				runtime.OnDrag += delegate(TowerDefenseCharacter target, bool success)
				{
					if (target == zombie && success)
					{
						dragCompleted = true;
					}
				};
				Check(zombie.config?.canDragIntoWater ?? false, "The real normal zombie must be eligible for a water drag.");
				GD.Print($"PLANT_TANGLEKELP_RUNTIME_DIAGNOSTIC state={runtime.StateMachine?.CurrentStateHandle?.StableId ?? "<null>"} initialized={runtime.StateMachine?.IsInitialized ?? false} inside={plant.IsInsideComponentBattlefield} game={manager.IsGameRunning()} inGame={plant.inGame} componentAlive={plant.componentAlive} componentRunning={plant.componentRunning}");
				runtime.IdleProcessing(0.0);
				await WaitFrames(16);
				Check(attack.target == zombie || dragBegan, "The real Tanglekelp plant must acquire the overlapping real zombie through live physics.");
				Check(dragBegan, "The production Tanglekelp runtime must start dragging the acquired zombie.");
				Check(dragCompleted, "The production Tanglekelp runtime must complete a successful water drag.");
				Check(!GodotObject.IsInstanceValid(zombie) || zombie.die || zombie.isDestroy, "The dragged real zombie must be removed from play.");
				Check(!GodotObject.IsInstanceValid(plant) || plant.isDestroy, "The one-use real Tanglekelp plant must consume itself after dragging.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[PlantTanglekelpZombieDrag] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
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
		bool flag = _failures == 0 && _checks == 14;
		GD.Print($"BUG_OVERVIEW_PLANT_TANGLEKELP_ZOMBIE_DRAG_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig()
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(gridNum.X + 1);
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(gridNum.Y + 1);
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig towerDefenseCellConfig = new TowerDefenseCellConfig();
				if (i == EncounterGrid.X && j == EncounterGrid.Y)
				{
					towerDefenseCellConfig.gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.WATER,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					};
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(towerDefenseCellConfig);
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantTanglekelp", "res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Packet/PlantTanglekelp.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantTanglekelp", "res://Asset/Anime/Character/Plant/Chapter0/Tanglekelp/Scene/TowerDefensePlantTanglekelp.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
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
			GD.PushError("[PlantTanglekelpZombieDrag] " + message);
		}
	}
}
