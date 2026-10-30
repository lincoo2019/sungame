using System.Collections.Generic;

public sealed class TowerDefenseBattleOperationTracker
{
	private readonly HashSet<int> _pending = new HashSet<int>();

	private int _nextId;

	public int Count => _pending.Count;

	public bool HasPending => _pending.Count > 0;

	public int Begin()
	{
		int nextId;
		do
		{
			_nextId++;
			if (_nextId <= 0)
			{
				_nextId = 1;
			}
			nextId = _nextId;
		}
		while (_pending.Contains(nextId));
		_pending.Add(nextId);
		return nextId;
	}

	public bool IsCurrent(int operationId)
	{
		if (operationId > 0)
		{
			return _pending.Contains(operationId);
		}
		return false;
	}

	public void Complete(int operationId)
	{
		_pending.Remove(operationId);
	}

	public void Clear()
	{
		_pending.Clear();
	}
}
