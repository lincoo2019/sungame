using System;
using System.Collections.Generic;
using Godot;

internal static class AdobeAnimateGpuRenderGraphBuilder
{
	private readonly struct PendingRenderSlot(AdobeAnimateSortPath sortPath, AdobeAnimateGpuRenderSlot slot)
	{
		public AdobeAnimateSortPath SortPath { get; } = sortPath;

		public AdobeAnimateGpuRenderSlot Slot { get; } = slot;
	}

	private sealed class BuildWorkspace
	{
		public readonly List<AdobeAnimateGpuRenderOwner> Owners = new List<AdobeAnimateGpuRenderOwner>();

		public readonly List<AdobeAnimateSprite> Sprites = new List<AdobeAnimateSprite>();

		public readonly List<AdobeAnimateGpuFrameSlotEntry> Lookup = new List<AdobeAnimateGpuFrameSlotEntry>();

		public readonly List<int> AttachmentPoseLookup = new List<int>();

		public readonly List<AdobeAnimateGpuManagedAttachmentPose> ManagedAttachmentPoses = new List<AdobeAnimateGpuManagedAttachmentPose>();

		public readonly List<AdobeAnimateGpuManagedVisualBinding> ManagedVisualBindings = new List<AdobeAnimateGpuManagedVisualBinding>();

		public readonly List<PendingRenderSlot> PendingSlots = new List<PendingRenderSlot>();

		public readonly List<AdobeAnimateGpuRenderSlot> RenderSlots = new List<AdobeAnimateGpuRenderSlot>();

		public readonly HashSet<AdobeAnimateSprite> Visited = new HashSet<AdobeAnimateSprite>();

		public void Clear()
		{
			Owners.Clear();
			Sprites.Clear();
			Lookup.Clear();
			AttachmentPoseLookup.Clear();
			ManagedAttachmentPoses.Clear();
			ManagedVisualBindings.Clear();
			PendingSlots.Clear();
			RenderSlots.Clear();
			Visited.Clear();
		}
	}

	private const ulong FnvOffsetBasis = 1469598103934665603uL;

	private const ulong FnvPrime = 1099511628211uL;

	private const int MaxGpuOwnerDepth = 8;

	[ThreadStatic]
	private static BuildWorkspace _threadWorkspace;

	public static bool TryBuildSingleOwner(AdobeAnimateRuntimeDefinition definition, out AdobeAnimateGpuRenderGraphDefinition graph)
	{
		graph = null;
		if (!IsValidDefinition(definition))
		{
			return false;
		}
		int num = definition.GpuRenderLocalSlots.Length;
		int num2 = 0;
		AdobeAnimateGpuRenderOwner[] owners = new AdobeAnimateGpuRenderOwner[1]
		{
			new AdobeAnimateGpuRenderOwner(definition, -1, num2, 0, AdobeAnimateGpuAttachmentKind.Root, -1, -1, -1, num, 0, usePos: true, useRotate: true, useScale: true, useSkew: true, Vector2.Zero, Vector2.Zero, 0f, useFollowVisible: false, Transform2D.Identity)
		};
		AdobeAnimateGpuRenderSlot[] array = new AdobeAnimateGpuRenderSlot[num];
		for (int i = 0; i < num; i++)
		{
			array[i] = new AdobeAnimateGpuRenderSlot(0, num2, i, AdobeAnimateGpuRenderSlotKind.Pose, -1);
		}
		AdobeAnimateGpuFrameSlotEntry[] frameSlotLookup = (AdobeAnimateGpuFrameSlotEntry[])definition.GpuFrameSlotLookup.Clone();
		int[] attachmentPoseLookup = Array.Empty<int>();
		AdobeAnimateGpuManagedAttachmentPose[] managedAttachmentPoses = Array.Empty<AdobeAnimateGpuManagedAttachmentPose>();
		AdobeAnimateGpuManagedVisualBinding[] managedVisualBindings = Array.Empty<AdobeAnimateGpuManagedVisualBinding>();
		graph = new AdobeAnimateGpuRenderGraphDefinition(ComputeSignature(owners, array, attachmentPoseLookup, managedAttachmentPoses, managedVisualBindings, 0uL), owners, array, frameSlotLookup, attachmentPoseLookup, managedAttachmentPoses, managedVisualBindings, 0uL, 0);
		return true;
	}

	public static bool TryBuild(AdobeAnimateSprite root, out AdobeAnimateGpuRenderGraphDefinition graph)
	{
		AdobeAnimateSprite[] ownerSprites;
		string failureReason;
		return TryBuild(root, null, out graph, out ownerSprites, out failureReason);
	}

	internal static bool TryBuild(AdobeAnimateSprite root, out AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateSprite[] ownerSprites)
	{
		string failureReason;
		return TryBuild(root, null, out graph, out ownerSprites, out failureReason);
	}

