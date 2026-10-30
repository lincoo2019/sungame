using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeToBool : XWBPNodeType
{
	public XWBPNodeToBool()
	{
		base.TypeId = "__XWBPGraphNode_ToBool";
		base.Category = "实用工具";
		base.DisplayName = "转布尔";
		base.Description = "将值转为 bool";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "值", function);
			return "Convert.ToBoolean(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			return "Convert.ToBoolean(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
