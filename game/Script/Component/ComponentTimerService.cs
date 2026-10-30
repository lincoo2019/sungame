using System;
using System.Collections.Generic;
using Godot;

internal sealed class ComponentTimerService
{
	private sealed class TimerEntry
	{
		public ComponentTimerKey Key;

		public WeakReference<ComponentBase> Owner;

		public double RemainingSeconds;

		public bool Running;

		public bool Paused;

		public bool Attached;

		public bool UseOwnerTimeScale;

		public int ActiveIndex = -1;
	}

	private readonly Dictionary<ComponentTimerKey, TimerEntry> _entries = new Dictionary<ComponentTimerKey, TimerEntry>();

	private readonly List<TimerEntry> _active = new List<TimerEntry>();

	private readonly List<TimerEntry> _expired = new List<TimerEntry>();

	private readonly List<TimerEntry> _removeBuffer = new List<TimerEntry>();

	public bool HasActiveTimers => _active.Count > 0;

	public bool Start(ComponentBase owner, ComponentTimerKey key, double duration, bool useOwnerTimeScale)
	{
		if (!IsUsable(owner, key))
		{
			return false;
		}
		duration = NormalizeDuration(duration);
		if (!_entries.TryGetValue(key, out var value) || !IsOwner(value, owner))
		{
			if (value != null)
			{
				RemoveEntry(value);
			}
			value = new TimerEntry
			{
				Key = key,
				Owner = new WeakReference<ComponentBase>(owner)
			};
			_entries[key] = value;
		}
		value.RemainingSeconds = duration;
		value.Running = true;
		value.Paused = false;
		value.Attached = owner.IsInsideTree();
		value.UseOwnerTimeScale = useOwnerTimeScale;
		RefreshActive(value);
		return true;
	}

	public bool Stop(ComponentBase owner, ComponentTimerKey key)
	{
		if (!TryGetOwnedEntry(owner, key, out var entry))
		{
			return false;
		}
		RemoveEntry(entry);
		return true;
	}

	public bool Pause(ComponentBase owner, ComponentTimerKey key)
	{
		if (!TryGetOwnedEntry(owner, key, out var entry) || !entry.Running)
		{
			return false;
		}
		entry.Paused = true;
		RefreshActive(entry);
		return true;
	}

	public bool Resume(ComponentBase owner, ComponentTimerKey key)
	{
		if (!TryGetOwnedEntry(owner, key, out var entry) || !entry.Running)
		{
			return false;
		}
		entry.Paused = false;
		RefreshActive(entry);
		return true;
	}

	public bool IsRunning(ComponentBase owner, ComponentTimerKey key)
	{
		if (TryGetOwnedEntry(owner, key, out var entry) && entry.Running)
		{
			return !entry.Paused;
		}
		return false;
	}

	public double Remaining(ComponentBase owner, ComponentTimerKey key)
	{
		if (!TryGetOwnedEntry(owner, key, out var entry))
		{
			return 0.0;
		}
		return Math.Max(0.0, entry.RemainingSeconds);
	}

	public void AttachComponent(ComponentBase owner, string componentSlot)
	{
		if (owner == null || string.IsNullOrEmpty(componentSlot))
		{
			return;
		}
		foreach (KeyValuePair<ComponentTimerKey, TimerEntry> entry in _entries)
		{
			TimerEntry value = entry.Value;
			if (entry.Key.ComponentSlot == componentSlot && IsOwner(value, owner))
			{
				value.Attached = true;
				RefreshActive(value);
			}
		}
	}

	public void DetachComponent(ComponentBase owner, string componentSlot)
	{
		if (owner == null || string.IsNullOrEmpty(componentSlot))
		{
			return;
		}
		foreach (KeyValuePair<ComponentTimerKey, TimerEntry> entry in _entries)
		{
			TimerEntry value = entry.Value;
			if (entry.Key.ComponentSlot == componentSlot && IsOwner(value, owner))
			{
				value.Attached = false;
				RefreshActive(value);
			}
		}
	}

