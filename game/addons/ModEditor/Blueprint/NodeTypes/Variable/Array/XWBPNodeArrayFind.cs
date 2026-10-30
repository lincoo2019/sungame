using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayFind : XWBPNodeType
{
	public XWBPNodeArrayFind()
	{
		base.TypeId = "__XWBPGraphNode_ArrayFind";
		base.Category = "变量";
		base.DisplayName = "数组查找";
		base.Description = "查找元素索引";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddInput("元素", XWBPNodePortData.PortType.Any);
		AddOutput("索引", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "索引")
		{
			string inputValue = generator.GetInputValue(node, "数组", function);
			string inputValue2 = generator.GetInputValue(node, "元素", function);
			return inputValue + ".IndexOf(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "索引")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "元素", graph);
			return inputValueFromGraph + ".IndexOf(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
