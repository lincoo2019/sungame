namespace PVZHE.ModEditor.Blueprint;

public sealed class XWBlueprintRuntimeTracePoint
{
	public XWBPGraphData Graph { get; internal init; }

	public int GraphId { get; internal init; }

	public int FunctionId { get; internal init; }

	public string FunctionName { get; internal init; } = string.Empty;

	public int NodeId { get; internal init; }

	public string Location { get; internal init; } = string.Empty;
}
