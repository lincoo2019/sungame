using System;

public static class TowerDefenseAllocationTelemetry
{
	private static readonly long[] AllocatedBytes = new long[77];

	private static readonly long[] SampleCounts = new long[77];

	public static bool Enabled { get; set; }

	public static long Begin()
	{
		if (!Enabled)
		{
			return -9223372036854775808L;
		}
		return GC.GetAllocatedBytesForCurrentThread();
	}

	public static void End(TowerDefenseAllocationMetric metric, long startBytes)
	{
		if (startBytes != -9223372036854775808L && Enabled)
		{
			long num = GC.GetAllocatedBytesForCurrentThread() - startBytes;
			if ((uint)metric < (uint)AllocatedBytes.Length && num >= 0)
			{
				AllocatedBytes[(int)metric] += num;
				SampleCounts[(int)metric]++;
			}
		}
	}

	public static void Sample(TowerDefenseAllocationMetric metric)
	{
		if (Enabled)
		{
			if ((uint)metric < (uint)SampleCounts.Length)
			{
				SampleCounts[(int)metric]++;
			}
		}
	}

	public static long GetAllocatedBytes(TowerDefenseAllocationMetric metric)
	{
		return AllocatedBytes[(int)metric];
	}

	public static long GetSampleCount(TowerDefenseAllocationMetric metric)
	{
		return SampleCounts[(int)metric];
	}

	public static void Reset()
	{
		Array.Clear(AllocatedBytes, 0, AllocatedBytes.Length);
		Array.Clear(SampleCounts, 0, SampleCounts.Length);
	}
}
