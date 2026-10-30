using System;
using Godot;

internal readonly struct AdobeAnimateCrowdResourceSignature(TextureLayered visualAtlas, Vector2 visualAtlasSize, TextureLayered poseTexture, Vector2I poseTextureSize, Texture2DArray gpuGraphTexture, Vector2I gpuGraphTextureSize, ImageTexture dynamicOverrideTexture, Vector2I dynamicOverrideTextureSize, AdobeAnimateRasterCompositeData rasterCompositeData = null) : IEquatable<AdobeAnimateCrowdResourceSignature>
{
	private readonly Rid _visualAtlasRid = GetRid(visualAtlas);

	private readonly Rid _poseTextureRid = GetRid(poseTexture);

	private readonly Rid _gpuGraphTextureRid = GetRid(gpuGraphTexture);

	private readonly Rid _dynamicOverrideTextureRid = GetRid(dynamicOverrideTexture);

	private readonly Rid _rasterCompositeAtlasRid = GetRid(rasterCompositeData?.atlas);

	public TextureLayered VisualAtlas { get; } = visualAtlas;

	public Vector2 VisualAtlasSize { get; } = visualAtlasSize;

	public TextureLayered PoseTexture { get; } = poseTexture;

	public Vector2I PoseTextureSize { get; } = poseTextureSize;

	public Texture2DArray GpuGraphTexture { get; } = gpuGraphTexture;

	public Vector2I GpuGraphTextureSize { get; } = gpuGraphTextureSize;

	public ImageTexture DynamicOverrideTexture { get; } = dynamicOverrideTexture;

	public Vector2I DynamicOverrideTextureSize { get; } = dynamicOverrideTextureSize;

	public Texture2D RasterCompositeAtlas { get; } = rasterCompositeData?.atlas;

	public Vector2I RasterCompositeAtlasSize { get; } = GodotObject.IsInstanceValid(rasterCompositeData?.atlas) ? new Vector2I(rasterCompositeData.atlas.GetWidth(), rasterCompositeData.atlas.GetHeight()) : Vector2I.Zero;

	public Vector2I RasterCompositeTileSize { get; } = rasterCompositeData?.tileSize ?? Vector2I.Zero;

	public Vector2 RasterCompositeOrigin { get; } = rasterCompositeData?.origin ?? Vector2.Zero;

	public int RasterCompositeColumns { get; } = rasterCompositeData?.columns ?? 0;

	public bool Equals(AdobeAnimateCrowdResourceSignature other)
	{
		if (_visualAtlasRid == other._visualAtlasRid && VisualAtlasSize == other.VisualAtlasSize && _poseTextureRid == other._poseTextureRid && PoseTextureSize == other.PoseTextureSize && _gpuGraphTextureRid == other._gpuGraphTextureRid && GpuGraphTextureSize == other.GpuGraphTextureSize && _dynamicOverrideTextureRid == other._dynamicOverrideTextureRid && DynamicOverrideTextureSize == other.DynamicOverrideTextureSize && _rasterCompositeAtlasRid == other._rasterCompositeAtlasRid && RasterCompositeAtlasSize == other.RasterCompositeAtlasSize && RasterCompositeTileSize == other.RasterCompositeTileSize && RasterCompositeOrigin == other.RasterCompositeOrigin)
		{
			return RasterCompositeColumns == other.RasterCompositeColumns;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is AdobeAnimateCrowdResourceSignature other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		HashCode hashCode = default(HashCode);
		hashCode.Add(_visualAtlasRid);
		hashCode.Add(VisualAtlasSize);
		hashCode.Add(_poseTextureRid);
		hashCode.Add(PoseTextureSize);
		hashCode.Add(_gpuGraphTextureRid);
		hashCode.Add(GpuGraphTextureSize);
		hashCode.Add(_dynamicOverrideTextureRid);
		hashCode.Add(DynamicOverrideTextureSize);
		hashCode.Add(_rasterCompositeAtlasRid);
		hashCode.Add(RasterCompositeAtlasSize);
		hashCode.Add(RasterCompositeTileSize);
		hashCode.Add(RasterCompositeOrigin);
		hashCode.Add(RasterCompositeColumns);
		return hashCode.ToHashCode();
	}

	public static bool operator ==(AdobeAnimateCrowdResourceSignature left, AdobeAnimateCrowdResourceSignature right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(AdobeAnimateCrowdResourceSignature left, AdobeAnimateCrowdResourceSignature right)
	{
		return !left.Equals(right);
	}

	private static Rid GetRid(TextureLayered texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return default(Rid);
		}
		return texture.GetRid();
	}

	private static Rid GetRid(ImageTexture texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return default(Rid);
		}
		return texture.GetRid();
	}

	private static Rid GetRid(Texture2D texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return default(Rid);
		}
		return texture.GetRid();
	}
}
