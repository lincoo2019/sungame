using System;

public readonly struct EconomyAccountId : IEquatable<EconomyAccountId>
{
	public const string LocalValue = "local";

	public const string PeerPrefix = "peer:";

	public static EconomyAccountId Local { get; } = new EconomyAccountId("local");

	public string Value { get; }

	public bool IsValid => !string.IsNullOrEmpty(Value);

	public bool IsLocal => string.Equals(Value, "local", StringComparison.Ordinal);

	private EconomyAccountId(string value)
	{
		Value = value;
	}

	public static bool TryFromPeerId(string peerId, out EconomyAccountId accountId)
	{
		accountId = default(EconomyAccountId);
		if (!IsValidPeerId(peerId))
		{
			return false;
		}
		accountId = new EconomyAccountId("peer:" + peerId.Trim());
		return true;
	}

	public static bool TryParse(string value, out EconomyAccountId accountId)
	{
		accountId = default(EconomyAccountId);
		if (string.Equals(value, "local", StringComparison.Ordinal))
		{
			accountId = Local;
			return true;
		}
		if (string.IsNullOrWhiteSpace(value) || !value.StartsWith("peer:", StringComparison.Ordinal))
		{
			return false;
		}
		int length = "peer:".Length;
		return TryFromPeerId(value.Substring(length, value.Length - length), out accountId);
	}

	private static bool IsValidPeerId(string peerId)
	{
		if (string.IsNullOrWhiteSpace(peerId))
		{
			return false;
		}
		string text = peerId.Trim();
		foreach (char c in text)
		{
			if (char.IsWhiteSpace(c) || char.IsControl(c) || c == ':')
			{
				return false;
			}
		}
		return true;
	}

	public bool Equals(EconomyAccountId other)
	{
		return string.Equals(Value, other.Value, StringComparison.Ordinal);
	}

	public override bool Equals(object obj)
	{
		if (obj is EconomyAccountId other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (Value != null)
		{
			return StringComparer.Ordinal.GetHashCode(Value);
		}
		return 0;
	}

	public override string ToString()
	{
		return Value ?? string.Empty;
	}

	public static bool operator ==(EconomyAccountId left, EconomyAccountId right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(EconomyAccountId left, EconomyAccountId right)
	{
		return !left.Equals(right);
	}
}
