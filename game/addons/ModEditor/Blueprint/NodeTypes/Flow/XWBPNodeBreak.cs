using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeBreak : XWBPNodeType
{
	public XWBPNodeBreak()
	{
		base.TypeId = "__XWBPGraphNode_Break";
		base.Category = "流程控制";
		base.DisplayName = "跳出循环";
		base.Description = "跳出当前循环";
		base.Color = new Color(0.8f, 0.4f, 0.2f);
		AddFlowInput();
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		generator.AddLine("break;");
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		generator.AddLine("break;");
	}
}
