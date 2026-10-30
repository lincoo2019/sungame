using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeOnDestroy : XWBPNodeType
{
	public XWBPNodeOnDestroy()
	{
		base.TypeId = "__XWBPGraphNode_OnDestroy";
		base.NodeTypeEnumValue = NodeTypeEnum.Entry;
		base.Category = "事件";
		base.DisplayName = "销毁时";
		base.Description = "节点 _ExitTree() 时执行";
		base.Color = new Color(0.8f, 0.6f, 0.2f);
		AddFlowOutput();
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
