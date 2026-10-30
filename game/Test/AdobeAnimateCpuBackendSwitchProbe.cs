using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

public partial class AdobeAnimateCpuBackendSwitchProbe : Node
{
	private readonly record struct AnimationStateSnapshot(string Clip, int FrameIndex, double ElapsedTimer)
	{
		public double Clock => (double)FrameIndex + ElapsedTimer;
	}

	private const string ResultMarker = "ADOBE_ANIMATE_CPU_BACKEND_SWITCH_RESULT";

	private const string AnimationScenePath = "res://Asset/Anime/Character/Zombie/Challenge/JacksonX/ZombieJacksonX.tscn";

	private readonly List<string> _failures = new List<string>();

	private AdobeAnimateRenderBackend _originalBackend;

	private AdobeAnimateSprite _root;

	private AdobeAnimateSprite _child;

	private int _checks;

	private int _cpuEventCount;

	public override async void _Ready()
	{
		try
		{
			if (Global.Instance == null)
			{
				throw new InvalidOperationException("Global autoload is unavailable.");
			}
			_originalBackend = Global.Instance.adobeAnimateRenderBackend;
			await RunProbe();
		}
		catch (Exception ex)
		{
			_failures.Add(ex.ToString());
		}
		finally
		{
			await Cleanup();
		}
		bool flag = _failures.Count == 0 && _checks == 10;
		GD.Print($"{"ADOBE_ANIMATE_CPU_BACKEND_SWITCH_RESULT"} passed={flag} checks={_checks} failures={_failures.Count}");
		foreach (string failure in _failures)
		{
			GD.PrintErr("ADOBE_ANIMATE_CPU_BACKEND_SWITCH_RESULT failure=" + failure);
		}
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private async Task RunProbe()
	{
		PackedScene packedScene = GD.Load<PackedScene>("res://Asset/Anime/Character/Zombie/Challenge/JacksonX/ZombieJacksonX.tscn");
		if (!GodotObject.IsInstanceValid(packedScene))
		{
			throw new InvalidOperationException("JacksonX animation scene must load.");
		}
		_root = packedScene.Instantiate<AdobeAnimateSprite>(PackedScene.GenEditState.Disabled);
		_root.Name = "BackendSwitchRoot";
		_root.Position = new Vector2(320f, 240f);
		AddChild(_root, forceReadableName: false, InternalMode.Disabled);
		_child = _root.GetNodeOrNull<AdobeAnimateSprite>("ZombieDuckytube");
		if (!GodotObject.IsInstanceValid(_child))
		{
			throw new InvalidOperationException("JacksonX nested child animation must load.");
		}
		_root.SetAnimation("Walk");
		_root.OnAnimeEvent += OnCpuAnimationEvent;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		await WaitProcessFrames(5);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats = AdobeAnimateRenderManager.GetAggregateRenderStats();
		Check(Global.Instance.adobeAnimateRenderBackend == AdobeAnimateRenderBackend.GpuCrowd && aggregateRenderStats.CpuRoots == 0 && aggregateRenderStats.CrowdRoots + aggregateRenderStats.FallbackRoots > 0, "GPU Crowd must publish visible output without CPU Pose roots.");
		AnimationStateSnapshot beforeCpu = CaptureState();
		ProcessModeEnum processMode = _root.ProcessMode;
		_root.ProcessMode = ProcessModeEnum.Disabled;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
		await WaitProcessFrames(4);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats2 = AdobeAnimateRenderManager.GetAggregateRenderStats();
		Check(Global.Instance.adobeAnimateRenderBackend == AdobeAnimateRenderBackend.CpuPose && aggregateRenderStats2.CpuRoots == 1 && aggregateRenderStats2.CrowdRoots == aggregateRenderStats2.CpuRoots && aggregateRenderStats2.FallbackRoots == 0, "CPU Pose must publish captured CPU state through the shared render graph.");
		Check(aggregateRenderStats2.FallbackRuns == 0 && aggregateRenderStats2.CpuFallbackRoots == 0 && aggregateRenderStats2.CpuValidationFailures == 0, "CPU Pose must avoid full-pose fallback while the switch boundary is frozen.");
		Check(CaptureState() == beforeCpu, "GPU-to-CPU switching must preserve the exact animation state.");
		Vector2 childBeforeCpuPlayback = _child.Position;
		_root.ProcessMode = processMode;
		await WaitProcessSeconds(0.15);
		Check(CurrentClock() > beforeCpu.Clock && _root.UsesRuntimeGpuClockInterpolation, "CPU Pose playback must continue with visual interpolation after the switch boundary.");
		Check(_child.Position.DistanceSquaredTo(childBeforeCpuPlayback) <= 0.0001f, "CPU Pose must leave visual-only nested attachment transforms on the shared graph.");
		_cpuEventCount = 0;
		_root.SetAnimation("PointUp");
		await WaitProcessSeconds(0.85);
		Check(_cpuEventCount > 0, "CPU Pose must keep authored animation events on the CPU while visual interpolation is retained.");
		AnimationStateSnapshot beforeGpu = CaptureState();
		_root.ProcessMode = ProcessModeEnum.Disabled;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		await WaitProcessFrames(4);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats3 = AdobeAnimateRenderManager.GetAggregateRenderStats();
		Check(aggregateRenderStats3.CpuRoots == 0 && aggregateRenderStats3.CrowdRoots + aggregateRenderStats3.FallbackRoots > 0 && CaptureState() == beforeGpu, "CPU-to-GPU switching must retire CPU Pose output without advancing animation state.");
		_root.ProcessMode = processMode;
		await WaitProcessSeconds(0.15);
		Check(CaptureState() != beforeGpu, "GPU Crowd playback must continue after returning from CPU Pose.");
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.GpuCrowd;
		Global.Instance.adobeAnimateRenderBackend = AdobeAnimateRenderBackend.CpuPose;
		await WaitProcessFrames(4);
		AdobeAnimateCrowdAggregateStats aggregateRenderStats4 = AdobeAnimateRenderManager.GetAggregateRenderStats();
		Check(aggregateRenderStats4.CpuRoots == 1 && aggregateRenderStats4.CrowdRoots == aggregateRenderStats4.CpuRoots && aggregateRenderStats4.FallbackRoots == 0, "A rapid switch must apply the final CPU Pose value.");
	}

	private async Task Cleanup()
	{
		if (Global.Instance != null)
		{
			Global.Instance.adobeAnimateRenderBackend = _originalBackend;
		}
		if (GodotObject.IsInstanceValid(_root))
		{
			_root.OnAnimeEvent -= OnCpuAnimationEvent;
		}
		if (GodotObject.IsInstanceValid(_root) && !_root.IsQueuedForDeletion())
		{
			_root.QueueFree();
		}
		await WaitProcessFrames(2);
	}

	private AnimationStateSnapshot CaptureState()
	{
		return new AnimationStateSnapshot(_root.clip, _root.frameIndex, _root.elapsedTimer);
	}

	private double CurrentClock()
	{
		return (double)_root.frameIndex + _root.elapsedTimer;
	}

	private void OnCpuAnimationEvent(string command, Variant argument)
	{
		if (command == "spawn")
		{
			_cpuEventCount++;
		}
	}

	private async Task WaitProcessFrames(int count)
	{
		for (int i = 0; i < count; i++)
		{
			await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}

	private async Task WaitProcessSeconds(double seconds)
	{
		await ToSignal(GetTree().CreateTimer(seconds), SceneTreeTimer.SignalName.Timeout);
	}

	private void Check(bool condition, string message)
	{
		_checks++;
		if (!condition)
		{
			_failures.Add(message);
		}
	}
}
