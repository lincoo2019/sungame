using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeTimerEvent : XWBPNodeType
{
	public double Interval { get; set; } = 1.0;

	public XWBPNodeTimerEvent()
	{
		base.TypeId = "__XWBPGraphNode_TimerEvent";
		base.NodeTypeEnumValue = NodeTypeEnum.Entry;
		base.Category = "事件";
		base.DisplayName = "定时器";
		base.Description = "定时触发事件";
		base.Color = new Color(0.8f, 0.6f, 0.2f);
		AddFlowOutput();
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		xWBPNodeData.SetMetaData("Interval", Interval);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		Interval = nodeData.GetMetaData("Interval").AsDouble();
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
