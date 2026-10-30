using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using Godot;
using Godot.Collections;

public partial class BulletField : Node2D
{
	public enum BulletLifecycleTerminalReason
	{
		Landed,
		BlockedBounce,
		Despawned
	}

	public delegate void BulletLifecycleEndedEventHandler(TowerDefenseCharacter source, int ownerSequence, int slot, BulletLifecycleTerminalReason reason);

	public delegate void LandOverEventHandler(Vector2 pos, Vector2I gridPos, ulong sourceInstanceId);

	private readonly struct DurabilitySweepCandidate(TowerDefenseCharacter character, float entryDistance)
	{
		public readonly TowerDefenseCharacter Character = character;

		public readonly float EntryDistance = entryDistance;
	}

	private readonly struct CollisionCandidateQueryKey(int minColumn, int maxColumn, int line, int minRow, int maxRow) : IEquatable<CollisionCandidateQueryKey>
	{
		public readonly int MinColumn = minColumn;

		public readonly int MaxColumn = maxColumn;

		public readonly int Line = line;

		public readonly int MinRow = minRow;

		public readonly int MaxRow = maxRow;

		public bool Equals(CollisionCandidateQueryKey other)
		{
			if (MinColumn == other.MinColumn && MaxColumn == other.MaxColumn && Line == other.Line && MinRow == other.MinRow)
			{
				return MaxRow == other.MaxRow;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is CollisionCandidateQueryKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(MinColumn, MaxColumn, Line, MinRow, MaxRow);
		}
	}

	private readonly struct CollisionOverlapQueryKey(Rect2 rect, int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp) : IEquatable<CollisionOverlapQueryKey>
	{
		public readonly Rect2 Rect = rect;

		public readonly int Line = line;

		public readonly TowerDefenseEnum.CHARACTER_CAMP ExcludedCamp = excludedCamp;

		public bool Equals(CollisionOverlapQueryKey other)
		{
			if (Rect.Equals(other.Rect) && Line == other.Line)
			{
				return ExcludedCamp == other.ExcludedCamp;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is CollisionOverlapQueryKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Rect, Line, ExcludedCamp);
		}
	}

	private sealed class AnimatedMeshBuildJob
	{
		internal AnimateMultiMeshRenderer.UnifiedBucket Bucket;

		internal readonly List<AnimatedMeshBuildSegment> Segments = new List<AnimatedMeshBuildSegment>(16);

		internal int BulletCount;
	}

	private readonly struct AnimatedMeshBuildSegment(List<int> row, int start, int count)
	{
		internal readonly List<int> Row = row;

		internal readonly int Start = start;

		internal readonly int Count = count;
	}

	internal readonly struct PreparedProjectileChange(bool isValid, string errorReason, BulletRenderMode renderMode, int animationDefinitionId, Vector2 animationOffset, int animationFrameMax, double animationFrameRate, int atlasCacheVersion, TowerDefenseProjectileConfig config, int behaviorProgramId = 0)
	{
		public readonly bool IsValid = isValid;

		public readonly string ErrorReason = errorReason;

		public readonly BulletRenderMode RenderMode = renderMode;

		public readonly int AnimationDefinitionId = animationDefinitionId;

		public readonly Vector2 AnimationOffset = animationOffset;

		public readonly float AnimationRotation = (isValid ? GetSceneAnimRotation(config.projectileScene) : 0f);

		public readonly int AnimationFrameMax = animationFrameMax;

		public readonly double AnimationFrameRate = animationFrameRate;

		public readonly int AtlasCacheVersion = atlasCacheVersion;

		public readonly int FireMethodFlags = config?.fireMethodFlags ?? 0;

		public readonly int DamageFlags = config?.damageFlags ?? 0;

		public readonly float RotateScale = ((config != null) ? ((float)config.rotateScale) : 0f);

		public readonly bool RotateFollowVelocity = config?.rotateFollowVelocity ?? false;

		public readonly int TrackSearchInterval = config?.trackSearchInterval ?? 0;

		public readonly int PenetrateNum = config?.penetrateNum ?? 0;

		public readonly bool CanReuseCollisionGeometry = CanReuseCollisionGeometry(config);

		public readonly int BehaviorProgramId = behaviorProgramId;
	}

	private enum BulletChangeInPlaceResult
	{
		RequiresReplacement,
		Changed,
		Rejected
	}

	private readonly struct StaticBucketKey(int gridY, ulong textureArrayId, int atlasLayer) : IEquatable<StaticBucketKey>
	{
		public readonly int GridY = gridY;

		public readonly ulong TextureArrayId = textureArrayId;

		public readonly int AtlasLayer = Math.Max(0, atlasLayer);

		public bool Equals(StaticBucketKey other)
		{
			if (GridY == other.GridY && TextureArrayId == other.TextureArrayId)
			{
				return AtlasLayer == other.AtlasLayer;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is StaticBucketKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(GridY, TextureArrayId, AtlasLayer);
		}
	}

	private readonly struct StaticMaterialKey(TextureLayered textureArray, int atlasLayer) : IEquatable<StaticMaterialKey>
	{
		private readonly ulong _textureArrayId = (GodotObject.IsInstanceValid(textureArray) ? textureArray.GetInstanceId() : 0);

		private readonly int _atlasLayer = Math.Max(0, atlasLayer);

		public bool Equals(StaticMaterialKey other)
		{
			if (_textureArrayId == other._textureArrayId)
			{
				return _atlasLayer == other._atlasLayer;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is StaticMaterialKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_textureArrayId, _atlasLayer);
		}
	}

	private sealed class StaticBucket
	{
		public MultiMeshInstance2D Instance;

		public MultiMesh MultiMesh;

		public float[] Buffer = System.Array.Empty<float>();

		public int Count;

		public int UnderusedFrames;

		public long LastTouchedFrame;
	}

	private sealed class SpriteAtlasEntry
	{
		public Texture2D texture;

		public Rect2 region;

		public TextureLayered atlasTextureArray;

		public ulong atlasTextureArrayId;

		public Vector2 atlasTextureArraySize;

		public int atlasLayer;

		public Vector2 size;

		public Vector2 uvOffset;

		public Vector2 uvSize;

		public Vector2 position;

		public bool centered;

		public Vector2 offset;

		public bool runtimePacked;
	}

	private readonly struct ProjectileRenderTemplate(bool isValid, string errorCode, string errorReason, BulletRenderMode renderMode, int animationDefinitionId, Vector2 animationOffset, float animationRotation, double animationTimeScale, int atlasCacheVersion)
	{
		public readonly bool IsValid = isValid;

		public readonly string ErrorCode = errorCode;

		public readonly string ErrorReason = errorReason;

		public readonly BulletRenderMode RenderMode = renderMode;

		public readonly int AnimationDefinitionId = animationDefinitionId;

		public readonly Vector2 AnimationOffset = animationOffset;

		public readonly float AnimationRotation = animationRotation;

		public readonly double AnimationTimeScale = animationTimeScale;

		public readonly int AtlasCacheVersion = atlasCacheVersion;
	}

	private sealed class ProjectileZoneFrameEntry
	{
		public readonly IProjectileZone Zone;

		public int GridY;

		public int RowSpan;

		public Rect2 WorldRect;

		public float MinX;

		public float MaxX;

		public float MinY;

		public float MaxY;

		public ProjectileZoneFrameEntry(IProjectileZone zone, int gridY, int rowSpan)
		{
			Zone = zone;
			GridY = gridY;
			RowSpan = rowSpan;
			SetWorldRect(zone.WorldRect);
		}

		public void RefreshWorldRect()
		{
			Zone.UpdateRect();
			SetWorldRect(Zone.WorldRect);
		}

		private void SetWorldRect(Rect2 rect)
		{
			WorldRect = rect;
			Vector2 position = rect.Position;
			Vector2 size = rect.Size;
			MinX = position.X;
			MaxX = position.X + size.X;
			MinY = position.Y;
			MaxY = position.Y + size.Y;
		}
	}

	private sealed class BulletBehaviorProgram
	{
		public readonly ProjectileBehaviorKernel[] Kernels;

		public readonly bool[] Disabled;

		public readonly string DiagnosticName;

		public BulletBehaviorProgram(ProjectileBehaviorKernel[] kernels, string diagnosticName)
		{
			Kernels = kernels;
			Disabled = new bool[kernels.Length];
			DiagnosticName = diagnosticName;
		}
	}

	private delegate void KernelCallback(ProjectileBehaviorKernel kernel, int index, ref BulletData bullet);

	internal static readonly Vector2 DefaultProjectileHitHalfSize = new Vector2(15f, 15f);

	private const int Capacity = 65536;

	private readonly BulletData[] _data = new BulletData[65536];

	private readonly int[] _freeList = new int[65536];

	private readonly int[] _activeIndices = new int[65536];

	private readonly int[] _activeSlots = new int[65536];

	private int _freeCount;

	private int _activeCount;

	private bool _publicationContinuityProbeEnabled;

	internal int _animMeshCount;

	private double _animMeshGlobalTimer;

	private readonly System.Collections.Generic.Dictionary<int, List<int>> _rowBuckets = new System.Collections.Generic.Dictionary<int, List<int>>();

	private readonly List<TowerDefenseCharacter> _collisionCandidates = new List<TowerDefenseCharacter>();

	private readonly List<TowerDefenseCharacter> _collisionOverlapScratch = new List<TowerDefenseCharacter>();

	private readonly List<DurabilitySweepCandidate> _durabilitySweepCandidates = new List<DurabilitySweepCandidate>();

	private readonly System.Collections.Generic.Dictionary<CollisionCandidateQueryKey, int> _collisionCandidateCacheLookup = new System.Collections.Generic.Dictionary<CollisionCandidateQueryKey, int>();

	private readonly List<List<TowerDefenseCharacter>> _collisionCandidateCacheEntries = new List<List<TowerDefenseCharacter>>();

	private int _collisionCandidateCacheEntryCount;

	private CollisionCandidateQueryKey _collisionCandidateCacheMostRecentKey;

	private int _collisionCandidateCacheMostRecentIndex = -1;

	private bool _collisionCandidateCacheHasMostRecent;

	private readonly System.Collections.Generic.Dictionary<CollisionOverlapQueryKey, int> _collisionOverlapCacheLookup = new System.Collections.Generic.Dictionary<CollisionOverlapQueryKey, int>();

	private readonly List<List<TowerDefenseCharacter>> _collisionOverlapCacheEntries = new List<List<TowerDefenseCharacter>>();

	private int _collisionOverlapCacheEntryCount;

	private CollisionOverlapQueryKey _collisionOverlapCacheMostRecentKey;

	private int _collisionOverlapCacheMostRecentIndex = -1;

	private bool _collisionOverlapCacheHasMostRecent;

	private ulong _collisionOverlapCacheGeometryRevision;

	private int _collisionOverlapCacheBuildCountLastUpdate;

	private int _collisionOverlapCacheHitCountLastUpdate;

	private ulong _collisionCandidateCacheRevision;

	private int _collisionCandidateCacheBuildCountLastUpdate;

	private int _collisionCandidateCacheHitCountLastUpdate;

	private const int DirectCampFilteredLineCandidateThreshold = 32;

	private const int PenetrationCurrentTargetInlineCapacity = 16;

	private readonly List<ulong> _penetratingTargetIdsThisFrame = new List<ulong>(16);

	private readonly HashSet<ulong> _penetratingOverflowTargetIdsThisFrame = new HashSet<ulong>();

	private readonly List<ulong> _penetratingTargetIdsToRemove = new List<ulong>();

	private const int PenetrationInlineTargetCapacity = 8;

	private const int PenetrationSecondaryTargetCapacity = 8;

	private const int PenetrationTertiaryTargetCapacity = 8;

	private readonly ulong[] _penetrationInlineTargetIds = new ulong[524288];

	private readonly byte[] _penetrationInlineTargetCounts = new byte[65536];

	private readonly ulong[] _penetrationSecondaryTargetIds = new ulong[524288];

	private readonly byte[] _penetrationSecondaryTargetCounts = new byte[65536];

	private readonly ulong[] _penetrationTertiaryTargetIds = new ulong[524288];

	private readonly byte[] _penetrationTertiaryTargetCounts = new byte[65536];

	private int _penetrationScanTargetOrdinal;

	private bool _penetrationRememberedSetEmptyAtScanStart;

	private int _penetrationRememberedOrderFastPathCountLastUpdate;

	private int _penetrationOverlapSetFastPathCountLastUpdate;

	private int _penetrationOverlapSetCleanupCountLastUpdate;

	private TowerDefenseBattleCharacterRegistry _collisionRegistryForFrame;

	private bool _hasRegisteredCharactersForFrame;

	private ulong _collisionPhysicsFrameForUpdate;

	private int _collisionMapColumnCountForFrame;

	private int _collisionMapRowCountForFrame;

	private Vector2 _collisionGridSizeForFrame;

	private Vector2 _collisionGridBeginForFrame;

	private TowerDefenseCellInstance[][] _mapGridCellsForFrame = System.Array.Empty<TowerDefenseCellInstance[]>();

	private int[] _mapGridRowCountsForFrame = System.Array.Empty<int>();

	private int _mapGridColumnCountForFrame;

	private TowerDefenseBattleFeatureMap _mapGridSnapshotFeature;

	private ulong _mapGridSnapshotRevision;

	private const double Gravity = 245.0;

	private const double CatapultGravity = 2000.0;

	private const int TrackSearchBudget = 10;

	private int _trackSearchBudgetUsed;

	private int _trackSearchBudgetFrame = -1;

	private const int EffectSpawnBudget = 50;

	private int _effectSpawnUsed;

	private long _effectSpawnFrame = -1L;

	private static TowerDefenseProjectile _eventProxy;

	private static Node2D _eventProxyBodyNode;

	private readonly System.Collections.Generic.Dictionary<int, Variant> _eventMetadataByIndex = new System.Collections.Generic.Dictionary<int, Variant>();

	private readonly System.Collections.Generic.Dictionary<int, BulletFieldStoredProjectile[]> _eventProjectilesByIndex = new System.Collections.Generic.Dictionary<int, BulletFieldStoredProjectile[]>();

	private Action _requestSourceDespawnAction;

	private static readonly Action IgnoreGuaranteedSourceDespawnAction = IgnoreGuaranteedSourceDespawn;

	private bool[] _sourceDespawnRequests = new bool[4];

	private int _sourceDespawnRequestDepth = -1;

	public static bool DrawCollisionDebug = false;

	private const int IncapacitatedLaneSearchRange = 20;

	private const int AnimatedMeshParallelBuildThreshold = 8192;

	private AnimateMultiMeshRenderer _animRenderer;

	private bool _animRendererInit;

	private AnimateMultiMeshRenderer.SimpleDrawContext[] _animMeshDrawContexts = System.Array.Empty<AnimateMultiMeshRenderer.SimpleDrawContext>();

	private AdobeAnimateZIndexCrowdBucket[] _animMeshDrawRenderBuckets = System.Array.Empty<AdobeAnimateZIndexCrowdBucket>();

	private int[] _animMeshDrawMetadataBaseTexels = System.Array.Empty<int>();

	private int[] _animMeshDrawRootCounts = System.Array.Empty<int>();

	private int[] _animMeshTouchedDefinitionIds = System.Array.Empty<int>();

	private int[] _animMeshDrawContextRows = System.Array.Empty<int>();

	private int _animMeshDrawContextToken;

	private readonly System.Collections.Generic.Dictionary<AnimateMultiMeshRenderer.UnifiedBucket, AnimatedMeshBuildJob> _animMeshBuildJobLookup = new System.Collections.Generic.Dictionary<AnimateMultiMeshRenderer.UnifiedBucket, AnimatedMeshBuildJob>();

	private readonly List<AnimatedMeshBuildJob> _animMeshBuildJobPool = new List<AnimatedMeshBuildJob>();

	private readonly List<AnimatedMeshBuildJob> _animMeshActiveBuildJobs = new List<AnimatedMeshBuildJob>();

	private readonly System.Collections.Generic.Dictionary<int, int> _animMeshDefinitionCounts = new System.Collections.Generic.Dictionary<int, int>();

	private readonly int[] _animMeshShadowCandidateIndices = new int[65536];

	private int _animMeshShadowCandidateCount;

	private Action<int> _animatedMeshBuildWorker;

	private readonly System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, PreparedProjectileChange> _preparedProjectileChanges = new System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, PreparedProjectileChange>();

	private TowerDefenseProjectileConfig _lastPreparedProjectileChangeConfig;

	private PreparedProjectileChange _lastPreparedProjectileChange;

	private const int GridRefreshInterval = 5;

	private const double NoShooterGroundBlock = -1.0 / 0.0;

	private readonly byte[] _gridRefreshPhases = new byte[65536];

	private readonly double[] _shooterGroundBlockHeights = new double[65536];

	private byte _gridRefreshPhaseForFrame;

	private double[][] _mapGridGroundHeightsForFrame = System.Array.Empty<double[]>();

	private TowerDefenseBattleFeatureMap _groundHeightSnapshotFeature;

	private ulong _groundHeightSnapshotRevision;

	private int _groundHeightSnapshotColumnCount;

	private bool _groundHeightSnapshotContainsCurve;

	private bool _groundHeightSnapshotDirty = true;

	private readonly List<Resource> _groundHeightSnapshotResources = new List<Resource>();

	private const int ParallelMotionThreshold = 20000;

	private const byte ParallelMotionHandled = 1;

	private const byte ParallelMotionRefreshGrid = 2;

	private const byte ParallelMotionDespawn = 4;

	private const byte ParallelMotionShadowCandidate = 8;

	private readonly byte[] _parallelMotionResults = new byte[65536];

	private Action<int, int> _parallelMotionRangeWorker;

	private double _parallelMotionDelta;

	private ulong _parallelMotionFrame;

	private bool _parallelMotionWasActive;

	private int _parallelMotionNeedsReconcile;

	private readonly System.Collections.Generic.Dictionary<StaticBucketKey, StaticBucket> _staticBuckets = new System.Collections.Generic.Dictionary<StaticBucketKey, StaticBucket>();

	private readonly List<StaticBucketKey> _staticTouchedBucketsLastFrame = new List<StaticBucketKey>(16);

	private readonly List<StaticBucketKey> _staticTouchedBucketsThisFrame = new List<StaticBucketKey>(16);

	private readonly List<StaticBucketKey> _staticBucketKeysToRemove = new List<StaticBucketKey>(16);

	private readonly System.Collections.Generic.Dictionary<StaticMaterialKey, ShaderMaterial> _staticShaderMaterials = new System.Collections.Generic.Dictionary<StaticMaterialKey, ShaderMaterial>();

	private readonly System.Collections.Generic.Dictionary<Rid, (TextureLayered TextureArray, Vector2 Size)> _staticSingleLayerTextureArrays = new System.Collections.Generic.Dictionary<Rid, (TextureLayered, Vector2)>();

	private readonly System.Collections.Generic.Dictionary<long, int> _configToAtlasIndex = new System.Collections.Generic.Dictionary<long, int>();

	private readonly List<SpriteAtlasEntry> _atlasEntries = new List<SpriteAtlasEntry>();

	private bool _atlasDirty = true;

	private TextureLayered _atlasTextureArray;

	private Shader _staticShader;

	private QuadMesh _sharedQuadMesh;

	private bool _rendererInitialized;

	private int _observedAtlasCacheVersion = -1;

	private long _staticFrameVersion;

	private long _lastStaticBucketMaintenanceFrame;

	private long _bulletShadowPhysicsFrame;

	private bool _bulletShadowSubmissionPrepared;

	private Texture2D _bulletShadowTexture;

	private Vector2 _bulletShadowTextureSize;

	private Vector2 _bulletShadowScaleBase;

	private TowerDefenseShadowMultiMeshRenderer.SubmissionContext _bulletShadowSubmission;

	private static readonly Color BulletShadowColor = new Color(0f, 0f, 0f, 0.4f);

	private const int StaticInitialBucketCapacity = 64;

	private const int StaticBucketShrinkDelayFrames = 90;

	private const int StaticBucketShrinkRatio = 4;

	private const int StaticBucketReclaimIntervalFrames = 120;

	private const int StaticBufferStride = 16;

	private const int StaticVisualTextureArrayLayerSize = 2048;

	private const string StaticMultiMeshShaderCode = "\nshader_type canvas_item;\n\nuniform sampler2DArray atlasTextureArray : source_color, repeat_disable, filter_linear;\nuniform float atlasLayer = 0.0;\n\nvoid vertex() {\n\tvec2 uvOffset = INSTANCE_CUSTOM.xy;\n\tvec2 uvSize = INSTANCE_CUSTOM.zw;\n\tUV = uvOffset + vec2(UV.x, 1.0 - UV.y) * uvSize;\n}\n\nvoid fragment() {\n\tCOLOR = texture(atlasTextureArray, vec3(UV, floor(atlasLayer + 0.5)));\n}\n\t";

	private readonly System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, ProjectileRenderTemplate> _renderTemplateCache = new System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, ProjectileRenderTemplate>();

	private readonly System.Collections.Generic.Dictionary<(StringName ProjectileName, StringName SkinName), int> _warmedRenderTemplateVersions = new System.Collections.Generic.Dictionary<(StringName, StringName), int>();

	private readonly HashSet<(StringName Name, string ScenePath, string Code, int AtlasVersion)> _reportedSpawnErrors = new HashSet<(StringName, string, string, int)>();

	private static readonly Queue<(StringName ProjectileName, StringName SkinName)> _renderTemplateWarmupQueue = new Queue<(StringName, StringName)>();

	private static readonly HashSet<(StringName ProjectileName, StringName SkinName)> _queuedRenderTemplateWarmups = new HashSet<(StringName, StringName)>();

	private uint _spawnRandomState = 2654435769u;

	private static readonly System.Collections.Generic.Dictionary<PackedScene, AdobeAnimateData> _sceneAnimeDataCache = new System.Collections.Generic.Dictionary<PackedScene, AdobeAnimateData>();

	private static readonly System.Collections.Generic.Dictionary<PackedScene, Vector2> _sceneAnimOffsetCache = new System.Collections.Generic.Dictionary<PackedScene, Vector2>();

	private static readonly System.Collections.Generic.Dictionary<PackedScene, float> _sceneAnimRotationCache = new System.Collections.Generic.Dictionary<PackedScene, float>();

	private static readonly System.Collections.Generic.Dictionary<PackedScene, string> _sceneAnimClipCache = new System.Collections.Generic.Dictionary<PackedScene, string>();

	private static readonly System.Collections.Generic.Dictionary<PackedScene, double> _sceneAnimTimeScaleCache = new System.Collections.Generic.Dictionary<PackedScene, double>();

	private static readonly System.Collections.Generic.Dictionary<PackedScene, bool> _sceneAnimatedMeshCompatibleCache = new System.Collections.Generic.Dictionary<PackedScene, bool>();

	private static readonly System.Collections.Generic.Dictionary<(StringName, StringName), TowerDefenseProjectileConfig> _templateConfigCache = new System.Collections.Generic.Dictionary<(StringName, StringName), TowerDefenseProjectileConfig>();

	private BulletFieldWorkerPool _workerPool;

	private readonly List<ProjectileZoneFrameEntry> _allZones = new List<ProjectileZoneFrameEntry>();

	private readonly System.Collections.Generic.Dictionary<int, List<ProjectileZoneFrameEntry>> _zonesByRow = new System.Collections.Generic.Dictionary<int, List<ProjectileZoneFrameEntry>>();

	private readonly System.Collections.Generic.Dictionary<IProjectileZone, ProjectileZoneFrameEntry> _zoneEntries = new System.Collections.Generic.Dictionary<IProjectileZone, ProjectileZoneFrameEntry>();

	private readonly List<BulletBehaviorProgram> _behaviorPrograms = new List<BulletBehaviorProgram> { null };

	private readonly System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, int> _behaviorProgramByConfig = new System.Collections.Generic.Dictionary<TowerDefenseProjectileConfig, int>();

	public static BulletField Instance { get; private set; }

	public int ActiveCount => _activeCount;

	public int PublicationMismatchFramesForTest { get; private set; }

	public int PublicationMaximumDeficitForTest { get; private set; }

	public int PublicationMaximumSurplusForTest { get; private set; }

	public int LastSpawnedIndex { get; private set; } = -1;

	internal int CollisionCandidateCacheBuildCountForTest => _collisionCandidateCacheBuildCountLastUpdate;

	internal int CollisionCandidateCacheHitCountForTest => _collisionCandidateCacheHitCountLastUpdate;

	internal int CollisionOverlapCacheBuildCountForTest => _collisionOverlapCacheBuildCountLastUpdate;

	internal int CollisionOverlapCacheHitCountForTest => _collisionOverlapCacheHitCountLastUpdate;

	private int CurrentPenetrationTargetCount => _penetratingTargetIdsThisFrame.Count + _penetratingOverflowTargetIdsThisFrame.Count;

	internal int PenetrationInlineTargetCapacityForTest => 24;

	internal int PenetrationOverlapSetFastPathCountForTest => _penetrationOverlapSetFastPathCountLastUpdate;

	internal int PenetrationRememberedOrderFastPathCountForTest => _penetrationRememberedOrderFastPathCountLastUpdate;

	internal int PenetrationOverlapSetCleanupCountForTest => _penetrationOverlapSetCleanupCountLastUpdate;

	private AnimateMultiMeshRenderer AnimRenderer
	{
		get
		{
			if (!_animRendererInit)
			{
				_animRendererInit = true;
				_animRenderer = new AnimateMultiMeshRenderer();
				AddChild(_animRenderer, forceReadableName: false, InternalMode.Disabled);
			}
			return _animRenderer;
		}
	}

	private bool ParallelMotionNeedsReconciliation => Volatile.Read(in _parallelMotionNeedsReconcile) != 0;

	internal int CurrentZonePhysicsFrame => _trackSearchBudgetFrame;

	public event BulletLifecycleEndedEventHandler OnBulletLifecycleEnded;

	public static event LandOverEventHandler OnLandOver;

	public event Action<int> OnBulletSpawned;

	internal static bool ShouldDurabilityBlockingSweepStop(double currentDurability, double threshold)
	{
		return currentDurability >= threshold;
	}

	private bool CanSpawnEffectThisFrame()
	{
		if (_effectSpawnUsed >= 50)
		{
			return false;
		}
		_effectSpawnUsed++;
		return true;
	}

	private static void IgnoreGuaranteedSourceDespawn()
	{
	}

	public override void _Ready()
	{
		Instance = this;
		InitializeWorkerPool();
		for (int i = 0; i < 65536; i++)
		{
			_freeList[i] = 65535 - i;
			_activeIndices[i] = -1;
			_activeSlots[i] = -1;
		}
		_freeCount = 65536;
		RegisterExistingZones(GetParent());
		if (_eventProxy == null)
		{
			_eventProxy = new TowerDefenseProjectile();
			_eventProxyBodyNode = new Node2D();
			_eventProxy.projectileBodyNode = _eventProxyBodyNode;
			_eventProxy.shadowSprite = TowerDefenseShadowVisual.Create(_eventProxy);
		}
		if (_requestSourceDespawnAction == null)
		{
			_requestSourceDespawnAction = RequestSourceDespawn;
		}
	}

	public override void _ExitTree()
	{
		DisposeGroundHeightSnapshot();
		DisposeWorkerPool();
		if (Instance == this)
		{
			Instance = null;
			ReleaseEventProxy();
			_eventMetadataByIndex.Clear();
			_eventProjectilesByIndex.Clear();
			ClearRuntimeCaches();
		}
		base._ExitTree();
	}

	private static void ReleaseEventProxy()
	{
		if (GodotObject.IsInstanceValid(_eventProxy))
		{
			_eventProxy.projectileBodyNode = null;
			_eventProxy.shadowSprite = null;
			_eventProxy.Free();
		}
		_eventProxy = null;
		if (GodotObject.IsInstanceValid(_eventProxyBodyNode))
		{
			_eventProxyBodyNode.Free();
		}
		_eventProxyBodyNode = null;
	}

	private void AddActiveIndex(int index)
	{
		_activeSlots[index] = _activeCount;
		_activeIndices[_activeCount] = index;
		_activeCount++;
	}

	private void RemoveActiveIndex(int index)
	{
		int num = _activeSlots[index];
		if (num >= 0 && num < _activeCount)
		{
			int num2 = _activeCount - 1;
			int num3 = _activeIndices[num2];
			if (num != num2)
			{
				_activeIndices[num] = num3;
				_activeSlots[num3] = num;
			}
			_activeIndices[num2] = -1;
			_activeSlots[index] = -1;
			_activeCount--;
		}
	}

	private static Vector2 GetShadowPos(ref BulletData b)
	{
		if (b.catapultOpen)
		{
			return new Vector2(b.pos.X, (float)((double)b.pos.Y + 20.0 - b.groundHeight));
		}
		float y = (float)((double)b.pos.Y + b.height + 40.0 - b.groundHeight);
		return new Vector2(b.pos.X, y);
	}

	private static Vector2 GetRenderPos(ref BulletData b)
	{
		if (b.absorbOpen)
		{
			return b.pos;
		}
		if (b.catapultOpen)
		{
			return new Vector2(b.pos.X, (float)((double)b.pos.Y - b.z));
		}
		if (b.useGravity || b.useFall)
		{
			return b.pos + new Vector2(0f, (float)(b.height + 20.0 - b.z));
		}
		return b.pos;
	}

	public int Spawn(in BulletData data)
	{
		if (_freeCount == 0)
		{
			return -1;
		}
		int num = _freeList[--_freeCount];
		bool flag = false;
		bool flag2 = false;
		bool flag3 = false;
		bool flag4 = false;
		try
		{
			BulletData bulletData = data;
			bulletData.active = true;
			bulletData.over = false;
			bulletData.canReuseCollisionGeometry = CanReuseCollisionGeometry(bulletData.config);
			bulletData.staticAtlasEntryIndex = ((bulletData.renderMode == BulletRenderMode.STATIC) ? EnsureSpriteRegistered(bulletData.config) : (-1));
			_data[num] = bulletData;
			InitializeBulletSpatialCaches(num, ref _data[num]);
			ClearPenetratingTargets(num, ref _data[num], releaseOverflow: true);
			AddActiveIndex(num);
			flag = true;
			AddToRowBucket(num, bulletData.gridY);
			flag2 = true;
			if (bulletData.renderMode == BulletRenderMode.ANIMATED_MESH)
			{
				_animMeshCount++;
				flag3 = true;
				AddAnimatedMeshDefinition(bulletData.animDefId);
				flag4 = true;
			}
			InitializeBulletBehavior(num, ref _data[num]);
		}
		catch
		{
			if (flag4)
			{
				RemoveAnimatedMeshDefinition(_data[num].animDefId);
			}
			if (flag3)
			{
				_animMeshCount--;
			}
			if (flag2)
			{
				RemoveFromRowBucket(num, _data[num].gridY);
			}
			if (flag)
			{
				RemoveActiveIndex(num);
			}
			ClearPenetratingTargets(num, ref _data[num], releaseOverflow: true);
			_eventMetadataByIndex.Remove(num);
			_eventProjectilesByIndex.Remove(num);
			_data[num] = default(BulletData);
			_freeList[_freeCount++] = num;
			throw;
		}
		LastSpawnedIndex = num;
		try
		{
			this.OnBulletSpawned?.Invoke(num);
		}
		catch (Exception ex)
		{
			GD.PushWarning("[BulletField] Spawn callback failed: " + ex.Message);
		}
		return num;
	}

	public void Despawn(int index)
	{
		ref BulletData reference = ref _data[index];
		if (reference.active)
		{
			TryEmitLifecycleTerminal(index, BulletLifecycleTerminalReason.Despawned);
			reference.active = false;
			ReleaseBulletBehavior(index, ref reference);
			RemoveActiveIndex(index);
			RemoveFromRowBucket(index, reference.gridY);
			if (reference.renderMode == BulletRenderMode.ANIMATED_MESH)
			{
				_animMeshCount--;
				RemoveAnimatedMeshDefinition(reference.animDefId);
			}
			reference.target = null;
			reference.magneticTarget = null;
			reference.cell = null;
			reference.fireCharacter = null;
			reference.config = null;
			reference.behaviorProgramId = 0;
			if (_eventMetadataByIndex.Count > 0)
			{
				_eventMetadataByIndex.Remove(index);
			}
			if (_eventProjectilesByIndex.Count > 0)
			{
				_eventProjectilesByIndex.Remove(index);
			}
			reference.canReuseCollisionGeometry = false;
			reference.staticAtlasEntryIndex = -1;
			ClearPenetratingTargets(index, ref reference, releaseOverflow: true);
			reference.lockGridY = false;
			reference.portalReleased = false;
			reference.zoneExclusionOwner = null;
			reference.zoneExclusionExpiryFrame = 0;
			reference.landOverSubscribed = false;
			reference.spawnSourceInstanceId = 0uL;
			reference.lifecycleSubscribed = false;
			reference.lifecycleTerminalEmitted = false;
			reference.lifecycleOwnerSequence = 0;
			reference.lifecycleSlot = -1;
			reference.externalControlled = false;
			reference.absorbOpen = false;
			reference.absorbDuration = 0f;
			reference.absorbTimer = 0f;
			reference.absorbScale = 1f;
			reference.absorbSpin = 0f;
			reference.absorbStartPos = Vector2.Zero;
			reference.absorbTargetPos = Vector2.Zero;
			reference.absorbControlPos = Vector2.Zero;
			reference.spawnTweenDuration = 0f;
			reference.spawnTweenTimer = 0f;
			reference.over = true;
			_freeList[_freeCount++] = index;
		}
	}

	private void TryEmitLifecycleTerminal(int index, BulletLifecycleTerminalReason reason)
	{
		ref BulletData reference = ref _data[index];
		if (reference.active && reference.lifecycleSubscribed && !reference.lifecycleTerminalEmitted)
		{
			reference.lifecycleTerminalEmitted = true;
			this.OnBulletLifecycleEnded?.Invoke(reference.fireCharacter, reference.lifecycleOwnerSequence, reference.lifecycleSlot, reason);
		}
	}

	public bool IsBulletActive(int index)
	{
		if (index < 0 || index >= 65536)
		{
			return false;
		}
		return _data[index].active;
	}

	public void StartRuntimeTween(int index, Vector2 targetPos, float duration, Tween.EaseType ease = Tween.EaseType.Out, Tween.TransitionType trans = Tween.TransitionType.Quart)
	{
		if (index >= 0 && index < 65536 && _data[index].active)
		{
			ref BulletData reference = ref _data[index];
			reference.spawnTweenStartPos = reference.pos;
			reference.spawnTweenOffset = targetPos - reference.pos;
			reference.spawnTweenDuration = duration;
			reference.spawnTweenTimer = 0f;
			reference.spawnTweenEase = ease;
			reference.spawnTweenTrans = trans;
		}
	}

	public bool IsBulletTweening(int index)
	{
		if (index < 0 || index >= 65536 || !_data[index].active)
		{
			return false;
		}
		ref BulletData reference = ref _data[index];
		if (reference.spawnTweenDuration > 0f)
		{
			return reference.spawnTweenTimer < reference.spawnTweenDuration;
		}
		return false;
	}

	private static Vector2 EvalQuadraticBezier(Vector2 start, Vector2 control, Vector2 end, float t)
	{
		float num = 1f - t;
		return num * num * start + 2f * num * t * control + t * t * end;
	}

	internal static float GetAbsorbVisualScale(ref BulletData b)
	{
		if (!b.absorbOpen)
		{
			return 1f;
		}
		return b.absorbScale;
	}

	public bool BeginAbsorb(int index, Vector2 targetPos, float duration, float spin = 0f, float curveSign = -1f, float curveRatio = 0.18f)
	{
		if (index < 0 || index >= 65536)
		{
			return false;
		}
		ref BulletData reference = ref _data[index];
		if (!reference.active || reference.absorbOpen)
		{
			return false;
		}
		if (duration <= 0f)
		{
			Despawn(index);
			return false;
		}
		Vector2 renderPos = GetRenderPos(ref reference);
		Vector2 vector = targetPos - renderPos;
		float num = vector.Length();
		Vector2 absorbControlPos = (renderPos + targetPos) * 0.5f;
		if (num > 0.01f && curveRatio != 0f)
		{
			Vector2 vector2 = new Vector2(0f - vector.Y, vector.X) / num;
			absorbControlPos += vector2 * (num * curveRatio * curveSign);
		}
		reference.absorbOpen = true;
		reference.absorbStartPos = renderPos;
		reference.absorbTargetPos = targetPos;
		reference.absorbControlPos = absorbControlPos;
		reference.absorbDuration = duration;
		reference.absorbTimer = 0f;
		reference.absorbScale = 1f;
		reference.absorbSpin = spin;
		reference.pos = renderPos;
		return true;
	}

	public bool IsBulletAbsorbing(int index)
	{
		if (index < 0 || index >= 65536)
		{
			return false;
		}
		if (_data[index].active)
		{
			return _data[index].absorbOpen;
		}
		return false;
	}

	private void AdvanceAbsorb(int index, ref BulletData b, double delta)
	{
		b.absorbTimer += (float)delta;
		float absorbDuration = b.absorbDuration;
		float num = ((absorbDuration > 0f) ? Mathf.Clamp(b.absorbTimer / absorbDuration, 0f, 1f) : 1f);
		float t = num * num;
		b.pos = EvalQuadraticBezier(b.absorbStartPos, b.absorbControlPos, b.absorbTargetPos, t);
		float num2 = num * num * (3f - 2f * num);
		b.absorbScale = Mathf.Max(1f - num2, 0f);
		if (b.absorbSpin != 0f)
		{
			b.rotation += (float)delta * b.absorbSpin * (0.35f + num);
		}
		if (num >= 1f)
		{
			Despawn(index);
		}
	}

	public void Update(double delta, ulong currentFrame)
	{
		UpdateSimulation(delta, currentFrame);
		PublishRenderState(delta, currentFrame);
	}

	internal void UpdateSimulation(double delta, ulong currentFrame)
	{
		long startTicks = TowerDefensePerfProfiler.Begin();
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		_collisionPhysicsFrameForUpdate = currentFrame;
		TowerDefenseManager instance = TowerDefenseManager.Instance;
		if (GodotObject.IsInstanceValid(instance))
		{
			_collisionRegistryForFrame = instance.characterRegistry;
			_collisionMapColumnCountForFrame = instance.gridNum.X;
			_collisionMapRowCountForFrame = instance.gridNum.Y;
			_collisionGridSizeForFrame = instance.gridSize;
			_collisionGridBeginForFrame = instance.gridBeginPos;
		}
		else
		{
			_collisionRegistryForFrame = null;
			_collisionMapColumnCountForFrame = 0;
			_collisionMapRowCountForFrame = 0;
			_collisionGridSizeForFrame = Vector2.Zero;
			_collisionGridBeginForFrame = Vector2.Zero;
		}
		_hasRegisteredCharactersForFrame = GodotObject.IsInstanceValid(_collisionRegistryForFrame) && _collisionRegistryForFrame.HasRegisteredCharacters;
		ResetCollisionCandidateCache(_hasRegisteredCharactersForFrame ? _collisionRegistryForFrame.QueryRevision : 0, _hasRegisteredCharactersForFrame ? _collisionRegistryForFrame.GeometryRevision : 0);
		_penetrationOverlapSetFastPathCountLastUpdate = 0;
		_penetrationOverlapSetCleanupCountLastUpdate = 0;
		_penetrationRememberedOrderFastPathCountLastUpdate = 0;
		PrepareMapGridColumnsForFrame();
		BeginSpatialCacheFrame(currentFrame);
		EnsureGroundHeightSnapshotForFrame();
		if (_animMeshCount > 0)
		{
			_animMeshGlobalTimer += delta;
			if (_animMeshGlobalTimer > 3600.0)
			{
				_animMeshGlobalTimer %= 3600.0;
			}
		}
		int num = (int)currentFrame;
		if (num != _trackSearchBudgetFrame)
		{
			_trackSearchBudgetFrame = num;
			_trackSearchBudgetUsed = 0;
		}
		if (currentFrame != (ulong)_effectSpawnFrame)
		{
			_effectSpawnFrame = (long)currentFrame;
			_effectSpawnUsed = 0;
		}
		TowerDefensePerfProfiler.End("bulletField.frame.setup", startTicks2, _activeCount);
		long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
		long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
		UpdateZoneRects();
		TowerDefensePerfProfiler.End("bulletField.simulation.zones", startTicks4, _allZones.Count);
		_animMeshShadowCandidateCount = 0;
		long startTicks5 = TowerDefensePerfProfiler.BeginHotPath();
		bool flag = TryRunParallelNoInteractionMotion(delta, currentFrame);
		TowerDefensePerfProfiler.End("bulletField.simulation.parallel", startTicks5, _activeCount);
		bool flag2 = _parallelMotionWasActive && !flag;
		long startTicks6 = 0L;
		if (!flag || ParallelMotionNeedsReconciliation)
		{
			startTicks6 = TowerDefensePerfProfiler.BeginHotPath();
			int i = 0;
			for (int activeCount = _activeCount; i < activeCount && i < _activeCount; i++)
			{
				int num2 = _activeIndices[i];
				if (!_data[num2].active)
				{
					continue;
				}
				ref BulletData reference = ref _data[num2];
				if (reference.absorbOpen)
				{
					AdvanceAbsorb(num2, ref reference, delta);
					continue;
				}
				if (flag2)
				{
					RefreshGridPosFromCollision(num2, ref reference);
				}
				if (flag)
				{
					byte b = _parallelMotionResults[num2];
					if ((b & 1) != 0)
					{
						if ((b & 2) != 0)
						{
							RefreshGridPosFromCollision(num2, ref reference);
						}
						if ((b & 4) != 0)
						{
							Despawn(num2);
						}
						if ((b & 8) != 0 && reference.active)
						{
							_animMeshShadowCandidateIndices[_animMeshShadowCandidateCount++] = num2;
						}
						continue;
					}
				}
				Vector2 collisionPos = GetCollisionPos(ref reference);
				ProcessBulletBehavior(num2, ref reference, delta);
				if (!reference.active)
				{
					continue;
				}
				if (!reference.externalControlled && !reference.rotateFollowVelocity && reference.rotateScale != 0f)
				{
					reference.rotation += (float)(delta * (double)reference.rotateScale * (double)reference.fireDirX);
				}
				if (reference.spawnTweenDuration > 0f && reference.spawnTweenTimer < reference.spawnTweenDuration)
				{
					float num3 = EvalTweenEase(reference.spawnTweenTimer / reference.spawnTweenDuration, reference.spawnTweenEase, reference.spawnTweenTrans);
					reference.pos = reference.spawnTweenStartPos + reference.spawnTweenOffset * num3;
					reference.spawnTweenTimer += (float)delta;
					if (ShouldRefreshGrid(num2))
					{
						RefreshGridPosFromCollision(num2, ref reference);
					}
					if (_allZones.Count > 0)
					{
						ProcessZonesForBullet(num2);
						if (!reference.active)
						{
							goto IL_05a8;
						}
						if (reference.absorbOpen)
						{
							continue;
						}
					}
					ProcessCollision(num2, collisionPos);
				}
				else if (reference.externalControlled)
				{
					if (ShouldRefreshGrid(num2))
					{
						RefreshGridPosFromCollision(num2, ref reference);
					}
					if (_allZones.Count > 0)
					{
						ProcessZonesForBullet(num2);
						if (!reference.active || reference.absorbOpen)
						{
							goto IL_05a8;
						}
					}
					ProcessCollision(num2, collisionPos);
				}
				else if (reference.trackOpen && reference.extId >= 0)
				{
					ProcessTrackCheckData(currentFrame, num2);
					if (reference.active)
					{
					}
				}
				else
				{
					if (reference.catapultOpen)
					{
						ProcessCatapultData(delta, currentFrame, num2, collisionPos);
					}
					else if (reference.useGravity)
					{
						ProcessGravityData(delta, currentFrame, num2);
					}
					else if (reference.useFall)
					{
						ProcessFallData(delta, currentFrame, num2);
					}
					else
					{
						if (reference.trackOpen)
						{
							if (_allZones.Count > 0)
							{
								ProcessZonesForBullet(num2);
								if (!reference.active || reference.absorbOpen)
								{
									continue;
								}
							}
							ProcessTrackData(delta, currentFrame, num2);
							if (reference.active)
							{
								CollectAnimatedMeshShadowCandidate(num2, ref reference);
							}
							continue;
						}
						ProcessShooterData(delta, currentFrame, num2);
					}
					if (!reference.active)
					{
						continue;
					}
					if (_allZones.Count > 0)
					{
						ProcessZonesForBullet(num2);
						if (!reference.active)
						{
							goto IL_05a8;
						}
						if (reference.absorbOpen)
						{
							continue;
						}
					}
					if (reference.catapultOpen || reference.useGravity || reference.useFall)
					{
						if (reference.collisionEnabled)
						{
							ProcessCollision(num2, collisionPos);
						}
					}
					else
					{
						ProcessCollision(num2, collisionPos);
					}
				}
				goto IL_05a8;
				IL_05a8:
				CollectAnimatedMeshShadowCandidate(num2, ref reference);
			}
		}
		TowerDefensePerfProfiler.End("bulletField.simulation.sequential", startTicks6, _activeCount);
		_parallelMotionWasActive = flag;
		TowerDefensePerfProfiler.End("bulletField.simulation", startTicks3, _activeCount);
		TowerDefensePerfProfiler.End("bulletField.update", startTicks, _activeCount);
		TowerDefensePerfProfiler.DumpIfNeeded();
	}

	internal void PublishRenderState(double delta, ulong currentFrame)
	{
		BeginBulletShadowFrame((long)currentFrame);
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		RenderSyncSTATIC(delta);
		TowerDefensePerfProfiler.End("bulletField.render.static", startTicks, _activeCount);
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		RenderSyncANIMATED_MESH(delta);
		TowerDefensePerfProfiler.End("bulletField.render.animatedMesh", startTicks2, _animMeshCount);
		RecordPublicationContinuityForTest();
		if (DrawCollisionDebug)
		{
			base.ZIndex = 4096;
			QueueRedraw();
		}
		TowerDefensePerfProfiler.DumpIfNeeded();
	}

	public void BeginPublicationContinuityProbeForTest()
	{
		PublicationMismatchFramesForTest = 0;
		PublicationMaximumDeficitForTest = 0;
		PublicationMaximumSurplusForTest = 0;
		_publicationContinuityProbeEnabled = true;
	}

	public void EndPublicationContinuityProbeForTest()
	{
		_publicationContinuityProbeEnabled = false;
	}

	private void RecordPublicationContinuityForTest()
	{
		if (!_publicationContinuityProbeEnabled)
		{
			return;
		}
		int num = GetStaticVisibleInstanceCountForTest() + GetAnimatedMeshVisibleInstanceCountForTest();
		int num2 = _activeCount - num;
		if (num2 != 0)
		{
			PublicationMismatchFramesForTest++;
			if (num2 > 0)
			{
				PublicationMaximumDeficitForTest = Math.Max(PublicationMaximumDeficitForTest, num2);
			}
			else
			{
				PublicationMaximumSurplusForTest = Math.Max(PublicationMaximumSurplusForTest, -num2);
			}
		}
	}

	public override void _Draw()
	{
		if (!DrawCollisionDebug)
		{
			return;
		}
		for (int i = 0; i < _activeCount; i++)
		{
			int num = _activeIndices[i];
			if (_data[num].active)
			{
				ref BulletData reference = ref _data[num];
				Vector2 vector = ((!reference.catapultOpen && !reference.useGravity && !reference.useFall) ? new Vector2(reference.pos.X, (float)((double)reference.pos.Y + reference.height)) : new Vector2(reference.pos.X, (float)((double)reference.pos.Y - reference.z)));
				Vector2 collisionHalfSize = GetCollisionHalfSize(ref reference);
				Rect2 rect = new Rect2(vector - collisionHalfSize, collisionHalfSize * 2f);
				DrawRect(rect, new Color(1f, 0f, 0f, 0.5f), filled: false, 1.5f);
				Vector2 renderPos = GetRenderPos(ref reference);
				DrawCircle(renderPos, 3f, new Color(1f, 1f, 0f, 0.8f));
			}
		}
		foreach (TowerDefenseCharacter cleanCharacters in TowerDefenseManager.Instance.characterRegistry.GetCleanCharactersList())
		{
			if (GodotObject.IsInstanceValid(cleanCharacters))
			{
				Rect2 worldHitRect = cleanCharacters.WorldHitRect;
				DrawRect(worldHitRect, new Color(0f, 1f, 0f, 0.5f), filled: false, 1.5f);
			}
		}
	}

	private void RenderSyncSTATIC(double delta)
	{
		if (_activeCount == 0 || _animMeshCount >= _activeCount)
		{
			if (_staticBuckets.Count > 0)
			{
				BeginStaticFrame();
				EndStaticFrame();
			}
			return;
		}
		EnsureRendererInit();
		SyncAtlasCacheVersion();
		PrepareStaticEntriesForTrace();
		if (_atlasDirty && !TryRebuildAtlas(out var reason))
		{
			GD.PushError("[BulletField:E_STATIC_RENDER_UNSUPPORTED] projectile='<registered>' scene='<cached>' reason='" + reason + "'");
		}
		BeginStaticFrame();
		WriteStaticBuckets();
		EndStaticFrame();
	}

	private void RenderSyncANIMATED_MESH(double delta)
	{
		if (_animMeshCount == 0)
		{
			if (_animRendererInit && GodotObject.IsInstanceValid(_animRenderer) && _animRenderer.GetUnifiedBucketCountForTest() > 0)
			{
				_animRenderer.BeginFrame();
				_animRenderer.EndFrame();
			}
			return;
		}
		AnimateMultiMeshRenderer animRenderer = AnimRenderer;
		animRenderer.SetAnimationTime(_animMeshGlobalTimer);
		animRenderer.BeginFrame();
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		int definitionId;
		if (_animMeshCount >= 8192 && HasMultiplePopulatedRows())
		{
			long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
			PrepareAnimatedMeshBuildJobs(animRenderer);
			TowerDefensePerfProfiler.End("bulletField.render.animatedMesh.prepare", startTicks2, _animMeshCount);
			long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
			if (_animMeshActiveBuildJobs.Count > 1)
			{
				if (_animatedMeshBuildWorker == null)
				{
					_animatedMeshBuildWorker = BuildAnimatedMeshJob;
				}
				RunParallelItems(_animMeshActiveBuildJobs.Count, _animatedMeshBuildWorker);
			}
			else if (_animMeshActiveBuildJobs.Count == 1)
			{
				BuildAnimatedMeshJob(0);
			}
			TowerDefensePerfProfiler.End("bulletField.render.animatedMesh.encode", startTicks3, _animMeshCount);
		}
		else if (TryGetSingleActiveAnimatedDefinition(out definitionId))
		{
			BuildHomogeneousAnimatedMeshSequential(animRenderer, definitionId);
			SubmitAnimatedMeshShadows();
		}
		else
		{
			BuildAnimatedMeshSequential(animRenderer);
		}
		TowerDefensePerfProfiler.End("bulletField.render.animatedMesh.build", startTicks, _animMeshCount);
		long startTicks4 = TowerDefensePerfProfiler.BeginHotPath();
		animRenderer.EndFrame();
		TowerDefensePerfProfiler.End("bulletField.render.animatedMesh.upload", startTicks4, _animMeshCount);
	}

	private static Vector2 GetCollisionPos(ref BulletData b)
	{
		if (b.catapultOpen || b.useGravity || b.useFall)
		{
			return new Vector2(b.pos.X, (float)((double)b.pos.Y - b.z));
		}
		return new Vector2(b.pos.X, (float)((double)b.pos.Y + b.height));
	}

	internal static Rect2 GetCollisionRect(ref BulletData b)
	{
		Vector2 collisionPos = GetCollisionPos(ref b);
		Vector2 collisionHalfSize = GetCollisionHalfSize(ref b);
		return new Rect2(collisionPos - collisionHalfSize, collisionHalfSize * 2f);
	}

	internal static Vector2 GetCollisionHalfSize(ref BulletData b)
	{
		return DefaultProjectileHitHalfSize * b.hitBoxScale.Abs();
	}

	private static Rect2 GetSweptCollisionRect(Vector2 startPosition, Vector2 endPosition, Vector2 halfSize)
	{
		Vector2 vector = new Vector2(Mathf.Min(startPosition.X, endPosition.X), Mathf.Min(startPosition.Y, endPosition.Y));
		return new Rect2(size: new Vector2(Mathf.Max(startPosition.X, endPosition.X), Mathf.Max(startPosition.Y, endPosition.Y)) - vector + halfSize * 2f, position: vector - halfSize);
	}

	internal static bool SweptCollisionIntersectsRect(Vector2 startPosition, Vector2 endPosition, Vector2 halfSize, Rect2 targetRect)
	{
		Vector2 vector = targetRect.Position + targetRect.Size;
		float minimum = Mathf.Min(targetRect.Position.X, vector.X) - halfSize.X;
		float maximum = Mathf.Max(targetRect.Position.X, vector.X) + halfSize.X;
		float minimum2 = Mathf.Min(targetRect.Position.Y, vector.Y) - halfSize.Y;
		float maximum2 = Mathf.Max(targetRect.Position.Y, vector.Y) + halfSize.Y;
		Vector2 vector2 = endPosition - startPosition;
		float minimumTime = 0f;
		float maximumTime = 1f;
		if (ClipSweptCollisionAxis(startPosition.X, vector2.X, minimum, maximum, ref minimumTime, ref maximumTime))
		{
			return ClipSweptCollisionAxis(startPosition.Y, vector2.Y, minimum2, maximum2, ref minimumTime, ref maximumTime);
		}
		return false;
	}

	private static bool ClipSweptCollisionAxis(float origin, float motion, float minimum, float maximum, ref float minimumTime, ref float maximumTime)
	{
		if (Mathf.IsZeroApprox(motion))
		{
			if (origin >= minimum)
			{
				return origin <= maximum;
			}
			return false;
		}
		float num = 1f / motion;
		float num2 = (minimum - origin) * num;
		float num3 = (maximum - origin) * num;
		if (num2 > num3)
		{
			float num4 = num2;
			num2 = num3;
			num3 = num4;
		}
		minimumTime = Mathf.Max(minimumTime, num2);
		maximumTime = Mathf.Min(maximumTime, num3);
		return minimumTime <= maximumTime;
	}

	private static bool TryGetSegmentCircleEntryPosition(Vector2 startPosition, Vector2 endPosition, Vector2 circleCenter, float radius, out Vector2 entryPosition)
	{
		entryPosition = startPosition;
		Vector2 vector = startPosition - circleCenter;
		float num = radius * radius;
		if (vector.LengthSquared() <= num)
		{
			return true;
		}
		Vector2 vector2 = endPosition - startPosition;
		float num2 = vector2.LengthSquared();
		if (num2 <= 1E-06f)
		{
			return false;
		}
		float num3 = 2f * vector.Dot(vector2);
		float num4 = vector.LengthSquared() - num;
		float num5 = num3 * num3 - 4f * num2 * num4;
		if (num5 < 0f)
		{
			return false;
		}
		float num6 = (0f - num3 - Mathf.Sqrt(num5)) / (2f * num2);
		if (num6 < 0f || num6 > 1f)
		{
			return false;
		}
		entryPosition = startPosition + vector2 * num6;
		return true;
	}

	private static bool IsCollisionRectOutOfBounds(ref BulletData b)
	{
		return !AabbShapeUtil.Intersects(b.rect, GetCollisionRect(ref b));
	}

	private static bool CanPlanarProjectileChangeRows(ref BulletData b)
	{
		if (!b.catapultOpen && !b.useGravity && !b.useFall && !b.trackOpen)
		{
			return !Mathf.IsZeroApprox(b.vel.Y);
		}
		return false;
	}

	private void RefreshGridPosFromCollision(int index, ref BulletData b)
	{
		Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(GetCollisionPos(ref b));
		int num = ((b.lockGridY && !CanPlanarProjectileChangeRows(ref b)) ? b.gridY : mapGridPos.Y);
		if (num != b.gridY)
		{
			RemoveFromRowBucket(index, b.gridY);
			b.gridY = num;
			AddToRowBucket(index, b.gridY);
		}
		b.gridPos = new Vector2I(mapGridPos.X, num);
		b.cell = GetMapCellForFrame(b.gridPos);
		RefreshBulletGroundData(index, ref b);
	}

	private void PrepareMapGridColumnsForFrame()
	{
		TowerDefenseBattleFeatureMap mapFeature = TowerDefenseManager.GetMapFeature();
		if (!GodotObject.IsInstanceValid(mapFeature) || mapFeature.plantGrid == null)
		{
			if (_mapGridSnapshotFeature == null && _mapGridColumnCountForFrame == 0)
			{
				return;
			}
			for (int i = 0; i < _mapGridColumnCountForFrame; i++)
			{
				TowerDefenseCellInstance[] array = _mapGridCellsForFrame[i];
				if (array != null && _mapGridRowCountsForFrame[i] > 0)
				{
					System.Array.Clear(array, 0, _mapGridRowCountsForFrame[i]);
				}
				_mapGridRowCountsForFrame[i] = 0;
			}
			_mapGridColumnCountForFrame = 0;
			_mapGridSnapshotFeature = null;
			_mapGridSnapshotRevision = 0uL;
			return;
		}
		ulong plantGridRevision = mapFeature.PlantGridRevision;
		if (_mapGridSnapshotFeature == mapFeature && _mapGridSnapshotRevision == plantGridRevision)
		{
			return;
		}
		Array<Godot.Collections.Array> plantGrid = mapFeature.plantGrid;
		int count = plantGrid.Count;
		if (_mapGridCellsForFrame.Length < count)
		{
			System.Array.Resize(ref _mapGridCellsForFrame, count);
			System.Array.Resize(ref _mapGridRowCountsForFrame, count);
		}
		for (int j = 0; j < count; j++)
		{
			Godot.Collections.Array array2 = plantGrid[j];
			int num = array2?.Count ?? 0;
			TowerDefenseCellInstance[] array3 = _mapGridCellsForFrame[j];
			if (array3 == null || array3.Length < num)
			{
				System.Array.Resize(ref array3, num);
				_mapGridCellsForFrame[j] = array3;
			}
			for (int k = 0; k < num; k++)
			{
				Variant variant = array2[k];
				array3[k] = ((variant.VariantType == Variant.Type.Nil) ? null : (variant.AsGodotObject() as TowerDefenseCellInstance));
			}
			int num2 = _mapGridRowCountsForFrame[j];
			if (array3 != null && num2 > num)
			{
				System.Array.Clear(array3, num, num2 - num);
			}
			_mapGridRowCountsForFrame[j] = num;
		}
		for (int l = count; l < _mapGridColumnCountForFrame; l++)
		{
			TowerDefenseCellInstance[] array4 = _mapGridCellsForFrame[l];
			if (array4 != null && _mapGridRowCountsForFrame[l] > 0)
			{
				System.Array.Clear(array4, 0, _mapGridRowCountsForFrame[l]);
			}
			_mapGridRowCountsForFrame[l] = 0;
		}
		_mapGridColumnCountForFrame = count;
		_mapGridSnapshotFeature = mapFeature;
		_mapGridSnapshotRevision = plantGridRevision;
	}

	private TowerDefenseCellInstance GetMapCellForFrame(Vector2I gridPos)
	{
		if (gridPos.X < 1 || gridPos.Y < 1 || gridPos.X > _collisionMapColumnCountForFrame || gridPos.Y > _collisionMapRowCountForFrame || gridPos.X >= _mapGridColumnCountForFrame)
		{
			return null;
		}
		if (_mapGridCellsForFrame[gridPos.X] == null || gridPos.Y >= _mapGridRowCountsForFrame[gridPos.X])
		{
			return null;
		}
		return _mapGridCellsForFrame[gridPos.X][gridPos.Y];
	}

	private static float EvalTweenEase(float t, Tween.EaseType ease, Tween.TransitionType trans)
	{
		if (t <= 0f)
		{
			return 0f;
		}
		if (t >= 1f)
		{
			return 1f;
		}
		if ((ulong)ease <= 3uL)
		{
			switch ((int)ease)
			{
			case 0:
				return EvalTransition(trans, t);
			case 1:
				return 1f - EvalTransition(trans, 1f - t);
			case 2:
				if (!(t < 0.5f))
				{
					return 1f - EvalTransition(trans, 2f * (1f - t)) * 0.5f;
				}
				return EvalTransition(trans, 2f * t) * 0.5f;
			case 3:
				if (!(t < 0.5f))
				{
					return EvalTransition(trans, 2f * t - 1f) * 0.5f + 0.5f;
				}
				return (1f - EvalTransition(trans, 1f - 2f * t)) * 0.5f;
			}
		}
		return EvalTransition(trans, t);
	}

	private static float EvalTransition(Tween.TransitionType trans, float t)
	{
		if ((ulong)trans <= 10uL)
		{
			switch ((int)trans)
			{
			case 0:
				return t;
			case 4:
				return t * t;
			case 7:
				return t * t * t;
			case 3:
				return t * t * t * t;
			case 2:
				return t * t * t * t * t;
			case 1:
				return 1f - Mathf.Cos(t * (float)Math.PI * 0.5f);
			case 5:
				if (!(t <= 0f))
				{
					return Mathf.Pow(2f, 10f * (t - 1f));
				}
				return 0f;
			case 8:
				return 0f - (Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) - 1f);
			case 10:
				return t * t * (2.70158f * t - 1.70158f);
			}
		}
		return t;
	}

	private void ProcessShooterData(double delta, ulong currentFrame, int index)
	{
		ref BulletData reference = ref _data[index];
		if (ShouldRefreshGrid(index))
		{
			RefreshGridPosFromCollision(index, ref reference);
		}
		if (CheckShooterGroundBlock(index, ref reference))
		{
			return;
		}
		reference.pos += reference.vel * (float)delta;
		bool flag = !Mathf.IsZeroApprox(reference.vel.Y);
		if (reference.yOffsetDuration > 0f && reference.yOffsetTimer < reference.yOffsetDuration)
		{
			flag = true;
			float yOffset = reference.yOffset;
			reference.yOffsetTimer += (float)delta;
			if (reference.yOffsetTimer >= reference.yOffsetDuration)
			{
				reference.yOffset = reference.yOffsetTarget;
				reference.yOffsetDuration = 0f;
			}
			else
			{
				reference.yOffset = reference.yOffsetTarget * (reference.yOffsetTimer / reference.yOffsetDuration);
			}
			reference.pos.Y += reference.yOffset - yOffset;
		}
		if (flag)
		{
			RefreshGridPosFromCollision(index, ref reference);
		}
		if (IsCollisionRectOutOfBounds(ref reference))
		{
			Despawn(index);
		}
		else if (reference.fireLength != -1 && reference.savePos.DistanceSquaredTo(reference.pos) > reference.checkDistance * reference.checkDistance)
		{
			Despawn(index);
		}
	}

	private void ProcessGravityData(double delta, ulong currentFrame, int index)
	{
		ref BulletData reference = ref _data[index];
		if (ShouldRefreshGrid(index))
		{
			RefreshGridPosFromCollision(index, ref reference);
		}
		reference.ySpeed += reference.gravity * (double)reference.gravityScale * delta;
		reference.z -= reference.ySpeed * delta;
		if (reference.ySpeed >= 0.0 && reference.z - reference.groundHeight < 60.0)
		{
			reference.collisionEnabled = true;
		}
		else
		{
			reference.collisionEnabled = false;
		}
		if (reference.z <= reference.groundHeight)
		{
			reference.collisionEnabled = false;
			LandData(index);
		}
		else
		{
			reference.pos += reference.vel * (float)delta;
		}
	}

	private void ProcessFallData(double delta, ulong currentFrame, int index)
	{
		ref BulletData reference = ref _data[index];
		if (ShouldRefreshGrid(index))
		{
			RefreshGridPosFromCollision(index, ref reference);
		}
		if (reference.z > reference.groundHeight)
		{
			reference.ySpeed += reference.gravity * (double)reference.gravityScale * delta;
			reference.z -= reference.ySpeed * delta;
			if (reference.z - reference.groundHeight <= 100.0)
			{
				reference.collisionEnabled = true;
			}
			else
			{
				reference.collisionEnabled = false;
			}
			reference.pos += reference.vel * (float)delta;
		}
		else
		{
			reference.collisionEnabled = false;
			Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(reference.pos);
			reference.gridPos = (reference.lockGridY ? new Vector2I(mapGridPos.X, reference.gridY) : mapGridPos);
			LandData(index);
		}
	}

	private void ProcessCatapultData(double delta, ulong currentFrame, int index, Vector2 collisionStartPosition)
	{
		ref BulletData reference = ref _data[index];
		if (ShouldRefreshGrid(index))
		{
			RefreshGridPosFromCollision(index, ref reference);
		}
		reference.catapultTimer += delta;
		if (reference.catapultSkyDrop && reference.catapultSkyDropPhase != CatapultSkyDropPhase.Descending)
		{
			ProcessCatapultSkyDropAscentAndWait(ref reference, delta);
			return;
		}
		reference.ySpeed += 2000.0 * delta;
		reference.z -= reference.ySpeed * delta;
		reference.pos += reference.vel * (float)delta;
		if (reference.ySpeed > 0.0 && reference.z - reference.groundHeight < 100.0 && !reference.blocked)
		{
			reference.collisionEnabled = true;
			if (ProcessPriorityCatapultBlockZones(index, collisionStartPosition))
			{
				return;
			}
			if (!reference.hitOver && GodotObject.IsInstanceValid(reference.target) && !reference.target.die && reference.target.targetRegistrationComponent.canProjectileCheck && CanTargetStruct(in reference, reference.target))
			{
				Vector2 globalPositionForPhysicsFrame = reference.target.GetGlobalPositionForPhysicsFrame(_collisionPhysicsFrameForUpdate);
				if ((double)reference.pos.DistanceSquaredTo(globalPositionForPhysicsFrame) < 625.0)
				{
					HitCharacterData(index, reference.target, IsPenetratingBullet(in reference));
					return;
				}
			}
		}
		else
		{
			reference.collisionEnabled = false;
		}
		if (reference.z <= reference.groundHeight && reference.ySpeed >= 0.0)
		{
			reference.z = reference.groundHeight;
			reference.ySpeed = 0.0;
			reference.isGround = true;
			reference.collisionEnabled = false;
			LandData(index);
		}
	}

	private void ProcessCatapultSkyDropAscentAndWait(ref BulletData b, double delta)
	{
		b.collisionEnabled = false;
		if (b.catapultSkyDropPhase == CatapultSkyDropPhase.Ascending)
		{
			double num = Math.Max(0.01, b.catapultSkyDropAscentDuration);
			b.catapultSkyDropAscentElapsed = Math.Min(num, b.catapultSkyDropAscentElapsed + delta);
			float num2 = (float)(b.catapultSkyDropAscentElapsed / num);
			float num3 = 1f - num2;
			float num4 = 1f - num3 * num3 * num3;
			double num5 = Math.Max(1.0, (b.catapultSkyDropAscentEndZ - b.catapultSkyDropLaunchZ) / num);
			b.pos = new Vector2(b.catapultSkyDropLaunchPos.X - b.catapultSkyDropAscentHorizontalOffset * num4, b.catapultSkyDropLaunchPos.Y);
			b.z = b.catapultSkyDropLaunchZ + (b.catapultSkyDropAscentEndZ - b.catapultSkyDropLaunchZ) * (double)num2;
			b.vel = new Vector2((0f - b.catapultSkyDropAscentHorizontalOffset) * 3f * num3 * num3 / (float)num, 0f);
			b.ySpeed = 0.0 - num5;
			if (!(b.catapultSkyDropAscentElapsed < num))
			{
				if (!IsSkyDropFullyAboveViewport(ref b))
				{
					b.pos = new Vector2(b.catapultSkyDropLaunchPos.X - b.catapultSkyDropAscentHorizontalOffset, b.catapultSkyDropLaunchPos.Y);
					b.z += num5 * delta;
					b.vel = Vector2.Zero;
				}
				else
				{
					b.catapultSkyDropPhase = CatapultSkyDropPhase.OffscreenWait;
					b.catapultSkyDropWaitRemaining = b.catapultSkyDropOffscreenWaitSeconds;
					b.vel = Vector2.Zero;
					b.ySpeed = 0.0;
				}
			}
		}
		else if (b.catapultSkyDropPhase == CatapultSkyDropPhase.OffscreenWait)
		{
			b.catapultSkyDropWaitRemaining = Math.Max(0.0, b.catapultSkyDropWaitRemaining - delta);
			if (!(b.catapultSkyDropWaitRemaining > 0.0))
			{
				float y = GetVisibleWorldRect().Position.Y;
				b.pos = b.catapultTargetPos;
				b.z = b.catapultTargetPos.Y - y + b.catapultSkyDropVisualRadius + 4f;
				b.vel = Vector2.Zero;
				b.ySpeed = 0.0;
				b.catapultSkyDropPhase = CatapultSkyDropPhase.Descending;
			}
		}
	}

	private bool IsSkyDropFullyAboveViewport(ref BulletData bullet)
	{
		return (float)((double)bullet.pos.Y - bullet.z) + Math.Max(0f, bullet.catapultSkyDropVisualRadius) < GetVisibleWorldRect().Position.Y;
	}

	private Rect2 GetVisibleWorldRect()
	{
		Viewport viewport = GetViewport();
		if (!GodotObject.IsInstanceValid(viewport))
		{
			return new Rect2(-100000f, -100000f, 200000f, 200000f);
		}
		Rect2 visibleRect = viewport.GetVisibleRect();
		Transform2D transform2D = viewport.GetCanvasTransform().AffineInverse();
		Vector2 position = transform2D * visibleRect.Position;
		Vector2 to = transform2D * new Vector2(visibleRect.End.X, visibleRect.Position.Y);
		Vector2 to2 = transform2D * new Vector2(visibleRect.Position.X, visibleRect.End.Y);
		Vector2 to3 = transform2D * visibleRect.End;
		return new Rect2(position, Vector2.Zero).Expand(to).Expand(to2).Expand(to3);
	}

	private void ProcessTrackData(double delta, ulong currentFrame, int index)
	{
		ref BulletData reference = ref _data[index];
		TowerDefenseCharacter target = reference.target;
		bool flag = GodotObject.IsInstanceValid(target);
		if (flag && !target.targetRegistrationComponent.canProjectileCheck)
		{
			flag = false;
		}
		if (flag && (target.nearDie || target.die || !CanTargetStruct(in reference, target) || !CanCollisionStruct(in reference, target.instance.maskFlags)))
		{
			flag = false;
		}
		if (flag && !target.IsHitBoxEnabled)
		{
			flag = false;
		}
		if (!flag)
		{
			reference.target = null;
			FindTrackTargetStruct(index, currentFrame);
			target = reference.target;
		}
		int num = (GodotObject.IsInstanceValid(target) ? reference.trackSearchInterval : reference.trackNoTargetInterval);
		if (num > 0 && currentFrame % (ulong)num == 0L)
		{
			RefreshGridPosFromCollision(index, ref reference);
			FindTrackTargetStruct(index, currentFrame);
			target = reference.target;
		}
		if (GodotObject.IsInstanceValid(target))
		{
			Vector2 globalPositionForPhysicsFrame = target.GetGlobalPositionForPhysicsFrame(currentFrame);
			Vector2 vector = (globalPositionForPhysicsFrame - reference.pos).Normalized();
			reference.rotation = (float)Mathf.LerpAngle(reference.rotation, vector.Angle(), delta * 5.0);
			reference.vel = vector * reference.speed;
			Vector2 pos = reference.pos;
			Vector2 vector2 = pos + reference.vel * (float)delta;
			if (TryGetSegmentCircleEntryPosition(pos, vector2, globalPositionForPhysicsFrame, 30f, out var entryPosition))
			{
				reference.pos = entryPosition;
				HitCharacterData(index, target, isPenetrate: false);
				return;
			}
			reference.pos = vector2;
		}
		else
		{
			Vector2 vector3 = reference.vel;
			if (vector3.LengthSquared() <= 0.01f)
			{
				vector3 = new Vector2(Mathf.Cos(reference.rotation), Mathf.Sin(reference.rotation));
			}
			reference.vel = vector3.Normalized() * reference.speed;
			reference.pos += reference.vel * (float)delta;
			if (IsCollisionRectOutOfBounds(ref reference))
			{
				Despawn(index);
				return;
			}
		}
		if (reference.fireLength != -1 && reference.savePos.DistanceSquaredTo(reference.pos) > reference.checkDistance * reference.checkDistance)
		{
			Despawn(index);
		}
	}

	private void ProcessTrackCheckData(ulong currentFrame, int index)
	{
		ref BulletData reference = ref _data[index];
		if (reference.over)
		{
			return;
		}
		int num = (GodotObject.IsInstanceValid(reference.target) ? reference.trackSearchInterval : reference.trackNoTargetInterval);
		if (num > 0 && (ulong)((long)currentFrame + (long)reference.randFreshIndex) % (ulong)num == 0L)
		{
			bool flag = GodotObject.IsInstanceValid(reference.target);
			if (flag && !reference.target.targetRegistrationComponent.canProjectileCheck)
			{
				flag = false;
			}
			if (flag && (reference.target.nearDie || reference.target.die || !CanTargetStruct(in reference, reference.target) || !CanCollisionStruct(in reference, reference.target.instance.maskFlags)))
			{
				flag = false;
			}
			if (flag && !reference.target.IsHitBoxEnabled)
			{
				flag = false;
			}
			if (!flag)
			{
				reference.target = null;
				FindTrackTargetStruct(index, currentFrame);
			}
			if (!GodotObject.IsInstanceValid(reference.target) && IsCollisionRectOutOfBounds(ref reference))
			{
				Despawn(index);
			}
		}
	}

	private void FindTrackTargetStruct(int index, ulong physicsFrame)
	{
		ref BulletData reference = ref _data[index];
		if (GodotObject.IsInstanceValid(reference.target) || _trackSearchBudgetUsed >= 10)
		{
			return;
		}
		if (GodotObject.IsInstanceValid(reference.magneticTarget) && !reference.magneticTarget.nearDie && !reference.magneticTarget.die && CanTargetStruct(in reference, reference.magneticTarget) && CanCollisionStruct(in reference, reference.magneticTarget.instance.maskFlags) && reference.magneticTarget.targetRegistrationComponent.canProjectileCheck && reference.magneticTarget.IsHitBoxEnabled)
		{
			reference.target = reference.magneticTarget;
			_trackSearchBudgetUsed++;
			return;
		}
		TowerDefenseCharacter projectileTargetNearestForPhysicsFrame = TowerDefenseManager.Instance.GetProjectileTargetNearestForPhysicsFrame(reference.pos, reference.gridPos, reference.collisionFlags, reference.camp, reference.speed, physicsFrame, fliterGravestone: true, reference.deprioritizeDisabledTargets);
		if (!GodotObject.IsInstanceValid(projectileTargetNearestForPhysicsFrame))
		{
			_trackSearchBudgetUsed++;
			return;
		}
		reference.target = projectileTargetNearestForPhysicsFrame;
		if (reference.target is TowerDefensePlant && GodotObject.IsInstanceValid(reference.cell))
		{
			reference.target = reference.cell.GetTarget(reference.collisionFlags, reference.camp);
		}
		_trackSearchBudgetUsed++;
	}

	private static bool CanTargetStruct(in BulletData b, TowerDefenseCharacter character)
	{
		if (b.fireCharacter is TowerDefenseZombie && character is TowerDefenseGravestone)
		{
			return false;
		}
		return b.camp != character.camp;
	}

	private static bool CanCollisionStruct(in BulletData b, int maskFlags)
	{
		return (maskFlags & b.collisionFlags) != 0;
	}

	private bool ShouldFilterCollisionLine(bool checkAll, int gridY)
	{
		if (checkAll)
		{
			return false;
		}
		if (_collisionMapRowCountForFrame <= 0)
		{
			return gridY != -2147483648;
		}
		if (gridY >= 1)
		{
			return gridY <= _collisionMapRowCountForFrame;
		}
		return false;
	}

	private void ResetCollisionCandidateCache(ulong registryRevision, ulong geometryRevision)
	{
		for (int i = 0; i < _collisionCandidateCacheEntryCount; i++)
		{
			_collisionCandidateCacheEntries[i].Clear();
		}
		_collisionCandidateCacheLookup.Clear();
		_collisionCandidateCacheEntryCount = 0;
		_collisionCandidateCacheMostRecentIndex = -1;
		_collisionCandidateCacheHasMostRecent = false;
		_collisionCandidateCacheRevision = registryRevision;
		_collisionCandidateCacheBuildCountLastUpdate = 0;
		_collisionCandidateCacheHitCountLastUpdate = 0;
		ResetCollisionOverlapCache(geometryRevision);
	}

	private void ResetCollisionOverlapCache(ulong geometryRevision)
	{
		for (int i = 0; i < _collisionOverlapCacheEntryCount; i++)
		{
			_collisionOverlapCacheEntries[i].Clear();
		}
		_collisionOverlapCacheLookup.Clear();
		_collisionOverlapCacheEntryCount = 0;
		_collisionOverlapCacheMostRecentIndex = -1;
		_collisionOverlapCacheHasMostRecent = false;
		_collisionOverlapCacheGeometryRevision = geometryRevision;
		_collisionOverlapCacheBuildCountLastUpdate = 0;
		_collisionOverlapCacheHitCountLastUpdate = 0;
	}

	private void ClearCollisionCandidateCacheStorage()
	{
		ResetCollisionCandidateCache(0uL, 0uL);
		_collisionOverlapScratch.Clear();
		for (int i = 0; i < _collisionCandidateCacheEntries.Count; i++)
		{
			_collisionCandidateCacheEntries[i].Clear();
		}
		_collisionCandidateCacheEntries.Clear();
		for (int j = 0; j < _collisionOverlapCacheEntries.Count; j++)
		{
			_collisionOverlapCacheEntries[j].Clear();
		}
		_collisionOverlapCacheEntries.Clear();
	}

	private bool TryGetCollisionCandidateQueryKey(Rect2 checkRect, int line, out CollisionCandidateQueryKey key)
	{
		key = default(CollisionCandidateQueryKey);
		if (_collisionMapColumnCountForFrame <= 0 || _collisionGridSizeForFrame.X <= 0f || _collisionGridSizeForFrame.Y <= 0f)
		{
			return false;
		}
		Vector2 vector = checkRect.Position + checkRect.Size;
		float num = Mathf.Min(checkRect.Position.X, vector.X);
		float num2 = Mathf.Max(checkRect.Position.X, vector.X);
		int num3 = Mathf.FloorToInt((num - _collisionGridBeginForFrame.X) / _collisionGridSizeForFrame.X);
		int num4 = Mathf.FloorToInt((num2 - _collisionGridBeginForFrame.X) / _collisionGridSizeForFrame.X);
		float num5 = Mathf.Min(checkRect.Position.Y, vector.Y);
		float num6 = Mathf.Max(checkRect.Position.Y, vector.Y);
		int minRow = Mathf.FloorToInt((num5 - _collisionGridBeginForFrame.Y) / _collisionGridSizeForFrame.Y);
		int maxRow = Mathf.FloorToInt((num6 - _collisionGridBeginForFrame.Y) / _collisionGridSizeForFrame.Y);
		key = new CollisionCandidateQueryKey(num3, num4, line, minRow, maxRow);
		return num3 <= num4;
	}

	private List<TowerDefenseCharacter> GetCollisionCandidatesCached(TowerDefenseBattleCharacterRegistry registry, Rect2 checkRect, int line, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		ulong queryRevision = registry.QueryRevision;
		if (_collisionCandidateCacheRevision != queryRevision)
		{
			ResetCollisionCandidateCache(queryRevision, registry.GeometryRevision);
		}
		if (line != -2147483648 && registry.TryGetSmallCharactersForLineListExcludingCampForFrame(line, excludedCamp, includeAllLineCheck: true, 32, _collisionPhysicsFrameForUpdate, out var characters))
		{
			return characters;
		}
		if (!TryGetCollisionCandidateQueryKey(checkRect, line, out var key))
		{
			registry.FillCharactersForWorldRectCandidatesListForFrame(checkRect, _collisionCandidates, line, line != -2147483648, _collisionPhysicsFrameForUpdate);
			_collisionCandidateCacheBuildCountLastUpdate++;
			return _collisionCandidates;
		}
		if (_collisionCandidateCacheHasMostRecent && key.Equals(_collisionCandidateCacheMostRecentKey))
		{
			_collisionCandidateCacheHitCountLastUpdate++;
			return _collisionCandidateCacheEntries[_collisionCandidateCacheMostRecentIndex];
		}
		if (_collisionCandidateCacheLookup.TryGetValue(key, out var value))
		{
			_collisionCandidateCacheMostRecentKey = key;
			_collisionCandidateCacheMostRecentIndex = value;
			_collisionCandidateCacheHasMostRecent = true;
			_collisionCandidateCacheHitCountLastUpdate++;
			return _collisionCandidateCacheEntries[value];
		}
		int num = _collisionCandidateCacheEntryCount++;
		if (num == _collisionCandidateCacheEntries.Count)
		{
			_collisionCandidateCacheEntries.Add(new List<TowerDefenseCharacter>());
		}
		List<TowerDefenseCharacter> list = _collisionCandidateCacheEntries[num];
		list.Clear();
		float num2 = _collisionGridBeginForFrame.X + (float)key.MinColumn * _collisionGridSizeForFrame.X;
		float num3 = _collisionGridBeginForFrame.X + (float)(key.MaxColumn + 1) * _collisionGridSizeForFrame.X;
		Rect2 checkRect2 = new Rect2(num2, checkRect.Position.Y, num3 - num2, checkRect.Size.Y);
		registry.FillCharactersForWorldRectCandidatesListForFrame(checkRect2, list, line, line != -2147483648, _collisionPhysicsFrameForUpdate);
		_collisionCandidateCacheLookup.Add(key, num);
		_collisionCandidateCacheMostRecentKey = key;
		_collisionCandidateCacheMostRecentIndex = num;
		_collisionCandidateCacheHasMostRecent = true;
		_collisionCandidateCacheBuildCountLastUpdate++;
		return list;
	}

	private static bool CanReuseCollisionGeometry(TowerDefenseProjectileConfig config)
	{
		if (config != null && !config.useRange && config.hitTargetEventList.Count == 0)
		{
			return config.hitCharacterEventList.Count == 0;
		}
		return false;
	}

	private List<TowerDefenseCharacter> GetCollisionOverlapsCached(TowerDefenseBattleCharacterRegistry registry, Rect2 checkRect, int line, bool filterLine, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		ulong queryRevision = registry.QueryRevision;
		ulong geometryRevision = registry.GeometryRevision;
		if (_collisionCandidateCacheRevision != queryRevision)
		{
			ResetCollisionCandidateCache(queryRevision, geometryRevision);
		}
		else if (_collisionOverlapCacheGeometryRevision != geometryRevision)
		{
			ResetCollisionOverlapCache(geometryRevision);
		}
		CollisionOverlapQueryKey collisionOverlapQueryKey = new CollisionOverlapQueryKey(checkRect, filterLine ? line : (-2147483648), excludedCamp);
		if (_collisionOverlapCacheHasMostRecent && collisionOverlapQueryKey.Equals(_collisionOverlapCacheMostRecentKey))
		{
			_collisionOverlapCacheHitCountLastUpdate++;
			return _collisionOverlapCacheEntries[_collisionOverlapCacheMostRecentIndex];
		}
		if (_collisionOverlapCacheLookup.TryGetValue(collisionOverlapQueryKey, out var value))
		{
			_collisionOverlapCacheMostRecentKey = collisionOverlapQueryKey;
			_collisionOverlapCacheMostRecentIndex = value;
			_collisionOverlapCacheHasMostRecent = true;
			_collisionOverlapCacheHitCountLastUpdate++;
			return _collisionOverlapCacheEntries[value];
		}
		List<TowerDefenseCharacter> candidates = (filterLine ? GetCollisionCandidatesCached(registry, checkRect, line, excludedCamp) : GetCollisionCandidatesCached(registry, checkRect, -2147483648, excludedCamp));
		int num = _collisionOverlapCacheEntryCount++;
		if (num == _collisionOverlapCacheEntries.Count)
		{
			_collisionOverlapCacheEntries.Add(new List<TowerDefenseCharacter>());
		}
		List<TowerDefenseCharacter> list = _collisionOverlapCacheEntries[num];
		list.Clear();
		FillCollisionOverlaps(checkRect, candidates, list);
		_collisionOverlapCacheLookup.Add(collisionOverlapQueryKey, num);
		_collisionOverlapCacheMostRecentKey = collisionOverlapQueryKey;
		_collisionOverlapCacheMostRecentIndex = num;
		_collisionOverlapCacheHasMostRecent = true;
		_collisionOverlapCacheBuildCountLastUpdate++;
		return list;
	}

	private List<TowerDefenseCharacter> GetCollisionOverlapsDirect(TowerDefenseBattleCharacterRegistry registry, Rect2 checkRect, int line, bool filterLine, TowerDefenseEnum.CHARACTER_CAMP excludedCamp)
	{
		List<TowerDefenseCharacter> candidates = (filterLine ? GetCollisionCandidatesCached(registry, checkRect, line, excludedCamp) : GetCollisionCandidatesCached(registry, checkRect, -2147483648, excludedCamp));
		_collisionOverlapScratch.Clear();
		FillCollisionOverlaps(checkRect, candidates, _collisionOverlapScratch);
		return _collisionOverlapScratch;
	}

	private static void FillCollisionOverlaps(Rect2 checkRect, List<TowerDefenseCharacter> candidates, List<TowerDefenseCharacter> overlaps)
	{
		float num = checkRect.Position.X;
		float num2 = num + checkRect.Size.X;
		if (num2 < num)
		{
			float num3 = num;
			num = num2;
			num2 = num3;
		}
		float num4 = checkRect.Position.Y;
		float num5 = num4 + checkRect.Size.Y;
		if (num5 < num4)
		{
			float num6 = num4;
			num4 = num5;
			num5 = num6;
		}
		for (int i = 0; i < candidates.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = candidates[i];
			if (!towerDefenseCharacter.TryGetProjectileWorldHitRect(out var rect))
			{
				continue;
			}
			float num7 = rect.Position.X;
			float num8 = num7 + rect.Size.X;
			if (num8 < num7)
			{
				float num9 = num7;
				num7 = num8;
				num8 = num9;
			}
			if (!(num8 < num) && !(num7 > num2))
			{
				float num10 = rect.Position.Y;
				float num11 = num10 + rect.Size.Y;
				if (num11 < num10)
				{
					float num12 = num10;
					num10 = num11;
					num11 = num12;
				}
				if (!(num11 < num4) && !(num10 > num5))
				{
					overlaps.Add(towerDefenseCharacter);
				}
			}
		}
	}

	private static bool IsPenetratingBullet(in BulletData b)
	{
		if ((b.fireMethodFlags & 4) != 0)
		{
			return !b.trackOpen;
		}
		return false;
	}

	private void BeginPenetrateOverlapScan(int index, ref BulletData b, bool isPenetrate)
	{
		if (isPenetrate)
		{
			ClearCurrentPenetrationTargets();
			_penetrationScanTargetOrdinal = 0;
			_penetrationRememberedSetEmptyAtScanStart = GetRememberedPenetrationTargetCount(index, ref b) == 0;
		}
	}

	private void ClearCurrentPenetrationTargets()
	{
		_penetratingTargetIdsThisFrame.Clear();
		_penetratingOverflowTargetIdsThisFrame.Clear();
	}

	private bool ContainsCurrentPenetrationTarget(ulong instanceId)
	{
		if (!_penetratingTargetIdsThisFrame.Contains(instanceId))
		{
			return _penetratingOverflowTargetIdsThisFrame.Contains(instanceId);
		}
		return true;
	}

	private bool AddCurrentPenetrationTarget(ulong instanceId)
	{
		if (ContainsCurrentPenetrationTarget(instanceId))
		{
			return false;
		}
		AddCurrentPenetrationTargetUnchecked(instanceId);
		return true;
	}

	private void AddCurrentPenetrationTargetUnchecked(ulong instanceId)
	{
		if (_penetratingTargetIdsThisFrame.Count < 16)
		{
			_penetratingTargetIdsThisFrame.Add(instanceId);
		}
		else
		{
			_penetratingOverflowTargetIdsThisFrame.Add(instanceId);
		}
	}

	private int GetRememberedPenetrationTargetCount(int index, ref BulletData b)
	{
		return _penetrationInlineTargetCounts[index] + _penetrationSecondaryTargetCounts[index] + _penetrationTertiaryTargetCounts[index] + (b.penetratingTargetIds?.Count ?? 0);
	}

	internal int GetRememberedPenetrationTargetCountForTest(int index)
	{
		if ((uint)index >= 65536u)
		{
			return 0;
		}
		return GetRememberedPenetrationTargetCount(index, ref _data[index]);
	}

	private bool TryGetRememberedPenetrationTargetAt(int index, int ordinal, out ulong instanceId)
	{
		int num = _penetrationInlineTargetCounts[index];
		if ((uint)ordinal < (uint)num)
		{
			instanceId = _penetrationInlineTargetIds[index * 8 + ordinal];
			return true;
		}
		ordinal -= num;
		int num2 = _penetrationSecondaryTargetCounts[index];
		if ((uint)ordinal < (uint)num2)
		{
			instanceId = _penetrationSecondaryTargetIds[index * 8 + ordinal];
			return true;
		}
		ordinal -= num2;
		int num3 = _penetrationTertiaryTargetCounts[index];
		if ((uint)ordinal < (uint)num3)
		{
			instanceId = _penetrationTertiaryTargetIds[index * 8 + ordinal];
			return true;
		}
		instanceId = 0uL;
		return false;
	}

	private bool ContainsRememberedPenetrationTarget(int index, ref BulletData b, ulong instanceId)
	{
		int num = index * 8;
		int num2 = _penetrationInlineTargetCounts[index];
		for (int i = 0; i < num2; i++)
		{
			if (_penetrationInlineTargetIds[num + i] == instanceId)
			{
				return true;
			}
		}
		int num3 = index * 8;
		int num4 = _penetrationSecondaryTargetCounts[index];
		for (int j = 0; j < num4; j++)
		{
			if (_penetrationSecondaryTargetIds[num3 + j] == instanceId)
			{
				return true;
			}
		}
		int num5 = index * 8;
		int num6 = _penetrationTertiaryTargetCounts[index];
		for (int k = 0; k < num6; k++)
		{
			if (_penetrationTertiaryTargetIds[num5 + k] == instanceId)
			{
				return true;
			}
		}
		return b.penetratingTargetIds?.Contains(instanceId) ?? false;
	}

	private bool AddRememberedPenetrationTarget(int index, ref BulletData b, ulong instanceId)
	{
		if (ContainsRememberedPenetrationTarget(index, ref b, instanceId))
		{
			return false;
		}
		return AddRememberedPenetrationTargetUnchecked(index, ref b, instanceId);
	}

	private bool AddRememberedPenetrationTargetUnchecked(int index, ref BulletData b, ulong instanceId)
	{
		int num = _penetrationInlineTargetCounts[index];
		if (num < 8)
		{
			_penetrationInlineTargetIds[index * 8 + num] = instanceId;
			_penetrationInlineTargetCounts[index] = (byte)(num + 1);
			return true;
		}
		int num2 = _penetrationSecondaryTargetCounts[index];
		if (num2 < 8)
		{
			_penetrationSecondaryTargetIds[index * 8 + num2] = instanceId;
			_penetrationSecondaryTargetCounts[index] = (byte)(num2 + 1);
			return true;
		}
		int num3 = _penetrationTertiaryTargetCounts[index];
		if (num3 < 8)
		{
			_penetrationTertiaryTargetIds[index * 8 + num3] = instanceId;
			_penetrationTertiaryTargetCounts[index] = (byte)(num3 + 1);
			return true;
		}
		ref HashSet<ulong> penetratingTargetIds = ref b.penetratingTargetIds;
		if (penetratingTargetIds == null)
		{
			penetratingTargetIds = new HashSet<ulong>();
		}
		return b.penetratingTargetIds.Add(instanceId);
	}

	private void ClearPenetratingTargets(int index, ref BulletData b, bool releaseOverflow = false)
	{
		_penetrationInlineTargetCounts[index] = 0;
		_penetrationSecondaryTargetCounts[index] = 0;
		_penetrationTertiaryTargetCounts[index] = 0;
		b.penetratingTargetIds?.Clear();
		if (releaseOverflow)
		{
			b.penetratingTargetIds = null;
		}
		b.lastHitInstanceId = 0uL;
	}

	private void RemoveExitedPenetrationTargets(int index, ref BulletData b)
	{
		int num = index * 8;
		int num2 = _penetrationInlineTargetCounts[index];
		int num3 = 0;
		for (int i = 0; i < num2; i++)
		{
			ulong num4 = _penetrationInlineTargetIds[num + i];
			if (ContainsCurrentPenetrationTarget(num4))
			{
				_penetrationInlineTargetIds[num + num3++] = num4;
			}
		}
		_penetrationInlineTargetCounts[index] = (byte)num3;
		int num5 = index * 8;
		int num6 = _penetrationSecondaryTargetCounts[index];
		num3 = 0;
		for (int j = 0; j < num6; j++)
		{
			ulong num7 = _penetrationSecondaryTargetIds[num5 + j];
			if (ContainsCurrentPenetrationTarget(num7))
			{
				_penetrationSecondaryTargetIds[num5 + num3++] = num7;
			}
		}
		_penetrationSecondaryTargetCounts[index] = (byte)num3;
		int num8 = index * 8;
		int num9 = _penetrationTertiaryTargetCounts[index];
		num3 = 0;
		for (int k = 0; k < num9; k++)
		{
			ulong num10 = _penetrationTertiaryTargetIds[num8 + k];
			if (ContainsCurrentPenetrationTarget(num10))
			{
				_penetrationTertiaryTargetIds[num8 + num3++] = num10;
			}
		}
		_penetrationTertiaryTargetCounts[index] = (byte)num3;
		if (b.penetratingTargetIds != null)
		{
			_penetratingTargetIdsToRemove.Clear();
			foreach (ulong penetratingTargetId in b.penetratingTargetIds)
			{
				if (!ContainsCurrentPenetrationTarget(penetratingTargetId))
				{
					_penetratingTargetIdsToRemove.Add(penetratingTargetId);
				}
			}
			for (int l = 0; l < _penetratingTargetIdsToRemove.Count; l++)
			{
				b.penetratingTargetIds.Remove(_penetratingTargetIdsToRemove[l]);
			}
		}
		if (GetRememberedPenetrationTargetCount(index, ref b) == 0)
		{
			b.lastHitInstanceId = 0uL;
		}
	}

	private void EndPenetrateOverlapScan(int index, ref BulletData b, bool isPenetrate)
	{
		if (!isPenetrate)
		{
			return;
		}
		try
		{
			if (!b.active || b.over)
			{
				return;
			}
			if (CurrentPenetrationTargetCount == 0)
			{
				if (GetRememberedPenetrationTargetCount(index, ref b) > 0)
				{
					ClearPenetratingTargets(index, ref b);
					_penetrationOverlapSetCleanupCountLastUpdate++;
				}
			}
			else if (CurrentPenetrationTargetCount == GetRememberedPenetrationTargetCount(index, ref b))
			{
				_penetrationOverlapSetFastPathCountLastUpdate++;
			}
			else
			{
				_penetrationOverlapSetCleanupCountLastUpdate++;
				RemoveExitedPenetrationTargets(index, ref b);
			}
		}
		finally
		{
			ClearCurrentPenetrationTargets();
			_penetratingTargetIdsToRemove.Clear();
			_penetrationRememberedSetEmptyAtScanStart = false;
		}
	}

	private bool TryRegisterPenetrateHit(int index, ref BulletData b, TowerDefenseCharacter character, bool isPenetrate)
	{
		if (!isPenetrate)
		{
			return true;
		}
		ulong instanceId = character.GetInstanceId();
		int penetrationScanTargetOrdinal = _penetrationScanTargetOrdinal;
		if (!_penetrationRememberedSetEmptyAtScanStart && TryGetRememberedPenetrationTargetAt(index, penetrationScanTargetOrdinal, out var instanceId2) && instanceId2 == instanceId)
		{
			AddCurrentPenetrationTargetUnchecked(instanceId);
			_penetrationScanTargetOrdinal = penetrationScanTargetOrdinal + 1;
			_penetrationRememberedOrderFastPathCountLastUpdate++;
			return false;
		}
		if (!AddCurrentPenetrationTarget(instanceId))
		{
			return false;
		}
		_penetrationScanTargetOrdinal = penetrationScanTargetOrdinal + 1;
		if (_penetrationRememberedSetEmptyAtScanStart)
		{
			if (!AddRememberedPenetrationTargetUnchecked(index, ref b, instanceId))
			{
				return false;
			}
		}
		else if (!AddRememberedPenetrationTarget(index, ref b, instanceId))
		{
			return false;
		}
		b.lastHitInstanceId = instanceId;
		return true;
	}

	private int BeginSourceDespawnRequest()
	{
		int num = ++_sourceDespawnRequestDepth;
		if (num >= _sourceDespawnRequests.Length)
		{
			System.Array.Resize(ref _sourceDespawnRequests, _sourceDespawnRequests.Length * 2);
		}
		_sourceDespawnRequests[num] = false;
		return num;
	}

	private void RequestSourceDespawn()
	{
		int sourceDespawnRequestDepth = _sourceDespawnRequestDepth;
		if ((uint)sourceDespawnRequestDepth < (uint)_sourceDespawnRequests.Length)
		{
			_sourceDespawnRequests[sourceDespawnRequestDepth] = true;
		}
	}

	private bool EndSourceDespawnRequest(int depth)
	{
		bool result = (uint)depth < (uint)_sourceDespawnRequests.Length && _sourceDespawnRequests[depth];
		if ((uint)depth < (uint)_sourceDespawnRequests.Length)
		{
			_sourceDespawnRequests[depth] = false;
		}
		_sourceDespawnRequestDepth = depth - 1;
		return result;
	}

	private void ProcessCollision(int index, Vector2 collisionStartPosition)
	{
		long startTicks = TowerDefensePerfProfiler.BeginHotPath();
		ref BulletData reference = ref _data[index];
		if (reference.over || reference.hitOver)
		{
			TowerDefensePerfProfiler.End("bulletField.collision.total", startTicks);
			return;
		}
		TowerDefenseBattleCharacterRegistry collisionRegistryForFrame = _collisionRegistryForFrame;
		if (!_hasRegisteredCharactersForFrame)
		{
			ClearPenetratingTargets(index, ref reference);
			TowerDefensePerfProfiler.End("bulletField.collision.total", startTicks);
			return;
		}
		Vector2 collisionPos = GetCollisionPos(ref reference);
		Vector2 collisionHalfSize = GetCollisionHalfSize(ref reference);
		Rect2 sweptCollisionRect = GetSweptCollisionRect(collisionStartPosition, collisionPos, collisionHalfSize);
		Vector2 vector = collisionPos - collisionStartPosition;
		bool flag = !Mathf.IsZeroApprox(vector.X) && !Mathf.IsZeroApprox(vector.Y);
		bool flag2 = ShouldFilterCollisionLine(reference.checkAll, reference.gridY);
		bool canReuseCollisionGeometry = reference.canReuseCollisionGeometry;
		bool flag3 = !canReuseCollisionGeometry || flag;
		Vector2 vector2 = (flag3 ? (sweptCollisionRect.Position + sweptCollisionRect.Size) : default(Vector2));
		float num = (flag3 ? Mathf.Min(sweptCollisionRect.Position.X, vector2.X) : 0f);
		float num2 = (flag3 ? Mathf.Max(sweptCollisionRect.Position.X, vector2.X) : 0f);
		float num3 = (flag3 ? Mathf.Min(sweptCollisionRect.Position.Y, vector2.Y) : 0f);
		float num4 = (flag3 ? Mathf.Max(sweptCollisionRect.Position.Y, vector2.Y) : 0f);
		bool flag4 = IsPenetratingBullet(in reference);
		long startTicks2 = TowerDefensePerfProfiler.BeginHotPath();
		List<TowerDefenseCharacter> list = ((!canReuseCollisionGeometry) ? GetCollisionCandidatesCached(collisionRegistryForFrame, sweptCollisionRect, flag2 ? reference.gridY : (-2147483648), reference.camp) : (flag4 ? GetCollisionOverlapsCached(collisionRegistryForFrame, sweptCollisionRect, reference.gridY, flag2, reference.camp) : GetCollisionOverlapsDirect(collisionRegistryForFrame, sweptCollisionRect, reference.gridY, flag2, reference.camp)));
		TowerDefensePerfProfiler.End("bulletField.collision.query", startTicks2, list.Count);
		TowerDefensePerfProfiler.SampleHotPath("bulletField.collision.candidates", list.Count);
		ulong geometryRevision = collisionRegistryForFrame.GeometryRevision;
		TowerDefenseProjectileConfig config = reference.config;
		if (config != null && config.useDurabilityBlockingSweep)
		{
			ProcessDurabilityBlockingSweep(index, list, collisionStartPosition, collisionPos, collisionHalfSize, flag2, canReuseCollisionGeometry, flag3, flag, num, num2, num3, num4);
			TowerDefensePerfProfiler.End("bulletField.collision.total", startTicks, list.Count);
			return;
		}
		bool flag5 = false;
		if (reference.deprioritizeDisabledTargets)
		{
			for (int i = 0; i < list.Count; i++)
			{
				TowerDefenseCharacter towerDefenseCharacter = list[i];
				if (GodotObject.IsInstanceValid(towerDefenseCharacter) && towerDefenseCharacter.IsIncapacitatedTarget())
				{
					flag5 = HasAttackableTargetAhead(in reference);
					break;
				}
			}
		}
		BeginPenetrateOverlapScan(index, ref reference, flag4);
		try
		{
			for (int j = 0; j < list.Count; j++)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = list[j];
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter2) || (!canReuseCollisionGeometry && !towerDefenseCharacter2.IsHitBoxEnabled) || (!canReuseCollisionGeometry && flag2 && !towerDefenseCharacter2.IsTargetableFromLine(reference.gridY)) || !towerDefenseCharacter2.targetRegistrationComponent.canProjectileCheck || !towerDefenseCharacter2.instance.canBeCollection || (reference.collisionFlags & towerDefenseCharacter2.instance.maskFlags) == 0 || !CanTargetStruct(in reference, towerDefenseCharacter2) || (flag5 && towerDefenseCharacter2.IsIncapacitatedTarget()) || (reference.checkHeight && reference.projectileHeight > towerDefenseCharacter2.instance.height))
				{
					continue;
				}
				if (flag3)
				{
					if (!towerDefenseCharacter2.TryGetActiveWorldHitRect(out var rect))
					{
						continue;
					}
					Vector2 vector3 = rect.Position + rect.Size;
					float num5 = Mathf.Min(rect.Position.X, vector3.X);
					float num6 = Mathf.Max(rect.Position.X, vector3.X);
					if (num2 < num5 || num > num6)
					{
						continue;
					}
					float num7 = Mathf.Min(rect.Position.Y, vector3.Y);
					float num8 = Mathf.Max(rect.Position.Y, vector3.Y);
					if (num4 < num7 || num3 > num8 || (flag && !SweptCollisionIntersectsRect(collisionStartPosition, collisionPos, collisionHalfSize, rect)))
					{
						continue;
					}
				}
				long startTicks3 = TowerDefensePerfProfiler.BeginHotPath();
				TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
				HitCharacterData(index, towerDefenseCharacter2, flag4);
				TowerDefensePerfProfiler.End("bulletField.collision.hit", startTicks3);
				TowerDefensePerfProfiler.EndSpikeProbe("bulletField.collision.hit", in probe, 1);
				if (reference.active && flag4)
				{
					if (canReuseCollisionGeometry && geometryRevision != collisionRegistryForFrame.GeometryRevision)
					{
						list = GetCollisionOverlapsCached(collisionRegistryForFrame, sweptCollisionRect, reference.gridY, flag2, reference.camp);
						geometryRevision = collisionRegistryForFrame.GeometryRevision;
						j = -1;
					}
					continue;
				}
				break;
			}
		}
		finally
		{
			if (reference.active)
			{
				EndPenetrateOverlapScan(index, ref reference, flag4);
			}
			else if (flag4)
			{
				ClearCurrentPenetrationTargets();
				_penetratingTargetIdsToRemove.Clear();
				_penetrationRememberedSetEmptyAtScanStart = false;
			}
			TowerDefensePerfProfiler.End("bulletField.collision.total", startTicks, list.Count);
		}
	}

	private bool HasAttackableTargetAhead(in BulletData b)
	{
		TowerDefenseBattleCharacterRegistry collisionRegistryForFrame = _collisionRegistryForFrame;
		if (!GodotObject.IsInstanceValid(collisionRegistryForFrame))
		{
			return false;
		}
		List<TowerDefenseCharacter> charactersForGridWindowList = collisionRegistryForFrame.GetCharactersForGridWindowList(b.gridPos.X, b.gridPos.Y, 20, 0);
		float fireDirX = b.fireDirX;
		for (int i = 0; i < charactersForGridWindowList.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = charactersForGridWindowList[i];
			if (GodotObject.IsInstanceValid(towerDefenseCharacter) && !towerDefenseCharacter.die && !towerDefenseCharacter.nearDie && !towerDefenseCharacter.isDestroy && towerDefenseCharacter.IsHitBoxEnabled && towerDefenseCharacter.instance.canBeCollection && towerDefenseCharacter.targetRegistrationComponent.canProjectileCheck && (b.collisionFlags & towerDefenseCharacter.instance.maskFlags) != 0 && CanTargetStruct(in b, towerDefenseCharacter) && (!b.checkHeight || b.projectileHeight <= towerDefenseCharacter.instance.height) && !towerDefenseCharacter.IsIncapacitatedTarget())
			{
				float x = towerDefenseCharacter.GetGlobalPositionForPhysicsFrame(_collisionPhysicsFrameForUpdate).X;
				if ((!(fireDirX < 0f) || !(x > b.pos.X + 1f)) && (!(fireDirX > 0f) || !(x < b.pos.X - 1f)))
				{
					return true;
				}
			}
		}
		return false;
	}

	private void ProcessDurabilityBlockingSweep(int index, List<TowerDefenseCharacter> characters, Vector2 collisionStartPosition, Vector2 collisionEndPosition, Vector2 bulletHalfSize, bool filterLine, bool geometryPreFiltered, bool requiresGeometryCheck, bool requiresSegmentClip, float sweptMinX, float sweptMaxX, float sweptMinY, float sweptMaxY)
	{
		ref BulletData reference = ref _data[index];
		Vector2 vector = collisionEndPosition - collisionStartPosition;
		vector = ((!(vector.LengthSquared() > 0.0001f)) ? ((reference.vel.LengthSquared() > 0.0001f) ? reference.vel.Normalized() : Vector2.Left) : vector.Normalized());
		_durabilitySweepCandidates.Clear();
		for (int i = 0; i < characters.Count; i++)
		{
			TowerDefenseCharacter towerDefenseCharacter = characters[i];
			if (!GodotObject.IsInstanceValid(towerDefenseCharacter) || (!geometryPreFiltered && !towerDefenseCharacter.IsHitBoxEnabled) || (!geometryPreFiltered && filterLine && !towerDefenseCharacter.IsTargetableFromLine(reference.gridY)) || !towerDefenseCharacter.targetRegistrationComponent.canProjectileCheck || !towerDefenseCharacter.instance.canBeCollection || (reference.collisionFlags & towerDefenseCharacter.instance.maskFlags) == 0 || !CanTargetStruct(in reference, towerDefenseCharacter) || (reference.checkHeight && reference.projectileHeight > towerDefenseCharacter.instance.height))
			{
				continue;
			}
			Rect2 rect = default(Rect2);
			if (requiresGeometryCheck)
			{
				if (!towerDefenseCharacter.TryGetActiveWorldHitRect(out rect))
				{
					continue;
				}
				Vector2 vector2 = rect.Position + rect.Size;
				float num = Mathf.Min(rect.Position.X, vector2.X);
				float num2 = Mathf.Max(rect.Position.X, vector2.X);
				float num3 = Mathf.Min(rect.Position.Y, vector2.Y);
				float num4 = Mathf.Max(rect.Position.Y, vector2.Y);
				if (sweptMaxX < num || sweptMinX > num2 || sweptMaxY < num3 || sweptMinY > num4 || (requiresSegmentClip && !SweptCollisionIntersectsRect(collisionStartPosition, collisionEndPosition, bulletHalfSize, rect)))
				{
					continue;
				}
			}
			float entryDistance = ((requiresGeometryCheck ? rect.GetCenter() : towerDefenseCharacter.GetGlobalPositionForPhysicsFrame(_collisionPhysicsFrameForUpdate)) - collisionStartPosition).Dot(vector);
			_durabilitySweepCandidates.Add(new DurabilitySweepCandidate(towerDefenseCharacter, entryDistance));
		}
		_durabilitySweepCandidates.Sort(delegate(DurabilitySweepCandidate left, DurabilitySweepCandidate right)
		{
			int num6 = left.EntryDistance.CompareTo(right.EntryDistance);
			return (num6 != 0) ? num6 : left.Character.GetInstanceId().CompareTo(right.Character.GetInstanceId());
		});
		BeginPenetrateOverlapScan(index, ref reference, isPenetrate: true);
		try
		{
			for (int num5 = 0; num5 < _durabilitySweepCandidates.Count; num5++)
			{
				TowerDefenseCharacter towerDefenseCharacter2 = _durabilitySweepCandidates[num5].Character;
				if (!GodotObject.IsInstanceValid(towerDefenseCharacter2))
				{
					continue;
				}
				if (towerDefenseCharacter2 is TowerDefensePlant && GodotObject.IsInstanceValid(towerDefenseCharacter2.cell))
				{
					TowerDefenseCharacter target = towerDefenseCharacter2.cell.GetTarget(reference.collisionFlags, reference.camp, checkInvincible: true, reference.catapultOpen);
					if (GodotObject.IsInstanceValid(target))
					{
						towerDefenseCharacter2 = target;
					}
				}
				if (GodotObject.IsInstanceValid(towerDefenseCharacter2))
				{
					bool flag = ShouldDurabilityBlockingSweepStop(towerDefenseCharacter2.instance.GetProjectileDamageableDurability(reference.damageFlags), reference.config.durabilityBlockingThreshold);
					HitCharacterData(index, towerDefenseCharacter2, isPenetrate: true, resolvePlantCellTarget: false, honorSourceDespawnRequest: false);
					if (!reference.active)
					{
						break;
					}
					if (flag)
					{
						Despawn(index);
						break;
					}
				}
			}
		}
		finally
		{
			if (reference.active)
			{
				EndPenetrateOverlapScan(index, ref reference, isPenetrate: true);
			}
			else
			{
				ClearCurrentPenetrationTargets();
				_penetratingTargetIdsToRemove.Clear();
				_penetrationRememberedSetEmptyAtScanStart = false;
			}
		}
	}

	private void SetBulletEventPayload(int index, Variant metaData, BulletFieldStoredProjectile[] eventProjectiles)
	{
		if (metaData.VariantType != Variant.Type.Nil)
		{
			_eventMetadataByIndex[index] = metaData;
		}
		if (eventProjectiles != null)
		{
			_eventProjectilesByIndex[index] = eventProjectiles;
		}
	}

	private void FillEventProxy(int index, ref BulletData b)
	{
		_eventProxy.GlobalPosition = b.pos;
		_eventProxy.collisionFlags = b.collisionFlags;
		_eventProxy.camp = b.camp;
		_eventProxy.height = b.z;
		_eventProxy.velocity = b.vel;
		_eventProxy.speed = b.speed;
		_eventProxy.gridPos = b.gridPos;
		_eventProxy.damageFlags = b.damageFlags;
		_eventProxy.fireCharacter = b.fireCharacter;
		_eventProxy.checkAll = b.checkAll;
		_eventProxy.useFall = b.useFall;
		_eventProxy.useGravity = b.useGravity;
		_eventProxy.z = b.z;
		_eventProxy.ySpeed = b.ySpeed;
		_eventProxy.catapultOpen = b.catapultOpen;
		_eventProxy.isGround = b.isGround;
		_eventProxy.config = b.config;
		_eventProxy.metaData = (_eventMetadataByIndex.TryGetValue(index, out var value) ? value : default(Variant));
		_eventProxy.eventProjectiles = (_eventProjectilesByIndex.TryGetValue(index, out var value2) ? value2 : null);
		_eventProxy.fireMethodFlags = b.fireMethodFlags;
		_eventProxy.target = b.target;
		_eventProxy.penetrateNum = b.penetrateNum;
		_eventProxy.fireDirX = (int)b.fireDirX;
		_eventProxy.projectileHeight = b.projectileHeight;
		_eventProxy.hitOver = b.hitOver;
		_eventProxy.blocked = b.blocked;
	}

	private void HitCharacterData(int index, TowerDefenseCharacter tdChar, bool isPenetrate, bool resolvePlantCellTarget = true, bool honorSourceDespawnRequest = true)
	{
		ref BulletData reference = ref _data[index];
		if (resolvePlantCellTarget && tdChar is TowerDefensePlant)
		{
			TowerDefenseCellInstance cell = tdChar.cell;
			if (GodotObject.IsInstanceValid(cell))
			{
				TowerDefenseCharacter target = cell.GetTarget(reference.collisionFlags, reference.camp, checkInvincible: true, reference.catapultOpen);
				if (GodotObject.IsInstanceValid(target))
				{
					tdChar = target;
				}
			}
		}
		if (!GodotObject.IsInstanceValid(tdChar))
		{
			return;
		}
		if (reference.suppressGameplay)
		{
			HitEffectData(index, tdChar);
			Despawn(index);
		}
		else
		{
			if (!TryRegisterPenetrateHit(index, ref reference, tdChar, isPenetrate))
			{
				return;
			}
			int depth = ((isPenetrate && honorSourceDespawnRequest) ? BeginSourceDespawnRequest() : (-1));
			bool flag = true;
			if (isPenetrate)
			{
				if (((reference.config != null) ? reference.config.penetrateNum : 0) == -1)
				{
					flag = false;
				}
				else
				{
					reference.penetrateNum--;
					if (reference.penetrateNum > 0)
					{
						flag = false;
					}
				}
			}
			float originX;
			float scaleX;
			bool hasTargetTransform = tdChar.TryGetCachedHitTransform(out originX, out scaleX);
			ProjectileHitInfo info = new ProjectileHitInfo
			{
				damage = reference.damage,
				damageFlags = reference.damageFlags,
				useRuntimeOverrides = true,
				fireMethodFlags = reference.fireMethodFlags,
				position = reference.pos,
				projectileHeight = reference.projectileHeight,
				config = reference.config,
				fireCharacter = reference.fireCharacter,
				camp = reference.camp,
				collisionFlags = reference.collisionFlags,
				gridPos = reference.gridPos,
				hasTargetTransform = hasTargetTransform,
				targetOriginX = originX,
				targetScaleX = scaleX,
				height = reference.z,
				onSourceDespawn = ((isPenetrate && honorSourceDespawnRequest) ? _requestSourceDespawnAction : IgnoreGuaranteedSourceDespawnAction)
			};
			bool flag2 = false;
			if (isPenetrate && honorSourceDespawnRequest)
			{
				try
				{
					tdChar.ProjectileHurt(in info, reference.config);
				}
				finally
				{
					flag2 = EndSourceDespawnRequest(depth);
				}
			}
			else
			{
				tdChar.ProjectileHurt(in info, reference.config);
			}
			if (flag2)
			{
				flag = true;
			}
			DispatchBulletBehaviorHit(index, ref reference, tdChar);
			if (reference.config != null && !reference.canReuseCollisionGeometry)
			{
				int count = reference.config.hitTargetEventList.Count;
				if (count > 0)
				{
					FillEventProxy(index, ref reference);
				}
				for (int i = 0; i < count; i++)
				{
					reference.config.hitTargetEventList[i].ExecuteProject(_eventProxy, tdChar);
				}
			}
			TowerDefenseProjectileConfig config = reference.config;
			if (!reference.suppressGameplay && config != null && !reference.canReuseCollisionGeometry && (config.hitCharacterEventList.Count > 0 || config.useRange))
			{
				Vector2 globalPositionForPhysicsFrame = tdChar.GetGlobalPositionForPhysicsFrame(_collisionPhysicsFrameForUpdate);
				if (!tdChar.HasShield() || (reference.damageFlags & 1) == 0 || ((reference.fireMethodFlags & 1) != 0 && ((tdChar.Scale.X > 0f && reference.pos.X > globalPositionForPhysicsFrame.X + 30f) || (tdChar.Scale.X < 0f && reference.pos.X < globalPositionForPhysicsFrame.X - 30f))) || (reference.fireMethodFlags & 2) != 0)
				{
					int num = ((!tdChar.ProjectileEffectsBlocked(reference.damageFlags, reference.fireMethodFlags)) ? config.hitCharacterEventList.Count : 0);
					if (num > 0)
					{
						FillEventProxy(index, ref reference);
					}
					for (int j = 0; j < num; j++)
					{
						config.hitCharacterEventList[j].ExecuteProject(_eventProxy, tdChar);
					}
					if (config.useRange)
					{
						Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(globalPositionForPhysicsFrame);
						Vector2 mapCellPosCenter = TowerDefenseManager.Instance.GetMapCellPosCenter(mapGridPos);
						mapCellPosCenter = new Vector2(globalPositionForPhysicsFrame.X, mapCellPosCenter.Y);
						TowerDefenseExplode.CreateProjectileExplodeSingleExclude(mapCellPosCenter, reference.config, tdChar, reference.camp, reference.useFall && config.UsesExplosionDamage, reference.damage, reference.damageFlags, reference.fireMethodFlags, reference.collisionFlags);
					}
				}
			}
			TowerDefensePerfProfiler.SpikeProbe probe = TowerDefensePerfProfiler.BeginSpikeProbe();
			HitEffectData(index, tdChar);
			TowerDefensePerfProfiler.End("bulletField.hit.effect", probe.StartTicks, 1);
			TowerDefensePerfProfiler.EndSpikeProbe("bulletField.hit.effect", in probe, 1, 0.5);
			if (flag)
			{
				reference.hitOver = true;
				reference.over = true;
				Despawn(index);
			}
		}
	}

	private void HitEffectData(int index, TowerDefenseCharacter character)
	{
		ref BulletData reference = ref _data[index];
		if (reference.config == null)
		{
			return;
		}
		bool num = CanSpawnEffectThisFrame();
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (num && reference.config.hitEffect != null)
		{
			Vector2I vector2I = (GodotObject.IsInstanceValid(character) ? character.gridPos : Vector2I.Zero);
			if (!GodotObject.IsInstanceValid(characterNode) || !TowerDefenseManager.TryCreateEffectSceneOnceFast(reference.config.hitEffect, characterNode, vector2I, reference.pos, preferSprite: false))
			{
				Node node = reference.config.hitEffect.Instantiate(PackedScene.GenEditState.Disabled);
				Node node2;
				if (node is GPUParticles2DOnece scene)
				{
					node2 = TowerDefenseManager.Instance.CreateEffectParticlesSceneOnce(scene, vector2I);
				}
				else
				{
					node2 = node;
					if (node2 is TowerDefenseProjectileEffectBase towerDefenseProjectileEffectBase)
					{
						towerDefenseProjectileEffectBase.suppressDeathrattles = (reference.damageFlags & 0x20) != 0;
						double groundHeight;
						if (GodotObject.IsInstanceValid(character))
						{
							towerDefenseProjectileEffectBase.Init(reference.gridPos, reference.camp, reference.collisionFlags, character, character.groundHeight);
						}
						else if (TryGetCachedGroundHeight(reference.gridPos, out groundHeight))
						{
							towerDefenseProjectileEffectBase.Init(reference.gridPos, reference.camp, reference.collisionFlags, null, groundHeight);
						}
					}
				}
				if (node2 is Node2D node2D)
				{
					node2D.GlobalPosition = reference.pos;
					if (GodotObject.IsInstanceValid(characterNode))
					{
						characterNode.AddChild(node2D, forceReadableName: false, InternalMode.Disabled);
					}
					else
					{
						node2.QueueFree();
					}
				}
				else
				{
					node2.QueueFree();
				}
			}
		}
		if (num && TowerDefenseManager.Instance != null && TowerDefenseManager.Instance.GetEffectCountForPhysicsFrame(_collisionPhysicsFrameForUpdate) < 100)
		{
			CreatSplatData(index, character);
		}
		if (reference.config.splatAudio != "SplatNormal" || (character != null && !character.instance.HasAnyArmor))
		{
			PlaySplatData(index);
		}
	}

	private void CreatSplatData(int index, TowerDefenseCharacter character)
	{
		ref BulletData reference = ref _data[index];
		if (reference.config == null || reference.config.splatScene == null)
		{
			return;
		}
		int y = character?.gridPos.Y ?? reference.gridPos.Y;
		Vector2 globalPosition = ((!reference.config.hitBody || !GodotObject.IsInstanceValid(character)) ? reference.pos : (character.GetGlobalPositionForPhysicsFrame(_collisionPhysicsFrameForUpdate) - new Vector2(0f, 20f)));
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		Vector2I gridPosition = new Vector2I(reference.gridPos.X, y);
		if (!GodotObject.IsInstanceValid(characterNode) || !TowerDefenseManager.TryCreateEffectSceneOnceFast(reference.config.splatScene, characterNode, gridPosition, globalPosition, string.Equals(reference.config.splatSceneType, "Sprite", StringComparison.OrdinalIgnoreCase)))
		{
			Node node = reference.config.splatScene.Instantiate(PackedScene.GenEditState.Disabled);
			TowerDefenseEffectBase towerDefenseEffectBase = null;
			if (node is GPUParticles2DOnece scene)
			{
				towerDefenseEffectBase = TowerDefenseManager.Instance.CreateEffectParticlesSceneOnce(scene, reference.gridPos);
			}
			else if (node is AdobeAnimateSprite scene2)
			{
				towerDefenseEffectBase = TowerDefenseManager.Instance.CreateEffectSpriteSceneOnce(scene2, reference.gridPos);
			}
			else if (node is TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce)
			{
				towerDefenseEffectBase = towerDefenseEffectSpriteOnce;
			}
			else if (node is TowerDefenseEffectParticlesOnce towerDefenseEffectParticlesOnce)
			{
				towerDefenseEffectBase = towerDefenseEffectParticlesOnce;
			}
			if (towerDefenseEffectBase == null)
			{
				node.QueueFree();
				return;
			}
			if (!GodotObject.IsInstanceValid(characterNode))
			{
				node.QueueFree();
				return;
			}
			characterNode.AddChild(towerDefenseEffectBase, forceReadableName: false, InternalMode.Disabled);
			towerDefenseEffectBase.gridPos = new Vector2I(towerDefenseEffectBase.gridPos.X, y);
			towerDefenseEffectBase.GlobalPosition = globalPosition;
		}
	}

	private void LandData(int index)
	{
		ref BulletData reference = ref _data[index];
		Vector2I mapGridPos = TowerDefenseManager.Instance.GetMapGridPos(reference.pos);
		reference.gridPos = (reference.lockGridY ? new Vector2I(mapGridPos.X, reference.gridY) : mapGridPos);
		reference.cell = TowerDefenseManager.GetMapCell(reference.gridPos);
		DispatchBulletBehaviorLand(index, ref reference);
		if (!reference.active)
		{
			return;
		}
		if (!reference.blocked)
		{
			if (!reference.suppressGameplay && reference.config != null)
			{
				FillEventProxy(index, ref reference);
				foreach (TowerDefenseCharacterEventBase hitGroundEvent in reference.config.hitGroundEventList)
				{
					hitGroundEvent.ExecuteGroundProject(_eventProxy, reference.pos, reference.gridPos);
				}
			}
			if (!reference.suppressGameplay && reference.config != null && reference.config.useRange)
			{
				TowerDefenseExplode.CreateProjectileExplode(reference.pos, reference.config, null, reference.camp, reference.useFall && reference.config.UsesExplosionDamage, reference.damage, reference.damageFlags, reference.fireMethodFlags, reference.collisionFlags);
			}
			HitEffectData(index, null);
			PlaySplatData(index);
		}
		TryEmitLifecycleTerminal(index, BulletLifecycleTerminalReason.Landed);
		if (!reference.suppressGameplay && reference.landOverSubscribed)
		{
			BulletField.OnLandOver?.Invoke(reference.pos, reference.gridPos, reference.spawnSourceInstanceId);
		}
		if (GodotObject.IsInstanceValid(reference.cell) && reference.cell.isWater)
		{
			CreateSplashData(index);
			Despawn(index);
		}
		else
		{
			Despawn(index);
		}
	}

	private void PlaySplatData(int index)
	{
		ref BulletData reference = ref _data[index];
		if (reference.config != null)
		{
			string splatAudio = reference.config.splatAudio;
			if (!string.IsNullOrEmpty(splatAudio) && AudioManager.Instance != null)
			{
				AudioManager.Instance.AudioPlay(splatAudio);
			}
		}
	}

	private void CreateSplashData(int index)
	{
		ref BulletData reference = ref _data[index];
		if (TowerDefenseManager.Instance != null && TowerDefenseManager.Instance.GetEffectCount() <= 100)
		{
			Node2D characterNode = TowerDefenseManager.GetCharacterNode();
			if (GodotObject.IsInstanceValid(characterNode) && ObjectManager.PoolPop(ObjectManagerConfig.OBJECT.PARTICLES_SPLASH, characterNode) is TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce)
			{
				towerDefenseEffectSpriteOnce.gridPos = reference.gridPos;
				towerDefenseEffectSpriteOnce.GlobalPosition = reference.pos - new Vector2(0f, 20f);
			}
		}
	}

	private void AddToRowBucket(int index, int gridY)
	{
		if (!_rowBuckets.TryGetValue(gridY, out var value))
		{
			value = new List<int>();
			_rowBuckets[gridY] = value;
		}
		_data[index].rowBucketSlot = value.Count;
		value.Add(index);
	}

	private void RemoveFromRowBucket(int index, int gridY)
	{
		if (!_rowBuckets.TryGetValue(gridY, out var value))
		{
			GD.PushError($"[BulletField:E_ROW_BUCKET_MISSING] index={index} gridY={gridY}");
			return;
		}
		int num = value.Count - 1;
		int rowBucketSlot = _data[index].rowBucketSlot;
		if (num < 0 || (uint)rowBucketSlot > (uint)num || value[rowBucketSlot] != index)
		{
			GD.PushError($"[BulletField:E_ROW_BUCKET_SLOT_INVALID] index={index} gridY={gridY} slot={rowBucketSlot} count={value.Count}");
		}
		else
		{
			if (rowBucketSlot != num)
			{
				int num2 = (value[rowBucketSlot] = value[num]);
				_data[num2].rowBucketSlot = rowBucketSlot;
			}
			value.RemoveAt(num);
			if (value.Count == 0)
			{
				_rowBuckets.Remove(gridY);
			}
			_data[index].rowBucketSlot = -1;
		}
	}

	public int GetAnimatedMeshActiveInstanceCountForTest()
	{
		return _animMeshCount;
	}

	public int GetAnimatedMeshVisibleInstanceCountForTest()
	{
		if (!GodotObject.IsInstanceValid(_animRenderer))
		{
			return 0;
		}
		return _animRenderer.GetVisibleInstanceCountForTest();
	}

	public int GetAnimatedMeshBucketCountForTest()
	{
		if (!GodotObject.IsInstanceValid(_animRenderer))
		{
			return 0;
		}
		return _animRenderer.GetUnifiedBucketCountForTest();
	}

	private void BuildHomogeneousAnimatedMeshSequential(AnimateMultiMeshRenderer renderer, int definitionId)
	{
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			List<int> value = rowBucket.Value;
			if (value.Count == 0 || !renderer.TryPrepareSimpleDrawContext(definitionId, rowBucket.Key, value.Count, out var context))
			{
				continue;
			}
			int simpleAnimationBaseTexel = context.Definition.simpleAnimationBaseTexel;
			if ((uint)simpleAnimationBaseTexel <= 16777215u)
			{
				AdobeAnimateZIndexCrowdBucket renderBucket = context.Bucket.RenderBucket;
				for (int i = 0; i < value.Count; i++)
				{
					ref BulletData reference = ref _data[value[i]];
					Transform2D transform = BuildAnimatedMeshTransform(ref reference);
					renderBucket.AppendGpuClockSimpleReserved(transform, simpleAnimationBaseTexel, reference.animElapsedTimer, (float)reference.animFrameRate);
				}
				context.Bucket.RootCount += value.Count;
			}
		}
	}

	private void BuildAnimatedMeshSequential(AnimateMultiMeshRenderer renderer)
	{
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			int key = rowBucket.Key;
			int num = NextAnimatedMeshContextToken();
			int num2 = 0;
			List<int> value = rowBucket.Value;
			for (int i = 0; i < value.Count; i++)
			{
				ref BulletData reference = ref _data[value[i]];
				if (reference.renderMode != BulletRenderMode.ANIMATED_MESH || reference.animDefId < 0)
				{
					continue;
				}
				int animDefId = reference.animDefId;
				if (_animMeshDrawContextRows[animDefId] != num)
				{
					_animMeshDrawContextRows[animDefId] = num;
					_animMeshTouchedDefinitionIds[num2++] = animDefId;
					_animMeshDrawRootCounts[animDefId] = 0;
					renderer.TryPrepareSimpleDrawContext(animDefId, key, value.Count, out _animMeshDrawContexts[animDefId]);
					ref AnimateMultiMeshRenderer.SimpleDrawContext reference2 = ref _animMeshDrawContexts[animDefId];
					_animMeshDrawRenderBuckets[animDefId] = (reference2.IsValid ? reference2.Bucket.RenderBucket : null);
					_animMeshDrawMetadataBaseTexels[animDefId] = (reference2.IsValid ? reference2.Definition.simpleAnimationBaseTexel : (-1));
				}
				AdobeAnimateZIndexCrowdBucket adobeAnimateZIndexCrowdBucket = _animMeshDrawRenderBuckets[animDefId];
				if (adobeAnimateZIndexCrowdBucket == null)
				{
					continue;
				}
				int num3 = _animMeshDrawMetadataBaseTexels[animDefId];
				if ((uint)num3 <= 16777215u)
				{
					Transform2D transform = BuildAnimatedMeshTransform(ref reference);
					adobeAnimateZIndexCrowdBucket.AppendGpuClockSimpleReserved(transform, num3, reference.animElapsedTimer, (float)reference.animFrameRate);
					_animMeshDrawRootCounts[animDefId]++;
					if ((reference.z > 0.0 || reference.height > 0.0) && reference.config != null)
					{
						SubmitBulletShadow(key, ref reference);
					}
				}
			}
			for (int j = 0; j < num2; j++)
			{
				int num4 = _animMeshTouchedDefinitionIds[j];
				int num5 = _animMeshDrawRootCounts[num4];
				if (num5 > 0)
				{
					_animMeshDrawContexts[num4].Bucket.RootCount += num5;
				}
			}
		}
	}

	private bool HasMultiplePopulatedRows()
	{
		bool flag = false;
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			if (rowBucket.Value.Count != 0)
			{
				if (flag)
				{
					return true;
				}
				flag = true;
			}
		}
		return false;
	}

	private void PrepareAnimatedMeshBuildJobs(AnimateMultiMeshRenderer renderer)
	{
		_animMeshBuildJobLookup.Clear();
		_animMeshActiveBuildJobs.Clear();
		for (int i = 0; i < _animMeshBuildJobPool.Count; i++)
		{
			_animMeshBuildJobPool[i].Bucket = null;
			_animMeshBuildJobPool[i].Segments.Clear();
			_animMeshBuildJobPool[i].BulletCount = 0;
		}
		if (TryGetSingleActiveAnimatedDefinition(out var definitionId))
		{
			PrepareHomogeneousAnimatedMeshBuildJobs(renderer, definitionId);
			SubmitAnimatedMeshShadows();
			ReserveAnimatedMeshBuildJobs();
			return;
		}
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			int key = rowBucket.Key;
			int num = NextAnimatedMeshContextToken();
			List<int> value = rowBucket.Value;
			AnimatedMeshBuildJob animatedMeshBuildJob = null;
			AnimateMultiMeshRenderer.UnifiedBucket unifiedBucket = null;
			AnimatedMeshBuildJob animatedMeshBuildJob2 = null;
			int start = 0;
			int num2 = 0;
			for (int j = 0; j < value.Count; j++)
			{
				int num3 = value[j];
				ref BulletData reference = ref _data[num3];
				if (reference.renderMode != BulletRenderMode.ANIMATED_MESH || reference.animDefId < 0)
				{
					FlushAnimatedMeshBuildSegment(animatedMeshBuildJob, value, start, num2);
					animatedMeshBuildJob = null;
					num2 = 0;
					continue;
				}
				EnsureAnimatedMeshContextCapacity(reference.animDefId + 1);
				if (_animMeshDrawContextRows[reference.animDefId] != num)
				{
					_animMeshDrawContextRows[reference.animDefId] = num;
					renderer.TryPrepareSimpleDrawContext(reference.animDefId, key, value.Count, out _animMeshDrawContexts[reference.animDefId]);
				}
				ref AnimateMultiMeshRenderer.SimpleDrawContext reference2 = ref _animMeshDrawContexts[reference.animDefId];
				if (!reference2.IsValid)
				{
					FlushAnimatedMeshBuildSegment(animatedMeshBuildJob, value, start, num2);
					animatedMeshBuildJob = null;
					num2 = 0;
					continue;
				}
				AnimatedMeshBuildJob animatedMeshBuildJob3;
				if (unifiedBucket == reference2.Bucket)
				{
					animatedMeshBuildJob3 = animatedMeshBuildJob2;
				}
				else
				{
					unifiedBucket = reference2.Bucket;
					animatedMeshBuildJob2 = GetOrCreateAnimatedMeshBuildJob(reference2.Bucket);
					animatedMeshBuildJob3 = animatedMeshBuildJob2;
				}
				if (animatedMeshBuildJob != animatedMeshBuildJob3)
				{
					FlushAnimatedMeshBuildSegment(animatedMeshBuildJob, value, start, num2);
					animatedMeshBuildJob = animatedMeshBuildJob3;
					start = j;
					num2 = 0;
				}
				num2++;
				if ((reference.z > 0.0 || reference.height > 0.0) && reference.config != null)
				{
					SubmitBulletShadow(key, ref reference);
				}
			}
			FlushAnimatedMeshBuildSegment(animatedMeshBuildJob, value, start, num2);
		}
		ReserveAnimatedMeshBuildJobs();
	}

	private bool TryGetSingleActiveAnimatedDefinition(out int definitionId)
	{
		definitionId = -1;
		if (_animMeshCount != _activeCount || _animMeshCount <= 0 || _animMeshDefinitionCounts.Count != 1)
		{
			return false;
		}
		using (System.Collections.Generic.Dictionary<int, int>.Enumerator enumerator = _animMeshDefinitionCounts.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				KeyValuePair<int, int> current = enumerator.Current;
				definitionId = current.Key;
				return current.Value == _animMeshCount;
			}
		}
		return false;
	}

	private void PrepareHomogeneousAnimatedMeshBuildJobs(AnimateMultiMeshRenderer renderer, int definitionId)
	{
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			List<int> value = rowBucket.Value;
			if (value.Count != 0 && renderer.TryPrepareSimpleDrawContext(definitionId, rowBucket.Key, value.Count, out var context))
			{
				AnimatedMeshBuildJob orCreateAnimatedMeshBuildJob = GetOrCreateAnimatedMeshBuildJob(context.Bucket);
				orCreateAnimatedMeshBuildJob.Segments.Add(new AnimatedMeshBuildSegment(value, 0, value.Count));
				orCreateAnimatedMeshBuildJob.BulletCount += value.Count;
			}
		}
	}

	private void SubmitAnimatedMeshShadows()
	{
		for (int i = 0; i < _animMeshShadowCandidateCount; i++)
		{
			int num = _animMeshShadowCandidateIndices[i];
			ref BulletData reference = ref _data[num];
			if (reference.active && reference.renderMode == BulletRenderMode.ANIMATED_MESH)
			{
				SubmitBulletShadow(reference.gridY, ref reference);
			}
		}
	}

	private void CollectAnimatedMeshShadowCandidate(int index, ref BulletData b)
	{
		if (IsAnimatedMeshShadowCandidate(ref b))
		{
			_animMeshShadowCandidateIndices[_animMeshShadowCandidateCount++] = index;
		}
	}

	private static bool IsAnimatedMeshShadowCandidate(ref BulletData b)
	{
		if (!b.active || b.renderMode != BulletRenderMode.ANIMATED_MESH || b.config == null)
		{
			return false;
		}
		double num = ((b.z > 0.0) ? b.z : b.height);
		if (num > 0.0)
		{
			return num < 600.0;
		}
		return false;
	}

	private void ReserveAnimatedMeshBuildJobs()
	{
		for (int i = 0; i < _animMeshActiveBuildJobs.Count; i++)
		{
			AnimatedMeshBuildJob animatedMeshBuildJob = _animMeshActiveBuildJobs[i];
			animatedMeshBuildJob.Bucket.RenderBucket.ReserveCapacity(animatedMeshBuildJob.BulletCount);
		}
	}

	private void AddAnimatedMeshDefinition(int definitionId)
	{
		if (definitionId >= 0)
		{
			EnsureAnimatedMeshContextCapacity(definitionId + 1);
			_animMeshDefinitionCounts.TryGetValue(definitionId, out var value);
			_animMeshDefinitionCounts[definitionId] = value + 1;
		}
	}

	private void RemoveAnimatedMeshDefinition(int definitionId)
	{
		if (definitionId >= 0 && _animMeshDefinitionCounts.TryGetValue(definitionId, out var value))
		{
			if (value <= 1)
			{
				_animMeshDefinitionCounts.Remove(definitionId);
			}
			else
			{
				_animMeshDefinitionCounts[definitionId] = value - 1;
			}
		}
	}

	private static void FlushAnimatedMeshBuildSegment(AnimatedMeshBuildJob job, List<int> row, int start, int count)
	{
		if (job != null && count > 0)
		{
			job.Segments.Add(new AnimatedMeshBuildSegment(row, start, count));
			job.BulletCount += count;
		}
	}

	private AnimatedMeshBuildJob GetOrCreateAnimatedMeshBuildJob(AnimateMultiMeshRenderer.UnifiedBucket bucket)
	{
		if (_animMeshBuildJobLookup.TryGetValue(bucket, out var value))
		{
			return value;
		}
		int count = _animMeshActiveBuildJobs.Count;
		if (count >= _animMeshBuildJobPool.Count)
		{
			_animMeshBuildJobPool.Add(new AnimatedMeshBuildJob());
		}
		value = _animMeshBuildJobPool[count];
		value.Bucket = bucket;
		_animMeshBuildJobLookup.Add(bucket, value);
		_animMeshActiveBuildJobs.Add(value);
		return value;
	}

	private void BuildAnimatedMeshJob(int jobIndex)
	{
		AnimatedMeshBuildJob animatedMeshBuildJob = _animMeshActiveBuildJobs[jobIndex];
		int num = -1;
		int num2 = -1;
		int num3 = 0;
		for (int i = 0; i < animatedMeshBuildJob.Segments.Count; i++)
		{
			AnimatedMeshBuildSegment animatedMeshBuildSegment = animatedMeshBuildJob.Segments[i];
			int num4 = animatedMeshBuildSegment.Start + animatedMeshBuildSegment.Count;
			for (int j = animatedMeshBuildSegment.Start; j < num4; j++)
			{
				ref BulletData reference = ref _data[animatedMeshBuildSegment.Row[j]];
				if (reference.animDefId != num)
				{
					num = reference.animDefId;
					num2 = _animRenderer.GetDefinition(num)?.simpleAnimationBaseTexel ?? (-1);
				}
				if ((uint)num2 <= 16777215u)
				{
					Transform2D transform = BuildAnimatedMeshTransform(ref reference);
					animatedMeshBuildJob.Bucket.RenderBucket.AppendGpuClockSimpleReserved(transform, num2, reference.animElapsedTimer, (float)reference.animFrameRate);
					num3++;
				}
			}
		}
		animatedMeshBuildJob.Bucket.RootCount += num3;
	}

	private int NextAnimatedMeshContextToken()
	{
		if (_animMeshDrawContextToken == 2147483647)
		{
			System.Array.Clear(_animMeshDrawContextRows);
			_animMeshDrawContextToken = 0;
		}
		return ++_animMeshDrawContextToken;
	}

	private void EnsureAnimatedMeshContextCapacity(int required)
	{
		if (_animMeshDrawContexts.Length < required)
		{
			int num;
			for (num = Math.Max(8, _animMeshDrawContexts.Length); num < required; num *= 2)
			{
			}
			System.Array.Resize(ref _animMeshDrawContexts, num);
			System.Array.Resize(ref _animMeshDrawRenderBuckets, num);
			System.Array.Resize(ref _animMeshDrawMetadataBaseTexels, num);
			System.Array.Resize(ref _animMeshDrawRootCounts, num);
			System.Array.Resize(ref _animMeshTouchedDefinitionIds, num);
			System.Array.Resize(ref _animMeshDrawContextRows, num);
		}
	}

	private float GetAnimatedMeshFrame(ref BulletData b)
	{
		if (b.animFrameMax <= 0 || b.animFrameRate <= 0.0)
		{
			return b.animElapsedTimer;
		}
		return NormalizeAnimatedMeshFrame(b.animElapsedTimer + (float)(_animMeshGlobalTimer * b.animFrameRate), b.animFrameMax);
	}

	private float GetAnimatedMeshPhaseFromFrame(float frame, int frameMax, double frameRate)
	{
		if (frameMax <= 0 || frameRate <= 0.0)
		{
			return frame;
		}
		return NormalizeAnimatedMeshFrame(frame - (float)(_animMeshGlobalTimer * frameRate), frameMax);
	}

	private static float NormalizeAnimatedMeshFrame(float frame, int frameMax)
	{
		if (frameMax <= 0)
		{
			return frame;
		}
		frame %= (float)frameMax;
		if (frame < 0f)
		{
			frame += (float)frameMax;
		}
		return frame;
	}

	private Transform2D BuildAnimatedMeshTransform(ref BulletData b)
	{
		GetAnimatedMeshBasis(ref b, out var cosine, out var sine);
		Vector2 visualScale = ((b.config != null) ? b.config.scale : Vector2.One);
		visualScale *= GetAbsorbVisualScale(ref b);
		Vector2 renderPos = GetRenderPos(ref b);
		Transform2D result = BuildProjectileVisualTransformFromBasis(cosine, sine, b.flipX, b.spriteRotationCos, b.spriteRotationSin, visualScale, renderPos);
		if (b.animOffset != Vector2.Zero)
		{
			result.Origin += result.X * b.animOffset.X + result.Y * b.animOffset.Y;
		}
		return result;
	}

	private static void GetAnimatedMeshBasis(ref BulletData b, out float cosine, out float sine)
	{
		if (b.useFall)
		{
			SetBasisFromVelocity(new Vector2(b.vel.X, (float)b.ySpeed), out cosine, out sine);
		}
		else if (b.trackOpen)
		{
			SetBasisFromAngle(b.rotation, out cosine, out sine);
		}
		else if ((b.catapultOpen || b.useGravity) && b.rotateFollowVelocity)
		{
			SetBasisFromVelocity(new Vector2(b.vel.X, (float)b.ySpeed), out cosine, out sine);
		}
		else if (b.rotateFollowVelocity)
		{
			SetBasisFromVelocity(b.vel, out cosine, out sine);
		}
		else
		{
			SetBasisFromAngle(b.rotation, out cosine, out sine);
		}
	}

	private static void SetBasisFromAngle(float angle, out float cosine, out float sine)
	{
		if (angle == 0f)
		{
			cosine = 1f;
			sine = 0f;
		}
		else
		{
			cosine = Mathf.Cos(angle);
			sine = Mathf.Sin(angle);
		}
	}

	private static void SetBasisFromVelocity(Vector2 velocity, out float cosine, out float sine)
	{
		float num = velocity.LengthSquared();
		if (num <= 0.01f)
		{
			cosine = 1f;
			sine = 0f;
		}
		else
		{
			float num2 = 1f / Mathf.Sqrt(num);
			cosine = velocity.X * num2;
			sine = velocity.Y * num2;
		}
	}

	private BulletChangeInPlaceResult TryChangeBulletDataInPlace(int index, TowerDefenseProjectileConfig newConfig)
	{
		PreparedProjectileChange prepared = GetOrCreatePreparedProjectileChange(newConfig);
		if (!prepared.IsValid)
		{
			RejectSpawn(newConfig, "E_CHANGE_RENDER_UNSUPPORTED", prepared.ErrorReason);
			return BulletChangeInPlaceResult.Rejected;
		}
		return ApplyPreparedProjectileChange(index, newConfig, in prepared, null);
	}

	private BulletChangeInPlaceResult ApplyPreparedProjectileChange(int index, TowerDefenseProjectileConfig newConfig, in PreparedProjectileChange prepared, double? damageOverride)
	{
		ref BulletData reference = ref _data[index];
		BulletRenderMode renderMode = prepared.RenderMode;
		int fireMethodFlags = prepared.FireMethodFlags;
		if (fireMethodFlags != reference.fireMethodFlags)
		{
			return BulletChangeInPlaceResult.RequiresReplacement;
		}
		BulletRenderMode renderMode2 = reference.renderMode;
		int animDefId = reference.animDefId;
		float rotation = reference.rotation;
		if (!reference.trackOpen && !prepared.RotateFollowVelocity)
		{
			rotation = GetCurrentVisualRotation(ref reference);
		}
		float animElapsedTimer = reference.animElapsedTimer;
		bool flag = (fireMethodFlags & 4) != 0 && !reference.trackOpen;
		if (renderMode2 != renderMode)
		{
			ReleaseCurrentRenderPath(renderMode2);
		}
		ReleaseBulletBehavior(index, ref reference);
		reference.renderMode = renderMode;
		reference.config = newConfig;
		reference.behaviorProgramId = prepared.BehaviorProgramId;
		reference.canReuseCollisionGeometry = prepared.CanReuseCollisionGeometry;
		reference.staticAtlasEntryIndex = ((renderMode == BulletRenderMode.STATIC) ? EnsureSpriteRegistered(newConfig) : (-1));
		reference.damage = damageOverride ?? newConfig.baseDamage;
		reference.damageFlags = prepared.DamageFlags;
		reference.rotateScale = prepared.RotateScale;
		reference.rotateFollowVelocity = prepared.RotateFollowVelocity;
		reference.trackSearchInterval = prepared.TrackSearchInterval;
		reference.trackNoTargetInterval = prepared.TrackSearchInterval * 4;
		reference.penetrateNum = prepared.PenetrateNum;
		reference.lastHitInstanceId = 0uL;
		reference.hitOver = false;
		reference.over = false;
		reference.collisionEnabled = true;
		reference.rotation = rotation;
		ApplyVelocityFollowingVisuals(ref reference, prepared.AnimationRotation);
		reference.animDefId = prepared.AnimationDefinitionId;
		reference.animOffset = prepared.AnimationOffset;
		if (prepared.RenderMode == BulletRenderMode.ANIMATED_MESH)
		{
			reference.animElapsedTimer = ((animElapsedTimer >= (float)prepared.AnimationFrameMax) ? (animElapsedTimer % (float)prepared.AnimationFrameMax) : animElapsedTimer);
		}
		else
		{
			reference.animElapsedTimer = 0f;
		}
		reference.animFrameMax = prepared.AnimationFrameMax;
		reference.animFrameRate = prepared.AnimationFrameRate;
		ClearPenetratingTargets(index, ref reference, !flag);
		if (renderMode2 != renderMode)
		{
			ActivateNewRenderPath(renderMode);
		}
		if (renderMode2 == BulletRenderMode.ANIMATED_MESH)
		{
			RemoveAnimatedMeshDefinition(animDefId);
		}
		if (renderMode == BulletRenderMode.ANIMATED_MESH)
		{
			AddAnimatedMeshDefinition(reference.animDefId);
		}
		InitializeBulletBehavior(index, ref reference);
		return BulletChangeInPlaceResult.Changed;
	}

	private static void ApplyVelocityFollowingVisuals(ref BulletData bullet, float animationRotation)
	{
		if (bullet.rotateFollowVelocity)
		{
			bullet.flipX = false;
			SetSpriteRotation(ref bullet, animationRotation);
		}
	}

	internal bool TryPrepareProjectileChange(TowerDefenseProjectileConfig config, out PreparedProjectileChange prepared)
	{
		prepared = GetOrCreatePreparedProjectileChange(config);
		if (prepared.IsValid)
		{
			return true;
		}
		RejectSpawn(config, "E_CHANGE_RENDER_UNSUPPORTED", prepared.ErrorReason);
		return false;
	}

	private bool IsPreparedProjectileChangeCurrent(in PreparedProjectileChange prepared)
	{
		if (prepared.IsValid)
		{
			return prepared.AtlasCacheVersion == AdobeAnimateGlobalAtlasCache.CacheVersion;
		}
		return false;
	}

	private void ReleaseCurrentRenderPath(BulletRenderMode oldRenderMode)
	{
		if (oldRenderMode == BulletRenderMode.ANIMATED_MESH && _animMeshCount > 0)
		{
			_animMeshCount--;
		}
	}

	private void ActivateNewRenderPath(BulletRenderMode newRenderMode)
	{
		if (newRenderMode == BulletRenderMode.ANIMATED_MESH)
		{
			_animMeshCount++;
		}
	}

	private PreparedProjectileChange GetOrCreatePreparedProjectileChange(TowerDefenseProjectileConfig config)
	{
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (config == _lastPreparedProjectileChangeConfig && _lastPreparedProjectileChange.AtlasCacheVersion == cacheVersion)
		{
			return _lastPreparedProjectileChange;
		}
		if (config != null && _preparedProjectileChanges.TryGetValue(config, out var value) && value.AtlasCacheVersion == cacheVersion)
		{
			_lastPreparedProjectileChangeConfig = config;
			_lastPreparedProjectileChange = value;
			return value;
		}
		PreparedProjectileChange preparedProjectileChange;
		if (config == null)
		{
			preparedProjectileChange = new PreparedProjectileChange(isValid: false, "projectile config is null", BulletRenderMode.STATIC, -1, Vector2.Zero, 0, 0.0, cacheVersion, config);
		}
		else
		{
			if (!TryGetOrCompileBehaviorProgram(config, out var programId, out var errorReason))
			{
				preparedProjectileChange = new PreparedProjectileChange(isValid: false, "E_BEHAVIOR_UNSUPPORTED: " + errorReason, BulletRenderMode.STATIC, -1, Vector2.Zero, 0, 0.0, cacheVersion, config);
			}
			else
			{
				ProjectileRenderTemplate orCreateRenderTemplate = GetOrCreateRenderTemplate(config);
				if (!orCreateRenderTemplate.IsValid)
				{
					preparedProjectileChange = new PreparedProjectileChange(isValid: false, orCreateRenderTemplate.ErrorCode + ": " + orCreateRenderTemplate.ErrorReason, orCreateRenderTemplate.RenderMode, orCreateRenderTemplate.AnimationDefinitionId, orCreateRenderTemplate.AnimationOffset, 0, 0.0, cacheVersion, config);
				}
				else if (orCreateRenderTemplate.RenderMode == BulletRenderMode.STATIC)
				{
					preparedProjectileChange = new PreparedProjectileChange(isValid: true, string.Empty, BulletRenderMode.STATIC, -1, orCreateRenderTemplate.AnimationOffset, 0, 0.0, cacheVersion, config, programId);
				}
				else
				{
					AnimateMultiMeshRenderer.Definition definition = AnimRenderer.GetDefinition(orCreateRenderTemplate.AnimationDefinitionId);
					preparedProjectileChange = ((definition != null && definition.frameMax > 0) ? new PreparedProjectileChange(isValid: true, string.Empty, BulletRenderMode.ANIMATED_MESH, orCreateRenderTemplate.AnimationDefinitionId, orCreateRenderTemplate.AnimationOffset, definition.frameMax, definition.frameRate * Math.Abs(orCreateRenderTemplate.AnimationTimeScale), cacheVersion, config, programId) : new PreparedProjectileChange(isValid: false, $"E_ANIMATION_DEFINITION_INVALID: Compact Crowd definition {orCreateRenderTemplate.AnimationDefinitionId} is unavailable", BulletRenderMode.ANIMATED_MESH, orCreateRenderTemplate.AnimationDefinitionId, orCreateRenderTemplate.AnimationOffset, 0, 0.0, cacheVersion, config));
				}
			}
			_preparedProjectileChanges[config] = preparedProjectileChange;
		}
		_lastPreparedProjectileChangeConfig = config;
		_lastPreparedProjectileChange = preparedProjectileChange;
		return preparedProjectileChange;
	}

	private static float GetCurrentVisualRotation(ref BulletData b)
	{
		if (b.trackOpen)
		{
			return b.rotation;
		}
		if (b.useFall)
		{
			Vector2 vector = new Vector2(b.vel.X, (float)b.ySpeed);
			if (!(vector.LengthSquared() > 0.01f))
			{
				return 0f;
			}
			return vector.Angle();
		}
		if ((b.catapultOpen || b.useGravity) && b.rotateFollowVelocity)
		{
			Vector2 vector2 = new Vector2(b.vel.X, (float)b.ySpeed);
			if (!(vector2.LengthSquared() > 0.01f))
			{
				return 0f;
			}
			return vector2.Angle();
		}
		if (b.rotateFollowVelocity)
		{
			return b.vel.Angle();
		}
		return b.rotation;
	}

	private void BeginSpatialCacheFrame(ulong currentFrame)
	{
		_gridRefreshPhaseForFrame = (byte)(currentFrame % 5);
	}

	private void InitializeBulletSpatialCaches(int index, ref BulletData bullet)
	{
		ulong num = (ulong)bullet.randFreshIndex % 5uL;
		_gridRefreshPhases[index] = (byte)((5 - num) % 5);
		RefreshBulletGroundData(index, ref bullet);
	}

	private bool ShouldRefreshGrid(int index)
	{
		return _gridRefreshPhaseForFrame == _gridRefreshPhases[index];
	}

	private void EnsureGroundHeightSnapshotForFrame()
	{
		TowerDefenseBattleFeatureMap mapGridSnapshotFeature = _mapGridSnapshotFeature;
		ulong mapGridSnapshotRevision = _mapGridSnapshotRevision;
		if (!_groundHeightSnapshotDirty && _groundHeightSnapshotFeature == mapGridSnapshotFeature && _groundHeightSnapshotRevision == mapGridSnapshotRevision && _groundHeightSnapshotColumnCount == _mapGridColumnCountForFrame)
		{
			return;
		}
		UnsubscribeGroundHeightSnapshotResources();
		_groundHeightSnapshotContainsCurve = false;
		int mapGridColumnCountForFrame = _mapGridColumnCountForFrame;
		if (_mapGridGroundHeightsForFrame.Length < mapGridColumnCountForFrame)
		{
			System.Array.Resize(ref _mapGridGroundHeightsForFrame, mapGridColumnCountForFrame);
		}
		for (int i = 0; i < mapGridColumnCountForFrame; i++)
		{
			int num = _mapGridRowCountsForFrame[i];
			double[] array = _mapGridGroundHeightsForFrame[i];
			if (array == null || array.Length < num)
			{
				System.Array.Resize(ref array, num);
				_mapGridGroundHeightsForFrame[i] = array;
			}
			TowerDefenseCellInstance[] array2 = _mapGridCellsForFrame[i];
			for (int j = 0; j < num; j++)
			{
				TowerDefenseCellInstance towerDefenseCellInstance = ((array2 != null) ? array2[j] : null);
				if (!GodotObject.IsInstanceValid(towerDefenseCellInstance))
				{
					array[j] = -1.0 / 0.0;
					continue;
				}
				array[j] = towerDefenseCellInstance.GetGroundHeight();
				CurveTexture groundHeightCurve = towerDefenseCellInstance.groundHeightCurve;
				if (GodotObject.IsInstanceValid(groundHeightCurve))
				{
					_groundHeightSnapshotContainsCurve = true;
					SubscribeGroundHeightSnapshotResource(groundHeightCurve);
					Curve curve = groundHeightCurve.Curve;
					if (GodotObject.IsInstanceValid(curve))
					{
						SubscribeGroundHeightSnapshotResource(curve);
					}
				}
			}
			if (array != null && array.Length > num)
			{
				System.Array.Fill(array, -1.0 / 0.0, num, array.Length - num);
			}
		}
		for (int k = mapGridColumnCountForFrame; k < _groundHeightSnapshotColumnCount; k++)
		{
			double[] array3 = _mapGridGroundHeightsForFrame[k];
			if (array3 != null)
			{
				System.Array.Fill(array3, -1.0 / 0.0);
			}
		}
		_groundHeightSnapshotFeature = mapGridSnapshotFeature;
		_groundHeightSnapshotRevision = mapGridSnapshotRevision;
		_groundHeightSnapshotColumnCount = mapGridColumnCountForFrame;
		_groundHeightSnapshotDirty = false;
		for (int l = 0; l < _activeCount; l++)
		{
			int num2 = _activeIndices[l];
			if (num2 >= 0 && _data[num2].active)
			{
				RefreshBulletGroundData(num2, ref _data[num2]);
			}
		}
	}

	private void SubscribeGroundHeightSnapshotResource(Resource resource)
	{
		for (int i = 0; i < _groundHeightSnapshotResources.Count; i++)
		{
			if (_groundHeightSnapshotResources[i] == resource)
			{
				return;
			}
		}
		resource.Changed += MarkGroundHeightSnapshotDirty;
		_groundHeightSnapshotResources.Add(resource);
	}

	private void UnsubscribeGroundHeightSnapshotResources()
	{
		for (int i = 0; i < _groundHeightSnapshotResources.Count; i++)
		{
			Resource resource = _groundHeightSnapshotResources[i];
			if (GodotObject.IsInstanceValid(resource))
			{
				resource.Changed -= MarkGroundHeightSnapshotDirty;
			}
		}
		_groundHeightSnapshotResources.Clear();
	}

	private void MarkGroundHeightSnapshotDirty()
	{
		_groundHeightSnapshotDirty = true;
	}

	private void DisposeGroundHeightSnapshot()
	{
		UnsubscribeGroundHeightSnapshotResources();
		_groundHeightSnapshotFeature = null;
		_groundHeightSnapshotRevision = 0uL;
		_groundHeightSnapshotColumnCount = 0;
		_groundHeightSnapshotDirty = true;
	}

	private bool TryGetCachedGroundHeight(Vector2I gridPos, out double groundHeight)
	{
		groundHeight = 0.0;
		if (gridPos.X < 1 || gridPos.Y < 1 || gridPos.X >= _groundHeightSnapshotColumnCount || gridPos.X >= _mapGridGroundHeightsForFrame.Length || gridPos.Y >= _mapGridRowCountsForFrame[gridPos.X])
		{
			return false;
		}
		TowerDefenseCellInstance[] array = _mapGridCellsForFrame[gridPos.X];
		double[] array2 = _mapGridGroundHeightsForFrame[gridPos.X];
		if (array == null || array2 == null || gridPos.Y >= array.Length || gridPos.Y >= array2.Length || array[gridPos.Y] == null)
		{
			return false;
		}
		groundHeight = array2[gridPos.Y];
		return groundHeight != -1.0 / 0.0;
	}

	private void RefreshBulletGroundData(int index, ref BulletData bullet)
	{
		if (!TryGetCachedGroundHeight(bullet.gridPos, out var groundHeight))
		{
			_shooterGroundBlockHeights[index] = -1.0 / 0.0;
			return;
		}
		if (bullet.useFall)
		{
			bullet.groundHeight = groundHeight;
		}
		double num = groundHeight;
		if (bullet.projectileHeight < TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL)
		{
			num -= 50.0;
		}
		else if (bullet.projectileHeight == TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL)
		{
			num -= 20.0;
		}
		_shooterGroundBlockHeights[index] = ((!_groundHeightSnapshotContainsCurve && bullet.height >= num) ? (-1.0 / 0.0) : num);
	}

	private bool CheckShooterGroundBlock(int index, ref BulletData bullet)
	{
		if (bullet.height >= _shooterGroundBlockHeights[index])
		{
			return false;
		}
		HitEffectData(index, null);
		Despawn(index);
		return true;
	}

	private bool TryRunParallelNoInteractionMotion(double delta, ulong currentFrame)
	{
		if (_activeCount < 20000 || _hasRegisteredCharactersForFrame || _allZones.Count > 0 || _mapGridColumnCountForFrame > 0)
		{
			return false;
		}
		_parallelMotionDelta = delta;
		_parallelMotionFrame = currentFrame;
		Volatile.Write(ref _parallelMotionNeedsReconcile, 0);
		if (_parallelMotionRangeWorker == null)
		{
			_parallelMotionRangeWorker = ProcessParallelMotionRange;
		}
		RunParallelRange(_activeCount, _parallelMotionRangeWorker);
		return true;
	}

	private void ProcessParallelMotionRange(int start, int end)
	{
		bool flag = false;
		for (int i = start; i < end; i++)
		{
			int num = _activeIndices[i];
			ref BulletData b = ref _data[num];
			byte b2 = TryProcessParallelNoInteractionMotion(ref b, _parallelMotionDelta, _parallelMotionFrame);
			if ((b2 & 1) != 0 && IsAnimatedMeshShadowCandidate(ref b))
			{
				b2 |= 8;
			}
			_parallelMotionResults[num] = b2;
			flag = flag || b2 != 1;
		}
		if (flag)
		{
			Interlocked.Exchange(ref _parallelMotionNeedsReconcile, 1);
		}
	}

	private static byte TryProcessParallelNoInteractionMotion(ref BulletData b, double delta, ulong currentFrame)
	{
		if (!b.active || b.behaviorProgramId > 0 || (b.catapultSkyDrop && b.catapultSkyDropPhase != CatapultSkyDropPhase.Descending) || b.externalControlled || b.absorbOpen || (b.spawnTweenDuration > 0f && b.spawnTweenTimer < b.spawnTweenDuration) || (b.trackOpen && b.extId >= 0) || b.fireLength != -1 || (b.yOffsetDuration > 0f && b.yOffsetTimer < b.yOffsetDuration))
		{
			return 0;
		}
		float num = b.rotation;
		if (!b.rotateFollowVelocity && b.rotateScale != 0f)
		{
			num += (float)(delta * (double)b.rotateScale * (double)b.fireDirX);
		}
		bool flag = false;
		if (b.catapultOpen)
		{
			if (b.target != null)
			{
				return 0;
			}
			double catapultTimer = b.catapultTimer + delta;
			double num2 = b.ySpeed + 2000.0 * delta;
			double num3 = b.z - num2 * delta;
			if (num3 <= b.groundHeight && num2 >= 0.0)
			{
				return 0;
			}
			b.catapultTimer = catapultTimer;
			b.ySpeed = num2;
			b.z = num3;
			b.pos += b.vel * (float)delta;
			b.collisionEnabled = num2 > 0.0 && num3 - b.groundHeight < 100.0 && !b.blocked;
			flag = !b.lockGridY && (ulong)((long)currentFrame + (long)b.randFreshIndex) % 5uL == 0;
		}
		else if (b.useGravity)
		{
			double num4 = b.ySpeed + b.gravity * (double)b.gravityScale * delta;
			double num5 = b.z - num4 * delta;
			if (num5 <= b.groundHeight)
			{
				return 0;
			}
			b.ySpeed = num4;
			b.z = num5;
			b.collisionEnabled = num4 >= 0.0 && num5 - b.groundHeight < 60.0;
			b.pos += b.vel * (float)delta;
			flag = !b.lockGridY && (ulong)((long)currentFrame + (long)b.randFreshIndex) % 5uL == 0;
		}
		else if (b.useFall)
		{
			if (b.cell != null)
			{
				return 0;
			}
			double num6 = b.ySpeed;
			double num7 = b.z;
			if (num7 > b.groundHeight)
			{
				num6 += b.gravity * (double)b.gravityScale * delta;
				num7 -= num6 * delta;
			}
			if (num7 <= b.groundHeight)
			{
				return 0;
			}
			b.ySpeed = num6;
			b.z = num7;
			b.collisionEnabled = num7 - b.groundHeight <= 100.0;
			b.pos += b.vel * (float)delta;
			flag = !b.lockGridY && (ulong)((long)currentFrame + (long)b.randFreshIndex) % 5uL == 0;
		}
		else if (b.trackOpen)
		{
			if (b.target != null || b.magneticTarget != null)
			{
				return 0;
			}
			Vector2 vector = b.vel;
			if (vector.LengthSquared() <= 0.01f)
			{
				vector = new Vector2(Mathf.Cos(num), Mathf.Sin(num));
			}
			b.vel = vector.Normalized() * b.speed;
			b.pos += b.vel * (float)delta;
			int trackNoTargetInterval = b.trackNoTargetInterval;
			flag = !b.lockGridY && trackNoTargetInterval > 0 && currentFrame % (ulong)trackNoTargetInterval == 0;
		}
		else
		{
			if (b.cell != null)
			{
				return 0;
			}
			b.pos += b.vel * (float)delta;
			flag = !Mathf.IsZeroApprox(b.vel.Y) || (!b.lockGridY && (ulong)((long)currentFrame + (long)b.randFreshIndex) % 5uL == 0);
		}
		b.rotation = num;
		byte b2 = 1;
		if (flag)
		{
			b2 |= 2;
		}
		if (!b.catapultOpen && IsCollisionRectOutOfBounds(ref b))
		{
			b2 |= 4;
		}
		return b2;
	}

	public int GetStaticVisibleInstanceCountForTest()
	{
		int num = 0;
		foreach (StaticBucket value in _staticBuckets.Values)
		{
			if (GodotObject.IsInstanceValid(value?.MultiMesh))
			{
				num += Math.Max(0, value.MultiMesh.VisibleInstanceCount);
			}
		}
		return num;
	}

	private void EnsureRendererInit()
	{
		if (!_rendererInitialized)
		{
			_rendererInitialized = true;
			_sharedQuadMesh = new QuadMesh();
			Shader shader = GD.Load<Shader>("res://Prefab/TowerDefense/Projectile/BulletField/MultiMeshSprite.gdshader");
			if (shader == null)
			{
				shader = new Shader();
				shader.Code = "\nshader_type canvas_item;\n\nuniform sampler2DArray atlasTextureArray : source_color, repeat_disable, filter_linear;\nuniform float atlasLayer = 0.0;\n\nvoid vertex() {\n\tvec2 uvOffset = INSTANCE_CUSTOM.xy;\n\tvec2 uvSize = INSTANCE_CUSTOM.zw;\n\tUV = uvOffset + vec2(UV.x, 1.0 - UV.y) * uvSize;\n}\n\nvoid fragment() {\n\tCOLOR = texture(atlasTextureArray, vec3(UV, floor(atlasLayer + 0.5)));\n}\n\t";
			}
			_staticShader = shader;
			_sharedQuadMesh.Material = null;
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void PrepareStaticEntriesForTrace()
	{
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			List<int> value = rowBucket.Value;
			for (int i = 0; i < value.Count; i++)
			{
				ref BulletData reference = ref _data[value[i]];
				if (reference.renderMode == BulletRenderMode.STATIC && (reference.staticAtlasEntryIndex < 0 || reference.staticAtlasEntryIndex >= _atlasEntries.Count))
				{
					reference.staticAtlasEntryIndex = EnsureSpriteRegistered(reference.config);
				}
			}
		}
	}

	private void BeginStaticFrame()
	{
		_staticFrameVersion++;
		_staticTouchedBucketsLastFrame.Clear();
		for (int i = 0; i < _staticTouchedBucketsThisFrame.Count; i++)
		{
			_staticTouchedBucketsLastFrame.Add(_staticTouchedBucketsThisFrame[i]);
		}
		_staticTouchedBucketsThisFrame.Clear();
	}

	private void WriteStaticBuckets()
	{
		foreach (KeyValuePair<int, List<int>> rowBucket in _rowBuckets)
		{
			int key = rowBucket.Key;
			List<int> value = rowBucket.Value;
			for (int i = 0; i < value.Count; i++)
			{
				ref BulletData reference = ref _data[value[i]];
				if (TryGetStaticEntry(key, ref reference, out var key2, out var entry))
				{
					StaticBucket bucket = EnsureBucket(key2, entry.atlasTextureArray, entry.atlasLayer);
					WriteStaticInstance(bucket, ref reference, entry);
					if (!reference.absorbOpen && (reference.z > 0.0 || reference.height > 0.0) && reference.config != null)
					{
						SubmitBulletShadow(key, ref reference);
					}
				}
			}
		}
	}

	private void EndStaticFrame()
	{
		FlushStaticBuckets();
		HideUntouchedStaticBuckets();
		ReclaimIdleStaticBuckets();
	}

	private void ReclaimIdleStaticBuckets()
	{
		if (_staticFrameVersion < _lastStaticBucketMaintenanceFrame || _staticFrameVersion - _lastStaticBucketMaintenanceFrame < 120)
		{
			return;
		}
		_lastStaticBucketMaintenanceFrame = _staticFrameVersion;
		_staticBucketKeysToRemove.Clear();
		foreach (KeyValuePair<StaticBucketKey, StaticBucket> staticBucket in _staticBuckets)
		{
			long lastTouchedFrame = staticBucket.Value.LastTouchedFrame;
			if (lastTouchedFrame != -9223372036854775808L && _staticFrameVersion >= lastTouchedFrame && _staticFrameVersion - lastTouchedFrame >= 120)
			{
				_staticBucketKeysToRemove.Add(staticBucket.Key);
			}
		}
		for (int i = 0; i < _staticBucketKeysToRemove.Count; i++)
		{
			StaticBucketKey staticBucketKey = _staticBucketKeysToRemove[i];
			if (_staticBuckets.Remove(staticBucketKey, out var value))
			{
				_staticTouchedBucketsLastFrame.Remove(staticBucketKey);
				_staticTouchedBucketsThisFrame.Remove(staticBucketKey);
				ReleaseStaticBucket(value);
			}
		}
		_staticBucketKeysToRemove.Clear();
	}

	private static void ReleaseStaticBucket(StaticBucket bucket)
	{
		if (bucket != null)
		{
			if (GodotObject.IsInstanceValid(bucket.Instance))
			{
				bucket.Instance.Multimesh = null;
				bucket.Instance.QueueFree();
			}
			bucket.MultiMesh?.Dispose();
			bucket.Instance = null;
			bucket.MultiMesh = null;
			bucket.Buffer = System.Array.Empty<float>();
			bucket.Count = 0;
		}
	}

	private void FlushStaticBuckets()
	{
		for (int i = 0; i < _staticTouchedBucketsThisFrame.Count; i++)
		{
			if (_staticBuckets.TryGetValue(_staticTouchedBucketsThisFrame[i], out var value))
			{
				ShrinkStaticBucketIfUnderused(value);
				int num = value.Buffer.Length / 16;
				if (value.MultiMesh.InstanceCount != num)
				{
					value.MultiMesh.InstanceCount = num;
				}
				value.MultiMesh.Buffer = value.Buffer;
				value.MultiMesh.VisibleInstanceCount = value.Count;
			}
		}
	}

	private void HideUntouchedStaticBuckets()
	{
		for (int i = 0; i < _staticTouchedBucketsLastFrame.Count; i++)
		{
			if (_staticBuckets.TryGetValue(_staticTouchedBucketsLastFrame[i], out var value) && value.LastTouchedFrame != _staticFrameVersion && value.MultiMesh != null)
			{
				value.MultiMesh.VisibleInstanceCount = 0;
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void SubmitBulletShadow(int gridY, ref BulletData b)
	{
		float num = (float)((b.z > 0.0) ? b.z : b.height);
		float num2 = (float)Mathf.Max(1.0 - (double)num / 600.0, 0.0);
		if (!(num2 <= 0f) && EnsureBulletShadowSubmission())
		{
			Vector2 size = b.config.size;
			float num3 = _bulletShadowScaleBase.X * size.X * num2;
			float num4 = _bulletShadowScaleBase.Y * size.Y * num2;
			if (num3 != 0f && num4 != 0f)
			{
				float positionY = (b.catapultOpen ? ((float)((double)b.pos.Y + 20.0 - b.groundHeight)) : ((float)((double)b.pos.Y + b.height + 40.0 - b.groundHeight)));
				_bulletShadowSubmission.SubmitAxisAligned(b.pos.X, positionY, num3, num4, BulletShadowColor);
			}
		}
	}

	private void BeginBulletShadowFrame(long physicsFrame)
	{
		_bulletShadowPhysicsFrame = physicsFrame;
		_bulletShadowSubmissionPrepared = false;
	}

	private bool EnsureBulletShadowSubmission()
	{
		if (!_bulletShadowSubmissionPrepared)
		{
			_bulletShadowSubmissionPrepared = true;
			_bulletShadowTexture = TowerDefenseShadowMultiMeshRenderer.DefaultShadowTexture;
			_bulletShadowTextureSize = TowerDefenseShadowMultiMeshRenderer.DefaultShadowTextureSize;
			_bulletShadowScaleBase = _bulletShadowTextureSize / 65f;
			_bulletShadowSubmission = TowerDefenseShadowMultiMeshRenderer.PrepareSubmission(this, _bulletShadowTexture, _bulletShadowPhysicsFrame);
		}
		if (_bulletShadowTexture != null && _bulletShadowTextureSize.X > 0f && _bulletShadowTextureSize.Y > 0f)
		{
			return _bulletShadowSubmission.IsValid;
		}
		return false;
	}

	private int EnsureSpriteRegistered(TowerDefenseProjectileConfig config)
	{
		if (config == null)
		{
			return -1;
		}
		long instanceId = (long)config.GetInstanceId();
		if (_configToAtlasIndex.TryGetValue(instanceId, out var value))
		{
			return value;
		}
		if (!TryGetStaticVisualTemplate(config, out var texture, out var region, out var offset, out var centered, out var usesAtlasAllocation, out var atlasAllocation, out var reason) || !EnsureStaticVisualRegistered(config, texture, region, offset, centered, usesAtlasAllocation, atlasAllocation, out reason))
		{
			return -1;
		}
		return _configToAtlasIndex[instanceId];
	}

	private bool TryPrepareStaticRenderTemplate(TowerDefenseProjectileConfig config, out string reason)
	{
		reason = string.Empty;
		if (config?.projectileScene == null)
		{
			reason = "projectileScene is null";
			return false;
		}
		SyncAtlasCacheVersion();
		if (!TryGetStaticVisualTemplate(config, out var texture, out var region, out var offset, out var centered, out var usesAtlasAllocation, out var atlasAllocation, out reason))
		{
			return false;
		}
		return EnsureStaticVisualRegistered(config, texture, region, offset, centered, usesAtlasAllocation, atlasAllocation, out reason);
	}

	private bool EnsureStaticVisualRegistered(TowerDefenseProjectileConfig config, Texture2D texture, Rect2 region, Vector2 offset, bool centered, bool usesAtlasAllocation, AdobeAnimateExternalTextureAtlasAllocation atlasAllocation, out string reason)
	{
		if (usesAtlasAllocation)
		{
			return EnsureAtlasAllocationRegistered(config, atlasAllocation, offset, centered, out reason);
		}
		if (!GodotObject.IsInstanceValid(texture))
		{
			reason = "Sprite2D texture is missing";
			return false;
		}
		return EnsureSpriteRegistered(config, texture, region, offset, out reason);
	}

	private bool EnsureAtlasAllocationRegistered(TowerDefenseProjectileConfig config, AdobeAnimateExternalTextureAtlasAllocation allocation, Vector2 offset, bool centered, out string reason)
	{
		reason = string.Empty;
		if (config == null || allocation.Rect.Size.X <= 0f || allocation.Rect.Size.Y <= 0f)
		{
			reason = "AdobeAnimatePart atlas allocation is incomplete";
			return false;
		}
		long instanceId = (long)config.GetInstanceId();
		if (_configToAtlasIndex.TryGetValue(instanceId, out var value))
		{
			if (value >= 0 && value < _atlasEntries.Count)
			{
				return true;
			}
			reason = "static render template registration previously failed";
			return false;
		}
		SpriteAtlasEntry spriteAtlasEntry = new SpriteAtlasEntry
		{
			region = new Rect2(Vector2.Zero, allocation.Rect.Size),
			size = allocation.Rect.Size,
			position = Vector2.Zero,
			centered = centered,
			offset = offset,
			runtimePacked = false
		};
		if (allocation.UsesTextureArray && GodotObject.IsInstanceValid(allocation.TextureArray) && allocation.TextureArrayRid.IsValid && allocation.TextureArraySize.X > 0f && allocation.TextureArraySize.Y > 0f)
		{
			SetAtlasTextureArray(spriteAtlasEntry, allocation.TextureArray, allocation.TextureArraySize);
		}
		else
		{
			if (!GodotObject.IsInstanceValid(allocation.Texture) || !TryGetOrCreateStaticTextureArray(allocation.Texture, out var textureArray, out var textureArraySize))
			{
				reason = "AdobeAnimatePart atlas allocation has no usable texture array";
				return false;
			}
			SetAtlasTextureArray(spriteAtlasEntry, textureArray, textureArraySize);
		}
		spriteAtlasEntry.atlasLayer = allocation.AtlasPage;
		spriteAtlasEntry.uvOffset = allocation.Rect.Position / spriteAtlasEntry.atlasTextureArraySize;
		spriteAtlasEntry.uvSize = allocation.Rect.Size / spriteAtlasEntry.atlasTextureArraySize;
		int count = _atlasEntries.Count;
		_atlasEntries.Add(spriteAtlasEntry);
		_configToAtlasIndex[instanceId] = count;
		return true;
	}

	private bool EnsureSpriteRegistered(TowerDefenseProjectileConfig config, Texture2D texture, Rect2 region, Vector2 offset, out string reason)
	{
		reason = string.Empty;
		if (config == null || !GodotObject.IsInstanceValid(texture))
		{
			reason = "static render template is incomplete";
			return false;
		}
		long instanceId = (long)config.GetInstanceId();
		if (_configToAtlasIndex.TryGetValue(instanceId, out var value))
		{
			if (value >= 0 && value < _atlasEntries.Count)
			{
				return true;
			}
			reason = "static render template registration previously failed";
			return false;
		}
		Vector2 size = region.Size;
		if (size.X <= 0f || size.Y <= 0f)
		{
			reason = $"Sprite2D region size is invalid: {size}";
			return false;
		}
		SpriteAtlasEntry spriteAtlasEntry = new SpriteAtlasEntry
		{
			texture = texture,
			region = region,
			size = size,
			position = Vector2.Zero,
			centered = true,
			offset = offset
		};
		if (AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture, region, out var allocation))
		{
			TextureLayered textureArray;
			Vector2 textureArraySize;
			if (GodotObject.IsInstanceValid(allocation.TextureArray) && allocation.TextureArrayRid.IsValid && allocation.TextureArraySize.X > 0f && allocation.TextureArraySize.Y > 0f)
			{
				SetAtlasTextureArray(spriteAtlasEntry, allocation.TextureArray, allocation.TextureArraySize);
				spriteAtlasEntry.atlasLayer = allocation.AtlasPage;
				spriteAtlasEntry.uvOffset = allocation.Rect.Position / allocation.TextureArraySize;
				spriteAtlasEntry.uvSize = allocation.Rect.Size / allocation.TextureArraySize;
			}
			else if (GodotObject.IsInstanceValid(allocation.Texture) && TryGetOrCreateStaticTextureArray(allocation.Texture, out textureArray, out textureArraySize))
			{
				SetAtlasTextureArray(spriteAtlasEntry, textureArray, textureArraySize);
				spriteAtlasEntry.atlasLayer = 0;
				spriteAtlasEntry.uvOffset = allocation.Rect.Position / textureArraySize;
				spriteAtlasEntry.uvSize = allocation.Rect.Size / textureArraySize;
			}
		}
		if (!GodotObject.IsInstanceValid(spriteAtlasEntry.atlasTextureArray))
		{
			Image image;
			try
			{
				image = texture.GetImage();
			}
			catch (Exception ex)
			{
				reason = "Sprite2D texture image read failed: " + ex.Message;
				return false;
			}
			if (!GodotObject.IsInstanceValid(image) || image.IsEmpty())
			{
				reason = "Sprite2D texture has no readable image for runtime atlas packing";
				return false;
			}
			spriteAtlasEntry.runtimePacked = true;
			_atlasDirty = true;
		}
		int count = _atlasEntries.Count;
		_atlasEntries.Add(spriteAtlasEntry);
		_configToAtlasIndex[instanceId] = count;
		if (spriteAtlasEntry.runtimePacked && !TryRebuildAtlas(out reason))
		{
			_configToAtlasIndex.Remove(instanceId);
			_atlasEntries.RemoveAt(count);
			return false;
		}
		if (!GodotObject.IsInstanceValid(spriteAtlasEntry.atlasTextureArray))
		{
			_configToAtlasIndex.Remove(instanceId);
			_atlasEntries.RemoveAt(count);
			reason = "static atlas registration completed without a Texture2DArray";
			return false;
		}
		return true;
	}

	private static void SetAtlasTextureArray(SpriteAtlasEntry entry, TextureLayered textureArray, Vector2 textureArraySize)
	{
		entry.atlasTextureArray = textureArray;
		entry.atlasTextureArrayId = (GodotObject.IsInstanceValid(textureArray) ? textureArray.GetInstanceId() : 0);
		entry.atlasTextureArraySize = textureArraySize;
	}

	private static bool TryGetStaticVisualTemplate(TowerDefenseProjectileConfig config, out Texture2D texture, out Rect2 region, out Vector2 offset, out bool centered, out bool usesAtlasAllocation, out AdobeAnimateExternalTextureAtlasAllocation atlasAllocation, out string reason)
	{
		texture = null;
		region = default(Rect2);
		offset = Vector2.Zero;
		centered = true;
		usesAtlasAllocation = false;
		atlasAllocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		reason = string.Empty;
		if (config?.projectileScene == null)
		{
			reason = "projectileScene is null";
			return false;
		}
		Node2D node2D = null;
		try
		{
			node2D = config.projectileScene.Instantiate<Node2D>(PackedScene.GenEditState.Disabled);
			if (!GodotObject.IsInstanceValid(node2D))
			{
				reason = "projectile scene did not instantiate a Node2D";
				return false;
			}
			if (node2D is AdobeAnimatePart adobeAnimatePart && !string.IsNullOrWhiteSpace(adobeAnimatePart.externalAtlasTexturePath))
			{
				if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(adobeAnimatePart.externalAtlasTexturePath, out atlasAllocation))
				{
					reason = "AdobeAnimatePart external atlas texture is unavailable: " + adobeAnimatePart.externalAtlasTexturePath;
					return false;
				}
				region = new Rect2(Vector2.Zero, atlasAllocation.Rect.Size);
				offset = adobeAnimatePart.Position;
				centered = adobeAnimatePart.externalAtlasCentered;
				usesAtlasAllocation = true;
				return true;
			}
			Sprite2D sprite2D = node2D as Sprite2D;
			if (sprite2D == null)
			{
				sprite2D = node2D.FindChild("*", recursive: true, owned: false) as Sprite2D;
			}
			if (sprite2D == null)
			{
				reason = "projectile scene does not contain a Sprite2D";
				return false;
			}
			texture = sprite2D.Texture;
			if (!GodotObject.IsInstanceValid(texture))
			{
				reason = "Sprite2D texture is missing";
				return false;
			}
			region = (sprite2D.RegionEnabled ? sprite2D.RegionRect : new Rect2(Vector2.Zero, texture.GetSize()));
			offset = sprite2D.Position + sprite2D.Offset;
			if (!sprite2D.Centered)
			{
				offset += region.Size / 2f;
			}
			centered = true;
			return true;
		}
		catch (Exception ex)
		{
			reason = "projectile scene inspection failed: " + ex.Message;
			return false;
		}
		finally
		{
			if (GodotObject.IsInstanceValid(node2D))
			{
				node2D.Free();
			}
		}
	}

	private bool TryRebuildAtlas(out string reason)
	{
		reason = string.Empty;
		_atlasDirty = false;
		List<int> list = new List<int>();
		for (int i = 0; i < _atlasEntries.Count; i++)
		{
			SpriteAtlasEntry spriteAtlasEntry = _atlasEntries[i];
			if (spriteAtlasEntry.runtimePacked && GodotObject.IsInstanceValid(spriteAtlasEntry.texture))
			{
				list.Add(i);
			}
		}
		int count = list.Count;
		if (count == 0)
		{
			return true;
		}
		int num = 1;
		int num2 = 1;
		for (int j = 0; j < count; j++)
		{
			Vector2 size = _atlasEntries[list[j]].size;
			if ((int)size.X > num)
			{
				num = (int)size.X;
			}
			if ((int)size.Y > num2)
			{
				num2 = (int)size.Y;
			}
		}
		int num3 = Mathf.Clamp(num, 1, 2048);
		int num4 = Mathf.Clamp(num2, 1, 2048);
		int num5 = Math.Max(1, 2048 / num3);
		int num6 = Math.Max(1, 2048 / num4);
		int num7 = Math.Max(1, num5 * num6);
		int num8 = Math.Max(1, (count + num7 - 1) / num7);
		Array<Image> array = new Array<Image>();
		for (int k = 0; k < num8; k++)
		{
			Image image = Image.CreateEmpty(2048, 2048, useMipmaps: false, Image.Format.Rgba8);
			image.Fill(new Color(0f, 0f, 0f, 0f));
			array.Add(image);
		}
		for (int l = 0; l < count; l++)
		{
			SpriteAtlasEntry spriteAtlasEntry2 = _atlasEntries[list[l]];
			int num9 = l / num7;
			int num10 = l % num7;
			int num11 = num10 % num5;
			int num12 = num10 / num5;
			int num13 = num11 * num3;
			int num14 = num12 * num4;
			Image image2;
			try
			{
				image2 = spriteAtlasEntry2.texture.GetImage();
			}
			catch (Exception ex)
			{
				reason = "static atlas source image read failed: " + ex.Message;
				return false;
			}
			if (!GodotObject.IsInstanceValid(image2) || image2.IsEmpty())
			{
				reason = "static atlas source image is unavailable";
				return false;
			}
			if (image2.GetFormat() != Image.Format.Rgba8)
			{
				image2.Convert(Image.Format.Rgba8);
			}
			int num15 = Math.Min((int)spriteAtlasEntry2.size.X, 2048 - num13);
			int num16 = Math.Min((int)spriteAtlasEntry2.size.Y, 2048 - num14);
			if (num15 <= 0 || num16 <= 0)
			{
				reason = $"static atlas region size is invalid: {spriteAtlasEntry2.size}";
				return false;
			}
			Rect2I srcRect = new Rect2I((Vector2I)spriteAtlasEntry2.region.Position, new Vector2I(num15, num16));
			array[num9].BlitRect(image2, srcRect, new Vector2I(num13, num14));
			spriteAtlasEntry2.uvOffset = new Vector2((float)num13 / 2048f, (float)num14 / 2048f);
			spriteAtlasEntry2.uvSize = new Vector2(Math.Min(spriteAtlasEntry2.size.X, 2048 - num13) / 2048f, Math.Min(spriteAtlasEntry2.size.Y, 2048 - num14) / 2048f);
			spriteAtlasEntry2.atlasLayer = num9;
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		Error error = texture2DArray.CreateFromImages(array);
		if (error != Error.Ok || !texture2DArray.GetRid().IsValid)
		{
			reason = $"Texture2DArray creation failed or produced an invalid RID: {error}";
			return false;
		}
		_atlasTextureArray = texture2DArray;
		Vector2 textureArraySize = new Vector2(2048f, 2048f);
		for (int m = 0; m < count; m++)
		{
			SetAtlasTextureArray(_atlasEntries[list[m]], _atlasTextureArray, textureArraySize);
		}
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private bool TryGetStaticEntry(int gridY, ref BulletData b, out StaticBucketKey key, out SpriteAtlasEntry entry)
	{
		key = default(StaticBucketKey);
		entry = null;
		if (b.renderMode != BulletRenderMode.STATIC || b.config == null)
		{
			return false;
		}
		int staticAtlasEntryIndex = b.staticAtlasEntryIndex;
		if (staticAtlasEntryIndex < 0 || staticAtlasEntryIndex >= _atlasEntries.Count)
		{
			return false;
		}
		entry = _atlasEntries[staticAtlasEntryIndex];
		if (!GodotObject.IsInstanceValid(entry.atlasTextureArray))
		{
			return false;
		}
		key = new StaticBucketKey(gridY, entry.atlasTextureArrayId, entry.atlasLayer);
		return true;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private StaticBucket EnsureBucket(StaticBucketKey key, TextureLayered atlasTextureArray, int atlasLayer)
	{
		if (_staticBuckets.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value.Instance))
		{
			return TouchStaticBucket(key, value);
		}
		MultiMesh multiMesh = new MultiMesh
		{
			TransformFormat = MultiMesh.TransformFormatEnum.Transform2D,
			Mesh = _sharedQuadMesh,
			UseCustomData = true,
			UseColors = true,
			InstanceCount = 0
		};
		MultiMeshInstance2D multiMeshInstance2D = new MultiMeshInstance2D
		{
			Multimesh = multiMesh,
			Material = GetOrCreateStaticMaterial(atlasTextureArray, atlasLayer)
		};
		long value2 = (long)key.GridY * 15L + 9;
		multiMeshInstance2D.ZIndex = (int)Math.Clamp(value2, -4096L, 4096L);
		AddChild(multiMeshInstance2D, forceReadableName: false, InternalMode.Disabled);
		StaticBucket staticBucket = new StaticBucket
		{
			Instance = multiMeshInstance2D,
			MultiMesh = multiMesh,
			Buffer = new float[1024],
			LastTouchedFrame = _staticFrameVersion
		};
		_staticBuckets[key] = staticBucket;
		_staticTouchedBucketsThisFrame.Add(key);
		return staticBucket;
	}

	private StaticBucket TouchStaticBucket(StaticBucketKey key, StaticBucket bucket)
	{
		if (bucket.LastTouchedFrame != _staticFrameVersion)
		{
			bucket.Count = 0;
			bucket.LastTouchedFrame = _staticFrameVersion;
			_staticTouchedBucketsThisFrame.Add(key);
		}
		return bucket;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private void WriteStaticInstance(StaticBucket bucket, ref BulletData b, SpriteAtlasEntry entry)
	{
		int num = bucket.Count++;
		EnsureStaticBucketCapacity(bucket, bucket.Count);
		int num2 = num * 16;
		Vector2 visualScale = entry.size * b.config.scale * GetAbsorbVisualScale(ref b);
		float staticRenderAngle = GetStaticRenderAngle(ref b);
		Vector2 renderPos = GetRenderPos(ref b);
		if (entry.position != Vector2.Zero)
		{
			float s = ((!b.trackOpen && !b.useFall && !b.rotateFollowVelocity) ? 0f : staticRenderAngle);
			float num3 = Mathf.Cos(s);
			float num4 = Mathf.Sin(s);
			renderPos += new Vector2(num3 * entry.position.X - num4 * entry.position.Y, num4 * entry.position.X + num3 * entry.position.Y);
		}
		Vector2 offset = entry.offset;
		if (!entry.centered)
		{
			offset += entry.size / 2f;
		}
		if (offset != Vector2.Zero)
		{
			renderPos += offset * b.config.scale;
		}
		Transform2D transform2D = BuildProjectileVisualTransform(staticRenderAngle, b.flipX, b.spriteRotationCos, b.spriteRotationSin, visualScale, renderPos);
		bucket.Buffer[num2] = transform2D.X.X;
		bucket.Buffer[num2 + 1] = transform2D.Y.X;
		bucket.Buffer[num2 + 2] = 0f;
		bucket.Buffer[num2 + 3] = transform2D.Origin.X;
		bucket.Buffer[num2 + 4] = transform2D.X.Y;
		bucket.Buffer[num2 + 5] = transform2D.Y.Y;
		bucket.Buffer[num2 + 6] = 0f;
		bucket.Buffer[num2 + 7] = transform2D.Origin.Y;
		bucket.Buffer[num2 + 8] = 1f;
		bucket.Buffer[num2 + 9] = 1f;
		bucket.Buffer[num2 + 10] = 1f;
		bucket.Buffer[num2 + 11] = 1f;
		bucket.Buffer[num2 + 12] = entry.uvOffset.X;
		bucket.Buffer[num2 + 13] = entry.uvOffset.Y;
		bucket.Buffer[num2 + 14] = entry.uvSize.X;
		bucket.Buffer[num2 + 15] = entry.uvSize.Y;
	}

	public static void SetSpriteRotation(ref BulletData bullet, float rotation)
	{
		bullet.spriteRotation = rotation;
		if (Mathf.IsZeroApprox(rotation))
		{
			bullet.spriteRotationCos = 1f;
			bullet.spriteRotationSin = 0f;
		}
		else
		{
			bullet.spriteRotationCos = Mathf.Cos(rotation);
			bullet.spriteRotationSin = Mathf.Sin(rotation);
		}
	}

	private static Transform2D BuildProjectileVisualTransform(float bodyAngle, bool flipX, float spriteCos, float spriteSin, Vector2 visualScale, Vector2 origin)
	{
		float bodyCos = Mathf.Cos(bodyAngle);
		float bodySin = Mathf.Sin(bodyAngle);
		return BuildProjectileVisualTransformFromBasis(bodyCos, bodySin, flipX, spriteCos, spriteSin, visualScale, origin);
	}

	private static Transform2D BuildProjectileVisualTransformFromBasis(float bodyCos, float bodySin, bool flipX, float spriteCos, float spriteSin, Vector2 visualScale, Vector2 origin)
	{
		float num = (flipX ? (-1f) : 1f);
		Vector2 vector = new Vector2(bodyCos * num * spriteCos - bodySin * spriteSin, bodySin * num * spriteCos + bodyCos * spriteSin);
		return new Transform2D(yAxis: new Vector2((0f - bodyCos) * num * spriteSin - bodySin * spriteCos, (0f - bodySin) * num * spriteSin + bodyCos * spriteCos) * visualScale.Y, xAxis: vector * visualScale.X, originPos: origin);
	}

	private static float GetStaticRenderAngle(ref BulletData b)
	{
		if (b.useFall)
		{
			Vector2 vector = new Vector2(b.vel.X, (float)b.ySpeed);
			if (!(vector.LengthSquared() > 0.01f))
			{
				return 0f;
			}
			return vector.Angle();
		}
		if (b.trackOpen)
		{
			return b.rotation;
		}
		if ((b.catapultOpen || b.useGravity) && b.rotateFollowVelocity)
		{
			Vector2 vector2 = new Vector2(b.vel.X, (float)b.ySpeed);
			if (!(vector2.LengthSquared() > 0.01f))
			{
				return 0f;
			}
			return vector2.Angle();
		}
		if (b.rotateFollowVelocity)
		{
			return b.vel.Angle();
		}
		return b.rotation;
	}

	private static void EnsureStaticBucketCapacity(StaticBucket bucket, int count)
	{
		int num = bucket.Buffer.Length / 16;
		if (count > num)
		{
			int num2 = Math.Max(count, Math.Max(64, num * 2));
			System.Array.Resize(ref bucket.Buffer, num2 * 16);
			bucket.UnderusedFrames = 0;
		}
	}

	private static void ShrinkStaticBucketIfUnderused(StaticBucket bucket)
	{
		int num = bucket.Buffer.Length / 16;
		if (num <= 64 || bucket.Count * 4 >= num)
		{
			bucket.UnderusedFrames = 0;
			return;
		}
		bucket.UnderusedFrames++;
		if (bucket.UnderusedFrames >= 90)
		{
			int num2 = Math.Max(64, bucket.Count);
			int num3;
			for (num3 = 64; num3 < num2; num3 *= 2)
			{
			}
			if (num3 < num)
			{
				System.Array.Resize(ref bucket.Buffer, num3 * 16);
			}
			bucket.UnderusedFrames = 0;
		}
	}

	private ShaderMaterial GetOrCreateStaticMaterial(TextureLayered atlasTextureArray, int atlasLayer)
	{
		if (!GodotObject.IsInstanceValid(atlasTextureArray))
		{
			return null;
		}
		StaticMaterialKey key = new StaticMaterialKey(atlasTextureArray, atlasLayer);
		if (_staticShaderMaterials.TryGetValue(key, out var value) && GodotObject.IsInstanceValid(value))
		{
			return value;
		}
		value = new ShaderMaterial
		{
			Shader = _staticShader
		};
		value.SetShaderParameter("atlasTextureArray", atlasTextureArray);
		value.SetShaderParameter("atlasLayer", Math.Max(0, atlasLayer));
		_staticShaderMaterials[key] = value;
		return value;
	}

	private bool TryGetOrCreateStaticTextureArray(Texture2D texture, out TextureLayered textureArray, out Vector2 textureArraySize)
	{
		textureArray = null;
		textureArraySize = default(Vector2);
		if (!GodotObject.IsInstanceValid(texture))
		{
			return false;
		}
		Rid rid = texture.GetRid();
		if (!rid.IsValid)
		{
			return false;
		}
		if (_staticSingleLayerTextureArrays.TryGetValue(rid, out (TextureLayered, Vector2) value) && GodotObject.IsInstanceValid(value.Item1))
		{
			(textureArray, textureArraySize) = value;
			return true;
		}
		Image image = texture.GetImage();
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return false;
		}
		if (image.GetFormat() != Image.Format.Rgba8)
		{
			image.Convert(Image.Format.Rgba8);
		}
		Image image2;
		if (image.GetWidth() == 2048 && image.GetHeight() == 2048)
		{
			image2 = image;
		}
		else
		{
			image2 = Image.CreateEmpty(2048, 2048, useMipmaps: false, Image.Format.Rgba8);
			image2.Fill(new Color(0f, 0f, 0f, 0f));
			int num = Math.Min(image.GetWidth(), 2048);
			int num2 = Math.Min(image.GetHeight(), 2048);
			if (num > 0 && num2 > 0)
			{
				image2.BlitRect(image, new Rect2I(Vector2I.Zero, new Vector2I(num, num2)), Vector2I.Zero);
			}
		}
		Array<Image> images = new Array<Image> { image2 };
		Texture2DArray texture2DArray = new Texture2DArray();
		if (texture2DArray.CreateFromImages(images) != Error.Ok || !texture2DArray.GetRid().IsValid)
		{
			return false;
		}
		Vector2 vector = new Vector2(2048f, 2048f);
		_staticSingleLayerTextureArrays[rid] = (texture2DArray, vector);
		textureArray = texture2DArray;
		textureArraySize = vector;
		return true;
	}

	private void SyncAtlasCacheVersion()
	{
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (_observedAtlasCacheVersion != cacheVersion)
		{
			if (_observedAtlasCacheVersion < 0)
			{
				_observedAtlasCacheVersion = cacheVersion;
				return;
			}
			_observedAtlasCacheVersion = cacheVersion;
			ClearStaticAtlasCache();
		}
	}

	private void ClearStaticAtlasCache()
	{
		if (_data != null)
		{
			for (int i = 0; i < _activeCount; i++)
			{
				int num = _activeIndices[i];
				if (num >= 0 && num < _data.Length)
				{
					_data[num].staticAtlasEntryIndex = -1;
				}
			}
		}
		foreach (KeyValuePair<StaticBucketKey, StaticBucket> staticBucket in _staticBuckets)
		{
			ReleaseStaticBucket(staticBucket.Value);
		}
		_staticBuckets.Clear();
		_staticTouchedBucketsLastFrame.Clear();
		_staticTouchedBucketsThisFrame.Clear();
		_staticBucketKeysToRemove.Clear();
		_staticShaderMaterials.Clear();
		_staticSingleLayerTextureArrays.Clear();
		_configToAtlasIndex.Clear();
		_atlasEntries.Clear();
		_atlasTextureArray = null;
		_atlasDirty = true;
	}

	public static BulletField EnsureMountedOnCharacterNode()
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			return Instance;
		}
		if (TowerDefenseManager.Instance == null)
		{
			return null;
		}
		if (!GodotObject.IsInstanceValid(TowerDefenseManager.Instance.currentControl))
		{
			return null;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		BulletField bulletField = new BulletField
		{
			Name = "BulletField"
		};
		characterNode.AddChild(bulletField, forceReadableName: false, InternalMode.Disabled);
		if (!GodotObject.IsInstanceValid(Instance))
		{
			return bulletField;
		}
		return Instance;
	}

	public Array<Dictionary> ExportBulletFieldSave()
	{
		Array<Dictionary> array = new Array<Dictionary>();
		for (int i = 0; i < _activeCount; i++)
		{
			int num = _activeIndices[i];
			if (_data[num].active && !_data[num].over && !_data[num].absorbOpen)
			{
				array.Add(ExportBulletSave(num));
			}
		}
		return array;
	}

	public void ImportBulletFieldSave(Array<Dictionary> savedBullets, TowerDefenseLevelSaveConfigCSharp owner)
	{
		if (owner == null)
		{
			owner = new TowerDefenseLevelSaveConfigCSharp();
		}
		ClearActiveBullets();
		if (savedBullets == null)
		{
			return;
		}
		foreach (Dictionary bulletSave in savedBullets)
		{
			owner.RestoreReport.Try("Projectile", delegate
			{
				if (bulletSave == null || bulletSave.Count == 0 || !TryImportBulletSave(bulletSave, owner, out var data))
				{
					owner.RestoreReport.Record("Projectile", "Skipped unavailable bullet record.");
				}
				else if (Spawn(in data) < 0)
				{
					owner.RestoreReport.Record("Projectile", "Bullet capacity exhausted.");
				}
			});
		}
	}

	public void ClearActiveBullets()
	{
		while (_activeCount > 0)
		{
			Despawn(_activeIndices[_activeCount - 1]);
		}
		LastSpawnedIndex = -1;
		_collisionCandidates.Clear();
		ClearCollisionCandidateCacheStorage();
	}

	private Dictionary ExportBulletSave(int index)
	{
		ref BulletData reference = ref _data[index];
		return new Dictionary
		{
			["configName"] = ((reference.config != null) ? reference.config.name : ""),
			["skinName"] = ((reference.config != null) ? reference.config.skinName.ToString() : "Default"),
			["pos"] = reference.pos,
			["vel"] = reference.vel,
			["speed"] = reference.speed,
			["gridY"] = reference.gridY,
			["gridPos"] = reference.gridPos,
			["camp"] = (int)reference.camp,
			["collisionFlags"] = reference.collisionFlags,
			["projectileHeight"] = (int)reference.projectileHeight,
			["checkHeight"] = reference.checkHeight,
			["damage"] = reference.damage,
			["damageFlags"] = reference.damageFlags,
			["fireCharacterName"] = GetSavedCharacterName(reference.fireCharacter),
			["fireLength"] = reference.fireLength,
			["savePos"] = reference.savePos,
			["checkDistance"] = reference.checkDistance,
			["rectPosition"] = reference.rect.Position,
			["rectSize"] = reference.rect.Size,
			["fireDirX"] = reference.fireDirX,
			["rotateScale"] = reference.rotateScale,
			["rotateFollowVelocity"] = reference.rotateFollowVelocity,
			["rotation"] = reference.rotation,
			["spriteRotation"] = reference.spriteRotation,
			["itemLayer"] = reference.itemLayer,
			["flipX"] = reference.flipX,
			["hitBoxScale"] = reference.hitBoxScale,
			["extId"] = reference.extId,
			["trackOpen"] = reference.trackOpen,
			["randFreshIndex"] = reference.randFreshIndex,
			["z"] = reference.z,
			["ySpeed"] = reference.ySpeed,
			["groundHeight"] = reference.groundHeight,
			["height"] = reference.height,
			["useGravity"] = reference.useGravity,
			["gravityScale"] = reference.gravityScale,
			["gravity"] = reference.gravity,
			["useFall"] = reference.useFall,
			["catapultOpen"] = reference.catapultOpen,
			["lockGridY"] = reference.lockGridY,
			["catapultTimer"] = reference.catapultTimer,
			["catapultTime"] = reference.catapultTime,
			["catapultTargetPos"] = reference.catapultTargetPos,
			["catapultSkyDrop"] = reference.catapultSkyDrop,
			["catapultSkyDropPhase"] = (int)reference.catapultSkyDropPhase,
			["catapultSkyDropLaunchPos"] = reference.catapultSkyDropLaunchPos,
			["catapultSkyDropLaunchZ"] = reference.catapultSkyDropLaunchZ,
			["catapultSkyDropAscentEndZ"] = reference.catapultSkyDropAscentEndZ,
			["catapultSkyDropAscentElapsed"] = reference.catapultSkyDropAscentElapsed,
			["catapultSkyDropAscentDuration"] = reference.catapultSkyDropAscentDuration,
			["catapultSkyDropAscentHorizontalOffset"] = reference.catapultSkyDropAscentHorizontalOffset,
			["catapultSkyDropWaitRemaining"] = reference.catapultSkyDropWaitRemaining,
			["catapultSkyDropOffscreenWaitSeconds"] = reference.catapultSkyDropOffscreenWaitSeconds,
			["catapultSkyDropVisualRadius"] = reference.catapultSkyDropVisualRadius,
			["isGround"] = reference.isGround,
			["blocked"] = reference.blocked,
			["targetName"] = GetSavedCharacterName(reference.target),
			["magneticTargetName"] = GetSavedCharacterName(reference.magneticTarget),
			["trackSearchInterval"] = reference.trackSearchInterval,
			["trackNoTargetInterval"] = reference.trackNoTargetInterval,
			["penetrateNum"] = reference.penetrateNum,
			["lastHitInstanceId"] = reference.lastHitInstanceId.ToString(),
			["hitOver"] = reference.hitOver,
			["checkAll"] = reference.checkAll,
			["collisionEnabled"] = reference.collisionEnabled,
			["suppressGameplay"] = reference.suppressGameplay,
			["portalReleased"] = reference.portalReleased,
			["fireMethodFlags"] = reference.fireMethodFlags,
			["landOverSubscribed"] = reference.landOverSubscribed,
			["spawnSourceInstanceId"] = reference.spawnSourceInstanceId.ToString(),
			["lifecycleSubscribed"] = reference.lifecycleSubscribed,
			["lifecycleTerminalEmitted"] = reference.lifecycleTerminalEmitted,
			["lifecycleOwnerSequence"] = reference.lifecycleOwnerSequence,
			["lifecycleSlot"] = reference.lifecycleSlot,
			["animElapsedTimer"] = ((reference.renderMode == BulletRenderMode.ANIMATED_MESH) ? GetAnimatedMeshFrame(ref reference) : reference.animElapsedTimer),
			["yOffset"] = reference.yOffset,
			["yOffsetTarget"] = reference.yOffsetTarget,
			["yOffsetTimer"] = reference.yOffsetTimer,
			["yOffsetDuration"] = reference.yOffsetDuration,
			["spawnTweenStartPos"] = reference.spawnTweenStartPos,
			["spawnTweenOffset"] = reference.spawnTweenOffset,
			["spawnTweenDuration"] = reference.spawnTweenDuration,
			["spawnTweenTimer"] = reference.spawnTweenTimer,
			["spawnTweenEase"] = (int)reference.spawnTweenEase,
			["spawnTweenTrans"] = (int)reference.spawnTweenTrans,
			["externalControlled"] = reference.externalControlled
		};
	}

	private bool TryImportBulletSave(Dictionary data, TowerDefenseLevelSaveConfigCSharp owner, out BulletData bullet)
	{
		bullet = default(BulletData);
		data = data.Duplicate();
		KeyValuePair<Variant, Variant>[] array = data.ToArray();
		for (int i = 0; i < array.Length; i++)
		{
			KeyValuePair<Variant, Variant> keyValuePair = array[i];
			if (keyValuePair.Value.VariantType switch
			{
				Variant.Type.Float => !double.IsFinite(keyValuePair.Value.AsDouble()) || Math.Abs(keyValuePair.Value.AsDouble()) > 3.4028234663852886E+38, 
				Variant.Type.Vector2 => !keyValuePair.Value.AsVector2().IsFinite(), 
				_ => false, 
			})
			{
				data.Remove(keyValuePair.Key);
				owner.RestoreReport.Record("Projectile", "Invalid bullet number; using default.");
			}
		}
		string text = data.GetValueOrDefault("configName", "").AsString();
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		string text2 = data.GetValueOrDefault("skinName", "Default").AsString();
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = BuildTemplateConfig(new StringName(text), new StringName(string.IsNullOrEmpty(text2) ? "Default" : text2));
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig))
		{
			towerDefenseProjectileConfig = TowerDefenseManager.GetProjectileConfig(text);
		}
		if (!GodotObject.IsInstanceValid(towerDefenseProjectileConfig))
		{
			return false;
		}
		Vector2 vector = data.GetValueOrDefault("pos", Vector2.Zero).AsVector2();
		Vector2I gridPos = data.GetValueOrDefault("gridPos", Vector2I.Zero).AsVector2I();
		if (!SetupRenderSaveFields(towerDefenseProjectileConfig, out var renderMode, out var animDefId, out var animOffset, out var animFrameMax, out var animFrameRate, out var renderReason))
		{
			owner.RestoreReport.Record("Projectile", renderReason);
			return false;
		}
		if (!TryGetOrCompileBehaviorProgram(towerDefenseProjectileConfig, out var programId, out var errorReason))
		{
			owner.RestoreReport.Record("Projectile", errorReason);
			return false;
		}
		bool flag = data.GetValueOrDefault("catapultOpen", false).AsBool();
		bool flag2 = data.GetValueOrDefault("checkAll", false).AsBool();
		bool lockGridY = data.GetValueOrDefault("lockGridY", flag && !flag2).AsBool();
		bool num = data.ContainsKey("catapultSkyDropPhase");
		bool flag3 = data.GetValueOrDefault("catapultSkyDropPending", false).AsBool();
		CatapultSkyDropPhase catapultSkyDropPhase = (num ? ((CatapultSkyDropPhase)Mathf.Clamp(data["catapultSkyDropPhase"].AsInt32(), 0, 3)) : (flag3 ? CatapultSkyDropPhase.Ascending : CatapultSkyDropPhase.None));
		bool flag4 = data.GetValueOrDefault("catapultSkyDrop", catapultSkyDropPhase != CatapultSkyDropPhase.None).AsBool();
		if (!num && !flag3 && flag4)
		{
			catapultSkyDropPhase = CatapultSkyDropPhase.Descending;
		}
		bullet = new BulletData
		{
			active = true,
			over = false,
			renderMode = renderMode,
			pos = vector,
			vel = data.GetValueOrDefault("vel", Vector2.Zero).AsVector2(),
			speed = (float)data.GetValueOrDefault("speed", 0.0).AsDouble(),
			gridY = data.GetValueOrDefault("gridY", gridPos.Y).AsInt32(),
			gridPos = gridPos,
			camp = (TowerDefenseEnum.CHARACTER_CAMP)data.GetValueOrDefault("camp", 2).AsInt32(),
			collisionFlags = data.GetValueOrDefault("collisionFlags", towerDefenseProjectileConfig.collisionFlags).AsInt32(),
			projectileHeight = (TowerDefenseEnum.CHARACTER_HEIGHT)data.GetValueOrDefault("projectileHeight", 2).AsInt32(),
			checkHeight = data.GetValueOrDefault("checkHeight", false).AsBool(),
			damage = data.GetValueOrDefault("damage", towerDefenseProjectileConfig.baseDamage).AsDouble(),
			damageFlags = data.GetValueOrDefault("damageFlags", towerDefenseProjectileConfig.damageFlags).AsInt32(),
			config = towerDefenseProjectileConfig,
			behaviorProgramId = programId,
			fireCharacter = ResolveSavedCharacter(owner, data.GetValueOrDefault("fireCharacterName", "").AsString()),
			fireLength = data.GetValueOrDefault("fireLength", -1).AsInt32(),
			savePos = data.GetValueOrDefault("savePos", vector).AsVector2(),
			checkDistance = (float)data.GetValueOrDefault("checkDistance", 0.0).AsDouble(),
			rect = new Rect2(data.GetValueOrDefault("rectPosition", Vector2.Zero).AsVector2(), data.GetValueOrDefault("rectSize", Vector2.Zero).AsVector2()),
			fireDirX = (float)data.GetValueOrDefault("fireDirX", 1.0).AsDouble(),
			rotateScale = (float)data.GetValueOrDefault("rotateScale", towerDefenseProjectileConfig.rotateScale).AsDouble(),
			rotateFollowVelocity = data.GetValueOrDefault("rotateFollowVelocity", towerDefenseProjectileConfig.rotateFollowVelocity).AsBool(),
			rotation = (float)data.GetValueOrDefault("rotation", 0.0).AsDouble(),
			spriteRotation = (float)data.GetValueOrDefault("spriteRotation", GetSceneAnimRotation(towerDefenseProjectileConfig.projectileScene)).AsDouble(),
			itemLayer = data.GetValueOrDefault("itemLayer", 9).AsInt32(),
			flipX = data.GetValueOrDefault("flipX", false).AsBool(),
			hitBoxScale = data.GetValueOrDefault("hitBoxScale", Vector2.One).AsVector2(),
			extId = data.GetValueOrDefault("extId", -1).AsInt32(),
			trackOpen = data.GetValueOrDefault("trackOpen", false).AsBool(),
			randFreshIndex = data.GetValueOrDefault("randFreshIndex", (int)GD.Randi()).AsInt32(),
			z = data.GetValueOrDefault("z", 0.0).AsDouble(),
			ySpeed = data.GetValueOrDefault("ySpeed", 0.0).AsDouble(),
			groundHeight = data.GetValueOrDefault("groundHeight", 0.0).AsDouble(),
			height = data.GetValueOrDefault("height", 0.0).AsDouble(),
			useGravity = data.GetValueOrDefault("useGravity", false).AsBool(),
			gravityScale = (float)data.GetValueOrDefault("gravityScale", 1.5).AsDouble(),
			gravity = data.GetValueOrDefault("gravity", 245.0).AsDouble(),
			useFall = data.GetValueOrDefault("useFall", false).AsBool(),
			cell = TowerDefenseManager.GetMapCell(gridPos),
			catapultOpen = flag,
			lockGridY = lockGridY,
			catapultTimer = data.GetValueOrDefault("catapultTimer", 0.0).AsDouble(),
			catapultTime = data.GetValueOrDefault("catapultTime", 0.0).AsDouble(),
			catapultTargetPos = data.GetValueOrDefault("catapultTargetPos", Vector2.Zero).AsVector2(),
			catapultSkyDrop = flag4,
			catapultSkyDropPhase = catapultSkyDropPhase,
			catapultSkyDropLaunchPos = data.GetValueOrDefault("catapultSkyDropLaunchPos", vector).AsVector2(),
			catapultSkyDropLaunchZ = data.GetValueOrDefault("catapultSkyDropLaunchZ", data.GetValueOrDefault("height", 0.0)).AsDouble(),
			catapultSkyDropAscentEndZ = data.GetValueOrDefault("catapultSkyDropAscentEndZ", data.GetValueOrDefault("z", 0.0)).AsDouble(),
			catapultSkyDropAscentElapsed = data.GetValueOrDefault("catapultSkyDropAscentElapsed", 0.0).AsDouble(),
			catapultSkyDropAscentDuration = data.GetValueOrDefault("catapultSkyDropAscentDuration", 0.01).AsDouble(),
			catapultSkyDropAscentHorizontalOffset = (float)data.GetValueOrDefault("catapultSkyDropAscentHorizontalOffset", 240.0).AsDouble(),
			catapultSkyDropWaitRemaining = data.GetValueOrDefault("catapultSkyDropWaitRemaining", 0.0).AsDouble(),
			catapultSkyDropOffscreenWaitSeconds = data.GetValueOrDefault("catapultSkyDropOffscreenWaitSeconds", 1.5).AsDouble(),
			catapultSkyDropVisualRadius = (float)data.GetValueOrDefault("catapultSkyDropVisualRadius", 0.0).AsDouble(),
			isGround = data.GetValueOrDefault("isGround", false).AsBool(),
			blocked = data.GetValueOrDefault("blocked", false).AsBool(),
			target = ResolveSavedCharacter(owner, data.GetValueOrDefault("targetName", "").AsString()),
			magneticTarget = ResolveSavedCharacter(owner, data.GetValueOrDefault("magneticTargetName", "").AsString()),
			trackSearchInterval = data.GetValueOrDefault("trackSearchInterval", towerDefenseProjectileConfig.trackSearchInterval).AsInt32(),
			trackNoTargetInterval = data.GetValueOrDefault("trackNoTargetInterval", towerDefenseProjectileConfig.trackSearchInterval * 4).AsInt32(),
			penetrateNum = data.GetValueOrDefault("penetrateNum", towerDefenseProjectileConfig.penetrateNum).AsInt32(),
			lastHitInstanceId = ParseUInt64(data.GetValueOrDefault("lastHitInstanceId", "0")),
			hitOver = data.GetValueOrDefault("hitOver", false).AsBool(),
			checkAll = flag2,
			collisionEnabled = data.GetValueOrDefault("collisionEnabled", true).AsBool(),
			suppressGameplay = data.GetValueOrDefault("suppressGameplay", false).AsBool(),
			portalReleased = data.GetValueOrDefault("portalReleased", false).AsBool(),
			fireMethodFlags = data.GetValueOrDefault("fireMethodFlags", towerDefenseProjectileConfig.fireMethodFlags).AsInt32(),
			landOverSubscribed = data.GetValueOrDefault("landOverSubscribed", false).AsBool(),
			spawnSourceInstanceId = ParseUInt64(data.GetValueOrDefault("spawnSourceInstanceId", "0")),
			lifecycleSubscribed = data.GetValueOrDefault("lifecycleSubscribed", false).AsBool(),
			lifecycleTerminalEmitted = data.GetValueOrDefault("lifecycleTerminalEmitted", false).AsBool(),
			lifecycleOwnerSequence = data.GetValueOrDefault("lifecycleOwnerSequence", 0).AsInt32(),
			lifecycleSlot = data.GetValueOrDefault("lifecycleSlot", -1).AsInt32(),
			animDefId = animDefId,
			animOffset = animOffset,
			animElapsedTimer = ((renderMode == BulletRenderMode.ANIMATED_MESH) ? GetAnimatedMeshPhaseFromFrame((float)data.GetValueOrDefault("animElapsedTimer", 0.0).AsDouble(), animFrameMax, animFrameRate) : ((float)data.GetValueOrDefault("animElapsedTimer", 0.0).AsDouble())),
			animFrameMax = animFrameMax,
			animFrameRate = animFrameRate,
			yOffset = (float)data.GetValueOrDefault("yOffset", 0.0).AsDouble(),
			yOffsetTarget = (float)data.GetValueOrDefault("yOffsetTarget", 0.0).AsDouble(),
			yOffsetTimer = (float)data.GetValueOrDefault("yOffsetTimer", 0.0).AsDouble(),
			yOffsetDuration = (float)data.GetValueOrDefault("yOffsetDuration", 0.0).AsDouble(),
			spawnTweenStartPos = data.GetValueOrDefault("spawnTweenStartPos", vector).AsVector2(),
			spawnTweenOffset = data.GetValueOrDefault("spawnTweenOffset", Vector2.Zero).AsVector2(),
			spawnTweenDuration = (float)data.GetValueOrDefault("spawnTweenDuration", 0.0).AsDouble(),
			spawnTweenTimer = (float)data.GetValueOrDefault("spawnTweenTimer", 0.0).AsDouble(),
			spawnTweenEase = (Tween.EaseType)data.GetValueOrDefault("spawnTweenEase", 0).AsInt32(),
			spawnTweenTrans = (Tween.TransitionType)data.GetValueOrDefault("spawnTweenTrans", 0).AsInt32(),
			externalControlled = false
		};
		SetSpriteRotation(ref bullet, bullet.spriteRotation);
		return true;
	}

	private bool SetupRenderSaveFields(TowerDefenseProjectileConfig config, out BulletRenderMode renderMode, out int animDefId, out Vector2 animOffset, out int animFrameMax, out double animFrameRate, out string renderReason)
	{
		renderMode = BulletRenderMode.STATIC;
		animDefId = -1;
		animOffset = Vector2.Zero;
		animFrameMax = 0;
		animFrameRate = 0.0;
		renderReason = string.Empty;
		ProjectileRenderTemplate orCreateRenderTemplate = GetOrCreateRenderTemplate(config);
		if (!orCreateRenderTemplate.IsValid)
		{
			renderReason = orCreateRenderTemplate.ErrorCode + ": " + orCreateRenderTemplate.ErrorReason;
			return false;
		}
		renderMode = orCreateRenderTemplate.RenderMode;
		animDefId = orCreateRenderTemplate.AnimationDefinitionId;
		animOffset = orCreateRenderTemplate.AnimationOffset;
		if (renderMode == BulletRenderMode.STATIC)
		{
			return true;
		}
		AnimateMultiMeshRenderer.Definition definition = AnimRenderer.GetDefinition(animDefId);
		if (definition == null || definition.frameMax <= 0)
		{
			renderReason = $"E_ANIMATION_DEFINITION_INVALID: Compact Crowd definition {animDefId} is unavailable";
			return false;
		}
		renderMode = BulletRenderMode.ANIMATED_MESH;
		animFrameMax = definition.frameMax;
		animFrameRate = definition.frameRate * Math.Abs(GetSceneAnimTimeScale(config.projectileScene));
		return true;
	}

	private static string GetSavedCharacterName(TowerDefenseCharacter character)
	{
		if (!GodotObject.IsInstanceValid(character))
		{
			return "";
		}
		return character.Name.ToString().ValidateNodeName();
	}

	private static TowerDefenseCharacter ResolveSavedCharacter(TowerDefenseLevelSaveConfigCSharp owner, string nodeName)
	{
		if (owner == null || string.IsNullOrEmpty(nodeName))
		{
			return null;
		}
		StringName key = new StringName(nodeName);
		if (!owner.charcterDicionary.TryGetValue(key, out var value) || !GodotObject.IsInstanceValid(value))
		{
			return null;
		}
		return value;
	}

	private static ulong ParseUInt64(Variant value)
	{
		if (!ulong.TryParse(value.AsString(), out var result))
		{
			return 0uL;
		}
		return result;
	}

	public static void QueueRenderTemplateWarmup(TowerDefenseProjectileCreateData projectileData)
	{
		if (projectileData != null && !(projectileData.projectileName == null))
		{
			(StringName, StringName) tuple = (projectileData.projectileName, projectileData.skinName);
			int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
			if ((!GodotObject.IsInstanceValid(Instance) || !Instance._warmedRenderTemplateVersions.TryGetValue(tuple, out var value) || value != cacheVersion) && _queuedRenderTemplateWarmups.Add(tuple))
			{
				_renderTemplateWarmupQueue.Enqueue(tuple);
			}
		}
	}

	internal void ProcessQueuedRenderTemplateWarmups(int maxDefinitions)
	{
		for (int i = 0; i < maxDefinitions; i++)
		{
			if (_renderTemplateWarmupQueue.Count <= 0)
			{
				break;
			}
			(StringName, StringName) tuple = _renderTemplateWarmupQueue.Dequeue();
			_queuedRenderTemplateWarmups.Remove(tuple);
			TowerDefenseProjectileConfig towerDefenseProjectileConfig = BuildTemplateConfig(tuple.Item1, tuple.Item2);
			if (towerDefenseProjectileConfig?.projectileScene != null)
			{
				ProjectileRenderTemplate orCreateRenderTemplate = GetOrCreateRenderTemplate(towerDefenseProjectileConfig);
				_warmedRenderTemplateVersions[tuple] = orCreateRenderTemplate.AtlasCacheVersion;
			}
		}
	}

	private uint NextSpawnRandom()
	{
		uint spawnRandomState = _spawnRandomState;
		spawnRandomState ^= spawnRandomState << 13;
		spawnRandomState ^= spawnRandomState >> 17;
		spawnRandomState ^= spawnRandomState << 5;
		_spawnRandomState = ((spawnRandomState != 0) ? spawnRandomState : 2654435769u);
		return _spawnRandomState;
	}

	private float NextSpawnRandom(float upperExclusive)
	{
		return (float)(NextSpawnRandom() >> 8) * 5.9604645E-08f * upperExclusive;
	}

	private int RejectSpawn(TowerDefenseProjectileConfig config, string code, string reason)
	{
		StringName stringName = config?.NameSN ?? new StringName("<null>");
		string text = config?.projectileScene?.ResourcePath ?? "<none>";
		(StringName, string, string, int) item = (stringName, text, code, AdobeAnimateGlobalAtlasCache.CacheVersion);
		if (_reportedSpawnErrors.Add(item))
		{
			GD.PushError($"[BulletField:{code}] projectile='{stringName}' scene='{text}' reason='{reason}'");
		}
		return -1;
	}

	private ProjectileRenderTemplate GetOrCreateRenderTemplate(TowerDefenseProjectileConfig config)
	{
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (config?.projectileScene == null)
		{
			return new ProjectileRenderTemplate(isValid: false, "E_SCENE_MISSING", "projectileScene is null", BulletRenderMode.STATIC, -1, Vector2.Zero, 0f, 1.0, cacheVersion);
		}
		PackedScene projectileScene = config.projectileScene;
		if (_renderTemplateCache.TryGetValue(config, out var value) && value.AtlasCacheVersion == cacheVersion)
		{
			return value;
		}
		AdobeAnimateData adobeAnimateData = DetectAnimatedData(projectileScene);
		Vector2 sceneAnimOffset = GetSceneAnimOffset(projectileScene);
		float sceneAnimRotation = GetSceneAnimRotation(projectileScene);
		double sceneAnimTimeScale = GetSceneAnimTimeScale(projectileScene);
		ProjectileRenderTemplate template;
		if (adobeAnimateData == null)
		{
			template = (TryPrepareStaticRenderTemplate(config, out var reason) ? new ProjectileRenderTemplate(isValid: true, string.Empty, string.Empty, BulletRenderMode.STATIC, -1, Vector2.Zero, 0f, 1.0, cacheVersion) : new ProjectileRenderTemplate(isValid: false, "E_STATIC_RENDER_UNSUPPORTED", reason, BulletRenderMode.STATIC, -1, Vector2.Zero, 0f, 1.0, cacheVersion));
		}
		else if (!CanUseSceneAnimatedMesh(projectileScene))
		{
			template = new ProjectileRenderTemplate(isValid: false, "E_ANIMATED_SCENE_UNSUPPORTED", "animated scene is incompatible with Compact Crowd rendering", BulletRenderMode.ANIMATED_MESH, -1, sceneAnimOffset, sceneAnimRotation, sceneAnimTimeScale, cacheVersion);
		}
		else
		{
			int num = AnimRenderer.RegisterDefinition(adobeAnimateData, GetSceneAnimClip(projectileScene), 9);
			AnimateMultiMeshRenderer.Definition definition = ((num >= 0) ? AnimRenderer.GetDefinition(num) : null);
			if (num < 0 || definition == null || definition.frameMax <= 0)
			{
				string errorReason = ((num < 0) ? ("Compact Crowd definition registration failed: " + AnimateMultiMeshRenderer.DescribeCompactCompatibility(adobeAnimateData)) : ((definition == null) ? $"Compact Crowd definition {num} is missing" : $"Compact Crowd definition {num} has frameMax {definition.frameMax}"));
				template = new ProjectileRenderTemplate(isValid: false, "E_ANIMATION_DEFINITION_INVALID", errorReason, BulletRenderMode.ANIMATED_MESH, -1, sceneAnimOffset, sceneAnimRotation, sceneAnimTimeScale, cacheVersion);
			}
			else
			{
				template = new ProjectileRenderTemplate(isValid: true, string.Empty, string.Empty, BulletRenderMode.ANIMATED_MESH, num, sceneAnimOffset, sceneAnimRotation, sceneAnimTimeScale, cacheVersion);
			}
		}
		_renderTemplateCache[config] = template;
		PrewarmRenderTemplateRows(in template);
		return template;
	}

	internal bool TryValidateRenderTemplateForTest(TowerDefenseProjectileConfig config, out string errorCode, out string errorReason)
	{
		ProjectileRenderTemplate orCreateRenderTemplate = GetOrCreateRenderTemplate(config);
		errorCode = orCreateRenderTemplate.ErrorCode;
		errorReason = orCreateRenderTemplate.ErrorReason;
		return orCreateRenderTemplate.IsValid;
	}

	private void PrewarmRenderTemplateRows(in ProjectileRenderTemplate template)
	{
		if (template.RenderMode == BulletRenderMode.ANIMATED_MESH && template.AnimationDefinitionId >= 0 && GodotObject.IsInstanceValid(TowerDefenseManager.Instance))
		{
			AnimRenderer.PrewarmDefinitionRows(template.AnimationDefinitionId, Math.Max(0, TowerDefenseManager.Instance.gridNum.Y));
		}
	}

	public int TrySpawnFromConfig(TowerDefenseProjectileConfig config, Vector2 pos, Vector2 vel, double speed, TowerDefenseCharacter fireCharacter, TowerDefenseEnum.CHARACTER_CAMP camp, Vector2I gridPos, int gridY, Rect2 rect, TowerDefenseCharacter target = null, double height = 0.0, double groundHeight = 0.0, int collisionFlagsOverride = -1, float fireLength = -1f, bool checkHeight = false, bool checkAll = false, bool useFall = false, bool useGravity = false, float gravityScale = 1.5f, float yOffsetTarget = 0f, float yOffsetDuration = 0f, BulletFieldSpawnOverrides overrides = default(BulletFieldSpawnOverrides))
	{
		if (config == null)
		{
			return RejectSpawn(null, "E_CONFIG_NULL", "projectile config is null");
		}
		if (!GodotObject.IsInstanceValid(this))
		{
			return RejectSpawn(config, "E_FIELD_UNAVAILABLE", "BulletField is unavailable");
		}
		if (!TryGetOrCompileBehaviorProgram(config, out var programId, out var errorReason))
		{
			return RejectSpawn(config, "E_BEHAVIOR_UNSUPPORTED", errorReason);
		}
		ProjectileRenderTemplate orCreateRenderTemplate = GetOrCreateRenderTemplate(config);
		if (!orCreateRenderTemplate.IsValid)
		{
			return RejectSpawn(config, orCreateRenderTemplate.ErrorCode, orCreateRenderTemplate.ErrorReason);
		}
		useFall = useFall || overrides.useFall;
		useGravity = useGravity || overrides.useGravity;
		bool lockGridY = overrides.gridYOverride.HasValue;
		if (overrides.gridYOverride.HasValue)
		{
			gridY = overrides.gridYOverride.Value;
			gridPos = new Vector2I(gridPos.X, gridY);
		}
		int animationDefinitionId = orCreateRenderTemplate.AnimationDefinitionId;
		BulletRenderMode renderMode = orCreateRenderTemplate.RenderMode;
		int num = 0;
		double animFrameRate = 0.0;
		float animElapsedTimer = 0f;
		Vector2 animationOffset = orCreateRenderTemplate.AnimationOffset;
		if (renderMode == BulletRenderMode.ANIMATED_MESH)
		{
			AnimateMultiMeshRenderer.Definition definition = AnimRenderer.GetDefinition(animationDefinitionId);
			if (definition == null || definition.frameMax <= 0)
			{
				return RejectSpawn(config, "E_ANIMATION_DEFINITION_INVALID", $"Compact Crowd definition {animationDefinitionId} is unavailable at spawn");
			}
			num = definition.frameMax;
			animFrameRate = definition.frameRate * Math.Abs(orCreateRenderTemplate.AnimationTimeScale);
			animElapsedTimer = NextSpawnRandom(num);
		}
		int num2 = overrides.fireMethodFlagsOverride ?? config.fireMethodFlags;
		int num3 = collisionFlagsOverride;
		if (num3 == -1)
		{
			num3 = ((!GodotObject.IsInstanceValid(fireCharacter)) ? config.collisionFlags : fireCharacter.instance.collisionFlags);
		}
		TowerDefenseEnum.CHARACTER_HEIGHT projectileHeight = TowerDefenseEnum.CHARACTER_HEIGHT.NORMAL;
		if (GodotObject.IsInstanceValid(fireCharacter))
		{
			projectileHeight = (TowerDefenseEnum.CHARACTER_HEIGHT)Mathf.Min(2, (int)fireCharacter.instance.height);
		}
		float fireDirX = ((vel.X != 0f) ? ((float)Mathf.Sign(vel.X)) : 1f);
		int num4 = Math.Max(1, overrides.trackSearchIntervalOverride ?? config.trackSearchInterval);
		int trackNoTargetInterval = num4 * 4;
		float checkDistance = 0f;
		if (fireLength != -1f)
		{
			checkDistance = TowerDefenseManager.Instance.GetMapGridSize().X * fireLength;
		}
		bool flag = (num2 & 0x20) != 0;
		bool flag2 = (num2 & 2) != 0;
		if (useFall || useGravity)
		{
			flag2 = false;
		}
		if (flag)
		{
			checkAll = true;
		}
		else if (overrides.checkAllOverride.HasValue)
		{
			checkAll = overrides.checkAllOverride.Value;
		}
		if (flag2 && !checkAll)
		{
			lockGridY = true;
		}
		ulong physicsFrame = ((flag || flag2) ? Engine.GetPhysicsFrames() : 18446744073709551615uL);
		TowerDefenseCharacter towerDefenseCharacter = target;
		if (flag && !GodotObject.IsInstanceValid(towerDefenseCharacter))
		{
			towerDefenseCharacter = TowerDefenseManager.Instance.GetProjectileTargetNearestForPhysicsFrame(pos, gridPos, num3, camp, speed, physicsFrame, fliterGravestone: false, overrides.deprioritizeDisabledTargets);
		}
		Vector2 vel2 = vel;
		double ySpeed = 0.0;
		double catapultTime = 0.0;
		Vector2 vector = Vector2.Zero;
		Vector2 vector2 = pos;
		double num5 = height;
		double num6 = Mathf.Max(height, 0.0);
		double z = height;
		float num7 = ((overrides.catapultSkyDropVisualRadius > 0f) ? overrides.catapultSkyDropVisualRadius : 60f);
		double num8 = 0.0;
		double num9 = num6;
		if (flag2)
		{
			num5 = height + 20.0;
			num6 = Mathf.Max(num5, 0.0);
			if (overrides.catapultStartPositionOverride.HasValue)
			{
				vector2 = overrides.catapultStartPositionOverride.Value + new Vector2(0f, (float)num6);
			}
			else if (GodotObject.IsInstanceValid(fireCharacter))
			{
				Vector2 globalPositionForPhysicsFrame = fireCharacter.GetGlobalPositionForPhysicsFrame(physicsFrame);
				vector2 = globalPositionForPhysicsFrame + new Vector2(pos.X - globalPositionForPhysicsFrame.X, 20f);
			}
			Vector2 vector3 = new Vector2((float)TowerDefenseManager.Instance.GetMapGroundRight(), vector2.Y);
			vector = (overrides.catapultTargetPositionOverride.HasValue ? overrides.catapultTargetPositionOverride.Value : ((!GodotObject.IsInstanceValid(towerDefenseCharacter)) ? (vector3 + new Vector2(60f, 0f)) : (towerDefenseCharacter.GetGlobalPositionForPhysicsFrame(physicsFrame) + new Vector2(-10f * towerDefenseCharacter.Scale.X * towerDefenseCharacter.spriteGroup.Scale.X, 0f))));
			double num10 = 2000.0;
			double num11 = config.catapultHeight;
			if (overrides.catapultSkyDrop)
			{
				float y = GetVisibleWorldRect().Position.Y;
				float num12 = overrides.catapultStartPositionOverride?.Y ?? ((float)((double)vector2.Y - num6));
				num11 = Math.Max(num11, num12 - y + num7 + 4f);
			}
			double num13 = Mathf.Sqrt(2.0 * num11 / num10);
			double num14 = Mathf.Sqrt(2.0 * (num6 + num11) / num10);
			double num15 = num13 + num14;
			if (overrides.catapultSkyDrop)
			{
				num8 = Math.Max(0.01, num13);
				num9 = num6 + num11;
				ySpeed = (0.0 - (num9 - num6)) / num8;
				vel2 = Vector2.Zero;
			}
			else
			{
				ySpeed = 0.0 - Mathf.Sqrt(2.0 * num10 * num11);
				vel2 = (vector - vector2) / (float)num15;
			}
			z = num6;
			catapultTime = num15;
		}
		if (overrides.ySpeedOverride.HasValue)
		{
			ySpeed = overrides.ySpeedOverride.Value;
		}
		if (overrides.zOverride.HasValue)
		{
			z = overrides.zOverride.Value;
		}
		int damageFlags = config.damageFlags;
		if (overrides.damageFlagsOverride.HasValue)
		{
			int num16 = config.damageFlags & 4;
			damageFlags = overrides.damageFlagsOverride.Value | num16;
		}
		BulletData bullet = new BulletData
		{
			active = true,
			over = false,
			renderMode = renderMode,
			pos = vector2,
			vel = vel2,
			speed = (float)speed,
			gridY = gridY,
			gridPos = gridPos,
			lockGridY = lockGridY,
			camp = camp,
			collisionFlags = num3,
			projectileHeight = projectileHeight,
			checkHeight = checkHeight,
			damage = (overrides.baseDamageOverride ?? config.baseDamage),
			damageFlags = damageFlags,
			config = config,
			behaviorProgramId = programId,
			fireCharacter = fireCharacter,
			fireLength = (int)fireLength,
			savePos = vector2,
			checkDistance = checkDistance,
			rect = rect,
			fireDirX = fireDirX,
			rotateScale = (float)config.rotateScale,
			rotateFollowVelocity = config.rotateFollowVelocity,
			rotation = (overrides.initialRotationOverride ?? (flag ? vel.Angle() : 0f)),
			spriteRotation = (overrides.spriteRotationOverride ?? orCreateRenderTemplate.AnimationRotation),
			itemLayer = 9,
			flipX = (overrides.flipXOverride == true),
			hitBoxScale = (overrides.hitBoxScaleOverride ?? Vector2.One),
			animDefId = animationDefinitionId,
			animOffset = animationOffset,
			animElapsedTimer = animElapsedTimer,
			animFrameMax = num,
			animFrameRate = animFrameRate,
			extId = -1,
			trackOpen = flag,
			randFreshIndex = (int)NextSpawnRandom(),
			z = z,
			ySpeed = ySpeed,
			groundHeight = groundHeight,
			height = num5,
			useGravity = useGravity,
			gravityScale = gravityScale,
			gravity = (overrides.gravityOverride ?? 245.0),
			useFall = useFall,
			cell = null,
			catapultOpen = flag2,
			catapultTimer = 0.0,
			catapultTime = catapultTime,
			catapultTargetPos = vector,
			catapultSkyDrop = overrides.catapultSkyDrop,
			catapultSkyDropPhase = (overrides.catapultSkyDrop ? CatapultSkyDropPhase.Ascending : CatapultSkyDropPhase.None),
			catapultSkyDropLaunchPos = vector2,
			catapultSkyDropLaunchZ = num6,
			catapultSkyDropAscentEndZ = num9,
			catapultSkyDropAscentElapsed = 0.0,
			catapultSkyDropAscentDuration = num8,
			catapultSkyDropAscentHorizontalOffset = overrides.catapultSkyDropAscentHorizontalOffset,
			catapultSkyDropWaitRemaining = 0.0,
			catapultSkyDropOffscreenWaitSeconds = Math.Max(0.0, overrides.catapultSkyDropOffscreenWaitSeconds),
			catapultSkyDropVisualRadius = (overrides.catapultSkyDrop ? num7 : 0f),
			isGround = false,
			blocked = false,
			target = towerDefenseCharacter,
			magneticTarget = null,
			trackSearchInterval = num4,
			trackNoTargetInterval = trackNoTargetInterval,
			deprioritizeDisabledTargets = overrides.deprioritizeDisabledTargets,
			penetrateNum = config.penetrateNum,
			lastHitInstanceId = 0uL,
			penetratingTargetIds = null,
			hitOver = false,
			checkAll = checkAll,
			suppressGameplay = overrides.suppressGameplay,
			portalReleased = overrides.portalReleasedOverride,
			fireMethodFlags = num2,
			collisionEnabled = true,
			landOverSubscribed = overrides.landOverSubscribed,
			spawnSourceInstanceId = overrides.spawnSourceInstanceId,
			lifecycleSubscribed = overrides.lifecycleSubscribed,
			lifecycleTerminalEmitted = false,
			lifecycleOwnerSequence = overrides.lifecycleOwnerSequence,
			lifecycleSlot = (overrides.lifecycleSubscribed ? overrides.lifecycleSlot : (-1)),
			yOffset = 0f,
			yOffsetTarget = yOffsetTarget,
			yOffsetTimer = 0f,
			yOffsetDuration = yOffsetDuration,
			spawnTweenStartPos = vector2,
			spawnTweenOffset = (overrides.spawnTweenOffset ?? Vector2.Zero),
			spawnTweenDuration = overrides.spawnTweenDuration,
			spawnTweenTimer = 0f,
			spawnTweenEase = overrides.spawnTweenEase,
			spawnTweenTrans = overrides.spawnTweenTrans
		};
		SetSpriteRotation(ref bullet, bullet.spriteRotation);
		int num17 = Spawn(in bullet);
		if (num17 < 0)
		{
			return RejectSpawn(config, "E_CAPACITY_EXHAUSTED", $"capacity {65536} is full");
		}
		SetBulletEventPayload(num17, overrides.metaData, overrides.eventProjectiles);
		return num17;
	}

	private static AdobeAnimateData DetectAnimatedData(PackedScene scene)
	{
		if (scene == null)
		{
			return null;
		}
		if (_sceneAnimeDataCache.TryGetValue(scene, out var value))
		{
			return value;
		}
		AdobeAnimateData adobeAnimateData = null;
		Vector2 value2 = Vector2.Zero;
		float value3 = 0f;
		string value4 = "";
		double value5 = 1.0;
		bool value6 = false;
		Node node = null;
		try
		{
			node = scene.Instantiate(PackedScene.GenEditState.Disabled);
			if (GodotObject.IsInstanceValid(node))
			{
				AdobeAnimateSprite adobeAnimateSprite = null;
				if (node is AdobeAnimateSprite adobeAnimateSprite2)
				{
					adobeAnimateSprite = adobeAnimateSprite2;
				}
				else if (node is Node2D node2D)
				{
					foreach (Node child in node2D.GetChildren())
					{
						if (child is AdobeAnimateSprite adobeAnimateSprite3)
						{
							adobeAnimateSprite = adobeAnimateSprite3;
							break;
						}
					}
				}
				if (adobeAnimateSprite != null)
				{
					adobeAnimateData = adobeAnimateSprite.flashAnimeData;
					value2 = adobeAnimateSprite.offset;
					value3 = adobeAnimateSprite.Rotation;
					value4 = adobeAnimateSprite.clip;
					value5 = adobeAnimateSprite.timeScale;
					value6 = CanUseUnifiedAnimatedMeshRoot(node, adobeAnimateSprite);
				}
			}
		}
		catch
		{
		}
		finally
		{
			if (GodotObject.IsInstanceValid(node))
			{
				node.Free();
			}
		}
		_sceneAnimeDataCache[scene] = adobeAnimateData;
		_sceneAnimOffsetCache[scene] = value2;
		_sceneAnimRotationCache[scene] = value3;
		_sceneAnimClipCache[scene] = value4;
		_sceneAnimTimeScaleCache[scene] = value5;
		_sceneAnimatedMeshCompatibleCache[scene] = value6;
		return adobeAnimateData;
	}

	private static Vector2 GetSceneAnimOffset(PackedScene scene)
	{
		if (scene == null)
		{
			return Vector2.Zero;
		}
		_sceneAnimOffsetCache.TryGetValue(scene, out var value);
		return value;
	}

	private static string GetSceneAnimClip(PackedScene scene)
	{
		if (scene == null)
		{
			return "";
		}
		_sceneAnimClipCache.TryGetValue(scene, out var value);
		return value ?? "";
	}

	private static float GetSceneAnimRotation(PackedScene scene)
	{
		if (scene == null)
		{
			return 0f;
		}
		_sceneAnimRotationCache.TryGetValue(scene, out var value);
		return value;
	}

	private static double GetSceneAnimTimeScale(PackedScene scene)
	{
		if (scene == null)
		{
			return 1.0;
		}
		if (!_sceneAnimTimeScaleCache.TryGetValue(scene, out var value))
		{
			return 1.0;
		}
		return value;
	}

	private static bool CanUseSceneAnimatedMesh(PackedScene scene)
	{
		if (scene == null)
		{
			return false;
		}
		_sceneAnimatedMeshCompatibleCache.TryGetValue(scene, out var value);
		return value;
	}

	private static bool CanUseUnifiedAnimatedMeshRoot(Node sceneRoot, AdobeAnimateSprite sprite)
	{
		if (sceneRoot != sprite || sprite.GetChildCount() > 0 || !sprite.Visible || sprite.Position != Vector2.Zero || !float.IsFinite(sprite.Rotation) || sprite.Scale != Vector2.One || sprite.playBack || sprite.blend || !double.IsFinite(sprite.timeScale) || sprite.GetVerticalClipState().Enabled || !IsWhite(sprite.Modulate) || !IsWhite(sprite.SelfModulate) || HasHiddenAnimationLayer(sprite.GetLayerVisibleForInternalRead()) || HasEnabledMediaReplace(sprite.GetMediaReplaceUseForInternalRead()))
		{
			return false;
		}
		return true;
	}

	private static bool HasHiddenAnimationLayer(Array<bool> layerVisible)
	{
		if (layerVisible == null)
		{
			return false;
		}
		for (int i = 0; i < layerVisible.Count; i++)
		{
			if (!layerVisible[i])
			{
				return true;
			}
		}
		return false;
	}

	private static bool HasEnabledMediaReplace(Array<bool> mediaReplaceUse)
	{
		if (mediaReplaceUse == null)
		{
			return false;
		}
		for (int i = 0; i < mediaReplaceUse.Count; i++)
		{
			if (mediaReplaceUse[i])
			{
				return true;
			}
		}
		return false;
	}

	private static bool IsWhite(Color color)
	{
		if (Math.Abs(color.R - 1f) <= 0.0001f && Math.Abs(color.G - 1f) <= 0.0001f && Math.Abs(color.B - 1f) <= 0.0001f)
		{
			return Math.Abs(color.A - 1f) <= 0.0001f;
		}
		return false;
	}

	public static TowerDefenseProjectileConfig BuildTemplateConfig(StringName projectileName, StringName skinName)
	{
		(StringName, StringName) key = (projectileName, skinName);
		if (_templateConfigCache.TryGetValue(key, out var value))
		{
			return value;
		}
		TowerDefenseProjectileConfig towerDefenseProjectileConfig = new TowerDefenseProjectileCreateData(projectileName)
		{
			skinName = skinName
		}.BuildConfig();
		if (towerDefenseProjectileConfig != null)
		{
			_templateConfigCache[key] = towerDefenseProjectileConfig;
		}
		return towerDefenseProjectileConfig;
	}

	public static void ClearRuntimeCaches()
	{
		if (GodotObject.IsInstanceValid(Instance))
		{
			Instance._renderTemplateCache.Clear();
			Instance._preparedProjectileChanges.Clear();
			Instance.ClearBehaviorProgramLookup();
			Instance._lastPreparedProjectileChangeConfig = null;
			Instance._lastPreparedProjectileChange = default(PreparedProjectileChange);
			Instance._warmedRenderTemplateVersions.Clear();
			Instance._reportedSpawnErrors.Clear();
			Instance.ClearCollisionCandidateCacheStorage();
		}
		_sceneAnimeDataCache.Clear();
		_sceneAnimOffsetCache.Clear();
		_sceneAnimRotationCache.Clear();
		_sceneAnimClipCache.Clear();
		_sceneAnimTimeScaleCache.Clear();
		_sceneAnimatedMeshCompatibleCache.Clear();
		_templateConfigCache.Clear();
		_renderTemplateWarmupQueue.Clear();
		_queuedRenderTemplateWarmups.Clear();
	}

	private void InitializeWorkerPool()
	{
		_workerPool?.Dispose();
		_workerPool = new BulletFieldWorkerPool();
		_workerPool.WarmUp();
	}

	private void DisposeWorkerPool()
	{
		_workerPool?.Dispose();
		_workerPool = null;
	}

	private void RunParallelRange(int itemCount, Action<int, int> rangeAction)
	{
		if (_workerPool == null)
		{
			rangeAction(0, itemCount);
		}
		else
		{
			_workerPool.Run(itemCount, rangeAction);
		}
	}

	private void RunParallelItems(int itemCount, Action<int> itemAction)
	{
		if (_workerPool == null)
		{
			for (int i = 0; i < itemCount; i++)
			{
				itemAction(i);
			}
		}
		else
		{
			_workerPool.RunItems(itemCount, itemAction);
		}
	}

	public int GetBackgroundWorkerCountForTest()
	{
		return _workerPool?.BackgroundWorkerCount ?? 0;
	}

	public void RegisterZone(IProjectileZone zone)
	{
		if (zone != null && !_zoneEntries.ContainsKey(zone))
		{
			int gridY = zone.GridY;
			int rowSpan = zone.RowSpan;
			ProjectileZoneFrameEntry projectileZoneFrameEntry = new ProjectileZoneFrameEntry(zone, gridY, rowSpan);
			_allZones.Add(projectileZoneFrameEntry);
			_zoneEntries.Add(zone, projectileZoneFrameEntry);
			for (int i = gridY - rowSpan; i <= gridY + rowSpan; i++)
			{
				AddZoneToRow(projectileZoneFrameEntry, i);
			}
		}
	}

	public void RegisterExistingZones(Node root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return;
		}
		if (root is IProjectileZone zone)
		{
			RegisterZone(zone);
		}
		if (root is TowerDefenseCharacter towerDefenseCharacter && GodotObject.IsInstanceValid(towerDefenseCharacter.componentManager))
		{
			foreach (CharacterComponentRuntime resourceComponent in towerDefenseCharacter.componentManager.ResourceComponents)
			{
				if (resourceComponent is IProjectileZoneBinding projectileZoneBinding && resourceComponent.Alive && resourceComponent.Lifecycle == ComponentRuntimeLifecycle.Active)
				{
					projectileZoneBinding.BindBulletField(this);
				}
			}
		}
		foreach (Node child in root.GetChildren())
		{
			if (child != null)
			{
				Node root2 = child;
				RegisterExistingZones(root2);
			}
		}
	}

	public void UnregisterZone(IProjectileZone zone)
	{
		if (zone == null || !_zoneEntries.Remove(zone, out var value))
		{
			return;
		}
		int num = _allZones.IndexOf(value);
		if (num >= 0)
		{
			int num2 = _allZones.Count - 1;
			if (num != num2)
			{
				_allZones[num] = _allZones[num2];
			}
			_allZones.RemoveAt(num2);
			for (int i = value.GridY - value.RowSpan; i <= value.GridY + value.RowSpan; i++)
			{
				RemoveZoneFromRow(value, i);
			}
		}
	}

	public void UpdateZoneRects()
	{
		if (_allZones.Count == 0)
		{
			return;
		}
		for (int i = 0; i < _allZones.Count; i++)
		{
			ProjectileZoneFrameEntry projectileZoneFrameEntry = _allZones[i];
			IProjectileZone zone = projectileZoneFrameEntry.Zone;
			projectileZoneFrameEntry.RefreshWorldRect();
			int gridY = zone.GridY;
			int rowSpan = zone.RowSpan;
			if (projectileZoneFrameEntry.GridY != gridY || projectileZoneFrameEntry.RowSpan != rowSpan)
			{
				for (int j = projectileZoneFrameEntry.GridY - projectileZoneFrameEntry.RowSpan; j <= projectileZoneFrameEntry.GridY + projectileZoneFrameEntry.RowSpan; j++)
				{
					RemoveZoneFromRow(projectileZoneFrameEntry, j);
				}
				for (int k = gridY - rowSpan; k <= gridY + rowSpan; k++)
				{
					AddZoneToRow(projectileZoneFrameEntry, k);
				}
				projectileZoneFrameEntry.GridY = gridY;
				projectileZoneFrameEntry.RowSpan = rowSpan;
			}
		}
	}

	private void ProcessZonesForBullet(int index)
	{
		if (_allZones.Count == 0)
		{
			return;
		}
		ref BulletData reference = ref _data[index];
		if (reference.over || reference.hitOver)
		{
			return;
		}
		TowerDefenseProjectileConfig config = reference.config;
		Vector2 vector = ((!reference.catapultOpen && !reference.useGravity && !reference.useFall) ? new Vector2(reference.pos.X, (float)((double)reference.pos.Y + reference.height)) : new Vector2(reference.pos.X, (float)((double)reference.pos.Y - reference.z)));
		Vector2 collisionHalfSize = GetCollisionHalfSize(ref reference);
		float num = vector.X - collisionHalfSize.X;
		float num2 = vector.X + collisionHalfSize.X;
		float num3 = vector.Y - collisionHalfSize.Y;
		float num4 = vector.Y + collisionHalfSize.Y;
		List<ProjectileZoneFrameEntry> value;
		if (reference.checkAll)
		{
			value = _allZones;
		}
		else if (!_zonesByRow.TryGetValue(reference.gridY, out value))
		{
			return;
		}
		for (int i = 0; i < value.Count; i++)
		{
			ProjectileZoneFrameEntry projectileZoneFrameEntry = value[i];
			if (!(num2 < projectileZoneFrameEntry.MinX) && !(num > projectileZoneFrameEntry.MaxX) && !(num4 < projectileZoneFrameEntry.MinY) && !(num3 > projectileZoneFrameEntry.MaxY))
			{
				projectileZoneFrameEntry.Zone.OnBulletIntersect(ref reference, index);
				if (!reference.active || reference.config != config)
				{
					break;
				}
			}
		}
	}

	private bool ProcessPriorityCatapultBlockZones(int index, Vector2 collisionStartPosition)
	{
		if (_allZones.Count == 0)
		{
			return false;
		}
		ref BulletData reference = ref _data[index];
		if (!reference.active || reference.over || reference.hitOver || reference.blocked || !reference.catapultOpen || !reference.collisionEnabled)
		{
			return false;
		}
		List<ProjectileZoneFrameEntry> value;
		if (reference.checkAll)
		{
			value = _allZones;
		}
		else if (!_zonesByRow.TryGetValue(reference.gridY, out value))
		{
			return false;
		}
		Vector2 collisionPos = GetCollisionPos(ref reference);
		Vector2 collisionHalfSize = GetCollisionHalfSize(ref reference);
		for (int i = 0; i < value.Count; i++)
		{
			ProjectileZoneFrameEntry projectileZoneFrameEntry = value[i];
			if (projectileZoneFrameEntry.Zone is ICatapultProjectileBlockZone catapultProjectileBlockZone && SweptCollisionIntersectsRect(collisionStartPosition, collisionPos, collisionHalfSize, projectileZoneFrameEntry.WorldRect) && catapultProjectileBlockZone.TryBlockCatapultBullet(ref reference, index, collisionStartPosition, collisionPos, collisionHalfSize))
			{
				return true;
			}
		}
		return false;
	}

	public void ProcessZonesForProjectile(TowerDefenseProjectile projectile)
	{
		if (_allZones.Count == 0 || !GodotObject.IsInstanceValid(projectile) || projectile.over || projectile.hitOver || !GodotObject.IsInstanceValid(projectile.hitBox) || projectile.hitBox.ProcessMode == ProcessModeEnum.Disabled)
		{
			return;
		}
		Rect2 a = AabbShapeUtil.ComputeAreaWorldRect(projectile.hitBox);
		List<ProjectileZoneFrameEntry> value;
		if (projectile.checkAll)
		{
			value = _allZones;
		}
		else if (!_zonesByRow.TryGetValue(projectile.gridPos.Y, out value))
		{
			return;
		}
		TowerDefenseProjectileConfig config = projectile.config;
		for (int i = 0; i < value.Count; i++)
		{
			ProjectileZoneFrameEntry projectileZoneFrameEntry = value[i];
			if (AabbShapeUtil.Intersects(a, projectileZoneFrameEntry.WorldRect))
			{
				projectileZoneFrameEntry.Zone.OnProjectileIntersect(projectile);
				if (!GodotObject.IsInstanceValid(projectile) || projectile.over || projectile.hitOver || projectile.config != config)
				{
					break;
				}
			}
		}
	}

	private void AddZoneToRow(ProjectileZoneFrameEntry zone, int gridY)
	{
		if (!_zonesByRow.TryGetValue(gridY, out var value))
		{
			value = new List<ProjectileZoneFrameEntry>();
			_zonesByRow[gridY] = value;
		}
		value.Add(zone);
	}

	private void RemoveZoneFromRow(ProjectileZoneFrameEntry zone, int gridY)
	{
		if (!_zonesByRow.TryGetValue(gridY, out var value))
		{
			return;
		}
		int num = value.IndexOf(zone);
		if (num >= 0)
		{
			int num2 = value.Count - 1;
			if (num != num2)
			{
				value[num] = value[num2];
			}
			value.RemoveAt(num2);
			if (value.Count == 0)
			{
				_zonesByRow.Remove(gridY);
			}
		}
	}

	public ref BulletData GetBulletDataRef(int index)
	{
		return ref _data[index];
	}

	public bool IsBulletIntersectingRect(int index, Rect2 worldRect)
	{
		if (index < 0 || index >= 65536)
		{
			return false;
		}
		ref BulletData reference = ref _data[index];
		if (reference.active && !reference.over)
		{
			return AabbShapeUtil.Intersects(GetCollisionRect(ref reference), worldRect);
		}
		return false;
	}

	public int ChangeBulletData(int index, TowerDefenseProjectileConfig newConfig, TowerDefenseCharacter changeCharacter, double? damageOverride = null)
	{
		ref BulletData reference = ref _data[index];
		if (!reference.active || reference.config == null || newConfig == null)
		{
			return -1;
		}
		switch (TryChangeBulletDataInPlace(index, newConfig))
		{
		case BulletChangeInPlaceResult.Changed:
			if (damageOverride.HasValue)
			{
				_data[index].damage = damageOverride.Value;
			}
			return index;
		case BulletChangeInPlaceResult.Rejected:
			return -1;
		default:
		{
			Vector2 pos = reference.pos;
			Vector2 vel = reference.vel;
			float speed = reference.speed;
			int gridY = reference.gridY;
			Vector2I gridPos = reference.gridPos;
			bool lockGridY = reference.lockGridY;
			TowerDefenseEnum.CHARACTER_CAMP camp = reference.camp;
			int collisionFlags = reference.collisionFlags;
			TowerDefenseEnum.CHARACTER_HEIGHT projectileHeight = reference.projectileHeight;
			bool checkHeight = reference.checkHeight;
			float fireLength = reference.fireLength;
			Vector2 savePos = reference.savePos;
			_ = reference;
			Rect2 rect = reference.rect;
			double height = reference.height;
			double groundHeight = reference.groundHeight;
			double z = reference.z;
			double ySpeed = reference.ySpeed;
			TowerDefenseCellInstance cell = reference.cell;
			double gravity = reference.gravity;
			float gravityScale = reference.gravityScale;
			bool useFall = reference.useFall;
			bool useGravity = reference.useGravity;
			double catapultTimer = reference.catapultTimer;
			double catapultTime = reference.catapultTime;
			Vector2 catapultTargetPos = reference.catapultTargetPos;
			bool catapultSkyDrop = reference.catapultSkyDrop;
			CatapultSkyDropPhase catapultSkyDropPhase = reference.catapultSkyDropPhase;
			Vector2 catapultSkyDropLaunchPos = reference.catapultSkyDropLaunchPos;
			double catapultSkyDropLaunchZ = reference.catapultSkyDropLaunchZ;
			double catapultSkyDropAscentEndZ = reference.catapultSkyDropAscentEndZ;
			double catapultSkyDropAscentElapsed = reference.catapultSkyDropAscentElapsed;
			double catapultSkyDropAscentDuration = reference.catapultSkyDropAscentDuration;
			float catapultSkyDropAscentHorizontalOffset = reference.catapultSkyDropAscentHorizontalOffset;
			double catapultSkyDropWaitRemaining = reference.catapultSkyDropWaitRemaining;
			double catapultSkyDropOffscreenWaitSeconds = reference.catapultSkyDropOffscreenWaitSeconds;
			float catapultSkyDropVisualRadius = reference.catapultSkyDropVisualRadius;
			bool isGround = reference.isGround;
			bool blocked = reference.blocked;
			TowerDefenseCharacter fireCharacter = (GodotObject.IsInstanceValid(reference.fireCharacter) ? reference.fireCharacter : null);
			TowerDefenseCharacter target = (GodotObject.IsInstanceValid(reference.target) ? reference.target : null);
			TowerDefenseCharacter magneticTarget = (GodotObject.IsInstanceValid(reference.magneticTarget) ? reference.magneticTarget : null);
			bool checkAll = reference.checkAll;
			bool landOverSubscribed = reference.landOverSubscribed;
			ulong spawnSourceInstanceId = reference.spawnSourceInstanceId;
			int extId = reference.extId;
			int randFreshIndex = reference.randFreshIndex;
			int itemLayer = reference.itemLayer;
			float fireDirX = reference.fireDirX;
			bool flipX = reference.flipX;
			Vector2 hitBoxScale = reference.hitBoxScale;
			bool portalReleased = reference.portalReleased;
			bool trackOpen = reference.trackOpen;
			bool catapultOpen = reference.catapultOpen;
			bool useFall2 = reference.useFall;
			bool useGravity2 = reference.useGravity;
			bool rotateFollowVelocity = reference.rotateFollowVelocity;
			Vector2 vel2 = reference.vel;
			double ySpeed2 = reference.ySpeed;
			float rotation = reference.rotation;
			float spriteRotation = reference.spriteRotation;
			float yOffset = reference.yOffset;
			float yOffsetTarget = reference.yOffsetTarget;
			float yOffsetTimer = reference.yOffsetTimer;
			float yOffsetDuration = reference.yOffsetDuration;
			Vector2 spawnTweenStartPos = reference.spawnTweenStartPos;
			Vector2 spawnTweenOffset = reference.spawnTweenOffset;
			float spawnTweenDuration = reference.spawnTweenDuration;
			float spawnTweenTimer = reference.spawnTweenTimer;
			Tween.EaseType spawnTweenEase = reference.spawnTweenEase;
			Tween.TransitionType spawnTweenTrans = reference.spawnTweenTrans;
			Despawn(index);
			int num = TrySpawnFromConfig(newConfig, pos, vel, speed, fireCharacter, camp, gridPos, gridY, rect, target, height, groundHeight, collisionFlags, fireLength, checkHeight, checkAll, useFall, useGravity, gravityScale, 0f, 0f, new BulletFieldSpawnOverrides
			{
				flipXOverride = flipX,
				spriteRotationOverride = spriteRotation,
				portalReleasedOverride = portalReleased
			});
			if (num < 0)
			{
				return num;
			}
			ref BulletData reference2 = ref _data[num];
			if (damageOverride.HasValue)
			{
				reference2.damage = damageOverride.Value;
			}
			reference2.pos = pos;
			reference2.height = height;
			reference2.groundHeight = groundHeight;
			reference2.savePos = savePos;
			reference2.z = z;
			reference2.ySpeed = ySpeed;
			reference2.cell = cell;
			reference2.gravity = gravity;
			reference2.catapultTimer = catapultTimer;
			reference2.catapultTime = catapultTime;
			reference2.catapultTargetPos = catapultTargetPos;
			reference2.catapultSkyDrop = catapultSkyDrop;
			reference2.catapultSkyDropPhase = catapultSkyDropPhase;
			reference2.catapultSkyDropLaunchPos = catapultSkyDropLaunchPos;
			reference2.catapultSkyDropLaunchZ = catapultSkyDropLaunchZ;
			reference2.catapultSkyDropAscentEndZ = catapultSkyDropAscentEndZ;
			reference2.catapultSkyDropAscentElapsed = catapultSkyDropAscentElapsed;
			reference2.catapultSkyDropAscentDuration = catapultSkyDropAscentDuration;
			reference2.catapultSkyDropAscentHorizontalOffset = catapultSkyDropAscentHorizontalOffset;
			reference2.catapultSkyDropWaitRemaining = catapultSkyDropWaitRemaining;
			reference2.catapultSkyDropOffscreenWaitSeconds = catapultSkyDropOffscreenWaitSeconds;
			reference2.catapultSkyDropVisualRadius = catapultSkyDropVisualRadius;
			reference2.isGround = isGround;
			reference2.blocked = blocked;
			reference2.magneticTarget = magneticTarget;
			reference2.landOverSubscribed = landOverSubscribed;
			reference2.spawnSourceInstanceId = spawnSourceInstanceId;
			reference2.extId = extId;
			reference2.randFreshIndex = randFreshIndex;
			reference2.itemLayer = itemLayer;
			reference2.fireDirX = fireDirX;
			reference2.flipX = flipX;
			reference2.hitBoxScale = hitBoxScale;
			SetSpriteRotation(ref reference2, spriteRotation);
			reference2.lockGridY = lockGridY;
			reference2.projectileHeight = projectileHeight;
			reference2.yOffset = yOffset;
			reference2.yOffsetTarget = yOffsetTarget;
			reference2.yOffsetTimer = yOffsetTimer;
			reference2.yOffsetDuration = yOffsetDuration;
			reference2.spawnTweenStartPos = spawnTweenStartPos;
			reference2.spawnTweenOffset = spawnTweenOffset;
			reference2.spawnTweenDuration = spawnTweenDuration;
			reference2.spawnTweenTimer = spawnTweenTimer;
			reference2.spawnTweenEase = spawnTweenEase;
			reference2.spawnTweenTrans = spawnTweenTrans;
			float rotation2;
			if (trackOpen)
			{
				rotation2 = rotation;
			}
			else if (useFall2)
			{
				Vector2 vector = new Vector2(vel2.X, (float)ySpeed2);
				rotation2 = ((vector.LengthSquared() > 0.01f) ? vector.Angle() : 0f);
			}
			else if (!((catapultOpen || useGravity2) && rotateFollowVelocity))
			{
				rotation2 = ((!rotateFollowVelocity) ? rotation : vel2.Angle());
			}
			else
			{
				Vector2 vector2 = new Vector2(vel2.X, (float)ySpeed2);
				rotation2 = ((vector2.LengthSquared() > 0.01f) ? vector2.Angle() : 0f);
			}
			reference2.rotation = rotation2;
			ApplyVelocityFollowingVisuals(ref reference2, GetOrCreatePreparedProjectileChange(newConfig).AnimationRotation);
			return num;
		}
		}
	}

	internal int ChangeBulletDataPrepared(int index, TowerDefenseProjectileConfig newConfig, TowerDefenseCharacter changeCharacter, in PreparedProjectileChange prepared, double damage)
	{
		ref BulletData reference = ref _data[index];
		if (!reference.active || reference.config == null || newConfig == null)
		{
			return -1;
		}
		if (!IsPreparedProjectileChangeCurrent(in prepared))
		{
			return ChangeBulletData(index, newConfig, changeCharacter, damage);
		}
		return ApplyPreparedProjectileChange(index, newConfig, in prepared, damage) switch
		{
			BulletChangeInPlaceResult.Changed => index, 
			BulletChangeInPlaceResult.Rejected => -1, 
			_ => ChangeBulletData(index, newConfig, changeCharacter, damage), 
		};
	}

	public void BlockedBounceData(int index)
	{
		ref BulletData reference = ref _data[index];
		if (reference.active && !reference.hitOver)
		{
			reference.blocked = true;
			reference.target = null;
			reference.hitOver = true;
			reference.ySpeed = -500.0;
			reference.vel *= 0.5f;
			reference.collisionEnabled = false;
			TryEmitLifecycleTerminal(index, BulletLifecycleTerminalReason.BlockedBounce);
		}
	}

	public void SetTrackData(int index, TowerDefenseCharacter trackTarget)
	{
		ref BulletData reference = ref _data[index];
		if (reference.active)
		{
			RemoveFromRowBucket(index, reference.gridY);
			reference.gridY = 10;
			AddToRowBucket(index, reference.gridY);
			reference.gridPos = new Vector2I(reference.gridPos.X, 10);
			reference.trackOpen = true;
			reference.checkAll = true;
			reference.penetrateNum = 0;
			reference.target = trackTarget;
			reference.magneticTarget = trackTarget;
			if (reference.config != null)
			{
				reference.trackSearchInterval = reference.config.trackSearchInterval;
				reference.trackNoTargetInterval = reference.config.trackSearchInterval * 4;
			}
		}
	}

	public void TeleportBullet(int index, Vector2 newPos, int newGridY)
	{
		ref BulletData reference = ref _data[index];
		if (reference.active)
		{
			if (reference.gridY != newGridY)
			{
				RemoveFromRowBucket(index, reference.gridY);
				reference.gridY = newGridY;
				AddToRowBucket(index, reference.gridY);
			}
			reference.pos = newPos;
			reference.gridPos = new Vector2I(reference.gridPos.X, newGridY);
			reference.savePos = newPos;
		}
	}

	private bool TryGetOrCompileBehaviorProgram(TowerDefenseProjectileConfig config, out int programId, out string errorReason)
	{
		programId = 0;
		errorReason = string.Empty;
		if (!GodotObject.IsInstanceValid(config))
		{
			errorReason = "projectile config is null";
			return false;
		}
		if (_behaviorProgramByConfig.TryGetValue(config, out programId))
		{
			return true;
		}
		List<ProjectileBehaviorDefinition> list = new List<ProjectileBehaviorDefinition>();
		if (config.behaviorIds != null)
		{
			for (int i = 0; i < config.behaviorIds.Count; i++)
			{
				StringName stringName = config.behaviorIds[i];
				Resource behavior = TowerDefenseBehaviorRegistry.GetBehavior(stringName);
				if (!(behavior is ProjectileBehaviorDefinition item))
				{
					errorReason = $"registered behavior '{stringName}' is '{behavior?.GetType().Name ?? "<missing>"}', expected projectile behavior";
					return false;
				}
				list.Add(item);
			}
		}
		if (config.behaviors != null)
		{
			for (int j = 0; j < config.behaviors.Count; j++)
			{
				ProjectileBehaviorDefinition projectileBehaviorDefinition = config.behaviors[j];
				if (GodotObject.IsInstanceValid(projectileBehaviorDefinition))
				{
					list.Add(projectileBehaviorDefinition);
				}
			}
		}
		if (list.Count == 0)
		{
			_behaviorProgramByConfig[config] = 0;
			return true;
		}
		List<ProjectileBehaviorKernel> list2 = new List<ProjectileBehaviorKernel>(list.Count);
		for (int k = 0; k < list.Count; k++)
		{
			ProjectileBehaviorDefinition projectileBehaviorDefinition2 = list[k];
			if (projectileBehaviorDefinition2.InitiallyEnabled)
			{
				ProjectileBehaviorKernel projectileBehaviorKernel;
				try
				{
					projectileBehaviorKernel = projectileBehaviorDefinition2.CreateBulletFieldKernel(65536);
				}
				catch (Exception ex)
				{
					errorReason = "behavior '" + projectileBehaviorDefinition2.GetDiagnosticName() + "' compile failed: " + ex.Message;
					return false;
				}
				if (projectileBehaviorKernel == null)
				{
					errorReason = "behavior '" + projectileBehaviorDefinition2.GetDiagnosticName() + "' has no BulletField kernel";
					return false;
				}
				projectileBehaviorKernel.Bind(this);
				list2.Add(projectileBehaviorKernel);
			}
		}
		if (list2.Count == 0)
		{
			_behaviorProgramByConfig[config] = 0;
			return true;
		}
		programId = _behaviorPrograms.Count;
		_behaviorPrograms.Add(new BulletBehaviorProgram(list2.ToArray(), config.name));
		_behaviorProgramByConfig[config] = programId;
		return true;
	}

	private void InitializeBulletBehavior(int index, ref BulletData bullet)
	{
		DispatchBulletBehavior(index, ref bullet, "E_SPAWN", delegate(ProjectileBehaviorKernel kernel, int bulletIndex, ref BulletData data)
		{
			kernel.OnSpawn(bulletIndex, ref data);
		});
	}

	private void ProcessBulletBehavior(int index, ref BulletData bullet, double delta)
	{
		int behaviorProgramId = bullet.behaviorProgramId;
		if (behaviorProgramId <= 0 || behaviorProgramId >= _behaviorPrograms.Count)
		{
			return;
		}
		BulletBehaviorProgram bulletBehaviorProgram = _behaviorPrograms[behaviorProgramId];
		for (int i = 0; i < bulletBehaviorProgram.Kernels.Length; i++)
		{
			if (!bulletBehaviorProgram.Disabled[i])
			{
				try
				{
					bulletBehaviorProgram.Kernels[i].Process(index, ref bullet, delta);
				}
				catch (Exception exception)
				{
					DisableFaultedKernel(bulletBehaviorProgram, i, "E_PROCESS", exception);
				}
			}
		}
	}

	private void DispatchBulletBehaviorHit(int index, ref BulletData bullet, TowerDefenseCharacter target)
	{
		int behaviorProgramId = bullet.behaviorProgramId;
		if (behaviorProgramId <= 0 || behaviorProgramId >= _behaviorPrograms.Count)
		{
			return;
		}
		BulletBehaviorProgram bulletBehaviorProgram = _behaviorPrograms[behaviorProgramId];
		for (int i = 0; i < bulletBehaviorProgram.Kernels.Length; i++)
		{
			if (!bulletBehaviorProgram.Disabled[i])
			{
				try
				{
					bulletBehaviorProgram.Kernels[i].OnHitTarget(index, ref bullet, target);
				}
				catch (Exception exception)
				{
					DisableFaultedKernel(bulletBehaviorProgram, i, "E_HIT", exception);
				}
			}
		}
	}

	private void DispatchBulletBehaviorLand(int index, ref BulletData bullet)
	{
		DispatchBulletBehavior(index, ref bullet, "E_LAND", delegate(ProjectileBehaviorKernel kernel, int bulletIndex, ref BulletData data)
		{
			kernel.OnLand(bulletIndex, ref data);
		});
	}

	private void ReleaseBulletBehavior(int index, ref BulletData bullet)
	{
		DispatchBulletBehavior(index, ref bullet, "E_DESPAWN", delegate(ProjectileBehaviorKernel kernel, int bulletIndex, ref BulletData data)
		{
			kernel.OnDespawn(bulletIndex, ref data);
		});
	}

	private void DispatchBulletBehavior(int index, ref BulletData bullet, string errorCode, KernelCallback callback)
	{
		int behaviorProgramId = bullet.behaviorProgramId;
		if (behaviorProgramId <= 0 || behaviorProgramId >= _behaviorPrograms.Count)
		{
			return;
		}
		BulletBehaviorProgram bulletBehaviorProgram = _behaviorPrograms[behaviorProgramId];
		for (int i = 0; i < bulletBehaviorProgram.Kernels.Length; i++)
		{
			if (!bulletBehaviorProgram.Disabled[i])
			{
				try
				{
					callback(bulletBehaviorProgram.Kernels[i], index, ref bullet);
				}
				catch (Exception exception)
				{
					DisableFaultedKernel(bulletBehaviorProgram, i, errorCode, exception);
				}
			}
		}
	}

	private static void DisableFaultedKernel(BulletBehaviorProgram program, int kernelIndex, string errorCode, Exception exception)
	{
		program.Disabled[kernelIndex] = true;
		GD.PushError($"[ProjectileBehavior:{errorCode}] projectile='{program.DiagnosticName}' kernel='{program.Kernels[kernelIndex].GetType().Name}' reason='{exception.Message}'");
	}

	private void ClearBehaviorProgramLookup()
	{
		_behaviorProgramByConfig.Clear();
	}
}
