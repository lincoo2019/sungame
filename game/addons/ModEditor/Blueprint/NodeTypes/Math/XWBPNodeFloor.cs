using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeFloor : XWBPNodeType
{
	public XWBPNodeFloor()
	{
		base.TypeId = "__XWBPGraphNode_Floor";
		base.Category = "数学";
		base.DisplayName = "向下取整";
		base.Description = "向下取整为整数";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Mathf.FloorToInt(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Mathf.FloorToInt(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
