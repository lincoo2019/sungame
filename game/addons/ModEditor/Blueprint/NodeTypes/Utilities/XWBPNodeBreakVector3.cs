using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeBreakVector3 : XWBPNodeType
{
	public XWBPNodeBreakVector3()
	{
		base.TypeId = "__XWBPGraphNode_BreakVector3";
		base.Category = "实用工具";
		base.DisplayName = "拆分 Vector3";
		base.Description = "拆分 Vector3 为 X、Y、Z 分量";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("向量", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddOutput("X", XWBPNodePortData.PortType.Float);
		AddOutput("Y", XWBPNodePortData.PortType.Float);
		AddOutput("Z", XWBPNodePortData.PortType.Float);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "向量", function);
		return portName switch
		{
			"X" => inputValue + ".X", 
			"Y" => inputValue + ".Y", 
			"Z" => inputValue + ".Z", 
			_ => "null", 
		};
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "向量", graph);
		return portName switch
		{
			"X" => inputValueFromGraph + ".X", 
			"Y" => inputValueFromGraph + ".Y", 
			"Z" => inputValueFromGraph + ".Z", 
			_ => "null", 
		};
	}
}
