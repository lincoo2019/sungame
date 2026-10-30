using System;
using System.Collections.Generic;
using System.IO;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModMigrationService
{
	private static readonly (string LegacyPath, string CurrentPath)[] PathMigrations = new(string, string)[9]
	{
		("Resources/CardBanks", "Resources/PacketBank"),
		("Resources/CardBank", "Resources/PacketBank"),
		("Scripts/Features", "Battle/Features"),
		("Scripts/Processes", "Battle/Processes"),
		("Scripts/Components", "Battle/Components"),
		("Scripts/Characters", "Scripts"),
		("Scripts/Projectiles", "Scripts"),
		("Scripts/Shared", "Scripts"),
		("Blueprints", "Scripts")
	};

	public XWModManifest MigrateProject(string projectPath)
	{
		if (string.IsNullOrWhiteSpace(projectPath))
		{
			return null;
		}
		XWModProjectLayout.EnsureProjectLayout(projectPath);
		string manifestPath = Path.Combine(projectPath, "mod.json");
		XWModManifest xWModManifest = XWModManifest.Load(manifestPath);
		if (xWModManifest == null)
		{
			return null;
		}
		bool flag = false;
		flag |= MigrateLegacyDirectories(projectPath);
		flag |= MigrateManifestPaths(xWModManifest);
		if (xWModManifest.SchemaVersion < 2)
		{
			xWModManifest.SchemaVersion = 2;
			flag = true;
		}
		NormalizeManifestCollections(xWModManifest);
		if (flag)
		{
			BackupManifest(manifestPath);
			xWModManifest.Save(manifestPath);
		}
		XWModProjectLayout.EnsureProjectLayout(projectPath);
		return xWModManifest;
	}

	private static bool MigrateLegacyDirectories(string projectPath)
	{
		bool flag = false;
		(string, string)[] pathMigrations = PathMigrations;
		for (int i = 0; i < pathMigrations.Length; i++)
		{
			(string, string) tuple = pathMigrations[i];
			string sourceDirectory = Path.Combine(projectPath, tuple.Item1.Replace('/', Path.DirectorySeparatorChar));
			string targetDirectory = Path.Combine(projectPath, tuple.Item2.Replace('/', Path.DirectorySeparatorChar));
			flag |= MoveLegacyDirectory(sourceDirectory, targetDirectory);
		}
		return flag;
	}

	private static bool MoveLegacyDirectory(string sourceDirectory, string targetDirectory)
	{
		if (string.IsNullOrWhiteSpace(sourceDirectory) || string.IsNullOrWhiteSpace(targetDirectory) || !Directory.Exists(sourceDirectory))
		{
			return false;
		}
		string text = Path.GetFullPath(sourceDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		string text2 = Path.GetFullPath(targetDirectory).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
		if (string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		Directory.CreateDirectory(text2);
		foreach (string item in Directory.EnumerateDirectories(text, "*", SearchOption.AllDirectories))
		{
			string relativePath = Path.GetRelativePath(text, item);
			Directory.CreateDirectory(Path.Combine(text2, relativePath));
		}
		foreach (string item2 in Directory.EnumerateFiles(text, "*", SearchOption.AllDirectories))
		{
			string relativePath2 = Path.GetRelativePath(text, item2);
			string text3 = Path.Combine(text2, relativePath2);
			Directory.CreateDirectory(Path.GetDirectoryName(text3) ?? text2);
			if (!File.Exists(text3))
			{
				File.Move(item2, text3);
			}
		}
		TryDeleteEmptyDirectory(text);
		return true;
	}

	private static bool MigrateManifestPaths(XWModManifest manifest)
	{
		return (byte)(0u | (MigrateList(manifest.Resources) ? 1u : 0u) | (MigrateList(manifest.Scripts) ? 1u : 0u) | (MigrateList(manifest.Blueprints) ? 1u : 0u) | (MigrateList(manifest.Translations) ? 1u : 0u)) != 0;
	}

	private static bool MigrateList(List<string> paths)
	{
		if (paths == null)
		{
			return false;
		}
		bool result = false;
		for (int i = 0; i < paths.Count; i++)
		{
			string text = MigratePath(paths[i]);
			if (text != paths[i])
			{
				paths[i] = text;
				result = true;
			}
		}
		return result;
	}

	private static string MigratePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return path;
		}
		string text = path.Replace('\\', '/').TrimStart('/');
		(string, string)[] pathMigrations = PathMigrations;
		for (int i = 0; i < pathMigrations.Length; i++)
		{
			(string, string) tuple = pathMigrations[i];
			if (string.Equals(text, tuple.Item1, StringComparison.OrdinalIgnoreCase))
			{
				return tuple.Item2;
			}
			if (text.StartsWith(tuple.Item1 + "/", StringComparison.OrdinalIgnoreCase))
			{
				return tuple.Item2 + text.Substring(tuple.Item1.Length);
			}
		}
		return text;
	}

	private static void NormalizeManifestCollections(XWModManifest manifest)
	{
		XWModManifest xWModManifest = manifest;
		if (xWModManifest.Dependencies == null)
		{
			List<XWModDependency> list = (xWModManifest.Dependencies = new List<XWModDependency>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Conflicts == null)
		{
			List<XWModDependency> list = (xWModManifest.Conflicts = new List<XWModDependency>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Provides == null)
		{
			Dictionary<string, List<string>> dictionary = (xWModManifest.Provides = new Dictionary<string, List<string>>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Overrides == null)
		{
			Dictionary<string, List<string>> dictionary = (xWModManifest.Overrides = new Dictionary<string, List<string>>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Scripts == null)
		{
			List<string> list4 = (xWModManifest.Scripts = new List<string>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Blueprints == null)
		{
			List<string> list4 = (xWModManifest.Blueprints = new List<string>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Translations == null)
		{
			List<string> list4 = (xWModManifest.Translations = new List<string>());
		}
		xWModManifest = manifest;
		if (xWModManifest.Resources == null)
		{
			List<string> list4 = (xWModManifest.Resources = new List<string>());
		}
	}

	private static void BackupManifest(string manifestPath)
	{
		if (!File.Exists(manifestPath))
		{
			return;
		}
		string text = manifestPath + ".bak";
		if (!File.Exists(text))
		{
			File.Copy(manifestPath, text);
			return;
		}
		int num = 1;
		string text2;
		while (true)
		{
			text2 = manifestPath + $".bak.{num}";
			if (!File.Exists(text2))
			{
				break;
			}
			num++;
		}
		File.Copy(manifestPath, text2);
	}

	private static void TryDeleteEmptyDirectory(string directory)
	{
		try
		{
			if (Directory.Exists(directory) && Directory.GetFiles(directory, "*", SearchOption.AllDirectories).Length == 0)
			{
				Directory.Delete(directory, recursive: true);
			}
		}
		catch
		{
		}
	}
}
