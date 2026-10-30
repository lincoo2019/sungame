using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDictRemove : XWBPNodeType
{
	public XWBPNodeDictRemove()
	{
		base.TypeId = "__XWBPGraphNode_DictRemove";
		base.Category = "变量";
		base.DisplayName = "字典移除";
		base.Description = "按键移除字典元素";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddFlowInput();
		AddInput("字典", XWBPNodePortData.PortType.Dictionary);
		AddInput("键", XWBPNodePortData.PortType.Any);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "字典", function);
		string inputValue2 = generator.GetInputValue(node, "键", function);
		generator.AddLine(inputValue + ".Remove(" + inputValue2 + ");");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "字典", graph);
		string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "键", graph);
		generator.AddLine(inputValueFromGraph + ".Remove(" + inputValueFromGraph2 + ");");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
