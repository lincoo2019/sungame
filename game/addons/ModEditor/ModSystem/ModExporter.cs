using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Godot;

namespace PVZHE.ModEditor.ModSystem;

public static class ModExporter
{
	public class ModInfo
	{
		public string Name { get; set; } = "未命名 Mod";

		public string Version { get; set; } = "1.0.0";

		public string Author { get; set; } = "未知";

		public string Description { get; set; } = "";

		public List<string> Files { get; set; } = new List<string>();
	}

	public static string Export(string modName, ModInfo info, List<string> resourcePaths)
	{
		string text = ProjectSettings.GlobalizePath("user://Mods/");
		Directory.CreateDirectory(text);
		string text2 = Path.Combine(text, GetPackageFileName(modName));
		if (resourcePaths == null)
		{
			resourcePaths = new List<string>();
		}
		string projectRoot = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
		List<string> list = resourcePaths.Select((string path2) => NormalizeProjectRelativePath(projectRoot, StripResourcePrefix(path2))).Where(ShouldPackageProjectFile).Distinct<string>(StringComparer.OrdinalIgnoreCase)
			.OrderBy<string, string>((string result) => result, StringComparer.OrdinalIgnoreCase)
			.ToList();
		info.Name = modName;
		info.Files = list;
		using (FileStream stream = new FileStream(text2, FileMode.Create))
		{
			using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
			using (Stream stream2 = zipArchive.CreateEntry("mod.json").Open())
			{
				string value = JsonSerializer.Serialize(CreateManifest(modName, info), XWModJsonContext.Default.XWModManifest);
				using StreamWriter streamWriter = new StreamWriter(stream2);
				streamWriter.Write(value);
			}
			foreach (string item in list)
			{
				string path = Path.Combine(projectRoot, item);
				if (!File.Exists(path))
				{
					GD.PrintErr("[ModExporter] 文件不存在，跳过: " + item);
					continue;
				}
				string entryName = item.Replace('\\', '/');
				using Stream destination = zipArchive.CreateEntry(entryName).Open();
				using FileStream fileStream = File.OpenRead(path);
				fileStream.CopyTo(destination);
			}
		}
		GD.Print("[ModExporter] 已导出 Mod: " + text2);
		return text2;
	}

	public static List<string> GetInstalledMods()
	{
		List<string> list = new List<string>();
		string path = ProjectSettings.GlobalizePath("user://Mods/");
		if (!Directory.Exists(path))
		{
			return list;
		}
		foreach (string item in Directory.EnumerateFiles(path, "*.pmod").OrderBy<string, string>((string result) => result, StringComparer.OrdinalIgnoreCase))
		{
			list.Add(item);
		}
		return list;
	}

