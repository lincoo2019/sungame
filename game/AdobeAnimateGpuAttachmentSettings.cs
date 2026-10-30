using Godot;

internal readonly struct AdobeAnimateGpuAttachmentSettings(AdobeAnimateGpuAttachmentKind kind, int key, int poseLookupBase, bool usePos, bool useRotate, bool useScale, bool useSkew, Vector2 parentOffset, Vector2 slotOffset, float offsetRotate, bool useFollowVisible, Transform2D localTransform)
{
	public static AdobeAnimateGpuAttachmentSettings Root => new AdobeAnimateGpuAttachmentSettings(AdobeAnimateGpuAttachmentKind.Root, -1, -1, usePos: true, useRotate: true, useScale: true, useSkew: true, Vector2.Zero, Vector2.Zero, 0f, useFollowVisible: false, Transform2D.Identity);

	public AdobeAnimateGpuAttachmentKind Kind { get; } = kind;

	public int Key { get; } = key;

	public int PoseLookupBase { get; } = poseLookupBase;

	public bool UsePos { get; } = usePos;

	public bool UseRotate { get; } = useRotate;

	public bool UseScale { get; } = useScale;

	public bool UseSkew { get; } = useSkew;

	public Vector2 ParentOffset { get; } = parentOffset;

	public Vector2 SlotOffset { get; } = slotOffset;

	public float OffsetRotate { get; } = offsetRotate;

	public bool UseFollowVisible { get; } = useFollowVisible;

	public Transform2D LocalTransform { get; } = localTransform;

	public AdobeAnimateGpuAttachmentSettings WithPoseLookupBase(int poseLookupBase)
	{
		return new AdobeAnimateGpuAttachmentSettings(Kind, Key, poseLookupBase, UsePos, UseRotate, UseScale, UseSkew, ParentOffset, SlotOffset, OffsetRotate, UseFollowVisible, LocalTransform);
	}
}
