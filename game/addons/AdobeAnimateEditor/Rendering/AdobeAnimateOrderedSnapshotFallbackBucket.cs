using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

internal sealed class AdobeAnimateOrderedSnapshotFallbackBucket : IDisposable
{
	private readonly struct ImmediateSubmission(AdobeAnimateRenderSnapshot snapshot, bool forceCpuPose, ulong stableOrder)
	{
		public AdobeAnimateRenderSnapshot Snapshot { get; } = snapshot;

		public bool ForceCpuPose { get; } = forceCpuPose;

		public ulong StableOrder { get; } = stableOrder;
	}

	private sealed class Run
	{
		public AdobeAnimateSnapshotRunSignature Signature;

		public AdobeAnimateMultiMeshBatcher Batcher;

		public int RootCount;
	}

	private readonly List<Run> _runs = new List<Run>();

	private readonly List<AdobeAnimateDrawItem> _drawItems = new List<AdobeAnimateDrawItem>();

	private readonly HashSet<AdobeAnimateSprite> _drawItemVisitedSprites = new HashSet<AdobeAnimateSprite>();

	private readonly List<ImmediateSubmission> _immediateSubmissions = new List<ImmediateSubmission>();

	private Node _parent;

	private AdobeAnimateSharedStateArena _sharedStateArena;

	private int _activeRunCount;

	private int _rootCount;

	private long _frameVersion = -9223372036854775808L;

	private long _lastTouchedRenderFrameVersion = -9223372036854775808L;

	private long _lastActivityFrame = -9223372036854775808L;

	private bool _published;

	public int EffectiveZIndex { get; private set; } = -2147483648;

	public int ActiveRunCount => _activeRunCount;

	public int RootCount => _rootCount;

	public long LastTouchedRenderFrameVersion => _lastTouchedRenderFrameVersion;

	public long LastActivityFrame => _lastActivityFrame;

	public bool HasImmediateSubmissions => _immediateSubmissions.Count > 0;

	public void Attach(Node parent, int effectiveZIndex)
	{
		if (!GodotObject.IsInstanceValid(parent))
		{
			throw new ArgumentException("A valid render mount is required.", "parent");
		}
		Hide();
		_parent = parent;
		EffectiveZIndex = effectiveZIndex;
		for (int i = 0; i < _runs.Count; i++)
		{
			AttachBatcher(_runs[i], i, effectiveZIndex);
		}
	}

	public void BeginFrame(long frameVersion)
	{
		BeginFrame(frameVersion, frameVersion);
	}

	public void BeginFrame(long frameVersion, long lifecycleTick)
	{
		BeginFrame(frameVersion, lifecycleTick, null);
	}

	internal void BeginFrame(long frameVersion, long lifecycleTick, AdobeAnimateSharedStateArena sharedStateArena)
	{
		if (_frameVersion == frameVersion)
		{
			_sharedStateArena = sharedStateArena;
			_lastTouchedRenderFrameVersion = frameVersion;
			_lastActivityFrame = lifecycleTick;
			return;
		}
		_immediateSubmissions.Clear();
		ResetBufferedRuns(preserveVisible: true);
		_frameVersion = frameVersion;
		_lastTouchedRenderFrameVersion = frameVersion;
		_lastActivityFrame = lifecycleTick;
		_sharedStateArena = sharedStateArena;
		_published = false;
	}

	public void AbortFrame(long frameVersion)
	{
		if (_frameVersion == frameVersion)
		{
			_frameVersion = -9223372036854775808L;
			_lastTouchedRenderFrameVersion = -9223372036854775808L;
			_sharedStateArena = null;
			_published = false;
		}
	}

	public void BeginImmediateFrame(long frameVersion, long lifecycleTick)
	{
		_sharedStateArena = null;
		if (_frameVersion == frameVersion)
		{
			_lastTouchedRenderFrameVersion = frameVersion;
			_lastActivityFrame = lifecycleTick;
			return;
		}
		_frameVersion = frameVersion;
		_lastTouchedRenderFrameVersion = frameVersion;
		_lastActivityFrame = lifecycleTick;
		_published = false;
	}

