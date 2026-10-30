using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Godot;

public static class TowerDefensePerfProfiler
{
	private struct Metric
	{
		public long Ticks;

		public long MaxTicks;

		public int Count;

		public int Items;

		public int MaxItems;
	}

	public readonly struct SpikeProbe(long startTicks, long startAllocatedBytes, int startGen0, int startGen1, int startGen2)
	{
		internal readonly long StartTicks = startTicks;

		internal readonly long StartAllocatedBytes = startAllocatedBytes;

		internal readonly int StartGen0 = startGen0;

		internal readonly int StartGen1 = startGen1;

		internal readonly int StartGen2 = startGen2;
	}

	public static bool Enabled = false;

	public static bool DetailedHotPathMetrics = false;

	public static int DumpIntervalFrames = 120;

	public static int MaxMetricsPerDump = 128;

	private static readonly Dictionary<string, Metric> Metrics = new Dictionary<string, Metric>();

	private static readonly List<KeyValuePair<string, Metric>> DumpMetrics = new List<KeyValuePair<string, Metric>>();

	private static readonly StringBuilder DumpBuilder = new StringBuilder(1024);

	private static readonly StringBuilder SpikeBuilder = new StringBuilder(256);

	private static ulong _lastDumpFrame = 0uL;

	private static int _lastDumpGen0;

	private static int _lastDumpGen1;

	private static int _lastDumpGen2;

	private static long _lastDumpAllocatedBytes;

	private static bool _hasDumpGcBaseline;

	private static void Dump(ulong frame, ulong windowFrames)
	{
		if (Metrics.Count != 0)
		{
			DumpBuilder.Clear();
			AppendDumpHeader(frame, windowFrames);
			AppendDumpRows(windowFrames);
			GD.Print(DumpBuilder.ToString());
			DumpMetrics.Clear();
		}
	}

	public static void DumpIfNeeded()
	{
		if (!Enabled)
		{
			return;
		}
		ulong physicsFrames = Engine.GetPhysicsFrames();
		if (_lastDumpFrame == 0L)
		{
			_lastDumpFrame = physicsFrames;
			CaptureDumpGcBaseline();
			return;
		}
		int num = ((DumpIntervalFrames <= 0) ? 120 : DumpIntervalFrames);
		if (physicsFrames - _lastDumpFrame >= (ulong)num)
		{
			Dump(physicsFrames, physicsFrames - _lastDumpFrame);
			_lastDumpFrame = physicsFrames;
			CaptureDumpGcBaseline();
			Metrics.Clear();
		}
	}

	public static void Reset()
	{
		Metrics.Clear();
		DumpMetrics.Clear();
		_lastDumpFrame = 0uL;
		_hasDumpGcBaseline = false;
	}

	private static void AppendDumpHeader(ulong frame, ulong windowFrames)
	{
		DumpBuilder.Append("[TDPerf] frame=");
		DumpBuilder.Append(frame);
		DumpBuilder.Append(" window=");
		DumpBuilder.Append(windowFrames);
		DumpBuilder.Append(" fps=");
		DumpBuilder.Append(Performance.GetMonitor(Performance.Monitor.TimeFps).ToString("F1"));
		DumpBuilder.Append(" processMs=");
		DumpBuilder.Append((Performance.GetMonitor(Performance.Monitor.TimeProcess) * 1000.0).ToString("F3"));
		DumpBuilder.Append(" physicsMs=");
		DumpBuilder.Append((Performance.GetMonitor(Performance.Monitor.TimePhysicsProcess) * 1000.0).ToString("F3"));
		DumpBuilder.Append(" metrics=");
		DumpBuilder.Append(Metrics.Count);
		AppendDumpGcMetrics();
	}

