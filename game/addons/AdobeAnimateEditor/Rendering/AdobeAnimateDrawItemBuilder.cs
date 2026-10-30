using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;

internal sealed class AdobeAnimateDrawItemBuilder
{
	private readonly struct NestedSortPathKey(int[] parent, int count, int first, int second, int third) : IEquatable<NestedSortPathKey>
	{
		private int[] Parent { get; } = parent;

		private int Count { get; } = count;

		private int First { get; } = first;

		private int Second { get; } = second;

		private int Third { get; } = third;

		public bool Equals(NestedSortPathKey other)
		{
			if (Parent == other.Parent && Count == other.Count && First == other.First && Second == other.Second)
			{
				return Third == other.Third;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is NestedSortPathKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(RuntimeHelpers.GetHashCode(Parent), Count, First, Second, Third);
		}
	}

	private const int MaxSpriteDepth = 32;

	private const int SlotSprite2DOrderAfterSpriteInsertLayer = 2147483647;

	private const int MaxInternedNestedSortPaths = 65536;

	[ThreadStatic]
	private static Dictionary<NestedSortPathKey, int[]> _nestedSortPathCache;

	private AdobeAnimateDrawItemBuilder()
	{
	}

	public static int Build(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, TextureLayered poseTextureArray = null, long externalVisualFrameVersion = -9223372036854775808L, HashSet<AdobeAnimateSprite> visitedSprites = null)
	{
		if (output == null)
		{
			return 0;
		}
		int count = output.Count;
		int stableOrder = count;
		visitedSprites?.Clear();
		try
		{
			BuildSpriteTree(snapshot, output, Array.Empty<int>(), 0, snapshot.EffectiveZIndex, snapshot.TreeOrderPath, null, null, visitedSprites, poseTextureArray, externalVisualFrameVersion, ref stableOrder);
		}
		finally
		{
			visitedSprites?.Clear();
		}
		return output.Count - count;
	}

	public static bool TryBuildSimpleShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateMultiMeshBatcher batcher, TextureLayered poseTextureArray, out int appended)
	{
		bool needsNestedBuild;
		return TryBuildSimpleShaderPoseFrame(snapshot, batcher, poseTextureArray, out appended, out needsNestedBuild);
	}

