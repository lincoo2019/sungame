using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

public sealed class OptimizationBatchSampler
{
	private static readonly double MillisecondsPerTick = 1000.0 / (double)Stopwatch.Frequency;

	private readonly double[] _elapsedMilliseconds;

	private int _sampleCount;

	private long _allocatedBytesBefore;

	private int _gen0Before;

	private int _gen1Before;

	private int _gen2Before;

	private bool _measurementActive;

	public int SampleCapacity => _elapsedMilliseconds.Length;

	public int SampleCount => _sampleCount;

	public OptimizationBatchSampler(int sampleCapacity)
	{
		if (sampleCapacity <= 0)
		{
			throw new ArgumentOutOfRangeException("sampleCapacity");
		}
		_elapsedMilliseconds = new double[sampleCapacity];
	}

	public double GetSampleMilliseconds(int index)
	{
		if ((uint)index >= (uint)_sampleCount)
		{
			throw new ArgumentOutOfRangeException("index");
		}
		return _elapsedMilliseconds[index];
	}

	public static void PrepareForWarmup()
	{
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
	}

	public void BeginMeasurement()
	{
		if (_measurementActive)
		{
			throw new InvalidOperationException("A batch measurement is already active.");
		}
		_sampleCount = 0;
		_allocatedBytesBefore = GC.GetAllocatedBytesForCurrentThread();
		_gen0Before = GC.CollectionCount(0);
		_gen1Before = GC.CollectionCount(1);
		_gen2Before = GC.CollectionCount(2);
		_measurementActive = true;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long BeginSample()
	{
		return Stopwatch.GetTimestamp();
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static long EndWarmupSample(long startTicks)
	{
		return Math.Max(0L, Stopwatch.GetTimestamp() - startTicks);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public void EndSample(long startTicks)
	{
		if (!_measurementActive)
		{
			throw new InvalidOperationException("BeginMeasurement must run before sampling.");
		}
		if ((uint)_sampleCount >= (uint)_elapsedMilliseconds.Length)
		{
			throw new InvalidOperationException("The batch sample buffer is full.");
		}
		long val = Stopwatch.GetTimestamp() - startTicks;
		_elapsedMilliseconds[_sampleCount++] = (double)Math.Max(0L, val) * MillisecondsPerTick;
	}

	public OptimizationBatchResult Complete(double budgetMilliseconds = 0.2)
	{
		if (!_measurementActive)
		{
			throw new InvalidOperationException("There is no active batch measurement.");
		}
		long val = GC.GetAllocatedBytesForCurrentThread() - _allocatedBytesBefore;
		int val2 = GC.CollectionCount(0) - _gen0Before;
		int val3 = GC.CollectionCount(1) - _gen1Before;
		int val4 = GC.CollectionCount(2) - _gen2Before;
		_measurementActive = false;
		int num = 0;
		int num2 = -1;
		double num3 = 0.0;
		for (int i = 0; i < _sampleCount; i++)
		{
			double num4 = _elapsedMilliseconds[i];
			if (num4 >= budgetMilliseconds)
			{
				num++;
			}
			if (num2 < 0 || num4 > num3)
			{
				num3 = num4;
				num2 = i;
			}
		}
		return new OptimizationBatchResult(BenchmarkStatistics.Average(_elapsedMilliseconds, _sampleCount), BenchmarkStatistics.Percentile(_elapsedMilliseconds, _sampleCount, 50.0), BenchmarkStatistics.Percentile(_elapsedMilliseconds, _sampleCount, 95.0), BenchmarkStatistics.Percentile(_elapsedMilliseconds, _sampleCount, 99.0), BenchmarkStatistics.Maximum(_elapsedMilliseconds, _sampleCount), Math.Max(0L, val), _sampleCount, num, num2, Math.Max(0, val2), Math.Max(0, val3), Math.Max(0, val4));
	}
}
