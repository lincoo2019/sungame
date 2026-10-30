using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

public partial class AnimateMultiMeshRenderer : Node2D
{
	internal readonly struct SimpleDrawContext
	{
		internal readonly Definition Definition;

		internal readonly UnifiedBucket Bucket;

		internal bool IsValid
		{
			get
			{
				if (Definition != null && Bucket != null)
				{
					return Bucket.RenderBucket != null;
				}
				return false;
			}
		}

		internal SimpleDrawContext(Definition definition, UnifiedBucket bucket)
		{
			Definition = definition;
			Bucket = bucket;
		}
	}

	public readonly struct FramePublicationResult
	{
		public readonly long Generation;

		public readonly int ExpectedInstanceCount;

		public readonly int AppendedInstanceCount;

		public readonly int RequestedBucketCount;

		public readonly int PublishedInstanceCount;

		public readonly int PublishedBucketCount;

		public bool IsComplete
		{
			get
			{
				if (AppendedInstanceCount == ExpectedInstanceCount && PublishedInstanceCount == ExpectedInstanceCount)
				{
					return PublishedBucketCount == RequestedBucketCount;
				}
				return false;
			}
		}

		internal FramePublicationResult(long generation, int expectedInstanceCount, int appendedInstanceCount, int requestedBucketCount, int publishedInstanceCount, int publishedBucketCount)
		{
			Generation = generation;
			ExpectedInstanceCount = expectedInstanceCount;
			AppendedInstanceCount = appendedInstanceCount;
			RequestedBucketCount = requestedBucketCount;
			PublishedInstanceCount = publishedInstanceCount;
			PublishedBucketCount = publishedBucketCount;
		}
	}

	public sealed class Definition
	{
		public AdobeAnimateData data;

		public AdobeAnimateRuntimeDefinition runtime;

		public string clipName;

		public int clipStart;

		public int frameMax;

		public bool hasLoopTerminalAlias;

		public bool preserveTerminalAlias;

		public double frameRate;

		public Texture2D atlas;

		public bool mediaRectsAreNormalized;

		public int layer;

		internal UnifiedResourceGroup resourceGroup;

		internal UnifiedBucket cachedBucket;

		internal int cachedBucketZIndex = -2147483648;

		internal UnifiedBucket[] exactZBucketLookup;

		internal int simpleAnimationBaseTexel = -1;

		internal int prewarmedMapRowCount;

		internal bool compactCrowdReady;
	}

	private readonly struct DefinitionKey(AdobeAnimateData data, string clipName, int layer, bool preserveTerminalAlias) : IEquatable<DefinitionKey>
	{
		private readonly ulong _dataId = data?.GetInstanceId() ?? 0;

		private readonly string _clipName = clipName ?? "";

		private readonly int _layer = layer;

		private readonly bool _preserveTerminalAlias = preserveTerminalAlias;

		public bool Equals(DefinitionKey other)
		{
			if (_dataId == other._dataId && _clipName == other._clipName && _layer == other._layer)
			{
				return _preserveTerminalAlias == other._preserveTerminalAlias;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is DefinitionKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_dataId, _clipName, _layer, _preserveTerminalAlias);
		}
	}

	internal sealed class UnifiedBucket
	{
		public AdobeAnimateZIndexCrowdBucket RenderBucket;

		public UnifiedResourceGroup Group;

		public int ZIndex;

		public long LastTouchedFrame = -9223372036854775808L;

		public int RootCount;

		public bool Visible;
	}

	internal sealed class UnifiedResourceGroup
	{
		public AdobeAnimateCrowdResourceSignature Signature;

		public readonly AdobeAnimateCrowdStateArena StateArena = new AdobeAnimateCrowdStateArena();

		public readonly Dictionary<int, UnifiedBucket> Buckets = new Dictionary<int, UnifiedBucket>();

		public readonly UnifiedBucket[] ExactZBucketLookup = new UnifiedBucket[8193];

		public ShaderMaterial Material;

		public ArrayMesh Mesh;

		public int MeshMaxSlices;

		public int MeshVersion;

		public long LastTouchedFrame = -9223372036854775808L;

		public bool StateUploaded;

		public int SimpleAnimationBindingVersion = -1;

		public bool SharedBindingsInitialized;

		public TextureLayered SharedBindingsStateTexture;

		public Vector2I SharedBindingsStateTextureSize;
	}

	private static readonly int LayerGroundItemMax = 15;

	private const int ExactZIndexMin = -4096;

	private const int ExactZIndexMax = 4096;

	private const int ExactZIndexCount = 8193;

	private readonly List<Definition> _definitions = new List<Definition>();

	private readonly Dictionary<DefinitionKey, int> _definitionIds = new Dictionary<DefinitionKey, int>();

	private readonly Dictionary<AdobeAnimateCrowdResourceSignature, UnifiedResourceGroup> _resourceGroups = new Dictionary<AdobeAnimateCrowdResourceSignature, UnifiedResourceGroup>();

	private readonly List<UnifiedBucket> _touchedBucketsLastFrame = new List<UnifiedBucket>(16);

	private readonly List<UnifiedBucket> _touchedBucketsThisFrame = new List<UnifiedBucket>(16);

	private readonly List<int> _bucketKeysToRemove = new List<int>(16);

	private readonly List<ArrayMesh> _retiredMeshes = new List<ArrayMesh>(2);

	private int _observedAtlasCacheVersion = -1;

	private long _frameVersion;

