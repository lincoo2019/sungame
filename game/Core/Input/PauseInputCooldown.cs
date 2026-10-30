using System;

public sealed class PauseInputCooldown
{
	public const double DefaultCooldownSeconds = 0.25;

	private readonly double _cooldownSeconds;

	private double _readyAtSeconds;

	public PauseInputCooldown(double cooldownSeconds)
	{
		if (cooldownSeconds < 0.0)
		{
			throw new ArgumentOutOfRangeException("cooldownSeconds", "Cooldown must not be negative.");
		}
		_cooldownSeconds = cooldownSeconds;
	}

	public bool TryUse(double nowSeconds)
	{
		if (nowSeconds < _readyAtSeconds)
		{
			return false;
		}
		Restart(nowSeconds);
		return true;
	}

	public void Restart(double nowSeconds)
	{
		_readyAtSeconds = nowSeconds + _cooldownSeconds;
	}
}
