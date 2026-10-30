using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModManifest
{
	public const int CurrentSchemaVersion = 2;

	[JsonPropertyName("schemaVersion")]
	public int SchemaVersion { get; set; } = 2;

	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("name")]
	public string Name { get; set; } = "";

	[JsonPropertyName("version")]
	public string Version { get; set; } = "1.0.0";

	[JsonPropertyName("author")]
	public string Author { get; set; } = "";

	[JsonPropertyName("description")]
	public string Description { get; set; } = "";

	[JsonPropertyName("dependencies")]
	public List<XWModDependency> Dependencies { get; set; } = new List<XWModDependency>();

	[JsonPropertyName("conflicts")]
	public List<XWModDependency> Conflicts { get; set; } = new List<XWModDependency>();

	[JsonPropertyName("provides")]
	public Dictionary<string, List<string>> Provides { get; set; } = new Dictionary<string, List<string>>();

	[JsonPropertyName("overrides")]
	public Dictionary<string, List<string>> Overrides { get; set; } = new Dictionary<string, List<string>>();

	[JsonPropertyName("scripts")]
	public List<string> Scripts { get; set; } = new List<string>();

	[JsonPropertyName("runtimeAssembly")]
	public string RuntimeAssembly { get; set; } = "";

	[JsonPropertyName("runtimeEntryType")]
	public string RuntimeEntryType { get; set; } = "";

	[JsonPropertyName("runtimeApiVersion")]
	public int RuntimeApiVersion { get; set; }

	[JsonPropertyName("runtimeAssemblyPolicy")]
	public string RuntimeAssemblyPolicy { get; set; } = "";

	[JsonPropertyName("blueprints")]
	public List<string> Blueprints { get; set; } = new List<string>();

	[JsonPropertyName("translations")]
	public List<string> Translations { get; set; } = new List<string>();

	[JsonPropertyName("resources")]
	public List<string> Resources { get; set; } = new List<string>();

	public static XWModManifest Load(string manifestPath)
	{
		if (!File.Exists(manifestPath))
		{
			return null;
		}
		return JsonSerializer.Deserialize(File.ReadAllText(manifestPath), XWModJsonContext.Default.XWModManifest);
	}

	public void Save(string manifestPath)
	{
		Directory.CreateDirectory(Path.GetDirectoryName(manifestPath) ?? ".");
		File.WriteAllText(manifestPath, JsonSerializer.Serialize(this, XWModJsonContext.Default.XWModManifest));
	}

	public bool IsRuntimeAssemblyRequired()
	{
		if (string.Equals(RuntimeAssemblyPolicy, "optional", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (string.IsNullOrWhiteSpace(RuntimeAssembly))
		{
			return !string.IsNullOrWhiteSpace(RuntimeEntryType);
		}
		return true;
	}
}
