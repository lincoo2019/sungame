using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayRemove : XWBPNodeType
{
	public XWBPNodeArrayRemove()
	{
		base.TypeId = "__XWBPGraphNode_ArrayRemove";
		base.Category = "变量";
		base.DisplayName = "数组移除";
		base.Description = "移除数组中首个匹配元素";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddFlowInput();
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddInput("元素", XWBPNodePortData.PortType.Any);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "数组", function);
		string inputValue2 = generator.GetInputValue(node, "元素", function);
		generator.AddLine(inputValue + ".Remove(" + inputValue2 + ");");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
		string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "元素", graph);
		generator.AddLine(inputValueFromGraph + ".Remove(" + inputValueFromGraph2 + ");");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
