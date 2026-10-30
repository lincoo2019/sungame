using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugDepartmentWaveVehicleEntryFireRuntimeTest : Node
{
	private readonly record struct VehicleCase(string Label, string CharacterName, string PacketPath, string ScenePath, Type CharacterType, int ProjectileCount, string PayloadCharacterName);

	private static readonly VehicleCase[] VehicleCases = new VehicleCase[2]
	{
		new VehicleCase("Balloonpult", "ZombieBalloonpult", "res://Asset/Anime/Character/Zombie/Chapter5/Balloonpult/Packet/ZombieBalloonpult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Balloonpult/Scene/TowerDefenseZombieBalloonpult.tscn", typeof(TowerDefenseZombieBalloonpult), 10, "ZombieBalloonBomb"),
		new VehicleCase("Imppult", "ZombieImppult", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Packet/ZombieImppult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn", typeof(TowerDefenseZombieImppult), 4, "ZombieImp")
	};

	private static readonly (string Name, string PacketPath, string ScenePath)[] PayloadFixtures = new(string, string, string)[2]
	{
		("ZombieBalloonBomb", "res://Asset/Anime/Character/Zombie/Chapter5/BalloonBomb/Packet/ZombieBalloonBomb.tres", "res://Asset/Anime/Character/Zombie/Chapter5/BalloonBomb/Scene/TowerDefenseZombieBalloonBomb.tscn"),
		("ZombieImp", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Packet/Base/ZombieImp.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Imp/Scene/Base/TowerDefenseZombieImp.tscn")
	};

	private const int SpawnLine = 2;

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		WaveVehicleEntryFireControlStub control = null;
		TowerDefenseInGameLevelControl levelControl = null;
		TowerDefenseMapControl mapControl = null;
		Node2D mapIceCap = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseBattleFeatureWave waveFeature = null;
		try
		{
			_ = 5;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_010b;
				}
				RegisterRealFixtures();
				control = new WaveVehicleEntryFireControlStub
				{
					Name = "WaveVehicleEntryFireControl",
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
				levelControl = (control.levelControl = new TowerDefenseInGameLevelControl
				{
					awardCreate = false
				});
				TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
				{
					gridNum = new Vector2I(9, 5),
					gridBeginPos = new Vector2(256f, 45f),
					gridSize = new Vector2(80f, 98f),
					edge = new Vector4(200f, 0f, 1100f, 600f)
				};
				manager.currentControl = control;
				manager.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
				manager.gridSize = towerDefenseMapConfig.gridSize;
				manager.gridNum = towerDefenseMapConfig.gridNum;
				mapControl = new TowerDefenseMapControl
				{
					Name = "MapControl"
				};
				mapIceCap = new Node2D
				{
					Name = "MapIceCap"
				};
				AddChild(mapIceCap, forceReadableName: false, InternalMode.Disabled);
				mapControl.mapIceCap = mapIceCap;
				mapFeature = CreateMapFeature(mapControl, towerDefenseMapConfig);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				waveFeature = CreateWaveFeature(control, mapFeature);
				control.featureDictionary[new StringName("Wave")] = waveFeature;
				control.isGameRunning = true;
				await waveFeature.SpawnZombie(0);
				await WaitFrames(8);
				Check(waveFeature.currentCharacter.Count == VehicleCases.Length, $"The real Wave feature must track exactly both authored vehicles; actual={waveFeature.currentCharacter.Count}.");
				int num = 0;
				foreach (Node child in control.characterNode.GetChildren())
				{
					if (child is TowerDefenseZombie towerDefenseZombie && (towerDefenseZombie.config?.name == "ZombieBalloonpult" || towerDefenseZombie.config?.name == "ZombieImppult"))
					{
						num++;
					}
				}
				Check(num == VehicleCases.Length, $"Wave SpawnZombie must add both production scenes to CharacterNode; actual={num}.");
				System.Collections.Generic.Dictionary<string, TowerDefenseZombie> spawned = new System.Collections.Generic.Dictionary<string, TowerDefenseZombie>(StringComparer.Ordinal);
				foreach (TowerDefenseCharacter item in waveFeature.currentCharacter)
				{
					if (item is TowerDefenseZombie towerDefenseZombie2 && !string.IsNullOrEmpty(towerDefenseZombie2.config?.name))
					{
						spawned[towerDefenseZombie2.config.name] = towerDefenseZombie2;
					}
				}
				VehicleCase[] vehicleCases = VehicleCases;
				foreach (VehicleCase vehicleCase in vehicleCases)
				{
					VerifySpawnedVehicle(manager, spawned, vehicleCase);
				}
				System.Collections.Generic.Dictionary<string, float> outsideCooldowns = new System.Collections.Generic.Dictionary<string, float>(StringComparer.Ordinal);
				vehicleCases = VehicleCases;
				for (int i = 0; i < vehicleCases.Length; i++)
				{
					VehicleCase vehicleCase2 = vehicleCases[i];
					if (spawned.TryGetValue(vehicleCase2.CharacterName, out var value))
					{
						FireComponent obj = value.componentManager?.GetRuntime<FireComponent>("character.fire");
						outsideCooldowns[vehicleCase2.CharacterName] = obj?.timer ?? (0f / 0f);
					}
				}
				await WaitFrames(20);
				vehicleCases = VehicleCases;
				for (int i = 0; i < vehicleCases.Length; i++)
				{
					VehicleCase vehicleCase3 = vehicleCases[i];
					if (!spawned.TryGetValue(vehicleCase3.CharacterName, out var value2))
					{
						continue;
					}
					FireComponent fireComponent = value2.componentManager?.GetRuntime<FireComponent>("character.fire");
					float valueOrDefault = outsideCooldowns.GetValueOrDefault(vehicleCase3.CharacterName, 0f / 0f);
					BugDepartmentWaveVehicleEntryFireRuntimeTest bugDepartmentWaveVehicleEntryFireRuntimeTest = this;
					int condition;
					if (!value2.IsInsideComponentBattlefield)
					{
						float? num2 = fireComponent?.timer;
						if (num2.HasValue)
						{
							float valueOrDefault2 = num2.GetValueOrDefault();
							condition = ((Math.Abs(valueOrDefault2 - valueOrDefault) < 0.001f) ? 1 : 0);
							goto IL_07dc;
						}
					}
					condition = 0;
					goto IL_07dc;
					IL_07dc:
					bugDepartmentWaveVehicleEntryFireRuntimeTest.Check((byte)condition != 0, $"{vehicleCase3.Label} must move in while keeping its full firing cooldown frozen; initial={valueOrDefault:F3}, current={fireComponent?.timer:F3}, x={value2.GetLogicalGlobalPosition().X:F3}.");
				}
				System.Collections.Generic.Dictionary<string, int> entryFrames = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
				System.Collections.Generic.Dictionary<string, float> entryCooldowns = new System.Collections.Generic.Dictionary<string, float>(StringComparer.Ordinal);
				System.Collections.Generic.Dictionary<string, int> acquisitionFrames = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
				for (int frame = 0; frame < 900; frame++)
				{
					bool flag = true;
					vehicleCases = VehicleCases;
					for (int i = 0; i < vehicleCases.Length; i++)
					{
						VehicleCase vehicleCase4 = vehicleCases[i];
						if (!spawned.TryGetValue(vehicleCase4.CharacterName, out var value3))
						{
							flag = false;
							continue;
						}
						CatapultComponent obj2 = value3.componentManager?.GetRuntime<CatapultComponent>();
						FireComponent fireComponent2 = value3.componentManager?.GetRuntime<FireComponent>("character.fire");
						if (value3.IsInsideComponentBattlefield && !entryFrames.ContainsKey(vehicleCase4.CharacterName))
						{
							entryFrames[vehicleCase4.CharacterName] = frame + 1;
							entryCooldowns[vehicleCase4.CharacterName] = fireComponent2?.timer ?? (0f / 0f);
						}
						if (obj2?.isFire ?? false)
						{
							if (!acquisitionFrames.ContainsKey(vehicleCase4.CharacterName))
							{
								acquisitionFrames[vehicleCase4.CharacterName] = frame + 1;
								value3.sprite.pause = true;
							}
						}
						else
						{
							flag = false;
						}
					}
					if (flag)
					{
						break;
					}
					await WaitFrames(1);
				}
				await WaitFrames(3);
				double num3 = manager.GetMapGroundRight() - (double)manager.GetMapGridSize().X * 0.5;
				vehicleCases = VehicleCases;
				for (int i = 0; i < vehicleCases.Length; i++)
				{
					VehicleCase vehicleCase5 = vehicleCases[i];
					if (spawned.TryGetValue(vehicleCase5.CharacterName, out var value4))
					{
						CatapultComponent catapultComponent = value4.componentManager?.GetRuntime<CatapultComponent>();
						int valueOrDefault3 = entryFrames.GetValueOrDefault(vehicleCase5.CharacterName);
						int valueOrDefault4 = acquisitionFrames.GetValueOrDefault(vehicleCase5.CharacterName);
						float valueOrDefault5 = entryCooldowns.GetValueOrDefault(vehicleCase5.CharacterName, 0f / 0f);
						Check(valueOrDefault3 > 0 && valueOrDefault5 > 0f, $"{vehicleCase5.Label} must cross the entry line with a positive full cooldown; entryFrame={valueOrDefault3}, timer={valueOrDefault5:F3}.");
						Check(catapultComponent != null && catapultComponent.isFire && valueOrDefault4 > valueOrDefault3 && valueOrDefault4 <= 900, $"{vehicleCase5.Label} must begin its first throw only after its post-entry cooldown; entryFrame={valueOrDefault3}, fireFrame={valueOrDefault4}, state={value4.CurrentStateHandle?.StableId ?? "<none>"}, timer={value4.componentManager?.GetRuntime<FireComponent>("character.fire")?.timer:F3}, canStart={catapultComponent?.CanStartFire()}.");
						Check(value4.CurrentStateHandle?.StableId != "zombie.walk", vehicleCase5.Label + " must leave Walk after its entry cooldown expires.");
						Check((double)value4.GetLogicalGlobalPosition().X <= num3 + 0.01, $"{vehicleCase5.Label} must not start throwing before crossing the entry line; x={value4.GetLogicalGlobalPosition().X:F3}, entry={num3:F3}.");
					}
				}
				VehicleCase[] vehicleCases2 = VehicleCases;
				foreach (VehicleCase vehicleCase6 in vehicleCases2)
				{
					await VerifyRepeatedRealPayload(spawned, vehicleCase6);
				}
				goto end_IL_00e8;
				end_IL_010b:;
			}
			catch (Exception value5)
			{
				_failures++;
				GD.PushError($"[BugDepartmentWaveVehicleEntryFireRuntimeTest] Unexpected exception: {value5}");
				goto end_IL_00e8;
			}
			return;
			end_IL_00e8:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(control))
			{
				control.isGameRunning = false;
				if (GodotObject.IsInstanceValid(control.characterNode))
				{
					foreach (Node child2 in control.characterNode.GetChildren())
					{
						if (GodotObject.IsInstanceValid(child2) && !child2.IsQueuedForDeletion())
						{
							child2.QueueFree();
						}
					}
				}
			}
			await WaitFrames(4);
			waveFeature?.Destroy();
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			if (GodotObject.IsInstanceValid(mapIceCap) && !mapIceCap.IsQueuedForDeletion())
			{
				mapIceCap.QueueFree();
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(levelControl))
			{
				levelControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			await WaitFrames(8);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			await WaitFrames(2);
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag2 = _failures == 0 && _checks == 34;
		GD.Print($"WAVE_VEHICLE_ENTRY_FIRE_RESULT passed={flag2} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag2) ? 2 : 0);
	}

	private void VerifySpawnedVehicle(TowerDefenseManager manager, System.Collections.Generic.Dictionary<string, TowerDefenseZombie> spawned, VehicleCase vehicleCase)
	{
		spawned.TryGetValue(vehicleCase.CharacterName, out var value);
		Check(GodotObject.IsInstanceValid(value) && value.config?.name == vehicleCase.CharacterName, "Wave SpawnZombie must instantiate real " + vehicleCase.Label + " packet data.");
		Check(GodotObject.IsInstanceValid(value) && vehicleCase.CharacterType.IsInstanceOfType(value), "Wave SpawnZombie must instantiate the production " + vehicleCase.CharacterType.Name + " scene.");
		CatapultComponent catapultComponent = value?.componentManager?.GetRuntime<CatapultComponent>();
		FireComponent fireComponent = value?.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(catapultComponent != null && !catapultComponent.IsReleased && fireComponent != null && !fireComponent.IsReleased, vehicleCase.Label + " must bind its authored Catapult and Fire runtimes.");
		Check(catapultComponent != null && !catapultComponent.useCanFireCheck && catapultComponent.projectileNum == vehicleCase.ProjectileCount && catapultComponent.currentProjectileNum == vehicleCase.ProjectileCount, vehicleCase.Label + " must retain its authored automatic throw and ammo configuration.");
		Check(GodotObject.IsInstanceValid(value) && !value.IsInsideComponentBattlefield && (double)value.GetLogicalGlobalPosition().X > manager.GetMapGroundRight(), vehicleCase.Label + " must be observed at the real Wave entry position outside the battlefield.");
	}

	private async Task VerifyRepeatedRealPayload(System.Collections.Generic.Dictionary<string, TowerDefenseZombie> spawned, VehicleCase vehicleCase)
	{
		if (!spawned.TryGetValue(vehicleCase.CharacterName, out var zombie) || !GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		CatapultComponent catapult = zombie.componentManager?.GetRuntime<CatapultComponent>();
		FireComponent fire = zombie.componentManager?.GetRuntime<FireComponent>("character.fire");
		if ((catapult?.IsReleased ?? true) || (fire?.IsReleased ?? true))
		{
			return;
		}
		bool previousPause = zombie.sprite.pause;
		zombie.sprite.pause = true;
		try
		{
			int initialAmmo = catapult.currentProjectileNum;
			zombie.AnimeEvent("fire", default(Variant));
			await WaitFrames(5);
			Check(catapult.currentProjectileNum == initialAmmo - 1, vehicleCase.Label + " first production animation event must consume exactly one payload.");
			Check(CountLiveCharacters(vehicleCase.PayloadCharacterName) >= 1, vehicleCase.Label + " first throw must create real " + vehicleCase.PayloadCharacterName + " gameplay payload.");
			zombie.AnimeCompleted("Fire");
			fire.timer = 0f;
			await WaitFrames(5);
			Check(catapult.isFire && !catapult.fireOver, vehicleCase.Label + " must stay operational and schedule another throw after the first cycle.");
			zombie.AnimeEvent("fire", default(Variant));
			await WaitFrames(5);
			Check(catapult.currentProjectileNum == initialAmmo - 2, vehicleCase.Label + " second production animation event must consume the next payload.");
			Check(CountLiveCharacters(vehicleCase.PayloadCharacterName) >= 2, vehicleCase.Label + " must create a second real " + vehicleCase.PayloadCharacterName + " payload instead of stopping.");
		}
		finally
		{
			zombie.sprite.pause = previousPause;
		}
	}

	private int CountLiveCharacters(string characterName)
	{
		int num = 0;
		foreach (Node item in GetTree().GetNodesInGroup(characterName))
		{
			if (item is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.IsQueuedForDeletion() && towerDefenseCharacter.config?.name == characterName)
			{
				num++;
			}
		}
		return num;
	}

	private static TowerDefenseBattleFeatureWave CreateWaveFeature(WaveVehicleEntryFireControlStub control, TowerDefenseBattleFeatureMap mapFeature)
	{
		TowerDefenseLevelWaveConfig towerDefenseLevelWaveConfig = new TowerDefenseLevelWaveConfig();
		VehicleCase[] vehicleCases = VehicleCases;
		foreach (VehicleCase vehicleCase in vehicleCases)
		{
			towerDefenseLevelWaveConfig.spawn.Add(new TowerDefenseLevelSpawnConfig
			{
				zombie = vehicleCase.CharacterName,
				line = 2,
				num = 1
			});
		}
		TowerDefenseLevelWaveManagerConfig towerDefenseLevelWaveManagerConfig = new TowerDefenseLevelWaveManagerConfig
		{
			flagZombieUse = false,
			spawnMaxCharactersPerFrame = 8,
			spawnFrameBudgetMilliseconds = 16.0
		};
		towerDefenseLevelWaveManagerConfig.wave.Add(towerDefenseLevelWaveConfig);
		return new TowerDefenseBattleFeatureWave
		{
			control = control,
			levelControl = control.levelControl,
			mapFeature = mapFeature,
			config = towerDefenseLevelWaveManagerConfig,
			currentDynamic = new TowerDefenseLevelDynamicConfig()
		};
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, TowerDefenseMapConfig mapConfig)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = mapConfig
		});
		towerDefenseBattleFeatureMap.plantGrid.Resize(mapConfig.gridNum.X + 1);
		for (int i = 0; i <= mapConfig.gridNum.X; i++)
		{
			Godot.Collections.Array array = new Godot.Collections.Array();
			array.Resize(mapConfig.gridNum.Y + 1);
			for (int j = 1; j <= mapConfig.gridNum.Y; j++)
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
		towerDefenseBattleFeatureMap.lineUse.Resize(mapConfig.gridNum.Y + 1);
		for (int k = 0; k <= mapConfig.gridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = k == 2;
		}
		towerDefenseBattleFeatureMap.iceCapList.Resize(mapConfig.gridNum.Y + 1);
		return towerDefenseBattleFeatureMap;
	}

	private void RegisterRealFixtures()
	{
		VehicleCase[] vehicleCases = VehicleCases;
		for (int i = 0; i < vehicleCases.Length; i++)
		{
			VehicleCase vehicleCase = vehicleCases[i];
			RegisterPacket(vehicleCase.CharacterName, vehicleCase.PacketPath);
			RegisterCharacter(vehicleCase.CharacterName, vehicleCase.ScenePath);
		}
		(string, string, string)[] payloadFixtures = PayloadFixtures;
		for (int i = 0; i < payloadFixtures.Length; i++)
		{
			(string, string, string) tuple = payloadFixtures[i];
			RegisterPacket(tuple.Item1, tuple.Item2);
			RegisterCharacter(tuple.Item1, tuple.Item3);
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
			GD.PushError("[BugDepartmentWaveVehicleEntryFireRuntimeTest] " + message);
		}
	}
}
