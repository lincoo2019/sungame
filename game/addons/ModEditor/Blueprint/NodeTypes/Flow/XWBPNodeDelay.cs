using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeDelay : XWBPNodeType
{
	public XWBPNodeDelay()
	{
		base.TypeId = "__XWBPGraphNode_Delay";
		base.Category = "流程控制";
		base.DisplayName = "延迟";
		base.Description = "延迟指定秒数后继续执行";
		base.Color = new Color(0.4f, 0.4f, 0.2f);
		AddFlowInput();
		AddInput("秒", XWBPNodePortData.PortType.Float, "", 1.0);
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "秒", function);
		generator.AddLine("await ToSignal(GetTree().CreateTimer(" + inputValue + "), \"timeout\");");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "秒", graph);
		generator.AddLine("await ToSignal(GetTree().CreateTimer(" + inputValueFromGraph + "), \"timeout\");");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
