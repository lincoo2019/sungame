using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewCatapultSpawnTargetAcquisitionRuntimeTest : Node
{
	private readonly record struct CatapultCase(string Label, string CharacterName, string PacketPath, string ScenePath);

	private static readonly CatapultCase[] CatapultCases = new CatapultCase[5]
	{
		new CatapultCase("Catapult", "ZombieCatapult", "res://Asset/Anime/Character/Zombie/Chapter5/Catapult/Packet/ZombieCatapult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Catapult/Scene/TowerDefenseZombieCatapult.tscn"),
		new CatapultCase("Balloonpult", "ZombieBalloonpult", "res://Asset/Anime/Character/Zombie/Chapter5/Balloonpult/Packet/ZombieBalloonpult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Balloonpult/Scene/TowerDefenseZombieBalloonpult.tscn"),
		new CatapultCase("Imppult", "ZombieImppult", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Packet/ZombieImppult.tres", "res://Asset/Anime/Character/Zombie/Chapter5/Imppult/Scene/TowerDefenseZombieImppult.tscn"),
		new CatapultCase("Zambonipult", "ZombieZambonipult", "res://Asset/Anime/Character/Zombie/Chapter6/Zambonipult/Packet/ZombieZambonipult.tres", "res://Asset/Anime/Character/Zombie/Chapter6/Zambonipult/Scene/TowerDefenseZombieZambonipult.tscn"),
		new CatapultCase("Catapow", "ZombieCatapow", "res://Asset/Anime/Character/Zombie/Challenge/Catapow/Packet/ZombieCatapow.tres", "res://Asset/Anime/Character/Zombie/Challenge/Catapow/Scene/TowerDefenseZombieCatapow.tscn")
	};

	private const string WallnutPacketPath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres";

	private const string WallnutScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn";

	private static readonly Vector2I TargetGrid = new Vector2I(6, 2);

	private int _checks;

	private int _failures;

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousPackets = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly System.Collections.Generic.Dictionary<string, Resource> _previousCharacters = new System.Collections.Generic.Dictionary<string, Resource>();

	private readonly HashSet<string> _missingPackets = new HashSet<string>();

	private readonly HashSet<string> _missingCharacters = new HashSet<string>();

	private GameplayResourceLoadState _previousGameplayResourceLoadState;

	private bool _gameplayResourceLoadStateOverridden;

	public override async void _Ready()
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		CatapultSpawnTargetAcquisitionControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		Node2D mapIceCap = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefensePlant wallnut = null;
		try
		{
			_ = 3;
			try
			{
				Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available.");
				Check(GodotObject.IsInstanceValid(ResourceManager.Instance), "ResourceManager autoload must be available.");
				if (!GodotObject.IsInstanceValid(manager) || !GodotObject.IsInstanceValid(ResourceManager.Instance))
				{
					goto end_IL_00f0;
				}
				RegisterRealFixtures();
				PublishFocusedGameplayResourceReady();
				control = new CatapultSpawnTargetAcquisitionControlStub
				{
					Name = "CatapultSpawnTargetAcquisitionControl",
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
				mapIceCap = new Node2D
				{
					Name = "MapIceCap"
				};
				AddChild(mapIceCap, forceReadableName: false, InternalMode.Disabled);
				mapControl.mapIceCap = mapIceCap;
				mapFeature = CreateMapFeature(mapControl, manager.gridNum, manager.gridSize, manager.gridBeginPos);
				mapFeature.control = control;
				control.featureDictionary[new StringName("Map")] = mapFeature;
				CatapultCase[] catapultCases = CatapultCases;
				foreach (CatapultCase catapultCase in catapultCases)
				{
					await VerifyNoTargetKeepsMoving(control, manager, catapultCase);
				}
				await VerifyStandardCatapultDoesNotStallAtLowHealth(control, manager);
				TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket("res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
				Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real Wall-nut packet must load.");
				TowerDefenseCellInstance targetCell = TowerDefenseManager.GetMapCell(TargetGrid);
				wallnut = towerDefensePacketConfig?.Plant(TargetGrid, playAudio: false, noLimit: true, default(EconomyAccountId), skipPlacementCheck: true) as TowerDefensePlant;
				await WaitFrames(6);
				Check(GodotObject.IsInstanceValid(wallnut) && wallnut.config?.name == "PlantWallnut", "The target fixture must use the real Wall-nut scene and packet.");
				BugOverviewCatapultSpawnTargetAcquisitionRuntimeTest bugOverviewCatapultSpawnTargetAcquisitionRuntimeTest = this;
				int condition;
				if (GodotObject.IsInstanceValid(targetCell) && targetCell.characterList.Contains(wallnut))
				{
					TowerDefensePlant towerDefensePlant = wallnut;
					condition = ((towerDefensePlant != null && towerDefensePlant.targetRegistrationComponent?.IsReleased == false) ? 1 : 0);
				}
				else
				{
					condition = 0;
				}
				bugOverviewCatapultSpawnTargetAcquisitionRuntimeTest.Check((byte)condition != 0, "The real Wall-nut must occupy and register in the target row.");
				if (GodotObject.IsInstanceValid(wallnut))
				{
					catapultCases = CatapultCases;
					foreach (CatapultCase catapultCase2 in catapultCases)
					{
						await VerifyTargetAcquiredAtFireBoundary(control, manager, wallnut, catapultCase2);
					}
				}
				goto end_IL_00d5;
				end_IL_00f0:;
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewCatapultSpawnTargetAcquisitionRuntimeTest] Unexpected exception: {value}");
				goto end_IL_00d5;
			}
			return;
			end_IL_00d5:;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(wallnut))
			{
				control?.CleanupCharacterCell(wallnut);
				if (!wallnut.IsQueuedForDeletion())
				{
					wallnut.QueueFree();
				}
			}
			if (GodotObject.IsInstanceValid(manager))
			{
				manager.currentControl = previousControl;
				manager.gridBeginPos = previousGridBegin;
				manager.gridSize = previousGridSize;
				manager.gridNum = previousGridNum;
			}
			mapFeature?.Destroy();
			if (GodotObject.IsInstanceValid(mapIceCap) && !mapIceCap.IsQueuedForDeletion())
			{
				mapIceCap.QueueFree();
			}
			if (GodotObject.IsInstanceValid(mapControl))
			{
				mapControl.Free();
			}
			if (GodotObject.IsInstanceValid(control) && !control.IsQueuedForDeletion())
			{
				control.QueueFree();
			}
			RestoreRealFixtures();
			RestoreGameplayResourceLoadState();
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
		bool flag = _failures == 0 && _checks == 109;
		GD.Print($"CATAPULT_SPAWN_TARGET_ACQUISITION_RESULT passed={flag} checks={_checks} failures={_failures}");
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

	private async Task VerifyStandardCatapultDoesNotStallAtLowHealth(CatapultSpawnTargetAcquisitionControlStub control, TowerDefenseManager manager)
	{
		control.isGameRunning = false;
		CatapultCase catapultCase = CatapultCases[0];
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket(catapultCase.PacketPath);
		Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real Catapult packet must load for the low-health regression.");
		TowerDefenseZombie zombie = towerDefensePacketConfig?.Spawn(TargetGrid.Y) as TowerDefenseZombie;
		await WaitFrames(6);
		Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == catapultCase.CharacterName, "The low-health regression must instantiate the real Catapult scene.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		CatapultComponent catapult = zombie.componentManager?.GetRuntime<CatapultComponent>();
		FireComponent fireComponent = zombie.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(catapult != null && !catapult.IsReleased && fireComponent != null && !fireComponent.IsReleased, "The low-health regression must bind the real Catapult and Fire runtimes.");
		if ((catapult?.IsReleased ?? true) || (fireComponent?.IsReleased ?? true))
		{
			zombie.QueueFree();
			await WaitFrames(3);
			return;
		}
		float startX = (float)(manager.GetMapGroundRight() + (double)manager.GetMapGridSize().X - 1.0);
		PrepareWalkingZombie(control, zombie, catapult, fireComponent, startX);
		catapult.speed = catapult.damageSpeedReduction;
		zombie.instance.hitpoints = 1.0;
		Check(Mathf.IsZeroApprox((float)catapult.lowHealthThreshold), "The standard Catapult must disable its low-health slowdown threshold.");
		Check(!zombie.sprite.Get("shake").AsBool(), "The standard Catapult must begin the low-health regression without random shake.");
		await WaitFrames(6);
		float x = zombie.GetLogicalGlobalPosition().X;
		Check(Mathf.IsEqualApprox((float)catapult.speed, (float)catapult.damageSpeedReduction), "Low health must retain the puncture speed instead of decelerating toward a stall.");
		Check(!zombie.sprite.Get("shake").AsBool(), "Low health must not enable per-display-frame random shake on the standard Catapult.");
		Check(x < startX - 0.1f, $"The low-health Catapult must keep moving; start={startX:F3}, end={x:F3}.");
		using Image proofImage = GetViewport().GetTexture().GetImage();
		Color pixel = proofImage.GetPixel(0, 0);
		int num = CountForegroundPixels(proofImage, pixel);
		Check(num > 100, $"The Vulkan frame must contain the real low-health Catapult pixels; foreground={num}.");
		string environment = OS.GetEnvironment("CATAPULT_PIXEL_PROOF_PATH");
		if (!string.IsNullOrWhiteSpace(environment))
		{
			proofImage.SavePng(environment);
		}
		GD.Print($"CATAPULT_LOW_HEALTH_PIXEL_PROOF renderer={RenderingServer.GetCurrentRenderingMethod()} foregroundPixels={num} width={proofImage.GetWidth()} height={proofImage.GetHeight()} capture={environment}");
		control.isGameRunning = false;
		zombie.QueueFree();
		await WaitFrames(3);
	}

	private async Task VerifyNoTargetKeepsMoving(CatapultSpawnTargetAcquisitionControlStub control, TowerDefenseManager manager, CatapultCase catapultCase)
	{
		control.isGameRunning = false;
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket(catapultCase.PacketPath);
		Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real " + catapultCase.Label + " packet must load for the no-target control.");
		TowerDefenseZombie zombie = towerDefensePacketConfig?.Spawn(TargetGrid.Y) as TowerDefenseZombie;
		await WaitFrames(6);
		Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == catapultCase.CharacterName, "The no-target control must instantiate the real " + catapultCase.Label + " scene.");
		if (GodotObject.IsInstanceValid(zombie))
		{
			CatapultComponent catapult = zombie.componentManager?.GetRuntime<CatapultComponent>();
			FireComponent fireComponent = zombie.componentManager?.GetRuntime<FireComponent>("character.fire");
			Check(catapult != null && !catapult.IsReleased && fireComponent != null && !fireComponent.IsReleased, catapultCase.Label + " must bind its real Catapult and Fire runtimes.");
			if ((catapult?.IsReleased ?? true) || (fireComponent?.IsReleased ?? true))
			{
				zombie.QueueFree();
				await WaitFrames(3);
				return;
			}
			float num = (float)(manager.GetMapGroundRight() - (double)manager.GetMapGridSize().X * catapult.idleBoundaryOffsetRatio);
			float x = manager.GetMapCellPosCenter(new Vector2I(manager.GetMapGridNum().X, TargetGrid.Y)).X;
			Check(Mathf.IsEqualApprox(num, x), $"{catapultCase.Label} must preserve the GD rightmost-cell center firing boundary; actual={num:F3}, expected={x:F3}.");
			float x2 = (float)(manager.GetMapGroundRight() + (double)manager.GetMapGridSize().X - 1.0);
			zombie.timeScaleInit = 3.0;
			zombie.timeScale = 3.0;
			PrepareWalkingZombie(control, zombie, catapult, fireComponent, x2);
			TowerDefenseProjectileCreateData primaryProjectileData = GetPrimaryProjectileData(fireComponent);
			Check(GodotObject.IsInstanceValid(primaryProjectileData), catapultCase.Label + " must retain its real primary projectile data.");
			bool flag = GodotObject.IsInstanceValid(primaryProjectileData) && fireComponent.CanFireCheckOnceByData(primaryProjectileData, -1, allowOutsideComponentBattlefield: true);
			Check(!flag, catapultCase.Label + " no-target control must not acquire an absent plant.");
			float startX = zombie.GetLogicalGlobalPosition().X;
			await WaitFrames(6);
			float x3 = zombie.GetLogicalGlobalPosition().X;
			float num2 = startX - x3;
			double num3 = catapult.speed * (6.0 / (double)Engine.PhysicsTicksPerSecond) * 3.0 * catapult.walkAnimationSpeedMultiplier * catapult.outsideMapSpeedMultiplier * (double)Math.Abs(zombie.transformPoint.Scale.X * zombie.Scale.X);
			Check(!catapult.isFire && zombie.CurrentStateHandle?.StableId == "zombie.walk", catapultCase.Label + " must remain in Walk when no target exists.");
			Check(x3 < startX - 0.1f, $"{catapultCase.Label} must keep moving forward when no target exists; start={startX:F3}, end={x3:F3}.");
			Check((double)num2 >= num3 * 0.5 && (double)num2 <= num3 * 1.5, $"{catapultCase.Label} accelerated movement must apply character time scale once; actual={num2:F3}, expected={num3:F3}.");
			control.isGameRunning = false;
			zombie.QueueFree();
			await WaitFrames(3);
		}
	}

	private async Task VerifyTargetAcquiredAtFireBoundary(CatapultSpawnTargetAcquisitionControlStub control, TowerDefenseManager manager, TowerDefensePlant wallnut, CatapultCase catapultCase)
	{
		control.isGameRunning = false;
		TowerDefensePacketConfig towerDefensePacketConfig = LoadPacket(catapultCase.PacketPath);
		Check(GodotObject.IsInstanceValid(towerDefensePacketConfig), "The real " + catapultCase.Label + " packet must load for target acquisition.");
		TowerDefenseZombie zombie = towerDefensePacketConfig?.Spawn(TargetGrid.Y) as TowerDefenseZombie;
		await WaitFrames(6);
		Check(GodotObject.IsInstanceValid(zombie) && zombie.config?.name == catapultCase.CharacterName, "The target case must instantiate the real " + catapultCase.Label + " scene.");
		if (!GodotObject.IsInstanceValid(zombie))
		{
			return;
		}
		CatapultComponent catapult = zombie.componentManager?.GetRuntime<CatapultComponent>();
		FireComponent fireComponent = zombie.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(catapult != null && !catapult.IsReleased && fireComponent != null && !fireComponent.IsReleased, catapultCase.Label + " target case must bind its real Catapult and Fire runtimes.");
		if ((catapult?.IsReleased ?? true) || (fireComponent?.IsReleased ?? true))
		{
			zombie.QueueFree();
			await WaitFrames(3);
			return;
		}
		float fireBoundary = (float)(manager.GetMapGroundRight() - (double)manager.GetMapGridSize().X * catapult.idleBoundaryOffsetRatio);
		float x = manager.GetMapCellPosCenter(new Vector2I(manager.GetMapGridNum().X, TargetGrid.Y)).X;
		Check(Mathf.IsEqualApprox(fireBoundary, x), $"{catapultCase.Label} target case must preserve the GD rightmost-cell center; actual={fireBoundary:F3}, expected={x:F3}.");
		float x2 = (float)(manager.GetMapGroundRight() + (double)manager.GetMapGridSize().X - 1.0);
		PrepareWalkingZombie(control, zombie, catapult, fireComponent, x2);
		TowerDefenseProjectileCreateData primaryProjectileData = GetPrimaryProjectileData(fireComponent);
		Check(GodotObject.IsInstanceValid(primaryProjectileData), catapultCase.Label + " target case must retain its real primary projectile data.");
		bool flag = GodotObject.IsInstanceValid(primaryProjectileData) && fireComponent.CanFireCheckOnceByData(primaryProjectileData, -1, allowOutsideComponentBattlefield: true);
		Check(flag && zombie.CanTarget(wallnut) && !catapult.CanStartFire(), catapultCase.Label + " may see the real Wall-nut while entering but must not fire outside.");
		fireComponent.checkIntreval = 0;
		fireComponent.isCheck = false;
		zombie.SetLogicalGlobalPosition(new Vector2(fireBoundary + 0.5f, zombie.GetLogicalGlobalPosition().Y));
		int acquisitionFrames;
		for (acquisitionFrames = 0; acquisitionFrames < 8; acquisitionFrames++)
		{
			if (catapult.isFire)
			{
				break;
			}
			await WaitFrames(1);
		}
		float acquiredX = zombie.GetLogicalGlobalPosition().X;
		Check(catapult.isFire && acquisitionFrames <= 8, $"{catapultCase.Label} must acquire after crossing the GD rightmost-cell boundary; frames={acquisitionFrames}, x={acquiredX:F3}.");
		Check(Mathf.Abs(acquiredX - fireBoundary) < 2f, $"{catapultCase.Label} must stop at the GD rightmost-cell boundary before firing; x={acquiredX:F3}, boundary={fireBoundary:F3}.");
		Check(zombie.CurrentStateHandle?.StableId != "zombie.walk", catapultCase.Label + " must leave Walk as soon as the target is acquired.");
		await WaitFrames(4);
		float x3 = zombie.GetLogicalGlobalPosition().X;
		Check(Mathf.Abs(x3 - acquiredX) < 0.05f, $"{catapultCase.Label} must stop after acquisition instead of sliding toward the old gate; acquired={acquiredX:F3}, end={x3:F3}.");
		control.isGameRunning = false;
		zombie.QueueFree();
		await WaitFrames(3);
	}

	private static void PrepareWalkingZombie(CatapultSpawnTargetAcquisitionControlStub control, TowerDefenseZombie zombie, CatapultComponent catapult, FireComponent fire, float x)
	{
		zombie.inGame = true;
		zombie.die = false;
		zombie.nearDie = false;
		zombie.ProcessMode = ProcessModeEnum.Inherit;
		zombie.SetLogicalGlobalPosition(new Vector2(x, zombie.GetLogicalGlobalPosition().Y));
		zombie.gridPos = new Vector2I(-1, TargetGrid.Y);
		catapult.isFire = false;
		catapult.fireOver = false;
		fire.timer = 0f;
		fire.checkIntreval = 0;
		fire.isCheck = false;
		zombie.Walk();
		control.isGameRunning = true;
	}

	private static TowerDefenseProjectileCreateData GetPrimaryProjectileData(FireComponent fire)
	{
		if (fire?.fireCheckList == null || fire.fireCheckList.Count == 0)
		{
			return null;
		}
		FireComponentCheckConfig fireComponentCheckConfig = fire.fireCheckList[0];
		if (!GodotObject.IsInstanceValid(fireComponentCheckConfig?.projectile))
		{
			return null;
		}
		return fireComponentCheckConfig.projectile.GetProjectile();
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl, Vector2I gridNum, Vector2 gridSize, Vector2 gridBeginPos)
	{
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = new TowerDefenseMapConfig
			{
				gridNum = gridNum,
				gridSize = gridSize,
				gridBeginPos = gridBeginPos
			}
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
		towerDefenseBattleFeatureMap.PlantGridInit();
		return towerDefenseBattleFeatureMap;
	}

	private static int CountForegroundPixels(Image image, Color background)
	{
		int num = 0;
		for (int i = 0; i < image.GetHeight(); i += 2)
		{
			for (int j = 0; j < image.GetWidth(); j += 2)
			{
				Color pixel = image.GetPixel(j, i);
				if (Mathf.Abs(pixel.R - background.R) + Mathf.Abs(pixel.G - background.G) + Mathf.Abs(pixel.B - background.B) + Mathf.Abs(pixel.A - background.A) > 0.08f)
				{
					num++;
				}
			}
		}
		return num;
	}

	private static TowerDefensePacketConfig LoadPacket(string path)
	{
		return ResourceLoader.Load<TowerDefensePacketConfig>(path, null, ResourceLoader.CacheMode.Ignore)?.Duplicate(deep: true) as TowerDefensePacketConfig;
	}

	private void RegisterRealFixtures()
	{
		RegisterPacket("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Packet/PlantWallnut.tres");
		RegisterCharacter("PlantWallnut", "res://Asset/Anime/Character/Plant/Chapter0/Wallnut/Scene/TowerDefensePlantWallnut.tscn");
		CatapultCase[] catapultCases = CatapultCases;
		for (int i = 0; i < catapultCases.Length; i++)
		{
			CatapultCase catapultCase = catapultCases[i];
			RegisterPacket(catapultCase.CharacterName, catapultCase.PacketPath);
			RegisterCharacter(catapultCase.CharacterName, catapultCase.ScenePath);
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
			GD.PushError("[BugOverviewCatapultSpawnTargetAcquisitionRuntimeTest] " + message);
		}
	}
}
