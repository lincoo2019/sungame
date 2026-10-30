using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class EdgarFireballDurabilityRuntimeTest : Node
{
	private const string BossScenePath = "res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn";

	private const string PlantScenePath = "res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn";

	private static readonly Vector2 GridBegin = new Vector2(100f, 100f);

	private static readonly Vector2 GridSize = new Vector2(100f, 76f);

	private static readonly Vector2I GridNum = new Vector2I(9, 5);

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			await Run();
		}
		catch (Exception value)
		{
			_failures++;
			GD.PushError($"EDGAR_FIREBALL_DURABILITY_FAILURE Unhandled test exception:\n{value}");
		}
		bool flag = _failures == 0;
		GD.Print($"EDGAR_FIREBALL_DURABILITY_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private async Task Run()
	{
		Check(!BulletField.ShouldDurabilityBlockingSweepStop(1999.0, 2000.0), "1999 durability must not stop the fireball.");
		Check(BulletField.ShouldDurabilityBlockingSweepStop(2000.0, 2000.0), "Exactly 2000 durability must stop the fireball.");
		Check(BulletField.ShouldDurabilityBlockingSweepStop(2001.0, 2000.0), "Durability above 2000 must stop the fireball.");
		TowerDefenseCharacterInstance towerDefenseCharacterInstance = new TowerDefenseCharacterInstance
		{
			hitpoints = 1200.0
		};
		TowerDefenseArmorInstance item = new TowerDefenseArmorInstance
		{
			hitPoints = 800.0,
			armorMethodFlags = 4
		};
		TowerDefenseArmorInstance item2 = new TowerDefenseArmorInstance
		{
			hitPoints = 5000.0,
			armorMethodFlags = 2048
		};
		towerDefenseCharacterInstance.armorList.Add(item);
		towerDefenseCharacterInstance.armorList.Add(item2);
		towerDefenseCharacterInstance.armorShield.Add(item);
		towerDefenseCharacterInstance.armorHeadCover.Add(item2);
		towerDefenseCharacterInstance.RefreshArmorRuntimeIndex();
		int damageFlags = 3;
		Check(Mathf.IsEqualApprox((float)towerDefenseCharacterInstance.GetProjectileDamageableDurability(damageFlags), 2000f), "Body plus damageable shield must total exactly 2000; excluded head cover must not count.");
		Check(BulletField.ShouldDurabilityBlockingSweepStop(towerDefenseCharacterInstance.GetProjectileDamageableDurability(damageFlags), 2000.0), "Body plus armor at the threshold must stop the fireball.");
		StringName registryKey = "ZombieBossEdgarIIFireball";
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/EdgarII/ZombieBossEdgarIIFireball.tres", null, ResourceLoader.CacheMode.Reuse);
		Check(towerDefenseProjectileConfig?.hitEffect == null, "Authored fireball must not instantiate a pea-cannon explosion effect.");
		Check(string.IsNullOrEmpty(towerDefenseProjectileConfig?.splatAudio), "Authored fireball must not play an explosion splat sound.");
		ResourceManager resourceManager = ResourceManager.Instance;
		Resource previousConfig;
		bool hadConfig = resourceManager.PROJECTILE_CONFIG.TryGetValue("ZombieBossEdgarIIFireball", out previousConfig);
		TowerDefenseProjectileData previousData;
		bool hadData = TowerDefenseProjectileRegistry.ProjectileDictionary.TryGetValue(registryKey, out previousData);
		TowerDefenseProjectileSkinData previousSkins;
		bool hadSkins = TowerDefenseProjectileRegistry.ProjectileSkinDictionary.TryGetValue(registryKey, out previousSkins);
		TowerDefenseProjectileConfig runtimeConfig = towerDefenseProjectileConfig?.Duplicate(deep: true) as TowerDefenseProjectileConfig;
		TowerDefenseProjectileData registryData = new TowerDefenseProjectileData
		{
			name = "ZombieBossEdgarIIFireball",
			baseDamage = (runtimeConfig?.baseDamage ?? 2000.0),
			projectileScene = runtimeConfig?.projectileScene
		};
		try
		{
			resourceManager.PROJECTILE_CONFIG["ZombieBossEdgarIIFireball"] = runtimeConfig;
			TowerDefenseProjectileRegistry.RegisterProjectile(registryKey, registryData);
			TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = new TowerDefenseProjectileCreateData(registryKey).BuildConfig();
			Check(towerDefenseProjectileConfig2 != null && towerDefenseProjectileConfig2.useDurabilityBlockingSweep && Mathf.IsEqualApprox((float)towerDefenseProjectileConfig2.durabilityBlockingThreshold, 2000f), "Registry BuildConfig reconstruction must preserve durability blocking fields.");
			await VerifyProductionFireball(runtimeConfig);
		}
		finally
		{
			if (hadConfig)
			{
				resourceManager.PROJECTILE_CONFIG["ZombieBossEdgarIIFireball"] = previousConfig;
			}
			else
			{
				resourceManager.PROJECTILE_CONFIG.Remove("ZombieBossEdgarIIFireball");
			}
			if (hadData)
			{
				TowerDefenseProjectileRegistry.ProjectileDictionary[registryKey] = previousData;
			}
			else
			{
				TowerDefenseProjectileRegistry.ProjectileDictionary.Remove(registryKey);
			}
			if (hadSkins)
			{
				TowerDefenseProjectileRegistry.ProjectileSkinDictionary[registryKey] = previousSkins;
			}
			else
			{
				TowerDefenseProjectileRegistry.ProjectileSkinDictionary.Remove(registryKey);
			}
			registryData.Dispose();
			runtimeConfig?.Dispose();
		}
	}

	private async Task VerifyProductionFireball(TowerDefenseProjectileConfig authored)
	{
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		TowerDefenseControlNew previousControl = manager?.currentControl;
		Vector2 previousGridBegin = manager?.gridBeginPos ?? Vector2.Zero;
		Vector2 previousGridSize = manager?.gridSize ?? Vector2.Zero;
		Vector2I previousGridNum = manager?.gridNum ?? Vector2I.Zero;
		ProjectileUpdateManager updateManager = ProjectileUpdateManager.Instance;
		ProcessModeEnum previousUpdateMode = updateManager?.ProcessMode ?? ProcessModeEnum.Inherit;
		EdgarFireballDurabilityControlStub control = null;
		TowerDefenseMapControl mapControl = null;
		TowerDefenseBattleFeatureMap mapFeature = null;
		TowerDefenseZombieBossEdgarII boss = null;
		TowerDefensePlant plant = null;
		TowerDefensePlant rearPlant = null;
		TowerDefenseZombieBossEdgarII entryBoss = null;
		TowerDefensePlant pulsePlantA = null;
		TowerDefensePlant pulsePlantB = null;
		BulletField field = null;
		int bulletIndex = -1;
		bool ownsField = false;
		try
		{
			Check(GodotObject.IsInstanceValid(manager), "TowerDefenseManager autoload must be available for the production fireball path.");
			if (!GodotObject.IsInstanceValid(manager))
			{
				throw new InvalidOperationException("TowerDefenseManager is unavailable.");
			}
			control = new EdgarFireballDurabilityControlStub
			{
				Name = "EdgarFireballDurabilityControl",
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
			manager.gridBeginPos = GridBegin;
			manager.gridSize = GridSize;
			manager.gridNum = GridNum;
			mapControl = new TowerDefenseMapControl
			{
				Name = "MapControl"
			};
			mapFeature = CreateMapFeature(mapControl);
			mapFeature.control = control;
			control.featureDictionary[new StringName("Map")] = mapFeature;
			if (GodotObject.IsInstanceValid(updateManager))
			{
				updateManager.ProcessMode = ProcessModeEnum.Disabled;
			}
			boss = Instantiate<TowerDefenseZombieBossEdgarII>("res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn");
			plant = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
			rearPlant = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
			Check(GodotObject.IsInstanceValid(boss) && GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(rearPlant), "Real Edgar II and PeaShooter scenes must instantiate for projectile damage verification.");
			if (!GodotObject.IsInstanceValid(boss) || !GodotObject.IsInstanceValid(plant) || !GodotObject.IsInstanceValid(rearPlant))
			{
				throw new InvalidOperationException("A real character fixture failed to instantiate.");
			}
			PlaceCharacter(boss, new Vector2I(9, 3));
			PlaceCharacter(plant, new Vector2I(5, 3));
			PlaceCharacter(rearPlant, new Vector2I(3, 3));
			control.characterNode.AddChild(boss, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(plant, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(rearPlant, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			boss.ProcessMode = ProcessModeEnum.Disabled;
			plant.ProcessMode = ProcessModeEnum.Disabled;
			rearPlant.ProcessMode = ProcessModeEnum.Disabled;
			plant.instance.physiqueTypeFlags |= 1024;
			Check(plant.instance.GetProjectileDamageableDurability(authored.damageFlags) < 2000.0, "The CANT_PENETRATE passthrough fixture must remain below the authored 2000 durability threshold.");
			TowerDefenseEnum.CHARACTER_CAMP camp = boss.camp;
			boss.Hypnoses();
			Check(!boss.instance.hypnoses && boss.camp == camp, "Edgar II must reject the real hypnoses path and remain in the zombie camp.");
			MethodInfo method = typeof(TowerDefenseZombieBossEdgarII).GetMethod("ResolveFireball", BindingFlags.Instance | BindingFlags.NonPublic);
			Check(method != null, "Edgar II must retain a production fireball resolution entry point.");
			if (method == null)
			{
				throw new MissingMethodException("TowerDefenseZombieBossEdgarII", "ResolveFireball");
			}
			int num = BulletField.Instance?.ActiveCount ?? 0;
			bool flag = GodotObject.IsInstanceValid(BulletField.Instance);
			method.Invoke(boss, null);
			field = BulletField.Instance;
			ownsField = !flag && GodotObject.IsInstanceValid(field);
			bulletIndex = field?.LastSpawnedIndex ?? (-1);
			bool flag2 = GodotObject.IsInstanceValid(field) && field.ActiveCount == num + 1 && bulletIndex >= 0 && field.IsBulletActive(bulletIndex);
			Check(flag2, "Edgar II ResolveFireball must emit one real BulletField projectile.");
			if (!flag2)
			{
				throw new InvalidOperationException("Edgar II did not emit a BulletField projectile.");
			}
			ref BulletData bulletDataRef = ref field.GetBulletDataRef(bulletIndex);
			Check(Mathf.IsZeroApprox((float)bulletDataRef.height) && Mathf.IsEqualApprox(bulletDataRef.pos.Y, boss.GetLogicalGlobalPosition().Y), $"Fireball visual and collision anchors must share Edgar II's current lane; pos={bulletDataRef.pos}, height={bulletDataRef.height}.");
			Check(bulletDataRef.renderMode == BulletRenderMode.ANIMATED_MESH, $"Fireball must use the animated FirePea render path; renderMode={bulletDataRef.renderMode}.");
			Check(bulletDataRef.animFrameMax == 24 && Mathf.IsEqualApprox((float)bulletDataRef.animFrameRate, 24f), $"Fireball must retain FirePea frames 0-24 at its effective 24 FPS; frameMax={bulletDataRef.animFrameMax}, fps={bulletDataRef.animFrameRate}.");
			Check(bulletDataRef.config.scale == new Vector2(2.25f, 2.25f), $"Fireball must enlarge the reused FirePea animation by 2.25x; scale={bulletDataRef.config.scale}.");
			Check(Mathf.IsZeroApprox(bulletDataRef.rotateScale), $"Animated FirePea must not use the old static-art tumble workaround; rotateScale={bulletDataRef.rotateScale}.");
			Check(bulletDataRef.flipX, "Left-moving Edgar II fireball must horizontally flip the right-facing FirePea art.");
			Check(bulletDataRef.collisionFlags == authored.collisionFlags && bulletDataRef.collisionFlags == 63, $"Fireball must retain its all-plant collision mask; collisionFlags={bulletDataRef.collisionFlags}.");
			float rotation = bulletDataRef.rotation;
			ulong num2 = Engine.GetPhysicsFrames() + 101;
			field.Update(0.25, num2);
			ref BulletData bulletDataRef2 = ref field.GetBulletDataRef(bulletIndex);
			Check(Mathf.IsEqualApprox(bulletDataRef2.rotation, rotation), $"Animated FirePea must preserve its directional pose without tumbling; rotation={bulletDataRef2.rotation}.");
			Check(bulletDataRef2.gridY == boss.gridPos.Y, $"Fireball collision plane must remain in Edgar II's lane; gridY={bulletDataRef2.gridY}.");
			double hitpoints = plant.instance.hitpoints;
			for (int i = 0; i < 30; i++)
			{
				if (!field.IsBulletActive(bulletIndex))
				{
					break;
				}
				if (!Mathf.IsEqualApprox((float)plant.instance.hitpoints, (float)hitpoints))
				{
					break;
				}
				field.Update(0.05, (ulong)((long)num2 + (long)i + 1));
			}
			Check(plant.instance.hitpoints < hitpoints, $"The real fireball must naturally reach and damage a real plant; hp={plant.instance.hitpoints}/{hitpoints}.");
			TowerDefenseEffectSpriteOnceBatcher nodeOrNull = control.characterNode.GetNodeOrNull<TowerDefenseEffectSpriteOnceBatcher>("TowerDefenseEffectSpriteOnceBatcher");
			AnimateMultiMeshRenderer animateMultiMeshRenderer = nodeOrNull?.GetNodeOrNull<AnimateMultiMeshRenderer>("EffectSpriteOnceAnimateMultiMeshRenderer");
			nodeOrNull?._Process(0.0);
			Check(GodotObject.IsInstanceValid(animateMultiMeshRenderer) && animateMultiMeshRenderer.GetVisibleInstanceCountForTest() > 0, "A real fireball hit must publish a visible one-shot FireSplats instance.");
			Check(field.IsBulletActive(bulletIndex), "A sub-2000 CANT_PENETRATE plant must not stop Edgar II's durability-sweep fireball.");
			double hitpoints2 = rearPlant.instance.hitpoints;
			for (int j = 0; j < 40; j++)
			{
				if (!field.IsBulletActive(bulletIndex))
				{
					break;
				}
				if (!Mathf.IsEqualApprox((float)rearPlant.instance.hitpoints, (float)hitpoints2))
				{
					break;
				}
				field.Update(0.05, (ulong)((long)num2 + (long)j + 101));
			}
			Check(rearPlant.instance.hitpoints < hitpoints2, "A fireball crossing a sub-2000 blocker must continue into the next real plant in the lane.");
			if (field.IsBulletActive(bulletIndex))
			{
				field.Despawn(bulletIndex);
			}
			bulletIndex = -1;
			await VerifyExactThresholdStopsFireball(control, boss, field, method);
			entryBoss = await VerifyEntryAndIdleMovement(control);
			VerifySkillSelectionContract(entryBoss);
			(pulsePlantA, pulsePlantB) = await VerifyPulsePerPlant(control, boss);
			await VerifyDeathPlayback(control, entryBoss);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(field) && field.IsBulletActive(bulletIndex))
			{
				field.Despawn(bulletIndex);
			}
			if (GodotObject.IsInstanceValid(updateManager))
			{
				updateManager.ProcessMode = previousUpdateMode;
			}
			if (GodotObject.IsInstanceValid(boss))
			{
				boss.QueueFree();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(rearPlant))
			{
				rearPlant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(entryBoss))
			{
				entryBoss.QueueFree();
			}
			if (GodotObject.IsInstanceValid(pulsePlantA))
			{
				pulsePlantA.QueueFree();
			}
			if (GodotObject.IsInstanceValid(pulsePlantB))
			{
				pulsePlantB.QueueFree();
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
			if (ownsField && GodotObject.IsInstanceValid(field))
			{
				field.QueueFree();
			}
			if (GodotObject.IsInstanceValid(control))
			{
				control.QueueFree();
			}
			await WaitFrames(3);
		}
	}

	private async Task VerifyExactThresholdStopsFireball(EdgarFireballDurabilityControlStub control, TowerDefenseZombieBossEdgarII boss, BulletField field, MethodInfo resolveFireball)
	{
		TowerDefensePlant blocker = null;
		TowerDefensePlant behind = null;
		int thresholdBulletIndex = -1;
		try
		{
			blocker = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
			behind = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
			Check(GodotObject.IsInstanceValid(blocker) && GodotObject.IsInstanceValid(behind), "Threshold verification must instantiate two real plants.");
			if (!GodotObject.IsInstanceValid(blocker) || !GodotObject.IsInstanceValid(behind))
			{
				throw new InvalidOperationException("Threshold plant fixture failed to instantiate.");
			}
			PlaceCharacter(blocker, new Vector2I(5, 4));
			PlaceCharacter(behind, new Vector2I(3, 4));
			control.characterNode.AddChild(blocker, forceReadableName: false, InternalMode.Disabled);
			control.characterNode.AddChild(behind, forceReadableName: false, InternalMode.Disabled);
			await WaitFrames(4);
			blocker.ProcessMode = ProcessModeEnum.Disabled;
			behind.ProcessMode = ProcessModeEnum.Disabled;
			blocker.instance.hitpoints = 2000.0;
			Check(Mathf.IsEqualApprox((float)blocker.instance.GetProjectileDamageableDurability(TowerDefenseManager.GetProjectileConfig("ZombieBossEdgarIIFireball").damageFlags), 2000f), "The stopping fixture must expose exactly 2000 damageable durability through the production API.");
			boss.gridPos = new Vector2I(9, 4);
			boss.SetLogicalGlobalPosition(TowerDefenseManager.GetMapCellPlantPos(boss.gridPos));
			double hitpoints = behind.instance.hitpoints;
			resolveFireball.Invoke(boss, null);
			thresholdBulletIndex = field.LastSpawnedIndex;
			ulong num = Engine.GetPhysicsFrames() + 1001;
			for (int i = 0; i < 60; i++)
			{
				if (!field.IsBulletActive(thresholdBulletIndex))
				{
					break;
				}
				field.Update(0.05, num + (ulong)i);
			}
			Check(blocker.instance.hitpoints < 2000.0 && !field.IsBulletActive(thresholdBulletIndex), "A real plant with exactly 2000 durability must take the hit and stop the fireball.");
			Check(Mathf.IsEqualApprox((float)behind.instance.hitpoints, (float)hitpoints), "A plant behind an exact-2000 blocker must remain unharmed.");
		}
		finally
		{
			if (thresholdBulletIndex >= 0 && field.IsBulletActive(thresholdBulletIndex))
			{
				field.Despawn(thresholdBulletIndex);
			}
			if (GodotObject.IsInstanceValid(blocker))
			{
				blocker.QueueFree();
			}
			if (GodotObject.IsInstanceValid(behind))
			{
				behind.QueueFree();
			}
			await WaitFrames(3);
		}
	}

	private async Task<TowerDefenseZombieBossEdgarII> VerifyEntryAndIdleMovement(EdgarFireballDurabilityControlStub control)
	{
		TowerDefenseZombieBossEdgarII entryBoss = Instantiate<TowerDefenseZombieBossEdgarII>("res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn");
		Check(GodotObject.IsInstanceValid(entryBoss), "Entry verification must instantiate a second real Edgar II.");
		if (!GodotObject.IsInstanceValid(entryBoss))
		{
			throw new InvalidOperationException("Edgar II entry fixture failed to instantiate.");
		}
		Vector2I homeGrid = new Vector2I(9, 2);
		Vector2 homePosition = TowerDefenseManager.GetMapCellPlantPos(homeGrid);
		Vector2 spawnPosition = homePosition + new Vector2(140f, 0f);
		entryBoss.gridPos = new Vector2I(-1, homeGrid.Y);
		entryBoss.inGame = true;
		entryBoss.Position = spawnPosition;
		control.isGameRunning = true;
		control.characterNode.AddChild(entryBoss, forceReadableName: false, InternalMode.Disabled);
		await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
		entryBoss.CallDeferred("Walk");
		await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		Dictionary dictionary = entryBoss.ExportVariantSave();
		Check(dictionary.GetValueOrDefault("action", -1).AsInt32() == 7 && entryBoss.GetLogicalGlobalPosition().IsEqualApprox(spawnPosition), "A freshly spawned Edgar II must preserve the off-map sentinel through the formal deferred Walk call.");
		await WaitFrames(90);
		EdgarFireballDurabilityRuntimeTest edgarFireballDurabilityRuntimeTest = this;
		GroundMoveComponent groundMoveComponent = entryBoss.groundMoveComponent;
		edgarFireballDurabilityRuntimeTest.Check(groundMoveComponent != null && groundMoveComponent.Alive && entryBoss.GetLogicalGlobalPosition().X < spawnPosition.X - 0.01f, $"Edgar II must visibly advance from the wave spawn point through GroundMove; alive={entryBoss.groundMoveComponent?.Alive}, source={entryBoss.groundMoveComponent?.HasMovementSource}, position={entryBoss.GetLogicalGlobalPosition()}, spawn={spawnPosition}, state={entryBoss.CurrentStateHandle?.StableId}.");
		entryBoss.ProcessMode = ProcessModeEnum.Disabled;
		entryBoss.SetLogicalGlobalPosition(homePosition - new Vector2(1f, 0f));
		InvokePrivate(entryBoss, "AdvanceEntry");
		Dictionary dictionary2 = entryBoss.ExportVariantSave();
		EdgarFireballDurabilityRuntimeTest edgarFireballDurabilityRuntimeTest2 = this;
		int condition;
		if (dictionary2.GetValueOrDefault("action", -1).AsInt32() == 0 && entryBoss.gridPos == homeGrid && entryBoss.GetLogicalGlobalPosition().IsEqualApprox(homePosition))
		{
			double num = dictionary2.GetValueOrDefault("idleMoveWaitDuration", 0.0).AsDouble();
			condition = ((num >= 3.0 && num <= 6.0) ? 1 : 0);
		}
		else
		{
			condition = 0;
		}
		edgarFireballDurabilityRuntimeTest2.Check((byte)condition != 0, "Edgar II must stop at its current row's ninth column and enter Idle after crossing home.");
		Dictionary dictionary3 = entryBoss.ExportVariantSave();
		dictionary3["action"] = 0;
		dictionary3["idleTimer"] = 5.0;
		dictionary3["idleMoveWaitTimer"] = 0.0;
		dictionary3["idleMoveWaitDuration"] = 0.1;
		dictionary3["idleRowMoveActive"] = false;
		dictionary3["skillPendingAfterRowMove"] = false;
		entryBoss.ImportVariantSave(dictionary3);
		entryBoss.BatchUpdate(0.11);
		Dictionary dictionary4 = entryBoss.ExportVariantSave();
		Check(dictionary4.GetValueOrDefault("idleRowMoveActive", false).AsBool() && dictionary4.GetValueOrDefault("action", -1).AsInt32() == 0 && dictionary4.GetValueOrDefault("idleRowMoveDuration", 0.0).AsDouble() > 0.0, "The real idle scheduler must start a row change without consuming an active-skill slot.");
		double num2 = dictionary4.GetValueOrDefault("idleRowMoveDuration", 0.0).AsDouble();
		Vector2 other = new Vector2(dictionary4.GetValueOrDefault("idleRowMoveStartX", 0.0).AsSingle(), dictionary4.GetValueOrDefault("idleRowMoveStartY", 0.0).AsSingle());
		Vector2 other2 = new Vector2(dictionary4.GetValueOrDefault("idleRowMoveEndX", 0.0).AsSingle(), dictionary4.GetValueOrDefault("idleRowMoveEndY", 0.0).AsSingle());
		entryBoss.BatchUpdate(num2 * 0.5);
		Vector2 logicalGlobalPosition = entryBoss.GetLogicalGlobalPosition();
		Check(Mathf.IsEqualApprox(logicalGlobalPosition.X, other.X) && !logicalGlobalPosition.IsEqualApprox(other) && !logicalGlobalPosition.IsEqualApprox(other2), "Idle row changes must visibly interpolate instead of teleporting or becoming an active skill.");
		entryBoss.BatchUpdate(num2 * 0.5 + 0.01);
		Dictionary dictionary5 = entryBoss.ExportVariantSave();
		Check(entryBoss.gridPos.X == 9 && entryBoss.GetLogicalGlobalPosition().IsEqualApprox(other2) && dictionary5.GetValueOrDefault("action", -1).AsInt32() == 0 && dictionary5.GetValueOrDefault("idleTimer", 0.0).AsDouble() > 5.0, "Finishing a random idle row change must preserve the same 10-second skill countdown.");
		int num3 = ((entryBoss.gridPos.Y != 1) ? 1 : GridNum.Y);
		Vector2 mapCellPlantPos = TowerDefenseManager.GetMapCellPlantPos(new Vector2I(9, num3));
		Dictionary dictionary6 = entryBoss.ExportVariantSave();
		dictionary6["action"] = 0;
		dictionary6["idleTimer"] = 9.9;
		dictionary6["idleRowMoveActive"] = true;
		dictionary6["idleRowMoveTimer"] = 0.0;
		dictionary6["idleRowMoveDuration"] = 1.0;
		dictionary6["idleRowMoveStartX"] = other2.X;
		dictionary6["idleRowMoveStartY"] = other2.Y;
		dictionary6["idleRowMoveEndX"] = mapCellPlantPos.X;
		dictionary6["idleRowMoveEndY"] = mapCellPlantPos.Y;
		dictionary6["idleRowMoveTargetRow"] = num3;
		entryBoss.ImportVariantSave(dictionary6);
		entryBoss.BatchUpdate(0.2);
		Dictionary dictionary7 = entryBoss.ExportVariantSave();
		int num4 = dictionary7.GetValueOrDefault("action", -1).AsInt32();
		bool flag = ((num4 == 1 || (uint)(num4 - 4) <= 2u) ? true : false);
		Check(flag && !dictionary7.GetValueOrDefault("idleRowMoveActive", true).AsBool() && Mathf.IsZeroApprox((float)dictionary7.GetValueOrDefault("idleTimer", -1.0).AsDouble()), "The 10-second active-skill boundary must not wait for a row change to finish.");
		control.isGameRunning = false;
		return entryBoss;
	}

	private void VerifySkillSelectionContract(TowerDefenseZombieBossEdgarII boss)
	{
		MethodInfo method = typeof(TowerDefenseZombieBossEdgarII).GetMethod("PickSkillByRoll", BindingFlags.Static | BindingFlags.NonPublic);
		MethodInfo method2 = typeof(TowerDefenseZombieBossEdgarII).GetMethod("SelectNextSkillByRoll", BindingFlags.Instance | BindingFlags.NonPublic);
		MethodInfo method3 = typeof(TowerDefenseZombieBossEdgarII).GetMethod("StartNextSkill", BindingFlags.Instance | BindingFlags.NonPublic);
		Check(method != null && method2 != null && method3 != null, "Edgar II must expose deterministic weighted-selection helpers to test the authored probability buckets.");
		if (method == null || method2 == null || method3 == null)
		{
			throw new MissingMethodException("TowerDefenseZombieBossEdgarII", "weighted skill selection");
		}
		int[] array = new int[6] { 0, 0, 5, 12, 8, 15 };
		int[] array2 = new int[6];
		for (int i = 0; i < 40; i++)
		{
			int num = (int)method.Invoke(null, new object[2] { i, 0 });
			array2[num]++;
		}
		Check(array2[2] == 5 && array2[3] == 12 && array2[4] == 8 && array2[5] == 15, "The 40 authored buckets must be Smash=5, Fire=12, Pulse=8 and Summon=15.");
		bool flag = true;
		for (int j = 2; j <= 5; j++)
		{
			System.Array.Clear(array2, 0, array2.Length);
			int num2 = 40 - array[j];
			for (int k = 0; k < num2; k++)
			{
				int num3 = (int)method.Invoke(null, new object[2] { k, j });
				array2[num3]++;
			}
			for (int l = 2; l <= 5; l++)
			{
				int num4 = ((l != j) ? array[l] : 0);
				flag &= array2[l] == num4;
			}
		}
		Check(flag, "Every twice-used skill must be removed while all remaining authored bucket weights stay intact.");
		Dictionary dictionary = boss.ExportVariantSave();
		dictionary["schemaVersion"] = 3;
		dictionary["action"] = 0;
		dictionary["lastSkill"] = 3;
		dictionary["skillRepeatCount"] = 1;
		boss.ImportVariantSave(dictionary);
		int num5 = (int)method2.Invoke(boss, new object[1] { 5 });
		Dictionary dictionary2 = boss.ExportVariantSave();
		Check(num5 == 3 && dictionary2.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == 2, "The same active skill must remain eligible for a second consecutive use.");
		Check(dictionary2.GetValueOrDefault("schemaVersion", 0).AsInt32() >= 3 && dictionary2.GetValueOrDefault("lastSkill", 0).AsInt32() == 3 && dictionary2.ContainsKey("skillRepeatCount"), "Persisted Edgar II state must carry the versioned consecutive-skill counter.");
		Dictionary dictionary3 = boss.ExportNetworkSpecialState();
		Check(dictionary3.GetValueOrDefault("lastSkill", 0).AsInt32() == 3 && dictionary3.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == 2, "Network special state must replicate the exact consecutive-skill guard.");
		Dictionary dictionary4 = dictionary2.Duplicate();
		dictionary4["lastSkill"] = 2;
		dictionary4["skillRepeatCount"] = 1;
		boss.ImportVariantSave(dictionary4);
		boss.ImportVariantSave(dictionary2);
		Dictionary dictionary5 = boss.ExportVariantSave();
		Check(dictionary5.GetValueOrDefault("lastSkill", 0).AsInt32() == 3 && dictionary5.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == 2, "Importing Edgar II state must restore the exact consecutive-skill guard.");
		int num6 = (int)method2.Invoke(boss, new object[1] { 5 });
		Dictionary dictionary6 = boss.ExportVariantSave();
		Check(num6 != 3 && dictionary6.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == 1, "A skill used twice consecutively must be excluded from the third draw.");
		Dictionary dictionary7 = dictionary2.Duplicate();
		dictionary7["schemaVersion"] = 2;
		dictionary7["lastSkill"] = 4;
		dictionary7.Remove("skillRepeatCount");
		boss.ImportVariantSave(dictionary7);
		Dictionary dictionary8 = boss.ExportVariantSave();
		Check(dictionary8.GetValueOrDefault("lastSkill", 0).AsInt32() == 4 && dictionary8.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == 1, "A v2 save must migrate its last skill as one prior use, not as an unguarded or double use.");
		int num7 = 4;
		int num8 = 1;
		bool flag2 = true;
		for (int m = 0; m < 256; m++)
		{
			method3.Invoke(boss, null);
			Dictionary dictionary9 = boss.ExportVariantSave();
			int num9 = dictionary9.GetValueOrDefault("lastSkill", 0).AsInt32();
			num8 = ((num9 != num7) ? 1 : (num8 + 1));
			int num10 = dictionary9.GetValueOrDefault("action", -1).AsInt32();
			int num11 = num9 switch
			{
				2 => 1, 
				3 => 4, 
				4 => 5, 
				5 => 6, 
				_ => -1, 
			};
			flag2 &= num8 <= 2 && dictionary9.GetValueOrDefault("skillRepeatCount", 0).AsInt32() == num8 && num10 == num11;
			num7 = num9;
		}
		Check(flag2, "The formal StartNextSkill entry must never produce a third repeat and must start the selected action.");
	}

	private async Task<(TowerDefensePlant, TowerDefensePlant)> VerifyPulsePerPlant(EdgarFireballDurabilityControlStub control, TowerDefenseZombieBossEdgarII boss)
	{
		TowerDefensePlant plantA = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
		TowerDefensePlant plantB = Instantiate<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Chapter0/PeaShooter/Scene/TowerDefensePlantPeaShooter.tscn");
		Check(GodotObject.IsInstanceValid(plantA) && GodotObject.IsInstanceValid(plantB), "Pulse verification must instantiate two real plants in the same cell.");
		if (!GodotObject.IsInstanceValid(plantA) || !GodotObject.IsInstanceValid(plantB))
		{
			throw new InvalidOperationException("Edgar II pulse fixture failed to instantiate plants.");
		}
		PlaceCharacter(plantA, new Vector2I(4, 1));
		PlaceCharacter(plantB, new Vector2I(4, 1));
		control.characterNode.AddChild(plantA, forceReadableName: false, InternalMode.Disabled);
		control.characterNode.AddChild(plantB, forceReadableName: false, InternalMode.Disabled);
		await WaitFrames(4);
		plantA.ProcessMode = ProcessModeEnum.Disabled;
		plantB.ProcessMode = ProcessModeEnum.Disabled;
		control.RegisterSyncCharacter(9101, plantA);
		control.RegisterSyncCharacter(9102, plantB);
		int childCount = plantA.frontEffectNode.GetChildCount();
		int childCount2 = plantB.frontEffectNode.GetChildCount();
		Dictionary dictionary = boss.ExportVariantSave();
		dictionary["action"] = 5;
		boss.ImportVariantSave(dictionary);
		boss.AnimeEvent("pulse", default(Variant));
		Check(Mathf.IsEqualApprox((float)plantA.buff.GetAttackSpeedMultiplier(), 0.5f) && Mathf.IsEqualApprox((float)plantB.buff.GetAttackSpeedMultiplier(), 0.5f), "The suppression pulse must apply a 0.5 attack cadence multiplier to every affected plant.");
		Check(plantA.frontEffectNode.GetChildCount() == childCount + 1 && plantB.frontEffectNode.GetChildCount() == childCount2 + 1, "Two plants sharing one cell must each receive their own EdgarSPDown effect.");
		Node2D child = plantA.frontEffectNode.GetChild<Node2D>(childCount);
		Node2D child2 = plantB.frontEffectNode.GetChild<Node2D>(childCount2);
		Check(child.Position.IsZeroApprox() && child2.Position.IsZeroApprox(), "EdgarSPDown effects must be attached to and follow their individual plant nodes.");
		Array<int> array = boss.ExportNetworkSpecialState().GetValueOrDefault("pulseAffectedCharacterIds", new Array<int>()).AsGodotArray<int>();
		Check(array.Count == 2 && array[0] == 9101 && array[1] == 9102, "Pulse network state must identify every affected plant, including same-cell plants.");
		plantA.buff.BuffUpdate(14.9);
		bool flag = Mathf.IsEqualApprox((float)plantA.buff.GetAttackSpeedMultiplier(), 0.5f);
		plantA.buff.BuffUpdate(0.11);
		Check(flag && Mathf.IsEqualApprox((float)plantA.buff.GetAttackSpeedMultiplier(), 1f), "The pulse attack-speed penalty must last 15 seconds and then expire.");
		int childCount3 = plantA.frontEffectNode.GetChildCount();
		int childCount4 = plantB.frontEffectNode.GetChildCount();
		TowerDefenseZombieBossEdgarII towerDefenseZombieBossEdgarII = Instantiate<TowerDefenseZombieBossEdgarII>("res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn");
		if (!GodotObject.IsInstanceValid(towerDefenseZombieBossEdgarII))
		{
			throw new InvalidOperationException("Remote pulse fixture failed to instantiate Edgar II.");
		}
		bool isMultiplayerMode = Global.Instance.isMultiplayerMode;
		bool isHost = MultiPlayerManager.Instance.isHost;
		bool isGameRunning = control.isGameRunning;
		try
		{
			Dictionary dictionary2 = boss.ExportNetworkSpecialState();
			int num = dictionary2.GetValueOrDefault("rev", 1).AsInt32();
			int num2 = dictionary2.GetValueOrDefault("pulseActionSequence", 1).AsInt32();
			Dictionary dictionary3 = dictionary2.Duplicate(deep: true);
			dictionary3["rev"] = Math.Max(0, num - 1);
			dictionary3["pulseActionSequence"] = Math.Max(0, num2 - 1);
			Global.Instance.isMultiplayerMode = true;
			MultiPlayerManager.Instance.isHost = false;
			control._syncCharacters.Remove(9102);
			towerDefenseZombieBossEdgarII.ImportNetworkSpawnState(dictionary3);
			PlaceCharacter(towerDefenseZombieBossEdgarII, new Vector2I(9, 5));
			control.characterNode.AddChild(towerDefenseZombieBossEdgarII, forceReadableName: false, InternalMode.Disabled);
			towerDefenseZombieBossEdgarII.ProcessMode = ProcessModeEnum.Disabled;
			control.isGameRunning = true;
			towerDefenseZombieBossEdgarII.ImportNetworkSpecialState(dictionary2);
			int childCount5 = plantA.frontEffectNode.GetChildCount();
			towerDefenseZombieBossEdgarII.ImportNetworkSpecialState(dictionary2);
			towerDefenseZombieBossEdgarII.BatchUpdate(0.5);
			control.RegisterSyncCharacter(9102, plantB);
			towerDefenseZombieBossEdgarII.BatchUpdate(0.0);
			Check(childCount5 == childCount3 + 1 && plantA.frontEffectNode.GetChildCount() == childCount5 && plantB.frontEffectNode.GetChildCount() == childCount4 + 1, "Formal repeated network pulse imports must resolve delayed IDs without replaying resolved plants.");
		}
		finally
		{
			control.RegisterSyncCharacter(9102, plantB);
			control.isGameRunning = isGameRunning;
			MultiPlayerManager.Instance.isHost = isHost;
			Global.Instance.isMultiplayerMode = isMultiplayerMode;
			towerDefenseZombieBossEdgarII.QueueFree();
		}
		TowerDefenseZombieBossEdgarII towerDefenseZombieBossEdgarII2 = Instantiate<TowerDefenseZombieBossEdgarII>("res://Asset/Anime/Character/Zombie/Boss/EdgarII/Scene/TowerDefenseZombieBossEdgarII.tscn");
		if (!GodotObject.IsInstanceValid(towerDefenseZombieBossEdgarII2))
		{
			throw new InvalidOperationException("Late-join Edgar II fixture failed to instantiate.");
		}
		Dictionary dictionary4 = boss.ExportNetworkSpawnState();
		dictionary4["action"] = 5;
		dictionary4["animationFrame"] = 260;
		towerDefenseZombieBossEdgarII2.ImportNetworkSpawnState(dictionary4);
		PlaceCharacter(towerDefenseZombieBossEdgarII2, new Vector2I(9, 5));
		control.characterNode.AddChild(towerDefenseZombieBossEdgarII2, forceReadableName: false, InternalMode.Disabled);
		towerDefenseZombieBossEdgarII2.ProcessMode = ProcessModeEnum.Disabled;
		Check(towerDefenseZombieBossEdgarII2.sprite.clip == "Electronic" && towerDefenseZombieBossEdgarII2.sprite.frameIndex == 260, "A late-joined Edgar II must restore the authoritative one-shot skill frame.");
		towerDefenseZombieBossEdgarII2.QueueFree();
		return (plantA, plantB);
	}

	private async Task VerifyDeathPlayback(EdgarFireballDurabilityControlStub control, TowerDefenseZombieBossEdgarII boss)
	{
		bool isGameRunning = control.isGameRunning;
		control.isGameRunning = true;
		boss.zombieDeathComponent.dropFeatureName = "";
		boss.instance.SkipInvincibleDealHurt(boss.instance.hitpoints + 1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		boss.BatchUpdate(0.0);
		Check(boss.die && boss.CurrentStateHandle?.StableId == "zombie.die" && boss.sprite.clip == "Death" && !boss.sprite.loop, $"A lethal hit must enter the real non-looping Death clip; state={boss.CurrentStateHandle?.StableId}, clip={boss.sprite?.clip}.");
		Vector2I clip = boss.sprite.flashAnimeData.GetClip("Death");
		boss.sprite.frameIndex = clip.Y - 2;
		boss.sprite.elapsedTimer = 0.0;
		boss.sprite.clipOver = false;
		boss.sprite.RunBatchedProcessUpdate(1.01 / boss.sprite.frameRate);
		Check(clip == new Vector2I(328, 387) && boss.sprite.frameIndex == 386 && !boss.sprite.clipOver, $"Death must visibly reach authored frame 386 before completion; range={clip}, frame={boss.sprite.frameIndex}, over={boss.sprite.clipOver}.");
		Dictionary dictionary = boss.ExportNetworkSpecialState();
		dictionary["rev"] = dictionary.GetValueOrDefault("rev", 0).AsInt32() + 1;
		dictionary["action"] = 5;
		dictionary["animationFrame"] = 260;
		boss.ImportNetworkSpecialState(dictionary);
		Check(boss.sprite.clip == "Death" && boss.sprite.frameIndex == 386, "Edgar II special-state presentation must never overwrite an active Death clip.");
		control.isGameRunning = isGameRunning;
		await Task.CompletedTask;
	}

	private static object InvokePrivate(object owner, string methodName, params object[] arguments)
	{
		MethodInfo? method = owner.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
		if (method == null)
		{
			throw new MissingMethodException(owner.GetType().FullName, methodName);
		}
		return method.Invoke(owner, arguments);
	}

	private static TowerDefenseBattleFeatureMap CreateMapFeature(TowerDefenseMapControl mapControl)
	{
		TowerDefenseMapConfig towerDefenseMapConfig = new TowerDefenseMapConfig
		{
			gridNum = GridNum,
			gridBeginPos = GridBegin,
			gridSize = GridSize,
			plantOffset = 50.0
		};
		TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = (mapControl.mapFeature = new TowerDefenseBattleFeatureMap
		{
			mapControl = mapControl,
			config = towerDefenseMapConfig,
			mapConfig = towerDefenseMapConfig,
			rect = new Rect2(GridBegin - new Vector2(100f, GridSize.Y), new Vector2(GridSize.X * (float)GridNum.X + 200f, GridSize.Y * (float)(GridNum.Y + 2)))
		});
		towerDefenseBattleFeatureMap.PlantGridInit();
		for (int i = 1; i <= GridNum.X; i++)
		{
			for (int j = 1; j <= GridNum.Y; j++)
			{
				towerDefenseBattleFeatureMap.GetPlantGridCell(new Vector2I(i, j))?.Init(new TowerDefenseCellConfig());
			}
		}
		for (int k = 1; k <= GridNum.Y; k++)
		{
			towerDefenseBattleFeatureMap.lineUse[k] = true;
		}
		return towerDefenseBattleFeatureMap;
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

	private static void PlaceCharacter(TowerDefenseCharacter character, Vector2I gridPos)
	{
		character.Position = GridBegin + new Vector2((float)(gridPos.X - 1) * GridSize.X, (float)(gridPos.Y - 1) * GridSize.Y);
		character.gridPos = gridPos;
		character.inGame = true;
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
			GD.PushError("EDGAR_FIREBALL_DURABILITY_FAILURE " + message);
		}
	}
}
