using System;
using Godot;

public sealed partial class AdobeAnimateSharedGpuClockPerformanceRuntimeTest : Node
{
	private const int InstanceCount = 1000;

	private const int WarmupSampleCount = 240;

	private const int SampleCount = 1200;

	private const float RuntimeManagerClockSeconds = 11.25f;

	private const float ExplicitClockSeconds = 73.5f;

	private const float FrameFloat = 4.25f;

	private readonly AdobeAnimateSprite[] _owners = new AdobeAnimateSprite[1000];

	private AdobeAnimateRuntimeManager _runtimeManager;

	private double _sink;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		bool flag = Array.Exists(OS.GetCmdlineUserArgs(), (string argument) => argument == "--legacy-clock");
		try
		{
			_runtimeManager = new AdobeAnimateRuntimeManager
			{
				Name = "SharedGpuClockRuntimeManager"
			};
			AddChild(_runtimeManager, forceReadableName: false, InternalMode.Disabled);
			_runtimeManager.SetAnimationClockForBareTest(11.25);
			AdobeAnimateRenderManager.SetAnimationClock(11.25);
			for (int num = 0; num < _owners.Length; num++)
			{
				AdobeAnimateSprite adobeAnimateSprite = new AdobeAnimateSprite();
				adobeAnimateSprite.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 2, 12, 30.0, 1.0, reverse: false, shouldLoop: true);
				_owners[num] = adobeAnimateSprite;
			}
			OptimizationBatchSampler.PrepareForWarmup();
			long num2 = 0L;
			for (int num3 = 0; num3 < 240; num3++)
			{
				long startTicks = OptimizationBatchSampler.BeginSample();
				ExecuteBatch();
				num2 += OptimizationBatchSampler.EndWarmupSample(startTicks);
			}
			double sink = _sink;
			OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
			optimizationBatchSampler.BeginMeasurement();
			for (int num4 = 0; num4 < 1200; num4++)
			{
				long startTicks2 = OptimizationBatchSampler.BeginSample();
				ExecuteBatch();
				optimizationBatchSampler.EndSample(startTicks2);
			}
			OptimizationBatchResult result = optimizationBatchSampler.Complete();
			float expectedClock = (flag ? 11.25f : 73.5f);
			bool flag2 = VerifyForwardClock(expectedClock);
			bool flag3 = VerifyReverseClock(expectedClock);
			bool flag4 = VerifyDisabledPredicate(expectedClock, paused: true, reachedClipEnd: false, -1.0, 2, 12, 30.0);
			bool flag5 = VerifyDisabledPredicate(expectedClock, paused: false, reachedClipEnd: true, -1.0, 2, 12, 30.0);
			bool flag6 = VerifyDisabledPredicate(expectedClock, paused: false, reachedClipEnd: false, 0.25, 2, 12, 30.0);
			bool flag7 = VerifyDisabledPredicate(expectedClock, paused: false, reachedClipEnd: false, -1.0, 2, 3, 30.0);
			bool flag8 = VerifyDisabledPredicate(expectedClock, paused: false, reachedClipEnd: false, -1.0, 2, 12, 0.0);
			bool flag9 = VerifyDisabledPredicate(expectedClock, paused: false, reachedClipEnd: false, -1.0, 2, 12, 30.0, dispatchActive: false);
			bool flag10 = VerifyOwnerClockExact(expectedClock);
			bool flag11 = VerifyDisabledSelectionDefault();
			bool flag12 = VerifyClockRefresh(flag);
			bool flag13 = VerifyInvalidClockSanitization();
			bool flag14 = VerifyPlaybackControl();
			bool functionalPassed = result.SampleCount == 1200 && num2 >= 0 && double.IsFinite(_sink) && _sink != sink && flag2 && flag3 && flag4 && flag5 && flag6 && flag7 && flag8 && flag9 && flag10 && flag11 && flag12 && flag13 && flag14;
			OptimizationResultIdentity identity = new OptimizationResultIdentity("adobe_animate_shared_gpu_clock_capture", OptimizationWorkloadKind.BareFunction, "adobe_animate_gpu_owner", "res://Test/AdobeAnimateSharedGpuClockPerformanceRuntimeTest.tscn", "AdobeAnimateSprite", "none", "none", "none", "owner_state_clock_capture", "render_prepare", OptimizationScheduleKind.BackToBack, "headless", Math.Max(1, Engine.PhysicsTicksPerSecond), 240);
			bool flag15 = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
			GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag15, 1000, 240, 1000, 1000));
			GD.Print("ADOBE_ANIMATE_SHARED_GPU_CLOCK_SEMANTICS " + $"legacyClockExpected={flag} " + $"forward={flag2} reverse={flag3} " + $"pauseDisabled={flag4} " + $"clipOverDisabled={flag5} " + $"delayDisabled={flag6} " + $"singleFrameDisabled={flag7} " + $"zeroRateDisabled={flag8} " + $"dispatchDisabled={flag9} " + $"ownerClockExact={flag10} " + $"disabledSelectionDefault={flag11} " + $"clockRefreshExact={flag12} " + $"invalidClockSanitized={flag13} " + $"playbackControl={flag14}");
			GetTree().Quit((!flag15) ? 2 : 0);
		}
		catch (Exception value)
		{
			GD.PrintErr("ADOBE_ANIMATE_SHARED_GPU_CLOCK_SEMANTICS " + $"passed=False exception={value}");
			GetTree().Quit(2);
		}
		finally
		{
			for (int num5 = 0; num5 < _owners.Length; num5++)
			{
				AdobeAnimateSprite adobeAnimateSprite2 = _owners[num5];
				if (GodotObject.IsInstanceValid(adobeAnimateSprite2))
				{
					adobeAnimateSprite2.Free();
				}
				_owners[num5] = null;
			}
			if (GodotObject.IsInstanceValid(_runtimeManager))
			{
				_runtimeManager.Free();
			}
			_runtimeManager = null;
		}
	}

	private void ExecuteBatch()
	{
		double num = 0.0;
		for (int i = 0; i < _owners.Length; i++)
		{
			AdobeAnimateGpuClockState adobeAnimateGpuClockState = _owners[i].BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
			num += (double)(adobeAnimateGpuClockState.StartTime + adobeAnimateGpuClockState.StartFrame + adobeAnimateGpuClockState.FramesPerSecond) + (adobeAnimateGpuClockState.Enabled ? 1.0 : 0.0);
		}
		_sink += num;
	}

	private bool VerifyForwardClock(float expectedClock)
	{
		AdobeAnimateSprite obj = _owners[0];
		obj.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 2, 12, 30.0, 1.5, reverse: false, shouldLoop: true);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = obj.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		if (adobeAnimateGpuClockState.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, expectedClock) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartFrame, 4.25f) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.FramesPerSecond, 45f) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.ClipStart, 2f) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.ClipEndExclusive, 12f))
		{
			return adobeAnimateGpuClockState.Loop;
		}
		return false;
	}

	private bool VerifyReverseClock(float expectedClock)
	{
		AdobeAnimateSprite obj = _owners[1];
		obj.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 3, 15, 24.0, 0.5, reverse: true, shouldLoop: false);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = obj.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		if (adobeAnimateGpuClockState.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, expectedClock) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.FramesPerSecond, -12f) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.ClipStart, 3f) && Mathf.IsEqualApprox(adobeAnimateGpuClockState.ClipEndExclusive, 15f))
		{
			return !adobeAnimateGpuClockState.Loop;
		}
		return false;
	}

	private bool VerifyDisabledPredicate(float expectedClock, bool paused, bool reachedClipEnd, double delaySeconds, int clipStart, int clipEndExclusive, double framesPerSecond, bool dispatchActive = true)
	{
		AdobeAnimateSprite obj = _owners[2];
		obj.ConfigureGpuClockForBareTest(paused, reachedClipEnd, delaySeconds, clipStart, clipEndExclusive, framesPerSecond, 1.0, reverse: false, shouldLoop: true, dispatchActive);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = obj.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		if (!adobeAnimateGpuClockState.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, expectedClock))
		{
			return Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartFrame, 4.25f);
		}
		return false;
	}

	private bool VerifyOwnerClockExact(float expectedClock)
	{
		AdobeAnimateSprite obj = _owners[3];
		AdobeAnimateSprite adobeAnimateSprite = _owners[4];
		obj.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 1, 8, 30.0, 1.0, reverse: false, shouldLoop: true);
		adobeAnimateSprite.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 1, 8, 30.0, 1.0, reverse: false, shouldLoop: true);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = obj.BuildGpuGraphClockForBareTest(4.25f, 73.5f, enableGpuClock: true);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState2 = adobeAnimateSprite.BuildGpuGraphClockForBareTest(5.25f, 73.5f, enableGpuClock: true);
		if (adobeAnimateGpuClockState.Enabled && adobeAnimateGpuClockState2.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, expectedClock) && Mathf.IsEqualApprox(adobeAnimateGpuClockState2.StartTime, expectedClock))
		{
			return Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, adobeAnimateGpuClockState2.StartTime);
		}
		return false;
	}

	private bool VerifyDisabledSelectionDefault()
	{
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = _owners[6].BuildGpuGraphClockForBareTest(4.25f, 73.5f, enableGpuClock: false);
		if (!adobeAnimateGpuClockState.Enabled && Mathf.IsZeroApprox(adobeAnimateGpuClockState.StartTime) && Mathf.IsZeroApprox(adobeAnimateGpuClockState.StartFrame) && Mathf.IsZeroApprox(adobeAnimateGpuClockState.FramesPerSecond) && Mathf.IsZeroApprox(adobeAnimateGpuClockState.ClipStart) && Mathf.IsZeroApprox(adobeAnimateGpuClockState.ClipEndExclusive))
		{
			return !adobeAnimateGpuClockState.Loop;
		}
		return false;
	}

	private bool VerifyClockRefresh(bool legacyClockExpected)
	{
		AdobeAnimateSprite obj = _owners[5];
		obj.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 1, 8, 30.0, 1.0, reverse: false, shouldLoop: true);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = obj.BuildRuntimeGpuClockStateForBareTest(4.25f, 19.25f);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState2 = obj.BuildRuntimeGpuClockStateForBareTest(4.25f, 91.75f);
		float b = (legacyClockExpected ? 11.25f : 19.25f);
		float b2 = (legacyClockExpected ? 11.25f : 91.75f);
		if (Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, b) && Mathf.IsEqualApprox(adobeAnimateGpuClockState2.StartTime, b2))
		{
			if (!legacyClockExpected)
			{
				return !Mathf.IsEqualApprox(adobeAnimateGpuClockState.StartTime, adobeAnimateGpuClockState2.StartTime);
			}
			return true;
		}
		return false;
	}

	private static bool VerifyInvalidClockSanitization()
	{
		AdobeAnimateRenderManager.SetAnimationClock(0.0 / 0.0);
		bool num = Mathf.IsZeroApprox(AdobeAnimateRenderManager.AnimationClockSecondsForBareTest);
		AdobeAnimateRenderManager.SetAnimationClock(1.0 / 0.0);
		bool flag = Mathf.IsZeroApprox(AdobeAnimateRenderManager.AnimationClockSecondsForBareTest);
		AdobeAnimateRenderManager.SetAnimationClock(-1.0 / 0.0);
		bool flag2 = Mathf.IsZeroApprox(AdobeAnimateRenderManager.AnimationClockSecondsForBareTest);
		AdobeAnimateRenderManager.SetAnimationClock(11.25);
		if (num && flag && flag2)
		{
			return Mathf.IsEqualApprox(AdobeAnimateRenderManager.AnimationClockSecondsForBareTest, 11.25f);
		}
		return false;
	}

	private bool VerifyPlaybackControl()
	{
		AdobeAnimateSprite adobeAnimateSprite = _owners[7];
		AdobeAnimateSprite adobeAnimateSprite2 = _owners[8];
		adobeAnimateSprite.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 2, 12, 30.0, 1.0, reverse: false, shouldLoop: true);
		adobeAnimateSprite2.ConfigureGpuClockForBareTest(paused: false, reachedClipEnd: false, -1.0, 2, 12, 30.0, 1.0, reverse: false, shouldLoop: true);
		adobeAnimateSprite.ClearRenderSubmissionForBareTest();
		ulong playbackRevisionForBareTest = adobeAnimateSprite.PlaybackRevisionForBareTest;
		adobeAnimateSprite.playBack = true;
		AdobeAnimateGpuClockState adobeAnimateGpuClockState = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		bool num = adobeAnimateSprite.NeedsRenderSubmissionForBareTest && adobeAnimateSprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest + 1 && adobeAnimateGpuClockState.Enabled && adobeAnimateGpuClockState.FramesPerSecond < 0f;
		adobeAnimateSprite.pause = true;
		adobeAnimateSprite.ClearRenderSubmissionForBareTest();
		ulong playbackRevisionForBareTest2 = adobeAnimateSprite.PlaybackRevisionForBareTest;
		adobeAnimateSprite.playBack = false;
		bool flag = adobeAnimateSprite.NeedsRenderSubmissionForBareTest && adobeAnimateSprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest2 + 1 && !adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled;
		adobeAnimateSprite.pause = false;
		AdobeAnimateGpuClockState adobeAnimateGpuClockState2 = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		bool flag2 = adobeAnimateGpuClockState2.Enabled && adobeAnimateGpuClockState2.FramesPerSecond > 0f;
		adobeAnimateSprite.ClearRenderSubmissionForBareTest();
		ulong playbackRevisionForBareTest3 = adobeAnimateSprite.PlaybackRevisionForBareTest;
		adobeAnimateSprite.timeScale = 2.0;
		AdobeAnimateGpuClockState adobeAnimateGpuClockState3 = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		bool flag3 = adobeAnimateSprite.NeedsRenderSubmissionForBareTest && adobeAnimateSprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest3 + 1 && adobeAnimateGpuClockState3.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState3.FramesPerSecond, 60f);
		adobeAnimateSprite.ClearRenderSubmissionForBareTest();
		ulong playbackRevisionForBareTest4 = adobeAnimateSprite.PlaybackRevisionForBareTest;
		adobeAnimateSprite.timeScale = 1.0;
		AdobeAnimateGpuClockState adobeAnimateGpuClockState4 = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		bool flag4 = adobeAnimateSprite.NeedsRenderSubmissionForBareTest && adobeAnimateSprite.PlaybackRevisionForBareTest == playbackRevisionForBareTest4 + 1 && adobeAnimateGpuClockState4.Enabled && Mathf.IsEqualApprox(adobeAnimateGpuClockState4.FramesPerSecond, 30f);
		adobeAnimateSprite.ClearRenderSubmissionForBareTest();
		adobeAnimateSprite.timeScale = 0.0;
		adobeAnimateSprite2.ApplyRuntimeParentStateForBareTest(adobeAnimateSprite);
		bool needsRenderSubmissionForBareTest = adobeAnimateSprite.NeedsRenderSubmissionForBareTest;
		bool flag5 = !adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled && !adobeAnimateSprite2.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled;
		adobeAnimateSprite.timeScale = 1.0;
		adobeAnimateSprite2.ApplyRuntimeParentStateForBareTest(adobeAnimateSprite);
		bool flag6 = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled && adobeAnimateSprite2.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled;
		adobeAnimateSprite.SetPlaybackBlocked(blocked: true);
		adobeAnimateSprite2.ApplyRuntimeParentStateForBareTest(adobeAnimateSprite);
		bool flag7 = !adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled && !adobeAnimateSprite2.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f).Enabled;
		adobeAnimateSprite.SetPlaybackBlocked(blocked: false);
		adobeAnimateSprite2.ApplyRuntimeParentStateForBareTest(adobeAnimateSprite);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState5 = adobeAnimateSprite.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		AdobeAnimateGpuClockState adobeAnimateGpuClockState6 = adobeAnimateSprite2.BuildRuntimeGpuClockStateForBareTest(4.25f, 73.5f);
		if (num && flag && flag2 && flag3 && flag4 && needsRenderSubmissionForBareTest && flag5 && flag6 && flag7 && adobeAnimateGpuClockState5.Enabled && adobeAnimateGpuClockState6.Enabled)
		{
			return Mathf.IsEqualApprox(adobeAnimateGpuClockState5.StartTime, adobeAnimateGpuClockState6.StartTime);
		}
		return false;
	}
}
