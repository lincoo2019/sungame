using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeContinue : XWBPNodeType
{
	public XWBPNodeContinue()
	{
		base.TypeId = "__XWBPGraphNode_Continue";
		base.Category = "流程控制";
		base.DisplayName = "继续循环";
		base.Description = "跳过本次循环剩余部分";
		base.Color = new Color(0.8f, 0.4f, 0.2f);
		AddFlowInput();
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		generator.AddLine("continue;");
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		generator.AddLine("continue;");
	}
}