	public void RemoveComponent(ComponentBase owner, string componentSlot)
	{
		if (owner == null || string.IsNullOrEmpty(componentSlot))
		{
			return;
		}
		_removeBuffer.Clear();
		foreach (KeyValuePair<ComponentTimerKey, TimerEntry> entry in _entries)
		{
			if (entry.Key.ComponentSlot == componentSlot && IsOwner(entry.Value, owner))
			{
				_removeBuffer.Add(entry.Value);
			}
		}
		for (int i = 0; i < _removeBuffer.Count; i++)
		{
			RemoveEntry(_removeBuffer[i]);
		}
		_removeBuffer.Clear();
	}

	public void Tick(double delta, double ownerTimeScale)
	{
		if (_active.Count == 0 || !double.IsFinite(delta) || delta <= 0.0)
		{
			return;
		}
		double num = (double.IsFinite(ownerTimeScale) ? Math.Max(0.0, ownerTimeScale) : 0.0);
		_expired.Clear();
		_removeBuffer.Clear();
		int num2 = 0;
		while (num2 < _active.Count)
		{
			TimerEntry timerEntry = _active[num2];
			if (!timerEntry.Owner.TryGetTarget(out var target) || !GodotObject.IsInstanceValid(target))
			{
				RemoveEntry(timerEntry);
				continue;
			}
			double num3 = (timerEntry.UseOwnerTimeScale ? num : 1.0);
			timerEntry.RemainingSeconds -= delta * num3;
			if (timerEntry.RemainingSeconds > 0.0)
			{
				num2++;
				continue;
			}
			timerEntry.RemainingSeconds = 0.0;
			timerEntry.Running = false;
			RemoveActive(timerEntry);
			_expired.Add(timerEntry);
		}
		for (int i = 0; i < _expired.Count; i++)
		{
			TimerEntry timerEntry2 = _expired[i];
			if (_entries.TryGetValue(timerEntry2.Key, out var value) && value == timerEntry2 && !timerEntry2.Running && timerEntry2.Attached && timerEntry2.Owner.TryGetTarget(out var target2) && GodotObject.IsInstanceValid(target2))
			{
				target2.DispatchComponentTimerTimeout(new StringName(timerEntry2.Key.TimerName));
			}
		}
		_expired.Clear();
	}

	public void Clear()
	{
		_entries.Clear();
		_active.Clear();
		_expired.Clear();
		_removeBuffer.Clear();
	}

	private static bool IsUsable(ComponentBase owner, ComponentTimerKey key)
	{
		if (GodotObject.IsInstanceValid(owner) && !string.IsNullOrEmpty(key.ComponentSlot))
		{
			return !string.IsNullOrEmpty(key.TimerName);
		}
		return false;
	}

	private bool TryGetOwnedEntry(ComponentBase owner, ComponentTimerKey key, out TimerEntry entry)
	{
		if (_entries.TryGetValue(key, out entry))
		{
			return IsOwner(entry, owner);
		}
		return false;
	}

	private static bool IsOwner(TimerEntry entry, ComponentBase owner)
	{
		if (entry?.Owner != null && entry.Owner.TryGetTarget(out var target))
		{
			return target == owner;
		}
		return false;
	}

	private static double NormalizeDuration(double duration)
	{
		if (!double.IsFinite(duration))
		{
			return 0.0;
		}
		return Math.Max(0.0, duration);
	}

	private void RefreshActive(TimerEntry entry)
	{
		if (entry.Running && !entry.Paused && entry.Attached)
		{
			AddActive(entry);
		}
		else
		{
			RemoveActive(entry);
		}
	}

	private void AddActive(TimerEntry entry)
	{
		if (entry.ActiveIndex < 0)
		{
			entry.ActiveIndex = _active.Count;
			_active.Add(entry);
		}
	}

	private void RemoveActive(TimerEntry entry)
	{
		int activeIndex = entry.ActiveIndex;
		if (activeIndex < 0 || activeIndex >= _active.Count)
		{
			entry.ActiveIndex = -1;
			return;
		}
		int num = _active.Count - 1;
		if (activeIndex != num)
		{
			TimerEntry timerEntry = _active[num];
			_active[activeIndex] = timerEntry;
			timerEntry.ActiveIndex = activeIndex;
		}
		_active.RemoveAt(num);
		entry.ActiveIndex = -1;
	}

	private void RemoveEntry(TimerEntry entry)
	{
		RemoveActive(entry);
		_entries.Remove(entry.Key);
		entry.Running = false;
		entry.RemainingSeconds = 0.0;
	}
}
