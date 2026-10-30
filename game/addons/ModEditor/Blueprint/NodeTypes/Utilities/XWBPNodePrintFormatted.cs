using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodePrintFormatted : XWBPNodeType
{
	public XWBPNodePrintFormatted()
	{
		base.TypeId = "__XWBPGraphNode_PrintFormatted";
		base.Category = "实用工具";
		base.DisplayName = "格式化打印";
		base.Description = "按格式字符串打印";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddFlowInput();
		AddInput("格式", XWBPNodePortData.PortType.String, "", "");
		AddInput("参数0", XWBPNodePortData.PortType.Any);
		AddInput("参数1", XWBPNodePortData.PortType.Any);
		AddInput("参数2", XWBPNodePortData.PortType.Any);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "格式", function);
		string inputValue2 = generator.GetInputValue(node, "参数0", function);
		string inputValue3 = generator.GetInputValue(node, "参数1", function);
		string inputValue4 = generator.GetInputValue(node, "参数2", function);
		generator.AddLine($"GD.Print(string.Format({inputValue}, {inputValue2}, {inputValue3}, {inputValue4}));");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "格式", graph);
		string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "参数0", graph);
		string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "参数1", graph);
		string inputValueFromGraph4 = generator.GetInputValueFromGraph(node, "参数2", graph);
		generator.AddLine($"GD.Print(string.Format({inputValueFromGraph}, {inputValueFromGraph2}, {inputValueFromGraph3}, {inputValueFromGraph4}));");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
