using System.Collections.Generic;
using Godot;

public sealed class ReplaceState
{
	private readonly Dictionary<int, StringName> _replaceKeys = new Dictionary<int, StringName>();

	public ulong Signature { get; private set; }

	public void Set(int mediaId, StringName replaceKey)
	{
		if (mediaId >= 0)
		{
			if (replaceKey == null || string.IsNullOrEmpty(replaceKey.ToString()))
			{
				_replaceKeys.Remove(mediaId);
			}
			else
			{
				_replaceKeys[mediaId] = replaceKey;
			}
			RebuildSignature();
		}
	}

	public bool TryGet(int mediaId, out StringName replaceKey)
	{
		return _replaceKeys.TryGetValue(mediaId, out replaceKey);
	}

	public void Clear()
	{
		_replaceKeys.Clear();
		Signature = 0uL;
	}

	private void RebuildSignature()
	{
		ulong num = 1469598103934665603uL;
		foreach (KeyValuePair<int, StringName> replaceKey in _replaceKeys)
		{
			num ^= (ulong)replaceKey.Key;
			num *= 1099511628211L;
			num ^= (ulong)replaceKey.Value.ToString().GetHashCode();
			num *= 1099511628211L;
		}
		Signature = num;
	}
}
