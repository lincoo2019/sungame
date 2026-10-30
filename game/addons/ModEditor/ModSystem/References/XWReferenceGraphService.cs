using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Godot;

namespace PVZHE.ModEditor.ModSystem.References;

public sealed class XWReferenceGraphService
{
	public sealed class ReferenceGraph
	{
		public Dictionary<string, List<string>> References { get; } = new Dictionary<string, List<string>>();

		public Dictionary<string, List<string>> ReverseReferences { get; } = new Dictionary<string, List<string>>();

		public Dictionary<string, List<string>> MissingReferences { get; } = new Dictionary<string, List<string>>();
	}

	private static readonly Regex ReferenceRegex = new Regex("(uid://[A-Za-z0-9_]+|res://[^\\s\"'\\)\\]\\}]+)", RegexOptions.Compiled);

	private static readonly Regex RelativeReferenceRegex = new Regex("\\b(?:Scenes|Scripts|Resources|Assets)/[^\\s\"'\\)\\]\\},]+", RegexOptions.IgnoreCase | RegexOptions.Compiled);

	public ReferenceGraph BuildForProject(string projectPath)
	{
		ReferenceGraph referenceGraph = new ReferenceGraph();
		if (!Directory.Exists(projectPath))
		{
			return referenceGraph;
		}
		AddManifestReferences(referenceGraph, projectPath);
		foreach (string item in Directory.EnumerateFiles(projectPath, "*", SearchOption.AllDirectories))
		{
			if (!IsTextResource(item))
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
			foreach (Match item2 in ReferenceRegex.Matches(text))
			{
				AddReferenceAndMissing(referenceGraph, projectPath, item, item2.Value);
			}
			foreach (Match item3 in RelativeReferenceRegex.Matches(text))
			{
				if (!IsEmbeddedInResPath(text, item3.Index))
				{
					AddReferenceAndMissing(referenceGraph, projectPath, item, item3.Value);
				}
			}
		}
		return referenceGraph;
	}

	private static void AddManifestReferences(ReferenceGraph graph, string projectPath)
	{
		string manifestPath = Path.Combine(projectPath, "mod.json");
		XWModManifest xWModManifest = XWModManifest.Load(manifestPath);
		if (xWModManifest != null)
		{
			AddManifestList(graph, projectPath, manifestPath, xWModManifest.Resources);
			AddManifestList(graph, projectPath, manifestPath, xWModManifest.Scripts);
			AddManifestList(graph, projectPath, manifestPath, xWModManifest.Blueprints);
			AddManifestList(graph, projectPath, manifestPath, xWModManifest.Translations);
		}
	}

	private static void AddManifestList(ReferenceGraph graph, string projectPath, string manifestPath, IEnumerable<string> references)
	{
		if (references == null)
		{
			return;
		}
		foreach (string reference in references)
		{
			AddReferenceAndMissing(graph, projectPath, manifestPath, reference);
		}
	}

	private static void AddReferenceAndMissing(ReferenceGraph graph, string projectPath, string source, string target)
	{
		if (!string.IsNullOrWhiteSpace(target))
		{
			string text = NormalizeReference(target);
			AddReference(graph, source, text);
			if (!ReferenceExists(projectPath, text))
			{
				AddMissingReference(graph, source, text);
			}
		}
	}

	private static void AddReference(ReferenceGraph graph, string source, string target)
	{
		if (!graph.References.TryGetValue(source, out var value))
		{
			value = new List<string>();
			graph.References[source] = value;
		}
		if (!value.Contains(target))
		{
			value.Add(target);
		}
		if (!graph.ReverseReferences.TryGetValue(target, out var value2))
		{
			value2 = new List<string>();
			graph.ReverseReferences[target] = value2;
		}
		if (!value2.Contains(source))
		{
			value2.Add(source);
		}
	}

	private static void AddMissingReference(ReferenceGraph graph, string source, string target)
	{
		if (!graph.MissingReferences.TryGetValue(source, out var value))
		{
			value = new List<string>();
			graph.MissingReferences[source] = value;
		}
		if (!value.Contains(target))
		{
			value.Add(target);
		}
	}

	private static bool ReferenceExists(string projectPath, string reference)
	{
		if (string.IsNullOrWhiteSpace(reference))
		{
			return false;
		}
		if (reference.StartsWith("uid://") || reference.StartsWith("res://"))
		{
			return ResourceLoader.Exists(reference);
		}
		string path = (Path.IsPathRooted(reference) ? reference : Path.Combine(projectPath, reference.Replace('/', Path.DirectorySeparatorChar)));
		if (!File.Exists(path))
		{
			return Directory.Exists(path);
		}
		return true;
	}

	private static string NormalizeReference(string reference)
	{
		return (reference ?? "").Trim().TrimEnd(',', ';').Replace('\\', '/');
	}

	private static bool IsEmbeddedInResPath(string text, int matchIndex)
	{
		if (matchIndex < "res://".Length)
		{
			return false;
		}
		return string.Equals(text.Substring(matchIndex - "res://".Length, "res://".Length), "res://", StringComparison.OrdinalIgnoreCase);
	}

	private static bool IsTextResource(string file)
	{
		switch (Path.GetExtension(file).ToLowerInvariant())
		{
		case ".tscn":
		case ".tres":
		case ".json":
		case ".cs":
		case ".gd":
		case ".cfg":
		case ".txt":
			return true;
		default:
			return false;
		}
	}
}
