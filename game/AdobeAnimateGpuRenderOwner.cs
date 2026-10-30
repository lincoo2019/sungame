using Godot;

internal readonly struct AdobeAnimateGpuRenderOwner(AdobeAnimateRuntimeDefinition definition, int parentOwnerIndex, int topologyOrder, int depth, AdobeAnimateGpuAttachmentKind attachmentKind, int attachmentKey, int attachmentPoseLookupBase, int renderLayer, int localSlotCount, int frameLookupBase, bool usePos, bool useRotate, bool useScale, bool useSkew, Vector2 parentOffset, Vector2 slotOffset, float offsetRotate, bool useFollowVisible, Transform2D localTransform)
{
	public AdobeAnimateRuntimeDefinition Definition { get; } = definition;

	public int ParentOwnerIndex { get; } = parentOwnerIndex;

	public int TopologyOrder { get; } = topologyOrder;

	public int Depth { get; } = depth;

	public AdobeAnimateGpuAttachmentKind AttachmentKind { get; } = attachmentKind;

	public int AttachmentKey { get; } = attachmentKey;

	public int AttachmentPoseLookupBase { get; } = attachmentPoseLookupBase;

	public int RenderLayer { get; } = renderLayer;

	public int LocalSlotCount { get; } = localSlotCount;

	public int FrameLookupBase { get; } = frameLookupBase;

	public bool UsePos { get; } = usePos;

	public bool UseRotate { get; } = useRotate;

	public bool UseScale { get; } = useScale;

	public bool UseSkew { get; } = useSkew;

	public Vector2 ParentOffset { get; } = parentOffset;

	public Vector2 SlotOffset { get; } = slotOffset;

	public float OffsetRotate { get; } = offsetRotate;

	public bool UseFollowVisible { get; } = useFollowVisible;

	public Transform2D LocalTransform { get; } = localTransform;
}
