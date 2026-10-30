using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeBreakpoint : XWBPNodeType
{
	public XWBPNodeBreakpoint()
	{
		base.TypeId = "__XWBPGraphNode_Breakpoint";
		base.Category = "实用工具";
		base.DisplayName = "断点";
		base.Description = "标记断点位置";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddFlowInput();
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		generator.AddLine("System.Diagnostics.Debugger.Break();");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		generator.AddLine("System.Diagnostics.Debugger.Break();");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
