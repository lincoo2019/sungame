using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class FumeShroomAwakeGridRangeRuntimeTest : Node
{
	private const string FumePacketPath = "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres";

	private const string FumeScenePath = "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Scene/TowerDefensePlantFumeShroom.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I FumeGrid = new Vector2I(2, 3);

	private static readonly Vector2I InRangeGrid = new Vector2I(6, 3);

	private static readonly Vector2I BeyondFourAndHalfCellsGrid = new Vector2I(7, 3);

	private static readonly Vector2I OutOfRangeGrid = new Vector2I(8, 3);

	private static readonly Vector2I WrongRowGrid = new Vector2I(6, 2);

	private static readonly Vector2 ScenarioGridSize = new Vector2(100f, 76f);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

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
		FumeShroomAwakeGridRangeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new FumeShroomAwakeGridRangeControlStub
				{
					Name = "FumeShroomAwakeGridRangeControl",
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
				manager.gridSize = ScenarioGridSize;
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum, manager.gridSize);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefenseCharacter towerDefenseCharacter = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres")?.Plant(FumeGrid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
				TowerDefensePlantFumeShroom fume = towerDefenseCharacter as TowerDefensePlantFumeShroom;
				TowerDefenseZombie inRange = SpawnZombie(InRangeGrid);
				TowerDefenseZombie beyondFourAndHalfCells = SpawnZombie(BeyondFourAndHalfCellsGrid);
				TowerDefenseZombie outOfRange = SpawnZombie(OutOfRangeGrid);
				TowerDefenseZombie wrongRow = SpawnZombie(WrongRowGrid);
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(fume) && fume.config?.name == "PlantFumeShroom", "The real production Fume-shroom packet and scene must instantiate.");
				Check(IsRealNormalZombie(inRange) && IsRealNormalZombie(beyondFourAndHalfCells) && IsRealNormalZombie(outOfRange) && IsRealNormalZombie(wrongRow), "Every target fixture must use the real production Normal zombie.");
				AttackComponent attack = fume?.componentManager?.GetRuntime<AttackComponent>("character.attack.0");
				SleepComponent sleep = fume?.componentManager?.GetRuntime<SleepComponent>("character.sleep");
				Check(attack != null && !attack.IsReleased && sleep != null && !sleep.IsReleased, "Fume-shroom must expose its production Attack and Sleep runtimes.");
				if (!GodotObject.IsInstanceValid(fume) || !IsRealNormalZombie(inRange) || !IsRealNormalZombie(beyondFourAndHalfCells) || !IsRealNormalZombie(outOfRange) || !IsRealNormalZombie(wrongRow) || attack == null || attack.IsReleased || sleep == null || sleep.IsReleased)
				{
					throw new InvalidOperationException("The production Fume-shroom scenario is incomplete.");
				}
				fume.ProcessMode = ProcessModeEnum.Disabled;
				inRange.ProcessMode = ProcessModeEnum.Disabled;
				beyondFourAndHalfCells.ProcessMode = ProcessModeEnum.Disabled;
				outOfRange.ProcessMode = ProcessModeEnum.Disabled;
				wrongRow.ProcessMode = ProcessModeEnum.Disabled;
				inRange.instance.canBeCollection = true;
				beyondFourAndHalfCells.instance.canBeCollection = true;
				outOfRange.instance.canBeCollection = true;
				wrongRow.instance.canBeCollection = true;
				Check(string.Equals(fume.config.sleepTime, "Day", StringComparison.OrdinalIgnoreCase) && sleep.CanSleep(), "Production Fume-shroom must be naturally sleep-eligible on a daytime map.");
				fume.Sleep();
				await WaitFrames(1);
				Check(fume.IsSleep() && !fume.componentAlive, "The daytime fixture must put Fume-shroom into its real sleeping state.");
				fume.WakeUp();
				sleep.SleepExited();
				attack.SetAlive(alive: true);
				attack.alive = true;
				attack.groundRight = 10000.0;
				attack.timer = 0.0;
				attack.checkIntrevalNow = 0;
				control.isGameRunning = true;
				Check(!fume.IsSleep() && fume.instance.wakeUp && fume.componentAlive && attack.alive, "The focused attack probe must explicitly wake Fume-shroom without changing the save.");
				Check(attack.CheckAreaShapeCount == 1 && attack.checkLine && attack.TryGetCheckAreaSegmentEnd(0, out var endpoint) && Mathf.IsEqualApprox(Mathf.Abs(endpoint.X), ScenarioGridSize.X * 4.5f), "Fume-shroom must retain its authored 4.5-cell same-row attack range.");
				List<TowerDefenseCharacter> targetList = attack.GetTargetList();
				Check(targetList.Contains(inRange), "An in-range same-row zombie must be returned by the grid-range query.");
				Check(!targetList.Contains(beyondFourAndHalfCells), "A zombie whose grid anchor is beyond the authored 4.5-cell endpoint must not be returned.");
				Check(!targetList.Contains(outOfRange), "An out-of-range same-row zombie must not be returned.");
				Check(!targetList.Contains(wrongRow), "A zombie in another row must not be returned.");
				attack.target = null;
				Check(attack.CanAttackOnce() && attack.target == inRange, "Awake Fume-shroom must acquire the real in-range zombie.");
				double hitpoints = inRange.instance.hitpoints;
				double hitpoints2 = beyondFourAndHalfCells.instance.hitpoints;
				double hitpoints3 = outOfRange.instance.hitpoints;
				double hitpoints4 = wrongRow.instance.hitpoints;
				attack.AttackEventExecute();
				Check(inRange.instance.hitpoints < hitpoints, "Fume-shroom's authored event must damage the in-range zombie.");
				Check(Mathf.IsEqualApprox((float)beyondFourAndHalfCells.instance.hitpoints, (float)hitpoints2), "A zombie beyond the 4.5-cell endpoint must not take damage.");
				Check(Mathf.IsEqualApprox((float)outOfRange.instance.hitpoints, (float)hitpoints3), "The out-of-range zombie must not take damage.");
				Check(Mathf.IsEqualApprox((float)wrongRow.instance.hitpoints, (float)hitpoints4), "The wrong-row zombie must not take damage.");
				inRange.gridPos = OutOfRangeGrid;
				inRange.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(OutOfRangeGrid);
				attack.checkIntrevalNow = 1;
				await WaitFrames(2);
				Check(!attack.CanAttack() && !GodotObject.IsInstanceValid(attack.target), "A cached target moved outside the configured cells must be invalidated.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[FumeShroomAwakeGridRangeRuntimeTest] Unexpected exception: {value}");
			}
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 18;
		GD.Print($"FUME_SHROOM_AWAKE_GRID_RANGE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool IsRealNormalZombie(TowerDefenseZombie zombie)
	{
		if (GodotObject.IsInstanceValid(zombie))
		{
			return zombie.config?.name == "ZombieNormal";
		}
		return false;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static TowerDefenseZombie SpawnZombie(Vector2I grid)
	{
		return LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(grid, playAudio: false) as TowerDefenseZombie;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum, Vector2 gridSize)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = gridSize,
				plantOffset = 50.0,
				isNight = false
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				TowerDefenseCellConfig config = new TowerDefenseCellConfig
				{
					gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
					{
						TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
						TowerDefenseEnum.PLANTGRIDTYPE.AIR
					}
				};
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(config);
			}
		}
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantFumeShroom", "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantFumeShroom", "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Scene/TowerDefensePlantFumeShroom.tscn");
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
			GD.PushError("[FumeShroomAwakeGridRangeRuntimeTest] " + message);
		}
	}
}