	private static void AppendDumpGcMetrics()
	{
		if (_hasDumpGcBaseline)
		{
			DumpBuilder.Append(" allocatedBytes=");
			DumpBuilder.Append(Math.Max(0L, GC.GetTotalAllocatedBytes() - _lastDumpAllocatedBytes));
			DumpBuilder.Append(" gc0=");
			DumpBuilder.Append(GC.CollectionCount(0) - _lastDumpGen0);
			DumpBuilder.Append(" gc1=");
			DumpBuilder.Append(GC.CollectionCount(1) - _lastDumpGen1);
			DumpBuilder.Append(" gc2=");
			DumpBuilder.Append(GC.CollectionCount(2) - _lastDumpGen2);
		}
	}

	private static void AppendDumpMetricRow(string name, Metric metric, ulong windowFrames, int printed)
	{
		double num = (double)metric.Ticks * 1000.0 / (double)Stopwatch.Frequency;
		double num2 = (double)metric.MaxTicks * 1000000.0 / (double)Stopwatch.Frequency;
		double num3 = ((windowFrames != 0) ? (num / (double)windowFrames) : 0.0);
		double num4 = ((metric.Count > 0) ? (num * 1000.0 / (double)metric.Count) : 0.0);
		DumpBuilder.AppendLine();
		DumpBuilder.Append("  ");
		DumpBuilder.Append((printed + 1).ToString("D2"));
		DumpBuilder.Append(". ");
		DumpBuilder.Append(name);
		DumpBuilder.Append(": count=");
		DumpBuilder.Append(metric.Count);
		DumpBuilder.Append(" totalMs=");
		DumpBuilder.Append(num.ToString("F3"));
		DumpBuilder.Append(" ms/frame=");
		DumpBuilder.Append(num3.ToString("F3"));
		DumpBuilder.Append(" avgUs=");
		DumpBuilder.Append(num4.ToString("F2"));
		DumpBuilder.Append(" maxUs=");
		DumpBuilder.Append(num2.ToString("F2"));
		AppendDumpMetricItems(metric);
	}

	private static void AppendDumpMetricItems(Metric metric)
	{
		if (metric.Items > 0 || metric.MaxItems > 0)
		{
			DumpBuilder.Append(" items=");
			DumpBuilder.Append(metric.Items);
			DumpBuilder.Append(" maxItems=");
			DumpBuilder.Append(metric.MaxItems);
		}
	}

	private static void AppendDumpRows(ulong windowFrames)
	{
		DumpMetrics.Clear();
		foreach (KeyValuePair<string, Metric> metric in Metrics)
		{
			DumpMetrics.Add(metric);
		}
		DumpMetrics.Sort(CompareMetricForDump);
		int num = ((MaxMetricsPerDump <= 0) ? DumpMetrics.Count : MaxMetricsPerDump);
		int num2 = 0;
		foreach (KeyValuePair<string, Metric> dumpMetric in DumpMetrics)
		{
			if (num2 >= num)
			{
				break;
			}
			AppendDumpMetricRow(dumpMetric.Key, dumpMetric.Value, windowFrames, num2);
			num2++;
		}
		AppendDumpMoreCount(num2);
	}

	private static void AppendDumpMoreCount(int printed)
	{
		if (Metrics.Count > printed)
		{
			DumpBuilder.AppendLine();
			DumpBuilder.Append("  more=");
			DumpBuilder.Append(Metrics.Count - printed);
		}
	}

	private static int CompareMetricForDump(KeyValuePair<string, Metric> left, KeyValuePair<string, Metric> right)
	{
		int num = right.Value.Ticks.CompareTo(left.Value.Ticks);
		if (num != 0)
		{
			return num;
		}
		int num2 = right.Value.Items.CompareTo(left.Value.Items);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = right.Value.Count.CompareTo(left.Value.Count);
		if (num3 != 0)
		{
			return num3;
		}
		return string.CompareOrdinal(left.Key, right.Key);
	}

	private static void CaptureDumpGcBaseline()
	{
		_lastDumpGen0 = GC.CollectionCount(0);
		_lastDumpGen1 = GC.CollectionCount(1);
		_lastDumpGen2 = GC.CollectionCount(2);
		_lastDumpAllocatedBytes = GC.GetTotalAllocatedBytes();
		_hasDumpGcBaseline = true;
	}

