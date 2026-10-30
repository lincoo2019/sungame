using System;
using System.Collections.Generic;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugStackFrame
{
	public string Id { get; }

	public string DisplayName { get; }

	public string SourcePath { get; }

	public int Line { get; }

	public string BlueprintNodeId { get; }

	public IReadOnlyDictionary<string, XWModDebugVariable> Variables { get; }

	public XWModDebugStackFrame(string id, string displayName, string sourcePath = "", int line = 0, string blueprintNodeId = "", IReadOnlyDictionary<string, XWModDebugVariable> variables = null)
	{
		Id = (string.IsNullOrWhiteSpace(id) ? Guid.NewGuid().ToString("N") : id.Trim());
		DisplayName = (string.IsNullOrWhiteSpace(displayName) ? "未命名调用帧" : displayName.Trim());
		SourcePath = sourcePath?.Trim() ?? string.Empty;
		Line = Math.Max(0, line);
		BlueprintNodeId = blueprintNodeId?.Trim() ?? string.Empty;
		Variables = XWModDebugModelCopy.CopyVariables(variables);
	}

	internal XWModDebugStackFrame Copy()
	{
		return new XWModDebugStackFrame(Id, DisplayName, SourcePath, Line, BlueprintNodeId, Variables);
	}
}
