using System;
using System.Collections.Generic;
using System.IO;
using Godot;
using Godot.Collections;
using PVZHE.ModEditor.ModSystem;
using PVZHE.ModEditor.ScriptEditor;

namespace PVZHE.ModEditor.FileSystem;

public static class XWFileSystemDropHelper
{
	public static bool CanDropFilesToDirectory(Variant data, string targetDirectory)
	{
		targetDirectory = NormalizeDirectoryPath(targetDirectory);
		if (string.IsNullOrWhiteSpace(targetDirectory) || !DirAccess.DirExistsAbsolute(targetDirectory))
		{
			return false;
		}
		List<string> validPaths;
		return ValidateDropPathsToDirectory(ExtractDropPaths(data), targetDirectory, out validPaths);
	}

	public static void DropFilesToDirectory(Variant data, string targetDirectory)
	{
		DropPathsToDirectory(ExtractDropPaths(data), targetDirectory);
	}

	public static List<string> ImportExternalFilesToDirectory(string[] files, string targetDirectory)
	{
		return DropPathsToDirectory(files ?? System.Array.Empty<string>(), targetDirectory, externalOnly: true);
	}

	public static List<string> ImportExternalFilesToBestDirectory(string[] files, string targetDirectory)
	{
		List<string> list = new List<string>();
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		string preferredDirectory = NormalizeDirectoryPath(targetDirectory);
		string[] array = files ?? System.Array.Empty<string>();
		for (int i = 0; i < array.Length; i++)
		{
			string text = NormalizeDropPath(array[i]);
			if (!string.IsNullOrWhiteSpace(text) && IsExternalDropPath(text, singleton.ProjectFolderPath) && PathExists(text) && !IsDirectoryPath(text))
			{
				string text2 = ResolveExternalImportDirectory(text, preferredDirectory, singleton.ProjectFolderPath);
				if (!string.IsNullOrWhiteSpace(text2))
				{
					list.AddRange(TryImportExternalPathToDirectory(text, text2));
				}
			}
		}
		if (list.Count > 0)
		{
			singleton.ScanChanges();
		}
		return list;
	}

	private static string ResolveExternalImportDirectory(string sourcePath, string preferredDirectory, string projectFolderPath)
	{
		sourcePath = NormalizeDropPath(sourcePath);
		preferredDirectory = NormalizeDirectoryPath(preferredDirectory);
		string fallbackExternalDropDirectory = GetFallbackExternalDropDirectory(sourcePath, projectFolderPath);
		if (string.IsNullOrWhiteSpace(preferredDirectory))
		{
			return fallbackExternalDropDirectory;
		}
		if (string.IsNullOrWhiteSpace(fallbackExternalDropDirectory))
		{
			if (!HasExplicitDropRule(preferredDirectory, projectFolderPath) || !CanDropPathToDirectory(sourcePath, preferredDirectory))
			{
				return "";
			}
			return preferredDirectory;
		}
		if (ShouldPreferFallbackExternalDirectory(preferredDirectory, fallbackExternalDropDirectory, projectFolderPath))
		{
			return fallbackExternalDropDirectory;
		}
		if (CanDropPathToDirectory(sourcePath, preferredDirectory))
		{
			return preferredDirectory;
		}
		return fallbackExternalDropDirectory;
	}

