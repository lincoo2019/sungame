using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeEntry : XWBPNodeType
{
	public XWBPNodeEntry()
	{
		base.TypeId = "__XWBPGraphNode_Entry";
		base.NodeTypeEnumValue = NodeTypeEnum.Entry;
		base.Category = "入口";
		base.DisplayName = "入口";
		base.Description = "蓝图执行入口点";
		base.Color = new Color(0.8f, 0.2f, 0.2f);
		AddFlowOutput();
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
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
