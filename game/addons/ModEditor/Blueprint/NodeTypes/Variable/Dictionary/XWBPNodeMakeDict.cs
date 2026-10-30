using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMakeDict : XWBPNodeType
{
	public XWBPNodeMakeDict()
	{
		base.TypeId = "__XWBPGraphNode_MakeDict";
		base.Category = "变量";
		base.DisplayName = "创建字典";
		base.Description = "从键值对创建字典";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("键0", XWBPNodePortData.PortType.Any);
		AddInput("值0", XWBPNodePortData.PortType.Any);
		AddInput("键1", XWBPNodePortData.PortType.Any);
		AddInput("值1", XWBPNodePortData.PortType.Any);
		AddOutput("字典", XWBPNodePortData.PortType.Dictionary);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "字典")
		{
			string inputValue = generator.GetInputValue(node, "键0", function);
			string inputValue2 = generator.GetInputValue(node, "值0", function);
			string inputValue3 = generator.GetInputValue(node, "键1", function);
			string inputValue4 = generator.GetInputValue(node, "值1", function);
			return $"new Godot.Collections.Dictionary {{ {{ {inputValue}, {inputValue2} }}, {{ {inputValue3}, {inputValue4} }} }}";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "字典")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "键0", graph);
			string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, "值0", graph);
			string inputValueFromGraph3 = generator.GetInputValueFromGraph(node, "键1", graph);
			string inputValueFromGraph4 = generator.GetInputValueFromGraph(node, "值1", graph);
			return $"new Godot.Collections.Dictionary {{ {{ {inputValueFromGraph}, {inputValueFromGraph2} }}, {{ {inputValueFromGraph3}, {inputValueFromGraph4} }} }}";
		}
		return "null";
	}
}
