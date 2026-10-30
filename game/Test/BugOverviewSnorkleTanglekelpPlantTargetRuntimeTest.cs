using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSnorkleTanglekelpPlantTargetRuntimeTest : Node
{
	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleTanglekelp.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Tanglekelp/TowerDefenseZombieSnorkleTanglekelp.tscn";

	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn";

	private const string RobotPacketPath = "res://Asset/Anime/Character/Plant/Star/Robot/Packet/PlantRobot.tres";

	private const string RobotScenePath = "res://Asset/Anime/Character/Plant/Star/Robot/Scene/TowerDefensePlantRobot.tscn";

	private static readonly Vector2I PlantGrid = new Vector2I(3, 2);

	private static readonly Vector2I ZombieGrid = new Vector2I(4, 2);

	private static readonly Vector2I RobotGrid = new Vector2I(3, 3);

	private static readonly Vector2I RobotZombieGrid = new Vector2I(5, 3);

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
		SnorkleTanglekelpPlantTargetRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 8;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_0090;
				}
				RegisterRealFixtures();
				control = new SnorkleTanglekelpPlantTargetRuntimeControlStub
				{
					Name = "SnorkleTanglekelpPlantTargetRuntimeControl",
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
				TowerDefensePlantPeaShooterSingle plant = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres")?.Plant(PlantGrid, playAudio: false) as TowerDefensePlantPeaShooterSingle;
				await WaitFrames(4);
				TowerDefenseZombieSnorkleTanglekelp zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleTanglekelp.tres")?.Plant(ZombieGrid, playAudio: false) as TowerDefenseZombieSnorkleTanglekelp;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(zombie), "Real PeaShooterSingle and SnorkleTanglekelp characters must spawn.");
				if (!GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_0090;
				}
				plant.ProcessMode = ProcessModeEnum.Disabled;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				plant.inGame = true;
				zombie.inGame = true;
				zombie.GlobalPosition = new Vector2(plant.GlobalPosition.X + 35f, plant.GlobalPosition.Y);
				zombie.gridPos = PlantGrid;
				await WaitFrames(2);
				control.isGameRunning = true;
				TanglekelpComponent runtime = zombie.componentManager.GetRuntime<TanglekelpComponent>();
				AttackComponent runtime2 = zombie.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				Check(runtime != null && !runtime.IsReleased && runtime2 != null && !runtime2.IsReleased, "The real zombie must expose active Tanglekelp and Attack runtimes.");
				if (runtime == null || runtime.IsReleased || runtime2 == null || runtime2.IsReleased)
				{
					goto end_IL_0090;
				}
				bool dragBegan = false;
				bool dragCompleted = false;
				runtime.dragDelay = 0f;
				runtime.OnDragBegin += delegate(TowerDefenseCharacter target)
				{
					if (target == plant)
					{
						dragBegan = true;
					}
				};
				runtime.OnDrag += delegate(TowerDefenseCharacter target, bool success)
				{
					if (target == plant && success)
					{
						dragCompleted = true;
					}
				};
				zombie.inWater = true;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				int num = 32;
				Check(runtime.Alive && (zombie.instance.maskFlags & num) != 0, $"Entering water must activate the drag runtime and submerged mask; alive={runtime.Alive}, mask={zombie.instance.maskFlags}.");
				Check(!zombie.componentRunning, "The submerged locomotion fixture must leave the independent drag runtime eligible.");
				bool flag = runtime2.CanAttackOnce();
				Check(flag && runtime2.target == plant, "The real submerged water-grass zombie must acquire the adjacent real plant.");
				runtime.IdleProcessing(0.0);
				await WaitFrames(6);
				Check(dragBegan, "The production Tanglekelp runtime must begin dragging its acquired plant.");
				Check(dragCompleted, "The real plant must pass the production water-drag eligibility check.");
				Check(!GodotObject.IsInstanceValid(plant) || plant.die || plant.isDestroy, "A completed water-grass drag must remove the plant instead of ignoring it.");
				Check(!GodotObject.IsInstanceValid(zombie) || zombie.isDestroy, "The one-use water-grass zombie must consume itself after a successful drag.");
				await WaitFrames(4);
				TowerDefensePlantRobot robot = LoadPacket("res://Asset/Anime/Character/Plant/Star/Robot/Packet/PlantRobot.tres")?.Plant(RobotGrid, playAudio: false) as TowerDefensePlantRobot;
				await WaitFrames(6);
				TowerDefenseZombieSnorkleTanglekelp robotZombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleTanglekelp.tres")?.Plant(RobotZombieGrid, playAudio: false) as TowerDefenseZombieSnorkleTanglekelp;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(robot) && GodotObject.IsInstanceValid(robotZombie), "The real PlantRobot and a second real SnorkleTanglekelp must spawn.");
				if (!GodotObject.IsInstanceValid(robot) || !GodotObject.IsInstanceValid(robotZombie))
				{
					goto end_IL_0090;
				}
				robot.ProcessMode = ProcessModeEnum.Disabled;
				robotZombie.ProcessMode = ProcessModeEnum.Disabled;
				robot.inGame = true;
				robotZombie.inGame = true;
				robotZombie.GlobalPosition = new Vector2(robot.GlobalPosition.X + 35f, robot.GlobalPosition.Y);
				robotZombie.gridPos = RobotGrid;
				await WaitFrames(2);
				TanglekelpComponent runtime3 = robotZombie.componentManager.GetRuntime<TanglekelpComponent>();
				AttackComponent runtime4 = robotZombie.componentManager.GetRuntime<AttackComponent>("character.attack.0");
				Check(runtime3 != null && !runtime3.IsReleased && runtime4 != null && !runtime4.IsReleased, "The second real zombie must expose active Tanglekelp and Attack runtimes.");
				if (runtime3 == null || runtime3.IsReleased || runtime4 == null || runtime4.IsReleased)
				{
					goto end_IL_0090;
				}
				BugOverviewSnorkleTanglekelpPlantTargetRuntimeTest bugOverviewSnorkleTanglekelpPlantTargetRuntimeTest = this;
				TowerDefenseCharacterConfig config = robot.config;
				bugOverviewSnorkleTanglekelpPlantTargetRuntimeTest.Check(config != null && !config.canDragIntoWater, "The real PlantRobot must retain its configured immunity to lethal water removal.");
				BugOverviewSnorkleTanglekelpPlantTargetRuntimeTest bugOverviewSnorkleTanglekelpPlantTargetRuntimeTest2 = this;
				TowerDefenseCharacterConfig config2 = robot.config;
				bugOverviewSnorkleTanglekelpPlantTargetRuntimeTest2.Check(config2 != null && config2.dragHurt > 0.0, "The real PlantRobot must expose its configured non-lethal water-grass damage.");
				bool robotDragBegan = false;
				bool robotDragCompleted = false;
				runtime3.dragDelay = 0f;
				runtime3.OnDragBegin += delegate(TowerDefenseCharacter target)
				{
					if (target == robot)
					{
						robotDragBegan = true;
					}
				};
				runtime3.OnDrag += delegate(TowerDefenseCharacter target, bool success)
				{
					if (target == robot && success)
					{
						robotDragCompleted = true;
					}
				};
				robotZombie.inWater = true;
				int num2 = 32;
				Check(runtime3.Alive && (robotZombie.instance.maskFlags & num2) != 0, "The second real zombie must activate its water-grass drag runtime underwater.");
				Check(!robotZombie.componentRunning, "The second submerged zombie must leave its drag runtime eligible.");
				double robotHitpointsBefore = robot.instance.hitpoints;
				bool flag2 = runtime4.CanAttackOnce();
				Check(flag2 && runtime4.target == robot, "The real water-grass zombie must acquire the overlapping real PlantRobot.");
				runtime3.IdleProcessing(0.0);
				await WaitFrames(6);
				Check(robotDragBegan, "The production water-grass runtime must begin its real PlantRobot drag.");
				Check(robotDragCompleted, "PlantRobot's configured drag damage must count as a successful water-grass interaction.");
				Check(Math.Abs(robotHitpointsBefore - robot.instance.hitpoints - robot.config.dragHurt) < 0.001, $"The real PlantRobot must take exactly its configured drag damage; before={robotHitpointsBefore}, after={robot.instance.hitpoints}, dragHurt={robot.config.dragHurt}.");
				Check(GodotObject.IsInstanceValid(robot) && !robot.die && !robot.isDestroy, "PlantRobot must survive non-lethal drag damage instead of being removed.");
				Check(!GodotObject.IsInstanceValid(robotZombie) || robotZombie.isDestroy, "The one-use water-grass zombie must consume itself after affecting PlantRobot.");
				goto end_IL_0061;
				end_IL_0090:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[SnorkleTanglekelpPlantTarget] Unexpected exception: {value}");
				goto end_IL_0061;
			}
			return;
			end_IL_0061:;
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
		bool flag3 = _failures == 0 && _checks == 23;
		GD.Print($"BUG_OVERVIEW_I125_SNORKLE_TANGLEKELP_TARGET_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
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
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
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
		RegisterPacket("ZombieSnorkleTanglekelp", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Packet/ZombieSnorkleTanglekelp.tres");
		RegisterPacket("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Packet/PlantPeaShooterSingle.tres");
		RegisterPacket("PlantRobot", "res://Asset/Anime/Character/Plant/Star/Robot/Packet/PlantRobot.tres");
		RegisterCharacter("ZombieSnorkleTanglekelp", "res://Asset/Anime/Character/Zombie/Chapter3/Snorkle/Scene/Tanglekelp/TowerDefenseZombieSnorkleTanglekelp.tscn");
		RegisterCharacter("PlantPeaShooterSingle", "res://Asset/Anime/Character/Plant/Chapter0/PeaShooterSingle/Scene/TowerDefensePlantPeaShooterSingle.tscn");
		RegisterCharacter("PlantRobot", "res://Asset/Anime/Character/Plant/Star/Robot/Scene/TowerDefensePlantRobot.tscn");
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
			GD.PushError("[SnorkleTanglekelpPlantTarget] " + message);
		}
	}
}
