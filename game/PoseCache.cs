using System.Collections.Generic;

public sealed class PoseCache
{
	public readonly List<PackedSlicePose> Slices = new List<PackedSlicePose>();

	public void Clear()
	{
		Slices.Clear();
	}
}
