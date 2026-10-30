using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeRandomFloat : XWBPNodeType
{
	public XWBPNodeRandomFloat()
	{
		base.TypeId = "__XWBPGraphNode_RandomFloat";
		base.Category = "数学";
		base.DisplayName = "随机浮点数";
		base.Description = "生成范围内的随机浮点数";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("最小值", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("最大值", XWBPNodePortData.PortType.Float, "", 1.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "最小值", function);
			string inputValue2 = generator.GetInputValue(node, "最大值", function);
			return $"(float)GD.RandRange({inputValue}, {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "最小值", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "最大值", graph);
			return $"(float)GD.RandRange({inputValueFromGraph}, {inputValueFromGraph2})";
		}
		return "null";
	}
}