	public bool RemoveImmediateSubmission(AdobeAnimateSprite sprite)
	{
		if (sprite == null)
		{
			return false;
		}
		bool flag = false;
		for (int num = _immediateSubmissions.Count - 1; num >= 0; num--)
		{
			AdobeAnimateSprite sprite2 = _immediateSubmissions[num].Snapshot.Sprite;
			if (!GodotObject.IsInstanceValid(sprite2) || sprite2 == sprite)
			{
				_immediateSubmissions.RemoveAt(num);
				flag = true;
			}
		}
		if (!flag)
		{
			return false;
		}
		if (_immediateSubmissions.Count == 0)
		{
			ResetBufferedRuns();
			return true;
		}
		if (!RebuildImmediateSnapshots())
		{
			Hide();
		}
		return true;
	}

	public bool TryAppendSnapshot(in AdobeAnimateRenderSnapshot snapshot, bool forceCpuPose = false)
	{
		return TryAppendSnapshotCore(in snapshot, forceCpuPose);
	}

	internal bool TryAppendSimpleCrowdState(AdobeAnimateCrowdRenderState state, AdobeAnimateSprite sprite)
	{
		if (_frameVersion == -9223372036854775808L || !GodotObject.IsInstanceValid(_parent) || state == null || state.Mode != AdobeAnimateCrowdRenderMode.Compact || !GodotObject.IsInstanceValid(sprite) || state.Definition?.Frames == null || state.Definition.SliceMetadata == null || state.Definition.Frames.Length == 0 || !GodotObject.IsInstanceValid(state.AtlasArray) || !GodotObject.IsInstanceValid(state.PoseTextureArray) || state.PoseTextureSize.X <= 0 || state.PoseTextureSize.Y <= 0 || state.HasMediaReplace || (!state.AllLayersVisible && !state.CanUseLayerMask) || sprite.NeedsDrawItemSortForRender() || sprite.HasManagedSlotSpritesForRender() || sprite.HasExternalVisualsForRender())
		{
			return false;
		}
		int frameOffset = state.FrameOffset;
		int frameCount = state.FrameCount;
		int poseBaseTexel = state.PoseBaseTexel;
		int poseLayer = state.PoseLayer;
		if (frameOffset < 0 || frameCount <= 0)
		{
			return false;
		}
		AdobeAnimateSnapshotRunSignature signature = new AdobeAnimateSnapshotRunSignature(state.AtlasArray, state.AtlasArraySize, state.PoseTextureArray, state.PoseTextureSize);
		Run run = TryGetCompatiblePreviousRun(in signature);
		if (run == null)
		{
			run = BeginNextRun(in signature);
			if (run == null)
			{
				return false;
			}
		}
		run.Batcher.AppendShaderPoseFrame(state.GlobalTransform, state.Modulate, state.Offset, state.VerticalClip.Enabled, state.VerticalClip.UpY, state.VerticalClip.DownY, state.Definition, frameOffset, frameCount, state.AllLayersVisible, state.LayerMask, poseBaseTexel, poseLayer, state.InterpolationT);
		run.RootCount++;
		_rootCount++;
		_published = false;
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.fallback.simpleStateRoots", 1);
		}
		return true;
	}

	internal bool TryAppendRetainedGpuGraphState(AdobeAnimateCrowdRenderState state, AdobeAnimateSprite sprite)
	{
		if (_frameVersion == -9223372036854775808L || !GodotObject.IsInstanceValid(_parent) || state == null || state.Mode != AdobeAnimateCrowdRenderMode.GpuGraph || !GodotObject.IsInstanceValid(sprite) || state.RenderMountParent != _parent || state.GpuGraphAllocation.Signature == 0L || state.GpuGraphAllocation.RenderSlotCount <= 0 || state.GpuGraphOwners == null || state.GpuGraphOwners.Length == 0 || state.GpuGraphOwners.Length != state.GpuGraphAllocation.OwnerCount || !GodotObject.IsInstanceValid(state.AtlasArray) || !GodotObject.IsInstanceValid(state.PoseTextureArray) || state.PoseTextureSize.X <= 0 || state.PoseTextureSize.Y <= 0 || !GodotObject.IsInstanceValid(state.GpuGraphTextureArray) || state.GpuGraphTextureSize.X <= 0 || state.GpuGraphTextureSize.Y <= 0)
		{
			return false;
		}
		AdobeAnimateCrowdLayout preparedGpuLayout = state.PreparedGpuLayout;
		float[] preparedRelocatableGpuState = state.PreparedRelocatableGpuState;
		bool gpuGraphUseAbsoluteTransform = state.GpuGraphUseAbsoluteTransform;
		if (preparedGpuLayout.StateTexelCount <= 0 || preparedGpuLayout.StateTexelCount > 536870911 || preparedRelocatableGpuState == null || preparedRelocatableGpuState.Length != preparedGpuLayout.StateTexelCount * 4 || state.PreparedRelocatableGpuUseAbsoluteTransform != gpuGraphUseAbsoluteTransform)
		{
			return false;
		}
		AdobeAnimateSnapshotRunSignature signature = new AdobeAnimateSnapshotRunSignature(state.AtlasArray, state.AtlasArraySize, state.PoseTextureArray, state.PoseTextureSize, state.GpuGraphTextureArray, state.GpuGraphTextureSize);
		Run run = TryGetCompatiblePreviousRun(in signature);
		if (run == null)
		{
			run = BeginNextRun(in signature);
			if (run == null)
			{
				return false;
			}
		}
		if (run.Batcher.AppendPreparedRelocatableGpuRenderGraphFrame(state.GpuGraphAllocation, state.GlobalTransform, preparedRelocatableGpuState, state.GpuGraphOwners.Length, preparedGpuLayout.StateTexelCount, gpuGraphUseAbsoluteTransform, state.GpuGraphRootOwnerState, state.RootMotion) != 1)
		{
			return false;
		}
		run.RootCount++;
		_rootCount++;
		_published = false;
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.fallback.retainedGpuGraphRoots", 1);
		}
		return true;
	}

	internal bool TryAppendPreparedGpuGraphState(AdobeAnimateCrowdRenderState state, AdobeAnimateSprite sprite, AdobeAnimateGpuRenderGraphDefinition graph, IReadOnlyList<AdobeAnimateGpuGraphOwnerState> ownerStates, int ownerStart, int ownerCount, IReadOnlyList<AdobeAnimateGpuDynamicOverrideAllocation> overrideAllocations, int overrideStart, IReadOnlyList<AdobeAnimateGpuManagedVisualState> managedVisualStates, int managedVisualStart, int managedVisualCount, int stateTexelCount, ImageTexture dynamicOverrideTexture, Vector2I dynamicOverrideTextureSize, bool requiresDynamicOverride)
	{
		if (_frameVersion == -9223372036854775808L || !GodotObject.IsInstanceValid(_parent) || state == null || state.Mode != AdobeAnimateCrowdRenderMode.GpuGraph || !GodotObject.IsInstanceValid(sprite) || state.RenderMountParent != _parent || graph == null || graph.Signature != state.GpuGraphAllocation.Signature || !GodotObject.IsInstanceValid(state.AtlasArray) || !GodotObject.IsInstanceValid(state.PoseTextureArray) || state.PoseTextureSize.X <= 0 || state.PoseTextureSize.Y <= 0 || !GodotObject.IsInstanceValid(state.GpuGraphTextureArray) || state.GpuGraphTextureSize.X <= 0 || state.GpuGraphTextureSize.Y <= 0 || (requiresDynamicOverride && (!GodotObject.IsInstanceValid(dynamicOverrideTexture) || dynamicOverrideTextureSize.X <= 0 || dynamicOverrideTextureSize.Y <= 0)))
		{
			return false;
		}
		AdobeAnimateSnapshotRunSignature signature = new AdobeAnimateSnapshotRunSignature(state.AtlasArray, state.AtlasArraySize, state.PoseTextureArray, state.PoseTextureSize, state.GpuGraphTextureArray, state.GpuGraphTextureSize);
		Run run = TryGetCompatiblePreviousRun(in signature);
		if (run == null)
		{
			run = BeginNextRun(in signature);
			if (run == null)
			{
				return false;
			}
		}
		if (requiresDynamicOverride)
		{
			run.Batcher.ConfigureBufferedGpuDynamicOverrideTexture(dynamicOverrideTexture, dynamicOverrideTextureSize);
		}
		if (run.Batcher.AppendPreparedGpuRenderGraphFrame(state.GpuGraphAllocation, graph, state.GlobalTransform, ownerStates, ownerStart, ownerCount, overrideAllocations, overrideStart, managedVisualStates, managedVisualStart, managedVisualCount, stateTexelCount, state.RootMotion) != 1)
		{
			return false;
		}
		run.RootCount++;
		_rootCount++;
		_published = false;
		if (TowerDefensePerfProfiler.DetailedHotPathMetrics)
		{
			TowerDefensePerfProfiler.SampleHotPath(requiresDynamicOverride ? "adobeAnimate.render.fallback.dynamicOverrideGpuGraphRoots" : "adobeAnimate.render.fallback.rootMotionGpuGraphRoots", 1);
		}
		return true;
	}

	internal bool TryAppendCachedDrawItems(in AdobeAnimateSnapshotRunSignature signature, IReadOnlyList<AdobeAnimateDrawItem> items, in Transform2D transform, in Color modulate, in Rect2 clipRect)
	{
		if (_frameVersion == -9223372036854775808L || !GodotObject.IsInstanceValid(_parent) || items == null || items.Count == 0 || !GodotObject.IsInstanceValid(signature.VisualAtlas))
		{
			return false;
		}
		Run run = TryGetCompatiblePreviousRun(in signature);
		if (run == null)
		{
			run = BeginNextRun(in signature);
			if (run == null)
			{
				return false;
			}
		}
		_drawItems.Clear();
		for (int i = 0; i < items.Count; i++)
		{
			AdobeAnimateDrawItem adobeAnimateDrawItem = items[i];
			_drawItems.Add(new AdobeAnimateDrawItem(transform * adobeAnimateDrawItem.Transform, adobeAnimateDrawItem.Size, adobeAnimateDrawItem.UvRect, adobeAnimateDrawItem.AtlasLayer, adobeAnimateDrawItem.Color * modulate, adobeAnimateDrawItem.SortPath, clipRect.Size.X > 0f && clipRect.Size.Y > 0f, clipRect.Position.Y, clipRect.End.Y, null, adobeAnimateDrawItem.UseShaderPose, adobeAnimateDrawItem.PoseTexel, adobeAnimateDrawItem.PoseLayer, adobeAnimateDrawItem.PoseFrameT, adobeAnimateDrawItem.PoseOffset, adobeAnimateDrawItem.UseVisualOverride, clipRect.Position.X, clipRect.End.X));
		}
		run.Batcher.AppendBuffered(_drawItems, 0, _drawItems.Count);
		run.RootCount++;
		_rootCount++;
		_published = false;
		return true;
	}

	public bool TryAppendImmediateSnapshot(in AdobeAnimateRenderSnapshot snapshot, bool forceCpuPose = false)
	{
		if (!CanAppendSnapshot(in snapshot))
		{
			return false;
		}
		ulong instanceId = snapshot.Sprite.GetInstanceId();
		for (int i = 0; i < _immediateSubmissions.Count; i++)
		{
			AdobeAnimateSprite sprite = _immediateSubmissions[i].Snapshot.Sprite;
			if (GodotObject.IsInstanceValid(sprite) && sprite.GetInstanceId() == instanceId)
			{
				ImmediateSubmission value = _immediateSubmissions[i];
				_immediateSubmissions[i] = new ImmediateSubmission(snapshot, forceCpuPose, _immediateSubmissions[i].StableOrder);
				if (RebuildImmediateSnapshots())
				{
					return true;
				}
				int num = FindImmediateSubmissionIndex(value.StableOrder, instanceId);
				if (num >= 0)
				{
					_immediateSubmissions[num] = value;
				}
				RebuildImmediateSnapshots();
				return false;
			}
		}
		ImmediateSubmission immediateSubmission = new ImmediateSubmission(snapshot, forceCpuPose, instanceId);
		int num2 = _immediateSubmissions.Count;
		while (num2 > 0 && CompareImmediateSubmissions(immediateSubmission, _immediateSubmissions[num2 - 1]) < 0)
		{
			num2--;
		}
		if (num2 == _immediateSubmissions.Count)
		{
			_immediateSubmissions.Add(immediateSubmission);
			if (TryAppendSnapshotCore(in snapshot, forceCpuPose))
			{
				return true;
			}
			_immediateSubmissions.RemoveAt(_immediateSubmissions.Count - 1);
			return false;
		}
		_immediateSubmissions.Insert(num2, immediateSubmission);
		if (RebuildImmediateSnapshots())
		{
			return true;
		}
		int num3 = FindImmediateSubmissionIndex(immediateSubmission.StableOrder, instanceId);
		if (num3 >= 0)
		{
			_immediateSubmissions.RemoveAt(num3);
		}
		RebuildImmediateSnapshots();
		return false;
	}

	private int FindImmediateSubmissionIndex(ulong stableOrder, ulong spriteId)
	{
		for (int i = 0; i < _immediateSubmissions.Count; i++)
		{
			ImmediateSubmission immediateSubmission = _immediateSubmissions[i];
			AdobeAnimateSprite sprite = immediateSubmission.Snapshot.Sprite;
			if (immediateSubmission.StableOrder == stableOrder && GodotObject.IsInstanceValid(sprite) && sprite.GetInstanceId() == spriteId)
			{
				return i;
			}
		}
		return -1;
	}

	private bool RebuildImmediateSnapshots()
	{
		_immediateSubmissions.Sort(CompareImmediateSubmissions);
		ResetBufferedRuns(preserveVisible: true);
		for (int i = 0; i < _immediateSubmissions.Count; i++)
		{
			ImmediateSubmission immediateSubmission = _immediateSubmissions[i];
			if (!TryAppendSnapshotCore(immediateSubmission.Snapshot, immediateSubmission.ForceCpuPose))
			{
				ResetBufferedRuns();
				return false;
			}
		}
		return true;
	}

	private static int CompareImmediateSubmissions(ImmediateSubmission left, ImmediateSubmission right)
	{
		long num = (GodotObject.IsInstanceValid(left.Snapshot.Sprite) ? left.Snapshot.Sprite.GetEffectiveRenderSortBandForRender() : 0);
		long value = (GodotObject.IsInstanceValid(right.Snapshot.Sprite) ? right.Snapshot.Sprite.GetEffectiveRenderSortBandForRender() : 0);
		int num2 = num.CompareTo(value);
		if (num2 != 0)
		{
			return num2;
		}
		int num3 = CompareTreeOrderPath(left.Snapshot.TreeOrderPath, right.Snapshot.TreeOrderPath);
		if (num3 == 0)
		{
			return left.StableOrder.CompareTo(right.StableOrder);
		}
		return num3;
	}

	private static int CompareTreeOrderPath(int[] left, int[] right)
	{
		int val = ((left != null) ? left.Length : 0);
		int num = ((right != null) ? right.Length : 0);
		int num2 = Math.Min(val, num);
		for (int i = 0; i < num2; i++)
		{
			int num3 = left[i].CompareTo(right[i]);
			if (num3 != 0)
			{
				return num3;
			}
		}
		return val.CompareTo(num);
	}

	private bool CanAppendSnapshot(in AdobeAnimateRenderSnapshot snapshot)
	{
		if (_frameVersion != -9223372036854775808L && GodotObject.IsInstanceValid(_parent) && GodotObject.IsInstanceValid(snapshot.Sprite) && snapshot.Definition != null && snapshot.Definition.Frames != null)
		{
			return snapshot.Definition.Frames.Length != 0;
		}
		return false;
	}

	private bool TryAppendSnapshotCore(in AdobeAnimateRenderSnapshot snapshot, bool forceCpuPose)
	{
		if (!CanAppendSnapshot(in snapshot) || !AdobeAnimateRenderManager.TryResolveSnapshotAtlas(snapshot, out var atlasArray, out var atlasSize))
		{
			return false;
		}
		TextureLayered poseTextureArray = null;
		Vector2I poseTextureSize = Vector2I.Zero;
		if (!forceCpuPose)
		{
			AdobeAnimateRenderManager.TryResolveSnapshotPoseTexture(snapshot, out poseTextureArray, out poseTextureSize);
		}
		AdobeAnimateSnapshotRunSignature signature = new AdobeAnimateSnapshotRunSignature(atlasArray, atlasSize, poseTextureArray, poseTextureSize);
		Run run = TryGetCompatiblePreviousRun(in signature);
		if (run == null)
		{
			run = BeginNextRun(in signature);
			if (run == null)
			{
				return false;
			}
		}
		_drawItems.Clear();
		int num = AdobeAnimateDrawItemBuilder.Build(snapshot, _drawItems, poseTextureArray, _frameVersion, _drawItemVisitedSprites);
		if (num > 1 && (snapshot.NeedsDrawItemSort || snapshot.Sprite.HasExternalVisualsInOwnedGraphForRender()))
		{
			CollectionsMarshal.AsSpan(_drawItems).Slice(0, num).Sort(AdobeAnimateDrawItemComparer.Instance);
		}
		run.Batcher.AppendBuffered(_drawItems, 0, num);
		run.RootCount++;
		_rootCount++;
		_published = false;
		return true;
	}

	private Run BeginNextRun(in AdobeAnimateSnapshotRunSignature signature)
	{
		int activeRunCount = _activeRunCount;
		Run run;
		if (activeRunCount < _runs.Count)
		{
			run = _runs[activeRunCount];
		}
		else
		{
			run = new Run();
			_runs.Add(run);
		}
		if (!GodotObject.IsInstanceValid(run.Batcher))
		{
			AttachBatcher(run, activeRunCount, EffectiveZIndex);
		}
		if (!GodotObject.IsInstanceValid(run.Batcher))
		{
			return null;
		}
		run.Signature = signature;
		run.RootCount = 0;
		run.Batcher.BeginFrame(signature.VisualAtlas, signature.VisualAtlasSize, preserveVisibleInstances: true);
		if (signature.UsesGpuGraph)
		{
			if (_sharedStateArena != null)
			{
				run.Batcher.BeginGpuGraphBuffered(signature.PoseTexture, signature.PoseTextureSize, signature.GpuGraphTexture, signature.GpuGraphTextureSize, _sharedStateArena, _frameVersion);
			}
			else
			{
				run.Batcher.BeginGpuGraphBuffered(signature.PoseTexture, signature.PoseTextureSize, signature.GpuGraphTexture, signature.GpuGraphTextureSize);
			}
		}
		else if (_sharedStateArena != null)
		{
			run.Batcher.BeginBuffered(signature.PoseTexture, signature.PoseTextureSize, _sharedStateArena, _frameVersion);
		}
		else
		{
			run.Batcher.BeginBuffered(signature.PoseTexture, signature.PoseTextureSize);
		}
		_activeRunCount++;
		return run;
	}

	private Run TryGetCompatiblePreviousRun(in AdobeAnimateSnapshotRunSignature signature)
	{
		if (_activeRunCount <= 0)
		{
			return null;
		}
		Run run = _runs[_activeRunCount - 1];
		AdobeAnimateSnapshotRunSignature signature2 = run.Signature;
		if (signature2.Equals(signature))
		{
			return run;
		}
		if (!signature2.HasSameBaseResources(signature) || (signature2.UsesGpuGraph && signature.UsesGpuGraph))
		{
			return null;
		}
		if (!signature2.UsesGpuGraph && signature.UsesGpuGraph)
		{
			run.Batcher.ConfigureBufferedGpuGraphTexture(signature.GpuGraphTexture, signature.GpuGraphTextureSize);
			run.Signature = signature;
		}
		return run;
	}

	internal void ApplyAnimationClock(float seconds, float physicsInterpolationFraction)
	{
		for (int i = 0; i < _activeRunCount; i++)
		{
			AdobeAnimateMultiMeshBatcher batcher = _runs[i].Batcher;
			if (GodotObject.IsInstanceValid(batcher))
			{
				batcher.ApplyStandaloneCrowdClock(seconds, physicsInterpolationFraction);
			}
		}
	}

	public bool TryPublish()
	{
		if (_published)
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(_parent))
		{
			return false;
		}
		for (int i = 0; i < _activeRunCount; i++)
		{
			Run run = _runs[i];
			if (!GodotObject.IsInstanceValid(run.Batcher))
			{
				Hide();
				return false;
			}
			run.Batcher.EndBuffered();
		}
		for (int j = _activeRunCount; j < _runs.Count; j++)
		{
			Run run2 = _runs[j];
			if (GodotObject.IsInstanceValid(run2.Batcher))
			{
				run2.Batcher.BeginFrame(null, Vector2.One);
			}
		}
		_published = true;
		return true;
	}

	public void Hide()
	{
		_immediateSubmissions.Clear();
		ResetBufferedRuns();
	}

	private void ResetBufferedRuns(bool preserveVisible = false)
	{
		for (int i = 0; i < _runs.Count; i++)
		{
			Run run = _runs[i];
			run.RootCount = 0;
			if (!preserveVisible && GodotObject.IsInstanceValid(run.Batcher))
			{
				run.Batcher.BeginFrame(null, Vector2.One);
			}
		}
		_activeRunCount = 0;
		_rootCount = 0;
		_published = false;
	}

	private void AttachBatcher(Run run, int runIndex, int effectiveZIndex)
	{
		if (!GodotObject.IsInstanceValid(_parent))
		{
			return;
		}
		if (!GodotObject.IsInstanceValid(run.Batcher))
		{
			run.Batcher = new AdobeAnimateMultiMeshBatcher
			{
				Name = $"AdobeAnimateSnapshotFallbackZ_{effectiveZIndex}_Run_{runIndex}",
				ProcessMode = Node.ProcessModeEnum.Always,
				ZAsRelative = false,
				ZIndex = effectiveZIndex
			};
		}
		else
		{
			run.Batcher.Name = $"AdobeAnimateSnapshotFallbackZ_{effectiveZIndex}_Run_{runIndex}";
			run.Batcher.ZAsRelative = false;
			run.Batcher.ZIndex = effectiveZIndex;
		}
		Node parent = run.Batcher.GetParent();
		if (GodotObject.IsInstanceValid(parent) && parent != _parent)
		{
			parent.RemoveChild(run.Batcher);
		}
		if (run.Batcher.GetParent() == null)
		{
			_parent.AddChild(run.Batcher, forceReadableName: false, Node.InternalMode.Disabled);
		}
		if (runIndex > 0 && runIndex - 1 < _runs.Count && GodotObject.IsInstanceValid(_runs[runIndex - 1].Batcher) && _runs[runIndex - 1].Batcher.GetParent() == _parent)
		{
			int num = Math.Min(_parent.GetChildCount() - 1, _runs[runIndex - 1].Batcher.GetIndex() + 1);
			if (run.Batcher.GetIndex() != num)
			{
				_parent.MoveChild(run.Batcher, num);
			}
		}
	}

	public void Dispose()
	{
		Hide();
		for (int i = 0; i < _runs.Count; i++)
		{
			AdobeAnimateMultiMeshBatcher batcher = _runs[i].Batcher;
			if (GodotObject.IsInstanceValid(batcher))
			{
				Node parent = batcher.GetParent();
				if (GodotObject.IsInstanceValid(parent))
				{
					parent.RemoveChild(batcher);
				}
				batcher.Free();
			}
		}
		_runs.Clear();
		_drawItems.Clear();
		_immediateSubmissions.Clear();
		_parent = null;
		_sharedStateArena = null;
		EffectiveZIndex = -2147483648;
		_frameVersion = -9223372036854775808L;
		_lastTouchedRenderFrameVersion = -9223372036854775808L;
		_lastActivityFrame = -9223372036854775808L;
	}

	internal void ReleaseForOwnerExit(bool queueNodeCleanup)
	{
		Hide();
		for (int i = 0; i < _runs.Count; i++)
		{
			AdobeAnimateMultiMeshBatcher batcher = _runs[i].Batcher;
			if (GodotObject.IsInstanceValid(batcher))
			{
				batcher.DetachRenderBindingsForOwnerExit();
			}
			if (queueNodeCleanup && GodotObject.IsInstanceValid(batcher) && !batcher.IsQueuedForDeletion())
			{
				batcher.QueueFree();
			}
			_runs[i].Batcher = null;
		}
		_runs.Clear();
		_drawItems.Clear();
		_immediateSubmissions.Clear();
		_parent = null;
		_sharedStateArena = null;
		EffectiveZIndex = -2147483648;
		_frameVersion = -9223372036854775808L;
		_lastTouchedRenderFrameVersion = -9223372036854775808L;
		_lastActivityFrame = -9223372036854775808L;
	}
}
