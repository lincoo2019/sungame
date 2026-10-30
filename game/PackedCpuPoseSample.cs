using Godot;

public readonly struct PackedCpuPoseSample(int mediaId, Transform2D transform, bool valid)
{
	public int MediaId { get; } = mediaId;

	public Transform2D Transform { get; } = transform;

	public bool Valid { get; } = valid;
}
