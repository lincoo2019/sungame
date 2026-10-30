internal readonly struct AdobeAnimateGpuRenderGraphAllocation(ulong signature, int page, int baseTexel, int texelCount, int renderSlotCount, int ownerCount)
{
	public ulong Signature { get; } = signature;

	public int Page { get; } = page;

	public int BaseTexel { get; } = baseTexel;

	public int TexelCount { get; } = texelCount;

	public int RenderSlotCount { get; } = renderSlotCount;

	public int OwnerCount { get; } = ownerCount;
}
