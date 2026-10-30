using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictContains : XWBPNodeType
{
	public XWBPNodeDictContains()
	{
		base.TypeId = "__XWBPGraphNode_DictContains";
		base.Category = "变量";
		base.DisplayName = "字典包含";
		base.Description = "检查字典是否包含键";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddInput("键", XWBPNodePortData.PortType.Any);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字典", function);
			string inputValue2 = generator.GetInputValue(node, "键", function);
			return inputValue + ".ContainsKey(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字典", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "键", graph);
			return inputValueFromGraph + ".ContainsKey(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
