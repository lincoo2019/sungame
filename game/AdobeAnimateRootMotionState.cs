using Godot;

internal readonly struct AdobeAnimateRootMotionState(Transform2D previousRelativeTransform, bool enabled)
{
	public Transform2D PreviousRelativeTransform { get; } = previousRelativeTransform;

	public bool Enabled { get; } = enabled;
}
