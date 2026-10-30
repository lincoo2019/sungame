using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSquashZombieCaltropTargetRuntimeTest : Node
{
	private const string SquashPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres";

	private const string SquashScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn";

	private const string CaltropPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres";

	private const string CaltropScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn";

	private static readonly Vector2I CaltropGrid = new Vector2I(4, 2);

	private static readonly Vector2I SquashGrid = new Vector2I(5, 2);

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
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		SquashZombieCaltropTargetControlStub control = null;
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
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				RegisterRealFixtures();
				control = new SquashZombieCaltropTargetControlStub
				{
					Name = "SquashZombieCaltropTargetControl",
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
				TowerDefensePlantCaltrop caltrop = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres")?.Plant(CaltropGrid, playAudio: false) as TowerDefensePlantCaltrop;
				await WaitFrames(4);
				TowerDefenseZombieNormalSquash squash = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres")?.Plant(SquashGrid, playAudio: false) as TowerDefenseZombieNormalSquash;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(caltrop) && caltrop.config?.name == "PlantCaltrop", "The scenario must plant the real Caltrop scene and config.");
				Check(GodotObject.IsInstanceValid(squash) && squash.config?.name == "ZombieNormalSquash", "The scenario must spawn the real Squash zombie scene and config.");
				Check(squash != null && squash.attackComponent2?.IsReleased == false, "The real Squash zombie must expose its secondary jump-target component.");
				if (!GodotObject.IsInstanceValid(caltrop) || !GodotObject.IsInstanceValid(squash) || (squash.attackComponent2?.IsReleased ?? true))
				{
					throw new InvalidOperationException("The production target scenario is incomplete.");
				}
				squash.ProcessMode = ProcessModeEnum.Disabled;
				squash.GlobalPosition = caltrop.GlobalPosition + new Vector2(50f, 0f);
				squash.gridPos = SquashGrid;
				control.isGameRunning = true;
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
				AttackComponent attackComponent = squash.attackComponent2;
				attackComponent.target = null;
				Check(caltrop.instance.height == TowerDefenseEnum.CHARACTER_HEIGHT.LOW, "The real Caltrop must exercise the low spike-plant target case.");
				Check(squash.CanCollision(caltrop.instance.maskFlags) && squash.CanTarget(caltrop), "Caltrop must be an otherwise valid opposing collision target.");
				Check(attackComponent.TryRetargetImmediately(), "Squash zombie must actively acquire a nearby Caltrop with its jump attack.");
				Check(attackComponent.target == caltrop, "Squash zombie jump targeting must select the real Caltrop object.");
				Check(attackComponent.CanAttack() && attackComponent.target == caltrop, "The acquired Caltrop target must remain valid for the committed jump.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewSquashZombieCaltropTargetRuntimeTest] Unexpected exception: {value}");
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
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 10;
		GD.Print($"SQUASH_ZOMBIE_CALTROP_TARGET_RESULT passed={flag} checks={_checks} failures={_failures}");
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
		RegisterPacket("ZombieNormalSquash", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalSquash.tres");
		RegisterPacket("PlantCaltrop", "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Packet/PlantCaltrop.tres");
		RegisterCharacter("ZombieNormalSquash", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Squash/TowerDefenseZombieNormalSquash.tscn");
		RegisterCharacter("PlantCaltrop", "res://Asset/Anime/Character/Plant/Chapter0/Caltrop/Scene/TowerDefensePlantCaltrop.tscn");
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
			GD.PushError("[BugOverviewSquashZombieCaltropTargetRuntimeTest] " + message);
		}
	}
}
