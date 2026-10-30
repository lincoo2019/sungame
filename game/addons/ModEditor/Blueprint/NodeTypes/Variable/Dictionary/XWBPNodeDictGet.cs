using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictGet : XWBPNodeType
{
	public XWBPNodeDictGet()
	{
		base.TypeId = "__XWBPGraphNode_DictGet";
		base.Category = "变量";
		base.DisplayName = "字典获取";
		base.Description = "按键获取字典值";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddInput("键", XWBPNodePortData.PortType.Any);
		AddOutput("值", XWBPNodePortData.PortType.Any);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "值")
		{
			string inputValue = generator.GetInputValue(node, "字典", function);
			string inputValue2 = generator.GetInputValue(node, "键", function);
			return inputValue + "[" + inputValue2 + "]";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "值")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字典", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "键", graph);
			return inputValueFromGraph + "[" + inputValueFromGraph2 + "]";
		}
		return "null";
	}
}
