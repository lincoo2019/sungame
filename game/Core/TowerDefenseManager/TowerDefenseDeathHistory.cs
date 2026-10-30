using System;
using System.Collections.Generic;

internal sealed class TowerDefenseDeathHistory
{
	private sealed class CampBuffer
	{
		private readonly TowerDefenseDeathRecord[] _records;

		private int _head;

		public int Count { get; private set; }

		public CampBuffer(int capacity)
		{
			_records = new TowerDefenseDeathRecord[capacity];
		}

		public void Clear()
		{
			Array.Clear(_records);
			_head = 0;
			Count = 0;
		}

		private int PhysicalIndex(int logicalIndex)
		{
			return (_head + logicalIndex) % _records.Length;
		}

		public bool HasDeathRecord(bool requireAngelEligible)
		{
			for (int num = Count - 1; num >= 0; num--)
			{
				TowerDefenseDeathRecord towerDefenseDeathRecord = _records[PhysicalIndex(num)];
				if (!requireAngelEligible || towerDefenseDeathRecord.CanAngelRevive)
				{
					return true;
				}
			}
			return false;
		}

		public bool TryTakeLatest(bool requireAngelEligible, out TowerDefenseDeathRecord record)
		{
			record = default(TowerDefenseDeathRecord);
			for (int num = Count - 1; num >= 0; num--)
			{
				int num2 = PhysicalIndex(num);
				TowerDefenseDeathRecord towerDefenseDeathRecord = _records[num2];
				if (!requireAngelEligible || towerDefenseDeathRecord.CanAngelRevive)
				{
					record = towerDefenseDeathRecord;
					RemoveAt(num);
					return true;
				}
			}
			return false;
		}

		public void Add(in TowerDefenseDeathRecord record)
		{
			int num;
			if (Count == _records.Length)
			{
				num = _head;
				_head = (_head + 1) % _records.Length;
			}
			else
			{
				num = PhysicalIndex(Count);
				Count++;
			}
			_records[num] = record;
		}

		private void RemoveAt(int logicalIndex)
		{
			for (int i = logicalIndex + 1; i < Count; i++)
			{
				_records[PhysicalIndex(i - 1)] = _records[PhysicalIndex(i)];
			}
			_records[PhysicalIndex(Count - 1)] = default(TowerDefenseDeathRecord);
			Count--;
			if (Count == 0)
			{
				_head = 0;
			}
		}
	}

	private readonly int _capacityPerCamp;

	private readonly Dictionary<TowerDefenseEnum.CHARACTER_CAMP, CampBuffer> _campBuffers = new Dictionary<TowerDefenseEnum.CHARACTER_CAMP, CampBuffer>();

	public int Count { get; private set; }

	public TowerDefenseDeathHistory(int capacityPerCamp)
	{
		_capacityPerCamp = Math.Max(1, capacityPerCamp);
	}

	public bool HasDeathRecord(TowerDefenseEnum.CHARACTER_CAMP camp, bool requireAngelEligible)
	{
		if (_campBuffers.TryGetValue(camp, out var value))
		{
			return value.HasDeathRecord(requireAngelEligible);
		}
		return false;
	}

	public bool TryTakeLatest(TowerDefenseEnum.CHARACTER_CAMP camp, bool requireAngelEligible, out TowerDefenseDeathRecord record)
	{
		record = default(TowerDefenseDeathRecord);
		if (!_campBuffers.TryGetValue(camp, out var value) || !value.TryTakeLatest(requireAngelEligible, out record))
		{
			return false;
		}
		Count--;
		return true;
	}

	public void Add(in TowerDefenseDeathRecord record)
	{
		if (!_campBuffers.TryGetValue(record.Camp, out var value))
		{
			value = new CampBuffer(_capacityPerCamp);
			_campBuffers.Add(record.Camp, value);
		}
		bool num = value.Count == _capacityPerCamp;
		value.Add(in record);
		if (!num)
		{
			Count++;
		}
	}

	public void Clear()
	{
		foreach (CampBuffer value in _campBuffers.Values)
		{
			value.Clear();
		}
		_campBuffers.Clear();
		Count = 0;
	}
}