	private long _lastBucketMaintenanceFrame;

	private AdobeAnimateCrowdResourceSignature _lastResourceSignature;

	private UnifiedResourceGroup _lastResourceGroup;

	private int _lastBucketZIndex = -2147483648;

	private UnifiedBucket _lastBucket;

	internal const int SimpleAnimationTextureWidth = 2048;

	private Image _simpleAnimationImage;

	private ImageTexture _simpleAnimationTexture;

	private Vector2I _simpleAnimationTextureSize = Vector2I.One;

	private bool _simpleAnimationTextureDirty = true;

	private int _simpleAnimationTextureVersion;

	private double _animationTime;

	private FramePublicationResult _lastPublication;

	private bool _failNextPublicationForTest;

	private static readonly StringName SimpleAnimationTextureParameter = "simpleAnimationTexture";

	private static readonly StringName SimpleAnimationTextureSizeParameter = "simpleAnimationTextureSize";

	private static readonly StringName AnimationClockParameter = "animationClock";

	private const int BucketReclaimIntervalFrames = 120;

	public int RegisterDefinition(AdobeAnimateData data, string clipName, int layer, bool preserveTerminalAlias = false)
	{
		if (data == null)
		{
			return -1;
		}
		SyncAtlasCacheVersion();
		AdobeAnimateRuntimeDefinition orBuildForCompactCrowd = AdobeAnimateDefinitionCache.GetOrBuildForCompactCrowd(data);
		if (!CanUseUnifiedCompactCrowd(orBuildForCompactCrowd))
		{
			return -1;
		}
		if (string.IsNullOrEmpty(clipName))
		{
			clipName = ((orBuildForCompactCrowd.Clips.Length != 0) ? orBuildForCompactCrowd.Clips[0].Name.ToString() : "");
		}
		DefinitionKey key = new DefinitionKey(data, clipName, layer, preserveTerminalAlias);
		if (_definitionIds.TryGetValue(key, out var value))
		{
			return value;
		}
		if (!TryResolveClip(orBuildForCompactCrowd, clipName, preserveTerminalAlias, out var clipStart, out var frameCount, out var hasLoopTerminalAlias))
		{
			return -1;
		}
		Definition item = new Definition
		{
			data = data,
			runtime = orBuildForCompactCrowd,
			clipName = clipName,
			clipStart = clipStart,
			frameMax = frameCount,
			hasLoopTerminalAlias = hasLoopTerminalAlias,
			preserveTerminalAlias = preserveTerminalAlias,
			frameRate = ((orBuildForCompactCrowd.FrameRate > 0.0) ? orBuildForCompactCrowd.FrameRate : data.frameRate),
			atlas = orBuildForCompactCrowd.BaseAtlas,
			mediaRectsAreNormalized = false,
			layer = layer,
			compactCrowdReady = true
		};
		int count = _definitions.Count;
		_definitions.Add(item);
		_definitionIds[key] = count;
		_simpleAnimationTextureDirty = true;
		return count;
	}

	public Definition GetDefinition(int definitionId)
	{
		if ((uint)definitionId >= (uint)_definitions.Count)
		{
			return null;
		}
		return _definitions[definitionId];
	}

	internal void PrewarmDefinitionRows(int definitionId, int mapRowCount)
	{
		SyncAtlasCacheVersion();
		if ((uint)definitionId >= (uint)_definitions.Count)
		{
			return;
		}
		Definition definition = _definitions[definitionId];
		if (!definition.compactCrowdReady)
		{
			return;
		}
		mapRowCount = Math.Max(0, mapRowCount);
		for (int i = definition.prewarmedMapRowCount + 1; i <= mapRowCount; i++)
		{
			int zIndex = (int)Math.Clamp((long)i * (long)LayerGroundItemMax + definition.layer, -4096L, 4096L);
			if (EnsureBucket(definition, zIndex, touch: false) == null)
			{
				break;
			}
			definition.prewarmedMapRowCount = i;
		}
	}

	internal bool TryPrepareSimpleDrawContext(int definitionId, int zGroup, int capacityHint, out SimpleDrawContext context)
	{
		context = default(SimpleDrawContext);
		if ((uint)definitionId >= (uint)_definitions.Count)
		{
			return false;
		}
		Definition definition = _definitions[definitionId];
		AdobeAnimateRuntimeDefinition runtime = definition.runtime;
		if (!definition.compactCrowdReady || runtime == null)
		{
			return false;
		}
		int zIndex = (int)Math.Clamp((long)zGroup * (long)LayerGroundItemMax + definition.layer, -4096L, 4096L);
		UnifiedBucket unifiedBucket = EnsureBucket(definition, zIndex);
		if (unifiedBucket == null)
		{
			return false;
		}
		unifiedBucket.RenderBucket.ReserveCapacity(capacityHint);
		context = new SimpleDrawContext(definition, unifiedBucket);
		return true;
	}

	internal bool DrawPreparedSimpleInstance(in SimpleDrawContext context, Transform2D transform, float framePhase, float frameRate)
	{
		if (!context.IsValid)
		{
			return false;
		}
		Definition definition = context.Definition;
		UnifiedBucket bucket = context.Bucket;
		if (definition.simpleAnimationBaseTexel < 0 || !bucket.RenderBucket.AppendGpuClockSimple(transform, definition.simpleAnimationBaseTexel, framePhase, frameRate))
		{
			return false;
		}
		bucket.RootCount++;
		return true;
	}

