using Godot;

internal sealed class AdobeAnimateCrowdResourceSignatureBuilder
{
	private TextureLayered _visualAtlas;

	private Vector2 _visualAtlasSize;

	private TextureLayered _poseTexture;

	private Vector2I _poseTextureSize;

	private Texture2DArray _gpuGraphTexture;

	private Vector2I _gpuGraphTextureSize;

	private AdobeAnimateGpuDynamicOverrideAtlas _dynamicOverrideProvider;

	private bool _hasVisualAtlas;

	private bool _hasPoseTexture;

	private bool _hasGpuGraphTexture;

	private bool _requiresDynamicOverride;

	private AdobeAnimateRasterCompositeData _rasterCompositeData;

	private bool _hasRasterComposite;

	private int _conflictCount;

	public bool HasVisualAtlas => _hasVisualAtlas;

	public bool RequiresDynamicOverride => _requiresDynamicOverride;

	public int ConflictCount => _conflictCount;

	public void Reset()
	{
		_visualAtlas = null;
		_visualAtlasSize = Vector2.Zero;
		_poseTexture = null;
		_poseTextureSize = Vector2I.Zero;
		_gpuGraphTexture = null;
		_gpuGraphTextureSize = Vector2I.Zero;
		_dynamicOverrideProvider = null;
		_hasVisualAtlas = false;
		_hasPoseTexture = false;
		_hasGpuGraphTexture = false;
		_requiresDynamicOverride = false;
		_rasterCompositeData = null;
		_hasRasterComposite = false;
		_conflictCount = 0;
	}

	public bool TryInclude(in AdobeAnimateCrowdResourceRequirements requirements)
	{
		if (TryIncludeExistingResources(in requirements))
		{
			return true;
		}
		if (!IsValid(requirements.VisualAtlas, requirements.VisualAtlasSize) || (requirements.RequiresPoseTexture && !IsValid(requirements.PoseTexture, requirements.PoseTextureSize)) || (requirements.RequiresGpuGraphTexture && !IsValid(requirements.GpuGraphTexture, requirements.GpuGraphTextureSize)) || (requirements.RequiresDynamicOverride && requirements.DynamicOverrideProvider == null) || !IsValid(requirements.RasterCompositeData))
		{
			return Reject();
		}
		bool num = _hasVisualAtlas && !IsSame(_visualAtlas, _visualAtlasSize, requirements.VisualAtlas, requirements.VisualAtlasSize);
		bool flag = requirements.RequiresPoseTexture && _hasPoseTexture && !IsSame(_poseTexture, _poseTextureSize, requirements.PoseTexture, requirements.PoseTextureSize);
		bool flag2 = requirements.RequiresGpuGraphTexture && _hasGpuGraphTexture && !IsSame(_gpuGraphTexture, _gpuGraphTextureSize, requirements.GpuGraphTexture, requirements.GpuGraphTextureSize);
		bool flag3 = requirements.RequiresDynamicOverride && _requiresDynamicOverride && _dynamicOverrideProvider != requirements.DynamicOverrideProvider;
		bool flag4 = requirements.RasterCompositeData != null && _hasRasterComposite && !IsSame(_rasterCompositeData, requirements.RasterCompositeData);
		if (num || flag || flag2 || flag3 || flag4)
		{
			return Reject();
		}
		if (!_hasVisualAtlas)
		{
			_visualAtlas = requirements.VisualAtlas;
			_visualAtlasSize = requirements.VisualAtlasSize;
			_hasVisualAtlas = true;
		}
		if (requirements.RequiresPoseTexture && !_hasPoseTexture)
		{
			_poseTexture = requirements.PoseTexture;
			_poseTextureSize = requirements.PoseTextureSize;
			_hasPoseTexture = true;
		}
		if (requirements.RequiresGpuGraphTexture && !_hasGpuGraphTexture)
		{
			_gpuGraphTexture = requirements.GpuGraphTexture;
			_gpuGraphTextureSize = requirements.GpuGraphTextureSize;
			_hasGpuGraphTexture = true;
		}
		if (requirements.RequiresDynamicOverride && !_requiresDynamicOverride)
		{
			_dynamicOverrideProvider = requirements.DynamicOverrideProvider;
			_requiresDynamicOverride = true;
		}
		if (requirements.RasterCompositeData != null && !_hasRasterComposite)
		{
			_rasterCompositeData = requirements.RasterCompositeData;
			_hasRasterComposite = true;
		}
		return true;
	}

