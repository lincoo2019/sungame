using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictClear : XWBPNodeType
{
	public XWBPNodeDictClear()
	{
		base.TypeId = "__XWBPGraphNode_DictClear";
		base.Category = "变量";
		base.DisplayName = "字典清空";
		base.Description = "清空字典";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddFlowInput();
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "字典", function);
		generator.AddLine(inputValue + ".Clear();");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字典", graph);
		generator.AddLine(inputValueFromGraph + ".Clear();");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