	internal bool TouchPreparedSimpleDrawContext(in SimpleDrawContext context)
	{
		if (!context.IsValid)
		{
			return false;
		}
		TouchBucket(context.Bucket);
		return true;
	}

	public void BeginFrame()
	{
		SyncAtlasCacheVersion();
		EnsureSimpleAnimationTexture();
		_frameVersion++;
		_touchedBucketsLastFrame.Clear();
		for (int i = 0; i < _touchedBucketsThisFrame.Count; i++)
		{
			_touchedBucketsLastFrame.Add(_touchedBucketsThisFrame[i]);
		}
		_touchedBucketsThisFrame.Clear();
	}

	public void SetAnimationTime(double animationTime)
	{
		_animationTime = animationTime;
	}

	internal bool TryUpdateRetainedSimpleAnimationFrame(long expectedGeneration, int expectedInstanceCount, int expectedBucketCount)
	{
		if (!_lastPublication.IsComplete || _lastPublication.Generation != expectedGeneration || _lastPublication.PublishedInstanceCount != expectedInstanceCount || _lastPublication.PublishedBucketCount != expectedBucketCount)
		{
			return false;
		}
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (_observedAtlasCacheVersion != cacheVersion)
		{
			SyncAtlasCacheVersion();
			return false;
		}
		int num = 0;
		int num2 = 0;
		foreach (UnifiedResourceGroup value in _resourceGroups.Values)
		{
			bool flag = false;
			foreach (UnifiedBucket value2 in value.Buckets.Values)
			{
				if (value2.Visible)
				{
					if (value2.RenderBucket == null || value2.RenderBucket.GpuInstanceCapacityForTest <= 0 || value2.RenderBucket.InstanceCount <= 0)
					{
						return false;
					}
					flag = true;
					num++;
					num2 += value2.RenderBucket.InstanceCount;
				}
			}
			if (flag)
			{
				ApplySimpleAnimationBindings(value, updateAnimationClock: true);
			}
		}
		if (num == expectedBucketCount)
		{
			return num2 == expectedInstanceCount;
		}
		return false;
	}

	public bool DrawInstance(int definitionId, int zGroup, Transform2D transform, float frameFloat, Color modulate, float framePhase = 0f / 0f)
	{
		if ((uint)definitionId >= (uint)_definitions.Count)
		{
			return false;
		}
		Definition definition = _definitions[definitionId];
		int zIndex = (int)Math.Clamp((long)zGroup * (long)LayerGroundItemMax + definition.layer, -4096L, 4096L);
		return DrawInstanceResolved(definition, zIndex, transform, frameFloat, modulate, framePhase);
	}

	public bool DrawInstanceAtZIndex(int definitionId, int zIndex, Transform2D transform, float frameFloat, Color modulate, float framePhase = 0f / 0f)
	{
		if ((uint)definitionId >= (uint)_definitions.Count)
		{
			return false;
		}
		Definition definition = _definitions[definitionId];
		return DrawInstanceResolved(definition, Math.Clamp(zIndex, -4096, 4096), transform, frameFloat, modulate, framePhase);
	}

	private bool DrawInstanceResolved(Definition definition, int zIndex, Transform2D transform, float frameFloat, Color modulate, float framePhase)
	{
		AdobeAnimateRuntimeDefinition runtime = definition.runtime;
		if (!definition.compactCrowdReady || runtime == null || !TryResolveFrame(definition, frameFloat, out var frame, out var interpolationT))
		{
			return false;
		}
		UnifiedBucket unifiedBucket = EnsureBucket(definition, zIndex);
		if (unifiedBucket == null)
		{
			return false;
		}
		bool flag = modulate == Colors.White && unifiedBucket.RenderBucket.AppendInlineSimple(transform, frame.Offset, frame.Count, runtime.GpuPoseTextureBaseTexel, runtime.GpuPoseTextureLayer, interpolationT);
		if (!flag)
		{
			flag = AdobeAnimateMultiMeshBatcher.TryAppendSimpleCompactCrowdFrame(unifiedBucket.Group.StateArena, unifiedBucket.RenderBucket, transform, modulate, Vector2.Zero, frame.Offset, frame.Count, runtime.GpuPoseTextureBaseTexel, runtime.GpuPoseTextureLayer, interpolationT);
		}
		if (!flag)
		{
			return false;
		}
		unifiedBucket.RootCount++;
		return true;
	}

	public FramePublicationResult EndFrame(int expectedInstanceCount = -1)
	{
		return EndFrameCore(updateAnimationClock: true, expectedInstanceCount);
	}

	internal FramePublicationResult EndFrameWithoutAnimationClockUpdate(int expectedInstanceCount = -1)
	{
		return EndFrameCore(updateAnimationClock: false, expectedInstanceCount);
	}

