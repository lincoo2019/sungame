using System;
using System.Text;
using Godot;

public static class ThumbnailBinaryResourceCache
{
	public const string MapThumbnailCategory = "map-thumbnail";

	private const string LegacyPacketThumbnailCategory = "packet-thumbnail";

	private const string LegacyZombieThumbnailCategory = "zombie-thumbnail";

	private const string CacheRoot = "user://Csharp/CharacterBinaryCache";

	private const string CacheSchemaVersion = "thumb-sprite-v16";

	public static bool IsTextureCached(string category, string key)
	{
		if (!IsKnownCategory(category) || string.IsNullOrEmpty(key))
		{
			return false;
		}
		return FileAccess.FileExists(GetCachePath(category, key));
	}

	public static bool HasAnyCachedTexture(string category)
	{
		if (!IsKnownCategory(category))
		{
			return false;
		}
		string categoryDirectory = GetCategoryDirectory(category);
		if (!DirAccess.DirExistsAbsolute(categoryDirectory))
		{
			return false;
		}
		DirAccess dirAccess = DirAccess.Open(categoryDirectory);
		if (dirAccess == null)
		{
			return false;
		}
		string[] files = dirAccess.GetFiles();
		for (int i = 0; i < files.Length; i++)
		{
			if (files[i].EndsWith(".res", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}

	public static Texture2D Load(string category, string key)
	{
		if (!IsKnownCategory(category) || string.IsNullOrEmpty(key))
		{
			return null;
		}
		string cachePath = GetCachePath(category, key);
		if (!FileAccess.FileExists(cachePath))
		{
			return null;
		}
		Texture2D texture2D = ResourceLoader.Load(cachePath, "", ResourceLoader.CacheMode.Ignore) as Texture2D;
		if (IsTextureUsable(texture2D))
		{
			return texture2D;
		}
		DirAccess.RemoveAbsolute(cachePath);
		return null;
	}

	public static bool Save(string category, string key, Texture2D texture)
	{
		if (!IsKnownCategory(category) || string.IsNullOrEmpty(key) || !IsTextureUsable(texture))
		{
			return false;
		}
		string cachePath = GetCachePath(category, key);
		EnsureDirectory(cachePath.GetBaseDir());
		return ResourceSaver.Save(texture, cachePath, ResourceSaver.SaverFlags.Compress) == Error.Ok;
	}

	public static Texture2D CreateCharacterThumbnailTexture(Image image)
	{
		if (!HasVisiblePixels(image))
		{
			return null;
		}
		return ImageTexture.CreateFromImage(image);
	}

	public static bool HasVisiblePixels(Image image)
	{
		if (!GodotObject.IsInstanceValid(image) || image.IsEmpty())
		{
			return false;
		}
		if (image.IsCompressed() && image.Decompress() != Error.Ok)
		{
			return false;
		}
		Rect2I usedRect = image.GetUsedRect();
		if (usedRect.Size.X > 0)
		{
			return usedRect.Size.Y > 0;
		}
		return false;
	}

	public static bool IsTextureUsable(Texture2D texture)
	{
		if (!GodotObject.IsInstanceValid(texture))
		{
			return false;
		}
		Image image = texture.GetImage();
		if (!GodotObject.IsInstanceValid(image))
		{
			return false;
		}
		bool result = HasVisiblePixels(image);
		image.Dispose();
		return result;
	}

	public static void ClearOutdatedVersionCaches()
	{
		if (!DirAccess.DirExistsAbsolute("user://Csharp/CharacterBinaryCache"))
		{
			return;
		}
		string versionDirectoryName = GetVersionDirectoryName();
		DirAccess dirAccess = DirAccess.Open("user://Csharp/CharacterBinaryCache");
		if (dirAccess == null)
		{
			return;
		}
		string[] directories = dirAccess.GetDirectories();
		foreach (string text in directories)
		{
			string text2 = "user://Csharp/CharacterBinaryCache".PathJoin(text);
			if (text != versionDirectoryName)
			{
				RemoveRecursive(text2);
			}
			else
			{
				ClearOutdatedSchemaCaches(text2);
			}
		}
	}

	private static void ClearOutdatedSchemaCaches(string versionPath)
	{
		DirAccess dirAccess = DirAccess.Open(versionPath);
		if (dirAccess == null)
		{
			return;
		}
		string[] directories = dirAccess.GetDirectories();
		foreach (string text in directories)
		{
			if (!(text == "thumb-sprite-v16") && text.StartsWith("thumb-sprite-", StringComparison.OrdinalIgnoreCase))
			{
				RemoveRecursive(versionPath.PathJoin(text));
			}
		}
	}

	public static int ClearAllCaches()
	{
		if (DirAccess.DirExistsAbsolute("user://Csharp/CharacterBinaryCache"))
		{
			return RemoveRecursive("user://Csharp/CharacterBinaryCache");
		}
		return 0;
	}

	public static int ClearLegacyCharacterThumbnailCategories()
	{
		return ClearCategoryDirectory("packet-thumbnail") + ClearCategoryDirectory("zombie-thumbnail");
	}

	private static int ClearCategoryDirectory(string category)
	{
		string categoryDirectory = GetCategoryDirectory(category);
		if (!DirAccess.DirExistsAbsolute(categoryDirectory))
		{
			return 0;
		}
		return RemoveRecursive(categoryDirectory);
	}

	private static bool IsKnownCategory(string category)
	{
		return category == "map-thumbnail";
	}

	private static string GetCachePath(string category, string key)
	{
		return GetCategoryDirectory(category).PathJoin(EncodeFileName(key) + ".res");
	}

	private static string GetCategoryDirectory(string category)
	{
		return "user://Csharp/CharacterBinaryCache".PathJoin(GetVersionDirectoryName()).PathJoin("thumb-sprite-v16").PathJoin(category);
	}

	private static string GetVersionDirectoryName()
	{
		string value = ((Global.Instance != null) ? Global.Instance.version : ProjectSettings.GetSetting("application/config/version", "unknown").AsString());
		if (string.IsNullOrEmpty(value))
		{
			value = "unknown";
		}
		return SanitizeFileName(value);
	}

	private static string SanitizeFileName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "_";
		}
		StringBuilder stringBuilder = new StringBuilder(value.Length);
		foreach (char c in value)
		{
			if (char.IsLetterOrDigit(c) || c == '-' || c == '_' || c == '.')
			{
				stringBuilder.Append(c);
			}
			else
			{
				stringBuilder.Append('_');
			}
		}
		return stringBuilder.ToString();
	}

	private static string EncodeFileName(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return "_";
		}
		return Convert.ToBase64String(Encoding.UTF8.GetBytes(value)).TrimEnd('=').Replace('+', '-')
			.Replace('/', '_');
	}

	private static void EnsureDirectory(string directoryPath)
	{
		if (!DirAccess.DirExistsAbsolute(directoryPath))
		{
			DirAccess.MakeDirRecursiveAbsolute(directoryPath);
		}
	}

	private static int RemoveRecursive(string path)
	{
		if (!DirAccess.DirExistsAbsolute(path))
		{
			return 0;
		}
		DirAccess dirAccess = DirAccess.Open(path);
		if (dirAccess == null)
		{
			return 0;
		}
		int num = 0;
		string[] files = dirAccess.GetFiles();
		foreach (string file in files)
		{
			if (DirAccess.RemoveAbsolute(path.PathJoin(file)) == Error.Ok)
			{
				num++;
			}
		}
		files = dirAccess.GetDirectories();
		foreach (string file2 in files)
		{
			num += RemoveRecursive(path.PathJoin(file2));
		}
		if (DirAccess.RemoveAbsolute(path) == Error.Ok)
		{
			num++;
		}
		return num;
	}
}
