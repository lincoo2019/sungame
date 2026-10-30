using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeRadToDeg : XWBPNodeType
{
	public XWBPNodeRadToDeg()
	{
		base.TypeId = "__XWBPGraphNode_RadToDeg";
		base.Category = "数学";
		base.DisplayName = "弧度转角度";
		base.Description = "将弧度转换为角度";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("弧度", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("角度", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "角度")
		{
			string inputValue = generator.GetInputValue(node, "弧度", function);
			return "Mathf.RadToDeg(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "角度")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "弧度", graph);
			return "Mathf.RadToDeg(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
