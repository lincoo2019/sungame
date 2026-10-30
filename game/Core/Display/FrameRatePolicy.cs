using System;
using System.Collections.Generic;
using System.Linq;

public static class FrameRatePolicy
{
	private static readonly int[] BaseOptions = new int[20]
	{
		24, 30, 35, 40, 45, 50, 55, 60, 70, 80,
		90, 100, 120, 144, 165, 180, 220, 240, 360, 480
	};

	public static int NormalizeRefreshRate(double value)
	{
		if (double.IsNaN(value) || double.IsInfinity(value) || value < 1.0)
		{
			return 60;
		}
		return Math.Max(1, (int)Math.Round(value, MidpointRounding.AwayFromZero));
	}

	public static int[] BuildOptions(int deviceLimit)
	{
		int num = Math.Max(1, deviceLimit);
		List<int> list = new List<int>();
		int[] baseOptions = BaseOptions;
		foreach (int num2 in baseOptions)
		{
			if (num2 <= num)
			{
				list.Add(num2);
			}
		}
		if (!list.Contains(num))
		{
			list.Add(num);
		}
		list.Sort();
		return list.Distinct().ToArray();
	}

	public static int ResolveEffective(int preferredFrameRate, int deviceLimit)
	{
		return Math.Max(1, Math.Min(Math.Max(1, preferredFrameRate), Math.Max(1, deviceLimit)));
	}

	public static DisplayRefreshMode SelectAndroidMode(DisplayRefreshMode[] modes, int targetFrameRate, int currentWidth, int currentHeight)
	{
		if (modes == null || modes.Length == 0)
		{
			return null;
		}
		DisplayRefreshMode[] array = modes.Where((DisplayRefreshMode mode) => mode != null && mode.RefreshRate > 0).ToArray();
		DisplayRefreshMode[] array2 = array.Where((DisplayRefreshMode mode) => mode.Width == currentWidth && mode.Height == currentHeight).ToArray();
		DisplayRefreshMode[] source = ((array2.Length != 0) ? array2 : array);
		return (from mode in source
			where mode.RefreshRate >= targetFrameRate
			orderby mode.RefreshRate - targetFrameRate, mode.Id
			select mode).FirstOrDefault() ?? (from mode in source
			orderby mode.RefreshRate descending, mode.Id
			select mode).FirstOrDefault();
	}
}
