using System;
using Godot;

public sealed class AdobeAnimateAtlasAllocation
{
	public Texture2D Texture;

	public Rid TextureRid;

	public TextureLayered TextureArray;

	public Rid TextureArrayRid;

	public Vector2 TextureArraySize;

	public int TextureArrayLayerOffset;

	public bool UsesTextureArrayLayout;

	public Texture2D[] Textures = Array.Empty<Texture2D>();

	public Rid[] TextureRids = Array.Empty<Rid>();

	public int[] MediaAtlasPages = Array.Empty<int>();

	public Rect2[] MediaRects = Array.Empty<Rect2>();

	public int PageIndex;

	public Rect2I PageRect;

	public Vector2I SourceSize;
}
