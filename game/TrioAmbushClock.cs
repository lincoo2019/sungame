using System;

public sealed class TrioAmbushClock
{
	private double _iceUntil;

	public double Time { get; private set; }

	public double IceRemaining => Math.Max(0.0, _iceUntil - Time);

	public bool Frozen => _iceUntil - Time > 1E-09;

	public void Advance(double delta)
	{
		if (!double.IsFinite(delta) || delta < 0.0)
		{
			throw new ArgumentOutOfRangeException("delta");
		}
		Time += delta;
	}

	public void Freeze()
	{
		_iceUntil = Time + 3.0;
	}

	public void RestoreIce(double remaining)
	{
		_iceUntil = Time + (double.IsFinite(remaining) ? Math.Clamp(remaining, 0.0, 3.0) : 0.0);
	}
}
