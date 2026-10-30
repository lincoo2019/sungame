using System;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewGarCobCannonVaseSettlementRuntimeTest : Node
{
	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private const string GarCobProjectilePath = "res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres";

	private const string VasePacketPath = "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres";

	private const string VaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string CobCannonPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalCobCannon.tres";

	private const string CobCannonScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/CobCannon/TowerDefenseZombieNormalCobCannon.tscn";

	private static readonly string[] GarCobContentKeys = new string[5] { "ZombieGargantuar", "ZombieGargantuarRedEyes", "ZombieFootballGargantuar", "ZombieFootballGargantuarBlack", "ZombieDiscoGargantuar" };

	private int _checks;

	private int _failures;

	private TowerDefenseControlNew _control;

	private TowerDefenseBattleProcessVase _process;

	public override async void _Ready()
	{
		try
		{
			_ = 1;
			try
			{
				await SetupBattleFixture();
				await VerifyCannonVaseSettlement();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[GarCobCannonVaseSettlement] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (TowerDefenseInGameLevelControl.instance == _control?.levelControl)
			{
				TowerDefenseInGameLevelControl.instance = null;
			}
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
		GD.Print($"BUG_OVERVIEW_GAR_COB_VASE_SETTLEMENT_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task SetupBattleFixture()
	{
		RegisterFixtures();
		_control = new BugOverviewGarCobCannonVaseSettlementControlStub
		{
			Name = "GarCobCannonVaseSettlementControl",
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
		_control.levelControl = new BugOverviewGarCobCannonVaseSettlementLevelControlStub
		{
			Name = "LevelControl"
		};
		_control.AddChild(_control.levelControl, forceReadableName: false, InternalMode.Disabled);
		TowerDefenseInGameLevelControl.instance = _control.levelControl;
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
		_process = new TowerDefenseBattleProcessVase
		{
			control = _control,
			levelControl = _control.levelControl
		};
		_control.process = _process;
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(_control.process is TowerDefenseBattleProcessVase, "The fixture must use the production TowerDefenseBattleProcessVase.");
	}

	private async Task VerifyCannonVaseSettlement()
	{
		TowerDefensePacketConfig vasePacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", null, ResourceLoader.CacheMode.Ignore);
		TowerDefensePacketConfig cobCannonPacket = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalCobCannon.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(vasePacket) && GodotObject.IsInstanceValid(cobCannonPacket), "The authored Zombie Vase and Zombie Normal Cob Cannon packets must load.");
		TowerDefenseProjectileData towerDefenseProjectileData = ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres", null, ResourceLoader.CacheMode.Ignore);
		Check(towerDefenseProjectileData?.hitEffect != null, "The canonical GarCobCannonCob must retain its vase-spawning landing effect.");
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = towerDefenseProjectileData?.hitEffect?.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		Check(towerDefenseProjectileEffectBase is TowerDefenseProjectileEffectGarCobCannonExplode, "The real Gar-cob landing effect must instantiate before settlement is checked.");
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileEffectBase) || !GodotObject.IsInstanceValid(vasePacket) || !GodotObject.IsInstanceValid(cobCannonPacket))
		{
			return;
		}
		Vector2I gridPos = new Vector2I(3, 2);
		towerDefenseProjectileEffectBase.Init(gridPos, TowerDefenseEnum.CHARACTER_CAMP.PLANT, 0, null);
		towerDefenseProjectileEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(gridPos);
		_control.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
		_process.PhysicsProcess(0.0);
		Check(!_control.levelControl.hasSpawn && !_control.levelControl.awardCreate, "The cannon landing spawn grace frame must be consumed without awarding an early victory.");
		TowerDefenseVase hypnotizedVase = await FindVaseAsync(gridPos);
		Check(hypnotizedVase is TowerDefenseVaseZombie, "The cannon landing must create the real TowerDefenseVaseZombie scene.");
		if (GodotObject.IsInstanceValid(hypnotizedVase))
		{
			hypnotizedVase.RemoveFromGroup("Vase");
			Check(TowerDefenseManager.Instance.characterRegistry.GetVaseCount() == 1 && !_process.CheckFinal(), "A registered cannon vase must block settlement during a scene-group visibility gap.");
			hypnotizedVase.AddToGroup("Vase", persistent: true);
		}
		for (int frame = 0; frame < 6; frame++)
		{
			if (!GodotObject.IsInstanceValid(hypnotizedVase))
			{
				break;
			}
			if (GodotObject.IsInstanceValid(hypnotizedVase.packetConfig))
			{
				break;
			}
			await WaitFramePair();
		}
		Check(GodotObject.IsInstanceValid(hypnotizedVase?.packetConfig) && hypnotizedVase.packetConfig.saveKey == "ZombieNormalCobCannon" && hypnotizedVase.packetConfig.GetHypnoses(), "The cannon-created vase must contain a hypnotized real Zombie Normal Cob Cannon packet.");
		Vector2I gridPos2 = new Vector2I(6, 4);
		TowerDefenseVase hostileVase = vasePacket.Plant(gridPos2, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefenseVase;
		Check(hostileVase is TowerDefenseVaseZombie, "The hostile comparison must instantiate a second real TowerDefenseVaseZombie scene.");
		if (!GodotObject.IsInstanceValid(hypnotizedVase) || !GodotObject.IsInstanceValid(hostileVase))
		{
			return;
		}
		hostileVase.useEnterAnime = false;
		TowerDefensePacketConfig towerDefensePacketConfig = cobCannonPacket.Duplicate(deep: true) as TowerDefensePacketConfig;
		towerDefensePacketConfig.overrideHypnoses = false;
		hostileVase.SetContentConfig(towerDefensePacketConfig);
		await WaitFramePairs(4);
		int num = CountCharacters<TowerDefenseVaseZombie>();
		Check(num >= 2, $"The fixture must keep at least two real Zombie Vases unopened; got {num}.");
		Check(!hostileVase.packetConfig.GetHypnoses(), "The second vase must keep hostile Zombie Normal Cob Cannon content.");
		_process.PhysicsProcess(0.0);
		Check(!_control.levelControl.hasSpawn && !_process.CheckFinal(), "Two unopened real Zombie Vases must block settlement after the spawn grace frame clears.");
		hypnotizedVase.SmashDestroy();
		hostileVase.SmashDestroy();
		Check(_control.HasPendingBattleOperations, "Opening both real vases must register their asynchronous content operations.");
		Check(!_process.CanFinish(), "The vase process must reject finishing while either real content operation is pending.");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Check(GetTree().GetNodeCountInGroup("Vase") == 0 && _control.HasPendingBattleOperations, "The test must reach the shell-free, content-still-generating settlement window.");
		Check(!_process.CheckFinal(), "CheckFinal itself must reject the shell-free asynchronous content window.");
		for (int frame = 0; frame < 90; frame++)
		{
			if (!_control.HasPendingBattleOperations && CountCharacters<TowerDefenseZombieNormalCobCannon>() >= 2)
			{
				break;
			}
			await WaitFramePair();
		}
		Check(!_control.HasPendingBattleOperations, "Both vase content operations must complete before objective evaluation resumes.");
		Check(CountCharacters<TowerDefenseZombieNormalCobCannon>() == 2, $"Both real vases must release real Zombie Normal Cob Cannons; got {CountCharacters<TowerDefenseZombieNormalCobCannon>()}.");
		TowerDefenseZombieNormalCobCannon towerDefenseZombieNormalCobCannon = null;
		TowerDefenseZombieNormalCobCannon hypnotized = null;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieNormalCobCannon towerDefenseZombieNormalCobCannon2 && GodotObject.IsInstanceValid(towerDefenseZombieNormalCobCannon2))
			{
				if (towerDefenseZombieNormalCobCannon2.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT)
				{
					hypnotized = towerDefenseZombieNormalCobCannon2;
				}
				else if (towerDefenseZombieNormalCobCannon2.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE)
				{
					towerDefenseZombieNormalCobCannon = towerDefenseZombieNormalCobCannon2;
				}
			}
		}
		BugOverviewGarCobCannonVaseSettlementRuntimeTest bugOverviewGarCobCannonVaseSettlementRuntimeTest = this;
		int condition;
		if (towerDefenseZombieNormalCobCannon != null)
		{
			TowerDefenseCharacterInstance instance = towerDefenseZombieNormalCobCannon.instance;
			condition = ((instance != null && !instance.hypnoses) ? 1 : 0);
		}
		else
		{
			condition = 0;
		}
		bugOverviewGarCobCannonVaseSettlementRuntimeTest.Check((byte)condition != 0, "The non-hypnotized vase content must remain an enemy objective.");
		Check(hypnotized != null && (hypnotized.instance?.hypnoses ?? false), "The cannon-created vase content must become a friendly hypnotized zombie.");
		if (!GodotObject.IsInstanceValid(towerDefenseZombieNormalCobCannon) || !GodotObject.IsInstanceValid(hypnotized))
		{
			return;
		}
		_process.PhysicsProcess(0.0);
		Check(!_process.CheckFinal(), "The living hostile Zombie Normal Cob Cannon must block vase-level settlement.");
		towerDefenseZombieNormalCobCannon.Scale = new Vector2(0f - Mathf.Abs(towerDefenseZombieNormalCobCannon.Scale.X), towerDefenseZombieNormalCobCannon.Scale.Y);
		Check(!_process.CheckFinal(), "A hostile zombie must remain an objective when only its visual X scale is negative.");
		hypnotized.Scale = new Vector2(Mathf.Abs(hypnotized.Scale.X), hypnotized.Scale.Y);
		Check(hypnotized.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && hypnotized.instance.hypnoses, "Hypnotized settlement semantics must come from camp/state, not visual orientation.");
		towerDefenseZombieNormalCobCannon.skipDestroySet = true;
		towerDefenseZombieNormalCobCannon.Destroy();
		for (int frame = 0; frame < 90; frame++)
		{
			if (!HasLivingHostileCharacter())
			{
				break;
			}
			await WaitFramePair();
		}
		Check(!HasLivingHostileCharacter(), "The hostile Zombie Normal Cob Cannon must fully leave the objective set.");
		Check(_process.CheckFinal() && !_control.levelControl.awardCreate, "After every vase and hostile content is cleared, the settlement check must pass without the test invoking award UI.");
		Check(GodotObject.IsInstanceValid(hypnotized) && hypnotized.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT && hypnotized.instance.hypnoses, "Successful settlement must leave the friendly hypnotized real cannon content intact.");
	}

	private async Task<TowerDefenseVase> FindVaseAsync(Vector2I gridPos)
	{
		for (int frame = 0; frame < 30; frame++)
		{
			foreach (Node child in _control.characterNode.GetChildren())
			{
				if (child is TowerDefenseVase towerDefenseVase && GodotObject.IsInstanceValid(towerDefenseVase) && towerDefenseVase.gridPos == gridPos)
				{
					return towerDefenseVase;
				}
			}
			await WaitFramePair();
		}
		return null;
	}

	private bool HasLivingHostileCharacter()
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				return true;
			}
		}
		return false;
	}

	private int CountCharacters<T>() where T : TowerDefenseCharacter
	{
		int num = 0;
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is T val && GodotObject.IsInstanceValid(val) && !val.IsQueuedForDeletion())
			{
				num++;
			}
		}
		return num;
	}

	private async Task WaitFramePair()
	{
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
	}

	private async Task WaitFramePairs(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await WaitFramePair();
		}
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
		TowerDefensePacketConfig value3 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Plant/ZombieNormalCobCannon.tres", null, ResourceLoader.CacheMode.Ignore);
		PackedScene value4 = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/CobCannon/TowerDefenseZombieNormalCobCannon.tscn", null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_PACKETS["VaseZombie"] = value;
		instance.TOWERDEFENSE_CHARCATERS["VaseZombie"] = value2;
		instance.TOWERDEFENSE_PACKETS["ZombieNormalCobCannon"] = value3;
		instance.TOWERDEFENSE_CHARCATERS["ZombieNormalCobCannon"] = value4;
		string[] garCobContentKeys = GarCobContentKeys;
		foreach (string key in garCobContentKeys)
		{
			instance.TOWERDEFENSE_PACKETS[key] = value3;
		}
	}

	private static void UnregisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			instance.TOWERDEFENSE_PACKETS.Remove("VaseZombie");
			instance.TOWERDEFENSE_CHARCATERS.Remove("VaseZombie");
			instance.TOWERDEFENSE_PACKETS.Remove("ZombieNormalCobCannon");
			instance.TOWERDEFENSE_CHARCATERS.Remove("ZombieNormalCobCannon");
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
			GD.PushError("[GarCobCannonVaseSettlement] " + message);
		}
	}
}
