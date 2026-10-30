using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;

public partial class BulletFieldPenetrationWorkloadTest : Node2D
{
	private readonly record struct Measurement(string Name, int Samples, double TotalMilliseconds, double MedianMilliseconds, double MaximumMilliseconds, long ThreadAllocatedBytes, long TotalAllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections)
	{
		public double AverageMilliseconds
		{
			get
			{
				if (Samples <= 0)
				{
					return 0.0;
				}
				return TotalMilliseconds / (double)Samples;
			}
		}
	}

	private readonly record struct CombatWaveMeasurement(bool Passed, double ImpactMilliseconds, double RangeDispatchMilliseconds, long ExpectedPenetrationDamageCalculations, long ExpectedRangeDamageCalculations, long ActualImpactDamageCalculations, long ActualRangeDamageCalculations, long ActualDamageCalculations, long RememberedPenetrationHits, int ImpactUpdatePasses, int RangeWorkItems, int RangeDispatchWorkUnits, int RangeDispatchFrames, double RangeDispatchMaxFrameMilliseconds, int ActivePenetrationBullets, long ThreadAllocatedBytes, long TotalAllocatedBytes, int Gen0Collections, int Gen1Collections, int Gen2Collections)
	{
		public double TotalMilliseconds => ImpactMilliseconds + RangeDispatchMilliseconds;
	}

	private enum DirectDamagePath
	{
		Character,
		CharacterCachedTransform,
		CharacterCachedTransformFallback,
		HurtComponent,
		HurtComponentCachedTransform,
		PipelinePrepared,
		PipelinePreparedCachedTransform,
		Instance,
		InstanceCachedTransform,
		FlagHurt,
		DealHurt
	}

	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private const string ProjectileScenePath = "res://Asset/Config/Projectile/Pea/Sprite/FirePea/FirePea.tscn";

	private const double DurableHitpoints = 1000000000.0;

	private const int HitBody = 2;

	private static readonly Action NoopDespawn = delegate
	{
	};

	private readonly List<TowerDefenseCharacter> _targets = new List<TowerDefenseCharacter>();

	private readonly List<Vector2> _targetPositions = new List<Vector2>();

	private readonly List<int> _bulletIndices = new List<int>();

	private readonly List<Vector2> _cellPositions = new List<Vector2>();

	private readonly List<Vector2I> _cellGridPositions = new List<Vector2I>();

	private readonly List<int> _bulletsPerCell = new List<int>();

	private readonly List<int> _penetrationBulletsPerCell = new List<int>();

	private readonly List<int> _rangeBulletsPerCell = new List<int>();

	private readonly List<int> _targetsPerCell = new List<int>();

	private readonly List<TowerDefenseCharacter> _candidateProbe = new List<TowerDefenseCharacter>();

	private readonly List<double> _rangeDispatchFrameMilliseconds = new List<double>(2048);

	private BulletField _bulletField;

	private TowerDefenseProjectileConfig _projectileConfig;

	private TowerDefenseProjectileConfig _rangeProjectileConfig;

	private TowerDefenseProjectileConfig _forcedFallbackProjectileConfig;

	private TowerDefenseManager _manager;

	private ProjectileUpdateManager _projectileUpdateManager;

	private ProcessModeEnum _originalProjectileManagerProcessMode;

	private Vector2 _originalGridSize;

	private Vector2 _originalGridBegin;

	private Vector2I _originalGridNum;

	private ulong _measurementFrame;

	private ulong _instanceIdChecksum;

	private bool _targetsRegistered;

	private int _penetrationBulletCount;

	private int _rangeBulletCount;

	[Export(PropertyHint.None, "")]
	public int BulletCount { get; set; } = 10000;

	[Export(PropertyHint.None, "")]
	public int TargetCount { get; set; } = 500;

	[Export(PropertyHint.None, "")]
	public int ColumnCount { get; set; } = 10;

	[Export(PropertyHint.None, "")]
	public int RowCount { get; set; } = 5;

	[Export(PropertyHint.None, "")]
	public int SampleCount { get; set; } = 3;

	[Export(PropertyHint.None, "")]
	public float BulletScale { get; set; } = 0.12f;

	[Export(PropertyHint.None, "")]
	public string RuntimeProfile { get; set; } = "unknown";

	[Export(PropertyHint.None, "")]
	public bool GeometryInvalidationProbe { get; set; }

	[Export(PropertyHint.None, "")]
	public bool CrossLineVelocityProbe { get; set; }

	[Export(PropertyHint.None, "")]
	public bool SweptCollisionProbe { get; set; }

	[Export(PropertyHint.None, "")]
	public bool TrackSweptCollisionProbe { get; set; }

	[Export(PropertyHint.None, "")]
	public bool RenderSync { get; set; }

	[Export(PropertyHint.None, "")]
	public bool RangePenetrationStress { get; set; }

	[Export(PropertyHint.None, "")]
	public int RangeBulletPercentage { get; set; } = 50;

	[Export(PropertyHint.None, "")]
	public int StressWarmupWaves { get; set; } = 1;

