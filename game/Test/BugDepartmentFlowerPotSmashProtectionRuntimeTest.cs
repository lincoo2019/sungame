using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class BugDepartmentFlowerPotSmashProtectionRuntimeTest : Node
{
	private const string MedicPotPacketPath = "res://Asset/Anime/Character/Plant/Other/MedicPot/Packet/PlantMedicPot.tres";

	private const string MedicPotScenePath = "res://Asset/Anime/Character/Plant/Other/MedicPot/Scene/TowerDefensePlantMedicPot.tscn";

	private const string PotatoMinePacketPath = "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres";

	private const string PotatoMineScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn";

	private const string PotPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres";

	private const string PotScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string ZamboniPacketPath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres";

	private const string ZamboniScenePath = "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn";

	private static readonly Vector2I ProtectedGrid = new Vector2I(4, 2);

	private static readonly Vector2I OrdinaryGrid = new Vector2I(4, 3);

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<TowerDefenseCharacter> _spawned = new List<TowerDefenseCharacter>();

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		FlowerPotSmashProtectionControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					return;
				}
				RegisterRealFixtures();
				control = new FlowerPotSmashProtectionControlStub
				{
					Name = "FlowerPotSmashProtectionControl",
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
				TowerDefensePlant medicPot = SpawnPlant("res://Asset/Anime/Character/Plant/Other/MedicPot/Packet/PlantMedicPot.tres", ProtectedGrid) as TowerDefensePlant;
				await WaitFrames(3);
				TowerDefensePlant potatoMine = SpawnPlant("res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", ProtectedGrid) as TowerDefensePlant;
				await WaitFrames(5);
				TowerDefenseZombieZamboni protectedZamboni = SpawnPlant("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres", ProtectedGrid) as TowerDefenseZombieZamboni;
				TowerDefensePlant ordinaryPot = SpawnPlant("res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres", OrdinaryGrid) as TowerDefensePlant;
				await WaitFrames(3);
				TowerDefensePlant sunflower = SpawnPlant("res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", OrdinaryGrid) as TowerDefensePlant;
				await WaitFrames(5);
				TowerDefenseZombieZamboni ordinaryZamboni = SpawnPlant("res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres", OrdinaryGrid) as TowerDefenseZombieZamboni;
				await WaitFrames(6);
				Check(medicPot?.config?.name == "PlantMedicPot" && potatoMine?.config?.name == "PlantPotatoMine", "The protected stack must use the real Medic Pot and Potato Mine scenes.");
				Check(ordinaryPot?.config?.name == "PlantPot" && sunflower?.config?.name == "PlantSunFlower", "The control stack must use the real ordinary Pot and Sunflower scenes.");
				Check(protectedZamboni?.config?.name == "ZombieZamboni" && ordinaryZamboni?.config?.name == "ZombieZamboni", "Both smash paths must use real production Zamboni scenes.");
				if (!AllValid(medicPot, potatoMine, protectedZamboni, ordinaryPot, sunflower, ordinaryZamboni))
				{
					return;
				}
				TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(ProtectedGrid);
				TowerDefenseCellInstance mapCell2 = TowerDefenseManager.GetMapCell(OrdinaryGrid);
				Check(GodotObject.IsInstanceValid(mapCell) && mapCell.GetSlot(medicPot) == potatoMine, "The real Potato Mine must be registered above the real Medic Pot.");
				Check(GodotObject.IsInstanceValid(mapCell2) && mapCell2.GetSlot(ordinaryPot) == sunflower, "The real Sunflower must be registered above the ordinary Pot.");
				PotatoComponent potatoComponent = potatoMine.componentManager?.GetRuntime<PotatoComponent>();
				Check(potatoComponent != null && !potatoComponent.IsReleased && potatoComponent.setSmashInvincibleOnCharge, "The real Potato Mine must retain its production smash-protection runtime.");
				BugDepartmentFlowerPotSmashProtectionRuntimeTest bugDepartmentFlowerPotSmashProtectionRuntimeTest = this;
				AttackComponent attackComponent = protectedZamboni.attackComponent;
				int condition;
				if (attackComponent != null && !attackComponent.IsReleased)
				{
					AttackComponent attackComponent2 = ordinaryZamboni.attackComponent;
					if (attackComponent2 != null && !attackComponent2.IsReleased && protectedZamboni.attackComponent.attackType == "Smash")
					{
						condition = ((ordinaryZamboni.attackComponent.attackType == "Smash") ? 1 : 0);
						goto IL_0750;
					}
				}
				condition = 0;
				goto IL_0750;
				IL_0750:
				bugDepartmentFlowerPotSmashProtectionRuntimeTest.Check((byte)condition != 0, "The real Zamboni scenes must expose their authored Smash attack runtimes.");
				if ((potatoComponent?.IsReleased ?? true) || (protectedZamboni.attackComponent?.IsReleased ?? true) || (ordinaryZamboni.attackComponent?.IsReleased ?? true))
				{
					goto end_IL_00d6;
				}
				foreach (TowerDefenseCharacter item in _spawned)
				{
					item.ProcessMode = ProcessModeEnum.Disabled;
				}
				control.isGameRunning = true;
				potatoComponent.rise = true;
				potatoComponent.ChargeEntered();
				Check(potatoComponent.isCharge && potatoMine.instance.invincibleSmash, "Entering the real Potato charge state must enable smash protection.");
				Check(!medicPot.instance.invincibleSmash && !ordinaryPot.instance.invincibleSmash && !sunflower.instance.invincibleSmash, "Only the upper Potato Mine may own smash protection in this scenario.");
				double hitpoints = medicPot.instance.hitpoints;
				double hitpoints2 = potatoMine.instance.hitpoints;
				protectedZamboni.attackComponent.target = potatoMine;
				protectedZamboni.attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)protectedZamboni.config).smashAttack);
				Check(Math.Abs(medicPot.instance.hitpoints - hitpoints) < 0.001 && !medicPot.die && !medicPot.nearDie, "An upper smash-protected plant must also protect its supporting Medic Pot.");
				Check(Math.Abs(potatoMine.instance.hitpoints - hitpoints2) < 0.001 && !potatoMine.die && !potatoMine.nearDie, "The real charged Potato Mine must remain protected from Zamboni smash.");
				Check(mapCell.GetSlot(medicPot) == potatoMine, "The protected real plant stack must remain intact after Zamboni smash.");
				double hitpoints3 = ordinaryPot.instance.hitpoints;
				double hitpoints4 = sunflower.instance.hitpoints;
				ordinaryZamboni.attackComponent.target = sunflower;
				ordinaryZamboni.attackComponent.SmashAttackCell(((TowerDefenseZombieConfig)ordinaryZamboni.config).smashAttack);
				Check(ordinaryPot.instance.hitpoints < hitpoints3 && ordinaryPot.instance.hitpoints <= 0.0 && ordinaryPot.nearDie, "A real ordinary Pot must still be crushed when its upper plant has no smash protection.");
				Check(sunflower.instance.hitpoints < hitpoints4 && sunflower.instance.hitpoints <= 0.0 && sunflower.nearDie, "A real unprotected Sunflower must still be crushed by the same Zamboni path.");
				Check(((TowerDefenseZombieConfig)protectedZamboni.config).smashAttack == 10000.0 && ((TowerDefenseZombieConfig)ordinaryZamboni.config).smashAttack == 10000.0, "The scenarios must retain the authored Zamboni smash payload.");
				goto end_IL_00b7;
				end_IL_00d6:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugDepartmentFlowerPotSmashProtectionRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00b7;
			}
			return;
			end_IL_00b7:;
		}
		finally
		{
			foreach (TowerDefenseCharacter item2 in _spawned)
			{
				if (GodotObject.IsInstanceValid(item2) && !item2.IsQueuedForDeletion())
				{
					item2.QueueFree();
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
			await WaitFrames(4);
		}
		bool flag = _failures == 0 && _checks == 16;
		GD.Print($"FLOWER_POT_SMASH_PROTECTION_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private TowerDefenseCharacter SpawnPlant(string path, Vector2I grid)
	{
		TowerDefenseCharacter towerDefenseCharacter = LoadPacket(path)?.Plant(grid, playAudio: false);
		if (GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			_spawned.Add(towerDefenseCharacter);
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
		Register("PlantMedicPot", "res://Asset/Anime/Character/Plant/Other/MedicPot/Packet/PlantMedicPot.tres", "res://Asset/Anime/Character/Plant/Other/MedicPot/Scene/TowerDefensePlantMedicPot.tscn");
		Register("PlantPotatoMine", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Packet/PlantPotatoMine.tres", "res://Asset/Anime/Character/Plant/Chapter0/PotatoMine/Scene/TowerDefensePlantPotatoMine.tscn");
		Register("PlantPot", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Packet/PlantPot.tres", "res://Asset/Anime/Character/Plant/Chapter0/Pot/Scene/TowerDefensePlantPot.tscn");
		Register("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
		Register("ZombieZamboni", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Packet/ZombieZamboni.tres", "res://Asset/Anime/Character/Zombie/Chapter2/Zamboni/Scene/Normal/TowerDefenseZombieZamboni.tscn");
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
			GD.PushError("[BugDepartmentFlowerPotSmashProtectionRuntimeTest] " + message);
		}
	}
}
