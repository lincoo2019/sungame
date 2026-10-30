using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class BugOverviewShootingLevel11WildPeaMowerRuntimeTest : Node
{
	private const string NormalLevelPath = "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11.tres";

	private const string DifficultLevelPath = "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11_D.tres";

	private const string MapPath = "res://Asset/Config/Map/Vampire/VampireMapVampire.tres";

	private const string WildPeaPacketPath = "res://Asset/Anime/Character/Plant/Gold/WildPea/Packet/PlantWildPea.tres";

	private const string WildPeaScenePath = "res://Asset/Anime/Character/Plant/Gold/WildPea/Scene/TowerDefensePlantWildPea.tscn";

	private const string MowerPacketPath = "res://Asset/Anime/Character/Mower/Default/Packet/MowerDefault.tres";

	private const string MowerScenePath = "res://Asset/Anime/Character/Mower/Default/Scene/TowerDefenseMowerDefault.tscn";

	private static readonly Vector2I WildPeaGrid = new Vector2I(5, 3);

	private int _checks;

	private int _failures;

	private ShootingLevel11WildPeaMowerControlStub _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseBattleFeatureMower _mowerFeature;

	private TowerDefenseMapControl _mapControl;

	private TowerDefensePlantWildPea _wildPea;

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private Dictionary<string, TowerDefensePacketConfig> _packetCache;

	private TowerDefensePacketConfig _previousMowerPacketCache;

	private bool _hadMowerPacketCache;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				TowerDefenseLevelConfig towerDefenseLevelConfig = Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11.tres");
				TowerDefenseLevelConfig towerDefenseLevelConfig2 = Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11_D.tres");
				Check(GodotObject.IsInstanceValid(towerDefenseLevelConfig) && towerDefenseLevelConfig.ResourcePath == "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11.tres" && towerDefenseLevelConfig.levelName == "野火燎原" && GodotObject.IsInstanceValid(towerDefenseLevelConfig2) && towerDefenseLevelConfig2.ResourcePath == "res://Asset/Config/Level/TowerDefense/Shooting/Shooting_Level1_11_D.tres" && towerDefenseLevelConfig2.levelName == "野火燎原", "Both fixtures must be the production Shooting Level 1-11 resources.");
				Check(HasMowerContract(towerDefenseLevelConfig), "Normal Shooting Level 1-11 must enable MowerUse and retain the Mower feature.");
				Check(HasMowerContract(towerDefenseLevelConfig2), "Difficult Shooting Level 1-11 must enable MowerUse and retain the Mower feature.");
				TowerDefenseLevelPacketConfig normalUpgrade = FindWildPeaFireUpgrade(towerDefenseLevelConfig);
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindWildPeaFireUpgrade(towerDefenseLevelConfig2);
				Check(GodotObject.IsInstanceValid(normalUpgrade), "The normal level's recursive WildPea chain must resolve the FirePea upgrade.");
				Check(GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig), "The difficult level's recursive WildPea chain must resolve the FirePea upgrade.");
				Check(HasPropertyValue(normalUpgrade?.@override?.characterOverride, "projectileName", (Variant variant) => variant.AsString() == "FirePea") && HasPropertyValue(towerDefenseLevelPacketConfig?.@override?.characterOverride, "projectileName", (Variant variant) => variant.AsString() == "FirePea"), "Both authored variants must upgrade WildPea to the same FirePea projectile.");
				TowerDefenseMapConfig towerDefenseMapConfig = Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Vampire/VampireMapVampire.tres");
				Check(GodotObject.IsInstanceValid(towerDefenseMapConfig) && towerDefenseMapConfig.ResourcePath == "res://Asset/Config/Map/Vampire/VampireMapVampire.tres" && towerDefenseMapConfig.gridNum == new Vector2I(9, 5), "The fixture must use the real 9x5 VampireMapVampire resource.");
				Check(towerDefenseMapConfig != null && towerDefenseMapConfig.lineUse?.Count == 5 && towerDefenseMapConfig.lineUse.Contains(1) && towerDefenseMapConfig.lineUse.Contains(2) && towerDefenseMapConfig.lineUse.Contains(3) && towerDefenseMapConfig.lineUse.Contains(4) && towerDefenseMapConfig.lineUse.Contains(5), "The production Vampire map must expose all five playable lanes.");
				RegisterRealFixtures();
				SetupBattleFixture(manager, towerDefenseMapConfig);
				Check(AllRealCellsExist(), "PlantGridInit must create every production Vampire map cell.");
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantWildPea");
				Check(GodotObject.IsInstanceValid(packetConfig) && packetConfig.characterConfig?.name == "PlantWildPea", "The production registry must resolve the real PlantWildPea packet.");
				_wildPea = packetConfig?.Plant(WildPeaGrid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlantWildPea;
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(_wildPea) && _wildPea.IsNodeReady() && _wildPea.config?.name == "PlantWildPea", "The real PlantWildPea scene must enter the live character tree and finish Ready.");
				Check(_wildPea?.packet?.saveKey == "PlantWildPea" && _wildPea.cell == TowerDefenseManager.GetMapCell(WildPeaGrid), "The live WildPea must retain its real packet and Vampire map cell ownership.");
				normalUpgrade?.@override?.characterOverride?.ExecuteCharacter(_wildPea);
				Check(_wildPea?.projectileName == "FirePea", "The real level upgrade must mutate the live WildPea facade to FirePea.");
				FireComponent fireComponent = _wildPea?.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fireComponent != null && !fireComponent.IsReleased && fireComponent.Alive, "The upgraded WildPea must use its real resource-backed character.fire runtime.");
				Check(fireComponent != null && fireComponent.fireCheckList?.Count > 0 && fireComponent.fireCheckList[0].projectile?.GetProjectile()?.projectileName.ToString() == "FirePea", "The live FireComponent must build FirePea after the authored upgrade is applied.");
				TowerDefensePacketConfig packetConfig2 = TowerDefenseManager.GetPacketConfig("MowerDefault");
				PackedScene packedScene = Load<PackedScene>("res://Asset/Anime/Character/Mower/Default/Scene/TowerDefenseMowerDefault.tscn");
				Check(GodotObject.IsInstanceValid(packetConfig2) && packetConfig2.characterConfig?.name == "MowerDefault" && GodotObject.IsInstanceValid(packedScene) && packedScene.CanInstantiate(), "The registry must resolve the real MowerDefault packet and production scene.");
				Check(_control.GetFeature(new StringName("Map")) == _mapFeature && _control.GetFeature(new StringName("Mower")) == _mowerFeature, "The runtime fixture must expose the production Map/Mower feature dependency graph.");
				_mowerFeature.MowerInit();
				await WaitFrames(5);
				Check(CountLiveMowers() == 5, "Production MowerInit must create exactly one mower on each of five active lanes.");
				Check(AllMowersMatchProductionContract(), "Every generated mower must be a real MowerDefault bound to its exact lane, cell, packet, and authored spawn position.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewShootingLevel11WildPeaMowerRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(_wildPea) && !_wildPea.IsQueuedForDeletion())
			{
				_wildPea.QueueFree();
			}
			if (_mowerFeature != null)
			{
				foreach (TowerDefenseMower item in _mowerFeature.mowerLine)
				{
					if (GodotObject.IsInstanceValid(item) && !item.IsQueuedForDeletion())
					{
						item.QueueFree();
					}
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			ObjectManager.Instance?.Clear();
			ResourceManager.Instance?.ReleaseTransientResources();
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(8);
		}
		bool flag = _failures == 0 && _checks == 20;
		GD.Print($"SHOOTING_LEVEL11_WILDPEA_MOWER_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void SetupBattleFixture(TowerDefenseManager manager, TowerDefenseMapConfig map)
	{
		_control = new ShootingLevel11WildPeaMowerControlStub
		{
			Name = "ShootingLevel11WildPeaMowerControl",
			isGameRunning = false,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		manager.currentControl = _control;
		manager.gridBeginPos = map.gridBeginPos;
		manager.gridSize = map.gridSize;
		manager.gridNum = map.gridNum;
		_mapControl = new TowerDefenseMapControl
		{
			Name = "DetachedVampireMapControl"
		};
		_mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			mapControl = _mapControl,
			config = map,
			mapConfig = map
		};
		_mapControl.mapFeature = _mapFeature;
		_mapFeature.PlantGridInit();
		_mowerFeature = new TowerDefenseBattleFeatureMower
		{
			control = _control,
			config = new TowerDefenseBattleFeatureMowerConfig
			{
				mowerPacketName = "MowerDefault"
			}
		};
		_mowerFeature.mowerLine.Resize(51);
		_mowerFeature.targetZombieLine.Resize(51);
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
		_control.featureDictionary[new StringName("Mower")] = _mowerFeature;
	}

	private static bool HasMowerContract(TowerDefenseLevelConfig level)
	{
		if (GodotObject.IsInstanceValid(level) && level.processData.GetValueOrDefault("MowerUse", false).AsBool())
		{
			return level.featureData.ContainsKey(new StringName("Mower"));
		}
		return false;
	}

	private static TowerDefenseLevelPacketConfig FindWildPeaFireUpgrade(TowerDefenseLevelConfig level)
	{
		if (!GodotObject.IsInstanceValid(level))
		{
			return null;
		}
		HashSet<ulong> visited = new HashSet<ulong>();
		foreach (Variant packetBank in level.packetBankList)
		{
			TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindWildPeaFireUpgrade(packetBank.AsGodotObject() as TowerDefenseLevelPacketConfig, visited);
			if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
			{
				return towerDefenseLevelPacketConfig;
			}
		}
		return null;
	}

	private static TowerDefenseLevelPacketConfig FindWildPeaFireUpgrade(TowerDefenseLevelPacketConfig packet, HashSet<ulong> visited)
	{
		if (!GodotObject.IsInstanceValid(packet) || !visited.Add(packet.GetInstanceId()))
		{
			return null;
		}
		if (packet.packetName == "PlantWildPea" && HasPropertyValue(packet.@override?.characterOverride, "projectileName", (Variant value) => value.AsString() == "FirePea"))
		{
			return packet;
		}
		if (!GodotObject.IsInstanceValid(packet.@override))
		{
			return null;
		}
		foreach (CardActionBehaviorDefinition useSucceededAction in packet.@override.useSucceededActions)
		{
			if (useSucceededAction is CardActionBehaviorChangePacket cardActionBehaviorChangePacket)
			{
				TowerDefenseLevelPacketConfig towerDefenseLevelPacketConfig = FindWildPeaFireUpgrade(cardActionBehaviorChangePacket.levelPacketConfig, visited);
				if (GodotObject.IsInstanceValid(towerDefenseLevelPacketConfig))
				{
					return towerDefenseLevelPacketConfig;
				}
			}
		}
		return null;
	}

	private static bool HasPropertyValue(TowerDefenseCharacterOverride characterOverride, string propertyName, Func<Variant, bool> predicate)
	{
		if (!GodotObject.IsInstanceValid(characterOverride))
		{
			return false;
		}
		foreach (TowerDefenseCharacterPropertyChangeConfig item in characterOverride.propertyChange)
		{
			if (GodotObject.IsInstanceValid(item) && item.propertyName == propertyName)
			{
				return predicate(item.value);
			}
		}
		return false;
	}

	private bool AllRealCellsExist()
	{
		for (int i = 1; i <= 9; i++)
		{
			for (int j = 1; j <= 5; j++)
			{
				if (!GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(new Vector2I(i, j))))
				{
					return false;
				}
			}
		}
		return true;
	}

	private int CountLiveMowers()
	{
		int num = 0;
		for (int i = 1; i <= 5; i++)
		{
			if (GodotObject.IsInstanceValid(_mowerFeature.mowerLine[i]))
			{
				num++;
			}
		}
		return num;
	}

	private bool AllMowersMatchProductionContract()
	{
		for (int i = 1; i <= 5; i++)
		{
			TowerDefenseMower towerDefenseMower = _mowerFeature.mowerLine[i];
			TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(new Vector2I(1, i));
			Vector2 to = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(0, i)) + new Vector2(10f, 0f);
			if (!(towerDefenseMower is TowerDefenseMowerDefault) || !GodotObject.IsInstanceValid(mapCell) || towerDefenseMower.gridPos != new Vector2I(0, i) || towerDefenseMower.packet?.saveKey != "MowerDefault" || towerDefenseMower.config?.name != "MowerDefault" || !towerDefenseMower.characterFilter || towerDefenseMower.GlobalPosition.DistanceTo(to) > 0.01f || Math.Abs(towerDefenseMower.groundHeight - mapCell.GetGroundHeight(0.0)) > 0.001)
			{
				return false;
			}
		}
		return true;
	}

	private void RegisterRealFixtures()
	{
		SnapshotPacketCache();
		RegisterPacket("PlantWildPea", "res://Asset/Anime/Character/Plant/Gold/WildPea/Packet/PlantWildPea.tres");
		RegisterCharacter("PlantWildPea", "res://Asset/Anime/Character/Plant/Gold/WildPea/Scene/TowerDefensePlantWildPea.tscn");
		RegisterPacket("MowerDefault", "res://Asset/Anime/Character/Mower/Default/Packet/MowerDefault.tres");
		RegisterCharacter("MowerDefault", "res://Asset/Anime/Character/Mower/Default/Scene/TowerDefenseMowerDefault.tscn");
	}

	private void SnapshotPacketCache()
	{
		_packetCache = typeof(TowerDefenseManager).GetField("_packetConfigRefCache", BindingFlags.Static | BindingFlags.NonPublic)?.GetValue(null) as Dictionary<string, TowerDefensePacketConfig>;
		if (_packetCache != null)
		{
			_hadMowerPacketCache = _packetCache.TryGetValue("MowerDefault", out _previousMowerPacketCache);
			_packetCache.Remove("MowerDefault");
		}
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
		instance.TOWERDEFENSE_PACKETS[key] = Load<TowerDefensePacketConfig>(path);
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
		instance.TOWERDEFENSE_CHARCATERS[key] = Load<PackedScene>(path);
	}

	private void RestoreRealFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
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
		if (_packetCache != null)
		{
			if (_hadMowerPacketCache)
			{
				_packetCache["MowerDefault"] = _previousMowerPacketCache;
			}
			else
			{
				_packetCache.Remove("MowerDefault");
			}
		}
	}

	private static T Load<T>(string path) where T : Resource
	{
		return ResourceLoader.Load<T>(path, null, ResourceLoader.CacheMode.Ignore);
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
			GD.PushError("[BugOverviewShootingLevel11WildPeaMowerRuntimeTest] " + message);
		}
	}
}
