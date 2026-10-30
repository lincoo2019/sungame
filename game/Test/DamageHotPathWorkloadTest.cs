using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;
using Godot.Collections;

public partial class DamageHotPathWorkloadTest : Node
{
	private readonly record struct Measurement(string Name, int Calls, double Milliseconds, long AllocatedBytes)
	{
		public double NanosecondsPerCall
		{
			get
			{
				if (Calls <= 0)
				{
					return 0.0;
				}
				return Milliseconds * 1000000.0 / (double)Calls;
			}
		}

		public double AllocatedBytesPerCall
		{
			get
			{
				if (Calls <= 0)
				{
					return 0.0;
				}
				return (double)AllocatedBytes / (double)Calls;
			}
		}
	}

	private readonly record struct StressMeasurement(string Name, long Calls, double Milliseconds, int BatchSamples, double BatchP50Milliseconds, double BatchP95Milliseconds, double BatchP99Milliseconds, double BatchMaxMilliseconds, long ThreadAllocatedBytes, long TotalAllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections, bool Passed)
	{
		public double CallsPerSecond
		{
			get
			{
				if (!(Milliseconds > 0.0))
				{
					return 0.0;
				}
				return (double)Calls * 1000.0 / Milliseconds;
			}
		}

		public double NanosecondsPerCall
		{
			get
			{
				if (Calls <= 0)
				{
					return 0.0;
				}
				return Milliseconds * 1000000.0 / (double)Calls;
			}
		}
	}

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const double DurableHitpoints = 1000000000.0;

	private const int NonModifierBuffCount = 16;

	private const int HitBody = 2;

	private const int HitShield = 1;

	private static readonly Action NoopDespawn = delegate
	{
	};

	private string _runtimeProfile = "unknown";

	private readonly List<TowerDefenseCharacter> _targets = new List<TowerDefenseCharacter>();

	private readonly TowerDefenseProjectileConfig _projectileConfig = new TowerDefenseProjectileConfig
	{
		name = "DamageHotPathWorkload",
		baseDamage = 1.0,
		damageFlags = 2,
		fireMethodFlags = 0,
		rangeType = "Default",
		useRange = false
	};

	[Export(PropertyHint.None, "")]
	public int TargetCount { get; set; } = 64;

	[Export(PropertyHint.None, "")]
	public int PassCount { get; set; } = 100;

	[Export(PropertyHint.None, "")]
	public int WarmupPassCount { get; set; } = 10;

	[Export(PropertyHint.None, "")]
	public int ShieldPassCount { get; set; } = 5;

	[Export(PropertyHint.None, "")]
	public string Mode { get; set; } = "short";

	[Export(PropertyHint.None, "")]
	public double StressWarmupSeconds { get; set; } = 10.0;

	[Export(PropertyHint.None, "")]
	public double StressMeasureSeconds { get; set; } = 10.0;

	[Export(PropertyHint.None, "")]
	public int StressMaxBatchSamples { get; set; } = 1000000;

	[Export(PropertyHint.None, "")]
	public bool EnableDamageFlash { get; set; }

	[Export(PropertyHint.None, "")]
	public bool EnableHealthBarFeedback { get; set; }

	public override void _Ready()
	{
		ApplyCommandLineArguments();
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		if (packedScene == null || !CreateTargets(packedScene))
		{
			GD.PrintErr("[DamageHotPathWorkloadTest] Failed to create configured zombie targets.");
			GetTree().Quit(2);
		}
		else if (Mode.Equals("stress", StringComparison.OrdinalIgnoreCase))
		{
			RunStress(packedScene);
		}
		else if (!Mode.Equals("short", StringComparison.OrdinalIgnoreCase))
		{
			GD.PrintErr("[DamageHotPathWorkloadTest] Unsupported mode '" + Mode + "'.");
			GetTree().Quit(2);
		}
		else
		{
			RunShort(packedScene);
		}
	}

