using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeBranch : XWBPNodeType
{
	public XWBPNodeBranch()
	{
		base.TypeId = "__XWBPGraphNode_Branch";
		base.Category = "流程控制";
		base.DisplayName = "分支";
		base.Description = "根据布尔条件选择执行路径";
		base.Color = new Color(0.2f, 0.4f, 0.6f);
		AddFlowInput();
		AddInput("条件", XWBPNodePortData.PortType.Bool, "", false);
		AddFlowOutput("真");
		AddFlowOutput("假");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "条件", function);
		generator.AddLine("if (" + inputValue + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
		generator.IndentLevel--;
		generator.AddLine("else");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNode2 = generator.GetNextNode(node, 1, function);
		if (nextNode2 != null)
		{
			generator.GenerateNodeCode(nextNode2, function);
		}
		generator.IndentLevel--;
		generator.AddLine("}");
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "条件", graph);
		generator.AddLine("if (" + inputValueFromGraph + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
		generator.IndentLevel--;
		generator.AddLine("else");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNodeFromGraph2 = generator.GetNextNodeFromGraph(node, 1, graph);
		if (nextNodeFromGraph2 != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph2, graph);
		}
		generator.IndentLevel--;
		generator.AddLine("}");
	}
}
