using System;
using System.Collections.Generic;

public static class ModRuntimeLifecycleProbeRecorder
{
	private static readonly object Gate = new object();

	private static readonly List<string> Events = new List<string>();

	private static IDisposable HeldRuntime;

	public static void Reset()
	{
		lock (Gate)
		{
			HeldRuntime?.Dispose();
			HeldRuntime = null;
			Events.Clear();
		}
	}

	public static void Record(string value)
	{
		lock (Gate)
		{
			Events.Add(value ?? "");
		}
	}

	public static string[] Snapshot()
	{
		lock (Gate)
		{
			return Events.ToArray();
		}
	}

	public static void Hold(IDisposable runtime)
	{
		lock (Gate)
		{
			HeldRuntime?.Dispose();
			HeldRuntime = runtime;
		}
	}

	public static void ReleaseHeld()
	{
		lock (Gate)
		{
			HeldRuntime?.Dispose();
			HeldRuntime = null;
		}
	}
}
