using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictSize : XWBPNodeType
{
	public XWBPNodeDictSize()
	{
		base.TypeId = "__XWBPGraphNode_DictSize";
		base.Category = "变量";
		base.DisplayName = "字典大小";
		base.Description = "获取字典元素个数";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddOutput("大小", XWBPNodePortData.PortType.Integer);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "大小")
		{
			return generator.GetInputValue(node, "字典", function) + ".Count";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "大小")
		{
			return generator.GetInputValueFromGraph(node, "字典", graph) + ".Count";
		}
		return "null";
	}
}