	public static string GetCallbackMetricName(string prefix, Delegate callback)
	{
		if ((object)callback == null)
		{
			return prefix;
		}
		MethodInfo method = callback.Method;
		string text = ((method.DeclaringType != null) ? method.DeclaringType.Name : "Unknown");
		return prefix + "." + text + "." + method.Name;
	}

	private static void Add(string name, long ticks, int items)
	{
		if (!Metrics.TryGetValue(name, out var value))
		{
			value = default(Metric);
		}
		value.Ticks += ticks;
		if (ticks > value.MaxTicks)
		{
			value.MaxTicks = ticks;
		}
		value.Count++;
		value.Items += items;
		if (items > value.MaxItems)
		{
			value.MaxItems = items;
		}
		Metrics[name] = value;
	}

	public static long Begin()
	{
		if (!Enabled)
		{
			return 0L;
		}
		return Stopwatch.GetTimestamp();
	}

	public static long BeginHotPath()
	{
		if (!DetailedHotPathMetrics || !Enabled)
		{
			return 0L;
		}
		return Stopwatch.GetTimestamp();
	}

	public static void End(string name, long startTicks, int items = 0)
	{
		if (startTicks != 0L && Enabled)
		{
			Add(name, Stopwatch.GetTimestamp() - startTicks, items);
		}
	}

	public static void Sample(string name, int items = 0)
	{
		if (Enabled)
		{
			Add(name, 0L, items);
		}
	}

	public static void SampleHotPath(string name, int items = 0)
	{
		if (DetailedHotPathMetrics && Enabled)
		{
			Add(name, 0L, items);
		}
	}

	private static void PrintSpikeProbe(string name, in SpikeProbe probe, int items, long elapsedTicks)
	{
		int value = GC.CollectionCount(0) - probe.StartGen0;
		int value2 = GC.CollectionCount(1) - probe.StartGen1;
		int value3 = GC.CollectionCount(2) - probe.StartGen2;
		long value4 = Math.Max(0L, GC.GetAllocatedBytesForCurrentThread() - probe.StartAllocatedBytes);
		SpikeBuilder.Clear();
		SpikeBuilder.Append("[TDSpike] frame=");
		SpikeBuilder.Append(Engine.GetPhysicsFrames());
		SpikeBuilder.Append(" scope=");
		SpikeBuilder.Append(name);
		SpikeBuilder.Append(" elapsedMs=");
		SpikeBuilder.Append(((double)elapsedTicks * 1000.0 / (double)Stopwatch.Frequency).ToString("F3"));
		SpikeBuilder.Append(" items=");
		SpikeBuilder.Append(items);
		SpikeBuilder.Append(" allocatedBytes=");
		SpikeBuilder.Append(value4);
		SpikeBuilder.Append(" gc0=");
		SpikeBuilder.Append(value);
		SpikeBuilder.Append(" gc1=");
		SpikeBuilder.Append(value2);
		SpikeBuilder.Append(" gc2=");
		SpikeBuilder.Append(value3);
		GD.Print(SpikeBuilder.ToString());
	}

	public static SpikeProbe BeginSpikeProbe()
	{
		if (!DetailedHotPathMetrics || !Enabled)
		{
			return default(SpikeProbe);
		}
		return new SpikeProbe(Stopwatch.GetTimestamp(), GC.GetAllocatedBytesForCurrentThread(), GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2));
	}

	public static void EndSpikeProbe(string name, in SpikeProbe probe, int items = 0, double thresholdMilliseconds = 4.0)
	{
		if (probe.StartTicks != 0L && DetailedHotPathMetrics && Enabled)
		{
			long num = Stopwatch.GetTimestamp() - probe.StartTicks;
			double num2 = Math.Max(0.0, thresholdMilliseconds) * (double)Stopwatch.Frequency / 1000.0;
			if (!((double)num < num2))
			{
				PrintSpikeProbe(name, in probe, items, num);
			}
		}
	}
}
