using System;
using Godot;
using Godot.Collections;

internal sealed class AdobeAnimateCrowdRenderState
{
	private Vector2 _gpuGraphTransformBasisX;

	private Vector2 _gpuGraphTransformBasisY;

	public AdobeAnimateCrowdRenderMode Mode { get; private set; }

	public AdobeAnimateRenderSnapshot Snapshot { get; private set; }

	public AdobeAnimateRuntimeDefinition Definition { get; private set; }

	public TextureLayered AtlasArray { get; private set; }

	public Vector2 AtlasArraySize { get; private set; }

	public TextureLayered PoseTextureArray { get; private set; }

	public Vector2I PoseTextureSize { get; private set; }

	public Node RenderMountParent { get; private set; }

	public Transform2D GlobalTransform { get; private set; }

	internal bool GpuGraphUseAbsoluteTransform { get; private set; }

	public Color Modulate { get; private set; }

	public Vector2 Offset { get; private set; }

	public VerticalClipState VerticalClip { get; private set; }

	public int FrameOffset { get; private set; }

	public int FrameCount { get; private set; }

	public int PoseBaseTexel { get; private set; }

	public int PoseLayer { get; private set; }

	public float InterpolationT { get; private set; }

	public bool AllLayersVisible { get; private set; }

	public bool CanUseLayerMask { get; private set; }

	public ulong LayerMask { get; private set; }

	public Array<bool> LayerVisible { get; private set; }

	public int EffectiveZIndex { get; private set; }

	public bool HasMediaReplace { get; private set; }

	public Array<Rect2> MediaReplaceRect { get; private set; }

	public Array<bool> MediaReplaceUse { get; private set; }

	public Array<int> MediaReplaceAtlasPages { get; private set; }

	public Vector2 MediaReplaceAtlasArraySize { get; private set; }

	public AdobeAnimateGpuRenderGraphAllocation GpuGraphAllocation { get; private set; }

	public Texture2DArray GpuGraphTextureArray { get; private set; }

	public Vector2I GpuGraphTextureSize { get; private set; }

	public int GpuGraphFrameIndex { get; private set; }

	public AdobeAnimateSprite[] GpuGraphOwners { get; private set; }

	public AdobeAnimateGpuGraphOwnerState GpuGraphRootOwnerState { get; private set; }

	public AdobeAnimateRootMotionState RootMotion { get; private set; }

	internal AdobeAnimateCrowdLayout PreparedGpuLayout { get; private set; }

	internal float[] PreparedRelocatableGpuState { get; private set; } = System.Array.Empty<float>();

	internal bool PreparedRelocatableGpuUseAbsoluteTransform { get; private set; }

	internal bool PreparedRelocatableGpuRequiresDynamicRefresh { get; private set; }

	public AdobeAnimateRasterCompositeData RasterCompositeData { get; private set; }

	public int RasterCompositeTileIndex { get; private set; }

	public AdobeAnimateCrowdRenderState(AdobeAnimateRuntimeDefinition definition, TextureLayered atlasArray, Vector2 atlasArraySize, TextureLayered poseTextureArray, Vector2I poseTextureSize, Node renderMountParent, Transform2D globalTransform, Color modulate, Vector2 offset, VerticalClipState verticalClip, int frameOffset, int frameCount, int poseBaseTexel, int poseLayer, float interpolationT, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int effectiveZIndex, bool hasMediaReplace = false, Array<Rect2> mediaReplaceRect = null, Array<bool> mediaReplaceUse = null, Array<int> mediaReplaceAtlasPages = null, Vector2 mediaReplaceAtlasArraySize = default(Vector2), AdobeAnimateRootMotionState rootMotion = default(AdobeAnimateRootMotionState))
		: this(AdobeAnimateCrowdRenderMode.Compact, default(AdobeAnimateRenderSnapshot), definition, atlasArray, atlasArraySize, poseTextureArray, poseTextureSize, renderMountParent, globalTransform, modulate, offset, verticalClip, frameOffset, frameCount, poseBaseTexel, poseLayer, interpolationT, allLayersVisible, canUseLayerMask, layerMask, layerVisible, effectiveZIndex, hasMediaReplace, mediaReplaceRect, mediaReplaceUse, mediaReplaceAtlasPages, mediaReplaceAtlasArraySize, default(AdobeAnimateGpuRenderGraphAllocation), null, default(Vector2I), 0, null, null, rootMotion)
	{
	}

