using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeFloatToInt : XWBPNodeType
{
	public XWBPNodeFloatToInt()
	{
		base.TypeId = "__XWBPGraphNode_FloatToInt";
		base.Category = "实用工具";
		base.DisplayName = "浮点转整数";
		base.Description = "将 float 截断为 int";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "((int)" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "((int)" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
