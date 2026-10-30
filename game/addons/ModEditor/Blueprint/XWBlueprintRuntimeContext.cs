using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Godot;
using PVZHE.ModEditor.Debugging;

namespace PVZHE.ModEditor.Blueprint;

public sealed class XWBlueprintRuntimeContext
{
	public XWBlueprintRuntimeLimits Limits { get; init; } = new XWBlueprintRuntimeLimits();

	public CancellationToken CancellationToken { get; init; }

	public Func<int, Task> NodeVisitedAsync { get; init; }

	public Func<XWBlueprintRuntimeTracePoint, Task> NodeLocationVisitedAsync { get; init; }

	public Func<double, Task> DelayAsync { get; init; }

	public Action<string> Output { get; init; }

	public Action<string> EventTriggered { get; init; }

	public string EntryEventName { get; init; } = string.Empty;

	public string ModProjectRoot { get; init; } = string.Empty;

	public string SelfClassName { get; init; } = "BlueprintSelf";

	public IReadOnlyDictionary<string, Variant> SelfProperties { get; init; }

	public IReadOnlyDictionary<int, Variant> BlueprintVariables { get; init; }

	public XWBPScriptData ScriptData { get; init; }

	public XWModDebugController DebugController { get; init; }

	public string BlueprintSourcePath { get; init; } = string.Empty;
}
