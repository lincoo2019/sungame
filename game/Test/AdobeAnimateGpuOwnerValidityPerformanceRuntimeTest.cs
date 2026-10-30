using System;
using Godot;

public sealed partial class AdobeAnimateGpuOwnerValidityPerformanceRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSampleCount = 240;

	private const int SampleCount = 1200;

	private readonly AdobeAnimateSprite[] _owners = new AdobeAnimateSprite[1000];

	private bool _legacyOuterValidity;

	private long _rejectedSink;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		try
		{
			_legacyOuterValidity = Array.Exists(OS.GetCmdlineUserArgs(), (string argument) => argument == "--legacy-outer-validity");
			for (int num = 0; num < _owners.Length; num++)
			{
				_owners[num] = new AdobeAnimateSprite();
			}
			OptimizationBatchSampler.PrepareForWarmup();
			long num2 = 0L;
			for (int num3 = 0; num3 < 240; num3++)
			{
				long startTicks = OptimizationBatchSampler.BeginSample();
				ExecuteBatch();
				num2 += OptimizationBatchSampler.EndWarmupSample(startTicks);
			}
			long rejectedSink = _rejectedSink;
			OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
			optimizationBatchSampler.BeginMeasurement();
			for (int num4 = 0; num4 < 1200; num4++)
			{
				long startTicks2 = OptimizationBatchSampler.BeginSample();
				ExecuteBatch();
				optimizationBatchSampler.EndSample(startTicks2);
			}
			OptimizationBatchResult result = optimizationBatchSampler.Complete();
			long num5 = 1200000L;
			bool flag = VerifyValidOwnerLifecycle();
			bool flag2 = VerifyNullOwnerLifecycle();
			bool flag3 = VerifyFreedOwnerLifecycle();
			bool functionalPassed = _rejectedSink - rejectedSink == num5 && result.SampleCount == 1200 && num2 >= 0 && flag && flag2 && flag3;
			OptimizationResultIdentity identity = new OptimizationResultIdentity("adobe_animate_gpu_owner_validity_fast_path", OptimizationWorkloadKind.BareFunction, "adobe_animate_gpu_owner", "res://Test/AdobeAnimateGpuOwnerValidityPerformanceRuntimeTest.tscn", "AdobeAnimateSprite", "none", "none", "none", "owner_definition_validation", "render_prepare", OptimizationScheduleKind.BackToBack, "headless", Math.Max(1, Engine.PhysicsTicksPerSecond), 240);
			bool flag4 = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
			GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag4, 1000, 240, 1000, 1000));
			GD.Print("ADOBE_ANIMATE_GPU_OWNER_VALIDITY_SEMANTICS " + $"legacyOuterValidity={_legacyOuterValidity} " + $"validOwnerLifecycle={flag} " + $"nullOwnerLifecycle={flag2} " + $"freedOwnerLifecycle={flag3}");
			GetTree().Quit((!flag4) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr("ADOBE_ANIMATE_GPU_OWNER_VALIDITY_SEMANTICS " + $"passed=False exception={value}");
			GetTree().Quit(2);
		}
		finally
		{
			for (int num6 = 0; num6 < _owners.Length; num6++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _owners[num6];
				if (GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					adobeAnimateSprite.Free();
				}
				_owners[num6] = null;
			}
		}
	}

	private void ExecuteBatch()
	{
		int num = 0;
		AdobeAnimateRuntimeDefinition definition;
		if (_legacyOuterValidity)
		{
			for (int i = 0; i < _owners.Length; i++)
			{
				AdobeAnimateSprite adobeAnimateSprite = _owners[i];
				if (!GodotObject.IsInstanceValid(adobeAnimateSprite) || !adobeAnimateSprite.TryGetGpuGraphOwnerDefinitionForRender(out definition))
				{
					num++;
				}
			}
		}
		else
		{
			for (int j = 0; j < _owners.Length; j++)
			{
				AdobeAnimateSprite adobeAnimateSprite2 = _owners[j];
				if (adobeAnimateSprite2 == null || !adobeAnimateSprite2.TryGetGpuGraphOwnerDefinitionForRender(out definition))
				{
					num++;
				}
			}
		}
		_rejectedSink += num;
	}

	private bool VerifyValidOwnerLifecycle()
	{
		AdobeAnimateSprite adobeAnimateSprite = _owners[0];
		AdobeAnimateRuntimeDefinition definition;
		if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite != null)
		{
			return !adobeAnimateSprite.TryGetGpuGraphOwnerDefinitionForRender(out definition);
		}
		return false;
	}

	private static bool VerifyNullOwnerLifecycle()
	{
		return null == null;
	}

	private static bool VerifyFreedOwnerLifecycle()
	{
		AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite();
		adobeAnimateSprite.Free();
		AdobeAnimateRuntimeDefinition definition;
		if (adobeAnimateSprite != null && !GodotObject.IsInstanceValid(adobeAnimateSprite))
		{
			return !adobeAnimateSprite.TryGetGpuGraphOwnerDefinitionForRender(out definition);
		}
		return false;
	}
}
