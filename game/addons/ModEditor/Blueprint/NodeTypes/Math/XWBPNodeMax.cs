using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMax : XWBPNodeType
{
	public XWBPNodeMax()
	{
		base.TypeId = "__XWBPGraphNode_Max";
		base.Category = "数学";
		base.DisplayName = "最大值";
		base.Description = "取两数最大值";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("A", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("B", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return $"Mathf.Max({inputValue}, {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return $"Mathf.Max({inputValueFromGraph}, {inputValueFromGraph2})";
		}
		return "null";
	}
}
