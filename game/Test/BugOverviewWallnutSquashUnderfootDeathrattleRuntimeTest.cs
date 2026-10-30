using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewWallnutSquashUnderfootDeathrattleRuntimeTest : Node
{
	private const string WallnutSquashPacketPath = "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Packet/PlantWallnutSquash.tres";

	private const string WallnutSquashScenePath = "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Scene/TowerDefensePlantWallnutSquash.tscn";

	private const string WallnutSquashDefinitionPath = "res://Script/Component/TowerDefense/Character/SquashComponent/Definitions/WallnutSquashDefinition.tres";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private static readonly Vector2I TestGrid = new Vector2I(4, 3);

	private int _checks;

	private int _failures;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		WallnutSquashUnderfootRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombie zombie = null;
		TowerDefensePlantWallnutSquash plant = null;
		try
		{
			int num;
			_ = num - 1;
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				SquashComponentDefinition squashComponentDefinition = ResourceLoader.Load<SquashComponentDefinition>("res://Script/Component/TowerDefense/Character/SquashComponent/Definitions/WallnutSquashDefinition.tres", null, ResourceLoader.CacheMode.Ignore);
				Check(GodotObject.IsInstanceValid(squashComponentDefinition), "The real Wallnut Squash component definition must load.");
				Check(squashComponentDefinition != null && !squashComponentDefinition.InitiallyAlive, "Wallnut Squash must preserve the legacy initially-disabled deathrattle component state.");
				RegisterRealFixtures();
				control = new WallnutSquashUnderfootRuntimeControlStub
				{
					Name = "WallnutSquashUnderfootRuntimeControl",
					isGameRunning = true,
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
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(TestGrid);
				Check(GodotObject.IsInstanceValid(mapCell), "The underfoot regression must use a real flat map cell.");
				zombie = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(TestGrid, playAudio: false) as TowerDefenseZombie;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(zombie), "The real normal zombie must spawn before Wallnut Squash placement.");
				if (!GodotObject.IsInstanceValid(zombie))
				{
					throw new InvalidOperationException("The real normal zombie did not spawn.");
				}
				zombie.ProcessMode = ProcessModeEnum.Disabled;
				zombie.gridPos = TestGrid;
				zombie.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(TestGrid);
				plant = LoadPacket("res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Packet/PlantWallnutSquash.tres")?.Plant(TestGrid, playAudio: false) as TowerDefensePlantWallnutSquash;
				await WaitFrames(4);
				Check(GodotObject.IsInstanceValid(plant), "The real Wallnut Squash must plant in the zombie's occupied world position.");
				if (!GodotObject.IsInstanceValid(plant))
				{
					throw new InvalidOperationException("The real Wallnut Squash did not spawn.");
				}
				SquashComponent squash = plant.componentManager?.GetRuntime<SquashComponent>();
				Check(squash != null && !squash.IsReleased, "The real Wallnut Squash must expose its Squash runtime.");
				Check(squash != null && !squash.Alive, "Placing Wallnut Squash under a zombie must not arm its deathrattle at birth.");
				await WaitFrames(8);
				Check(squash != null && !squash.IsRunning(), "A co-located zombie must not immediately start the Wallnut Squash deathrattle.");
				Check(GodotObject.IsInstanceValid(plant) && !plant.isDestroy, "Wallnut Squash must remain a living defensive plant after underfoot placement.");
				Check(plant.instance.hitpoints > 300.0, $"Underfoot placement must retain defensive hitpoints; got {plant.instance.hitpoints}.");
				if ((squash?.Alive ?? true) || squash.IsRunning())
				{
					Check(condition: false, "The lethal-damage phase requires an initially dormant Squash component.");
					Check(condition: false, "The component cannot be armed exclusively by lethal damage after starting at birth.");
					Check(condition: false, "The authored 300-hitpoint deathrattle restore cannot be isolated after premature activation.");
					Check(condition: false, "Post-lethal target acquisition cannot be isolated after premature activation.");
				}
				else
				{
					double hitpoints = plant.instance.hitpoints;
					plant.SkipInvincibleHurt(hitpoints + 1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
					Check(plant.instance.hitpoints <= 0.0, "The real keep-alive Wallnut Squash must accept explicit lethal damage.");
					plant.IdleProcessing(0.0);
					Check(squash.Alive, "Only lethal damage should arm the Wallnut Squash deathrattle component.");
					Check(Mathf.IsEqualApprox((float)plant.instance.hitpoints, 300f), $"Deathrattle arming must restore the authored 300 hitpoints; got {plant.instance.hitpoints}.");
					await WaitFrames(8);
					Check(squash.IsRunning(), "Once armed by lethal damage, the real component must still target the co-located zombie.");
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewWallnutSquashUnderfootDeathrattleRuntimeTest] Unexpected exception: {value}");
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
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 16;
		GD.Print($"WALLNUT_SQUASH_UNDERFOOT_DEATHRATTLE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			isNight = true,
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
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantWallnutSquash", "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Packet/PlantWallnutSquash.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantWallnutSquash", "res://Asset/Anime/Character/Plant/Chapter6/WallnutSquash/Scene/TowerDefensePlantWallnutSquash.tscn");
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
			GD.PushError("[BugOverviewWallnutSquashUnderfootDeathrattleRuntimeTest] " + message);
		}
	}
}
