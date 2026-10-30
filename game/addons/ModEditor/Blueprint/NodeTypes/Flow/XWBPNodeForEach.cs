using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeForEach : XWBPNodeType
{
	public XWBPNodeForEach()
	{
		base.TypeId = "__XWBPGraphNode_ForEach";
		base.Category = "流程控制";
		base.DisplayName = "ForEach 循环";
		base.Description = "遍历数组每个元素";
		base.Color = new Color(0.2f, 0.6f, 0.4f);
		AddFlowInput();
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddFlowOutput("循环");
		AddFlowOutput("完成");
		AddOutput("元素", XWBPNodePortData.PortType.Any);
		AddOutput("索引", XWBPNodePortData.PortType.Integer);
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "数组", function);
		string value = $"var_{node.Id}_element";
		string text = $"var_{node.Id}_index";
		generator.AddLine("long " + text + " = 0;");
		generator.AddLine($"foreach (var {value} in {inputValue})");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
		generator.AddLine(text + "++;");
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
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
		string value = $"var_{node.Id}_element";
		string text = $"var_{node.Id}_index";
		generator.AddLine("long " + text + " = 0;");
		generator.AddLine($"foreach (var {value} in {inputValueFromGraph})");
		generator.AddLine("{");
		generator.IndentLevel++;
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
		generator.AddLine(text + "++;");
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
		if (portName == "元素")
		{
			return $"var_{node.Id}_element";
		}
		if (portName == "索引")
		{
			return $"var_{node.Id}_index";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "元素")
		{
			return $"var_{node.Id}_element";
		}
		if (portName == "索引")
		{
			return $"var_{node.Id}_index";
		}
		return "null";
	}
}
