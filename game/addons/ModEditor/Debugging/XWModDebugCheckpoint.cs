using System;
using System.Collections.Generic;
using System.Linq;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugCheckpoint
{
	public XWModDebugCheckpointKind Kind { get; }

	public string SourcePath { get; }

	public int Line { get; }

	public string BlueprintNodeId { get; }

	public string BlueprintNodeTypeId { get; }

	public string DisplayName { get; }

	public int FrameDepth { get; }

	public bool ForcePause { get; }

	public IReadOnlyList<XWModDebugStackFrame> CallStack { get; }

	public IReadOnlyDictionary<string, XWModDebugVariable> Variables { get; }

	private XWModDebugCheckpoint(XWModDebugCheckpointKind kind, string sourcePath, int line, string blueprintNodeId, string blueprintNodeTypeId, string displayName, int frameDepth, bool forcePause, IReadOnlyList<XWModDebugStackFrame> callStack, IReadOnlyDictionary<string, XWModDebugVariable> variables)
	{
		Kind = kind;
		SourcePath = XWModDebugModelCopy.NormalizeSourcePath(sourcePath);
		Line = Math.Max(0, line);
		BlueprintNodeId = blueprintNodeId?.Trim() ?? string.Empty;
		BlueprintNodeTypeId = blueprintNodeTypeId?.Trim() ?? string.Empty;
		DisplayName = ((!string.IsNullOrWhiteSpace(displayName)) ? displayName.Trim() : ((kind == XWModDebugCheckpointKind.CSharpLine) ? "C# 代码" : "蓝图节点"));
		CallStack = Array.AsReadOnly((from frame in callStack ?? Array.Empty<XWModDebugStackFrame>()
			where frame != null
			select frame.Copy()).ToArray());
		FrameDepth = ((frameDepth >= 0) ? frameDepth : Math.Max(0, CallStack.Count - 1));
		ForcePause = forcePause;
		Variables = XWModDebugModelCopy.CopyVariables(variables);
	}

	public static XWModDebugCheckpoint ForCSharp(string filePath, int line, string displayName = "", int frameDepth = -1, IReadOnlyList<XWModDebugStackFrame> callStack = null, IReadOnlyDictionary<string, XWModDebugVariable> variables = null, bool forcePause = false)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("C# 检查点必须指定文件路径。", "filePath");
		}
		if (line < 1)
		{
			throw new ArgumentOutOfRangeException("line", "C# 检查点行号必须从 1 开始。");
		}
		return new XWModDebugCheckpoint(XWModDebugCheckpointKind.CSharpLine, filePath, line, string.Empty, string.Empty, displayName, frameDepth, forcePause, callStack, variables);
	}

	public static XWModDebugCheckpoint ForBlueprint(string blueprintPath, string nodeId, string nodeDisplayName, string nodeTypeId = "", IReadOnlyList<XWModDebugStackFrame> callStack = null, IReadOnlyDictionary<string, XWModDebugVariable> variables = null, bool forcePause = false)
	{
		if (string.IsNullOrWhiteSpace(blueprintPath))
		{
			throw new ArgumentException("蓝图检查点必须指定蓝图路径。", "blueprintPath");
		}
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			throw new ArgumentException("蓝图检查点必须指定节点编号。", "nodeId");
		}
		return new XWModDebugCheckpoint(XWModDebugCheckpointKind.BlueprintNode, blueprintPath, 0, nodeId, nodeTypeId, nodeDisplayName, -1, forcePause, callStack, variables);
	}

	internal XWModDebugCheckpoint Copy()
	{
		return new XWModDebugCheckpoint(Kind, SourcePath, Line, BlueprintNodeId, BlueprintNodeTypeId, DisplayName, FrameDepth, ForcePause, CallStack, Variables);
	}
}
