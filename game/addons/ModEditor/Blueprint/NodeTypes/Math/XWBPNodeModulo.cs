using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeModulo : XWBPNodeType
{
	public XWBPNodeModulo()
	{
		base.TypeId = "__XWBPGraphNode_Modulo";
		base.Category = "数学";
		base.DisplayName = "取模";
		base.Description = "计算整数取模";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("A", XWBPNodePortData.PortType.Integer, "", 0);
		AddInput("B", XWBPNodePortData.PortType.Integer, "", 1);
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return $"({inputValue} % {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return $"({inputValueFromGraph} % {inputValueFromGraph2})";
		}
		return "null";
	}
}
