using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Godot;
using Godot.Collections;

public sealed class AdobeAnimateDefinitionCache
{
	private sealed class GpuPoseAtlasBakeEntry
	{
		public AdobeAnimateRuntimeDefinition Definition;

		public string SourceKey;

		public int SourceIndex;

		public Image Image;

		public Vector2I Size;

		public int TexelCount;

		public ulong Signature;

		public string DebugPath;
	}

	private sealed class GpuPoseTextureCacheEntry
	{
		public Texture2D Texture;

		public TextureLayered TextureArray;

		public Rid Rid;

		public Vector2I Size;

		public int TextureLayer;

		public bool UsesTextureArray;
	}

	private static readonly System.Collections.Generic.Dictionary<string, AdobeAnimateRuntimeDefinition> Cache = new System.Collections.Generic.Dictionary<string, AdobeAnimateRuntimeDefinition>(StringComparer.Ordinal);

	private static readonly System.Collections.Generic.Dictionary<ulong, GpuPoseTextureCacheEntry> GpuPoseTextureCache = new System.Collections.Generic.Dictionary<ulong, GpuPoseTextureCacheEntry>();

	public const string GeneratedGpuPoseTextureDir = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData";

	public const int GpuPoseTexelsPerSlice = 5;

	internal const Image.Format GpuPoseImageFormat = Image.Format.Rgbah;

	private const int GpuPoseTextureMaxWidth = 2048;

	private const int GpuPoseTextureMaxHeight = 2048;

	private const int GpuPoseTextureVersion = 7;

	private const ulong HashOffset = 1469598103934665603uL;

	private const ulong HashPrime = 1099511628211uL;

