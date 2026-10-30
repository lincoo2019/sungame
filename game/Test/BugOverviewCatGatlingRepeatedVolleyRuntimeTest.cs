using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewCatGatlingRepeatedVolleyRuntimeTest : Node
{
	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Scene/TowerDefensePlantCatGatlingPea.tscn";

	private const string PlantPacketPath = "res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Packet/PlantCatGatlingPea.tres";

	private const string TargetScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string TargetPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string StarConfigPath = "res://Asset/Config/Projectile/Star/StarDefault.tres";

	private const int ExpectedRounds = 3;

	private const int ExpectedNormalShotsPerRound = 4;

	private const int ExpectedScatterShotsPerRound = 5;

	private int _spawnedProjectiles;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousProjectiles = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly HashSet<string> _missingProjectiles = new HashSet<string>();

	private readonly List<int> _fireEventsPerRound = new List<int>();

	private readonly List<int> _normalShotsPerRound = new List<int>();

	private int _checks;

	private int _failures;

	private int _roundsCompleted;

	private int _totalFireEvents;

	private int _totalNormalShots;

	private int _previousRoundFireEvents;

	private int _previousRoundNormalShots;

	private TowerDefensePlantCatGatlingPea _plant;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		BugOverviewCatGatlingRepeatedVolleyControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		BulletField bulletField = null;
		try
		{
			_ = 4;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager))
				{
					goto end_IL_00fe;
				}
				ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				bulletField = BulletField.EnsureMountedOnCharacterNode();
				Check(GodotObject.IsInstanceValid(bulletField), "The BulletField must mount for the repeated-volley probe.");
				if (!GodotObject.IsInstanceValid(bulletField))
				{
					goto end_IL_00fe;
				}
				bulletField.ClearActiveBullets();
				bulletField.OnBulletSpawned += OnBulletSpawned;
				TowerDefenseProjectileRegistry.Init();
				RegisterRealFixtures();
				control = new BugOverviewCatGatlingRepeatedVolleyControlStub
				{
					Name = "CatGatlingRepeatedVolleyControl",
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
				manager.gridBeginPos = new Vector2(0f, 100f);
				manager.gridSize = new Vector2(100f, 76f);
				manager.gridNum = new Vector2I(9, 5);
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapFeature = CreateMapFeature(mapControl, manager.gridNum);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				_plant = LoadPacket("res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Packet/PlantCatGatlingPea.tres")?.Plant(new Vector2I(2, 3), playAudio: false) as TowerDefensePlantCatGatlingPea;
				await WaitPhysicsFrames(4);
				TowerDefenseCharacter target = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(new Vector2I(7, 3), playAudio: false);
				await WaitPhysicsFrames(5);
				Check(GodotObject.IsInstanceValid(_plant) && _plant.config?.name == "PlantCatGatlingPea", "The reported real Cat Star Gatling scene must instantiate.");
				Check(GodotObject.IsInstanceValid(target) && target.config?.name == "ZombieNormal", "The fixture must instantiate a real normal-zombie target.");
				if (!GodotObject.IsInstanceValid(_plant) || !GodotObject.IsInstanceValid(target))
				{
					goto end_IL_00fe;
				}
				_plant.timeScale = 4.0;
				target.instance.hitpoints = 1000000.0;
				target.instance.hitpointsBase = 1000000.0;
				target.ProcessMode = ProcessModeEnum.Disabled;
				FireComponent fire = _plant.componentManager?.GetRuntime<FireComponent>("character.fire");
				AdobeAnimateSprite adobeAnimateSprite = (_plant.sprite as CatGatlingPeaSprite)?.head;
				Check(fire != null && !fire.IsReleased && fire.fireNum == 4, "The real Cat Star Gatling FireComponent must retain a four-shot volley.");
				Check(GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.HasClip("HeadFire"), "The real nested head sprite must expose the HeadFire animation.");
				if (fire == null || fire.IsReleased || !GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					goto end_IL_00fe;
				}
				adobeAnimateSprite.OnAnimeEvent += OnHeadAnimeEvent;
				fire.OnFireVolley += OnNormalVolley;
				fire.OnFireOver += OnFireOver;
				fire.alive = true;
				fire.timer = 0f;
				fire.checkInterval = 0;
				FireComponentCheckConfig fireComponentCheckConfig = fire.fireCheckList[0];
				TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = fireComponentCheckConfig.projectile?.GetProjectile();
				control.isGameRunning = true;
				Check(GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) && fire.CanFireCheckOnce(towerDefenseProjectileCreateData, fireComponentCheckConfig.GetCollisionFlags()), "The real six-direction Cat Star Gatling check must acquire the live target.");
				fire.runningCheck = fireComponentCheckConfig;
				fire.runningCheckId = 0;
				for (int round = 0; round < 3; round++)
				{
					int expectedCompletedRounds = round + 1;
					fire.AttackEntered();
					for (int frame = 0; frame < 240; frame++)
					{
						if (_roundsCompleted >= expectedCompletedRounds)
						{
							break;
						}
						fire.AttackProcessing(1.0 / 60.0);
						_plant.BatchUpdate(1.0 / 60.0);
						await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
						await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					}
				}
				await WaitPhysicsFrames(2);
				Check(_roundsCompleted == 3, $"The real animation must complete {3} repeated attack rounds; got {_roundsCompleted}.");
				Check(_fireEventsPerRound.Count == 3 && _fireEventsPerRound.TrueForAll((int count) => count == 4), "Every natural HeadFire round must emit four fire events; got [" + string.Join(", ", _fireEventsPerRound) + "].");
				Check(_normalShotsPerRound.Count == 3 && _normalShotsPerRound.TrueForAll((int count) => count == 4), "Every natural round must create four normal bullets; got [" + string.Join(", ", _normalShotsPerRound) + "].");
				Check(_totalFireEvents == 12, $"Three natural rounds must emit 12 fire events; got {_totalFireEvents}.");
				Check(_totalNormalShots == 12, $"Three natural rounds must create 12 normal bullets; got {_totalNormalShots}.");
				int num = 27;
				Check(_spawnedProjectiles == num, $"Three real rounds must create 27 total bullets including five first-shot scatter stars per round; got {_spawnedProjectiles}.");
				Check(fire.currentFireNum == 0 && _plant.currentFireNum == 0, $"Both volley counters must reset after repeated rounds; component={fire.currentFireNum}, plant={_plant.currentFireNum}.");
				goto end_IL_00df;
				end_IL_00fe:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCatGatlingRepeatedVolleyRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00df;
			}
			return;
			end_IL_00df:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.OnBulletSpawned -= OnBulletSpawned;
			}
			if (GodotObject.IsInstanceValid(ProjectileUpdateManager.Instance))
			{
				ProjectileUpdateManager.Instance.ProcessMode = previousProjectileProcessMode;
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
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			_plant = null;
			for (int round = 0; round < 4; round++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		bool flag = _failures == 0;
		GD.Print($"CAT_GATLING_REPEATED_VOLLEY_RESULT passed={flag} checks={_checks} failures={_failures}");
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

	private void OnHeadAnimeEvent(string command, Variant argument)
	{
		if (command == "fire")
		{
			_totalFireEvents++;
		}
	}

	private void OnNormalVolley(ulong randomSeed)
	{
		_totalNormalShots++;
	}

	private void OnBulletSpawned(int index)
	{
		BulletField instance = BulletField.Instance;
		if (GodotObject.IsInstanceValid(instance) && instance.IsBulletActive(index) && instance.GetBulletDataRef(index).fireCharacter == _plant)
		{
			_spawnedProjectiles++;
		}
	}

	private void OnFireOver()
	{
		_roundsCompleted++;
		_fireEventsPerRound.Add(_totalFireEvents - _previousRoundFireEvents);
		_normalShotsPerRound.Add(_totalNormalShots - _previousRoundNormalShots);
		_previousRoundFireEvents = _totalFireEvents;
		_previousRoundNormalShots = _totalNormalShots;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantCatGatlingPea", "res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Packet/PlantCatGatlingPea.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantCatGatlingPea", "res://Asset/Anime/Character/Plant/Gold/CatGatlingPea/Scene/TowerDefensePlantCatGatlingPea.tscn");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterProjectile("Star", "res://Asset/Config/Projectile/Star/StarDefault.tres");
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

	private void RegisterProjectile(string key, string path)
	{
		ResourceManager instance = ResourceManager.Instance;
		if (instance.PROJECTILE_CONFIG.TryGetValue(key, out var value))
		{
			_previousProjectiles[key] = value;
		}
		else
		{
			_missingProjectiles.Add(key);
		}
		instance.PROJECTILE_CONFIG[key] = ResourceLoader.Load<TowerDefenseProjectileConfig>(path, null, ResourceLoader.CacheMode.Ignore);
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
		foreach (string missingProjectile in _missingProjectiles)
		{
			instance.PROJECTILE_CONFIG.Remove(missingProjectile);
		}
		foreach (KeyValuePair<string, Resource> previousProjectile in _previousProjectiles)
		{
			instance.PROJECTILE_CONFIG[previousProjectile.Key] = previousProjectile.Value;
		}
	}

	private async Task WaitPhysicsFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewCatGatlingRepeatedVolleyRuntimeTest] " + message);
		}
	}
}
