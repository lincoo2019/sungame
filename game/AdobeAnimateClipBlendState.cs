using Godot;

internal readonly struct AdobeAnimateClipBlendState(bool enabled, float fromFrameFloat, float weight)
{
	public bool Enabled { get; } = enabled;

	public float FromFrameFloat { get; } = fromFrameFloat;

	public float Weight { get; } = Mathf.Clamp(weight, 0f, 1f);
}
