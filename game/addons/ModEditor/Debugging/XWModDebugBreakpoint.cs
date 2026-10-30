using System;

namespace PVZHE.ModEditor.Debugging;

public sealed class XWModDebugBreakpoint : IEquatable<XWModDebugBreakpoint>
{
	public XWModDebugCheckpointKind Kind { get; }

	public string SourcePath { get; }

	public int Line { get; }

	public string BlueprintNodeId { get; }

	public string DisplayText { get; }

	private XWModDebugBreakpoint(XWModDebugCheckpointKind kind, string sourcePath, int line, string blueprintNodeId)
	{
		Kind = kind;
		SourcePath = XWModDebugModelCopy.NormalizeSourcePath(sourcePath);
		Line = line;
		BlueprintNodeId = blueprintNodeId?.Trim() ?? string.Empty;
		DisplayText = ((kind == XWModDebugCheckpointKind.CSharpLine) ? $"C#：{SourcePath} 第 {Line} 行" : ("蓝图：" + SourcePath + " / 节点 " + BlueprintNodeId));
	}

	public static XWModDebugBreakpoint ForCSharp(string filePath, int line)
	{
		if (string.IsNullOrWhiteSpace(filePath))
		{
			throw new ArgumentException("C# 断点必须指定文件路径。", "filePath");
		}
		if (line < 1)
		{
			throw new ArgumentOutOfRangeException("line", "C# 断点行号必须从 1 开始。");
		}
		return new XWModDebugBreakpoint(XWModDebugCheckpointKind.CSharpLine, filePath, line, string.Empty);
	}

	public static XWModDebugBreakpoint ForBlueprint(string blueprintPath, string nodeId)
	{
		if (string.IsNullOrWhiteSpace(blueprintPath))
		{
			throw new ArgumentException("蓝图断点必须指定蓝图路径。", "blueprintPath");
		}
		if (string.IsNullOrWhiteSpace(nodeId))
		{
			throw new ArgumentException("蓝图断点必须指定节点编号。", "nodeId");
		}
		return new XWModDebugBreakpoint(XWModDebugCheckpointKind.BlueprintNode, blueprintPath, 0, nodeId);
	}

	internal bool Matches(XWModDebugCheckpoint checkpoint)
	{
		if (checkpoint == null || Kind != checkpoint.Kind)
		{
			return false;
		}
		if (!string.Equals(SourcePath, XWModDebugModelCopy.NormalizeSourcePath(checkpoint.SourcePath), StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}
		if (Kind != XWModDebugCheckpointKind.CSharpLine)
		{
			return string.Equals(BlueprintNodeId, checkpoint.BlueprintNodeId, StringComparison.Ordinal);
		}
		return Line == checkpoint.Line;
	}

	public bool Equals(XWModDebugBreakpoint other)
	{
		if (other == null || Kind != other.Kind)
		{
			return false;
		}
		if (string.Equals(SourcePath, other.SourcePath, StringComparison.OrdinalIgnoreCase) && Line == other.Line)
		{
			return string.Equals(BlueprintNodeId, other.BlueprintNodeId, StringComparison.Ordinal);
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		return Equals(obj as XWModDebugBreakpoint);
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(Kind, StringComparer.OrdinalIgnoreCase.GetHashCode(SourcePath), Line, StringComparer.Ordinal.GetHashCode(BlueprintNodeId));
	}
}
