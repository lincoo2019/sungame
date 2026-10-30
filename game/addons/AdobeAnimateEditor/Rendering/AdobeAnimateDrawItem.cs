using System;
using Godot;

internal readonly struct AdobeAnimateDrawItem(Transform2D transform, Vector2 size, Rect2 uvRect, int atlasLayer, Color color, AdobeAnimateSortPath sortPath, bool clipEnabled = false, float clipUp = 0f, float clipDown = 0f, AdobeAnimateSprite owner = null, bool useShaderPose = false, int poseTexel = 0, int poseLayer = 0, float poseFrameT = 0f, Vector2 poseOffset = default(Vector2), bool useVisualOverride = false, float clipLeft = 0f, float clipRight = 0f)
{
	public Transform2D Transform { get; } = transform;

	public Vector2 Size { get; } = size;

	public Rect2 UvRect { get; } = uvRect;

	public int AtlasLayer { get; } = Math.Max(0, atlasLayer);

	public Color Color { get; } = color;

	public AdobeAnimateSortPath SortPath { get; } = sortPath;

	public bool ClipEnabled { get; } = clipEnabled;

	public float ClipUp { get; } = clipUp;

	public float ClipDown { get; } = clipDown;

	public AdobeAnimateSprite Owner { get; } = owner;

	public bool UseShaderPose { get; } = useShaderPose;

	public int PoseTexel { get; } = Math.Max(0, poseTexel);

	public int PoseLayer { get; } = Math.Max(0, poseLayer);

	public float PoseFrameT { get; } = Mathf.Clamp(poseFrameT, 0f, 1f);

	public Vector2 PoseOffset { get; } = poseOffset;

	public bool UseVisualOverride { get; } = useVisualOverride;

	public float ClipLeft { get; } = clipLeft;

	public float ClipRight { get; } = clipRight;
}
