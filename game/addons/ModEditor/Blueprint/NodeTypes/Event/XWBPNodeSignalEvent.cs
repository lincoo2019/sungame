using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSignalEvent : XWBPNodeType
{
	public int SignalId { get; set; }

	public XWBPNodeSignalEvent()
	{
		base.TypeId = "__XWBPGraphNode_SignalEvent";
		base.NodeTypeEnumValue = NodeTypeEnum.Entry;
		base.Category = "事件";
		base.DisplayName = "信号事件";
		base.Description = "信号触发时执行";
		base.Color = new Color(0.8f, 0.6f, 0.2f);
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		xWBPNodeData.SetMetaData("SignalId", SignalId);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		SignalId = nodeData.GetMetaData("SignalId").AsInt32();
	}

	public void BuildSignal(XWBPSignalData signalData)
	{
		ClearAllPort();
		AddFlowOutput();
		foreach (XWBPNodePortData input in signalData.Inputs)
		{
			AddOutput(input.Name, input.PortTypeValue, input.ClassName, input.DefaultValue);
		}
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
