using System;

internal readonly struct AdobeAnimateGpuDynamicOverrideAllocation(ulong signature, int baseTexel, int mediaCount)
{
	public ulong Signature { get; } = signature;

	public int BaseTexel { get; } = baseTexel;

	public int MediaCount { get; } = Math.Max(0, mediaCount);
}
