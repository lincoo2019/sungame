using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeClamp : XWBPNodeType
{
	public XWBPNodeClamp()
	{
		base.TypeId = "__XWBPGraphNode_Clamp";
		base.Category = "数学";
		base.DisplayName = "钳制";
		base.Description = "将值限制在范围内";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("最小值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("最大值", XWBPNodePortData.PortType.Float, "", 1.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			string inputValue2 = generator.GetInputValue(node, "最小值", function);
			string inputValue3 = generator.GetInputValue(node, "最大值", function);
			return $"Mathf.Clamp({inputValue}, {inputValue2}, {inputValue3})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "最小值", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "最大值", graph);
			return $"Mathf.Clamp({inputValueFromGraph}, {inputValueFromGraph2}, {inputValueFromGraph3})";
		}
		return "null";
	}
}
