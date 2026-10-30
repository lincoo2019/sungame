using System;

internal readonly struct AdobeAnimateSortPath(int zIndex, int[] treeOrderPath, int layerOrder, int drawOrder, int stableOrder, int[] nestedPath = null) : IComparable<AdobeAnimateSortPath>
{
	private static readonly int[] EmptyNestedPath = Array.Empty<int>();

	private static readonly int[] EmptyTreeOrderPath = Array.Empty<int>();

	public int ZIndex { get; } = zIndex;

	public int[] TreeOrderPath { get; } = (treeOrderPath == null || treeOrderPath.Length == 0) ? EmptyTreeOrderPath : treeOrderPath;

	public int LayerOrder { get; } = layerOrder;

	public int DrawOrder { get; } = drawOrder;

	public int StableOrder { get; } = stableOrder;

	public int[] NestedPath { get; } = (nestedPath == null || nestedPath.Length == 0) ? EmptyNestedPath : nestedPath;

	private int[] SafeTreeOrderPath => TreeOrderPath ?? EmptyTreeOrderPath;

	private int[] SafeNestedPath => NestedPath ?? EmptyNestedPath;

	public AdobeAnimateSortPath AppendNested(int component, int stableOrder)
	{
		int[] safeNestedPath = SafeNestedPath;
		int[] array = new int[safeNestedPath.Length + 1];
		if (safeNestedPath.Length != 0)
		{
			Array.Copy(safeNestedPath, array, safeNestedPath.Length);
		}
		array[^1] = component;
		return new AdobeAnimateSortPath(ZIndex, TreeOrderPath, LayerOrder, DrawOrder, stableOrder, array);
	}

	public int CompareTo(AdobeAnimateSortPath other)
	{
		int num = ZIndex.CompareTo(other.ZIndex);
		if (num != 0)
		{
			return num;
		}
		int num2 = ComparePath(SafeTreeOrderPath, other.SafeTreeOrderPath);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = LayerOrder.CompareTo(other.LayerOrder);
		if (num3 != 0)
		{
			return num3;
		}
		int num4 = DrawOrder.CompareTo(other.DrawOrder);
		if (num4 != 0)
		{
			return num4;
		}
		int[] safeNestedPath = SafeNestedPath;
		int[] safeNestedPath2 = other.SafeNestedPath;
		int num5 = Math.Min(safeNestedPath.Length, safeNestedPath2.Length);
		for (int i = 0; i < num5; i++)
		{
			int num6 = safeNestedPath[i].CompareTo(safeNestedPath2[i]);
			if (num6 != 0)
			{
				return num6;
			}
		}
		int num7 = safeNestedPath.Length.CompareTo(safeNestedPath2.Length);
		if (num7 != 0)
		{
			return num7;
		}
		return StableOrder.CompareTo(other.StableOrder);
	}

	private static int ComparePath(int[] left, int[] right)
	{
		int val = ((left != null) ? left.Length : 0);
		int num = ((right != null) ? right.Length : 0);
		int num2 = Math.Min(val, num);
		for (int i = 0; i < num2; i++)
		{
			int num3 = left[i].CompareTo(right[i]);
			if (num3 != 0)
			{
				return num3;
			}
		}
		return val.CompareTo(num);
	}
}
