using Godot;

internal readonly struct AdobeAnimateCrowdResourceRequirements(TextureLayered visualAtlas, Vector2 visualAtlasSize, bool requiresPoseTexture, TextureLayered poseTexture, Vector2I poseTextureSize, bool requiresGpuGraphTexture, Texture2DArray gpuGraphTexture, Vector2I gpuGraphTextureSize, bool requiresDynamicOverride, AdobeAnimateGpuDynamicOverrideAtlas dynamicOverrideProvider, AdobeAnimateRasterCompositeData rasterCompositeData)
{
	public TextureLayered VisualAtlas { get; } = visualAtlas;

	public Vector2 VisualAtlasSize { get; } = visualAtlasSize;

	public bool RequiresPoseTexture { get; } = requiresPoseTexture;

	public TextureLayered PoseTexture { get; } = poseTexture;

	public Vector2I PoseTextureSize { get; } = poseTextureSize;

	public bool RequiresGpuGraphTexture { get; } = requiresGpuGraphTexture;

	public Texture2DArray GpuGraphTexture { get; } = gpuGraphTexture;

	public Vector2I GpuGraphTextureSize { get; } = gpuGraphTextureSize;

	public bool RequiresDynamicOverride { get; } = requiresDynamicOverride;

	public AdobeAnimateGpuDynamicOverrideAtlas DynamicOverrideProvider { get; } = dynamicOverrideProvider;

	public AdobeAnimateRasterCompositeData RasterCompositeData { get; } = rasterCompositeData;

	public bool IsExactMatch(in AdobeAnimateCrowdResourceRequirements other)
	{
		if (VisualAtlas == other.VisualAtlas && VisualAtlasSize == other.VisualAtlasSize && RequiresPoseTexture == other.RequiresPoseTexture && PoseTexture == other.PoseTexture && PoseTextureSize == other.PoseTextureSize && RequiresGpuGraphTexture == other.RequiresGpuGraphTexture && GpuGraphTexture == other.GpuGraphTexture && GpuGraphTextureSize == other.GpuGraphTextureSize && RequiresDynamicOverride == other.RequiresDynamicOverride && DynamicOverrideProvider == other.DynamicOverrideProvider)
		{
			return RasterCompositeData == other.RasterCompositeData;
		}
		return false;
	}
}
