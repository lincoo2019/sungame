using System;

public static class BenchmarkStatistics
{
	public static double Average(double[] values, int count)
	{
		ValidateCount(values, count);
		if (count == 0)
		{
			return 0.0;
		}
		double num = 0.0;
		for (int i = 0; i < count; i++)
		{
			num += values[i];
		}
		return num / (double)count;
	}

	public static double Maximum(double[] values, int count)
	{
		ValidateCount(values, count);
		if (count == 0)
		{
			return 0.0;
		}
		double num = values[0];
		for (int i = 1; i < count; i++)
		{
			num = Math.Max(num, values[i]);
		}
		return num;
	}

	public static double Percentile(double[] values, int count, double percentile)
	{
		ValidateCount(values, count);
		if (count == 0)
		{
			return 0.0;
		}
		double[] array = new double[count];
		Array.Copy(values, array, count);
		Array.Sort(array);
		return PercentileOfSorted(array, percentile);
	}

	public static double WorstFractionFps(double[] frameSeconds, int count, double fraction)
	{
		ValidateCount(frameSeconds, count);
		if (count == 0)
		{
			return 0.0;
		}
		fraction = Math.Clamp(fraction, 1.0 / (double)count, 1.0);
		double[] array = new double[count];
		Array.Copy(frameSeconds, array, count);
		Array.Sort(array);
		int num = Math.Clamp((int)Math.Ceiling((double)count * fraction), 1, count);
		double num2 = 0.0;
		for (int i = count - num; i < count; i++)
		{
			num2 += array[i];
		}
		double num3 = num2 / (double)num;
		if (!(num3 > 0.0))
		{
			return 0.0;
		}
		return 1.0 / num3;
	}

	private static double PercentileOfSorted(double[] sorted, double percentile)
	{
		percentile = Math.Clamp(percentile, 0.0, 100.0);
		double num = percentile / 100.0 * (double)(sorted.Length - 1);
		int num2 = (int)Math.Floor(num);
		int num3 = (int)Math.Ceiling(num);
		if (num2 == num3)
		{
			return sorted[num2];
		}
		double num4 = num - (double)num2;
		return sorted[num2] + (sorted[num3] - sorted[num2]) * num4;
	}

	private static void ValidateCount(double[] values, int count)
	{
		ArgumentNullException.ThrowIfNull(values, "values");
		if (count < 0 || count > values.Length)
		{
			throw new ArgumentOutOfRangeException("count");
		}
	}
}
