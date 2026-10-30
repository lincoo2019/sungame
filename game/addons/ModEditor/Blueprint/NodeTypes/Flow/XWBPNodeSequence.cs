using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSequence : XWBPNodeType
{
	public XWBPNodeSequence()
	{
		base.TypeId = "__XWBPGraphNode_Sequence";
		base.Category = "流程控制";
		base.DisplayName = "顺序执行";
		base.Description = "按顺序依次执行多个分支";
		base.Color = new Color(0.2f, 0.4f, 0.6f);
		AddFlowInput();
		AddFlowOutput("0");
		AddFlowOutput("1");
		AddFlowOutput("2");
		AddFlowOutput("3");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		for (int i = 0; i < node.OutputPorts.Count; i++)
		{
			XWBPNodeData nextNode = generator.GetNextNode(node, i, function);
			if (nextNode != null)
			{
				generator.GenerateNodeCode(nextNode, function);
			}
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		for (int i = 0; i < node.OutputPorts.Count; i++)
		{
			XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, i, graph);
			if (nextNodeFromGraph != null)
			{
				generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
			}
		}
	}
}
