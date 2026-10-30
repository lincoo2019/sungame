using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class TorchwoodStarSpecialProjectileRuntimeTest : Node
{
	private const string TorchwoodStarScenePath = "res://Asset/Anime/Character/Plant/Other/TorchwoodStar/Scene/TowerDefensePlantTorchwoodStar.tscn";

	private const string CatTailScenePath = "res://Asset/Anime/Character/Plant/Cover/CatTail/Scene/TowerDefensePlantCatTail.tscn";

	private const string LegacySpecialStarConfigPath = "res://Asset/Config/Projectile/Star/ShootingStartsBigStarFull.tres";

	private int _checks;

	private int _failures;

	public override async void _Ready()
	{
		TowerDefensePlant plant = null;
		TowerDefensePlantCatTail catTail = null;
		BulletField bulletField = null;
		PackedScene torchwoodScene = null;
		PackedScene catTailScene = null;
		TowerDefenseProjectileConfig catTailSpikeConfig = null;
		try
		{
			_ = 3;
			try
			{
				TowerDefenseProjectileRegistry.Init();
				bulletField = new BulletField
				{
					Name = "TorchwoodStarBulletField"
				};
				AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
				torchwoodScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Other/TorchwoodStar/Scene/TowerDefensePlantTorchwoodStar.tscn", null, ResourceLoader.CacheMode.Ignore);
				plant = torchwoodScene?.Instantiate<TowerDefensePlant>(PackedScene.GenEditState.Disabled);
				catTailScene = ResourceLoader.Load<PackedScene>("res://Asset/Anime/Character/Plant/Cover/CatTail/Scene/TowerDefensePlantCatTail.tscn", null, ResourceLoader.CacheMode.Ignore);
				catTail = catTailScene?.Instantiate<TowerDefensePlantCatTail>(PackedScene.GenEditState.Disabled);
				Check(GodotObject.IsInstanceValid(plant), "The real Yangtao Torchwood scene must instantiate.");
				Check(GodotObject.IsInstanceValid(catTail), "The real CatTail scene must instantiate.");
				if (GodotObject.IsInstanceValid(plant) && GodotObject.IsInstanceValid(catTail))
				{
					plant.inGame = false;
					plant.editorPreviewMode = true;
					catTail.inGame = false;
					catTail.editorPreviewMode = true;
					AddChild(plant, forceReadableName: false, InternalMode.Disabled);
					AddChild(catTail, forceReadableName: false, InternalMode.Disabled);
					await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
					await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
					ChangeProjectileComponent change = plant.componentManager?.GetRuntime<ChangeProjectileComponent>("change_projectile");
					Check(change != null && !change.IsReleased, "The real Yangtao Torchwood must bind its ChangeProjectile runtime.");
					if (change != null && !change.IsReleased)
					{
						catTailSpikeConfig = GetCatTailSpikeConfig(catTail);
						RunSplitBoundaryChecks(change);
						RunStructProjectileChecks(change, bulletField, plant);
						RunMeteorAnimatedRootChecks(bulletField, plant);
						for (int frame = 0; frame < 4; frame++)
						{
							await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
							await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
						}
						change.UpdateRect();
						RunCatTailStructProjectileCheck(change, bulletField, plant, catTailSpikeConfig);
					}
				}
			}
			catch (Exception value)
			{
				_failures++;
				GD.PushError($"[TorchwoodStarSpecialProjectileRuntimeTest] Unexpected exception: {value}");
			}
		}
		finally
		{
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.ClearActiveBullets();
			}
			if (GodotObject.IsInstanceValid(plant))
			{
				plant.QueueFree();
			}
			if (GodotObject.IsInstanceValid(catTail))
			{
				catTail.QueueFree();
			}
			if (GodotObject.IsInstanceValid(bulletField))
			{
				bulletField.QueueFree();
			}
			for (int frame = 0; frame < 6; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
				await ToSignal(GetTree(), SceneTree.SignalName.PhysicsFrame);
			}
			if (GodotObject.IsInstanceValid(ObjectManager.Instance))
			{
				ObjectManager.Instance.Clear();
			}
			AdobeAnimateRenderManager.ClearGpuRenderGraphCaches();
			AdobeAnimateDefinitionCache.Clear();
			catTailSpikeConfig?.Dispose();
			catTailScene?.Dispose();
			torchwoodScene?.Dispose();
			TowerDefenseGroundItemBase.ClearStaticBattleReferences();
			GC.Collect();
			GC.WaitForPendingFinalizers();
		}
		bool flag = _failures == 0;
		GD.Print($"TORCHWOOD_STAR_SPECIAL_PROJECTILE_RESULT passed={flag} checks={_checks} failures={_failures}");
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void RunSplitBoundaryChecks(ChangeProjectileComponent change)
	{
		Check(change.isStar, "The real component must keep Star split enabled for ordinary projectiles.");
		Check(change.changeName == new StringName("Star"), "The real component must use the Star conversion table.");
		string[] array = new string[10] { "BigFireStar", "BigStar", "FeverStar", "FireStar", "MeteorStar", "MeteorStarS", "PotatoStar", "SnowStar", "Star", "StarCaltrop" };
		foreach (string text in array)
		{
			StringName projectileName = new StringName(text);
			Check(TowerDefenseProjectileRegistry.IsStarProjectile(projectileName), text + " must load from the live registry as a star-family projectile.");
			Check(!change.ShouldSplitProjectileIntoStars(projectileName), text + " must never enter the ordinary-projectile Star split fallback.");
		}
		array = new string[16]
		{
			"res://Asset/Config/Projectile/Star/BigFireStar.tres", "res://Asset/Config/Projectile/Star/BigStarDefault.tres", "res://Asset/Config/Projectile/Star/FeverStar.tres", "res://Asset/Config/Projectile/Star/FireStar.tres", "res://Asset/Config/Projectile/Star/FireStarBigCatGatlingPea.tres", "res://Asset/Config/Projectile/Star/FireStarMedium.tres", "res://Asset/Config/Projectile/Star/PotatoStar.tres", "res://Asset/Config/Projectile/Star/ShootingStartsBigStarFull.tres", "res://Asset/Config/Projectile/Star/SnowStar.tres", "res://Asset/Config/Projectile/Star/StarBigCatGatlingPea.tres",
			"res://Asset/Config/Projectile/Star/StarCaltrop.tres", "res://Asset/Config/Projectile/Star/StarDefault.tres", "res://Asset/Config/Projectile/Star/StarFull.tres", "res://Asset/Config/Projectile/Star/StarMedium.tres", "res://Asset/Config/Projectile/MeteorStar/MeteorStar.tres", "res://Asset/Config/Projectile/MeteorStar/MeteorStarS.tres"
		};
		foreach (string text2 in array)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = ResourceLoader.Load<TowerDefenseProjectileConfig>(text2, null, ResourceLoader.CacheMode.Ignore);
			Check(towerDefenseProjectileConfig != null, "Legacy star-family config must load: " + text2);
			if (towerDefenseProjectileConfig != null)
			{
				Check(towerDefenseProjectileConfig.isStar, "Legacy star-family config must declare its identity: " + text2);
				Check(!change.ShouldSplitProjectileIntoStars(towerDefenseProjectileConfig.NameSN, towerDefenseProjectileConfig), $"Legacy star-family projectile {towerDefenseProjectileConfig.NameSN} must not enter the ordinary-projectile Star split fallback.");
			}
		}
		Check(change.ShouldSplitProjectileIntoStars(new StringName("Pea")), "An ordinary unmapped shooter must still split into five Stars.");
		Check(!change.ShouldConvertLowDamageIntoStraightStar(new StringName("Pea"), null, 20.0), "An ordinary projectile at exactly 20 damage must keep the five-Star split rule.");
		Check(!change.ShouldConvertLowDamageIntoStraightStar(new StringName("Pea"), null, 20.01), "An ordinary projectile above 20 damage must keep the five-Star split rule.");
		Check(!change.ShouldSplitProjectileIntoStars(new StringName("FireStar")), "A Star conversion target must not split again in the same zone.");
		Check(TowerDefenseProjectileRegistry.GetProjectileChange("Star", "Star") == new StringName("FireStar"), "The supported ordinary Star must still ignite into FireStar.");
	}

	private void RunStructProjectileChecks(ChangeProjectileComponent change, BulletField bulletField, TowerDefensePlant plant)
	{
		Check(GodotObject.IsInstanceValid(bulletField), "The struct-projectile test requires the live BulletField runtime.");
		if (!GodotObject.IsInstanceValid(bulletField))
		{
			return;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(new StringName("Star")).BuildConfig();
		Check(towerDefenseProjectileConfig != null, "The canonical Star config must build for the struct path.");
		if (towerDefenseProjectileConfig != null)
		{
			int num = SpawnStructProjectile(bulletField, towerDefenseProjectileConfig, plant);
			Check(num >= 0, "The canonical Star must spawn through the struct path.");
			if (num >= 0)
			{
				change.OnBulletIntersect(ref bulletField.GetBulletDataRef(num), num);
				Check(bulletField.IsBulletActive(num), "The struct Star must remain active after conversion.");
				if (bulletField.IsBulletActive(num))
				{
					Check(bulletField.GetBulletDataRef(num).config?.NameSN == new StringName("FireStar"), "The real struct path must convert ordinary Star into FireStar.");
				}
			}
		}
		int activeCount = bulletField.ActiveCount;
		TowerDefenseProjectileConfig towerDefenseProjectileConfig2 = LoadLegacySpecialStarConfig();
		Check(towerDefenseProjectileConfig2 != null, "The legacy ShootingStartsBigStar config must load for the struct path.");
		if (towerDefenseProjectileConfig2 != null)
		{
			int num2 = SpawnStructProjectile(bulletField, towerDefenseProjectileConfig2, plant);
			Check(num2 >= 0, "The legacy ShootingStartsBigStar must spawn through the struct path.");
			if (num2 >= 0)
			{
				change.OnBulletIntersect(ref bulletField.GetBulletDataRef(num2), num2);
				Check(bulletField.IsBulletActive(num2), "The struct legacy special Star must be preserved instead of splitting.");
				Check(bulletField.ActiveCount == activeCount + 1, "The struct legacy special Star must not create five replacement Stars.");
				if (bulletField.IsBulletActive(num2))
				{
					Check(bulletField.GetBulletDataRef(num2).config == towerDefenseProjectileConfig2, "The struct legacy special Star must keep its original config.");
				}
			}
		}
		bulletField.ClearActiveBullets();
	}

	private void RunMeteorAnimatedRootChecks(BulletField bulletField, TowerDefensePlant plant)
	{
		string[] array = new string[2] { "MeteorStar", "MeteorStarS" };
		foreach (string text in array)
		{
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = BulletField.BuildTemplateConfig(new StringName(text), new StringName("Default"));
			Check(towerDefenseProjectileConfig != null, text + " must build from the live projectile registry.");
			if (towerDefenseProjectileConfig == null)
			{
				continue;
			}
			Node2D node2D = towerDefenseProjectileConfig.projectileScene?.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
			Check(GodotObject.IsInstanceValid(node2D), text + " must instantiate its authored Adobe Animate root.");
			float num = (GodotObject.IsInstanceValid(node2D) ? node2D.Rotation : 0f);
			Check(Mathf.IsEqualApprox(num, -(float)Math.PI / 2f), text + " must retain its authored -90 degree root rotation.");
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.Free();
			}
			for (int j = 1; j <= 2; j++)
			{
				int num2 = SpawnStructProjectile(bulletField, towerDefenseProjectileConfig, plant);
				Check(num2 >= 0, $"{text} pass {j} must spawn through Compact Crowd.");
				if (num2 >= 0)
				{
					ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num2);
					Check(bulletDataRef.renderMode == BulletRenderMode.ANIMATED_MESH, $"{text} pass {j} must use the animated mesh renderer.");
					Check(Mathf.IsEqualApprox(bulletDataRef.spriteRotation, num), $"{text} pass {j} must preserve its authored root rotation.");
				}
			}
		}
		bulletField.ClearActiveBullets();
	}

	private TowerDefenseProjectileConfig GetCatTailSpikeConfig(TowerDefensePlantCatTail catTail)
	{
		FireComponent fireComponent = catTail.componentManager?.GetRuntime<FireComponent>("character.fire");
		Check(fireComponent != null && !fireComponent.IsReleased, "The real CatTail must bind its authored FireComponent.");
		Check(fireComponent != null && fireComponent.fireNum == 2, "The real CatTail must retain its two-spike volley.");
		TowerDefenseProjectileCreateData towerDefenseProjectileCreateData = ((fireComponent != null && fireComponent.fireCheckList.Count > 0) ? fireComponent.fireCheckList[0].GetProjectile() : null);
		Check(towerDefenseProjectileCreateData?.projectileName == new StringName("Spike"), "The real CatTail volley must use the reported small Spike projectile.");
		Check(towerDefenseProjectileCreateData != null && (towerDefenseProjectileCreateData.fireMethodFlags & 0x20) != 0, "The real CatTail Spike must retain its authored tracking flag before Starfire.");
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = towerDefenseProjectileCreateData?.BuildConfig();
		Check(towerDefenseProjectileConfig?.NameSN == new StringName("Spike"), "The real CatTail Spike config must build from its authored create data.");
		return towerDefenseProjectileConfig;
	}

	private void RunCatTailStructProjectileCheck(ChangeProjectileComponent change, BulletField bulletField, TowerDefensePlant plant, TowerDefenseProjectileConfig catTailSpikeConfig)
	{
		Check(catTailSpikeConfig != null, "The CatTail Starfire struct check requires the real Spike config.");
		if (catTailSpikeConfig == null)
		{
			return;
		}
		bulletField.ClearActiveBullets();
		int num = SpawnStructProjectile(bulletField, catTailSpikeConfig, plant);
		Check(num >= 0, "The real CatTail Spike must spawn through the struct path.");
		List<int> spawnedIndices;
		if (num >= 0)
		{
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(num);
			Check(!change.ShouldConvertLowDamageIntoStraightStar(bulletDataRef.config.NameSN, bulletDataRef.config, bulletDataRef.damage), "The real 20-damage CatTail Spike must use the five-Star split rule.");
			spawnedIndices = new List<int>();
			bulletField.OnBulletSpawned += CaptureSpawn;
			change.OnBulletIntersect(ref bulletDataRef, num);
			bulletField.OnBulletSpawned -= CaptureSpawn;
			Check(spawnedIndices.Count == 5 && bulletField.ActiveCount == 5 && !bulletField.IsBulletActive(num), "One 20-damage CatTail Spike crossing Starfire must split into exactly five Stars.");
			Check(AllSpawnedStars(bulletField, spawnedIndices), "The exact-20 split must create five canonical Yangtao Stars.");
			Check(AllSpawnedStarsAreStraight(bulletField, spawnedIndices), "The exact-20 split Stars must use ordinary straight-shot behavior.");
			Check(SpawnedDirectionsMatch(bulletField, spawnedIndices, change.starDirections), "The exact-20 split must retain the authored five directions.");
			Check(AllSpawnedStarsReleaseTargets(bulletField, spawnedIndices), "The exact-20 split Stars must release CatTail tracking targets.");
			Check(AllSpawnedStarsExcludeZone(bulletField, spawnedIndices, change), "The exact-20 split Stars must be excluded from immediate same-zone recursion.");
			bulletField.ClearActiveBullets();
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = (TowerDefenseProjectileConfig)catTailSpikeConfig.Duplicate(deep: true);
			towerDefenseProjectileConfig.baseDamage = 20.01;
			int num2 = SpawnStructProjectile(bulletField, towerDefenseProjectileConfig, plant);
			Check(num2 >= 0, "The above-threshold CatTail Spike must spawn through the struct path.");
			if (num2 >= 0)
			{
				ref BulletData bulletDataRef2 = ref bulletField.GetBulletDataRef(num2);
				Check(!change.ShouldConvertLowDamageIntoStraightStar(bulletDataRef2.config.NameSN, bulletDataRef2.config, bulletDataRef2.damage), "A projectile above 20 damage must not use the one-straight-Star rule.");
				spawnedIndices.Clear();
				bulletField.OnBulletSpawned += CaptureSpawn;
				change.OnBulletIntersect(ref bulletDataRef2, num2);
				bulletField.OnBulletSpawned -= CaptureSpawn;
				Check(spawnedIndices.Count == 5 && bulletField.ActiveCount == 5 && !bulletField.IsBulletActive(num2), "A projectile above 20 damage must still split into exactly five Stars.");
				Check(AllSpawnedStars(bulletField, spawnedIndices) && AllSpawnedStarsAreStraight(bulletField, spawnedIndices) && SpawnedDirectionsMatch(bulletField, spawnedIndices, change.starDirections) && AllSpawnedStarsReleaseTargets(bulletField, spawnedIndices) && AllSpawnedStarsExcludeZone(bulletField, spawnedIndices, change), "The above-threshold split must retain the authored five-Star behavior.");
			}
			towerDefenseProjectileConfig.Dispose();
			bulletField.ClearActiveBullets();
		}
		void CaptureSpawn(int spawnedIndex)
		{
			spawnedIndices.Add(spawnedIndex);
		}
	}

	private static bool AllSpawnedStars(BulletField bulletField, List<int> indices)
	{
		if (indices.Count != 5)
		{
			return false;
		}
		foreach (int index in indices)
		{
			if (!bulletField.IsBulletActive(index) || bulletField.GetBulletDataRef(index).config?.NameSN != new StringName("Star"))
			{
				return false;
			}
		}
		return true;
	}

	private static bool AllSpawnedStarsAreStraight(BulletField bulletField, List<int> indices)
	{
		foreach (int index in indices)
		{
			if (!bulletField.IsBulletActive(index))
			{
				return false;
			}
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
			if (bulletDataRef.trackOpen || (bulletDataRef.fireMethodFlags & 0x20) != 0)
			{
				return false;
			}
		}
		return indices.Count == 5;
	}

	private static bool AllSpawnedStarsReleaseTargets(BulletField bulletField, List<int> indices)
	{
		foreach (int index in indices)
		{
			if (!bulletField.IsBulletActive(index))
			{
				return false;
			}
			ref BulletData bulletDataRef = ref bulletField.GetBulletDataRef(index);
			if (GodotObject.IsInstanceValid(bulletDataRef.target) || GodotObject.IsInstanceValid(bulletDataRef.magneticTarget))
			{
				return false;
			}
		}
		return indices.Count == 5;
	}

	private static bool AllSpawnedStarsExcludeZone(BulletField bulletField, List<int> indices, ChangeProjectileComponent change)
	{
		foreach (int index in indices)
		{
			if (!bulletField.IsBulletActive(index) || bulletField.GetBulletDataRef(index).zoneExclusionOwner != change)
			{
				return false;
			}
		}
		return indices.Count == 5;
	}

	private static bool SpawnedDirectionsMatch(BulletField bulletField, List<int> indices, Array<float> directions)
	{
		if (indices.Count != directions.Count)
		{
			return false;
		}
		foreach (float direction in directions)
		{
			Vector2 other = Vector2.FromAngle(Mathf.DegToRad(direction)).Normalized();
			bool flag = false;
			foreach (int index in indices)
			{
				if (bulletField.IsBulletActive(index) && bulletField.GetBulletDataRef(index).vel.Normalized().IsEqualApprox(other))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return false;
			}
		}
		return true;
	}

	private int SpawnStructProjectile(BulletField bulletField, TowerDefenseProjectileConfig config, TowerDefensePlant plant)
	{
		Vector2 vel = Vector2.Right * 500f;
		return bulletField.TrySpawnFromConfig(config, plant.GlobalPosition, vel, vel.Length(), null, plant.camp, plant.gridPos, plant.gridPos.Y, new Rect2(-10000f, -10000f, 20000f, 20000f), null, 0.0, 0.0, config.collisionFlags);
	}

	private static TowerDefenseProjectileConfig LoadLegacySpecialStarConfig()
	{
		return ResourceLoader.Load<TowerDefenseProjectileConfig>("res://Asset/Config/Projectile/Star/ShootingStartsBigStarFull.tres", null, ResourceLoader.CacheMode.Ignore);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures++;
			GD.PushError("[TorchwoodStarSpecialProjectileRuntimeTest] " + message);
		}
	}
}
