using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector2ToVector3 : XWBPNodeType
{
	public XWBPNodeVector2ToVector3()
	{
		base.TypeId = "__XWBPGraphNode_Vector2ToVector3";
		base.Category = "实用工具";
		base.DisplayName = "Vector2 转 Vector3";
		base.Description = "将 Vector2 转为 Vector3（Z=0）";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("向量", XWBPNodePortData.PortType.Vector2, "", Vector2.Zero);
		AddOutput("结果", XWBPNodePortData.PortType.Vector3);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "向量", function);
			return $"new Vector3({inputValue}.X, {inputValue}.Y, 0f)";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "向量", graph);
			return $"new Vector3({inputValueFromGraph}.X, {inputValueFromGraph}.Y, 0f)";
		}
		return "null";
	}
}
