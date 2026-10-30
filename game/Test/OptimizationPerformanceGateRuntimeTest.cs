using Godot;

public sealed partial class OptimizationPerformanceGateRuntimeTest : Node
{
	private const int MeasuredSamples = 1200;

	private readonly int[] _fixture = new int[1000];

	private long _sink;

	public override void _Ready()
	{
		base.ProcessMode = ProcessModeEnum.Always;
		Callable.From(Run).CallDeferred();
	}

	private void Run()
	{
		OptimizationResultIdentity identity = new OptimizationResultIdentity("measurement-contract", OptimizationWorkloadKind.HeadlessContract, "none", "res://Test/OptimizationPerformanceGateRuntimeTest.tscn", "none", "none", "none", "none", "noop-batch", "steady", OptimizationScheduleKind.BackToBack, "headless", 60, 240);
		OptimizationBatchSampler.PrepareForWarmup();
		long num = 0L;
		for (int i = 0; i < 240; i++)
		{
			long startTicks = OptimizationBatchSampler.BeginSample();
			ExecuteBatch();
			num += OptimizationBatchSampler.EndWarmupSample(startTicks);
		}
		long sink = _sink;
		OptimizationBatchSampler optimizationBatchSampler = new OptimizationBatchSampler(1200);
		optimizationBatchSampler.BeginMeasurement();
		for (int j = 0; j < 600; j++)
		{
			long startTicks2 = OptimizationBatchSampler.BeginSample();
			ExecuteBatch();
			optimizationBatchSampler.EndSample(startTicks2);
		}
		for (int k = 600; k < 1200; k++)
		{
			long startTicks3 = OptimizationBatchSampler.BeginSample();
			ExecuteBatch();
			optimizationBatchSampler.EndSample(startTicks3);
		}
		OptimizationBatchResult result = optimizationBatchSampler.Complete();
		long num2 = 499500L;
		bool functionalPassed = _sink - sink == num2 * 1200 && result.SampleCount == 1200 && num >= 0 && VerifyRejectedGateContracts(in identity);
		bool flag = OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed, 1000, 240, 1000, 1000, in identity);
		GD.Print(OptimizationPerformanceGate.FormatBareResult(in identity, in result, functionalPassed, flag, 1000, 240, 1000, 1000));
		GetTree().Quit((!flag) ? 2 : 0);
	}

	private void ExecuteBatch()
	{
		long num = 0L;
		for (int i = 0; i < _fixture.Length; i++)
		{
			num += _fixture[i] + i;
		}
		_sink += num;
	}

	private static bool VerifyRejectedGateContracts(in OptimizationResultIdentity identity)
	{
		OptimizationBatchResult result = CreateSyntheticResult(0.199, 0L, 600);
		OptimizationBatchResult result2 = CreateSyntheticResult(0.2, 0L, 600);
		OptimizationBatchResult result3 = CreateSyntheticResult(0.1, 1L, 600);
		OptimizationBatchResult result4 = CreateSyntheticResult(0.1, 0L, 599);
		if (OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: true, 1000, 240, 1000, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: true, 999, 240, 1000, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: true, 1000, 240, 999, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: true, 1000, 240, 1000, 999, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: true, 1000, 239, 1000, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result, functionalPassed: false, 1000, 240, 1000, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result2, functionalPassed: true, 1000, 240, 1000, 1000, in identity) && !OptimizationPerformanceGate.IsBareResultPassed(in result3, functionalPassed: true, 1000, 240, 1000, 1000, in identity))
		{
			return !OptimizationPerformanceGate.IsBareResultPassed(in result4, functionalPassed: true, 1000, 240, 1000, 1000, in identity);
		}
		return false;
	}

	private static OptimizationBatchResult CreateSyntheticResult(double p99Milliseconds, long allocatedBytes, int sampleCount)
	{
		return new OptimizationBatchResult(0.1, 0.1, 0.1, p99Milliseconds, p99Milliseconds, allocatedBytes, sampleCount, (p99Milliseconds >= 0.2) ? 1 : 0, 0, 0, 0, 0);
	}
}
