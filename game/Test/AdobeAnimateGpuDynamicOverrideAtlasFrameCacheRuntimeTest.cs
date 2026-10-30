using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public sealed partial class AdobeAnimateGpuDynamicOverrideAtlasFrameCacheRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupBatchCount = 240;

	private const int SampleCount = 1200;

	private const ulong SignatureA = 2703024129uL;

	private const ulong SignatureB = 2989285378uL;

	private readonly List<string> _failures = new List<string>();

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		AdobeAnimateGpuDynamicOverrideAtlas adobeAnimateGpuDynamicOverrideAtlas = new AdobeAnimateGpuDynamicOverrideAtlas();
		AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState = CreateOwnerState(2703024129uL, new Rect2(4f, 8f, 32f, 48f), 0);
		AdobeAnimateGpuGraphOwnerState adobeAnimateGpuGraphOwnerState2 = CreateOwnerState(2989285378uL, new Rect2(64f, 16f, 24f, 40f), 1);
		AdobeAnimateGpuGraphOwnerState[] array = new AdobeAnimateGpuGraphOwnerState[1000];
		AdobeAnimateGpuGraphOwnerState[] array2 = new AdobeAnimateGpuGraphOwnerState[1000];
		for (int i = 0; i < 1000; i++)
		{
			array[i] = adobeAnimateGpuGraphOwnerState;
			array2[i] = (((i & 1) == 0) ? adobeAnimateGpuGraphOwnerState : adobeAnimateGpuGraphOwnerState2);
		}
		long frameVersion = 1L;
		adobeAnimateGpuDynamicOverrideAtlas.BeginFrame(frameVersion);
		AdobeAnimateGpuDynamicOverrideAllocation allocation;
		bool flag = adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, adobeAnimateGpuGraphOwnerState, out allocation);
		AdobeAnimateGpuDynamicOverrideAllocation allocation2;
		bool flag2 = adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, adobeAnimateGpuGraphOwnerState2, out allocation2);
		bool flag3 = flag && flag2 && allocation.Signature == 2703024129u && allocation.BaseTexel == 0 && allocation.MediaCount == 1 && allocation2.Signature == 2989285378u && allocation2.BaseTexel != allocation.BaseTexel && allocation2.MediaCount == 1 && adobeAnimateGpuDynamicOverrideAtlas.AllocationCount == 2;
		Check(flag3, "Two distinct signatures must receive stable, distinct allocations.");
		bool warmupStable;
		bool measuredStable;
		long warmupTicks;
		OptimizationBatchResult result = MeasureSameSignatureWorkload(adobeAnimateGpuDynamicOverrideAtlas, ref frameVersion, array, in allocation, out warmupStable, out measuredStable, out warmupTicks);
		Check(warmupStable && measuredStable, "Every same-signature lookup must reuse the signature-A allocation.");
		Check(result.AllocatedBytes == 0, "The measured same-signature workload must not allocate.");
		bool warmupStable2;
		bool measuredStable2;
		long warmupTicks2;
		OptimizationBatchResult result2 = MeasureAlternatingSignatureWorkload(adobeAnimateGpuDynamicOverrideAtlas, ref frameVersion, array2, in allocation, in allocation2, out warmupStable2, out measuredStable2, out warmupTicks2);
		Check(warmupStable2 && measuredStable2, "Alternating signatures must preserve both persistent allocations.");
		Check(result2.AllocatedBytes == 0, "The measured alternating-signature workload must not allocate.");
		AdobeAnimateGpuGraphOwnerState ownerState = CreateOwnerState(2703024129uL, new Rect2(4f, 8f, 32f, 48f), 0, 2);
		AdobeAnimateGpuDynamicOverrideAllocation allocation3;
		bool flag4 = !adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, ownerState, out allocation3);
		Check(flag4, "The frame-local hit must not alias a different MediaCount.");
		long frameVersion2 = frameVersion;
		frameVersion++;
		adobeAnimateGpuDynamicOverrideAtlas.BeginFrame(frameVersion);
		bool flag5 = !adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion2, adobeAnimateGpuGraphOwnerState, out allocation3);
		AdobeAnimateGpuDynamicOverrideAllocation allocation4;
		bool flag6 = adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, adobeAnimateGpuGraphOwnerState, out allocation4) && SameAllocation(in allocation4, in allocation);
		adobeAnimateGpuDynamicOverrideAtlas.BeginFrame(frameVersion);
		AdobeAnimateGpuDynamicOverrideAllocation allocation5;
		bool flag7 = adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, adobeAnimateGpuGraphOwnerState, out allocation5) && SameAllocation(in allocation5, in allocation);
		Check(flag5 && flag6 && flag7, "Frame validation must reject stale versions and preserve repeated BeginFrame calls.");
		int allocationCount = adobeAnimateGpuDynamicOverrideAtlas.AllocationCount;
		adobeAnimateGpuDynamicOverrideAtlas.ResetForTests();
		frameVersion++;
		adobeAnimateGpuDynamicOverrideAtlas.BeginFrame(frameVersion);
		AdobeAnimateGpuDynamicOverrideAllocation allocation6;
		bool flag8 = adobeAnimateGpuDynamicOverrideAtlas.TryGetOrAdd(frameVersion, adobeAnimateGpuGraphOwnerState, out allocation6) && allocation6.Signature == 2703024129u && allocation6.BaseTexel == 0 && allocation6.MediaCount == 1 && adobeAnimateGpuDynamicOverrideAtlas.AllocationCount == 1;
		Check(flag8, "ResetForTests must invalidate the cache and restart at BaseTexel zero.");
		adobeAnimateGpuDynamicOverrideAtlas.ResetForTests();
		bool functionalPassed = _failures.Count == 0 && warmupTicks >= 0 && warmupTicks2 >= 0 && flag3 && flag4 && flag5 && flag7 && flag8;
		OptimizationResultIdentity identity = CreateIdentity("same_signature_last_hit");
		OptimizationResultIdentity identity2 = CreateIdentity("alternating_signature_lookup");
		bool flag9 = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
		bool flag10 = OptimizationPerformanceGate.IsBareResultPassed(in result2, functionalPassed, 1000, 240, 1000, 1000, in identity2);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag9, 1000, 240, 1000, 1000));
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity2, in result2, functionalPassed, flag10, 1000, 240, 1000, 1000));
		GD.Print("ADOBE_ANIMATE_GPU_DYNAMIC_OVERRIDE_FRAME_CACHE_SEMANTICS " + $"signaturesAllocated={flag3} " + $"sameStable={warmupStable && measuredStable} " + $"alternatingStable={warmupStable2 && measuredStable2} " + $"mediaCountChangedRejected={flag4} " + $"oldFrameRejected={flag5} frameChanged={flag6} " + $"repeatedBeginFrameStable={flag7} " + $"resetCorrect={flag8} " + $"allocationCountBeforeReset={allocationCount} " + $"failures={_failures.Count}");
		for (int j = 0; j < _failures.Count; j++)
		{
			GD.PrintErr(_failures[j]);
		}
		GetTree().Quit((!(flag9 && flag10)) ? 2 : 0);
	}

	private static OptimizationBatchResult MeasureSameSignatureWorkload(AdobeAnimateGpuDynamicOverrideAtlas atlas, ref long frameVersion, AdobeAnimateGpuGraphOwnerState[] ownerStates, in AdobeAnimateGpuDynamicOverrideAllocation expected, out bool warmupStable, out bool measuredStable, out long warmupTicks)
	{
		OptimizationBatchSampler.PrepareForWarmup();
		warmupStable = true;
		warmupTicks = 0L;
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			frameVersion++;
			atlas.BeginFrame(frameVersion);
			warmupStable &= RunSameSignatureBatch(atlas, frameVersion, ownerStates, in expected);
			warmupTicks += OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		measuredStable = true;
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			frameVersion++;
			atlas.BeginFrame(frameVersion);
			measuredStable &= RunSameSignatureBatch(atlas, frameVersion, ownerStates, in expected);
			optimizationBatchSampler.EndSample(startTicks2);
		}
		return optimizationBatchSampler.Complete();
	}

	private static OptimizationBatchResult MeasureAlternatingSignatureWorkload(AdobeAnimateGpuDynamicOverrideAtlas atlas, ref long frameVersion, AdobeAnimateGpuGraphOwnerState[] ownerStates, in AdobeAnimateGpuDynamicOverrideAllocation expectedA, in AdobeAnimateGpuDynamicOverrideAllocation expectedB, out bool warmupStable, out bool measuredStable, out long warmupTicks)
	{
		OptimizationBatchSampler.PrepareForWarmup();
		warmupStable = true;
		warmupTicks = 0L;
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			frameVersion++;
			atlas.BeginFrame(frameVersion);
			warmupStable &= RunAlternatingSignatureBatch(atlas, frameVersion, ownerStates, in expectedA, in expectedB);
			warmupTicks += OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		measuredStable = true;
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 1200; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			frameVersion++;
			atlas.BeginFrame(frameVersion);
			measuredStable &= RunAlternatingSignatureBatch(atlas, frameVersion, ownerStates, in expectedA, in expectedB);
			optimizationBatchSampler.EndSample(startTicks2);
		}
		return optimizationBatchSampler.Complete();
	}

	private static bool RunSameSignatureBatch(AdobeAnimateGpuDynamicOverrideAtlas atlas, long frameVersion, AdobeAnimateGpuGraphOwnerState[] ownerStates, in AdobeAnimateGpuDynamicOverrideAllocation expected)
	{
		for (int i = 0; i < ownerStates.Length; i++)
		{
			if (!atlas.TryGetOrAdd(frameVersion, ownerStates[i], out var allocation) || !SameAllocation(in allocation, in expected))
			{
				return false;
			}
		}
		return true;
	}

	private static bool RunAlternatingSignatureBatch(AdobeAnimateGpuDynamicOverrideAtlas atlas, long frameVersion, AdobeAnimateGpuGraphOwnerState[] ownerStates, in AdobeAnimateGpuDynamicOverrideAllocation expectedA, in AdobeAnimateGpuDynamicOverrideAllocation expectedB)
	{
		for (int i = 0; i < ownerStates.Length; i++)
		{
			AdobeAnimateGpuDynamicOverrideAllocation right = (((i & 1) == 0) ? expectedA : expectedB);
			if (!atlas.TryGetOrAdd(frameVersion, ownerStates[i], out var allocation) || !SameAllocation(in allocation, in right))
			{
				return false;
			}
		}
		return true;
	}

	private static OptimizationResultIdentity CreateIdentity(string stateId)
	{
		return new OptimizationResultIdentity("adobe_animate_dynamic_override_atlas_cache", OptimizationWorkloadKind.BareFunction, "media_replace_owner", "res://Test/AdobeAnimateGpuDynamicOverrideAtlasFrameCacheRuntimeTest.tscn", "AdobeAnimateGpuDynamicOverrideAtlas", "none", "none", "none", stateId, "transaction_resolve", OptimizationScheduleKind.BackToBack, "headless", Math.Max(1, Engine.PhysicsTicksPerSecond), 240);
	}

	private static bool SameAllocation(in AdobeAnimateGpuDynamicOverrideAllocation left, in AdobeAnimateGpuDynamicOverrideAllocation right)
	{
		if (left.Signature == right.Signature && left.BaseTexel == right.BaseTexel)
		{
			return left.MediaCount == right.MediaCount;
		}
		return false;
	}

	private static AdobeAnimateGpuGraphOwnerState CreateOwnerState(ulong signature, Rect2 rect, int atlasPage, int mediaCount = 1)
	{
		Array<Rect2> array = new Array<Rect2>();
		Array<bool> array2 = new Array<bool>();
		Array<int> array3 = new Array<int>();
		for (int i = 0; i < mediaCount; i++)
		{
			array.Add(rect);
			array2.Add(item: true);
			array3.Add(atlasPage);
		}
		return new AdobeAnimateGpuGraphOwnerState(null, null, Transform2D.Identity, Colors.White, Vector2.Zero, default(VerticalClipState), 0, 0f, allLayersVisible: true, canUseLayerMask: true, 18446744073709551615uL, null, 0, hasMediaReplace: true, array, array2, array3, new Vector2(256f, 256f), mediaCount, signature, visible: true, default(AdobeAnimateClipBlendState));
	}

	private void Check(bool condition, string failure)
	{
		if (!condition)
		{
			_failures.Add(failure);
		}
	}
}