	internal static bool TryBuild(AdobeAnimateSprite root, IReadOnlyDictionary<ulong, AdobeAnimateGpuRenderGraphDefinition> sharedDefinitions, out AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateSprite[] ownerSprites)
	{
		string failureReason;
		return TryBuild(root, sharedDefinitions, out graph, out ownerSprites, out failureReason);
	}

	internal static bool TryBuild(AdobeAnimateSprite root, out AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateSprite[] ownerSprites, out string failureReason)
	{
		return TryBuild(root, null, out graph, out ownerSprites, out failureReason);
	}

	private static bool TryBuild(AdobeAnimateSprite root, IReadOnlyDictionary<ulong, AdobeAnimateGpuRenderGraphDefinition> sharedDefinitions, out AdobeAnimateGpuRenderGraphDefinition graph, out AdobeAnimateSprite[] ownerSprites, out string failureReason)
	{
		graph = null;
		ownerSprites = Array.Empty<AdobeAnimateSprite>();
		failureReason = "";
		if (!GodotObject.IsInstanceValid(root))
		{
			failureReason = "root instance is invalid";
			return false;
		}
		BuildWorkspace buildWorkspace = _threadWorkspace ?? (_threadWorkspace = new BuildWorkspace());
		buildWorkspace.Clear();
		List<AdobeAnimateGpuRenderOwner> owners = buildWorkspace.Owners;
		List<AdobeAnimateSprite> sprites = buildWorkspace.Sprites;
		List<AdobeAnimateGpuFrameSlotEntry> lookup = buildWorkspace.Lookup;
		List<int> attachmentPoseLookup = buildWorkspace.AttachmentPoseLookup;
		List<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses = buildWorkspace.ManagedAttachmentPoses;
		List<AdobeAnimateGpuManagedVisualBinding> managedVisualBindings = buildWorkspace.ManagedVisualBindings;
		List<PendingRenderSlot> pendingSlots = buildWorkspace.PendingSlots;
		HashSet<AdobeAnimateSprite> visited = buildWorkspace.Visited;
		if (!TryCollectOwner(root, -1, 0, AdobeAnimateGpuAttachmentSettings.Root, -1, null, null, Array.Empty<int>(), owners, sprites, lookup, attachmentPoseLookup, managedAttachmentPoses, managedVisualBindings, pendingSlots, visited, out var atlasArrayRid, out var poseArrayRid, out failureReason))
		{
			buildWorkspace.Clear();
			return false;
		}
		if (owners.Count == 0 || pendingSlots.Count == 0)
		{
			failureReason = $"graph is empty owners={owners.Count} slots={pendingSlots.Count}";
			buildWorkspace.Clear();
			return false;
		}
		pendingSlots.Sort((PendingRenderSlot left, PendingRenderSlot right) => left.SortPath.CompareTo(right.SortPath));
		List<AdobeAnimateGpuRenderSlot> renderSlots = buildWorkspace.RenderSlots;
		for (int num = 0; num < pendingSlots.Count; num++)
		{
			renderSlots.Add(pendingSlots[num].Slot);
		}
		visited.Clear();
		ulong managedTopologySignature = ComputeManagedTopologySignature(root, visited);
		ulong num2 = ComputeSignature(owners, renderSlots, attachmentPoseLookup, managedAttachmentPoses, managedVisualBindings, managedTopologySignature);
		ownerSprites = sprites.ToArray();
		bool flag = num2 != 0L && atlasArrayRid.IsValid && poseArrayRid.IsValid;
		if (!flag)
		{
			failureReason = $"graph output is invalid signature=0x{num2:X16} atlas={atlasArrayRid.IsValid} pose={poseArrayRid.IsValid}";
			buildWorkspace.Clear();
			return false;
		}
		if (sharedDefinitions != null && sharedDefinitions.TryGetValue(num2, out var value))
		{
			graph = value;
		}
		else
		{
			for (int num3 = 0; num3 < owners.Count; num3++)
			{
				lookup.AddRange(owners[num3].Definition.GpuFrameSlotLookup);
			}
			AdobeAnimateGpuRenderOwner[] owners2 = owners.ToArray();
			graph = new AdobeAnimateGpuRenderGraphDefinition(num2, owners2, renderSlots.ToArray(), lookup.ToArray(), attachmentPoseLookup.ToArray(), managedAttachmentPoses.ToArray(), managedVisualBindings.ToArray(), managedTopologySignature, GetMaxOwnerDepth(owners2));
		}
		buildWorkspace.Clear();
		return flag;
	}

