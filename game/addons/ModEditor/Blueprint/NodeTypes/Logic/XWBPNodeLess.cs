using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLess : XWBPNodeType
{
	public XWBPNodeLess()
	{
		base.TypeId = "__XWBPGraphNode_Less";
		base.Category = "逻辑";
		base.DisplayName = "小于";
		base.Color = new Color(0.6f, 0.3f, 0.6f);
		AddInput("A", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("B", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return $"({inputValue} < {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return $"({inputValueFromGraph} < {inputValueFromGraph2})";
		}
		return "null";
	}
}
