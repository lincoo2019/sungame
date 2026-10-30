using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayLength : XWBPNodeType
{
	public XWBPNodeArrayLength()
	{
		base.TypeId = "__XWBPGraphNode_ArrayLength";
		base.Category = "变量";
		base.DisplayName = "数组长度";
		base.Description = "获取数组元素个数";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddOutput("长度", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "长度")
		{
			return generator.GetInputValue(node, "数组", function) + ".Count";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "长度")
		{
			return generator.GetInputValueFromGraph(node, "数组", graph) + ".Count";
		}
		return "null";
	}
}
