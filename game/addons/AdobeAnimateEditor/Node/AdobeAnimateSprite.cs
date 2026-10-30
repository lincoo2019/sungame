using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Godot;
using Godot.Collections;

[Tool]
[GlobalClass]
public partial class AdobeAnimateSprite : Node2D
{
	private struct OwnedChildBinding
	{
		public AdobeAnimateSlot Slot;

		public AdobeAnimateSprite Sprite;

		public AdobeAnimateSlot OwnerSlot;
	}

	private struct InsertedSprite
	{
		public AdobeAnimateSprite sprite;

		public int layerId;
	}

	private readonly struct LayerActiveCacheKey(AdobeAnimateRuntimeDefinition definition, int layerCount, int startFrame, int endFrameExclusive) : IEquatable<LayerActiveCacheKey>
	{
		public AdobeAnimateRuntimeDefinition Definition { get; } = definition;

		public int LayerCount { get; } = layerCount;

		public int StartFrame { get; } = startFrame;

		public int EndFrameExclusive { get; } = endFrameExclusive;

		public bool Equals(LayerActiveCacheKey other)
		{
			if (Definition == other.Definition && LayerCount == other.LayerCount && StartFrame == other.StartFrame)
			{
				return EndFrameExclusive == other.EndFrameExclusive;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is LayerActiveCacheKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Definition, LayerCount, StartFrame, EndFrameExclusive);
		}
	}

	public delegate void AnimeCompletedEventHandler(string clip);

	public delegate void AnimeStopedEventHandler(string clip);

	public delegate void AnimeStartedEventHandler(string clip);

	public delegate void AnimeEventEventHandler(string command, Variant argument);

	public delegate void AnimeBlendCompletedEventHandler(string clip);

	internal enum EffectOnceGpuAdvanceResult
	{
		Invalidated,
		Running,
		Completed
	}

	private readonly struct EffectiveAncestorModulateCacheEntry(Node renderMountParent, Color value)
	{
		public Node RenderMountParent { get; } = renderMountParent;

		public Color Value { get; } = value;
	}

	private readonly struct StaticAtlasPathLayoutKey(int cacheVersion, ulong dataInstanceId, long authoringRevision, int mediaCount, int textureCount, int useCount, int pathCount, int effectiveLimit, int activeCount, ulong slotSignature) : IEquatable<StaticAtlasPathLayoutKey>
	{
		public int CacheVersion { get; } = cacheVersion;

		public ulong DataInstanceId { get; } = dataInstanceId;

		public long AuthoringRevision { get; } = authoringRevision;

		public int MediaCount { get; } = mediaCount;

		public int TextureCount { get; } = textureCount;

		public int UseCount { get; } = useCount;

		public int PathCount { get; } = pathCount;

		public int EffectiveLimit { get; } = effectiveLimit;

		public int ActiveCount { get; } = activeCount;

		public ulong SlotSignature { get; } = slotSignature;

		public bool Equals(StaticAtlasPathLayoutKey other)
		{
			if (CacheVersion == other.CacheVersion && DataInstanceId == other.DataInstanceId && AuthoringRevision == other.AuthoringRevision && MediaCount == other.MediaCount && TextureCount == other.TextureCount && UseCount == other.UseCount && PathCount == other.PathCount && EffectiveLimit == other.EffectiveLimit && ActiveCount == other.ActiveCount)
			{
				return SlotSignature == other.SlotSignature;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is StaticAtlasPathLayoutKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(CacheVersion, DataInstanceId, AuthoringRevision, MediaCount, TextureCount, UseCount, PathCount, HashCode.Combine(EffectiveLimit, ActiveCount, SlotSignature));
		}
	}

	private sealed class StaticAtlasPathLayoutCacheEntry
	{
		public StaticAtlasPathLayoutKey Key { get; }

		public AdobeAnimateData Data { get; }

		public bool[] Active { get; }

		public string[] Paths { get; }

		public Rect2[] Rects { get; }

		public int[] Pages { get; }

		public TextureLayered TextureArray { get; }

		public Rid TextureArrayRid { get; }

		public Vector2 TextureArraySize { get; }

		public StaticAtlasPathLayoutCacheEntry(in StaticAtlasPathLayoutKey key, AdobeAnimateData data, bool[] active, string[] paths, Rect2[] rects, int[] pages, TextureLayered textureArray, Rid textureArrayRid, Vector2 textureArraySize)
		{
			Key = key;
			Data = data;
			Active = active;
			Paths = paths;
			Rects = rects;
			Pages = pages;
			TextureArray = textureArray;
			TextureArrayRid = textureArrayRid;
			TextureArraySize = textureArraySize;
		}
	}

	public const int EmptyMediaId = 65535;

	internal const long ParentLayerSortBandStride = 1000000L;

	internal const long NestedLayerSortBandStride = 1L;

	internal const int InsertedLayerSortBandStride = 4;

	internal const long InsertedChildSortBandOffset = 1L;

	internal const long ManagedSpriteSortBandOffset = 1L;

	internal const long CharacterSortBandStride = 1000000000000L;

	private const long ShowBehindParentSortBandOffset = -1000000L;

	private const int RenderStateRescanInterval = 60;

	private const int RenderMountAuditPhysicsInterval = 60;

	private const int ManagedSlotGpuStateRescanInterval = 30;

	private const int EffectiveZIndexRescanInterval = 20;

	private const int EffectiveRenderLocalModulateRescanInterval = 60;

	private const int EffectOnceBatchNativeAuditInterval = 256;

	private const int GpuGraphCrowdStateAuditPhysicsInterval = 240;

	private const float GpuGraphNestedOwnerStateRebaseSeconds = 1f;

	private const int GpuGraphNestedOwnerStateRebaseBuckets = 60;

	private const float GpuGraphNestedOwnerPhaseTolerance = 0.001f;

	private const int EffectiveRenderModulateAncestorRescanInterval = 60;

	private const int TreeOrderFallbackRescanInterval = 60;

	private const int RuntimeRenderBoundsRescanInterval = 12;

	private const float RuntimeChildViewportPadding = 256f;

	private const float RuntimeCrowdViewportPrefetchPadding = 256f;

	private const float AtlasArrayLayerPackScale = 4096f;

	private const string ParentSpriteFollowLayerPropertyName = "Parent Sprite/Follow Layer";

	private const string ParentSpriteInsertLayerPropertyName = "Parent Sprite/Insert Layer";

	private const string ParentSpriteInsertTopLayerHint = "Top:-1";

	private const string ParentSpriteFallbackLayerHint = "0";

	private const string LegacyParentSpriteLayerPropertyName = "Layer";

	private static readonly bool CachedEditorHint = Engine.IsEditorHint();

	private const int LayerActiveCacheMaxEntries = 256;

	private static readonly System.Collections.Generic.Dictionary<LayerActiveCacheKey, bool[]> LayerActiveInClipCache = new System.Collections.Generic.Dictionary<LayerActiveCacheKey, bool[]>();

	private int _effectOnceBatchVisualRevision;

	private int _effectOnceBatchEligibilityRevision;

	private AdobeAnimateData _flashAnimeData;

	private Action _fileChangeAction;

	private bool _flashAnimeDataChangePending;

	private AdobeAnimateAtlasProfile _atlasProfileOverride;

	private bool _preview = true;

	private bool _forceLocalRender;

	private Control _renderClipControl;

	private bool _forceCpuPoseRender;

	[ExportGroup("Preset", "")]
	private bool _invisible;

	[Export(PropertyHint.None, "")]
	public bool normalAlpha;

	private Vector2 _offset = Vector2.Zero;

	[Export(PropertyHint.None, "")]
	public double offsetRotate;

	private double _timeScale = 1.0;

	[Export(PropertyHint.None, "")]
	public double trueFrameRate = 30.0;

	[Export(PropertyHint.None, "")]
	public bool refreshEveryFlame;

	[Export(PropertyHint.None, "")]
	public bool skipLastFrame = true;

	private bool _deduplicateLoopTerminalFrame = true;

	[Export(PropertyHint.None, "")]
	public bool usePos = true;

	[Export(PropertyHint.None, "")]
	public bool useRotate = true;

	[Export(PropertyHint.None, "")]
	public bool useFollowVisible;

	[ExportGroup("Animation", "")]
	[Export(PropertyHint.None, "")]
	public double blendTimeInit;

	[ExportGroup("", "")]
	private bool _pause;

	private bool _playBack;

	private bool _playbackBlocked;

	private bool _parentPlaybackStopped;

	private bool _frozenPreview;

	private bool _effectOnceGpuSuppressed;

	private int _effectOnceGpuSuppressionToken;

	private bool _applyingImmediateAnimation;

	public bool keepRenderSubmittedWhenPaused;

	private bool _runtimeViewportCullingEnabled = true;

	public bool onlyDraw;

	public double elapsedTimer;

	public double refreshTimer;

	public bool blend;

	public double blendTime;

	public double blendTimer;

	private float _blendFromFrameFloat;

	public int frameIndex;

	public double frameRate;

	public bool refreshEveryFrame;

	public bool loop = true;

	private string _clip = "";

	private ulong _playbackRevision;

	private ulong _managedPoseRevision;

	public Vector2I clipRange = Vector2I.Zero;

	public bool clipOver;

	private Array<bool> _layerVisible = new Array<bool>();

	private int _effectOnceBatchArrayExposureRevision;

	private bool _effectOnceBatchEligibilityTracking;

	public Texture2D mediaReplaceAtlas;

	public TextureLayered mediaReplaceAtlasArray;

	public Vector2 mediaReplaceAtlasArraySize;

	public bool mediaReplaceAtlasUsesTextureArray;

	public Array<Rect2> mediaReplaceRect = new Array<Rect2>();

	public Array<int> mediaReplaceAtlasPages = new Array<int>();

	private Array<Texture2D> _mediaReplace = new Array<Texture2D>();

	private Array<bool> _mediaReplaceUse = new Array<bool>();

	private Array<string> _mediaReplaceAtlasPaths = new Array<string>();

	public bool mediaReplaceAtlasShared;

	public List<AdobeAnimateTrack> track = new List<AdobeAnimateTrack>();

	public Rid canvasItem;

	public Rid meshTexture;

	public ArrayMesh mesh;

	public bool needMediaReplaceUpdate;

	private bool _canRun = true;

	public bool initClip;

	private AdobeAnimateSprite _parentSprite;

	private bool _parentSpriteResolved;

	[Export(PropertyHint.None, "")]
	public int followParentSpriteLayerId;

	[Export(PropertyHint.None, "")]
	public int insertLayerId = -1;

	private Color _meshColor = Colors.White;

	private Color _renderColorMultiplier = Colors.White;

	private AdobeAnimateGpuHitFlashState _gpuBrightFlash;

	private AdobeAnimateGpuHitFlashState _gpuWhiteFlash;

	private bool _runtimeGpuGraphActive;

	private bool _hasCachedGpuGraphCrowdState;

	private AdobeAnimateCrowdRenderState _cachedGpuGraphCrowdState;

	private ulong _cachedGpuGraphCrowdStatePhysicsFrame = 18446744073709551615uL;

	private bool _hasCachedRasterCompositeRenderState;

	private AdobeAnimateRasterCompositeRenderState _cachedRasterCompositeRenderState;

	private bool _gpuGraphCrowdStaticStateDirty;

	private bool _gpuGraphCrowdPresentationStateDirty;

	private ulong _gpuGraphCrowdStateNextAuditPhysicsFrame = 18446744073709551615uL;

	private bool _renderGrayscale;

	private readonly List<InsertedSprite> _insertedSprites = new List<InsertedSprite>();

	private bool _autoInserting;

	private AdobeAnimateSlot[] _slotChildren = System.Array.Empty<AdobeAnimateSlot>();

	private AdobeAnimateSlot[] _runtimeSlotChildren = System.Array.Empty<AdobeAnimateSlot>();

	private AdobeAnimateManagedSlotSprite[] _managedSlotSprites = System.Array.Empty<AdobeAnimateManagedSlotSprite>();

	private ulong[] _managedSlotGpuStateSignatures = System.Array.Empty<ulong>();

	private Rid[] _managedSlotGpuStateAtlasRids = System.Array.Empty<Rid>();

	private AdobeAnimateGpuManagedVisualState[] _managedSlotGpuStates = System.Array.Empty<AdobeAnimateGpuManagedVisualState>();

	private ulong[] _managedSlotGpuStateFastSignatures = System.Array.Empty<ulong>();

	private int[] _managedSlotGpuStateRescanCountdowns = System.Array.Empty<int>();

	private bool[] _managedSlotGpuStateKnown = System.Array.Empty<bool>();

	private bool[] _managedSlotGpuStateValid = System.Array.Empty<bool>();

	private Sprite2D[] _managedSlotGpuStateWatchedSprites = System.Array.Empty<Sprite2D>();

	private AdobeAnimatePart[] _managedSlotGpuStateWatchedParts = System.Array.Empty<AdobeAnimatePart>();

	private bool _runtimeSlotChildrenDirty = true;

	private bool _runtimeHasChildUpdates;

	private bool _runtimeChildUpdatesVisualOnlyDirty = true;

	private bool _runtimeChildUpdatesVisualOnlyCached;

	private bool _managedSlotSpritesDirty = true;

	private AdobeAnimateSprite[] _spriteChildren = System.Array.Empty<AdobeAnimateSprite>();

	private AdobeAnimateSlot[] _spriteChildOwnerSlots = System.Array.Empty<AdobeAnimateSlot>();

	private bool _hasChildren;

	private long _runtimeChildViewportFrame = -1L;

	private bool _runtimeChildViewportVisible = true;

	private int _lastUpdateFrame = -1;

	private bool _isVisibleInTree = true;

	private bool _runtimeInsideTree;

	private bool _processEnabled = true;

	private bool _usingRuntimeManager;

	private ulong _cachedInstanceIdForRender;

	private ulong _gpuGraphPreparedGeneration;

	private ulong _gpuGraphPreparedSignature;

	private int _gpuGraphPreparedQuadCount = 1;

	private object _preparedGpuRootCacheForRender;

	private bool _dispatchingBatchedProcess;

	private bool _displayFrameVisualDispatchActive;

	private ulong _runtimeDisplayTickVersion;

	private bool _runtimeReadyCached;

	private bool _runtimeNativeCanvasSuppressed;

	private long _runtimeNativeCanvasPublishedFrameVersion = -9223372036854775808L;

	private bool _runtimeManagerFirstFrameHandoffActive;

	private uint _runtimeNativeCanvasVisibilityLayer = 1u;

	private bool _runtimeNativeCanvasSuppressionEligibilityKnown;

	private bool _runtimeNativeCanvasSuppressionEligible;

	private const int RuntimeParentStateValidationSkipUpdates = 30;

	private AdobeAnimateSprite _runtimeParentStateSource;

	private int _runtimeParentStateValidationCountdown;

	private bool _runtimeFollowVisibilityKnown;

	private bool _runtimeFollowVisibilityValue;

	private bool _needsRenderSubmission = true;

	private bool _runtimeRenderSubmissionRetryRequested;

	private bool _runtimeGpuPrepareRequired = true;

	private static bool MissingMediaReplaceAtlasWarningPrinted;

	private CanvasItem _visibilityParentCanvas;

	private bool _selfVisibilityChangedConnected;

	private bool _parentVisibilityChangedConnected;

	private VerticalClipState _verticalClip = VerticalClipState.Disabled;

	private Node _cachedRenderMountParent;

	private Viewport _cachedViewport;

	private ulong _renderMountNextAuditPhysicsFrame;

	private readonly List<CanvasItem> _renderModulateAncestors = new List<CanvasItem>();

	private Node _renderModulateMountParent;

	private bool _renderModulateAncestorCacheReady;

	private int _renderModulateAncestorRescanCountdown;

	private int _cachedCanvasLayer;

	private AdobeAnimateRuntimeDefinition _runtimeDefinition;

	private AdobeAnimateRuntimeDefinition _cachedManagedPoseTrackDefinition;

	private int _cachedManagedPoseTrackKey = -1;

	private bool _cachedManagedPoseTrackUseLayerId;

	private PackedCpuPoseSample[] _cachedManagedPoseTrack = System.Array.Empty<PackedCpuPoseSample>();

	private double _nextAnimDelayTimer = -1.0;

	private AdobeAnimateTrack _pendingTrackConfig;

	private bool _renderStateCached;

	private bool _renderStaticStateCached;

	private bool _renderStaticLayerArrayEscaped = true;

	private bool _renderStaticMediaArraysEscaped = true;

	private bool _renderStaticArrayAuditSubmissionPending;

	private ulong _renderStaticStateNextAuditPhysicsFrame = 18446744073709551615uL;

	private Array<bool> _renderStaticAuditLayerVisibleSnapshot;

	private Array<bool> _renderStaticAuditMediaReplaceUseSnapshot;

	private Array<Texture2D> _renderStaticAuditMediaReplaceSnapshot;

	private Array<Rect2> _renderStaticAuditMediaReplaceRectSnapshot;

	private Array<int> _renderStaticAuditMediaReplacePageSnapshot;

	private Array<string> _renderStaticAuditMediaReplaceAtlasPathSnapshot;

	private AdobeAnimateGpuGraphOwnerStateTemplate _cachedGpuGraphOwnerStateTemplate;

	private ulong _cachedGpuGraphRootOwnerPlaybackRevision;

	private AdobeAnimateGpuGraphOwnerState _cachedGpuGraphRootOwnerState;

	private bool _hasCachedGpuGraphNestedOwnerState;

	private AdobeAnimateGpuGraphOwnerState _cachedGpuGraphNestedOwnerState;

	private ulong _cachedGpuGraphNestedOwnerPlaybackRevision;

	private float _gpuGraphNestedOwnerStateRebaseClockSeconds;

	private string _cachedClip = "";

	private Vector2I _cachedClipRange;

	private float _cachedFrameFloat;

	private Transform2D _cachedGlobalTransform;

	private Transform2D _renderGlobalTransform;

	private Transform2D _renderPreviousGlobalTransform;

	private bool _renderGlobalTransformCached;

	private bool _renderGlobalTransformDirty = true;

	private bool _renderTransformChangedInPhysicsFrame;

	private bool _runtimePhysicsAncestorTransformPending;

	private bool _knownAncestorTranslationNotificationPending;

	private bool _renderRootMotionEnabled;

	private bool _renderTreePaused;

	private ulong _renderGlobalTransformPhysicsFrame;

	private const float RenderMotionTeleportDistance = 96f;

	private Color _cachedModulate;

	private Vector2 _cachedOffset;

	private Texture2D _cachedMediaReplaceAtlas;

	private TextureLayered _cachedMediaReplaceAtlasArray;

	private Rid _cachedMediaReplaceAtlasRid;

	private Rid _cachedMediaReplaceAtlasArrayRid;

	private bool _cachedMediaReplaceAtlasShared;

	private bool _cachedMediaReplaceAtlasUsesTextureArray;

	private Node _cachedRenderMountParentSubmit;

	private int _cachedCanvasLayerSubmit;

	private int _cachedZIndex;

	private int _cachedEffectiveZIndex;

	private bool _effectiveZIndexCached;

	private int _effectiveZIndexCacheLocalZ;

	private bool _effectiveZIndexCacheZAsRelative;

	private int _effectiveZIndexRescanCountdown;

	private Transform2D _effectOnceBatchRenderTransform;

	private int _effectOnceBatchEffectiveZIndex;

	private int _effectOnceBatchRenderRevision;

	private int _effectOnceBatchObservedLocalZIndex = -2147483648;

	private bool _effectOnceBatchObservedZAsRelative;

	private ulong _effectOnceBatchRenderNextAuditFrame = 18446744073709551615uL;

	private bool _effectOnceBatchRenderStateDirty = true;

	private long _cachedEffectiveRenderSortBand;

	private bool _effectiveRenderSortBandCached;

	private long _effectiveRenderSortBandCacheLocalSortBand;

	private int _effectiveRenderSortBandRescanCountdown;

	private int[] _cachedEffectiveTreeOrderPath = System.Array.Empty<int>();

	private int _treeOrderPathRescanCountdown;

	private Node _treeOrderWatchedParent;

	private long _renderSortBand;

	private long _cachedSortBand;

	private bool _cachedUseDrawOrderSortBands;

	private bool _drawOrderSortBandCacheDirty = true;

	private bool _needsDrawOrderSortBandsCached;

	private bool _cachedRefreshEveryFrame;

	private ulong _cachedLayerMask;

	private bool _cachedAllLayersVisible;

	private bool _cachedConfiguredLayersAllVisible;

	private bool _cachedCanUseLayerMask;

	private int _cachedLayerVisibleCount;

	private int _cachedLayerVisibleValueCount;

	private bool[] _cachedLayerVisibleValues = System.Array.Empty<bool>();

	private ulong _cachedLayerVisibleSignature;

	private AdobeAnimateRasterCompositeData _cachedRasterCompositeSequenceData;

	private string _cachedRasterCompositeSequenceClip = "";

	private Vector2I _cachedRasterCompositeSequenceRange;

	private ulong _cachedRasterCompositeSequenceLayerSignature;

	private int _cachedRasterCompositeSequenceTileBase;

	private int _cachedRasterCompositeSequenceClipStart;

	private int _cachedRasterCompositeSequenceFrameCount;

	private bool _cachedRasterCompositeSequenceValid;

	private bool[] _cachedLayerActiveInCurrentClip = System.Array.Empty<bool>();

	private int _mediaReplaceStateVersion;

	private int _cachedMediaReplaceStateVersion;

	private int _cachedMediaReplaceLimit;

	private bool _cachedHasMediaReplace;

	private bool _cachedAnyRequestedMediaReplace;

	private ulong _cachedMediaReplaceUseMask;

	private bool _cachedMediaReplaceUseMaskOverflow;

	private int _cachedMediaReplaceAtlasPageCount;

	private bool[] _cachedMediaReplaceUseValues = System.Array.Empty<bool>();

	private Rect2[] _cachedMediaReplaceRects = System.Array.Empty<Rect2>();

	private int[] _cachedMediaReplaceAtlasPageValues = System.Array.Empty<int>();

	private ulong _cachedMediaReplaceSignature;

	private float _cachedMediaReplaceBoundsGrow;

	private VerticalClipState _cachedVerticalClip;

	private int _layerStateVersion;

	private int _cachedLayerStateVersion;

	private bool _runtimeManagerDispatchActive = true;

	private bool _displayFrameGpuClockPoseReusable;

	private const int RuntimeDisplayVisualRequirementValidationSkipUpdates = 30;

	private const int RuntimeGpuClockPoseReuseValidationSkipCalls = 8;

	private bool _runtimeDisplayVisualRequirementCached;

	private bool _cachedRuntimeDisplayVisualRequirement;

	private bool _cachedRuntimeDisplayGpuClockPoseReusable;

	private int _runtimeDisplayVisualRequirementValidationCountdown;

	private bool _runtimeGpuClockPoseReuseCached;

	private bool _cachedRuntimeGpuClockPoseReusable;

	private int _runtimeGpuClockPoseReuseValidationCountdown;

	private bool _runtimeGpuClockInterpolationActive;

	private float _runtimeGpuClockFramesPerSecond;

	private float _runtimeGpuClockClipStart;

	private float _runtimeGpuClockClipEndExclusive;

	private bool _runtimeGpuClockLoop;

	private bool _hasCachedRenderSnapshot;

	private AdobeAnimateRenderSnapshot _cachedRenderSnapshot;

	private AdobeAnimateRuntimeDefinition _loopTerminalAliasCacheDefinition;

	private Vector2I _loopTerminalAliasCacheRange;

	private bool _loopTerminalAliasCacheHasValue;

	private bool _loopTerminalAliasCacheValue;

	private AdobeAnimateRuntimeDefinition _playbackClipEndCacheDefinition;

	private Vector2I _playbackClipEndCacheRange;

	private bool _playbackClipEndCacheLoop;

	private bool _playbackClipEndCachePlayBack;

	private bool _playbackClipEndCacheHasValue;

	private int _playbackClipEndCacheValue;

	private bool _renderOrderDebugPending;

	private string _renderOrderDebugReason = "";

	private static readonly bool DebugRenderOrderTrace = false;

	private const int DebugRenderOrderTraceLimit = 800;

	private static int _debugRenderOrderTraceCount;

	private static readonly System.Collections.Generic.Dictionary<ulong, Rect2> CachedViewportWorldRects = new System.Collections.Generic.Dictionary<ulong, Rect2>();

	private static long CachedViewportWorldRectsFrame = -9223372036854775808L;

	private static int CachedViewportWorldRectRenderScopeDepth;

	private static long CachedRenderColorTransactionVersion;

	private long _cachedOwnRenderFrameModulateVersion = -9223372036854775808L;

	private Color _cachedOwnRenderFrameModulate;

	private long _cachedOwnRenderFrameSelfModulateVersion = -9223372036854775808L;

	private Color _cachedOwnRenderFrameSelfModulate;

	private bool _cachedRenderLocalModulateReady;

	private Color _cachedRenderLocalModulate;

	private bool _cachedRenderLocalSelfModulateReady;

	private Color _cachedRenderLocalSelfModulate;

	private int _renderLocalModulateRescanCountdown;

	private long _renderLocalModulateAuditTransactionVersion = -9223372036854775808L;

	private static Viewport CachedViewportWorldRectLastViewport;

	private static Rect2 CachedViewportWorldRectLastValue;

	private static readonly System.Collections.Generic.Dictionary<CanvasItem, Color> CachedRenderFrameModulates = new System.Collections.Generic.Dictionary<CanvasItem, Color>(ReferenceEqualityComparer.Instance);

	private static CanvasItem CachedRenderFrameLastModulateItem;

	private static Color CachedRenderFrameLastModulateValue;

	private bool _runtimeRenderBoundsCached;

	private bool _cachedRuntimeRenderBoundsVisible;

	private bool _cachedRuntimeRenderBoundsStrictVisible;

	private bool _cachedRuntimeRenderBoundsCullingEnabled;

	private AdobeAnimateRuntimeDefinition _cachedRuntimeRenderBoundsDefinition;

	private Transform2D _cachedRuntimeRenderBoundsTransform;

	private Vector2 _cachedRuntimeRenderBoundsOffset;

	private Node _cachedRuntimeRenderBoundsMountParent;

	private Viewport _cachedRuntimeRenderBoundsViewport;

	private Rect2 _cachedRuntimeRenderBoundsViewportWorldRect;

	private Rect2 _cachedRuntimeRenderBoundsRelativeRect;

	private bool _cachedRuntimeRenderBoundsHasRelativeRect;

	private int _runtimeRenderBoundsRescanCountdown;

	private readonly AdobeAnimateExternalVisualRegistry _externalVisuals = new AdobeAnimateExternalVisualRegistry();

	private long _externalVisualPreparationFrame = -9223372036854775808L;

	private ulong[] _externalVisualGpuStateSignatures = System.Array.Empty<ulong>();

	private ulong[] _externalVisualGpuStateVersions = System.Array.Empty<ulong>();

	private Rid[] _externalVisualGpuStateAtlasRids = System.Array.Empty<Rid>();

	private AdobeAnimateGpuManagedVisualState[] _externalVisualGpuStates = System.Array.Empty<AdobeAnimateGpuManagedVisualState>();

	private string[] _externalVisualGpuStateFailures = System.Array.Empty<string>();

	private bool[] _externalVisualGpuStateKnown = System.Array.Empty<bool>();

	private bool[] _externalVisualGpuStateValid = System.Array.Empty<bool>();

	private static readonly System.Collections.Generic.Dictionary<CanvasItem, EffectiveAncestorModulateCacheEntry> CachedRenderFrameEffectiveAncestorModulates = new System.Collections.Generic.Dictionary<CanvasItem, EffectiveAncestorModulateCacheEntry>();

	private const int StaticAtlasPathLayoutCacheCapacity = 256;

	private static readonly System.Collections.Generic.Dictionary<StaticAtlasPathLayoutKey, StaticAtlasPathLayoutCacheEntry> StaticAtlasPathLayoutCache = new System.Collections.Generic.Dictionary<StaticAtlasPathLayoutKey, StaticAtlasPathLayoutCacheEntry>();

	private static int StaticAtlasPathLayoutCacheVersion = -2147483648;

	private StaticAtlasPathLayoutCacheEntry _appliedStaticAtlasPathLayout;

	private bool _staticAtlasPathLayoutRuntimeMutationSeen;

	private bool _bypassStaticAtlasPathLayoutCacheForTests;

	private bool _staticAtlasPathLayoutGpuResetPending;

	private AdobeAnimateData _staticAtlasPathSnapshotData;

	private ulong _staticAtlasPathSnapshotDataInstanceId;

	private long _staticAtlasPathSnapshotAuthoringRevision;

	private int _staticAtlasPathSnapshotMediaCount;

	private int _staticAtlasPathSnapshotTextureCount;

	private int _staticAtlasPathSnapshotUseCount;

	private int _staticAtlasPathSnapshotPathCount;

	private int _staticAtlasPathSnapshotEffectiveLimit;

	private int _staticAtlasPathSnapshotActiveCount;

	private ulong _staticAtlasPathSnapshotSlotSignature;

	private bool[] _staticAtlasPathSnapshotActive = System.Array.Empty<bool>();

	private string[] _staticAtlasPathSnapshotPaths = System.Array.Empty<string>();

	private bool _staticAtlasPathSnapshotValid;

	private static readonly AdobeAnimateRuntimeDefinition SharedGpuClockBareTestDefinition = new AdobeAnimateRuntimeDefinition
	{
		Frames = new PackedFrame[1]
		{
			new PackedFrame(0, 0)
		},
		RuntimeLayerCount = 0
	};

	protected static bool IsEditorContext => CachedEditorHint;

	public new Color Modulate
	{
		get
		{
			return base.Modulate;
		}
		set
		{
			if (!_cachedRenderLocalModulateReady || !(_cachedRenderLocalModulate == value))
			{
				base.Modulate = value;
				_cachedRenderLocalModulate = value;
				_cachedRenderLocalModulateReady = true;
				_gpuGraphCrowdPresentationStateDirty = true;
				ScheduleNextRenderLocalModulateAudit();
				_effectOnceBatchVisualRevision++;
				MarkEffectOnceBatchEligibilityChanged();
			}
		}
	}

	public new Color SelfModulate
	{
		get
		{
			return base.SelfModulate;
		}
		set
		{
			if (!_cachedRenderLocalSelfModulateReady || !(_cachedRenderLocalSelfModulate == value))
			{
				base.SelfModulate = value;
				_cachedRenderLocalSelfModulate = value;
				_cachedRenderLocalSelfModulateReady = true;
				_gpuGraphCrowdPresentationStateDirty = true;
				ScheduleNextRenderLocalModulateAudit();
				_effectOnceBatchVisualRevision++;
				MarkEffectOnceBatchEligibilityChanged();
			}
		}
	}

	public new int ZIndex
	{
		get
		{
			return base.ZIndex;
		}
		set
		{
			if (base.ZIndex != value)
			{
				base.ZIndex = value;
				InvalidateEffectOnceBatchRenderState();
			}
		}
	}

	public new bool ZAsRelative
	{
		get
		{
			return base.ZAsRelative;
		}
		set
		{
			if (base.ZAsRelative != value)
			{
				base.ZAsRelative = value;
				InvalidateEffectOnceBatchRenderState();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public AdobeAnimateAtlasProfile atlasProfileOverride
	{
		get
		{
			return _atlasProfileOverride;
		}
		set
		{
			if (_atlasProfileOverride != value)
			{
				_atlasProfileOverride = value;
				MarkEffectOnceBatchEligibilityChanged();
				InvalidateStaticAtlasPathLayoutForNodeMutation();
				_runtimeDefinition = null;
				meshTexture = default(Rid);
				InvalidateRenderSnapshotCache(force: true);
				if (IsInsideTree())
				{
					AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.OwnerDefinition);
					RequestNodeRedraw();
					CanRun();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public AdobeAnimateData flashAnimeData
	{
		get
		{
			return _flashAnimeData;
		}
		set
		{
			if (_flashAnimeData == value)
			{
				return;
			}
			InvalidateStaticAtlasPathLayoutConfigurationSnapshot();
			_managedPoseRevision++;
			if (_flashAnimeData != null && _fileChangeAction != null)
			{
				_flashAnimeData.Changed -= _fileChangeAction;
			}
			_flashAnimeData = value;
			MarkEffectOnceBatchEligibilityChanged();
			if (DeferFlashAnimeDataChangeUntilReady())
			{
				PrepareFlashAnimeDataSerializedOverrides();
				_flashAnimeDataChangePending = true;
			}
			else
			{
				ApplyFlashAnimeDataChange(invalidateDefinitionCache: false);
				InvalidateRenderSnapshotCache(force: true);
			}
			if (_flashAnimeData != null)
			{
				if (_fileChangeAction == null)
				{
					_fileChangeAction = OnFlashAnimeDataChanged;
				}
				_flashAnimeData.Changed += _fileChangeAction;
			}
			if (CachedEditorHint)
			{
				NotifyPropertyListChanged();
			}
			if (IsInsideTree())
			{
				AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.OwnerDefinition);
				RequestNodeRedraw();
				CanRun();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool preview
	{
		get
		{
			return _preview;
		}
		set
		{
			if (_preview != value)
			{
				_preview = value;
				CanRun();
				RequestNodeRedraw();
			}
		}
	}

	public bool forceLocalRender
	{
		get
		{
			return _forceLocalRender;
		}
		set
		{
			if (_forceLocalRender != value)
			{
				AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
				RestoreRuntimeNativeCanvasLayer();
				_forceLocalRender = value;
				InvalidateRenderSnapshotCache(force: true);
				if (IsInsideTree())
				{
					RefreshRenderMountCache();
					RefreshTransformNotificationModeForRenderTree();
					SetProcessEnabled(_canRun && !clipOver);
				}
				RequestNodeRedraw();
			}
		}
	}

	public bool forceCpuPoseRender
	{
		get
		{
			return _forceCpuPoseRender;
		}
		set
		{
			if (_forceCpuPoseRender != value)
			{
				_forceCpuPoseRender = value;
				InvalidateRenderSnapshotCache(force: true);
				RequestNodeRedraw();
			}
		}
	}

	public bool suppressEditorRenderSubmission { get; set; }

	[Export(PropertyHint.None, "")]
	public bool invisible
	{
		get
		{
			return _invisible;
		}
		set
		{
			if (_invisible != value)
			{
				_invisible = value;
				CanRun();
				RequestNodeRedraw();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public Vector2 offset
	{
		get
		{
			return _offset;
		}
		set
		{
			if (!(_offset == value))
			{
				_offset = value;
				MarkEffectOnceBatchRenderStateDirty();
				_managedPoseRevision++;
				if (IsInsideTree())
				{
					UpdateChildPoseImmediate();
					InvalidateGpuRenderGraphForOffsetChange();
				}
				InvalidateRenderSnapshotCache();
				RequestNodeRedraw();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public double timeScale
	{
		get
		{
			return _timeScale;
		}
		set
		{
			if (!Mathf.IsEqualApprox((float)_timeScale, (float)value))
			{
				bool isPlaybackStopped = IsPlaybackStopped;
				_timeScale = value;
				_playbackRevision++;
				if (CommitPlaybackControlChange(isPlaybackStopped))
				{
					RefreshRuntimeChildPlaybackControl();
				}
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool deduplicateLoopTerminalFrame
	{
		get
		{
			return _deduplicateLoopTerminalFrame;
		}
		set
		{
			if (_deduplicateLoopTerminalFrame != value)
			{
				_deduplicateLoopTerminalFrame = value;
				_playbackClipEndCacheHasValue = false;
			}
		}
	}

	public bool IsFrozenPreview => _frozenPreview;

	[Export(PropertyHint.None, "")]
	public bool pause
	{
		get
		{
			return _pause;
		}
		set
		{
			if (_pause != value)
			{
				bool isPlaybackStopped = IsPlaybackStopped;
				_pause = value;
				CommitPlaybackControlChange(isPlaybackStopped);
				RefreshRuntimeChildPlaybackControl();
			}
		}
	}

	private bool IsPlaybackStopped
	{
		get
		{
			if (!_pause && !_playbackBlocked && !_parentPlaybackStopped)
			{
				return Math.Abs(_timeScale) <= 1E-06;
			}
			return true;
		}
	}

	internal bool IsEffectOnceGpuSuppressed => _effectOnceGpuSuppressed;

	[Export(PropertyHint.None, "")]
	public bool runtimeViewportCullingEnabled
	{
		get
		{
			return _runtimeViewportCullingEnabled;
		}
		set
		{
			if (_runtimeViewportCullingEnabled != value)
			{
				_runtimeViewportCullingEnabled = value;
				InvalidateRenderSnapshotCache(force: true);
				RequestNodeRedraw();
			}
		}
	}

	[Export(PropertyHint.None, "")]
	public bool playBack
	{
		get
		{
			return _playBack;
		}
		set
		{
			if (_playBack != value)
			{
				_playBack = value;
				_playbackRevision++;
				CommitPlaybackDirectionChange();
			}
		}
	}

	internal ulong ManagedPoseRevision => _managedPoseRevision;

	[Export(PropertyHint.None, "")]
	public string clip
	{
		get
		{
			return _clip;
		}
		set
		{
			SetClip(value);
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<bool> layerVisible
	{
		get
		{
			MarkEffectOnceBatchArrayExposure();
			MarkRenderStaticLayerArrayExposure();
			return _layerVisible;
		}
		set
		{
			MarkEffectOnceBatchArrayExposure();
			_layerVisible = value ?? new Array<bool>();
			MarkRenderStaticLayerArrayExposure();
			MarkLayerStateChanged();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<Texture2D> mediaReplace
	{
		get
		{
			MarkRenderStaticMediaArrayExposure();
			return _mediaReplace;
		}
		set
		{
			_mediaReplace = value ?? new Array<Texture2D>();
			MarkRenderStaticMediaArrayExposure();
			CaptureStaticAtlasPathLayoutConfigurationSnapshot();
			QueueUpdateMediaReplace();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<bool> mediaReplaceUse
	{
		get
		{
			MarkEffectOnceBatchArrayExposure();
			MarkRenderStaticMediaArrayExposure();
			return _mediaReplaceUse;
		}
		set
		{
			MarkEffectOnceBatchArrayExposure();
			_mediaReplaceUse = value ?? new Array<bool>();
			MarkRenderStaticMediaArrayExposure();
			MarkEffectOnceBatchEligibilityChanged();
			CaptureStaticAtlasPathLayoutConfigurationSnapshot();
			QueueUpdateMediaReplace();
		}
	}

	[Export(PropertyHint.None, "")]
	public Array<string> mediaReplaceAtlasPaths
	{
		get
		{
			MarkRenderStaticMediaArrayExposure();
			return _mediaReplaceAtlasPaths;
		}
		set
		{
			_mediaReplaceAtlasPaths = value ?? new Array<string>();
			MarkRenderStaticMediaArrayExposure();
			if (_mediaReplaceUse.Count < _mediaReplaceAtlasPaths.Count)
			{
				_mediaReplaceUse.Resize(_mediaReplaceAtlasPaths.Count);
			}
			for (int i = 0; i < _mediaReplaceAtlasPaths.Count; i++)
			{
				if (!string.IsNullOrWhiteSpace(_mediaReplaceAtlasPaths[i]))
				{
					_mediaReplaceUse[i] = true;
				}
			}
			CaptureStaticAtlasPathLayoutConfigurationSnapshot();
			InvalidateStaticAtlasPathLayoutForNodeMutation();
			QueueUpdateMediaReplace();
		}
	}

	[Export(PropertyHint.None, "")]
	public AdobeAnimateSprite parentSprite
	{
		get
		{
			return _parentSprite;
		}
		set
		{
			_parentSpriteResolved = true;
			if (_parentSprite == value)
			{
				if (!CachedEditorHint)
				{
					SetParentPlaybackStopped(GodotObject.IsInstanceValid(_parentSprite) && _parentSprite.IsPlaybackStopped);
				}
				return;
			}
			AdobeAnimateSprite adobeAnimateSprite = _parentSprite;
			_parentSprite = value;
			if (!CachedEditorHint)
			{
				SetParentPlaybackStopped(GodotObject.IsInstanceValid(_parentSprite) && _parentSprite.IsPlaybackStopped);
			}
			RefreshTransformNotificationMode();
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.OnSpriteChildParentChanged(this);
			}
			if (GodotObject.IsInstanceValid(_parentSprite))
			{
				_parentSprite.MarkManagedSlotSpriteCacheDirty();
			}
			AdobeAnimateRuntimeManager.InvalidateRenderRoots();
			InvalidateTreeOrderPathCache();
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
			NotifyPropertyListChanged();
		}
	}

	[Export(PropertyHint.None, "")]
	public Color meshColor
	{
		get
		{
			return _meshColor;
		}
		set
		{
			if (!(_meshColor == value))
			{
				_meshColor = value;
				ApplyComposedModulate();
				_needsRenderSubmission = true;
				if (IsNodeReady())
				{
					SetMultimeshModulate(this);
					RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
				}
			}
		}
	}

	internal bool IsRuntimeActive
	{
		get
		{
			if (!_effectOnceGpuSuppressed && _canRun)
			{
				return _processEnabled;
			}
			return false;
		}
	}

	internal bool IsRuntimeTickPaused
	{
		get
		{
			if (!IsPlaybackStopped)
			{
				return _effectOnceGpuSuppressed;
			}
			return true;
		}
	}

	internal bool IsRuntimeInsideTree => _runtimeInsideTree;

	internal bool IsRuntimeVisibleInTree => _isVisibleInTree;

	public bool IsRuntimeReadyCached => _runtimeReadyCached;

	internal bool IsRuntimeDisplayTickActive
	{
		get
		{
			if (_usingRuntimeManager && !_effectOnceGpuSuppressed && _canRun && _processEnabled)
			{
				return !IsPlaybackStopped;
			}
			return false;
		}
	}

	internal bool NeedsRuntimeRenderSubmission
	{
		get
		{
			if (!_effectOnceGpuSuppressed)
			{
				return _needsRenderSubmission;
			}
			return false;
		}
	}

	internal bool UsesRuntimeGpuClockInterpolation => _runtimeGpuClockInterpolationActive;

	internal bool PrefersCachedGpuGraphCrowdRenderState
	{
		get
		{
			if (_runtimeGpuGraphActive && _hasCachedGpuGraphCrowdState)
			{
				return _cachedGpuGraphCrowdState.Mode == AdobeAnimateCrowdRenderMode.GpuGraph;
			}
			return false;
		}
	}

	internal bool RuntimeManagerDispatchActive => _runtimeManagerDispatchActive;

	protected bool ShouldDispatchProcessCallback
	{
		get
		{
			if (_usingRuntimeManager)
			{
				return _dispatchingBatchedProcess;
			}
			return true;
		}
	}

	protected virtual bool RequiresDisplayFrameBatchedProcessExtension => false;

	internal static int StaticAtlasPathLayoutCacheEntryCountForTests => StaticAtlasPathLayoutCache.Count;

	internal bool NeedsRenderSubmissionForBareTest => _needsRenderSubmission;

	internal ulong PlaybackRevisionForBareTest => _playbackRevision;

	public event AnimeCompletedEventHandler OnAnimeCompleted;

	public event AnimeStopedEventHandler OnAnimeStoped;

	public event AnimeStartedEventHandler OnAnimeStarted;

	public event AnimeEventEventHandler OnAnimeEvent;

	public event AnimeBlendCompletedEventHandler OnAnimeBlendCompleted;

	internal event Action EffectOnceBatchEligibilityChanged;

	internal event Action EffectOnceBatchRenderStateChanged;

	private AdobeAnimateCrowdRenderStateResult TryBuildCompositeCrowdRenderState(AdobeAnimateRuntimeDefinition definition, Node renderMountParent, Transform2D globalTransform, out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		if (_effectOnceGpuSuppressed)
		{
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		Transform2D transform2D = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, globalTransform);
		ResolveRenderPresentationForRender(renderMountParent, out var modulate, out var effectiveZIndex);
		bool needsDrawItemSort = NeedsDrawItemSortForRender();
		AdobeAnimateRenderSnapshot snapshot = new AdobeAnimateRenderSnapshot(this, _flashAnimeData, definition, _clip, clipRange, GetRenderFrameFloat(), transform2D, offset, modulate, _layerVisible, _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _cachedLayerVisibleValueCount, _cachedLayerVisibleValues, mediaReplaceAtlas, mediaReplaceRect, _mediaReplaceUse, _cachedHasMediaReplace, _cachedMediaReplaceLimit, _cachedMediaReplaceUseMask, _cachedMediaReplaceUseMaskOverflow, mediaReplaceAtlasPages, _cachedMediaReplaceAtlasPageCount, _cachedMediaReplaceUseValues, _cachedMediaReplaceRects, _cachedMediaReplaceAtlasPageValues, mediaReplaceAtlasArray, mediaReplaceAtlasArraySize, mediaReplaceAtlasUsesTextureArray, _verticalClip, renderMountParent, _cachedCanvasLayer, effectiveZIndex, System.Array.Empty<int>(), needsDrawItemSort, GetClipBlendStateForRender());
		_needsRenderSubmission = false;
		if (!AdobeAnimateRenderManager.TryResolveSnapshotAtlas(snapshot, out var atlasArray, out var atlasSize))
		{
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		TextureLayered textureLayered = (CanUseCrowdShaderPoseDefinition(definition) ? definition.GpuPoseTextureArray : null);
		Vector2I poseTextureSize = (GodotObject.IsInstanceValid(textureLayered) ? definition.GpuPoseTextureSize : Vector2I.Zero);
		state = AdobeAnimateCrowdRenderState.CreateComposite(snapshot, atlasArray, atlasSize, textureLayered, poseTextureSize, BuildRootMotionStateForRender(renderMountParent, transform2D));
		return AdobeAnimateCrowdRenderStateResult.Submitted;
	}

	private bool CanUseCrowdShaderPoseDefinition(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition != null && definition.Frames != null && definition.Frames.Length != 0 && definition.SliceMetadata != null && definition.UsesGpuPoseTextureArray && definition.GpuPoseTextureRid.IsValid && definition.GpuPoseTextureSize.X > 0 && definition.GpuPoseTextureSize.Y > 0)
		{
			return GodotObject.IsInstanceValid(definition.GpuPoseTextureArray);
		}
		return false;
	}

	private int ResolveCrowdFrameIndex(AdobeAnimateRuntimeDefinition definition, float frameFloat)
	{
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0)
		{
			return 0;
		}
		if (clipRange != Vector2I.Zero)
		{
			frameFloat = Mathf.Clamp(frameFloat, clipRange.X, Math.Max(clipRange.X, clipRange.Y - 1));
		}
		return Mathf.Clamp(Mathf.FloorToInt(frameFloat), 0, definition.Frames.Length - 1);
	}

	private static float ResolveCrowdFrameInterpolation(float frameFloat)
	{
		return Mathf.Clamp(frameFloat - Mathf.Floor(frameFloat), 0f, 1f);
	}

	internal bool TryCollectCompositeCacheOwnersForRender(List<AdobeAnimateSprite> output)
	{
		if (output == null)
		{
			return false;
		}
		return TryCollectCompositeCacheOwnersForRender(output, 0);
	}

	private bool TryCollectCompositeCacheOwnersForRender(List<AdobeAnimateSprite> output, int depth)
	{
		if (depth > 32 || !GodotObject.IsInstanceValid(this))
		{
			return false;
		}
		if (_effectOnceGpuSuppressed)
		{
			return false;
		}
		if (_flashAnimeData == null || !_isVisibleInTree)
		{
			return true;
		}
		if (output.Contains(this))
		{
			return false;
		}
		if (HasManagedSlotSpritesForRender())
		{
			return false;
		}
		output.Add(this);
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				return false;
			}
			if (!adobeAnimateSprite.TryCollectCompositeCacheOwnersForRender(output, depth + 1))
			{
				return false;
			}
		}
		return true;
	}

	internal bool TryBuildCompositePoseRefreshState(Node renderMountParent, out AdobeAnimateCompositePoseRefreshState state)
	{
		state = default(AdobeAnimateCompositePoseRefreshState);
		if (_effectOnceGpuSuppressed || _flashAnimeData == null || !_isVisibleInTree)
		{
			return false;
		}
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		RefreshRenderStaticStateCache();
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (!CanUseCrowdShaderPoseDefinition(runtimeDefinition))
		{
			return false;
		}
		float renderFrameFloat = GetRenderFrameFloat();
		int num = ResolveCrowdFrameIndex(runtimeDefinition, renderFrameFloat);
		PackedFrame packedFrame = runtimeDefinition.Frames[num];
		ulong frameLayoutSignature = ((runtimeDefinition.FrameLayoutSignatures != null && num < runtimeDefinition.FrameLayoutSignatures.Length) ? runtimeDefinition.FrameLayoutSignatures[num] : (((ulong)(uint)packedFrame.Offset << 32) | (uint)packedFrame.Count));
		Transform2D globalTransform = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, GetCachedGlobalTransformForRender());
		ResolveRenderPresentationForRender(renderMountParent, out var modulate, out var _);
		state = new AdobeAnimateCompositePoseRefreshState(runtimeDefinition, globalTransform, modulate, offset, _verticalClip, packedFrame.Offset, packedFrame.Count, runtimeDefinition.GpuPoseTextureBaseTexel, runtimeDefinition.GpuPoseTextureLayer, ResolveCrowdFrameInterpolation(renderFrameFloat), frameLayoutSignature, BuildCompositeCacheStaticSignature());
		return true;
	}

	private ulong BuildCompositeCacheStaticSignature()
	{
		ulong value = 1469598103934665603uL;
		Add(ref value, GetCachedInstanceIdForRender());
		Add(ref value, GodotObject.IsInstanceValid(_flashAnimeData) ? _flashAnimeData.GetInstanceId() : 0);
		Add(ref value, (uint)_layerStateVersion);
		Add(ref value, (uint)_mediaReplaceStateVersion);
		Add(ref value, _cachedLayerVisibleSignature);
		Add(ref value, _cachedMediaReplaceSignature);
		Add(ref value, (uint)StringComparer.Ordinal.GetHashCode(_clip ?? ""));
		Add(ref value, (uint)insertLayerId);
		Add(ref value, (uint)followParentSpriteLayerId);
		Add(ref value, (ulong)_spriteChildren.Length);
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spriteChildren[i];
			AdobeAnimateSlot adobeAnimateSlot = ((i < _spriteChildOwnerSlots.Length) ? _spriteChildOwnerSlots[i] : null);
			Add(ref value, GodotObject.IsInstanceValid(adobeAnimateSprite) ? adobeAnimateSprite.GetInstanceId() : 0);
			Add(ref value, GodotObject.IsInstanceValid(adobeAnimateSlot) ? adobeAnimateSlot.GetInstanceId() : 0);
			Add(ref value, (ulong)((GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite._isVisibleInTree) ? 1 : 0));
			Add(ref value, GodotObject.IsInstanceValid(adobeAnimateSprite) ? ((uint)adobeAnimateSprite.insertLayerId) : 0);
			Add(ref value, GodotObject.IsInstanceValid(adobeAnimateSprite) ? ((uint)adobeAnimateSprite.followParentSpriteLayerId) : 0);
		}
		return value;
		static void Add(ref ulong reference, ulong component)
		{
			reference ^= component;
			reference *= 1099511628211uL;
		}
	}

	internal bool CanSkipCrowdRenderFromCachedCulling()
	{
		if (_effectOnceGpuSuppressed || !_usingRuntimeManager || _flashAnimeData == null || !_isVisibleInTree || !_runtimeRenderBoundsCached || _cachedRuntimeRenderBoundsVisible || _runtimeRenderBoundsRescanCountdown <= 0 || _renderGlobalTransformDirty || !_renderGlobalTransformCached || _cachedRuntimeRenderBoundsTransform != _renderGlobalTransform || _cachedRuntimeRenderBoundsOffset != offset || _cachedRuntimeRenderBoundsDefinition != _runtimeDefinition || _cachedRuntimeRenderBoundsMountParent != _cachedRenderMountParent || HasRuntimeCrowdViewportChanged())
		{
			return false;
		}
		_runtimeRenderBoundsRescanCountdown--;
		return true;
	}

	internal bool TryGetCachedOffscreenCrowdWarmupPlacement(out Node renderMountParent, out int effectiveZIndex)
	{
		renderMountParent = null;
		effectiveZIndex = 0;
		if (_effectOnceGpuSuppressed || !_usingRuntimeManager || !_isVisibleInTree || !IsInsideTree() || !_runtimeRenderBoundsCached || _cachedRuntimeRenderBoundsVisible || !GodotObject.IsInstanceValid(_cachedRenderMountParent))
		{
			return false;
		}
		renderMountParent = _cachedRenderMountParent;
		effectiveZIndex = GetEffectiveZIndexForRender();
		return true;
	}

	internal AdobeAnimateCrowdRenderStateResult TryBuildCrowdRenderState(out AdobeAnimateCrowdRenderState state)
	{
		return TryBuildCrowdRenderState((float)AdobeAnimateRuntimeManager.AnimationClockSeconds, out state);
	}

	internal AdobeAnimateCrowdRenderStateResult TryBuildCrowdRenderState(float animationClockSeconds, out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		bool prefersCachedGpuGraphCrowdRenderState = PrefersCachedGpuGraphCrowdRenderState;
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		bool num = prefersCachedGpuGraphCrowdRenderState && TryReuseGpuGraphCrowdRenderStateForCurrentPhysicsFrame(out state);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateSamePhysicsFrame, startBytes);
		if (num)
		{
			return AdobeAnimateCrowdRenderStateResult.Submitted;
		}
		long startBytes2 = TowerDefenseAllocationTelemetry.Begin();
		AdobeAnimateCrowdRenderStateResult result = TryBuildCrowdRenderStateCore(out state, cpuDrivenPose: false, animationClockSeconds, prefersCachedGpuGraphCrowdRenderState);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateBuild, startBytes2);
		return result;
	}

	internal bool TryReuseGpuGraphCrowdRenderStateForCurrentPhysicsFrame(out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		ulong physicsFrameForRender = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
		if (!_hasCachedGpuGraphCrowdState || !_runtimeGpuGraphActive || _cachedGpuGraphCrowdStatePhysicsFrame != physicsFrameForRender || _cachedGpuGraphCrowdState.Mode != AdobeAnimateCrowdRenderMode.GpuGraph || _needsRenderSubmission || _gpuGraphCrowdStaticStateDirty || _renderGlobalTransformDirty || _renderRootMotionEnabled || needMediaReplaceUpdate || !_runtimeGpuClockInterpolationActive || IsClipBlendActiveForRender() || !AdobeAnimateRenderManager.IsGpuGraphStateCurrent(this, _cachedGpuGraphCrowdState.GpuGraphAllocation.Signature) || !AdobeAnimateRenderManager.TryRefreshGpuGraphStateAtlasBinding(_cachedGpuGraphCrowdState))
		{
			return false;
		}
		state = _cachedGpuGraphCrowdState;
		return true;
	}

	internal bool TryBuildRasterCompositeRenderState(out AdobeAnimateRasterCompositeRenderState state)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		state = default(AdobeAnimateRasterCompositeRenderState);
		_runtimeGpuGraphActive = false;
		if (_effectOnceGpuSuppressed || !_usingRuntimeManager || CachedEditorHint || _flashAnimeData == null || !_isVisibleInTree || !AdobeAnimateRenderManager.RasterCompositeEnabled)
		{
			return false;
		}
		if (TryReuseRasterCompositeRenderState(out state))
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.rasterCompactCached", startTicks, 1);
			return true;
		}
		AdobeAnimateRasterCompositeData rasterCompositeData = _flashAnimeData.rasterCompositeData;
		if (!GodotObject.IsInstanceValid(rasterCompositeData))
		{
			return false;
		}
		_hasCachedGpuGraphCrowdState = false;
		_cachedGpuGraphCrowdState = null;
		_gpuGraphCrowdStaticStateDirty = false;
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		RefreshRenderStaticStateCache();
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (!HasRenderableAtlas(runtimeDefinition))
		{
			return false;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		if (!IsRuntimeCrowdRenderBoundsVisible(runtimeDefinition, cachedGlobalTransformForRender, cachedRenderMountParent) || !TryBuildRasterCompositeRenderStateCore(runtimeDefinition, rasterCompositeData, cachedRenderMountParent, cachedGlobalTransformForRender, out state))
		{
			return false;
		}
		_cachedRasterCompositeRenderState = state;
		_hasCachedRasterCompositeRenderState = true;
		TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.rasterCompact", startTicks, 1);
		return true;
	}

	private bool TryReuseRasterCompositeRenderState(out AdobeAnimateRasterCompositeRenderState state)
	{
		state = default(AdobeAnimateRasterCompositeRenderState);
		if (!_hasCachedRasterCompositeRenderState || needMediaReplaceUpdate || IsClipBlendActiveForRender() || RefreshRenderStaticStateCache())
		{
			return false;
		}
		AdobeAnimateRasterCompositeData rasterCompositeData = _cachedRasterCompositeRenderState.RasterCompositeData;
		if (rasterCompositeData != _flashAnimeData?.rasterCompositeData || !TryResolveRasterCompositeTile(rasterCompositeData, GetRenderFrameFloat(), out var tileIndex))
		{
			return false;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		if (cachedRenderMountParent != _cachedRasterCompositeRenderState.RenderMountParent)
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition definition = _cachedRasterCompositeRenderState.Definition;
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		if (!IsRuntimeCrowdRenderBoundsVisible(definition, cachedGlobalTransformForRender, cachedRenderMountParent))
		{
			return false;
		}
		Color modulate = GetEffectiveRenderModulate(cachedRenderMountParent);
		int effectiveZIndexForRender = GetEffectiveZIndexForRender();
		Transform2D globalTransform = AdobeAnimateRenderManager.ToRenderMountLocalTransform(cachedRenderMountParent, cachedGlobalTransformForRender);
		state = _cachedRasterCompositeRenderState.WithDynamicState(in globalTransform, in modulate, effectiveZIndexForRender, tileIndex);
		_cachedRasterCompositeRenderState = state;
		_needsRenderSubmission = false;
		return true;
	}

	internal AdobeAnimateCrowdRenderStateResult TryBuildCpuPoseCrowdRenderState(out AdobeAnimateCrowdRenderState state)
	{
		return TryBuildCpuPoseCrowdRenderState((float)AdobeAnimateRuntimeManager.AnimationClockSeconds, out state);
	}

	internal AdobeAnimateCrowdRenderStateResult TryBuildCpuPoseCrowdRenderState(float animationClockSeconds, out AdobeAnimateCrowdRenderState state)
	{
		return TryBuildCrowdRenderStateCore(out state, cpuDrivenPose: true, animationClockSeconds, preferCachedGpuGraph: false);
	}

	private AdobeAnimateCrowdRenderStateResult TryBuildCrowdRenderStateCore(out AdobeAnimateCrowdRenderState state, bool cpuDrivenPose, float animationClockSeconds, bool preferCachedGpuGraph)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		state = null;
		_runtimeGpuGraphActive = false;
		if (_effectOnceGpuSuppressed || !_usingRuntimeManager || CachedEditorHint)
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.unsupported", startTicks);
			return AdobeAnimateCrowdRenderStateResult.NotSupported;
		}
		if (_flashAnimeData == null || !_isVisibleInTree)
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.skip", startTicks);
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		bool flag = !preferCachedGpuGraph && AdobeAnimateRenderManager.RasterCompositeEnabled && GodotObject.IsInstanceValid(_flashAnimeData.rasterCompositeData);
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		bool num = !cpuDrivenPose && !flag && TryReuseGpuGraphCrowdRenderState(animationClockSeconds, out state);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuReuse, startBytes);
		if (num)
		{
			_runtimeGpuGraphActive = true;
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.gpuGraphCached", startTicks, 1);
			return AdobeAnimateCrowdRenderStateResult.Submitted;
		}
		AdobeAnimateCrowdRenderState cachedGpuGraphCrowdState = _cachedGpuGraphCrowdState;
		AdobeAnimateCrowdRenderState reusableState = ((cachedGpuGraphCrowdState != null && cachedGpuGraphCrowdState.Mode == AdobeAnimateCrowdRenderMode.GpuGraph) ? _cachedGpuGraphCrowdState : null);
		_hasCachedGpuGraphCrowdState = false;
		_cachedGpuGraphCrowdState = null;
		_gpuGraphCrowdStaticStateDirty = false;
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		if (RefreshRenderStaticStateCache())
		{
			_runtimeGpuPrepareRequired = true;
		}
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (!HasRenderableAtlas(runtimeDefinition))
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.noDefinition", startTicks);
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		if (!IsRuntimeCrowdRenderBoundsVisible(runtimeDefinition, cachedGlobalTransformForRender, cachedRenderMountParent))
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.culled", startTicks);
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		if (flag)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.candidate", 1);
			if (TryBuildRasterCompositeCrowdRenderState(runtimeDefinition, cachedRenderMountParent, cachedGlobalTransformForRender, out state))
			{
				TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.rasterComposite", startTicks, 1);
				return AdobeAnimateCrowdRenderStateResult.Submitted;
			}
		}
		if (AdobeAnimateRenderManager.GpuRenderGraphEnabled && CanUseCrowdShaderPoseDefinition(runtimeDefinition) && (_cachedAllLayersVisible || _cachedCanUseLayerMask || _cachedLayerVisibleCount > 0) && CanUseGpuGraphMediaReplace(runtimeDefinition))
		{
			long startBytes2 = TowerDefenseAllocationTelemetry.Begin();
			bool num2 = TryBuildGpuGraphCrowdRenderState(runtimeDefinition, cachedRenderMountParent, cachedGlobalTransformForRender, enableGpuClock: true, animationClockSeconds, reusableState, out state);
			TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuild, startBytes2);
			if (num2)
			{
				_runtimeGpuGraphActive = true;
				_cachedGpuGraphCrowdState = state;
				_hasCachedGpuGraphCrowdState = true;
				_cachedGpuGraphCrowdStatePhysicsFrame = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
				_cachedGpuGraphRootOwnerPlaybackRevision = _playbackRevision;
				_gpuGraphCrowdPresentationStateDirty = false;
				ScheduleNextGpuGraphCrowdStateAudit();
				TowerDefensePerfProfiler.End(cpuDrivenPose ? "adobeAnimate.render.crowdState.cpuDrivenGraph" : "adobeAnimate.render.crowdState.gpuGraph", startTicks, 1);
				return AdobeAnimateCrowdRenderStateResult.Submitted;
			}
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.gpuGraphFallback", 1);
		}
		bool flag2 = IsClipBlendActiveForRender();
		DisableRuntimeGpuClockInterpolationForOwnedGraph();
		if (!CanUseCrowdShaderPoseDefinition(runtimeDefinition) || flag2 || RequiresInternalDrawItemSortForRender() || !_cachedAllLayersVisible || _cachedHasMediaReplace || _cachedAnyRequestedMediaReplace || runtimeDefinition.MaxFrameSliceCount <= 0 || (mediaReplaceAtlasUsesTextureArray && !GodotObject.IsInstanceValid(mediaReplaceAtlasArray)))
		{
			AdobeAnimateCrowdRenderStateResult adobeAnimateCrowdRenderStateResult = TryBuildCompositeCrowdRenderState(runtimeDefinition, cachedRenderMountParent, cachedGlobalTransformForRender, out state);
			TowerDefensePerfProfiler.End((adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted) ? "adobeAnimate.render.crowdState.composite" : "adobeAnimate.render.crowdState.compositeSkip", startTicks, (adobeAnimateCrowdRenderStateResult == AdobeAnimateCrowdRenderStateResult.Submitted) ? 1 : 0);
			return adobeAnimateCrowdRenderStateResult;
		}
		Transform2D transform2D = AdobeAnimateRenderManager.ToRenderMountLocalTransform(cachedRenderMountParent, cachedGlobalTransformForRender);
		ResolveRenderPresentationForRender(cachedRenderMountParent, out var modulate, out var effectiveZIndex);
		float renderFrameFloat = GetRenderFrameFloat();
		int num3 = ResolveCrowdFrameIndex(runtimeDefinition, renderFrameFloat);
		PackedFrame packedFrame = runtimeDefinition.Frames[num3];
		if (packedFrame.Count <= 0)
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.skip", startTicks);
			return AdobeAnimateCrowdRenderStateResult.Skipped;
		}
		state = new AdobeAnimateCrowdRenderState(runtimeDefinition, _cachedHasMediaReplace ? mediaReplaceAtlasArray : runtimeDefinition.AtlasTextureArray, (_cachedHasMediaReplace && mediaReplaceAtlasArraySize.X > 0f && mediaReplaceAtlasArraySize.Y > 0f) ? mediaReplaceAtlasArraySize : ((runtimeDefinition.AtlasTextureArraySize.X > 0f && runtimeDefinition.AtlasTextureArraySize.Y > 0f) ? runtimeDefinition.AtlasTextureArraySize : Vector2.One), runtimeDefinition.GpuPoseTextureArray, runtimeDefinition.GpuPoseTextureSize, cachedRenderMountParent, transform2D, modulate, offset, _verticalClip, packedFrame.Offset, packedFrame.Count, runtimeDefinition.GpuPoseTextureBaseTexel, runtimeDefinition.GpuPoseTextureLayer, ResolveCrowdFrameInterpolation(renderFrameFloat), _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _layerVisible, effectiveZIndex, _cachedHasMediaReplace, mediaReplaceRect, _mediaReplaceUse, mediaReplaceAtlasPages, mediaReplaceAtlasArraySize, BuildRootMotionStateForRender(cachedRenderMountParent, transform2D));
		TowerDefensePerfProfiler.End("adobeAnimate.render.crowdState.build", startTicks, 1);
		return AdobeAnimateCrowdRenderStateResult.Submitted;
	}

	private bool TryBuildRasterCompositeCrowdRenderState(AdobeAnimateRuntimeDefinition definition, Node renderMountParent, Transform2D globalTransform, out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		AdobeAnimateRasterCompositeData raster = _flashAnimeData?.rasterCompositeData;
		if (!TryBuildRasterCompositeRenderStateCore(definition, raster, renderMountParent, globalTransform, out var state2))
		{
			return false;
		}
		state = AdobeAnimateCrowdRenderState.CreateRasterComposite(state2.Definition, state2.RenderMountParent, state2.GlobalTransform, state2.Modulate, state2.EffectiveZIndex, state2.VerticalClip, state2.RasterCompositeData, state2.RasterCompositeTileIndex);
		return true;
	}

	private bool TryBuildRasterCompositeRenderStateCore(AdobeAnimateRuntimeDefinition definition, AdobeAnimateRasterCompositeData raster, Node renderMountParent, Transform2D globalTransform, out AdobeAnimateRasterCompositeRenderState state)
	{
		state = default(AdobeAnimateRasterCompositeRenderState);
		if (!GodotObject.IsInstanceValid(raster))
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.data", 1);
			return false;
		}
		if (IsClipBlendActiveForRender())
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.blend", 1);
			return false;
		}
		if (!raster.AllowsMediaReplaceMask(_cachedMediaReplaceUseMask, _cachedMediaReplaceUseMaskOverflow))
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.media", 1);
			return false;
		}
		if (GetSpriteChildrenForRender().Length != 0)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.children", 1);
			return false;
		}
		if (HasManagedSlotSpritesForRender())
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.slots", 1);
			return false;
		}
		if (HasExternalVisualsForRender())
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.external", 1);
			return false;
		}
		if (RequiresInternalDrawItemSortForRender())
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.sort", 1);
			return false;
		}
		if (!TryResolveRasterCompositeTile(raster, GetRenderFrameFloat(), out var tileIndex))
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.raster.reject.pose", 1);
			return false;
		}
		Transform2D globalTransform2 = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, globalTransform);
		Color effectiveRenderModulate = GetEffectiveRenderModulate(renderMountParent);
		int effectiveZIndexForRender = GetEffectiveZIndexForRender();
		state = new AdobeAnimateRasterCompositeRenderState(definition, renderMountParent, globalTransform2, effectiveRenderModulate, _verticalClip, effectiveZIndexForRender, raster, tileIndex);
		_needsRenderSubmission = false;
		return true;
	}

	private bool TryResolveRasterCompositeTile(AdobeAnimateRasterCompositeData raster, float frameFloat, out int tileIndex)
	{
		if (_cachedRasterCompositeSequenceData != raster || _cachedRasterCompositeSequenceClip != _clip || _cachedRasterCompositeSequenceRange != clipRange || _cachedRasterCompositeSequenceLayerSignature != _cachedLayerVisibleSignature)
		{
			_cachedRasterCompositeSequenceData = raster;
			_cachedRasterCompositeSequenceClip = _clip;
			_cachedRasterCompositeSequenceRange = clipRange;
			_cachedRasterCompositeSequenceLayerSignature = _cachedLayerVisibleSignature;
			_cachedRasterCompositeSequenceValid = raster.TryResolveSequence(_clip, clipRange, _cachedLayerVisibleSignature, out _cachedRasterCompositeSequenceTileBase, out _cachedRasterCompositeSequenceClipStart, out _cachedRasterCompositeSequenceFrameCount);
		}
		if (!_cachedRasterCompositeSequenceValid)
		{
			tileIndex = 0;
			return false;
		}
		int num = Math.Clamp(Mathf.FloorToInt(frameFloat) - _cachedRasterCompositeSequenceClipStart, 0, _cachedRasterCompositeSequenceFrameCount - 1);
		tileIndex = _cachedRasterCompositeSequenceTileBase + num;
		return tileIndex >= 0;
	}

	private bool TryReuseGpuGraphCrowdRenderState(float animationClockSeconds, out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		if (!_hasCachedGpuGraphCrowdState)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectCache);
			return false;
		}
		if (_cachedGpuGraphCrowdState.Mode != AdobeAnimateCrowdRenderMode.GpuGraph)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectCache);
			return false;
		}
		if (RefreshRenderStaticStateCache())
		{
			_gpuGraphCrowdStaticStateDirty = true;
			_runtimeGpuPrepareRequired = true;
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectStatic);
			return false;
		}
		if (_needsRenderSubmission && _gpuGraphCrowdStaticStateDirty)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectStatic);
			return false;
		}
		if (!AdobeAnimateRenderManager.IsGpuGraphStateCurrent(this, _cachedGpuGraphCrowdState.GpuGraphAllocation.Signature))
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectGraph);
			return false;
		}
		if (!AdobeAnimateRenderManager.TryRefreshGpuGraphStateAtlasBinding(_cachedGpuGraphCrowdState))
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectGraph);
			return false;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		if (cachedRenderMountParent == null || cachedRenderMountParent != _cachedGpuGraphCrowdState.RenderMountParent || (_forceLocalRender && !GodotObject.IsInstanceValid(cachedRenderMountParent)))
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectMount);
			return false;
		}
		Color modulate = _cachedGpuGraphCrowdState.Modulate;
		int num = _cachedGpuGraphCrowdState.EffectiveZIndex;
		ulong physicsFrameForRender = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
		bool gpuGraphCrowdPresentationStateDirty = _gpuGraphCrowdPresentationStateDirty;
		bool flag = physicsFrameForRender >= _gpuGraphCrowdStateNextAuditPhysicsFrame;
		if (gpuGraphCrowdPresentationStateDirty || flag)
		{
			modulate = GetEffectiveRenderModulate(cachedRenderMountParent);
			num = GetEffectiveZIndexForRender();
			if (flag)
			{
				ScheduleNextGpuGraphCrowdStateAudit();
			}
			bool flag2 = modulate != _cachedGpuGraphCrowdState.Modulate || num != _cachedGpuGraphCrowdState.EffectiveZIndex;
			if (!gpuGraphCrowdPresentationStateDirty && flag2)
			{
				TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectPresentation);
				return false;
			}
		}
		AdobeAnimateRuntimeDefinition definition = _cachedGpuGraphCrowdState.Definition;
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		if (!IsRuntimeCrowdRenderBoundsVisible(definition, cachedGlobalTransformForRender, cachedRenderMountParent))
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderCollectStateGpuRejectBounds);
			return false;
		}
		Transform2D globalTransform = AdobeAnimateRenderManager.ToRenderMountLocalTransform(cachedRenderMountParent, cachedGlobalTransformForRender);
		AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState;
		if (_cachedGpuGraphRootOwnerPlaybackRevision != _playbackRevision || !SameGpuHitFlashState(_cachedGpuGraphCrowdState.GpuGraphRootOwnerState.GpuBrightFlash, _gpuBrightFlash) || !SameGpuHitFlashState(_cachedGpuGraphCrowdState.GpuGraphRootOwnerState.GpuWhiteFlash, _gpuWhiteFlash) || IsClipBlendActiveForRender())
		{
			gpuGraphRootOwnerState = (_cachedGpuGraphRootOwnerState = BuildGpuGraphOwnerStateForRender(definition, globalTransform, modulate, GetRenderFrameFloat(), enableGpuClock: true, animationClockSeconds, _cachedGpuGraphRootOwnerState));
			_cachedGpuGraphRootOwnerPlaybackRevision = _playbackRevision;
		}
		else
		{
			gpuGraphRootOwnerState = _cachedGpuGraphCrowdState.GpuGraphRootOwnerState.WithPresentation(in globalTransform, in modulate);
		}
		AdobeAnimateRootMotionState rootMotion = BuildRootMotionStateForRender(cachedRenderMountParent, globalTransform);
		state = _cachedGpuGraphCrowdState.WithGpuGraphDynamicState(in globalTransform, in modulate, num, gpuGraphRootOwnerState, in rootMotion);
		_cachedGpuGraphCrowdState = state;
		_cachedGpuGraphCrowdStatePhysicsFrame = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
		_gpuGraphCrowdStaticStateDirty = false;
		_gpuGraphCrowdPresentationStateDirty = false;
		return true;
	}

	private void ScheduleNextGpuGraphCrowdStateAudit()
	{
		ulong physicsFrameForRender = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
		_gpuGraphCrowdStateNextAuditPhysicsFrame = physicsFrameForRender + (ulong)GetStaggeredRescanInterval(240);
	}

	public void EmitAnimeCompleted(string clip)
	{
		this.OnAnimeCompleted?.Invoke(clip);
	}

	private void MarkEffectOnceBatchEligibilityChanged()
	{
		_effectOnceBatchEligibilityRevision++;
		this.EffectOnceBatchEligibilityChanged?.Invoke();
	}

	internal ulong GetEffectOnceBatchPlaybackRevision()
	{
		return _playbackRevision;
	}

	internal bool IsEffectOnceGpuBatchPlaybackPrepared(string resolvedClip, int expectedClipStart = -2147483648, int expectedClipEndExclusive = -2147483648, int expectedPlaybackFrameCount = -1, float expectedFrameRate = -1f)
	{
		int num = GetPlaybackClipEndExclusive() - clipRange.X;
		if (!string.IsNullOrEmpty(resolvedClip) && string.Equals(_clip, resolvedClip, StringComparison.Ordinal) && _canRun && !loop && !playBack && !blend && !clipOver && track.Count == 0 && _nextAnimDelayTimer <= 0.0 && _pendingTrackConfig == null && !refreshEveryFrame && !refreshEveryFlame && Math.Abs(timeScale - 1.0) <= 0.0001 && num > 1 && (expectedClipStart == -2147483648 || clipRange.X == expectedClipStart) && (expectedClipEndExclusive == -2147483648 || GetPlaybackClipEndExclusive() == expectedClipEndExclusive) && (expectedPlaybackFrameCount < 0 || num == expectedPlaybackFrameCount) && frameRate > 0.0)
		{
			if (!(expectedFrameRate < 0f))
			{
				return Math.Abs(frameRate - (double)expectedFrameRate) <= 0.0001;
			}
			return true;
		}
		return false;
	}

	internal float GetEffectOnceGpuBatchElapsedFrame()
	{
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		int num = Math.Max(1, playbackClipEndExclusive - clipRange.X);
		return (float)Math.Clamp((double)(frameIndex - clipRange.X) + elapsedTimer, 0.0, Math.Max(0.0, (double)num - 0.0001));
	}

	internal EffectOnceGpuAdvanceResult AdvanceEffectOnceGpuPlayback(double delta, int suppressionToken, string expectedClip, ulong expectedPlaybackRevision, double expectedElapsedFrame, int expectedClipStart, int expectedClipEndExclusive, int expectedPlaybackFrameCount, float expectedFrameRate)
	{
		if (!_effectOnceGpuSuppressed || suppressionToken == 0 || suppressionToken != _effectOnceGpuSuppressionToken || _playbackRevision != expectedPlaybackRevision || clipRange.X != expectedClipStart || GetPlaybackClipEndExclusive() != expectedClipEndExclusive || Math.Abs((double)GetEffectOnceGpuBatchElapsedFrame() - expectedElapsedFrame) > 0.001 || !IsEffectOnceGpuBatchPlaybackPrepared(expectedClip, expectedClipStart, expectedClipEndExclusive, expectedPlaybackFrameCount, expectedFrameRate))
		{
			return EffectOnceGpuAdvanceResult.Invalidated;
		}
		int x = clipRange.X;
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		bool flag = false;
		refreshTimer += delta * Math.Max(10.0, trueFrameRate);
		if (refreshTimer >= 1.0)
		{
			flag = true;
			refreshTimer = 0.0;
		}
		elapsedTimer += delta * Math.Abs(timeScale) * frameRate;
		while (elapsedTimer >= 1.0)
		{
			elapsedTimer -= 1.0;
			EmitFrameEvents(frameIndex);
			if (!IsEffectOnceGpuCallbackStateCurrent(suppressionToken, expectedClip, expectedPlaybackRevision))
			{
				return EffectOnceGpuAdvanceResult.Invalidated;
			}
			frameIndex++;
			if (frameIndex >= playbackClipEndExclusive)
			{
				EmitFrameEvents(frameIndex);
				if (!IsEffectOnceGpuCallbackStateCurrent(suppressionToken, expectedClip, expectedPlaybackRevision))
				{
					return EffectOnceGpuAdvanceResult.Invalidated;
				}
				string text = _clip;
				clipOver = true;
				frameIndex = Math.Max(x, playbackClipEndExclusive - 1);
				elapsedTimer = 0.0;
				_lastUpdateFrame = frameIndex;
				_managedPoseRevision++;
				NextAnimation();
				if (!GodotObject.IsInstanceValid(this))
				{
					return EffectOnceGpuAdvanceResult.Completed;
				}
				if (!CachedEditorHint)
				{
					this.OnAnimeCompleted?.Invoke(text);
				}
				return EffectOnceGpuAdvanceResult.Completed;
			}
		}
		if (flag)
		{
			_lastUpdateFrame = frameIndex;
		}
		_managedPoseRevision++;
		return EffectOnceGpuAdvanceResult.Running;
	}

	private bool IsEffectOnceGpuCallbackStateCurrent(int suppressionToken, string expectedClip, ulong expectedPlaybackRevision)
	{
		if (GodotObject.IsInstanceValid(this) && _effectOnceGpuSuppressed && suppressionToken == _effectOnceGpuSuppressionToken && _playbackRevision == expectedPlaybackRevision)
		{
			return string.Equals(_clip, expectedClip, StringComparison.Ordinal);
		}
		return false;
	}

	internal bool TryImportEffectOnceGpuPlaybackPositionWithoutEvents(string expectedClip, float targetElapsedFrame)
	{
		if (!IsEffectOnceGpuBatchPlaybackPrepared(expectedClip))
		{
			return false;
		}
		int x = clipRange.X;
		int num = GetPlaybackClipEndExclusive() - x;
		if (num <= 0)
		{
			return false;
		}
		double num2 = Math.Clamp(targetElapsedFrame, 0.0, Math.Max(0.0, (double)num - 0.0001));
		_managedPoseRevision++;
		refreshTimer = 0.0;
		int num3 = Math.Clamp((int)Math.Floor(num2), 0, num - 1);
		frameIndex = x + num3;
		elapsedTimer = num2 - (double)num3;
		_lastUpdateFrame = frameIndex;
		return true;
	}

	internal int GetEffectOnceBatchVisualRevision()
	{
		return _effectOnceBatchVisualRevision;
	}

	internal int GetEffectOnceBatchEligibilityRevision()
	{
		return _effectOnceBatchEligibilityRevision;
	}

	internal void InvalidateEffectOnceBatchRenderState()
	{
		MarkEffectOnceBatchRenderStateDirty(invalidateZ: true);
	}

	private void MarkEffectOnceBatchRenderStateDirty(bool invalidateZ = false)
	{
		_effectOnceBatchRenderStateDirty = true;
		_effectOnceBatchRenderRevision++;
		this.EffectOnceBatchRenderStateChanged?.Invoke();
		if (invalidateZ)
		{
			_effectiveZIndexCached = false;
			_effectiveZIndexRescanCountdown = 0;
		}
	}

	public void SetRenderClipControl(Control control)
	{
		if (_renderClipControl != control)
		{
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
			_renderClipControl = (GodotObject.IsInstanceValid(control) ? control : null);
			InvalidateRenderSnapshotCache(force: true);
			if (IsInsideTree())
			{
				RefreshRenderMountCache();
			}
			RequestNodeRedraw();
		}
	}

	public void ClearRenderClipControl()
	{
		SetRenderClipControl(null);
	}

	private void InvalidateGpuRenderGraphForOffsetChange()
	{
	}

	internal void SetPlaybackBlocked(bool blocked)
	{
		if (_playbackBlocked != blocked)
		{
			bool isPlaybackStopped = IsPlaybackStopped;
			_playbackBlocked = blocked;
			if (CommitPlaybackControlChange(isPlaybackStopped))
			{
				RefreshRuntimeChildPlaybackControl();
			}
		}
	}

	private bool CommitPlaybackControlChange(bool wasPlaybackStopped)
	{
		bool isPlaybackStopped = IsPlaybackStopped;
		MarkEffectOnceBatchEligibilityChanged();
		InvalidateRuntimeDisplayVisualRequirementCache();
		InvalidateRenderSnapshotCache();
		if (isPlaybackStopped)
		{
			ClearRetainedRootMotionForPause();
		}
		if (wasPlaybackStopped != isPlaybackStopped)
		{
			_playbackRevision++;
			InvalidateRuntimePauseDispatchCache();
		}
		if (!isPlaybackStopped && _canRun && !_processEnabled)
		{
			SetProcessEnabled(enabled: true);
		}
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		return wasPlaybackStopped != isPlaybackStopped;
	}

	private void SetParentPlaybackStopped(bool stopped)
	{
		if (_parentPlaybackStopped != stopped)
		{
			bool isPlaybackStopped = IsPlaybackStopped;
			_parentPlaybackStopped = stopped;
			if (CommitPlaybackControlChange(isPlaybackStopped))
			{
				RefreshRuntimeChildPlaybackControl();
			}
		}
	}

	private void RefreshRuntimeChildPlaybackControl()
	{
		if (CachedEditorHint)
		{
			return;
		}
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spriteChildren[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.IsRuntimeInsideTree)
			{
				adobeAnimateSprite.ApplyRuntimeParentState(this);
			}
		}
	}

	internal int GetEffectOnceGpuBatchSuppressionToken()
	{
		if (!_effectOnceGpuSuppressed)
		{
			return 0;
		}
		return _effectOnceGpuSuppressionToken;
	}

	internal int BeginEffectOnceGpuBatchSuppression()
	{
		_effectOnceGpuSuppressionToken++;
		if (_effectOnceGpuSuppressionToken == 0)
		{
			_effectOnceGpuSuppressionToken = 1;
		}
		_effectOnceGpuSuppressed = true;
		_runtimeRenderSubmissionRetryRequested = false;
		_needsRenderSubmission = false;
		InvalidateRuntimeDisplayVisualRequirementCache();
		AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
		AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.OwnerDefinition);
		SetProcessEnabled(enabled: false);
		QueueRedraw();
		return _effectOnceGpuSuppressionToken;
	}

	internal bool EndEffectOnceGpuBatchSuppression(int suppressionToken)
	{
		if (!_effectOnceGpuSuppressed || suppressionToken == 0 || suppressionToken != _effectOnceGpuSuppressionToken)
		{
			return false;
		}
		_effectOnceGpuSuppressed = false;
		InvalidateRuntimeDisplayVisualRequirementCache();
		InvalidateRenderSnapshotCache(force: true);
		_needsRenderSubmission = true;
		if (IsInsideTree())
		{
			AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.OwnerDefinition);
			RefreshProcessScheduling();
			RequestNodeRedraw();
		}
		return true;
	}

	public void SetFrozenPreview(bool frozen)
	{
		if (_frozenPreview != frozen)
		{
			if (frozen)
			{
				_frozenPreview = true;
				keepRenderSubmittedWhenPaused = true;
				pause = true;
				_needsRenderSubmission = true;
				RequestNodeRedraw();
				StopRuntimeTickKeepRender();
			}
			else
			{
				_frozenPreview = false;
				keepRenderSubmittedWhenPaused = false;
				pause = false;
				_needsRenderSubmission = true;
				CanRun();
				SetProcessEnabled(_canRun && !clipOver);
				RequestNodeRedraw();
			}
		}
	}

	public void EnsureFrozenPreviewRenderSubmission()
	{
		if (_frozenPreview && IsInsideTree())
		{
			UpdateVisibilityCache();
			if (_isVisibleInTree)
			{
				RequestNodeRedraw();
			}
		}
	}

	private void CommitPlaybackDirectionChange()
	{
		MarkEffectOnceBatchEligibilityChanged();
		InvalidateRuntimeDisplayVisualRequirementCache();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
	}

	internal Array<bool> GetEffectOnceBatchLayerVisible()
	{
		return _layerVisible;
	}

	internal Array<bool> GetLayerVisibleForInternalRead()
	{
		return _layerVisible;
	}

	internal int GetLayerVisibleCountForRender()
	{
		if (!_renderStaticStateCached)
		{
			return _layerVisible?.Count ?? 0;
		}
		return _cachedLayerVisibleValueCount;
	}

	internal Array<Texture2D> GetMediaReplaceForInternalRead()
	{
		return _mediaReplace;
	}

	internal Array<bool> GetMediaReplaceUseForInternalRead()
	{
		return _mediaReplaceUse;
	}

	internal Array<bool> GetEffectOnceBatchMediaReplaceUse()
	{
		return _mediaReplaceUse;
	}

	internal int BeginEffectOnceBatchEligibilityTracking()
	{
		_effectOnceBatchEligibilityTracking = true;
		return _effectOnceBatchArrayExposureRevision;
	}

	internal void EndEffectOnceBatchEligibilityTracking()
	{
		_effectOnceBatchEligibilityTracking = false;
	}

	internal int GetEffectOnceBatchArrayExposureRevision()
	{
		return _effectOnceBatchArrayExposureRevision;
	}

	private void MarkEffectOnceBatchArrayExposure()
	{
		if (_effectOnceBatchEligibilityTracking)
		{
			_effectOnceBatchArrayExposureRevision++;
			MarkEffectOnceBatchEligibilityChanged();
		}
	}

	private void MarkRenderStaticLayerArrayExposure()
	{
		if (!_renderStaticLayerArrayEscaped)
		{
			_renderStaticLayerArrayEscaped = true;
			ArmRenderStaticArrayAuditAfterExposure();
		}
	}

	private void MarkRenderStaticMediaArrayExposure()
	{
		if (!_renderStaticMediaArraysEscaped)
		{
			_renderStaticMediaArraysEscaped = true;
			QueueUpdateMediaReplace();
			ArmRenderStaticArrayAuditAfterExposure();
		}
	}

	private void ArmRenderStaticArrayAuditAfterExposure()
	{
		AdobeAnimateRuntimeManager.SetRenderStaticArrayAuditRegistration(this, enabled: true);
		if (_renderStaticStateCached)
		{
			_renderStaticStateCached = false;
			_renderStaticArrayAuditSubmissionPending = true;
			InvalidateRuntimeCrowdCullingForRefresh();
			RequestNodeRedraw();
		}
	}

	public void SetRenderColorMultiplier(Color multiplier)
	{
		if (!(_renderColorMultiplier == multiplier))
		{
			_renderColorMultiplier = multiplier;
			ApplyComposedModulate();
			_needsRenderSubmission = true;
			if (IsNodeReady())
			{
				RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
			}
		}
	}

	internal bool CanUseGpuHitFlashEnvelope()
	{
		if (!_usingRuntimeManager || CachedEditorHint || !_isVisibleInTree || !IsInsideTree())
		{
			return false;
		}
		if (_runtimeGpuGraphActive)
		{
			return true;
		}
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (!GodotObject.IsInstanceValid(renderSortRootForRender) || renderSortRootForRender == this || !renderSortRootForRender._runtimeGpuGraphActive || !renderSortRootForRender._hasCachedGpuGraphCrowdState)
		{
			return false;
		}
		AdobeAnimateSprite[] array = renderSortRootForRender._cachedGpuGraphCrowdState?.GpuGraphOwners;
		for (int i = 0; i < ((array != null) ? array.Length : 0); i++)
		{
			if (array[i] == this)
			{
				return true;
			}
		}
		return false;
	}

	internal void StartGpuHitFlashEnvelope(bool white, float startTime, float strength, float duration)
	{
		AdobeAnimateGpuHitFlashState adobeAnimateGpuHitFlashState = new AdobeAnimateGpuHitFlashState(startTime, strength, duration);
		if (white)
		{
			_gpuWhiteFlash = adobeAnimateGpuHitFlashState;
		}
		else
		{
			_gpuBrightFlash = adobeAnimateGpuHitFlashState;
		}
		RequestNodeRedraw(preferDisplayCadence: true, transformOnly: false, staticStateDirty: false);
	}

	internal void ClearGpuHitFlashEnvelopes(bool clearBright, bool clearWhite)
	{
		if ((clearBright && _gpuBrightFlash.Enabled) || (clearWhite && _gpuWhiteFlash.Enabled))
		{
			if (clearBright)
			{
				_gpuBrightFlash = default(AdobeAnimateGpuHitFlashState);
			}
			if (clearWhite)
			{
				_gpuWhiteFlash = default(AdobeAnimateGpuHitFlashState);
			}
			RequestNodeRedraw(preferDisplayCadence: true, transformOnly: false, staticStateDirty: false);
		}
	}

	public void SetRenderGrayscale(bool enabled)
	{
		if (_renderGrayscale != enabled)
		{
			_renderGrayscale = enabled;
			InvalidateRenderSnapshotCache(force: true);
			if (IsNodeReady())
			{
				RequestNodeRedraw();
			}
		}
	}

	public bool IsRenderGrayscaleEnabled()
	{
		return _renderGrayscale;
	}

	public void SetRenderSelfModulate(Color selfModulate)
	{
		if (!(SelfModulate == selfModulate))
		{
			SelfModulate = selfModulate;
			_needsRenderSubmission = true;
			if (IsNodeReady())
			{
				RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
			}
		}
	}

	internal void NotifyAncestorModulateChangedForRender()
	{
		_gpuGraphCrowdPresentationStateDirty = true;
		_needsRenderSubmission = true;
		if (IsNodeReady())
		{
			RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		}
	}

	private void ApplyComposedModulate()
	{
		Modulate = _meshColor * _renderColorMultiplier;
	}

	internal void RequestRuntimeRenderSubmissionRetry()
	{
		_runtimeRenderSubmissionRetryRequested = true;
		_needsRenderSubmission = true;
	}

	internal bool MarkRuntimeRenderSubmissionConsumed()
	{
		if (_runtimeRenderSubmissionRetryRequested)
		{
			_runtimeRenderSubmissionRetryRequested = false;
			_needsRenderSubmission = true;
			return true;
		}
		_needsRenderSubmission = false;
		return false;
	}

	internal void MarkRuntimeDisplayTicked(ulong version)
	{
		_runtimeDisplayTickVersion = version;
	}

	internal bool WasRuntimeDisplayTicked(ulong version)
	{
		return _runtimeDisplayTickVersion == version;
	}

	internal void SetRuntimeManagerDispatchActive(bool active)
	{
		if (_runtimeManagerDispatchActive != active)
		{
			_runtimeManagerDispatchActive = active;
			RestoreRuntimeNativeCanvasLayer();
			InvalidateRuntimeDisplayVisualRequirementCache();
			RequestNodeRedraw();
		}
	}

	internal void InvalidateRuntimePauseDispatchCache()
	{
		AdobeAnimateRuntimeManager.NotifyPauseDispatchInvalidated(this);
	}

	public override Array<Dictionary> _GetPropertyList()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		if (_flashAnimeData == null || _flashAnimeData.clips == null || _flashAnimeData.layerDictionary == null || _flashAnimeData.mediaDictionary == null)
		{
			return array;
		}
		long num = 4L;
		if (_flashAnimeData.clips.Count > 0)
		{
			array.Add(new Dictionary
			{
				{ "name", "Animation/Clip" },
				{ "type", 4 },
				{ "hint", 2 },
				{
					"hint_string",
					"Null," + string.Join(",", _flashAnimeData.clips.Keys)
				},
				{ "usage", num }
			});
		}
		foreach (Variant key in _flashAnimeData.layerDictionary.Keys)
		{
			array.Add(new Dictionary
			{
				{
					"name",
					$"Animation/LayerVisible/{key}"
				},
				{ "type", 1 },
				{ "usage", num }
			});
		}
		foreach (Variant key2 in _flashAnimeData.mediaDictionary.Keys)
		{
			array.Add(new Dictionary
			{
				{
					"name",
					$"Animation/MediaReplace/{key2}"
				},
				{ "type", 4 },
				{ "hint", 13 },
				{ "hint_string", "*.png,*.webp,*.jpg,*.jpeg,*.svg,*.bmp,*.tga" },
				{ "usage", num }
			});
		}
		AddParentSpriteFollowLayerProperty(array, num);
		AddParentSpriteInsertLayerProperty(array, num);
		return array;
	}

	private void AddParentSpriteFollowLayerProperty(Array<Dictionary> properties, long usage)
	{
		AddParentSpriteLayerProperty(properties, usage, "Parent Sprite/Follow Layer", includeTopOption: false);
	}

	private void AddParentSpriteInsertLayerProperty(Array<Dictionary> properties, long usage)
	{
		AddParentSpriteLayerProperty(properties, usage, "Parent Sprite/Insert Layer", includeTopOption: true);
	}

	private void AddParentSpriteLayerProperty(Array<Dictionary> properties, long usage, string propertyName, bool includeTopOption)
	{
		properties.Add(new Dictionary
		{
			{ "name", propertyName },
			{ "type", 2 },
			{ "hint", 2 },
			{
				"hint_string",
				BuildParentSpriteLayerHintString(includeTopOption)
			},
			{ "usage", usage }
		});
	}

	private string BuildParentSpriteLayerHintString(bool includeTopOption)
	{
		string text = "0";
		AdobeAnimateData adobeAnimateData = ResolveParentSpriteLayerInspectorData();
		if (adobeAnimateData != null && adobeAnimateData.layerDictionary != null && adobeAnimateData.layerDictionary.Count > 0)
		{
			text = adobeAnimateData.GetLayerHintString();
		}
		if (string.IsNullOrEmpty(text))
		{
			text = "0";
		}
		if (!includeTopOption)
		{
			return text;
		}
		return "Top:-1," + text;
	}

	private AdobeAnimateData ResolveParentSpriteLayerInspectorData()
	{
		AdobeAnimateData adobeAnimateData = ResolveParentSpriteForLayerInspector()?.flashAnimeData;
		if (adobeAnimateData != null && adobeAnimateData.layerDictionary != null && adobeAnimateData.layerDictionary.Count > 0)
		{
			return adobeAnimateData;
		}
		return _flashAnimeData;
	}

	private AdobeAnimateSprite ResolveParentSpriteForLayerInspector()
	{
		if (!_parentSpriteResolved || (_parentSprite != null && !GodotObject.IsInstanceValid(_parentSprite)))
		{
			parentSprite = FindParentSpriteAncestor();
		}
		return _parentSprite;
	}

	public override void _Notification(int what)
	{
		base._Notification(what);
		if ((long)what == 20)
		{
			SealPackedSceneStaticArraysInSubtree(this);
		}
		if ((long)what == 14)
		{
			_renderTreePaused = true;
			bool num = ClearRetainedRootMotionForPause();
			InvalidateRuntimePauseDispatchCache();
			if (num)
			{
				RequestNodeRedraw();
			}
		}
		else if ((long)what == 15)
		{
			_renderTreePaused = false;
			InvalidateRuntimePauseDispatchCache();
		}
		else if ((long)what == 28 || (long)what == 29)
		{
			InvalidateRuntimePauseDispatchCache();
		}
		if ((long)what == 2000)
		{
			MarkEffectOnceBatchRenderStateDirty();
			AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
			if (GodotObject.IsInstanceValid(renderSortRootForRender) && renderSortRootForRender != this && renderSortRootForRender.ShouldUseGlobalRuntimeManager())
			{
				if (!renderSortRootForRender.TryConsumeKnownAncestorTranslationNotification())
				{
					renderSortRootForRender.MarkRenderTransformDirty();
				}
			}
			else if (!TryConsumeKnownAncestorTranslationNotification())
			{
				MarkRenderTransformDirty();
			}
		}
		else if ((long)what == 18)
		{
			if (_usingRuntimeManager)
			{
				AdobeAnimateRuntimeManager.InvalidateRenderRoots();
			}
			MarkEffectOnceBatchRenderStateDirty(invalidateZ: true);
			InvalidateRuntimePauseDispatchCache();
			_renderMountNextAuditPhysicsFrame = 0uL;
			ResetEffectiveRenderModulateAncestorCache();
			InvalidateTreeOrderPathCache();
			_parentSpriteResolved = false;
			parentSprite = FindParentSpriteAncestor();
		}
		else if ((long)what == 19)
		{
			if (_usingRuntimeManager)
			{
				AdobeAnimateRuntimeManager.InvalidateRenderRoots();
			}
			MarkEffectOnceBatchRenderStateDirty(invalidateZ: true);
			InvalidateRuntimePauseDispatchCache();
			_renderMountNextAuditPhysicsFrame = 0uL;
			ResetEffectiveRenderModulateAncestorCache();
			InvalidateTreeOrderPathCache();
			_parentSpriteResolved = true;
			parentSprite = null;
		}
	}

	internal static void SealPackedSceneStaticArraysInSubtree(Node root)
	{
		if (GodotObject.IsInstanceValid(root))
		{
			if (root is AdobeAnimateSprite adobeAnimateSprite)
			{
				adobeAnimateSprite.SealPackedSceneStaticArrays();
			}
			int childCount = root.GetChildCount();
			for (int i = 0; i < childCount; i++)
			{
				SealPackedSceneStaticArraysInSubtree(root.GetChild(i));
			}
		}
	}

	private void SealPackedSceneStaticArrays()
	{
		_layerVisible = _layerVisible?.Duplicate() ?? new Array<bool>();
		_mediaReplace = _mediaReplace?.Duplicate() ?? new Array<Texture2D>();
		_mediaReplaceUse = _mediaReplaceUse?.Duplicate() ?? new Array<bool>();
		_mediaReplaceAtlasPaths = _mediaReplaceAtlasPaths?.Duplicate() ?? new Array<string>();
		_renderStaticLayerArrayEscaped = false;
		_renderStaticMediaArraysEscaped = false;
		_renderStaticArrayAuditSubmissionPending = false;
		_renderStaticStateNextAuditPhysicsFrame = 18446744073709551615uL;
		_renderStaticAuditLayerVisibleSnapshot = null;
		_renderStaticAuditMediaReplaceUseSnapshot = null;
		_renderStaticAuditMediaReplaceSnapshot = null;
		_renderStaticAuditMediaReplaceRectSnapshot = null;
		_renderStaticAuditMediaReplacePageSnapshot = null;
		_renderStaticAuditMediaReplaceAtlasPathSnapshot = null;
		_renderStaticStateCached = false;
		AdobeAnimateRuntimeManager.SetRenderStaticArrayAuditRegistration(this, enabled: false);
	}

	public override bool _Set(StringName property, Variant value)
	{
		if (property == (StringName)"Animation/Clip")
		{
			_clip = value.AsString();
			if (_flashAnimeData != null && _flashAnimeData.clips != null && _flashAnimeData.clips.Count > 0)
			{
				SetClip(value.AsString());
			}
			return true;
		}
		string text = property.ToString();
		if (text.StartsWith("Animation/LayerVisible"))
		{
			string text2 = text.TrimPrefix("Animation/LayerVisible/");
			if (_flashAnimeData != null && _flashAnimeData.layerDictionary != null && _flashAnimeData.layerDictionary.ContainsKey(text2))
			{
				int num = (int)_flashAnimeData.layerDictionary[text2];
				if (num >= 0 && num < _layerVisible.Count)
				{
					bool flag = value.AsBool();
					if (_layerVisible[num] != flag)
					{
						_layerVisible[num] = flag;
						MarkLayerStateChanged();
					}
				}
			}
			return true;
		}
		if (text.StartsWith("Animation/MediaReplace"))
		{
			string text3 = text.TrimPrefix("Animation/MediaReplace/");
			if (_flashAnimeData != null && _flashAnimeData.mediaDictionary != null && _flashAnimeData.mediaDictionary.ContainsKey(text3))
			{
				int num2 = (int)_flashAnimeData.mediaDictionary[text3];
				if (num2 >= 0 && num2 < _mediaReplace.Count)
				{
					string text4 = value.AsString();
					bool flag2 = !string.IsNullOrWhiteSpace(text4);
					if (!GodotObject.IsInstanceValid(_mediaReplace[num2]) && num2 < _mediaReplaceAtlasPaths.Count && _mediaReplaceAtlasPaths[num2] == text4 && num2 < _mediaReplaceUse.Count && _mediaReplaceUse[num2] == flag2)
					{
						return true;
					}
					_mediaReplace[num2] = null;
					if (num2 < _mediaReplaceAtlasPaths.Count)
					{
						_mediaReplaceAtlasPaths[num2] = text4;
					}
					_mediaReplaceUse[num2] = flag2;
					InvalidateStaticAtlasPathLayoutConfigurationSnapshot();
					InvalidateStaticAtlasPathLayoutForNodeMutation();
					QueueUpdateMediaReplace();
				}
			}
			return true;
		}
		if (IsParentSpriteFollowLayerProperty(property))
		{
			SetParentSpriteFollowLayer(value.AsInt32());
			return true;
		}
		if (IsParentSpriteInsertLayerProperty(property))
		{
			SetParentSpriteInsertLayer(value.AsInt32());
			return true;
		}
		return false;
	}

	public override Variant _Get(StringName property)
	{
		if (property == (StringName)"Animation/Clip")
		{
			return _clip;
		}
		string text = property.ToString();
		if (text.StartsWith("Animation/LayerVisible"))
		{
			if (_flashAnimeData == null || _flashAnimeData.layerDictionary == null)
			{
				return default(Variant);
			}
			string text2 = text.TrimPrefix("Animation/LayerVisible/");
			if (!_flashAnimeData.layerDictionary.ContainsKey(text2))
			{
				return default(Variant);
			}
			int num = (int)_flashAnimeData.layerDictionary[text2];
			return num >= 0 && num < _layerVisible.Count && _layerVisible[num];
		}
		if (text.StartsWith("Animation/MediaReplace"))
		{
			if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null)
			{
				return default(Variant);
			}
			string text3 = text.TrimPrefix("Animation/MediaReplace/");
			if (!_flashAnimeData.mediaDictionary.ContainsKey(text3))
			{
				return default(Variant);
			}
			int num2 = (int)_flashAnimeData.mediaDictionary[text3];
			return (num2 >= 0 && num2 < _mediaReplaceAtlasPaths.Count) ? _mediaReplaceAtlasPaths[num2] : null;
		}
		if (IsParentSpriteFollowLayerProperty(property))
		{
			return followParentSpriteLayerId;
		}
		if (IsParentSpriteInsertLayerProperty(property))
		{
			return insertLayerId;
		}
		return default(Variant);
	}

	public override bool _PropertyCanRevert(StringName property)
	{
		if (_flashAnimeData == null || _flashAnimeData.clips == null)
		{
			return false;
		}
		if (property == (StringName)"Animation/Clip" && _flashAnimeData.clips.Count > 0)
		{
			return _clip != _flashAnimeData.clips.Keys.ElementAt(0).ToString();
		}
		string text = property.ToString();
		if (!text.StartsWith("Animation/LayerVisible") && !text.StartsWith("Animation/MediaReplace") && !IsParentSpriteFollowLayerProperty(property))
		{
			return IsParentSpriteInsertLayerProperty(property);
		}
		return true;
	}

	public override Variant _PropertyGetRevert(StringName property)
	{
		if (_flashAnimeData == null || _flashAnimeData.clips == null)
		{
			return default(Variant);
		}
		if (property == (StringName)"Animation/Clip" && _flashAnimeData.clips.Count > 0)
		{
			return _flashAnimeData.clips.Keys.ElementAt(0);
		}
		string text = property.ToString();
		if (text.StartsWith("Animation/LayerVisible"))
		{
			return true;
		}
		if (text.StartsWith("Animation/MediaReplace"))
		{
			return default(Variant);
		}
		if (IsParentSpriteFollowLayerProperty(property))
		{
			return 0;
		}
		if (IsParentSpriteInsertLayerProperty(property))
		{
			return -1;
		}
		return default(Variant);
	}

	private static bool IsParentSpriteFollowLayerProperty(StringName property)
	{
		if (!(property == (StringName)"Parent Sprite/Follow Layer"))
		{
			return property == (StringName)"Layer";
		}
		return true;
	}

	private static bool IsParentSpriteInsertLayerProperty(StringName property)
	{
		return property == (StringName)"Parent Sprite/Insert Layer";
	}

	private void SetParentSpriteFollowLayer(int layerId)
	{
		followParentSpriteLayerId = layerId;
		AdobeAnimateSprite adobeAnimateSprite = ResolveParentSpriteForLayerInspector();
		if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite != this)
		{
			adobeAnimateSprite.UpdateChild();
			adobeAnimateSprite.InvalidateRenderSnapshotCache();
			adobeAnimateSprite.RequestNodeRedraw();
		}
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	private void SetParentSpriteInsertLayer(int layerId)
	{
		insertLayerId = layerId;
		AdobeAnimateSprite adobeAnimateSprite = ResolveParentSpriteForLayerInspector();
		if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite != this)
		{
			adobeAnimateSprite.RefreshChildInsertLayerForEditor(this);
			return;
		}
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	public bool CanRun()
	{
		bool canRun = _canRun;
		_canRun = false;
		if (_effectOnceGpuSuppressed)
		{
			TryDisableProcess(canRun);
			return false;
		}
		if (_frozenPreview)
		{
			TryDisableProcess(canRun);
			return false;
		}
		if (!_invisible && !_isVisibleInTree)
		{
			TryDisableProcess(canRun);
			return false;
		}
		if (!HasUsableFlashAnimeData())
		{
			TryDisableProcess(canRun);
			return false;
		}
		if (CachedEditorHint && !_preview)
		{
			TryDisableProcess(canRun);
			return false;
		}
		_canRun = true;
		if (canRun != _canRun && IsNodeReady() && !clipOver)
		{
			SetProcessEnabled(enabled: true);
		}
		return true;
	}

	private void TryDisableProcess(bool prevCanRun)
	{
		if (prevCanRun && IsNodeReady())
		{
			SetProcessEnabled(enabled: false);
		}
	}

	private void UpdateVisibilityCache()
	{
		bool flag = IsVisibleInTree();
		if (_isVisibleInTree != flag)
		{
			_isVisibleInTree = flag;
			AdobeAnimateRuntimeManager.NotifyRenderRootVisibilityChanged(this);
			if (!flag)
			{
				ReleaseRuntimeManagerFirstFrameHandoff();
				AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
			}
			CanRun();
			RequestNodeRedraw();
		}
	}

	private void OnSelfVisibilityChanged()
	{
		UpdateVisibilityCache();
		if (_runtimeFollowVisibilityKnown && base.Visible != _runtimeFollowVisibilityValue)
		{
			base.Visible = _runtimeFollowVisibilityValue;
		}
	}

	private void SetProcessEnabled(bool enabled)
	{
		if ((_frozenPreview || _effectOnceGpuSuppressed) && enabled)
		{
			enabled = false;
		}
		InvalidateRuntimePauseDispatchCache();
		InvalidateRuntimeDisplayVisualRequirementCache();
		_processEnabled = enabled;
		if (!enabled)
		{
			ReleaseRuntimeManagerFirstFrameHandoff();
			RestoreRuntimeNativeCanvasLayer();
			if (_usingRuntimeManager)
			{
				AdobeAnimateRuntimeManager.Unregister(this);
				_usingRuntimeManager = false;
			}
			SetProcess(enable: false);
			SetPhysicsProcess(enable: false);
			return;
		}
		if (ShouldUseGlobalRuntimeManager() && IsInsideTree())
		{
			bool flag = !_usingRuntimeManager;
			ReleaseRuntimeManagerFirstFrameHandoff();
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
			_usingRuntimeManager = AdobeAnimateRuntimeManager.Register(this);
			if (_usingRuntimeManager && flag)
			{
				SubmitRuntimeManagerFirstFrameHandoff();
			}
		}
		else if (_usingRuntimeManager)
		{
			AdobeAnimateRuntimeManager.Unregister(this);
			_usingRuntimeManager = false;
		}
		if (_usingRuntimeManager)
		{
			SetProcess(enable: false);
			SetPhysicsProcess(enable: false);
			_needsRenderSubmission = true;
		}
		else
		{
			SetProcess(enable: true);
			SetPhysicsProcess(enable: false);
		}
	}

	private void ConnectVisibilitySignals()
	{
		if (!_selfVisibilityChangedConnected)
		{
			base.VisibilityChanged += OnSelfVisibilityChanged;
			_selfVisibilityChangedConnected = true;
		}
		if (!(GetParent() is CanvasItem canvasItem))
		{
			return;
		}
		if (_visibilityParentCanvas != canvasItem)
		{
			if (_parentVisibilityChangedConnected && GodotObject.IsInstanceValid(_visibilityParentCanvas))
			{
				_visibilityParentCanvas.VisibilityChanged -= UpdateVisibilityCache;
			}
			_parentVisibilityChangedConnected = false;
			_visibilityParentCanvas = canvasItem;
		}
		if (!_parentVisibilityChangedConnected)
		{
			_visibilityParentCanvas.VisibilityChanged += UpdateVisibilityCache;
			_parentVisibilityChangedConnected = true;
		}
	}

	private void DisconnectVisibilitySignals()
	{
		if (_parentVisibilityChangedConnected && GodotObject.IsInstanceValid(_visibilityParentCanvas))
		{
			_visibilityParentCanvas.VisibilityChanged -= UpdateVisibilityCache;
		}
		_parentVisibilityChangedConnected = false;
		_visibilityParentCanvas = null;
		if (_selfVisibilityChangedConnected)
		{
			base.VisibilityChanged -= OnSelfVisibilityChanged;
		}
		_selfVisibilityChangedConnected = false;
	}

	private void StopRuntimeTickKeepRender()
	{
		_needsRenderSubmission = true;
		SetProcessEnabled(enabled: false);
		if (!_usingRuntimeManager)
		{
			QueueRedraw();
		}
	}

	public void RefreshProcessScheduling()
	{
		CanRun();
		SetProcessEnabled(_canRun && !clipOver);
	}

	public override void _EnterTree()
	{
		_runtimeInsideTree = true;
		_runtimeParentStateSource = null;
		_runtimeParentStateValidationCountdown = 0;
		ResetRuntimeFollowVisibilityCache();
		InvalidateRuntimeDisplayVisualRequirementCache();
		_renderTreePaused = GetTree()?.Paused ?? false;
		_cachedInstanceIdForRender = GetInstanceId();
		parentSprite = FindParentSpriteAncestor();
		InvalidateRuntimePauseDispatchCache();
		RefreshTransformNotificationMode();
		MarkRenderTransformDirty();
		ConnectVisibilitySignals();
		RefreshRenderMountCache();
		if (IsNodeReady())
		{
			_runtimeReadyCached = true;
			_isVisibleInTree = IsVisibleInTree();
			RefreshProcessScheduling();
		}
	}

	internal ulong GetCachedInstanceIdForRender()
	{
		if (_cachedInstanceIdForRender == 0L)
		{
			_cachedInstanceIdForRender = GetInstanceId();
		}
		return _cachedInstanceIdForRender;
	}

	internal bool ConsumeRuntimeGpuPrepareRequiredForRender()
	{
		bool runtimeGpuPrepareRequired = _runtimeGpuPrepareRequired;
		_runtimeGpuPrepareRequired = false;
		return runtimeGpuPrepareRequired;
	}

	internal bool HasPreparedGpuRenderGraph(ulong generation)
	{
		if (generation != 0L)
		{
			return _gpuGraphPreparedGeneration == generation;
		}
		return false;
	}

	internal int GetPreparedGpuRenderGraphQuadCount(ulong generation)
	{
		if (!HasPreparedGpuRenderGraph(generation))
		{
			return 1;
		}
		return _gpuGraphPreparedQuadCount;
	}

	internal void MarkGpuRenderGraphPrepared(ulong generation, ulong signature, int quadCount)
	{
		_gpuGraphPreparedGeneration = generation;
		_gpuGraphPreparedSignature = signature;
		_gpuGraphPreparedQuadCount = Math.Max(1, quadCount);
	}

	internal bool HasPreparedGpuRenderGraphState(ulong generation, ulong signature)
	{
		if (HasPreparedGpuRenderGraph(generation) && signature != 0L)
		{
			return _gpuGraphPreparedSignature == signature;
		}
		return false;
	}

	internal object GetPreparedGpuRootCacheForRender()
	{
		return _preparedGpuRootCacheForRender;
	}

	internal void StorePreparedGpuRootCacheForRender(object entry)
	{
		_preparedGpuRootCacheForRender = entry;
	}

	internal void ClearPreparedGpuRootCacheForRender()
	{
		_preparedGpuRootCacheForRender = null;
	}

	internal void InvalidateGpuRenderGraphPreparation()
	{
		_gpuGraphPreparedGeneration = 0uL;
		_gpuGraphPreparedSignature = 0uL;
		_gpuGraphPreparedQuadCount = 1;
		_preparedGpuRootCacheForRender = null;
		_hasCachedGpuGraphCrowdState = false;
		_cachedGpuGraphCrowdState = null;
		_cachedGpuGraphCrowdStatePhysicsFrame = 18446744073709551615uL;
		_hasCachedRasterCompositeRenderState = false;
		_cachedRasterCompositeRenderState = default(AdobeAnimateRasterCompositeRenderState);
		_gpuGraphCrowdStaticStateDirty = false;
		_gpuGraphCrowdPresentationStateDirty = false;
		_gpuGraphCrowdStateNextAuditPhysicsFrame = 18446744073709551615uL;
	}

	public override void _Ready()
	{
		_isVisibleInTree = IsVisibleInTree();
		canvasItem = GetCanvasItem();
		RefreshRenderMountCache();
		ApplyPendingFlashAnimeDataChange();
		TryHydrateFlashAnimeDataBeforeFileChange();
		CanRun();
		parentSprite = FindParentSpriteAncestor();
		frameIndex = clipRange.X;
		elapsedTimer = GD.RandRange(0.1, 1.0);
		clipOver = false;
		if (!HasUsableFlashAnimeData())
		{
			SetProcessEnabled(enabled: false);
			_runtimeReadyCached = true;
			return;
		}
		if (_clip != "" && _flashAnimeData.clips.ContainsKey(_clip))
		{
			clipRange = _flashAnimeData.GetClip(_clip);
			frameIndex = clipRange.X;
		}
		if (HasAnyRequestedMediaReplace() || needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		onlyDraw = false;
		meshTexture = default(Rid);
		mesh = null;
		base.Material = null;
		ApplyComposedModulate();
		CacheChildren();
		RefreshTransformNotificationModeForRenderTree();
		UpdateChildPoseImmediate();
		_processEnabled = false;
		SetProcessEnabled(_canRun && !clipOver);
		RequestNodeRedraw();
		_runtimeReadyCached = true;
	}

	public override void _ExitTree()
	{
		_effectOnceGpuSuppressed = false;
		_effectOnceGpuSuppressionToken++;
		if (_effectOnceGpuSuppressionToken == 0)
		{
			_effectOnceGpuSuppressionToken = 1;
		}
		_runtimeReadyCached = false;
		_runtimeRenderSubmissionRetryRequested = false;
		_runtimeParentStateSource = null;
		_runtimeParentStateValidationCountdown = 0;
		ResetRuntimeFollowVisibilityCache();
		InvalidateRuntimeDisplayVisualRequirementCache();
		_runtimeInsideTree = false;
		_isVisibleInTree = false;
		RestoreRuntimeNativeCanvasLayer();
		RestoreExternalVisualNativeFallbacks((long)Engine.GetProcessFrames());
		ReleaseRuntimeManagerFirstFrameHandoff();
		AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
		AdobeAnimateRenderManager.InvalidateGpuRenderGraph(this, AdobeAnimateGpuGraphInvalidationReason.OwnerExit);
		InvalidateGpuRenderGraphPreparation();
		InvalidateRuntimePauseDispatchCache();
		SetNotifyTransform(enable: false);
		_renderGlobalTransformCached = false;
		_renderGlobalTransformDirty = true;
		_renderTransformChangedInPhysicsFrame = false;
		_runtimePhysicsAncestorTransformPending = false;
		_knownAncestorTranslationNotificationPending = false;
		_renderRootMotionEnabled = false;
		_renderTreePaused = false;
		_renderGlobalTransformPhysicsFrame = 0uL;
		if (_usingRuntimeManager)
		{
			AdobeAnimateRuntimeManager.Unregister(this);
			_usingRuntimeManager = false;
		}
		DisconnectVisibilitySignals();
		_cachedRenderMountParent = null;
		_cachedViewport = null;
		_renderMountNextAuditPhysicsFrame = 0uL;
		ResetEffectiveRenderModulateAncestorCache();
		DisconnectTreeOrderPathWatchers();
		_treeOrderPathRescanCountdown = 0;
		DisconnectManagedSlotGpuStateWatchers();
		_cachedCanvasLayer = 0;
		_runtimeDefinition = null;
	}

	internal void GetEffectOnceBatchEligibilityVersions(out int layerStateVersion, out int mediaReplaceStateVersion)
	{
		layerStateVersion = _layerStateVersion;
		mediaReplaceStateVersion = _mediaReplaceStateVersion;
	}

	internal Transform2D GetEffectOnceBatchRenderTransform()
	{
		GetEffectOnceBatchRenderState(out var transform, out var _);
		return transform;
	}

	internal int GetEffectOnceBatchEffectiveZIndex()
	{
		GetEffectOnceBatchRenderState(out var _, out var effectiveZIndex);
		return effectiveZIndex;
	}

	internal int GetEffectOnceBatchRenderRevision(ulong processFrame)
	{
		if (_effectOnceBatchRenderStateDirty || processFrame < _effectOnceBatchRenderNextAuditFrame)
		{
			return _effectOnceBatchRenderRevision;
		}
		int zIndex = ZIndex;
		bool zAsRelative = ZAsRelative;
		if (_effectOnceBatchObservedLocalZIndex != zIndex || _effectOnceBatchObservedZAsRelative != zAsRelative)
		{
			_effectOnceBatchObservedLocalZIndex = zIndex;
			_effectOnceBatchObservedZAsRelative = zAsRelative;
			MarkEffectOnceBatchRenderStateDirty(invalidateZ: true);
			return _effectOnceBatchRenderRevision;
		}
		_effectiveZIndexCached = false;
		int cachedEffectiveZIndexForRender = GetCachedEffectiveZIndexForRender();
		_effectOnceBatchRenderNextAuditFrame = processFrame + 256;
		if (cachedEffectiveZIndexForRender == _effectOnceBatchEffectiveZIndex)
		{
			return _effectOnceBatchRenderRevision;
		}
		_effectOnceBatchEffectiveZIndex = cachedEffectiveZIndexForRender;
		_effectOnceBatchRenderRevision++;
		this.EffectOnceBatchRenderStateChanged?.Invoke();
		return _effectOnceBatchRenderRevision;
	}

	internal void GetEffectOnceBatchRenderState(out Transform2D transform, out int effectiveZIndex)
	{
		GetEffectOnceBatchRenderState(Engine.GetProcessFrames(), out transform, out effectiveZIndex);
	}

	internal void GetEffectOnceBatchRenderState(ulong processFrame, out Transform2D transform, out int effectiveZIndex)
	{
		if (_effectOnceBatchRenderStateDirty)
		{
			Transform2D effectOnceBatchRenderTransform = GetCachedGlobalTransformForRender();
			if (offset != Vector2.Zero)
			{
				effectOnceBatchRenderTransform = effectOnceBatchRenderTransform.TranslatedLocal(offset);
			}
			_effectOnceBatchRenderTransform = effectOnceBatchRenderTransform;
			_effectOnceBatchEffectiveZIndex = GetCachedEffectiveZIndexForRender();
			_effectOnceBatchObservedLocalZIndex = _effectiveZIndexCacheLocalZ;
			_effectOnceBatchObservedZAsRelative = _effectiveZIndexCacheZAsRelative;
			_effectOnceBatchRenderNextAuditFrame = processFrame + 1 + GetCachedInstanceIdForRender() % 256;
			_effectOnceBatchRenderStateDirty = false;
		}
		else if (processFrame >= _effectOnceBatchRenderNextAuditFrame)
		{
			_effectiveZIndexCached = false;
			_effectOnceBatchEffectiveZIndex = GetCachedEffectiveZIndexForRender();
			_effectOnceBatchRenderNextAuditFrame = processFrame + 256;
		}
		transform = _effectOnceBatchRenderTransform;
		effectiveZIndex = _effectOnceBatchEffectiveZIndex;
	}

	public string BuildCrowdFilterDebugReport(string label = "", int maxFrameSlices = 256)
	{
		StringBuilder stringBuilder = new StringBuilder(8192);
		string value = (IsInsideTree() ? GetPath().ToString() : ((string?)base.Name));
		if (string.IsNullOrEmpty(value))
		{
			value = "<unnamed>";
		}
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		RefreshRenderStaticStateCache(force: true);
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		bool value2 = runtimeDefinition != null;
		bool value3 = HasManagedSlotSpritesForRender();
		bool needsDrawOrderSortBandsForRender = GetNeedsDrawOrderSortBandsForRender();
		bool value4 = runtimeDefinition != null && runtimeDefinition.UsesGpuPoseTextureArray && runtimeDefinition.GpuPoseTextureRid.IsValid && GodotObject.IsInstanceValid(runtimeDefinition.GpuPoseTextureArray);
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		Transform2D transform2D = AdobeAnimateRenderManager.ToRenderMountLocalTransform(GetCachedRenderMountParent(), cachedGlobalTransformForRender);
		AdobeAnimateGpuRenderGraphDefinition graph;
		AdobeAnimateSprite[] ownerSprites;
		string failureReason;
		bool flag = AdobeAnimateGpuRenderGraphBuilder.TryBuild(this, out graph, out ownerSprites, out failureReason);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(38, 2, stringBuilder2);
		handler.AppendLiteral("[AdobeAnimateRenderDebug] label=");
		handler.AppendFormatted(label);
		handler.AppendLiteral(" path=");
		handler.AppendFormatted(value);
		stringBuilder3.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(57, 6, stringBuilder2);
		handler.AppendLiteral("  data=");
		handler.AppendFormatted(_flashAnimeData?.ResourcePath ?? "<null>");
		handler.AppendLiteral(" clip=");
		handler.AppendFormatted(_clip);
		handler.AppendLiteral(" clipRange=");
		handler.AppendFormatted(clipRange);
		handler.AppendLiteral(" frameIndex=");
		handler.AppendFormatted(frameIndex);
		handler.AppendLiteral(" elapsed=");
		handler.AppendFormatted(elapsedTimer, "F4");
		handler.AppendLiteral(" frameFloat=");
		handler.AppendFormatted(GetRenderFrameFloat(), "F4");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(44, 6, stringBuilder2);
		handler.AppendLiteral("  transform global=(");
		handler.AppendFormatted(cachedGlobalTransformForRender.Origin.X, "F2");
		handler.AppendLiteral(",");
		handler.AppendFormatted(cachedGlobalTransformForRender.Origin.Y, "F2");
		handler.AppendLiteral(") render=(");
		handler.AppendFormatted(transform2D.Origin.X, "F2");
		handler.AppendLiteral(",");
		handler.AppendFormatted(transform2D.Origin.Y, "F2");
		handler.AppendLiteral(") offset=(");
		handler.AppendFormatted(offset.X, "F2");
		handler.AppendLiteral(",");
		handler.AppendFormatted(offset.Y, "F2");
		handler.AppendLiteral(")");
		stringBuilder5.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(114, 7, stringBuilder2);
		handler.AppendLiteral("  runtime canRun=");
		handler.AppendFormatted(_canRun);
		handler.AppendLiteral(" processEnabled=");
		handler.AppendFormatted(_processEnabled);
		handler.AppendLiteral(" pause=");
		handler.AppendFormatted(_pause);
		handler.AppendLiteral(" usingRuntimeManager=");
		handler.AppendFormatted(_usingRuntimeManager);
		handler.AppendLiteral(" visibleInTree=");
		handler.AppendFormatted(_isVisibleInTree);
		handler.AppendLiteral(" refreshEveryFrame=");
		handler.AppendFormatted(refreshEveryFrame);
		handler.AppendLiteral(" refreshEveryFlame=");
		handler.AppendFormatted(refreshEveryFlame);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(141, 9, stringBuilder2);
		handler.AppendLiteral("  gpuPose capable=");
		handler.AppendFormatted(value4);
		handler.AppendLiteral(" hasDefinition=");
		handler.AppendFormatted(value2);
		handler.AppendLiteral(" atlasArrayRid=");
		handler.AppendFormatted(runtimeDefinition?.AtlasTextureArrayRid.IsValid ?? false);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("poseRid=");
		handler.AppendFormatted(runtimeDefinition?.GpuPoseTextureRid.IsValid ?? false);
		handler.AppendLiteral(" poseTextureArrayValid=");
		handler.AppendFormatted(GodotObject.IsInstanceValid(runtimeDefinition?.GpuPoseTextureArray));
		handler.AppendLiteral(" ");
		handler.AppendLiteral("maxSlices=");
		handler.AppendFormatted(runtimeDefinition?.MaxFrameSliceCount ?? 0);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("repeatedMedia=");
		handler.AppendFormatted(runtimeDefinition?.HasRepeatedMediaPerFrame ?? false);
		handler.AppendLiteral(" managedSlotSprites=");
		handler.AppendFormatted(value3);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("drawOrderSort=");
		handler.AppendFormatted(needsDrawOrderSortBandsForRender);
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(30, 2, stringBuilder2);
		handler.AppendLiteral("  gpuGraph buildable=");
		handler.AppendFormatted(flag);
		handler.AppendLiteral(" failure=");
		handler.AppendFormatted(flag ? "<none>" : failureReason);
		stringBuilder8.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder9 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(59, 5, stringBuilder2);
		handler.AppendLiteral("  layer count=");
		handler.AppendFormatted(_cachedLayerVisibleCount);
		handler.AppendLiteral(" allVisible=");
		handler.AppendFormatted(_cachedAllLayersVisible);
		handler.AppendLiteral(" canUseMask=");
		handler.AppendFormatted(_cachedCanUseLayerMask);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("mask=0x");
		handler.AppendFormatted(_cachedLayerMask, "X16");
		handler.AppendLiteral(" signature=0x");
		handler.AppendFormatted(_cachedLayerVisibleSignature, "X16");
		stringBuilder9.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder10 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(76, 6, stringBuilder2);
		handler.AppendLiteral("  mediaReplace has=");
		handler.AppendFormatted(_cachedHasMediaReplace);
		handler.AppendLiteral(" sharedAtlas=");
		handler.AppendFormatted(mediaReplaceAtlasShared);
		handler.AppendLiteral(" atlasValid=");
		handler.AppendFormatted(_cachedMediaReplaceAtlasRid.IsValid);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("limit=");
		handler.AppendFormatted(_cachedMediaReplaceLimit);
		handler.AppendLiteral(" signature=0x");
		handler.AppendFormatted(_cachedMediaReplaceSignature, "X16");
		handler.AppendLiteral(" boundsGrow=");
		handler.AppendFormatted(_cachedMediaReplaceBoundsGrow, "F2");
		stringBuilder10.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder11 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(33, 3, stringBuilder2);
		handler.AppendLiteral("  verticalClip enabled=");
		handler.AppendFormatted(_verticalClip.Enabled);
		handler.AppendLiteral(" up=");
		handler.AppendFormatted(_verticalClip.UpY, "F2");
		handler.AppendLiteral(" down=");
		handler.AppendFormatted(_verticalClip.DownY, "F2");
		stringBuilder11.AppendLine(ref handler);
		AppendCrowdFilterBoundsDebug(stringBuilder, runtimeDefinition);
		AppendCrowdFilterLayerDebug(stringBuilder);
		AppendCrowdFilterMediaDebug(stringBuilder);
		AppendCrowdFilterFrameDebug(stringBuilder, runtimeDefinition, maxFrameSlices);
		return stringBuilder.ToString();
	}

	public void PrintCrowdFilterDebugReport(string label = "", int maxFrameSlices = 256)
	{
		GD.Print(BuildCrowdFilterDebugReport(label, maxFrameSlices));
	}

	internal void GetRuntimeCrowdCullingDebugState(out bool cached, out bool cachedVisible, out bool currentVisible, out bool currentVisibleWithPrefetch, out bool nativeCanvasSuppressed)
	{
		cached = _runtimeRenderBoundsCached;
		cachedVisible = _cachedRuntimeRenderBoundsVisible;
		currentVisible = true;
		currentVisibleWithPrefetch = true;
		nativeCanvasSuppressed = _runtimeNativeCanvasSuppressed;
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null)
		{
			return;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		Viewport viewport = ResolveRenderViewport(cachedRenderMountParent);
		if (GodotObject.IsInstanceValid(viewport))
		{
			Rect2 cachedViewportWorldRect = GetCachedViewportWorldRect(viewport);
			if (!(cachedViewportWorldRect.Size.X <= 0f) && !(cachedViewportWorldRect.Size.Y <= 0f))
			{
				Rect2 b = TransformRect(rect: new Rect2(runtimeDefinition.LocalBounds.Position + offset, runtimeDefinition.LocalBounds.Size), transform: GetCachedGlobalTransformForRender());
				currentVisible = cachedViewportWorldRect.Intersects(b, includeBorders: true);
				currentVisibleWithPrefetch = cachedViewportWorldRect.Grow(256f).Intersects(b, includeBorders: true);
			}
		}
	}

	private void AppendCrowdFilterBoundsDebug(StringBuilder sb, AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			sb.AppendLine("  bounds: <no definition>");
			return;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		Viewport viewport = ((GodotObject.IsInstanceValid(cachedRenderMountParent) && cachedRenderMountParent is Viewport viewport2) ? viewport2 : GetViewport());
		Rect2 rect = (GodotObject.IsInstanceValid(viewport) ? GetCachedViewportWorldRect(viewport) : default(Rect2));
		Rect2 renderLocalBounds = GetRenderLocalBounds(definition);
		Rect2 rect2 = new Rect2(renderLocalBounds.Position + offset, renderLocalBounds.Size);
		Rect2 rect3 = TransformRect(GetCachedGlobalTransformForRender(), rect2);
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(50, 5, sb);
		handler.AppendLiteral("  bounds local=");
		handler.AppendFormatted(FormatDebugRect(renderLocalBounds));
		handler.AppendLiteral(" offset=");
		handler.AppendFormatted(FormatDebugRect(rect2));
		handler.AppendLiteral(" ");
		handler.AppendLiteral("render=");
		handler.AppendFormatted(FormatDebugRect(rect3));
		handler.AppendLiteral(" viewport=");
		handler.AppendFormatted(FormatDebugRect(rect));
		handler.AppendLiteral(" visible=");
		handler.AppendFormatted(rect.Intersects(rect3, includeBorders: true));
		sb.AppendLine(ref handler);
	}

	private void AppendCrowdFilterLayerDebug(StringBuilder sb)
	{
		sb.AppendLine("  layers:");
		if (_cachedLayerVisibleCount <= 0)
		{
			sb.AppendLine("    <none>");
			return;
		}
		for (int i = 0; i < _cachedLayerVisibleCount; i++)
		{
			bool flag = IsLayerVisibleFromArrayForDebug(i);
			bool flag2 = LayerHasSlicesInCurrentClip(i);
			bool value = flag || !flag2;
			StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(43, 5, sb);
			handler.AppendLiteral("    [");
			handler.AppendFormatted(i);
			handler.AppendLiteral("] ");
			handler.AppendFormatted(GetLayerNameForDebug(i));
			handler.AppendLiteral(" visible=");
			handler.AppendFormatted(flag);
			handler.AppendLiteral(" activeClip=");
			handler.AppendFormatted(flag2);
			handler.AppendLiteral(" renderVisible=");
			handler.AppendFormatted(value);
			sb.AppendLine(ref handler);
		}
	}

	private void AppendCrowdFilterMediaDebug(StringBuilder sb)
	{
		sb.AppendLine("  mediaReplace:");
		if (_cachedMediaReplaceLimit <= 0)
		{
			sb.AppendLine("    <none>");
			return;
		}
		bool flag = false;
		for (int i = 0; i < _cachedMediaReplaceLimit; i++)
		{
			if (_mediaReplaceUse != null && i < _mediaReplaceUse.Count && _mediaReplaceUse[i])
			{
				flag = true;
				Texture2D texture2D = ((_mediaReplace != null && i < _mediaReplace.Count) ? _mediaReplace[i] : null);
				Rect2 rect = ((mediaReplaceRect != null && i < mediaReplaceRect.Count) ? mediaReplaceRect[i] : default(Rect2));
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(32, 5, sb);
				handler.AppendLiteral("    [");
				handler.AppendFormatted(i);
				handler.AppendLiteral("] ");
				handler.AppendFormatted(GetMediaNameForDebug(i));
				handler.AppendLiteral(" texValid=");
				handler.AppendFormatted(GodotObject.IsInstanceValid(texture2D));
				handler.AppendLiteral(" ");
				handler.AppendLiteral("texPath=");
				handler.AppendFormatted(texture2D?.ResourcePath ?? "<null>");
				handler.AppendLiteral(" rect=");
				handler.AppendFormatted(FormatDebugRect(rect));
				sb.AppendLine(ref handler);
			}
		}
		if (!flag)
		{
			sb.AppendLine("    <no active media replacements>");
		}
	}

	private void AppendCrowdFilterFrameDebug(StringBuilder sb, AdobeAnimateRuntimeDefinition definition, int maxFrameSlices)
	{
		sb.AppendLine("  currentFrameSlices:");
		if (definition == null || definition.Frames == null || definition.SliceMetadata == null || definition.Frames.Length == 0)
		{
			sb.AppendLine("    <no definition/frame data>");
			return;
		}
		string info;
		Image gpuPoseDebugImage = GetGpuPoseDebugImage(definition, out info);
		StringBuilder stringBuilder = sb;
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(48, 4, stringBuilder);
		handler.AppendLiteral("    poseTexture ");
		handler.AppendFormatted(info);
		handler.AppendLiteral(" baked=");
		handler.AppendFormatted(definition.UsesBakedGpuPoseTexture);
		handler.AppendLiteral(" ");
		handler.AppendLiteral("baseTexel=");
		handler.AppendFormatted(definition.GpuPoseTextureBaseTexel);
		handler.AppendLiteral(" expectedSize=");
		handler.AppendFormatted(definition.GpuPoseTextureSize);
		stringBuilder2.AppendLine(ref handler);
		float num = GetRenderFrameFloat();
		if (clipRange != Vector2I.Zero)
		{
			num = Mathf.Clamp(num, clipRange.X, Math.Max(clipRange.X, clipRange.Y - 1));
		}
		int num2 = Mathf.Clamp(Mathf.FloorToInt(num), 0, definition.Frames.Length - 1);
		float value = Mathf.Clamp(num - (float)num2, 0f, 1f);
		PackedFrame packedFrame = definition.Frames[num2];
		int num3 = Math.Max(0, packedFrame.Count);
		int num4 = ((maxFrameSlices <= 0) ? num3 : Math.Min(num3, maxFrameSlices));
		stringBuilder = sb;
		StringBuilder stringBuilder3 = stringBuilder;
		handler = new StringBuilder.AppendInterpolatedStringHandler(41, 5, stringBuilder);
		handler.AppendLiteral("    frame=");
		handler.AppendFormatted(num2);
		handler.AppendLiteral(" alpha=");
		handler.AppendFormatted(value, "F4");
		handler.AppendLiteral(" offset=");
		handler.AppendFormatted(packedFrame.Offset);
		handler.AppendLiteral(" count=");
		handler.AppendFormatted(num3);
		handler.AppendLiteral(" printed=");
		handler.AppendFormatted(num4);
		stringBuilder3.AppendLine(ref handler);
		int num5 = Math.Min(packedFrame.Offset + num4, definition.SliceMetadata.Length);
		int num6 = packedFrame.Offset;
		int num7 = 0;
		while (num6 < num5)
		{
			ref PackedSliceMetadata reference = ref definition.SliceMetadata[num6];
			int layerId = reference.LayerId;
			int mediaId = reference.MediaId;
			bool flag = IsLayerVisibleFromArrayForDebug(layerId);
			bool value2 = flag || !LayerHasSlicesInCurrentClip(layerId);
			bool flag2 = IsMediaReplacedCached(mediaId);
			Rect2 rect = ((mediaId < definition.MediaRects.Length) ? definition.MediaRects[mediaId] : default(Rect2));
			Rect2 rect2 = ((flag2 && mediaReplaceRect != null && mediaId < mediaReplaceRect.Count) ? mediaReplaceRect[mediaId] : default(Rect2));
			int num8 = definition.GpuPoseTextureBaseTexel + num6 * 5;
			Color color;
			bool flag3 = TryReadGpuPoseTexelForDebug(gpuPoseDebugImage, num8 + 1, out color);
			Color color2;
			bool flag4 = TryReadGpuPoseTexelForDebug(gpuPoseDebugImage, num8 + 2, out color2);
			int value3 = (flag3 ? Mathf.RoundToInt(color.A) : (-1));
			stringBuilder = sb;
			StringBuilder stringBuilder4 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(174, 18, stringBuilder);
			handler.AppendLiteral("    slot=");
			handler.AppendFormatted(num7);
			handler.AppendLiteral(" source=");
			handler.AppendFormatted(num6);
			handler.AppendLiteral(" sliceKey=");
			handler.AppendFormatted(reference.SliceKey);
			handler.AppendLiteral(" layer=");
			handler.AppendFormatted(layerId);
			handler.AppendLiteral(":");
			handler.AppendFormatted(GetLayerNameForDebug(layerId));
			handler.AppendLiteral(" ");
			handler.AppendLiteral("drawOrder=");
			handler.AppendFormatted(reference.DrawOrder);
			handler.AppendLiteral(" visible=");
			handler.AppendFormatted(flag);
			handler.AppendLiteral(" renderVisible=");
			handler.AppendFormatted(value2);
			handler.AppendLiteral(" media=");
			handler.AppendFormatted(mediaId);
			handler.AppendLiteral(":");
			handler.AppendFormatted(GetMediaNameForDebug(mediaId));
			handler.AppendLiteral(" ");
			handler.AppendLiteral("replaced=");
			handler.AppendFormatted(flag2);
			handler.AppendLiteral(" baseRect=");
			handler.AppendFormatted(FormatDebugRect(rect));
			handler.AppendLiteral(" replaceRect=");
			handler.AppendFormatted(FormatDebugRect(rect2));
			handler.AppendLiteral(" alpha=");
			handler.AppendFormatted(flag3 ? color.B : 0f, "F3");
			handler.AppendLiteral(" ");
			handler.AppendLiteral("poseBaseTexel=");
			handler.AppendFormatted(num8);
			handler.AppendLiteral(" poseTransform=");
			handler.AppendFormatted(flag3 ? FormatDebugColor(color) : "<unreadable>");
			handler.AppendLiteral(" ");
			handler.AppendLiteral("poseAtlasLayer=");
			handler.AppendFormatted(value3);
			handler.AppendLiteral(" poseRect=");
			handler.AppendFormatted(flag4 ? FormatDebugColor(color2) : "<unreadable>");
			stringBuilder4.AppendLine(ref handler);
			num6++;
			num7++;
		}
		if (num4 < num3)
		{
			stringBuilder = sb;
			StringBuilder stringBuilder5 = stringBuilder;
			handler = new StringBuilder.AppendInterpolatedStringHandler(25, 1, stringBuilder);
			handler.AppendLiteral("    ... truncated ");
			handler.AppendFormatted(num3 - num4);
			handler.AppendLiteral(" slices");
			stringBuilder5.AppendLine(ref handler);
		}
	}

	private static Image GetGpuPoseDebugImage(AdobeAnimateRuntimeDefinition definition, out string info)
	{
		info = "image=<none>";
		if (definition == null)
		{
			info = "image=<no definition>";
			return null;
		}
		Image image = (GodotObject.IsInstanceValid(definition.GpuPoseTexture) ? definition.GpuPoseTexture.GetImage() : null);
		string text = definition.GpuPoseTexture?.ResourcePath ?? "<runtime>";
		if ((image == null || image.IsEmpty()) && definition.UsesGpuPoseTextureArray)
		{
			text = definition.GpuPoseTextureArray?.ResourcePath ?? "<runtime-array>";
			if (GodotObject.IsInstanceValid(definition.GpuPoseTextureArray) && definition.GpuPoseTextureLayer >= 0 && definition.GpuPoseTextureLayer < definition.GpuPoseTextureArray.GetLayers())
			{
				image = definition.GpuPoseTextureArray.GetLayerData(definition.GpuPoseTextureLayer);
			}
		}
		if (image == null || image.IsEmpty())
		{
			info = "image=<empty> texturePath=" + text;
			return null;
		}
		info = $"image=({image.GetWidth()}x{image.GetHeight()} {image.GetFormat()}) texturePath={text}";
		return image;
	}

	private static bool TryReadGpuPoseTexelForDebug(Image image, int texelIndex, out Color color)
	{
		color = default(Color);
		if (image == null || image.IsEmpty() || texelIndex < 0)
		{
			return false;
		}
		int width = image.GetWidth();
		int height = image.GetHeight();
		if (width <= 0 || height <= 0)
		{
			return false;
		}
		int num = texelIndex / width;
		int num2 = texelIndex - num * width;
		if (num < 0 || num >= height || num2 < 0 || num2 >= width)
		{
			return false;
		}
		color = image.GetPixel(num2, num);
		return true;
	}

	private bool IsLayerVisibleFromArrayForDebug(int layerId)
	{
		if (_layerVisible != null && layerId >= 0 && layerId < _layerVisible.Count)
		{
			return _layerVisible[layerId];
		}
		return true;
	}

	private string GetLayerNameForDebug(int layerId)
	{
		if (_flashAnimeData?.layerDictionary == null)
		{
			return "<no-layer-dictionary>";
		}
		foreach (Variant key in _flashAnimeData.layerDictionary.Keys)
		{
			if ((int)_flashAnimeData.layerDictionary[key] == layerId)
			{
				return key.ToString();
			}
		}
		return "<unknown>";
	}

	private string GetMediaNameForDebug(int mediaId)
	{
		if (_flashAnimeData?.mediaDictionary == null)
		{
			return "<no-media-dictionary>";
		}
		foreach (Variant key in _flashAnimeData.mediaDictionary.Keys)
		{
			if ((int)_flashAnimeData.mediaDictionary[key] == mediaId)
			{
				return key.ToString();
			}
		}
		return "<unknown>";
	}

	private static string FormatDebugRect(Rect2 rect)
	{
		return $"({rect.Position.X:F2},{rect.Position.Y:F2},{rect.Size.X:F2},{rect.Size.Y:F2})";
	}

	private static string FormatDebugColor(Color color)
	{
		return $"({color.R:F6},{color.G:F6},{color.B:F6},{color.A:F6})";
	}

	public AdobeAnimateExternalVisualHandle RegisterExternalVisual(Sprite2D sprite, AdobeAnimateExternalVisualDescriptor descriptor)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return AdobeAnimateExternalVisualHandle.Invalid;
		}
		if (descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot && !GodotObject.IsInstanceValid(descriptor.Slot))
		{
			return AdobeAnimateExternalVisualHandle.Invalid;
		}
		Transform2D registeredWorldTransform = sprite.GlobalTransform;
		Transform2D globalTransform = base.GlobalTransform;
		Transform2D transform2D = ((descriptor.AttachmentMode != AdobeAnimateExternalVisualAttachmentMode.Slot) ? (globalTransform.AffineInverse() * registeredWorldTransform) : (descriptor.Slot.GlobalTransform.AffineInverse() * registeredWorldTransform));
		Transform2D transform = transform2D;
		AdobeAnimateExternalVisualHandle result = _externalVisuals.Register(sprite, in descriptor, in transform, in registeredWorldTransform);
		if (result.IsValid)
		{
			InvalidateExternalVisualTopology();
		}
		return result;
	}

	public bool UnregisterExternalVisual(AdobeAnimateExternalVisualHandle handle)
	{
		if (!_externalVisuals.Unregister(handle))
		{
			return false;
		}
		InvalidateExternalVisualTopology();
		return true;
	}

	public bool SetExternalVisualVisible(AdobeAnimateExternalVisualHandle handle, bool visible)
	{
		return ApplyExternalVisualStateChange(() => _externalVisuals.SetVisible(handle, visible));
	}

	public bool SetExternalVisualTexture(AdobeAnimateExternalVisualHandle handle, Texture2D texture)
	{
		return ApplyExternalVisualStateChange(() => _externalVisuals.SetTexture(handle, texture));
	}

	public bool SetExternalVisualTransform(AdobeAnimateExternalVisualHandle handle, Transform2D transform)
	{
		return ApplyExternalVisualStateChange(() => _externalVisuals.SetTransform(handle, in transform));
	}

	public bool SetExternalVisualModulate(AdobeAnimateExternalVisualHandle handle, Color modulate)
	{
		return ApplyExternalVisualStateChange(() => _externalVisuals.SetModulate(handle, in modulate));
	}

	internal ReadOnlySpan<int> GetActiveExternalVisualIndicesForRender()
	{
		return _externalVisuals.GetActiveIndices();
	}

	internal bool HasExternalVisualsForRender()
	{
		return _externalVisuals.ActiveCount > 0;
	}

	internal void GetExternalVisualDiagnosticStats(out int activeVisible, out int nativeFallback)
	{
		activeVisible = 0;
		nativeFallback = 0;
		AccumulateExternalVisualDiagnosticStats(ref activeVisible, ref nativeFallback, 0);
	}

	private void AccumulateExternalVisualDiagnosticStats(ref int activeVisible, ref int nativeFallback, int depth)
	{
		_externalVisuals.GetDiagnosticStats(out var activeVisible2, out var nativeFallback2);
		activeVisible += activeVisible2;
		nativeFallback += nativeFallback2;
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.AccumulateExternalVisualDiagnosticStats(ref activeVisible, ref nativeFallback, depth + 1);
			}
		}
	}

	internal bool HasExternalVisualsInOwnedGraphForRender()
	{
		return HasExternalVisualsInOwnedGraphForRender(0);
	}

	private bool HasExternalVisualsInOwnedGraphForRender(int depth)
	{
		if (_externalVisuals.ActiveCount > 0)
		{
			return true;
		}
		if (depth >= 8)
		{
			return false;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite) && adobeAnimateSprite.HasExternalVisualsInOwnedGraphForRender(depth + 1))
			{
				return true;
			}
		}
		return false;
	}

	internal ulong GetExternalVisualStateVersionForRender()
	{
		return _externalVisuals.StateVersion;
	}

	internal ulong GetExternalVisualTopologyVersionForRender()
	{
		return _externalVisuals.TopologyVersion;
	}

	internal ulong GetCpuVisualSignatureForRender()
	{
		ulong hash = 1469598103934665603uL;
		AccumulateCpuVisualSignatureForRender(ref hash, 0);
		return hash;
	}

	private void AccumulateCpuVisualSignatureForRender(ref ulong hash, int depth)
	{
		AddCpuVisualSignature(ref hash, _externalVisuals.TopologyVersion);
		AddCpuVisualSignature(ref hash, _externalVisuals.StateVersion);
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = GetManagedSlotSpritesForRender();
		AddCpuVisualSignature(ref hash, (ulong)managedSlotSpritesForRender.Length);
		for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
		{
			AdobeAnimateManagedSlotSprite pair = managedSlotSpritesForRender[i];
			AddCpuVisualSignature(ref hash, AdobeAnimateManagedSprite2D.BuildTopologySignature(in pair));
			AddCpuVisualSignature(ref hash, AdobeAnimateManagedSprite2D.BuildGpuStateSignature(in pair));
			CanvasItem visual = pair.Visual;
			Material material = (GodotObject.IsInstanceValid(visual) ? visual.Material : null);
			AddCpuVisualSignature(ref hash, GodotObject.IsInstanceValid(material) ? material.GetInstanceId() : 0);
		}
		ReadOnlySpan<int> activeIndices = _externalVisuals.GetActiveIndices();
		AddCpuVisualSignature(ref hash, (ulong)activeIndices.Length);
		for (int j = 0; j < activeIndices.Length; j++)
		{
			if (_externalVisuals.TryGet(activeIndices[j], out var snapshot))
			{
				AddCpuVisualSignature(ref hash, AdobeAnimateManagedSprite2D.BuildExternalGpuStateSignature(in snapshot, snapshot.Transform));
				Material material2 = (GodotObject.IsInstanceValid(snapshot.Sprite) ? snapshot.Sprite.Material : null);
				AddCpuVisualSignature(ref hash, GodotObject.IsInstanceValid(material2) ? material2.GetInstanceId() : 0);
			}
		}
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		AddCpuVisualSignature(ref hash, (ulong)spriteChildrenForRender.Length);
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.AccumulateCpuVisualSignatureForRender(ref hash, depth + 1);
			}
		}
	}

	private static void AddCpuVisualSignature(ref ulong hash, ulong value)
	{
		hash ^= value;
		hash *= 1099511628211uL;
	}

	internal bool TryGetExternalVisualForRender(int visualIndex, out AdobeAnimateExternalVisualSnapshot visual)
	{
		return _externalVisuals.TryGet(visualIndex, out visual);
	}

	internal bool TryGetExternalVisualForRender(AdobeAnimateExternalVisualHandle handle, out AdobeAnimateExternalVisualSnapshot visual)
	{
		return _externalVisuals.TryGet(handle, out visual);
	}

	internal bool TryBuildExternalVisualGpuStateForRender(int visualIndex, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		if (!_externalVisuals.TryGet(visualIndex, out var snapshot))
		{
			failureReason = $"external visual index {visualIndex} is out of range";
			return false;
		}
		EnsureExternalVisualGpuStateCache(visualIndex + 1);
		ulong stateVersion = _externalVisuals.StateVersion;
		if (!snapshot.Visible && TryGetCachedExternalVisualGpuState(visualIndex, stateVersion, expectedAtlasArrayRid, out state, out failureReason, out var valid))
		{
			_externalVisuals.MarkPrepared(visualIndex, _externalVisualPreparationFrame, valid);
			return valid;
		}
		Transform2D localTransform = ((snapshot.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.World) ? (GetCachedGlobalTransformForRender().AffineInverse() * snapshot.RegisteredWorldTransform) : snapshot.Transform);
		ulong num = AdobeAnimateManagedSprite2D.BuildExternalGpuStateSignature(in snapshot, in localTransform);
		if (_externalVisualGpuStateKnown[visualIndex] && _externalVisualGpuStateSignatures[visualIndex] == num && _externalVisualGpuStateVersions[visualIndex] == stateVersion && _externalVisualGpuStateAtlasRids[visualIndex] == expectedAtlasArrayRid)
		{
			state = _externalVisualGpuStates[visualIndex];
			failureReason = _externalVisualGpuStateFailures[visualIndex] ?? "";
			bool flag = _externalVisualGpuStateValid[visualIndex];
			_externalVisuals.MarkPrepared(visualIndex, _externalVisualPreparationFrame, flag);
			return flag;
		}
		bool flag2 = AdobeAnimateManagedSprite2D.TryBuildExternalGpuState(in snapshot, in localTransform, expectedAtlasArrayRid, out state, out failureReason);
		_externalVisualGpuStateSignatures[visualIndex] = num;
		_externalVisualGpuStateVersions[visualIndex] = stateVersion;
		_externalVisualGpuStateAtlasRids[visualIndex] = expectedAtlasArrayRid;
		_externalVisualGpuStates[visualIndex] = state;
		_externalVisualGpuStateFailures[visualIndex] = failureReason;
		_externalVisualGpuStateKnown[visualIndex] = true;
		_externalVisualGpuStateValid[visualIndex] = flag2;
		_externalVisuals.MarkPrepared(visualIndex, _externalVisualPreparationFrame, flag2);
		return flag2;
	}

	private bool TryGetCachedExternalVisualGpuState(int visualIndex, ulong stateVersion, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason, out bool valid)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		valid = false;
		if (!_externalVisualGpuStateKnown[visualIndex] || _externalVisualGpuStateVersions[visualIndex] != stateVersion || _externalVisualGpuStateAtlasRids[visualIndex] != expectedAtlasArrayRid)
		{
			return false;
		}
		state = _externalVisualGpuStates[visualIndex];
		failureReason = _externalVisualGpuStateFailures[visualIndex] ?? "";
		valid = _externalVisualGpuStateValid[visualIndex];
		return true;
	}

	private void EnsureExternalVisualGpuStateCache(int count)
	{
		if (_externalVisualGpuStates.Length < count)
		{
			int newSize = Math.Max(count, Math.Max(4, _externalVisualGpuStates.Length * 2));
			System.Array.Resize(ref _externalVisualGpuStateSignatures, newSize);
			System.Array.Resize(ref _externalVisualGpuStateVersions, newSize);
			System.Array.Resize(ref _externalVisualGpuStateAtlasRids, newSize);
			System.Array.Resize(ref _externalVisualGpuStates, newSize);
			System.Array.Resize(ref _externalVisualGpuStateFailures, newSize);
			System.Array.Resize(ref _externalVisualGpuStateKnown, newSize);
			System.Array.Resize(ref _externalVisualGpuStateValid, newSize);
		}
	}

	private void ResetExternalVisualGpuStateCache()
	{
		_externalVisualGpuStateSignatures = System.Array.Empty<ulong>();
		_externalVisualGpuStateVersions = System.Array.Empty<ulong>();
		_externalVisualGpuStateAtlasRids = System.Array.Empty<Rid>();
		_externalVisualGpuStates = System.Array.Empty<AdobeAnimateGpuManagedVisualState>();
		_externalVisualGpuStateFailures = System.Array.Empty<string>();
		_externalVisualGpuStateKnown = System.Array.Empty<bool>();
		_externalVisualGpuStateValid = System.Array.Empty<bool>();
	}

	internal void BeginExternalVisualPreparationForRender(long frameVersion)
	{
		_externalVisualPreparationFrame = frameVersion;
	}

	internal void MarkExternalVisualPreparedForRender(int visualIndex, long frameVersion, bool preparedForCrowd)
	{
		_externalVisualPreparationFrame = frameVersion;
		_externalVisuals.MarkPrepared(visualIndex, frameVersion, preparedForCrowd);
	}

	internal void CommitExternalVisualCrowdFrame(long frameVersion)
	{
		CommitExternalVisualCrowdFrame(frameVersion, 0);
	}

	internal void CommitExternalVisualCpuFrame(long frameVersion, ReadOnlySpan<AdobeAnimateCpuNativeSpriteItem> nativeItems)
	{
		CommitExternalVisualCpuFrame(frameVersion, nativeItems, 0);
	}

	internal void HideExternalVisualCpuFrame(long frameVersion)
	{
		HideExternalVisualCpuFrame(frameVersion, 0);
	}

	internal void RestoreExternalVisualNativeFallbacks(long frameVersion)
	{
		RestoreExternalVisualNativeFallbacks(frameVersion, 0);
	}

	private void CommitExternalVisualCrowdFrame(long frameVersion, int depth)
	{
		_externalVisuals.CommitCrowdFrame(frameVersion);
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.CommitExternalVisualCrowdFrame(frameVersion, depth + 1);
			}
		}
	}

	private void CommitExternalVisualCpuFrame(long frameVersion, ReadOnlySpan<AdobeAnimateCpuNativeSpriteItem> nativeItems, int depth)
	{
		_externalVisuals.CommitCpuFrame(frameVersion, nativeItems);
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.CommitExternalVisualCpuFrame(frameVersion, nativeItems, depth + 1);
			}
		}
	}

	private void HideExternalVisualCpuFrame(long frameVersion, int depth)
	{
		_externalVisuals.HideCpuFrame(frameVersion);
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.HideExternalVisualCpuFrame(frameVersion, depth + 1);
			}
		}
	}

	private void RestoreExternalVisualNativeFallbacks(long frameVersion, int depth)
	{
		_externalVisuals.RestoreNativeFallbacks();
		if (depth >= 8)
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.RestoreExternalVisualNativeFallbacks(frameVersion, depth + 1);
			}
		}
	}

	private bool ApplyExternalVisualStateChange(Func<bool> change)
	{
		ulong stateVersion = _externalVisuals.StateVersion;
		if (!change())
		{
			return false;
		}
		if (_externalVisuals.StateVersion != stateVersion)
		{
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
		}
		return true;
	}

	private void InvalidateExternalVisualTopology()
	{
		InvalidateRuntimeNativeCanvasSuppressionEligibility();
		ResetExternalVisualGpuStateCache();
		AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.ExternalVisualTopology);
		InvalidateRenderSnapshotCache(force: true);
		RequestNodeRedraw();
	}

	private bool TryBuildGpuGraphCrowdRenderState(AdobeAnimateRuntimeDefinition definition, Node renderMountParent, Transform2D globalTransform, bool enableGpuClock, float animationClockSeconds, AdobeAnimateCrowdRenderState reusableState, out AdobeAnimateCrowdRenderState state)
	{
		state = null;
		long startBytes = TowerDefenseAllocationTelemetry.Begin();
		TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
		if (!AdobeAnimateRenderManager.TryResolveGpuRenderGraph(this, out var graph, out var graphOwners, out var allocation, out var textureArray, out var textureSize))
		{
			TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuildResolve, startBytes);
			TowerDefensePerfProfiler.EndSpikeProbe("adobeAnimate.render.gpuGraph.resolveFailed", in probe);
			return false;
		}
		TowerDefensePerfProfiler.EndSpikeProbe("adobeAnimate.render.gpuGraph.resolve", in probe, (graphOwners != null) ? graphOwners.Length : 0);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuildResolve, startBytes);
		if (graph == null || graphOwners == null || graphOwners.Length != graph.Owners.Length || graphOwners.Length != allocation.OwnerCount)
		{
			return false;
		}
		long startBytes2 = TowerDefenseAllocationTelemetry.Begin();
		TowerDefensePerfProfiler.SpikeProbe probe2 = TowerDefensePerfProfiler.BeginSpikeProbe();
		float renderFrameFloat = GetRenderFrameFloat();
		int num = ResolveCrowdFrameIndex(definition, renderFrameFloat);
		Transform2D globalTransform2 = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, globalTransform);
		Color modulate = GetEffectiveRenderModulate(renderMountParent);
		int effectiveZIndexForRender = GetEffectiveZIndexForRender();
		TowerDefensePerfProfiler.EndSpikeProbe("adobeAnimate.render.gpuGraph.presentation", in probe2, graphOwners.Length);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuildPresentation, startBytes2);
		long startBytes3 = TowerDefenseAllocationTelemetry.Begin();
		TowerDefensePerfProfiler.SpikeProbe probe3 = TowerDefensePerfProfiler.BeginSpikeProbe();
		AdobeAnimateGpuGraphOwnerState gpuGraphRootOwnerState = (_cachedGpuGraphRootOwnerState = BuildGpuGraphOwnerStateForRender(definition, globalTransform2, modulate, renderFrameFloat, enableGpuClock, animationClockSeconds, _cachedGpuGraphRootOwnerState));
		TowerDefensePerfProfiler.EndSpikeProbe("adobeAnimate.render.gpuGraph.rootState", in probe3, graphOwners.Length);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuildRootState, startBytes3);
		long startBytes4 = TowerDefenseAllocationTelemetry.Begin();
		TowerDefensePerfProfiler.SpikeProbe probe4 = TowerDefensePerfProfiler.BeginSpikeProbe();
		state = ((reusableState != null) ? reusableState.ResetGpuGraph(definition, definition.AtlasTextureArray, (definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f) ? definition.AtlasTextureArraySize : Vector2.One, definition.GpuPoseTextureArray, definition.GpuPoseTextureSize, in allocation, textureArray, in textureSize, renderMountParent, in globalTransform2, in modulate, offset, in _verticalClip, num, ResolveCrowdFrameInterpolation(renderFrameFloat), _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _layerVisible, effectiveZIndexForRender, graphOwners, gpuGraphRootOwnerState, BuildRootMotionStateForRender(renderMountParent, globalTransform2)) : AdobeAnimateCrowdRenderState.CreateGpuGraph(definition, definition.AtlasTextureArray, (definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f) ? definition.AtlasTextureArraySize : Vector2.One, definition.GpuPoseTextureArray, definition.GpuPoseTextureSize, allocation, textureArray, textureSize, renderMountParent, globalTransform2, modulate, offset, _verticalClip, num, ResolveCrowdFrameInterpolation(renderFrameFloat), _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _layerVisible, effectiveZIndexForRender, graphOwners, gpuGraphRootOwnerState, BuildRootMotionStateForRender(renderMountParent, globalTransform2)));
		TowerDefensePerfProfiler.EndSpikeProbe("adobeAnimate.render.gpuGraph.createState", in probe4, graphOwners.Length);
		TowerDefenseAllocationTelemetry.End(TowerDefenseAllocationMetric.RenderCollectStateGpuBuildCreateState, startBytes4);
		return state.Mode == AdobeAnimateCrowdRenderMode.GpuGraph;
	}

	internal bool TryGetGpuGraphOwnerDefinitionForRender(out AdobeAnimateRuntimeDefinition definition)
	{
		string failureReason;
		return TryGetGpuGraphOwnerDefinitionForRender(out definition, out failureReason);
	}

	internal bool TryGetGpuGraphOwnerDefinitionForRender(out AdobeAnimateRuntimeDefinition definition, out string failureReason)
	{
		definition = null;
		failureReason = "";
		if (_effectOnceGpuSuppressed)
		{
			failureReason = "owner is internally suppressed by the effect-once GPU batch";
			return false;
		}
		if (!GodotObject.IsInstanceValid(this))
		{
			failureReason = "owner instance is invalid";
			return false;
		}
		if (_flashAnimeData == null)
		{
			failureReason = "owner has no AdobeAnimateData";
			return false;
		}
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		if (RefreshRenderStaticStateCache())
		{
			_runtimeGpuPrepareRequired = true;
		}
		definition = GetRuntimeDefinition();
		if (!CanUseCrowdShaderPoseDefinition(definition))
		{
			failureReason = "runtime definition has no usable GPU pose texture";
			return false;
		}
		if (!_cachedAllLayersVisible && !_cachedCanUseLayerMask && _cachedLayerVisibleCount <= 0)
		{
			failureReason = "layer visibility cannot be represented by the GPU mask";
			return false;
		}
		if (!CanUseGpuGraphMediaReplace(definition))
		{
			failureReason = "media replacement is not backed by the shared texture array";
			return false;
		}
		return true;
	}

	internal bool TryBuildGpuGraphOwnerStateForRender(Node renderMountParent, bool enableGpuClock, out AdobeAnimateGpuGraphOwnerState state)
	{
		return TryBuildGpuGraphOwnerStateForRender(renderMountParent, enableGpuClock, (float)AdobeAnimateRuntimeManager.AnimationClockSeconds, out state);
	}

	internal bool TryBuildGpuGraphOwnerStateForRender(Node renderMountParent, bool enableGpuClock, float animationClockSeconds, out AdobeAnimateGpuGraphOwnerState state)
	{
		return TryBuildGpuGraphOwnerStateForRender(renderMountParent, enableGpuClock, animationClockSeconds, GetEffectiveRenderModulate(renderMountParent), out state);
	}

	internal bool TryBuildGpuGraphOwnerStateForRender(Node renderMountParent, bool enableGpuClock, float animationClockSeconds, in Color renderModulate, out AdobeAnimateGpuGraphOwnerState state)
	{
		state = null;
		if (!TryGetGpuGraphOwnerDefinitionForRender(out var definition))
		{
			return false;
		}
		float renderFrameFloat = GetRenderFrameFloat();
		Transform2D renderTransform = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, GetCachedGlobalTransformForRender());
		state = BuildGpuGraphOwnerStateForRender(definition, renderTransform, renderModulate, renderFrameFloat, enableGpuClock, animationClockSeconds, _cachedGpuGraphRootOwnerState);
		_cachedGpuGraphRootOwnerState = state;
		return true;
	}

	internal bool TryBuildGpuGraphNestedOwnerStateForRender(Node renderMountParent, bool enableGpuClock, float animationClockSeconds, out AdobeAnimateGpuGraphOwnerState state)
	{
		return TryBuildGpuGraphNestedOwnerStateForRender(renderMountParent, enableGpuClock, animationClockSeconds, GetEffectiveRenderModulate(renderMountParent), out state);
	}

	internal bool TryReusePreparedGpuGraphNestedOwnerStateForRender(AdobeAnimateGpuGraphOwnerState preparedState, float animationClockSeconds, in Color renderModulate, out AdobeAnimateGpuGraphOwnerState state)
	{
		state = null;
		if (!_hasCachedGpuGraphNestedOwnerState || preparedState != _cachedGpuGraphNestedOwnerState)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderEncodeGpuRefreshNestedRejectCache);
			return false;
		}
		bool flag = _renderStaticStateNextAuditPhysicsFrame != 18446744073709551615uL && AdobeAnimateRenderManager.GetPhysicsFrameForRender() >= _renderStaticStateNextAuditPhysicsFrame;
		if (_gpuGraphCrowdStaticStateDirty || animationClockSeconds >= _gpuGraphNestedOwnerStateRebaseClockSeconds || flag)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderEncodeGpuRefreshNestedRejectSchedule);
			return false;
		}
		if (!_runtimeGpuClockInterpolationActive || _effectOnceGpuSuppressed || needMediaReplaceUpdate || !_runtimeInsideTree || _flashAnimeData == null || !_renderStaticStateCached)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderEncodeGpuRefreshNestedRejectRuntime);
			return false;
		}
		if (_cachedLayerStateVersion != _layerStateVersion || _cachedMediaReplaceStateVersion != _mediaReplaceStateVersion || _cachedGpuGraphNestedOwnerPlaybackRevision != _playbackRevision)
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderEncodeGpuRefreshNestedRejectVersion);
			return false;
		}
		if (preparedState.Visible != _isVisibleInTree || preparedState.Modulate != renderModulate || !SameGpuHitFlashState(preparedState.GpuBrightFlash, _gpuBrightFlash) || !SameGpuHitFlashState(preparedState.GpuWhiteFlash, _gpuWhiteFlash) || IsClipBlendActiveForRender())
		{
			TowerDefenseAllocationTelemetry.Sample(TowerDefenseAllocationMetric.RenderEncodeGpuRefreshNestedRejectPresentation);
			return false;
		}
		state = preparedState;
		return true;
	}

	internal bool TryBuildGpuGraphNestedOwnerStateForRender(Node renderMountParent, bool enableGpuClock, float animationClockSeconds, in Color renderModulate, out AdobeAnimateGpuGraphOwnerState state)
	{
		state = null;
		if (TryReuseGpuGraphNestedOwnerStateForRender(enableGpuClock, animationClockSeconds, in renderModulate, out state))
		{
			_gpuGraphCrowdStaticStateDirty = false;
			return true;
		}
		if (!TryGetGpuGraphOwnerDefinitionForRender(out var definition))
		{
			return false;
		}
		state = BuildGpuGraphOwnerStateForRender(definition, Transform2D.Identity, renderModulate, GetRenderFrameFloat(), enableGpuClock, animationClockSeconds, _cachedGpuGraphNestedOwnerState);
		CacheGpuGraphNestedOwnerStateForRender(state, animationClockSeconds);
		return true;
	}

	private bool TryReuseGpuGraphNestedOwnerStateForRender(bool enableGpuClock, float animationClockSeconds, in Color renderModulate, out AdobeAnimateGpuGraphOwnerState state)
	{
		state = null;
		if (!_hasCachedGpuGraphNestedOwnerState || !enableGpuClock || !_runtimeGpuClockInterpolationActive || _effectOnceGpuSuppressed || needMediaReplaceUpdate || !_runtimeInsideTree || _flashAnimeData == null || !_renderStaticStateCached || _cachedLayerStateVersion != _layerStateVersion || _cachedMediaReplaceStateVersion != _mediaReplaceStateVersion || _cachedGpuGraphNestedOwnerPlaybackRevision != _playbackRevision || IsClipBlendActiveForRender())
		{
			return false;
		}
		if (_renderStaticStateNextAuditPhysicsFrame != 18446744073709551615uL && AdobeAnimateRenderManager.GetPhysicsFrameForRender() >= _renderStaticStateNextAuditPhysicsFrame && RefreshRenderStaticStateCache())
		{
			return false;
		}
		AdobeAnimateGpuGraphOwnerState cachedGpuGraphNestedOwnerState = _cachedGpuGraphNestedOwnerState;
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = cachedGpuGraphNestedOwnerState?.Definition;
		if (cachedGpuGraphNestedOwnerState == null || adobeAnimateRuntimeDefinition == null || !CanUseCrowdShaderPoseDefinition(adobeAnimateRuntimeDefinition) || _runtimeDefinition != adobeAnimateRuntimeDefinition || cachedGpuGraphNestedOwnerState.StaticState != _cachedGpuGraphOwnerStateTemplate || !IsGpuGraphOwnerStateTemplateCurrentForRender(cachedGpuGraphNestedOwnerState.StaticState, adobeAnimateRuntimeDefinition) || cachedGpuGraphNestedOwnerState.Visible != _isVisibleInTree || !SameGpuHitFlashState(cachedGpuGraphNestedOwnerState.GpuBrightFlash, _gpuBrightFlash) || !SameGpuHitFlashState(cachedGpuGraphNestedOwnerState.GpuWhiteFlash, _gpuWhiteFlash) || !CanUseGpuGraphMediaReplace(adobeAnimateRuntimeDefinition))
		{
			return false;
		}
		float renderFrameFloat = GetRenderFrameFloat();
		AdobeAnimateGpuClockState gpuClock = BuildRuntimeGpuClockState(renderFrameFloat, animationClockSeconds);
		if (!SameGpuClockConfiguration(cachedGpuGraphNestedOwnerState.GpuClock, gpuClock) || !GpuClockPhaseMatches(cachedGpuGraphNestedOwnerState.GpuClock, animationClockSeconds, renderFrameFloat))
		{
			return false;
		}
		if (animationClockSeconds >= _gpuGraphNestedOwnerStateRebaseClockSeconds)
		{
			state = cachedGpuGraphNestedOwnerState.ResetDynamicState(cachedGpuGraphNestedOwnerState.StaticState, cachedGpuGraphNestedOwnerState.GlobalTransform, in renderModulate, cachedGpuGraphNestedOwnerState.FrameIndex, cachedGpuGraphNestedOwnerState.InterpolationT, _isVisibleInTree, cachedGpuGraphNestedOwnerState.ClipBlend, in gpuClock, in _gpuBrightFlash, in _gpuWhiteFlash);
			CacheGpuGraphNestedOwnerStateForRender(state, animationClockSeconds);
			return true;
		}
		state = ((cachedGpuGraphNestedOwnerState.Modulate == renderModulate) ? cachedGpuGraphNestedOwnerState : cachedGpuGraphNestedOwnerState.WithModulate(renderModulate));
		_cachedGpuGraphNestedOwnerState = state;
		return true;
	}

	private void CacheGpuGraphNestedOwnerStateForRender(AdobeAnimateGpuGraphOwnerState state, float animationClockSeconds)
	{
		if (state == null)
		{
			_cachedGpuGraphNestedOwnerState = null;
			_hasCachedGpuGraphNestedOwnerState = false;
			return;
		}
		_cachedGpuGraphNestedOwnerState = state;
		if (!state.GpuClock.Enabled || state.ClipBlend.Enabled)
		{
			_hasCachedGpuGraphNestedOwnerState = false;
			return;
		}
		_cachedGpuGraphNestedOwnerPlaybackRevision = _playbackRevision;
		_gpuGraphCrowdStaticStateDirty = false;
		_gpuGraphNestedOwnerStateRebaseClockSeconds = animationClockSeconds + 1f + (float)(GetCachedInstanceIdForRender() % 60) / 60f;
		_hasCachedGpuGraphNestedOwnerState = true;
	}

	private static bool SameGpuClockConfiguration(AdobeAnimateGpuClockState cached, AdobeAnimateGpuClockState current)
	{
		if (cached.Enabled && current.Enabled && Mathf.IsEqualApprox(cached.FramesPerSecond, current.FramesPerSecond) && Mathf.IsEqualApprox(cached.ClipStart, current.ClipStart) && Mathf.IsEqualApprox(cached.ClipEndExclusive, current.ClipEndExclusive))
		{
			return cached.Loop == current.Loop;
		}
		return false;
	}

	private static bool GpuClockPhaseMatches(AdobeAnimateGpuClockState cached, float animationClockSeconds, float currentFrameFloat)
	{
		float num = Math.Max(1f, cached.ClipEndExclusive - cached.ClipStart);
		float num2 = cached.StartFrame + (animationClockSeconds - cached.StartTime) * cached.FramesPerSecond;
		if (cached.Loop)
		{
			num2 = cached.ClipStart + PositiveModulo(num2 - cached.ClipStart, num);
			currentFrameFloat = cached.ClipStart + PositiveModulo(currentFrameFloat - cached.ClipStart, num);
			float num3 = Math.Abs(num2 - currentFrameFloat);
			num3 = Math.Min(num3, num - num3);
			return num3 <= 0.001f;
		}
		num2 = Mathf.Clamp(num2, cached.ClipStart, Math.Max(cached.ClipStart, cached.ClipEndExclusive - 1f));
		return Math.Abs(num2 - currentFrameFloat) <= 0.001f;
	}

	private static float PositiveModulo(float value, float modulus)
	{
		float num = value % modulus;
		if (!(num < 0f))
		{
			return num;
		}
		return num + modulus;
	}

	private static bool SameGpuHitFlashState(AdobeAnimateGpuHitFlashState left, AdobeAnimateGpuHitFlashState right)
	{
		if (left.Enabled == right.Enabled && Mathf.IsEqualApprox(left.StartTime, right.StartTime) && Mathf.IsEqualApprox(left.Strength, right.Strength))
		{
			return Mathf.IsEqualApprox(left.Duration, right.Duration);
		}
		return false;
	}

	internal bool TryBuildGpuGraphOwnerStateForRender(Node renderMountParent, out AdobeAnimateGpuGraphOwnerState state)
	{
		return TryBuildGpuGraphOwnerStateForRender(renderMountParent, enableGpuClock: true, out state);
	}

	private AdobeAnimateGpuGraphOwnerState BuildGpuGraphOwnerStateForRender(AdobeAnimateRuntimeDefinition definition, Transform2D renderTransform, Color renderModulate, float frameFloat, bool enableGpuClock, float animationClockSeconds, AdobeAnimateGpuGraphOwnerState reusableState = null)
	{
		AdobeAnimateGpuClockState gpuClock = (enableGpuClock ? BuildRuntimeGpuClockState(frameFloat, animationClockSeconds) : default(AdobeAnimateGpuClockState));
		CacheRuntimeGpuClockConfiguration(gpuClock);
		AdobeAnimateGpuGraphOwnerStateTemplate orCreateGpuGraphOwnerStateTemplateForRender = GetOrCreateGpuGraphOwnerStateTemplateForRender(definition);
		int num = 0;
		float interpolationT = 0f;
		if (!gpuClock.Enabled)
		{
			num = ResolveCrowdFrameIndex(definition, frameFloat);
			interpolationT = ResolveCrowdFrameInterpolation(frameFloat);
		}
		AdobeAnimateClipBlendState clipBlend = GetClipBlendStateForRender();
		return reusableState?.ResetDynamicState(orCreateGpuGraphOwnerStateTemplateForRender, in renderTransform, in renderModulate, num, interpolationT, _isVisibleInTree, in clipBlend, in gpuClock, in _gpuBrightFlash, in _gpuWhiteFlash) ?? new AdobeAnimateGpuGraphOwnerState(orCreateGpuGraphOwnerStateTemplateForRender, renderTransform, renderModulate, num, interpolationT, _isVisibleInTree, clipBlend, gpuClock, _gpuBrightFlash, _gpuWhiteFlash);
	}

	private AdobeAnimateGpuGraphOwnerStateTemplate GetOrCreateGpuGraphOwnerStateTemplateForRender(AdobeAnimateRuntimeDefinition definition)
	{
		AdobeAnimateGpuGraphOwnerStateTemplate cachedGpuGraphOwnerStateTemplate = _cachedGpuGraphOwnerStateTemplate;
		if (IsGpuGraphOwnerStateTemplateCurrentForRender(cachedGpuGraphOwnerStateTemplate, definition))
		{
			return cachedGpuGraphOwnerStateTemplate;
		}
		int layerCount = Math.Max(_cachedLayerVisibleCount, definition?.RuntimeLayerCount ?? 0);
		int mediaCount = Math.Max(_cachedMediaReplaceLimit, (definition?.MediaRects?.Length).GetValueOrDefault());
		return _cachedGpuGraphOwnerStateTemplate = new AdobeAnimateGpuGraphOwnerStateTemplate(this, definition, offset, _verticalClip, _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _layerVisible, layerCount, _cachedHasMediaReplace, mediaReplaceRect, _mediaReplaceUse, mediaReplaceAtlasPages, mediaReplaceAtlasArraySize, mediaCount, _cachedMediaReplaceSignature);
	}

	private bool IsGpuGraphOwnerStateTemplateCurrentForRender(AdobeAnimateGpuGraphOwnerStateTemplate cached, AdobeAnimateRuntimeDefinition definition)
	{
		int num = Math.Max(_cachedLayerVisibleCount, definition?.RuntimeLayerCount ?? 0);
		int num2 = Math.Max(_cachedMediaReplaceLimit, (definition?.MediaRects?.Length).GetValueOrDefault());
		if (cached != null && cached.SourceSprite == this && cached.Definition == definition && cached.Offset == offset && SameVerticalClip(cached.VerticalClip, _verticalClip) && cached.AllLayersVisible == _cachedAllLayersVisible && cached.CanUseLayerMask == _cachedCanUseLayerMask && cached.LayerMask == _cachedLayerMask && cached.LayerVisible == _layerVisible && cached.LayerCount == num && cached.HasMediaReplace == _cachedHasMediaReplace && cached.MediaReplaceRect == mediaReplaceRect && cached.MediaReplaceUse == _mediaReplaceUse && cached.MediaReplaceAtlasPages == mediaReplaceAtlasPages && cached.MediaReplaceAtlasSize == mediaReplaceAtlasArraySize && cached.MediaCount == num2)
		{
			return cached.MediaReplaceSignature == _cachedMediaReplaceSignature;
		}
		return false;
	}

	private static bool SameVerticalClip(VerticalClipState left, VerticalClipState right)
	{
		if (left.Enabled == right.Enabled && Mathf.IsEqualApprox(left.UpY, right.UpY))
		{
			return Mathf.IsEqualApprox(left.DownY, right.DownY);
		}
		return false;
	}

	private void CacheRuntimeGpuClockConfiguration(AdobeAnimateGpuClockState gpuClock)
	{
		bool num = _runtimeGpuClockInterpolationActive != gpuClock.Enabled || !Mathf.IsEqualApprox(_runtimeGpuClockFramesPerSecond, gpuClock.FramesPerSecond) || !Mathf.IsEqualApprox(_runtimeGpuClockClipStart, gpuClock.ClipStart) || !Mathf.IsEqualApprox(_runtimeGpuClockClipEndExclusive, gpuClock.ClipEndExclusive) || _runtimeGpuClockLoop != gpuClock.Loop;
		_runtimeGpuClockInterpolationActive = gpuClock.Enabled;
		_runtimeGpuClockFramesPerSecond = gpuClock.FramesPerSecond;
		_runtimeGpuClockClipStart = gpuClock.ClipStart;
		_runtimeGpuClockClipEndExclusive = gpuClock.ClipEndExclusive;
		_runtimeGpuClockLoop = gpuClock.Loop;
		if (num)
		{
			InvalidateRuntimeDisplayVisualRequirementCache();
		}
	}

	private bool CanReuseRuntimeGpuClockPose()
	{
		if (_runtimeGpuClockPoseReuseCached && _runtimeGpuClockPoseReuseValidationCountdown > 0)
		{
			_runtimeGpuClockPoseReuseValidationCountdown--;
			return _cachedRuntimeGpuClockPoseReusable;
		}
		bool flag = ComputeCanReuseRuntimeGpuClockPose();
		_runtimeGpuClockPoseReuseCached = true;
		_cachedRuntimeGpuClockPoseReusable = flag;
		_runtimeGpuClockPoseReuseValidationCountdown = 8;
		return flag;
	}

	private bool ComputeCanReuseRuntimeGpuClockPose()
	{
		if (!_runtimeGpuClockInterpolationActive)
		{
			return false;
		}
		int x = clipRange.X;
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		double num = frameRate * Math.Abs(timeScale) * (playBack ? (-1.0) : 1.0);
		if (_usingRuntimeManager && !IsPlaybackStopped && _runtimeManagerDispatchActive && !clipOver && _nextAnimDelayTimer <= 0.0 && playbackClipEndExclusive - x > 1 && Math.Abs(num) > 1E-06 && Mathf.IsEqualApprox((float)num, _runtimeGpuClockFramesPerSecond) && Mathf.IsEqualApprox(x, _runtimeGpuClockClipStart) && Mathf.IsEqualApprox(playbackClipEndExclusive, _runtimeGpuClockClipEndExclusive))
		{
			return loop == _runtimeGpuClockLoop;
		}
		return false;
	}

	internal bool RequiresDisplayFrameVisualTick()
	{
		if (_effectOnceGpuSuppressed)
		{
			return false;
		}
		if (_runtimeDisplayVisualRequirementCached && _runtimeDisplayVisualRequirementValidationCountdown > 0)
		{
			_runtimeDisplayVisualRequirementValidationCountdown--;
			_displayFrameGpuClockPoseReusable = _cachedRuntimeDisplayGpuClockPoseReusable;
			return _cachedRuntimeDisplayVisualRequirement;
		}
		bool flag = ComputeRequiresDisplayFrameVisualTick();
		_runtimeDisplayVisualRequirementCached = true;
		_cachedRuntimeDisplayVisualRequirement = flag;
		_cachedRuntimeDisplayGpuClockPoseReusable = _displayFrameGpuClockPoseReusable;
		_runtimeDisplayVisualRequirementValidationCountdown = 30;
		return flag;
	}

	private bool ComputeRequiresDisplayFrameVisualTick()
	{
		_displayFrameGpuClockPoseReusable = false;
		if (CanReuseCachedCulledPose())
		{
			return false;
		}
		if (RequiresDisplayFrameBatchedProcessExtension)
		{
			_displayFrameGpuClockPoseReusable = CanReuseRuntimeGpuClockPose();
			return true;
		}
		if (IsClipBlendActiveForRender())
		{
			return true;
		}
		_displayFrameGpuClockPoseReusable = CanReuseRuntimeGpuClockPose();
		if (!_displayFrameGpuClockPoseReusable)
		{
			return true;
		}
		return RequiresDisplayCadenceRuntimeChildUpdate();
	}

	private void InvalidateRuntimeDisplayVisualRequirementCache()
	{
		_runtimeDisplayVisualRequirementCached = false;
		_runtimeDisplayVisualRequirementValidationCountdown = 0;
		_runtimeGpuClockPoseReuseCached = false;
		_runtimeGpuClockPoseReuseValidationCountdown = 0;
	}

	private bool RequiresDisplayCadenceRuntimeChildUpdate()
	{
		if (!HasRuntimeChildUpdates() || ShouldSkipRuntimeChildUpdateForViewport())
		{
			return false;
		}
		return Mathf.Max(10.0, trueFrameRate) > 30.0;
	}

	private void RequestAnimationPoseRedraw()
	{
		if ((IsClipBlendActiveForRender() || !CanReuseRuntimeGpuClockPose()) && !CanReuseCachedCulledPose())
		{
			RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		}
	}

	private bool CanReuseCachedCulledPose()
	{
		AdobeAnimateSprite adobeAnimateSprite = ((_parentSprite == null) ? this : GetRenderSortRootForRender());
		if (adobeAnimateSprite == null || !adobeAnimateSprite.IsRuntimeInsideTree)
		{
			return false;
		}
		if (adobeAnimateSprite._usingRuntimeManager && adobeAnimateSprite._flashAnimeData != null && adobeAnimateSprite._isVisibleInTree && adobeAnimateSprite._runtimeRenderBoundsCached && !adobeAnimateSprite._cachedRuntimeRenderBoundsVisible && !adobeAnimateSprite._renderGlobalTransformDirty && adobeAnimateSprite._renderGlobalTransformCached && adobeAnimateSprite._cachedRuntimeRenderBoundsTransform == adobeAnimateSprite._renderGlobalTransform && adobeAnimateSprite._cachedRuntimeRenderBoundsOffset == adobeAnimateSprite.offset && adobeAnimateSprite._cachedRuntimeRenderBoundsDefinition == adobeAnimateSprite._runtimeDefinition)
		{
			return adobeAnimateSprite._cachedRuntimeRenderBoundsMountParent == adobeAnimateSprite._cachedRenderMountParent;
		}
		return false;
	}

	internal void InvalidateRuntimeCrowdCullingForRefresh()
	{
		_runtimeRenderBoundsRescanCountdown = 0;
		InvalidateRuntimeDisplayVisualRequirementCache();
	}

	internal bool InvalidateRuntimeCrowdCullingIfCachedOffscreen()
	{
		if (!_runtimeRenderBoundsCached || _cachedRuntimeRenderBoundsVisible)
		{
			return false;
		}
		if (!HasRuntimeCrowdViewportChanged())
		{
			return false;
		}
		_runtimeRenderBoundsRescanCountdown = 0;
		InvalidateRuntimeDisplayVisualRequirementCache();
		return true;
	}

	private bool HasRuntimeCrowdViewportChanged()
	{
		if (!_runtimeViewportCullingEnabled)
		{
			return false;
		}
		Viewport viewport = ResolveRenderViewport(_cachedRuntimeRenderBoundsMountParent);
		if (!GodotObject.IsInstanceValid(viewport) || !GodotObject.IsInstanceValid(_cachedRuntimeRenderBoundsViewport) || viewport != _cachedRuntimeRenderBoundsViewport)
		{
			return true;
		}
		return GetCachedViewportWorldRect(viewport) != _cachedRuntimeRenderBoundsViewportWorldRect;
	}

	internal void DisableRuntimeGpuClockInterpolationForOwnedGraph()
	{
		if (_runtimeGpuClockInterpolationActive)
		{
			InvalidateRuntimeDisplayVisualRequirementCache();
		}
		_runtimeGpuClockInterpolationActive = false;
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spriteChildren[i];
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.DisableRuntimeGpuClockInterpolationForOwnedGraph();
			}
		}
	}

	internal void InvalidateRuntimeRenderAfterGpuCacheReset()
	{
		InvalidateStaticAtlasPathLayoutForNodeMutation(markRuntimeMutation: false);
		_staticAtlasPathLayoutGpuResetPending = true;
		needMediaReplaceUpdate = true;
		_displayFrameGpuClockPoseReusable = false;
		DisableRuntimeGpuClockInterpolationForOwnedGraph();
		InvalidateRuntimeCrowdCullingForRefresh();
		InvalidateRenderSnapshotCache(force: true);
		_needsRenderSubmission = true;
	}

	private AdobeAnimateGpuClockState BuildRuntimeGpuClockState(float frameFloat, float animationClockSeconds)
	{
		int x = clipRange.X;
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		double num = frameRate * Math.Abs(timeScale) * (playBack ? (-1.0) : 1.0);
		return new AdobeAnimateGpuClockState(_usingRuntimeManager && !IsPlaybackStopped && _runtimeManagerDispatchActive && !clipOver && _nextAnimDelayTimer <= 0.0 && playbackClipEndExclusive - x > 1 && Math.Abs(num) > 1E-06, animationClockSeconds, frameFloat, (float)num, x, playbackClipEndExclusive, loop);
	}

	private bool CanUseGpuGraphMediaReplace(AdobeAnimateRuntimeDefinition definition)
	{
		if (!_cachedAnyRequestedMediaReplace)
		{
			return !_cachedHasMediaReplace;
		}
		if (!_cachedHasMediaReplace || !mediaReplaceAtlasShared || !mediaReplaceAtlasUsesTextureArray || !GodotObject.IsInstanceValid(mediaReplaceAtlasArray) || definition == null || !definition.AtlasTextureArrayRid.IsValid)
		{
			return false;
		}
		if (_cachedMediaReplaceAtlasArrayRid == definition.AtlasTextureArrayRid && mediaReplaceAtlasArraySize.X > 0f)
		{
			return mediaReplaceAtlasArraySize.Y > 0f;
		}
		return false;
	}

	private bool HasAnyRequestedMediaReplace()
	{
		Array<bool> array = _mediaReplaceUse;
		int num = array?.Count ?? 0;
		if (num <= 0)
		{
			return false;
		}
		for (int i = 0; i < num; i++)
		{
			if (array[i])
			{
				return true;
			}
		}
		return false;
	}

	internal bool TryGetGpuGraphChildAttachmentForRender(AdobeAnimateSprite child, out AdobeAnimateGpuAttachmentKind attachmentKind, out int attachmentKey)
	{
		attachmentKind = AdobeAnimateGpuAttachmentKind.Root;
		attachmentKey = -1;
		if (!TryGetGpuGraphChildAttachmentForRender(child, out var attachment))
		{
			return false;
		}
		attachmentKind = attachment.Kind;
		attachmentKey = attachment.Key;
		return true;
	}

	internal bool TryGetGpuGraphChildAttachmentForRender(AdobeAnimateSprite child, out AdobeAnimateGpuAttachmentSettings attachment)
	{
		attachment = default(AdobeAnimateGpuAttachmentSettings);
		if (!GodotObject.IsInstanceValid(child))
		{
			return false;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		for (int i = 0; i < spriteChildrenForRender.Length; i++)
		{
			if (spriteChildrenForRender[i] != child)
			{
				continue;
			}
			int num = ResolveSpriteChildFollowLayer(child, i);
			if (num < 0)
			{
				return false;
			}
			if (IsSpriteChildOwnedBySlot(i))
			{
				AdobeAnimateSlot adobeAnimateSlot = _spriteChildOwnerSlots[i];
				if (!GodotObject.IsInstanceValid(adobeAnimateSlot) || adobeAnimateSlot.mode != 0)
				{
					return false;
				}
				attachment = new AdobeAnimateGpuAttachmentSettings(AdobeAnimateGpuAttachmentKind.Slot, num, -1, usePos: true, adobeAnimateSlot.useRotate, adobeAnimateSlot.useScale, adobeAnimateSlot.useSkew, offset, adobeAnimateSlot.offset, 0f, adobeAnimateSlot.useFollowVisible, child.Transform);
				return true;
			}
			Transform2D localTransform = new Transform2D(child.useRotate ? 0f : child.Rotation, child.Scale, child.Skew, child.usePos ? Vector2.Zero : child.Position);
			attachment = new AdobeAnimateGpuAttachmentSettings(AdobeAnimateGpuAttachmentKind.FollowLayer, num, -1, child.usePos, child.useRotate, useScale: false, useSkew: false, offset, Vector2.Zero, (float)child.offsetRotate, child.useFollowVisible, localTransform);
			return true;
		}
		return false;
	}

	public void UpdateChild()
	{
		if (!_hasChildren || _flashAnimeData == null)
		{
			return;
		}
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null || runtimeDefinition.Frames == null || runtimeDefinition.Frames.Length == 0)
		{
			return;
		}
		bool cachedEditorHint = CachedEditorHint;
		AdobeAnimateSlot[] array = (cachedEditorHint ? _slotChildren : GetRuntimeSlotChildren());
		int i;
		for (i = 0; i < array.Length; i++)
		{
			AdobeAnimateSlot adobeAnimateSlot = array[i];
			if (adobeAnimateSlot == null || !adobeAnimateSlot.IsRuntimeInsideTree || adobeAnimateSlot.mode != 0)
			{
				continue;
			}
			int slotLayerId = adobeAnimateSlot.followSlotId - 1;
			if (!TryGetInterpolatedSlotPoseFast(slotLayerId, out var mediaId, out var transform) || mediaId == 65535 || transform == Transform2D.Identity)
			{
				if (adobeAnimateSlot.useFollowVisible)
				{
					adobeAnimateSlot.ApplyRuntimeFollowVisibility(visible: false);
				}
				continue;
			}
			Transform2D transform2 = transform.Translated(offset).TranslatedLocal(adobeAnimateSlot.offset);
			if (!adobeAnimateSlot.useRotate)
			{
				transform2 = transform2.RotatedLocal(0f - transform2.Rotation);
			}
			if (!adobeAnimateSlot.useScale)
			{
				Vector2 scale = transform2.Scale;
				if (scale.X != 0f && scale.Y != 0f)
				{
					transform2 = transform2.ScaledLocal(Vector2.One / scale);
				}
			}
			adobeAnimateSlot.ApplyRuntimeTransform(transform2);
			if (!adobeAnimateSlot.useSkew)
			{
				adobeAnimateSlot.Skew = 0f;
			}
			if (adobeAnimateSlot.useFollowVisible)
			{
				adobeAnimateSlot.ApplyRuntimeFollowVisibility(visible: true);
			}
		}
		for (int j = 0; j < _spriteChildren.Length; j++)
		{
			AdobeAnimateSprite adobeAnimateSprite = _spriteChildren[j];
			if (adobeAnimateSprite == null || !adobeAnimateSprite.IsRuntimeInsideTree)
			{
				continue;
			}
			if (!cachedEditorHint)
			{
				adobeAnimateSprite.ApplyRuntimeParentState(this);
			}
			int layerId;
			bool isInserted = TryGetInsertedLayerForChild(adobeAnimateSprite, out layerId);
			int num = ResolveSpriteChildRenderLayer(adobeAnimateSprite, j, isInserted, layerId);
			int num2 = ResolveSpriteChildFollowLayer(adobeAnimateSprite, j);
			if (num < 0 && num2 < 0)
			{
				if (adobeAnimateSprite.useFollowVisible)
				{
					adobeAnimateSprite.ApplyRuntimeFollowVisibility(visible: false);
				}
				continue;
			}
			if (IsSpriteChildOwnedBySlot(j))
			{
				if (cachedEditorHint && num >= 0)
				{
					ApplyChildRenderSort(adobeAnimateSprite, num);
				}
				continue;
			}
			if (num2 < 0 || (!TryGetInterpolatedLayerPose(num2, out i, out var transform3) && !TryGetInterpolatedDrawOrderPose(num2, out i, out transform3)))
			{
				if (adobeAnimateSprite.useFollowVisible)
				{
					adobeAnimateSprite.ApplyRuntimeFollowVisibility(visible: false);
				}
				continue;
			}
			if (transform3 == Transform2D.Identity)
			{
				if (adobeAnimateSprite.useFollowVisible)
				{
					adobeAnimateSprite.ApplyRuntimeFollowVisibility(visible: false);
				}
				continue;
			}
			if (adobeAnimateSprite.useFollowVisible)
			{
				adobeAnimateSprite.ApplyRuntimeFollowVisibility(visible: true);
			}
			if (adobeAnimateSprite.usePos)
			{
				bool flag = false;
				Vector2 vector = transform3.Origin + offset;
				if (adobeAnimateSprite.Position != vector)
				{
					adobeAnimateSprite.Position = vector;
					flag = true;
				}
				if (adobeAnimateSprite.useRotate)
				{
					float num3 = (float)((double)transform3.Rotation + adobeAnimateSprite.offsetRotate);
					if (!Mathf.IsEqualApprox(adobeAnimateSprite.Rotation, num3))
					{
						adobeAnimateSprite.Rotation = num3;
						flag = true;
					}
				}
				if (flag)
				{
					adobeAnimateSprite.NotifyAncestorTransformChangedForRender();
				}
			}
			if (cachedEditorHint && num >= 0)
			{
				ApplyChildRenderSort(adobeAnimateSprite, num);
			}
		}
	}

	private void EmitFrameEvents(int frameIdx)
	{
		if (_flashAnimeData == null || frameIdx < 0)
		{
			return;
		}
		HashSet<int> hashSet = _flashAnimeData.EventFrameIndices;
		if (hashSet == null)
		{
			hashSet = new HashSet<int>();
			int count = _flashAnimeData.events.Count;
			for (int i = 0; i < count; i++)
			{
				Godot.Collections.Array array = _flashAnimeData.events[i];
				if (array != null && array.Count > 0)
				{
					hashSet.Add(i);
				}
			}
			_flashAnimeData.EventFrameIndices = hashSet;
		}
		if (!hashSet.Contains(frameIdx))
		{
			return;
		}
		if (frameIdx >= _flashAnimeData.events.Count)
		{
			hashSet.Remove(frameIdx);
			return;
		}
		Godot.Collections.Array array2 = _flashAnimeData.events[frameIdx];
		if (array2 == null || array2.Count <= 0)
		{
			return;
		}
		foreach (Variant item in array2)
		{
			Dictionary dictionary = (Dictionary)item;
			this.OnAnimeEvent?.Invoke(dictionary["Command"].AsString(), dictionary["Argument"]);
		}
	}

	private void CacheChildren()
	{
		OwnedChildBinding[] bindings = ArrayPool<OwnedChildBinding>.Shared.Rent(Math.Max(4, GetChildCount()));
		int bindingCount = 0;
		int slotCount = 0;
		int spriteCount = 0;
		try
		{
			CollectOwnedChildBindings(this, collectSpriteChildren: true, null, !IsInsideTree(), ref bindings, ref bindingCount, ref slotCount, ref spriteCount);
			_slotChildren = ((slotCount > 0) ? new AdobeAnimateSlot[slotCount] : System.Array.Empty<AdobeAnimateSlot>());
			_spriteChildren = ((spriteCount > 0) ? new AdobeAnimateSprite[spriteCount] : System.Array.Empty<AdobeAnimateSprite>());
			_spriteChildOwnerSlots = ((spriteCount > 0) ? new AdobeAnimateSlot[spriteCount] : System.Array.Empty<AdobeAnimateSlot>());
			int num = 0;
			int num2 = 0;
			for (int i = 0; i < bindingCount; i++)
			{
				ref OwnedChildBinding reference = ref bindings[i];
				if (reference.Slot != null)
				{
					_slotChildren[num++] = reference.Slot;
					continue;
				}
				_spriteChildren[num2] = reference.Sprite;
				_spriteChildOwnerSlots[num2++] = reference.OwnerSlot;
			}
		}
		finally
		{
			ArrayPool<OwnedChildBinding>.Shared.Return(bindings, clearArray: true);
		}
		_hasChildren = _slotChildren.Length != 0 || _spriteChildren.Length != 0;
		AutoInsertChildSprites();
	}

	private void CollectOwnedChildBindings(Node parent, bool collectSpriteChildren, AdobeAnimateSlot ownerSlot, bool refreshSlotRequirements, ref OwnedChildBinding[] bindings, ref int bindingCount, ref int slotCount, ref int spriteCount)
	{
		int childCount = parent.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = parent.GetChild(i);
			if (child is AdobeAnimateSlot adobeAnimateSlot)
			{
				adobeAnimateSlot.sprite = this;
				if (refreshSlotRequirements)
				{
					adobeAnimateSlot.RefreshRuntimeUpdateRequirement();
				}
				AddOwnedChildBinding(ref bindings, ref bindingCount, new OwnedChildBinding
				{
					Slot = adobeAnimateSlot
				});
				slotCount++;
				CollectOwnedChildBindings(adobeAnimateSlot, collectSpriteChildren: true, adobeAnimateSlot, refreshSlotRequirements, ref bindings, ref bindingCount, ref slotCount, ref spriteCount);
			}
			else if (child is AdobeAnimateSprite sprite)
			{
				if (collectSpriteChildren)
				{
					AddOwnedChildBinding(ref bindings, ref bindingCount, new OwnedChildBinding
					{
						Sprite = sprite,
						OwnerSlot = ownerSlot
					});
					spriteCount++;
				}
			}
			else if (child.GetChildCount() > 0)
			{
				CollectOwnedChildBindings(child, ownerSlot != null, ownerSlot, refreshSlotRequirements, ref bindings, ref bindingCount, ref slotCount, ref spriteCount);
			}
		}
	}

	private static void AddOwnedChildBinding(ref OwnedChildBinding[] bindings, ref int bindingCount, OwnedChildBinding binding)
	{
		if (bindingCount == bindings.Length)
		{
			OwnedChildBinding[] array = ArrayPool<OwnedChildBinding>.Shared.Rent(bindings.Length * 2);
			System.Array.Copy(bindings, array, bindingCount);
			ArrayPool<OwnedChildBinding>.Shared.Return(bindings, clearArray: true);
			bindings = array;
		}
		bindings[bindingCount++] = binding;
	}

	private void CollectOwnedChildren(Node parent, List<AdobeAnimateSlot> slots, List<AdobeAnimateSprite> sprites, List<AdobeAnimateSlot> spriteOwnerSlots, bool collectSpriteChildren, AdobeAnimateSlot ownerSlot)
	{
		int childCount = parent.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = parent.GetChild(i);
			if (child is AdobeAnimateSlot adobeAnimateSlot)
			{
				adobeAnimateSlot.sprite = this;
				adobeAnimateSlot.RefreshRuntimeUpdateRequirement();
				slots.Add(adobeAnimateSlot);
				CollectOwnedChildren(adobeAnimateSlot, slots, sprites, spriteOwnerSlots, collectSpriteChildren: true, adobeAnimateSlot);
			}
			else if (child is AdobeAnimateSprite item)
			{
				if (collectSpriteChildren)
				{
					sprites.Add(item);
					spriteOwnerSlots.Add(ownerSlot);
				}
			}
			else if (child.GetChildCount() > 0)
			{
				CollectOwnedChildren(child, slots, sprites, spriteOwnerSlots, ownerSlot != null, ownerSlot);
			}
		}
	}

	internal void MarkSlotRuntimeUpdateCacheDirty()
	{
		_runtimeSlotChildrenDirty = true;
		_runtimeChildUpdatesVisualOnlyDirty = true;
		RefreshRuntimeChildUpdatePresence();
		InvalidateRuntimeDisplayVisualRequirementCache();
	}

	protected void SyncRuntimeChildState(AdobeAnimateSprite child)
	{
		if (child != null && child.IsRuntimeInsideTree)
		{
			if (CachedEditorHint)
			{
				child.pause = pause;
				child.LightMask = base.LightMask;
			}
			else
			{
				child.ApplyRuntimeParentState(this);
			}
		}
	}

	private void ApplyRuntimeParentState(AdobeAnimateSprite parent)
	{
		if (_runtimeParentStateSource != parent)
		{
			_runtimeParentStateSource = parent;
			_runtimeParentStateValidationCountdown = 0;
		}
		SetParentPlaybackStopped(parent.IsPlaybackStopped);
		if (_pause != parent._pause)
		{
			pause = parent._pause;
		}
		if (_runtimeParentStateValidationCountdown > 0)
		{
			_runtimeParentStateValidationCountdown--;
			return;
		}
		int lightMask = parent.LightMask;
		if (base.LightMask != lightMask)
		{
			base.LightMask = lightMask;
		}
		_runtimeParentStateValidationCountdown = 30;
	}

	private void ApplyRuntimeFollowVisibility(bool visible)
	{
		if (!_runtimeFollowVisibilityKnown || _runtimeFollowVisibilityValue != visible)
		{
			_runtimeFollowVisibilityKnown = true;
			_runtimeFollowVisibilityValue = visible;
			if (base.Visible != visible)
			{
				base.Visible = visible;
			}
		}
	}

	private void ResetRuntimeFollowVisibilityCache()
	{
		_runtimeFollowVisibilityKnown = false;
	}

	private AdobeAnimateSlot[] GetRuntimeSlotChildren()
	{
		if (!_runtimeSlotChildrenDirty)
		{
			return _runtimeSlotChildren;
		}
		int num = 0;
		for (int i = 0; i < _slotChildren.Length; i++)
		{
			AdobeAnimateSlot adobeAnimateSlot = _slotChildren[i];
			if (adobeAnimateSlot != null && adobeAnimateSlot.RequiresRuntimeUpdate)
			{
				num++;
			}
		}
		if (num == _slotChildren.Length)
		{
			_runtimeSlotChildren = _slotChildren;
			_runtimeSlotChildrenDirty = false;
			return _runtimeSlotChildren;
		}
		AdobeAnimateSlot[] array = new AdobeAnimateSlot[num];
		int num2 = 0;
		for (int j = 0; j < _slotChildren.Length; j++)
		{
			AdobeAnimateSlot adobeAnimateSlot2 = _slotChildren[j];
			if (adobeAnimateSlot2 != null && adobeAnimateSlot2.RequiresRuntimeUpdate)
			{
				array[num2++] = adobeAnimateSlot2;
			}
		}
		_runtimeSlotChildren = array;
		_runtimeSlotChildrenDirty = false;
		return _runtimeSlotChildren;
	}

	private void AutoInsertChildSprites()
	{
		if (_autoInserting || _layerVisible.Count == 0 || _spriteChildren.Length == 0)
		{
			return;
		}
		List<(AdobeAnimateSprite, int)> list = new List<(AdobeAnimateSprite, int)>();
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (adobeAnimateSprite == null || !GodotObject.IsInstanceValid(adobeAnimateSprite) || adobeAnimateSprite._parentSprite != this || adobeAnimateSprite.GetParent() != this)
			{
				continue;
			}
			bool flag = false;
			foreach (InsertedSprite insertedSprite in _insertedSprites)
			{
				if (insertedSprite.sprite == adobeAnimateSprite)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				int num = adobeAnimateSprite.insertLayerId;
				if (num >= 0 && num < _layerVisible.Count)
				{
					list.Add((adobeAnimateSprite, num));
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		_autoInserting = true;
		try
		{
			foreach (var (childSprite, layerId) in list)
			{
				InsertSpriteAtLayer(childSprite, layerId);
			}
		}
		finally
		{
			_autoInserting = false;
		}
	}

	public void SetMultimeshModulate(Node parent)
	{
		ApplyComposedModulate();
		int childCount = parent.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = parent.GetChild(i);
			if (child is AdobeAnimateSprite adobeAnimateSprite)
			{
				adobeAnimateSprite.meshColor = _meshColor;
			}
			if (child.GetChildCount() > 0)
			{
				SetMultimeshModulate(child);
			}
		}
	}

	private bool TryGetRuntimeClipRange(string clipName, out Vector2I range)
	{
		range = default(Vector2I);
		if (CachedEditorHint || !GodotObject.IsInstanceValid(_flashAnimeData))
		{
			return false;
		}
		return GetRuntimeDefinition()?.TryGetClipRange(clipName, out range) ?? false;
	}

	public bool HasClip(string clipName)
	{
		if (!GodotObject.IsInstanceValid(_flashAnimeData))
		{
			return false;
		}
		if (!CachedEditorHint)
		{
			AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
			if (runtimeDefinition != null && runtimeDefinition.HasClip(clipName))
			{
				return true;
			}
		}
		return _flashAnimeData.HasClip(clipName);
	}

	private void UpdateChildPoseImmediate()
	{
		if (_hasChildren)
		{
			UpdateChild();
			_lastUpdateFrame = frameIndex;
		}
	}

	public virtual void SetClip(string clipName)
	{
		_playbackRevision++;
		_managedPoseRevision++;
		MarkEffectOnceBatchEligibilityChanged();
		InvalidateRuntimeDisplayVisualRequirementCache();
		_needsRenderSubmission = true;
		string text = _clip;
		Vector2I vector2I = clipRange;
		bool flag = _renderStaticStateCached && _cachedConfiguredLayersAllVisible && _cachedLayerStateVersion == _layerStateVersion && _cachedMediaReplaceStateVersion == _mediaReplaceStateVersion;
		float renderFrameFloat = GetRenderFrameFloat();
		_clip = clipName;
		if (GodotObject.IsInstanceValid(_flashAnimeData))
		{
			if (CachedEditorHint)
			{
				blendTime = blendTimeInit;
			}
			Vector2I range;
			Vector2I vector2I2 = (TryGetRuntimeClipRange(_clip, out range) ? range : _flashAnimeData.GetClip(_clip));
			vector2I2.Y = Mathf.Clamp(vector2I2.Y, -1, _flashAnimeData.frameMax);
			if (vector2I2 != Vector2I.One * -1)
			{
				int num = Mathf.Clamp(frameIndex, clipRange.X, clipRange.Y);
				if (initClip && (!skipLastFrame || (num != clipRange.X && num != clipRange.Y)))
				{
					blendTimer = 0.0;
					if (blendTime > 0.0 && HasClip(text))
					{
						_blendFromFrameFloat = renderFrameFloat;
						blend = true;
					}
					else
					{
						blend = false;
						blendTime = 0.0;
						blendTimer = 0.0;
					}
				}
				clipRange = vector2I2;
				frameIndex = clipRange.X;
				elapsedTimer = 0.0;
				_lastUpdateFrame = -1;
				if (_applyingImmediateAnimation)
				{
					clipOver = false;
				}
				else
				{
					SetDeferred("clipOver", false);
				}
				if (!_processEnabled)
				{
					SetProcessEnabled(enabled: true);
				}
			}
		}
		initClip = true;
		UpdateChildPoseImmediate();
		if (text != _clip || vector2I != clipRange)
		{
			if (flag)
			{
				RefreshLayerActiveInCurrentClipCache(_cachedLayerVisibleCount, GetRuntimeDefinition());
			}
			else
			{
				_renderStaticStateCached = false;
			}
		}
		InvalidateRenderSnapshotCache();
		InvalidateRuntimeCrowdCullingForRefresh();
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, !flag);
	}

	public virtual void ResetAnimation()
	{
		_playbackRevision++;
		_managedPoseRevision++;
		MarkEffectOnceBatchEligibilityChanged();
		elapsedTimer = 0.0;
		frameIndex = clipRange.X;
		_lastUpdateFrame = -1;
		UpdateChildPoseImmediate();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		AdobeAnimateSprite[] spriteChildren = _spriteChildren;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildren)
		{
			if (adobeAnimateSprite != null && GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.ResetAnimation();
			}
		}
		if (!_processEnabled)
		{
			SetProcessEnabled(enabled: true);
		}
	}

	public void SetAnimation(string clipName, bool loop = true, double blendTime = 0.0)
	{
		MarkEffectOnceBatchEligibilityChanged();
		track.Clear();
		_nextAnimDelayTimer = -1.0;
		_pendingTrackConfig = null;
		if (!string.IsNullOrEmpty(clipName) && clipName.IndexOf('&') < 0)
		{
			clipOver = false;
			if (!_processEnabled)
			{
				SetProcessEnabled(enabled: true);
			}
			ApplyImmediateAnimation(clipName, loop, blendTime);
			return;
		}
		AddAnimation(clipName, 0.0, loop, blendTime);
		clipOver = false;
		if (!_processEnabled)
		{
			SetProcessEnabled(enabled: true);
		}
		NextAnimation();
	}

	public void AddAnimation(string clipName, double delay, bool loop = true, double blendTime = 0.0)
	{
		string[] array = clipName.Split("&", StringSplitOptions.RemoveEmptyEntries);
		if (array.Length != 0)
		{
			string text = array[GD.RandRange(0, array.Length - 1)];
			track.Insert(0, new AdobeAnimateTrack(text, delay, loop, blendTime));
			MarkEffectOnceBatchEligibilityChanged();
		}
	}

	public AdobeAnimateTrack NextAnimation()
	{
		if (track.Count <= 0)
		{
			if (!CachedEditorHint)
			{
				this.OnAnimeStoped?.Invoke(_clip);
			}
			return null;
		}
		AdobeAnimateTrack adobeAnimateTrack = track[track.Count - 1];
		track.RemoveAt(track.Count - 1);
		if (adobeAnimateTrack != null)
		{
			if (adobeAnimateTrack.delay != 0.0)
			{
				_nextAnimDelayTimer = adobeAnimateTrack.delay;
				_pendingTrackConfig = adobeAnimateTrack;
				if (!_processEnabled)
				{
					SetProcessEnabled(enabled: true);
				}
				return adobeAnimateTrack;
			}
			_nextAnimDelayTimer = -1.0;
			_pendingTrackConfig = null;
			ApplyNextAnimation(adobeAnimateTrack);
		}
		else if (!CachedEditorHint)
		{
			this.OnAnimeStoped?.Invoke(_clip);
		}
		return adobeAnimateTrack;
	}

	private void ApplyNextAnimation(AdobeAnimateTrack trackConfig)
	{
		if (GodotObject.IsInstanceValid(this) && trackConfig != null)
		{
			ApplyImmediateAnimation(trackConfig.clip, trackConfig.loop, trackConfig.blendTime);
		}
	}

	private void ApplyImmediateAnimation(string clipName, bool nextLoop, double nextBlendTime)
	{
		blendTime = nextBlendTime;
		if (CachedEditorHint)
		{
			blendTime = blendTimeInit;
		}
		loop = nextLoop;
		bool applyingImmediateAnimation = _applyingImmediateAnimation;
		_applyingImmediateAnimation = true;
		try
		{
			SetClip(clipName);
		}
		finally
		{
			_applyingImmediateAnimation = applyingImmediateAnimation;
		}
		if (!CachedEditorHint)
		{
			this.OnAnimeStarted?.Invoke(_clip);
			clipOver = false;
			if (!_processEnabled)
			{
				SetProcessEnabled(enabled: true);
			}
		}
	}

	public double GetProgress()
	{
		double num = (double)(clipRange.Y - clipRange.X) + 1.0;
		if (num <= 0.0)
		{
			return 0.0;
		}
		return (double)(frameIndex - clipRange.X) / num;
	}

	public void SetFliter(StringName layerName, bool open)
	{
		if (_flashAnimeData != null && _flashAnimeData.layerDictionary != null && _flashAnimeData.layerDictionary.ContainsKey(layerName))
		{
			int num = (int)_flashAnimeData.layerDictionary[layerName];
			if (num >= 0 && num < _layerVisible.Count)
			{
				_layerVisible[num] = open;
				MarkLayerStateChanged();
			}
		}
	}

	public bool GetFliter(StringName layerName)
	{
		if (_flashAnimeData == null || _flashAnimeData.layerDictionary == null)
		{
			return false;
		}
		if (_flashAnimeData.layerDictionary.ContainsKey(layerName))
		{
			int num = (int)_flashAnimeData.layerDictionary[layerName];
			if (num >= 0 && num < _layerVisible.Count)
			{
				return _layerVisible[num];
			}
		}
		return false;
	}

	public void SetFliters(Godot.Collections.Array layerNameList, bool open)
	{
		foreach (Variant layerName in layerNameList)
		{
			SetFliter(layerName.AsStringName(), open);
		}
	}

	public void SetFliterRecursive(StringName layerName, bool open)
	{
		SetFliter(layerName, open);
		string recursiveFilterSourceKey = GetRecursiveFilterSourceKey(_flashAnimeData);
		if (!string.IsNullOrEmpty(recursiveFilterSourceKey))
		{
			ApplyFliterToChildSprites(this, recursiveFilterSourceKey, layerName, open);
		}
	}

	public void SetFlitersRecursive(Godot.Collections.Array layerNameList, bool open)
	{
		SetFliters(layerNameList, open);
		string recursiveFilterSourceKey = GetRecursiveFilterSourceKey(_flashAnimeData);
		if (!string.IsNullOrEmpty(recursiveFilterSourceKey))
		{
			ApplyFlitersToChildSprites(this, recursiveFilterSourceKey, layerNameList, open);
		}
	}

	private static void ApplyFliterToChildSprites(Node node, string sourceKey, StringName layerName, bool open)
	{
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = node.GetChild(i);
			if (child is AdobeAnimateSprite adobeAnimateSprite && IsMatchingRecursiveFilterSprite(adobeAnimateSprite, sourceKey))
			{
				adobeAnimateSprite.SetFliter(layerName, open);
			}
			ApplyFliterToChildSprites(child, sourceKey, layerName, open);
		}
	}

	private static void ApplyFlitersToChildSprites(Node node, string sourceKey, Godot.Collections.Array layerNameList, bool open)
	{
		int childCount = node.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = node.GetChild(i);
			if (child is AdobeAnimateSprite adobeAnimateSprite && IsMatchingRecursiveFilterSprite(adobeAnimateSprite, sourceKey))
			{
				adobeAnimateSprite.SetFliters(layerNameList, open);
			}
			ApplyFlitersToChildSprites(child, sourceKey, layerNameList, open);
		}
	}

	private static bool IsMatchingRecursiveFilterSprite(AdobeAnimateSprite sprite, string sourceKey)
	{
		if (!string.IsNullOrEmpty(sourceKey))
		{
			return string.Equals(GetRecursiveFilterSourceKey(sprite?._flashAnimeData), sourceKey, StringComparison.Ordinal);
		}
		return false;
	}

	private static string GetRecursiveFilterSourceKey(AdobeAnimateData data)
	{
		return data?.GetAtlasSourceKey() ?? "";
	}

	public void SetReplace(StringName mediaName, PackedScene scene)
	{
		SetReplace(mediaName, (Resource)scene);
	}

	public void SetReplace(StringName mediaName, Resource resource)
	{
		SetReplace(mediaName, resource as Texture2D);
	}

	public void SetReplace(StringName mediaName, Texture2D texture)
	{
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null || !_flashAnimeData.mediaDictionary.ContainsKey(mediaName))
		{
			return;
		}
		int num = (int)_flashAnimeData.mediaDictionary[mediaName];
		if (num >= 0 && num < _mediaReplace.Count)
		{
			_mediaReplace[num] = texture;
			if (num < _mediaReplaceAtlasPaths.Count)
			{
				_mediaReplaceAtlasPaths[num] = string.Empty;
			}
			_mediaReplaceUse[num] = GodotObject.IsInstanceValid(texture);
			CaptureStaticAtlasPathLayoutConfigurationSnapshot();
			InvalidateStaticAtlasPathLayoutForNodeMutation();
			QueueUpdateMediaReplace();
		}
	}

	public bool SetAtlasReplace(StringName mediaName, string textureReference, bool queueUpdate = true)
	{
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null || !_flashAnimeData.mediaDictionary.ContainsKey(mediaName))
		{
			return false;
		}
		int num = (int)_flashAnimeData.mediaDictionary[mediaName];
		if (num < 0 || num >= _mediaReplace.Count || num >= _mediaReplaceUse.Count || num >= _mediaReplaceAtlasPaths.Count)
		{
			return false;
		}
		string value = textureReference ?? string.Empty;
		_mediaReplace[num] = null;
		_mediaReplaceAtlasPaths[num] = value;
		_mediaReplaceUse[num] = !string.IsNullOrWhiteSpace(value);
		CaptureStaticAtlasPathLayoutConfigurationSnapshot();
		InvalidateStaticAtlasPathLayoutForNodeMutation();
		if (queueUpdate)
		{
			QueueUpdateMediaReplace();
		}
		return true;
	}

	public string GetAtlasReplacePath(StringName mediaName)
	{
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null || !_flashAnimeData.mediaDictionary.ContainsKey(mediaName))
		{
			return string.Empty;
		}
		int num = (int)_flashAnimeData.mediaDictionary[mediaName];
		if (num < 0 || num >= _mediaReplaceAtlasPaths.Count)
		{
			return string.Empty;
		}
		return _mediaReplaceAtlasPaths[num];
	}

	public Texture2D GetReplace(StringName mediaName)
	{
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null)
		{
			return null;
		}
		if (_flashAnimeData.mediaDictionary.ContainsKey(mediaName))
		{
			int num = (int)_flashAnimeData.mediaDictionary[mediaName];
			if (num >= 0 && num < _mediaReplace.Count)
			{
				return _mediaReplace[num];
			}
		}
		return null;
	}

	public void SetVerticalClip(bool enabled, float upY, float downY)
	{
		if (_verticalClip.Enabled != enabled || !Mathf.IsEqualApprox(_verticalClip.UpY, upY) || !Mathf.IsEqualApprox(_verticalClip.DownY, downY))
		{
			_verticalClip.Enabled = enabled;
			_verticalClip.UpY = upY;
			_verticalClip.DownY = downY;
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
		}
	}

	public void SetVerticalClip(float upY, float downY)
	{
		SetVerticalClip(enabled: true, upY, downY);
	}

	public VerticalClipState GetVerticalClipState()
	{
		return _verticalClip;
	}

	public void SetDiscardUpPos(float value)
	{
		if (!_verticalClip.Enabled || !Mathf.IsEqualApprox(_verticalClip.UpY, value))
		{
			_verticalClip.Enabled = true;
			_verticalClip.UpY = value;
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
		}
	}

	public void SetDiscardDownPos(float value)
	{
		bool flag = !Mathf.IsEqualApprox(_verticalClip.UpY, -10000f) || !Mathf.IsEqualApprox(value, 10000f);
		if (_verticalClip.Enabled != flag || !Mathf.IsEqualApprox(_verticalClip.DownY, value))
		{
			_verticalClip.Enabled = flag;
			_verticalClip.DownY = value;
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
		}
	}

	public void SetSpriteGroupShaderParameter(string property, Variant value)
	{
		if (!(property == "discardUpPos"))
		{
			if (property == "discardDownPos")
			{
				SetDiscardDownPos((float)value.AsDouble());
			}
		}
		else
		{
			SetDiscardUpPos((float)value.AsDouble());
		}
	}

	public void InsertSpriteAtLayer(AdobeAnimateSprite childSprite, int layerId)
	{
		if (childSprite != null && childSprite != this && layerId >= 0 && layerId < _layerVisible.Count)
		{
			childSprite.insertLayerId = layerId;
			TrackInsertedSpriteAtLayer(childSprite, layerId);
		}
	}

	private void RefreshChildInsertLayerForEditor(AdobeAnimateSprite child)
	{
		if (child != null && GodotObject.IsInstanceValid(child) && child != this)
		{
			int num = ResolveSpriteChildInsertLayer(child);
			if (num >= 0)
			{
				TrackInsertedSpriteAtLayer(child, num);
			}
			else if (!UntrackInsertedSpriteForEditor(child))
			{
				ClearInsertedChildRenderSort(child);
				child.InvalidateRenderSnapshotCache();
				child.RequestNodeRedraw();
				InvalidateRenderSnapshotCache();
				RequestNodeRedraw();
			}
		}
	}

	private void TrackInsertedSpriteAtLayer(AdobeAnimateSprite childSprite, int layerId)
	{
		if (childSprite == null || childSprite == this || layerId < 0 || layerId > _layerVisible.Count)
		{
			return;
		}
		if (childSprite.GetParent() != this)
		{
			AddChild(childSprite, forceReadableName: false, InternalMode.Disabled);
		}
		for (int i = 0; i < _insertedSprites.Count; i++)
		{
			if (_insertedSprites[i].sprite == childSprite)
			{
				_insertedSprites[i] = new InsertedSprite
				{
					sprite = childSprite,
					layerId = layerId
				};
				ApplyChildRenderSort(childSprite, layerId);
				CacheChildren();
				InvalidateRenderSnapshotCache();
				RequestNodeRedraw();
				return;
			}
		}
		_insertedSprites.Add(new InsertedSprite
		{
			sprite = childSprite,
			layerId = layerId
		});
		ApplyChildRenderSort(childSprite, layerId);
		CacheChildren();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	private bool UntrackInsertedSpriteForEditor(AdobeAnimateSprite childSprite)
	{
		for (int i = 0; i < _insertedSprites.Count; i++)
		{
			if (_insertedSprites[i].sprite == childSprite)
			{
				_insertedSprites.RemoveAt(i);
				ClearInsertedChildRenderSort(childSprite);
				CacheChildren();
				childSprite.InvalidateRenderSnapshotCache();
				childSprite.RequestNodeRedraw();
				InvalidateRenderSnapshotCache();
				RequestNodeRedraw();
				return true;
			}
		}
		return false;
	}

	private void OnSpriteChildParentChanged(AdobeAnimateSprite childSprite)
	{
		bool flag = false;
		for (int num = _insertedSprites.Count - 1; num >= 0; num--)
		{
			if (_insertedSprites[num].sprite == childSprite)
			{
				_insertedSprites.RemoveAt(num);
				flag = true;
			}
		}
		if (flag && GodotObject.IsInstanceValid(childSprite))
		{
			ClearInsertedChildRenderSort(childSprite);
		}
		MarkManagedSlotSpriteCacheDirty();
	}

	public void RemoveInsertedSprite(AdobeAnimateSprite childSprite)
	{
		int num = -1;
		for (int i = 0; i < _insertedSprites.Count; i++)
		{
			if (_insertedSprites[i].sprite == childSprite)
			{
				num = i;
				break;
			}
		}
		if (num >= 0)
		{
			InsertedSprite insertedSprite = _insertedSprites[num];
			if (GodotObject.IsInstanceValid(insertedSprite.sprite))
			{
				ClearInsertedChildRenderSort(insertedSprite.sprite);
				RemoveChild(insertedSprite.sprite);
			}
			_insertedSprites.RemoveAt(num);
			CacheChildren();
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw();
		}
	}

	public void RemoveAllInsertedSprites()
	{
		foreach (InsertedSprite insertedSprite in _insertedSprites)
		{
			if (GodotObject.IsInstanceValid(insertedSprite.sprite))
			{
				ClearInsertedChildRenderSort(insertedSprite.sprite);
				RemoveChild(insertedSprite.sprite);
			}
		}
		_insertedSprites.Clear();
		CacheChildren();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	public Dictionary ExportSpriteSave()
	{
		Godot.Collections.Array array = new Godot.Collections.Array();
		foreach (AdobeAnimateTrack item in track)
		{
			array.Add(new Dictionary
			{
				{ "clip", item.clip },
				{ "delay", item.delay },
				{ "loop", item.loop },
				{ "blendTime", item.blendTime }
			});
		}
		return new Dictionary
		{
			{ "clip", _clip },
			{ "clipRangeX", clipRange.X },
			{ "clipRangeY", clipRange.Y },
			{ "frameIndex", frameIndex },
			{ "elapsedTimer", elapsedTimer },
			{ "loop", loop },
			{ "clipOver", clipOver },
			{ "timeScale", timeScale },
			{ "pause", _pause },
			{ "playBack", playBack },
			{ "blend", blend },
			{ "blendTime", blendTime },
			{ "blendTimer", blendTimer },
			{ "blendFromFrameFloat", _blendFromFrameFloat },
			{ "track", array },
			{
				"layerVisible",
				_layerVisible.Duplicate(deep: true)
			},
			{ "initClip", initClip }
		};
	}

	public void ImportSpriteSave(Dictionary data)
	{
		if (data.Count == 0)
		{
			return;
		}
		string text = (data.ContainsKey("clip") ? data["clip"].AsString() : _clip);
		int x = (data.ContainsKey("clipRangeX") ? data["clipRangeX"].AsInt32() : clipRange.X);
		int y = (data.ContainsKey("clipRangeY") ? data["clipRangeY"].AsInt32() : clipRange.Y);
		int num = (data.ContainsKey("frameIndex") ? data["frameIndex"].AsInt32() : frameIndex);
		double num2 = (data.ContainsKey("elapsedTimer") ? data["elapsedTimer"].AsDouble() : elapsedTimer);
		bool flag = (data.ContainsKey("loop") ? data["loop"].AsBool() : loop);
		bool flag2 = (data.ContainsKey("clipOver") ? data["clipOver"].AsBool() : clipOver);
		double num3 = (data.ContainsKey("timeScale") ? data["timeScale"].AsDouble() : timeScale);
		bool flag3 = (data.ContainsKey("pause") ? data["pause"].AsBool() : _pause);
		bool flag4 = (data.ContainsKey("playBack") ? data["playBack"].AsBool() : playBack);
		bool flag5 = (data.ContainsKey("blend") ? data["blend"].AsBool() : blend);
		double num4 = (data.ContainsKey("blendTime") ? data["blendTime"].AsDouble() : blendTime);
		double num5 = (data.ContainsKey("blendTimer") ? data["blendTimer"].AsDouble() : blendTimer);
		float blendFromFrameFloat = (data.ContainsKey("blendFromFrameFloat") ? data["blendFromFrameFloat"].AsSingle() : ((float)num + (float)num2));
		bool flag6 = (data.ContainsKey("initClip") ? data["initClip"].AsBool() : initClip);
		Array<bool> array = (data.ContainsKey("layerVisible") ? data["layerVisible"].AsGodotArray<bool>() : new Array<bool>());
		Godot.Collections.Array obj = (data.ContainsKey("track") ? data["track"].AsGodotArray() : new Godot.Collections.Array());
		_clip = text;
		clipRange = new Vector2I(x, y);
		frameIndex = num;
		elapsedTimer = num2;
		loop = flag;
		clipOver = flag2;
		timeScale = num3;
		pause = flag3;
		playBack = flag4;
		blend = flag5;
		blendTime = num4;
		blendTimer = num5;
		_blendFromFrameFloat = blendFromFrameFloat;
		initClip = flag6;
		if (array.Count == _layerVisible.Count)
		{
			layerVisible = array;
			MarkLayerStateChanged();
		}
		track.Clear();
		foreach (Variant item2 in obj)
		{
			Dictionary dictionary = item2.AsGodotDictionary();
			AdobeAnimateTrack item = new AdobeAnimateTrack(dictionary.ContainsKey("clip") ? dictionary["clip"].AsString() : "", dictionary.ContainsKey("delay") ? dictionary["delay"].AsDouble() : 0.0, !dictionary.ContainsKey("loop") || dictionary["loop"].AsBool(), dictionary.ContainsKey("blendTime") ? dictionary["blendTime"].AsDouble() : 0.0);
			track.Add(item);
		}
		if (!clipOver || track.Count > 0)
		{
			SetProcessEnabled(enabled: true);
		}
		InvalidateRenderSnapshotCache(force: true);
		RequestNodeRedraw();
	}

	private static void ResetCachedRenderFrameEffectiveAncestorModulates()
	{
		CachedRenderFrameEffectiveAncestorModulates.Clear();
	}

	private Color GetCachedEffectiveRenderAncestorModulate(Node renderMountParent)
	{
		int count = _renderModulateAncestors.Count;
		if (count == 0)
		{
			return Colors.White;
		}
		if (CachedViewportWorldRectRenderScopeDepth <= 0)
		{
			Color left = Colors.White;
			for (int i = 0; i < count; i++)
			{
				left = MultiplyRenderColor(in left, GetCachedRenderFrameModulate(_renderModulateAncestors[i]));
			}
			return left;
		}
		Color right = Colors.White;
		int num = count;
		for (int j = 0; j < count; j++)
		{
			CanvasItem key = _renderModulateAncestors[j];
			if (CachedRenderFrameEffectiveAncestorModulates.TryGetValue(key, out var value) && value.RenderMountParent == renderMountParent)
			{
				right = value.Value;
				num = j;
				break;
			}
		}
		for (int num2 = num - 1; num2 >= 0; num2--)
		{
			CanvasItem key2 = _renderModulateAncestors[num2];
			right = MultiplyRenderColor(GetCachedRenderFrameModulate(key2), in right);
			CachedRenderFrameEffectiveAncestorModulates[key2] = new EffectiveAncestorModulateCacheEntry(renderMountParent, right);
		}
		return right;
	}

	internal void MarkRenderOrderDebugForRender(string reason)
	{
		if (DebugRenderOrderTrace)
		{
			_renderOrderDebugPending = true;
			_renderOrderDebugReason = reason ?? "";
		}
	}

	internal bool HasRenderOrderDebugPendingForRender()
	{
		if (DebugRenderOrderTrace)
		{
			return _renderOrderDebugPending;
		}
		return false;
	}

	internal void InvalidateInheritedRenderOrderForRender(string reason = "")
	{
		_gpuGraphCrowdPresentationStateDirty = true;
		MarkEffectOnceBatchRenderStateDirty(invalidateZ: true);
		_effectiveRenderSortBandCached = false;
		_effectiveRenderSortBandRescanCountdown = 0;
		_treeOrderPathRescanCountdown = 0;
		MarkRenderOrderDebugForRender(reason);
		InvalidateRenderSnapshotCache();
	}

	internal void PrintRenderOrderDebugForRender(string phase, int effectiveZIndex, long sortBand, Node renderMountParent, AdobeAnimateRuntimeDefinition definition, string extra = "", bool consume = true)
	{
		if (!DebugRenderOrderTrace || !_renderOrderDebugPending)
		{
			return;
		}
		if (_debugRenderOrderTraceCount >= 800)
		{
			if (consume)
			{
				_renderOrderDebugPending = false;
			}
			return;
		}
		_debugRenderOrderTraceCount++;
		string text = (IsInsideTree() ? GetPath().ToString() : base.Name.ToString());
		string value = ((GodotObject.IsInstanceValid(renderMountParent) && renderMountParent.IsInsideTree()) ? renderMountParent.GetPath().ToString() : (renderMountParent?.Name.ToString() ?? "<null>"));
		string value2 = definition?.Source?.ResourcePath ?? _flashAnimeData?.ResourcePath ?? "<null>";
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		GD.Print($"[AdobeAnimateRenderOrderDebug] #{_debugRenderOrderTraceCount} phase={phase} path={text} reason=\"{_renderOrderDebugReason}\" localZ={ZIndex} zRel={ZAsRelative} behind={base.ShowBehindParent} effectiveZ={effectiveZIndex} cachedEffectiveZ={_cachedEffectiveZIndex} zCache={_effectiveZIndexCached} sortBand={sortBand} localSortBand={_renderSortBand} sortCache={_effectiveRenderSortBandCached} runtimeManager={_usingRuntimeManager} snapshotCache={_hasCachedRenderSnapshot} needsSubmit={_needsRenderSubmission} drawOrderSort={GetNeedsDrawOrderSortBandsForRender()} managedSlots={HasManagedSlotSpritesForRender()} childSprites={HasSpriteChildrenForRender()} clip={_clip} frame={GetRenderFrameFloat():F3} global=({cachedGlobalTransformForRender.Origin.X:F1},{cachedGlobalTransformForRender.Origin.Y:F1}) mount={value} canvas={_cachedCanvasLayer} data={value2} extra={extra}");
		GD.Print("[AdobeAnimateRenderOrderDebug.ParentChain] path=" + text + " chain=" + BuildRenderOrderParentChainDebug());
		if (consume)
		{
			_renderOrderDebugPending = false;
		}
	}

	private string BuildRenderOrderParentChainDebug()
	{
		StringBuilder stringBuilder = new StringBuilder(512);
		CanvasItem canvasItem = this;
		int num = 0;
		while (GodotObject.IsInstanceValid(canvasItem) && num++ < 16)
		{
			if (stringBuilder.Length > 0)
			{
				stringBuilder.Append(" <- ");
			}
			stringBuilder.Append(canvasItem.Name);
			stringBuilder.Append("(type=");
			stringBuilder.Append(canvasItem.GetType().Name);
			stringBuilder.Append(",z=");
			stringBuilder.Append(canvasItem.ZIndex);
			stringBuilder.Append(",rel=");
			stringBuilder.Append(canvasItem.ZAsRelative);
			stringBuilder.Append(",behind=");
			stringBuilder.Append(canvasItem.ShowBehindParent);
			stringBuilder.Append(')');
			if (!canvasItem.ZAsRelative)
			{
				break;
			}
			canvasItem = canvasItem.GetParent() as CanvasItem;
		}
		if (num >= 16 && GodotObject.IsInstanceValid(canvasItem))
		{
			stringBuilder.Append(" <- ...");
		}
		return stringBuilder.ToString();
	}

	private int GetEffectiveZIndexForRender()
	{
		if (!_usingRuntimeManager)
		{
			return GetEffectiveZIndex();
		}
		return GetCachedEffectiveZIndexForRender();
	}

	private int GetCachedEffectiveZIndexForRender()
	{
		if (_effectiveZIndexCached && _effectiveZIndexRescanCountdown > 0)
		{
			_effectiveZIndexRescanCountdown--;
			return _cachedEffectiveZIndex;
		}
		int zIndex = ZIndex;
		bool zAsRelative = ZAsRelative;
		_cachedEffectiveZIndex = GetEffectiveZIndex(zIndex, zAsRelative);
		_effectiveZIndexCacheLocalZ = zIndex;
		_effectiveZIndexCacheZAsRelative = zAsRelative;
		_effectiveZIndexCached = true;
		_effectiveZIndexRescanCountdown = GetStaggeredRescanInterval(20);
		return _cachedEffectiveZIndex;
	}

	private int GetEffectiveZIndex(int localZ, bool localZAsRelative)
	{
		long num = localZ;
		if (!localZAsRelative)
		{
			return (int)Math.Clamp(num, -4096L, 4096L);
		}
		CanvasItem canvasItem = GetParent() as CanvasItem;
		while (GodotObject.IsInstanceValid(canvasItem))
		{
			num += canvasItem.ZIndex;
			if (!canvasItem.ZAsRelative)
			{
				break;
			}
			canvasItem = canvasItem.GetParent() as CanvasItem;
		}
		return (int)Math.Clamp(num, -4096L, 4096L);
	}

	private int GetEffectiveZIndex()
	{
		long num = 0L;
		CanvasItem canvasItem = this;
		while (GodotObject.IsInstanceValid(canvasItem))
		{
			num += canvasItem.ZIndex;
			if (!canvasItem.ZAsRelative)
			{
				break;
			}
			canvasItem = canvasItem.GetParent() as CanvasItem;
		}
		return (int)Math.Clamp(num, -4096L, 4096L);
	}

	internal int[] GetEffectiveTreeOrderPathForRender()
	{
		if (!_usingRuntimeManager && CachedEditorHint)
		{
			AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
			Node cachedRenderMountParent = GetCachedRenderMountParent();
			return BuildEffectiveTreeOrderPathForRender(renderSortRootForRender, cachedRenderMountParent);
		}
		if (_treeOrderPathRescanCountdown > 0 && _cachedEffectiveTreeOrderPath != null)
		{
			_treeOrderPathRescanCountdown--;
			return _cachedEffectiveTreeOrderPath;
		}
		AdobeAnimateSprite renderSortRootForRender2 = GetRenderSortRootForRender();
		Node cachedRenderMountParent2 = GetCachedRenderMountParent();
		RefreshTreeOrderPathWatcher(renderSortRootForRender2);
		int[] cachedEffectiveTreeOrderPath = _cachedEffectiveTreeOrderPath;
		int[] array = BuildEffectiveTreeOrderPathForRender(renderSortRootForRender2, cachedRenderMountParent2);
		bool flag = !AreTreeOrderPathsEqual(cachedEffectiveTreeOrderPath, array);
		_cachedEffectiveTreeOrderPath = (flag ? array : cachedEffectiveTreeOrderPath);
		_treeOrderPathRescanCountdown = GetStaggeredRescanInterval(60);
		if (_usingRuntimeManager && cachedEffectiveTreeOrderPath != null && flag)
		{
			AdobeAnimateRuntimeManager.InvalidateRenderRoots();
		}
		return _cachedEffectiveTreeOrderPath;
	}

	private void RefreshTreeOrderPathWatcher(Node root)
	{
		Node node = (GodotObject.IsInstanceValid(root) ? root.GetParent() : null);
		if (_treeOrderWatchedParent != node)
		{
			DisconnectTreeOrderPathWatchers();
			_treeOrderWatchedParent = node;
			if (GodotObject.IsInstanceValid(_treeOrderWatchedParent))
			{
				_treeOrderWatchedParent.ChildOrderChanged += OnTreeOrderHierarchyChanged;
			}
		}
	}

	private void OnTreeOrderHierarchyChanged()
	{
		InvalidateTreeOrderPathCache();
		if (_usingRuntimeManager)
		{
			AdobeAnimateRuntimeManager.InvalidateRenderRoots();
		}
	}

	private static bool AreTreeOrderPathsEqual(int[] left, int[] right)
	{
		if (left == right)
		{
			return true;
		}
		if (left == null || right == null || left.Length != right.Length)
		{
			return false;
		}
		for (int i = 0; i < left.Length; i++)
		{
			if (left[i] != right[i])
			{
				return false;
			}
		}
		return true;
	}

	private void InvalidateTreeOrderPathCache()
	{
		_treeOrderPathRescanCountdown = 0;
	}

	internal void InvalidateRuntimeTreeOrderForTopologyChange()
	{
		InvalidateTreeOrderPathCache();
	}

	private void DisconnectTreeOrderPathWatchers()
	{
		if (GodotObject.IsInstanceValid(_treeOrderWatchedParent))
		{
			_treeOrderWatchedParent.ChildOrderChanged -= OnTreeOrderHierarchyChanged;
		}
		_treeOrderWatchedParent = null;
	}

	private static int[] BuildEffectiveTreeOrderPathForRender(Node node, Node renderMountParent)
	{
		if (!GodotObject.IsInstanceValid(node))
		{
			return System.Array.Empty<int>();
		}
		List<int> list = new List<int>();
		Node node2 = node;
		int num = 0;
		while (GodotObject.IsInstanceValid(node2) && node2 != renderMountParent && num++ < 128)
		{
			list.Add(Math.Max(0, node2.GetIndex()));
			node2 = node2.GetParent();
		}
		if (list.Count == 0)
		{
			list.Add(Math.Max(0, node.GetIndex()));
		}
		list.Reverse();
		return list.ToArray();
	}

	internal long GetEffectiveRenderSortBandForRender()
	{
		if (!_usingRuntimeManager)
		{
			return ComputeEffectiveRenderSortBand();
		}
		long renderSortBand = _renderSortBand;
		if (!_effectiveRenderSortBandCached || renderSortBand != _effectiveRenderSortBandCacheLocalSortBand || _effectiveRenderSortBandRescanCountdown <= 0)
		{
			_cachedEffectiveRenderSortBand = ComputeEffectiveRenderSortBand();
			_effectiveRenderSortBandCacheLocalSortBand = renderSortBand;
			_effectiveRenderSortBandCached = true;
			_effectiveRenderSortBandRescanCountdown = GetStaggeredRescanInterval(20);
			return _cachedEffectiveRenderSortBand;
		}
		_effectiveRenderSortBandRescanCountdown--;
		return _cachedEffectiveRenderSortBand;
	}

	private long ComputeEffectiveRenderSortBand()
	{
		return _renderSortBand + GetShowBehindParentSortBandBias();
	}

	private long GetShowBehindParentSortBandBias()
	{
		long num = 0L;
		CanvasItem canvasItem = this;
		int num2 = 0;
		while (GodotObject.IsInstanceValid(canvasItem) && num2++ < 64)
		{
			if (canvasItem.ShowBehindParent)
			{
				num += -1000000;
			}
			if (!canvasItem.ZAsRelative)
			{
				break;
			}
			canvasItem = canvasItem.GetParent() as CanvasItem;
		}
		return num;
	}

	private int GetStaggeredRescanInterval(int baseInterval)
	{
		int num = Math.Max(1, baseInterval);
		return num + (int)(GetCachedInstanceIdForRender() % (ulong)num);
	}

	private static ulong ComputeLayerVisibilitySignature(Array<bool> values, int valueCount)
	{
		if (values == null)
		{
			return 0uL;
		}
		ulong num = 1469598103934665603uL;
		for (int i = 0; i < valueCount; i++)
		{
			num ^= (ulong)(values[i] ? 1 : 0);
			num *= 1099511628211L;
		}
		return num;
	}

	private void DisableUnsharedMediaReplaceTextures(List<int> mediaIds)
	{
		if (mediaIds == null || mediaIds.Count == 0)
		{
			return;
		}
		int num = 0;
		for (int i = 0; i < mediaIds.Count; i++)
		{
			int num2 = mediaIds[i];
			if ((uint)num2 < (uint)(_mediaReplaceUse?.Count ?? 0) && _mediaReplaceUse[num2])
			{
				_mediaReplaceUse[num2] = false;
				if ((uint)num2 < (uint)(mediaReplaceRect?.Count ?? 0))
				{
					mediaReplaceRect[num2] = default(Rect2);
				}
				if ((uint)num2 < (uint)(mediaReplaceAtlasPages?.Count ?? 0))
				{
					mediaReplaceAtlasPages[num2] = 0;
				}
				num++;
			}
		}
		if (num > 0)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.mediaReplaceAtlas.missingSharedAtlas", num);
			if (!MissingMediaReplaceAtlasWarningPrinted)
			{
				MissingMediaReplaceAtlasWarningPrinted = true;
				GD.PushWarning("AdobeAnimate media replacement texture is missing from the shared Texture2DArray atlas; replacement drawing was disabled for that sprite.");
			}
		}
	}

	private bool TryUseSharedMediaReplaceAtlas(List<Texture2D> textures, List<string> atlasPaths, List<int> mediaIds)
	{
		if (textures == null || atlasPaths == null || mediaIds == null || textures.Count == 0 || textures.Count != atlasPaths.Count || textures.Count != mediaIds.Count)
		{
			return false;
		}
		Rect2[] array = new Rect2[textures.Count];
		int[] array2 = new int[textures.Count];
		TextureLayered textureLayered = null;
		Rid rid = default(Rid);
		Vector2 vector = default(Vector2);
		for (int i = 0; i < textures.Count; i++)
		{
			Texture2D texture2D = textures[i];
			string text = atlasPaths[i];
			AdobeAnimateExternalTextureAtlasAllocation allocation;
			if (!string.IsNullOrEmpty(text))
			{
				if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(text, out allocation))
				{
					return false;
				}
			}
			else
			{
				if (!GodotObject.IsInstanceValid(texture2D))
				{
					return false;
				}
				Vector2 size = texture2D.GetSize();
				if (size.X <= 0f || size.Y <= 0f)
				{
					return false;
				}
				if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(texture2D, new Rect2(Vector2.Zero, size), out allocation))
				{
					return false;
				}
			}
			if (!allocation.UsesTextureArray || !GodotObject.IsInstanceValid(allocation.TextureArray) || !allocation.TextureArrayRid.IsValid)
			{
				return false;
			}
			if (!rid.IsValid)
			{
				rid = allocation.TextureArrayRid;
				textureLayered = allocation.TextureArray;
				vector = allocation.TextureArraySize;
			}
			else if (allocation.TextureArrayRid != rid)
			{
				return false;
			}
			array[i] = allocation.Rect;
			array2[i] = allocation.AtlasPage;
		}
		mediaReplaceAtlas = null;
		mediaReplaceAtlasArray = textureLayered;
		mediaReplaceAtlasArraySize = vector;
		mediaReplaceAtlasUsesTextureArray = true;
		mediaReplaceAtlasShared = true;
		for (int j = 0; j < mediaIds.Count; j++)
		{
			int num = mediaIds[j];
			if ((uint)num < (uint)mediaReplaceRect.Count)
			{
				mediaReplaceRect[num] = array[j];
				mediaReplaceAtlasPages[num] = array2[j];
			}
		}
		TowerDefensePerfProfiler.Sample("adobeAnimate.mediaReplaceAtlas.sharedAtlas", mediaIds.Count);
		return true;
	}

	private static ulong BuildMediaReplaceSignature(Array<Texture2D> textures, int textureCount, Array<bool> uses, int useCount, Array<Rect2> rects, int rectCount, int limit, Array<int> pages, int pageCount, bool usesTextureArray)
	{
		if (textures == null || uses == null || rects == null || limit <= 0)
		{
			return 0uL;
		}
		ulong num = 1469598103934665603uL;
		num ^= (ulong)(usesTextureArray ? 1 : 0);
		num *= 1099511628211L;
		int num2 = Math.Min(limit, Math.Min(textureCount, Math.Min(useCount, rectCount)));
		for (int i = 0; i < num2; i++)
		{
			bool flag = uses[i];
			num ^= (ulong)(flag ? 1 : 0);
			num *= 1099511628211L;
			if (flag)
			{
				Texture2D texture2D = textures[i];
				num ^= (GodotObject.IsInstanceValid(texture2D) ? texture2D.GetInstanceId() : 0);
				num *= 1099511628211L;
				Rect2 rect = rects[i];
				num ^= (ulong)((usesTextureArray && pages != null && i < pageCount) ? pages[i] : 0).GetHashCode();
				num *= 1099511628211L;
				num ^= (ulong)rect.Position.X.GetHashCode();
				num *= 1099511628211L;
				num ^= (ulong)rect.Position.Y.GetHashCode();
				num *= 1099511628211L;
				num ^= (ulong)rect.Size.X.GetHashCode();
				num *= 1099511628211L;
				num ^= (ulong)rect.Size.Y.GetHashCode();
				num *= 1099511628211L;
			}
		}
		return num;
	}

	private static float ComputeMediaReplaceBoundsGrow(Array<bool> uses, int useCount, Array<Rect2> rects, int rectCount, int limit)
	{
		if (uses == null || rects == null || limit <= 0)
		{
			return 0f;
		}
		int num = Math.Min(limit, Math.Min(useCount, rectCount));
		float num2 = 0f;
		for (int i = 0; i < num; i++)
		{
			if (uses[i])
			{
				Rect2 rect = rects[i];
				if (!(rect.Size.X <= 0f) && !(rect.Size.Y <= 0f))
				{
					num2 = Math.Max(num2, Math.Max(rect.Size.X, rect.Size.Y));
				}
			}
		}
		return num2;
	}

	private static bool HasActiveMediaReplace(Array<bool> uses, int useCount, int limit)
	{
		if (uses == null || limit <= 0)
		{
			return false;
		}
		int num = Math.Min(limit, useCount);
		for (int i = 0; i < num; i++)
		{
			if (uses[i])
			{
				return true;
			}
		}
		return false;
	}

	private int GetMediaReplaceAtlasLayer()
	{
		if (!mediaReplaceAtlasUsesTextureArray || mediaReplaceAtlasPages == null)
		{
			return 0;
		}
		for (int i = 0; i < mediaReplaceAtlasPages.Count; i++)
		{
			if (_mediaReplaceUse != null && i < _mediaReplaceUse.Count && _mediaReplaceUse[i])
			{
				return Math.Max(0, mediaReplaceAtlasPages[i]);
			}
		}
		return 0;
	}

	private int GetMediaReplaceAtlasLayer(int mediaId)
	{
		if (!mediaReplaceAtlasUsesTextureArray || mediaReplaceAtlasPages == null || mediaId < 0 || mediaId >= mediaReplaceAtlasPages.Count)
		{
			return 0;
		}
		return Math.Max(0, mediaReplaceAtlasPages[mediaId]);
	}

	private bool HasValidMediaReplaceAtlas()
	{
		if (!mediaReplaceAtlasUsesTextureArray)
		{
			return GodotObject.IsInstanceValid(mediaReplaceAtlas);
		}
		return GodotObject.IsInstanceValid(mediaReplaceAtlasArray);
	}

	private Rid GetMediaReplaceAtlasRid()
	{
		if (!mediaReplaceAtlasUsesTextureArray || !GodotObject.IsInstanceValid(mediaReplaceAtlasArray))
		{
			if (!GodotObject.IsInstanceValid(mediaReplaceAtlas))
			{
				return default(Rid);
			}
			return mediaReplaceAtlas.GetRid();
		}
		return mediaReplaceAtlasArray.GetRid();
	}

	private void MarkLayerStateChanged()
	{
		_layerStateVersion++;
		MarkEffectOnceBatchEligibilityChanged();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	internal virtual void OnPartSnapshotCreated(AdobeAnimateSlot slot, Array<StringName> layers, AdobeAnimatePart part)
	{
	}

	private void RequestNodeRedraw(bool preferDisplayCadence = false, bool transformOnly = false, bool staticStateDirty = true)
	{
		if (staticStateDirty && !transformOnly)
		{
			_gpuGraphCrowdStaticStateDirty = true;
		}
		if (_effectOnceGpuSuppressed)
		{
			return;
		}
		if (_usingRuntimeManager && !CachedEditorHint && _parentSprite != null)
		{
			AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
			if (GodotObject.IsInstanceValid(renderSortRootForRender) && renderSortRootForRender != this)
			{
				renderSortRootForRender.RequestNodeRedraw(preferDisplayCadence, transformOnly, staticStateDirty);
				return;
			}
		}
		if (_usingRuntimeManager && CanReuseCachedCulledPose())
		{
			return;
		}
		_needsRenderSubmission = true;
		if (_usingRuntimeManager)
		{
			if (preferDisplayCadence)
			{
				AdobeAnimateRuntimeManager.NotifyDisplayRenderSubmission(this);
			}
			else
			{
				AdobeAnimateRuntimeManager.NotifyRenderSubmission(this);
			}
		}
		else
		{
			QueueRedraw();
		}
	}

	internal bool TryGetFrameDrawOrderPose(int frame, int drawOrder, out int mediaId, out Transform2D transform)
	{
		return TryGetFramePose(frame, drawOrder, useLayerId: false, out mediaId, out transform);
	}

	internal bool TryGetInterpolatedDrawOrderPose(int drawOrder, out int mediaId, out Transform2D transform)
	{
		return TryGetInterpolatedPose(drawOrder, useLayerId: false, out mediaId, out transform);
	}

	private bool TryGetFrameLayerPose(int frame, int layerId, out int mediaId, out Transform2D transform)
	{
		return TryGetFramePose(frame, layerId, useLayerId: true, out mediaId, out transform);
	}

	internal bool TryGetFrameLayerPoseForRender(int frame, int layerId, out int mediaId, out Transform2D transform)
	{
		return TryGetFrameLayerPose(frame, layerId, out mediaId, out transform);
	}

	internal bool TryGetInterpolatedLayerPoseForRender(int layerId, out int mediaId, out Transform2D transform)
	{
		return TryGetInterpolatedPose(layerId, useLayerId: true, out mediaId, out transform);
	}

	internal void AppendInterpolatedLayerElementsForRender(HashSet<int> selectedLayerIds, Vector2 anchorOrigin, Array<Godot.Collections.Array> output)
	{
		if (selectedLayerIds == null || selectedLayerIds.Count == 0 || output == null || !TryGetRenderPoseWindow(out var definition, out var frame, out var interpolationT))
		{
			return;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(definition.SliceMetadata.Length, num + Math.Max(0, frame.Count));
		for (int i = num; i < num2; i++)
		{
			int layerId = definition.SliceMetadata[i].LayerId;
			if (selectedLayerIds.Contains(layerId) && (layerId < 0 || layerId >= _layerVisible.Count || _layerVisible[layerId]) && AdobeAnimateDefinitionCache.TryGetInterpolatedSlicePose(definition, i, interpolationT, out var mediaId, out var transform, out var alpha) && mediaId != 65535)
			{
				transform = transform.Translated(offset);
				transform.Origin -= anchorOrigin;
				BlendLayerElementFromPreviousClip(definition, frame, i, anchorOrigin, ref transform, ref alpha);
				output.Add(new Godot.Collections.Array
				{
					mediaId,
					transform,
					new Color(1f, 1f, 1f, alpha)
				});
			}
		}
	}

	internal int AppendInterpolatedLayerElementsForRender(HashSet<int> selectedLayerIds, Vector2 anchorOrigin, IAdobeAnimateInterpolatedElementSink sink)
	{
		if (selectedLayerIds == null || selectedLayerIds.Count == 0 || sink == null || !TryGetRenderPoseWindow(out var definition, out var frame, out var interpolationT))
		{
			return 0;
		}
		int num = 0;
		int num2 = Math.Max(0, frame.Offset);
		int num3 = Math.Min(definition.SliceMetadata.Length, num2 + Math.Max(0, frame.Count));
		for (int i = num2; i < num3; i++)
		{
			int layerId = definition.SliceMetadata[i].LayerId;
			if (selectedLayerIds.Contains(layerId) && (layerId < 0 || layerId >= _layerVisible.Count || _layerVisible[layerId]) && AdobeAnimateDefinitionCache.TryGetInterpolatedSlicePose(definition, i, interpolationT, out var mediaId, out var transform, out var alpha) && mediaId != 65535)
			{
				transform = transform.Translated(offset);
				transform.Origin -= anchorOrigin;
				BlendLayerElementFromPreviousClip(definition, frame, i, anchorOrigin, ref transform, ref alpha);
				if (sink.TryAppendInterpolatedElement(definition, this, mediaId, transform, new Color(1f, 1f, 1f, alpha)))
				{
					num++;
				}
			}
		}
		return num;
	}

	internal bool TryCreateVisibleFrameElementsForRender(out AdobeAnimateData animeData, out Array<Texture2D> mediaReplace, out Array<string> mediaReplaceAtlasPaths, out Array<Godot.Collections.Array> elements)
	{
		animeData = _flashAnimeData;
		mediaReplace = null;
		mediaReplaceAtlasPaths = null;
		elements = null;
		if (!GodotObject.IsInstanceValid(animeData) || _layerVisible == null || _layerVisible.Count == 0)
		{
			return false;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < _layerVisible.Count; i++)
		{
			if (_layerVisible[i])
			{
				hashSet.Add(i);
			}
		}
		if (hashSet.Count == 0)
		{
			return false;
		}
		mediaReplace = CreateActiveMediaReplaceSnapshotForRender();
		mediaReplaceAtlasPaths = CreateActiveMediaReplaceAtlasPathSnapshotForRender();
		elements = new Array<Godot.Collections.Array>();
		AppendInterpolatedLayerElementsForRender(hashSet, Vector2.Zero, elements);
		return elements.Count > 0;
	}

	private bool TryGetRenderPoseWindow(out AdobeAnimateRuntimeDefinition definition, out PackedFrame frame, out float interpolationT)
	{
		definition = GetRuntimeDefinition();
		frame = default(PackedFrame);
		interpolationT = 0f;
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0 || definition.SliceMetadata == null)
		{
			return false;
		}
		float num = GetRenderFrameFloat();
		if (clipRange != Vector2I.Zero)
		{
			num = Mathf.Clamp(num, clipRange.X, Math.Max(clipRange.X, clipRange.Y - 1));
		}
		int num2 = Mathf.Clamp(Mathf.FloorToInt(num), 0, definition.Frames.Length - 1);
		interpolationT = Mathf.Clamp(num - Mathf.Floor(num), 0f, 1f);
		frame = definition.Frames[num2];
		return true;
	}

	internal Array<Texture2D> CreateActiveMediaReplaceSnapshotForRender()
	{
		Array<Texture2D> array = _mediaReplace?.Duplicate() ?? new Array<Texture2D>();
		for (int i = 0; i < array.Count; i++)
		{
			if (_mediaReplaceUse == null || i >= _mediaReplaceUse.Count || !_mediaReplaceUse[i] || !GodotObject.IsInstanceValid(array[i]))
			{
				array[i] = null;
			}
		}
		return array;
	}

	internal Array<string> CreateActiveMediaReplaceAtlasPathSnapshotForRender()
	{
		Array<string> array = _mediaReplaceAtlasPaths?.Duplicate() ?? new Array<string>();
		for (int i = 0; i < array.Count; i++)
		{
			if (_mediaReplaceUse == null || i >= _mediaReplaceUse.Count || !_mediaReplaceUse[i] || string.IsNullOrWhiteSpace(array[i]))
			{
				array[i] = string.Empty;
			}
		}
		return array;
	}

	internal Texture2D GetActiveMediaReplacementForRender(int mediaId)
	{
		if (mediaId < 0 || _mediaReplaceUse == null || mediaId >= _mediaReplaceUse.Count || !_mediaReplaceUse[mediaId] || _mediaReplace == null || mediaId >= _mediaReplace.Count || !GodotObject.IsInstanceValid(_mediaReplace[mediaId]))
		{
			return null;
		}
		return _mediaReplace[mediaId];
	}

	internal string GetActiveMediaReplacementAtlasPathForRender(int mediaId)
	{
		if (mediaId < 0 || _mediaReplaceUse == null || mediaId >= _mediaReplaceUse.Count || !_mediaReplaceUse[mediaId] || _mediaReplaceAtlasPaths == null || mediaId >= _mediaReplaceAtlasPaths.Count || string.IsNullOrWhiteSpace(_mediaReplaceAtlasPaths[mediaId]))
		{
			return string.Empty;
		}
		return _mediaReplaceAtlasPaths[mediaId];
	}

	internal Vector2 GetMediaSizeForRender(int mediaId)
	{
		if (mediaId >= 0 && _mediaReplaceUse != null && mediaId < _mediaReplaceUse.Count && _mediaReplaceUse[mediaId] && _mediaReplace != null && mediaId < _mediaReplace.Count && GodotObject.IsInstanceValid(_mediaReplace[mediaId]))
		{
			Vector2 size = _mediaReplace[mediaId].GetSize();
			if (size.X > 0f && size.Y > 0f)
			{
				return size;
			}
		}
		if (mediaId >= 0 && _mediaReplaceUse != null && mediaId < _mediaReplaceUse.Count && _mediaReplaceUse[mediaId] && mediaId < _mediaReplaceAtlasPaths.Count && !string.IsNullOrWhiteSpace(_mediaReplaceAtlasPaths[mediaId]) && AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(_mediaReplaceAtlasPaths[mediaId], out var allocation) && allocation.Rect.Size.X > 0f && allocation.Rect.Size.Y > 0f)
		{
			return allocation.Rect.Size;
		}
		if (_flashAnimeData == null)
		{
			return Vector2.Zero;
		}
		return _flashAnimeData.GetMediaRect(mediaId).Size;
	}

	internal bool TryGetFrameSlotPose(int frame, int slotLayerId, out int mediaId, out Transform2D transform)
	{
		if (slotLayerId < 0)
		{
			mediaId = 65535;
			transform = Transform2D.Identity;
			return false;
		}
		if (TryGetFrameLayerPose(frame, slotLayerId, out mediaId, out transform))
		{
			return true;
		}
		if (ShouldFallbackSlotPoseToDrawOrder(slotLayerId))
		{
			return TryGetFrameDrawOrderPose(frame, slotLayerId, out mediaId, out transform);
		}
		return false;
	}

	private bool TryGetFrameLayerDrawOrder(int frame, int layerId, out int drawOrder)
	{
		drawOrder = -1;
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null || runtimeDefinition.Frames == null || runtimeDefinition.SliceMetadata == null || runtimeDefinition.Frames.Length == 0 || layerId < 0)
		{
			return false;
		}
		int num = Mathf.Clamp(frame, 0, runtimeDefinition.Frames.Length - 1);
		PackedFrame frame2 = runtimeDefinition.Frames[num];
		if (AdobeAnimateDefinitionCache.TryGetFrameLayerMaxDrawOrder(runtimeDefinition, num, frame2, layerId, out drawOrder))
		{
			return true;
		}
		int num2 = frame2.Offset + frame2.Count;
		for (int i = frame2.Offset; i < num2; i++)
		{
			PackedSliceMetadata packedSliceMetadata = runtimeDefinition.SliceMetadata[i];
			if (packedSliceMetadata.LayerId == layerId)
			{
				drawOrder = Math.Max(drawOrder, packedSliceMetadata.DrawOrder);
			}
		}
		return drawOrder >= 0;
	}

	private bool TryGetInterpolatedLayerPose(int layerId, out int mediaId, out Transform2D transform)
	{
		return TryGetInterpolatedPose(layerId, useLayerId: true, out mediaId, out transform);
	}

	internal bool TryGetInterpolatedSlotPose(int slotLayerId, out int mediaId, out Transform2D transform)
	{
		if (slotLayerId < 0)
		{
			mediaId = 65535;
			transform = Transform2D.Identity;
			return false;
		}
		if (TryGetInterpolatedLayerPose(slotLayerId, out mediaId, out transform))
		{
			return true;
		}
		if (ShouldFallbackSlotPoseToDrawOrder(slotLayerId))
		{
			return TryGetInterpolatedDrawOrderPose(slotLayerId, out mediaId, out transform);
		}
		return false;
	}

	private bool TryGetInterpolatedPose(int key, bool useLayerId, out int mediaId, out Transform2D transform)
	{
		mediaId = 65535;
		transform = Transform2D.Identity;
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null || runtimeDefinition.Frames == null || runtimeDefinition.Frames.Length == 0)
		{
			return false;
		}
		if (!TryGetCachedManagedPoseTrack(runtimeDefinition, key, useLayerId, out var array))
		{
			return false;
		}
		if (!TryGetPoseAtFrameFloat(runtimeDefinition, array, GetRenderFrameFloat(), out mediaId, out transform))
		{
			return false;
		}
		BlendPoseTransformFromPreviousClip(runtimeDefinition, array, ref transform);
		return true;
	}

	private bool TryGetCachedManagedPoseTrack(AdobeAnimateRuntimeDefinition definition, int key, bool useLayerId, out PackedCpuPoseSample[] track)
	{
		if (_cachedManagedPoseTrackDefinition == definition && _cachedManagedPoseTrackKey == key && _cachedManagedPoseTrackUseLayerId == useLayerId && _cachedManagedPoseTrack.Length == definition.Frames.Length)
		{
			track = _cachedManagedPoseTrack;
			return true;
		}
		if (!AdobeAnimateDefinitionCache.TryGetCpuPoseTrack(definition, key, useLayerId, out track))
		{
			return false;
		}
		_cachedManagedPoseTrackDefinition = definition;
		_cachedManagedPoseTrackKey = key;
		_cachedManagedPoseTrackUseLayerId = useLayerId;
		_cachedManagedPoseTrack = track;
		return true;
	}

	private void ResetManagedPoseTrackCache()
	{
		_cachedManagedPoseTrackDefinition = null;
		_cachedManagedPoseTrackKey = -1;
		_cachedManagedPoseTrackUseLayerId = false;
		_cachedManagedPoseTrack = System.Array.Empty<PackedCpuPoseSample>();
	}

	private static bool TryGetPoseAtFrameFloat(AdobeAnimateRuntimeDefinition definition, PackedCpuPoseSample[] track, float frameFloat, out int mediaId, out Transform2D transform)
	{
		mediaId = 65535;
		transform = Transform2D.Identity;
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0 || track == null || track.Length != definition.Frames.Length)
		{
			return false;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt(frameFloat), 0, definition.Frames.Length - 1);
		ref PackedCpuPoseSample reference = ref track[num];
		if (!reference.Valid)
		{
			return false;
		}
		mediaId = reference.MediaId;
		Transform2D source = (transform = reference.Transform);
		float num2 = Mathf.Clamp(frameFloat - Mathf.Floor(frameFloat), 0f, 1f);
		int num3 = Mathf.Clamp(num + 1, 0, definition.Frames.Length - 1);
		if (num2 <= 0f || num3 == num || !track[num3].Valid || track[num3].MediaId != mediaId)
		{
			return true;
		}
		transform = LerpPoseTransform(source, track[num3].Transform, num2);
		return true;
	}

	private void BlendPoseTransformFromPreviousClip(AdobeAnimateRuntimeDefinition definition, PackedCpuPoseSample[] track, ref Transform2D targetTransform)
	{
		AdobeAnimateClipBlendState clipBlendStateForRender = GetClipBlendStateForRender();
		if (clipBlendStateForRender.Enabled && !(clipBlendStateForRender.Weight >= 1f) && TryGetPoseAtFrameFloat(definition, track, clipBlendStateForRender.FromFrameFloat, out var _, out var transform))
		{
			targetTransform = LerpPoseTransform(transform, targetTransform, clipBlendStateForRender.Weight);
		}
	}

	private void BlendLayerElementFromPreviousClip(AdobeAnimateRuntimeDefinition definition, PackedFrame targetFrame, int targetSourceIndex, Vector2 anchorOrigin, ref Transform2D targetTransform, ref float targetAlpha)
	{
		AdobeAnimateClipBlendState clipBlendStateForRender = GetClipBlendStateForRender();
		if (!clipBlendStateForRender.Enabled || clipBlendStateForRender.Weight >= 1f || definition?.Frames == null || definition.Frames.Length == 0 || targetSourceIndex < 0)
		{
			return;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt(clipBlendStateForRender.FromFrameFloat), 0, definition.Frames.Length - 1);
		PackedFrame sourceFrame = definition.Frames[num];
		if (AdobeAnimateDefinitionCache.TryGetMatchingSliceIndexInFrame(definition, sourceFrame, targetFrame, targetSourceIndex, out var sourceAbsoluteIndex))
		{
			float interpolationT = Mathf.Clamp(clipBlendStateForRender.FromFrameFloat - Mathf.Floor(clipBlendStateForRender.FromFrameFloat), 0f, 1f);
			if (AdobeAnimateDefinitionCache.TryGetInterpolatedSlicePose(definition, sourceAbsoluteIndex, interpolationT, out var mediaId, out var transform, out var alpha) && mediaId != 65535)
			{
				transform = transform.Translated(offset);
				transform.Origin -= anchorOrigin;
				targetTransform = LerpPoseTransform(transform, targetTransform, clipBlendStateForRender.Weight);
				targetAlpha = Mathf.Lerp(alpha, targetAlpha, clipBlendStateForRender.Weight);
			}
		}
	}

	private static Transform2D LerpPoseTransform(Transform2D source, Transform2D target, float weight)
	{
		float num = Mathf.Clamp(weight, 0f, 1f);
		return new Transform2D(source.X + (target.X - source.X) * num, source.Y + (target.Y - source.Y) * num, source.Origin + (target.Origin - source.Origin) * num);
	}

	private bool TryGetFramePose(int frame, int key, bool useLayerId, out int mediaId, out Transform2D transform)
	{
		mediaId = 65535;
		transform = Transform2D.Identity;
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null || runtimeDefinition.Frames == null || runtimeDefinition.SliceMetadata == null || runtimeDefinition.Frames.Length == 0 || key < 0)
		{
			return false;
		}
		return TryGetFramePose(runtimeDefinition, frame, key, useLayerId, out mediaId, out transform);
	}

	private static bool TryGetFramePose(AdobeAnimateRuntimeDefinition definition, int frame, int key, bool useLayerId, out int mediaId, out Transform2D transform)
	{
		mediaId = 65535;
		transform = Transform2D.Identity;
		if (definition == null || definition.Frames == null || definition.SliceMetadata == null || definition.Frames.Length == 0 || key < 0)
		{
			return false;
		}
		return AdobeAnimateDefinitionCache.TryGetCpuPoseSample(definition, Mathf.Clamp(frame, 0, definition.Frames.Length - 1), key, useLayerId, out mediaId, out transform);
	}

	private static Transform2D CreateSliceTransform(PackedSlicePose slice)
	{
		return new Transform2D(new Vector2(slice.Xx, slice.Xy), new Vector2(slice.Yx, slice.Yy), new Vector2(slice.Ox, slice.Oy));
	}

	private bool HasInsertedSpritesForRender()
	{
		for (int i = 0; i < _insertedSprites.Count; i++)
		{
			AdobeAnimateSprite sprite = _insertedSprites[i].sprite;
			if (GodotObject.IsInstanceValid(sprite) && sprite._parentSprite == this)
			{
				return true;
			}
		}
		return false;
	}

	internal bool HasSpriteChildrenForRender()
	{
		if (_managedSlotSpritesDirty)
		{
			RefreshManagedSlotSpriteOwnerCache();
		}
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			if (GodotObject.IsInstanceValid(_spriteChildren[i]))
			{
				return true;
			}
		}
		return false;
	}

	private bool NeedsDrawOrderSortBandsForRender()
	{
		if (!HasInsertedSpritesForRender() && !HasManagedSlotSpritesForRender())
		{
			return HasSpriteChildrenForRender();
		}
		return true;
	}

	private bool GetNeedsDrawOrderSortBandsForRender()
	{
		if (_drawOrderSortBandCacheDirty || _managedSlotSpritesDirty)
		{
			_needsDrawOrderSortBandsCached = NeedsDrawOrderSortBandsForRender();
			_drawOrderSortBandCacheDirty = false;
		}
		return _needsDrawOrderSortBandsCached;
	}

	internal bool HasManagedSlotSpritesForRender()
	{
		return GetManagedSlotSpritesForRender().Length != 0;
	}

	internal AdobeAnimateManagedSlotSprite[] GetManagedSlotSpritesForRender()
	{
		if (!_managedSlotSpritesDirty)
		{
			return _managedSlotSprites;
		}
		RefreshManagedSlotSpriteOwnerCache();
		List<AdobeAnimateManagedSlotSprite> list = new List<AdobeAnimateManagedSlotSprite>();
		for (int i = 0; i < _slotChildren.Length; i++)
		{
			AdobeAnimateSlot adobeAnimateSlot = _slotChildren[i];
			if (adobeAnimateSlot != null && GodotObject.IsInstanceValid(adobeAnimateSlot))
			{
				adobeAnimateSlot.TryCollectManagedSprite2DChildren(list);
			}
		}
		_managedSlotSprites = ((list.Count > 0) ? list.ToArray() : System.Array.Empty<AdobeAnimateManagedSlotSprite>());
		RefreshManagedSlotGpuStateWatchers(_managedSlotSprites);
		ResetManagedSlotGpuStateCache();
		_managedSlotSpritesDirty = false;
		return _managedSlotSprites;
	}

	internal AdobeAnimateSprite[] GetSpriteChildrenForRender()
	{
		if (_managedSlotSpritesDirty)
		{
			RefreshManagedSlotSpriteOwnerCache();
		}
		return _spriteChildren ?? System.Array.Empty<AdobeAnimateSprite>();
	}

	internal bool IsEmptyHiddenGpuGraphPlaceholderForRender()
	{
		if (_flashAnimeData == null && !base.Visible && GetChildCount() == 0)
		{
			return !HasExternalVisualsForRender();
		}
		return false;
	}

	internal void PrepareDetachedGpuGraphWarmup()
	{
		if (!GodotObject.IsInstanceValid(this) || IsInsideTree())
		{
			return;
		}
		ApplyPendingFlashAnimeDataChange();
		TryHydrateFlashAnimeDataBeforeFileChange();
		if (HasUsableFlashAnimeData())
		{
			if (HasAnyRequestedMediaReplace())
			{
				UpdateMediaReplaceData();
			}
			parentSprite = FindParentSpriteAncestor();
			CacheChildren();
			RefreshRenderStaticStateCache(force: true);
			GetRuntimeDefinition();
		}
	}

	private void RefreshManagedSlotSpriteOwnerCache()
	{
		if (GodotObject.IsInstanceValid(this))
		{
			List<AdobeAnimateSlot> list = new List<AdobeAnimateSlot>();
			List<AdobeAnimateSprite> list2 = new List<AdobeAnimateSprite>();
			List<AdobeAnimateSlot> list3 = new List<AdobeAnimateSlot>();
			CollectOwnedChildren(this, list, list2, list3, collectSpriteChildren: true, null);
			if (!SameSlotChildren(_slotChildren, list))
			{
				_slotChildren = list.ToArray();
				MarkSlotRuntimeUpdateCacheDirty();
			}
			if (!SameSpriteChildren(_spriteChildren, list2) || !SameSpriteChildOwnerSlots(_spriteChildOwnerSlots, list3))
			{
				_spriteChildren = list2.ToArray();
				_spriteChildOwnerSlots = list3.ToArray();
				_drawOrderSortBandCacheDirty = true;
			}
			_hasChildren = _slotChildren.Length != 0 || _spriteChildren.Length != 0;
			RefreshRuntimeChildUpdatePresence();
		}
	}

	private static bool SameSlotChildren(AdobeAnimateSlot[] cached, List<AdobeAnimateSlot> current)
	{
		if (cached == null || cached.Length != current.Count)
		{
			return false;
		}
		for (int i = 0; i < cached.Length; i++)
		{
			if (cached[i] != current[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool SameSpriteChildren(AdobeAnimateSprite[] cached, List<AdobeAnimateSprite> current)
	{
		if (cached == null || cached.Length != current.Count)
		{
			return false;
		}
		for (int i = 0; i < cached.Length; i++)
		{
			if (cached[i] != current[i])
			{
				return false;
			}
		}
		return true;
	}

	private static bool SameSpriteChildOwnerSlots(AdobeAnimateSlot[] cached, List<AdobeAnimateSlot> current)
	{
		if (cached == null || cached.Length != current.Count)
		{
			return false;
		}
		for (int i = 0; i < cached.Length; i++)
		{
			if (cached[i] != current[i])
			{
				return false;
			}
		}
		return true;
	}

	internal void MarkManagedSlotSpriteCacheDirty()
	{
		InvalidateRuntimeNativeCanvasSuppressionEligibility();
		_managedSlotSpritesDirty = true;
		DisconnectManagedSlotGpuStateWatchers();
		ResetManagedSlotGpuStateCache();
		_drawOrderSortBandCacheDirty = true;
		AdobeAnimateRenderManager.InvalidateGpuRenderGraph(GetRenderSortRootForRender(), AdobeAnimateGpuGraphInvalidationReason.ManagedSlotTopology);
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	internal bool TryBuildManagedSlotGpuStateForRender(int managedSpriteIndex, Rid expectedAtlasArrayRid, out AdobeAnimateGpuManagedVisualState state, out string failureReason)
	{
		state = default(AdobeAnimateGpuManagedVisualState);
		failureReason = "";
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = GetManagedSlotSpritesForRender();
		if ((uint)managedSpriteIndex >= (uint)managedSlotSpritesForRender.Length)
		{
			failureReason = $"managed Slot visual index {managedSpriteIndex} is out of range";
			return false;
		}
		EnsureManagedSlotGpuStateCache(managedSlotSpritesForRender.Length);
		AdobeAnimateManagedSlotSprite pair = managedSlotSpritesForRender[managedSpriteIndex];
		ulong num = AdobeAnimateManagedSprite2D.BuildGpuStateFastSignature(in pair);
		bool flag = _managedSlotGpuStateAtlasRids[managedSpriteIndex] == expectedAtlasArrayRid;
		if (_managedSlotGpuStateKnown[managedSpriteIndex] && flag && _managedSlotGpuStateFastSignatures[managedSpriteIndex] == num && _managedSlotGpuStateRescanCountdowns[managedSpriteIndex] > 0)
		{
			_managedSlotGpuStateRescanCountdowns[managedSpriteIndex]--;
			state = _managedSlotGpuStates[managedSpriteIndex];
			if (!_managedSlotGpuStateValid[managedSpriteIndex])
			{
				failureReason = "cached managed Slot visual GPU state is invalid";
			}
			return _managedSlotGpuStateValid[managedSpriteIndex];
		}
		ulong num2 = AdobeAnimateManagedSprite2D.BuildGpuStateSignature(in pair);
		if (_managedSlotGpuStateKnown[managedSpriteIndex] && _managedSlotGpuStateSignatures[managedSpriteIndex] == num2 && flag)
		{
			_managedSlotGpuStateFastSignatures[managedSpriteIndex] = num;
			_managedSlotGpuStateRescanCountdowns[managedSpriteIndex] = GetManagedSlotGpuStateRescanInterval(managedSpriteIndex);
			state = _managedSlotGpuStates[managedSpriteIndex];
			if (!_managedSlotGpuStateValid[managedSpriteIndex])
			{
				failureReason = "cached managed Slot visual GPU state is invalid";
			}
			return _managedSlotGpuStateValid[managedSpriteIndex];
		}
		bool num3 = AdobeAnimateManagedSprite2D.TryBuildGpuState(in pair, expectedAtlasArrayRid, out state, out failureReason);
		if (num3)
		{
			_managedSlotGpuStateKnown[managedSpriteIndex] = true;
			_managedSlotGpuStateValid[managedSpriteIndex] = true;
			_managedSlotGpuStateSignatures[managedSpriteIndex] = num2;
			_managedSlotGpuStateFastSignatures[managedSpriteIndex] = num;
			_managedSlotGpuStateRescanCountdowns[managedSpriteIndex] = GetManagedSlotGpuStateRescanInterval(managedSpriteIndex);
			_managedSlotGpuStateAtlasRids[managedSpriteIndex] = expectedAtlasArrayRid;
			_managedSlotGpuStates[managedSpriteIndex] = state;
			return num3;
		}
		_managedSlotGpuStateKnown[managedSpriteIndex] = false;
		_managedSlotGpuStateRescanCountdowns[managedSpriteIndex] = 0;
		return num3;
	}

	private void EnsureManagedSlotGpuStateCache(int count)
	{
		if (_managedSlotGpuStates.Length != count)
		{
			_managedSlotGpuStateSignatures = new ulong[count];
			_managedSlotGpuStateAtlasRids = new Rid[count];
			_managedSlotGpuStates = new AdobeAnimateGpuManagedVisualState[count];
			_managedSlotGpuStateFastSignatures = new ulong[count];
			_managedSlotGpuStateRescanCountdowns = new int[count];
			_managedSlotGpuStateKnown = new bool[count];
			_managedSlotGpuStateValid = new bool[count];
		}
	}

	private void ResetManagedSlotGpuStateCache()
	{
		_managedSlotGpuStateSignatures = System.Array.Empty<ulong>();
		_managedSlotGpuStateAtlasRids = System.Array.Empty<Rid>();
		_managedSlotGpuStates = System.Array.Empty<AdobeAnimateGpuManagedVisualState>();
		_managedSlotGpuStateFastSignatures = System.Array.Empty<ulong>();
		_managedSlotGpuStateRescanCountdowns = System.Array.Empty<int>();
		_managedSlotGpuStateKnown = System.Array.Empty<bool>();
		_managedSlotGpuStateValid = System.Array.Empty<bool>();
	}

	internal void MarkManagedSlotVisualStateChanged()
	{
		if (_managedSlotGpuStateKnown.Length != 0)
		{
			System.Array.Fill(_managedSlotGpuStateKnown, value: false);
		}
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	private int GetManagedSlotGpuStateRescanInterval(int managedSpriteIndex)
	{
		return GetStaggeredRescanInterval(30) + managedSpriteIndex % 30;
	}

	private void RefreshManagedSlotGpuStateWatchers(AdobeAnimateManagedSlotSprite[] managedSprites)
	{
		DisconnectManagedSlotGpuStateWatchers();
		if (managedSprites == null || managedSprites.Length == 0)
		{
			return;
		}
		List<Sprite2D> list = new List<Sprite2D>(managedSprites.Length);
		List<AdobeAnimatePart> list2 = new List<AdobeAnimatePart>(managedSprites.Length);
		for (int i = 0; i < managedSprites.Length; i++)
		{
			Sprite2D sprite = managedSprites[i].Sprite;
			if (GodotObject.IsInstanceValid(sprite))
			{
				sprite.TextureChanged += MarkManagedSlotVisualStateChanged;
				sprite.FrameChanged += MarkManagedSlotVisualStateChanged;
				sprite.ItemRectChanged += MarkManagedSlotVisualStateChanged;
				sprite.VisibilityChanged += MarkManagedSlotVisualStateChanged;
				list.Add(sprite);
				continue;
			}
			AdobeAnimatePart atlasPart = managedSprites[i].AtlasPart;
			if (GodotObject.IsInstanceValid(atlasPart))
			{
				atlasPart.ItemRectChanged += MarkManagedSlotVisualStateChanged;
				atlasPart.VisibilityChanged += MarkManagedSlotVisualStateChanged;
				list2.Add(atlasPart);
			}
		}
		_managedSlotGpuStateWatchedSprites = ((list.Count > 0) ? list.ToArray() : System.Array.Empty<Sprite2D>());
		_managedSlotGpuStateWatchedParts = ((list2.Count > 0) ? list2.ToArray() : System.Array.Empty<AdobeAnimatePart>());
	}

	private void DisconnectManagedSlotGpuStateWatchers()
	{
		for (int i = 0; i < _managedSlotGpuStateWatchedSprites.Length; i++)
		{
			Sprite2D sprite2D = _managedSlotGpuStateWatchedSprites[i];
			if (GodotObject.IsInstanceValid(sprite2D))
			{
				sprite2D.TextureChanged -= MarkManagedSlotVisualStateChanged;
				sprite2D.FrameChanged -= MarkManagedSlotVisualStateChanged;
				sprite2D.ItemRectChanged -= MarkManagedSlotVisualStateChanged;
				sprite2D.VisibilityChanged -= MarkManagedSlotVisualStateChanged;
			}
		}
		_managedSlotGpuStateWatchedSprites = System.Array.Empty<Sprite2D>();
		for (int j = 0; j < _managedSlotGpuStateWatchedParts.Length; j++)
		{
			AdobeAnimatePart adobeAnimatePart = _managedSlotGpuStateWatchedParts[j];
			if (GodotObject.IsInstanceValid(adobeAnimatePart))
			{
				adobeAnimatePart.ItemRectChanged -= MarkManagedSlotVisualStateChanged;
				adobeAnimatePart.VisibilityChanged -= MarkManagedSlotVisualStateChanged;
			}
		}
		_managedSlotGpuStateWatchedParts = System.Array.Empty<AdobeAnimatePart>();
	}

	internal void RefreshManagedSlotSpriteCacheForRender()
	{
		MarkManagedSlotSpriteCacheDirty();
		GetManagedSlotSpritesForRender();
	}

	internal AdobeAnimateSprite GetRenderSortRootForRender()
	{
		AdobeAnimateSprite adobeAnimateSprite = this;
		AdobeAnimateSprite adobeAnimateSprite2 = _parentSprite;
		int num = 0;
		while (GodotObject.IsInstanceValid(adobeAnimateSprite2) && adobeAnimateSprite2.OwnsSpriteChildForRender(adobeAnimateSprite) && num++ < 64)
		{
			adobeAnimateSprite = adobeAnimateSprite2;
			adobeAnimateSprite2 = adobeAnimateSprite2._parentSprite;
		}
		return adobeAnimateSprite;
	}

	private bool OwnsSpriteChildForRender(AdobeAnimateSprite child)
	{
		if (!GodotObject.IsInstanceValid(child) || child._parentSprite != this)
		{
			return false;
		}
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			if (_spriteChildren[i] == child)
			{
				return true;
			}
		}
		for (int j = 0; j < _insertedSprites.Count; j++)
		{
			if (_insertedSprites[j].sprite == child)
			{
				return true;
			}
		}
		return false;
	}

	private AdobeAnimateSprite FindParentSpriteAncestor()
	{
		Node parent = GetParent();
		int num = 0;
		while (GodotObject.IsInstanceValid(parent) && num++ < 64)
		{
			if (parent is AdobeAnimateSprite result)
			{
				return result;
			}
			parent = parent.GetParent();
		}
		return null;
	}

	internal bool TryGetManagedSlotTransformForRender(AdobeAnimateSlot slot, out Transform2D transform)
	{
		transform = Transform2D.Identity;
		if (slot == null || !GodotObject.IsInstanceValid(slot) || slot.mode != 0)
		{
			return false;
		}
		int slotLayerId = slot.followSlotId - 1;
		if (!TryGetInterpolatedSlotPoseFast(slotLayerId, out var mediaId, out var transform2) || mediaId == 65535 || transform2 == Transform2D.Identity)
		{
			return false;
		}
		Transform2D transform2D = transform2.Translated(offset).TranslatedLocal(slot.offset);
		if (!slot.useRotate)
		{
			transform2D = transform2D.RotatedLocal(0f - transform2D.Rotation);
		}
		if (!slot.useScale)
		{
			Vector2 scale = transform2D.Scale;
			if (scale.X != 0f && scale.Y != 0f)
			{
				transform2D = transform2D.ScaledLocal(Vector2.One / scale);
			}
		}
		if (!slot.useSkew)
		{
			transform2D = new Transform2D(transform2D.Rotation, transform2D.Scale, 0f, transform2D.Origin);
		}
		transform = transform2D;
		return true;
	}

	internal bool TryGetManagedSlotPositionForRender(AdobeAnimateSlot slot, out Vector2 position)
	{
		position = Vector2.Zero;
		if (slot == null || !GodotObject.IsInstanceValid(slot) || slot.mode != 0)
		{
			return false;
		}
		int slotLayerId = slot.followSlotId - 1;
		if (!TryGetInterpolatedSlotPoseFast(slotLayerId, out var mediaId, out var transform) || mediaId == 65535 || transform == Transform2D.Identity)
		{
			return false;
		}
		position = transform.Translated(offset).TranslatedLocal(slot.offset).Origin;
		return true;
	}

	internal bool TryResolveLayerIdForRender(StringName layerName, out int layerId)
	{
		layerId = -1;
		if (layerName == null || layerName.IsEmpty || _flashAnimeData?.layerDictionary == null || !_flashAnimeData.layerDictionary.ContainsKey(layerName))
		{
			return false;
		}
		layerId = _flashAnimeData.layerDictionary[layerName].AsInt32();
		return layerId >= 0;
	}

	internal bool TryGetManagedLayerPositionForRender(int layerId, Vector2 localOffset, out Vector2 position)
	{
		position = Vector2.Zero;
		if (!TryGetInterpolatedSlotPoseFast(layerId, out var mediaId, out var transform) || mediaId == 65535 || transform == Transform2D.Identity)
		{
			return false;
		}
		position = transform.Translated(offset).TranslatedLocal(localOffset).Origin;
		return true;
	}

	private bool TryGetInterpolatedSlotPoseFast(int slotLayerId, out int mediaId, out Transform2D transform)
	{
		if (slotLayerId < 0)
		{
			mediaId = 65535;
			transform = Transform2D.Identity;
			return false;
		}
		if (TryGetInterpolatedLayerPose(slotLayerId, out mediaId, out transform))
		{
			return true;
		}
		if (ShouldFallbackSlotPoseToDrawOrder(slotLayerId))
		{
			return TryGetInterpolatedDrawOrderPoseFast(slotLayerId, out mediaId, out transform);
		}
		return false;
	}

	private bool ShouldFallbackSlotPoseToDrawOrder(int slotLayerId)
	{
		if (slotLayerId < 0)
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition != null)
		{
			return slotLayerId >= runtimeDefinition.RuntimeLayerCount;
		}
		return !LayerDictionaryContainsId(slotLayerId);
	}

	private bool LayerDictionaryContainsId(int layerId)
	{
		Dictionary dictionary = _flashAnimeData?.layerDictionary;
		if (dictionary == null || dictionary.Count == 0)
		{
			return false;
		}
		foreach (Variant key in dictionary.Keys)
		{
			if (dictionary[key].AsInt32() == layerId)
			{
				return true;
			}
		}
		return false;
	}

	private bool TryGetInterpolatedDrawOrderPoseFast(int drawOrder, out int mediaId, out Transform2D transform)
	{
		return TryGetInterpolatedPose(drawOrder, useLayerId: false, out mediaId, out transform);
	}

	internal bool TryGetFrameDrawOrderForRender(int frame, int layerId, out int drawOrder)
	{
		return TryGetFrameLayerDrawOrder(frame, layerId, out drawOrder);
	}

	internal static long ResolveLayerSortBand(long parentSortBand, int drawOrder, long offset = 0L)
	{
		int num = Math.Max(0, drawOrder);
		long num2 = ((parentSortBand == 0L) ? 1000000 : 1);
		return parentSortBand + num * num2 + offset;
	}

	internal static long ResolveManagedSpriteLayerSortBand(long parentSortBand, int drawOrder, long characterSortBand)
	{
		int num = Math.Max(0, drawOrder);
		long num2 = ((parentSortBand == 0L) ? 1000000 : 1);
		return characterSortBand + parentSortBand + num * num2 + 1;
	}

	private long ResolveChildSortBand(int layerId)
	{
		if (!TryGetFrameLayerDrawOrder(frameIndex, layerId, out var drawOrder))
		{
			drawOrder = Math.Max(0, layerId);
		}
		return ResolveLayerSortBand(_renderSortBand, drawOrder, 1L);
	}

	private bool TryGetInsertedLayerForChild(AdobeAnimateSprite child, out int layerId)
	{
		if (!GodotObject.IsInstanceValid(child) || child._parentSprite != this)
		{
			layerId = -1;
			return false;
		}
		for (int i = 0; i < _insertedSprites.Count; i++)
		{
			InsertedSprite insertedSprite = _insertedSprites[i];
			if (insertedSprite.sprite == child)
			{
				layerId = insertedSprite.layerId;
				return true;
			}
		}
		layerId = -1;
		return false;
	}

	internal bool TryGetChildRenderLayerForRender(AdobeAnimateSprite child, out int layerId)
	{
		if (TryGetInsertedLayerForChild(child, out layerId))
		{
			return true;
		}
		if (TryGetTrackedChildRenderLayerForRender(child, out layerId))
		{
			return layerId >= 0;
		}
		if (GodotObject.IsInstanceValid(child) && child._parentSprite == this)
		{
			layerId = ResolveSpriteChildInsertLayer(child);
			return layerId >= 0;
		}
		layerId = -1;
		return false;
	}

	private bool TryGetTrackedChildRenderLayerForRender(AdobeAnimateSprite child, out int layerId)
	{
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			if (_spriteChildren[i] == child)
			{
				int layerId2;
				bool isInserted = TryGetInsertedLayerForChild(child, out layerId2);
				layerId = ResolveSpriteChildRenderLayer(child, i, isInserted, layerId2);
				return true;
			}
		}
		layerId = -1;
		return false;
	}

	private bool IsSpriteChildOwnedBySlot(int index)
	{
		if (index >= 0 && index < _spriteChildOwnerSlots.Length)
		{
			return GodotObject.IsInstanceValid(_spriteChildOwnerSlots[index]);
		}
		return false;
	}

	private int ResolveSpriteChildRenderLayer(AdobeAnimateSprite child, int childIndex, bool isInserted, int insertedLayerId)
	{
		if (isInserted)
		{
			return insertedLayerId;
		}
		return ResolveSpriteChildInsertLayer(child);
	}

	private int ResolveSpriteChildInsertLayer(AdobeAnimateSprite child)
	{
		if (!GodotObject.IsInstanceValid(child))
		{
			return -1;
		}
		if (child.insertLayerId >= 0)
		{
			return child.insertLayerId;
		}
		return ResolveTopInsertLayerId();
	}

	private int ResolveTopInsertLayerId()
	{
		int layerVisibleCountForRender = GetLayerVisibleCountForRender();
		if (layerVisibleCountForRender <= 0)
		{
			return -1;
		}
		return layerVisibleCountForRender;
	}

	private int ResolveSpriteChildFollowLayer(AdobeAnimateSprite child, int childIndex)
	{
		if (!GodotObject.IsInstanceValid(child))
		{
			return -1;
		}
		if (IsSpriteChildOwnedBySlot(childIndex))
		{
			return _spriteChildOwnerSlots[childIndex].followSlotId - 1;
		}
		return child.followParentSpriteLayerId;
	}

	private void ApplyChildRenderSort(AdobeAnimateSprite child, int layerId)
	{
		if (child != null && child.IsRuntimeInsideTree)
		{
			long num = ResolveChildSortBand(layerId);
			if (child._renderSortBand != num)
			{
				child._renderSortBand = num;
				child.InvalidateRenderSnapshotCache();
				child.RequestNodeRedraw();
			}
		}
	}

	private static void ClearInsertedChildRenderSort(AdobeAnimateSprite child)
	{
		if (child != null && child.IsRuntimeInsideTree && child._renderSortBand != 0L)
		{
			child._renderSortBand = 0L;
			child.InvalidateRenderSnapshotCache();
			child.RequestNodeRedraw();
		}
	}

	private void ResolveRenderPresentationForRender(Node renderMountParent, out Color modulate, out int effectiveZIndex)
	{
		modulate = GetEffectiveRenderModulate(renderMountParent);
		effectiveZIndex = GetEffectiveZIndexForRender();
	}

	private void ApplyKnownGlobalTranslationForRender(Vector2 globalDelta, ulong physicsFrame)
	{
		if (!_renderGlobalTransformCached || _renderGlobalTransformDirty)
		{
			MarkRenderTransformDirty(changedInPhysicsFrame: true);
			return;
		}
		Transform2D renderGlobalTransform = _renderGlobalTransform;
		Transform2D transform2D = renderGlobalTransform;
		transform2D.Origin += globalDelta;
		bool flag = _renderRootMotionEnabled && physicsFrame == _renderGlobalTransformPhysicsFrame;
		Transform2D transform2D2 = (flag ? _renderPreviousGlobalTransform : renderGlobalTransform);
		bool flag2 = CanInterpolateRenderRootMotion() && (flag || physicsFrame != _renderGlobalTransformPhysicsFrame) && IsContinuousRenderRootMotion(transform2D2, transform2D);
		_renderPreviousGlobalTransform = (flag2 ? transform2D2 : transform2D);
		_renderGlobalTransform = transform2D;
		_renderRootMotionEnabled = flag2;
		_renderGlobalTransformPhysicsFrame = physicsFrame;
		_renderTransformChangedInPhysicsFrame = false;
		_runtimePhysicsAncestorTransformPending = false;
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw(preferDisplayCadence: true, transformOnly: true);
		_knownAncestorTranslationNotificationPending = true;
	}

	private bool TryConsumeKnownAncestorTranslationNotification()
	{
		if (!_knownAncestorTranslationNotificationPending)
		{
			return false;
		}
		_knownAncestorTranslationNotificationPending = false;
		return true;
	}

	internal void NotifySlotOwnedSpriteTransformChangedForRender(AdobeAnimateSlot ownerSlot)
	{
		if (!GodotObject.IsInstanceValid(ownerSlot))
		{
			return;
		}
		for (int i = 0; i < _spriteChildren.Length; i++)
		{
			if (i < _spriteChildOwnerSlots.Length && _spriteChildOwnerSlots[i] == ownerSlot)
			{
				AdobeAnimateSprite adobeAnimateSprite = _spriteChildren[i];
				if (GodotObject.IsInstanceValid(adobeAnimateSprite))
				{
					adobeAnimateSprite.NotifyAncestorTransformChangedForRender();
				}
			}
		}
	}

	private void NotifyLocalRenderTransformChangedForTree(bool changedInPhysicsFrame)
	{
		if (changedInPhysicsFrame)
		{
			_runtimePhysicsAncestorTransformPending = true;
		}
		MarkRenderTransformDirty(changedInPhysicsFrame);
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.NotifyLocalRenderTransformChangedForTree(changedInPhysicsFrame);
			}
		}
	}

	private Transform2D GetCachedGlobalTransformForRender()
	{
		_knownAncestorTranslationNotificationPending = false;
		if (!_renderGlobalTransformCached || _renderGlobalTransformDirty)
		{
			Transform2D globalTransform = base.GlobalTransform;
			ulong physicsFrameForRender = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
			bool flag = CanInterpolateRenderRootMotion() && _renderGlobalTransformCached && _renderTransformChangedInPhysicsFrame && physicsFrameForRender != _renderGlobalTransformPhysicsFrame && IsContinuousRenderRootMotion(_renderGlobalTransform, globalTransform);
			_renderPreviousGlobalTransform = (flag ? _renderGlobalTransform : globalTransform);
			_renderGlobalTransform = globalTransform;
			_renderRootMotionEnabled = flag;
			_renderGlobalTransformPhysicsFrame = physicsFrameForRender;
			_renderGlobalTransformCached = true;
			_renderGlobalTransformDirty = false;
			_renderTransformChangedInPhysicsFrame = false;
			_runtimePhysicsAncestorTransformPending = false;
		}
		return _renderGlobalTransform;
	}

	private static bool IsContinuousRenderRootMotion(Transform2D previous, Transform2D current)
	{
		if (Mathf.IsZeroApprox(previous.Determinant()) || Mathf.IsZeroApprox(current.Determinant()) || previous.Origin.DistanceSquaredTo(current.Origin) > 9216f)
		{
			return false;
		}
		if (previous.X.DistanceSquaredTo(current.X) <= 0.25f)
		{
			return previous.Y.DistanceSquaredTo(current.Y) <= 0.25f;
		}
		return false;
	}

	private AdobeAnimateRootMotionState BuildRootMotionStateForRender(Node renderMountParent, Transform2D currentRenderTransform)
	{
		int physicsTicksPerSecondForRender = AdobeAnimateRenderManager.GetPhysicsTicksPerSecondForRender();
		int frameLimitForRender = AdobeAnimateRenderManager.GetFrameLimitForRender();
		if (!CanInterpolateRenderRootMotion() || !HasCurrentPhysicsRootMotion() || (frameLimitForRender > 0 && frameLimitForRender <= physicsTicksPerSecondForRender) || Mathf.IsZeroApprox(currentRenderTransform.Determinant()))
		{
			return default(AdobeAnimateRootMotionState);
		}
		Transform2D transform2D = AdobeAnimateRenderManager.ToRenderMountLocalTransform(renderMountParent, _renderPreviousGlobalTransform);
		return new AdobeAnimateRootMotionState(currentRenderTransform.AffineInverse() * transform2D, enabled: true);
	}

	private void RefreshRenderMountCache()
	{
		Node cachedRenderMountParent = _cachedRenderMountParent;
		Node mountParent;
		Viewport viewport;
		if (_usingRuntimeManager)
		{
			AdobeAnimateRenderManager.ResolveRuntimeRenderMount(this, out mountParent, out _cachedCanvasLayer, out viewport);
		}
		else
		{
			AdobeAnimateRenderManager.ResolveRenderMount(this, out mountParent, out _cachedCanvasLayer);
			viewport = GetViewport();
		}
		_cachedRenderMountParent = ((_forceLocalRender && GodotObject.IsInstanceValid(_renderClipControl) && _renderClipControl.GetViewport() == viewport) ? _renderClipControl : mountParent);
		_cachedViewport = viewport;
		ScheduleNextRenderMountAudit();
		ResetEffectiveRenderModulateAncestorCache();
		if (cachedRenderMountParent != _cachedRenderMountParent)
		{
			InvalidateTreeOrderPathCache();
		}
		InvalidateRenderSnapshotCache();
	}

	private void ScheduleNextRenderMountAudit()
	{
		if (!_usingRuntimeManager)
		{
			_renderMountNextAuditPhysicsFrame = 0uL;
		}
		else
		{
			_renderMountNextAuditPhysicsFrame = AdobeAnimateRenderManager.GetPhysicsFrameForRender() + (ulong)GetStaggeredRescanInterval(60);
		}
	}

	private Node GetCachedRenderMountParent()
	{
		if (!_usingRuntimeManager)
		{
			if (!GodotObject.IsInstanceValid(_cachedRenderMountParent) || _cachedViewport != GetViewport())
			{
				RefreshRenderMountCache();
			}
			return _cachedRenderMountParent;
		}
		if (_cachedRenderMountParent == null || AdobeAnimateRenderManager.GetPhysicsFrameForRender() >= _renderMountNextAuditPhysicsFrame)
		{
			RefreshRenderMountCache();
		}
		return _cachedRenderMountParent;
	}

	private Color GetEffectiveRenderModulate(Node renderMountParent)
	{
		if (!_renderModulateAncestorCacheReady || _renderModulateAncestorRescanCountdown <= 0 || _renderModulateMountParent != renderMountParent)
		{
			RefreshEffectiveRenderModulateAncestorCache(GetParent(), renderMountParent);
		}
		else
		{
			_renderModulateAncestorRescanCountdown--;
		}
		Color result = MultiplyRenderColor(MultiplyRenderColor(GetCachedOwnRenderFrameModulate(), GetCachedOwnRenderFrameSelfModulate()), GetCachedEffectiveRenderAncestorModulate(renderMountParent));
		if (_renderGrayscale && result.A > 0f)
		{
			result.A = 0f - result.A;
		}
		return result;
	}

	private static Color GetCachedRenderFrameModulate(CanvasItem canvasItem)
	{
		if (CachedViewportWorldRectRenderScopeDepth <= 0)
		{
			return canvasItem.Modulate;
		}
		if (canvasItem is AdobeAnimateSprite adobeAnimateSprite)
		{
			return adobeAnimateSprite.GetCachedOwnRenderFrameModulate();
		}
		if (CachedRenderFrameLastModulateItem == canvasItem)
		{
			return CachedRenderFrameLastModulateValue;
		}
		if (!CachedRenderFrameModulates.TryGetValue(canvasItem, out var value))
		{
			value = canvasItem.Modulate;
			CachedRenderFrameModulates[canvasItem] = value;
		}
		CachedRenderFrameLastModulateItem = canvasItem;
		CachedRenderFrameLastModulateValue = value;
		return value;
	}

	private Color GetCachedOwnRenderFrameModulate()
	{
		if (CachedViewportWorldRectRenderScopeDepth <= 0)
		{
			return Modulate;
		}
		long cachedRenderColorTransactionVersion = CachedRenderColorTransactionVersion;
		if (_cachedOwnRenderFrameModulateVersion != cachedRenderColorTransactionVersion)
		{
			EnsureCachedRenderLocalModulates(cachedRenderColorTransactionVersion);
			_cachedOwnRenderFrameModulate = _cachedRenderLocalModulate;
			_cachedOwnRenderFrameModulateVersion = cachedRenderColorTransactionVersion;
		}
		return _cachedOwnRenderFrameModulate;
	}

	private Color GetCachedOwnRenderFrameSelfModulate()
	{
		if (CachedViewportWorldRectRenderScopeDepth <= 0)
		{
			return SelfModulate;
		}
		long cachedRenderColorTransactionVersion = CachedRenderColorTransactionVersion;
		if (_cachedOwnRenderFrameSelfModulateVersion != cachedRenderColorTransactionVersion)
		{
			EnsureCachedRenderLocalModulates(cachedRenderColorTransactionVersion);
			_cachedOwnRenderFrameSelfModulate = _cachedRenderLocalSelfModulate;
			_cachedOwnRenderFrameSelfModulateVersion = cachedRenderColorTransactionVersion;
		}
		return _cachedOwnRenderFrameSelfModulate;
	}

	private void EnsureCachedRenderLocalModulates(long transactionVersion)
	{
		if (_renderLocalModulateAuditTransactionVersion == transactionVersion)
		{
			return;
		}
		_renderLocalModulateAuditTransactionVersion = transactionVersion;
		if (_cachedRenderLocalModulateReady && _cachedRenderLocalSelfModulateReady && _renderLocalModulateRescanCountdown > 0)
		{
			_renderLocalModulateRescanCountdown--;
			return;
		}
		Color modulate = base.Modulate;
		Color selfModulate = base.SelfModulate;
		bool num = (_cachedRenderLocalModulateReady && _cachedRenderLocalModulate != modulate) || (_cachedRenderLocalSelfModulateReady && _cachedRenderLocalSelfModulate != selfModulate);
		_cachedRenderLocalModulate = modulate;
		_cachedRenderLocalSelfModulate = selfModulate;
		_cachedRenderLocalModulateReady = true;
		_cachedRenderLocalSelfModulateReady = true;
		ScheduleNextRenderLocalModulateAudit();
		if (num)
		{
			_effectOnceBatchVisualRevision++;
			MarkEffectOnceBatchEligibilityChanged();
		}
	}

	private void ScheduleNextRenderLocalModulateAudit()
	{
		_renderLocalModulateRescanCountdown = GetStaggeredRescanInterval(60);
	}

	private void RefreshEffectiveRenderModulateAncestorCache(Node directParent, Node renderMountParent)
	{
		_renderModulateAncestors.Clear();
		Node node = directParent;
		while (GodotObject.IsInstanceValid(node) && node != renderMountParent)
		{
			if (node is CanvasItem item)
			{
				_renderModulateAncestors.Add(item);
			}
			node = node.GetParent();
		}
		_renderModulateMountParent = renderMountParent;
		_renderModulateAncestorCacheReady = true;
		_renderModulateAncestorRescanCountdown = GetStaggeredRescanInterval(60);
	}

	private void ResetEffectiveRenderModulateAncestorCache()
	{
		_renderModulateAncestors.Clear();
		_renderModulateMountParent = null;
		_renderModulateAncestorCacheReady = false;
		_renderModulateAncestorRescanCountdown = 0;
	}

	private static Color MultiplyRenderColor(in Color left, in Color right)
	{
		return new Color(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
	}

	private bool IsInSubViewportRenderTarget()
	{
		return GetViewport() is SubViewport;
	}

	private bool ShouldUseGlobalRuntimeManager()
	{
		if (AdobeAnimateRuntimeManager.UseRuntimeManager && !CachedEditorHint && !IsInSubViewportRenderTarget())
		{
			return !_forceLocalRender;
		}
		return false;
	}

	internal void CommitRuntimeNativeCanvasTakeover(long frameVersion)
	{
		_runtimeNativeCanvasPublishedFrameVersion = frameVersion;
		ReleaseRuntimeManagerFirstFrameHandoffTree();
		if (!_runtimeNativeCanvasSuppressed && _usingRuntimeManager && _runtimeInsideTree && !_forceLocalRender && CanSuppressRuntimeNativeCanvasTree())
		{
			uint visibilityLayer = base.VisibilityLayer;
			if (visibilityLayer != 0)
			{
				_runtimeNativeCanvasVisibilityLayer = visibilityLayer;
				base.VisibilityLayer = 0u;
				_runtimeNativeCanvasSuppressed = true;
			}
		}
	}

	internal bool RecoverRuntimeNativeCanvasAfterMissedTransaction(long frameVersion)
	{
		if (!_runtimeNativeCanvasSuppressed || _runtimeNativeCanvasPublishedFrameVersion == frameVersion)
		{
			return false;
		}
		RestoreRuntimeNativeCanvasLayer();
		RequestRuntimeRenderSubmissionRetry();
		return true;
	}

	internal void RestoreRuntimeNativeCanvasLayer()
	{
		if (_runtimeNativeCanvasSuppressed)
		{
			uint runtimeNativeCanvasVisibilityLayer = _runtimeNativeCanvasVisibilityLayer;
			_runtimeNativeCanvasSuppressed = false;
			base.VisibilityLayer = runtimeNativeCanvasVisibilityLayer;
		}
	}

	private void InvalidateRuntimeNativeCanvasSuppressionEligibility()
	{
		_runtimeNativeCanvasSuppressionEligibilityKnown = false;
		_runtimeNativeCanvasSuppressionEligible = false;
		RestoreRuntimeNativeCanvasLayer();
	}

	private bool CanSuppressRuntimeNativeCanvasTree()
	{
		if (_runtimeNativeCanvasSuppressionEligibilityKnown)
		{
			return _runtimeNativeCanvasSuppressionEligible;
		}
		_runtimeNativeCanvasSuppressionEligible = IsRuntimeNativeCanvasBranchCrowdManaged(this);
		_runtimeNativeCanvasSuppressionEligibilityKnown = true;
		return _runtimeNativeCanvasSuppressionEligible;
	}

	private static bool IsRuntimeNativeCanvasBranchCrowdManaged(Node parent)
	{
		int childCount = parent.GetChildCount();
		for (int i = 0; i < childCount; i++)
		{
			Node child = parent.GetChild(i);
			if (child is CanvasItem canvasItem && (!(canvasItem is Node2D node2D) || !node2D.IsSetAsTopLevel()))
			{
				bool num = canvasItem.GetType() == typeof(Node2D) || canvasItem is AdobeAnimateSlot;
				bool flag = canvasItem is Sprite2D && AdobeAnimateManagedSprite2D.IsCrowdManaged(canvasItem);
				bool flag2 = canvasItem is AdobeAnimateSprite adobeAnimateSprite && adobeAnimateSprite._usingRuntimeManager;
				if (!num && !flag && !flag2)
				{
					return false;
				}
				if (!IsRuntimeNativeCanvasBranchCrowdManaged(child))
				{
					return false;
				}
			}
		}
		return true;
	}

	private AdobeAnimateRuntimeDefinition GetRuntimeDefinition()
	{
		if (_runtimeDefinition == null || _runtimeDefinition.Source != _flashAnimeData || _runtimeDefinition.AtlasProfile != _atlasProfileOverride)
		{
			AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = ((_atlasProfileOverride == null) ? AdobeAnimateDefinitionCache.GetOrBuild(_flashAnimeData) : AdobeAnimateDefinitionCache.GetOrBuild(_flashAnimeData, _atlasProfileOverride));
			if (_runtimeDefinition != adobeAnimateRuntimeDefinition)
			{
				ResetManagedPoseTrackCache();
			}
			_runtimeDefinition = adobeAnimateRuntimeDefinition;
			meshTexture = _runtimeDefinition?.BaseAtlasRid ?? default(Rid);
		}
		return _runtimeDefinition;
	}

	internal bool TryBuildOrderedFallbackSnapshotFromCrowdState(AdobeAnimateCrowdRenderState state, out AdobeAnimateRenderSnapshot snapshot, out bool culled)
	{
		snapshot = default(AdobeAnimateRenderSnapshot);
		culled = false;
		if (state == null || _effectOnceGpuSuppressed || _flashAnimeData == null || !_isVisibleInTree || !HasRenderableAtlas(state.Definition))
		{
			return false;
		}
		if (_usingRuntimeManager && !IsOrderedFallbackRenderBoundsVisibleFromCrowdCache(state.Definition, state.RenderMountParent))
		{
			culled = true;
			return false;
		}
		snapshot = new AdobeAnimateRenderSnapshot(this, _flashAnimeData, state.Definition, _clip, clipRange, GetRenderFrameFloat(), state.GlobalTransform, state.Offset, state.Modulate, _layerVisible, _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _cachedLayerVisibleValueCount, _cachedLayerVisibleValues, mediaReplaceAtlas, mediaReplaceRect, _mediaReplaceUse, _cachedHasMediaReplace, _cachedMediaReplaceLimit, _cachedMediaReplaceUseMask, _cachedMediaReplaceUseMaskOverflow, mediaReplaceAtlasPages, _cachedMediaReplaceAtlasPageCount, _cachedMediaReplaceUseValues, _cachedMediaReplaceRects, _cachedMediaReplaceAtlasPageValues, mediaReplaceAtlasArray, mediaReplaceAtlasArraySize, mediaReplaceAtlasUsesTextureArray, state.VerticalClip, state.RenderMountParent, _cachedCanvasLayer, state.EffectiveZIndex, System.Array.Empty<int>(), NeedsDrawItemSortForRender(), GetClipBlendStateForRender());
		_needsRenderSubmission = false;
		return true;
	}

	private bool IsOrderedFallbackRenderBoundsVisibleFromCrowdCache(AdobeAnimateRuntimeDefinition definition, Node renderMountParent)
	{
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		if (_runtimeRenderBoundsCached && _cachedRuntimeRenderBoundsCullingEnabled == _runtimeViewportCullingEnabled && _cachedRuntimeRenderBoundsDefinition == definition && _cachedRuntimeRenderBoundsTransform == cachedGlobalTransformForRender && _cachedRuntimeRenderBoundsOffset == offset && _cachedRuntimeRenderBoundsMountParent == renderMountParent)
		{
			return _cachedRuntimeRenderBoundsStrictVisible;
		}
		return IsRenderBoundsVisible(definition, cachedGlobalTransformForRender, renderMountParent);
	}

	internal bool TryBuildRenderSnapshot(out AdobeAnimateRenderSnapshot snapshot, bool allowUnchanged = true)
	{
		bool culled;
		return TryBuildRenderSnapshot(out snapshot, out culled, allowUnchanged);
	}

	internal bool TryBuildRenderSnapshot(out AdobeAnimateRenderSnapshot snapshot, out bool culled, bool allowUnchanged = true)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		snapshot = default(AdobeAnimateRenderSnapshot);
		culled = false;
		if (_effectOnceGpuSuppressed || _flashAnimeData == null || !_isVisibleInTree)
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.snapshotBuild.skip", startTicks);
			return false;
		}
		if (needMediaReplaceUpdate)
		{
			UpdateMediaReplaceData();
		}
		RefreshRenderStaticStateCache();
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (!HasRenderableAtlas(runtimeDefinition))
		{
			TowerDefensePerfProfiler.End("adobeAnimate.render.snapshotBuild.noDefinition", startTicks);
			return false;
		}
		Node cachedRenderMountParent = GetCachedRenderMountParent();
		Transform2D cachedGlobalTransformForRender = GetCachedGlobalTransformForRender();
		Transform2D globalTransform = AdobeAnimateRenderManager.ToRenderMountLocalTransform(cachedRenderMountParent, cachedGlobalTransformForRender);
		if (_usingRuntimeManager && !IsRenderBoundsVisible(runtimeDefinition, cachedGlobalTransformForRender, cachedRenderMountParent))
		{
			culled = true;
			TowerDefensePerfProfiler.End("adobeAnimate.render.snapshotBuild.culled", startTicks);
			return false;
		}
		bool needsDrawItemSort = NeedsDrawItemSortForRender();
		ResolveRenderPresentationForRender(cachedRenderMountParent, out var modulate, out var effectiveZIndex);
		int[] treeOrderPath = ((_usingRuntimeManager && !CachedEditorHint) ? System.Array.Empty<int>() : GetEffectiveTreeOrderPathForRender());
		snapshot = new AdobeAnimateRenderSnapshot(this, _flashAnimeData, runtimeDefinition, _clip, clipRange, GetRenderFrameFloat(), globalTransform, offset, modulate, _layerVisible, _cachedAllLayersVisible, _cachedCanUseLayerMask, _cachedLayerMask, _cachedLayerVisibleValueCount, _cachedLayerVisibleValues, mediaReplaceAtlas, mediaReplaceRect, _mediaReplaceUse, _cachedHasMediaReplace, _cachedMediaReplaceLimit, _cachedMediaReplaceUseMask, _cachedMediaReplaceUseMaskOverflow, mediaReplaceAtlasPages, _cachedMediaReplaceAtlasPageCount, _cachedMediaReplaceUseValues, _cachedMediaReplaceRects, _cachedMediaReplaceAtlasPageValues, mediaReplaceAtlasArray, mediaReplaceAtlasArraySize, mediaReplaceAtlasUsesTextureArray, _verticalClip, cachedRenderMountParent, _cachedCanvasLayer, effectiveZIndex, treeOrderPath, needsDrawItemSort, GetClipBlendStateForRender());
		_needsRenderSubmission = false;
		TowerDefensePerfProfiler.End("adobeAnimate.render.snapshotBuild", startTicks, 1);
		return true;
	}

	internal bool NeedsDrawItemSortForRender()
	{
		bool flag = RequiresInternalDrawItemSortForRender();
		if (_usingRuntimeManager && !CachedEditorHint && !flag)
		{
			return false;
		}
		return flag;
	}

	private bool RequiresInternalDrawItemSortForRender()
	{
		if (!GetNeedsDrawOrderSortBandsForRender() && GetSpriteChildrenForRender().Length == 0)
		{
			return HasManagedSlotSpritesForRender();
		}
		return true;
	}

	private bool IsRenderBoundsVisible(AdobeAnimateRuntimeDefinition definition, Transform2D globalTransform, Node renderMountParent, bool useDefinitionBounds = false, float viewportPadding = 0f)
	{
		if (!_runtimeViewportCullingEnabled || definition == null)
		{
			return true;
		}
		Viewport viewport = ResolveRenderViewport(renderMountParent);
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return true;
		}
		Rect2 rect = GetCachedViewportWorldRect(viewport);
		if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
		{
			return true;
		}
		if (viewportPadding > 0f)
		{
			rect = rect.Grow(viewportPadding);
		}
		Rect2 rect2 = ((useDefinitionBounds && IsValidBounds(definition.LocalBounds)) ? definition.LocalBounds : GetRenderLocalBounds(definition));
		rect2.Position += offset;
		Rect2 b = TransformRect(globalTransform, rect2);
		return rect.Intersects(b, includeBorders: true);
	}

	private Viewport ResolveRenderViewport(Node renderMountParent)
	{
		if (GodotObject.IsInstanceValid(renderMountParent) && renderMountParent is Viewport result)
		{
			return result;
		}
		if (GodotObject.IsInstanceValid(_cachedViewport))
		{
			return _cachedViewport;
		}
		_cachedViewport = GetViewport();
		return _cachedViewport;
	}

	private bool IsRuntimeCrowdRenderBoundsVisible(AdobeAnimateRuntimeDefinition definition, Transform2D globalTransform, Node renderMountParent)
	{
		if (definition == null)
		{
			return true;
		}
		Viewport viewport = ResolveRenderViewport(renderMountParent);
		Rect2 rect = (GodotObject.IsInstanceValid(viewport) ? GetCachedViewportWorldRect(viewport) : default(Rect2));
		bool flag = _runtimeRenderBoundsCached && _cachedRuntimeRenderBoundsCullingEnabled == _runtimeViewportCullingEnabled && _cachedRuntimeRenderBoundsDefinition == definition && _cachedRuntimeRenderBoundsOffset == offset && _cachedRuntimeRenderBoundsMountParent == renderMountParent && _cachedRuntimeRenderBoundsViewport == viewport && _cachedRuntimeRenderBoundsViewportWorldRect == rect;
		if (flag && _runtimeRenderBoundsRescanCountdown > 0 && _cachedRuntimeRenderBoundsTransform == globalTransform)
		{
			_runtimeRenderBoundsRescanCountdown--;
			return _cachedRuntimeRenderBoundsVisible;
		}
		bool cachedRuntimeRenderBoundsHasRelativeRect = false;
		Rect2 rect2 = default(Rect2);
		bool flag2 = true;
		bool cachedRuntimeRenderBoundsStrictVisible = true;
		if (_runtimeViewportCullingEnabled && GodotObject.IsInstanceValid(viewport) && rect.Size.X > 0f && rect.Size.Y > 0f)
		{
			Rect2 rect3;
			if (flag && _cachedRuntimeRenderBoundsHasRelativeRect && _cachedRuntimeRenderBoundsTransform.X == globalTransform.X && _cachedRuntimeRenderBoundsTransform.Y == globalTransform.Y)
			{
				rect2 = _cachedRuntimeRenderBoundsRelativeRect;
				rect3 = rect2;
				rect3.Position += globalTransform.Origin;
			}
			else
			{
				Rect2 rect4 = (IsValidBounds(definition.LocalBounds) ? definition.LocalBounds : GetRenderLocalBounds(definition));
				rect4.Position += offset;
				rect3 = TransformRect(globalTransform, rect4);
				rect2 = rect3;
				rect2.Position -= globalTransform.Origin;
			}
			cachedRuntimeRenderBoundsHasRelativeRect = true;
			cachedRuntimeRenderBoundsStrictVisible = rect.Intersects(rect3, includeBorders: true);
			flag2 = rect.Grow(256f).Intersects(rect3, includeBorders: true);
		}
		_runtimeRenderBoundsCached = true;
		_cachedRuntimeRenderBoundsVisible = flag2;
		_cachedRuntimeRenderBoundsStrictVisible = cachedRuntimeRenderBoundsStrictVisible;
		_cachedRuntimeRenderBoundsCullingEnabled = _runtimeViewportCullingEnabled;
		_cachedRuntimeRenderBoundsDefinition = definition;
		_cachedRuntimeRenderBoundsTransform = globalTransform;
		_cachedRuntimeRenderBoundsOffset = offset;
		_cachedRuntimeRenderBoundsMountParent = renderMountParent;
		_cachedRuntimeRenderBoundsViewport = viewport;
		_cachedRuntimeRenderBoundsViewportWorldRect = rect;
		_cachedRuntimeRenderBoundsRelativeRect = rect2;
		_cachedRuntimeRenderBoundsHasRelativeRect = cachedRuntimeRenderBoundsHasRelativeRect;
		_runtimeRenderBoundsRescanCountdown = GetStaggeredRescanInterval(12);
		return flag2;
	}

	private Rect2 GetRenderLocalBounds(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return new Rect2(new Vector2(-128f, -128f), new Vector2(256f, 256f));
		}
		Rect2 localBounds = definition.LocalBounds;
		Rect2[] frameLocalBounds = definition.FrameLocalBounds;
		if (frameLocalBounds == null || frameLocalBounds.Length == 0)
		{
			return localBounds;
		}
		int num = Mathf.Clamp(frameIndex, 0, frameLocalBounds.Length - 1);
		if (TryGetLoopTerminalAliasFrame(out var terminalFrame, out var aliasFrame) && num >= terminalFrame)
		{
			num = Mathf.Clamp(aliasFrame, 0, frameLocalBounds.Length - 1);
		}
		Rect2 rect = (IsValidBounds(frameLocalBounds[num]) ? frameLocalBounds[num] : localBounds);
		int num2 = Mathf.Clamp(num + ((!playBack) ? 1 : (-1)), 0, frameLocalBounds.Length - 1);
		if (num2 != num && IsValidBounds(frameLocalBounds[num2]))
		{
			rect = MergeBounds(rect, frameLocalBounds[num2]);
		}
		if (IsClipBlendActiveForRender())
		{
			int num3 = Mathf.Clamp(Mathf.FloorToInt(_blendFromFrameFloat), 0, frameLocalBounds.Length - 1);
			if (IsValidBounds(frameLocalBounds[num3]))
			{
				rect = MergeBounds(rect, frameLocalBounds[num3]);
			}
			int num4 = Mathf.Clamp(num3 + 1, 0, frameLocalBounds.Length - 1);
			if (num4 != num3 && IsValidBounds(frameLocalBounds[num4]))
			{
				rect = MergeBounds(rect, frameLocalBounds[num4]);
			}
		}
		if (!IsValidBounds(rect))
		{
			return localBounds;
		}
		return rect;
	}

	internal static void BeginViewportWorldRectRenderFrame(long frameVersion)
	{
		if (CachedViewportWorldRectRenderScopeDepth == 0)
		{
			CachedRenderColorTransactionVersion++;
			CachedRenderFrameModulates.Clear();
			ResetCachedRenderFrameEffectiveAncestorModulates();
			CachedRenderFrameLastModulateItem = null;
			CachedRenderFrameLastModulateValue = default(Color);
		}
		CachedViewportWorldRectRenderScopeDepth++;
		ResetCachedViewportWorldRectsForFrame(frameVersion);
	}

	internal static void EndViewportWorldRectRenderFrame()
	{
		if (CachedViewportWorldRectRenderScopeDepth > 0)
		{
			CachedViewportWorldRectRenderScopeDepth--;
		}
		if (CachedViewportWorldRectRenderScopeDepth == 0)
		{
			CachedRenderFrameModulates.Clear();
			ResetCachedRenderFrameEffectiveAncestorModulates();
			CachedRenderFrameLastModulateItem = null;
			CachedRenderFrameLastModulateValue = default(Color);
		}
	}

	private static void ResetCachedViewportWorldRectsForFrame(long frame)
	{
		if (CachedViewportWorldRectsFrame != frame)
		{
			CachedViewportWorldRectsFrame = frame;
			CachedViewportWorldRects.Clear();
			CachedViewportWorldRectLastViewport = null;
		}
	}

	private static Rect2 GetCachedViewportWorldRect(Viewport viewport)
	{
		if (CachedViewportWorldRectRenderScopeDepth <= 0)
		{
			ResetCachedViewportWorldRectsForFrame((long)Engine.GetProcessFrames());
		}
		if (CachedViewportWorldRectLastViewport == viewport)
		{
			return CachedViewportWorldRectLastValue;
		}
		ulong instanceId = viewport.GetInstanceId();
		if (CachedViewportWorldRects.TryGetValue(instanceId, out var value))
		{
			CachedViewportWorldRectLastViewport = viewport;
			CachedViewportWorldRectLastValue = value;
			return value;
		}
		Rect2 visibleRect = viewport.GetVisibleRect();
		if (visibleRect.Size.X <= 0f || visibleRect.Size.Y <= 0f)
		{
			CachedViewportWorldRects[instanceId] = visibleRect;
			CachedViewportWorldRectLastViewport = viewport;
			CachedViewportWorldRectLastValue = visibleRect;
			return visibleRect;
		}
		Transform2D transform2D = viewport.GetCanvasTransform().AffineInverse();
		Vector2 position = transform2D * visibleRect.Position;
		Vector2 to = transform2D * (visibleRect.Position + new Vector2(visibleRect.Size.X, 0f));
		Vector2 to2 = transform2D * (visibleRect.Position + visibleRect.Size);
		Vector2 to3 = transform2D * (visibleRect.Position + new Vector2(0f, visibleRect.Size.Y));
		Rect2 rect = new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
		CachedViewportWorldRects[instanceId] = rect;
		CachedViewportWorldRectLastViewport = viewport;
		CachedViewportWorldRectLastValue = rect;
		return rect;
	}

	private static bool IsValidBounds(Rect2 bounds)
	{
		if (bounds.Size.X > 0f)
		{
			return bounds.Size.Y > 0f;
		}
		return false;
	}

	private static Rect2 MergeBounds(Rect2 left, Rect2 right)
	{
		if (!IsValidBounds(left))
		{
			return right;
		}
		if (!IsValidBounds(right))
		{
			return left;
		}
		Vector2 position = right.Position;
		Vector2 to = right.Position + right.Size;
		return left.Expand(position).Expand(new Vector2(to.X, position.Y)).Expand(to)
			.Expand(new Vector2(position.X, to.Y))
			.Abs();
	}

	private static Rect2 TransformRect(Transform2D transform, Rect2 rect)
	{
		Vector2 position = transform * rect.Position;
		Vector2 to = transform * (rect.Position + new Vector2(rect.Size.X, 0f));
		Vector2 to2 = transform * (rect.Position + rect.Size);
		Vector2 to3 = transform * (rect.Position + new Vector2(0f, rect.Size.Y));
		return new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3)
			.Abs();
	}

	private float GetRenderFrameFloat()
	{
		if (TryGetLoopTerminalAliasFrame(out var terminalFrame, out var aliasFrame) && frameIndex >= terminalFrame)
		{
			return aliasFrame;
		}
		return (float)frameIndex + (float)elapsedTimer;
	}

	private bool IsClipBlendActiveForRender()
	{
		if (blend)
		{
			return blendTime > 0.0;
		}
		return false;
	}

	private AdobeAnimateClipBlendState GetClipBlendStateForRender()
	{
		if (!IsClipBlendActiveForRender())
		{
			return default(AdobeAnimateClipBlendState);
		}
		return new AdobeAnimateClipBlendState(enabled: true, _blendFromFrameFloat, (float)(blendTimer / blendTime));
	}

	private int GetPlaybackClipEndExclusive()
	{
		if (_playbackClipEndCacheHasValue && _playbackClipEndCacheDefinition == _runtimeDefinition && _playbackClipEndCacheRange == clipRange && _playbackClipEndCacheLoop == loop && _playbackClipEndCachePlayBack == playBack)
		{
			return _playbackClipEndCacheValue;
		}
		int terminalFrame;
		int aliasFrame;
		int num = (TryGetLoopTerminalAliasFrame(out terminalFrame, out aliasFrame) ? Math.Max(clipRange.X + 1, terminalFrame) : clipRange.Y);
		_playbackClipEndCacheDefinition = _runtimeDefinition;
		_playbackClipEndCacheRange = clipRange;
		_playbackClipEndCacheLoop = loop;
		_playbackClipEndCachePlayBack = playBack;
		_playbackClipEndCacheValue = num;
		_playbackClipEndCacheHasValue = true;
		return num;
	}

	private bool TryGetLoopTerminalAliasFrame(out int terminalFrame, out int aliasFrame)
	{
		terminalFrame = clipRange.Y - 1;
		aliasFrame = clipRange.X;
		if (loop && deduplicateLoopTerminalFrame && !playBack && clipRange.Y - clipRange.X > 1)
		{
			return DoesLoopTerminalFrameAliasFirstFrame(terminalFrame, aliasFrame);
		}
		return false;
	}

	private bool DoesLoopTerminalFrameAliasFirstFrame(int terminalFrame, int aliasFrame)
	{
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition == null)
		{
			return false;
		}
		if (_loopTerminalAliasCacheHasValue && _loopTerminalAliasCacheDefinition == runtimeDefinition && _loopTerminalAliasCacheRange == clipRange)
		{
			return _loopTerminalAliasCacheValue;
		}
		bool flag = FramesHaveMatchingPose(runtimeDefinition, terminalFrame, aliasFrame);
		_loopTerminalAliasCacheDefinition = runtimeDefinition;
		_loopTerminalAliasCacheRange = clipRange;
		_loopTerminalAliasCacheHasValue = true;
		_loopTerminalAliasCacheValue = flag;
		return flag;
	}

	private static bool FramesHaveMatchingPose(AdobeAnimateRuntimeDefinition definition, int leftFrame, int rightFrame)
	{
		if (definition == null || definition.Frames == null)
		{
			return false;
		}
		if (leftFrame < 0 || rightFrame < 0 || leftFrame >= definition.Frames.Length || rightFrame >= definition.Frames.Length)
		{
			return false;
		}
		if (definition.FramePoseSignatures != null && leftFrame < definition.FramePoseSignatures.Length && rightFrame < definition.FramePoseSignatures.Length)
		{
			return definition.FramePoseSignatures[leftFrame] == definition.FramePoseSignatures[rightFrame];
		}
		if (!AdobeAnimateDefinitionCache.EnsureCpuPoseData(definition))
		{
			return false;
		}
		PackedFrame packedFrame = definition.Frames[leftFrame];
		PackedFrame packedFrame2 = definition.Frames[rightFrame];
		if (packedFrame.Count != packedFrame2.Count || packedFrame.Offset < 0 || packedFrame2.Offset < 0)
		{
			return false;
		}
		if (packedFrame.Offset + packedFrame.Count > definition.Slices.Length || packedFrame2.Offset + packedFrame2.Count > definition.Slices.Length)
		{
			return false;
		}
		for (int i = 0; i < packedFrame.Count; i++)
		{
			if (!PackedSlicePoseMatches(definition.Slices[packedFrame.Offset + i], definition.Slices[packedFrame2.Offset + i]))
			{
				return false;
			}
		}
		return true;
	}

	private static bool PackedSlicePoseMatches(PackedSlicePose left, PackedSlicePose right)
	{
		if (left.SliceKey == right.SliceKey && left.MediaId == right.MediaId && left.LayerId == right.LayerId && left.DrawOrder == right.DrawOrder && left.Flags == right.Flags && Mathf.IsEqualApprox(left.Xx, right.Xx) && Mathf.IsEqualApprox(left.Xy, right.Xy) && Mathf.IsEqualApprox(left.Yx, right.Yx) && Mathf.IsEqualApprox(left.Yy, right.Yy) && Mathf.IsEqualApprox(left.Ox, right.Ox) && Mathf.IsEqualApprox(left.Oy, right.Oy))
		{
			return Mathf.IsEqualApprox(left.Alpha, right.Alpha);
		}
		return false;
	}

	private void CacheRenderSnapshotState(float frameFloat, Transform2D globalTransform, Color modulate, Node renderMountParent, int effectiveZIndex, long sortBand, bool useDrawOrderSortBands)
	{
		_cachedClip = _clip;
		_cachedClipRange = clipRange;
		_cachedFrameFloat = frameFloat;
		_cachedGlobalTransform = globalTransform;
		_cachedModulate = modulate;
		_cachedOffset = offset;
		_cachedMediaReplaceAtlas = mediaReplaceAtlas;
		_cachedMediaReplaceAtlasArray = mediaReplaceAtlasArray;
		_cachedMediaReplaceAtlasRid = ((mediaReplaceAtlasUsesTextureArray && GodotObject.IsInstanceValid(mediaReplaceAtlasArray)) ? mediaReplaceAtlasArray.GetRid() : (GodotObject.IsInstanceValid(mediaReplaceAtlas) ? mediaReplaceAtlas.GetRid() : default(Rid)));
		_cachedMediaReplaceAtlasArrayRid = (GodotObject.IsInstanceValid(mediaReplaceAtlasArray) ? mediaReplaceAtlasArray.GetRid() : default(Rid));
		_cachedMediaReplaceAtlasShared = mediaReplaceAtlasShared;
		_cachedMediaReplaceAtlasUsesTextureArray = mediaReplaceAtlasUsesTextureArray;
		_cachedRenderMountParentSubmit = renderMountParent;
		_cachedCanvasLayerSubmit = _cachedCanvasLayer;
		_cachedZIndex = ZIndex;
		_cachedEffectiveZIndex = effectiveZIndex;
		_cachedSortBand = sortBand;
		_cachedUseDrawOrderSortBands = useDrawOrderSortBands;
		_cachedRefreshEveryFrame = refreshEveryFrame;
		_cachedVerticalClip = _verticalClip;
		_needsRenderSubmission = false;
		_renderStateCached = true;
	}

	private bool RefreshRenderStaticStateCache(bool force = false)
	{
		ulong physicsFrameForRender = AdobeAnimateRenderManager.GetPhysicsFrameForRender();
		bool flag = _cachedLayerStateVersion != _layerStateVersion || _cachedMediaReplaceStateVersion != _mediaReplaceStateVersion;
		bool flag2 = ShouldAuditRenderStaticArrays() && physicsFrameForRender >= _renderStaticStateNextAuditPhysicsFrame;
		if (!(force || !_renderStaticStateCached || flag || flag2))
		{
			return false;
		}
		if (!force && _renderStaticStateCached && !flag && flag2 && RenderStaticArrayAuditSnapshotsMatch())
		{
			ScheduleNextRenderStaticStateAudit(physicsFrameForRender);
			return false;
		}
		int cachedLayerVisibleCount = _cachedLayerVisibleCount;
		ulong cachedLayerMask = _cachedLayerMask;
		bool cachedAllLayersVisible = _cachedAllLayersVisible;
		bool cachedCanUseLayerMask = _cachedCanUseLayerMask;
		ulong cachedLayerVisibleSignature = _cachedLayerVisibleSignature;
		int cachedMediaReplaceLimit = _cachedMediaReplaceLimit;
		bool cachedHasMediaReplace = _cachedHasMediaReplace;
		bool cachedAnyRequestedMediaReplace = _cachedAnyRequestedMediaReplace;
		ulong cachedMediaReplaceSignature = _cachedMediaReplaceSignature;
		float cachedMediaReplaceBoundsGrow = _cachedMediaReplaceBoundsGrow;
		Rid cachedMediaReplaceAtlasRid = _cachedMediaReplaceAtlasRid;
		Array<bool> array = _layerVisible;
		int num = (_cachedLayerVisibleValueCount = array?.Count ?? 0);
		if (_cachedLayerVisibleValues.Length != num)
		{
			System.Array.Resize(ref _cachedLayerVisibleValues, num);
		}
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		_cachedLayerVisibleCount = GetRuntimeLayerVisibleCount(num, runtimeDefinition);
		_cachedLayerVisibleSignature = ComputeLayerVisibilitySignature(array, num);
		RefreshLayerActiveInCurrentClipCache(_cachedLayerVisibleCount, runtimeDefinition);
		_cachedLayerStateVersion = _layerStateVersion;
		_cachedAllLayersVisible = true;
		_cachedConfiguredLayersAllVisible = true;
		_cachedCanUseLayerMask = true;
		_cachedLayerMask = 0uL;
		if (_cachedLayerVisibleCount > 0)
		{
			for (int i = 0; i < _cachedLayerVisibleCount; i++)
			{
				bool flag3 = i >= num || array[i];
				if (i < num)
				{
					_cachedLayerVisibleValues[i] = flag3;
				}
				_cachedConfiguredLayersAllVisible &= flag3;
				_cachedAllLayersVisible &= flag3;
				if (i >= 64)
				{
					_cachedCanUseLayerMask = false;
				}
				else if (flag3)
				{
					_cachedLayerMask |= (ulong)(1L << i);
				}
			}
		}
		Array<bool> array2 = _mediaReplaceUse;
		int num2 = array2?.Count ?? 0;
		_cachedAnyRequestedMediaReplace = false;
		_cachedMediaReplaceUseMask = 0uL;
		_cachedMediaReplaceUseMaskOverflow = false;
		for (int j = 0; j < num2; j++)
		{
			if (array2[j])
			{
				_cachedAnyRequestedMediaReplace = true;
				if (j < 64)
				{
					_cachedMediaReplaceUseMask |= (ulong)(1L << j);
				}
				else
				{
					_cachedMediaReplaceUseMaskOverflow = true;
				}
			}
		}
		Array<Texture2D> array3 = _mediaReplace;
		int textureCount = array3?.Count ?? 0;
		Array<Rect2> array4 = mediaReplaceRect;
		int num3 = array4?.Count ?? 0;
		Array<int> array5 = mediaReplaceAtlasPages;
		int num4 = (_cachedMediaReplaceAtlasPageCount = array5?.Count ?? 0);
		if (_cachedMediaReplaceUseValues.Length != num2)
		{
			System.Array.Resize(ref _cachedMediaReplaceUseValues, num2);
		}
		if (_cachedMediaReplaceRects.Length != num3)
		{
			System.Array.Resize(ref _cachedMediaReplaceRects, num3);
		}
		if (_cachedMediaReplaceAtlasPageValues.Length != num4)
		{
			System.Array.Resize(ref _cachedMediaReplaceAtlasPageValues, num4);
		}
		for (int k = 0; k < num2; k++)
		{
			_cachedMediaReplaceUseValues[k] = array2[k];
		}
		for (int l = 0; l < num3; l++)
		{
			_cachedMediaReplaceRects[l] = array4[l];
		}
		for (int m = 0; m < num4; m++)
		{
			_cachedMediaReplaceAtlasPageValues[m] = array5[m];
		}
		if (HasValidMediaReplaceAtlas())
		{
			_cachedMediaReplaceAtlasRid = GetMediaReplaceAtlasRid();
			_cachedMediaReplaceAtlasArrayRid = (GodotObject.IsInstanceValid(mediaReplaceAtlasArray) ? mediaReplaceAtlasArray.GetRid() : default(Rid));
			_cachedMediaReplaceLimit = Math.Min(num2, num3);
			_cachedHasMediaReplace = HasActiveMediaReplace(array2, num2, _cachedMediaReplaceLimit);
			_cachedMediaReplaceSignature = BuildMediaReplaceSignature(array3, textureCount, array2, num2, array4, num3, _cachedMediaReplaceLimit, array5, num4, mediaReplaceAtlasUsesTextureArray);
			_cachedMediaReplaceBoundsGrow = ComputeMediaReplaceBoundsGrow(array2, num2, array4, num3, _cachedMediaReplaceLimit);
		}
		else
		{
			_cachedMediaReplaceAtlasRid = default(Rid);
			_cachedMediaReplaceAtlasArrayRid = default(Rid);
			_cachedMediaReplaceLimit = 0;
			_cachedHasMediaReplace = false;
			_cachedMediaReplaceSignature = 0uL;
			_cachedMediaReplaceBoundsGrow = 0f;
		}
		_cachedMediaReplaceStateVersion = _mediaReplaceStateVersion;
		_renderStaticStateCached = true;
		CaptureRenderStaticArrayAuditSnapshots();
		ScheduleNextRenderStaticStateAudit(physicsFrameForRender);
		if (!force && cachedLayerVisibleCount == _cachedLayerVisibleCount && cachedLayerMask == _cachedLayerMask && cachedAllLayersVisible == _cachedAllLayersVisible && cachedCanUseLayerMask == _cachedCanUseLayerMask && cachedLayerVisibleSignature == _cachedLayerVisibleSignature && cachedMediaReplaceLimit == _cachedMediaReplaceLimit && cachedHasMediaReplace == _cachedHasMediaReplace && cachedAnyRequestedMediaReplace == _cachedAnyRequestedMediaReplace && cachedMediaReplaceSignature == _cachedMediaReplaceSignature && !(cachedMediaReplaceAtlasRid != _cachedMediaReplaceAtlasRid))
		{
			return !Mathf.IsEqualApprox(cachedMediaReplaceBoundsGrow, _cachedMediaReplaceBoundsGrow);
		}
		return true;
	}

	private bool RenderStaticArrayAuditSnapshotsMatch()
	{
		if (_renderStaticLayerArrayEscaped && !GodotArraysEqual(_layerVisible, _renderStaticAuditLayerVisibleSnapshot))
		{
			return false;
		}
		if (!ShouldAuditRenderStaticMediaArrays())
		{
			return true;
		}
		bool flag = RenderStaticMediaAtlasPathAuditSnapshotMatches();
		return GodotArraysEqual(_mediaReplaceUse, _renderStaticAuditMediaReplaceUseSnapshot) && GodotArraysEqual(_mediaReplace, _renderStaticAuditMediaReplaceSnapshot) && GodotArraysEqual(mediaReplaceRect, _renderStaticAuditMediaReplaceRectSnapshot) && GodotArraysEqual(mediaReplaceAtlasPages, _renderStaticAuditMediaReplacePageSnapshot) && flag;
	}

	private bool RenderStaticMediaAtlasPathAuditSnapshotMatches()
	{
		if (GodotArraysEqual(_mediaReplaceAtlasPaths, _renderStaticAuditMediaReplaceAtlasPathSnapshot))
		{
			return true;
		}
		InvalidateStaticAtlasPathLayoutConfigurationSnapshot();
		InvalidateStaticAtlasPathLayoutForNodeMutation();
		QueueUpdateMediaReplace();
		return false;
	}

	private void CaptureRenderStaticArrayAuditSnapshots()
	{
		if (_renderStaticLayerArrayEscaped)
		{
			ReplaceGodotArraySnapshot(ref _renderStaticAuditLayerVisibleSnapshot, _layerVisible);
		}
		else
		{
			_renderStaticAuditLayerVisibleSnapshot = null;
		}
		if (ShouldAuditRenderStaticMediaArrays())
		{
			ReplaceGodotArraySnapshot(ref _renderStaticAuditMediaReplaceUseSnapshot, _mediaReplaceUse);
			ReplaceGodotArraySnapshot(ref _renderStaticAuditMediaReplaceSnapshot, _mediaReplace);
			ReplaceGodotArraySnapshot(ref _renderStaticAuditMediaReplaceRectSnapshot, mediaReplaceRect);
			ReplaceGodotArraySnapshot(ref _renderStaticAuditMediaReplacePageSnapshot, mediaReplaceAtlasPages);
			ReplaceGodotArraySnapshot(ref _renderStaticAuditMediaReplaceAtlasPathSnapshot, _mediaReplaceAtlasPaths);
		}
		else
		{
			_renderStaticAuditMediaReplaceUseSnapshot = null;
			_renderStaticAuditMediaReplaceSnapshot = null;
			_renderStaticAuditMediaReplaceRectSnapshot = null;
			_renderStaticAuditMediaReplacePageSnapshot = null;
			_renderStaticAuditMediaReplaceAtlasPathSnapshot = null;
		}
	}

	private bool ShouldAuditRenderStaticArrays()
	{
		if (!_renderStaticLayerArrayEscaped)
		{
			return ShouldAuditRenderStaticMediaArrays();
		}
		return true;
	}

	internal bool RequiresRenderStaticArrayAuditForRuntimeManager()
	{
		return ShouldAuditRenderStaticArrays();
	}

	internal bool QueueRenderStaticArrayAuditIfDue(ulong physicsFrame)
	{
		if (_renderStaticArrayAuditSubmissionPending || _renderStaticStateNextAuditPhysicsFrame == 18446744073709551615uL || physicsFrame < _renderStaticStateNextAuditPhysicsFrame)
		{
			return false;
		}
		if (_renderStaticStateCached && _cachedLayerStateVersion == _layerStateVersion && _cachedMediaReplaceStateVersion == _mediaReplaceStateVersion && RenderStaticArrayAuditSnapshotsMatch())
		{
			ScheduleNextRenderStaticStateAudit(physicsFrame);
			return false;
		}
		_renderStaticArrayAuditSubmissionPending = true;
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender))
		{
			renderSortRootForRender.InvalidateRuntimeCrowdCullingForRefresh();
		}
		else
		{
			InvalidateRuntimeCrowdCullingForRefresh();
		}
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		return true;
	}

	private bool ShouldAuditRenderStaticMediaArrays()
	{
		if (!_renderStaticMediaArraysEscaped)
		{
			return _cachedAnyRequestedMediaReplace;
		}
		return true;
	}

	private static bool GodotArraysEqual<[MustBeVariant] T>(Array<T> values, Array<T> snapshot)
	{
		if (values == null || snapshot == null)
		{
			if (values == null)
			{
				return snapshot == null;
			}
			return false;
		}
		return values.RecursiveEqual(snapshot);
	}

	private static void ReplaceGodotArraySnapshot<[MustBeVariant] T>(ref Array<T> snapshot, Array<T> values)
	{
		snapshot = values?.Duplicate();
	}

	private void ScheduleNextRenderStaticStateAudit(ulong physicsFrame)
	{
		_renderStaticArrayAuditSubmissionPending = false;
		if (!ShouldAuditRenderStaticArrays())
		{
			_renderStaticStateNextAuditPhysicsFrame = 18446744073709551615uL;
			AdobeAnimateRuntimeManager.SetRenderStaticArrayAuditRegistration(this, enabled: false);
		}
		else
		{
			_renderStaticStateNextAuditPhysicsFrame = physicsFrame + (ulong)GetStaggeredRescanInterval(60);
			AdobeAnimateRuntimeManager.SetRenderStaticArrayAuditRegistration(this, enabled: true);
		}
	}

	private int GetRuntimeLayerVisibleCount(int layerValueCount, AdobeAnimateRuntimeDefinition definition)
	{
		int val = layerValueCount;
		if (definition != null)
		{
			return Math.Max(val, definition.RuntimeLayerCount);
		}
		return Math.Max(val, GetLayerDictionaryLayerCount(_flashAnimeData?.layerDictionary));
	}

	private int GetDataRuntimeLayerCount()
	{
		int num = GetLayerDictionaryLayerCount(_flashAnimeData?.layerDictionary);
		AdobeAnimateRuntimeDefinition runtimeDefinition = GetRuntimeDefinition();
		if (runtimeDefinition != null)
		{
			num = Math.Max(num, runtimeDefinition.RuntimeLayerCount);
		}
		return num;
	}

	private static int GetLayerDictionaryLayerCount(Dictionary dictionary)
	{
		if (dictionary == null || dictionary.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (Variant key in dictionary.Keys)
		{
			int num2 = dictionary[key].AsInt32();
			if (num2 >= 0)
			{
				num = Math.Max(num, num2 + 1);
			}
		}
		return num;
	}

	private bool LayerHasSlicesInCurrentClip(int layerId)
	{
		if (layerId < 0 || layerId >= _cachedLayerVisibleCount)
		{
			return true;
		}
		if (layerId < _cachedLayerActiveInCurrentClip.Length)
		{
			return _cachedLayerActiveInCurrentClip[layerId];
		}
		return true;
	}

	private void RefreshLayerActiveInCurrentClipCache(int layerCount, AdobeAnimateRuntimeDefinition definition)
	{
		if (layerCount <= 0)
		{
			return;
		}
		if (definition == null || definition.Frames == null || definition.SliceMetadata == null)
		{
			EnsureLayerActiveCache(layerCount, defaultValue: true);
			return;
		}
		int num = definition.Frames.Length;
		if (num <= 0)
		{
			EnsureLayerActiveCache(layerCount, defaultValue: true);
			System.Array.Fill(_cachedLayerActiveInCurrentClip, value: true, 0, layerCount);
			return;
		}
		int num2 = 0;
		int num3 = num;
		if (clipRange != Vector2I.Zero)
		{
			num2 = Mathf.Clamp(clipRange.X, 0, num);
			num3 = Mathf.Clamp(clipRange.Y, num2, num);
		}
		LayerActiveCacheKey key = new LayerActiveCacheKey(definition, layerCount, num2, num3);
		if (LayerActiveInClipCache.TryGetValue(key, out var value) && value != null && value.Length >= layerCount)
		{
			EnsureLayerActiveCache(layerCount, defaultValue: false);
			System.Array.Copy(value, _cachedLayerActiveInCurrentClip, layerCount);
			return;
		}
		bool[] array = new bool[layerCount];
		PackedFrame[] frames = definition.Frames;
		PackedSliceMetadata[] sliceMetadata = definition.SliceMetadata;
		for (int i = num2; i < num3; i++)
		{
			PackedFrame packedFrame = frames[i];
			int num4 = packedFrame.Offset;
			int count = packedFrame.Count;
			if (num4 < 0 || count <= 0 || num4 >= sliceMetadata.Length)
			{
				continue;
			}
			int num5 = Math.Min(num4 + count, sliceMetadata.Length);
			for (int j = num4; j < num5; j++)
			{
				int layerId = sliceMetadata[j].LayerId;
				if (layerId >= 0 && layerId < layerCount)
				{
					array[layerId] = true;
				}
			}
		}
		if (LayerActiveInClipCache.Count >= 256)
		{
			LayerActiveInClipCache.Clear();
		}
		LayerActiveInClipCache[key] = array;
		EnsureLayerActiveCache(layerCount, defaultValue: false);
		System.Array.Copy(array, _cachedLayerActiveInCurrentClip, layerCount);
	}

	private void EnsureLayerActiveCache(int layerCount, bool defaultValue)
	{
		if (layerCount > 0)
		{
			if (_cachedLayerActiveInCurrentClip.Length < layerCount)
			{
				System.Array.Resize(ref _cachedLayerActiveInCurrentClip, layerCount);
			}
			System.Array.Fill(_cachedLayerActiveInCurrentClip, defaultValue, 0, layerCount);
		}
	}

	private bool IsMediaReplacedCached(int mediaId)
	{
		if (_cachedHasMediaReplace && mediaId >= 0 && mediaId < _cachedMediaReplaceLimit && _mediaReplaceUse != null)
		{
			return _mediaReplaceUse[mediaId];
		}
		return false;
	}

	private void InvalidateRenderSnapshotCache(bool force = false)
	{
		_needsRenderSubmission = true;
		if (force)
		{
			InvalidateRuntimeDisplayVisualRequirementCache();
			_hasCachedRenderSnapshot = false;
			_cachedRenderSnapshot = default(AdobeAnimateRenderSnapshot);
			_renderStateCached = false;
			_renderStaticStateCached = false;
			_runtimeRenderBoundsCached = false;
			_runtimeRenderBoundsRescanCountdown = 0;
			_loopTerminalAliasCacheHasValue = false;
			_playbackClipEndCacheHasValue = false;
			_effectiveZIndexCached = false;
			_effectiveRenderSortBandCached = false;
			_treeOrderPathRescanCountdown = 0;
		}
	}

	public void QueueUpdateMediaReplace()
	{
		if (!needMediaReplaceUpdate)
		{
			InvalidateStaticAtlasPathLayoutForNodeMutation();
			needMediaReplaceUpdate = true;
			MarkEffectOnceBatchEligibilityChanged();
			if (IsInsideTree())
			{
				CallDeferred(MethodName.UpdateMediaReplace);
			}
		}
	}

	public void UpdateMediaReplace()
	{
		if (needMediaReplaceUpdate)
		{
			needMediaReplaceUpdate = false;
			UpdateMediaReplaceData();
		}
	}

	public void UpdateMediaReplaceData()
	{
		PrepareStaticAtlasPathLayoutForExplicitUpdate();
		needMediaReplaceUpdate = false;
		CreateMediaReplaceAtlas();
		_mediaReplaceStateVersion++;
		MarkEffectOnceBatchEligibilityChanged();
		InvalidateRenderSnapshotCache();
		RequestNodeRedraw();
	}

	public void CreateMediaReplaceAtlas()
	{
		if (!_bypassStaticAtlasPathLayoutCacheForTests && TryReuseAppliedStaticAtlasPathLayout())
		{
			return;
		}
		mediaReplaceAtlasShared = false;
		mediaReplaceAtlas = null;
		mediaReplaceAtlasArray = null;
		mediaReplaceAtlasArraySize = default(Vector2);
		mediaReplaceAtlasUsesTextureArray = false;
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null || (!_bypassStaticAtlasPathLayoutCacheForTests && TryApplyStaticAtlasPathLayoutCache()))
		{
			return;
		}
		List<Texture2D> list = new List<Texture2D>();
		List<string> list2 = new List<string>();
		List<int> list3 = new List<int>();
		int num = Math.Min(_mediaReplace.Count, _mediaReplaceUse.Count);
		for (int i = 0; i < num; i++)
		{
			if (_mediaReplaceUse[i])
			{
				Texture2D texture2D = _mediaReplace[i];
				string text = ((i < _mediaReplaceAtlasPaths.Count) ? _mediaReplaceAtlasPaths[i] : string.Empty);
				if (!GodotObject.IsInstanceValid(texture2D) && string.IsNullOrEmpty(text))
				{
					_mediaReplaceUse[i] = false;
					continue;
				}
				list.Add(texture2D);
				list2.Add(text);
				list3.Add(i);
			}
		}
		mediaReplaceRect.Clear();
		mediaReplaceRect.Resize(_flashAnimeData.mediaDictionary.Count);
		mediaReplaceAtlasPages.Clear();
		mediaReplaceAtlasPages.Resize(_flashAnimeData.mediaDictionary.Count);
		for (int j = 0; j < mediaReplaceRect.Count; j++)
		{
			mediaReplaceRect[j] = default(Rect2);
			mediaReplaceAtlasPages[j] = 0;
		}
		if (list.Count == 0)
		{
			mediaReplaceAtlas = null;
		}
		else if (!TryUseSharedMediaReplaceAtlas(list, list2, list3))
		{
			DisableUnsharedMediaReplaceTextures(list3);
		}
		else if (!_bypassStaticAtlasPathLayoutCacheForTests)
		{
			TryPublishStaticAtlasPathLayoutCache();
		}
	}

	public override void _Draw()
	{
		if (_effectOnceGpuSuppressed || suppressEditorRenderSubmission || (_usingRuntimeManager && !CachedEditorHint && !_forceLocalRender) || IsRenderedByParentSpriteForRender() || !TryBuildRenderSnapshot(out var snapshot))
		{
			return;
		}
		if (CachedEditorHint)
		{
			if (_preview)
			{
				SubmitRuntimeRenderSnapshot(snapshot);
			}
		}
		else if (IsInSubViewportRenderTarget())
		{
			SubmitRuntimeRenderSnapshot(snapshot);
		}
		else if (_forceLocalRender)
		{
			SubmitRuntimeRenderSnapshot(snapshot);
		}
		else if (!_usingRuntimeManager)
		{
			SubmitRuntimeRenderSnapshot(snapshot);
		}
	}

	internal bool IsRenderedByParentSpriteForRender()
	{
		AdobeAnimateSprite adobeAnimateSprite = ResolveParentSpriteForLayerInspector();
		if (adobeAnimateSprite != null && adobeAnimateSprite != this)
		{
			return adobeAnimateSprite.OwnsSpriteChildForRender(this);
		}
		return false;
	}

	private void SubmitRuntimeRenderSnapshot(AdobeAnimateRenderSnapshot snapshot)
	{
		if (_forceCpuPoseRender)
		{
			AdobeAnimateRenderManager.SubmitCpuPose(snapshot, immediateFlush: true);
		}
		else
		{
			AdobeAnimateRenderManager.Submit(snapshot, immediateFlush: true);
		}
	}

	private void SubmitRuntimeManagerFirstFrameHandoff()
	{
		if (_usingRuntimeManager && _runtimeInsideTree && !_forceLocalRender && !IsRenderedByParentSpriteForRender() && AdobeAnimateRenderManager.HasPreparedRenderMount(this) && TryBuildRenderSnapshot(out var snapshot))
		{
			_runtimeManagerFirstFrameHandoffActive = AdobeAnimateRenderManager.SubmitCpuPose(snapshot, immediateFlush: true);
		}
	}

	internal bool TrySubmitRuntimeRenderFailureHandoff()
	{
		if (!_usingRuntimeManager || !_runtimeInsideTree || _forceLocalRender || IsRenderedByParentSpriteForRender() || !AdobeAnimateRenderManager.HasPreparedRenderMount(this))
		{
			return false;
		}
		if (!TryBuildRenderSnapshot(out var snapshot))
		{
			return false;
		}
		bool flag = AdobeAnimateRenderManager.SubmitCpuPose(snapshot);
		_runtimeManagerFirstFrameHandoffActive |= flag;
		RequestRuntimeRenderSubmissionRetry();
		return flag;
	}

	private void ReleaseRuntimeManagerFirstFrameHandoff()
	{
		if (_runtimeManagerFirstFrameHandoffActive)
		{
			_runtimeManagerFirstFrameHandoffActive = false;
			AdobeAnimateRenderManager.ReleaseImmediateSubmission(this);
			if (_runtimeDefinition != null)
			{
				AdobeAnimateDefinitionCache.ReleaseCpuPoseDataIfBaked(_runtimeDefinition);
			}
		}
	}

	private void ReleaseRuntimeManagerFirstFrameHandoffTree()
	{
		ReleaseRuntimeManagerFirstFrameHandoff();
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.ReleaseRuntimeManagerFirstFrameHandoffTree();
			}
		}
	}

	public void ReleaseForcedCpuPoseData()
	{
		if (_forceCpuPoseRender)
		{
			_forceCpuPoseRender = false;
			if (_runtimeDefinition != null)
			{
				AdobeAnimateDefinitionCache.ReleaseCpuPoseDataIfBaked(_runtimeDefinition);
			}
			InvalidateRenderSnapshotCache(force: true);
			RequestNodeRedraw();
		}
	}

	private static bool HasRenderableAtlas(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition != null && definition.Frames != null && definition.SliceMetadata != null && GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			return definition.AtlasTextureArrayRid.IsValid;
		}
		return false;
	}

	public override void _Process(double delta)
	{
		if (!_effectOnceGpuSuppressed && (!_usingRuntimeManager || _dispatchingBatchedProcess))
		{
			RunDefaultBatchedProcessUpdate(delta);
		}
	}

	public void RunBatchedProcessUpdate(double delta)
	{
		if (_effectOnceGpuSuppressed)
		{
			return;
		}
		_dispatchingBatchedProcess = true;
		try
		{
			RunDefaultBatchedProcessUpdate(delta);
		}
		finally
		{
			_dispatchingBatchedProcess = false;
		}
	}

	internal void RunDisplayFrameVisualUpdate(double delta)
	{
		if (_effectOnceGpuSuppressed)
		{
			return;
		}
		_displayFrameVisualDispatchActive = true;
		try
		{
			RunDefaultBatchedProcessUpdate(delta, _displayFrameGpuClockPoseReusable);
		}
		finally
		{
			_displayFrameVisualDispatchActive = false;
		}
	}

	public void BatchProcessUpdate(double delta)
	{
		BatchProcessUpdate(delta, reuseRuntimeGpuClockPose: false);
	}

	internal void RunDefaultBatchedProcessUpdate(double delta)
	{
		RunDefaultBatchedProcessUpdate(delta, reuseRuntimeGpuClockPose: false);
	}

	private void RunDefaultBatchedProcessUpdate(double delta, bool reuseRuntimeGpuClockPose)
	{
		BatchProcessUpdate(delta, reuseRuntimeGpuClockPose);
		RunBatchedProcessExtension(delta);
	}

	protected virtual void RunBatchedProcessExtension(double delta)
	{
	}

	private void BatchProcessUpdate(double delta, bool reuseRuntimeGpuClockPose)
	{
		if (_effectOnceGpuSuppressed)
		{
			return;
		}
		_managedPoseRevision++;
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		if (!_canRun)
		{
			return;
		}
		if (IsPlaybackStopped)
		{
			if (keepRenderSubmittedWhenPaused && !_usingRuntimeManager)
			{
				QueueRedraw();
			}
			return;
		}
		if (TryFastLoopProcessUpdate(delta, reuseRuntimeGpuClockPose))
		{
			TowerDefensePerfProfiler.End("adobeAnimate.sprite.fastLoop", startTicks, 1);
			return;
		}
		int num = frameIndex;
		double num2 = elapsedTimer;
		bool flag = clipOver;
		bool flag2 = blend;
		double num3 = blendTimer;
		if (_nextAnimDelayTimer > 0.0)
		{
			_nextAnimDelayTimer -= delta;
			if (_nextAnimDelayTimer <= 0.0)
			{
				_nextAnimDelayTimer = -1.0;
				ApplyNextAnimation(_pendingTrackConfig);
				_pendingTrackConfig = null;
			}
			TowerDefensePerfProfiler.End("adobeAnimate.sprite.nextDelay", startTicks, 1);
			return;
		}
		bool flag3 = _displayFrameVisualDispatchActive;
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		bool flag4 = playbackClipEndExclusive - clipRange.X <= 1;
		if (!clipOver)
		{
			if (!flag4)
			{
				refreshTimer += delta * Mathf.Max(10.0, trueFrameRate);
				if (refreshTimer >= 1.0)
				{
					flag3 = true;
					refreshTimer = 0.0;
				}
			}
			else
			{
				frameIndex = clipRange.X;
				flag3 = true;
				refreshTimer = 0.0;
			}
			if (!playBack)
			{
				if (!flag4)
				{
					elapsedTimer += delta * Mathf.Abs(timeScale) * frameRate;
				}
				if (refreshEveryFrame || refreshEveryFlame || elapsedTimer >= 1.0)
				{
					flag3 = true;
				}
				while (elapsedTimer >= 1.0)
				{
					elapsedTimer -= 1.0;
					EmitFrameEvents(frameIndex);
					frameIndex++;
					if (frameIndex < playbackClipEndExclusive)
					{
						continue;
					}
					EmitFrameEvents(frameIndex);
					if (!loop)
					{
						clipOver = true;
						string text = _clip;
						frameIndex = Math.Max(clipRange.X, playbackClipEndExclusive - 1);
						elapsedTimer = 0.0;
						NextAnimation();
						if (!CachedEditorHint)
						{
							this.OnAnimeCompleted?.Invoke(text);
						}
					}
					else
					{
						if (!CachedEditorHint)
						{
							this.OnAnimeCompleted?.Invoke(_clip);
						}
						frameIndex = clipRange.X;
						elapsedTimer = 0.0;
						flag3 = true;
					}
				}
			}
			else
			{
				if (!flag4)
				{
					elapsedTimer -= delta * Mathf.Abs(timeScale) * frameRate;
				}
				if (refreshEveryFrame || refreshEveryFlame || elapsedTimer <= -1.0)
				{
					flag3 = true;
				}
				while (elapsedTimer <= -1.0)
				{
					elapsedTimer += 1.0;
					EmitFrameEvents(frameIndex);
					frameIndex--;
					if (frameIndex >= clipRange.X)
					{
						continue;
					}
					if (!loop)
					{
						clipOver = true;
						string text2 = _clip;
						frameIndex = clipRange.X;
						elapsedTimer = 0.0;
						NextAnimation();
						if (!CachedEditorHint)
						{
							this.OnAnimeCompleted?.Invoke(text2);
						}
					}
					else
					{
						if (!CachedEditorHint)
						{
							this.OnAnimeCompleted?.Invoke(_clip);
						}
						frameIndex = Math.Max(clipRange.X, clipRange.Y - 1);
						elapsedTimer = 0.0;
						flag3 = true;
					}
				}
			}
			if (!clipOver && blend)
			{
				blendTimer = Mathf.Clamp(blendTimer + delta * (double)Mathf.Abs(Mathf.Sign(timeScale)), 0.0, blendTime);
				if (blendTimer >= blendTime)
				{
					this.OnAnimeBlendCompleted?.Invoke(_clip);
					blendTime = 0.0;
					blendTimer = 0.0;
					blend = false;
					flag3 = true;
				}
			}
		}
		if (clipOver)
		{
			_needsRenderSubmission = true;
			RequestNodeRedraw();
			if (track.Count <= 0 && _nextAnimDelayTimer <= 0.0)
			{
				StopRuntimeTickKeepRender();
			}
			return;
		}
		if (flag3)
		{
			bool flag5 = frameIndex != _lastUpdateFrame;
			if (flag5)
			{
				_lastUpdateFrame = frameIndex;
			}
			if (_hasChildren && !ShouldSkipRuntimeChildUpdateForViewport())
			{
				long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
				UpdateChild();
				TowerDefensePerfProfiler.End("adobeAnimate.sprite.updateChild", startTicks2, 1);
			}
			refreshTimer = 0.0;
			if (_invisible && flag5)
			{
				CallDeferred(MethodName.UpdateInvisibleChild);
			}
		}
		if (frameIndex != num || !Mathf.IsEqualApprox((float)elapsedTimer, (float)num2) || clipOver != flag || blend != flag2 || !Mathf.IsEqualApprox((float)blendTimer, (float)num3))
		{
			RequestAnimationPoseRedraw();
		}
		TowerDefensePerfProfiler.End("adobeAnimate.sprite.fullProcess", startTicks, 1);
	}

	private bool TryFastLoopProcessUpdate(double delta, bool reuseRuntimeGpuClockPose)
	{
		if (clipOver || playBack || !loop || blend || _nextAnimDelayTimer > 0.0 || refreshEveryFrame || refreshEveryFlame || track.Count > 0)
		{
			return false;
		}
		bool flag = HasRuntimeChildUpdates();
		int x = clipRange.X;
		int playbackClipEndExclusive = GetPlaybackClipEndExclusive();
		int num = playbackClipEndExclusive - x;
		if (num <= 1)
		{
			if (frameIndex != x || elapsedTimer != 0.0)
			{
				frameIndex = x;
				elapsedTimer = 0.0;
				if (!reuseRuntimeGpuClockPose)
				{
					RequestAnimationPoseRedraw();
				}
			}
			if (flag && _lastUpdateFrame != frameIndex && !ShouldSkipRuntimeChildUpdateForViewport())
			{
				_lastUpdateFrame = frameIndex;
				long startTicks = TowerDefensePerfProfiler.BeginHotPath();
				UpdateChild();
				TowerDefensePerfProfiler.End("adobeAnimate.sprite.fastSlotUpdate", startTicks, 1);
			}
			return true;
		}
		double num2 = delta * Math.Abs(timeScale) * frameRate;
		if (num2 <= 0.0)
		{
			return true;
		}
		int num3 = frameIndex;
		double num4 = elapsedTimer;
		int num5 = (int)Math.Floor(num4 + num2);
		double num6 = (double)(frameIndex - x) + elapsedTimer + num2;
		if (num6 >= (double)num || num6 < 0.0)
		{
			num6 %= (double)num;
			if (num6 < 0.0)
			{
				num6 += (double)num;
			}
		}
		if (num5 > 0 && (this.OnAnimeEvent != null || this.OnAnimeCompleted != null))
		{
			ulong playbackRevision = _playbackRevision;
			if (EmitFastLoopCallbacks(num3, num5, x, playbackClipEndExclusive, playbackRevision))
			{
				return true;
			}
		}
		int num7 = (int)num6;
		frameIndex = x + num7;
		elapsedTimer = num6 - (double)num7;
		if (!reuseRuntimeGpuClockPose && (frameIndex != num3 || !Mathf.IsEqualApprox((float)elapsedTimer, (float)num4)))
		{
			RequestAnimationPoseRedraw();
		}
		if (flag)
		{
			refreshTimer += delta * Mathf.Max(10.0, trueFrameRate);
			if (_displayFrameVisualDispatchActive || refreshTimer >= 1.0)
			{
				_lastUpdateFrame = frameIndex;
				if (!ShouldSkipRuntimeChildUpdateForViewport())
				{
					long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
					UpdateChild();
					TowerDefensePerfProfiler.End("adobeAnimate.sprite.fastSlotUpdate", startTicks2, 1);
				}
				refreshTimer = 0.0;
			}
		}
		return true;
	}

	private bool EmitFastLoopCallbacks(int startFrame, int crossedFrames, int clipStart, int clipEnd, ulong playbackRevision)
	{
		int num = clipEnd - clipStart;
		if (num <= 0)
		{
			return false;
		}
		int num2 = Math.Min(crossedFrames, num * 4);
		int num3 = Mathf.Clamp(startFrame - clipStart, 0, num - 1);
		for (int i = 0; i < num2; i++)
		{
			int frameIdx = clipStart + num3;
			EmitFrameEvents(frameIdx);
			if (_playbackRevision != playbackRevision)
			{
				return true;
			}
			num3++;
			if (num3 >= num)
			{
				EmitFrameEvents(clipEnd);
				if (_playbackRevision != playbackRevision)
				{
					return true;
				}
				if (!CachedEditorHint)
				{
					this.OnAnimeCompleted?.Invoke(_clip);
				}
				if (_playbackRevision != playbackRevision)
				{
					return true;
				}
				num3 = 0;
			}
		}
		if (crossedFrames > num2)
		{
			int num4 = (crossedFrames - num2) / num;
			for (int j = 0; j < num4; j++)
			{
				if (!CachedEditorHint)
				{
					this.OnAnimeCompleted?.Invoke(_clip);
				}
				if (_playbackRevision != playbackRevision)
				{
					return true;
				}
			}
		}
		return false;
	}

	private bool HasRuntimeChildUpdates()
	{
		if (!CachedEditorHint)
		{
			return _runtimeHasChildUpdates;
		}
		return _hasChildren;
	}

	private void RefreshRuntimeChildUpdatePresence()
	{
		if (_spriteChildren.Length != 0)
		{
			_runtimeHasChildUpdates = true;
			return;
		}
		for (int i = 0; i < _slotChildren.Length; i++)
		{
			AdobeAnimateSlot adobeAnimateSlot = _slotChildren[i];
			if (adobeAnimateSlot != null && adobeAnimateSlot.RequiresRuntimeUpdate)
			{
				_runtimeHasChildUpdates = true;
				return;
			}
		}
		_runtimeHasChildUpdates = false;
	}

	private bool ShouldSkipRuntimeChildUpdateForViewport()
	{
		if (ShouldUseGpuGraphVisualAttachment())
		{
			return true;
		}
		if (CachedEditorHint || !_hasChildren || !RuntimeChildUpdatesAreVisualOnly())
		{
			return false;
		}
		return !IsRuntimeChildUpdateNearViewport();
	}

	private bool ShouldUseGpuGraphVisualAttachment()
	{
		if (CachedEditorHint || !_usingRuntimeManager || (Global.Instance != null && Global.Instance.adobeAnimateRenderBackend != AdobeAnimateRenderBackend.GpuCrowd && Global.Instance.adobeAnimateRenderBackend != AdobeAnimateRenderBackend.CpuPose) || !_hasChildren || !RuntimeChildUpdatesAreVisualOnly())
		{
			return false;
		}
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender) && !renderSortRootForRender._forceCpuPoseRender)
		{
			return AdobeAnimateRenderManager.IsGpuGraphReady(renderSortRootForRender);
		}
		return false;
	}

	private bool RuntimeChildUpdatesAreVisualOnly()
	{
		if (!_runtimeChildUpdatesVisualOnlyDirty)
		{
			return _runtimeChildUpdatesVisualOnlyCached;
		}
		bool flag = _spriteChildren.Length != 0;
		AdobeAnimateSlot[] runtimeSlotChildren = GetRuntimeSlotChildren();
		foreach (AdobeAnimateSlot adobeAnimateSlot in runtimeSlotChildren)
		{
			if (adobeAnimateSlot != null && GodotObject.IsInstanceValid(adobeAnimateSlot))
			{
				if (!adobeAnimateSlot.RuntimeUpdateVisualOnly)
				{
					flag = false;
					break;
				}
				flag = true;
			}
		}
		_runtimeChildUpdatesVisualOnlyCached = flag;
		_runtimeChildUpdatesVisualOnlyDirty = false;
		return flag;
	}

	private bool IsRuntimeChildUpdateNearViewport()
	{
		long processFrames = (long)Engine.GetProcessFrames();
		if (_runtimeChildViewportFrame == processFrames)
		{
			return _runtimeChildViewportVisible;
		}
		_runtimeChildViewportFrame = processFrames;
		_runtimeChildViewportVisible = true;
		Viewport viewport = GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return true;
		}
		Rect2 visibleRect = viewport.GetVisibleRect();
		if (visibleRect.Size.X <= 0f || visibleRect.Size.Y <= 0f)
		{
			return true;
		}
		Vector2 origin = GetGlobalTransformWithCanvas().Origin;
		Vector2 vector = new Vector2(256f, 256f);
		_runtimeChildViewportVisible = new Rect2(visibleRect.Position - vector, visibleRect.Size + vector * 2f).HasPoint(origin);
		return _runtimeChildViewportVisible;
	}

	public void UpdateInvisibleChild()
	{
		UpdateChild();
	}

	public void FileChange()
	{
		ApplyFlashAnimeDataChange(invalidateDefinitionCache: true);
	}

	private void OnFlashAnimeDataChanged()
	{
		ApplyFlashAnimeDataChange(invalidateDefinitionCache: true);
	}

	private void ApplyFlashAnimeDataChange(bool invalidateDefinitionCache)
	{
		InvalidateStaticAtlasPathLayoutForNodeMutation();
		_managedPoseRevision++;
		_runtimeDefinition = null;
		meshTexture = default(Rid);
		if (_flashAnimeData == null)
		{
			return;
		}
		TryHydrateFlashAnimeDataBeforeFileChange();
		if (invalidateDefinitionCache)
		{
			AdobeAnimateDefinitionCache.Invalidate(_flashAnimeData);
		}
		if (!HasUsableFlashAnimeData())
		{
			return;
		}
		frameIndex = 0;
		frameRate = _flashAnimeData.frameRate;
		int layerDictionaryLayerCount = GetLayerDictionaryLayerCount(_flashAnimeData.layerDictionary);
		if (_layerVisible.Count != layerDictionaryLayerCount)
		{
			_layerVisible.Clear();
			_layerVisible.Resize(layerDictionaryLayerCount);
			for (int i = 0; i < _layerVisible.Count; i++)
			{
				_layerVisible[i] = true;
			}
			_mediaReplace.Clear();
			_mediaReplace.Resize(_flashAnimeData.mediaDictionary.Count);
			_mediaReplaceUse.Clear();
			_mediaReplaceUse.Resize(_flashAnimeData.mediaDictionary.Count);
			_mediaReplaceAtlasPaths.Clear();
			_mediaReplaceAtlasPaths.Resize(_flashAnimeData.mediaDictionary.Count);
			MarkLayerStateChanged();
		}
		else if (_mediaReplaceAtlasPaths.Count != _flashAnimeData.mediaDictionary.Count)
		{
			_mediaReplaceAtlasPaths.Clear();
			_mediaReplaceAtlasPaths.Resize(_flashAnimeData.mediaDictionary.Count);
		}
		mediaReplaceRect.Clear();
		mediaReplaceRect.Resize(_flashAnimeData.mediaDictionary.Count);
		for (int j = 0; j < mediaReplaceRect.Count; j++)
		{
			mediaReplaceRect[j] = default(Rect2);
		}
		_mediaReplaceStateVersion++;
		MarkEffectOnceBatchEligibilityChanged();
		onlyDraw = false;
		mesh = null;
		base.Material = null;
		ICollection<Variant> keys = _flashAnimeData.clips.Keys;
		if (keys.Count > 0 && string.IsNullOrEmpty(_clip))
		{
			using IEnumerator<Variant> enumerator = keys.GetEnumerator();
			if (enumerator.MoveNext())
			{
				_clip = enumerator.Current.ToString();
			}
		}
		InvalidateRenderSnapshotCache(force: true);
	}

	private bool DeferFlashAnimeDataChangeUntilReady()
	{
		if (!IsInsideTree())
		{
			return !CachedEditorHint;
		}
		return false;
	}

	private void ApplyPendingFlashAnimeDataChange()
	{
		if (_flashAnimeDataChangePending)
		{
			_flashAnimeDataChangePending = false;
			ApplyFlashAnimeDataChange(invalidateDefinitionCache: false);
		}
	}

	private void PrepareFlashAnimeDataSerializedOverrides()
	{
		_runtimeDefinition = null;
		meshTexture = default(Rid);
		if (_flashAnimeData == null)
		{
			return;
		}
		TryHydrateFlashAnimeDataBeforeFileChange();
		if (!HasUsableFlashAnimeData())
		{
			return;
		}
		frameRate = _flashAnimeData.frameRate;
		int layerDictionaryLayerCount = GetLayerDictionaryLayerCount(_flashAnimeData.layerDictionary);
		if (_layerVisible.Count != layerDictionaryLayerCount)
		{
			_layerVisible.Clear();
			_layerVisible.Resize(layerDictionaryLayerCount);
			for (int i = 0; i < _layerVisible.Count; i++)
			{
				_layerVisible[i] = true;
			}
		}
		int count = _flashAnimeData.mediaDictionary.Count;
		if (_mediaReplace.Count != count)
		{
			_mediaReplace.Clear();
			_mediaReplace.Resize(count);
		}
		if (_mediaReplaceUse.Count != count)
		{
			_mediaReplaceUse.Clear();
			_mediaReplaceUse.Resize(count);
		}
		if (_mediaReplaceAtlasPaths.Count != count)
		{
			_mediaReplaceAtlasPaths.Clear();
			_mediaReplaceAtlasPaths.Resize(count);
		}
	}

	private bool HasUsableFlashAnimeData()
	{
		if (_flashAnimeData == null || _flashAnimeData.layerDictionary == null || _flashAnimeData.mediaDictionary == null || _flashAnimeData.clips == null)
		{
			return false;
		}
		return _flashAnimeData.HasPackedRuntimeData();
	}

	private void TryHydrateFlashAnimeDataBeforeFileChange()
	{
		if (_flashAnimeData != null && (_flashAnimeData.clips == null || _flashAnimeData.clips.Count == 0 || !_flashAnimeData.HasPackedRuntimeData()))
		{
			_flashAnimeData.EnsurePackedRuntimeData();
			if (!_flashAnimeData.HasPackedRuntimeData())
			{
				_flashAnimeData.TryInitForEditorAssignedAnimeFile();
			}
		}
	}

	private void RefreshTransformNotificationMode()
	{
		if (_runtimeInsideTree)
		{
			AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
			bool flag = _parentSprite != null && GodotObject.IsInstanceValid(renderSortRootForRender) && renderSortRootForRender != this && renderSortRootForRender.ShouldUseGlobalRuntimeManager();
			SetNotifyTransform(CachedEditorHint || !flag);
		}
	}

	private void RefreshTransformNotificationModeForRenderTree()
	{
		RefreshTransformNotificationMode();
		MarkRenderTransformDirty();
		AdobeAnimateSprite[] spriteChildrenForRender = GetSpriteChildrenForRender();
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				adobeAnimateSprite.RefreshTransformNotificationModeForRenderTree();
			}
		}
	}

	private void MarkRenderTransformDirty(bool changedInPhysicsFrame = false)
	{
		_knownAncestorTranslationNotificationPending = false;
		bool flag = changedInPhysicsFrame || _runtimePhysicsAncestorTransformPending || TowerDefenseProcessModeDispatch.IsCharacterPhysicsBatchDispatchActive || Engine.IsInPhysicsFrame();
		if (!changedInPhysicsFrame)
		{
			_runtimePhysicsAncestorTransformPending = false;
		}
		if (_renderGlobalTransformDirty)
		{
			if (flag)
			{
				_renderTransformChangedInPhysicsFrame = true;
			}
		}
		else
		{
			_renderGlobalTransformDirty = true;
			_renderTransformChangedInPhysicsFrame = flag;
			InvalidateRenderSnapshotCache();
			RequestNodeRedraw(flag, transformOnly: true);
		}
	}

	private bool ClearRetainedRootMotionForPause()
	{
		return ClearRetainedRootMotionForStoppedMovement();
	}

	internal bool ClearRetainedRootMotionForStoppedMovement()
	{
		Transform2D globalTransform = base.GlobalTransform;
		if (!_renderRootMotionEnabled && _renderGlobalTransformCached && !_renderGlobalTransformDirty && !(_renderGlobalTransform != globalTransform))
		{
			return false;
		}
		_renderRootMotionEnabled = false;
		_renderGlobalTransform = globalTransform;
		_renderPreviousGlobalTransform = globalTransform;
		_renderGlobalTransformCached = true;
		_renderGlobalTransformDirty = false;
		_renderTransformChangedInPhysicsFrame = false;
		_runtimePhysicsAncestorTransformPending = false;
		_renderGlobalTransformPhysicsFrame = Engine.GetPhysicsFrames();
		if (_hasCachedGpuGraphCrowdState)
		{
			_cachedGpuGraphCrowdState.ClearRootMotionForRender();
		}
		InvalidateRenderSnapshotCache(force: true);
		RequestNodeRedraw();
		return true;
	}

	private bool CanInterpolateRenderRootMotion()
	{
		if (!IsPlaybackStopped)
		{
			return !_renderTreePaused;
		}
		return false;
	}

	private bool HasCurrentPhysicsRootMotion()
	{
		if (!_renderRootMotionEnabled)
		{
			return false;
		}
		if (AdobeAnimateRenderManager.GetPhysicsFrameForRender() == _renderGlobalTransformPhysicsFrame)
		{
			return true;
		}
		_renderRootMotionEnabled = false;
		_renderPreviousGlobalTransform = _renderGlobalTransform;
		return false;
	}

	internal void NotifyAncestorTransformChangedForRender(bool changedInPhysicsFrame = false)
	{
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender) && renderSortRootForRender != this && renderSortRootForRender.ShouldUseGlobalRuntimeManager())
		{
			if (changedInPhysicsFrame)
			{
				renderSortRootForRender._runtimePhysicsAncestorTransformPending = true;
			}
			renderSortRootForRender.MarkRenderTransformDirty(changedInPhysicsFrame);
		}
		else
		{
			NotifyLocalRenderTransformChangedForTree(changedInPhysicsFrame);
		}
	}

	internal void NotifyAncestorTranslatedForRender(Vector2 globalDelta, ulong physicsFrame)
	{
		if (globalDelta == Vector2.Zero)
		{
			return;
		}
		if (_parentSprite == null)
		{
			ApplyKnownGlobalTranslationForRender(globalDelta, physicsFrame);
			return;
		}
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender) && renderSortRootForRender != this && renderSortRootForRender.ShouldUseGlobalRuntimeManager())
		{
			renderSortRootForRender.ApplyKnownGlobalTranslationForRender(globalDelta, physicsFrame);
		}
		else
		{
			ApplyKnownGlobalTranslationForRender(globalDelta, physicsFrame);
		}
	}

	internal void PrimeAncestorTranslationForRender()
	{
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender))
		{
			renderSortRootForRender.GetCachedGlobalTransformForRender();
		}
	}

	internal void InvalidateRuntimeRenderMountForAncestorChange()
	{
		_renderMountNextAuditPhysicsFrame = 0uL;
		ResetEffectiveRenderModulateAncestorCache();
		InvalidateTreeOrderPathCache();
		RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
	}

	internal void InvalidateProgressRestorePresentation()
	{
		AdobeAnimateSprite renderSortRootForRender = GetRenderSortRootForRender();
		if (GodotObject.IsInstanceValid(renderSortRootForRender))
		{
			renderSortRootForRender.InvalidateRuntimeCrowdCullingForRefresh();
			renderSortRootForRender.InvalidateRenderSnapshotCache(force: true);
			renderSortRootForRender.MarkRenderTransformDirty();
			renderSortRootForRender._needsRenderSubmission = true;
			renderSortRootForRender.RequestNodeRedraw(preferDisplayCadence: false, transformOnly: false, staticStateDirty: false);
		}
	}

	private void InvalidateStaticAtlasPathLayoutConfigurationSnapshot()
	{
		_staticAtlasPathSnapshotValid = false;
		_staticAtlasPathSnapshotData = null;
	}

	private void CaptureStaticAtlasPathLayoutConfigurationSnapshot()
	{
		_staticAtlasPathSnapshotValid = false;
		if (_flashAnimeData == null || _flashAnimeData.mediaDictionary == null)
		{
			return;
		}
		int count = _flashAnimeData.mediaDictionary.Count;
		int num = _mediaReplace?.Count ?? 0;
		int num2 = _mediaReplaceUse?.Count ?? 0;
		int num3 = _mediaReplaceAtlasPaths?.Count ?? 0;
		int num4 = Math.Min(num, num2);
		if (count <= 0 || num != count || num2 != count || num3 != count || num4 != count)
		{
			return;
		}
		if (_staticAtlasPathSnapshotActive.Length != num4)
		{
			_staticAtlasPathSnapshotActive = new bool[num4];
			_staticAtlasPathSnapshotPaths = new string[num4];
		}
		ulong num5 = 1469598103934665603uL;
		int num6 = 0;
		for (int i = 0; i < num4; i++)
		{
			bool flag = _mediaReplaceUse[i];
			string text = _mediaReplaceAtlasPaths[i] ?? string.Empty;
			_staticAtlasPathSnapshotActive[i] = flag;
			_staticAtlasPathSnapshotPaths[i] = text;
			num5 ^= (ulong)(flag ? 1 : 0);
			num5 *= 1099511628211L;
			for (int j = 0; j < text.Length; j++)
			{
				num5 ^= text[j];
				num5 *= 1099511628211L;
			}
			num5 ^= 0xFF;
			num5 *= 1099511628211L;
			if (flag)
			{
				num6++;
				Texture2D texture2D = _mediaReplace[i];
				if ((texture2D != null && GodotObject.IsInstanceValid(texture2D)) || string.IsNullOrWhiteSpace(text))
				{
					return;
				}
			}
		}
		if (num6 > 0)
		{
			_staticAtlasPathSnapshotData = _flashAnimeData;
			_staticAtlasPathSnapshotDataInstanceId = _flashAnimeData.GetInstanceId();
			_staticAtlasPathSnapshotAuthoringRevision = _flashAnimeData.AuthoringRevision;
			_staticAtlasPathSnapshotMediaCount = count;
			_staticAtlasPathSnapshotTextureCount = num;
			_staticAtlasPathSnapshotUseCount = num2;
			_staticAtlasPathSnapshotPathCount = num3;
			_staticAtlasPathSnapshotEffectiveLimit = num4;
			_staticAtlasPathSnapshotActiveCount = num6;
			_staticAtlasPathSnapshotSlotSignature = num5;
			_staticAtlasPathSnapshotValid = true;
		}
	}

	private void InvalidateStaticAtlasPathLayoutForNodeMutation(bool markRuntimeMutation = true)
	{
		_appliedStaticAtlasPathLayout = null;
		if (markRuntimeMutation && _runtimeReadyCached)
		{
			_staticAtlasPathLayoutRuntimeMutationSeen = true;
		}
	}

	private void PrepareStaticAtlasPathLayoutForExplicitUpdate()
	{
		bool staticAtlasPathLayoutGpuResetPending = _staticAtlasPathLayoutGpuResetPending;
		_staticAtlasPathLayoutGpuResetPending = false;
		if (!_runtimeReadyCached)
		{
			InvalidateStaticAtlasPathLayoutForNodeMutation(markRuntimeMutation: false);
			CaptureStaticAtlasPathLayoutConfigurationSnapshot();
		}
		else
		{
			InvalidateStaticAtlasPathLayoutForNodeMutation(!staticAtlasPathLayoutGpuResetPending);
		}
	}

	internal static void InvalidateStaticAtlasPathLayoutCacheForGpuReset()
	{
		StaticAtlasPathLayoutCache.Clear();
		StaticAtlasPathLayoutCacheVersion = -2147483648;
	}

	internal static void ClearStaticAtlasPathLayoutCacheForTests()
	{
		InvalidateStaticAtlasPathLayoutCacheForGpuReset();
	}

	internal bool TryApplyStaticAtlasPathLayoutCacheForTests()
	{
		return TryApplyStaticAtlasPathLayoutCache();
	}

	internal bool CanReuseAppliedStaticAtlasPathLayoutForTests()
	{
		return TryReuseAppliedStaticAtlasPathLayout();
	}

	internal void CreateMediaReplaceAtlasWithoutStaticCacheForTests()
	{
		bool bypassStaticAtlasPathLayoutCacheForTests = _bypassStaticAtlasPathLayoutCacheForTests;
		_bypassStaticAtlasPathLayoutCacheForTests = true;
		try
		{
			CreateMediaReplaceAtlas();
		}
		finally
		{
			_bypassStaticAtlasPathLayoutCacheForTests = bypassStaticAtlasPathLayoutCacheForTests;
		}
	}

	internal bool CorruptStaticAtlasPathLayoutEntryForTests()
	{
		if (!TryBuildStaticAtlasPathLayoutKey(out var key) || !StaticAtlasPathLayoutCache.TryGetValue(key, out var value))
		{
			return false;
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		Rid rid = texture2DArray.GetRid();
		texture2DArray.Dispose();
		StaticAtlasPathLayoutCacheEntry staticAtlasPathLayoutCacheEntry = new StaticAtlasPathLayoutCacheEntry(in key, value.Data, value.Active, value.Paths, value.Rects, value.Pages, texture2DArray, rid, value.TextureArraySize);
		StaticAtlasPathLayoutCache[key] = staticAtlasPathLayoutCacheEntry;
		_appliedStaticAtlasPathLayout = staticAtlasPathLayoutCacheEntry;
		return true;
	}

	private bool TryApplyStaticAtlasPathLayoutCache()
	{
		if (!TryBuildStaticAtlasPathLayoutKey(out var key))
		{
			return false;
		}
		RefreshStaticAtlasPathLayoutCacheVersion(key.CacheVersion);
		if (!StaticAtlasPathLayoutCache.TryGetValue(key, out var value))
		{
			return false;
		}
		if (!VerifyStaticAtlasPathLayoutEntry(value, in key))
		{
			StaticAtlasPathLayoutCache.Remove(key);
			return false;
		}
		ApplyStaticAtlasPathLayoutEntry(value);
		return true;
	}

	private bool TryPublishStaticAtlasPathLayoutCache()
	{
		if (!mediaReplaceAtlasShared || !mediaReplaceAtlasUsesTextureArray || !GodotObject.IsInstanceValid(mediaReplaceAtlasArray) || !TryBuildStaticAtlasPathLayoutKey(out var key))
		{
			return false;
		}
		Rid rid = mediaReplaceAtlasArray.GetRid();
		if (!rid.IsValid)
		{
			return false;
		}
		RefreshStaticAtlasPathLayoutCacheVersion(key.CacheVersion);
		if (StaticAtlasPathLayoutCache.TryGetValue(key, out var value))
		{
			if (VerifyStaticAtlasPathLayoutEntry(value, in key))
			{
				_appliedStaticAtlasPathLayout = value;
				return true;
			}
			StaticAtlasPathLayoutCache.Remove(key);
		}
		bool[] array = new bool[key.EffectiveLimit];
		string[] array2 = new string[key.EffectiveLimit];
		Rect2[] array3 = new Rect2[key.MediaCount];
		int[] array4 = new int[key.MediaCount];
		for (int i = 0; i < key.EffectiveLimit; i++)
		{
			array[i] = _staticAtlasPathSnapshotActive[i];
			array2[i] = _staticAtlasPathSnapshotPaths[i];
		}
		for (int j = 0; j < key.MediaCount; j++)
		{
			array3[j] = mediaReplaceRect[j];
			array4[j] = mediaReplaceAtlasPages[j];
		}
		if (StaticAtlasPathLayoutCache.Count >= 256)
		{
			StaticAtlasPathLayoutCache.Clear();
		}
		StaticAtlasPathLayoutCacheEntry staticAtlasPathLayoutCacheEntry = new StaticAtlasPathLayoutCacheEntry(in key, _flashAnimeData, array, array2, array3, array4, mediaReplaceAtlasArray, rid, mediaReplaceAtlasArraySize);
		StaticAtlasPathLayoutCache.Add(key, staticAtlasPathLayoutCacheEntry);
		_appliedStaticAtlasPathLayout = staticAtlasPathLayoutCacheEntry;
		return true;
	}

	private bool TryBuildStaticAtlasPathLayoutKey(out StaticAtlasPathLayoutKey key)
	{
		key = default(StaticAtlasPathLayoutKey);
		if (_staticAtlasPathLayoutRuntimeMutationSeen || _atlasProfileOverride != null || _flashAnimeData == null || !_staticAtlasPathSnapshotValid || _staticAtlasPathSnapshotData != _flashAnimeData || _staticAtlasPathSnapshotAuthoringRevision != _flashAnimeData.AuthoringRevision)
		{
			return false;
		}
		int count = _flashAnimeData.mediaDictionary.Count;
		int num = _mediaReplace?.Count ?? 0;
		int num2 = _mediaReplaceUse?.Count ?? 0;
		int num3 = _mediaReplaceAtlasPaths?.Count ?? 0;
		int num4 = Math.Min(num, num2);
		if (count != _staticAtlasPathSnapshotMediaCount || num != _staticAtlasPathSnapshotTextureCount || num2 != _staticAtlasPathSnapshotUseCount || num3 != _staticAtlasPathSnapshotPathCount || num4 != _staticAtlasPathSnapshotEffectiveLimit)
		{
			return false;
		}
		key = new StaticAtlasPathLayoutKey(AdobeAnimateGlobalAtlasCache.CacheVersion, _staticAtlasPathSnapshotDataInstanceId, _staticAtlasPathSnapshotAuthoringRevision, count, num, num2, num3, num4, _staticAtlasPathSnapshotActiveCount, _staticAtlasPathSnapshotSlotSignature);
		return true;
	}

	private bool VerifyStaticAtlasPathLayoutEntry(StaticAtlasPathLayoutCacheEntry entry, in StaticAtlasPathLayoutKey key)
	{
		if (entry == null || !entry.Key.Equals(key) || entry.Data != _flashAnimeData || entry.Active.Length != key.EffectiveLimit || entry.Paths.Length != key.EffectiveLimit || entry.Rects.Length != key.MediaCount || entry.Pages.Length != key.MediaCount || !GodotObject.IsInstanceValid(entry.TextureArray) || !entry.TextureArrayRid.IsValid || entry.TextureArray.GetRid() != entry.TextureArrayRid)
		{
			return false;
		}
		for (int i = 0; i < key.EffectiveLimit; i++)
		{
			bool flag = _staticAtlasPathSnapshotActive[i];
			string b = _staticAtlasPathSnapshotPaths[i];
			if (entry.Active[i] != flag || !string.Equals(entry.Paths[i], b, StringComparison.Ordinal))
			{
				return false;
			}
		}
		return true;
	}

	private bool TryReuseAppliedStaticAtlasPathLayout()
	{
		StaticAtlasPathLayoutCacheEntry appliedStaticAtlasPathLayout = _appliedStaticAtlasPathLayout;
		if (appliedStaticAtlasPathLayout != null && !_staticAtlasPathLayoutRuntimeMutationSeen && appliedStaticAtlasPathLayout.Data == _flashAnimeData && appliedStaticAtlasPathLayout.Key.CacheVersion == AdobeAnimateGlobalAtlasCache.CacheVersion && appliedStaticAtlasPathLayout.Key.AuthoringRevision == (_flashAnimeData?.AuthoringRevision ?? (-9223372036854775808L)) && GodotObject.IsInstanceValid(appliedStaticAtlasPathLayout.TextureArray) && appliedStaticAtlasPathLayout.TextureArrayRid.IsValid && appliedStaticAtlasPathLayout.TextureArray.GetRid() == appliedStaticAtlasPathLayout.TextureArrayRid && mediaReplaceAtlasArray == appliedStaticAtlasPathLayout.TextureArray && mediaReplaceAtlasUsesTextureArray)
		{
			return mediaReplaceAtlasShared;
		}
		return false;
	}

	private void ApplyStaticAtlasPathLayoutEntry(StaticAtlasPathLayoutCacheEntry entry)
	{
		int mediaCount = entry.Key.MediaCount;
		if (mediaReplaceRect.Count != mediaCount)
		{
			mediaReplaceRect.Clear();
			mediaReplaceRect.Resize(mediaCount);
		}
		if (mediaReplaceAtlasPages.Count != mediaCount)
		{
			mediaReplaceAtlasPages.Clear();
			mediaReplaceAtlasPages.Resize(mediaCount);
		}
		for (int i = 0; i < mediaCount; i++)
		{
			mediaReplaceRect[i] = entry.Rects[i];
			mediaReplaceAtlasPages[i] = entry.Pages[i];
		}
		mediaReplaceAtlas = null;
		mediaReplaceAtlasArray = entry.TextureArray;
		mediaReplaceAtlasArraySize = entry.TextureArraySize;
		mediaReplaceAtlasUsesTextureArray = true;
		mediaReplaceAtlasShared = true;
		_appliedStaticAtlasPathLayout = entry;
	}

	private static void RefreshStaticAtlasPathLayoutCacheVersion(int cacheVersion)
	{
		if (StaticAtlasPathLayoutCacheVersion != cacheVersion)
		{
			StaticAtlasPathLayoutCache.Clear();
			StaticAtlasPathLayoutCacheVersion = cacheVersion;
		}
	}

	internal AdobeAnimateGpuClockState BuildRuntimeGpuClockStateForBareTest(float frameFloat, float animationClockSeconds)
	{
		return BuildRuntimeGpuClockState(frameFloat, animationClockSeconds);
	}

	internal AdobeAnimateGpuClockState BuildGpuGraphClockForBareTest(float frameFloat, float animationClockSeconds, bool enableGpuClock)
	{
		return BuildGpuGraphOwnerStateForRender(SharedGpuClockBareTestDefinition, Transform2D.Identity, Colors.White, frameFloat, enableGpuClock, animationClockSeconds).GpuClock;
	}

	internal void ConfigureGpuClockForBareTest(bool paused, bool reachedClipEnd, double delaySeconds, int clipStart, int clipEndExclusive, double framesPerSecond, double timeScaleValue, bool reverse, bool shouldLoop, bool dispatchActive = true)
	{
		_usingRuntimeManager = true;
		_pause = paused;
		_playbackBlocked = false;
		_parentPlaybackStopped = false;
		_runtimeManagerDispatchActive = dispatchActive;
		clipOver = reachedClipEnd;
		_nextAnimDelayTimer = delaySeconds;
		clipRange = new Vector2I(clipStart, clipEndExclusive);
		frameRate = framesPerSecond;
		timeScale = timeScaleValue;
		playBack = reverse;
		loop = shouldLoop;
	}

	internal void ApplyRuntimeParentStateForBareTest(AdobeAnimateSprite parent)
	{
		ApplyRuntimeParentState(parent);
	}

	internal void ClearRenderSubmissionForBareTest()
	{
		_needsRenderSubmission = false;
	}
}
