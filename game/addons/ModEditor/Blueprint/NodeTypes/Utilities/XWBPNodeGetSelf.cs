using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeGetSelf : XWBPNodeType
{
	public XWBPNodeGetSelf()
	{
		base.TypeId = "__XWBPGraphNode_GetSelf";
		base.Category = "实用工具";
		base.DisplayName = "获取自身";
		base.Description = "获取当前脚本所在对象";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddOutput("自身", XWBPNodePortData.PortType.Object, "");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "自身")
		{
			return "__XWResolveSelf()";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "自身")
		{
			return "__XWResolveSelf()";
		}
		return "null";
	}
}
