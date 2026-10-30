using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeIsValid : XWBPNodeType
{
	public XWBPNodeIsValid()
	{
		base.TypeId = "__XWBPGraphNode_IsValid";
		base.Category = "逻辑";
		base.DisplayName = "是否有效";
		base.Description = "判断对象是否有效";
		base.Color = new Color(0.6f, 0.3f, 0.6f);
		AddInput("对象", XWBPNodePortData.PortType.Object);
		AddOutput("结果", XWBPNodePortData.PortType.Bool);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "对象", function);
			return "GodotObject.IsInstanceValid(" + inputValue + ")";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "对象", graph);
			return "GodotObject.IsInstanceValid(" + inputValueFromGraph + ")";
		}
		return "null";
	}
}
