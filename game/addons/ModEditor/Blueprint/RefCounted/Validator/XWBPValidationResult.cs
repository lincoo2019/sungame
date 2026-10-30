using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPValidationResult : RefCounted
{
	public enum Severity
	{
		Error,
		Warning,
		Info
	}

	public enum ErrorType
	{
		DeadLoop,
		UnconnectedPort,
		MissingMethod,
		MissingVariable,
		MissingSignal,
		MissingEntry,
		OrphanNode,
		InvalidConnection,
		DuplicateEntry,
		DuplicateSignalName,
		DuplicateVariableName,
		DuplicateFunctionName,
		DuplicateParameterName,
		MissingReturn,
		UnsupportedNode,
		GeneratedCode
	}

	public Severity ResultSeverity { get; set; }

	public ErrorType TypeError { get; set; }

	public string Message { get; set; } = "";

	public int NodeId { get; set; } = -1;

	public int PortIndex { get; set; } = -1;

	public XWBPGraphData GraphData { get; set; }

	public Vector2 Position { get; set; } = Vector2.Zero;

	public XWBPValidationResult()
	{
	}

	public XWBPValidationResult(ErrorType errorType, string message, Severity severity = Severity.Error)
	{
		TypeError = errorType;
		Message = message;
		ResultSeverity = severity;
	}

	public XWBPValidationResult SetNodeId(int nodeId)
	{
		NodeId = nodeId;
		return this;
	}

	public XWBPValidationResult SetPortIndex(int portIndex)
	{
		PortIndex = portIndex;
		return this;
	}

	public XWBPValidationResult SetGraphData(XWBPGraphData graphData)
	{
		GraphData = graphData;
		return this;
	}

	public XWBPValidationResult SetPosition(Vector2 position)
	{
		Position = position;
		return this;
	}

	public string GetSeverityText()
	{
		return ResultSeverity switch
		{
			Severity.Error => "错误", 
			Severity.Warning => "警告", 
			Severity.Info => "信息", 
			_ => "未知", 
		};
	}

	public string GetErrorTypeText()
	{
		return TypeError switch
		{
			ErrorType.DeadLoop => "死循环", 
			ErrorType.UnconnectedPort => "未连接端口", 
			ErrorType.MissingMethod => "方法丢失", 
			ErrorType.MissingVariable => "变量丢失", 
			ErrorType.MissingSignal => "信号丢失", 
			ErrorType.MissingEntry => "缺少入口", 
			ErrorType.OrphanNode => "孤立节点", 
			ErrorType.InvalidConnection => "无效连接", 
			ErrorType.DuplicateEntry => "重复入口", 
			ErrorType.DuplicateSignalName => "重复信号名", 
			ErrorType.DuplicateVariableName => "重复变量名", 
			ErrorType.DuplicateFunctionName => "重复函数名", 
			ErrorType.DuplicateParameterName => "重复参数名", 
			ErrorType.MissingReturn => "缺少返回节点", 
			ErrorType.UnsupportedNode => "不支持的节点", 
			ErrorType.GeneratedCode => "生成代码错误", 
			_ => "未知错误", 
		};
	}

	public override string ToString()
	{
		string text = $"[{GetSeverityText()}] {GetErrorTypeText()}: {Message}";
		if (NodeId >= 0)
		{
			text += $" (节点ID: {NodeId}";
			if (PortIndex >= 0)
			{
				text += $", 端口: {PortIndex}";
			}
			text += ")";
		}
		return text;
	}
}