	private bool TryIncludeExistingResources(in AdobeAnimateCrowdResourceRequirements requirements)
	{
		if (!_hasVisualAtlas || _visualAtlas != requirements.VisualAtlas || _visualAtlasSize != requirements.VisualAtlasSize)
		{
			return false;
		}
		if (requirements.RequiresPoseTexture && (!_hasPoseTexture || _poseTexture != requirements.PoseTexture || _poseTextureSize != requirements.PoseTextureSize))
		{
			return false;
		}
		if (requirements.RequiresGpuGraphTexture && (!_hasGpuGraphTexture || _gpuGraphTexture != requirements.GpuGraphTexture || _gpuGraphTextureSize != requirements.GpuGraphTextureSize))
		{
			return false;
		}
		if (requirements.RasterCompositeData != null && (!_hasRasterComposite || !IsSame(_rasterCompositeData, requirements.RasterCompositeData)))
		{
			return false;
		}
		if (!requirements.RequiresDynamicOverride)
		{
			return true;
		}
		if (requirements.DynamicOverrideProvider == null || (_requiresDynamicOverride && _dynamicOverrideProvider != requirements.DynamicOverrideProvider))
		{
			return false;
		}
		if (!_requiresDynamicOverride)
		{
			_dynamicOverrideProvider = requirements.DynamicOverrideProvider;
			_requiresDynamicOverride = true;
		}
		return true;
	}

	public bool TryFreeze(ImageTexture dynamicOverrideTexture, Vector2I dynamicOverrideTextureSize, out AdobeAnimateCrowdResourceSignature signature)
	{
		signature = default(AdobeAnimateCrowdResourceSignature);
		if (!_hasVisualAtlas || !IsValid(_visualAtlas, _visualAtlasSize) || (_hasPoseTexture && !IsValid(_poseTexture, _poseTextureSize)) || (_hasGpuGraphTexture && !IsValid(_gpuGraphTexture, _gpuGraphTextureSize)))
		{
			return false;
		}
		if (_requiresDynamicOverride && !IsValid(dynamicOverrideTexture, dynamicOverrideTextureSize))
		{
			return false;
		}
		signature = new AdobeAnimateCrowdResourceSignature(_visualAtlas, _visualAtlasSize, _hasPoseTexture ? _poseTexture : null, _hasPoseTexture ? _poseTextureSize : Vector2I.Zero, _hasGpuGraphTexture ? _gpuGraphTexture : null, _hasGpuGraphTexture ? _gpuGraphTextureSize : Vector2I.Zero, _requiresDynamicOverride ? dynamicOverrideTexture : null, _requiresDynamicOverride ? dynamicOverrideTextureSize : Vector2I.Zero, _hasRasterComposite ? _rasterCompositeData : null);
		return true;
	}

	private bool Reject()
	{
		_conflictCount++;
		return false;
	}

	private static bool IsValid(TextureLayered texture, Vector2 size)
	{
		if (GodotObject.IsInstanceValid(texture) && texture.GetRid().IsValid && size.X > 0f)
		{
			return size.Y > 0f;
		}
		return false;
	}

	private static bool IsValid(TextureLayered texture, Vector2I size)
	{
		if (GodotObject.IsInstanceValid(texture) && texture.GetRid().IsValid && size.X > 0)
		{
			return size.Y > 0;
		}
		return false;
	}

	private static bool IsValid(ImageTexture texture, Vector2I size)
	{
		if (GodotObject.IsInstanceValid(texture) && texture.GetRid().IsValid && size.X > 0)
		{
			return size.Y > 0;
		}
		return false;
	}

	private static bool IsValid(AdobeAnimateRasterCompositeData data)
	{
		if (data != null)
		{
			if (GodotObject.IsInstanceValid(data) && GodotObject.IsInstanceValid(data.atlas) && data.atlas.GetRid().IsValid && data.atlas.GetWidth() > 0 && data.atlas.GetHeight() > 0 && data.tileSize.X > 0 && data.tileSize.Y > 0)
			{
				return data.columns > 0;
			}
			return false;
		}
		return true;
	}

	private static bool IsSame(AdobeAnimateRasterCompositeData left, AdobeAnimateRasterCompositeData right)
	{
		if (left == right)
		{
			return true;
		}
		if (IsValid(left) && IsValid(right) && left.atlas.GetRid() == right.atlas.GetRid() && left.tileSize == right.tileSize && left.origin == right.origin)
		{
			return left.columns == right.columns;
		}
		return false;
	}

	private static bool IsSame(TextureLayered left, Vector2 leftSize, TextureLayered right, Vector2 rightSize)
	{
		if (leftSize == rightSize && GodotObject.IsInstanceValid(left) && GodotObject.IsInstanceValid(right) && left.GetRid().IsValid)
		{
			return left.GetRid() == right.GetRid();
		}
		return false;
	}

	private static bool IsSame(TextureLayered left, Vector2I leftSize, TextureLayered right, Vector2I rightSize)
	{
		if (leftSize == rightSize && GodotObject.IsInstanceValid(left) && GodotObject.IsInstanceValid(right) && left.GetRid().IsValid)
		{
			return left.GetRid() == right.GetRid();
		}
		return false;
	}
}
