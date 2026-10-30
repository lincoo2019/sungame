using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeRandomInt : XWBPNodeType
{
	public XWBPNodeRandomInt()
	{
		base.TypeId = "__XWBPGraphNode_RandomInt";
		base.Category = "数学";
		base.DisplayName = "随机整数";
		base.Description = "生成范围内的随机整数";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("最小值", XWBPNodePortData.PortType.Integer, "", 0);
		AddInput("最大值", XWBPNodePortData.PortType.Integer, "", 100);
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "最小值", function);
			string inputValue2 = generator.GetInputValue(node, "最大值", function);
			return $"GD.RandRange({inputValue}, {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "最小值", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "最大值", graph);
			return $"GD.RandRange({inputValueFromGraph}, {inputValueFromGraph2})";
		}
		return "null";
	}
}
