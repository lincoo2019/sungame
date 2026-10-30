using System;
using Godot;

public static class TowerDefenseCharacterSpawnBudget
{
	private static readonly object Sync = new object();

	public const double MaxFrameBudgetMilliseconds = 16.0;

	public const double AtomicWorkReserveMilliseconds = 6.0;

	public const double MaximumAtomicWorkReserveMilliseconds = 10.0;

	private static ulong _budgetProcessFrame = 18446744073709551615uL;

	private static ulong _recordedFrameWorkUsec;

	private static ulong _observedAtomicWorkMaxUsec = 6000uL;

	public const int DefaultNoGcReservationCharacters = 1024;

	private static int _spawnNoGcScopeDepth;

	private static bool _spawnNoGcRegionActive;

	private static bool _spawnNoGcRegionUnavailable;

	public static bool IsFrameBudgetExhausted(double frameBudgetMilliseconds)
	{
		lock (Sync)
		{
			RefreshFrameBudgetLocked();
			double num = ClampFrameBudget(frameBudgetMilliseconds) * 1000.0;
			double num2 = Math.Max(250.0, num - (double)_observedAtomicWorkMaxUsec);
			return (double)_recordedFrameWorkUsec >= num2;
		}
	}

	public static void RecordFrameWork(ulong workStartUsec, bool updateAtomicEstimate = true)
	{
		ulong ticksUsec = Time.GetTicksUsec();
		ulong num = ((ticksUsec >= workStartUsec) ? (ticksUsec - workStartUsec) : 0);
		lock (Sync)
		{
			RefreshFrameBudgetLocked();
			if (updateAtomicEstimate)
			{
				UpdateAtomicWorkEstimateLocked(num);
			}
			_recordedFrameWorkUsec = (((ulong)(-1L - (long)_recordedFrameWorkUsec) < num) ? 18446744073709551615uL : (_recordedFrameWorkUsec + num));
		}
	}

	public static double ClampFrameBudget(double frameBudgetMilliseconds)
	{
		return Mathf.Clamp(frameBudgetMilliseconds, 0.25, 16.0);
	}

	private static void UpdateAtomicWorkEstimateLocked(ulong elapsedUsec)
	{
		ulong val = 6000uL;
		ulong val2 = 10000uL;
		ulong num = Math.Min(elapsedUsec, val2);
		if (num >= _observedAtomicWorkMaxUsec)
		{
			_observedAtomicWorkMaxUsec = num;
			return;
		}
		ulong val3 = (_observedAtomicWorkMaxUsec * 7 + num) / 8;
		_observedAtomicWorkMaxUsec = Math.Max(val, val3);
	}

	private static void RefreshFrameBudgetLocked()
	{
		ulong processFrames = Engine.GetProcessFrames();
		if (_budgetProcessFrame != processFrames)
		{
			_budgetProcessFrame = processFrames;
			_recordedFrameWorkUsec = 0uL;
		}
	}

	public static bool TryBeginSpawnNoGcRegion(int expectedCharacters)
	{
		lock (Sync)
		{
			if (_spawnNoGcRegionUnavailable)
			{
				return false;
			}
			if (_spawnNoGcScopeDepth > 0)
			{
				_spawnNoGcScopeDepth++;
				return _spawnNoGcRegionActive;
			}
			long totalSize = CalculateNoGcReserveBytes(expectedCharacters);
			try
			{
				_spawnNoGcRegionActive = GC.TryStartNoGCRegion(totalSize, disallowFullBlockingGC: true);
			}
			catch (InvalidOperationException)
			{
				_spawnNoGcRegionActive = false;
			}
			catch (NotImplementedException)
			{
				_spawnNoGcRegionActive = false;
				_spawnNoGcRegionUnavailable = true;
			}
			catch (PlatformNotSupportedException)
			{
				_spawnNoGcRegionActive = false;
				_spawnNoGcRegionUnavailable = true;
			}
			if (_spawnNoGcRegionActive)
			{
				_spawnNoGcScopeDepth = 1;
			}
			return _spawnNoGcRegionActive;
		}
	}

	public static void EndSpawnNoGcRegion()
	{
		bool flag;
		lock (Sync)
		{
			if (_spawnNoGcScopeDepth <= 0)
			{
				return;
			}
			_spawnNoGcScopeDepth--;
			flag = _spawnNoGcScopeDepth == 0 && _spawnNoGcRegionActive;
			if (flag)
			{
				_spawnNoGcRegionActive = false;
			}
		}
		if (flag)
		{
			EndActiveNoGcRegion();
		}
	}

	private static long CalculateNoGcReserveBytes(int expectedCharacters)
	{
		return Math.Clamp((long)Mathf.Max(1, expectedCharacters) * 64L * 1024, 16777216L, 134217728L);
	}

	private static void EndActiveNoGcRegion()
	{
		try
		{
			GC.EndNoGCRegion();
		}
		catch (InvalidOperationException)
		{
		}
		catch (NotImplementedException)
		{
			MarkNoGcRegionUnavailable();
		}
		catch (PlatformNotSupportedException)
		{
			MarkNoGcRegionUnavailable();
		}
	}

	private static void MarkNoGcRegionUnavailable()
	{
		lock (Sync)
		{
			_spawnNoGcRegionUnavailable = true;
		}
	}
}
