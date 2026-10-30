using Godot;

internal readonly record struct AdobeAnimateCpuNativeSpriteItem(Sprite2D Sprite, AdobeAnimateSortPath SortPath, string ResourceIdentity, AdobeAnimateCpuNativeDrawBand DrawBand, int NativeDrawIndex)
{
	public AdobeAnimateCpuNativeSpriteItem WithDrawBand(AdobeAnimateCpuNativeDrawBand drawBand)
	{
		return this with
		{
			DrawBand = drawBand
		};
	}

	public AdobeAnimateCpuNativeSpriteItem WithNativeDrawIndex(int nativeDrawIndex)
	{
		return this with
		{
			NativeDrawIndex = nativeDrawIndex
		};
	}
}
