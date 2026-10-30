using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewHypnotizedPresentBoxSpawnRuntimeTest : Node
{
	private const string FocusedPacketBankName = "RuntimeHypnotizedPresentBox";

	private const string NormalPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ImpPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/PresentBox/ZombieImpPresentBox.tres";

	private const string ImpScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PresentBox/TowerDefenseZombieImpPresentBox.tscn";

	private const string GargantuarPacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/PresentBox/ZombieGargantuarPresentBox.tres";

	private const string GargantuarScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/PresentBox/TowerDefenseZombieGargantuarPresentBox.tscn";

	private static readonly Vector2I ImpGrid = new Vector2I(3, 2);

	private static readonly Vector2I GargantuarGrid = new Vector2I(6, 3);

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly List<Resource> _registeredResources = new List<Resource>();

	private TowerDefensePacketBankData _previousPacketBank;

	private bool _packetBankWasMissing;

	private GameplayResourceLoadState _previousGameplayResourceLoadState;

	private bool _gameplayResourceLoadStateOverridden;

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		HypnotizedPresentBoxSpawnRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombie impGenerated = null;
		TowerDefenseZombie gargantuarGenerated = null;
		try
		{
			_ = 2;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "测试必须能访问 TowerDefenseManager 自动加载节点。");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "测试必须能访问 ResourceManager 自动加载节点。");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					throw new InvalidOperationException("运行回归所需的自动加载节点不可用。");
				}
				RegisterRealFixtures();
				PublishFocusedGameplayResourceReady();
				Check(ResourceManager.Instance.AreFullGameplayResourcesReady, "聚焦夹具必须只在所需真实资源注册完成后发布 Ready 状态。");
				Check(GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketConfig("ZombieNormal")) && GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketConfig("ZombieImpPresentBox")) && GodotObject.IsInstanceValid(TowerDefenseManager.GetPacketConfig("ZombieGargantuarPresentBox")), "测试必须注册普通僵尸及两类真实礼盒僵尸卡片。");
				control = new HypnotizedPresentBoxSpawnRuntimeControlStub
				{
					Name = "HypnotizedPresentBoxSpawnRuntimeControl",
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
				Check(GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(ImpGrid)) && GodotObject.IsInstanceValid(TowerDefenseManager.GetMapCell(GargantuarGrid)), "测试地图必须发布两类礼盒使用的格子。");
				impGenerated = await VerifyPresentBox<TowerDefenseZombieImpPresentBox>("ZombieImpPresentBox", ImpGrid);
				if (GodotObject.IsInstanceValid(impGenerated))
				{
					impGenerated.QueueFree();
					await WaitFrames(4);
				}
				gargantuarGenerated = await VerifyPresentBox<TowerDefenseZombieGargantuarPresentBox>("ZombieGargantuarPresentBox", GargantuarGrid);
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewHypnotizedPresentBoxSpawnRuntimeTest] 运行测试出现异常：{value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(impGenerated) && !impGenerated.IsQueuedForDeletion())
			{
				impGenerated.QueueFree();
			}
			if (GodotObject.IsInstanceValid(gargantuarGenerated) && !gargantuarGenerated.IsQueuedForDeletion())
			{
				gargantuarGenerated.QueueFree();
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
			await WaitFrames(8);
			RestoreRealFixtures();
			RestoreGameplayResourceLoadState();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
		}
		bool flag = _failures == 0 && _checks == 25;
		GD.Print($"HYPNOTIZED_PRESENT_BOX_SPAWN_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void PublishFocusedGameplayResourceReady()
	{
		FieldInfo field = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
		if (field == null)
		{
			throw new MissingFieldException(typeof(ResourceManager).FullName, "_gameplayResourceLoadState");
		}
		_previousGameplayResourceLoadState = (GameplayResourceLoadState)field.GetValue(ResourceManager.Instance);
		field.SetValue(ResourceManager.Instance, GameplayResourceLoadState.Ready);
		_gameplayResourceLoadStateOverridden = true;
	}

	private void RestoreGameplayResourceLoadState()
	{
		if (_gameplayResourceLoadStateOverridden && GodotObject.IsInstanceValid(ResourceManager.Instance))
		{
			FieldInfo field = typeof(ResourceManager).GetField("_gameplayResourceLoadState", BindingFlags.Instance | BindingFlags.NonPublic);
			if (field != null)
			{
				field.SetValue(ResourceManager.Instance, _previousGameplayResourceLoadState);
			}
			_gameplayResourceLoadStateOverridden = false;
		}
	}

	private async Task<TowerDefenseZombie> VerifyPresentBox<T>(string packetName, Vector2I gridPos) where T : TowerDefenseZombie
	{
		T presentBox = TowerDefenseManager.GetPacketConfig(packetName)?.Plant(gridPos) as T;
		Check(GodotObject.IsInstanceValid(presentBox), packetName + " 必须实例化对应的真实礼盒僵尸场景。");
		if (!GodotObject.IsInstanceValid(presentBox))
		{
			return null;
		}
		await WaitFrames(6);
		Check(presentBox.config?.name == packetName && presentBox.packet?.saveKey == packetName, packetName + " 必须保留正式配置和卡片身份。");
		if (!(presentBox is TowerDefenseZombieImpPresentBox towerDefenseZombieImpPresentBox))
		{
			if (presentBox is TowerDefenseZombieGargantuarPresentBox towerDefenseZombieGargantuarPresentBox)
			{
				towerDefenseZombieGargantuarPresentBox.packetBank = "RuntimeHypnotizedPresentBox";
			}
		}
		else
		{
			towerDefenseZombieImpPresentBox.packetBank = "RuntimeHypnotizedPresentBox";
		}
		presentBox.Hypnoses(-1.0, canFliter: false);
		await WaitFrames(2);
		Check((presentBox.instance?.hypnoses ?? false) && presentBox.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, packetName + " 死亡前必须已经处于植物阵营的魅惑状态。");
		Check(presentBox.BuffGet("Hypnoses") is TowerDefenseCharacterBuffHypnoses, packetName + " 死亡前必须持有真实魅惑 Buff。");
		presentBox.HitpointsEmpty();
		presentBox.BuffDelete("Hypnoses");
		BugOverviewHypnotizedPresentBoxSpawnRuntimeTest bugOverviewHypnotizedPresentBoxSpawnRuntimeTest = this;
		TowerDefenseCharacterInstance instance = presentBox.instance;
		bugOverviewHypnotizedPresentBoxSpawnRuntimeTest.Check(instance != null && !instance.hypnoses && presentBox.camp == TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE, packetName + " 的夹具必须在异步生成前模拟死亡清理魅惑状态。");
		await WaitFrames(16);
		TowerDefenseZombie towerDefenseZombie = FindGeneratedZombie(TowerDefenseManager.GetCharacterNode(), presentBox);
		Check(GodotObject.IsInstanceValid(towerDefenseZombie), packetName + " 死亡后必须生成确定性的普通僵尸。");
		Check(towerDefenseZombie?.config?.name == "ZombieNormal", packetName + " 测试生成物必须来自指定的普通僵尸卡片。");
		Check(towerDefenseZombie != null && towerDefenseZombie.instance?.hypnoses == true && towerDefenseZombie != null && towerDefenseZombie.camp == TowerDefenseEnum.CHARACTER_CAMP.PLANT, packetName + " 生成的普通僵尸必须立即继承魅惑状态和植物阵营。");
		Check(towerDefenseZombie?.BuffGet("Hypnoses") is TowerDefenseCharacterBuffHypnoses, packetName + " 生成的普通僵尸必须持有永久魅惑 Buff。");
		Check(towerDefenseZombie != null && towerDefenseZombie.Scale.X < 0f, packetName + " 生成的魅惑僵尸必须使用反向逻辑朝向。");
		return towerDefenseZombie;
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridSize = new Vector2(100f, 76f),
				gridBeginPos = Vector2.Zero
			}
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= gridNum.X; i++)
		{
			for (int j = 1; j <= gridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(new TowerDefenseCellConfig());
			}
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres");
		RegisterPacket("ZombieImpPresentBox", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/PresentBox/ZombieImpPresentBox.tres");
		RegisterPacket("ZombieGargantuarPresentBox", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Packet/PresentBox/ZombieGargantuarPresentBox.tres");
		RegisterCharacter("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterCharacter("ZombieImpPresentBox", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/PresentBox/TowerDefenseZombieImpPresentBox.tscn");
		RegisterCharacter("ZombieGargantuarPresentBox", "res://Asset/Anime/Character/Zombie/Chapter1/Gargantuar/Scene/PresentBox/TowerDefenseZombieGargantuarPresentBox.tscn");
		ResourceManager instance = ResourceManager.Instance;
		if (instance.TOWERDEFENSE_PACKETBANKS.TryGetValue("RuntimeHypnotizedPresentBox", out var value))
		{
			_previousPacketBank = value;
		}
		else
		{
			_packetBankWasMissing = true;
		}
		instance.TOWERDEFENSE_PACKETBANKS["RuntimeHypnotizedPresentBox"] = new TowerDefensePacketBankData
		{
			category = new Dictionary { ["Zombie"] = new Godot.Collections.Array { "ZombieNormal" } }
		};
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
		TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefensePacketConfig);
		instance.TOWERDEFENSE_PACKETS[key] = towerDefensePacketConfig;
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
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(packedScene);
		instance.TOWERDEFENSE_CHARCATERS[key] = packedScene;
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
		if (_packetBankWasMissing)
		{
			instance.TOWERDEFENSE_PACKETBANKS.Remove("RuntimeHypnotizedPresentBox");
		}
		else
		{
			instance.TOWERDEFENSE_PACKETBANKS["RuntimeHypnotizedPresentBox"] = _previousPacketBank;
		}
	}

	private static TowerDefenseZombie FindGeneratedZombie(Node parent, TowerDefenseZombie presentBox)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			return null;
		}
		foreach (Node child in parent.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie != presentBox && towerDefenseZombie.config?.name == "ZombieNormal")
			{
				return towerDefenseZombie;
			}
		}
		return null;
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
			GD.PushError("[BugOverviewHypnotizedPresentBoxSpawnRuntimeTest] " + message);
		}
	}
}
