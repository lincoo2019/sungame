using System.Collections.Generic;
using System.Runtime.CompilerServices;

internal sealed class CharacterComponentOperationDestroyBarrier
{
	private sealed class BarrierState
	{
		public long Generation;

		public int Pending;
	}

	public const double OperationTimeoutSeconds = 5.0;

	public const double RetryIntervalSeconds = 0.05;

	private static readonly ConditionalWeakTable<BattleNetworkSession, CharacterComponentOperationDestroyBarrier> SessionBarriers = new ConditionalWeakTable<BattleNetworkSession, CharacterComponentOperationDestroyBarrier>();

	private readonly object _gate = new object();

	private readonly Dictionary<int, BarrierState> _statesBySyncId = new Dictionary<int, BarrierState>();

	private long _nextGeneration;

	public static CharacterComponentOperationDestroyBarrier ForSession(BattleNetworkSession session)
	{
		if (session != null)
		{
			return SessionBarriers.GetValue(session, (BattleNetworkSession _) => new CharacterComponentOperationDestroyBarrier());
		}
		return null;
	}

	public long TrackArrival(int syncId)
	{
		if (syncId < 0)
		{
			return 0L;
		}
		lock (_gate)
		{
			if (!_statesBySyncId.TryGetValue(syncId, out var value))
			{
				if (_nextGeneration < 9223372036854775807L)
				{
					_nextGeneration++;
				}
				value = new BarrierState
				{
					Generation = _nextGeneration
				};
				_statesBySyncId[syncId] = value;
			}
			value.Pending++;
			return value.Generation;
		}
	}

	public void MarkResolved(int syncId, long generation)
	{
		if (syncId < 0 || generation <= 0)
		{
			return;
		}
		lock (_gate)
		{
			if (_statesBySyncId.TryGetValue(syncId, out var value) && value.Generation == generation)
			{
				if (value.Pending > 0)
				{
					value.Pending--;
				}
				if (value.Pending == 0)
				{
					_statesBySyncId.Remove(syncId);
				}
			}
		}
	}

	public bool HasPending(int syncId)
	{
		if (syncId < 0)
		{
			return false;
		}
		lock (_gate)
		{
			BarrierState value;
			return _statesBySyncId.TryGetValue(syncId, out value) && value.Pending > 0;
		}
	}

	public bool IsCurrent(int syncId, long generation)
	{
		if (syncId < 0 || generation <= 0)
		{
			return false;
		}
		lock (_gate)
		{
			BarrierState value;
			return _statesBySyncId.TryGetValue(syncId, out value) && value.Generation == generation;
		}
	}

	public void Invalidate(int syncId)
	{
		if (syncId < 0)
		{
			return;
		}
		lock (_gate)
		{
			_statesBySyncId.Remove(syncId);
		}
	}

	public void Clear()
	{
		lock (_gate)
		{
			_statesBySyncId.Clear();
			_nextGeneration = 0L;
		}
	}
}
