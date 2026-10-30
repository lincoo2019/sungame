using Godot;

internal readonly struct AdobeAnimateCompositePoseRefreshState(AdobeAnimateRuntimeDefinition definition, Transform2D globalTransform, Color modulate, Vector2 offset, VerticalClipState verticalClip, int frameOffset, int frameCount, int poseBaseTexel, int poseLayer, float interpolationT, ulong frameLayoutSignature, ulong staticSignature)
{
	public AdobeAnimateRuntimeDefinition Definition { get; } = definition;

	public Transform2D GlobalTransform { get; } = globalTransform;

	public Color Modulate { get; } = modulate;

	public Vector2 Offset { get; } = offset;

	public VerticalClipState VerticalClip { get; } = verticalClip;

	public int FrameOffset { get; } = frameOffset;

	public int FrameCount { get; } = frameCount;

	public int PoseBaseTexel { get; } = poseBaseTexel;

	public int PoseLayer { get; } = poseLayer;

	public float InterpolationT { get; } = interpolationT;

	public ulong FrameLayoutSignature { get; } = frameLayoutSignature;

	public ulong StaticSignature { get; } = staticSignature;
}
