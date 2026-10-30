using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;

internal sealed class AdobeAnimateCpuPackagingCatalog
{
	private readonly record struct ExtResource(string Path, string Type);

	private static readonly StringComparer PathComparer = StringComparer.Ordinal;

	private static readonly HashSet<string> ExcludedRootDirectories = new HashSet<string>(new string[12]
	{
		".godot", ".git", ".codex-tmp", ".worktrees", ".superpowers", "docs", "export", "exports", "build", "Test",
		"Tests", "tools"
	}, StringComparer.OrdinalIgnoreCase);

	private static readonly HashSet<string> TextExtensions = new HashSet<string>(new string[8] { ".tscn", ".tres", ".cs", ".gd", ".json", ".cfg", ".ini", ".txt" }, StringComparer.OrdinalIgnoreCase);

	private const string GeneratedValidationReportSuffix = ".adobe-animate-cpu-validation.json";

	private static readonly Regex ExtResourceRegex = new Regex("\\[ext_resource\\s+(?<attributes>[^\\]]+)\\]", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex AttributeRegex = new Regex("(?<name>[A-Za-z_][A-Za-z0-9_]*)=\"(?<value>[^\"]*)\"", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex FlashAssignmentRegex = new Regex("flashAnimeData\\s*=\\s*ExtResource\\(\"(?<id>[^\"]+)\"\\)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex QuotedResourcePathRegex = new Regex("[\"'](?<path>(?:res|uid)://[^\"'\\r\\n]+)[\"']", RegexOptions.Compiled | RegexOptions.CultureInvariant);

	private static readonly Regex BootstrapAnimeDataPathsRegex = new Regex("BootstrapAnimeDataPaths\\s*=\\s*\\{(?<paths>.*?)\\};", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

	private static readonly Regex ExportPresetRegex = new Regex("^\\[preset\\.(?<id>\\d+)\\]\\r?\\n(?<body>.*?)(?=^\\[)", RegexOptions.Multiline | RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.CultureInvariant);

	public string ProjectRoot { get; private init; } = string.Empty;

	public IReadOnlyList<string> ScenePaths { get; private init; } = Array.Empty<string>();

	public IReadOnlyList<string> DefinitionPaths { get; private init; } = Array.Empty<string>();

	public IReadOnlyList<AdobeAnimateCpuConsumerReference> ConsumerReferences { get; private init; } = Array.Empty<AdobeAnimateCpuConsumerReference>();

	public IReadOnlyList<AdobeAnimateCpuConsumerReference> DedicatedRendererReferences { get; private init; } = Array.Empty<AdobeAnimateCpuConsumerReference>();

	public IReadOnlyList<AdobeAnimateCpuConsumerReference> UnclassifiedReferences { get; private init; } = Array.Empty<AdobeAnimateCpuConsumerReference>();

	public IReadOnlyList<AdobeAnimateCpuConsumerReference> SuspendedReferences { get; private init; } = Array.Empty<AdobeAnimateCpuConsumerReference>();

	public IReadOnlyList<string> ExternalTexturePaths { get; private init; } = Array.Empty<string>();

	public IReadOnlyList<string> InputPaths { get; private init; } = Array.Empty<string>();

	public static AdobeAnimateCpuPackagingCatalog BuildProjectCatalog()
	{
		string projectRoot = Path.GetFullPath(ProjectSettings.GlobalizePath("res://"));
		string[] array = (from path in Directory.EnumerateFiles(projectRoot, "*", SearchOption.AllDirectories)
			select ToResourcePath(projectRoot, path) into path
			where !IsExcluded(path)
			select path).OrderBy<string, string>((string path) => path, StringComparer.Ordinal).ToArray();
		Dictionary<string, string> dictionary = new Dictionary<string, string>(PathComparer);
		foreach (string text in array)
		{
			if (TextExtensions.Contains(Path.GetExtension(text)))
			{
				dictionary[text] = File.ReadAllText(ToAbsolutePath(projectRoot, text));
			}
		}
		SortedSet<string> sortedSet = new SortedSet<string>(PathComparer);
		SortedSet<string> sortedSet2 = new SortedSet<string>(PathComparer);
		SortedSet<string> sortedSet3 = new SortedSet<string>(PathComparer);
		SortedSet<string> sortedSet4 = new SortedSet<string>(PathComparer);
		List<AdobeAnimateCpuConsumerReference> list = new List<AdobeAnimateCpuConsumerReference>();
		foreach (KeyValuePair<string, string> item2 in dictionary)
		{
			if (item2.Key.EndsWith(".tres", StringComparison.OrdinalIgnoreCase))
			{
				Dictionary<string, ExtResource> extResources = ParseExtResources(item2.Value);
				if (LooksLikeAdobeAnimateData(item2.Value))
				{
					sortedSet.Add(item2.Key);
					AddTexturePaths(extResources, sortedSet3);
				}
				if (LooksLikeExternalVisualTextureRegistry(item2.Value))
				{
					sortedSet4.Add(item2.Key);
					AddTexturePaths(extResources, sortedSet2);
					AddTexturePaths(extResources, sortedSet3);
				}
			}
		}
		SortedSet<string> sortedSet5 = new SortedSet<string>(PathComparer);
		List<AdobeAnimateCpuConsumerReference> list2 = new List<AdobeAnimateCpuConsumerReference>();
		List<AdobeAnimateCpuConsumerReference> list3 = new List<AdobeAnimateCpuConsumerReference>();
		List<AdobeAnimateCpuConsumerReference> list4 = new List<AdobeAnimateCpuConsumerReference>();
		foreach (KeyValuePair<string, string> item3 in dictionary.OrderBy<KeyValuePair<string, string>, string>((KeyValuePair<string, string> pair) => pair.Key, PathComparer))
		{
			if (!item3.Key.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			Dictionary<string, ExtResource> dictionary2 = ParseExtResources(item3.Value);
			AddTexturePaths(dictionary2, sortedSet3);
			MatchCollection matchCollection = FlashAssignmentRegex.Matches(item3.Value);
			if (matchCollection.Count == 0)
			{
				continue;
			}
			sortedSet5.Add(item3.Key);
			for (int num2 = 0; num2 < matchCollection.Count; num2++)
			{
				string value = matchCollection[num2].Groups["id"].Value;
				if (!dictionary2.TryGetValue(value, out var value2))
				{
					list.Add(Unclassified(item3.Key, value, "unresolved-ext-resource", "flashAnimeData references an unknown ExtResource id."));
					continue;
				}
				string text2 = ResolveResourcePath(value2.Path);
				if (string.IsNullOrWhiteSpace(text2))
				{
					list.Add(Unclassified(item3.Key, value2.Path, "unresolved-animation-reference", "The animation resource path could not be resolved."));
					continue;
				}
				if (!sortedSet.Contains(text2))
				{
					if (!GodotObject.IsInstanceValid(ResourceLoader.Load<AdobeAnimateData>(text2, string.Empty, ResourceLoader.CacheMode.Ignore)))
					{
						list.Add(Unclassified(item3.Key, text2, "animation-resource-not-data", "The assigned resource is not loadable AdobeAnimateData."));
						continue;
					}
					sortedSet.Add(text2);
				}
				bool flag = item3.Value.Contains("AdobeAnimatePart", StringComparison.Ordinal);
				AdobeAnimateCpuConsumerReference item = new AdobeAnimateCpuConsumerReference(item3.Key, text2, flag ? AdobeAnimateConsumerKind.DedicatedRenderer : AdobeAnimateConsumerKind.AdobeAnimateSprite, flag ? "AdobeAnimatePart" : "AdobeAnimateSprite", "flashAnimeData = ExtResource(\"" + value + "\")");
				list2.Add(item);
				if (flag)
				{
					list3.Add(item);
				}
			}
		}
		ScanDynamicDefinitionReferences(dictionary, sortedSet, list2, list3, list);
		ScanExportSuspensions(dictionary, sortedSet, list2, list4);
		AdobeAnimateCpuConsumerReference[] array2 = list2.OrderBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.ResourcePath, PathComparer).ThenBy((AdobeAnimateCpuConsumerReference reference) => reference.Kind)
			.ToArray();
		AdobeAnimateCpuConsumerReference[] dedicatedRendererReferences = list3.OrderBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.ResourcePath, PathComparer).ToArray();
		AdobeAnimateCpuConsumerReference[] unclassifiedReferences = list.OrderBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.ResourcePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.Evidence, PathComparer)
			.ToArray();
		AdobeAnimateCpuConsumerReference[] array3 = list4.OrderBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.ResourcePath, PathComparer).ThenBy<AdobeAnimateCpuConsumerReference, string>((AdobeAnimateCpuConsumerReference reference) => reference.Evidence, PathComparer)
			.ToArray();
		string[] inputPaths = sortedSet5.Concat(sortedSet).Concat(sortedSet4).Concat(sortedSet3)
			.Concat(array2.Select((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath))
			.Concat(array3.Select((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath))
			.Distinct<string>(PathComparer)
			.OrderBy<string, string>((string path) => path, PathComparer)
			.ToArray();
		return new AdobeAnimateCpuPackagingCatalog
		{
			ProjectRoot = projectRoot,
			ScenePaths = sortedSet5.ToArray(),
			DefinitionPaths = sortedSet.ToArray(),
			ConsumerReferences = array2,
			DedicatedRendererReferences = dedicatedRendererReferences,
			UnclassifiedReferences = unclassifiedReferences,
			SuspendedReferences = array3,
			ExternalTexturePaths = sortedSet2.ToArray(),
			InputPaths = inputPaths
		};
	}

	private static void ScanExportSuspensions(Dictionary<string, string> textByPath, SortedSet<string> definitionPaths, List<AdobeAnimateCpuConsumerReference> consumers, List<AdobeAnimateCpuConsumerReference> suspended)
	{
		if (!textByPath.TryGetValue("res://export_presets.cfg", out var value))
		{
			return;
		}
		foreach (Match item2 in ExportPresetRegex.Matches(value))
		{
			string value2 = item2.Groups["body"].Value;
			string configString = GetConfigString(value2, "export_filter");
			string configString2 = GetConfigString(value2, "export_path");
			if (!configString.Equals("exclude", StringComparison.Ordinal) || string.IsNullOrWhiteSpace(configString2))
			{
				continue;
			}
			string configString3 = GetConfigString(value2, "name");
			string value3 = item2.Groups["id"].Value;
			foreach (Match item3 in QuotedResourcePathRegex.Matches(value2))
			{
				string text = ResolveResourcePath(item3.Groups["path"].Value);
				if (!string.IsNullOrWhiteSpace(text) && definitionPaths.Contains(text))
				{
					AdobeAnimateCpuConsumerReference item = new AdobeAnimateCpuConsumerReference("res://export_presets.cfg", text, AdobeAnimateConsumerKind.Suspended, configString3, $"preset={value3}:{configString3}, export_filter=exclude, export_path={configString2}");
					consumers.Add(item);
					suspended.Add(item);
				}
			}
		}
	}

	private static string GetConfigString(string body, string key)
	{
		Match match = Regex.Match(body, "^" + Regex.Escape(key) + "=\"(?<value>[^\"]*)\"\\r?$", RegexOptions.Multiline | RegexOptions.CultureInvariant);
		if (!match.Success)
		{
			return string.Empty;
		}
		return match.Groups["value"].Value;
	}

	private static void ScanDynamicDefinitionReferences(Dictionary<string, string> textByPath, SortedSet<string> definitionPaths, List<AdobeAnimateCpuConsumerReference> consumers, List<AdobeAnimateCpuConsumerReference> dedicated, List<AdobeAnimateCpuConsumerReference> unclassified)
	{
		HashSet<string> hashSet = new HashSet<string>(consumers.Select((AdobeAnimateCpuConsumerReference reference) => reference.EvidencePath + "\n" + reference.ResourcePath), StringComparer.Ordinal);
		foreach (KeyValuePair<string, string> item3 in textByPath.OrderBy<KeyValuePair<string, string>, string>((KeyValuePair<string, string> pair) => pair.Key, PathComparer))
		{
			if (item3.Key.Equals("res://export_presets.cfg", StringComparison.Ordinal))
			{
				continue;
			}
			string extension = Path.GetExtension(item3.Key);
			if (!extension.Equals(".cs", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".gd", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".json", StringComparison.OrdinalIgnoreCase) && !extension.Equals(".cfg", StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			foreach (Match item4 in QuotedResourcePathRegex.Matches(item3.Value))
			{
				string text = ResolveResourcePath(item4.Groups["path"].Value);
				if (string.IsNullOrWhiteSpace(text) || !definitionPaths.Contains(text) || IsBootstrapAtlasMetadataReference(item3.Value, item4))
				{
					continue;
				}
				string item = item3.Key + "\n" + text;
				if (hashSet.Add(item))
				{
					bool flag = item3.Value.Contains("AdobeAnimateSprite", StringComparison.Ordinal) || item3.Value.Contains("flashAnimeData", StringComparison.Ordinal);
					bool flag2 = item3.Value.Contains("AdobeAnimatePart", StringComparison.Ordinal);
					AdobeAnimateConsumerKind kind = (flag2 ? AdobeAnimateConsumerKind.DedicatedRenderer : ((!flag) ? AdobeAnimateConsumerKind.Unclassified : AdobeAnimateConsumerKind.AdobeAnimateSprite));
					AdobeAnimateCpuConsumerReference item2 = new AdobeAnimateCpuConsumerReference(item3.Key, text, kind, flag2 ? "AdobeAnimatePart" : (flag ? "AdobeAnimateSprite" : string.Empty), item4.Value);
					consumers.Add(item2);
					if (flag2)
					{
						dedicated.Add(item2);
					}
					else if (!flag)
					{
						unclassified.Add(item2);
					}
				}
			}
		}
	}

	private static bool IsBootstrapAtlasMetadataReference(string text, Match referenceMatch)
	{
		foreach (Match item in BootstrapAnimeDataPathsRegex.Matches(text))
		{
			Group obj = item.Groups["paths"];
			if (referenceMatch.Index >= obj.Index && referenceMatch.Index + referenceMatch.Length <= obj.Index + obj.Length)
			{
				return true;
			}
		}
		return false;
	}

	private static Dictionary<string, ExtResource> ParseExtResources(string text)
	{
		Dictionary<string, ExtResource> dictionary = new Dictionary<string, ExtResource>(StringComparer.Ordinal);
		foreach (Match item in ExtResourceRegex.Matches(text))
		{
			Dictionary<string, string> dictionary2 = new Dictionary<string, string>(StringComparer.Ordinal);
			foreach (Match item2 in AttributeRegex.Matches(item.Groups["attributes"].Value))
			{
				dictionary2[item2.Groups["name"].Value] = item2.Groups["value"].Value;
			}
			if (dictionary2.TryGetValue("id", out var value) && dictionary2.TryGetValue("path", out var value2))
			{
				dictionary2.TryGetValue("type", out var value3);
				dictionary[value] = new ExtResource(value2, value3 ?? string.Empty);
			}
		}
		return dictionary;
	}

	private static bool LooksLikeAdobeAnimateData(string text)
	{
		if (!text.Contains("script_class=\"AdobeAnimateData\"", StringComparison.Ordinal))
		{
			return text.Contains("AdobeAnimateEditor/Resource/AdobeAnimateData.cs", StringComparison.Ordinal);
		}
		return true;
	}

	private static bool LooksLikeExternalVisualTextureRegistry(string text)
	{
		if (!text.Contains("script_class=\"AdobeAnimateExternalVisualTextureRegistry\"", StringComparison.Ordinal))
		{
			return text.Contains("AdobeAnimateEditor/Runtime/AdobeAnimateExternalVisualTextureRegistry.cs", StringComparison.Ordinal);
		}
		return true;
	}

	private static void AddTexturePaths(Dictionary<string, ExtResource> extResources, SortedSet<string> output)
	{
		foreach (ExtResource value in extResources.Values)
		{
			if (value.Type.Contains("Texture", StringComparison.OrdinalIgnoreCase))
			{
				string text = ResolveResourcePath(value.Path);
				if (!string.IsNullOrWhiteSpace(text))
				{
					output.Add(text);
				}
			}
		}
	}

	private static AdobeAnimateCpuConsumerReference Unclassified(string evidencePath, string resourcePath, string code, string detail)
	{
		return new AdobeAnimateCpuConsumerReference(evidencePath, resourcePath, AdobeAnimateConsumerKind.Unclassified, string.Empty, code + ": " + detail);
	}

	private static string ResolveResourcePath(string path)
	{
		if (string.IsNullOrWhiteSpace(path))
		{
			return string.Empty;
		}
		if (path.StartsWith("uid://", StringComparison.Ordinal))
		{
			long num = ResourceUid.TextToId(path);
			if (num == -1 || !ResourceUid.HasId(num))
			{
				return string.Empty;
			}
			path = ResourceUid.GetIdPath(num);
		}
		if (!path.StartsWith("res://", StringComparison.Ordinal))
		{
			return string.Empty;
		}
		string text = path;
		int length = "res://".Length;
		return "res://" + text.Substring(length, text.Length - length).Replace('\\', '/');
	}

	private static bool IsExcluded(string resourcePath)
	{
		if (resourcePath.EndsWith(".adobe-animate-cpu-validation.json", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		int length = "res://".Length;
		string text = resourcePath.Substring(length, resourcePath.Length - length);
		int num = text.IndexOf('/');
		string item = ((num >= 0) ? text.Substring(0, num) : text);
		return ExcludedRootDirectories.Contains(item);
	}

	private static string ToResourcePath(string projectRoot, string absolutePath)
	{
		string text = Path.GetRelativePath(projectRoot, absolutePath).Replace('\\', '/');
		return "res://" + text;
	}

	private static string ToAbsolutePath(string projectRoot, string resourcePath)
	{
		int length = "res://".Length;
		return Path.Combine(projectRoot, resourcePath.Substring(length, resourcePath.Length - length).Replace('/', Path.DirectorySeparatorChar));
	}
}
