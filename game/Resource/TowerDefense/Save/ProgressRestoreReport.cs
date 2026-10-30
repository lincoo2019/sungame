using System;
using System.Collections.Generic;
using Godot;

public sealed class ProgressRestoreReport
{
	private readonly Dictionary<string, int> _counts = new Dictionary<string, int>(StringComparer.Ordinal);

	public bool HasIssues => _counts.Count != 0;

	public int Count(string category)
	{
		return _counts.GetValueOrDefault(category);
	}

	public void Record(string category, string detail, int count = 1)
	{
		if (count > 0)
		{
			int valueOrDefault = _counts.GetValueOrDefault(category);
			_counts[category] = valueOrDefault + count;
			if (valueOrDefault == 0)
			{
				GD.PushWarning("[ProgressLoad:" + category + "] " + detail);
			}
		}
	}

	public bool Try(string category, Action restore)
	{
		try
		{
			restore();
			return true;
		}
		catch (Exception ex)
		{
			Record(category, ex.Message);
			return false;
		}
	}
}
