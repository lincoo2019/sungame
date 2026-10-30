using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Godot;

public partial class BulletFieldSequentialOverfireRuntimeTest : Node
{
	private const string FireScenePath = "res://Asset/Anime/Character/Plant/Chapter0/Torchwood/Scene/TowerDefensePlantTorchwood.tscn";

	private const string IceFireScenePath = "res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn";

	private const string MegaFireScenePath = "res://Asset/Anime/Character/Item/MegaFire/Scene/TowerDefenseItemMegaFire.tscn";

	private const string CherryPeaEffectScenePath = "res://Prefab/ProjectileEffect/CherryPea/TowerDefenseProjectileEffectCherryPea.tscn";

	private const int CherryPeaProjectilesPerWave = 36;

	private const int BulletFieldCapacity = 65536;

	private int _checks;

	private int _failures;

	private bool _rendered;

	private int _stressCount;

	public override async void _Ready()
	{
		BulletField bulletField = null;
		TowerDefenseCharacter fire = null;
		TowerDefenseCharacter secondFire = null;
		TowerDefenseCharacter iceFire = null;
		TowerDefenseCharacter megaFire = null;
		try
		{
			_ = 3;
			try
			{
				_rendered = HasCommandLineArgument("--sequential-overfire-rendered");
				_stressCount = ReadStressCount();
				TowerDefenseProjectileRegistry.Init();
				fire = InstantiateCharacter("res://Asset/Anime/Character/Plant/Chapter0/Torchwood/Scene/TowerDefensePlantTorchwood.tscn", new Vector2I(2, 2));
				secondFire = InstantiateCharacter("res://Asset/Anime/Character/Plant/Chapter0/Torchwood/Scene/TowerDefensePlantTorchwood.tscn", new Vector2I(3, 2));
				iceFire = InstantiateCharacter("res://Asset/Anime/Character/Plant/Cover/PumpkinFireIce/Scene/TowerDefensePlantPumpkinFireIce.tscn", new Vector2I(4, 2));
				megaFire = InstantiateCharacter("res://Asset/Anime/Character/Item/MegaFire/Scene/TowerDefenseItemMegaFire.tscn", new Vector2I(5, 2));
				await WaitFrames(3);
				fire.GlobalPosition = new Vector2(160f, 360f);
				secondFire.GlobalPosition = new Vector2(320f, 360f);
				iceFire.GlobalPosition = new Vector2(480f, 360f);
				megaFire.GlobalPosition = new Vector2(640f, 360f);
				bulletField = new BulletField
				{
					Name = "SequentialOverfireBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				await WaitFrames(1);
				ChangeProjectileComponent fireChange = GetChange(fire, "ordinary fire");
				ChangeProjectileComponent secondFireChange = GetChange(secondFire, "second ordinary fire");
				ChangeProjectileComponent iceFireChange = GetChange(iceFire, "ice fire");
				ChangeProjectileComponent megaFireChange = GetChange(megaFire, "mega fire");
				if (fireChange != null && secondFireChange != null && iceFireChange != null && megaFireChange != null)
				{
					VerifyCreateDataSkinCacheInvalidation();
					await VerifyRegisteredZoneChain(bulletField, fire, fireChange, iceFireChange, megaFireChange);
					VerifyPeaChain(bulletField, fire, fireChange, megaFireChange);
					VerifySnowPeaChain(bulletField, fire, fireChange, secondFireChange, megaFireChange);
					VerifyTrackedSkinChain(bulletField, fire, iceFireChange, megaFireChange);
					VerifyPeaSkinFamilyChains(bulletField, fire, fireChange, iceFireChange, megaFireChange);
					VerifySnowPeaArmorSkinChain(bulletField, fire, fireChange, secondFireChange, iceFireChange, megaFireChange);
					VerifyUnmatchedTargetSkinChain(bulletField, fire, fireChange, secondFireChange, iceFireChange, megaFireChange);
					if (_stressCount > 0)
					{
						await VerifySequentialOverfireStress(bulletField, fire, fireChange, iceFireChange, _stressCount);
					}
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[BulletFieldSequentialOverfireRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			QueueFreeIfValid(fire);
			QueueFreeIfValid(secondFire);
			QueueFreeIfValid(iceFire);
			QueueFreeIfValid(megaFire);
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			await WaitFrames(3);
			AdobeAnimateDefinitionCache.Clear();
			await WaitFrames(2);
		}
		bool flag = _failures == 0;
		GD.Print($"BULLET_FIELD_SEQUENTIAL_OVERFIRE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task VerifyRegisteredZoneChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent fire, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int index = Spawn(bulletField, source, "Pea", "Default", track: false);
		Check(index >= 0, "Registered-zone Pea must spawn through BulletField.");
		if (index >= 0)
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
			bulletDataRef.vel = Vector2.Zero;
			bulletDataRef.speed = 0f;
			bulletDataRef.lockGridY = true;
			MoveThroughRegisteredZone(bulletField, index, fire, 1uL);
			CheckName(bulletField, index, "FirePea", "The real registered fire zone must ignite Pea.");
			await WaitFrames((!_rendered) ? 1 : 8);
			MoveThroughRegisteredZone(bulletField, index, iceFire, 2uL);
			CheckName(bulletField, index, "IceFirePea", "The later real ice-fire zone must change FirePea again.");
			await WaitFrames((!_rendered) ? 1 : 8);
			MoveThroughRegisteredZone(bulletField, index, megaFire, 3uL);
			CheckName(bulletField, index, "MegaFirePea", "The later real mega-fire zone must change IceFirePea again.");
			await WaitFrames((!_rendered) ? 1 : 12);
			CheckMultiMeshOnly(bulletField, index, "registered-zone sequential overfire");
		}
	}

	private static void MoveThroughRegisteredZone(BulletField bulletField, int index, IProjectileZone zone, ulong updateFrame)
	{
		if (bulletField.IsBulletActive(index))
		{
			if (zone is ChangeProjectileComponent changeProjectileComponent && GodotObject.IsInstanceValid(changeProjectileComponent.parent))
			{
				changeProjectileComponent.parent.gridPos = new Vector2I(changeProjectileComponent.parent.gridPos.X, 2);
			}
			zone.UpdateRect();
			Vector2 center = zone.WorldRect.GetCenter();
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
			Vector2 newPos = new Vector2(center.X, center.Y - (float)bulletDataRef.height);
			bulletField.TeleportBullet(index, newPos, zone.GridY);
			bulletField.Update(0.0, updateFrame);
		}
	}

	private void VerifyPeaChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent fire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "Pea", "Default", track: false);
		Check(num >= 0, "Pea must spawn through BulletField.");
		if (num >= 0)
		{
			Apply(fire, bulletField, num);
			CheckName(bulletField, num, "FirePea", "Pea must become FirePea in ordinary fire.");
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", "FirePea must become MegaFirePea in later mega fire.");
			CheckMultiMeshOnly(bulletField, num, "Pea sequential overfire");
		}
	}

	private void VerifySnowPeaChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent firstFire, ChangeProjectileComponent secondFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "SnowPea", "Default", track: false);
		Check(num >= 0, "SnowPea must spawn through BulletField.");
		if (num >= 0)
		{
			Apply(firstFire, bulletField, num);
			CheckName(bulletField, num, "Pea", "SnowPea must thaw into Pea in the first fire.");
			Apply(secondFire, bulletField, num);
			CheckName(bulletField, num, "FirePea", "Thawed Pea must ignite in a later independent fire.");
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", "FirePea must upgrade again in mega fire.");
			CheckMultiMeshOnly(bulletField, num, "SnowPea three-stage overfire");
		}
	}

