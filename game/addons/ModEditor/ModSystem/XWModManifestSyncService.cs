using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace PVZHE.ModEditor.ModSystem;

public static class XWModManifestSyncService
{
	private const string ManifestFileName = "mod.json";

	private const string SectionScripts = "Scripts";

	private const string SectionBlueprints = "Blueprints";

	private const string SectionTranslations = "Translations";

	private const string SectionResources = "Resources";

	private static readonly HashSet<string> IgnoredFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "mod.json" };

	private static readonly HashSet<string> IgnoredExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".uid", ".import", ".bak", ".tmp", ".pmod", ".pvzmodeproject", ".csproj", ".sln" };

	private static readonly HashSet<string> IgnoredDirectoryNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".build", ".git", ".godot", "bin", "obj" };

	public static bool SyncProject(string projectPath)
	{
		projectPath = NormalizeProjectPath(projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
		{
			return false;
		}
		XWModManifest xWModManifest = LoadOrCreateManifest(projectPath);
		Dictionary<string, SortedSet<string>> dictionary = new Dictionary<string, SortedSet<string>>(StringComparer.OrdinalIgnoreCase)
		{
			["Scripts"] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase),
			["Blueprints"] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase),
			["Translations"] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase),
			["Resources"] = new SortedSet<string>(StringComparer.OrdinalIgnoreCase)
		};
		foreach (string item in EnumerateManifestFiles(projectPath))
		{
			string text = ToProjectRelativeManifestPath(item, projectPath);
			if (!string.IsNullOrWhiteSpace(text))
			{
				dictionary[GetManifestSection(text)].Add(text);
			}
		}
		bool num = ReplaceCollection(xWModManifest.Scripts, dictionary["Scripts"]) | ReplaceCollection(xWModManifest.Blueprints, dictionary["Blueprints"]) | ReplaceCollection(xWModManifest.Translations, dictionary["Translations"]) | ReplaceCollection(xWModManifest.Resources, dictionary["Resources"]) | NormalizeManifestCollections(xWModManifest);
		if (num)
		{
			xWModManifest.Save(GetManifestPath(projectPath));
		}
		return num;
	}

	public static bool RegisterPath(string projectPath, string path)
	{
		projectPath = NormalizeProjectPath(projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		if (Directory.Exists(ToAbsolutePath(path)))
		{
			return RegisterPaths(projectPath, EnumerateManifestFiles(ToAbsolutePath(path)));
		}
		string text = ToProjectRelativeManifestPath(path, projectPath);
		if (string.IsNullOrWhiteSpace(text) || ShouldIgnoreManifestRelativePath(text))
		{
			return false;
		}
		XWModManifest xWModManifest = LoadOrCreateManifest(projectPath);
		bool num = AddManifestPath(xWModManifest, text) | NormalizeManifestCollections(xWModManifest);
		if (num)
		{
			xWModManifest.Save(GetManifestPath(projectPath));
		}
		return num;
	}

	public static bool RegisterPaths(string projectPath, IEnumerable<string> paths)
	{
		projectPath = NormalizeProjectPath(projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || paths == null)
		{
			return false;
		}
		XWModManifest xWModManifest = LoadOrCreateManifest(projectPath);
		bool flag = false;
		foreach (string path in paths)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				continue;
			}
			if (Directory.Exists(ToAbsolutePath(path)))
			{
				foreach (string item in EnumerateManifestFiles(ToAbsolutePath(path)))
				{
					string text = ToProjectRelativeManifestPath(item, projectPath);
					if (!string.IsNullOrWhiteSpace(text) && !ShouldIgnoreManifestRelativePath(text))
					{
						flag |= AddManifestPath(xWModManifest, text);
					}
				}
			}
			else
			{
				string text2 = ToProjectRelativeManifestPath(path, projectPath);
				if (!string.IsNullOrWhiteSpace(text2) && !ShouldIgnoreManifestRelativePath(text2))
				{
					flag |= AddManifestPath(xWModManifest, text2);
				}
			}
		}
		flag |= NormalizeManifestCollections(xWModManifest);
		if (flag)
		{
			xWModManifest.Save(GetManifestPath(projectPath));
		}
		return flag;
	}

	public static bool RemovePath(string projectPath, string path)
	{
		projectPath = NormalizeProjectPath(projectPath);
		string text = ToProjectRelativeManifestPath(path, projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(text))
		{
			return false;
		}
		string manifestPath = GetManifestPath(projectPath);
		XWModManifest xWModManifest = XWModManifest.Load(manifestPath);
		if (xWModManifest == null)
		{
			return false;
		}
		bool num = RemoveManifestEntries(xWModManifest.Scripts, text) | RemoveManifestEntries(xWModManifest.Blueprints, text) | RemoveManifestEntries(xWModManifest.Translations, text) | RemoveManifestEntries(xWModManifest.Resources, text) | NormalizeManifestCollections(xWModManifest);
		if (num)
		{
			xWModManifest.Save(manifestPath);
		}
		return num;
	}

	public static bool MovePath(string projectPath, string from, string to)
	{
		projectPath = NormalizeProjectPath(projectPath);
		string text = ToProjectRelativeManifestPath(from, projectPath);
		string text2 = ToProjectRelativeManifestPath(to, projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || string.IsNullOrWhiteSpace(text) || string.IsNullOrWhiteSpace(text2))
		{
			return false;
		}
		XWModManifest xWModManifest = LoadOrCreateManifest(projectPath);
		List<string> list = new List<string>();
		bool flag = CollectMovedManifestEntries(xWModManifest.Scripts, text, text2, list);
		flag |= CollectMovedManifestEntries(xWModManifest.Blueprints, text, text2, list);
		flag |= CollectMovedManifestEntries(xWModManifest.Translations, text, text2, list);
		flag |= CollectMovedManifestEntries(xWModManifest.Resources, text, text2, list);
		foreach (string item in list)
		{
			flag |= AddManifestPath(xWModManifest, item);
		}
		if (!flag && File.Exists(ToAbsolutePath(to)))
		{
			flag |= AddManifestPath(xWModManifest, text2);
		}
		flag |= NormalizeManifestCollections(xWModManifest);
		if (flag)
		{
			xWModManifest.Save(GetManifestPath(projectPath));
		}
		return flag;
	}

	public static IEnumerable<string> EnumerateManifestFiles(string projectPath)
	{
		projectPath = NormalizeProjectPath(projectPath);
		if (string.IsNullOrWhiteSpace(projectPath) || !Directory.Exists(projectPath))
		{
			yield break;
		}
		foreach (string item in Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories))
		{
			if (!ShouldIgnoreManifestFile(projectPath, item))
			{
				yield return item;
			}
		}
	}

	public static string GetManifestSection(string relativePath)
	{
		string text = NormalizeManifestPath(relativePath);
		string text2 = Path.GetExtension(text).ToLowerInvariant();
		if (text2 == ".cs")
		{
			return "Scripts";
		}
		if (text.StartsWith("Scripts/", StringComparison.OrdinalIgnoreCase))
		{
			if ((!(text2 == ".tres") && !(text2 == ".res")) || 1 == 0)
			{
				return "Resources";
			}
			return "Blueprints";
		}
		if (text.StartsWith("Blueprints/", StringComparison.OrdinalIgnoreCase))
		{
			return "Blueprints";
		}
		bool flag = text.StartsWith("Resources/Characters/", StringComparison.OrdinalIgnoreCase) && text.Contains("/Script/", StringComparison.OrdinalIgnoreCase);
		if (flag)
		{
			bool flag2 = ((text2 == ".tres" || text2 == ".res") ? true : false);
			flag = flag2;
		}
		if (flag)
		{
			return "Blueprints";
		}
		if (text.StartsWith("Localization/", StringComparison.OrdinalIgnoreCase))
		{
			return "Translations";
		}
		return "Resources";
	}

	public static string ToProjectRelativeManifestPath(string path, string projectPath)
	{
		if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(projectPath))
		{
			return "";
		}
		string text = path.Replace('\\', '/').Trim();
		if (!Path.IsPathRooted(text) && !text.Contains("://", StringComparison.Ordinal))
		{
			string text2 = NormalizeManifestPath(text);
			if (text2 == "." || text2.StartsWith("../", StringComparison.Ordinal))
			{
				return "";
			}
			return text2;
		}
		string path2 = ToAbsolutePath(path);
		string relativeTo = NormalizeProjectPath(projectPath);
		try
		{
			string text3 = Path.GetRelativePath(relativeTo, path2).Replace('\\', '/');
			if (text3 == "." || text3.StartsWith("../", StringComparison.Ordinal) || Path.IsPathRooted(text3))
			{
				return "";
			}
			return NormalizeManifestPath(text3);
		}
		catch
		{
			return "";
		}
	}

	public static bool NormalizeManifestCollections(XWModManifest manifest)
	{
		if (manifest == null)
		{
			return false;
		}
		bool flag = false;
		if (manifest.Dependencies == null)
		{
			manifest.Dependencies = new List<XWModDependency>();
			flag = true;
		}
		if (manifest.Conflicts == null)
		{
			manifest.Conflicts = new List<XWModDependency>();
			flag = true;
		}
		if (manifest.Provides == null)
		{
			manifest.Provides = new Dictionary<string, List<string>>();
			flag = true;
		}
		if (manifest.Overrides == null)
		{
			manifest.Overrides = new Dictionary<string, List<string>>();
			flag = true;
		}
		if (manifest.Scripts == null)
		{
			manifest.Scripts = new List<string>();
			flag = true;
		}
		if (manifest.Blueprints == null)
		{
			manifest.Blueprints = new List<string>();
			flag = true;
		}
		if (manifest.Translations == null)
		{
			manifest.Translations = new List<string>();
			flag = true;
		}
		if (manifest.Resources == null)
		{
			manifest.Resources = new List<string>();
			flag = true;
		}
		flag |= NormalizePathList(manifest.Scripts);
		flag |= NormalizePathList(manifest.Blueprints);
		flag |= NormalizePathList(manifest.Translations);
		return flag | NormalizePathList(manifest.Resources);
	}

	private static XWModManifest LoadOrCreateManifest(string projectPath)
	{
		XWModManifest xWModManifest = XWModManifest.Load(GetManifestPath(projectPath)) ?? new XWModManifest();
		string fileName = Path.GetFileName(projectPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
		if (string.IsNullOrWhiteSpace(xWModManifest.Id))
		{
			xWModManifest.Id = SanitizeKey(fileName);
		}
		if (string.IsNullOrWhiteSpace(xWModManifest.Name))
		{
			xWModManifest.Name = fileName;
		}
		NormalizeManifestCollections(xWModManifest);
		return xWModManifest;
	}

	private static bool AddManifestPath(XWModManifest manifest, string relative)
	{
		relative = NormalizeManifestPath(relative);
		if (string.IsNullOrWhiteSpace(relative))
		{
			return false;
		}
		return GetManifestSection(relative) switch
		{
			"Scripts" => AddUniqueManifestPath(manifest.Scripts, relative), 
			"Blueprints" => AddUniqueManifestPath(manifest.Blueprints, relative), 
			"Translations" => AddUniqueManifestPath(manifest.Translations, relative), 
			_ => AddUniqueManifestPath(manifest.Resources, relative), 
		};
	}

	private static bool ReplaceCollection(List<string> target, SortedSet<string> expected)
	{
		if (target == null)
		{
			target = new List<string>();
		}
		if ((from path in target.Select(NormalizeManifestPath)
			where !string.IsNullOrWhiteSpace(path)
			select path).ToList().SequenceEqual(expected))
		{
			return false;
		}
		target.Clear();
		target.AddRange(expected);
		return true;
	}

	private static bool CollectMovedManifestEntries(List<string> paths, string oldRelative, string newRelative, List<string> movedPaths)
	{
		if (paths == null)
		{
			return false;
		}
		bool result = false;
		for (int num = paths.Count - 1; num >= 0; num--)
		{
			string path = NormalizeManifestPath(paths[num]);
			if (PathMatchesManifestPrefix(path, oldRelative))
			{
				paths.RemoveAt(num);
				movedPaths.Add(ReplaceManifestPrefix(path, oldRelative, newRelative));
				result = true;
			}
		}
		return result;
	}

	private static bool RemoveManifestEntries(List<string> paths, string relative)
	{
		if (paths == null)
		{
			return false;
		}
		bool result = false;
		for (int num = paths.Count - 1; num >= 0; num--)
		{
			if (PathMatchesManifestPrefix(NormalizeManifestPath(paths[num]), relative))
			{
				paths.RemoveAt(num);
				result = true;
			}
		}
		return result;
	}

	private static bool AddUniqueManifestPath(List<string> paths, string path)
	{
		if (paths == null || string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		path = NormalizeManifestPath(path);
		foreach (string path2 in paths)
		{
			if (string.Equals(NormalizeManifestPath(path2), path, StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
		}
		paths.Add(path);
		return true;
	}

	private static bool NormalizePathList(List<string> paths)
	{
		if (paths == null)
		{
			return false;
		}
		List<string> list = (from path in paths.Select(NormalizeManifestPath)
			where !string.IsNullOrWhiteSpace(path)
			select path).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string path) => path, StringComparer.OrdinalIgnoreCase).ToList();
		if (paths.SequenceEqual(list))
		{
			return false;
		}
		paths.Clear();
		paths.AddRange(list);
		return true;
	}

	private static bool ShouldIgnoreManifestFile(string projectPath, string filePath)
	{
		return ShouldIgnoreManifestRelativePath(ToProjectRelativeManifestPath(filePath, projectPath));
	}

	private static bool ShouldIgnoreManifestRelativePath(string relative)
	{
		relative = NormalizeManifestPath(relative);
		if (string.IsNullOrWhiteSpace(relative))
		{
			return true;
		}
		string fileName = Path.GetFileName(relative);
		if (IgnoredFileNames.Contains(fileName))
		{
			return true;
		}
		if (IgnoredExtensions.Contains(Path.GetExtension(relative)))
		{
			return true;
		}
		string[] array = relative.Split('/');
		foreach (string item in array)
		{
			if (IgnoredDirectoryNames.Contains(item))
			{
				return true;
			}
		}
		return false;
	}

	private static string GetManifestPath(string projectPath)
	{
		return Path.Combine(NormalizeProjectPath(projectPath), "mod.json");
	}

	private static string NormalizeProjectPath(string projectPath)
	{
		if (string.IsNullOrWhiteSpace(projectPath))
		{
			return "";
		}
		try
		{
			return Path.GetFullPath(ToAbsolutePath(projectPath.TrimEnd('/', '\\')));
		}
		catch
		{
			return projectPath.Replace('\\', '/').TrimEnd('/', '\\');
		}
	}

	private static string ToAbsolutePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return "";
		}
		return path.Replace('\\', '/').TrimEnd('/');
	}

	private static string NormalizeManifestPath(string path)
	{
		return (path ?? "").Replace('\\', '/').Trim('/');
	}

	private static bool PathMatchesManifestPrefix(string path, string prefix)
	{
		path = NormalizeManifestPath(path);
		prefix = NormalizeManifestPath(prefix);
		if (!string.Equals(path, prefix, StringComparison.OrdinalIgnoreCase))
		{
			return path.StartsWith(prefix + "/", StringComparison.OrdinalIgnoreCase);
		}
		return true;
	}

	private static string ReplaceManifestPrefix(string path, string oldPrefix, string newPrefix)
	{
		path = NormalizeManifestPath(path);
		oldPrefix = NormalizeManifestPath(oldPrefix);
		newPrefix = NormalizeManifestPath(newPrefix);
		if (string.Equals(path, oldPrefix, StringComparison.OrdinalIgnoreCase))
		{
			return newPrefix;
		}
		return newPrefix + path.Substring(oldPrefix.Length);
	}

	private static string SanitizeKey(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "mod";
		}
		List<char> list = new List<char>(value.Length);
		string text = value.Trim();
		foreach (char c in text)
		{
			if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
			{
				list.Add(c);
			}
			else if (char.IsWhiteSpace(c) || c == '.' || c == '/')
			{
				list.Add('_');
			}
		}
		string text2 = new string(list.ToArray()).Trim('_');
		if (!string.IsNullOrWhiteSpace(text2))
		{
			return text2;
		}
		return "mod";
	}
}
