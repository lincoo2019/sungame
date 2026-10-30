using System.Collections.Generic;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeReturn : XWBPNodeType
{
	public XWBPNodeReturn()
	{
		base.TypeId = "__XWBPGraphNode_Return";
		base.Category = "流程控制";
		base.DisplayName = "返回";
		base.Description = "提前返回函数";
		base.Color = new Color(0.8f, 0.2f, 0.2f);
		AddFlowInput();
	}

	public void BuildReturnPorts(XWBPFunctionData functionData)
	{
		ClearAllPort();
		AddFlowInput();
		foreach (XWBPNodePortData output in functionData.Outputs)
		{
			AddInput(output.Name, output.PortTypeValue, output.ClassName, output.DefaultValue);
		}
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		List<XWBPNodePortData> list = new List<XWBPNodePortData>();
		foreach (XWBPNodePortData inputPort in node.InputPorts)
		{
			if (inputPort.PortTypeValue != XWBPNodePortData.PortType.Flow)
			{
				list.Add(inputPort);
			}
		}
		if (list.Count == 0)
		{
			generator.AddLine("return;");
			return;
		}
		if (list.Count == 1)
		{
			string inputValue = generator.GetInputValue(node, list[0].Name, function);
			generator.AddLine("return " + inputValue + ";");
			return;
		}
		List<string> list2 = new List<string>();
		foreach (XWBPNodePortData item in list)
		{
			list2.Add(generator.GetInputValue(node, item.Name, function));
		}
		generator.AddLine("return new object[] { " + string.Join(", ", list2) + " };");
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		List<XWBPNodePortData> list = new List<XWBPNodePortData>();
		foreach (XWBPNodePortData inputPort in node.InputPorts)
		{
			if (inputPort.PortTypeValue != XWBPNodePortData.PortType.Flow)
			{
				list.Add(inputPort);
			}
		}
		if (list.Count == 0)
		{
			generator.AddLine("return;");
			return;
		}
		if (list.Count == 1)
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, list[0].Name, graph);
			generator.AddLine("return " + inputValueFromGraph + ";");
			return;
		}
		List<string> list2 = new List<string>();
		foreach (XWBPNodePortData item in list)
		{
			list2.Add(generator.GetInputValueFromGraph(node, item.Name, graph));
		}
		generator.AddLine("return new object[] { " + string.Join(", ", list2) + " };");
	}
}
