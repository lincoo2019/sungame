using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewDoubleMelonVolleyRuntimeTest : Node
{
	private const string WinterPacketPath = "res://Asset/Anime/Character/Plant/Gold/WinterMelonCat/Packet/PlantWinterMelonCat.tres";

	private const string WinterScenePath = "res://Asset/Anime/Character/Plant/Gold/WinterMelonCat/Scene/TowerDefensePlantWinterMelonCat.tscn";

	private const string GoldPacketPath = "res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Packet/PlantGoldMelonpult.tres";

	private const string GoldScenePath = "res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Scene/TowerDefensePlantGoldMelonpult.tscn";

	private const string ZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const int ExpectedRounds = 2;

	private const int ExpectedShotsPerRound = 2;

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
		ProcessModeEnum previousProjectileProcessMode = ProjectileUpdateManager.Instance?.ProcessMode ?? ProcessModeEnum.Inherit;
		BugOverviewDoubleMelonVolleyControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ObjectManager.Instance), "ObjectManager autoload must be available for real Node projectiles.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance) || !GodotObject.IsInstanceValid(ObjectManager.Instance))
				{
					goto end_IL_00fa;
				}
				ProjectileUpdateManager.Instance.ProcessMode = ProcessModeEnum.Disabled;
				TowerDefenseProjectileRegistry.Init();
				RegisterRealFixtures();
				control = new BugOverviewDoubleMelonVolleyControlStub
				{
					Name = "DoubleMelonVolleyControl",
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
				TowerDefensePlantWinterMelonCat winter = LoadPacket("res://Asset/Anime/Character/Plant/Gold/WinterMelonCat/Packet/PlantWinterMelonCat.tres")?.Plant(new Vector2I(2, 2), playAudio: false) as TowerDefensePlantWinterMelonCat;
				TowerDefensePlantGoldMelonpult gold = LoadPacket("res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Packet/PlantGoldMelonpult.tres")?.Plant(new Vector2I(2, 3), playAudio: false) as TowerDefensePlantGoldMelonpult;
				await WaitFrames(5);
				TowerDefenseCharacter winterTarget = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(new Vector2I(7, 2), playAudio: false);
				TowerDefenseCharacter goldTarget = LoadPacket("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres")?.Plant(new Vector2I(7, 3), playAudio: false);
				await WaitFrames(5);
				Check(GodotObject.IsInstanceValid(winter) && winter.config?.name == "PlantWinterMelonCat", "The real Winter Melon Cattail scene must instantiate.");
				Check(GodotObject.IsInstanceValid(gold) && gold.config?.name == "PlantGoldMelonpult", "The real Gold Melon-pult scene must instantiate.");
				Check(GodotObject.IsInstanceValid(winterTarget) && GodotObject.IsInstanceValid(goldTarget), "Two real normal-zombie targets must instantiate.");
				if (!GodotObject.IsInstanceValid(winter) || !GodotObject.IsInstanceValid(gold) || !GodotObject.IsInstanceValid(winterTarget) || !GodotObject.IsInstanceValid(goldTarget))
				{
					goto end_IL_00fa;
				}
				winterTarget.instance.hitpoints = 1000000.0;
				winterTarget.instance.hitpointsBase = 1000000.0;
				goldTarget.instance.hitpoints = 1000000.0;
				goldTarget.instance.hitpointsBase = 1000000.0;
				winterTarget.ProcessMode = ProcessModeEnum.Disabled;
				goldTarget.ProcessMode = ProcessModeEnum.Disabled;
				FireComponent fireComponent = winter.componentManager?.GetRuntime<FireComponent>("character.fire");
				FireComponent goldFire = gold.componentManager?.GetRuntime<FireComponent>("character.fire");
				Check(fireComponent != null && !fireComponent.IsReleased && fireComponent.fireNum == 2 && winter.fireNum == 2, "Fresh Winter Melon Cattail data must retain the authored two-shot volley.");
				Check(goldFire != null && !goldFire.IsReleased && goldFire.fireNum == 2 && gold.fireNum == 2, "Fresh Gold Melon-pult data must retain the authored two-shot volley.");
				if (fireComponent == null || fireComponent.IsReleased || goldFire == null || goldFire.IsReleased)
				{
					goto end_IL_00fa;
				}
				winter.ImportVariantSave(new Dictionary());
				Check(winter.fireNum == 2 && fireComponent.fireNum == 2, $"A legacy Winter Melon Cattail save without fireNum must default to two shots; plant={winter.fireNum}, component={fireComponent.fireNum}.");
				Dictionary dictionary = winter.ExportVariantSave();
				Check(dictionary.GetValueOrDefault("fireNum", 0).AsInt32() == 2, "The repaired legacy value must round-trip as fireNum=2.");
				winter.timeScale = 4.0;
				gold.timeScale = 4.0;
				control.isGameRunning = true;
				await RunTwoRounds(winter, fireComponent, "Winter Melon Cattail", 32);
				await RunTwoRounds(gold, goldFire, "Gold Melon-pult", 2);
				goto end_IL_00df;
				end_IL_00fa:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[DoubleMelonVolley] Unexpected exception: {value}");
				goto end_IL_00df;
			}
			return;
			end_IL_00df:;
		}
		finally
		{
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
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"DOUBLE_MELON_VOLLEY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunTwoRounds(TowerDefensePlant plant, FireComponent fire, string label, int requiredFireMethodFlag)
	{
		Check(fire.fireNum == 2, label + " must enter the shared FireComponent path with fireNum=2.");
		FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = fireComponentCheckConfig?.projectile?.GetProjectile();
		Check(GodotObject.IsInstanceValid(fireComponentCheckConfig) && GodotObject.IsInstanceValid(towerDefenseProjectileCreateData) && (towerDefenseProjectileCreateData.fireMethodFlags & requiredFireMethodFlag) != 0, label + " must retain its authored projectile method.");
		if (!GodotObject.IsInstanceValid(fireComponentCheckConfig) || !GodotObject.IsInstanceValid(towerDefenseProjectileCreateData))
		{
			return;
		}
		fire.alive = true;
		fire.timer = 0f;
		fire.checkInterval = 0;
		fire.runningCheck = fireComponentCheckConfig;
		fire.runningCheckId = 0;
		Check(fire.CanFireCheckOnce(towerDefenseProjectileCreateData, fireComponentCheckConfig.GetCollisionFlags()), label + " must acquire a real live target before firing.");
		int projectileCount = 0;
		int volleyCount = 0;
		int roundsCompleted = 0;
		int previousRoundProjectiles = 0;
		List<int> shotsPerRound = new List<int>();
		List<int> fireIndices = new List<int>();
		fire.OnFireVolley += OnVolley;
		fire.OnFireOver += OnFireOver;
		for (int round = 0; round < 2; round++)
		{
			int expectedCompleted = round + 1;
			fire.currentFireNum = 0;
			fire.AttackEntered();
			for (int frame = 0; frame < 300; frame++)
			{
				if (roundsCompleted >= expectedCompleted)
				{
					break;
				}
				fire.AttackProcessing(1.0 / 60.0);
				plant.BatchUpdate(1.0 / 60.0);
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
		}
		fire.OnFireVolley -= OnVolley;
		fire.OnFireOver -= OnFireOver;
		Check(roundsCompleted == 2, $"{label} must complete two real animated attack rounds; rounds={roundsCompleted}.");
		Check(shotsPerRound.Count == 2 && shotsPerRound.TrueForAll((int count) => count == 2), $"Every {label} round must create exactly two real projectiles; shots=[{string.Join(", ", shotsPerRound)}].");
		Check(projectileCount == 4 && volleyCount == projectileCount, $"{label} must create four projectiles and four volley signals; projectiles={projectileCount}, volleys={volleyCount}.");
		Check(fireIndices.Count == 4 && fireIndices[0] == 0 && fireIndices[1] == 1 && fireIndices[2] == 0 && fireIndices[3] == 1, $"{label} must preserve two-shot burst indices across both rounds; indices=[{string.Join(", ", fireIndices)}].");
		fire.alive = false;
		void OnFireOver()
		{
			shotsPerRound.Add(projectileCount - previousRoundProjectiles);
			previousRoundProjectiles = projectileCount;
			roundsCompleted++;
		}
		void OnVolley(ulong _)
		{
			projectileCount++;
			volleyCount++;
			fireIndices.Add(fire.currentFireNum);
		}
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
		RegisterPacket("PlantWinterMelonCat", "res://Asset/Anime/Character/Plant/Gold/WinterMelonCat/Packet/PlantWinterMelonCat.tres");
		RegisterPacket("PlantGoldMelonpult", "res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Packet/PlantGoldMelonpult.tres");
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterCharacter("PlantWinterMelonCat", "res://Asset/Anime/Character/Plant/Gold/WinterMelonCat/Scene/TowerDefensePlantWinterMelonCat.tscn");
		RegisterCharacter("PlantGoldMelonpult", "res://Asset/Anime/Character/Plant/Gold/GoldMelonpult/Scene/TowerDefensePlantGoldMelonpult.tscn");
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
			GD.PushError("[DoubleMelonVolley] " + message);
		}
	}
}
