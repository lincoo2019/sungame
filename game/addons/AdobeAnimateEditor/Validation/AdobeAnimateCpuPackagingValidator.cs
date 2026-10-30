using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

internal static class AdobeAnimateCpuPackagingValidator
{
	private struct DefinitionCounters
	{
		public int ClipCount;

		public int FrameCount;

		public int ExpectedVisibleItems;

		public int CpuMeshItems;

		public int NativeSpriteItems;

		public Rid AtlasTextureArrayRid;

		public Rid PoseTextureArrayRid;

		public int RenderSlotCount;

		public int MeshCapacity;

		public int StateTexels;

		public bool PoseArrayReady;
	}

	private struct VisualCounters
	{
		public int ExpectedVisibleItems;

		public int CpuMeshItems;

		public int NativeSpriteItems;

		public int NativeBehindItems;

		public int NativeFrontItems;
	}

	private struct GraphCounters
	{
		public int RenderGraphCount;

		public int MaxRenderSlots;

		public int MaxMeshCapacity;

		public int StateTexels;

		public VisualCounters Visuals;
	}

	public static AdobeAnimateCpuPackagingReport ValidateProject(AdobeAnimateCpuValidationOptions options)
	{
		if ((object)options == null)
		{
			options = AdobeAnimateCpuValidationOptions.Normal("res://.godot/adobe-animate-cpu-validation/report.json");
		}
		AdobeAnimateDefinitionCache.Clear();
		List<AdobeAnimateCpuValidationError> list = new List<AdobeAnimateCpuValidationError>();
		List<AdobeAnimateCpuDefinitionSummary> list2 = new List<AdobeAnimateCpuDefinitionSummary>();
		AdobeAnimateCpuPackagingCatalog adobeAnimateCpuPackagingCatalog;
		try
		{
			adobeAnimateCpuPackagingCatalog = AdobeAnimateCpuPackagingCatalog.BuildProjectCatalog();
		}
		catch (Exception ex)
		{
			list.Add(Error("res://", string.Empty, -1, "catalog-build-failed", ex.Message));
			return WriteOrReturnFailure(BuildReport(0, 0, list2, list, string.Empty), options.ReportPath);
		}
		AppendCatalogErrors(adobeAnimateCpuPackagingCatalog, list);
		ApplyFaultBeforeDefinitionValidation(options.Fault, list);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		int num6 = 0;
		Rid rid = default(Rid);
		Rid rid2 = default(Rid);
		for (int i = 0; i < adobeAnimateCpuPackagingCatalog.DefinitionPaths.Count; i++)
		{
			string text = adobeAnimateCpuPackagingCatalog.DefinitionPaths[i];
			DefinitionCounters definitionCounters = ValidateDefinition(text, list);
			num += definitionCounters.ClipCount;
			num2 += definitionCounters.FrameCount;
			num3 += definitionCounters.ExpectedVisibleItems;
			num4 += definitionCounters.CpuMeshItems;
			num5 += definitionCounters.NativeSpriteItems;
			if (definitionCounters.PoseArrayReady)
			{
				num6++;
			}
			if (definitionCounters.AtlasTextureArrayRid.IsValid)
			{
				if (!rid.IsValid)
				{
					rid = definitionCounters.AtlasTextureArrayRid;
				}
				else if (rid != definitionCounters.AtlasTextureArrayRid)
				{
					list.Add(Error(text, string.Empty, -1, "shared-atlas-rid-mismatch", "The definition does not use the project shared visual Texture2DArray RID."));
				}
			}
			if (definitionCounters.PoseTextureArrayRid.IsValid)
			{
				if (!rid2.IsValid)
				{
					rid2 = definitionCounters.PoseTextureArrayRid;
				}
				else if (rid2 != definitionCounters.PoseTextureArrayRid)
				{
					list.Add(Error(text, string.Empty, -1, "shared-pose-rid-mismatch", "The definition does not use the project shared Pose Texture2DArray RID."));
				}
			}
			list2.Add(new AdobeAnimateCpuDefinitionSummary(text, definitionCounters.ClipCount, definitionCounters.FrameCount, definitionCounters.ExpectedVisibleItems, definitionCounters.CpuMeshItems, definitionCounters.NativeSpriteItems, definitionCounters.RenderSlotCount, definitionCounters.MeshCapacity, definitionCounters.StateTexels, definitionCounters.PoseArrayReady));
		}
		GraphCounters graphCounters = ValidateSceneGraphs(adobeAnimateCpuPackagingCatalog, rid, rid2, list);
		VisualCounters visualCounters = ValidateRegisteredExternalVisuals(adobeAnimateCpuPackagingCatalog, rid, list);
		num3 += graphCounters.Visuals.ExpectedVisibleItems + visualCounters.ExpectedVisibleItems;
		num4 += graphCounters.Visuals.CpuMeshItems + visualCounters.CpuMeshItems;
		num5 += graphCounters.Visuals.NativeSpriteItems + visualCounters.NativeSpriteItems;
		if (num3 != num4 + num5)
		{
			list.Add(Error("res://", string.Empty, -1, "visible-item-accounting-mismatch", $"ExpectedVisibleItems={num3}, CpuMeshItems={num4}, NativeSpriteItems={num5}."));
		}
		SortErrors(list);
		list2.Sort((AdobeAnimateCpuDefinitionSummary left, AdobeAnimateCpuDefinitionSummary right) => string.Compare(left.ResourcePath, right.ResourcePath, StringComparison.Ordinal));
		string inputSignature = AdobeAnimateCpuPackagingReport.ComputeInputSignature(adobeAnimateCpuPackagingCatalog.ProjectRoot, adobeAnimateCpuPackagingCatalog.InputPaths);
		AdobeAnimateCpuPackagingReport adobeAnimateCpuPackagingReport = new AdobeAnimateCpuPackagingReport
		{
			DefinitionCount = adobeAnimateCpuPackagingCatalog.DefinitionPaths.Count,
			SceneCount = adobeAnimateCpuPackagingCatalog.ScenePaths.Count,
			ClipCount = num,
			FrameCount = num2,
			ExpectedVisibleItems = num3,
			CpuMeshItems = num4,
			NativeSpriteItems = num5,
			PoseArrayDefinitions = num6,
			RenderGraphCount = graphCounters.RenderGraphCount,
			MaxRenderSlots = graphCounters.MaxRenderSlots,
			MaxMeshCapacity = graphCounters.MaxMeshCapacity,
			StateTexels = graphCounters.StateTexels,
			ManagedVisualItems = graphCounters.Visuals.CpuMeshItems + visualCounters.CpuMeshItems,
			NativeBehindItems = graphCounters.Visuals.NativeBehindItems,
			NativeFrontItems = graphCounters.Visuals.NativeFrontItems + visualCounters.NativeFrontItems,
			CpuFallbackRoots = 0,
			CpuValidationFailures = list.Count,
			InputSignature = inputSignature,
			Definitions = list2.ToArray(),
			Errors = list.ToArray()
		};
		if (adobeAnimateCpuPackagingReport.CpuFallbackRoots != 0)
		{
			list.Add(Error("res://", string.Empty, -1, "cpu-fallback-nonzero", $"CpuFallbackRoots={adobeAnimateCpuPackagingReport.CpuFallbackRoots}."));
			adobeAnimateCpuPackagingReport = BuildReport(adobeAnimateCpuPackagingCatalog.ScenePaths.Count, adobeAnimateCpuPackagingCatalog.DefinitionPaths.Count, list2, list, inputSignature);
		}
		return WriteOrReturnFailure(adobeAnimateCpuPackagingReport, options.ReportPath);
	}

