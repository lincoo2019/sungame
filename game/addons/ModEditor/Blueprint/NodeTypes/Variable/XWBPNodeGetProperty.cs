using System;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeGetProperty : XWBPNodeType
{
	public enum Type
	{
		Bp,
		Script
	}

	public int VariableId { get; set; }

	public Type MethodType { get; set; }

	public Dictionary PropertyData { get; set; } = new Dictionary();

	public XWBPNodeGetProperty()
	{
		base.TypeId = "__XWBPGraphNode_GetProperty";
		base.NodeTypeEnumValue = NodeTypeEnum.PropertyGet;
		base.Category = "变量";
		base.DisplayName = "获取属性";
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
		XWBPNodePortData.PortType portType = (XWBPNodePortData.PortType)variable.Type;
		StringName className = "";
		if (variable.Type == Variant.Type.Object)
		{
			className = variable.ClassName;
		}
		AddOutput("值", portType, className);
	}

	public void BuildProperty()
	{
		ClearAllPort();
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
			SetCatalogPresentation(string.IsNullOrWhiteSpace(text2) ? ("获取 " + text) : ("获取 " + text2 + "." + text), ("读取 " + text2 + "." + text).Trim('.'));
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
				string value2 = $"var_{node.Id}";
				generator.NodeVariables[node.Id] = value2;
				string value3 = generator.ToPascalCase(value.Name);
				generator.AddLine($"var {value2}_value = {value3};");
			}
		}
		else
		{
			Dictionary dictionary = node.GetMetaData("PropertyData").As<Dictionary>();
			Variant value4;
			string fallbackName = (dictionary.TryGetValue("name", out value4) ? value4.AsString() : "");
			Variant value5;
			StringName stringName = (dictionary.TryGetValue("base_class_name", out value5) ? value5.AsStringName() : ((StringName)""));
			string value6 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValue(node, stringName, function)));
			string value7 = $"var_{node.Id}";
			generator.NodeVariables[node.Id] = value7;
			generator.AddLine($"var {value7}_value = {value6}.{GetCsPropertyName(dictionary, fallbackName, generator)};");
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		if (node.GetMetaData("MethodType").AsInt32() == 0)
		{
			int key = node.GetMetaData("VariableId").AsInt32();
			if (generator.ScriptData.Variables.TryGetValue(key, out var value))
			{
				string value2 = $"var_{node.Id}";
				generator.NodeVariables[node.Id] = value2;
				string value3 = generator.ToPascalCase(value.Name);
				generator.AddLine($"var {value2}_value = {value3};");
			}
		}
		else
		{
			Dictionary dictionary = node.GetMetaData("PropertyData").As<Dictionary>();
			Variant value4;
			string fallbackName = (dictionary.TryGetValue("name", out value4) ? value4.AsString() : "");
			Variant value5;
			StringName stringName = (dictionary.TryGetValue("base_class_name", out value5) ? value5.AsStringName() : ((StringName)""));
			string value6 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValueFromGraph(node, stringName, graph)));
			string value7 = $"var_{node.Id}";
			generator.NodeVariables[node.Id] = value7;
			generator.AddLine($"var {value7}_value = {value6}.{GetCsPropertyName(dictionary, fallbackName, generator)};");
		}
	}

	public override string GetOutputValue(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPFunctionData function)
	{
		if (generator.NodeVariables.TryGetValue(node.Id, out var value))
		{
			return value + "_value";
		}
		return "null";
	}

	public override string GetOutputValueFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, string portName, XWBPGraphData graph)
	{
		if (generator.NodeVariables.TryGetValue(node.Id, out var value))
		{
			return value + "_value";
		}
		return "null";
	}
}
