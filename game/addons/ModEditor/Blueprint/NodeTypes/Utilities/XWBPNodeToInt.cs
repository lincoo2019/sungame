using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeToInt : XWBPNodeType
{
	public XWBPNodeToInt()
	{
		base.TypeId = "__XWBPGraphNode_ToInt";
		base.Category = "实用工具";
		base.DisplayName = "转整数";
		base.Description = "将值转为 int";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Convert.ToInt32(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Convert.ToInt32(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