	private void RunShort(PackedScene zombieScene)
	{
		TowerDefensePerfProfiler.Reset();
		TowerDefensePerfProfiler.Enabled = true;
		TowerDefensePerfProfiler.DetailedHotPathMetrics = true;
		RunPasses(WarmupPassCount, ApplyProjectileDamage);
		RunPasses(WarmupPassCount, ApplyExplosionDamage);
		RunPasses(1, ApplyShieldDamage);
		RunPasses(WarmupPassCount, ApplyBuffModifier);
		Measurement measurement = Measure("projectile", ApplyProjectileDamage);
		Measurement measurement2 = Measure("explosion", ApplyExplosionDamage);
		Measurement measurement3 = Measure("shield", ShieldPassCount, ApplyShieldDamage);
		Measurement measurement4 = Measure("buffModifier", ApplyBuffModifier);
		PrintMeasurement(measurement);
		PrintMeasurement(measurement2);
		PrintMeasurement(measurement3);
		PrintMeasurement(measurement4);
		bool flag = ValidateArmorRemovalTraversal();
		bool flag2 = ValidateStableArmorDamageBoundaries();
		bool flag3 = ValidateExplosionOverflowTraversal(zombieScene);
		bool flag4 = ValidateBuffModifierIndex();
		bool flag5 = ValidateDamagePipelineEntry();
		bool flag6 = ValidateStableBodyDamageBoundaries(zombieScene);
		bool flag7 = ValidateStableProjectileBodyFastPath(zombieScene);
		bool flag8 = ValidateHurtComponentRuntimeCache(zombieScene);
		bool flag9 = ValidateProjectileEventDispatch(zombieScene);
		bool flag10 = _targets.Count == TargetCount && measurement.Calls == TargetCount * PassCount && measurement2.Calls == TargetCount * PassCount && measurement3.Calls == TargetCount * ShieldPassCount && measurement4.Calls == TargetCount * PassCount && double.IsFinite(measurement.Milliseconds) && double.IsFinite(measurement2.Milliseconds) && double.IsFinite(measurement3.Milliseconds) && double.IsFinite(measurement4.Milliseconds) && measurement.AllocatedBytes >= 0 && measurement2.AllocatedBytes >= 0 && measurement3.AllocatedBytes >= 0 && measurement4.AllocatedBytes >= 0 && flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9;
		GD.Print($"[DamageHotPathWorkloadResult] targets={_targets.Count}/{TargetCount} passes={PassCount} shieldPasses={ShieldPassCount} warmupPasses={WarmupPassCount} armorRemovalSafe={flag} stableArmorDamageBoundariesSafe={flag2} explosionOverflowSafe={flag3} buffModifierSafe={flag4} pipelineEntrySafe={flag5} stableBodyDamageBoundariesSafe={flag6} stableProjectileBodyFastPathSafe={flag7} hurtComponentRuntimeCacheSafe={flag8} projectileEventDispatchSafe={flag9} passed={flag10}");
		TowerDefensePerfProfiler.DetailedHotPathMetrics = false;
		TowerDefensePerfProfiler.Enabled = false;
		TowerDefensePerfProfiler.Reset();
		GetTree().Quit((!flag10) ? 1 : 0);
	}

	private void RunStress(PackedScene zombieScene)
	{
		TowerDefensePerfProfiler.DetailedHotPathMetrics = false;
		TowerDefensePerfProfiler.Enabled = false;
		TowerDefensePerfProfiler.Reset();
		RunStressWarmup();
		StressMeasurement measurement = MeasureStress("projectile", ApplyProjectileDamage);
		StressMeasurement measurement2 = MeasureStress("explosion", ApplyExplosionDamage);
		StressMeasurement measurement3 = MeasureStress("shield", ApplyShieldDamage);
		StressMeasurement measurement4 = MeasureStress("buffModifier", ApplyBuffModifier);
		bool flag = ValidateArmorRemovalTraversal();
		bool flag2 = ValidateStableArmorDamageBoundaries();
		bool flag3 = ValidateExplosionOverflowTraversal(zombieScene);
		bool flag4 = ValidateBuffModifierIndex();
		bool flag5 = ValidateDamagePipelineEntry();
		bool flag6 = ValidateStableBodyDamageBoundaries(zombieScene);
		bool flag7 = ValidateStableProjectileBodyFastPath(zombieScene);
		bool flag8 = ValidateHurtComponentRuntimeCache(zombieScene);
		bool flag9 = ValidateProjectileEventDispatch(zombieScene);
		bool flag10 = _targets.Count == TargetCount && flag && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9;
		PrintStressMeasurement(measurement, flag10);
		PrintStressMeasurement(measurement2, flag10);
		PrintStressMeasurement(measurement3, flag10);
		PrintStressMeasurement(measurement4, flag10);
		bool flag11 = flag10 && measurement.Passed && measurement2.Passed && measurement3.Passed && measurement4.Passed;
		GD.Print($"[DamageHotPathWorkloadResult] mode=stress runtimeProfile={_runtimeProfile} targets={_targets.Count}/{TargetCount} warmupSeconds={StressWarmupSeconds:F3} measureSeconds={StressMeasureSeconds:F3} damageFlash={EnableDamageFlash} healthBarFeedback={EnableHealthBarFeedback} armorRemovalSafe={flag} stableArmorDamageBoundariesSafe={flag2} explosionOverflowSafe={flag3} buffModifierSafe={flag4} pipelineEntrySafe={flag5} stableBodyDamageBoundariesSafe={flag6} stableProjectileBodyFastPathSafe={flag7} hurtComponentRuntimeCacheSafe={flag8} projectileEventDispatchSafe={flag9} passed={flag11}");
		GetTree().Quit((!flag11) ? 1 : 0);
	}

	private void RunStressWarmup()
	{
		if (!(StressWarmupSeconds <= 0.0))
		{
			long num = Stopwatch.GetTimestamp() + (long)(StressWarmupSeconds * (double)Stopwatch.Frequency);
			while (Stopwatch.GetTimestamp() < num)
			{
				RunPasses(1, ApplyProjectileDamage);
				RunPasses(1, ApplyExplosionDamage);
				RunPasses(1, ApplyShieldDamage);
				RunPasses(1, ApplyBuffModifier);
			}
		}
	}

