using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeComment : XWBPNodeType
{
	public XWBPNodeComment()
	{
		base.TypeId = "__XWBPGraphNode_Comment";
		base.Category = "实用工具";
		base.DisplayName = "注释";
		base.Description = "在蓝图画布上添加注释";
		base.Color = new Color(0.2f, 0.2f, 0.2f);
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
	}
}
