public readonly struct PackedFrame(int offset, int count)
{
	public int Offset { get; } = offset;

	public int Count { get; } = count;
}
