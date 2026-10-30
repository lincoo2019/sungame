using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLiteral : XWBPNodeType
{
	public XWBPNodeLiteral()
	{
		base.TypeId = "__XWBPGraphNode_Literal";
		base.Category = "实用工具";
		base.DisplayName = "字面量";
		base.Description = "输出常量值";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddOutput("值", XWBPNodePortData.PortType.Any);
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "值")
		{
			XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName("值");
			if (xWBPNodePortData != null)
			{
				return generator.GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
			}
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "值")
		{
			XWBPNodePortData xWBPNodePortData = node.FindOutputPortByName("值");
			if (xWBPNodePortData != null)
			{
				return generator.GetDefaultValueString(xWBPNodePortData.Value, (int)xWBPNodePortData.PortTypeValue);
			}
		}
		return "null";
	}
}
