using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringToLower : XWBPNodeType
{
	public XWBPNodeStringToLower()
	{
		base.TypeId = "__XWBPGraphNode_StringToLower";
		base.Category = "字符串";
		base.DisplayName = "转小写";
		base.Description = "将字符串转换为小写";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			return generator.GetInputValue(node, "字符串", function) + ".ToLower()";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			return generator.GetInputValueFromGraph(node, "字符串", graph) + ".ToLower()";
		}
		return "null";
	}
}
