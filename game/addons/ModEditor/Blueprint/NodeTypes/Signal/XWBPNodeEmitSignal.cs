using System.Text;
using Godot;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeEmitSignal : XWBPNodeType
{
	public int SignalId { get; set; } = -1;

	public XWBPNodeEmitSignal()
	{
		base.TypeId = "__XWBPGraphNode_EmitSignal";
		base.NodeTypeEnumValue = NodeTypeEnum.General;
		base.Category = "事件";
		base.DisplayName = "发射信号";
		base.Description = "发射蓝图定义的信号";
		base.Color = new Color(0.6f, 0.4f, 0.2f);
	}

	public override XWBPNodeData CreateNodeData()
	{
		XWBPNodeData xWBPNodeData = base.CreateNodeData();
		xWBPNodeData.SetMetaData("SignalId", SignalId);
		return xWBPNodeData;
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		SignalId = nodeData.GetMetaData("SignalId").AsInt32();
	}

	public override void GraphNodeInit(GodotObject graphNode)
	{
		if (SignalId == -1)
		{
			return;
		}
		XWBPEditor xWBPEditor = (graphNode as XWBPGraphNode)?.Editor;
		if (xWBPEditor?.BpScriptData?.SignalDatas != null)
		{
			if (!xWBPEditor.BpScriptData.SignalDatas.TryGetValue(SignalId, out var value))
			{
				(graphNode as XWBPGraphNode)?.RemoveSelf();
			}
			else
			{
				(graphNode as XWBPGraphNode).Title = "发射 " + value.Name;
			}
		}
	}

	public override void GraphNodeBuildRefresh(GodotObject graphNode)
	{
		if (SignalId == -1)
		{
			return;
		}
		XWBPEditor xWBPEditor = (graphNode as XWBPGraphNode)?.Editor;
		if (xWBPEditor?.BpScriptData?.SignalDatas != null && xWBPEditor.BpScriptData.SignalDatas.TryGetValue(SignalId, out var value))
		{
			BuildSignal(value);
			XWBPNodeData xWBPNodeData = (graphNode as XWBPGraphNode)?.NodeData;
			if (xWBPNodeData != null)
			{
				BuildNodeData(xWBPNodeData);
				xWBPNodeData.SetMetaData("SignalId", SignalId);
			}
		}
	}

	public void BuildSignal(XWBPSignalData signalData)
	{
		ClearAllPort();
		AddFlowInput("");
		AddFlowOutput("");
		foreach (XWBPNodePortData input in signalData.Inputs)
		{
			AddInput(input.Name, input.PortTypeValue, input.ClassName, input.DefaultValue);
		}
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		int key = node.GetMetaData("SignalId").AsInt32();
		if (generator.ScriptData?.SignalDatas == null || !generator.ScriptData.SignalDatas.TryGetValue(key, out var value))
		{
			XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
			if (nextNode != null)
			{
				generator.GenerateNodeCode(nextNode, function);
			}
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (XWBPNodePortData input in value.Inputs)
		{
			if (!flag)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(generator.GetInputValue(node, input.Name, function));
			flag = false;
		}
		string text = generator.ToPascalCase(value.Name.ToString());
		if (stringBuilder.Length == 0)
		{
			generator.AddLine("EmitSignal(SignalName." + text + ");");
		}
		else
		{
			generator.AddLine($"EmitSignal(SignalName.{text}, {stringBuilder});");
		}
		XWBPNodeData nextNode2 = generator.GetNextNode(node, 0, function);
		if (nextNode2 != null)
		{
			generator.GenerateNodeCode(nextNode2, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		int key = node.GetMetaData("SignalId").AsInt32();
		if (generator.ScriptData?.SignalDatas == null || !generator.ScriptData.SignalDatas.TryGetValue(key, out var value))
		{
			XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
			if (nextNodeFromGraph != null)
			{
				generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
			}
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		bool flag = true;
		foreach (XWBPNodePortData input in value.Inputs)
		{
			if (!flag)
			{
				stringBuilder.Append(", ");
			}
			stringBuilder.Append(generator.GetInputValueFromGraph(node, input.Name, graph));
			flag = false;
		}
		string text = generator.ToPascalCase(value.Name.ToString());
		if (stringBuilder.Length == 0)
		{
			generator.AddLine("EmitSignal(SignalName." + text + ");");
		}
		else
		{
			generator.AddLine($"EmitSignal(SignalName.{text}, {stringBuilder});");
		}
		XWBPNodeData nextNodeFromGraph2 = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph2 != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph2, graph);
		}
	}
}
