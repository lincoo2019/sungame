public struct VerticalClipState
{
	public bool Enabled;

	public float UpY;

	public float DownY;

	public static VerticalClipState Disabled => new VerticalClipState
	{
		Enabled = false,
		UpY = -10000f,
		DownY = 10000f
	};
}
