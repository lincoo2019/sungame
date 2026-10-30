using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSin : XWBPNodeType
{
	public XWBPNodeSin()
	{
		base.TypeId = "__XWBPGraphNode_Sin";
		base.Category = "数学";
		base.DisplayName = "正弦 (Sin)";
		base.Description = "计算弧度的正弦值";
		base.Color = new Color(0.4f, 0.3f, 0.2f);
		AddInput("角度(弧度)", XWBPNodePortData.PortType.Float, "", 0.0);
		AddOutput("结果", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "角度(弧度)", function);
			return "Mathf.Sin(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "角度(弧度)", graph);
			return "Mathf.Sin(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