	public static string ExportFromDirectory(string modName, ModInfo info, string projectDir, List<string> relativePaths, string outputDir, string runtimeAssemblyPath = "", CancellationToken cancellationToken = default(CancellationToken), XWModManifest manifestSnapshot = null, XWModExportSnapshot exportSnapshot = null)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrEmpty(outputDir))
		{
			outputDir = ProjectSettings.GlobalizePath("user://Mods/");
		}
		Directory.CreateDirectory(outputDir);
		string text = Path.Combine(outputDir, GetPackageFileName(modName));
		string text2 = text + "." + Guid.NewGuid().ToString("N") + ".tmp";
		info.Name = modName;
		if (relativePaths == null)
		{
			relativePaths = new List<string>();
		}
		string fullProjectDir = Path.GetFullPath(projectDir);
		List<string> list = (from path in relativePaths
			select NormalizeProjectRelativePath(fullProjectDir, path) into path
			where !string.IsNullOrWhiteSpace(path)
			select path).Where(ShouldPackageProjectFile).Distinct<string>(StringComparer.OrdinalIgnoreCase).OrderBy<string, string>((string path) => path, StringComparer.OrdinalIgnoreCase)
			.ToList();
		string text3 = "";
		List<(string, string)> list2 = new List<(string, string)>();
		if (!string.IsNullOrWhiteSpace(runtimeAssemblyPath))
		{
			text3 = Path.GetFullPath(runtimeAssemblyPath);
			if (!File.Exists(text3) || !Path.GetExtension(text3).Equals(".dll", StringComparison.OrdinalIgnoreCase))
			{
				throw new FileNotFoundException("声明的 Mod 运行程序集不存在或不是 DLL 文件。", text3);
			}
			list2.Add((text3, "Runtime/ModAssembly.dll"));
			string text4 = Path.ChangeExtension(text3, ".pdb");
			if (File.Exists(text4))
			{
				list2.Add((text4, "Runtime/ModAssembly.pdb"));
			}
			list2.AddRange(EnumerateRuntimeDependencyFiles(text3));
			HashSet<string> reservedRuntimeEntries = list2.Select<(string, string), string>(((string SourcePath, string EntryName) file) => file.EntryName).ToHashSet<string>(StringComparer.OrdinalIgnoreCase);
			list.RemoveAll((string path) => reservedRuntimeEntries.Contains(path.Replace('\\', '/')));
		}
		info.Files = list;
		string text5 = BuildRootManifestJson(fullProjectDir, info, string.IsNullOrWhiteSpace(text3) ? "" : "Runtime/ModAssembly.dll", manifestSnapshot);
		XWModManifest xWModManifest = JsonSerializer.Deserialize(text5, XWModJsonContext.Default.XWModManifest);
		foreach (string item in xWModManifest.Resources.Concat(xWModManifest.Blueprints).Concat(xWModManifest.Translations))
		{
			if (!list.Contains<string>(item.Replace('\\', '/'), StringComparer.OrdinalIgnoreCase))
			{
				throw new InvalidDataException("安装包缺少清单声明文件：" + item);
			}
		}
		try
		{
			using (FileStream stream = new FileStream(text2, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None))
			{
				using ZipArchive zipArchive = new ZipArchive(stream, ZipArchiveMode.Create);
				cancellationToken.ThrowIfCancellationRequested();
				using (Stream stream2 = zipArchive.CreateEntry("mod.json").Open())
				{
					using StreamWriter streamWriter = new StreamWriter(stream2);
					streamWriter.Write(text5);
				}
				foreach (string item2 in list)
				{
					cancellationToken.ThrowIfCancellationRequested();
					string text6 = Path.Combine(fullProjectDir, item2);
					if (!File.Exists(text6))
					{
						throw new FileNotFoundException("导出期间资源被删除，旧安装包未覆盖：" + item2, text6);
					}
					string entryName = item2.Replace('\\', '/');
					using Stream destination = zipArchive.CreateEntry(entryName).Open();
					using FileStream source = File.OpenRead(text6);
					CopyStream(source, destination, cancellationToken);
				}
				if (!string.IsNullOrWhiteSpace(text3))
				{
					foreach (var (sourcePath, entryName2) in list2)
					{
						AddFileEntry(zipArchive, entryName2, sourcePath, cancellationToken);
					}
				}
			}
			cancellationToken.ThrowIfCancellationRequested();
			using (ZipArchive zipArchive2 = ZipFile.OpenRead(text2))
			{
				exportSnapshot?.VerifyArchive(zipArchive2);
				foreach (string item3 in list.Concat(list2.Select<(string, string), string>(((string SourcePath, string EntryName) file) => file.EntryName)).Append("mod.json"))
				{
					if (zipArchive2.GetEntry(item3.Replace('\\', '/')) == null)
					{
						throw new InvalidDataException("安装包完整性检查失败：" + item3);
					}
				}
			}
			File.Move(text2, text, overwrite: true);
		}
		catch
		{
			try
			{
				if (File.Exists(text2))
				{
					File.Delete(text2);
				}
			}
			catch
			{
			}
			throw;
		}
		GD.Print("[ModExporter] 已导出 Mod: " + text);
		return text;
	}

	private static string GetPackageFileName(string modName)
	{
		string text = modName?.Trim() ?? "";
		bool flag = string.IsNullOrWhiteSpace(text);
		if (!flag)
		{
			bool flag2 = ((text == "." || text == "..") ? true : false);
			flag = flag2;
		}
		if (flag || text.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || !string.Equals(Path.GetFileName(text), text, StringComparison.Ordinal))
		{
			throw new InvalidDataException("Mod name is not a safe package file name: " + modName);
		}
		return text + ".pmod";
	}

	private static string StripResourcePrefix(string path)
	{
		if (path == null || !path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
		{
			return path;
		}
		return path.Substring("res://".Length);
	}

	private static string NormalizeProjectRelativePath(string fullProjectDir, string relativePath)
	{
		if (string.IsNullOrWhiteSpace(relativePath))
		{
			return "";
		}
		if (Path.IsPathRooted(relativePath))
		{
			throw new InvalidDataException("Mod project file must be relative: " + relativePath);
		}
		string fullPath = Path.GetFullPath(Path.Combine(fullProjectDir, relativePath));
		string value = fullProjectDir.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
		if (!fullPath.StartsWith(value, StringComparison.OrdinalIgnoreCase))
		{
			throw new InvalidDataException("Mod project file escapes the project directory: " + relativePath);
		}
		return Path.GetRelativePath(fullProjectDir, fullPath).Replace('\\', '/');
	}

	private static string BuildRootManifestJson(string projectDir, ModInfo info, string runtimeAssemblyEntry, XWModManifest manifestSnapshot = null)
	{
		string manifestPath = Path.Combine(projectDir, "mod.json");
		XWModManifest xWModManifest = manifestSnapshot;
		try
		{
			if (xWModManifest == null)
			{
				xWModManifest = XWModManifest.Load(manifestPath);
			}
		}
		catch
		{
		}
		if (xWModManifest == null)
		{
			xWModManifest = CreateManifest(info.Name, info);
		}
		xWModManifest.Id = (string.IsNullOrWhiteSpace(xWModManifest.Id) ? info.Name : xWModManifest.Id);
		xWModManifest.SchemaVersion = 2;
		xWModManifest.Name = info.Name;
		xWModManifest.Version = info.Version;
		xWModManifest.Author = info.Author;
		xWModManifest.Description = info.Description;
		xWModManifest.RuntimeAssembly = runtimeAssemblyEntry ?? "";
		if (!string.IsNullOrWhiteSpace(xWModManifest.RuntimeAssembly))
		{
			if (xWModManifest.RuntimeApiVersion != 0 && xWModManifest.RuntimeApiVersion != 1)
			{
				throw new InvalidDataException($"runtimeApiVersion={xWModManifest.RuntimeApiVersion} is incompatible; rebuild with the current toolchain.");
			}
			xWModManifest.RuntimeApiVersion = 1;
		}
		if (!string.IsNullOrWhiteSpace(xWModManifest.RuntimeEntryType) && string.IsNullOrWhiteSpace(xWModManifest.RuntimeAssembly))
		{
			throw new InvalidDataException("mod.json declares runtimeEntryType, but the project produced no runtime assembly.");
		}
		if (!string.IsNullOrWhiteSpace(xWModManifest.RuntimeAssembly) && string.IsNullOrWhiteSpace(xWModManifest.RuntimeAssemblyPolicy))
		{
			xWModManifest.RuntimeAssemblyPolicy = "required";
		}
		if (!XWModRuntimeCompatibility.ValidatePackage(xWModManifest, out var diagnostic))
		{
			throw new InvalidDataException(diagnostic);
		}
		return JsonSerializer.Serialize(xWModManifest, XWModJsonContext.Default.XWModManifest);
	}

	private static XWModManifest CreateManifest(string modName, ModInfo info)
	{
		string id = SanitizeManifestId(modName);
		return new XWModManifest
		{
			SchemaVersion = 2,
			Id = id,
			Name = (string.IsNullOrWhiteSpace(info?.Name) ? modName : info.Name),
			Version = (string.IsNullOrWhiteSpace(info?.Version) ? "1.0.0" : info.Version),
			Author = (info?.Author ?? ""),
			Description = (info?.Description ?? "")
		};
	}

	private static bool ShouldPackageProjectFile(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return false;
		}
		string text = path.Replace('\\', '/').Trim('/');
		string extension = Path.GetExtension(text);
		if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".sln", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".uid", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".import", StringComparison.OrdinalIgnoreCase) && !text.StartsWith(".build/", StringComparison.OrdinalIgnoreCase) && !text.StartsWith("bin/", StringComparison.OrdinalIgnoreCase))
		{
			return !text.StartsWith("obj/", StringComparison.OrdinalIgnoreCase);
		}
		return false;
	}

	private static IEnumerable<(string SourcePath, string EntryName)> EnumerateRuntimeDependencyFiles(string runtimeAssemblyPath)
	{
		string path = Path.GetDirectoryName(runtimeAssemblyPath) ?? "";
		string mainAssembly = Path.GetFullPath(runtimeAssemblyPath);
		if (!Directory.Exists(path))
		{
			yield break;
		}
		foreach (string dependency in Directory.EnumerateFiles(path, "*.dll", SearchOption.TopDirectoryOnly).OrderBy<string, string>((string path2) => Path.GetFileName(path2), StringComparer.OrdinalIgnoreCase))
		{
			if (!string.Equals(Path.GetFullPath(dependency), mainAssembly, StringComparison.OrdinalIgnoreCase))
			{
				string fileName = Path.GetFileName(dependency);
				yield return (SourcePath: dependency, EntryName: "Runtime/Dependencies/" + fileName);
				string text = Path.ChangeExtension(dependency, ".pdb");
				if (File.Exists(text))
				{
					yield return (SourcePath: text, EntryName: "Runtime/Dependencies/" + Path.GetFileName(text));
				}
			}
		}
	}

	private static string SanitizeManifestId(string value)
	{
		if (string.IsNullOrWhiteSpace(value))
		{
			return "mod";
		}
		string text = new string(value.Trim().Select(delegate(char character)
		{
			bool flag = char.IsLetterOrDigit(character);
			if (!flag)
			{
				bool flag2 = ((character == '-' || character == '_') ? true : false);
				flag = flag2;
			}
			return (!flag) ? '_' : character;
		}).ToArray()).Trim('_');
		if (!string.IsNullOrWhiteSpace(text))
		{
			return text;
		}
		return "mod";
	}

	private static void AddFileEntry(ZipArchive archive, string entryName, string sourcePath, CancellationToken cancellationToken)
	{
		using Stream destination = archive.CreateEntry(entryName.Replace('\\', '/')).Open();
		using FileStream source = File.OpenRead(sourcePath);
		CopyStream(source, destination, cancellationToken);
	}

	private static void CopyStream(Stream source, Stream destination, CancellationToken cancellationToken)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(81920);
		try
		{
			int count;
			while ((count = source.Read(array, 0, array.Length)) > 0)
			{
				cancellationToken.ThrowIfCancellationRequested();
				destination.Write(array, 0, count);
			}
		}
		finally
		{
			ArrayPool<byte>.Shared.Return(array);
		}
	}
}
