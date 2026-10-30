using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringToFloat : XWBPNodeType
{
	public XWBPNodeStringToFloat()
	{
		base.TypeId = "__XWBPGraphNode_StringToFloat";
		base.Category = "实用工具";
		base.DisplayName = "字符串转浮点";
		base.Description = "将字符串解析为 float";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			return "float.Parse(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			return "float.Parse(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
