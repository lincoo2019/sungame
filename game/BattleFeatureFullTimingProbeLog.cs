using System.Collections.Generic;

public static class BattleFeatureFullTimingProbeLog
{
	public static readonly List<string> Entries = new List<string>();

	public static void Add(string entry)
	{
		Entries.Add(entry);
	}

	public static void Reset()
	{
		Entries.Clear();
	}
}