	private FramePublicationResult EndFrameCore(bool updateAnimationClock, int expectedInstanceCount)
	{
		using AdobeAnimateMultiMeshRdUploadDispatcher.Batch batch = AdobeAnimateMultiMeshRdUploadDispatcher.Shared.BeginBatch(_frameVersion);
		int num = 0;
		for (int i = 0; i < _touchedBucketsThisFrame.Count; i++)
		{
			num += _touchedBucketsThisFrame[i].RootCount;
		}
		if (expectedInstanceCount < 0)
		{
			expectedInstanceCount = num;
		}
		foreach (UnifiedResourceGroup value in _resourceGroups.Values)
		{
			if (value.LastTouchedFrame == _frameVersion)
			{
				ApplySimpleAnimationBindings(value, updateAnimationClock);
				value.StateUploaded = value.StateArena.UploadOnce();
				if (value.StateUploaded)
				{
					EnsureSharedCrowdBindings(value);
				}
			}
		}
		bool failNextPublicationForTest = _failNextPublicationForTest;
		_failNextPublicationForTest = false;
		int num2 = 0;
		int num3 = 0;
		for (int j = 0; j < _touchedBucketsThisFrame.Count; j++)
		{
			UnifiedBucket unifiedBucket = _touchedBucketsThisFrame[j];
			UnifiedResourceGroup unifiedResourceGroup = unifiedBucket.Group;
			bool flag = failNextPublicationForTest && j == 0;
			unifiedBucket.Visible = !flag && unifiedResourceGroup.StateUploaded && unifiedBucket.RenderBucket.TryPreparePublication(unifiedResourceGroup.Mesh, unifiedResourceGroup.MeshVersion, unifiedResourceGroup.Material) && unifiedBucket.RenderBucket.Publish(batch, 65536);
			if (unifiedBucket.Visible && unifiedBucket.RenderBucket.InstanceCount != unifiedBucket.RootCount)
			{
				unifiedBucket.Visible = false;
			}
			if (!unifiedBucket.Visible)
			{
				if (!unifiedBucket.RenderBucket.TryQueueHide(batch))
				{
					throw new InvalidOperationException($"BulletField 无法隐藏发布失败的 RD 桶，帧版本: {_frameVersion}");
				}
			}
			else
			{
				num3++;
				num2 += unifiedBucket.RenderBucket.InstanceCount;
			}
		}
		for (int k = 0; k < _touchedBucketsLastFrame.Count; k++)
		{
			UnifiedBucket unifiedBucket2 = _touchedBucketsLastFrame[k];
			if (unifiedBucket2.LastTouchedFrame != _frameVersion)
			{
				if (!unifiedBucket2.RenderBucket.TryQueueHide(batch))
				{
					throw new InvalidOperationException($"BulletField 无法隐藏未触及的 RD 桶，帧版本: {_frameVersion}");
				}
				unifiedBucket2.Visible = false;
			}
		}
		if (!batch.EndBatch())
		{
			throw new InvalidOperationException($"BulletField RD MultiMesh 批次提交失败，帧版本: {_frameVersion}");
		}
		ReclaimIdleBuckets();
		DisposeRetiredMeshes();
		_lastPublication = new FramePublicationResult(_frameVersion, expectedInstanceCount, num, _touchedBucketsThisFrame.Count, num2, num3);
		return _lastPublication;
	}

	internal void FailNextPublicationForTest()
	{
		_failNextPublicationForTest = true;
	}

	internal bool HideOneRetainedBucketForTest()
	{
		for (int i = 0; i < _touchedBucketsThisFrame.Count; i++)
		{
			UnifiedBucket unifiedBucket = _touchedBucketsThisFrame[i];
			if (unifiedBucket.Visible && unifiedBucket.RenderBucket != null)
			{
				unifiedBucket.RenderBucket.Hide();
				unifiedBucket.Visible = false;
				return true;
			}
		}
		return false;
	}

	internal float GetSimpleAnimationNextFlagForTest(int definitionId, int localFrame)
	{
		EnsureSimpleAnimationTexture();
		Definition definition = GetDefinition(definitionId);
		if (definition == null || definition.simpleAnimationBaseTexel < 0 || localFrame < 0 || localFrame >= definition.frameMax || _simpleAnimationImage == null)
		{
			return 0f / 0f;
		}
		int num = definition.simpleAnimationBaseTexel + 1 + localFrame;
		int x = num % 2048;
		int y = num / 2048;
		return _simpleAnimationImage.GetPixel(x, y).A;
	}

	public int GetVisibleInstanceCountForTest()
	{
		int num = 0;
		foreach (UnifiedResourceGroup value in _resourceGroups.Values)
		{
			foreach (UnifiedBucket value2 in value.Buckets.Values)
			{
				if (value2.Visible)
				{
					num += value2.RenderBucket.InstanceCount;
				}
			}
		}
		return num;
	}

	public int GetUnifiedBucketCountForTest()
	{
		int num = 0;
		foreach (UnifiedResourceGroup value in _resourceGroups.Values)
		{
			num += value.Buckets.Count;
		}
		return num;
	}

	internal int GetRetiredMeshCountForTest()
	{
		return _retiredMeshes.Count;
	}

	private void ReclaimIdleBuckets()
	{
		if (_frameVersion < _lastBucketMaintenanceFrame || _frameVersion - _lastBucketMaintenanceFrame < 120)
		{
			return;
		}
		_lastBucketMaintenanceFrame = _frameVersion;
		foreach (UnifiedResourceGroup value2 in _resourceGroups.Values)
		{
			_bucketKeysToRemove.Clear();
			foreach (KeyValuePair<int, UnifiedBucket> bucket in value2.Buckets)
			{
				long lastTouchedFrame = bucket.Value.LastTouchedFrame;
				if (lastTouchedFrame != -9223372036854775808L && _frameVersion >= lastTouchedFrame && _frameVersion - lastTouchedFrame >= 120)
				{
					_bucketKeysToRemove.Add(bucket.Key);
				}
			}
			for (int i = 0; i < _bucketKeysToRemove.Count; i++)
			{
				int num = _bucketKeysToRemove[i];
				if (value2.Buckets.Remove(num, out var value))
				{
					_touchedBucketsLastFrame.Remove(value);
					_touchedBucketsThisFrame.Remove(value);
					ClearCachedBucketReferences(value);
					value2.ExactZBucketLookup[num - -4096] = null;
					value.RenderBucket?.Dispose();
					value.RenderBucket = null;
				}
			}
		}
		_bucketKeysToRemove.Clear();
	}