	public override void _Ready()
	{
		ApplyCommandLineArguments();
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		int exitCode = 2;
		try
		{
			if (PrepareWorkload())
			{
				if (GeometryInvalidationProbe)
				{
					exitCode = ((!RunGeometryInvalidationProbe()) ? 1 : 0);
					return;
				}
				if (CrossLineVelocityProbe)
				{
					exitCode = ((!RunCrossLineVelocityProbe()) ? 1 : 0);
					return;
				}
				if (SweptCollisionProbe)
				{
					exitCode = ((!RunSweptCollisionProbe()) ? 1 : 0);
					return;
				}
				if (TrackSweptCollisionProbe)
				{
					exitCode = ((!RunTrackSweptCollisionProbe()) ? 1 : 0);
					return;
				}
				if (RangePenetrationStress)
				{
					exitCode = ((!RunRangePenetrationStress()) ? 1 : 0);
					return;
				}
				RunUntimedUpdates(2);
				Measurement measurement = MeasureUpdates("baseline-no-targets", SampleCount);
				MoveTargets(overlapping: false);
				RegisterTargets();
				long num = CountRegistryEntriesScannedPerUpdate();
				long num2 = CountCandidateChecksPerUpdate();
				RunUntimedUpdates(1);
				Measurement measurement2 = MeasureUpdates("candidate-miss", SampleCount);
				int collisionCandidateCacheBuildCountForTest = _bulletField.CollisionCandidateCacheBuildCountForTest;
				int collisionCandidateCacheHitCountForTest = _bulletField.CollisionCandidateCacheHitCountForTest;
				int collisionOverlapCacheBuildCountForTest = _bulletField.CollisionOverlapCacheBuildCountForTest;
				int collisionOverlapCacheHitCountForTest = _bulletField.CollisionOverlapCacheHitCountForTest;
				MoveTargets(overlapping: true);
				long num3 = CountExpectedOverlaps();
				Measurement measurement3 = MeasureUpdates("first-entry-hit", 1);
				long num4 = CountRememberedPenetrationTargets();
				Measurement measurement4 = MeasureUpdates("steady-overlap", SampleCount);
				long num5 = CountRememberedPenetrationTargets();
				int penetrationOverlapSetFastPathCountForTest = _bulletField.PenetrationOverlapSetFastPathCountForTest;
				int penetrationRememberedOrderFastPathCountForTest = _bulletField.PenetrationRememberedOrderFastPathCountForTest;
				int penetrationOverlapSetCleanupCountForTest = _bulletField.PenetrationOverlapSetCleanupCountForTest;
				double value = ((num3 > 0) ? ((double)measurement4.ThreadAllocatedBytes / (double)(num3 * SampleCount)) : 0.0);
				MoveTargets(overlapping: false);
				Measurement measurement5 = MeasureUpdates("overlap-exit", 1);
				long num6 = CountRememberedPenetrationTargets();
				Measurement measurement6 = MeasureInstanceIdLookup(num4);
				ulong num7 = AudioManager.Instance?.DuplicateSfxRejectCountForTest ?? 0;
				ulong num8 = AudioManager.Instance?.AudioResourceLookupCountForTest ?? 0;
				Measurement measurement7 = MeasureDirectDamage(num4, playSplatAudio: true);
				ulong value2 = (AudioManager.Instance?.DuplicateSfxRejectCountForTest ?? 0) - num7;
				ulong value3 = (AudioManager.Instance?.AudioResourceLookupCountForTest ?? 0) - num8;
				Measurement measurement8 = MeasureDirectDamage(num4, playSplatAudio: false);
				Measurement measurement9 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.CharacterCachedTransform);
				MeasureDirectDamage(1L, playSplatAudio: false, DirectDamagePath.CharacterCachedTransformFallback);
				Measurement measurement10 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.CharacterCachedTransformFallback);
				Measurement measurement11 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.HurtComponent);
				Measurement measurement12 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.HurtComponentCachedTransform);
				Measurement measurement13 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.PipelinePrepared);
				Measurement measurement14 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.PipelinePreparedCachedTransform);
				Measurement measurement15 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.Instance);
				Measurement measurement16 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.InstanceCachedTransform);
				Measurement measurement17 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.FlagHurt);
				Measurement measurement18 = MeasureDirectDamage(num4, playSplatAudio: false, DirectDamagePath.DealHurt);
				bool flag = AllBulletsUseMultiMesh();
				string name = DisplayServer.GetName();
				bool flag2 = !RenderSync || !name.Equals("headless", StringComparison.OrdinalIgnoreCase);
				bool flag3 = TargetCount <= _cellPositions.Count * _bulletField.PenetrationInlineTargetCapacityForTest;
				bool flag4 = _bulletField.ActiveCount == BulletCount && _targets.Count == TargetCount && flag && num >= num2 && num2 >= num3 && collisionCandidateCacheBuildCountForTest > 0 && collisionOverlapCacheBuildCountForTest > 0 && collisionOverlapCacheHitCountForTest > collisionOverlapCacheBuildCountForTest && num4 == num3 && (!flag3 || measurement3.ThreadAllocatedBytes <= 64000) && num5 == num3 && penetrationOverlapSetFastPathCountForTest == BulletCount && penetrationRememberedOrderFastPathCountForTest == num3 && penetrationOverlapSetCleanupCountForTest == 0 && measurement4.ThreadAllocatedBytes <= 64000 + num3 * SampleCount && measurement8.ThreadAllocatedBytes <= 64000 && flag2 && num6 == 0;
				PrintMeasurement(measurement);
				PrintMeasurement(measurement2);
				PrintMeasurement(measurement3);
				PrintMeasurement(measurement4);
				PrintMeasurement(measurement5);
				PrintMeasurement(measurement6);
				PrintMeasurement(measurement7);
				PrintMeasurement(measurement8);
				PrintMeasurement(measurement9);
				PrintMeasurement(measurement10);
				PrintMeasurement(measurement11);
				PrintMeasurement(measurement12);
				PrintMeasurement(measurement13);
				PrintMeasurement(measurement14);
				PrintMeasurement(measurement15);
				PrintMeasurement(measurement16);
				PrintMeasurement(measurement17);
				PrintMeasurement(measurement18);
				GD.Print($"[PenetrationAudioWorkload] calls={num4} duplicateSfxRejects={value2} audioResourceLookups={value3}");
				GD.Print($"[PenetrationWorkloadBreakdown] runtimeProfile={RuntimeProfile} renderBaselineMs={measurement.AverageMilliseconds:F3} candidateScanExcessMs={measurement2.AverageMilliseconds - measurement.AverageMilliseconds:F3} firstEntryExcessMs={measurement3.AverageMilliseconds - measurement.AverageMilliseconds:F3} steadyOverlapExcessMs={measurement4.AverageMilliseconds - measurement.AverageMilliseconds:F3} steadyAllocatedBytesPerOverlap={value:F3} overlapExitExcessMs={measurement5.AverageMilliseconds - measurement.AverageMilliseconds:F3} instanceIdLookupMs={measurement6.TotalMilliseconds:F3} instanceIdLookupChecksum={_instanceIdChecksum} directDamageMs={measurement7.TotalMilliseconds:F3} directDamageWithoutAudioMs={measurement8.TotalMilliseconds:F3} directDamageCachedTransformMs={measurement9.TotalMilliseconds:F3} directDamageCachedTransformFallbackMs={measurement10.TotalMilliseconds:F3} directDamageHurtComponentMs={measurement11.TotalMilliseconds:F3} directDamageHurtComponentCachedTransformMs={measurement12.TotalMilliseconds:F3} directDamagePipelinePreparedMs={measurement13.TotalMilliseconds:F3} directDamagePipelinePreparedCachedTransformMs={measurement14.TotalMilliseconds:F3} directDamageInstanceMs={measurement15.TotalMilliseconds:F3} directDamageInstanceCachedTransformMs={measurement16.TotalMilliseconds:F3} directDamageFlagHurtMs={measurement17.TotalMilliseconds:F3} directDamageDealHurtMs={measurement18.TotalMilliseconds:F3}");
				GD.Print($"[PenetrationRenderWorkload] renderSync={RenderSync} displayServer={name} renderedWhenRequested={flag2}");
				GD.Print($"[PenetrationWorkloadResult] runtimeProfile={RuntimeProfile} bullets={_bulletField.ActiveCount}/{BulletCount} targets={_targets.Count}/{TargetCount} cells={_cellPositions.Count} registryEntriesScanned={num} candidateChecks={num2} candidateCacheBuilds={collisionCandidateCacheBuildCountForTest} candidateCacheHits={collisionCandidateCacheHitCountForTest} overlapCacheBuilds={collisionOverlapCacheBuildCountForTest} overlapCacheHits={collisionOverlapCacheHitCountForTest} expectedOverlaps={num3} firstEntryIds={num4} steadyIds={num5} steadySetFastPaths={penetrationOverlapSetFastPathCountForTest} steadyOrderFastPaths={penetrationRememberedOrderFastPathCountForTest} steadySetCleanups={penetrationOverlapSetCleanupCountForTest} exitIds={num6} multiMeshOnly={flag} passed={flag4}");
				exitCode = ((!flag4) ? 1 : 0);
			}
		}
		catch (Exception value4)
		{
			GD.PrintErr($"[BulletFieldPenetrationWorkloadTest] {value4}");
			exitCode = 2;
		}
		finally
		{
			Cleanup();
			GetTree().Quit(exitCode);
		}
	}

	private bool RunRangePenetrationStress()
	{
		RegisterTargets();
		for (int i = 0; i < StressWarmupWaves; i++)
		{
			if (!PrepareCombatWave())
			{
				return false;
			}
			if (!RunCombatWave().Passed)
			{
				return false;
			}
		}
		_rangeDispatchFrameMilliseconds.Clear();
		int num = Math.Max(1, SampleCount);
		double[] array = new double[num];
		double[] array2 = new double[num];
		double[] array3 = new double[num];
		long num2 = 0L;
		long num3 = 0L;
		long num4 = 0L;
		long num5 = 0L;
		long num6 = 0L;
		long num7 = 0L;
		long num8 = 0L;
		long num9 = 0L;
		int num10 = 0;
		int num11 = 0;
		int num12 = 0;
		int num13 = 0;
		int num14 = 0;
		int num15 = 0;
		int num16 = 0;
		double num17 = 0.0;
		bool flag = true;
		for (int j = 0; j < num; j++)
		{
			if (!PrepareCombatWave())
			{
				return false;
			}
			CombatWaveMeasurement combatWaveMeasurement = RunCombatWave();
			array[j] = combatWaveMeasurement.ImpactMilliseconds;
			array2[j] = combatWaveMeasurement.RangeDispatchMilliseconds;
			array3[j] = combatWaveMeasurement.TotalMilliseconds;
			num2 += combatWaveMeasurement.ExpectedPenetrationDamageCalculations;
			num3 += combatWaveMeasurement.ExpectedRangeDamageCalculations;
			num4 += combatWaveMeasurement.ActualImpactDamageCalculations;
			num5 += combatWaveMeasurement.ActualRangeDamageCalculations;
			num6 += combatWaveMeasurement.ActualDamageCalculations;
			num7 += combatWaveMeasurement.RememberedPenetrationHits;
			num8 += combatWaveMeasurement.ThreadAllocatedBytes;
			num9 += combatWaveMeasurement.TotalAllocatedBytes;
			num10 += combatWaveMeasurement.Gen0Collections;
			num11 += combatWaveMeasurement.Gen1Collections;
			num12 += combatWaveMeasurement.Gen2Collections;
			num13 += combatWaveMeasurement.RangeWorkItems;
			num14 += combatWaveMeasurement.RangeDispatchWorkUnits;
			num16 += combatWaveMeasurement.RangeDispatchFrames;
			num17 = Math.Max(num17, combatWaveMeasurement.RangeDispatchMaxFrameMilliseconds);
			num15 += combatWaveMeasurement.ImpactUpdatePasses;
			flag &= combatWaveMeasurement.Passed;
		}
		Array.Sort(array);
		Array.Sort(array2);
		Array.Sort(array3);
		double[] array4 = _rangeDispatchFrameMilliseconds.ToArray();
		Array.Sort(array4);
		double value = Percentile(array4, 95.0);
		double value2 = Percentile(array4, 99.0);
		double num18 = 0.0;
		for (int k = 0; k < array3.Length; k++)
		{
			num18 += array3[k];
		}
		double value3 = ((num18 > 0.0) ? ((double)num6 * 1000.0 / num18) : 0.0);
		long num19 = num7 + num3;
		double num20 = ((num2 > 0) ? ((double)num7 * 100.0 / (double)num2) : 100.0);
		flag &= num19 == num6 && num20 >= 95.0 && num16 == num && num10 == 0 && num11 == 0 && num12 == 0 && TowerDefenseExplode.GetPendingWorkItemCountForTest() == 0;
		GD.Print($"[RangePenetrationStressBreakdown] runtimeProfile={RuntimeProfile} samples={num} impactP50Ms={Percentile(array, 50.0):F3} impactP95Ms={Percentile(array, 95.0):F3} impactMaxMs={array[^1]:F3} rangeDispatchP50Ms={Percentile(array2, 50.0):F3} rangeDispatchP95Ms={Percentile(array2, 95.0):F3} rangeDispatchMaxMs={array2[^1]:F3} totalP50Ms={Percentile(array3, 50.0):F3} totalP95Ms={Percentile(array3, 95.0):F3} totalMaxMs={array3[^1]:F3}");
		GD.Print($"[RangePenetrationStressResult] runtimeProfile={RuntimeProfile} bullets={BulletCount} penetrationBullets={_penetrationBulletCount} rangeBullets={_rangeBulletCount} targets={TargetCount} cells={_cellPositions.Count} samples={num} expectedPenetrationDamageCalculations={num2} expectedRangeDamageCalculations={num3} actualImpactDamageCalculations={num4} actualRangeDamageCalculations={num5} actualDamageCalculations={num6} rememberedPenetrationHits={num7} penetrationCandidateCoverage={num20:F2} damageCalculationsPerSecond={value3:F2} impactUpdatePasses={num15} rangeWorkItems={num13} rangeDispatchWorkUnits={num14} atomicDispatchFrames={num16} rangeDispatchP95FrameMs={value:F3} rangeDispatchP99FrameMs={value2:F3} rangeDispatchMaxFrameMs={num17:F3} threadAllocatedBytes={num8} totalAllocatedBytes={num9} gen0={num10} gen1={num11} gen2={num12} pendingRangeWorkItems={TowerDefenseExplode.GetPendingWorkItemCountForTest()} passed={flag}");
		return flag;
	}

	private bool PrepareCombatWave()
	{
		if (TowerDefenseExplode.GetPendingWorkItemCountForTest() != 0)
		{
			TowerDefenseExplode.DrainPendingWorkForTest();
		}
		_bulletField.ClearActiveBullets();
		for (int i = 0; i < _targets.Count; i++)
		{
			TowerDefenseCharacterInstance instance = _targets[i].instance;
			instance.hitpointsBase = 1000000000.0;
			instance.hitpointsSave = 1000000000.0;
			instance.hitpoints = 1000000000.0;
			instance.hitpointsNearDeath = 0.0;
		}
		return SpawnBullets();
	}

	private CombatWaveMeasurement RunCombatWave()
	{
		bool flag = AllBulletsUseMultiMesh();
		CountExpectedCombatDamageCalculations(out var penetrationDamageCalculations, out var rangeDamageCalculations);
		double totalTargetHitpoints = GetTotalTargetHitpoints();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		long timestamp = Stopwatch.GetTimestamp();
		int num4 = 0;
		do
		{
			_bulletField.Update(0.0, _measurementFrame++);
			num4++;
		}
		while (_bulletField.ActiveCount > _penetrationBulletCount && num4 < 32);
		double impactMilliseconds = ElapsedMilliseconds(timestamp);
		double totalTargetHitpoints2 = GetTotalTargetHitpoints();
		long num5 = CountRememberedPenetrationTargets();
		int pendingWorkItemCountForTest = TowerDefenseExplode.GetPendingWorkItemCountForTest();
		long timestamp2 = Stopwatch.GetTimestamp();
		int num6 = TowerDefenseExplode.DrainPendingWorkForTest();
		double num7 = ElapsedMilliseconds(timestamp2);
		int num8 = ((pendingWorkItemCountForTest > 0) ? 1 : 0);
		double rangeDispatchMaxFrameMilliseconds = num7;
		if (num8 > 0)
		{
			_rangeDispatchFrameMilliseconds.Add(num7);
		}
		long threadAllocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		long totalAllocatedBytes2 = GC.GetTotalAllocatedBytes() - totalAllocatedBytes;
		int gen0Collections = GC.CollectionCount(0) - num;
		int gen1Collections = GC.CollectionCount(1) - num2;
		int gen2Collections = GC.CollectionCount(2) - num3;
		double totalTargetHitpoints3 = GetTotalTargetHitpoints();
		long actualImpactDamageCalculations = (long)Math.Round(totalTargetHitpoints - totalTargetHitpoints2);
		long actualRangeDamageCalculations = (long)Math.Round(totalTargetHitpoints2 - totalTargetHitpoints3);
		double num9 = totalTargetHitpoints - totalTargetHitpoints3;
		long actualDamageCalculations = (long)Math.Round(num9);
		long num10 = num5 + rangeDamageCalculations;
		double num11 = ((penetrationDamageCalculations > 0) ? ((double)num5 * 100.0 / (double)penetrationDamageCalculations) : 100.0);
		return new CombatWaveMeasurement(flag && pendingWorkItemCountForTest == _rangeBulletCount && _bulletField.ActiveCount == _penetrationBulletCount && num6 >= pendingWorkItemCountForTest + rangeDamageCalculations && num11 >= 95.0 && Math.Abs(num9 - (double)num10) <= 0.001 && TowerDefenseExplode.GetPendingWorkItemCountForTest() == 0, impactMilliseconds, num7, penetrationDamageCalculations, rangeDamageCalculations, actualImpactDamageCalculations, actualRangeDamageCalculations, actualDamageCalculations, num5, num4, pendingWorkItemCountForTest, num6, num8, rangeDispatchMaxFrameMilliseconds, _bulletField.ActiveCount, threadAllocatedBytes, totalAllocatedBytes2, gen0Collections, gen1Collections, gen2Collections);
	}

	private double GetTotalTargetHitpoints()
	{
		double num = 0.0;
		for (int i = 0; i < _targets.Count; i++)
		{
			num += _targets[i].instance.hitpoints;
		}
		return num;
	}

	private void CountExpectedCombatDamageCalculations(out long penetrationDamageCalculations, out long rangeDamageCalculations)
	{
		penetrationDamageCalculations = 0L;
		rangeDamageCalculations = 0L;
		Vector2 size = _manager.GetMapGridSize() * 2f * _rangeProjectileConfig.rangeSize;
		Vector2 vector = _projectileConfig.size * 0.5f;
		for (int i = 0; i < _cellPositions.Count; i++)
		{
			_manager.characterRegistry.FillCharactersIntersectingRectList(new Rect2(_cellPositions[i] - vector, vector * 2f), _candidateProbe, _cellGridPositions[i].Y);
			penetrationDamageCalculations += (long)_penetrationBulletsPerCell[i] * (long)_candidateProbe.Count;
			Vector2I mapGridPos = _manager.GetMapGridPos(_cellPositions[i]);
			Vector2 mapCellPosCenter = _manager.GetMapCellPosCenter(mapGridPos);
			mapCellPosCenter.X = _cellPositions[i].X;
			_manager.characterRegistry.FillCharactersIntersectingRectList(AabbShapeUtil.RectFromCenter(mapCellPosCenter, size), _candidateProbe);
			rangeDamageCalculations += (long)_rangeBulletsPerCell[i] * (long)_candidateProbe.Count;
		}
	}

	private bool RunGeometryInvalidationProbe()
	{
		RegisterTargets();
		TowerDefenseCharacter target = _targets[0];
		target.ProcessMode = ProcessModeEnum.Inherit;
		double hitpoints = target.instance.hitpoints;
		int hurtSignals = 0;
		target.OnBodyHurt += MoveTargetOnFirstHit;
		_bulletField.Update(0.0, _measurementFrame++);
		target.OnBodyHurt -= MoveTargetOnFirstHit;
		double num = hitpoints - target.instance.hitpoints;
		bool flag = hurtSignals == 1 && Mathf.IsEqualApprox(num, 1.0) && target.GlobalPosition.Y > _targetPositions[0].Y + 1000f && AllBulletsUseMultiMesh();
		GD.Print($"[PenetrationGeometryInvalidationResult] hurtSignals={hurtSignals} damageTaken={num:F1} geometryRevision={_manager.characterRegistry.GeometryRevision} multiMeshOnly={AllBulletsUseMultiMesh()} passed={flag}");
		return flag;
		void MoveTargetOnFirstHit(int damage)
		{
			if (hurtSignals++ == 0)
			{
				target.GlobalPosition += new Vector2(0f, 10000f);
			}
		}
	}

	private bool RunCrossLineVelocityProbe()
	{
		RegisterTargets();
		TowerDefenseCharacter towerDefenseCharacter = _targets[0];
		int bulletIndex = _bulletIndices[0];
		Vector2I gridPos = new Vector2I(2, 1);
		Vector2I gridPos2 = new Vector2I(4, 3);
		Vector2 vector = new Vector2(120f, 38f);
		Vector2 vector2 = new Vector2(280f, 190f);
		towerDefenseCharacter.gridPos = gridPos2;
		towerDefenseCharacter.GlobalPosition = vector2;
		towerDefenseCharacter.InvalidateHitBoxBounds();
		double hitpoints = towerDefenseCharacter.instance.hitpoints;
		int hurtSignals = 0;
		int hitGridY = -2147483648;
		ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(bulletIndex);
		bulletDataRef.pos = new Vector2(vector.X, vector2.Y);
		bulletDataRef.vel = new Vector2(vector2.X - vector.X, 0f);
		bulletDataRef.speed = bulletDataRef.vel.Length();
		bulletDataRef.gridY = gridPos.Y;
		bulletDataRef.gridPos = gridPos;
		bulletDataRef.lockGridY = true;
		bulletDataRef.randFreshIndex = 0;
		towerDefenseCharacter.OnBodyHurt += CaptureHitLine;
		for (int i = 0; i < 90; i++)
		{
			_bulletField.Update(1.0 / 60.0, _measurementFrame++);
		}
		bool flag = hurtSignals == 0 && bulletDataRef.gridY == gridPos.Y;
		bulletDataRef.pos = vector;
		bulletDataRef.vel = vector2 - vector;
		bulletDataRef.speed = bulletDataRef.vel.Length();
		bulletDataRef.gridY = gridPos.Y;
		bulletDataRef.gridPos = gridPos;
		bulletDataRef.lockGridY = true;
		bulletDataRef.randFreshIndex = 0;
		for (int j = 0; j < 90; j++)
		{
			if (hurtSignals != 0)
			{
				break;
			}
			_bulletField.Update(1.0 / 60.0, _measurementFrame++);
		}
		towerDefenseCharacter.OnBodyHurt -= CaptureHitLine;
		double num = hitpoints - towerDefenseCharacter.instance.hitpoints;
		bool flag2 = AllBulletsUseMultiMesh();
		bool flag3 = flag && hurtSignals == 1 && Mathf.IsEqualApprox(num, 1.0) && hitGridY == gridPos2.Y && flag2;
		GD.Print($"[CrossLineVelocityResult] sourceGridY={gridPos.Y} targetGridY={gridPos2.Y} lockedControl={flag} hitGridY={hitGridY} hurtSignals={hurtSignals} damageTaken={num:F1} multiMeshOnly={flag2} passed={flag3}");
		return flag3;
		void CaptureHitLine(int damage)
		{
			hurtSignals++;
			if (_bulletField.IsBulletActive(bulletIndex))
			{
				hitGridY = _bulletField.GetBulletDataRef(bulletIndex).gridY;
			}
		}
	}

	private bool RunSweptCollisionProbe()
	{
		RegisterTargets();
		TowerDefenseCharacter towerDefenseCharacter = _targets[0];
		int index = _bulletIndices[0];
		if (!towerDefenseCharacter.TryGetActiveWorldHitRect(out var rect))
		{
			GD.PrintErr("[SweptCollisionResult] target hit box is unavailable.");
			return false;
		}
		towerDefenseCharacter.gridPos = new Vector2I(towerDefenseCharacter.gridPos.X + 6, towerDefenseCharacter.gridPos.Y);
		ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(index);
		Vector2 collisionHalfSize = BulletField.GetCollisionHalfSize(ref bulletDataRef);
		float num = ((towerDefenseCharacter.Scale.X >= 0f) ? 1f : (-1f));
		Vector2 vector = new Vector2((num > 0f) ? (rect.Position.X - collisionHalfSize.X - 40f) : (rect.End.X + collisionHalfSize.X + 40f), rect.GetCenter().Y);
		Vector2 vector2 = new Vector2((num > 0f) ? (rect.End.X + collisionHalfSize.X + 40f) : (rect.Position.X - collisionHalfSize.X - 40f), rect.GetCenter().Y);
		Vector2 vector3 = new Vector2(0f, (float)bulletDataRef.height);
		bulletDataRef.pos = vector - vector3;
		bulletDataRef.vel = vector2 - vector;
		bulletDataRef.speed = bulletDataRef.vel.Length();
		bulletDataRef.gridY = towerDefenseCharacter.gridPos.Y;
		bulletDataRef.gridPos = towerDefenseCharacter.gridPos;
		bulletDataRef.lockGridY = true;
		bulletDataRef.randFreshIndex = 0;
		Rect2 rect2 = new Rect2(vector - collisionHalfSize, collisionHalfSize * 2f);
		Rect2 rect3 = new Rect2(vector2 - collisionHalfSize, collisionHalfSize * 2f);
		bool flag = !rect2.Intersects(rect);
		bool flag2 = !rect3.Intersects(rect);
		double hitpoints = towerDefenseCharacter.instance.hitpoints;
		int hurtSignals = 0;
		towerDefenseCharacter.OnBodyHurt += CaptureHit;
		_bulletField.Update(1.0, _measurementFrame++);
		towerDefenseCharacter.OnBodyHurt -= CaptureHit;
		double num2 = hitpoints - towerDefenseCharacter.instance.hitpoints;
		bool flag3 = AllBulletsUseMultiMesh();
		bool flag4 = flag && flag2 && hurtSignals == 1 && Mathf.IsEqualApprox(num2, 1.0) && flag3;
		GD.Print($"[SweptCollisionResult] startOutside={flag} endOutside={flag2} targetGridX={towerDefenseCharacter.gridPos.X} targetWorldX={rect.GetCenter().X:F1} hurtSignals={hurtSignals} damageTaken={num2:F1} multiMeshOnly={flag3} passed={flag4}");
		return flag4;
		void CaptureHit(int damage)
		{
			hurtSignals++;
		}
	}

	private bool RunTrackSweptCollisionProbe()
	{
		RegisterTargets();
		TowerDefenseCharacter towerDefenseCharacter = _targets[0];
		int index = _bulletIndices[0];
		Vector2 cachedWorldPositionForProjectile = towerDefenseCharacter.GetCachedWorldPositionForProjectile();
		Vector2 vector = cachedWorldPositionForProjectile - new Vector2(160f, 0f);
		Vector2 vector2 = cachedWorldPositionForProjectile + new Vector2(160f, 0f);
		ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(index);
		bulletDataRef.pos = vector;
		bulletDataRef.vel = vector2 - vector;
		bulletDataRef.speed = bulletDataRef.vel.Length();
		bulletDataRef.target = towerDefenseCharacter;
		bulletDataRef.trackOpen = true;
		bulletDataRef.fireMethodFlags = 32;
		bulletDataRef.gridY = towerDefenseCharacter.gridPos.Y;
		bulletDataRef.gridPos = towerDefenseCharacter.gridPos;
		bulletDataRef.lockGridY = true;
		_projectileConfig.fireMethodFlags = bulletDataRef.fireMethodFlags;
		bool flag = vector.DistanceSquaredTo(cachedWorldPositionForProjectile) > 900f;
		bool flag2 = vector2.DistanceSquaredTo(cachedWorldPositionForProjectile) > 900f;
		bool flag3 = AllBulletsUseMultiMesh();
		double hitpoints = towerDefenseCharacter.instance.hitpoints;
		int hurtSignals = 0;
		towerDefenseCharacter.OnBodyHurt += CaptureHit;
		_bulletField.Update(1.0, _measurementFrame++);
		towerDefenseCharacter.OnBodyHurt -= CaptureHit;
		double num = hitpoints - towerDefenseCharacter.instance.hitpoints;
		bool flag4 = flag && flag2 && hurtSignals == 1 && Mathf.IsEqualApprox(num, 1.0) && flag3;
		GD.Print($"[TrackSweptCollisionResult] startOutside={flag} endOutside={flag2} hurtSignals={hurtSignals} damageTaken={num:F1} multiMeshOnly={flag3} passed={flag4}");
		return flag4;
		void CaptureHit(int damage)
		{
			hurtSignals++;
		}
	}

	private bool PrepareWorkload()
	{
		_manager = TowerDefenseManager.Instance;
		_projectileUpdateManager = ProjectileUpdateManager.Instance;
		if (!GodotObject.IsInstanceValid(_manager?.characterRegistry) || !GodotObject.IsInstanceValid(_projectileUpdateManager))
		{
			GD.PrintErr("[BulletFieldPenetrationWorkloadTest] Required autoloads are unavailable.");
			return false;
		}
		_originalProjectileManagerProcessMode = _projectileUpdateManager.ProcessMode;
		_projectileUpdateManager.ProcessMode = ProcessModeEnum.Disabled;
		_originalGridSize = _manager.gridSize;
		_originalGridBegin = _manager.gridBeginPos;
		_originalGridNum = _manager.gridNum;
		_manager.gridBeginPos = Vector2.Zero;
		_manager.gridSize = new Vector2(80f, 76f);
		_manager.gridNum = new Vector2I(Math.Max(16, ColumnCount + 4), Math.Max(8, RowCount + 2));
		BuildCells();
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		PackedScene packedScene2 = GD.Load<PackedScene>("res://Asset/Config/Projectile/Pea/Sprite/FirePea/FirePea.tscn");
		if (packedScene == null || packedScene2 == null)
		{
			GD.PrintErr("[BulletFieldPenetrationWorkloadTest] Failed to load workload scenes.");
			return false;
		}
		_bulletField = new BulletField
		{
			Name = "PenetrationBulletField"
		};
		AddChild(_bulletField, forceReadableName: false, InternalMode.Disabled);
		_projectileConfig = new TowerDefenseProjectileConfig
		{
			name = "PenetrationWorkload",
			projectileScene = packedScene2,
			scale = Vector2.One * BulletScale,
			size = new Vector2(28f, 28f),
			baseDamage = 1.0,
			damageFlags = 2,
			collisionFlags = 1,
			fireMethodFlags = 5,
			penetrateNum = -1,
			splatAudio = string.Empty
		};
		_rangeProjectileConfig = new TowerDefenseProjectileConfig
		{
			name = "RangeWorkload",
			projectileScene = packedScene2,
			scale = Vector2.One * BulletScale,
			size = new Vector2(28f, 28f),
			baseDamage = 1.0,
			damageFlags = 2,
			collisionFlags = 1,
			fireMethodFlags = 1,
			useRange = true,
			rangeSize = new Vector2(0.45f, 0.45f),
			hitPesontage = 1.0,
			splatAudio = string.Empty
		};
		_forcedFallbackProjectileConfig = new TowerDefenseProjectileConfig
		{
			name = "PenetrationWorkloadForcedFallback",
			baseDamage = 1.0,
			damageFlags = 2,
			collisionFlags = 1,
			fireMethodFlags = _projectileConfig.fireMethodFlags,
			penetrateNum = -1,
			rangeType = "ForcedFallback"
		};
		if (!CreateTargets(packedScene) || !SpawnBullets())
		{
			return false;
		}
		_measurementFrame = Engine.GetPhysicsFrames() + 1;
		return true;
	}

	private void BuildCells()
	{
		int num = Math.Max(1, ColumnCount);
		int num2 = Math.Max(1, RowCount);
		int num3 = num * num2;
		for (int i = 0; i < num3; i++)
		{
			int num4 = 2 + i % num;
			int num5 = 1 + i / num;
			Vector2I item = new Vector2I(num4, num5);
			_cellGridPositions.Add(item);
			_cellPositions.Add(new Vector2(((float)num4 - 0.5f) * 80f, ((float)num5 - 0.5f) * 76f));
			_bulletsPerCell.Add(0);
			_penetrationBulletsPerCell.Add(0);
			_rangeBulletsPerCell.Add(0);
			_targetsPerCell.Add(0);
		}
	}

	private bool CreateTargets(PackedScene zombieScene)
	{
		for (int i = 0; i < TargetCount; i++)
		{
			if (!(zombieScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter))
			{
				GD.PrintErr($"[BulletFieldPenetrationWorkloadTest] Target {i} failed to instantiate.");
				return false;
			}
			int index = i % _cellPositions.Count;
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.gridPos = _cellGridPositions[index];
			towerDefenseCharacter.GlobalPosition = _cellPositions[index];
			AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			towerDefenseCharacter.ProcessMode = (ProcessModeEnum)(RangePenetrationStress ? 0 : 4);
			if (RangePenetrationStress)
			{
				towerDefenseCharacter.SetProcess(enable: false);
				towerDefenseCharacter.SetPhysicsProcess(enable: false);
			}
			towerDefenseCharacter.Visible = false;
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter.instance) || towerDefenseCharacter.hurtComponent == null || towerDefenseCharacter.hurtComponent.IsReleased || towerDefenseCharacter.targetRegistrationComponent == null)
			{
				GD.PrintErr($"[BulletFieldPenetrationWorkloadTest] Target {i} is not runtime-ready.");
				return false;
			}
			towerDefenseCharacter.camp = TowerDefenseEnum.CHARACTER_CAMP.ZOMBIE;
			towerDefenseCharacter.targetRegistrationComponent.canProjectileCheck = true;
			towerDefenseCharacter.hurtComponent.flashOnDamage = false;
			towerDefenseCharacter.hurtComponent.markHealthBarDirty = false;
			towerDefenseCharacter.instance.keepAlive = true;
			towerDefenseCharacter.instance.hitpointsNearDeath = 0.0;
			towerDefenseCharacter.instance.hitpointsBase = 1000000000.0;
			towerDefenseCharacter.instance.hitpointsSave = 1000000000.0;
			towerDefenseCharacter.instance.hitpoints = 1000000000.0;
			_targets.Add(towerDefenseCharacter);
			_targetPositions.Add(_cellPositions[index]);
			_targetsPerCell[index]++;
		}
		return true;
	}

	private bool SpawnBullets()
	{
		_bulletIndices.Clear();
		_penetrationBulletCount = 0;
		_rangeBulletCount = 0;
		for (int i = 0; i < _cellPositions.Count; i++)
		{
			_bulletsPerCell[i] = 0;
			_penetrationBulletsPerCell[i] = 0;
			_rangeBulletsPerCell[i] = 0;
		}
		Rect2 rect = new Rect2(-1000000f, -1000000f, 2000000f, 2000000f);
		for (int j = 0; j < BulletCount; j++)
		{
			int index = j % _cellPositions.Count;
			bool flag = RangePenetrationStress && j % 100 < RangeBulletPercentage;
			TowerDefenseProjectileConfig config = (flag ? _rangeProjectileConfig : _projectileConfig);
			int num = _bulletField.TrySpawnFromConfig(config, _cellPositions[index], Vector2.Zero, 0.0, null, TowerDefenseEnum.CHARACTER_CAMP.PLANT, _cellGridPositions[index], _cellGridPositions[index].Y, rect, null, 0.0, 0.0, 1);
			if (num < 0)
			{
				GD.PrintErr($"[BulletFieldPenetrationWorkloadTest] Bullet {j} failed to spawn.");
				return false;
			}
			ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(num);
			bulletDataRef.lockGridY = true;
			bulletDataRef.randFreshIndex = j % 5;
			if (bulletDataRef.renderMode != BulletRenderMode.ANIMATED_MESH || bulletDataRef.animDefId < 0)
			{
				GD.PrintErr($"[BulletFieldPenetrationWorkloadTest] Bullet {j} is not using animated MultiMesh.");
				return false;
			}
			_bulletIndices.Add(num);
			_bulletsPerCell[index]++;
			if (flag)
			{
				_rangeBulletCount++;
				_rangeBulletsPerCell[index]++;
			}
			else
			{
				_penetrationBulletCount++;
				_penetrationBulletsPerCell[index]++;
			}
		}
		return _bulletField.ActiveCount == BulletCount;
	}

	private void RegisterTargets()
	{
		if (!_targetsRegistered)
		{
			for (int i = 0; i < _targets.Count; i++)
			{
				_manager.CharacterRegister(_targets[i]);
			}
			_targetsRegistered = true;
		}
	}

	private void MoveTargets(bool overlapping)
	{
		Vector2 vector = new Vector2(0f, 10000f);
		for (int i = 0; i < _targets.Count; i++)
		{
			_targets[i].GlobalPosition = _targetPositions[i] + (overlapping ? Vector2.Zero : vector);
			_targets[i].InvalidateHitBoxBounds();
		}
	}

	private void RunUntimedUpdates(int count)
	{
		for (int i = 0; i < count; i++)
		{
			_bulletField.Update(0.0, _measurementFrame++);
			FlushRenderedFrame();
		}
	}

	private Measurement MeasureUpdates(string name, int samples)
	{
		samples = Math.Max(1, samples);
		double[] array = new double[samples];
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		long timestamp = Stopwatch.GetTimestamp();
		for (int i = 0; i < samples; i++)
		{
			long timestamp2 = Stopwatch.GetTimestamp();
			_bulletField.Update(0.0, _measurementFrame++);
			FlushRenderedFrame();
			array[i] = ElapsedMilliseconds(timestamp2);
		}
		double totalMilliseconds = ElapsedMilliseconds(timestamp);
		Array.Sort(array);
		return new Measurement(name, samples, totalMilliseconds, Percentile(array, 50.0), array[^1], GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread, GC.GetTotalAllocatedBytes() - totalAllocatedBytes, GC.CollectionCount(0) - num, GC.CollectionCount(1) - num2, GC.CollectionCount(2) - num3);
	}

	private void FlushRenderedFrame()
	{
		if (RenderSync)
		{
			RenderingServer.ForceDraw();
		}
	}

	private Measurement MeasureDirectDamage(long calls, bool playSplatAudio, DirectDamagePath path = DirectDamagePath.Character)
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		long timestamp = Stopwatch.GetTimestamp();
		for (long num4 = 0L; num4 < calls; num4++)
		{
			TowerDefenseCharacter towerDefenseCharacter = _targets[(int)(num4 % _targets.Count)];
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = ((path == DirectDamagePath.CharacterCachedTransformFallback) ? _forcedFallbackProjectileConfig : _projectileConfig);
			ProjectileHitInfo info = new ProjectileHitInfo
			{
				damage = 1.0,
				damageFlags = 2,
				position = towerDefenseCharacter.GlobalPosition,
				projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL,
				config = towerDefenseProjectileConfig,
				fireCharacter = null,
				camp = TowerDefenseEnum.CHARACTER_CAMP.PLANT,
				collisionFlags = 1,
				gridPos = towerDefenseCharacter.gridPos,
				height = 0.0,
				onSourceDespawn = NoopDespawn
			};
			if ((path == DirectDamagePath.CharacterCachedTransform || path == DirectDamagePath.CharacterCachedTransformFallback || path == DirectDamagePath.HurtComponentCachedTransform || path == DirectDamagePath.PipelinePreparedCachedTransform || path == DirectDamagePath.InstanceCachedTransform) && towerDefenseCharacter.TryGetCachedHitTransform(out var originX, out var scaleX))
			{
				info.hasTargetTransform = true;
				info.targetOriginX = originX;
				info.targetScaleX = scaleX;
			}
			switch (path)
			{
			case DirectDamagePath.HurtComponent:
			case DirectDamagePath.HurtComponentCachedTransform:
				towerDefenseCharacter.hurtComponent.ProjectileHurt(in info, _projectileConfig, playSplatAudio);
				break;
			case DirectDamagePath.CharacterCachedTransform:
			case DirectDamagePath.CharacterCachedTransformFallback:
				towerDefenseCharacter.ProjectileHurt(in info, towerDefenseProjectileConfig, playSplatAudio);
				break;
			case DirectDamagePath.PipelinePrepared:
			case DirectDamagePath.PipelinePreparedCachedTransform:
				_manager.damagePipeline.ApplyPreparedProjectileHurt(towerDefenseCharacter, towerDefenseCharacter.instance, in info, _projectileConfig, playSplatAudio);
				break;
			case DirectDamagePath.Instance:
			case DirectDamagePath.InstanceCachedTransform:
				towerDefenseCharacter.instance.ProjectileHurt(in info, _projectileConfig, playSplatAudio);
				break;
			case DirectDamagePath.FlagHurt:
				towerDefenseCharacter.instance.FlagHurt(info.damage, info.damageFlags, playSplatAudio);
				break;
			case DirectDamagePath.DealHurt:
				towerDefenseCharacter.instance.DealHurt(info.damage, playSplatAudio);
				break;
			default:
				towerDefenseCharacter.ProjectileHurt(in info, _projectileConfig, playSplatAudio);
				break;
			}
		}
		double num5 = ElapsedMilliseconds(timestamp);
		return new Measurement((path != DirectDamagePath.Character) ? ("direct-damage-" + path.ToString().ToLowerInvariant()) : (playSplatAudio ? "direct-damage" : "direct-damage-no-audio"), 1, num5, num5, num5, GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread, GC.GetTotalAllocatedBytes() - totalAllocatedBytes, GC.CollectionCount(0) - num, GC.CollectionCount(1) - num2, GC.CollectionCount(2) - num3);
	}

	private Measurement MeasureInstanceIdLookup(long calls)
	{
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		long totalAllocatedBytes = GC.GetTotalAllocatedBytes();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		ulong num4 = 0uL;
		long timestamp = Stopwatch.GetTimestamp();
		for (long num5 = 0L; num5 < calls; num5++)
		{
			num4 ^= _targets[(int)(num5 % _targets.Count)].GetInstanceId();
		}
		double num6 = ElapsedMilliseconds(timestamp);
		_instanceIdChecksum = num4;
		return new Measurement("instance-id-lookup", 1, num6, num6, num6, GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread, GC.GetTotalAllocatedBytes() - totalAllocatedBytes, GC.CollectionCount(0) - num, GC.CollectionCount(1) - num2, GC.CollectionCount(2) - num3);
	}

	private long CountCandidateChecksPerUpdate()
	{
		long num = 0L;
		Vector2 defaultProjectileHitHalfSize = BulletField.DefaultProjectileHitHalfSize;
		for (int i = 0; i < _cellPositions.Count; i++)
		{
			Rect2 checkRect = new Rect2(_cellPositions[i] - defaultProjectileHitHalfSize, defaultProjectileHitHalfSize * 2f);
			_manager.characterRegistry.FillCharactersForRectGridWindowList(checkRect, _candidateProbe, _cellGridPositions[i].Y, includeAllLineCheck: false, clampToMapPadding: false);
			num += (long)_candidateProbe.Count * (long)_bulletsPerCell[i];
		}
		return num;
	}

	private long CountRegistryEntriesScannedPerUpdate()
	{
		long num = 0L;
		Vector2 defaultProjectileHitHalfSize = BulletField.DefaultProjectileHitHalfSize;
		Vector2 gridBeginPos = _manager.gridBeginPos;
		Vector2 gridSize = _manager.gridSize;
		for (int i = 0; i < _cellPositions.Count; i++)
		{
			float num2 = _cellPositions[i].X - defaultProjectileHitHalfSize.X;
			float num3 = _cellPositions[i].X + defaultProjectileHitHalfSize.X;
			int num4 = Mathf.FloorToInt((num2 - gridBeginPos.X) / gridSize.X);
			int num5 = Mathf.FloorToInt((num3 - gridBeginPos.X) / gridSize.X);
			long num6 = 0L;
			for (int j = num4; j <= num5; j++)
			{
				num6 += _manager.characterRegistry.GetColumnCharactersList(j).Count;
			}
			num += num6 * _bulletsPerCell[i];
		}
		return num;
	}

	private long CountExpectedOverlaps()
	{
		long num = 0L;
		for (int i = 0; i < _cellPositions.Count; i++)
		{
			num += (long)_bulletsPerCell[i] * (long)_targetsPerCell[i];
		}
		return num;
	}

	private long CountRememberedPenetrationTargets()
	{
		long num = 0L;
		for (int i = 0; i < _bulletIndices.Count; i++)
		{
			num += _bulletField.GetRememberedPenetrationTargetCountForTest(_bulletIndices[i]);
		}
		return num;
	}

	private bool AllBulletsUseMultiMesh()
	{
		for (int i = 0; i < _bulletIndices.Count; i++)
		{
			ref BulletData bulletDataRef = ref _bulletField.GetBulletDataRef(_bulletIndices[i]);
			if (!bulletDataRef.active || bulletDataRef.renderMode != BulletRenderMode.ANIMATED_MESH || bulletDataRef.animDefId < 0)
			{
				return false;
			}
		}
		return true;
	}

	private static void PrintMeasurement(Measurement measurement)
	{
		GD.Print($"[PenetrationWorkload] phase={measurement.Name} samples={measurement.Samples} totalMs={measurement.TotalMilliseconds:F3} avgMs={measurement.AverageMilliseconds:F3} p50Ms={measurement.MedianMilliseconds:F3} maxMs={measurement.MaximumMilliseconds:F3} threadAllocatedBytes={measurement.ThreadAllocatedBytes} totalAllocatedBytes={measurement.TotalAllocatedBytes} gen0={measurement.Gen0Collections} gen1={measurement.Gen1Collections} gen2={measurement.Gen2Collections}");
	}

	private static double ElapsedMilliseconds(long startTimestamp)
	{
		return (double)(Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static double Percentile(double[] sortedValues, double percentile)
	{
		if (sortedValues.Length == 0)
		{
			return 0.0;
		}
		double num = (double)(sortedValues.Length - 1) * percentile / 100.0;
		int num2 = (int)Math.Floor(num);
		int num3 = (int)Math.Ceiling(num);
		if (num2 == num3)
		{
			return sortedValues[num2];
		}
		double num4 = num - (double)num2;
		return sortedValues[num2] * (1.0 - num4) + sortedValues[num3] * num4;
	}

	private void Cleanup()
	{
		if (_targetsRegistered && GodotObject.IsInstanceValid(_manager?.characterRegistry))
		{
			for (int i = 0; i < _targets.Count; i++)
			{
				if (GodotObject.IsInstanceValid(_targets[i]))
				{
					_manager.CharacterUnregister(_targets[i]);
				}
			}
		}
		_targetsRegistered = false;
		if (GodotObject.IsInstanceValid(_bulletField))
		{
			_bulletField.ClearActiveBullets();
		}
		if (GodotObject.IsInstanceValid(_projectileUpdateManager))
		{
			_projectileUpdateManager.ProcessMode = _originalProjectileManagerProcessMode;
		}
		if (GodotObject.IsInstanceValid(_manager))
		{
			_manager.gridSize = _originalGridSize;
			_manager.gridBeginPos = _originalGridBegin;
			_manager.gridNum = _originalGridNum;
		}
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string text in cmdlineUserArgs)
		{
			int value;
			int value2;
			int value3;
			int value4;
			int value5;
			int value6;
			int value7;
			float value8;
			string value9;
			if (text.Equals("--penetration-geometry-invalidation-probe", StringComparison.OrdinalIgnoreCase))
			{
				GeometryInvalidationProbe = true;
				BulletCount = 2;
				TargetCount = 1;
				ColumnCount = 1;
				RowCount = 1;
			}
			else if (text.Equals("--cross-line-velocity-probe", StringComparison.OrdinalIgnoreCase))
			{
				CrossLineVelocityProbe = true;
				BulletCount = 1;
				TargetCount = 1;
				ColumnCount = 1;
				RowCount = 1;
			}
			else if (text.Equals("--swept-collision-probe", StringComparison.OrdinalIgnoreCase))
			{
				SweptCollisionProbe = true;
				BulletCount = 1;
				TargetCount = 1;
				ColumnCount = 1;
				RowCount = 1;
			}
			else if (text.Equals("--track-swept-collision-probe", StringComparison.OrdinalIgnoreCase))
			{
				TrackSweptCollisionProbe = true;
				BulletCount = 1;
				TargetCount = 1;
				ColumnCount = 1;
				RowCount = 1;
			}
			else if (text.Equals("--penetration-render-sync", StringComparison.OrdinalIgnoreCase))
			{
				RenderSync = true;
			}
			else if (text.Equals("--range-penetration-stress", StringComparison.OrdinalIgnoreCase))
			{
				RangePenetrationStress = true;
			}
			else if (TryReadInt(text, "--range-bullet-percentage=", out value))
			{
				RangeBulletPercentage = Math.Clamp(value, 1, 99);
			}
			else if (TryReadInt(text, "--combat-stress-warmup-waves=", out value2))
			{
				StressWarmupWaves = Math.Max(0, value2);
			}
			else if (TryReadInt(text, "--penetration-bullets=", out value3))
			{
				BulletCount = Math.Clamp(value3, 1, 65536);
			}
			else if (TryReadInt(text, "--penetration-targets=", out value4))
			{
				TargetCount = Math.Max(1, value4);
			}
			else if (TryReadInt(text, "--penetration-columns=", out value5))
			{
				ColumnCount = Math.Max(1, value5);
			}
			else if (TryReadInt(text, "--penetration-rows=", out value6))
			{
				RowCount = Math.Max(1, value6);
			}
			else if (TryReadInt(text, "--penetration-samples=", out value7))
			{
				SampleCount = Math.Max(1, value7);
			}
			else if (TryReadFloat(text, "--penetration-bullet-scale=", out value8))
			{
				BulletScale = Math.Max(0.001f, value8);
			}
			else if (TryReadString(text, "--runtime-profile=", out value9))
			{
				RuntimeProfile = value9;
			}
		}
	}

	private static bool TryReadInt(string arg, string prefix, out int value)
	{
		value = 0;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(arg.Substring(length, arg.Length - length), NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}

	private static bool TryReadFloat(string arg, string prefix, out float value)
	{
		value = 0f;
		if (arg.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return float.TryParse(arg.Substring(length, arg.Length - length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
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
