using Godot;

public readonly struct PackedSliceRenderInfo(Vector2 sourceSize, Rect2 uvRect, int atlasLayer)
{
	public Vector2 SourceSize { get; } = sourceSize;

	public Rect2 UvRect { get; } = uvRect;

	public int AtlasLayer { get; } = atlasLayer;

	public bool IsValid
	{
		get
		{
			if (SourceSize.X > 0f)
			{
				return SourceSize.Y > 0f;
			}
			return false;
		}
	}
}
