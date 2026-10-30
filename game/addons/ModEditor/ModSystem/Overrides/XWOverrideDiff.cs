using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.ModSystem.Overrides;

public sealed class XWOverrideDiff
{
	public sealed class Entry
	{
		public string PropertyPath { get; set; } = "";

		public Variant OriginalValue { get; set; }

		public Variant ModValue { get; set; }

		public bool Enabled { get; set; } = true;
	}

	public string Category { get; set; } = "";

	public string BaseKey { get; set; } = "";

	public string TargetKey { get; set; } = "";

	public List<Entry> Entries { get; } = new List<Entry>();

	public Entry Add(string propertyPath, Variant originalValue, Variant modValue, bool enabled = true)
	{
		Entry entry = new Entry
		{
			PropertyPath = propertyPath,
			OriginalValue = originalValue,
			ModValue = modValue,
			Enabled = enabled
		};
		Entries.Add(entry);
		return entry;
	}

	public void SetEnabled(string propertyPath, bool enabled)
	{
		foreach (Entry entry in Entries)
		{
			if (entry.PropertyPath == propertyPath)
			{
				entry.Enabled = enabled;
			}
		}
	}
}
