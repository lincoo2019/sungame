using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodePrint : XWBPNodeType
{
	public XWBPNodePrint()
	{
		base.TypeId = "__XWBPGraphNode_Print";
		base.Category = "实用工具";
		base.DisplayName = "打印";
		base.Description = "输出到控制台";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddFlowInput();
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "值", function);
		generator.AddLine("GD.Print(" + inputValue + ");");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
		generator.AddLine("GD.Print(" + inputValueFromGraph + ");");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
