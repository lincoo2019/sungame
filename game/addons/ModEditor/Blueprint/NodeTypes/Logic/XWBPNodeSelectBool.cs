using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSelectBool : XWBPNodeType
{
	public XWBPNodeSelectBool()
	{
		base.TypeId = "__XWBPGraphNode_SelectBool";
		base.Category = "逻辑";
		base.DisplayName = "选择";
		base.Description = "根据布尔条件选择两个值之一";
		base.Color = new Color(0.6f, 0.3f, 0.6f);
		AddInput("条件", XWBPNodePortData.PortType.Bool, "", false);
		AddInput("真值", XWBPNodePortData.PortType.Any);
		AddInput("假值", XWBPNodePortData.PortType.Any);
		AddOutput("结果", XWBPNodePortData.PortType.Any);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "条件", function);
			string inputValue2 = generator.GetInputValue(node, "真值", function);
			string inputValue3 = generator.GetInputValue(node, "假值", function);
			return $"({inputValue} ? {inputValue2} : {inputValue3})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "条件", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "真值", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "假值", graph);
			return $"({inputValueFromGraph} ? {inputValueFromGraph2} : {inputValueFromGraph3})";
		}
		return "null";
	}
}
