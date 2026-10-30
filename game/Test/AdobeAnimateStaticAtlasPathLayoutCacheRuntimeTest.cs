using System;
using System.Diagnostics;
using Godot;
using Godot.Collections;

public sealed partial class AdobeAnimateStaticAtlasPathLayoutCacheRuntimeTest : Node
{
	private readonly struct FirstApplyDiagnosticResult(double meanMilliseconds, double p50Milliseconds, double p95Milliseconds, double p99Milliseconds, double maxMilliseconds, long allocatedBytes, int gen0, int gen1, int gen2, bool stable)
	{
		public double MeanMilliseconds { get; } = meanMilliseconds;

		public double P50Milliseconds { get; } = p50Milliseconds;

		public double P95Milliseconds { get; } = p95Milliseconds;

		public double P99Milliseconds { get; } = p99Milliseconds;

		public double MaxMilliseconds { get; } = maxMilliseconds;

		public long AllocatedBytes { get; } = allocatedBytes;

		public int Gen0 { get; } = gen0;

		public int Gen1 { get; } = gen1;

		public int Gen2 { get; } = gen2;

		public bool Stable { get; } = stable;
	}

	private const int InstanceCount = 1000;

	private const int WarmupSampleCount = 240;

	private const int SampleCount = 1200;

	private const int ReplacementMediaId = 6;

	private const string ReplacementMediaName = "Zombie_sleep_0010_23.png";

	private const string DataPath = "res://Asset/Anime/Character/Zombie/Chapter1/Sleeper/ZombieSleeper.tres";

	private const string ReplacementPath = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/SleepHat/ZombieSleepHat1.png";

	private readonly AdobeAnimateSprite[] _owners = new AdobeAnimateSprite[1000];

	private AdobeAnimateData _data;

