using System;
using System.Collections.Generic;

public sealed class FullGameplayResourceCategoryMetrics
{
	public string Category { get; }

	public int RootCount { get; }

	public double TotalActualLoadMilliseconds { get; }

	public double TotalPublishMilliseconds { get; }

	public double P50ActualLoadMilliseconds { get; }

	public double P95ActualLoadMilliseconds { get; }

	public double MaximumActualLoadMilliseconds { get; }

	public int CacheBindingHitCount { get; }

	public int CacheBindingMissCount { get; }

	public FullGameplayResourceCategoryMetrics(string category, IReadOnlyList<FullGameplayRootLoadMetric> metrics)
	{
		Category = category;
		RootCount = metrics.Count;
		double[] array = new double[metrics.Count];
		for (int i = 0; i < metrics.Count; i++)
		{
			FullGameplayRootLoadMetric fullGameplayRootLoadMetric = metrics[i];
			array[i] = fullGameplayRootLoadMetric.ActualLoadMilliseconds;
			TotalActualLoadMilliseconds += fullGameplayRootLoadMetric.ActualLoadMilliseconds;
			TotalPublishMilliseconds += fullGameplayRootLoadMetric.PublishMilliseconds;
			CacheBindingHitCount += fullGameplayRootLoadMetric.CacheBindingHitCount;
			CacheBindingMissCount += fullGameplayRootLoadMetric.CacheBindingMissCount;
		}
		Array.Sort(array);
		P50ActualLoadMilliseconds = Percentile(array, 0.5);
		P95ActualLoadMilliseconds = Percentile(array, 0.95);
		MaximumActualLoadMilliseconds = ((array.Length == 0) ? 0.0 : array[^1]);
	}

	private static double Percentile(double[] sortedValues, double percentile)
	{
		if (sortedValues.Length == 0)
		{
			return 0.0;
		}
		int num = Math.Clamp((int)Math.Ceiling((double)sortedValues.Length * percentile) - 1, 0, sortedValues.Length - 1);
		return sortedValues[num];
	}
}
