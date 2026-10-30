using System;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeSetProperty : XWBPNodeType
{
	public enum Type
	{
		Bp,
		Script
	}

	public int VariableId { get; set; }

	public Type MethodType { get; set; }

	public Dictionary PropertyData { get; set; } = new Dictionary();

	public XWBPNodeSetProperty()
	{
		base.TypeId = "__XWBPGraphNode_SetProperty";
		base.NodeTypeEnumValue = NodeTypeEnum.PropertySet;
		base.Category = "变量";
		base.DisplayName = "设置属性";
		base.Color = new Color(0.4f, 0.6f, 0.3f);
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
			nodeData.SetMetaData("VariableId", VariableId);
			return;
		}
		if (PropertyData == null)
		{
			Dictionary dictionary = (PropertyData = new Dictionary());
		}
		nodeData.SetMetaData("PropertyData", PropertyData);
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		MethodType = (Type)nodeData.GetMetaData("MethodType").AsInt32();
		if (MethodType == Type.Bp)
		{
			VariableId = nodeData.GetMetaData("VariableId").AsInt32();
			return;
		}
		PropertyData = nodeData.GetMetaData("PropertyData").As<Dictionary>();
		RefreshScriptPresentation();
	}

	public void BuildVariable(XWBPVariableData variable)
	{
		ClearAllPort();
		AddFlowInput("");
		AddFlowOutput("");
		XWBPNodePortData.PortType portType = (XWBPNodePortData.PortType)variable.Type;
		StringName className = "";
		if (variable.Type == Variant.Type.Object)
		{
			className = variable.ClassName;
		}
		AddInput("值", portType, className);
		AddOutput("值", portType, className);
	}

	public void BuildProperty()
	{
		ClearAllPort();
		AddFlowInput("");
		AddFlowOutput("");
		Variant value;
		StringName stringName = (PropertyData.TryGetValue("base_class_name", out value) ? value.AsStringName() : ((StringName)""));
		Variant value2;
		int num = (PropertyData.TryGetValue("type", out value2) ? value2.AsInt32() : 0);
		if ((!PropertyData.TryGetValue("is_static", out var value3) || !value3.AsBool()) && !IsSelfMember(PropertyData))
		{
			AddInput(stringName, XWBPNodePortData.PortType.Object, stringName);
		}
		XWBPNodePortData.PortType portType = (XWBPNodePortData.PortType)num;
		StringName className = "";
		if (num == 24)
		{
			className = (PropertyData.TryGetValue("class_name", out var value4) ? value4.AsStringName() : ((StringName)""));
		}
		AddInput("值", portType, className);
		AddOutput("值", portType, className);
		RefreshScriptPresentation();
	}

	private void RefreshScriptPresentation()
	{
		if (MethodType == Type.Script && PropertyData != null)
		{
			Variant value;
			string text = (PropertyData.TryGetValue("cs_name", out value) ? value.AsString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = (PropertyData.TryGetValue("name", out var value2) ? value2.AsString() : "属性");
			}
			Variant value3;
			string text2 = (PropertyData.TryGetValue("base_class_name", out value3) ? value3.AsString() : "");
			if (text2.Contains(".", StringComparison.Ordinal))
			{
				text2 = text2.Substring(text2.LastIndexOf('.') + 1);
			}
			SetCatalogPresentation(string.IsNullOrWhiteSpace(text2) ? ("设置 " + text) : ("设置 " + text2 + "." + text), ("写入 " + text2 + "." + text).Trim('.'));
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
			if (editor?.BpScriptData?.Variables != null && editor.BpScriptData.Variables.TryGetValue(VariableId, out var value))
			{
				BuildVariable(value);
			}
		}
		else
		{
			BuildProperty();
		}
		BuildNodeData(xWBPGraphNode.NodeData);
		WriteNodeMetaData(xWBPGraphNode.NodeData);
	}

	private static string GetCsPropertyName(Dictionary propertyData, string fallbackName, XWBPCodeGenerator generator)
	{
		if (propertyData.TryGetValue("cs_name", out var value))
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return generator.ToPascalCase(fallbackName);
	}

	private static string GetCsClassName(Dictionary propertyData)
	{
		if (propertyData.TryGetValue("cs_qualified_class_name", out var value))
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		if (propertyData.TryGetValue("cs_class_name", out var value2))
		{
			string text2 = value2.AsString();
			if (!string.IsNullOrEmpty(text2))
			{
				return text2;
			}
		}
		if (!propertyData.TryGetValue("base_class_name", out var value3))
		{
			return "";
		}
		return value3.AsString();
	}

	private static bool IsStaticMember(Dictionary propertyData)
	{
		if (propertyData.TryGetValue("is_static", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	private static bool IsSelfMember(Dictionary propertyData)
	{
		if (propertyData.TryGetValue("xw_self_member", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		if (node.GetMetaData("MethodType").AsInt32() == 0)
		{
			int key = node.GetMetaData("VariableId").AsInt32();
			if (generator.ScriptData.Variables.TryGetValue(key, out var value))
			{
				string text = generator.ToPascalCase(value.Name);
				string inputValue = generator.GetInputValue(node, "值", function);
				generator.AddLine(text + " = " + inputValue + ";");
			}
		}
		else
		{
			Dictionary dictionary = node.GetMetaData("PropertyData").As<Dictionary>();
			Variant value2;
			string fallbackName = (dictionary.TryGetValue("name", out value2) ? value2.AsString() : "");
			Variant value3;
			StringName stringName = (dictionary.TryGetValue("base_class_name", out value3) ? value3.AsStringName() : ((StringName)""));
			string value4 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValue(node, stringName, function)));
			string value5 = generator.ConvertToCSharpMemberValue(generator.GetInputValue(node, "值", function), dictionary);
			generator.AddLine($"{value4}.{GetCsPropertyName(dictionary, fallbackName, generator)} = {value5};");
		}
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		if (node.GetMetaData("MethodType").AsInt32() == 0)
		{
			int key = node.GetMetaData("VariableId").AsInt32();
			if (generator.ScriptData.Variables.TryGetValue(key, out var value))
			{
				string text = generator.ToPascalCase(value.Name);
				string inputValueFromGraph = generator.GetInputValueFromGraph(node, "值", graph);
				generator.AddLine(text + " = " + inputValueFromGraph + ";");
			}
		}
		else
		{
			Dictionary dictionary = node.GetMetaData("PropertyData").As<Dictionary>();
			Variant value2;
			string fallbackName = (dictionary.TryGetValue("name", out value2) ? value2.AsString() : "");
			Variant value3;
			StringName stringName = (dictionary.TryGetValue("base_class_name", out value3) ? value3.AsStringName() : ((StringName)""));
			string value4 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValueFromGraph(node, stringName, graph)));
			string value5 = generator.ConvertToCSharpMemberValue(generator.GetInputValueFromGraph(node, "值", graph), dictionary);
			generator.AddLine($"{value4}.{GetCsPropertyName(dictionary, fallbackName, generator)} = {value5};");
		}
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
