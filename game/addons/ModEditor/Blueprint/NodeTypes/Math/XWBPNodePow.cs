using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodePow : XWBPNodeType
{
	public XWBPNodePow()
	{
		base.TypeId = "__XWBPGraphNode_Pow";
		base.Category = "数学";
		base.DisplayName = "幂运算";
		base.Description = "计算基数的指数次幂";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("基数", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("指数", XWBPNodePortData.PortType.Float, "", 1.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "基数", function);
			string inputValue2 = generator.GetInputValue(node, "指数", function);
			return $"Mathf.Pow({inputValue}, {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "基数", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "指数", graph);
			return $"Mathf.Pow({inputValueFromGraph}, {inputValueFromGraph2})";
		}
		return "null";
	}
}
