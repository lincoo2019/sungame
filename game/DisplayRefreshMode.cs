public sealed class DisplayRefreshMode
{
	public readonly int Id;

	public readonly int Width;

	public readonly int Height;

	public readonly int RefreshRate;

	public DisplayRefreshMode(int id, int width, int height, int refreshRate)
	{
		Id = id;
		Width = width;
		Height = height;
		RefreshRate = refreshRate;
	}
}