	private void ClearCachedBucketReferences(UnifiedBucket bucket)
	{
		if (_lastBucket == bucket)
		{
			_lastBucket = null;
			_lastBucketZIndex = -2147483648;
		}
		for (int i = 0; i < _definitions.Count; i++)
		{
			Definition definition = _definitions[i];
			if (definition.exactZBucketLookup != null)
			{
				int num = bucket.ZIndex - -4096;
				if (definition.exactZBucketLookup[num] == bucket)
				{
					definition.exactZBucketLookup[num] = null;
				}
			}
			if (definition.cachedBucket == bucket)
			{
				definition.cachedBucket = null;
				definition.cachedBucketZIndex = -2147483648;
			}
		}
	}

	private UnifiedBucket EnsureBucket(Definition definition, int zIndex, bool touch = true)
	{
		AdobeAnimateRuntimeDefinition runtime = definition.runtime;
		int num = zIndex - -4096;
		UnifiedBucket[] exactZBucketLookup = definition.exactZBucketLookup;
		if (exactZBucketLookup != null)
		{
			UnifiedBucket unifiedBucket = exactZBucketLookup[num];
			if (unifiedBucket != null)
			{
				if (!touch)
				{
					return unifiedBucket;
				}
				return TouchBucket(unifiedBucket);
			}
		}
		if (definition.cachedBucket != null && definition.cachedBucketZIndex == zIndex)
		{
			if (!touch)
			{
				return definition.cachedBucket;
			}
			return TouchBucket(definition.cachedBucket);
		}
		UnifiedResourceGroup value = definition.resourceGroup;
		if (value == null)
		{
			AdobeAnimateCrowdResourceSignature adobeAnimateCrowdResourceSignature = BuildResourceSignature(runtime);
			if (_lastResourceGroup != null && _lastResourceSignature.Equals(adobeAnimateCrowdResourceSignature))
			{
				value = _lastResourceGroup;
			}
			else if (!_resourceGroups.TryGetValue(adobeAnimateCrowdResourceSignature, out value))
			{
				value = CreateResourceGroup(adobeAnimateCrowdResourceSignature, runtime.MaxFrameSliceCount);
				if (value == null)
				{
					return null;
				}
				_resourceGroups.Add(adobeAnimateCrowdResourceSignature, value);
			}
			definition.resourceGroup = value;
			_lastResourceSignature = adobeAnimateCrowdResourceSignature;
			_lastResourceGroup = value;
		}
		if (!EnsureSharedMesh(value, runtime.MaxFrameSliceCount))
		{
			return null;
		}
		if (_lastBucket != null && _lastBucket.Group == value && _lastBucketZIndex == zIndex)
		{
			definition.cachedBucket = _lastBucket;
			definition.cachedBucketZIndex = zIndex;
			if (!touch)
			{
				return _lastBucket;
			}
			return TouchBucket(_lastBucket);
		}
		UnifiedBucket unifiedBucket2 = value.ExactZBucketLookup[num];
		if (unifiedBucket2 == null)
		{
			AdobeAnimateZIndexCrowdBucket adobeAnimateZIndexCrowdBucket = new AdobeAnimateZIndexCrowdBucket();
			adobeAnimateZIndexCrowdBucket.Attach(this, zIndex);
			unifiedBucket2 = new UnifiedBucket
			{
				RenderBucket = adobeAnimateZIndexCrowdBucket,
				Group = value,
				ZIndex = zIndex
			};
			value.Buckets.Add(zIndex, unifiedBucket2);
			value.ExactZBucketLookup[num] = unifiedBucket2;
		}
		if (definition.exactZBucketLookup == null)
		{
			definition.exactZBucketLookup = new UnifiedBucket[8193];
		}
		definition.exactZBucketLookup[num] = unifiedBucket2;
		_lastBucket = unifiedBucket2;
		_lastBucketZIndex = zIndex;
		definition.cachedBucket = unifiedBucket2;
		definition.cachedBucketZIndex = zIndex;
		if (!touch)
		{
			return unifiedBucket2;
		}
		return TouchBucket(unifiedBucket2);
	}

	private static AdobeAnimateCrowdResourceSignature BuildResourceSignature(AdobeAnimateRuntimeDefinition runtime)
	{
		return new AdobeAnimateCrowdResourceSignature(runtime.AtlasTextureArray, runtime.AtlasTextureArraySize, runtime.GpuPoseTextureArray, runtime.GpuPoseTextureSize, null, Vector2I.Zero, null, Vector2I.Zero);
	}

	private static UnifiedResourceGroup CreateResourceGroup(AdobeAnimateCrowdResourceSignature signature, int maxSlices)
	{
		ShaderMaterial shaderMaterial = AdobeAnimateMultiMeshBatcher.CreateSharedCrowdMaterial();
		ArrayMesh arrayMesh = AdobeAnimateMultiMeshBatcher.CreateSharedCrowdMesh(maxSlices);
		if (!GodotObject.IsInstanceValid(shaderMaterial) || !GodotObject.IsInstanceValid(arrayMesh))
		{
			shaderMaterial?.Dispose();
			arrayMesh?.Dispose();
			return null;
		}
		return new UnifiedResourceGroup
		{
			Signature = signature,
			Material = shaderMaterial,
			Mesh = arrayMesh,
			MeshMaxSlices = Math.Max(1, maxSlices),
			MeshVersion = 1
		};
	}

