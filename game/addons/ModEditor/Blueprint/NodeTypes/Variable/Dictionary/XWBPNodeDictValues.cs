using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictValues : XWBPNodeType
{
	public XWBPNodeDictValues()
	{
		base.TypeId = "__XWBPGraphNode_DictValues";
		base.Category = "变量";
		base.DisplayName = "字典值集";
		base.Description = "获取字典所有值";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddOutput("值", XWBPNodePortData.PortType.Array);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "值")
		{
			return generator.GetInputValue(node, "字典", function) + ".Values";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "值")
		{
			return generator.GetInputValueFromGraph(node, "字典", graph) + ".Values";
		}
		return "null";
	}
}
