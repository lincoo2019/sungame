using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMakeVector3 : XWBPNodeType
{
	public XWBPNodeMakeVector3()
	{
		base.TypeId = "__XWBPGraphNode_MakeVector3";
		base.Category = "实用工具";
		base.DisplayName = "构造 Vector3";
		base.Description = "从 X、Y、Z 分量构造 Vector3";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("X", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("Y", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("Z", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Vector3);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "X", function);
			string inputValue2 = generator.GetInputValue(node, "Y", function);
			string inputValue3 = generator.GetInputValue(node, "Z", function);
			return $"new Vector3({inputValue}, {inputValue2}, {inputValue3})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "X", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "Y", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "Z", graph);
			return $"new Vector3({inputValueFromGraph}, {inputValueFromGraph2}, {inputValueFromGraph3})";
		}
		return "null";
	}
}