	private static bool TryCollectOwner(AdobeAnimateSprite owner, int parentOwnerIndex, int depth, AdobeAnimateGpuAttachmentSettings attachment, int renderLayer, int? sortLayerOverride, int? sortDrawOverride, int[] nestedPath, List<AdobeAnimateGpuRenderOwner> owners, List<AdobeAnimateSprite> sprites, List<AdobeAnimateGpuFrameSlotEntry> lookup, List<int> attachmentPoseLookup, List<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses, List<AdobeAnimateGpuManagedVisualBinding> managedVisualBindings, List<PendingRenderSlot> pendingSlots, HashSet<AdobeAnimateSprite> visited, out Rid atlasArrayRid, out Rid poseArrayRid, out string failureReason)
	{
		atlasArrayRid = default(Rid);
		poseArrayRid = default(Rid);
		failureReason = "";
		if (depth > 8)
		{
			failureReason = $"owner depth {depth} exceeds {8}";
			return false;
		}
		if (!GodotObject.IsInstanceValid(owner))
		{
			failureReason = "owner instance is invalid";
			return false;
		}
		string ownerLabel = GetOwnerLabel(owner);
		if (!visited.Add(owner))
		{
			failureReason = "owner cycle detected at " + ownerLabel;
			return false;
		}
		if (!owner.TryGetGpuGraphOwnerDefinitionForRender(out var definition, out var failureReason2))
		{
			failureReason = "owner " + ownerLabel + " rejected: " + failureReason2;
			return false;
		}
		string definitionFailure = GetDefinitionFailure(definition);
		if (!string.IsNullOrEmpty(definitionFailure))
		{
			failureReason = "owner " + ownerLabel + " definition rejected: " + definitionFailure;
			return false;
		}
		atlasArrayRid = definition.AtlasTextureArrayRid;
		poseArrayRid = definition.GpuPoseTextureArray.GetRid();
		if (!atlasArrayRid.IsValid || !poseArrayRid.IsValid)
		{
			failureReason = $"owner {ownerLabel} has invalid texture RID atlas={atlasArrayRid.IsValid} pose={poseArrayRid.IsValid}";
			return false;
		}
		int count = owners.Count;
		int num = count;
		int frameLookupCount = GetFrameLookupCount(owners);
		owners.Add(new AdobeAnimateGpuRenderOwner(definition, parentOwnerIndex, num, depth, attachment.Kind, attachment.Key, attachment.PoseLookupBase, renderLayer, definition.GpuRenderLocalSlots.Length, frameLookupCount, attachment.UsePos, attachment.UseRotate, attachment.UseScale, attachment.UseSkew, attachment.ParentOffset, attachment.SlotOffset, attachment.OffsetRotate, attachment.UseFollowVisible, attachment.LocalTransform));
		sprites.Add(owner);
		for (int i = 0; i < definition.GpuRenderLocalSlots.Length; i++)
		{
			AdobeAnimateGpuLocalSlot adobeAnimateGpuLocalSlot = definition.GpuRenderLocalSlots[i];
			int[] nestedPath2 = ((sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, adobeAnimateGpuLocalSlot.LayerId, adobeAnimateGpuLocalSlot.DrawOrder) : nestedPath);
			AdobeAnimateSortPath sortPath = new AdobeAnimateSortPath(0, Array.Empty<int>(), sortLayerOverride ?? adobeAnimateGpuLocalSlot.LayerId, sortDrawOverride ?? adobeAnimateGpuLocalSlot.DrawOrder, num * 1000000 + i, nestedPath2);
			pendingSlots.Add(new PendingRenderSlot(sortPath, new AdobeAnimateGpuRenderSlot(count, num, i, AdobeAnimateGpuRenderSlotKind.Pose, -1)));
		}
		if (!TryAppendManagedSpriteSlots(owner, definition, count, num, sortLayerOverride, sortDrawOverride, nestedPath, managedAttachmentPoses, managedVisualBindings, pendingSlots, out failureReason))
		{
			return false;
		}
		if (!TryAppendExternalVisualSlots(owner, definition, count, num, sortLayerOverride, sortDrawOverride, nestedPath, managedAttachmentPoses, managedVisualBindings, pendingSlots, out failureReason))
		{
			return false;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = owner.GetSpriteChildrenForRender();
		for (int j = 0; j < spriteChildrenForRender.Length; j++)
		{
			AdobeAnimateSprite adobeAnimateSprite = spriteChildrenForRender[j];
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite))
			{
				failureReason = $"owner {ownerLabel} child[{j}] is invalid";
				return false;
			}
			if (!adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
			{
				string ownerLabel2 = GetOwnerLabel(adobeAnimateSprite);
				if (!owner.TryGetChildRenderLayerForRender(adobeAnimateSprite, out var layerId) || layerId < 0)
				{
					failureReason = "owner " + ownerLabel + " cannot resolve render layer for child " + ownerLabel2;
					return false;
				}
				if (!owner.TryGetGpuGraphChildAttachmentForRender(adobeAnimateSprite, out var attachment2))
				{
					failureReason = "owner " + ownerLabel + " cannot resolve attachment for child " + ownerLabel2;
					return false;
				}
				if (!TryAppendAttachmentPoseLookup(definition, attachment2.Key, attachmentPoseLookup, out var attachmentPoseLookupBase))
				{
					failureReason = $"owner {ownerLabel} attachment key {attachment2.Key} has no usable per-frame pose lookup for child {ownerLabel2}";
					return false;
				}
				attachment2 = attachment2.WithPoseLookupBase(attachmentPoseLookupBase);
				int num2 = ResolveStableLayerDrawOrder(definition, layerId);
				bool num3 = sortLayerOverride.HasValue || sortDrawOverride.HasValue;
				int? sortLayerOverride2 = sortLayerOverride ?? layerId;
				int? sortDrawOverride2 = sortDrawOverride ?? num2;
				int[] nestedPath3 = (num3 ? AppendNestedPath(nestedPath, layerId, num2, adobeAnimateSprite.GetIndex()) : AppendNestedPath(nestedPath, adobeAnimateSprite.GetIndex()));
				if (!TryCollectOwner(adobeAnimateSprite, count, depth + 1, attachment2, layerId, sortLayerOverride2, sortDrawOverride2, nestedPath3, owners, sprites, lookup, attachmentPoseLookup, managedAttachmentPoses, managedVisualBindings, pendingSlots, visited, out var atlasArrayRid2, out var poseArrayRid2, out var failureReason3))
				{
					failureReason = $"owner {ownerLabel} child {ownerLabel2} failed: {failureReason3}";
					return false;
				}
				if (atlasArrayRid2 != atlasArrayRid || poseArrayRid2 != poseArrayRid)
				{
					failureReason = $"owner {ownerLabel} child {ownerLabel2} uses a different atlas or pose array";
					return false;
				}
			}
		}
		return true;
	}

