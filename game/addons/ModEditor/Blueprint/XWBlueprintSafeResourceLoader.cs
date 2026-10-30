using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

internal static class XWBlueprintSafeResourceLoader
{
	private const long MaxResourceBytes = 16777216L;

	private const int MaxDependencyDepth = 16;

	private const int MaxDependencyFiles = 128;

	private static readonly HashSet<string> AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
	{
		".tres", ".tscn", ".png", ".jpg", ".jpeg", ".webp", ".svg", ".bmp", ".tga", ".wav",
		".ogg", ".mp3", ".flac", ".ttf", ".otf", ".woff", ".woff2"
	};

	private static readonly HashSet<string> TextResourceExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".tres", ".tscn" };

	private static readonly Regex DependencyPathPattern = new Regex("path\\s*=\\s*\"(?<path>[^\"]+)\"", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromMilliseconds(100L, 0L));

	public static bool TryLoadSnapshot(string requestedPath, string modProjectRoot, XWBlueprintRuntimeLimits limits, out XWBlueprintSafeObject safeObject, out string diagnostic)
	{
		safeObject = null;
		if (!TryResolveContainedPath(requestedPath, modProjectRoot, out var absolutePath, out diagnostic))
		{
			return false;
		}
		if (!File.Exists(absolutePath))
		{
			diagnostic = "Resource file does not exist inside the active Mod project.";
			return false;
		}
		string extension = Path.GetExtension(absolutePath);
		if (!AllowedExtensions.Contains(extension))
		{
			diagnostic = "Resource extension '" + extension + "' is not allowed in safe preview.";
			return false;
		}
		FileInfo fileInfo = new FileInfo(absolutePath);
		if (fileInfo.Length <= 0 || fileInfo.Length > 16777216)
		{
			diagnostic = $"Resource size is outside the safe preview limit: {fileInfo.Length} bytes.";
			return false;
		}
		if (HasReparsePoint(modProjectRoot, absolutePath))
		{
			diagnostic = "Resource path crosses a symbolic link or junction and was denied.";
			return false;
		}
		if (TextResourceExtensions.Contains(extension) && !ValidateTextResource(absolutePath, modProjectRoot, out diagnostic))
		{
			return false;
		}
		System.Collections.Generic.Dictionary<string, Variant> dictionary = new System.Collections.Generic.Dictionary<string, Variant>(StringComparer.Ordinal);
		string from = Path.GetFileNameWithoutExtension(absolutePath);
		dictionary["resource_name"] = Variant.From(in from);
		dictionary["resource_path"] = Variant.From<string>(absolutePath.Replace('\\', '/'));
		dictionary["file_extension"] = Variant.From<string>(extension.TrimStart('.').ToLowerInvariant());
		dictionary["file_size"] = Variant.From<long>(fileInfo.Length);
		System.Collections.Generic.Dictionary<string, Variant> dictionary2 = dictionary;
		HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal) { "resource_name" };
		string from2 = "Resource";
		if (TextResourceExtensions.Contains(extension))
		{
			Resource resource = ResourceLoader.Load<Resource>(absolutePath, "", ResourceLoader.CacheMode.Ignore);
			if (!GodotObject.IsInstanceValid(resource))
			{
				diagnostic = "Godot rejected the contained text resource.";
				return false;
			}
			from2 = resource.GetClass();
			if (!string.IsNullOrWhiteSpace(resource.ResourceName))
			{
				from = resource.ResourceName;
				dictionary2["resource_name"] = Variant.From(in from);
			}
			int clonedValues = 0;
			foreach (Dictionary property in resource.GetPropertyList())
			{
				Variant value;
				string text = (property.TryGetValue("name", out value) ? value.AsString() : string.Empty);
				bool flag = !XWBlueprintSafeObject.IsSafeIdentifier(text);
				if (!flag)
				{
					bool flag2;
					switch (text)
					{
					case "script":
					case "resource_local_to_scene":
					case "resource_path":
						flag2 = true;
						break;
					default:
						flag2 = false;
						break;
					}
					flag = flag2;
				}
				if (flag || dictionary2.ContainsKey(text))
				{
					continue;
				}
				Variant value2;
				PropertyUsageFlags propertyUsageFlags = (PropertyUsageFlags)(property.TryGetValue("usage", out value2) ? value2.AsInt64() : 0);
				if ((propertyUsageFlags & PropertyUsageFlags.Storage) != PropertyUsageFlags.None && XWBlueprintSafeValue.TryClone(resource.Get(text), limits, (XWBlueprintSafeObject _) => false, ref clonedValues, 0, out var clone, out from))
				{
					dictionary2[text] = clone;
					if ((propertyUsageFlags & PropertyUsageFlags.Editor) != PropertyUsageFlags.None)
					{
						hashSet.Add(text);
					}
				}
			}
		}
		dictionary2["resource_class"] = Variant.From(in from2);
		safeObject = new XWBlueprintSafeObject(from2, absolutePath.Replace('\\', '/'), dictionary2, hashSet, new string[5] { "has_property", "get_property_count", "get_type", "get_resource_name", "get_resource_path" });
		diagnostic = string.Empty;
		return true;
	}

	internal static bool TryResolveContainedPath(string requestedPath, string modProjectRoot, out string absolutePath, out string diagnostic)
	{
		absolutePath = string.Empty;
		diagnostic = string.Empty;
		if (string.IsNullOrWhiteSpace(modProjectRoot))
		{
			diagnostic = "Safe resource loading requires an active Mod project root.";
			return false;
		}
		if (string.IsNullOrWhiteSpace(requestedPath) || requestedPath.IndexOf('\0') >= 0)
		{
			diagnostic = "Resource path is empty or malformed.";
			return false;
		}
		try
		{
			string text = Path.GetFullPath(ProjectSettings.GlobalizePath(modProjectRoot)).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
			string text2 = requestedPath.Trim();
			if (text2.StartsWith("uid://", StringComparison.OrdinalIgnoreCase))
			{
				diagnostic = "UID resource paths are not accepted by the Mod sandbox.";
				return false;
			}
			string text3 = ((text2.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || text2.StartsWith("user://", StringComparison.OrdinalIgnoreCase)) ? ProjectSettings.GlobalizePath(text2) : text2);
			if (!Path.IsPathRooted(text3))
			{
				text3 = Path.Combine(text, text3.Replace('/', Path.DirectorySeparatorChar));
			}
			text3 = Path.GetFullPath(text3);
			StringComparison comparisonType = (OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
			string value = text + Path.DirectorySeparatorChar;
			if (!text3.StartsWith(value, comparisonType))
			{
				diagnostic = "Resource path escapes the active Mod project root.";
				return false;
			}
			absolutePath = text3;
			return true;
		}
		catch (Exception ex) when (((ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) ? 1 : 0) != 0)
		{
			diagnostic = "Resource path is invalid: " + ex.Message;
			return false;
		}
	}

	private static bool ValidateTextResource(string resourcePath, string modProjectRoot, out string diagnostic)
	{
		HashSet<string> visited = new HashSet<string>(OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		int dependencyFiles = 0;
		return ValidateTextResourceRecursive(resourcePath, modProjectRoot, visited, ref dependencyFiles, 0, out diagnostic);
	}

	private static bool ValidateTextResourceRecursive(string resourcePath, string modProjectRoot, HashSet<string> visited, ref int dependencyFiles, int depth, out string diagnostic)
	{
		diagnostic = string.Empty;
		if (depth > 16)
		{
			diagnostic = $"Text resource dependency depth exceeds {16}.";
			return false;
		}
		if (!TryValidateDependencyFile(resourcePath, modProjectRoot, requireTextResource: true, out var absolutePath, out var _, out diagnostic))
		{
			return false;
		}
		if (!visited.Add(absolutePath))
		{
			return true;
		}
		dependencyFiles++;
		if (dependencyFiles > 128)
		{
			diagnostic = $"Text resource dependency graph exceeds {128} files.";
			return false;
		}
		string text;
		try
		{
			text = File.ReadAllText(absolutePath);
		}
		catch (Exception ex)
		{
			diagnostic = "Resource text could not be inspected: " + ex.Message;
			return false;
		}
		if (text.Contains("type=\"Script\"", StringComparison.OrdinalIgnoreCase) || text.Contains("type = \"Script\"", StringComparison.OrdinalIgnoreCase) || text.Contains("script =", StringComparison.OrdinalIgnoreCase) || text.Contains("GDExtension", StringComparison.OrdinalIgnoreCase) || text.Contains(".dll", StringComparison.OrdinalIgnoreCase) || text.Contains(".exe", StringComparison.OrdinalIgnoreCase))
		{
			diagnostic = "Script, assembly, and extension dependencies are denied in safe resource preview.";
			return false;
		}
		foreach (Match item in DependencyPathPattern.Matches(text))
		{
			string value = item.Groups["path"].Value;
			string text2 = Path.GetExtension(value).ToLowerInvariant();
			string requestedPath = value;
			if (!value.StartsWith("res://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("user://", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("uid://", StringComparison.OrdinalIgnoreCase) && !Path.IsPathRooted(value))
			{
				requestedPath = Path.Combine(Path.GetDirectoryName(absolutePath) ?? modProjectRoot, value.Replace('/', Path.DirectorySeparatorChar));
			}
			bool flag;
			switch (text2)
			{
			case ".cs":
			case ".gd":
			case ".dll":
			case ".exe":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				diagnostic = "Resource dependency '" + value + "' is outside the safe Mod resource policy.";
				return false;
			}
			if (!TryValidateDependencyFile(requestedPath, modProjectRoot, requireTextResource: false, out var absolutePath2, out var extension2, out var diagnostic2))
			{
				diagnostic = "Nested resource dependency '" + value + "' was denied: " + diagnostic2;
				return false;
			}
			if (TextResourceExtensions.Contains(extension2) && !ValidateTextResourceRecursive(absolutePath2, modProjectRoot, visited, ref dependencyFiles, depth + 1, out diagnostic2))
			{
				diagnostic = "Nested resource dependency '" + value + "' was denied: " + diagnostic2;
				return false;
			}
			if (!TextResourceExtensions.Contains(extension2) && visited.Add(absolutePath2))
			{
				dependencyFiles++;
				if (dependencyFiles > 128)
				{
					diagnostic = $"Text resource dependency graph exceeds {128} files.";
					return false;
				}
			}
		}
		return true;
	}

	private static bool TryValidateDependencyFile(string requestedPath, string modProjectRoot, bool requireTextResource, out string absolutePath, out string extension, out string diagnostic)
	{
		extension = string.Empty;
		if (!TryResolveContainedPath(requestedPath, modProjectRoot, out absolutePath, out diagnostic))
		{
			return false;
		}
		if (!File.Exists(absolutePath))
		{
			diagnostic = "Dependency file does not exist inside the active Mod project.";
			return false;
		}
		extension = Path.GetExtension(absolutePath).ToLowerInvariant();
		if (!AllowedExtensions.Contains(extension) || (requireTextResource && !TextResourceExtensions.Contains(extension)))
		{
			diagnostic = "Dependency extension '" + extension + "' is not allowed in safe preview.";
			return false;
		}
		long length;
		try
		{
			length = new FileInfo(absolutePath).Length;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			diagnostic = "Dependency file metadata could not be inspected: " + ex.Message;
			return false;
		}
		if (length <= 0 || length > 16777216)
		{
			diagnostic = $"Dependency size is outside the safe preview limit: {length} bytes.";
			return false;
		}
		if (HasReparsePoint(modProjectRoot, absolutePath))
		{
			diagnostic = "Dependency path crosses a symbolic link or junction and was denied.";
			return false;
		}
		return true;
	}

	private static bool HasReparsePoint(string rootPath, string candidatePath)
	{
		try
		{
			string fullPath = Path.GetFullPath(ProjectSettings.GlobalizePath(rootPath));
			string fullPath2 = Path.GetFullPath(candidatePath);
			if (Directory.Exists(fullPath) && (File.GetAttributes(fullPath) & FileAttributes.ReparsePoint) != FileAttributes.None)
			{
				return true;
			}
			string relativePath = Path.GetRelativePath(fullPath, fullPath2);
			string text = fullPath;
			string[] array = relativePath.Split(new char[2]
			{
				Path.DirectorySeparatorChar,
				Path.AltDirectorySeparatorChar
			}, StringSplitOptions.RemoveEmptyEntries);
			foreach (string path in array)
			{
				text = Path.Combine(text, path);
				if ((File.Exists(text) || Directory.Exists(text)) && (File.GetAttributes(text) & FileAttributes.ReparsePoint) != FileAttributes.None)
				{
					return true;
				}
			}
			return false;
		}
		catch (Exception ex) when (((ex is IOException || ex is UnauthorizedAccessException) ? 1 : 0) != 0)
		{
			return true;
		}
	}
}
