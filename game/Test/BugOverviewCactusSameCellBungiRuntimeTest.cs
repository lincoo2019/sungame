using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewCactusSameCellBungiRuntimeTest : Node
{
	private const string CactusPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Packet/PlantCactus.tres";

	private const string CactusScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn";

	private const string BungiPacketPath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres";

	private const string BungiScenePath = "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn";

	private const string SpikeConfigPath = "res://Asset/Config/Projectile/Spike/SpikeDefault.tres";

	private static readonly Vector2I AttackGrid = new Vector2I(4, 3);

	private static readonly Vector2I ProtectedSummonGrid = new Vector2I(7, 3);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousProjectiles = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private readonly HashSet<string> _missingProjectiles = new HashSet<string>();

	private readonly List<Resource> _registeredResources = new List<Resource>();

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		CactusSameCellBungiRuntimeControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		try
		{
			_ = 1;
			try
			{
				Check(GodotObject.IsInstanceValid(manager) && GodotObject.IsInstanceValid(ResourceManager.Instance), "TowerDefenseManager and ResourceManager autoloads must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00d1;
				}
				RegisterRealFixtures();
				control = new CactusSameCellBungiRuntimeControlStub
				{
					Name = "CactusSameCellBungiRuntimeControl",
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
				await VerifyOrdinaryBungiBecomesTargetableAfterLanding(control);
				await VerifyHypnotizedFixedSummonStaysUntargetable(control);
				goto end_IL_00bf;
				end_IL_00d1:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCactusSameCellBungiRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00bf;
			}
			return;
			end_IL_00bf:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(control?.characterNode))
			{
				foreach (Node child in control.characterNode.GetChildren())
				{
					if (!child.IsQueuedForDeletion())
					{
						child.QueueFree();
					}
				}
			}
			await WaitFrames(5);
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
			RestoreRealFixtures();
			await WaitFrames(6);
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			foreach (Resource registeredResource in _registeredResources)
			{
				registeredResource?.Dispose();
			}
			_registeredResources.Clear();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0 && _checks >= 17;
		GD.Print($"CACTUS_SAME_CELL_BUNGI_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyOrdinaryBungiBecomesTargetableAfterLanding(CactusSameCellBungiRuntimeControlStub control)
	{
		TowerDefensePlantCactus cactus = Instantiate<TowerDefensePlantCactus>("res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn");
		TowerDefenseZombieBungi bungi = Instantiate<TowerDefenseZombieBungi>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
		Check(GodotObject.IsInstanceValid(cactus) && cactus.config?.name == "PlantCactus" && GodotObject.IsInstanceValid(bungi) && bungi.config?.name == "ZombieBungi", "The regression must instantiate the real Cactus and Bungi scenes.");
		if (!GodotObject.IsInstanceValid(cactus) || !GodotObject.IsInstanceValid(bungi))
		{
			return;
		}
		PrepareCharacter(cactus, AttackGrid);
		PrepareCharacter(bungi, AttackGrid);
		control.characterNode.AddChild(cactus, forceReadableName: false, InternalMode.Disabled);
		control.characterNode.AddChild(bungi, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(5);
		TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
		{
			gridPos = AttackGrid
		};
		towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
		towerDefenseCellInstance.characterList.Add(cactus);
		towerDefenseCellInstance.characterList.Add(bungi);
		cactus.cell = towerDefenseCellInstance;
		bungi.cell = towerDefenseCellInstance;
		Check(GodotObject.IsInstanceValid(cactus) && GodotObject.IsInstanceValid(bungi) && cactus.gridPos == bungi.gridPos, $"The real Cactus and ordinary Bungi must spawn in the same map cell; cactusValid={GodotObject.IsInstanceValid(cactus)}, bungiValid={GodotObject.IsInstanceValid(bungi)}, cactusGrid={(GodotObject.IsInstanceValid(cactus) ? cactus.gridPos : Vector2I.Zero)}, bungiGrid={(GodotObject.IsInstanceValid(bungi) ? bungi.gridPos : Vector2I.Zero)}.");
		Check(GodotObject.IsInstanceValid(bungi.cell), "The real Bungi slot runtime must bind the same-cell map instance before descent.");
		if (!GodotObject.IsInstanceValid(cactus) || !GodotObject.IsInstanceValid(bungi) || !GodotObject.IsInstanceValid(bungi.cell))
		{
			return;
		}
		FireComponent fire = cactus.componentManager?.GetRuntime<FireComponent>("character.fire");
		FireComponentExtendCactus extension = cactus.componentManager?.GetRuntime<FireComponentExtendCactus>("character.fire.cactus");
		Check(fire != null && !fire.IsReleased && extension != null && !extension.IsReleased, "The original Cactus fire and extension runtimes must be active.");
		if (fire == null || fire.IsReleased || extension == null || extension.IsReleased)
		{
			return;
		}
		Check(fire.snapStraightProjectileToSameCellTarget, "The original Cactus must enable its same-cell straight-projectile correction.");
		control.isGameRunning = true;
		bungi.Walk();
		Check(bungi.instance.invincible && !bungi.instance.canBeCollection && !bungi.targetRegistrationComponent.canProjectileCheck && bungi.instance.maskFlags == 0, $"Drop entry must keep an ordinary Bungi outside every targeting channel; invincible={bungi.instance.invincible}, collection={bungi.instance.canBeCollection}, projectile={bungi.targetRegistrationComponent.canProjectileCheck}, mask={bungi.instance.maskFlags}.");
		await WaitFrames(2);
		int num = 2;
		Check(bungi.z > bungi.groundHeight && !bungi.isGround, "The targetability check must run while the ordinary Bungi is still descending.");
		Check(bungi.instance.invincible && !bungi.instance.canBeCollection, $"An ordinary descending Bungi must remain invincible and outside target collection; invincible={bungi.instance.invincible}, collection={bungi.instance.canBeCollection}.");
		Check(!bungi.targetRegistrationComponent.canProjectileCheck, $"An ordinary descending Bungi must not participate in projectile target checks; projectile={bungi.targetRegistrationComponent.canProjectileCheck}.");
		Check(bungi.instance.maskFlags == 0 && !bungi.IsHitBoxEnabled && !bungi.IsHitBoxMonitorable, $"An ordinary descending Bungi must have no collision mask or active hit box; mask={bungi.instance.maskFlags}, enabled={bungi.IsHitBoxEnabled}, monitorable={bungi.IsHitBoxMonitorable}.");
		fire.groundRight = 10000f;
		fire.timer = 0f;
		fire.checkIntreval = 0;
		FireComponentCheckConfig fireComponentCheckConfig = ((fire.fireCheckList.Count > 0) ? fire.fireCheckList[0] : null);
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = fireComponentCheckConfig?.projectile?.GetProjectile();
		Check(GodotObject.IsInstanceValid(fireComponentCheckConfig) && fireComponentCheckConfig.GetCollisionFlags() == num && GodotObject.IsInstanceValid(towerDefenseProjectileCreateData), "The original Cactus must retain its authored first off-ground Spike check.");
		Check(!fire.CanFireCheckOnce(towerDefenseProjectileCreateData, num) && fire.firstCharacter != bungi, "The original Cactus ray must not acquire the descending Bungi in its own cell.");
		extension.IdleProcessing(0.0);
		Check(extension.StateMachine?.CurrentStateHandle?.StableId == "cactus.idle" && !extension.IsUp() && !extension.CanRun(), "A same-cell descending Bungi must leave the original Cactus idle.");
		Check(await WaitUntil(() => GodotObject.IsInstanceValid(bungi) && bungi.waitGrab, 300), "The ordinary Bungi must finish its descent before the targetable assertions.");
		int num2 = 1;
		Check(GodotObject.IsInstanceValid(bungi) && bungi.isGround && !bungi.instance.invincible && bungi.instance.canBeCollection && bungi.targetRegistrationComponent.canProjectileCheck && bungi.instance.maskFlags == num2 && bungi.IsHitBoxEnabled && bungi.IsHitBoxMonitorable, $"An ordinary landed Bungi must re-enter ground targeting; ground={bungi?.isGround}, invincible={bungi?.instance?.invincible}, collection={bungi?.instance?.canBeCollection}, projectile={bungi?.targetRegistrationComponent?.canProjectileCheck}, mask={bungi?.instance?.maskFlags}, enabled={bungi?.IsHitBoxEnabled}, monitorable={bungi?.IsHitBoxMonitorable}.");
		FireComponentCheckConfig fireComponentCheckConfig2 = ((fire.fireCheckList.Count > 1) ? fire.fireCheckList[1] : null);
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData2 = fireComponentCheckConfig2?.projectile?.GetProjectile();
		Check(GodotObject.IsInstanceValid(fireComponentCheckConfig2) && GodotObject.IsInstanceValid(towerDefenseProjectileCreateData2), "The original Cactus must retain its authored ground Spike check.");
		Check(fire.CanFireCheckOnce(towerDefenseProjectileCreateData2, num2) && fire.firstCharacter == bungi, "The original Cactus ray must acquire the Bungi only after it has landed.");
		extension.IdleProcessing(0.0);
		Check(!extension.IsUp() && extension.CanRun(), "The lowered Cactus must release its FireComponent gate for the landed Bungi.");
		fire.timer = 0f;
		fire.checkIntreval = 0;
		fire.IdleProcessing(0.0);
		Check(fire.StateMachine?.CurrentStateHandle?.StableId == "fire.attack" && fire.runningCheckId == 1, "The released original Cactus must enter its ground attack with the landed same-cell Bungi selected.");
		double hitpointsBefore = bungi.instance.hitpoints;
		int volleyCount = 0;
		int spawnedBulletIndex = -1;
		bool spawnedBulletActive = false;
		Vector2 spawnedBulletPosition = Vector2.Zero;
		Vector2 spawnedBulletVelocity = Vector2.Zero;
		double spawnedBulletHeight = 0.0;
		int spawnedBulletGridY = -1;
		int spawnedBulletCollisionFlags = -1;
		Rect2 bungiRectAtVolley = default(Rect2);
		Vector2 markerPositionAtVolley = Vector2.Zero;
		string firstCharacterAtVolley = string.Empty;
		fire.OnFireVolley += OnVolley;
		int firstVolleyFrame = -1;
		for (int frame = 0; frame < 180; frame++)
		{
			fire.AttackProcessing(1.0 / 60.0);
			cactus.BatchUpdate(1.0 / 60.0);
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			if (volleyCount > 0 && firstVolleyFrame < 0)
			{
				firstVolleyFrame = frame;
			}
			if (firstVolleyFrame >= 0 && frame - firstVolleyFrame >= 50)
			{
				break;
			}
		}
		fire.OnFireVolley -= OnVolley;
		fire.alive = false;
		Check(firstVolleyFrame >= 0, "The real original Cactus attack animation must emit its authored fire event.");
		Check(volleyCount == 1, $"One original Cactus air shot must be emitted; volleys={volleyCount}.");
		Check(bungi.instance.hitpoints < hitpointsBefore, $"The real same-cell Spike must damage the landed Bungi; hp={bungi.instance.hitpoints}/{hitpointsBefore}, bulletIndex={spawnedBulletIndex}, active={spawnedBulletActive}, pos={spawnedBulletPosition}, vel={spawnedBulletVelocity}, height={spawnedBulletHeight}, gridY={spawnedBulletGridY}, flags={spawnedBulletCollisionFlags}, marker={markerPositionAtVolley}, targetRect={bungiRectAtVolley}, first={firstCharacterAtVolley}, snap={fire.snapStraightProjectileToSameCellTarget}, cactus={cactus.GetLogicalGlobalPosition()}, bungi={bungi.GetLogicalGlobalPosition()}.");
		void OnVolley(ulong _)
		{
			volleyCount++;
			firstCharacterAtVolley = (GodotObject.IsInstanceValid(fire.firstCharacter) ? $"{fire.firstCharacter.Name}@{fire.firstCharacter.gridPos}" : "<null>");
			bungiRectAtVolley = bungi.WorldHitRect;
			if (fire.firePosMarker.Count > 0 && GodotObject.IsInstanceValid(fire.firePosMarker[0]))
			{
				markerPositionAtVolley = cactus.GetLogicalGlobalPosition(fire.firePosMarker[0]);
			}
			BulletField instance = BulletField.Instance;
			if (GodotObject.IsInstanceValid(instance))
			{
				spawnedBulletIndex = instance.LastSpawnedIndex;
				spawnedBulletActive = instance.IsBulletActive(spawnedBulletIndex);
				if (spawnedBulletActive)
				{
					ref BulletData bulletDataRef = ref instance.GetBulletDataRef(spawnedBulletIndex);
					spawnedBulletPosition = bulletDataRef.pos;
					spawnedBulletVelocity = bulletDataRef.vel;
					spawnedBulletHeight = bulletDataRef.height;
					spawnedBulletGridY = bulletDataRef.gridY;
					spawnedBulletCollisionFlags = bulletDataRef.collisionFlags;
				}
			}
		}
	}

	private async Task VerifyHypnotizedFixedSummonStaysUntargetable(CactusSameCellBungiRuntimeControlStub control)
	{
		control.isGameRunning = false;
		TowerDefenseZombieBungi bungi = Instantiate<TowerDefenseZombieBungi>("res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
		if (GodotObject.IsInstanceValid(bungi))
		{
			PrepareCharacter(bungi, ProtectedSummonGrid);
			control.characterNode.AddChild(bungi, forceReadableName: false, InternalMode.Disabled);
		}
		await WaitFrames(4);
		TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
		{
			gridPos = ProtectedSummonGrid
		};
		towerDefenseCellInstance.Init(new TowerDefenseCellConfig());
		if (GodotObject.IsInstanceValid(bungi))
		{
			towerDefenseCellInstance.characterList.Add(bungi);
			bungi.cell = towerDefenseCellInstance;
		}
		Check(GodotObject.IsInstanceValid(bungi) && GodotObject.IsInstanceValid(bungi.cell), $"The protected-variant check must spawn a second real Bungi on a valid cell; bungiValid={GodotObject.IsInstanceValid(bungi)}, cellValid={GodotObject.IsInstanceValid(bungi?.cell)}.");
		if (GodotObject.IsInstanceValid(bungi) && GodotObject.IsInstanceValid(bungi.cell))
		{
			bungi.skipBungeeTarget = true;
			bungi.Hypnoses();
			control.isGameRunning = true;
			bungi.Walk();
			await WaitFrames(2);
			Check(bungi.instance.hypnoses && bungi.skipBungeeTarget, "The protected fixture must match the fixed-position hypnotized Bungi variant.");
			Check(bungi.instance.invincible && !bungi.instance.canBeCollection, "The hypnotized fixed-position summon must remain invincible and outside target collection while descending.");
			Check(!bungi.targetRegistrationComponent.canProjectileCheck && bungi.instance.maskFlags == 0 && !bungi.IsHitBoxEnabled && !bungi.IsHitBoxMonitorable, "The hypnotized fixed-position summon must retain disabled projectile targeting, a zero mask, and a suppressed hit box.");
			Check(await WaitUntil(() => GodotObject.IsInstanceValid(bungi) && bungi.waitGrab, 300), "The fixed-position hypnotized Bungi must complete its descent in the production state flow.");
			Check(GodotObject.IsInstanceValid(bungi) && bungi.isGround && bungi.instance.invincible && !bungi.instance.canBeCollection && !bungi.targetRegistrationComponent.canProjectileCheck && bungi.instance.maskFlags == 0 && !bungi.IsHitBoxEnabled && !bungi.IsHitBoxMonitorable, "The handbook-untargetable hypnotized Bungi must remain outside every target channel after landing.");
		}
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

	private static void PrepareCharacter(TowerDefenseCharacter character, Vector2I grid)
	{
		character.editorPreviewMode = false;
		character.inGame = true;
		character.gridPos = grid;
		character.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(grid));
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
		towerDefenseBattleFeatureMap.rect = TowerDefenseBattleFeatureMap.BuildProjectileBoundaryRect(towerDefenseMapConfig);
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
		RegisterPacket("PlantCactus", "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Packet/PlantCactus.tres");
		RegisterPacket("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Packet/ZombieBungi.tres");
		RegisterCharacter("PlantCactus", "res://Asset/Anime/Character/Plant/Chapter0/Cactus/Scene/TowerDefensePlantCactus.tscn");
		RegisterCharacter("ZombieBungi", "res://Asset/Anime/Character/Zombie/Chapter5/Bungi/Scene/TowerDefenseZombieBungi.tscn");
		RegisterProjectile("Spike", "res://Asset/Config/Projectile/Spike/SpikeDefault.tres");
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
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>(path, null, ResourceLoader.CacheMode.Ignore);
		_registeredResources.Add(towerDefenseProjectileConfig);
		instance.PROJECTILE_CONFIG[key] = towerDefenseProjectileConfig;
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

	private async Task WaitFrames(int count)
	{
		for (int frame = 0; frame < count; frame++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		}
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

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewCactusSameCellBungiRuntimeTest] " + message);
		}
	}
}
