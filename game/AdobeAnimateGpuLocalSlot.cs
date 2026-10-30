internal readonly struct AdobeAnimateGpuLocalSlot(int sliceKey, int occurrence, int layerId, int drawOrder)
{
	public int SliceKey { get; } = sliceKey;

	public int Occurrence { get; } = occurrence;

	public int LayerId { get; } = layerId;

	public int DrawOrder { get; } = drawOrder;
}
