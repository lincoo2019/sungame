public readonly struct OptimizationBatchResult(double meanMilliseconds, double p50Milliseconds, double p95Milliseconds, double p99Milliseconds, double maximumMilliseconds, long allocatedBytes, int sampleCount, int overBudgetSamples, int maximumSampleIndex, int gen0Collections, int gen1Collections, int gen2Collections)
{
	public double MeanMilliseconds { get; } = meanMilliseconds;

	public double P50Milliseconds { get; } = p50Milliseconds;

	public double P95Milliseconds { get; } = p95Milliseconds;

	public double P99Milliseconds { get; } = p99Milliseconds;

	public double MaximumMilliseconds { get; } = maximumMilliseconds;

	public long AllocatedBytes { get; } = allocatedBytes;

	public int SampleCount { get; } = sampleCount;

	public int OverBudgetSamples { get; } = overBudgetSamples;

	public int MaximumSampleIndex { get; } = maximumSampleIndex;

	public int Gen0Collections { get; } = gen0Collections;

	public int Gen1Collections { get; } = gen1Collections;

	public int Gen2Collections { get; } = gen2Collections;
}
