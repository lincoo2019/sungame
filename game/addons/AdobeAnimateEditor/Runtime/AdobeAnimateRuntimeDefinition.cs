using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Godot;

public sealed class AdobeAnimateRuntimeDefinition
{
	public AdobeAnimateData Source;

	public AdobeAnimateAtlasProfile AtlasProfile;

	public Texture2D BaseAtlas;

	public Rid BaseAtlasRid;

	public Vector2 BaseAtlasSize;

	public int BaseAtlasPage;

	public TextureLayered AtlasTextureArray;

	public Rid AtlasTextureArrayRid;

	public Vector2 AtlasTextureArraySize;

	public bool UsesAtlasTextureArrayLayout;

	public Texture2D[] AtlasPages = Array.Empty<Texture2D>();

	public Rid[] AtlasPageRids = Array.Empty<Rid>();

	public int[] MediaAtlasPages = Array.Empty<int>();

	public Texture2D[] MediaTextures = Array.Empty<Texture2D>();

	public Rid[] MediaTextureRids = Array.Empty<Rid>();

	public Vector2[] MediaTextureSizes = Array.Empty<Vector2>();

	public PackedSliceRenderInfo[] MediaRenderInfos = Array.Empty<PackedSliceRenderInfo>();

	public bool HasSingleAtlasPage;

	public int SingleAtlasPage;

	public Texture2D SingleAtlasTexture;

	public Rid SingleAtlasTextureRid;

	public Vector2 SingleAtlasTextureSize;

	public Rect2[] MediaRects = Array.Empty<Rect2>();

	public PackedFrame[] Frames = Array.Empty<PackedFrame>();

	public PackedSliceMetadata[] SliceMetadata = Array.Empty<PackedSliceMetadata>();

	public PackedSlicePose[] Slices = Array.Empty<PackedSlicePose>();

	internal bool CpuPoseDataResident;

	internal readonly ConcurrentDictionary<long, PackedCpuPoseSample[]> CpuPoseTracks = new ConcurrentDictionary<long, PackedCpuPoseSample[]>();

	public ulong[] FrameLayoutSignatures = Array.Empty<ulong>();

	public ulong[] FramePoseSignatures = Array.Empty<ulong>();

	internal AdobeAnimateGpuLocalSlot[] GpuRenderLocalSlots = Array.Empty<AdobeAnimateGpuLocalSlot>();

	internal AdobeAnimateGpuFrameSlotEntry[] GpuFrameSlotLookup = Array.Empty<AdobeAnimateGpuFrameSlotEntry>();

	internal bool GpuRenderSlotOrderStable;

	public int MaxFrameSliceCount;

	public int RuntimeLayerCount;

	public bool HasRepeatedMediaPerFrame;

	public int[] NextSliceIndices = Array.Empty<int>();

	public bool[] HasNextSliceDeltas = Array.Empty<bool>();

	public float[] NextDeltaXx = Array.Empty<float>();

	public float[] NextDeltaXy = Array.Empty<float>();

	public float[] NextDeltaYx = Array.Empty<float>();

	public float[] NextDeltaYy = Array.Empty<float>();

	public float[] NextDeltaOx = Array.Empty<float>();

	public float[] NextDeltaOy = Array.Empty<float>();

	public float[] NextDeltaAlpha = Array.Empty<float>();

	internal bool CpuInterpolationDataResident;

	public Rect2 LocalBounds;

	public Rect2[] FrameLocalBounds = Array.Empty<Rect2>();

	public Texture2D GpuPoseTexture;

	public TextureLayered GpuPoseTextureArray;

	public Rid GpuPoseTextureRid;

	public Vector2I GpuPoseTextureSize;

	public int GpuPoseTextureBaseTexel;

	public int GpuPoseTextureLayer;

	public ulong GpuPoseSignature;

	public bool HasGpuPoseManifestEntry;

	public ulong GpuPoseManifestSignature;

	public int GpuPoseManifestTexelCount;

	public bool UsesBakedGpuPoseTexture;

	public bool UsesGpuPoseTextureArray;

	internal long SourceAuthoringRevision;

	internal bool CompactCrowdRecoveryAttempted;

	public PackedClip[] Clips = Array.Empty<PackedClip>();

	internal Dictionary<string, Vector2I> ClipRangesByName = new Dictionary<string, Vector2I>(StringComparer.Ordinal);

	public PackedEventFrame[] Events = Array.Empty<PackedEventFrame>();

	public Dictionary<StringName, int> MediaNameToId = new Dictionary<StringName, int>();

	public Dictionary<StringName, int> LayerNameToId = new Dictionary<StringName, int>();

	public PackedReplaceSlot[] ReplaceSlots = Array.Empty<PackedReplaceSlot>();

	public double FrameRate;

	public int FrameMax;

	public bool TryGetClipRange(string clipName, out Vector2I range)
	{
		if (clipName == null)
		{
			range = default(Vector2I);
			return false;
		}
		return ClipRangesByName.TryGetValue(clipName, out range);
	}

	public bool HasClip(string clipName)
	{
		if (clipName != null)
		{
			return ClipRangesByName.ContainsKey(clipName);
		}
		return false;
	}
}
