using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLog : XWBPNodeType
{
	public XWBPNodeLog()
	{
		base.TypeId = "__XWBPGraphNode_Log";
		base.Category = "数学";
		base.DisplayName = "对数 (Log)";
		base.Description = "计算自然对数";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 1.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Mathf.Log(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Mathf.Log(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
