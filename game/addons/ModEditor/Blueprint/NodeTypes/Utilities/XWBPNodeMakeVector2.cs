using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMakeVector2 : XWBPNodeType
{
	public XWBPNodeMakeVector2()
	{
		base.TypeId = "__XWBPGraphNode_MakeVector2";
		base.Category = "实用工具";
		base.DisplayName = "构造 Vector2";
		base.Description = "从 X、Y 分量构造 Vector2";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("X", XWBPNodePortData.PortType.Float, "", 0.0);
		AddInput("Y", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Vector2);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "X", function);
			string inputValue2 = generator.GetInputValue(node, "Y", function);
			return $"new Vector2({inputValue}, {inputValue2})";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "X", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "Y", graph);
			return $"new Vector2({inputValueFromGraph}, {inputValueFromGraph2})";
		}
		return "null";
	}
}
