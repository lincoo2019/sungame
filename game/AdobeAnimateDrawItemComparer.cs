using System.Collections.Generic;

internal sealed class AdobeAnimateDrawItemComparer : IComparer<AdobeAnimateDrawItem>
{
	public static readonly AdobeAnimateDrawItemComparer Instance = new AdobeAnimateDrawItemComparer();

	private AdobeAnimateDrawItemComparer()
	{
	}

	public int Compare(AdobeAnimateDrawItem left, AdobeAnimateDrawItem right)
	{
		return left.SortPath.CompareTo(right.SortPath);
	}
}
