using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringSubstring : XWBPNodeType
{
	public XWBPNodeStringSubstring()
	{
		base.TypeId = "__XWBPGraphNode_StringSubstring";
		base.Category = "字符串";
		base.DisplayName = "子字符串";
		base.Description = "截取子字符串（长度 -1 表示到末尾）";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddInput("起始位置", XWBPNodePortData.PortType.Integer, "", 0);
		AddInput("长度", XWBPNodePortData.PortType.Integer, "", -1);
		AddOutput("结果", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			string inputValue2 = generator.GetInputValue(node, "起始位置", function);
			string inputValue3 = generator.GetInputValue(node, "长度", function);
			return $"(({inputValue3} < 0) ? {inputValue}.Substring({inputValue2}) : {inputValue}.Substring({inputValue2}, System.Math.Max(0, {inputValue3})))";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "起始位置", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "长度", graph);
			return $"(({inputValueFromGraph3} < 0) ? {inputValueFromGraph}.Substring({inputValueFromGraph2}) : {inputValueFromGraph}.Substring({inputValueFromGraph2}, System.Math.Max(0, {inputValueFromGraph3})))";
		}
		return "null";
	}
}
