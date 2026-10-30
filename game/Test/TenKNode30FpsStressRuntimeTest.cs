using System;
using System.Diagnostics;
using Godot;

public sealed partial class TenKNode30FpsStressRuntimeTest : Node
{
	private sealed partial class StressNode : Node
	{
		public static long ProcessCalls;

		private double _elapsed;

		public override void _Process(double delta)
		{
			_elapsed += delta;
			if (_elapsed >= 0.0)
			{
				ProcessCalls++;
			}
		}
	}

	private const int InstanceCount = 10000;

	private const int WarmupFrames = 30;

	private const int MeasuredFrames = 120;

	private Node _stressParent;

	private int _previousMaxFps;

	private bool _maxFpsOverridden;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		OS.LowProcessorUsageMode = false;
		Callable.From(Run).CallDeferred();
	}

	private async void Run()
	{
		bool passed = false;
		long processCalls = 0L;
		double actualFps = 0.0;
		double meanFrameMs = 0.0;
		try
		{
			_previousMaxFps = Engine.MaxFps;
			Engine.MaxFps = 30;
			_maxFpsOverridden = true;
			_stressParent = new Node
			{
				Name = "TenKStressNodes"
			};
			AddChild(_stressParent, forceReadableName: false, InternalMode.Disabled);
			for (int i = 0; i < 10000; i++)
			{
				_stressParent.AddChild(new StressNode(), forceReadableName: false, InternalMode.Disabled);
			}
			if (_stressParent.GetChildCount() != 10000)
			{
				throw new InvalidOperationException($"Expected {10000} real Nodes, got {_stressParent.GetChildCount()}.");
			}
			for (int frame = 0; frame < 30; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			StressNode.ProcessCalls = 0L;
			long startTicks = Stopwatch.GetTimestamp();
			for (int frame = 0; frame < 120; frame++)
			{
				await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
			}
			double num = (double)(Stopwatch.GetTimestamp() - startTicks) * 1000.0 / (double)Stopwatch.Frequency;
			actualFps = ((num > 0.0) ? (120000.0 / num) : 0.0);
			meanFrameMs = num / 120.0;
			processCalls = StressNode.ProcessCalls;
			long num2 = 1200000L;
			passed = processCalls == num2 && actualFps >= 28.5 && actualFps <= 32.0;
		}
		catch (Exception ex)
		{
			GD.PushError(ex.ToString());
		}
		finally
		{
			if (_maxFpsOverridden)
			{
				Engine.MaxFps = _previousMaxFps;
			}
			GD.Print($"TEN_K_NODE_30FPS_STRESS passed={passed} instances={10000} warmupFrames={30} measuredFrames={120} processCalls={processCalls} expectedCalls={1200000L} actualFps={actualFps:F3} meanFrameMs={meanFrameMs:F6} maxFps={Engine.MaxFps}");
			GetTree().Quit((!passed) ? 2 : 0);
		}
	}
}
