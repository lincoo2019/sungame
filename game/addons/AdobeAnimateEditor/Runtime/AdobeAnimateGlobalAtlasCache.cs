using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Godot;
using Godot.Collections;

public static class AdobeAnimateGlobalAtlasCache
{
	private sealed class AtlasPage
	{
		public int Index;

		public Image Image;

		public int Width;

		public int Height;

		public int UsedWidth;

		public int UsedHeight;

		public readonly List<Rect2I> FreeRects = new List<Rect2I>();

		public bool TryAllocate(Vector2I contentSize, out Rect2I paddedRect, out Vector2I contentPosition)
		{
			int num = contentSize.X + 2;
			int num2 = contentSize.Y + 2;
			paddedRect = default(Rect2I);
			contentPosition = default(Vector2I);
			if (num > Width || num2 > Height)
			{
				return false;
			}
			int num3 = -1;
			long num4 = 9223372036854775807L;
			int num5 = 2147483647;
			for (int i = 0; i < FreeRects.Count; i++)
			{
				Rect2I rect2I = FreeRects[i];
				if (num <= rect2I.Size.X && num2 <= rect2I.Size.Y)
				{
					int val = rect2I.Size.X - num;
					int val2 = rect2I.Size.Y - num2;
					int num6 = Math.Min(val, val2);
					long num7 = (long)rect2I.Size.X * (long)rect2I.Size.Y - (long)num * (long)num2;
					if (num7 < num4 || (num7 == num4 && num6 < num5))
					{
						num3 = i;
						num4 = num7;
						num5 = num6;
					}
				}
			}
			if (num3 < 0)
			{
				return false;
			}
			paddedRect = new Rect2I(FreeRects[num3].Position, new Vector2I(num, num2));
			contentPosition = paddedRect.Position + new Vector2I(1, 1);
			SplitFreeRects(paddedRect);
			PruneFreeRects();
			UsedWidth = Math.Max(UsedWidth, paddedRect.Position.X + paddedRect.Size.X);
			UsedHeight = Math.Max(UsedHeight, paddedRect.Position.Y + paddedRect.Size.Y);
			return true;
		}

		private void SplitFreeRects(Rect2I usedRect)
		{
			for (int num = FreeRects.Count - 1; num >= 0; num--)
			{
				Rect2I a = FreeRects[num];
				if (Intersects(a, usedRect))
				{
					FreeRects.RemoveAt(num);
					int num2 = a.Position.X + a.Size.X;
					int num3 = a.Position.Y + a.Size.Y;
					int num4 = usedRect.Position.X + usedRect.Size.X;
					int num5 = usedRect.Position.Y + usedRect.Size.Y;
					AddFreeRect(new Rect2I(a.Position.X, a.Position.Y, usedRect.Position.X - a.Position.X, a.Size.Y));
					AddFreeRect(new Rect2I(num4, a.Position.Y, num2 - num4, a.Size.Y));
					AddFreeRect(new Rect2I(a.Position.X, a.Position.Y, a.Size.X, usedRect.Position.Y - a.Position.Y));
					AddFreeRect(new Rect2I(a.Position.X, num5, a.Size.X, num3 - num5));
				}
			}
		}

		private void AddFreeRect(Rect2I rect)
		{
			if (rect.Size.X > 0 && rect.Size.Y > 0)
			{
				FreeRects.Add(rect);
			}
		}

		private void PruneFreeRects()
		{
			for (int num = FreeRects.Count - 1; num >= 0; num--)
			{
				for (int num2 = FreeRects.Count - 1; num2 >= 0; num2--)
				{
					if (num != num2 && Contains(FreeRects[num2], FreeRects[num]))
					{
						FreeRects.RemoveAt(num);
						break;
					}
				}
			}
		}

		private static bool Intersects(Rect2I a, Rect2I b)
		{
			if (a.Position.X < b.Position.X + b.Size.X && a.Position.X + a.Size.X > b.Position.X && a.Position.Y < b.Position.Y + b.Size.Y)
			{
				return a.Position.Y + a.Size.Y > b.Position.Y;
			}
			return false;
		}

		private static bool Contains(Rect2I outer, Rect2I inner)
		{
			if (inner.Position.X >= outer.Position.X && inner.Position.Y >= outer.Position.Y && inner.Position.X + inner.Size.X <= outer.Position.X + outer.Size.X)
			{
				return inner.Position.Y + inner.Size.Y <= outer.Position.Y + outer.Size.Y;
			}
			return false;
		}
	}

	private sealed class ManifestTextureArrayEntry
	{
		public AdobeAnimateGlobalAtlasManifest Manifest;

		public int PageCount;

		public int LayerOffset;

		public string[] PagePaths = System.Array.Empty<string>();
	}

	private sealed class SharedVisualTextureArrayAllocation
	{
		public TextureLayered Texture;

		public Rid Rid;

		public readonly System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, int> LayerOffsets = new System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, int>();
	}

	private enum ExternalTextureAtlasPreference
	{
		Any,
		Replace
	}

	private readonly struct RuntimeExternalTextureKey(ulong instanceId, ExternalTextureAtlasPreference preference) : IEquatable<RuntimeExternalTextureKey>
	{
		public ulong InstanceId { get; } = instanceId;

		public ExternalTextureAtlasPreference Preference { get; } = preference;

		public bool Equals(RuntimeExternalTextureKey other)
		{
			if (InstanceId == other.InstanceId)
			{
				return Preference == other.Preference;
			}
			return false;
		}

		public override bool Equals(object obj)
		{
			if (obj is RuntimeExternalTextureKey other)
			{
				return Equals(other);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(InstanceId, (int)Preference);
		}
	}

	private readonly struct RuntimeExternalTextureEntry(Rid sourceRid, in AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		public Rid SourceRid { get; } = sourceRid;

		public AdobeAnimateExternalTextureAtlasAllocation Allocation { get; } = allocation;
	}

	private sealed class ExtResourceReference
	{
		public string Type = string.Empty;

		public string Path = string.Empty;
	}

	private const int MaxAtlasPageSize = 2048;

	private static readonly Vector2I AtlasArrayLayerSize = new Vector2I(2048, 2048);

	private const int BorderPadding = 1;

	private const int AtlasTextureImportCompressionMode = 2;

	private const int MaxVisualTextureArrayGridDimension = 8;

	private const string GlobalAtlasPagePrefix = "AdobeAnimateGlobalAtlasPage_";

	private const string VisualTextureArrayFileName = "AdobeAnimateVisualTextureArray.png";

	private const string PoseTextureArrayFileName = "AdobeAnimateGpuPoseTextureArray.exr";

	public const string VisualTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png";

	public const string PoseTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr";

	private const ulong SourceSignatureHashOffset = 1469598103934665603uL;

	private const ulong SourceSignatureHashPrime = 1099511628211uL;

	public const string GeneratedAtlasDir = "res://addons/AdobeAnimateEditor/GeneratedAtlas";

	public const string ManifestPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres";

	public const string BootstrapGeneratedAtlasDir = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap";

	public const string BootstrapManifestPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres";

	public const string BootstrapVisualTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png";

	public const string BootstrapPoseTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapPoseTextureArray.exr";

	public const string BootstrapAtlasProfilePath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasProfile.tres";

	private const string BootstrapAtlasPagePrefix = "AdobeAnimateBootstrapAtlasPage_";

	private const string BootstrapPoseTextureDir = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AnimationData";

	private static readonly string[] BootstrapAnimeDataPaths = new string[2] { "res://Asset/Anime/Effect/LoadBar/LoadBarSprout.tres", "res://Asset/Anime/Effect/LoadBar/LoadBarZombieHead.tres" };

	private const string ProjectAtlasDefaultSearchRoot = "res://Asset/Anime";

	private const string PrefabParticlesAtlasSearchRoot = "res://Prefab/Particles";

	private static readonly string[] ProjectileAtlasSearchRoots = new string[2] { "res://Asset/Config/Projectile", "res://Registry/Projectile/Config" };

	private static readonly string[] EffectAtlasSearchRoots = new string[5] { "res://Asset/Anime/Effect", "res://Asset/Anime/Mower", "res://Asset/Anime/Particles", "res://Asset/Anime/Splat", "res://Prefab/Particles" };

	private static readonly System.Collections.Generic.Dictionary<string, AdobeAnimateAtlasAllocation> Allocations = new System.Collections.Generic.Dictionary<string, AdobeAnimateAtlasAllocation>();

	private static readonly System.Collections.Generic.Dictionary<string, AdobeAnimateExternalTextureAtlasAllocation> ExternalTextureAllocations = new System.Collections.Generic.Dictionary<string, AdobeAnimateExternalTextureAtlasAllocation>();

	private static readonly System.Collections.Generic.Dictionary<RuntimeExternalTextureKey, RuntimeExternalTextureEntry> RuntimeExternalTextureAllocations = new System.Collections.Generic.Dictionary<RuntimeExternalTextureKey, RuntimeExternalTextureEntry>();

	private static readonly System.Collections.Generic.Dictionary<string, AdobeAnimatePoseAtlasAllocation> PoseAtlasAllocations = new System.Collections.Generic.Dictionary<string, AdobeAnimatePoseAtlasAllocation>();

	private static readonly System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, (TextureLayered Texture, Rid Rid, Vector2I Size)> PoseTextureArrayAllocations = new System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, (TextureLayered, Rid, Vector2I)>();

	private static readonly System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, SharedVisualTextureArrayAllocation> ProfileVisualTextureArrayAllocations = new System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, SharedVisualTextureArrayAllocation>();

	private static readonly System.Collections.Generic.Dictionary<string, bool> PoseAtlasSourceReadability = new System.Collections.Generic.Dictionary<string, bool>(StringComparer.Ordinal);

	private static SharedVisualTextureArrayAllocation SharedVisualTextureArray;

	private static readonly object CacheLock = new object();

	private static readonly HashSet<string> MissingAtlasEntryWarnings = new HashSet<string>();

	private static int CacheVersionValue;

	private static AdobeAnimateGlobalAtlasManifest Manifest;

	private static bool ManifestLoadAttempted;

	private static bool MissingAtlasManifestWarningPrinted;

	private static string[] ManifestAtlasPagePathFallback = System.Array.Empty<string>();

	private static bool ManifestAtlasPagePathFallbackAttempted;

	private static string[] BootstrapManifestAtlasPagePathFallback = System.Array.Empty<string>();

	private static bool BootstrapManifestAtlasPagePathFallbackAttempted;

	public static int CacheVersion => Volatile.Read(in CacheVersionValue);

	private static void PrepareGeneratedAtlasDirectory(string generatedAtlasDir, string pagePrefix)
	{
		string path = ProjectSettings.GlobalizePath(generatedAtlasDir);
		Directory.CreateDirectory(path);
		foreach (string item in Directory.EnumerateFiles(path, pagePrefix + "*.*"))
		{
			File.Delete(item);
		}
	}

	private static string GetPagePath(string generatedAtlasDir, string pagePrefix, int pageIndex)
	{
		return $"{generatedAtlasDir}/{pagePrefix}{pageIndex}.png";
	}

	public static string[] GetGeneratedVisualAtlasImportPaths()
	{
		string text = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png");
		if (!string.IsNullOrEmpty(text) && File.Exists(text))
		{
			return new string[1] { "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png" };
		}
		return GetGeneratedVisualAtlasPagePaths();
	}

	public static string[] GetGeneratedAtlasImportPaths()
	{
		List<string> list = new List<string>(4);
		AddGeneratedImportPath(list, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png");
		AddGeneratedImportPath(list, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr");
		AddGeneratedImportPath(list, "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png");
		AddGeneratedImportPath(list, "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapPoseTextureArray.exr");
		return list.ToArray();
	}

	private static void AddGeneratedImportPath(List<string> paths, string resourcePath)
	{
		if (paths != null && !string.IsNullOrWhiteSpace(resourcePath))
		{
			string text = ProjectSettings.GlobalizePath(resourcePath);
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				paths.Add(resourcePath);
			}
		}
	}

	private static string GetManifestResourcePath(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest == null)
		{
			return string.Empty;
		}
		if (manifest == Manifest)
		{
			return "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres";
		}
		return string.Empty;
	}

	private static void DeleteIntermediateVisualAtlasPages(List<ManifestTextureArrayEntry> entries)
	{
		if (entries == null)
		{
			return;
		}
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		for (int i = 0; i < entries.Count; i++)
		{
			ManifestTextureArrayEntry manifestTextureArrayEntry = entries[i];
			int pageCount = manifestTextureArrayEntry.PageCount;
			string[] pagePaths = manifestTextureArrayEntry.PagePaths;
			int num = Math.Min(pageCount, (pagePaths != null) ? pagePaths.Length : 0);
			for (int j = 0; j < num; j++)
			{
				string text = manifestTextureArrayEntry.PagePaths[j];
				if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
				{
					continue;
				}
				DeleteResourceFile(text);
				DeleteResourceFile(text + ".import");
				foreach (string generatedImportedTexturePath in GetGeneratedImportedTexturePaths(text))
				{
					DeleteResourceFile(generatedImportedTexturePath);
				}
			}
		}
	}

	private static void DeleteResourceFile(string resourcePath)
	{
		string text = ProjectSettings.GlobalizePath(resourcePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return;
		}
		try
		{
			File.Delete(text);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Unable to delete generated AdobeAnimate atlas file " + resourcePath + ": " + ex.Message);
		}
	}

	private static void WriteTextureImportPreset(string pagePath)
	{
		string text = ProjectSettings.GlobalizePath(pagePath + ".import");
		string value = $"res://.godot/imported/{pagePath.GetFile()}-{GetImportPathHash(pagePath)}.ctex";
		StringBuilder stringBuilder = new StringBuilder(1024);
		stringBuilder.AppendLine("[remap]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("importer=\"texture\"");
		stringBuilder.AppendLine("type=\"CompressedTexture2D\"");
		AppendExistingImportUid(stringBuilder, text);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("path=\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine("metadata={");
		stringBuilder.AppendLine("\"vram_texture\": true");
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[deps]");
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("source_file=\"");
		handler.AppendFormatted(pagePath);
		handler.AppendLiteral("\"");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("dest_files=[\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"]");
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[params]");
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("compress/mode=");
		handler.AppendFormatted(2);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder.AppendLine("compress/high_quality=false");
		stringBuilder.AppendLine("compress/lossy_quality=0.7");
		stringBuilder.AppendLine("compress/uastc_level=0");
		stringBuilder.AppendLine("compress/rdo_quality_loss=0.0");
		stringBuilder.AppendLine("compress/hdr_compression=1");
		stringBuilder.AppendLine("compress/normal_map=0");
		stringBuilder.AppendLine("compress/channel_pack=0");
		stringBuilder.AppendLine("mipmaps/generate=false");
		stringBuilder.AppendLine("mipmaps/limit=-1");
		stringBuilder.AppendLine("roughness/mode=0");
		stringBuilder.AppendLine("roughness/src_normal=\"\"");
		stringBuilder.AppendLine("process/channel_remap/red=0");
		stringBuilder.AppendLine("process/channel_remap/green=1");
		stringBuilder.AppendLine("process/channel_remap/blue=2");
		stringBuilder.AppendLine("process/channel_remap/alpha=3");
		stringBuilder.AppendLine("process/fix_alpha_border=true");
		stringBuilder.AppendLine("process/premult_alpha=false");
		stringBuilder.AppendLine("process/normal_map_invert_y=false");
		stringBuilder.AppendLine("process/hdr_as_srgb=false");
		stringBuilder.AppendLine("process/hdr_clamp_exposure=false");
		stringBuilder.AppendLine("process/size_limit=0");
		stringBuilder.AppendLine("detect_3d/compress_to=1");
		File.WriteAllText(text, stringBuilder.ToString().Replace("\r\n", "\n"));
	}

	private static void WriteTextureArrayImportPreset(string sourcePath, int horizontalSlices, int verticalSlices)
	{
		string text = ProjectSettings.GlobalizePath(sourcePath + ".import");
		string value = $"res://.godot/imported/{sourcePath.GetFile()}-{GetImportPathHash(sourcePath)}.ctexarray";
		StringBuilder stringBuilder = new StringBuilder(1024);
		stringBuilder.AppendLine("[remap]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("importer=\"2d_array_texture\"");
		stringBuilder.AppendLine("type=\"CompressedTexture2DArray\"");
		AppendExistingImportUid(stringBuilder, text);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("path=\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine("metadata={");
		stringBuilder.AppendLine("\"vram_texture\": true");
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[deps]");
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("source_file=\"");
		handler.AppendFormatted(sourcePath);
		handler.AppendLiteral("\"");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("dest_files=[\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"]");
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[params]");
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("compress/mode=");
		handler.AppendFormatted(2);
		stringBuilder6.AppendLine(ref handler);
		stringBuilder.AppendLine("compress/high_quality=false");
		stringBuilder.AppendLine("compress/lossy_quality=0.7");
		stringBuilder.AppendLine("compress/uastc_level=0");
		stringBuilder.AppendLine("compress/rdo_quality_loss=0.0");
		stringBuilder.AppendLine("compress/hdr_compression=1");
		stringBuilder.AppendLine("compress/channel_pack=0");
		stringBuilder.AppendLine("mipmaps/generate=false");
		stringBuilder.AppendLine("mipmaps/limit=-1");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("slices/horizontal=");
		handler.AppendFormatted(Math.Max(1, horizontalSlices));
		stringBuilder7.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder8 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("slices/vertical=");
		handler.AppendFormatted(Math.Max(1, verticalSlices));
		stringBuilder8.AppendLine(ref handler);
		File.WriteAllText(text, stringBuilder.ToString().Replace("\r\n", "\n"));
	}

	private static void WritePoseTextureArrayImportPreset(string sourcePath, int horizontalSlices, int verticalSlices)
	{
		string text = ProjectSettings.GlobalizePath(sourcePath + ".import");
		string value = $"res://.godot/imported/{sourcePath.GetFile()}-{GetImportPathHash(sourcePath)}.ctexarray";
		StringBuilder stringBuilder = new StringBuilder(1024);
		stringBuilder.AppendLine("[remap]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("importer=\"2d_array_texture\"");
		stringBuilder.AppendLine("type=\"CompressedTexture2DArray\"");
		AppendExistingImportUid(stringBuilder, text);
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(7, 1, stringBuilder2);
		handler.AppendLiteral("path=\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"");
		stringBuilder3.AppendLine(ref handler);
		stringBuilder.AppendLine("metadata={");
		stringBuilder.AppendLine("\"vram_texture\": false");
		stringBuilder.AppendLine("}");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[deps]");
		stringBuilder.AppendLine();
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder4 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(14, 1, stringBuilder2);
		handler.AppendLiteral("source_file=\"");
		handler.AppendFormatted(sourcePath);
		handler.AppendLiteral("\"");
		stringBuilder4.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder5 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(15, 1, stringBuilder2);
		handler.AppendLiteral("dest_files=[\"");
		handler.AppendFormatted(value);
		handler.AppendLiteral("\"]");
		stringBuilder5.AppendLine(ref handler);
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("[params]");
		stringBuilder.AppendLine();
		stringBuilder.AppendLine("compress/mode=0");
		stringBuilder.AppendLine("compress/high_quality=false");
		stringBuilder.AppendLine("compress/lossy_quality=0.7");
		stringBuilder.AppendLine("compress/uastc_level=0");
		stringBuilder.AppendLine("compress/rdo_quality_loss=0.0");
		stringBuilder.AppendLine("compress/hdr_compression=1");
		stringBuilder.AppendLine("compress/channel_pack=0");
		stringBuilder.AppendLine("mipmaps/generate=false");
		stringBuilder.AppendLine("mipmaps/limit=-1");
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder6 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(18, 1, stringBuilder2);
		handler.AppendLiteral("slices/horizontal=");
		handler.AppendFormatted(Math.Max(1, horizontalSlices));
		stringBuilder6.AppendLine(ref handler);
		stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder7 = stringBuilder2;
		handler = new StringBuilder.AppendInterpolatedStringHandler(16, 1, stringBuilder2);
		handler.AppendLiteral("slices/vertical=");
		handler.AppendFormatted(Math.Max(1, verticalSlices));
		stringBuilder7.AppendLine(ref handler);
		File.WriteAllText(text, stringBuilder.ToString().Replace("\r\n", "\n"));
	}

	private static void AppendExistingImportUid(StringBuilder builder, string importPath)
	{
		if (builder == null || string.IsNullOrEmpty(importPath) || !File.Exists(importPath))
		{
			return;
		}
		try
		{
			foreach (string item in File.ReadLines(importPath))
			{
				string text = item.Trim();
				if (text.StartsWith("uid=\"uid://", StringComparison.Ordinal) && text.EndsWith('"'))
				{
					builder.AppendLine(text);
					break;
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("Unable to preserve AdobeAnimate import UID from " + importPath + ": " + ex.Message);
		}
	}

	private static string GetImportPathHash(string resourcePath)
	{
		byte[] array = MD5.HashData(Encoding.UTF8.GetBytes(resourcePath));
		StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
		for (int i = 0; i < array.Length; i++)
		{
			stringBuilder.Append(array[i].ToString("x2"));
		}
		return stringBuilder.ToString();
	}

	public static void Invalidate(AdobeAnimateData data)
	{
		if (data == null)
		{
			return;
		}
		lock (CacheLock)
		{
			string value = data.GetAtlasSourceKey() + "|";
			List<string> list = new List<string>();
			foreach (string key in Allocations.Keys)
			{
				if (key.StartsWith(value, StringComparison.Ordinal))
				{
					list.Add(key);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				Allocations.Remove(list[i]);
			}
			BumpCacheVersion();
		}
	}

	public static void ReleaseProfile(AdobeAnimateAtlasProfile atlasProfile)
	{
		if (atlasProfile == null)
		{
			return;
		}
		lock (CacheLock)
		{
			string stableCacheKey = atlasProfile.GetStableCacheKey();
			string allocationPrefix = "profile=" + stableCacheKey + "|";
			string[] array = Allocations.Keys.Where((string text) => text.StartsWith(allocationPrefix, StringComparison.Ordinal)).ToArray();
			foreach (string key in array)
			{
				Allocations.Remove(key);
			}
			string posePrefix = stableCacheKey + "|";
			array = PoseAtlasAllocations.Keys.Where((string text) => text.StartsWith(posePrefix, StringComparison.Ordinal)).ToArray();
			foreach (string key2 in array)
			{
				PoseAtlasAllocations.Remove(key2);
			}
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = null;
			try
			{
				adobeAnimateGlobalAtlasManifest = atlasProfile.LoadManifest();
			}
			catch
			{
			}
			if (adobeAnimateGlobalAtlasManifest != null)
			{
				ProfileVisualTextureArrayAllocations.Remove(adobeAnimateGlobalAtlasManifest);
				PoseTextureArrayAllocations.Remove(adobeAnimateGlobalAtlasManifest);
				foreach (string item in adobeAnimateGlobalAtlasManifest.PoseAtlasPagePaths ?? new Array<string>())
				{
					string readabilityPrefix = item + "|";
					array = PoseAtlasSourceReadability.Keys.Where((string text) => text.StartsWith(readabilityPrefix, StringComparison.Ordinal)).ToArray();
					foreach (string key3 in array)
					{
						PoseAtlasSourceReadability.Remove(key3);
					}
				}
			}
			BumpCacheVersion();
		}
	}

	public static void Clear()
	{
		lock (CacheLock)
		{
			Allocations.Clear();
			ExternalTextureAllocations.Clear();
			RuntimeExternalTextureAllocations.Clear();
			PoseAtlasAllocations.Clear();
			SharedVisualTextureArray = null;
			ProfileVisualTextureArrayAllocations.Clear();
			PoseTextureArrayAllocations.Clear();
			PoseAtlasSourceReadability.Clear();
			MissingAtlasEntryWarnings.Clear();
			Manifest = null;
			ManifestLoadAttempted = false;
			ManifestAtlasPagePathFallback = System.Array.Empty<string>();
			ManifestAtlasPagePathFallbackAttempted = false;
			BootstrapManifestAtlasPagePathFallback = System.Array.Empty<string>();
			BootstrapManifestAtlasPagePathFallbackAttempted = false;
			MissingAtlasManifestWarningPrinted = false;
			BumpCacheVersion();
		}
	}

	private static void BumpCacheVersion()
	{
		Interlocked.Increment(ref CacheVersionValue);
	}

	private static bool TryGetManifestAllocation(string sourceKey, int expectedMediaCount, ulong expectedSignature, out AdobeAnimateAtlasAllocation allocation)
	{
		return TryGetManifestAllocation(GetManifest(), sourceKey, expectedMediaCount, expectedSignature, out allocation);
	}

	private static bool TryGetManifestAllocation(AdobeAnimateGlobalAtlasManifest manifest, string sourceKey, int expectedMediaCount, ulong expectedSignature, out AdobeAnimateAtlasAllocation allocation, bool isolatedTextureArray = false)
	{
		allocation = null;
		int manifestAtlasPageCount = GetManifestAtlasPageCount(manifest);
		if (manifest == null || manifestAtlasPageCount == 0)
		{
			return false;
		}
		for (int i = 0; i < manifest.SourceKeys.Count; i++)
		{
			if (!(manifest.SourceKeys[i] != sourceKey))
			{
				int num = manifest.SourceStarts[i];
				int num2 = manifest.SourceCounts[i];
				if (num2 <= 0 || num < 0 || num + num2 > manifest.MediaRects.Count)
				{
					return false;
				}
				if (expectedMediaCount > 0 && num2 < expectedMediaCount)
				{
					return false;
				}
				if (!SourceSignatureMatches(manifest, i, expectedSignature))
				{
					return false;
				}
				int[] array = new int[num2];
				Rect2[] array2 = new Rect2[num2];
				for (int j = 0; j < num2; j++)
				{
					int index = num + j;
					array[j] = manifest.MediaAtlasPages[index];
					array2[j] = manifest.MediaRects[index];
				}
				if (!(isolatedTextureArray ? TryGetProfileManifestTextureArray(manifest, manifestAtlasPageCount, out var textureArray, out var textureArrayRid, out var layerOffset) : TryGetManifestTextureArray(manifest, manifestAtlasPageCount, out textureArray, out textureArrayRid, out layerOffset)))
				{
					return false;
				}
				if (!textureArrayRid.IsValid || !GodotObject.IsInstanceValid(textureArray))
				{
					return false;
				}
				int num3 = ((num2 > 0) ? array[0] : 0);
				if (num3 < 0 || num3 >= manifestAtlasPageCount)
				{
					num3 = 0;
				}
				allocation = new AdobeAnimateAtlasAllocation
				{
					Texture = null,
					TextureRid = textureArrayRid,
					TextureArray = textureArray,
					TextureArrayRid = textureArrayRid,
					TextureArraySize = GetManifestAtlasLayerSize(manifest),
					TextureArrayLayerOffset = layerOffset,
					UsesTextureArrayLayout = true,
					Textures = System.Array.Empty<Texture2D>(),
					TextureRids = System.Array.Empty<Rid>(),
					MediaAtlasPages = OffsetAtlasPages(array, layerOffset),
					MediaRects = array2,
					PageIndex = num3 + layerOffset
				};
				return true;
			}
		}
		return false;
	}

	public static bool TryGetManifestMetadataAllocation(string sourceKey, int expectedMediaCount, out AdobeAnimateAtlasAllocation allocation)
	{
		return TryGetManifestMetadataAllocation(GetManifest(), sourceKey, expectedMediaCount, out allocation);
	}

	public static bool TryGetManifestMetadataAllocation(AdobeAnimateGlobalAtlasManifest manifest, string sourceKey, int expectedMediaCount, out AdobeAnimateAtlasAllocation allocation)
	{
		allocation = null;
		int manifestAtlasPageCount = GetManifestAtlasPageCount(manifest);
		if (manifest == null || manifestAtlasPageCount == 0)
		{
			return false;
		}
		for (int i = 0; i < manifest.SourceKeys.Count; i++)
		{
			if (!(manifest.SourceKeys[i] != sourceKey))
			{
				int num = manifest.SourceStarts[i];
				int num2 = manifest.SourceCounts[i];
				if (num2 <= 0 || num < 0 || num + num2 > manifest.MediaRects.Count)
				{
					return false;
				}
				if (expectedMediaCount > 0 && num2 < expectedMediaCount)
				{
					return false;
				}
				int[] array = new int[num2];
				Rect2[] array2 = new Rect2[num2];
				for (int j = 0; j < num2; j++)
				{
					int index = num + j;
					array[j] = manifest.MediaAtlasPages[index];
					array2[j] = manifest.MediaRects[index];
				}
				int num3 = ((num2 > 0) ? array[0] : 0);
				if (num3 < 0 || num3 >= manifestAtlasPageCount)
				{
					num3 = 0;
				}
				int layerOffset;
				int num4 = (TryGetManifestTextureArrayLayerOffset(manifest, manifestAtlasPageCount, out layerOffset) ? layerOffset : 0);
				allocation = new AdobeAnimateAtlasAllocation
				{
					Textures = System.Array.Empty<Texture2D>(),
					TextureRids = System.Array.Empty<Rid>(),
					TextureArraySize = GetManifestAtlasLayerSize(manifest),
					TextureArrayLayerOffset = num4,
					UsesTextureArrayLayout = true,
					MediaAtlasPages = OffsetAtlasPages(array, num4),
					MediaRects = array2,
					PageIndex = num3 + num4
				};
				if (allocation.TextureArraySize.X > 0f)
				{
					return allocation.TextureArraySize.Y > 0f;
				}
				return false;
			}
		}
		return false;
	}

	private static bool TryGetManifestExternalTextureAllocation(string texturePath, ExternalTextureAtlasPreference preference, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		return TryGetManifestExternalTextureAllocation(GetManifest(), texturePath, out allocation);
	}

	private static bool TryGetManifestExternalTextureAllocation(AdobeAnimateGlobalAtlasManifest manifest, string texturePath, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		int manifestAtlasPageCount = GetManifestAtlasPageCount(manifest);
		if (manifest == null || manifestAtlasPageCount == 0 || manifest.ExternalTexturePaths == null)
		{
			return false;
		}
		string legacyAtlasSourceTexturePath = GetLegacyAtlasSourceTexturePath(texturePath);
		string categorizedAtlasSourceTexturePath = GetCategorizedAtlasSourceTexturePath(texturePath);
		int num = Math.Min(manifest.ExternalTexturePaths.Count, Math.Min(manifest.ExternalTextureAtlasPages?.Count ?? 0, manifest.ExternalTextureRects?.Count ?? 0));
		for (int i = 0; i < num; i++)
		{
			string a = NormalizeProjectPath(manifest.ExternalTexturePaths[i]);
			if (string.Equals(a, texturePath, StringComparison.OrdinalIgnoreCase) || string.Equals(a, legacyAtlasSourceTexturePath, StringComparison.OrdinalIgnoreCase) || string.Equals(a, categorizedAtlasSourceTexturePath, StringComparison.OrdinalIgnoreCase))
			{
				int num2 = manifest.ExternalTextureAtlasPages[i];
				if (num2 < 0 || num2 >= manifestAtlasPageCount)
				{
					return false;
				}
				if (TryGetManifestTextureArray(manifest, manifestAtlasPageCount, out var textureArray, out var textureArrayRid, out var layerOffset) && textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray))
				{
					allocation = new AdobeAnimateExternalTextureAtlasAllocation(null, textureArrayRid, num2 + layerOffset, manifest.ExternalTextureRects[i], textureArray, textureArrayRid, new Vector2(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y), layerOffset, usesTextureArray: true);
					return true;
				}
				return false;
			}
		}
		return false;
	}

	private static string GetLegacyAtlasSourceTexturePath(string texturePath)
	{
		if (string.IsNullOrEmpty(texturePath) || !texturePath.StartsWith("res://Asset/AtlasSource/", StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}
		string text = texturePath.Substring("res://Asset/AtlasSource/".Length);
		int num = text.IndexOf('/');
		if (num < 0 || num + 1 >= text.Length)
		{
			return string.Empty;
		}
		return "res://Asset/" + text.Substring(num + 1);
	}

	private static string GetCategorizedAtlasSourceTexturePath(string texturePath)
	{
		if (string.IsNullOrEmpty(texturePath) || !texturePath.StartsWith("res://Asset/", StringComparison.OrdinalIgnoreCase) || texturePath.StartsWith("res://Asset/AtlasSource/", StringComparison.OrdinalIgnoreCase))
		{
			return string.Empty;
		}
		string text = (texturePath.Contains("/DamagePoint/", StringComparison.OrdinalIgnoreCase) ? "DamagePoint" : (texturePath.Contains("/Armor/", StringComparison.OrdinalIgnoreCase) ? "Armor" : (texturePath.Contains("/Custom/", StringComparison.OrdinalIgnoreCase) ? "Custom" : string.Empty)));
		if (string.IsNullOrEmpty(text))
		{
			return string.Empty;
		}
		return "res://Asset/AtlasSource/" + text + "/" + texturePath.Substring("res://Asset/".Length);
	}

	private static bool TryGetManifestPoseAtlasAllocation(string sourceKey, ulong expectedSignature, int expectedTexelCount, out AdobeAnimatePoseAtlasAllocation allocation)
	{
		return TryGetManifestPoseAtlasAllocation(GetManifest(), sourceKey, expectedSignature, expectedTexelCount, out allocation);
	}

	private static bool TryGetManifestPoseAtlasAllocation(AdobeAnimateGlobalAtlasManifest manifest, string sourceKey, ulong expectedSignature, int expectedTexelCount, out AdobeAnimatePoseAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimatePoseAtlasAllocation);
		if (manifest == null || manifest.PoseAtlasPagePaths == null || manifest.PoseAtlasPagePaths.Count == 0 || manifest.SourcePoseAtlasPages == null || manifest.SourcePoseTexelStarts == null || manifest.SourcePoseTexelCounts == null || manifest.SourcePoseSignatures == null)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.invalidManifest");
			return false;
		}
		for (int i = 0; i < manifest.SourceKeys.Count; i++)
		{
			if (manifest.SourceKeys[i] != sourceKey)
			{
				continue;
			}
			if (i >= manifest.SourcePoseAtlasPages.Count || i >= manifest.SourcePoseTexelStarts.Count || i >= manifest.SourcePoseTexelCounts.Count || i >= manifest.SourcePoseSignatures.Count)
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.missingArrays");
				return false;
			}
			int num = manifest.SourcePoseAtlasPages[i];
			int num2 = manifest.SourcePoseTexelStarts[i];
			int num3 = manifest.SourcePoseTexelCounts[i];
			if (num < 0 || num >= manifest.PoseAtlasPagePaths.Count)
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.posePage");
				return false;
			}
			if (num2 < 0 || num3 < expectedTexelCount)
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.texelCount", expectedTexelCount);
				return false;
			}
			if (!TryParseSignature(manifest.SourcePoseSignatures[i], out var signature) || signature != expectedSignature)
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.signature", expectedTexelCount);
				return false;
			}
			if (TryGetManifestPoseTextureArray(manifest, out var textureArray, out var textureArrayRid, out var textureSize))
			{
				if (textureSize.X <= 0 || textureSize.Y <= 0 || num2 + expectedTexelCount > textureSize.X * textureSize.Y)
				{
					TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.arraySize", expectedTexelCount);
					return false;
				}
				allocation = new AdobeAnimatePoseAtlasAllocation(null, textureArrayRid, num, num2, num3, textureSize, signature, textureArray, textureArrayRid, usesTextureArray: true);
				return allocation.TextureArrayRid.IsValid;
			}
			if (!HasReadablePoseAtlasSource(manifest, num, num2, expectedTexelCount))
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.unreadable", expectedTexelCount);
				return false;
			}
			Texture2D texture2D = LoadManifestPoseAtlasPage(manifest, num);
			if (!GodotObject.IsInstanceValid(texture2D))
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.loadPage", expectedTexelCount);
				return false;
			}
			Vector2 size = texture2D.GetSize();
			Vector2I textureSize2 = new Vector2I(Mathf.RoundToInt(size.X), Mathf.RoundToInt(size.Y));
			if (textureSize2.X <= 0 || textureSize2.Y <= 0 || num2 + expectedTexelCount > textureSize2.X * textureSize2.Y)
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.pageSize", expectedTexelCount);
				return false;
			}
			allocation = new AdobeAnimatePoseAtlasAllocation(texture2D, texture2D.GetRid(), num, num2, num3, textureSize2, signature);
			return allocation.TextureRid.IsValid;
		}
		TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.manifestMiss.sourceMissing");
		return false;
	}

