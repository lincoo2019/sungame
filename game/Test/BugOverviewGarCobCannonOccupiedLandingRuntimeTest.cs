using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGarCobCannonOccupiedLandingRuntimeTest : Node
{
	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private const string GarCobProjectilePath = "res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres";

	private const string GarCobPacketPath = "res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Packet/PlantGarCobCannon.tres";

	private const string GarCobScenePath = "res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Scene/TowerDefensePlantGarCobCannon.tscn";

	private const string VasePacketPath = "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres";

	private const string VaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string ContentPacketPath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres";

	private static readonly string[] GarCobContentKeys = new string[5] { "ZombieGargantuar", "ZombieGargantuarRedEyes", "ZombieFootballGargantuar", "ZombieFootballGargantuarBlack", "ZombieDiscoGargantuar" };

	private int _checks;

	private int _failures;

	private TowerDefenseControlNew _control;

	public override async void _Ready()
	{
		try
		{
			_ = 3;
			try
			{
				await SetupBattleFixture();
				await VerifyEmptyCellLandingStillCreatesVase();
				await VerifyOccupiedGarCobLandingCreatesVase();
				await VerifyExtendGridNoLimitBoundarySafety();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[GarCobOccupiedLanding] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
			{
				TowerDefenseManager.Instance.currentControl = null;
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			UnregisterFixtures();
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_I106_GAR_COB_OCCUPIED_LANDING_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task SetupBattleFixture()
	{
		RegisterFixtures();
		_control = new BugOverviewI106GarCobControlStub
		{
			Name = "GarCobOccupiedLandingControl",
			isGameRunning = true,
			isInit = true,
			levelConfig = new TowerDefenseLevelConfig()
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.levelControl = new BugOverviewI106GarCobLevelControlStub
		{
			Name = "LevelControl"
		};
		_control.AddChild(_control.levelControl, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (!GodotObject.IsInstanceValid(instance))
		{
			throw new InvalidOperationException("TowerDefenseManager autoload is unavailable.");
		}
		instance.currentControl = _control;
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = new Vector2I(9, 5),
			gridBeginPos = new Vector2(0f, 100f),
			gridSize = new Vector2(100f, 76f),
			plantOffset = 50.0
		};
		instance.gridNum = towerDefenseMapConfig.gridNum;
		instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
		instance.gridSize = towerDefenseMapConfig.gridSize;
		TowerDefenseMapControl towerDefenseMapControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseMapControl>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseMapControl))
		{
			throw new InvalidOperationException("The real map control did not instantiate.");
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (towerDefenseMapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			mapControl = towerDefenseMapControl
		});
		_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
		CreateGrid(towerDefenseBattleFeatureMap, towerDefenseMapConfig.gridNum);
		_control.AddChild(towerDefenseMapControl, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task VerifyEmptyCellLandingStillCreatesVase()
	{
		Vector2I gridPos = new Vector2I(3, 3);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(gridPos);
		Check(GodotObject.IsInstanceValid(mapCell), "The empty-cell scenario must use a real map cell.");
		Check((await SpawnLandingVase(gridPos)) is TowerDefenseVaseZombie, "A Gar-cob landing on an empty cell must still create a Zombie Vase.");
	}

	private async Task VerifyOccupiedGarCobLandingCreatesVase()
	{
		Vector2I gridPos = new Vector2I(5, 3);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(gridPos);
		Check(GodotObject.IsInstanceValid(cell), "The occupied-cell scenario must use a real map cell.");
		TowerDefensePacketConfig garCobPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Packet/PlantGarCobCannon.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(garCobPacket), "The canonical GarCob packet must load.");
		Check(GodotObject.IsInstanceValid(cell) && cell.CanPacketPlant(garCobPacket), "The real map cell must accept the GarCob plant before occupation.");
		TowerDefenseCharacter garCob = garCobPacket?.Plant(gridPos, playAudio: false);
		Check(garCob is TowerDefensePlantGarCobCannon, "The occupied landing cell must contain a real GarCob plant.");
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(GodotObject.IsInstanceValid(garCob) && GodotObject.IsInstanceValid(cell) && cell.GetCharacterList().Contains(garCob), "The GarCob plant must occupy the landing cell's ground slot.");
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefensePacketConfig) && GodotObject.IsInstanceValid(cell) && !cell.CanPacketPlant(towerDefensePacketConfig), "Normal placement rules must reject the Zombie Vase on the occupied GarCob cell.");
		TowerDefenseCharacter instance = towerDefensePacketConfig?.Plant(gridPos, playAudio: false);
		Check(!GodotObject.IsInstanceValid(instance), "Plant with noLimit=false must preserve the occupied-cell rejection.");
		TowerDefenseCharacter instance2 = towerDefensePacketConfig?.Plant(gridPos, playAudio: false, noLimit: true);
		Check(!GodotObject.IsInstanceValid(instance2), "Plant with noLimit=true must still preserve terrain and occupied-cell validation unless placement checks are explicitly skipped.");
		Check((await SpawnLandingVase(gridPos)) is TowerDefenseVaseZombie, "A Gar-cob landing on its occupied origin cell must create a Zombie Vase.");
		Check(GodotObject.IsInstanceValid(garCob) && GodotObject.IsInstanceValid(cell) && cell.GetCharacterList().Contains(garCob), "Spawning the vase without placement limits must retain the original GarCob plant.");
		TowerDefenseCharacter forcedGarCob = garCobPacket.Plant(gridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
		Check(forcedGarCob is TowerDefensePlantGarCobCannon && forcedGarCob != garCob, "A valid occupied footprint must allow a real extend-grid GarCob to be force-spawned.");
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(GodotObject.IsInstanceValid(forcedGarCob) && cell.GetCharacterList().Contains(forcedGarCob), "The valid no-limit GarCob must finish planting on the occupied cell.");
	}

	private async Task VerifyExtendGridNoLimitBoundarySafety()
	{
		Vector2I boundaryGridPos = new Vector2I(9, 4);
		TowerDefenseCellInstance mapCell = TowerDefenseManager.GetMapCell(boundaryGridPos);
		Check(GodotObject.IsInstanceValid(mapCell), "The extend-grid boundary scenario must start from a real rightmost map cell.");
		TowerDefenseCharacter instance = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Packet/PlantGarCobCannon.tres", null, ResourceLoader.CacheMode.Ignore)?.Plant(boundaryGridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true);
		Check(!GodotObject.IsInstanceValid(instance), "A no-limit extend-grid plant whose footprint leaves the map must return null.");
		for (int frame = 0; frame < 4; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		Check(!HasGarCobAtGrid(boundaryGridPos), "A rejected right-boundary GarCob must not be added on a later deferred frame.");
	}

	private async Task<TowerDefenseVase> SpawnLandingVase(Vector2I gridPos)
	{
		TowerDefenseProjectileData towerDefenseProjectileData = ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(towerDefenseProjectileData?.hitEffect != null, "The canonical GarCobCannonCob must retain its real landing effect.");
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = towerDefenseProjectileData?.hitEffect?.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		Check(towerDefenseProjectileEffectBase is TowerDefenseProjectileEffectGarCobCannonExplode, "GarCobCannonCob must instantiate the production vase-spawning effect.");
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileEffectBase))
		{
			return null;
		}
		towerDefenseProjectileEffectBase.Init(gridPos, TowerDefenseEnum.CHARACTER_CAMP.PLANT, 0, null);
		towerDefenseProjectileEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		_control.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseVase towerDefenseVase = null;
		for (int frame = 0; frame < 20; frame++)
		{
			if (GodotObject.IsInstanceValid(towerDefenseVase))
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			towerDefenseVase = FindSpawnedVase(gridPos);
		}
		return towerDefenseVase;
	}

	private TowerDefenseVase FindSpawnedVase(Vector2I gridPos)
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase) && towerDefenseVase.gridPos == gridPos)
			{
				return towerDefenseVase;
			}
		}
		return null;
	}

	private bool HasGarCobAtGrid(Vector2I gridPos)
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefensePlantGarCobCannon towerDefensePlantGarCobCannon && GodotObject.IsInstanceValid(towerDefensePlantGarCobCannon) && towerDefensePlantGarCobCannon.gridPos == gridPos)
			{
				return true;
			}
		}
		return false;
	}

	private static void CreateGrid(TowerDefenseBattleFeatureMap mapFeature, Vector2I gridNum)
	{
		TowerDefenseCellConfig config = new TowerDefenseCellConfig();
		mapFeature.plantGrid.Clear();
		for (int i = 0; i <= gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			for (int j = 0; j <= gridNum.Y; j++)
			{
				if (i == 0 || j == 0)
				{
					array.Add(default(Variant));
					continue;
				}
				TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
				{
					gridPos = new Vector2I(i, j)
				};
				towerDefenseCellInstance.Init(config);
				array.Add(towerDefenseCellInstance);
			}
			mapFeature.plantGrid.Add(array);
		}
		mapFeature.iceCapList.Clear();
		mapFeature.lineUse.Clear();
		for (int k = 0; k <= gridNum.Y; k++)
		{
			mapFeature.iceCapList.Add(default(Variant));
			mapFeature.lineUse.Add(k > 0);
		}
	}

	private static void RegisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		TowerDefensePacketConfig value = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene value2 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePacketConfig value3 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Packet/PlantGarCobCannon.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene value4 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Gold/GarCobCannon/Scene/TowerDefensePlantGarCobCannon.tscn", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePacketConfig value5 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres", null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_PACKETS["VaseZombie"] = value;
		instance.TOWERDEFENSE_CHARCATERS["VaseZombie"] = value2;
		instance.TOWERDEFENSE_PACKETS["PlantGarCobCannon"] = value3;
		instance.TOWERDEFENSE_CHARCATERS["PlantGarCobCannon"] = value4;
		string[] garCobContentKeys = GarCobContentKeys;
		foreach (string key in garCobContentKeys)
		{
			instance.TOWERDEFENSE_PACKETS[key] = value5;
		}
	}

	private static void UnregisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS.Remove("VaseZombie");
			instance.TOWERDEFENSE_CHARCATERS.Remove("VaseZombie");
			instance.TOWERDEFENSE_PACKETS.Remove("PlantGarCobCannon");
			instance.TOWERDEFENSE_CHARCATERS.Remove("PlantGarCobCannon");
			string[] garCobContentKeys = GarCobContentKeys;
			foreach (string key in garCobContentKeys)
			{
				instance.TOWERDEFENSE_PACKETS.Remove(key);
			}
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[GarCobOccupiedLanding] " + message);
		}
	}
}
