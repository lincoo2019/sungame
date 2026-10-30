internal readonly struct AdobeAnimateGpuRenderSlot(int ownerIndex, int ownerTopologyOrder, int ownerLocalSlot, AdobeAnimateGpuRenderSlotKind kind, int staticVisualIndex)
{
	public int OwnerIndex { get; } = ownerIndex;

	public int OwnerTopologyOrder { get; } = ownerTopologyOrder;

	public int OwnerLocalSlot { get; } = ownerLocalSlot;

	public AdobeAnimateGpuRenderSlotKind Kind { get; } = kind;

	public int StaticVisualIndex { get; } = staticVisualIndex;
}
