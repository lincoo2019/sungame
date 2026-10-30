using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentDuckRiderCrossLaneSmashRuntimeTest : Node
{
	private const string DuckPacketPath = "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres";

	private const string DuckScenePath = "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Scene/TowerDefenseZombieDuckRider.tscn";

	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private static readonly Vector2I DuckGrid = new Vector2I(5, 2);

	private static readonly Vector2I SameLaneGrid = new Vector2I(4, 2);

	private static readonly Vector2I NextLaneGrid = new Vector2I(4, 3);

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
		DuckRiderCrossLaneControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d2;
				}
				RegisterRealFixtures();
				control = new DuckRiderCrossLaneControlStub
				{
					Name = "DuckRiderCrossLaneControl",
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
				TowerDefensePlant sameLane = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres")?.Plant(SameLaneGrid, playAudio: false) as TowerDefensePlant;
				TowerDefensePlant nextLane = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres")?.Plant(NextLaneGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				TowerDefenseZombieDuckRider duck = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres")?.Plant(DuckGrid, playAudio: false) as TowerDefenseZombieDuckRider;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(duck) && duck.config?.name == "ZombieDuckRider", "The scenario must spawn the corresponding real Duck Rider scene.");
				Check(GodotObject.IsInstanceValid(sameLane) && GodotObject.IsInstanceValid(nextLane) && sameLane.config?.name == "PlantSunFlower" && nextLane.config?.name == "PlantSunFlower", "The scenario must plant real Sunflower objects in the current and next lanes.");
				Check(duck != null && duck.attackComponent?.IsReleased == false && duck.attackComponent.attackType == "Smash", "The real Duck Rider must expose its production smash component.");
				if (!GodotObject.IsInstanceValid(duck) || !GodotObject.IsInstanceValid(sameLane) || !GodotObject.IsInstanceValid(nextLane) || (duck.attackComponent?.IsReleased ?? true))
				{
					goto end_IL_00d2;
				}
				duck.ProcessMode = ProcessModeEnum.Disabled;
				sameLane.ProcessMode = ProcessModeEnum.Disabled;
				nextLane.ProcessMode = ProcessModeEnum.Disabled;
				control.isGameRunning = true;
				duck.GlobalPosition = new Vector2(500f, 200f);
				duck.gridPos = DuckGrid;
				nextLane.GlobalPosition = duck.GlobalPosition;
				nextLane.gridPos = NextLaneGrid;
				sameLane.GlobalPosition = duck.GlobalPosition + new Vector2(-400f, 0f);
				sameLane.gridPos = SameLaneGrid;
				await WaitFrames(3);
				AttackComponent attack = duck.attackComponent;
				attack.target = null;
				attack.InvalidateCheckRect();
				Check(attack.checkLine, "Duck Rider smash targeting must be explicitly lane-scoped.");
				Check(!attack.TryRetargetImmediately() && !GodotObject.IsInstanceValid(attack.target), "Duck Rider must not acquire or crush a real plant whose grid row is the next lane.");
				nextLane.GlobalPosition = duck.GlobalPosition + new Vector2(-400f, 76f);
				sameLane.GlobalPosition = duck.GlobalPosition;
				await WaitFrames(3);
				attack.target = null;
				attack.InvalidateCheckRect();
				Check(attack.TryRetargetImmediately(), "Duck Rider must still acquire a colliding real plant in its own lane.");
				Check(attack.target == sameLane, "Duck Rider must select the real same-lane Sunflower after rejecting the next lane.");
				Check(attack.CanAttack() && attack.target == sameLane, "The accepted same-lane smash target must remain valid for the committed attack.");
				goto end_IL_00b7;
				end_IL_00d2:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentDuckRiderCrossLaneSmashRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks >= 9;
		GD.Print($"BUG_DEPARTMENT_DUCK_RIDER_CROSS_LANE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridBeginPos = Vector2.Zero,
				gridSize = new Vector2(100f, 76f),
				plantOffset = 50.0
			}
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
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
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
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
		RegisterPacket("ZombieDuckRider", "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Packet/ZombieDuckRider.tres");
		RegisterPacket("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres");
		RegisterCharacter("ZombieDuckRider", "res://Asset/Anime/Character/Zombie/Chapter3/DuckRider/Scene/TowerDefenseZombieDuckRider.tscn");
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
			GD.PushError("[BugDepartmentDuckRiderCrossLaneSmashRuntimeTest] " + message);
		}
	}
}
