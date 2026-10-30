using Godot;

internal readonly struct AdobeAnimateGpuHitFlashState(float startTime, float strength, float duration)
{
	public float StartTime { get; } = Mathf.Max(0f, startTime);

	public float Strength { get; } = Mathf.Max(0f, strength);

	public float Duration { get; } = Mathf.Max(1E-06f, duration);

	public bool Enabled => Strength > 0f;
}
