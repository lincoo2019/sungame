using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringReplace : XWBPNodeType
{
	public XWBPNodeStringReplace()
	{
		base.TypeId = "__XWBPGraphNode_StringReplace";
		base.Category = "字符串";
		base.DisplayName = "字符串替换";
		base.Description = "替换字符串中的内容";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddInput("查找", XWBPNodePortData.PortType.String, "", "");
		AddInput("替换为", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			string inputValue2 = generator.GetInputValue(node, "查找", function);
			string inputValue3 = generator.GetInputValue(node, "替换为", function);
			return $"{inputValue}.Replace({inputValue2}, {inputValue3})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "查找", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "替换为", graph);
			return $"{inputValueFromGraph}.Replace({inputValueFromGraph2}, {inputValueFromGraph3})";
		}
		return "null";
	}
}
