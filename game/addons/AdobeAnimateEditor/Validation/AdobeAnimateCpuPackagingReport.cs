using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;

internal sealed class AdobeAnimateCpuPackagingReport
{
	private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
	{
		WriteIndented = true,
		PropertyNamingPolicy = null,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never
	};

	private static readonly AdobeAnimateCpuPackagingJsonContext JsonContext = new AdobeAnimateCpuPackagingJsonContext(JsonOptions);

	[JsonPropertyOrder(0)]
	public int DefinitionCount { get; init; }

	[JsonPropertyOrder(1)]
	public int SceneCount { get; init; }

	[JsonPropertyOrder(2)]
	public int ClipCount { get; init; }

	[JsonPropertyOrder(3)]
	public int FrameCount { get; init; }

	[JsonPropertyOrder(4)]
	public int ExpectedVisibleItems { get; init; }

	[JsonPropertyOrder(5)]
	public int CpuMeshItems { get; init; }

	[JsonPropertyOrder(6)]
	public int NativeSpriteItems { get; init; }

	[JsonPropertyOrder(7)]
	public int PoseArrayDefinitions { get; init; }

	[JsonPropertyOrder(8)]
	public int RenderGraphCount { get; init; }

	[JsonPropertyOrder(9)]
	public int MaxRenderSlots { get; init; }

	[JsonPropertyOrder(10)]
	public int MaxMeshCapacity { get; init; }

	[JsonPropertyOrder(11)]
	public int StateTexels { get; init; }

	[JsonPropertyOrder(12)]
	public int ManagedVisualItems { get; init; }

	[JsonPropertyOrder(13)]
	public int NativeBehindItems { get; init; }

	[JsonPropertyOrder(14)]
	public int NativeFrontItems { get; init; }

	[JsonPropertyOrder(15)]
	public int CpuFallbackRoots { get; init; }

	[JsonPropertyOrder(16)]
	public int CpuValidationFailures { get; init; }

	[JsonPropertyOrder(17)]
	public string InputSignature { get; init; } = string.Empty;

	[JsonPropertyOrder(18)]
	public IReadOnlyList<AdobeAnimateCpuDefinitionSummary> Definitions { get; init; } = Array.Empty<AdobeAnimateCpuDefinitionSummary>();

	[JsonPropertyOrder(19)]
	public IReadOnlyList<AdobeAnimateCpuValidationError> Errors { get; init; } = Array.Empty<AdobeAnimateCpuValidationError>();

	[JsonPropertyOrder(12)]
	public bool Passed
	{
		get
		{
			if (Errors.Count == 0 && CpuFallbackRoots == 0)
			{
				return CpuValidationFailures == 0;
			}
			return false;
		}
	}

	public string ToExportSummary()
	{
		return $"passed={Passed} definitions={DefinitionCount} scenes={SceneCount} clips={ClipCount} frames={FrameCount} errors={Errors.Count} poseArrays={PoseArrayDefinitions} graphs={RenderGraphCount} maxRenderSlots={MaxRenderSlots} maxMeshCapacity={MaxMeshCapacity} cpuFallbackRoots={CpuFallbackRoots} cpuValidationFailures={CpuValidationFailures}";
	}

	public void WriteDeterministic(string reportPath)
	{
		if (string.IsNullOrWhiteSpace(reportPath))
		{
			throw new ArgumentException("A report path is required.", "reportPath");
		}
		string text = ((reportPath.StartsWith("res://", StringComparison.Ordinal) || reportPath.StartsWith("user://", StringComparison.Ordinal)) ? ProjectSettings.GlobalizePath(reportPath) : Path.GetFullPath(reportPath));
		string? directoryName = Path.GetDirectoryName(text);
		if (string.IsNullOrWhiteSpace(directoryName))
		{
			throw new IOException("The report path has no parent directory: " + text);
		}
		Directory.CreateDirectory(directoryName);
		string contents = JsonSerializer.Serialize(this, JsonContext.AdobeAnimateCpuPackagingReport).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n";
		File.WriteAllText(text, contents, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	internal static string ComputeInputSignature(string projectRoot, IReadOnlyList<string> normalizedResourcePaths)
	{
		StringBuilder stringBuilder = new StringBuilder();
		for (int i = 0; i < normalizedResourcePaths.Count; i++)
		{
			string text = normalizedResourcePaths[i];
			string text2 = text;
			int length = "res://".Length;
			string path = Path.Combine(projectRoot, text2.Substring(length, text2.Length - length).Replace('/', Path.DirectorySeparatorChar));
			stringBuilder.Append(text).Append('\n');
			if (!File.Exists(path))
			{
				stringBuilder.Append("<missing>\n");
				continue;
			}
			byte[] inArray = SHA256.HashData(File.ReadAllBytes(path));
			stringBuilder.Append(Convert.ToHexString(inArray)).Append('\n');
		}
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(stringBuilder.ToString())));
	}
}