	private static DefinitionCounters ValidateDefinition(string definitionPath, List<AdobeAnimateCpuValidationError> errors)
	{
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition;
		try
		{
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(definitionPath, string.Empty, ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(adobeAnimateData))
			{
				errors.Add(Error(definitionPath, string.Empty, -1, "definition-load-failed", "AdobeAnimateData could not be loaded."));
				return default(DefinitionCounters);
			}
			adobeAnimateRuntimeDefinition = AdobeAnimateDefinitionCache.BuildForPackagingValidation(adobeAnimateData);
		}
		catch (Exception ex)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "definition-build-failed", ex.Message));
			return default(DefinitionCounters);
		}
		if (adobeAnimateRuntimeDefinition == null)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "definition-build-failed", "The runtime definition builder returned null."));
			return default(DefinitionCounters);
		}
		PackedFrame[] array = adobeAnimateRuntimeDefinition.Frames ?? Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = adobeAnimateRuntimeDefinition.SliceMetadata ?? Array.Empty<PackedSliceMetadata>();
		PackedClip[] array3 = (adobeAnimateRuntimeDefinition.Clips ?? Array.Empty<PackedClip>()).OrderBy<PackedClip, string>((PackedClip clip) => clip.Name.ToString(), StringComparer.Ordinal).ToArray();
		AdobeAnimateGpuLocalSlot[] array4 = adobeAnimateRuntimeDefinition.GpuRenderLocalSlots ?? Array.Empty<AdobeAnimateGpuLocalSlot>();
		AdobeAnimateGpuFrameSlotEntry[] array5 = adobeAnimateRuntimeDefinition.GpuFrameSlotLookup ?? Array.Empty<AdobeAnimateGpuFrameSlotEntry>();
		bool flag = adobeAnimateRuntimeDefinition.UsesGpuPoseTextureArray && GodotObject.IsInstanceValid(adobeAnimateRuntimeDefinition.GpuPoseTextureArray) && adobeAnimateRuntimeDefinition.GpuPoseTextureRid.IsValid && adobeAnimateRuntimeDefinition.GpuPoseTextureSize.X > 0 && adobeAnimateRuntimeDefinition.GpuPoseTextureSize.Y > 0 && adobeAnimateRuntimeDefinition.GpuPoseTextureLayer >= 0 && adobeAnimateRuntimeDefinition.GpuPoseTextureLayer < adobeAnimateRuntimeDefinition.GpuPoseTextureArray.GetLayers();
		DefinitionCounters counters = new DefinitionCounters
		{
			ClipCount = array3.Length,
			FrameCount = array.Length,
			NativeSpriteItems = 0,
			AtlasTextureArrayRid = adobeAnimateRuntimeDefinition.AtlasTextureArrayRid,
			PoseTextureArrayRid = adobeAnimateRuntimeDefinition.GpuPoseTextureRid,
			PoseArrayReady = flag,
			RenderSlotCount = array4.Length
		};
		if (array.Length == 0)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "missing-frame", "The runtime definition has no frames."));
		}
		if (adobeAnimateRuntimeDefinition.FrameMax != array.Length)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "frame-count-mismatch", $"FrameMax={adobeAnimateRuntimeDefinition.FrameMax}, Frames={array.Length}."));
		}
		bool flag2 = adobeAnimateRuntimeDefinition.GpuRenderSlotOrderStable && array4.Length != 0 && array5.Length == array.Length * array4.Length;
		if (!flag2)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "invalid-render-graph", $"stable={adobeAnimateRuntimeDefinition.GpuRenderSlotOrderStable}, slots={array4.Length}, lookup={array5.Length}, expected={array.Length * array4.Length}."));
		}
		if (!flag)
		{
			if (adobeAnimateRuntimeDefinition.HasGpuPoseManifestEntry && adobeAnimateRuntimeDefinition.GpuPoseManifestSignature != adobeAnimateRuntimeDefinition.GpuPoseSignature)
			{
				errors.Add(Error(definitionPath, string.Empty, -1, "pose-signature-mismatch", $"runtime={adobeAnimateRuntimeDefinition.GpuPoseSignature:X16}, manifest={adobeAnimateRuntimeDefinition.GpuPoseManifestSignature:X16}, runtimeTexels={Math.Max(1, array2.Length * 5)}, manifestTexels={adobeAnimateRuntimeDefinition.GpuPoseManifestTexelCount}. Refresh the Adobe atlas before exporting."));
			}
			else
			{
				errors.Add(Error(definitionPath, string.Empty, -1, "invalid-pose-array", "The shared GPU Pose Texture2DArray allocation is unavailable."));
			}
		}
		if (!adobeAnimateRuntimeDefinition.UsesAtlasTextureArrayLayout || !GodotObject.IsInstanceValid(adobeAnimateRuntimeDefinition.AtlasTextureArray) || !adobeAnimateRuntimeDefinition.AtlasTextureArrayRid.IsValid || adobeAnimateRuntimeDefinition.AtlasTextureArraySize.X <= 0f || adobeAnimateRuntimeDefinition.AtlasTextureArraySize.Y <= 0f)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "missing-texture", "The shared visual Texture2DArray allocation is unavailable."));
		}
		ulong[] frameLayoutSignatures = adobeAnimateRuntimeDefinition.FrameLayoutSignatures;
		if (((frameLayoutSignatures != null) ? frameLayoutSignatures.Length : 0) != array.Length)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "frame-layout-signature-mismatch", "Frame layout signatures do not cover every frame."));
		}
		AdobeAnimateGpuGraphStateLayout layout;
		if (!AdobeAnimateGpuRenderGraphBuilder.TryBuildSingleOwner(adobeAnimateRuntimeDefinition, out var graph) || graph == null)
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "invalid-render-graph", "The definition cannot build a single-owner shared render graph."));
		}
		else if (!TryMeasureGraph(graph, null, out layout))
		{
			errors.Add(Error(definitionPath, string.Empty, -1, "invalid-render-graph", "The definition graph has no valid shared state layout."));
		}
		else
		{
			counters.StateTexels = layout.StateTexelCount;
			try
			{
				counters.MeshCapacity = CapacityForRenderSlots(layout.QuadCount);
				if (counters.MeshCapacity < graph.RenderSlots.Length)
				{
					throw new InvalidOperationException("The selected static Mesh class is smaller than the graph.");
				}
			}
			catch (Exception ex2)
			{
				errors.Add(Error(definitionPath, string.Empty, -1, "render-slot-capacity", ex2.Message));
			}
		}
		if (flag2)
		{
			for (int num = 0; num < array.Length; num++)
			{
				string clipName = ResolveFrameClip(array3, num);
				ValidateFrameLookup(definitionPath, clipName, num, array[num], adobeAnimateRuntimeDefinition, array2, array4, array5, flag, ref counters, errors);
			}
		}
		return counters;
	}

	private static void ValidateFrameLookup(string definitionPath, string clipName, int frameIndex, PackedFrame frame, AdobeAnimateRuntimeDefinition definition, PackedSliceMetadata[] metadata, AdobeAnimateGpuLocalSlot[] localSlots, AdobeAnimateGpuFrameSlotEntry[] frameLookup, bool poseArrayReady, ref DefinitionCounters counters, List<AdobeAnimateCpuValidationError> errors)
	{
		if (frame.Offset < 0 || frame.Count < 0 || frame.Offset > metadata.Length || frame.Offset + frame.Count > metadata.Length)
		{
			errors.Add(Error(definitionPath, clipName, frameIndex, "missing-frame", $"Frame slice range offset={frame.Offset}, count={frame.Count}, metadata={metadata.Length}."));
			return;
		}
		int num = definition.GpuPoseTextureSize.X * definition.GpuPoseTextureSize.Y;
		for (int i = 0; i < localSlots.Length; i++)
		{
			int num2 = frameIndex * localSlots.Length + i;
			AdobeAnimateGpuFrameSlotEntry adobeAnimateGpuFrameSlotEntry = frameLookup[num2];
			if (!adobeAnimateGpuFrameSlotEntry.Visible)
			{
				continue;
			}
			counters.ExpectedVisibleItems++;
			bool flag = true;
			if (!poseArrayReady || adobeAnimateGpuFrameSlotEntry.PoseTexel < 0 || adobeAnimateGpuFrameSlotEntry.PoseTexel > num - 5)
			{
				flag = false;
				errors.Add(Error(definitionPath, clipName, frameIndex, "invalid-pose-array", $"Slot {i} pose texel {adobeAnimateGpuFrameSlotEntry.PoseTexel} exceeds capacity {num}."));
			}
			int mediaId = adobeAnimateGpuFrameSlotEntry.MediaId;
			if (!AdobeAnimateDefinitionCache.IsBaseSliceRenderable(definition, mediaId))
			{
				flag = false;
				errors.Add(Error(definitionPath, clipName, frameIndex, "missing-texture", $"Slot {i} media {mediaId} has no renderable source rectangle."));
			}
			int[] mediaAtlasPages = definition.MediaAtlasPages;
			if ((uint)mediaId >= (uint)((mediaAtlasPages != null) ? mediaAtlasPages.Length : 0))
			{
				flag = false;
				errors.Add(Error(definitionPath, clipName, frameIndex, "invalid-atlas-page", $"Media {mediaId} has no atlas page entry."));
			}
			else
			{
				int num3 = definition.MediaAtlasPages[mediaId];
				int num4 = (GodotObject.IsInstanceValid(definition.AtlasTextureArray) ? definition.AtlasTextureArray.GetLayers() : 0);
				if (num3 < 0 || (num4 > 0 && num3 >= num4))
				{
					flag = false;
					errors.Add(Error(definitionPath, clipName, frameIndex, "invalid-atlas-page", $"Media {mediaId} uses page {num3}, layers={num4}."));
				}
			}
			if (flag)
			{
				counters.CpuMeshItems++;
			}
		}
	}

	private static void AppendCatalogErrors(AdobeAnimateCpuPackagingCatalog catalog, List<AdobeAnimateCpuValidationError> errors)
	{
		for (int i = 0; i < catalog.UnclassifiedReferences.Count; i++)
		{
			AdobeAnimateCpuConsumerReference adobeAnimateCpuConsumerReference = catalog.UnclassifiedReferences[i];
			errors.Add(Error(adobeAnimateCpuConsumerReference.ResourcePath, string.Empty, -1, "unclassified-reference", "evidence=" + adobeAnimateCpuConsumerReference.EvidencePath + ": " + adobeAnimateCpuConsumerReference.Evidence));
		}
		for (int j = 0; j < catalog.SuspendedReferences.Count; j++)
		{
			AdobeAnimateCpuConsumerReference adobeAnimateCpuConsumerReference2 = catalog.SuspendedReferences[j];
			errors.Add(Error(adobeAnimateCpuConsumerReference2.ResourcePath, string.Empty, -1, "suspended-reference", "evidence=" + adobeAnimateCpuConsumerReference2.EvidencePath + ": " + adobeAnimateCpuConsumerReference2.Evidence));
		}
	}

	private static void ApplyFaultBeforeDefinitionValidation(AdobeAnimateCpuValidationFault fault, List<AdobeAnimateCpuValidationError> errors)
	{
		string text = fault switch
		{
			AdobeAnimateCpuValidationFault.None => string.Empty, 
			AdobeAnimateCpuValidationFault.MissingFrame => "missing-frame", 
			AdobeAnimateCpuValidationFault.MissingTexture => "missing-texture", 
			AdobeAnimateCpuValidationFault.InvalidAtlasPage => "invalid-atlas-page", 
			AdobeAnimateCpuValidationFault.InvalidPoseArray => "invalid-pose-array", 
			AdobeAnimateCpuValidationFault.InvalidRenderGraph => "invalid-render-graph", 
			AdobeAnimateCpuValidationFault.RenderSlotCapacity => "render-slot-capacity", 
			AdobeAnimateCpuValidationFault.UnclassifiedVisual => "unclassified-visual", 
			AdobeAnimateCpuValidationFault.NativeOrderInterleaveUnsupported => "native-order-interleave-unsupported", 
			AdobeAnimateCpuValidationFault.SuspendedReference => "suspended-reference", 
			_ => "unknown-validation-fault", 
		};
		if (!string.IsNullOrEmpty(text))
		{
			errors.Add(Error("res://__fault__/AdobeAnimateCpuValidationFault.tres", string.Empty, -1, text, $"Injected additive validation fault: {fault}."));
		}
	}

	private static AdobeAnimateCpuPackagingReport WriteOrReturnFailure(AdobeAnimateCpuPackagingReport report, string reportPath)
	{
		try
		{
			report.WriteDeterministic(reportPath);
			return report;
		}
		catch (Exception ex)
		{
			List<AdobeAnimateCpuValidationError> list = new List<AdobeAnimateCpuValidationError>(report.Errors) { Error(reportPath ?? string.Empty, string.Empty, -1, "report-write-failed", ex.Message) };
			SortErrors(list);
			return new AdobeAnimateCpuPackagingReport
			{
				DefinitionCount = report.DefinitionCount,
				SceneCount = report.SceneCount,
				ClipCount = report.ClipCount,
				FrameCount = report.FrameCount,
				ExpectedVisibleItems = report.ExpectedVisibleItems,
				CpuMeshItems = report.CpuMeshItems,
				NativeSpriteItems = report.NativeSpriteItems,
				PoseArrayDefinitions = report.PoseArrayDefinitions,
				RenderGraphCount = report.RenderGraphCount,
				MaxRenderSlots = report.MaxRenderSlots,
				MaxMeshCapacity = report.MaxMeshCapacity,
				StateTexels = report.StateTexels,
				ManagedVisualItems = report.ManagedVisualItems,
				NativeBehindItems = report.NativeBehindItems,
				NativeFrontItems = report.NativeFrontItems,
				CpuFallbackRoots = report.CpuFallbackRoots,
				CpuValidationFailures = list.Count,
				InputSignature = report.InputSignature,
				Definitions = report.Definitions,
				Errors = list.ToArray()
			};
		}
	}

	private static AdobeAnimateCpuPackagingReport BuildReport(int sceneCount, int definitionCount, IReadOnlyList<AdobeAnimateCpuDefinitionSummary> definitions, IReadOnlyList<AdobeAnimateCpuValidationError> errors, string inputSignature)
	{
		return new AdobeAnimateCpuPackagingReport
		{
			DefinitionCount = definitionCount,
			SceneCount = sceneCount,
			ClipCount = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.ClipCount),
			FrameCount = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.FrameCount),
			ExpectedVisibleItems = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.ExpectedVisibleItems),
			CpuMeshItems = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.CpuMeshItems),
			NativeSpriteItems = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.NativeSpriteItems),
			PoseArrayDefinitions = definitions.Count((AdobeAnimateCpuDefinitionSummary item) => item.PoseArrayReady),
			RenderGraphCount = 0,
			MaxRenderSlots = ((definitions.Count > 0) ? definitions.Max((AdobeAnimateCpuDefinitionSummary item) => item.RenderSlotCount) : 0),
			MaxMeshCapacity = ((definitions.Count > 0) ? definitions.Max((AdobeAnimateCpuDefinitionSummary item) => item.MeshCapacity) : 0),
			StateTexels = definitions.Sum((AdobeAnimateCpuDefinitionSummary item) => item.StateTexels),
			ManagedVisualItems = 0,
			NativeBehindItems = 0,
			NativeFrontItems = 0,
			CpuFallbackRoots = 0,
			CpuValidationFailures = errors.Count,
			InputSignature = inputSignature,
			Definitions = definitions.ToArray(),
			Errors = errors.ToArray()
		};
	}

	private static string ResolveFrameClip(PackedClip[] clips, int frame)
	{
		for (int i = 0; i < clips.Length; i++)
		{
			Vector2I range = clips[i].Range;
			int num = Math.Min(range.X, range.Y);
			int num2 = Math.Max(range.X, range.Y);
			if (frame >= num && frame <= num2)
			{
				return clips[i].Name.ToString();
			}
		}
		return "<unowned>";
	}

	private static GraphCounters ValidateSceneGraphs(AdobeAnimateCpuPackagingCatalog catalog, Rid sharedAtlasArrayRid, Rid sharedPoseArrayRid, List<AdobeAnimateCpuValidationError> errors)
	{
		GraphCounters counters = default(GraphCounters);
		for (int i = 0; i < catalog.ScenePaths.Count; i++)
		{
			string text = catalog.ScenePaths[i];
			Node node = null;
			try
			{
				PackedScene packedScene = ResourceLoader.Load<PackedScene>(text, string.Empty, ResourceLoader.CacheMode.Ignore);
				if (!GodotObject.IsInstanceValid(packedScene))
				{
					errors.Add(Error(text, string.Empty, -1, "scene-load-failed", "The animation scene could not be loaded."));
					continue;
				}
				node = packedScene.Instantiate(PackedScene.GenEditState.Disabled);
				if (!GodotObject.IsInstanceValid(node))
				{
					errors.Add(Error(text, string.Empty, -1, "scene-instantiate-failed", "The animation scene could not be instantiated."));
				}
				else
				{
					ValidateSceneNodeGraphs(text, node, sharedAtlasArrayRid, sharedPoseArrayRid, ref counters, errors);
				}
			}
			catch (Exception ex)
			{
				errors.Add(Error(text, string.Empty, -1, "invalid-render-graph", ex.Message));
			}
			finally
			{
				if (GodotObject.IsInstanceValid(node))
				{
					node.Free();
				}
			}
		}
		return counters;
	}

	private static void ValidateSceneNodeGraphs(string scenePath, Node node, Rid sharedAtlasArrayRid, Rid sharedPoseArrayRid, ref GraphCounters counters, List<AdobeAnimateCpuValidationError> errors)
	{
		if (node is AdobeAnimateSprite adobeAnimateSprite && !adobeAnimateSprite.IsRenderedByParentSpriteForRender())
		{
			ValidateSceneGraph(scenePath, adobeAnimateSprite, sharedAtlasArrayRid, sharedPoseArrayRid, ref counters, errors);
		}
		foreach (Node child in node.GetChildren())
		{
			ValidateSceneNodeGraphs(scenePath, child, sharedAtlasArrayRid, sharedPoseArrayRid, ref counters, errors);
		}
	}

	private static void ValidateSceneGraph(string scenePath, AdobeAnimateSprite root, Rid sharedAtlasArrayRid, Rid sharedPoseArrayRid, ref GraphCounters counters, List<AdobeAnimateCpuValidationError> errors)
	{
		if (!AdobeAnimateGpuRenderGraphBuilder.TryBuild(root, out var graph, out var ownerSprites, out var failureReason) || graph == null || ownerSprites == null || ownerSprites.Length != graph.Owners.Length)
		{
			errors.Add(Error(scenePath, string.Empty, -1, "invalid-render-graph", failureReason));
			return;
		}
		for (int i = 0; i < graph.Owners.Length; i++)
		{
			AdobeAnimateRuntimeDefinition definition = graph.Owners[i].Definition;
			if (definition == null || (sharedAtlasArrayRid.IsValid && definition.AtlasTextureArrayRid != sharedAtlasArrayRid) || (sharedPoseArrayRid.IsValid && definition.GpuPoseTextureRid != sharedPoseArrayRid))
			{
				errors.Add(Error(scenePath, string.Empty, -1, "invalid-render-graph", $"Owner {i} does not share the project visual/Pose arrays."));
			}
		}
		if (!TryMeasureGraph(graph, ownerSprites, out var layout))
		{
			errors.Add(Error(scenePath, string.Empty, -1, "invalid-render-graph", "The graph has no valid shared state layout."));
			return;
		}
		int num;
		try
		{
			num = CapacityForRenderSlots(layout.QuadCount);
			if (num < graph.RenderSlots.Length)
			{
				throw new InvalidOperationException("The selected static Mesh class is smaller than the graph.");
			}
		}
		catch (Exception ex)
		{
			errors.Add(Error(scenePath, string.Empty, -1, "render-slot-capacity", ex.Message));
			return;
		}
		counters.RenderGraphCount++;
		counters.MaxRenderSlots = Math.Max(counters.MaxRenderSlots, graph.RenderSlots.Length);
		counters.MaxMeshCapacity = Math.Max(counters.MaxMeshCapacity, num);
		counters.StateTexels += layout.StateTexelCount;
		ValidateGraphVisuals(scenePath, graph, ownerSprites, sharedAtlasArrayRid, ref counters.Visuals, errors);
	}

	private static bool TryMeasureGraph(AdobeAnimateGpuRenderGraphDefinition graph, IReadOnlyList<AdobeAnimateSprite> ownerSprites, out AdobeAnimateGpuGraphStateLayout layout)
	{
		layout = default(AdobeAnimateGpuGraphStateLayout);
		if (graph == null || graph.Owners.Length == 0 || graph.RenderSlots.Length == 0)
		{
			return false;
		}
		List<AdobeAnimateGpuGraphOwnerState> list = new List<AdobeAnimateGpuGraphOwnerState>(graph.Owners.Length);
		for (int i = 0; i < graph.Owners.Length; i++)
		{
			AdobeAnimateRuntimeDefinition definition = graph.Owners[i].Definition;
			AdobeAnimateSprite sourceSprite = ((ownerSprites != null && i < ownerSprites.Count) ? ownerSprites[i] : null);
			list.Add(new AdobeAnimateGpuGraphOwnerState(sourceSprite, definition, Transform2D.Identity, Colors.White, Vector2.Zero, default(VerticalClipState), 0, 0f, allLayersVisible: true, canUseLayerMask: true, 18446744073709551615uL, null, definition?.RuntimeLayerCount ?? 0, hasMediaReplace: false, null, null, null, Vector2.Zero, 0, 0uL, visible: true, default(AdobeAnimateClipBlendState)));
		}
		return AdobeAnimateGpuGraphStateWriter.TryMeasure(new AdobeAnimateGpuRenderGraphAllocation(graph.Signature, 0, 0, 1, graph.RenderSlots.Length, graph.Owners.Length), list, 0, list.Count, graph.ManagedVisualBindings.Length, out layout);
	}

	private static int CapacityForRenderSlots(int requiredSlots)
	{
		return AdobeAnimateRenderManager.CapacityForCrowdMeshSlots(requiredSlots);
	}

	private static void ValidateGraphVisuals(string scenePath, AdobeAnimateGpuRenderGraphDefinition graph, IReadOnlyList<AdobeAnimateSprite> ownerSprites, Rid expectedAtlasArrayRid, ref VisualCounters counters, List<AdobeAnimateCpuValidationError> errors)
	{
		int num = 2147483647;
		int num2 = -2147483648;
		for (int i = 0; i < graph.RenderSlots.Length; i++)
		{
			if (graph.RenderSlots[i].Kind == AdobeAnimateGpuRenderSlotKind.Pose)
			{
				num = Math.Min(num, i);
				num2 = Math.Max(num2, i);
			}
		}
		for (int j = 0; j < graph.ManagedVisualBindings.Length; j++)
		{
			AdobeAnimateGpuManagedVisualBinding adobeAnimateGpuManagedVisualBinding = graph.ManagedVisualBindings[j];
			int num3 = FindGraphSlot(graph.RenderSlots, j);
			if (num3 < 0 || adobeAnimateGpuManagedVisualBinding.OwnerIndex < 0 || adobeAnimateGpuManagedVisualBinding.OwnerIndex >= ownerSprites.Count)
			{
				errors.Add(Error(scenePath, string.Empty, -1, "invalid-render-graph", $"Managed binding {j} has no owner or render slot."));
				continue;
			}
			bool flag = num3 < num;
			bool flag2 = num3 > num2;
			AdobeAnimateCpuNativeDrawBand nativeBand = (flag ? AdobeAnimateCpuNativeDrawBand.BehindRootMesh : AdobeAnimateCpuNativeDrawBand.InFrontOfRootMesh);
			int num4 = (flag ? (-2147483648) : 2147483647);
			AdobeAnimateSortPath sortPath = new AdobeAnimateSortPath(0, Array.Empty<int>(), num4, num4, num3, Array.Empty<int>());
			AdobeAnimateSprite adobeAnimateSprite = ownerSprites[adobeAnimateGpuManagedVisualBinding.OwnerIndex];
			AdobeAnimateCpuVisualClassification classification;
			if (adobeAnimateGpuManagedVisualBinding.SourceKind == AdobeAnimateGpuManagedVisualSourceKind.SlotSprite2D)
			{
				AdobeAnimateManagedSlotSprite[] managedSlotSpritesForRender = adobeAnimateSprite.GetManagedSlotSpritesForRender();
				if ((uint)adobeAnimateGpuManagedVisualBinding.VisualIndex >= (uint)managedSlotSpritesForRender.Length)
				{
					errors.Add(Error(scenePath, string.Empty, -1, "unclassified-visual", $"Managed Sprite2D {adobeAnimateGpuManagedVisualBinding.VisualIndex} is unavailable."));
					continue;
				}
				AdobeAnimateManagedSlotSprite visual = managedSlotSpritesForRender[adobeAnimateGpuManagedVisualBinding.VisualIndex];
				bool logicallyVisible = GodotObject.IsInstanceValid(visual.Slot) && GodotObject.IsInstanceValid(visual.Visual) && AdobeAnimateManagedSprite2D.GetLogicalVisible(visual.Visual) && (visual.Slot.Visible || visual.Slot.useFollowVisible);
				classification = ClassifyManagedVisualForPackaging(in visual, expectedAtlasArrayRid, in sortPath, logicallyVisible);
			}
			else
			{
				if (adobeAnimateGpuManagedVisualBinding.SourceKind != AdobeAnimateGpuManagedVisualSourceKind.ExternalVisual || !adobeAnimateSprite.TryGetExternalVisualForRender(adobeAnimateGpuManagedVisualBinding.VisualIndex, out var visual2))
				{
					errors.Add(Error(scenePath, string.Empty, -1, "unclassified-visual", $"External visual {adobeAnimateGpuManagedVisualBinding.VisualIndex} is unavailable."));
					continue;
				}
				classification = ClassifyExternalVisualForPackaging(in visual2, expectedAtlasArrayRid, in sortPath);
			}
			if (classification.Disposition == AdobeAnimateCpuVisualDisposition.NativeSprite && !flag && !flag2)
			{
				errors.Add(Error(scenePath, string.Empty, -1, "native-order-interleave-unsupported", $"Managed binding {j} splits shared Pose slots."));
			}
			AccumulateClassification(scenePath, in classification, nativeBand, ref counters, errors);
		}
	}

	private static int FindGraphSlot(AdobeAnimateGpuRenderSlot[] renderSlots, int staticVisualIndex)
	{
		for (int i = 0; i < renderSlots.Length; i++)
		{
			AdobeAnimateGpuRenderSlot adobeAnimateGpuRenderSlot = renderSlots[i];
			if (adobeAnimateGpuRenderSlot.Kind == AdobeAnimateGpuRenderSlotKind.ManagedSprite2D && adobeAnimateGpuRenderSlot.StaticVisualIndex == staticVisualIndex)
			{
				return i;
			}
		}
		return -1;
	}

	private static VisualCounters ValidateRegisteredExternalVisuals(AdobeAnimateCpuPackagingCatalog catalog, Rid sharedAtlasArrayRid, List<AdobeAnimateCpuValidationError> errors)
	{
		VisualCounters counters = default(VisualCounters);
		for (int i = 0; i < catalog.ExternalTexturePaths.Count; i++)
		{
			string text = catalog.ExternalTexturePaths[i];
			Sprite2D sprite2D = null;
			try
			{
				Texture2D texture = ResourceLoader.Load<Texture2D>(text, string.Empty, ResourceLoader.CacheMode.Ignore);
				sprite2D = new Sprite2D
				{
					Name = PathName(text),
					Texture = texture,
					Visible = true
				};
				AccumulateClassification(text, ClassifyExternalVisualForPackaging(new AdobeAnimateExternalVisualSnapshot(sprite2D, new AdobeAnimateExternalVisualDescriptor(AdobeAnimateExternalVisualAttachmentMode.Root, AdobeAnimateExternalVisualDrawBand.InFrontOfAnimation), visible: true, texture, Transform2D.Identity, Transform2D.Identity, Colors.White, topologyDirty: false, stateDirty: false, 0L), sharedAtlasArrayRid, default(AdobeAnimateSortPath)), AdobeAnimateCpuNativeDrawBand.InFrontOfRootMesh, ref counters, errors);
			}
			catch (Exception ex)
			{
				errors.Add(Error(text, string.Empty, -1, "external-visual-validation-failed", ex.Message));
			}
			finally
			{
				if (GodotObject.IsInstanceValid(sprite2D))
				{
					sprite2D.Free();
				}
			}
		}
		return counters;
	}

	private static void AccumulateClassification(string resourcePath, in AdobeAnimateCpuVisualClassification classification, AdobeAnimateCpuNativeDrawBand nativeBand, ref VisualCounters counters, List<AdobeAnimateCpuValidationError> errors)
	{
		if (classification.Disposition == AdobeAnimateCpuVisualDisposition.IgnoredInvisible)
		{
			return;
		}
		counters.ExpectedVisibleItems++;
		switch (classification.Disposition)
		{
		case AdobeAnimateCpuVisualDisposition.MergedMesh:
			counters.CpuMeshItems++;
			break;
		case AdobeAnimateCpuVisualDisposition.NativeSprite:
			counters.NativeSpriteItems++;
			if (nativeBand == AdobeAnimateCpuNativeDrawBand.BehindRootMesh)
			{
				counters.NativeBehindItems++;
			}
			else
			{
				counters.NativeFrontItems++;
			}
			break;
		default:
			errors.Add(Error(resourcePath, string.Empty, -1, "unclassified-visual", $"code={classification.Failure.Code}, identity={classification.Failure.ResourceIdentity}, detail={classification.Failure.Detail}"));
			break;
		}
	}

	private static string PathName(string resourcePath)
	{
		int num = resourcePath.LastIndexOf('/');
		string text;
		if (num < 0)
		{
			text = resourcePath;
		}
		else
		{
			int num2 = num + 1;
			text = resourcePath.Substring(num2, resourcePath.Length - num2);
		}
		string text2 = text;
		int num3 = text2.LastIndexOf('.');
		if (num3 <= 0)
		{
			return text2;
		}
		return text2.Substring(0, num3);
	}

	private static void SortErrors(List<AdobeAnimateCpuValidationError> errors)
	{
		errors.Sort(delegate(AdobeAnimateCpuValidationError left, AdobeAnimateCpuValidationError right)
		{
			int num = string.Compare(left.ResourcePath, right.ResourcePath, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(left.Clip, right.Clip, StringComparison.Ordinal);
			if (num != 0)
			{
				return num;
			}
			num = left.Frame.CompareTo(right.Frame);
			if (num != 0)
			{
				return num;
			}
			num = string.Compare(left.FailureCode, right.FailureCode, StringComparison.Ordinal);
			return (num != 0) ? num : string.Compare(left.Detail, right.Detail, StringComparison.Ordinal);
		});
	}

	private static AdobeAnimateCpuValidationError Error(string resourcePath, string clip, int frame, string failureCode, string detail)
	{
		return new AdobeAnimateCpuValidationError(resourcePath ?? string.Empty, clip ?? string.Empty, frame, failureCode ?? string.Empty, detail ?? string.Empty);
	}

	private static AdobeAnimateCpuVisualClassification ClassifyManagedVisualForPackaging(in AdobeAnimateManagedSlotSprite visual, Rid expectedAtlasArrayRid, in AdobeAnimateSortPath sortPath, bool logicallyVisible)
	{
		return AdobeAnimateCpuVisualClassifier.ClassifyManagedSlot(in visual, expectedAtlasArrayRid, default(AdobeAnimateDrawItem), in sortPath, logicallyVisible);
	}

	private static AdobeAnimateCpuVisualClassification ClassifyExternalVisualForPackaging(in AdobeAnimateExternalVisualSnapshot visual, Rid expectedAtlasArrayRid, in AdobeAnimateSortPath sortPath)
	{
		return AdobeAnimateCpuVisualClassifier.ClassifyExternalVisual(in visual, expectedAtlasArrayRid, default(AdobeAnimateDrawItem), in sortPath);
	}
}
