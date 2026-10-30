using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayContains : XWBPNodeType
{
	public XWBPNodeArrayContains()
	{
		base.TypeId = "__XWBPGraphNode_ArrayContains";
		base.Category = "变量";
		base.DisplayName = "数组包含";
		base.Description = "检查数组是否包含元素";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddInput("元素", XWBPNodePortData.PortType.Any);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "数组", function);
			string inputValue2 = generator.GetInputValue(node, "元素", function);
			return inputValue + ".Contains(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "元素", graph);
			return inputValueFromGraph + ".Contains(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
