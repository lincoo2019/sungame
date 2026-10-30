using Godot;

public readonly struct AdobeAnimatePoseAtlasAllocation(Texture2D texture, Rid textureRid, int atlasPage, int texelStart, int texelCount, Vector2I textureSize, ulong signature, TextureLayered textureArray = null, Rid textureArrayRid = default(Rid), bool usesTextureArray = false)
{
	public Texture2D Texture { get; } = texture;

	public Rid TextureRid { get; } = textureRid;

	public TextureLayered TextureArray { get; } = textureArray;

	public Rid TextureArrayRid { get; } = textureArrayRid;

	public int AtlasPage { get; } = atlasPage;

	public int TexelStart { get; } = texelStart;

	public int TexelCount { get; } = texelCount;

	public Vector2I TextureSize { get; } = textureSize;

	public ulong Signature { get; } = signature;

	public bool UsesTextureArray { get; } = usesTextureArray;
}
