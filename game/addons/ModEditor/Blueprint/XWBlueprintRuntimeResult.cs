using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public sealed class XWBlueprintRuntimeResult
{
	public bool Success { get; internal set; }

	public bool WasCancelled { get; internal set; }

	public int StepCount { get; internal set; }

	public string StopReason { get; internal set; } = string.Empty;

	public Variant ReturnValue { get; internal set; }

	public List<int> TraceNodeIds { get; } = new List<int>();

	public List<string> TraceLocations { get; } = new List<string>();

	public List<string> OutputMessages { get; } = new List<string>();

	public List<string> Diagnostics { get; } = new List<string>();
}
