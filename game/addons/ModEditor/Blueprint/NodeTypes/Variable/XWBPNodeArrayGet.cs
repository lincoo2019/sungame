using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayGet : XWBPNodeType
{
	public XWBPNodeArrayGet()
	{
		base.TypeId = "__XWBPGraphNode_ArrayGet";
		base.Category = "变量";
		base.DisplayName = "数组获取";
		base.Description = "按索引获取数组元素";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddInput("索引", XWBPNodePortData.PortType.Integer, "", 0);
		AddOutput("元素", XWBPNodePortData.PortType.Any);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "元素")
		{
			string inputValue = generator.GetInputValue(node, "数组", function);
			string inputValue2 = generator.GetInputValue(node, "索引", function);
			return inputValue + "[" + inputValue2 + "]";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "元素")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "索引", graph);
			return inputValueFromGraph + "[" + inputValueFromGraph2 + "]";
		}
		return "null";
	}
}
