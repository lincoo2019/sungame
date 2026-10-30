using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDegToRad : XWBPNodeType
{
	public XWBPNodeDegToRad()
	{
		base.TypeId = "__XWBPGraphNode_DegToRad";
		base.Category = "数学";
		base.DisplayName = "角度转弧度";
		base.Description = "将角度转换为弧度";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("角度", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("弧度", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "弧度")
		{
			string inputValue = generator.GetInputValue(node, "角度", function);
			return "Mathf.DegToRad(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "弧度")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "角度", graph);
			return "Mathf.DegToRad(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
