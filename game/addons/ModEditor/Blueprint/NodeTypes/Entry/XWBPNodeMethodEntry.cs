using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMethodEntry : XWBPNodeType
{
	public enum Type
	{
		Bp,
		Script,
		Virtual
	}

	public int FunctionId { get; set; }

	public Type MethodType { get; set; }

	public Dictionary MethodData { get; set; } = new Dictionary();

	public XWBPNodeMethodEntry()
	{
		base.TypeId = "__XWBPGraphNode_MethodEntry";
		base.NodeTypeEnumValue = NodeTypeEnum.FunctionEntry;
		base.Category = "入口";
		base.DisplayName = "方法入口";
		base.Color = new Color(0.651f, 0.22f, 0.958f);
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		WriteNodeMetaData(xWBPNodeData);
		return xWBPNodeData;
	}

	private void WriteNodeMetaData(XWBPNodeData nodeData)
	{
		if (nodeData == null)
		{
			return;
		}
		nodeData.SetMetaData("MethodType", (int)MethodType);
		if (MethodType == Type.Bp)
		{
			nodeData.SetMetaData("FunctionId", FunctionId);
		}
		else if (MethodType == Type.Virtual)
		{
			if (MethodData == null)
			{
				Dictionary dictionary = (MethodData = new Dictionary());
			}
			nodeData.SetMetaData("MethodData", MethodData);
		}
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		MethodType = (Type)nodeData.GetMetaData("MethodType").AsInt32();
		if (MethodType == Type.Bp)
		{
			FunctionId = nodeData.GetMetaData("FunctionId").AsInt32();
		}
		else if (MethodType == Type.Virtual)
		{
			MethodData = nodeData.GetMetaData("MethodData").As<Dictionary>();
		}
	}

	public void BuildFunction(XWBPFunctionData function)
	{
		ClearAllPort();
		AddFlowOutput("");
		foreach (XWBPNodePortData input in function.Inputs)
		{
			AddOutput(input.Name, input.PortTypeValue, input.ClassName, input.DefaultValue);
		}
	}

	public void BuildVirtualMethod()
	{
		ClearAllPort();
		AddFlowOutput("");
		Variant value;
		foreach (Variant item in MethodData.TryGetValue("args", out value) ? value.As<Array>() : new Array())
		{
			Dictionary dictionary = item.As<Dictionary>();
			Variant value2;
			string portName = (dictionary.TryGetValue("name", out value2) ? value2.AsString() : "参数");
			Variant value3;
			int num = (dictionary.TryGetValue("type", out value3) ? value3.AsInt32() : 0);
			XWBPNodePortData.PortType portType = XWBPNodeCallMethod.ToPortType(num);
			StringName className = "";
			if (num == 24)
			{
				className = (dictionary.TryGetValue("class_name", out var value4) ? value4.AsStringName() : ((StringName)""));
			}
			AddOutput(portName, portType, className);
		}
	}

	public override void GraphNodeBuildRefresh(GodotObject graphNode)
	{
		if (!(graphNode is XWBPGraphNode { NodeData: not null } xWBPGraphNode))
		{
			return;
		}
		if (MethodType == Type.Bp)
		{
			XWBPEditor editor = xWBPGraphNode.Editor;
			if (editor?.BpScriptData?.Functions != null && editor.BpScriptData.Functions.TryGetValue(FunctionId, out var value))
			{
				BuildFunction(value);
			}
		}
		else if (MethodType == Type.Virtual)
		{
			BuildVirtualMethod();
		}
		BuildNodeData(xWBPGraphNode.NodeData);
		WriteNodeMetaData(xWBPGraphNode.NodeData);
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