	private bool EnsureSharedMesh(UnifiedResourceGroup group, int maxSlices)
	{
		maxSlices = Math.Max(1, maxSlices);
		if (group.Mesh != null && group.MeshMaxSlices >= maxSlices)
		{
			return true;
		}
		ArrayMesh arrayMesh = AdobeAnimateMultiMeshBatcher.CreateSharedCrowdMesh(maxSlices);
		if (!GodotObject.IsInstanceValid(arrayMesh))
		{
			return false;
		}
		ArrayMesh mesh = group.Mesh;
		group.Mesh = arrayMesh;
		group.MeshMaxSlices = maxSlices;
		group.MeshVersion = ((group.MeshVersion == 2147483647) ? 1 : (group.MeshVersion + 1));
		if (GodotObject.IsInstanceValid(mesh))
		{
			_retiredMeshes.Add(mesh);
		}
		return true;
	}

	private void DisposeRetiredMeshes()
	{
		for (int i = 0; i < _retiredMeshes.Count; i++)
		{
			_retiredMeshes[i]?.Dispose();
		}
		_retiredMeshes.Clear();
	}

	private UnifiedBucket TouchBucket(UnifiedBucket bucket)
	{
		if (bucket.LastTouchedFrame == _frameVersion)
		{
			return bucket;
		}
		UnifiedResourceGroup unifiedResourceGroup = bucket.Group;
		if (unifiedResourceGroup.LastTouchedFrame != _frameVersion)
		{
			unifiedResourceGroup.LastTouchedFrame = _frameVersion;
			unifiedResourceGroup.StateUploaded = false;
			unifiedResourceGroup.StateArena.BeginFrame(_frameVersion);
		}
		bucket.LastTouchedFrame = _frameVersion;
		bucket.RootCount = 0;
		bucket.RenderBucket.BeginFrame(_frameVersion);
		_touchedBucketsThisFrame.Add(bucket);
		return bucket;
	}

	private static bool TryResolveFrame(Definition definition, float localFrameFloat, out PackedFrame frame, out float interpolationT)
	{
		frame = default(PackedFrame);
		interpolationT = 0f;
		AdobeAnimateRuntimeDefinition runtime = definition.runtime;
		if (runtime == null || runtime.Frames.Length == 0 || definition.frameMax <= 0)
		{
			return false;
		}
		float num = localFrameFloat % (float)definition.frameMax;
		if (num < 0f)
		{
			num += (float)definition.frameMax;
		}
		float num2 = (float)definition.clipStart + num;
		int num3 = Math.Min(runtime.Frames.Length, definition.clipStart + definition.frameMax);
		int num4 = Math.Clamp((int)Math.Floor(num2), definition.clipStart, Math.Max(definition.clipStart, num3 - 1));
		interpolationT = Mathf.Clamp(num2 - (float)num4, 0f, 1f);
		if ((definition.preserveTerminalAlias || !definition.hasLoopTerminalAlias) && num4 >= num3 - 1)
		{
			interpolationT = 0f;
		}
		frame = runtime.Frames[num4];
		return frame.Count > 0;
	}