	private static bool IsValidDefinition(AdobeAnimateRuntimeDefinition definition)
	{
		return string.IsNullOrEmpty(GetDefinitionFailure(definition));
	}

	private static string GetDefinitionFailure(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return "definition is null";
		}
		if (!definition.GpuRenderSlotOrderStable)
		{
			return "render slot order is not stable across frames";
		}
		if (definition.Frames == null || definition.Frames.Length == 0 || definition.GpuRenderLocalSlots == null || definition.GpuRenderLocalSlots.Length == 0 || definition.GpuFrameSlotLookup == null || definition.GpuFrameSlotLookup.Length != definition.Frames.Length * definition.GpuRenderLocalSlots.Length || !definition.UsesGpuPoseTextureArray || !definition.GpuPoseTextureRid.IsValid || !GodotObject.IsInstanceValid(definition.GpuPoseTextureArray) || !definition.AtlasTextureArrayRid.IsValid || !GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			return "definition GPU frame lookup or texture arrays are incomplete";
		}
		return "";
	}

	private static string GetOwnerLabel(AdobeAnimateSprite owner)
	{
		if (!GodotObject.IsInstanceValid(owner))
		{
			return "<invalid>";
		}
		string sceneFilePath = owner.SceneFilePath;
		if (!string.IsNullOrWhiteSpace(sceneFilePath))
		{
			return sceneFilePath;
		}
		if (!owner.IsInsideTree())
		{
			return owner.Name.ToString();
		}
		return owner.GetPath().ToString();
	}

	private static int ResolveStableLayerDrawOrder(AdobeAnimateRuntimeDefinition definition, int layerId)
	{
		int num = -1;
		for (int i = 0; i < definition.GpuRenderLocalSlots.Length; i++)
		{
			AdobeAnimateGpuLocalSlot adobeAnimateGpuLocalSlot = definition.GpuRenderLocalSlots[i];
			if (adobeAnimateGpuLocalSlot.LayerId == layerId)
			{
				num = Math.Max(num, adobeAnimateGpuLocalSlot.DrawOrder);
			}
		}
		if (num < 0)
		{
			return layerId;
		}
		return num;
	}

	private static bool TryAppendManagedSpriteSlots(AdobeAnimateSprite owner, AdobeAnimateRuntimeDefinition definition, int ownerIndex, int ownerTopologyOrder, int? sortLayerOverride, int? sortDrawOverride, int[] nestedPath, List<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses, List<AdobeAnimateGpuManagedVisualBinding> managedVisualBindings, List<PendingRenderSlot> pendingSlots, out string failureReason)
	{
		failureReason = "";
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = owner.GetManagedSlotSpritesForRender();
		for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
		{
			AdobeAnimateManagedSlotSprite adobeAnimateManagedSlotSprite = managedSlotSpritesForRender[i];
			AdobeAnimateSlot slot = adobeAnimateManagedSlotSprite.Slot;
			CanvasItem visual = adobeAnimateManagedSlotSprite.Visual;
			if (!GodotObject.IsInstanceValid(slot) || !GodotObject.IsInstanceValid(visual) || visual.GetParent() != slot)
			{
				failureReason = $"managed Slot visual[{i}] is invalid or detached";
				return false;
			}
			if (slot.mode != 0 || slot.followSlotId <= 0)
			{
				failureReason = $"managed Slot visual {visual.Name} requires unsupported slot mode={slot.mode} follow={slot.followSlotId}";
				return false;
			}
			if (!TryAppendManagedAttachmentPoses(definition, slot.followSlotId - 1, managedAttachmentPoses, out var attachmentPoseBase))
			{
				failureReason = $"managed Slot visual {visual.Name} follow layer {slot.followSlotId - 1} has no pose lookup";
				return false;
			}
			int count = managedVisualBindings.Count;
			managedVisualBindings.Add(new AdobeAnimateGpuManagedVisualBinding(ownerIndex, i, attachmentPoseBase));
			int num = Math.Max(0, slot.ResolveDrawLayerId() - 1);
			int num2 = ResolveStableLayerDrawOrder(definition, num);
			int[] nestedPath2 = ((sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, num, num2, 2147483647) : AppendNestedPath(nestedPath, 2147483647));
			AdobeAnimateSortPath sortPath = new AdobeAnimateSortPath(0, Array.Empty<int>(), sortLayerOverride ?? num, sortDrawOverride ?? num2, ownerTopologyOrder * 1000000 + definition.GpuRenderLocalSlots.Length + i, nestedPath2);
			pendingSlots.Add(new PendingRenderSlot(sortPath, new AdobeAnimateGpuRenderSlot(ownerIndex, ownerTopologyOrder, -1, AdobeAnimateGpuRenderSlotKind.ManagedSprite2D, count)));
		}
		return true;
	}

	private static bool TryAppendManagedAttachmentPoses(AdobeAnimateRuntimeDefinition definition, int attachmentKey, List<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses, out int attachmentPoseBase)
	{
		attachmentPoseBase = -1;
		if (!IsValidDefinition(definition) || attachmentKey < 0 || managedAttachmentPoses == null)
		{
			return false;
		}
		attachmentPoseBase = managedAttachmentPoses.Count;
		bool flag = false;
		for (int i = 0; i < definition.Frames.Length; i++)
		{
			int mediaId;
			Transform2D transform;
			bool flag2 = AdobeAnimateDefinitionCache.TryGetCpuPoseSample(definition, i, attachmentKey, useLayerId: true, out mediaId, out transform);
			if (!flag2)
			{
				flag2 = AdobeAnimateDefinitionCache.TryGetCpuPoseSample(definition, i, attachmentKey, useLayerId: false, out mediaId, out transform);
			}
			managedAttachmentPoses.Add(new AdobeAnimateGpuManagedAttachmentPose(flag2 ? transform : Transform2D.Identity, flag2));
			flag = flag || flag2;
		}
		if (flag)
		{
			return true;
		}
		managedAttachmentPoses.RemoveRange(attachmentPoseBase, managedAttachmentPoses.Count - attachmentPoseBase);
		attachmentPoseBase = -1;
		return false;
	}

	private static bool TryAppendExternalVisualSlots(AdobeAnimateSprite owner, AdobeAnimateRuntimeDefinition definition, int ownerIndex, int ownerTopologyOrder, int? sortLayerOverride, int? sortDrawOverride, int[] nestedPath, List<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses, List<AdobeAnimateGpuManagedVisualBinding> managedVisualBindings, List<PendingRenderSlot> pendingSlots, out string failureReason)
	{
		failureReason = "";
		ulong externalVisualTopologyVersionForRender = owner.GetExternalVisualTopologyVersionForRender();
		ReadOnlySpan<int> activeExternalVisualIndicesForRender = owner.GetActiveExternalVisualIndicesForRender();
		int num = owner.GetManagedSlotSpritesForRender().Length;
		for (int i = 0; i < activeExternalVisualIndicesForRender.Length; i++)
		{
			int num2 = activeExternalVisualIndicesForRender[i];
			if (!owner.TryGetExternalVisualForRender(num2, out var visual) || !GodotObject.IsInstanceValid(visual.Sprite))
			{
				failureReason = $"external visual[{num2}] is invalid";
				return false;
			}
			int attachmentPoseBase = -1;
			int num3 = 0;
			int num4 = 0;
			int num5;
			switch (visual.Descriptor.AttachmentMode)
			{
			case AdobeAnimateExternalVisualAttachmentMode.Slot:
			{
				AdobeAnimateSlot slot = visual.Descriptor.Slot;
				if (!GodotObject.IsInstanceValid(slot) || slot.mode != 0 || slot.followSlotId <= 0 || !TryAppendManagedAttachmentPoses(definition, slot.followSlotId - 1, managedAttachmentPoses, out attachmentPoseBase))
				{
					failureReason = $"external visual {visual.Sprite.Name} has no usable slot pose";
					return false;
				}
				num3 = Math.Max(0, slot.ResolveDrawLayerId() - 1);
				num4 = ResolveStableLayerDrawOrder(definition, num3);
				num5 = 2147483647;
				break;
			}
			case AdobeAnimateExternalVisualAttachmentMode.Root:
			case AdobeAnimateExternalVisualAttachmentMode.World:
				num5 = visual.Descriptor.DrawBand switch
				{
					AdobeAnimateExternalVisualDrawBand.BehindAnimation => -2147483648, 
					AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation => 2147483647, 
					_ => 2147483647, 
				};
				break;
			default:
				failureReason = $"external visual {visual.Sprite.Name} has unsupported attachment mode";
				return false;
			}
			int count = managedVisualBindings.Count;
			managedVisualBindings.Add(new AdobeAnimateGpuManagedVisualBinding(ownerIndex, AdobeAnimateGpuManagedVisualSourceKind.ExternalVisual, num2, attachmentPoseBase));
			int[] nestedPath2 = ((visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot) ? ((sortLayerOverride.HasValue || sortDrawOverride.HasValue) ? AppendNestedPath(nestedPath, num3, num4, num5, visual.Descriptor.RelativeZ, i) : AppendNestedPath(nestedPath, num5, visual.Descriptor.RelativeZ, i)) : AppendNestedPath(nestedPath, num5, visual.Descriptor.RelativeZ, i));
			AdobeAnimateSortPath sortPath = new AdobeAnimateSortPath(0, Array.Empty<int>(), sortLayerOverride ?? ((visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot) ? num3 : num5), sortDrawOverride ?? ((visual.Descriptor.AttachmentMode == AdobeAnimateExternalVisualAttachmentMode.Slot) ? num4 : num5), ownerTopologyOrder * 1000000 + definition.GpuRenderLocalSlots.Length + num + i, nestedPath2);
			pendingSlots.Add(new PendingRenderSlot(sortPath, new AdobeAnimateGpuRenderSlot(ownerIndex, ownerTopologyOrder, -1, AdobeAnimateGpuRenderSlotKind.ManagedSprite2D, count)));
		}
		if (owner.GetExternalVisualTopologyVersionForRender() != externalVisualTopologyVersionForRender)
		{
			failureReason = "external visual topology changed while building the GPU graph";
			return false;
		}
		return true;
	}

	private static bool TryAppendAttachmentPoseLookup(AdobeAnimateRuntimeDefinition definition, int attachmentKey, List<int> attachmentPoseLookup, out int attachmentPoseLookupBase)
	{
		attachmentPoseLookupBase = -1;
		if (!IsValidDefinition(definition) || attachmentKey < 0 || attachmentPoseLookup == null)
		{
			return false;
		}
		attachmentPoseLookupBase = attachmentPoseLookup.Count;
		bool flag = false;
		for (int i = 0; i < definition.Frames.Length; i++)
		{
			if (!TryFindAttachmentSliceIndex(definition, i, attachmentKey, out var sourceIndex))
			{
				attachmentPoseLookup.Add(-1);
				continue;
			}
			int item = definition.GpuPoseTextureBaseTexel + sourceIndex * 5;
			attachmentPoseLookup.Add(item);
			flag = true;
		}
		if (flag)
		{
			return true;
		}
		attachmentPoseLookup.RemoveRange(attachmentPoseLookupBase, attachmentPoseLookup.Count - attachmentPoseLookupBase);
		attachmentPoseLookupBase = -1;
		return false;
	}

	private static bool TryFindAttachmentSliceIndex(AdobeAnimateRuntimeDefinition definition, int frameIndex, int attachmentKey, out int sourceIndex)
	{
		sourceIndex = -1;
		PackedFrame packedFrame = definition.Frames[frameIndex];
		int num = Math.Max(0, packedFrame.Offset);
		int num2 = Math.Min(definition.SliceMetadata.Length, num + Math.Max(0, packedFrame.Count));
		for (int i = num; i < num2; i++)
		{
			if (definition.SliceMetadata[i].LayerId == attachmentKey)
			{
				sourceIndex = i;
				return true;
			}
		}
		for (int j = num; j < num2; j++)
		{
			if (definition.SliceMetadata[j].DrawOrder == attachmentKey)
			{
				sourceIndex = j;
				return true;
			}
		}
		return false;
	}

	private static int GetMaxOwnerDepth(AdobeAnimateGpuRenderOwner[] owners)
	{
		int num = 0;
		for (int i = 0; i < owners.Length; i++)
		{
			num = Math.Max(num, owners[i].Depth);
		}
		return num;
	}

	private static int GetFrameLookupCount(List<AdobeAnimateGpuRenderOwner> owners)
	{
		int num = 0;
		for (int i = 0; i < owners.Count; i++)
		{
			num += owners[i].Definition.GpuFrameSlotLookup.Length;
		}
		return num;
	}

	private static int[] AppendNestedPath(int[] source, params int[] values)
	{
		int num = ((source != null) ? source.Length : 0);
		int num2 = ((values != null) ? values.Length : 0);
		if (num2 == 0)
		{
			if (num != 0)
			{
				return source;
			}
			return Array.Empty<int>();
		}
		int[] array = new int[num + num2];
		if (num > 0)
		{
			Array.Copy(source, array, num);
		}
		Array.Copy(values, 0, array, num, num2);
		return array;
	}

	internal static ulong ComputeManagedTopologySignature(AdobeAnimateSprite root)
	{
		if (!GodotObject.IsInstanceValid(root))
		{
			return 0uL;
		}
		HashSet<AdobeAnimateSprite> visited = new HashSet<AdobeAnimateSprite>();
		return ComputeManagedTopologySignature(root, visited);
	}

	private static ulong ComputeManagedTopologySignature(AdobeAnimateSprite root, HashSet<AdobeAnimateSprite> visited)
	{
		ulong hash = 1469598103934665603uL;
		int managedCount = 0;
		AppendManagedTopology(root, visited, ref hash, ref managedCount);
		if (managedCount <= 0)
		{
			return 0uL;
		}
		return hash;
	}

	private static void AppendManagedTopology(AdobeAnimateSprite owner, HashSet<AdobeAnimateSprite> visited, ref ulong hash, ref int managedCount)
	{
		if (!GodotObject.IsInstanceValid(owner) || !visited.Add(owner))
		{
			Add(ref hash, -1L);
			return;
		}
		AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = owner.GetManagedSlotSpritesForRender();
		Add(ref hash, managedSlotSpritesForRender.Length);
		for (int i = 0; i < managedSlotSpritesForRender.Length; i++)
		{
			Add(ref hash, i);
			Add(ref hash, AdobeAnimateManagedSprite2D.BuildTopologySignature(in managedSlotSpritesForRender[i]));
			managedCount++;
		}
		ReadOnlySpan<int> activeExternalVisualIndicesForRender = owner.GetActiveExternalVisualIndicesForRender();
		Add(ref hash, activeExternalVisualIndicesForRender.Length);
		for (int j = 0; j < activeExternalVisualIndicesForRender.Length; j++)
		{
			if (!owner.TryGetExternalVisualForRender(activeExternalVisualIndicesForRender[j], out var visual))
			{
				Add(ref hash, -1L);
				continue;
			}
			Add(ref hash, activeExternalVisualIndicesForRender[j]);
			Add(ref hash, (long)visual.Descriptor.AttachmentMode);
			Add(ref hash, (long)visual.Descriptor.DrawBand);
			Add(ref hash, visual.Descriptor.RelativeZ);
			AdobeAnimateSlot slot = visual.Descriptor.Slot;
			if (GodotObject.IsInstanceValid(slot))
			{
				Add(ref hash, slot.mode);
				Add(ref hash, slot.followSlotId);
				Add(ref hash, slot.ResolveDrawLayerId());
			}
			managedCount++;
		}
		AdobeAnimateSprite[] spriteChildrenForRender = owner.GetSpriteChildrenForRender();
		int num = 0;
		foreach (AdobeAnimateSprite adobeAnimateSprite in spriteChildrenForRender)
		{
			if (GodotObject.IsInstanceValid(adobeAnimateSprite) && !adobeAnimateSprite.IsEmptyHiddenGpuGraphPlaceholderForRender())
			{
				num++;
			}
		}
		Add(ref hash, num);
		for (int l = 0; l < spriteChildrenForRender.Length; l++)
		{
			AdobeAnimateSprite adobeAnimateSprite2 = spriteChildrenForRender[l];
			if (!GodotObject.IsInstanceValid(adobeAnimateSprite2) || !adobeAnimateSprite2.IsEmptyHiddenGpuGraphPlaceholderForRender())
			{
				Add(ref hash, l);
				AppendManagedTopology(adobeAnimateSprite2, visited, ref hash, ref managedCount);
			}
		}
	}

	private static ulong ComputeSignature(IReadOnlyList<AdobeAnimateGpuRenderOwner> owners, IReadOnlyList<AdobeAnimateGpuRenderSlot> renderSlots, IReadOnlyList<int> attachmentPoseLookup, IReadOnlyList<AdobeAnimateGpuManagedAttachmentPose> managedAttachmentPoses, IReadOnlyList<AdobeAnimateGpuManagedVisualBinding> managedVisualBindings, ulong managedTopologySignature)
	{
		ulong hash = 1469598103934665603uL;
		Add(ref hash, 7L);
		Add(ref hash, managedTopologySignature);
		Add(ref hash, owners.Count);
		Add(ref hash, renderSlots.Count);
		for (int i = 0; i < owners.Count; i++)
		{
			AdobeAnimateGpuRenderOwner adobeAnimateGpuRenderOwner = owners[i];
			AdobeAnimateRuntimeDefinition definition = adobeAnimateGpuRenderOwner.Definition;
			Add(ref hash, definition.GpuPoseSignature);
			Add(ref hash, definition.GpuPoseTextureLayer);
			Add(ref hash, definition.GpuPoseTextureSize.X);
			Add(ref hash, definition.GpuPoseTextureSize.Y);
			Add(ref hash, definition.Frames.Length);
			Add(ref hash, adobeAnimateGpuRenderOwner.ParentOwnerIndex);
			Add(ref hash, adobeAnimateGpuRenderOwner.TopologyOrder);
			Add(ref hash, adobeAnimateGpuRenderOwner.Depth);
			Add(ref hash, (long)adobeAnimateGpuRenderOwner.AttachmentKind);
			Add(ref hash, adobeAnimateGpuRenderOwner.AttachmentKey);
			Add(ref hash, adobeAnimateGpuRenderOwner.AttachmentPoseLookupBase);
			Add(ref hash, adobeAnimateGpuRenderOwner.RenderLayer);
			Add(ref hash, adobeAnimateGpuRenderOwner.LocalSlotCount);
			Add(ref hash, adobeAnimateGpuRenderOwner.FrameLookupBase);
			Add(ref hash, adobeAnimateGpuRenderOwner.UsePos ? 1 : 0);
			Add(ref hash, adobeAnimateGpuRenderOwner.UseRotate ? 1 : 0);
			Add(ref hash, adobeAnimateGpuRenderOwner.UseScale ? 1 : 0);
			Add(ref hash, adobeAnimateGpuRenderOwner.UseSkew ? 1 : 0);
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.SlotOffset.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.SlotOffset.Y));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.OffsetRotate));
			Add(ref hash, adobeAnimateGpuRenderOwner.UseFollowVisible ? 1 : 0);
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.X.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.X.Y));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.Y.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.Y.Y));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.Origin.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuRenderOwner.LocalTransform.Origin.Y));
			AdobeAnimateGpuLocalSlot[] array = definition?.GpuRenderLocalSlots ?? Array.Empty<AdobeAnimateGpuLocalSlot>();
			for (int j = 0; j < array.Length; j++)
			{
				AdobeAnimateGpuLocalSlot adobeAnimateGpuLocalSlot = array[j];
				Add(ref hash, adobeAnimateGpuLocalSlot.SliceKey);
				Add(ref hash, adobeAnimateGpuLocalSlot.LayerId);
				Add(ref hash, adobeAnimateGpuLocalSlot.DrawOrder);
				Add(ref hash, adobeAnimateGpuLocalSlot.Occurrence);
			}
		}
		for (int k = 0; k < renderSlots.Count; k++)
		{
			AdobeAnimateGpuRenderSlot adobeAnimateGpuRenderSlot = renderSlots[k];
			Add(ref hash, adobeAnimateGpuRenderSlot.OwnerIndex);
			Add(ref hash, adobeAnimateGpuRenderSlot.OwnerTopologyOrder);
			Add(ref hash, adobeAnimateGpuRenderSlot.OwnerLocalSlot);
			Add(ref hash, (long)adobeAnimateGpuRenderSlot.Kind);
			Add(ref hash, adobeAnimateGpuRenderSlot.StaticVisualIndex);
		}
		for (int l = 0; l < attachmentPoseLookup.Count; l++)
		{
			Add(ref hash, attachmentPoseLookup[l]);
		}
		for (int m = 0; m < managedAttachmentPoses.Count; m++)
		{
			AdobeAnimateGpuManagedAttachmentPose adobeAnimateGpuManagedAttachmentPose = managedAttachmentPoses[m];
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Origin.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Origin.Y));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Rotation));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Scale.X));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Scale.Y));
			Add(ref hash, BitConverter.SingleToInt32Bits(adobeAnimateGpuManagedAttachmentPose.Skew));
			Add(ref hash, adobeAnimateGpuManagedAttachmentPose.Valid ? 1 : 0);
		}
		for (int n = 0; n < managedVisualBindings.Count; n++)
		{
			AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding = managedVisualBindings[n];
			Add(ref hash, adobeAnimateGpuManagedVisualBinding.OwnerIndex);
			Add(ref hash, (long)adobeAnimateGpuManagedVisualBinding.SourceKind);
			Add(ref hash, adobeAnimateGpuManagedVisualBinding.VisualIndex);
			Add(ref hash, adobeAnimateGpuManagedVisualBinding.AttachmentPoseBase);
		}
		return hash;
	}

	private static void Add(ref ulong hash, long component)
	{
		hash ^= (ulong)component;
		hash *= 1099511628211uL;
	}

	private static void Add(ref ulong hash, ulong component)
	{
		hash ^= component;
		hash *= 1099511628211uL;
	}
}
