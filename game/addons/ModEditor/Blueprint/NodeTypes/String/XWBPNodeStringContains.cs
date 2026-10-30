using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringContains : XWBPNodeType
{
	public XWBPNodeStringContains()
	{
		base.TypeId = "__XWBPGraphNode_StringContains";
		base.Category = "字符串";
		base.DisplayName = "字符串包含";
		base.Description = "检查字符串是否包含子串";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddInput("查找", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			string inputValue2 = generator.GetInputValue(node, "查找", function);
			return inputValue + ".Contains(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "查找", graph);
			return inputValueFromGraph + ".Contains(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
