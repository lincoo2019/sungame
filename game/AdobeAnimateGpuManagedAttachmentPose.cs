using Godot;

internal readonly struct AdobeAnimateGpuManagedAttachmentPose(Transform2D transform, bool valid)
{
	public Vector2 Origin { get; } = transform.Origin;

	public float Rotation { get; } = transform.Rotation;

	public Vector2 Scale { get; } = transform.Scale;

	public float Skew { get; } = transform.Skew;

	public bool Valid { get; } = valid;
}
