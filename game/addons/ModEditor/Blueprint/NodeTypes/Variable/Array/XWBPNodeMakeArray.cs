using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMakeArray : XWBPNodeType
{
	public XWBPNodeMakeArray()
	{
		base.TypeId = "__XWBPGraphNode_MakeArray";
		base.Category = "变量";
		base.DisplayName = "创建数组";
		base.Description = "从元素创建数组";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("元素0", XWBPNodePortData.PortType.Any);
		AddInput("元素1", XWBPNodePortData.PortType.Any);
		AddInput("元素2", XWBPNodePortData.PortType.Any);
		AddInput("元素3", XWBPNodePortData.PortType.Any);
		AddOutput("数组", XWBPNodePortData.PortType.Array);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "数组")
		{
			List<string> list = new List<string>();
			foreach (XWBPNodePortData inputPort in node.InputPorts)
			{
				list.Add(generator.GetInputValue(node, inputPort.Name, function));
			}
			return "new Godot.Collections.Array { " + string.Join(", ", list) + " }";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "数组")
		{
			List<string> list = new List<string>();
			foreach (XWBPNodePortData inputPort in node.InputPorts)
			{
				list.Add(generator.GetInputValueFromGraph(node, inputPort.Name, graph));
			}
			return "new Godot.Collections.Array { " + string.Join(", ", list) + " }";
		}
		return "null";
	}
}
