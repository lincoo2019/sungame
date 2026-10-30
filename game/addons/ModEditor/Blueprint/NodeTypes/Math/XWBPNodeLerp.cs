using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLerp : XWBPNodeType
{
	public XWBPNodeLerp()
	{
		base.TypeId = "__XWBPGraphNode_Lerp";
		base.Category = "数学";
		base.DisplayName = "线性插值";
		base.Description = "浮点数线性插值";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("起点", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("终点", XWBPNodePortData.PortType.Float, "", 1.0);
		AddInput("权重", XWBPNodePortData.PortType.Float, "", 0.5);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "起点", function);
			string inputValue2 = generator.GetInputValue(node, "终点", function);
			string inputValue3 = generator.GetInputValue(node, "权重", function);
			return $"Mathf.Lerp({inputValue}, {inputValue2}, {inputValue3})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "起点", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "终点", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "权重", graph);
			return $"Mathf.Lerp({inputValueFromGraph}, {inputValueFromGraph2}, {inputValueFromGraph3})";
		}
		return "null";
	}
}
