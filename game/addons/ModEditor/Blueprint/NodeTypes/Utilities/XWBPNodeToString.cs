using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeToString : XWBPNodeType
{
	public XWBPNodeToString()
	{
		base.TypeId = "__XWBPGraphNode_ToString";
		base.Category = "实用工具";
		base.DisplayName = "转字符串";
		base.Description = "将任意值转为字符串";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddOutput("字符串", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "字符串")
		{
			return generator.GetInputValue(node, "值", function) + ".ToString()";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "字符串")
		{
			return generator.GetInputValueFromGraph(node, "值", graph) + ".ToString()";
		}
		return "null";
	}
}
