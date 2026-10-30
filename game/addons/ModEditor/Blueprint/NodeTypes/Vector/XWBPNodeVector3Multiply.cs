using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector3Multiply : XWBPNodeType
{
	public XWBPNodeVector3Multiply()
	{
		base.TypeId = "__XWBPGraphNode_Vector3Multiply";
		base.Category = "向量";
		base.DisplayName = "Vector3 乘法";
		base.Description = "Vector3 乘以标量";
		base.Color = new Color(0.3f, 0.5f, 0.7f);
		AddInput("向量", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddInput("标量", XWBPNodePortData.PortType.Float, "", 1.0);
		AddOutput("结果", XWBPNodePortData.PortType.Vector3);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "向量", function);
			string inputValue2 = generator.GetInputValue(node, "标量", function);
			return $"({inputValue} * {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "向量", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "标量", graph);
			return $"({inputValueFromGraph} * {inputValueFromGraph2})";
		}
		return "null";
	}
}