	private AdobeAnimateCrowdRenderState(AdobeAnimateCrowdRenderMode mode, AdobeAnimateRenderSnapshot snapshot, AdobeAnimateRuntimeDefinition definition, TextureLayered atlasArray, Vector2 atlasArraySize, TextureLayered poseTextureArray, Vector2I poseTextureSize, Node renderMountParent, Transform2D globalTransform, Color modulate, Vector2 offset, VerticalClipState verticalClip, int frameOffset, int frameCount, int poseBaseTexel, int poseLayer, float interpolationT, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int effectiveZIndex, bool hasMediaReplace, Array<Rect2> mediaReplaceRect, Array<bool> mediaReplaceUse, Array<int> mediaReplaceAtlasPages, Vector2 mediaReplaceAtlasArraySize, AdobeAnimateGpuRenderGraphAllocation gpuGraphAllocation = default(AdobeAnimateGpuRenderGraphAllocation), Texture2DArray gpuGraphTextureArray = null, Vector2I gpuGraphTextureSize = default(Vector2I), int gpuGraphFrameIndex = 0, AdobeAnimateSprite[] gpuGraphOwners = null, AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState = null, AdobeAnimateRootMotionState rootMotion = default(AdobeAnimateRootMotionState), AdobeAnimateRasterCompositeData rasterCompositeData = null, int rasterCompositeTileIndex = 0)
	{
		Mode = mode;
		Snapshot = snapshot;
		Definition = definition;
		AtlasArray = atlasArray;
		AtlasArraySize = atlasArraySize;
		PoseTextureArray = poseTextureArray;
		PoseTextureSize = poseTextureSize;
		RenderMountParent = renderMountParent;
		GlobalTransform = globalTransform;
		_gpuGraphTransformBasisX = globalTransform.X;
		_gpuGraphTransformBasisY = globalTransform.Y;
		GpuGraphUseAbsoluteTransform = mode == AdobeAnimateCrowdRenderMode.GpuGraph && Mathf.IsZeroApprox(globalTransform.Determinant());
		Modulate = modulate;
		Offset = offset;
		VerticalClip = verticalClip;
		FrameOffset = frameOffset;
		FrameCount = frameCount;
		PoseBaseTexel = poseBaseTexel;
		PoseLayer = poseLayer;
		InterpolationT = interpolationT;
		AllLayersVisible = allLayersVisible;
		CanUseLayerMask = canUseLayerMask;
		LayerMask = layerMask;
		LayerVisible = layerVisible;
		EffectiveZIndex = effectiveZIndex;
		HasMediaReplace = hasMediaReplace;
		MediaReplaceRect = mediaReplaceRect;
		MediaReplaceUse = mediaReplaceUse;
		MediaReplaceAtlasPages = mediaReplaceAtlasPages;
		MediaReplaceAtlasArraySize = mediaReplaceAtlasArraySize;
		GpuGraphAllocation = gpuGraphAllocation;
		GpuGraphTextureArray = gpuGraphTextureArray;
		GpuGraphTextureSize = gpuGraphTextureSize;
		GpuGraphFrameIndex = gpuGraphFrameIndex;
		GpuGraphOwners = gpuGraphOwners ?? System.Array.Empty<AdobeAnimateSprite>();
		GpuGraphRootOwnerState = gpuGraphRootOwnerState;
		RootMotion = rootMotion;
		RasterCompositeData = rasterCompositeData;
		RasterCompositeTileIndex = Math.Max(0, rasterCompositeTileIndex);
	}

	public static AdobeAnimateCrowdRenderState CreateComposite(AdobeAnimateRenderSnapshot snapshot, TextureLayered atlasArray, Vector2 atlasArraySize, TextureLayered poseTextureArray, Vector2I poseTextureSize, AdobeAnimateRootMotionState rootMotion = default(AdobeAnimateRootMotionState))
	{
		return new AdobeAnimateCrowdRenderState(AdobeAnimateCrowdRenderMode.Composite, snapshot, snapshot.Definition, atlasArray, atlasArraySize, poseTextureArray, poseTextureSize, snapshot.RenderMountParent, snapshot.GlobalTransform, snapshot.Modulate, snapshot.Offset, snapshot.VerticalClip, 0, 0, 0, 0, 0f, snapshot.AllLayersVisible, snapshot.CanUseLayerMask, snapshot.LayerMask, snapshot.LayerVisible, snapshot.EffectiveZIndex, snapshot.HasMediaReplace, snapshot.MediaReplaceRect, snapshot.MediaReplaceUse, snapshot.MediaReplaceAtlasPages, snapshot.MediaReplaceAtlasArraySize, default(AdobeAnimateGpuRenderGraphAllocation), null, default(Vector2I), 0, null, null, rootMotion);
	}

