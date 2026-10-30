using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeNot : XWBPNodeType
{
	public XWBPNodeNot()
	{
		base.TypeId = "__XWBPGraphNode_Not";
		base.Category = "逻辑";
		base.DisplayName = "非 (NOT)";
		base.Color = new Color(0.6f, 0.3f, 0.6f);
		AddInput("值", XWBPNodePortData.PortType.Bool, "", false);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "(!" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "(!" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
