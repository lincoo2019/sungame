using System;
using System.Collections.Generic;
using System.Threading.Tasks;

internal sealed class ModCompatibilityBatch
{
	private readonly HashSet<string> _candidates;

	private readonly HashSet<string> _accepted = new HashSet<string>(StringComparer.Ordinal);

	public string Id { get; } = Guid.NewGuid().ToString("N");

	public string Digest { get; }

	public string LevelIdentity { get; }

	public IReadOnlySet<string> Candidates => _candidates;

	public long Deadline { get; }

	public string Failure { get; private set; } = "";

	public TaskCompletionSource<bool> Completion { get; } = new TaskCompletionSource<bool>();

	public bool Accepted
	{
		get
		{
			if (Failure == "")
			{
				return _candidates.SetEquals(_accepted);
			}
			return false;
		}
	}

	public ModCompatibilityBatch(IEnumerable<string> candidates, string digest, long now, int timeoutMs = 15000, string levelIdentity = "")
	{
		_candidates = new HashSet<string>(candidates, StringComparer.Ordinal);
		Digest = digest;
		LevelIdentity = levelIdentity;
		Deadline = now + timeoutMs;
		if (_candidates.Count == 0)
		{
			Completion.TrySetResult(result: true);
		}
	}

	public bool Confirm(string authenticatedPeer, string batchId, bool accepted, string digest, string reason, long now, string levelIdentity = "")
	{
		if (batchId != Id || !_candidates.Contains(authenticatedPeer) || Failure != "" || _accepted.Contains(authenticatedPeer) || levelIdentity != LevelIdentity)
		{
			return false;
		}
		if (now >= Deadline)
		{
			Cancel("Mod 环境确认超时。");
			return false;
		}
		if (!accepted || digest != Digest)
		{
			Cancel("成员 " + authenticatedPeer + " 的 Mod 环境未通过：" + reason);
			return false;
		}
		_accepted.Add(authenticatedPeer);
		if (Accepted)
		{
			Completion.TrySetResult(result: true);
		}
		return true;
	}

	public void Cancel(string reason)
	{
		if (Failure == "")
		{
			Failure = reason;
		}
		Completion.TrySetResult(result: false);
	}
}
