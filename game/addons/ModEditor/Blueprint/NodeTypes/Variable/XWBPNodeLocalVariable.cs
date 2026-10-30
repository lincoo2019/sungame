using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeLocalVariable : XWBPNodeType
{
	public StringName VarName { get; set; } = "局部变量";

	public int VarType { get; set; }

	public XWBPNodeLocalVariable()
	{
		base.TypeId = "__XWBPGraphNode_LocalVariable";
		base.Category = "变量";
		base.DisplayName = "局部变量";
		base.Description = "声明并获取局部变量";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddOutput("值", XWBPNodePortData.PortType.Any);
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		xWBPNodeData.SetMetaData("VarName", VarName);
		xWBPNodeData.SetMetaData("VarType", VarType);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		VarName = nodeData.GetMetaData("VarName").AsStringName();
		VarType = nodeData.GetMetaData("VarType").AsInt32();
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (portName == "值")
		{
			string text = $"local_{node.Id}";
			generator.NodeVariables[node.Id] = text;
			string inputValue = generator.GetInputValue(node, "值", function);
			generator.AddLine($"var {text} = {inputValue};");
			return text;
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (portName == "值")
		{
			string text = $"local_{node.Id}";
			generator.NodeVariables[node.Id] = text;
			string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
			generator.AddLine($"var {text} = {inputValueFromGraph};");
			return text;
		}
		return "null";
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
