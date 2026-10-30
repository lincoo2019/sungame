using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector3Dot : XWBPNodeType
{
	public XWBPNodeVector3Dot()
	{
		base.TypeId = "__XWBPGraphNode_Vector3Dot";
		base.Category = "向量";
		base.DisplayName = "Vector3 点积";
		base.Description = "计算两个 Vector3 的点积";
		base.Color = new Color(0.3f, 0.5f, 0.7f);
		AddInput("A", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddInput("B", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return inputValue + ".Dot(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return inputValueFromGraph + ".Dot(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
