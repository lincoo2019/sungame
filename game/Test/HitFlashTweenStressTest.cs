using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Godot;

public partial class HitFlashTweenStressTest : Node2D
{
	private const string ZombieScenePath = "res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn";

	private readonly List<TowerDefenseCharacter> _targets = new List<TowerDefenseCharacter>();

	private string _runtimeProfile = "unknown";

	private double[] _frameSeconds;

	private bool _setupComplete;

	private bool _measuring;

	private long _phaseStartTimestamp;

	private long _lastFrameTimestamp;

	private double _nextFlashSeconds;

	private int _frameCount;

	private int _flashBursts;

	private long _flashRequests;

	private long _maxFlashBurstTicks;

	private long _totalFlashBurstTicks;

	private int _maxProcessedTweens;

	private int _maxBatchedFlashComponents;

	private int _maxGpuFlashComponents;

	private int _physicsProcessingTargets;

	private bool _observedBrightStrength;

	private bool _observedCompletedFlashReset;

	private long _threadAllocatedBefore;

	private long _totalAllocatedBefore;

	private int _gen0Before;

	private int _gen1Before;

	private int _gen2Before;

	private int _originalMaxFps;

	[Export(PropertyHint.None, "")]
	public int TargetCount { get; set; } = 1000;

	[Export(PropertyHint.None, "")]
	public double WarmupSeconds { get; set; } = 2.0;

	[Export(PropertyHint.None, "")]
	public double MeasureSeconds { get; set; } = 5.0;

	[Export(PropertyHint.None, "")]
	public double FlashIntervalSeconds { get; set; } = 0.25;

	[Export(PropertyHint.None, "")]
	public float FlashDurationSeconds { get; set; } = 0.2f;

