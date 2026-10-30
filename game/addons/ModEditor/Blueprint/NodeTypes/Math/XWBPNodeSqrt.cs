using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSqrt : XWBPNodeType
{
	public XWBPNodeSqrt()
	{
		base.TypeId = "__XWBPGraphNode_Sqrt";
		base.Category = "数学";
		base.DisplayName = "平方根";
		base.Description = "计算平方根";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Mathf.Sqrt(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Mathf.Sqrt(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
