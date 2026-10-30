using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeBreakVector2 : XWBPNodeType
{
	public XWBPNodeBreakVector2()
	{
		base.TypeId = "__XWBPGraphNode_BreakVector2";
		base.Category = "实用工具";
		base.DisplayName = "拆分 Vector2";
		base.Description = "拆分 Vector2 为 X、Y 分量";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("向量", XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
		AddOutput("X", XWBPNodePortData.PortType.Float);
		AddOutput("Y", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "向量", function);
		if (!(portName == "X"))
		{
			if (portName == "Y")
			{
				return inputValue + ".Y";
			}
			return "null";
		}
		return inputValue + ".X";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "向量", graph);
		if (!(portName == "X"))
		{
			if (portName == "Y")
			{
				return inputValueFromGraph + ".Y";
			}
			return "null";
		}
		return inputValueFromGraph + ".X";
	}
}
