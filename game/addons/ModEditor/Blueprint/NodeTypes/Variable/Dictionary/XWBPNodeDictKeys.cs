using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictKeys : XWBPNodeType
{
	public XWBPNodeDictKeys()
	{
		base.TypeId = "__XWBPGraphNode_DictKeys";
		base.Category = "变量";
		base.DisplayName = "字典键集";
		base.Description = "获取字典所有键";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddOutput("键", XWBPNodePortData.PortType.Array);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "键")
		{
			return generator.GetInputValue(node, "字典", function) + ".Keys";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "键")
		{
			return generator.GetInputValueFromGraph(node, "字典", graph) + ".Keys";
		}
		return "null";
	}
}