	private long _hitSink;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		AdobeAnimateSprite adobeAnimateSprite = null;
		AdobeAnimateSprite adobeAnimateSprite2 = null;
		AdobeAnimateSprite adobeAnimateSprite3 = null;
		Node node = null;
		AdobeAnimateSprite adobeAnimateSprite4 = null;
		try
		{
			_data = GD.Load<AdobeAnimateData>("res://Asset/Anime/Character/Zombie/Chapter1/Sleeper/ZombieSleeper.tres");
			if (!GodotObject.IsInstanceValid(_data) || _data.mediaDictionary == null || !_data.mediaDictionary.ContainsKey("Zombie_sleep_0010_23.png") || (int)_data.mediaDictionary["Zombie_sleep_0010_23.png"] != 6)
			{
				throw new InvalidOperationException("The production Sleeper replacement fixture is unavailable.");
			}
			AdobeAnimateSprite.ClearStaticAtlasPathLayoutCacheForTests();
			adobeAnimateSprite = CreateConfiguredOwner();
			long timestamp = Stopwatch.GetTimestamp();
			adobeAnimateSprite.UpdateMediaReplaceData();
			double value = (double)(Stopwatch.GetTimestamp() - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			bool flag = adobeAnimateSprite.mediaReplaceAtlasShared && adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(adobeAnimateSprite.mediaReplaceAtlasArray) && adobeAnimateSprite.mediaReplaceRect.Count == _data.mediaDictionary.Count && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == 1;
			FirstApplyDiagnosticResult result = MeasureFirstApply(bypassStaticCache: true);
			FirstApplyDiagnosticResult result2 = MeasureFirstApply(bypassStaticCache: false);
			bool flag2 = result2.Stable && result2.P99Milliseconds < 0.2 && result2.AllocatedBytes == 0L && result2.Gen0 == 0 && result2.Gen1 == 0 && result2.Gen2 == 0;
			for (int i = 0; i < _owners.Length; i++)
			{
				_owners[i] = CreateConfiguredOwner();
				if (!_owners[i].TryApplyStaticAtlasPathLayoutCacheForTests())
				{
					throw new InvalidOperationException($"Static layout cache missed owner {i}.");
				}
				_owners[i].needMediaReplaceUpdate = false;
			}
			node = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Sleeper/Scene/TowerDefenseZombieSleeper.tscn")?.Instantiate(PackedScene.GenEditState.Disabled);
			adobeAnimateSprite4 = node?.GetNodeOrNull<AdobeAnimateSprite>("SpriteGroup/TransformPoint/ZombieSleeper");
			bool flag3 = GodotObject.IsInstanceValid(adobeAnimateSprite4) && adobeAnimateSprite4.mediaReplaceUse.Count > 6 && adobeAnimateSprite4.mediaReplaceUse[6] && adobeAnimateSprite4.GetAtlasReplacePath("Zombie_sleep_0010_23.png") == "res://Asset/AtlasSource/Armor/Texture/Character/Armor/SleepHat/ZombieSleepHat1.png" && adobeAnimateSprite4.TryApplyStaticAtlasPathLayoutCacheForTests() && adobeAnimateSprite4.mediaReplaceAtlasShared;
			bool flag4 = VerifyPeerIsolation();
			bool flag5 = VerifyClearRestoreQueueFalse(_owners[0]);
			bool flag6 = VerifyDetachedInPlaceMutation();
			adobeAnimateSprite3 = CreateConfiguredOwner();
			adobeAnimateSprite3.atlasProfileOverride = new AdobeAnimateAtlasProfile();
			bool flag7 = !adobeAnimateSprite3.TryApplyStaticAtlasPathLayoutCacheForTests();
			adobeAnimateSprite2 = CreateConfiguredOwner();
			AddChild(adobeAnimateSprite2, forceReadableName: false, InternalMode.Disabled);
			bool num = adobeAnimateSprite2.SetAtlasReplace("Zombie_sleep_0010_23.png", "res://Asset/AtlasSource/Armor/Texture/Character/Armor/SleepHat/ZombieSleepHat1.png", queueUpdate: false);
			int staticAtlasPathLayoutCacheEntryCountForTests = AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests;
			bool flag8 = num && !adobeAnimateSprite2.TryApplyStaticAtlasPathLayoutCacheForTests();
			adobeAnimateSprite2.CreateMediaReplaceAtlas();
			bool flag9 = adobeAnimateSprite2.mediaReplaceAtlasShared && adobeAnimateSprite2.mediaReplaceAtlasUsesTextureArray && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == staticAtlasPathLayoutCacheEntryCountForTests;
			AdobeAnimateSprite.InvalidateStaticAtlasPathLayoutCacheForGpuReset();
			_owners[1].InvalidateRuntimeRenderAfterGpuCacheReset();
			bool num2 = _owners[1].needMediaReplaceUpdate && !_owners[1].CanReuseAppliedStaticAtlasPathLayoutForTests();
			_owners[1].UpdateMediaReplaceData();
			bool flag10 = num2 && !_owners[1].needMediaReplaceUpdate && _owners[1].mediaReplaceAtlasShared && _owners[1].CanReuseAppliedStaticAtlasPathLayoutForTests() && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == 1;
			bool flag11 = _owners[2].CorruptStaticAtlasPathLayoutEntryForTests() && !_owners[2].CanReuseAppliedStaticAtlasPathLayoutForTests();
			_owners[2].CreateMediaReplaceAtlas();
			bool flag12 = flag11 && _owners[2].mediaReplaceAtlasShared && _owners[2].mediaReplaceAtlasUsesTextureArray && _owners[2].CanReuseAppliedStaticAtlasPathLayoutForTests() && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == 1;
			int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
			AdobeAnimateGlobalAtlasCache.Clear();
			bool flag13 = AdobeAnimateGlobalAtlasCache.CacheVersion != cacheVersion && !_owners[0].TryApplyStaticAtlasPathLayoutCacheForTests() && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == 0;
			adobeAnimateSprite.UpdateMediaReplaceData();
			bool flag14 = adobeAnimateSprite.mediaReplaceAtlasShared && AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests == 1;
			bool flag15 = true;
			int num3 = 0;
			for (int j = 0; j < _owners.Length; j++)
			{
				flag15 &= _owners[j].TryApplyStaticAtlasPathLayoutCacheForTests();
				_owners[j].needMediaReplaceUpdate = false;
				if (_owners[j].CanReuseAppliedStaticAtlasPathLayoutForTests())
				{
					num3++;
				}
			}
			OptimizationBatchSampler.PrepareForWarmup();
			long num4 = 0L;
			bool flag16 = true;
			for (int k = 0; k < 240; k++)
			{
				long startTicks = OptimizationBatchSampler.BeginSample();
				flag16 &= ExecuteCacheHitBatch();
				num4 += OptimizationBatchSampler.EndWarmupSample(startTicks);
			}
			long hitSink = _hitSink;
			OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
			bool flag17 = true;
			optimizationBatchSampler.BeginMeasurement();
			for (int l = 0; l < 1200; l++)
			{
				long startTicks2 = OptimizationBatchSampler.BeginSample();
				flag17 &= ExecuteCacheHitBatch();
				optimizationBatchSampler.EndSample(startTicks2);
			}
			OptimizationBatchResult result3 = optimizationBatchSampler.Complete();
			long num5 = 1200000L;
			bool functionalPassed = flag && result.Stable && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9 && flag10 && flag11 && flag12 && flag13 && flag14 && flag15 && num3 == 1000 && flag16 && flag17 && num4 >= 0 && _hitSink - hitSink == num5;
			OptimizationResultIdentity identity = new OptimizationResultIdentity("adobe_animate_static_atlas_path_layout_cache", OptimizationWorkloadKind.BareFunction, "zombie_sleeper_static_replacement", "res://Asset/Anime/Character/Zombie/Chapter1/Sleeper/Scene/TowerDefenseZombieSleeper.tscn", "AdobeAnimateSprite", "none", "none", "none", "applied_layout_reuse", "steady_applied_layout_reuse", OptimizationScheduleKind.BackToBack, "headless", Math.Max(1, Engine.PhysicsTicksPerSecond), 240);
			bool flag18 = OptimizationPerformanceGate.IsBareResultPassed(in result3, functionalPassed, 1000, 240, 1000, 1000, in identity);
			GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result3, functionalPassed, flag18, 1000, 240, 1000, 1000));
			GD.Print("ADOBE_ANIMATE_STATIC_ATLAS_PATH_LAYOUT_COLD_DIAGNOSTIC " + $"coldBuildMs={value:F6} " + $"cacheEntries={AdobeAnimateSprite.StaticAtlasPathLayoutCacheEntryCountForTests} " + $"shared={adobeAnimateSprite.mediaReplaceAtlasShared} " + $"usesTextureArray={adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray}");
			PrintFirstApplyDiagnostic("baseline_slow_path", in result);
			PrintFirstApplyDiagnostic("cached_first_apply", in result2);
			GD.Print("ADOBE_ANIMATE_STATIC_ATLAS_PATH_LAYOUT_SEMANTICS " + $"coldBuildSucceeded={flag} " + $"cachedFirstApplyPassed={flag2} " + $"productionSceneDeserialized={flag3} " + $"peerIsolation={flag4} " + $"clearRestoreQueueFalse={flag5} " + $"detachedInPlaceMutationPreserved={flag6} " + $"profileMismatchBypassed={flag7} " + $"dynamicMutationBypassed={flag8} " + $"dynamicMutationSlowPathPreserved={flag9} " + $"gpuResetReparsed={flag10} " + $"freedAppliedRejected={flag11} " + $"staleEntryRepublished={flag12} " + $"staleVersionRejected={flag13} " + $"versionRebuilt={flag14} " + $"everyOwnerRebound={flag15} " + $"appliedReuseCount={num3}");
			GetTree().Quit((!flag18) ? 2 : 0);
		}
		catch (Exception value2)
		{
			GD.PrintErr("ADOBE_ANIMATE_STATIC_ATLAS_PATH_LAYOUT_SEMANTICS " + $"passed=False exception={value2}");
			GetTree().Quit(2);
		}
		finally
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite4))
			{
				adobeAnimateSprite4.flashAnimeData = null;
			}
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
			ReleaseOwner(adobeAnimateSprite3);
			if (GodotObject.IsInstanceValid(adobeAnimateSprite2))
			{
				if (adobeAnimateSprite2.GetParent() == this)
				{
					RemoveChild(adobeAnimateSprite2);
				}
				ReleaseOwner(adobeAnimateSprite2);
			}
			for (int m = 0; m < _owners.Length; m++)
			{
				ReleaseOwner(_owners[m]);
				_owners[m] = null;
			}
			ReleaseOwner(adobeAnimateSprite);
			AdobeAnimateSprite.ClearStaticAtlasPathLayoutCacheForTests();
		}
	}

	private AdobeAnimateSprite CreateConfiguredOwner()
	{
		AdobeAnimateSprite obj = new AdobeAnimateSprite
		{
			flashAnimeData = _data
		};
		Array<string> array = new Array<string>();
		array.Resize(_data.mediaDictionary.Count);
		array[6] = "res://Asset/AtlasSource/Armor/Texture/Character/Armor/SleepHat/ZombieSleepHat1.png";
		obj.mediaReplaceAtlasPaths = array;
		obj.mediaReplaceUse[6] = true;
		return obj;
	}

	private FirstApplyDiagnosticResult MeasureFirstApply(bool bypassStaticCache)
	{
		for (int i = 0; i < 240; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = CreateConfiguredOwner();
			if (bypassStaticCache)
			{
				adobeAnimateSprite.CreateMediaReplaceAtlasWithoutStaticCacheForTests();
			}
			else
			{
				adobeAnimateSprite.TryApplyStaticAtlasPathLayoutCacheForTests();
			}
			ReleaseOwner(adobeAnimateSprite);
		}
		AdobeAnimateSprite[] array = new AdobeAnimateSprite[1000];
		double[] array2 = new double[1000];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = CreateConfiguredOwner();
		}
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		long allocatedBytesForCurrentThread = GC.GetAllocatedBytesForCurrentThread();
		int num = GC.CollectionCount(0);
		int num2 = GC.CollectionCount(1);
		int num3 = GC.CollectionCount(2);
		bool flag = true;
		for (int k = 0; k < array.Length; k++)
		{
			AdobeAnimateSprite adobeAnimateSprite2 = array[k];
			long timestamp = Stopwatch.GetTimestamp();
			bool flag2;
			if (bypassStaticCache)
			{
				adobeAnimateSprite2.CreateMediaReplaceAtlasWithoutStaticCacheForTests();
				flag2 = adobeAnimateSprite2.mediaReplaceAtlasShared && adobeAnimateSprite2.mediaReplaceAtlasUsesTextureArray;
			}
			else
			{
				flag2 = adobeAnimateSprite2.TryApplyStaticAtlasPathLayoutCacheForTests();
			}
			long timestamp2 = Stopwatch.GetTimestamp();
			array2[k] = (double)(timestamp2 - timestamp) * 1000.0 / (double)Stopwatch.Frequency;
			flag &= flag2 && adobeAnimateSprite2.mediaReplaceAtlasShared && adobeAnimateSprite2.mediaReplaceAtlasUsesTextureArray;
		}
		long allocatedBytes = GC.GetAllocatedBytesForCurrentThread() - allocatedBytesForCurrentThread;
		int gen = GC.CollectionCount(0) - num;
		int gen2 = GC.CollectionCount(1) - num2;
		int gen3 = GC.CollectionCount(2) - num3;
		double num4 = 0.0;
		for (int l = 0; l < array2.Length; l++)
		{
			num4 += array2[l];
		}
		System.Array.Sort(array2);
		FirstApplyDiagnosticResult result = new FirstApplyDiagnosticResult(num4 / (double)array2.Length, Percentile(array2, 0.5), Percentile(array2, 0.95), Percentile(array2, 0.99), array2[^1], allocatedBytes, gen, gen2, gen3, flag);
		for (int m = 0; m < array.Length; m++)
		{
			ReleaseOwner(array[m]);
		}
		return result;
	}

	private static double Percentile(double[] sortedValues, double percentile)
	{
		int num = Math.Clamp((int)Math.Ceiling((double)sortedValues.Length * percentile) - 1, 0, sortedValues.Length - 1);
		return sortedValues[num];
	}

	private static void PrintFirstApplyDiagnostic(string variant, in FirstApplyDiagnosticResult result)
	{
		GD.Print("ADOBE_ANIMATE_STATIC_ATLAS_PATH_LAYOUT_FIRST_APPLY_DIAGNOSTIC " + $"variant={variant} instances={1000} " + $"warmupOwners={240} " + $"meanMs={result.MeanMilliseconds:F6} " + $"p50Ms={result.P50Milliseconds:F6} " + $"p95Ms={result.P95Milliseconds:F6} " + $"p99Ms={result.P99Milliseconds:F6} " + $"maxMs={result.MaxMilliseconds:F6} " + $"allocatedBytes={result.AllocatedBytes} " + $"gen0={result.Gen0} gen1={result.Gen1} " + $"gen2={result.Gen2} stable={result.Stable}");
	}

	private bool ExecuteCacheHitBatch()
	{
		int num = 0;
		for (int i = 0; i < _owners.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _owners[i];
			if (adobeAnimateSprite.CanReuseAppliedStaticAtlasPathLayoutForTests() && adobeAnimateSprite.mediaReplaceAtlasShared && adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray)
			{
				num++;
			}
		}
		_hitSink += num;
		return num == 1000;
	}

	private bool VerifyPeerIsolation()
	{
		AdobeAnimateSprite obj = _owners[0];
		AdobeAnimateSprite adobeAnimateSprite = _owners[999];
		Rect2 rect = obj.mediaReplaceRect[6];
		adobeAnimateSprite.mediaReplaceRect[6] = new Rect2(999f, 999f, 1f, 1f);
		bool num = obj.mediaReplaceRect[6] == rect && adobeAnimateSprite.mediaReplaceRect[6] != rect;
		bool flag = adobeAnimateSprite.TryApplyStaticAtlasPathLayoutCacheForTests() && adobeAnimateSprite.mediaReplaceRect[6] == rect;
		return num && flag;
	}

	private static bool VerifyClearRestoreQueueFalse(AdobeAnimateSprite owner)
	{
		bool num = owner.SetAtlasReplace("Zombie_sleep_0010_23.png", string.Empty, queueUpdate: false);
		bool flag = !owner.needMediaReplaceUpdate;
		owner.CreateMediaReplaceAtlas();
		bool flag2 = !owner.mediaReplaceAtlasShared && !owner.mediaReplaceAtlasUsesTextureArray && owner.mediaReplaceAtlasArray == null;
		bool flag3 = owner.SetAtlasReplace("Zombie_sleep_0010_23.png", "res://Asset/AtlasSource/Armor/Texture/Character/Armor/SleepHat/ZombieSleepHat1.png", queueUpdate: false);
		bool flag4 = !owner.needMediaReplaceUpdate;
		owner.CreateMediaReplaceAtlas();
		bool flag5 = owner.mediaReplaceAtlasShared && owner.mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(owner.mediaReplaceAtlasArray);
		return num && flag && flag2 && flag3 && flag4 && flag5;
	}

	private bool VerifyDetachedInPlaceMutation()
	{
		AdobeAnimateSprite adobeAnimateSprite = CreateConfiguredOwner();
		try
		{
			bool num = adobeAnimateSprite.TryApplyStaticAtlasPathLayoutCacheForTests() && adobeAnimateSprite.mediaReplaceAtlasShared && adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray;
			adobeAnimateSprite.mediaReplaceUse[6] = false;
			adobeAnimateSprite.UpdateMediaReplaceData();
			bool flag = !adobeAnimateSprite.mediaReplaceUse[6] && !adobeAnimateSprite.mediaReplaceAtlasShared && !adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray && adobeAnimateSprite.mediaReplaceAtlasArray == null;
			adobeAnimateSprite.mediaReplaceUse[6] = true;
			adobeAnimateSprite.UpdateMediaReplaceData();
			bool flag2 = adobeAnimateSprite.mediaReplaceUse[6] && adobeAnimateSprite.mediaReplaceAtlasShared && adobeAnimateSprite.mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(adobeAnimateSprite.mediaReplaceAtlasArray);
			return num && flag && flag2;
		}
		finally
		{
			ReleaseOwner(adobeAnimateSprite);
		}
	}

	private static void ReleaseOwner(AdobeAnimateSprite owner)
	{
		if (GodotObject.IsInstanceValid(owner))
		{
			owner.flashAnimeData = null;
			owner.Free();
		}
	}
}