	public static string GetBakedGpuPoseTexturePath(AdobeAnimateData data, ulong signature)
	{
		return GetBakedGpuPoseTexturePath(data, signature, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData");
	}

	private static string GetBakedGpuPoseTexturePath(AdobeAnimateData data, ulong signature, string generatedGpuPoseTextureDir)
	{
		ulong value = HashString(data?.GetAtlasSourceKey() ?? "");
		return $"{generatedGpuPoseTextureDir}/AdobeAnimateGpuPose_{value:X16}_{signature:X16}.exr";
	}

	private static string GetExistingBakedGpuPoseTexturePath(AdobeAnimateData data, ulong signature)
	{
		string bakedGpuPoseTexturePath = GetBakedGpuPoseTexturePath(data, signature, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData");
		if (Godot.FileAccess.FileExists(bakedGpuPoseTexturePath) || File.Exists(ProjectSettings.GlobalizePath(bakedGpuPoseTexturePath)))
		{
			return bakedGpuPoseTexturePath;
		}
		return string.Empty;
	}

	public static string GetBakedGpuPoseAtlasPagePath(int visualAtlasPage)
	{
		return GetBakedGpuPoseAtlasPagePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData", visualAtlasPage);
	}

	private static string GetBakedGpuPoseAtlasPagePath(string generatedGpuPoseTextureDir, int visualAtlasPage)
	{
		return $"{generatedGpuPoseTextureDir}/AdobeAnimateGpuPoseAtlasPage_{Math.Max(0, visualAtlasPage)}.exr";
	}

	private static bool TrySaveGpuPoseImage(Image image, string resourcePath, out string reason)
	{
		reason = string.Empty;
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			reason = "pose image is empty";
			return false;
		}
		string text = ProjectSettings.GlobalizePath(resourcePath);
		if (string.IsNullOrEmpty(text))
		{
			reason = "pose image path is invalid";
			return false;
		}
		Error error = image.SaveExr(text);
		if (error != Error.Ok)
		{
			reason = $"failed to save {resourcePath}: {error}";
			return false;
		}
		return true;
	}

	private static void PrepareBakedGpuPoseTextureDirectory(string generatedGpuPoseTextureDir)
	{
		string path = ProjectSettings.GlobalizePath(generatedGpuPoseTextureDir);
		Directory.CreateDirectory(path);
		foreach (string item in Directory.EnumerateFiles(path, "AdobeAnimateGpuPose_*.exr"))
		{
			File.Delete(item);
		}
		foreach (string item2 in Directory.EnumerateFiles(path, "AdobeAnimateGpuPoseAtlasPage_*.exr"))
		{
			File.Delete(item2);
		}
	}

	private static void PushLimitedBakeWarning(ref int warningCount, string message)
	{
		if (warningCount < 32)
		{
			GD.PushWarning(message);
		}
		else if (warningCount == 32)
		{
			GD.PushWarning("AdobeAnimate pose texture bake has more skipped resources; suppressing additional warnings for this run.");
		}
		warningCount++;
	}

	private static string GetResourceDebugPath(AdobeAnimateData data)
	{
		if (data == null)
		{
			return "<null>";
		}
		if (!string.IsNullOrWhiteSpace(data.ResourcePath))
		{
			return data.ResourcePath;
		}
		return data.GetAtlasSourceKey();
	}

	private static float GetDeltaValue(float[] values, int index)
	{
		if (values == null || (uint)index >= (uint)values.Length)
		{
			return 0f;
		}
		return values[index];
	}

	private static void WriteGpuPoseTexel(Image image, int width, int index, Color value)
	{
		int x = index % width;
		int y = index / width;
		image.SetPixel(x, y, value);
	}

	private static Color ReadGpuPoseTexel(Image image, int index)
	{
		int width = image.GetWidth();
		return image.GetPixel(index % width, index / width);
	}

	private static void HashAddRectArray(ref ulong hash, Rect2[] values)
	{
		HashAddInt(ref hash, (values != null) ? values.Length : 0);
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				HashAddFloat(ref hash, values[i].Position.X);
				HashAddFloat(ref hash, values[i].Position.Y);
				HashAddFloat(ref hash, values[i].Size.X);
				HashAddFloat(ref hash, values[i].Size.Y);
			}
		}
	}

	private static void HashAddIntArray(ref ulong hash, int[] values)
	{
		HashAddInt(ref hash, (values != null) ? values.Length : 0);
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				HashAddInt(ref hash, values[i]);
			}
		}
	}

	private static void HashAddFloatArray(ref ulong hash, float[] values)
	{
		HashAddInt(ref hash, (values != null) ? values.Length : 0);
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				HashAddFloat(ref hash, values[i]);
			}
		}
	}

	private static ulong HashString(string value)
	{
		ulong hash = 1469598103934665603uL;
		HashAddString(ref hash, value);
		return hash;
	}

	private static void HashAddString(ref ulong hash, string value)
	{
		if (value == null)
		{
			value = string.Empty;
		}
		HashAddInt(ref hash, value.Length);
		for (int i = 0; i < value.Length; i++)
		{
			HashAdd(ref hash, value[i]);
		}
	}

	private static void HashAddInt(ref ulong hash, int value)
	{
		HashAdd(ref hash, (ulong)value);
	}

	private static void HashAddFloat(ref ulong hash, float value)
	{
		HashAdd(ref hash, BitConverter.SingleToUInt32Bits(value));
	}

	private static void HashAdd(ref ulong hash, ulong value)
	{
		hash ^= value;
		hash *= 1099511628211uL;
	}

	private static AdobeAnimateRuntimeDefinition Build(AdobeAnimateData data, bool allowBakedGpuPoseTexture = true, bool buildGpuPoseTexture = false, bool allowRuntimeGpuPoseTextureFallback = false, bool useTexturelessManifestAllocation = false, AdobeAnimateGlobalAtlasManifest metadataManifest = null, AdobeAnimateAtlasProfile atlasProfile = null)
	{
		Rect2[] array = BuildMediaRects(data);
		PackedClip[] clips = BuildClips(data);
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = new AdobeAnimateRuntimeDefinition
		{
			Source = data,
			AtlasProfile = atlasProfile,
			SourceAuthoringRevision = data.AuthoringRevision,
			MediaRects = array,
			MediaNameToId = BuildNameLookup(data.mediaDictionary),
			LayerNameToId = BuildNameLookup(data.layerDictionary),
			Clips = clips,
			ClipRangesByName = BuildClipRangeLookup(clips),
			Events = BuildEvents(data),
			ReplaceSlots = BuildReplaceSlots(data),
			FrameRate = data.frameRate,
			FrameMax = Math.Max(0, data.frameMax)
		};
		if ((!useTexturelessManifestAllocation) ? ((atlasProfile == null) ? AdobeAnimateGlobalAtlasCache.TryGetOrBuild(data, array, out var allocation) : AdobeAnimateGlobalAtlasCache.TryGetOrBuild(data, array, atlasProfile, out allocation)) : ((metadataManifest != null) ? AdobeAnimateGlobalAtlasCache.TryGetManifestMetadataAllocation(metadataManifest, data.GetAtlasSourceKey(), array.Length, out allocation) : AdobeAnimateGlobalAtlasCache.TryGetManifestMetadataAllocation(data.GetAtlasSourceKey(), array.Length, out allocation)))
		{
			adobeAnimateRuntimeDefinition.BaseAtlas = allocation.Texture;
			adobeAnimateRuntimeDefinition.BaseAtlasRid = allocation.TextureRid;
			adobeAnimateRuntimeDefinition.BaseAtlasPage = allocation.PageIndex;
			adobeAnimateRuntimeDefinition.AtlasTextureArray = allocation.TextureArray;
			adobeAnimateRuntimeDefinition.AtlasTextureArrayRid = allocation.TextureArrayRid;
			adobeAnimateRuntimeDefinition.AtlasTextureArraySize = allocation.TextureArraySize;
			adobeAnimateRuntimeDefinition.UsesAtlasTextureArrayLayout = allocation.UsesTextureArrayLayout;
			adobeAnimateRuntimeDefinition.BaseAtlasSize = (adobeAnimateRuntimeDefinition.UsesAtlasTextureArrayLayout ? adobeAnimateRuntimeDefinition.AtlasTextureArraySize : (GodotObject.IsInstanceValid(adobeAnimateRuntimeDefinition.BaseAtlas) ? adobeAnimateRuntimeDefinition.BaseAtlas.GetSize() : Vector2.Zero));
			adobeAnimateRuntimeDefinition.AtlasPages = allocation.Textures;
			adobeAnimateRuntimeDefinition.AtlasPageRids = allocation.TextureRids;
			adobeAnimateRuntimeDefinition.MediaAtlasPages = allocation.MediaAtlasPages;
			adobeAnimateRuntimeDefinition.MediaRects = allocation.MediaRects;
		}
		if (HasPackedSlices(data))
		{
			BuildFromPacked(data, adobeAnimateRuntimeDefinition);
		}
		adobeAnimateRuntimeDefinition.HasRepeatedMediaPerFrame = DetectRepeatedMediaPerFrame(adobeAnimateRuntimeDefinition);
		adobeAnimateRuntimeDefinition.RuntimeLayerCount = Math.Max(adobeAnimateRuntimeDefinition.RuntimeLayerCount, GetLookupLayerCount(adobeAnimateRuntimeDefinition.LayerNameToId));
		BuildMediaTextureLookups(adobeAnimateRuntimeDefinition);
		BuildMediaRenderInfoLookups(adobeAnimateRuntimeDefinition);
		BuildFrameLayoutSignatures(adobeAnimateRuntimeDefinition);
		adobeAnimateRuntimeDefinition.LocalBounds = BuildLocalBounds(adobeAnimateRuntimeDefinition);
		adobeAnimateRuntimeDefinition.FrameLocalBounds = BuildFrameLocalBounds(adobeAnimateRuntimeDefinition);
		if (buildGpuPoseTexture)
		{
			if (allowBakedGpuPoseTexture)
			{
				TryUseBakedGpuPoseTexture(data, adobeAnimateRuntimeDefinition);
			}
			if (!adobeAnimateRuntimeDefinition.GpuPoseTextureRid.IsValid && allowRuntimeGpuPoseTextureFallback)
			{
				BuildGpuPoseTexture(adobeAnimateRuntimeDefinition);
			}
		}
		return adobeAnimateRuntimeDefinition;
	}

	private static void BuildMediaTextureLookups(AdobeAnimateRuntimeDefinition definition)
	{
		Rect2[] mediaRects = definition.MediaRects;
		int num = ((mediaRects != null) ? mediaRects.Length : 0);
		if (num <= 0)
		{
			definition.MediaTextures = System.Array.Empty<Texture2D>();
			definition.MediaTextureRids = System.Array.Empty<Rid>();
			definition.MediaTextureSizes = System.Array.Empty<Vector2>();
			definition.HasSingleAtlasPage = false;
			return;
		}
		definition.MediaTextures = new Texture2D[num];
		definition.MediaTextureRids = new Rid[num];
		definition.MediaTextureSizes = new Vector2[num];
		bool flag = false;
		bool flag2 = true;
		int num2 = 0;
		Rid rid = default(Rid);
		Texture2D singleAtlasTexture = null;
		Vector2 singleAtlasTextureSize = Vector2.Zero;
		for (int num3 = 0; num3 < num; num3++)
		{
			int num4 = ResolveMediaAtlasPage(definition, num3);
			int num5;
			object obj;
			if (definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(definition.AtlasTextureArray) && definition.AtlasTextureArraySize.X > 0f)
			{
				num5 = ((definition.AtlasTextureArraySize.Y > 0f) ? 1 : 0);
				if (num5 != 0)
				{
					obj = null;
					goto IL_00fc;
				}
			}
			else
			{
				num5 = 0;
			}
			obj = ((num4 >= 0 && num4 < definition.AtlasPages.Length) ? definition.AtlasPages[num4] : definition.BaseAtlas);
			goto IL_00fc;
			IL_00fc:
			Texture2D texture2D = (Texture2D)obj;
			Rid rid2 = ((num5 != 0) ? definition.AtlasTextureArrayRid : ((num4 >= 0 && num4 < definition.AtlasPageRids.Length) ? definition.AtlasPageRids[num4] : definition.BaseAtlasRid));
			Vector2 vector = ((num5 != 0) ? definition.AtlasTextureArraySize : definition.BaseAtlasSize);
			if ((vector.X <= 0f || vector.Y <= 0f) && GodotObject.IsInstanceValid(texture2D))
			{
				vector = texture2D.GetSize();
			}
			definition.MediaTextures[num3] = texture2D;
			definition.MediaTextureRids[num3] = rid2;
			definition.MediaTextureSizes[num3] = vector;
			if (!flag)
			{
				flag = true;
				num2 = num4;
				rid = rid2;
				singleAtlasTexture = texture2D;
				singleAtlasTextureSize = vector;
			}
			else if (num4 != num2 || (rid.IsValid && rid2.IsValid && rid2 != rid))
			{
				flag2 = false;
			}
		}
		definition.HasSingleAtlasPage = flag2 && flag && (rid.IsValid || (singleAtlasTextureSize.X > 0f && singleAtlasTextureSize.Y > 0f));
		definition.SingleAtlasPage = num2;
		definition.SingleAtlasTexture = singleAtlasTexture;
		definition.SingleAtlasTextureRid = rid;
		definition.SingleAtlasTextureSize = singleAtlasTextureSize;
	}

	private static int ResolveMediaAtlasPage(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		if (definition.MediaAtlasPages != null && mediaId >= 0 && mediaId < definition.MediaAtlasPages.Length)
		{
			return definition.MediaAtlasPages[mediaId];
		}
		return definition.BaseAtlasPage;
	}

	private static void BuildMediaRenderInfoLookups(AdobeAnimateRuntimeDefinition definition)
	{
		int valueOrDefault = (definition?.MediaRects?.Length).GetValueOrDefault();
		if (valueOrDefault <= 0)
		{
			if (definition != null)
			{
				definition.MediaRenderInfos = System.Array.Empty<PackedSliceRenderInfo>();
			}
			return;
		}
		PackedSliceRenderInfo[] array = new PackedSliceRenderInfo[valueOrDefault];
		for (int i = 0; i < valueOrDefault; i++)
		{
			Rect2 rect = definition.MediaRects[i];
			if (!(rect.Size.X <= 0f) && !(rect.Size.Y <= 0f))
			{
				Vector2 vector = ResolveMediaAtlasSize(definition, i);
				float num = Math.Max(1f, vector.X);
				float num2 = Math.Max(1f, vector.Y);
				array[i] = new PackedSliceRenderInfo(uvRect: new Rect2(new Vector2(rect.Position.X / num, rect.Position.Y / num2), new Vector2(rect.Size.X / num, rect.Size.Y / num2)), sourceSize: rect.Size, atlasLayer: Math.Max(0, ResolveMediaAtlasPage(definition, i)));
			}
		}
		definition.MediaRenderInfos = array;
	}

	public static bool TryGetBaseSliceRenderInfo(AdobeAnimateRuntimeDefinition definition, int mediaId, out PackedSliceRenderInfo renderInfo)
	{
		renderInfo = default(PackedSliceRenderInfo);
		if (definition?.MediaRenderInfos == null || mediaId == 65535 || (uint)mediaId >= (uint)definition.MediaRenderInfos.Length)
		{
			return false;
		}
		renderInfo = definition.MediaRenderInfos[mediaId];
		return renderInfo.IsValid;
	}

	private static Vector2 ResolveMediaAtlasSize(AdobeAnimateRuntimeDefinition definition, int mediaId)
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
		if (definition.MediaTextures != null && mediaId >= 0 && mediaId < definition.MediaTextures.Length)
		{
			Texture2D texture2D = definition.MediaTextures[mediaId];
			if (GodotObject.IsInstanceValid(texture2D))
			{
				return texture2D.GetSize();
			}
		}
		if (GodotObject.IsInstanceValid(definition.BaseAtlas))
		{
			return definition.BaseAtlas.GetSize();
		}
		return Vector2.One;
	}

	public static bool IsBaseSliceRenderable(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		if (definition?.MediaRects == null || mediaId == 65535 || (uint)mediaId >= (uint)definition.MediaRects.Length)
		{
			return false;
		}
		Vector2 size = definition.MediaRects[mediaId].Size;
		if (size.X > 0f)
		{
			return size.Y > 0f;
		}
		return false;
	}

	private static void BuildFrameLayoutSignatures(AdobeAnimateRuntimeDefinition definition)
	{
		PackedFrame[] array = definition?.Frames ?? System.Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		if (definition == null || array.Length == 0)
		{
			return;
		}
		ulong[] array3 = new ulong[array.Length];
		ulong[] array4 = new ulong[array.Length];
		float[] array5 = definition.Source?.sliceTransforms ?? System.Array.Empty<float>();
		float[] array6 = definition.Source?.sliceAlpha ?? System.Array.Empty<float>();
		for (int i = 0; i < array.Length; i++)
		{
			PackedFrame packedFrame = array[i];
			int num = Math.Max(0, packedFrame.Offset);
			int num2 = Math.Min(array2.Length, num + Math.Max(0, packedFrame.Count));
			ulong value = 1469598103934665603uL;
			ulong value2 = 1469598103934665603uL;
			Add(ref value, (uint)(num2 - num));
			Add(ref value2, (uint)(num2 - num));
			for (int j = num; j < num2; j++)
			{
				ref PackedSliceMetadata reference = ref array2[j];
				Add(ref value, reference.MediaId);
				Add(ref value, reference.LayerId);
				Add(ref value, (uint)reference.DrawOrder);
				Add(ref value, (ulong)(IsBaseSliceRenderable(definition, reference.MediaId) ? 1 : 0));
				Add(ref value2, (uint)reference.SliceKey);
				Add(ref value2, reference.MediaId);
				Add(ref value2, reference.LayerId);
				Add(ref value2, (uint)reference.DrawOrder);
				Add(ref value2, (uint)reference.Flags);
				int num3 = j * 6;
				if (num3 + 5 < array5.Length)
				{
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3]));
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3 + 1]));
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3 + 2]));
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3 + 3]));
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3 + 4]));
					Add(ref value2, BitConverter.SingleToUInt32Bits(array5[num3 + 5]));
					Add(ref value2, BitConverter.SingleToUInt32Bits((j < array6.Length) ? array6[j] : 1f));
				}
			}
			array3[i] = value;
			array4[i] = value2;
		}
		definition.FrameLayoutSignatures = array3;
		definition.FramePoseSignatures = array4;
		static void Add(ref ulong reference2, ulong component)
		{
			reference2 ^= component;
			reference2 *= 1099511628211uL;
		}
	}

	private static void BuildGpuFrameSlotLookup(AdobeAnimateRuntimeDefinition definition)
	{
		PackedFrame[] array = definition?.Frames ?? System.Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		if (definition != null)
		{
			definition.GpuRenderSlotOrderStable = false;
		}
		if (definition == null || array.Length == 0 || array2.Length == 0)
		{
			if (definition != null)
			{
				definition.GpuRenderLocalSlots = System.Array.Empty<AdobeAnimateGpuLocalSlot>();
				definition.GpuFrameSlotLookup = System.Array.Empty<AdobeAnimateGpuFrameSlotEntry>();
			}
			return;
		}
		List<(int, int)> list = new List<(int, int)>();
		List<(int, int)> list2 = new List<(int, int)>();
		System.Collections.Generic.Dictionary<(int, int), int> dictionary = new System.Collections.Generic.Dictionary<(int, int), int>();
		for (int i = 0; i < array.Length; i++)
		{
			PackedFrame packedFrame = array[i];
			int num = Math.Max(0, packedFrame.Offset);
			int num2 = Math.Min(array2.Length, num + Math.Max(0, packedFrame.Count));
			System.Collections.Generic.Dictionary<int, int> dictionary2 = new System.Collections.Generic.Dictionary<int, int>();
			for (int j = num; j < num2; j++)
			{
				ref PackedSliceMetadata reference = ref array2[j];
				if (IsBaseSliceRenderable(definition, reference.MediaId))
				{
					int sliceKey = reference.SliceKey;
					dictionary2.TryGetValue(sliceKey, out var value);
					dictionary2[sliceKey] = value + 1;
					(int, int) tuple = (sliceKey, value);
					if (!dictionary.ContainsKey(tuple))
					{
						dictionary.Add(tuple, list.Count);
						list.Add(tuple);
						list2.Add((reference.LayerId, reference.DrawOrder));
					}
				}
			}
		}
		HashSet<int>[] array3 = new HashSet<int>[list.Count];
		int[] indegrees = new int[list.Count];
		for (int k = 0; k < array3.Length; k++)
		{
			array3[k] = new HashSet<int>();
		}
		for (int l = 0; l < array.Length; l++)
		{
			PackedFrame packedFrame2 = array[l];
			int num3 = Math.Max(0, packedFrame2.Offset);
			int num4 = Math.Min(array2.Length, num3 + Math.Max(0, packedFrame2.Count));
			System.Collections.Generic.Dictionary<int, int> dictionary3 = new System.Collections.Generic.Dictionary<int, int>();
			int before = -1;
			for (int m = num3; m < num4; m++)
			{
				ref PackedSliceMetadata reference2 = ref array2[m];
				if (IsBaseSliceRenderable(definition, reference2.MediaId))
				{
					int sliceKey2 = reference2.SliceKey;
					dictionary3.TryGetValue(sliceKey2, out var value2);
					dictionary3[sliceKey2] = value2 + 1;
					int num5 = dictionary[(sliceKey2, value2)];
					AddGpuRenderSlotOrderEdge(array3, indegrees, before, num5);
					before = num5;
				}
			}
		}
		TrySortGpuRenderSlots(array3, indegrees, out var sortedSlotIndices);
		if (sortedSlotIndices.Count != list.Count)
		{
			definition.GpuRenderLocalSlots = System.Array.Empty<AdobeAnimateGpuLocalSlot>();
			definition.GpuFrameSlotLookup = System.Array.Empty<AdobeAnimateGpuFrameSlotEntry>();
			return;
		}
		AdobeAnimateGpuLocalSlot[] array4 = new AdobeAnimateGpuLocalSlot[sortedSlotIndices.Count];
		System.Collections.Generic.Dictionary<(int, int), int> dictionary4 = new System.Collections.Generic.Dictionary<(int, int), int>(sortedSlotIndices.Count);
		for (int n = 0; n < sortedSlotIndices.Count; n++)
		{
			int index = sortedSlotIndices[n];
			(int, int) key = list[index];
			(int, int) tuple2 = list2[index];
			array4[n] = new AdobeAnimateGpuLocalSlot(key.Item1, key.Item2, tuple2.Item1, tuple2.Item2);
			dictionary4.Add(key, n);
		}
		AdobeAnimateGpuFrameSlotEntry value3 = new AdobeAnimateGpuFrameSlotEntry(0, 65535, -1, visible: false);
		AdobeAnimateGpuFrameSlotEntry[] array5 = new AdobeAnimateGpuFrameSlotEntry[array.Length * array4.Length];
		System.Array.Fill(array5, value3);
		for (int num6 = 0; num6 < array.Length; num6++)
		{
			PackedFrame packedFrame3 = array[num6];
			int num7 = Math.Max(0, packedFrame3.Offset);
			int num8 = Math.Min(array2.Length, num7 + Math.Max(0, packedFrame3.Count));
			System.Collections.Generic.Dictionary<int, int> dictionary5 = new System.Collections.Generic.Dictionary<int, int>();
			for (int num9 = num7; num9 < num8; num9++)
			{
				ref PackedSliceMetadata reference3 = ref array2[num9];
				if (IsBaseSliceRenderable(definition, reference3.MediaId))
				{
					int sliceKey3 = reference3.SliceKey;
					dictionary5.TryGetValue(sliceKey3, out var value4);
					dictionary5[sliceKey3] = value4 + 1;
					if (dictionary4.TryGetValue((sliceKey3, value4), out var value5))
					{
						int poseTexel = definition.GpuPoseTextureBaseTexel + num9 * 5;
						array5[num6 * array4.Length + value5] = new AdobeAnimateGpuFrameSlotEntry(poseTexel, reference3.MediaId, reference3.LayerId, visible: true);
					}
				}
			}
		}
		definition.GpuRenderLocalSlots = array4;
		definition.GpuFrameSlotLookup = array5;
		definition.GpuRenderSlotOrderStable = true;
	}

	private static void AddGpuRenderSlotOrderEdge(HashSet<int>[] edges, int[] indegrees, int before, int after)
	{
		if (before >= 0 && after >= 0 && before != after && edges[before].Add(after))
		{
			indegrees[after]++;
		}
	}

	private static void TrySortGpuRenderSlots(HashSet<int>[] edges, int[] indegrees, out List<int> sortedSlotIndices)
	{
		sortedSlotIndices = new List<int>(indegrees.Length);
		SortedSet<int> sortedSet = new SortedSet<int>();
		for (int i = 0; i < indegrees.Length; i++)
		{
			if (indegrees[i] == 0)
			{
				sortedSet.Add(i);
			}
		}
		while (sortedSet.Count > 0)
		{
			int min = sortedSet.Min;
			sortedSet.Remove(min);
			sortedSlotIndices.Add(min);
			foreach (int item in edges[min])
			{
				indegrees[item]--;
				if (indegrees[item] == 0)
				{
					sortedSet.Add(item);
				}
			}
		}
	}

	public static bool TryGetSliceInFrame(AdobeAnimateRuntimeDefinition definition, int frameIndex, PackedFrame frame, int sliceKey, out PackedSlicePose slice)
	{
		slice = default(PackedSlicePose);
		if (!TryGetSliceIndexInFrame(definition, frameIndex, frame, sliceKey, out var absoluteIndex))
		{
			return false;
		}
		if (!EnsureCpuPoseData(definition) || (uint)absoluteIndex >= (uint)definition.Slices.Length)
		{
			return false;
		}
		slice = definition.Slices[absoluteIndex];
		return true;
	}

	public static bool TryGetSliceIndexInFrame(AdobeAnimateRuntimeDefinition definition, int frameIndex, PackedFrame frame, int sliceKey, out int absoluteIndex)
	{
		absoluteIndex = -1;
		PackedSliceMetadata[] array = definition?.SliceMetadata;
		if (array == null || array.Length == 0 || frame.Count <= 0)
		{
			return false;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, frame.Count));
		for (int i = num; i < num2; i++)
		{
			if (array[i].SliceKey == sliceKey)
			{
				absoluteIndex = i;
				return true;
			}
		}
		return false;
	}

	public static bool TryGetMatchingSliceIndexInFrame(AdobeAnimateRuntimeDefinition definition, PackedFrame sourceFrame, PackedFrame targetFrame, int targetAbsoluteIndex, out int sourceAbsoluteIndex)
	{
		sourceAbsoluteIndex = -1;
		PackedSliceMetadata[] array = definition?.SliceMetadata;
		if (array == null || array.Length == 0 || sourceFrame.Count <= 0 || targetFrame.Count <= 0)
		{
			return false;
		}
		int num = Math.Max(0, sourceFrame.Offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, sourceFrame.Count));
		int num3 = Math.Max(0, targetFrame.Offset);
		int num4 = Math.Min(array.Length, num3 + Math.Max(0, targetFrame.Count));
		if (targetAbsoluteIndex < num3 || targetAbsoluteIndex >= num4)
		{
			return false;
		}
		int sliceKey = array[targetAbsoluteIndex].SliceKey;
		int num5 = 0;
		for (int i = num3; i < targetAbsoluteIndex; i++)
		{
			if (array[i].SliceKey == sliceKey)
			{
				num5++;
			}
		}
		int num6 = 0;
		for (int j = num; j < num2; j++)
		{
			if (array[j].SliceKey == sliceKey)
			{
				if (num6 == num5)
				{
					sourceAbsoluteIndex = j;
					return true;
				}
				num6++;
			}
		}
		return false;
	}

	public static bool TryGetLayerSliceInFrame(AdobeAnimateRuntimeDefinition definition, int frameIndex, PackedFrame frame, int layerId, out PackedSlicePose slice)
	{
		slice = default(PackedSlicePose);
		PackedSliceMetadata[] array = definition?.SliceMetadata;
		if (array == null || array.Length == 0 || frame.Count <= 0)
		{
			return false;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, frame.Count));
		for (int i = num; i < num2; i++)
		{
			if (array[i].LayerId == layerId)
			{
				if (!EnsureCpuPoseData(definition) || (uint)i >= (uint)definition.Slices.Length)
				{
					return false;
				}
				slice = definition.Slices[i];
				return true;
			}
		}
		return false;
	}

	public static bool TryGetFrameLayerMaxDrawOrder(AdobeAnimateRuntimeDefinition definition, int frameIndex, PackedFrame frame, int layerId, out int drawOrder)
	{
		drawOrder = -1;
		PackedSliceMetadata[] array = definition?.SliceMetadata;
		if (array == null || array.Length == 0 || frame.Count <= 0 || layerId < 0)
		{
			return false;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, frame.Count));
		for (int i = num; i < num2; i++)
		{
			ref PackedSliceMetadata reference = ref array[i];
			if (reference.LayerId == layerId)
			{
				drawOrder = Math.Max(drawOrder, reference.DrawOrder);
			}
		}
		return drawOrder >= 0;
	}

	public static bool TryGetDrawOrderSliceInFrame(AdobeAnimateRuntimeDefinition definition, int frameIndex, PackedFrame frame, int drawOrder, out PackedSlicePose slice)
	{
		slice = default(PackedSlicePose);
		PackedSliceMetadata[] array = definition?.SliceMetadata;
		if (array == null || array.Length == 0 || frame.Count <= 0)
		{
			return false;
		}
		int num = Math.Max(0, frame.Offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, frame.Count));
		for (int i = num; i < num2; i++)
		{
			if (array[i].DrawOrder == drawOrder)
			{
				if (!EnsureCpuPoseData(definition) || (uint)i >= (uint)definition.Slices.Length)
				{
					return false;
				}
				slice = definition.Slices[i];
				return true;
			}
		}
		return false;
	}

	public static AdobeAnimateRuntimeDefinition GetOrBuild(AdobeAnimateData data)
	{
		return GetOrBuild(data, null);
	}

	public static AdobeAnimateRuntimeDefinition GetOrBuild(AdobeAnimateData data, AdobeAnimateAtlasProfile atlasProfile)
	{
		if (data == null)
		{
			return null;
		}
		string cacheKey = GetCacheKey(data, atlasProfile);
		if (!data.HasPackedRuntimeData() && data.EnsurePackedRuntimeData())
		{
			Cache.Remove(cacheKey);
			cacheKey = GetCacheKey(data, atlasProfile);
		}
		if (Cache.TryGetValue(cacheKey, out var value) && value.Source == data && value.AtlasProfile == atlasProfile && value.SourceAuthoringRevision == data.AuthoringRevision)
		{
			return value;
		}
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = Build(data, allowBakedGpuPoseTexture: true, buildGpuPoseTexture: true, allowRuntimeGpuPoseTextureFallback: false, useTexturelessManifestAllocation: false, null, atlasProfile);
		Cache[cacheKey] = adobeAnimateRuntimeDefinition;
		return adobeAnimateRuntimeDefinition;
	}

	internal static AdobeAnimateRuntimeDefinition BuildForPackagingValidation(AdobeAnimateData data)
	{
		if (data == null)
		{
			return null;
		}
		if (!data.HasPackedRuntimeData() && !data.EnsurePackedRuntimeData())
		{
			return null;
		}
		return Build(data, allowBakedGpuPoseTexture: true, buildGpuPoseTexture: true);
	}

	public static AdobeAnimateRuntimeDefinition GetOrBuildForCompactCrowd(AdobeAnimateData data)
	{
		AdobeAnimateRuntimeDefinition orBuild = GetOrBuild(data);
		if (HasCompactCrowdTextureArrays(orBuild) || (orBuild != null && orBuild.CompactCrowdRecoveryAttempted) || data == null)
		{
			return orBuild;
		}
		TextureLayered instance = AdobeAnimateGlobalAtlasCache.PreloadVisualTextureArray();
		TextureLayered instance2 = AdobeAnimateGlobalAtlasCache.PreloadGpuPoseTextureArray();
		if (!GodotObject.IsInstanceValid(instance) || !GodotObject.IsInstanceValid(instance2))
		{
			return orBuild;
		}
		string cacheKey = GetCacheKey(data);
		Cache.Remove(cacheKey);
		orBuild = Build(data, allowBakedGpuPoseTexture: true, buildGpuPoseTexture: true, allowRuntimeGpuPoseTextureFallback: true);
		if (orBuild != null)
		{
			orBuild.CompactCrowdRecoveryAttempted = true;
		}
		Cache[cacheKey] = orBuild;
		return orBuild;
	}

	private static bool HasCompactCrowdTextureArrays(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition != null && definition.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(definition.AtlasTextureArray) && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f && definition.UsesGpuPoseTextureArray && definition.GpuPoseTextureRid.IsValid && GodotObject.IsInstanceValid(definition.GpuPoseTextureArray) && definition.GpuPoseTextureSize.X > 0)
		{
			return definition.GpuPoseTextureSize.Y > 0;
		}
		return false;
	}

	public static void Invalidate(AdobeAnimateData data)
	{
		if (data == null)
		{
			return;
		}
		List<string> list = new List<string>();
		foreach (KeyValuePair<string, AdobeAnimateRuntimeDefinition> item in Cache)
		{
			if (item.Value?.Source == data)
			{
				list.Add(item.Key);
			}
		}
		for (int i = 0; i < list.Count; i++)
		{
			Cache.Remove(list[i]);
		}
		AdobeAnimateGlobalAtlasCache.Invalidate(data);
	}

	public static void ReleaseProfile(AdobeAnimateAtlasProfile atlasProfile)
	{
		if (atlasProfile != null)
		{
			string[] array = (from pair in Cache
				where pair.Value?.AtlasProfile == atlasProfile
				select pair.Key).ToArray();
			foreach (string key in array)
			{
				Cache.Remove(key);
			}
			AdobeAnimateGlobalAtlasCache.ReleaseProfile(atlasProfile);
		}
	}

	public static void Clear()
	{
		Cache.Clear();
		GpuPoseTextureCache.Clear();
		AdobeAnimateGlobalAtlasCache.Clear();
	}

	private static string GetCacheKey(AdobeAnimateData data)
	{
		return $"{AdobeAnimateGlobalAtlasCache.CacheVersion}:{data?.GetAtlasSourceKey() ?? string.Empty}";
	}

	private static string GetCacheKey(AdobeAnimateData data, AdobeAnimateAtlasProfile atlasProfile)
	{
		if (atlasProfile == null)
		{
			return GetCacheKey(data);
		}
		return $"{AdobeAnimateGlobalAtlasCache.CacheVersion}:profile={atlasProfile.GetStableCacheKey()}:{data?.GetAtlasSourceKey() ?? string.Empty}";
	}

	public static bool TryGetCpuPoseTrack(AdobeAnimateRuntimeDefinition definition, int key, bool useLayerId, out PackedCpuPoseSample[] track)
	{
		track = System.Array.Empty<PackedCpuPoseSample>();
		if (definition == null || definition.Frames == null || definition.Frames.Length == 0 || definition.SliceMetadata == null || key < 0)
		{
			return false;
		}
		long key2 = ((long)(useLayerId ? 1 : 0) << 32) | (uint)key;
		if (!definition.CpuPoseTracks.TryGetValue(key2, out track))
		{
			PackedCpuPoseSample[] value = BuildCpuPoseTrack(definition, key, useLayerId);
			track = definition.CpuPoseTracks.GetOrAdd(key2, value);
		}
		return track.Length == definition.Frames.Length;
	}

	public static bool TryGetCpuPoseSample(AdobeAnimateRuntimeDefinition definition, int frameIndex, int key, bool useLayerId, out int mediaId, out Transform2D transform)
	{
		mediaId = -1;
		transform = Transform2D.Identity;
		if (!TryGetCpuPoseTrack(definition, key, useLayerId, out var track))
		{
			return false;
		}
		frameIndex = Math.Clamp(frameIndex, 0, track.Length - 1);
		if ((uint)frameIndex >= (uint)track.Length || !track[frameIndex].Valid)
		{
			return false;
		}
		mediaId = track[frameIndex].MediaId;
		transform = track[frameIndex].Transform;
		return true;
	}

	private static PackedCpuPoseSample[] BuildCpuPoseTrack(AdobeAnimateRuntimeDefinition definition, int key, bool useLayerId)
	{
		PackedFrame[] array = definition.Frames ?? System.Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = definition.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		float[] array3 = definition.Source?.sliceTransforms ?? System.Array.Empty<float>();
		PackedCpuPoseSample[] array4 = new PackedCpuPoseSample[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			PackedFrame packedFrame = array[i];
			int num = Math.Max(0, packedFrame.Offset);
			int num2 = Math.Min(array2.Length, num + Math.Max(0, packedFrame.Count));
			for (int j = num; j < num2; j++)
			{
				ref PackedSliceMetadata reference = ref array2[j];
				if (!(useLayerId ? (reference.LayerId != key) : (reference.DrawOrder != key)))
				{
					int num3 = j * 6;
					if (num3 + 5 < array3.Length)
					{
						array4[i] = new PackedCpuPoseSample(transform: new Transform2D(new Vector2(array3[num3], array3[num3 + 1]), new Vector2(array3[num3 + 2], array3[num3 + 3]), new Vector2(array3[num3 + 4], array3[num3 + 5])), mediaId: reference.MediaId, valid: true);
					}
					break;
				}
			}
		}
		return array4;
	}

	public static bool TryGetInterpolatedSlicePose(AdobeAnimateRuntimeDefinition definition, int sourceIndex, float interpolationT, out int mediaId, out Transform2D transform, out float alpha)
	{
		mediaId = -1;
		transform = Transform2D.Identity;
		alpha = 0f;
		if (definition == null || definition.SliceMetadata == null || (uint)sourceIndex >= (uint)definition.SliceMetadata.Length || !EnsureCpuPoseData(definition) || (uint)sourceIndex >= (uint)definition.Slices.Length)
		{
			return false;
		}
		PackedSlicePose packedSlicePose = definition.Slices[sourceIndex];
		float num = Mathf.Clamp(interpolationT, 0f, 1f);
		if (num > 0f)
		{
			EnsureCpuInterpolationData(definition);
			if (definition.HasNextSliceDeltas != null && sourceIndex < definition.HasNextSliceDeltas.Length && definition.HasNextSliceDeltas[sourceIndex])
			{
				packedSlicePose.Xx += GetDeltaValue(definition.NextDeltaXx, sourceIndex) * num;
				packedSlicePose.Xy += GetDeltaValue(definition.NextDeltaXy, sourceIndex) * num;
				packedSlicePose.Yx += GetDeltaValue(definition.NextDeltaYx, sourceIndex) * num;
				packedSlicePose.Yy += GetDeltaValue(definition.NextDeltaYy, sourceIndex) * num;
				packedSlicePose.Ox += GetDeltaValue(definition.NextDeltaOx, sourceIndex) * num;
				packedSlicePose.Oy += GetDeltaValue(definition.NextDeltaOy, sourceIndex) * num;
				packedSlicePose.Alpha += GetDeltaValue(definition.NextDeltaAlpha, sourceIndex) * num;
			}
		}
		mediaId = definition.SliceMetadata[sourceIndex].MediaId;
		transform = new Transform2D(new Vector2(packedSlicePose.Xx, packedSlicePose.Xy), new Vector2(packedSlicePose.Yx, packedSlicePose.Yy), new Vector2(packedSlicePose.Ox, packedSlicePose.Oy));
		alpha = packedSlicePose.Alpha;
		return true;
	}

	private static Rect2[] BuildMediaRects(AdobeAnimateData data)
	{
		if (data.mediaRects != null && data.mediaRects.Length != 0)
		{
			Rect2[] array = new Rect2[data.mediaRects.Length];
			for (int i = 0; i < array.Length; i++)
			{
				Vector4 vector = data.mediaRects[i];
				array[i] = new Rect2(vector.X, vector.Y, vector.Z, vector.W);
			}
			return array;
		}
		Rect2[] array2 = new Rect2[data.mediaDictionary?.Count ?? 0];
		for (int j = 0; j < array2.Length; j++)
		{
			array2[j] = data.GetMediaRect(j);
		}
		return array2;
	}

	private static System.Collections.Generic.Dictionary<StringName, int> BuildNameLookup(Dictionary source)
	{
		System.Collections.Generic.Dictionary<StringName, int> dictionary = new System.Collections.Generic.Dictionary<StringName, int>();
		if (source == null)
		{
			return dictionary;
		}
		foreach (Variant key2 in source.Keys)
		{
			StringName key = key2.AsStringName();
			dictionary[key] = source[key2].AsInt32();
		}
		return dictionary;
	}

	private static int GetLookupLayerCount(System.Collections.Generic.Dictionary<StringName, int> lookup)
	{
		if (lookup == null || lookup.Count == 0)
		{
			return 0;
		}
		int num = 0;
		foreach (int value in lookup.Values)
		{
			if (value >= 0)
			{
				num = Math.Max(num, value + 1);
			}
		}
		return num;
	}

	private static PackedClip[] BuildClips(AdobeAnimateData data)
	{
		if (data.clips == null)
		{
			return System.Array.Empty<PackedClip>();
		}
		List<PackedClip> list = new List<PackedClip>(data.clips.Count);
		foreach (Variant key in data.clips.Keys)
		{
			list.Add(new PackedClip(key.AsStringName(), (Vector2I)data.clips[key]));
		}
		return list.ToArray();
	}

	private static System.Collections.Generic.Dictionary<string, Vector2I> BuildClipRangeLookup(PackedClip[] clips)
	{
		System.Collections.Generic.Dictionary<string, Vector2I> dictionary = new System.Collections.Generic.Dictionary<string, Vector2I>((clips != null) ? clips.Length : 0, StringComparer.Ordinal);
		if (clips == null)
		{
			return dictionary;
		}
		for (int i = 0; i < clips.Length; i++)
		{
			dictionary[clips[i].Name.ToString()] = clips[i].Range;
		}
		return dictionary;
	}

	private static PackedEventFrame[] BuildEvents(AdobeAnimateData data)
	{
		if (data.events == null)
		{
			return System.Array.Empty<PackedEventFrame>();
		}
		List<PackedEventFrame> list = new List<PackedEventFrame>();
		for (int i = 0; i < data.events.Count; i++)
		{
			if (data.events[i] != null && data.events[i].Count > 0)
			{
				list.Add(new PackedEventFrame(i, data.events[i]));
			}
		}
		return list.ToArray();
	}

	private static PackedReplaceSlot[] BuildReplaceSlots(AdobeAnimateData data)
	{
		if (data.replaceSlotDictionary == null || data.replaceSlotDictionary.Count == 0)
		{
			return System.Array.Empty<PackedReplaceSlot>();
		}
		List<PackedReplaceSlot> list = new List<PackedReplaceSlot>();
		foreach (Variant key in data.replaceSlotDictionary.Keys)
		{
			StringName stringName = key.AsStringName();
			int num = ((data.mediaDictionary != null && data.mediaDictionary.ContainsKey(stringName)) ? data.mediaDictionary[stringName].AsInt32() : (-1));
			Vector2 size = ((num >= 0) ? data.GetMediaRect(num).Size : Vector2.Zero);
			list.Add(new PackedReplaceSlot(stringName, num, size));
		}
		return list.ToArray();
	}

	private static bool HasPackedSlices(AdobeAnimateData data)
	{
		return data?.HasPackedRuntimeData() ?? false;
	}

	private static void BuildFromPacked(AdobeAnimateData data, AdobeAnimateRuntimeDefinition definition)
	{
		definition.Frames = new PackedFrame[data.frameOffsets.Length];
		for (int i = 0; i < definition.Frames.Length; i++)
		{
			definition.Frames[i] = new PackedFrame(data.frameOffsets[i], data.frameCounts[i]);
			definition.MaxFrameSliceCount = Math.Max(definition.MaxFrameSliceCount, data.frameCounts[i]);
		}
		int num = data.sliceKeys.Length;
		definition.SliceMetadata = new PackedSliceMetadata[num];
		for (int j = 0; j < num; j++)
		{
			int num2 = Math.Clamp((j < data.sliceLayerIds.Length) ? data.sliceLayerIds[j] : 0, 0, 65535);
			definition.RuntimeLayerCount = Math.Max(definition.RuntimeLayerCount, num2 + 1);
			PackedSliceMetadata packedSliceMetadata = new PackedSliceMetadata(data.sliceKeys[j], (ushort)Math.Clamp((j < data.sliceMediaIds.Length) ? data.sliceMediaIds[j] : 0, 0, 65535), (ushort)num2, (j < data.sliceDrawOrders.Length) ? data.sliceDrawOrders[j] : j, (j < data.sliceFlags.Length) ? data.sliceFlags[j] : 0);
			definition.SliceMetadata[j] = packedSliceMetadata;
		}
		definition.Slices = System.Array.Empty<PackedSlicePose>();
		definition.CpuPoseDataResident = false;
		definition.CpuInterpolationDataResident = false;
		definition.FrameMax = definition.Frames.Length;
	}

	private static bool BuildCpuPosesFromPacked(AdobeAnimateData data, AdobeAnimateRuntimeDefinition definition)
	{
		int valueOrDefault = (definition?.SliceMetadata?.Length).GetValueOrDefault();
		if (data == null || definition == null || valueOrDefault == 0 || data.sliceTransforms == null || data.sliceTransforms.Length < valueOrDefault * 6)
		{
			if (definition != null)
			{
				definition.Slices = System.Array.Empty<PackedSlicePose>();
				definition.CpuPoseDataResident = false;
			}
			return false;
		}
		PackedSlicePose[] array = new PackedSlicePose[valueOrDefault];
		for (int i = 0; i < valueOrDefault; i++)
		{
			int num = i * 6;
			PackedSliceMetadata packedSliceMetadata = definition.SliceMetadata[i];
			array[i] = new PackedSlicePose
			{
				SliceKey = packedSliceMetadata.SliceKey,
				MediaId = packedSliceMetadata.MediaId,
				LayerId = packedSliceMetadata.LayerId,
				DrawOrder = packedSliceMetadata.DrawOrder,
				Flags = packedSliceMetadata.Flags,
				Xx = data.sliceTransforms[num],
				Xy = data.sliceTransforms[num + 1],
				Yx = data.sliceTransforms[num + 2],
				Yy = data.sliceTransforms[num + 3],
				Ox = data.sliceTransforms[num + 4],
				Oy = data.sliceTransforms[num + 5],
				Alpha = ((i < data.sliceAlpha.Length) ? data.sliceAlpha[i] : 1f)
			};
		}
		definition.Slices = array;
		definition.CpuPoseDataResident = true;
		return true;
	}

	private static bool DetectRepeatedMediaPerFrame(AdobeAnimateRuntimeDefinition definition)
	{
		PackedFrame[] array = definition?.Frames ?? System.Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		if (array.Length == 0 || array2.Length == 0)
		{
			return false;
		}
		HashSet<int> hashSet = new HashSet<int>();
		for (int i = 0; i < array.Length; i++)
		{
			hashSet.Clear();
			PackedFrame packedFrame = array[i];
			int num = Math.Max(0, packedFrame.Offset);
			int num2 = Math.Min(array2.Length, num + Math.Max(0, packedFrame.Count));
			for (int j = num; j < num2; j++)
			{
				if (!hashSet.Add(array2[j].MediaId))
				{
					return true;
				}
			}
		}
		return false;
	}

	private static Rect2 BuildLocalBounds(AdobeAnimateRuntimeDefinition definition)
	{
		int valueOrDefault = (definition?.SliceMetadata?.Length).GetValueOrDefault();
		return BuildSliceRangeBounds(definition, 0, valueOrDefault, CreateFallbackLocalBounds());
	}

	private static Rect2[] BuildFrameLocalBounds(AdobeAnimateRuntimeDefinition definition)
	{
		PackedFrame[] array = definition?.Frames ?? System.Array.Empty<PackedFrame>();
		if (array.Length == 0)
		{
			return System.Array.Empty<Rect2>();
		}
		Rect2 fallbackBounds = definition?.LocalBounds ?? CreateFallbackLocalBounds();
		Rect2[] array2 = new Rect2[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			PackedFrame packedFrame = array[i];
			array2[i] = BuildSliceRangeBounds(definition, packedFrame.Offset, packedFrame.Count, fallbackBounds);
		}
		return array2;
	}

	private static Rect2 BuildSliceRangeBounds(AdobeAnimateRuntimeDefinition definition, int offset, int count, Rect2 fallbackBounds)
	{
		PackedSliceMetadata[] array = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		float[] array2 = definition?.Source?.sliceTransforms ?? System.Array.Empty<float>();
		Rect2[] array3 = definition?.MediaRects ?? System.Array.Empty<Rect2>();
		bool hasPoint = false;
		float minX = 0f;
		float minY = 0f;
		float maxX = 0f;
		float maxY = 0f;
		int num = Math.Max(0, offset);
		int num2 = Math.Min(array.Length, num + Math.Max(0, count));
		for (int i = num; i < num2; i++)
		{
			int num3 = i * 6;
			if (num3 + 5 >= array2.Length)
			{
				break;
			}
			int mediaId = array[i].MediaId;
			if ((uint)mediaId < (uint)array3.Length)
			{
				Vector2 size = array3[mediaId].Size;
				if (!(size.X <= 0f) && !(size.Y <= 0f))
				{
					PackedSlicePose slice = new PackedSlicePose
					{
						Xx = array2[num3],
						Xy = array2[num3 + 1],
						Yx = array2[num3 + 2],
						Yy = array2[num3 + 3],
						Ox = array2[num3 + 4],
						Oy = array2[num3 + 5]
					};
					IncludeSliceBoundsPoint(ref hasPoint, ref minX, ref minY, ref maxX, ref maxY, in slice, 0f, 0f);
					IncludeSliceBoundsPoint(ref hasPoint, ref minX, ref minY, ref maxX, ref maxY, in slice, size.X, 0f);
					IncludeSliceBoundsPoint(ref hasPoint, ref minX, ref minY, ref maxX, ref maxY, in slice, 0f, size.Y);
					IncludeSliceBoundsPoint(ref hasPoint, ref minX, ref minY, ref maxX, ref maxY, in slice, size.X, size.Y);
				}
			}
		}
		if (!hasPoint)
		{
			if (!(fallbackBounds.Size.X > 0f) || !(fallbackBounds.Size.Y > 0f))
			{
				return CreateFallbackLocalBounds();
			}
			return fallbackBounds;
		}
		return new Rect2(new Vector2(minX, minY), new Vector2(Math.Max(1f, maxX - minX), Math.Max(1f, maxY - minY)));
	}

	private static Rect2 CreateFallbackLocalBounds()
	{
		return new Rect2(new Vector2(-128f, -128f), new Vector2(256f, 256f));
	}

	private static void IncludeSliceBoundsPoint(ref bool hasPoint, ref float minX, ref float minY, ref float maxX, ref float maxY, in PackedSlicePose slice, float x, float y)
	{
		float num = slice.Xx * x + slice.Yx * y + slice.Ox;
		float num2 = slice.Xy * x + slice.Yy * y + slice.Oy;
		if (!hasPoint)
		{
			minX = (maxX = num);
			minY = (maxY = num2);
			hasPoint = true;
			return;
		}
		if (num < minX)
		{
			minX = num;
		}
		if (num2 < minY)
		{
			minY = num2;
		}
		if (num > maxX)
		{
			maxX = num;
		}
		if (num2 > maxY)
		{
			maxY = num2;
		}
	}

	private static void BuildNextSliceLookup(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return;
		}
		PackedFrame[] frames = definition.Frames;
		PackedSlicePose[] slices = definition.Slices;
		PackedSliceMetadata[] sliceMetadata = definition.SliceMetadata;
		if (frames.Length == 0 || slices.Length == 0 || sliceMetadata.Length != slices.Length)
		{
			definition.NextSliceIndices = System.Array.Empty<int>();
			definition.HasNextSliceDeltas = System.Array.Empty<bool>();
			definition.NextDeltaXx = System.Array.Empty<float>();
			definition.NextDeltaXy = System.Array.Empty<float>();
			definition.NextDeltaYx = System.Array.Empty<float>();
			definition.NextDeltaYy = System.Array.Empty<float>();
			definition.NextDeltaOx = System.Array.Empty<float>();
			definition.NextDeltaOy = System.Array.Empty<float>();
			definition.NextDeltaAlpha = System.Array.Empty<float>();
			definition.CpuInterpolationDataResident = true;
			return;
		}
		int[] array = new int[slices.Length];
		bool[] array2 = new bool[slices.Length];
		float[] array3 = new float[slices.Length];
		float[] array4 = new float[slices.Length];
		float[] array5 = new float[slices.Length];
		float[] array6 = new float[slices.Length];
		float[] array7 = new float[slices.Length];
		float[] array8 = new float[slices.Length];
		float[] array9 = new float[slices.Length];
		System.Array.Fill(array, -1);
		for (int i = 0; i < frames.Length; i++)
		{
			PackedFrame packedFrame = frames[i];
			if (packedFrame.Count <= 0)
			{
				continue;
			}
			int num = Math.Min(i + 1, frames.Length - 1);
			PackedFrame frame = frames[num];
			int num2 = Math.Min(packedFrame.Offset + packedFrame.Count, slices.Length);
			int num3 = packedFrame.Offset;
			int num4 = 0;
			while (num3 < num2)
			{
				int num5 = -1;
				if (num == i)
				{
					num5 = num3;
				}
				else
				{
					ref PackedSliceMetadata reference = ref sliceMetadata[num3];
					int num6 = frame.Offset + num4;
					int absoluteIndex;
					if (num4 < frame.Count && (uint)num6 < (uint)slices.Length && sliceMetadata[num6].SliceKey == reference.SliceKey)
					{
						num5 = num6;
					}
					else if (TryGetSliceIndexInFrame(definition, num, frame, reference.SliceKey, out absoluteIndex))
					{
						num5 = absoluteIndex;
					}
				}
				array[num3] = num5;
				if ((uint)num5 < (uint)slices.Length && num5 != num3)
				{
					ref PackedSlicePose reference2 = ref slices[num3];
					ref PackedSlicePose reference3 = ref slices[num5];
					float num7 = reference3.Xx - reference2.Xx;
					float num8 = reference3.Xy - reference2.Xy;
					float num9 = reference3.Yx - reference2.Yx;
					float num10 = reference3.Yy - reference2.Yy;
					float num11 = reference3.Ox - reference2.Ox;
					float num12 = reference3.Oy - reference2.Oy;
					float num13 = reference3.Alpha - reference2.Alpha;
					if (num7 != 0f || num8 != 0f || num9 != 0f || num10 != 0f || num11 != 0f || num12 != 0f || num13 != 0f)
					{
						array2[num3] = true;
						array3[num3] = num7;
						array4[num3] = num8;
						array5[num3] = num9;
						array6[num3] = num10;
						array7[num3] = num11;
						array8[num3] = num12;
						array9[num3] = num13;
					}
				}
				num3++;
				num4++;
			}
		}
		definition.NextSliceIndices = array;
		definition.HasNextSliceDeltas = array2;
		definition.NextDeltaXx = array3;
		definition.NextDeltaXy = array4;
		definition.NextDeltaYx = array5;
		definition.NextDeltaYy = array6;
		definition.NextDeltaOx = array7;
		definition.NextDeltaOy = array8;
		definition.NextDeltaAlpha = array9;
		definition.CpuInterpolationDataResident = true;
	}

	public static void EnsureCpuInterpolationData(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null || definition.CpuInterpolationDataResident || !EnsureCpuPoseData(definition))
		{
			return;
		}
		lock (definition)
		{
			if (!definition.CpuInterpolationDataResident)
			{
				BuildNextSliceLookup(definition);
			}
		}
	}

	public static bool EnsureCpuPoseData(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return false;
		}
		PackedSliceMetadata[] sliceMetadata = definition.SliceMetadata;
		int num = ((sliceMetadata != null) ? sliceMetadata.Length : 0);
		if (definition.CpuPoseDataResident && definition.Slices != null && definition.Slices.Length == num)
		{
			return true;
		}
		lock (definition)
		{
			if (definition.CpuPoseDataResident && definition.Slices != null && definition.Slices.Length == num)
			{
				return true;
			}
			AdobeAnimateData source = definition.Source;
			if (source == null || (!source.HasPackedRuntimeData() && !source.EnsurePackedRuntimeData()))
			{
				return false;
			}
			if (!BuildCpuPosesFromPacked(source, definition))
			{
				return false;
			}
			BuildNextSliceLookup(definition);
			return true;
		}
	}

	private static void ReleaseCpuInterpolationData(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition != null)
		{
			definition.NextSliceIndices = System.Array.Empty<int>();
			definition.HasNextSliceDeltas = System.Array.Empty<bool>();
			definition.NextDeltaXx = System.Array.Empty<float>();
			definition.NextDeltaXy = System.Array.Empty<float>();
			definition.NextDeltaYx = System.Array.Empty<float>();
			definition.NextDeltaYy = System.Array.Empty<float>();
			definition.NextDeltaOx = System.Array.Empty<float>();
			definition.NextDeltaOy = System.Array.Empty<float>();
			definition.NextDeltaAlpha = System.Array.Empty<float>();
			definition.CpuInterpolationDataResident = false;
		}
	}

	public static void ReleaseCpuPoseDataIfBaked(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null || !definition.UsesBakedGpuPoseTexture)
		{
			return;
		}
		lock (definition)
		{
			ReleaseCpuInterpolationData(definition);
			definition.Slices = System.Array.Empty<PackedSlicePose>();
			definition.CpuPoseDataResident = false;
		}
	}

	public static int RefreshBakedGpuPoseTextures(IReadOnlyList<AdobeAnimateData> resources, out int skipped)
	{
		return RefreshBakedGpuPoseTextures(resources, null, out skipped);
	}

	public static int RefreshBakedGpuPoseTextures(IReadOnlyList<AdobeAnimateData> resources, AdobeAnimateGlobalAtlasManifest manifest, out int skipped)
	{
		return RefreshBakedGpuPoseTextures(resources, manifest, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData", out skipped);
	}

	public static int RefreshBakedGpuPoseTextures(IReadOnlyList<AdobeAnimateData> resources, AdobeAnimateGlobalAtlasManifest manifest, string generatedGpuPoseTextureDir, out int skipped)
	{
		skipped = 0;
		if (resources == null || resources.Count == 0)
		{
			return 0;
		}
		PrepareBakedGpuPoseTextureDirectory(generatedGpuPoseTextureDir);
		if (manifest != null && manifest.SourceKeys != null && manifest.SourceKeys.Count > 0 && manifest.MediaAtlasPages != null && manifest.MediaAtlasPages.Count > 0)
		{
			return RefreshBakedGpuPoseAtlases(resources, manifest, generatedGpuPoseTextureDir, out skipped);
		}
		int num = 0;
		int warningCount = 0;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < resources.Count; i++)
		{
			AdobeAnimateData adobeAnimateData = resources[i];
			if (adobeAnimateData == null)
			{
				skipped++;
				continue;
			}
			string text = ((!string.IsNullOrWhiteSpace(adobeAnimateData.ResourcePath)) ? adobeAnimateData.ResourcePath : adobeAnimateData.GetAtlasSourceKey());
			if (!hashSet.Add(text))
			{
				continue;
			}
			if (TryBakeGpuPoseTexture(adobeAnimateData, out var _, out var reason, generatedGpuPoseTextureDir))
			{
				num++;
				continue;
			}
			skipped++;
			if (!string.IsNullOrEmpty(reason))
			{
				PushLimitedBakeWarning(ref warningCount, "Skip AdobeAnimate pose texture bake " + text + ": " + reason);
			}
		}
		return num;
	}

	private static int RefreshBakedGpuPoseAtlases(IReadOnlyList<AdobeAnimateData> resources, AdobeAnimateGlobalAtlasManifest manifest, string generatedGpuPoseTextureDir, out int skipped)
	{
		skipped = 0;
		ResetPoseAtlasManifestFields(manifest);
		System.Collections.Generic.Dictionary<string, int> dictionary = new System.Collections.Generic.Dictionary<string, int>(StringComparer.Ordinal);
		for (int i = 0; i < manifest.SourceKeys.Count; i++)
		{
			string text = manifest.SourceKeys[i];
			if (!string.IsNullOrEmpty(text) && !dictionary.ContainsKey(text))
			{
				dictionary[text] = i;
			}
		}
		List<GpuPoseAtlasBakeEntry> list = new List<GpuPoseAtlasBakeEntry>();
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		int warningCount = 0;
		int num = 0;
		for (int j = 0; j < resources.Count; j++)
		{
			AdobeAnimateData adobeAnimateData = resources[j];
			if (adobeAnimateData == null)
			{
				skipped++;
				continue;
			}
			string atlasSourceKey = adobeAnimateData.GetAtlasSourceKey();
			string item = ((!string.IsNullOrWhiteSpace(adobeAnimateData.ResourcePath)) ? adobeAnimateData.ResourcePath : atlasSourceKey);
			if (!hashSet.Add(item))
			{
				continue;
			}
			GpuPoseAtlasBakeEntry entry;
			string reason;
			if (!dictionary.TryGetValue(atlasSourceKey, out var value))
			{
				skipped++;
				PushLimitedBakeWarning(ref warningCount, "Skip AdobeAnimate pose atlas bake " + GetResourceDebugPath(adobeAnimateData) + ": source is missing from the visual atlas manifest.");
			}
			else if (!TryBuildGpuPoseAtlasBakeEntry(adobeAnimateData, manifest, atlasSourceKey, value, out entry, out reason))
			{
				if (TryBakeGpuPoseTexture(adobeAnimateData, out var _, out var reason2, generatedGpuPoseTextureDir, useTexturelessManifestAllocation: true, manifest))
				{
					num++;
					continue;
				}
				skipped++;
				string text2 = ((!string.IsNullOrEmpty(reason)) ? reason : reason2);
				if (!string.IsNullOrEmpty(text2))
				{
					PushLimitedBakeWarning(ref warningCount, "Skip AdobeAnimate pose atlas bake " + GetResourceDebugPath(adobeAnimateData) + ": " + text2);
				}
			}
			else
			{
				list.Add(entry);
			}
		}
		int skipped2;
		int num2 = TrySaveGpuPoseAtlasPages(list, manifest, generatedGpuPoseTextureDir, ref warningCount, out skipped2);
		skipped += skipped2;
		return num2 + num;
	}

	private static void ResetPoseAtlasManifestFields(AdobeAnimateGlobalAtlasManifest manifest)
	{
		manifest.PoseTextureArrayPath = string.Empty;
		manifest.PoseTextureArrayLayerSize = Vector2I.Zero;
		manifest.PoseTextureArrayLayerCount = 0;
		manifest.PoseTextureArrayColumns = 0;
		manifest.PoseAtlasPagePaths.Clear();
		manifest.SourcePoseAtlasPages.Clear();
		manifest.SourcePoseTexelStarts.Clear();
		manifest.SourcePoseTexelCounts.Clear();
		manifest.SourcePoseSignatures.Clear();
		int num = manifest.SourceKeys?.Count ?? 0;
		for (int i = 0; i < num; i++)
		{
			manifest.SourcePoseAtlasPages.Add(-1);
			manifest.SourcePoseTexelStarts.Add(0);
			manifest.SourcePoseTexelCounts.Add(0);
			manifest.SourcePoseSignatures.Add(string.Empty);
		}
	}

	private static bool TryBuildGpuPoseAtlasBakeEntry(AdobeAnimateData data, AdobeAnimateGlobalAtlasManifest manifest, string sourceKey, int sourceIndex, out GpuPoseAtlasBakeEntry entry, out string reason)
	{
		entry = null;
		reason = string.Empty;
		if (data == null)
		{
			reason = "source data is null";
			return false;
		}
		if (!data.HasPackedRuntimeData() && !data.EnsurePackedRuntimeData())
		{
			reason = "packed runtime data is unavailable";
			return false;
		}
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = Build(data, allowBakedGpuPoseTexture: false, buildGpuPoseTexture: false, allowRuntimeGpuPoseTextureFallback: false, useTexturelessManifestAllocation: true, manifest);
		if (adobeAnimateRuntimeDefinition == null || !EnsureCpuPoseData(adobeAnimateRuntimeDefinition))
		{
			reason = "animation has no packed slices";
			return false;
		}
		if (!HasUsableAtlasAllocation(adobeAnimateRuntimeDefinition))
		{
			reason = "shared atlas allocation is unavailable";
			return false;
		}
		ulong signature = ComputeGpuPoseSignature(data, adobeAnimateRuntimeDefinition);
		Vector2I size;
		Image image = BuildGpuPoseImage(adobeAnimateRuntimeDefinition, out size);
		int texelCount = Math.Max(1, adobeAnimateRuntimeDefinition.Slices.Length * 5);
		if (image == null || size.X <= 0 || size.Y <= 0)
		{
			reason = "pose texture image could not be built";
			return false;
		}
		if (!HasReadableGpuPoseImage(image, adobeAnimateRuntimeDefinition.Slices.Length))
		{
			reason = "pose texture image contains no readable slice rects";
			return false;
		}
		entry = new GpuPoseAtlasBakeEntry
		{
			Definition = adobeAnimateRuntimeDefinition,
			SourceKey = sourceKey,
			SourceIndex = sourceIndex,
			Image = image,
			Size = size,
			TexelCount = texelCount,
			Signature = signature,
			DebugPath = GetResourceDebugPath(data)
		};
		return true;
	}

	private static int TrySaveGpuPoseAtlasPages(List<GpuPoseAtlasBakeEntry> entries, AdobeAnimateGlobalAtlasManifest manifest, string generatedGpuPoseTextureDir, ref int warningCount, out int skipped)
	{
		skipped = 0;
		if (entries == null || entries.Count == 0)
		{
			return 0;
		}
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(generatedGpuPoseTextureDir));
		Image image = null;
		int num = -1;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < entries.Count; i++)
		{
			GpuPoseAtlasBakeEntry gpuPoseAtlasBakeEntry = entries[i];
			if (gpuPoseAtlasBakeEntry == null || gpuPoseAtlasBakeEntry.TexelCount <= 0)
			{
				skipped++;
				continue;
			}
			if (gpuPoseAtlasBakeEntry.TexelCount > 4194304)
			{
				skipped++;
				PushLimitedBakeWarning(ref warningCount, $"Skip AdobeAnimate pose atlas bake {gpuPoseAtlasBakeEntry.DebugPath}: pose data {gpuPoseAtlasBakeEntry.TexelCount} texels exceeds one {2048}x{2048} page.");
				continue;
			}
			if (image == null || num2 + gpuPoseAtlasBakeEntry.TexelCount > 4194304)
			{
				if (!TrySaveGpuPoseAtlasPageImage(image, num, manifest, generatedGpuPoseTextureDir, ref warningCount))
				{
					skipped += num4 + entries.Count - i;
					return Math.Max(0, num3 - num4);
				}
				num++;
				num2 = 0;
				num4 = 0;
				image = Image.CreateEmpty(2048, 2048, useMipmaps: false, Image.Format.Rgbah);
				image.Fill(new Color(0f, 0f, 0f, 0f));
			}
			int num5 = num2;
			CopyGpuPoseImage(gpuPoseAtlasBakeEntry.Image, gpuPoseAtlasBakeEntry.Size, image, 2048, num5, gpuPoseAtlasBakeEntry.TexelCount);
			num2 += gpuPoseAtlasBakeEntry.TexelCount;
			manifest.SourcePoseAtlasPages[gpuPoseAtlasBakeEntry.SourceIndex] = num;
			manifest.SourcePoseTexelStarts[gpuPoseAtlasBakeEntry.SourceIndex] = num5;
			manifest.SourcePoseTexelCounts[gpuPoseAtlasBakeEntry.SourceIndex] = gpuPoseAtlasBakeEntry.TexelCount;
			manifest.SourcePoseSignatures[gpuPoseAtlasBakeEntry.SourceIndex] = gpuPoseAtlasBakeEntry.Signature.ToString("X16", CultureInfo.InvariantCulture);
			num3++;
			num4++;
		}
		if (!TrySaveGpuPoseAtlasPageImage(image, num, manifest, generatedGpuPoseTextureDir, ref warningCount))
		{
			skipped += num4;
			return Math.Max(0, num3 - num4);
		}
		return num3;
	}

	private static bool TrySaveGpuPoseAtlasPageImage(Image pageImage, int pageIndex, AdobeAnimateGlobalAtlasManifest manifest, string generatedGpuPoseTextureDir, ref int warningCount)
	{
		if (pageImage == null || pageIndex < 0)
		{
			return true;
		}
		string bakedGpuPoseAtlasPagePath = GetBakedGpuPoseAtlasPagePath(generatedGpuPoseTextureDir, pageIndex);
		if (!TrySaveGpuPoseImage(pageImage, bakedGpuPoseAtlasPagePath, out var reason))
		{
			PushLimitedBakeWarning(ref warningCount, "Failed to save AdobeAnimate pose atlas page " + bakedGpuPoseAtlasPagePath + ": " + reason);
			return false;
		}
		while (manifest.PoseAtlasPagePaths.Count <= pageIndex)
		{
			manifest.PoseAtlasPagePaths.Add(string.Empty);
		}
		manifest.PoseAtlasPagePaths[pageIndex] = bakedGpuPoseAtlasPagePath;
		return true;
	}

	private static void CopyGpuPoseImage(Image source, Vector2I sourceSize, Image target, int targetWidth, int targetStartTexel, int texelCount)
	{
		for (int i = 0; i < texelCount; i++)
		{
			int x = i % sourceSize.X;
			int y = i / sourceSize.X;
			int num = targetStartTexel + i;
			int x2 = num % targetWidth;
			int y2 = num / targetWidth;
			target.SetPixel(x2, y2, source.GetPixel(x, y));
		}
	}

	public static bool TryBakeGpuPoseTexture(AdobeAnimateData data, out string texturePath, out string reason)
	{
		return TryBakeGpuPoseTexture(data, out texturePath, out reason, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData");
	}

	private static bool TryBakeGpuPoseTexture(AdobeAnimateData data, out string texturePath, out string reason, string generatedGpuPoseTextureDir, bool useTexturelessManifestAllocation = false, AdobeAnimateGlobalAtlasManifest metadataManifest = null)
	{
		texturePath = string.Empty;
		reason = string.Empty;
		if (data == null)
		{
			reason = "source data is null";
			return false;
		}
		if (!data.HasPackedRuntimeData() && !data.EnsurePackedRuntimeData())
		{
			reason = "packed runtime data is unavailable";
			return false;
		}
		AdobeAnimateRuntimeDefinition adobeAnimateRuntimeDefinition = Build(data, allowBakedGpuPoseTexture: false, buildGpuPoseTexture: false, allowRuntimeGpuPoseTextureFallback: false, useTexturelessManifestAllocation, metadataManifest);
		if (adobeAnimateRuntimeDefinition == null || !EnsureCpuPoseData(adobeAnimateRuntimeDefinition))
		{
			reason = "animation has no packed slices";
			return false;
		}
		if (!HasUsableAtlasAllocation(adobeAnimateRuntimeDefinition))
		{
			reason = "shared atlas allocation is unavailable";
			return false;
		}
		ulong signature = ComputeGpuPoseSignature(data, adobeAnimateRuntimeDefinition);
		Vector2I size;
		Image image = BuildGpuPoseImage(adobeAnimateRuntimeDefinition, out size);
		if (image == null || size.X <= 0 || size.Y <= 0)
		{
			reason = "pose texture image could not be built";
			return false;
		}
		if (!HasReadableGpuPoseImage(image, adobeAnimateRuntimeDefinition.Slices.Length))
		{
			reason = "pose texture image contains no readable slice rects";
			return false;
		}
		texturePath = GetBakedGpuPoseTexturePath(data, signature, generatedGpuPoseTextureDir);
		Directory.CreateDirectory(ProjectSettings.GlobalizePath(generatedGpuPoseTextureDir));
		if (!TrySaveGpuPoseImage(image, texturePath, out var reason2))
		{
			reason = reason2;
			return false;
		}
		return true;
	}

	private static bool HasUsableAtlasAllocation(AdobeAnimateRuntimeDefinition definition)
	{
		if (definition == null)
		{
			return false;
		}
		if (definition.AtlasTextureArrayRid.IsValid && GodotObject.IsInstanceValid(definition.AtlasTextureArray))
		{
			return true;
		}
		if (GodotObject.IsInstanceValid(definition.BaseAtlas) && definition.BaseAtlasRid.IsValid)
		{
			return true;
		}
		if (definition.AtlasPages == null || definition.AtlasPageRids == null)
		{
			return false;
		}
		int num = Math.Min(definition.AtlasPages.Length, definition.AtlasPageRids.Length);
		for (int i = 0; i < num; i++)
		{
			if (GodotObject.IsInstanceValid(definition.AtlasPages[i]) && definition.AtlasPageRids[i].IsValid)
			{
				return true;
			}
		}
		if (definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f)
		{
			return true;
		}
		return false;
	}

	private static bool TryUseBakedGpuPoseTexture(AdobeAnimateData data, AdobeAnimateRuntimeDefinition definition)
	{
		string sourceKey = data?.GetAtlasSourceKey() ?? string.Empty;
		int valueOrDefault = (definition?.SliceMetadata?.Length).GetValueOrDefault();
		if (definition == null || valueOrDefault == 0)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.noSlices");
			return false;
		}
		ulong num = (definition.GpuPoseSignature = ComputeGpuPoseSignature(data, definition));
		int num2 = Math.Max(1, valueOrDefault * 5);
		if ((definition.AtlasProfile == null) ? AdobeAnimateGlobalAtlasCache.TryGetPoseAtlasManifestEntry(sourceKey, out var signature, out var texelCount) : AdobeAnimateGlobalAtlasCache.TryGetPoseAtlasManifestEntry(definition.AtlasProfile, sourceKey, out signature, out texelCount))
		{
			definition.HasGpuPoseManifestEntry = true;
			definition.GpuPoseManifestSignature = signature;
			definition.GpuPoseManifestTexelCount = texelCount;
		}
		if ((definition.AtlasProfile == null) ? AdobeAnimateGlobalAtlasCache.TryGetPoseAtlasAllocation(sourceKey, num, num2, out var allocation) : AdobeAnimateGlobalAtlasCache.TryGetPoseAtlasAllocation(definition.AtlasProfile, sourceKey, num, num2, out allocation))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.hit.manifest", num2);
			ApplyGpuPoseTexture(definition, allocation.Texture, allocation.TextureSize, num, baked: true, allocation.TexelStart, cacheTexture: false, allocation.TextureArray, allocation.TextureArrayRid, allocation.AtlasPage, allocation.UsesTextureArray);
			return true;
		}
		TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.manifest", num2);
		if (definition.AtlasProfile != null)
		{
			return false;
		}
		Vector2I gpuPoseTextureSize = GetGpuPoseTextureSize(valueOrDefault);
		if (TryApplyCachedGpuPoseTexture(definition, num, gpuPoseTextureSize, baked: false))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.hit.cache", num2);
			return true;
		}
		string existingBakedGpuPoseTexturePath = GetExistingBakedGpuPoseTexturePath(data, num);
		if (string.IsNullOrEmpty(existingBakedGpuPoseTexturePath))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.noBakedPath", num2);
			return false;
		}
		Texture2D texture2D = AdobeAnimateGlobalAtlasCache.LoadGeneratedTextureWithImageFallback(existingBakedGpuPoseTexturePath, ResourceLoader.CacheMode.Ignore);
		if (!GodotObject.IsInstanceValid(texture2D))
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.loadTexture", num2);
			return false;
		}
		Vector2 size = texture2D.GetSize();
		if ((int)size.X != gpuPoseTextureSize.X || (int)size.Y != gpuPoseTextureSize.Y)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.textureSize", num2);
			return false;
		}
		Image image = texture2D.GetImage();
		Texture2DArray textureArray;
		Rid textureArrayRid;
		try
		{
			if (!TryCreateGpuPoseTextureArray(image, gpuPoseTextureSize, num2, out textureArray, out textureArrayRid))
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.miss.createArray", num2);
				return false;
			}
		}
		finally
		{
			image?.Dispose();
		}
		TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.hit.standalone", num2);
		ApplyGpuPoseTexture(definition, null, gpuPoseTextureSize, num, baked: true, 0, cacheTexture: true, textureArray, textureArrayRid, 0, usesTextureArray: true);
		return true;
	}

	private static void BuildGpuPoseTexture(AdobeAnimateRuntimeDefinition definition)
	{
		if (!EnsureCpuPoseData(definition))
		{
			return;
		}
		Vector2I size;
		Image image = BuildGpuPoseImage(definition, out size);
		if (image == null || size.X <= 0 || size.Y <= 0)
		{
			return;
		}
		try
		{
			ulong signature = ((definition.GpuPoseSignature != 0L) ? definition.GpuPoseSignature : ComputeGpuPoseSignature(definition.Source, definition));
			if (!TryApplyCachedGpuPoseTexture(definition, signature, size, baked: false))
			{
				int texelCount = Math.Max(1, definition.Slices.Length * 5);
				if (TryCreateGpuPoseTextureArray(image, size, texelCount, out var textureArray, out var textureArrayRid))
				{
					ApplyGpuPoseTexture(definition, null, size, signature, baked: false, 0, cacheTexture: true, textureArray, textureArrayRid, 0, usesTextureArray: true);
				}
			}
		}
		finally
		{
			image.Dispose();
		}
	}

	private static Image BuildGpuPoseImage(AdobeAnimateRuntimeDefinition definition, out Vector2I size)
	{
		size = Vector2I.Zero;
		if (definition == null || definition.Slices == null || definition.Slices.Length == 0)
		{
			return null;
		}
		size = GetGpuPoseTextureSize(definition.Slices.Length);
		int x = size.X;
		Image image = Image.CreateEmpty(x, size.Y, useMipmaps: false, Image.Format.Rgbah);
		Rect2[] array = definition.MediaRects ?? System.Array.Empty<Rect2>();
		for (int i = 0; i < definition.Slices.Length; i++)
		{
			ref PackedSlicePose reference = ref definition.Slices[i];
			Rect2 rect = ((reference.MediaId < array.Length) ? array[reference.MediaId] : default(Rect2));
			Texture2D texture2D = ((reference.MediaId < definition.MediaTextures.Length) ? definition.MediaTextures[reference.MediaId] : definition.BaseAtlas);
			int val = ResolveMediaAtlasPage(definition, reference.MediaId);
			Vector2 vector = ((definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f) ? definition.AtlasTextureArraySize : ((reference.MediaId >= 0 && reference.MediaId < definition.MediaTextureSizes.Length) ? definition.MediaTextureSizes[reference.MediaId] : definition.BaseAtlasSize));
			if ((vector.X <= 0f || vector.Y <= 0f) && GodotObject.IsInstanceValid(texture2D))
			{
				vector = texture2D.GetSize();
			}
			if (vector.X <= 0f || vector.Y <= 0f)
			{
				vector = Vector2.One;
			}
			float num = Math.Max(1f, vector.X);
			float num2 = Math.Max(1f, vector.Y);
			WriteGpuPoseTexel(image, x, i * 5, new Color(reference.Xx, reference.Xy, reference.Yx, reference.Yy));
			WriteGpuPoseTexel(image, x, i * 5 + 1, new Color(reference.Ox, reference.Oy, reference.Alpha, Math.Max(0, val)));
			WriteGpuPoseTexel(image, x, i * 5 + 2, new Color(rect.Position.X / num, rect.Position.Y / num2, rect.Size.X / num, rect.Size.Y / num2));
			WriteGpuPoseTexel(image, x, i * 5 + 3, new Color(GetDeltaValue(definition.NextDeltaXx, i), GetDeltaValue(definition.NextDeltaXy, i), GetDeltaValue(definition.NextDeltaYx, i), GetDeltaValue(definition.NextDeltaYy, i)));
			WriteGpuPoseTexel(image, x, i * 5 + 4, new Color(GetDeltaValue(definition.NextDeltaOx, i), GetDeltaValue(definition.NextDeltaOy, i), GetDeltaValue(definition.NextDeltaAlpha, i), (int)reference.LayerId));
		}
		return image;
	}

	private static bool HasReadableGpuPoseImage(Image image, int sliceCount)
	{
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0 || sliceCount <= 0)
		{
			return false;
		}
		int num = image.GetWidth() * image.GetHeight();
		int num2 = Math.Min(sliceCount, num / 5);
		for (int i = 0; i < num2; i++)
		{
			Color color = ReadGpuPoseTexel(image, i * 5 + 2);
			if (color.B > 0f && color.A > 0f)
			{
				return true;
			}
		}
		return false;
	}

	private static void ApplyGpuPoseTexture(AdobeAnimateRuntimeDefinition definition, Texture2D texture, Vector2I size, ulong signature, bool baked, int baseTexel = 0, bool cacheTexture = true, TextureLayered textureArray = null, Rid textureArrayRid = default(Rid), int textureLayer = 0, bool usesTextureArray = false)
	{
		definition.GpuPoseTexture = texture;
		definition.GpuPoseTextureArray = textureArray;
		definition.UsesGpuPoseTextureArray = usesTextureArray && textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray);
		definition.GpuPoseTextureRid = (definition.UsesGpuPoseTextureArray ? textureArrayRid : (GodotObject.IsInstanceValid(texture) ? texture.GetRid() : default(Rid)));
		definition.GpuPoseTextureSize = size;
		definition.GpuPoseTextureBaseTexel = Math.Max(0, baseTexel);
		definition.GpuPoseTextureLayer = (definition.UsesGpuPoseTextureArray ? Math.Max(0, textureLayer) : 0);
		definition.GpuPoseSignature = signature;
		definition.UsesBakedGpuPoseTexture = baked;
		BuildGpuFrameSlotLookup(definition);
		if (baked)
		{
			ReleaseCpuPoseDataIfBaked(definition);
		}
		if (cacheTexture && definition.GpuPoseTextureRid.IsValid)
		{
			GpuPoseTextureCache[signature] = new GpuPoseTextureCacheEntry
			{
				Texture = (definition.UsesGpuPoseTextureArray ? null : texture),
				TextureArray = (definition.UsesGpuPoseTextureArray ? textureArray : null),
				Rid = definition.GpuPoseTextureRid,
				Size = size,
				TextureLayer = definition.GpuPoseTextureLayer,
				UsesTextureArray = definition.UsesGpuPoseTextureArray
			};
		}
	}

	private static bool TryCreateGpuPoseTextureArray(Image image, Vector2I sourceSize, int texelCount, out Texture2DArray textureArray, out Rid textureArrayRid)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		if (image == null || sourceSize.X <= 0 || sourceSize.Y <= 0 || texelCount <= 0)
		{
			return false;
		}
		if (image.GetFormat() != Image.Format.Rgbah)
		{
			image.Convert(Image.Format.Rgbah);
		}
		int num = Math.Clamp(sourceSize.X, 1, 2048);
		int num2 = Math.Clamp(sourceSize.Y, 1, 2048);
		bool flag = false;
		Image image2;
		if (image.GetWidth() == num && image.GetHeight() == num2)
		{
			image2 = image;
		}
		else
		{
			image2 = Image.CreateEmpty(num, num2, useMipmaps: false, Image.Format.Rgbah);
			flag = true;
			image2.Fill(new Color(0f, 0f, 0f, 0f));
			CopyGpuPoseImage(image, sourceSize, image2, num, 0, Math.Min(texelCount, num * num2));
		}
		try
		{
			Array<Image> images = new Array<Image> { image2 };
			Texture2DArray texture2DArray = new Texture2DArray();
			if (texture2DArray.CreateFromImages(images) != Error.Ok)
			{
				return false;
			}
			textureArray = texture2DArray;
			textureArrayRid = texture2DArray.GetRid();
			return textureArrayRid.IsValid;
		}
		finally
		{
			if (flag)
			{
				image2.Dispose();
			}
		}
	}

	private static bool TryApplyCachedGpuPoseTexture(AdobeAnimateRuntimeDefinition definition, ulong signature, Vector2I size, bool baked)
	{
		if (definition == null || signature == 0L)
		{
			return false;
		}
		if (!GpuPoseTextureCache.TryGetValue(signature, out var value))
		{
			return false;
		}
		if (value == null || value.Size != size || !value.Rid.IsValid)
		{
			GpuPoseTextureCache.Remove(signature);
			return false;
		}
		if (value.UsesTextureArray)
		{
			if (!GodotObject.IsInstanceValid(value.TextureArray))
			{
				GpuPoseTextureCache.Remove(signature);
				return false;
			}
			ApplyGpuPoseTexture(definition, null, value.Size, signature, baked, 0, cacheTexture: false, value.TextureArray, value.Rid, value.TextureLayer, usesTextureArray: true);
			return true;
		}
		if (!GodotObject.IsInstanceValid(value.Texture))
		{
			GpuPoseTextureCache.Remove(signature);
			return false;
		}
		ApplyGpuPoseTexture(definition, value.Texture, value.Size, signature, baked, 0, cacheTexture: false);
		return true;
	}

	private static Vector2I GetGpuPoseTextureSize(int sliceCount)
	{
		int num = Math.Max(1, sliceCount * 5);
		int num2 = Math.Min(2048, num);
		int y = Math.Max(1, (num + num2 - 1) / num2);
		return new Vector2I(num2, y);
	}

	private static ulong ComputeGpuPoseSignature(AdobeAnimateData data, AdobeAnimateRuntimeDefinition definition)
	{
		ulong hash = 1469598103934665603uL;
		HashAddInt(ref hash, 7);
		HashAddInt(ref hash, (definition?.Frames?.Length).GetValueOrDefault());
		if (definition?.Frames != null)
		{
			for (int i = 0; i < definition.Frames.Length; i++)
			{
				HashAddInt(ref hash, definition.Frames[i].Offset);
				HashAddInt(ref hash, definition.Frames[i].Count);
			}
		}
		PackedSliceMetadata[] array = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		float[] array2 = data?.sliceTransforms ?? System.Array.Empty<float>();
		float[] array3 = data?.sliceAlpha ?? System.Array.Empty<float>();
		HashAddInt(ref hash, array.Length);
		for (int j = 0; j < array.Length; j++)
		{
			ref PackedSliceMetadata reference = ref array[j];
			HashAddInt(ref hash, reference.MediaId);
			HashAddInt(ref hash, reference.LayerId);
			int num = j * 6;
			for (int k = 0; k < 6; k++)
			{
				HashAddFloat(ref hash, (num + k < array2.Length) ? array2[num + k] : 0f);
			}
			HashAddFloat(ref hash, (j < array3.Length) ? array3[j] : 1f);
		}
		HashAddRectArray(ref hash, definition?.MediaRects);
		HashAddIntArray(ref hash, definition?.MediaAtlasPages);
		HashAddInt(ref hash, (definition?.UsesAtlasTextureArrayLayout ?? false) ? 1 : 0);
		HashAddFloat(ref hash, definition?.AtlasTextureArraySize.X ?? 0f);
		HashAddFloat(ref hash, definition?.AtlasTextureArraySize.Y ?? 0f);
		int[] array4 = BuildNextSliceTargetsForSignature(definition);
		for (int l = 0; l < 7; l++)
		{
			HashAddInt(ref hash, array.Length);
			for (int m = 0; m < array.Length; m++)
			{
				int num2 = ((m < array4.Length) ? array4[m] : (-1));
				float value = ((num2 >= 0 && num2 != m) ? (ReadPackedPoseComponent(array2, array3, num2, l) - ReadPackedPoseComponent(array2, array3, m, l)) : 0f);
				HashAddFloat(ref hash, value);
			}
		}
		int num3 = definition?.MediaTextureSizes?.Length ?? (definition?.MediaTextures?.Length).GetValueOrDefault();
		HashAddInt(ref hash, num3);
		for (int n = 0; n < num3; n++)
		{
			Vector2 vector = ResolveGpuPoseSignatureMediaSize(definition, n);
			HashAddFloat(ref hash, vector.X);
			HashAddFloat(ref hash, vector.Y);
		}
		return hash;
	}

	private static int[] BuildNextSliceTargetsForSignature(AdobeAnimateRuntimeDefinition definition)
	{
		PackedFrame[] array = definition?.Frames ?? System.Array.Empty<PackedFrame>();
		PackedSliceMetadata[] array2 = definition?.SliceMetadata ?? System.Array.Empty<PackedSliceMetadata>();
		int[] array3 = new int[array2.Length];
		System.Array.Fill(array3, -1);
		for (int i = 0; i < array.Length; i++)
		{
			PackedFrame packedFrame = array[i];
			if (packedFrame.Count <= 0)
			{
				continue;
			}
			int num = Math.Min(i + 1, array.Length - 1);
			PackedFrame frame = array[num];
			int num2 = Math.Min(array2.Length, packedFrame.Offset + packedFrame.Count);
			int num3 = Math.Max(0, packedFrame.Offset);
			int num4 = 0;
			while (num3 < num2)
			{
				if (num == i)
				{
					array3[num3] = num3;
				}
				else
				{
					ref PackedSliceMetadata reference = ref array2[num3];
					int num5 = frame.Offset + num4;
					int absoluteIndex;
					if (num4 < frame.Count && (uint)num5 < (uint)array2.Length && array2[num5].SliceKey == reference.SliceKey)
					{
						array3[num3] = num5;
					}
					else if (TryGetSliceIndexInFrame(definition, num, frame, reference.SliceKey, out absoluteIndex))
					{
						array3[num3] = absoluteIndex;
					}
				}
				num3++;
				num4++;
			}
		}
		return array3;
	}

	private static float ReadPackedPoseComponent(float[] transforms, float[] alpha, int sourceIndex, int component)
	{
		if (component == 6)
		{
			if (sourceIndex < 0 || sourceIndex >= alpha.Length)
			{
				return 1f;
			}
			return alpha[sourceIndex];
		}
		int num = sourceIndex * 6 + component;
		if (num < 0 || num >= transforms.Length)
		{
			return 0f;
		}
		return transforms[num];
	}

	private static Vector2 ResolveGpuPoseSignatureMediaSize(AdobeAnimateRuntimeDefinition definition, int mediaId)
	{
		if (definition?.MediaTextureSizes != null && mediaId >= 0 && mediaId < definition.MediaTextureSizes.Length)
		{
			Vector2 result = definition.MediaTextureSizes[mediaId];
			if (result.X > 0f && result.Y > 0f)
			{
				return result;
			}
		}
		if (definition != null && definition.UsesAtlasTextureArrayLayout && definition.AtlasTextureArraySize.X > 0f && definition.AtlasTextureArraySize.Y > 0f)
		{
			return definition.AtlasTextureArraySize;
		}
		if (definition != null && definition.BaseAtlasSize.X > 0f && definition.BaseAtlasSize.Y > 0f)
		{
			return definition.BaseAtlasSize;
		}
		if (definition?.MediaTextures != null && mediaId >= 0 && mediaId < definition.MediaTextures.Length && GodotObject.IsInstanceValid(definition.MediaTextures[mediaId]))
		{
			return definition.MediaTextures[mediaId].GetSize();
		}
		return Vector2.One;
	}
}
