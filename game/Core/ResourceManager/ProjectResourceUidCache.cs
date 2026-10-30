using System;
using System.Collections.Generic;
using Godot;

public static class ProjectResourceUidCache
{
	private static readonly HashSet<string> _warnedInvalidUids = new HashSet<string>();

	public static void EnsureRegistered()
	{
	}

	public static string ResolveResourcePath(string path)
	{
		if (string.IsNullOrEmpty(path) || !path.StartsWith("uid://", StringComparison.Ordinal))
		{
			return path;
		}
		long num = ResourceUid.TextToId(path);
		if (num != -1 && ResourceUid.HasId(num))
		{
			string idPath = ResourceUid.GetIdPath(num);
			if (!string.IsNullOrEmpty(idPath))
			{
				return idPath;
			}
		}
		WarnInvalidUid(path);
		return path;
	}

	private static void WarnInvalidUid(string uid)
	{
		lock (_warnedInvalidUids)
		{
			if (!_warnedInvalidUids.Add(uid))
			{
				return;
			}
		}
		GD.PushWarning("Invalid resource UID at runtime: " + uid + ". Fix UIDs from the editor toolbar before running/exporting.");
	}
}