	private void EnsureSimpleAnimationTexture()
	{
		if (!_simpleAnimationTextureDirty)
		{
			return;
		}
		_simpleAnimationTextureDirty = false;
		int num = 1;
		for (int i = 0; i < _definitions.Count; i++)
		{
			Definition definition = _definitions[i];
			definition.simpleAnimationBaseTexel = -1;
			if (definition.runtime != null && definition.frameMax > 0)
			{
				num += 1 + definition.frameMax;
			}
		}
		int num2 = Math.Max(1, (num + 2048 - 1) / 2048);
		int num3;
		for (num3 = 1; num3 < num2; num3 *= 2)
		{
		}
		_simpleAnimationTextureSize = new Vector2I(2048, num3);
		float[] array = new float[2048 * num3 * 4];
		int num4 = 1;
		for (int j = 0; j < _definitions.Count; j++)
		{
			Definition definition2 = _definitions[j];
			AdobeAnimateRuntimeDefinition runtime = definition2.runtime;
			if (runtime == null || definition2.frameMax <= 0)
			{
				continue;
			}
			definition2.simpleAnimationBaseTexel = num4;
			int num5 = num4 * 4;
			array[num5] = definition2.frameMax;
			array[num5 + 1] = runtime.GpuPoseTextureLayer;
			array[num5 + 2] = 0f;
			array[num5 + 3] = 0f;
			int x = runtime.GpuPoseTextureSize.X;
			for (int k = 0; k < definition2.frameMax; k++)
			{
				int num6 = definition2.clipStart + k;
				if ((uint)num6 >= (uint)runtime.Frames.Length)
				{
					break;
				}
				PackedFrame packedFrame = runtime.Frames[num6];
				int num7 = runtime.GpuPoseTextureBaseTexel + packedFrame.Offset * 5;
				int num8 = (num4 + 1 + k) * 4;
				array[num8] = num7 % x;
				array[num8 + 1] = num7 / x;
				array[num8 + 2] = packedFrame.Count;
				array[num8 + 3] = ((k < definition2.frameMax - 1 || (!definition2.preserveTerminalAlias && definition2.hasLoopTerminalAlias)) ? 1f : 0f);
			}
			num4 += 1 + definition2.frameMax;
		}
		ReadOnlySpan<byte> data = MemoryMarshal.AsBytes(array.AsSpan());
		if (_simpleAnimationImage == null || _simpleAnimationImage.GetWidth() != _simpleAnimationTextureSize.X || _simpleAnimationImage.GetHeight() != _simpleAnimationTextureSize.Y || !GodotObject.IsInstanceValid(_simpleAnimationTexture))
		{
			Image image = Image.CreateEmpty(_simpleAnimationTextureSize.X, _simpleAnimationTextureSize.Y, useMipmaps: false, Image.Format.Rgbaf);
			image.SetData(_simpleAnimationTextureSize.X, _simpleAnimationTextureSize.Y, useMipmaps: false, Image.Format.Rgbaf, data);
			ImageTexture imageTexture = ImageTexture.CreateFromImage(image);
			if (!GodotObject.IsInstanceValid(imageTexture))
			{
				image.Dispose();
				for (int l = 0; l < _definitions.Count; l++)
				{
					_definitions[l].simpleAnimationBaseTexel = -1;
				}
				return;
			}
			_simpleAnimationImage?.Dispose();
			_simpleAnimationTexture?.Dispose();
			_simpleAnimationImage = image;
			_simpleAnimationTexture = imageTexture;
		}
		else
		{
			_simpleAnimationImage.SetData(_simpleAnimationTextureSize.X, _simpleAnimationTextureSize.Y, useMipmaps: false, Image.Format.Rgbaf, data);
			_simpleAnimationTexture.Update(_simpleAnimationImage);
		}
		_simpleAnimationTextureVersion = ((_simpleAnimationTextureVersion == 2147483647) ? 1 : (_simpleAnimationTextureVersion + 1));
	}

	private void ApplySimpleAnimationBindings(UnifiedResourceGroup group, bool updateAnimationClock)
	{
		if (group?.Material == null)
		{
			return;
		}
		if (group.SimpleAnimationBindingVersion != _simpleAnimationTextureVersion)
		{
			if (GodotObject.IsInstanceValid(_simpleAnimationTexture))
			{
				group.Material.SetShaderParameter(SimpleAnimationTextureParameter, _simpleAnimationTexture);
			}
			group.Material.SetShaderParameter(SimpleAnimationTextureSizeParameter, new Vector2(_simpleAnimationTextureSize.X, _simpleAnimationTextureSize.Y));
			group.SimpleAnimationBindingVersion = _simpleAnimationTextureVersion;
		}
		if (updateAnimationClock)
		{
			group.Material.SetShaderParameter(AnimationClockParameter, (float)_animationTime);
		}
	}