	private StressMeasurement MeasureStress(string name, Action<TowerDefenseCharacter> hit)
	{
		double[] array = new double[StressMaxBatchSamples];
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		long timestamp = Stopwatch.GetTimestamp();
		long num4 = timestamp + (long)(StressMeasureSeconds * (double)Stopwatch.Frequency);
		long num5 = 0L;
		int num6 = 0;
		bool flag = false;
		do
		{
			if (num6 >= array.Length)
			{
				flag = true;
				break;
			}
			long timestamp2 = Stopwatch.GetTimestamp();
			for (int i = 0; i < _targets.Count; i++)
			{
				hit(_targets[i]);
				num5++;
			}
			array[num6++] = (double)(Stopwatch.GetTimestamp() - timestamp2) * 1000.0 / (double)Stopwatch.Frequency;
		}
		while (Stopwatch.GetTimestamp() < num4);
		long timestamp3 = Stopwatch.GetTimestamp();
		long num7 = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		long num8 = GC.GetTotalAllocatedBytes() - totalAllocatedBytes;
		int num9 = GC.CollectionCount(0) - num;
		int num10 = GC.CollectionCount(1) - num2;
		int num11 = GC.CollectionCount(2) - num3;
		double num12 = (double)(timestamp3 - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		double num13 = BenchmarkStatistics.Percentile(array, num6, 50.0);
		double num14 = BenchmarkStatistics.Percentile(array, num6, 95.0);
		double num15 = BenchmarkStatistics.Percentile(array, num6, 99.0);
		double num16 = BenchmarkStatistics.Maximum(array, num6);
		bool passed = !flag && num5 > 0 && num6 > 0 && double.IsFinite(num12) && double.IsFinite(num13) && double.IsFinite(num14) && double.IsFinite(num15) && double.IsFinite(num16) && num7 == 0L && num8 >= 0 && num9 >= 0 && num10 >= 0 && num11 >= 0;
		if (flag)
		{
			GD.PrintErr($"[DamageHotPathWorkloadTest] Stress sample capacity exhausted for {name}: capacity={StressMaxBatchSamples}.");
		}
		return new StressMeasurement(name, num5, num12, num6, num13, num14, num15, num16, num7, num8, num9, num10, num11, passed);
	}

	private bool CreateTargets(PackedScene zombieScene)
	{
		for (int i = 0; i < TargetCount; i++)
		{
			if (!(zombieScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter))
			{
				GD.PrintErr($"[DamageHotPathWorkloadTest] Target {i}: scene root is not a character.");
				return false;
			}
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter.instance) || towerDefenseCharacter.hurtComponent == null || towerDefenseCharacter.hurtComponent.IsReleased)
			{
				GD.PrintErr($"[DamageHotPathWorkloadTest] Target {i}: instance or hurt component is unavailable.");
				return false;
			}
			towerDefenseCharacter.hurtComponent.flashOnDamage = EnableDamageFlash;
			towerDefenseCharacter.hurtComponent.markHealthBarDirty = EnableHealthBarFeedback;
			towerDefenseCharacter.instance.keepAlive = true;
			towerDefenseCharacter.instance.hitpointsNearDeath = 0.0;
			towerDefenseCharacter.instance.hitpointsSave = 1000000000.0;
			towerDefenseCharacter.instance.hitpoints = 1000000000.0;
			towerDefenseCharacter.instance.ArmorAdd("Cone");
			towerDefenseCharacter.instance.ArmorAdd("Bucket");
			towerDefenseCharacter.instance.ArmorAdd("Screendoor");
			if (towerDefenseCharacter.instance.armorHelm.Count < 1 || towerDefenseCharacter.instance.armorShield.Count < 1)
			{
				GD.PrintErr($"[DamageHotPathWorkloadTest] Target {i}: expected armor was not created (helm={towerDefenseCharacter.instance.armorHelm.Count}, shield={towerDefenseCharacter.instance.armorShield.Count}).");
				return false;
			}
			if (!towerDefenseCharacter.instance.HasHelmetArmor || !towerDefenseCharacter.instance.HasShieldArmor)
			{
				GD.PrintErr($"[DamageHotPathWorkloadTest] Target {i}: armor runtime index was not synchronized.");
				return false;
			}
			for (int j = 0; j < towerDefenseCharacter.instance.armorList.Count; j++)
			{
				TowerDefenseArmorInstance towerDefenseArmorInstance = towerDefenseCharacter.instance.armorList[j];
				towerDefenseArmorInstance.damagePointBase = 1000000000.0;
				towerDefenseArmorInstance.hitpointsSave = 1000000000.0;
				towerDefenseArmorInstance.hitPoints = 1000000000.0;
			}
			for (int k = 0; k < 16; k++)
			{
				towerDefenseCharacter.buff.AddBuff(new TowerDefenseCharacterBuffConfig
				{
					key = $"DamageHotPathNoop{k}",
					refresh = false
				});
			}
			towerDefenseCharacter.buff.AddBuff(new TowerDefenseCharacterBuffSquid());
			_targets.Add(towerDefenseCharacter);
		}
		return true;
	}

	private void ApplyProjectileDamage(TowerDefenseCharacter target)
	{
		ProjectileHitInfo info = new ProjectileHitInfo
		{
			damage = 1.0,
			damageFlags = 2,
			position = target.GlobalPosition,
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = _projectileConfig,
			fireCharacter = null,
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			collisionFlags = 0,
			gridPos = target.gridPos,
			height = 0.0,
			onSourceDespawn = NoopDespawn
		};
		target.ProjectileHurt(in info, _projectileConfig, playSplatAudio: false);
	}

	private static void ApplyExplosionDamage(TowerDefenseCharacter target)
	{
		target.ExplodeHurt(1.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
	}

	private static void ApplyShieldDamage(TowerDefenseCharacter target)
	{
		target.instance.FlagHurt(1.0, 1, playSplatAudio: false, default(Vector2), createDamagePart: false);
	}

	private static void ApplyBuffModifier(TowerDefenseCharacter target)
	{
		target.buff.SetAttackNum(1.0);
	}

	private bool ValidateBuffModifierIndex()
	{
		for (int i = 0; i < _targets.Count; i++)
		{
			if (_targets[i].buff.buffDictionary.Count < 18 || Math.Abs(_targets[i].buff.SetAttackNum(1.0) - 2.0) >= 0.0001)
			{
				return false;
			}
		}
		return true;
	}

	private static bool ValidateDamagePipelineEntry()
	{
		DamagePipeline damagePipeline = TowerDefenseManager.Instance?.damagePipeline;
		if (GodotObject.IsInstanceValid(damagePipeline))
		{
			return damagePipeline.ApplyHurt(null, 1.0, playSplatAudio: false) == 0.0;
		}
		return false;
	}

	private bool ValidateStableBodyDamageBoundaries(PackedScene zombieScene)
	{
		TowerDefenseCharacter towerDefenseCharacter = CreateBoundaryTarget(zombieScene);
		TowerDefenseCharacter towerDefenseCharacter2 = CreateBoundaryTarget(zombieScene);
		TowerDefenseCharacter towerDefenseCharacter3 = CreateBoundaryTarget(zombieScene);
		TowerDefenseCharacter towerDefenseCharacter4 = CreateBoundaryTarget(zombieScene);
		if (towerDefenseCharacter == null || towerDefenseCharacter2 == null || towerDefenseCharacter3 == null || towerDefenseCharacter4 == null)
		{
			return false;
		}
		TowerDefenseCharacterInstance instance = towerDefenseCharacter.instance;
		int stableBodyEvents = 0;
		int stableBodyDamage = 0;
		towerDefenseCharacter.OnBodyHurt += CountStableBodyDamage;
		instance.hitpoints = 1000.0;
		instance.damagePointIndex = 0;
		double num = instance.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		towerDefenseCharacter.OnBodyHurt -= CountStableBodyDamage;
		bool flag = num == 0.0 && Math.Abs(instance.hitpoints - 999.0) < 0.0001 && instance.damagePointIndex == 0 && !instance.nearDie && !instance.die && stableBodyEvents == 1 && stableBodyDamage == 1;
		TowerDefenseCharacterInstance eventBoundary = towerDefenseCharacter2.instance;
		bool flag2 = false;
		double threshold;
		if (eventBoundary.damagePoints.Count > 0)
		{
			double num2 = (double)eventBoundary.damagePoints[0]["Persontage"];
			threshold = eventBoundary.hitpointsNearDeath + (eventBoundary.hitpointsSave - eventBoundary.hitpointsNearDeath) * num2;
			eventBoundary.hitpoints = 1000.0;
			eventBoundary.damagePointIndex = 0;
			towerDefenseCharacter2.OnBodyHurt += MoveHitpointsAcrossDamagePoint;
			eventBoundary.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
			towerDefenseCharacter2.OnBodyHurt -= MoveHitpointsAcrossDamagePoint;
			flag2 = eventBoundary.damagePointIndex > 0 && Math.Abs(eventBoundary.hitpoints - (threshold - 0.5)) < 0.0001;
		}
		TowerDefenseCharacterInstance instance2 = towerDefenseCharacter3.instance;
		instance2.damagePointIndex = instance2.damagePoints.Count;
		instance2.hitpoints = instance2.hitpointsNearDeath + 1.0;
		instance2.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		bool flag3 = instance2.nearDie && !instance2.die && Math.Abs(instance2.hitpoints - instance2.hitpointsNearDeath) < 0.0001;
		TowerDefenseCharacterInstance instance3 = towerDefenseCharacter4.instance;
		instance3.keepAlive = false;
		instance3.hitpointsNearDeath = 0.0;
		instance3.hitpoints = 1.0;
		instance3.damagePointIndex = instance3.damagePoints.Count;
		instance3.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		bool flag4 = instance3.hitpoints == 0.0 && instance3.nearDie && instance3.die;
		bool flag5 = flag && flag2 && flag3 && flag4;
		GD.Print($"[StableBodyDamageBoundaryResult] stableHit={flag} eventBoundary={flag2} nearDeath={flag3} lethal={flag4} passed={flag5}");
		return flag5;
		void CountStableBodyDamage(int damage)
		{
			stableBodyEvents++;
			stableBodyDamage += damage;
		}
		void MoveHitpointsAcrossDamagePoint(int _)
		{
			eventBoundary.hitpoints = threshold + 0.5;
		}
	}

	private bool ValidateStableProjectileBodyFastPath(PackedScene zombieScene)
	{
		TowerDefenseCharacter towerDefenseCharacter = CreateBoundaryTarget(zombieScene);
		TowerDefenseCharacter towerDefenseCharacter2 = CreateBoundaryTarget(zombieScene);
		TowerDefenseCharacter towerDefenseCharacter3 = CreateBoundaryTarget(zombieScene);
		if (towerDefenseCharacter == null || towerDefenseCharacter2 == null || towerDefenseCharacter3 == null)
		{
			return false;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileConfig
		{
			name = "StableProjectileBodyFastPath",
			baseDamage = 7.0,
			damageFlags = 2,
			fireMethodFlags = 5,
			rangeType = "Default",
			useRange = false
		};
		int despawnCalls = 0;
		int stableBodyEvents = 0;
		int stableBodyDamage = 0;
		towerDefenseCharacter.OnBodyHurt += CountStableBodyDamage;
		ProjectileHitInfo info = new ProjectileHitInfo
		{
			damage = 7.0,
			damageFlags = 2,
			position = towerDefenseCharacter.GlobalPosition,
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = towerDefenseProjectileConfig,
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			hasTargetTransform = true,
			targetOriginX = towerDefenseCharacter.GlobalPosition.X,
			targetScaleX = towerDefenseCharacter.Scale.X,
			onSourceDespawn = delegate
			{
				despawnCalls++;
			}
		};
		double num = towerDefenseCharacter.instance.ProjectileHurt(in info, towerDefenseProjectileConfig, playSplatAudio: false, default(Vector2), isRange: false, createDamagePart: false);
		towerDefenseCharacter.OnBodyHurt -= CountStableBodyDamage;
		bool flag = num == 0.0 && Math.Abs(towerDefenseCharacter.instance.hitpoints - 993.0) < 0.0001 && stableBodyEvents == 1 && stableBodyDamage == 7 && despawnCalls == 0;
		int fallbackBodyEvents = 0;
		towerDefenseCharacter2.instance.invincible = true;
		towerDefenseCharacter2.OnBodyHurt += CountFallbackBodyDamage;
		ProjectileHitInfo info2 = info;
		info2.position = towerDefenseCharacter2.GlobalPosition;
		info2.targetOriginX = towerDefenseCharacter2.GlobalPosition.X;
		info2.targetScaleX = towerDefenseCharacter2.Scale.X;
		double num2 = towerDefenseCharacter2.instance.ProjectileHurt(in info2, towerDefenseProjectileConfig, playSplatAudio: false, default(Vector2), isRange: false, createDamagePart: false);
		towerDefenseCharacter2.OnBodyHurt -= CountFallbackBodyDamage;
		bool flag2 = num2 == 0.0 && Math.Abs(towerDefenseCharacter2.instance.hitpoints - 1000.0) < 0.0001 && fallbackBodyEvents == 2 && despawnCalls == 0;
		towerDefenseCharacter3.buff.AddBuff(new TowerDefenseCharacterBuffSquid());
		int modifierBodyEvents = 0;
		int modifierBodyDamage = 0;
		towerDefenseCharacter3.OnBodyHurt += CountModifierBodyDamage;
		ProjectileHitInfo info3 = info;
		info3.position = towerDefenseCharacter3.GlobalPosition;
		info3.targetOriginX = towerDefenseCharacter3.GlobalPosition.X;
		info3.targetScaleX = towerDefenseCharacter3.Scale.X;
		double num3 = towerDefenseCharacter3.instance.ProjectileHurt(in info3, towerDefenseProjectileConfig, playSplatAudio: false, default(Vector2), isRange: false, createDamagePart: false);
		towerDefenseCharacter3.OnBodyHurt -= CountModifierBodyDamage;
		bool flag3 = num3 == 0.0 && Math.Abs(towerDefenseCharacter3.instance.hitpoints - 986.0) < 0.0001 && modifierBodyEvents == 1 && modifierBodyDamage == 14 && despawnCalls == 0;
		bool flag4 = flag && flag2 && flag3;
		GD.Print($"[StableProjectileBodyFastPathResult] stableHit={flag} specialFallback={flag2} modifierFallback={flag3} passed={flag4}");
		return flag4;
		void CountFallbackBodyDamage(int _)
		{
			fallbackBodyEvents++;
		}
		void CountModifierBodyDamage(int damage)
		{
			modifierBodyEvents++;
			modifierBodyDamage += damage;
		}
		void CountStableBodyDamage(int damage)
		{
			stableBodyEvents++;
			stableBodyDamage += damage;
		}
	}

	private bool ValidateHurtComponentRuntimeCache(PackedScene zombieScene)
	{
		TowerDefenseCharacter towerDefenseCharacter = CreateBoundaryTarget(zombieScene);
		HurtComponent hurtComponent = towerDefenseCharacter?.hurtComponent;
		if (towerDefenseCharacter == null || hurtComponent == null || hurtComponent.IsReleased)
		{
			return false;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileConfig
		{
			name = "HurtComponentRuntimeCache",
			baseDamage = 7.0,
			damageFlags = 2,
			fireMethodFlags = 5,
			rangeType = "Default",
			useRange = false
		};
		ProjectileHitInfo info = new ProjectileHitInfo
		{
			damage = 7.0,
			damageFlags = 2,
			position = towerDefenseCharacter.GlobalPosition,
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = towerDefenseProjectileConfig,
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			hasTargetTransform = true,
			targetOriginX = towerDefenseCharacter.GlobalPosition.X,
			targetScaleX = towerDefenseCharacter.Scale.X
		};
		hurtComponent.SetAlive(alive: false);
		bool flag = towerDefenseCharacter.ProjectileHurt(in info, towerDefenseProjectileConfig, playSplatAudio: false) == 0.0 && Math.Abs(towerDefenseCharacter.instance.hitpoints - 1000.0) < 0.0001;
		hurtComponent.SetAlive(alive: true);
		bool flag2 = towerDefenseCharacter.ProjectileHurt(in info, towerDefenseProjectileConfig, playSplatAudio: false) == 0.0 && Math.Abs(towerDefenseCharacter.instance.hitpoints - 993.0) < 0.0001;
		bool flag3 = flag && flag2;
		GD.Print($"[HurtComponentRuntimeCacheResult] disabled={flag} restored={flag2} passed={flag3}");
		return flag3;
	}

	private bool ValidateProjectileEventDispatch(PackedScene zombieScene)
	{
		TowerDefenseCharacter towerDefenseCharacter = CreateBoundaryTarget(zombieScene);
		BattleEventBus instance = BattleEventBus.Instance;
		if (towerDefenseCharacter == null || instance == null)
		{
			return false;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileConfig
		{
			name = "ProjectileEventDispatch",
			baseDamage = 7.0,
			damageFlags = 2,
			fireMethodFlags = 5,
			rangeType = "Default",
			useRange = false
		};
		ProjectileHitInfo info = new ProjectileHitInfo
		{
			damage = 7.0,
			damageFlags = 2,
			position = towerDefenseCharacter.GlobalPosition,
			projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
			config = towerDefenseProjectileConfig,
			camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
			hasTargetTransform = true,
			targetOriginX = towerDefenseCharacter.GlobalPosition.X,
			targetScaleX = towerDefenseCharacter.Scale.X
		};
		int eventCount = 0;
		TowerDefenseCharacter eventCharacter = null;
		int eventDamage = -2147483648;
		Node eventSource = null;
		instance.OnCharacterHurt += CaptureEvent;
		try
		{
			towerDefenseCharacter.ProjectileHurt(in info, towerDefenseProjectileConfig, playSplatAudio: false);
		}
		finally
		{
			instance.OnCharacterHurt -= CaptureEvent;
		}
		bool flag = eventCount == 1 && eventCharacter == towerDefenseCharacter && eventDamage == 0 && eventSource == null && Math.Abs(towerDefenseCharacter.instance.hitpoints - 993.0) < 0.0001;
		GD.Print($"[ProjectileEventDispatchResult] events={eventCount} damage={eventDamage} sourceNull={eventSource == null} passed={flag}");
		return flag;
		void CaptureEvent(TowerDefenseCharacter character, int damage, Node source)
		{
			eventCount++;
			eventCharacter = character;
			eventDamage = damage;
			eventSource = source;
		}
	}

	private TowerDefenseCharacter CreateBoundaryTarget(PackedScene zombieScene)
	{
		if (!(zombieScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter))
		{
			return null;
		}
		towerDefenseCharacter.inGame = false;
		towerDefenseCharacter.editorPreviewMode = true;
		AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			return null;
		}
		towerDefenseCharacter.hurtComponent.flashOnDamage = false;
		towerDefenseCharacter.hurtComponent.markHealthBarDirty = false;
		TowerDefenseCharacterInstance instance = towerDefenseCharacter.instance;
		instance.armorList.Clear();
		instance.armorShield.Clear();
		instance.armorHelm.Clear();
		instance.armorBody.Clear();
		instance.armorHeadCover.Clear();
		instance.RefreshArmorRuntimeIndex();
		instance.keepAlive = true;
		instance.nearDie = false;
		instance.die = false;
		instance.hitpointsNearDeath = 100.0;
		instance.hitpointsBase = 900.0;
		instance.hitpointsSave = 1000.0;
		instance.hitpoints = 1000.0;
		return towerDefenseCharacter;
	}

	private bool ValidateStableArmorDamageBoundaries()
	{
		if (_targets.Count < 2 || _targets[1].instance.armorHelm.Count == 0)
		{
			return false;
		}
		TowerDefenseCharacter towerDefenseCharacter = _targets[1];
		TowerDefenseArmorInstance armor = towerDefenseCharacter.instance.armorHelm[0];
		armor.damagePointBase = 100.0;
		armor.hitpointScale = 1.0;
		armor.hitpointsSave = 100.0;
		armor.hitPoints = 100.0;
		armor.stagePersontage = new Array<double>();
		armor.stageIndex = 0;
		int callbackCount = 0;
		double callbackHitPoints = -1.0;
		towerDefenseCharacter.OnArmorHurt += CaptureArmorDamage;
		double num = armor.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		towerDefenseCharacter.OnArmorHurt -= CaptureArmorDamage;
		bool flag = num == 0.0 && callbackCount == 1 && Math.Abs(callbackHitPoints - 100.0) < 0.0001 && Math.Abs(armor.hitPoints - 99.0) < 0.0001;
		armor.damagePointBase = 100.0;
		armor.hitpointsSave = 100.0;
		armor.hitPoints = 51.0;
		armor.stagePersontage = new Array<double> { 0.5 };
		armor.stageIndex = 0;
		armor.DealHurt(1.0, playSplatAudio: false, default(Vector2), createDamagePart: false);
		bool flag2 = armor.stageIndex == 1 && Math.Abs(armor.hitPoints - 50.0) < 0.0001;
		bool flag3 = flag && flag2;
		GD.Print($"[StableArmorDamageBoundaryResult] callbackOrder={flag} stageBoundary={flag2} passed={flag3}");
		return flag3;
		void CaptureArmorDamage(int _)
		{
			callbackCount++;
			callbackHitPoints = armor.hitPoints;
		}
	}

	private bool ValidateArmorRemovalTraversal()
	{
		TowerDefenseCharacter towerDefenseCharacter = _targets[0];
		if (towerDefenseCharacter.instance.armorHelm.Count == 0)
		{
			return false;
		}
		TowerDefenseArmorInstance towerDefenseArmorInstance = towerDefenseCharacter.instance.armorHelm[0];
		towerDefenseArmorInstance.damagePointBase = 1.0;
		towerDefenseArmorInstance.hitpointsSave = 1.0;
		towerDefenseArmorInstance.hitPoints = 1.0;
		towerDefenseCharacter.instance.FlagHurt(2.0, 2, playSplatAudio: false, default(Vector2), createDamagePart: false);
		if (towerDefenseArmorInstance.isRemove && !towerDefenseCharacter.instance.armorHelm.Contains(towerDefenseArmorInstance))
		{
			return !towerDefenseCharacter.instance.HasHelmetArmor;
		}
		return false;
	}

	private bool ValidateExplosionOverflowTraversal(PackedScene zombieScene)
	{
		if (!(zombieScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter))
		{
			return false;
		}
		towerDefenseCharacter.inGame = false;
		towerDefenseCharacter.editorPreviewMode = true;
		AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
		if (!GodotObject.IsInstanceValid(towerDefenseCharacter.instance))
		{
			return false;
		}
		towerDefenseCharacter.instance.armorList.Clear();
		towerDefenseCharacter.instance.armorHeadCover.Clear();
		towerDefenseCharacter.instance.armorShield.Clear();
		towerDefenseCharacter.instance.armorHelm.Clear();
		towerDefenseCharacter.instance.armorBody.Clear();
		towerDefenseCharacter.instance.RefreshArmorRuntimeIndex();
		towerDefenseCharacter.instance.hitpointsBase = 10.0;
		towerDefenseCharacter.instance.hitpointsSave = 10.0;
		towerDefenseCharacter.instance.hitpoints = 10.0;
		towerDefenseCharacter.instance.hitpointsNearDeath = 0.0;
		ArmorSlotConfig slotConfig = new ArmorSlotConfig
		{
			armorName = "Cone",
			replaceMethod = "",
			damagePoint = 1.0
		};
		for (int i = 0; i < 2; i++)
		{
			TowerDefenseArmorInstance towerDefenseArmorInstance = new TowerDefenseArmorInstance(towerDefenseCharacter, slotConfig);
			towerDefenseArmorInstance.remove += towerDefenseCharacter.instance.ArmorDestroy;
			towerDefenseArmorInstance.hitpointsEmpty += towerDefenseCharacter.instance.ArmorDestroy;
			towerDefenseCharacter.instance.armorList.Add(towerDefenseArmorInstance);
			towerDefenseCharacter.instance.armorHelm.Add(towerDefenseArmorInstance);
		}
		towerDefenseCharacter.instance.RefreshArmorRuntimeIndex();
		towerDefenseCharacter.instance.ExplodeHurt(3.0, TowerDefenseEnum.EXPLOSION_DAMAGE_KIND.BOMB, playSplatAudio: false);
		bool flag = towerDefenseCharacter.instance.armorHelm.Count == 0 && !towerDefenseCharacter.instance.HasHelmetArmor && Math.Abs(towerDefenseCharacter.instance.hitpoints - 9.0) < 0.0001;
		GD.Print($"[DamageHotPathSemantics] explosionOverflowSafe={flag} remainingHelm={towerDefenseCharacter.instance.armorHelm.Count} hitpoints={towerDefenseCharacter.instance.hitpoints:F3}");
		return flag;
	}

	private Measurement Measure(string name, Action<TowerDefenseCharacter> hit)
	{
		return Measure(name, PassCount, hit);
	}

	private Measurement Measure(string name, int passes, Action<TowerDefenseCharacter> hit)
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long timestamp = Stopwatch.GetTimestamp();
		int calls = RunPasses(passes, hit);
		double milliseconds = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
		long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		return new Measurement(name, calls, milliseconds, allocatedBytes);
	}

	private int RunPasses(int passes, Action<TowerDefenseCharacter> hit)
	{
		int num = 0;
		for (int i = 0; i < passes; i++)
		{
			for (int j = 0; j < _targets.Count; j++)
			{
				hit(_targets[j]);
				num++;
			}
		}
		return num;
	}

	private static void PrintMeasurement(Measurement measurement)
	{
		GD.Print($"[DamageHotPathWorkload] name={measurement.Name} calls={measurement.Calls} totalMs={measurement.Milliseconds:F3} nsPerCall={measurement.NanosecondsPerCall:F1} allocatedBytes={measurement.AllocatedBytes} allocatedBytesPerCall={measurement.AllocatedBytesPerCall:F2}");
	}

	private void PrintStressMeasurement(StressMeasurement measurement, bool semanticsPassed)
	{
		bool value = semanticsPassed && measurement.Passed;
		GD.Print($"[DamageStressResult] runtimeProfile={_runtimeProfile} name={measurement.Name} targets={TargetCount} calls={measurement.Calls} totalMs={measurement.Milliseconds:F3} callsPerSecond={measurement.CallsPerSecond:F3} nsPerCall={measurement.NanosecondsPerCall:F3} batchSamples={measurement.BatchSamples} batchP50Ms={measurement.BatchP50Milliseconds:F6} batchP95Ms={measurement.BatchP95Milliseconds:F6} batchP99Ms={measurement.BatchP99Milliseconds:F6} batchMaxMs={measurement.BatchMaxMilliseconds:F6} threadAllocatedBytes={measurement.ThreadAllocatedBytes} totalAllocatedBytes={measurement.TotalAllocatedBytes} gen0={measurement.Gen0Collections} gen1={measurement.Gen1Collections} gen2={measurement.Gen2Collections} passed={value}");
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value2;
			int value3;
			int value4;
			string value5;
			double value6;
			double value7;
			int value8;
			string value9;
			if (TryReadInt(text, "--damage-targets=", out var value))
			{
				TargetCount = Math.Max(1, value);
			}
			else if (TryReadInt(text, "--damage-passes=", out value2))
			{
				PassCount = Math.Max(1, value2);
			}
			else if (TryReadInt(text, "--damage-warmup-passes=", out value3))
			{
				WarmupPassCount = Math.Max(0, value3);
			}
			else if (TryReadInt(text, "--damage-shield-passes=", out value4))
			{
				ShieldPassCount = Math.Max(1, value4);
			}
			else if (TryReadString(text, "--damage-mode=", out value5))
			{
				Mode = value5;
			}
			else if (TryReadDouble(text, "--damage-stress-warmup=", out value6))
			{
				StressWarmupSeconds = Math.Max(0.0, value6);
			}
			else if (TryReadDouble(text, "--damage-stress-measure=", out value7))
			{
				StressMeasureSeconds = Math.Max(0.001, value7);
			}
			else if (TryReadInt(text, "--damage-stress-max-samples=", out value8))
			{
				StressMaxBatchSamples = Math.Max(1, value8);
			}
			else if (text.Equals("--damage-feedback", StringComparison.OrdinalIgnoreCase))
			{
				EnableDamageFlash = true;
				EnableHealthBarFeedback = true;
			}
			else if (text.Equals("--damage-flash", StringComparison.OrdinalIgnoreCase))
			{
				EnableDamageFlash = true;
			}
			else if (text.Equals("--damage-health-feedback", StringComparison.OrdinalIgnoreCase))
			{
				EnableHealthBarFeedback = true;
			}
			else if (TryReadString(text, "--runtime-profile=", out value9))
			{
				_runtimeProfile = value9;
			}
		}
	}

	private static bool TryReadInt(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(arg.Substring(length, arg.Length - length), out value);
		}
		return false;
	}

	private static bool TryReadDouble(string arg, string prefix, out double value)
	{
		value = 0.0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return double.TryParse(arg.Substring(length, arg.Length - length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}

	private static bool TryReadString(string arg, string prefix, out string value)
	{
		value = string.Empty;
		if (!arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int length = prefix.Length;
		value = arg.Substring(length, arg.Length - length).Trim();
		return value.Length > 0;
	}
}
