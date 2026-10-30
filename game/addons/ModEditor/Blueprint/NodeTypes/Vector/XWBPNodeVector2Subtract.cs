using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector2Subtract : XWBPNodeType
{
	public XWBPNodeVector2Subtract()
	{
		base.TypeId = "__XWBPGraphNode_Vector2Subtract";
		base.Category = "向量";
		base.DisplayName = "Vector2 减法";
		base.Description = "Vector2 相减";
		base.Color = new Color(0.3f, 0.5f, 0.7f);
		AddInput("A", XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
		AddInput("B", XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
		AddOutput("结果", XWBPNodePortData.PortType.Vector2);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return $"({inputValue} - {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return $"({inputValueFromGraph} - {inputValueFromGraph2})";
		}
		return "null";
	}
}