	public static bool CanBuildCrowdShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, TextureLayered poseTextureArray)
	{
		AdobeAnimateRuntimeDefinition definition;
		int frameIndex;
		PackedFrame frame;
		float interpolationT;
		return TryResolveCrowdShaderPoseFrame(snapshot, poseTextureArray, out definition, out frameIndex, out frame, out interpolationT);
	}

	public static bool TryBuildCrowdShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateMultiMeshBatcher batcher, TextureLayered poseTextureArray, out int appended)
	{
		appended = 0;
		if (batcher == null)
		{
			return false;
		}
		if (!TryResolveCrowdShaderPoseFrame(snapshot, poseTextureArray, out var definition, out var _, out var frame, out var interpolationT))
		{
			return false;
		}
		if (frame.Count <= 0)
		{
			return true;
		}
		batcher.AppendCrowdShaderPoseFrame(definition, snapshot.GlobalTransform, snapshot.Modulate, snapshot.Offset, snapshot.VerticalClip.Enabled, snapshot.VerticalClip.UpY, snapshot.VerticalClip.DownY, frame.Offset, frame.Count, definition.GpuPoseTextureBaseTexel, definition.GpuPoseTextureLayer, interpolationT, snapshot.AllLayersVisible, snapshot.CanUseLayerMask, snapshot.LayerMask, snapshot.LayerVisible, snapshot.HasMediaReplace, snapshot.MediaReplaceRect, snapshot.MediaReplaceUse, snapshot.MediaReplaceAtlasPages, snapshot.MediaReplaceAtlasArraySize);
		appended = 1;
		TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.crowdFrames", 1);
		TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.crowdSlices", frame.Count);
		return true;
	}

	public static bool TryBuildSimpleShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateMultiMeshBatcher batcher, TextureLayered poseTextureArray, out int appended, out bool needsNestedBuild)
	{
		appended = 0;
		needsNestedBuild = false;
		if (batcher == null || snapshot.Sprite == null || !GodotObject.IsInstanceValid(snapshot.Sprite))
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		if (definition == null || definition.Frames == null || definition.SliceMetadata == null || definition.Frames.Length == 0)
		{
			return false;
		}
		if (!CanUseShaderPose(snapshot, poseTextureArray))
		{
			return false;
		}
		if (snapshot.NeedsDrawItemSort || snapshot.HasMediaReplace || (!snapshot.AllLayersVisible && !snapshot.CanUseLayerMask))
		{
			return false;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = snapshot.Sprite.GetSpriteChildrenForRender();
		needsNestedBuild = spriteChildrenForRender.Length != 0 || snapshot.Sprite.HasManagedSlotSpritesForRender() || snapshot.Sprite.HasExternalVisualsForRender();
		PackedSliceMetadata[] sliceMetadata = definition.SliceMetadata;
		if (sliceMetadata == null)
		{
			return false;
		}
		int num = ResolveFrameIndex(snapshot, definition);
		PackedFrame packedFrame = definition.Frames[num];
		int num2 = Math.Max(0, packedFrame.Offset);
		int num3 = Math.Min(sliceMetadata.Length, num2 + Math.Max(0, packedFrame.Count));
		if (num3 <= num2)
		{
			return true;
		}
		Transform2D globalTransform = snapshot.GlobalTransform;
		Color modulate = snapshot.Modulate;
		Vector2 offset = snapshot.Offset;
		bool enabled = snapshot.VerticalClip.Enabled;
		float upY = snapshot.VerticalClip.UpY;
		float downY = snapshot.VerticalClip.DownY;
		bool allLayersVisible = snapshot.AllLayersVisible;
		ulong layerMask = snapshot.LayerMask;
		int gpuPoseTextureLayer = definition.GpuPoseTextureLayer;
		int gpuPoseTextureBaseTexel = definition.GpuPoseTextureBaseTexel;
		float interpolationT = ResolveFrameInterpolation(snapshot.FrameFloat);
		appended = batcher.AppendShaderPoseFrame(globalTransform, modulate, offset, enabled, upY, downY, definition, num2, num3 - num2, allLayersVisible, layerMask, gpuPoseTextureBaseTexel, gpuPoseTextureLayer, interpolationT);
		SamplePosePathCounts(appended, 0, 0);
		if (appended > 0)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.shaderPoseFastSlices", appended);
		}
		return true;
	}

	private static bool TryResolveCrowdShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, TextureLayered poseTextureArray, out AdobeAnimateRuntimeDefinition definition, out int frameIndex, out PackedFrame frame, out float interpolationT)
	{
		definition = snapshot.Definition;
		frameIndex = 0;
		frame = default(PackedFrame);
		interpolationT = 0f;
		if (snapshot.Sprite == null || !GodotObject.IsInstanceValid(snapshot.Sprite))
		{
			return false;
		}
		if (definition == null || definition.Frames == null || definition.SliceMetadata == null || definition.Frames.Length == 0)
		{
			return false;
		}
		if (!CanUseShaderPose(snapshot, poseTextureArray))
		{
			return false;
		}
		if (snapshot.NeedsDrawItemSort)
		{
			return false;
		}
		if (snapshot.Sprite.HasExternalVisualsForRender())
		{
			return false;
		}
		bool flag = !snapshot.Sprite.HasManagedSlotSpritesForRender();
		if (snapshot.Sprite.GetSpriteChildrenForRender().Length != 0 || !flag)
		{
			return false;
		}
		if (definition.MaxFrameSliceCount <= 0)
		{
			return false;
		}
		frameIndex = ResolveFrameIndex(snapshot, definition);
		frame = definition.Frames[frameIndex];
		interpolationT = ResolveFrameInterpolation(snapshot.FrameFloat);
		return true;
	}

	public static int BuildNestedRenderItems(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, TextureLayered poseTextureArray = null, long externalVisualFrameVersion = -9223372036854775808L)
	{
		if (output == null || snapshot.Sprite == null || !GodotObject.IsInstanceValid(snapshot.Sprite))
		{
			return 0;
		}
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0)
		{
			return 0;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = snapshot.Sprite.GetSpriteChildrenForRender();
		if (spriteChildrenForRender.Length == 0 && !snapshot.Sprite.HasManagedSlotSpritesForRender() && !snapshot.Sprite.HasExternalVisualsForRender())
		{
			return 0;
		}
		int count = output.Count;
		int stableOrder = count;
		int frameIndex = ResolveFrameIndex(snapshot, definition);
		HashSet<AdobeAnimateSprite> hashSet = new HashSet<AdobeAnimateSprite>();
		hashSet.Add(snapshot.Sprite);
		AppendChildSprites(snapshot, output, spriteChildrenForRender, Array.Empty<int>(), 0, snapshot.EffectiveZIndex, snapshot.TreeOrderPath, null, null, buildSortPath: false, hashSet, poseTextureArray, externalVisualFrameVersion, frameIndex, ref stableOrder);
		AppendSlotSprite2D(snapshot, output, Array.Empty<int>(), snapshot.EffectiveZIndex, snapshot.TreeOrderPath, null, null, buildSortPath: false, frameIndex, ref stableOrder);
		AppendExternalVisuals(snapshot, output, Array.Empty<int>(), snapshot.EffectiveZIndex, snapshot.TreeOrderPath, null, null, buildSortPath: true, frameIndex, externalVisualFrameVersion, ref stableOrder);
		return output.Count - count;
	}

	private static void BuildSpriteTree(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, int[] nestedPath, int depth, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, HashSet<AdobeAnimateSprite> visited, TextureLayered poseTextureArray, long externalVisualFrameVersion, ref int stableOrder, AdobeAnimateCpuVisualBuildResult cpuResult = null, Rid cpuAtlasArrayRid = default(Rid))
	{
		if (depth > 32 || snapshot.Sprite == null || !GodotObject.IsInstanceValid(snapshot.Sprite))
		{
			return;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = snapshot.Sprite.GetSpriteChildrenForRender();
		bool flag = false;
		if (visited != null || spriteChildrenForRender.Length != 0)
		{
			if (visited == null)
			{
				visited = new HashSet<AdobeAnimateSprite>();
			}
			if (!visited.Add(snapshot.Sprite))
			{
				return;
			}
			flag = true;
		}
		try
		{
			AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
			if (definition == null || definition.Frames == null || definition.SliceMetadata == null || definition.Frames.Length == 0)
			{
				return;
			}
			int num = ResolveFrameIndex(snapshot, definition);
			PackedFrame packedFrame = definition.Frames[num];
			int num2 = Math.Max(0, packedFrame.Offset);
			int num3 = Math.Min(definition.SliceMetadata.Length, num2 + Math.Max(0, packedFrame.Count));
			float interpolationT = ResolveFrameInterpolation(snapshot.FrameFloat);
			bool flag2 = CanUseShaderPose(snapshot, poseTextureArray);
			bool buildSortPath = snapshot.NeedsDrawItemSort || cpuResult != null || sortLayerOverride.HasValue || sortDrawOverride.HasValue || snapshot.Sprite.HasExternalVisualsForRender() || (nestedPath != null && nestedPath.Length != 0);
			if (TryAppendSimpleShaderPoseFrame(snapshot, definition, packedFrame, output, spriteChildrenForRender.Length, sortLayerOverride, sortDrawOverride, buildSortPath, flag2, interpolationT, out var appended))
			{
				SamplePosePathCounts(appended, 0, 0);
				if (appended > 0)
				{
					TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.shaderPoseFastSlices", appended);
				}
				return;
			}
			int num4 = 0;
			int packedPoseSlices = 0;
			int num5 = 0;
			PackedSlicePose[] array = (flag2 ? null : PrepareCpuPoseSampling(definition, interpolationT, snapshot.ClipBlend));
			for (int i = num2; i < num3; i++)
			{
				if (flag2 && i >= 0 && i < definition.SliceMetadata.Length && TryAppendShaderPoseSlice(snapshot, i, definition.SliceMetadata[i], output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, interpolationT, ref stableOrder))
				{
					num4++;
					continue;
				}
				if (array == null)
				{
					array = PrepareCpuPoseSampling(definition, interpolationT, snapshot.ClipBlend);
				}
				PackedSlicePose target = SampleSlice(definition, array, i, interpolationT);
				BlendSliceFromPreviousClip(snapshot, definition, array, packedFrame, i, ref target);
				bool flag3 = target.MediaId != 65535 && IsLayerVisible(snapshot, target.LayerId);
				if (flag3 && cpuResult != null)
				{
					cpuResult.ExpectedVisibleItems++;
				}
				if (TryAppendSlice(snapshot, i, target, output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, ref stableOrder))
				{
					num5++;
					if (cpuResult != null)
					{
						cpuResult.MergedVisibleItems++;
					}
				}
				else if (flag3 && cpuResult != null)
				{
					cpuResult.Failure = new AdobeAnimateCpuVisualFailure("animation-slice-unclassified", GetCpuRootIdentity(snapshot.Sprite), $"Visible animation slice {i} could not build a CPU draw item.");
					return;
				}
			}
			SamplePosePathCounts(num4, packedPoseSlices, num5);
			AppendChildSprites(snapshot, output, spriteChildrenForRender, nestedPath, depth, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, visited, poseTextureArray, externalVisualFrameVersion, num, ref stableOrder, cpuResult, cpuAtlasArrayRid);
			if (cpuResult == null || !cpuResult.Failure.IsValid)
			{
				AppendSlotSprite2D(snapshot, output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, num, ref stableOrder, cpuResult, cpuAtlasArrayRid);
				if (cpuResult == null || !cpuResult.Failure.IsValid)
				{
					AppendExternalVisuals(snapshot, output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, num, externalVisualFrameVersion, ref stableOrder, cpuResult, cpuAtlasArrayRid);
				}
			}
		}
		finally
		{
			if (flag)
			{
				visited.Remove(snapshot.Sprite);
			}
		}
	}

	private static void SamplePosePathCounts(int shaderPoseSlices, int packedPoseSlices, int dynamicPoseSlices)
	{
		if (shaderPoseSlices > 0)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.shaderPoseSlices", shaderPoseSlices);
		}
		if (packedPoseSlices > 0)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.packedPoseSlices", packedPoseSlices);
		}
		if (dynamicPoseSlices > 0)
		{
			TowerDefensePerfProfiler.SampleHotPath("adobeAnimate.render.buildItems.dynamicPoseSlices", dynamicPoseSlices);
		}
	}

	private static bool TryAppendSimpleShaderPoseFrame(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateRuntimeDefinition definition, PackedFrame frame, List<AdobeAnimateDrawItem> output, int childSpriteCount, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, bool useShaderPose, float interpolationT, out int appended)
	{
		appended = 0;
		if (!useShaderPose || buildSortPath || childSpriteCount > 0 || snapshot.Sprite.HasExternalVisualsForRender() || sortLayerOverride.HasValue || sortDrawOverride.HasValue || (!snapshot.AllLayersVisible && !snapshot.CanUseLayerMask) || snapshot.HasMediaReplace || definition?.SliceMetadata == null)
		{
			return false;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(definition.SliceMetadata.Length, num + Math.Max(0, frame.Count));
		if (num2 <= num)
		{
			return true;
		}
		Transform2D globalTransform = snapshot.GlobalTransform;
		Color modulate = snapshot.Modulate;
		Vector2 offset = snapshot.Offset;
		bool enabled = snapshot.VerticalClip.Enabled;
		float upY = snapshot.VerticalClip.UpY;
		float downY = snapshot.VerticalClip.DownY;
		bool allLayersVisible = snapshot.AllLayersVisible;
		ulong layerMask = snapshot.LayerMask;
		int gpuPoseTextureLayer = definition.GpuPoseTextureLayer;
		int gpuPoseTextureBaseTexel = definition.GpuPoseTextureBaseTexel;
		for (int i = num; i < num2; i++)
		{
			ref PackedSliceMetadata reference = ref definition.SliceMetadata[i];
			if (AdobeAnimateDefinitionCache.IsBaseSliceRenderable(definition, reference.MediaId) && (allLayersVisible || IsLayerVisible(layerMask, reference.LayerId)))
			{
				output.Add(new AdobeAnimateDrawItem(globalTransform, Vector2.One, default(Rect2), 0, modulate, default(AdobeAnimateSortPath), enabled, upY, downY, snapshot.Sprite, useShaderPose: true, gpuPoseTextureBaseTexel + i * 5, gpuPoseTextureLayer, interpolationT, offset));
				appended++;
			}
		}
		return true;
	}

	private static bool TryAppendShaderPoseSlice(AdobeAnimateRenderSnapshot snapshot, int sourceIndex, PackedSliceMetadata slice, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, float interpolationT, ref int stableOrder)
	{
		int mediaId = slice.MediaId;
		if (mediaId == 65535)
		{
			return false;
		}
		if (!IsLayerVisible(snapshot, slice.LayerId))
		{
			return false;
		}
		bool flag = IsMediaReplaceActive(snapshot, mediaId);
		PackedSliceRenderInfo renderInfo;
		if (!flag && AdobeAnimateDefinitionCache.IsBaseSliceRenderable(snapshot.Definition, mediaId))
		{
			renderInfo = new PackedSliceRenderInfo(Vector2.One, default(Rect2), 0);
		}
		else if (!TryResolveSliceRenderInfo(snapshot, sourceIndex, mediaId, flag, out renderInfo))
		{
			return false;
		}
		if (!renderInfo.IsValid)
		{
			return false;
		}
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		int poseTexel = definition.GpuPoseTextureBaseTexel + sourceIndex * 5;
		Color modulate = snapshot.Modulate;
		AdobeAnimateSortPath sortPath = (buildSortPath ? new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, sortLayerOverride ?? slice.LayerId, sortDrawOverride ?? slice.DrawOrder, stableOrder++, (sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, slice.LayerId, slice.DrawOrder) : nestedPath) : default(AdobeAnimateSortPath));
		output.Add(new AdobeAnimateDrawItem(snapshot.GlobalTransform, renderInfo.SourceSize, renderInfo.UvRect, renderInfo.AtlasLayer, modulate, sortPath, snapshot.VerticalClip.Enabled, snapshot.VerticalClip.UpY, snapshot.VerticalClip.DownY, snapshot.Sprite, useShaderPose: true, poseTexel, definition.GpuPoseTextureLayer, interpolationT, snapshot.Offset, flag));
		return true;
	}

	private static bool TryAppendSlice(AdobeAnimateRenderSnapshot snapshot, int sourceIndex, PackedSlicePose slice, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, ref int stableOrder)
	{
		int mediaId = slice.MediaId;
		if (mediaId == 65535)
		{
			return false;
		}
		if (!IsLayerVisible(snapshot, slice.LayerId))
		{
			return false;
		}
		bool mediaReplaceActive = IsMediaReplaceActive(snapshot, mediaId);
		if (!TryResolveSliceRenderInfo(snapshot, sourceIndex, mediaId, mediaReplaceActive, out var renderInfo))
		{
			return false;
		}
		Rect2 uvRect = renderInfo.UvRect;
		Vector2 sourceSize = renderInfo.SourceSize;
		if (sourceSize.X <= 0f || sourceSize.Y <= 0f)
		{
			return false;
		}
		Transform2D transform = BuildSliceTransform(snapshot.GlobalTransform, snapshot.Offset, slice, sourceSize);
		Color modulate = snapshot.Modulate;
		modulate.A *= slice.Alpha;
		AdobeAnimateSortPath sortPath = (buildSortPath ? new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, sortLayerOverride ?? slice.LayerId, sortDrawOverride ?? slice.DrawOrder, stableOrder++, (sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, slice.LayerId, slice.DrawOrder) : nestedPath) : default(AdobeAnimateSortPath));
		output.Add(new AdobeAnimateDrawItem(transform, sourceSize, uvRect, renderInfo.AtlasLayer, modulate, sortPath, snapshot.VerticalClip.Enabled, snapshot.VerticalClip.UpY, snapshot.VerticalClip.DownY, snapshot.Sprite));
		return true;
	}

	private static bool TryResolveSliceRenderInfo(AdobeAnimateRenderSnapshot snapshot, int sourceIndex, int mediaId, bool mediaReplaceActive, out PackedSliceRenderInfo renderInfo)
	{
		renderInfo = default(PackedSliceRenderInfo);
		if (mediaReplaceActive)
		{
			if (!ResolveMediaRect(snapshot, mediaId, out var rect, out var atlasLayer, out var atlasSize))
			{
				return false;
			}
			renderInfo = new PackedSliceRenderInfo(rect.Size, NormalizeRect(rect, atlasSize), atlasLayer);
			return renderInfo.IsValid;
		}
		return AdobeAnimateDefinitionCache.TryGetBaseSliceRenderInfo(snapshot.Definition, mediaId, out renderInfo);
	}

	private static void AppendSlotSprite2D(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, int frameIndex, ref int stableOrder, AdobeAnimateCpuVisualBuildResult cpuResult = null, Rid cpuAtlasArrayRid = default(Rid))
	{
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = snapshot.Sprite.GetManagedSlotSpritesForRender();
		for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
		{
			AdobeAnimateManagedSlotSprite pair = managedSlotSpritesForRender[i];
			if (cpuResult != null)
			{
				AppendCpuManagedSlotSprite(snapshot, in pair, output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, frameIndex, ref stableOrder, cpuAtlasArrayRid, cpuResult);
				if (cpuResult.Failure.IsValid)
				{
					break;
				}
				continue;
			}
			AdobeAnimateSlot slot = pair.Slot;
			CanvasItem visual = pair.Visual;
			Sprite2D sprite = pair.Sprite;
			AdobeAnimatePart atlasPart = pair.AtlasPart;
			if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual) || visual.GetParent() != slot || !AdobeAnimateManagedSprite2D.ShouldRender(visual) || (!slot.Visible && !slot.useFollowVisible) || !snapshot.Sprite.TryGetManagedSlotTransformForRender(slot, out var transform))
			{
				continue;
			}
			Rect2 rect;
			int atlasLayer;
			Vector2 vector;
			Vector2 vector2;
			bool flag;
			bool flag2;
			if (GodotObject.IsInstanceValid(atlasPart))
			{
				if (!AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasPart.externalAtlasTexturePath, out var allocation) || !allocation.UsesTextureArray || allocation.TextureArraySize.X <= 0f || allocation.TextureArraySize.Y <= 0f)
				{
					continue;
				}
				rect = allocation.Rect;
				atlasLayer = Math.Max(0, allocation.AtlasPage);
				vector = allocation.TextureArraySize;
				vector2 = (atlasPart.externalAtlasCentered ? (-rect.Size * 0.5f) : Vector2.Zero);
				flag = false;
				flag2 = false;
			}
			else
			{
				if (!GodotObject.IsInstanceValid(sprite) || !GodotObject.IsInstanceValid(sprite.Texture))
				{
					continue;
				}
				rect = ResolveSpriteSourceRect(sprite, sprite.Texture);
				atlasLayer = 0;
				vector = sprite.Texture.GetSize();
				vector2 = ResolveSpriteDrawOrigin(sprite, rect.Size);
				flag = sprite.FlipH;
				flag2 = sprite.FlipV;
				if (AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(sprite.Texture, rect, out var allocation2))
				{
					rect = allocation2.Rect;
					atlasLayer = Math.Max(0, allocation2.AtlasPage);
					vector = ((allocation2.TextureArraySize.X > 0f && allocation2.TextureArraySize.Y > 0f) ? allocation2.TextureArraySize : (GodotObject.IsInstanceValid(allocation2.Texture) ? allocation2.Texture.GetSize() : vector));
				}
			}
			if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
			{
				continue;
			}
			AdobeAnimateSortPath sortPath = default(AdobeAnimateSortPath);
			if (buildSortPath)
			{
				int num = Math.Max(0, slot.ResolveDrawLayerId() - 1);
				int drawOrder = num;
				if (!snapshot.Sprite.TryGetFrameDrawOrderForRender(frameIndex, num, out drawOrder))
				{
					drawOrder = num;
				}
				int layerOrder = sortLayerOverride ?? num;
				int drawOrder2 = sortDrawOverride ?? drawOrder;
				int[] nestedPath2 = ((sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, num, drawOrder, 2147483647) : AppendNestedPath(nestedPath, 2147483647));
				sortPath = new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, layerOrder, drawOrder2, stableOrder++, nestedPath2);
			}
			Transform2D transform2D = ((visual is Node2D node2D) ? node2D.Transform : Transform2D.Identity);
			Transform2D transform2D2 = transform * transform2D;
			Vector2 originPos = transform2D2 * vector2;
			Vector2 vector3 = (flag ? (-transform2D2.X) : transform2D2.X);
			Vector2 vector4 = (flag2 ? (-transform2D2.Y) : transform2D2.Y);
			Transform2D transform2 = snapshot.GlobalTransform * new Transform2D(vector3 * rect.Size.X, vector4 * rect.Size.Y, originPos);
			Color color = Multiply(Multiply(snapshot.Modulate, slot.Modulate), visual.Modulate);
			output.Add(new AdobeAnimateDrawItem(transform2, rect.Size, NormalizeRect(rect, vector), atlasLayer, color, sortPath, clipEnabled: false, 0f, 0f, snapshot.Sprite));
		}
	}

	private static void AppendExternalVisuals(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, int frameIndex, long frameVersion, ref int stableOrder, AdobeAnimateCpuVisualBuildResult cpuResult = null, Rid cpuAtlasArrayRid = default(Rid))
	{
		AdobeAnimateSprite sprite = snapshot.Sprite;
		sprite.BeginExternalVisualPreparationForRender(frameVersion);
		ReadOnlySpan<int> activeExternalVisualIndicesForRender = sprite.GetActiveExternalVisualIndicesForRender();
		for (int i = 0; i < activeExternalVisualIndicesForRender.Length; i++)
		{
			int num = activeExternalVisualIndicesForRender[i];
			if (cpuResult != null)
			{
				if (!sprite.TryGetExternalVisualForRender(num, out var visual))
				{
					cpuResult.Failure = new AdobeAnimateCpuVisualFailure("external-visual-unavailable", GetCpuRootIdentity(sprite), $"Registered external visual {num} is unavailable.");
					break;
				}
				AppendCpuExternalVisual(snapshot, in visual, i, output, nestedPath, rootZIndex, rootTreeOrderPath, sortLayerOverride, sortDrawOverride, buildSortPath, frameIndex, ref stableOrder, cpuAtlasArrayRid, cpuResult);
				if (cpuResult.Failure.IsValid)
				{
					break;
				}
				continue;
			}
			if (!sprite.TryGetExternalVisualForRender(num, out var visual2) || !GodotObject.IsInstanceValid(visual2.Sprite))
			{
				sprite.MarkExternalVisualPreparedForRender(num, frameVersion, preparedForCrowd: false);
				continue;
			}
			if (!visual2.Visible)
			{
				sprite.MarkExternalVisualPreparedForRender(num, frameVersion, preparedForCrowd: true);
				continue;
			}
			Sprite2D sprite2 = visual2.Sprite;
			Texture2D texture = visual2.Texture;
			Rect2 localSourceRect = (GodotObject.IsInstanceValid(texture) ? ResolveSpriteSourceRect(sprite2, texture) : default(Rect2));
			if (!GodotObject.IsInstanceValid(texture) || localSourceRect.Size.X <= 0f || localSourceRect.Size.Y <= 0f || !AdobeAnimateGlobalAtlasCache.TryGetExternalTextureAllocation(texture, localSourceRect, out var allocation) || !allocation.UsesTextureArray || !allocation.TextureArrayRid.IsValid || snapshot.Definition == null || allocation.TextureArrayRid != snapshot.Definition.AtlasTextureArrayRid)
			{
				sprite.MarkExternalVisualPreparedForRender(num, frameVersion, preparedForCrowd: false);
				continue;
			}
			AdobeAnimateSlot slot = visual2.Descriptor.Slot;
			Transform2D transform2D = visual2.Transform;
			Color color = Multiply(snapshot.Modulate, visual2.Modulate);
			int num2;
			int drawOrder;
			int num3;
			if (visual2.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot)
			{
				if (!GodotObject.IsInstanceValid(slot) || !sprite.TryGetManagedSlotTransformForRender(slot, out var transform))
				{
					sprite.MarkExternalVisualPreparedForRender(num, frameVersion, preparedForCrowd: false);
					continue;
				}
				transform2D = transform * transform2D;
				color = Multiply(color, slot.Modulate);
				num2 = Math.Max(0, slot.ResolveDrawLayerId() - 1);
				if (!sprite.TryGetFrameDrawOrderForRender(frameIndex, num2, out drawOrder))
				{
					drawOrder = num2;
				}
				num3 = 2147483647;
			}
			else
			{
				num3 = ((visual2.Descriptor.DrawBand == AdobeAnimateExternalVisualDrawBand.BehindAnimation) ? (-2147483648) : 2147483647);
				num2 = num3;
				drawOrder = num3;
			}
			Rect2 rect = allocation.Rect;
			Vector2 size = rect.Size;
			Vector2 originPos = ResolveSpriteDrawOrigin(sprite2, size);
			Transform2D transform2D2 = new Transform2D((sprite2.FlipH ? (-Vector2.Right) : Vector2.Right) * size.X, (sprite2.FlipV ? (-Vector2.Down) : Vector2.Down) * size.Y, originPos);
			Transform2D transform2 = ((visual2.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.World) ? (visual2.RegisteredWorldTransform * transform2D2) : (snapshot.GlobalTransform * transform2D * transform2D2));
			AdobeAnimateSortPath sortPath = default(AdobeAnimateSortPath);
			if (buildSortPath)
			{
				int[] nestedPath2 = AppendNestedPath(nestedPath, num3, visual2.Descriptor.RelativeZ, i);
				sortPath = new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, sortLayerOverride ?? num2, sortDrawOverride ?? drawOrder, stableOrder++, nestedPath2);
			}
			output.Add(new AdobeAnimateDrawItem(transform2, size, NormalizeRect(rect, allocation.TextureArraySize), allocation.AtlasPage, color, sortPath, clipEnabled: false, 0f, 0f, sprite));
			sprite.MarkExternalVisualPreparedForRender(num, frameVersion, preparedForCrowd: true);
		}
	}

	private static void AppendCpuManagedSlotSprite(AdobeAnimateRenderSnapshot snapshot, in AdobeAnimateManagedSlotSprite pair, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, int frameIndex, ref int stableOrder, Rid expectedAtlasArrayRid, AdobeAnimateCpuVisualBuildResult result)
	{
		AdobeAnimateSlot slot = pair.Slot;
		CanvasItem visual = pair.Visual;
		Sprite2D sprite = pair.Sprite;
		AdobeAnimatePart atlasPart = pair.AtlasPart;
		Transform2D transform = Transform2D.Identity;
		bool flag = GodotObject.IsInstanceValid(slot) && snapshot.Sprite.TryGetManagedSlotTransformForRender(slot, out transform);
		bool flag2 = GodotObject.IsInstanceValid(slot) && GodotObject.IsInstanceValid(visual) && AdobeAnimateManagedSprite2D.GetLogicalVisible(visual) && (slot.Visible || (slot.useFollowVisible && flag));
		if (flag2)
		{
			result.ExpectedVisibleItems++;
		}
		AdobeAnimateSortPath sortPath = default(AdobeAnimateSortPath);
		if (GodotObject.IsInstanceValid(slot) && buildSortPath)
		{
			int num = Math.Max(0, slot.ResolveDrawLayerId() - 1);
			int drawOrder;
			int num2 = (snapshot.Sprite.TryGetFrameDrawOrderForRender(frameIndex, num, out drawOrder) ? drawOrder : num);
			int[] nestedPath2 = ((sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, num, num2, 2147483647) : AppendNestedPath(nestedPath, 2147483647));
			sortPath = new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, sortLayerOverride ?? num, sortDrawOverride ?? num2, stableOrder++, nestedPath2);
		}
		AdobeAnimateDrawItem drawItem = default(AdobeAnimateDrawItem);
		if (flag2 && flag)
		{
			Rect2 rect = default(Rect2);
			Vector2 atlasSize = default(Vector2);
			int atlasLayer = 0;
			Vector2 vector = Vector2.Zero;
			bool flag3 = false;
			bool flag4 = false;
			if (GodotObject.IsInstanceValid(atlasPart) && AdobeAnimateGlobalAtlasCache.TryGetReplaceTextureAllocation(atlasPart.externalAtlasTexturePath, out var allocation) && allocation.TextureArraySize.X > 0f && allocation.TextureArraySize.Y > 0f)
			{
				rect = allocation.Rect;
				atlasSize = allocation.TextureArraySize;
				atlasLayer = Math.Max(0, allocation.AtlasPage);
				vector = (atlasPart.externalAtlasCentered ? (-rect.Size * 0.5f) : Vector2.Zero);
			}
			else if (GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(sprite.Texture))
			{
				Texture2D texture = sprite.Texture;
				rect = ResolveSpriteSourceRect(sprite, texture);
				atlasSize = texture.GetSize();
				vector = ResolveSpriteDrawOrigin(sprite, rect.Size);
				flag3 = sprite.FlipH;
				flag4 = sprite.FlipV;
			}
			Transform2D transform2D = ((visual is Node2D node2D) ? node2D.Transform : Transform2D.Identity);
			Transform2D transform2D2 = transform * transform2D;
			Vector2 originPos = transform2D2 * vector;
			Transform2D transform2 = snapshot.GlobalTransform * new Transform2D((flag3 ? (-transform2D2.X) : transform2D2.X) * rect.Size.X, (flag4 ? (-transform2D2.Y) : transform2D2.Y) * rect.Size.Y, originPos);
			drawItem = new AdobeAnimateDrawItem(transform2, rect.Size, NormalizeRect(rect, atlasSize), atlasLayer, Multiply(Multiply(snapshot.Modulate, slot.Modulate), visual.Modulate), sortPath, clipEnabled: false, 0f, 0f, snapshot.Sprite);
		}
		else if (flag2)
		{
			result.Failure = new AdobeAnimateCpuVisualFailure("managed-slot-transform-unavailable", GetCpuRootIdentity(snapshot.Sprite), "The visible managed Slot visual has no render transform.");
			return;
		}
		RecordCpuClassification(AdobeAnimateCpuVisualClassifier.ClassifyManagedSlot(in pair, expectedAtlasArrayRid, in drawItem, in sortPath, flag2), output, result);
	}

	private static void AppendCpuExternalVisual(AdobeAnimateRenderSnapshot snapshot, in AdobeAnimateExternalVisualSnapshot visual, int activeIndex, List<AdobeAnimateDrawItem> output, int[] nestedPath, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, int frameIndex, ref int stableOrder, Rid expectedAtlasArrayRid, AdobeAnimateCpuVisualBuildResult result)
	{
		if (visual.Visible)
		{
			result.ExpectedVisibleItems++;
		}
		Sprite2D sprite = visual.Sprite;
		AdobeAnimateSlot slot = visual.Descriptor.Slot;
		int num = ((visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot) ? 2147483647 : ((visual.Descriptor.DrawBand == AdobeAnimateExternalVisualDrawBand.BehindAnimation) ? (-2147483648) : 2147483647));
		int num2 = num;
		int num3 = num;
		Transform2D transform2D = visual.Transform;
		Color color = Multiply(snapshot.Modulate, visual.Modulate);
		if (visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot)
		{
			if (visual.Visible && (!GodotObject.IsInstanceValid(slot) || !snapshot.Sprite.TryGetManagedSlotTransformForRender(slot, out var _)))
			{
				result.Failure = new AdobeAnimateCpuVisualFailure("external-slot-transform-unavailable", GetCpuRootIdentity(snapshot.Sprite), "The visible external Slot visual has no render transform.");
				return;
			}
			if (GodotObject.IsInstanceValid(slot))
			{
				if (snapshot.Sprite.TryGetManagedSlotTransformForRender(slot, out var transform2))
				{
					transform2D = transform2 * transform2D;
				}
				color = Multiply(color, slot.Modulate);
				num2 = Math.Max(0, slot.ResolveDrawLayerId() - 1);
				num3 = (snapshot.Sprite.TryGetFrameDrawOrderForRender(frameIndex, num2, out var drawOrder) ? drawOrder : num2);
			}
		}
		AdobeAnimateSortPath sortPath = default(AdobeAnimateSortPath);
		if (buildSortPath)
		{
			int[] nestedPath2 = AppendNestedPath(nestedPath, num, visual.Descriptor.RelativeZ, activeIndex);
			sortPath = new AdobeAnimateSortPath(rootZIndex, rootTreeOrderPath, sortLayerOverride ?? num2, sortDrawOverride ?? num3, stableOrder++, nestedPath2);
		}
		AdobeAnimateDrawItem drawItem = default(AdobeAnimateDrawItem);
		if (visual.Visible && GodotObject.IsInstanceValid(sprite) && GodotObject.IsInstanceValid(visual.Texture))
		{
			Rect2 rect = ResolveSpriteSourceRect(sprite, visual.Texture);
			Vector2 size = rect.Size;
			Vector2 originPos = ResolveSpriteDrawOrigin(sprite, size);
			Transform2D transform2D2 = new Transform2D((sprite.FlipH ? (-Vector2.Right) : Vector2.Right) * size.X, (sprite.FlipV ? (-Vector2.Down) : Vector2.Down) * size.Y, originPos);
			Transform2D transform3 = ((visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.World) ? (visual.RegisteredWorldTransform * transform2D2) : (snapshot.GlobalTransform * transform2D * transform2D2));
			drawItem = new AdobeAnimateDrawItem(transform3, size, NormalizeRect(rect, visual.Texture.GetSize()), 0, color, sortPath, clipEnabled: false, 0f, 0f, snapshot.Sprite);
		}
		RecordCpuClassification(AdobeAnimateCpuVisualClassifier.ClassifyExternalVisual(in visual, expectedAtlasArrayRid, in drawItem, in sortPath), output, result);
	}

	private static void RecordCpuClassification(in AdobeAnimateCpuVisualClassification classification, List<AdobeAnimateDrawItem> output, AdobeAnimateCpuVisualBuildResult result)
	{
		switch (classification.Disposition)
		{
		case AdobeAnimateCpuVisualDisposition.MergedMesh:
		{
			AdobeAnimateDrawItem drawItem = classification.DrawItem;
			AdobeAnimateExternalTextureAtlasAllocation allocation = classification.Allocation;
			output.Add(new AdobeAnimateDrawItem(drawItem.Transform, allocation.Rect.Size, NormalizeRect(allocation.Rect, allocation.TextureArraySize), allocation.AtlasPage, drawItem.Color, drawItem.SortPath, drawItem.ClipEnabled, drawItem.ClipUp, drawItem.ClipDown, drawItem.Owner, drawItem.UseShaderPose, drawItem.PoseTexel, drawItem.PoseLayer, drawItem.PoseFrameT, drawItem.PoseOffset, drawItem.UseVisualOverride, drawItem.ClipLeft, drawItem.ClipRight));
			result.MergedVisibleItems++;
			break;
		}
		case AdobeAnimateCpuVisualDisposition.NativeSprite:
			result.NativeSpriteItems.Add(classification.NativeItem);
			result.NativeVisibleItems++;
			break;
		case AdobeAnimateCpuVisualDisposition.Invalid:
			result.Failure = classification.Failure;
			break;
		case AdobeAnimateCpuVisualDisposition.IgnoredInvisible:
			break;
		}
	}

	private static string GetCpuRootIdentity(AdobeAnimateSprite sprite)
	{
		if (!GodotObject.IsInstanceValid(sprite))
		{
			return "adobe-animate-root";
		}
		string text = (sprite.IsInsideTree() ? sprite.GetPath().ToString() : string.Empty);
		if (!string.IsNullOrEmpty(text))
		{
			return text;
		}
		return "adobe-animate-root:" + sprite.Name;
	}

	private static bool TryAssignNativeDrawBands(AdobeAnimateSprite sprite, AdobeAnimateCpuVisualBuildResult result)
	{
		if (result.NativeSpriteItems.Count == 0)
		{
			return true;
		}
		if (result.MergedDrawItems.Count == 0)
		{
			result.Failure = new AdobeAnimateCpuVisualFailure("no-merged-draw-items", GetCpuRootIdentity(sprite), "A CPU root with native Sprite2D exceptions still requires one root Mesh.");
			return false;
		}
		AdobeAnimateSortPath adobeAnimateSortPath = result.MergedDrawItems[0].SortPath;
		AdobeAnimateSortPath other = adobeAnimateSortPath;
		for (int i = 1; i < result.MergedDrawItems.Count; i++)
		{
			AdobeAnimateSortPath sortPath = result.MergedDrawItems[i].SortPath;
			if (sortPath.CompareTo(adobeAnimateSortPath) < 0)
			{
				adobeAnimateSortPath = sortPath;
			}
			if (sortPath.CompareTo(other) > 0)
			{
				other = sortPath;
			}
		}
		for (int j = 0; j < result.NativeSpriteItems.Count; j++)
		{
			AdobeAnimateCpuNativeSpriteItem adobeAnimateCpuNativeSpriteItem = result.NativeSpriteItems[j];
			if (adobeAnimateCpuNativeSpriteItem.SortPath.CompareTo(adobeAnimateSortPath) < 0)
			{
				result.NativeSpriteItems[j] = adobeAnimateCpuNativeSpriteItem.WithDrawBand(AdobeAnimateCpuNativeDrawBand.BehindRootMesh);
				continue;
			}
			if (adobeAnimateCpuNativeSpriteItem.SortPath.CompareTo(other) > 0)
			{
				result.NativeSpriteItems[j] = adobeAnimateCpuNativeSpriteItem.WithDrawBand(AdobeAnimateCpuNativeDrawBand.InFrontOfRootMesh);
				continue;
			}
			result.Failure = new AdobeAnimateCpuVisualFailure("native-order-interleave-unsupported", adobeAnimateCpuNativeSpriteItem.ResourceIdentity, "The native Sprite2D would split the single root Mesh draw call and cannot preserve draw order.");
			return false;
		}
		return true;
	}

	private static void AppendChildSprites(AdobeAnimateRenderSnapshot snapshot, List<AdobeAnimateDrawItem> output, AdobeAnimateSprite[] children, int[] nestedPath, int depth, int rootZIndex, int[] rootTreeOrderPath, int? sortLayerOverride, int? sortDrawOverride, bool buildSortPath, HashSet<AdobeAnimateSprite> visited, TextureLayered poseTextureArray, long externalVisualFrameVersion, int frameIndex, ref int stableOrder, AdobeAnimateCpuVisualBuildResult cpuResult = null, Rid cpuAtlasArrayRid = default(Rid))
	{
		foreach (AdobeAnimateSprite adobeAnimateSprite in children)
		{
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite) || !snapshot.Sprite.TryGetChildRenderLayerForRender(adobeAnimateSprite, out var layerId) || layerId < 0 || !adobeAnimateSprite.TryBuildRenderSnapshot(out var snapshot2))
			{
				continue;
			}
			int? sortLayerOverride2 = null;
			int? sortDrawOverride2 = null;
			int[] nestedPath2 = Array.Empty<int>();
			if (buildSortPath)
			{
				int drawOrder = layerId;
				if (!snapshot.Sprite.TryGetFrameDrawOrderForRender(frameIndex, layerId, out drawOrder))
				{
					drawOrder = layerId;
				}
				bool num = sortLayerOverride.HasValue || sortDrawOverride.HasValue;
				sortLayerOverride2 = sortLayerOverride ?? layerId;
				sortDrawOverride2 = sortDrawOverride ?? drawOrder;
				nestedPath2 = (num ? AppendNestedPath(nestedPath, layerId, drawOrder, adobeAnimateSprite.GetIndex()) : AppendNestedPath(nestedPath, adobeAnimateSprite.GetIndex()));
			}
			BuildSpriteTree(snapshot2, output, nestedPath2, depth + 1, rootZIndex, rootTreeOrderPath, sortLayerOverride2, sortDrawOverride2, visited, poseTextureArray, externalVisualFrameVersion, ref stableOrder, cpuResult, cpuAtlasArrayRid);
			if (cpuResult != null && cpuResult.Failure.IsValid)
			{
				break;
			}
		}
	}

	private static bool CanUseShaderPose(AdobeAnimateRenderSnapshot snapshot, TextureLayered poseTextureArray)
	{
		if (snapshot.ClipBlend.Enabled)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.render.shaderPoseMiss.clipBlend");
			return false;
		}
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		if (definition == null)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.render.shaderPoseMiss.noDefinition");
			return false;
		}
		if (!definition.UsesGpuPoseTextureArray || !definition.GpuPoseTextureRid.IsValid || definition.GpuPoseTextureSize.X <= 0 || definition.GpuPoseTextureSize.Y <= 0 || !GodotObject.IsInstanceValid(definition.GpuPoseTextureArray))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.render.shaderPoseMiss.definitionNoPose");
			return false;
		}
		if (!GodotObject.IsInstanceValid(poseTextureArray))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.render.shaderPoseMiss.noUnifiedPose");
			return false;
		}
		if (!IsSameTextureArray(definition.GpuPoseTextureArray, poseTextureArray))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.render.shaderPoseMiss.textureMismatch");
			return false;
		}
		return true;
	}

	private static bool ResolveMediaRect(AdobeAnimateRenderSnapshot snapshot, int mediaId, out Rect2 rect, out int atlasLayer, out Vector2 atlasSize)
	{
		rect = default(Rect2);
		atlasLayer = 0;
		atlasSize = Vector2.One;
		if (mediaId < 0)
		{
			return false;
		}
		if (IsMediaReplaceActive(snapshot, mediaId))
		{
			rect = snapshot.MediaReplaceRects[mediaId];
			atlasLayer = ((mediaId < snapshot.MediaReplaceAtlasPageCount) ? Math.Max(0, snapshot.MediaReplaceAtlasPageValues[mediaId]) : 0);
			atlasSize = ((snapshot.MediaReplaceAtlasArraySize.X > 0f && snapshot.MediaReplaceAtlasArraySize.Y > 0f) ? snapshot.MediaReplaceAtlasArraySize : (GodotObject.IsInstanceValid(snapshot.MediaReplaceAtlas) ? snapshot.MediaReplaceAtlas.GetSize() : Vector2.One));
			if (rect.Size.X > 0f)
			{
				return rect.Size.Y > 0f;
			}
			return false;
		}
		AdobeAnimateRuntimeDefinition definition = snapshot.Definition;
		if (definition == null || definition.MediaRects == null || mediaId >= definition.MediaRects.Length)
		{
			return false;
		}
		rect = definition.MediaRects[mediaId];
		atlasLayer = ((definition.MediaAtlasPages != null && mediaId < definition.MediaAtlasPages.Length) ? Math.Max(0, definition.MediaAtlasPages[mediaId]) : Math.Max(0, definition.BaseAtlasPage));
		atlasSize = ResolveAtlasSize(definition, mediaId);
		if (rect.Size.X > 0f)
		{
			return rect.Size.Y > 0f;
		}
		return false;
	}

	private static bool IsMediaReplaceActive(AdobeAnimateRenderSnapshot snapshot, int mediaId)
	{
		if (!snapshot.HasMediaReplace || snapshot.MediaReplaceUse == null || snapshot.MediaReplaceRect == null || mediaId < 0 || mediaId >= snapshot.MediaReplaceLimit)
		{
			return false;
		}
		if (mediaId < 64)
		{
			return (snapshot.MediaReplaceUseMask & (ulong)(1L << mediaId)) != 0;
		}
		if (snapshot.MediaReplaceUseMaskOverflow && mediaId < snapshot.MediaReplaceUseValues.Length)
		{
			return snapshot.MediaReplaceUseValues[mediaId];
		}
		return false;
	}

	private static bool IsLayerVisible(AdobeAnimateRenderSnapshot snapshot, int layerId)
	{
		if (snapshot.AllLayersVisible)
		{
			return true;
		}
		if (snapshot.CanUseLayerMask)
		{
			return IsLayerVisible(snapshot.LayerMask, layerId);
		}
		if (snapshot.LayerVisible != null && layerId >= 0 && layerId < snapshot.LayerVisibleCount)
		{
			return snapshot.LayerVisibleValues[layerId];
		}
		return true;
	}

	private static bool IsLayerVisible(ulong layerMask, int layerId)
	{
		if (layerId >= 0 && layerId < 64)
		{
			return (layerMask & (ulong)(1L << layerId)) != 0;
		}
		return true;
	}

	private static bool IsSameTextureArray(TextureLayered left, TextureLayered right)
	{
		if (left == right)
		{
			return true;
		}
		if (!GodotObject.IsInstanceValid(left) || !GodotObject.IsInstanceValid(right))
		{
			return false;
		}
		Rid rid = left.GetRid();
		Rid rid2 = right.GetRid();
		if (rid.IsValid && rid2.IsValid)
		{
			return rid == rid2;
		}
		return false;
	}

	private static int ResolveFrameIndex(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0)
		{
			return 0;
		}
		float num = snapshot.FrameFloat;
		if (snapshot.ClipRange != Vector2I.Zero)
		{
			num = Mathf.Clamp(num, snapshot.ClipRange.X, Math.Max(snapshot.ClipRange.X, snapshot.ClipRange.Y - 1));
		}
		return Mathf.Clamp(Mathf.FloorToInt(num), 0, definition.Frames.Length - 1);
	}

	private static float ResolveFrameInterpolation(float frameFloat)
	{
		return Mathf.Clamp(frameFloat - Mathf.Floor(frameFloat), 0f, 1f);
	}

	private static PackedSlicePose[] PrepareCpuPoseSampling(AdobeAnimateRuntimeDefinition definition, float interpolationT, AdobeAnimateClipBlendState clipBlend)
	{
		if (!AdobeAnimateDefinitionCache.EnsureCpuPoseData(definition))
		{
			return Array.Empty<PackedSlicePose>();
		}
		float num = (clipBlend.Enabled ? ResolveFrameInterpolation(clipBlend.FromFrameFloat) : 0f);
		if (interpolationT > 0f || num > 0f)
		{
			AdobeAnimateDefinitionCache.EnsureCpuInterpolationData(definition);
		}
		return definition.Slices ?? Array.Empty<PackedSlicePose>();
	}

	private static PackedSlicePose SampleSlice(AdobeAnimateRuntimeDefinition definition, PackedSlicePose[] slices, int sourceIndex, float interpolationT)
	{
		if (slices == null || (uint)sourceIndex >= (uint)slices.Length)
		{
			return default(PackedSlicePose);
		}
		PackedSlicePose result = slices[sourceIndex];
		if (interpolationT <= 0f)
		{
			return result;
		}
		if (definition.HasNextSliceDeltas == null || sourceIndex >= definition.HasNextSliceDeltas.Length || !definition.HasNextSliceDeltas[sourceIndex])
		{
			return result;
		}
		result.Xx += GetDelta(definition.NextDeltaXx, sourceIndex) * interpolationT;
		result.Xy += GetDelta(definition.NextDeltaXy, sourceIndex) * interpolationT;
		result.Yx += GetDelta(definition.NextDeltaYx, sourceIndex) * interpolationT;
		result.Yy += GetDelta(definition.NextDeltaYy, sourceIndex) * interpolationT;
		result.Ox += GetDelta(definition.NextDeltaOx, sourceIndex) * interpolationT;
		result.Oy += GetDelta(definition.NextDeltaOy, sourceIndex) * interpolationT;
		result.Alpha += GetDelta(definition.NextDeltaAlpha, sourceIndex) * interpolationT;
		return result;
	}

	private static void BlendSliceFromPreviousClip(AdobeAnimateRenderSnapshot snapshot, AdobeAnimateRuntimeDefinition definition, PackedSlicePose[] cpuPoseSlices, PackedFrame targetFrame, int targetSourceIndex, ref PackedSlicePose target)
	{
		AdobeAnimateClipBlendState clipBlend = snapshot.ClipBlend;
		if (!clipBlend.Enabled || clipBlend.Weight >= 1f || definition?.Frames == null || definition.Frames.Length == 0 || targetSourceIndex < 0)
		{
			return;
		}
		int num = Mathf.Clamp(Mathf.FloorToInt(clipBlend.FromFrameFloat), 0, definition.Frames.Length - 1);
		PackedFrame sourceFrame = definition.Frames[num];
		if (AdobeAnimateDefinitionCache.TryGetMatchingSliceIndexInFrame(definition, sourceFrame, targetFrame, targetSourceIndex, out var sourceAbsoluteIndex))
		{
			float interpolationT = ResolveFrameInterpolation(clipBlend.FromFrameFloat);
			PackedSlicePose packedSlicePose = SampleSlice(definition, cpuPoseSlices, sourceAbsoluteIndex, interpolationT);
			if (packedSlicePose.MediaId != 65535)
			{
				float weight = clipBlend.Weight;
				target.Xx = Mathf.Lerp(packedSlicePose.Xx, target.Xx, weight);
				target.Xy = Mathf.Lerp(packedSlicePose.Xy, target.Xy, weight);
				target.Yx = Mathf.Lerp(packedSlicePose.Yx, target.Yx, weight);
				target.Yy = Mathf.Lerp(packedSlicePose.Yy, target.Yy, weight);
				target.Ox = Mathf.Lerp(packedSlicePose.Ox, target.Ox, weight);
				target.Oy = Mathf.Lerp(packedSlicePose.Oy, target.Oy, weight);
				target.Alpha = Mathf.Lerp(packedSlicePose.Alpha, target.Alpha, weight);
			}
		}
	}

	private static Transform2D BuildSliceTransform(Transform2D parent, Vector2 offset, PackedSlicePose slice, Vector2 sourceSize)
	{
		Transform2D transform2D = new Transform2D(new Vector2(slice.Xx * sourceSize.X, slice.Xy * sourceSize.X), new Vector2(slice.Yx * sourceSize.Y, slice.Yy * sourceSize.Y), new Vector2(slice.Ox + offset.X, slice.Oy + offset.Y));
		return parent * transform2D;
	}

	internal static Rect2 ResolveSpriteSourceRect(Sprite2D sprite, Texture2D texture)
	{
		if (sprite.RegionEnabled)
		{
			return sprite.RegionRect;
		}
		Vector2 size = texture.GetSize();
		int num = Math.Max(1, sprite.Hframes);
		int num2 = Math.Max(1, sprite.Vframes);
		if (num <= 1 && num2 <= 1)
		{
			return new Rect2(Vector2.Zero, size);
		}
		Vector2 size2 = new Vector2(size.X / (float)num, size.Y / (float)num2);
		int num3 = Math.Clamp(sprite.Frame, 0, num * num2 - 1);
		int num4 = num3 % num;
		int num5 = num3 / num;
		return new Rect2(new Vector2((float)num4 * size2.X, (float)num5 * size2.Y), size2);
	}

	internal static Vector2 ResolveSpriteDrawOrigin(Sprite2D sprite, Vector2 size)
	{
		Vector2 offset = sprite.Offset;
		if (sprite.Centered)
		{
			offset -= size * 0.5f;
		}
		if (sprite.FlipH)
		{
			offset.X += size.X;
		}
		if (sprite.FlipV)
		{
			offset.Y += size.Y;
		}
		return offset;
	}

	private static Vector2 ResolveAtlasSize(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		if (definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f)
		{
			return definition.AtlasTextureArraySize;
		}
		if (definition.MediaTextureSizes != null && mediaId >= 0 && mediaId < definition.MediaTextureSizes.Length)
		{
			Vector2 result = definition.MediaTextureSizes[mediaId];
			if (result.X > 0f && result.Y > 0f)
			{
				return result;
			}
		}
		if (definition.BaseAtlasSize.X > 0f && definition.BaseAtlasSize.Y > 0f)
		{
			return definition.BaseAtlasSize;
		}
		if (GodotObject.IsInstanceValid(definition.BaseAtlas))
		{
			return definition.BaseAtlas.GetSize();
		}
		return Vector2.One;
	}

	internal static Rect2 NormalizeRect(Rect2 rect, Vector2 atlasSize)
	{
		float num = Math.Max(1f, atlasSize.X);
		float num2 = Math.Max(1f, atlasSize.Y);
		return new Rect2(new Vector2(rect.Position.X / num, rect.Position.Y / num2), new Vector2(rect.Size.X / num, rect.Size.Y / num2));
	}

	private static int[] AppendNestedPath(int[] nestedPath, int value)
	{
		return GetOrCreateNestedPath(nestedPath, 1, value, 0, 0);
	}

	private static int[] AppendNestedPath(int[] nestedPath, int first, int second)
	{
		return GetOrCreateNestedPath(nestedPath, 2, first, second, 0);
	}

	private static int[] AppendNestedPath(int[] nestedPath, int first, int second, int third)
	{
		return GetOrCreateNestedPath(nestedPath, 3, first, second, third);
	}

	private static int[] GetOrCreateNestedPath(int[] nestedPath, int appendedCount, int first, int second, int third)
	{
		int[] array = ((nestedPath == null || nestedPath.Length == 0) ? Array.Empty<int>() : nestedPath);
		NestedSortPathKey key = new NestedSortPathKey(array, appendedCount, first, second, third);
		Dictionary<NestedSortPathKey, int[]> dictionary = _nestedSortPathCache ?? (_nestedSortPathCache = new Dictionary<NestedSortPathKey, int[]>(4096));
		if (dictionary.TryGetValue(key, out var value))
		{
			return value;
		}
		if (dictionary.Count >= 65536)
		{
			dictionary.Clear();
		}
		int num = array.Length;
		int[] array2 = new int[num + appendedCount];
		if (num > 0)
		{
			Array.Copy(array, array2, num);
		}
		array2[num] = first;
		if (appendedCount > 1)
		{
			array2[num + 1] = second;
		}
		if (appendedCount > 2)
		{
			array2[num + 2] = third;
		}
		dictionary[key] = array2;
		return array2;
	}

	private static float GetDelta(float[] values, int index)
	{
		if (values == null || index < 0 || index >= values.Length)
		{
			return 0f;
		}
		return values[index];
	}

	internal static Color Multiply(Color left, Color right)
	{
		return new Color(left.R * right.R, left.G * right.G, left.B * right.B, left.A * right.A);
	}
}
