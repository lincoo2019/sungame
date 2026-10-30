using System.Collections.Generic;

internal sealed class AdobeAnimateCpuVisualBuildResult
{
	public readonly List<AdobeAnimateDrawItem> MergedDrawItems = new List<AdobeAnimateDrawItem>();

	public readonly List<AdobeAnimateCpuNativeSpriteItem> NativeSpriteItems = new List<AdobeAnimateCpuNativeSpriteItem>();

	public int ExpectedVisibleItems { get; internal set; }

	public int MergedVisibleItems { get; internal set; }

	public int NativeVisibleItems { get; internal set; }

	public AdobeAnimateCpuVisualFailure Failure { get; internal set; }

	public bool IsReady
	{
		get
		{
			if (!Failure.IsValid)
			{
				return MergedVisibleItems + NativeVisibleItems == ExpectedVisibleItems;
			}
			return false;
		}
	}
}
