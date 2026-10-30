using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeFrame : XWBPNodeType
{
	public XWBPNodeFrame()
	{
		base.TypeId = "__XWBPGraphNode_Frame";
		base.Category = "实用工具";
		base.DisplayName = "框架";
		base.Description = "装饰框，用于组织节点";
		base.Color = new Color(0.3f, 0.3f, 0.3f);
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
	}
}
