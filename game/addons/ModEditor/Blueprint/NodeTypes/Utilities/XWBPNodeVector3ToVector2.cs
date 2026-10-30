using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeVector3ToVector2 : XWBPNodeType
{
	public XWBPNodeVector3ToVector2()
	{
		base.TypeId = "__XWBPGraphNode_Vector3ToVector2";
		base.Category = "实用工具";
		base.DisplayName = "Vector3 转 Vector2";
		base.Description = "将 Vector3 转为 Vector2（舍 Z）";
		base.Color = new Color(0.3f, 0.5f, 0.3f);
		AddInput("向量", XWBPNodePortData.PortType.Vector3, "", Vector3.Zero);
		AddOutput("结果", XWBPNodePortData.PortType.Vector2);
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "结果")
		{
			string inputValue = generator.GetInputValue(node, "向量", function);
			return $"new Vector2({inputValue}.X, {inputValue}.Y)";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "结果")
		{
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "向量", graph);
			return $"new Vector2({inputValueFromGraph}.X, {inputValueFromGraph}.Y)";
		}
		return "null";
	}
}
