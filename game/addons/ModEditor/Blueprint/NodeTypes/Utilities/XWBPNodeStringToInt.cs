using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringToInt : XWBPNodeType
{
	public XWBPNodeStringToInt()
	{
		base.TypeId = "__XWBPGraphNode_StringToInt";
		base.Category = "实用工具";
		base.DisplayName = "字符串转整数";
		base.Description = "将字符串解析为 int";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			return "int.Parse(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			return "int.Parse(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
