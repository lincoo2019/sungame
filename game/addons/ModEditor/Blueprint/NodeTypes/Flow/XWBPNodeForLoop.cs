using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeForLoop : XWBPNodeType
{
	public XWBPNodeForLoop()
	{
		base.TypeId = "__XWBPGraphNode_ForLoop";
		base.Category = "流程控制";
		base.DisplayName = "For 循环";
		base.Description = "按整数范围循环";
		base.Color = new Color(0.2f, 0.6f, 0.4f);
		AddFlowInput();
		AddInput("起始", XWBPNodePortData.PortType.Integer, "", 0);
		AddInput("结束", XWBPNodePortData.PortType.Integer, "", 10);
		AddFlowOutput("循环");
		AddFlowOutput("完成");
		AddOutput("索引", XWBPNodePortData.PortType.Integer);
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "起始", function);
		string inputValue2 = generator.GetInputValue(node, "结束", function);
		string value = $"var_{node.Id}_index";
		generator.AddLine($"for (long {value} = {inputValue}; {value} < {inputValue2}; {value}++)");
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
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "起始", graph);
		string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "结束", graph);
		string value = $"var_{node.Id}_index";
		generator.AddLine($"for (long {value} = {inputValueFromGraph}; {value} < {inputValueFromGraph2}; {value}++)");
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

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "索引")
		{
			return $"var_{node.Id}_index";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "索引")
		{
			return $"var_{node.Id}_index";
		}
		return "null";
	}
}
