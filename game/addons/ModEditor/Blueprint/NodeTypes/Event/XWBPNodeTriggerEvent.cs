using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeTriggerEvent : XWBPNodeType
{
	public XWBPNodeTriggerEvent()
	{
		base.TypeId = "__XWBPGraphNode_TriggerEvent";
		base.Category = "事件";
		base.DisplayName = "触发事件";
		base.Description = "触发自定义事件";
		base.Color = new Color(0.8f, 0.6f, 0.2f);
		AddFlowInput();
		AddInput("事件名", XWBPNodePortData.PortType.String, "", "");
		AddFlowOutput("完成");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "事件名", function);
		generator.AddLine("EmitSignal(" + inputValue + ");");
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "事件名", graph);
		generator.AddLine("EmitSignal(" + inputValueFromGraph + ");");
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
