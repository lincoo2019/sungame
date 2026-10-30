using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector2Length : XWBPNodeType
{
	public XWBPNodeVector2Length()
	{
		base.TypeId = "__XWBPGraphNode_Vector2Length";
		base.Category = "向量";
		base.DisplayName = "Vector2 长度";
		base.Description = "计算 Vector2 的长度";
		base.Color = new Color(0.3f, 0.5f, 0.7f);
		AddInput("向量", XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
		AddOutput("长度", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "长度")
		{
			return generator.GetInputValue(node, "向量", function) + ".Length()";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "长度")
		{
			return generator.GetInputValueFromGraph(node, "向量", graph) + ".Length()";
		}
		return "null";
	}
}
