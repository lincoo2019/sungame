using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeCustomEvent : XWBPNodeType
{
	public StringName EventName { get; set; } = "";

	public XWBPNodeCustomEvent()
	{
		base.TypeId = "__XWBPGraphNode_CustomEvent";
		base.NodeTypeEnumValue = NodeTypeEnum.Entry;
		base.Category = "事件";
		base.DisplayName = "自定义事件";
		base.Description = "自定义事件触发时执行";
		base.Color = new Color(0.8f, 0.6f, 0.2f);
		AddFlowOutput();
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		xWBPNodeData.SetMetaData("EventName", EventName);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		EventName = nodeData.GetMetaData("EventName").AsStringName();
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
