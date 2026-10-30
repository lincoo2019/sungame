using System.Text.Json.Serialization;

namespace PVZHE.ModEditor.ModSystem;

public sealed class XWModDependency
{
	[JsonPropertyName("id")]
	public string Id { get; set; } = "";

	[JsonPropertyName("version")]
	public string Version { get; set; } = "";
}
