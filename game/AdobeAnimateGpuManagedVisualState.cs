using System;
using Godot;

internal readonly struct AdobeAnimateGpuManagedVisualState
{
	public static AdobeAnimateGpuManagedVisualState Hidden => new AdobeAnimateGpuManagedVisualState(Vector2.One, new Rect2(Vector2.Zero, Vector2.Zero), 0, Vector2.Zero, Transform2D.Identity, Colors.White, Vector2.Zero, flipH: false, flipV: false, useRotate: false, useScale: false, useSkew: false, visible: false, blink: false);

	public Vector2 SourceSize { get; }

	public Rect2 UvRect { get; }

	public int AtlasLayer { get; }

	public Vector2 DrawOrigin { get; }

	public Transform2D LocalTransform { get; }

	public Color Modulate { get; }

	public Vector2 SlotOffset { get; }

	public bool FlipH { get; }

	public bool FlipV { get; }

	public bool UseRotate { get; }

	public bool UseScale { get; }

	public bool UseSkew { get; }

	public bool Visible { get; }

	public bool Blink { get; }

	public AdobeAnimateGpuManagedVisualState(Vector2 sourceSize, Rect2 uvRect, int atlasLayer, Vector2 drawOrigin, Transform2D localTransform, Color modulate, Vector2 slotOffset, bool flipH, bool flipV, bool useRotate, bool useScale, bool useSkew, bool visible, bool blink)
	{
		SourceSize = sourceSize;
		UvRect = uvRect;
		AtlasLayer = Math.Max(0, atlasLayer);
		DrawOrigin = drawOrigin;
		LocalTransform = localTransform;
		Modulate = modulate;
		SlotOffset = slotOffset;
		FlipH = flipH;
		FlipV = flipV;
		UseRotate = useRotate;
		UseScale = useScale;
		UseSkew = useSkew;
		Visible = visible;
		Blink = blink;
	}
}