	public static AdobeAnimateCrowdRenderState CreateGpuGraph(AdobeAnimateRuntimeDefinition definition, TextureLayered atlasArray, Vector2 atlasArraySize, TextureLayered poseTextureArray, Vector2I poseTextureSize, AdobeAnimateGpuRenderGraphAllocation gpuGraphAllocation, Texture2DArray gpuGraphTextureArray, Vector2I gpuGraphTextureSize, Node renderMountParent, Transform2D globalTransform, Color modulate, Vector2 offset, VerticalClipState verticalClip, int frameIndex, float interpolationT, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int effectiveZIndex, AdobeAnimateSprite[] gpuGraphOwners, AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState, AdobeAnimateRootMotionState rootMotion = default(AdobeAnimateRootMotionState))
	{
		return new AdobeAnimateCrowdRenderState(AdobeAnimateCrowdRenderMode.GpuGraph, default(AdobeAnimateRenderSnapshot), definition, atlasArray, atlasArraySize, poseTextureArray, poseTextureSize, renderMountParent, globalTransform, modulate, offset, verticalClip, 0, 0, definition?.GpuPoseTextureBaseTexel ?? 0, definition?.GpuPoseTextureLayer ?? 0, interpolationT, allLayersVisible, canUseLayerMask, layerMask, layerVisible, effectiveZIndex, hasMediaReplace: false, null, null, null, default(Vector2), gpuGraphAllocation, gpuGraphTextureArray, gpuGraphTextureSize, Math.Max(0, frameIndex), gpuGraphOwners, gpuGraphRootOwnerState, rootMotion);
	}

	public AdobeAnimateCrowdRenderState ResetGpuGraph(AdobeAnimateRuntimeDefinition definition, TextureLayered atlasArray, Vector2 atlasArraySize, TextureLayered poseTextureArray, Vector2I poseTextureSize, in AdobeAnimateGpuRenderGraphAllocation gpuGraphAllocation, Texture2DArray gpuGraphTextureArray, in Vector2I gpuGraphTextureSize, Node renderMountParent, in Transform2D globalTransform, in Color modulate, in Vector2 offset, in VerticalClipState verticalClip, int frameIndex, float interpolationT, bool allLayersVisible, bool canUseLayerMask, ulong layerMask, Array<bool> layerVisible, int effectiveZIndex, AdobeAnimateSprite[] gpuGraphOwners, AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState, in AdobeAnimateRootMotionState rootMotion)
	{
		Mode = AdobeAnimateCrowdRenderMode.GpuGraph;
		Snapshot = default(AdobeAnimateRenderSnapshot);
		Definition = definition;
		AtlasArray = atlasArray;
		AtlasArraySize = atlasArraySize;
		PoseTextureArray = poseTextureArray;
		PoseTextureSize = poseTextureSize;
		RenderMountParent = renderMountParent;
		GlobalTransform = globalTransform;
		_gpuGraphTransformBasisX = globalTransform.X;
		_gpuGraphTransformBasisY = globalTransform.Y;
		GpuGraphUseAbsoluteTransform = Mathf.IsZeroApprox(globalTransform.Determinant());
		Modulate = modulate;
		Offset = offset;
		VerticalClip = verticalClip;
		FrameOffset = 0;
		FrameCount = 0;
		PoseBaseTexel = definition?.GpuPoseTextureBaseTexel ?? 0;
		PoseLayer = definition?.GpuPoseTextureLayer ?? 0;
		InterpolationT = interpolationT;
		AllLayersVisible = allLayersVisible;
		CanUseLayerMask = canUseLayerMask;
		LayerMask = layerMask;
		LayerVisible = layerVisible;
		EffectiveZIndex = effectiveZIndex;
		HasMediaReplace = false;
		MediaReplaceRect = null;
		MediaReplaceUse = null;
		MediaReplaceAtlasPages = null;
		MediaReplaceAtlasArraySize = default(Vector2);
		GpuGraphAllocation = gpuGraphAllocation;
		GpuGraphTextureArray = gpuGraphTextureArray;
		GpuGraphTextureSize = gpuGraphTextureSize;
		GpuGraphFrameIndex = Math.Max(0, frameIndex);
		GpuGraphOwners = gpuGraphOwners ?? System.Array.Empty<AdobeAnimateSprite>();
		GpuGraphRootOwnerState = gpuGraphRootOwnerState;
		RootMotion = rootMotion;
		RasterCompositeData = null;
		RasterCompositeTileIndex = 0;
		ClearPreparedRelocatableGpuState();
		return this;
	}

