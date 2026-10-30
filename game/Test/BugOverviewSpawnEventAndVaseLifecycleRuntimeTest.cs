using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewSpawnEventAndVaseLifecycleRuntimeTest : Node
{
	private const string MapControlScenePath = "res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn";

	private const string SunflowerPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres";

	private const string SunflowerScenePath = "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn";

	private const string NormalZombiePacketPath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string VasePacketPath = "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres";

	private const string VaseScenePath = "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn";

	private const string BossPacketPath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres";

	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn";

	private int _i9Checks;

	private int _i9Failures;

	private int _i10Checks;

	private int _i10Failures;

	private int _i11Checks;

	private int _i11Failures;

	private int _jackboxChecks;

	private int _jackboxFailures;

	private TowerDefenseControlNew _control;

	private TowerDefenseBattleFeatureMap _mapFeature;

	public override async void _Ready()
	{
		try
		{
			_ = 4;
			try
			{
				await SetupRealBattleFixture();
				await VerifyI9SpawnEventPlantStillOccupiesItsCell();
				await VerifyI10VaseDeathSpawnLifecycleAndSettlement();
				await VerifyI11ExplosionBreaksVasesWithoutDeepCopy();
				await VerifyJackboxExplosionReleasesVaseContents();
			}
			catch (Exception value)
			{
				_i9Failures++;
				_i10Failures++;
				_i11Failures++;
				_jackboxFailures++;
				GD.PushError($"[BugOverviewSpawnEventAndVaseLifecycleRuntimeTest] Unexpected exception: {value}");
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
		bool flag = _i9Failures == 0;
		bool flag2 = _i10Failures == 0;
		bool flag3 = _i11Failures == 0;
		bool flag4 = _jackboxFailures == 0 && _jackboxChecks == 10;
		GD.Print($"BUG_OVERVIEW_I9_SPAWN_EVENT_OCCUPANCY_RESULT passed={flag} checks={_i9Checks} failures={_i9Failures}");
		GD.Print($"BUG_OVERVIEW_I10_VASE_DEATH_SPAWN_RESULT passed={flag2} checks={_i10Checks} failures={_i10Failures}");
		GD.Print($"VASE_EXPLOSION_RUNTIME_COPY_RESULT passed={flag3} checks={_i11Checks} failures={_i11Failures}");
		GD.Print($"JACKBOX_VASE_CONTENT_RESULT passed={flag4} checks={_jackboxChecks} failures={_jackboxFailures}");
		GetTree().Quit((!(flag && flag2 && flag3 && flag4)) ? 2 : 0);
	}

	private async Task SetupRealBattleFixture()
	{
		await ResourceManager.Instance.EnsureFullGameplayResourcesReadyAsync();
		RegisterFixtures();
		_control = new BugOverviewSpawnEventAndVaseRuntimeControlStub
		{
			Name = "SpawnEventAndVaseRuntimeControl",
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
		_control.levelControl = new BugOverviewSpawnEventAndVaseLevelControlStub
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
		TowerDefenseMapConfig towerDefenseMapConfig = ResourceLoader.Load<TowerDefenseMapConfig>("res://Asset/Config/Map/Frontlawn/Config/FrontlawnMapFrontlawn.tres", null, ResourceLoader.CacheMode.Ignore);
		instance.gridNum = towerDefenseMapConfig.gridNum;
		instance.gridBeginPos = towerDefenseMapConfig.gridBeginPos;
		instance.gridSize = towerDefenseMapConfig.gridSize;
		TowerDefenseMapControl towerDefenseMapControl = ResourceLoader.Load<PackedScene>("res://Registry/Battle/Feature/Map/Control/TowerDefenseMapControl.tscn", null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseMapControl>(PackedScene.GenEditState.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseMapControl))
		{
			throw new InvalidOperationException("The real TowerDefenseMapControl scene did not instantiate.");
		}
		_mapFeature = new TowerDefenseBattleFeatureMap
		{
			control = _control,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			mapControl = towerDefenseMapControl
		};
		towerDefenseMapControl.mapFeature = _mapFeature;
		_control.featureDictionary["Map"] = _mapFeature;
		_control.AddChild(towerDefenseMapControl, forceReadableName: false, InternalMode.Disabled);
		if (!_mapFeature.MapInit(towerDefenseMapConfig))
		{
			throw new InvalidOperationException("真实前院地图初始化失败。");
		}
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
	}

	private async Task VerifyI9SpawnEventPlantStillOccupiesItsCell()
	{
		TowerDefenseLevelConfig towerDefenseLevelConfig = _control.levelConfig as TowerDefenseLevelConfig;
		towerDefenseLevelConfig.finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM;
		towerDefenseLevelConfig.izmManager = new TowerDefenseLevelIZMManagerConfig
		{
			shuffle = true
		};
		TowerDefenseBattleProcessIZM process = new TowerDefenseBattleProcessIZM
		{
			control = _control,
			mapFeature = _mapFeature
		};
		process.Init(new Dictionary
		{
			["Shuffle"] = true,
			["PreSpawnMaxRetryPasses"] = 8
		});
		_control.process = process;
		TowerDefenseCharacterOverride towerDefenseCharacterOverride = new TowerDefenseCharacterOverride();
		towerDefenseCharacterOverride.spawnEvent.Add(new TowerDefenseCharacterEventPacketSpawn
		{
			packetName = "ZombieNormal",
			percentage = 1.0
		});
		TowerDefenseLevelPreSpawnConfig towerDefenseLevelPreSpawnConfig = new TowerDefenseLevelPreSpawnConfig
		{
			packetName = "PlantSunFlower",
			gridPos = new Vector2I(2, 2),
			characterOverride = towerDefenseCharacterOverride
		};
		TowerDefenseBattleFeaturePreSpawn preSpawnFeature = new TowerDefenseBattleFeaturePreSpawn
		{
			control = _control
		};
		preSpawnFeature.Init(new Dictionary { ["MaxRetryPasses"] = 1 });
		preSpawnFeature.config.preSpawnList.Add(towerDefenseLevelPreSpawnConfig);
		try
		{
			I9Check(towerDefenseLevelConfig.finishMethod == TowerDefenseEnum.LEVEL_FINISH_METHOD.IZM && towerDefenseLevelConfig.izmManager.shuffle, "The fixture must enter the real shuffled IZM pre-spawn branch.");
			I9Check(_control.process == process && process.mapFeature == _mapFeature, "The real IZM process must own the fixture map before pre-spawn runs.");
			I9Check(GodotObject.IsInstanceValid(towerDefenseLevelPreSpawnConfig.characterOverride) && towerDefenseLevelPreSpawnConfig.characterOverride.spawnEvent.Count == 1, "The zombie event must be attached to TowerDefenseLevelPreSpawnConfig.characterOverride.");
			await preSpawnFeature.GameEntry();
			I9Check(preSpawnFeature.preSpawnList.Count == 1, $"The real IZM pre-spawn chain must return exactly one Sunflower; got {preSpawnFeature.preSpawnList.Count}.");
			TowerDefenseCharacter firstPlant = ((preSpawnFeature.preSpawnList.Count == 1) ? preSpawnFeature.preSpawnList[0] : null);
			I9Check(GodotObject.IsInstanceValid(firstPlant) && !firstPlant.IsInsideTree(), "IZM GameEntry must expose the real deferred-add window before cell registration.");
			TowerDefenseCellInstance cell = null;
			for (int frame = 0; frame < 30; frame++)
			{
				if (GodotObject.IsInstanceValid(firstPlant))
				{
					cell = TowerDefenseManager.GetMapCell(firstPlant.gridPos);
				}
				if (GodotObject.IsInstanceValid(firstPlant) && firstPlant.IsNodeReady() && GodotObject.IsInstanceValid(cell) && cell.characterList.Contains(firstPlant) && FindCharacter("ZombieNormal") is TowerDefenseZombie)
				{
					break;
				}
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			I9Check(firstPlant is TowerDefensePlant && GodotObject.IsInstanceValid(cell) && cell.characterList.Contains(firstPlant), "The IZM spawn-event Sunflower must register in its shuffled real map cell.");
			I9Check(cell != null && cell.slot.TryGetValue(TowerDefenseEnum.PLANTGRIDTYPE.GROUND, out var value) && value == firstPlant, "The IZM spawn-event Sunflower must retain the real ground slot.");
			TowerDefenseZombie eventZombie = FindCharacter("ZombieNormal") as TowerDefenseZombie;
			I9Check(eventZombie != null, "The pre-spawn character override must create the real ZombieNormal.");
			Vector2 expectedShadowPosition = (eventZombie?.GlobalPosition + new Vector2(12f, 36f)) ?? Vector2.Zero;
			BugOverviewSpawnEventAndVaseLifecycleRuntimeTest bugOverviewSpawnEventAndVaseLifecycleRuntimeTest = this;
			int condition;
			if (GodotObject.IsInstanceValid(eventZombie?.shadowSprite))
			{
				ShadowComponent shadowComponent = eventZombie.shadowComponent;
				condition = ((shadowComponent != null && !shadowComponent.IsReleased) ? 1 : 0);
			}
			else
			{
				condition = 0;
			}
			bugOverviewSpawnEventAndVaseLifecycleRuntimeTest.I9Check((byte)condition != 0, "The event-spawned real ZombieNormal must own its authored shadow sprite and component.");
			I9Check(eventZombie != null && eventZombie.shadowComponent?.GetShadowPosition().IsEqualApprox(expectedShadowPosition) == true, $"PacketSpawn must recapture the real shadow baseline after moving the zombie; expected={expectedShadowPosition}, actual={eventZombie?.shadowComponent?.GetShadowPosition()}.");
			await WaitFrames(4);
			I9Check(eventZombie != null && eventZombie.shadowSprite?.GlobalPosition.IsEqualApprox(expectedShadowPosition) == true, $"The real ZombieNormal shadow must stay grounded after component physics; expected={expectedShadowPosition}, actual={eventZombie?.shadowSprite?.GlobalPosition}.");
			TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("PlantSunFlower");
			I9Check(GodotObject.IsInstanceValid(packetConfig) && !cell.CanPacketPlant(packetConfig), "After deferred registration settles, the event-bearing pre-spawn cell must reject another Sunflower.");
			TowerDefenseCharacter instance = packetConfig?.Plant(firstPlant.gridPos, playAudio: false);
			I9Check(!GodotObject.IsInstanceValid(instance), "A normal planting attempt must not stack onto the real IZM pre-spawn Sunflower.");
			await WaitFrames(2);
			I9Check(CountCharactersInCell(cell, "PlantSunFlower") == 1, $"The shuffled IZM cell must contain exactly one Sunflower; got {CountCharactersInCell(cell, "PlantSunFlower")}.");
		}
		finally
		{
			preSpawnFeature.Destroy();
			process.Destroy();
			foreach (Node child in _control.characterNode.GetChildren())
			{
				TowerDefenseCharacter towerDefenseCharacter = child as TowerDefenseCharacter;
				bool flag = towerDefenseCharacter == null;
				if (!flag)
				{
					string text = towerDefenseCharacter.config?.name;
					bool flag2 = ((text == "PlantSunFlower" || text == "ZombieNormal") ? true : false);
					flag = !flag2;
				}
				if (!flag)
				{
					_control.CleanupCharacterCell(towerDefenseCharacter);
					TowerDefenseManager.Instance.CharacterUnregister(towerDefenseCharacter);
					towerDefenseCharacter.QueueFree();
				}
			}
			await WaitFrames(3);
		}
	}

	private async Task VerifyI10VaseDeathSpawnLifecycleAndSettlement()
	{
		TowerDefenseBattleProcessVase process = new TowerDefenseBattleProcessVase
		{
			control = _control,
			levelControl = _control.levelControl
		};
		_control.process = process;
		Vector2I gridPos = new Vector2I(5, 2);
		TowerDefenseCellInstance cell = TowerDefenseManager.GetMapCell(gridPos);
		TowerDefenseVase vase = ((ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) is TowerDefensePacketConfig towerDefensePacketConfig) ? towerDefensePacketConfig.Plant(gridPos, playAudio: false, noLimit: true) : null) as TowerDefenseVase;
		I10Check(GodotObject.IsInstanceValid(vase), "The real Zombie Vase must instantiate as a pre-placed vase.");
		if (!GodotObject.IsInstanceValid(vase))
		{
			return;
		}
		vase.useEnterAnime = false;
		await WaitFrames(8);
		TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieNormal");
		vase.SetContentConfig(packetConfig);
		vase.dieEvent.Add(new TowerDefenseCharacterEventPacketSpawn
		{
			packetName = "ZombieBoss",
			percentage = 1.0,
			dieSpawn = true
		});
		I10Check(cell.HasVase() && vase.IsInGroup("Vase"), "The pre-placed real vase must occupy its cell and Vase objective group before opening.");
		bool destroySignalObserved = false;
		vase.OnDestroy += delegate
		{
			destroySignalObserved = true;
		};
		AdobeAnimateSpriteBase nodeOrNull = vase.GetNodeOrNull<AdobeAnimateSpriteBase>("%Hammer");
		I10Check(GodotObject.IsInstanceValid(nodeOrNull), "The real vase Hammer animation node must exist.");
		I10Check(InputMap.HasAction("Press"), "The real Press input action must exist for mouse opening.");
		MousePressComponent mousePressComponent = vase.componentManager?.GetRuntime<MousePressComponent>();
		I10Check(mousePressComponent != null && !mousePressComponent.IsReleased && mousePressComponent.Lifecycle == ComponentRuntimeLifecycle.Active && GodotObject.IsInstanceValid(mousePressComponent.ClickShape), "The real vase MousePress click-shape resource must be active.");
		if (GodotObject.IsInstanceValid(nodeOrNull) && InputMap.HasAction("Press") && mousePressComponent != null && !mousePressComponent.IsReleased && mousePressComponent.TryGetClickShapeScreenCenter(out var clickCenter))
		{
			bool flag = false;
			InputEventMouseButton pressEvent = new InputEventMouseButton
			{
				ButtonIndex = MouseButton.Left,
				ButtonMask = MouseButtonMask.Left,
				Pressed = true,
				Position = clickCenter,
				GlobalPosition = clickCenter
			};
			try
			{
				vase.componentManager._Input(pressEvent);
				flag = GodotObject.IsInstanceValid(vase) && vase.pressed;
				for (int frame = 0; frame < 30; frame++)
				{
					if (flag)
					{
						break;
					}
					if (!GodotObject.IsInstanceValid(vase))
					{
						break;
					}
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					flag = GodotObject.IsInstanceValid(vase) && vase.pressed;
				}
			}
			finally
			{
				InputEventMouseButton inputEventMouseButton = new InputEventMouseButton
				{
					ButtonIndex = MouseButton.Left,
					ButtonMask = (MouseButtonMask)0L,
					Pressed = false,
					Position = clickCenter,
					GlobalPosition = clickCenter
				};
				vase.componentManager._Input(inputEventMouseButton);
				inputEventMouseButton.Dispose();
				pressEvent.Dispose();
			}
			I10Check(flag, "The real vase mouse/input gate must accept Press and start its hammer animation.");
		}
		TowerDefenseCharacter boss = null;
		for (int frame = 0; frame < 240; frame++)
		{
			if (boss == null)
			{
				boss = FindCharacter("ZombieBoss");
			}
			if (!GodotObject.IsInstanceValid(vase) && GodotObject.IsInstanceValid(boss) && !_control.HasPendingBattleOperations)
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
		I10Check(destroySignalObserved, "The real OpenPot timeline must naturally reach the vase destroy lifecycle.");
		I10Check(!GodotObject.IsInstanceValid(vase), "The opened vase shell must leave the tree instead of becoming an immortal objective.");
		I10Check(!cell.HasVase() && GetTree().GetNodeCountInGroup("Vase") == 0, "Opening must remove the vase from both its real cell and the Vase objective group.");
		I10Check(GodotObject.IsInstanceValid(boss) && boss.config?.name == "ZombieBoss", "The vase death event must create the configured real ZombieBoss exactly through the death path.");
		Vector2I vector2I = new Vector2I(TowerDefenseManager.Instance.GetMapGridNum().X, (int)Mathf.Floor((float)TowerDefenseManager.Instance.GetMapGridNum().Y / 2f) + 1);
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(vector2I);
		I10Check(GodotObject.IsInstanceValid(boss) && boss.gridPos == vector2I, $"The event-spawned real ZombieBoss must retain its authored rightmost spawn grid; expected={vector2I}, actual={boss?.gridPos}.");
		I10Check(GodotObject.IsInstanceValid(boss) && boss.GlobalPosition.IsEqualApprox(mapCellPlantPos), $"PacketSpawn must not overwrite the real ZombieBoss rightmost world position with the vase position; expected={mapCellPlantPos}, actual={boss?.GlobalPosition}.");
		I10Check(FindCharacter("ZombieNormal") is TowerDefenseZombie, "Opening the fixed zombie vase must also release its configured real ZombieNormal content.");
		I10Check(!_control.HasPendingBattleOperations, "The asynchronous vase-content operation must complete instead of blocking settlement forever.");
		I10Check(!process.CheckFinal(), "Vase settlement must remain blocked while the death-spawned ZombieBoss is alive.");
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie { die: false, nearDie: false } towerDefenseZombie)
			{
				towerDefenseZombie.skipDestroySet = true;
				towerDefenseZombie.Destroy();
			}
		}
		for (int frame = 0; frame < 60; frame++)
		{
			if (!HasLivingHostileCharacter())
			{
				break;
			}
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
		I10Check(process.CheckFinal(), "After the fixed zombie content and spawned ZombieBoss die, the vase process must settle the level.");
	}

	private async Task VerifyI11ExplosionBreaksVasesWithoutDeepCopy()
	{
		List<TowerDefenseVase> vases = new List<TowerDefenseVase>();
		List<TowerDefensePacketConfig> sourcePackets = new List<TowerDefensePacketConfig>();
		List<TowerDefensePacketChangeCost> builtInChangeCosts = new List<TowerDefensePacketChangeCost>();
		List<VaseExplosionDerivedChangeCost> derivedChangeCosts = new List<VaseExplosionDerivedChangeCost>();
		HashSet<ulong> existingNormalZombieIds = new HashSet<ulong>();
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.config?.name == "ZombieNormal")
			{
				existingNormalZombieIds.Add(towerDefenseZombie.GetInstanceId());
			}
		}
		VaseExplosionDerivedPacketEvent derivedEvent = new VaseExplosionDerivedPacketEvent();
		for (int i = 3; i <= 6; i++)
		{
			Vector2I gridPos = new Vector2I(i, 4);
			TowerDefenseVase towerDefenseVase = TowerDefenseManager.GetPacketConfig("VaseZombie")?.Plant(gridPos, playAudio: false, noLimit: true) as TowerDefenseVase;
			if (GodotObject.IsInstanceValid(towerDefenseVase))
			{
				towerDefenseVase.useEnterAnime = false;
				TowerDefensePacketConfig packetConfig = TowerDefenseManager.GetPacketConfig("ZombieNormal");
				TowerDefensePacketChangeCost item = new TowerDefensePacketChangeCost
				{
					key = $"built-in-{i}"
				};
				VaseExplosionDerivedChangeCost item2 = new VaseExplosionDerivedChangeCost
				{
					key = $"derived-{i}"
				};
				packetConfig.useSucceededActions.Add(derivedEvent);
				packetConfig.changeCostList.Add(item);
				packetConfig.changeCostList.Add(item2);
				packetConfig.coldDownDecreaseDictionary["vase-source"] = new Dictionary
				{
					["ControlCharacterList"] = new Godot.Collections.Array { this },
					["Percentage"] = 0.1
				};
				towerDefenseVase.SetContentConfig(packetConfig);
				vases.Add(towerDefenseVase);
				sourcePackets.Add(packetConfig);
				builtInChangeCosts.Add(item);
				derivedChangeCosts.Add(item2);
			}
		}
		await WaitFrames(10);
		I11Check(vases.Count == 4 && vases.TrueForAll(GodotObject.IsInstanceValid), $"The explosion fixture must create four real zombie vases; count={vases.Count}.");
		I11Check(vases.TrueForAll((TowerDefenseVase vase) => TowerDefenseManager.Instance.characterRegistry.GetActiveCharacters().Contains(vase) && vase.HasHitBox && vase.IsHitBoxMonitorable), "Every vase must enter the production explosion registry with a live hitbox.");
		Array<TowerDefenseCharacterEventBase> eventList = new Array<TowerDefenseCharacterEventBase>
		{
			new TowerDefenseCharacterEventExplodeHurt
			{
				num = 1800.0,
				type = "Bomb"
			}
		};
		TowerDefenseExplode.CreateExplode(TowerDefenseManager.GetMapCellPlantPos(new Vector2I(4, 4)) + new Vector2(50f, 0f), new Vector2(2.25f, 0.75f), eventList, null, TowerDefenseEnum.CHARACTER_CAMP.ALL, -1);
		I11Check(vases.TrueForAll((TowerDefenseVase vase) => GodotObject.IsInstanceValid(vase) && !vase.isDestroy), "Range explosions must retain their existing one-physics-frame delay.");
		List<TowerDefenseZombie> spawnedContents = null;
		for (int frame = 0; frame < 120; frame++)
		{
			spawnedContents = FindNewNormalZombies(existingNormalZombieIds);
			if (vases.TrueForAll((TowerDefenseVase vase) => !GodotObject.IsInstanceValid(vase)) && spawnedContents.Count == 4 && !_control.HasPendingBattleOperations)
			{
				break;
			}
			await WaitFrames(1);
		}
		I11Check(vases.TrueForAll((TowerDefenseVase vase) => !GodotObject.IsInstanceValid(vase)), "One range explosion must release all four vase shells.");
		BugOverviewSpawnEventAndVaseLifecycleRuntimeTest bugOverviewSpawnEventAndVaseLifecycleRuntimeTest = this;
		List<TowerDefenseZombie> list = spawnedContents;
		bugOverviewSpawnEventAndVaseLifecycleRuntimeTest.I11Check(list != null && list.Count == 4, $"All four exploded vases must release their zombie contents; count={spawnedContents?.Count ?? 0}.");
		List<TowerDefenseZombie> list2 = spawnedContents;
		bool flag = list2 != null && list2.Count == 4;
		List<TowerDefenseZombie> list3 = spawnedContents;
		bool flag2 = list3 != null && list3.Count == 4;
		if (spawnedContents != null)
		{
			for (int num = 0; num < spawnedContents.Count; num++)
			{
				TowerDefensePacketConfig packet = spawnedContents[num].packet;
				flag &= GodotObject.IsInstanceValid(packet) && packet.useSucceededActions.Contains(derivedEvent) && packet.changeCostList.Count == 2 && packet.changeCostList[1] == derivedChangeCosts[num];
				flag2 &= GodotObject.IsInstanceValid(packet) && packet != sourcePackets[num] && packet.changeCostList.Count == 2 && packet.changeCostList[0] != builtInChangeCosts[num] && packet.coldDownDecreaseDictionary.Count == 0;
			}
		}
		I11Check(flag, "Derived packet Resources must remain shared instead of entering a deep-copy fallback.");
		I11Check(flag2, "Built-in mutable packet state must be isolated while transient cooldown references are cleared.");
		I11Check(!_control.HasPendingBattleOperations, "All asynchronous vase-content operations must settle after the explosion burst.");
		foreach (TowerDefenseZombie item3 in spawnedContents ?? new List<TowerDefenseZombie>())
		{
			if (GodotObject.IsInstanceValid(item3) && !item3.isDestroy)
			{
				item3.skipDestroySet = true;
				item3.Destroy();
			}
		}
		await WaitFrames(3);
	}

	private async Task VerifyJackboxExplosionReleasesVaseContents()
	{
		((TowerDefenseLevelConfig)_control.levelConfig).finishMethod = TowerDefenseEnum.LEVEL_FINISH_METHOD.VASE;
		Node2D previousGroundItemParent = TowerDefenseGroundItemBase.characterNode;
		TowerDefenseGroundItemBase.characterNode = _control.characterNode;
		try
		{
			List<TowerDefenseVase> vases = new List<TowerDefenseVase>();
			string[] array = new string[2] { "PlantSunFlower", "ZombieNormal" };
			for (int i = 0; i < array.Length; i++)
			{
				TowerDefenseVase towerDefenseVase = TowerDefenseManager.GetPacketConfig("VaseZombie").Plant(new Vector2I(3 + i * 2, 3), playAudio: false, noLimit: true) as TowerDefenseVase;
				if (!GodotObject.IsInstanceValid(towerDefenseVase))
				{
					throw new InvalidOperationException("玩偶匣回归无法生成罐子。");
				}
				towerDefenseVase.useEnterAnime = false;
				towerDefenseVase.SetContentConfig(TowerDefenseManager.GetPacketConfig(array[i]));
				towerDefenseVase.dieEvent.Add(new TowerDefenseCharacterEventPacketSpawn
				{
					packetName = "ZombieBoss",
					percentage = 1.0,
					dieSpawn = true
				});
				vases.Add(towerDefenseVase);
			}
			TowerDefenseCharacter towerDefenseCharacter = TowerDefenseManager.GetPacketConfig("PlantSunFlower").Plant(new Vector2I(4, 4), playAudio: false, noLimit: true);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
			{
				throw new InvalidOperationException("玩偶匣回归无法生成亡语抑制对照植物。");
			}
			towerDefenseCharacter.dieEvent.Add(new TowerDefenseCharacterEventPacketSpawn
			{
				packetName = "ZombieBoss",
				percentage = 1.0,
				dieSpawn = true
			});
			bool plantDestroyed = false;
			towerDefenseCharacter.OnDestroy += delegate
			{
				plantDestroyed = true;
			};
			await WaitFrames(10);
			JackboxCheck(vases.TrueForAll((TowerDefenseVase vase) => vase.HasHitBox && vase.IsHitBoxMonitorable && TowerDefenseManager.GetMapCell(vase.gridPos).HasVase()), "两只真实罐子必须进入地图与爆炸碰撞查询。");
			JackboxCheck(!GodotObject.IsInstanceValid(FindCharacter("ZombieBoss")) && FindNewNormalZombies(new HashSet<ulong>()).Count == 0 && GetTree().GetNodeCountInGroup("VasePacketShow") == 0, "爆炸前不得残留前序测试的僵尸或掉落卡片。");
			TowerDefenseZombieJackbox jackbox = TowerDefenseManager.GetPacketConfig("ZombieJackbox").Plant(new Vector2I(4, 3), playAudio: false, noLimit: true) as TowerDefenseZombieJackbox;
			if (!GodotObject.IsInstanceValid(jackbox))
			{
				throw new InvalidOperationException("玩偶匣回归无法生成真实玩偶匣僵尸。");
			}
			bool jackboxExploded = false;
			jackbox.OnDestroy += delegate
			{
				jackboxExploded = jackbox.over && jackbox.hasJackBox;
			};
			await WaitFrames(1);
			JackboxCheck(jackbox.eventList.Count == 1 && jackbox.eventList[0] is TowerDefenseCharacterEventExplodeHurt, "玩偶匣必须使用资源自带的爆炸事件。");
			await CaptureJackboxVaseFrame("before");
			for (int frame = 0; frame < 240; frame++)
			{
				if (jackboxExploded && vases.TrueForAll((TowerDefenseVase vase) => !GodotObject.IsInstanceValid(vase)) && !_control.HasPendingBattleOperations)
				{
					break;
				}
				await WaitFrames(1);
			}
			await WaitFrames(10);
			JackboxCheck(jackboxExploded, "砸罐模式必须自然完成玩偶匣的 Bomb 动画并引爆。");
			JackboxCheck(vases.TrueForAll((TowerDefenseVase vase) => !GodotObject.IsInstanceValid(vase)) && !TowerDefenseManager.GetMapCell(new Vector2I(3, 3)).HasVase() && !TowerDefenseManager.GetMapCell(new Vector2I(5, 3)).HasVase(), "爆炸后两只罐壳必须释放并清除地图占用。");
			List<TowerDefenseZombie> list = FindNewNormalZombies(new HashSet<ulong>());
			JackboxCheck(list.Count == 1 && !list[0].die && list[0].instance.hitpoints > 0.0 && list[0].gridPos == new Vector2I(5, 3), "僵尸罐必须只释放一只存活的普通僵尸，且不会被同次爆炸吞掉。");
			Array<Node> nodesInGroup = GetTree().GetNodesInGroup("VasePacketShow");
			JackboxCheck(nodesInGroup.Count == 1 && nodesInGroup[0] is TowerDefenseInGamePacketShow towerDefenseInGamePacketShow && towerDefenseInGamePacketShow.config?.saveKey == "PlantSunFlower", "植物罐必须只掉落一张向日葵卡片。");
			JackboxCheck(plantDestroyed && !GodotObject.IsInstanceValid(FindCharacter("ZombieBoss")), "爆炸仍须炸死对照植物，并抑制植物和罐子额外配置的亡语。");
			JackboxCheck(!_control.HasPendingBattleOperations, "罐子内容物异步结算必须结束。");
			JackboxCheck(!_control.process.CheckFinal(), "释放的僵尸仍然存活时，砸罐关卡不得提前胜利。");
			await CaptureJackboxVaseFrame("after");
		}
		finally
		{
			TowerDefenseGroundItemBase.characterNode = previousGroundItemParent;
		}
	}

	private async Task CaptureJackboxVaseFrame(string phase)
	{
		if (DisplayServer.GetName() == "headless")
		{
			return;
		}
		await ToSignal(RenderingServer.Singleton, RenderingServer.SignalName.FramePostDraw);
		using Image image = GetViewport().GetTexture().GetImage();
		image.SavePng("user://jackbox-vase-" + phase + ".png");
	}

	private void JackboxCheck(bool condition, string message)
	{
		_jackboxChecks++;
		if (!condition)
		{
			_jackboxFailures++;
			GD.PushError("[JackboxVaseContent] " + message);
		}
	}

	private List<TowerDefenseZombie> FindNewNormalZombies(HashSet<ulong> existingIds)
	{
		List<TowerDefenseZombie> list = new List<TowerDefenseZombie>();
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseZombie towerDefenseZombie && towerDefenseZombie.config?.name == "ZombieNormal" && !towerDefenseZombie.isDestroy && !existingIds.Contains(towerDefenseZombie.GetInstanceId()))
			{
				list.Add(towerDefenseZombie);
			}
		}
		list.Sort((TowerDefenseZombie left, TowerDefenseZombie right) => left.gridPos.X.CompareTo(right.gridPos.X));
		return list;
	}

	private bool HasLivingHostileCharacter()
	{
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return false;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && towerDefenseCharacter.camp != TowerDefenseEnum.CHARACTER_CAMP.PLANT)
			{
				return true;
			}
		}
		return false;
	}

	private TowerDefenseCharacter FindCharacter(string configName)
	{
		if (!GodotObject.IsInstanceValid(_control?.characterNode))
		{
			return null;
		}
		foreach (Node child in _control.characterNode.GetChildren())
		{
			if (child is TowerDefenseCharacter towerDefenseCharacter && towerDefenseCharacter.config?.name == configName && !towerDefenseCharacter.IsQueuedForDeletion())
			{
				return towerDefenseCharacter;
			}
		}
		return null;
	}

	private static int CountCharactersInCell(TowerDefenseCellInstance cell, string configName)
	{
		if (!GodotObject.IsInstanceValid(cell))
		{
			return 0;
		}
		cell.ClearEmpty();
		int num = 0;
		foreach (TowerDefenseCharacter character in cell.characterList)
		{
			if (GodotObject.IsInstanceValid(character) && character.config?.name == configName)
			{
				num++;
			}
		}
		return num;
	}

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
	}

	private static void RegisterFixtures()
	{
		RegisterFixture("PlantSunFlower", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Packet/PlantSunFlower.tres", "res://Asset/Anime/Character/Plant/Chapter0/SunFlower/Scene/TowerDefensePlantSunFlower.tscn");
		RegisterFixture("ZombieNormal", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Packet/Normal/ZombieNormal.tres", "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		RegisterFixture("VaseZombie", "res://Asset/Anime/Character/Vase/Zombie/Packet/VaseZombie.tres", "res://Asset/Anime/Character/Vase/Zombie/Scene/TowerDefenseVaseZombie.tscn");
		RegisterFixture("ZombieBoss", "res://Asset/Anime/Character/Zombie/Boss/Boss/Packet/ZombieBoss.tres", "res://Asset/Anime/Character/Zombie/Boss/Boss/Scene/TowerDefenseZombieBoss.tscn");
		RegisterFixture("ZombieJackbox", "res://Asset/Anime/Character/Zombie/Chapter2/Jackbox/Packet/ZombieJackbox.tres", "res://Asset/Anime/Character/Zombie/Chapter2/Jackbox/Scene/TowerDefenseZombieJackbox.tscn");
	}

	private static void RegisterFixture(string key, string packetPath, string scenePath)
	{
		ResourceManager instance = ResourceManager.Instance;
		instance.TOWERDEFENSE_PACKETS[key] = ResourceLoader.Load<TowerDefensePacketConfig>(packetPath, null, ResourceLoader.CacheMode.Ignore);
		instance.TOWERDEFENSE_CHARCATERS[key] = ResourceLoader.Load<PackedScene>(scenePath, null, ResourceLoader.CacheMode.Ignore);
	}

	private static void UnregisterFixtures()
	{
		ResourceManager instance = ResourceManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			string[] array = new string[5] { "PlantSunFlower", "ZombieNormal", "VaseZombie", "ZombieBoss", "ZombieJackbox" };
			foreach (string key in array)
			{
				instance.TOWERDEFENSE_PACKETS.Remove(key);
				instance.TOWERDEFENSE_CHARCATERS.Remove(key);
			}
		}
	}

	private void I9Check(bool condition, string message)
	{
		_i9Checks++;
		if (!condition)
		{
			_i9Failures++;
			GD.PushError("[I9SpawnEventOccupancy] " + message);
		}
	}

	private void I10Check(bool condition, string message)
	{
		_i10Checks++;
		if (!condition)
		{
			_i10Failures++;
			GD.PushError("[I10VaseDeathSpawn] " + message);
		}
	}

	private void I11Check(bool condition, string message)
	{
		_i11Checks++;
		if (!condition)
		{
			_i11Failures++;
			GD.PushError("[I11VaseExplosionRuntimeCopy] " + message);
		}
	}
}
