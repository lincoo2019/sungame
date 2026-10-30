using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeGetType : XWBPNodeType
{
	public XWBPNodeGetType()
	{
		base.TypeId = "__XWBPGraphNode_GetType";
		base.Category = "实用工具";
		base.DisplayName = "获取类型";
		base.Description = "获取对象类型名";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("对象", XWBPNodePortData.PortType.Any);
		AddOutput("类型名", XWBPNodePortData.PortType.String);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "类型名")
		{
			return generator.GetInputValue(node, "对象", function) + ".GetType().Name";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "类型名")
		{
			return generator.GetInputValueFromGraph(node, "对象", graph) + ".GetType().Name";
		}
		return "null";
	}
}
