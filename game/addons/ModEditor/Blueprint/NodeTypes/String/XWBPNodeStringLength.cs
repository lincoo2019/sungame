using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringLength : XWBPNodeType
{
	public XWBPNodeStringLength()
	{
		base.TypeId = "__XWBPGraphNode_StringLength";
		base.Category = "字符串";
		base.DisplayName = "字符串长度";
		base.Description = "获取字符串长度";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddOutput("长度", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "长度")
		{
			return generator.GetInputValue(node, "字符串", function) + ".Length";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "长度")
		{
			return generator.GetInputValueFromGraph(node, "字符串", graph) + ".Length";
		}
		return "null";
	}
}