	[Export(PropertyHint.None, "")]
	public bool EnableFlash { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public bool VisibleTargets { get; set; } = true;

	[Export(PropertyHint.None, "")]
	public float TargetScale { get; set; } = 0.35f;

	[Export(PropertyHint.None, "")]
	public int GridColumns { get; set; } = 40;

	[Export(PropertyHint.None, "")]
	public int MaxFrameSamples { get; set; } = 100000;

	[Export(PropertyHint.None, "")]
	public int BenchmarkMaxFps { get; set; } = 360;

	[Export(PropertyHint.None, "")]
	public int ProcessedTweenLimit { get; set; }

	[Export(PropertyHint.None, "")]
	public bool RequireGpuFlashEnvelope { get; set; } = true;

	public override void _Ready()
	{
		SetProcess(enable: false);
		ApplyCommandLineArguments();
		_originalMaxFps = Engine.MaxFps;
		Callable.From(Setup).CallDeferred();
	}

	public override void _Process(double delta)
	{
		if (!_setupComplete)
		{
			return;
		}
		if (Engine.MaxFps != BenchmarkMaxFps)
		{
			Engine.MaxFps = BenchmarkMaxFps;
		}
		long timestamp = Stopwatch.GetTimestamp();
		double elapsedSeconds = GetElapsedSeconds(_phaseStartTimestamp, timestamp);
		ObserveFlashState();
		if (!_measuring)
		{
			TryTriggerFlash(elapsedSeconds);
			if (elapsedSeconds >= WarmupSeconds)
			{
				BeginMeasurement();
			}
			return;
		}
		if (_frameCount >= _frameSeconds.Length)
		{
			Finish(capacitySafe: false, "frame-sample-capacity");
			return;
		}
		double elapsedSeconds2 = GetElapsedSeconds(_lastFrameTimestamp, timestamp);
		_lastFrameTimestamp = timestamp;
		if (elapsedSeconds2 > 0.0 && double.IsFinite(elapsedSeconds2))
		{
			_frameSeconds[_frameCount++] = elapsedSeconds2;
		}
		TryTriggerFlash(elapsedSeconds);
		if (elapsedSeconds >= MeasureSeconds)
		{
			Finish(capacitySafe: true, string.Empty);
		}
	}

	private void Setup()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Chapter1/Normal/Scene/Base/TowerDefenseZombieNormal.tscn");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			FailSetup("zombie-scene-load");
			return;
		}
		int num = Math.Max(1, GridColumns);
		for (int i = 0; i < TargetCount; i++)
		{
			if (!(packedScene.Instantiate(PackedScene.GenEditState.Disabled) is TowerDefenseCharacter towerDefenseCharacter))
			{
				FailSetup($"target-instantiate-{i}");
				return;
			}
			towerDefenseCharacter.inGame = false;
			towerDefenseCharacter.editorPreviewMode = true;
			towerDefenseCharacter.Visible = VisibleTargets;
			towerDefenseCharacter.Scale = Vector2.One * Math.Max(0.01f, TargetScale);
			towerDefenseCharacter.Position = new Vector2(20f + (float)(i % num) * 30f, 30f + (float)(i / num) * 36f);
			AddChild(towerDefenseCharacter, forceReadableName: false, InternalMode.Disabled);
			if (towerDefenseCharacter.IsPhysicsProcessing())
			{
				_physicsProcessingTargets++;
			}
			if (towerDefenseCharacter.hitFlashComponent == null || towerDefenseCharacter.hitFlashComponent.IsReleased)
			{
				FailSetup($"hit-flash-runtime-{i}");
				return;
			}
			towerDefenseCharacter.hitFlashComponent.restartBrightOnHit = false;
			_targets.Add(towerDefenseCharacter);
		}
		_frameSeconds = new double[Math.Max(256, MaxFrameSamples)];
		Engine.MaxFps = BenchmarkMaxFps;
		_setupComplete = true;
		_phaseStartTimestamp = Stopwatch.GetTimestamp();
		_lastFrameTimestamp = _phaseStartTimestamp;
		_nextFlashSeconds = 0.0;
		SetProcess(enable: true);
		GD.Print($"[HitFlashTweenStressReady] runtimeProfile={_runtimeProfile} targets={_targets.Count}/{TargetCount} flashEnabled={EnableFlash} physicsProcessingTargets={_physicsProcessingTargets} visibleTargets={VisibleTargets} warmupSeconds={WarmupSeconds:F3} measureSeconds={MeasureSeconds:F3} intervalSeconds={FlashIntervalSeconds:F3} durationSeconds={FlashDurationSeconds:F3} maxFps={BenchmarkMaxFps}");
	}

	private void BeginMeasurement()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		_frameCount = 0;
		_flashBursts = 0;
		_flashRequests = 0L;
		_maxFlashBurstTicks = 0L;
		_totalFlashBurstTicks = 0L;
		_maxProcessedTweens = 0;
		_maxBatchedFlashComponents = 0;
		_maxGpuFlashComponents = 0;
		_observedBrightStrength = false;
		_observedCompletedFlashReset = false;
		_threadAllocatedBefore = GC.GetAllocatedBytesForCurrentThread();
		_totalAllocatedBefore = GC.GetTotalAllocatedBytes();
		_gen0Before = GC.CollectionCount(0);
		_gen1Before = GC.CollectionCount(1);
		_gen2Before = GC.CollectionCount(2);
		_measuring = true;
		_phaseStartTimestamp = Stopwatch.GetTimestamp();
		_lastFrameTimestamp = _phaseStartTimestamp;
		_nextFlashSeconds = 0.0;
	}

	private void TryTriggerFlash(double phaseElapsed)
	{
		if (!EnableFlash || phaseElapsed < _nextFlashSeconds)
		{
			return;
		}
		_nextFlashSeconds = phaseElapsed + Math.Max(0.001, FlashIntervalSeconds);
		long timestamp = Stopwatch.GetTimestamp();
		int num = 0;
		for (int i = 0; i < _targets.Count; i++)
		{
			_targets[i].Bright(0.5, 0.0, 0.5, 0.0, Math.Max(0.001f, FlashDurationSeconds));
			if (_targets[i].hitFlashComponent.IsGpuFlashEnvelopeActive)
			{
				num++;
			}
		}
		_flashBursts++;
		_flashRequests += _targets.Count;
		long num2 = Stopwatch.GetTimestamp() - timestamp;
		_maxFlashBurstTicks = Math.Max(_maxFlashBurstTicks, num2);
		_totalFlashBurstTicks += num2;
		_maxProcessedTweens = Math.Max(_maxProcessedTweens, GetTree().GetProcessedTweens().Count);
		_maxBatchedFlashComponents = Math.Max(_maxBatchedFlashComponents, TowerDefenseHitFlashBatch.ActiveCount);
		_maxGpuFlashComponents = Math.Max(_maxGpuFlashComponents, num);
	}

	private void Finish(bool capacitySafe, string failureReason)
	{
		SetProcess(enable: false);
		long num = GC.GetAllocatedBytesForCurrentThread() - _threadAllocatedBefore;
		long num2 = GC.GetTotalAllocatedBytes() - _totalAllocatedBefore;
		int value = GC.CollectionCount(0) - _gen0Before;
		int value2 = GC.CollectionCount(1) - _gen1Before;
		int value3 = GC.CollectionCount(2) - _gen2Before;
		Array.Sort(_frameSeconds, 0, _frameCount);
		double num3 = 0.0;
		for (int i = 0; i < _frameCount; i++)
		{
			num3 += _frameSeconds[i];
		}
		double num4 = ((num3 > 0.0) ? ((double)_frameCount / num3) : 0.0);
		double num5 = CalculateOnePercentLowFps();
		double value4 = Percentile(50.0) * 1000.0;
		double value5 = Percentile(95.0) * 1000.0;
		double num6 = Percentile(99.0) * 1000.0;
		double value6 = ((_frameCount > 0) ? (_frameSeconds[_frameCount - 1] * 1000.0) : 0.0);
		bool flag = capacitySafe && _targets.Count == TargetCount && _physicsProcessingTargets == 0 && _frameCount >= 30 && double.IsFinite(num4) && double.IsFinite(num5) && double.IsFinite(num6) && num >= 0 && num2 >= 0 && _maxProcessedTweens <= ProcessedTweenLimit && (!EnableFlash || ((!RequireGpuFlashEnvelope || _maxGpuFlashComponents == TargetCount) && _observedBrightStrength && _observedCompletedFlashReset));
		GD.Print($"[HitFlashTweenStressResult] runtimeProfile={_runtimeProfile} targets={_targets.Count}/{TargetCount} flashEnabled={EnableFlash} physicsProcessingTargets={_physicsProcessingTargets} visibleTargets={VisibleTargets} maxFps={BenchmarkMaxFps} frames={_frameCount} averageFps={num4:F3} onePercentLowFps={num5:F3} frameP50Ms={value4:F6} frameP95Ms={value5:F6} frameP99Ms={num6:F6} frameMaxMs={value6:F6} flashBursts={_flashBursts} flashRequests={_flashRequests} averageFlashBurstMs={GetAverageFlashBurstMilliseconds():F6} maxFlashBurstMs={GetMilliseconds(_maxFlashBurstTicks):F6} maxProcessedTweens={_maxProcessedTweens} processedTweenLimit={ProcessedTweenLimit} maxBatchedFlashComponents={_maxBatchedFlashComponents} maxGpuFlashComponents={_maxGpuFlashComponents} requireGpuFlashEnvelope={RequireGpuFlashEnvelope} observedBrightStrength={_observedBrightStrength} observedCompletedFlashReset={_observedCompletedFlashReset} threadAllocatedBytes={num} totalAllocatedBytes={num2} allocatedBytesPerFlashRequest={GetAllocatedBytesPerRequest(num):F3} gen0={value} gen1={value2} gen2={value3} failureReason={failureReason} passed={flag}");
		GetTree().Quit((!flag) ? 1 : 0);
	}

	private void ObserveFlashState()
	{
		if (!EnableFlash || _targets.Count == 0)
		{
			return;
		}
		HitFlashComponent hitFlashComponent = _targets[0].hitFlashComponent;
		if (hitFlashComponent != null && !hitFlashComponent.IsReleased)
		{
			if (hitFlashComponent.CurrentBrightStrength > 0.0001f)
			{
				_observedBrightStrength = true;
			}
			if (_observedBrightStrength && !hitFlashComponent.IsVisualFlashActive && Mathf.IsZeroApprox(hitFlashComponent.CurrentBrightStrength))
			{
				_observedCompletedFlashReset = true;
			}
		}
	}

	private double CalculateOnePercentLowFps()
	{
		if (_frameCount <= 0)
		{
			return 0.0;
		}
		int num = Math.Max(1, (int)Math.Ceiling((double)_frameCount * 0.01));
		double num2 = 0.0;
		for (int i = _frameCount - num; i < _frameCount; i++)
		{
			num2 += _frameSeconds[i];
		}
		if (!(num2 > 0.0))
		{
			return 0.0;
		}
		return (double)num / num2;
	}

	private double Percentile(double percentile)
	{
		if (_frameCount <= 0)
		{
			return 0.0;
		}
		double num = Math.Clamp(percentile, 0.0, 100.0) / 100.0 * (double)(_frameCount - 1);
		int num2 = (int)Math.Floor(num);
		int num3 = (int)Math.Ceiling(num);
		if (num2 == num3)
		{
			return _frameSeconds[num2];
		}
		double num4 = num - (double)num2;
		return _frameSeconds[num2] + (_frameSeconds[num3] - _frameSeconds[num2]) * num4;
	}

	private double GetAllocatedBytesPerRequest(long allocatedBytes)
	{
		if (_flashRequests <= 0)
		{
			return 0.0;
		}
		return (double)allocatedBytes / (double)_flashRequests;
	}

	private double GetAverageFlashBurstMilliseconds()
	{
		if (_flashBursts <= 0)
		{
			return 0.0;
		}
		return GetMilliseconds(_totalFlashBurstTicks) / (double)_flashBursts;
	}

	private static double GetMilliseconds(long ticks)
	{
		return (double)ticks * 1000.0 / (double)Stopwatch.Frequency;
	}

	private static double GetElapsedSeconds(long start, long end)
	{
		return (double)(end - start) / (double)Stopwatch.Frequency;
	}

	private void FailSetup(string reason)
	{
		GD.PrintErr($"[HitFlashTweenStressResult] runtimeProfile={_runtimeProfile} targets={_targets.Count}/{TargetCount} failureReason={reason} passed=False");
		GetTree().Quit(2);
	}

	private void ApplyCommandLineArguments()
	{
		string[] cmdlineUserArgs = OS.GetCmdlineUserArgs();
		foreach (string argument in cmdlineUserArgs)
		{
			double value2;
			double value3;
			double value4;
			double value5;
			bool value6;
			bool value7;
			int value8;
			bool value9;
			int value10;
			string value11;
			if (TryReadInt(argument, "--hit-flash-targets=", out var value))
			{
				TargetCount = Math.Max(1, value);
			}
			else if (TryReadDouble(argument, "--hit-flash-warmup=", out value2))
			{
				WarmupSeconds = Math.Max(0.0, value2);
			}
			else if (TryReadDouble(argument, "--hit-flash-measure=", out value3))
			{
				MeasureSeconds = Math.Max(0.25, value3);
			}
			else if (TryReadDouble(argument, "--hit-flash-interval=", out value4))
			{
				FlashIntervalSeconds = Math.Max(0.001, value4);
			}
			else if (TryReadDouble(argument, "--hit-flash-duration=", out value5))
			{
				FlashDurationSeconds = (float)Math.Max(0.001, value5);
			}
			else if (TryReadBool(argument, "--hit-flash-enabled=", out value6))
			{
				EnableFlash = value6;
			}
			else if (TryReadBool(argument, "--hit-flash-visible=", out value7))
			{
				VisibleTargets = value7;
			}
			else if (TryReadInt(argument, "--hit-flash-max-fps=", out value8))
			{
				BenchmarkMaxFps = Math.Max(0, value8);
			}
			else if (TryReadBool(argument, "--hit-flash-require-gpu-envelope=", out value9))
			{
				RequireGpuFlashEnvelope = value9;
			}
			else if (TryReadInt(argument, "--hit-flash-processed-tween-limit=", out value10))
			{
				ProcessedTweenLimit = Math.Max(0, value10);
			}
			else if (TryReadString(argument, "--runtime-profile=", out value11))
			{
				_runtimeProfile = value11;
			}
		}
	}

	public override void _ExitTree()
	{
		Engine.MaxFps = _originalMaxFps;
	}

	private static bool TryReadInt(string argument, string prefix, out int value)
	{
		value = 0;
		if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return int.TryParse(argument.Substring(length, argument.Length - length), out value);
		}
		return false;
	}

	private static bool TryReadDouble(string argument, string prefix, out double value)
	{
		value = 0.0;
		if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return double.TryParse(argument.Substring(length, argument.Length - length), NumberStyles.Float, CultureInfo.InvariantCulture, out value);
		}
		return false;
	}

	private static bool TryReadBool(string argument, string prefix, out bool value)
	{
		value = false;
		if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			int length = prefix.Length;
			return bool.TryParse(argument.Substring(length, argument.Length - length), out value);
		}
		return false;
	}

	private static bool TryReadString(string argument, string prefix, out string value)
	{
		value = string.Empty;
		if (!argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		int length = prefix.Length;
		value = argument.Substring(length, argument.Length - length).Trim();
		return value.Length > 0;
	}
}
