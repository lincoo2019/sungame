using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringSplit : XWBPNodeType
{
	public XWBPNodeStringSplit()
	{
		base.TypeId = "__XWBPGraphNode_StringSplit";
		base.Category = "字符串";
		base.DisplayName = "字符串分割";
		base.Description = "按分隔符分割字符串";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("字符串", XWBPNodePortData.PortType.String, "", "");
		AddInput("分隔符", XWBPNodePortData.PortType.String, "", " ");
		AddOutput("结果", XWBPNodePortData.PortType.Array);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "字符串", function);
			string inputValue2 = generator.GetInputValue(node, "分隔符", function);
			return inputValue + ".Split(" + inputValue2 + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字符串", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "分隔符", graph);
			return inputValueFromGraph + ".Split(" + inputValueFromGraph2 + ")";
		}
		return "null";
	}
}
