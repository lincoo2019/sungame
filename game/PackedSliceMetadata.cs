public readonly struct PackedSliceMetadata(int sliceKey, ushort mediaId, ushort layerId, int drawOrder, int flags)
{
	public int SliceKey { get; } = sliceKey;

	public ushort MediaId { get; } = mediaId;

	public ushort LayerId { get; } = layerId;

	public int DrawOrder { get; } = drawOrder;

	public int Flags { get; } = flags;
}
