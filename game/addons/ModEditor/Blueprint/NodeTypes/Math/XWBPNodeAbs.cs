using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeAbs : XWBPNodeType
{
	public XWBPNodeAbs()
	{
		base.TypeId = "__XWBPGraphNode_Abs";
		base.Category = "数学";
		base.DisplayName = "绝对值";
		base.Description = "计算浮点数绝对值";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Mathf.Abs(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Mathf.Abs(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
