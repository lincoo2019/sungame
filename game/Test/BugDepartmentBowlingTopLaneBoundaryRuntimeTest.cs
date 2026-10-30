using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentBowlingTopLaneBoundaryRuntimeTest : Node
{
	private const string LevelPath = "res://Asset/Config/Level/TowerDefense/Survival/Entertainment/Survival_Level_Entertainment5_3.tres";

	private const string MapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnBig.tres";

	private const string MapScenePath = "res://Asset/Config/Map/Frontlawn/Scene/DayBig/TowerDefenseMapFrontlawnBig.tscn";

	private const string BowlingPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnutBowling.tres";

	private const string BowlingScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantBowlingWallnut.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I TopLaneBowlingGrid = new Vector2I(2, 1);

	private static readonly Vector2I TopLaneZombieGrid = new Vector2I(4, 1);

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
		BowlingTopLaneBoundaryControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseMapFrontlawnBig mapScene = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d0;
				}
				TowerDefenseLevelConfig towerDefenseLevelConfig = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Survival/Entertainment/Survival_Level_Entertainment5_3.tres", null, ResourceLoader.CacheMode.Ignore);
				TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnBig.tres", null, ResourceLoader.CacheMode.Ignore);
				mapScene = ResourceLoader.Load<PackedScene>("res://Asset/Config/Map/Frontlawn/Scene/DayBig/TowerDefenseMapFrontlawnBig.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseMapFrontlawnBig>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && towerDefenseLevelConfig.name == "Survival_Level_Entertainment5_3" && towerDefenseLevelConfig.levelName == "坚果保龄球（无尽）" && towerDefenseLevelConfig.map == "FrontlawnBig" && towerDefenseLevelConfig.waveManager?.survival.AsString() == "WallnutBowlingEndlessNormal", "The scenario must load the real Wall-nut Bowling Endless level and survival configuration.");
				Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig?.conveyorData) && HasConveyorPacket(towerDefenseLevelConfig.conveyorData, "PlantWallnutBowling"), "The real endless conveyor must include the reported Wall-nut Bowling packet.");
				Check(GodotObject.IsInstanceValid(towerDefenseMapConfig) && towerDefenseMapConfig.gridBeginPos == new Vector2(240f, 60f) && towerDefenseMapConfig.gridSize == new Vector2(69f, 76f) && towerDefenseMapConfig.gridNum == new Vector2I(11, 7) && Mathf.IsEqualApprox((float)towerDefenseMapConfig.plantOffset, 25f), "The scenario must use the production FrontlawnBig 11x7 geometry and 25px plant offset.");
				Check(GodotObject.IsInstanceValid(mapScene) && towerDefenseMapConfig?.mapScenePath == "res://Asset/Config/Map/Frontlawn/Scene/DayBig/TowerDefenseMapFrontlawnBig.tscn", "The production FrontlawnBig map scene must load from the real map configuration.");
				if (!GodotObject.IsInstanceValid(towerDefenseLevelConfig) || !GodotObject.IsInstanceValid(towerDefenseMapConfig) || !GodotObject.IsInstanceValid(mapScene))
				{
					goto end_IL_00d0;
				}
				RegisterRealFixtures();
				control = new BowlingTopLaneBoundaryControlStub
				{
					Name = "BowlingTopLaneBoundaryControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = towerDefenseLevelConfig
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
				manager.currentControl = control;
				manager.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
				manager.gridSize = towerDefenseMapConfig.gridSize;
				manager.gridNum = towerDefenseMapConfig.gridNum;
				mapControl = new TowerDefenseMapControl
				{
					Name = "FrontlawnBigMapControl"
				};
				mapFeature = CreateMapFeature(mapControl, towerDefenseMapConfig);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePlantBowlingWallnut bowlingPlant = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnutBowling.tres")?.Plant(TopLaneBowlingGrid, playAudio: false) as TowerDefensePlantBowlingWallnut;
				await WaitFrames(5);
				TowerDefenseZombie zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(TopLaneZombieGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(bowlingPlant) && bowlingPlant.config?.name == "PlantWallnutBowling" && GodotObject.IsInstanceValid(zombie) && zombie.config?.name == "ZombieNormal", "The live scenario must instantiate the real bowling Wall-nut and normal zombie scenes.");
				if (!GodotObject.IsInstanceValid(bowlingPlant) || !GodotObject.IsInstanceValid(zombie))
				{
					goto end_IL_00d0;
				}
				bowlingPlant.ProcessMode = ProcessModeEnum.Disabled;
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				BowlingComponent bowlingComponent = bowlingPlant.componentManager?.GetRuntime<BowlingComponent>();
				CharacterMoveComponent characterMoveComponent = bowlingPlant.componentManager?.GetRuntime<CharacterMoveComponent>();
				Check(bowlingComponent != null && !bowlingComponent.IsReleased && characterMoveComponent != null && !characterMoveComponent.IsReleased && Mathf.IsEqualApprox((float)bowlingComponent.topBoundaryOffset, 50f), "The real bowling character must expose its production Bowling and movement runtimes.");
				if ((bowlingComponent?.IsReleased ?? true) || (characterMoveComponent?.IsReleased ?? true))
				{
					goto end_IL_00d0;
				}
				control.isGameRunning = true;
				bowlingComponent.SetAlive(alive: true);
				bowlingComponent.isRoll = true;
				bowlingComponent.RefreshMapMetrics();
				Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(TopLaneBowlingGrid);
				double num = manager.GetMapGroundUp() + bowlingComponent.topBoundaryOffset;
				Check(Mathf.IsEqualApprox(mapCellPlantPos.Y, 85f) && num > (double)mapCellPlantPos.Y, "The real 0.25 geometry must reproduce the old 85px spawn versus 110px fixed-boundary conflict.");
				bowlingPlant.GlobalPosition = mapCellPlantPos;
				bowlingPlant.gridPos = TopLaneBowlingGrid;
				bowlingComponent.hitLineSave = -1;
				characterMoveComponent.velocity = new Vector2(200f, 0f);
				bowlingComponent.RollProcessing(0.0);
				Check(Mathf.IsEqualApprox(bowlingPlant.GlobalPosition.Y, mapCellPlantPos.Y), "A legal top-lane spawn must not be clamped downward on its first rolling frame.");
				Check(Mathf.IsZeroApprox(characterMoveComponent.velocity.Y), "A legal top-lane spawn must keep horizontal velocity instead of being forced to turn.");
				bowlingPlant.GlobalPosition = mapCellPlantPos + new Vector2(0f, -5f);
				bowlingComponent.hitLineSave = -1;
				characterMoveComponent.velocity = new Vector2(200f, -250f);
				bowlingComponent.RollProcessing(0.0);
				Check(Mathf.IsEqualApprox(bowlingPlant.GlobalPosition.Y, mapCellPlantPos.Y) && characterMoveComponent.velocity.Y > 0f, "A bowling object that truly exits above the top row must still clamp and rebound downward.");
				bowlingPlant.GlobalPosition = mapCellPlantPos;
				bowlingPlant.gridPos = TopLaneBowlingGrid;
				zombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TopLaneZombieGrid);
				zombie.gridPos = TopLaneZombieGrid;
				bowlingComponent.hitLineSave = -1;
				characterMoveComponent.velocity = new Vector2(200f, 0f);
				double hitpoints = zombie.instance.hitpoints;
				int hitNum = bowlingComponent.hitNum;
				bowlingComponent.BowlingHit(zombie);
				Check(bowlingComponent.hitNum == hitNum + 1 && zombie.instance.hitpoints < hitpoints, "The real bowling collision handler must still damage the real zombie.");
				Check(bowlingComponent.hitLineSave == 1 && characterMoveComponent.velocity.Y > 0f, "A real collision in row one must still select the normal downward turn rule.");
				goto end_IL_00be;
				end_IL_00d0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentBowlingTopLaneBoundaryRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00be;
			}
			return;
			end_IL_00be:;
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
			if (GodotObject.IsInstanceValid(mapScene))
			{
				mapScene.Free();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 13;
		GD.Print($"BUG_DEPARTMENT_BOWLING_TOP_LANE_BOUNDARY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static bool HasConveyorPacket(TowerDefenseConveyorConfig conveyor, string name)
	{
		if (!GodotObject.IsInstanceValid(conveyor))
		{
			return false;
		}
		foreach (TowerDefenseConveyorPacketConfig packet in conveyor.packetList)
		{
			if (GodotObject.IsInstanceValid(packet) && packet.name == name)
			{
				return true;
			}
		}
		return false;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseMapConfig mapConfig)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = mapConfig,
			mapConfig = mapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(mapConfig.gridNum.X + 1);
		for (int i = 0; i <= mapConfig.gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(mapConfig.gridNum.Y + 1);
			for (int j = 1; j <= mapConfig.gridNum.Y; j++)
			{
				TowerDefenseCellConfig config = ((i == 0) ? new TowerDefenseCellConfig() : (mapConfig.GetEffectiveCellConfig(i, j) ?? new TowerDefenseCellConfig()));
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(config);
				array[j] = towerDefenseCellInstance;
			}
			towerDefenseBattleFeatureMap.plantGrid[i] = array;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(mapConfig.gridNum.Y + 1);
		towerDefenseBattleFeatureMap.lineUse.Resize(mapConfig.gridNum.Y + 1);
		for (int k = 1; k <= mapConfig.gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantWallnutBowling", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnutBowling.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantWallnutBowling", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantBowlingWallnut.tscn");
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
			GD.PushError("[BugDepartmentBowlingTopLaneBoundaryRuntimeTest] " + message);
		}
	}
}
