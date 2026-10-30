using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeArraySet : XWBPNodeType
{
	public XWBPNodeArraySet()
	{
		base.TypeId = "__XWBPGraphNode_ArraySet";
		base.Category = "变量";
		base.DisplayName = "数组设置";
		base.Description = "按索引设置数组元素";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddFlowInput();
		AddInput("数组", XWBPNodePortData.PortType.Array);
		AddInput("索引", XWBPNodePortData.PortType.Integer, "", 0);
		AddInput("元素", XWBPNodePortData.PortType.Any);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "数组", function);
		string inputValue2 = generator.GetInputValue(node, "索引", function);
		string inputValue3 = generator.GetInputValue(node, "元素", function);
		generator.AddLine($"{inputValue}[{inputValue2}] = {inputValue3};");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "数组", graph);
		string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "索引", graph);
		string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "元素", graph);
		generator.AddLine($"{inputValueFromGraph}[{inputValueFromGraph2}] = {inputValueFromGraph3};");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