	private void VerifyTrackedSkinChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "FirePea", "Note", track: true);
		Check(num >= 0, "Tracked Note FirePea must spawn through BulletField.");
		if (num >= 0)
		{
			Apply(iceFire, bulletField, num);
			CheckName(bulletField, num, "IceFirePea", "Tracked FirePea must become IceFirePea.");
			CheckSkinVisual(bulletField, num, "IceFirePea", "Note");
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", "Tracked IceFirePea must become MegaFirePea later.");
			CheckSkinVisual(bulletField, num, "MegaFirePea", "Note");
			if (bulletField.IsBulletActive(num))
			{
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
				Check(bulletDataRef.config?.skinName == new StringName("Note"), "Sequential overfire must preserve the Note projectile skin.");
				Check(bulletDataRef.trackOpen && (bulletDataRef.fireMethodFlags & 0x20) != 0, "Sequential overfire must preserve tracking behavior.");
			}
			CheckMultiMeshOnly(bulletField, num, "tracked Note sequential overfire");
		}
	}

	private void VerifyCreateDataSkinCacheInvalidation()
	{
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = new TowerDefenseProjectileCreateData(new StringName("Pea"));
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = towerDefenseProjectileCreateData.BuildConfig();
		Check(towerDefenseProjectileConfig != null, "Pea default config must build before a runtime skin switch.");
		towerDefenseProjectileCreateData.skinName = new StringName("Cyber");
		TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = towerDefenseProjectileCreateData.BuildConfig();
		Check(towerDefenseProjectileConfig2 != null, "Pea/Cyber config must rebuild after skinName changes.");
		Check(towerDefenseProjectileConfig != towerDefenseProjectileConfig2, "Changing skinName must invalidate the cached projectile config.");
		Check(towerDefenseProjectileConfig2?.skinName == new StringName("Cyber"), "The rebuilt projectile config must carry the new Cyber skin name.");
		PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(new StringName("Pea"), new StringName("Cyber"));
		Check(SameScene(towerDefenseProjectileConfig2?.projectileScene, projectileSkinProjectileScene), "The rebuilt Pea/Cyber config must use the registered Cyber scene.");
	}

	private void VerifyPeaSkinFamilyChains(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent fire, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		VerifyPeaSkinFamilyChain(bulletField, source, "Armor", fire, iceFire, megaFire);
		VerifyPeaSkinFamilyChain(bulletField, source, "Cyber", fire, iceFire, megaFire);
		VerifyPeaSkinFamilyChain(bulletField, source, "Pow", fire, iceFire, megaFire);
	}

	private void VerifyPeaSkinFamilyChain(BulletField bulletField, TowerDefenseCharacter source, string skinName, ChangeProjectileComponent fire, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "Pea", skinName, track: false);
		Check(num >= 0, "Pea/" + skinName + " must spawn through BulletField.");
		if (num >= 0)
		{
			CheckSkinVisual(bulletField, num, "Pea", skinName);
			Apply(fire, bulletField, num);
			CheckName(bulletField, num, "FirePea", $"Pea/{skinName} must become FirePea/{skinName} in ordinary fire.");
			CheckSkinVisual(bulletField, num, "FirePea", skinName);
			Apply(iceFire, bulletField, num);
			CheckName(bulletField, num, "IceFirePea", $"FirePea/{skinName} must become IceFirePea/{skinName} in ice fire.");
			CheckSkinVisual(bulletField, num, "IceFirePea", skinName);
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", $"IceFirePea/{skinName} must become MegaFirePea/{skinName} in mega fire.");
			CheckSkinVisual(bulletField, num, "MegaFirePea", skinName);
			CheckMultiMeshOnly(bulletField, num, "Pea/" + skinName + " sequential overfire");
		}
	}

	private void VerifySnowPeaArmorSkinChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent firstFire, ChangeProjectileComponent secondFire, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "SnowPea", "Armor", track: false);
		Check(num >= 0, "SnowPea/Armor must spawn through BulletField.");
		if (num >= 0)
		{
			CheckSkinVisual(bulletField, num, "SnowPea", "Armor");
			Apply(firstFire, bulletField, num);
			CheckName(bulletField, num, "Pea", "SnowPea/Armor must thaw into Pea/Armor.");
			CheckSkinVisual(bulletField, num, "Pea", "Armor");
			Apply(secondFire, bulletField, num);
			CheckName(bulletField, num, "FirePea", "Pea/Armor must ignite into FirePea/Armor.");
			CheckSkinVisual(bulletField, num, "FirePea", "Armor");
			Apply(iceFire, bulletField, num);
			CheckName(bulletField, num, "IceFirePea", "FirePea/Armor must become IceFirePea/Armor.");
			CheckSkinVisual(bulletField, num, "IceFirePea", "Armor");
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", "IceFirePea/Armor must become MegaFirePea/Armor.");
			CheckSkinVisual(bulletField, num, "MegaFirePea", "Armor");
			CheckMultiMeshOnly(bulletField, num, "SnowPea/Armor sequential overfire");
		}
	}

	private void VerifyUnmatchedTargetSkinChain(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent fire, ChangeProjectileComponent secondFire, ChangeProjectileComponent iceFire, ChangeProjectileComponent megaFire)
	{
		bulletField.ClearActiveBullets();
		int num = Spawn(bulletField, source, "SnowPea", "Sward", track: false);
		Check(num >= 0, "SnowPea/Sward must spawn through BulletField.");
		if (num >= 0)
		{
			Apply(fire, bulletField, num);
			CheckName(bulletField, num, "Pea", "SnowPea/Sward must thaw in ordinary fire.");
			Apply(secondFire, bulletField, num);
			CheckName(bulletField, num, "FirePea", "Pea/Sward must ignite in later ordinary fire.");
			Apply(iceFire, bulletField, num);
			CheckName(bulletField, num, "IceFirePea", "FirePea/Sward must change in ice fire.");
			Apply(megaFire, bulletField, num);
			CheckName(bulletField, num, "MegaFirePea", "IceFirePea/Sward must change in mega fire.");
			if (bulletField.IsBulletActive(num))
			{
				Check(bulletField.GetBulletDataRef(num).config?.skinName == new StringName("Sward"), "A skin without a dedicated fire target must retain its skin identity.");
			}
			CheckMultiMeshOnly(bulletField, num, "Sward fallback-skin sequential overfire");
		}
	}

	private async Task VerifySequentialOverfireStress(BulletField bulletField, TowerDefenseCharacter source, ChangeProjectileComponent fire, ChangeProjectileComponent iceFire, int count)
	{
		bulletField.ClearActiveBullets();
		ResetZoneRow(fire, 2);
		ResetZoneRow(iceFire, 2);
		source.gridPos = new Vector2I(source.gridPos.X, 2);
		TowerDefenseManager manager = TowerDefenseManager.Instance;
		Check(GodotObject.IsInstanceValid(manager), "The CherryPea stress scene requires the production TowerDefenseManager autoload.");
		if (!GodotObject.IsInstanceValid(manager))
		{
			return;
		}
		Vector2 originalGridSize = manager.gridSize;
		Vector2 originalGridBegin = manager.gridBeginPos;
		Vector2I originalGridNum = manager.gridNum;
		manager.gridSize = new Vector2(80f, 80f);
		manager.gridBeginPos = new Vector2(0f, 240f);
		manager.gridNum = new Vector2I(20, 8);
		try
		{
			PackedScene packedScene = ResourceLoader.Load<PackedScene>("res://Prefab/ProjectileEffect/CherryPea/TowerDefenseProjectileEffectCherryPea.tscn", null, ResourceLoader.CacheMode.Reuse);
			Check(GodotObject.IsInstanceValid(packedScene), "The real TowerDefenseProjectileEffectCherryPea scene must load.");
			if (!GodotObject.IsInstanceValid(packedScene))
			{
				return;
			}
			int effectCount = Math.Max(1, (count + 36 - 1) / 36);
			int expectedProjectileCount = effectCount * 36;
			long timestamp = Stopwatch.GetTimestamp();
			for (int i = 0; i < effectCount; i++)
			{
				TowerDefenseProjectileEffectCherryPea towerDefenseProjectileEffectCherryPea = packedScene.Instantiate<TowerDefenseProjectileEffectCherryPea>(PackedScene.GenEditState.Disabled);
				towerDefenseProjectileEffectCherryPea.num = 1;
				towerDefenseProjectileEffectCherryPea.GlobalPosition = new Vector2(320f, 360f);
				towerDefenseProjectileEffectCherryPea.Init(new Vector2I(4, 2), source.camp, 0, null, 10.0);
				AddChild(towerDefenseProjectileEffectCherryPea, forceReadableName: false, InternalMode.Disabled);
			}
			double spawnMilliseconds = ElapsedMilliseconds(timestamp);
			int[] indices = new int[expectedProjectileCount];
			int num = 0;
			for (int j = 0; j < 65536; j++)
			{
				if (num >= indices.Length)
				{
					break;
				}
				if (bulletField.IsBulletActive(j))
				{
					indices[num++] = j;
				}
			}
			Check(num == expectedProjectileCount, $"The real CherryPea effects must spawn every projectile in their first wave. actual={num}/{expectedProjectileCount} effects={effectCount}");
			if (num == expectedProjectileCount)
			{
				long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
				long timestamp2 = Stopwatch.GetTimestamp();
				double firstFireMilliseconds = ApplyBurstTimed(fire, bulletField, indices);
				double secondFireMilliseconds = ApplyBurstTimed(iceFire, bulletField, indices);
				double transitionMilliseconds = ElapsedMilliseconds(timestamp2);
				long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
				for (int k = 0; k < indices.Length; k += Math.Max(1, indices.Length / 97))
				{
					CheckName(bulletField, indices[k], "IceFirePea", "Every sampled moving stress projectile must finish both overfire changes.");
					CheckMultiMeshOnly(bulletField, indices[k], "moving sequential overfire stress");
				}
				float firstPositionBeforeUpdate = bulletField.GetBulletDataRef(indices[0]).pos.X;
				bool previousProfilerEnabled = TowerDefensePerfProfiler.Enabled;
				bool previousDetailedMetrics = TowerDefensePerfProfiler.DetailedHotPathMetrics;
				int previousDumpInterval = TowerDefensePerfProfiler.DumpIntervalFrames;
				TowerDefensePerfProfiler.Enabled = true;
				TowerDefensePerfProfiler.DetailedHotPathMetrics = true;
				TowerDefensePerfProfiler.DumpIntervalFrames = 1;
				TowerDefensePerfProfiler.Reset();
				TowerDefensePerfProfiler.DumpIfNeeded();
				long timestamp3 = Stopwatch.GetTimestamp();
				bulletField.Update(1.0 / 60.0, 10000uL);
				double publishMilliseconds = ElapsedMilliseconds(timestamp3);
				await WaitFrames(1);
				TowerDefensePerfProfiler.DumpIfNeeded();
				TowerDefensePerfProfiler.Enabled = previousProfilerEnabled;
				TowerDefensePerfProfiler.DetailedHotPathMetrics = previousDetailedMetrics;
				TowerDefensePerfProfiler.DumpIntervalFrames = previousDumpInterval;
				TowerDefensePerfProfiler.Reset();
				int animatedMeshVisibleInstanceCountForTest = bulletField.GetAnimatedMeshVisibleInstanceCountForTest();
				Check(bulletField.ActiveCount == expectedProjectileCount, $"Sequential overfire must retain all active bullets. actual={bulletField.ActiveCount}/{expectedProjectileCount}");
				Check(animatedMeshVisibleInstanceCountForTest > 0, "The stress scene must publish visible animated MultiMesh instances.");
				Check(bulletField.GetBulletDataRef(indices[0]).pos.X > firstPositionBeforeUpdate, "The sequential overfire stress projectiles must keep moving during the rendered update.");
				GD.Print($"BULLET_FIELD_SEQUENTIAL_OVERFIRE_STRESS requested={count} count={expectedProjectileCount} effects={effectCount} spawnMs={spawnMilliseconds:F3} transitionMs={transitionMilliseconds:F3} firstFireMs={firstFireMilliseconds:F3} secondFireMs={secondFireMilliseconds:F3} publishMs={publishMilliseconds:F3} allocatedBytes={allocatedBytes} active={bulletField.ActiveCount} visible={animatedMeshVisibleInstanceCountForTest}");
				await WaitFrames(_rendered ? 30 : 2);
			}
		}
		finally
		{
			manager.gridSize = originalGridSize;
			manager.gridBeginPos = originalGridBegin;
			manager.gridNum = originalGridNum;
		}
	}

	private static void ApplyBurst(ChangeProjectileComponent change, BulletField bulletField, int[] indices)
	{
		foreach (int index in indices)
		{
			if (bulletField.IsBulletActive(index))
			{
				change.OnBulletIntersect(ref bulletField.GetBulletDataRef(index), index);
			}
		}
	}

	private static double ApplyBurstTimed(ChangeProjectileComponent change, BulletField bulletField, int[] indices)
	{
		long timestamp = Stopwatch.GetTimestamp();
		ApplyBurst(change, bulletField, indices);
		return ElapsedMilliseconds(timestamp);
	}

	private static void ResetZoneRow(ChangeProjectileComponent change, int gridY)
	{
		if (GodotObject.IsInstanceValid(change?.parent))
		{
			change.parent.gridPos = new Vector2I(change.parent.gridPos.X, gridY);
		}
	}

	private static double ElapsedMilliseconds(long start)
	{
		return (double)(Stopwatch.GetTimestamp() - start) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private int Spawn(BulletField bulletField, TowerDefenseCharacter source, string projectileName, string skinName, bool track)
	{
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName(projectileName))
		{
			skinName = new StringName(skinName),
			fireMethodFlags = ((!track) ? 1 : 32)
		}.BuildConfig();
		Check(towerDefenseProjectileConfig != null, projectileName + "/" + skinName + " config must build.");
		if (towerDefenseProjectileConfig == null)
		{
			return -1;
		}
		Vector2 vel = Vector2.Right * 500f;
		return bulletField.TrySpawnFromConfig(towerDefenseProjectileConfig, Vector2.Zero, vel, vel.Length(), source, source.camp, source.gridPos, source.gridPos.Y, new Rect2(-10000f, -10000f, 20000f, 20000f), null, 0.0, 0.0, towerDefenseProjectileConfig.collisionFlags);
	}

	private static void Apply(ChangeProjectileComponent change, BulletField bulletField, int index)
	{
		if (bulletField.IsBulletActive(index))
		{
			change.OnBulletIntersect(ref bulletField.GetBulletDataRef(index), index);
		}
	}

	private void CheckName(BulletField bulletField, int index, string expected, string message)
	{
		Check(bulletField.IsBulletActive(index), message + " Bullet must remain active.");
		if (bulletField.IsBulletActive(index))
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
			Check(bulletDataRef.config?.NameSN == new StringName(expected), $"{message} actual={bulletDataRef.config?.NameSN}");
		}
	}

	private void CheckSkinVisual(BulletField bulletField, int index, string projectileName, string skinName)
	{
		Check(bulletField.IsBulletActive(index), projectileName + "/" + skinName + " must remain active while checking its skin.");
		if (bulletField.IsBulletActive(index))
		{
			StringName projectileName2 = new StringName(projectileName);
			StringName stringName = new StringName(skinName);
			bool flag = TowerDefenseProjectileRegistry.HasProjectileSkin(projectileName2, stringName);
			Check(flag, projectileName + "/" + skinName + " must be registered in the projectile skin system.");
			if (flag)
			{
				PackedScene projectileSkinProjectileScene = TowerDefenseProjectileRegistry.GetProjectileSkinProjectileScene(projectileName2, stringName);
				ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
				Check(bulletDataRef.config?.skinName == stringName, projectileName + " must preserve skinName=" + skinName + " after overfire.");
				Check(SameScene(bulletDataRef.config?.projectileScene, projectileSkinProjectileScene), projectileName + "/" + skinName + " must use its registered skin scene after overfire.");
			}
		}
	}

	private static bool SameScene(PackedScene actual, PackedScene expected)
	{
		if (actual == null || expected == null)
		{
			return false;
		}
		if (actual != expected)
		{
			return actual.ResourcePath == expected.ResourcePath;
		}
		return true;
	}

	private void CheckMultiMeshOnly(BulletField bulletField, int index, string label)
	{
		if (bulletField.IsBulletActive(index))
		{
			BulletRenderMode renderMode = bulletField.GetBulletDataRef(index).renderMode;
			bool condition = ((renderMode == BulletRenderMode.STATIC || renderMode == BulletRenderMode.ANIMATED_MESH) ? true : false);
			Check(condition, label + " must stay on a MultiMesh render mode.");
		}
	}

	private ChangeProjectileComponent GetChange(TowerDefenseCharacter character, string label)
	{
		ChangeProjectileComponent changeProjectileComponent = character?.componentManager?.GetRuntime<ChangeProjectileComponent>("change_projectile");
		Check(changeProjectileComponent != null && !changeProjectileComponent.IsReleased, "The real " + label + " component must bind.");
		if (changeProjectileComponent == null || changeProjectileComponent.IsReleased)
		{
			return null;
		}
		return changeProjectileComponent;
	}

	private TowerDefenseCharacter InstantiateCharacter(string path, Vector2I gridPos)
	{
		TowerDefenseCharacter towerDefenseCharacter = ResourceLoader.Load<PackedScene>(path, null, ResourceLoader.CacheMode.Ignore)?.Instantiate<TowerDefenseCharacter>(PackedScene.GenEditState.Disabled);
		Check(GodotObject.IsInstanceValid(towerDefenseCharacter), "Real overfire character must instantiate: " + path);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			return null;
		}
		towerDefenseCharacter.inGame = false;
		towerDefenseCharacter.editorPreviewMode = true;
		towerDefenseCharacter.camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT;
		towerDefenseCharacter.gridPos = gridPos;
		AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		return towerDefenseCharacter;
	}

	private async Task WaitFrames(int count)
	{
		for (int i = 0; i < count; i++)
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
			GD.PushError("[BulletFieldSequentialOverfireRuntimeTest] " + message);
		}
	}

	private static void QueueFreeIfValid(Node node)
	{
		if (GodotObject.IsInstanceValid(node))
		{
			node.QueueFree();
		}
	}

	private static bool HasCommandLineArgument(string expected)
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		for (int i = 0; i < cmdlineUserArgs.Length; i++)
		{
			if (string.Equals(cmdlineUserArgs[i], expected, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static int ReadStressCount()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			if (text.StartsWith("--sequential-overfire-stress=", StringComparison.OrdinalIgnoreCase))
			{
				string text2 = text;
				int length = "--sequential-overfire-stress=".Length;
				if (int.TryParse(text2.Substring(length, text2.Length - length), out var result))
				{
					return Math.Clamp(result, 0, 60000);
				}
			}
		}
		return 0;
	}
}
