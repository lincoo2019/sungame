using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeWhileLoop : XWBPNodeType
{
	public XWBPNodeWhileLoop()
	{
		base.TypeId = "__XWBPGraphNode_WhileLoop";
		base.Category = "流程控制";
		base.DisplayName = "While 循环";
		base.Description = "条件为真时循环";
		base.Color = new Color(0.2f, 0.6f, 0.4f);
		AddFlowInput();
		AddInput("条件", XWBPNodePortData.PortType.Bool, "", false);
		AddFlowOutput("循环");
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "条件", function);
		generator.AddLine("while (" + inputValue + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
		generator.IndentLevel--;
		generator.AddLine("}");
		XWBPNodeData nextNode2 = generator.GetNextNode(node, 1, function);
		if (nextNode2 != null)
		{
			generator.GenerateNodeCode(nextNode2, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "条件", graph);
		generator.AddLine("while (" + inputValueFromGraph + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
		generator.IndentLevel--;
		generator.AddLine("}");
		XWBPNodeData nextNodeFromGraph2 = generator.GetNextNodeFromGraph(node, 1, graph);
		if (nextNodeFromGraph2 != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph2, graph);
		}
	}
}
