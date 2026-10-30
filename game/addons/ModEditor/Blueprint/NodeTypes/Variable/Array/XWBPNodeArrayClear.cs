using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArrayClear : XWBPNodeType
{
	public XWBPNodeArrayClear()
	{
		base.TypeId = "__XWBPGraphNode_ArrayClear";
		base.Category = "变量";
		base.DisplayName = "数组清空";
		base.Description = "清空数组";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddFlowInput();
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "数组", function);
		generator.AddLine(inputValue + ".Clear();");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
		generator.AddLine(inputValueFromGraph + ".Clear();");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
