using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeMatch : XWBPNodeType
{
	public XWBPNodeMatch()
	{
		base.TypeId = "__XWBPGraphNode_Match";
		base.Category = "流程控制";
		base.DisplayName = "匹配";
		base.Description = "根据值匹配执行不同分支";
		base.Color = new Color(0.2f, 0.4f, 0.6f);
		AddFlowInput();
		AddInput("值", XWBPNodePortData.PortType.Any);
		AddFlowOutput("默认");
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		string inputValue = generator.GetInputValue(node, "值", function);
		generator.AddLine("switch (" + inputValue + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		EmitCases(generator, node, function, isGraph: false);
		generator.AddLine("default:");
		generator.IndentLevel++;
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
		generator.AddLine("break;");
		generator.IndentLevel--;
		generator.IndentLevel--;
		generator.AddLine("}");
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
		generator.AddLine("switch (" + inputValueFromGraph + ")");
		generator.AddLine("{");
		generator.IndentLevel++;
		EmitCases(generator, node, graph, isGraph: true);
		generator.AddLine("default:");
		generator.IndentLevel++;
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
		generator.AddLine("break;");
		generator.IndentLevel--;
		generator.IndentLevel--;
		generator.AddLine("}");
	}

	private void EmitCases(XWBPCodeGenerator generator, XWBPNodeData node, GodotObject context, bool isGraph)
	{
		Variant metaData = node.GetMetaData("Cases");
		if (metaData.VariantType != Variant.Type.Array)
		{
			return;
		}
		Array array = metaData.As<Array>();
		for (int i = 0; i < array.Count; i++)
		{
			if (array[i].VariantType != Variant.Type.Dictionary)
			{
				continue;
			}
			Dictionary dictionary = array[i].As<Dictionary>();
			if (!dictionary.TryGetValue("value", out var value))
			{
				continue;
			}
			string text = ((value.VariantType == Variant.Type.String) ? ("\"" + value.AsString() + "\"") : value.AsString());
			Variant value2;
			int outputIndex = (dictionary.TryGetValue("portIndex", out value2) ? value2.AsInt32() : (i + 1));
			generator.AddLine("case " + text + ":");
			generator.IndentLevel++;
			if (isGraph)
			{
				XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, outputIndex, (XWBPGraphData)context);
				if (nextNodeFromGraph != null)
				{
					generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, (XWBPGraphData)context);
				}
			}
			else
			{
				XWBPNodeData nextNode = generator.GetNextNode(node, outputIndex, (XWBPFunctionData)context);
				if (nextNode != null)
				{
					generator.GenerateNodeCode(nextNode, (XWBPFunctionData)context);
				}
			}
			generator.AddLine("break;");
			generator.IndentLevel--;
		}
	}
}