	private static void EnsureSharedCrowdBindings(UnifiedResourceGroup group)
	{
		TextureLayered texture = group.StateArena.Texture;
		Vector2I textureSize = group.StateArena.TextureSize;
		if (!group.SharedBindingsInitialized || group.SharedBindingsStateTexture != texture || !(group.SharedBindingsStateTextureSize == textureSize))
		{
			AdobeAnimateMultiMeshBatcher.ApplySharedCrowdBindings(group.Material, in group.Signature, group.StateArena);
			group.SharedBindingsInitialized = true;
			group.SharedBindingsStateTexture = texture;
			group.SharedBindingsStateTextureSize = textureSize;
		}
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
			RefreshDefinitionsAfterAtlasChange();
			ClearBuckets(queueFree: true);
		}
	}

	private void RefreshDefinitionsAfterAtlasChange()
	{
		_simpleAnimationTextureDirty = true;
		for (int i = 0; i < _definitions.Count; i++)
		{
			Definition definition = _definitions[i];
			if (definition != null && definition.data != null)
			{
				AdobeAnimateRuntimeDefinition orBuildForCompactCrowd = AdobeAnimateDefinitionCache.GetOrBuildForCompactCrowd(definition.data);
				if (!CanUseUnifiedCompactCrowd(orBuildForCompactCrowd) || !TryResolveClip(orBuildForCompactCrowd, definition.clipName, definition.preserveTerminalAlias, out var clipStart, out var frameCount, out var hasLoopTerminalAlias))
				{
					definition.runtime = null;
					definition.atlas = null;
					definition.frameMax = 0;
					definition.compactCrowdReady = false;
				}
				else
				{
					definition.runtime = orBuildForCompactCrowd;
					definition.clipStart = clipStart;
					definition.frameMax = frameCount;
					definition.hasLoopTerminalAlias = hasLoopTerminalAlias;
					definition.frameRate = ((orBuildForCompactCrowd.FrameRate > 0.0) ? orBuildForCompactCrowd.FrameRate : definition.data.frameRate);
					definition.atlas = orBuildForCompactCrowd.BaseAtlas;
					definition.compactCrowdReady = true;
				}
			}
		}
	}

	private static bool TryResolveClip(AdobeAnimateRuntimeDefinition runtime, string clipName, bool preserveTerminalAlias, out int clipStart, out int frameCount, out bool hasLoopTerminalAlias)
	{
		clipStart = 0;
		frameCount = 0;
		hasLoopTerminalAlias = false;
		if (runtime == null || runtime.Frames.Length == 0)
		{
			return false;
		}
		if (runtime.Clips.Length == 0)
		{
			frameCount = runtime.Frames.Length;
			return true;
		}
		for (int i = 0; i < runtime.Clips.Length; i++)
		{
			if (!string.IsNullOrEmpty(clipName) && runtime.Clips[i].Name.ToString() != clipName)
			{
				continue;
			}
			Vector2I range = runtime.Clips[i].Range;
			clipStart = Math.Clamp(range.X, 0, runtime.Frames.Length - 1);
			int num = Math.Clamp(range.Y, clipStart + 1, runtime.Frames.Length);
			int num2 = num - 1;
			if (num - clipStart > 1 && FramesHaveMatchingPose(runtime, num2, clipStart))
			{
				hasLoopTerminalAlias = true;
				if (!preserveTerminalAlias)
				{
					num = num2;
				}
			}
			frameCount = num - clipStart;
			return frameCount > 0;
		}
		return false;
	}

	private static bool FramesHaveMatchingPose(AdobeAnimateRuntimeDefinition runtime, int leftFrame, int rightFrame)
	{
		if (runtime?.Frames == null || runtime.FramePoseSignatures == null)
		{
			return false;
		}
		if (leftFrame < 0 || rightFrame < 0 || leftFrame >= runtime.Frames.Length || rightFrame >= runtime.Frames.Length)
		{
			return false;
		}
		if (leftFrame >= runtime.FramePoseSignatures.Length || rightFrame >= runtime.FramePoseSignatures.Length)
		{
			return false;
		}
		return runtime.FramePoseSignatures[leftFrame] == runtime.FramePoseSignatures[rightFrame];
	}

	private static bool CanUseUnifiedCompactCrowd(AdobeAnimateRuntimeDefinition runtime)
	{
		if (runtime != null && runtime.Frames.Length != 0 && runtime.MaxFrameSliceCount > 0 && runtime.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(runtime.AtlasTextureArray) && runtime.AtlasTextureArraySize.X > 0f && runtime.AtlasTextureArraySize.Y > 0f && runtime.UsesGpuPoseTextureArray && runtime.GpuPoseTextureRid.IsValid && GodotObject.IsInstanceValid(runtime.GpuPoseTextureArray) && runtime.GpuPoseTextureSize.X > 0)
		{
			return runtime.GpuPoseTextureSize.Y > 0;
		}
		return false;
	}

	internal static string DescribeCompactCompatibility(AdobeAnimateData data)
	{
		AdobeAnimateRuntimeDefinition orBuild = AdobeAnimateDefinitionCache.GetOrBuild(data);
		if (orBuild == null)
		{
			return "sourceKey=" + (data?.GetAtlasSourceKey() ?? "<null>") + " runtime=null compatible=false";
		}
		bool value = orBuild.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(orBuild.AtlasTextureArray);
		bool value2 = orBuild.UsesGpuPoseTextureArray && orBuild.GpuPoseTextureRid.IsValid && GodotObject.IsInstanceValid(orBuild.GpuPoseTextureArray);
		return $"sourceKey={data?.GetAtlasSourceKey() ?? "<null>"} packed={data?.HasPackedRuntimeData() ?? false} frames={orBuild.Frames.Length} maxSlices={orBuild.MaxFrameSliceCount} atlasArray={value} atlasSize={orBuild.AtlasTextureArraySize} poseArray={value2} poseSize={orBuild.GpuPoseTextureSize} poseSignature={orBuild.GpuPoseSignature:X16} manifestPoseSignature={(orBuild.HasGpuPoseManifestEntry ? $"{orBuild.GpuPoseManifestSignature:X16}" : "<missing>")} poseSignatureMatch={orBuild.HasGpuPoseManifestEntry && orBuild.GpuPoseSignature == orBuild.GpuPoseManifestSignature} compatible={CanUseUnifiedCompactCrowd(orBuild)}";
	}

	public override void _ExitTree()
	{
		_lastPublication = default(FramePublicationResult);
		ClearBuckets(queueFree: false);
		_simpleAnimationImage?.Dispose();
		_simpleAnimationTexture?.Dispose();
		_simpleAnimationImage = null;
		_simpleAnimationTexture = null;
		_definitions.Clear();
		_definitionIds.Clear();
		base._ExitTree();
	}

	private void ClearBuckets(bool queueFree)
	{
		foreach (UnifiedResourceGroup value in _resourceGroups.Values)
		{
			foreach (UnifiedBucket value2 in value.Buckets.Values)
			{
				value2.RenderBucket?.ReleaseForOwnerExit(queueFree);
				value2.RenderBucket = null;
			}
			value.Buckets.Clear();
			value.StateArena.Dispose();
			value.Material?.Dispose();
			value.Mesh?.Dispose();
		}
		_resourceGroups.Clear();
		DisposeRetiredMeshes();
		for (int i = 0; i < _definitions.Count; i++)
		{
			Definition definition = _definitions[i];
			definition.resourceGroup = null;
			definition.cachedBucket = null;
			definition.cachedBucketZIndex = -2147483648;
			definition.exactZBucketLookup = null;
			definition.prewarmedMapRowCount = 0;
		}
		_touchedBucketsLastFrame.Clear();
		_touchedBucketsThisFrame.Clear();
		_bucketKeysToRemove.Clear();
		_lastResourceGroup = null;
		_lastBucket = null;
		_lastBucketZIndex = -2147483648;
	}
}