	private static bool ShouldPreferFallbackExternalDirectory(string preferredDirectory, string fallbackDirectory, string projectFolderPath)
	{
		if (string.IsNullOrWhiteSpace(fallbackDirectory) || string.IsNullOrWhiteSpace(projectFolderPath))
		{
			return false;
		}
		string text = XWModProjectLayout.ToProjectRelativePath(preferredDirectory, projectFolderPath);
		string text2 = XWModProjectLayout.ToProjectRelativePath(fallbackDirectory, projectFolderPath);
		if (string.IsNullOrWhiteSpace(text) || text == ".")
		{
			return true;
		}
		if (string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (XWModProjectLayout.GetDropTargetRuleRoot(text2).StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return string.IsNullOrWhiteSpace(XWModProjectLayout.GetDropTargetRuleRoot(text));
	}

	private static bool HasExplicitDropRule(string directory, string projectFolderPath)
	{
		if (string.IsNullOrWhiteSpace(directory) || string.IsNullOrWhiteSpace(projectFolderPath))
		{
			return false;
		}
		return !string.IsNullOrWhiteSpace(XWModProjectLayout.GetDropTargetRuleRoot(XWModProjectLayout.ToProjectRelativePath(directory, projectFolderPath)));
	}

	private static List<string> TryImportExternalPathToDirectory(string sourcePath, string targetDirectory)
	{
		if (!CanDropPathToDirectory(sourcePath, targetDirectory))
		{
			return new List<string>();
		}
		return CopyExternalPathToDirectory(sourcePath, targetDirectory);
	}

	private static string GetFallbackExternalDropDirectory(string sourcePath, string projectFolderPath)
	{
		string fallbackExternalDropRelativeFolder = GetFallbackExternalDropRelativeFolder(sourcePath);
		if (string.IsNullOrWhiteSpace(fallbackExternalDropRelativeFolder) || string.IsNullOrWhiteSpace(projectFolderPath))
		{
			return "";
		}
		string text = NormalizeDirectoryPath(NormalizeDirectoryPath(projectFolderPath).PathJoin(fallbackExternalDropRelativeFolder));
		DirAccess.MakeDirRecursiveAbsolute(text);
		return text;
	}

	private static string GetFallbackExternalDropRelativeFolder(string sourcePath)
	{
		if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(sourcePath))
		{
			return "";
		}
		string text = Path.GetExtension(sourcePath).TrimStart('.').ToLowerInvariant();
		switch (text)
		{
		case "tscn":
		case "scn":
			return "Scenes";
		case "cs":
			return "Scripts";
		case "csv":
			return "Localization";
		case "tres":
		case "res":
		case "fla":
		case "xfl":
		case "dat":
			return "";
		default:
			return XWModProjectLayout.GetAssetFolderForExtension(text);
		}
	}

	private static List<string> DropPathsToDirectory(IEnumerable<string> paths, string targetDirectory, bool externalOnly = false)
	{
		List<string> list = new List<string>();
		targetDirectory = NormalizeDirectoryPath(targetDirectory);
		if (string.IsNullOrWhiteSpace(targetDirectory) || !DirAccess.DirExistsAbsolute(targetDirectory))
		{
			return list;
		}
		if (!ValidateDropPathsToDirectory(paths, targetDirectory, out var validPaths))
		{
			return list;
		}
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		bool flag = false;
		foreach (string item in validPaths)
		{
			if (IsExternalDropPath(item, singleton.ProjectFolderPath))
			{
				List<string> list2 = CopyExternalPathToDirectory(item, targetDirectory);
				if (list2.Count > 0)
				{
					flag = true;
					list.AddRange(list2);
				}
			}
			else if (!externalOnly)
			{
				string text = MoveInternalPathToDirectory(item, targetDirectory);
				if (!string.IsNullOrWhiteSpace(text))
				{
					list.Add(text);
				}
			}
		}
		if (flag)
		{
			singleton.ScanChanges();
		}
		return list;
	}

	private static bool ValidateDropPathsToDirectory(IEnumerable<string> paths, string targetDirectory, out List<string> validPaths)
	{
		validPaths = new List<string>();
		foreach (string item in paths ?? System.Array.Empty<string>())
		{
			string text = NormalizeDropPath(item);
			if (!string.IsNullOrWhiteSpace(text))
			{
				if (!CanDropPathToDirectory(text, targetDirectory))
				{
					return false;
				}
				validPaths.Add(text);
			}
		}
		return validPaths.Count > 0;
	}

	private static string MoveInternalPathToDirectory(string sourcePath, string targetDirectory)
	{
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		string file = sourcePath.TrimEnd('/').GetFile();
		if (string.IsNullOrWhiteSpace(file))
		{
			return "";
		}
		string text = MakeUniqueTargetPath(singleton, sourcePath, NormalizeDirectoryPath(targetDirectory).PathJoin(file));
		if (XWFileSystem.GetSingleton().MoveFile(sourcePath, text) != Error.Ok)
		{
			return "";
		}
		return text;
	}

	private static List<string> CopyExternalPathToDirectory(string sourcePath, string targetDirectory)
	{
		XWFileSystem singleton = XWFileSystem.GetSingleton();
		string file = sourcePath.TrimEnd('/').GetFile();
		if (string.IsNullOrWhiteSpace(file))
		{
			return new List<string>();
		}
		string text = MakeUniqueTargetPath(singleton, sourcePath, NormalizeDirectoryPath(targetDirectory).PathJoin(file));
		List<string> list = new List<string>();
		Error error = Error.Ok;
		try
		{
			Directory.CreateDirectory(NormalizeDirectoryPath(text.GetBaseDir()));
			if (IsDirectoryPath(sourcePath))
			{
				error = CopyExternalDirectoryRecursive(NormalizeDirectoryPath(sourcePath), NormalizeDirectoryPath(text), list);
			}
			else
			{
				File.Copy(sourcePath, text, overwrite: false);
				list.Add(text);
			}
		}
		catch
		{
			error = Error.CantCreate;
		}
		if (error != Error.Ok)
		{
			return new List<string>();
		}
		foreach (string item in list)
		{
			singleton.RegisterManifestPath(item);
		}
		return list;
	}

	private static Error CopyExternalDirectoryRecursive(string sourceDirectory, string targetDirectory, List<string> copiedFiles)
	{
		sourceDirectory = NormalizeDirectoryPath(sourceDirectory);
		targetDirectory = NormalizeDirectoryPath(targetDirectory);
		try
		{
			Directory.CreateDirectory(targetDirectory);
			string[] directories = Directory.GetDirectories(sourceDirectory);
			foreach (string path in directories)
			{
				string path2 = targetDirectory.PathJoin(Path.GetFileName(path));
				Error error = CopyExternalDirectoryRecursive(NormalizeDirectoryPath(path), NormalizeDirectoryPath(path2), copiedFiles);
				if (error != Error.Ok)
				{
					return error;
				}
			}
			directories = Directory.GetFiles(sourceDirectory);
			foreach (string text in directories)
			{
				string text2 = targetDirectory.PathJoin(Path.GetFileName(text));
				File.Copy(text, text2, overwrite: false);
				copiedFiles.Add(text2);
			}
		}
		catch
		{
			return Error.CantCreate;
		}
		return Error.Ok;
	}

	public static List<string> ExtractDropPaths(Variant data)
	{
		List<string> list = new List<string>();
		if (data.VariantType != Variant.Type.Dictionary)
		{
			return list;
		}
		Dictionary dictionary = data.As<Dictionary>();
		if (dictionary.ContainsKey("files"))
		{
			AddVariantPaths(list, dictionary["files"]);
		}
		else if (dictionary.ContainsKey("paths"))
		{
			AddVariantPaths(list, dictionary["paths"]);
		}
		else if (dictionary.ContainsKey("path"))
		{
			AddDropPath(list, dictionary["path"].AsString());
		}
		return list;
	}

	private static void AddVariantPaths(List<string> paths, Variant value)
	{
		if (value.VariantType == Variant.Type.PackedStringArray)
		{
			string[] array = value.AsStringArray();
			foreach (string path in array)
			{
				AddDropPath(paths, path);
			}
			return;
		}
		if (value.VariantType == Variant.Type.Array)
		{
			foreach (Variant item in value.As<Godot.Collections.Array>())
			{
				AddDropPath(paths, item.AsString());
			}
			return;
		}
		AddDropPath(paths, value.AsString());
	}

	private static void AddDropPath(List<string> paths, string path)
	{
		path = NormalizeDropPath(path);
		if (!string.IsNullOrWhiteSpace(path))
		{
			paths.Add(path);
		}
	}

	private static bool CanDropPathToDirectory(string path, string targetDirectory)
	{
		string text = NormalizeDropPath(path);
		targetDirectory = NormalizeDirectoryPath(targetDirectory);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(targetDirectory))
		{
			return false;
		}
		if (!PathExists(text))
		{
			return false;
		}
		if (ContainsUnsupportedScriptSource(text))
		{
			return false;
		}
		if ((IsDirectoryPath(text) ? NormalizeDirectoryPath(text) : NormalizeDirectoryPath(text.GetBaseDir())) == targetDirectory)
		{
			return false;
		}
		if (IsDirectoryPath(text) && targetDirectory.StartsWith(NormalizeDirectoryPath(text), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (!XWModProjectLayout.IsDropAllowedInDirectory(text, targetDirectory, XWFileSystem.GetSingleton().ProjectFolderPath))
		{
			return false;
		}
		return true;
	}

	private static bool ContainsUnsupportedScriptSource(string sourcePath)
	{
		if (!IsDirectoryPath(sourcePath))
		{
			return XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(sourcePath);
		}
		try
		{
			foreach (string item in Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories))
			{
				if (XWCSharpOnlyPolicy.IsUnsupportedScriptSourcePath(item))
				{
					return true;
				}
			}
		}
		catch
		{
			return true;
		}
		return false;
	}

	private static bool IsExternalDropPath(string path, string projectFolderPath)
	{
		string text = NormalizeDropPath(path);
		string text2 = NormalizeDirectoryPath(projectFolderPath);
		if (string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return true;
		}
		if (!text.Equals(text2.TrimEnd('/'), StringComparison.OrdinalIgnoreCase))
		{
			return !text.StartsWith(text2, StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static bool PathExists(string path)
	{
		path = NormalizeDropPath(path);
		if (!File.Exists(path) && !Directory.Exists(path) && !Godot.FileAccess.FileExists(path))
		{
			return DirAccess.DirExistsAbsolute(path);
		}
		return true;
	}

	private static bool IsDirectoryPath(string path)
	{
		path = NormalizeDropPath(path);
		if (!Directory.Exists(path))
		{
			return DirAccess.DirExistsAbsolute(path);
		}
		return true;
	}

	private static string MakeUniqueTargetPath(XWFileSystem fs, string sourcePath, string targetPath)
	{
		string text = NormalizeDirectoryPath(targetPath.GetBaseDir());
		string file = targetPath.GetFile();
		if (IsDirectoryPath(sourcePath))
		{
			if (!PathExists(targetPath))
			{
				return NormalizeDirectoryPath(targetPath);
			}
			return NormalizeDirectoryPath(text.PathJoin(fs.GenerateUniqueFolderName(text, file)));
		}
		if (!PathExists(targetPath))
		{
			return targetPath;
		}
		return text.PathJoin(fs.GenerateUniqueFileName(text, file));
	}

	private static string NormalizeDirectoryPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		path = NormalizeDropPath(path);
		if (!path.EndsWith("/"))
		{
			return path + "/";
		}
		return path;
	}

	private static string NormalizeDropPath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		path = path.Trim();
		if (Uri.TryCreate(path, UriKind.Absolute, out Uri result) && result.IsFile)
		{
			path = result.LocalPath;
		}
		else if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("user://", StringComparison.OrdinalIgnoreCase))
		{
			path = ProjectSettings.GlobalizePath(path);
		}
		return path.Replace('\\', '/').TrimEnd('/');
	}
}
