using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest : Node
{
	private const string NormalLevelPath = "res://Asset/Config/Level/TowerDefense/Chapter1/Level1_5.tres";

	private const string DifficultLevelPath = "res://Asset/Config/Level/TowerDefense/Chapter1/Level1_5_D.tres";

	private const string FrontlawnMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private const string SunflowerPeaPacketPath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres";

	private const string SunflowerPeaScenePath = "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn";

	private const string DefaultShovelPath = "res://Asset/Config/Shovel/Config/ShovelDefault.tres";

	private const string TutorialKey = "Level1_5TutorialTalk";

	private const string RuntimeUser = "BugOverviewLevel15ReplayRuntime";

	private const int ExpectedChecks = 74;

	private static readonly HashSet<Vector2I> ExpectedGridPositions = new HashSet<Vector2I>
	{
		new Vector2I(6, 2),
		new Vector2I(8, 3),
		new Vector2I(7, 4)
	};

	private int _checks;

	private int _failures;

	private Resource _previousMap;

	private Resource _previousPacket;

	private Resource _previousCharacter;

	private Resource _previousShovel;

	private bool _mapWasMissing;

	private bool _packetWasMissing;

	private bool _characterWasMissing;

	private bool _shovelWasMissing;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		ResourceManager resources = ResourceManager.Instance;
		GameSaveManager saveManager = GameSaveManager.Instance;
		GlobalFeatureManager instance = GlobalFeatureManager.Instance;
		Global global = Global.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		string previousUser = saveManager?.GetUserCurrent() ?? "";
		bool previousMultiplayerMode = Global.IsMultiplayerMode;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(resources), "ResourceManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(saveManager), "GameSaveManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(instance), "GlobalFeatureManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(resources) || !GodotObject.IsInstanceValid(saveManager) || !GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(global))
				{
					throw new InvalidOperationException("Required gameplay autoloads are unavailable.");
				}
				global.isMultiplayerMode = false;
				saveManager.SetUserCurrent("BugOverviewLevel15ReplayRuntime");
				RegisterRealResources(resources);
				await VerifyLevelVariant("Normal", "res://Asset/Config/Level/TowerDefense/Chapter1/Level1_5.tres");
				await VerifyLevelVariant("Difficult", "res://Asset/Config/Level/TowerDefense/Chapter1/Level1_5_D.tres");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			try
			{
				if (GodotObject.IsInstanceValid(manager))
				{
					manager.currentControl = previousControl;
				}
				RestoreRealResources(resources);
				if (GodotObject.IsInstanceValid(global))
				{
					global.isMultiplayerMode = previousMultiplayerMode;
				}
				if (GodotObject.IsInstanceValid(saveManager) && !string.IsNullOrEmpty(previousUser) && saveManager.HasUser(previousUser))
				{
					if (saveManager.HasUser("BugOverviewLevel15ReplayRuntime"))
					{
						saveManager.DeleteUser("BugOverviewLevel15ReplayRuntime");
					}
					if (saveManager.HasUser(previousUser))
					{
						saveManager.SetUserCurrent(previousUser);
					}
				}
			}
			catch (Exception value2)
			{
				_failures++;
				GD.PushError($"[BugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest] Cleanup failed: {value2}");
			}
			await WaitFrames(3);
		}
		bool flag = _failures == 0 && _checks == 74;
		GD.Print($"BUG_OVERVIEW_LEVEL15_REPLAY_PRESPAWN_CLEANUP_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyLevelVariant(string variant, string levelPath)
	{
		TowerDefenseLevelConfig level = ResourceLoader.Load<TowerDefenseLevelConfig>(levelPath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(level), variant + " must load the real Level1_5 resource.");
		if (GodotObject.IsInstanceValid(level))
		{
			Check(level.name == "Level1_5" && level.map == "Frontlawn", $"{variant} must retain the authored Level1_5 Frontlawn identity; name={level.name}, map={level.map}.");
			Check(MatchesAuthoredPreSpawn(level.preSpawnList), variant + " must retain the three authored PlantSunflowerPea positions.");
			Check(level.eventInit.Count == 0, $"{variant} EventInit must not clear characters before PreSpawn; count={level.eventInit.Count}.");
			Check(level.eventEntry.Count == 1, $"{variant} EventEntry must contain the tutorial condition; count={level.eventEntry.Count}.");
			Check(level.eventReady.Count == 1 && GetOnlyMapFunction(level.eventReady) == "UseStripe", $"{variant} EventReady must retain only UseStripe; count={level.eventReady.Count}.");
			TowerDefenseLevelEventConditionNpcTalkFinish towerDefenseLevelEventConditionNpcTalkFinish = FindTutorialCondition(level.eventEntry);
			Check(GodotObject.IsInstanceValid(towerDefenseLevelEventConditionNpcTalkFinish) && towerDefenseLevelEventConditionNpcTalkFinish.npcTalkKey == "Level1_5TutorialTalk", variant + " EventEntry must own the real Level1_5TutorialTalk condition.");
			Check(GodotObject.IsInstanceValid(towerDefenseLevelEventConditionNpcTalkFinish) && GetOnlyMapFunction(towerDefenseLevelEventConditionNpcTalkFinish.finishEventList) == "CharacterClear" && GetOnlyMapFunction(towerDefenseLevelEventConditionNpcTalkFinish.unfinishEventList) == "ShowShovel", variant + " must clear replay pre-spawns but show the shovel on the first tutorial run.");
			level.ExportToFeatureProcess();
			await VerifyLifecycleScenario(variant, level, tutorialFinished: false);
			await VerifyLifecycleScenario(variant, level, tutorialFinished: true);
		}
	}

	private async Task VerifyLifecycleScenario(string variant, TowerDefenseLevelConfig level, bool tutorialFinished)
	{
		string scenario = (tutorialFinished ? "Replay" : "FirstTutorial");
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		GameSaveManager instance = GameSaveManager.Instance;
		BugOverviewLevel15ReplayPreSpawnCleanupRuntimeControlStub control = new BugOverviewLevel15ReplayPreSpawnCleanupRuntimeControlStub
		{
			Name = "Level15" + variant + scenario + "Control",
			isGameRunning = false,
			isInit = true,
			hasProgress = false,
			levelConfig = level
		};
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleFeaturePacketPick packetPickFeature = null;
		TowerDefenseBattleFeatureShovel shovelFeature = null;
		TowerDefenseBattleFeatureEvent eventFeature = null;
		TowerDefenseBattleFeaturePreSpawn preSpawnFeature = null;
		try
		{
			AddChild(control, forceReadableName: false, InternalMode.Disabled);
			control.characterNode = new Node2D
			{
				Name = "CharacterNode"
			};
			control.AddChild(control.characterNode, forceReadableName: false, InternalMode.Disabled);
			control.uITopPropContainer = new HBoxContainer
			{
				Name = "UITopPropContainer"
			};
			control.AddChild(control.uITopPropContainer, forceReadableName: false, InternalMode.Disabled);
			manager.currentControl = control;
			instance.SetTutorialValue("Level1_5TutorialTalk", tutorialFinished);
			if (tutorialFinished)
			{
				GlobalFeatureManager.Instance.Unlock("Shovel", saveImmediately: false);
			}
			else
			{
				GlobalFeatureManager.Instance.Lock("Shovel", saveImmediately: false);
			}
			mapFeature = new TowerDefenseBattleFeatureMap
			{
				control = control
			};
			control.featureDictionary["Map"] = mapFeature;
			mapFeature.Init(GetFeatureData(level, "Map"));
			await WaitFrames(2);
			await mapFeature.GameInit();
			packetPickFeature = new TowerDefenseBattleFeaturePacketPick
			{
				control = control
			};
			control.featureDictionary["PacketPick"] = packetPickFeature;
			packetPickFeature.Init(GetFeatureData(level, "PacketPick"));
			await packetPickFeature.GameInit();
			shovelFeature = new TowerDefenseBattleFeatureShovel
			{
				control = control
			};
			control.featureDictionary["Shovel"] = shovelFeature;
			shovelFeature.Init(GetFeatureData(level, "Shovel"));
			await WaitFrames(2);
			await shovelFeature.GameInit();
			await shovelFeature.GameEntry();
			ShovelManager shovelManager = shovelFeature.shovelManager;
			eventFeature = new TowerDefenseBattleFeatureEvent
			{
				control = control
			};
			control.featureDictionary["Event"] = eventFeature;
			eventFeature.Init(GetFeatureData(level, "Event"));
			preSpawnFeature = new TowerDefenseBattleFeaturePreSpawn
			{
				control = control
			};
			control.featureDictionary["PreSpawn"] = preSpawnFeature;
			preSpawnFeature.Init(GetFeatureData(level, "PreSpawn"));
			Check(mapFeature.currentMap is TowerDefenseMapFrontlawn && (mapFeature.config?.mapScenePath.EndsWith("TowerDefenseMapFrontlawn.tscn", StringComparison.Ordinal) ?? false), variant + "/" + scenario + " must initialize the real Frontlawn map scene.");
			Check(eventFeature.eventInit.Count == 0 && eventFeature.eventEntry.Count == 1 && eventFeature.eventReady.Count == 1, variant + "/" + scenario + " runtime Event feature must place the condition in GameEntry.");
			BugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest bugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest = this;
			TowerDefenseBattleFeaturePreSpawnConfig config = preSpawnFeature.config;
			bugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest.Check(config != null && config.preSpawnList.Count == 3, variant + "/" + scenario + " runtime PreSpawn feature must load all three authored plants.");
			await eventFeature.GameInit();
			Check(CountRegisteredSunflowerPeas() == 0, variant + "/" + scenario + " Event.GameInit must run before and leave PreSpawn empty.");
			await preSpawnFeature.GameEntry();
			await WaitFrames(8);
			List<TowerDefensePlantSunflowerPea> spawnedPlants = GetRegisteredSunflowerPeas();
			Check(spawnedPlants.Count == 3, $"{variant}/{scenario} PreSpawn.GameEntry must create three real plants; got {spawnedPlants.Count}.");
			Check(AllPlantsUseRealScene(spawnedPlants), variant + "/" + scenario + " must instantiate the real TowerDefensePlantSunflowerPea scene objects.");
			Check(MatchesRuntimePositions(spawnedPlants), variant + "/" + scenario + " real plants must occupy the authored Level1_5 cells.");
			await eventFeature.GameEntry();
			bool allReplayPlantsEnteredDestroy = tutorialFinished && spawnedPlants.TrueForAll((TowerDefensePlantSunflowerPea plant) => GodotObject.IsInstanceValid(plant) && plant.isDestroy && plant.die);
			await WaitFrames(3);
			if (tutorialFinished)
			{
				Check(CountRegisteredSunflowerPeas() == 0, variant + "/Replay must clear the three tutorial plants after PreSpawn.");
				Check(allReplayPlantsEnteredDestroy, variant + "/Replay CharacterClear must enter the real destroy lifecycle for every plant.");
				Check(GodotObject.IsInstanceValid(shovelManager?.shovelButton) && shovelManager.shovelButton.Visible, variant + "/Replay must retain the previously unlocked real shovel button.");
			}
			else
			{
				Check(CountRegisteredSunflowerPeas() == 3, variant + "/FirstTutorial must preserve all three tutorial plants.");
				Check(GodotObject.IsInstanceValid(shovelManager?.shovelButton) && shovelManager.shovelShow && shovelManager.shovelButton.Visible && GlobalFeatureManager.Instance.IsUnlocked("Shovel"), variant + "/FirstTutorial must show the real shovel button before NpcTalk.");
			}
			await eventFeature.GameReady();
			Check(GodotObject.IsInstanceValid(mapFeature.currentMap?.stripe) && mapFeature.currentMap.stripe.Visible && mapFeature.stripeRow == 3, variant + "/" + scenario + " Event.GameReady must still execute the authored UseStripe(3).");
			await eventFeature.GameStart();
			await preSpawnFeature.GameStart();
			await shovelFeature.GameStart();
			await WaitFrames(5);
			int num = ((!tutorialFinished) ? 3 : 0);
			Check(CountRegisteredSunflowerPeas() == num, $"{variant}/{scenario} must retain {num} plants after GameStart; got {CountRegisteredSunflowerPeas()}.");
			Check(preSpawnFeature.preSpawnList.Count == 0, variant + "/" + scenario + " PreSpawn.GameStart must release its temporary ownership list.");
			Check(GodotObject.IsInstanceValid(shovelManager?.shovelButton) && shovelManager.shovelButton.Visible, variant + "/" + scenario + " the real shovel button must remain visible after GameStart.");
		}
		finally
		{
			shovelFeature?.Destroy();
			packetPickFeature?.Destroy();
			eventFeature?.Destroy();
			preSpawnFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapFeature))
			{
				mapFeature.Destroy();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			manager.currentControl = null;
			await WaitFrames(5);
		}
	}

	private static Dictionary GetFeatureData(TowerDefenseLevelConfig level, string featureName)
	{
		if (!level.featureData.TryGetValue(new StringName(featureName), out var value))
		{
			return new Dictionary();
		}
		return value.Duplicate(deep: true);
	}

	private static TowerDefenseLevelEventConditionNpcTalkFinish FindTutorialCondition(Array<TowerDefenseLevelEventBase> events)
	{
		foreach (TowerDefenseLevelEventBase @event in events)
		{
			if (@event is TowerDefenseLevelEventConditionNpcTalkFinish result)
			{
				return result;
			}
		}
		return null;
	}

	private static string GetOnlyMapFunction(Array<TowerDefenseLevelEventBase> events)
	{
		if (events.Count != 1 || !(events[0] is TowerDefenseLevelEventCurrentMapFunctionExecute towerDefenseLevelEventCurrentMapFunctionExecute))
		{
			return "";
		}
		return towerDefenseLevelEventCurrentMapFunctionExecute.functionName;
	}

	private static bool MatchesAuthoredPreSpawn(Array<TowerDefenseLevelPreSpawnConfig> configs)
	{
		if (configs.Count != ExpectedGridPositions.Count)
		{
			return false;
		}
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		foreach (TowerDefenseLevelPreSpawnConfig config in configs)
		{
			if (!GodotObject.IsInstanceValid(config) || config.packetName != "PlantSunflowerPea")
			{
				return false;
			}
			hashSet.Add(config.gridPos);
		}
		return hashSet.SetEquals(ExpectedGridPositions);
	}

	private static bool AllPlantsUseRealScene(List<TowerDefensePlantSunflowerPea> plants)
	{
		if (plants.Count != ExpectedGridPositions.Count)
		{
			return false;
		}
		foreach (TowerDefensePlantSunflowerPea plant in plants)
		{
			if (!GodotObject.IsInstanceValid(plant) || plant.config?.name != "PlantSunflowerPea" || plant.SceneFilePath != "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn")
			{
				return false;
			}
		}
		return true;
	}

	private static bool MatchesRuntimePositions(List<TowerDefensePlantSunflowerPea> plants)
	{
		HashSet<Vector2I> hashSet = new HashSet<Vector2I>();
		foreach (TowerDefensePlantSunflowerPea plant in plants)
		{
			hashSet.Add(plant.gridPos);
		}
		return hashSet.SetEquals(ExpectedGridPositions);
	}

	private static List<TowerDefensePlantSunflowerPea> GetRegisteredSunflowerPeas()
	{
		List<TowerDefensePlantSunflowerPea> list = new List<TowerDefensePlantSunflowerPea>();
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			return list;
		}
		foreach (Variant item in TowerDefenseManager.Instance.GetCharacter())
		{
			if (item.AsGodotObject() is TowerDefensePlantSunflowerPea towerDefensePlantSunflowerPea && GodotObject.IsInstanceValid(towerDefensePlantSunflowerPea) && !towerDefensePlantSunflowerPea.isDestroy)
			{
				list.Add(towerDefensePlantSunflowerPea);
			}
		}
		return list;
	}

	private static int CountRegisteredSunflowerPeas()
	{
		return GetRegisteredSunflowerPeas().Count;
	}

	private void RegisterRealResources(ResourceManager resources)
	{
		RegisterResource(resources.MAPS, "Frontlawn", "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", out _previousMap, out _mapWasMissing);
		RegisterResource(resources.TOWERDEFENSE_PACKETS, "PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Packet/PlantSunflowerPea.tres", out _previousPacket, out _packetWasMissing);
		RegisterResource(resources.TOWERDEFENSE_CHARCATERS, "PlantSunflowerPea", "res://Asset/Anime/Character/Plant/Chapter1/SunflowerPea/Scene/TowerDefensePlantSunflowerPea.tscn", out _previousCharacter, out _characterWasMissing);
		RegisterResource(resources.SHOVELS, "ShovelDefault", "res://Asset/Config/Shovel/Config/ShovelDefault.tres", out _previousShovel, out _shovelWasMissing);
	}

	private static void RegisterResource(System.Collections.Generic.Dictionary<string, Resource> registry, string key, string path, out Resource previous, out bool wasMissing)
	{
		wasMissing = !registry.TryGetValue(key, out previous);
		Resource resource = ResourceLoader.Load<Resource>(path, null, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(resource))
		{
			throw new InvalidOperationException("Real fixture resource failed to load: " + path);
		}
		registry[key] = resource;
	}

	private void RestoreRealResources(ResourceManager resources)
	{
		if (GodotObject.IsInstanceValid(resources))
		{
			RestoreResource(resources.MAPS, "Frontlawn", _previousMap, _mapWasMissing);
			RestoreResource(resources.TOWERDEFENSE_PACKETS, "PlantSunflowerPea", _previousPacket, _packetWasMissing);
			RestoreResource(resources.TOWERDEFENSE_CHARCATERS, "PlantSunflowerPea", _previousCharacter, _characterWasMissing);
			RestoreResource(resources.SHOVELS, "ShovelDefault", _previousShovel, _shovelWasMissing);
		}
	}

	private static void RestoreResource(System.Collections.Generic.Dictionary<string, Resource> registry, string key, Resource previous, bool wasMissing)
	{
		if (wasMissing)
		{
			registry.Remove(key);
		}
		else if (GodotObject.IsInstanceValid(previous))
		{
			registry[key] = previous;
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
			GD.PushError("[BugOverviewLevel15ReplayPreSpawnCleanupRuntimeTest] " + message);
		}
	}
}
