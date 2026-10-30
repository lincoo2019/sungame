using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeStringConcat : XWBPNodeType
{
	public XWBPNodeStringConcat()
	{
		base.TypeId = "__XWBPGraphNode_StringConcat";
		base.Category = "字符串";
		base.DisplayName = "字符串拼接";
		base.Description = "拼接两个字符串";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
		AddInput("A", XWBPNodePortData.PortType.String, "", "");
		AddInput("B", XWBPNodePortData.PortType.String, "", "");
		AddOutput("结果", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "A", function);
			string inputValue2 = generator.GetInputValue(node, "B", function);
			return $"({inputValue} + {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "A", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "B", graph);
			return $"({inputValueFromGraph} + {inputValueFromGraph2})";
		}
		return "null";
	}
}