	public AdobeAnimateCrowdRenderState WithGpuGraphDynamicState(in Transform2D globalTransform, in Color modulate, int effectiveZIndex, AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState, in AdobeAnimateRootMotionState rootMotion)
	{
		if (_gpuGraphTransformBasisX != globalTransform.X || _gpuGraphTransformBasisY != globalTransform.Y)
		{
			_gpuGraphTransformBasisX = globalTransform.X;
			_gpuGraphTransformBasisY = globalTransform.Y;
			GpuGraphUseAbsoluteTransform = Mathf.IsZeroApprox(globalTransform.Determinant());
		}
		GlobalTransform = globalTransform;
		Modulate = modulate;
		EffectiveZIndex = effectiveZIndex;
		GpuGraphRootOwnerState = gpuGraphRootOwnerState;
		RootMotion = rootMotion;
		return this;
	}

	internal void ClearRootMotionForRender()
	{
		RootMotion = default(AdobeAnimateRootMotionState);
	}

	internal void StorePreparedRelocatableGpuState(in AdobeAnimateCrowdLayout layout, float[] state, bool useAbsoluteTransform, bool requiresDynamicRefresh)
	{
		PreparedGpuLayout = layout;
		PreparedRelocatableGpuState = state ?? System.Array.Empty<float>();
		PreparedRelocatableGpuUseAbsoluteTransform = useAbsoluteTransform;
		PreparedRelocatableGpuRequiresDynamicRefresh = requiresDynamicRefresh;
	}

	internal void ClearPreparedRelocatableGpuState()
	{
		PreparedGpuLayout = default(AdobeAnimateCrowdLayout);
		PreparedRelocatableGpuState = System.Array.Empty<float>();
		PreparedRelocatableGpuUseAbsoluteTransform = false;
		PreparedRelocatableGpuRequiresDynamicRefresh = false;
	}

	internal bool RefreshGpuGraphAtlasBinding(in AdobeAnimateGpuRenderGraphAllocation allocation, Texture2DArray textureArray, in Vector2I textureSize)
	{
		if (Mode != AdobeAnimateCrowdRenderMode.GpuGraph || allocation.Signature == 0L || allocation.Signature != GpuGraphAllocation.Signature || !GodotObject.IsInstanceValid(textureArray) || !textureArray.GetRid().IsValid || textureSize.X <= 0 || textureSize.Y <= 0)
		{
			return false;
		}
		bool num = allocation.Page != GpuGraphAllocation.Page || allocation.BaseTexel != GpuGraphAllocation.BaseTexel || allocation.TexelCount != GpuGraphAllocation.TexelCount || allocation.RenderSlotCount != GpuGraphAllocation.RenderSlotCount || allocation.OwnerCount != GpuGraphAllocation.OwnerCount;
		GpuGraphAllocation = allocation;
		GpuGraphTextureArray = textureArray;
		GpuGraphTextureSize = textureSize;
		if (num)
		{
			ClearPreparedRelocatableGpuState();
		}
		return true;
	}

	public static AdobeAnimateCrowdRenderState CreateRasterComposite(AdobeAnimateRuntimeDefinition definition, Node renderMountParent, Transform2D globalTransform, Color modulate, int effectiveZIndex, VerticalClipState verticalClip, AdobeAnimateRasterCompositeData rasterCompositeData, int rasterCompositeTileIndex)
	{
		return new AdobeAnimateCrowdRenderState(AdobeAnimateCrowdRenderMode.RasterComposite, default(AdobeAnimateRenderSnapshot), definition, definition?.AtlasTextureArray, definition?.AtlasTextureArraySize ?? Vector2.One, null, Vector2I.Zero, renderMountParent, globalTransform, modulate, Vector2.Zero, verticalClip, 0, 0, 0, 0, 0f, allLayersVisible: true, canUseLayerMask: true, 18446744073709551615uL, null, effectiveZIndex, hasMediaReplace: false, null, null, null, default(Vector2), default(AdobeAnimateGpuRenderGraphAllocation), null, default(Vector2I), 0, null, null, default(AdobeAnimateRootMotionState), rasterCompositeData, rasterCompositeTileIndex);
	}
}
