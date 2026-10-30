using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

namespace PVZHE.ModEditor.Blueprint;

public partial class XWBPNodeCallMethod : XWBPNodeType
{
	public enum Type
	{
		Bp,
		Script,
		Virtual,
		Super,
		SignalEvent,
		SignalEmit
	}

	public int FunctionId { get; set; }

	public Type MethodType { get; set; }

	public Dictionary MethodData { get; set; } = new Dictionary();

	public XWBPNodeCallMethod()
	{
		base.TypeId = "__XWBPGraphNode_CallMethod";
		base.NodeTypeEnumValue = NodeTypeEnum.Function;
		base.Category = "方法";
		base.DisplayName = "调用方法";
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
			return;
		}
		if (MethodData == null)
		{
			Dictionary dictionary = (MethodData = new Dictionary());
		}
		nodeData.SetMetaData("MethodData", MethodData);
	}

	public override void LoadNodeData(XWBPNodeData nodeData)
	{
		MethodType = (Type)nodeData.GetMetaData("MethodType").AsInt32();
		if (MethodType == Type.Bp)
		{
			FunctionId = nodeData.GetMetaData("FunctionId").AsInt32();
			return;
		}
		MethodData = nodeData.GetMetaData("MethodData").As<Dictionary>();
		RefreshScriptPresentation();
	}

	public static XWBPNodePortData.PortType ToPortType(int variantType)
	{
		if (variantType >= 100)
		{
			return (XWBPNodePortData.PortType)variantType;
		}
		if (variantType >= 0 && variantType <= 29)
		{
			return (XWBPNodePortData.PortType)variantType;
		}
		return XWBPNodePortData.PortType.Any;
	}

	public void BuildFunction(XWBPFunctionData function)
	{
		ClearAllPort();
		MethodData = new Dictionary();
		AddFlowInput("");
		AddFlowOutput("");
		foreach (XWBPNodePortData input in function.Inputs)
		{
			AddInput(input.Name, input.PortTypeValue, input.ClassName, input.DefaultValue);
		}
		foreach (XWBPNodePortData output in function.Outputs)
		{
			AddOutput(output.Name, output.PortTypeValue, output.ClassName, output.DefaultValue);
		}
	}

	public void BuildMethod()
	{
		ClearAllPort();
		Variant value;
		Godot.Collections.Array obj = (MethodData.TryGetValue("args", out value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array());
		Variant value2;
		Variant value3;
		int num = (MethodData.TryGetValue("return", out value2) ? (value2.As<Dictionary>().TryGetValue("type", out value3) ? value3.AsInt32() : 0) : 0);
		AddFlowInput("");
		AddFlowOutput("");
		Variant value4;
		bool flag = MethodData.TryGetValue("is_static", out value4) && value4.AsBool();
		bool flag2 = IsSelfMember(MethodData);
		if (MethodType != Type.Super && !flag && !flag2)
		{
			Variant value5;
			StringName stringName = (MethodData.TryGetValue("base_class_name", out value5) ? value5.AsStringName() : ((StringName)""));
			AddInput(stringName, XWBPNodePortData.PortType.Object, stringName);
		}
		foreach (Variant item in obj)
		{
			Dictionary dictionary = item.As<Dictionary>();
			Variant value6;
			string portName = (dictionary.TryGetValue("name", out value6) ? value6.AsString() : "参数");
			Variant value7;
			int num2 = (dictionary.TryGetValue("type", out value7) ? value7.AsInt32() : 0);
			XWBPNodePortData.PortType portType = ToPortType(num2);
			StringName className = "";
			if (num2 == 24)
			{
				className = (dictionary.TryGetValue("class_name", out var value8) ? value8.AsStringName() : ((StringName)""));
			}
			AddInput(portName, portType, className);
		}
		if (num != 0)
		{
			XWBPNodePortData.PortType portType2 = ToPortType(num);
			StringName className2 = "";
			if (num == 24)
			{
				className2 = ((!MethodData.TryGetValue("return", out var value9)) ? "" : (value9.As<Dictionary>().TryGetValue("class_name", out var value10) ? ((string?)value10.AsStringName()) : ""));
			}
			AddOutput("返回值", portType2, className2);
		}
		RefreshScriptPresentation();
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
		else if (MethodType == Type.SignalEvent)
		{
			BuildSignalEvent();
		}
		else if (MethodType == Type.SignalEmit)
		{
			BuildSignalEmit();
		}
		else
		{
			BuildMethod();
		}
		BuildNodeData(xWBPGraphNode.NodeData);
		WriteNodeMetaData(xWBPGraphNode.NodeData);
	}

	public void BuildSignalEvent()
	{
		ClearAllPort();
		Variant value;
		Godot.Collections.Array obj = (MethodData.TryGetValue("args", out value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array());
		AddFlowOutput();
		foreach (Variant item in obj)
		{
			Dictionary dictionary = item.As<Dictionary>();
			Variant value2;
			string portName = (dictionary.TryGetValue("name", out value2) ? value2.AsString() : "参数");
			Variant value3;
			int num = (dictionary.TryGetValue("type", out value3) ? value3.AsInt32() : 0);
			XWBPNodePortData.PortType portType = ToPortType(num);
			StringName className = "";
			if (num == 24)
			{
				className = (dictionary.TryGetValue("class_name", out var value4) ? value4.AsStringName() : ((StringName)""));
			}
			AddOutput(portName, portType, className);
		}
		RefreshScriptPresentation();
	}

	public void BuildSignalEmit()
	{
		ClearAllPort();
		Variant value;
		Godot.Collections.Array obj = (MethodData.TryGetValue("args", out value) ? value.As<Godot.Collections.Array>() : new Godot.Collections.Array());
		AddFlowInput("");
		AddFlowOutput("");
		foreach (Variant item in obj)
		{
			Dictionary dictionary = item.As<Dictionary>();
			Variant value2;
			string portName = (dictionary.TryGetValue("name", out value2) ? value2.AsString() : "参数");
			Variant value3;
			int num = (dictionary.TryGetValue("type", out value3) ? value3.AsInt32() : 0);
			XWBPNodePortData.PortType portType = ToPortType(num);
			StringName className = "";
			if (num == 24)
			{
				className = (dictionary.TryGetValue("class_name", out var value4) ? value4.AsStringName() : ((StringName)""));
			}
			AddInput(portName, portType, className);
		}
		RefreshScriptPresentation();
	}

	private void RefreshScriptPresentation()
	{
		if (MethodType != Type.Bp && MethodData != null)
		{
			Variant value;
			string text = (MethodData.TryGetValue("cs_name", out value) ? value.AsString() : "");
			if (string.IsNullOrWhiteSpace(text))
			{
				text = (MethodData.TryGetValue("name", out var value2) ? value2.AsString() : "方法");
			}
			Variant value3;
			string text2 = (MethodData.TryGetValue("base_class_name", out value3) ? value3.AsString() : "");
			if (text2.Contains(".", StringComparison.Ordinal))
			{
				text2 = text2.Substring(text2.LastIndexOf('.') + 1);
			}
			SetCatalogPresentation(MethodType switch
			{
				Type.SignalEvent => "事件 " + text, 
				Type.SignalEmit => "发射 " + text, 
				Type.Super => "Super " + text, 
				_ => string.IsNullOrWhiteSpace(text2) ? (text + "()") : (text2 + "." + text + "()"), 
			}, string.IsNullOrWhiteSpace(text2) ? text : (text2 + "." + text));
		}
	}

	private static string GetCsMemberName(Dictionary methodData, string fallbackName, XWBPCodeGenerator generator)
	{
		if (methodData.TryGetValue("cs_name", out var value))
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		return generator.ToCsMethodName(fallbackName);
	}

	private static string GetCsClassName(Dictionary methodData)
	{
		if (methodData.TryGetValue("cs_qualified_class_name", out var value))
		{
			string text = value.AsString();
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
		}
		if (methodData.TryGetValue("cs_class_name", out var value2))
		{
			string text2 = value2.AsString();
			if (!string.IsNullOrEmpty(text2))
			{
				return text2;
			}
		}
		if (!methodData.TryGetValue("base_class_name", out var value3))
		{
			return "";
		}
		return value3.AsString();
	}

	private static bool IsStaticMember(Dictionary methodData)
	{
		if (methodData.TryGetValue("is_static", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	private static bool IsSelfMember(Dictionary methodData)
	{
		if (methodData.TryGetValue("xw_self_member", out var value))
		{
			return value.AsBool();
		}
		return false;
	}

	private static bool HasValueOutput(XWBPNodeData node)
	{
		foreach (XWBPNodePortData outputPort in node.OutputPorts)
		{
			if (outputPort.PortTypeValue != XWBPNodePortData.PortType.Flow)
			{
				return true;
			}
		}
		return false;
	}

	public override void GenerateCode(XWBPCodeGenerator generator, XWBPNodeData node, XWBPFunctionData function)
	{
		int num = node.GetMetaData("MethodType").AsInt32();
		List<string> list = new List<string>();
		switch (num)
		{
		case 0:
		{
			int key = node.GetMetaData("FunctionId").AsInt32();
			if (!generator.ScriptData.Functions.TryGetValue(key, out var value12))
			{
				break;
			}
			string text3 = generator.ToCsMethodName(value12.Name);
			foreach (XWBPNodePortData input in value12.Inputs)
			{
				list.Add(generator.GetInputValue(node, input.Name, function));
			}
			if (value12.Outputs.Count > 0)
			{
				string value13 = $"var_{node.Id}";
				generator.NodeVariables[node.Id] = value13;
				string value14 = text3 + "(" + string.Join(", ", list) + ")";
				generator.AddLine($"var {value13}_result = {value14};");
			}
			else
			{
				generator.AddLine(text3 + "(" + string.Join(", ", list) + ");");
			}
			break;
		}
		case 5:
		{
			Dictionary dictionary4 = node.GetMetaData("MethodData").As<Dictionary>();
			Variant value9;
			string snakeName = (dictionary4.TryGetValue("name", out value9) ? value9.AsStringName().ToString() : "");
			Variant value10;
			foreach (Variant item in dictionary4.TryGetValue("args", out value10) ? value10.As<Godot.Collections.Array>() : new Godot.Collections.Array())
			{
				Variant value11;
				string portName3 = (item.As<Dictionary>().TryGetValue("name", out value11) ? value11.AsString() : "arg");
				list.Add(generator.GetInputValue(node, portName3, function));
			}
			string text2 = generator.ToPascalCase(snakeName);
			if (list.Count == 0)
			{
				generator.AddLine("EmitSignal(SignalName." + text2 + ");");
				break;
			}
			generator.AddLine($"EmitSignal(SignalName.{text2}, {string.Join(", ", list)});");
			break;
		}
		default:
		{
			Dictionary dictionary = node.GetMetaData("MethodData").As<Dictionary>();
			Variant value;
			string text = (dictionary.TryGetValue("name", out value) ? value.AsStringName().ToString() : "");
			Variant value2;
			Godot.Collections.Array array = (dictionary.TryGetValue("args", out value2) ? value2.As<Godot.Collections.Array>() : new Godot.Collections.Array());
			string csMemberName = GetCsMemberName(dictionary, text, generator);
			switch (num)
			{
			case 3:
				if (!generator.ShouldCallSuper(text))
				{
					generator.AddLine("// Warning: Parent class does not implement " + text);
					break;
				}
				foreach (Variant item2 in array)
				{
					Variant value8;
					string name = (item2.As<Dictionary>().TryGetValue("name", out value8) ? value8.AsString() : "arg");
					list.Add(generator.ToValidIdentifier(name));
				}
				if (list.Count == 0)
				{
					generator.AddLine("base." + csMemberName + "();");
					break;
				}
				generator.AddLine($"base.{csMemberName}({string.Join(", ", list)});");
				break;
			case 2:
				foreach (Variant item3 in array)
				{
					Dictionary dictionary3 = item3.As<Dictionary>();
					Variant value7;
					string portName2 = (dictionary3.TryGetValue("name", out value7) ? value7.AsString() : "arg");
					string inputValue2 = generator.GetInputValue(node, portName2, function);
					list.Add(generator.ConvertToCSharpMemberValue(inputValue2, dictionary3));
				}
				generator.AddLine($"base.{csMemberName}({string.Join(", ", list)});");
				break;
			case 1:
			{
				Variant value3;
				StringName stringName = (dictionary.TryGetValue("base_class_name", out value3) ? value3.AsStringName() : ((StringName)""));
				string value4 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValue(node, stringName, function)));
				foreach (Variant item4 in array)
				{
					Dictionary dictionary2 = item4.As<Dictionary>();
					Variant value5;
					string portName = (dictionary2.TryGetValue("name", out value5) ? value5.AsString() : "arg");
					string inputValue = generator.GetInputValue(node, portName, function);
					list.Add(generator.ConvertToCSharpMemberValue(inputValue, dictionary2));
				}
				if (HasValueOutput(node))
				{
					string value6 = $"var_{node.Id}";
					generator.NodeVariables[node.Id] = value6;
					generator.AddLine($"var {value6}_result = {value4}.{csMemberName}({string.Join(", ", list)});");
				}
				else
				{
					generator.AddLine($"{value4}.{csMemberName}({string.Join(", ", list)});");
				}
				break;
			}
			}
			break;
		}
		}
		XWBPNodeData nextNode = generator.GetNextNode(node, 0, function);
		if (nextNode != null)
		{
			generator.GenerateNodeCode(nextNode, function);
		}
	}

	public override void GenerateCodeFromGraph(XWBPCodeGenerator generator, XWBPNodeData node, XWBPGraphData graph)
	{
		int num = node.GetMetaData("MethodType").AsInt32();
		List<string> list = new List<string>();
		switch (num)
		{
		case 0:
		{
			int key = node.GetMetaData("FunctionId").AsInt32();
			if (!generator.ScriptData.Functions.TryGetValue(key, out var value12))
			{
				break;
			}
			string text3 = generator.ToCsMethodName(value12.Name);
			foreach (XWBPNodePortData input in value12.Inputs)
			{
				list.Add(generator.GetInputValueFromGraph(node, input.Name, graph));
			}
			if (value12.Outputs.Count > 0)
			{
				string value13 = $"var_{node.Id}";
				generator.NodeVariables[node.Id] = value13;
				string value14 = text3 + "(" + string.Join(", ", list) + ")";
				generator.AddLine($"var {value13}_result = {value14};");
			}
			else
			{
				generator.AddLine(text3 + "(" + string.Join(", ", list) + ");");
			}
			break;
		}
		case 5:
		{
			Dictionary dictionary4 = node.GetMetaData("MethodData").As<Dictionary>();
			Variant value9;
			string snakeName = (dictionary4.TryGetValue("name", out value9) ? value9.AsStringName().ToString() : "");
			Variant value10;
			foreach (Variant item in dictionary4.TryGetValue("args", out value10) ? value10.As<Godot.Collections.Array>() : new Godot.Collections.Array())
			{
				Variant value11;
				string portName3 = (item.As<Dictionary>().TryGetValue("name", out value11) ? value11.AsString() : "arg");
				list.Add(generator.GetInputValueFromGraph(node, portName3, graph));
			}
			string text2 = generator.ToPascalCase(snakeName);
			if (list.Count == 0)
			{
				generator.AddLine("EmitSignal(SignalName." + text2 + ");");
				break;
			}
			generator.AddLine($"EmitSignal(SignalName.{text2}, {string.Join(", ", list)});");
			break;
		}
		default:
		{
			Dictionary dictionary = node.GetMetaData("MethodData").As<Dictionary>();
			Variant value;
			string text = (dictionary.TryGetValue("name", out value) ? value.AsStringName().ToString() : "");
			Variant value2;
			Godot.Collections.Array array = (dictionary.TryGetValue("args", out value2) ? value2.As<Godot.Collections.Array>() : new Godot.Collections.Array());
			string csMemberName = GetCsMemberName(dictionary, text, generator);
			switch (num)
			{
			case 3:
				if (!generator.ShouldCallSuper(text))
				{
					generator.AddLine("// Warning: Parent class does not implement " + text);
					break;
				}
				foreach (Variant item2 in array)
				{
					Variant value8;
					string name = (item2.As<Dictionary>().TryGetValue("name", out value8) ? value8.AsString() : "arg");
					list.Add(generator.ToValidIdentifier(name));
				}
				if (list.Count == 0)
				{
					generator.AddLine("base." + csMemberName + "();");
					break;
				}
				generator.AddLine($"base.{csMemberName}({string.Join(", ", list)});");
				break;
			case 2:
				foreach (Variant item3 in array)
				{
					Dictionary dictionary3 = item3.As<Dictionary>();
					Variant value7;
					string portName2 = (dictionary3.TryGetValue("name", out value7) ? value7.AsString() : "arg");
					string inputValueFromGraph2 = generator.GetInputValueFromGraph(node, portName2, graph);
					list.Add(generator.ConvertToCSharpMemberValue(inputValueFromGraph2, dictionary3));
				}
				generator.AddLine($"base.{csMemberName}({string.Join(", ", list)});");
				break;
			case 1:
			{
				Variant value3;
				StringName stringName = (dictionary.TryGetValue("base_class_name", out value3) ? value3.AsStringName() : ((StringName)""));
				string value4 = (IsStaticMember(dictionary) ? GetCsClassName(dictionary) : (IsSelfMember(dictionary) ? "this" : generator.GetInputValueFromGraph(node, stringName, graph)));
				foreach (Variant item4 in array)
				{
					Dictionary dictionary2 = item4.As<Dictionary>();
					Variant value5;
					string portName = (dictionary2.TryGetValue("name", out value5) ? value5.AsString() : "arg");
					string inputValueFromGraph = generator.GetInputValueFromGraph(node, portName, graph);
					list.Add(generator.ConvertToCSharpMemberValue(inputValueFromGraph, dictionary2));
				}
				if (HasValueOutput(node))
				{
					string value6 = $"var_{node.Id}";
					generator.NodeVariables[node.Id] = value6;
					generator.AddLine($"var {value6}_result = {value4}.{csMemberName}({string.Join(", ", list)});");
				}
				else
				{
					generator.AddLine($"{value4}.{csMemberName}({string.Join(", ", list)});");
				}
				break;
			}
			}
			break;
		}
		case 4:
			break;
		}
		XWBPNodeData nextNodeFromGraph = generator.GetNextNodeFromGraph(node, 0, graph);
		if (nextNodeFromGraph != null)
		{
			generator.GenerateNodeCodeFromGraph(nextNodeFromGraph, graph);
		}
	}
}
