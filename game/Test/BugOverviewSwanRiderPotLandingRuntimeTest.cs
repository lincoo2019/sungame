using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSwanRiderPotLandingRuntimeTest : Node
{
	private const string PotPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres";

	private const string PotScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn";

	private const string SwanPacketPath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres";

	private const string SwanScenePath = "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn";

	private const string NormalPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

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
		SwanRiderPotLandingRuntimeControlStub control = null;
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
					goto end_IL_0086;
				}
				RegisterRealFixtures();
				control = new SwanRiderPotLandingRuntimeControlStub
				{
					Name = "SwanRiderPotLandingRuntimeControl",
					isGameRunning = false,
					isInit = true,
					levelConfig = new TowerDefenseLevelConfig()
				};
				AddChild(control, forceReadableName: false, InternalMode.Disabled);
				Node2D characterNode = new Node2D
				{
					Name = "CharacterNode"
				};
				control.AddChild(characterNode, forceReadableName: false, InternalMode.Disabled);
				control.characterNode = characterNode;
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
				TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(cell), "The regression must use a real flat map cell.");
				double terrainHeight = cell?.GetGroundHeight() ?? 0.0;
				TowerDefensePlant pot = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres")?.Plant(TestGrid, playAudio: false) as TowerDefensePlant;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(pot) && cell.GetCharacterList().Contains(pot), "A real flower pot must occupy the Swan landing cell.");
				TowerDefenseZombieSwanRider swan = LoadPacket("res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres")?.Plant(TestGrid, playAudio: false) as TowerDefenseZombieSwanRider;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(swan), "The real SwanRider must spawn above the occupied flower-pot cell.");
				if (!GodotObject.IsInstanceValid(swan))
				{
					goto end_IL_0086;
				}
				swan.ProcessMode = ProcessModeEnum.Disabled;
				swan.useCone = false;
				swan.useBucket = false;
				swan.mowerKill = false;
				swan.groundHeight = terrainHeight;
				swan.z = terrainHeight;
				Check(Mathf.IsEqualApprox((float)swan.z, (float)terrainHeight), $"The Swan fixture must begin on the terrain plane; z={swan.z}, terrain={terrainHeight}.");
				swan.DestroySet();
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				TowerDefenseZombieNormal towerDefenseZombieNormal = FindDroppedRider(characterNode);
				Check(GodotObject.IsInstanceValid(towerDefenseZombieNormal), "Destroying a real SwanRider must create its real normal-zombie rider.");
				if (!GodotObject.IsInstanceValid(towerDefenseZombieNormal))
				{
					goto end_IL_0086;
				}
				towerDefenseZombieNormal.ProcessMode = ProcessModeEnum.Disabled;
				Check(towerDefenseZombieNormal.gridPos == TestGrid, $"The dropped rider must retain the Swan grid; got {towerDefenseZombieNormal.gridPos}.");
				Check(Mathf.IsEqualApprox(towerDefenseZombieNormal.GlobalPosition.Y, swan.GlobalPosition.Y), $"The dropped rider must retain the Swan world baseline; riderY={towerDefenseZombieNormal.GlobalPosition.Y}, swanY={swan.GlobalPosition.Y}.");
				Check(Mathf.IsEqualApprox((float)towerDefenseZombieNormal.groundHeight, (float)terrainHeight), $"The dropped rider ground height must remain the cell terrain, not the flower pot; ground={towerDefenseZombieNormal.groundHeight}, terrain={terrainHeight}.");
				Check(Mathf.IsEqualApprox((float)towerDefenseZombieNormal.z, (float)terrainHeight), $"The dropped rider must appear on the terrain plane immediately instead of standing at flower-pot height; z={towerDefenseZombieNormal.z}, terrain={terrainHeight}.");
				Check(GodotObject.IsInstanceValid(pot) && cell.GetCharacterList().Contains(pot), "Dropping the rider must not remove or replace the flower pot.");
				goto end_IL_006f;
				end_IL_0086:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSwanRiderPotLandingRuntimeTest] Unexpected exception: {value}");
				goto end_IL_006f;
			}
			return;
			end_IL_006f:;
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
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0 && _checks == 12;
		GD.Print($"SWAN_RIDER_POT_LANDING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseZombieNormal FindDroppedRider(Node characterNode)
	{
		foreach (Node child in characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieNormal towerDefenseZombieNormal && !(child is TowerDefenseZombieSwanRider) && GodotObject.IsInstanceValid(towerDefenseZombieNormal))
			{
				return towerDefenseZombieNormal;
			}
		}
		return null;
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
		RegisterPacket("PlantPot", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres");
		RegisterPacket("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Packet/ZombieSwanRider.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantPot", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn");
		RegisterCharacter("ZombieSwanRider", "res://Asset/Anime/Character/Zombie/Challenge/SwanRider/Scene/TowerDefenseZombieSwanRider.tscn");
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
			GD.PushError("[BugOverviewSwanRiderPotLandingRuntimeTest] " + message);
		}
	}
}
