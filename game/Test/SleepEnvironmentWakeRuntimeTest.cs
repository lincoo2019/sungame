using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class SleepEnvironmentWakeRuntimeTest : Node
{
	private const string FumePacketPath = "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres";

	private const string FumeScenePath = "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Scene/TowerDefensePlantFumeShroom.tscn";

	private const string CoffeeleafPacketPath = "res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Packet/PlantCoffeeleaf.tres";

	private const string CoffeeleafScenePath = "res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Scene/TowerDefensePlantCoffeeleaf.tscn";

	private const string GloomSquashPacketPath = "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres";

	private const string GloomSquashScenePath = "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn";

	private const string DayMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres";

	private const string NightMapConfigPath = "res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnNight.tres";

	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private static readonly Vector2I CoffeeGrid = new Vector2I(2, 2);

	private static readonly Vector2I DayNightGrid = new Vector2I(4, 2);

	private static readonly Vector2I UpgradeGrid = new Vector2I(3, 3);

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
		SleepEnvironmentWakeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 14;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("Required autoloads are unavailable.");
				}
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				ResourceManager.Instance.RequireFullGameplayResourcesReady("SleepEnvironmentWakeRuntimeTest");
				RegisterRealFixtures();
				TowerDefenseMapConfig dayMapConfig = LoadMapConfig("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres");
				TowerDefenseMapConfig nightMapConfig = LoadMapConfig("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawnNight.tres");
				if (!GodotObject.IsInstanceValid(dayMapConfig) || !GodotObject.IsInstanceValid(nightMapConfig))
				{
					throw new InvalidOperationException("Production day and night map configs are unavailable.");
				}
				control = new SleepEnvironmentWakeControlStub
				{
					Name = "SleepEnvironmentWakeControl",
					isGameRunning = true,
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
				manager.gridBeginPos = dayMapConfig.gridBeginPos;
				manager.gridSize = dayMapConfig.gridSize;
				manager.gridNum = dayMapConfig.gridNum;
				mapControl = Instantiate<TowerDefenseMapControl>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn");
				AddChild(mapControl, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(2);
				mapFeature = CreateMapFeature(mapControl, dayMapConfig);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				TowerDefensePlantFumeShroom coffeeFume = SpawnFume(CoffeeGrid);
				await WaitFrames(8);
				SleepComponent coffeeSleep = coffeeFume?.componentManager?.GetRuntime<SleepComponent>("character.sleep");
				Check(GodotObject.IsInstanceValid(coffeeFume) && coffeeSleep != null && !coffeeSleep.IsReleased && coffeeFume.IsSleep(), "The production Fume-shroom must expose its real SleepComponent and start asleep during daytime.");
				coffeeFume.SetMainStateMachineDispatchEnabled(enabled: false);
				control.isGameRunning = false;
				TowerDefensePlantCoffeeleaf coffeeleaf = SpawnCoffeeleaf(CoffeeGrid);
				await WaitFrames(8);
				Check(GodotObject.IsInstanceValid(coffeeleaf), "The production Coffeeleaf must be planted over the sleeping mushroom.");
				Check(GodotObject.IsInstanceValid(coffeeleaf) && coffeeFume.cell.HasCoffee((int)coffeeFume.camp), "The production Coffeeleaf must provide same-camp coffee protection in the shared cell.");
				Check(coffeeSleep != null && !coffeeSleep.CanSleep(), "Coffee protection must make the sleeping Fume-shroom ineligible for sleep.");
				Check(!coffeeFume.IsSleep() && coffeeFume.componentAlive, "Coffee protection must automatically wake the sleeping Fume-shroom.");
				TowerDefensePlantFumeShroom upgradeFume = SpawnFume(UpgradeGrid);
				await WaitFrames(8);
				SleepComponent sleepComponent = upgradeFume?.componentManager?.GetRuntime<SleepComponent>("character.sleep");
				Check(GodotObject.IsInstanceValid(upgradeFume) && sleepComponent != null && !sleepComponent.IsReleased && upgradeFume.IsSleep(), "The upgrade target must be a real daytime Fume-shroom that starts asleep.");
				upgradeFume.WakeUp();
				sleepComponent.ReevaluateEnvironmentState();
				await WaitFrames(2);
				Check(upgradeFume.instance.wakeUp && !upgradeFume.IsSleep() && upgradeFume.componentAlive, "The target Fume-shroom must be awake through the production wake-up authority before upgrading.");
				TowerDefensePlantGloomSquash gloomSquash = LoadPacket("res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres")?.PlantOnPlant(upgradeFume, playAudio: false) as TowerDefensePlantGloomSquash;
				await WaitFrames(10);
				Check(GodotObject.IsInstanceValid(gloomSquash) && gloomSquash.config?.name == "PlantGloomSquash" && gloomSquash.targetPlant == upgradeFume, "The scenario must use the real GloomSquash upgrade attached to the awakened Fume-shroom.");
				Check(GodotObject.IsInstanceValid(gloomSquash) && gloomSquash.instance.wakeUp, "A mushroom upgrade must inherit the awakened state of its target plant.");
				Check(GodotObject.IsInstanceValid(gloomSquash) && !gloomSquash.IsSleep() && gloomSquash.componentAlive, "The upgraded GloomSquash must remain awake during daytime without another Coffee Bean.");
				control.isGameRunning = true;
				TowerDefensePlantFumeShroom dayNightFume = SpawnFume(DayNightGrid);
				await WaitFrames(8);
				SleepComponent dayNightSleep = dayNightFume?.componentManager?.GetRuntime<SleepComponent>("character.sleep");
				Check(GodotObject.IsInstanceValid(dayNightFume) && dayNightSleep != null && !dayNightSleep.IsReleased && dayNightFume.IsSleep(), "The second production Fume-shroom must start asleep during daytime.");
				await CheckSleepAnimation(dayNightSleep, "A sleeping mushroom's indicator must keep advancing during environment checks.");
				dayNightFume.SetSpriteGroupShaderParameter("hologram", true);
				dayNightFume.instance.hologram = true;
				dayNightFume.instance.canBeCollection = false;
				dayNightFume.cell.ReleaseHologramGridOccupancy(dayNightFume);
				await CheckSleepAnimation(dayNightSleep, "Converting a sleeping mushroom to a hologram must preserve its animated sleep indicator.");
				await CheckSleepAnimation(dayNightSleep, "Repeated sleep snapshots must not restart the hologram's indicator.", applySync: true);
				await CheckHypnoSeedWake(new Vector2I(6, 2), hypnoses: false, control);
				await CheckHypnoSeedWake(new Vector2I(7, 2), hypnoses: true, control);
				dayNightFume.SetMainStateMachineDispatchEnabled(enabled: false);
				mapFeature.MapChange(nightMapConfig);
				await WaitForMapChange(mapFeature, control, nightMapConfig, 30);
				Check(mapFeature.config == nightMapConfig && mapFeature.config.isNight && !control.HasPendingBattleOperations, "The production map-change operation must commit the nighttime map before sleep is reevaluated.");
				await WaitFrames(8);
				Check(dayNightSleep != null && !dayNightSleep.CanSleep(), "Nighttime must make a day-sleeping mushroom ineligible for sleep.");
				Check(!dayNightFume.IsSleep() && dayNightFume.componentAlive, "Changing the map from day to night must automatically wake the mushroom.");
				Check(!dayNightSleep.sleepSprite.Visible && dayNightSleep.sleepSprite.pause, "Waking the hologram must still hide and pause its sleep indicator.");
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[SleepEnvironmentWakeRuntimeTest] Unexpected exception: {value}");
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
		bool flag = _failures == 0 && _checks == 25;
		GD.Print($"SLEEP_ENVIRONMENT_WAKE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task CheckSleepAnimation(SleepComponent sleep, string message, bool applySync = false)
	{
		AdobeAnimateSprite indicator = sleep.sleepSprite;
		if (!GodotObject.IsInstanceValid(indicator))
		{
			throw new InvalidOperationException("The sleeping mushroom must have a real sleep indicator.");
		}
		double minimumFrame = indicator.frameIndex;
		double maximumFrame = minimumFrame;
		Dictionary snapshot = new Dictionary { ["sleep"] = true };
		for (int frame = 0; frame < 18; frame++)
		{
			sleep.SleepProcessing(1f / 60f);
			if (applySync)
			{
				sleep.SyncDeserialize(snapshot);
			}
			await WaitFrames(1);
			minimumFrame = Math.Min(minimumFrame, indicator.frameIndex);
			maximumFrame = Math.Max(maximumFrame, indicator.frameIndex);
		}
		Check(indicator.Visible && !indicator.pause && maximumFrame - minimumFrame >= 3.0, message);
	}

	private async Task CheckHypnoSeedWake(Vector2I grid, bool hypnoses, SleepEnvironmentWakeControlStub control)
	{
		TowerDefensePlantHypnoSeed seed = TowerDefenseManager.GetPacketConfig("PlantHypnoSeed")?.Plant(grid, playAudio: false) as TowerDefensePlantHypnoSeed;
		await WaitFrames(4);
		if (!GodotObject.IsInstanceValid(seed))
		{
			throw new InvalidOperationException("The real HypnoSeed must be planted for the daytime maturity test.");
		}
		if (hypnoses)
		{
			seed.Hypnoses();
		}
		seed.BatchUpdate(seed.changeTime);
		await WaitFrames(12);
		TowerDefenseCharacter towerDefenseCharacter = null;
		foreach (Node child in control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter2 && towerDefenseCharacter2.gridPos == grid && towerDefenseCharacter2.config?.name == "PlantHypnoShroom")
			{
				towerDefenseCharacter = towerDefenseCharacter2;
			}
		}
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter), "A mature HypnoSeed must produce a real Hypno-shroom.");
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.instance.wakeUp && !towerDefenseCharacter.IsSleep() && towerDefenseCharacter.componentAlive, "A seed-grown Hypno-shroom must automatically wake and stay awake during daytime.");
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.instance.hypnoses == hypnoses, "Automatically waking a seed-grown Hypno-shroom must preserve its inherited camp.");
	}

	private static TowerDefensePlantFumeShroom SpawnFume(Vector2I grid)
	{
		return LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres")?.Plant(grid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlantFumeShroom;
	}

	private static TowerDefensePlantCoffeeleaf SpawnCoffeeleaf(Vector2I grid)
	{
		return LoadPacket("res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Packet/PlantCoffeeleaf.tres")?.Plant(grid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlantCoffeeleaf;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private static TowerDefenseMapConfig LoadMapConfig(string path)
	{
		return ResourceLoader.Load<TowerDefenseMapConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefenseMapConfig;
	}

	private static T Instantiate<T>(string path) where T : Node
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		if (packedScene == null)
		{
			return null;
		}
		return packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseMapConfig dayMapConfig)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = dayMapConfig
		};
		towerDefenseBattleFeatureMap.mapConfig = towerDefenseBattleFeatureMap.config;
		mapControl.mapFeature = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= dayMapConfig.gridNum.X; i++)
		{
			for (int j = 1; j <= dayMapConfig.gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j)).Init(dayMapConfig.GetEffectiveCellConfig(i, j));
			}
		}
		for (int k = 1; k <= dayMapConfig.gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		TowerDefenseMap towerDefenseMap = dayMapConfig.GetMapScene(cache: false)?.Instantiate<TowerDefenseMap>(PackedScene.GenEditState.Disabled);
		mapControl.mapNode.AddChild(towerDefenseMap, forceReadableName: false, InternalMode.Disabled);
		towerDefenseBattleFeatureMap.currentMap = towerDefenseMap;
		return towerDefenseBattleFeatureMap;
	}

	private async Task WaitForMapChange(TowerDefenseBattleFeatureMap feature, SleepEnvironmentWakeControlStub control, TowerDefenseMapConfig targetConfig, int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (feature.config == targetConfig && !control.HasPendingBattleOperations)
			{
				break;
			}
			await WaitFrames(1);
		}
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantFumeShroom", "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Packet/PlantFumeShroom.tres");
		RegisterPacket("PlantCoffeeleaf", "res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Packet/PlantCoffeeleaf.tres");
		RegisterPacket("PlantGloomSquash", "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Packet/PlantGloomSquash.tres");
		RegisterCharacter("PlantFumeShroom", "res://Asset/Anime/Character/Plant/Chapter0/FumeShroom/Scene/TowerDefensePlantFumeShroom.tscn");
		RegisterCharacter("PlantCoffeeleaf", "res://Asset/Anime/Character/Plant/Chapter5/Coffeeleaf/Scene/TowerDefensePlantCoffeeleaf.tscn");
		RegisterCharacter("PlantGloomSquash", "res://Asset/Anime/Character/Plant/Chapter6/GloomSquash/Scene/TowerDefensePlantGloomSquash.tscn");
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
			GD.PushError("[SleepEnvironmentWakeRuntimeTest] " + message);
		}
	}
}
