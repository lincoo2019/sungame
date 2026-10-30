using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector3Normalize : XWBPNodeType
{
	public XWBPNodeVector3Normalize()
	{
		base.TypeId = "__XWBPGraphNode_Vector3Normalize";
		base.Category = "向量";
		base.DisplayName = "Vector3 归一化";
		base.Description = "归一化 Vector3";
		base.Color = new Color(0.3f, 0.5f, 0.7f);
		AddInput("向量", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddOutput("结果", XWBPNodePortData.PortType.Vector3);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			return generator.GetInputValue(node, "向量", function) + ".Normalized()";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			return generator.GetInputValueFromGraph(node, "向量", graph) + ".Normalized()";
		}
		return "null";
	}
}
