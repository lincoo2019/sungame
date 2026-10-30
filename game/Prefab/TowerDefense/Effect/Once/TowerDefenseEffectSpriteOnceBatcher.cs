using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public partial class TowerDefenseEffectSpriteOnceBatcher : Node
{
	private sealed class ReferenceComparer<T> : IEqualityComparer<T> where T : class
	{
		public static readonly ReferenceComparer<T> Instance = new ReferenceComparer<T>();

		public bool Equals(T left, T right)
		{
			return left == right;
		}

		public int GetHashCode(T value)
		{
			if (value != null)
			{
				return RuntimeHelpers.GetHashCode(value);
			}
			return 0;
		}
	}

	private sealed class Entry
	{
		public Entry Previous;

		public Entry Next;

		public Entry FreeNext;

		public bool IsLinked;

		public uint LinkGeneration;

		public ulong Order;

		public TowerDefenseEffectSpriteOnce Effect;

		public AdobeAnimateSprite Sprite;

		public AdobeAnimateData Data;

		public EligibilitySnapshot Eligibility;

		public int DefId;

		public int RendererGeneration;

		public string Clip;

		public float PhaseFrame;

		public float FrameRate;

		public int PlaybackFrameCount;

		public int ClipStart;

		public int ClipEndExclusive;

		public ulong PlaybackRevision;

		public int SuppressionToken;

		public int PublishedRenderRevision;

		public int RenderStateRevision;

		public bool RenderStateReady;

		public Transform2D RenderTransform;

		public int RenderZIndex;

		public AnimateMultiMeshRenderer.SimpleDrawContext DrawContext;

		public int DrawContextZIndex;

		public int DrawContextDefId = -1;

		public int DrawContextRendererGeneration;

		public AnimateMultiMeshRenderer.SimpleDrawContext AlternateDrawContext;

		public int AlternateDrawContextZIndex;

		public int AlternateDrawContextDefId = -1;

		public int AlternateDrawContextRendererGeneration;
	}

	private readonly struct PlaybackDescriptorKey(AdobeAnimateData data, string clip) : IEquatable<PlaybackDescriptorKey>
	{
		private readonly AdobeAnimateData _data = data;

		private readonly string _clip = clip;

		public bool Equals(PlaybackDescriptorKey other)
		{
			if (_data == other._data)
			{
				return string.Equals(_clip, other._clip, StringComparison.Ordinal);
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is PlaybackDescriptorKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine((_data != null) ? RuntimeHelpers.GetHashCode(_data) : 0, StringComparer.Ordinal.GetHashCode(_clip ?? ""));
		}
	}

	private sealed class PlaybackDataStamp
	{
		public AdobeAnimateData Data;

		public uint Version = 1u;

		public Dictionary Clips;

		public Variant ClipsVariant;

		public int ClipsHash;

		public double FrameRate;

		public int FrameMax;

		public ulong LastValidationFrame = 18446744073709551615uL;

		public ulong NextNativeAuditFrame;

		public uint DefinitionInvalidatedVersion = 1u;

		public void OnDataChanged()
		{
			Version++;
			if (Version == 0)
			{
				Version = 1u;
			}
			LastValidationFrame = 18446744073709551615uL;
		}
	}

	private sealed class PlaybackDescriptor
	{
		public int DefinitionId;

		public int FrameMax;

		public float FrameRate;

		public int PlaybackFrameCount;

		public int ClipStart;

		public int ClipEndExclusive;

		public uint DataVersion;

		public int AtlasCacheVersion;

		public int RendererGeneration;
	}

	private sealed class PlaybackPlan
	{
		public ClipChoices Choices;

		public PlaybackDescriptor[] Descriptors;

		public uint DataVersion;

		public int AtlasCacheVersion;

		public int RendererGeneration;
	}

	private sealed class NodeFreeTemplate
	{
		public PackedScene Scene;

		public AdobeAnimateData Data;

		public PlaybackDataStamp DataStamp;

		public Transform2D LocalTransform;

		public Transform2D DrawTransform;

		public Vector2 Offset;

		public int LocalZIndex;

		public string DefaultClip;

		public double TimeScale;

		public uint Revision = 1u;

		public string LastSequenceKey;

		public NodeFreeSequence LastSequence;

		public readonly System.Collections.Generic.Dictionary<string, NodeFreeSequence> Sequences = new System.Collections.Generic.Dictionary<string, NodeFreeSequence>(StringComparer.Ordinal);

		public void OnSceneChanged()
		{
			Revision++;
			if (Revision == 0)
			{
				Revision = 1u;
			}
		}
	}

	private sealed class NodeFreeSequence
	{
		public NodeFreeClipChoice[][] Alternatives;

		public uint DataVersion;

		public int AtlasCacheVersion;

		public int RendererGeneration;

		public double CachedDueTime;

		public int CachedDueGroupIndex = -1;

		public uint CachedDueGroupGeneration;
	}

	private readonly struct NodeFreeClipChoice(string clip, int definitionId, int rendererGeneration, int playbackFrameCount, float frameRate, double durationSeconds)
	{
		public string Clip { get; } = clip;

		public int DefinitionId { get; } = definitionId;

		public int RendererGeneration { get; } = rendererGeneration;

		public int PlaybackFrameCount { get; } = playbackFrameCount;

		public float FrameRate { get; } = frameRate;

		public double DurationSeconds { get; } = durationSeconds;
	}

	private struct NodeFreeEntry
	{
		public NodeFreeTemplate Template;

		public NodeFreeSequence Sequence;

		public Action<string> OnAnimeCompleted;

		public Transform2D Transform;

		public AnimateMultiMeshRenderer.SimpleDrawContext DrawContext;

		public int DrawContextZIndex;

		public int DrawContextDefId;

		public int DrawContextRendererGeneration;

		public int ZIndex;

		public int DefinitionId;

		public int RendererGeneration;

		public int AtlasCacheVersion;

		public int SequenceIndex;

		public int PlaybackFrameCount;

		public uint DataVersion;

		public uint TemplateRevision;

		public float PhaseFrame;

		public float FrameRate;

		public string Clip;

		public double DueTime;

		public int DueGroupIndex;

		public int DuePreviousIndex;

		public int DueNextIndex;

		public bool Active;
	}

	private struct NodeFreeDueGroup
	{
		public double DueTime;

		public int HeadIndex;

		public int TailIndex;

		public int HeapSlot;

		public uint Generation;

		public bool Active;

		public bool Processing;
	}

	private sealed class EligibilitySnapshot
	{
		public TowerDefenseEffectSpriteOnceBatcher Owner;

		public TowerDefenseEffectSpriteOnce Effect;

		public AdobeAnimateSprite Sprite;

		public AdobeAnimateData Data;

		public Array<bool> LayerVisible;

		public Array<bool> MediaReplaceUse;

		public int LayerVisibleCount;

		public int MediaReplaceUseCount;

		public int LayerStateVersion;

		public int MediaReplaceStateVersion;

		public int EffectVisualRevision;

		public int EffectEligibilityRevision;

		public int SpriteVisualRevision;

		public int SpriteEligibilityRevision;

		public ulong SpritePlaybackRevision;

		public int ArrayExposureRevision;

		public string RequestedClip;

		public PlaybackPlan CurrentPlan;

		public string AlternateRequestedClip;

		public PlaybackPlan AlternatePlan;

		public ClipChoices Choices;

		public PlaybackDescriptor[] Descriptors;

		public PlaybackDataStamp DataStamp;

		public Entry ActiveEntry;

		public Entry ParkedEntry;

		public uint DataVersion;

		public int AtlasCacheVersion;

		public int RendererGeneration;

		public ulong LastFullValidationFrame = 18446744073709551615uL;

		public bool LastFullValidationResult;

		public bool ScriptChanged;

		public bool ChildOrderChanged;

		public bool VisibilityChanged;

		public bool EligibilityInvalidated;

		public bool RenderStateInvalidated;

		public bool TrustedBuiltInNodePair;

		public ulong NextNativeVisualAuditFrame;

		public ulong NextNativeRenderAuditFrame;

		private void InvalidateEligibility()
		{
			EligibilityInvalidated = true;
			LastFullValidationResult = false;
			LastFullValidationFrame = 18446744073709551615uL;
			if (GodotObject.IsInstanceValid(Owner) && ActiveEntry != null)
			{
				Owner.SetProcess(enable: true);
			}
		}

		public void OnScriptChanged()
		{
			ScriptChanged = true;
			InvalidateEligibility();
		}

		public void OnChildOrderChanged()
		{
			ChildOrderChanged = true;
			InvalidateEligibility();
		}

		public void OnVisibilityChanged()
		{
			VisibilityChanged = true;
			InvalidateEligibility();
		}

		public void OnEffectOnceBatchEligibilityChanged()
		{
			InvalidateEligibility();
		}

		public void OnEffectOnceBatchRenderStateChanged()
		{
			RenderStateInvalidated = true;
			if (GodotObject.IsInstanceValid(Owner))
			{
				Owner._publicationDirty = true;
				if (ActiveEntry != null)
				{
					Owner.SetProcess(enable: true);
				}
			}
		}

		public void OnEffectTreeExiting()
		{
			Owner?.RemoveEligibilitySnapshot(Sprite);
		}

		public void OnSpriteTreeExiting()
		{
			Owner?.RemoveEligibilitySnapshot(Sprite, resumeCpu: true);
		}
	}

	private sealed class ClipChoices
	{
		public string[] Parts { get; }

		public ClipChoices(string[] parts)
		{
			Parts = parts;
		}
	}

	private const int MaxClipChoiceCacheEntries = 256;

	private const int MaxPlaybackDescriptorCacheEntries = 256;

	private const int MaxPlaybackDataStampEntries = 256;

	private const int MaxPlaybackPlanCacheEntries = 256;

	private const int MaxEligibilityCacheEntries = 4096;

	private const int MaxNodeFreeEntries = 4096;

	private const int NodeFreeRecentTemplateCount = 8;

	private const int NativeVisualAuditInterval = 256;

	private const int PlaybackDataNativeAuditInterval = 256;

	private static TowerDefenseEffectSpriteOnceBatcher _instance;

	private Entry _head;

	private Entry _tail;

	private Entry _freeEntries;

	private int _entryCount;

	private int _freeEntryCount;

	private int _createdEntryCount;

	private uint _nextEntryGeneration;

	private ulong _nextEntryOrder;

	private readonly System.Collections.Generic.Dictionary<TowerDefenseEffectSpriteOnce, Entry> _entryByEffect = new System.Collections.Generic.Dictionary<TowerDefenseEffectSpriteOnce, Entry>(ReferenceComparer<TowerDefenseEffectSpriteOnce>.Instance);

	private readonly System.Collections.Generic.Dictionary<string, ClipChoices> _clipChoiceCache = new System.Collections.Generic.Dictionary<string, ClipChoices>(256, StringComparer.Ordinal);

	private readonly System.Collections.Generic.Dictionary<PlaybackDescriptorKey, PlaybackDescriptor> _playbackDescriptors = new System.Collections.Generic.Dictionary<PlaybackDescriptorKey, PlaybackDescriptor>(256);

	private readonly System.Collections.Generic.Dictionary<PlaybackDescriptorKey, PlaybackPlan> _playbackPlans = new System.Collections.Generic.Dictionary<PlaybackDescriptorKey, PlaybackPlan>(256);

	private readonly System.Collections.Generic.Dictionary<AdobeAnimateData, PlaybackDataStamp> _playbackDataStamps = new System.Collections.Generic.Dictionary<AdobeAnimateData, PlaybackDataStamp>(ReferenceComparer<AdobeAnimateData>.Instance);

	private readonly System.Collections.Generic.Dictionary<AdobeAnimateSprite, EligibilitySnapshot> _eligibilityBySprite = new System.Collections.Generic.Dictionary<AdobeAnimateSprite, EligibilitySnapshot>(4096, ReferenceComparer<AdobeAnimateSprite>.Instance);

	private readonly System.Collections.Generic.Dictionary<PackedScene, NodeFreeTemplate> _nodeFreeTemplates = new System.Collections.Generic.Dictionary<PackedScene, NodeFreeTemplate>(ReferenceComparer<PackedScene>.Instance);

	private readonly HashSet<PackedScene> _nodeFreeRejectedScenes = new HashSet<PackedScene>(ReferenceComparer<PackedScene>.Instance);

	private readonly NodeFreeTemplate[] _nodeFreeRecentTemplates = new NodeFreeTemplate[8];

	private int _nodeFreeRecentTemplateCursor;

	private readonly NodeFreeEntry[] _nodeFreeEntries = new NodeFreeEntry[4096];

	private readonly uint[] _nodeFreeGenerations = new uint[4096];

	private readonly int[] _nodeFreeFreeList = new int[4096];

	private readonly int[] _nodeFreeActiveIndices = new int[4096];

	private readonly int[] _nodeFreeActiveSlots = new int[4096];

	private readonly NodeFreeDueGroup[] _nodeFreeDueGroups = new NodeFreeDueGroup[4096];

	private readonly int[] _nodeFreeDueGroupFreeList = new int[4096];

	private readonly int[] _nodeFreeDueGroupHeap = new int[4096];

	private readonly System.Collections.Generic.Dictionary<double, int> _nodeFreeDueGroupByTime = new System.Collections.Generic.Dictionary<double, int>(64);

	private int _nodeFreeFreeCount;

	private int _nodeFreeActiveCount;

	private int _nodeFreeDueGroupFreeCount;

	private int _nodeFreeDueGroupHeapCount;

	private uint _nodeFreeDueResetVersion;

	private bool _nodeFreeStorageInitialized;

	private AnimateMultiMeshRenderer _renderer;

	private int _rendererGeneration;

	private bool _hadBuckets;

	private int _lastDrawSubmissionCount;

	private double _animationTime;

	private bool _publicationDirty = true;

	private bool _hasRetainedPublication;

	private AnimateMultiMeshRenderer.FramePublicationResult _retainedPublication;

	private bool _tearingDown;

	private Node2D _nodeFreeMount;

	private const string BuiltInEffectScriptPath = "res://Prefab/TowerDefense/Effect/Once/TowerDefenseEffectSpriteOnce.cs";

	private const string BuiltInSpriteScriptPath = "res://Extends/AdobeAnimateSprite/AdobeAnimateSpriteBase.cs";

	private const string BuiltInAnimationAssetRoot = "res://Asset/Anime/";

	private const string BuiltInParticleAnimationRoot = "res://Prefab/Particles/Splats/";

	private static Script _builtInEffectScript;

	private static Script _builtInSpriteScript;

	internal int ActiveNodeFreeCount => _nodeFreeActiveCount;

	internal int LastDrawSubmissionCount => _lastDrawSubmissionCount;

	private bool HasCachedTrustedBuiltInNodePair(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data)
	{
		if (sprite != null && _eligibilityBySprite.TryGetValue(sprite, out var value) && value.TrustedBuiltInNodePair && !value.ScriptChanged && !value.ChildOrderChanged && value.Effect == effect && value.Sprite == sprite && value.Data == data && effect?.GetType() == typeof(TowerDefenseEffectSpriteOnce) && sprite.GetType() == typeof(AdobeAnimateSpriteBase))
		{
			return sprite.GetParent() == effect;
		}
		return false;
	}

	private bool IsCachedEligibilityCurrent(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data)
	{
		if (!_eligibilityBySprite.TryGetValue(sprite, out var value))
		{
			return false;
		}
		return IsCachedEligibilityCurrent(value, effect, sprite, data, Engine.GetProcessFrames());
	}

	private bool IsCachedEligibilityCurrent(EligibilitySnapshot snapshot, TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, ulong processFrame)
	{
		if (snapshot == null || snapshot.Effect != effect || snapshot.Sprite != sprite || snapshot.Data != data)
		{
			return false;
		}
		return IsSnapshotEligibilityCurrent(snapshot, effect, sprite, data, processFrame);
	}

	private bool IsSnapshotEligibilityCurrent(EligibilitySnapshot snapshot, TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, ulong processFrame)
	{
		if (snapshot == null || snapshot.Effect != effect || snapshot.Sprite != sprite || snapshot.Data != data || effect?.sprite != sprite || sprite?.flashAnimeData != data || snapshot.EffectEligibilityRevision != effect.GetGpuBatchEligibilityRevision() || snapshot.SpriteEligibilityRevision != sprite.GetEffectOnceBatchEligibilityRevision() || snapshot.SpritePlaybackRevision != sprite.GetEffectOnceBatchPlaybackRevision() || snapshot.EligibilityInvalidated || snapshot.ScriptChanged || snapshot.ChildOrderChanged || snapshot.VisibilityChanged)
		{
			snapshot.LastFullValidationResult = false;
			return false;
		}
		if (snapshot.LastFullValidationFrame == processFrame)
		{
			return snapshot.LastFullValidationResult;
		}
		snapshot.LastFullValidationFrame = processFrame;
		if (processFrame < snapshot.NextNativeVisualAuditFrame)
		{
			snapshot.LastFullValidationResult = true;
			return true;
		}
		snapshot.NextNativeVisualAuditFrame = processFrame + 256;
		bool flag = (snapshot.LastFullValidationResult = IsBoundedEligibilityAuditCurrent(snapshot, effect, sprite, data));
		if (!flag)
		{
			snapshot.EligibilityInvalidated = true;
		}
		return flag;
	}

	private void CacheEligibility(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, string requestedClip, bool trustedBuiltInNodePair)
	{
		if (effect == null || sprite == null || data == null || string.IsNullOrEmpty(requestedClip))
		{
			return;
		}
		PlaybackDataStamp orCreatePlaybackDataStamp = GetOrCreatePlaybackDataStamp(data);
		if (orCreatePlaybackDataStamp == null)
		{
			return;
		}
		PlaybackPlan orCreatePlaybackPlan = GetOrCreatePlaybackPlan(data, requestedClip, orCreatePlaybackDataStamp, Engine.GetProcessFrames());
		if (orCreatePlaybackPlan == null)
		{
			return;
		}
		bool flag = false;
		if (!_eligibilityBySprite.TryGetValue(sprite, out var value))
		{
			if (_eligibilityBySprite.Count >= 4096)
			{
				return;
			}
			value = new EligibilitySnapshot();
			_eligibilityBySprite.Add(sprite, value);
		}
		else
		{
			flag = value.Owner == this && value.Effect == effect && value.Sprite == sprite && value.Data == data;
			if (!flag)
			{
				UnsubscribeEligibilitySnapshot(value);
			}
		}
		sprite.GetEffectOnceBatchEligibilityVersions(out var layerStateVersion, out var mediaReplaceStateVersion);
		value.Owner = this;
		value.Effect = effect;
		value.Sprite = sprite;
		value.Data = data;
		value.LayerVisible = sprite.GetEffectOnceBatchLayerVisible();
		value.MediaReplaceUse = sprite.GetEffectOnceBatchMediaReplaceUse();
		value.LayerVisibleCount = value.LayerVisible?.Count ?? 0;
		value.MediaReplaceUseCount = value.MediaReplaceUse?.Count ?? 0;
		value.LayerStateVersion = layerStateVersion;
		value.MediaReplaceStateVersion = mediaReplaceStateVersion;
		value.EffectVisualRevision = effect.GetGpuBatchVisualRevision();
		value.EffectEligibilityRevision = effect.GetGpuBatchEligibilityRevision();
		value.SpriteVisualRevision = sprite.GetEffectOnceBatchVisualRevision();
		value.SpriteEligibilityRevision = sprite.GetEffectOnceBatchEligibilityRevision();
		value.SpritePlaybackRevision = sprite.GetEffectOnceBatchPlaybackRevision();
		value.ArrayExposureRevision = (flag ? sprite.GetEffectOnceBatchArrayExposureRevision() : sprite.BeginEffectOnceBatchEligibilityTracking());
		value.RequestedClip = null;
		value.CurrentPlan = null;
		value.AlternateRequestedClip = null;
		value.AlternatePlan = null;
		ApplyPlaybackPlanToSnapshot(value, requestedClip, orCreatePlaybackPlan);
		value.DataStamp = orCreatePlaybackDataStamp;
		value.LastFullValidationFrame = Engine.GetProcessFrames();
		value.LastFullValidationResult = true;
		value.ScriptChanged = false;
		value.ChildOrderChanged = false;
		value.VisibilityChanged = false;
		value.EligibilityInvalidated = false;
		value.RenderStateInvalidated = false;
		value.TrustedBuiltInNodePair = trustedBuiltInNodePair;
		ulong processFrames = Engine.GetProcessFrames();
		value.NextNativeVisualAuditFrame = processFrames + 1 + sprite.GetCachedInstanceIdForRender() % 256;
		value.NextNativeRenderAuditFrame = processFrames + 1 + sprite.GetCachedInstanceIdForRender() % 256;
		if (!flag)
		{
			effect.EffectOnceBatchEligibilityChanged += value.OnEffectOnceBatchEligibilityChanged;
			effect.ScriptChanged += value.OnScriptChanged;
			effect.ChildOrderChanged += value.OnChildOrderChanged;
			effect.VisibilityChanged += value.OnVisibilityChanged;
			effect.TreeExiting += value.OnEffectTreeExiting;
			sprite.ScriptChanged += value.OnScriptChanged;
			sprite.EffectOnceBatchEligibilityChanged += value.OnEffectOnceBatchEligibilityChanged;
			sprite.EffectOnceBatchRenderStateChanged += value.OnEffectOnceBatchRenderStateChanged;
			sprite.VisibilityChanged += value.OnVisibilityChanged;
			sprite.TreeExiting += value.OnSpriteTreeExiting;
			sprite.ChildOrderChanged += value.OnChildOrderChanged;
		}
	}

	private void RemoveEligibilitySnapshot(AdobeAnimateSprite sprite, bool resumeCpu = false)
	{
		if (sprite == null || !_eligibilityBySprite.TryGetValue(sprite, out var value))
		{
			return;
		}
		_eligibilityBySprite.Remove(sprite);
		Entry activeEntry = value.ActiveEntry;
		TowerDefenseEffectSpriteOnce towerDefenseEffectSpriteOnce = null;
		AdobeAnimateSprite instance = null;
		string clip = null;
		float elapsedFrame = 0f;
		if (activeEntry != null && activeEntry.Eligibility == value)
		{
			if (resumeCpu && GodotObject.IsInstanceValid(activeEntry.Effect) && GodotObject.IsInstanceValid(activeEntry.Sprite))
			{
				towerDefenseEffectSpriteOnce = activeEntry.Effect;
				instance = activeEntry.Sprite;
				clip = activeEntry.Clip;
				elapsedFrame = activeEntry.Sprite.GetEffectOnceGpuBatchElapsedFrame();
			}
			activeEntry.Eligibility = null;
			value.ActiveEntry = null;
			RemoveAndRecycleEntry(activeEntry);
		}
		if (value.ParkedEntry != null)
		{
			RecycleDetachedEntry(value.ParkedEntry);
			value.ParkedEntry = null;
		}
		UnsubscribeEligibilitySnapshot(value);
		if (towerDefenseEffectSpriteOnce != null && GodotObject.IsInstanceValid(towerDefenseEffectSpriteOnce) && GodotObject.IsInstanceValid(instance))
		{
			towerDefenseEffectSpriteOnce.AbortGpuBatchAndResumeCpu(clip, elapsedFrame);
		}
	}

	private void DetachEntryAndResumeCpu(Entry entry)
	{
		if (entry != null)
		{
			TowerDefenseEffectSpriteOnce effect = entry.Effect;
			AdobeAnimateSprite sprite = entry.Sprite;
			string clip = entry.Clip;
			float effectOnceGpuBatchElapsedFrame = sprite.GetEffectOnceGpuBatchElapsedFrame();
			RemoveEligibilitySnapshot(sprite);
			RemoveAndRecycleEntry(entry);
			if (GodotObject.IsInstanceValid(effect) && GodotObject.IsInstanceValid(sprite))
			{
				effect.AbortGpuBatchAndResumeCpu(clip, effectOnceGpuBatchElapsedFrame);
			}
		}
	}

	private static void UnsubscribeEligibilitySnapshot(EligibilitySnapshot snapshot)
	{
		if (snapshot != null)
		{
			if (GodotObject.IsInstanceValid(snapshot.Effect))
			{
				snapshot.Effect.EffectOnceBatchEligibilityChanged -= snapshot.OnEffectOnceBatchEligibilityChanged;
				snapshot.Effect.ScriptChanged -= snapshot.OnScriptChanged;
				snapshot.Effect.ChildOrderChanged -= snapshot.OnChildOrderChanged;
				snapshot.Effect.VisibilityChanged -= snapshot.OnVisibilityChanged;
				snapshot.Effect.TreeExiting -= snapshot.OnEffectTreeExiting;
			}
			if (GodotObject.IsInstanceValid(snapshot.Sprite))
			{
				snapshot.Sprite.EndEffectOnceBatchEligibilityTracking();
				snapshot.Sprite.EffectOnceBatchEligibilityChanged -= snapshot.OnEffectOnceBatchEligibilityChanged;
				snapshot.Sprite.EffectOnceBatchRenderStateChanged -= snapshot.OnEffectOnceBatchRenderStateChanged;
				snapshot.Sprite.ScriptChanged -= snapshot.OnScriptChanged;
				snapshot.Sprite.VisibilityChanged -= snapshot.OnVisibilityChanged;
				snapshot.Sprite.TreeExiting -= snapshot.OnSpriteTreeExiting;
				snapshot.Sprite.ChildOrderChanged -= snapshot.OnChildOrderChanged;
			}
			snapshot.Owner = null;
		}
	}

	private static bool CanBatchSprite(AdobeAnimateSprite sprite)
	{
		if (sprite.needMediaReplaceUpdate || sprite.atlasProfileOverride != null || sprite.pause || !sprite.Visible || (sprite.IsInsideTree() && !sprite.IsVisibleInTree()) || sprite.playBack || sprite.loop || sprite.blend || sprite.track.Count > 0 || sprite.refreshEveryFrame || sprite.refreshEveryFlame || Math.Abs(sprite.timeScale - 1.0) > 0.0001 || !IsWhite(sprite.Modulate) || !IsWhite(sprite.SelfModulate) || sprite.GetChildCount() > 0 || HasHiddenLayer(sprite.GetLayerVisibleForInternalRead()) || HasMediaReplace(sprite.GetMediaReplaceUseForInternalRead()))
		{
			return false;
		}
		return true;
	}

	private static bool IsCachedArrayEligibilityCurrent(EligibilitySnapshot snapshot)
	{
		Array<bool> layerVisible = snapshot.LayerVisible;
		Array<bool> mediaReplaceUse = snapshot.MediaReplaceUse;
		if ((layerVisible?.Count ?? 0) == snapshot.LayerVisibleCount && (mediaReplaceUse?.Count ?? 0) == snapshot.MediaReplaceUseCount && !HasHiddenLayer(layerVisible))
		{
			return !HasMediaReplace(mediaReplaceUse);
		}
		return false;
	}

	private bool IsBoundedEligibilityAuditCurrent(EligibilitySnapshot snapshot, TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data)
	{
		sprite.GetEffectOnceBatchEligibilityVersions(out var layerStateVersion, out var mediaReplaceStateVersion);
		if (effect.sprite == sprite && sprite.flashAnimeData == data && effect.useGpuBatch && sprite.atlasProfileOverride == null && snapshot.EffectEligibilityRevision == effect.GetGpuBatchEligibilityRevision() && snapshot.SpriteEligibilityRevision == sprite.GetEffectOnceBatchEligibilityRevision() && snapshot.SpritePlaybackRevision == sprite.GetEffectOnceBatchPlaybackRevision() && snapshot.LayerStateVersion == layerStateVersion && snapshot.MediaReplaceStateVersion == mediaReplaceStateVersion && snapshot.ArrayExposureRevision == sprite.GetEffectOnceBatchArrayExposureRevision() && snapshot.LayerVisible == sprite.GetEffectOnceBatchLayerVisible() && snapshot.MediaReplaceUse == sprite.GetEffectOnceBatchMediaReplaceUse() && IsCachedArrayEligibilityCurrent(snapshot) && IsWhite(effect.Modulate) && IsWhite(effect.SelfModulate) && IsWhite(sprite.Modulate) && IsWhite(sprite.SelfModulate) && !sprite.needMediaReplaceUpdate && !sprite.playBack && !sprite.loop && !sprite.blend && sprite.track.Count == 0 && !sprite.refreshEveryFrame && !sprite.refreshEveryFlame && Math.Abs(sprite.timeScale - 1.0) <= 0.0001)
		{
			return IsBatchMountCompatible(effect, sprite);
		}
		return false;
	}

	private bool CanBatchEffectSprite(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite)
	{
		if (effect != null && effect.useGpuBatch && effect.sprite == sprite && effect.Visible && sprite != null && sprite.Visible && !sprite.pause && effect.IsInsideTree() && sprite.IsInsideTree() && effect.IsVisibleInTree() && sprite.IsVisibleInTree() && IsWhite(effect.Modulate) && IsWhite(effect.SelfModulate) && CanBatchSprite(sprite))
		{
			return IsBatchMountCompatible(effect, sprite);
		}
		return false;
	}

	private bool IsBatchMountCompatible(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite)
	{
		Node parent = GetParent();
		if (!GodotObject.IsInstanceValid(parent) || !GodotObject.IsInstanceValid(_renderer) || (parent != effect && !parent.IsAncestorOf(effect)) || sprite.GetViewport() != _renderer.GetViewport())
		{
			return false;
		}
		AdobeAnimateRenderManager.ResolveRenderMount(sprite, out var mountParent, out var canvasLayer);
		AdobeAnimateRenderManager.ResolveRenderMount(_renderer, out var mountParent2, out var canvasLayer2);
		if (mountParent == mountParent2)
		{
			return canvasLayer == canvasLayer2;
		}
		return false;
	}

	private static bool IsEntryValid(Entry entry)
	{
		if (entry != null && GodotObject.IsInstanceValid(entry.Effect) && GodotObject.IsInstanceValid(entry.Sprite) && entry.Effect.IsInsideTree())
		{
			return entry.Sprite.IsInsideTree();
		}
		return false;
	}

	private static bool HasTrustedActiveSnapshot(Entry entry)
	{
		EligibilitySnapshot eligibilitySnapshot = entry?.Eligibility;
		if (entry != null && entry.IsLinked && eligibilitySnapshot != null && eligibilitySnapshot.ActiveEntry == entry && eligibilitySnapshot.Effect == entry.Effect && eligibilitySnapshot.Sprite == entry.Sprite)
		{
			return eligibilitySnapshot.Data == entry.Data;
		}
		return false;
	}

	private Entry RentEntry()
	{
		Entry entry = _freeEntries;
		if (entry != null)
		{
			_freeEntries = entry.FreeNext;
			entry.FreeNext = null;
			_freeEntryCount--;
		}
		else
		{
			_createdEntryCount++;
			entry = new Entry();
		}
		entry.LinkGeneration = NextEntryGeneration();
		return entry;
	}

	private void AppendEntry(Entry entry)
	{
		if (entry != null && !entry.IsLinked)
		{
			entry.Order = NextEntryOrder();
			entry.Previous = _tail;
			entry.Next = null;
			if (_tail != null)
			{
				_tail.Next = entry;
			}
			else
			{
				_head = entry;
			}
			_tail = entry;
			entry.IsLinked = true;
			_entryCount++;
			_publicationDirty = true;
			if (_entryCount == 1)
			{
				SetProcess(enable: true);
			}
		}
	}

	private void MoveEntryToTail(Entry entry)
	{
		if (entry == null || !entry.IsLinked)
		{
			return;
		}
		if (entry != _tail)
		{
			Entry previous = entry.Previous;
			Entry next = entry.Next;
			if (previous != null)
			{
				previous.Next = next;
			}
			else
			{
				_head = next;
			}
			if (next != null)
			{
				next.Previous = previous;
			}
			entry.Previous = _tail;
			entry.Next = null;
			if (_tail != null)
			{
				_tail.Next = entry;
			}
			else
			{
				_head = entry;
			}
			_tail = entry;
		}
		entry.Order = NextEntryOrder();
		_publicationDirty = true;
	}

	private uint NextEntryGeneration()
	{
		_nextEntryGeneration++;
		if (_nextEntryGeneration == 0)
		{
			_nextEntryGeneration = 1u;
		}
		return _nextEntryGeneration;
	}

	private ulong NextEntryOrder()
	{
		_nextEntryOrder++;
		if (_nextEntryOrder != 0L)
		{
			return _nextEntryOrder;
		}
		ulong num = 0uL;
		for (Entry entry = _head; entry != null; entry = entry.Next)
		{
			num = (entry.Order = num + 1);
		}
		_nextEntryOrder = num + 1;
		return _nextEntryOrder;
	}

	private Entry ResolveTraversalEntryAfterCallback(Entry candidate, uint candidateGeneration, ulong orderWatermark, ulong upperOrderExclusive)
	{
		if (candidate != null && candidate.IsLinked && candidate.LinkGeneration == candidateGeneration && candidate.Order <= orderWatermark && candidate.Order < upperOrderExclusive)
		{
			return candidate;
		}
		for (Entry entry = _tail; entry != null; entry = entry.Previous)
		{
			if (entry.IsLinked && entry.Order <= orderWatermark && entry.Order < upperOrderExclusive)
			{
				return entry;
			}
		}
		return null;
	}

	private void RemoveAndRecycleEntry(Entry entry)
	{
		if (entry != null && entry.IsLinked)
		{
			DetachEntry(entry);
			RecycleDetachedEntry(entry);
		}
	}

	private void DetachEntry(Entry entry, bool dictionaryAlreadyRemoved = false)
	{
		if (entry != null && entry.IsLinked)
		{
			Entry previous = entry.Previous;
			Entry next = entry.Next;
			if (previous != null)
			{
				previous.Next = next;
			}
			else
			{
				_head = next;
			}
			if (next != null)
			{
				next.Previous = previous;
			}
			else
			{
				_tail = previous;
			}
			entry.IsLinked = false;
			_entryCount--;
			_publicationDirty = true;
			TowerDefenseEffectSpriteOnce effect = entry.Effect;
			if (!dictionaryAlreadyRemoved && effect != null)
			{
				_entryByEffect.Remove(effect);
			}
			if (entry.Eligibility != null && entry.Eligibility.ActiveEntry == entry)
			{
				entry.Eligibility.ActiveEntry = null;
			}
			entry.Previous = null;
			entry.Next = null;
		}
	}

	private void RecycleDetachedEntry(Entry entry)
	{
		if (entry != null && !entry.IsLinked)
		{
			ResetEntry(entry);
			entry.FreeNext = _freeEntries;
			_freeEntries = entry;
			_freeEntryCount++;
		}
	}

	private static void ResetEntry(Entry entry)
	{
		entry.Previous = null;
		entry.Next = null;
		entry.FreeNext = null;
		entry.IsLinked = false;
		entry.Order = 0uL;
		entry.Effect = null;
		entry.Sprite = null;
		entry.Data = null;
		entry.Eligibility = null;
		entry.DefId = 0;
		entry.RendererGeneration = 0;
		entry.Clip = null;
		entry.PhaseFrame = 0f;
		entry.FrameRate = 0f;
		entry.PlaybackFrameCount = 0;
		entry.ClipStart = 0;
		entry.ClipEndExclusive = 0;
		entry.PlaybackRevision = 0uL;
		entry.SuppressionToken = 0;
		entry.PublishedRenderRevision = 0;
		entry.RenderStateRevision = 0;
		entry.RenderStateReady = false;
		entry.RenderTransform = default(Transform2D);
		entry.RenderZIndex = 0;
		entry.DrawContext = default(AnimateMultiMeshRenderer.SimpleDrawContext);
		entry.DrawContextZIndex = 0;
		entry.DrawContextDefId = -1;
		entry.DrawContextRendererGeneration = 0;
		entry.AlternateDrawContext = default(AnimateMultiMeshRenderer.SimpleDrawContext);
		entry.AlternateDrawContextZIndex = 0;
		entry.AlternateDrawContextDefId = -1;
		entry.AlternateDrawContextRendererGeneration = 0;
	}

	private void EnsureNodeFreeStorage()
	{
		if (!_nodeFreeStorageInitialized)
		{
			for (int i = 0; i < 4096; i++)
			{
				_nodeFreeFreeList[i] = 4096 - i - 1;
				_nodeFreeActiveSlots[i] = -1;
				_nodeFreeDueGroupFreeList[i] = 4096 - i - 1;
				_nodeFreeDueGroups[i] = new NodeFreeDueGroup
				{
					HeadIndex = -1,
					TailIndex = -1,
					HeapSlot = -1
				};
			}
			_nodeFreeFreeCount = 4096;
			_nodeFreeDueGroupFreeCount = 4096;
			_nodeFreeDueGroupHeapCount = 0;
			_nodeFreeDueGroupByTime.Clear();
			_nodeFreeStorageInitialized = true;
		}
	}

	private bool TryGetNodeFreeIndex(int candidateIndex, uint generation, out int index)
	{
		if ((uint)candidateIndex < 4096u && generation != 0 && _nodeFreeGenerations[candidateIndex] == generation && _nodeFreeActiveSlots[candidateIndex] >= 0 && _nodeFreeEntries[candidateIndex].Active)
		{
			index = candidateIndex;
			return true;
		}
		index = -1;
		return false;
	}

	private void ScheduleNodeFreeDue(int index)
	{
		ScheduleNodeFreeDue(index, -1, 0u, out var _, out var _);
	}

	private void ScheduleNodeFreeDue(int index, int preferredGroupIndex, uint preferredGroupGeneration, out int scheduledGroupIndex, out uint scheduledGroupGeneration)
	{
		ref NodeFreeEntry reference = ref _nodeFreeEntries[index];
		double dueTime = reference.DueTime;
		int value = -1;
		if ((uint)preferredGroupIndex < 4096u)
		{
			ref NodeFreeDueGroup reference2 = ref _nodeFreeDueGroups[preferredGroupIndex];
			if (reference2.Active && !reference2.Processing && reference2.Generation == preferredGroupGeneration && reference2.DueTime == dueTime)
			{
				value = preferredGroupIndex;
			}
		}
		if (value < 0 && !_nodeFreeDueGroupByTime.TryGetValue(dueTime, out value))
		{
			if (_nodeFreeDueGroupFreeCount <= 0)
			{
				throw new InvalidOperationException("One-shot GPU due-group capacity exhausted.");
			}
			value = _nodeFreeDueGroupFreeList[--_nodeFreeDueGroupFreeCount];
			uint num = _nodeFreeDueGroups[value].Generation + 1;
			if (num == 0)
			{
				num = 1u;
			}
			_nodeFreeDueGroups[value] = new NodeFreeDueGroup
			{
				DueTime = dueTime,
				HeadIndex = -1,
				TailIndex = -1,
				HeapSlot = -1,
				Generation = num,
				Active = true
			};
			_nodeFreeDueGroupByTime.Add(dueTime, value);
			AddNodeFreeDueGroupToHeap(value);
		}
		ref NodeFreeDueGroup reference3 = ref _nodeFreeDueGroups[value];
		int tailIndex = reference3.TailIndex;
		reference.DueGroupIndex = value;
		reference.DuePreviousIndex = tailIndex;
		reference.DueNextIndex = -1;
		if (tailIndex >= 0)
		{
			_nodeFreeEntries[tailIndex].DueNextIndex = index;
		}
		else
		{
			reference3.HeadIndex = index;
		}
		reference3.TailIndex = index;
		scheduledGroupIndex = value;
		scheduledGroupGeneration = reference3.Generation;
	}

	private void UnscheduleNodeFreeDue(int index)
	{
		ref NodeFreeEntry reference = ref _nodeFreeEntries[index];
		int dueGroupIndex = reference.DueGroupIndex;
		int duePreviousIndex = reference.DuePreviousIndex;
		int dueNextIndex = reference.DueNextIndex;
		reference.DueGroupIndex = -1;
		reference.DuePreviousIndex = -1;
		reference.DueNextIndex = -1;
		if ((uint)dueGroupIndex >= 4096u)
		{
			return;
		}
		ref NodeFreeDueGroup reference2 = ref _nodeFreeDueGroups[dueGroupIndex];
		if (reference2.Active)
		{
			if (duePreviousIndex >= 0)
			{
				_nodeFreeEntries[duePreviousIndex].DueNextIndex = dueNextIndex;
			}
			else if (reference2.HeadIndex == index)
			{
				reference2.HeadIndex = dueNextIndex;
			}
			if (dueNextIndex >= 0)
			{
				_nodeFreeEntries[dueNextIndex].DuePreviousIndex = duePreviousIndex;
			}
			else if (reference2.TailIndex == index)
			{
				reference2.TailIndex = duePreviousIndex;
			}
			if (reference2.HeadIndex < 0 && !reference2.Processing)
			{
				ReleaseNodeFreeDueGroup(dueGroupIndex);
			}
		}
	}

	private bool TryPeekNodeFreeDueGroup(out int groupIndex, out double dueTime)
	{
		if (_nodeFreeDueGroupHeapCount <= 0)
		{
			groupIndex = -1;
			dueTime = 0.0;
			return false;
		}
		groupIndex = _nodeFreeDueGroupHeap[0];
		dueTime = _nodeFreeDueGroups[groupIndex].DueTime;
		return true;
	}

	private int PopNodeFreeDueGroup()
	{
		int num = _nodeFreeDueGroupHeap[0];
		RemoveNodeFreeDueGroupFromHeap(num);
		return num;
	}

	private void AddNodeFreeDueGroupToHeap(int groupIndex)
	{
		int num = _nodeFreeDueGroupHeapCount++;
		_nodeFreeDueGroupHeap[num] = groupIndex;
		_nodeFreeDueGroups[groupIndex].HeapSlot = num;
		SiftNodeFreeDueGroupUp(num);
	}

	private void RemoveNodeFreeDueGroupFromHeap(int groupIndex)
	{
		int heapSlot = _nodeFreeDueGroups[groupIndex].HeapSlot;
		if ((uint)heapSlot >= (uint)_nodeFreeDueGroupHeapCount)
		{
			return;
		}
		int num = --_nodeFreeDueGroupHeapCount;
		int num2 = _nodeFreeDueGroupHeap[num];
		_nodeFreeDueGroups[groupIndex].HeapSlot = -1;
		if (heapSlot != num)
		{
			_nodeFreeDueGroupHeap[heapSlot] = num2;
			_nodeFreeDueGroups[num2].HeapSlot = heapSlot;
			int num3 = heapSlot - 1 >> 1;
			if (heapSlot > 0 && NodeFreeDueGroupComesBefore(num2, _nodeFreeDueGroupHeap[num3]))
			{
				SiftNodeFreeDueGroupUp(heapSlot);
			}
			else
			{
				SiftNodeFreeDueGroupDown(heapSlot);
			}
		}
	}

	private void SiftNodeFreeDueGroupUp(int slot)
	{
		int num = _nodeFreeDueGroupHeap[slot];
		while (slot > 0)
		{
			int num2 = slot - 1 >> 1;
			int num3 = _nodeFreeDueGroupHeap[num2];
			if (!NodeFreeDueGroupComesBefore(num, num3))
			{
				break;
			}
			_nodeFreeDueGroupHeap[slot] = num3;
			_nodeFreeDueGroups[num3].HeapSlot = slot;
			slot = num2;
		}
		_nodeFreeDueGroupHeap[slot] = num;
		_nodeFreeDueGroups[num].HeapSlot = slot;
	}

	private void SiftNodeFreeDueGroupDown(int slot)
	{
		int num = _nodeFreeDueGroupHeap[slot];
		int num2 = _nodeFreeDueGroupHeapCount >> 1;
		while (slot < num2)
		{
			int num3 = (slot << 1) + 1;
			int num4 = num3 + 1;
			int num5 = num3;
			int num6 = _nodeFreeDueGroupHeap[num3];
			if (num4 < _nodeFreeDueGroupHeapCount)
			{
				int num7 = _nodeFreeDueGroupHeap[num4];
				if (NodeFreeDueGroupComesBefore(num7, num6))
				{
					num5 = num4;
					num6 = num7;
				}
			}
			if (!NodeFreeDueGroupComesBefore(num6, num))
			{
				break;
			}
			_nodeFreeDueGroupHeap[slot] = num6;
			_nodeFreeDueGroups[num6].HeapSlot = slot;
			slot = num5;
		}
		_nodeFreeDueGroupHeap[slot] = num;
		_nodeFreeDueGroups[num].HeapSlot = slot;
	}

	private bool NodeFreeDueGroupComesBefore(int leftGroupIndex, int rightGroupIndex)
	{
		double dueTime = _nodeFreeDueGroups[leftGroupIndex].DueTime;
		double dueTime2 = _nodeFreeDueGroups[rightGroupIndex].DueTime;
		if (!(dueTime < dueTime2))
		{
			if (dueTime == dueTime2)
			{
				return leftGroupIndex < rightGroupIndex;
			}
			return false;
		}
		return true;
	}

	private void RemoveNodeFreeDueGroupLookup(int groupIndex, double dueTime)
	{
		if (_nodeFreeDueGroupByTime.TryGetValue(dueTime, out var value) && value == groupIndex)
		{
			_nodeFreeDueGroupByTime.Remove(dueTime);
		}
	}

	private void ReleaseNodeFreeDueGroup(int groupIndex)
	{
		ref NodeFreeDueGroup reference = ref _nodeFreeDueGroups[groupIndex];
		if (reference.Active)
		{
			RemoveNodeFreeDueGroupLookup(groupIndex, reference.DueTime);
			if (reference.HeapSlot >= 0)
			{
				RemoveNodeFreeDueGroupFromHeap(groupIndex);
			}
			reference = new NodeFreeDueGroup
			{
				HeadIndex = -1,
				TailIndex = -1,
				HeapSlot = -1
			};
			_nodeFreeDueGroupFreeList[_nodeFreeDueGroupFreeCount++] = groupIndex;
		}
	}

	private void ResetNodeFreeDueGroups()
	{
		_nodeFreeDueResetVersion++;
		_nodeFreeDueGroupByTime.Clear();
		_nodeFreeDueGroupHeapCount = 0;
		_nodeFreeDueGroupFreeCount = 4096;
		for (int i = 0; i < 4096; i++)
		{
			_nodeFreeDueGroupFreeList[i] = 4096 - i - 1;
			_nodeFreeDueGroups[i] = new NodeFreeDueGroup
			{
				HeadIndex = -1,
				TailIndex = -1,
				HeapSlot = -1
			};
		}
		for (int j = 0; j < _nodeFreeActiveCount; j++)
		{
			ref NodeFreeEntry reference = ref _nodeFreeEntries[_nodeFreeActiveIndices[j]];
			reference.DueGroupIndex = -1;
			reference.DuePreviousIndex = -1;
			reference.DueNextIndex = -1;
		}
	}

	private void RemoveNodeFreeAt(int index)
	{
		ref NodeFreeEntry reference = ref _nodeFreeEntries[index];
		if (reference.Active)
		{
			UnscheduleNodeFreeDue(index);
			int num = _nodeFreeActiveSlots[index];
			int num2 = _nodeFreeActiveCount - 1;
			if (num != num2)
			{
				int num3 = _nodeFreeActiveIndices[num2];
				_nodeFreeActiveIndices[num] = num3;
				_nodeFreeActiveSlots[num3] = num;
			}
			_nodeFreeActiveCount = num2;
			_nodeFreeActiveSlots[index] = -1;
			reference = default(NodeFreeEntry);
			_nodeFreeGenerations[index]++;
			if (_nodeFreeGenerations[index] == 0)
			{
				_nodeFreeGenerations[index] = 1u;
			}
			_nodeFreeFreeList[_nodeFreeFreeCount++] = index;
			_publicationDirty = true;
		}
	}

	private void RebuildNodeFreeDueQueue()
	{
		ResetNodeFreeDueGroups();
		for (int i = 0; i < _nodeFreeActiveCount; i++)
		{
			int index = _nodeFreeActiveIndices[i];
			ScheduleNodeFreeDue(index);
		}
	}

	private void ClearNodeFreeEntries()
	{
		while (_nodeFreeActiveCount > 0)
		{
			RemoveNodeFreeAt(_nodeFreeActiveIndices[_nodeFreeActiveCount - 1]);
		}
		ResetNodeFreeDueGroups();
	}

	private bool TryGetPreparedDescriptor(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, string requestedClip, out string resolvedClip, out PlaybackDescriptor descriptor, out EligibilitySnapshot preparedSnapshot)
	{
		resolvedClip = null;
		descriptor = null;
		preparedSnapshot = null;
		if (!_eligibilityBySprite.TryGetValue(sprite, out var value))
		{
			return false;
		}
		ulong processFrames = Engine.GetProcessFrames();
		if (!string.Equals(value.RequestedClip, requestedClip, StringComparison.Ordinal))
		{
			if (!TryGetPlaybackPlanDescriptor(value, effect, sprite, data, requestedClip, processFrames, out var plan, out resolvedClip, out descriptor))
			{
				return false;
			}
			ApplyPlaybackPlanToSnapshot(value, requestedClip, plan);
			preparedSnapshot = value;
			return true;
		}
		if (!TryGetPreparedDescriptor(value, effect, sprite, data, requestedClip, processFrames, out resolvedClip, out descriptor))
		{
			return false;
		}
		preparedSnapshot = value;
		return true;
	}

	private bool TryGetPreparedDescriptor(EligibilitySnapshot snapshot, TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, string requestedClip, ulong processFrame, out string resolvedClip, out PlaybackDescriptor descriptor)
	{
		resolvedClip = null;
		descriptor = null;
		if (!string.Equals(snapshot?.RequestedClip, requestedClip, StringComparison.Ordinal) || !IsCachedEligibilityCurrent(snapshot, effect, sprite, data, processFrame))
		{
			return false;
		}
		ValidatePlaybackDataStamp(snapshot.DataStamp, processFrame);
		if (snapshot.DataStamp == null || snapshot.DataVersion != snapshot.DataStamp.Version || snapshot.AtlasCacheVersion != AdobeAnimateGlobalAtlasCache.CacheVersion || snapshot.RendererGeneration != _rendererGeneration || snapshot.Choices == null || snapshot.Descriptors == null || snapshot.Descriptors.Length != snapshot.Choices.Parts.Length || snapshot.Descriptors.Length == 0)
		{
			return false;
		}
		int num = ((snapshot.Descriptors.Length != 1) ? GD.RandRange(0, snapshot.Descriptors.Length - 1) : 0);
		descriptor = snapshot.Descriptors[num];
		resolvedClip = snapshot.Choices.Parts[num];
		if (descriptor != null && descriptor.DataVersion == snapshot.DataVersion && descriptor.AtlasCacheVersion == snapshot.AtlasCacheVersion)
		{
			return descriptor.RendererGeneration == snapshot.RendererGeneration;
		}
		return false;
	}

	private bool TryGetPlaybackPlanDescriptor(EligibilitySnapshot snapshot, TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, string requestedClip, ulong processFrame, out PlaybackPlan plan, out string resolvedClip, out PlaybackDescriptor descriptor)
	{
		plan = null;
		resolvedClip = null;
		descriptor = null;
		if (!IsCachedEligibilityCurrent(snapshot, effect, sprite, data, processFrame))
		{
			return false;
		}
		ValidatePlaybackDataStamp(snapshot.DataStamp, processFrame);
		if (snapshot.DataStamp == null)
		{
			return false;
		}
		if (string.Equals(snapshot.AlternateRequestedClip, requestedClip, StringComparison.Ordinal) && IsPlaybackPlanCurrent(snapshot.AlternatePlan, snapshot.DataStamp))
		{
			plan = snapshot.AlternatePlan;
		}
		else
		{
			plan = GetOrCreatePlaybackPlan(data, requestedClip, snapshot.DataStamp, processFrame);
		}
		if (plan == null || plan.DataVersion != snapshot.DataStamp.Version || plan.AtlasCacheVersion != AdobeAnimateGlobalAtlasCache.CacheVersion || plan.RendererGeneration != _rendererGeneration || plan.Choices == null || plan.Descriptors == null || plan.Descriptors.Length != plan.Choices.Parts.Length || plan.Descriptors.Length == 0)
		{
			return false;
		}
		int num = ((plan.Descriptors.Length != 1) ? GD.RandRange(0, plan.Descriptors.Length - 1) : 0);
		descriptor = plan.Descriptors[num];
		resolvedClip = plan.Choices.Parts[num];
		if (descriptor != null && descriptor.DataVersion == plan.DataVersion && descriptor.AtlasCacheVersion == plan.AtlasCacheVersion)
		{
			return descriptor.RendererGeneration == plan.RendererGeneration;
		}
		return false;
	}

	private static void ApplyPlaybackPlanToSnapshot(EligibilitySnapshot snapshot, string requestedClip, PlaybackPlan plan)
	{
		if (snapshot.CurrentPlan != null && !string.Equals(snapshot.RequestedClip, requestedClip, StringComparison.Ordinal))
		{
			snapshot.AlternateRequestedClip = snapshot.RequestedClip;
			snapshot.AlternatePlan = snapshot.CurrentPlan;
		}
		snapshot.RequestedClip = requestedClip;
		snapshot.CurrentPlan = plan;
		snapshot.Choices = plan.Choices;
		snapshot.Descriptors = plan.Descriptors;
		snapshot.DataVersion = plan.DataVersion;
		snapshot.AtlasCacheVersion = plan.AtlasCacheVersion;
		snapshot.RendererGeneration = plan.RendererGeneration;
	}

	private bool IsPlaybackPlanCurrent(PlaybackPlan plan, PlaybackDataStamp stamp)
	{
		if (plan != null && stamp != null && plan.DataVersion == stamp.Version && plan.AtlasCacheVersion == AdobeAnimateGlobalAtlasCache.CacheVersion)
		{
			return plan.RendererGeneration == _rendererGeneration;
		}
		return false;
	}

	private bool TryResolvePlaybackDescriptor(AdobeAnimateData data, string clip, bool trustedBuiltInPair, out PlaybackDescriptor descriptor, out bool descriptorWasTrusted)
	{
		descriptor = null;
		descriptorWasTrusted = false;
		if (data == null || string.IsNullOrEmpty(clip))
		{
			return false;
		}
		PlaybackDataStamp playbackDataStamp = null;
		if (trustedBuiltInPair)
		{
			playbackDataStamp = GetOrCreatePlaybackDataStamp(data);
			descriptorWasTrusted = playbackDataStamp != null;
			if (playbackDataStamp != null)
			{
				ValidatePlaybackDataStamp(playbackDataStamp, Engine.GetProcessFrames());
			}
		}
		EnsureRenderer();
		PlaybackDescriptorKey key = new PlaybackDescriptorKey(data, clip);
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (descriptorWasTrusted && _playbackDescriptors.TryGetValue(key, out descriptor) && descriptor.DataVersion == playbackDataStamp.Version && descriptor.AtlasCacheVersion == cacheVersion && descriptor.RendererGeneration == _rendererGeneration)
		{
			return true;
		}
		if (!data.HasClip(clip))
		{
			return false;
		}
		int num = _renderer.RegisterDefinition(data, clip, 10, preserveTerminalAlias: true);
		if (num < 0)
		{
			return false;
		}
		AnimateMultiMeshRenderer.Definition definition = _renderer.GetDefinition(num);
		if (definition == null || definition.frameMax <= 0 || definition.frameRate <= 0.0)
		{
			return false;
		}
		Vector2I clip2 = data.GetClip(clip);
		int num2 = clip2.Y - clip2.X;
		if (num2 <= 1)
		{
			return false;
		}
		if (!descriptorWasTrusted)
		{
			descriptor = new PlaybackDescriptor();
		}
		else if (descriptor == null)
		{
			if (_playbackDescriptors.Count >= 256)
			{
				descriptorWasTrusted = false;
				descriptor = new PlaybackDescriptor();
			}
			else
			{
				descriptor = new PlaybackDescriptor();
				_playbackDescriptors.Add(key, descriptor);
			}
		}
		descriptor.DefinitionId = num;
		descriptor.FrameMax = definition.frameMax;
		descriptor.FrameRate = (float)definition.frameRate;
		descriptor.PlaybackFrameCount = num2;
		descriptor.ClipStart = clip2.X;
		descriptor.ClipEndExclusive = clip2.Y;
		descriptor.DataVersion = playbackDataStamp?.Version ?? 0;
		descriptor.AtlasCacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		descriptor.RendererGeneration = _rendererGeneration;
		return true;
	}

	private PlaybackDataStamp GetOrCreatePlaybackDataStamp(AdobeAnimateData data)
	{
		if (_playbackDataStamps.TryGetValue(data, out var value))
		{
			return value;
		}
		string text = data?.ResourcePath ?? "";
		if ((!text.StartsWith("res://Asset/Anime/", StringComparison.Ordinal) && !text.StartsWith("res://Prefab/Particles/Splats/", StringComparison.Ordinal)) || !text.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) || ResourceLoader.Load<AdobeAnimateData>(text, "", ResourceLoader.CacheMode.Reuse) != data)
		{
			return null;
		}
		if (_playbackDataStamps.Count >= 256)
		{
			return null;
		}
		value = new PlaybackDataStamp
		{
			Data = data,
			Clips = data.clips,
			ClipsVariant = data.clips,
			FrameRate = data.frameRate,
			FrameMax = data.frameMax,
			LastValidationFrame = Engine.GetProcessFrames(),
			NextNativeAuditFrame = Engine.GetProcessFrames() + 1 + data.GetInstanceId() % 256
		};
		value.ClipsHash = GD.Hash(value.ClipsVariant);
		data.Changed += value.OnDataChanged;
		_playbackDataStamps.Add(data, value);
		return value;
	}

	private void ValidatePlaybackDataStamp(PlaybackDataStamp stamp, ulong processFrame)
	{
		if (stamp == null || stamp.Data == null || stamp.LastValidationFrame == processFrame)
		{
			return;
		}
		AdobeAnimateData data = stamp.Data;
		if (stamp.DefinitionInvalidatedVersion != stamp.Version)
		{
			RefreshPlaybackDataFingerprint(stamp, data);
			InvalidatePlaybackDataDefinition(stamp, data);
		}
		if (processFrame >= stamp.NextNativeAuditFrame)
		{
			stamp.NextNativeAuditFrame = processFrame + 256;
			Dictionary clips = data.clips;
			bool num = stamp.Clips != clips;
			if (num)
			{
				stamp.Clips = clips;
				stamp.ClipsVariant = clips;
			}
			int num2 = GD.Hash(stamp.ClipsVariant);
			if (num || num2 != stamp.ClipsHash || data.frameRate != stamp.FrameRate || data.frameMax != stamp.FrameMax)
			{
				stamp.OnDataChanged();
				stamp.ClipsHash = num2;
				stamp.FrameRate = data.frameRate;
				stamp.FrameMax = data.frameMax;
			}
			if (stamp.DefinitionInvalidatedVersion != stamp.Version)
			{
				InvalidatePlaybackDataDefinition(stamp, data);
			}
		}
		stamp.LastValidationFrame = processFrame;
	}

	private void ValidatePlaybackDataStampsForFrame(ulong processFrame)
	{
		foreach (PlaybackDataStamp value in _playbackDataStamps.Values)
		{
			ValidatePlaybackDataStamp(value, processFrame);
		}
	}

	private void InvalidatePlaybackDataDefinition(PlaybackDataStamp stamp, AdobeAnimateData data)
	{
		AdobeAnimateDefinitionCache.Invalidate(data);
		stamp.DefinitionInvalidatedVersion = stamp.Version;
		RecreateRendererAfterDefinitionInvalidation();
	}

	private static void RefreshPlaybackDataFingerprint(PlaybackDataStamp stamp, AdobeAnimateData data)
	{
		stamp.ClipsVariant = (stamp.Clips = data.clips);
		stamp.ClipsHash = GD.Hash(stamp.ClipsVariant);
		stamp.FrameRate = data.frameRate;
		stamp.FrameMax = data.frameMax;
	}

	private void RecreateRendererAfterDefinitionInvalidation()
	{
		if (_tearingDown)
		{
			return;
		}
		ClearNodeFreeEntries();
		AnimateMultiMeshRenderer renderer = _renderer;
		_renderer = null;
		if (GodotObject.IsInstanceValid(renderer))
		{
			if (renderer.GetParent() == this)
			{
				RemoveChild(renderer);
			}
			renderer.QueueFree();
		}
		_rendererGeneration++;
		if (_rendererGeneration <= 0)
		{
			_rendererGeneration = 1;
		}
		_playbackDescriptors.Clear();
		_playbackPlans.Clear();
		InstallRenderer(new AnimateMultiMeshRenderer
		{
			Name = "EffectSpriteOnceAnimateMultiMeshRenderer"
		});
	}

	private PlaybackPlan GetOrCreatePlaybackPlan(AdobeAnimateData data, string requestedClip, PlaybackDataStamp stamp, ulong processFrame)
	{
		ValidatePlaybackDataStamp(stamp, processFrame);
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		PlaybackDescriptorKey key = new PlaybackDescriptorKey(data, requestedClip);
		if (_playbackPlans.TryGetValue(key, out var value) && value.DataVersion == stamp.Version && value.AtlasCacheVersion == cacheVersion && value.RendererGeneration == _rendererGeneration)
		{
			return value;
		}
		if (value == null && _playbackPlans.Count >= 256)
		{
			return null;
		}
		ClipChoices orCreateClipChoices = GetOrCreateClipChoices(requestedClip);
		if (orCreateClipChoices == null || orCreateClipChoices.Parts.Length == 0)
		{
			return null;
		}
		PlaybackDescriptor[] array = new PlaybackDescriptor[orCreateClipChoices.Parts.Length];
		for (int i = 0; i < orCreateClipChoices.Parts.Length; i++)
		{
			if (!TryResolvePlaybackDescriptor(data, orCreateClipChoices.Parts[i], trustedBuiltInPair: true, out var descriptor, out var descriptorWasTrusted) || !descriptorWasTrusted)
			{
				return null;
			}
			array[i] = descriptor;
		}
		if (value == null)
		{
			value = new PlaybackPlan();
		}
		value.Choices = orCreateClipChoices;
		value.Descriptors = array;
		value.DataVersion = stamp.Version;
		value.AtlasCacheVersion = cacheVersion;
		value.RendererGeneration = _rendererGeneration;
		_playbackPlans[key] = value;
		return value;
	}

	private ClipChoices GetOrCreateClipChoices(string clip)
	{
		if (_clipChoiceCache.TryGetValue(clip, out var value))
		{
			return value;
		}
		value = ((clip.IndexOf('&') < 0) ? new ClipChoices(new string[1] { clip }) : new ClipChoices(clip.Split('&', StringSplitOptions.RemoveEmptyEntries)));
		if (_clipChoiceCache.Count < 256)
		{
			_clipChoiceCache.Add(clip, value);
		}
		return value;
	}

	private void AdvanceAnimationClock(double delta)
	{
		_animationTime += delta;
		if (!(_animationTime > -1024.0) || !(_animationTime < 1024.0))
		{
			double animationTime = _animationTime;
			for (Entry entry = _head; entry != null; entry = entry.Next)
			{
				entry.PhaseFrame += (float)(animationTime * (double)entry.FrameRate);
			}
			for (int i = 0; i < _nodeFreeActiveCount; i++)
			{
				int num = _nodeFreeActiveIndices[i];
				_nodeFreeEntries[num].PhaseFrame += (float)(animationTime * (double)_nodeFreeEntries[num].FrameRate);
				_nodeFreeEntries[num].DueTime -= animationTime;
			}
			RebuildNodeFreeDueQueue();
			_animationTime = 0.0;
			_publicationDirty = true;
		}
	}

	private void PublishOrRetainFrame(ulong processFrame, bool rebuildPublication)
	{
		rebuildPublication |= _publicationDirty;
		if (!rebuildPublication && _hasRetainedPublication && _renderer.TryUpdateRetainedSimpleAnimationFrame(_retainedPublication.Generation, _retainedPublication.PublishedInstanceCount, _retainedPublication.PublishedBucketCount))
		{
			_lastDrawSubmissionCount = _entryCount + _nodeFreeActiveCount;
			return;
		}
		_renderer.BeginFrame();
		_hadBuckets = true;
		_lastDrawSubmissionCount = 0;
		for (Entry entry = _tail; entry != null; entry = entry.Previous)
		{
			if (DrawEntry(entry, processFrame))
			{
				_lastDrawSubmissionCount++;
			}
			entry.PublishedRenderRevision = entry.RenderStateRevision;
		}
		for (int i = 0; i < _nodeFreeActiveCount; i++)
		{
			int num = _nodeFreeActiveIndices[i];
			if (DrawNodeFreeEntry(ref _nodeFreeEntries[num]))
			{
				_lastDrawSubmissionCount++;
			}
		}
		int num2 = _entryCount + _nodeFreeActiveCount;
		AnimateMultiMeshRenderer.FramePublicationResult framePublicationResult = _renderer.EndFrame(num2);
		_hasRetainedPublication = _lastDrawSubmissionCount == num2 && framePublicationResult.IsComplete;
		_retainedPublication = (_hasRetainedPublication ? framePublicationResult : default(AnimateMultiMeshRenderer.FramePublicationResult));
		_publicationDirty = !_hasRetainedPublication;
	}

	private void HideRetainedPublication()
	{
		if (_hadBuckets)
		{
			_renderer.BeginFrame();
			_renderer.EndFrame(0);
		}
		_hadBuckets = false;
		_hasRetainedPublication = false;
		_publicationDirty = true;
		_lastDrawSubmissionCount = 0;
		_retainedPublication = default(AnimateMultiMeshRenderer.FramePublicationResult);
	}

	private bool EnsureEntryRendererCurrent(Entry entry)
	{
		if (entry.RendererGeneration == _rendererGeneration)
		{
			return true;
		}
		bool flag = HasCachedTrustedBuiltInNodePair(entry.Effect, entry.Sprite, entry.Data) || IsTrustedBuiltInNodePair(entry.Effect, entry.Sprite);
		if (entry.Data == null || string.IsNullOrEmpty(entry.Clip) || !TryResolvePlaybackDescriptor(entry.Data, entry.Clip, flag, out var descriptor, out var _))
		{
			return false;
		}
		entry.DefId = descriptor.DefinitionId;
		entry.RendererGeneration = descriptor.RendererGeneration;
		float effectOnceGpuBatchElapsedFrame = entry.Sprite.GetEffectOnceGpuBatchElapsedFrame();
		entry.FrameRate = descriptor.FrameRate;
		entry.PhaseFrame = effectOnceGpuBatchElapsedFrame - (float)(_animationTime * (double)entry.FrameRate);
		entry.PlaybackFrameCount = descriptor.PlaybackFrameCount;
		entry.ClipStart = descriptor.ClipStart;
		entry.ClipEndExclusive = descriptor.ClipEndExclusive;
		_publicationDirty = true;
		string requestedClip = entry.Eligibility?.RequestedClip;
		CacheEligibility(entry.Effect, entry.Sprite, entry.Data, requestedClip, flag);
		if (!_eligibilityBySprite.TryGetValue(entry.Sprite, out var value) || value.Effect != entry.Effect || value.RendererGeneration != _rendererGeneration)
		{
			return false;
		}
		value.ActiveEntry = entry;
		entry.Eligibility = value;
		return true;
	}

	private bool IsActivePreparedEntryCurrent(Entry entry, ulong processFrame, int atlasCacheVersion, int rendererGeneration)
	{
		EligibilitySnapshot eligibility = entry.Eligibility;
		if (eligibility == null || eligibility.ActiveEntry != entry || !IsSnapshotEligibilityCurrent(eligibility, entry.Effect, entry.Sprite, entry.Data, processFrame))
		{
			return false;
		}
		if (eligibility.DataStamp != null && entry.SuppressionToken != 0 && entry.SuppressionToken == entry.Sprite.GetEffectOnceGpuBatchSuppressionToken() && eligibility.DataVersion == eligibility.DataStamp.Version && eligibility.AtlasCacheVersion == atlasCacheVersion && eligibility.RendererGeneration == rendererGeneration && eligibility.Choices != null && eligibility.Descriptors != null && eligibility.Descriptors.Length == eligibility.Choices.Parts.Length)
		{
			return eligibility.Descriptors.Length != 0;
		}
		return false;
	}

	private void ValidateNodeFreeEntriesForFrame(int atlasCacheVersion, int rendererGeneration)
	{
		if (_nodeFreeActiveCount == 0)
		{
			return;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			characterNode = TowerDefenseGroundItemBase.characterNode;
		}
		if (!GodotObject.IsInstanceValid(characterNode) || GetParent() != characterNode)
		{
			ClearNodeFreeEntries();
			return;
		}
		for (int num = _nodeFreeActiveCount - 1; num >= 0; num--)
		{
			int num2 = _nodeFreeActiveIndices[num];
			ref NodeFreeEntry reference = ref _nodeFreeEntries[num2];
			NodeFreeTemplate template = reference.Template;
			PlaybackDataStamp playbackDataStamp = template?.DataStamp;
			if (template == null || !GodotObject.IsInstanceValid(template.Scene) || !GodotObject.IsInstanceValid(template.Data) || template.Revision != reference.TemplateRevision || playbackDataStamp == null || playbackDataStamp.Version != reference.DataVersion || reference.RendererGeneration != rendererGeneration || reference.AtlasCacheVersion != atlasCacheVersion)
			{
				RemoveNodeFreeAt(num2);
			}
		}
	}

	private void ProcessNodeFreeDueEntries(int atlasCacheVersion, int rendererGeneration)
	{
		int groupIndex;
		double dueTime;
		while (TryPeekNodeFreeDueGroup(out groupIndex, out dueTime) && dueTime <= _animationTime + 1E-07)
		{
			uint nodeFreeDueResetVersion = _nodeFreeDueResetVersion;
			PopNodeFreeDueGroup();
			ref NodeFreeDueGroup reference = ref _nodeFreeDueGroups[groupIndex];
			RemoveNodeFreeDueGroupLookup(groupIndex, reference.DueTime);
			reference.Processing = true;
			while (reference.HeadIndex >= 0)
			{
				int index = reference.HeadIndex;
				uint generation = _nodeFreeGenerations[index];
				double dueTime2 = _nodeFreeEntries[index].DueTime;
				UnscheduleNodeFreeDue(index);
				if (nodeFreeDueResetVersion != _nodeFreeDueResetVersion)
				{
					return;
				}
				if (!TryGetNodeFreeIndex(index, generation, out index))
				{
					continue;
				}
				ref NodeFreeEntry reference2 = ref _nodeFreeEntries[index];
				PlaybackDataStamp dataStamp = reference2.Template.DataStamp;
				if (dataStamp == null || reference2.Template.Revision != reference2.TemplateRevision || dataStamp.Version != reference2.DataVersion || reference2.RendererGeneration != rendererGeneration || reference2.AtlasCacheVersion != atlasCacheVersion)
				{
					RemoveNodeFreeAt(index);
					continue;
				}
				string clip = reference2.Clip;
				reference2.OnAnimeCompleted?.Invoke(clip);
				if (nodeFreeDueResetVersion != _nodeFreeDueResetVersion)
				{
					return;
				}
				if (!TryGetNodeFreeIndex(index, generation, out index))
				{
					continue;
				}
				reference2 = ref _nodeFreeEntries[index];
				int num = reference2.SequenceIndex + 1;
				if (num >= reference2.Sequence.Alternatives.Length)
				{
					RemoveNodeFreeAt(index);
					continue;
				}
				NodeFreeClipChoice nodeFreeClipChoice = ResolveNodeFreeClip(reference2.Sequence, num);
				if (nodeFreeClipChoice.RendererGeneration != rendererGeneration)
				{
					RemoveNodeFreeAt(index);
					continue;
				}
				double num2 = Math.Max(0.0, _animationTime - dueTime2);
				float frameRate = nodeFreeClipChoice.FrameRate;
				if (!float.IsFinite(frameRate) || frameRate <= 0.0001f)
				{
					RemoveNodeFreeAt(index);
					continue;
				}
				reference2.SequenceIndex = num;
				reference2.Clip = nodeFreeClipChoice.Clip;
				reference2.DefinitionId = nodeFreeClipChoice.DefinitionId;
				reference2.RendererGeneration = nodeFreeClipChoice.RendererGeneration;
				reference2.AtlasCacheVersion = atlasCacheVersion;
				reference2.PlaybackFrameCount = nodeFreeClipChoice.PlaybackFrameCount;
				reference2.FrameRate = frameRate;
				reference2.PhaseFrame = (float)(num2 * (double)frameRate - _animationTime * (double)frameRate);
				reference2.DrawContext = default(AnimateMultiMeshRenderer.SimpleDrawContext);
				reference2.DrawContextDefId = -1;
				reference2.DrawContextRendererGeneration = 0;
				reference2.DueTime = _animationTime + Math.Max(1E-06, nodeFreeClipChoice.DurationSeconds - num2);
				ScheduleNodeFreeDue(index);
				_publicationDirty = true;
			}
			if (nodeFreeDueResetVersion != _nodeFreeDueResetVersion)
			{
				break;
			}
			reference.Processing = false;
			ReleaseNodeFreeDueGroup(groupIndex);
		}
	}

	private bool DrawNodeFreeEntry(ref NodeFreeEntry entry)
	{
		AnimateMultiMeshRenderer.SimpleDrawContext context = entry.DrawContext;
		bool flag = entry.DrawContextRendererGeneration == _rendererGeneration && entry.DrawContextDefId == entry.DefinitionId && entry.DrawContextZIndex == entry.ZIndex && _renderer.TouchPreparedSimpleDrawContext(in context);
		AnimateMultiMeshRenderer.Definition definition = null;
		if (!flag)
		{
			definition = _renderer.GetDefinition(entry.DefinitionId);
			int num = 15;
			flag = definition != null && num > 0 && (entry.ZIndex - definition.layer) % num == 0 && _renderer.TryPrepareSimpleDrawContext(entry.DefinitionId, (entry.ZIndex - definition.layer) / num, 1, out context);
			if (flag)
			{
				entry.DrawContext = context;
				entry.DrawContextDefId = entry.DefinitionId;
				entry.DrawContextZIndex = entry.ZIndex;
				entry.DrawContextRendererGeneration = _rendererGeneration;
			}
		}
		if (flag && _renderer.DrawPreparedSimpleInstance(in context, entry.Transform, entry.PhaseFrame, entry.FrameRate))
		{
			return true;
		}
		if (definition == null)
		{
			definition = context.Definition;
		}
		if (definition != null)
		{
			return _renderer.DrawInstanceAtZIndex(entry.DefinitionId, entry.ZIndex, entry.Transform, (float)Math.Clamp((double)entry.PhaseFrame + _animationTime * (double)entry.FrameRate, 0.0, Math.Max(0, entry.PlaybackFrameCount - 1)), Colors.White);
		}
		return false;
	}

	private bool DrawEntry(Entry entry, ulong processFrame)
	{
		if (!entry.RenderStateReady)
		{
			int effectOnceBatchRenderRevision = entry.Sprite.GetEffectOnceBatchRenderRevision(processFrame);
			entry.Sprite.GetEffectOnceBatchRenderState(processFrame, out entry.RenderTransform, out entry.RenderZIndex);
			entry.RenderStateRevision = effectOnceBatchRenderRevision;
			entry.RenderStateReady = true;
		}
		Transform2D renderTransform = entry.RenderTransform;
		int renderZIndex = entry.RenderZIndex;
		AnimateMultiMeshRenderer.SimpleDrawContext context = entry.DrawContext;
		bool flag = entry.DrawContextRendererGeneration == _rendererGeneration && entry.DrawContextDefId == entry.DefId && entry.DrawContextZIndex == renderZIndex;
		bool flag2 = flag && _renderer.TouchPreparedSimpleDrawContext(in context);
		if (!flag2 && entry.AlternateDrawContextRendererGeneration == _rendererGeneration && entry.AlternateDrawContextDefId == entry.DefId && entry.AlternateDrawContextZIndex == renderZIndex)
		{
			context = entry.AlternateDrawContext;
			flag2 = _renderer.TouchPreparedSimpleDrawContext(in context);
		}
		AnimateMultiMeshRenderer.Definition definition = null;
		if (!flag2)
		{
			definition = _renderer.GetDefinition(entry.DefId);
			int num = 15;
			flag2 = definition != null && num > 0 && (renderZIndex - definition.layer) % num == 0 && _renderer.TryPrepareSimpleDrawContext(entry.DefId, (renderZIndex - definition.layer) / num, 1, out context);
			if (flag2)
			{
				if (flag || !entry.DrawContext.IsValid || entry.DrawContextRendererGeneration != _rendererGeneration || entry.DrawContextZIndex != renderZIndex)
				{
					bool num2 = entry.DrawContextRendererGeneration != _rendererGeneration || entry.DrawContextZIndex != renderZIndex;
					entry.DrawContext = context;
					entry.DrawContextZIndex = renderZIndex;
					entry.DrawContextDefId = entry.DefId;
					entry.DrawContextRendererGeneration = _rendererGeneration;
					if (num2)
					{
						entry.AlternateDrawContext = default(AnimateMultiMeshRenderer.SimpleDrawContext);
						entry.AlternateDrawContextDefId = -1;
						entry.AlternateDrawContextRendererGeneration = 0;
					}
				}
				else
				{
					entry.AlternateDrawContext = context;
					entry.AlternateDrawContextZIndex = renderZIndex;
					entry.AlternateDrawContextDefId = entry.DefId;
					entry.AlternateDrawContextRendererGeneration = _rendererGeneration;
				}
			}
		}
		if (flag2 && _renderer.DrawPreparedSimpleInstance(in context, renderTransform, entry.PhaseFrame, entry.FrameRate))
		{
			return true;
		}
		if (definition == null)
		{
			definition = context.Definition;
		}
		if (definition != null)
		{
			return _renderer.DrawInstanceAtZIndex(entry.DefId, renderZIndex, renderTransform, entry.Sprite.GetEffectOnceGpuBatchElapsedFrame(), Colors.White);
		}
		return false;
	}

	public override void _ExitTree()
	{
		_tearingDown = true;
		ClearNodeFreeEntries();
		List<(TowerDefenseEffectSpriteOnce, AdobeAnimateSprite, string, float)> list = new List<(TowerDefenseEffectSpriteOnce, AdobeAnimateSprite, string, float)>(_entryCount);
		Entry entry = _head;
		while (entry != null)
		{
			Entry next = entry.Next;
			if (GodotObject.IsInstanceValid(entry.Effect) && GodotObject.IsInstanceValid(entry.Sprite))
			{
				list.Add((entry.Effect, entry.Sprite, entry.Clip, entry.Sprite.GetEffectOnceGpuBatchElapsedFrame()));
			}
			DetachEntry(entry);
			RecycleDetachedEntry(entry);
			entry = next;
		}
		foreach (EligibilitySnapshot value in _eligibilityBySprite.Values)
		{
			if (value.ParkedEntry != null)
			{
				RecycleDetachedEntry(value.ParkedEntry);
				value.ParkedEntry = null;
			}
			UnsubscribeEligibilitySnapshot(value);
		}
		_eligibilityBySprite.Clear();
		for (int i = 0; i < list.Count; i++)
		{
			(TowerDefenseEffectSpriteOnce, AdobeAnimateSprite, string, float) tuple = list[i];
			if (GodotObject.IsInstanceValid(tuple.Item1) && GodotObject.IsInstanceValid(tuple.Item2))
			{
				tuple.Item1.AbortGpuBatchAndResumeCpu(tuple.Item3, tuple.Item4);
			}
		}
		_head = null;
		_tail = null;
		_freeEntries = null;
		_entryCount = 0;
		_freeEntryCount = 0;
		_createdEntryCount = 0;
		_nextEntryGeneration = 0u;
		_nextEntryOrder = 0uL;
		_entryByEffect.Clear();
		_clipChoiceCache.Clear();
		_playbackDescriptors.Clear();
		_playbackPlans.Clear();
		foreach (PlaybackDataStamp value2 in _playbackDataStamps.Values)
		{
			if (GodotObject.IsInstanceValid(value2.Data))
			{
				value2.Data.Changed -= value2.OnDataChanged;
			}
		}
		_playbackDataStamps.Clear();
		foreach (NodeFreeTemplate value3 in _nodeFreeTemplates.Values)
		{
			if (GodotObject.IsInstanceValid(value3.Scene))
			{
				value3.Scene.Changed -= value3.OnSceneChanged;
			}
		}
		_nodeFreeTemplates.Clear();
		_nodeFreeRejectedScenes.Clear();
		System.Array.Clear(_nodeFreeRecentTemplates);
		_nodeFreeRecentTemplateCursor = 0;
		_nodeFreeDueGroupByTime.Clear();
		_nodeFreeDueGroupHeapCount = 0;
		_nodeFreeDueGroupFreeCount = 0;
		_nodeFreeDueResetVersion++;
		_nodeFreeFreeCount = 0;
		_nodeFreeActiveCount = 0;
		_nodeFreeStorageInitialized = false;
		_animationTime = 0.0;
		_publicationDirty = true;
		_hasRetainedPublication = false;
		_retainedPublication = default(AnimateMultiMeshRenderer.FramePublicationResult);
		_hadBuckets = false;
		_lastDrawSubmissionCount = 0;
		_renderer = null;
		_nodeFreeMount = null;
		if (_instance == this)
		{
			_instance = null;
		}
	}

	public static TowerDefenseEffectSpriteOnceBatcher GetOrCreate()
	{
		TowerDefenseEffectSpriteOnceBatcher instance = _instance;
		Node2D node2D = TowerDefenseManager.CurrentControl?.characterNode ?? TowerDefenseGroundItemBase.characterNode;
		if (instance != null && !instance._tearingDown && instance._nodeFreeMount == node2D)
		{
			return instance;
		}
		Node2D characterNode = TowerDefenseManager.GetCharacterNode();
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			characterNode = TowerDefenseGroundItemBase.characterNode;
		}
		if (!GodotObject.IsInstanceValid(characterNode))
		{
			return null;
		}
		if (GodotObject.IsInstanceValid(_instance) && _instance.GetParent() == characterNode)
		{
			return _instance;
		}
		if (GodotObject.IsInstanceValid(_instance))
		{
			_instance.QueueFree();
		}
		_instance = new TowerDefenseEffectSpriteOnceBatcher
		{
			Name = "TowerDefenseEffectSpriteOnceBatcher",
			_nodeFreeMount = characterNode
		};
		characterNode.AddChild(_instance, forceReadableName: false, InternalMode.Disabled);
		return _instance;
	}

	internal static TowerDefenseEffectSpriteOnceBatcher GetOrCreateNodeFreeFast()
	{
		TowerDefenseEffectSpriteOnceBatcher instance = _instance;
		if (instance == null || instance._tearingDown)
		{
			return GetOrCreate();
		}
		return instance;
	}

	public static void UnregisterIfExists(TowerDefenseEffectSpriteOnce effect)
	{
		if (GodotObject.IsInstanceValid(_instance))
		{
			_instance.Unregister(effect);
		}
	}

	public override void _Ready()
	{
		InstallRenderer(new AnimateMultiMeshRenderer
		{
			Name = "EffectSpriteOnceAnimateMultiMeshRenderer"
		});
		_rendererGeneration = 1;
	}

	public bool RegisterOrRestart(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, string clip)
	{
		if (_tearingDown || !GodotObject.IsInstanceValid(effect) || !GodotObject.IsInstanceValid(sprite) || string.IsNullOrEmpty(clip))
		{
			return false;
		}
		AdobeAnimateData flashAnimeData = sprite.flashAnimeData;
		if (!RetireConflictingRegistration(effect, sprite, flashAnimeData))
		{
			return false;
		}
		if (_renderer == null)
		{
			EnsureRenderer();
		}
		if (TryGetPreparedDescriptor(effect, sprite, flashAnimeData, clip, out var resolvedClip, out var descriptor, out var preparedSnapshot))
		{
			if (!sprite.IsEffectOnceGpuBatchPlaybackPrepared(resolvedClip, descriptor.ClipStart, descriptor.ClipEndExclusive, descriptor.PlaybackFrameCount, descriptor.FrameRate) || sprite.GetEffectOnceGpuBatchSuppressionToken() == 0 || !IsBatchMountCompatible(effect, sprite))
			{
				return false;
			}
			return ApplyRegistration(effect, sprite, flashAnimeData, resolvedClip, descriptor, preparedSnapshot);
		}
		if (!IsCachedEligibilityCurrent(effect, sprite, flashAnimeData) && !CanBatchEffectSprite(effect, sprite))
		{
			return false;
		}
		bool flag = HasCachedTrustedBuiltInNodePair(effect, sprite, flashAnimeData) || IsTrustedBuiltInNodePair(effect, sprite);
		if (!flag)
		{
			return false;
		}
		if (!TryResolvePlaybackDescriptor(flashAnimeData, clip, flag, out var descriptor2, out var descriptorWasTrusted))
		{
			return false;
		}
		if (!descriptorWasTrusted)
		{
			return false;
		}
		if (!sprite.IsEffectOnceGpuBatchPlaybackPrepared(clip, descriptor2.ClipStart, descriptor2.ClipEndExclusive, descriptor2.PlaybackFrameCount, descriptor2.FrameRate) || sprite.GetEffectOnceGpuBatchSuppressionToken() == 0 || !IsBatchMountCompatible(effect, sprite))
		{
			return false;
		}
		EligibilitySnapshot value = null;
		CacheEligibility(effect, sprite, flashAnimeData, clip, flag);
		_eligibilityBySprite.TryGetValue(sprite, out value);
		if (value == null || value.Effect != effect || value.Sprite != sprite || value.Data != flashAnimeData)
		{
			return false;
		}
		return ApplyRegistration(effect, sprite, flashAnimeData, clip, descriptor2, value);
	}

	public bool TryRegisterNodeFree(PackedScene scene, Vector2I gridPosition, string clips, Transform2D transform, Action<string> onAnimeCompleted, out TowerDefenseEffectSpriteOnceGpuHandle handle)
	{
		return TryRegisterNodeFreeCore(scene, gridPosition, clips, transform, transform == Transform2D.Identity, onAnimeCompleted, out handle);
	}

	internal bool TryRegisterNodeFreeIdentity(PackedScene scene, Vector2I gridPosition, string clips, Action<string> onAnimeCompleted, out TowerDefenseEffectSpriteOnceGpuHandle handle)
	{
		return TryRegisterNodeFreeCore(scene, gridPosition, clips, Transform2D.Identity, transformIsIdentity: true, onAnimeCompleted, out handle);
	}

	private bool TryRegisterNodeFreeCore(PackedScene scene, Vector2I gridPosition, string clips, Transform2D transform, bool transformIsIdentity, Action<string> onAnimeCompleted, out TowerDefenseEffectSpriteOnceGpuHandle handle)
	{
		handle = default(TowerDefenseEffectSpriteOnceGpuHandle);
		if (_tearingDown || scene == null || (!transformIsIdentity && !transform.IsFinite()) || !TryGetOrBuildNodeFreeTemplate(scene, out var template))
		{
			return false;
		}
		NodeFreeSequence orBuildNodeFreeSequence = GetOrBuildNodeFreeSequence(template, clips);
		if (orBuildNodeFreeSequence == null || orBuildNodeFreeSequence.Alternatives == null || orBuildNodeFreeSequence.Alternatives.Length == 0)
		{
			return false;
		}
		NodeFreeClipChoice nodeFreeClipChoice = ResolveNodeFreeClip(orBuildNodeFreeSequence, 0);
		EnsureNodeFreeStorage();
		if (_nodeFreeFreeCount <= 0)
		{
			return false;
		}
		bool flag = _entryCount == 0 && _nodeFreeActiveCount == 0;
		int num = _nodeFreeFreeList[--_nodeFreeFreeCount];
		uint num2 = _nodeFreeGenerations[num];
		if (num2 == 0)
		{
			num2 = 1u;
			_nodeFreeGenerations[num] = num2;
		}
		float frameRate = nodeFreeClipChoice.FrameRate;
		if (!float.IsFinite(frameRate) || frameRate <= 0.0001f)
		{
			_nodeFreeFreeList[_nodeFreeFreeCount++] = num;
			return false;
		}
		int num3 = ComputeNodeFreeZIndex(template, gridPosition.Y);
		double num4 = _animationTime + nodeFreeClipChoice.DurationSeconds;
		_nodeFreeEntries[num] = new NodeFreeEntry
		{
			Template = template,
			Sequence = orBuildNodeFreeSequence,
			OnAnimeCompleted = onAnimeCompleted,
			Transform = (transformIsIdentity ? template.DrawTransform : (transform * template.DrawTransform)),
			DrawContextDefId = -1,
			DrawContextZIndex = num3,
			ZIndex = num3,
			DefinitionId = nodeFreeClipChoice.DefinitionId,
			RendererGeneration = nodeFreeClipChoice.RendererGeneration,
			SequenceIndex = 0,
			PlaybackFrameCount = nodeFreeClipChoice.PlaybackFrameCount,
			DataVersion = orBuildNodeFreeSequence.DataVersion,
			AtlasCacheVersion = orBuildNodeFreeSequence.AtlasCacheVersion,
			TemplateRevision = template.Revision,
			PhaseFrame = (float)((0.0 - _animationTime) * (double)frameRate),
			FrameRate = frameRate,
			Clip = nodeFreeClipChoice.Clip,
			DueTime = num4,
			DueGroupIndex = -1,
			DuePreviousIndex = -1,
			DueNextIndex = -1,
			Active = true
		};
		_nodeFreeActiveSlots[num] = _nodeFreeActiveCount;
		_nodeFreeActiveIndices[_nodeFreeActiveCount++] = num;
		ScheduleNodeFreeDue(num, (orBuildNodeFreeSequence.CachedDueTime == num4) ? orBuildNodeFreeSequence.CachedDueGroupIndex : (-1), orBuildNodeFreeSequence.CachedDueGroupGeneration, out var scheduledGroupIndex, out var scheduledGroupGeneration);
		orBuildNodeFreeSequence.CachedDueTime = num4;
		orBuildNodeFreeSequence.CachedDueGroupIndex = scheduledGroupIndex;
		orBuildNodeFreeSequence.CachedDueGroupGeneration = scheduledGroupGeneration;
		_publicationDirty = true;
		if (flag)
		{
			SetProcess(enable: true);
		}
		handle = new TowerDefenseEffectSpriteOnceGpuHandle(this, num, num2);
		return true;
	}

	internal bool IsNodeFreeHandleValid(TowerDefenseEffectSpriteOnceGpuHandle handle)
	{
		int index;
		if (handle.Owner == this)
		{
			return TryGetNodeFreeIndex(handle.Index, handle.Generation, out index);
		}
		return false;
	}

	internal bool SetNodeFreeTransform(TowerDefenseEffectSpriteOnceGpuHandle handle, Transform2D transform)
	{
		if (!transform.IsFinite() || handle.Owner != this || !TryGetNodeFreeIndex(handle.Index, handle.Generation, out var index))
		{
			return false;
		}
		ref NodeFreeEntry reference = ref _nodeFreeEntries[index];
		reference.Transform = transform * reference.Template.LocalTransform.TranslatedLocal(reference.Template.Offset);
		_publicationDirty = true;
		return true;
	}

	internal bool SetNodeFreeZIndex(TowerDefenseEffectSpriteOnceGpuHandle handle, int zIndex)
	{
		if (handle.Owner != this || !TryGetNodeFreeIndex(handle.Index, handle.Generation, out var index))
		{
			return false;
		}
		ref NodeFreeEntry reference = ref _nodeFreeEntries[index];
		reference.ZIndex = Math.Clamp(zIndex, -4096, 4096);
		reference.DrawContext = default(AnimateMultiMeshRenderer.SimpleDrawContext);
		reference.DrawContextDefId = -1;
		reference.DrawContextRendererGeneration = 0;
		_publicationDirty = true;
		return true;
	}

	internal bool RemoveNodeFree(TowerDefenseEffectSpriteOnceGpuHandle handle)
	{
		if (handle.Owner != this || !TryGetNodeFreeIndex(handle.Index, handle.Generation, out var index))
		{
			return false;
		}
		RemoveNodeFreeAt(index);
		return true;
	}

	private bool RetireConflictingRegistration(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data)
	{
		if (_entryByEffect.TryGetValue(effect, out var value))
		{
			bool num = value.IsLinked && value.Sprite == sprite && value.Data == data;
			EligibilitySnapshot value2;
			bool flag = !_eligibilityBySprite.TryGetValue(sprite, out value2) || value.Eligibility == value2;
			if (!num || !flag)
			{
				DetachEntryAndResumeCpu(value);
			}
		}
		if (_eligibilityBySprite.TryGetValue(sprite, out var value3) && value3.ActiveEntry != null && value3.ActiveEntry.Effect != effect)
		{
			DetachEntryAndResumeCpu(value3.ActiveEntry);
		}
		if (GodotObject.IsInstanceValid(effect) && GodotObject.IsInstanceValid(sprite) && effect.sprite == sprite && sprite.flashAnimeData == data && sprite.GetEffectOnceGpuBatchSuppressionToken() != 0)
		{
			if (_entryByEffect.TryGetValue(effect, out var value4))
			{
				if (value4.IsLinked && value4.Sprite == sprite)
				{
					return value4.Data == data;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	private bool ApplyRegistration(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite, AdobeAnimateData data, string resolvedClip, PlaybackDescriptor descriptor, EligibilitySnapshot preparedSnapshot)
	{
		Entry value;
		bool flag = _entryByEffect.TryGetValue(effect, out value);
		if (flag && (!value.IsLinked || value.Sprite != sprite || value.Data != data || value.Eligibility != preparedSnapshot))
		{
			return false;
		}
		if (!flag && preparedSnapshot?.ActiveEntry != null)
		{
			return false;
		}
		if (!flag)
		{
			value = preparedSnapshot?.ParkedEntry;
			if (value != null)
			{
				preparedSnapshot.ParkedEntry = null;
				value.LinkGeneration = NextEntryGeneration();
			}
			else
			{
				value = RentEntry();
			}
		}
		value.Effect = effect;
		value.Sprite = sprite;
		value.Data = data;
		value.Eligibility = preparedSnapshot;
		value.DefId = descriptor.DefinitionId;
		value.RendererGeneration = descriptor.RendererGeneration;
		value.Clip = resolvedClip;
		float effectOnceGpuBatchElapsedFrame = sprite.GetEffectOnceGpuBatchElapsedFrame();
		value.FrameRate = descriptor.FrameRate;
		value.PhaseFrame = effectOnceGpuBatchElapsedFrame - (float)(_animationTime * (double)value.FrameRate);
		value.PlaybackFrameCount = descriptor.PlaybackFrameCount;
		value.ClipStart = descriptor.ClipStart;
		value.ClipEndExclusive = descriptor.ClipEndExclusive;
		value.PlaybackRevision = sprite.GetEffectOnceBatchPlaybackRevision();
		value.SuppressionToken = sprite.GetEffectOnceGpuBatchSuppressionToken();
		if (flag)
		{
			MoveEntryToTail(value);
		}
		else
		{
			AppendEntry(value);
			_entryByEffect.Add(effect, value);
		}
		if (preparedSnapshot != null)
		{
			preparedSnapshot.ActiveEntry = value;
		}
		return true;
	}

	public void Unregister(TowerDefenseEffectSpriteOnce effect)
	{
		if (effect != null && _entryByEffect.Remove(effect, out var value))
		{
			EligibilitySnapshot eligibility = value.Eligibility;
			if (eligibility != null && eligibility.Effect == effect && eligibility.ParkedEntry == null)
			{
				DetachEntry(value, dictionaryAlreadyRemoved: true);
				eligibility.ActiveEntry = null;
				eligibility.ParkedEntry = value;
			}
			else
			{
				DetachEntry(value, dictionaryAlreadyRemoved: true);
				RecycleDetachedEntry(value);
			}
		}
	}

	public override void _Process(double delta)
	{
		ulong processFrames = Engine.GetProcessFrames();
		EnsureRenderer();
		ValidatePlaybackDataStampsForFrame(processFrames);
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		int rendererGeneration = _rendererGeneration;
		ValidateNodeFreeEntriesForFrame(cacheVersion, rendererGeneration);
		AdvanceAnimationClock(delta);
		_renderer.SetAnimationTime(_animationTime);
		ProcessNodeFreeDueEntries(cacheVersion, rendererGeneration);
		if (_entryCount == 0 && _nodeFreeActiveCount == 0)
		{
			HideRetainedPublication();
			SetProcess(enable: false);
			return;
		}
		bool rebuildPublication = _publicationDirty || !_hasRetainedPublication;
		ulong orderWatermark = _tail?.Order ?? 0;
		Entry entry = _tail;
		while (entry != null)
		{
			ulong order = entry.Order;
			uint linkGeneration = entry.LinkGeneration;
			TowerDefenseEffectSpriteOnce effect = entry.Effect;
			AdobeAnimateSprite sprite = entry.Sprite;
			Entry previous = entry.Previous;
			uint candidateGeneration = previous?.LinkGeneration ?? 0;
			if (!HasTrustedActiveSnapshot(entry) && !IsEntryValid(entry))
			{
				RemoveEligibilitySnapshot(entry.Sprite);
				RemoveAndRecycleEntry(entry);
				entry = previous;
				continue;
			}
			if (!IsActivePreparedEntryCurrent(entry, processFrames, cacheVersion, rendererGeneration))
			{
				DetachEntryAndResumeCpu(entry);
				entry = ResolveTraversalEntryAfterCallback(previous, candidateGeneration, orderWatermark, order);
				continue;
			}
			if (!EnsureEntryRendererCurrent(entry))
			{
				DetachEntryAndResumeCpu(entry);
				entry = ResolveTraversalEntryAfterCallback(previous, candidateGeneration, orderWatermark, order);
				continue;
			}
			EligibilitySnapshot eligibility = entry.Eligibility;
			bool flag = processFrames >= eligibility.NextNativeRenderAuditFrame;
			if (!entry.RenderStateReady || eligibility.RenderStateInvalidated || flag)
			{
				if (flag)
				{
					eligibility.NextNativeRenderAuditFrame = processFrames + 256;
				}
				int effectOnceBatchRenderRevision = entry.Sprite.GetEffectOnceBatchRenderRevision(processFrames);
				if (!entry.RenderStateReady || eligibility.RenderStateInvalidated || entry.RenderStateRevision != effectOnceBatchRenderRevision)
				{
					entry.Sprite.GetEffectOnceBatchRenderState(processFrames, out entry.RenderTransform, out entry.RenderZIndex);
					entry.RenderStateRevision = effectOnceBatchRenderRevision;
					entry.RenderStateReady = true;
					eligibility.RenderStateInvalidated = false;
				}
				if (entry.PublishedRenderRevision != effectOnceBatchRenderRevision)
				{
					rebuildPublication = true;
				}
			}
			double expectedElapsedFrame = (double)entry.PhaseFrame + _animationTime * (double)entry.FrameRate - delta * (double)entry.FrameRate;
			AdobeAnimateSprite.EffectOnceGpuAdvanceResult effectOnceGpuAdvanceResult = entry.Sprite.AdvanceEffectOnceGpuPlayback(delta, entry.SuppressionToken, entry.Clip, entry.PlaybackRevision, expectedElapsedFrame, entry.ClipStart, entry.ClipEndExclusive, entry.PlaybackFrameCount, entry.FrameRate);
			switch (effectOnceGpuAdvanceResult)
			{
			case AdobeAnimateSprite.EffectOnceGpuAdvanceResult.Invalidated:
				if (entry.IsLinked && entry.LinkGeneration == linkGeneration && entry.Effect == effect && entry.Sprite == sprite)
				{
					DetachEntryAndResumeCpu(entry);
				}
				entry = ResolveTraversalEntryAfterCallback(previous, candidateGeneration, orderWatermark, order);
				break;
			default:
				if (entry.IsLinked && entry.LinkGeneration == linkGeneration && entry.Effect == effect && entry.Sprite == sprite && entry.Order == order)
				{
					entry = previous;
					break;
				}
				goto case AdobeAnimateSprite.EffectOnceGpuAdvanceResult.Completed;
			case AdobeAnimateSprite.EffectOnceGpuAdvanceResult.Completed:
				if (effectOnceGpuAdvanceResult == AdobeAnimateSprite.EffectOnceGpuAdvanceResult.Completed && entry.IsLinked && entry.LinkGeneration == linkGeneration && entry.Effect == effect && entry.Sprite == sprite)
				{
					DetachEntryAndResumeCpu(entry);
				}
				entry = ResolveTraversalEntryAfterCallback(previous, candidateGeneration, orderWatermark, order);
				break;
			}
		}
		if (_entryCount == 0 && _nodeFreeActiveCount == 0)
		{
			HideRetainedPublication();
			SetProcess(enable: false);
		}
		else
		{
			PublishOrRetainFrame(processFrames, rebuildPublication);
		}
	}

	private void EnsureRenderer()
	{
		if (!GodotObject.IsInstanceValid(_renderer))
		{
			InstallRenderer(new AnimateMultiMeshRenderer
			{
				Name = "EffectSpriteOnceAnimateMultiMeshRenderer"
			});
			_rendererGeneration++;
			if (_rendererGeneration <= 0)
			{
				_rendererGeneration = 1;
			}
		}
	}

	private void InstallRenderer(AnimateMultiMeshRenderer renderer)
	{
		_renderer = renderer;
		renderer.TopLevel = true;
		renderer.Transform = Transform2D.Identity;
		_publicationDirty = true;
		_hasRetainedPublication = false;
		AddChild(renderer, forceReadableName: false, InternalMode.Disabled);
		renderer.GlobalTransform = Transform2D.Identity;
		renderer.SetAnimationTime(_animationTime);
		renderer.TreeExiting += delegate
		{
			OnRendererTreeExiting(renderer);
		};
	}

	private void OnRendererTreeExiting(AnimateMultiMeshRenderer renderer)
	{
		if (_renderer == renderer)
		{
			_renderer = null;
			_publicationDirty = true;
			_hasRetainedPublication = false;
			_retainedPublication = default(AnimateMultiMeshRenderer.FramePublicationResult);
		}
	}

	private static bool IsTrustedBuiltInNodePair(TowerDefenseEffectSpriteOnce effect, AdobeAnimateSprite sprite)
	{
		if (effect?.GetType() == typeof(TowerDefenseEffectSpriteOnce) && sprite?.GetType() == typeof(AdobeAnimateSpriteBase) && sprite.GetParent() == effect && HasExpectedBuiltInScript(effect, "res://Prefab/TowerDefense/Effect/Once/TowerDefenseEffectSpriteOnce.cs", ref _builtInEffectScript))
		{
			return HasExpectedBuiltInScript(sprite, "res://Extends/AdobeAnimateSprite/AdobeAnimateSpriteBase.cs", ref _builtInSpriteScript);
		}
		return false;
	}

	private static bool HasExpectedBuiltInScript(GodotObject owner, string expectedPath, ref Script expectedScript)
	{
		if (!GodotObject.IsInstanceValid(owner))
		{
			return false;
		}
		Variant script = owner.GetScript();
		if (script.VariantType == Variant.Type.Nil || !(script.AsGodotObject() is Script script2))
		{
			return false;
		}
		if (!GodotObject.IsInstanceValid(expectedScript))
		{
			expectedScript = ResourceLoader.Load<Script>(expectedPath, "", ResourceLoader.CacheMode.Reuse);
		}
		if (GodotObject.IsInstanceValid(expectedScript))
		{
			return script2 == expectedScript;
		}
		return false;
	}

	private bool TryGetOrBuildNodeFreeTemplate(PackedScene scene, out NodeFreeTemplate template)
	{
		for (int i = 0; i < _nodeFreeRecentTemplates.Length; i++)
		{
			template = _nodeFreeRecentTemplates[i];
			if (template != null && template.Scene == scene)
			{
				return template.Revision != 0;
			}
		}
		if (_nodeFreeTemplates.TryGetValue(scene, out template))
		{
			CacheRecentNodeFreeTemplate(template);
			return template.Revision != 0;
		}
		if (_nodeFreeRejectedScenes.Contains(scene) || _nodeFreeTemplates.Count >= 256)
		{
			template = null;
			return false;
		}
		string text = scene.ResourcePath ?? "";
		SceneState sceneState = (((text.StartsWith("res://Asset/Anime/", StringComparison.Ordinal) || text.StartsWith("res://Prefab/Particles/Splats/", StringComparison.Ordinal)) && text.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) && ResourceLoader.Load<PackedScene>(text, "", ResourceLoader.CacheMode.Reuse) == scene) ? scene.GetState() : null);
		if (!GodotObject.IsInstanceValid(sceneState) || sceneState.GetNodeCount() != 1 || !string.Equals(sceneState.GetNodeType(0).ToString(), "Node2D", StringComparison.Ordinal))
		{
			_nodeFreeRejectedScenes.Add(scene);
			template = null;
			return false;
		}
		Script script = null;
		AdobeAnimateData adobeAnimateData = null;
		Material material = null;
		Vector2 origin = Vector2.Zero;
		Vector2 scale = Vector2.One;
		Vector2 offset = Vector2.Zero;
		float num = 0f;
		float num2 = 0f;
		int localZIndex = 0;
		double num3 = 1.0;
		string text2 = "";
		bool flag = true;
		bool flag2 = true;
		bool flag3 = false;
		Color color = Colors.White;
		Color color2 = Colors.White;
		int nodePropertyCount = sceneState.GetNodePropertyCount(0);
		for (int j = 0; j < nodePropertyCount; j++)
		{
			string text3 = sceneState.GetNodePropertyName(0, j).ToString();
			Variant nodePropertyValue = sceneState.GetNodePropertyValue(0, j);
			if (text3.StartsWith("Animation/MediaReplace/", StringComparison.Ordinal) && nodePropertyValue.VariantType != Variant.Type.Nil)
			{
				flag3 = true;
			}
			if (text3.StartsWith("Animation/LayerVisible/", StringComparison.Ordinal) && nodePropertyValue.VariantType == Variant.Type.Bool && !nodePropertyValue.AsBool())
			{
				flag3 = true;
			}
			switch (text3)
			{
			case "script":
				script = nodePropertyValue.AsGodotObject() as Script;
				break;
			case "flashAnimeData":
				adobeAnimateData = nodePropertyValue.AsGodotObject() as AdobeAnimateData;
				break;
			case "material":
				material = nodePropertyValue.AsGodotObject() as Material;
				break;
			case "position":
				origin = nodePropertyValue.AsVector2();
				break;
			case "rotation":
				num = nodePropertyValue.AsSingle();
				break;
			case "scale":
				scale = nodePropertyValue.AsVector2();
				break;
			case "skew":
				num2 = nodePropertyValue.AsSingle();
				break;
			case "offset":
				offset = nodePropertyValue.AsVector2();
				break;
			case "z_index":
				localZIndex = nodePropertyValue.AsInt32();
				break;
			case "z_as_relative":
				flag = nodePropertyValue.AsBool();
				break;
			case "visible":
				flag2 = nodePropertyValue.AsBool();
				break;
			case "modulate":
				color = nodePropertyValue.AsColor();
				break;
			case "self_modulate":
				color2 = nodePropertyValue.AsColor();
				break;
			case "timeScale":
				num3 = nodePropertyValue.AsDouble();
				break;
			case "Animation/Clip":
				text2 = nodePropertyValue.AsString();
				break;
			case "y_sort_enabled":
			case "playBack":
			case "blend":
			case "pause":
			case "top_level":
			case "refreshEveryFlame":
			case "refreshEveryFrame":
			case "use_parent_material":
			case "show_behind_parent":
				flag3 |= nodePropertyValue.AsBool();
				break;
			case "clip_children":
				flag3 |= nodePropertyValue.AsInt32() != 0;
				break;
			}
		}
		if (!GodotObject.IsInstanceValid(_builtInSpriteScript))
		{
			_builtInSpriteScript = ResourceLoader.Load<Script>("res://Extends/AdobeAnimateSprite/AdobeAnimateSpriteBase.cs", "", ResourceLoader.CacheMode.Reuse);
		}
		bool flag4 = material == null || (material is ShaderMaterial shaderMaterial && GodotObject.IsInstanceValid(shaderMaterial.Shader) && string.Equals(shaderMaterial.Shader.ResourcePath, "res://Asset/Shader/TowerDefense/ShaderTowerDefenseCharacter.gdshader", StringComparison.OrdinalIgnoreCase));
		PlaybackDataStamp orCreatePlaybackDataStamp = GetOrCreatePlaybackDataStamp(adobeAnimateData);
		if (flag3 || script != _builtInSpriteScript || orCreatePlaybackDataStamp == null || string.IsNullOrEmpty(text2) || !adobeAnimateData.HasClip(text2) || !flag || !flag2 || !IsWhite(color) || !IsWhite(color2) || !flag4 || !origin.IsFinite() || !scale.IsFinite() || !offset.IsFinite() || !float.IsFinite(num) || !float.IsFinite(num2) || !double.IsFinite(num3) || num3 <= 1E-06)
		{
			_nodeFreeRejectedScenes.Add(scene);
			template = null;
			return false;
		}
		template = new NodeFreeTemplate
		{
			Scene = scene,
			Data = adobeAnimateData,
			DataStamp = orCreatePlaybackDataStamp,
			LocalTransform = new Transform2D(num, scale, num2, origin),
			DrawTransform = new Transform2D(num, scale, num2, origin).TranslatedLocal(offset),
			Offset = offset,
			LocalZIndex = localZIndex,
			DefaultClip = text2,
			TimeScale = num3
		};
		scene.Changed += template.OnSceneChanged;
		_nodeFreeTemplates.Add(scene, template);
		CacheRecentNodeFreeTemplate(template);
		return true;
	}

	private void CacheRecentNodeFreeTemplate(NodeFreeTemplate template)
	{
		_nodeFreeRecentTemplates[_nodeFreeRecentTemplateCursor] = template;
		_nodeFreeRecentTemplateCursor = (_nodeFreeRecentTemplateCursor + 1) % _nodeFreeRecentTemplates.Length;
	}

	private NodeFreeSequence GetOrBuildNodeFreeSequence(NodeFreeTemplate template, string clips)
	{
		string text = (string.IsNullOrWhiteSpace(clips) ? template.DefaultClip : clips);
		PlaybackDataStamp dataStamp = template.DataStamp;
		if (dataStamp == null)
		{
			return null;
		}
		int rendererGeneration = _rendererGeneration;
		NodeFreeSequence lastSequence = template.LastSequence;
		if (lastSequence != null && ((object)template.LastSequenceKey == text || string.Equals(template.LastSequenceKey, text, StringComparison.Ordinal)) && lastSequence.DataVersion == dataStamp.Version && lastSequence.RendererGeneration == rendererGeneration)
		{
			return lastSequence;
		}
		int cacheVersion = AdobeAnimateGlobalAtlasCache.CacheVersion;
		if (template.Sequences.TryGetValue(text, out var value) && value.DataVersion == dataStamp.Version && value.AtlasCacheVersion == cacheVersion && value.RendererGeneration == rendererGeneration)
		{
			template.LastSequenceKey = text;
			template.LastSequence = value;
			return value;
		}
		if (value == null && template.Sequences.Count >= 256)
		{
			return null;
		}
		string[] array = text.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
		if (array.Length == 0)
		{
			return null;
		}
		NodeFreeClipChoice[][] array2 = new NodeFreeClipChoice[array.Length][];
		for (int i = 0; i < array.Length; i++)
		{
			string[] array3 = array[i].Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			if (array3.Length == 0)
			{
				return null;
			}
			NodeFreeClipChoice[] array4 = new NodeFreeClipChoice[array3.Length];
			for (int j = 0; j < array3.Length; j++)
			{
				string clip = array3[j];
				if (!TryResolvePlaybackDescriptor(template.Data, clip, trustedBuiltInPair: true, out var descriptor, out var descriptorWasTrusted) || !descriptorWasTrusted)
				{
					return null;
				}
				array4[j] = CreateNodeFreeClipChoice(template, clip, descriptor);
			}
			array2[i] = array4;
		}
		NodeFreeSequence nodeFreeSequence = new NodeFreeSequence
		{
			Alternatives = array2,
			DataVersion = dataStamp.Version,
			AtlasCacheVersion = cacheVersion,
			RendererGeneration = rendererGeneration
		};
		template.Sequences[text] = nodeFreeSequence;
		template.LastSequenceKey = text;
		template.LastSequence = nodeFreeSequence;
		return nodeFreeSequence;
	}

	private static NodeFreeClipChoice CreateNodeFreeClipChoice(NodeFreeTemplate template, string clip, PlaybackDescriptor descriptor)
	{
		float num = descriptor.FrameRate * (float)Math.Abs(template.TimeScale);
		return new NodeFreeClipChoice(clip, descriptor.DefinitionId, descriptor.RendererGeneration, descriptor.PlaybackFrameCount, num, (double)descriptor.PlaybackFrameCount / (double)num);
	}

	private static NodeFreeClipChoice ResolveNodeFreeClip(NodeFreeSequence sequence, int sequenceIndex)
	{
		NodeFreeClipChoice[] array = sequence.Alternatives[sequenceIndex];
		if (array.Length != 1)
		{
			return array[GD.RandRange(0, array.Length - 1)];
		}
		return array[0];
	}

	private static int ComputeNodeFreeZIndex(NodeFreeTemplate template, int gridY)
	{
		return (int)Math.Clamp((long)gridY * 15L + 10 + template.LocalZIndex, -4096L, 4096L);
	}

	private static bool IsWhite(Color color)
	{
		if (Math.Abs(color.R - 1f) <= 0.0001f && Math.Abs(color.G - 1f) <= 0.0001f && Math.Abs(color.B - 1f) <= 0.0001f)
		{
			return Math.Abs(color.A - 1f) <= 0.0001f;
		}
		return false;
	}

	private static bool HasHiddenLayer(Array<bool> layerVisible)
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

	private static bool HasMediaReplace(Array<bool> mediaReplaceUse)
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
}
