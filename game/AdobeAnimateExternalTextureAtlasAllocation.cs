using System;
using Godot;

public readonly struct AdobeAnimateExternalTextureAtlasAllocation(Texture2D texture, Rid textureRid, int atlasPage, Rect2 rect, TextureLayered textureArray = null, Rid textureArrayRid = default(Rid), Vector2 textureArraySize = default(Vector2), int textureArrayLayerOffset = 0, bool usesTextureArray = false)
{
	public Texture2D Texture { get; } = texture;

	public Rid TextureRid { get; } = textureRid;

	public TextureLayered TextureArray { get; } = textureArray;

	public Rid TextureArrayRid { get; } = textureArrayRid;

	public Vector2 TextureArraySize { get; } = textureArraySize;

	public int TextureArrayLayerOffset { get; } = Math.Max(0, textureArrayLayerOffset);

	public int AtlasPage { get; } = atlasPage;

	public Rect2 Rect { get; } = rect;

	public bool UsesTextureArray { get; } = usesTextureArray;
}