	private static bool HasReadablePoseAtlasSource(AdobeAnimateGlobalAtlasManifest manifest, int posePage, int texelStart, int expectedTexelCount)
	{
		if (manifest?.PoseAtlasPagePaths == null || posePage < 0 || posePage >= manifest.PoseAtlasPagePaths.Count || texelStart < 0 || expectedTexelCount <= 0)
		{
			return false;
		}
		string text = manifest.PoseAtlasPagePaths[posePage];
		string key = $"{text}|{posePage}|{texelStart}|{expectedTexelCount}";
		if (PoseAtlasSourceReadability.TryGetValue(key, out var value))
		{
			return value;
		}
		Image layerImage = null;
		bool flag;
		try
		{
			flag = TryLoadPoseArrayLayerImage(text, out layerImage) && HasReadablePosePixels(layerImage, texelStart, expectedTexelCount);
		}
		finally
		{
			layerImage?.Dispose();
		}
		PoseAtlasSourceReadability[key] = flag;
		return flag;
	}

	private static bool HasReadablePosePixels(Image image, int texelStart, int texelCount)
	{
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return false;
		}
		int num = image.GetWidth() * image.GetHeight();
		if (texelStart < 0 || texelCount < 5 || texelStart + texelCount > num)
		{
			return false;
		}
		int num2 = texelCount / 5;
		for (int i = 0; i < num2; i++)
		{
			int num3 = texelStart + i * 5 + 2;
			if (num3 >= texelStart + texelCount)
			{
				break;
			}
			Color color = ReadPosePixel(image, num3);
			if (color.B > 0f && color.A > 0f)
			{
				return true;
			}
		}
		return false;
	}

	private static Color ReadPosePixel(Image image, int texelIndex)
	{
		int width = image.GetWidth();
		return image.GetPixel(texelIndex % width, texelIndex / width);
	}

	private static AdobeAnimateGlobalAtlasManifest GetManifest()
	{
		if (ManifestLoadAttempted)
		{
			return Manifest;
		}
		ManifestLoadAttempted = true;
		string path = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres");
		if (!Godot.FileAccess.FileExists("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres") && !File.Exists(path))
		{
			return null;
		}
		SanitizeLegacyManifestTexturePages();
		Manifest = TryReadManifestText("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", "res://addons/AdobeAnimateEditor/GeneratedAtlas", "AdobeAnimateGlobalAtlasPage_", ref ManifestAtlasPagePathFallback, ref ManifestAtlasPagePathFallbackAttempted);
		return Manifest;
	}

	private static AdobeAnimateGlobalAtlasManifest ReadBootstrapManifest()
	{
		string path = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres");
		if (!Godot.FileAccess.FileExists("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres") && !File.Exists(path))
		{
			return null;
		}
		return TryReadManifestText("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres", "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap", "AdobeAnimateBootstrapAtlasPage_", ref BootstrapManifestAtlasPagePathFallback, ref BootstrapManifestAtlasPagePathFallbackAttempted);
	}

	private static int GetManifestAtlasPageCount(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest == null)
		{
			return 0;
		}
		if (manifest.AtlasTextureArrayLayerCount > 0)
		{
			return manifest.AtlasTextureArrayLayerCount;
		}
		int num = -1;
		if (manifest.MediaAtlasPages != null)
		{
			for (int i = 0; i < manifest.MediaAtlasPages.Count; i++)
			{
				num = Math.Max(num, manifest.MediaAtlasPages[i]);
			}
		}
		if (manifest.ExternalTextureAtlasPages != null)
		{
			for (int j = 0; j < manifest.ExternalTextureAtlasPages.Count; j++)
			{
				num = Math.Max(num, manifest.ExternalTextureAtlasPages[j]);
			}
		}
		if (num >= 0)
		{
			return num + 1;
		}
		return GetGeneratedAtlasPagePaths().Length;
	}

	public static Texture2D LoadGeneratedTextureWithImageFallback(string pagePath, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Reuse)
	{
		if (string.IsNullOrEmpty(pagePath))
		{
			return null;
		}
		if (ShouldLoadGeneratedImageFallbackFirst(pagePath))
		{
			Texture2D texture2D = LoadGeneratedImageTextureFallback(pagePath);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				return texture2D;
			}
		}
		Texture2D texture2D2 = ResourceLoader.Load<Texture2D>(pagePath, "", cacheMode);
		if (GodotObject.IsInstanceValid(texture2D2))
		{
			return texture2D2;
		}
		return LoadGeneratedImageTextureFallback(pagePath);
	}

	private static Texture2D LoadManifestPoseAtlasPage(AdobeAnimateGlobalAtlasManifest manifest, int index)
	{
		if (manifest?.PoseAtlasPagePaths != null && index >= 0 && index < manifest.PoseAtlasPagePaths.Count)
		{
			string text = manifest.PoseAtlasPagePaths[index];
			if (!string.IsNullOrEmpty(text))
			{
				return LoadGeneratedTextureWithImageFallback(text, ResourceLoader.CacheMode.Ignore);
			}
		}
		return null;
	}

	private static Texture2D LoadGeneratedImageTextureFallback(string pagePath)
	{
		string text = ProjectSettings.GlobalizePath(pagePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return null;
		}
		Image image = Image.LoadFromFile(text);
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return null;
		}
		return ImageTexture.CreateFromImage(image);
	}

	private static bool ShouldLoadGeneratedImageFallbackFirst(string pagePath)
	{
		if (IsRuntimeRawImagePath(pagePath))
		{
			return true;
		}
		if (!IsGeneratedAtlasResourcePath(pagePath))
		{
			return false;
		}
		string text = ProjectSettings.GlobalizePath(pagePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return false;
		}
		if (!TryGetGeneratedImportWriteTimeUtc(pagePath, out var writeTimeUtc))
		{
			return true;
		}
		try
		{
			if (File.GetLastWriteTimeUtc(text) <= writeTimeUtc)
			{
				return false;
			}
			return !GeneratedImportSourceHashMatches(pagePath, text);
		}
		catch
		{
			return true;
		}
	}

	private static bool IsRuntimeRawImagePath(string resourcePath)
	{
		if (string.IsNullOrWhiteSpace(resourcePath) || !resourcePath.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		string text = Path.GetExtension(resourcePath).ToLowerInvariant();
		if (text != null)
		{
			int length = text.Length;
			if (length != 4)
			{
				if (length == 5)
				{
					char c = text[1];
					if (c != 'j')
					{
						if (c == 'w' && text == ".webp")
						{
							goto IL_00fd;
						}
					}
					else if (text == ".jpeg")
					{
						goto IL_00fd;
					}
				}
			}
			else
			{
				char c = text[1];
				if ((uint)c <= 104u)
				{
					if (c != 'b')
					{
						if (c != 'e')
						{
							if (c == 'h' && text == ".hdr")
							{
								goto IL_00fd;
							}
						}
						else if (text == ".exr")
						{
							goto IL_00fd;
						}
					}
					else if (text == ".bmp")
					{
						goto IL_00fd;
					}
				}
				else if (c != 'j')
				{
					if (c != 'p')
					{
						if (c == 't' && text == ".tga")
						{
							goto IL_00fd;
						}
					}
					else if (text == ".png")
					{
						goto IL_00fd;
					}
				}
				else if (text == ".jpg")
				{
					goto IL_00fd;
				}
			}
		}
		return false;
		IL_00fd:
		return true;
	}

	private static bool GeneratedImportSourceHashMatches(string resourcePath, string absoluteSourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath) || string.IsNullOrEmpty(absoluteSourcePath) || !File.Exists(absoluteSourcePath))
		{
			return false;
		}
		string text = ProjectSettings.GlobalizePath("res://.godot/imported");
		if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
		{
			return false;
		}
		string path = Path.Combine(text, resourcePath.GetFile() + "-" + GetImportPathHash(resourcePath) + ".md5");
		if (!File.Exists(path))
		{
			return false;
		}
		try
		{
			Match match = Regex.Match(File.ReadAllText(path), "(?m)^source_md5=\"(?<hash>[0-9a-fA-F]{32})\"$");
			if (!match.Success)
			{
				return false;
			}
			using FileStream source = File.OpenRead(absoluteSourcePath);
			return Convert.ToHexString(MD5.HashData(source)).Equals(match.Groups["hash"].Value, StringComparison.OrdinalIgnoreCase);
		}
		catch
		{
			return false;
		}
	}

	private static bool TryGetGeneratedImportWriteTimeUtc(string resourcePath, out DateTime writeTimeUtc)
	{
		writeTimeUtc = default(DateTime);
		bool flag = false;
		foreach (string generatedImportedTexturePath in GetGeneratedImportedTexturePaths(resourcePath))
		{
			string text = ProjectSettings.GlobalizePath(generatedImportedTexturePath);
			if (string.IsNullOrEmpty(text) || !File.Exists(text))
			{
				continue;
			}
			try
			{
				DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(text);
				if (!flag || lastWriteTimeUtc > writeTimeUtc)
				{
					writeTimeUtc = lastWriteTimeUtc;
				}
				flag = true;
			}
			catch
			{
			}
		}
		return flag;
	}

	private static IEnumerable<string> GetGeneratedImportedTexturePaths(string resourcePath)
	{
		string text = ProjectSettings.GlobalizePath(resourcePath + ".import");
		if (!string.IsNullOrEmpty(text) && File.Exists(text))
		{
			_ = string.Empty;
			string text2;
			try
			{
				text2 = File.ReadAllText(text);
			}
			catch
			{
				text2 = string.Empty;
			}
			if (!string.IsNullOrEmpty(text2))
			{
				foreach (Match item in Regex.Matches(text2, "\"(res://\\.godot/imported/[^\"]+\\.ctex(?:array)?)\""))
				{
					yield return item.Groups[1].Value;
				}
			}
		}
		string text3 = ProjectSettings.GlobalizePath("res://.godot/imported");
		if (string.IsNullOrEmpty(text3) || !Directory.Exists(text3))
		{
			yield break;
		}
		string text4 = resourcePath.GetFile() + "-" + GetImportPathHash(resourcePath);
		IEnumerable<string> enumerable;
		try
		{
			enumerable = new List<string>(Directory.EnumerateFiles(text3, text4 + "*.ctex*"));
		}
		catch
		{
			enumerable = System.Array.Empty<string>();
		}
		foreach (string item2 in enumerable)
		{
			yield return "res://.godot/imported/" + Path.GetFileName(item2);
		}
	}

	private static AdobeAnimateGlobalAtlasManifest TryReadManifestText(string manifestPath, string generatedAtlasDir, string pagePrefix, ref string[] fallbackPaths, ref bool fallbackAttempted)
	{
		if (!TryReadTextResource(manifestPath, out var text, out var error))
		{
			if (!string.IsNullOrEmpty(error))
			{
				GD.PushWarning("Unable to read AdobeAnimate atlas manifest text " + manifestPath + ": " + error);
			}
			return null;
		}
		AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = new AdobeAnimateGlobalAtlasManifest();
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayPath = ReadStringProperty(text, "AtlasTextureArrayPath");
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize = ReadVector2Property(text, "AtlasTextureArrayLayerSize");
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerOffset = ReadIntProperty(text, "AtlasTextureArrayLayerOffset");
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerCount = ReadIntProperty(text, "AtlasTextureArrayLayerCount");
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayColumns = ReadIntProperty(text, "AtlasTextureArrayColumns");
		adobeAnimateGlobalAtlasManifest.PoseTextureArrayPath = ReadStringProperty(text, "PoseTextureArrayPath");
		adobeAnimateGlobalAtlasManifest.PoseTextureArrayLayerSize = ReadVector2IProperty(text, "PoseTextureArrayLayerSize");
		adobeAnimateGlobalAtlasManifest.PoseTextureArrayLayerCount = ReadIntProperty(text, "PoseTextureArrayLayerCount");
		adobeAnimateGlobalAtlasManifest.PoseTextureArrayColumns = ReadIntProperty(text, "PoseTextureArrayColumns");
		ReadStringArray(text, "SourceKeys", adobeAnimateGlobalAtlasManifest.SourceKeys);
		ReadIntArray(text, "SourceStarts", adobeAnimateGlobalAtlasManifest.SourceStarts);
		ReadIntArray(text, "SourceCounts", adobeAnimateGlobalAtlasManifest.SourceCounts);
		ReadStringArray(text, "SourceSignatures", adobeAnimateGlobalAtlasManifest.SourceSignatures);
		ReadStringArray(text, "PoseAtlasPagePaths", adobeAnimateGlobalAtlasManifest.PoseAtlasPagePaths);
		ReadIntArray(text, "SourcePoseAtlasPages", adobeAnimateGlobalAtlasManifest.SourcePoseAtlasPages);
		ReadIntArray(text, "SourcePoseTexelStarts", adobeAnimateGlobalAtlasManifest.SourcePoseTexelStarts);
		ReadIntArray(text, "SourcePoseTexelCounts", adobeAnimateGlobalAtlasManifest.SourcePoseTexelCounts);
		ReadStringArray(text, "SourcePoseSignatures", adobeAnimateGlobalAtlasManifest.SourcePoseSignatures);
		ReadIntArray(text, "MediaAtlasPages", adobeAnimateGlobalAtlasManifest.MediaAtlasPages);
		ReadRect2Array(text, "MediaRects", adobeAnimateGlobalAtlasManifest.MediaRects);
		ReadStringArray(text, "ExternalTexturePaths", adobeAnimateGlobalAtlasManifest.ExternalTexturePaths);
		ReadIntArray(text, "ExternalTextureAtlasPages", adobeAnimateGlobalAtlasManifest.ExternalTextureAtlasPages);
		ReadRect2Array(text, "ExternalTextureRects", adobeAnimateGlobalAtlasManifest.ExternalTextureRects);
		bool num = adobeAnimateGlobalAtlasManifest.SourceKeys.Count > 0 && adobeAnimateGlobalAtlasManifest.SourceStarts.Count == adobeAnimateGlobalAtlasManifest.SourceKeys.Count && adobeAnimateGlobalAtlasManifest.SourceCounts.Count == adobeAnimateGlobalAtlasManifest.SourceKeys.Count && adobeAnimateGlobalAtlasManifest.MediaAtlasPages.Count > 0 && adobeAnimateGlobalAtlasManifest.MediaRects.Count > 0;
		bool flag = adobeAnimateGlobalAtlasManifest.ExternalTexturePaths.Count > 0 && adobeAnimateGlobalAtlasManifest.ExternalTextureAtlasPages.Count == adobeAnimateGlobalAtlasManifest.ExternalTexturePaths.Count && adobeAnimateGlobalAtlasManifest.ExternalTextureRects.Count == adobeAnimateGlobalAtlasManifest.ExternalTexturePaths.Count;
		if (!num && !flag)
		{
			GD.PushWarning("AdobeAnimate shared atlas manifest text could not parse required data from " + manifestPath + ".");
			return null;
		}
		return adobeAnimateGlobalAtlasManifest;
	}

	private static bool TryReadTextResource(string resourcePath, out string text, out string error)
	{
		text = string.Empty;
		error = string.Empty;
		if (Godot.FileAccess.FileExists(resourcePath))
		{
			using (Godot.FileAccess fileAccess = Godot.FileAccess.Open(resourcePath, Godot.FileAccess.ModeFlags.Read))
			{
				if (fileAccess == null)
				{
					error = Godot.FileAccess.GetOpenError().ToString();
					return false;
				}
				text = fileAccess.GetAsText();
				return true;
			}
		}
		string text2 = ProjectSettings.GlobalizePath(resourcePath);
		if (string.IsNullOrEmpty(text2) || !File.Exists(text2))
		{
			return false;
		}
		try
		{
			text = File.ReadAllText(text2);
			return true;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	private static void ReadStringArray(string text, string propertyName, Array<string> target)
	{
		string text2 = ExtractArrayBody(text, propertyName);
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		foreach (Match item in Regex.Matches(text2, "\"((?:\\\\.|[^\"])*)\""))
		{
			target.Add(item.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\"));
		}
	}

	private static string ReadStringProperty(string text, string propertyName)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(propertyName))
		{
			return string.Empty;
		}
		Match match = Regex.Match(text, "^\\s*" + Regex.Escape(propertyName) + "\\s*=\\s*\"((?:\\\\.|[^\"])*)\"", RegexOptions.Multiline);
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups[1].Value.Replace("\\\"", "\"").Replace("\\\\", "\\");
	}

	private static int ReadIntProperty(string text, string propertyName)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(propertyName))
		{
			return 0;
		}
		Match match = Regex.Match(text, "^\\s*" + Regex.Escape(propertyName) + "\\s*=\\s*(-?\\d+)", RegexOptions.Multiline);
		if (!match.Success || !int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return 0;
		}
		return result;
	}

	private static Vector2 ReadVector2Property(string text, string propertyName)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(propertyName))
		{
			return Vector2.Zero;
		}
		Match match = Regex.Match(text, "^\\s*" + Regex.Escape(propertyName) + "\\s*=\\s*Vector2\\(([^,]+),\\s*([^)]+)\\)", RegexOptions.Multiline);
		if (!match.Success)
		{
			return Vector2.Zero;
		}
		if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
		{
			return Vector2.Zero;
		}
		if (!float.TryParse(match.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2))
		{
			return Vector2.Zero;
		}
		return new Vector2(result, result2);
	}

	private static Vector2I ReadVector2IProperty(string text, string propertyName)
	{
		if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(propertyName))
		{
			return Vector2I.Zero;
		}
		Match match = Regex.Match(text, "^\\s*" + Regex.Escape(propertyName) + "\\s*=\\s*Vector2i\\(([^,]+),\\s*([^)]+)\\)", RegexOptions.IgnoreCase | RegexOptions.Multiline);
		if (!match.Success)
		{
			return Vector2I.Zero;
		}
		if (!int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
		{
			return Vector2I.Zero;
		}
		if (!int.TryParse(match.Groups[2].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2))
		{
			return Vector2I.Zero;
		}
		return new Vector2I(result, result2);
	}

	private static void ReadIntArray(string text, string propertyName, Array<int> target)
	{
		string text2 = ExtractArrayBody(text, propertyName);
		if (string.IsNullOrWhiteSpace(text2))
		{
			return;
		}
		string[] array = text2.Split(',');
		for (int i = 0; i < array.Length; i++)
		{
			if (int.TryParse(array[i].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				target.Add(result);
			}
		}
	}

	private static void ReadRect2Array(string text, string propertyName, Array<Rect2> target)
	{
		string text2 = ExtractArrayBody(text, propertyName);
		if (string.IsNullOrEmpty(text2))
		{
			return;
		}
		foreach (Match item in Regex.Matches(text2, "Rect2\\(([^,]+),\\s*([^,]+),\\s*([^,]+),\\s*([^)]+)\\)"))
		{
			if (float.TryParse(item.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result) && float.TryParse(item.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && float.TryParse(item.Groups[3].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result3) && float.TryParse(item.Groups[4].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result4))
			{
				target.Add(new Rect2(result, result2, result3, result4));
			}
		}
	}

	private static bool TryParseSignature(string value, out ulong signature)
	{
		signature = 0uL;
		if (string.IsNullOrWhiteSpace(value))
		{
			return false;
		}
		value = value.Trim();
		if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
		{
			string text = value;
			value = text.Substring(2, text.Length - 2);
		}
		if (!ulong.TryParse(value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out signature))
		{
			return ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out signature);
		}
		return true;
	}

	private static bool SourceSignatureMatches(AdobeAnimateGlobalAtlasManifest manifest, int sourceIndex, ulong expectedSignature)
	{
		if (expectedSignature == 0L)
		{
			return true;
		}
		if (manifest?.SourceSignatures == null || manifest.SourceSignatures.Count != manifest.SourceKeys.Count || sourceIndex < 0 || sourceIndex >= manifest.SourceSignatures.Count)
		{
			return false;
		}
		if (TryParseSignature(manifest.SourceSignatures[sourceIndex], out var signature))
		{
			return signature == expectedSignature;
		}
		return false;
	}

	private static string ExtractArrayBody(string text, string propertyName)
	{
		string value = propertyName + " = Array";
		int num = text.IndexOf(value, StringComparison.Ordinal);
		if (num < 0)
		{
			return string.Empty;
		}
		int num2 = text.IndexOf("([", num, StringComparison.Ordinal);
		if (num2 < 0)
		{
			return string.Empty;
		}
		int num3 = num2 + 2;
		int num4 = FindArrayBodyEnd(text, num3);
		if (num4 < num3)
		{
			return string.Empty;
		}
		return text.Substring(num3, num4 - num3);
	}

	private static int FindArrayBodyEnd(string text, int bodyStart)
	{
		bool flag = false;
		int num = 0;
		for (int i = bodyStart; i < text.Length - 1; i++)
		{
			char c = text[i];
			if (c == '"' && (i == 0 || text[i - 1] != '\\'))
			{
				flag = !flag;
			}
			else
			{
				if (flag)
				{
					continue;
				}
				switch (c)
				{
				case '(':
					num++;
					continue;
				case ')':
					if (num > 0)
					{
						num--;
						continue;
					}
					break;
				}
				if (c == ']' && text[i + 1] == ')' && num == 0)
				{
					return i;
				}
			}
		}
		return -1;
	}

	private static void SanitizeLegacyManifestTexturePages()
	{
		string path = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres");
		if (!File.Exists(path))
		{
			return;
		}
		string text;
		try
		{
			text = File.ReadAllText(path);
		}
		catch (Exception ex)
		{
			GD.PushWarning("Unable to inspect AdobeAnimate atlas manifest res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres: " + ex.Message);
			return;
		}
		bool num = text.Contains("AtlasPages = Array[Texture2D]", StringComparison.Ordinal) || (text.Contains("AdobeAnimateGlobalAtlasPage_", StringComparison.Ordinal) && text.Contains(".tres", StringComparison.Ordinal));
		bool flag = text.Contains("AtlasPagePaths = Array[String]", StringComparison.Ordinal) || text.Contains("AtlasPageSizes = Array[Vector2]", StringComparison.Ordinal);
		if (!num && !flag)
		{
			return;
		}
		string text2 = RemoveLegacyAtlasPageLines(text);
		if (text2 == text)
		{
			return;
		}
		try
		{
			File.WriteAllText(path, text2, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
			GD.PushWarning("AdobeAnimate shared atlas manifest contained legacy AtlasPages references; sanitized res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres before loading.");
		}
		catch (Exception ex2)
		{
			GD.PushWarning("Unable to sanitize AdobeAnimate atlas manifest res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres: " + ex2.Message);
		}
	}

	private static string RemoveLegacyAtlasPageLines(string text)
	{
		StringBuilder stringBuilder = new StringBuilder(text.Length);
		using StringReader stringReader = new StringReader(text);
		while (true)
		{
			string text2 = stringReader.ReadLine();
			if (text2 == null)
			{
				break;
			}
			if (!IsLegacyAtlasPageLine(text2))
			{
				stringBuilder.Append(text2).Append('\n');
			}
		}
		return stringBuilder.ToString();
	}

	private static bool IsLegacyAtlasPageLine(string line)
	{
		if (line.StartsWith("AtlasPages = Array[Texture2D]", StringComparison.Ordinal))
		{
			return true;
		}
		if (line.StartsWith("AtlasPagePaths = Array[String]", StringComparison.Ordinal))
		{
			return true;
		}
		if (line.StartsWith("AtlasPageSizes = Array[Vector2]", StringComparison.Ordinal))
		{
			return true;
		}
		if (!line.StartsWith("[ext_resource type=\"Texture2D\"", StringComparison.Ordinal))
		{
			return false;
		}
		if (line.Contains("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasPage_", StringComparison.Ordinal))
		{
			return line.Contains(".tres", StringComparison.Ordinal);
		}
		return false;
	}

	private static Vector2 GetManifestAtlasLayerSize(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest == null)
		{
			return new Vector2(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y);
		}
		if (!(manifest.AtlasTextureArrayLayerSize.X > 0f) || !(manifest.AtlasTextureArrayLayerSize.Y > 0f))
		{
			return new Vector2(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y);
		}
		return manifest.AtlasTextureArrayLayerSize;
	}

	private static string[] GetGeneratedAtlasPagePaths()
	{
		return GetGeneratedAtlasPagePaths("res://addons/AdobeAnimateEditor/GeneratedAtlas", "AdobeAnimateGlobalAtlasPage_", ref ManifestAtlasPagePathFallback, ref ManifestAtlasPagePathFallbackAttempted);
	}

	public static string[] GetGeneratedVisualAtlasPagePaths()
	{
		List<string> list = new List<string>();
		AppendGeneratedAtlasPagePaths(list, "res://addons/AdobeAnimateEditor/GeneratedAtlas", "AdobeAnimateGlobalAtlasPage_");
		return list.ToArray();
	}

	private static void AppendGeneratedAtlasPagePaths(List<string> paths, string generatedAtlasDir, string pagePrefix)
	{
		if (paths == null)
		{
			return;
		}
		string path = ProjectSettings.GlobalizePath(generatedAtlasDir);
		if (!Directory.Exists(path))
		{
			return;
		}
		List<(int, string)> list = new List<(int, string)>();
		foreach (string item in Directory.EnumerateFiles(path, pagePrefix + "*.png"))
		{
			if (TryGetGeneratedAtlasPageIndex(item, pagePrefix, out var index))
			{
				string text = ProjectSettings.LocalizePath(item.Replace('\\', '/'));
				if (!string.IsNullOrEmpty(text))
				{
					list.Add((index, text));
				}
			}
		}
		list.Sort(((int Index, string Path) a, (int Index, string Path) b) => a.Index.CompareTo(b.Index));
		for (int num = 0; num < list.Count; num++)
		{
			paths.Add(list[num].Item2);
		}
	}

	private static string[] GetGeneratedAtlasPagePaths(string generatedAtlasDir, string pagePrefix, ref string[] fallbackPathsCache, ref bool fallbackAttempted)
	{
		if (fallbackAttempted)
		{
			return fallbackPathsCache;
		}
		fallbackAttempted = true;
		string path = ProjectSettings.GlobalizePath(generatedAtlasDir);
		if (!Directory.Exists(path))
		{
			return fallbackPathsCache;
		}
		List<(int, string)> list = new List<(int, string)>();
		foreach (string item2 in Directory.EnumerateFiles(path, pagePrefix + "*.png"))
		{
			if (TryGetGeneratedAtlasPageIndex(item2, pagePrefix, out var index))
			{
				string item = ProjectSettings.LocalizePath(item2.Replace('\\', '/'));
				list.Add((index, item));
			}
		}
		list.Sort(((int Index, string Path) a, (int Index, string Path) b) => a.Index.CompareTo(b.Index));
		string[] array = new string[list.Count];
		for (int num = 0; num < list.Count; num++)
		{
			array[num] = list[num].Item2;
		}
		fallbackPathsCache = array;
		return fallbackPathsCache;
	}

	private static bool TryGetGeneratedAtlasPageIndex(string absolutePath, string pagePrefix, out int index)
	{
		index = -1;
		string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(absolutePath);
		if (string.IsNullOrEmpty(fileNameWithoutExtension) || !fileNameWithoutExtension.StartsWith(pagePrefix, StringComparison.Ordinal))
		{
			return false;
		}
		return int.TryParse(fileNameWithoutExtension.Substring(pagePrefix.Length), out index);
	}

	private static void WarnProjectAtlasUnavailable(string sourceKey)
	{
		if (Manifest == null)
		{
			if (!MissingAtlasManifestWarningPrinted)
			{
				MissingAtlasManifestWarningPrinted = true;
				GD.PushWarning("AdobeAnimate atlas manifest is missing or failed to load: res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres. Click 'Refresh Adobe Atlas' in the toolbar before running animations.");
			}
		}
		else if (MissingAtlasEntryWarnings.Count < 16 && MissingAtlasEntryWarnings.Add(sourceKey))
		{
			GD.PushWarning($"AdobeAnimate unified atlas does not contain {sourceKey}. Click 'Refresh Adobe Atlas' to rebuild {"res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres"}.");
		}
	}

	private static bool TryGetManifestPoseTextureArray(AdobeAnimateGlobalAtlasManifest manifest, out TextureLayered textureArray, out Rid textureArrayRid, out Vector2I textureSize)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		textureSize = Vector2I.Zero;
		if (manifest == null)
		{
			return false;
		}
		if (PoseTextureArrayAllocations.TryGetValue(manifest, out (TextureLayered, Rid, Vector2I) value) && GodotObject.IsInstanceValid(value.Item1) && value.Item2.IsValid)
		{
			(textureArray, textureArrayRid, textureSize) = value;
			return true;
		}
		if (HasImportedPoseTextureArrayMetadata(manifest))
		{
			if (!ShouldLoadGeneratedImageFallbackFirst(manifest.PoseTextureArrayPath))
			{
				TextureLayered textureLayered = ResourceLoader.Load<TextureLayered>(manifest.PoseTextureArrayPath, null, ResourceLoader.CacheMode.Reuse);
				if (IsImportedPoseTextureArrayValid(textureLayered, manifest))
				{
					textureArray = textureLayered;
					textureArrayRid = textureLayered.GetRid();
					textureSize = manifest.PoseTextureArrayLayerSize;
					PoseTextureArrayAllocations[manifest] = (textureArray, textureArrayRid, textureSize);
					return true;
				}
			}
			if (TryCreatePoseTextureArrayFromArraySource(manifest, out textureArray, out textureArrayRid, out textureSize))
			{
				PoseTextureArrayAllocations[manifest] = (textureArray, textureArrayRid, textureSize);
				return true;
			}
		}
		if (manifest.PoseAtlasPagePaths == null || manifest.PoseAtlasPagePaths.Count == 0)
		{
			return false;
		}
		Array<Image> array = new Array<Image>();
		try
		{
			Vector2I vector2I = Vector2I.Zero;
			for (int i = 0; i < manifest.PoseAtlasPagePaths.Count; i++)
			{
				if (!TryLoadPoseArrayLayerImage(manifest.PoseAtlasPagePaths[i], out var layerImage))
				{
					return false;
				}
				array.Add(layerImage);
				if (vector2I == Vector2I.Zero)
				{
					vector2I = new Vector2I(layerImage.GetWidth(), layerImage.GetHeight());
				}
				if (layerImage.GetWidth() != vector2I.X || layerImage.GetHeight() != vector2I.Y)
				{
					return false;
				}
			}
			if (array.Count == 0 || vector2I.X <= 0 || vector2I.Y <= 0)
			{
				return false;
			}
			Texture2DArray texture2DArray = new Texture2DArray();
			Error error = texture2DArray.CreateFromImages(array);
			if (error != Error.Ok)
			{
				GD.PushWarning($"AdobeAnimate pose Texture2DArray build failed: {error}");
				return false;
			}
			textureArray = texture2DArray;
			textureArrayRid = texture2DArray.GetRid();
			textureSize = vector2I;
			PoseTextureArrayAllocations[manifest] = (textureArray, textureArrayRid, textureSize);
			return textureArrayRid.IsValid;
		}
		finally
		{
			foreach (Image item in array)
			{
				item?.Dispose();
			}
			array.Clear();
		}
	}

	private static bool HasImportedPoseTextureArrayMetadata(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest != null && !string.IsNullOrEmpty(manifest.PoseTextureArrayPath) && manifest.PoseTextureArrayLayerSize.X > 0 && manifest.PoseTextureArrayLayerSize.Y > 0)
		{
			return manifest.PoseTextureArrayLayerCount > 0;
		}
		return false;
	}

	private static bool IsImportedPoseTextureArrayValid(TextureLayered textureArray, AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (!GodotObject.IsInstanceValid(textureArray) || !textureArray.GetRid().IsValid || manifest == null)
		{
			return false;
		}
		if (textureArray.GetLayers() >= manifest.PoseTextureArrayLayerCount && textureArray.GetWidth() == manifest.PoseTextureArrayLayerSize.X)
		{
			return textureArray.GetHeight() == manifest.PoseTextureArrayLayerSize.Y;
		}
		return false;
	}

	private static bool TryCreatePoseTextureArrayFromArraySource(AdobeAnimateGlobalAtlasManifest manifest, out TextureLayered textureArray, out Rid textureArrayRid, out Vector2I textureSize)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		textureSize = Vector2I.Zero;
		if (!HasImportedPoseTextureArrayMetadata(manifest))
		{
			return false;
		}
		string text = ProjectSettings.GlobalizePath(manifest.PoseTextureArrayPath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return false;
		}
		Image image = Image.LoadFromFile(text);
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			image?.Dispose();
			return false;
		}
		try
		{
			if (image.GetFormat() != Image.Format.Rgbah)
			{
				image.Convert(Image.Format.Rgbah);
			}
			int x = manifest.PoseTextureArrayLayerSize.X;
			int y = manifest.PoseTextureArrayLayerSize.Y;
			int num = Math.Max(1, manifest.PoseTextureArrayColumns);
			Array<Image> array = new Array<Image>();
			try
			{
				for (int i = 0; i < manifest.PoseTextureArrayLayerCount; i++)
				{
					int num2 = i % num;
					int num3 = i / num;
					Image region = image.GetRegion(new Rect2I(num2 * x, num3 * y, x, y));
					if (region.GetFormat() != Image.Format.Rgbah)
					{
						region.Convert(Image.Format.Rgbah);
					}
					array.Add(region);
				}
				Texture2DArray texture2DArray = new Texture2DArray();
				Error error = texture2DArray.CreateFromImages(array);
				if (error != Error.Ok)
				{
					GD.PushWarning($"AdobeAnimate pose Texture2DArray source fallback build failed: {error}");
					return false;
				}
				textureArray = texture2DArray;
				textureArrayRid = texture2DArray.GetRid();
				textureSize = manifest.PoseTextureArrayLayerSize;
				return textureArrayRid.IsValid;
			}
			finally
			{
				foreach (Image item in array)
				{
					item?.Dispose();
				}
				array.Clear();
			}
		}
		finally
		{
			image.Dispose();
		}
	}

	private static bool TryGetManifestTextureArrayLayerOffset(AdobeAnimateGlobalAtlasManifest manifest, int atlasPageCount, out int layerOffset)
	{
		layerOffset = 0;
		if (manifest == null || atlasPageCount <= 0)
		{
			return false;
		}
		if (HasImportedVisualTextureArrayMetadata(manifest))
		{
			layerOffset = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
			return true;
		}
		if (SharedVisualTextureArray != null && GodotObject.IsInstanceValid(SharedVisualTextureArray.Texture) && SharedVisualTextureArray.Rid.IsValid && SharedVisualTextureArray.LayerOffsets.TryGetValue(manifest, out layerOffset))
		{
			return true;
		}
		List<ManifestTextureArrayEntry> list = BuildSharedVisualTextureArrayEntries(manifest, atlasPageCount, requireImages: false);
		for (int i = 0; i < list.Count; i++)
		{
			if (list[i].Manifest == manifest)
			{
				layerOffset = list[i].LayerOffset;
				return true;
			}
		}
		return false;
	}

	private static int[] OffsetAtlasPages(int[] pages, int layerOffset)
	{
		if (pages == null || pages.Length == 0 || layerOffset <= 0)
		{
			return pages;
		}
		int[] array = new int[pages.Length];
		for (int i = 0; i < pages.Length; i++)
		{
			array[i] = ((pages[i] >= 0) ? (pages[i] + layerOffset) : pages[i]);
		}
		return array;
	}

	private static bool TryGetManifestTextureArray(AdobeAnimateGlobalAtlasManifest manifest, int atlasPageCount, out TextureLayered textureArray, out Rid textureArrayRid, out int layerOffset)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		layerOffset = 0;
		if (manifest == null || atlasPageCount <= 0)
		{
			return false;
		}
		if (SharedVisualTextureArray != null && GodotObject.IsInstanceValid(SharedVisualTextureArray.Texture) && SharedVisualTextureArray.Rid.IsValid && SharedVisualTextureArray.LayerOffsets.TryGetValue(manifest, out layerOffset))
		{
			textureArray = SharedVisualTextureArray.Texture;
			textureArrayRid = SharedVisualTextureArray.Rid;
			return true;
		}
		if (TryLoadImportedVisualTextureArray(manifest, out textureArray, out textureArrayRid, out layerOffset))
		{
			return true;
		}
		List<ManifestTextureArrayEntry> list = BuildSharedVisualTextureArrayEntries(manifest, atlasPageCount, requireImages: true);
		if (list.Count == 0)
		{
			return false;
		}
		Array<Image> array = new Array<Image>();
		System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, int> dictionary = new System.Collections.Generic.Dictionary<AdobeAnimateGlobalAtlasManifest, int>();
		for (int i = 0; i < list.Count; i++)
		{
			ManifestTextureArrayEntry manifestTextureArrayEntry = list[i];
			dictionary[manifestTextureArrayEntry.Manifest] = manifestTextureArrayEntry.LayerOffset;
			for (int j = 0; j < manifestTextureArrayEntry.PageCount; j++)
			{
				if (!TryLoadAtlasArrayLayerImage(manifestTextureArrayEntry.PagePaths[j], out var layerImage))
				{
					if (manifestTextureArrayEntry.Manifest != manifest)
					{
						break;
					}
					return false;
				}
				array.Add(layerImage);
			}
		}
		if (array.Count == 0 || !dictionary.TryGetValue(manifest, out layerOffset))
		{
			return false;
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		Error error = texture2DArray.CreateFromImages(array);
		if (error != Error.Ok)
		{
			GD.PushWarning($"AdobeAnimate atlas Texture2DArray build failed: {error}");
			return false;
		}
		textureArray = texture2DArray;
		textureArrayRid = texture2DArray.GetRid();
		SharedVisualTextureArray = new SharedVisualTextureArrayAllocation
		{
			Texture = textureArray,
			Rid = textureArrayRid
		};
		foreach (KeyValuePair<AdobeAnimateGlobalAtlasManifest, int> item in dictionary)
		{
			SharedVisualTextureArray.LayerOffsets[item.Key] = item.Value;
		}
		return textureArrayRid.IsValid;
	}

	private static bool TryGetProfileManifestTextureArray(AdobeAnimateGlobalAtlasManifest manifest, int atlasPageCount, out TextureLayered textureArray, out Rid textureArrayRid, out int layerOffset)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		layerOffset = 0;
		if (manifest == null || atlasPageCount <= 0 || !HasImportedVisualTextureArrayMetadata(manifest))
		{
			return false;
		}
		if (ProfileVisualTextureArrayAllocations.TryGetValue(manifest, out var value) && GodotObject.IsInstanceValid(value.Texture) && value.Rid.IsValid)
		{
			textureArray = value.Texture;
			textureArrayRid = value.Rid;
			layerOffset = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
			return true;
		}
		if (!ShouldLoadGeneratedImageFallbackFirst(manifest.AtlasTextureArrayPath))
		{
			TextureLayered textureLayered = ResourceLoader.Load<TextureLayered>(manifest.AtlasTextureArrayPath, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(textureLayered) && textureLayered.GetRid().IsValid)
			{
				textureArray = textureLayered;
				textureArrayRid = textureLayered.GetRid();
			}
		}
		if (!textureArrayRid.IsValid && !TryCreateTextureArrayFromVisualArraySource(manifest, out textureArray, out textureArrayRid))
		{
			return false;
		}
		layerOffset = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
		ProfileVisualTextureArrayAllocations[manifest] = new SharedVisualTextureArrayAllocation
		{
			Texture = textureArray,
			Rid = textureArrayRid
		};
		return textureArrayRid.IsValid;
	}

	private static bool HasImportedVisualTextureArrayMetadata(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest != null && !string.IsNullOrEmpty(manifest.AtlasTextureArrayPath) && manifest.AtlasTextureArrayLayerSize.X > 0f && manifest.AtlasTextureArrayLayerSize.Y > 0f)
		{
			return manifest.AtlasTextureArrayLayerCount > 0;
		}
		return false;
	}

	private static bool TryLoadImportedVisualTextureArray(AdobeAnimateGlobalAtlasManifest manifest, out TextureLayered textureArray, out Rid textureArrayRid, out int layerOffset)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		layerOffset = 0;
		if (!HasImportedVisualTextureArrayMetadata(manifest))
		{
			return false;
		}
		string atlasTextureArrayPath = manifest.AtlasTextureArrayPath;
		if (!ShouldLoadGeneratedImageFallbackFirst(atlasTextureArrayPath))
		{
			TextureLayered textureLayered = ResourceLoader.Load<TextureLayered>(atlasTextureArrayPath, null, ResourceLoader.CacheMode.Reuse);
			if (GodotObject.IsInstanceValid(textureLayered))
			{
				textureArray = textureLayered;
				textureArrayRid = textureLayered.GetRid();
				layerOffset = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
				if (textureArrayRid.IsValid)
				{
					CacheSharedVisualTextureArray(textureArray, textureArrayRid, atlasTextureArrayPath);
					return true;
				}
			}
		}
		if (!TryCreateTextureArrayFromVisualArraySource(manifest, out textureArray, out textureArrayRid))
		{
			return false;
		}
		layerOffset = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
		CacheSharedVisualTextureArray(textureArray, textureArrayRid, atlasTextureArrayPath);
		return textureArrayRid.IsValid;
	}

	private static void CacheSharedVisualTextureArray(TextureLayered textureArray, Rid textureArrayRid, string arrayPath)
	{
		if (GodotObject.IsInstanceValid(textureArray) && textureArrayRid.IsValid && !string.IsNullOrEmpty(arrayPath))
		{
			SharedVisualTextureArray = new SharedVisualTextureArrayAllocation
			{
				Texture = textureArray,
				Rid = textureArrayRid
			};
			AddSharedVisualTextureArrayOffset(GetManifest(), arrayPath);
		}
	}

	private static void AddSharedVisualTextureArrayOffset(AdobeAnimateGlobalAtlasManifest manifest, string arrayPath)
	{
		if (SharedVisualTextureArray != null && manifest != null && string.Equals(manifest.AtlasTextureArrayPath, arrayPath, StringComparison.OrdinalIgnoreCase))
		{
			SharedVisualTextureArray.LayerOffsets[manifest] = Math.Max(0, manifest.AtlasTextureArrayLayerOffset);
		}
	}

	private static bool TryCreateTextureArrayFromVisualArraySource(AdobeAnimateGlobalAtlasManifest manifest, out TextureLayered textureArray, out Rid textureArrayRid)
	{
		textureArray = null;
		textureArrayRid = default(Rid);
		string text = ProjectSettings.GlobalizePath(manifest.AtlasTextureArrayPath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return false;
		}
		Image image = Image.LoadFromFile(text);
		if (image == null)
		{
			return false;
		}
		if (image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			image.Dispose();
			return false;
		}
		if (image.GetFormat() != Image.Format.Rgba8)
		{
			image.Convert(Image.Format.Rgba8);
		}
		int num = Math.Max(1, Mathf.RoundToInt(manifest.AtlasTextureArrayLayerSize.X));
		int num2 = Math.Max(1, Mathf.RoundToInt(manifest.AtlasTextureArrayLayerSize.Y));
		int num3 = ((manifest.AtlasTextureArrayColumns > 0) ? manifest.AtlasTextureArrayColumns : Math.Max(1, image.GetWidth() / num));
		int num4 = Math.Max(1, image.GetHeight() / num2);
		int num5 = Math.Max(1, num3 * num4);
		Array<Image> array = new Array<Image>();
		for (int i = 0; i < num5; i++)
		{
			int num6 = i % num3;
			int num7 = i / num3;
			Image region = image.GetRegion(new Rect2I(num6 * num, num7 * num2, num, num2));
			if (region.GetFormat() != Image.Format.Rgba8)
			{
				region.Convert(Image.Format.Rgba8);
			}
			array.Add(region);
		}
		Texture2DArray texture2DArray = new Texture2DArray();
		Error error = texture2DArray.CreateFromImages(array);
		if (error != Error.Ok)
		{
			GD.PushWarning($"AdobeAnimate visual Texture2DArray fallback build failed: {error}");
			return false;
		}
		textureArray = texture2DArray;
		textureArrayRid = texture2DArray.GetRid();
		return textureArrayRid.IsValid;
	}

	private static List<ManifestTextureArrayEntry> BuildSharedVisualTextureArrayEntries(AdobeAnimateGlobalAtlasManifest requestedManifest, int requestedPageCount, bool requireImages)
	{
		List<ManifestTextureArrayEntry> list = new List<ManifestTextureArrayEntry>(5);
		int layerOffset = 0;
		if (requestedManifest != null && !ContainsManifestEntry(list, requestedManifest))
		{
			AddManifestTextureArrayEntry(list, requestedManifest, requestedManifest, requestedPageCount, requireImages, ref layerOffset);
		}
		AddManifestTextureArrayEntry(list, GetManifest(), requestedManifest, requestedPageCount, requireImages, ref layerOffset);
		return list;
	}

	private static void AddManifestTextureArrayEntry(List<ManifestTextureArrayEntry> entries, AdobeAnimateGlobalAtlasManifest manifest, AdobeAnimateGlobalAtlasManifest requestedManifest, int requestedPageCount, bool requireImages, ref int layerOffset)
	{
		if (manifest == null || ContainsManifestEntry(entries, manifest))
		{
			return;
		}
		int num = ((manifest == requestedManifest) ? requestedPageCount : GetManifestAtlasPageCount(manifest));
		if (num <= 0)
		{
			return;
		}
		string[] array = (requireImages ? GetManifestVisualAtlasPagePaths(manifest, num) : System.Array.Empty<string>());
		if (requireImages)
		{
			if (array.Length < num)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				string text = ProjectSettings.GlobalizePath(array[i]);
				if (string.IsNullOrEmpty(text) || !File.Exists(text))
				{
					return;
				}
			}
		}
		entries.Add(new ManifestTextureArrayEntry
		{
			Manifest = manifest,
			PageCount = num,
			LayerOffset = layerOffset,
			PagePaths = array
		});
		layerOffset += num;
	}

	private static string[] GetManifestVisualAtlasPagePaths(AdobeAnimateGlobalAtlasManifest manifest, int pageCount)
	{
		if (manifest == null || pageCount <= 0)
		{
			return System.Array.Empty<string>();
		}
		string[] generatedAtlasPagePaths = GetGeneratedAtlasPagePaths();
		if (generatedAtlasPagePaths.Length < pageCount)
		{
			return System.Array.Empty<string>();
		}
		return generatedAtlasPagePaths;
	}

	private static bool ContainsManifestEntry(List<ManifestTextureArrayEntry> entries, AdobeAnimateGlobalAtlasManifest manifest)
	{
		for (int i = 0; i < entries.Count; i++)
		{
			if (entries[i].Manifest == manifest)
			{
				return true;
			}
		}
		return false;
	}

	private static bool TryLoadAtlasArrayLayerImage(string pagePath, out Image layerImage)
	{
		layerImage = null;
		string text = ProjectSettings.GlobalizePath(pagePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return false;
		}
		Image image = Image.LoadFromFile(text);
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return false;
		}
		if (image.GetFormat() != Image.Format.Rgba8)
		{
			image.Convert(Image.Format.Rgba8);
		}
		if (image.GetWidth() == AtlasArrayLayerSize.X && image.GetHeight() == AtlasArrayLayerSize.Y)
		{
			layerImage = image;
			return true;
		}
		Image image2 = Image.CreateEmpty(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y, useMipmaps: false, Image.Format.Rgba8);
		image2.Fill(new Color(0f, 0f, 0f, 0f));
		int num = Math.Min(image.GetWidth(), AtlasArrayLayerSize.X);
		int num2 = Math.Min(image.GetHeight(), AtlasArrayLayerSize.Y);
		if (num > 0 && num2 > 0)
		{
			image2.BlitRect(image, new Rect2I(Vector2I.Zero, new Vector2I(num, num2)), Vector2I.Zero);
		}
		layerImage = image2;
		return true;
	}

	private static bool TryLoadPoseArrayLayerImage(string pagePath, out Image layerImage)
	{
		layerImage = null;
		string text = ProjectSettings.GlobalizePath(pagePath);
		Image image = ((!string.IsNullOrEmpty(text) && File.Exists(text)) ? Image.LoadFromFile(text) : null);
		if (image == null)
		{
			Texture2D texture2D = ResourceLoader.Load<Texture2D>(pagePath, "", ResourceLoader.CacheMode.Ignore);
			if (GodotObject.IsInstanceValid(texture2D))
			{
				image = texture2D.GetImage();
				texture2D.Dispose();
			}
		}
		if (image == null || image.GetWidth() <= 0 || image.GetHeight() <= 0)
		{
			return false;
		}
		if (image.GetFormat() != Image.Format.Rgbah)
		{
			image.Convert(Image.Format.Rgbah);
		}
		if (image.GetWidth() == 2048 && image.GetHeight() == 2048)
		{
			layerImage = image;
			return true;
		}
		Image image2 = Image.CreateEmpty(2048, 2048, useMipmaps: false, Image.Format.Rgbah);
		image2.Fill(new Color(0f, 0f, 0f, 0f));
		int num = Math.Min(image.GetWidth(), 2048);
		int num2 = Math.Min(image.GetHeight(), 2048);
		if (num > 0 && num2 > 0)
		{
			image2.BlitRect(image, new Rect2I(Vector2I.Zero, new Vector2I(num, num2)), Vector2I.Zero);
		}
		image.Dispose();
		layerImage = image2;
		return true;
	}

	private static AtlasPage AllocatePageSlot(List<AtlasPage> pages, Vector2I contentSize, out Rect2I paddedRect, out Vector2I contentPosition)
	{
		if (!CanFitAtlasPageContent(contentSize))
		{
			throw new InvalidOperationException($"Unable to allocate AdobeAnimate atlas region {contentSize}; maximum content size is {MaxAtlasContentSizeText()} for a {2048}x{2048} atlas page.");
		}
		for (int i = 0; i < pages.Count; i++)
		{
			if (pages[i].TryAllocate(contentSize, out paddedRect, out contentPosition))
			{
				return pages[i];
			}
		}
		AtlasPage atlasPage = CreatePage(contentSize);
		if (!atlasPage.TryAllocate(contentSize, out paddedRect, out contentPosition))
		{
			throw new InvalidOperationException($"Unable to allocate AdobeAnimate atlas region {contentSize}.");
		}
		atlasPage.Index = pages.Count;
		pages.Add(atlasPage);
		return atlasPage;
	}

	private static AtlasPage CreatePage(Vector2I minimumContentSize)
	{
		if (!CanFitAtlasPageContent(minimumContentSize))
		{
			throw new InvalidOperationException($"Unable to create AdobeAnimate atlas page for content {minimumContentSize}; maximum page size is {2048}x{2048}.");
		}
		return CreatePageWithSize(2048, 2048);
	}

	private static AtlasPage CreatePageWithSize(int width, int height)
	{
		Image image = Image.CreateEmpty(width, height, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		return new AtlasPage
		{
			Index = 0,
			Image = image,
			Width = width,
			Height = height,
			UsedWidth = 0,
			UsedHeight = 0,
			FreeRects = 
			{
				new Rect2I(Vector2I.Zero, new Vector2I(width, height))
			}
		};
	}

	private static Image CreateTrimmedPageImage(AtlasPage page)
	{
		int val = AlignToCompressionBlock(Math.Clamp(page.UsedWidth, 1, page.Width));
		int val2 = AlignToCompressionBlock(Math.Clamp(page.UsedHeight, 1, page.Height));
		val = Math.Min(val, page.Width);
		val2 = Math.Min(val2, page.Height);
		Image image = Image.CreateEmpty(val, val2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		image.BlitRect(page.Image, new Rect2I(Vector2I.Zero, new Vector2I(val, val2)), Vector2I.Zero);
		return image;
	}

	private static Image CreateFixedAtlasPageImage(AtlasPage page)
	{
		Image image = Image.CreateEmpty(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		int num = Math.Min(page.Image.GetWidth(), AtlasArrayLayerSize.X);
		int num2 = Math.Min(page.Image.GetHeight(), AtlasArrayLayerSize.Y);
		if (num > 0 && num2 > 0)
		{
			image.BlitRect(page.Image, new Rect2I(Vector2I.Zero, new Vector2I(num, num2)), Vector2I.Zero);
		}
		return image;
	}

	private static int AlignToCompressionBlock(int value)
	{
		return Math.Max(4, (value + 4 - 1) / 4 * 4);
	}

	private static int NextPowerOfTwo(int value)
	{
		int num;
		for (num = 1; num < value; num <<= 1)
		{
		}
		return num;
	}

	private static void PackMediaRectsIntoPages(List<AtlasPage> pages, Image sourceImage, Rect2[] sourceMediaRects, int[] mediaPages, Rect2[] remappedRects, string sourceKey, ref int warningCount)
	{
		Vector2I sourceSize = new Vector2I(sourceImage.GetWidth(), sourceImage.GetHeight());
		List<(int, Rect2, Rect2I)> list = new List<(int, Rect2, Rect2I)>(sourceMediaRects.Length);
		for (int i = 0; i < sourceMediaRects.Length; i++)
		{
			Rect2 rect = sourceMediaRects[i];
			if (!TryGetValidSourceRect(rect, sourceSize, out var sourceRect))
			{
				mediaPages[i] = 0;
				remappedRects[i] = default(Rect2);
			}
			else if (!CanFitAtlasPageContent(sourceRect.Size))
			{
				mediaPages[i] = 0;
				remappedRects[i] = default(Rect2);
				PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas media rect {sourceKey}#{i}: source rect {sourceRect.Size.X}x{sourceRect.Size.Y} exceeds {2048}x{2048} atlas page limit.");
			}
			else
			{
				list.Add((i, rect, sourceRect));
			}
		}
		SortValidRectsForPacking(list);
		for (int j = 0; j < list.Count; j++)
		{
			(int, Rect2, Rect2I) tuple = list[j];
			Rect2I item = tuple.Item3;
			Rect2I paddedRect;
			Vector2I contentPosition;
			AtlasPage atlasPage = AllocatePageSlot(pages, item.Size, out paddedRect, out contentPosition);
			BlitWithBorder(atlasPage.Image, sourceImage, item, contentPosition);
			mediaPages[tuple.Item1] = atlasPage.Index;
			remappedRects[tuple.Item1] = new Rect2(contentPosition, tuple.Item2.Size);
		}
	}

	private static void SortValidRectsForPacking(List<(int Index, Rect2 SourceRectFloat, Rect2I SourceRect)> validRects)
	{
		validRects.Sort(delegate((int Index, Rect2 SourceRectFloat, Rect2I SourceRect) a, (int Index, Rect2 SourceRectFloat, Rect2I SourceRect) b)
		{
			long value = (long)a.SourceRect.Size.X * (long)a.SourceRect.Size.Y;
			int num = ((long)b.SourceRect.Size.X * (long)b.SourceRect.Size.Y).CompareTo(value);
			if (num != 0)
			{
				return num;
			}
			int value2 = Math.Max(a.SourceRect.Size.X, a.SourceRect.Size.Y);
			int num2 = Math.Max(b.SourceRect.Size.X, b.SourceRect.Size.Y).CompareTo(value2);
			return (num2 == 0) ? a.Index.CompareTo(b.Index) : num2;
		});
	}

	private static bool TryPackMediaRectsAsSingleSourceGroup(List<AtlasPage> pages, Image sourceImage, List<(int Index, Rect2 SourceRectFloat, Rect2I SourceRect)> validRects, int[] mediaPages, Rect2[] remappedRects)
	{
		if (validRects.Count == 0)
		{
			return true;
		}
		long num = 0L;
		int val = 1;
		int val2 = 1;
		for (int i = 0; i < validRects.Count; i++)
		{
			Vector2I size = validRects[i].SourceRect.Size;
			int num2 = size.X + 2;
			int num3 = size.Y + 2;
			num += (long)num2 * (long)num3;
			val = Math.Max(val, num2);
			val2 = Math.Max(val2, num3);
		}
		int val3 = NextPowerOfTwo((int)Math.Ceiling(Math.Sqrt(Math.Max(1L, num))));
		int num4 = Math.Min(2048, NextPowerOfTwo(Math.Max(val, val3)));
		int num5 = Math.Min(2048, NextPowerOfTwo(Math.Max(val2, val3)));
		for (int j = 0; j < 12; j++)
		{
			if (num4 > 2048 || num5 > 2048)
			{
				return false;
			}
			AtlasPage atlasPage = CreatePageWithSize(num4, num5);
			Vector2I[] array = new Vector2I[validRects.Count];
			bool flag = true;
			Rect2I paddedRect;
			for (int k = 0; k < validRects.Count; k++)
			{
				(int, Rect2, Rect2I) tuple = validRects[k];
				if (!atlasPage.TryAllocate(tuple.Item3.Size, out paddedRect, out var contentPosition))
				{
					flag = false;
					break;
				}
				BlitWithBorder(atlasPage.Image, sourceImage, tuple.Item3, contentPosition);
				array[k] = contentPosition;
			}
			if (flag)
			{
				Image image = CreateTrimmedPageImage(atlasPage);
				Vector2I vector2I = new Vector2I(image.GetWidth(), image.GetHeight());
				if (!CanFitAtlasPageContent(vector2I))
				{
					return false;
				}
				Vector2I contentPosition2;
				AtlasPage atlasPage2 = AllocatePageSlot(pages, vector2I, out paddedRect, out contentPosition2);
				BlitWithBorder(atlasPage2.Image, image, new Rect2I(Vector2I.Zero, vector2I), contentPosition2);
				for (int l = 0; l < validRects.Count; l++)
				{
					(int, Rect2, Rect2I) tuple2 = validRects[l];
					mediaPages[tuple2.Item1] = atlasPage2.Index;
					remappedRects[tuple2.Item1] = new Rect2(contentPosition2 + array[l], tuple2.Item2.Size);
				}
				return true;
			}
			if (num4 <= num5)
			{
				num4 *= 2;
			}
			else
			{
				num5 *= 2;
			}
		}
		return false;
	}

	private static bool ValidateSourceAtlasImage(string sourceKey, Image sourceImage, Rect2[] sourceMediaRects, ref int warningCount)
	{
		Vector2I sourceSize = new Vector2I(sourceImage.GetWidth(), sourceImage.GetHeight());
		if (sourceSize.X <= 1 && sourceSize.Y <= 1 && HasNonFallbackMediaRect(sourceMediaRects))
		{
			PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas source {sourceKey}: embedded DAT atlas decoded to {sourceSize.X}x{sourceSize.Y}, but media rects require a real atlas image.");
			return false;
		}
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < sourceMediaRects.Length; i++)
		{
			if (TryGetValidSourceRect(sourceMediaRects[i], sourceSize, out var _))
			{
				num++;
			}
			else if (sourceMediaRects[i].Size.X > 0f || sourceMediaRects[i].Size.Y > 0f)
			{
				num2++;
			}
		}
		if (num == 0)
		{
			PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas source {sourceKey}: no media rect fits inside embedded atlas size {sourceSize.X}x{sourceSize.Y}.");
			return false;
		}
		if (num2 > 0)
		{
			PushLimitedRefreshWarning(ref warningCount, $"AdobeAnimate atlas source {sourceKey} has {num2} media rect(s) outside embedded atlas size {sourceSize.X}x{sourceSize.Y}; those rects will be left empty.");
		}
		return true;
	}

	private static bool HasNonFallbackMediaRect(Rect2[] sourceMediaRects)
	{
		for (int i = 0; i < sourceMediaRects.Length; i++)
		{
			Vector2 size = sourceMediaRects[i].Size;
			if (size.X > 1f || size.Y > 1f)
			{
				return true;
			}
		}
		return false;
	}

	private static void PushLimitedRefreshWarning(ref int warningCount, string message)
	{
		if (warningCount < 32)
		{
			GD.PushWarning(message);
		}
		else if (warningCount == 32)
		{
			GD.PushWarning("AdobeAnimate atlas refresh has more invalid source warnings; suppressing additional warnings for this run.");
		}
		warningCount++;
	}

	private static bool TryGetValidSourceRect(Rect2 rect, Vector2I sourceSize, out Rect2I sourceRect)
	{
		sourceRect = ToImageRect(rect);
		if (sourceRect.Size.X <= 0 || sourceRect.Size.Y <= 0)
		{
			return false;
		}
		if (sourceRect.Position.X < 0 || sourceRect.Position.Y < 0)
		{
			return false;
		}
		if (sourceRect.Position.X + sourceRect.Size.X > sourceSize.X)
		{
			return false;
		}
		if (sourceRect.Position.Y + sourceRect.Size.Y > sourceSize.Y)
		{
			return false;
		}
		return true;
	}

	private static Rect2I ToImageRect(Rect2 rect)
	{
		int x = Mathf.FloorToInt(rect.Position.X);
		int y = Mathf.FloorToInt(rect.Position.Y);
		int width = Mathf.CeilToInt(rect.Size.X);
		int height = Mathf.CeilToInt(rect.Size.Y);
		return new Rect2I(x, y, width, height);
	}

	private static bool CanFitAtlasPageContent(Vector2I contentSize)
	{
		if (contentSize.X <= 0 || contentSize.Y <= 0)
		{
			return false;
		}
		if (contentSize.X + 2 <= 2048)
		{
			return contentSize.Y + 2 <= 2048;
		}
		return false;
	}

	private static string MaxAtlasContentSizeText()
	{
		int value = 2046;
		return $"{value}x{value}";
	}

	private static void BlitWithBorder(Image target, Image source, Rect2I sourceRect, Vector2I contentPosition)
	{
		Vector2I size = sourceRect.Size;
		target.BlitRect(source, sourceRect, contentPosition);
		target.BlitRect(source, new Rect2I(sourceRect.Position.X, sourceRect.Position.Y, size.X, 1), contentPosition + new Vector2I(0, -1));
		target.BlitRect(source, new Rect2I(sourceRect.Position.X, sourceRect.Position.Y + size.Y - 1, size.X, 1), contentPosition + new Vector2I(0, size.Y));
		target.BlitRect(source, new Rect2I(sourceRect.Position.X, sourceRect.Position.Y, 1, size.Y), contentPosition + new Vector2I(-1, 0));
		target.BlitRect(source, new Rect2I(sourceRect.Position.X + size.X - 1, sourceRect.Position.Y, 1, size.Y), contentPosition + new Vector2I(size.X, 0));
		target.SetPixel(contentPosition.X - 1, contentPosition.Y - 1, source.GetPixel(sourceRect.Position.X, sourceRect.Position.Y));
		target.SetPixel(contentPosition.X + size.X, contentPosition.Y - 1, source.GetPixel(sourceRect.Position.X + size.X - 1, sourceRect.Position.Y));
		target.SetPixel(contentPosition.X - 1, contentPosition.Y + size.Y, source.GetPixel(sourceRect.Position.X, sourceRect.Position.Y + size.Y - 1));
		target.SetPixel(contentPosition.X + size.X, contentPosition.Y + size.Y, source.GetPixel(sourceRect.Position.X + size.X - 1, sourceRect.Position.Y + size.Y - 1));
	}

	private static void PackExternalTexturesIntoPages(List<AtlasPage> pages, List<string> texturePaths, AdobeAnimateGlobalAtlasManifest manifest, ref int warningCount)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		List<(string, Image, Vector2I)> list = new List<(string, Image, Vector2I)>();
		for (int i = 0; i < texturePaths.Count; i++)
		{
			string text = NormalizeProjectPath(texturePaths[i]);
			if (string.IsNullOrEmpty(text) || !hashSet.Add(text))
			{
				continue;
			}
			if (!TryLoadExternalTextureImage(text, out var image, out var size))
			{
				PushLimitedRefreshWarning(ref warningCount, "Skip AdobeAnimate external atlas texture " + text + ": image file is missing or cannot be decoded.");
				continue;
			}
			if (image.GetFormat() != Image.Format.Rgba8)
			{
				image.Convert(Image.Format.Rgba8);
			}
			if (!CanFitAtlasPageContent(size))
			{
				PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate external atlas texture {text}: texture {size.X}x{size.Y} exceeds {2048}x{2048} atlas page limit.");
			}
			else
			{
				list.Add((text, image, size));
			}
		}
		list.Sort(delegate((string Path, Image Image, Vector2I Size) a, (string Path, Image Image, Vector2I Size) b)
		{
			long value = (long)a.Size.X * (long)a.Size.Y;
			int num2 = ((long)b.Size.X * (long)b.Size.Y).CompareTo(value);
			if (num2 != 0)
			{
				return num2;
			}
			int value2 = Math.Max(a.Size.X, a.Size.Y);
			int num3 = Math.Max(b.Size.X, b.Size.Y).CompareTo(value2);
			return (num3 == 0) ? string.Compare(a.Path, b.Path, StringComparison.OrdinalIgnoreCase) : num3;
		});
		for (int num = 0; num < list.Count; num++)
		{
			(string, Image, Vector2I) tuple = list[num];
			Rect2I paddedRect;
			Vector2I contentPosition;
			AtlasPage atlasPage = AllocatePageSlot(pages, tuple.Item3, out paddedRect, out contentPosition);
			BlitWithBorder(atlasPage.Image, tuple.Item2, new Rect2I(Vector2I.Zero, tuple.Item3), contentPosition);
			manifest.ExternalTexturePaths.Add(tuple.Item1);
			manifest.ExternalTextureAtlasPages.Add(atlasPage.Index);
			manifest.ExternalTextureRects.Add(new Rect2(contentPosition, tuple.Item3));
		}
	}

	private static bool TryPackExternalTexturesAsSingleAtlasGroup(List<AtlasPage> pages, List<(string Path, Image Image, Vector2I Size)> loadedTextures, AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (loadedTextures == null || loadedTextures.Count == 0)
		{
			return true;
		}
		long num = 0L;
		int val = 1;
		int val2 = 1;
		for (int i = 0; i < loadedTextures.Count; i++)
		{
			Vector2I item = loadedTextures[i].Size;
			int num2 = item.X + 2;
			int num3 = item.Y + 2;
			num += (long)num2 * (long)num3;
			val = Math.Max(val, num2);
			val2 = Math.Max(val2, num3);
		}
		int val3 = NextPowerOfTwo((int)Math.Ceiling(Math.Sqrt(Math.Max(1L, num))));
		int num4 = Math.Min(2048, NextPowerOfTwo(Math.Max(val, val3)));
		int num5 = Math.Min(2048, NextPowerOfTwo(Math.Max(val2, val3)));
		for (int j = 0; j < 12; j++)
		{
			if (num4 > 2048 || num5 > 2048)
			{
				return false;
			}
			AtlasPage atlasPage = CreatePageWithSize(num4, num5);
			Vector2I[] array = new Vector2I[loadedTextures.Count];
			bool flag = true;
			Rect2I paddedRect;
			for (int k = 0; k < loadedTextures.Count; k++)
			{
				(string, Image, Vector2I) tuple = loadedTextures[k];
				if (!atlasPage.TryAllocate(tuple.Item3, out paddedRect, out var contentPosition))
				{
					flag = false;
					break;
				}
				BlitWithBorder(atlasPage.Image, tuple.Item2, new Rect2I(Vector2I.Zero, tuple.Item3), contentPosition);
				array[k] = contentPosition;
			}
			if (flag)
			{
				Image image = CreateTrimmedPageImage(atlasPage);
				Vector2I vector2I = new Vector2I(image.GetWidth(), image.GetHeight());
				if (!CanFitAtlasPageContent(vector2I))
				{
					return false;
				}
				Vector2I contentPosition2;
				AtlasPage atlasPage2 = AllocatePageSlot(pages, vector2I, out paddedRect, out contentPosition2);
				BlitWithBorder(atlasPage2.Image, image, new Rect2I(Vector2I.Zero, vector2I), contentPosition2);
				for (int l = 0; l < loadedTextures.Count; l++)
				{
					(string, Image, Vector2I) tuple2 = loadedTextures[l];
					manifest.ExternalTexturePaths.Add(tuple2.Item1);
					manifest.ExternalTextureAtlasPages.Add(atlasPage2.Index);
					manifest.ExternalTextureRects.Add(new Rect2(contentPosition2 + array[l], tuple2.Item3));
				}
				return true;
			}
			if (num4 <= num5)
			{
				num4 *= 2;
			}
			else
			{
				num5 *= 2;
			}
		}
		return false;
	}

	private static bool TryLoadExternalTextureImage(string resourcePath, out Image image, out Vector2I size)
	{
		image = null;
		size = Vector2I.Zero;
		string text = ProjectSettings.GlobalizePath(resourcePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return false;
		}
		image = Image.LoadFromFile(text);
		if (image == null)
		{
			return false;
		}
		size = new Vector2I(image.GetWidth(), image.GetHeight());
		if (size.X > 0)
		{
			return size.Y > 0;
		}
		return false;
	}

	private static Rect2 RemapExternalTextureRect(Rect2 atlasRect, Rect2 localSourceRect, Vector2 textureSize)
	{
		Rect2 rect = localSourceRect;
		if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
		{
			rect = new Rect2(Vector2.Zero, textureSize);
		}
		float num = Mathf.Clamp(rect.Position.X, 0f, textureSize.X);
		float num2 = Mathf.Clamp(rect.Position.Y, 0f, textureSize.Y);
		float num3 = Mathf.Clamp(rect.Position.X + rect.Size.X, num, textureSize.X);
		float num4 = Mathf.Clamp(rect.Position.Y + rect.Size.Y, num2, textureSize.Y);
		return new Rect2(atlasRect.Position + new Vector2(num, num2), new Vector2(num3 - num, num4 - num2));
	}

	private static Rect2[] BuildSourceMediaRects(AdobeAnimateData data)
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
		int num = data.mediaDictionary?.Count ?? 0;
		Rect2[] array2 = new Rect2[num];
		for (int j = 0; j < num; j++)
		{
			array2[j] = data.GetMediaRect(j);
		}
		return array2;
	}

	private static ulong ComputeSourceMediaSignature(Rect2[] sourceMediaRects)
	{
		ulong hash = 1469598103934665603uL;
		HashAddInt(ref hash, (sourceMediaRects != null) ? sourceMediaRects.Length : 0);
		if (sourceMediaRects != null)
		{
			for (int i = 0; i < sourceMediaRects.Length; i++)
			{
				Rect2 rect = sourceMediaRects[i];
				HashAddFloat(ref hash, rect.Position.X);
				HashAddFloat(ref hash, rect.Position.Y);
				HashAddFloat(ref hash, rect.Size.X);
				HashAddFloat(ref hash, rect.Size.Y);
			}
		}
		return hash;
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

	public static TextureLayered PreloadVisualTextureArray()
	{
		lock (CacheLock)
		{
			AdobeAnimateGlobalAtlasManifest manifest = GetManifest();
			int manifestAtlasPageCount = GetManifestAtlasPageCount(manifest);
			if (!TryGetManifestTextureArray(manifest, manifestAtlasPageCount, out var textureArray, out var textureArrayRid, out var _))
			{
				return null;
			}
			return (textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray)) ? textureArray : null;
		}
	}

	public static TextureLayered PreloadVisualTextureArray(AdobeAnimateAtlasProfile atlasProfile)
	{
		if (atlasProfile == null)
		{
			return PreloadVisualTextureArray();
		}
		lock (CacheLock)
		{
			AdobeAnimateGlobalAtlasManifest manifest = atlasProfile.LoadManifest();
			int manifestAtlasPageCount = GetManifestAtlasPageCount(manifest);
			if (!TryGetProfileManifestTextureArray(manifest, manifestAtlasPageCount, out var textureArray, out var textureArrayRid, out var _))
			{
				return null;
			}
			return (textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray)) ? textureArray : null;
		}
	}

	public static TextureLayered PreloadGpuPoseTextureArray()
	{
		lock (CacheLock)
		{
			if (!TryGetManifestPoseTextureArray(GetManifest(), out var textureArray, out var textureArrayRid, out var _))
			{
				return null;
			}
			return (textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray)) ? textureArray : null;
		}
	}

	public static TextureLayered PreloadGpuPoseTextureArray(AdobeAnimateAtlasProfile atlasProfile)
	{
		if (atlasProfile == null)
		{
			return PreloadGpuPoseTextureArray();
		}
		lock (CacheLock)
		{
			if (!TryGetManifestPoseTextureArray(atlasProfile.LoadManifest(), out var textureArray, out var textureArrayRid, out var _))
			{
				return null;
			}
			return (textureArrayRid.IsValid && GodotObject.IsInstanceValid(textureArray)) ? textureArray : null;
		}
	}

	public static bool TryGetOrBuild(AdobeAnimateData data, Rect2[] sourceMediaRects, out AdobeAnimateAtlasAllocation allocation)
	{
		lock (CacheLock)
		{
			allocation = null;
			if (data == null || sourceMediaRects == null)
			{
				return false;
			}
			string atlasSourceKey = data.GetAtlasSourceKey();
			ulong num = ComputeSourceMediaSignature(sourceMediaRects);
			string allocationCacheKey = GetAllocationCacheKey(atlasSourceKey, num);
			if (Allocations.TryGetValue(allocationCacheKey, out allocation) && IsAtlasAllocationValid(allocation))
			{
				return true;
			}
			if (TryGetManifestAllocation(atlasSourceKey, sourceMediaRects.Length, num, out allocation))
			{
				Allocations[allocationCacheKey] = allocation;
				return true;
			}
			if (TryBuildStandaloneAllocation(data, sourceMediaRects, out allocation))
			{
				Allocations[allocationCacheKey] = allocation;
				return true;
			}
			WarnProjectAtlasUnavailable(atlasSourceKey);
			return false;
		}
	}

	public static bool TryGetOrBuild(AdobeAnimateData data, Rect2[] sourceMediaRects, AdobeAnimateAtlasProfile atlasProfile, out AdobeAnimateAtlasAllocation allocation)
	{
		lock (CacheLock)
		{
			allocation = null;
			if (data == null || sourceMediaRects == null || atlasProfile == null)
			{
				return false;
			}
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = atlasProfile.LoadManifest();
			if (adobeAnimateGlobalAtlasManifest == null)
			{
				GD.PushWarning($"AdobeAnimate atlas profile '{atlasProfile.GetStableCacheKey()}' cannot load manifest '{atlasProfile.ManifestPath}'.");
				return false;
			}
			string atlasSourceKey = data.GetAtlasSourceKey();
			ulong num = ComputeSourceMediaSignature(sourceMediaRects);
			string key = "profile=" + atlasProfile.GetStableCacheKey() + "|" + GetAllocationCacheKey(atlasSourceKey, num);
			if (Allocations.TryGetValue(key, out allocation) && IsAtlasAllocationValid(allocation))
			{
				return true;
			}
			if (!TryGetManifestAllocation(adobeAnimateGlobalAtlasManifest, atlasSourceKey, sourceMediaRects.Length, num, out allocation, isolatedTextureArray: true))
			{
				GD.PushWarning($"AdobeAnimate atlas profile '{atlasProfile.GetStableCacheKey()}' does not contain a valid allocation for {atlasSourceKey}.");
				return false;
			}
			Allocations[key] = allocation;
			return true;
		}
	}

	private static string GetAllocationCacheKey(string sourceKey, ulong sourceSignature)
	{
		return $"{sourceKey}|{sourceSignature:X16}";
	}

	private static bool IsAtlasAllocationValid(AdobeAnimateAtlasAllocation allocation)
	{
		if (allocation == null)
		{
			return false;
		}
		if (allocation.UsesTextureArrayLayout)
		{
			if (allocation.TextureArrayRid.IsValid && GodotObject.IsInstanceValid(allocation.TextureArray) && allocation.TextureArraySize.X > 0f)
			{
				return allocation.TextureArraySize.Y > 0f;
			}
			return false;
		}
		if (allocation.TextureRid.IsValid)
		{
			return GodotObject.IsInstanceValid(allocation.Texture);
		}
		return false;
	}

	private static bool TryBuildStandaloneAllocation(AdobeAnimateData data, Rect2[] sourceMediaRects, out AdobeAnimateAtlasAllocation allocation)
	{
		allocation = null;
		if (data == null || sourceMediaRects == null || sourceMediaRects.Length == 0 || !data.TryReadEmbeddedAtlasImage(out var image, out var imageAtlasSize) || image == null || image.IsEmpty() || imageAtlasSize.X <= 0 || imageAtlasSize.Y <= 0)
		{
			return false;
		}
		bool flag = false;
		for (int i = 0; i < sourceMediaRects.Length; i++)
		{
			if (TryGetValidSourceRect(sourceMediaRects[i], imageAtlasSize, out var _))
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return false;
		}
		Array<Image> array = new Array<Image> { image };
		Texture2DArray texture2DArray = new Texture2DArray();
		ImageTexture imageTexture;
		try
		{
			if (texture2DArray.CreateFromImages(array) != Error.Ok || !texture2DArray.GetRid().IsValid)
			{
				texture2DArray.Dispose();
				return false;
			}
			imageTexture = ImageTexture.CreateFromImage(image);
			if (!GodotObject.IsInstanceValid(imageTexture))
			{
				texture2DArray.Dispose();
				return false;
			}
		}
		finally
		{
			array.Clear();
			image.Dispose();
		}
		allocation = new AdobeAnimateAtlasAllocation
		{
			Texture = imageTexture,
			TextureRid = imageTexture.GetRid(),
			TextureArray = texture2DArray,
			TextureArrayRid = texture2DArray.GetRid(),
			TextureArraySize = new Vector2(imageAtlasSize.X, imageAtlasSize.Y),
			UsesTextureArrayLayout = true,
			Textures = new Texture2D[1] { imageTexture },
			TextureRids = new Rid[1] { imageTexture.GetRid() },
			MediaAtlasPages = new int[sourceMediaRects.Length],
			MediaRects = (Rect2[])sourceMediaRects.Clone(),
			PageIndex = 0,
			PageRect = new Rect2I(Vector2I.Zero, imageAtlasSize),
			SourceSize = imageAtlasSize
		};
		return true;
	}

	public static bool TryGetExternalTextureAllocation(Texture2D texture, Rect2 localSourceRect, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		return TryGetExternalTextureAllocation(texture, localSourceRect, ExternalTextureAtlasPreference.Any, out allocation);
	}

	public static bool TryGetReplaceTextureAllocation(Texture2D texture, Rect2 localSourceRect, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		return TryGetExternalTextureAllocation(texture, localSourceRect, ExternalTextureAtlasPreference.Replace, out allocation);
	}

	public static bool TryGetReplaceTextureAllocation(string textureReference, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		string text = NormalizeResourceReferencePath(textureReference);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		lock (CacheLock)
		{
			string key = $"{ExternalTextureAtlasPreference.Replace}:{text}";
			if (ExternalTextureAllocations.TryGetValue(key, out allocation) && IsExternalTextureAllocationValid(allocation))
			{
				return true;
			}
			if (!TryGetManifestExternalTextureAllocation(text, ExternalTextureAtlasPreference.Replace, out allocation))
			{
				WarnProjectAtlasUnavailable(text);
				return false;
			}
			ExternalTextureAllocations[key] = allocation;
			return true;
		}
	}

	private static bool TryGetExternalTextureAllocation(Texture2D texture, Rect2 localSourceRect, ExternalTextureAtlasPreference preference, out AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimateExternalTextureAtlasAllocation);
		if (!GodotObject.IsInstanceValid(texture))
		{
			return false;
		}
		Rid rid = texture.GetRid();
		RuntimeExternalTextureKey key = new RuntimeExternalTextureKey(texture.GetInstanceId(), preference);
		lock (CacheLock)
		{
			AdobeAnimateExternalTextureAtlasAllocation value2;
			if (RuntimeExternalTextureAllocations.TryGetValue(key, out var value) && value.SourceRid == rid && IsExternalTextureAllocationValid(value.Allocation))
			{
				value2 = value.Allocation;
			}
			else
			{
				string text = NormalizeProjectPath(texture.ResourcePath);
				if (string.IsNullOrEmpty(text))
				{
					return false;
				}
				string key2 = $"{preference}:{text}";
				if ((!ExternalTextureAllocations.TryGetValue(key2, out value2) || !IsExternalTextureAllocationValid(value2)) && !TryGetManifestExternalTextureAllocation(text, preference, out value2))
				{
					WarnProjectAtlasUnavailable(text);
					return false;
				}
				ExternalTextureAllocations[key2] = value2;
				RuntimeExternalTextureAllocations[key] = new RuntimeExternalTextureEntry(rid, in value2);
			}
			Rect2 rect = RemapExternalTextureRect(value2.Rect, localSourceRect, texture.GetSize());
			if (rect.Size.X <= 0f || rect.Size.Y <= 0f)
			{
				return false;
			}
			allocation = new AdobeAnimateExternalTextureAtlasAllocation(value2.Texture, value2.TextureRid, value2.AtlasPage, rect, value2.TextureArray, value2.TextureArrayRid, value2.TextureArraySize, value2.TextureArrayLayerOffset, value2.UsesTextureArray);
			return true;
		}
	}

	private static bool IsExternalTextureAllocationValid(AdobeAnimateExternalTextureAtlasAllocation allocation)
	{
		if (allocation.UsesTextureArray)
		{
			if (allocation.TextureArrayRid.IsValid)
			{
				return GodotObject.IsInstanceValid(allocation.TextureArray);
			}
			return false;
		}
		if (allocation.TextureRid.IsValid)
		{
			return GodotObject.IsInstanceValid(allocation.Texture);
		}
		return false;
	}

	private static bool IsPoseAtlasAllocationValid(AdobeAnimatePoseAtlasAllocation allocation)
	{
		if (allocation.UsesTextureArray)
		{
			if (allocation.TextureArrayRid.IsValid)
			{
				return GodotObject.IsInstanceValid(allocation.TextureArray);
			}
			return false;
		}
		if (allocation.TextureRid.IsValid)
		{
			return GodotObject.IsInstanceValid(allocation.Texture);
		}
		return false;
	}

	public static bool TryGetPoseAtlasAllocation(string sourceKey, ulong expectedSignature, int expectedTexelCount, out AdobeAnimatePoseAtlasAllocation allocation)
	{
		return TryGetPoseAtlasAllocation(null, sourceKey, expectedSignature, expectedTexelCount, out allocation);
	}

	public static bool TryGetPoseAtlasAllocation(AdobeAnimateAtlasProfile atlasProfile, string sourceKey, ulong expectedSignature, int expectedTexelCount, out AdobeAnimatePoseAtlasAllocation allocation)
	{
		allocation = default(AdobeAnimatePoseAtlasAllocation);
		if (string.IsNullOrEmpty(sourceKey) || expectedSignature == 0L || expectedTexelCount <= 0)
		{
			TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.allocationMiss.invalidInput");
			return false;
		}
		lock (CacheLock)
		{
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = ((atlasProfile == null) ? GetManifest() : atlasProfile.LoadManifest());
			if (adobeAnimateGlobalAtlasManifest == null)
			{
				return false;
			}
			string value = atlasProfile?.GetStableCacheKey() ?? "gameplay";
			string key = $"{value}|{sourceKey}|{expectedSignature:X16}|{expectedTexelCount}";
			if (PoseAtlasAllocations.TryGetValue(key, out allocation) && IsPoseAtlasAllocationValid(allocation))
			{
				return true;
			}
			if (!TryGetManifestPoseAtlasAllocation(adobeAnimateGlobalAtlasManifest, sourceKey, expectedSignature, expectedTexelCount, out allocation))
			{
				TowerDefensePerfProfiler.Sample("adobeAnimate.poseAtlas.allocationMiss.manifest");
				return false;
			}
			PoseAtlasAllocations[key] = allocation;
			return true;
		}
	}

	public static bool TryGetPoseAtlasManifestEntry(string sourceKey, out ulong signature, out int texelCount)
	{
		return TryGetPoseAtlasManifestEntry(null, sourceKey, out signature, out texelCount);
	}

	public static bool TryGetPoseAtlasManifestEntry(AdobeAnimateAtlasProfile atlasProfile, string sourceKey, out ulong signature, out int texelCount)
	{
		signature = 0uL;
		texelCount = 0;
		if (string.IsNullOrEmpty(sourceKey))
		{
			return false;
		}
		lock (CacheLock)
		{
			AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = ((atlasProfile == null) ? GetManifest() : atlasProfile.LoadManifest());
			if (adobeAnimateGlobalAtlasManifest?.SourceKeys == null || adobeAnimateGlobalAtlasManifest.SourcePoseSignatures == null || adobeAnimateGlobalAtlasManifest.SourcePoseTexelCounts == null)
			{
				return false;
			}
			for (int i = 0; i < adobeAnimateGlobalAtlasManifest.SourceKeys.Count; i++)
			{
				if (!(adobeAnimateGlobalAtlasManifest.SourceKeys[i] != sourceKey))
				{
					if (i >= adobeAnimateGlobalAtlasManifest.SourcePoseSignatures.Count || i >= adobeAnimateGlobalAtlasManifest.SourcePoseTexelCounts.Count || !TryParseSignature(adobeAnimateGlobalAtlasManifest.SourcePoseSignatures[i], out signature))
					{
						signature = 0uL;
						return false;
					}
					texelCount = adobeAnimateGlobalAtlasManifest.SourcePoseTexelCounts[i];
					return signature != 0L && texelCount > 0;
				}
			}
		}
		return false;
	}

	public static bool RefreshProjectAtlas(string searchRoot = "res://Asset/Anime", bool bakePoseTextures = true)
	{
		List<string> list = FindProjectileRegistryScenePaths();
		List<string> list2 = FindEffectScenePaths();
		List<AdobeAnimateData> list3 = FindUnifiedProjectAtlasResources(searchRoot, list, list2);
		List<string> list4 = FindUnifiedProjectExternalTextureResources(searchRoot, list3, list, list2);
		GD.Print($"[AdobeAnimate Atlas] unified scan animeData={list3.Count}, externalTextures={list4.Count}, projectileScenes={list.Count}, effectScenes={list2.Count}");
		if (RefreshAtlas(list3, list4, "res://addons/AdobeAnimateEditor/GeneratedAtlas", "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", "AdobeAnimateGlobalAtlasPage_", "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData", bakePoseTextures: false, "[AdobeAnimate Atlas]"))
		{
			if (bakePoseTextures)
			{
				return RefreshProjectPoseData(searchRoot);
			}
			return true;
		}
		return false;
	}

	public static bool RefreshBootstrapAtlas(bool bakePoseTextures = true)
	{
		if (!RefreshAtlas(LoadBootstrapAnimeDataResources(), new List<string>(), "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap", "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres", "AdobeAnimateBootstrapAtlasPage_", "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AnimationData", bakePoseTextures: false, "[AdobeAnimate Bootstrap Atlas]"))
		{
			return false;
		}
		if (RefreshBootstrapVisualTextureArray() <= 0)
		{
			return false;
		}
		if (bakePoseTextures)
		{
			return RefreshBootstrapPoseData();
		}
		return true;
	}

	private static List<AdobeAnimateData> LoadBootstrapAnimeDataResources()
	{
		List<AdobeAnimateData> list = new List<AdobeAnimateData>(BootstrapAnimeDataPaths.Length);
		for (int i = 0; i < BootstrapAnimeDataPaths.Length; i++)
		{
			AdobeAnimateData adobeAnimateData = ResourceLoader.Load<AdobeAnimateData>(BootstrapAnimeDataPaths[i], null, ResourceLoader.CacheMode.Reuse);
			if (adobeAnimateData == null)
			{
				GD.PushWarning("AdobeAnimate bootstrap source is unavailable: " + BootstrapAnimeDataPaths[i]);
			}
			else
			{
				list.Add(adobeAnimateData);
			}
		}
		return list;
	}

	private static bool RefreshAtlas(List<AdobeAnimateData> resources, List<string> externalTexturePaths, string generatedAtlasDir, string manifestPath, string pagePrefix, string generatedGpuPoseTextureDir, bool bakePoseTextures, string logPrefix)
	{
		if (resources == null)
		{
			resources = new List<AdobeAnimateData>();
		}
		if (externalTexturePaths == null)
		{
			externalTexturePaths = new List<string>();
		}
		if (resources.Count == 0 && externalTexturePaths.Count == 0)
		{
			return false;
		}
		List<AtlasPage> list = new List<AtlasPage>();
		AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = new AdobeAnimateGlobalAtlasManifest();
		HashSet<string> hashSet = new HashSet<string>();
		int warningCount = 0;
		foreach (AdobeAnimateData resource in resources)
		{
			if (resource == null)
			{
				continue;
			}
			string resolvedAnimeFilePath = resource.GetResolvedAnimeFilePath();
			if ((resource.mediaRects == null || resource.mediaRects.Length == 0) && !resource.EnsurePackedRuntimeData())
			{
				PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas source {GetResourceDebugPath(resource)}: cannot build packed runtime data from animeFile '{resolvedAnimeFilePath}'.");
				continue;
			}
			string atlasSourceKey = resource.GetAtlasSourceKey();
			if (!hashSet.Add(atlasSourceKey))
			{
				continue;
			}
			Rect2[] array = BuildSourceMediaRects(resource);
			if (array.Length == 0)
			{
				continue;
			}
			if (!resource.TryReadEmbeddedAtlasImage(out var image, out var imageAtlasSize))
			{
				PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas source {GetResourceDebugPath(resource)}: animeFile '{resolvedAnimeFilePath}' is missing or cannot be read.");
				continue;
			}
			if (image == null || imageAtlasSize.X <= 0 || imageAtlasSize.Y <= 0)
			{
				PushLimitedRefreshWarning(ref warningCount, $"Skip AdobeAnimate atlas source {GetResourceDebugPath(resource)}: animeFile '{resolvedAnimeFilePath}' produced an empty atlas image.");
				continue;
			}
			if (image.GetFormat() != Image.Format.Rgba8)
			{
				image.Convert(Image.Format.Rgba8);
			}
			if (ValidateSourceAtlasImage(atlasSourceKey, image, array, ref warningCount))
			{
				int count = adobeAnimateGlobalAtlasManifest.MediaRects.Count;
				int[] array2 = new int[array.Length];
				Rect2[] array3 = new Rect2[array.Length];
				PackMediaRectsIntoPages(list, image, array, array2, array3, atlasSourceKey, ref warningCount);
				for (int i = 0; i < array.Length; i++)
				{
					adobeAnimateGlobalAtlasManifest.MediaAtlasPages.Add(array2[i]);
					adobeAnimateGlobalAtlasManifest.MediaRects.Add(array3[i]);
				}
				adobeAnimateGlobalAtlasManifest.SourceKeys.Add(atlasSourceKey);
				adobeAnimateGlobalAtlasManifest.SourceStarts.Add(count);
				adobeAnimateGlobalAtlasManifest.SourceCounts.Add(array.Length);
				adobeAnimateGlobalAtlasManifest.SourceSignatures.Add(ComputeSourceMediaSignature(array).ToString("X16", CultureInfo.InvariantCulture));
			}
		}
		PackExternalTexturesIntoPages(list, externalTexturePaths, adobeAnimateGlobalAtlasManifest, ref warningCount);
		if (adobeAnimateGlobalAtlasManifest.SourceKeys.Count == 0 && adobeAnimateGlobalAtlasManifest.ExternalTexturePaths.Count == 0)
		{
			return false;
		}
		PrepareGeneratedAtlasDirectory(generatedAtlasDir, pagePrefix);
		long num = 0L;
		long num2 = 0L;
		for (int j = 0; j < list.Count; j++)
		{
			AtlasPage atlasPage = list[j];
			string pagePath = GetPagePath(generatedAtlasDir, pagePrefix, j);
			string path = ProjectSettings.GlobalizePath(pagePath);
			Image image2 = CreateFixedAtlasPageImage(atlasPage);
			num += (long)atlasPage.Width * (long)atlasPage.Height;
			num2 += (long)image2.GetWidth() * (long)image2.GetHeight();
			Error error = image2.SavePng(path);
			if (error != Error.Ok)
			{
				GD.PushError($"Failed to save AdobeAnimate atlas page image {pagePath}: {error}");
			}
			else
			{
				WriteTextureImportPreset(pagePath);
			}
		}
		Error error2 = ResourceSaver.Save(adobeAnimateGlobalAtlasManifest, manifestPath, ResourceSaver.SaverFlags.None);
		if (error2 != Error.Ok)
		{
			GD.PushError($"Failed to save AdobeAnimate atlas manifest {manifestPath}: {error2}");
			return false;
		}
		int skipped = 0;
		int value = 0;
		if (bakePoseTextures)
		{
			Clear();
			value = AdobeAnimateDefinitionCache.RefreshBakedGpuPoseTextures(resources, adobeAnimateGlobalAtlasManifest, generatedGpuPoseTextureDir, out skipped);
			error2 = ResourceSaver.Save(adobeAnimateGlobalAtlasManifest, manifestPath, ResourceSaver.SaverFlags.None);
			if (error2 != Error.Ok)
			{
				GD.PushError($"Failed to save AdobeAnimate atlas manifest with pose atlas data {manifestPath}: {error2}");
				return false;
			}
		}
		Clear();
		float value2 = ((num > 0) ? ((float)num2 / (float)num) : 0f);
		string value3 = (bakePoseTextures ? $"bakedPoseTextures={value}, skippedPoseTextures={skipped}" : "poseBake=deferred");
		GD.Print($"{logPrefix} packed sources={adobeAnimateGlobalAtlasManifest.SourceKeys.Count}, externalTextures={adobeAnimateGlobalAtlasManifest.ExternalTexturePaths.Count}, pages={list.Count}, {value3}, savedPixels={num2}/{num} ({value2:P1})");
		return true;
	}

	public static int RefreshSharedVisualTextureArray()
	{
		Clear();
		List<ManifestTextureArrayEntry> list = BuildSharedVisualTextureArrayEntries(null, 0, requireImages: true);
		if (list.Count == 0)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < list.Count; i++)
		{
			num += Math.Max(0, list[i].PageCount);
		}
		if (num <= 0)
		{
			return 0;
		}
		int visualTextureArrayColumnCount = GetVisualTextureArrayColumnCount(num);
		int num2 = Math.Max(1, (num + visualTextureArrayColumnCount - 1) / visualTextureArrayColumnCount);
		Image image = Image.CreateEmpty(AtlasArrayLayerSize.X * visualTextureArrayColumnCount, AtlasArrayLayerSize.Y * num2, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		for (int j = 0; j < list.Count; j++)
		{
			ManifestTextureArrayEntry manifestTextureArrayEntry = list[j];
			for (int k = 0; k < manifestTextureArrayEntry.PageCount; k++)
			{
				int num3 = manifestTextureArrayEntry.LayerOffset + k;
				if (!TryLoadAtlasArrayLayerImage(manifestTextureArrayEntry.PagePaths[k], out var layerImage))
				{
					return 0;
				}
				int num4 = num3 % visualTextureArrayColumnCount;
				int num5 = num3 / visualTextureArrayColumnCount;
				image.BlitRect(layerImage, new Rect2I(Vector2I.Zero, AtlasArrayLayerSize), new Vector2I(num4 * AtlasArrayLayerSize.X, num5 * AtlasArrayLayerSize.Y));
			}
		}
		string path = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png");
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		Error error = image.SavePng(path);
		if (error != Error.Ok)
		{
			GD.PushError($"Failed to save AdobeAnimate visual Texture2DArray source {"res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png"}: {error}");
			return 0;
		}
		WriteTextureArrayImportPreset("res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png", visualTextureArrayColumnCount, num2);
		for (int l = 0; l < list.Count; l++)
		{
			ManifestTextureArrayEntry manifestTextureArrayEntry2 = list[l];
			manifestTextureArrayEntry2.Manifest.AtlasTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png";
			manifestTextureArrayEntry2.Manifest.AtlasTextureArrayLayerSize = new Vector2(AtlasArrayLayerSize.X, AtlasArrayLayerSize.Y);
			manifestTextureArrayEntry2.Manifest.AtlasTextureArrayLayerOffset = manifestTextureArrayEntry2.LayerOffset;
			manifestTextureArrayEntry2.Manifest.AtlasTextureArrayLayerCount = manifestTextureArrayEntry2.PageCount;
			manifestTextureArrayEntry2.Manifest.AtlasTextureArrayColumns = visualTextureArrayColumnCount;
			string manifestResourcePath = GetManifestResourcePath(manifestTextureArrayEntry2.Manifest);
			if (!string.IsNullOrEmpty(manifestResourcePath))
			{
				Error error2 = ResourceSaver.Save(manifestTextureArrayEntry2.Manifest, manifestResourcePath, ResourceSaver.SaverFlags.None);
				if (error2 != Error.Ok)
				{
					GD.PushWarning($"Failed to save AdobeAnimate visual Texture2DArray metadata to {manifestResourcePath}: {error2}");
				}
			}
		}
		DeleteIntermediateVisualAtlasPages(list);
		Clear();
		GD.Print($"[AdobeAnimate Atlas] generated visual Texture2DArray source layers={num}, grid={visualTextureArrayColumnCount}x{num2}: {"res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateVisualTextureArray.png"}");
		return num;
	}

	public static int RefreshBootstrapVisualTextureArray()
	{
		AdobeAnimateGlobalAtlasManifest adobeAnimateGlobalAtlasManifest = ReadBootstrapManifest();
		int manifestAtlasPageCount = GetManifestAtlasPageCount(adobeAnimateGlobalAtlasManifest);
		if (adobeAnimateGlobalAtlasManifest == null || manifestAtlasPageCount <= 0)
		{
			return 0;
		}
		string[] array = new string[manifestAtlasPageCount];
		for (int i = 0; i < manifestAtlasPageCount; i++)
		{
			array[i] = GetPagePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap", "AdobeAnimateBootstrapAtlasPage_", i);
		}
		Vector2I compactVisualLayerSize = GetCompactVisualLayerSize(adobeAnimateGlobalAtlasManifest);
		Vector2I value = new Vector2I(NextPowerOfTwo(compactVisualLayerSize.X), NextPowerOfTwo(compactVisualLayerSize.Y));
		GetTextureArrayGrid(manifestAtlasPageCount, out var bestColumns, out var bestRows);
		Image image = Image.CreateEmpty(value.X * bestColumns, value.Y * bestRows, useMipmaps: false, Image.Format.Rgba8);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		for (int j = 0; j < manifestAtlasPageCount; j++)
		{
			Image image2 = Image.LoadFromFile(ProjectSettings.GlobalizePath(array[j]));
			if (image2 == null || image2.GetWidth() <= 0 || image2.GetHeight() <= 0)
			{
				image2?.Dispose();
				return 0;
			}
			try
			{
				if (image2.GetFormat() != Image.Format.Rgba8)
				{
					image2.Convert(Image.Format.Rgba8);
				}
				int num = j % bestColumns;
				int num2 = j / bestColumns;
				image.BlitRect(image2, new Rect2I(Vector2I.Zero, new Vector2I(Math.Min(value.X, image2.GetWidth()), Math.Min(value.Y, image2.GetHeight()))), new Vector2I(num * value.X, num2 * value.Y));
			}
			finally
			{
				image2.Dispose();
			}
		}
		string path = ProjectSettings.GlobalizePath("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png");
		Directory.CreateDirectory(Path.GetDirectoryName(path));
		Error error = image.SavePng(path);
		image.Dispose();
		if (error != Error.Ok)
		{
			GD.PushError($"Failed to save AdobeAnimate bootstrap visual Texture2DArray source {"res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png"}: {error}");
			return 0;
		}
		WriteTextureArrayImportPreset("res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png", bestColumns, bestRows);
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayPath = "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapVisualTextureArray.png";
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerSize = new Vector2(value.X, value.Y);
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerOffset = 0;
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayLayerCount = manifestAtlasPageCount;
		adobeAnimateGlobalAtlasManifest.AtlasTextureArrayColumns = bestColumns;
		Error error2 = ResourceSaver.Save(adobeAnimateGlobalAtlasManifest, "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres", ResourceSaver.SaverFlags.None);
		if (error2 != Error.Ok)
		{
			GD.PushError($"Failed to save AdobeAnimate bootstrap visual array metadata: {error2}");
			return 0;
		}
		for (int k = 0; k < array.Length; k++)
		{
			DeleteResourceFile(array[k]);
			DeleteResourceFile(array[k] + ".import");
		}
		Clear();
		GD.Print($"[AdobeAnimate Bootstrap Atlas] generated visual Texture2DArray layers={manifestAtlasPageCount}, layerSize={value}, grid={bestColumns}x{bestRows}");
		return manifestAtlasPageCount;
	}

	private static Vector2I GetCompactVisualLayerSize(AdobeAnimateGlobalAtlasManifest manifest)
	{
		int num = 4;
		int num2 = 4;
		if (manifest?.MediaRects != null)
		{
			for (int i = 0; i < manifest.MediaRects.Count; i++)
			{
				Rect2 rect = manifest.MediaRects[i];
				num = Math.Max(num, Mathf.CeilToInt(rect.End.X) + 1);
				num2 = Math.Max(num2, Mathf.CeilToInt(rect.End.Y) + 1);
			}
		}
		if (manifest?.ExternalTextureRects != null)
		{
			for (int j = 0; j < manifest.ExternalTextureRects.Count; j++)
			{
				Rect2 rect2 = manifest.ExternalTextureRects[j];
				num = Math.Max(num, Mathf.CeilToInt(rect2.End.X) + 1);
				num2 = Math.Max(num2, Mathf.CeilToInt(rect2.End.Y) + 1);
			}
		}
		return new Vector2I(Math.Min(2048, AlignToCompressionBlock(num)), Math.Min(2048, AlignToCompressionBlock(num2)));
	}

	private static int GetVisualTextureArrayColumnCount(int layerCount)
	{
		GetTextureArrayGrid(layerCount, out var bestColumns, out var _);
		return bestColumns;
	}

	private static void GetTextureArrayGrid(int layerCount, out int bestColumns, out int bestRows)
	{
		layerCount = Math.Max(1, layerCount);
		bestColumns = 1;
		bestRows = layerCount;
		int num = 2147483647;
		int num2 = 2147483647;
		for (int i = 1; i <= Math.Min(8, layerCount); i++)
		{
			int num3 = (layerCount + i - 1) / i;
			if (num3 <= 8)
			{
				int num4 = i * num3;
				int num5 = Math.Abs(num3 - i);
				if (num4 < num || (num4 == num && num5 < num2) || (num4 == num && num5 == num2 && i > bestColumns))
				{
					bestColumns = i;
					bestRows = num3;
					num = num4;
					num2 = num5;
				}
			}
		}
		if (num == 2147483647)
		{
			bestColumns = Math.Min(8, layerCount);
			bestRows = (layerCount + bestColumns - 1) / bestColumns;
		}
	}

	public static bool RefreshProjectPoseData(string searchRoot = "res://Asset/Anime")
	{
		List<string> projectileScenePaths = FindProjectileRegistryScenePaths();
		List<string> effectScenePaths = FindEffectScenePaths();
		return RefreshPoseData(FindUnifiedProjectAtlasResources(searchRoot, projectileScenePaths, effectScenePaths), GetManifest(), "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", "res://addons/AdobeAnimateEditor/GeneratedAtlas/AnimationData", "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGpuPoseTextureArray.exr", compactArrayLayers: false, "[AdobeAnimate Atlas]");
	}

	public static bool RefreshBootstrapPoseData()
	{
		List<AdobeAnimateData> resources = LoadBootstrapAnimeDataResources();
		AdobeAnimateGlobalAtlasManifest manifest = ReadBootstrapManifest();
		return RefreshPoseData(resources, manifest, "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapAtlasManifest.tres", "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AnimationData", "res://addons/AdobeAnimateEditor/GeneratedAtlas/Bootstrap/AdobeAnimateBootstrapPoseTextureArray.exr", compactArrayLayers: true, "[AdobeAnimate Bootstrap Atlas]");
	}

	private static bool RefreshPoseData(List<AdobeAnimateData> resources, AdobeAnimateGlobalAtlasManifest manifest, string manifestPath, string generatedGpuPoseTextureDir, string poseTextureArrayPath, bool compactArrayLayers, string logPrefix)
	{
		if (resources.Count == 0)
		{
			return false;
		}
		Clear();
		if (manifest == null || GetManifestAtlasPageCount(manifest) == 0)
		{
			GD.PushWarning("AdobeAnimate pose texture data bake skipped because the shared atlas manifest is missing visual atlas layer metadata: " + manifestPath + ".");
			return false;
		}
		int skipped;
		int value = AdobeAnimateDefinitionCache.RefreshBakedGpuPoseTextures(resources, manifest, generatedGpuPoseTextureDir, out skipped);
		if (!RefreshPoseTextureArraySource(manifest, poseTextureArrayPath, compactArrayLayers, out var layerCount, out var layerSize))
		{
			GD.PushError("Failed to generate AdobeAnimate pose Texture2DArray source: " + poseTextureArrayPath);
			return false;
		}
		Error error = ResourceSaver.Save(manifest, manifestPath, ResourceSaver.SaverFlags.None);
		if (error != Error.Ok)
		{
			GD.PushError($"Failed to save AdobeAnimate atlas manifest with pose atlas data {manifestPath}: {error}");
			return false;
		}
		DeleteIntermediatePoseAtlasPages(manifest);
		Clear();
		GD.Print($"{logPrefix} baked pose texture data: bakedPoseTextures={value}, skippedPoseTextures={skipped}, poseArrayLayers={layerCount}, poseLayerSize={layerSize}");
		return true;
	}

	private static bool RefreshPoseTextureArraySource(AdobeAnimateGlobalAtlasManifest manifest, string poseTextureArrayPath, bool compactArrayLayers, out int layerCount, out Vector2I layerSize)
	{
		layerCount = (manifest?.PoseAtlasPagePaths?.Count).GetValueOrDefault();
		layerSize = Vector2I.Zero;
		if (manifest == null || layerCount <= 0 || string.IsNullOrWhiteSpace(poseTextureArrayPath))
		{
			return false;
		}
		if (layerCount > 64)
		{
			GD.PushError($"AdobeAnimate pose Texture2DArray has {layerCount} layers; the generated source grid supports at most {64} fixed 2048x2048 layers.");
			return false;
		}
		int[] poseLayerUsedTexelCounts = GetPoseLayerUsedTexelCounts(manifest, layerCount);
		layerSize = (compactArrayLayers ? GetCompactPoseLayerSize(poseLayerUsedTexelCounts) : new Vector2I(2048, 2048));
		if (layerSize.X <= 0 || layerSize.Y <= 0)
		{
			return false;
		}
		GetTextureArrayGrid(layerCount, out var bestColumns, out var bestRows);
		Image image = Image.CreateEmpty(layerSize.X * bestColumns, layerSize.Y * bestRows, useMipmaps: false, Image.Format.Rgbah);
		image.Fill(new Color(0f, 0f, 0f, 0f));
		try
		{
			for (int i = 0; i < layerCount; i++)
			{
				Image image2 = Image.LoadFromFile(ProjectSettings.GlobalizePath(manifest.PoseAtlasPagePaths[i]));
				if (image2 == null || image2.GetWidth() <= 0 || image2.GetHeight() <= 0)
				{
					image2?.Dispose();
					return false;
				}
				try
				{
					if (image2.GetFormat() != Image.Format.Rgbah)
					{
						image2.Convert(Image.Format.Rgbah);
					}
					int num = i % bestColumns * layerSize.X;
					int num2 = i / bestColumns * layerSize.Y;
					if (!compactArrayLayers && image2.GetWidth() == layerSize.X && image2.GetHeight() == layerSize.Y)
					{
						image.BlitRect(image2, new Rect2I(Vector2I.Zero, layerSize), new Vector2I(num, num2));
						continue;
					}
					int num3 = (compactArrayLayers ? poseLayerUsedTexelCounts[i] : Math.Min(image2.GetWidth() * image2.GetHeight(), layerSize.X * layerSize.Y));
					if (num3 > layerSize.X * layerSize.Y)
					{
						return false;
					}
					for (int j = 0; j < num3; j++)
					{
						Color pixel = image2.GetPixel(j % image2.GetWidth(), j / image2.GetWidth());
						image.SetPixel(num + j % layerSize.X, num2 + j / layerSize.X, pixel);
					}
				}
				finally
				{
					image2.Dispose();
				}
			}
			string path = ProjectSettings.GlobalizePath(poseTextureArrayPath);
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			Error error = image.SaveExr(path);
			if (error != Error.Ok)
			{
				GD.PushError($"Failed to save AdobeAnimate pose Texture2DArray source {poseTextureArrayPath}: {error}");
				return false;
			}
		}
		finally
		{
			image.Dispose();
		}
		WritePoseTextureArrayImportPreset(poseTextureArrayPath, bestColumns, bestRows);
		manifest.PoseTextureArrayPath = poseTextureArrayPath;
		manifest.PoseTextureArrayLayerSize = layerSize;
		manifest.PoseTextureArrayLayerCount = layerCount;
		manifest.PoseTextureArrayColumns = bestColumns;
		return true;
	}

	private static int[] GetPoseLayerUsedTexelCounts(AdobeAnimateGlobalAtlasManifest manifest, int layerCount)
	{
		int[] array = new int[Math.Max(0, layerCount)];
		int num = Math.Min(manifest.SourcePoseAtlasPages?.Count ?? 0, Math.Min(manifest.SourcePoseTexelStarts?.Count ?? 0, manifest.SourcePoseTexelCounts?.Count ?? 0));
		for (int i = 0; i < num; i++)
		{
			int num2 = manifest.SourcePoseAtlasPages[i];
			if (num2 >= 0 && num2 < array.Length)
			{
				array[num2] = Math.Max(array[num2], Math.Max(0, manifest.SourcePoseTexelStarts[i]) + Math.Max(0, manifest.SourcePoseTexelCounts[i]));
			}
		}
		return array;
	}

	private static Vector2I GetCompactPoseLayerSize(int[] usedTexelsPerLayer)
	{
		int num = 1;
		for (int i = 0; i < ((usedTexelsPerLayer != null) ? usedTexelsPerLayer.Length : 0); i++)
		{
			num = Math.Max(num, usedTexelsPerLayer[i]);
		}
		int num2 = Math.Clamp(NextPowerOfTwo((int)Math.Ceiling(Math.Sqrt(num))), 4, 2048);
		int num3 = NextPowerOfTwo(Math.Max(1, (num + num2 - 1) / num2));
		if (num3 > 2048)
		{
			num2 = 2048;
			num3 = NextPowerOfTwo(Math.Max(1, (num + num2 - 1) / num2));
		}
		return new Vector2I(num2, Math.Clamp(num3, 1, 2048));
	}

	private static void DeleteIntermediatePoseAtlasPages(AdobeAnimateGlobalAtlasManifest manifest)
	{
		if (manifest?.PoseAtlasPagePaths == null)
		{
			return;
		}
		for (int i = 0; i < manifest.PoseAtlasPagePaths.Count; i++)
		{
			string text = manifest.PoseAtlasPagePaths[i];
			if (string.IsNullOrWhiteSpace(text))
			{
				continue;
			}
			DeleteResourceFile(text);
			DeleteResourceFile(text + ".import");
			foreach (string generatedImportedTexturePath in GetGeneratedImportedTexturePaths(text))
			{
				DeleteResourceFile(generatedImportedTexturePath);
			}
		}
	}

	private static List<AdobeAnimateData> FindUnifiedProjectAtlasResources(string searchRoot, IReadOnlyList<string> projectileScenePaths = null, IReadOnlyList<string> effectScenePaths = null)
	{
		List<AdobeAnimateData> list = new List<AdobeAnimateData>();
		HashSet<string> seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (AdobeAnimateData item in FindAdobeAnimateDataResources(searchRoot))
		{
			AddAdobeAnimateDataResource(item, list, seenSources);
		}
		foreach (AdobeAnimateData item2 in FindProjectileAdobeAnimateDataResources(projectileScenePaths ?? System.Array.Empty<string>()))
		{
			AddAdobeAnimateDataResource(item2, list, seenSources);
		}
		foreach (AdobeAnimateData item3 in FindEffectAdobeAnimateDataResources(effectScenePaths ?? System.Array.Empty<string>()))
		{
			AddAdobeAnimateDataResource(item3, list, seenSources);
		}
		list.Sort((AdobeAnimateData a, AdobeAnimateData b) => string.Compare(a?.GetAtlasSourceKey(), b?.GetAtlasSourceKey(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static List<string> FindUnifiedProjectExternalTextureResources(string searchRoot, IReadOnlyList<AdobeAnimateData> resources, IReadOnlyList<string> projectileScenePaths, IReadOnlyList<string> effectScenePaths)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectExternalTextureResources(searchRoot, hashSet, IsUnifiedExternalTextureAtlasSourceText);
		CollectExternalTextureResources("res://Registry/AdobeAnimate", hashSet, IsUnifiedExternalTextureAtlasSourceText);
		CollectSceneMediaReplaceTextureResources(searchRoot, hashSet);
		CollectRuntimeSetReplaceTextureResources(searchRoot, hashSet);
		CollectExternalTextureResources("res://Registry/Armor", hashSet, IsArmorExternalTextureAtlasSourceText);
		CollectAdobeAnimateDataExternalTextures(resources, hashSet);
		AddExternalTexturePaths(hashSet, FindReplaceExternalTextureResources(searchRoot, resources));
		AddExternalTexturePaths(hashSet, FindProjectileExternalTextureResources(projectileScenePaths));
		AddExternalTexturePaths(hashSet, FindEffectExternalTextureResources(effectScenePaths, resources));
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static void AddExternalTexturePaths(HashSet<string> result, IEnumerable<string> texturePaths)
	{
		if (result == null || texturePaths == null)
		{
			return;
		}
		foreach (string texturePath in texturePaths)
		{
			string text = NormalizeProjectPath(texturePath);
			if (!string.IsNullOrEmpty(text) && ResourceFileExists(text))
			{
				result.Add(text);
			}
		}
	}

	private static List<string> FindExternalTextureResources(string searchRoot, IReadOnlyList<AdobeAnimateData> resources)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectExternalTextureResources(searchRoot, hashSet, IsSharedExternalTextureAtlasSourceText);
		RemoveReplaceTextureResources(resources, hashSet);
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static List<string> FindArmorExternalTextureResources()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectExternalTextureResources("res://Registry/Armor", hashSet, IsArmorExternalTextureAtlasSourceText);
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static List<string> FindReplaceExternalTextureResources(string searchRoot, IReadOnlyList<AdobeAnimateData> resources)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectAdobeAnimateDataExternalTextures(resources, hashSet);
		CollectExternalTextureResources(searchRoot, hashSet, IsReplaceExternalTextureAtlasSourceText);
		CollectSceneMediaReplaceTextureResources(searchRoot, hashSet);
		CollectRuntimeSetReplaceTextureResources(searchRoot, hashSet);
		string[] projectileAtlasSearchRoots = ProjectileAtlasSearchRoots;
		foreach (string searchRoot2 in projectileAtlasSearchRoots)
		{
			CollectExternalTextureResources(searchRoot2, hashSet, IsReplaceExternalTextureAtlasSourceText);
			CollectSceneMediaReplaceTextureResources(searchRoot2, hashSet);
			CollectRuntimeSetReplaceTextureResources(searchRoot2, hashSet);
		}
		projectileAtlasSearchRoots = EffectAtlasSearchRoots;
		foreach (string searchRoot3 in projectileAtlasSearchRoots)
		{
			CollectExternalTextureResources(searchRoot3, hashSet, IsReplaceExternalTextureAtlasSourceText);
			CollectSceneMediaReplaceTextureResources(searchRoot3, hashSet);
			CollectRuntimeSetReplaceTextureResources(searchRoot3, hashSet);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static void CollectAdobeAnimateDataExternalTextures(IReadOnlyList<AdobeAnimateData> resources, HashSet<string> result)
	{
		if (resources == null || result == null)
		{
			return;
		}
		for (int i = 0; i < resources.Count; i++)
		{
			Array<string> array = resources[i]?.extraMediaReplaceTexturePaths;
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Count; j++)
			{
				string text = NormalizeProjectPath(array[j]);
				if (!string.IsNullOrEmpty(text) && ResourceFileExists(text))
				{
					result.Add(text);
				}
			}
		}
	}

	private static void RemoveReplaceTextureResources(IReadOnlyList<AdobeAnimateData> resources, HashSet<string> result)
	{
		if (resources == null || result == null || result.Count == 0)
		{
			return;
		}
		for (int i = 0; i < resources.Count; i++)
		{
			Array<string> array = resources[i]?.extraMediaReplaceTexturePaths;
			if (array == null)
			{
				continue;
			}
			for (int j = 0; j < array.Count; j++)
			{
				string text = NormalizeProjectPath(array[j]);
				if (!string.IsNullOrEmpty(text))
				{
					result.Remove(text);
				}
			}
		}
	}

	private static void CollectExternalTextureResources(string searchRoot, HashSet<string> result, Func<string, bool> sourceTextPredicate)
	{
		if (!Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.tres"))
		{
			if (ShouldSkipAtlasScanResourcePath(ProjectSettings.LocalizePath(item.Replace('\\', '/'))))
			{
				continue;
			}
			string text;
			try
			{
				text = File.ReadAllText(item);
			}
			catch
			{
				continue;
			}
			if (sourceTextPredicate != null && !sourceTextPredicate(text))
			{
				continue;
			}
			foreach (Match item2 in Regex.Matches(text, "\\[ext_resource\\s+[^\\]]*type=\"Texture2D\"[^\\]]*path=\"([^\"]+)\""))
			{
				string text2 = NormalizeProjectPath(item2.Groups[1].Value);
				if (!string.IsNullOrEmpty(text2) && ResourceFileExists(text2))
				{
					result.Add(text2);
				}
			}
			foreach (Match item3 in Regex.Matches(text, "\"(?<path>res://[^\"]+\\.(?:png|webp|jpg|jpeg|svg|bmp|tga))\"", RegexOptions.IgnoreCase))
			{
				string text3 = NormalizeResourceReferencePath(item3.Groups["path"].Value);
				if (!string.IsNullOrEmpty(text3) && ResourceFileExists(text3))
				{
					result.Add(text3);
				}
			}
		}
	}

	private static void CollectSceneMediaReplaceTextureResources(string searchRoot, HashSet<string> result)
	{
		if (!Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.tscn"))
		{
			string text = ProjectSettings.LocalizePath(item.Replace('\\', '/'));
			if (!ShouldSkipAtlasScanResourcePath(text))
			{
				string text2;
				try
				{
					text2 = File.ReadAllText(item);
				}
				catch
				{
					continue;
				}
				if (text2.Contains("mediaReplace", StringComparison.Ordinal) || text2.Contains("MediaReplace", StringComparison.Ordinal))
				{
					CollectMediaReplaceExtResourceTextures(text2, result);
					CollectMediaReplaceAtlasPathTextures(text2, result);
					CollectTextureResourcesFromScene(text, result);
				}
			}
		}
	}

	private static void CollectMediaReplaceAtlasPathTextures(string text, HashSet<string> result)
	{
		if (result == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (Match item in Regex.Matches(text, "(?m)^\\s*mediaReplaceAtlasPaths\\s*=\\s*Array\\[String\\]\\(\\[(?<items>[^\\r\\n]*)\\]\\)\\s*$"))
		{
			AddQuotedTexturePaths(item.Groups["items"].Value, result);
		}
		foreach (Match item2 in Regex.Matches(text, "(?m)^\\s*Animation/MediaReplace/[^=\\r\\n]+\\s*=\\s*(?<items>\"(?:uid|res)://[^\"]+\")\\s*$"))
		{
			AddQuotedTexturePaths(item2.Groups["items"].Value, result);
		}
	}

	private static void AddQuotedTexturePaths(string text, HashSet<string> result)
	{
		if (result == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (Match item in Regex.Matches(text, "\"(?<path>(?:uid|res)://[^\"]+)\"", RegexOptions.IgnoreCase))
		{
			string text2 = NormalizeResourceReferencePath(item.Groups["path"].Value);
			if (!string.IsNullOrEmpty(text2) && ResourceFileExists(text2))
			{
				result.Add(text2);
			}
		}
	}

	private static void CollectRuntimeSetReplaceTextureResources(string searchRoot, HashSet<string> result)
	{
		if (!Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.cs"))
		{
			if (ShouldSkipAtlasScanResourcePath(ProjectSettings.LocalizePath(item.Replace('\\', '/'))))
			{
				continue;
			}
			string text;
			try
			{
				text = File.ReadAllText(item);
			}
			catch
			{
				continue;
			}
			bool flag = text.Contains("SetReplace(", StringComparison.Ordinal) && text.Contains("GD.Load<Texture2D>", StringComparison.Ordinal);
			bool flag2 = text.Contains("SetAtlasReplace(", StringComparison.Ordinal);
			if (!flag && !flag2)
			{
				continue;
			}
			if (flag)
			{
				foreach (Match item2 in Regex.Matches(text, "GD\\.Load<Texture2D>\\(\\s*\"(?<path>(?:uid|res)://[^\"]+)\"\\s*\\)"))
				{
					string text2 = NormalizeResourceReferencePath(item2.Groups["path"].Value);
					if (!string.IsNullOrEmpty(text2) && ResourceFileExists(text2))
					{
						result.Add(text2);
					}
				}
			}
			System.Collections.Generic.Dictionary<string, string> dictionary = ParseCSharpStringResourceReferences(text);
			if (flag)
			{
				foreach (Match item3 in Regex.Matches(text, "GD\\.Load<Texture2D>\\(\\s*(?<symbol>[A-Za-z_][A-Za-z0-9_]*)\\s*\\)"))
				{
					if (dictionary.TryGetValue(item3.Groups["symbol"].Value, out var value))
					{
						string text3 = NormalizeResourceReferencePath(value);
						if (!string.IsNullOrEmpty(text3) && ResourceFileExists(text3))
						{
							result.Add(text3);
						}
					}
				}
			}
			if (!flag2)
			{
				continue;
			}
			foreach (Match item4 in Regex.Matches(text, "SetAtlasReplace\\s*\\(\\s*[^,\\r\\n]+,\\s*\"(?<path>(?:uid|res)://[^\"]+)\""))
			{
				string text4 = NormalizeResourceReferencePath(item4.Groups["path"].Value);
				if (!string.IsNullOrEmpty(text4) && ResourceFileExists(text4))
				{
					result.Add(text4);
				}
			}
			foreach (Match item5 in Regex.Matches(text, "SetAtlasReplace\\s*\\(\\s*[^,\\r\\n]+,\\s*(?<symbol>[A-Za-z_][A-Za-z0-9_]*)"))
			{
				if (dictionary.TryGetValue(item5.Groups["symbol"].Value, out var value2))
				{
					string text5 = NormalizeResourceReferencePath(value2);
					if (!string.IsNullOrEmpty(text5) && ResourceFileExists(text5))
					{
						result.Add(text5);
					}
				}
			}
		}
	}

	private static void CollectMediaReplaceExtResourceTextures(string text, HashSet<string> result)
	{
		if (result == null || string.IsNullOrEmpty(text))
		{
			return;
		}
		System.Collections.Generic.Dictionary<string, ExtResourceReference> dictionary = ParseExtResources(text);
		if (dictionary.Count == 0)
		{
			return;
		}
		foreach (Match item in Regex.Matches(text, "mediaReplace\\s*=\\s*Array\\[Texture2D\\]\\(\\[(?<items>[^\\r\\n]*)\\]\\)"))
		{
			foreach (Match item2 in Regex.Matches(item.Groups["items"].Value, "ExtResource\\(\"(?<id>[^\"]+)\"\\)"))
			{
				if (dictionary.TryGetValue(item2.Groups["id"].Value, out var value) && string.Equals(value.Type, "Texture2D", StringComparison.Ordinal))
				{
					string text2 = NormalizeResourceReferencePath(value.Path);
					if (!string.IsNullOrEmpty(text2) && ResourceFileExists(text2))
					{
						result.Add(text2);
					}
				}
			}
		}
	}

	private static System.Collections.Generic.Dictionary<string, string> ParseCSharpStringResourceReferences(string text)
	{
		System.Collections.Generic.Dictionary<string, string> dictionary = new System.Collections.Generic.Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(text))
		{
			return dictionary;
		}
		foreach (Match item in Regex.Matches(text, "\\bstring\\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\\s*=\\s*\"(?<path>(?:uid|res)://[^\"]+)\""))
		{
			dictionary[item.Groups["name"].Value] = item.Groups["path"].Value;
		}
		return dictionary;
	}

	private static bool IsSharedExternalTextureAtlasSourceText(string text)
	{
		if (text.Contains("stageAnimeTexture", StringComparison.Ordinal) && !text.Contains("script_class=\"TowerDefenseArmorTypeData\"", StringComparison.Ordinal) && !text.Contains("res://Registry/Armor/Data/TowerDefenseArmorTypeData.cs", StringComparison.Ordinal))
		{
			return !IsReplaceExternalTextureAtlasSourceText(text);
		}
		return false;
	}

	private static bool IsUnifiedExternalTextureAtlasSourceText(string text)
	{
		if (!text.Contains("stageAnimeTexture", StringComparison.Ordinal) && !text.Contains("AdobeAnimateExternalVisualTextureRegistry", StringComparison.Ordinal) && !IsReplaceExternalTextureAtlasSourceText(text))
		{
			return IsArmorExternalTextureAtlasSourceText(text);
		}
		return true;
	}

	private static bool IsArmorExternalTextureAtlasSourceText(string text)
	{
		if (!text.Contains("script_class=\"TowerDefenseArmorTypeData\"", StringComparison.Ordinal) && !text.Contains("res://Registry/Armor/Data/TowerDefenseArmorTypeData.cs", StringComparison.Ordinal))
		{
			return text.Contains("stageAnimeTexture", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool IsReplaceExternalTextureAtlasSourceText(string text)
	{
		if (!text.Contains("script_class=\"CharacterDamagePointConfig\"", StringComparison.Ordinal) && !text.Contains("res://Resource/General/Character/DamagePoint/CharacterDamagePointConfig.cs", StringComparison.Ordinal) && !text.Contains("replaceMediaTexture", StringComparison.Ordinal))
		{
			return text.Contains("damagePointChangeMediaTexture", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool ResourceFileExists(string resourcePath)
	{
		string text = ProjectSettings.GlobalizePath(resourcePath);
		if (!string.IsNullOrEmpty(text))
		{
			return File.Exists(text);
		}
		return false;
	}

	private static string NormalizeProjectPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}
		return path.Replace('\\', '/');
	}

	private static List<AdobeAnimateData> FindAdobeAnimateDataResources(string searchRoot)
	{
		List<AdobeAnimateData> list = new List<AdobeAnimateData>();
		if (!Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return list;
		}
		foreach (string item2 in EnumerateAtlasScanFiles(searchRoot, "*.tres"))
		{
			string text = ProjectSettings.LocalizePath(item2.Replace('\\', '/'));
			if (ShouldSkipAtlasScanResourcePath(text) || !IsAdobeAnimateDataResourceFile(item2))
			{
				continue;
			}
			try
			{
				if (ResourceLoader.Load<Resource>(text, "", ResourceLoader.CacheMode.Ignore) is AdobeAnimateData item)
				{
					list.Add(item);
				}
			}
			catch (Exception ex)
			{
				GD.PushWarning("Skip AdobeAnimate atlas scan resource " + text + ": " + ex.Message);
			}
		}
		return list;
	}

	private static List<AdobeAnimateData> FindProjectileAdobeAnimateDataResources(IReadOnlyList<string> scenePaths)
	{
		List<AdobeAnimateData> list = new List<AdobeAnimateData>();
		HashSet<string> seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (string scenePath in scenePaths)
		{
			CollectAdobeAnimateDataFromScene(scenePath, list, seenSources);
		}
		list.Sort((AdobeAnimateData a, AdobeAnimateData b) => string.Compare(a?.GetAtlasSourceKey(), b?.GetAtlasSourceKey(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static List<AdobeAnimateData> FindEffectAdobeAnimateDataResources(IReadOnlyList<string> scenePaths)
	{
		List<AdobeAnimateData> list = new List<AdobeAnimateData>();
		HashSet<string> seenSources = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (scenePaths != null)
		{
			foreach (string scenePath in scenePaths)
			{
				CollectAdobeAnimateDataFromScene(scenePath, list, seenSources);
			}
		}
		string[] effectAtlasSearchRoots = EffectAtlasSearchRoots;
		for (int i = 0; i < effectAtlasSearchRoots.Length; i++)
		{
			foreach (AdobeAnimateData item in FindAdobeAnimateDataResources(effectAtlasSearchRoots[i]))
			{
				AddAdobeAnimateDataResource(item, list, seenSources);
			}
		}
		list.Sort((AdobeAnimateData a, AdobeAnimateData b) => string.Compare(a?.GetAtlasSourceKey(), b?.GetAtlasSourceKey(), StringComparison.OrdinalIgnoreCase));
		return list;
	}

	private static void AddAdobeAnimateDataResource(AdobeAnimateData data, List<AdobeAnimateData> result, HashSet<string> seenSources)
	{
		if (data != null && result != null && seenSources != null)
		{
			string atlasSourceKey = data.GetAtlasSourceKey();
			if (!string.IsNullOrEmpty(atlasSourceKey) && seenSources.Add(atlasSourceKey))
			{
				result.Add(data);
			}
		}
	}

	private static List<string> FindProjectileExternalTextureResources(IReadOnlyList<string> scenePaths)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		if (scenePaths != null)
		{
			foreach (string scenePath in scenePaths)
			{
				CollectTextureResourcesFromScene(scenePath, hashSet);
			}
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static List<string> FindEffectExternalTextureResources(IReadOnlyList<string> scenePaths, IReadOnlyList<AdobeAnimateData> resources)
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectAdobeAnimateDataExternalTextures(resources, hashSet);
		if (scenePaths != null)
		{
			foreach (string scenePath in scenePaths)
			{
				CollectTextureResourcesFromScene(scenePath, hashSet);
			}
		}
		string[] effectAtlasSearchRoots = EffectAtlasSearchRoots;
		for (int i = 0; i < effectAtlasSearchRoots.Length; i++)
		{
			CollectTextureResourcesFromRoot(effectAtlasSearchRoots[i], hashSet);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static List<string> FindEffectScenePaths()
	{
		HashSet<string> hashSet = new HashSet<string>(FindEffectRegistryScenePaths(), StringComparer.OrdinalIgnoreCase);
		string[] effectAtlasSearchRoots = EffectAtlasSearchRoots;
		for (int i = 0; i < effectAtlasSearchRoots.Length; i++)
		{
			CollectScenePathsFromRoot(effectAtlasSearchRoots[i], hashSet);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static void CollectScenePathsFromRoot(string searchRoot, HashSet<string> paths)
	{
		if (paths == null || !Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.tscn"))
		{
			string text = ProjectSettings.LocalizePath(item.Replace('\\', '/'));
			if (!ShouldSkipAtlasScanResourcePath(text))
			{
				paths.Add(text);
			}
		}
	}

	private static void CollectTextureResourcesFromRoot(string searchRoot, HashSet<string> result)
	{
		if (!Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.tscn"))
		{
			string text = ProjectSettings.LocalizePath(item.Replace('\\', '/'));
			if (!ShouldSkipAtlasScanResourcePath(text))
			{
				CollectTextureResourcesFromScene(text, result);
			}
		}
		foreach (string item2 in EnumerateAtlasScanFiles(searchRoot, "*.tres"))
		{
			string text2 = ProjectSettings.LocalizePath(item2.Replace('\\', '/'));
			if (!ShouldSkipAtlasScanResourcePath(text2))
			{
				CollectTextureResourcesFromScene(text2, result);
			}
		}
	}

	private static List<string> FindProjectileRegistryScenePaths()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectProjectileRegistryJsonScenePaths(hashSet, includeProjectileScenes: true, includeEffectScenes: false);
		string[] projectileAtlasSearchRoots = ProjectileAtlasSearchRoots;
		foreach (string searchRoot in projectileAtlasSearchRoots)
		{
			CollectProjectileConfigScenePathsFromRoot(searchRoot, hashSet, includeProjectileScenes: true, includeEffectScenes: false);
			CollectScenePathsFromRoot(searchRoot, hashSet);
		}
		try
		{
			TowerDefenseProjectileRegistry.Init();
			foreach (KeyValuePair<StringName, TowerDefenseProjectileData> item in TowerDefenseProjectileRegistry.ProjectileDictionary)
			{
				AddPackedScenePath(item.Value?.projectileScene, hashSet);
			}
			foreach (KeyValuePair<StringName, TowerDefenseProjectileSkinData> item2 in TowerDefenseProjectileRegistry.ProjectileSkinDictionary)
			{
				TowerDefenseProjectileSkinData value = item2.Value;
				if (value == null)
				{
					continue;
				}
				foreach (KeyValuePair<StringName, PackedScene> item3 in value.SkinProjectileSceneDictionary)
				{
					AddPackedScenePath(item3.Value, hashSet);
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("AdobeAnimate projectile atlas registry scan failed: " + ex.Message);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static List<string> FindEffectRegistryScenePaths()
	{
		HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		CollectProjectileRegistryJsonScenePaths(hashSet, includeProjectileScenes: false, includeEffectScenes: true);
		try
		{
			TowerDefenseProjectileRegistry.Init();
			foreach (KeyValuePair<StringName, TowerDefenseProjectileData> item in TowerDefenseProjectileRegistry.ProjectileDictionary)
			{
				AddPackedScenePath(item.Value?.splatScene, hashSet);
				AddPackedScenePath(item.Value?.hitEffect, hashSet);
			}
			foreach (KeyValuePair<StringName, TowerDefenseProjectileSkinData> item2 in TowerDefenseProjectileRegistry.ProjectileSkinDictionary)
			{
				TowerDefenseProjectileSkinData value = item2.Value;
				if (value == null)
				{
					continue;
				}
				foreach (KeyValuePair<StringName, PackedScene> item3 in value.SkinSplatSceneDictionary)
				{
					AddPackedScenePath(item3.Value, hashSet);
				}
			}
		}
		catch (Exception ex)
		{
			GD.PushWarning("AdobeAnimate effect atlas projectile registry scan failed: " + ex.Message);
		}
		List<string> list = new List<string>(hashSet);
		list.Sort(StringComparer.OrdinalIgnoreCase);
		return list;
	}

	private static void CollectProjectileRegistryJsonScenePaths(HashSet<string> paths, bool includeProjectileScenes, bool includeEffectScenes)
	{
		string text = ReadResourceText("res://Registry/Projectile/ProjectileRegistry.json");
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		foreach (Match item in Regex.Matches(text, "\"(?<field>ProjectileScene|SplatScene|HitEffect)\"\\s*:\\s*\"(?<path>(?:uid|res)://[^\"]+)\""))
		{
			if (string.Equals(item.Groups["field"].Value, "ProjectileScene", StringComparison.Ordinal) ? includeProjectileScenes : includeEffectScenes)
			{
				string text2 = NormalizeResourceReferencePath(item.Groups["path"].Value);
				if (!string.IsNullOrEmpty(text2))
				{
					paths.Add(text2);
				}
			}
		}
		foreach (Match item2 in Regex.Matches(text, "\"((?:uid|res)://[^\"]+)\""))
		{
			string text3 = NormalizeResourceReferencePath(item2.Groups[1].Value);
			if (!string.IsNullOrEmpty(text3) && text3.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
			{
				CollectProjectileDataScenePaths(text3, paths, includeProjectileScenes, includeEffectScenes);
			}
		}
	}

	private static void CollectProjectileConfigScenePathsFromRoot(string searchRoot, HashSet<string> paths, bool includeProjectileScenes, bool includeEffectScenes)
	{
		if (paths == null || !Directory.Exists(ProjectSettings.GlobalizePath(searchRoot)))
		{
			return;
		}
		foreach (string item in EnumerateAtlasScanFiles(searchRoot, "*.tres"))
		{
			string text = ProjectSettings.LocalizePath(item.Replace('\\', '/'));
			if (!ShouldSkipAtlasScanResourcePath(text))
			{
				CollectProjectileDataScenePaths(text, paths, includeProjectileScenes, includeEffectScenes);
			}
		}
	}

	private static void CollectProjectileDataScenePaths(string dataPath, HashSet<string> paths, bool includeProjectileScenes, bool includeEffectScenes)
	{
		string text = ReadResourceText(dataPath);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		System.Collections.Generic.Dictionary<string, ExtResourceReference> dictionary = ParseExtResources(text);
		foreach (Match item in Regex.Matches(text, "\\b(?<field>projectileScene|splatScene|hitEffect)\\s*=\\s*ExtResource\\(\"(?<id>[^\"]+)\"\\)"))
		{
			if (!(string.Equals(item.Groups["field"].Value, "projectileScene", StringComparison.Ordinal) ? includeProjectileScenes : includeEffectScenes))
			{
				continue;
			}
			string value = item.Groups["id"].Value;
			if (dictionary.TryGetValue(value, out var value2) && (string.IsNullOrEmpty(value2.Type) || string.Equals(value2.Type, "PackedScene", StringComparison.Ordinal)))
			{
				string text2 = NormalizeResourceReferencePath(value2.Path);
				if (!string.IsNullOrEmpty(text2))
				{
					paths.Add(text2);
				}
			}
		}
	}

	private static void AddPackedScenePath(PackedScene scene, HashSet<string> paths)
	{
		if (GodotObject.IsInstanceValid(scene) && paths != null)
		{
			string text = NormalizeResourceReferencePath(scene.ResourcePath);
			if (!string.IsNullOrEmpty(text))
			{
				paths.Add(text);
			}
		}
	}

	private static void CollectAdobeAnimateDataFromScene(string scenePath, List<AdobeAnimateData> result, HashSet<string> seenSources)
	{
		string text = ReadResourceText(scenePath);
		if (string.IsNullOrEmpty(text) || !text.Contains("flashAnimeData", StringComparison.Ordinal))
		{
			return;
		}
		System.Collections.Generic.Dictionary<string, ExtResourceReference> dictionary = ParseExtResources(text);
		foreach (Match item in Regex.Matches(text, "flashAnimeData\\s*=\\s*ExtResource\\(\"([^\"]+)\"\\)"))
		{
			string value = item.Groups[1].Value;
			if (!dictionary.TryGetValue(value, out var value2) || (!string.IsNullOrEmpty(value2.Type) && !string.Equals(value2.Type, "Resource", StringComparison.Ordinal) && !string.Equals(value2.Type, "AdobeAnimateData", StringComparison.Ordinal)))
			{
				continue;
			}
			string text2 = NormalizeResourceReferencePath(value2.Path);
			if (string.IsNullOrEmpty(text2))
			{
				continue;
			}
			AdobeAnimateData adobeAnimateData = LoadAdobeAnimateData(text2);
			if (adobeAnimateData != null)
			{
				string atlasSourceKey = adobeAnimateData.GetAtlasSourceKey();
				if (!string.IsNullOrEmpty(atlasSourceKey) && seenSources.Add(atlasSourceKey))
				{
					result.Add(adobeAnimateData);
				}
			}
		}
	}

	private static void CollectTextureResourcesFromScene(string scenePath, HashSet<string> result)
	{
		string text = ReadResourceText(scenePath);
		if (string.IsNullOrEmpty(text))
		{
			return;
		}
		if (text.Contains("texture = ExtResource", StringComparison.Ordinal))
		{
			System.Collections.Generic.Dictionary<string, ExtResourceReference> dictionary = ParseExtResources(text);
			foreach (Match item in Regex.Matches(text, "texture\\s*=\\s*ExtResource\\(\"([^\"]+)\"\\)"))
			{
				string value = item.Groups[1].Value;
				if (dictionary.TryGetValue(value, out var value2) && string.Equals(value2.Type, "Texture2D", StringComparison.Ordinal))
				{
					string text2 = NormalizeResourceReferencePath(value2.Path);
					if (!string.IsNullOrEmpty(text2) && ResourceFileExists(text2))
					{
						result.Add(text2);
					}
				}
			}
		}
		foreach (Match item2 in Regex.Matches(text, "\\bexternalAtlasTexturePath\\s*=\\s*\"(?<path>(?:uid|res)://[^\"]+)\""))
		{
			string text3 = NormalizeResourceReferencePath(item2.Groups["path"].Value);
			if (!string.IsNullOrEmpty(text3) && ResourceFileExists(text3))
			{
				result.Add(text3);
			}
		}
	}

	private static System.Collections.Generic.Dictionary<string, ExtResourceReference> ParseExtResources(string text)
	{
		System.Collections.Generic.Dictionary<string, ExtResourceReference> dictionary = new System.Collections.Generic.Dictionary<string, ExtResourceReference>(StringComparer.Ordinal);
		using StringReader stringReader = new StringReader(text);
		while (true)
		{
			string text2 = stringReader.ReadLine();
			if (text2 == null)
			{
				break;
			}
			if (text2.TrimStart().StartsWith("[ext_resource", StringComparison.Ordinal))
			{
				string text3 = MatchAttribute(text2, "id");
				if (!string.IsNullOrEmpty(text3))
				{
					dictionary[text3] = new ExtResourceReference
					{
						Type = MatchAttribute(text2, "type"),
						Path = MatchAttribute(text2, "path")
					};
				}
			}
		}
		return dictionary;
	}

	private static string MatchAttribute(string line, string attribute)
	{
		Match match = Regex.Match(line, "\\b" + Regex.Escape(attribute) + "=\"([^\"]*)\"");
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups[1].Value;
	}

	private static AdobeAnimateData LoadAdobeAnimateData(string resourcePath)
	{
		try
		{
			return ResourceLoader.Load<Resource>(resourcePath, "", ResourceLoader.CacheMode.Ignore) as AdobeAnimateData;
		}
		catch (Exception ex)
		{
			GD.PushWarning("Skip AdobeAnimate projectile atlas resource " + resourcePath + ": " + ex.Message);
			return null;
		}
	}

	private static string ReadResourceText(string resourcePath)
	{
		resourcePath = NormalizeResourceReferencePath(resourcePath);
		if (string.IsNullOrEmpty(resourcePath))
		{
			return string.Empty;
		}
		string text = ProjectSettings.GlobalizePath(resourcePath);
		if (string.IsNullOrEmpty(text) || !File.Exists(text))
		{
			return string.Empty;
		}
		try
		{
			return File.ReadAllText(text);
		}
		catch
		{
			return string.Empty;
		}
	}

	private static string NormalizeResourceReferencePath(string path)
	{
		path = NormalizeProjectPath(path);
		if (string.IsNullOrEmpty(path))
		{
			return string.Empty;
		}
		if (path.StartsWith("res://", StringComparison.Ordinal))
		{
			return path;
		}
		if (path.StartsWith("uid://", StringComparison.Ordinal))
		{
			long num = ResourceUid.TextToId(path);
			if (num == -1 || !ResourceUid.HasId(num))
			{
				return string.Empty;
			}
			return NormalizeProjectPath(ResourceUid.GetIdPath(num));
		}
		return string.Empty;
	}

	private static IEnumerable<string> EnumerateAtlasScanFiles(string searchRoot, string searchPattern)
	{
		string text = ProjectSettings.GlobalizePath(searchRoot);
		if (!Directory.Exists(text))
		{
			yield break;
		}
		Stack<string> pendingDirectories = new Stack<string>();
		pendingDirectories.Push(text);
		while (pendingDirectories.Count > 0)
		{
			string directory = pendingDirectories.Pop();
			string[] files;
			try
			{
				files = Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
			}
			catch
			{
				continue;
			}
			System.Array.Sort(files, (IComparer<string>?)StringComparer.OrdinalIgnoreCase);
			string[] array = files;
			for (int i = 0; i < array.Length; i++)
			{
				yield return array[i];
			}
			string[] directories;
			try
			{
				directories = Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly);
			}
			catch
			{
				continue;
			}
			System.Array.Sort(directories, (IComparer<string>?)StringComparer.OrdinalIgnoreCase);
			for (int num = directories.Length - 1; num >= 0; num--)
			{
				if (!ShouldSkipAtlasScanDirectory(directories[num]))
				{
					pendingDirectories.Push(directories[num]);
				}
			}
		}
	}

	private static bool ShouldSkipAtlasScanDirectory(string absoluteDirectory)
	{
		if (string.IsNullOrEmpty(absoluteDirectory))
		{
			return true;
		}
		string fileName = Path.GetFileName(absoluteDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (!string.IsNullOrEmpty(fileName) && fileName.StartsWith(".", StringComparison.Ordinal))
		{
			return true;
		}
		string text = ProjectSettings.LocalizePath(absoluteDirectory.Replace('\\', '/'));
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (!IsGeneratedAtlasResourcePath(text) && !string.Equals(text, "res://addons/AdobeAnimateEditor/GeneratedAtlas", StringComparison.OrdinalIgnoreCase))
		{
			return IsEffectAtlasSearchRoot(text);
		}
		return true;
	}

	private static bool IsEffectAtlasSearchRoot(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			return false;
		}
		string a = NormalizeProjectPath(resourcePath);
		for (int i = 0; i < EffectAtlasSearchRoots.Length; i++)
		{
			if (string.Equals(a, EffectAtlasSearchRoots[i], StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ShouldSkipAtlasScanResourcePath(string resourcePath)
	{
		return IsGeneratedAtlasResourcePath(resourcePath);
	}

	private static bool IsGeneratedAtlasResourcePath(string resourcePath)
	{
		if (string.IsNullOrEmpty(resourcePath))
		{
			return false;
		}
		string text = resourcePath.Replace('\\', '/');
		if (!text.StartsWith("res://addons/AdobeAnimateEditor/GeneratedAtlas/", StringComparison.OrdinalIgnoreCase))
		{
			return string.Equals(text, "res://addons/AdobeAnimateEditor/GeneratedAtlas/AdobeAnimateGlobalAtlasManifest.tres", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static bool IsAdobeAnimateDataResourceFile(string absolutePath)
	{
		try
		{
			using StreamReader streamReader = new StreamReader(absolutePath);
			for (int i = 0; i < 16; i++)
			{
				if (streamReader.EndOfStream)
				{
					break;
				}
				string text = streamReader.ReadLine();
				if (text != null)
				{
					if (text.Contains("script_class=\"AdobeAnimateData\"", StringComparison.Ordinal) || text.Contains("res://addons/AdobeAnimateEditor/Resource/AdobeAnimateData.cs", StringComparison.Ordinal))
					{
						return true;
					}
					continue;
				}
				break;
			}
		}
		catch
		{
		}
		return false;
	}

	private static string GetResourceDebugPath(AdobeAnimateData data)
	{
		if (data == null || string.IsNullOrWhiteSpace(data.ResourcePath))
		{
			return "<memory>";
		}
		return data.ResourcePath;
	}
}
