using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Godot;

public static class ProjectResourceUidRepairTool
{
	private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

	private static readonly Regex ExtResourceRegex = new Regex("^\\s*\\[ext_resource\\b", RegexOptions.Compiled);

	private static readonly Regex UidRegex = new Regex("uid=\"(?<uid>uid://[^\"]+)\"", RegexOptions.Compiled);

	private static readonly Regex PathRegex = new Regex("path=\"(?<path>res://[^\"]+)\"", RegexOptions.Compiled);

	private static readonly Regex ResourceHeaderRegex = new Regex("^\\s*\\[(gd_scene|gd_resource)\\b", RegexOptions.Compiled);

	public static Task<ProjectResourceUidRepairResult> RepairProjectAsync(string searchRoot = "res://")
	{
		string projectRoot = ProjectSettings.GlobalizePath("res://");
		string scanRoot = ProjectSettings.GlobalizePath(searchRoot);
		return Task.Run(() => RepairProjectFiles(projectRoot, scanRoot));
	}

	private static ProjectResourceUidRepairResult RepairProjectFiles(string projectRoot, string scanRoot)
	{
		ProjectResourceUidRepairResult projectResourceUidRepairResult = new ProjectResourceUidRepairResult();
		StringBuilder stringBuilder = new StringBuilder(4096);
		Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.Ordinal);
		if (string.IsNullOrEmpty(projectRoot) || !Directory.Exists(projectRoot))
		{
			projectResourceUidRepairResult.Errors++;
			projectResourceUidRepairResult.Details = "Project root is missing.";
			return projectResourceUidRepairResult;
		}
		if (string.IsNullOrEmpty(scanRoot) || !Directory.Exists(scanRoot))
		{
			scanRoot = projectRoot;
		}
		foreach (string item in EnumerateRepairFiles(scanRoot))
		{
			projectResourceUidRepairResult.ScannedFiles++;
			try
			{
				RepairFile(projectRoot, item, projectResourceUidRepairResult, stringBuilder, dictionary);
			}
			catch (Exception ex)
			{
				projectResourceUidRepairResult.Errors++;
				AppendLimited(stringBuilder, "ERROR " + ToResourcePath(projectRoot, item) + ": " + ex.Message);
			}
		}
		projectResourceUidRepairResult.CollectedUidMappings = dictionary.Count;
		projectResourceUidRepairResult.UidMappings = BuildUidMappingText(dictionary);
		projectResourceUidRepairResult.Details = stringBuilder.ToString();
		return projectResourceUidRepairResult;
	}

	private static void RepairFile(string projectRoot, string filePath, ProjectResourceUidRepairResult result, StringBuilder details, Dictionary<string, string> uidMappings)
	{
		string[] array = SplitLines(File.ReadAllText(filePath, Encoding.UTF8));
		bool flag = false;
		int num = 0;
		string resourcePath = ToResourcePath(projectRoot, filePath);
		if (TryRepairResourceHeaderUid(array, resourcePath, result, details, uidMappings, out var fixedCount))
		{
			flag = true;
			num += fixedCount;
		}
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i];
			if (!ExtResourceRegex.IsMatch(text))
			{
				continue;
			}
			Match match = PathRegex.Match(text);
			Match match2 = UidRegex.Match(text);
			if (!match.Success || !match2.Success)
			{
				continue;
			}
			string value = match.Groups["path"].Value;
			string text2 = value;
			string value2 = match2.Groups["uid"].Value;
			if (!TryReadResourceUid(projectRoot, text2, out var uid, out var pathExists))
			{
				if (pathExists || !TryResolveMovedResourcePath(projectRoot, value, out var resolvedResourcePath) || !TryReadResourceUid(projectRoot, resolvedResourcePath, out uid, out pathExists))
				{
					if (pathExists)
					{
						result.MissingUids++;
						AppendLimited(details, "MISSING_UID " + ToResourcePath(projectRoot, filePath) + " -> " + value);
					}
					else
					{
						result.MissingPaths++;
						AppendLimited(details, "MISSING_PATH " + ToResourcePath(projectRoot, filePath) + " -> " + value);
					}
					continue;
				}
				text2 = resolvedResourcePath;
				result.FixedPaths++;
				AppendLimited(details, $"FIX_PATH {ToResourcePath(projectRoot, filePath)}: {value} -> {text2}");
			}
			TryAddUidMapping(uidMappings, uid, text2, result, details);
			if (!(value2 == uid) || !(value == text2))
			{
				array[i] = ReplaceExtResourceUidAndPath(text, match2.Groups["uid"], uid, match.Groups["path"], text2);
				flag = true;
				num++;
				if (value2 != uid)
				{
					result.FixedUids++;
				}
				AppendLimited(details, $"FIX {ToResourcePath(projectRoot, filePath)}: {value2} -> {uid} ({text2})");
			}
		}
		if (flag)
		{
			File.WriteAllText(filePath, string.Join("", array), Utf8NoBom);
			result.ModifiedFiles++;
			AppendLimited(details, $"WRITE {ToResourcePath(projectRoot, filePath)} fixed={num}");
		}
	}

	public static ProjectResourceUidRegistrationResult RegisterUidMappings(string uidMappings)
	{
		ProjectResourceUidRegistrationResult projectResourceUidRegistrationResult = new ProjectResourceUidRegistrationResult();
		StringBuilder stringBuilder = new StringBuilder(4096);
		if (string.IsNullOrWhiteSpace(uidMappings))
		{
			return projectResourceUidRegistrationResult;
		}
		string[] array = uidMappings.Split('\n', StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < array.Length; i++)
		{
			string text = array[i].TrimEnd('\r');
			int num = text.IndexOf('\t');
			if (num <= 0 || num >= text.Length - 1)
			{
				projectResourceUidRegistrationResult.Errors++;
				AppendLimited(stringBuilder, "REGISTER_BAD_LINE " + text);
				continue;
			}
			string text2 = text.Substring(0, num);
			string text3 = text.Substring(num + 1);
			long num2 = ResourceUid.TextToId(text2);
			if (num2 == -1)
			{
				projectResourceUidRegistrationResult.Errors++;
				AppendLimited(stringBuilder, "REGISTER_BAD_UID " + text2 + " -> " + text3);
				continue;
			}
			try
			{
				if (!ResourceUid.HasId(num2))
				{
					ResourceUid.AddId(num2, text3);
					projectResourceUidRegistrationResult.Registered++;
					continue;
				}
				string idPath = ResourceUid.GetIdPath(num2);
				if (idPath == text3)
				{
					projectResourceUidRegistrationResult.Unchanged++;
					continue;
				}
				ResourceUid.SetId(num2, text3);
				projectResourceUidRegistrationResult.Remapped++;
				AppendLimited(stringBuilder, $"REGISTER_REMAP {text2}: {idPath} -> {text3}");
			}
			catch (Exception ex)
			{
				projectResourceUidRegistrationResult.Errors++;
				AppendLimited(stringBuilder, $"REGISTER_ERROR {text2} -> {text3}: {ex.Message}");
			}
		}
		projectResourceUidRegistrationResult.Details = stringBuilder.ToString();
		return projectResourceUidRegistrationResult;
	}

	private static bool TryReadResourceUid(string projectRoot, string resourcePath, out string uid, out bool pathExists)
	{
		uid = "";
		pathExists = false;
		string text = ToGlobalPath(projectRoot, resourcePath);
		if (string.IsNullOrEmpty(text))
		{
			return false;
		}
		if (File.Exists(text))
		{
			pathExists = true;
			if (TryReadUidHeader(text, out uid))
			{
				return true;
			}
		}
		string path = text + ".uid";
		if (File.Exists(path))
		{
			pathExists = true;
			uid = File.ReadAllText(path, Encoding.UTF8).Trim();
			return uid.StartsWith("uid://", StringComparison.Ordinal);
		}
		string text2 = text + ".import";
		if (File.Exists(text2))
		{
			pathExists = true;
			return TryReadUidHeader(text2, out uid);
		}
		return false;
	}

	private static bool TryResolveMovedResourcePath(string projectRoot, string resourcePath, out string resolvedResourcePath)
	{
		resolvedResourcePath = "";
		foreach (string item in EnumerateMovedResourcePathCandidates(resourcePath))
		{
			string text = ToGlobalPath(projectRoot, item);
			if (!string.IsNullOrEmpty(text) && File.Exists(text))
			{
				resolvedResourcePath = item;
				return true;
			}
		}
		return false;
	}

	private static IEnumerable<string> EnumerateMovedResourcePathCandidates(string resourcePath)
	{
		if (resourcePath.StartsWith("res://Assets/", StringComparison.Ordinal))
		{
			yield return "res://Asset/" + resourcePath.Substring("res://Assets/".Length);
		}
		if (resourcePath.StartsWith("res://Tool/MapEditor/Control/Properties/Type/", StringComparison.Ordinal))
		{
			yield return "res://Prefab/GUI/LevelEditor/Inspector/Properties/Type/" + resourcePath.Substring("res://Tool/MapEditor/Control/Properties/Type/".Length);
		}
	}

	private static string ReplaceExtResourceUidAndPath(string line, Group uidGroup, string newUid, Group pathGroup, string newPath)
	{
		if (uidGroup.Index > pathGroup.Index)
		{
			line = line.Remove(uidGroup.Index, uidGroup.Length).Insert(uidGroup.Index, newUid);
			return line.Remove(pathGroup.Index, pathGroup.Length).Insert(pathGroup.Index, newPath);
		}
		line = line.Remove(pathGroup.Index, pathGroup.Length).Insert(pathGroup.Index, newPath);
		return line.Remove(uidGroup.Index, uidGroup.Length).Insert(uidGroup.Index, newUid);
	}

	private static void TryAddUidMapping(Dictionary<string, string> uidMappings, string uid, string resourcePath, ProjectResourceUidRepairResult result, StringBuilder details)
	{
		if (!string.IsNullOrEmpty(uid) && uid.StartsWith("uid://", StringComparison.Ordinal) && !string.IsNullOrEmpty(resourcePath) && resourcePath.StartsWith("res://", StringComparison.Ordinal))
		{
			if (!uidMappings.TryGetValue(uid, out var value))
			{
				uidMappings[uid] = resourcePath;
			}
			else if (!(value == resourcePath))
			{
				result.Errors++;
				AppendLimited(details, $"UID_CONFLICT {uid}: {value} / {resourcePath}");
			}
		}
	}

	private static string BuildUidMappingText(Dictionary<string, string> uidMappings)
	{
		StringBuilder stringBuilder = new StringBuilder(uidMappings.Count * 48);
		foreach (KeyValuePair<string, string> uidMapping in uidMappings)
		{
			stringBuilder.Append(uidMapping.Key);
			stringBuilder.Append('\t');
			stringBuilder.Append(uidMapping.Value);
			stringBuilder.Append('\n');
		}
		return stringBuilder.ToString();
	}

	private static bool TryReadUidHeader(string filePath, out string uid)
	{
		uid = "";
		if (!IsSafeTextFile(filePath))
		{
			return false;
		}
		using StreamReader streamReader = new StreamReader(filePath, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
		for (int i = 0; i < 32; i++)
		{
			string text = streamReader.ReadLine();
			if (text == null)
			{
				break;
			}
			Match match = UidRegex.Match(text);
			if (match.Success)
			{
				uid = match.Groups["uid"].Value;
				return true;
			}
		}
		return false;
	}

	private static bool TryRepairResourceHeaderUid(string[] lines, string resourcePath, ProjectResourceUidRepairResult result, StringBuilder details, Dictionary<string, string> uidMappings, out int fixedCount)
	{
		fixedCount = 0;
		if (lines == null || lines.Length == 0)
		{
			return false;
		}
		for (int i = 0; i < Math.Min(8, lines.Length); i++)
		{
			string text = lines[i];
			if (ResourceHeaderRegex.IsMatch(text))
			{
				Match match = UidRegex.Match(text);
				if (!match.Success)
				{
					return false;
				}
				string value = match.Groups["uid"].Value;
				if (IsValidResourceUid(value))
				{
					TryAddUidMapping(uidMappings, value, resourcePath, result, details);
					return false;
				}
				string text2 = CreateValidResourceUid();
				lines[i] = text.Remove(match.Groups["uid"].Index, match.Groups["uid"].Length).Insert(match.Groups["uid"].Index, text2);
				TryAddUidMapping(uidMappings, text2, resourcePath, result, details);
				result.FixedUids++;
				fixedCount = 1;
				AppendLimited(details, $"FIX_HEADER_UID {resourcePath}: {value} -> {text2}");
				return true;
			}
		}
		return false;
	}

	private static bool IsValidResourceUid(string uid)
	{
		if (string.IsNullOrEmpty(uid) || !uid.StartsWith("uid://", StringComparison.Ordinal))
		{
			return false;
		}
		return ResourceUid.TextToId(uid) != -1;
	}

	private static string CreateValidResourceUid()
	{
		return ResourceUid.IdToText(ResourceUid.CreateId());
	}

	private static IEnumerable<string> EnumerateRepairFiles(string rootPath)
	{
		Stack<string> pending = new Stack<string>();
		pending.Push(rootPath);
		while (pending.Count > 0)
		{
			string directory = pending.Pop();
			if (ShouldSkipDirectory(directory))
			{
				continue;
			}
			string[] files;
			try
			{
				files = Directory.GetFiles(directory);
			}
			catch
			{
				continue;
			}
			string[] array = files;
			foreach (string text in array)
			{
				if (IsRepairTarget(text))
				{
					yield return text;
				}
			}
			string[] directories;
			try
			{
				directories = Directory.GetDirectories(directory);
			}
			catch
			{
				continue;
			}
			string[] array2 = directories;
			foreach (string item in array2)
			{
				pending.Push(item);
			}
		}
	}

	private static bool ShouldSkipDirectory(string path)
	{
		string fileName = Path.GetFileName(path);
		switch (fileName)
		{
		default:
			return fileName == "obj";
		case ".git":
		case ".godot":
		case ".vs":
		case "bin":
			return true;
		}
	}

	private static bool IsRepairTarget(string filePath)
	{
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		switch (text)
		{
		default:
			return text == ".json";
		case ".tscn":
		case ".tres":
		case ".gdshader":
			return true;
		}
	}

	private static bool IsSafeTextFile(string filePath)
	{
		string text = Path.GetExtension(filePath).ToLowerInvariant();
		switch (text)
		{
		default:
			return text == ".cs";
		case ".tscn":
		case ".tres":
		case ".gdshader":
		case ".json":
		case ".import":
		case ".uid":
			return true;
		}
	}

	private static string[] SplitLines(string text)
	{
		List<string> list = new List<string>();
		int num = 0;
		for (int i = 0; i < text.Length; i++)
		{
			if (text[i] == '\n')
			{
				list.Add(text.Substring(num, i - num + 1));
				num = i + 1;
			}
		}
		if (num < text.Length)
		{
			list.Add(text.Substring(num));
		}
		return list.ToArray();
	}

	private static string ToGlobalPath(string projectRoot, string resourcePath)
	{
		if (!resourcePath.StartsWith("res://", StringComparison.Ordinal))
		{
			return "";
		}
		string path = resourcePath.Substring("res://".Length).Replace('/', Path.DirectorySeparatorChar);
		return Path.Combine(projectRoot, path);
	}

	private static string ToResourcePath(string projectRoot, string globalPath)
	{
		string text = Path.GetRelativePath(projectRoot, globalPath).Replace('\\', '/');
		return "res://" + text;
	}

	private static void AppendLimited(StringBuilder builder, string line)
	{
		if (builder.Length <= 12000)
		{
			builder.AppendLine(line);
		}
	}
}
