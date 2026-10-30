using System;
using System.Reflection;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;

public partial class BugOverviewHammerQueenRuntimeTest : Node
{
	private const string HammerLightScenePath = "res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Scene/TowerDefensePlantHammerLight.tscn";

	private const string GoldHammerScenePath = "res://Asset/Anime/Character/Plant/Star/GoldHammer/Scene/TowerDefensePlantGoldHammer.tscn";

	private const string DayCraterScenePath = "res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn";

	private const string HammerLightPacketPath = "res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Packet/PlantHammerLight.tres";

	private const string DayCraterPacketPath = "res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres";

	private const string NormalZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string QueenScenePath = "res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn";

	private const string IceFirePumpkinScenePath = "res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		try
		{
			_ = 1;
			try
			{
				TowerDefenseProjectileRegistry.Init();
				await VerifyHammerPlacementContract();
				await VerifyQueenNoteSkinThroughIceFire();
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BugOverviewHammerQueenRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			await WaitFrames(3);
		}
		bool flag = _failures == 0;
		GD.Print($"BUG_OVERVIEW_HAMMER_QUEEN_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyHammerPlacementContract()
	{
		TowerDefenseZombie zombie = null;
		TowerDefensePlantHammerLight hammerLight = null;
		TowerDefensePlantGoldHammer goldHammer = null;
		try
		{
			TowerDefensePlantConfig towerDefensePlantConfig = ResourceLoader.Load<TowerDefensePlantConfig>("res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Config/TowerDefensePlantHammerLight.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefensePlantConfig towerDefensePlantConfig2 = ResourceLoader.Load<TowerDefensePlantConfig>("res://Asset/Anime/Character/Plant/Star/GoldHammer/Config/TowerDefensePlantGoldHammer.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(towerDefensePlantConfig != null && towerDefensePlantConfig2 != null, "Both real hammer character configs must load.");
			Check(towerDefensePlantConfig != null && towerDefensePlantConfig.maskFlags == 2 && towerDefensePlantConfig2 != null && towerDefensePlantConfig2.maskFlags == 2, "Hammer cards must retain their placement-only zombie mask instead of becoming ordinary bite targets.");
			Check(towerDefensePlantConfig != null && towerDefensePlantConfig.collisionFlags == 11 && towerDefensePlantConfig2 != null && towerDefensePlantConfig2.collisionFlags == 11, "Hammer cards must retain their one-shot plant collision contract.");
			zombie = InstantiateCharacter<TowerDefenseZombie>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
			hammerLight = InstantiateCharacter<TowerDefensePlantHammerLight>("res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Scene/TowerDefensePlantHammerLight.tscn");
			goldHammer = InstantiateCharacter<TowerDefensePlantGoldHammer>("res://Asset/Anime/Character/Plant/Star/GoldHammer/Scene/TowerDefensePlantGoldHammer.tscn");
			Check(GodotObject.IsInstanceValid(zombie) && GodotObject.IsInstanceValid(hammerLight) && GodotObject.IsInstanceValid(goldHammer), "The real normal zombie and both hammer scenes must instantiate.");
			if (!GodotObject.IsInstanceValid(zombie) || !GodotObject.IsInstanceValid(hammerLight) || !GodotObject.IsInstanceValid(goldHammer))
			{
				return;
			}
			await WaitFrames(2);
			Check(zombie.instance != null && hammerLight.instance != null && goldHammer.instance != null, "The real hammer damage check requires initialized character instances.");
			if (zombie.instance == null)
			{
				return;
			}
			await VerifyHammerDamage(hammerLight, zombie, 500.0, "light hammer");
			await VerifyHammerDamage(goldHammer, zombie, 3000.0, "gold hammer");
			await VerifyHammerLightCraterRepair();
			await VerifyGoldHammerCraterRepair();
			VerifyPacketPickCraterPriority();
			await VerifyHammerModeCraterRepair();
		}
		finally
		{
			if (GodotObject.IsInstanceValid(hammerLight))
			{
				hammerLight.QueueFree();
			}
			if (GodotObject.IsInstanceValid(goldHammer))
			{
				goldHammer.QueueFree();
			}
			if (GodotObject.IsInstanceValid(zombie))
			{
				zombie.QueueFree();
			}
			await WaitFrames(2);
		}
	}

	private async Task VerifyHammerModeCraterRepair()
	{
		TowerDefenseCrater crater = null;
		try
		{
			crater = InstantiateCharacter<TowerDefenseCrater>("res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn");
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(crater) && towerDefensePacketConfig != null, "The hammer mode crater regression requires the production crater and packet resources.");
			if (!GodotObject.IsInstanceValid(crater) || towerDefensePacketConfig == null)
			{
				return;
			}
			TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
			{
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR
				}
			};
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in towerDefenseCellInstance.gridType)
			{
				towerDefenseCellInstance.slot[item] = null;
			}
			crater.packet = towerDefensePacketConfig;
			towerDefenseCellInstance.CharacterPlant(towerDefensePacketConfig, crater, noLimit: true);
			Check(towerDefenseCellInstance.characterList.Contains(crater), "The hammer mode regression needs a crater registered through the normal cell occupancy path.");
			bool condition = new TowerDefenseHammerMode().TryRepairCraterAtCell(towerDefenseCellInstance, Vector2.Zero);
			Check(condition, "Hammer mode must repair a crater from cell occupancy because craters have no hitbox for rectangle attacks.");
			Check(crater.isDestroy && crater.die && crater.instance.die, "Hammer mode repair must end the crater gameplay lifecycle immediately.");
			Check(!towerDefenseCellInstance.characterList.Contains(crater), "Hammer mode repair must clear the crater from its production cell synchronously.");
			await WaitFrames(2);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(crater))
			{
				crater.QueueFree();
			}
			await WaitFrames(1);
		}
	}

	private async Task VerifyHammerLightCraterRepair()
	{
		TowerDefensePlantHammerLight hammerLight = null;
		TowerDefenseCrater crater = null;
		try
		{
			hammerLight = InstantiateCharacter<TowerDefensePlantHammerLight>("res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Scene/TowerDefensePlantHammerLight.tscn");
			crater = InstantiateCharacter<TowerDefenseCrater>("res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn");
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Packet/PlantHammerLight.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefensePacketConfig towerDefensePacketConfig2 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(hammerLight) && GodotObject.IsInstanceValid(crater) && towerDefensePacketConfig != null && towerDefensePacketConfig2 != null, "The real light-hammer crater regression requires the production hammer, crater, and packet resources.");
			if (!GodotObject.IsInstanceValid(hammerLight) || !GodotObject.IsInstanceValid(crater) || towerDefensePacketConfig == null || towerDefensePacketConfig2 == null)
			{
				return;
			}
			TowerDefenseCellInstance cell = new TowerDefenseCellInstance
			{
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR
				}
			};
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in cell.gridType)
			{
				cell.slot[item] = null;
			}
			crater.packet = towerDefensePacketConfig2;
			hammerLight.packet = towerDefensePacketConfig;
			hammerLight.targetZombie = null;
			hammerLight.cell = cell;
			cell.CharacterPlant(towerDefensePacketConfig2, crater, noLimit: true);
			cell.CharacterPlant(towerDefensePacketConfig, hammerLight, noLimit: true);
			Check(cell.characterSlotDictionary.TryGetValue(hammerLight, out var value) && value == crater, "Production planting must register the crater as the light hammer's covered slot.");
			await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			await WaitFrames(1);
			Check(hammerLight.crater == crater, "The light hammer's deferred ready lookup must retain its registered crater target.");
			hammerLight.crater = null;
			hammerLight.Explode();
			Check(crater.isDestroy && crater.die && crater.instance.die, "The light hammer must lazily resolve and immediately end the registered crater's gameplay lifecycle.");
			Check(!cell.characterList.Contains(crater) && cell.GetSlot(hammerLight) != crater, "Repairing the crater must synchronously clear its production cell occupancy through OnDestroy.");
			await WaitFrames(2);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(crater))
			{
				crater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(hammerLight))
			{
				hammerLight.cell = null;
				hammerLight.crater = null;
				hammerLight.QueueFree();
			}
			await WaitFrames(1);
		}
	}

	private void VerifyPacketPickCraterPriority()
	{
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		TowerDefenseControlNew currentControl = instance?.currentControl;
		TowerDefenseControlNew towerDefenseControlNew = null;
		PacketPickControl packetPickControl = null;
		TowerDefenseInGamePacketShow towerDefenseInGamePacketShow = null;
		TowerDefenseCrater towerDefenseCrater = null;
		try
		{
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Chapter8/HammerLight/Packet/PlantHammerLight.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefensePacketConfig towerDefensePacketConfig2 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(instance) && towerDefensePacketConfig != null && towerDefensePacketConfig2 != null, "The packet-pick crater priority regression requires the manager and real hammer/crater packets.");
			if (!GodotObject.IsInstanceValid(instance) || towerDefensePacketConfig == null || towerDefensePacketConfig2 == null)
			{
				return;
			}
			towerDefenseControlNew = new TowerDefenseControlNew();
			TowerDefenseBattleFeatureMap towerDefenseBattleFeatureMap = new TowerDefenseBattleFeatureMap();
			towerDefenseBattleFeatureMap.iceCapList.Resize(2);
			towerDefenseControlNew.featureDictionary[new StringName("Map")] = towerDefenseBattleFeatureMap;
			instance.currentControl = towerDefenseControlNew;
			towerDefenseCrater = InstantiateCharacter<TowerDefenseCrater>("res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn");
			Check(GodotObject.IsInstanceValid(towerDefenseCrater), "The packet-pick crater priority regression requires the real crater scene.");
			if (!GodotObject.IsInstanceValid(towerDefenseCrater))
			{
				return;
			}
			TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
			{
				gridPos = new Vector2I(1, 1),
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR
				}
			};
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in towerDefenseCellInstance.gridType)
			{
				towerDefenseCellInstance.slot[item] = null;
			}
			towerDefenseCrater.packet = towerDefensePacketConfig2;
			towerDefenseCellInstance.CharacterPlant(towerDefensePacketConfig2, towerDefenseCrater, noLimit: true);
			packetPickControl = new PacketPickControl();
			towerDefenseInGamePacketShow = (packetPickControl.packetPick = new TowerDefenseInGamePacketShow
			{
				config = towerDefensePacketConfig,
				alive = true,
				select = true
			});
			MethodInfo method = typeof(PacketPickControl).GetMethod("ShouldPreferCraterRepair", BindingFlags.Instance | BindingFlags.NonPublic);
			Check(method != null, "PacketPickControl must expose the private crater-priority predicate for this regression.");
			if (!(method == null))
			{
				bool condition = (bool)method.Invoke(packetPickControl, new object[1] { towerDefenseCellInstance });
				Check(condition, "A zombie-place hammer card must prefer repairing the current crater cell before scanning for a zombie target.");
				towerDefenseCrater.Destroy();
				bool flag = (bool)method.Invoke(packetPickControl, new object[1] { towerDefenseCellInstance });
				Check(!flag, "The packet-pick crater priority must stop after the crater is already destroyed.");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(instance))
			{
				instance.currentControl = currentControl;
			}
			if (GodotObject.IsInstanceValid(towerDefenseCrater))
			{
				towerDefenseCrater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(towerDefenseInGamePacketShow))
			{
				towerDefenseInGamePacketShow.Free();
			}
			if (GodotObject.IsInstanceValid(packetPickControl))
			{
				packetPickControl.Free();
			}
			if (GodotObject.IsInstanceValid(towerDefenseControlNew))
			{
				towerDefenseControlNew.featureDictionary.Clear();
				towerDefenseControlNew.Free();
			}
		}
	}

	private async Task VerifyGoldHammerCraterRepair()
	{
		TowerDefensePlantGoldHammer goldHammer = null;
		TowerDefenseCrater crater = null;
		try
		{
			goldHammer = InstantiateCharacter<TowerDefensePlantGoldHammer>("res://Asset/Anime/Character/Plant/Star/GoldHammer/Scene/TowerDefensePlantGoldHammer.tscn");
			crater = InstantiateCharacter<TowerDefenseCrater>("res://Asset/Anime/Character/Crater/CraterDayGround/Scene/TowerDefenseCraterDayGround.tscn");
			TowerDefensePacketConfig towerDefensePacketConfig = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Plant/Star/GoldHammer/Packet/PlantGoldHammer.tres", null, ResourceLoader.CacheMode.Ignore);
			TowerDefensePacketConfig towerDefensePacketConfig2 = ResourceLoader.Load<TowerDefensePacketConfig>("res://Asset/Anime/Character/Crater/CraterDayGround/Packet/CraterDayGround.tres", null, ResourceLoader.CacheMode.Ignore);
			Check(GodotObject.IsInstanceValid(goldHammer) && GodotObject.IsInstanceValid(crater) && towerDefensePacketConfig != null && towerDefensePacketConfig2 != null, "The real gold-hammer crater regression requires the production hammer, crater, and packet resources.");
			if (!GodotObject.IsInstanceValid(goldHammer) || !GodotObject.IsInstanceValid(crater) || towerDefensePacketConfig == null || towerDefensePacketConfig2 == null)
			{
				return;
			}
			TowerDefenseCellInstance towerDefenseCellInstance = new TowerDefenseCellInstance
			{
				gridType = new Array<TowerDefenseEnum.PLANTGRIDTYPE>
				{
					TowerDefenseEnum.PLANTGRIDTYPE.GROUND,
					TowerDefenseEnum.PLANTGRIDTYPE.AIR
				}
			};
			foreach (TowerDefenseEnum.PLANTGRIDTYPE item in towerDefenseCellInstance.gridType)
			{
				towerDefenseCellInstance.slot[item] = null;
			}
			crater.packet = towerDefensePacketConfig2;
			goldHammer.packet = towerDefensePacketConfig;
			goldHammer.targetZombie = null;
			goldHammer.cell = towerDefenseCellInstance;
			towerDefenseCellInstance.CharacterPlant(towerDefensePacketConfig2, crater, noLimit: true);
			towerDefenseCellInstance.CharacterPlant(towerDefensePacketConfig, goldHammer, noLimit: true);
			Check(towerDefenseCellInstance.characterSlotDictionary.TryGetValue(goldHammer, out var value) && value == crater, "Production planting must register the crater as the gold hammer's covered slot.");
			goldHammer.crater = null;
			goldHammer.instance.hypnoses = true;
			goldHammer.Explode();
			Check(crater.isDestroy && crater.die && crater.instance.die, "The gold hammer must lazily resolve and immediately end the registered crater's gameplay lifecycle.");
			Check(!towerDefenseCellInstance.characterList.Contains(crater) && towerDefenseCellInstance.GetSlot(goldHammer) != crater, "Gold hammer crater repair must synchronously clear its production cell occupancy through OnDestroy.");
			await WaitFrames(2);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(crater))
			{
				crater.QueueFree();
			}
			if (GodotObject.IsInstanceValid(goldHammer))
			{
				goldHammer.cell = null;
				goldHammer.crater = null;
				goldHammer.QueueFree();
			}
			await WaitFrames(1);
		}
	}

	private async Task VerifyHammerDamage(TowerDefensePlant hammer, TowerDefenseZombie zombie, double expectedDamage, string label)
	{
		zombie.instance.hitpointsBase = 10000.0;
		zombie.instance.hitpointsSave = 10000.0;
		zombie.instance.hitpoints = 10000.0;
		zombie.die = false;
		zombie.nearDie = false;
		hammer.targetZombie = zombie;
		double before = zombie.instance.hitpoints;
		if (hammer is TowerDefensePlantHammerLight towerDefensePlantHammerLight)
		{
			towerDefensePlantHammerLight.Explode();
		}
		else if (hammer is TowerDefensePlantGoldHammer towerDefensePlantGoldHammer)
		{
			towerDefensePlantGoldHammer.Explode();
		}
		await WaitFrames(1);
		Check(Math.Abs(hammer.zombiePlaceDamage - expectedDamage) < 0.001, $"The real {label} must keep its {expectedDamage:0} placement damage.");
		Check(zombie.instance.hitpoints <= before - expectedDamage + 0.001, $"The real {label} must damage its placement target instead of waiting to be bitten; before={before}, after={zombie.instance.hitpoints}.");
	}

	private async Task VerifyQueenNoteSkinThroughIceFire()
	{
		TowerDefensePlantQueenSunFlower queen = null;
		TowerDefensePlant iceFirePumpkin = null;
		BulletField bulletField = BulletField.Instance;
		bool ownsBulletField = false;
		try
		{
			if (!GodotObject.IsInstanceValid(bulletField))
			{
				bulletField = new BulletField
				{
					Name = "HammerQueenBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				ownsBulletField = true;
				await WaitFrames(1);
			}
			Check(GodotObject.IsInstanceValid(bulletField), "A live BulletField is required for the Queen Note-skin conversion regression.");
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
				queen = InstantiateCharacter<TowerDefensePlantQueenSunFlower>("res://Asset/Anime/Character/Plant/Gold/QueenSunFlower/Scene/TowerDefensePlantQueenSunFlower.tscn", new Array<string> { "Custom0" });
				iceFirePumpkin = InstantiateCharacter<TowerDefensePlant>("res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn");
				Check(GodotObject.IsInstanceValid(queen) && GodotObject.IsInstanceValid(iceFirePumpkin), "The real Queen Sunflower and blue-fire Pumpkin scenes must instantiate.");
				if (GodotObject.IsInstanceValid(queen) && GodotObject.IsInstanceValid(iceFirePumpkin))
				{
					queen.gridPos = new Vector2I(2, 2);
					iceFirePumpkin.gridPos = new Vector2I(3, 2);
					queen.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
					iceFirePumpkin.camp = queen.camp;
					await WaitFrames(2);
					FireComponent fireComponent = queen.componentManager?.GetRuntime<FireComponent>("character.fire");
					ChangeProjectileComponent changeProjectileComponent = iceFirePumpkin.componentManager?.GetRuntime<ChangeProjectileComponent>("change_projectile");
					Check(fireComponent != null && !fireComponent.IsReleased && changeProjectileComponent != null && !changeProjectileComponent.IsReleased, "The real Queen FireComponent and blue-fire ChangeProjectileComponent must be active.");
					if (fireComponent != null && !fireComponent.IsReleased && changeProjectileComponent != null && !changeProjectileComponent.IsReleased)
					{
						Check(queen.skinName == "Note", "Queen Sunflower Custom0 must apply the live Note projectile skin.");
						TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = ((fireComponent.fireCheckList.Count <= 0) ? null : fireComponent.fireCheckList[0].projectile?.GetProjectile());
						Check(towerDefenseProjectileCreateData != null && towerDefenseProjectileCreateData.projectileName == new StringName("FirePea") && towerDefenseProjectileCreateData.skinName == new StringName("Note"), "The live Queen FireComponent must produce the tracking FirePea Note source data.");
						TowerDefenseProjectileConfig towerDefenseProjectileConfig = towerDefenseProjectileCreateData?.BuildConfig();
						Check(towerDefenseProjectileConfig != null && towerDefenseProjectileConfig.skinName == new StringName("Note"), "The real Queen source projectile config must retain the Note skin.");
						if (towerDefenseProjectileConfig != null)
						{
							int num = bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, queen.GlobalPosition, Vector2.Right * 500f, 500.0, queen, queen.camp, queen.gridPos, queen.gridPos.Y, new Rect2(-10000f, -10000f, 20000f, 20000f), null, 0.0, 0.0, towerDefenseProjectileConfig.collisionFlags);
							Check(num >= 0, "The real Queen Note projectile must spawn through BulletField.");
							if (num >= 0)
							{
								changeProjectileComponent.OnBulletIntersect(ref bulletField.GetBulletDataRef(num), num);
								Check(bulletField.IsBulletActive(num), "The Queen projectile must remain active after the blue-fire conversion.");
								if (bulletField.IsBulletActive(num))
								{
									ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
									PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(new StringName("IceFirePea"), new StringName("Note"));
									Check(bulletDataRef.config?.NameSN.ToString().StartsWith("IceFirePea", StringComparison.Ordinal) ?? false, "Blue fire must convert the Queen projectile into the IceFirePea family.");
									Check(bulletDataRef.config?.skinName == new StringName("Note"), "Blue fire must preserve the Queen projectile's Note skin name.");
									Check(projectileSkinProjectileScene != null && bulletDataRef.config?.projectileScene != null && (bulletDataRef.config.projectileScene == projectileSkinProjectileScene || bulletDataRef.config.projectileScene.ResourcePath == projectileSkinProjectileScene.ResourcePath), "Blue fire must resolve the dedicated IceFirePea Note scene instead of the ordinary blue pea visual.");
									Check((bulletDataRef.fireMethodFlags & 0x20) != 0, "Blue fire must preserve the Queen projectile's tracking fire method.");
									return;
								}
								return;
							}
							return;
						}
						return;
					}
					return;
				}
				return;
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(queen))
			{
				queen.QueueFree();
			}
			if (GodotObject.IsInstanceValid(iceFirePumpkin))
			{
				iceFirePumpkin.QueueFree();
			}
			if (ownsBulletField && GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			await WaitFrames(2);
		}
	}

	private T InstantiateCharacter<T>(string path, Array<string> customs = null) where T : TowerDefenseCharacter
	{
		PackedScene packedScene = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore);
		T val = ((packedScene != null) ? packedScene.Instantiate<T>(PackedScene.GenEditState.Disabled) : null);
		if (!GodotObject.IsInstanceValid(val))
		{
			return null;
		}
		val.inGame = false;
		val.editorPreviewMode = true;
		if (customs != null)
		{
			val.currentCustom = customs;
		}
		AddChild(val, forceReadableName: false, InternalMode.Disabled);
		return val;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[BugOverviewHammerQueenRuntimeTest] " + message);
		}
	}
}
