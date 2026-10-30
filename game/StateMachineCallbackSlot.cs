using System;

internal readonly struct StateMachineCallbackSlot(string fullKey, StateMachineCallbackPhase phase) : IEquatable<StateMachineCallbackSlot>
{
	public string FullKey { get; } = fullKey ?? string.Empty;

	public StateMachineCallbackPhase Phase { get; } = phase;

	public bool Equals(StateMachineCallbackSlot other)
	{
		if (Phase == other.Phase)
		{
			return string.Equals(FullKey, other.FullKey, StringComparison.Ordinal);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is StateMachineCallbackSlot other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(StringComparer.Ordinal.GetHashCode(FullKey), (int)Phase);
	}
}
