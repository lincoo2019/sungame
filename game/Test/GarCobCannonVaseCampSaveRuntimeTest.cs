using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;

public partial class GarCobCannonVaseCampSaveRuntimeTest : Node
{
	private static readonly (string Key, string PacketPath, string ScenePath, string SpritePath)[] Fixtures = new(string, string, string, string)[6]
	{
		("VaseZombie", "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn", "res://Asset/Anime/Character/Vase/Zombie/VaseZombie.tscn"),
		("ZombieGargantuar", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Base/ZombieGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/Base/TowerDefenseZombieGargantuar.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Sprite/Base/ZombieGargantuar.tscn"),
		("ZombieGargantuarRedEyes", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/Redeyes/ZombieGargantuarRedeyes.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/RedEyes/TowerDefenseZombieGargantuarRedEyes.tscn", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Sprite/RedEyes/ZombieGargantuarRedEyes.tscn"),
		("ZombieFootballGargantuar", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Packet/Base/ZombieFootballGargantuar.tres", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Scene/Base/TowerDefenseZombieFootballGargantuar.tscn", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Sprite/Base/ZombieFootballGargantuar.tscn"),
		("ZombieFootballGargantuarBlack", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Packet/Base/ZombieFootballGargantuarBlack.tres", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Scene/Black/TowerDefenseZombieFootballGargantuarBlack.tscn", "res://Asset/Anime/Character/Zombie/Challenge/FootballGargantuar/Sprite/Black/ZombieFootballGargantuarBlack.tscn"),
		("ZombieDiscoGargantuar", "res://Asset/Anime/Character/Zombie/Chapter6/DiscoGargantuar/Packet/ZombieDiscoGargantuar.tres", "res://Asset/Anime/Character/Zombie/Chapter6/DiscoGargantuar/Scene/TowerDefenseZombieDiscoGargantuar.tscn", "res://Asset/Anime/Character/Zombie/Chapter6/DiscoGargantuar/ZombieDiscoGargantuar.tscn")
	};

	private readonly Dictionary<string, Resource> _previousPackets = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousCharacters = new Dictionary<string, Resource>();

	private readonly Dictionary<string, Resource> _previousSprites = new Dictionary<string, Resource>();

	private GarCobCannonVaseCampSaveControl _control;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		ResourceManager resources = ResourceManager.Instance;
		TowerDefenseControlNew previousControl = manager.currentControl;
		Vector2 previousGridBegin = manager.gridBeginPos;
		Vector2 previousGridSize = manager.gridSize;
		Vector2I previousGridNum = manager.gridNum;
		FieldInfo loadStateField = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
		GameplayResourceLoadState previousLoadState = (GameplayResourceLoadState)loadStateField.GetValue(resources);
		TowerDefenseMapControl mapControl = null;
		try
		{
			RegisterFixtures(resources);
			loadStateField.SetValue(resources, GameplayResourceLoadState.Ready);
			_control = new GarCobCannonVaseCampSaveControl
			{
				isGameRunning = false,
				isInit = true,
				levelConfig = new TowerDefenseLevelConfig(),
				characterNode = new Node2D()
			};
			AddChild(_control, forceReadableName: false, InternalMode.Disabled);
			_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
			manager.currentControl = _control;
			manager.gridBeginPos = Vector2.Zero;
			manager.gridSize = new Vector2(100f, 76f);
			manager.gridNum = new Vector2I(9, 5);
			mapControl = new TowerDefenseMapControl();
			CreateMap(mapControl, manager);
			await VerifyCannonVaseRoundTrip();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"[GarCobCannonVaseCampSaveRuntimeTest] {value}");
		}
		finally
		{
			_control?.QueueFree();
			await WaitFrames(3);
			manager.currentControl = previousControl;
			manager.gridBeginPos = previousGridBegin;
			manager.gridSize = previousGridSize;
			manager.gridNum = previousGridNum;
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			(string, string, string, string)[] fixtures = Fixtures;
			for (int i = 0; i < fixtures.Length; i++)
			{
				(string, string, string, string) tuple = fixtures[i];
				RestoreRegistryEntry(resources.TOWERDEFENSE_PACKETS, _previousPackets, tuple.Item1);
				RestoreRegistryEntry(resources.TOWERDEFENSE_CHARCATERS, _previousCharacters, tuple.Item1);
				RestoreRegistryEntry(resources.CHARCTAER_SPRITE, _previousSprites, tuple.Item1);
			}
			loadStateField.SetValue(resources, previousLoadState);
		}
		bool flag = _failures == 0 && _checks == 21;
		GD.Print($"GAR_COB_CANNON_VASE_CAMP_SAVE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyCannonVaseRoundTrip()
	{
		Vector2I friendlyGrid = new Vector2I(3, 2);
		TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase = ResourceLoader.Load<TowerDefenseProjectileData>("res://Registry/Projectile/Config/CannonCob/GarCobCannonCob.tres", null, ResourceLoader.CacheMode.Reuse).hitEffect.Instantiate<TowerDefenseProjectileEffectBase>(PackedScene.GenEditState.Disabled);
		Check(towerDefenseProjectileEffectBase is TowerDefenseProjectileEffectGarCobCannonExplode, "必须使用正式僵尸加农炮炮弹的落地效果。");
		towerDefenseProjectileEffectBase.Init(friendlyGrid, TowerDefenseEnum.CHARACTER_CAMP.PLANT, 0, null);
		towerDefenseProjectileEffectBase.GlobalPosition = TowerDefenseManager.GetMapCellPlantPos(friendlyGrid);
		_control.characterNode.AddChild(towerDefenseProjectileEffectBase, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(5);
		TowerDefenseVase towerDefenseVase = FindCharacter<TowerDefenseVase>(friendlyGrid);
		Check(towerDefenseVase is TowerDefenseVaseZombie && GodotObject.IsInstanceValid(towerDefenseVase.packetConfig), "正式炮弹落地后必须创建装有巨人的僵尸罐子。");
		string contentKey = towerDefenseVase.packetConfig.saveKey;
		Check(towerDefenseVase.packetConfig.GetHypnoses(), "刚投放的罐子必须装有魅惑巨人。");
		for (int round = 1; round <= 2; round++)
		{
			towerDefenseVase = await SaveAndRestoreVase(towerDefenseVase, contentKey, round);
			Check(towerDefenseVase.packetConfig.GetHypnoses(), $"第 {round} 次读档后，罐内巨人必须保留魅惑状态。");
		}
		await BreakAndCheckCamp(towerDefenseVase, contentKey, friendly: true);
		Vector2I gridPos = new Vector2I(6, 4);
		TowerDefenseVase hostileVase = TowerDefenseManager.GetPacketConfig("VaseZombie").Plant(gridPos, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefenseVase;
		hostileVase.SetContentConfig(TowerDefenseManager.GetPacketConfig(contentKey));
		await WaitFrames(3);
		Check(!hostileVase.packetConfig.GetHypnoses(), "普通对照罐子必须装有敌方巨人。");
		hostileVase = await SaveAndRestoreVase(hostileVase, contentKey, 3);
		Check(!hostileVase.packetConfig.GetHypnoses(), "普通罐子读档后不得被错误改为友方。");
		await BreakAndCheckCamp(hostileVase, contentKey, friendly: false);
		Check(!TowerDefenseManager.GetPacketConfigReadOnly(contentKey).GetHypnoses(), "罐内的独立魅惑状态不得污染共享巨人卡包。");
	}

	private async Task<TowerDefenseVase> SaveAndRestoreVase(TowerDefenseVase vase, string contentKey, int round)
	{
		TowerDefenseCharacterSaveConfigCSharp towerDefenseCharacterSaveConfigCSharp = new TowerDefenseCharacterSaveConfigCSharp();
		towerDefenseCharacterSaveConfigCSharp.SaveCharacter(vase);
		string savePath = $"user://gar-cob-vase-camp-{round}.res";
		Check(ResourceSaver.Save(towerDefenseCharacterSaveConfigCSharp, savePath, ResourceSaver.SaverFlags.None) == Error.Ok, $"第 {round} 次角色进度必须成功写入磁盘。");
		vase.QueueFree();
		await WaitFrames(3);
		towerDefenseCharacterSaveConfigCSharp = ResourceLoader.Load<TowerDefenseCharacterSaveConfigCSharp>(savePath, null, ResourceLoader.CacheMode.Ignore);
		Check(GodotObject.IsInstanceValid(towerDefenseCharacterSaveConfigCSharp), $"第 {round} 次必须从磁盘重新读取角色进度。");
		_control.hasProgress = true;
		towerDefenseCharacterSaveConfigCSharp.owner = new TowerDefenseLevelSaveConfigCSharp();
		TowerDefenseVase restored = towerDefenseCharacterSaveConfigCSharp.InstantiateCharacterForRestore() as TowerDefenseVase;
		towerDefenseCharacterSaveConfigCSharp.owner.charcterDicionary[towerDefenseCharacterSaveConfigCSharp.nodeName] = restored;
		towerDefenseCharacterSaveConfigCSharp.RestoreCharacter(restored);
		await WaitFrames(3);
		Check(restored is TowerDefenseVaseZombie && restored.packetConfig?.saveKey == contentKey, $"第 {round} 次读档必须保留罐子类型和原先抽中的巨人。");
		return restored;
	}

	private async Task BreakAndCheckCamp(TowerDefenseVase vase, string contentKey, bool friendly)
	{
		Vector2I grid = vase.gridPos;
		vase.SmashDestroy();
		for (int frame = 0; frame < 45; frame++)
		{
			if (!_control.HasPendingBattleOperations)
			{
				break;
			}
			await WaitFrames(1);
		}
		TowerDefenseZombie towerDefenseZombie = FindCharacter<TowerDefenseZombie>(grid);
		Check(GodotObject.IsInstanceValid(towerDefenseZombie) && towerDefenseZombie.config.name == contentKey, "读档后开罐必须生成原先保存的正式巨人角色。");
		TowerDefenseEnum.CHARACTER_CAMP cHARACTER_CAMP = ((!friendly) ? TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE : TowerDefenseEnum.CHARACTER_CAMP.PLANT);
		Check(towerDefenseZombie != null && towerDefenseZombie.instance?.hypnoses == friendly && towerDefenseZombie.camp == cHARACTER_CAMP, $"开罐后巨人的阵营必须为 {cHARACTER_CAMP}，魅惑状态必须为 {friendly}。");
		GD.Print($"GAR_COB_VASE_RELEASE content={contentKey} expectedCamp={cHARACTER_CAMP} actualCamp={towerDefenseZombie?.camp} hypnoses={towerDefenseZombie?.instance?.hypnoses}");
	}

	private T FindCharacter<T>(Vector2I grid) where T : TowerDefenseCharacter
	{
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is T val && !val.IsQueuedForDeletion() && val.gridPos == grid)
			{
				return val;
			}
		}
		return null;
	}

	private void CreateMap(TowerDefenseMapControl mapControl, TowerDefenseManager manager)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = manager.gridNum,
			gridBeginPos = manager.gridBeginPos,
			gridSize = manager.gridSize,
			plantOffset = 50.0
		};
		towerDefenseMapConfig.cellConfig.Add(new TowerDefenseCellConfig
		{
			pos = new Vector4I(1, 1, manager.gridNum.X, manager.gridNum.Y)
		});
		for (int i = 1; i <= manager.gridNum.Y; i++)
		{
			towerDefenseMapConfig.lineUse.Add(i);
		}
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			mapControl = mapControl
		});
		_control.featureDictionary["Map"] = towerDefenseBattleFeatureMap;
		towerDefenseBattleFeatureMap.PlantGridInit();
	}

	private void RegisterFixtures(ResourceManager resources)
	{
		(string, string, string, string)[] fixtures = Fixtures;
		for (int i = 0; i < fixtures.Length; i++)
		{
			(string, string, string, string) tuple = fixtures[i];
			if (resources.TOWERDEFENSE_PACKETS.TryGetValue(tuple.Item1, out var value))
			{
				_previousPackets[tuple.Item1] = value;
			}
			if (resources.TOWERDEFENSE_CHARCATERS.TryGetValue(tuple.Item1, out var value2))
			{
				_previousCharacters[tuple.Item1] = value2;
			}
			if (resources.CHARCTAER_SPRITE.TryGetValue(tuple.Item1, out var value3))
			{
				_previousSprites[tuple.Item1] = value3;
			}
			resources.TOWERDEFENSE_PACKETS[tuple.Item1] = ResourceLoader.Load<TowerDefensePacketConfig>(tuple.Item2, null, ResourceLoader.CacheMode.Reuse);
			resources.TOWERDEFENSE_CHARCATERS[tuple.Item1] = ResourceLoader.Load<PackedScene>(tuple.Item3, null, ResourceLoader.CacheMode.Reuse);
			resources.CHARCTAER_SPRITE[tuple.Item1] = ResourceLoader.Load<PackedScene>(tuple.Item4, null, ResourceLoader.CacheMode.Reuse);
		}
	}

	private static void RestoreRegistryEntry(Dictionary<string, Resource> registry, Dictionary<string, Resource> previous, string key)
	{
		if (previous.TryGetValue(key, out var value))
		{
			registry[key] = value;
		}
		else
		{
			registry.Remove(key);
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
			GD.PushError("[GarCobCannonVaseCampSaveRuntimeTest] " + message);
		}
	}
}
