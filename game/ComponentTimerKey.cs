using System;
using Godot;

internal readonly struct ComponentTimerKey : IEquatable<ComponentTimerKey>
{
	public string ComponentSlot { get; }

	public string TimerName { get; }

	public ComponentTimerKey(string componentSlot, StringName timerName)
	{
		ComponentSlot = componentSlot ?? string.Empty;
		TimerName = (timerName.IsEmpty ? string.Empty : timerName.ToString());
	}

	public bool Equals(ComponentTimerKey other)
	{
		if (string.Equals(ComponentSlot, other.ComponentSlot, StringComparison.Ordinal))
		{
			return string.Equals(TimerName, other.TimerName, StringComparison.Ordinal);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ComponentTimerKey other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(StringComparer.Ordinal.GetHashCode(ComponentSlot), StringComparer.Ordinal.GetHashCode(TimerName));
	}
}
