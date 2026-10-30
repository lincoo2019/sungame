using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class EventInitBungiZeroPacketRuntimeTest : Node
{
	private const string SourceLevelPath = "res://Asset/Config/Level/TowerDefense/Test/LevelTest.tres";

	private const string NormalPacketPath = "uid://b2brg7mubg1b6";

	private const string NormalScenePath = "uid://b1dgft6pefocm";

	private const string ImitaterPacketPath = "uid://d2mw2xscdafep";

	private const string ImitaterScenePath = "uid://c3ejmxpkruorn";

	private const string NormalPacketName = "ZombieNormal";

	private const string ImitaterPacketName = "ZombieNormalLmitater";

	private int _checks;

	private int _failures;

	private EventInitBungiZeroPacketRuntimeControlStub _control;

	private TowerDefenseInGameLevelControl _levelControl;

	private TowerDefenseBattleFeatureMap _mapFeature;

	private TowerDefenseMapControl _mapControl;

	private TowerDefenseBattleFeatureWave _waveFeature;

	private TowerDefenseBattleFeatureEvent _eventFeature;

	private readonly List<string> _characterLifecycle = new List<string>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseLevelConfig source = null;
		TowerDefenseBattleFeaturePacketBank packetBankFeature = null;
		TowerDefenseInGamePacketBank packetBank = null;
		TowerDefenseInGameSeedBank seedBank = null;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		TowerDefenseLevelBaseConfig previousLevel = manager?.currentLevelConfig;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		string previousEntryMode = Global.Instance?.enterLevelMode ?? "";
		bool previousMultiplayer = Global.IsMultiplayerMode;
		bool previousDebugNoZombieSpawn = CommandManager.Instance?.debugNoZombieSpawn ?? false;
		try
		{
			_ = 7;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance) && GodotObject.IsInstanceValid(GameSaveManager.Instance) && GodotObject.IsInstanceValid(CommandManager.Instance), "The gameplay autoloads required by the real fixture must be available.");
				if (_failures > 0)
				{
					goto end_IL_0163;
				}
				GameSaveManager.Instance.EnsureLoaded();
				if (string.IsNullOrEmpty(GameSaveManager.Instance.EnsureUser()))
				{
					GameSaveManager.Instance.SetUserCurrent("EventInitBungiZeroPacketRuntime");
				}
				Global.Instance.isMultiplayerMode = false;
				CommandManager.Instance.debugNoZombieSpawn = false;
				GameSaveManager.Instance.SetConfigValue("Backgrounder", true);
				await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
				RegisterRealFixtures();
				TowerDefensePacketConfig packetConfigReadOnly = TowerDefenseManager.GetPacketConfigReadOnly("ZombieNormalLmitater");
				Check(GodotObject.IsInstanceValid(packetConfigReadOnly) && packetConfigReadOnly.characterConfig is TowerDefenseZombieConfig { weight: >0 } && GodotObject.IsInstanceValid(ResourceManager.Instance.GetCharacterScene("ZombieNormalLmitater")), "The real ZombieNormalLmitater packet, weighted zombie config and scene must be registered.");
				source = ResourceLoader.Load<TowerDefenseLevelConfig>("res://Asset/Config/Level/TowerDefense/Test/LevelTest.tres", null, ResourceLoader.CacheMode.IgnoreDeep);
				Check(GodotObject.IsInstanceValid(source), "The runtime fixture must load the real TestLevel resource.");
				if (!GodotObject.IsInstanceValid(source))
				{
					goto end_IL_0163;
				}
				source.Init();
				Check(source.name == "LevelTest" && source.packetBankMethod == TowerDefenseEnum.LEVEL_SEEDBANK_METHOD.CHOOSE && source.eventInit.Count == 1 && source.eventInit[0] is TowerDefenseLevelEventBungiSpawnZombie, "The source fixture must retain TestLevel's choose mode and authored EventInit bungee event.");
				Check(GodotObject.IsInstanceValid(source.waveManager) && source.waveManager.wave.Count > 0 && source.waveManager.wave[0].spawn.Count == 1 && source.waveManager.wave[0].spawn[0].zombie == "ZombieNormal" && source.waveManager.wave[0].spawn[0].num == 1, "The source fixture must retain TestLevel's authored first ZombieNormal wave.");
				SetupBattleFixture(manager, source);
				await WaitFrames(2);
				_eventFeature = new TowerDefenseBattleFeatureEvent
				{
					control = _control
				};
				_control.featureDictionary[new StringName("Event")] = _eventFeature;
				_eventFeature.Init(GetFeatureData(source, "Event"));
				Check(_eventFeature.eventInit.Count == 1 && _eventFeature.eventInit[0] is TowerDefenseLevelEventBungiSpawnZombie, "The live Event feature must deserialize the real EventInit bungee event.");
				await _eventFeature.GameInit();
				Check(_waveFeature.HasPendingSpawnOperations, "Event.GameInit must synchronously start a tracked real bungee spawn operation.");
				_waveFeature.Refresh();
				Check(_waveFeature.HasPendingSpawnOperations, "Wave.Refresh must preserve the EventInit bungee operation that is still landing.");
				bool flag = await WaitUntil(() => FindLandedZombie("ZombieNormalLmitater") != null, 360);
				TowerDefenseZombie eventPayload = FindLandedZombie("ZombieNormalLmitater");
				Check(flag && GodotObject.IsInstanceValid(eventPayload), "A real ZombieNormalLmitater payload must land after Wave.Refresh. " + DescribeCharacterNode());
				EventInitBungiZeroPacketRuntimeTest eventInitBungiZeroPacketRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(eventPayload) && eventPayload.isGround && eventPayload.instance.canBeCollection)
				{
					AttackComponent attackComponent = eventPayload.attackComponent;
					condition = ((attackComponent != null && !attackComponent.IsReleased && attackComponent.alive) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				eventInitBungiZeroPacketRuntimeTest.Check((byte)condition != 0, "The EventInit payload must finish the real bungee placement lifecycle. " + DescribeCharacterNode());
				seedBank = new TowerDefenseInGameSeedBank
				{
					packetNum = 0
				};
				packetBank = new TowerDefenseInGamePacketBank
				{
					seedBank = seedBank
				};
				packetBankFeature = new TowerDefenseBattleFeaturePacketBank
				{
					control = _control,
					packetBank = packetBank
				};
				int chooseOverCount = 0;
				packetBankFeature.OnChooseOver += delegate
				{
					chooseOverCount++;
				};
				Global.Instance.enterLevelMode = "DiyLevel";
				packetBankFeature.RockButtonPressed();
				await WaitFrames(2);
				Check(chooseOverCount == 0, "An empty ordinary card-selection screen must still reject confirmation.");
				Global.Instance.enterLevelMode = "LevelTest";
				packetBankFeature.RockButtonPressed();
				Check(seedBank.packetNum == 0 && chooseOverCount == 1, "LevelTest must emit ChooseOver exactly once when the real seed bank has zero cards.");
				_control.isGameRunning = chooseOverCount == 1;
				await _waveFeature.SpawnZombie(0);
				bool flag2 = await WaitUntil(() => FindLandedZombie("ZombieNormal") != null, 90);
				TowerDefenseZombie towerDefenseZombie = FindLandedZombie("ZombieNormal");
				Check(flag2 && towerDefenseZombie is TowerDefenseZombieNormal, "After zero-card confirmation, the real Wave feature must instantiate TestLevel's first ZombieNormal.");
				Check(GodotObject.IsInstanceValid(towerDefenseZombie) && towerDefenseZombie.gridPos.Y == 3 && towerDefenseZombie.instance.canBeCollection, "The first-wave zombie must use TestLevel's authored lane and enter live target collection.");
				Check(await WaitUntil(() => GodotObject.IsInstanceValid(eventPayload) && eventPayload is TowerDefenseZombieNormalImitater towerDefenseZombieNormalImitater && towerDefenseZombieNormalImitater.over, 90), "The live ZombieNormalLmitater must enter its transformation after an imitable allied zombie appears. " + DescribeCharacterNode());
				goto end_IL_0138;
				end_IL_0163:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[EventInitBungiZeroPacketRuntimeTest] Unexpected exception: {value}");
				goto end_IL_0138;
			}
			return;
			end_IL_0138:;
		}
		finally
		{
			packetBankFeature?.CancelLifetime();
			packetBankFeature?.Dispose();
			if (GodotObject.IsInstanceValid(packetBank))
			{
				packetBank.Free();
			}
			if (GodotObject.IsInstanceValid(seedBank))
			{
				seedBank.Free();
			}
			_eventFeature?.Destroy();
			_eventFeature?.Dispose();
			_eventFeature = null;
			_waveFeature?.Destroy();
			_waveFeature?.Dispose();
			_waveFeature = null;
			_mapFeature?.Destroy();
			_mapFeature?.Dispose();
			_mapFeature = null;
			if (GodotObject.IsInstanceValid(_mapControl))
			{
				_mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(_levelControl))
			{
				_levelControl.Free();
			}
			if (GodotObject.IsInstanceValid(_control))
			{
				_control.QueueFree();
			}
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.currentLevelConfig = previousLevel;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			RestoreRealFixtures();
			if (GodotObject.IsInstanceValid(Global.Instance))
			{
				Global.Instance.enterLevelMode = previousEntryMode;
				Global.Instance.isMultiplayerMode = previousMultiplayer;
			}
			if (GodotObject.IsInstanceValid(CommandManager.Instance))
			{
				CommandManager.Instance.debugNoZombieSpawn = previousDebugNoZombieSpawn;
			}
			source?.Dispose();
			GC.Collect();
			GC.WaitForPendingFinalizers();
			await WaitFrames(4);
		}
		bool flag3 = _failures == 0;
		GD.Print($"EVENT_INIT_BUNGI_ZERO_PACKET_RESULT passed={flag3} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag3) ? 2 : 0);
	}

	private void SetupBattleFixture(TowerDefenseManager manager, TowerDefenseLevelConfig source)
	{
		_control = new EventInitBungiZeroPacketRuntimeControlStub
		{
			Name = "EventInitBungiZeroPacketRuntimeControl",
			isGameRunning = false,
			isInit = true,
			levelConfig = source
		};
		AddChild(_control, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode = new Node2D
		{
			Name = "CharacterNode"
		};
		_control.AddChild(_control.characterNode, forceReadableName: false, InternalMode.Disabled);
		_control.characterNode.ChildEnteredTree += OnCharacterEntered;
		_levelControl = new TowerDefenseInGameLevelControl
		{
			awardCreate = false
		};
		_control.levelControl = _levelControl;
		manager.currentControl = _control;
		manager.currentLevelConfig = source;
		manager.gridBeginPos = Vector2.Zero;
		manager.gridSize = new Vector2(100f, 76f);
		manager.gridNum = new Vector2I(9, 5);
		_mapControl = new TowerDefenseMapControl
		{
			Name = "MapControl"
		};
		_mapFeature = CreateMapFeature(_mapControl, manager.gridNum);
		_mapFeature.control = _control;
		_control.featureDictionary[new StringName("Map")] = _mapFeature;
		TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = source.waveManager.wave[0].Duplicate(deep: true) as TowerDefenseLevelWaveConfig;
		towerDefenseLevelWaveConfig.gridSpawn.Clear();
		towerDefenseLevelWaveConfig.eventList.Clear();
		TowerDefenseLevelWaveManagerConfig towerDefenseLevelWaveManagerConfig = new TowerDefenseLevelWaveManagerConfig
		{
			flagZombieUse = false,
			spawnFrameBudgetMilliseconds = 16.0,
			spawnMaxCharactersPerFrame = 8
		};
		towerDefenseLevelWaveManagerConfig.wave.Add(towerDefenseLevelWaveConfig);
		_waveFeature = new TowerDefenseBattleFeatureWave
		{
			control = _control,
			levelControl = _levelControl,
			mapFeature = _mapFeature,
			config = towerDefenseLevelWaveManagerConfig,
			currentDynamic = new TowerDefenseLevelDynamicConfig()
		};
		_control.featureDictionary[new StringName("Wave")] = _waveFeature;
		_waveFeature.OnReady();
	}

	private TowerDefenseZombie FindLandedZombie(string configName)
	{
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return null;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && !(child is TowerDefenseZombieBungiSpawn) && towerDefenseZombie.config?.name == configName && towerDefenseZombie.isGround)
			{
				return towerDefenseZombie;
			}
		}
		return null;
	}

	private string DescribeCharacterNode()
	{
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return "characterNode=<invalid>";
		}
		List<string> list = new List<string>();
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn)
			{
				list.Add($"carrier(name={towerDefenseZombieBungiSpawn.characterName}, state={towerDefenseZombieBungiSpawn.CurrentStateHandle?.StableId ?? "<null>"}, z={towerDefenseZombieBungiSpawn.z:F1}, payload={towerDefenseZombieBungiSpawn.character?.config?.name ?? "<null>"})");
			}
			else if (child is TowerDefenseZombie towerDefenseZombie)
			{
				list.Add($"zombie(config={towerDefenseZombie.config?.name ?? "<null>"}, ground={towerDefenseZombie.isGround}, collect={towerDefenseZombie.instance?.canBeCollection}, state={towerDefenseZombie.CurrentStateHandle?.StableId ?? "<null>"})");
			}
			else
			{
				list.Add(child.GetType().Name);
			}
		}
		return $"pendingWave={_waveFeature?.HasPendingSpawnOperations}, pendingBattle={_control?.HasPendingBattleOperations}, children=[{string.Join("; ", list)}], lifecycle=[{string.Join("; ", _characterLifecycle)}]";
	}

	private void OnCharacterEntered(Node child)
	{
		string item = ((child is TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn) ? ("enter-carrier(" + towerDefenseZombieBungiSpawn.characterName + ")") : ((child is TowerDefenseZombie towerDefenseZombie) ? ("enter-zombie(" + (towerDefenseZombie.config?.name ?? child.Name.ToString()) + ")") : ("enter-" + child.GetType().Name)));
		_characterLifecycle.Add(item);
		child.TreeExiting += delegate
		{
			string item2 = ((child is TowerDefenseZombieBungiSpawn towerDefenseZombieBungiSpawn2) ? $"exit-carrier({towerDefenseZombieBungiSpawn2.characterName}, payload={towerDefenseZombieBungiSpawn2.character?.config?.name ?? "<null>"})" : ((child is TowerDefenseZombie towerDefenseZombie2) ? $"exit-zombie({towerDefenseZombie2.config?.name ?? child.Name.ToString()}, ground={towerDefenseZombie2.isGround})" : ("exit-" + child.GetType().Name)));
			_characterLifecycle.Add(item2);
		};
	}

	private static Dictionary GetFeatureData(TowerDefenseLevelConfig level, string featureName)
	{
		if (!level.featureData.TryGetValue(new StringName(featureName), out var value))
		{
			return new Dictionary();
		}
		return value.Duplicate(deep: true);
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
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig
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
		towerDefenseBattleFeatureMap.lineUse.Resize(gridNum.Y + 1);
		for (int k = 1; k <= gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("ZombieNormal", "uid://b2brg7mubg1b6");
		RegisterPacket("ZombieNormalLmitater", "uid://d2mw2xscdafep");
		RegisterCharacter("ZombieNormal", "uid://b1dgft6pefocm");
		RegisterCharacter("ZombieNormalLmitater", "uid://c3ejmxpkruorn");
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
		_previousPackets.Clear();
		_previousCharacters.Clear();
		_missingPackets.Clear();
		_missingCharacters.Clear();
	}

	private async Task<bool> WaitUntil(Func<bool> predicate, int maximumFrames)
	{
		for (int frame = 0; frame < maximumFrames; frame++)
		{
			if (predicate())
			{
				return true;
			}
			await WaitFrames(1);
		}
		return predicate();
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
			GD.PushError("[EventInitBungiZeroPacketRuntimeTest] " + message);
		}
	}
}
